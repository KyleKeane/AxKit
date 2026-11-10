using AxDescribe.Core;
using AxKit.Forms;

namespace AxDescribe;

/// <summary>
/// axDescribe's entry point inside axKit (docs/axkit/SPEC.md AX-1.7; SPEC.md DE-1): <c>AxKit.exe describe [--hidden]</c>
/// calls <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/>. One axDescribe runs at a time, held by
/// a named mutex, as axStart: a start while it runs hands "show" to its hidden window (<see cref="AppMessage"/>) and
/// ends, or with --hidden just ends; otherwise <see cref="DescribeForm"/> runs, hidden with --hidden.
/// </summary>
public static class AxDescribeApp
{
    public const string Name = "axDescribe";

    public const string Usage = """
        Usage: axKit describe [--hidden]
               axdescribe           the same, as the installed console command

          (nothing)         the window in front described, and axDescribe's field; axDescribe then keeps running and its hotkey describes the window in front
          --hidden          axDescribe running with its window hidden until the hotkey, as at sign-in
          --help, --version this text, or the version
        """;

    /// <summary>Held for the life of the process, so that a second start finds the first (DE-1.2).</summary>
    private static Mutex? _one;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axdescribe", Usage, args, arguments =>
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

            _one = new Mutex(initiallyOwned: true, @"Local\AxKit.AxDescribe", out var first);
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

            var settings = DescribeSettings.Load(DescribeSettings.DefaultPath, out var settingsError);
            return new DescribeForm(hidden, settings, settingsError);
        });
}
