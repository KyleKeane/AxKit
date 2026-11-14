using System.Runtime.InteropServices;
using AxKit.Core;
using AxKit.Core.Mcp;
using static AxKit.Forms.User32;

namespace AxKit.Forms;

/// <summary>
/// The window's part of the AI tools' access (docs/axKit/mcp.md MC-6): its pipe started as it first shows, the axKit
/// menu's AI tools submenu, what the pipe reads of the window, and a command run for an AI tool, with the question an
/// act command asks first. What the submenu's items do and the pairing notice are <see cref="AiToolsSetup"/>'s, which
/// the window holds. The pipe itself is <see cref="McpChannel"/>; the broker, which the AI client starts, is
/// <see cref="McpBroker"/>.
/// </summary>
public abstract partial class AxKitForm
{
    /// <summary>The app's verb (<see cref="Dispatch.Apps"/>): the command line's word for this window, and what decides whether the window is read at all (<see cref="McpTools.NeverRead"/>). Every window's app is listed there (InventoryTests).</summary>
    protected string AppVerb => Dispatch.ByName(AppName)?.Verb ?? throw new InvalidOperationException($"{AppName} is not an app of Dispatch.Apps");

    /// <summary>
    /// What an AI tool sees and runs in this window (docs/axKit/app-kit.md AK-2.5): the command palette's entries, the
    /// settings and the recent lists among them, but those withheld from the AI tools (AK-2.6, exception 3). One list
    /// for <c>axkit_window_commands</c> and <c>axkit_window_run</c>, so that the two and the palette never disagree.
    /// </summary>
    internal List<CommandPalette.MenuEntry> ToolEntries() =>
        CommandPalette.Entries(MainMenuStrip).Where(entry => CommandPalette.ForAiTools(entry.Item)).ToList();

    /// <summary>
    /// <c>axkit_window_run</c>, on the window's thread (docs/axKit/mcp.md MC-7): the refusals first (axPass and axClip,
    /// the switch off, a notice or a Windows dialog the user has in hand, the user working in the window in front, a name
    /// that names no command or two, a key with no menu item, an item the menus do not offer now); then a command marked
    /// act asks the user (MC-7.3), and any other is posted to run as its menu item would, after the words that say who
    /// ran it (MC-7.5). Posted, so that the pipe's answer never waits on a command that shows a Windows dialog; the
    /// ticket finds what came of it (<see cref="McpAfterRun"/>). <paramref name="allowed"/> is the switch as the pipe's
    /// thread read it from the sealed file just before (MC-7.6), so that the window's thread never waits on the file.
    /// The palette's entries are searched (<see cref="ToolEntries"/>, AK-2.5): every menu item, a setting or a recent
    /// file among them, with its mark (an item with none is act, and asks); the AI tools' own switches are not there.
    /// </summary>
    internal (string? Error, string Label, bool Asked, int Ticket) McpRunCommand(string? name, string? menu, string client, bool allowed)
    {
        if (McpRun.AppRefusal(AppVerb) is { } refused)
        {
            return (refused, string.Empty, false, 0);
        }

        if (!allowed)
        {
            return (McpRun.SwitchOff, string.Empty, false, 0);
        }

        if (McpChangeRefusal() is { } busy)
        {
            return (busy, string.Empty, false, 0);
        }

        var entries = ToolEntries();
        var (index, refusal) = McpRun.Find([.. entries.Select(entry => new McpCommandName(entry.Name, entry.Menu))], name, menu);
        if (refusal is not null)
        {
            return (refusal, string.Empty, false, 0);
        }

        // The entry is kept by its name and menus, never by its item (docs/axKit/repairs.json R-08): a recent or window
        // list makes its items again at every reading, and what runs later is found again then (ToolItem).
        var found = entries[index];
        var label = found.Name;
        var menus = found.Menu;
        if (CommandPalette.MarkOf(found.Item) == CommandMark.Act)
        {
            // After the user's Deny, the same window asks no more for a minute: a client that asks again at once does not
            // wear the user down (as the pairing's Deny, MC-6.4).
            // Per window, whichever client asks: the broker does not learn of the user's Deny (MC-7.3).
            if (Environment.TickCount64 < _mcpRunDeniedUntil)
            {
                return ("The user denied running a command a moment ago; this window asks no more within a minute.", label, false, 0);
            }

            AskToRunMcp(client, label, menus, McpTarget());
            return (null, label, true, 0);
        }

        var ticket = ++_mcpRunTickets;
        _mcpRun = (ticket, false, null);
        BeginInvoke(() =>
        {
            // The outcome is kept before the click, which may hold this thread in a Windows dialog. The user may have
            // started typing in the window between the check and now (MC-7.8).
            var notRun = WhyNotRunForMcp(allowed: true)
                ?? (McpRun.IsUserBusy(GetForegroundWindow() == Handle, UserIdleMs()) ? "the user was working in the window" : null);
            var item = notRun is null ? ToolItem(label, menus) : null;
            notRun ??= item is null ? NotAvailableNow : null;
            _mcpRun = (ticket, true, notRun);
            if (item is not null)
            {
                RunForMcp(client, label, item);
            }
        });
        return (null, label, false, ticket);
    }

    /// <summary>Why a run does not happen when its entry is no longer among the window's entries: disabled, renamed, or its list made anew without it.</summary>
    private const string NotAvailableNow = "its menu item is not available now";

    /// <summary>
    /// The item of the entry of that name and menus as the menus stand now, or null (R-08): found again at the moment a
    /// run happens, posted or after the user's Run, so that what is clicked is live, enabled and still what was named.
    /// </summary>
    private ToolStripMenuItem? ToolItem(string label, string menus) =>
        CommandPalette.Find(MainMenuStrip, label, menus) is { } entry && CommandPalette.ForAiTools(entry.Item) ? entry.Item : null;

    /// <summary>
    /// Why an AI tool may not change anything in this window now, or null: a notice or a Windows dialog the user has in
    /// hand (MC-7.3), or the user working in the window in front (MC-7.8), since a change may move the caret, the focus
    /// or the selection as the user's own key would. A run and a typed data tool that changes (MC-8.3) both ask it.
    /// </summary>
    protected string? McpChangeRefusal()
    {
        if (NoticeOpen)
        {
            return $"The window shows the user a notice, {Overlay.Shown?.Title}: commands run once the user has answered it, and AI tools never answer it.";
        }

        if (DialogShowing)
        {
            return "The window shows the user a Windows dialog: commands run once the user has closed it.";
        }

        return McpRun.IsUserBusy(GetForegroundWindow() == Handle, UserIdleMs()) ? McpRun.UserBusy : null;
    }

    /// <summary>
    /// MC-8: a typed data tool of this window's app (<see cref="McpTools.AppTools"/>), on the window's thread: the
    /// refusals every app shares first (axPass and axClip; a tool of another app; for a change, the switch off as
    /// <paramref name="allowed"/> was read off this thread, then <see cref="McpChangeRefusal"/>), then the app's own
    /// <see cref="McpAppRequest"/>. The answer is an object the pipe writes as JSON, <c>error</c> for a refusal.
    /// </summary>
    internal object McpApp(string tool, System.Text.Json.Nodes.JsonObject arguments, string client, bool allowed)
    {
        if (McpRun.AppRefusal(AppVerb) is { } refused)
        {
            return new { error = refused };
        }

        if (!McpTools.AppTools.TryGetValue(tool, out var appTool) || appTool.App != AppVerb)
        {
            return new { error = $"{AppName}'s window answers no tool {tool}." };
        }

        if (appTool.Changes)
        {
            if (!allowed)
            {
                return new { error = McpRun.SwitchOff };
            }

            if (McpChangeRefusal() is { } busy)
            {
                return new { error = busy };
            }
        }

        return McpAppRequest(tool, arguments, client) ?? new { error = $"{AppName}'s window answers no tool {tool}." };
    }

    /// <summary>
    /// MC-8: what a window answers to a typed data tool of its app, on the window's thread, after the shared refusals of
    /// <see cref="McpApp"/>; null for a tool it does not answer. <paramref name="client"/> is the name the pairing
    /// recorded, for the words that say who changed something (<see cref="McpRun.Speaker"/>); a window that changes
    /// something calls <see cref="ChimeForMcp"/> after its own announcement. An answer that takes a long read (many
    /// folders) is a <see cref="Task{Object}"/> started with what it needs taken on this thread; the pipe's thread
    /// waits for it (bundle rule 3).
    /// </summary>
    protected virtual object? McpAppRequest(string tool, System.Text.Json.Nodes.JsonObject arguments, string client) => null;

    /// <summary>MC-7.5: a change by an AI tool in a window that is not in front chimes and flashes it on the taskbar, since NVDA does not speak a window behind; once for a burst.</summary>
    protected void ChimeForMcp()
    {
        if (GetForegroundWindow() != Handle && McpRun.Chimes(_mcpLastChime, Environment.TickCount64))
        {
            _mcpLastChime = Environment.TickCount64;
            Sounds.Notice();
            FlashIfBehind();
        }
    }

    /// <summary>Until when an act's run is refused without asking, after the user's Deny (milliseconds of <see cref="Environment.TickCount64"/>).</summary>
    private long _mcpRunDeniedUntil;

    /// <summary>The last posted run (MC-7.1): its ticket, whether it was taken up, and why it did not run, null when it ran.</summary>
    private (int Ticket, bool Done, string? NotRun) _mcpRun;

    private int _mcpRunTickets;

    /// <summary>
    /// What a command left the window doing, for the run's answer: the notice it shows the user, or a Windows dialog; and
    /// for the run of <paramref name="ticket"/>, whether it ran (null while not known: not taken up yet, or another run
    /// came after it) and why not.
    /// </summary>
    internal (string? Notice, string? NoticeText, bool Dialog, bool? Ran, string? NotRun) McpAfterRun(int ticket)
    {
        var (current, done, notRun) = _mcpRun;
        bool? ran = current == ticket && done ? notRun is null : null;
        return (NoticeOpen ? Overlay.Shown?.Title : null, NoticeOpen ? Overlay.Shown?.Text : null, DialogShowing, ran, ran == false ? notRun : null);
    }

    /// <summary>Why a run that waited (posted, or asked of the user) does not run now, or null when it runs: checked again at the moment it would run.</summary>
    private string? WhyNotRunForMcp(bool allowed)
    {
        if (IsDisposed)
        {
            return "the window closed";
        }

        if (!allowed)
        {
            return "Let AI tools run commands is off";
        }

        if (NoticeOpen || DialogShowing)
        {
            return "the window showed the user a question first";
        }

        return null;
    }

    /// <summary>
    /// MC-7.5: the words that say who ran it, joined to what the command says next, then the item clicked as the menu
    /// would. A window that is not in front chimes and flashes on the taskbar as well, since NVDA does not speak the
    /// notifications of a window behind (<see cref="Raise"/>).
    /// </summary>
    private void RunForMcp(string client, string label, ToolStripMenuItem item)
    {
        AnnounceBefore(McpRun.Said(client, label));
        // One chime and flash for a burst of runs, not one per run.
        ChimeForMcp();
        item.PerformClick();
    }

    /// <summary>When a run in this window behind last chimed (<see cref="Environment.TickCount64"/>), -1 for never.</summary>
    private long _mcpLastChime = -1;

    /// <summary>
    /// MC-7.3: what a command of this window would act on now, for the question an act command asks and the check at the
    /// user's Run: the window's selection in a few words ("report.pdf in files"), empty when there is none. By default
    /// the focused control's name and, for a list, its selected item as NVDA reads it; the windows whose commands act on
    /// a selection say it themselves (axFile's selected entries, axStart's program, axProcess's process or service,
    /// axGIT's file, the item apps' row), with an identity of their own where the few words could name two things
    /// (axFile's full paths, the item apps' list and item file), which is what the user's Run compares.
    /// </summary>
    protected virtual RunTarget McpTarget()
    {
        if (FocusedControl() is not { } control)
        {
            return RunTarget.None;
        }

        var name = control.AccessibleName ?? string.Empty;
        return RunTarget.Of(control is ListBox && WindowReading.ValueOf(control) is { Length: > 0 } selected
            ? name.Length > 0 ? $"{selected}, in {name}" : selected
            : name);
    }

    /// <summary>Milliseconds since the user's last key or mouse move in this session (<c>GetLastInputInfo</c>), -1 when Windows cannot tell.</summary>
    private static long UserIdleMs()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        return GetLastInputInfo(ref info) ? unchecked((uint)Environment.TickCount - info.Time) : -1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    /// <summary>
    /// MC-7.3: a command that acts outside the window (a delete, a push, a program started) is asked of the user first,
    /// in the window, unprompted, with Deny the default and Escape; only the user's Run runs it, once, and only while the
    /// switch is still on (read again off the window's thread), the entry of that name still among the window's entries
    /// (found again then, <see cref="ToolItem"/>) and the window's selection still the one the question named
    /// (<paramref name="target"/>, <see cref="McpTarget"/>).
    /// </summary>
    private void AskToRunMcp(string client, string label, string menu, RunTarget target)
    {
        // The control that has the focus now is the one the target was read from, and the one a Yes reads it from again.
        var asked = FocusedControl();

        void Yes()
        {
            if (McpRun.TargetRefusal(target, McpTarget()) is { } changed)
            {
                Announce($"{label} not run: {changed}", true);
                return;
            }

            Do(async () =>
            {
                // The user may have turned the switch off while the question waited.
                var allowed = await Task.Run(() => McpAccess.RunsCommands);
                var reason = WhyNotRunForMcp(allowed);
                var item = reason is null ? ToolItem(label, menu) : null;
                if (item is null)
                {
                    if (!IsDisposed)
                    {
                        Announce($"{label} not run: {reason ?? NotAvailableNow}", true);
                    }

                    return;
                }

                RunForMcp(client, label, item);
            });
        }

        void No()
        {
            _mcpRunDeniedUntil = Environment.TickCount64 + 60_000;
            Announce($"{label} not run", true);
        }

        // Where the window has a place for it, the question is the list an offered command's is (MC-7.3, MC-9.4): the
        // window's content stays there to read, and the list starts on the answer that only explains.
        var request = new OfferedRequest(McpRun.AskName(client, label, menu, target.Shown), McpRun.AskText(client, label, menu, target.Shown),
            McpRun.ExplainTitle, McpRun.ExplainText(client, label, menu, target.Shown), McpRun.Answers, McpRun.Explain, McpRun.No, answer =>
            {
                if (answer != McpRun.Yes)
                {
                    No();
                    return;
                }

                // The list is gone and the bottom of the window has the focus: the control the question was asked from
                // gets it back first, so that McpTarget reads the same pane as when it was asked.
                if (asked is { IsDisposed: false, Visible: true, Enabled: true })
                {
                    asked.Select();
                }

                Yes();
            });
        if (ShowOfferedRequest(request))
        {
            FlashIfBehind();
            return;
        }

        // Elsewhere a notice, with the offer's chime, name and order of answers; its text is the explanation.
        if (NoticeChime)
        {
            Sounds.Offer();
        }

        ShowNotice(new Notice(McpRun.AskTitle(client, label), request.Text,
        [
            new OverlayChoice(McpRun.YesButton, () =>
            {
                // The order matters: CloseNotice gives the focus back to the control that had it before the question
                // (the notice rule, AxKitForm.Notices.cs: _focusBeforeNotice selected before CloseNotice returns, the
                // Yes being StaysOpen and outside any chain), so that McpTarget reads the same pane as when it was asked;
                // read with the notice still open, the focus would be on this button and the default target would differ.
                CloseNotice(null);
                Yes();
            }, StaysOpen: true),
            new OverlayChoice(McpRun.NoButton, No, IsDefault: true, IsCancel: true),
        ])
        {
            Unprompted = true,
            Silent = true,
        });
        FlashIfBehind();
    }

    /// <summary>
    /// What an AI asks the user to let happen, as a window shows it (MC-7.3, MC-9.4): the list's name, read with every
    /// arrival; the text Ctrl+Q reads where a window has that key; the explanation the first answer opens; the answers,
    /// with which one explains and which one is No (Escape); and what a chosen answer other than Explain does, called
    /// once, after the list has gone.
    /// </summary>
    protected sealed record OfferedRequest(string Name, string Text, string ExplainTitle, string ExplainText, IReadOnlyList<string> Answers, int Explain, int No, Action<int> Answer);

    /// <summary>
    /// Shows an AI's request as a list answered with the arrows. The console windows and the text editors show it at
    /// the bottom, with the conversation or the text above it still there to read; every other window, here, shows it
    /// over the whole window. False when the place is taken, and the request is asked in a notice instead.
    /// </summary>
    protected virtual bool ShowOfferedRequest(OfferedRequest request)
    {
        // A window with no section to leave in view (the user's decision of 2026-11-13): the same list over the whole
        // window, its sections hidden as for a notice, so that the arrows answer and Tab has nowhere else to go.
        if (_overWindow is null)
        {
            _overWindow = new ControlArea { Dock = DockStyle.Fill };
            Controls.Add(_overWindow);
        }

        var area = _overWindow;
        var before = FocusedControl();
        return ShowOfferedRequestIn(area, request, () =>
        {
            HideForNotice();
            area.BringToFront();
            PaintTheme(area);
            PerformLayout();
        }, () =>
        {
            ShowAfterNotice();
            PerformLayout();
            (before is { IsDisposed: false, Enabled: true } ? before : FocusTarget).Select();
        });
    }

    /// <summary>The list of an AI's request over the whole window, made at the first request; null in a window that never had one, and in the windows that show the list at the bottom.</summary>
    private ControlArea? _overWindow;

    /// <summary>Whether an AI's request covers the window now: its sections stay hidden when a notice over it closes (the explanation).</summary>
    private bool RequestOverWindow => _overWindow is { Current: not null };

    /// <summary>
    /// The request as a list in <paramref name="area"/>, starting on Explain, with the offer's chime and a moment in
    /// which the answering keys do nothing. Explain opens the text as a notice with one button and comes back to the
    /// list (the notice rule); any other answer, or Escape for No, empties the area, calls <paramref name="closed"/>
    /// for the window to put its own control back, then the request's answer. False when the area or a notice is in use.
    /// </summary>
    protected bool ShowOfferedRequestIn(ControlArea area, OfferedRequest request, Action shown, Action closed)
    {
        if (area.Current is not null || NoticeOpen || DialogShowing)
        {
            return false;
        }

        if (NoticeChime)
        {
            Sounds.Offer();
        }

        void Answer(int answer)
        {
            if (answer == request.Explain)
            {
                ShowNotice(Notice.Plain(request.ExplainTitle, request.ExplainText));
                return;
            }

            area.Clear();
            closed();
            request.Answer(answer);
        }

        Recent.Add(RecentActionKind.Notice, request.Name, DateTime.UtcNow);
        area.ShowList(request.Name, request.Answers, request.Explain, null, focus: true, hold: true, (chosen, _) => Answer(chosen), () => Answer(request.No));
        shown();
        return true;
    }

    /// <summary>The window flashed on the taskbar until it comes to the front, when it is not in front: a notice an AI tool caused waits for the user there.</summary>
    internal void FlashIfBehind()
    {
        if (GetForegroundWindow() != Handle)
        {
            var info = new FlashInfo { Size = (uint)Marshal.SizeOf<FlashInfo>(), Window = Handle, Flags = FlashAll | FlashUntilForeground };
            FlashWindowEx(ref info);
        }
    }

    /// <summary>For the pipe's <c>hello</c>: the app's name.</summary>
    internal string AppNameForTools => AppName;

    /// <summary>What the AI tools submenu's items do, and the pairing notice; made in the constructor (AxKitForm.cs).</summary>
    private readonly AiToolsSetup _aiTools;

    private void StartMcp()
    {
        McpChannel.Start(this, AppVerb);
    }

    /// <summary>What the pipe's <c>state</c> reads (MC-6.5): the title, the notice and its text, the status bar, the focused control and its value.</summary>
    internal object McpState()
    {
        var focused = Control.FromChildHandle(GetFocus());
        return new
        {
            title = Text,
            notice = NoticeOpen ? Overlay.Shown?.Title : null,
            noticeText = NoticeOpen ? Overlay.Shown?.Text : null,
            status = Controls.OfType<StatusStrip>().SelectMany(strip => strip.Items.Cast<ToolStripItem>()).Select(item => item.Text).ToArray(),
            focus = focused is null || focused.FindForm() != this ? null : new { name = focused.AccessibleName, value = WindowReading.ValueOf(focused) },
        };
    }

    /// <summary>
    /// axKit → AI tools ▸ (MC-6.1): the bundle's own setting, in the bundle's menu, never an app's Options. The check
    /// read as the submenu opens, since another window may have changed it.
    /// </summary>
    internal ToolStripMenuItem CreateMcpMenu()
    {
        // Never offered to an AI tool, nor anything in it (docs/axKit/app-kit.md AK-2.6, exception 3).
        var menu = CommandPalette.NeverForAiTools(new ToolStripMenuItem("A&I tools"));
        var access = CommandPalette.Marked(new ToolStripMenuItem("&Let AI tools read and open axKit", null, (_, _) => _aiTools.ToggleMcp()), CommandMark.Act);
        menu.DropDownItems.Add(access);
        // MC-7.2: the second switch, a menu item of the bundle's and never a command of the window's list, so that no AI tool can run it.
        var run = CommandPalette.Marked(new ToolStripMenuItem("Let AI tools r&un commands", null, (_, _) => _aiTools.ToggleRunCommands()), CommandMark.Act);
        menu.DropDownItems.Add(run);
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Add axKit to Claude Code", null, (_, _) => _aiTools.AddToClaudeCode()), CommandMark.Act));
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Forget AI tools", null, (_, _) => _aiTools.ForgetMcpClients()), CommandMark.Act));
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Read the log", null, (_, _) => _aiTools.ReadMcpLog()), CommandMark.Act));
        CommandPalette.RefreshedOnOpening(menu, () =>
        {
            var settings = McpAccess.Load();
            access.Checked = settings.Enabled;
            run.Checked = settings.RunCommands;
        });
        return menu;
    }

    /// <summary>The pairing notice (MC-6.4), from the pipe's thread: Allow and Deny, Deny the default; false, with nothing shown, while another notice is open. The notice is <see cref="AiToolsSetup"/>'s.</summary>
    internal bool AskToPairMcp(string name, string program, TaskCompletionSource<bool> answer) => _aiTools.AskToPairMcp(name, program, answer);

    /// <summary>The pairing notice of a call that stopped waiting, closed when it is still the one shown.</summary>
    internal void EndMcpPairing(string name) => _aiTools.EndMcpPairing(name);

    /// <summary>The app's <see cref="Fail"/>, for the services the window holds.</summary>
    internal void ShowFailure(string message) => Fail(message);

    private const uint FlashAll = 3;
    private const uint FlashUntilForeground = 12;

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashInfo
    {
        public uint Size;
        public nint Window;
        public uint Flags;
        public uint Count;
        public uint Timeout;
    }

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FlashInfo info);
}
