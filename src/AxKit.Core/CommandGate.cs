namespace AxKit.Core;

/// <summary>Where the wish to run a command comes from (docs/axKit/command-language.md CL-2.1), in the document's order.</summary>
public enum CommandOrigin
{
    /// <summary>The user presses the command's key in the window.</summary>
    Key,

    /// <summary>The user chooses the menu item.</summary>
    Menu,

    /// <summary>The user picks a line of the command palette.</summary>
    Palette,

    /// <summary>A window script's <c>do</c> step, which presses the key the window lists for the command now.</summary>
    Script,

    /// <summary>The automation channel's <c>choose</c>, and its older <c>menu</c>: the runner names a menu item and the window clicks it.</summary>
    Choose,

    /// <summary>An AI tool, <c>axkit_window_run</c>.</summary>
    AiTool,
}

/// <summary>What a cell of the table says (docs/axKit/command-language.md section 3).</summary>
public enum CommandAnswer
{
    /// <summary>The command runs, and the window says what the command itself says.</summary>
    Runs,

    /// <summary>The command runs, and the window also says who ran it.</summary>
    RunsAndIsSaid,

    /// <summary>The user is asked first.</summary>
    AsksFirst,

    /// <summary>The command does not run; <see cref="CommandVerdict.Why"/> says why.</summary>
    Never,
}

/// <summary>
/// Why a command does not run, as a code: the words a user or a client gets stay where they are written
/// (<c>McpRun</c>, <c>KeyTest.AiToolsRefusal</c>, <c>DoStep</c>, <c>AxKitForm.Choose</c>), and each caller gives its own
/// for the code. The numbers are the conditions of docs/axKit/command-language.md section 3.
/// </summary>
public enum CommandRefusal
{
    /// <summary>It runs.</summary>
    None,

    /// <summary>Condition 2: the window's key test runs.</summary>
    KeyTest,

    /// <summary>Condition 1: a notice is open.</summary>
    Notice,

    /// <summary>Condition 1, for the AI tool: the window shows a Windows dialog.</summary>
    Dialog,

    /// <summary>Condition 3: the command's key does not act where the focus is.</summary>
    KeyNotHere,

    /// <summary>Condition 9, and CL-4.8 (5): a key with no menu item, which only the key reaches.</summary>
    NoMenuItem,

    /// <summary>Conditions 4 and 12: the menus do not offer the item now.</summary>
    NotOffered,

    /// <summary>Condition 5: no test run, so there is no automation channel.</summary>
    NotATestRun,

    /// <summary>Condition 7: the app's commands are never run by AI tools (axPass, axClip).</summary>
    AppNeverRun,

    /// <summary>Condition 8: an item withheld from the AI tools.</summary>
    Withheld,

    /// <summary>Condition 10: Let AI tools read and open axKit is off, or the client is not paired.</summary>
    AiToolsOff,

    /// <summary>Condition 10: Let AI tools run commands is off.</summary>
    RunCommandsOff,

    /// <summary>Condition 11: the window is in front and the user pressed a key or moved the mouse a moment ago.</summary>
    UserBusy,

    /// <summary>Condition 13: the user said No to a question of this window within the last minute.</summary>
    DeniedLately,
}

/// <summary>The gate's answer: one of the four things a cell says, with the reason when it is <see cref="CommandAnswer.Never"/>.</summary>
public sealed record CommandVerdict(CommandAnswer Answer, CommandRefusal Why)
{
    public static readonly CommandVerdict Runs = new(CommandAnswer.Runs, CommandRefusal.None);
    public static readonly CommandVerdict RunsAndIsSaid = new(CommandAnswer.RunsAndIsSaid, CommandRefusal.None);
    public static readonly CommandVerdict AsksFirst = new(CommandAnswer.AsksFirst, CommandRefusal.None);

    public static CommandVerdict Never(CommandRefusal why) => new(CommandAnswer.Never, why);
}

/// <summary>
/// The facts the conditions ask about, each one something that stands in a command's way: a state with every fact
/// false is a window at rest with an ordinary command, and every cell then says what the table says without its
/// conditions. The first group is about the window, whichever command is named; the second about the one command.
/// </summary>
public sealed record CommandState
{
    /// <summary>Condition 1: a notice is open (a question, an error, a help text, the palette, a pairing).</summary>
    public bool NoticeOpen { get; init; }

    /// <summary>Condition 1: the window shows a Windows dialog. Asked for the AI tool alone: the user's own keys go to the dialog.</summary>
    public bool DialogShowing { get; init; }

    /// <summary>Condition 2: the window's key test runs.</summary>
    public bool KeyTestRunning { get; init; }

    /// <summary>Condition 5: the window was not started by the test runner, so it has no automation channel.</summary>
    public bool NoTestRun { get; init; }

    /// <summary>Condition 7: the app serves no AI tool (axPass, axClip).</summary>
    public bool AppServesNoAiTool { get; init; }

    /// <summary>Condition 10: Let AI tools read and open axKit is off, its sealed file is not whole, or the client is not paired.</summary>
    public bool AiToolsOff { get; init; }

    /// <summary>Condition 10: Let AI tools run commands is off.</summary>
    public bool RunCommandsOff { get; init; }

    /// <summary>Condition 11: the window is in front and the user's last key or mouse move is a moment ago.</summary>
    public bool UserBusy { get; init; }

    /// <summary>Condition 3: the command's key does not act where the focus is (<c>Command.When</c>).</summary>
    public bool KeyDoesNotActHere { get; init; }

    /// <summary>Condition 1's exception: a bundle key that is information alone (the text size keys, Say the status, the mode key), which works in a notice too.</summary>
    public bool InformationKey { get; init; }

    /// <summary>Condition 9: the command is a key with no menu item.</summary>
    public bool KeyOnly { get; init; }

    /// <summary>Condition 8: the item is never for AI tools (the switches that decide what an AI may do).</summary>
    public bool NeverForAiTools { get; init; }

    /// <summary>Conditions 4 and 12: the menus do not offer the item now (disabled, gone, renamed).</summary>
    public bool NotOffered { get; init; }

    /// <summary>Condition 13: the user said No to a question of this window within the last minute.</summary>
    public bool DeniedLately { get; init; }
}

/// <summary>
/// The table of docs/axKit/command-language.md section 3 as one pure function: who may run a command of which mark,
/// in which state of the window. <c>tests/AxKit.Tests/CommandGateTests</c> is the table, a test per cell and per
/// condition, and fails when the document names a cell that has no test.
///
/// The AI tool's path asks it (<c>AxKitForm.McpRunCommand</c>) and gives each reason its own words. For the user's
/// key, menu and palette, and for the automated checks' two origins, it is the description, held by the tests, while
/// the enforcement stays where it was: WinForms' message handling, the key test's filter, the disabled menu bar, the
/// window's listing (CL-4.8). A new origin starts here.
/// </summary>
public static class CommandGate
{
    /// <summary>
    /// What the table says for a command of <paramref name="mark"/> asked for by <paramref name="origin"/>: the
    /// window's conditions first (<see cref="Window"/>), then the command's own, then the cell.
    /// </summary>
    public static CommandVerdict Decide(CommandOrigin origin, CommandMark mark, CommandState state)
    {
        if (Window(origin, state) is { } held)
        {
            return CommandVerdict.Never(held);
        }

        if (OfCommand(origin, mark, state) is { } refused)
        {
            return CommandVerdict.Never(refused);
        }

        if (origin != CommandOrigin.AiTool)
        {
            // The user's own hands and the automated checks: axKit asks nothing because of a mark.
            return CommandVerdict.Runs;
        }

        return mark == CommandMark.Act ? CommandVerdict.AsksFirst : CommandVerdict.RunsAndIsSaid;
    }

    /// <summary>
    /// Why no command of the window runs for <paramref name="origin"/> now, whichever is named, or null: the part of
    /// <see cref="Decide"/> that needs no command, for a caller that refuses before it reads the menus (the AI tool's
    /// path: reading the menus fills the lists that fill as they open). The order is the order the code asks in, which
    /// decides the words when two hold at once: for the AI tool the pipe's own check, the app, the switch, the key
    /// test, a notice, a Windows dialog, the user's hands.
    /// </summary>
    public static CommandRefusal? Window(CommandOrigin origin, CommandState state)
    {
        switch (origin)
        {
            case CommandOrigin.Key:
                // The key test's filter takes the key before the window's key handling, which a notice holds; the
                // bundle's information keys are handled ahead of the notice.
                return state.KeyTestRunning ? CommandRefusal.KeyTest
                    : state.NoticeOpen && !state.InformationKey ? CommandRefusal.Notice
                    : null;
            case CommandOrigin.Menu:
            case CommandOrigin.Palette:
                // By the keyboard nothing reaches the menu bar or opens the palette in the key test; a notice
                // disables the bar, and the palette does not open over one.
                return state.KeyTestRunning ? CommandRefusal.KeyTest
                    : state.NoticeOpen ? CommandRefusal.Notice
                    : null;
            case CommandOrigin.Script:
            case CommandOrigin.Choose:
                // The listing names the key test where a notice's title stands, the key test first; choose is held
                // by the key test as every other origin since 2026-11-25 (CL-4.8, 1), asked here by AxKitForm.Choose.
                return state.NoTestRun ? CommandRefusal.NotATestRun
                    : state.KeyTestRunning ? CommandRefusal.KeyTest
                    : state.NoticeOpen ? CommandRefusal.Notice
                    : null;
            default:
                return state.AiToolsOff ? CommandRefusal.AiToolsOff
                    : state.AppServesNoAiTool ? CommandRefusal.AppNeverRun
                    : state.RunCommandsOff ? CommandRefusal.RunCommandsOff
                    : state.KeyTestRunning ? CommandRefusal.KeyTest
                    : state.NoticeOpen ? CommandRefusal.Notice
                    : state.DialogShowing ? CommandRefusal.Dialog
                    : state.UserBusy ? CommandRefusal.UserBusy
                    : null;
        }
    }

    /// <summary>Why this one command does not run for the origin, the window's conditions passed, or null.</summary>
    private static CommandRefusal? OfCommand(CommandOrigin origin, CommandMark mark, CommandState state)
    {
        switch (origin)
        {
            case CommandOrigin.Key:
            case CommandOrigin.Script:
                // A do step presses the very key, so the key's condition is its own; a command with no key at all is
                // run by choose, and is decided as that origin.
                return state.KeyDoesNotActHere ? CommandRefusal.KeyNotHere : null;
            case CommandOrigin.Menu:
            case CommandOrigin.Palette:
            case CommandOrigin.Choose:
                return state.KeyOnly ? CommandRefusal.NoMenuItem
                    : state.NotOffered ? CommandRefusal.NotOffered
                    : null;
            default:
                // The name is resolved among the entries an AI tool may see before the mark is asked, and only an act
                // is held by the minute after a No.
                return state.KeyOnly ? CommandRefusal.NoMenuItem
                    : state.NeverForAiTools ? CommandRefusal.Withheld
                    : state.NotOffered ? CommandRefusal.NotOffered
                    : mark == CommandMark.Act && state.DeniedLately ? CommandRefusal.DeniedLately
                    : null;
        }
    }
}
