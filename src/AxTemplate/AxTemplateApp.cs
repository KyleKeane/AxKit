using AxTemplate.Core;
using AxKit.Forms;

namespace AxTemplate;

/// <summary>
/// axTemplate's entry point inside axKit (docs/axKit/SPEC.md AX-1.7): <c>AxKit.exe template</c> calls <see cref="Run"/>, one
/// call into the bundle's <see cref="AppHost"/> with the settings and the browser, <see cref="BrowserForm"/>
/// (SPEC.md TP-1.1). It takes no path; any argument is reported with the usage text. The launcher behind the
/// hotkey (TP-1.2, <c>--launcher</c>) is the next step of SPEC.md section 8.
/// </summary>
public static class AxTemplateApp
{
    public const string Name = "axTemplate";

    public const string Usage = """
        Usage: AxKit template
               axTemplate            the same, as the installed console command

          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axTemplate", Usage, args, arguments =>
        {
            if (arguments.Length > 0)
            {
                AppHost.BadCommandLine(Name, $"axTemplate takes no arguments: {string.Join(" ", arguments)}", Usage);
                return null;
            }

            var settings = TemplateSettings.Load(TemplateSettings.DefaultPath, out var settingsError);
            return new BrowserForm(settings, settingsError);
        });
}
