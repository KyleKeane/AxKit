using System.Diagnostics;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The bundle's run of a program to its end (inventory item 3), with Windows' own small programs.</summary>
public class ProcessRunTests
{
    private static string System32(string name) => Path.Combine(Environment.SystemDirectory, name);

    [Fact]
    public async Task The_output_and_the_exit_code_come_back()
    {
        var start = new ProcessStartInfo(SystemPrograms.Cmd);
        start.ArgumentList.Add("/d");
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("echo hello& exit 3");
        var result = await ProcessRun.RunAsync(start, [], TimeSpan.FromSeconds(20));
        Assert.False(result.TimedOut);
        Assert.Equal(3, result.ExitCode);
        Assert.Equal("hello", result.Output?.Trim());
    }

    [Fact]
    public async Task A_program_that_ends_without_reading_its_input_gives_its_words_and_exit_code()
    {
        // docs/axKit/repairs.json R-26: more input than a pipe holds, to a program that reads none of it and ends.
        var start = new ProcessStartInfo(SystemPrograms.Cmd);
        start.ArgumentList.Add("/d");
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("echo refused& exit 5");
        var line = new string('x', 4096);
        var result = await ProcessRun.RunAsync(start, [.. Enumerable.Repeat(line, 200)], TimeSpan.FromSeconds(20));
        Assert.False(result.TimedOut);
        Assert.Equal(5, result.ExitCode);
        Assert.Equal("refused", result.Output?.Trim());
    }

    [Fact]
    public async Task The_input_lines_reach_the_program()
    {
        // findstr prints the lines of its input that hold the text.
        var start = new ProcessStartInfo(System32("findstr.exe"));
        start.ArgumentList.Add("x");
        var result = await ProcessRun.RunAsync(start, ["x one", "two", "x three"], TimeSpan.FromSeconds(20));
        Assert.Equal(["x one", "x three"], (result.Output ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    [Fact]
    public async Task A_program_past_its_time_is_ended_and_said_so()
    {
        var start = new ProcessStartInfo(System32("ping.exe"));
        foreach (var argument in new[] { "-n", "30", "127.0.0.1" })
        {
            start.ArgumentList.Add(argument);
        }

        var watch = Stopwatch.StartNew();
        var result = ProcessRun.Run(start, [], TimeSpan.FromMilliseconds(500));
        Assert.True(result.TimedOut);
        Assert.False(result.Finished);
        Assert.Equal(-1, result.ExitCode);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task A_program_past_its_time_keeps_what_it_printed_until_then()
    {
        // Review of 2026-11-27: a read cancelled at the limit threw its part away, so a run that timed out had
        // printed nothing, whatever it printed.
        var start = new ProcessStartInfo(SystemPrograms.Cmd);
        start.ArgumentList.Add("/d");
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("echo so far& echo oops 1>&2& ping -n 30 127.0.0.1 >nul");
        var result = await ProcessRun.RunAsync(start, [], TimeSpan.FromSeconds(2));
        Assert.True(result.TimedOut);
        Assert.False(result.Finished);
        Assert.Null(result.Output);
        Assert.Equal("so far", result.Printed.Trim());
        Assert.Equal("oops", result.Error.Trim());
    }

    [Fact]
    public async Task A_program_that_ended_while_another_holds_its_output_gives_all_it_printed()
    {
        // The program ends at once with code 4; the ping it started inherits the output pipe and keeps it open.
        var start = new ProcessStartInfo(SystemPrograms.Cmd);
        start.ArgumentList.Add("/d");
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add("echo early& start /b ping -n 20 127.0.0.1 >nul& exit 4");
        var watch = Stopwatch.StartNew();
        var result = await ProcessRun.RunAsync(start, [], TimeSpan.FromSeconds(2));
        Assert.True(result.TimedOut);
        Assert.True(result.Finished);
        Assert.Equal(4, result.ExitCode);
        Assert.Equal("early", result.Output?.Trim());
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(15));
        Assert.False(result.Stopped);
    }

    [Fact]
    public void A_run_ended_at_its_limit_is_stopped_and_one_that_finished_is_not()
    {
        Assert.True(new ProcessResult(-1, "part", string.Empty, TimedOut: true, Finished: false).Stopped);
        Assert.False(new ProcessResult(0, "all", string.Empty, TimedOut: true, Finished: true).Stopped);
        Assert.False(new ProcessResult(0, "all", string.Empty, TimedOut: false, Finished: true).Stopped);
    }

    /// <summary>
    /// A run ended at its time limit gives exit code -1 and a part of the output, which no caller may read as a
    /// result (review of 2026-11-27: git's runner read an empty output with exit code 0 as an empty status). The
    /// compiler holds the callers (the review's second pass: a test that read the sources for the word "Stopped"
    /// held nothing, since the word anywhere in a file satisfied it): the output of a stopped run is null, so a
    /// caller reads <see cref="ProcessResult.Output"/> only after it asked <see cref="ProcessResult.Stopped"/>,
    /// and a caller that reads it unasked does not build.
    /// </summary>
    [Fact]
    public void The_output_of_a_run_ended_at_its_limit_is_no_output_and_what_it_printed_is_kept_apart()
    {
        var stopped = new ProcessResult(-1, "part", "complaint", TimedOut: true, Finished: false);
        Assert.Null(stopped.Output);
        Assert.Equal("part", stopped.Printed);
        Assert.Equal("complaint", stopped.Error);
        Assert.NotEqual(0, stopped.ExitCode);

        // Finished, with its output held open by a program it started: the output is whole.
        var held = new ProcessResult(0, "all", string.Empty, TimedOut: true, Finished: true);
        Assert.Equal("all", held.Output);
        Assert.Equal("all", held.Printed);
        Assert.Equal("all", new ProcessResult(0, "all", string.Empty, TimedOut: false, Finished: true).Output);
    }

    /// <summary>The files that take what a run printed whatever became of it, each for its reason.</summary>
    private static readonly string[] MayTakeThePart =
    [
        // git's own result carries the stop on (GitResult.TimedOut, never Ok), and the window shows the part as git's words.
        Path.Combine("src", "AxGit.Core", "GitRunner.cs"),
        // An action's output is kept for the user to read, said as what the program wrote until its limit.
        Path.Combine("src", "AxTask", "TaskForm.Actions.cs"),
        Path.Combine("src", "AxKit.Core", "ProcessRun.cs"),
    ];

    /// <summary>
    /// The one way around the compiler's hold is <see cref="ProcessResult.Printed"/>, the part of a stopped run:
    /// it is taken in the places listed with their reasons and nowhere else under <c>src</c> and <c>tools</c>.
    /// </summary>
    [Fact]
    public void What_a_stopped_run_printed_is_taken_only_where_that_is_meant()
    {
        var root = SourceTexts.RepositoryRoot();
        var separator = Path.DirectorySeparatorChar;
        var offenders = new List<string>();
        foreach (var folder in new[] { "src", "tools" })
        {
            foreach (var file in Directory.GetFiles(Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file);
                if (relative.Contains($"{separator}obj{separator}", StringComparison.Ordinal) || relative.Contains($"{separator}bin{separator}", StringComparison.Ordinal)
                    || MayTakeThePart.Contains(relative))
                {
                    continue;
                }

                if (File.ReadAllText(file).Contains(".Printed", StringComparison.Ordinal))
                {
                    offenders.Add(relative);
                }
            }
        }

        Assert.True(offenders.Count == 0, "What a run printed taken without asking whether it was stopped (ProcessResult.Printed); read Output after asking Stopped, or list the file here with its reason: " + string.Join(", ", offenders));
    }
}
