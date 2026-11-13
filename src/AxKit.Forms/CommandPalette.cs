using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The command palette's window side (docs/axKit/SPEC.md AX-10): the window's menu bar read into entries, and the
/// marks on the submenus it leaves out or refreshes first. The notice and its keys are the window base's
/// (<see cref="AxKitForm"/>); the lines and the search are <see cref="Palette"/>, tested.
/// </summary>
public static class CommandPalette
{
    /// <summary>The keys in the palette's field (AX-10.4), which act there only, as axStart's do in its field.</summary>
    public const Keys NextMatch = Keys.Down;

    public const Keys PreviousMatch = Keys.Up;
    public const Keys FirstMatch = Keys.Control | Keys.Home;
    public const Keys LastMatch = Keys.Control | Keys.End;

    /// <summary>A menu's mark for the palette, kept in the item's Tag: left out, since its items are made as it opens; or refreshed before it is read, since its items change as it opens.</summary>
    private sealed record Mark(bool LeftOut, Action? Refresh);

    /// <summary>
    /// One item of the menu bar as every reader takes it (docs/axKit/app-kit.md AK-2.5): the palette, the AI tools'
    /// <c>axkit_window_commands</c> and <c>axkit_window_run</c>, and the tests. <paramref name="Path"/> is the menus from
    /// the bar down ("Session", "Open recent file").
    /// </summary>
    public sealed record MenuEntry(ToolStripMenuItem Item, string Name, IReadOnlyList<string> Path, string? Key, bool Checked)
    {
        /// <summary>The menus as one text, "Session, Open recent file", as the palette and the AI tools name them.</summary>
        public string Menu => string.Join(", ", Path);
    }

    /// <summary>What each item does beyond reading, for the AI tools (AK-2.5): set where the item is made.</summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ToolStripItem, object> Marks = new();

    /// <summary>The menus and items never offered to an AI tool (AK-2.6, exception 3), with everything under them.</summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ToolStripItem, object> Withheld = new();

    /// <summary>
    /// Gives an item its mark (AK-2.5): a command's own (<see cref="AxKitForm"/>'s Add), change for a setting, open for a
    /// recent file or window, read for an item that only says something. Returns the item.
    /// </summary>
    public static ToolStripMenuItem Marked(ToolStripMenuItem item, CommandMark mark)
    {
        Marks.AddOrUpdate(item, mark);
        return item;
    }

    /// <summary>
    /// An item's mark; act for one that was never given one, so that an AI tool asks the user before it runs anything
    /// whose effect no one stated (AK-2.6, exception 2).
    /// </summary>
    public static CommandMark MarkOf(ToolStripItem item) => Marks.TryGetValue(item, out var mark) ? (CommandMark)mark : CommandMark.Act;

    /// <summary>Withholds a menu or an item from the AI tools, with everything under it (AK-2.6, exception 3: the AI tools' own switches). Returns the item.</summary>
    public static ToolStripMenuItem NeverForAiTools(ToolStripMenuItem item)
    {
        Withheld.AddOrUpdate(item, true);
        return item;
    }

    /// <summary>Whether an AI tool may see and run the item: neither it nor a menu over it is withheld.</summary>
    public static bool ForAiTools(ToolStripMenuItem item) =>
        !Withheld.TryGetValue(item, out _) && !Owners(item).Any(owner => Withheld.TryGetValue(owner, out _));

    /// <summary>
    /// Every enabled item of the menu bar that does something, in menu order, the lists a menu fills as it opens filled
    /// first (AX-10.2): the one list the palette and the AI tools read (AK-2.5).
    /// </summary>
    public static List<MenuEntry> Entries(MenuStrip? menu)
    {
        var entries = new List<MenuEntry>();
        if (menu is not null)
        {
            Collect(menu.Items, [], entries);
        }

        return entries;
    }

    /// <summary>
    /// Marks a submenu whose items are made each time it opens and are slow to make (the drives, Open with): the palette
    /// leaves it out (AX-10.2). A quick list, Open recent and the Window menu's, is <see cref="RefreshedOnOpening"/> with
    /// its filling instead, so that the palette fills it before reading (2026-11-13). Returns the item.
    /// </summary>
    public static ToolStripMenuItem FilledOnOpening(ToolStripMenuItem item)
    {
        item.Tag = new Mark(true, null);
        return item;
    }

    /// <summary>
    /// Attaches <paramref name="refresh"/> to a menu whose items change their text or check mark as it opens (axFile's
    /// Undo named after what it would undo, axTerminal's Switch shell with the shell in use checked), and marks the
    /// menu so that the palette runs it before reading (AX-10.2; until 2026-10-23 the palette read such an item as it
    /// last stood). Returns the item.
    /// </summary>
    public static ToolStripMenuItem RefreshedOnOpening(ToolStripMenuItem item, Action refresh)
    {
        item.DropDownOpening += (_, _) => refresh();
        item.Tag = new Mark(false, refresh);
        return item;
    }

    /// <summary>
    /// Every enabled item of the menu bar that does something, in menu order, as an entry that clicks it (AX-10.2).
    /// A checked setting says its new state after the click through <paramref name="say"/>, "Word wrap, checked".
    /// Read while the menu bar is enabled: an item of a disabled bar counts as disabled.
    /// </summary>
    public static List<PaletteEntry> Commands(MenuStrip? menu, Action<string> say) =>
        Entries(menu).Select(entry => new PaletteEntry(Palette.Text(entry.Name, [.. entry.Path], entry.Key, entry.Checked), () =>
        {
            entry.Item.PerformClick();
            if (entry.Item.CheckOnClick)
            {
                say($"{entry.Name}, {(entry.Item.Checked ? "checked" : "not checked")}");
            }
        })).ToList();

    /// <summary>
    /// The menus that hold an item, from the bar down, as the palette names them: "Navigate, Next"; empty for an item
    /// of no menu bar's (a context menu's). For <c>axkit_window_commands</c> and <c>axkit_window_run</c> (docs/axKit/mcp.md MC-7.1).
    /// </summary>
    public static string MenuPath(ToolStripMenuItem item) =>
        string.Join(", ", Owners(item).Select(owner => MenuText.Plain(owner.Text ?? string.Empty)));

    /// <summary>
    /// Whether the palette would offer the item now (AX-10.2, MC-7.1): its menus refreshed as on opening, top down, then
    /// the item and every menu over it available and enabled, none of them filled on opening.
    /// </summary>
    public static bool Offered(ToolStripMenuItem item)
    {
        foreach (var owner in Owners(item))
        {
            (owner.Tag as Mark)?.Refresh?.Invoke();
        }

        // A refresh may have moved the item (the bar's Context menu, ContextBarMenu): its menus are read again after it,
        // and an item held in a menu off the bar (one Context is not showing now) is not offered.
        var owners = Owners(item);
        return (owners.Count == 0 || owners[0].Owner is MenuStrip)
            && owners.Append(item).All(menu => menu.Available && menu.Enabled && menu.Tag is not Mark { LeftOut: true });
    }

    /// <summary>The menu items over an item, the bar's first.</summary>
    private static List<ToolStripMenuItem> Owners(ToolStripMenuItem item)
    {
        var owners = new List<ToolStripMenuItem>();
        for (var owner = item.OwnerItem as ToolStripMenuItem; owner is not null; owner = owner.OwnerItem as ToolStripMenuItem)
        {
            owners.Insert(0, owner);
        }

        return owners;
    }

    private static void Collect(ToolStripItemCollection items, List<string> path, List<MenuEntry> entries)
    {
        foreach (ToolStripItem item in items)
        {
            if (item is not ToolStripMenuItem menuItem || !menuItem.Available || !menuItem.Enabled || menuItem.Tag is Mark { LeftOut: true })
            {
                continue;
            }

            var name = MenuText.Plain(menuItem.Text ?? string.Empty);
            if (menuItem.HasDropDownItems)
            {
                // What the menu would show on opening, before its items are read.
                (menuItem.Tag as Mark)?.Refresh?.Invoke();
                path.Add(name);
                Collect(menuItem.DropDownItems, path, entries);
                path.RemoveAt(path.Count - 1);
                continue;
            }

            // The bar's own items (File, Edit) only open their menus.
            if (path.Count == 0)
            {
                continue;
            }

            var key = menuItem.ShortcutKeyDisplayString ?? (menuItem.ShortcutKeys != Keys.None ? BundleKeys.Text(menuItem.ShortcutKeys) : null);
            entries.Add(new MenuEntry(menuItem, name, [.. path], key, menuItem.Checked));
        }
    }
}
