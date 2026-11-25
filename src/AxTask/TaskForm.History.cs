using AxKit.Core;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// The history of a list's items (SPEC.md TK-4.11, 2026-11-24): kept only for a list whose Options switch is on, off
/// by default; every step and every undo adds its lines (<see cref="ItemHistory.Lines"/>) to the list's history file,
/// and Item, History reads one item's. The rules are <see cref="ItemHistory"/>'s; this file only writes and shows.
/// </summary>
public sealed partial class TaskForm
{
    /// <summary>
    /// The lines of a step's parts added to the history files of the lists that keep one. A file that cannot be written
    /// is logged with the kind of error alone and never stops the step, which is already on disk.
    /// </summary>
    private static void RecordHistory(IReadOnlyList<FileChange> parts)
    {
        var keeps = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        bool Keeps(string folder)
        {
            if (!keeps.TryGetValue(folder, out var on))
            {
                on = ItemHistory.On(ItemFolder.ReadConfig(folder).Config);
                keeps[folder] = on;
            }

            return on;
        }

        foreach (var group in ItemHistory.Lines(parts, DateTime.Now, Keeps).GroupBy(line => line.Folder, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                File.AppendAllLines(Path.Combine(group.Key, ItemHistory.FileName), group.Select(line => line.Line));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Never a name or a value: the history holds the user's words.
                Log.Error("A list's history could not be written: " + ex.GetType().Name);
            }
        }
    }

    /// <summary>Options → Keep a history of changes in this list: the list's own switch, one step for Ctrl+Z.</summary>
    private void ToggleKeepHistory()
    {
        if (AtTop || _smart is not null || Gathered || IsArchiveFolder(_folder))
        {
            Announce("Open a list of yours first: the setting is the list's own", true);
            return;
        }

        var folder = _folder;
        if (ConfigurationForChange(folder) is not { } list)
        {
            return;
        }

        var on = !ItemHistory.On(list.Config);
        var written = ItemHistory.With(list.Config, on).ToText();
        Commit(new Step([FileChange.Write(Path.Combine(folder, ItemDocument.ListFile), list.Text, written)], on ? $"A history of changes is kept for {Path.GetFileName(folder)}" : $"No history of changes is kept for {Path.GetFileName(folder)}; what was kept stays in its file"), SelectedKey());
    }

    /// <summary>Item → History: the changes of the current item, newest first, in a notice; read off the UI thread.</summary>
    private void ShowItemHistory()
    {
        if (ItemOf(SelectedRow()) is not { } item)
        {
            Refuse("Choose an item first");
            return;
        }

        var folder = Path.GetDirectoryName(item.Path)!;
        var title = ItemFolder.NameOf(item);
        if (!ItemHistory.On(ItemFolder.ReadConfig(folder).Config))
        {
            Announce($"No history is kept for {Path.GetFileName(folder)}: Options, Keep a history of changes in this list turns it on", true);
            return;
        }

        Do(async () =>
        {
            var file = Path.Combine(folder, ItemHistory.FileName);
            string text;
            try
            {
                text = await Task.Run(() => File.Exists(file) ? File.ReadAllText(file) : string.Empty);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Fail("The list's history could not be read: " + ex.Message);
                return;
            }

            var entries = ItemHistory.Of(text, title);
            var shown = entries.Count == 0
                ? $"No change of {title} is in the history yet. It holds what changed since the history was turned on for this list."
                : string.Join("\n", entries.Select(ItemHistory.Said));
            ShowNotice(new Notice($"History of {title}, {entries.Count} {(entries.Count == 1 ? "change" : "changes")}", shown, [new OverlayChoice("&Close", () => { }, IsDefault: true, IsCancel: true)]));
        });
    }
}
