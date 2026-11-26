namespace AxClaude.Core.Transcript;

/// <summary>
/// One answer to a question: the key Claude takes for it (<c>1</c>, <c>2</c>, <c>y</c>, <c>n</c>), its text without
/// the key, and whether Claude marks it as the current choice (<c>(selected)</c>).
/// </summary>
public sealed record QuestionOption(string Key, string Text, bool Current = false)
{
    /// <summary>
    /// The name and value of a /config row ("Show tips: true", "Thinking mode: true (Thinking can't be turned off …)"),
    /// or null when the text has no <c>: </c>.
    /// </summary>
    public (string Name, string Value)? Setting =>
        Text.IndexOf(ClaudeCodeTexts.SettingSeparator, StringComparison.Ordinal) is var colon and > 0 ? (Text[..colon], Text[(colon + ClaudeCodeTexts.SettingSeparator.Length)..]) : null;

    /// <summary>
    /// The value of a /config switch, a setting that is true or false, which Claude flips when its number is sent; null
    /// for a setting with other values, whose number opens Claude's list of them.
    /// </summary>
    public bool? Switch => Setting?.Value switch
    {
        ClaudeCodeTexts.SettingTrue => true,
        ClaudeCodeTexts.SettingFalse => false,
        { } value when value.StartsWith(ClaudeCodeTexts.SettingTrueThen, StringComparison.Ordinal) => true,
        { } value when value.StartsWith(ClaudeCodeTexts.SettingFalseThen, StringComparison.Ordinal) => false,
        _ => null,
    };
}

/// <summary>
/// A question Claude waits to have answered (the workspace trust dialog, a permission prompt, a picker such as
/// <c>/model</c>, a question of Claude's own): its title, the lines between the title and the answers, the answers,
/// and the key hints Claude prints under the question (<c>s to use this session only</c>).
/// </summary>
public sealed record Question(string Title, IReadOnlyList<string> Text, IReadOnlyList<QuestionOption> Options)
{
    /// <summary>
    /// Several answers may be given at once, their numbers separated by commas ("1,3", typed one keystroke each; the
    /// same text in one write is a paste, which Claude ignores). The answer list in the control area shows check boxes for them (D33).
    /// </summary>
    public bool AllowsSeveral { get; init; }

    /// <summary>The screen row the question starts on (its title), where the app marks it in the conversation.</summary>
    public int FirstRow { get; init; }

    /// <summary>
    /// Where the question stands when Claude asks several in one call: its number and how many there are, read off
    /// the tab row above it (the first tab not yet ticked); null for a single question and for the review.
    /// </summary>
    public (int Number, int Count)? Step { get; init; }

    /// <summary>
    /// Claude Code shows "Review your answers" once this question is answered: it is the last of several in one call,
    /// or a question alone that takes several answers, which has a Submit step of its own (the wrapped-answers
    /// fixture, 2.1.282: the tab row "←   ☐ Next work   ✔ Submit   →" over one question, Step 1 of 1). Since 2.1.319
    /// a question alone is drawn under "Question: Next work", which numbers nothing (Step null), and the review after
    /// such a question with several answers went to the user although Review answers was off (2026-11-14). A
    /// question alone with one answer has no review (the one-question fixture).
    /// </summary>
    public bool ReviewFollows => Step is (int number, int count) ? number == count : AllowsSeveral;

    /// <summary>
    /// Answered by its key alone, with no Enter after it: Claude's feedback surveys at its own prompt
    /// (<see cref="SurveyReader"/>), where Enter would go to the prompt instead.
    /// </summary>
    public bool KeyOnly { get; init; }

    /// <summary>The key Escape stands for, an answer of its own (Dismiss on a survey); null when Escape is sent as Escape.</summary>
    public string? EscapeKey { get; init; }

    /// <summary>
    /// A permission question's core, its tool and target in a few words ("Write permission-test.txt"), which every
    /// answer's label carries in the control area (<see cref="PermissionCore"/>, SPEC.md D33); null for any other
    /// question. Not part of the <see cref="Signature"/>, and never sent: an answer goes as its key.
    /// </summary>
    public string? Core { get; init; }

    /// <summary>/config's list of settings, waiting on "Enter a number to change [1-44], or Escape to save and close:".</summary>
    public bool IsSettings => Title.StartsWith(ClaudeCodeTexts.EnterNumberToChange, StringComparison.Ordinal);

    /// <summary>What tells one question from the next: the title and the answers. The same question redrawn keeps it.</summary>
    public string Signature => Title + "\n" + string.Join("\n", Options.Select(option => option.Key + ". " + option.Text));

    /// <summary>Typed text that answers the question: one of its keys, or, when several answers are allowed, keys separated by spaces or commas.</summary>
    public bool Accepts(string text)
    {
        var keys = text.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        return keys.Length > 0 && (keys.Length == 1 || AllowsSeveral) && keys.All(key => OptionFor(key) is not null);
    }

    /// <summary>The answer a key stands for, or null. Keys compare without regard to case.</summary>
    public QuestionOption? OptionFor(string key) =>
        Options.FirstOrDefault(option => string.Equals(option.Key, key, StringComparison.OrdinalIgnoreCase));
}
