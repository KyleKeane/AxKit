using System.Runtime.CompilerServices;
using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The window's menu bar as every reader takes it (docs/axKit/app-kit.md AK-2.5, docs/axKit/SPEC.md AX-10): the entries
/// the command palette, the AI tools and the automated checks all read, and what each menu item says of itself for
/// them. The notice and its keys are the window base's (<see cref="AxKitForm"/>); the lines and the search are
/// <see cref="Palette"/>, tested.
///
/// What an item says of itself is one record per item (<see cref="ItemInfo"/>), kept beside the item and never in its
/// Tag: its mark, whether it is withheld from the AI tools, whether it is no entry at all, and what refreshes it before
/// it is read. Until 2026-11-13 these were three stores, and the Tag held one refresh, which a second one replaced
/// (docs/axKit/repairs.json R-09).
/// </summary>
public static class CommandPalette
{
    /// <summary>The keys in the palette's field (AX-10.4), which act there only, as axStart's do in its field.</summary>
    public const Keys NextMatch = Keys.Down;

    public const Keys PreviousMatch = Keys.Up;
    public const Keys FirstMatch = Keys.Control | Keys.Home;
    public const Keys LastMatch = Keys.Control | Keys.End;

    /// <summary>
    /// One item of the menu bar as every reader takes it (AK-2.5). <paramref name="Path"/> is the menus from the bar
    /// down ("Session", "Open recent file"). An entry is known by its name and its menus: the item itself may be made
    /// again whenever its menu is refreshed, so a reader that acts later finds the entry again (<see cref="Find"/>).
    /// </summary>
    public sealed record MenuEntry(ToolStripMenuItem Item, string Name, IReadOnlyList<string> Path, string? Key, bool Checked)
    {
        /// <summary>The menus as one text, "Session, Open recent file", as the palette and the AI tools name them.</summary>
        public string Menu => string.Join(", ", Path);
    }

    /// <summary>What one menu item says of itself for the readers.</summary>
    private sealed class ItemInfo
    {
        /// <summary>What choosing it does beyond reading; null when nobody said, which the AI tools take as act.</summary>
        public CommandMark? Mark { get; set; }

        /// <summary>Never offered to an AI tool, nor anything under it (AK-2.6, exception 3).</summary>
        public bool Withheld { get; set; }

        /// <summary>No entry, nor anything under it: a list slow to fill, or the line that says a list is empty.</summary>
        public bool NoEntry { get; set; }

        /// <summary>What makes the menu's items current, run before it is read, in the order they were attached.</summary>
        public List<Action> Refreshes { get; } = [];
    }

    private static readonly ConditionalWeakTable<ToolStripItem, ItemInfo> Infos = [];

    private static ItemInfo? InfoOf(ToolStripItem item) => Infos.TryGetValue(item, out var info) ? info : null;

    /// <summary>
    /// Gives an item its mark (AK-2.5): a command's own (<see cref="AxKitForm"/>'s Add), change for a setting, open for a
    /// recent file or window, read for an item that only says something, act for what starts a program or reaches past
    /// the window. Returns the item.
    /// </summary>
    public static ToolStripMenuItem Marked(ToolStripMenuItem item, CommandMark mark)
    {
        Infos.GetOrCreateValue(item).Mark = mark;
        return item;
    }

    /// <summary>
    /// An item's mark; act for one that was never given one, so that an AI tool asks the user before it runs anything
    /// whose effect no one stated (AK-2.6, exception 2).
    /// </summary>
    public static CommandMark MarkOf(ToolStripItem item) => InfoOf(item)?.Mark ?? CommandMark.Act;

    /// <summary>Whether the item states its mark: for the check that every entry of a built window does.</summary>
    public static bool StatesMark(ToolStripItem item) => InfoOf(item)?.Mark is not null;

    /// <summary>Withholds a menu or an item from the AI tools, with everything under it (AK-2.6, exception 3: the AI tools' own switches). Returns the item.</summary>
    public static ToolStripMenuItem NeverForAiTools(ToolStripMenuItem item)
    {
        Infos.GetOrCreateValue(item).Withheld = true;
        return item;
    }

    /// <summary>Whether an AI tool may see and run the item: neither it nor a menu over it is withheld.</summary>
    public static bool ForAiTools(ToolStripMenuItem item) =>
        InfoOf(item)?.Withheld != true && !Owners(item).Any(owner => InfoOf(owner)?.Withheld == true);

    /// <summary>
    /// Keeps an item, and everything under it, out of the entries (AX-10.2): a submenu whose items are made as it opens
    /// and are slow to make (the drives, Open with), or the one line that says a list is empty, which does nothing.
    /// The menu itself and its keys work as ever. Returns the item.
    /// </summary>
    public static ToolStripMenuItem NoEntry(ToolStripMenuItem item)
    {
        Infos.GetOrCreateValue(item).NoEntry = true;
        return item;
    }

    /// <summary>
    /// Attaches <paramref name="refresh"/> to a menu whose items are made or change as it opens (Open recent, the Window
    /// menu's lists, axFile's Undo named after what it would undo, a check read from a file), so that it runs as the
    /// menu opens and before any reader reads the menu (AX-10.2). A menu may have several, each attached where its
    /// items are made; all run, in order. Returns the item.
    /// </summary>
    public static ToolStripMenuItem RefreshedOnOpening(ToolStripMenuItem item, Action refresh)
    {
        item.DropDownOpening += (_, _) => refresh();
        Infos.GetOrCreateValue(item).Refreshes.Add(refresh);
        return item;
    }

    /// <summary>
    /// Every enabled item of the menu bar that does something, in menu order, each menu refreshed first as on opening
    /// (AX-10.2): the one list the palette and the AI tools read (AK-2.5). Read while the menu bar is enabled: an item
    /// of a disabled bar counts as disabled, so a window showing a notice has no entries.
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
    /// The entry of that name under those menus as the bar stands now, or null: how a reader that found an entry in one
    /// moment acts on it in another (docs/axKit/repairs.json R-08). The item found before may have been made again by
    /// a refresh, renamed (axFile's Undo) or disabled since.
    /// </summary>
    public static MenuEntry? Find(MenuStrip? menu, string name, string menus) =>
        Entries(menu).FirstOrDefault(entry => entry.Name == name && entry.Menu == menus);

    /// <summary>
    /// The entries as the palette's lines, each clicking its item (AX-10.2). A checked setting says its new state after
    /// the click through <paramref name="say"/>, "Word wrap, checked". An entry whose item was made again since the
    /// palette read it is found again by its name; one that is gone says so.
    /// </summary>
    public static List<PaletteEntry> Commands(MenuStrip? menu, Action<string> say) =>
        Entries(menu).Select(entry => new PaletteEntry(Palette.Text(entry.Name, [.. entry.Path], entry.Key, entry.Checked), () =>
        {
            // An item no longer in a menu was replaced when its list was filled again.
            var item = entry.Item.Owner is not null ? entry.Item : Find(menu, entry.Name, entry.Menu)?.Item;
            if (item is null)
            {
                say($"{entry.Name} is not available now");
                return;
            }

            item.PerformClick();
            if (item.CheckOnClick)
            {
                say($"{entry.Name}, {(item.Checked ? "checked" : "not checked")}");
            }
        })).ToList();

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
        // A copy: a refresh below may make a menu's items again while they are walked.
        foreach (var item in items.Cast<ToolStripItem>().ToList())
        {
            if (item is not ToolStripMenuItem menuItem || !menuItem.Available || !menuItem.Enabled)
            {
                continue;
            }

            var info = InfoOf(menuItem);
            if (info?.NoEntry == true)
            {
                continue;
            }

            // What the menu would show on opening, before its items are read; a menu the user has open is read as it
            // stands, since making its items again would take the one the user is on (R-10).
            if (info is not null && !menuItem.DropDown.Visible)
            {
                foreach (var refresh in info.Refreshes)
                {
                    refresh();
                }
            }

            var name = MenuText.Plain(menuItem.Text ?? string.Empty);
            if (menuItem.HasDropDownItems)
            {
                path.Add(name);
                Collect(menuItem.DropDownItems, path, entries);
                path.RemoveAt(path.Count - 1);
                continue;
            }

            // The bar's own items (Session, Actions) only open their menus.
            if (path.Count == 0)
            {
                continue;
            }

            var key = menuItem.ShortcutKeyDisplayString ?? (menuItem.ShortcutKeys != Keys.None ? BundleKeys.Text(menuItem.ShortcutKeys) : null);
            entries.Add(new MenuEntry(menuItem, name, [.. path], key, menuItem.Checked));
        }
    }
}
