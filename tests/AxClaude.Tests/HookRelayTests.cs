using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using AxClaude.Core.Hooks;

namespace AxClaude.Tests;

/// <summary>The verb <c>AxKit.exe hook</c>: standard input to the window's pipe as one frame, and every failure swallowed (docs/axClaude/events-and-grounding.md EG-2.6).</summary>
public class HookRelayTests
{
    private const string Body = "{\"event\":\"invented\",\"text\":\"an invented reply\"}";

    private static readonly string Secret = HookFrame.NewSecret();

    private static string Variable(string pipeName) => HookFrame.VariableValue(pipeName, Secret);

    private static MemoryStream Input(string text) => new(Encoding.UTF8.GetBytes(text));

    /// <summary>A connect that records what it was asked and hands out a stream in memory.</summary>
    private sealed class Recorder
    {
        public List<(string Name, int TimeoutMs)> Calls { get; } = [];

        public MemoryStream Sent { get; } = new();

        public Stream? Connect(string name, int timeoutMs)
        {
            Calls.Add((name, timeoutMs));
            return Sent;
        }
    }

    private static (byte[]? Body, HookFrameFault Fault) ReadFrame(Stream pipe)
    {
        var body = HookFrame.Read(pipe, Secret, out var fault);
        return (body, fault);
    }

    [Fact]
    public void Standard_input_is_relayed_as_one_frame()
    {
        var name = HookFrame.NewPipeName("S-1-5-21-1");
        foreach (var lineEnd in new[] { string.Empty, "\n", "\r\n" })
        {
            var recorder = new Recorder();
            using var input = Input(Body + lineEnd);
            Assert.True(HookRelay.Relay(input, Variable(name), recorder.Connect));
            Assert.Equal((name, HookRelay.ConnectMs), Assert.Single(recorder.Calls));

            // One frame, the body without its last line end, and nothing after it.
            var sent = new MemoryStream(recorder.Sent.ToArray());
            var (body, fault) = ReadFrame(sent);
            Assert.Equal(HookFrameFault.None, fault);
            Assert.Equal(Body, Encoding.UTF8.GetString(body!));
            Assert.Equal(sent.Length, sent.Position);
        }
    }

    [Fact]
    public void Without_the_variable_nothing_is_connected_and_nothing_thrown()
    {
        foreach (var value in new[] { null, string.Empty, "AxKit-hook-x", "AxKit-hook-x;not-a-secret" })
        {
            var recorder = new Recorder();
            using var input = Input(Body);
            Assert.False(HookRelay.Relay(input, value, recorder.Connect));
            Assert.Empty(recorder.Calls);
            // Standard input is still read to its end, so that the writer is never cut off.
            Assert.Equal(input.Length, input.Position);
        }

        // Nor with an empty event.
        var empty = new Recorder();
        Assert.False(HookRelay.Relay(Input("\r\n"), Variable(HookFrame.NewPipeName("S-1-5-21-1")), empty.Connect));
        Assert.Empty(empty.Calls);
    }

    [Fact]
    public void A_connection_that_fails_is_swallowed()
    {
        var name = HookFrame.NewPipeName("S-1-5-21-1");
        Func<string, int, Stream?>[] failing =
        [
            (_, _) => throw new TimeoutException("No pipe."),
            (_, _) => throw new IOException("The pipe is busy."),
            (_, _) => throw new UnauthorizedAccessException("Access is denied."),
            (_, _) => null,
        ];
        foreach (var connect in failing)
        {
            Assert.False(HookRelay.Relay(Input(Body), Variable(name), connect));
        }

        // A pipe that is not there: the real connect gives up after its time, and nothing is thrown.
        var watch = Stopwatch.StartNew();
        Assert.False(HookRelay.Relay(Input(Body), Variable(name), HookRelay.Connect));
        Assert.True(watch.ElapsedMilliseconds < HookRelay.WatchdogMs, $"took {watch.ElapsedMilliseconds} ms");

        // A pipe that breaks while the frame is written.
        Assert.False(HookRelay.Relay(Input(Body), Variable(name), (_, _) => new MemoryStream(new byte[4], writable: false)));
    }

    [Fact]
    public void Input_over_the_limit_is_not_sent()
    {
        var name = HookFrame.NewPipeName("S-1-5-21-1");
        var recorder = new Recorder();
        using var over = new MemoryStream(Enumerable.Repeat((byte)'x', HookFrame.MaxBody + 1).ToArray());
        Assert.False(HookRelay.Relay(over, Variable(name), recorder.Connect));
        Assert.Empty(recorder.Calls);
        Assert.Equal(over.Length, over.Position);

        // Far over it: read to its end and dropped.
        using var far = new MemoryStream(Enumerable.Repeat((byte)'x', 3 * HookFrame.MaxBody).ToArray());
        Assert.False(HookRelay.Relay(far, Variable(name), recorder.Connect));
        Assert.Empty(recorder.Calls);
        Assert.Equal(far.Length, far.Position);

        // The limit itself, with its line end, is sent.
        using var limit = new MemoryStream(Enumerable.Repeat((byte)'x', HookFrame.MaxBody).Concat("\r\n"u8.ToArray()).ToArray());
        Assert.True(HookRelay.Relay(limit, Variable(name), recorder.Connect));
        var (body, fault) = ReadFrame(new MemoryStream(recorder.Sent.ToArray()));
        Assert.Equal(HookFrameFault.None, fault);
        Assert.Equal(HookFrame.MaxBody, body!.Length);
    }

    [Fact]
    public async Task A_real_pipe_carries_an_event_end_to_end()
    {
        var name = HookFrame.NewPipeName("S-1-5-21-1");
        using var server = new NamedPipeServerStream(name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var received = Task.Run(async () =>
        {
            await server.WaitForConnectionAsync(timeout.Token);
            return ReadFrame(server);
        });

        Assert.True(HookRelay.Relay(Input(Body + "\n"), Variable(name), HookRelay.Connect));
        var (body, fault) = await received.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(HookFrameFault.None, fault);
        Assert.Equal(Body, Encoding.UTF8.GetString(body!));
    }
}
