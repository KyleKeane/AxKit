using AxClaude.Core.Record;

namespace AxClaude.Tests;

/// <summary>
/// The letters by which a reply's source is compared with its rows on the screen (docs/axClaude/events-and-grounding.md
/// EG-3.7): <see cref="RecordText.Letters"/> and Claude Code's rules in <see cref="ClaudeCodeTranscript.ScreenKey"/>,
/// against the rows the measured rules give (EG 4.3: Claude Code 2.1.325 in screen reader mode at 240 columns).
/// </summary>
public sealed class RecordTextTests
{
    [Fact]
    public void Letters_keeps_letters_and_digits_in_lower_case()
    {
        Assert.Equal("helloworld42caféxy", RecordText.Letters("Hello, World! 42 · Café — x_y"));
        Assert.Equal("ab", RecordText.Letters("  A\r\n\tb  ".AsSpan()));
        Assert.Equal("", RecordText.Letters("- * > # `` | --- 🙂"));
    }

    [Fact]
    public void The_screen_key_drops_fence_lines()
    {
        // The fences and the language named on the first are not drawn; the code between them is, as written.
        Assert.Equal("runthisprint1done", ClaudeCodeTranscript.ScreenKey(Source("Run this:", "```python", "print(1)", "```", "Done.")));
        Assert.Equal("runthisprint1done", ClaudeCodeTranscript.ScreenKey(Source("Run this:", "~~~~ text", "print(1)", "~~~~", "Done.")));

        // A table's rule does not reach into code: these rows are code and keep their order.
        Assert.Equal("ab12", ClaudeCodeTranscript.ScreenKey(Source("```", "| a | b |", "| - | - |", "| 1 | 2 |", "```")));

        // A shorter run does not close the fence: it is a line of the code, and has no letters.
        Assert.Equal("xyz", ClaudeCodeTranscript.ScreenKey(Source("````", "x", "```", "y", "````", "z")));
    }

    [Fact]
    public void The_screen_key_reads_a_table_as_rows_of_header_and_cell()
    {
        var key = ClaudeCodeTranscript.ScreenKey(Source("| Name | Size |", "| --- | ---: |", "| apple | 3 |", "| pear | 12 |", "", "After."));

        // Claude Code draws "Name: apple. Size: 3." and "Name: pear. Size: 12.", and neither the header nor the separator.
        Assert.Equal("nameapplesize3namepearsize12after", key);
        Assert.Contains(RecordText.Letters("Name: apple. Size: 3."), key);
        Assert.Contains(RecordText.Letters("Name: pear. Size: 12."), key);

        // Without the outer pipes, and with a cell missing: the header stays, so the key holds what the row may show.
        Assert.Equal("a1b", ClaudeCodeTranscript.ScreenKey(Source("A | B", "--|--", "1 |")));
    }

    [Fact]
    public void A_link_whose_text_is_its_target_counts_once()
    {
        // Measured: "see https://example.com." and "Read the example site (https://example.com/page) today."
        Assert.Equal("seehttpsexamplecom", ClaudeCodeTranscript.ScreenKey("see [https://example.com](https://example.com)."));
        Assert.Equal("readtheexamplesitehttpsexamplecompagetoday",
            ClaudeCodeTranscript.ScreenKey("Read [the example site](https://example.com/page) today."));
        Assert.Equal(RecordText.Letters("see https://example.com."), ClaudeCodeTranscript.ScreenKey("see [https://example.com](https://example.com)."));
    }

    /// <summary>
    /// An invented reply with a heading, a list, a numbered list, a code block, a table, a quote, a paragraph of 704
    /// characters and two links, and the rows Claude Code's measured rules give for it at 240 columns (EG 4.3), written
    /// out by hand: every row's letters stand in the key, in the rows' order, each behind the one before. The head's
    /// label is the gate's to take off (EG-5.3), so the first row is written without it. Blank rows have no letters.
    /// </summary>
    [Fact]
    public void Every_screen_row_of_the_markdown_sample_is_found_in_the_key_in_order()
    {
        var source = Source(
            "# Garden Notes",
            "",
            "The garden has three beds and one small pond.",
            "",
            "## What to plant",
            "",
            "- Tomatoes in the **sunny** bed",
            "- Beans along the fence",
            "- Mint in a pot, *never* in the ground",
            "",
            "1. Dig the soil in March.",
            "2. Sow the seeds in `April`.",
            "",
            "```bash",
            "water --bed north --minutes 10",
            "```",
            "",
            "| Bed | Plant |",
            "| --- | --- |",
            "| North | Tomatoes |",
            "| East | Beans |",
            "",
            "> A garden is never finished.",
            "",
            Paragraph,
            "",
            "See [https://example.org/garden](https://example.org/garden) and [the seed list](https://example.org/seeds) for more.");
        string[] rows =
        [
            "Garden Notes",
            "",
            "The garden has three beds and one small pond.",
            "",
            "What to plant",
            "",
            "- Tomatoes in the sunny bed",
            "- Beans along the fence",
            "- Mint in a pot, never in the ground",
            "",
            "1. Dig the soil in March.",
            "2. Sow the seeds in April.",
            "",
            "water --bed north --minutes 10",
            "",
            "Bed: North. Plant: Tomatoes.",
            "Bed: East. Plant: Beans.",
            "A garden is never finished.",
            "",
            "Every spring the garden wakes slowly, first with the green tips of the garlic that went into the ground in October, then with the crocus by the gate and the rhubarb that pushes its red fists up through the mulch. The pond fills with frog",
            "spawn in the second week of March, and the blackbirds start to sing before the light comes up. By May the beds need water every other evening, the slugs come out after rain, and the tomatoes have to be tied to their canes as they climb.",
            "Weeding is easier after a shower, when the roots let go of the soil without a fight. In the long evenings of June there is time to sit by the pond, count the dragonflies and plan the next year's beds on the back of a seed packet.",
            "",
            "See https://example.org/garden and the seed list (https://example.org/seeds) for more.",
        ];
        Assert.Equal(704, Paragraph.Length);
        Assert.All(rows, row => Assert.True(row.Length <= 240, row));
        Assert.Equal(Paragraph, string.Join(' ', rows[19..22]));

        var key = ClaudeCodeTranscript.ScreenKey(source);
        var place = 0;
        foreach (var row in rows)
        {
            var letters = RecordText.Letters(row);
            var found = key.IndexOf(letters, place, StringComparison.Ordinal);
            Assert.True(found >= 0, $"Not found in the key at or after {place}: {row}");
            place = found + letters.Length;
        }

        Assert.Equal(key.Length, place);
        Assert.DoesNotContain("bash", key);
        Assert.DoesNotContain("bedplant", key);
    }

    private const string Paragraph =
        "Every spring the garden wakes slowly, first with the green tips of the garlic that went into the ground in October, then with the crocus by the gate and the rhubarb that pushes its red fists up through the mulch. "
        + "The pond fills with frog spawn in the second week of March, and the blackbirds start to sing before the light comes up. "
        + "By May the beds need water every other evening, the slugs come out after rain, and the tomatoes have to be tied to their canes as they climb. "
        + "Weeding is easier after a shower, when the roots let go of the soil without a fight. "
        + "In the long evenings of June there is time to sit by the pond, count the dragonflies and plan the next year's beds on the back of a seed packet.";

    /// <summary>A Markdown source as a reply's text holds it: the lines joined by LF.</summary>
    private static string Source(params string[] lines) => string.Join('\n', lines);
}
