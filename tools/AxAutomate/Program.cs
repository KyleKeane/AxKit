// AxAutomate: runs scripts of user steps against axKit's windows through their automation channel
// (docs/axKit/automation.md, tier 2). The windows are started on a desktop of the runner's own ("axkit-tests"), so that
// they never take the user's keyboard or foreground; the keys go into each window's own message queue, the sounds are
// recorded and not played, and the windows keep their settings and logs in the run's own folder (AXKIT_HOME).
//
// Usage:
//   AxAutomate <script or folder of scripts> [--exe <AxKit.exe> | --build] [--report <file.md>] [--keep] [--nvda]
//              [--repository <folder>] [--inventory check|write|rewrite|off] [--traces check|write|off] [--record]
//              [--convert] [--ai-tools]
//   AxAutomate --reference [--check] [--repository <folder>]
//   --build      builds src\AxKit in Release into the run's own folder first (the running app's bin is never touched)
//   --exe        the AxKit.exe to drive; there is no default: a run names its program or builds it
//   --report     writes every script's steps and its whole timeline, with times, to a Markdown file; with --traces
//                check or write given by name, also every script's trace under its steps, section by section, the
//                setup among them
//   --nvda       the NVDA tier (tier 3): the windows open on the user's own desktop and come to the front, the keys go
//                through SendInput as a keyboard's do, and NVDA's speech is read from its capture add-on
//                (tools/nvda-capture). It takes over the machine: only at the user's word, with the user away.
//                When no window of the run is in front the runner takes the foreground itself (one Alt key press);
//                when the run's first window cannot come to the front at all, the run stops with one line, exit 2.
//   --keep       keeps %TEMP%\axkit-automate\<process id>, the run's folder ({temp} in a script), with its logs
//   --repository the repository whose docs/axKit/window-commands.json and tests/traces the run reads and writes; by
//                default the folder above this program that holds AxKit.sln, and none when no folder does (a runner
//                built into %TEMP%): without one the command inventory and the traces are off
//   --inventory  the command inventory, docs/axKit/window-commands.json (docs/axKit/user-workflows.md WF-2), from the
//                snapshots of the windows' commands that every "check commands" and "do" step takes:
//                check    (the default) every snapshot held against the file; what a window lists that the file does
//                         not hold as it is is named after the summary, under "The command inventory is stale"; an
//                         app the file holds no row for is named once and not checked
//                write    every snapshot merged into the file, written at the end when it changed; only the scripts
//                         with a "check commands" or "do" step play, and a line says how many were passed over
//                rewrite  as write, and an app every script of which passed, one snapshot of it without a notice,
//                         keeps only the rows this run saw: how a command that left a window leaves the file
//                off      nothing (a build that is not the repository's; tools\upstream.ps1 -Verify)
//   --traces     the kept traces (docs/axKit/user-workflows.md WF-5, TraceRun): a trace is, for each section of a
//                script, each input step as a line ("do Find = ctrl+f", "type apple") and under it what the windows
//                said, without times, the run's folder written {temp}. A trace is kept in the repository only for a
//                section marked "# Stable", in tests/traces/<folder>/<script>.trace.txt; a script is found there by
//                its own path, or, played from elsewhere, by its label. The trace is collected only for a script with
//                such a section, and for every script when --report and --traces check or write are both given by
//                name: each window is then asked once more after each line for what it recorded. A run that names no
//                --traces plays every script without such a section exactly as before.
//                check    (the default) after a script whose steps passed, the trace of each "# Stable" section is
//                         held against its kept section: one that differs, or has none, fails its section and so the
//                         script ("FAIL after the steps: the trace of the stable goal ... differs at its line n: kept
//                         ...; now ..."), and the sections' lines are said once more as they then stand; the run's
//                         last lines count the stable sections equal, differing and without a kept trace
//                write    after a script that passed, its file is written whole, or deleted when the script has no
//                         "# Stable" section; read the file whole before it is committed (WF-5.6)
//                off      nothing is collected, compared or written; always so with --nvda and without a repository
//                         (check and write are refused with --nvda), and what tools\upstream.ps1 -Verify runs with
//   --reference  plays nothing (no build, no desktop, no lock): writes the workflow pages for people
//                (docs/axKit/user-workflows.md WF-7, ReferenceRun) from every script of tests\scripts and
//                tests\nvda-scripts and the command inventory: docs/axKit/workflows.md, the index;
//                docs/axKit/workflows/<group>.md, each goal's steps; docs/axKit/window-commands.md, every command. Each
//                page with CRLF, UTF-8 without a byte order mark, one line per page written; a page of
//                docs/axKit/workflows no group makes any more is deleted. tools\workflows.ps1 runs it.
//     --check    writes and deletes nothing, compares line ends aside: exit 0 when every page is current, 1 naming each
//                page that is not (missing, different, left over). A script without its label: exit 1, named.
//   --record     the workflow record (RecordRun, WF-6): after the last script every section the run played is entered
//                into the repository's docs/axKit/workflow-record.json for the run's tier (desktop, nvda under
//                --nvda, ai-tools under --ai-tools) with its result, the version, the commit and the date; one line
//                says how many.
//   --convert    the converter (ConvertRun, WF-10), on the hidden desktop alone: moves a script's command keys to do
//                lines. Each script is played with the window asked for its commands before every press of a keys
//                line that carries ctrl or alt or is a function key (alt+f4 aside); a press becomes "do <name>" only
//                when exactly one row of the window's list at that moment has it as its key and would run by it now,
//                and do would press that very key. Everything else stays keys: plain keys, a further key of a
//                command, a key in a notice. A keys line of several presses becomes several lines in the same order,
//                an iffound before it on each; comments, blank lines and line ends stay. A script that did not pass
//                is left alone. A rewritten script is played once more as it then stands: when it does not pass, the
//                file is put back byte for byte and named after the summary. The rows the new do lines ran are merged
//                into the command inventory, written at the end when it changed. One line per script
//                ("<script>: 3 presses converted", or why not), and a count after the summary. No traces (--traces
//                check or write are refused with it); refused with --nvda, with --inventory write or rewrite and
//                without a repository. Afterwards: the folder once more without the option and with --record, then
//                tools\workflows.ps1.
//   --ai-tools   the AI tools mode (AiToolsMode, WF-9), on the hidden desktop alone: the scripts' do steps run through
//                the AI tools, with the runner as the MCP client it is in a script's own mcp steps. Only a script of
//                one window (one start; no launch, adopt or use), of an app the AI tools read (not axPass, not
//                axClip), with a do step and no mcp step plays; a line says how many were passed over. Right before
//                such a script's first input step after its start, the access is turned on as a user turns it on, by
//                the steps of tests\scripts\mcp\02-run-commands.txt: the menu item, the pairing answered in the
//                window's notice, Let AI tools run commands; the report shows them once, each marked "AI tools:".
//                Nothing is written into a settings file and no gate of docs/axKit/mcp.md is passed by. Then every do
//                step whose command the tools may run without asking (a menu command that is for them and is not
//                marked act, no notice showing) is run with axkit_window_run by its label and menus, and the step
//                waits for the tool's answer ("-> AI tools: Find"); a refusal fails the step with the tool's words,
//                but "The user is working in this window right now", after which the call is sent again 3.5 s later,
//                three times at most. Every other step plays by the keyboard as written. A section's line of the
//                report ends ", through the AI tools: 3 of 5 inputs"; with --record the sections with such a step are
//                entered for the tier ai-tools and nothing for desktop. A step that fails only here is the tier's
//                finding: no script and no window is changed for it. No traces (--traces check or write are refused
//                with it); refused with --nvda and with --convert.
//   A folder runs every *.txt under it, one after another, each with fresh windows and a fresh {temp}. The exit code
//   (WF-11.5) is 0 when every script passed and nothing is stale, 1 when a script failed (a "# Stable" section whose
//   trace differs from the kept one, or has none, fails its script), 2 when the run could not
//   start (the command line, the build, the program, another window run that did not end within the lock's hour), 3
//   when every script passed and the command inventory is stale.
//   One window run plays at a time on a computer (RunLock): the clipboard and the system hotkeys are shared, so a
//   second run waits for the first, for an hour at most, and says once that it waits.
//
// Script steps, one per line, '#' starts a comment:
//   mkdir <name>              a fresh folder {temp}\<name>
//   file <name> <text>        a file {temp}\<name> holding the text (\n for a new line); "<name>" in quotes with a space
//   silence <name> <ms>       a silent WAV file {temp}\<name> of that length, for axMedia without a sound
//   run <command line>        a command through cmd.exe in {temp}, waited for up to 60 s (git init, git commit ...)
//   start <arguments>         AxKit.exe with these arguments, on the test desktop; the window becomes the current one
//   launch <arguments>        AxKit.exe on the test desktop without waiting for a window: a start that hands over to an
//                             open window (axMedia, axPreview) and ends; waits up to 30 s for the process to end
//   adopt                     waits up to 15 s for an axKit window the app itself started (Ctrl+N, New window, Show
//                             in) and makes it the current one; it is closed at the end like the others
//   use <n>                   the n-th window started in the script (from 1) becomes the current one
//   keys <key> ...            key presses: ctrl+shift+p, escape, down, enter, f1, alt+f4 ...
//   type <text>               characters into the focused control
//   menu <name>               a menu command by the command palette's name ("Force Claude to restart")
//   do <name>                 a command of the current window by its name, run as a user runs it: the window is asked
//                             for its commands, and the key that runs this one now is pressed ("-> key ctrl+f"); a
//                             command with no key runs by its menu item ("-> menu item"). The name is the command's
//                             label ("Find"), or, where two commands share a label, the label and its menus
//                             ("Heading, Navigation, Next"). Fails with nothing pressed when no key of the command
//                             acts where the focus is, when a notice shows, and when the name finds no one command
//                             (docs/axKit/user-workflows.md WF-4). Not for a command that closes the window (close,
//                             keys alt+f4), a control's or a notice's own keys (keys, type)
//   said                      prints the timeline since the last look (! interrupting, ♪ sound, → focus, · value)
//   text | list | commands | state | focus   prints the focused text's lines, the focused list's items, the menus'
//                             commands, the window's state (title, notice and its text, status bar), the focus
//   check commands            fails when the window's command list has a problem: a key the F1 text does not name, a
//                             command that never runs because an earlier one takes its key everywhere (app-kit AK-2.4)
//   mcp start                 (after a start) the runner becomes an MCP client: AxKit.exe mcp as its child, with the
//                             run's settings and key (docs/axKit/mcp.md MC-6)
//   mcp call <tool> [<json>]  sends a tools/call without waiting; {window} is the id the last axkit_windows answer named
//   expect mcp <text>         waits up to 60 s for an answer holding the text (a refusal reads "error: ...")
//   refuse mcp <text>         fails when an answer waiting holds the text after 2 s (an axPass window listed)
//   expect said <text>        waits up to 60 s until the timeline holds the text (an announcement, "♪ Edge" for a
//                             sound, "→ Conversation" for a focus move); takes it and what came before it in its
//                             stream: the sounds are one stream, the rest another, since a sound plays at once while
//                             its text may wait to be joined to another (AX-7.3); a text later in the announcement the
//                             last expectation matched counts too ("Sent. hello. Done" answers Sent, then Done)
//   expect focus|state|list|text|commands <text>   waits up to 10 s until that answer holds the text
//   refuse said <text>        fails when the timeline holds the text within 2 s (what must not be said)
//   refuse focus|state|list|text|commands <text>   fails when that answer holds the text after 2 s
//   expect closed             waits up to 10 s for the current window's process to end (a close without a question)
//   allow error <text>        a log's ERROR line that holds the text does not fail the run (a refused file the spec
//                             wants reported)
//   expect quiet <ms>         fails when anything is announced within the time (sounds and values aside)
//   try said <text>           as expect said, up to 15 s, and never fails: it records whether the text came
//   expect spoken <text>      (--nvda) waits up to 15 s until NVDA has spoken the text; takes it and what came before
//   refuse spoken <text>      (--nvda) fails when NVDA speaks the text within 2 s
//   spoken                    (--nvda) prints what NVDA spoke since the last look, "[cancel]" where it stopped speech
//   iffound <step>            the step, only when the last try found its text (a trust prompt that may not come)
//   wait <ms>                 waits
//   close                     closes the current window as Alt+F4 does
// Goals and checks (docs/axKit/user-workflows.md WF-3): a comment line "# Goal: <title>" starts a goal, what a user
// gets done by the steps under it, and "# Check: <title>" a check, steps that only test; each runs to the next such
// line or to the script's end, and the lines before the first are the setup. A failing step still stops its script.
// For a script with such lines the console says "-- Setup", "-- Goal: <title>" or "-- Check: <title>" before a
// section's first step, and after the last step "Goals: <n> passed[, <n> failed][, <n> not run]" with one line per
// section; the run's last lines add "Goals:" with the counts over all scripts. A script without one is one goal and
// prints as before.
// The clipboard's text is read before the first script and put back after the last (ClipboardText): the run's desktop
// shares the clipboard with the user's. A clipboard without text is left as the run left it.
// After the last step every window is closed, and the run fails when a window's process ended on its own or a log of
// the run holds an ERROR line. Any other process of the run's AxKit.exe (by its full path) that descends from the
// runner or has a window on the run's desktop, and started after the runner, is then ended with its tree, a line in the
// report.
//
// The steps, the run on its own desktop and the report's lines are the engine of src/AxAutomate.Core (ScriptRunner, Run,
// docs/axAutomate/SPEC.md AU-5.1), which axAutomate's window plays workflows through as well; this program is the
// command line over it. A .workflow.json file runs too, written as the same lines (ScriptLines, AU-5.2).

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AxAutomate.Core;

namespace AxAutomate;

internal static class Program
{
    private static int Main(string[] args)
    {
        // The stand-in of Claude Code (StandIn.cs, docs/axClaude/events-and-grounding.md EG-11.3): a script's claude.cmd
        // calls this program through AXKIT_STANDIN, which every window, cmd.exe and stand-in of the run inherits.
        if (args.Length > 0 && args[0] == StandIn.Verb)
        {
            return StandIn.Run(args[1..]);
        }

        Environment.SetEnvironmentVariable(StandIn.Variable, Environment.ProcessPath);

        if (args.Length == 0)
        {
            Console.Error.WriteLine(Usage);
            return CannotStart;
        }

        Console.OutputEncoding = Encoding.UTF8;

        // The workflow pages (ReferenceRun.cs, docs/axKit/user-workflows.md WF-7.1): this form plays nothing, so it
        // comes before anything of a run: no build, no desktop, no lock.
        if (args[0] == RunOptions.ReferenceOption)
        {
            var (repository, checkPages, refused) = RunOptions.ReadReference(args);
            if (repository is null)
            {
                Console.Error.WriteLine($"{refused} {Usage}");
                return CannotStart;
            }

            return ReferenceRun.Run(repository, checkPages, Console.WriteLine);
        }

        // Every option read once (Options.cs, docs/axKit/user-workflows.md section 6.4).
        var (options, refusal) = RunOptions.Read(args);
        if (options is null)
        {
            Console.Error.WriteLine($"{refusal} {Usage}");
            return CannotStart;
        }

        var (target, exe, build, reportPath, keep) = (options.Target, options.Exe, options.Build, options.Report, options.Keep);
        Run.Nvda = options.Nvda;

        // The process id and the time: a reused process id never meets an old folder (git's read-only objects in it
        // stopped a run once).
        var root = Path.Combine(Path.GetTempPath(), "axkit-automate", $"{Environment.ProcessId}-{DateTime.Now:HHmmss}");
        Directory.CreateDirectory(root);
        if (build)
        {
            // The build keeps its own fallback to the current folder (6.4); the inventory has none.
            exe = Build(options.Repository ?? Environment.CurrentDirectory, Path.Combine(root, "build"));
            if (exe is null)
            {
                return CannotStart;
            }
        }

        // No default program (docs/axKit/repairs.json R-32, 2026-11-13): the old default, src\AxKit\bin\Release, was
        // whatever build happened to lie there, and a run against a stale one read exactly like a real one, twice in
        // one week. The run names the program it drives, or builds it.
        if (exe is null)
        {
            Console.Error.WriteLine($"Say which AxKit.exe to drive: --build builds the sources as they are now, --exe names a built one. {Usage}");
            return CannotStart;
        }

        exe = Path.GetFullPath(exe);
        if (!File.Exists(exe))
        {
            Console.Error.WriteLine($"AxKit.exe not found: {exe}. Use --build, or --exe with a built AxKit.exe. {Usage}");
            return CannotStart;
        }

        var scripts = Directory.Exists(target)
            ? Directory.GetFiles(target, "*.txt", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(target, "*" + WorkflowFile.Extension, SearchOption.AllDirectories))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList()
            : [target];

        // The command inventory (InventoryRun, WF-2.7 to WF-2.10): read now; under write or rewrite only the scripts
        // that list a window's commands play.
        if (InventoryRun.Start(options.Inventory, options.Repository, root) is { } unreadable)
        {
            Console.Error.WriteLine(unreadable);
            return CannotStart;
        }

        scripts = InventoryRun.Playing(scripts, Console.WriteLine);

        // The traces (TraceRun, WF-5.4 to WF-5.8): off on the NVDA tier and without a repository (Options.cs).
        TraceRun.Start(options.Traces, options.Repository, options.TraceReport);

        // The workflow record (RecordRun, WF-6): entered after the last script, under --record.
        RecordRun.Start(options.Record, options.Repository, options.Nvda, options.AiTools, exe);

        // The AI tools mode (WF-9.2): only the scripts it covers play; the keyboard run is where the others are checked.
        if (options.AiTools)
        {
            var covered = scripts.Where(script => !script.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase) && AiToolsMode.Covers(File.ReadAllLines(script))).ToList();
            if (covered.Count < scripts.Count)
            {
                Console.WriteLine($"--ai-tools: {scripts.Count - covered.Count} of {scripts.Count} script(s) passed over, since the mode plays a script of one window, of an app the AI tools read, with a do step and no mcp step.");
            }

            scripts = covered;
        }

        // The converter (ConvertRun, WF-10): each script's command keys moved to do lines, proved by a second play.
        ConvertRun.Start(options.Convert);

        // A run that ended without its end (the runner killed, its console closed) left NVDA's capture on, and the add-on
        // then wrote everything NVDA spoke into a file (R-33): an earlier run's flag goes first, in every run.
        File.Delete(Speech.FlagPath);

        // Which build this run drives, in the console and in the report's first lines (R-32).
        var built = $"AxKit.exe {FileVersionInfo.GetVersionInfo(exe).ProductVersion ?? "of unknown version"}, built {File.GetLastWriteTime(exe):yyyy-MM-dd HH:mm}";
        Console.WriteLine(built);
        var report = new StringBuilder($"# AxAutomate report\n\n{DateTime.Now:yyyy-MM-dd HH:mm}, {exe}\n{built}\n\n");
        var failed = new List<string>();
        var notHeard = new List<string>();
        var goalResults = new List<GoalResult>();
        var anySections = false;

        // One window run at a time on this computer (RunLock, docs/axKit/user-workflows.md WF-D-29): taken after the
        // build and before the first script, released in the finally that puts the clipboard back.
        var runLock = RunLock.Take(TimeSpan.FromMinutes(60), Console.WriteLine);
        if (runLock is null)
        {
            Console.Error.WriteLine(RunLock.GaveUp);
            return CannotStart;
        }

        var clock = Stopwatch.StartNew();

        // The run's desktop shares the clipboard with the user's, and some scripts write it (axClip's): its text is
        // read before the first script and put back after the last, whatever ends the run. No text there (empty, a
        // picture, files): nothing is put back.
        var clipboardBefore = ClipboardText.Read();
        try
        {
            if (Run.Nvda)
            {
                // NVDA records only while this file exists (tools/nvda-capture); the log starts empty for the run.
                File.Delete(Speech.LogPath);
                File.WriteAllText(Speech.FlagPath, string.Empty);

                // The user has stepped away at the assistant's question (automation.md, tier 3): Windows' own voice says
                // the run takes the keyboard now, before the first key, and says when it is over. Not NVDA, so the
                // capture holds only what the windows made NVDA say, and no window takes the focus.
                Console.WriteLine("NVDA tier: hands off the keyboard and mouse until it says the tests are finished.");
                Speech.WaitForQuiet(3000);
                HandsOff.StartLoop("Running NVDA tests. Please do not touch the keyboard or mouse.");
                Thread.Sleep(5000);
                File.Delete(Speech.LogPath);
            }

            foreach (var script in scripts)
            {
                var name = Path.GetRelativePath(Directory.Exists(target) ? target : Path.GetDirectoryName(Path.GetFullPath(target))!, script);
                Console.WriteLine($"== {name}");
                var temp = Path.Combine(root, Path.GetFileNameWithoutExtension(script) + "-" + scripts.IndexOf(script));
                Directory.CreateDirectory(temp);
                // WF-5.5, WF-5.8: a stable section's trace held against the kept one, or written, before anything
                // reads the script's result.
                var result = TraceRun.After(RunScript(script, exe, temp, ConvertRun.On, options.AiTools), Console.WriteLine);
                // WF-10.3, WF-10.4: under --convert the script rewritten and played once more, in a folder of its own.
                result = ConvertRun.After(script, name, result, () => RunScript(script, exe, Directory.CreateDirectory(temp + "-converted").FullName, false, false), Console.WriteLine);
                var (ok, log, goals) = (result.Ok, result.Log, result.Goals);
                InventoryRun.After(result);
                RecordRun.After(script, result);
                goalResults.AddRange(goals);
                anySections |= goals.Any(goal => goal.Goal.Named);
                report.Append($"## {name}: {(ok ? "PASS" : "FAIL")}\n\n```\n{log}```\n\n{TraceRun.Report(result)}");
                if (!ok)
                {
                    failed.Add(name);
                    if (log.Contains("|| NVDA did not speak", StringComparison.Ordinal) || log.Contains("|| NVDA spoke it only after", StringComparison.Ordinal))
                    {
                        notHeard.Add(name);
                    }
                }

                if (Run.Stopped is not null)
                {
                    // The NVDA tier could not bring the run's first window to the front (Front.StopsRun): every
                    // script after it would fail at its first key the same way, an hour of them once. No more is run.
                    break;
                }
            }

            if (Run.Nvda)
            {
                // The last windows have closed and NVDA reads what comes to the front: the end waits for it.
                Speech.WaitForQuiet(4000);
            }
        }
        finally
        {
            Console.WriteLine(ClipboardText.PutBack(clipboardBefore));
            if (Run.Nvda)
            {
                // Whatever ended the run, an exception included: the warning stops, the capture ends and its log goes,
                // and the user hears that the keyboard is theirs again (R-33).
                HandsOff.StopLoop();
                Thread.Sleep(500);
                File.Delete(Speech.FlagPath);
                File.Delete(Speech.LogPath);
                HandsOff.Say("NVDA tests finished. The keyboard is yours.");
            }

            runLock.ReleaseMutex();
            runLock.Dispose();
        }

        var summary = failed.Count == 0
            ? $"PASS: {scripts.Count} script(s), {clock.Elapsed.TotalSeconds:0} s"
            : $"FAILED: {failed.Count} of {scripts.Count} script(s): {string.Join(", ", failed)}";
        if (notHeard.Count > 0)
        {
            // The window said it and NVDA did not speak it: the NVDA tier's own finding, told apart from the window's.
            summary += $"\nNVDA did not speak what the window said in {notHeard.Count} of them: {string.Join(", ", notHeard)}";
        }

        if (Run.Stopped is { } stopped)
        {
            // One plain line, the whole result: the scripts did not fail, they were not run.
            summary = $"{stopped}\nSTOPPED: {scripts.Count} script(s), none run to its end.";
        }

        if (anySections)
        {
            // WF-3.10: the sections over all scripts, when a script of the run names its own; a script without a goal
            // or check line counts as its one goal.
            summary += "\nGoals: " + GoalResults.Counts(goalResults);
        }

        // WF-5.5: the stable sections of the run, counted, when it met one.
        foreach (var line in TraceRun.End())
        {
            summary += "\n" + line;
        }

        // WF-2.9, WF-2.10: the command inventory's lines after the summary, its file written under write or rewrite.
        var inventory = InventoryRun.End(Run.Stopped is not null);
        foreach (var line in inventory.Lines)
        {
            summary += "\n" + line;
        }

        // WF-10.4: how many presses the converter moved, and each script it put back.
        foreach (var line in ConvertRun.End())
        {
            summary += "\n" + line;
        }

        // WF-6.2: the run's sections entered into the workflow record, and the line that says so.
        foreach (var line in RecordRun.End())
        {
            summary += "\n" + line;
        }

        report.Append(summary).Append('\n');
        if (reportPath is not null)
        {
            File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));
        }

        if (!keep)
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A process still closing holds its folder; the temp folder is Windows' to clean.
            }
        }

        Console.WriteLine();
        Console.WriteLine(summary);
        return Run.Stopped is not null ? CannotStart : failed.Count > 0 ? ScriptsFailed : inventory.Stale ? InventoryStale : 0;
    }

    private const string Usage = "Usage: AxAutomate <script or folder> (--exe <AxKit.exe> | --build) [--report <file.md>] [--keep] [--nvda] [--repository <folder>] [--inventory check|write|rewrite|off] [--traces check|write|off] [--record] [--convert] [--ai-tools], or AxAutomate --reference [--check] [--repository <folder>]. See docs/axKit/automation.md.";

    /// <summary>
    /// The exit codes (docs/axKit/user-workflows.md WF-11.5): 0 every script passed and nothing is stale, 1 a script
    /// failed, 2 the run could not start (the command line, the build, the program, the lock), 3 every script passed
    /// and the command inventory is stale. Until 2026-11-13 the number of failed scripts, which two failures shared
    /// with a usage error.
    /// </summary>
    private const int ScriptsFailed = 1;

    private const int CannotStart = 2;

    private const int InventoryStale = 3;

    /// <summary>
    /// One script or workflow with its own windows and folder, played by the engine (ScriptRunner); the console gets
    /// each step, the report the steps and the timeline. A script's sections, its goals and checks, are read with it
    /// (ScriptReader.Goals, docs/axKit/user-workflows.md WF-3) and their results come back; a workflow file has none.
    /// The result also holds the listings of the windows' commands the run saw (WF-2.7), for the command inventory,
    /// and, when TraceRun asks for it (a script with a stable section; a run with a report and --traces named), the
    /// run's trace (WF-5.1). With <paramref name="convert"/> (ConvertRun, WF-10.1) a script is played as the converter
    /// plays it, and the result holds the presses to write as do lines; a workflow file never is. With
    /// <paramref name="aiTools"/> (WF-9) a script is played in the AI tools mode: the access turned on before its
    /// first input step after its start, the do steps the tools may run through them.
    /// </summary>
    private static ScriptResult RunScript(string script, string exe, string temp, bool convert, bool aiTools)
    {
        IReadOnlyList<ScriptLine> lines;
        IReadOnlyList<Goal> goals = [];
        Workflow? workflow = null;
        if (script.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase))
        {
            var (prepared, refusal) = ScriptLines.Prepare(WorkflowFile.Read(script), Run.Nvda);
            if (refusal is not null)
            {
                var text = "FAIL: the workflow cannot run: " + refusal;
                Console.WriteLine(text);
                TraceRun.Playing(script, null);
                return new ScriptResult(false, text + "\n", null, [], false);
            }

            lines = prepared;
        }
        else
        {
            // A script's own lines, exactly as the runner always read them; its sections from the same lines, the
            // one goal of a script without a goal or check line titled by its label.
            var fileLines = File.ReadAllLines(script);
            lines = ScriptLines.FromScript(fileLines, null);
            workflow = ScriptReader.Parse(fileLines, Path.GetFileNameWithoutExtension(script), script);
            goals = workflow.Goals;
        }

        return ScriptRunner.Play(lines, exe, temp, Console.WriteLine, options: new PlayOptions(goals, TraceRun.Playing(script, workflow), convert && workflow is not null, aiTools && workflow is not null));
    }

    /// <summary>src\AxKit in Release into a folder of the run's, never into the running app's bin.</summary>
    private static string? Build(string repository, string output)
    {
        Console.WriteLine($"Building axKit into {output} ...");
        var start = new ProcessStartInfo("dotnet", ["build", Path.Combine(repository, "src", "AxKit", "AxKit.csproj"), "-c", "Release", "-o", output, "--nologo", "-v", "q"])
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        var output1 = process.StandardOutput.ReadToEndAsync();
        var output2 = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            Console.Error.WriteLine(output1.Result + output2.Result);
            return null;
        }

        return Path.Combine(output, "AxKit.exe");
    }
}

/// <summary>
/// The NVDA tier's word to the user away from the keys, in Windows' own voice (SAPI's SpVoice through COM, no package),
/// spoken to the end before the run goes on; NVDA never speaks it, so its capture stays the windows' alone. A computer
/// without the voice gets a sound instead.
/// </summary>
internal static class HandsOff
{
    private static readonly string WavePath = Path.Combine(Path.GetTempPath(), "axkit-nvda-hands-off.wav");

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern bool PlaySoundW(string? sound, nint module, uint flags);

    /// <summary>
    /// The warning recorded once into a wave file in Windows' voice, with a pause after it, and played in a loop for
    /// the whole run (the user's ask, 2026-10-31), until <see cref="StopLoop"/>.
    /// </summary>
    public static void StartLoop(string text)
    {
        try
        {
            var voiceType = Type.GetTypeFromProgID("SAPI.SpVoice") ?? throw new InvalidOperationException("No SAPI voice.");
            var streamType = Type.GetTypeFromProgID("SAPI.SpFileStream") ?? throw new InvalidOperationException("No SAPI file stream.");
            var voice = Activator.CreateInstance(voiceType)!;
            var stream = Activator.CreateInstance(streamType)!;
            // 3 is SSFMCreateForWrite; 8 is SVSFIsXML, for the pause after the words.
            streamType.InvokeMember("Open", System.Reflection.BindingFlags.InvokeMethod, null, stream, [WavePath, 3, false]);
            voiceType.InvokeMember("AudioOutputStream", System.Reflection.BindingFlags.SetProperty, null, voice, [stream]);
            voiceType.InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, voice, [System.Security.SecurityElement.Escape(text) + "<silence msec=\"2500\"/>", 8]);
            streamType.InvokeMember("Close", System.Reflection.BindingFlags.InvokeMethod, null, stream, null);
            Marshal.FinalReleaseComObject(stream);
            Marshal.FinalReleaseComObject(voice);
            // SND_ASYNC | SND_LOOP | SND_FILENAME | SND_NODEFAULT.
            PlaySoundW(WavePath, 0, 0x0001 | 0x0008 | 0x00020000 | 0x0002);
        }
        catch (Exception error)
        {
            Console.WriteLine($"(Windows' voice is not available: {error.Message})");
            Console.Beep();
        }
    }

    public static void StopLoop()
    {
        PlaySoundW(null, 0, 0);
        File.Delete(WavePath);
    }

    public static void Say(string text)
    {
        try
        {
            var type = Type.GetTypeFromProgID("SAPI.SpVoice") ?? throw new InvalidOperationException("No SAPI voice.");
            var voice = Activator.CreateInstance(type)!;
            type.InvokeMember("Speak", System.Reflection.BindingFlags.InvokeMethod, null, voice, [text, 0]);
            Marshal.FinalReleaseComObject(voice);
        }
        catch (Exception error)
        {
            Console.WriteLine($"(Windows' voice is not available: {error.Message})");
            Console.Beep();
        }
    }
}
