using System.Diagnostics;
using AxClaude.Core.Pty;

namespace AxClaude.Tests;

public class ClaudeLauncherTests
{
    /// <summary>A fake claude.cmd with the body given, in a folder of its own that goes with the test.</summary>
    private sealed class FakeClaude : IDisposable
    {
        private readonly TempFolder _dir = new("axclaude-tests");

        public FakeClaude(string body)
        {
            Command = _dir.File("claude.cmd", "@echo off\r\n" + body + "\r\n");
        }

        public string Command { get; }

        public void Dispose() => _dir.Dispose();
    }

    [Fact]
    public async Task The_version_is_the_first_line_claude_prints_for_version()
    {
        // %~1: the argument without the quotes every argument of a batch program travels in.
        using var claude = new FakeClaude("echo 2.0.30 %~1\r\necho a second line");
        Assert.Equal("2.0.30 --version", await ClaudeLauncher.VersionAsync(claude.Command, TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task A_claude_that_does_not_answer_in_time_gives_no_version_and_is_ended()
    {
        using var claude = new FakeClaude("ping -n 30 127.0.0.1 >nul\r\necho 9.9.9");
        var watch = Stopwatch.StartNew();
        Assert.Null(await ClaudeLauncher.VersionAsync(claude.Command, TimeSpan.FromSeconds(1)));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20), $"took {watch.Elapsed}");
    }

    [Fact]
    public async Task A_claude_that_cannot_start_gives_no_version()
    {
        var nowhere = Path.Combine(Path.GetTempPath(), "axclaude-nowhere-" + Guid.NewGuid().ToString("N"), "claude.exe");
        Assert.Null(await ClaudeLauncher.VersionAsync(nowhere, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Split_at_whitespace_including_new_lines()
    {
        Assert.Equal(["--continue", "--permission-mode", "plan"], ClaudeLauncher.SplitArguments("  --continue\r\n--permission-mode   plan \n"));
    }

    [Fact]
    public void Empty_and_blank_text_give_no_arguments()
    {
        Assert.Empty(ClaudeLauncher.SplitArguments(string.Empty));
        Assert.Empty(ClaudeLauncher.SplitArguments(" \r\n\t"));
    }

    [Fact]
    public void Quotes_keep_spaces_and_an_escaped_quote_is_literal()
    {
        Assert.Equal(
            ["--append-system-prompt", "Always say \"hi\" first", "--name", "my feature"],
            ClaudeLauncher.SplitArguments("--append-system-prompt \"Always say \\\"hi\\\" first\" --name \"my feature\""));
        Assert.Equal(["ab c"], ClaudeLauncher.SplitArguments("a\"b c\""));
        Assert.Equal([string.Empty], ClaudeLauncher.SplitArguments("\"\""));
    }

    [Fact]
    public void Join_and_split_round_trip()
    {
        string[] arguments = ["--continue", "--name", "my feature", "say \"hi\"", string.Empty];
        var line = ClaudeLauncher.JoinArguments(arguments);
        Assert.Equal("--continue --name \"my feature\" \"say \\\"hi\\\"\" \"\"", line);
        Assert.Equal(arguments, ClaudeLauncher.SplitArguments(line));
    }

    [Fact]
    public void Backslashes_follow_the_windows_rules()
    {
        // Literal everywhere but in front of a quote: a quoted folder with its trailing backslash keeps the arguments after it.
        string[] arguments = ["--add-dir", @"C:\Dir With Space\", @"C:\plain\", @"back\\slash", "odd\\\"quote"];
        var line = ClaudeLauncher.JoinArguments(arguments);
        Assert.Equal(@"--add-dir ""C:\Dir With Space\\"" C:\plain\ back\\slash ""odd\\\""quote""", line);
        Assert.Equal(arguments, ClaudeLauncher.SplitArguments(line));
        Assert.Equal([@"C:\Dir With Space\", "--verbose"], ClaudeLauncher.SplitArguments(@"""C:\Dir With Space\\"" --verbose"));
    }

    [Fact]
    public void The_command_line_quotes_the_executable_and_the_arguments_that_need_it()
    {
        Assert.Equal(
            @"""C:\Tools\claude.exe"" --ax-screen-reader --name ""my feature""",
            ClaudeLauncher.BuildCommandLine(@"C:\Tools\claude.exe", ["--ax-screen-reader", "--name", "my feature"]).CommandLine);
        // The native program takes any character: nothing is refused.
        var odd = ClaudeLauncher.BuildCommandLine(@"C:\Tools\claude.exe", ["--add-dir", @"C:\R&D", "--name", "100% wow!"]);
        Assert.Null(odd.Refusal);
        Assert.Equal(@"""C:\Tools\claude.exe"" --add-dir C:\R&D --name ""100% wow!""", odd.CommandLine);
    }

    [Fact]
    public void A_cmd_wrapper_runs_through_cmd_exe_with_the_whole_line_quoted_once_more()
    {
        // cmd /s strips the outer quotes, leaving the quoted path and the arguments, each in quotes of its own; the extension's case does not matter.
        // S-1: cmd.exe by its full path, never one in the project folder.
        var cmd = "\"" + AxKit.Core.SystemPrograms.Cmd + "\"";
        Assert.Equal(
            cmd + @" /d /s /c """"C:\npm\claude.cmd"" ""--name"" ""my feature""""",
            ClaudeLauncher.BuildCommandLine(@"C:\npm\claude.cmd", ["--name", "my feature"]).CommandLine);
        Assert.StartsWith(cmd + " /d /s /c ", ClaudeLauncher.BuildCommandLine(@"C:\npm\CLAUDE.CMD", ["--continue"]).CommandLine);
    }

    [Fact]
    public void A_cmd_wrapper_gets_a_folder_named_R_and_D_in_quotes_and_is_refused_what_no_quoting_makes_safe()
    {
        // docs/axKit/repairs.json R-24.
        var cmd = "\"" + AxKit.Core.SystemPrograms.Cmd + "\"";
        var line = ClaudeLauncher.BuildCommandLine(@"C:\npm\claude.cmd", ["--ax-screen-reader", "--add-dir", @"C:\work\R&D"]);
        Assert.Null(line.Refusal);
        Assert.Equal(cmd + @" /d /s /c """"C:\npm\claude.cmd"" ""--ax-screen-reader"" ""--add-dir"" ""C:\work\R&D""""", line.CommandLine);

        foreach (var (argument, character) in new[] { (@"C:\work\100%", "a percent sign (%)"), ("wow!", "an exclamation mark (!)"), ("say \"hi\"", "a double quote (\")") })
        {
            var refusal = ClaudeLauncher.BuildCommandLine(@"C:\npm\claude.cmd", ["--name", argument]).Refusal;
            Assert.NotNull(refusal);
            Assert.StartsWith("claude.cmd is a batch program, which Windows runs through cmd.exe, and \"" + argument + "\" holds " + character, refusal);
            Assert.EndsWith(AxKit.Core.ClaudeCodeProgram.BatchAdvice, refusal);
        }
    }

    [Fact]
    public async Task A_real_claude_cmd_receives_the_arguments_as_they_were_sent()
    {
        // The session's own line, run by cmd.exe: what the batch file got, split as Windows splits it, is what was sent.
        using var claude = new FakeClaude(">\"%~dp0out.txt\" echo %*");
        string[] sent = ["--add-dir", @"C:\work\R&D", "--name", "a^b it's (1)", "x&mkdir pwned", @"C:\dir with space\"];
        var line = ClaudeLauncher.BuildCommandLine(claude.Command, sent);
        Assert.Null(line.Refusal);
        var folder = Path.GetDirectoryName(claude.Command)!;
        var result = await AxKit.Core.ProcessRun.RunAsync(new ProcessStartInfo(line.FileName, line.Arguments) { WorkingDirectory = folder }, [], TimeSpan.FromSeconds(30));
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(sent, ClaudeLauncher.SplitArguments(File.ReadAllText(Path.Combine(folder, "out.txt"))));
        Assert.False(Directory.Exists(Path.Combine(folder, "pwned")));
    }

    [Fact]
    public async Task A_claude_cmd_in_a_folder_cmd_would_act_on_is_not_asked_its_version()
    {
        using var temp = new TempFolder("axclaude-tests");
        var claude = temp.File(@"R&D\claude.cmd", "@echo 9.9.9\r\n");
        Assert.Null(await ClaudeLauncher.VersionAsync(claude, TimeSpan.FromSeconds(10)));
    }
}
