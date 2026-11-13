using AxHelp.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxHelp;

/// <summary>
/// axHelp's entry point inside axKit (docs/axKit/SPEC.md AX-1.7; SPEC.md HE-1): <c>AxKit.exe help [--hidden |
/// &lt;picture&gt;]</c> calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/>; <c>AxKit.exe describe</c>,
/// axDescribe's verb until the two merged in 5.7.0, comes here too. One axHelp runs at a time, held by a named mutex,
/// as axStart: a start while it runs hands "show", or "open" with the picture file, to its hidden window
/// (<see cref="AppMessage"/>) and ends, or with --hidden just ends; otherwise <see cref="HelpForm"/> runs.
/// </summary>
public static class AxHelpApp
{
    public const string Name = "axHelp";

    /// <summary>
    /// The title of axHelp's own window, by which a second start finds it (HE-1.2): not "axHelp", which is the
    /// conversation's (<see cref="HelpForm.ConversationTitle"/>), so that a start never hands its message to axClaude.
    /// </summary>
    public const string WindowTitle = "axHelp hotkeys";

    public const string Usage = """
        Usage: axKit help [--hidden | <picture>]
               axHelp               the same, as the installed console command

          (nothing)         axHelp's window, which says what its two hotkeys do; axHelp then keeps running in the background
          <picture>         that picture file put into the conversation (a .png, .jpg, .jpeg, .gif or .webp file)
          --hidden          axHelp running in the background until a hotkey, as at sign-in
          --help, --version this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axHelp", Usage, args, arguments =>
        {
            var hidden = false;
            string? picture = null;
            foreach (var argument in arguments)
            {
                if (argument == "--hidden")
                {
                    hidden = true;
                }
                else if (argument.StartsWith("--", StringComparison.Ordinal))
                {
                    AppHost.BadCommandLine(Name, $"Unknown argument {argument}.", Usage);
                    return null;
                }
                else if (picture is not null)
                {
                    AppHost.BadCommandLine(Name, $"{Name} takes one picture.", Usage);
                    return null;
                }
                else
                {
                    picture = Path.GetFullPath(argument);
                }
            }

            // HE-3.3: a file that is not a picture, or is gone, is refused here, before any window or hand-over.
            if (picture is not null && (hidden ? "--hidden and a picture do not go together." : PictureFile.Refusal(picture)) is { } refusal)
            {
                AppHost.BadCommandLine(Name, refusal, Usage);
                return null;
            }

            if (!AppHost.FirstInstance("AxHelp"))
            {
                if (HotkeyForm.FindRunning(WindowTitle) is var running and not 0)
                {
                    if (picture is not null)
                    {
                        AppMessage.SendTo(running, Name, AppMessage.Open, [picture]);
                    }
                    else if (!hidden)
                    {
                        AppMessage.SendTo(running, Name, AppMessage.Show, []);
                    }
                }

                return null;
            }

            var settings = HelpSettings.Load(HelpSettings.DefaultPath, out var settingsError);
            return new HelpForm(hidden, settings, settingsError, picture);
        });
}
