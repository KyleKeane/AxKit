using System.Diagnostics;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>
/// The two sides of an approval held to one text (docs/axKit/outside-reports.md OR-6.4, OR-7): an entry signed by
/// axTask's <see cref="Approvals"/> is accepted by the scripts that hold the work to it (tools/outside-check.ps1,
/// tools/item.ps1, tools/item-of-commit.ps1, through tools/approvals.ps1 in Windows PowerShell), with a key pinned in a
/// repository of the test's own; and once one sentence of the approved scope changes, each refuses. The lists are in a
/// temporary folder.
/// </summary>
public sealed class ApprovalScriptTests
{
    private const string Passphrase = "correct horse battery";
    private const string ItemTitle = "axStart: on its first launch NVDA does not speak its list until it is started again";
    private const string Report = "KyleKeane/AxKit-reports issue 15";

    private sealed class Setup : IDisposable
    {
        private readonly TempFolder _temp = new("axkit-approval-scripts");

        public Setup()
        {
            Management = Directory.CreateDirectory(Path.Combine(_temp.Path, "axkit-management")).FullName;
            Priorities = Directory.CreateDirectory(Path.Combine(Management, "priorities")).FullName;
            Outside = Directory.CreateDirectory(Path.Combine(Management, OutsideReports.FolderName)).FullName;
            Repository = Directory.CreateDirectory(Path.Combine(_temp.Path, "repository")).FullName;
            Directory.CreateDirectory(Path.Combine(Repository, "docs", "axKit"));
            var (keyFile, publicKey) = Approvals.MakeKey(Passphrase, 1000);
            KeyFile = keyFile;
            File.WriteAllText(Path.Combine(Repository, "docs", "axKit", "approval-key.pub"), Approvals.PinnedText(publicKey));
            EntryPath = Path.Combine(Outside, "axStart said nothing.json");
        }

        public string Management { get; }

        public string Priorities { get; }

        public string Outside { get; }

        public string Repository { get; }

        public string KeyFile { get; }

        public string EntryPath { get; }

        /// <summary>The entry awaiting approval, then signed with the test's key as axTask signs it.</summary>
        public void WriteApprovedEntry(string proposed = "Reproduce a fresh start;\r\nmake the first showing take the foreground.")
        {
            var entry = ItemDocument.Parse($$"""
                {"format":"axkit-item 1","fields":[
                  {"title":"Title","type":"text","value":"axStart: said nothing on its first launch"},
                  {"title":"Report","type":"text","value":"{{Report}}"},
                  {"title":"Received","type":"date","value":"2026-11-21"},
                  {"title":"State","type":"choice","value":"Awaiting approval","options":["To review","Awaiting approval","Approved","Declined","Done"]},
                  {"title":"Harm review","type":"longtext","value":"Nothing found."},
                  {"title":"Category","type":"choice","value":"New item","options":[]},
                  {"title":"Item","type":"text","value":"{{ItemTitle}}"},
                  {"title":"Proposed","type":"longtext","value":{{System.Text.Json.JsonSerializer.Serialize(proposed)}}},
                  {"title":"Proposed priority","type":"choice","value":"High","options":["High","Normal","Low"]},
                  {"title":"Approval","type":"text","value":""},
                  {"title":"Approved on","type":"date","value":""}
                ]}
                """);
            Assert.Null(OutsideReports.Refusal(entry));
            var text = Approvals.Text(OutsideReports.EntryOf(entry, Approvals.Approved, "2026-11-21"));
            var signed = OutsideReports.Signed(entry, approve: true, Approvals.Sign(text, KeyFile, Passphrase), "2026-11-21");
            File.WriteAllText(EntryPath, signed.ToText());
        }

        /// <summary>One sentence added to the approved scope, as a later edit of the file would.</summary>
        public void WidenTheScope() =>
            File.WriteAllText(EntryPath, File.ReadAllText(EntryPath).Replace("take the foreground.", "take the foreground. And add a download.", StringComparison.Ordinal));

        public void Dispose() => _temp.Dispose();
    }

    private static ProcessResult Script(string script, params string[] arguments)
    {
        var start = new ProcessStartInfo(SystemPrograms.PowerShell);
        foreach (var argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.Combine(SourceTexts.RepositoryRoot(), "tools", script) }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        var result = ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
        Assert.False(result.TimedOut, $"{script} did not end");
        return result;
    }

    private static ProcessResult NewItem(Setup setup, string title) =>
        Script("item.ps1", "new", "-Title", title, "-App", "axStart", "-Priority", "High", "-Where", Report, "-Notes", "From the approved entry.",
            "-Lists", setup.Priorities, "-PinnedFrom", setup.Repository);

    private static ProcessResult Check(Setup setup) =>
        Script("outside-check.ps1", "-Management", setup.Management, "-Repository", setup.Repository);

    private static ProcessResult Commit(Setup setup)
    {
        var message = Path.Combine(setup.Management, "message.txt");
        File.WriteAllText(message, "Make axStart's first showing take the foreground\n\nItem: on its first launch NVDA does not speak\n");
        return Script("item-of-commit.ps1", message, "-Lists", setup.Priorities, "-PinnedFrom", setup.Repository, "-Check");
    }

    [Fact]
    public void An_entry_signed_by_axTask_lets_its_item_be_written_checked_and_committed_on()
    {
        using var setup = new Setup();
        setup.WriteApprovedEntry();

        var written = NewItem(setup, ItemTitle);
        Assert.True(written.ExitCode == 0, written.Output);

        var checkedOnce = Check(setup);
        Assert.True(checkedOnce.ExitCode == 0, checkedOnce.Output);
        Assert.Contains("no item from outside without an approval (1 entries", checkedOnce.Output, StringComparison.Ordinal);

        var committed = Commit(setup);
        Assert.True(committed.ExitCode == 0, committed.Output);
    }

    [Fact]
    public void An_item_the_entry_does_not_name_is_refused_even_with_an_approval()
    {
        using var setup = new Setup();
        setup.WriteApprovedEntry();

        var other = NewItem(setup, "axStart: something else the report did not ask for");

        Assert.Equal(1, other.ExitCode);
        Assert.Contains("report 15 is outside work", other.Output, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(setup.Priorities));
    }

    [Fact]
    public void Without_an_approval_nothing_of_the_report_is_written()
    {
        using var setup = new Setup();

        var refused = NewItem(setup, ItemTitle);

        Assert.Equal(1, refused.ExitCode);
        Assert.Contains("report 15 is outside work", refused.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_scope_widened_after_the_approval_is_refused_by_the_check_and_the_commit()
    {
        using var setup = new Setup();
        setup.WriteApprovedEntry();
        Assert.Equal(0, NewItem(setup, ItemTitle).ExitCode);

        setup.WidenTheScope();

        var checkedAgain = Check(setup);
        Assert.Equal(1, checkedAgain.ExitCode);
        Assert.Contains("signature:", checkedAgain.Output, StringComparison.Ordinal);
        Assert.Contains("unapproved:", checkedAgain.Output, StringComparison.Ordinal);

        var committed = Commit(setup);
        Assert.Equal(1, committed.ExitCode);
        Assert.Contains("comes from outside (report 15)", committed.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_key_of_its_own_beside_the_repository_approves_nothing()
    {
        // Someone who makes a key of their own and signs with it: the pinned key does not verify the signature.
        using var setup = new Setup();
        var (ownKey, ownPublic) = Approvals.MakeKey("someone else's passphrase", 1000);
        File.WriteAllText(Path.Combine(setup.Management, OutsideReports.KeyFileName), ownKey);
        setup.WriteApprovedEntry();
        var entry = ItemDocument.Parse(File.ReadAllText(setup.EntryPath));
        var text = Approvals.Text(OutsideReports.EntryOf(entry, Approvals.Approved, "2026-11-21"));
        File.WriteAllText(setup.EntryPath, OutsideReports.Signed(entry, approve: true, Approvals.Sign(text, ownKey, "someone else's passphrase"), "2026-11-21").ToText());
        Assert.True(Approvals.Verify(text, OutsideReports.Field(ItemDocument.Parse(File.ReadAllText(setup.EntryPath)), "Approval"), ownPublic));

        var refused = NewItem(setup, ItemTitle);

        Assert.Equal(1, refused.ExitCode);
        Assert.Equal(1, Check(setup).ExitCode);
    }
}
