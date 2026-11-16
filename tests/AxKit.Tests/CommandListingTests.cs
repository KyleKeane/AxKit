using System.Text.Json;
using AxKit.Core;
using AxKit.Core.Mcp;

namespace AxKit.Tests;

/// <summary>
/// A window's commands as data (docs/axKit/user-workflows.md WF-2.3 to WF-2.5, section 5.2): the rows made from the
/// menu bar's entries and the command list's keys, the chord that would run each now, and the one line the automation
/// channel answers <c>commands data</c> with, written and read back. The rows here are axDown's and the item window's
/// as their sources make them.
/// </summary>
public sealed class CommandListingTests
{
    private static MenuCommand Item(string label, string menu, string key = "", string keyText = "", string mark = "change", bool? isChecked = null, bool ai = true) =>
        new(label, menu, key, keyText, mark, isChecked, ai);

    /// <summary>A command of the window's list with its key: one made with a menu item (<paramref name="keyOnly"/> false) or a key alone.</summary>
    private static KeyCommand Key(string label, string key, bool applies = true, bool keyOnly = true, string keyText = "", string mark = "open") =>
        new(label, key, keyText, mark, applies, keyOnly);

    private static ListedCommand Row(IReadOnlyList<ListedCommand> rows, string label) => Assert.Single(rows, row => row.Label == label);

    [Fact]
    public void A_menu_item_is_a_row_with_its_key_and_its_menus()
    {
        var rows = CommandListing.Rows(
            [
                Item("Undo", "Actions", "ctrl+z", "Ctrl+Z"),
                Item("Heading", "Navigation, Next", "ctrl+h", "Ctrl+H", "read"),
                Item("Word wrap", "Options, Text", "ctrl+shift+w", "Ctrl+Shift+W", "change", isChecked: true),
                Item("User guide", "Help", mark: "read"),
                Item("Let AI tools run commands", "axKit, Claude, AI tools", ai: false),
            ],
            [Key("Undo", "ctrl+z", keyOnly: false), Key("Heading", "ctrl+h", keyOnly: false), Key("Word wrap", "ctrl+shift+w", keyOnly: false)]);

        // In menu order, one row each.
        Assert.Equal(["Undo", "Heading", "Word wrap", "User guide", "Let AI tools run commands"], rows.Select(row => row.Label));
        var undo = rows[0];
        Assert.Equal(("Actions", "ctrl+z", "Ctrl+Z", "change"), (undo.Menu, undo.Key, undo.KeyText, undo.Mark));
        Assert.Empty(undo.Also);
        Assert.Null(undo.Checked);
        Assert.False(undo.KeyOnly);
        Assert.True(undo.Ai);
        Assert.Equal("ctrl+z", undo.Now);

        Assert.Equal("Navigation, Next", rows[1].Menu);
        Assert.Equal("read", rows[1].Mark);
        Assert.True(rows[2].Checked);
        // An item with no key has no chord, now or ever; an item withheld from the AI tools says so.
        Assert.Equal((string.Empty, string.Empty, string.Empty), (rows[3].Key, rows[3].KeyText, rows[3].Now));
        Assert.False(rows[4].Ai);
    }

    [Fact]
    public void A_key_alone_with_a_menu_items_label_and_chord_adds_nothing()
    {
        // The item window's Context menu shows Rename with F2, and the list's own key command of that name handles it.
        var rows = CommandListing.Rows(
            [Item("Rename", "Context", "f2", "F2")],
            [Key("Rename", "f2")]);

        var row = Assert.Single(rows);
        Assert.Equal("f2", row.Key);
        Assert.Empty(row.Also);
        Assert.False(row.KeyOnly);
        Assert.Equal("Context", row.Menu);
        Assert.Equal("f2", row.Now);
    }

    [Fact]
    public void A_key_alone_with_a_menu_items_label_and_another_chord_is_that_rows_other_key()
    {
        // axDown: Redo is Ctrl+Y in the Actions menu, and Ctrl+Shift+Z as a key alone. The label is compared without
        // regard to case.
        var rows = CommandListing.Rows(
            [Item("Undo", "Actions", "ctrl+z", "Ctrl+Z"), Item("Redo", "Actions", "ctrl+y", "Ctrl+Y")],
            [Key("Undo", "ctrl+z", keyOnly: false), Key("Redo", "ctrl+y", keyOnly: false), Key("redo", "ctrl+shift+z"), Key("Redo", "ctrl+shift+z")]);

        Assert.Equal(2, rows.Count);
        var redo = Row(rows, "Redo");
        Assert.Equal("ctrl+y", redo.Key);
        // Once, though two keys alone brought the chord.
        Assert.Equal(["ctrl+shift+z"], redo.Also);
        Assert.Equal("Ctrl+Y", redo.KeyText);
        Assert.False(redo.KeyOnly);
        Assert.Equal("ctrl+y", redo.Now);
        Assert.Empty(Row(rows, "Undo").Also);
    }

    [Fact]
    public void A_key_alone_goes_to_the_first_menu_item_of_its_label_unless_another_has_its_chord()
    {
        // Two items named Heading, under Next and under Previous: a key alone of that name with the second one's chord
        // is the second one's key; with a chord neither has, it is one more key of the first.
        var menus = new[] { Item("Heading", "Navigation, Next", "ctrl+h", "Ctrl+H"), Item("Heading", "Navigation, Previous", "ctrl+shift+h", "Ctrl+Shift+H") };
        var absorbed = CommandListing.Rows(menus, [Key("Heading", "ctrl+shift+h")]);
        Assert.All(absorbed, row => Assert.Empty(row.Also));

        var added = CommandListing.Rows(menus, [Key("Heading", "alt+h")]);
        Assert.Equal(["alt+h"], added[0].Also);
        Assert.Empty(added[1].Also);
        Assert.Equal(2, added.Count);
    }

    [Fact]
    public void Keys_alone_of_one_label_are_one_row_the_first_its_key()
    {
        // The editor shell: Ctrl+Tab, Ctrl+Shift+Tab and F6 each move between the text and the details.
        var rows = CommandListing.Rows(
            [Item("Undo", "Actions", "ctrl+z", "Ctrl+Z")],
            [
                Key("Undo", "ctrl+z", keyOnly: false),
                Key("Between the text and the details", "ctrl+tab", keyText: "Ctrl+Tab"),
                Key("Between the text and the details", "ctrl+shift+tab", keyText: "Ctrl+Shift+Tab"),
                Key("Between the text and the details", "f6", keyText: "F6"),
            ]);

        // After the menu rows.
        Assert.Equal(["Undo", "Between the text and the details"], rows.Select(row => row.Label));
        var row = rows[1];
        Assert.Equal("ctrl+tab", row.Key);
        Assert.Equal(["ctrl+shift+tab", "f6"], row.Also);
        Assert.Equal("Ctrl+Tab", row.KeyText);
        Assert.Equal("open", row.Mark);
        Assert.Equal(string.Empty, row.Menu);
        Assert.Null(row.Checked);
        Assert.True(row.KeyOnly);
        // A key with no menu item is never for the AI tools, which list and run menu items alone.
        Assert.False(row.Ai);
        Assert.Equal("ctrl+tab", row.Now);
    }

    [Fact]
    public void A_key_without_a_name_gives_its_key_text_and_no_chord()
    {
        // A key the chord vocabulary has no name for: the row says what the menu would show, and nothing to press.
        var rows = CommandListing.Rows([], [Key("Mute", string.Empty, keyText: "Ctrl+Scroll"), Key("Mute", "ctrl+m", keyText: "Ctrl+M")]);
        var row = Assert.Single(rows);
        Assert.Equal(string.Empty, row.Key);
        Assert.Equal("Ctrl+Scroll", row.KeyText);
        Assert.Equal(["ctrl+m"], row.Also);
        Assert.Equal("ctrl+m", row.Now);

        var alone = Assert.Single(CommandListing.Rows([], [Key("Mute", string.Empty, keyText: "Ctrl+Scroll")]));
        Assert.Empty(alone.Also);
        Assert.Equal(string.Empty, alone.Now);
    }

    [Fact]
    public void Now_is_the_first_chord_that_acts()
    {
        // A row of keys alone whose command does not apply where the focus is: no chord runs it now.
        var waiting = CommandListing.Rows([], [Key("Up a level", "backspace", applies: false), Key("Up a level", "alt+up", applies: false)]);
        Assert.Equal(string.Empty, Assert.Single(waiting).Now);

        // Its second chord applies: that one.
        var second = Assert.Single(CommandListing.Rows([], [Key("Up a level", "backspace", applies: false), Key("Up a level", "alt+up")]));
        Assert.Equal("backspace", second.Key);
        Assert.Equal(["alt+up"], second.Also);
        Assert.Equal("alt+up", second.Now);

        // Both apply: the row's own, before the further ones.
        Assert.Equal("backspace", Assert.Single(CommandListing.Rows([], [Key("Up a level", "backspace"), Key("Up a level", "alt+up")])).Now);

        // One label on one chord for two places (Find next in each of two lists): the chord acts when either does.
        var either = Assert.Single(CommandListing.Rows([], [Key("Find next", "f3", applies: false), Key("Find next", "f3")]));
        Assert.Empty(either.Also);
        Assert.Equal("f3", either.Now);
    }

    [Fact]
    public void A_chord_another_label_takes_now_does_not_act_for_a_row_of_keys_alone()
    {
        // The item window: Right is "Into the list" in the navigator and "The next value of the field" in the detail
        // pane. With the focus in the detail pane the second has the chord, in the navigator the first.
        var inDetail = CommandListing.Rows([], [Key("Into the list", "right", applies: false), Key("The next value of the field", "right")]);
        Assert.Equal(string.Empty, Row(inDetail, "Into the list").Now);
        Assert.Equal("right", Row(inDetail, "The next value of the field").Now);

        var inNavigator = CommandListing.Rows([], [Key("Into the list", "right"), Key("The next value of the field", "right", applies: false)]);
        Assert.Equal("right", Row(inNavigator, "Into the list").Now);
        Assert.Equal(string.Empty, Row(inNavigator, "The next value of the field").Now);

        // Both would act: the window runs the first registered, so the second's chord does not run it.
        var both = CommandListing.Rows([], [Key("Into the list", "right"), Key("The next value of the field", "right")]);
        Assert.Equal("right", Row(both, "Into the list").Now);
        Assert.Equal(string.Empty, Row(both, "The next value of the field").Now);
    }

    [Fact]
    public void A_menu_rows_chord_that_no_command_holds_acts()
    {
        // F1 and Ctrl+S: the window base handles them itself, no command of the list has them.
        var rows = CommandListing.Rows(
            [Item("Keyboard shortcuts", "Help", "f1", "F1", "read"), Item("Save", "Session", "ctrl+s", "Ctrl+S")],
            [Key("Find", "ctrl+f", applies: false, keyOnly: false)]);
        Assert.Equal("f1", rows[0].Now);
        Assert.Equal("ctrl+s", rows[1].Now);
    }

    [Fact]
    public void A_menu_rows_chord_whose_commands_all_wait_does_not_act()
    {
        // The item shows Delete; the commands that have the key act in the list alone, and the focus is elsewhere.
        var menus = new[] { Item("Delete", "Context", "delete", "Delete", "act") };
        var waiting = CommandListing.Rows(menus, [Key("Delete", "delete", applies: false, keyOnly: false), Key("Delete the field", "delete", applies: false)]);
        Assert.Equal(string.Empty, Row(waiting, "Delete").Now);

        // One of them acts: the item's chord acts, whatever that command is called (an item made by itself shows a
        // key that a command of another label handles: Find, and Find in the list).
        var acting = CommandListing.Rows(menus, [Key("Delete", "delete", applies: false, keyOnly: false), Key("Delete the field", "delete")]);
        Assert.Equal("delete", Row(acting, "Delete").Now);

        // Its first chord waits and a further one acts: the further one.
        var further = CommandListing.Rows(menus, [Key("Delete", "delete", applies: false, keyOnly: false), Key("Delete", "ctrl+d")]);
        Assert.Equal(["ctrl+d"], Row(further, "Delete").Also);
        Assert.Equal("ctrl+d", Row(further, "Delete").Now);
    }

    private static readonly WindowCommands Listing = new("axDown", null,
    [
        new("Undo", "Actions", "ctrl+z", [], "Ctrl+Z", "change", null, false, true, "ctrl+z"),
        new("Redo", "Actions", "ctrl+y", ["ctrl+shift+z"], "Ctrl+Y", "change", null, false, true, "ctrl+y"),
        new("Word wrap", "Options, Text", "ctrl+shift+w", [], "Ctrl+Shift+W", "change", true, false, true, "ctrl+shift+w"),
        new("Say new line", "Options, Text", string.Empty, [], string.Empty, "change", false, false, true, string.Empty),
        new("Larger text", "Options, Text", "ctrl+plus", [], "Ctrl+Plus", "change", null, false, true, "ctrl+plus"),
        new("1. Müller's \"notes\" & more.md (C:\\notes)", "Session, Open recent file", string.Empty, [], string.Empty, "open", null, false, false, string.Empty),
        new("Between the text and the details", string.Empty, "ctrl+tab", ["ctrl+shift+tab", "f6"], "Ctrl+Tab", "open", null, true, false, string.Empty),
    ]);

    private static string Fields(ListedCommand row) =>
        $"{row.Label}|{row.Menu}|{row.Key}|{string.Join(' ', row.Also)}|{row.KeyText}|{row.Mark}|{row.Checked}|{row.KeyOnly}|{row.Ai}|{row.Now}";

    [Fact]
    public void The_listing_reads_back_what_it_wrote()
    {
        var text = CommandListing.Write(Listing);
        // One line, the chords and the texts as themselves (section 5.2).
        Assert.DoesNotContain('\n', text);
        Assert.StartsWith("""{"app":"axDown","notice":null,"commands":[{"label":"Undo","menu":"Actions","key":"ctrl+z","also":[],"keyText":"Ctrl+Z","mark":"change","checked":null,"keyOnly":false,"ai":true,"now":"ctrl+z"},""", text);
        Assert.Contains("""{"label":"Redo","menu":"Actions","key":"ctrl+y","also":["ctrl+shift+z"],"keyText":"Ctrl+Y","mark":"change","checked":null,"keyOnly":false,"ai":true,"now":"ctrl+y"}""", text);
        Assert.Contains("""{"label":"Between the text and the details","menu":"","key":"ctrl+tab","also":["ctrl+shift+tab","f6"],"keyText":"Ctrl+Tab","mark":"open","checked":null,"keyOnly":true,"ai":false,"now":""}""", text);
        Assert.Contains("\"checked\":true", text);
        Assert.Contains("Müller's \\\"notes\\\" & more.md", text);

        var read = CommandListing.Read(text);
        Assert.NotNull(read);
        Assert.Equal("axDown", read.App);
        Assert.Null(read.Notice);
        Assert.Equal(Listing.Commands.Select(Fields), read.Commands.Select(Fields));
        Assert.Equal(text, CommandListing.Write(read));

        // A window that shows a notice says which, and lists what it has then.
        var held = CommandListing.Read(CommandListing.Write(new WindowCommands("axPass", "Unlock the database", [])));
        Assert.NotNull(held);
        Assert.Equal(("axPass", "Unlock the database"), (held.App, held.Notice));
        Assert.Empty(held.Commands);
    }

    [Fact]
    public void The_channels_answer_reads_as_a_listing()
    {
        // What the window sends is the text Write gave, and it reads as the rows that were written.
        var text = CommandListing.Write(new WindowCommands("axDown", null, CommandListing.Rows(
            [Item("Redo", "Actions", "ctrl+y", "Ctrl+Y")],
            [Key("Redo", "ctrl+y", keyOnly: false), Key("Redo", "ctrl+shift+z"), Key("Between the text and the details", "ctrl+tab", keyText: "Ctrl+Tab")])));
        var read = CommandListing.Read(text);
        Assert.NotNull(read);
        Assert.Equal(["Redo|Actions|ctrl+y|ctrl+shift+z|Ctrl+Y|change||False|True|ctrl+y", "Between the text and the details||ctrl+tab||Ctrl+Tab|open||True|False|ctrl+tab"], read.Commands.Select(Fields));

        // Serialized once more, as the channel serializes its other answers, the text is a JSON string and no listing:
        // the channel must send the text itself.
        var twice = JsonSerializer.Serialize(text);
        Assert.StartsWith("\"", twice);
        Assert.Null(CommandListing.Read(twice));
    }

    [Theory]
    // What a window from before this request answers: the palette's lines.
    [InlineData("""["Undo, Actions, Ctrl+Z","Redo, Actions, Ctrl+Y"]""")]
    [InlineData("[]")]
    // An error of the channel.
    [InlineData("""{"error":"unknown command commands"}""")]
    [InlineData("""{"ok":true}""")]
    // A JSON string, a text that is no JSON, nothing.
    [InlineData("\"axDown\"")]
    [InlineData("the window did not answer")]
    [InlineData("")]
    // The name without rows, rows that are no array, a row without a label, a row that is no object.
    [InlineData("""{"app":"axDown"}""")]
    [InlineData("""{"app":"axDown","commands":"none"}""")]
    [InlineData("""{"app":"axDown","commands":[{"menu":"Actions"}]}""")]
    [InlineData("""{"app":"axDown","commands":["Undo"]}""")]
    [InlineData("""{"commands":[]}""")]
    public void An_answer_that_is_no_listing_reads_as_none(string answer)
    {
        Assert.Null(CommandListing.Read(answer));
    }

    [Fact]
    public void A_row_that_lacks_a_part_reads_with_that_part_empty()
    {
        var read = CommandListing.Read("""{"app":"axDown","commands":[{"label":"Undo","also":"ctrl+z","checked":"yes"}]}""");
        Assert.NotNull(read);
        Assert.Equal("Undo|||||||False|False|", Fields(Assert.Single(read.Commands)));
    }

    [Fact]
    public void A_row_of_an_app_the_tools_never_read_is_never_for_the_tools()
    {
        // axPass and axClip: whatever the item says.
        Assert.False(CommandListing.Ai(true, Dispatch.AxPass));
        Assert.False(CommandListing.Ai(false, Dispatch.AxPass));
        Assert.False(CommandListing.Ai(true, Dispatch.AxClip));
        Assert.False(CommandListing.Ai(true, "PASS"));

        // Every other app: the item's own answer.
        foreach (var app in Dispatch.Apps.Where(app => !McpTools.NeverRead.Contains(app.Verb)))
        {
            Assert.True(CommandListing.Ai(true, app.Verb), app.Name);
            Assert.False(CommandListing.Ai(false, app.Verb), app.Name);
        }

        Assert.Equal(2, Dispatch.Apps.Count(app => !CommandListing.Ai(true, app.Verb)));
    }
}
