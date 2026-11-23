using AxKit.Core;
using AxKit.Forms;
using AxTemplate.Core;

namespace AxTemplate;

/// <summary>
/// axTemplate's entry point inside axKit (docs/axKit/SPEC.md AX-1.7): <c>AxKit.exe template</c> calls <see cref="Run"/>, one
/// call into the bundle's <see cref="AppHost"/>. Two windows, each a start of its own (SPEC.md TP-1, TP-D8):
/// a plain start shows the browser, <see cref="BrowserForm"/>, and starts the launcher in the background when it
/// does not run yet (TP-1.1); <c>--launcher</c> is the launcher behind the hotkey, <see cref="LauncherForm"/>, one
/// copy at a time held by a named mutex as axClip's (TP-1.2): a second start of it hands "show" to the running
/// window and ends, or with <c>--hidden</c> just ends.
/// </summary>
public static class AxTemplateApp
{
    public const string Name = "axTemplate";

    /// <summary>The argument that starts the launcher and not the browser.</summary>
    public const string LauncherArgument = "--launcher";

    private const string HiddenArgument = "--hidden";

    public const string Usage = """
        Usage: axKit template [--launcher | --hidden]
               axTemplate            the same, as the installed console command

          (nothing)          the browser, where templates are made and changed; the launcher is started with it
          --launcher         the launcher's search field, in front; it then keeps running and its hotkey brings it back
          --hidden           the launcher running with its window hidden until the hotkey, as at sign-in
          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axTemplate", Usage, args, arguments =>
        {
            var launcher = false;
            var hidden = false;
            foreach (var argument in arguments)
            {
                if (argument == LauncherArgument)
                {
                    launcher = true;
                }
                else if (argument == HiddenArgument)
                {
                    hidden = true;
                }
                else
                {
                    AppHost.BadCommandLine(Name, $"Unknown argument {argument}.", Usage);
                    return null;
                }
            }

            // --hidden alone is the launcher too: the browser is never hidden, and the bundle's sign-in start writes
            // "<verb> --hidden" for every background app (AxKit.Core/SignInStart).
            if (launcher || hidden)
            {
                // One launcher; the running one shows its field, unless this start only wanted it running. A running
                // one that does not answer is asked about (bundle AX-1.9).
                if (!AppHost.OneCopy(Name, "AxTemplateLauncher", Dispatch.AxTemplate, Name, hidden,
                    running => hidden ? AppMessage.Answers(running) : AppMessage.SendTo(running, Name, AppMessage.Show, [])))
                {
                    return null;
                }

                var launcherSettings = TemplateSettings.Load(TemplateSettings.DefaultPath, out var launcherError);
                return new LauncherForm(hidden, launcherSettings, launcherError);
            }

            // TP-1.1: the hotkey works from the first plain start on. The launcher's window is the one titled with
            // the app's name alone (HotkeyForm); the browser's title ends in it.
            if (HotkeyForm.FindRunning(Name) == 0)
            {
                WindowMenu.StartWith(Dispatch.AxTemplate, [HiddenArgument]);
            }

            var settings = TemplateSettings.Load(TemplateSettings.DefaultPath, out var settingsError);
            return new BrowserForm(settings, settingsError);
        });
}
