using System.Buffers.Binary;
using System.Text;
using AxGit.Core;

namespace AxGit.Tests;

/// <summary>
/// The index read strictly (docs/axGIT/SPEC.md AG-1.6a; the review's sixth pass of 2026-11-28, G3, H1, H2, H5):
/// what <see cref="GitIndex.Read"/> cannot read as git reads it is not known, which the scan names as a risk, and
/// never a list of gitlinks that may be short. Pure: indexes written by the test, byte by byte, in git's documented
/// format (gitformat-index).
/// </summary>
public sealed class GitIndexTests
{
    internal const int HashLength = 20;

    /// <summary>One entry: its mode, its path as bytes, and the length the flags give (null: the path's own).</summary>
    internal sealed record Entry(uint Mode, byte[] Path, int? FlagsLength = null);

    internal static Entry File(string path) => new(0x81A4, Encoding.UTF8.GetBytes(path));

    internal static Entry Gitlink(string path) => new(0xE000, Encoding.UTF8.GetBytes(path));

    /// <summary>An index of <paramref name="version"/> with the entries in order, its checksum's place left as zeros (git's own check of it is not the scan's).</summary>
    internal static byte[] Index(int version, params Entry[] entries)
    {
        var bytes = new List<byte>();
        void Number(uint value, int size)
        {
            var buffer = new byte[size];
            if (size == 4)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
            }
            else
            {
                BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)value);
            }

            bytes.AddRange(buffer);
        }

        bytes.AddRange("DIRC"u8.ToArray());
        Number((uint)version, 4);
        Number((uint)entries.Length, 4);
        var previous = Array.Empty<byte>();
        foreach (var entry in entries)
        {
            var start = bytes.Count;
            bytes.AddRange(new byte[24]);
            Number(entry.Mode, 4);
            bytes.AddRange(new byte[12]);
            bytes.AddRange(Enumerable.Repeat((byte)0xAB, HashLength));
            var length = entry.FlagsLength ?? Math.Min(entry.Path.Length, 0xFFF);
            Number((uint)length, 2);
            if (version == 4)
            {
                var common = 0;
                while (common < previous.Length && common < entry.Path.Length && previous[common] == entry.Path[common])
                {
                    common++;
                }

                var drop = previous.Length - common;
                if (drop > 127)
                {
                    throw new ArgumentException("the test writes drops below 128 only");
                }

                bytes.Add((byte)drop);
                bytes.AddRange(entry.Path[common..]);
                bytes.Add(0);
                previous = entry.Path;
            }
            else
            {
                bytes.AddRange(entry.Path);
                var size = bytes.Count - start;
                bytes.AddRange(new byte[((size + 8) & ~7) - size]);
            }
        }

        bytes.AddRange(new byte[HashLength]);
        return [.. bytes];
    }

    [Fact]
    public void Plain_indexes_of_every_version_give_their_gitlinks()
    {
        foreach (var version in new[] { 2, 3, 4 })
        {
            var read = GitIndex.Read(Index(version, File("a.txt"), Gitlink("libs/zip"), File("libs/zz.txt")), HashLength);
            Assert.True(read.Known, $"version {version}");
            Assert.Equal(["libs/zip"], read.Gitlinks);
        }
    }

    /// <summary>
    /// H2 (the sixth pass): version 4 writes a path as a number of bytes to drop from the path before it and the rest.
    /// The drop was taken in characters of the decoded path, so a gitlink after a path of letters outside ASCII was
    /// read as another path (here "x/\uFFFDsub" in place of "x/ösub") and its repository never entered. Each path is
    /// now put together as bytes and decoded once.
    /// </summary>
    [Fact]
    public void A_version_4_path_after_one_with_letters_outside_ascii_is_read_whole()
    {
        var read = GitIndex.Read(Index(4, File("x/ä"), Gitlink("x/ösub"), File("y/日本.txt"), Gitlink("y/日本/mod")), HashLength);
        Assert.True(read.Known);
        Assert.Equal(["x/ösub", "y/日本/mod"], read.Gitlinks);
    }

    /// <summary>H1 (the sixth pass): a split index writes an entry that replaces one of its shared index with an empty name; which path it is lies in a bitmap axGIT does not read, so the index is not known.</summary>
    [Fact]
    public void An_entry_with_an_empty_name_is_an_index_axgit_does_not_read()
    {
        Assert.False(GitIndex.Read(Index(2, File("a.txt"), Gitlink(string.Empty)), HashLength).Known);
        Assert.False(GitIndex.Read(Index(2, new Entry(0x81A4, [])), HashLength).Known);
    }

    /// <summary>git takes a name's length from the flags below 0xFFF, not from its zero byte: an index where the two differ is not read as git reads it.</summary>
    [Fact]
    public void A_name_whose_length_in_the_flags_differs_from_its_end_is_not_known()
    {
        Assert.False(GitIndex.Read(Index(2, File("a.txt"), new Entry(0xE000, "libs/zip"u8.ToArray(), FlagsLength: 4)), HashLength).Known);
        Assert.False(GitIndex.Read(Index(4, File("a.txt"), new Entry(0xE000, "libs/zip"u8.ToArray(), FlagsLength: 3)), HashLength).Known);
        // A name of 0xFFF bytes or more has 0xFFF in its flags, and its zero byte ends it.
        var long1 = new string('n', 5000);
        Assert.True(GitIndex.Read(Index(2, File(long1)), HashLength).Known);
    }

    /// <summary>
    /// H5 (the sixth pass): git reads the paths of its index as they are and looks at each in the work tree (a status
    /// looks at every one); a path git's own check (verify_path, with core.protectNTFS as on Windows) would never have
    /// let in names a place outside the work tree, a share among them. Any such path makes the index one axGIT does
    /// not read, whatever its mode.
    /// </summary>
    [Theory]
    [InlineData("//localhost/C$/x.txt")]
    [InlineData("/x.txt")]
    [InlineData(@"\x.txt")]
    [InlineData(@"a\b.txt")]
    [InlineData("C:/x.txt")]
    [InlineData("c:x.txt")]
    [InlineData("a/b.txt:stream")]
    [InlineData("../x.txt")]
    [InlineData("a/../../x.txt")]
    [InlineData("a/./b.txt")]
    [InlineData("a//b.txt")]
    [InlineData("a/")]
    [InlineData(".git/hooks/pre-commit")]
    [InlineData("a/.GIT/config")]
    [InlineData("a/.git./config")]
    [InlineData("a/.git /config")]
    [InlineData("git~1/config")]
    public void A_path_git_would_never_have_let_in_is_not_known(string path)
    {
        Assert.False(GitIndex.Read(Index(2, File("a.txt"), File(path)), HashLength).Known, path);
        Assert.False(GitIndex.IsPath(path), path);
    }

    [Theory]
    [InlineData("a.txt")]
    [InlineData(".gitignore")]
    [InlineData("docs/.gitattributes")]
    [InlineData("a/.github/x.yml")]
    [InlineData("x/ä b.txt")]
    [InlineData("a..b/c")]
    public void An_ordinary_path_is_read(string path)
    {
        Assert.True(GitIndex.Read(Index(2, File(path)), HashLength).Known, path);
        Assert.True(GitIndex.IsPath(path), path);
    }

    /// <summary>A path that is no UTF-8 cannot name a file of Windows as git names it: not known.</summary>
    [Fact]
    public void A_path_that_is_no_utf8_is_not_known()
    {
        Assert.False(GitIndex.Read(Index(2, new Entry(0xE000, [(byte)'x', 0xC3, (byte)'s'])), HashLength).Known);
    }
}
