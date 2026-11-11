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
    /// the switch off, a notice or a Windows dialog the user has in hand, a name that names no command or two, a key with no
    /// menu item, an item the menus do not offer now); then a command marked act asks the user (MC-7.3), and any other is
    /// posted to run as its menu item would, after the words that say who ran it (MC-7.5). Posted, so that the pipe's
    /// answer never waits on a command that shows a Windows dialog. Only the window's own command list is searched:
    /// the bundle's menus (the AI tools' switches among them) are not commands of it and never run for a client.
    /// </summary>
    internal (string? Error, string Label, bool Asked) McpRunCommand(string? name, string? menu, string client)
    {
        if (McpRun.AppRefusal(AppVerb) is { } refused)
        {
            return (refused, string.Empty, false);
        }

        if (!McpAccess.RunsCommands)
        {
            return (McpRun.SwitchOff, string.Empty, false);
        }

        if (NoticeOpen)
        {
            return ($"The window shows the user a notice, {Overlay.Shown?.Title}: commands run once the user has answered it, and AI tools never answer it.", string.Empty, false);
        }

        if (DialogShowing)
        {
            return ("The window shows the user a Windows dialog: commands run once the user has closed it.", string.Empty, false);
        }

        var (index, refusal) = McpRun.Find([.. _commands.Select(command => new McpCommandName(command.Label, MenuOf(command)))], name, menu);
        if (refusal is not null)
        {
            return (refusal, string.Empty, false);
        }

        var found = _commands[index];
        var label = McpRun.Plain(found.Label);
        if (!_commandItems.TryGetValue(found, out var items))
        {
            return ($"{label} is a key of the window with no menu item: AI tools run what the menus offer.", label, false);
        }

        if (items.FirstOrDefault(CommandPalette.Offered) is not { } item)
        {
            return ($"{label} is not available now: its menu item is disabled.", label, false);
        }

        if (found.Mark == CommandMark.Act)
        {
            // After the user's Deny, the same window asks no more for a minute: a client that asks again at once does not
            // wear the user down (as the pairing's Deny, MC-6.4).
            if (Environment.TickCount64 < _mcpRunDeniedUntil)
            {
                return ("The user denied running a command a moment ago; this window asks no more within a minute.", label, false);
            }

            AskToRunMcp(client, label, MenuOf(found), item);
            return (null, label, true);
        }

        BeginInvoke(() => RunForMcp(client, label, item));
        return (null, label, false);
    }

    /// <summary>Until when an act's run is refused without asking, after the user's Deny (milliseconds of <see cref="Environment.TickCount64"/>).</summary>
    private long _mcpRunDeniedUntil;

    /// <summary>What a command left the window doing, for the run's answer: the notice it shows the user, or a Windows dialog.</summary>
    internal (string? Notice, string? NoticeText, bool Dialog) McpAfterRun() =>
        (NoticeOpen ? Overlay.Shown?.Title : null, NoticeOpen ? Overlay.Shown?.Text : null, DialogShowing);

    /// <summary>MC-7.5: the words that say who ran it, joined to what the command says next, then the item clicked as the menu would.</summary>
    private void RunForMcp(string client, string label, ToolStripMenuItem item)
    {
        // The switch asked again: the user may have turned it off while an act's question waited.
        if (IsDisposed || NoticeOpen || !McpAccess.RunsCommands)
        {
            return;
        }

        AnnounceBefore(McpRun.Said(client, label));
        item.PerformClick();
    }

    /// <summary>
    /// MC-7.3: a command that acts outside the window (a delete, a push, a program started) is asked of the user first,
    /// in the window, unprompted, with Deny the default and Escape; only the user's Run runs it, once.
    /// </summary>
    private void AskToRunMcp(string client, string label, string menu, ToolStripMenuItem item)
    {
        var speaker = McpRun.Speaker(client);
        var place = menu.Length > 0 ? $" ({menu})" : string.Empty;
        var text = $"{speaker} asks to run {label}{place} in this window. It acts outside the window: it may delete, move, send, start or end something, as when you run it yourself.\n\n"
            + "Run runs it once, now. Deny, the default, does not.";
        ShowNotice(new Notice(McpRun.AskTitle(client, label), text,
        [
            new OverlayChoice("&Run", () =>
            {
                CloseNotice(null);
                BeginInvoke(() => RunForMcp(client, label, item));
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
                Announce("AI tools off: no AI tool can read or open axKit", true);
            }

            return;
        }

        var claude = ClaudeCodeProgram.Find(null);
        var text = "AI tools such as Claude Code may then ask to read what the axKit windows show (their text, lists and status) "
            + "and to open apps. They run a window's commands only after you also turn on Let AI tools run commands, and they never read axPass or axClip. Each tool asks you "
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

            Announce("AI tools forgotten: each asks again before it reads anything", true);
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
            + "To allow it, check the box, then Allow. Options, General, Forget AI tools takes this back.";
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
