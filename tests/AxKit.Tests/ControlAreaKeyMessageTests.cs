using System.Reflection;
using System.Windows.Forms;
using AxKit.Core;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The question area's rules for a key that answers (<see cref="ControlArea"/>, <see cref="KeyHold"/>;
/// docs/axKit/SPEC.md AX-6.4), played with the key messages Windows sends (<see cref="KeyMessages"/>) to the area's
/// own control, which has a handle and no window on screen: a press and a repeat are told apart by the message's own
/// bit, the focus arrives by the message that says so, and the clock is the test's. <see cref="ControlAreaHoldTests"/>
/// holds the same rules on the key's event, with the callers' table. The third review of 2026-11-27 found what these
/// play: the hold ended at the very moment the window put the focus on a question (rule 3 of <see cref="KeyHold"/>),
/// a repeat of a screen's own letter answered again, and a held list swallowed Shift with a letter and a punctuation
/// mark without a sound.
/// </summary>
public sealed class ControlAreaKeyMessageTests
{
    /// <summary>A clock the test moves.</summary>
    private long _now = 100_000;

    /// <summary>How often a held key was heard.</summary>
    private int _heard;

    private ControlArea Area() => new(() => _now, () => _heard++);

    /// <summary>The control the area shows, with its handle made, so that it takes messages.</summary>
    private static Control Shown(ControlArea area)
    {
        var control = area.Current!;
        Assert.NotEqual(0, control.Handle);
        return control;
    }

    /// <summary>
    /// The fault itself (H1). A question arrives while the focus is in the conversation; the window puts the focus on
    /// its list a second later, which was the moment its hold ended, and the Enter the user typed in the conversation
    /// at that moment answered it. The hold now counts again from when the focus arrives.
    /// </summary>
    [Fact]
    public void The_hold_counts_again_from_when_the_focus_arrives()
    {
        using var area = Area();
        var chosen = 0;
        var escaped = 0;
        area.ShowList("Create file beta.txt?", ["1. Yes", "2. No"], 0, null, focus: false, hold: true, (_, _) => chosen++, () => escaped++);
        var list = Shown(area);

        // A second after it was shown its first moment is over, and the window moves the focus onto it.
        _now += KeyHold.DefaultMs;
        Assert.False(area.Holding);
        KeyMessages.Focus(list);
        Assert.True(area.Holding);
        KeyMessages.Press(list, Keys.Enter);
        KeyMessages.Press(list, Keys.Escape);
        _now += KeyHold.DefaultMs - 1;
        KeyMessages.Press(list, Keys.Enter);
        Assert.Equal(0, chosen);
        Assert.Equal(0, escaped);
        Assert.Equal(3, _heard);

        // A second after the focus came, the same key answers.
        _now += 1;
        KeyMessages.Press(list, Keys.Enter);
        Assert.Equal(1, chosen);
        Assert.Equal(3, _heard);
    }

    /// <summary>
    /// Whoever moves the focus: a notice that closes over the question, the window becoming the active one, the
    /// user's own focus key. Each arrival starts the moment again, and never shortens a hold that runs longer.
    /// </summary>
    [Fact]
    public void Every_arrival_of_the_focus_starts_the_moment_again_and_shortens_nothing()
    {
        using var area = Area();
        var entered = 0;
        area.ShowField("You chose Other. Your answer", focus: false, hold: true, _ => entered++, () => { });
        var field = Shown(area);
        _now += 60_000;
        KeyMessages.Focus(field);
        _now += 500;
        KeyMessages.Focus(field);
        _now += KeyHold.DefaultMs - 1;
        KeyMessages.Press(field, Keys.Enter);
        Assert.Equal(0, entered);
        _now += 1;
        KeyMessages.Press(field, Keys.Enter);
        Assert.Equal(1, entered);

        // The answer is on its way for three seconds; the focus coming back meanwhile leaves those three seconds.
        area.Hold(3_000);
        _now += 100;
        KeyMessages.Focus(field);
        _now += 2_899;
        KeyMessages.Press(field, Keys.Enter);
        Assert.Equal(1, entered);
        _now += 1;
        KeyMessages.Press(field, Keys.Enter);
        Assert.Equal(2, entered);
    }

    /// <summary>
    /// A control the user opened with a command of their own (Go to line, the recent commands) starts no hold when
    /// shown and none when the focus arrives: its first keys are meant for it, and ordinary use must not wait.
    /// </summary>
    [Fact]
    public void A_control_the_user_opened_starts_no_hold_when_the_focus_arrives_either()
    {
        using var area = Area();
        var chosen = 0;
        area.ShowList("Recent commands", ["dir", "git status"], 0, null, focus: false, hold: false, (_, _) => chosen++, () => { });
        var list = Shown(area);
        Assert.False(area.Arrived);
        KeyMessages.Focus(list);
        Assert.False(area.Holding);
        KeyMessages.Press(list, Keys.Enter);
        Assert.Equal(1, chosen);
        Assert.Equal(0, _heard);

        // Opened by a command run for an AI tool, it counts as arrived by itself: held now, and again at the focus.
        area.HoldAsArrived();
        Assert.True(area.Arrived);
        _now += KeyHold.DefaultMs;
        Assert.False(area.Holding);
        KeyMessages.Focus(list);
        KeyMessages.Press(list, Keys.Enter);
        Assert.Equal(1, chosen);
        Assert.Equal(1, _heard);
        _now += KeyHold.DefaultMs;
        KeyMessages.Press(list, Keys.Enter);
        Assert.Equal(2, chosen);

        // Whether the control shown arrived by itself ends with it.
        area.Clear();
        Assert.False(area.Arrived);
    }

    /// <summary>
    /// A repeat is not a press, read off the message itself: Enter, Escape, Space, a screen's own letter and a digit
    /// that arrive with "the key was down already" do nothing, long after any hold, and say nothing; the same key let
    /// go and pressed again acts (L1: a repeat of a screen's letter sent its key again until that day).
    /// </summary>
    [Fact]
    public void A_repeat_of_any_key_that_answers_does_nothing_and_a_press_does()
    {
        using var area = Area();
        var chosen = 0;
        var escaped = 0;
        var letters = 0;
        area.ShowList("Status", ["Version 1", "Discard (d)"], 0, [], focus: false, hold: true, (_, _) => chosen++, () => escaped++,
            letter: key =>
            {
                letters++;
                return key == 'd';
            });
        var list = (CheckedListBox)Shown(area);
        _now += 60_000;
        for (var i = 0; i < 30; i++)
        {
            KeyMessages.Press(list, Keys.Enter, repeat: true);
            KeyMessages.Press(list, Keys.Escape, repeat: true);
            KeyMessages.Press(list, Keys.Space, repeat: true);
            KeyMessages.Press(list, Keys.D, repeat: true);
            KeyMessages.Press(list, Keys.D2, repeat: true);
            KeyMessages.Press(list, Keys.Shift | Keys.D, repeat: true);
            _now += 33;
        }

        Assert.Equal(0, chosen);
        Assert.Equal(0, escaped);
        Assert.Equal(0, letters);
        Assert.Empty(list.CheckedIndices);
        Assert.Equal(0, _heard);

        // The arrows repeat as they always did.
        KeyMessages.Press(list, Keys.Down, repeat: true);
        Assert.Equal(1, list.SelectedIndex);

        KeyMessages.Press(list, Keys.D);
        // A press of Space is let through, and its character ticks the answer (a repeat's key message is taken, so
        // that its character never comes).
        KeyMessages.Press(list, Keys.Space);
        KeyMessages.Type(list, ' ');
        KeyMessages.Press(list, Keys.Enter);
        KeyMessages.Press(list, Keys.Escape);
        Assert.Equal(1, letters);
        Assert.Single(list.CheckedIndices);
        Assert.Equal(1, chosen);
        Assert.Equal(1, escaped);
    }

    /// <summary>
    /// In a field a repeat of Enter or Escape does nothing either, and what is typed repeats as text does.
    /// </summary>
    [Fact]
    public void In_a_field_a_repeat_of_Enter_sends_nothing_and_typed_characters_repeat()
    {
        using var area = Area();
        var entered = new List<string>();
        area.ShowField("Search the rules", focus: false, hold: false, entered.Add, () => entered.Add("escape"));
        var field = (TextBox)Shown(area);
        KeyMessages.Type(field, 'a');
        KeyMessages.Type(field, 'a', repeat: true);
        KeyMessages.Press(field, Keys.Enter, repeat: true);
        KeyMessages.Press(field, Keys.Escape, repeat: true);
        Assert.Empty(entered);
        KeyMessages.Press(field, Keys.Enter);
        Assert.Equal(["aa"], entered);
    }

    /// <summary>
    /// One rule for what a held list takes: Enter, Escape, Space and everything typed, with Shift or without, a
    /// punctuation mark included, each heard at a press and silent as a repeat. The keys that only move are not held.
    /// (L8: Shift with a letter and a punctuation mark were swallowed in silence by the list of Claude's question.)
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Whatever_a_held_list_takes_is_heard(bool readsTheQuestion)
    {
        using var area = Area();
        var typed = 0;
        area.ShowList("Create file beta.txt?", ["1. Yes", "2. No", "3. ? Other"], 0, null, focus: false, hold: true, (_, _) => typed += 100, () => typed += 100,
            typed: readsTheQuestion ? () => typed++ : null);
        var list = (ListBox)Shown(area);

        KeyMessages.Press(list, Keys.Enter);
        KeyMessages.Press(list, Keys.Escape);
        KeyMessages.Press(list, Keys.Space);
        KeyMessages.Press(list, Keys.Y);
        KeyMessages.Press(list, Keys.Shift | Keys.Y);
        KeyMessages.Press(list, Keys.D3);
        KeyMessages.Press(list, Keys.Shift | Keys.D3);
        Assert.Equal(7, _heard);
        // A punctuation mark is no key the key's own event knows: its character comes, and is held and heard there.
        KeyMessages.Press(list, Keys.OemQuestion);
        KeyMessages.Type(list, '?');
        Assert.Equal(8, _heard);
        // As a repeat it is silent, and still does nothing.
        KeyMessages.Type(list, '?', repeat: true);
        Assert.Equal(8, _heard);
        Assert.Equal(0, typed);
        Assert.Equal(0, list.SelectedIndex);

        // An arrow moves, held or not, and says nothing.
        KeyMessages.Press(list, Keys.Down);
        Assert.Equal(1, list.SelectedIndex);
        Assert.Equal(8, _heard);

        // After the moment a typed character reads the question again where the list has that, once for a press.
        _now += KeyHold.DefaultMs;
        KeyMessages.Press(list, Keys.OemQuestion);
        KeyMessages.Type(list, '?');
        KeyMessages.Type(list, '?', repeat: true);
        Assert.Equal(readsTheQuestion ? 1 : 0, typed);
        Assert.Equal(8, _heard);
    }

    /// <summary>
    /// The gate every answer passes after its key was read (R2, <c>ControlArea.Takes</c>): held, it is heard and
    /// says no; it says no for a control that is no longer the one shown; otherwise yes. The reading of the keys
    /// holds them first, so no key reaches the gate held; it is there for the key path that was not thought of.
    /// </summary>
    [Fact]
    public void The_gate_of_an_answer_refuses_while_the_hold_runs_and_for_a_control_that_is_gone()
    {
        using var area = Area();
        area.ShowList("Create file beta.txt?", ["1. Yes", "2. No"], 0, null, focus: false, hold: true, (_, _) => { }, () => { });
        var first = area.Current!;
        var takes = typeof(ControlArea).GetMethod("Takes", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.False((bool)takes.Invoke(area, [first])!);
        Assert.Equal(1, _heard);
        _now += KeyHold.DefaultMs;
        Assert.True((bool)takes.Invoke(area, [first])!);
        Assert.Equal(1, _heard);

        // Another control takes its place while one is kept back for the test: the one before answers nothing.
        using var other = new ListBox();
        Assert.False((bool)takes.Invoke(area, [other])!);
        Assert.Equal(1, _heard);
    }

    /// <summary>
    /// Every callback the area hands an answer to stands behind that gate, read off the source, since a key cannot
    /// reach the gate held: the list's choice, its Escape, its letter, its typed character, and a field's Enter and
    /// Escape. <c>Fill</c>, which enters the text an AI tool's run carries into the field that run opened, is the
    /// one call that passes none, and says so.
    /// </summary>
    [Fact]
    public void Every_answer_of_the_area_is_handed_on_behind_the_gate()
    {
        var source = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "ControlArea.cs")).ReplaceLineEndings("\n");
        Assert.Contains("if (list.SelectedIndex >= 0 && Takes(list))\n                {\n                    choose(", source, StringComparison.Ordinal);
        Assert.Contains("if (Takes(list))\n                {\n                    escape();", source, StringComparison.Ordinal);
        Assert.Contains("&& (!Takes(list) || letter(", source, StringComparison.Ordinal);
        Assert.Contains("if (!repeat && Takes(list))\n            {\n                typed();", source, StringComparison.Ordinal);
        Assert.Contains("if (Takes(field))\n                {\n                    enter(field.Text);", source, StringComparison.Ordinal);
        Assert.Contains("if (Takes(field))\n                {\n                    escape();", source, StringComparison.Ordinal);
        // No other place calls a callback: one call each of the list's four, and for a field its two and Fill's.
        Assert.Equal(1, source.Split("choose(list.SelectedIndex").Length - 1);
        Assert.Equal(2, source.Split("escape();").Length - 1);
        Assert.Equal(1, source.Split("letter((char)").Length - 1);
        Assert.Equal(1, source.Split("typed();").Length - 1);
        Assert.Equal(1, source.Split("enter(field.Text);").Length - 1);
        Assert.Equal(1, source.Split("enter(text);").Length - 1);
    }
}
