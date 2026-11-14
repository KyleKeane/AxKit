using System.Runtime.InteropServices;
using AxKit.Core;
using AxKit.Core.Mcp;
using static AxKit.Forms.User32;

namespace AxKit.Forms;

/// <summary>
/// The window's part of the AI tools' access (docs/axKit/mcp.md MC-6): its pipe started as it first shows, the axKit
/// menu's AI tools submenu, what the pipe reads of the window, and the pairing notice. The pipe itself is
/// <see cref="McpChannel"/>; the broker, which the AI client starts, is <see cref="McpBroker"/>.
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
    private void FlashIfBehind()
    {
        if (GetForegroundWindow() != Handle)
        {
            var info = new FlashInfo { Size = (uint)Marshal.SizeOf<FlashInfo>(), Window = Handle, Flags = FlashAll | FlashUntilForeground };
            FlashWindowEx(ref info);
        }
    }

    /// <summary>For the pipe's <c>hello</c>: the app's name.</summary>
    internal string AppNameForTools => AppName;

    /// <summary>The App of an AI tool's consent in consent.json (bundle AX-11.5), the tool's name its target.</summary>
    private const string McpConsentApp = "AI tools";

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
        var access = CommandPalette.Marked(new ToolStripMenuItem("&Let AI tools read and open axKit", null, (_, _) => ToggleMcp()), CommandMark.Act);
        menu.DropDownItems.Add(access);
        // MC-7.2: the second switch, a menu item of the bundle's and never a command of the window's list, so that no AI tool can run it.
        var run = CommandPalette.Marked(new ToolStripMenuItem("Let AI tools r&un commands", null, (_, _) => ToggleRunCommands()), CommandMark.Act);
        menu.DropDownItems.Add(run);
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Add axKit to Claude Code", null, (_, _) => AddToClaudeCode()), CommandMark.Act));
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Forget AI tools", null, (_, _) => ForgetMcpClients()), CommandMark.Act));
        menu.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Read the log", null, (_, _) => ReadMcpLog()), CommandMark.Act));
        CommandPalette.RefreshedOnOpening(menu, () =>
        {
            var settings = McpAccess.Load();
            access.Checked = settings.Enabled;
            run.Checked = settings.RunCommands;
        });
        return menu;
    }

    /// <summary>On: asked once, with what it allows (MC-6.1); off: at once, said.</summary>
    private void ToggleMcp()
    {
        if (McpAccess.Enabled)
        {
            if (SetMcp(false))
            {
                Announce("AI tools off: no AI tool can read, open or run anything in axKit, and Let AI tools run commands is off too", true);
            }

            return;
        }

        var claude = ClaudeCodeProgram.Find(null);
        // The run switch as it really is: it can be turned on while the access is off, and then counts from Turn on.
        var runs = McpAccess.Load().RunCommands
            ? "Let AI tools run commands is on, so they may also run a window's commands and add items to your axTask lists, each said as it happens, a command that acts outside its window asked of you first"
            : "They run a window's commands and add items to your axTask lists only after you also turn on Let AI tools run commands, which is off";
        var text = "AI tools such as Claude Code may then ask to read what the axKit windows show (their text, lists and status) "
            + $"and to open apps. {runs}; and they never read axPass or axClip. Each tool asks you "
            + "once, in an axKit window, before it reads anything, and every call is written in the log (AI tools, Read the log).\n\n"
            + (claude is null
                ? "Claude Code was not found here, so Turn on only; then add axKit to your AI tool with this command line:\n\n" + McpClaudeCode.AddCommandLine(PackageIdentity.ProgramPath!)
                : "Turn on and add to Claude Code also adds axKit to Claude Code for every folder; a new Claude session can then use it.");
        List<OverlayChoice> choices = claude is null
            ? [new OverlayChoice("&Turn on", () => TurnMcpOn(addToClaude: false), IsDefault: true)]
            : [new OverlayChoice("&Turn on and add to Claude Code", () => TurnMcpOn(addToClaude: true), IsDefault: true), new OverlayChoice("Turn on &only", () => TurnMcpOn(addToClaude: false))];
        choices.Add(new OverlayChoice("&Cancel", IsCancel: true));
        ShowNotice(new Notice("Turn on AI tools?", text, choices));
    }

    /// <summary>
    /// MC-7.2: Let AI tools run commands, on or off at once and said; sealed in the AI tools' setting with the rest, so
    /// that no file written by hand or by an AI tool turns it on. Said with what it needs while the access itself is off.
    /// </summary>
    private void ToggleRunCommands()
    {
        var on = !McpAccess.Load().RunCommands;
        try
        {
            McpAccess.SetRunCommands(on);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail("The AI tools' setting was not changed: " + ex.Message);
            return;
        }

        Announce(!on
            ? "AI tools may no longer run commands"
            : McpAccess.Enabled
                ? "AI tools may run commands: each is said as it runs, and one that acts outside its window asks you first. They may also add items to your axTask lists, each said as it is added"
                : "AI tools may run commands once AI tools are on: Let AI tools read and open axKit", true);
    }

    private void TurnMcpOn(bool addToClaude)
    {
        if (!SetMcp(true))
        {
            return;
        }

        Announce("AI tools on", true);
        if (addToClaude)
        {
            AddToClaudeCode();
        }
    }

    /// <summary>The setting written; false after the notice that says why not.</summary>
    private bool SetMcp(bool on)
    {
        try
        {
            McpAccess.SetEnabled(on);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Fail("The AI tools' setting was not changed: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// MC-6.8: Claude Code's own <c>claude mcp add</c> for every folder, with this very AxKit.exe, an earlier entry removed
    /// first; off the window's thread, the result said. Never in a test run, whose windows must not touch the user's
    /// Claude Code settings.
    /// </summary>
    private void AddToClaudeCode()
    {
        if (AutomationEnvironment.Requested)
        {
            Announce("Not in a test run: Claude Code's settings are the user's", true);
            return;
        }

        // AX-11.5: this runs Claude Code's program too (claude mcp add), so the same agreement comes first, under this
        // window's app (review, 2026-11-02).
        if (!HasConsent(ConsentTarget.ClaudeCode, AddToClaudeCode, () => Announce("axKit not added to Claude Code", true)))
        {
            return;
        }

        if (ClaudeCodeProgram.Find(null) is not { } claude)
        {
            ShowNotice(Notice.Plain("Claude Code was not found",
                "Claude Code is not installed here, or not where axKit looks (the PATH, your .local\\bin folder and npm's folder, where axClaude looks too). Add axKit to your AI tool with this command line:\n\n"
                + McpClaudeCode.AddCommandLine(PackageIdentity.ProgramPath!)));
            return;
        }

        var exe = PackageIdentity.ProgramPath!;
        Announce("Adding axKit to Claude Code", true);
        Do(async () =>
        {
            // The entries of before go first, the one under the bundle's old name too; either may be missing.
            await Task.Run(() => RunClaude(claude, McpClaudeCode.RemoveArguments(McpClaudeCode.OldServerName)));
            await Task.Run(() => RunClaude(claude, McpClaudeCode.RemoveArguments(McpClaudeCode.ServerName)));
            var (code, output) = await Task.Run(() => RunClaude(claude, McpClaudeCode.AddArguments(exe)));
            if (IsDisposed)
            {
                return;
            }

            Log.Info($"claude mcp add: exit {code}");
            if (code == 0)
            {
                Announce("axKit is added to Claude Code: a new Claude session can use it", true);
                return;
            }

            ShowNotice(Notice.Plain("axKit was not added to Claude Code",
                $"Claude Code said: {output}\n\nThe command line, to run by hand:\n\n{McpClaudeCode.AddCommandLine(exe)}"));
        });
    }

    /// <summary>Claude Code run without a window, its arguments as a list (cmd for npm's claude.cmd), at most 30 seconds; its exit code and what it printed.</summary>
    private static (int Code, string Output) RunClaude(string claude, IReadOnlyList<string> arguments)
    {
        // S-53 (security review, 2026-10-25), on the bundle's one rule since R-24 (2026-11-14): claude.cmd runs through
        // cmd.exe with every argument quoted; a path cmd would act on inside quotes too (an AxKit.exe under C:\100%)
        // is refused rather than run, with the sentence that names the character.
        var line = ClaudeCodeProgram.LineFor(claude, arguments);
        if (line.Refusal is { } refusal)
        {
            return (-1, refusal + " Or add axKit by hand with the command line shown.");
        }

        var start = new System.Diagnostics.ProcessStartInfo(line.FileName, line.Arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        try
        {
            // Outside call, program, Claude Code: runs the user's Claude Code without a window for "claude mcp add" and
            // "claude mcp remove", so that Add axKit to Claude Code registers axKit's AI tools there (docs/axKit/mcp.md
            // MC-6.8).
            using var process = System.Diagnostics.Process.Start(start);
            if (process is null)
            {
                return (-1, "it did not start");
            }

            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30_000))
            {
                process.Kill(entireProcessTree: true);
                return (-1, "it did not finish within 30 seconds");
            }

            var reads = Task.WhenAll(output, error);
            var text = reads.Wait(5000) ? (output.Result + "\n" + error.Result).Trim() : string.Empty;
            return (process.ExitCode, text.ReplaceLineEndings(" ").Trim());
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return (-1, ex.Message);
        }
    }

    private void ForgetMcpClients()
    {
        try
        {
            McpAccess.ForgetAll();
            Consent.WithdrawAll(AppPaths.ConsentFile, McpConsentApp);

            Announce("AI tools forgotten: each asks again before it reads anything, and Let AI tools run commands is off", true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("The AI tools were not forgotten: " + ex.Message);
        }
    }

    /// <summary>The audit log (MC-6.6) in axPreview; said when there is none yet.</summary>
    private void ReadMcpLog()
    {
        if (!File.Exists(AppPaths.McpAuditFile))
        {
            Announce("No AI tool has called axKit yet: the log is empty", true);
            return;
        }

        WindowMenu.Start(Dispatch.AxPreview, AppPaths.McpAuditFile);
    }
    /// <summary>
    /// The pairing notice (MC-6.4), from the pipe's thread: "Claude Code asks to use axKit …", Allow and Deny. Deny is the
    /// default and Escape, so that Enter alone never allows; the notice is unprompted (its chime, and a moment in
    /// which keys do nothing), and the window flashes on the taskbar when it is not in front. Closing the window or
    /// leaving the notice unanswered refuses, when the pipe stops waiting. False, with nothing shown, while another
    /// notice is open: a notice replaces the one shown, and the user may be in the middle of answering it.
    /// </summary>
    internal bool AskToPairMcp(string name, string program, TaskCompletionSource<bool> answer)
    {
        if (NoticeOpen)
        {
            return false;
        }

        var target = ConsentTarget.AiTool(name);
        var text = $"{name} asks to use axKit. It runs as {program}.\n\n"
            + "It can read what the axKit windows show (their text, lists and status) and open apps. It runs a window's commands and adds items to your axTask lists only while AI tools, Let AI tools run commands is on, each said as it happens, and it never reads axPass or axClip.\n\n"
            + $"A reminder: axKit is only an access layer here. {target.Work}\n\n"
            + "To allow it, check the box, then Allow. The axKit menu's AI tools, Forget AI tools, takes this back, and turns Let AI tools run commands off.";
        ShowNotice(new Notice($"{name} asks to use axKit", text,
        [
            new OverlayChoice("&Allow", () =>
            {
                // The call gave up waiting (two minutes): its notice is gone, and no pairing is written for it.
                if (answer.Task.IsCompleted)
                {
                    Announce($"{name}'s request has ended: nothing allowed", true);
                    return;
                }

                try
                {
                    McpAccess.Pair(program, name);
                    Consent.Record(AppPaths.ConsentFile, McpConsentApp, name, BundleInfo.Version, DateTime.Now);
                    answer.TrySetResult(true);
                    Announce($"{name} may read axKit", true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    answer.TrySetResult(false);
                    Fail("The pairing was not written: " + ex.Message);
                }
            }, NeedsConsent: true),
            new OverlayChoice("&Deny", () => answer.TrySetResult(false), IsDefault: true, IsCancel: true),
        ])
        {
            Unprompted = true,
            Consent = ConsentBox("axKit", name),
        });
        // The window closing refuses; the handler goes with the answer, so that requests do not pile handlers up.
        FormClosedEventHandler refuse = (_, _) => answer.TrySetResult(false);
        FormClosed += refuse;
        answer.Task.ContinueWith(_ =>
        {
            try
            {
                BeginInvoke(() => FormClosed -= refuse);
            }
            catch (InvalidOperationException)
            {
                // The window is gone, its handlers with it.
            }
        }, TaskScheduler.Default);

        FlashIfBehind();
        return true;
    }

    /// <summary>The pairing notice of a call that stopped waiting, closed when it is still the one shown.</summary>
    internal void EndMcpPairing(string name)
    {
        if (NoticeOpen && Overlay.Shown?.Title == $"{name} asks to use axKit")
        {
            CloseNotice(null);
            Announce($"{name}'s request has ended: nothing allowed", true);
        }
    }

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
