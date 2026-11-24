using System.Text;

namespace AxKit.Core;

/// <summary>
/// The part of a window's F1 text that no one writes (docs/axKit/SPEC.md AX-7.2a; the maintainer, 2026-11-23: "make
/// sure that there is only one source of truth for the hotkeys and that everything is filling its code and hints from
/// those"): every command of the window that has a key, made from the window's own command list as it stands
/// (<see cref="CommandListing"/>), by menu, then the keys with no menu item. A key changed in its table is changed
/// here at once; a key that a hand-written line of F1 forgets is still here. Pure and tested in
/// <c>tests/AxKit.Tests/KeyReferenceTests</c>.
/// </summary>
public static class KeyReference
{
    /// <summary>The section's heading, which NVDA's heading keys reach in the F1 notice.</summary>
    public const string Heading = "## Every key of this window";

    /// <summary>The line under the heading.</summary>
    public const string Intro = "  Made from the window's own commands as they are now: by menu, then the keys with no menu item.";

    /// <summary>The heading of the keys with no menu item.</summary>
    public const string KeysAlone = "### Keys with no menu item";

    /// <summary>
    /// The section: a heading per top menu that has a command with a key ("### File menu"), each command a line "Ctrl+O:
    /// Open", a command in a submenu with the submenu after it ("Ctrl+H: Heading (Next)"); then <see cref="KeysAlone"/>.
    /// A row with no key is left out, and a command listed twice (a menu item and its key) once. Every line ends with a
    /// line feed.
    /// </summary>
    public static string Section(IReadOnlyList<ListedCommand> rows)
    {
        var text = new StringBuilder();
        text.Append(Heading).Append('\n').Append(Intro).Append('\n');
        var withKey = rows.Where(row => row.KeyText.Length > 0).ToList();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var menu in withKey.Where(row => !row.KeyOnly && row.Menu.Length > 0).GroupBy(row => TopMenu(row.Menu)))
        {
            text.Append("### ").Append(menu.Key).Append(" menu\n");
            foreach (var row in menu)
            {
                if (seen.Add(row.KeyText + "\n" + row.Label))
                {
                    text.Append("  ").Append(row.KeyText).Append(": ").Append(Named(row)).Append('\n');
                }
            }
        }

        var alone = withKey.Where(row => (row.KeyOnly || row.Menu.Length == 0) && seen.Add(row.KeyText + "\n" + row.Label)).ToList();
        if (alone.Count > 0)
        {
            text.Append(KeysAlone).Append('\n');
            foreach (var row in alone)
            {
                text.Append("  ").Append(row.KeyText).Append(": ").Append(row.Label).Append('\n');
            }
        }

        return text.ToString();
    }

    /// <summary>The top menu of a row's menus: "Navigation" of "Navigation, Next".</summary>
    private static string TopMenu(string menus)
    {
        var comma = menus.IndexOf(", ", StringComparison.Ordinal);
        return comma < 0 ? menus : menus[..comma];
    }

    /// <summary>A row's label, with the submenus under its top menu after it: "Heading (Next)".</summary>
    private static string Named(ListedCommand row)
    {
        var comma = row.Menu.IndexOf(", ", StringComparison.Ordinal);
        return comma < 0 ? row.Label : $"{row.Label} ({row.Menu[(comma + 2)..]})";
    }
}
