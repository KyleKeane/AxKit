using AxKit.Core;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// The item's form (docs/axTask/forms-framework.md AF-14.1, AF-14.2; the maintainer's message 8 of 2026-11-21: "the
/// item details for a task into a standard form that opens on enter"). The form itself is the item window's, the
/// same in every app of it since 2026-11-23 (<c>AxKit.Forms/ItemForm.Form.cs</c>): Enter on an item opens it, Enter
/// on a field in the detail pane opens it in that field. This file says which rows have one, with the item's links,
/// and writes what a save changed.
/// </summary>
public sealed partial class TaskForm
{
    /// <summary>
    /// An item's form: one that can be changed, outside the search's level (where an item is changed in its fields,
    /// and renamed only where it lives, TK-7b.4), and whose configuration reads; any other row keeps Enter's own work.
    /// </summary>
    protected override ItemFormSource? FormOf(ItemRow row)
    {
        if (_search is not null || row.Tag is not ItemFile { ConfigProblem: null } item)
        {
            return null;
        }

        var top = TopOf(item.Path);

        // A link is typed as the other item's title, looked for among the items of the item's own folder of lists.
        (string? Path, string? Refused) Link(string title)
        {
            var matches = ItemFolder.AllItems(top).Where(other => !other.Path.Equals(item.Path, StringComparison.OrdinalIgnoreCase)
                && ItemFolder.NameOf(other).Equals(title, StringComparison.OrdinalIgnoreCase)).ToList();
            return matches.Count switch
            {
                0 => (null, $"No item is called {title}"),
                1 => (ItemFolder.LinkTo(item.Path, matches[0].Path), null),
                _ => (null, $"{matches.Count} items are called {title}: rename one first"),
            };
        }

        return new ItemFormSource(ItemFolder.NameOf(item), item.Document, (changed, said) => SaveItemForm(item, changed, said))
        {
            LinkTitle = link => ItemFolder.Resolve(item.Path, link) is { } path && ItemFolder.ReadItem(path) is { } linked ? ItemFolder.NameOf(linked) : string.Empty,
            Link = Link,
            Note = item.KeptLine is { } kept ? $"{kept}: they are in the item's file and no field of its list." : null,
        };
    }

    protected override DateOnly FormToday => Today;

    /// <summary>
    /// The form saved: every field that changed, written as one step. A changed Title renames the item's file with
    /// it, the links to it following, as F2 does (IA-4.5, IA-D3).
    /// </summary>
    private void SaveItemForm(ItemFile item, ItemDocument changed, string said)
    {
        if (!Changeable(item) || !CheckFresh(item.Path, item.Text))
        {
            return;
        }

        var (parts, path) = ItemChangeParts(item, changed);
        Commit(new Step(parts, said), path);
    }

    /// <summary>
    /// The writes of a change to an item: its file with what changed, and, when the Title changed, the file renamed
    /// after it with every link to it written again (IA-4.5, IA-D3); and the item's path after them.
    /// </summary>
    private (List<FileChange> Parts, string Path) ItemChangeParts(ItemFile item, ItemDocument changed)
    {
        var parts = new List<FileChange> { FileChange.Write(item.Path, item.Text, item.TextWith(changed)) };
        var to = item.Path;
        if (changed.Title != item.Document.Title)
        {
            to = ItemFiles.FreePath(Path.GetDirectoryName(item.Path)!, ItemFiles.FileStem(changed.Title), ".json", own: item.Path);
            if (!to.Equals(item.Path, StringComparison.Ordinal))
            {
                parts.Add(FileChange.Move(item.Path, to));
                parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopOf(item.Path)), item.Path, to, skip: item.Path));
                // TK-4.9: the renamed item keeps its place in the list's own order.
                var folder = Path.GetDirectoryName(item.Path)!;
                var list = ItemFolder.ReadConfig(folder);
                if (list.Config is { } config && ListOrder.Renamed(ListOrder.Of(config), Path.GetFileNameWithoutExtension(item.Path), Path.GetFileNameWithoutExtension(to)) is { } order)
                {
                    parts.Add(FileChange.Write(Path.Combine(folder, ItemDocument.ListFile), list.Text, ListOrder.WithOrder(config, order).ToText()));
                }
            }
        }

        return (parts, to);
    }
}
