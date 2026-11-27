using System.Text.Json;
using AxClaude.Core.Streaming;

namespace AxClaude.Tests;

/// <summary>
/// A reply's Markdown made to read as Claude Code's screen shows it (docs/axClaude/stream-mode.md SM-4.5): the source
/// of the timeline fixture long-reply against the rows its recording shows for the same reply (read with
/// PtyCapture's dump), headings compared by their text, since the stream's model keeps their marks for the view.
/// </summary>
public class ClaudeCodeMarkdownTests
{
    /// <summary>The screen's rows of the reply, as recorded in tests/fixtures/timelines/long-reply/pty.vt, blank rows left out.</summary>
    private static readonly string[] Screen =
    [
        "Lighthouses at a Glance",
        "Core Purposes",
        "- Guiding ships safely past hazards",
        "- Marking harbor entrances and channels",
        "- Warning of reefs, sandbars, and wrecks",
        "1. Fresnel lenses concentrated lamp light into powerful beams.",
        "2. Each lighthouse used a unique flash pattern for identification.",
        "3. Automation replaced most lightkeepers in the twentieth century.",
        "The lighthouse is a reliable beacon, and its light is steady while it keeps watch with flash_pattern = \"group\".",
        "lights = {\"Pharos\": \"fire\", \"Fastnet\": \"electric\"}",
        "for name, source in lights.items():",
        "    print(f\"{name} is powered by {source}.\")",
        "Notable Examples",
        "Lighthouse: Pharos of Alexandria. Location: Egypt.",
        "Lighthouse: Eddystone Lighthouse. Location: England.",
        "Lighthouse: Fastnet Rock. Location: Ireland.",
        "For more on maritime navigation history, see https://example.com.",
        "A lighthouse does not chase the ships that pass it; it simply keeps shining, and sailors learn to steer by its steady promise through the dark.",
    ];

    private static string[] Source()
    {
        var path = Path.Combine(Fixtures.Folder(), "timelines", "long-reply", "entries.jsonl");
        foreach (var line in File.ReadLines(path))
        {
            using var document = JsonDocument.Parse(line);
            if (!document.RootElement.TryGetProperty("entry", out var entry) || entry.GetProperty("type").GetString() != "assistant"
                || !entry.TryGetProperty("message", out var message) || !message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.GetProperty("type").GetString() == "text" && part.GetProperty("text").GetString() is { } text && text.Contains("Lighthouses at a Glance"))
                {
                    return text.Replace("\r\n", "\n").Split('\n');
                }
            }
        }

        throw new InvalidOperationException("The long reply is not in the fixture.");
    }

    [Fact]
    public void The_long_reply_reads_as_the_screen_showed_it()
    {
        var plain = ClaudeCodeMarkdown.Plain(Source())
            .Where(line => line.Trim().Length > 0)
            .Select(line => line.StartsWith('#') ? line.TrimStart('#').TrimStart() : line)
            .Take(Screen.Length)
            .ToArray();
        Assert.Equal(Screen, plain);
    }

    [Theory]
    [InlineData("a **bold** and __strong__ word", "a bold and strong word")]
    [InlineData("an *italic* and _slanted_ word", "an italic and slanted word")]
    [InlineData("snake_case_name and 2*3*4 stay", "snake_case_name and 2*3*4 stay")]
    [InlineData("code `**not bold**` stays", "code **not bold** stays")]
    [InlineData("see [the guide](https://example.com/g)", "see the guide (https://example.com/g)")]
    [InlineData("see [https://example.com](https://example.com)", "see https://example.com")]
    [InlineData("## A **marked** heading", "## A marked heading")]
    public void Marks_inside_a_line_go(string source, string expected) =>
        Assert.Equal(expected, ClaudeCodeMarkdown.Inline(source));

    [Fact]
    public void A_fence_goes_and_the_code_under_it_stays_as_written()
    {
        Assert.Equal(["before", "x = **y**", "after"], ClaudeCodeMarkdown.Plain(["before", "```python", "x = **y**", "```", "after"]));
    }

    [Fact]
    public void A_table_cell_already_ended_gets_no_second_full_stop_and_the_table_ends_at_a_line_without_bars()
    {
        Assert.Equal(["Step: Run it. Result: Done!", "Text after."],
            ClaudeCodeMarkdown.Plain(["| Step | Result |", "|:---|---:|", "| Run it. | **Done!** |", "Text after."]));
    }

    /// <summary>
    /// A long line of bold marks that never close took time growing with the square of its length, on the window's
    /// thread (found by the review of 2026-11-27: this line took far longer than the test allows). A pattern that does
    /// not get through a line in its time leaves the line as it came, and the lines around it are still made plain.
    /// </summary>
    [Fact]
    public void A_line_no_pattern_gets_through_in_time_is_left_as_it_came()
    {
        var hostile = string.Concat(Enumerable.Repeat("**a ", 60_000));
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var plain = ClaudeCodeMarkdown.Plain(["**before**", hostile, "**after**"]);
        clock.Stop();

        Assert.Equal(["before", hostile, "after"], plain);
        Assert.Equal(hostile, ClaudeCodeMarkdown.Inline(hostile));
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(5), $"took {clock.Elapsed.TotalSeconds:0.0} s");
    }
}
