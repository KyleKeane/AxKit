using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// A list's own order (SPEC.md TK-4.9, TK-4.10; the pure rules are <see cref="ListOrder"/>): Ctrl+Shift+Up and
/// Ctrl+Shift+Down move the item you are on one place, Ctrl+Shift+Home and Ctrl+Shift+End to the top and the bottom,
/// each one step of Ctrl+Z, the order written into the list's configuration; Options, New items at the top says where
/// a new item lands. Only in a list of the user's own: not at the top, not in a smart list, the search or the Archive.
/// </summary>
public sealed partial class TaskForm
{
    /// <summary>Whether the keys of the order act here: in a list of the user's, on an item.</summary>
    private bool InOrderedList() =>
        !AtTop && _smart is null && !Gathered && !IsArchiveFolder(_folder) && NotRenaming() && SelectedRow() is { Tag: ItemFile };

    /// <summary>Registers the four commands of the order, in the Item menu, before the shared keys (which take Ctrl with the arrows nowhere).</summary>
    private void AddOrderCommands(ToolStripMenuItem item)
    {
        Add(item, new Command("Move &up in the list", AxTaskKeys.MoveItemUp, () => MoveInList(-1)) { When = InOrderedList, Mark = CommandMark.Change });
        Add(item, new Command("Move do&wn in the list", AxTaskKeys.MoveItemDown, () => MoveInList(1)) { When = InOrderedList, Mark = CommandMark.Change });
        Add(item, new Command("Move to the &top of the list", AxTaskKeys.MoveItemToTop, () => MoveInList(int.MinValue)) { When = InOrderedList, Mark = CommandMark.Change });
        Add(item, new Command("Move to the &bottom of the list", AxTaskKeys.MoveItemToBottom, () => MoveInList(int.MaxValue)) { When = InOrderedList, Mark = CommandMark.Change });
    }

    /// <summary>The item you are on moved in its list (TK-4.9): the new order written as one step; the edge sound at an end.</summary>
    private void MoveInList(int by)
    {
        if (!InOrderedList() || SelectedRow() is not { Tag: ItemFile item })
        {
            Announce("Items are moved in a list of yours: open one first", true);
            return;
        }

        var folder = _folder;
        if (ConfigurationForChange(folder) is not { } list)
        {
            return;
        }

        var shown = Rows.Where(row => row.Tag is ItemFile).Select(row => StemOf(((ItemFile)row.Tag!).Path)).ToList();
        var all = ItemFolder.Read(folder).Items.Select(file => StemOf(file.Path)).ToList();
        var from = shown.FindIndex(name => name.Equals(StemOf(item.Path), StringComparison.OrdinalIgnoreCase));
        if (ListOrder.MovedAmong(all, shown, from, by) is not { } moved)
        {
            EventDoor.Raise(EventKind.Edge);
            return;
        }

        var listFile = Path.Combine(folder, ItemDocument.ListFile);
        var written = ListOrder.WithOrder(list.Config, moved.Names).ToText();
        Commit(new Step([FileChange.Write(listFile, list.Text, written)], ListOrder.Said(by, ItemFolder.NameOf(item), moved.At, shown.Count)), item.Path);
    }

    /// <summary>Options, New items at the top (TK-4.10): where a new item of the list shown lands, written into its configuration.</summary>
    private void ToggleNewItemsAtTop()
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

        var top = !ListOrder.NewAtTop(list.Config);
        var written = ListOrder.WithNewItems(list.Config, top).ToText();
        Commit(new Step([FileChange.Write(Path.Combine(folder, ItemDocument.ListFile), list.Text, written)], top ? $"New items go to the top of {Path.GetFileName(folder)}" : $"New items go to the bottom of {Path.GetFileName(folder)}"), SelectedKey());
    }

    /// <summary>An item's name in the order: its file's name without ".json".</summary>
    private static string StemOf(string path) => Path.GetFileNameWithoutExtension(path);
}
