using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AxAutomate.Core;

/// <summary>
/// What Windows' voice says while an NVDA pass runs (docs/axKit/automation.md section 3, step 4; the user's ask of
/// 2026-11-18): which test runs and of how many, how many come after it, and about how long the rest takes. Until then
/// the voice repeated one sentence for the whole pass, two hours for every app, and nobody at the computer could tell
/// how long the keyboard stayed away. Pure: the runner (tools/AxAutomate, Program.cs, HandsOff) records these words
/// anew for every script and plays them in a loop, and keeps how long each script took in a file of the temporary
/// folder (<see cref="TimesPath"/>). The next pass then knows the length of what is still to come, where an average
/// of the scripts so far is off by half: the scripts that talk to Claude Code take minutes, the others seconds.
/// </summary>
public static class PassProgress
{
    /// <summary>The sentence every form of the warning holds.</summary>
    public const string HandsOff = "Please do not touch the keyboard or mouse.";

    /// <summary>Before the first script, while the run gets ready.</summary>
    public const string Starting = "Running NVDA tests. " + HandsOff;

    /// <summary>The longest time kept for one script, in seconds: what a file holds beyond it is not a script's time.</summary>
    private const double LongestKept = 3600;

    /// <summary>The file that keeps how long each script took the last time it passed with NVDA listening: one JSON object, a script's name as the run prints it and its seconds.</summary>
    public static string TimesPath => Path.Combine(Path.GetTempPath(), "axkit-nvda-times.json");

    /// <summary>
    /// The warning for the script that starts now: its number of <paramref name="count"/>, the sentence that keeps
    /// the user off the keys, how many scripts come after this one, and about how long this one and those take, when
    /// that is known (<see cref="SecondsLeft"/>).
    /// </summary>
    public static string Words(int number, int count, double? secondsLeft)
    {
        var after = count - number;
        var remaining = after <= 0 ? "This is the last test."
            : after == 1 ? "1 test remaining."
            : string.Create(CultureInfo.InvariantCulture, $"{after} tests remaining.");
        var running = string.Create(CultureInfo.InvariantCulture, $"Running NVDA test {number} of {count}.");
        return secondsLeft is { } seconds ? $"{running} {HandsOff} {remaining} {Left(seconds)}" : $"{running} {HandsOff} {remaining}";
    }

    /// <summary>
    /// A length of time as the voice says it, an estimate in the words of one: to the minute under an hour, to five
    /// minutes from an hour on, "Less than a minute left." under 45 seconds.
    /// </summary>
    public static string Left(double seconds)
    {
        if (seconds < 45)
        {
            return "Less than a minute left.";
        }

        var minutes = (int)Math.Round(seconds / 60, MidpointRounding.AwayFromZero);
        if (minutes < 60)
        {
            return minutes == 1 ? "About 1 minute left." : string.Create(CultureInfo.InvariantCulture, $"About {minutes} minutes left.");
        }

        var fives = (int)Math.Round(minutes / 5.0, MidpointRounding.AwayFromZero) * 5;
        var hours = fives / 60;
        var rest = fives % 60;
        var hourWords = hours == 1 ? "1 hour" : string.Create(CultureInfo.InvariantCulture, $"{hours} hours");
        return rest == 0 ? $"About {hourWords} left." : string.Create(CultureInfo.InvariantCulture, $"About {hourWords} {rest} minutes left.");
    }

    /// <summary>
    /// About how many seconds the scripts still to come take, the one that starts now among them
    /// (<paramref name="coming"/>, by the names the times are kept under). For each: what it took the last time
    /// (<paramref name="remembered"/>); else the average of the scripts this run has finished; else the average of
    /// what is remembered. Null when nothing at all is known, which is the first script of the first pass on a
    /// computer: the voice then says no time.
    /// </summary>
    public static double? SecondsLeft(IReadOnlyList<string> coming, IReadOnlyDictionary<string, double> remembered, int finished, double finishedSeconds)
    {
        double? usual = finished > 0 ? finishedSeconds / finished
            : remembered.Count > 0 ? remembered.Values.Average()
            : null;
        var total = 0.0;
        foreach (var script in coming)
        {
            if (remembered.TryGetValue(script, out var took))
            {
                total += took;
            }
            else if (usual is { } average)
            {
                total += average;
            }
            else
            {
                return null;
            }
        }

        return total;
    }

    /// <summary>
    /// The kept times read: each script's name and its seconds, names found without regard to case. A text that is
    /// not the file's (empty, cut off, of another shape) gives none, and a value that is not a time is passed over:
    /// the file is a convenience, and a pass runs the same without it.
    /// </summary>
    public static Dictionary<string, double> Read(string? json)
    {
        var times = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return times;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return times;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDouble(out var seconds)
                    && seconds > 0 && seconds <= LongestKept)
                {
                    times[property.Name] = seconds;
                }
            }
        }
        catch (JsonException)
        {
            times.Clear();
        }

        return times;
    }

    /// <summary>The times as the file holds them: one object, the scripts in the order of their names, each with its seconds to a tenth.</summary>
    public static string Write(IReadOnlyDictionary<string, double> times)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            foreach (var (script, seconds) in times.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                writer.WriteNumber(script, Math.Round(seconds, 1));
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
