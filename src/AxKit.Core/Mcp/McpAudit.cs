using System.Security.Cryptography;
using System.Text;

namespace AxKit.Core.Mcp;

/// <summary>
/// The record of every AI tool call (docs/axKit/mcp.md MC-5.2, MC-6.6): one line in <see cref="AppPaths.McpAuditFile"/>
/// per call, with the time, the client, the tool, a short hash of the arguments (never the arguments themselves, which
/// may name the user's files) and the result. A line that cannot be written is logged and the call goes on.
/// </summary>
public static class McpAudit
{
    /// <summary>One line: "2026-10-24 21:05:03  Claude Code (C:\…\claude.exe)  axkit_open  args 3f2a…  done".</summary>
    public static string Line(DateTime time, string client, string program, string tool, string arguments, string result) =>
        $"{time:yyyy-MM-dd HH:mm:ss}  {Clean(client)} ({Clean(program)})  {Clean(tool)}  args {Hash(arguments)}  {Clean(result)}";

    public static void Write(string client, string program, string tool, string arguments, string result)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.McpAuditFile)!);
            File.AppendAllText(AppPaths.McpAuditFile, Line(DateTime.Now, client, program, tool, arguments, result) + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("The AI tools' log was not written", ex);
        }
    }

    /// <summary>
    /// The result as the line holds it (docs/axKit/repairs.json R-06). A command run is named, so that the user can
    /// read what an AI tool ran: "done: ran Word wrap", the name cut before a recent file's path ("1. notes.md"). A
    /// refusal's first line goes through <see cref="Redaction"/>, since it may quote a path the client gave.
    /// <paramref name="text"/> is the tool's answer; for <c>axkit_window_run</c> the window's own (<see cref="RunResult"/>).
    /// An <c>axkit_open</c> has two results of its own since the third review of 2026-11-27: "not known" when the
    /// window that was asked to start the app did not say how it ended (<see cref="McpTools.OpenNotKnown"/>), which
    /// until then read "refused or failed" though the app may have opened; and "asked the user" for an app that is
    /// asked first, which read "done" though nothing opens before the user's yes.
    /// </summary>
    public static string Result(string tool, bool failed, string text)
    {
        var first = text.Split('\n')[0].Trim();
        if (tool == McpTools.Open && failed && first == McpTools.OpenNotKnown)
        {
            return "not known";
        }

        if (failed)
        {
            return "refused or failed: " + Redaction.Redact(first);
        }

        if (tool == McpTools.Open && McpTools.OpenWasAsked(text))
        {
            return "asked the user";
        }

        return tool == McpTools.Run ? RunResult(text) : "done";
    }

    /// <summary>
    /// What became of an <c>axkit_window_run</c>, from the window's answer (MC-7.1) and with the label the window
    /// matched, never the client's own argument: "done: ran Word wrap" only for a command the window ran; "asked the
    /// user: Delete" for an act command, which runs only if the user says so in the window; "not run: Find" when a
    /// notice, a dialog, the switch or a disabled item stopped it in between; "not known" when the window had not
    /// taken it up or closed. Until 2026-11-27 every run without an error was written "done: ran", with the name the
    /// client gave.
    /// </summary>
    public static string RunResult(string answer)
    {
        try
        {
            if (System.Text.Json.Nodes.JsonNode.Parse(answer) is System.Text.Json.Nodes.JsonObject window
                && (McpProtocol.Text(window["command"]) ?? McpProtocol.Text(window["ran"])) is { } label
                && NameOnly(label) is { Length: > 0 } name)
            {
                return window["ran"] is not null ? "done: ran " + name
                    : window["asked"] is not null ? "asked the user: " + name
                    : window["notRun"] is not null ? "not run: " + name
                    : "not known";
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // An answer that is no JSON says nothing of what ran.
        }

        return "not known";
    }

    /// <summary>A command's name without the path a recent file's entry carries in brackets: "1. notes.md (C:\…\notes.md)" is "1. notes.md".</summary>
    private static string NameOnly(string command)
    {
        var bracket = command.IndexOf(" (", StringComparison.Ordinal);
        return (bracket > 0 ? command[..bracket] : command).Trim();
    }

    /// <summary>The first 16 hex digits of the arguments' SHA-256: the same arguments give the same digits, and nothing of them can be read back.</summary>
    public static string Hash(string arguments) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(arguments)))[..16];

    /// <summary>A text on one line: a client's name is its own to choose and may hold line breaks that would forge a line.</summary>
    private static string Clean(string text) =>
        new(text.Select(c => char.IsControl(c) ? ' ' : c).Take(200).ToArray());
}

/// <summary>At most <see cref="PerSecond"/> calls in any second (MC-3.5), for one broker, that is one client.</summary>
public sealed class McpRate
{
    public const int PerSecond = 10;

    private readonly Queue<long> _calls = new();

    /// <summary>Whether a call at this time (milliseconds) is within the limit; a call allowed is counted.</summary>
    public bool Allow(long now)
    {
        while (_calls.Count > 0 && now - _calls.Peek() >= 1000)
        {
            _calls.Dequeue();
        }

        if (_calls.Count >= PerSecond)
        {
            return false;
        }

        _calls.Enqueue(now);
        return true;
    }
}
