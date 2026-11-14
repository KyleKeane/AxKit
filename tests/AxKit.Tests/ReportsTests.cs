using AxKit.Core;
using AxKit.Core.Reports;

namespace AxKit.Tests;

/// <summary>A report as the form holds it, as an item of the Reports list, as plain text and as an issue's sealed body (docs/axKit/reports.md RP-3, RP-5, RP-6.1).</summary>
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
    public void The_plain_report_puts_the_words_first_and_the_reporter_line_last()
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

    [Theory]
    [InlineData(ReportKind.Bug, "Bug report, axKit 5.9.0")]
    [InlineData(ReportKind.Feature, "Feature request, axKit 5.9.0")]
    [InlineData(ReportKind.Feedback, "Feedback, axKit 5.9.0")]
    public void The_issue_title_is_the_kind_and_the_version_and_never_the_users_words(ReportKind kind, string expected)
    {
        Assert.Equal(expected, ReportBody.IssueTitle(kind, " 5.9.0 "));
    }

    [Fact]
    public void The_issue_body_is_the_envelope_alone_and_holds_nothing_of_the_report_in_the_clear()
    {
        var report = Sample();
        var body = ReportBody.Sealed(report, "5.9.0", Reporter, ReportSealTests.Keys.Public);

        // One JSON object between the two lines of a code fence, and nothing else.
        Assert.StartsWith("```json\n{\n", body);
        Assert.EndsWith("\n}\n```\n", body);
        using var json = System.Text.Json.JsonDocument.Parse(body["```json\n".Length..^"```\n".Length]);
        Assert.Equal(11, json.RootElement.EnumerateObject().Count());

        // What is read without a key: who sent it (a random id), with which version, of which kind.
        var envelope = ReportSeal.Read(body);
        Assert.NotNull(envelope);
        Assert.Equal(Reporter, envelope.Reporter);
        Assert.Equal("5.9.0", envelope.Version);
        Assert.Equal("bug", envelope.Kind);

        // Nothing the user wrote and nothing the window added: no title, no words, no context line, no action, no log line.
        var title = ReportBody.IssueTitle(report.Kind, "5.9.0");
        foreach (var text in new[] { title, body })
        {
            Assert.DoesNotContain("loses", text);
            Assert.DoesNotContain("my place", text);
            Assert.DoesNotContain("focus lands", text);
            Assert.DoesNotContain("axFile", text);
            Assert.DoesNotContain("Windows 11", text);
            Assert.DoesNotContain("NVDA 2025.3", text);
            Assert.DoesNotContain("item 7 of 40", text);
            Assert.DoesNotContain("Ctrl+F", text);
            Assert.DoesNotContain("3 matches", text);
            Assert.DoesNotContain("14:29:58", text);
            Assert.DoesNotContain("Context", text);
            Assert.DoesNotContain("Sent from axKit", text);
        }

        // The maintainer's key gives the whole report back: its title and kind, then the plain report as before.
        var plain = ReportSeal.Open(body, ReportSealTests.Keys.Private);
        Assert.Equal(ReportBody.Plain(report, "5.9.0", Reporter), plain);
        Assert.StartsWith("Title: The list loses my place\nKind: Bug\n\nThe list loses my place.\nAfter Ctrl+F", plain);
        Assert.EndsWith(ReportBody.Markdown(report, "5.9.0", Reporter), plain);
        Assert.Equal(("5.9.0", Reporter), ReportBody.ParseReporterLine(plain));
    }

    [Fact]
    public void An_agreement_to_the_public_issues_of_earlier_versions_does_not_count_for_the_sealed_reports()
    {
        var file = Path.Combine(Path.GetTempPath(), "axkit-tests", "consent-" + Guid.NewGuid().ToString("N") + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        try
        {
            // What 5.1.0 to 5.8.0 recorded: the app "axKit", the target "GitHub".
            Consent.Record(file, "axKit", ConsentTarget.GitHubReportsBefore, "5.8.0", new DateTime(2026, 11, 8));
            Assert.False(Consent.Given(file, "axKit", ConsentTarget.GitHubReports.Program));

            // Agreeing to the new words takes the old record out and counts from then on.
            Consent.Withdraw(file, "axKit", ConsentTarget.GitHubReportsBefore);
            Consent.Record(file, "axKit", ConsentTarget.GitHubReports.Program, "5.9.0", new DateTime(2026, 11, 14));
            Assert.True(Consent.Given(file, "axKit", ConsentTarget.GitHubReports.Program));
            Assert.Equal("axKit, GitHub, encrypted reports, 14 November 2026, axKit 5.9.0", Consent.Line(Assert.Single(Consent.Read(file))));
        }
        finally
        {
            File.Delete(file);
        }

        // The words the user agrees to: encrypted here, a private repository, the maintainer alone; never "public issue".
        var work = ConsentTarget.GitHubReports.Work;
        Assert.Contains("encrypted on your computer before it is sent", work);
        Assert.Contains("private repository on GitHub that only axKit's maintainer can open", work);
        Assert.Contains("only the maintainer's key can read it", work);
        Assert.Contains("sees when and from which network address", work);
        Assert.Contains("may quote or summarise", work);
        Assert.DoesNotContain("public issue", work);
    }

    [Fact]
    public void A_list_made_when_reports_were_public_gets_the_new_notes_and_keeps_what_the_user_set()
    {
        var folder = Path.Combine(Path.GetTempPath(), "axkit-tests", "reports-" + Guid.NewGuid().ToString("N"));
        try
        {
            ReportFiles.EnsureList(folder);
            var file = Path.Combine(folder, ItemDocument.ListFile);
            var made = File.ReadAllText(file);
            Assert.Contains("sent encrypted to a private repository", made);
            Assert.DoesNotContain("public", made);
            Assert.DoesNotContain("Open on GitHub", made);

            // An earlier version's list: its own Notes, and a key the user added.
            var list = ItemDocument.Parse(made);
            var old = list.WithValue(list.IndexOf("Notes"), System.Text.Json.Nodes.JsonValue.Create(ReportFiles.ListNotesBefore))
                .WithExtra("keys", new System.Text.Json.Nodes.JsonObject { ["7"] = new System.Text.Json.Nodes.JsonObject { ["name"] = "Mine", ["open"] = "Issue" } });
            File.WriteAllText(file, old.ToText());
            ReportFiles.EnsureList(folder);
            var after = File.ReadAllText(file);
            Assert.DoesNotContain("public issue", after);
            Assert.Contains("\"Mine\"", after);
            Assert.Equal(ReportFiles.ListNotes, FieldTypes.TextOf(ItemDocument.Parse(after).Fields[list.IndexOf("Notes")].Value));

            // Notes the user wrote themselves stay.
            File.WriteAllText(file, list.WithValue(list.IndexOf("Notes"), System.Text.Json.Nodes.JsonValue.Create("My own words")).ToText());
            ReportFiles.EnsureList(folder);
            Assert.Contains("My own words", File.ReadAllText(file));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
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
