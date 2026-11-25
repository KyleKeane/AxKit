using AxKit.Core;
using AxKit.Core.Reports;

namespace AxKit.Tests;

/// <summary>
/// What a report's plain form carries (docs/axKit/reports.md RP-4.2a): a report goes to someone else, sealed for the maintainer since 2026-11-14 and public before, so nothing in it may name
/// the user's files, folders, lists or items unless the user chose the fuller form.
/// </summary>
public class ReportPrivacyTests
{
    private static readonly string[] NoNames = [];

    [Theory]
    [InlineData(@"Could not find file 'C:\work\journals\march 18.txt'.", "Could not find file <text>.")]
    [InlineData(@"Access to the path ""D:\My Notes\plan.md"" is denied.", "Access to the path <text> is denied.")]
    [InlineData(@"Opened C:\work\journals\notes.md, 3 lines", "Opened <path>, 3 lines")]
    [InlineData(@"Reading \\server\share\team\budget.xlsx failed", "Reading <path> failed")]
    [InlineData("cloning into /home/sam/secret-project", "cloning into <path>")]
    [InlineData(@"saved under ~\Documents\diary", "saved under <path>")]
    public void A_path_and_a_quoted_text_are_masked_whole(string line, string masked)
    {
        Assert.Equal(masked, ReportPrivacy.Mask(line, NoNames));
    }

    [Theory]
    [InlineData("at System.Net.Http.HttpConnectionPool.ConnectAsync(HttpRequestMessage request, Boolean async, CancellationToken cancellationToken)")]
    [InlineData("System.IO.IOException: The process cannot access the file because it is being used by another process.")]
    [InlineData("It doesn't answer and the user's choice stands")]
    [InlineData("3 matches, 2/3 done, and/or nothing")]
    public void Code_and_plain_words_stay_as_they_are(string line)
    {
        Assert.Equal(line, ReportPrivacy.Mask(line, NoNames));
    }

    [Fact]
    public void Masking_also_does_what_redaction_does()
    {
        // The address is put together here: PrivacyTests fails on a whole one anywhere in the repository.
        var address = "someone" + "@" + "example.org";
        Assert.Equal("sent to <email> by <name>", ReportPrivacy.Mask($"sent to {address} by Sam", ["Sam"]));
    }

    [Fact]
    public void The_plain_app_line_has_no_window_title()
    {
        var plain = ReportPrivacy.AppLine("axFile", "5.8.0", "Windows 11 build 26200", "NVDA 2025.3");
        var full = ReportPrivacy.AppLine("axFile", "5.8.0", "Windows 11 build 26200", "NVDA 2025.3", "journals - axFile");

        Assert.Equal("axFile 5.8.0, Windows 11 build 26200, NVDA 2025.3", plain);
        Assert.DoesNotContain("journals", plain);
        Assert.Equal(plain + "; window: journals - axFile", full);
    }

    [Fact]
    public void The_plain_actions_are_the_commands_alone()
    {
        var now = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);
        var actions = new RecentActions();
        actions.Add(RecentActionKind.Command, "Refresh, F5", now.AddSeconds(-30));
        actions.Add(RecentActionKind.Said, "journals, 4 items, march 18.txt", now.AddSeconds(-30));
        actions.Add(RecentActionKind.Notice, "Delete march 18.txt?", now.AddSeconds(-20));
        actions.Add(RecentActionKind.Command, "Delete, Delete", now.AddSeconds(-20));

        var lines = ReportPrivacy.Commands(actions.Entries(), now);

        Assert.Equal(["30 s ago: Refresh, F5", "20 s ago: Delete, Delete"], lines);
    }

    [Fact]
    public void The_plain_log_is_todays_errors_with_their_stacks_masked()
    {
        var now = new DateTime(2026, 11, 14, 12, 0, 0);
        string[] log =
        [
            "2026-10-29 09:48:40.000 ERROR [8352] The update check failed",
            "   at Old.Stack.Frame()",
            "2026-10-29 09:48:48.430 INFO  [8352] axFile exiting",
            @"2026-11-14 10:59:21.681 INFO  [18156] axFile 5.8.0 starting from C:\Program Files\axKit\AxKit.exe as process 18156, arguments: D:\journals",
            @"2026-11-14 11:02:00.000 ERROR [18156] The folder could not be read: Could not find a part of the path 'D:\journals\private'.",
            "System.IO.DirectoryNotFoundException: Could not find a part of the path 'D:\\journals\\private'.",
            "   at System.IO.Enumeration.FileSystemEnumerator`1.CreateDirectoryHandle(String path, Boolean ignoreNotFound)",
            "2026-11-14 11:03:00.000 INFO  [18156] axFile exiting",
        ];

        var lines = ReportPrivacy.Errors(log, now, NoNames);

        Assert.Equal(
        [
            "2026-11-14 11:02:00.000 ERROR [18156] The folder could not be read: Could not find a part of the path <text>.",
            "System.IO.DirectoryNotFoundException: Could not find a part of the path <text>.",
            "at System.IO.Enumeration.FileSystemEnumerator`1.CreateDirectoryHandle(String path, Boolean ignoreNotFound)",
        ], lines);
        Assert.DoesNotContain(lines, line => line.Contains("journals", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_plain_log_keeps_the_last_twenty_lines()
    {
        var now = new DateTime(2026, 11, 14, 12, 0, 0);
        var log = Enumerable.Range(0, 30).Select(i => $"2026-11-14 11:{i:00}:00.000 ERROR [1] failure {i}").ToList();

        var lines = ReportPrivacy.Errors(log, now, NoNames);

        Assert.Equal(ReportPrivacy.LogLines, lines.Count);
        Assert.EndsWith("failure 29", lines[^1]);
    }

    [Fact]
    public void The_report_form_starts_plain_and_the_fuller_form_is_a_box_the_user_checks()
    {
        var root = SourceTexts.RepositoryRoot();
        var flow = File.ReadAllText(Path.Combine(root, "src", "AxKit.Forms", "ReportFlow.cs"));
        var controls = File.ReadAllText(Path.Combine(root, "src", "AxKit.Forms", "ReportControls.cs"));

        // The plain form: no title, no names in the focus line, the commands alone, the errors alone.
        Assert.Contains("_form.FocusDescription(named: false)", flow, StringComparison.Ordinal);
        Assert.Contains("ReportPrivacy.Commands(", flow, StringComparison.Ordinal);
        Assert.Contains("ReportPrivacy.Errors(", flow, StringComparison.Ordinal);
        // What is sent starts as the plain form, shown and held, and becomes the fuller one only by the box.
        Assert.Contains("_context = plain;", controls, StringComparison.Ordinal);
        Assert.Contains("Fill(plain);", controls, StringComparison.Ordinal);
        Assert.Contains("Fill(_detail.Checked ? full : plain)", controls, StringComparison.Ordinal);
        Assert.DoesNotContain("_detail.Checked = true", controls, StringComparison.Ordinal);
        Assert.Contains("may name your files", ReportPrivacy.DetailBox, StringComparison.Ordinal);
        // The box that shows it is only read, and the report is built from the context it shows, never from its text:
        // nothing the form holds can differ from what the user was shown.
        Assert.Contains("_sent.ReadOnly = true;", controls, StringComparison.Ordinal);
        Assert.Contains("_sent.Text = string.Join(\"\\r\\n\", ReportPrivacy.Shown(context));", controls, StringComparison.Ordinal);
        Assert.Contains("_context.App, _context.Where.Trim(), _context.Actions, _context.Log", controls, StringComparison.Ordinal);
        Assert.DoesNotContain("_sent.Text.", controls, StringComparison.Ordinal);
        // The context is set in two places alone: the start, and Fill.
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(controls, @"_context = ").Count);
    }

    [Fact]
    public void What_is_sent_is_shown_under_short_labels_with_none_for_an_empty_part()
    {
        var context = new ReportContext("axThing 1.0, Windows 11 build 26200, NVDA 2025.3", "a list, item 2 of 9", ["5 s ago: Find, Ctrl+F", "2 s ago: Refresh, F5"], []);

        Assert.Equal(
            [
                "App and version:", "axThing 1.0, Windows 11 build 26200, NVDA 2025.3", "",
                "Last actions:", "5 s ago: Find, Ctrl+F", "2 s ago: Refresh, F5", "",
                "Where the focus was:", "a list, item 2 of 9", "",
                "Log lines:", "none",
            ],
            ReportPrivacy.Shown(context));
        Assert.Equal(["Last actions:", "none"], ReportPrivacy.Shown(context with { Actions = [] }).Skip(3).Take(2));
    }
}
