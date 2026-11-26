using AxKit.Core;

namespace AxKit.Tests;

/// <summary>The command palette's lines and search (docs/axKit/SPEC.md AX-10.2, AX-10.3, AX-10.4).</summary>
public sealed class PaletteTests
{
    private static readonly IReadOnlyList<PaletteEntry> Entries =
    [
        Entry("Save, File, Ctrl+S"),
        Entry("Save as, File, Ctrl+Shift+S"),
        Entry("Larger text, Options, Text, Ctrl+Plus"),
        Entry("Smaller text, Options, Text, Ctrl+Minus"),
        Entry("Open settings file, Options, Developer"),
    ];

    private static PaletteEntry Entry(string text) => new(text, () => { });

    private static List<string> Lines(string text) => Palette.Find(Entries, text).Select(entry => entry.Text).ToList();

    [Fact]
    public void A_line_is_the_name_the_menus_the_key_and_checked()
    {
        Assert.Equal("Word wrap, Edit, Ctrl+Shift+W, checked", Palette.Text("Word wrap", ["Edit"], "Ctrl+Shift+W", true));
        Assert.Equal("Larger text, Options, Text", Palette.Text("Larger text", ["Options", "Text"], null, false));
    }

    [Fact]
    public void Every_word_counts_in_any_order_and_the_menus_too()
    {
        Assert.Equal(["Larger text, Options, Text, Ctrl+Plus"], Lines("text larger"));
        Assert.Equal(["Open settings file, Options, Developer"], Lines("developer"));
    }

    [Fact]
    public void A_line_starting_with_the_text_comes_first_and_the_menus_order_stays()
    {
        Assert.Equal(["Save, File, Ctrl+S", "Save as, File, Ctrl+Shift+S"], Lines("sa"));
        Assert.Equal(["Save, File, Ctrl+S", "Save as, File, Ctrl+Shift+S", "Open settings file, Options, Developer"], Lines("file"));
    }

    [Fact]
    public void An_empty_text_gives_every_entry_in_the_menus_order()
    {
        Assert.Equal(Entries.Select(entry => entry.Text), Lines(" "));
    }

    [Fact]
    public void What_is_said()
    {
        Assert.Equal("Save as, File, Ctrl+Shift+S, 2 of 5", Palette.Spoken(Entries[1], 1, 5));
        Assert.Equal("No commands match xyz", Palette.NoMatch(" xyz "));
    }

    /// <summary>With nothing typed (AX-10.2, 2026-11-26): the F1 text's lines, read, then the commands, which run, then the keys alone.</summary>
    [Fact]
    public void With_nothing_typed_the_list_is_the_help_then_the_commands_then_the_keys_alone()
    {
        var lines = Palette.WithNothingTyped("## How the keys go\n  A key alone moves.\n\n## In every window\n  F1: the palette\n", Entries, ["Keys with no menu item", "  F6: Next pane"]);
        Assert.Equal(
            ["How the keys go", "A key alone moves.", "In every window", "F1: the palette", Palette.CommandsLine, .. Entries.Select(entry => entry.Text), "Keys with no menu item", "F6: Next pane"],
            lines.Select(line => line.Text));
        Assert.Equal(Entries.Count, lines.Count(line => line.IsCommand));
        Assert.All(lines.Take(5), line => Assert.False(line.IsCommand));
    }
}
