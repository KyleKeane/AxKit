using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The one resolver of a command's canonical name (docs/axKit/user-workflows.md WF-1): the label, plain, and where two share it, the label with its menus.</summary>
public sealed class CommandNamesTests
{
    private static readonly CommandName[] Commands =
    [
        new("&Word wrap", "Options, Text"),
        new("Save &as...", "File"),
        new("&Heading", "Navigation, Next"),
        new("&Heading", "Navigation, Previous"),
        new("Between the text and the details", string.Empty),
        new("&Go to them at once, interrupting speech", "Options, Questions"),
        new("Not set", "Context, Action keys"),
        new("Not set", "Context, Action keys"),
    ];

    private static CommandResolution Resolve(string? name, string? menu = null) => CommandNames.Resolve(Commands, name, menu);

    [Theory]
    [InlineData("Word wrap", 0)]
    [InlineData("word WRAP", 0)]
    [InlineData("&Word wrap", 0)]
    [InlineData("Save as...", 1)]
    [InlineData("Save as", 1)]
    [InlineData("  between THE text and the details  ", 4)]
    public void A_name_is_found_by_its_label_whatever_its_case_its_letter_marker_or_its_dots(string name, int index)
    {
        var found = Resolve(name);
        Assert.Equal(CommandMatch.Found, found.Match);
        Assert.Equal(index, found.Index);
        Assert.Empty(found.Candidates);
    }

    [Fact]
    public void Equal_labels_are_ambiguous_and_the_candidates_are_the_ones_that_share_the_name()
    {
        var shared = Resolve("Heading");
        Assert.Equal(CommandMatch.Ambiguous, shared.Match);
        Assert.Equal(-1, shared.Index);
        Assert.Equal([2, 3], shared.Candidates);
        Assert.Equal("Heading", shared.Wanted);

        var found = Resolve("Heading", "Navigation, Next");
        Assert.Equal(CommandMatch.Found, found.Match);
        Assert.Equal(2, found.Index);
        Assert.Equal(3, Resolve("&heading", "navigation, previous").Index);
    }

    [Fact]
    public void The_palettes_line_without_its_key_names_a_command()
    {
        var next = Resolve("Heading, Navigation, Next");
        Assert.Equal(CommandMatch.Found, next.Match);
        Assert.Equal(2, next.Index);
        Assert.Equal(3, Resolve("heading, navigation, previous").Index);
        Assert.Equal(0, Resolve("Word wrap, Options, Text").Index);

        // The line is a name without a menu; given with one, the label alone is compared, as before.
        Assert.Equal(CommandMatch.UnknownInMenu, Resolve("Heading, Navigation, Next", "Navigation, Next").Match);
    }

    [Fact]
    public void A_label_wins_over_a_line()
    {
        CommandName[] commands = [new("A, B", "M"), new("A", "B")];
        var found = CommandNames.Resolve(commands, "A, B");
        Assert.Equal(CommandMatch.Found, found.Match);
        Assert.Equal(0, found.Index);
        Assert.Equal(1, CommandNames.Resolve(commands, "A").Index);
    }

    [Fact]
    public void A_label_that_holds_a_comma_is_found_whole()
    {
        var found = Resolve("Go to them at once, interrupting speech");
        Assert.Equal(CommandMatch.Found, found.Match);
        Assert.Equal(5, found.Index);
        Assert.Equal(5, Resolve("Go to them at once, interrupting speech, Options, Questions").Index);
    }

    [Fact]
    public void An_unknown_name_names_the_nearest_labels_at_most_five()
    {
        var wrap = Resolve("Wrap");
        Assert.Equal(CommandMatch.Unknown, wrap.Match);
        Assert.Equal(-1, wrap.Index);
        Assert.Contains(0, wrap.Candidates);

        // A name that holds a label names it too: a line with a menu the command is not in.
        Assert.Equal([2, 3], Resolve("Heading, Sideways").Candidates);

        var none = Resolve("Qqq");
        Assert.Equal(CommandMatch.Unknown, none.Match);
        Assert.Empty(none.Candidates);

        CommandName[] many = [.. Enumerable.Range(1, 8).Select(n => new CommandName($"Open {n}", "File"))];
        var open = CommandNames.Resolve(many, "Open");
        Assert.Equal(CommandMatch.Unknown, open.Match);
        Assert.Equal([0, 1, 2, 3, 4], open.Candidates);
    }

    [Fact]
    public void Commands_no_name_tells_apart_stay_ambiguous_with_their_menus()
    {
        var bare = Resolve("Not set");
        Assert.Equal(CommandMatch.Ambiguous, bare.Match);
        Assert.Equal([6, 7], bare.Candidates);

        var withMenu = Resolve("Not set", "Context, Action keys");
        Assert.Equal(CommandMatch.Ambiguous, withMenu.Match);
        Assert.Equal([6, 7], withMenu.Candidates);

        var line = Resolve("Not set, Context, Action keys");
        Assert.Equal(CommandMatch.Ambiguous, line.Match);
        Assert.Equal([6, 7], line.Candidates);
    }

    [Fact]
    public void The_canonical_name_is_the_label_alone_or_the_label_with_its_menus()
    {
        Assert.Equal("Word wrap", CommandNames.CanonicalOf(Commands, 0));
        Assert.Equal("Save as", CommandNames.CanonicalOf(Commands, 1));
        Assert.Equal("Heading, Navigation, Next", CommandNames.CanonicalOf(Commands, 2));
        Assert.Equal("Heading, Navigation, Previous", CommandNames.CanonicalOf(Commands, 3));
        Assert.Equal("Between the text and the details", CommandNames.Line(Commands[4]));
        Assert.Equal("Between the text and the details", CommandNames.CanonicalOf(Commands, 4));
        Assert.Equal("Word wrap, Options, Text", CommandNames.Line(Commands[0]));

        // Every canonical name a command can tell apart finds that command again.
        foreach (var index in new[] { 0, 1, 2, 3, 4, 5 })
        {
            Assert.Equal(index, Resolve(CommandNames.CanonicalOf(Commands, index)).Index);
        }
    }

    [Fact]
    public void A_refusal_says_what_to_write()
    {
        Assert.Equal("no command is named", CommandNames.Refusal(Resolve("  "), Commands));
        Assert.Equal("no command is named", CommandNames.Refusal(Resolve(null), Commands));
        Assert.Equal("no command \"Qqq\"", CommandNames.Refusal(Resolve("Qqq"), Commands));
        Assert.Equal("no command \"Wrap\"; nearest: Word wrap, Options, Text", CommandNames.Refusal(Resolve("Wrap"), Commands));
        Assert.Equal("no command \"Heading\" in the menu File", CommandNames.Refusal(Resolve("&Heading", "File"), Commands));
        Assert.Equal(
            "several commands are named \"Heading\": write one of: Heading, Navigation, Next; Heading, Navigation, Previous",
            CommandNames.Refusal(Resolve("&Heading"), Commands));
        Assert.Equal(
            "several commands are named \"Not set, Context, Action keys\" and the name cannot tell them apart",
            CommandNames.Refusal(Resolve("Not set"), Commands));
        Assert.Equal(string.Empty, CommandNames.Refusal(Resolve("Word wrap"), Commands));
    }
}
