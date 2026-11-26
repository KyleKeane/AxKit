using AxClaude.Core;
using AxKit.Forms;

namespace AxClaude;

/// <summary>
/// axClaude's entry point inside axKit (docs/axKit/SPEC.md AX-1.7): <c>AxKit.exe claude [arguments]</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with axClaude's own command line
/// (<see cref="StartupOptions"/>), the settings and <see cref="MainForm"/>.
/// </summary>
public static class AxClaudeApp
{
    public static void Run(string[] args) =>
        AppHost.Run("axClaude", "axClaude", StartupOptions.Usage, args, arguments =>
        {
            StartupOptions options;
            try
            {
                options = StartupOptions.Parse(arguments);
            }
            catch (ArgumentException ex)
            {
                AppHost.BadCommandLine("axClaude", ex.Message, usage: null);
                return null;
            }

            var settings = AppSettings.Load(AppSettings.DefaultPath, out var settingsError);
            return new MainForm(options, settings, settingsError);
        });
}
