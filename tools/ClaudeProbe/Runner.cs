using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AxClaude.Core.Pty;
using AxKit.Core;
using AxKit.Core.Pty;

namespace Probe;

/// <summary>One run: Claude Code started as the app starts it, the three streams recorded on one clock, the script walked, everything ended.</summary>
internal static class Runner
{
    public static int PrintSettings(string configPath)
    {
        var config = Config.Load(configPath);
        var text = SettingsWriter.Build(config, 0, 0, Environment.ProcessPath ?? "probe.exe").ToJsonString(Json.Pretty);
        Redactor.Out(text.Replace("127.0.0.1:0/", "127.0.0.1:PORT/"));
        return 0;
    }

    public static int Run(string configPath)
    {
        Clock.Touch();
        var config = Config.Load(configPath);
        Directory.CreateDirectory(config.Cwd);
        Directory.CreateDirectory(config.Out);
        NativeTimer.Begin();

        using var hooksLog = new Jsonl(Path.Combine(config.Out, "hooks.jsonl"));
        using var fileLog = new Jsonl(Path.Combine(config.Out, "transcript-arrivals.jsonl"));
        using var ptyLog = new Jsonl(Path.Combine(config.Out, "pty-events.jsonl"));
        using var appLog = new Jsonl(Path.Combine(config.Out, "app.jsonl"));
        using var scriptLog = new Jsonl(Path.Combine(config.Out, "script.jsonl"));
        using var processLog = new Jsonl(Path.Combine(config.Out, "processes.jsonl"));
        using var recorder = new PtyRecorder(Path.Combine(config.Out, "pty.vt"));

        // The session files: Claude Code's configuration folder (CLAUDE_CONFIG_DIR, else .claude in the profile), its
        // projects folder, and in it the folder named after the working folder. Only that one folder is looked into.
        var configDir = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR") is { Length: > 0 } moved
            ? moved
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");
        if (config.Env.TryGetValue("CLAUDE_CONFIG_DIR", out var configured))
        {
            configDir = configured;
        }

        var projectName = TranscriptWatcher.ProjectFolderName(config.Cwd);
        var projectFolder = Path.Combine(configDir, "projects", projectName);
        var watcher = new TranscriptWatcher(fileLog, projectName, [config.Cwd, config.Out]);
        watcher.AddRoot(projectFolder, initial: true);
        watcher.Start();

        var listener = new HookListener(config.Hooks, hooksLog, watcher);
        listener.Start(config.Port);
        var httpPort = config.Hooks.ClosedPort ? HookListener.ClosedPort() : listener.Port;
        var probeExe = Environment.ProcessPath ?? throw new InvalidOperationException("The probe's own path is unknown.");
        var settingsPath = Path.Combine(config.Out, "settings.json");
        File.WriteAllText(settingsPath, SettingsWriter.Build(config, listener.Port, httpPort, probeExe).ToJsonString(Json.Pretty));

        string commandLine;
        string? claude = null;
        string? claudeVersion = null;
        var debugPath = Path.Combine(config.Out, "claude-debug.txt");
        if (config.Program is { Count: > 0 } program)
        {
            commandLine = ClaudeLauncher.JoinArguments(program);
        }
        else
        {
            claude = ClaudeCodeProgram.Find(config.Claude);
            if (claude is null)
            {
                Redactor.Err("Claude Code was not found.");
                return 2;
            }

            claudeVersion = ClaudeLauncher.VersionAsync(claude, TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
            var arguments = new List<string>();
            if (config.ScreenReader)
            {
                arguments.Add("--ax-screen-reader");
            }

            if (config.Model.Length > 0)
            {
                arguments.Add("--model");
                arguments.Add(config.Model);
            }

            arguments.Add("--strict-mcp-config");
            if (config.PassSettings)
            {
                arguments.Add("--settings");
                arguments.Add(settingsPath);
            }

            if (config.DebugFile)
            {
                arguments.Add("--debug-file");
                arguments.Add(debugPath);
            }

            arguments.AddRange(config.Args);
            var line = ClaudeLauncher.BuildCommandLine(claude, arguments);
            if (line.Refusal is not null)
            {
                Redactor.Err(line.Refusal);
                return 2;
            }

            commandLine = line.CommandLine;
        }

        // The app's environment for Claude Code, and never an update of the user's Claude Code from an experiment.
        var environment = ClaudeLauncher.BuildEnvironment();
        environment["DISABLE_AUTOUPDATER"] = "1";
        foreach (var pair in config.Env)
        {
            environment[pair.Key] = pair.Value;
        }

        var run = new JsonObject
        {
            ["startedWall"] = Clock.StartUtc.ToString("O"),
            ["probe"] = probeExe,
            ["claude"] = claude,
            ["claudeVersion"] = claudeVersion,
            ["commandLine"] = commandLine,
            ["cwd"] = config.Cwd,
            ["out"] = config.Out,
            ["size"] = $"{config.Cols}x{config.Rows}",
            ["listenerPort"] = listener.Port,
            ["httpHookPort"] = httpPort,
            ["closedPort"] = config.Hooks.ClosedPort,
            ["projectFolder"] = projectFolder,
            ["hookPlan"] = config.Hooks.Mode == "none" ? "none" : config.Hooks.Resolved(),
            ["environmentAdded"] = new JsonObject
            {
                ["TERM"] = environment["TERM"],
                ["CLAUDE_AX_STARTUP_QUIET_MS"] = environment["CLAUDE_AX_STARTUP_QUIET_MS"],
                ["CLAUDE_AX_PREPARK_MS"] = environment["CLAUDE_AX_PREPARK_MS"],
                ["CLAUDE_CODE_DISABLE_ALTERNATE_SCREEN"] = environment["CLAUDE_CODE_DISABLE_ALTERNATE_SCREEN"],
                ["DISABLE_AUTOUPDATER"] = "1",
            },
        };
        File.WriteAllText(Path.Combine(config.Out, "run.json"), run.ToJsonString(Json.Pretty));
        Redactor.Err($"[probe] Claude Code {claudeVersion ?? "(another program)"}; listener on 127.0.0.1:{listener.Port}; output {config.Out}");
        Redactor.Err($"[probe] command: {commandLine}");

        using var model = new ModelWatch(config.Cols, config.Rows, ptyLog, appLog);
        var scanner = new VtScanner(ptyLog);
        var exited = new ManualResetEventSlim();
        int? exitCode = null;
        var host = PtyHost.Start(new PtyOptions(commandLine, config.Cwd, config.Cols, config.Rows, environment),
            data =>
            {
                var t = Clock.Ms;
                var offset = recorder.Write(data, t);
                ptyLog.Write(new JsonObject { ["t"] = t, ["k"] = "chunk", ["offset"] = offset, ["bytes"] = data.Length });
                scanner.Feed(data, t, offset);
                model.Feed(data, t);
            },
            code =>
            {
                exitCode = code;
                scriptLog.Write(new JsonObject { ["t"] = Clock.Ms, ["wall"] = Clock.Wall, ["k"] = "exit", ["code"] = code });
                exited.Set();
            });
        scriptLog.Write(new JsonObject { ["t"] = Clock.Ms, ["wall"] = Clock.Wall, ["k"] = "started", ["pid"] = host.ProcessId });
        var tree = new ProcessTree(host.ProcessId, processLog);
        var script = new ScriptRunner(config, host, model, recorder, listener, watcher, scriptLog, exited);

        // The probe ended from outside (Ctrl+C, a closed console): the console of the child closes with it.
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            script.Abort("Ctrl+C");
        };

        try
        {
            script.RunSteps(config.Script);
        }
        catch (Exception ex)
        {
            scriptLog.Write(new JsonObject { ["t"] = Clock.Ms, ["k"] = "script-error", ["error"] = ex.GetType().Name + ": " + ex.Message });
            Redactor.Err("[probe] script error: " + ex.Message);
        }

        var closedByProbe = !host.HasExited;
        if (closedByProbe)
        {
            scriptLog.Write(new JsonObject { ["t"] = Clock.Ms, ["wall"] = Clock.Wall, ["k"] = "closing", ["why"] = script.Aborted ?? "the script ended" });
        }

        host.Dispose(); // Closes the console, which ends the client; it is terminated if it lingers two seconds.
        model.Tick(force: true);

        // What comes late: a SessionEnd hook, the last lines of the file.
        Thread.Sleep(config.LingerMs);
        tree.Stop();
        var leftovers = tree.EndLeftovers();
        var files = watcher.Finish(config.Out);
        listener.Stop();
        NativeTimer.End();

        var hookCounts = new JsonObject();
        foreach (var pair in listener.Counts())
        {
            hookCounts[pair.Key] = pair.Value;
        }

        var summary = new JsonObject
        {
            ["durationMs"] = Clock.Ms,
            ["exitCode"] = exitCode,
            ["closedByProbe"] = closedByProbe,
            ["aborted"] = script.Aborted,
            ["timeouts"] = script.Timeouts,
            ["sessionId"] = listener.SessionId,
            ["ptyBytes"] = recorder.Length,
            ["hookRequests"] = hooksLog.Count,
            ["hooks"] = hookCounts,
            ["transcriptFiles"] = files,
            ["processesUnderClaude"] = tree.Seen,
            ["leftoverProcessesEnded"] = leftovers,
        };
        File.WriteAllText(Path.Combine(config.Out, "summary.json"), summary.ToJsonString(Json.Pretty));
        Redactor.Out(summary.ToJsonString(Json.Pretty));
        return script.Aborted is null ? 0 : 3;
    }
}

/// <summary>Walks the input script: waits, text, keys, the end of the session. Every step is a record of script.jsonl.</summary>
internal sealed class ScriptRunner
{
    private readonly Config _config;
    private readonly PtyHost _host;
    private readonly ModelWatch _model;
    private readonly PtyRecorder _recorder;
    private readonly HookListener _hooks;
    private readonly TranscriptWatcher _watcher;
    private readonly Jsonl _log;
    private readonly ManualResetEventSlim _exited;
    private double _since;
    private long _rawMark;
    private bool _ended;

    public ScriptRunner(Config config, PtyHost host, ModelWatch model, PtyRecorder recorder, HookListener hooks, TranscriptWatcher watcher, Jsonl log, ManualResetEventSlim exited)
    {
        _config = config;
        _host = host;
        _model = model;
        _recorder = recorder;
        _hooks = hooks;
        _watcher = watcher;
        _log = log;
        _exited = exited;
    }

    /// <summary>Why the rest of the script was dropped; null while it runs.</summary>
    public string? Aborted { get; private set; }

    public int Timeouts { get; private set; }

    private bool Expired => Clock.Ms > _config.MaxRunMs;

    public void Abort(string why) => Aborted ??= why;

    public void RunSteps(JsonArray steps)
    {
        foreach (var node in steps)
        {
            if (Aborted is not null || _ended)
            {
                return;
            }

            if (Expired)
            {
                Abort($"maxRunMs ({_config.MaxRunMs}) passed");
                return;
            }

            if (node is JsonObject step)
            {
                RunStep(step);
            }
        }
    }

    private void RunStep(JsonObject step)
    {
        var timeout = Json.Int(step, "timeout", 30_000);
        if (step["comment"] is not null && step.Count == 1)
        {
            return;
        }

        if (step["wait"] is not null)
        {
            Note("wait", Json.Int(step, "wait", 0).ToString());
            Sleep(Json.Int(step, "wait", 0));
        }
        else if (Json.Str(step, "waitFor") is { } forPattern)
        {
            var regex = new Regex(forPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var kind = Json.Str(step, "kind");
            var visible = Json.Bool(step, "visible", false);
            var found = Wait("waitFor", forPattern, timeout, () => regex.IsMatch(_model.Text(120, visible, kind)));
            Branch(step, found);
        }
        else if (Json.Str(step, "if") is { } ifPattern)
        {
            var found = new Regex(ifPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline).IsMatch(_model.Text(120, false, null));
            Note("if", $"{ifPattern} => {found}");
            if ((found ? step["then"] : step["else"]) is JsonArray branch)
            {
                RunSteps(branch);
            }
        }
        else if (Json.Str(step, "waitRaw") is { } rawPattern)
        {
            var regex = new Regex(rawPattern);
            Branch(step, Wait("waitRaw", rawPattern, timeout, () => regex.IsMatch(_recorder.TextSince(_rawMark))));
        }
        else if (Json.Str(step, "waitStatus") is { } statuses)
        {
            var wanted = statuses.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var stable = Json.Int(step, "stableMs", 0);
            double? heldSince = null;
            Branch(step, Wait("waitStatus", statuses, timeout, () =>
            {
                var now = _host.HasExited ? "exited" : _model.Status();
                if (!wanted.Contains(now) || (now != "exited" && _model.Dirty))
                {
                    heldSince = null;
                    return false;
                }

                heldSince ??= Clock.Ms;
                return Clock.Ms - heldSince.Value >= stable;
            }, endsAtExit: !wanted.Contains("exited")));
        }
        else if (Json.Str(step, "waitHook") is { } hookPattern)
        {
            var regex = new Regex("^(?:" + hookPattern + ")$");
            var count = Json.Int(step, "count", 1);
            Branch(step, Wait("waitHook", hookPattern, timeout, () => _hooks.CountSince(regex, _since) >= count, endsAtExit: false));
        }
        else if (Json.Str(step, "waitFile") is { } filePattern)
        {
            var regex = new Regex(filePattern);
            Branch(step, Wait("waitFile", filePattern, timeout, () => _watcher.AnySince(regex, _since), endsAtExit: false));
        }
        else if (step["waitQuiet"] is not null)
        {
            var quiet = Json.Int(step, "waitQuiet", 1000);
            Branch(step, Wait("waitQuiet", quiet.ToString(), timeout, () => Clock.Ms - _recorder.LastAt >= quiet));
        }
        else if (Json.Str(step, "send") is { } text)
        {
            Input("send", text, text);
        }
        else if (Json.Str(step, "type") is { } typed)
        {
            // One write per character, as keystrokes arrive: the same text in one write is a paste to Claude Code.
            var gap = Json.Int(step, "perCharMs", 30);
            var first = true;
            foreach (var rune in typed.EnumerateRunes())
            {
                Input(first ? "type" : "type-more", rune.ToString(), rune.ToString(), keepSince: !first);
                first = false;
                Sleep(gap);
            }
        }
        else if (step["keys"] is { } keys)
        {
            var names = keys is JsonArray list ? list.Select(item => item!.GetValue<string>()).ToList() : [keys.GetValue<string>()];
            var gap = Json.Int(step, "gapMs", 150);
            for (var i = 0; i < names.Count; i++)
            {
                var bytes = Keys.Bytes(names[i]) ?? throw new InvalidOperationException($"Unknown key name: {names[i]} (probe keys lists them).");
                Input("key", names[i], bytes, keepSince: i > 0);
                if (i + 1 < names.Count)
                {
                    Sleep(gap);
                }
            }
        }
        else if (Json.Str(step, "bytes") is { } hex)
        {
            Input("bytes", hex, System.Text.Encoding.UTF8.GetString(Convert.FromHexString(hex.Replace(" ", string.Empty))));
        }
        else if (Json.Str(step, "message") is { } message)
        {
            Message(step, message);
        }
        else if (step["mark"] is { } mark)
        {
            _since = Clock.Ms;
            _rawMark = _recorder.Length;
            _log.Write(new JsonObject { ["t"] = _since, ["wall"] = Clock.Wall, ["k"] = "mark", ["label"] = mark.ToString() });
        }
        else if (step["writeFile"] is JsonObject file)
        {
            var relative = Json.Str(file, "path") ?? throw new InvalidOperationException("writeFile needs \"path\".");
            var full = Path.GetFullPath(Path.Combine(_config.Cwd, relative));
            if (!full.StartsWith(_config.Cwd, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("writeFile writes only inside the working folder.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            var content = Json.Str(file, "text") ?? string.Empty;
            _since = Clock.Ms;
            if (Json.Bool(file, "append", false))
            {
                File.AppendAllText(full, content);
            }
            else
            {
                File.WriteAllText(full, content);
            }

            _log.Write(new JsonObject { ["t"] = _since, ["wall"] = Clock.Wall, ["k"] = "write-file", ["path"] = relative, ["chars"] = content.Length });
        }
        else if (Json.Str(step, "end") is { } how)
        {
            End(how, Json.Int(step, "timeout", 20_000));
        }
        else
        {
            throw new InvalidOperationException("Unknown script step: " + step.ToJsonString(Json.Line));
        }
    }

    /// <summary>A message as the window sends it: the app's exchange block in the model, the text, 100 ms, Enter, and Enter again while the prompt still holds the text 1.5 s later (FR-2.1), three times at most.</summary>
    private void Message(JsonObject step, string message)
    {
        if (Json.Bool(step, "appBlock", true))
        {
            _model.Send(message);
        }

        Input("message", message, message);
        Sleep(Json.Int(step, "enterDelayMs", 100));
        Input("message-enter", "Enter", "\r", keepSince: true);
        if (!Json.Bool(step, "retryEnter", true))
        {
            return;
        }

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Sleep(1500);
            if (_host.HasExited || !_model.PromptHeld(message))
            {
                break;
            }

            Input("enter-again", $"Enter ({attempt})", "\r", keepSince: true);
        }
    }

    private void End(string how, int timeout)
    {
        Note("end", how);
        switch (how)
        {
            case "exit":
                Input("send", "/exit", "/exit");
                Sleep(300);
                Input("key", "Enter", "\r", keepSince: true);
                if (!_exited.Wait(timeout))
                {
                    Timeouts++;
                    _log.Write(new JsonObject { ["t"] = Clock.Ms, ["k"] = "waited", ["verb"] = "end", ["ok"] = false, ["ms"] = timeout });
                }

                break;
            case "kill":
                _host.Kill();
                _exited.Wait(5000);
                break;
            case "close":
                // The console is closed by the run's end, right after the script.
                break;
            default:
                throw new InvalidOperationException("end takes exit, close or kill.");
        }

        _ended = true;
    }

    private void Branch(JsonObject step, bool found)
    {
        if (found)
        {
            if (step["then"] is JsonArray then)
            {
                RunSteps(then);
            }

            return;
        }

        if (step["else"] is JsonArray otherwise)
        {
            RunSteps(otherwise);
            return;
        }

        if (Json.Str(step, "onTimeout") != "continue")
        {
            Abort("a wait timed out: " + step.ToJsonString(Json.Line));
        }
    }

    private bool Wait(string verb, string detail, int timeout, Func<bool> condition, bool endsAtExit = true)
    {
        var start = Clock.Ms;
        double? exitedAt = null;
        while (true)
        {
            bool ok;
            try
            {
                ok = condition();
            }
            catch (RegexMatchTimeoutException)
            {
                ok = false;
            }

            var now = Clock.Ms;
            if (ok)
            {
                _log.Write(new JsonObject { ["t"] = now, ["wall"] = Clock.Wall, ["k"] = "waited", ["verb"] = verb, ["detail"] = detail, ["ok"] = true, ["ms"] = Math.Round(now - start, 1) });
                return true;
            }

            // After the exit nothing more comes to the screen; a hook or a file line gets two seconds more.
            if (_host.HasExited)
            {
                exitedAt ??= now;
            }

            var over = now - start >= timeout || Expired || Aborted == "Ctrl+C" || (exitedAt is { } at && now - at >= (endsAtExit ? 500 : 2000));
            if (over)
            {
                Timeouts++;
                _log.Write(new JsonObject { ["t"] = now, ["wall"] = Clock.Wall, ["k"] = "waited", ["verb"] = verb, ["detail"] = detail, ["ok"] = false, ["ms"] = Math.Round(now - start, 1), ["exited"] = _host.HasExited });
                Redactor.Err($"[probe t={now:F0}] TIMEOUT {verb} {detail}");
                return false;
            }

            Thread.Sleep(10);
        }
    }

    private void Input(string what, string shown, string bytes, bool keepSince = false)
    {
        var before = Clock.Ms;
        if (!keepSince)
        {
            _since = before;
        }

        _host.Write(bytes);
        var after = Clock.Ms;
        _log.Write(new JsonObject
        {
            ["t"] = before, ["wall"] = Clock.Wall, ["k"] = "send", ["what"] = what, ["text"] = shown,
            ["hex"] = Txt.Hex(bytes), ["tWritten"] = after,
        });
        Redactor.Err($"[probe t={before:F0}] {what} {Txt.Clip(shown, 80)}");
    }

    private void Note(string verb, string detail) =>
        _log.Write(new JsonObject { ["t"] = Clock.Ms, ["wall"] = Clock.Wall, ["k"] = "step", ["verb"] = verb, ["detail"] = detail });

    private void Sleep(int ms)
    {
        var until = Clock.Ms + ms;
        while (Clock.Ms < until && Aborted != "Ctrl+C")
        {
            Thread.Sleep((int)Math.Clamp(until - Clock.Ms, 1, 50));
        }
    }
}
