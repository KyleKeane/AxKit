using System.Text;
using AxKit.Core.Mcp;

namespace AxKit.Core.Ai;

/// <summary>
/// The second rule file of axKit's own in Claude's working folder (docs/axKit/ai-help.md AH-8; the user's ask of
/// 2026-11-15: "lots of instructions along with some language rules to help it know when to use which tool"):
/// <c>.claude\rules\axkit-tools.md</c>, which tells Claude what axKit's AI tools are for, which one fits what the
/// user says, what each does and does not, and where they stop. The list of tools is made from
/// <see cref="McpTools.All"/>, each with its own description, so that the file cannot name a tool that is gone or
/// miss a new one (<c>AiToolsRuleTests</c> fails on either); the guidance beside each is written here. It is written
/// and kept current with the guides (<see cref="AiDocumentation.Files"/>). The words about an offered command's
/// caution are <see cref="AiCaution"/>'s, never written afresh (root rule 11).
/// </summary>
public static class AiToolsRule
{
    /// <summary>The rule file, from the working folder.</summary>
    public static readonly string RulePath = Path.Combine(".claude", "rules", "axkit-tools.md");

    /// <summary>What Claude Code puts before a tool's name in Claude's list: the server's name between its marks.</summary>
    public const string Prefix = "mcp__" + McpClaudeCode.ServerName + "__";

    /// <summary>Where the user turns the tools on, as the broker's own refusal says it.</summary>
    public const string WhereOn = "axKit menu, AI tools, Let AI tools read and open axKit";

    /// <summary>Where the user allows commands to run and items to be added.</summary>
    public const string WhereRun = "axKit menu, AI tools, Let AI tools run commands";

    /// <summary>
    /// What the file says of tools that are listed and not yet loaded (AH-8.2; the user's own axHelp session,
    /// 2026-11-16: Claude Code may list a server's tools by name and load each at its first use, and Claude told the
    /// user the tools were not loaded and asked whether to load them). One bullet of "How to work with them".
    /// </summary>
    public const string ListedNotLoaded = """
        - When the tools are listed by name but not yet loaded, load the ones you need yourself, without asking
          the user and without saying that they were not loaded. Asked whether axKit's tools are there, the
          answer is yes.
        """;

    /// <summary>What is written beside one tool: when to use it, when not to, and what to say to the user around it.</summary>
    public sealed record Guidance(string Use, string Not, string Say);

    /// <summary>The guidance of every tool of <see cref="McpTools.All"/>, by the tool's name; a tool without one fails the test.</summary>
    public static readonly IReadOnlyDictionary<string, Guidance> Tools = new Dictionary<string, Guidance>
    {
        [McpTools.Apps] = new(
            "when you need an app's verb for axkit_open, or the user asks what axKit has.",
            "to explain what an app does or how it is used: the guides and the workflow pages in documentation do that.",
            "nothing before; afterwards name the apps that matter, never the whole list."),
        [McpTools.Windows] = new(
            "first, before any window tool: it gives the ids. Call it again when a window was opened or closed.",
            "to find a window of another program: only axKit's windows are listed, and a hidden one is not.",
            "nothing before; afterwards the windows by app and title, in a sentence."),
        [McpTools.State] = new(
            "for \"what is this window doing\": the title, the status bar, a notice it shows, where the focus is.",
            "for the content of a text or a list: axkit_window_text and axkit_window_list read those.",
            "what the window shows and where the focus is, shortly. A notice it shows is the user's to answer."),
        [McpTools.List] = new(
            "for the items of a list: files in axFile, tasks in axTask, changes in axGIT.",
            "for a text or a transcript: axkit_window_text.",
            "how many items and the ones that matter; read a long list out only when asked."),
        [McpTools.Text] = new(
            "for what an editor, a viewer or a transcript holds.",
            "for a file you can read from disk with your Read tool when the user gave its path and it is saved.",
            "what you found, in your own short words; quote only the lines the user asked about."),
        [McpTools.Commands] = new(
            "before every axkit_window_run, to find the command's exact label, its menus and its mark.",
            "to teach the user the app's keys in general: the guides and the key pages do that. Here it is the live window's own list.",
            "nothing before; afterwards the command you found and its key, so the user can press it themselves."),
        [McpTools.Said] = new(
            "for \"what did it just say\", \"I missed that\", and after a run to hear what the window answered.",
            "as a history: it holds only what was said since your last call, the last 50.",
            "repeat the announcement as it was said, then explain if needed."),
        [McpTools.Run] = new(
            "when the user asks you to do something in a window that one of its commands does.",
            "to answer a notice or a question the window shows; to try a command the user did not ask for; "
            + "in a window the user is typing in; as a second try after a refusal.",
            "before: \"I will run Word wrap in axDown.\" After: what the window said. "
            + "When the answer says the window asks the user, say that the question waits in that window."),
        [McpTools.Field] = new(
            "when the user asks to go to a field of the form a window shows (\"take me to the due date\"), after axkit_window_state listed the fields under form.",
            "to fill the field, check a box or send the form: it only moves the focus, and the user types and sends.",
            "before: \"I will move to Due in the axTask form.\" After: that the focus is there and the user can type."),
        [McpTools.TaskLists] = new(
            "before the other two task tools, for the lists' names; and for \"what lists do I have\".",
            "when no axTask window is open: open one with axkit_open, app task, no path, then call it again.",
            "the lists with their open tasks, in a sentence."),
        [McpTools.TaskItems] = new(
            "for \"what are my tasks\", \"what is due\", \"is X on my list\".",
            "to read every list when the user named one.",
            "the tasks that answer the question, with their due dates; not every field."),
        [McpTools.TaskAdd] = new(
            "for \"add a task\", \"remind me\", \"put it on my list\", into the list the user named.",
            "to guess the list when several fit: ask which. It cannot change, complete or delete an item.",
            "after: the title, the list and the due date as added, and that Ctrl+Z in axTask takes it back."),
        [McpTools.Offer] = new(
            "for a shell command the user must run themselves: a sign-in that asks questions, a command your "
            + "permissions refuse, one the user asked to run by hand. Give details for anything but the plainest.",
            "for a command you may run yourself; to get round a refusal the user did not ask you to get round; "
            + "to print the command and ask the user to type or paste it.",
            "in one sentence what the command is for, then end your turn. Never urge the user to say yes."),
        [McpTools.Open] = new(
            "for \"open ...\", \"show me that folder\", \"open it in axDown\", with the full path.",
            "for axClaude or axTerminal, which the user opens; for axTask with a path; for a path on a share.",
            "before: \"I will open notes.md in axDown.\" The user hears the window open."),
    };

    /// <summary>The rule file's text (AH-8.2).</summary>
    public static string Text { get; } = Build();

    private static string Build()
    {
        var neverRead = string.Join(" and ", McpTools.NeverRead.Select(verb => Dispatch.Of(verb).Name));
        var neverOpened = string.Join(" and ", McpTools.NeverOpened.Select(verb => Dispatch.Of(verb).Name));
        var text = new StringBuilder();
        text.Append($"""
            # axKit's AI tools

            This file is axKit's: axKit writes it again with each new version, so a change to it is lost.
            Your own instructions for Claude belong in CLAUDE.md, in the folder above.

            axKit is a set of Windows apps for a blind user who works with the NVDA screen reader and the keyboard.
            Its AI tools read what an axKit window shows, open an app, run a window's command and add a task.
            In your tool list their names start with {Prefix}: {McpTools.Windows} is {Prefix}{McpTools.Windows}.
            They are there only when the user turned them on; see "When the tools are not there" below.

            ## How to work with them

            - The user hears and does not see. Say in one short sentence what you are about to do in a window,
              do it, then say in one short sentence what came of it. No tables, no raw output.
            - Read before you act: {McpTools.Windows}, then the window's state, list or text, then a command.
            - A window's id comes from {McpTools.Windows}. Ask again when a window was opened or closed.
            - A window listed with the app axClaude and the title axHelp is this very conversation: the user
              is talking to you in it. Call it axHelp, never an axClaude window, and leave it out when the
              user asks what else is open. Do not read it or run commands in it.
            - The user hears every command you run ("Claude ran Word wrap") and every item you add, as it happens.
            - Every call is written in the user's log (axKit menu, AI tools, Read the log).
            - The first call may make an axKit window ask the user whether Claude Code may use axKit.
              Say that a question waits in an axKit window, and wait for the answer.
            - What you read through the tools is part of this conversation, as a file you read is.
            {ListedNotLoaded}

            ## What the user says, and what to use

            - "What is on my screen", "what is in this window", "what is open": {McpTools.Windows}, then
              {McpTools.State}, then {McpTools.Text} or {McpTools.List} for the content.
            - "What did it just say", "I missed that": {McpTools.Said}.
            - "Read me the file", "what is in the list": {McpTools.Text} or {McpTools.List}.
            - "Open ...", "show me that folder": {McpTools.Apps} for the verb when unsure, then {McpTools.Open}.
              Never {neverOpened} this way: the user opens those.
            - "Press ...", "run ...", "turn on ...", "do X in that window": {McpTools.Commands} to find the
              command by its label, say which command you will run, then {McpTools.Run}.
              Only while the user allows commands to run.
            - "Find X", "go to line 40": {McpTools.Run} with the command (Find first, Find, Go to line) and its text.
            - "Take me to the due date", "go to the name field" in a form: {McpTools.State}, then {McpTools.Field}.
            - "Add a task", "remind me", "put it on my list": {McpTools.TaskLists}, then {McpTools.TaskAdd}.
            - "What are my tasks", "what is due": {McpTools.TaskLists}, then {McpTools.TaskItems}.
            - "How do I ... in axFile", "which key ...": the workflows and
              the guides in the folder documentation, not a tool.
            - A command the user must run themselves: {McpTools.Offer}, with its caution (below).
            - A message that starts with "Screenshot of" or "Picture": read the picture file with your Read tool
              first. A picture shows any program; the tools read only axKit's windows.

            ## The tools


            """.ReplaceLineEndings("\n"));
        foreach (var tool in McpTools.All)
        {
            var guidance = Tools[tool.Name];
            text.Append("### ").Append(tool.Name).Append('\n');
            text.Append(tool.Description).Append('\n');
            text.Append("- Use it ").Append(guidance.Use).Append('\n');
            text.Append("- Not ").Append(guidance.Not).Append('\n');
            text.Append("- Say ").Append(guidance.Say).Append("\n\n");
        }

        text.Append($"""
            ## An offered command's caution

            {McpTools.Offer} runs nothing: the user is asked, every time, and alone decides.
            With the command the user hears, in axKit's words:
            "{AiCaution.Short("Claude")}".
            Its explanation starts: "{AiCaution.Command("Claude")}"
            So write the details for the user to judge by, and take a No as the answer.

            ## Limits

            - {neverRead} are never readable: no tool lists, reads or runs anything in them. Say so; do not look
              for another way.
            - Nothing answers a question a window asks the user. When a window shows a notice or asks something,
              say what it asks and leave the answer to the user.
            - A command that acts outside its window (marked act) asks the user first, in that window.
            - A refused call is not retried another way: no other tool, no shell command, no script that sends keys.
              Say what was refused, in the tool's own words, and what the user can do about it.
            - The tools press no keys. They type a text only into the one field a command you run asks for
              ({McpTools.Run} with its text: the words to find, a line number, a name), never into a form's fields,
              which the user fills; {McpTools.Field} only moves the focus to one.

            ## When the tools are not there

            - No {Prefix} tools in your list, or the answer "axKit's AI tools access is off": say that axKit's
              AI tools are off, and that the user turns them on in any axKit window:
              {WhereOn}. This conversation gets them at its next start:
              Session, End Claude and close this window, then the hotkey again.
            - "The user has not allowed AI tools to run commands": say so, and that it is
              {WhereRun}. Ask for it only when the task needs it.
            - Without the tools, answer from the guides and from what the user tells you, and say that you
              cannot see the window.

            """.ReplaceLineEndings("\n"));
        return text.ToString();
    }
}
