using System.Windows.Forms;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The menu bar as every reader takes it (docs/axKit/app-kit.md AK-2.5, AK-2.6; docs/axKit/repairs.json R-08 to R-10):
/// the entries of a built menu bar, what each item says of itself, and an entry found again by its name. On menu items
/// alone: no window is shown.
/// </summary>
public sealed class CommandPaletteTests
{
    private static (MenuStrip Bar, ToolStripMenuItem Session) Bar()
    {
        var bar = new MenuStrip();
        var session = new ToolStripMenuItem("&Session");
        bar.Items.Add(session);
        return (bar, session);
    }

    private static ToolStripMenuItem Item(string text) => new(text, null, (_, _) => { });

    [Fact]
    public void An_entry_has_its_plain_name_its_menus_and_its_key_and_the_bar_s_own_items_are_none()
    {
        var (bar, session) = Bar();
        var sub = new ToolStripMenuItem("Open &recent file");
        sub.DropDownItems.Add(Item("&1. notes.md (C:\\notes.md)"));
        session.DropDownItems.Add(new ToolStripMenuItem("Save &as...", null, (_, _) => { }) { ShortcutKeyDisplayString = "Ctrl+Shift+S" });
        session.DropDownItems.Add(sub);
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(new ToolStripMenuItem("&Disabled") { Enabled = false });

        var entries = CommandPalette.Entries(bar);
        Assert.Equal(["Save as", "1. notes.md (C:\\notes.md)"], entries.Select(entry => entry.Name));
        Assert.Equal("Session", entries[0].Menu);
        Assert.Equal("Ctrl+Shift+S", entries[0].Key);
        Assert.Equal("Session, Open recent file", entries[1].Menu);
    }

    [Fact]
    public void Every_refresh_attached_to_a_menu_runs_before_it_is_read_in_the_order_attached()
    {
        // R-09: the second refresh on a menu replaced the first, which then never ran for the palette.
        var (bar, session) = Bar();
        var first = new ToolStripMenuItem("Say &new line", null, (_, _) => { });
        var second = new ToolStripMenuItem("Sound on &headings", null, (_, _) => { });
        session.DropDownItems.Add(first);
        session.DropDownItems.Add(second);
        var order = new List<string>();
        CommandPalette.RefreshedOnOpening(session, () =>
        {
            order.Add("first");
            first.Checked = true;
        });
        CommandPalette.RefreshedOnOpening(session, () =>
        {
            order.Add("second");
            second.Checked = true;
        });

        var entries = CommandPalette.Entries(bar);
        Assert.Equal(["first", "second"], order);
        Assert.All(entries, entry => Assert.True(entry.Checked));
    }

    [Fact]
    public void A_list_made_as_its_menu_is_read_is_among_the_entries_and_its_empty_line_is_not()
    {
        var (bar, session) = Bar();
        var recent = new ToolStripMenuItem("Open &recent file");
        session.DropDownItems.Add(recent);
        var paths = new List<string>();
        // The menu holds a line from the start, as the apps' do (an empty submenu is no submenu to WinForms).
        void Fill() => RecentMenu.Fill(recent, paths, _ => true, path => path, "&No recent files", CommandMark.Open, _ => { });
        Fill();
        CommandPalette.RefreshedOnOpening(recent, Fill);

        // R-10: the line that says the list is empty runs nothing, and is no entry.
        Assert.Empty(CommandPalette.Entries(bar));

        paths.Add("C:\\a.md");
        var entry = Assert.Single(CommandPalette.Entries(bar));
        Assert.Equal("1. C:\\a.md (C:\\a.md)", entry.Name);
        Assert.Equal(CommandMark.Open, CommandPalette.MarkOf(entry.Item));
    }

    [Fact]
    public void A_recent_list_states_the_mark_its_caller_gives()
    {
        // R-01: a console's recent folder starts Claude Code or a shell again, and says act.
        var recent = new ToolStripMenuItem("Open &recent folder");
        RecentMenu.Fill(recent, ["C:\\proj"], _ => true, path => path, "&No recent folders", CommandMark.Act, _ => { });
        Assert.Equal(CommandMark.Act, CommandPalette.MarkOf(recent.DropDownItems[0]));
    }

    [Fact]
    public void An_item_that_states_no_mark_is_act_and_one_that_states_it_keeps_it()
    {
        var plain = Item("&Export");
        Assert.False(CommandPalette.StatesMark(plain));
        Assert.Equal(CommandMark.Act, CommandPalette.MarkOf(plain));
        var read = CommandPalette.Marked(Item("&Version"), CommandMark.Read);
        Assert.True(CommandPalette.StatesMark(read));
        Assert.Equal(CommandMark.Read, CommandPalette.MarkOf(read));
    }

    [Fact]
    public void A_withheld_menu_withholds_everything_under_it_from_the_AI_tools_and_nothing_from_the_palette()
    {
        var (bar, session) = Bar();
        var tools = CommandPalette.NeverForAiTools(new ToolStripMenuItem("A&I tools"));
        var run = Item("Let AI tools r&un commands");
        tools.DropDownItems.Add(run);
        var save = Item("&Save");
        session.DropDownItems.Add(tools);
        session.DropDownItems.Add(save);

        Assert.False(CommandPalette.ForAiTools(run));
        Assert.True(CommandPalette.ForAiTools(save));
        Assert.Equal(2, CommandPalette.Entries(bar).Count);
    }

    [Fact]
    public void A_menu_marked_as_no_entry_is_left_out_with_everything_under_it()
    {
        var (bar, session) = Bar();
        var drives = CommandPalette.NoEntry(new ToolStripMenuItem("&Drives"));
        drives.DropDownItems.Add(Item("&This PC"));
        session.DropDownItems.Add(drives);
        session.DropDownItems.Add(Item("&Save"));
        Assert.Equal(["Save"], CommandPalette.Entries(bar).Select(entry => entry.Name));
    }

    [Fact]
    public void An_entry_is_found_again_by_its_name_and_menus_after_its_list_was_made_anew_and_not_once_it_is_gone()
    {
        // R-08: what runs later is the live item of that name, never the one found before.
        var (bar, session) = Bar();
        var recent = new ToolStripMenuItem("Open &recent file");
        session.DropDownItems.Add(recent);
        var paths = new List<string> { "C:\\a.md" };
        void Fill() => RecentMenu.Fill(recent, paths, _ => true, path => path, "&No recent files", CommandMark.Open, _ => { });
        Fill();
        CommandPalette.RefreshedOnOpening(recent, Fill);

        var found = Assert.Single(CommandPalette.Entries(bar));
        var again = CommandPalette.Find(bar, found.Name, found.Menu);
        Assert.NotNull(again);
        Assert.NotSame(found.Item, again.Item);
        Assert.NotNull(again.Item.Owner);

        paths.Clear();
        Assert.Null(CommandPalette.Find(bar, found.Name, found.Menu));
        Assert.Null(CommandPalette.Find(bar, found.Name, "Session"));
    }

    [Fact]
    public void A_palette_line_runs_the_live_item_and_says_so_when_it_is_gone()
    {
        var (bar, session) = Bar();
        var recent = new ToolStripMenuItem("Open &recent file");
        session.DropDownItems.Add(recent);
        var paths = new List<string> { "C:\\a.md" };
        var opened = new List<string>();
        void Fill() => RecentMenu.Fill(recent, paths, _ => true, path => path, "&No recent files", CommandMark.Open, opened.Add);
        Fill();
        CommandPalette.RefreshedOnOpening(recent, Fill);
        var said = new List<string>();

        var line = Assert.Single(CommandPalette.Commands(bar, said.Add));
        // The list is made anew before the line runs, as an AI tool's reading or the menu's opening does.
        Fill();
        line.Run();
        Assert.Equal(["C:\\a.md"], opened);

        paths.Clear();
        Fill();
        line.Run();
        Assert.Equal(["C:\\a.md"], opened);
        Assert.Equal(["1. C:\\a.md (C:\\a.md) is not available now"], said);
    }
}
