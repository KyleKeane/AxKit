using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using AxClaude.Core.Hooks;
using AxClaude.Core.Streaming;

namespace AxClaude.Tests;

/// <summary>
/// The relay of a Claude Code's questions (docs/axClaude/stream-mode.md SM-7.3), with stand-ins on both of its
/// sides: Claude Code's requests as lines, and the window as a function or as the other end of a pipe. What it must
/// hold: a question goes to the window as it came and the answer back as it came; anything that is no answer, and
/// every way the window cannot be asked, is answered with no.
/// </summary>
public sealed class AskRelayTests
{
    private const string Question = """{"tool_name":"Write","input":{"file_path":"a.txt","content":"hi"},"tool_use_id":"toolu_1"}""";

    private static readonly string Secret = new('a', HookFrame.SecretLength);

    /// <summary>What the relay writes for the requests given, one line a request, with the window as a function.</summary>
    private static List<JsonElement> Served(Func<byte[], byte[]?> ask, params string[] requests)
    {
        var output = new StringWriter { NewLine = "\n" };
        AskRelay.Serve(new StringReader(string.Join("\n", requests)), output, ask);
        return output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToList();
    }

    private static string Call(int id, string arguments) =>
        "{\"jsonrpc\":\"2.0\",\"id\":" + id + ",\"method\":\"tools/call\",\"params\":{\"name\":\"approve\",\"arguments\":" + arguments + "}}";

    /// <summary>The text of a call's answer: what the tool gave back.</summary>
    private static string Text(JsonElement answer) => answer.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()!;

    [Fact]
    public void It_says_what_it_is_and_names_its_one_tool()
    {
        var answers = Served(
            _ => null,
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{}}}""",
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",
            """{"jsonrpc":"2.0","id":3,"method":"resources/list"}""",
            "not a request at all");

        // One answer for each request with an id: the notification and the line that is no request get none.
        Assert.Equal([1, 2, 3], answers.Select(answer => answer.GetProperty("id").GetInt32()));
        Assert.Equal("2025-06-18", answers[0].GetProperty("result").GetProperty("protocolVersion").GetString());
        Assert.Equal(AskRelay.Server, answers[0].GetProperty("result").GetProperty("serverInfo").GetProperty("name").GetString());
        var tool = Assert.Single(answers[1].GetProperty("result").GetProperty("tools").EnumerateArray());
        Assert.Equal(AskRelay.Tool, tool.GetProperty("name").GetString());
        Assert.Equal("mcp__axask__approve", AskRelay.ToolName);
        Assert.Equal(JsonValueKind.Object, answers[2].GetProperty("result").ValueKind);
    }

    [Fact]
    public void A_question_goes_to_the_window_as_it_came_and_the_answer_back_as_it_came()
    {
        byte[]? asked = null;
        var allow = ClaudeCodeStream.AllowAnswer(JsonDocument.Parse("""{"file_path":"a.txt","content":"hi"}""").RootElement);
        var answers = Served(
            question =>
            {
                asked = question;
                return Encoding.UTF8.GetBytes(allow);
            },
            Call(7, Question));

        Assert.Equal(Question, Encoding.UTF8.GetString(asked!));
        Assert.Equal(7, answers[0].GetProperty("id").GetInt32());
        Assert.Equal(allow, Text(answers[0]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("yes")]
    [InlineData("{\"behavior\":\"maybe\"}")]
    [InlineData("{\"allowed\":true}")]
    [InlineData("[\"allow\"]")]
    public void What_is_no_answer_is_answered_with_no(string? fromTheWindow)
    {
        var answers = Served(_ => fromTheWindow is null ? null : Encoding.UTF8.GetBytes(fromTheWindow), Call(1, Question));

        using var answer = JsonDocument.Parse(Text(answers[0]));
        Assert.Equal("deny", answer.RootElement.GetProperty("behavior").GetString());
        Assert.Equal(AskRelay.NoWindowWords, answer.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void A_window_that_throws_and_a_call_without_a_question_are_answered_with_no()
    {
        var thrown = Served(_ => throw new IOException("the pipe broke"), Call(1, Question));
        Assert.Contains("\"deny\"", Text(thrown[0]));

        var asked = false;
        var without = Served(
            _ =>
            {
                asked = true;
                return null;
            },
            """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"approve"}}""");
        Assert.Contains("\"deny\"", Text(without[0]));
        Assert.False(asked);
    }

    [Theory]
    [InlineData("{\"behavior\":\"allow\",\"updatedInput\":{}}", true)]
    [InlineData("{\"behavior\":\"deny\",\"message\":\"no\"}", true)]
    [InlineData("{\"behavior\":\"ask\"}", false)]
    [InlineData("{\"behavior\":true}", false)]
    [InlineData("{}", false)]
    [InlineData("allow", false)]
    public void An_answer_is_an_object_whose_behaviour_is_allow_or_deny(string text, bool isAnswer)
    {
        Assert.Equal(isAnswer, AskRelay.IsAnswer(text));
    }

    [Fact]
    public void The_question_is_one_frame_with_the_secret_and_the_answers_frame_is_read()
    {
        var answer = Encoding.UTF8.GetBytes("""{"behavior":"deny","message":"no"}""");
        var fromWindow = new MemoryStream();
        HookFrame.Write(fromWindow, Secret, answer);
        fromWindow.Position = 0;
        var pipe = new TwoWay(fromWindow);
        string? connectedTo = null;

        var back = AskRelay.Ask(
            Encoding.UTF8.GetBytes(Question),
            AskRelay.VariableValue(AskRelay.PipePrefix + "S-1-5-21-0f", Secret),
            (name, _) =>
            {
                connectedTo = name;
                return pipe;
            });

        Assert.Equal(answer, back);
        Assert.Equal(AskRelay.PipePrefix + "S-1-5-21-0f", connectedTo);
        Assert.Equal(Secret + "\n" + Encoding.UTF8.GetByteCount(Question) + "\n" + Question, Encoding.UTF8.GetString(pipe.Written.ToArray()));
    }

    [Fact]
    public void A_window_that_cannot_be_asked_gives_nothing_and_never_an_exception()
    {
        var question = Encoding.UTF8.GetBytes(Question);
        var connected = false;
        Stream? Never(string name, int ms)
        {
            connected = true;
            return null;
        }

        // No variable, a variable of another pipe (the hooks'), a path in the name: nothing is connected to.
        Assert.Null(AskRelay.Ask(question, null, Never));
        Assert.Null(AskRelay.Ask(question, HookFrame.VariableValue(HookFrame.PipePrefix + "S-1-5-21-0f", Secret), Never));
        Assert.Null(AskRelay.Ask(question, AskRelay.VariableValue(AskRelay.PipePrefix + @"..\other", Secret), Never));
        Assert.Null(AskRelay.Ask(question, AskRelay.VariableValue(AskRelay.PipePrefix + "S-1", "short"), Never));
        Assert.False(connected);

        var variable = AskRelay.VariableValue(AskRelay.PipePrefix + "S-1-5-21-0f", Secret);
        // The window is not there, the connection throws, the answer carries another secret, the pipe ends early.
        Assert.Null(AskRelay.Ask(question, variable, (_, _) => null));
        Assert.Null(AskRelay.Ask(question, variable, (_, _) => throw new TimeoutException()));
        var other = new MemoryStream();
        HookFrame.Write(other, new string('b', HookFrame.SecretLength), "{}"u8);
        other.Position = 0;
        Assert.Null(AskRelay.Ask(question, variable, (_, _) => new TwoWay(other)));
        Assert.Null(AskRelay.Ask(question, variable, (_, _) => new TwoWay(new MemoryStream())));
    }

    [Fact]
    public void Over_a_real_pipe_the_window_reads_the_question_and_its_answer_comes_back()
    {
        var name = AskRelay.NewPipeName("S-1-5-21-tests");
        var secret = HookFrame.NewSecret();
        var answer = Encoding.UTF8.GetBytes("""{"behavior":"allow","updatedInput":{"file_path":"a.txt","content":"hi"}}""");
        byte[]? read = null;
        using var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly);
        var window = new Thread(() =>
        {
            server.WaitForConnection();
            read = HookFrame.Read(server, secret, out _);
            HookFrame.Write(server, secret, answer);
            server.WaitForPipeDrain();
        })
        {
            IsBackground = true,
        };
        window.Start();

        var back = AskRelay.Ask(Encoding.UTF8.GetBytes(Question), AskRelay.VariableValue(name, secret), AskRelay.Connect);

        Assert.True(window.Join(TimeSpan.FromSeconds(10)));
        Assert.Equal(Question, Encoding.UTF8.GetString(read!));
        Assert.Equal(answer, back);
    }

    [Fact]
    public void The_configuration_names_the_program_and_the_verb_and_nothing_of_the_pipe()
    {
        var text = AskRelay.ConfigText(@"C:\Program Files\AxKit\AxKit.exe");

        using var document = JsonDocument.Parse(text);
        var server = document.RootElement.GetProperty("mcpServers").GetProperty(AskRelay.Server);
        Assert.Equal(@"C:\Program Files\AxKit\AxKit.exe", server.GetProperty("command").GetString());
        Assert.Equal([AskRelay.Verb], server.GetProperty("args").EnumerateArray().Select(argument => argument.GetString()));
        // The pipe and its secret travel in the environment alone: never in a file.
        Assert.False(server.TryGetProperty("env", out _));
        Assert.DoesNotContain(AskRelay.PipePrefix, text);
    }

    [Fact]
    public void The_configuration_is_written_when_it_is_missing_or_names_another_program_and_left_when_it_is_right()
    {
        var folder = Path.Combine(Path.GetTempPath(), "axclaude-tests", Guid.NewGuid().ToString("N"));
        var file = Path.Combine(folder, "AxKit", "claude-ask.json");
        try
        {
            // Missing, with its folder: written.
            Assert.True(AskRelay.WriteConfig(file, @"C:\one\AxKit.exe"));
            Assert.Equal(AskRelay.ConfigText(@"C:\one\AxKit.exe"), File.ReadAllText(file));
            // UTF-8 without a mark: Claude Code reads the file as JSON from its first byte.
            Assert.Equal((byte)'{', File.ReadAllBytes(file)[0]);

            // Right as it is: not written again.
            var written = File.GetLastWriteTimeUtc(file);
            Assert.False(AskRelay.WriteConfig(file, @"C:\one\AxKit.exe"));
            Assert.Equal(written, File.GetLastWriteTimeUtc(file));

            // Another program file (another build, a copy that moved): written for this one.
            Assert.True(AskRelay.WriteConfig(file, @"D:\two\AxKit.exe"));
            Assert.Equal(AskRelay.ConfigText(@"D:\two\AxKit.exe"), File.ReadAllText(file));

            // Nothing is left beside it.
            Assert.Equal([file], Directory.GetFiles(Path.GetDirectoryName(file)!));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void A_name_of_the_questions_pipe_is_no_name_of_the_hooks_pipe()
    {
        var name = AskRelay.NewPipeName("S-1-5-21-0f");
        Assert.StartsWith(AskRelay.PipePrefix, name);
        Assert.True(HookFrame.TryParseVariable(AskRelay.VariableValue(name, Secret), AskRelay.PipePrefix, out var parsed, out var secret));
        Assert.Equal(name, parsed);
        Assert.Equal(Secret, secret);
        // Each relay takes its own kind of pipe alone.
        Assert.False(HookFrame.TryParseVariable(AskRelay.VariableValue(name, Secret), out _, out _));
    }

    /// <summary>A stream that is read from one side and written to another, as a pipe's two directions are.</summary>
    private sealed class TwoWay(Stream toRead) : Stream
    {
        public MemoryStream Written { get; } = new();

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => toRead.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => Written.Write(buffer, offset, count);
    }
}
