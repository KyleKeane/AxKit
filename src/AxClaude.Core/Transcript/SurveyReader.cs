using System.Text.RegularExpressions;

namespace AxClaude.Core.Transcript;

/// <summary>
/// Claude Code's feedback surveys, drawn above the mode line while Claude waits at its own prompt (docs/axClaude/claude-screens.md,
/// "The feedback surveys"; recorded with 2.1.323 in the <c>survey</c> fixture):
/// <code>
/// ● How is Claude doing this session? (optional)
/// 1: Bad 2: Fine 3: Good 0: Dismiss
/// auto mode on (shift+tab to cycle)
/// effort: medium · /effort
/// $
/// </code>
/// A key alone answers it, without Enter, and only after a pause with nothing typed after it: a key followed by more
/// typing is the start of a message, and the survey closes. So it is a question answered by its key alone
/// (<see cref="Question.KeyOnly"/>), with Dismiss for Escape. The same shape asks "How was Claude's recollection?"
/// (with 4: Unsure) and "Can Anthropic look at your session transcript …?" (y: Yes n: No d: Don't ask again, under
/// a link row); those two are read from Claude Code's source, not recorded.
/// </summary>
public static partial class SurveyReader
{
    /// <summary>How far above the cursor the answers row may be: the mode line, the effort row, the branch row and a blank row lie between.</summary>
    private const int MaxRowsAbove = 6;

    // "1: Bad 2: Fine 3: Good 0: Dismiss", "y: Yes n: No d: Don't ask again": a key, a colon, a label, two answers at least.
    [GeneratedRegex(@"^(?:[0-9a-z]: \S[^:]*?)(?: [0-9a-z]: \S[^:]*?)+$")]
    private static partial Regex AnswersRowRegex();

    [GeneratedRegex(@"(?:^| )(?<key>[0-9a-z]): ")]
    private static partial Regex KeyRegex();

    /// <summary>
    /// Whether a row can be a survey's answers row, a cheap test for every frame: its last answer is Dismiss or
    /// Don't ask again, the ways out of every survey Claude draws.
    /// </summary>
    public static bool IsAnswersRow(string text) =>
        (text.EndsWith(" 0: Dismiss", StringComparison.Ordinal) || text.EndsWith(" d: Don't ask again", StringComparison.Ordinal))
        && AnswersRowRegex().IsMatch(text);

    /// <summary>
    /// The survey waiting above the cursor, or null. The cursor sits on Claude's bare prompt row (<c>$</c>, nothing
    /// typed: a key only answers into an empty prompt), the rows between it and the answers row are chrome, and the
    /// title row starts with Claude's bullet, <c>●</c>, directly above the answers or above one link row.
    /// </summary>
    public static Question? Read(IReadOnlyList<string> rows, int cursorRow)
    {
        if (cursorRow <= 0 || cursorRow >= rows.Count || !LineClassifier.IsBarePrompt(rows[cursorRow]))
        {
            return null;
        }

        for (var r = cursorRow - 1; r >= 1 && cursorRow - r <= MaxRowsAbove; r--)
        {
            var text = rows[r].Trim();
            if (IsAnswersRow(text))
            {
                return Survey(rows, r, text);
            }

            if (text.Length > 0 && !LineClassifier.IsChrome(text))
            {
                return null;
            }
        }

        return null;
    }

    private static Question? Survey(IReadOnlyList<string> rows, int answersRow, string answers)
    {
        // The title directly above, or above a link row ("https://code.claude.com/docs/en/data-usage#…").
        var titleRow = answersRow - 1;
        var lines = new List<string>();
        if (titleRow > 0 && rows[titleRow].Trim().StartsWith("https://", StringComparison.Ordinal))
        {
            lines.Add(rows[titleRow].Trim());
            titleRow--;
        }

        var title = rows[titleRow].Trim();
        if (!title.StartsWith("● ", StringComparison.Ordinal))
        {
            return null;
        }

        var options = Options(answers);
        var leave = options.FirstOrDefault(option => option.Key == "0") ?? options.FirstOrDefault(option => option.Key == "n");
        return new Question(title[2..].Trim(), lines, options)
        {
            FirstRow = titleRow,
            KeyOnly = true,
            EscapeKey = leave?.Key,
        };
    }

    /// <summary>The answers of a row, "1: Bad 2: Fine": each key with the text up to the next key.</summary>
    public static IReadOnlyList<QuestionOption> Options(string answers)
    {
        var matches = KeyRegex().Matches(answers);
        var options = new List<QuestionOption>();
        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : answers.Length;
            options.Add(new QuestionOption(matches[i].Groups["key"].Value, answers[start..end].Trim()));
        }

        return options;
    }
}
