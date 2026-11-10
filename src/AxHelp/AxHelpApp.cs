using AxHelp.Core;
using AxKit.Forms;

namespace AxHelp;

/// <summary>
/// axHelp's entry point inside axKit (docs/axkit/SPEC.md AX-1.7; SPEC.md HE-1): <c>AxKit.exe help [--hidden]</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/>. One axHelp runs at a time, held by a named
/// mutex, as axStart: a start while it runs hands "show" to its hidden window (<see cref="AppMessage"/>) and ends,
/// or with --hidden just ends; otherwise <see cref="HelpForm"/> runs, hidden with --hidden.
/// </summary>
public static class AxHelpApp
{
    public const string Name = "axHelp";

    public const string Usage = """
        Usage: axKit help [--hidden]
               axhelp               the same, as the installed console command

          (nothing)         axHelp's question field, in front; axHelp then keeps running and its hotkey brings the field back
          --hidden          axHelp running with its window hidden until the hotkey, as at sign-in
          --help, --version this text, or the version
        """;

    /// <summary>Held for the life of the process, so that a second start finds the first (HE-1.2).</summary>
    private static Mutex? _one;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axhelp", Usage, args, arguments =>
        {
            var hidden = false;
            foreach (var argument in arguments)
            {
                if (argument == "--hidden")
                {
                    hidden = true;
                }
                else
                {
                    AppHost.BadCommandLine(Name, $"Unknown argument {argument}.", Usage);
                    return null;
                }
            }

            _one = new Mutex(initiallyOwned: true, @"Local\AxKit.AxHelp", out var first);
            if (!first)
            {
                _one.Dispose();
                _one = null;
                if (!hidden && HotkeyForm.FindRunning(Name) is var running and not 0)
                {
                    AppMessage.SendTo(running, Name, AppMessage.Show, []);
                }

                return null;
            }

            var settings = HelpSettings.Load(HelpSettings.DefaultPath, out var settingsError);
            return new HelpForm(hidden, settings, settingsError);
        });
}
