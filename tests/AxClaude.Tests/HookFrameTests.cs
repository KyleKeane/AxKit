using System.Text;
using AxClaude.Core.Hooks;

namespace AxClaude.Tests;

/// <summary>The frame of one event on the window's pipe and the variable that names the pipe (docs/axClaude/events-and-grounding.md EG-2.7, EG-2.8, section 6.2).</summary>
public class HookFrameTests
{
    private const string Sid = "S-1-5-21-1111111111-2222222222-3333333333-1001";

    private static readonly string Secret = new string('a', 32) + new string('7', 32);

    /// <summary>A stream whose every read fails, as a pipe does when the other end breaks it.</summary>
    private sealed class BrokenPipe : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("The pipe has been ended.");

        public override int Read(Span<byte> buffer) => throw new IOException("The pipe has been ended.");

        public override int ReadByte() => throw new IOException("The pipe has been ended.");
    }

    private static MemoryStream Stream(string text) => new(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void A_frame_written_is_read_back()
    {
        var body = Encoding.UTF8.GetBytes("{\"event\":\"invented\",\"text\":\"caf\u00e9 \u2014 ok\"}");
        using var pipe = new MemoryStream();
        HookFrame.Write(pipe, Secret, body);
        Assert.Equal(Encoding.ASCII.GetBytes(Secret + "\n" + body.Length + "\n").Concat(body), pipe.ToArray());

        pipe.Position = 0;
        Assert.Equal(body, HookFrame.Read(pipe, Secret, out var fault));
        Assert.Equal(HookFrameFault.None, fault);

        // An empty body and one of the largest length both make the round trip.
        foreach (var size in new[] { 0, HookFrame.MaxBody })
        {
            using var other = new MemoryStream();
            var bytes = Enumerable.Repeat((byte)'x', size).ToArray();
            HookFrame.Write(other, Secret, bytes);
            other.Position = 0;
            Assert.Equal(bytes, HookFrame.Read(other, Secret, out fault));
            Assert.Equal(HookFrameFault.None, fault);
        }
    }

    [Fact]
    public void A_wrong_secret_is_refused_and_the_body_not_read()
    {
        var other = new string('b', 64);
        using var pipe = new MemoryStream();
        HookFrame.Write(pipe, other, Encoding.UTF8.GetBytes("{\"event\":\"invented\"}"));
        pipe.Position = 0;
        Assert.Null(HookFrame.Read(pipe, Secret, out var fault));
        Assert.Equal(HookFrameFault.WrongSecret, fault);
        // The secret's line and nothing after it.
        Assert.Equal(65, pipe.Position);

        // A first line shorter or longer than the secret, or the secret in other letters, is no secret either.
        foreach (var first in new[] { string.Empty, "short", Secret + "0", Secret.ToUpperInvariant(), Secret[..63] + "\r" })
        {
            using var wrong = Stream(first + "\n2\n{}");
            Assert.Null(HookFrame.Read(wrong, Secret, out fault));
            Assert.Equal(HookFrameFault.WrongSecret, fault);
            Assert.True(wrong.Position <= 66, $"read past the first line for '{first}'");
        }
    }

    [Fact]
    public void A_body_over_the_limit_is_refused()
    {
        foreach (var length in new[] { (HookFrame.MaxBody + 1).ToString(), "99999999", "123456789012345678901234567890" })
        {
            var header = Secret + "\n" + length + "\n";
            using var pipe = Stream(header + "{}");
            Assert.Null(HookFrame.Read(pipe, Secret, out var fault));
            Assert.Equal(HookFrameFault.TooLong, fault);
            // Refused on its length: not a byte of the body is read.
            Assert.True(pipe.Position <= header.Length, $"read into the body for a length of {length}");
        }
    }

    [Fact]
    public void A_stream_that_ends_early_is_broken_not_thrown()
    {
        foreach (var text in new[] { string.Empty, Secret[..10], Secret, Secret + "\n", Secret + "\n12", Secret + "\n5\nab", Secret + "\n\n{}", Secret + "\n-1\n", Secret + "\n1x\n{}" })
        {
            using var pipe = Stream(text);
            Assert.Null(HookFrame.Read(pipe, Secret, out var fault));
            Assert.Equal(HookFrameFault.Broken, fault);
        }

        using var broken = new BrokenPipe();
        Assert.Null(HookFrame.Read(broken, Secret, out var brokenFault));
        Assert.Equal(HookFrameFault.Broken, brokenFault);
    }

    [Fact]
    public void The_variables_value_is_split_at_its_first_semicolon()
    {
        var name = HookFrame.NewPipeName(Sid);
        var value = HookFrame.VariableValue(name, Secret);
        Assert.Equal(name + ";" + Secret, value);
        Assert.True(HookFrame.TryParseVariable(value, out var pipeName, out var secret));
        Assert.Equal(name, pipeName);
        Assert.Equal(Secret, secret);

        // Everything after the first semicolon is the secret, so a second one makes it no secret.
        Assert.False(HookFrame.TryParseVariable(value + ";" + Secret, out pipeName, out secret));
        Assert.Equal(string.Empty, pipeName);
        Assert.Equal(string.Empty, secret);
    }

    [Fact]
    public void A_missing_or_malformed_variable_is_refused()
    {
        var name = HookFrame.NewPipeName(Sid);
        string?[] values =
        [
            null,
            string.Empty,
            name,
            ";" + Secret,
            HookFrame.PipePrefix + ";" + Secret,
            name + ";",
            name + ";" + Secret[..63],
            name + ";" + Secret + "0",
            name + ";" + Secret[..63] + "g",
            name + "; " + Secret[..63],
            "Other-pipe;" + Secret,
            HookFrame.PipePrefix + @"..\x;" + Secret,
            @"\\.\pipe\" + name + ";" + Secret,
            HookFrame.PipePrefix + "a b;" + Secret,
        ];
        foreach (var value in values)
        {
            Assert.False(HookFrame.TryParseVariable(value, out var pipeName, out var secret), $"took '{value}'");
            Assert.Equal(string.Empty, pipeName);
            Assert.Equal(string.Empty, secret);
        }
    }

    [Fact]
    public void Pipe_names_and_secrets_differ_from_call_to_call()
    {
        var names = Enumerable.Range(0, 20).Select(_ => HookFrame.NewPipeName(Sid)).ToList();
        var secrets = Enumerable.Range(0, 20).Select(_ => HookFrame.NewSecret()).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
        Assert.Equal(secrets.Count, secrets.Distinct().Count());
        foreach (var name in names)
        {
            Assert.StartsWith("AxKit-hook-" + Sid + "-", name, StringComparison.Ordinal);
            var random = name[("AxKit-hook-" + Sid + "-").Length..];
            Assert.Equal(16, random.Length);
            Assert.True(random.All(char.IsAsciiHexDigitLower), name);
        }

        foreach (var secret in secrets)
        {
            Assert.Equal(64, secret.Length);
            Assert.True(secret.All(char.IsAsciiHexDigitLower), secret);
            Assert.True(HookFrame.TryParseVariable(HookFrame.VariableValue(names[0], secret), out _, out _));
        }
    }
}
