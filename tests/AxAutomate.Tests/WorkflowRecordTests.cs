using AxAutomate.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The workflow record (WorkflowRecord, docs/axKit/user-workflows.md WF-6, format 5.5): a run entered for its tier,
/// the last pass kept through a failure, the file written in its one order and read back, the goals no script has any
/// more dropped, and an unreadable record read as an empty one.
/// </summary>
public sealed class WorkflowRecordTests
{
    private const string Label = "Find text and go to a line";
    private const string Script = "tests/scripts/axDown/04-find-and-go-to-line.txt";

    private static RecordedRun Ran(string result, string commit = "f56b1ee", string date = "2026-11-15") => new(result, "5.9.1", commit, date);

    [Fact]
    public void A_result_is_the_goals_last_for_its_tier_and_a_pass_is_its_last_pass_too()
    {
        var record = WorkflowRecord.With([], Label, "Find a word in the text", Script, WorkflowRecord.Desktop, Ran("passed"));
        var tier = Assert.Single(record).Tiers[WorkflowRecord.Desktop];
        Assert.Equal("passed", tier.Last.Result);
        Assert.Equal(tier.Last, tier.Passed);

        var later = WorkflowRecord.With(record, Label, "Find a word in the text", Script, WorkflowRecord.Desktop, Ran("passed", "a1b2c3d", "2026-11-16"));
        tier = Assert.Single(later).Tiers[WorkflowRecord.Desktop];
        Assert.Equal("a1b2c3d", tier.Last.Commit);
        Assert.Equal("2026-11-16", tier.Passed!.Date);
    }

    [Fact]
    public void A_failure_keeps_the_pass_of_before()
    {
        var record = WorkflowRecord.With([], Label, "Find a word in the text", Script, WorkflowRecord.Desktop, Ran("passed"));
        record = WorkflowRecord.With(record, Label, "Find a word in the text", Script, WorkflowRecord.Desktop, Ran("failed", "f56b1ee+", "2026-11-16"));
        var tier = Assert.Single(record).Tiers[WorkflowRecord.Desktop];
        Assert.Equal("failed", tier.Last.Result);
        Assert.Equal("f56b1ee+", tier.Last.Commit);
        Assert.Equal("passed", tier.Passed!.Result);
        Assert.Equal("f56b1ee", tier.Passed.Commit);
        Assert.Equal("2026-11-15", tier.Passed.Date);

        // A goal that never passed has no pass after a failure.
        var never = WorkflowRecord.With([], Label, "Go to a line by its number", Script, WorkflowRecord.Desktop, Ran("failed"));
        Assert.Null(Assert.Single(never).Tiers[WorkflowRecord.Desktop].Passed);
        Assert.Contains("\"passed\": null", WorkflowRecord.Write(never), StringComparison.Ordinal);
    }

    [Fact]
    public void Each_tier_is_kept_apart()
    {
        var record = WorkflowRecord.With([], Label, "Find a word in the text", Script, WorkflowRecord.Desktop, Ran("passed"));
        record = WorkflowRecord.With(record, Label, "Find a word in the text", Script, WorkflowRecord.Nvda, Ran("failed", "a1b2c3d"));
        var goal = Assert.Single(record);
        Assert.Equal(2, goal.Tiers.Count);
        Assert.Equal("passed", goal.Tiers[WorkflowRecord.Desktop].Last.Result);
        Assert.Equal("f56b1ee", goal.Tiers[WorkflowRecord.Desktop].Last.Commit);
        Assert.Equal("failed", goal.Tiers[WorkflowRecord.Nvda].Last.Result);
        Assert.Null(goal.Tiers[WorkflowRecord.Nvda].Passed);
        Assert.False(goal.Tiers.ContainsKey(WorkflowRecord.AiTools));

        // The line writes the tiers in one order, whatever order they ran in, and a tier never run has no member.
        var text = WorkflowRecord.Write(record);
        Assert.True(text.IndexOf("\"desktop\":", StringComparison.Ordinal) < text.IndexOf("\"nvda\":", StringComparison.Ordinal));
        Assert.DoesNotContain("ai-tools", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_goal_not_run_is_recorded_as_not_run()
    {
        var record = WorkflowRecord.With([], Label, "Find a word in the text", Script, WorkflowRecord.Desktop, Ran("passed"));
        record = WorkflowRecord.With(record, Label, "Find a word in the text", Script, WorkflowRecord.Desktop, Ran("not run", "a1b2c3d"));
        var tier = Assert.Single(record).Tiers[WorkflowRecord.Desktop];
        Assert.Equal("not run", tier.Last.Result);
        Assert.Equal("f56b1ee", tier.Passed!.Commit);
        var back = Assert.Single(WorkflowRecord.Read(WorkflowRecord.Write(record))).Tiers[WorkflowRecord.Desktop];
        Assert.Equal("not run", back.Last.Result);
    }

    [Fact]
    public void The_record_reads_back_what_it_wrote_in_the_one_order()
    {
        // Entered in an order of no rule: a later script first, a script with capitals, two goals of one script.
        var record = WorkflowRecord.With([], "Zoom", "Zoom", "tests/scripts/axMedia/01-zoom.txt", WorkflowRecord.Desktop, Ran("passed"));
        record = WorkflowRecord.With(record, Label, "Find a word in the text", Script, WorkflowRecord.Desktop, Ran("passed"));
        record = WorkflowRecord.With(record, "Help", "Help", "tests/scripts/AxClip/01-help.txt", WorkflowRecord.Nvda, Ran("failed"));
        record = WorkflowRecord.With(record, Label, "Go to a line by its number", Script, WorkflowRecord.Desktop, Ran("failed", "f56b1ee+"));

        var text = WorkflowRecord.Write(record);
        var expected =
            "{\n" +
            "  \"format\": \"axkit-workflow-record 1\",\n" +
            "  \"goals\": [\n" +
            "    {\"workflow\": \"Help\", \"goal\": \"Help\", \"script\": \"tests/scripts/AxClip/01-help.txt\", \"tiers\": {\"nvda\": {\"last\": {\"result\": \"failed\", \"version\": \"5.9.1\", \"commit\": \"f56b1ee\", \"date\": \"2026-11-15\"}, \"passed\": null}}},\n" +
            "    {\"workflow\": \"Find text and go to a line\", \"goal\": \"Find a word in the text\", \"script\": \"tests/scripts/axDown/04-find-and-go-to-line.txt\", \"tiers\": {\"desktop\": {\"last\": {\"result\": \"passed\", \"version\": \"5.9.1\", \"commit\": \"f56b1ee\", \"date\": \"2026-11-15\"}, \"passed\": {\"version\": \"5.9.1\", \"commit\": \"f56b1ee\", \"date\": \"2026-11-15\"}}}},\n" +
            "    {\"workflow\": \"Find text and go to a line\", \"goal\": \"Go to a line by its number\", \"script\": \"tests/scripts/axDown/04-find-and-go-to-line.txt\", \"tiers\": {\"desktop\": {\"last\": {\"result\": \"failed\", \"version\": \"5.9.1\", \"commit\": \"f56b1ee+\", \"date\": \"2026-11-15\"}, \"passed\": null}}},\n" +
            "    {\"workflow\": \"Zoom\", \"goal\": \"Zoom\", \"script\": \"tests/scripts/axMedia/01-zoom.txt\", \"tiers\": {\"desktop\": {\"last\": {\"result\": \"passed\", \"version\": \"5.9.1\", \"commit\": \"f56b1ee\", \"date\": \"2026-11-15\"}, \"passed\": {\"version\": \"5.9.1\", \"commit\": \"f56b1ee\", \"date\": \"2026-11-15\"}}}}\n" +
            "  ]\n" +
            "}\n";
        Assert.Equal(expected, text);

        var back = WorkflowRecord.Read(text);
        Assert.Equal(4, back.Count);
        Assert.Equal(text, WorkflowRecord.Write(back));
        Assert.Equal("passed", back[1].Tiers[WorkflowRecord.Desktop].Passed!.Result);

        // A label with quotes and an accented letter, and a title with a plus, are written plainly and read back as they were.
        var odd = WorkflowRecord.With([], "Café \"quoted\"", "A + B", Script, WorkflowRecord.AiTools, Ran("passed", "f56b1ee+"));
        var oddText = WorkflowRecord.Write(odd);
        Assert.Contains("\"workflow\": \"Café \\\"quoted\\\"\", \"goal\": \"A + B\"", oddText, StringComparison.Ordinal);
        Assert.Contains("\"commit\": \"f56b1ee+\"", oddText, StringComparison.Ordinal);
        Assert.Equal("Café \"quoted\"", Assert.Single(WorkflowRecord.Read(oddText)).Workflow);
        Assert.Equal(oddText, WorkflowRecord.Write(WorkflowRecord.Read(oddText)));

        // An empty record is written whole too.
        Assert.Equal("{\n  \"format\": \"axkit-workflow-record 1\",\n  \"goals\": []\n}\n", WorkflowRecord.Write([]));
        Assert.Empty(WorkflowRecord.Read(WorkflowRecord.Write([])));
    }

    [Fact]
    public void Goals_no_script_has_any_more_are_dropped()
    {
        var record = WorkflowRecord.With([], Label, "Find a word in the text", Script, WorkflowRecord.Desktop, Ran("passed"));
        record = WorkflowRecord.With(record, Label, "Go to a line by its number", Script, WorkflowRecord.Desktop, Ran("passed"));
        record = WorkflowRecord.With(record, "Gone", "Gone", "tests/scripts/axDown/09-gone.txt", WorkflowRecord.Desktop, Ran("passed"));

        // The second goal was renamed, the script "Gone" deleted, and the first script moved; a new goal has no entry yet.
        const string moved = "tests/scripts/axDown/05-find-and-go-to-line.txt";
        var pruned = WorkflowRecord.Pruned(record,
        [
            (Label, "Find a word in the text", moved),
            (Label, "Go to a line by its number, renamed", moved),
            ("New", "New", "tests/scripts/axDown/10-new.txt"),
        ]);
        var kept = Assert.Single(pruned);
        Assert.Equal("Find a word in the text", kept.Goal);
        Assert.Equal(moved, kept.Script);
        Assert.Equal("passed", kept.Tiers[WorkflowRecord.Desktop].Last.Result);

        // A goal is known by its label and its title, not its path: an entry of another label with the same title goes.
        Assert.Empty(WorkflowRecord.Pruned(record, [("Another label", "Find a word in the text", Script)]));
    }

    [Fact]
    public void An_unreadable_record_is_an_empty_one()
    {
        Assert.Empty(WorkflowRecord.Read(""));
        Assert.Empty(WorkflowRecord.Read("not json"));
        Assert.Empty(WorkflowRecord.Read("[]"));
        Assert.Empty(WorkflowRecord.Read("{\"format\": \"axkit-workflow-record 1\", \"goals\": ["));
        Assert.Empty(WorkflowRecord.Read("{\"format\": \"axkit-workflow-record 2\", \"goals\": []}"));
        Assert.Empty(WorkflowRecord.Read("{\"format\": \"axkit-workflow-record 1\"}"));
        // A conflict's marks left in the file make it unreadable as a whole.
        Assert.Empty(WorkflowRecord.Read("<<<<<<< HEAD\n{\"format\": \"axkit-workflow-record 1\", \"goals\": []}\n>>>>>>> main\n"));

        // An entry that is not whole is left out, and the rest still read.
        var text = "{\"format\": \"axkit-workflow-record 1\", \"goals\": [" +
            "{\"workflow\": \"A\", \"goal\": \"A\", \"tiers\": {}}," +
            "{\"workflow\": \"B\", \"goal\": \"B\", \"script\": \"tests/scripts/b.txt\", \"tiers\": {\"desktop\": {\"last\": {\"result\": \"lost\", \"version\": \"1\", \"commit\": \"c\", \"date\": \"d\"}, \"passed\": null}}}," +
            "{\"workflow\": \"C\", \"goal\": \"C\", \"script\": \"tests/scripts/c.txt\", \"tiers\": {\"desktop\": {\"last\": {\"result\": \"passed\", \"version\": \"1\", \"commit\": \"c\", \"date\": \"d\"}, \"passed\": null}}}]}";
        var read = WorkflowRecord.Read(text);
        Assert.Equal(["B", "C"], read.Select(goal => goal.Workflow));
        Assert.Empty(read[0].Tiers);
        Assert.Single(read[1].Tiers);
    }
}
