using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The key test's words (docs/axKit/SPEC.md AX-8.12): what one key does in a window, from the window's commands and
/// the bundle's keys, and the run of a test: Escape ending it, a modifier alone.
/// </summary>
public sealed class KeyTestTests
{
    /// <summary>The bundle's keys as the window base hands them: the ones it handles itself apply, the others do not.</summary>
    private static readonly KeyTestCommand[] Bundle =
    [
        new("F9", "Say the status"),
        new("F4", "Send a report"),
        new("Ctrl+S", "Save"),
        new("Ctrl+F", "Find", Applies: false),
        new("Ctrl+Up", "Paragraph up", Applies: false),
        new("Ctrl+Shift+Down", "Select to the paragraph below", Applies: false),
        new("PageDown", "Page down", Applies: false),
        new("Ctrl+Enter", "The main action", Applies: false),
    ];

    private static KeyTestPress Press(string text, int code, bool ctrl = false, bool shift = false, bool alt = false) => new(code, ctrl, shift, alt, text);

    private static string Line(KeyTestPress press, KeyTestFocus focus, params KeyTestCommand[] commands) => KeyTest.Describe(press, commands, Bundle, focus);

    [Fact]
    public void A_bundle_key_the_base_handles_is_named_by_its_command_wherever_the_focus_is()
    {
        Assert.Equal("F9: Say the status", Line(Press("F9", 0x78), KeyTestFocus.Text));
        Assert.Equal("F4: Send a report", Line(Press("F4", 0x73), KeyTestFocus.List));
        // Even over a command of the window on the same key: the base takes the key first.
        Assert.Equal("Ctrl+S: Save", Line(Press("Ctrl+S", 'S', ctrl: true), KeyTestFocus.Other, new KeyTestCommand("Ctrl+S", "Something else")));
    }

    [Fact]
    public void A_window_key_is_named_by_the_first_command_that_acts_now()
    {
        var commands = new[]
        {
            new KeyTestCommand("Ctrl+B", "Bold", Applies: false),
            new KeyTestCommand("Ctrl+B", "What runs in the background"),
            new KeyTestCommand("Ctrl+B", "Never reached"),
        };
        Assert.Equal("Ctrl+B: What runs in the background", Line(Press("Ctrl+B", 'B', ctrl: true), KeyTestFocus.Text, commands));
        // A number row key's menu text ends in a full stop (AX-8.5); the key is the same.
        Assert.Equal("Ctrl+1: Go to the message field", Line(Press("Ctrl+1", '1', ctrl: true), KeyTestFocus.Text, new KeyTestCommand("Ctrl+1.", "Go to the message field")));
        Assert.Equal("Alt+H: opens the Help menu", Line(Press("Alt+H", 'H', alt: true), KeyTestFocus.Text, new KeyTestCommand("Alt+H", "opens the Help menu")));
    }

    /// <summary>KeyTests' overrides: Ctrl+Up is the item apps' previous section in the navigator, and the bundle's paragraph key in a text.</summary>
    [Fact]
    public void A_window_key_in_a_bundle_keys_place_is_the_windows_where_it_acts_and_the_bundles_elsewhere()
    {
        var press = Press("Ctrl+Up", 0x26, ctrl: true);
        Assert.Equal("Ctrl+Up: Previous section", Line(press, KeyTestFocus.List, new KeyTestCommand("Ctrl+Up", "Previous section")));
        Assert.Equal("Ctrl+Up: Paragraph up", Line(press, KeyTestFocus.Text, new KeyTestCommand("Ctrl+Up", "Previous section", Applies: false)));
        Assert.Equal("Ctrl+Up: Paragraph up", Line(press, KeyTestFocus.ReadOnlyText));
        Assert.Equal("Ctrl+Shift+Down: Select to the paragraph below", Line(Press("Ctrl+Shift+Down", 0x28, ctrl: true, shift: true), KeyTestFocus.Text));
        // A list has no paragraphs, and a field of one line none either.
        Assert.Equal("Ctrl+Up: no command of this window", Line(press, KeyTestFocus.List));
        Assert.Equal("Ctrl+Up: moves in the text", Line(press, KeyTestFocus.OneLineText));
        // A bundle key that only a command gives a meaning says nothing it cannot know.
        Assert.Equal("Ctrl+F: no command of this window", Line(Press("Ctrl+F", 'F', ctrl: true), KeyTestFocus.List));
        Assert.Equal("Ctrl+F: Find", Line(Press("Ctrl+F", 'F', ctrl: true), KeyTestFocus.List, new KeyTestCommand("Ctrl+F", "Find")));
    }

    [Theory]
    [InlineData("A", 'A', false, KeyTestFocus.Text, "A: types the letter")]
    [InlineData("Shift+A", 'A', true, KeyTestFocus.OneLineText, "Shift+A: types the letter")]
    [InlineData("Space", 0x20, false, KeyTestFocus.Text, "Space: types a space")]
    [InlineData("5", '5', false, KeyTestFocus.Text, "5: types the character")]
    [InlineData("Comma", 0xBC, false, KeyTestFocus.Text, "Comma: types the character")]
    [InlineData("A", 'A', false, KeyTestFocus.ReadOnlyText, "A: no command of this window")]
    [InlineData("A", 'A', false, KeyTestFocus.List, "A: finds the next item that starts with what you type")]
    [InlineData("A", 'A', false, KeyTestFocus.Other, "A: no command of this window")]
    [InlineData("Down", 0x28, false, KeyTestFocus.List, "Down: moves in the list")]
    [InlineData("Down", 0x28, false, KeyTestFocus.Text, "Down: moves in the text")]
    [InlineData("Down", 0x28, false, KeyTestFocus.Other, "Down: moves in the list or the text")]
    [InlineData("Shift+Right", 0x27, true, KeyTestFocus.Text, "Shift+Right: selects text")]
    [InlineData("PageDown", 0x22, false, KeyTestFocus.ReadOnlyText, "PageDown: moves in the text")]
    [InlineData("Enter", 0x0D, false, KeyTestFocus.Text, "Enter: makes a new line")]
    [InlineData("Enter", 0x0D, false, KeyTestFocus.OneLineText, "Enter: no command of this window")]
    [InlineData("Enter", 0x0D, false, KeyTestFocus.Button, "Enter: presses the button")]
    [InlineData("Backspace", 0x08, false, KeyTestFocus.Text, "Backspace: deletes the character before the caret")]
    [InlineData("Delete", 0x2E, false, KeyTestFocus.ReadOnlyText, "Delete: no command of this window")]
    [InlineData("Space", 0x20, false, KeyTestFocus.CheckBox, "Space: checks or unchecks the box")]
    [InlineData("Tab", 0x09, false, KeyTestFocus.List, "Tab: moves to the next control")]
    [InlineData("Shift+Tab", 0x09, true, KeyTestFocus.Text, "Shift+Tab: moves to the previous control")]
    public void A_key_of_the_focused_control_gets_a_plain_line(string text, int code, bool shift, KeyTestFocus focus, string expected)
    {
        Assert.Equal(expected, Line(Press(text, code, shift: shift), focus));
    }

    [Fact]
    public void The_edit_keys_of_a_text_are_named_and_only_where_they_act()
    {
        Assert.Equal("Ctrl+V: pastes", Line(Press("Ctrl+V", 'V', ctrl: true), KeyTestFocus.Text));
        Assert.Equal("Ctrl+V: no command of this window", Line(Press("Ctrl+V", 'V', ctrl: true), KeyTestFocus.ReadOnlyText));
        Assert.Equal("Ctrl+C: copies the selected text", Line(Press("Ctrl+C", 'C', ctrl: true), KeyTestFocus.ReadOnlyText));
        Assert.Equal("Ctrl+C: no command of this window", Line(Press("Ctrl+C", 'C', ctrl: true), KeyTestFocus.List));
        // A command of the window on the key comes first.
        Assert.Equal("Ctrl+C: Copy the entry", Line(Press("Ctrl+C", 'C', ctrl: true), KeyTestFocus.List, new KeyTestCommand("Ctrl+C", "Copy the entry")));
    }

    [Fact]
    public void A_command_on_Tab_or_Enter_comes_before_the_controls_own_meaning()
    {
        Assert.Equal("Tab: Go to the fields", Line(Press("Tab", 0x09), KeyTestFocus.List, new KeyTestCommand("Tab", "Go to the fields")));
        Assert.Equal("Enter: Send", Line(Press("Enter", 0x0D), KeyTestFocus.Text, new KeyTestCommand("Enter", "Send")));
    }

    [Fact]
    public void The_window_says_a_controls_own_key_where_it_knows_it()
    {
        Assert.Equal("H: moves to the next heading", KeyTest.Describe(Press("H", 'H'), [], Bundle, KeyTestFocus.ReadOnlyText, "moves to the next heading"));
        // A command still comes first.
        Assert.Equal("H: Hide", KeyTest.Describe(Press("H", 'H'), [new KeyTestCommand("H", "Hide")], Bundle, KeyTestFocus.ReadOnlyText, "moves to the next heading"));
    }

    [Fact]
    public void An_unknown_key_is_no_command_of_this_window()
    {
        Assert.Equal("F11: no command of this window", Line(Press("F11", 0x7A), KeyTestFocus.Text));
        Assert.Equal("Ctrl+Alt+Y: no command of this window", Line(Press("Ctrl+Alt+Y", 'Y', ctrl: true, alt: true), KeyTestFocus.Text));
        Assert.Equal("Escape: no command of this window", Line(Press("Escape", 0x1B), KeyTestFocus.List));
    }

    [Fact]
    public void Windows_own_keys_say_what_they_would_do()
    {
        Assert.Equal("Alt+F4: closes this window", Line(Press("Alt+F4", 0x73, alt: true), KeyTestFocus.Text));
        Assert.Equal("F10: opens the menu bar", Line(Press("F10", 0x79), KeyTestFocus.Text));
        Assert.Equal("Shift+F10: opens the Context menu", Line(Press("Shift+F10", 0x79, shift: true), KeyTestFocus.List));
        Assert.Equal("Apps: opens the Context menu", Line(Press("Apps", 0x5D), KeyTestFocus.List));
        Assert.Equal("Ctrl+Enter: the main action; there is none where you are", Line(Press("Ctrl+Enter", 0x0D, ctrl: true), KeyTestFocus.List));
        Assert.Equal("Ctrl+Enter: Send", Line(Press("Ctrl+Enter", 0x0D, ctrl: true), KeyTestFocus.Text, new KeyTestCommand("Ctrl+Enter", "Send")));
    }

    [Fact]
    public void A_modifier_alone_is_named_once_when_it_is_released_and_not_with_another_key()
    {
        var test = new KeyTestSession();
        Assert.Equal(((string?)null, false), test.Down(Press("Alt", 0x12, alt: true), "Alt", () => "never"));
        Assert.Equal("Alt alone: opens the menu bar", test.Up(0x12));
        Assert.Null(test.Up(0x12));

        // Ctrl, B, B up, Ctrl up: the combination's line and nothing for the modifier.
        Assert.Equal(((string?)null, false), test.Down(Press("Ctrl", 0x11, ctrl: true), "Ctrl", () => "never"));
        Assert.Equal(("Ctrl+B: Bold", false), test.Down(Press("Ctrl+B", 'B', ctrl: true), "Ctrl", () => "Ctrl+B: Bold"));
        Assert.Null(test.Up('B'));
        Assert.Null(test.Up(0x11));

        // Two modifiers together: named as held, once.
        test.Down(Press("Ctrl", 0x11, ctrl: true), "Ctrl", () => "never");
        test.Down(Press("Shift", 0x10, ctrl: true, shift: true), "Ctrl+Shift", () => "never");
        Assert.Equal("Ctrl+Shift alone: no command of this window", test.Up(0x10));
        Assert.Null(test.Up(0x11));

        test.Down(Press("Windows", 0x5B), "Windows", () => "never");
        Assert.Equal("Windows key alone: opens the Start menu, Windows' own key", test.Up(0x5B));
    }

    [Fact]
    public void Escape_ends_at_once_and_is_never_named()
    {
        var escape = Press("Escape", 0x1B);
        Assert.Equal((KeyTest.Ended, true), new KeyTestSession().Down(escape, string.Empty, () => "never"));

        var test = new KeyTestSession();
        Assert.Equal(("A: types the letter", false), test.Down(Press("A", 'A'), string.Empty, () => "A: types the letter"));
        // Shift+Escape, Ctrl+Escape and Alt+Escape are keys of their own, said and not ending the test.
        Assert.Equal(("Shift+Escape: Interrupt", false), test.Down(Press("Shift+Escape", 0x1B, shift: true), "Shift", () => "Shift+Escape: Interrupt"));
        Assert.Equal(("Ctrl+Escape: no command of this window", false), test.Down(Press("Ctrl+Escape", 0x1B, ctrl: true), "Ctrl", () => "Ctrl+Escape: no command of this window"));
        // A modifier held before Escape and released: Escape alone still ends it.
        test.Down(Press("Shift", 0x10, shift: true), "Shift", () => "never");
        test.Up(0x10);
        Assert.Equal((KeyTest.Ended, true), test.Down(escape, string.Empty, () => "never"));
    }

    [Fact]
    public void The_texts_are_the_specifications()
    {
        Assert.Equal("Key test. Press any key to hear its name and what it does here. Press Escape to end.", KeyTest.Started);
        Assert.Equal("Key test ended", KeyTest.Ended);
    }
}
