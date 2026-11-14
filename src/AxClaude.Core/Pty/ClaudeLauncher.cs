using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using AxKit.Core;

namespace AxClaude.Core.Pty;

/// <summary>Builds the command line and the environment for Claude Code, which <c>AxKit.Core/ClaudeCodeProgram</c> finds (FR-1.2).</summary>
public static class ClaudeLauncher
{
    /// <summary>The native Windows installer, as documented at <see cref="InstallUrl"/>; one source with the Install Claude Code now button (<c>ProgramInstall.ClaudeCode</c>).</summary>
    public static string InstallCommand => ProgramInstall.ClaudeCode.PowerShellCommand!;

    public static string InstallUrl => ProgramInstall.ClaudeCode.Page;

    /// <summary>
    /// How Claude Code is started with these arguments (FR-1.2): <c>claude.exe</c> as it is; npm's <c>claude.cmd</c>
    /// through cmd.exe, every argument quoted, or, when its path or an argument holds a character cmd.exe acts on
    /// inside quotes too, the refusal with <see cref="ClaudeCodeProgram.BatchAdvice"/> after it and nothing to run.
    /// The rule is the bundle's, <c>ClaudeCodeProgram.LineFor</c> over <c>SystemPrograms.LineFor</c>
    /// (docs/axKit/repairs.json R-24).
    /// </summary>
    public static ProgramLine BuildCommandLine(string claudePath, IEnumerable<string> arguments) =>
        ClaudeCodeProgram.LineFor(claudePath, arguments);

    /// <summary>
    /// What <c>claude --version</c> prints, its first line (<c>2.0.30 (Claude Code)</c>), or null when Claude cannot
    /// be started or has not answered within <paramref name="timeout"/>, in which case it is ended with whatever it
    /// started. Asked once per run of the app, logged and named by Copy diagnostics (FR-13.2, FR-13.3), so that a
    /// transcript bug can be matched to the Claude Code that drew the screens (docs/axKit/longevity.md 3.10). A
    /// <c>.cmd</c> runs through cmd.exe, as the session does (<see cref="BuildCommandLine"/>), and one whose path
    /// cmd.exe would act on is not run and gives null.
    /// </summary>
    public static async Task<string?> VersionAsync(string claudePath, TimeSpan timeout)
    {
        var line = BuildCommandLine(claudePath, ["--version"]);
        if (line.Refusal is not null)
        {
            return null;
        }

        var start = new ProcessStartInfo { FileName = line.FileName, Arguments = line.Arguments };
        try
        {
            // The bundle's run (AxKit.Core/ProcessRun, 2026-10-25): both pipes read to their end, so that a full one
            // cannot hold Claude, and a program Claude started that keeps them open is covered by the timeout.
            // Outside call, program, Claude Code: runs the user's Claude Code once with --version, without a window, so
            // that axClaude knows which Claude Code it found and that it runs.
            var result = await ProcessRun.RunAsync(start, [], timeout).ConfigureAwait(false);
            if (result.TimedOut)
            {
                return null;
            }

            var first = result.Output.Split('\n')[0].Trim();
            return first.Length > 0 ? first : null;
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// The current environment without any CLAUDE* variable (so a nested session is not detected), plus the
    /// variables that make screen reader mode immediate.
    /// </summary>
    public static Dictionary<string, string> BuildEnvironment()
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = (string)entry.Key;
            if (key.StartsWith("CLAUDE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            env[key] = (string?)entry.Value ?? string.Empty;
        }

        env["TERM"] = "xterm-256color";
        env["CLAUDE_AX_STARTUP_QUIET_MS"] = "0";
        env["CLAUDE_AX_PREPARK_MS"] = "0";
        env["CLAUDE_CODE_DISABLE_ALTERNATE_SCREEN"] = "1";
        return env;
    }

    /// <summary>
    /// The arguments in a line the user typed (the New session notice, FR-8.6), split the way Windows splits a command
    /// line (CommandLineToArgvW): at whitespace; a double-quoted part keeps its spaces; a backslash is literal except in
    /// front of a quote, where each pair is one backslash and an odd one left over makes the quote literal. So
    /// <c>"C:\Dir With Space\\"</c> is that folder with its trailing backslash. The inverse of <see cref="JoinArguments"/>.
    /// </summary>
    public static IReadOnlyList<string> SplitArguments(string text)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var hasToken = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\')
            {
                var run = 1;
                while (i + run < text.Length && text[i + run] == '\\')
                {
                    run++;
                }

                if (i + run < text.Length && text[i + run] == '"')
                {
                    current.Append('\\', run / 2);
                    if (run % 2 == 1)
                    {
                        current.Append('"');
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    i += run;
                }
                else
                {
                    current.Append('\\', run);
                    i += run - 1;
                }

                hasToken = true;
            }
            else if (c == '"')
            {
                // "" is an empty argument; a quoted part joins what is around it: a"b c"d is one argument.
                inQuotes = !inQuotes;
                hasToken = true;
            }
            else if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (hasToken)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }
            }
            else
            {
                current.Append(c);
                hasToken = true;
            }
        }

        if (hasToken)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    /// <summary>The arguments as one line, quoted where needed, so that <see cref="SplitArguments"/> and Windows give them back.</summary>
    public static string JoinArguments(IEnumerable<string> arguments) => string.Join(' ', arguments.Select(SystemPrograms.QuoteArgument));
}
