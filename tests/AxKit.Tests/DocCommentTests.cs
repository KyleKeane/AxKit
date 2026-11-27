namespace AxKit.Tests;

/// <summary>
/// One doc comment for one member (2026-11-27, after a review found fourteen places in src where a member had been
/// put in under another member's summary: the summary then described the wrong member, and its own stood below
/// with none). A doc comment block is a run of lines that start with three slashes. This test reads every C# file
/// under src and fails on a block that opens more than one summary, and on a block that follows another with only
/// blank lines between them; both are two comments on one member. The lines are read as text, as the other source
/// tests read them: lint, which a raw string holding such lines would mislead.
/// </summary>
public sealed class DocCommentTests
{
    private const string SummaryTag = "<summary>";

    /// <summary>
    /// What is wrong with the doc comments of one file's text, each as "file:line: what": the line of the block that
    /// opens several summaries, or of the block that follows another.
    /// </summary>
    public static List<string> Problems(string file, string text)
    {
        var problems = new List<string>();
        var lines = text.Split('\n');
        // The first line of the block being read, or -1 between blocks.
        var start = -1;
        var summaries = 0;
        // A block has ended and only blank lines have come since.
        var afterBlock = false;
        for (var i = 0; i <= lines.Length; i++)
        {
            // One line past the end closes a block the file ends in.
            var line = i < lines.Length ? lines[i].Trim() : "end of file";
            if (line.StartsWith("///", StringComparison.Ordinal))
            {
                if (start < 0)
                {
                    if (afterBlock)
                    {
                        problems.Add($"{file}:{i + 1}: a doc comment follows another with only blank lines between them: two comments on one member");
                    }

                    start = i;
                    summaries = 0;
                    afterBlock = false;
                }

                summaries += Occurrences(line, SummaryTag);
                continue;
            }

            if (start >= 0)
            {
                if (summaries > 1)
                {
                    problems.Add($"{file}:{start + 1}: {summaries} summaries in one doc comment: each member has its own");
                }

                start = -1;
                afterBlock = true;
            }

            if (line.Length > 0)
            {
                afterBlock = false;
            }
        }

        return problems;
    }

    private static int Occurrences(string line, string word)
    {
        var count = 0;
        for (var at = line.IndexOf(word, StringComparison.Ordinal); at >= 0; at = line.IndexOf(word, at + word.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    [Fact]
    public void Every_member_of_the_sources_has_one_doc_comment_of_its_own()
    {
        var root = SourceTexts.RepositoryRoot();
        var problems = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            problems.AddRange(Problems(relative, File.ReadAllText(file)));
        }

        Assert.True(problems.Count == 0, "Move each summary to the member it describes:\n" + string.Join("\n", problems));
    }

    [Fact]
    public void Two_summaries_in_one_block_and_two_blocks_on_one_member_are_found()
    {
        const string twoSummaries = "class A\r\n{\r\n    /// <summary>The first.</summary>\r\n    /// <summary>\r\n    /// The second.\r\n    /// </summary>\r\n    void M() { }\r\n}\r\n";
        Assert.Equal("a.cs:3: 2 summaries in one doc comment: each member has its own", string.Join("\n", Problems("a.cs", twoSummaries)));

        const string twoBlocks = "    /// <summary>The first.</summary>\n\n\n    /// <summary>The second.</summary>\n    void M() { }\n";
        Assert.Equal("a.cs:4: a doc comment follows another with only blank lines between them: two comments on one member", string.Join("\n", Problems("a.cs", twoBlocks)));

        // A block the file ends in is read to its end.
        Assert.Equal("a.cs:1: 2 summaries in one doc comment: each member has its own", string.Join("\n", Problems("a.cs", "/// <summary>One.</summary> <summary>Two.</summary>")));
    }

    [Fact]
    public void A_comment_of_its_own_on_each_member_is_left_alone()
    {
        const string fine = "    /// <summary>\n    /// The first.\n    /// </summary>\n    /// <param name=\"a\">A number.</param>\n    void M(int a) { }\n\n"
            + "    /// <summary>The second.</summary>\n    void N() { }\n    // A note between two members.\n\n    /// <summary>The third.</summary>\n    int _x;\n";
        Assert.Equal(string.Empty, string.Join("\n", Problems("a.cs", fine)));
    }
}
