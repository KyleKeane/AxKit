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
    /// Marks a submenu whose items are made each time it opens (Open recent, Open windows, the drives): the palette
    /// leaves it out, since what it holds is known only once it opens (AX-10.2). Returns the item.
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
    public static List<PaletteEntry> Commands(MenuStrip? menu, Action<string> say)
    {
        var entries = new List<PaletteEntry>();
        if (menu is not null)
        {
            Collect(menu.Items, [], entries, say);
        }

        return entries;
    }

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

    private static void Collect(ToolStripItemCollection items, List<string> path, List<PaletteEntry> entries, Action<string> say)
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
                Collect(menuItem.DropDownItems, path, entries, say);
                path.RemoveAt(path.Count - 1);
                continue;
            }

            // The bar's own items (File, Edit) only open their menus.
            if (path.Count == 0)
            {
                continue;
            }

            var key = menuItem.ShortcutKeyDisplayString ?? (menuItem.ShortcutKeys != Keys.None ? BundleKeys.Text(menuItem.ShortcutKeys) : null);
            var text = Palette.Text(name, path, key, menuItem.Checked);
            entries.Add(new PaletteEntry(text, () =>
            {
                menuItem.PerformClick();
                if (menuItem.CheckOnClick)
                {
                    say($"{name}, {(menuItem.Checked ? "checked" : "not checked")}");
                }
            }));
        }
    }
}
