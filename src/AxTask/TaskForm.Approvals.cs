using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

public sealed partial class TaskForm
{
    // ---- the maintainer's approval of outside reports (docs/axKit/outside-reports.md sections 5 and 6) ----
    //
    // Approve and Decline sign an entry of the outside reports list with the approval key, which only the maintainer's
    // passphrase opens; Set up makes that key, and a second time changes its passphrase, signing the new key with the
    // old. The passphrase is read from a hidden field of a notice, used off the UI thread, and never kept, logged or
    // shown. None of these is for the AI tools (OR-5.4).

    /// <summary>The menu items: Approve and Decline for the Item menu, Set up for Options; each withheld from the AI tools.</summary>
    private ToolStripMenuItem[] CreateApprovalItems() =>
    [
        CommandPalette.NeverForAiTools(CommandPalette.Marked(new ToolStripMenuItem("&Approve this report...", null, (_, _) => DecideReport(approve: true)), CommandMark.Act)),
        CommandPalette.NeverForAiTools(CommandPalette.Marked(new ToolStripMenuItem("Dec&line this report...", null, (_, _) => DecideReport(approve: false)), CommandMark.Act)),
    ];

    private ToolStripMenuItem CreateApprovalKeyItem() =>
        CommandPalette.NeverForAiTools(CommandPalette.Marked(new ToolStripMenuItem("Approval &passphrase...", null, (_, _) => SetUpApprovalKey()), CommandMark.Act));

    /// <summary>Approve or Decline on the entry selected in the outside reports list (OR-5.1).</summary>
    private void DecideReport(bool approve)
    {
        if (SelectedRow() is not { Tag: ItemFile entry } || !OutsideReports.IsList(Path.GetDirectoryName(entry.Path) ?? string.Empty))
        {
            EventDoor.Raise(EventKind.Refused);
            Announce("Approve and Decline act on an entry of the outside reports list: open the list and choose an entry", true);
            return;
        }

        if (!Changeable(entry))
        {
            return;
        }

        if (OutsideReports.Refusal(entry.Flat) is { } refused)
        {
            EventDoor.Raise(EventKind.Refused);
            Announce(refused, true);
            return;
        }

        var keyFile = OutsideReports.KeyFileOf(Path.GetDirectoryName(entry.Path)!);
        if (!File.Exists(keyFile))
        {
            EventDoor.Raise(EventKind.Refused);
            Announce("No approval passphrase is set up yet: Options, Approval passphrase", true);
            return;
        }

        var verb = approve ? "approve" : "decline";
        ShowNotice(new Notice((approve ? "Approve: " : "Decline: ") + entry.Flat.Title, OutsideReports.NoticeText(entry.Flat, approve),
        [
            new OverlayChoice(approve ? "&Approve" : "&Decline", () =>
            {
                var passphrase = Overlay.InputText;
                Overlay.SetInputText(string.Empty);
                SignEntry(entry, keyFile, passphrase, approve);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Passphrase:", $"Type your approval passphrase to {verb}", string.Empty, $"Type your approval passphrase to {verb}", Hidden: true),
            // The entry is read before it is signed: the focus starts in the text, the passphrase field is the next stop (OR-5.1).
            FocusText = true,
        });
    }

    /// <summary>The signature made off the UI thread (PBKDF2 takes about half a second), then the entry written as one undo step (OR-5.2).</summary>
    private async void SignEntry(ItemFile entry, string keyFile, string passphrase, bool approve)
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var text = Approvals.Text(OutsideReports.EntryOf(entry.Flat, approve ? Approvals.Approved : Approvals.Declined, date));
        string? signature = null;
        string? failed = null;
        try
        {
            var keyText = await File.ReadAllTextAsync(keyFile);
            signature = await Task.Run(() => Approvals.Sign(text, keyText, passphrase));
        }
        catch (ApprovalException ex)
        {
            failed = ex.Message;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failed = "The approval key could not be read: " + ex.Message;
        }

        if (signature is null)
        {
            EventDoor.Raise(EventKind.Refused);
            Announce(failed ?? "Nothing was signed", true);
            return;
        }

        if (!CheckFresh(entry.Path, entry.Text))
        {
            return;
        }

        var signed = OutsideReports.Signed(entry.Flat, entry.Config, approve, signature, date);
        Commit(new Step([FileChange.Write(entry.Path, entry.Text, signed.ToText())], (approve ? "Approved: " : "Declined: ") + entry.Flat.Title), entry.Path);
    }

    /// <summary>
    /// Options, Approval passphrase (OR-6.1, OR-6.3): with no key beside the list shown, a new one under a passphrase typed
    /// twice; with one, the old passphrase first, then the new one twice, and the change signed with the old key.
    /// </summary>
    private void SetUpApprovalKey()
    {
        if (!OutsideReports.IsList(_folder))
        {
            EventDoor.Raise(EventKind.Refused);
            Announce("Open the outside reports list first: the approval key is kept beside it", true);
            return;
        }

        var keyFile = OutsideReports.KeyFileOf(_folder);
        if (!File.Exists(keyFile))
        {
            AskNewPassphrase(keyFile, oldKeyText: null, oldPassphrase: null);
            return;
        }

        // One change at a time (OR-6.3): the line the last change wrote is still beside the key, so that change is not
        // pinned yet, and a second one would be signed by a key the code repository never pinned.
        if (File.Exists(Path.Combine(Path.GetDirectoryName(keyFile)!, OutsideReports.ChangeFileName)))
        {
            EventDoor.Raise(EventKind.Refused);
            Announce("The last change of the approval passphrase is not pinned in the code repository yet: have it pinned first, then change it again", true);
            return;
        }

        string oldKeyText;
        string fingerprint;
        try
        {
            oldKeyText = File.ReadAllText(keyFile);
            fingerprint = SpokenFingerprint(Approvals.PublicKeyOf(oldKeyText));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ApprovalException)
        {
            Fail("The approval key could not be read: " + ex.Message);
            return;
        }

        ShowNotice(new Notice("Change the approval passphrase",
            $"The key now in use has the fingerprint {fingerprint}. It is kept in {keyFile}.\r\n\r\n"
            + "A new key replaces it, and the old one signs the change. Approvals given with the old key and still in force are given again afterwards. Type the current passphrase first.",
        [
            new OverlayChoice("&Next", () =>
            {
                var old = Overlay.InputText;
                Overlay.SetInputText(string.Empty);
                AskNewPassphrase(keyFile, oldKeyText, old);
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Current passphrase:", "Type your current approval passphrase", string.Empty, "Type your current approval passphrase", Hidden: true),
        });
    }

    private void AskNewPassphrase(string keyFile, string? oldKeyText, string? oldPassphrase)
    {
        ShowNotice(new Notice("New approval passphrase",
            $"At least {Approvals.MinimumPassphrase} characters that only you know. axKit never keeps it and cannot show it again; a forgotten one cannot be got back.\r\n\r\n"
            + $"The key it locks is kept in {keyFile}.",
        [
            new OverlayChoice("&Next", () =>
            {
                var typed = Overlay.InputText;
                Overlay.SetInputText(string.Empty);
                if (typed.Length < Approvals.MinimumPassphrase)
                {
                    EventDoor.Raise(EventKind.Refused);
                    Announce($"The passphrase needs {Approvals.MinimumPassphrase} characters at least: nothing changed", true);
                    return;
                }

                ShowNotice(new Notice("Confirm the approval passphrase", "Type the same passphrase once more, to confirm it.",
                [
                    new OverlayChoice("&Set", () =>
                    {
                        var again = Overlay.InputText;
                        Overlay.SetInputText(string.Empty);
                        if (again != typed)
                        {
                            EventDoor.Raise(EventKind.Refused);
                            Announce("The two passphrases differ: nothing changed", true);
                            return;
                        }

                        MakeApprovalKey(keyFile, typed, oldKeyText, oldPassphrase);
                    }, IsDefault: true),
                    new OverlayChoice("&Cancel", IsCancel: true),
                ])
                {
                    Input = new OverlayInput("Confirm passphrase:", "Re-enter the new approval passphrase to confirm it", string.Empty, "Type the passphrase again to confirm it", Hidden: true),
                });
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("New passphrase:", "Type a new approval passphrase", string.Empty, "Type a new approval passphrase", Hidden: true),
        });
    }

    /// <summary>The key made off the UI thread; the old key, when there is one, signs the new key's fingerprint first, so a wrong old passphrase changes nothing.</summary>
    private async void MakeApprovalKey(string keyFile, string passphrase, string? oldKeyText, string? oldPassphrase)
    {
        string? failed = null;
        try
        {
            var (file, publicKey) = await Task.Run(() => Approvals.MakeKey(passphrase));
            string? change = null;
            if (oldKeyText is not null)
            {
                change = await Task.Run(() => Approvals.Sign(Approvals.ChangeText(publicKey), oldKeyText, oldPassphrase ?? string.Empty));
            }

            // The change's line and the public half first, the key itself last and by a move: whatever fails on the
            // way, the key file is the old one whole or the new one whole, never half, and the old key is not lost
            // before its signature of the change is on disk.
            var folder = Path.GetDirectoryName(keyFile)!;
            if (change is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(folder, OutsideReports.ChangeFileName), $"Approval key changed: {change}\r\n");
            }

            await File.WriteAllTextAsync(Path.Combine(folder, OutsideReports.PinnedFileName), Approvals.PinnedText(publicKey));
            var written = keyFile + ".new";
            await File.WriteAllTextAsync(written, file);
            File.Move(written, keyFile, overwrite: true);

            Announce($"Approval passphrase set. The key's fingerprint is {SpokenFingerprint(publicKey)}", true);
        }
        catch (ApprovalException ex)
        {
            failed = oldKeyText is null ? ex.Message : "That is not the current approval passphrase: nothing changed";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failed = "The approval key could not be written: " + ex.Message;
        }

        if (failed is not null)
        {
            EventDoor.Raise(EventKind.Refused);
            Announce(failed, true);
        }
    }

    /// <summary>A key's fingerprint as it is said: sixteen characters in groups of four.</summary>
    private static string SpokenFingerprint(ApprovalPublicKey key) => string.Join(' ', key.Fingerprint.Chunk(4).Select(part => new string(part)));
}
