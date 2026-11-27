using AxKit.Core;
using AxTemplate.Core;

namespace AxTemplate.Tests;

/// <summary>SPEC.md TP-4.8: a template of several lines into a field of one line, or into a terminal.</summary>
public sealed class PasteTargetTests
{
    /// <summary>TP-4.3 (review, 2026-11-27): a password copied as private was put back as plain text, into Windows' clipboard history and axClip, past its owner's clearing.</summary>
    [Fact]
    public void The_clipboard_gets_back_a_plain_text_and_never_a_private_copy()
    {
        Assert.True(PasteTarget.GivesBack("what was copied", beforeWasPrivate: false, stillTheTemplate: true));
        Assert.False(PasteTarget.GivesBack("a password", beforeWasPrivate: true, stillTheTemplate: true));
        // Nothing that was copied since the template went on the clipboard is lost, and no empty text is put there.
        Assert.False(PasteTarget.GivesBack("what was copied", beforeWasPrivate: false, stillTheTemplate: false));
        Assert.False(PasteTarget.GivesBack(string.Empty, beforeWasPrivate: false, stillTheTemplate: true));

        // The launcher asks the rule with the marks it read as it came up, and its F1 text promises no more than it.
        var root = SourceTexts.RepositoryRoot();
        var launcher = File.ReadAllText(Path.Combine(root, "src", "AxTemplate", "LauncherForm.cs"));
        Assert.Contains("PrivateClipboard.HoldsPrivateCopy(Handle)", launcher, StringComparison.Ordinal);
        Assert.Contains("PasteTarget.GivesBack(before, beforeWasPrivate, PrivateClipboard.Sequence == sequence)", launcher, StringComparison.Ordinal);
        var help = File.ReadAllText(Path.Combine(root, "src", "AxTemplate", "HelpText.cs"));
        Assert.Contains("a password copied as private are not put back", help, StringComparison.Ordinal);
        Assert.DoesNotContain("what you had copied is on the clipboard again afterwards", help, StringComparison.Ordinal);
    }

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

    /// <summary>
    /// TP-4.8: lines that came with the selection, after the question could be asked, never reach a terminal as
    /// lines: without an answer the text goes in on one line. With an answer, the answer; a field of one line always.
    /// </summary>
    [Theory]
    [InlineData(null, LineBreaks.EachLineRuns, true, true)]
    [InlineData(LineBreaks.Kept, LineBreaks.EachLineRuns, true, false)]
    [InlineData(LineBreaks.OneLine, LineBreaks.EachLineRuns, true, true)]
    [InlineData(null, LineBreaks.OneLine, true, true)]
    [InlineData(null, LineBreaks.Kept, true, false)]
    [InlineData(null, LineBreaks.EachLineRuns, false, false)]
    [InlineData(null, LineBreaks.OneLine, false, false)]
    public void Lines_reach_a_terminal_only_by_the_users_answer(LineBreaks? answer, LineBreaks target, bool hasLines, bool oneLine)
    {
        Assert.Equal(oneLine, PasteTarget.MadeOneLine(answer, target, hasLines));
    }

    /// <summary>TP-4.3: a text filled from a private copy is put on the clipboard as a private copy; any other as plain text.</summary>
    [Fact]
    public void A_template_filled_from_a_private_copy_is_copied_as_private()
    {
        Assert.True(PasteTarget.CopiedAsPrivate("Password: {clipboard}", clipboardWasPrivate: true));
        Assert.False(PasteTarget.CopiedAsPrivate("Password: {clipboard}", clipboardWasPrivate: false));
        Assert.False(PasteTarget.CopiedAsPrivate("Kind regards", clipboardWasPrivate: true));
    }

    /// <summary>TP-D10: the caret's mark is taken for the pasted one only when more marks stand before the caret than before the paste.</summary>
    [Fact]
    public void The_marks_of_a_text_are_counted()
    {
        Assert.Equal(0, PasteTarget.MarksIn("no mark here", TextTemplate.CaretMark));
        Assert.Equal(2, PasteTarget.MarksIn("Dear {caret}, see {caret}", TextTemplate.CaretMark));
        Assert.Equal(1, PasteTarget.MarksIn("{caret}", TextTemplate.CaretMark));
        Assert.Equal(0, PasteTarget.MarksIn("{caret}", string.Empty));
    }

    [Fact]
    public void The_question_counts_the_lines()
    {
        Assert.Equal(3, PasteTarget.LinesOf("cd x\r\nls\r\necho done\r\n"));
        Assert.Equal("Deploy has 3 lines, and a terminal runs each line as it is pasted", PasteTarget.ConsoleQuestion("Deploy", 3));
    }
}
