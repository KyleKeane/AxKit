using AxDescribe.Core;
using AxKit.Forms;

namespace AxDescribe;

/// <summary>
/// axDescribe's entry point inside axKit (docs/axkit/SPEC.md AX-1.7): <c>AxKit.exe describe</c> calls <see cref="Run"/>, one
/// call into the bundle's <see cref="AppHost"/> with the settings and <see cref="DescribeForm"/>. It takes no path;
/// any argument is reported with the usage text.
/// </summary>
public static class AxDescribeApp
{
    public const string Name = "axDescribe";

    public const string Usage = """
        Usage: AxKit describe
               axdescribe            the same, as the installed console command

          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axdescribe", Usage, args, arguments =>
        {
            if (arguments.Length > 0)
            {
                AppHost.BadCommandLine(Name, $"axDescribe takes no arguments: {string.Join(" ", arguments)}", Usage);
                return null;
            }

            var settings = DescribeSettings.Load(DescribeSettings.DefaultPath, out var settingsError);
            return new DescribeForm(null, settings, settingsError);
        });
}
