using System.Buffers.Binary;
using System.Text;

namespace AxGit.Core;

/// <summary>What <see cref="GitIndex.Read"/> found in an index: the paths of its gitlinks, the shared index a split index names, and whether the file was of a form axGIT reads.</summary>
public sealed record IndexEntries(IReadOnlyList<string> Gitlinks, string? SharedIndex, bool Known);

/// <summary>
/// The gitlinks of a repository's index (SPEC.md AG-1.6a; the review's fifth pass, P4): the entries of mode 160000,
/// a submodule or a repository Stage all added, which are where git runs itself inside another repository (tried
/// with git 2.53 on every command axGIT has: git entered such a repository on a status). Read in C# from git's
/// documented format (gitformat-index: the signature DIRC, versions 2, 3 and 4, an entry's fixed part, its flags with
/// the extended flag of version 3, the paths of version 4 written as what is kept of the path before them; the
/// extensions after the entries, of which "link", a split index's, names the shared index whose entries count too).
///
/// Read strictly since the review's sixth pass (2026-11-28, G3): what cannot be read as git reads it is not
/// <see cref="IndexEntries.Known"/>, which the scan names as a risk, never a list of gitlinks that may be short. So a
/// file that ends before it should, an entry with an empty name (a split index writes so an entry that replaces one
/// of its shared index, H1, and which path it is lies in a bitmap not read here), a name whose length in the flags
/// differs from where its zero byte ends it (git takes the flags' length), a path that is no UTF-8, and any path git's
/// own check would never have let in (<see cref="IsPath"/>; git looks at every path of the index in the work tree, and
/// such a path names a place outside it, a share among them, H5). The paths of version 4 are put together as bytes
/// and each decoded once (H2: the bytes dropped were taken as characters). Pure, tested on indexes git wrote and on
/// indexes written byte by byte (<c>GitIndexTests</c>).
/// </summary>
public static class GitIndex
{
    /// <summary>A gitlink's mode, as the index writes it.</summary>
    private const uint GitlinkMode = 0xE000;

    /// <summary>The largest index read: one bigger is not known, rather than read without end.</summary>
    public const long Largest = 512L * 1024 * 1024;

    /// <summary>What the flags of an entry hold for a name of this length or longer, whose zero byte then ends it.</summary>
    private const int LongName = 0xFFF;

    /// <summary>UTF-8 that refuses what is no UTF-8, rather than putting a mark in its place.</summary>
    private static readonly UTF8Encoding Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>The entries of an index file's bytes; <paramref name="hashLength"/>: 20 for SHA-1, 32 for SHA-256 (<c>extensions.objectformat</c>).</summary>
    public static IndexEntries Read(byte[] bytes, int hashLength)
    {
        var unknown = new IndexEntries([], null, Known: false);
        if (bytes.Length < 12 + hashLength || Encoding.ASCII.GetString(bytes, 0, 4) != "DIRC")
        {
            return unknown;
        }

        var version = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(4));
        var count = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8));
        if (version is < 2 or > 4)
        {
            return unknown;
        }

        var gitlinks = new List<string>();
        var at = 12;
        var end = bytes.Length - hashLength;
        var previous = Array.Empty<byte>();
        // The fixed part: ctime, mtime (8 bytes each), dev, ino, mode, uid, gid, size (4 bytes each), the object id, the flags.
        var fixedPart = 40 + hashLength + 2;
        for (var number = 0u; number < count; number++)
        {
            if (at + fixedPart > end)
            {
                return unknown;
            }

            var mode = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(at + 24));
            var flags = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(at + 40 + hashLength));
            var length = flags & LongName;
            var start = at + fixedPart;
            if ((flags & 0x4000) != 0)
            {
                if (version < 3)
                {
                    return unknown;
                }

                start += 2;
            }

            byte[] path;
            if (version == 4)
            {
                // What to take off the end of the path before, in bytes, as a number of git's own varint form, then the rest.
                var drop = 0L;
                var first = true;
                while (true)
                {
                    if (start >= end)
                    {
                        return unknown;
                    }

                    var one = bytes[start++];
                    drop = first ? one & 0x7F : ((drop + 1) << 7) | (long)(one & 0x7F);
                    first = false;
                    if ((one & 0x80) == 0)
                    {
                        break;
                    }

                    if (drop > int.MaxValue)
                    {
                        return unknown;
                    }
                }

                var nul = Array.IndexOf(bytes, (byte)0, start, end - start);
                if (nul < 0 || drop > previous.Length)
                {
                    return unknown;
                }

                path = [.. previous.AsSpan(0, previous.Length - (int)drop), .. bytes.AsSpan(start, nul - start)];
                at = nul + 1;
            }
            else
            {
                var nul = Array.IndexOf(bytes, (byte)0, start, end - start);
                if (nul < 0)
                {
                    return unknown;
                }

                path = bytes[start..nul];
                // An entry is padded with one to eight zero bytes to a multiple of eight from its start.
                var size = nul - at;
                at += (size + 8) & ~7;
                if (at > end)
                {
                    return unknown;
                }
            }

            // git takes a name's length from the flags below 0xFFF, and from its zero byte from there on.
            if (path.Length == 0 || (length < LongName ? path.Length != length : path.Length < LongName))
            {
                return unknown;
            }

            string text;
            try
            {
                text = Strict.GetString(path);
            }
            catch (DecoderFallbackException)
            {
                return unknown;
            }

            if (!IsPath(text))
            {
                return unknown;
            }

            previous = path;
            if ((mode & 0xF000) == GitlinkMode)
            {
                gitlinks.Add(text);
            }
        }

        // The extensions: a signature and a size, each; "link" holds the shared index's object id first.
        string? shared = null;
        while (at + 8 <= end)
        {
            var signature = Encoding.ASCII.GetString(bytes, at, 4);
            var size = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(at + 4));
            if (size > end - at - 8)
            {
                return unknown;
            }

            if (signature == "link")
            {
                if (size < hashLength)
                {
                    return unknown;
                }

                shared = Convert.ToHexString(bytes, at + 8, hashLength).ToLowerInvariant();
            }

            at += 8 + (int)size;
        }

        return at == end ? new IndexEntries(gitlinks, shared, Known: true) : unknown;
    }

    /// <summary>
    /// Whether a path of the index, or of <c>.gitmodules</c>, is one git's own check (verify_path, with
    /// core.protectNTFS as on Windows) lets in: parts split by a forward slash, none of them empty, none ending in a
    /// dot or a space ("." and ".." among them), nor ".git" in any case, nor "git~1", its short name; no backslash and no colon
    /// anywhere (a drive, a stream), no control character. Such a path names a place in the work tree; any other may
    /// name one outside it, a share among them.
    /// </summary>
    public static bool IsPath(string path)
    {
        if (path.Length == 0 || path.Any(c => c is '\\' or ':' || char.IsControl(c)))
        {
            return false;
        }

        foreach (var part in path.Split('/'))
        {
            // Windows drops the dots and spaces at a name's end, so such a part names another file than it says.
            var bare = part.TrimEnd('.', ' ');
            if (bare.Length != part.Length || part.Length == 0 || bare.Equals(".git", StringComparison.OrdinalIgnoreCase) || bare.Equals("git~1", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
