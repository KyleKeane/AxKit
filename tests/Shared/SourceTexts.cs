namespace AxKit.TestShared;

/// <summary>
/// The string literals of source files, for the tests that keep an outside program's words in one place (root
/// CLAUDE.md rule 12; docs/axKit/access-layers.md): <c>GitTextsTests</c>, <c>KeePassCliTextsTests</c> and
/// <c>ShellTextsTests</c>. Comments are not looked at, since they quote a program's words to explain the code: a line
/// that starts with two slashes is skipped, and on any other line only what stands between double quotes counts, up
/// to a comment that starts outside a string. A block comment or a raw string over several lines is not understood:
/// each of its lines is read as a line of code. The reading is the one <c>ClaudeCodeTextsTests</c> has had since
/// R-28 and tests itself; that class keeps its copy. Compiled into every test project through
/// tests/Directory.Build.props.
/// </summary>
internal static class SourceTexts
{
    /// <summary>One string literal: the file from the repository's root, the line's number, the text between the quotes with its escapes as written.</summary>
    public sealed record Literal(string File, int Line, string Text)
    {
        public override string ToString() => $"{File}:{Line}: \"{Text}\"";
    }

    /// <summary>The folder that holds AxKit.sln, found above the test binary.</summary>
    public static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AxKit.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("AxKit.sln not found above the test directory.");
    }

    /// <summary>Every string literal of the C# files under a folder of the repository ("src/AxGit"), bin and obj left out.</summary>
    public static List<Literal> LiteralsUnder(string folder)
    {
        var root = RepositoryRoot();
        var found = new List<Literal>();
        var separator = Path.DirectorySeparatorChar;
        foreach (var file in Directory.GetFiles(Path.Combine(root, folder.Replace('/', separator)), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace(separator, '/');
            if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                foreach (var text in StringLiterals(line))
                {
                    found.Add(new Literal(relative, number, text));
                }
            }
        }

        return found;
    }

    /// <summary>
    /// What stands between double quotes on one line of C#, escapes left as written: plain strings with backslash
    /// escapes, verbatim strings with doubled quotes, a quote as a character passed over; nothing of a line that is
    /// a comment, and nothing after a comment that starts outside a string.
    /// </summary>
    public static List<string> StringLiterals(string line)
    {
        var found = new List<string>();
        if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
        {
            return found;
        }

        var i = 0;
        while (i < line.Length)
        {
            var c = line[i];
            if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                break;
            }

            if (c == '\'')
            {
                // A character: past what it holds, then to its closing quote.
                i += i + 1 < line.Length && line[i + 1] == '\\' ? 3 : 2;
                while (i < line.Length && line[i] != '\'')
                {
                    i++;
                }

                i++;
                continue;
            }

            if (c != '"')
            {
                i++;
                continue;
            }

            var verbatim = i > 0 && (line[i - 1] == '@' || (line[i - 1] == '$' && i > 1 && line[i - 2] == '@'));
            var start = ++i;
            while (i < line.Length)
            {
                if (verbatim && line[i] == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                if (!verbatim && line[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (line[i] == '"')
                {
                    break;
                }

                i++;
            }

            found.Add(line[start..Math.Min(i, line.Length)]);
            i++;
        }

        return found;
    }
}
