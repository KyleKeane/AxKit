using AxMCP.Core;
using AxKit.Forms;

namespace AxMCP;

/// <summary>
/// axMCP's entry point inside axKit (docs/axkit/SPEC.md AX-1.7): <c>AxKit.exe mcpclient [&lt;folder&gt;]</c> calls
/// <see cref="Run"/>, one call into the bundle's <see cref="AppHost"/> with the one path of the command line, the
/// settings and <see cref="MCPForm"/>.
/// </summary>
public static class AxMCPApp
{
    public const string Name = "axMCP";

    public const string Usage = """
        Usage: AxKit mcpclient [<folder>]
               axmcp [<folder>]  the same, as the installed console command

          <folder>  what axMCP opens; without one, nothing
          --help, --version  this text, or the version
        """;

    public static void Run(string[] args) =>
        AppHost.Run(Name, "axmcp", Usage, args, arguments =>
        {
            if (!AppHost.OnePath(Name, "folder", Usage, arguments, out var path))
            {
                return null;
            }

            var settings = MCPSettings.Load(MCPSettings.DefaultPath, out var settingsError);
            return new MCPForm(path, settings, settingsError);
        });
}
