namespace AxKit.Forms;

/// <summary>
/// The bar's Context menu (docs/axKit/SPEC.md AX-9.11, B-19; 2026-11-11): what acts on the thing the user is on, its
/// items those of the window's menu for that thing (axTask's List, Item or Field; axProcess's Process, Service or
/// Startup by view). The window builds those menus as before, never places them on the bar, and hands them here;
/// <see cref="Fill"/> moves the items of the one <c>current</c> names into Context and puts the ones shown before back
/// into their own menu. Those menus stay off the bar and disabled, so that an item held there is never offered: every
/// reader reads the bar alone (<see cref="CommandPalette.Entries"/>), which refreshes Context first. Context refreshes
/// as it opens, as the palette reads it and as an AI tool asks, so that it always follows the focus. It is never
/// empty: with nothing to act on it holds one enabled item that says so.
/// </summary>
public sealed class ContextBarMenu
{
    /// <summary>The letter of Context on the bar (C), for Shift+F10, the Applications key and a right click.</summary>
    public const char Letter = 'C';

    private readonly Func<ToolStripMenuItem?> _current;
    private readonly Func<string> _nothingText;

    /// <summary>The menu that holds Context's one item while there is nothing to act on.</summary>
    private readonly ToolStripMenuItem _nothing = new() { Enabled = false };

    private readonly ToolStripMenuItem _nothingItem;

    /// <summary>The menu whose items Context holds now; null before the first fill.</summary>
    private ToolStripMenuItem? _shown;

    /// <param name="current">The menu for what the user is on now, one of <paramref name="menus"/>; null for nothing to act on.</param>
    /// <param name="nothing">What Context's one item says then, and says again when chosen: "Nothing selected".</param>
    /// <param name="say">The window's announcement, for that item.</param>
    /// <param name="menus">Every menu whose items Context may hold; none of them is placed on the bar.</param>
    public ContextBarMenu(Func<ToolStripMenuItem?> current, Func<string> nothing, Action<string> say, params ToolStripMenuItem[] menus)
    {
        _current = current;
        _nothingText = nothing;
        foreach (var menu in menus)
        {
            // Off the bar and disabled: nothing reads or opens it, only Context, which takes its items.
            menu.Enabled = false;
        }

        // Says only that there is nothing to act on.
        _nothingItem = CommandPalette.Marked(new ToolStripMenuItem(string.Empty, null, (_, _) => say(nothing())), CommandMark.Read);
        _nothing.DropDownItems.Add(_nothingItem);
        Menu = CommandPalette.RefreshedOnOpening(new ToolStripMenuItem("&Context"), Fill);
        Fill();
    }

    /// <summary>The bar's Context menu.</summary>
    public ToolStripMenuItem Menu { get; }

    /// <summary>The items of the menu for what the user is on moved into Context, those shown before put back; nothing moves when it is the same menu.</summary>
    public void Fill()
    {
        var current = _current() ?? _nothing;
        _nothingItem.Text = "&" + _nothingText();
        if (ReferenceEquals(current, _shown))
        {
            return;
        }

        var shownItems = Menu.DropDownItems.Cast<ToolStripItem>().ToArray();
        _shown?.DropDownItems.AddRange(shownItems);
        Menu.DropDownItems.AddRange(current.DropDownItems.Cast<ToolStripItem>().ToArray());
        _shown = current;
    }
}
