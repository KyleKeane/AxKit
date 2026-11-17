using System.Collections.Concurrent;
using AxClaude.Core.Streaming;
using AxKit.Core;

namespace AxClaude.Tests;

/// <summary>
/// The host of a program of lines (docs/axClaude/stream-mode.md SM-4.1) against programs every Windows has: what it
/// reads, what it writes, the end it reports, what it keeps of the errors. Claude Code itself is the probe's and the
/// live scripts' to start; here the pipes and the threads are what is tested.
/// </summary>
public sealed class StreamHostTests
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(20);

    /// <summary>Windows' own command interpreter with one command: quiet, no start-up script, the command as given.</summary>
    private static StreamHostOptions Cmd(string command) =>
        new(new ProgramLine(SystemPrograms.Cmd, "/d /q /c \"" + command + "\"", null), Environment.SystemDirectory, Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string?)entry.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase));

    /// <summary>A host started with what it reports gathered: its lines in order, and its end.</summary>
    private static StreamHost Start(StreamHostOptions options, out BlockingCollection<string> lines, out Ending ended)
    {
        var read = new BlockingCollection<string>();
        var end = new Ending();
        lines = read;
        ended = end;
        return StreamHost.Start(options, read.Add, end.Report);
    }

    /// <summary>The end a host reports: whether it came, and with which code.</summary>
    private sealed class Ending
    {
        private readonly ManualResetEventSlim _came = new();

        public int Code { get; private set; } = int.MinValue;

        public int Reports { get; private set; }

        public void Report(int code)
        {
            Code = code;
            Reports++;
            _came.Set();
        }

        public bool Wait(TimeSpan limit) => _came.Wait(limit);
    }

    [Fact]
    public void Every_line_of_the_output_comes_in_order_and_then_the_end_with_its_code()
    {
        using var host = Start(Cmd("echo one& echo two& echo three& exit 7"), out var lines, out var ended);

        Assert.True(ended.Wait(Limit), "the end was not reported");
        Assert.Equal(7, ended.Code);
        Assert.Equal(["one", "two", "three"], lines.ToArray());
        Assert.True(host.HasExited);
        Assert.True(host.ProcessId > 0);
    }

    [Fact]
    public void Lines_written_reach_the_program_in_order_and_closing_the_input_ends_it()
    {
        // findstr with a pattern every line matches writes back what it reads, and ends when its input ends.
        using var host = Start(Cmd("findstr \"^\""), out var lines, out var ended);

        host.Write("{\"first\":1}");
        host.Write("zweite Zeile, mit ä und €");
        host.CloseInput();

        Assert.True(ended.Wait(Limit), "the program did not end when its input was closed");
        Assert.Equal(0, ended.Code);
        Assert.Equal(["{\"first\":1}", "zweite Zeile, mit ä und €"], lines.ToArray());

        // After the end nothing is written and nothing is thrown.
        host.Write("too late");
        host.CloseInput();
    }

    [Fact]
    public void What_the_program_writes_as_errors_is_kept_for_the_user_and_is_no_line_of_the_output()
    {
        using var host = Start(Cmd("echo to the errors 1>&2& echo to the output& exit 3"), out var lines, out var ended);

        Assert.True(ended.Wait(Limit));
        Assert.Equal(3, ended.Code);
        Assert.Equal(["to the output"], lines.ToArray());
        // Whole when the end is reported: the errors are read to their end first.
        Assert.Contains("to the errors", host.ErrorTail);
        Assert.True(host.ErrorCharacters >= "to the errors".Length);
    }

    [Fact]
    public void A_program_that_runs_on_is_ended_with_what_it_started_and_its_end_is_still_reported()
    {
        // A second interpreter under the first, waiting on its input for good.
        using var host = Start(Cmd(SystemPrograms.Cmd + " /d /q /c findstr \"^\""), out _, out var ended);
        Assert.False(ended.Wait(TimeSpan.FromMilliseconds(400)));

        host.Kill();

        Assert.True(ended.Wait(Limit), "the end was not reported after the program was ended");
        Assert.True(host.HasExited);
    }

    [Fact]
    public void Disposing_ends_a_program_that_still_runs()
    {
        var host = Start(Cmd("findstr \"^\""), out _, out var ended);
        host.Dispose();
        host.Dispose();

        Assert.True(ended.Wait(Limit));
    }

    [Fact]
    public void A_line_that_was_refused_and_a_program_that_is_not_there_are_not_started()
    {
        var refused = new StreamHostOptions(new ProgramLine(string.Empty, string.Empty, "It is not started."), Environment.SystemDirectory, new Dictionary<string, string>());
        var error = Assert.Throws<ArgumentException>(() => StreamHost.Start(refused, _ => { }, _ => { }));
        Assert.StartsWith("It is not started.", error.Message);

        var missing = new StreamHostOptions(new ProgramLine(Path.Combine(Environment.SystemDirectory, "no-such-program-of-the-tests.exe"), string.Empty, null), Environment.SystemDirectory, new Dictionary<string, string>());
        Assert.Throws<System.ComponentModel.Win32Exception>(() => StreamHost.Start(missing, _ => { }, _ => { }));
    }

    [Fact]
    public void The_program_gets_the_environment_it_is_given_and_no_other()
    {
        var options = Cmd("echo [%AXKIT_TEST_GIVEN%][%AXKIT_TEST_NOT_GIVEN%]");
        var environment = new Dictionary<string, string>(options.Environment, StringComparer.OrdinalIgnoreCase) { ["AXKIT_TEST_GIVEN"] = "yes" };
        environment.Remove("AXKIT_TEST_NOT_GIVEN");
        using var host = Start(options with { Environment = environment }, out var lines, out var ended);

        Assert.True(ended.Wait(Limit));
        // A variable that is not set stays as it was written.
        Assert.Equal(["[yes][%AXKIT_TEST_NOT_GIVEN%]"], lines.ToArray());
    }
}
