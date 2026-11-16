using AxAutomate.Core;
using AxKit.Core;

namespace AxAutomate.Tests;

/// <summary>
/// The converter's pure half (docs/axKit/user-workflows.md WF-10.1 to WF-10.3): which press of a <c>keys</c> line is
/// a command the window lists (<see cref="Converter.NameFor"/>) and how a script's lines read once those presses are
/// <c>do</c> lines (<see cref="Converter.Rewrite"/>). The rows are axDown's as section 5.2 of that document gives them.
/// </summary>
public sealed class ConverterTests
{
    private static ListedCommand Row(string label, string menu, string key, string now, bool keyOnly = false, string[]? also = null) =>
        new(label, menu, key, also ?? [], string.Empty, "read", null, keyOnly, !keyOnly, now);

    /// <summary>axDown's window with a file open and the focus in the text: every key acts.</summary>
    private static readonly WindowCommands InTheText = new("axDown", null,
    [
        Row("Redo", "Actions", "ctrl+y", "ctrl+y", also: ["ctrl+shift+z"]),
        Row("Find", "Navigation", "ctrl+f", "ctrl+f"),
        Row("Find next", "Navigation", "f3", "f3"),
        Row("Go to line...", "Navigation", "ctrl+g", "ctrl+g"),
        Row("Heading", "Navigation, Next", "ctrl+h", "ctrl+h"),
        Row("Heading", "Navigation, Previous", "ctrl+shift+h", "ctrl+shift+h"),
        Row("Between the text and the details", string.Empty, "ctrl+tab", "ctrl+tab", keyOnly: true, also: ["f6"]),
    ]);

    private static WindowCommands With(params ListedCommand[] more) => InTheText with { Commands = [.. InTheText.Commands, .. more] };

    [Fact]
    public void A_chord_converts_when_exactly_one_row_has_it_as_its_key_and_it_acts_now()
    {
        Assert.Equal("Find", Converter.NameFor(InTheText, "ctrl+f"));
        Assert.Equal("Find next", Converter.NameFor(InTheText, "f3"));

        // The name is the canonical one: the label plain, and with its menus where two commands share it.
        Assert.Equal("Go to line", Converter.NameFor(InTheText, "ctrl+g"));
        Assert.Equal("Heading, Navigation, Previous", Converter.NameFor(InTheText, "ctrl+shift+h"));

        // The press as a script may write it: the one order and case decide.
        Assert.Equal("Heading, Navigation, Previous", Converter.NameFor(InTheText, "Shift+Ctrl+H"));

        // A key with no menu item is a command too.
        Assert.Equal("Between the text and the details", Converter.NameFor(InTheText, "ctrl+tab"));

        // Every name is one the step do answers with the same press.
        foreach (var chord in new[] { "ctrl+f", "f3", "ctrl+g", "ctrl+h", "ctrl+shift+h", "ctrl+tab", "ctrl+y" })
        {
            Assert.Equal(chord, DoStep.Decide(InTheText, Converter.NameFor(InTheText, chord)!).Chord);
        }
    }

    [Fact]
    public void A_chord_that_is_only_another_key_of_its_command_stays()
    {
        // do Redo would press ctrl+y: the press would change.
        Assert.Null(Converter.NameFor(InTheText, "ctrl+shift+z"));
        Assert.Null(Converter.NameFor(InTheText, "f6"));

        // Also when the further key is the one that acts now.
        var further = InTheText with { Commands = [.. InTheText.Commands.Select(row => row.Label == "Redo" ? row with { Now = "ctrl+shift+z" } : row)] };
        Assert.Null(Converter.NameFor(further, "ctrl+shift+z"));
        Assert.Null(Converter.NameFor(further, "ctrl+y"));
    }

    [Fact]
    public void A_chord_two_rows_have_stays()
    {
        Assert.Null(Converter.NameFor(With(Row("Find in files", "Navigation", "ctrl+f", "ctrl+f")), "ctrl+f"));

        // Two rows of one label and one menus: the chord has one row, and its name would find two.
        Assert.Null(Converter.NameFor(With(Row("Find", "Navigation", "ctrl+shift+f", "ctrl+shift+f")), "ctrl+f"));

        // A second row that has the chord and does not act now does not count: the first is the one the press runs.
        Assert.Equal("Find", Converter.NameFor(With(Row("Find in files", "Navigation", "ctrl+f", string.Empty)), "ctrl+f"));
    }

    [Fact]
    public void A_chord_that_does_not_act_now_stays()
    {
        var inAField = InTheText with { Commands = [.. InTheText.Commands.Select(row => row with { Now = string.Empty })] };
        Assert.Null(Converter.NameFor(inAField, "ctrl+f"));
        Assert.Null(Converter.NameFor(inAField, "f3"));

        // A chord no row has, and no chord at all.
        Assert.Null(Converter.NameFor(InTheText, "ctrl+q"));
        Assert.Null(Converter.NameFor(InTheText, string.Empty));
    }

    [Fact]
    public void A_window_that_lists_nothing_converts_nothing()
    {
        // A window from before the request, a notice (its own keys are keys steps), and a list without rows.
        Assert.Null(Converter.NameFor(null, "ctrl+f"));
        Assert.Null(Converter.NameFor(InTheText with { Notice = "Close without saving?" }, "ctrl+f"));
        Assert.Null(Converter.NameFor(InTheText with { Notice = string.Empty }, "ctrl+f"));
        Assert.Null(Converter.NameFor(new WindowCommands("axDown", null, []), "ctrl+f"));
    }

    [Fact]
    public void A_converted_press_becomes_a_do_line_and_the_presses_around_it_stay_keys_in_order()
    {
        string[] lines =
        [
            "start down {temp}\\find.md",
            "keys down down ctrl+f a enter f3 f3 escape",
            "keys ctrl+g",
            "keys ctrl+h",
            "keys ctrl+f enter",
        ];
        Conversion[] conversions =
        [
            new(2, 2, "Find"), new(2, 5, "Find next"), new(2, 6, "Find next"),
            new(3, 0, "Go to line"),
            new(5, 0, "Find"),
        ];
        Assert.Equal(
        [
            "start down {temp}\\find.md",
            "keys down down",
            "do Find",
            "keys a enter",
            "do Find next",
            "do Find next",
            "keys escape",
            "do Go to line",
            "keys ctrl+h",
            "do Find",
            "keys enter",
        ], Converter.Rewrite(lines, conversions));

        // No conversion: the same lines. One that names no keys step, or no press of the line, changes nothing.
        Assert.Equal(lines, Converter.Rewrite(lines, []));
        Assert.Equal(lines, Converter.Rewrite(lines, [new(1, 0, "Find"), new(3, 1, "Find"), new(9, 0, "Find"), new(0, 0, "Find")]));
    }

    [Fact]
    public void An_iffound_stands_before_every_line_its_line_became()
    {
        string[] lines =
        [
            "try said Trust this folder?",
            "iffound keys enter ctrl+f a",
            "  iffound keys ctrl+g",
            "iffound iffound keys f3 enter",
        ];
        Assert.Equal(
        [
            "try said Trust this folder?",
            "iffound keys enter",
            "iffound do Find",
            "iffound keys a",
            "  iffound do Go to line",
            "iffound iffound do Find next",
            "iffound iffound keys enter",
        ], Converter.Rewrite(lines, [new(2, 1, "Find"), new(3, 0, "Go to line"), new(4, 0, "Find next")]));
    }

    [Fact]
    public void Comments_blank_lines_and_line_ends_stay()
    {
        // A file split at its line feeds: each line carries its carriage return, the last element is what follows
        // the last line feed.
        var text = "# Workflow: Find\r\n\r\n# keys ctrl+f is a comment\r\nkeys ctrl+f enter\r\n   \r\nkeys f3\ntype keys ctrl+f\r\n";
        var rewritten = Converter.Rewrite(text.Split('\n'), [new(3, 1, "Find"), new(4, 0, "Find"), new(6, 0, "Find next"), new(7, 0, "Find")]);
        Assert.Equal(
            "# Workflow: Find\r\n\r\n# keys ctrl+f is a comment\r\ndo Find\r\nkeys enter\r\n   \r\ndo Find next\ntype keys ctrl+f\r\n",
            string.Join('\n', rewritten));

        // Line feeds alone stay line feeds, and a last line without an end gets none.
        Assert.Equal("keys a\ndo Find\nkeys b", string.Join('\n', Converter.Rewrite("keys a ctrl+f b".Split('\n'), [new(1, 1, "Find")])));
        Assert.Equal("# c\r\nkeys a\r\ndo Find", string.Join('\n', Converter.Rewrite("# c\r\nkeys a ctrl+f".Split('\n'), [new(2, 1, "Find")])));
    }
}
