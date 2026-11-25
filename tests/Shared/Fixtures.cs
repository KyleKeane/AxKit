namespace AxKit.TestShared;

/// <summary>The recorded fixtures under tests/fixtures. Compiled into every test project through tests/Directory.Build.props.</summary>
internal static class Fixtures
{
    /// <summary>The tests/fixtures folder, found above the test binary.</summary>
    public static string Folder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "fixtures");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("tests/fixtures was not found above " + AppContext.BaseDirectory);
    }

    /// <summary>The bytes of a recording in tests/fixtures.</summary>
    public static byte[] Bytes(string name) => File.ReadAllBytes(Path.Combine(Folder(), name));

    /// <summary>The console size a recording was made at, from its name (<c>-240x50.vt</c>); 120 by 40 when the name does not say.</summary>
    public static (int Columns, int Rows) Size(string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(name, @"-(\d+)x(\d+)\.vt$");
        return match.Success ? (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)) : (120, 40);
    }

    /// <summary>
    /// The chunk index of a recording (<c>&lt;fixture&gt;.chunks.txt</c>, as StreamRecorder and PtyCapture write it): the
    /// offset, length and elapsed milliseconds of each read; the header row, with or without a byte order mark, skipped.
    /// Written in AxClaude.Tests and AxTerminal.Tests alike until 2026-11-24 (docs/axKit/repairs.json R-96).
    /// </summary>
    public static List<(int Offset, int Length, int Ms)> Chunks(string fixture) =>
        File.ReadAllLines(Path.Combine(Folder(), fixture + ".chunks.txt"))
            .Where(l => l.Length > 0 && char.IsDigit(l[0]))
            .Select(l => l.Split(' '))
            .Select(p => (Offset: int.Parse(p[0]), Length: int.Parse(p[1]), Ms: int.Parse(p[2])))
            .ToList();

    /// <summary>
    /// Writes an expected file again in a test's update mode, and only when what it holds differs, line ends aside.
    /// A file written again with the same text has other line ends than git checked out, and git then lists it as
    /// changed: on 2026-11-17 the update for one new fixture listed 32 expected files so, each to be compared with
    /// its commit and put back by hand. Written as UTF-8 without a byte order mark, its lines ended as the text has
    /// them. True when the file was written.
    /// </summary>
    public static bool WriteExpected(string path, string text)
    {
        if (File.Exists(path) && File.ReadAllText(path).Replace("\r\n", "\n") == text.Replace("\r\n", "\n"))
        {
            return false;
        }

        File.WriteAllText(path, text, new System.Text.UTF8Encoding(false));
        return true;
    }
}
