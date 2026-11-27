using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The format commands of a Markdown text (docs/axDown/SPEC.md AD-12): each case is a text with the caret as "|",
/// or the selection between "[" and "]", the command, and the text after it with the caret as "|". Every command
/// is a toggle, so most cases come in pairs.
/// </summary>
public sealed class MarkdownEditTests
{
    /// <summary>The command run on a marked text: the text after it with the caret marked, and what was said.</summary>
    private static (string Text, string Said, bool Edge) Run(string marked, Func<string, int, int, MarkdownChange> command)
    {
        int start;
        var length = 0;
        string text;
        if (marked.Contains('['))
        {
            start = marked.IndexOf('[');
            length = marked.IndexOf(']') - start - 1;
            text = marked.Replace("[", string.Empty).Replace("]", string.Empty);
        }
        else
        {
            start = marked.IndexOf('|');
            text = marked.Replace("|", string.Empty);
        }

        var change = command(text, start, length);
        var after = text[..change.Start] + change.Text + text[(change.Start + change.Length)..];
        Assert.InRange(change.Caret, 0, after.Length);
        return (after.Insert(change.Caret, "|"), change.Said, change.Edge);
    }

    private static Func<string, int, int, MarkdownChange> Heading(int level) => (text, caret, _) => MarkdownEdit.Heading(text, caret, level);

    private static readonly Func<string, int, int, MarkdownChange> Deeper = (text, caret, _) => MarkdownEdit.HeadingDeeper(text, caret);

    private static readonly Func<string, int, int, MarkdownChange> Shallower = (text, caret, _) => MarkdownEdit.HeadingShallower(text, caret);

    // ---- headings ----

    [Theory]
    [InlineData("one\r\nTi|tle\r\nthree", 2, "one\r\n## Ti|tle\r\nthree", "Heading 2")]
    [InlineData("one\r\n## Ti|tle\r\nthree", 2, "one\r\nTi|tle\r\nthree", "No heading")]
    [InlineData("## Ti|tle", 4, "#### Ti|tle", "Heading 4")]
    [InlineData("#|## Title", 1, "# |Title", "Heading 1")]
    [InlineData("|", 1, "# |", "Heading 1")]
    [InlineData("  indented| line", 3, "### indented| line", "Heading 3")]
    [InlineData("#tag| is no heading", 1, "# #tag| is no heading", "Heading 1")]
    [InlineData("a\nb|\nc", 1, "a\n# b|\nc", "Heading 1")]
    public void A_heading_level_is_set_changed_or_taken_away(string before, int level, string after, string said)
    {
        Assert.Equal((after, said, false), Run(before, Heading(level)));
    }

    [Fact]
    public void Deeper_adds_a_number_sign_and_shallower_takes_one()
    {
        Assert.Equal(("# Ti|tle", "Heading 1", false), Run("Ti|tle", Deeper));
        Assert.Equal(("## Ti|tle", "Heading 2", false), Run("# Ti|tle", Deeper));
        Assert.Equal(("# Ti|tle", "Heading 1", false), Run("## Ti|tle", Shallower));
        Assert.Equal(("Ti|tle", "No heading", false), Run("# Ti|tle", Shallower));
    }

    [Fact]
    public void At_an_end_of_the_levels_nothing_changes_and_the_edge_is_said()
    {
        Assert.Equal(("###### Ti|tle", "Heading 6", true), Run("###### Ti|tle", Deeper));
        Assert.Equal(("Ti|tle", "No heading", true), Run("Ti|tle", Shallower));
    }

    // ---- bold, italic, code ----

    [Theory]
    [InlineData("a wo|rd here", "a **wo|rd** here", "Bold on")]
    [InlineData("a **wo|rd** here", "a wo|rd here", "Bold off")]
    [InlineData("a [two words] here", "a **two words|** here", "Bold on")]
    [InlineData("a **[two words]** here", "a two words| here", "Bold off")]
    [InlineData("a [**two words**] here", "a two words| here", "Bold off")]
    [InlineData("a[ word ]here", "a **word|** here", "Bold on")]
    [InlineData("a | b", "a **|** b", "Bold on")]
    [InlineData("a **|** b", "a | b", "Bold off")]
    [InlineData("a *wo|rd* here", "a ***wo|rd*** here", "Bold on")]
    [InlineData("a ***wo|rd*** here", "a *wo|rd* here", "Bold off")]
    [InlineData("it's| here", "**it's|** here", "Bold on")]
    public void Bold_goes_around_the_selection_or_the_word_and_comes_off_again(string before, string after, string said)
    {
        Assert.Equal((after, said, false), Run(before, MarkdownEdit.Bold));
    }

    [Theory]
    [InlineData("a wo|rd here", "a *wo|rd* here", "Italic on")]
    [InlineData("a *wo|rd* here", "a wo|rd here", "Italic off")]
    [InlineData("a **wo|rd** here", "a ***wo|rd*** here", "Italic on")]
    [InlineData("a ***wo|rd*** here", "a **wo|rd** here", "Italic off")]
    public void Italic_is_one_asterisk_beside_bolds_two(string before, string after, string said)
    {
        Assert.Equal((after, said, false), Run(before, MarkdownEdit.Italic));
    }

    [Theory]
    [InlineData("run dot|net now", "run `dot|net` now", "Code on")]
    [InlineData("run `dot|net` now", "run dot|net now", "Code off")]
    [InlineData("run [dotnet build] now", "run `dotnet build|` now", "Code on")]
    [InlineData("say [the ` sign] here", "say ``the ` sign|`` here", "Code on")]
    public void Code_is_backticks(string before, string after, string said)
    {
        Assert.Equal((after, said, false), Run(before, MarkdownEdit.Code));
    }

    /// <summary>
    /// The caret inside the marks that open or close a word acts on that word. Until 2026-11-27 the marks on both
    /// sides of the caret were taken for a pair around nothing and deleted: "**word**" with the caret after the
    /// first asterisk became "word**".
    /// </summary>
    [Theory]
    [InlineData("*|*word**", "***|word***", "Italic on")]
    [InlineData("**word*|*", "***word|***", "Italic on")]
    [InlineData("a *|**word*** b", "a **|word** b", "Italic off")]
    [InlineData("a *|* b", "a | b", "Italic off")]
    public void Italic_with_the_caret_inside_a_words_marks_acts_on_the_word(string before, string after, string said)
    {
        Assert.Equal((after, said, false), Run(before, MarkdownEdit.Italic));
    }

    [Theory]
    [InlineData("`|`dotnet``", "|dotnet", "Code off")]
    [InlineData("run `dotnet|` now", "run dotnet| now", "Code off")]
    [InlineData("run `|` now", "run | now", "Code off")]
    public void Code_with_the_caret_inside_a_words_marks_acts_on_the_word(string before, string after, string said)
    {
        Assert.Equal((after, said, false), Run(before, MarkdownEdit.Code));
    }

    // ---- list items and block quotes ----

    [Theory]
    [InlineData("mi|lk", "- mi|lk", "List item on")]
    [InlineData("- mi|lk", "mi|lk", "List item off")]
    [InlineData("|", "- |", "List item on")]
    [InlineData("  * mi|lk", "  mi|lk", "List item off")]
    [InlineData("  mi|lk", "  - mi|lk", "List item on")]
    [InlineData("-| milk", "|milk", "List item off")]
    public void A_line_becomes_a_list_item_and_back(string before, string after, string said)
    {
        Assert.Equal((after, said, false), Run(before, MarkdownEdit.ListItem));
    }

    [Fact]
    public void Every_line_of_a_selection_becomes_an_item_but_the_empty_ones()
    {
        Assert.Equal(("- milk\r\n\r\n- eggs\r\n- tea|\r\nlast", "List item on, 3 lines", false),
            Run("mi[lk\r\n\r\n- eggs\r\nte]a\r\nlast", MarkdownEdit.ListItem));
        Assert.Equal(("milk\r\n\r\neggs|\r\nlast", "List item off, 2 lines", false),
            Run("[- milk\r\n\r\n- eggs\r\n]last", MarkdownEdit.ListItem));
    }

    [Theory]
    [InlineData("he sa|id", "> he sa|id", "Block quote on")]
    [InlineData("> he sa|id", "he sa|id", "Block quote off")]
    [InlineData(">he sa|id", "he sa|id", "Block quote off")]
    [InlineData("|", "> |", "Block quote on")]
    public void A_line_becomes_a_block_quote_and_back(string before, string after, string said)
    {
        Assert.Equal((after, said, false), Run(before, MarkdownEdit.BlockQuote));
    }

    [Fact]
    public void A_quote_of_several_lines_stays_one_quote()
    {
        Assert.Equal(("> one\n>\n> two|\nlast", "Block quote on, 2 lines", false), Run("[one\n\ntwo]\nlast", MarkdownEdit.BlockQuote));
        Assert.Equal(("one\n\ntwo|\nlast", "Block quote off, 2 lines", false), Run("[> one\n>\n> two]\nlast", MarkdownEdit.BlockQuote));
    }
}
