using System.Text.RegularExpressions;
using AxKit.Core.Transcript;

namespace AxClaude.Core.Transcript;

/// <summary>
/// Recognises Claude Code's screen reader mode labels, chrome rows and heading-like lines. The texts and patterns
/// themselves are <see cref="ClaudeCodeTexts"/>'s; the rules are here.
/// </summary>
public static partial class LineClassifier
{
    // A list item of a reply's own markdown, which is never a heading: the reply's text, not Claude Code's interface.
    [GeneratedRegex(@"^([-*•]|\d+[.)])\s")]
    private static partial Regex ListItemRegex();

    /// <summary>Rows that belong to the interface rather than the conversation.</summary>
    public static bool IsChrome(string text) =>
        ClaudeCodeTexts.ModeLine().IsMatch(text) || ClaudeCodeTexts.Spinner().IsMatch(text) || ClaudeCodeTexts.Tip().IsMatch(text)
        || ClaudeCodeTexts.NotepadHint().IsMatch(text) || ClaudeCodeTexts.EffortHint().IsMatch(text) || ClaudeCodeTexts.BackgroundHint().IsMatch(text)
        || ClaudeCodeTexts.QueuedHint().IsMatch(text) || ClaudeCodeTexts.AnnouncementRow().IsMatch(text) || ClaudeCodeTexts.AgentRow().IsMatch(text)
        || ClaudeCodeTexts.BranchRow().IsMatch(text) || ClaudeCodeTexts.FeedbackCardHint().IsMatch(text) || ClaudeCodeTexts.ToolTimer().IsMatch(text);

    /// <summary>The key-hint row of the feedback card, <c>1 to review · 2 to send · 0 to dismiss</c> (D33).</summary>
    public static bool IsFeedbackCardHint(string text) => ClaudeCodeTexts.FeedbackCardHint().IsMatch(text);


    /// <summary>A heading /feedback puts between its drafts: <c>This session</c>, <c>Other sessions</c> (2.1.302).</summary>
    public static bool IsDraftsHeading(string text) => text is ClaudeCodeTexts.DraftsOfThisSession or ClaudeCodeTexts.DraftsOfOtherSessions;

    /// <summary>The draft's title off the first row of the feedback card (<c>Product feedback drafted: &lt;title&gt;</c>); null for any other row.</summary>
    public static string? FeedbackCardTitle(string text)
    {
        const string marker = ClaudeCodeTexts.FeedbackCardTitleMarker;
        var at = text.IndexOf(marker, StringComparison.Ordinal);
        return at < 0 ? null : text[(at + marker.Length)..];
    }

    /// <summary>A bracketed announcement row; the text inside the brackets is what Claude wants spoken.</summary>
    public static string? AnnouncementText(string text) =>
        ClaudeCodeTexts.AnnouncementRow().IsMatch(text) ? text[1..^1] : null;

    /// <summary>The status row of Claude's detailed transcript view, which takes the place of the prompt.</summary>
    public static bool IsTranscriptViewRow(string text) => ClaudeCodeTexts.TranscriptViewRow().IsMatch(text);

    /// <summary>A prompt row with nothing typed: <c>$</c>, <c>&gt;</c> or <c>!</c> alone.</summary>
    public static bool IsBarePrompt(string text) =>
        text.Trim() is [ClaudeCodeTexts.PromptDollar or ClaudeCodeTexts.PromptAngle or ClaudeCodeTexts.PromptBang];

    /// <summary>The hint under a queued message; the rows above it up to the <c>you:</c> row are the queued message.</summary>
    public static bool IsQueuedHint(string text) => ClaudeCodeTexts.QueuedHint().IsMatch(text);

    public static bool IsNotepadHint(string text) => ClaudeCodeTexts.NotepadHint().IsMatch(text);

    public static string? SpinnerText(string text) =>
        ClaudeCodeTexts.Spinner().Match(text) is { Success: true } m ? m.Groups[1].Value + ClaudeCodeTexts.Ellipsis : null;

    /// <summary>The first row of Claude's banner, "Claude Code v2.1.309".</summary>
    public static bool IsBannerTop(string text) =>
        text.StartsWith(ClaudeCodeTexts.BannerStart, StringComparison.Ordinal) && text.Length > ClaudeCodeTexts.BannerStart.Length
        && char.IsAsciiDigit(text[ClaudeCodeTexts.BannerStart.Length]);

    /// <summary>The prompt row, or the draft being typed on it.</summary>
    public static bool IsPromptRow(string text) =>
        text.Length == 0
        || (text[0] is ClaudeCodeTexts.PromptDollar or ClaudeCodeTexts.PromptAngle or ClaudeCodeTexts.PromptBang && (text.Length == 1 || char.IsWhiteSpace(text[1])));

    /// <summary>
    /// The echo of a draft on the prompt row. Claude prints the prompt as a dollar sign followed by a no-break space,
    /// which also tells it apart from a command line quoted in a reply ("$ printf ...").
    /// </summary>
    public static bool IsDraftRow(string text) => text.Length > 2 && text[0] == ClaudeCodeTexts.PromptDollar && text[1] == ClaudeCodeTexts.DraftSpace;

    public static bool IsCompletionRow(string text) => ClaudeCodeTexts.CompletionRow().IsMatch(text);

    public static bool IsCompletionContinuation(string text) => ClaudeCodeTexts.CompletionContinuation().IsMatch(text);

    public static string? ModeText(string text)
    {
        var m = ClaudeCodeTexts.ModeLine().Match(text);
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>
    /// The background work a mode line counts ("1 shell", or "1 shell, 2 agents"), "see /tasks" when it only says
    /// "↓ to manage" (a background agent), or null when it shows none or is not a mode line. A feedback draft the
    /// mode line counts ("1 feedback draft") is no work running: it waits in /feedback and is left out.
    /// </summary>
    public static string? BackgroundText(string text)
    {
        var m = ClaudeCodeTexts.ModeLine().Match(text);
        // Runs at every frame end: no allocation unless the line counts something.
        if (!m.Success)
        {
            return null;
        }

        if (!m.Groups[2].ValueSpan.ContainsAnyInRange('0', '9'))
        {
            return m.Groups[2].ValueSpan.Contains(ClaudeCodeTexts.ManageHint, StringComparison.Ordinal) ? "see /tasks" : null;
        }

        var counts = m.Groups[2].Value.Split(ClaudeCodeTexts.PartSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(part => char.IsAsciiDigit(part[0]) && !part.Contains(ClaudeCodeTexts.FeedbackDraft, StringComparison.Ordinal))
            .ToList();
        return counts.Count > 0 ? string.Join(", ", counts) : null;
    }

    /// <summary>
    /// The feedback drafts a mode line counts ("1 feedback draft", "2 feedback drafts"): queued by Claude Code's
    /// SendFeedback tool, waiting in /feedback (D33). Zero when it counts none or is not a mode line.
    /// </summary>
    public static int FeedbackDrafts(string text)
    {
        var m = ClaudeCodeTexts.ModeLine().Match(text);
        if (!m.Success)
        {
            return 0;
        }

        var at = m.Groups[2].ValueSpan.IndexOf(ClaudeCodeTexts.FeedbackDraftAfterCount, StringComparison.Ordinal);
        if (at < 0)
        {
            return 0;
        }

        var start = at;
        while (start > 0 && char.IsAsciiDigit(m.Groups[2].ValueSpan[start - 1]))
        {
            start--;
        }

        return int.TryParse(m.Groups[2].ValueSpan[start..at], out var count) ? count : 0;
    }

    /// <summary>A row Claude prints when it waits for an answer: a permission or trust question, a numbered menu, a yes-or-no question, the /config list (§4.4).</summary>
    public static bool IsPromptText(string text) =>
        text.StartsWith(ClaudeCodeTexts.PermissionRequired, StringComparison.Ordinal)
        || text.StartsWith(ClaudeCodeTexts.EnterSelection, StringComparison.Ordinal)
        || text.StartsWith(ClaudeCodeTexts.EnterYesNo, StringComparison.Ordinal)
        || text.StartsWith(ClaudeCodeTexts.SelectWithNumbers, StringComparison.Ordinal)
        || text.StartsWith(ClaudeCodeTexts.EnterNumberToChange, StringComparison.Ordinal)
        || IsTextPrompt(text);

    /// <summary>The row where Claude waits for the user's own words: "Enter text for option 4 (Other), or Escape for the list:".</summary>
    public static bool IsTextPrompt(string text) => text.StartsWith(ClaudeCodeTexts.EnterTextFor, StringComparison.Ordinal);

    /// <summary>The answer a text row asks the words for: "Other" in "Enter text for option 4 (Other), or Escape for the list:"; null when it names none.</summary>
    public static string? TextPromptAnswer(string text) =>
        ClaudeCodeTexts.TextPrompt().Match(text) is { Success: true } m ? m.Groups[1].Value : null;

    public static LineKind Classify(string text, bool inPromptBlock)
    {
        if (text.StartsWith(ClaudeCodeTexts.YouLabel, StringComparison.Ordinal))
        {
            return LineKind.UserEcho;
        }

        if (text.StartsWith(ClaudeCodeTexts.ClaudeLabel, StringComparison.Ordinal))
        {
            return LineKind.ClaudeReply;
        }

        if (text.StartsWith(ClaudeCodeTexts.ThinkingLabel, StringComparison.Ordinal))
        {
            return LineKind.Thinking;
        }

        if (text.StartsWith(ClaudeCodeTexts.ToolErrorLabel, StringComparison.Ordinal))
        {
            return LineKind.ToolError;
        }

        if (text.StartsWith(ClaudeCodeTexts.ToolLabel, StringComparison.Ordinal) || text.EndsWith(ClaudeCodeTexts.ExpandHint, StringComparison.Ordinal))
        {
            return LineKind.Tool;
        }

        if (text.StartsWith(ClaudeCodeTexts.ErrorLabel, StringComparison.Ordinal))
        {
            return LineKind.Error;
        }

        if (text.StartsWith(ClaudeCodeTexts.WarningLabel, StringComparison.Ordinal))
        {
            return LineKind.Warning;
        }

        if (IsPromptText(text))
        {
            return LineKind.Prompt;
        }

        if (inPromptBlock && ClaudeCodeTexts.AnswerStart().IsMatch(text))
        {
            return LineKind.PromptOption;
        }

        if (ClaudeCodeTexts.TurnSummary().IsMatch(text))
        {
            return LineKind.TurnSummary;
        }

        if (text.StartsWith(ClaudeCodeTexts.ScreenReaderModeRow, StringComparison.Ordinal))
        {
            return LineKind.System;
        }

        return LineKind.Plain;
    }

    /// <summary>
    /// Heading level of a line, or 0. Screen reader mode prints markdown headings as plain rows, so a short row
    /// inside a reply, after a blank row or on the <c>claude:</c> label row, without terminal punctuation and
    /// followed by a blank row counts as level 2. Rows outside a reply never qualify: a short tool-output row
    /// followed by a blank row is not a heading, and neither is the last line of a code block. Raw markdown
    /// hashes count anywhere.
    /// </summary>
    public static int HeadingLevel(string text, LineKind kind, string? previousText, string? nextText, bool inReply)
    {
        if (kind is not (LineKind.Plain or LineKind.ClaudeReply))
        {
            return 0;
        }

        // Spans throughout: this runs for every row on screen at every frame end.
        var t = kind == LineKind.ClaudeReply ? text.AsSpan(ClaudeCodeTexts.ClaudeLabel.Length).Trim() : text.AsSpan();

        // Raw markdown ("## Title", as inside a code block Claude quotes): the number of hashes is the level.
        var hashes = 0;
        while (hashes < t.Length && t[hashes] == '#')
        {
            hashes++;
        }

        if (hashes is >= 1 and <= 6 && hashes < t.Length && char.IsWhiteSpace(t[hashes]) && !t[hashes..].IsWhiteSpace())
        {
            return hashes;
        }

        if (kind == LineKind.Plain && (!inReply || previousText is null || previousText.Length > 0))
        {
            return 0;
        }

        if (nextText is null || nextText.Length > 0 || t.Length is 0 or > 80)
        {
            return 0;
        }

        if (".,;:?!".Contains(t[^1]) || ListItemRegex().IsMatch(t) || !ContainsLetter(t))
        {
            return 0;
        }

        return 2;
    }

    private static bool ContainsLetter(ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            if (char.IsLetter(c))
            {
                return true;
            }
        }

        return false;
    }
}
