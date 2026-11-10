using AxHelp.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxHelp;

/// <summary>
/// axHelp's window (SPEC.md HE-2): a menu bar and one list, on the bundle's window base (<see cref="AxKitForm"/>),
/// which holds the notices, the fonts, the window's place, find and the update flow. A first draft from
/// tools/new-app.ps1; axCalc's <c>CalcForm</c> is the small example of a window grown from here.
/// </summary>
public sealed class HelpForm : AxKitForm
{
    private readonly MenuStrip _menu = new();
    private readonly AxKitList _list = new();
    private readonly IReadOnlyList<string> _lines;

    public HelpForm(string? path, HelpSettings settings, string? settingsError)
        : base(AxHelpApp.Name, settings, HelpSettings.DefaultPath, settingsError)
    {
        _lines = HelpLines.For(path);
        Text = HelpLines.Title(path) + " - " + AxHelpApp.Name;

        // ---- menus and keys (HE-7): one command list (docs/axkit/app-kit.md AK-2), each key from the tables ----
        var first = new ToolStripMenuItem("&Help");
        first.DropDownItems.Add(CreateExitItem());

        // Options, as the other windows' (AX-6): the window's own submenus and items first, then Text, General, Developer.
        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "axHelp &resources",
            [("&Adding an app to axKit", "https://github.com/KyleKeane/AxKit/blob/main/docs/axkit/adding-an-app.md")]);

        // The bundle's axKit menu (AX-9) sits before Help in every window.
        _menu.Items.AddRange([first, options, CreateAxKitMenu(), help]);
        MainMenuStrip = _menu;

        AddKey(new Command("Find in the list", BundleKeys.Find, () => ShowFindInList(_list, () => _lines)) { Mark = CommandMark.Read });
        AddKey(new Command("Find next", BundleKeys.FindNext, () => FindInList(_list, () => _lines, 1)) { Mark = CommandMark.Read });
        AddKey(new Command("Find previous", BundleKeys.FindPrevious, () => FindInList(_list, () => _lines, -1)) { Mark = CommandMark.Read });

        // ---- the list (HE-2.1) ----
        _list.IntegralHeight = false;
        _list.Dock = DockStyle.Fill;
        _list.TabIndex = 0;
        _list.AccessibleName = "Help";
        _list.Items.AddRange(_lines.Select(line => (object)line).ToArray());
        _list.SelectedIndex = 0;

        // Docked from the last control to the first: the menu takes the top, the list (or the notice over it) the rest.
        Controls.AddRange([Overlay, _list, _menu]);

        ApplyTextFont();
        RestoreWindow();
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice() => _list.Visible = false;

    protected override void ShowAfterNotice() => _list.Visible = true;

    protected override Control FocusTarget => _list;

    protected override void ApplyFont(Font font) => _list.Font = font;

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxHelp];

    protected override void OnShown(EventArgs e)
    {
        if (!NoticeOpen)
        {
            _list.Select();
        }

        base.OnShown(e);
    }
}
