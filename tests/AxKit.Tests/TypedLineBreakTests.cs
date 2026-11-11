using AxKit.Core;

namespace AxKit.Tests;

/// <summary>docs/axKit/SPEC.md AX-7.8: "new line" only when a key put a line break into the text.</summary>
public class TypedLineBreakTests
{
    [Fact]
    public void A_line_break_typed_at_the_caret_is_one()
    {
        Assert.True(TypedLineBreak.Inserted("ab", 1, 0, "a\r\nb", 3));
        Assert.True(TypedLineBreak.Inserted("", 0, 0, "\r\n", 2));
        Assert.True(TypedLineBreak.Inserted("line", 4, 0, "line\n", 5));
    }

    [Fact]
    public void A_selection_replaced_by_a_line_break_with_an_indent_is_one()
    {
        Assert.True(TypedLineBreak.Inserted("one two", 3, 1, "one\r\ntwo", 5));
        Assert.True(TypedLineBreak.Inserted("  a", 3, 0, "  a\r\n  ", 7));
    }

    [Fact]
    public void Enter_that_sends_submits_or_meets_a_read_only_field_is_none()
    {
        // axClaude's Enter sends the message and empties the field.
        Assert.False(TypedLineBreak.Inserted("hello", 5, 0, "", 0));
        // Enter on an empty field that sends nothing, or a read-only field: nothing changed.
        Assert.False(TypedLineBreak.Inserted("", 0, 0, "", 0));
        Assert.False(TypedLineBreak.Inserted("text", 2, 0, "text", 2));
        // Another text in its place (a recalled command), or a letter typed.
        Assert.False(TypedLineBreak.Inserted("ab", 1, 0, "a\r\n", 3));
        Assert.False(TypedLineBreak.Inserted("ab", 1, 0, "axb", 2));
        // A line break somewhere else than at the caret.
        Assert.False(TypedLineBreak.Inserted("ab", 1, 0, "a\r\nb", 1));
        // Spaces alone are no line break.
        Assert.False(TypedLineBreak.Inserted("ab", 1, 0, "a  b", 3));
        // A selection out of the text's range.
        Assert.False(TypedLineBreak.Inserted("ab", 3, 0, "ab\r\n", 4));
    }
}
