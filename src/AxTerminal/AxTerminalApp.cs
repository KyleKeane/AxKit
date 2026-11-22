using AxKit.Forms;
using AxTerminal.Core;

namespace AxTerminal;

/// <summary>
/// axTerminal's entry point inside axKit (docs/axKit/SPEC.md AX-1.7; SPEC.md AT-1.5): <c>AxKit.exe terminal [&lt;folder&gt;]</c>
/// calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one folder of the command line,
/// the settings and <see cref="TerminalForm"/>.
/// </summary>
public static class AxTerminalApp
{
    public const string Name = "axTerminal";

    public const string Usage = """
        Usage: axKit terminal [<folder>] [--command <program> [<argument> ...]]
               axterminal [<folder>]  the same, as the installed console command

          <folder>          the folder the shell starts in; without one, the current folder
          --command         everything after it is one command, the program and then its arguments,
                            put into the command field for you to read and send; it is never sent for you
          --help, --version this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axTerminal", Usage, args, arguments =>
        {
            var (folder, command, problem) = HandedCommand.Parse(arguments);
            if (problem is not null)
            {
                AppHost.BadCommandLine(Name, problem, Usage);
                return null;
            }

            var settings = TerminalSettings.Load(TerminalSettings.DefaultPath, out var settingsError);
            return new TerminalForm(folder is null ? null : Path.GetFullPath(folder), settings, settingsError, command);
        });
}
