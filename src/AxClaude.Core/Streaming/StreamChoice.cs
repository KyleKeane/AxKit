using System.Globalization;
using AxClaude.Core.Transcript;
using AxKit.Core;

namespace AxClaude.Core.Streaming;

/// <summary>
/// Choose the model and Choose the effort over the stream (docs/axClaude/stream-mode.md SM-5.5): Claude Code's own
/// pickers are screens the stream does not have, so the window asks with a list of its own, made from what the answer
/// to the first request listed (<see cref="StreamModel.Models"/>), and sends the choice as the slash command with its
/// value, which the stream runs as a message and answers with one line of print (measured: the fixture choices,
/// <c>/model haiku</c> and <c>/effort low</c> while the run goes on). Pure: the window shows <see cref="Question"/> in
/// its control area and hands back the key chosen.
/// </summary>
public sealed class StreamChoice
{
    /// <summary>The slash command the choice is sent with (<c>/model</c>, <c>/effort</c>).</summary>
    private readonly string _command;

    /// <summary>The value of each answer, in the list's order: what goes after the command.</summary>
    private readonly IReadOnlyList<string> _values;

    private StreamChoice(string command, Question question, IReadOnlyList<string> values)
    {
        _command = command;
        Question = question;
        _values = values;
    }

    /// <summary>The list the window shows.</summary>
    public Question Question { get; }

    /// <summary>
    /// The models, each by the name Claude Code shows with its words about it; the one in use marked, when it is
    /// named by the same name. Null when no model is known: the first request was not answered.
    /// </summary>
    public static StreamChoice? Model(string command, IReadOnlyList<StreamModelChoice> models, string inUse)
    {
        if (models.Count == 0)
        {
            return null;
        }

        var options = new List<QuestionOption>();
        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            var name = model.Name.Length > 0 ? model.Name : model.Value;
            var text = model.Description.Length > 0 && !string.Equals(model.Description, name, StringComparison.OrdinalIgnoreCase)
                ? name + ClaudeCodeTexts.AnswerDash + model.Description
                : name;
            options.Add(new QuestionOption(Key(i + 1), DisplayText.Visible(text), model.Value == inUse));
        }

        return new StreamChoice(command, new Question("Choose the model", [], options), [.. models.Select(model => model.Value)]);
    }

    /// <summary>
    /// The efforts the models take, each once, in the order the list gives them (low to high). Claude Code's answer to
    /// the first request does not say which is in force, so none is marked. Null when no model takes an effort.
    /// </summary>
    public static StreamChoice? Effort(string command, IReadOnlyList<StreamModelChoice> models)
    {
        var levels = models.SelectMany(model => model.Efforts).Where(level => level.Length > 0).Distinct(StringComparer.Ordinal).ToList();
        if (levels.Count == 0)
        {
            return null;
        }

        var options = levels.Select((level, i) => new QuestionOption(Key(i + 1), DisplayText.Visible(level))).ToList();
        return new StreamChoice(command, new Question("Choose the effort", [], options), levels);
    }

    /// <summary>The line sent for the answer of this key (<c>/model haiku</c>); null for a key that is no answer here.</summary>
    public string? CommandFor(string key)
    {
        if (!int.TryParse(key.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 1 || number > _values.Count)
        {
            return null;
        }

        return _command + " " + _values[number - 1];
    }

    private static string Key(int number) => number.ToString(CultureInfo.InvariantCulture);
}
