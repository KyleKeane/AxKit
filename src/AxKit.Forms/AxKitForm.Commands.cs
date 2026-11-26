using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Core.Mcp;

namespace AxKit.Forms;

public abstract partial class AxKitForm
{
    // ---- the window's commands as data (docs/axKit/user-workflows.md WF-2.3), for the automation channel ----

    /// <summary>
    /// The window's commands as data, for the automation channel's <c>commands data</c> (docs/axKit/user-workflows.md
    /// WF-2.3, section 5.2): the menu bar's entries as the palette and the AI tools read them
    /// (<see cref="CommandPalette.Entries"/>), each with its key as a chord (<see cref="CommandPalette.KeyOf"/>,
    /// <see cref="BundleKeys.Chord"/>), its mark and its checked state as the AI tools' pipe gives them, and whether
    /// the AI tools may see it (never in axPass or axClip, <see cref="CommandListing.Ai"/>); then every command of the
    /// window's list that has a key, with whether its key acts now and whether it is a key with no menu item
    /// (<see cref="AddKey"/>). <see cref="CommandListing.Rows"/> folds the keys into the rows and says which chord
    /// would run each now.
    ///
    /// While a notice shows, the menu bar is disabled and has no entries, and no command's key acts: the window base's
    /// key handling runs none of the list then (<see cref="ProcessCmdKey"/>), so each is given as not acting whatever
    /// its <see cref="Command.When"/> says. While the key test runs (AX-8.12) no chord acts either: every row's
    /// <c>now</c> is empty and the listing names the key test where a notice's title stands.
    /// </summary>
    /// <summary>What the listing gives in a notice's place while the key test runs.</summary>
    private const string KeyTestTitle = "Key test";

    internal WindowCommands CommandRows()
    {
        var verb = AppVerb;
        var menus = new List<MenuCommand>();
        foreach (var entry in CommandPalette.Entries(MainMenuStrip))
        {
            menus.Add(new MenuCommand(
                entry.Name,
                entry.Menu,
                BundleKeys.Chord(entry.KeyData),
                // The key as the menu shows it, without the full stop that follows a number-row key for NVDA (AX-8.5).
                (entry.Key ?? string.Empty).TrimEnd('.'),
                CommandPalette.MarkOf(entry.Item).ToString().ToLowerInvariant(),
                entry.Item.CheckOnClick || entry.Checked ? entry.Checked : null,
                CommandListing.Ai(CommandPalette.ForAiTools(entry.Item), verb)));
        }

        var keys = new List<KeyCommand>();
        foreach (var command in _commands)
        {
            if (command.Key == Keys.None)
            {
                continue;
            }

            keys.Add(new KeyCommand(
                MenuText.Plain(command.Label),
                BundleKeys.Chord(command.Key),
                BundleKeys.Text(command.Key).TrimEnd('.'),
                command.Mark.ToString().ToLowerInvariant(),
                !NoticeOpen && (command.When?.Invoke() ?? true),
                _keyOnly.Any(known => ReferenceEquals(known, command))));
            if (command.Key == ContextMenuOfWindows)
            {
                // AX-8.15: Ctrl+Period runs what Shift+F10 runs (ProcessCmdKey), so it is a further key of that command.
                keys.Add(keys[^1] with { Key = BundleKeys.Chord(BundleKeys.ContextMenu), KeyText = BundleKeys.Text(BundleKeys.ContextMenu), KeyOnly = true });
            }
        }

        // AX-8.15: Ctrl+Slash, the base's further key of Ask about this window, which is no command of the list.
        keys.Add(new KeyCommand(MenuText.Plain(AskAboutThisWindowLabel), BundleKeys.Chord(BundleKeys.AskClaudeAlso), BundleKeys.Text(BundleKeys.AskClaudeAlso), "act", !NoticeOpen, KeyOnly: true));

        var rows = CommandListing.Rows(menus, keys);
        if (KeyTestRunning)
        {
            // AX-8.12: in the key test a key is named and nothing is run, so no chord acts now, and the listing says
            // that the window holds its commands, as it does for a notice. A script's do step then waits instead of
            // choosing the menu item, and the converter takes no tested key for a command (R-60).
            rows = [.. rows.Select(row => row with { Now = string.Empty })];
            return new WindowCommands(AppName, KeyTestTitle, rows);
        }

        return new WindowCommands(AppName, NoticeOpen ? Overlay.Shown?.Title : null, rows);
    }

    /// <summary>
    /// Runs one menu item for the automation channel's <c>choose</c> (docs/axKit/user-workflows.md section 5.2): the
    /// entry of exactly that name under exactly those menus as the bar stands now (<see cref="CommandPalette.Find"/>),
    /// clicked as the command palette clicks it (<see cref="ClickFor"/>): a command says what it says, and a setting
    /// that is a check item has its new state said as the palette says it ("Check for updates at start, checked").
    /// Null when the item was clicked; otherwise why not: the key test runs (the gate's condition 2, held since
    /// 2026-11-25, docs/axKit/command-language.md CL-4.8), or the menus do not offer such an item now (it is gone,
    /// disabled, or a notice holds the window).
    /// </summary>
    internal string? Choose(string label, string menus)
    {
        if (CommandGate.Window(CommandOrigin.Choose, new CommandState { KeyTestRunning = KeyTestRunning }) == CommandRefusal.KeyTest)
        {
            return $"the window is in its key test: nothing is carried out until it ends, so {label} was not chosen";
        }

        if (CommandPalette.Find(MainMenuStrip, label, menus) is not { } entry)
        {
            return $"no menu item {label} in {menus} now";
        }

        ClickFor(CommandOrigin.Choose, entry.Item, entry.Name);
        return null;
    }

    // ---- the one click (docs/axKit/command-language.md CL-4.7) ----

    /// <summary>
    /// The menu bar's entries as the palette's lines (<see cref="CommandPalette.Commands"/>), each clicking its item
    /// for <paramref name="origin"/>: <see cref="CommandOrigin.Palette"/> for the command palette,
    /// <see cref="CommandOrigin.Choose"/> for the automation channel's <c>commands</c> and its older <c>menu</c> step.
    /// </summary>
    internal List<PaletteEntry> MenuLines(CommandOrigin origin) =>
        CommandPalette.Commands(MainMenuStrip, text => SayFor(origin, EventKind.Said, text), (item, name) => ClickFor(origin, item, name));

    /// <summary>
    /// Whether the command that runs now was clicked for an AI tool (MC-7.5): true only while its click runs. For a
    /// command that then asks the user more than it would ask him for his own press (axTask's actions that start a
    /// program, docs/axTask/forms-framework.md AF-7.5); a command that goes on later takes the answer with it.
    /// </summary>
    protected bool ClickedForAiTool { get; private set; }

    /// <summary>
    /// The one place where axKit itself clicks a menu item (CL-4.7): for the command palette's line, for the
    /// automation channel's <c>choose</c> and <c>menu</c>, and for an AI tool's run. Each caller has made its own
    /// checks before (<see cref="CommandGate"/> says which belong to which origin); here are the click and what goes
    /// with it for that origin, no more than each did before 2026-11-16: for an AI tool the words that say who ran
    /// it, ahead of the click so that they join what the command says, and the chime of a window behind (MC-7.5);
    /// for the palette and the channel a check item's new state after the click (<see cref="MenuText.CheckSaid"/>),
    /// since no menu shows it. The click is written into the recent actions by the menu that holds the item, before
    /// the item's own click, whoever clicks (<see cref="CommandPalette"/>'s <c>WritePicks</c>; a command of the
    /// window's list by <c>Run</c>). The user's own choice in a menu is WinForms' click and does not pass here.
    /// </summary>
    private void ClickFor(CommandOrigin origin, ToolStripMenuItem item, string name, string? client = null, string? text = null)
    {
        if (origin == CommandOrigin.AiTool)
        {
            AnnounceBefore(McpRun.Said(client ?? string.Empty, name, text));
            // One chime and flash for a burst of runs, not one per run.
            ChimeForMcp();
        }

        ClickedForAiTool = origin == CommandOrigin.AiTool;
        try
        {
            item.PerformClick();
        }
        finally
        {
            ClickedForAiTool = false;
        }

        if (origin != CommandOrigin.AiTool && item.CheckOnClick)
        {
            SayFor(origin, EventKind.Changed, MenuText.CheckSaid(name, item.Checked));
        }
    }

    /// <summary>
    /// What a click for an origin says beside the command's own words, as each said it before: the palette through
    /// the window's <see cref="Announce(EventKind, string, bool)"/>, the automation channel straight on the window
    /// (<see cref="EventDoor.SayOn"/>), each raised at the door with its kind.
    /// </summary>
    private void SayFor(CommandOrigin origin, EventKind kind, string text)
    {
        if (origin == CommandOrigin.Palette)
        {
            Announce(kind, text, true);
        }
        else
        {
            EventDoor.SayOn(kind, this, text, true);
        }
    }
}
