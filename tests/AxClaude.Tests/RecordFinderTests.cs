using AxClaude.Core.Record;
using static AxClaude.Tests.RecordTailTests;

namespace AxClaude.Tests;

/// <summary>
/// The folder rule, <see cref="RecordFinder"/> (docs/axClaude/events-and-grounding.md EG-3.2 item 2 and EG-3.3): which
/// session file of the project folder is this window's, found by the prompt it sent, and where a tail of a file starts.
/// Files written by the test, in the shapes of tests/fixtures/records, texts invented.
/// </summary>
public sealed class RecordFinderTests
{
    /// <summary>A project folder's name as Claude Code makes it from a working folder (C:\Temp\axclaude-fixture).</summary>
    private const string Project = "C--Temp-axclaude-fixture";

    [Fact]
    public void The_file_that_gained_this_windows_prompt_is_found()
    {
        using var temp = new TempFolder("axclaude-finder");
        var project = ProjectFolder(temp);
        var mine = temp.File(Path.Combine("projects", Project, "s1.jsonl"), Lines(Prompt("a1", "An earlier question."), Reply("a2", "An earlier answer.")));
        var other = temp.File(Path.Combine("projects", Project, "s2.jsonl"), Lines(Prompt("b1", "Another session.")));
        var finder = new RecordFinder();
        finder.Note([project]);

        Append(other, Prompt("b2", "Something of its own."));
        Append(mine, Prompt("a3", "Name three colours of a rainbow."), Reply("a4", "Red, green and blue."));

        Assert.Equal(mine, finder.Find([Key("Name three colours of a rainbow.")], ClaudeCodeTranscript.Parse));

        // Read once, the prompt still counts at a later look, with keys given later.
        Assert.Equal(mine, finder.Find([Key("Something else."), Key("Name three colours of a rainbow.")], ClaudeCodeTranscript.Parse));
    }

    [Fact]
    public void A_file_that_grew_with_another_prompt_is_not_taken()
    {
        using var temp = new TempFolder("axclaude-finder");
        var project = ProjectFolder(temp);

        // The window's message stands in the file already, from before the note: an older session's, not this one's.
        var older = temp.File(Path.Combine("projects", Project, "s1.jsonl"), Lines(Prompt("a1", "Yes, go on.")));
        var finder = new RecordFinder();
        finder.Note([project]);

        Append(older, Prompt("a2", "A prompt another window sent."), Reply("a3", "Its reply."));
        Assert.Null(finder.Find([Key("Yes, go on.")], ClaudeCodeTranscript.Parse));
        Assert.Null(finder.Find([Key("Yes, go on.")], ClaudeCodeTranscript.Parse));
    }

    [Fact]
    public void A_new_file_is_found_as_well_as_one_that_grew()
    {
        using var temp = new TempFolder("axclaude-finder");
        var project = ProjectFolder(temp);
        var grown = temp.File(Path.Combine("projects", Project, "s1.jsonl"), Lines(Prompt("a1", "Old.")));

        // A project folder that did not exist at the note: the first session of a working folder.
        var finder = new RecordFinder();
        var fresh = Path.Combine(temp.Path, "projects", "C--Temp-another-folder");
        finder.Note([project, fresh]);

        var made = temp.File(Path.Combine("projects", "C--Temp-another-folder", "s3.jsonl"), Lines(Prompt("c1", "Start a new session.")));
        Assert.Equal(made, finder.Find([Key("Start a new session.")], ClaudeCodeTranscript.Parse));

        Append(grown, Prompt("a2", "Continue the old one."));
        Assert.Equal(grown, finder.Find([Key("Continue the old one.")], ClaudeCodeTranscript.Parse));
    }

    [Fact]
    public void Files_in_subfolders_are_never_looked_at()
    {
        using var temp = new TempFolder("axclaude-finder");
        var project = ProjectFolder(temp);
        var finder = new RecordFinder();
        finder.Note([project]);

        var line = Lines(Prompt("a1", "Find me."));
        temp.File(Path.Combine("projects", Project, "s1", "subagents", "agent-1.jsonl"), line);
        temp.File(Path.Combine("projects", Project, "s1", "tool-results", "r1.jsonl"), line);
        temp.File(Path.Combine("projects", Project, "s1.orphaned-20261116-ab.jsonl"), line);
        temp.File(Path.Combine("projects", Project, "s1.txt"), line);
        temp.File(Path.Combine("projects", "s0.jsonl"), line);

        Assert.Null(finder.Find([Key("Find me.")], ClaudeCodeTranscript.Parse));
    }

    [Fact]
    public void Nothing_sent_finds_nothing()
    {
        using var temp = new TempFolder("axclaude-finder");
        var project = ProjectFolder(temp);
        var finder = new RecordFinder();
        finder.Note([project]);
        temp.File(Path.Combine("projects", Project, "s1.jsonl"), Lines(Prompt("a1", "A prompt."), Prompt("a2", "?")));

        var lines = 0;
        RecordLineParser counting = (ReadOnlySpan<byte> line, long offset, out RecordLine what) =>
        {
            lines++;
            return ClaudeCodeTranscript.Parse(line, offset, out what);
        };

        // No key, or a key without letters, looks at no file at all.
        Assert.Null(finder.Find([], counting));
        Assert.Null(finder.Find([Key("?")], counting));
        Assert.Equal(0, lines);
    }

    [Fact]
    public void Each_look_reads_only_what_a_file_gained()
    {
        using var temp = new TempFolder("axclaude-finder");
        var project = ProjectFolder(temp);
        var path = temp.File(Path.Combine("projects", Project, "s1.jsonl"), Lines(Prompt("a1", "Before the note."), Reply("a2", "A reply.")));
        var finder = new RecordFinder();
        finder.Note([project]);

        var lines = 0;
        RecordLineParser counting = (ReadOnlySpan<byte> line, long offset, out RecordLine what) =>
        {
            lines++;
            return ClaudeCodeTranscript.Parse(line, offset, out what);
        };

        string[] keys = [Key("Not in any file.")];
        Assert.Null(finder.Find(keys, counting));
        Assert.Equal(0, lines);

        Append(path, Prompt("a3", "Another window's."));
        Assert.Null(finder.Find(keys, counting));
        Assert.Equal(1, lines);
        Assert.Null(finder.Find(keys, counting));
        Assert.Equal(1, lines);

        Append(path, Reply("a4", "Its reply."), TurnEnd("a5"));
        Assert.Null(finder.Find(keys, counting));
        Assert.Equal(3, lines);
    }

    [Fact]
    public void StartOf_is_the_noted_length_zero_for_a_new_file_and_from_the_end_elsewhere()
    {
        using var temp = new TempFolder("axclaude-finder");
        var project = ProjectFolder(temp);
        var noted = temp.File(Path.Combine("projects", Project, "s1.jsonl"), Lines(Prompt("a1", "Noted.")));
        var length = new FileInfo(noted).Length;
        var finder = new RecordFinder();
        finder.Note([project + Path.DirectorySeparatorChar]);

        Append(noted, Reply("a2", "Grown since."));
        Assert.Equal(length, finder.StartOf(noted));
        Assert.Equal(length, finder.StartOf(noted.ToUpperInvariant()));
        Assert.Equal(0, finder.StartOf(Path.Combine(project, "s2.jsonl")));
        Assert.Equal(RecordTail.FromEnd, finder.StartOf(Path.Combine(temp.Path, "projects", "C--Temp-elsewhere", "s3.jsonl")));
        Assert.Equal(RecordTail.FromEnd, finder.StartOf(Path.Combine(project, "s1", "subagents", "agent-1.jsonl")));

        // A new note forgets the old one.
        finder.Note([]);
        Assert.Equal(RecordTail.FromEnd, finder.StartOf(noted));
    }

    private static string ProjectFolder(TempFolder temp)
    {
        var folder = Path.Combine(temp.Path, "projects", Project);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static string Key(string message) => RecordText.Letters(message);
}
