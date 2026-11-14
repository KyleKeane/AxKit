using System.Text.RegularExpressions;
using AxPass.Core;

namespace AxPass.Tests;

/// <summary>
/// The words of KeePassXC's command-line tool are written in one place, <see cref="KeePassCliTexts"/> (root CLAUDE.md
/// rule 12; docs/axKit/access-layers.md): a command's name, an option or a mark of the tool's as a string of any
/// other file of axPass's two source folders fails here. Read off the sources with <see cref="SourceTexts"/>. The
/// words themselves are pinned too, so that a move changes nothing the tool is told.
/// </summary>
public sealed partial class KeePassCliTextsTests
{
    private const string Table = "src/AxPass.Core/KeePassCliTexts.cs";

    /// <summary>The tool's commands and marks, which no other file may hold as a whole string.</summary>
    private static readonly string[] Words =
    [
        "ls", "show", "edit", "add", "rm", "rmdir", "mkdir", "mv", "generate", "db-create",
        "[empty]", "Recycle Bin", "Uuid", "UserName",
    ];

    /// <summary>An option as the tool takes one: one or two dashes, then a letter.</summary>
    [GeneratedRegex(@"^--?[A-Za-z]")]
    private static partial Regex Option();

    [Fact]
    public void The_tools_commands_and_options_are_written_in_the_table_alone()
    {
        var offenders = SourceTexts.LiteralsUnder("src/AxPass.Core").Concat(SourceTexts.LiteralsUnder("src/AxPass"))
            .Where(literal => literal.File != Table)
            .Where(literal => Option().IsMatch(literal.Text) || Words.Contains(literal.Text, StringComparer.Ordinal))
            .Select(literal => literal.ToString())
            .ToList();
        Assert.True(offenders.Count == 0, "A word of KeePassXC's tool outside KeePassCliTexts, where it belongs (rule 12): " + string.Join("; ", offenders));
    }

    /// <summary>The list above is real: the table holds every word watched, as a string of its own.</summary>
    [Fact]
    public void Every_watched_word_is_in_the_table()
    {
        var table = SourceTexts.LiteralsUnder("src/AxPass.Core").Where(literal => literal.File == Table).Select(literal => literal.Text).ToHashSet(StringComparer.Ordinal);
        Assert.All(Words, word => Assert.Contains(word, table));
    }

    /// <summary>No prompt and no error text of the tool is matched: nothing in either folder searches what the tool printed.</summary>
    [Fact]
    public void Nothing_matches_the_tools_prompts_or_errors()
    {
        var root = SourceTexts.RepositoryRoot();
        foreach (var folder in new[] { "AxPass", "AxPass.Core" })
        {
            foreach (var file in Directory.GetFiles(Path.Combine(root, "src", folder), "*.cs"))
            {
                var source = File.ReadAllText(file);
                Assert.DoesNotContain("Error.Contains(", source);
                Assert.DoesNotContain("Error.StartsWith(", source);
                Assert.DoesNotContain("Why.Contains(", source);
                Assert.DoesNotContain("Invalid credentials", source);
                Assert.DoesNotContain("Enter password", source);
            }
        }
    }

    [Fact]
    public void The_words_are_what_the_tool_takes()
    {
        Assert.Equal(["ls", "-q", "-R", "-f", @"C:\v\mine.kdbx"], KeePassCli.On(KeePassCliTexts.List, @"C:\v\mine.kdbx", KeePassCliTexts.ListOptions));
        Assert.Equal(["show", "-q", "-s", "-a", "Password", "mine.kdbx", "Email/Gmail"], KeePassCli.On(KeePassCliTexts.Show, "mine.kdbx", KeePassCliTexts.ShowValueOptions(EntryFields.Password), "Email/Gmail"));
        Assert.Equal(["edit", "-q", "-p", "mine.kdbx", "Email/Gmail"], KeePassCli.On(KeePassCliTexts.Edit, "mine.kdbx", [KeePassCliTexts.PasswordPromptOption], "Email/Gmail"));
        Assert.Equal(["db-create", "-q", "-p", "new.kdbx"], KeePassCli.On(KeePassCliTexts.CreateDatabase, "new.kdbx", [KeePassCliTexts.PasswordPromptOption]));
        Assert.Equal(["mv", "-q", "mine.kdbx", "Recycle Bin/Gmail", "/"], KeePassCli.On(KeePassCliTexts.Move, "mine.kdbx", null, PassTree.Join(PassTree.RecycleBin, "Gmail"), KeePassCliTexts.Root));
        Assert.Equal(["generate", "-L", "20", "-l", "-U", "-n", "-s"], KeePassCliTexts.GenerateArguments);
        Assert.Equal(["add", "rm", "rmdir", "mkdir"], new[] { KeePassCliTexts.Add, KeePassCliTexts.Remove, KeePassCliTexts.RemoveGroup, KeePassCliTexts.MakeGroup });
        Assert.Equal(["Title", "UserName", "Password", "URL", "Notes"], new[] { EntryFields.Title, EntryFields.UserName, EntryFields.Password, EntryFields.Url, EntryFields.Notes });
    }

    [Fact]
    public void A_failure_without_words_is_told_from_one_the_tool_explains()
    {
        Assert.True(new CliResult(1, string.Empty, " \r\n").SaidNothing);
        Assert.False(new CliResult(1, string.Empty, "Could not find entry with path Email/None.\n").SaidNothing);
    }
}
