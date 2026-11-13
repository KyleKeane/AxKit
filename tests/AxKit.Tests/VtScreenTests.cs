using AxKit.Core.Transcript;
using AxKit.Core.Vt;
using Screen = AxKit.Core.Vt.Screen;

namespace AxKit.Tests;

/// <summary>
/// The screen's rows and their lines (docs/axTerminal/SPEC.md AT-4.1, AT-4.2): a line stays bound to its row as the
/// rows scroll, is found again by identity, and is committed when its row leaves the screen.
/// </summary>
public sealed class VtScreenTests
{
    private sealed class Store : LineStore
    {
        /// <summary>The texts the screen handed over as changed, in order.</summary>
        public List<string> Rewritten { get; } = [];

        public override void AddSystemLine(string text)
        {
        }

        protected override void CompleteFrame()
        {
        }

        protected override void TextChanged(Line line, string text)
        {
            Rewritten.Add(text);
            base.TextChanged(line, text);
        }
    }

    [Fact]
    public void A_row_rewritten_and_scrolled_away_within_one_frame_reaches_the_store_as_a_changed_text()
    {
        // docs/axKit/repairs.json R-21: the model learns of a rewritten row through TextChanged, also when the row
        // leaves the screen before the frame's end.
        var store = new Store();
        var screen = new Screen(20, 2, store);
        var parser = new VtParser(screen);
        parser.Feed("old");
        screen.PullRowText();
        store.Rewritten.Clear();

        // The first row rewritten, then two line feeds that scroll it away, with no frame end in between.
        parser.Feed("\rnew text\r\n\r\nz");
        Assert.Contains("new text", store.Rewritten);
        Assert.Equal("new text", store.Lines[0].Text);
        Assert.True(store.Lines[0].Committed);
    }

    [Fact]
    public void A_cell_takes_a_few_combining_characters_and_no_more()
    {
        // R-27: a run of combining characters without end must not grow one cell with each of them.
        var store = new Store();
        var screen = new Screen(20, 2, store);
        var parser = new VtParser(screen);
        parser.Feed("a" + new string('́', 500) + "b");
        screen.PullRowText();
        var text = store.Lines[0].Text;
        Assert.StartsWith("á", text);
        Assert.EndsWith("b", text);
        Assert.True(text.Length <= 20, $"The row's text is {text.Length} characters long.");
    }

    [Fact]
    public void A_line_is_found_on_its_row_through_a_scroll_and_committed_when_it_leaves()
    {
        var screen = new Screen(20, 3, new Store());
        var parser = new VtParser(screen);
        parser.Feed("a\r\nb\r\n");
        var line = screen.EnsureLine(2);
        Assert.Equal(2, screen.RowOf(line));
        Assert.Equal(string.Empty, line.Text);

        parser.Feed("c\r\nd");
        screen.PullRowText();
        Assert.Equal(1, screen.RowOf(line));
        Assert.Equal("c", line.Text);
        Assert.False(line.Committed);

        parser.Feed("\r\ne\r\nf");
        Assert.Equal(-1, screen.RowOf(line));
        Assert.True(line.Committed);
        Assert.Equal("c", line.Text);
    }

    /// <summary>Detached rows start blank (docs/axTerminal/SPEC.md AT-4.4): a shorter rewrite reads the new text alone, and a row left alone comes back as a blank line, not as a copy of the old one.</summary>
    [Fact]
    public void Detached_rows_keep_their_lines_text_and_start_blank()
    {
        var store = new Store();
        var screen = new Screen(20, 3, store);
        var parser = new VtParser(screen);
        parser.Feed("first long row\r\nsecond row");
        screen.PullRowText();
        var first = screen.LineAt(0)!;
        screen.DetachRows();
        Assert.True(first.Committed);
        Assert.Equal("first long row", first.Text);

        // The program homes the cursor and rewrites row 0 shorter, then prints on row 2: row 1 is a blank new line, not "second row" again.
        parser.Feed("\x1b[Hnew\r\n\r\nthird");
        screen.PullRowText();
        Assert.Equal("new", screen.LineAt(0)!.Text);
        Assert.Equal(string.Empty, screen.LineAt(1)!.Text);
        Assert.Equal("third", screen.LineAt(2)!.Text);
        Assert.Equal(5, store.Lines.Count);
    }

    /// <summary>A terminal reset lets the rows go like an erase: the Cleared event comes before the rows are blanked.</summary>
    [Fact]
    public void A_reset_raises_cleared_first()
    {
        var screen = new Screen(20, 3, new Store());
        var parser = new VtParser(screen);
        parser.Feed("kept");
        screen.PullRowText();
        var line = screen.LineAt(0)!;
        var seen = string.Empty;
        screen.Cleared += () => seen = screen.LineAt(0)?.Text ?? "(no line)";
        // ESC c (written as \u001b so that the c is not read as a hex digit of the escape).
        parser.Feed("\u001bc");
        Assert.Equal("kept", seen);
        Assert.Equal("kept", line.Text);
        Assert.True(screen.ClearedSinceFrameEnd);
    }

    [Fact]
    public void Ensuring_a_row_gives_the_blank_rows_above_it_lines_too()
    {
        var store = new Store();
        var screen = new Screen(20, 4, store);
        new VtParser(screen).Feed("a\r\n\r\n");
        var line = screen.EnsureLine(2);
        Assert.Same(line, screen.LineAt(2));
        Assert.NotNull(screen.LineAt(1));
        Assert.Equal(3, store.Lines.Count);
        // Another screen's line is on no row of this one.
        Assert.Equal(-1, screen.RowOf(new Screen(20, 4, new Store()).EnsureLine(0)));
    }

    /// <summary>S-40: an unterminated string in a program's output is dropped past its limit, and what follows is shown, not swallowed.</summary>
    [Fact]
    public void An_unterminated_osc_does_not_hide_what_follows()
    {
        var screen = new Screen(40, 3, new Store());
        var parser = new VtParser(screen);
        parser.Feed("\u001b]0;" + new string('x', VtParser.MaxString + 10) + "\r\nshown");
        screen.PullRowText();
        Assert.Contains("shown", Enumerable.Range(0, 3).Select(row => screen.LineAt(row)?.Text ?? string.Empty));
    }

    /// <summary>S-41: a scroll count far past the screen is clamped to the scroll region, and is quick.</summary>
    [Fact]
    public void A_huge_scroll_count_is_clamped()
    {
        var screen = new Screen(20, 3, new Store());
        var parser = new VtParser(screen);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 200; i++)
        {
            parser.Feed("\u001b[65535S\u001b[65535T");
        }

        Assert.True(watch.ElapsedMilliseconds < 2000, $"{watch.ElapsedMilliseconds} ms");
    }
}