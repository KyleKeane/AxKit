using AxKit.TestShared;
using AxMCP.Core;

namespace AxMCP.Tests;

/// <summary>The program a server's command names (docs/axMCP/SPEC.md MP-2.1; docs/axKit/repairs.json R-25).</summary>
public sealed class StdioServerTests : IDisposable
{
    private readonly TempFolder _temp = new("axmcp-stdio");

    public void Dispose() => _temp.Dispose();

    /// <summary>
    /// MP-2.1b (2026-11-27): a path that is not full names a file of the folder the server starts in, never of the
    /// folder axKit's own program runs in, which is where Windows would look for it.
    /// </summary>
    [Fact]
    public void A_full_path_is_taken_as_it_is_and_another_path_from_the_folder_the_server_starts_in()
    {
        Assert.Equal(@"C:\Tools\server.exe", StdioServer.Resolve(@"C:\Tools\server.exe", null, @"D:\project"));
        Assert.Equal(@"D:\project\server.exe", StdioServer.Resolve(@".\server.exe", null, @"D:\project"));
        Assert.Equal(@"D:\project\tools\server.cmd", StdioServer.Resolve(@"tools\server.cmd", null, @"D:\project\"));
        Assert.Equal(@"D:\project\tools\server.cmd", StdioServer.Resolve("tools/server.cmd", null, @"D:\project"));
        Assert.Equal(@"D:\server.exe", StdioServer.Resolve(@"..\server.exe", null, @"D:\project"));
        Assert.Equal(@"\\server\share\mcp.exe", StdioServer.Resolve(@"\\server\share\mcp.exe", null, @"D:\project"));
    }

    /// <summary>The same through a real start: the program found is the folder's own, and it runs.</summary>
    [Fact]
    public void A_server_named_by_a_path_that_is_not_full_starts_from_its_own_folder()
    {
        var folder = Path.GetDirectoryName(_temp.File(Path.Combine("project", "tools", "said.cmd"), "@echo off\r\necho from-the-folder 1>&2\r\n"))!;
        var project = Path.GetDirectoryName(folder)!;
        var server = new McpServer("fake", ServerKind.Stdio, @"tools\said.cmd", [], new Dictionary<string, string>(), null, "test", project, Own: true);
        using var running = StdioServer.Start(server, project, _ => { });
        for (var tries = 0; tries < 100 && running.ErrorLines.Count == 0; tries++)
        {
            Thread.Sleep(50);
        }

        Assert.Contains("from-the-folder", string.Join("\n", running.ErrorLines.Last(10)));
    }

    /// <summary>
    /// MP-2.3 (2026-11-27): Stop closes the input and returns at once; the two seconds a server has to end by itself,
    /// and its end with what it started, run off the caller's thread, which is the window's.
    /// </summary>
    [Fact]
    public async Task Stop_returns_at_once_and_a_server_that_ignores_its_closed_input_is_ended_after_the_two_seconds()
    {
        // ping reads no input and goes on for a minute: closing its input ends nothing.
        var ping = Path.Combine(Environment.SystemDirectory, "ping.exe");
        var server = new McpServer("stays", ServerKind.Stdio, ping, ["-n", "60", "127.0.0.1"], new Dictionary<string, string>(), null, "test", _temp.Path, Own: true);
        var exited = new TaskCompletionSource<(int Code, bool Stopping)>();
        var running = StdioServer.Start(server, _temp.Path, _ => { });
        running.Exited += (code, stopping) => exited.TrySetResult((code, stopping));
        Assert.True(running.Ending.IsCompleted);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        running.Stop();
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1), $"Stop took {watch.ElapsedMilliseconds} ms on the caller's thread");
        Assert.False(running.HasExited);
        // Asked again, it is the one end.
        var ending = running.Ending;
        running.Stop();
        Assert.Same(ending, running.Ending);

        await running.Ending.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.True(watch.Elapsed >= StdioServer.Grace - TimeSpan.FromMilliseconds(200), "The server was not given its two seconds");
        var end = await exited.Task.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.True(end.Stopping);
        Assert.True(running.HasExited);
        running.Dispose();
    }

    /// <summary>The one ping this test's server starts, known by a count no other test asks for.</summary>
    private static System.Diagnostics.Process? LeftBehind() =>
        System.Diagnostics.Process.GetProcessesByName("PING").FirstOrDefault(process => AxKit.Core.ThisProgram.CommandLineOf((uint)process.Id)?.Contains("-n 47 ", StringComparison.Ordinal) == true);

    /// <summary>
    /// MP-2.3 (the review's second pass of 2026-11-27): a server that ends by itself ends alone. What it started
    /// stays, as a browser it opened for a sign-in would, through Stop and through the server's being let go;
    /// until then every Stop ended everything the server had ever started. Here the server is a command that
    /// starts a ping and ends at once.
    /// </summary>
    [Fact]
    public async Task What_a_server_that_ended_by_itself_had_started_is_left_running()
    {
        var cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var server = new McpServer("leaves", ServerKind.Stdio, cmd, ["/d", "/c", "start /b ping -n 47 127.0.0.1 >nul"], new Dictionary<string, string>(), null, "test", _temp.Path, Own: true);
        var running = StdioServer.Start(server, _temp.Path, _ => { });
        System.Diagnostics.Process? left = null;
        try
        {
            for (var tries = 0; tries < 200 && (left is null || !running.HasExited); tries++)
            {
                await Task.Delay(50);
                left ??= LeftBehind();
            }

            Assert.True(running.HasExited, "The server did not end by itself");
            Assert.NotNull(left);

            running.Stop();
            await running.Ending.WaitAsync(TimeSpan.FromSeconds(20));
            running.Dispose();
            await Task.Delay(1000);
            Assert.False(left.HasExited, "What the server had started was ended with it, though the server ended by itself");
        }
        finally
        {
            // The test's own process, by its id; gone already when the assertion above failed.
            if (left is { HasExited: false })
            {
                left.Kill();
            }

            left?.Dispose();
        }
    }

    [Fact]
    public void A_bare_name_is_found_in_the_PATH_s_full_folders_with_exe_then_cmd_then_bat()
    {
        var first = Path.GetDirectoryName(_temp.File(Path.Combine("one", "npx.cmd"), "@echo off"))!;
        var second = Path.GetDirectoryName(_temp.File(Path.Combine("two", "node.exe"), "x"))!;
        _temp.File(Path.Combine("two", "npx.bat"), "@echo off");
        var path = $"relative;{first};{second}";
        // The folder the server starts in is never looked in for a bare name, not even when it holds one.
        var folder = Path.GetDirectoryName(_temp.File(Path.Combine("start", "node.exe"), "x"))!;
        Assert.Equal(Path.Combine(second, "node.exe"), StdioServer.Resolve("node", path, folder));
        Assert.Equal(Path.Combine(first, "npx.cmd"), StdioServer.Resolve("npx", path, folder));
        Assert.Equal(Path.Combine(second, "npx.bat"), StdioServer.Resolve("npx.bat", path, folder));
    }

    [Fact]
    public void A_name_no_folder_of_the_PATH_holds_is_refused_by_name_with_where_to_read()
    {
        var problem = Assert.Throws<FileNotFoundException>(() => StdioServer.Resolve("nothing-like-this", _temp.Path, _temp.Path));
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
