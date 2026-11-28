using System.Reflection;
using System.Windows.Forms;
using AxKit.Core;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// A notice's rules for a key that answers (<see cref="OverlayPanel"/>, <see cref="KeyHold"/>; docs/axKit/SPEC.md
/// AX-6.4), on the panel itself with its buttons, a clock of the test's own and the key messages Windows sends
/// (<see cref="KeyMessages"/>). The panel stands in a form that is no window of its own, so nothing comes on screen;
/// the form is there because it is the window that presses its default button for Enter and its cancel button for
/// Escape, which is where the fault was: it does so for the keys with Shift as well, and the panel looked for the
/// plain keys alone, so Shift+Enter and Shift+Escape answered a notice inside its hold, and as repeats (the third
/// review of 2026-11-27). Held now where the keys are read, with Shift or without, and once more where a button
/// runs, whatever pressed it.
/// </summary>
public sealed class OverlayPanelHoldTests : IDisposable
{
    /// <summary>A clock the test moves.</summary>
    private long _now = 100_000;

    /// <summary>How often a held press was heard.</summary>
    private int _heard;

    /// <summary>The texts of the buttons that ran, in order.</summary>
    private readonly List<string> _chosen = [];

    private readonly Form _window = new() { TopLevel = false };
    private readonly OverlayPanel _panel;

    public OverlayPanelHoldTests()
    {
        _panel = new OverlayPanel(() => _now, () => _heard++);
        _panel.Chosen += choice => _chosen.Add(choice.Text);
        _window.Controls.Add(_panel);
    }

    public void Dispose() => _window.Dispose();

    /// <summary>The notice in the panel as the window base shows it: the panel on view, Enter and Escape on its default and cancel buttons.</summary>
    private void Show(Notice notice)
    {
        _panel.Populate(notice);
        _panel.Visible = true;
        // No window of its own: it is parked, and nothing comes on screen.
        _window.Visible = true;
        _window.AcceptButton = _panel.DefaultButton;
        _window.CancelButton = _panel.CancelButton;
        Assert.NotEqual(0, _panel.Body.Handle);
    }

    private static Notice Question() => new("Open axTask?", "An AI tool asks to open axTask.",
    [
        new OverlayChoice("&No", IsDefault: true, IsCancel: true),
        new OverlayChoice("&Open"),
    ]);

    private static Notice TwoWays() => Notice.Ask("Update", "A new version.", "&Install", () => { }, "&Later");

    private Button ButtonOf(string text) => AllControls(_panel).OfType<Button>().First(button => button.Text == text);

    private static IEnumerable<Control> AllControls(Control parent) =>
        parent.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(AllControls(child)));

    /// <summary>The gate: a button pressed by code, as a click, a letter with Alt or UI Automation's Invoke presses it, does nothing inside the hold, and is heard.</summary>
    [Fact]
    public void A_button_pressed_inside_the_hold_does_nothing_whatever_pressed_it()
    {
        Show(Question().ByItself());
        ButtonOf("&Open").PerformClick();
        ButtonOf("&No").PerformClick();
        _panel.PressDefault();
        _panel.PressMain();
        // The button's own letter, as the character Alt and the letter type.
        KeyMessages.Type(_panel.Body, 'o', alt: true);
        Assert.Empty(_chosen);
        Assert.Equal(5, _heard);

        _now += KeyHold.DefaultMs;
        ButtonOf("&Open").PerformClick();
        Assert.Equal(["&Open"], _chosen);
        Assert.Equal(5, _heard);
    }

    /// <summary>
    /// The fault itself (M2): Shift+Enter and Shift+Escape, which the window takes for Enter and Escape. Inside the
    /// hold they answer nothing and are heard, as the plain keys; after it a press of either answers.
    /// </summary>
    [Theory]
    [InlineData(Keys.Enter, "&Install")]
    [InlineData(Keys.Shift | Keys.Enter, "&Install")]
    [InlineData(Keys.Escape, "&Later")]
    [InlineData(Keys.Shift | Keys.Escape, "&Later")]
    public void Enter_and_Escape_with_Shift_or_without_wait_for_the_hold(Keys key, string button)
    {
        Show(TwoWays().ByItself());
        Assert.True(KeyMessages.Press(_panel.Body, key));
        _now += KeyHold.DefaultMs - 1;
        Assert.True(KeyMessages.Press(_panel.Body, key));
        Assert.Empty(_chosen);
        Assert.Equal(2, _heard);

        _now += 1;
        KeyMessages.Press(_panel.Body, key);
        Assert.Equal([button], _chosen);
        Assert.Equal(2, _heard);
    }

    /// <summary>A repeat is not a press: none of the keys that answer does anything as the repeat of a key held down, held or not, and none is heard.</summary>
    [Theory]
    [InlineData(Keys.Enter)]
    [InlineData(Keys.Shift | Keys.Enter)]
    [InlineData(Keys.Escape)]
    [InlineData(Keys.Shift | Keys.Escape)]
    [InlineData(Keys.Control | Keys.Enter)]
    [InlineData(Keys.Alt | Keys.I)]
    public void A_repeat_of_a_key_that_answers_never_answers_a_notice(Keys key)
    {
        Show(TwoWays().ByItself());
        for (var i = 0; i < 60; i++)
        {
            Assert.True(KeyMessages.Press(_panel.Body, key, repeat: true));
            _now += 33;
        }

        Assert.Empty(_chosen);
        Assert.Equal(0, _heard);

        // The same holds for a notice the user opened, which has no hold at all.
        Show(TwoWays().ByUser());
        Assert.True(KeyMessages.Press(_panel.Body, key, repeat: true));
        Assert.Empty(_chosen);
    }

    /// <summary>Space on a button presses it, so it is held and its repeat is dropped; in the text it is no answer and is left alone.</summary>
    [Fact]
    public void Space_is_held_on_a_button_and_left_alone_in_the_text()
    {
        Show(TwoWays().ByItself());
        Assert.True(KeyMessages.Press(ButtonOf("&Install"), Keys.Space));
        Assert.Equal(1, _heard);
        Assert.False(KeyMessages.Press(_panel.Body, Keys.Space));
        Assert.Equal(1, _heard);
        _now += KeyHold.DefaultMs;
        Assert.True(KeyMessages.Press(ButtonOf("&Install"), Keys.Space, repeat: true));
        Assert.Empty(_chosen);
    }

    /// <summary>
    /// The hold counts again from when the focus comes back into the notice (R1): the window became the active one
    /// again, or the focus entered the panel from outside it. Only for a notice that arrived by itself.
    /// </summary>
    [Fact]
    public void A_notice_that_arrived_by_itself_holds_again_when_the_focus_comes_back()
    {
        Show(TwoWays().ByItself());
        _now += 60_000;
        KeyMessages.Press(_panel.Body, Keys.Enter, repeat: true);
        typeof(Control).GetMethod("OnEnter", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_panel, [EventArgs.Empty]);
        Assert.True(KeyMessages.Press(_panel.Body, Keys.Enter));
        Assert.Empty(_chosen);
        Assert.Equal(1, _heard);
        _now += KeyHold.DefaultMs;
        KeyMessages.Press(_panel.Body, Keys.Enter);
        Assert.Equal(["&Install"], _chosen);

        // A notice the user opened starts no hold when shown and none when the focus comes back.
        _chosen.Clear();
        _now += 60_000;
        Show(TwoWays().ByUser());
        typeof(Control).GetMethod("OnEnter", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_panel, [EventArgs.Empty]);
        KeyMessages.Press(_panel.Body, Keys.Enter);
        Assert.Equal(["&Install"], _chosen);
        Assert.Equal(1, _heard);
    }

    /// <summary>A letter the notice gives a meaning (axClaude's side answer: c copies) is heard and not asked about while the hold runs, and never asked about as a repeat.</summary>
    [Fact]
    public void A_letter_of_the_notices_own_waits_for_the_hold_and_never_repeats()
    {
        var letters = new List<char>();
        Show((Notice.Plain("Side answer", "Forty-two.") with
        {
            Letter = letter =>
            {
                letters.Add(letter);
                return letter == 'c';
            },
        }).ByItself());
        KeyMessages.Press(_panel.Body, Keys.C);
        Assert.Empty(letters);
        Assert.Equal(1, _heard);
        _now += KeyHold.DefaultMs;
        KeyMessages.Press(_panel.Body, Keys.C, repeat: true);
        Assert.Empty(letters);
        KeyMessages.Press(_panel.Body, Keys.C);
        Assert.Equal(['c'], letters);
        Assert.Equal(1, _heard);
    }

    /// <summary>
    /// Whether a notice arrived by itself (its chime) cannot be handed a value worked out (M5): the one way to say
    /// it is <see cref="Notice.ByItself"/>, the property has no setter a caller can reach, and no file of the sources
    /// writes it. Until that day three places set it from whether the user had acted lately or had asked for the
    /// check, and a screen of Claude Code's that followed the user's own key had no hold. Since the fourth review of
    /// that day the hold is every notice's but one the user opened (<see cref="NoticeHoldTests"/>).
    /// </summary>
    [Fact]
    public void Whether_a_notice_holds_is_never_a_value_worked_out()
    {
        var property = typeof(Notice).GetProperty(nameof(Notice.Unprompted))!;
        Assert.False(property.SetMethod?.IsPublic ?? false, "Notice.Unprompted can be set by a caller again: the hold of a notice must not be a value worked out.");
        Assert.False(new Notice("t", "x", [OverlayChoice.Close]).Unprompted);
        Assert.True(Notice.Plain("t", "x").ByItself().Unprompted);
        // Whatever is changed about a notice afterwards, the chime included, it stays one that arrived by itself.
        Assert.True((Notice.Plain("t", "x").ByItself() with { Silent = true }).Unprompted);

        var root = SourceTexts.RepositoryRoot();
        var writers = Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.EndsWith("OverlayPanel.cs", StringComparison.OrdinalIgnoreCase))
            .Where(file => File.ReadAllText(file).Contains("Unprompted =", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(root, file));
        Assert.Empty(writers);
    }
}
