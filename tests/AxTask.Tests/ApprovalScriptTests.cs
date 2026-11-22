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
            // The priorities list's configuration: the scripts take the fields of an item from it.
            static System.Text.Json.Nodes.JsonNode Empty() => System.Text.Json.Nodes.JsonValue.Create(string.Empty);
            TestLists.List(Priorities,
            [
                new("Done", FieldTypes.Check, System.Text.Json.Nodes.JsonValue.Create(false), []),
                new("Started", FieldTypes.Date, Empty(), []),
                new("Priority", FieldTypes.Choice, Empty(), ["High", "Normal", "Low"]),
                new("App", FieldTypes.Text, Empty(), []),
                new("Tags", FieldTypes.Text, Empty(), []),
                new("Due", FieldTypes.Date, Empty(), []),
                new("Where it is written", FieldTypes.Text, Empty(), []),
                new("Notes", FieldTypes.LongText, Empty(), []),
            ]);
            Outside = Directory.CreateDirectory(Path.Combine(Management, OutsideReports.FolderName)).FullName;
            Repository = Directory.CreateDirectory(Path.Combine(_temp.Path, "repository")).FullName;
            Directory.CreateDirectory(Path.Combine(Repository, "docs", "axKit"));
            // A repository of git's own, as the commit hook asks git what the commit changes in it.
            Git(Repository, "init", "-q");
            Git(Repository, "config", "user.name", "test");
            Git(Repository, "config", "user.email", "test");
            var (keyFile, publicKey) = Approvals.MakeKey(Passphrase, 1000);
            KeyFile = keyFile;
            PublicKey = publicKey;
            File.WriteAllText(PinnedPath, Approvals.PinnedText(publicKey));
            EntryPath = Path.Combine(Outside, "axStart said nothing.json");
        }

        public ApprovalPublicKey PublicKey { get; }

        public string PinnedPath => Path.Combine(Repository, "docs", "axKit", "approval-key.pub");

        public string Management { get; }

        public string Priorities { get; }

        public string Outside { get; }

        public string Repository { get; }

        public string KeyFile { get; }

        public string EntryPath { get; }

        /// <summary>The entry awaiting approval, then signed with the test's key as axTask signs it.</summary>
        public void WriteApprovedEntry(string proposed = "Reproduce a fresh start;\r\nmake the first showing take the foreground.")
        {
            var entry = FlatItem.Parse($$"""
                {
                  "schema": "list.json",
                  "Title": "axStart: said nothing on its first launch",
                  "Report": "{{Report}}",
                  "Received": "2026-11-21",
                  "State": "Awaiting approval",
                  "Harm review": "Nothing found.",
                  "Category": "New item",
                  "Item": "{{ItemTitle}}",
                  "Proposed": {{System.Text.Json.JsonSerializer.Serialize(proposed)}},
                  "Proposed priority": "High",
                  "Approval": "",
                  "Approved on": ""
                }
                """);
            Assert.Null(OutsideReports.Refusal(entry));
            var text = Approvals.Text(OutsideReports.EntryOf(entry, Approvals.Approved, "2026-11-21"));
            var signed = OutsideReports.Signed(entry, null, approve: true, Approvals.Sign(text, KeyFile, Passphrase), "2026-11-21");
            File.WriteAllText(EntryPath, signed.ToText());
        }

        /// <summary>The entry's State written as the scripts or a hand would, the signature left as it is.</summary>
        public void SetState(string state)
        {
            var entry = FlatItem.Parse(File.ReadAllText(EntryPath));
            File.WriteAllText(EntryPath, entry.With("State", System.Text.Json.Nodes.JsonValue.Create(state)).ToText());
        }

        /// <summary>A key as axTask writes it beside the lists: the key file, its public half, and for a change the line the old key signed.</summary>
        public void WriteKeyBesideLists(string keyFile, ApprovalPublicKey publicKey, string? changeSignature = null)
        {
            File.WriteAllText(Path.Combine(Management, OutsideReports.KeyFileName), keyFile);
            File.WriteAllText(Path.Combine(Management, OutsideReports.PinnedFileName), Approvals.PinnedText(publicKey));
            if (changeSignature is not null)
            {
                File.WriteAllText(Path.Combine(Management, OutsideReports.ChangeFileName), $"Approval key changed: {changeSignature}\r\n");
            }
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

    /// <summary>git in the test's repository, with a name of the test's own for its commits.</summary>
    private static void Git(string repository, params string[] arguments)
    {
        var start = new ProcessStartInfo("git");
        foreach (var argument in new[] { "-C", repository, "-c", "user.name=test", "-c", "user.email=test" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        var result = ProcessRun.Run(start, [], TimeSpan.FromSeconds(60));
        Assert.True(!result.TimedOut && result.ExitCode == 0, $"git {string.Join(' ', arguments)}: {result.Output}");
    }

    /// <summary>The commit hook on a message about the pinned key alone, with its change staged.</summary>
    private static ProcessResult CommitKey(Setup setup, string extraLine = "")
    {
        Git(setup.Repository, "add", "docs/axKit/approval-key.pub");
        var message = Path.Combine(setup.Management, "message.txt");
        File.WriteAllText(message, $"Pin the approval key\n\nItem: none, the test's key\n{extraLine}");
        return Script("item-of-commit.ps1", message, "-Lists", setup.Priorities, "-PinnedFrom", setup.Repository, "-Check");
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
        // Nothing but the list's own file: no item was written.
        Assert.Equal(["list.json"], Directory.GetFiles(setup.Priorities).Select(Path.GetFileName));
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
    public void The_first_key_is_pinned_and_a_new_one_only_with_the_old_ones_signature()
    {
        // OR-6.3. The first pin failed in the real hook on 2026-11-21: git's error for the key missing from HEAD
        // stopped the script.
        using var setup = new Setup();
        var first = CommitKey(setup);
        Assert.True(first.ExitCode == 0, first.Output);
        Git(setup.Repository, "commit", "-q", "-m", "Pin the approval key");

        var (_, newPublic) = Approvals.MakeKey("a new long passphrase", 1000);
        File.WriteAllText(setup.PinnedPath, Approvals.PinnedText(newPublic));
        var unsigned = CommitKey(setup);
        Assert.Equal(1, unsigned.ExitCode);
        Assert.Contains("changes or removes the pinned approval key", unsigned.Output, StringComparison.Ordinal);

        var signature = Approvals.Sign(Approvals.ChangeText(newPublic), setup.KeyFile, Passphrase);
        var signed = CommitKey(setup, $"Approval key changed: {signature}\n");
        Assert.True(signed.ExitCode == 0, signed.Output);
    }

    [Fact]
    public void After_a_change_of_key_a_closed_entry_stays_his_and_an_approval_in_force_is_offered_again()
    {
        // OR-6.5: the key history comes from git. The approval in force must be the pinned key's own.
        using var setup = new Setup();
        setup.WriteApprovedEntry();
        Git(setup.Repository, "add", "docs/axKit/approval-key.pub");
        Git(setup.Repository, "commit", "-q", "-m", "Pin the approval key");
        var (_, newPublic) = Approvals.MakeKey("a new long passphrase", 1000);
        File.WriteAllText(setup.PinnedPath, Approvals.PinnedText(newPublic));
        Git(setup.Repository, "add", "docs/axKit/approval-key.pub");
        Git(setup.Repository, "commit", "-q", "-m", $"Change the key\n\nApproval key changed: {Approvals.Sign(Approvals.ChangeText(newPublic), setup.KeyFile, Passphrase)}");

        var inForce = Check(setup);
        Assert.Equal(1, inForce.ExitCode);
        Assert.Contains("signed with a key pinned before this one", inForce.Output, StringComparison.Ordinal);

        // Closed under the old key: still what he signed, and nothing is named.
        setup.SetState("Done");
        var closed = Check(setup);
        Assert.True(closed.ExitCode == 0, closed.Output);
        Assert.DoesNotContain("replaced without", closed.Output, StringComparison.Ordinal);

        // In force again: offered to him anew, never while the pinned key verifies it.
        setup.SetState("Approved");
        var reopened = Script("outside-report.ps1", "reopen", "-Name", "said nothing", "-Management", setup.Management, "-Repository", setup.Repository);
        Assert.True(reopened.ExitCode == 0, reopened.Output);
        Assert.Equal(OutsideReports.AwaitingApproval, OutsideReports.Field(FlatItem.Parse(File.ReadAllText(setup.EntryPath)), "State"));
        Assert.Equal(string.Empty, OutsideReports.Field(FlatItem.Parse(File.ReadAllText(setup.EntryPath)), "Approval"));
        Assert.Equal(0, Check(setup).ExitCode);
    }

    [Fact]
    public void An_entry_closed_by_hand_and_a_key_replaced_without_a_signature_are_both_said()
    {
        using var setup = new Setup();
        setup.WriteApprovedEntry();
        Git(setup.Repository, "add", "docs/axKit/approval-key.pub");
        Git(setup.Repository, "commit", "-q", "-m", "Pin the approval key");

        // Declined without his signature of a decline: the approval's signature does not cover it.
        setup.SetState("Declined");
        var byHand = Check(setup);
        Assert.Equal(1, byHand.ExitCode);
        Assert.Contains("holds no approval of a key this repository pinned", byHand.Output, StringComparison.Ordinal);
        setup.SetState("Done");

        // A key put in place of the pinned one by a commit nobody signed: said every time, with the commit.
        var (_, other) = Approvals.MakeKey("someone else's passphrase", 1000);
        File.WriteAllText(setup.PinnedPath, Approvals.PinnedText(other));
        Git(setup.Repository, "add", "docs/axKit/approval-key.pub");
        Git(setup.Repository, "commit", "-q", "-m", "Replace the key");
        var replaced = Check(setup);
        Assert.True(replaced.ExitCode == 0, replaced.Output);
        Assert.Contains("replaced without the old key's signature in commit", replaced.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void The_pin_script_pins_the_first_key_and_a_changed_one_with_its_signed_line()
    {
        using var setup = new Setup();
        File.Delete(setup.PinnedPath);
        setup.WriteKeyBesideLists(setup.KeyFile, setup.PublicKey);

        // Waiting to be pinned: the check says so.
        var waiting = Check(setup);
        Assert.Equal(1, waiting.ExitCode);
        Assert.Contains("Outside reports: pin:", waiting.Output, StringComparison.Ordinal);

        var first = Pin(setup);
        Assert.True(first.ExitCode == 0, first.Output);
        Assert.Equal(setup.PublicKey.Fingerprint, Approvals.ReadPinned(File.ReadAllText(setup.PinnedPath))!.Fingerprint);
        Assert.Equal(0, Check(setup).ExitCode);
        Assert.Equal(1, Pin(setup).ExitCode);

        // A change without the line the old key signed is not pinned, and the pinned key stays.
        var (newKeyFile, newPublic) = Approvals.MakeKey("a new long passphrase", 1000);
        setup.WriteKeyBesideLists(newKeyFile, newPublic);
        var unsigned = Pin(setup);
        Assert.Equal(1, unsigned.ExitCode);
        Assert.Equal(setup.PublicKey.Fingerprint, Approvals.ReadPinned(File.ReadAllText(setup.PinnedPath))!.Fingerprint);

        setup.WriteKeyBesideLists(newKeyFile, newPublic, Approvals.Sign(Approvals.ChangeText(newPublic), setup.KeyFile, Passphrase));
        var changed = Pin(setup);
        Assert.True(changed.ExitCode == 0, changed.Output);
        Assert.Equal(newPublic.Fingerprint, Approvals.ReadPinned(File.ReadAllText(setup.PinnedPath))!.Fingerprint);
        Assert.False(File.Exists(Path.Combine(setup.Management, OutsideReports.ChangeFileName)));
        var after = Check(setup);
        Assert.True(after.ExitCode == 0, after.Output);
        Assert.DoesNotContain("replaced without", after.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_report_is_known_by_its_words_in_any_case_and_in_a_list_of_numbers()
    {
        using var setup = new Setup();
        setup.WriteApprovedEntry();

        var third = Script("item.ps1", "new", "-Title", ItemTitle, "-App", "axStart", "-Priority", "High",
            "-Where", "kylekeane/axkit-reports issues 15, #15 and 16", "-Notes", "From the approved entry.",
            "-Lists", setup.Priorities, "-PinnedFrom", setup.Repository);

        Assert.Equal(1, third.ExitCode);
        Assert.Contains("report 16 is outside work", third.Output, StringComparison.Ordinal);
    }

    private static ProcessResult Pin(Setup setup) =>
        Script("pin-approval-key.ps1", "-Management", setup.Management, "-Repository", setup.Repository, "-Trailer", "Signed-off-by: the test");

    [Fact]
    public void A_key_of_its_own_beside_the_repository_approves_nothing()
    {
        // Someone who makes a key of their own and signs with it: the pinned key does not verify the signature.
        using var setup = new Setup();
        var (ownKey, ownPublic) = Approvals.MakeKey("someone else's passphrase", 1000);
        File.WriteAllText(Path.Combine(setup.Management, OutsideReports.KeyFileName), ownKey);
        setup.WriteApprovedEntry();
        var entry = FlatItem.Parse(File.ReadAllText(setup.EntryPath));
        var text = Approvals.Text(OutsideReports.EntryOf(entry, Approvals.Approved, "2026-11-21"));
        File.WriteAllText(setup.EntryPath, OutsideReports.Signed(entry, null, approve: true, Approvals.Sign(text, ownKey, "someone else's passphrase"), "2026-11-21").ToText());
        Assert.True(Approvals.Verify(text, OutsideReports.Field(FlatItem.Parse(File.ReadAllText(setup.EntryPath)), "Approval"), ownPublic));

        var refused = NewItem(setup, ItemTitle);

        Assert.Equal(1, refused.ExitCode);
        Assert.Equal(1, Check(setup).ExitCode);
    }
}
