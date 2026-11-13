using AxKit.TestShared;
using AxMCP.Core;

namespace AxMCP.Tests;

/// <summary>The program a server's command names (docs/axMCP/SPEC.md MP-2.1; docs/axKit/repairs.json R-25).</summary>
public sealed class StdioServerTests : IDisposable
{
    private readonly TempFolder _temp = new("axmcp-stdio");

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void A_path_is_taken_as_it_is()
    {
        Assert.Equal(@"C:\Tools\server.exe", StdioServer.Resolve(@"C:\Tools\server.exe", null));
        Assert.Equal(@".\server.exe", StdioServer.Resolve(@".\server.exe", null));
    }

    [Fact]
    public void A_bare_name_is_found_in_the_PATH_s_full_folders_with_exe_then_cmd_then_bat()
    {
        var first = Path.GetDirectoryName(_temp.File(Path.Combine("one", "npx.cmd"), "@echo off"))!;
        var second = Path.GetDirectoryName(_temp.File(Path.Combine("two", "node.exe"), "x"))!;
        _temp.File(Path.Combine("two", "npx.bat"), "@echo off");
        var path = $"relative;{first};{second}";
        Assert.Equal(Path.Combine(second, "node.exe"), StdioServer.Resolve("node", path));
        Assert.Equal(Path.Combine(first, "npx.cmd"), StdioServer.Resolve("npx", path));
        Assert.Equal(Path.Combine(second, "npx.bat"), StdioServer.Resolve("npx.bat", path));
    }

    [Fact]
    public void A_name_no_folder_of_the_PATH_holds_is_refused_by_name_with_where_to_read()
    {
        var problem = Assert.Throws<FileNotFoundException>(() => StdioServer.Resolve("nothing-like-this", _temp.Path));
        Assert.Contains("nothing-like-this", problem.Message);
        Assert.Contains("docs/axMCP/user-guide.md", problem.Message);
    }
}
