using AxClaude.Core.Hooks;
using AxClaude.Core.Pty;
using AxClaude.Core.Streaming;
using AxKit.Core;
using AxKit.Core.Pty;
using AxKit.Forms;

namespace AxClaude;

internal sealed partial class MainForm
{
    // ---- Claude's process (FR-1): what is Claude's around the console window's StartProcess and StopProcess ----

    /// <summary>The arguments Claude gets after --ax-screen-reader: the chosen ones, or the default (FR-9.1).</summary>
    private IReadOnlyList<string> ClaudeArgs => _claudeArgs ?? (_nothingToContinue ? [] : ["--continue"]);

    /// <summary>
    /// The arguments a start of the screen passes: <see cref="ClaudeArgs"/>, but with --continue (the default or the
    /// user's own) in a folder whose newest session is the one a window of Claude Code's stream last ran there
    /// (docs/axClaude/stream-mode.md SM-8.3): the interactive program's --continue passes over a session the stream
    /// began (measured), so that one is taken up by its id, and Claude Code draws its conversation as for any resumed
    /// one. A restart replays it as the same --continue would (<see cref="Relaunch"/>).
    /// </summary>
    private IReadOnlyList<string> ScreenClaudeArgs()
    {
        if (!SessionHandover.Continues(ClaudeArgs) || _folder is null || _settings.StreamSessionOf(_folder) is not { } kept)
        {
            return ClaudeArgs;
        }

        // The folder's session files are listed here, on the window's thread, at a start that continues in a folder a
        // stream window kept a session for; the user's own Claude Code file is read at every start (AiToolsArguments).
        // Both belong off this thread (bundle rule 3) and are left here on purpose: moving them makes every start go on
        // after a read, as the start that waits for Claude Code's version does, and that reordering of the start is
        // its own piece of work, left open by the review of 2026-11-27.
        var arguments = SessionHandover.ForScreen(ClaudeArgs, kept, SessionHandover.Newest(SessionHandover.ProjectFolders(SessionProjectsFolders(), _folder)));
        if (!SessionHandover.Continues(arguments))
        {
            Log.Info("Claude Code takes up the session a window of its stream last ran in this folder");
        }

        return arguments;
    }

    /// <summary>
    /// Claude → Force Claude to restart (Ctrl+Shift+R, FR-1.8): the same folder and arguments; the conversation
    /// stays. A running Claude is stopped only after the Force Claude to restart? notice; a stopped one starts at
    /// once. The steps are the console window's (<see cref="ConsoleForm.RestartProgram"/>): the question first
    /// while there is one, "Restarting", then <see cref="RestartNow"/>.
    /// </summary>
    protected override ConsoleQuestion? RestartQuestion()
    {
        if (!ProcessRunning)
        {
            return null;
        }

        var state = !ClaudeBusy ? string.Empty
            : _model.PromptPending ? "Claude is waiting for your answer. "
            : "Claude is still working. ";
        if (ClaudeBackground is { } background)
        {
            state += $"Claude has work running in the background ({background}). ";
        }

        return new ConsoleQuestion("Force Claude to restart?",
            state + "Restarting immediately quits Claude Code and terminates any processes it runs in the background, then starts it again in the same folder with the same options. The conversation stays in the window.",
            "&Keep Claude running");
    }

    protected override void RestartNow() => Relaunch("Restarting Claude");

    /// <summary>FR-13.2: Claude Code's version, asked once for each program file in a run of the app; null until Claude has been found once (FR-13.3 names it).</summary>
    private string? _claudeVersion;

    /// <summary>How long claude --version may take before it is given up on and ended.</summary>
    private static readonly TimeSpan ClaudeVersionTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Asks Claude Code's version, for the log and the diagnostics (FR-13.2) and for the hooks' gate, which is why
    /// the answer is the task's result (events-and-grounding.md EG-2.4; <c>HookVersionOf</c> keeps the task for each
    /// program file): what <c>claude --version</c> printed, null for no answer.
    /// </summary>
    private async Task<string?> AskClaudeVersionAsync(string claude)
    {
        _claudeVersion = "(not answered yet)";
        var version = await ClaudeLauncher.VersionAsync(claude, ClaudeVersionTimeout);
        _claudeVersion = version ?? "(no answer to --version)";
        _health.ProgramVersion = ClaudeCodeHooks.VersionNumbers(version);
        Log.Info($"Claude Code version: {_claudeVersion} ({claude})");
        return version;
    }

    /// <summary>
    /// Starts Claude in the project folder: the folder, the program and the user's consent first. Then Claude Code's
    /// version, which decides whether this start may pass the hooks (events-and-grounding.md EG-2.4): it was asked as
    /// the window opened and is in by now, or the start waits for what is left of its two seconds, without holding
    /// the window, and goes on in <see cref="StartClaudeNow"/>. A start that passes no hooks whatever the version
    /// says never waits.
    /// </summary>
    private void StartClaude()
    {
        // A start that still waited for the version is over: this one takes its place.
        HookCancelStart();
        if (!Directory.Exists(_folder))
        {
            Fail($"The project folder no longer exists: {_folder}. Press {BundleKeys.Text(AxClaudeKeys.NewSession)} to choose another.");
            return;
        }

        var configured = _options.ClaudePath ?? _settings.ClaudePath;
        var claude = ClaudeCodeProgram.Find(configured);
        if (claude is null)
        {
            // FR-1.9: say where the app looked, offer the install command, or let the user point at claude.exe.
            Log.Error("Claude Code was not found");
            _model.AddSystemLine($"Claude Code was not found. Install it, then press {BundleKeys.Text(AxClaudeKeys.RestartClaude)}.");
            UpdateStatus();
            ShowClaudeNotFound(ClaudeCodeProgram.Candidates(configured));
            return;
        }

        // Bundle AX-11.4: the first connection to Claude Code waits for the user's consent; Connect starts it.
        if (!HasConsent(ConsentTarget.ClaudeCode, StartClaude, () =>
            {
                _model.AddSystemLine($"Not connected to Claude Code. Press {BundleKeys.Text(AxClaudeKeys.RestartClaude)} to connect.");
                UpdateStatus();
            }))
        {
            return;
        }

        // A window made for Claude Code's stream (stream-mode.md SM-9.2) starts it as one process over pipes
        // (MainForm.Stream.cs): no hooks are passed, so nothing waits for Claude Code's version.
        if (Streams)
        {
            StartStream(claude);
            return;
        }

        // Asked once for each program file, here at the latest and never before the consent above (FR-13.2).
        var version = HookVersionOf(claude);
        if (HookWaitLeft(version) is var left and > 0)
        {
            HookStartWhenAnswered(claude, version.Answer, left);
            return;
        }

        StartClaudeNow(claude);
    }

    /// <summary>The start itself: Claude Code's command line and environment, the process in its pseudo console, and the window's state for a new run.</summary>
    private void StartClaudeNow(string claude)
    {
        var arguments = new List<string>();
        if (!_options.NoAx)
        {
            arguments.Add("--ax-screen-reader");
        }

        arguments.AddRange(ScreenClaudeArgs());
        // --model (FR-9.2): the window's own model on every start of Claude, the default --continue and its retry included (axHelp's conversation).
        if (_options.Model is { Length: > 0 } model && !ClaudeArgs.Contains("--model"))
        {
            arguments.Add("--model");
            arguments.Add(model);
        }

        // The name Claude draws above its prompt (--name or -n), so that the model knows that row for chrome.
        var nameAt = ClaudeArgs.ToList().FindIndex(argument => argument is "--name" or "-n");
        _model.StartName = nameAt >= 0 && nameAt + 1 < ClaudeArgs.Count ? ClaudeArgs[nameAt + 1] : null;
        // FR-1.2: npm's claude.cmd runs through cmd.exe, and an argument cmd.exe would act on is refused, said as any failed start.
        var line = ClaudeLauncher.BuildCommandLine(claude, arguments);
        // The hooks (events-and-grounding.md EG-2.3): --settings with axKit's own file right after --ax-screen-reader,
        // before the user's own arguments, on every start that may pass it (--continue and --resume do not restore
        // it); its pipe is served from here on. A line cmd.exe would refuse because of the file's path starts
        // without the hooks rather than not at all. Without them the line and the environment are what they were.
        if (HookArguments(claude) is { Count: > 0 } hooks)
        {
            List<string> withHooks = [.. arguments];
            withHooks.InsertRange(_options.NoAx ? 0 : 1, hooks);
            var hooksLine = ClaudeLauncher.BuildCommandLine(claude, withHooks);
            if (hooksLine.Refusal is null)
            {
                arguments = withHooks;
                line = hooksLine;
            }
            else
            {
                HookNotPassed(HookReasonPath);
            }
        }

        // --ai-tools (FR-9.7, docs/axKit/mcp.md MC-6.9): axKit's AI tools for this start of Claude, only while the user
        // has them on. Last on the line, since --mcp-config takes every word up to the next option. A line cmd.exe
        // would refuse because of the file's path starts without the tools rather than not at all.
        if (AiToolsArguments() is { Count: > 0 } tools)
        {
            var withTools = ClaudeLauncher.BuildCommandLine(claude, [.. arguments, .. tools]);
            if (withTools.Refusal is null)
            {
                line = withTools;
            }
            else
            {
                Log.Error("Claude Code started without axKit's AI tools: " + withTools.Refusal);
            }
        }

        if (line.Refusal is { } refusal)
        {
            HookStartFailed();
            Fail("Claude Code was not started. " + refusal);
            return;
        }

        _launchedFolder = _folder;
        KeepFilesOutOfGit();
        // The hooks' pipe and its secret reach a hook's process through Claude Code's environment (EG-2.8); null without hooks.
        var options = new PtyOptions(line.CommandLine, _folder!, _model.Columns, _model.Rows, ClaudeLauncher.BuildEnvironment(_hookChannel?.VariableValue));
        if (!StartProcess(options, "Claude"))
        {
            HookStartFailed();
            return;
        }

        ClaudeStarted();
    }

    /// <summary>A run of Claude Code has started, by whichever source feeds the window: the window's state for a new run.</summary>
    private void ClaudeStarted()
    {
        _readyAnnounced = false;
        // The core and the record's reader learn of the new run (MainForm.Flow.cs); nothing in a window the stream feeds.
        FlowProcessStarted();
        // The old Claude's background work is gone with it, unsignalled (FR-7.10).
        ForgetBackground();
        // Nothing of the old Claude's is handled: its question, if any, is gone with it.
        _wait = null;
        // Nor is anything of the old run still owed or still the user's doing (since 2026-11-27): a bell of the old
        // run that no settle took said Done in the new one, reply lines of the new run waited for a Done the old run
        // owed, and what the new Claude first asked counted as caused by an answer given to the old one.
        _bellPending = false;
        _bellNoMarks = false;
        _doneAwaitedSince = 0;
        _userActedAt = 0;
        // A question of the new Claude belongs to no slash command of the old one (QuestionRouter).
        _lastCommand = null;
        // Its drafts are counted afresh when it is ready: none of them is a draft of a turn of this run.
        _drafts.Opened(0);
        _draft = null;
        _settings.RememberFolder(_folder!);
        SaveSettings();
        UpdateStatus();
    }

    /// <summary>
    /// Claude Code's arguments for axKit's AI tools (FR-9.7, docs/axKit/mcp.md MC-6.9): in every window, screen and
    /// stream alike, since 2026-11-26 (until then only in a window started with <c>--ai-tools</c>, axHelp's
    /// conversation), and only while the user has the AI tools on, read from the sealed setting at this start. When the
    /// user's own Claude Code file already names the server <c>axkit</c> (MC-6.8, the user added axKit himself), that
    /// entry gives the tools and nothing is added; the log says only that it was found. Otherwise the file that names
    /// this very program file is written when it is missing or names another, and <c>--mcp-config</c> points Claude
    /// Code at it. Off, or a file not readable or writable: nothing, and Claude starts as it always did. Nothing is
    /// asked and nothing is turned on here.
    /// </summary>
    private IReadOnlyList<string> AiToolsArguments()
    {
        if (!AxKit.Core.Mcp.McpAccess.Enabled || PackageIdentity.ProgramPath is not { } program)
        {
            return [];
        }

        try
        {
            var named = UserNamesAxKitServer();
            if (named)
            {
                Log.Info("Claude Code's own configuration already names axKit's server: it is not named a second time");
            }

            var arguments = AxKit.Core.Mcp.McpClaudeCode.SessionArguments(toolsOn: true, named, AppPaths.McpClaudeConfigFile);
            if (arguments.Count > 0 && AxKit.Core.Mcp.McpClaudeCode.WriteConfig(AppPaths.McpClaudeConfigFile, program))
            {
                Log.Info("Wrote the file that gives Claude Code axKit's AI tools for this conversation");
            }

            return arguments;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Log.Error("axKit's AI tools were not given to Claude Code: " + ex.Message);
            return [];
        }
    }

    /// <summary>
    /// Whether the user's own Claude Code file (<c>.claude.json</c> in the profile, or in <c>CLAUDE_CONFIG_DIR</c> as
    /// Claude is started with it; a test run's own folder in a run) names the server <c>axkit</c> in its user scope
    /// (MC-6.8). Read only, nothing of it logged; a file that cannot be read counts as naming none.
    /// </summary>
    private static bool UserNamesAxKitServer()
    {
        ClaudeLauncher.BuildEnvironment().TryGetValue(AxClaude.Core.ClaudeCodeSettings.ConfigDirVariable, out var configDir);
        var file = AxKit.Core.Mcp.McpClaudeCode.UserFile(AutomationEnvironment.Home ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), configDir);
        try
        {
            return File.Exists(file) && AxKit.Core.Mcp.McpClaudeCode.NamesServer(File.ReadAllText(file));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// After Install Claude Code now (bundle AX-2.9): Claude where starting looks for it (the path set, or the usual
    /// places), or, when a path set no longer exists, where the installer puts it; that stale path is then cleared, so
    /// that the next start finds the new one (2026-10-24 review).
    /// </summary>
    private bool ClaudeInstalled()
    {
        var configured = _options.ClaudePath ?? _settings.ClaudePath;
        if (ClaudeCodeProgram.Find(configured) is not null)
        {
            return true;
        }

        if (configured is null || ClaudeCodeProgram.Find(null) is null)
        {
            return false;
        }

        _options = _options with { ClaudePath = null };
        _settings.ClaudePath = null;
        SaveSettings();
        return true;
    }

    /// <summary>Claude started once installed, unless one was started meanwhile (Ctrl+Shift+R while the install ran): never a second over the first.</summary>
    private void StartClaudeAfterInstall()
    {
        if (!ProcessRunning)
        {
            StartClaude();
        }
    }

    /// <summary>FR-1.9: where the app looked, the install command with a button that copies it, the install page, and Locate claude.exe.</summary>
    private void ShowClaudeNotFound(IReadOnlyList<string> candidates)
    {
        ShowNotice(new Notice("Claude Code was not found", HelpText.ClaudeNotFound(candidates),
        [
            new OverlayChoice("&Install Claude Code now", () => InstallProgram(ProgramInstall.ClaudeCode, ClaudeInstalled, StartClaudeAfterInstall)),
            new OverlayChoice("Copy install &command", () =>
            {
                CopyText(ClaudeLauncher.InstallCommand, "Copied. Paste it into PowerShell");
            }, StaysOpen: true),
            new OverlayChoice("&Open install instructions", () => OpenUrl(ClaudeLauncher.InstallUrl), StaysOpen: true),
            new OverlayChoice("&Locate claude.exe...", LocateClaude, StaysOpen: true),
            OverlayChoice.Close,
        ])
        {
            Unprompted = true,
        });
    }

    /// <summary>The standard file picker; a choice closes the notice, is saved as claudePath and started at once. Cancel keeps the notice.</summary>
    private void LocateClaude()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Locate claude.exe",
            Filter = "Claude Code (claude.exe;claude.cmd)|claude.exe;claude.cmd|Programs (*.exe;*.cmd)|*.exe;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        CloseNotice(null);
        _settings.ClaudePath = dialog.FileName;
        SaveSettings();
        StartClaude();
    }


    /// <summary>Claude's process ended, its last output in and the process stopped: the explanation shown, the default --continue retried as a new conversation (FR-9.1), or "Claude stopped".</summary>
    protected override void OnProcessExited(int code)
    {
        // FR-7.10: what ran in the background went with Claude; its going is no end to signal.
        ForgetBackground();
        if (_model.HideReplay)
        {
            // Claude ended before it was ready: whatever it printed is the explanation.
            _model.ShowHiddenLines();
        }

        if (_claudeArgs is null && !_nothingToContinue && _model.SaidNoConversationToContinue())
        {
            // The default --continue in a folder without a conversation: Claude says so and exits (FR-9.1).
            _nothingToContinue = true;
            Announce("Nothing to continue, new conversation", false);
            Relaunch("No conversation to continue here; starting a new one");
            return;
        }

        _model.AddSystemLine($"Claude stopped (exit code {code}). Press {BundleKeys.Text(AxClaudeKeys.RestartClaude)} to start it again.");
        Announce(StoppedText, false);
    }

    /// <summary>Said when Claude's process ended; a window put away keeps it for its bringing up (<see cref="SaidWhilePutAway"/>).</summary>
    private const string StoppedText = "Claude stopped";

    /// <summary>
    /// The attention timer stops with Claude; the console window stops the process, the writes and the control area.
    /// The core and the record's reader are told here and nowhere else that a Claude that ran is gone: every end passes
    /// here, and <see cref="OnProcessExited"/> only follows it (events-and-grounding.md EG-4.6). A start that still
    /// waited for Claude Code's version is over too. The hooks' pipe is stopped after the base has ended Claude Code,
    /// not before: Claude Code runs its hook for the session's end while it ends, and that hook then finds the pipe
    /// at once instead of waiting half a second for one that is gone; what it posts is dropped as the old run's (EG-2.7).
    /// </summary>
    protected override void StopProcess(bool terminateAtOnce = false)
    {
        _attention.Stop();
        HookCancelStart();
        var ran = ProcessRunning;
        var hooks = _hookChannel is not null;
        var since = Environment.TickCount64;
        // A Claude Code over its stream (MainForm.Stream.cs) is let go of first: the base's status bar and music ask
        // whether Claude runs. Nothing in a window the screen feeds.
        StopStream(terminateAtOnce);
        base.StopProcess(terminateAtOnce);
        // What an AI tool offered in this run goes with it (MainForm.Mcp.cs).
        ForgetOffers();
        HookStopChannel();
        if (ran)
        {
            // How long the window stood still while Claude Code ended: with hooks, the hook of the session's end
            // runs inside that time (events-and-grounding.md EG-2.5), and a slow end is found here.
            Log.Info($"Events: Claude's end took {Environment.TickCount64 - since} ms, hooks {(hooks ? "passed" : "not passed")}{(terminateAtOnce ? ", ended at once" : string.Empty)}");
            FlowProcessEnded();
        }
    }

    /// <summary>
    /// Stops Claude and starts it again with the current folder and arguments, after the system line. The
    /// conversation stays in the window (Restart Claude) unless <paramref name="clearConversation"/> is set (New
    /// session, a folder change; FR-8.3, FR-8.6), in which case the system line is the first line of the emptied window.
    /// A Claude in the middle of a turn is terminated at once (FR-1.8): the user chose the restart knowing it.
    /// </summary>
    private void Relaunch(string systemLine, bool clearConversation = false)
    {
        StopProcess(terminateAtOnce: ClaudeBusy);
        if (clearConversation)
        {
            _model.Clear();
        }
        else
        {
            _model.ResetScreen();
        }

        _model.AddSystemLine(systemLine);
        // The same folder with --continue replays the conversation the window already shows: hidden until Claude is
        // ready (FR-4.8). A cleared window shows the replay with its blocks, as at startup.
        _model.HideReplay = !clearConversation && _folder is not null && string.Equals(_folder, _launchedFolder, StringComparison.OrdinalIgnoreCase) && SessionHandover.Continues(ClaudeArgs);
        if (_folder is not null)
        {
            StartClaude();
        }
    }
}
