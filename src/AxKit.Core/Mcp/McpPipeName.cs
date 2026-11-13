namespace AxKit.Core.Mcp;

/// <summary>
/// The part of a window's AI tools' pipe name after the user's prefix (docs/axKit/mcp.md MC-6.3): sixteen random hex
/// digits for the user's own windows, and "run", the run's mark and a dash before them for a test run's windows
/// (<see cref="AppPaths.RunMark"/>). A broker connects only to the pipes of its own kind, so that a test run's broker
/// never knocks on the user's windows, which refused it and logged each knock (2026-11-12), and the user's broker
/// never lists a run's windows.
/// </summary>
public static class McpPipeName
{
    /// <summary>The part after the prefix for a window of this process: the run's mark when there is one, then the random digits.</summary>
    public static string Suffix(string? runMark, string random) => runMark is null ? random : "run" + runMark + "-" + random;

    /// <summary>Whether a pipe, by the part after the prefix, belongs to a broker of the same kind: the same run, or no run on both sides.</summary>
    public static bool SameKind(string suffix, string? runMark) =>
        runMark is null
            ? suffix.Length == 16 && suffix.All(Uri.IsHexDigit)
            : suffix.StartsWith("run" + runMark + "-", StringComparison.Ordinal);
}
