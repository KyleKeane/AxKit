using AxDescribe.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxDescribe;

/// <summary>
/// axDescribe's entry point inside axKit (docs/axKit/SPEC.md AX-1.7; SPEC.md DE-1): <c>AxKit.exe describe [--hidden |
/// &lt;picture&gt;]</c> calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/>. One axDescribe runs at a
/// time, held by a named mutex, as axStart: a start while it runs hands "show", or "open" with the picture file
/// (DE-2.7), to its window (<see cref="AppMessage"/>) and ends, or with --hidden just ends; otherwise
/// <see cref="DescribeForm"/> runs, hidden with --hidden, or on the picture file.
/// </summary>
public static class AxDescribeApp
{
    public const string Name = "axDescribe";

    public const string Usage = """
        Usage: axKit describe [--hidden | <picture>]
               axDescribe           the same, as the installed console command

          (nothing)         the window in front described, and axDescribe's field; axDescribe then keeps running and its hotkey describes the window in front
          <picture>         that picture file described instead (a .png, .jpg, .jpeg, .gif or .webp file); a running axDescribe takes it
          --hidden          axDescribe running with its window hidden until the hotkey, as at sign-in
          --help, --version this text, or the version
        """;

    /// <summary>Held for the life of the process, so that a second start finds the first (DE-1.2).</summary>
    private static Mutex? _one;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axDescribe", Usage, args, arguments =>
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
                    AppHost.BadCommandLine(Name, $"{Name} describes one picture.", Usage);
                    return null;
                }
                else
                {
                    picture = Path.GetFullPath(argument);
                }
            }

            // DE-2.7: a file that is not a picture, or is gone, is refused here, before any window or hand-over.
            if (picture is not null && (hidden ? "--hidden and a picture do not go together." : PictureFile.Refusal(picture)) is { } refusal)
            {
                AppHost.BadCommandLine(Name, refusal, Usage);
                return null;
            }

            _one = new Mutex(initiallyOwned: true, AppPaths.OneAtATime("AxDescribe"), out var first);
            if (!first)
            {
                _one.Dispose();
                _one = null;
                if (HotkeyForm.FindRunning(Name) is var running and not 0)
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

            var settings = DescribeSettings.Load(DescribeSettings.DefaultPath, out var settingsError);
            return new DescribeForm(hidden, settings, settingsError, picture);
        });
}
