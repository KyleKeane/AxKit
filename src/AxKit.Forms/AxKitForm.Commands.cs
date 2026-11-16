using AxKit.Core;

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
        }

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
    /// clicked as the command palette clicks it: a command says what it says, and a setting that is a check item has
    /// its new state said as the palette says it ("Check for updates at start, checked"). Null when the item was
    /// clicked; otherwise why not: the menus do not offer such an item now (it is gone, disabled, or a notice holds
    /// the window).
    /// </summary>
    internal string? Choose(string label, string menus)
    {
        if (CommandPalette.Find(MainMenuStrip, label, menus) is not { } entry)
        {
            return $"no menu item {label} in {menus} now";
        }

        entry.Item.PerformClick();
        if (entry.Item.CheckOnClick)
        {
            Announcer.Raise(this, $"{entry.Name}, {(entry.Item.Checked ? "checked" : "not checked")}", true);
        }

        return null;
    }
}
