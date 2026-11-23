using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The templates of an editor (docs/axDown/SPEC.md AD-13): three marks, the caret's place, the selection carried
/// in, and nothing else read as a mark. Each case shows the caret as "|" in what comes out.
/// </summary>
public sealed class TextTemplateTests
{
    private static string Out(string template, string selection = "", string clipboard = "")
    {
        var made = TextTemplate.Expand(template, selection, clipboard);
        Assert.InRange(made.Caret, 0, made.Text.Length);
        return made.Text.Insert(made.Caret, "|");
    }

    [Theory]
    [InlineData("[{selection}]({caret})", "axKit", "[axKit](|)")]
    [InlineData("[{selection}]({caret})", "", "[](|)")]
    [InlineData("plain text", "", "plain text|")]
    [InlineData("{caret}first, {caret}second", "", "|first, second")]
    [InlineData("a {selection} and again {selection}", "x", "a x and again x|")]
    public void The_caret_lands_at_its_mark_and_the_selection_goes_where_it_is_named(string template, string selection, string expected)
    {
        Assert.Equal(expected, Out(template, selection));
    }

    [Fact]
    public void The_clipboard_has_a_mark_of_its_own()
    {
        Assert.Equal("[site](https://example.org)|", Out("[{selection}]({clipboard})", "site", "https://example.org"));
        Assert.Equal("[site]()|", Out("[{selection}]({clipboard})", "site"));
    }

    [Fact]
    public void What_comes_in_through_a_mark_is_never_read_for_marks()
    {
        Assert.Equal("say {caret} here|", Out("say {selection} here", "{caret}"));
        Assert.Equal("{selection}|", Out("{clipboard}", "words", "{selection}"));
    }

    [Fact]
    public void Other_braces_are_the_templates_own_text()
    {
        Assert.Equal("{ \"name\": \"|\" }", Out("{ \"name\": \"{caret}\" }"));
        Assert.Equal("{carets} {Caret} {|", Out("{carets} {Caret} {"));
    }

    [Fact]
    public void Line_endings_become_the_edit_controls()
    {
        Assert.Equal("```\r\ncode\r\nmore|\r\n```", Out("```\n{selection}{caret}\n```", "code\nmore"));
    }

    [Fact]
    public void A_template_file_is_named_by_its_file()
    {
        Assert.Equal("Meeting notes", TextTemplate.NameOf(Path.Combine("templates", "axDown", "Meeting notes.md")));
    }
}
