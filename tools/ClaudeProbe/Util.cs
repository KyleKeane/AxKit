using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Probe;

/// <summary>The one clock of a run: milliseconds since the probe started, from one Stopwatch, and the wall clock beside it.</summary>
internal static class Clock
{
    private static readonly Stopwatch Watch = Stopwatch.StartNew();

    /// <summary>The wall clock when the Stopwatch started: what maps another process's wall time onto the probe's clock.</summary>
    public static readonly DateTime StartUtc = DateTime.UtcNow;

    public static double Ms => Math.Round(Watch.Elapsed.TotalMilliseconds, 1);

    public static string Wall => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

    /// <summary>A wall time of another process (UTC ticks) on the probe's clock.</summary>
    public static double FromTicks(long utcTicks) => Math.Round((utcTicks - StartUtc.Ticks) / 10000.0, 1);

    public static void Touch() => _ = Ms;
}

internal static class Json
{
    public static readonly JsonSerializerOptions Line = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static readonly JsonSerializerOptions Pretty = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true };
    public static readonly JsonDocumentOptions Lenient = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    public static string? Str(JsonObject? o, string name) => o?[name] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    public static int Int(JsonObject? o, string name, int fallback) =>
        o?[name] is JsonValue value && value.TryGetValue<double>(out var number) ? (int)number : fallback;

    public static double? Num(JsonObject? o, string name) =>
        o?[name] is JsonValue value && value.TryGetValue<double>(out var number) ? number : null;

    public static bool Bool(JsonObject? o, string name, bool fallback) =>
        o?[name] is JsonValue value && value.TryGetValue<bool>(out var flag) ? flag : fallback;

    /// <summary>A line of a .jsonl file as an object, or null when it is no JSON object.</summary>
    public static JsonObject? TryObject(string text)
    {
        try
        {
            return JsonNode.Parse(text) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static IEnumerable<JsonObject> ReadLines(string path)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0 && TryObject(line) is { } record)
            {
                yield return record;
            }
        }
    }
}

/// <summary>A .jsonl log: one record per line, flushed at once, safe from several threads.</summary>
internal sealed class Jsonl : IDisposable
{
    private readonly object _gate = new();
    private readonly StreamWriter _writer;
    private bool _disposed;

    public Jsonl(string path)
    {
        _writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete), new UTF8Encoding(false)) { AutoFlush = true };
    }

    public long Count { get; private set; }

    public void Write(JsonObject record)
    {
        var text = record.ToJsonString(Json.Line);
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _writer.WriteLine(text);
            Count++;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (!_disposed)
            {
                _disposed = true;
                _writer.Dispose();
            }
        }
    }
}

/// <summary>
/// Everything the probe prints goes through here: the user's profile folder becomes %USERPROFILE% in every spelling
/// (plain, with doubled backslashes as in JSON, with forward slashes, as Git Bash writes it, and as Claude Code names a
/// project folder, every other character a hyphen), e-mail addresses become &lt;email&gt;, the account name &lt;name&gt;.
/// The files of a run are not changed: they hold what was measured.
/// </summary>
internal static class Redactor
{
    private static readonly List<(Regex Pattern, string With)> Rules = Build();

    public static string Redact(string text)
    {
        foreach (var (pattern, with) in Rules)
        {
            text = pattern.Replace(text, with);
        }

        return text;
    }

    public static void Out(string text) => Console.Out.WriteLine(Redact(text));

    public static void Err(string text) => Console.Error.WriteLine(Redact(text));

    private static List<(Regex, string)> Build()
    {
        var rules = new List<(Regex, string)>();
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('\\');
        if (profile.Length > 3)
        {
            var variants = new List<string>
            {
                profile.Replace("\\", "\\\\"),
                profile,
                profile.Replace('\\', '/'),
                Regex.Replace(profile, "[^A-Za-z0-9]", "-"),
            };
            if (profile.Length > 2 && profile[1] == ':')
            {
                variants.Add("/" + char.ToLowerInvariant(profile[0]) + profile[2..].Replace('\\', '/'));
            }

            foreach (var variant in variants)
            {
                rules.Add((new Regex(Regex.Escape(variant), RegexOptions.IgnoreCase), "%USERPROFILE%"));
            }
        }

        rules.Add((new Regex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.NonBacktracking), "<email>"));
        var name = Environment.UserName;
        if (name.Length >= 3 && !name.Equals("user", StringComparison.OrdinalIgnoreCase) && !name.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            rules.Add((new Regex($@"\b{Regex.Escape(name)}\b", RegexOptions.IgnoreCase), "<name>"));
        }

        return rules;
    }
}

/// <summary>The system timer at 1 ms for the run, so that a 10 ms poll is 10 ms and not 15.6.</summary>
internal static class NativeTimer
{
    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint period);

    [DllImport("winmm.dll")]
    private static extern uint timeEndPeriod(uint period);

    public static void Begin() => timeBeginPeriod(1);

    public static void End() => timeEndPeriod(1);
}

internal static class Txt
{
    /// <summary>One line, control characters shown, cut to <paramref name="max"/> characters.</summary>
    public static string Clip(string? text, int max)
    {
        if (text is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(Math.Min(text.Length, max + 1));
        foreach (var c in text)
        {
            if (sb.Length >= max)
            {
                sb.Append('…');
                break;
            }

            sb.Append(c switch
            {
                '\n' => '⏎',
                '\r' => ' ',
                '\t' => ' ',
                < ' ' => '·',
                _ => c,
            });
        }

        return sb.ToString();
    }

    public static string Hex(string text) => Convert.ToHexString(Encoding.UTF8.GetBytes(text)).ToLowerInvariant();
}
