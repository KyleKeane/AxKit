using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AxClaude.Core.Hooks;

/// <summary>Why <see cref="HookFrame.Read"/> gave no body: the first line was not the secret, the body was over the limit, or the stream broke or ended early.</summary>
public enum HookFrameFault
{
    None,
    WrongSecret,
    TooLong,
    Broken,
}

/// <summary>
/// One event on the window's pipe (docs/axClaude/events-and-grounding.md EG-2.7, EG-2.8, section 6.2): the secret, the
/// body's length and the body, in one connection that carries nothing else. Neutral: it knows nothing of the program
/// whose events it carries, so that a second agent program would use it unchanged. The variable names the pipe and its
/// secret for the hook's process, which inherits it from the program axClaude started.
/// </summary>
public static class HookFrame
{
    /// <summary>The environment variable that carries the pipe's name and secret, <c>&lt;name&gt;;&lt;secret&gt;</c>.</summary>
    public const string Variable = "AXKIT_HOOK_PIPE";

    /// <summary>The largest body a frame carries (EG-2.6, EG-2.7); a longer one is never sent and never read.</summary>
    public const int MaxBody = 1_048_576;

    /// <summary>Every pipe of the hooks starts so, which <see cref="TryParseVariable"/> asks of a name.</summary>
    public const string PipePrefix = "AxKit-hook-";

    /// <summary>The secret's length: 32 random bytes as hex digits.</summary>
    public const int SecretLength = 64;

    // The longest length line: the digits of MaxBody. A line of more digits is a length over the limit.
    private static readonly int LengthDigits = MaxBody.ToString(CultureInfo.InvariantCulture).Length;

    /// <summary>How a line of the header ended.</summary>
    private enum LineEnd
    {
        Found,
        StreamEnded,
        TooLong,
    }

    /// <summary>A new pipe name for one start of the program: the prefix, the user's SID and 16 random hex digits (EG-2.7).</summary>
    public static string NewPipeName(string userSid) =>
        PipePrefix + userSid + "-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));

    /// <summary>A new secret for one start of the program: 64 random hex digits.</summary>
    public static string NewSecret() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(SecretLength / 2));

    /// <summary>The variable's value for the program's environment (EG-2.8, section 6.2).</summary>
    public static string VariableValue(string pipeName, string secret) => pipeName + ";" + secret;

    /// <summary>
    /// The pipe's name and secret from the variable's value, split at its first semicolon: the part before is the name,
    /// the rest the secret. False, with both empty, for a value that is missing, a name that is not one of the hooks'
    /// pipes (the prefix, then letters, digits and hyphens: never a path to another pipe) or a secret that is not
    /// <see cref="SecretLength"/> hex digits.
    /// </summary>
    public static bool TryParseVariable(string? value, out string pipeName, out string secret)
    {
        pipeName = string.Empty;
        secret = string.Empty;
        var split = value?.IndexOf(';') ?? -1;
        if (value is null || split < 0)
        {
            return false;
        }

        var name = value[..split];
        var rest = value[(split + 1)..];
        if (name.Length <= PipePrefix.Length || !name.StartsWith(PipePrefix, StringComparison.Ordinal) || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')
            || rest.Length != SecretLength || !rest.All(char.IsAsciiHexDigit))
        {
            return false;
        }

        pipeName = name;
        secret = rest;
        return true;
    }

    /// <summary>
    /// Writes one frame: the secret, a line end, the body's length in decimal, a line end, the body; in one write, so that
    /// the other end never sees half a header.
    /// </summary>
    public static void Write(Stream pipe, string secret, ReadOnlySpan<byte> body)
    {
        var header = Encoding.ASCII.GetBytes(secret + "\n" + body.Length.ToString(CultureInfo.InvariantCulture) + "\n");
        var frame = new byte[header.Length + body.Length];
        header.CopyTo(frame, 0);
        body.CopyTo(frame.AsSpan(header.Length));
        pipe.Write(frame, 0, frame.Length);
        pipe.Flush();
    }

    /// <summary>
    /// Reads one frame and returns its body, or null with the fault. The first line must be the secret, compared in
    /// constant time; nothing after it is read when it is not. A length over <see cref="MaxBody"/> is refused before the
    /// body is read. A stream that ends early, a length that is no number, or a pipe that breaks is
    /// <see cref="HookFrameFault.Broken"/>, never an exception.
    /// </summary>
    public static byte[]? Read(Stream pipe, string secret, out HookFrameFault fault)
    {
        try
        {
            var expected = Encoding.ASCII.GetBytes(secret);
            var first = ReadLine(pipe, expected.Length, out var end);
            if (end == LineEnd.StreamEnded)
            {
                fault = HookFrameFault.Broken;
                return null;
            }

            if (end == LineEnd.TooLong || first.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(first, expected))
            {
                fault = HookFrameFault.WrongSecret;
                return null;
            }

            var digits = ReadLine(pipe, LengthDigits, out end);
            if (digits.Length == 0 || !digits.All(b => char.IsAsciiDigit((char)b)) || end == LineEnd.StreamEnded)
            {
                fault = HookFrameFault.Broken;
                return null;
            }

            var length = end == LineEnd.TooLong ? int.MaxValue : int.Parse(Encoding.ASCII.GetString(digits), CultureInfo.InvariantCulture);
            if (length > MaxBody)
            {
                fault = HookFrameFault.TooLong;
                return null;
            }

            var body = new byte[length];
            if (pipe.ReadAtLeast(body, length, throwOnEndOfStream: false) < length)
            {
                fault = HookFrameFault.Broken;
                return null;
            }

            fault = HookFrameFault.None;
            return body;
        }
        catch (IOException)
        {
            fault = HookFrameFault.Broken;
            return null;
        }
    }

    /// <summary>
    /// A line of at most <paramref name="max"/> bytes without its line end, read a byte at a time so that nothing after
    /// it is taken from the stream; what was read when the stream ended or the line ran past the limit.
    /// </summary>
    private static byte[] ReadLine(Stream pipe, int max, out LineEnd end)
    {
        var line = new byte[max];
        var count = 0;
        while (true)
        {
            var next = pipe.ReadByte();
            if (next < 0)
            {
                end = LineEnd.StreamEnded;
                return line[..count];
            }

            if (next == '\n')
            {
                end = LineEnd.Found;
                return line[..count];
            }

            if (count == max)
            {
                end = LineEnd.TooLong;
                return line[..count];
            }

            line[count++] = (byte)next;
        }
    }
}
