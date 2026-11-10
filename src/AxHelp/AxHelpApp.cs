using AxHelp.Core;
using AxKit.Forms;

namespace AxHelp;

/// <summary>
/// axHelp's entry point inside axKit (docs/axkit/SPEC.md AX-1.7): <c>AxKit.exe help</c> calls <see cref="Run"/>, one
/// call into the bundle's <see cref="AppHost"/> with the settings and <see cref="HelpForm"/>. It takes no path;
/// any argument is reported with the usage text.
/// </summary>
public static class AxHelpApp
{
    public const string Name = "axHelp";

    public const string Usage = """
        Usage: AxKit help
               axhelp            the same, as the installed console command

          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axhelp", Usage, args, arguments =>
        {
            if (arguments.Length > 0)
            {
                AppHost.BadCommandLine(Name, $"axHelp takes no arguments: {string.Join(" ", arguments)}", Usage);
                return null;
            }

            var settings = HelpSettings.Load(HelpSettings.DefaultPath, out var settingsError);
            return new HelpForm(null, settings, settingsError);
        });
}
