using AxKit.Core;
using AxTemplate.Core;

namespace AxTemplate.Tests;

/// <summary>SPEC.md TP-4.8: a template of several lines into a field of one line, or into a terminal.</summary>
public sealed class PasteTargetTests
{
    [Theory]
    [InlineData("Notepad", "Edit", 0x0004, LineBreaks.Kept)]
    [InlineData("#32770", "Edit", 0x0080, LineBreaks.OneLine)]
    [InlineData("WindowsForms10.Window.8.app.0.1", "WindowsForms10.EDIT.app.0.1", 0, LineBreaks.OneLine)]
    [InlineData("WordPadClass", "RICHEDIT50W", 0x0004, LineBreaks.Kept)]
    [InlineData("ConsoleWindowClass", "ConsoleWindowClass", 0, LineBreaks.EachLineRuns)]
    [InlineData("CASCADIA_HOSTING_WINDOW_CLASS", "Windows.UI.Input.InputSite.WindowClass", 0, LineBreaks.EachLineRuns)]
    [InlineData("Chrome_WidgetWin_1", "Chrome_RenderWidgetHostHWND", 0, LineBreaks.Kept)]
    public void What_Windows_says_of_the_control_decides(string top, string focus, long style, LineBreaks expected) =>
        Assert.Equal(expected, PasteTarget.Of(top, focus, style));

    [Fact]
    public void One_line_joins_the_lines_with_one_space_and_keeps_the_caret()
    {
        var made = TextTemplate.Expand("Dear {caret}Sam,  \r\n\r\n  Thanks\n", string.Empty, string.Empty);
        var one = TextTemplate.OneLine(made);
        Assert.Equal("Dear Sam, Thanks", one.Text);
        Assert.Equal(5, one.Caret);
        Assert.False(TextTemplate.HasLines(one));
    }

    [Fact]
    public void A_caret_at_a_break_lands_on_its_space()
    {
        var one = TextTemplate.OneLine(TextTemplate.Expand("one\r\n{caret}two", string.Empty, string.Empty));
        Assert.Equal("one two", one.Text);
        Assert.Equal(4, one.Caret);
        Assert.Equal("first", TextTemplate.OneLine(TextTemplate.Expand("\r\nfirst\r\n", string.Empty, string.Empty)).Text);
        var end = TextTemplate.OneLine(TextTemplate.Expand("a\r\nb", string.Empty, string.Empty));
        Assert.Equal(3, end.Caret);
    }

    [Fact]
    public void The_question_counts_the_lines()
    {
        Assert.Equal(3, PasteTarget.LinesOf("cd x\r\nls\r\necho done\r\n"));
        Assert.Equal("Deploy has 3 lines, and a terminal runs each line as it is pasted", PasteTarget.ConsoleQuestion("Deploy", 3));
    }
}
