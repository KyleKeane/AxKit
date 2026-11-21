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

    /// <summary>A field's text by its title; empty when the entry has none.</summary>
    public static string Field(ItemDocument entry, string title) =>
        entry.IndexOf(title) is var i && i >= 0 && entry.Fields[i].Value is JsonValue value && value.TryGetValue<string>(out var text) ? text : string.Empty;

    /// <summary>The entry as an approval of the decision on the date signs it (OR-5.3).</summary>
    public static ApprovalEntry EntryOf(ItemDocument entry, string decision, string date) => new(
        decision, Field(entry, "Report"), entry.Title, Field(entry, "Category"), Field(entry, "Item"),
        Field(entry, "Proposed priority"), Field(entry, "Proposed"), Field(entry, "Harm review"), date);

    /// <summary>Why the entry cannot be decided now, or null: only an entry awaiting approval is approved or declined, and it holds its report.</summary>
    public static string? Refusal(ItemDocument entry)
    {
        var state = Field(entry, "State");
        if (state != AwaitingApproval)
        {
            return state.Length == 0 ? "This item is no entry of the outside reports list" : $"This entry is {state}, not awaiting your approval";
        }

        return Field(entry, "Report").Length == 0 ? "This entry names no report" : null;
    }

    /// <summary>The notice's text: everything the approval covers, the review whole, in the order the maintainer reads it.</summary>
    public static string NoticeText(ItemDocument entry, bool approve) =>
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

    /// <summary>The entry signed: its State, its Approval and its date, written; every other field as it was.</summary>
    public static ItemDocument Signed(ItemDocument entry, bool approve, string signature, string date)
    {
        var signed = With(entry, "State", approve ? ApprovedState : DeclinedState);
        signed = With(signed, "Approved on", date);
        return With(signed, "Approval", signature);
    }

    private static ItemDocument With(ItemDocument entry, string title, string value)
    {
        var i = entry.IndexOf(title);
        if (i < 0)
        {
            var type = title == "Approved on" ? FieldTypes.Date : FieldTypes.Text;
            entry = entry.WithField(entry.Fields.Count - 1, title, type, []);
            i = entry.IndexOf(title);
        }

        return entry.WithValue(i, JsonValue.Create(value));
    }
}
