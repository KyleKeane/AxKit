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
