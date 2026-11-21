using System.Globalization;
using System.Text.Json;
using AxClaude.Core.Transcript;
using AxKit.Core;

namespace AxClaude.Core.Streaming;

/// <summary>
/// One question Claude Code asked through its permission tool, as a window asks the user
/// (docs/axClaude/stream-mode.md SM-7.4 to SM-7.6), and the answer that goes back through the relay. Two kinds. A tool
/// call that needs the user's word is one question with No where the list starts, then Yes once, Yes for the rest of
/// the session, and an answer that only explains: it opens everything Claude asks to do and answers nothing. A
/// question of Claude's own is its questions one after another, each with its choices and Other for the user's own
/// words, answered as a whole after the last. The words to the user are axClaude's own and the caution's are
/// <see cref="AiCaution"/>'s (bundle rule 11); what Claude Code is told is <see cref="ClaudeCodeStream"/>'s. Pure:
/// the window shows <see cref="Current"/>, or asks for the words of <see cref="WordsPrompt"/>, and hands back what
/// the user chose.
/// </summary>
public sealed class StreamAsking
{
    /// <summary>The keys of a tool call's question, in the list's order: no is first, where a list starts.</summary>
    public const string RefuseKey = "1";
    public const string OnceKey = "2";
    public const string AlwaysKey = "3";
    public const string ExplainKey = "4";

    /// <summary>Who asks, as the caution names it.</summary>
    public const string Who = "Claude";

    /// <summary>After what a tool call is about, when the question's title holds only its start.</summary>
    public const string MoreWords = ", and more, which the explanation shows";

    private readonly StreamAsk _ask;

    /// <summary>The folder Claude works in, with its separator at the end, by which a file the call names is shortened; empty when not known.</summary>
    private readonly string _folder;

    /// <summary>The answers given so far to Claude's own questions, by each question's text.</summary>
    private readonly Dictionary<string, string> _answers = new(StringComparer.Ordinal);

    /// <summary>Which of Claude's own questions is asked now.</summary>
    private int _at;

    /// <summary>The choices ticked beside Other, kept while the user's own words are awaited.</summary>
    private List<string> _chosen = [];

    /// <param name="folder">The folder Claude works in, for shortening a file the call names; null when not known.</param>
    public StreamAsking(StreamAsk ask, string? folder = null)
    {
        _ask = ask;
        _folder = string.IsNullOrEmpty(folder) ? string.Empty : folder.TrimEnd('\\', '/') + "\\";
        Current = ask.IsQuestion ? Own(0) : Permission();
    }

    /// <summary>The tool Claude Code asked about, for the log: a tool's name, never what it was asked to do.</summary>
    public string Tool => _ask.Tool;

    /// <summary>The question the window shows now.</summary>
    public Question Current { get; private set; }

    /// <summary>
    /// While the user's own words are awaited (Other was chosen): the row Claude Code's screen shows for them, so
    /// that the window names the answer as it does with the screen; null otherwise.
    /// </summary>
    public string? WordsPrompt { get; private set; }

    /// <summary>Whether these keys only open the explanation of a tool call: nothing is answered by them.</summary>
    public bool Explains(string keys) => !_ask.IsQuestion && keys.Trim() == ExplainKey;

    /// <summary>The title of the explanation's text.</summary>
    public string ExplainTitle => $"What {Who} asks to do";

    /// <summary>
    /// Everything Claude asks to do, to be read before it is allowed: the caution first, then the tool and each
    /// member of the call's input by Claude Code's own name for it, a text on its own lines.
    /// </summary>
    public string ExplainText
    {
        get
        {
            var lines = new List<string> { AiCaution.ToolCall(Who), string.Empty, "Tool: " + Shown(_ask.Tool) };
            if (_ask.Input.ValueKind == JsonValueKind.Object)
            {
                foreach (var member in _ask.Input.EnumerateObject())
                {
                    var value = member.Value.ValueKind == JsonValueKind.String ? member.Value.GetString() ?? string.Empty : member.Value.GetRawText();
                    var parts = value.Replace("\r\n", "\n").Split('\n');
                    if (parts.Length == 1)
                    {
                        lines.Add($"{Shown(member.Name)}: {Shown(parts[0])}");
                    }
                    else
                    {
                        lines.Add(Shown(member.Name) + ":");
                        lines.AddRange(parts.Select(Shown));
                    }
                }
            }

            return string.Join("\n", lines);
        }
    }

    /// <summary>
    /// One line of what Claude asks, as it is put into a control: every character that is not seen or heard itself
    /// (a control character, a zero-width or direction character) written out as its code, a tab kept. A text box
    /// ends its text at a character of code 0, so that without this the explanation could stop before the part that
    /// matters, while a yes ran all of it (the 2026-11-21 audit).
    /// </summary>
    private static string Shown(string line) =>
        line.Contains('\t', StringComparison.Ordinal) ? string.Join('\t', line.Split('\t').Select(DisplayText.Visible)) : DisplayText.Visible(line);

    /// <summary>
    /// Takes what the user chose: the key of an answer ("2"), several keys where several may be chosen ("1,3"), or,
    /// while <see cref="WordsPrompt"/> is set, the user's own words. Gives the answer for the relay when the whole
    /// is answered; null while more follows (<see cref="Current"/> or <see cref="WordsPrompt"/> has changed) and for
    /// what is no answer here, which changes nothing.
    /// </summary>
    public string? Answer(string given)
    {
        if (!_ask.IsQuestion)
        {
            return given.Trim() switch
            {
                RefuseKey => Refusal(),
                OnceKey => ClaudeCodeStream.AllowAnswer(_ask.Input),
                AlwaysKey => ClaudeCodeStream.AlwaysAnswer(_ask.Input, _ask.Tool),
                _ => null,
            };
        }

        var asked = _ask.Questions[_at];
        if (WordsPrompt is not null)
        {
            var words = given.Trim();
            if (words.Length == 0)
            {
                return null;
            }

            WordsPrompt = null;
            return Next(asked, [.. _chosen, words]);
        }

        var keys = given.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        if (keys.Length == 0 || (keys.Length > 1 && !asked.Several))
        {
            return null;
        }

        var labels = new List<string>();
        var other = false;
        foreach (var key in keys)
        {
            if (!int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 1 || number > asked.Choices.Count + 1)
            {
                return null;
            }

            if (number == asked.Choices.Count + 1)
            {
                other = true;
            }
            else if (!labels.Contains(asked.Choices[number - 1].Label))
            {
                labels.Add(asked.Choices[number - 1].Label);
            }
        }

        if (other)
        {
            _chosen = labels;
            WordsPrompt = ClaudeCodeTexts.TextPromptFor(asked.Choices.Count + 1, ClaudeCodeTexts.OtherAnswer);
            return null;
        }

        return Next(asked, labels);
    }

    /// <summary>
    /// Escape. While the user's own words are awaited, back to the question's list, and null. Else no to the whole:
    /// the answer that refuses, also for Claude's own questions (SM-7.5).
    /// </summary>
    public string? Escape()
    {
        if (WordsPrompt is not null)
        {
            WordsPrompt = null;
            _chosen = [];
            return null;
        }

        return Refusal();
    }

    /// <summary>The answer that refuses, whatever is asked: for an interrupt, and for a question the window cannot ask.</summary>
    public static string Refusal() => ClaudeCodeStream.DenyAnswer(ClaudeCodeStream.RefusedWords);

    private string? Next(AskQuestion asked, IReadOnlyList<string> answer)
    {
        _answers[asked.Text] = string.Join(", ", answer);
        _chosen = [];
        _at++;
        if (_at < _ask.Questions.Count)
        {
            Current = Own(_at);
            return null;
        }

        return ClaudeCodeStream.QuestionAnswer(_ask.Input, _answers);
    }

    /// <summary>A tool call's question: who asks and for what in its title, the caution in its text, and no first.</summary>
    private Question Permission()
    {
        var about = _folder.Length > 0 && _ask.About.StartsWith(_folder, StringComparison.OrdinalIgnoreCase) ? _ask.About[_folder.Length..] : _ask.About;
        // A command of several lines, or a long one, is named by its start alone: the title says that more follows,
        // since a yes runs all of it.
        var more = _ask.AboutCut ? MoreWords : string.Empty;
        // What the title names is shown with its hidden characters written out (a direction mark, a control
        // character), and Claude's own description of a call is said as Claude's, never as the action (rule 11).
        about = Shown(about);
        var tool = Shown(_ask.Tool);
        var named = about.Length == 0 ? $"{Who} asks to use {tool}"
            : _ask.AboutDescribes ? $"{Who} asks to use {tool}, and describes it as: {about}"
            : $"{Who} asks to use {tool}: {about}";
        return new Question(
            named + more,
            [AiCaution.Asked(Who)],
            [
                new QuestionOption(RefuseKey, "No"),
                new QuestionOption(OnceKey, "Yes, once"),
                new QuestionOption(AlwaysKey, $"Yes, and do not ask again for {_ask.Tool} in this session"),
                new QuestionOption(ExplainKey, $"Explain what {Who} asks to do"),
            ]);
    }

    /// <summary>One of Claude's own questions: its choices by number, each with Claude's word about it where that says more than its name, and Other last.</summary>
    private Question Own(int index)
    {
        var asked = _ask.Questions[index];
        var options = new List<QuestionOption>();
        for (var i = 0; i < asked.Choices.Count; i++)
        {
            var choice = asked.Choices[i];
            var says = choice.Description.Length > 0 && !string.Equals(choice.Description, choice.Label, StringComparison.OrdinalIgnoreCase);
            options.Add(new QuestionOption(Key(i + 1), Shown(says ? choice.Label + ClaudeCodeTexts.AnswerDash + choice.Description : choice.Label)));
        }

        options.Add(new QuestionOption(Key(asked.Choices.Count + 1), ClaudeCodeTexts.OtherAnswer));
        return new Question(Shown(asked.Text), [], options)
        {
            AllowsSeveral = asked.Several,
            Step = _ask.Questions.Count > 1 ? (index + 1, _ask.Questions.Count) : null,
        };
    }

    private static string Key(int number) => number.ToString(CultureInfo.InvariantCulture);
}
