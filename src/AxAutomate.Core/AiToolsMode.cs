using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Core.Mcp;

namespace AxAutomate.Core;

/// <summary>
/// The AI tools mode of the command line (docs/axKit/user-workflows.md WF-9, <c>--ai-tools</c>), its pure half: which
/// scripts the mode plays (<see cref="Covers"/>), the steps that turn the access on as a user turns it on
/// (<see cref="Preamble"/>), which <c>do</c> step goes through <c>axkit_window_run</c> (<see cref="MayRun"/>), the
/// call's line (<see cref="Call"/>) and the one refusal that is waited out (<see cref="IsBusyRefusal"/>).
/// <see cref="Run"/> plays them, as the MCP client the runner already is (<see cref="Broker"/>): no gate of
/// docs/axKit/mcp.md is passed by, nothing is written into a settings file, and the mode reaches only the run's own
/// windows. Tested in <c>tests/AxAutomate.Tests/AiToolsModeTests</c>.
/// </summary>
public static class AiToolsMode
{
    /// <summary>
    /// WF-9.3: the access on in the menu, the runner paired in the window's notice, Let AI tools run commands on.
    /// Each is a line of <c>tests/scripts/mcp/02-run-commands.txt</c> as it stands, in that script's order (the line
    /// <c>expect mcp "app":</c> is the script's without the app's name); a test holds them to the script, so a changed
    /// notice is changed there and shows here at once.
    /// </summary>
    public static readonly IReadOnlyList<string> Preamble =
    [
        "menu Let AI tools read and open axKit",
        "expect state \"notice\":\"Turn on AI tools?\"",
        // A notice a menu item opens holds its keys for a second (bundle AX-6.4, E3 of the fifth review of 2026-11-27).
        "wait 1100",
        "keys enter",
        "expect said AI tools on",
        "try said Not in a test run",
        "mcp start",
        "mcp call axkit_windows",
        "expect state \"notice\":\"axAutomate asks to use axKit\"",
        "wait 1500",
        "keys tab",
        "expect focus I understand",
        "keys space",
        "expect said ! checked",
        "keys tab",
        "expect focus &Allow",
        "keys enter",
        "expect said axAutomate may read axKit",
        "expect mcp \"app\":",
        "menu Let AI tools run commands",
        "expect said AI tools may run commands: each is said as it runs",
    ];

    /// <summary>What a step that ran through the tools shows after its line, before the command's label (WF-9.4).</summary>
    public const string Detail = "AI tools: ";

    /// <summary>How long the refusal of <see cref="IsBusyRefusal"/> is waited out before the call is sent again, and how many times at most (WF-9.4).</summary>
    public const int BusyWaitMs = 3500;

    public const int BusyRetries = 3;

    /// <summary>
    /// WF-9.2: whether the mode plays the script: it holds at least one <c>do</c> line (an <c>iffound do</c> counts),
    /// no line whose step word is <c>mcp</c>, <c>launch</c>, <c>adopt</c> or <c>use</c>, and exactly one
    /// <c>start</c>, whose first word is a verb of <see cref="Dispatch.Apps"/> that is not among
    /// <see cref="McpTools.NeverRead"/> (an axPass or axClip window gives the tools nothing, so the pairing would
    /// never end). Every other script is the keyboard run's.
    /// </summary>
    public static bool Covers(IReadOnlyList<string> fileLines)
    {
        var does = 0;
        var starts = 0;
        foreach (var fileLine in fileLines)
        {
            var line = fileLine.Trim();
            while (line.StartsWith("iffound ", StringComparison.Ordinal))
            {
                line = line[8..].TrimStart();
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            var space = line.IndexOf(' ');
            var word = space < 0 ? line : line[..space];
            var rest = space < 0 ? string.Empty : line[(space + 1)..].Trim();
            switch (word)
            {
                case "do":
                    does++;
                    break;
                case "mcp" or "launch" or "adopt" or "use":
                    return false;
                case "start":
                    starts++;
                    var first = rest.Split(' ', 2)[0];
                    if (!Dispatch.Apps.Any(app => string.Equals(app.Verb, first, StringComparison.OrdinalIgnoreCase))
                        || McpTools.NeverRead.Contains(first, StringComparer.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    break;
            }
        }

        return does > 0 && starts == 1;
    }

    /// <summary>
    /// WF-9.4: whether the tools may run the command without asking the user: the window shows no notice, the row is
    /// not a key alone (it has a menu item), it is for the AI tools, and its mark is not act (an act command asks the
    /// user first, docs/axKit/mcp.md MC-7.3, and nothing answers that question for the user). Every other
    /// <c>do</c> step plays by the keyboard as written.
    /// </summary>
    public static bool MayRun(WindowCommands listing, ListedCommand row) =>
        listing.Notice is null && !row.KeyOnly && row.Ai && !string.Equals(row.Mark, "act", StringComparison.OrdinalIgnoreCase);

    /// <summary>WF-9.4: whether a tool's answer is the refusal of docs/axKit/mcp.md MC-7.8, the user at work in the session, which is waited out; every other refusal fails its step.</summary>
    public static bool IsBusyRefusal(string answer) =>
        answer.Contains("The user is working in this window right now", StringComparison.Ordinal);

    /// <summary>
    /// WF-9.4: whether a tool's answer says that the run was accepted and then not taken up, because the user began to
    /// work in the session in between (docs/axKit/mcp.md MC-7.8, <see cref="McpRun.UserWasBusy"/>): nothing ran, and
    /// the call is sent again, once (<see cref="TakeUpRetries"/>).
    /// </summary>
    public static bool IsBusyAtTakeUp(string answer) =>
        !answer.Contains("\"ran\"", StringComparison.Ordinal) && answer.Contains("did not run: " + McpRun.UserWasBusy, StringComparison.Ordinal);

    /// <summary>The name the runner gives itself as an MCP client (<see cref="Broker"/>'s <c>initialize</c>), which a window says when it ran a command for it: "axAutomate ran Find". A test holds it to the broker's line.</summary>
    public const string Client = "axAutomate";

    /// <summary>
    /// WF-9.4: whether an announcement is the mode's own and nothing else: the words that say who ran a command the
    /// mode ran through the tools (<see cref="McpRun.Said"/>, docs/axKit/mcp.md MC-7.5), alone, as a window says them
    /// when the command itself says nothing. An <c>expect quiet</c> step does not count such an announcement: by its
    /// key the command says nothing, and that is what the step checks. Words of the command's own joined to them
    /// ("axAutomate ran Word wrap. Word wrap off") are no such announcement, and are counted.
    /// </summary>
    public static bool IsOwnWords(string said, IReadOnlyCollection<string> ranLabels) =>
        ranLabels.Any(label => string.Equals(said, McpRun.Said(Client, label), StringComparison.Ordinal));

    /// <summary>What an <c>expect quiet</c> step that passed shows when it left the mode's own words out, before them (WF-9.4).</summary>
    public const string QuietDetail = "AI tools: not counted, the words that say who ran a command: ";

    /// <summary>How many times a run not taken up for the user at work is sent again (<see cref="IsBusyAtTakeUp"/>), each after <see cref="BusyWaitMs"/>.</summary>
    public const int TakeUpRetries = 1;

    /// <summary>
    /// WF-9.4: what follows <c>mcp call</c> for the row: <c>axkit_window_run</c> with the window (<c>{window}</c>,
    /// which the runner fills with the id the preamble's <c>axkit_windows</c> answer named), the command's label, and
    /// the menus that hold it, left out for a row with none.
    /// </summary>
    public static string Call(ListedCommand row)
    {
        var arguments = new JsonObject { ["window"] = "{window}", ["command"] = row.Label };
        if (row.Menu.Length > 0)
        {
            arguments["menu"] = row.Menu;
        }

        return "axkit_window_run " + arguments.ToJsonString(Plain);
    }

    /// <summary>The call's JSON written as a script writes it: an apostrophe or an ampersand of a label as itself.</summary>
    private static readonly JsonSerializerOptions Plain = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
}
