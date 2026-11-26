using System.Diagnostics;
using System.Runtime.InteropServices;
using AxKit.Core;
using static AxKit.Forms.User32;

namespace AxKit.Forms;

/// <summary>
/// The Window menu every window has (docs/axKit/SPEC.md AX-9; the axKit menu until 5.7.0, when the user asked for
/// the bundle's menus by intent, Window, axKit and Help, 2026-11-10): Open windows
/// (the other windows of the bundle's apps, most recently used first), Recently closed (the bundle's list, AX-9.4)
/// and Close all windows. The lists are filled each time the menu opens, the windows from what the system lists in Z
/// order, which is Alt+Tab's order, so nothing about windows is stored. The lists and labels are
/// <see cref="AxKitWindows"/> and <see cref="RecentlyClosed"/>, tested; the rest is checked with NVDA. The statics
/// start a window of any app (<see cref="Start"/>, <see cref="StartWith"/>) and list and activate the bundle's windows
/// (<see cref="OpenWindows"/>, <see cref="Activate"/>) for every caller. Open windows by type (one submenu per app) was
/// here until 5.6.0; the command palette, the reports, the update items and About moved to Help in 5.7.0.
/// </summary>
public sealed class WindowMenu : ToolStripMenuItem
{
    private readonly AxKitForm _owner;
    private readonly ToolStripMenuItem _recentlyClosed = new("&Recently closed");
    private readonly ToolStripMenuItem _openWindows = new("&Open windows");

    /// <param name="owner">The window: its handle is the one checked in the lists and closed last by Close all windows.</param>
    public WindowMenu(AxKitForm owner)
        : base("&Window")
    {
        _owner = owner;
        // The lists made as the menu opens, and again before the command palette reads it, so that an open window or a
        // recently closed file is one search away (AX-10.2, 2026-11-13; the palette left them out before).
        DropDownItems.Add(_openWindows);
        DropDownItems.Add(_recentlyClosed);
        DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Close all windows", null, (_, _) => CloseAll()), CommandMark.Act));
        // AX-7.10: this window's status bar, said by axKit itself; the bundle's key, shown here, held as a value for
        // the window's list of its commands (docs/axKit/user-workflows.md WF-2.1) and handled by the base.
        DropDownItems.Add(new ToolStripSeparator());
        DropDownItems.Add(CommandPalette.Keyed(CommandPalette.Marked(new ToolStripMenuItem("&Say the status", null, (_, _) => owner.SayStatus())
        {
            ShortcutKeyDisplayString = BundleKeys.Text(BundleKeys.SayStatus),
        }, CommandMark.Read), BundleKeys.SayStatus));
        CommandPalette.RefreshedOnOpening(this, Fill);
        Fill();
    }

    /// <summary>
    /// The axKit menu's Apps and Access layers (AX-9.2; the maintainer, 2026-11-23: "we should split axKit Apps and
    /// axKit Access Layers in the menus ... these are very importantly different things", then "you should move the
    /// apps and access layets into the axkit menu rather than the window menu"): a new window of a standalone app
    /// under Apps, of an access layer under Access layers, each by group in the order of its function keys
    /// (AX-8.18 rule 6).
    /// </summary>
    public static (ToolStripMenuItem Apps, ToolStripMenuItem Layers) CreateAppMenus()
    {
        var apps = new ToolStripMenuItem("&Apps");
        AddApps(apps.DropDownItems, Dispatch.Apps.Where(app => !app.IsAccessLayer), NewWindowItem);
        var layers = new ToolStripMenuItem("Access &layers");
        AddApps(layers.DropDownItems, Dispatch.Apps.Where(app => app.IsAccessLayer), NewWindowItem);
        return (apps, layers);
    }

    /// <summary>An app's item under Apps or Access layers, a new window of it, with the app's key as it holds (AX-8.18 rule 6, <see cref="AppLaunchKeys.Current"/>, the user's file over the defaults) shown, which the base handles in every window; no key shown for an app the file took it from.</summary>
    private static ToolStripMenuItem NewWindowItem(App app)
    {
        var item = CommandPalette.Marked(new ToolStripMenuItem(AxKitWindows.AppLabel(app), null, (_, _) => Start(app.Verb, null)), OpenMark(app.Verb));
        if (AppLaunchKeys.Of(app.Name) is { } key)
        {
            item.ShortcutKeyDisplayString = key.Text;
            CommandPalette.Keyed(item, BundleKeys.LaunchKey(key));
        }

        return item;
    }

    private void Fill()
    {
        _recentlyClosed.DropDownItems.Clear();
        var closed = RecentlyClosed.Load();
        for (var i = 0; i < closed.Count; i++)
        {
            var item = closed[i];
            if (Dispatch.ByName(item.App) is { } app)
            {
                _recentlyClosed.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem(AxKitWindows.Label(i, RecentlyClosed.Title(item)), null, (_, _) => Start(app.Verb, item.Path.Length == 0 ? null : item.Path)), OpenMark(app.Verb)));
            }
        }

        RecentMenu.NoneWhenEmpty(_recentlyClosed, "&Nothing recently closed");

        var windows = OpenWindows();
        var current = Current();
        FillWindows(_openWindows, AxKitWindows.Others(windows, current), current, "&No other open windows");
    }

    /// <summary>Starting an app is open, but axClaude and axTerminal start Claude Code or a shell as they open: act (docs/axKit/mcp.md MC-3.1, app-kit.md AK-2.5).</summary>
    public static CommandMark OpenMark(string verb) => AxKit.Core.Mcp.McpTools.StartsAProgram.Contains(verb) ? CommandMark.Act : CommandMark.Open;

    private nint Current() => _owner.IsHandleCreated ? _owner.Handle : 0;

    private static void FillWindows(ToolStripMenuItem list, IReadOnlyList<AxKitWindow> windows, nint current, string none)
    {
        list.DropDownItems.Clear();
        for (var i = 0; i < windows.Count; i++)
        {
            var window = windows[i];
            list.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem(AxKitWindows.Label(i, window.Title), null, (_, _) => Activate(window.Handle))
            {
                Checked = window.Handle == current,
            }, CommandMark.Open));
        }

        RecentMenu.NoneWhenEmpty(list, none);
    }

    /// <summary>
    /// AX-9.8: the close command Alt+F4 sends, posted to every other axKit window and then to this one; each window
    /// closes as it does on Alt+F4 and asks there what it would ask then (unsaved changes, Claude working). In a test
    /// run the other windows are the run's own alone (<see cref="BundleWindows.Visible"/>): never the user's.
    /// </summary>
    private void CloseAll()
    {
        var current = Current();
        foreach (var window in AxKitWindows.Others(OpenWindows(), current))
        {
            PostMessage(window.Handle, WM_SYSCOMMAND, SC_CLOSE, 0);
        }

        if (current != 0)
        {
            PostMessage(current, WM_SYSCOMMAND, SC_CLOSE, 0);
        }
    }

    /// <summary>The visible windows of the bundle's processes, most recently used first; in a test run only the run's own (<see cref="BundleWindows.Visible"/>, the one place that lists them).</summary>
    public static IReadOnlyList<AxKitWindow> OpenWindows() => BundleWindows.Visible();

    /// <summary>Brings the window to the front, restored when it was minimized. Windows allows it because the user's own key in the foreground window asked.</summary>
    public static void Activate(nint handle)
    {
        if (IsIconic(handle))
        {
            ShowWindow(handle, SW_RESTORE);
        }

        SetForegroundWindow(handle);
    }

    /// <summary>
    /// The apps as menu items in the bundle's order, a separator before each new group (AX-9.2, 2026-10-24): every list
    /// of apps in the windows is made here (Apps and Access layers, axFile's Open with axKit apps and access layers), so that a new app of
    /// <see cref="Dispatch.Apps"/> is in all of them.
    /// </summary>
    public static void AddApps(ToolStripItemCollection items, IEnumerable<App> apps, Func<App, ToolStripMenuItem> item)
    {
        AppGroup? group = null;
        foreach (var app in apps)
        {
            if (group is { } last && last != app.Group)
            {
                items.Add(new ToolStripSeparator());
            }

            group = app.Group;
            items.Add(item(app));
        }
    }

    /// <summary>A new window of the app: this program file with the app's verb and, for a recently closed item, its path; a new process (B-2).</summary>
    public static void Start(string verb, string? path) => StartWith(verb, path is null ? [] : [path]);

    /// <summary>The app started with several arguments after its verb (axMedia's paths and --add, a viewer's --new); a new process, which may hand its paths to the app's open window and end (AppMessage).</summary>
    public static void StartWith(string verb, IReadOnlyList<string> arguments) => StartKept(verb, arguments)?.Dispose();

    /// <summary>
    /// As <see cref="StartWith"/>, with the started process handed back for a caller that keeps one window of its own
    /// and must know whether it still lives before its window exists (axHelp's conversation, docs/axHelp/SPEC.md
    /// HE-2.1); null when it could not be started. The caller disposes it.
    /// </summary>
    public static Process? StartKept(string verb, IReadOnlyList<string> arguments)
    {
        if (Environment.ProcessPath is not { } exe)
        {
            return null;
        }

        var info = new ProcessStartInfo(exe) { UseShellExecute = false };
        // A new window of a test run joins the run; nothing else ever inherits its variables (security audit S-11).
        AutomationEnvironment.PassTo(info);
        info.ArgumentList.Add(verb);
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        try
        {
            // Outside call, program, axKit: starts another window of axKit from the axKit menu (Apps, Access layers), the Window menu (Show
            // current folder in, axFile's Open with axKit apps and access layers): this very program file with the app's verb and path; axHelp starts its conversation, an axClaude window, the same way, hidden as axHelp itself starts in the background only when the user turned on its setting Keep the conversation ready in the background, off at first (docs/axHelp/SPEC.md HE-2.7).
            return Process.Start(info);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // The program file blocked (a policy, an antivirus): logged, since the menu has closed and the caller is any window.
            Log.Error($"axKit {verb} could not be started", ex);
            return null;
        }
    }

    private const int SW_RESTORE = 9;
    private const nint SC_CLOSE = 0xF060;

    [DllImport("user32.dll")]
    private static extern bool IsIconic(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int cmd);
}
