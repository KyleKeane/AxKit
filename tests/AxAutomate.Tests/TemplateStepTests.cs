using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The step <c>template</c> (docs/axTemplate/SPEC.md TP-6): a template found by its name, its marks filled from the
/// step's own texts, typed by lines with the caret put back at its mark; a name no template has fails the step.
/// </summary>
public sealed class TemplateStepTests
{
    private static readonly Template[] Templates =
    [
        new("Thanks", "Dear {caret}, thank you for {clipboard}", [], "Thanks.json"),
        new("Link", "[{selection}]({caret})", [], "Link.json"),
        new("Two lines", "First\nSecond {caret}line", [], "Two lines.json"),
    ];

    [Fact]
    public void A_template_is_found_by_its_name_whatever_its_capitals_and_its_marks_are_filled()
    {
        var (typing, error) = TemplateStep.Of("thanks | | the report", Templates);
        Assert.Null(error);
        Assert.Equal(["Dear , thank you for the report"], typing!.Lines);
        Assert.Equal(", thank you for the report".Length, typing.LeftKeys);
    }

    [Fact]
    public void The_selection_is_the_second_part()
    {
        var (typing, _) = TemplateStep.Of("Link | axKit", Templates);
        Assert.Equal(["[axKit]()"], typing!.Lines);
        Assert.Equal(1, typing.LeftKeys);
    }

    [Fact]
    public void A_template_of_several_lines_is_typed_by_lines()
    {
        var (typing, _) = TemplateStep.Of("Two lines", Templates);
        Assert.Equal(["First", "Second line"], typing!.Lines);
        Assert.Equal("line".Length, typing.LeftKeys);
    }

    [Fact]
    public void A_name_no_template_has_fails_with_the_name()
    {
        Assert.Equal("No template is called Signature", TemplateStep.Of("Signature", Templates).Error);
        Assert.Equal("A template step names a template: template <name>", TemplateStep.Of("  | x", Templates).Error);
    }
}
