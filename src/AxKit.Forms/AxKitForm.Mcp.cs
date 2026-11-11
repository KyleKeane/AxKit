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

    /// <summary>The window's commands for <c>axkit_window_commands</c>.</summary>
    internal IReadOnlyList<Command> CommandList => _commands;

    /// <summary>The menus that hold a command's first item ("Navigate, Next"), empty for a key with no item: for <c>axkit_window_commands</c> and <c>axkit_window_run</c> (MC-7.1).</summary>
    internal string MenuOf(Command command) =>
        _commandItems.TryGetValue(command, out var items) ? CommandPalette.MenuPath(items[0]) : string.Empty;

    /// <summary>
    /// <c>axkit_window_run</c>, on the window's thread (docs/axKit/mcp.md MC-7): the refusals first (axPass and axClip,
    /// the switch off, a notice or a Windows dialog the user has in hand, the user working in the window in front, a name
    /// that names no command or two, a key with no menu item, an item the menus do not offer now); then a command marked
    /// act asks the user (MC-7.3), and any other is posted to run as its menu item would, after the words that say who
    /// ran it (MC-7.5). Posted, so that the pipe's answer never waits on a command that shows a Windows dialog; the
    /// ticket finds what came of it (<see cref="McpAfterRun"/>). <paramref name="allowed"/> is the switch as the pipe's
    /// thread read it from the sealed file just before (MC-7.6), so that the window's thread never waits on the file.
    /// Only the window's own command list is searched: the bundle's menus (the AI tools' switches among them) are not
    /// commands of it and never run for a client.
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

        if (NoticeOpen)
        {
            return ($"The window shows the user a notice, {Overlay.Shown?.Title}: commands run once the user has answered it, and AI tools never answer it.", string.Empty, false, 0);
        }

        if (DialogShowing)
        {
            return ("The window shows the user a Windows dialog: commands run once the user has closed it.", string.Empty, false, 0);
        }

        // MC-7.8: never under the user's hands; a command may move the caret or the focus as its key would.
        if (McpRun.IsUserBusy(GetForegroundWindow() == Handle, UserIdleMs()))
        {
            return (McpRun.UserBusy, string.Empty, false, 0);
        }

        var (index, refusal) = McpRun.Find([.. _commands.Select(command => new McpCommandName(command.Label, MenuOf(command)))], name, menu);
        if (refusal is not null)
        {
            return (refusal, string.Empty, false, 0);
        }

        var found = _commands[index];
        var label = McpRun.Plain(found.Label);
        if (!_commandItems.TryGetValue(found, out var items))
        {
            return ($"{label} is a key of the window with no menu item: AI tools run what the menus offer.", label, false, 0);
        }

        if (items.FirstOrDefault(CommandPalette.Offered) is not { } item)
        {
            return ($"{label} is not available now: its menu item is disabled.", label, false, 0);
        }

        if (found.Mark == CommandMark.Act)
        {
            // After the user's Deny, the same window asks no more for a minute: a client that asks again at once does not
            // wear the user down (as the pairing's Deny, MC-6.4).
            // Per window, whichever client asks: the broker does not learn of the user's Deny (MC-7.3).
            if (Environment.TickCount64 < _mcpRunDeniedUntil)
            {
                return ("The user denied running a command a moment ago; this window asks no more within a minute.", label, false, 0);
            }

            AskToRunMcp(client, label, MenuOf(found), item, McpTarget());
            return (null, label, true, 0);
        }

        var ticket = ++_mcpRunTickets;
        _mcpRun = (ticket, false, null);
        BeginInvoke(() =>
        {
            // The outcome is kept before the click, which may hold this thread in a Windows dialog. The user may have
            // started typing in the window between the check and now (MC-7.8).
            var notRun = WhyNotRunForMcp(item, allowed: true)
                ?? (McpRun.IsUserBusy(GetForegroundWindow() == Handle, UserIdleMs()) ? "the user was working in the window" : null);
            _mcpRun = (ticket, true, notRun);
            if (notRun is null)
            {
                RunForMcp(client, label, item);
            }
        });
        return (null, label, false, ticket);
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
    private string? WhyNotRunForMcp(ToolStripMenuItem item, bool allowed)
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

        return CommandPalette.Offered(item) ? null : "its menu item is not available now";
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
        if (GetForegroundWindow() != Handle && McpRun.Chimes(_mcpLastChime, Environment.TickCount64))
        {
            _mcpLastChime = Environment.TickCount64;
            Sounds.Notice();
            FlashIfBehind();
        }

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
    /// switch is still on (read again off the window's thread), the item still available and the window's selection still
    /// the one the question named (<paramref name="target"/>, <see cref="McpTarget"/>).
    /// </summary>
    private void AskToRunMcp(string client, string label, string menu, ToolStripMenuItem item, RunTarget target)
    {
        var text = McpRun.AskText(client, label, menu, target.Shown);
        ShowNotice(new Notice(McpRun.AskTitle(client, label), text,
        [
            new OverlayChoice("&Run", () =>
            {
                // The order matters: CloseNotice gives the focus back to the control that had it before the question
                // (the notice rule, AxKitForm.Notices.cs: _focusBeforeNotice selected before CloseNotice returns, the
                // Run being StaysOpen and outside any chain), so that McpTarget reads the same pane as when it was asked;
                // read with the notice still open, the focus would be on this button and the default target would differ.
                CloseNotice(null);
                if (McpRun.TargetRefusal(target, McpTarget()) is { } changed)
                {
                    Announce($"{label} not run: {changed}", true);
                    return;
                }

                Do(async () =>
                {
                    // The user may have turned the switch off while the question waited.
                    var allowed = await Task.Run(() => McpAccess.RunsCommands);
                    if (WhyNotRunForMcp(item, allowed) is { } reason)
                    {
                        if (!IsDisposed)
                        {
                            Announce($"{label} not run: {reason}", true);
                        }

                        return;
                    }

                    RunForMcp(client, label, item);
                });
            }, StaysOpen: true),
            new OverlayChoice("&Deny", () =>
            {
                _mcpRunDeniedUntil = Environment.TickCount64 + 60_000;
                Announce($"{label} not run", true);
            }, IsDefault: true, IsCancel: true),
        ])
        {
            Unprompted = true,
        });
        FlashIfBehind();
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
        var menu = new ToolStripMenuItem("A&I tools");
        var access = new ToolStripMenuItem("&Let AI tools read and open axKit", null, (_, _) => ToggleMcp());
        menu.DropDownItems.Add(access);
        // MC-7.2: the second switch, a menu item of the bundle's and never a command of the window's list, so that no AI tool can run it.
        var run = new ToolStripMenuItem("Let AI tools r&un commands", null, (_, _) => ToggleRunCommands());
        menu.DropDownItems.Add(run);
        menu.DropDownItems.Add(new ToolStripMenuItem("&Add axKit to Claude Code", null, (_, _) => AddToClaudeCode()));
        menu.DropDownItems.Add(new ToolStripMenuItem("&Forget AI tools", null, (_, _) => ForgetMcpClients()));
        menu.DropDownItems.Add(new ToolStripMenuItem("&Read the log", null, (_, _) => ReadMcpLog()));
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
            ? "Let AI tools run commands is on, so they may also run a window's commands, each said as it runs, one that acts outside its window asked of you first"
            : "They run a window's commands only after you also turn on Let AI tools run commands, which is off";
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
                ? "AI tools may run commands: each is said as it runs, and one that acts outside its window asks you first"
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
        var script = claude.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase);
        var start = new System.Diagnostics.ProcessStartInfo(script ? SystemPrograms.Cmd : claude)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (script)
        {
            // S-53 (security review, 2026-10-25): cmd reads its special characters even inside quotes, so a path or an
            // argument holding one (an AxKit.exe under C:\R&D) is refused rather than run; every argument is quoted.
            if (SystemPrograms.HasCmdSpecials(claude) || arguments.Any(SystemPrograms.HasCmdSpecials))
            {
                return (-1, "a path holds one of cmd's special characters (& | < > ^ % ! and the like), so claude.cmd is not run with it; add axKit by hand with the command line shown");
            }

            start.Arguments = "/d /s /c \"\"" + claude + "\" " + string.Join(' ', arguments.Select(a => $"\"{a}\"")) + "\"";
        }
        else
        {
            foreach (var argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }
        }

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
            + "It can read what the axKit windows show (their text, lists and status) and open apps. It runs a window's commands only while AI tools, Let AI tools run commands is on, each said as it runs, and it never reads axPass or axClip.\n\n"
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
