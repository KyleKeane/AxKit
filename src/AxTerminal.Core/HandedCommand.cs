using AxKit.Core;

namespace AxTerminal.Core;

/// <summary>
/// A command another window hands to axTerminal for the user to read and send himself (SPEC.md AT-1.5a;
/// docs/axTask/forms-framework.md AF-4.5, AF-7.4): <c>terminal [&lt;folder&gt;] --command &lt;program&gt;
/// [&lt;argument&gt; ...]</c>. The parts come as the process's own arguments, one each, and never as a joined
/// line: this window knows its shell, so the line is made here, each part quoted by that shell's rule
/// (<see cref="ShellQuote"/>). The line is put into the command field and is never sent by axKit. Pure.
/// </summary>
public static class HandedCommand
{
    /// <summary>The option after which every argument is a part of the command, whatever it looks like.</summary>
    public const string Option = "--command";

    /// <summary>
    /// The folder and the command's parts of axTerminal's arguments, or what is wrong with them: the one folder
    /// stands before <see cref="Option"/>, and everything after it is the command, an argument that starts with
    /// two hyphens included.
    /// </summary>
    public static (string? Folder, IReadOnlyList<string> Command, string? Problem) Parse(IReadOnlyList<string> args)
    {
        var at = -1;
        for (var i = 0; i < args.Count && at < 0; i++)
        {
            if (args[i] == Option)
            {
                at = i;
            }
        }

        var before = at < 0 ? args.Count : at;
        for (var i = 0; i < before; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                return (null, [], $"Unknown option {args[i]}.");
            }
        }

        if (before > 1)
        {
            return (null, [], "axTerminal opens one folder.");
        }

        IReadOnlyList<string> command = at < 0 ? [] : args.Skip(at + 1).ToList();
        if (at >= 0 && command.Count == 0)
        {
            return (null, [], $"{Option} needs the command after it: the program, then its arguments.");
        }

        return (before == 1 ? args[0] : null, command, null);
    }

    /// <summary>
    /// The command line for the shell the window starts, or why there is none: a part the shell's rule cannot make
    /// plain, or a shell without a rule (Git Bash, a custom shell and a shell of the user's own have none).
    /// </summary>
    public static (string? Line, string? Refused) Line(ShellKind kind, IReadOnlyList<string> command) => kind switch
    {
        ShellKind.PowerShell7 => ShellQuote.CommandLine("pwsh.exe", command),
        ShellKind.WindowsPowerShell => ShellQuote.CommandLine("powershell.exe", command),
        ShellKind.CommandPrompt => ShellQuote.CommandLine("cmd.exe", command),
        _ => (null, $"axKit has no quoting rule for {ShellLauncher.Describe(kind)}"),
    };

    /// <summary>
    /// The command field's name while the handed line stands in it as it came (AT-1.5a, 2026-11-22): the caution
    /// as the field's own name, in the place of "Command". NVDA reads a field's name before its text whenever
    /// the focus arrives in it, so the warning comes before the command, every time, and no reading of the opening
    /// window can cut it off, as it once cut the announcement these words were until then. It says the same as
    /// <see cref="Said"/>: put there by another program, not sent, nothing checked, Enter is the user's choice.
    /// </summary>
    public const string FieldName = "Command, put here by another program and not sent. axKit checked nothing of it: read it, and press Enter only if you want to run it";

    /// <summary>
    /// The system line of the transcript: that a line is in the field, not sent, and who put it there; or why
    /// nothing is, which is also spoken once the window shows, since no field text carries it. The line itself is
    /// not said here: NVDA reads the field, where the focus is, under its name of the moment (<see cref="FieldName"/>).
    /// </summary>
    public static string Said(string? refused) => refused is null
        ? "A command is in the command field, not sent. Another program put it there, and axKit checked nothing of it: read it, and press Enter only if you want to run it"
        : $"A command was handed to this window and is not in the command field: {refused}";
}
