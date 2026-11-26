using System.Text;
using AxKit.Core.Transcript;
using AxKit.Core.Vt;
using AxKit.TestShared;
using Screen = AxKit.Core.Vt.Screen;

namespace AxKit.Tests;

/// <summary>
/// The page of the alternate screen (docs/axTerminal/SPEC.md AT-9.2, AT-9.4): the rows as text, the cursor, and the
/// marked runs, read off each cell's background as the program set it with SGR; and the recording of Windows' edit
/// read through it.
/// </summary>
public sealed class ScreenPageTests
{
    private const string Esc = "\x1b";

    private sealed class Store : LineStore
    {
        public override void AddSystemLine(string text)
        {
        }

        protected override void CompleteFrame()
        {
        }
    }

    /// <summary>A page of 10 by 3 on the alternate screen after the given text: row 0 is what the test draws, rows 1 and 2 stay of the default background, so that it is the page's own.</summary>
    private static ScreenPage PageAfter(string text)
    {
        var screen = new Screen(10, 3, new Store()) { KeepsAlternateApart = true };
        new VtParser(screen).Feed($"{Esc}[?1049h{Esc}[H" + text);
        return ScreenPage.Read(screen)!;
    }

    [Fact]
    public void Rows_leave_out_trailing_spaces_and_keep_box_and_block_characters_as_drawn()
    {
        var page = PageAfter("┌─ ab ─┐█▆\r\n│ x  │  ");
        Assert.Equal(["┌─ ab ─┐█▆", "│ x  │", string.Empty], page.Rows);
        Assert.Equal((1, 8), (page.CursorRow, page.CursorColumn));
    }

    [Fact]
    public void A_run_inside_a_box_of_another_background_is_marked()
    {
        // A blue box with a red entry in it, drawn on the page's own background.
        var page = PageAfter($"{Esc}[44m  {Esc}[41mNew{Esc}[44m   {Esc}[0m");
        var run = Assert.Single(page.Marked);
        Assert.Equal(new MarkedRun(0, 2, 3, "New"), run);
    }

    [Fact]
    public void A_box_on_the_pages_own_background_and_a_run_at_the_rows_end_are_not_marked()
    {
        Assert.Empty(PageAfter($"  {Esc}[44mbox{Esc}[0m   ").Marked);
        Assert.Empty(PageAfter($"{Esc}[44m  {Esc}[41medge{Esc}[0m").Marked);
        Assert.Empty(PageAfter($"{Esc}[41medge{Esc}[44m  {Esc}[0m").Marked);
    }

    [Fact]
    public void Reverse_video_is_marked_on_the_pages_own_background_too()
    {
        var page = PageAfter($"a {Esc}[7m^G{Esc}[27m b");
        Assert.Equal(new MarkedRun(0, 2, 2, "^G"), Assert.Single(page.Marked));
    }

    [Fact]
    public void The_same_colour_written_two_ways_is_one_background()
    {
        // 41 and 48;5;1 are palette colour 1; 48;2;r;g;b and 48:2::r:g:b the same colour of 24 bits.
        Assert.Equal("ab", Assert.Single(PageAfter($"{Esc}[44m {Esc}[41ma{Esc}[48;5;1mb{Esc}[44m {Esc}[0m").Marked).Text);
        Assert.Equal("ab", Assert.Single(PageAfter($"{Esc}[44m {Esc}[48;2;1;2;3ma{Esc}[48:2::1:2:3mb{Esc}[44m {Esc}[0m").Marked).Text);
        Assert.Equal("ab", Assert.Single(PageAfter($"{Esc}[44m {Esc}[48;2;1;2;3ma{Esc}[48:2:1:2:3mb{Esc}[44m {Esc}[0m").Marked).Text);
        // A bright colour is another one: its run between two of palette colour 1 is marked.
        Assert.Equal("b", Assert.Single(PageAfter($"{Esc}[44m {Esc}[41ma{Esc}[101mb{Esc}[41mc{Esc}[44m {Esc}[0m").Marked).Text);
    }

    [Fact]
    public void Foregrounds_and_other_attributes_leave_the_background_alone()
    {
        // 38;5;41 is a foreground whose index looks like a background code; 4:3 a curly underline, not 43; 1 bold.
        Assert.Empty(PageAfter($"{Esc}[44m {Esc}[38;5;41ma{Esc}[38:2::9:9:9mb{Esc}[4:3mc{Esc}[1md {Esc}[0m").Marked);
        // 49 and 0 put the default back.
        Assert.Empty(PageAfter($"{Esc}[44m{Esc}[49m  a  {Esc}[44m{Esc}[0m  b").Marked);
    }

    [Fact]
    public void An_erase_takes_the_background_in_force()
    {
        // The whole page erased blue: blue is the page's own, and a red run on it is not marked; a green one inside the red is.
        var page = PageAfter($"{Esc}[44m{Esc}[2J{Esc}[1;2H{Esc}[41mab{Esc}[42mc{Esc}[41md{Esc}[44m");
        Assert.Equal("c", Assert.Single(page.Marked).Text);
        // An erase to the end of the row (ESC[K) and of characters (ESC[X) too.
        var row = PageAfter($"{Esc}[44m{Esc}[K{Esc}[1;3H{Esc}[41m{Esc}[2X{Esc}[44m");
        Assert.Equal(new MarkedRun(0, 2, 2, string.Empty), Assert.Single(row.Marked));
    }

    /// <summary>
    /// The recording of Windows' edit 1.2.1 at 120 by 30 (AT-9.7), replayed chunk by chunk with a page read after
    /// each: it switches to the alternate screen and draws its menu bar; with its File menu open the one marked run is
    /// the selected entry; its unsaved-changes dialog marks its button in focus; after Ctrl+Q and N it has left the
    /// alternate screen, and nothing of it reached the transcript.
    /// </summary>
    [Fact]
    public void Windows_edit_is_read_as_a_page()
    {
        const string fixture = "fullscreen-edit1.2.1-120x30.vt";
        var bytes = Fixtures.Bytes(fixture);
        var store = new Store();
        var screen = new Screen(120, 30, store) { KeepsAlternateApart = true };
        var parser = new VtParser(screen);
        var decoder = Encoding.UTF8.GetDecoder();
        var pages = new List<ScreenPage>();
        foreach (var chunk in Fixtures.Chunks(fixture))
        {
            var chars = new char[chunk.Length + 4];
            var count = decoder.GetChars(bytes, chunk.Offset, chunk.Length, chars, 0);
            parser.Feed(chars.AsSpan(0, count));
            if (ScreenPage.Read(screen) is { } page)
            {
                pages.Add(page);
            }
        }

        // The first page with the file drawn (before it, edit shows an ellipsis while it waits on its colour questions).
        var first = pages.First(p => p.Rows[1].Length > 0);
        Assert.Equal(" File  Edit  View  Help", first.Rows[0]);
        // The line's number, edit's vertical bar, the line; at the row's end the scroll bar, drawn with block characters.
        Assert.StartsWith("1 │ First line of a note.    ", first.Rows[1], StringComparison.Ordinal);
        Assert.InRange(first.Rows[1][^1], '─', '▟');
        Assert.Equal((1, 4), (first.CursorRow, first.CursorColumn));
        Assert.All(pages.TakeWhile(p => !ReferenceEquals(p, first)), p => Assert.Empty(p.Marked));

        var menu = pages.First(p => p.Marked.Any(m => m.Text.StartsWith("New File", StringComparison.Ordinal)));
        var selected = Assert.Single(menu.Marked);
        Assert.Equal(new MarkedRun(2, 1, 22, "New File    Ctrl+N"), selected);
        Assert.Contains(pages, p => p.Marked.Any(m => m.Text == "[Save]"));

        Assert.False(screen.Alternate);
        Assert.Empty(store.Lines);
    }
}
