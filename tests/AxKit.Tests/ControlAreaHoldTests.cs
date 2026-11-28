using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AxKit.Core;
using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The question area's rules for a key that answers (<see cref="ControlArea"/>, <see cref="KeyHold"/>;
/// docs/axKit/SPEC.md AX-6.4, "A question that arrives by itself"), on the control itself with a clock of the test's
/// own, and on the sources: an Enter within the moment after a new question is shown does nothing, whatever came
/// before; a repeat of a key held down never answers; a press the hold took is heard; and no caller can say otherwise.
///
/// The review of 2026-11-27 found the hold given up by its caller: axClaude passed "the user did not cause this",
/// which was false for the question that follows the user's own answer, and the area then cleared the three seconds
/// that ran. Enter on "1. Yes" for the first of two files, pressed twice, approved the second before NVDA had said
/// more than "Question". Every question of axKit is to start on its confirming answer (the maintainer's decision of
/// that day), which leaves these rules as the one protection; hence the two halves here.
///
/// The tests here press a key by raising the key's own event on the control. The same rules with the key messages
/// Windows sends, a repeat told by the message's bit and the focus arriving by its message, are
/// <see cref="ControlAreaKeyMessageTests"/>; a notice's are <see cref="OverlayPanelHoldTests"/> (until the third review
/// of 2026-11-27 the notice's rules were held by a search of its source alone).
/// </summary>
public sealed partial class ControlAreaHoldTests
{
    /// <summary>A clock the test moves.</summary>
    private long _now = 100_000;

    /// <summary>How often a held key was heard: the area raises the refusal at the door, and here it only counts.</summary>
    private int _heard;

    private ControlArea Area() => new(() => _now, () => _heard++);

    /// <summary>A key pressed in the control the area shows, as its window would deliver it; the event says whether the area took it.</summary>
    private static KeyEventArgs Press(ControlArea area, Keys key)
    {
        var pressed = new KeyEventArgs(key);
        typeof(Control).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(area.Current, [pressed]);
        return pressed;
    }

    /// <summary>
    /// The same key arriving as the repeat of a key held down: the key-down message Windows sends again and again
    /// carries bit 30, "the key was down already", which the shown control's own class notes with the area before the
    /// key's event (<see cref="The_areas_own_controls_hand_it_every_key_message"/> holds that it does).
    /// </summary>
    private static KeyEventArgs Repeat(ControlArea area, Keys key)
    {
        var message = Message.Create(0, 0x0100, (nint)(int)key, (nint)(1L << 30));
        typeof(ControlArea).GetMethod("NoteKey", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(area, [message]);
        return Press(area, key);
    }

    /// <summary>A character typed in the control the area shows.</summary>
    private static void Type(ControlArea area, char character) =>
        typeof(Control).GetMethod("OnKeyPress", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(area.Current, [new KeyPressEventArgs(character)]);

    [Fact]
    public void A_list_that_is_shown_takes_no_answering_key_for_its_moment()
    {
        using var area = Area();
        var chosen = 0;
        var escaped = 0;
        area.ShowList("Create file beta.txt?", ["1. Yes", "2. No"], 0, null, focus: false, hold: true, (_, _) => chosen++, () => escaped++);

        Assert.True(Press(area, Keys.Enter).Handled);
        Assert.True(Press(area, Keys.Escape).Handled);
        _now += KeyHold.DefaultMs - 1;
        Press(area, Keys.Enter);
        Press(area, Keys.Escape);
        Assert.Equal(0, chosen);
        Assert.Equal(0, escaped);

        _now += 1;
        Press(area, Keys.Enter);
        Press(area, Keys.Escape);
        Assert.Equal(1, chosen);
        Assert.Equal(1, escaped);
    }

    /// <summary>The fault itself: the answer's three seconds run, the next question's list is put in place 600 ms later, and Enter comes again.</summary>
    [Fact]
    public void An_Enter_right_after_a_new_question_is_shown_does_nothing_whatever_came_before()
    {
        using var area = Area();
        var first = 0;
        var second = 0;
        area.ShowList("Create file alpha.txt?", ["1. Yes", "2. No"], 0, null, focus: false, hold: true, (_, _) => first++, () => { });
        _now += 5_000;
        Press(area, Keys.Enter);
        Assert.Equal(1, first);
        // The window holds the keys of the list just answered while the answer is on its way (axClaude: three seconds).
        area.Hold(3_000);
        _now += 500;
        Press(area, Keys.Enter);
        Assert.Equal(1, first);

        _now += 100;
        area.ShowList("Create file beta.txt?", ["1. Yes", "2. No"], 0, null, focus: false, hold: true, (_, _) => second++, () => second++);
        // The doubled Enter, an Escape: nothing, for the new list's own moment.
        for (var waited = 0; waited < KeyHold.DefaultMs; waited += 100)
        {
            Assert.True(Press(area, Keys.Enter).Handled);
            Assert.True(Press(area, Keys.Escape).Handled);
            _now += 100;
        }

        Assert.Equal(0, second);
        Press(area, Keys.Enter);
        Assert.Equal(1, second);
    }

    /// <summary>
    /// One hold for each control, and nothing inherited: the three seconds after an answer are the answered control's
    /// own, so that a second Enter does not send it twice. The list shown next takes Enter after its own second, not
    /// after what was left of the three: held so long, every step through a list the user had already heard waited.
    /// </summary>
    [Fact]
    public void A_new_control_holds_for_its_own_moment_and_inherits_nothing()
    {
        using var area = Area();
        var chosen = 0;
        area.ShowList("Claude's settings", ["1. Theme", "2. Model"], 0, null, focus: false, hold: true, (_, _) => { }, () => { });
        _now += 5_000;
        area.Hold(3_000);
        _now += 400;
        area.ShowList("Theme", ["1. Dark", "2. Light"], 0, null, focus: false, hold: true, (_, _) => chosen++, () => { });
        _now += KeyHold.DefaultMs;
        // A second and four tenths after the key before: the three seconds were the other list's.
        Assert.False(area.Holding);
        Press(area, Keys.Enter);
        Assert.Equal(1, chosen);

        // And the hold of this one, once it was answered, is not carried on either.
        area.Hold(3_000);
        Assert.True(area.Holding);
        area.ShowField("Your answer", focus: false, hold: false, _ => chosen++, () => { });
        Assert.False(area.Holding);
    }

    /// <summary>
    /// A control the user opened with a command of their own (Go to line, the recent commands) starts no hold, and
    /// that is all its caller can say: nothing ends the hold of a control that has one.
    /// </summary>
    [Fact]
    public void A_control_the_user_opened_starts_no_hold()
    {
        using var area = Area();
        var chosen = 0;
        area.ShowList("Recent commands", ["dir", "git status"], 0, null, focus: false, hold: false, (_, _) => chosen++, () => { });
        Assert.False(area.Holding);
        Press(area, Keys.Enter);
        Assert.Equal(1, chosen);
        Assert.Equal(0, _heard);

        // A command run for an AI tool opened it: the window holds it after all (AxKitForm.RunForMcp), and that holds.
        area.HoldAsArrived();
        Press(area, Keys.Enter);
        Assert.Equal(1, chosen);
        _now += KeyHold.DefaultMs;
        Press(area, Keys.Enter);
        Assert.Equal(2, chosen);
    }

    [Fact]
    public void A_held_list_takes_no_typed_key_either()
    {
        using var area = Area();
        var letters = 0;
        var typed = 0;
        area.ShowList("Status", ["Version 1", "Discard (d)"], 0, null, focus: false, hold: true, (_, _) => { }, () => { },
            letter: _ =>
            {
                letters++;
                return true;
            },
            typed: () => typed++);

        // A screen's own key (d to discard), a digit, Space: all held, and kept from the list's own jump.
        Assert.True(Press(area, Keys.D).Handled);
        Assert.True(Press(area, Keys.D2).Handled);
        Assert.True(Press(area, Keys.NumPad2).Handled);
        Assert.True(Press(area, Keys.Space).Handled);
        Type(area, 'd');
        Assert.Equal(0, letters);
        Assert.Equal(0, typed);

        _now += KeyHold.DefaultMs;
        Press(area, Keys.D);
        Type(area, 'x');
        Assert.Equal(1, letters);
        Assert.Equal(1, typed);
    }

    /// <summary>
    /// In a field the hold stops Enter and Escape alone: what is typed only stands in the field until Enter, and the
    /// words typed right after "Your words next" must not lose their first letters.
    /// </summary>
    [Fact]
    public void A_held_field_takes_what_is_typed_and_neither_Enter_nor_Escape()
    {
        using var area = Area();
        var entered = 0;
        var escaped = 0;
        area.ShowField("You chose Other. Your answer", focus: false, hold: true, _ => entered++, () => escaped++);

        Assert.False(Press(area, Keys.S).Handled);
        Assert.False(Press(area, Keys.Space).Handled);
        Assert.False(Press(area, Keys.D1).Handled);
        Assert.True(Press(area, Keys.Enter).Handled);
        Assert.True(Press(area, Keys.Escape).Handled);
        _now += KeyHold.DefaultMs - 1;
        Press(area, Keys.Enter);
        Press(area, Keys.Escape);
        Assert.Equal(0, entered);
        Assert.Equal(0, escaped);

        _now += 1;
        Press(area, Keys.Enter);
        Assert.Equal(1, entered);

        // The words are on their way: the window holds the keys, and a second Enter does not send them twice.
        area.Hold(3_000);
        _now += 2_999;
        Press(area, Keys.Enter);
        Press(area, Keys.Escape);
        Assert.Equal(1, entered);
        Assert.Equal(0, escaped);
        _now += 1;
        Press(area, Keys.Escape);
        Assert.Equal(1, escaped);
    }

    /// <summary>
    /// A repeat is not a press. Enter held down a moment too long at the first of two questions goes on arriving, and
    /// once the second list's own second was over the next repeat would answer it, on the answer it starts on, with
    /// the question still being read. A repeat of Enter, Escape or Space never answers, held or not; the key let go
    /// and pressed again does.
    /// </summary>
    [Fact]
    public void A_repeat_of_an_answering_key_never_answers_and_a_fresh_press_does()
    {
        using var area = Area();
        var chosen = 0;
        var escaped = 0;
        area.ShowList("Create file beta.txt?", ["1. Yes", "2. No"], 0, null, focus: false, hold: true, (_, _) => chosen++, () => escaped++);
        // Long after the hold: the key is still down from before.
        _now += 10_000;
        Assert.False(area.Holding);
        for (var i = 0; i < 40; i++)
        {
            Assert.True(Repeat(area, Keys.Enter).Handled);
            Assert.True(Repeat(area, Keys.Escape).Handled);
            _now += 33;
        }

        Assert.Equal(0, chosen);
        Assert.Equal(0, escaped);
        // Without a sound: only a press is answered with the refusal.
        Assert.Equal(0, _heard);

        // Let go and pressed again: a press, which answers.
        Press(area, Keys.Enter);
        Press(area, Keys.Escape);
        Assert.Equal(1, chosen);
        Assert.Equal(1, escaped);
    }

    [Fact]
    public void A_repeat_of_Space_or_a_letter_does_nothing_in_a_list_and_the_arrows_repeat_as_they_did()
    {
        using var area = Area();
        var letters = 0;
        area.ShowList("Which fruits?", ["1. Apple", "2. Cherry"], 0, [], focus: false, hold: false, (_, _) => { }, () => { },
            letter: _ =>
            {
                letters++;
                return true;
            });
        // Space held down would tick and untick the answer over and over.
        Assert.True(Repeat(area, Keys.Space).Handled);
        // An arrow held down moves on: it is not the area's to stop. A letter of the list's own is a key that
        // answers (a screen's d to discard), and its repeat does nothing; the press does.
        Assert.False(Repeat(area, Keys.Down).Handled);
        Assert.True(Repeat(area, Keys.D).Handled);
        Assert.Equal(0, letters);
        Press(area, Keys.D);
        Assert.Equal(1, letters);

        var entered = 0;
        area.ShowField("Search the rules", focus: false, hold: false, _ => entered++, () => { });
        // In a field Space and every character are typed text, and repeat as text does; Enter does not.
        Assert.False(Repeat(area, Keys.Space).Handled);
        Assert.False(Repeat(area, Keys.A).Handled);
        Assert.True(Repeat(area, Keys.Enter).Handled);
        Assert.Equal(0, entered);
        Press(area, Keys.Enter);
        Assert.Equal(1, entered);
    }

    /// <summary>
    /// The note of a repeat is the key's own and is used once: a key event that follows without a message of its own
    /// (a control's key raised by code) counts as a press, and a new control starts without the note of the one before.
    /// </summary>
    [Fact]
    public void The_note_of_a_repeat_is_used_once_and_never_carried_to_the_next_control()
    {
        using var area = Area();
        var chosen = 0;
        area.ShowList("Recent commands", ["dir"], 0, null, focus: false, hold: false, (_, _) => chosen++, () => { });
        Repeat(area, Keys.Enter);
        Assert.Equal(0, chosen);
        Press(area, Keys.Enter);
        Assert.Equal(1, chosen);

        // A repeat of another key is noted, and the list is replaced before any event took the note.
        typeof(ControlArea).GetMethod("NoteKey", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(area, [Message.Create(0, 0x0100, (nint)(int)Keys.Enter, (nint)(1L << 30))]);
        area.ShowList("Recent commands", ["dir"], 0, null, focus: false, hold: false, (_, _) => chosen++, () => { });
        Press(area, Keys.Enter);
        Assert.Equal(2, chosen);
    }

    /// <summary>
    /// A held key is heard: each fresh press of an answering key that the hold takes raises the refusal (its sound at
    /// the door in a window, the count here), with no limit; a repeat is silent, and so is a key the hold does not take.
    /// </summary>
    [Fact]
    public void A_fresh_press_the_hold_takes_is_heard_each_time_and_a_repeat_is_not()
    {
        using var area = Area();
        area.ShowList("Create file beta.txt?", ["1. Yes", "2. No"], 0, null, focus: false, hold: true, (_, _) => { }, () => { });
        Press(area, Keys.Enter);
        Assert.Equal(1, _heard);
        Press(area, Keys.Escape);
        Press(area, Keys.Space);
        Press(area, Keys.Y);
        Press(area, Keys.D1);
        Assert.Equal(5, _heard);
        // The arrows are not held, and say nothing; a repeat of a held key says nothing either.
        Press(area, Keys.Down);
        Repeat(area, Keys.Enter);
        Repeat(area, Keys.Y);
        Assert.Equal(5, _heard);

        // After the hold a press acts and nothing is heard.
        _now += KeyHold.DefaultMs;
        Press(area, Keys.Enter);
        Assert.Equal(5, _heard);

        // In a field only Enter and Escape are held, so only they are heard; what is typed goes in.
        area.ShowField("Your answer", focus: false, hold: true, _ => { }, () => { });
        Press(area, Keys.A);
        Press(area, Keys.Space);
        Assert.Equal(5, _heard);
        Press(area, Keys.Enter);
        Press(area, Keys.Escape);
        Assert.Equal(7, _heard);
    }

    /// <summary>
    /// The repeat is read off the key message itself, which only the control that gets it sees: the area's list, its
    /// list with check boxes and its field are classes of its own that hand the area every key message before the
    /// key's event. Held here, since the tests above hand the note over themselves.
    /// </summary>
    [Fact]
    public void The_areas_own_controls_hand_it_every_key_message()
    {
        using var area = Area();
        area.ShowList("One answer", ["1. Yes"], 0, null, focus: false, hold: false, (_, _) => { }, () => { });
        AssertHandsOver(area.Current!);
        area.ShowList("Several answers", ["1. Apple"], 0, [], focus: false, hold: false, (_, _) => { }, () => { });
        Assert.IsAssignableFrom<CheckedListBox>(area.Current);
        AssertHandsOver(area.Current!);
        area.ShowField("Your answer", focus: false, hold: false, _ => { }, () => { });
        Assert.IsAssignableFrom<AxKitTextBox>(area.Current);
        AssertHandsOver(area.Current!);

        static void AssertHandsOver(Control shown)
        {
            var handles = shown.GetType().GetMethod("ProcessKeyMessage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.True(handles?.DeclaringType == shown.GetType() && shown.GetType().DeclaringType == typeof(ControlArea),
                $"{shown.GetType().Name} does not hand its key messages to the area: a repeat would be taken for a press.");
        }

        var source = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "ControlArea.cs"));
        Assert.Equal(3, source.Split("area.NoteKey(m);").Length - 1);
    }

    /// <summary>What makes a held key heard in a window is the kind Refused, raised at the door (bundle rule 13): the one line the tests' own counter stands in for.</summary>
    [Fact]
    public void A_held_key_is_heard_as_the_kind_Refused()
    {
        var shared = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "AnswerKeys.cs"));
        Assert.Contains("public static void HeldBack() => EventDoor.Raise(EventKind.Refused);", shared, StringComparison.Ordinal);
    }

    /// <summary>
    /// The callers allowed <c>hold: false</c>, by file, with how many calls and why: a control the user opened with a
    /// command of their own a moment ago, whose first keys are meant for it. Nothing a program asks, and nothing that
    /// follows an answer, belongs here: add a caller only when its control cannot appear without the user's own key
    /// for it, and never one that answers a question.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (int Calls, string Why)> OpenedByTheUser = new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase)
    {
        [Path.Combine("src", "AxKit.Forms", "TextEditorForm.cs")] = (2, "Go to line (Ctrl+G) and the list of an editor's offer, each opened by its command, the digits typed at once"),
        [Path.Combine("src", "AxTerminal", "TerminalForm.Input.cs")] = (1, "the recent commands (Ctrl+R): Enter only puts the command into the field"),
    };

    [GeneratedRegex(@"\.Show(?:List|Field)\(")]
    private static partial Regex Shows();

    [GeneratedRegex(@"\bhold:\s*(true|false)\b")]
    private static partial Regex HoldNamed();

    /// <summary>
    /// Read off the sources, as <see cref="ListTests"/> reads them: every call that puts a control into a question
    /// area names its hold in the call, <c>hold: true</c> or <c>hold: false</c>, never a value worked out (axClaude's
    /// was "the user did not act lately"), and <c>hold: false</c> stands only where the table above allows it.
    /// </summary>
    [Fact]
    public void Every_control_put_into_a_question_area_holds_unless_its_caller_is_listed()
    {
        var root = SourceTexts.RepositoryRoot();
        var offenders = new List<string>();
        var unheld = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            if (relative.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relative.EndsWith(Path.Combine("AxKit.Forms", "ControlArea.cs"), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var calls = Shows().Matches(text);
            for (var i = 0; i < calls.Count; i++)
            {
                var start = calls[i].Index;
                var lineStart = text.LastIndexOf('\n', start) + 1;
                if (text[lineStart..start].TrimStart().StartsWith("//", StringComparison.Ordinal))
                {
                    // A comment may name the method.
                    continue;
                }

                // The call's own arguments: up to the next call, or a few lines.
                var end = Math.Min(text.Length, i + 1 < calls.Count ? calls[i + 1].Index : start + 600);
                var named = HoldNamed().Match(text, start, Math.Min(end, start + 600) - start);
                var line = text[..start].Count(c => c == '\n') + 1;
                if (!named.Success)
                {
                    offenders.Add($"{relative}:{line} does not name its hold in the call (hold: true)");
                }
                else if (named.Groups[1].Value == "false")
                {
                    unheld[relative] = unheld.GetValueOrDefault(relative) + 1;
                }
            }
        }

        foreach (var (file, calls) in unheld)
        {
            if (!OpenedByTheUser.TryGetValue(file, out var allowed) || allowed.Calls != calls)
            {
                offenders.Add($"{file} shows {calls} controls with hold: false, and the table allows {(OpenedByTheUser.TryGetValue(file, out var known) ? known.Calls : 0)}");
            }
        }

        foreach (var (file, allowed) in OpenedByTheUser)
        {
            if (!unheld.ContainsKey(file))
            {
                offenders.Add($"{file} is listed for {allowed.Calls} controls with hold: false and has none: take it off the list");
            }
        }

        Assert.True(offenders.Count == 0,
            "A control in a question area must hold its answering keys for a moment after it is shown (AxKit.Core/KeyHold, SPEC AX-6.4): " + string.Join("; ", offenders));
    }

    /// <summary>A command run for an AI tool may open a control whose own code says hold: false, as for the user's key: the window holds it all the same.</summary>
    [Fact]
    public void A_control_that_a_command_run_for_an_AI_tool_opened_is_held()
    {
        var text = File.ReadAllText(Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms", "AxKitForm.Mcp.cs"));
        var run = text.IndexOf("private void RunForMcp(", StringComparison.Ordinal);
        Assert.True(run >= 0, "RunForMcp is gone: where is a control that an AI tool's run opened held now?");
        var click = text.IndexOf("ClickFor(CommandOrigin.AiTool", run, StringComparison.Ordinal);
        var hold = text.IndexOf("area.HoldAsArrived();", run, StringComparison.Ordinal);
        Assert.True(click > run && hold > click && hold - click < 900, "After the click for an AI tool, RunForMcp holds the keys of every control area whose control the click changed.");
    }

    /// <summary>
    /// The field of a conversation window (axClaude's message field, axTerminal's command field), read off the source,
    /// since the window has no life in a test; the hold itself is played on the stand-in by
    /// tests/scripts/axClaude/66-question-while-reading.txt, and a repeat is a key no script can press. A repeat of
    /// Enter or Ctrl+Enter is taken before any command or handler; the field's hold is asked where the key is read
    /// and where the field sends; and the hold starts in the one place the window gives the field the focus back
    /// from a control that arrived by itself, never after one the user opened.
    /// </summary>
    [Fact]
    public void A_conversation_windows_field_sends_for_no_repeat_and_waits_when_a_question_gave_it_the_focus()
    {
        var folder = Path.Combine(SourceTexts.RepositoryRoot(), "src", "AxKit.Forms");
        var window = File.ReadAllText(Path.Combine(folder, "ConversationForm.cs")).ReplaceLineEndings("\n");
        var keys = window[window.IndexOf("protected override bool ProcessCmdKey(ref Message msg, Keys keyData)", StringComparison.Ordinal)..];
        keys = keys[..keys.IndexOf("return base.ProcessCmdKey(ref msg, keyData);", StringComparison.Ordinal)];
        Assert.Contains("if (keyData is Keys.Enter or BundleKeys.MainAction && Input.Focused && !NoticeOpen)", keys, StringComparison.Ordinal);
        var repeat = keys.IndexOf("if (AnswerKeys.IsRepeat(msg))\n            {\n                return true;", StringComparison.Ordinal);
        var held = keys.IndexOf("if (!FieldTakesEnter())\n            {\n                return true;", StringComparison.Ordinal);
        Assert.True(repeat > 0 && held > repeat, "a repeat is dropped first, then a press inside the field's hold, which is heard");

        var close = window[window.IndexOf("protected virtual void CloseArea()", StringComparison.Ordinal)..];
        close = close[..close.IndexOf("Area.Clear();", StringComparison.Ordinal)];
        Assert.Contains("var arrived = Area.Arrived;", close, StringComparison.Ordinal);
        Assert.Contains("if (focus && arrived)\n        {\n            FocusFieldByItself();", close, StringComparison.Ordinal);
        // The hold starts nowhere else but there, where an AI's request gives the focus back, and where a notice that
        // held its keys gives it back (the fourth review of 2026-11-27: a doubled Enter on a notice sent the field).
        Assert.Equal(1, window.Split("_fieldHold.Start(").Length - 1);
        Assert.Equal(3, window.Split("FocusFieldByItself();").Length - 1);
        Assert.Contains("if (held && ReferenceEquals(target, Input))\n        {\n            FocusFieldByItself();", window, StringComparison.Ordinal);
        var notices = File.ReadAllText(Path.Combine(folder, "AxKitForm.Notices.cs")).ReplaceLineEndings("\n");
        Assert.Contains("FocusBackAfterNotice(target, held, ReferenceEquals(target, back));", notices, StringComparison.Ordinal);

        var field = File.ReadAllText(Path.Combine(folder, "ConsoleForm.Field.cs")).ReplaceLineEndings("\n");
        Assert.Contains("BundleKeys.MainAction, SendFieldUnlessHeld)", field, StringComparison.Ordinal);
        Assert.Contains("e.Handled = e.SuppressKeyPress = true;\n                SendFieldUnlessHeld();", field, StringComparison.Ordinal);
        // SendField is called by the one method that asks the hold first.
        Assert.Equal(1, field.Split("            SendField();").Length - 1);
    }
}
