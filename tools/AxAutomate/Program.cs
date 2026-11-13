// AxAutomate: runs scripts of user steps against axKit's windows through their automation channel
// (docs/axKit/automation.md, tier 2). The windows are started on a desktop of the runner's own ("axkit-tests"), so that
// they never take the user's keyboard or foreground; the keys go into each window's own message queue, the sounds are
// recorded and not played, and the windows keep their settings and logs in the run's own folder (AXKIT_HOME).
//
// Usage:
//   AxAutomate <script or folder of scripts> [--exe <AxKit.exe> | --build] [--report <file.md>] [--keep]
//   --build      builds src\AxKit in Release into the run's own folder first (the running app's bin is never touched)
//   --exe        the AxKit.exe to drive (default: src\AxKit\bin\Release\net10.0-windows\AxKit.exe)
//   --report     writes every script's steps and its whole timeline, with times, to a Markdown file
//   --nvda       the NVDA tier (tier 3): the windows open on the user's own desktop and come to the front, the keys go
//                through SendInput as a keyboard's do, and NVDA's speech is read from its capture add-on
//                (tools/nvda-capture). It takes over the machine: only at the user's word, with the user away.
//   --keep       keeps %TEMP%\axkit-automate\<process id>, the run's folder ({temp} in a script), with its logs
//   A folder runs every *.txt under it, one after another, each with fresh windows and a fresh {temp}; the exit code
//   is the number of scripts that failed.
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
        if (args.Length == 0)
        {
            Console.Error.WriteLine(Usage);
            return CannotStart;
        }

        Console.OutputEncoding = Encoding.UTF8;
        var target = args[0];
        string? exe = null;
        string? reportPath = null;
        var keep = false;
        var build = false;
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--exe" or "--report" when i + 1 >= args.Length:
                    Console.Error.WriteLine($"{args[i]} needs a value. {Usage}");
                    return CannotStart;
                case "--exe": exe = args[++i]; break;
                case "--build": build = true; break;
                case "--report": reportPath = args[++i]; break;
                case "--keep": keep = true; break;
                case "--nvda": Run.Nvda = true; break;
                default:
                    Console.Error.WriteLine($"Unknown option: {args[i]}. {Usage}");
                    return CannotStart;
            }
        }

        // The process id and the time: a reused process id never meets an old folder (git's read-only objects in it
        // stopped a run once).
        var root = Path.Combine(Path.GetTempPath(), "axkit-automate", $"{Environment.ProcessId}-{DateTime.Now:HHmmss}");
        Directory.CreateDirectory(root);
        var repository = FindRepository();
        if (build)
        {
            exe = Build(repository, Path.Combine(root, "build"));
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

        // A run that ended without its end (the runner killed, its console closed) left NVDA's capture on, and the add-on
        // then wrote everything NVDA spoke into a file (R-33): an earlier run's flag goes first, in every run.
        File.Delete(Speech.FlagPath);

        // Which build this run drives, in the console and in the report's first lines (R-32).
        var built = $"AxKit.exe {FileVersionInfo.GetVersionInfo(exe).ProductVersion ?? "of unknown version"}, built {File.GetLastWriteTime(exe):yyyy-MM-dd HH:mm}";
        Console.WriteLine(built);
        var report = new StringBuilder($"# AxAutomate report\n\n{DateTime.Now:yyyy-MM-dd HH:mm}, {exe}\n{built}\n\n");
        var failed = new List<string>();
        var notHeard = new List<string>();
        var clock = Stopwatch.StartNew();
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
                var (ok, log) = RunScript(script, exe, temp);
                report.Append($"## {name}: {(ok ? "PASS" : "FAIL")}\n\n```\n{log}```\n\n");
                if (!ok)
                {
                    failed.Add(name);
                    if (log.Contains("|| NVDA did not speak", StringComparison.Ordinal) || log.Contains("|| NVDA spoke it only after", StringComparison.Ordinal))
                    {
                        notHeard.Add(name);
                    }
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
        }

        var summary = failed.Count == 0
            ? $"PASS: {scripts.Count} script(s), {clock.Elapsed.TotalSeconds:0} s"
            : $"FAILED: {failed.Count} of {scripts.Count} script(s): {string.Join(", ", failed)}";
        if (notHeard.Count > 0)
        {
            // The window said it and NVDA did not speak it: the NVDA tier's own finding, told apart from the window's.
            summary += $"\nNVDA did not speak what the window said in {notHeard.Count} of them: {string.Join(", ", notHeard)}";
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
        return failed.Count == 0 ? 0 : ScriptsFailed;
    }

    private const string Usage = "Usage: AxAutomate <script or folder> (--exe <AxKit.exe> | --build) [--report <file.md>] [--keep] [--nvda]. See docs/axKit/automation.md.";

    /// <summary>The exit codes: 0 every script passed, 1 a script failed, 2 the run could not start (the command line, the build, the program). Until 2026-11-13 the number of failed scripts, which two failures shared with a usage error.</summary>
    private const int ScriptsFailed = 1;

    private const int CannotStart = 2;

    /// <summary>One script or workflow with its own windows and folder, played by the engine (ScriptRunner); the console gets each step, the report the steps and the timeline.</summary>
    private static (bool Ok, string Log) RunScript(string script, string exe, string temp)
    {
        IReadOnlyList<ScriptLine> lines;
        if (script.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase))
        {
            var (prepared, refusal) = ScriptLines.Prepare(WorkflowFile.Read(script), Run.Nvda);
            if (refusal is not null)
            {
                var text = "FAIL: the workflow cannot run: " + refusal;
                Console.WriteLine(text);
                return (false, text + "\n");
            }

            lines = prepared;
        }
        else
        {
            // A script's own lines, exactly as the runner always read them.
            lines = ScriptLines.FromScript(File.ReadAllLines(script), null);
        }

        var result = ScriptRunner.Play(lines, exe, temp, Console.WriteLine);
        return (result.Ok, result.Log);
    }

    private static string FindRepository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AxKit.sln")))
            {
                return dir.FullName;
            }
        }

        return Environment.CurrentDirectory;
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
