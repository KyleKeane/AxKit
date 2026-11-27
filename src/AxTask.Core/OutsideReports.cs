using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxTask.Core;

/// <summary>
/// The outside reports list as axTask's approval commands see it (docs/axKit/outside-reports.md sections 3, 5 and 6):
/// which folder it is, an entry's fields as the approval signs them, the notice's text, and the entry once signed.
/// Pure; the signing itself is <see cref="Approvals"/>.
/// </summary>
public static class OutsideReports
{
    /// <summary>The list's folder name beside the priorities list.</summary>
    public const string FolderName = "outside-reports";

    /// <summary>The key file beside the list (in the management folder), and the public half written beside it for the code repository to pin.</summary>
    public const string KeyFileName = "approval-key.json";
    public const string PinnedFileName = "approval-key.pub";

    /// <summary>Written beside the key by a change of passphrase: the line the commit that pins the new key carries (OR-6.3).</summary>
    public const string ChangeFileName = "approval-key-change.txt";

    public const string AwaitingApproval = "Awaiting approval";
    public const string ApprovedState = "Approved";
    public const string DeclinedState = "Declined";

    /// <summary>Whether a folder is the outside reports list.</summary>
    public static bool IsList(string folder) =>
        string.Equals(Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)), FolderName, StringComparison.OrdinalIgnoreCase);

    /// <summary>The key file of a list's folder: in the folder above it.</summary>
    public static string KeyFileOf(string listFolder) => Path.Combine(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(listFolder)) ?? listFolder, KeyFileName);

    /// <summary>
    /// A new approval key written beside the list (OR-6.3): for a change of passphrase the line the old key signed,
    /// then the public half, then the key itself, last and by a move, so that the key file is the old one whole or
    /// the new one whole and the old key is not lost before its signature of the change is on disk. Whole or not at
    /// all: when a write fails, the change's line is taken away again and the public half put back as it was, and
    /// the failure thrown on. Until 2026-11-27 a failure after the first write left the line, and with it every
    /// later change refused as "not pinned yet" for a change that never happened. Nothing of a key is logged or said.
    /// </summary>
    public static void WriteKey(string keyFile, string file, ApprovalPublicKey publicKey, string? change)
    {
        var folder = Path.GetDirectoryName(keyFile)!;
        var changeFile = Path.Combine(folder, ChangeFileName);
        var pinnedFile = Path.Combine(folder, PinnedFileName);
        var written = keyFile + ".new";
        var pinnedBefore = File.Exists(pinnedFile) ? File.ReadAllText(pinnedFile) : null;
        try
        {
            if (change is not null)
            {
                File.WriteAllText(changeFile, $"Approval key changed: {change}\r\n");
            }

            File.WriteAllText(pinnedFile, Approvals.PinnedText(publicKey));
            File.WriteAllText(written, file);
            File.Move(written, keyFile, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (change is not null)
                {
                    File.Delete(changeFile);
                }

                if (pinnedBefore is null)
                {
                    File.Delete(pinnedFile);
                }
                else
                {
                    File.WriteAllText(pinnedFile, pinnedBefore);
                }

                if (File.Exists(written))
                {
                    File.Delete(written);
                }
            }
            catch (Exception back) when (back is IOException or UnauthorizedAccessException)
            {
                // What could not be put back stays; the first failure is the one said.
            }

            throw;
        }
    }

    /// <summary>
    /// A value's text by its name; empty when the entry has none. Read from the entry's file itself, never from the
    /// item as the list's configuration shows it: what an approval signs must not depend on which fields a
    /// configuration names (docs/axTask/forms-framework.md AF-11.3, where a value not named is kept and not shown).
    /// </summary>
    public static string Field(FlatItem entry, string title) => entry.Text(title);

    /// <summary>The entry as an approval of the decision on the date signs it (OR-5.3).</summary>
    public static ApprovalEntry EntryOf(FlatItem entry, string decision, string date) => new(
        decision, Field(entry, "Report"), entry.Title, Field(entry, "Category"), Field(entry, "Item"),
        Field(entry, "Proposed priority"), Field(entry, "Proposed"), Field(entry, "Harm review"), date);

    /// <summary>Why the entry cannot be decided now, or null: only an entry awaiting approval is approved or declined, and it holds its report.</summary>
    public static string? Refusal(FlatItem entry)
    {
        var state = Field(entry, "State");
        if (state != AwaitingApproval)
        {
            return state.Length == 0 ? "This item is no entry of the outside reports list" : $"This entry is {state}, not awaiting your approval";
        }

        return Field(entry, "Report").Length == 0 ? "This entry names no report" : Unheard(entry);
    }

    /// <summary>The fields an approval signs (OR-5.3), by their titles; the Title is the entry's own.</summary>
    private static readonly string[] SignedFields = ["Title", "Report", "Category", "Item", "Proposed priority", "Proposed", "Harm review"];

    /// <summary>
    /// What is signed is what was heard (OR-5.6): the refusal for an entry whose signed fields hold a character a
    /// screen reader does not speak (a control character other than a line end or a tab, a zero-width or direction
    /// character, a separator or a character of private use), or null. Such a character could hide words from the
    /// maintainer's ear inside the scope he signs.
    /// </summary>
    public static string? Unheard(FlatItem entry)
    {
        foreach (var title in SignedFields)
        {
            var text = title == "Title" ? entry.Title : Field(entry, title);
            // By code point, not by char: the invisible tag characters are beyond the first plane (U+E0000 and on).
            var i = 0;
            while (i < text.Length)
            {
                var whole = System.Text.Rune.DecodeFromUtf16(text.AsSpan(i), out var rune, out var length) == System.Buffers.OperationStatus.Done;
                var category = System.Text.Rune.GetUnicodeCategory(rune);
                var spoken = rune.Value is '\r' or '\n' or '\t' || (whole
                    && category is not (System.Globalization.UnicodeCategory.Control or System.Globalization.UnicodeCategory.Format
                        or System.Globalization.UnicodeCategory.LineSeparator or System.Globalization.UnicodeCategory.ParagraphSeparator
                        or System.Globalization.UnicodeCategory.PrivateUse or System.Globalization.UnicodeCategory.OtherNotAssigned
                        or System.Globalization.UnicodeCategory.Surrogate));
                if (!spoken)
                {
                    var code = whole ? rune.Value : text[i];
                    return $"The field {title} holds a character that is not spoken (U+{code:X4}, character {i + 1}): nothing is signed. Have the entry written again without it";
                }

                i += Math.Max(1, length);
            }
        }

        return null;
    }

    /// <summary>The notice's text: everything the approval covers, the review whole, in the order the maintainer reads it.</summary>
    public static string NoticeText(FlatItem entry, bool approve) =>
        string.Join("\r\n",
            $"Report: {Field(entry, "Report")}",
            $"Category: {Field(entry, "Category")}",
            $"Item: {Field(entry, "Item")}",
            $"Proposed priority: {Field(entry, "Proposed priority")}",
            string.Empty,
            "Proposed, the scope your approval covers:",
            Field(entry, "Proposed"),
            string.Empty,
            "Harm review, written by the assistant:",
            Field(entry, "Harm review"),
            string.Empty,
            approve
                ? "Your passphrase signs exactly this. If any of it changes afterwards, the approval no longer counts."
                : "Your passphrase signs the decline: nothing of this report is worked on, and it is answered and closed.");

    /// <summary>
    /// The entry signed: its State, its Approval and its date, written; every other value as it was. A value the
    /// entry lacked is put where the list's configuration has its field.
    /// </summary>
    public static FlatItem Signed(FlatItem entry, ListConfig? config, bool approve, string signature, string date) =>
        entry.With("State", JsonValue.Create(approve ? ApprovedState : DeclinedState), config)
            .With("Approved on", JsonValue.Create(date), config)
            .With("Approval", JsonValue.Create(signature), config);
}
