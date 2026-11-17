namespace AxKit.Core;

/// <summary>
/// Where Claude Code's program is, for axClaude (docs/axClaude/SPEC.md FR-1.2) and for the AI tools registration
/// (docs/axKit/mcp.md MC-6.8), one search since 2026-11-09 (from <c>AxClaude.Core/ClaudeLauncher</c> on its second
/// use; the registration had a shorter search of its own that missed an npm-only install). Pure but for the file
/// lookups, and tested.
/// </summary>
public static class ClaudeCodeProgram
{
    /// <summary>
    /// The places searched for Claude Code, in order: the explicit path when one is given, otherwise <c>claude.exe</c>
    /// or <c>claude.cmd</c> in every PATH folder, the native installer's folder and the npm folder.
    /// <paramref name="pathVariable"/> stands in for the process's PATH in tests.
    /// </summary>
    public static IReadOnlyList<string> Candidates(string? explicitPath, string? pathVariable = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return [explicitPath];
        }

        var candidates = new List<string>();
        // Only the PATH's full folders: a relative entry would be the current folder (S-1).
        foreach (var directory in pathVariable is null ? SystemPrograms.PathFolders() : SystemPrograms.PathFolders(pathVariable))
        {
            foreach (var name in new[] { "claude.exe", "claude.cmd" })
            {
                candidates.Add(Path.Combine(directory, name));
            }
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        candidates.Add(Path.Combine(profile, ".local", "bin", "claude.exe"));
        candidates.Add(Path.Combine(appData, "npm", "claude.cmd"));
        return candidates;
    }

    /// <summary>What a refusal to start npm's <c>claude.cmd</c> ends with: what the user can do about it.</summary>
    public const string BatchAdvice = "Rename the folder or change the argument, or install Claude Code with its own installer: its claude.exe is no batch program and takes any character.";

    /// <summary>
    /// How Claude Code is started with these arguments, for every caller (axClaude's session and version, AI help,
    /// the AI tools' registration; docs/axKit/repairs.json R-24): <see cref="SystemPrograms.LineFor"/>, so
    /// <c>claude.exe</c> as it is and npm's <c>claude.cmd</c> through cmd.exe with every argument quoted, or, when
    /// its path or an argument holds a character cmd.exe acts on inside quotes too, the refusal with
    /// <see cref="BatchAdvice"/> after it and nothing to run.
    /// </summary>
    public static ProgramLine LineFor(string claude, IEnumerable<string> arguments)
    {
        var line = SystemPrograms.LineFor(claude, arguments);
        return line.Refusal is null ? line : line with { Refusal = line.Refusal + " " + BatchAdvice };
    }

    // Claude Code's own words for a run that obeys nothing of the folder it starts in (docs/axKit/ai-help.md AH-2.6;
    // code.claude.com/docs/en/cli-reference and settings, checked against 2.1.325 on 2026-11-16).

    /// <summary>Claude Code's option that loads only the servers named with <c>--mcp-config</c>: none of a folder's <c>.mcp.json</c>.</summary>
    public const string StrictMcpConfigOption = "--strict-mcp-config";

    /// <summary>Claude Code's option that loads one more settings file for a run; what it holds goes before the folder's settings files.</summary>
    public const string SettingsOption = "--settings";

    /// <summary>The settings that turn every hook off for a run, Claude Code's <c>disableAllHooks</c>.</summary>
    public const string NoHooksSettings = "{\"disableAllHooks\":true}";

    /// <summary>
    /// Claude Code's variable that stops it from updating itself in the background, set to <c>1</c> (its
    /// documentation, the pages env-vars and setup, read 2026-11-17: "Set to 1 to disable automatic background
    /// updates"; it checks for updates as it starts and while it runs, and an update takes effect at the next start).
    /// No app of axKit sets it: whether Claude Code updates is the user's own. The tools that check Claude Code set
    /// it for what they start (tools/AxAutomate, tools/ClaudeProbe), so that one run meets one version: on
    /// 2026-11-17 Claude Code installed 2.1.327 two minutes into a run, and every script after that ran another
    /// program than the one whose version the run had asked. A session of the user's own can still update it
    /// meanwhile, which <c>tools/upstream.ps1</c> says when it finds the version changed under a run.
    /// </summary>
    public const string NoBackgroundUpdateVariable = "DISABLE_AUTOUPDATER";

    /// <summary>
    /// The file of <see cref="NoHooksSettings"/> for <see cref="SettingsOption"/>, written when it is missing or holds
    /// anything else, so that a file changed by another program never reaches Claude Code. A file and not the text
    /// on the command line: npm's <c>claude.cmd</c> runs through cmd.exe, where no argument may hold a double quote
    /// (<see cref="SystemPrograms.CmdNeverSafe"/>). Throws as <see cref="File.WriteAllText(string, string)"/> does.
    /// </summary>
    public static void WriteNoHooksSettings(string file)
    {
        if (File.Exists(file) && File.ReadAllText(file) == NoHooksSettings)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, NoHooksSettings);
    }

    /// <summary>The first of <see cref="Candidates"/> that exists; null when Claude Code is not installed.</summary>
    public static string? Find(string? explicitPath, string? pathVariable = null)
    {
        foreach (var candidate in Candidates(explicitPath, pathVariable))
        {
            try
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // A PATH entry with characters that are not allowed in a path.
            }
        }

        return null;
    }
}
