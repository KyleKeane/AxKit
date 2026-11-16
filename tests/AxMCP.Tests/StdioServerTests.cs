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

    /// <summary>MP-10 (bundle repairs.md R-74): the log holds a line's number and length, the text is kept for the window alone.</summary>
    [Fact]
    public void An_error_line_is_kept_for_the_window_and_only_its_length_is_logged()
    {
        var lines = new ServerErrorLines();
        const string written = "token sk-secret, folder D:\\work";
        var logged = lines.Add("fake", written);
        Assert.Equal($"fake: error line 1, {written.Length} characters", logged);
        Assert.DoesNotContain("secret", logged, StringComparison.Ordinal);
        Assert.Equal([written], lines.Last(10));

        var longLine = new string('x', ServerErrorLines.MaxLineLength + 5);
        Assert.Equal($"fake: error line 2, {longLine.Length} characters", lines.Add("fake", longLine));
        Assert.Equal(ServerErrorLines.MaxLineLength + 3, lines.Last(1)[0].Length);

        for (var i = 3; i <= ServerErrorLines.Kept + 10; i++)
        {
            lines.Add("fake", "line " + i);
        }

        Assert.Equal(ServerErrorLines.Kept + 10, lines.Count);
        Assert.Equal(ServerErrorLines.Kept, lines.Last(1000).Count);
        Assert.Equal(["line " + (ServerErrorLines.Kept + 9), "line " + (ServerErrorLines.Kept + 10)], lines.Last(2));

        Assert.Equal("Error lines: none", ServerErrorLines.Summary(0, 0));
        Assert.Equal("Error lines: 2, shown below", ServerErrorLines.Summary(2, 2));
        Assert.Equal("Error lines: 80, the last 10 shown below", ServerErrorLines.Summary(80, 10));
    }

    /// <summary>The same through a real process: what a server writes to its standard error never reaches the log's delegate.</summary>
    [Fact]
    public void What_a_started_server_writes_to_its_standard_error_is_not_in_the_log()
    {
        const string secret = "token-abc123";
        var logged = new System.Collections.Concurrent.ConcurrentQueue<string>();
        var server = new McpServer("fake", ServerKind.Stdio, AxKit.Core.SystemPrograms.Cmd, ["/c", $"echo {secret} 1>&2"], new Dictionary<string, string>(), null, "test", _temp.Path, Own: true);
        using var running = StdioServer.Start(server, _temp.Path, logged.Enqueue);
        for (var tries = 0; tries < 100 && running.ErrorLines.Count == 0; tries++)
        {
            Thread.Sleep(50);
        }

        Assert.Contains(running.ErrorLines.Last(10), line => line.Contains(secret, StringComparison.Ordinal));
        Assert.Contains(logged, line => line.StartsWith("fake: error line 1, ", StringComparison.Ordinal));
        Assert.DoesNotContain(logged, line => line.Contains(secret, StringComparison.Ordinal));
    }
}
