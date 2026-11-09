using AxKit.Core;
using AxKit.Core.Reports;

namespace AxKit.Tests;

/// <summary>A report as the form holds it, as an item of the Reports list and as an issue's body (docs/axkit/reports.md RP-3, RP-5, RP-6.1).</summary>
public sealed class ReportsTests
{
    private static readonly DateTime Created = new(2026, 11, 7, 14, 30, 0, DateTimeKind.Utc);
    private static readonly Guid Reporter = Guid.Parse("6f1c2d3e-4a5b-4c6d-8e7f-901234567890");

    private static Report Sample() => new(
        "The list loses my place",
        ReportKind.Bug,
        "The list loses my place.\r\nAfter Ctrl+F the focus lands on the first item.",
        "axFile 5.1.0, Windows 11 build 26200, NVDA 2025.3",
        "Files, item 7 of 40",
        ["12 s ago: Find, Ctrl+F", "11 s ago, said: 3 matches"],
        ["14:29:58 Find: 3 matches for notes"],
        Created);

    [Theory]
    [InlineData("Short idea", "Short idea")]
    [InlineData("\r\n\r\n  First line with text  \r\nsecond", "First line with text")]
    [InlineData("", "Report")]
    [InlineData("   ", "Report")]
    public void The_title_is_the_first_line_with_text(string words, string expected)
    {
        Assert.Equal(expected, Report.TitleFrom(words));
    }

    [Fact]
    public void A_long_first_line_is_cut_at_a_word_within_eighty_characters()
    {
        var words = string.Join(' ', Enumerable.Repeat("word", 30));
        var title = Report.TitleFrom(words);
        Assert.True(title.Length <= Report.TitleLength + 1, title);
        Assert.EndsWith("…", title);
        Assert.DoesNotContain("  ", title);
    }

    [Theory]
    [InlineData(ReportKind.Feedback, "Feedback", "feedback")]
    [InlineData(ReportKind.Bug, "Bug", "bug")]
    [InlineData(ReportKind.Feature, "Feature request", "feature")]
    public void Kinds_have_a_name_and_a_label_and_are_read_back_from_either(ReportKind kind, string name, string label)
    {
        Assert.Equal(name, ReportKinds.Name(kind));
        Assert.Equal(label, ReportKinds.Label(kind));
        Assert.Equal(kind, ReportKinds.Parse(name));
        Assert.Equal(kind, ReportKinds.Parse(label.ToUpperInvariant()));
        Assert.Null(ReportKinds.Parse("question"));
    }

    [Fact]
    public void Statuses_say_whether_the_sender_should_try()
    {
        Assert.Equal("Not sent yet: no network", ReportStatus.NotSent("no network"));
        Assert.Equal("Not sent yet", ReportStatus.NotSent(null));
        Assert.True(ReportStatus.AwaitsSending(ReportStatus.NotSent("blocked")));
        Assert.False(ReportStatus.AwaitsSending(ReportStatus.Draft));
        Assert.False(ReportStatus.AwaitsSending(ReportStatus.Sent));
    }

    [Fact]
    public void A_report_goes_into_an_item_and_comes_back()
    {
        var item = ReportFiles.ToItem(Sample(), ReportStatus.NotSent("no network"));
        Assert.Equal("The list loses my place", item.Title);
        Assert.Equal(ReportFiles.Template.Count + 1, item.Fields.Count);
        Assert.Equal("Not sent yet: no network", ReportFiles.Status(item));

        // A record with lists compares them by reference, so field by field.
        var back = ReportFiles.FromItem(ItemDocument.Parse(item.ToText()));
        Assert.NotNull(back);
        var sample = Sample();
        Assert.Equal(sample.Title, back.Title);
        Assert.Equal(sample.Kind, back.Kind);
        Assert.Equal(sample.Words, back.Words);
        Assert.Equal(sample.App, back.App);
        Assert.Equal(sample.Where, back.Where);
        Assert.Equal(sample.Actions, back.Actions);
        Assert.Equal(sample.Log, back.Log);
        Assert.Equal(Created.ToString("yyyy-MM-dd HH:mm"), back.Created.ToString("yyyy-MM-dd HH:mm"));
    }

    [Fact]
    public void An_item_that_is_not_a_report_reads_as_none()
    {
        var task = ItemDocument.NewItem("Fix the tap", [new ItemField("Done", FieldTypes.Check, System.Text.Json.Nodes.JsonValue.Create(false), [])]);
        Assert.Null(ReportFiles.FromItem(task));
    }

    [Fact]
    public void Sent_fills_the_issue_fields()
    {
        var item = ReportFiles.WithSent(ReportFiles.ToItem(Sample(), ReportStatus.NotSent(null)), 12, "https://github.com/KyleKeane/AxKit-reports/issues/12");
        Assert.Equal(ReportStatus.Sent, ReportFiles.Status(item));
        var text = item.ToText();
        Assert.Contains("\"https://github.com/KyleKeane/AxKit-reports/issues/12\"", text);
        Assert.Contains("\"title\": \"Number\"", text);
        Assert.Contains("\"12\"", text);
    }

    [Fact]
    public void Save_makes_the_folder_with_its_list_and_names_the_item_after_the_title()
    {
        var folder = Path.Combine(Path.GetTempPath(), "axkit-tests", "reports-" + Guid.NewGuid().ToString("N"));
        try
        {
            var first = ReportFiles.Save(Sample(), ReportStatus.NotSent("no network"), folder);
            var second = ReportFiles.Save(Sample() with { Words = "again" }, ReportStatus.Sent, folder);
            Assert.Equal("The list loses my place.json", Path.GetFileName(first));
            Assert.Equal("The list loses my place - 2.json", Path.GetFileName(second));

            var list = ItemDocument.Parse(File.ReadAllText(Path.Combine(folder, ItemDocument.ListFile)));
            Assert.True(list.HasTemplate);
            Assert.Equal(ReportFiles.Template.Select(f => f.Title), list.Template.Select(f => f.Title));
            Assert.Equal(ReportKinds.Name(ReportKind.Feedback), FieldTypes.TextOf(list.Template.First(f => f.Title == ReportFiles.KindField).Value));
            Assert.Equal(ReportStatus.Draft, FieldTypes.TextOf(list.Template.First(f => f.Title == ReportFiles.StatusField).Value));
            Assert.Contains("\"keys\"", File.ReadAllText(Path.Combine(folder, ItemDocument.ListFile)));

            // The unsent ones, and only those, oldest first.
            var unsent = ReportFiles.Unsent(folder);
            Assert.Equal([first], unsent.Select(entry => entry.Path));
            Assert.Equal("The list loses my place", unsent[0].Item.Title);
            Assert.Empty(ReportFiles.Unsent(Path.Combine(folder, "missing")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void The_body_puts_the_words_first_and_the_reporter_line_last()
    {
        var body = ReportBody.Markdown(Sample(), "5.1.0", Reporter);
        Assert.StartsWith("The list loses my place.\nAfter Ctrl+F the focus lands on the first item.\n\n### Context\n", body);
        Assert.Contains("- App and version: axFile 5.1.0, Windows 11 build 26200, NVDA 2025.3\n", body);
        Assert.Contains("- Where the focus was: Files, item 7 of 40\n", body);
        Assert.Contains("### Last actions\n\n```text\n12 s ago: Find, Ctrl+F\n11 s ago, said: 3 matches\n```\n", body);
        Assert.Contains("<details>\n<summary>Log lines</summary>\n\n```text\n14:29:58 Find: 3 matches for notes\n```\n\n</details>\n", body);
        Assert.EndsWith("\nSent from axKit 5.1.0 with F4; reporter 6f1c2d3e-4a5b-4c6d-8e7f-901234567890\n", body);
        Assert.Equal(("5.1.0", Reporter), ReportBody.ParseReporterLine(body));
    }

    [Fact]
    public void A_body_without_actions_or_log_has_no_empty_sections_and_a_fence_inside_a_line_is_defused()
    {
        var report = Sample() with { Actions = ["said: ``` three backticks"], Log = [], Where = " " };
        var body = ReportBody.Markdown(report, "5.1.0", Reporter);
        Assert.DoesNotContain("Log lines", body);
        Assert.DoesNotContain("Where the focus was", body);
        Assert.Contains("said: ''' three backticks", body);
        Assert.Null(ReportBody.ParseReporterLine("A report typed in the browser.\n\nNo last line of ours."));
    }
}
