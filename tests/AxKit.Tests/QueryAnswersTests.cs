using AxKit.Core.Vt;

namespace AxKit.Tests;

/// <summary>A program's colour questions and the terminal's answers (docs/axTerminal/SPEC.md AT-9.10): a plain black background and light text.</summary>
public sealed class QueryAnswersTests
{
    private const string Esc = "\x1b";

    [Fact]
    public void Each_colour_asked_gets_its_answer_in_xterms_form()
    {
        Assert.Equal(
            [$"{Esc}]4;0;rgb:0000/0000/0000\a", $"{Esc}]4;7;rgb:cccc/cccc/cccc\a", $"{Esc}]4;9;rgb:e7e7/4848/5656\a"],
            QueryAnswers.To("4;0;?;7;?;9;?"));
        Assert.Equal([$"{Esc}]10;rgb:cccc/cccc/cccc\a"], QueryAnswers.To("10;?"));
        Assert.Equal([$"{Esc}]11;rgb:0000/0000/0000\a"], QueryAnswers.To("11;?"));
    }

    [Fact]
    public void A_colour_set_or_another_string_asks_nothing()
    {
        Assert.Empty(QueryAnswers.To("4;1;rgb:ff/00/00"));
        Assert.Empty(QueryAnswers.To("4;300;?"));
        Assert.Empty(QueryAnswers.To("11;rgb:00/00/00"));
        Assert.Empty(QueryAnswers.To("0;a title"));
        Assert.Empty(QueryAnswers.To("133;A"));
        // A pair asked among pairs set: only the one asked.
        Assert.Equal([$"{Esc}]4;2;rgb:1313/a1a1/0e0e\a"], QueryAnswers.To("4;1;rgb:ff/00/00;2;?"));
    }

    [Fact]
    public void The_colours_above_sixteen_are_xterms_cube_and_greys()
    {
        Assert.Equal(0x000000, QueryAnswers.Colour(16));
        Assert.Equal(0x5F87AF, QueryAnswers.Colour(67));
        Assert.Equal(0xFFFFFF, QueryAnswers.Colour(231));
        Assert.Equal(0x080808, QueryAnswers.Colour(232));
        Assert.Equal(0xEEEEEE, QueryAnswers.Colour(255));
    }
}
