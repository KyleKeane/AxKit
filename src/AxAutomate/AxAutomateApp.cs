using AxAutomate.Core;
using AxKit.Forms;

namespace AxAutomate;

/// <summary>
/// axAutomate's entry point inside axKit (docs/axKit/SPEC.md AX-1.7): <c>AxKit.exe automate [&lt;folder&gt;]</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one path of the command line, the
/// settings and <see cref="AutomateForm"/>.
/// </summary>
public static class AxAutomateApp
{
    public const string Name = "axAutomate";

    public const string Usage = """
        Usage: AxKit automate [<folder>]
               axAutomate [<folder>]  the same, as the installed console command

          <folder>  what axAutomate opens; without one, nothing
          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axAutomate", Usage, args, arguments =>
        {
            if (!AppHost.OnePath(Name, "folder", Usage, arguments, out var path))
            {
                return null;
            }

            var settings = AutomateSettings.Load(AutomateSettings.DefaultPath, out var settingsError);
            return new AutomateForm(path, settings, settingsError);
        });
}
