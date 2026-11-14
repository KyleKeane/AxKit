using AxKit.Core;
using AxKit.Core.Mcp;

namespace AxKit.Forms;

/// <summary>
/// The setting up of the AI tools' access from a window (docs/axKit/mcp.md MC-6): what the items of the axKit menu's
/// AI tools submenu do (the two switches, Add axKit to Claude Code, Forget AI tools, Read the log) and the pairing
/// notice a new AI tool is answered with. A window holds one of these (<c>AxKitForm.Mcp.cs</c> makes the submenu and
/// calls it). It keeps nothing itself: the switches and the pairings are in the sealed setting (<see cref="McpAccess"/>);
/// it asks of the window its notices, its announcements and the consent. What an AI tool reads and runs in the window
/// stays in the window base (docs/axKit/repairs.json R-15: out of the window base, 2026-11-14).
/// </summary>
internal sealed class AiToolsSetup
{
    /// <summary>The App of an AI tool's consent in consent.json (bundle AX-11.5), the tool's name its target.</summary>
    private const string McpConsentApp = "AI tools";

    private readonly AxKitForm _form;

    public AiToolsSetup(AxKitForm form)
    {
        _form = form;
    }

    /// <summary>On: asked once, with what it allows (MC-6.1); off: at once, said.</summary>
    public void ToggleMcp()
    {
        if (McpAccess.Enabled)
        {
            if (SetMcp(false))
            {
                _form.Announce("AI tools off: no AI tool can read, open or run anything in axKit, and Let AI tools run commands is off too", true);
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
        _form.ShowNotice(new Notice("Turn on AI tools?", text, choices));
    }

    /// <summary>
    /// MC-7.2: Let AI tools run commands, on or off at once and said; sealed in the AI tools' setting with the rest, so
    /// that no file written by hand or by an AI tool turns it on. Said with what it needs while the access itself is off.
    /// </summary>
    public void ToggleRunCommands()
    {
        var on = !McpAccess.Load().RunCommands;
        try
        {
            McpAccess.SetRunCommands(on);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            _form.ShowFailure("The AI tools' setting was not changed: " + ex.Message);
            return;
        }

        _form.Announce(!on
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

        _form.Announce("AI tools on", true);
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
            _form.ShowFailure("The AI tools' setting was not changed: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// MC-6.8: Claude Code's own <c>claude mcp add</c> for every folder, with this very AxKit.exe, an earlier entry removed
    /// first; off the window's thread, the result said. Never in a test run, whose windows must not touch the user's
    /// Claude Code settings.
    /// </summary>
    public void AddToClaudeCode()
    {
        if (AutomationEnvironment.Requested)
        {
            _form.Announce("Not in a test run: Claude Code's settings are the user's", true);
            return;
        }

        // AX-11.5: this runs Claude Code's program too (claude mcp add), so the same agreement comes first, under this
        // window's app (review, 2026-11-02).
        if (!_form.HasConsent(ConsentTarget.ClaudeCode, AddToClaudeCode, () => _form.Announce("axKit not added to Claude Code", true)))
        {
            return;
        }

        if (ClaudeCodeProgram.Find(null) is not { } claude)
        {
            _form.ShowNotice(Notice.Plain("Claude Code was not found",
                "Claude Code is not installed here, or not where axKit looks (the PATH, your .local\\bin folder and npm's folder, where axClaude looks too). Add axKit to your AI tool with this command line:\n\n"
                + McpClaudeCode.AddCommandLine(PackageIdentity.ProgramPath!)));
            return;
        }

        var exe = PackageIdentity.ProgramPath!;
        _form.Announce("Adding axKit to Claude Code", true);
        _form.Do(async () =>
        {
            // The entries of before go first, the one under the bundle's old name too; either may be missing.
            await Task.Run(() => RunClaude(claude, McpClaudeCode.RemoveArguments(McpClaudeCode.OldServerName)));
            await Task.Run(() => RunClaude(claude, McpClaudeCode.RemoveArguments(McpClaudeCode.ServerName)));
            var (code, output) = await Task.Run(() => RunClaude(claude, McpClaudeCode.AddArguments(exe)));
            if (_form.IsDisposed)
            {
                return;
            }

            Log.Info($"claude mcp add: exit {code}");
            if (code == 0)
            {
                _form.Announce("axKit is added to Claude Code: a new Claude session can use it", true);
                return;
            }

            _form.ShowNotice(Notice.Plain("axKit was not added to Claude Code",
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

    public void ForgetMcpClients()
    {
        try
        {
            McpAccess.ForgetAll();
            Consent.WithdrawAll(AppPaths.ConsentFile, McpConsentApp);

            _form.Announce("AI tools forgotten: each asks again before it reads anything, and Let AI tools run commands is off", true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _form.ShowFailure("The AI tools were not forgotten: " + ex.Message);
        }
    }

    /// <summary>The audit log (MC-6.6) in axPreview; said when there is none yet.</summary>
    public void ReadMcpLog()
    {
        if (!File.Exists(AppPaths.McpAuditFile))
        {
            _form.Announce("No AI tool has called axKit yet: the log is empty", true);
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
    public bool AskToPairMcp(string name, string program, TaskCompletionSource<bool> answer)
    {
        if (_form.NoticeOpen)
        {
            return false;
        }

        var target = ConsentTarget.AiTool(name);
        var text = $"{name} asks to use axKit. It runs as {program}.\n\n"
            + "It can read what the axKit windows show (their text, lists and status) and open apps. It runs a window's commands and adds items to your axTask lists only while AI tools, Let AI tools run commands is on, each said as it happens, and it never reads axPass or axClip.\n\n"
            + $"A reminder: axKit is only an access layer here. {target.Work}\n\n"
            + "To allow it, check the box, then Allow. The axKit menu's AI tools, Forget AI tools, takes this back, and turns Let AI tools run commands off.";
        _form.ShowNotice(new Notice($"{name} asks to use axKit", text,
        [
            new OverlayChoice("&Allow", () =>
            {
                // The call gave up waiting (two minutes): its notice is gone, and no pairing is written for it.
                if (answer.Task.IsCompleted)
                {
                    _form.Announce($"{name}'s request has ended: nothing allowed", true);
                    return;
                }

                try
                {
                    McpAccess.Pair(program, name);
                    Consent.Record(AppPaths.ConsentFile, McpConsentApp, name, BundleInfo.Version, DateTime.Now);
                    answer.TrySetResult(true);
                    _form.Announce($"{name} may read axKit", true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    answer.TrySetResult(false);
                    _form.ShowFailure("The pairing was not written: " + ex.Message);
                }
            }, NeedsConsent: true),
            new OverlayChoice("&Deny", () => answer.TrySetResult(false), IsDefault: true, IsCancel: true),
        ])
        {
            Unprompted = true,
            Consent = AxKitForm.ConsentBox("axKit", name),
        });
        // The window closing refuses; the handler goes with the answer, so that requests do not pile handlers up.
        FormClosedEventHandler refuse = (_, _) => answer.TrySetResult(false);
        _form.FormClosed += refuse;
        answer.Task.ContinueWith(_ =>
        {
            try
            {
                _form.BeginInvoke(() => _form.FormClosed -= refuse);
            }
            catch (InvalidOperationException)
            {
                // The window is gone, its handlers with it.
            }
        }, TaskScheduler.Default);

        _form.FlashIfBehind();
        return true;
    }

    /// <summary>The pairing notice of a call that stopped waiting, closed when it is still the one shown.</summary>
    public void EndMcpPairing(string name)
    {
        if (_form.NoticeOpen && _form.Overlay.Shown?.Title == $"{name} asks to use axKit")
        {
            _form.CloseNotice(null);
            _form.Announce($"{name}'s request has ended: nothing allowed", true);
        }
    }
}
