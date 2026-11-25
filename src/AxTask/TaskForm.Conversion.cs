using AxKit.Core;
using AxKit.Core.Conversion;
using AxKit.Core.Events;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// The conversion of a list written in axTask's earlier format (docs/axTask/forms-framework.md AF-13.6): such a list
/// is not opened; Right or Enter on it asks once, and Convert copies the folder into the Archive, then writes the
/// list's configuration and every item anew, as one step that Ctrl+Z takes back. The converting itself is
/// <c>AxKit.Core/Conversion</c>; this file only asks, keeps the copy and commits the plan.
/// </summary>
public sealed partial class TaskForm
{
    /// <summary>A list of the earlier format is not gone into: the question is asked instead, and the level stays as it was.</summary>
    protected override string? EntryRefused(ItemRow row)
    {
        if (row is not { IsList: true, Tag: null } || !ItemFolder.IsEarlier(row.Key))
        {
            return null;
        }

        AskConversion(row);
        return string.Empty;
    }

    private void AskConversion(ItemRow row)
    {
        // The other lists of the same folder of lists that are of the earlier format too: one answer can take them all.
        var all = ItemFolder.EarlierLists(Path.GetDirectoryName(row.Key) ?? TopFolder);
        if (!all.Contains(row.Key, StringComparer.OrdinalIgnoreCase))
        {
            all = [row.Key];
        }

        var choices = new List<OverlayChoice> { new("&Convert", () => ConvertLists([row.Key], row.Key), IsDefault: true) };
        if (all.Count > 1)
        {
            choices.Add(new OverlayChoice($"Convert &all {all.Count} lists", () => ConvertLists(all, row.Key)));
        }

        choices.Add(new OverlayChoice("Cance&l", IsCancel: true));
        ShowNotice(new Notice($"Convert {row.Name}?",
            "This list is written in axTask's earlier format, and is not opened until it is converted. Convert it? A copy is kept among the Archived lists, named with \", before conversion\" after the list's name.\r\n\r\n"
            + $"What changes: an item's file then holds only its values, and the list's configuration says which fields its items have, in which order and of which type. {BundleKeys.Text(AxTaskKeys.OpenInAxJson)} on the list opens that configuration. Nothing an item holds is dropped, and {BundleKeys.Text(ItemKeys.Undo)} takes the conversion back.\r\n\r\n"
            + "A copy of axKit older than this one does not read a converted list.",
            choices));
    }

    /// <summary>The lists converted, each after its copy is made, all as one step; what the conversion has to say is shown after.</summary>
    private void ConvertLists(IReadOnlyList<string> folders, string select)
    {
        var parts = new List<FileChange>();
        var notes = new List<string>();
        var items = 0;
        try
        {
            foreach (var folder in folders)
            {
                var plan = ListConversion.Plan(folder);
                if (plan.Files.Count == 0)
                {
                    continue;
                }

                // The copy first: nothing of a list is written before its copy is there.
                var archive = ItemFolder.ArchiveFolder(Path.GetDirectoryName(folder) is { } above && !ItemFolder.IsArchive(TopFolder, folder) ? above : TopFolder);
                Directory.CreateDirectory(archive);
                ListConversion.KeepCopy(folder, archive);
                parts.AddRange(plan.Files.Select(file => FileChange.Write(file.Path, file.Before, file.After)));
                items += plan.Files.Count(file => !Path.GetFileName(file.Path).Equals(ItemDocument.ListFile, StringComparison.OrdinalIgnoreCase));
                notes.AddRange(plan.Notes.Select(note => folders.Count > 1 ? $"{Path.GetFileName(folder)}: {note}" : note));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException)
        {
            Fail($"The conversion could not be prepared, and nothing was changed: {ex.Message}");
            return;
        }

        if (parts.Count == 0)
        {
            Refuse("Nothing to convert");
            Fill(select);
            return;
        }

        var what = folders.Count == 1 ? Path.GetFileName(folders[0]) : Count(folders.Count, "list");
        var said = $"Converted {what}: {Count(items, "item")}. A copy is among the Archived lists" + (notes.Count > 0 ? $". {Count(notes.Count, "thing")} to know" : string.Empty);
        if (!Commit(new Step(parts, said), select))
        {
            return;
        }

        Log.Info($"Converted {folders.Count} list(s) from the earlier format: {items} item(s), {notes.Count} note(s)");
        if (notes.Count > 0)
        {
            ShowNotice(Notice.Plain("The conversion: what to know", string.Join("\r\n", notes)));
        }
    }
}
