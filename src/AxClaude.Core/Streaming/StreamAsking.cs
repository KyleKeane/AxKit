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
    /// <summary>
    /// The keys of a tool call's question, in the list's order: no is first, where a list starts, and the no with the
    /// user's words why follows it (since 2026-11-26, SM-7.4), so that both refusals stand before any yes.
    /// </summary>
    public const string RefuseKey = "1";
    public const string RefuseWhyKey = "2";
    public const string OnceKey = "3";
    public const string AlwaysKey = "4";
    public const string ExplainKey = "5";

    /// <summary>The answer that refuses with the user's own words, which go back to Claude with the refusal (SM-7.4; measured 2026-11-26: Claude reads them).</summary>
    public const string RefuseWhyLabel = "No, and tell " + Who + " why";

    /// <summary>
    /// The keys of a plan's question (SM-7.7), in the list's order: the screen's three answers with running never where
    /// the list starts (bundle rule 11), so the no that keeps planning first, then the yes that still asks before each
    /// edit, then the yes with auto mode; and an answer that only opens the whole plan.
    /// </summary>
    public const string KeepPlanningKey = "1";
    public const string PlanManualKey = "2";
    public const string PlanAutoKey = "3";
    public const string PlanReadKey = "4";

    /// <summary>The plan's answers, in the screen's words where it has them ("No, keep planning", "Yes, manually approve edits", "Yes, and use auto mode").</summary>
    public const string KeepPlanningLabel = "No, keep planning, and tell " + Who + " what to change";
    public const string PlanManualLabel = "Yes, and manually approve edits";
    public const string PlanAutoLabel = "Yes, and use auto mode";

    /// <summary>The permission modes a yes to a plan goes on in, by Claude Code's own names (measured: the answer's mode is the mode in force after it).</summary>
    public const string ManualMode = "default";
    public const string AutoMode = "auto";

    /// <summary>The user's words after <see cref="RefuseWhyLabel"/> or <see cref="KeepPlanningLabel"/>, kept from the answer that sent them for the line that stays in the conversation; null before.</summary>
    public string? ToldWhy { get; private set; }

    /// <summary>The line that stays in the conversation once the refusal, or a plan sent back, went with the user's words, so that what Claude was told can be found again; null before and for every other answer.</summary>
    public string? ToldWhyLine => ToldWhy is null ? null
        : IsPlan ? $"You sent {Who}'s plan back to keep planning and told {Who}: {Shown(ToldWhy)}"
        : $"You refused {Who}'s use of {Shown(_ask.Tool)} and told {Who}: {Shown(ToldWhy)}";

    /// <summary>The question is Claude's plan for the user's approval (<see cref="ClaudeCodeStream.PlanTool"/>, SM-7.7), and no other tool call.</summary>
    public bool IsPlan => !_ask.IsQuestion && _ask.Tool == ClaudeCodeStream.PlanTool;

    /// <summary>The answer given to a plan, while it stays on record: the key of a yes; null before and for any other answer.</summary>
    private string? _planApproved;

    /// <summary>
    /// The plan's lines as a reader reads them: its Markdown made plain as a reply's is (SM-4.5), a heading without its
    /// marks, each with its hidden characters written out; empty for no plan.
    /// </summary>
    private IReadOnlyList<string> PlanLines()
    {
        var plan = _ask.Input.ValueKind == JsonValueKind.Object && _ask.Input.TryGetProperty(ClaudeCodeStream.PlanMember, out var given) && given.ValueKind == JsonValueKind.String
            ? given.GetString() ?? string.Empty
            : string.Empty;
        return plan.Trim().Length == 0 ? [] : [.. ClaudeCodeMarkdown.Plain(plan.Trim().Replace("\r\n", "\n").Split('\n')).Select(line => Shown(Unmarked(line.TrimEnd())))];

        // "## Steps" as "Steps": one to six marks and a space.
        static string Unmarked(string line)
        {
            var marks = line.TakeWhile(c => c == '#').Count();
            return marks is >= 1 and <= 6 && marks < line.Length && line[marks] == ' ' ? line[(marks + 1)..].TrimStart() : line;
        }
    }

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
        Current = ask.IsQuestion ? Own(0) : IsPlan ? Plan() : Permission();
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

    /// <summary>Whether this key of a tool call's question asks for the user's words next (<see cref="RefuseWhyLabel"/>, or a plan's <see cref="KeepPlanningLabel"/>): nothing is answered until they are given.</summary>
    public bool AsksForWords(string key) => !_ask.IsQuestion && key.Trim() == (IsPlan ? KeepPlanningKey : RefuseWhyKey);

    /// <summary>Whether these keys only open the explanation of a tool call, or a plan's whole text: nothing is answered by them.</summary>
    public bool Explains(string keys) => !_ask.IsQuestion && keys.Trim() == (IsPlan ? PlanReadKey : ExplainKey);

    /// <summary>The title of the explanation's text.</summary>
    public string ExplainTitle => IsPlan ? $"{Who}'s plan" : $"What {Who} asks to do";

    /// <summary>
    /// Everything Claude asks to do, to be read before it is allowed: the caution first, then the tool and each
    /// member of the call's input by Claude Code's own name for it, a text on its own lines. For a plan, the plan's
    /// caution and then the whole plan as a reader reads it.
    /// </summary>
    public string ExplainText
    {
        get
        {
            if (IsPlan)
            {
                return string.Join("\n", new[] { AiCaution.Plan(Who), string.Empty }.Concat(PlanLines()));
            }

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
        if (IsPlan)
        {
            return AnswerPlan(given);
        }

        if (!_ask.IsQuestion)
        {
            if (WordsPrompt is not null)
            {
                // The words after "No, and tell Claude why": the refusal goes with them.
                var why = given.Trim();
                if (why.Length == 0)
                {
                    return null;
                }

                WordsPrompt = null;
                ToldWhy = why;
                return ClaudeCodeStream.DenyAnswer(ClaudeCodeStream.RefusedWithWords(why));
            }

            switch (given.Trim())
            {
                case RefuseWhyKey:
                    // The words are asked for next, in the window's field, as for Other on Claude's own question.
                    WordsPrompt = ClaudeCodeTexts.TextPromptFor(int.Parse(RefuseWhyKey, CultureInfo.InvariantCulture), RefuseWhyLabel);
                    return null;
                case RefuseKey:
                    return Refusal();
                case OnceKey:
                    return ClaudeCodeStream.AllowAnswer(_ask.Input);
                case AlwaysKey:
                    return ClaudeCodeStream.AlwaysAnswer(_ask.Input, _ask.Tool);
                default:
                    return null;
            }
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
    /// The line that stays in the conversation when the user chose the answer that stops the asking for a tool
    /// (<see cref="AlwaysKey"/>), or null for every other answer: from then on Claude uses the whole tool unasked, and
    /// until 2026-11-21 nothing in the window said afterwards that such a word had been given (the audit of that day).
    /// </summary>
    public string? StandingWords(string given) =>
        !_ask.IsQuestion && !IsPlan && given.Trim() == AlwaysKey
            ? $"You allowed {Who} to use {Shown(_ask.Tool)} without asking for the rest of this session. A new start of Claude Code asks again."
            : null;

    /// <summary>
    /// A plan's answer (SM-7.7; measured 2026-11-26 with Claude Code 2.1.332, the probe's cases plan-manual, plan-edits,
    /// plan-auto and plan-keep): a yes lets the call run and sets the mode the work goes on in, in the answer itself,
    /// which Claude Code follows (its next status names that mode); the no that keeps planning asks for the user's words
    /// first and sends the plan back with them, and Claude plans again and asks anew. Null while the words are awaited
    /// and for what is no answer here.
    /// </summary>
    private string? AnswerPlan(string given)
    {
        if (WordsPrompt is not null)
        {
            var words = given.Trim();
            if (words.Length == 0)
            {
                return null;
            }

            WordsPrompt = null;
            ToldWhy = words;
            return ClaudeCodeStream.DenyAnswer(ClaudeCodeStream.KeepPlanning(words));
        }

        switch (given.Trim())
        {
            case KeepPlanningKey:
                WordsPrompt = ClaudeCodeTexts.TextPromptFor(int.Parse(KeepPlanningKey, CultureInfo.InvariantCulture), KeepPlanningLabel);
                return null;
            case PlanManualKey:
                _planApproved = PlanManualKey;
                return ClaudeCodeStream.AllowWithMode(_ask.Input, ManualMode);
            case PlanAutoKey:
                _planApproved = PlanAutoKey;
                return ClaudeCodeStream.AllowWithMode(_ask.Input, AutoMode);
            default:
                return null;
        }
    }

    /// <summary>A plan's question: who wrote it and what it asks in its title, the caution and then the plan in its text, and the no that keeps planning first.</summary>
    private Question Plan() =>
        new($"{Who} has a plan and asks to carry it out", [AiCaution.PlanAsked(Who), .. PlanLines().Where(line => line.Trim().Length > 0)],
        [
            new QuestionOption(KeepPlanningKey, KeepPlanningLabel),
            new QuestionOption(PlanManualKey, PlanManualLabel),
            new QuestionOption(PlanAutoKey, PlanAutoLabel),
            new QuestionOption(PlanReadKey, "Read the whole plan"),
        ]);

    /// <summary>
    /// Once Claude's own questions are answered as a whole: the record Claude Code's screen prints in their place,
    /// its label, then a row for each question with the answer given ("· Which colour? → Red"), so that the
    /// conversation keeps what was answered as it does with the screen (<see cref="LineKind.AnswerRecord"/>). Empty
    /// for a tool call's question, before the last answer, and after a refusal, which answers nothing.
    /// </summary>
    public IReadOnlyList<string> AnswerRecord
    {
        get
        {
            if (_planApproved is { } approved)
            {
                // A plan approved: the plan stays in the conversation under the answer given, to be read again while
                // Claude carries it out (SM-7.7).
                return [$"You approved {Who}'s plan, {(approved == PlanAutoKey ? "with auto mode" : "and approve each edit yourself")}:", .. PlanLines()];
            }

            if (!_ask.IsQuestion || _at < _ask.Questions.Count)
            {
                return [];
            }

            List<string> rows = [ClaudeCodeTexts.AnsweredLabelText];
            foreach (var asked in _ask.Questions)
            {
                if (_answers.TryGetValue(asked.Text, out var answer))
                {
                    rows.Add(ClaudeCodeTexts.AnsweredRowStart + Shown(asked.Text) + ClaudeCodeTexts.AnsweredArrow + Shown(answer));
                }
            }

            return rows;
        }
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
                new QuestionOption(RefuseWhyKey, RefuseWhyLabel),
                new QuestionOption(OnceKey, "Yes, once"),
                new QuestionOption(AlwaysKey, $"Yes, and do not ask again for {_ask.Tool} in this session"),
                new QuestionOption(ExplainKey, $"Explain what {Who} asks to do"),
            ])
        {
            // The tool and its target in a few words, on every answer's label (PermissionCore, SPEC.md D33); Claude's
            // own description of a call is no target, so then the tool alone.
            Core = PermissionCore.Of(tool, _ask.AboutDescribes ? string.Empty : about),
        };
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
