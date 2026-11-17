using System.Text.Json;
using AxClaude.Core.Streaming;
using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The stream fixtures (tests/fixtures/streams, docs/axClaude/stream-mode.md SM-10.1) replayed through the code a
/// window over the stream will use: each line Claude Code wrote goes through <see cref="ClaudeCodeStream.Read"/>
/// into a <see cref="StreamModel"/>, each message the host wrote is sent to the model first, as the window sends
/// it, and each question to the permission tool is passed on as the relay will pass it. What comes out is a text:
/// what was written and read, in the stream's own order, with what the model raised and how its facts changed
/// after each step, and then the conversation as it stands, every line with its kind.
/// </summary>
internal static class StreamReplay
{
    /// <summary>The folder a recording's own folder was replaced with by the fixtures' maker.</summary>
    private const string Folder = @"C:\work";

    public static string FixturesFolder() => Path.Combine(Fixtures.Folder(), "streams");

    /// <summary>The names of the recorded cases, in order.</summary>
    public static IEnumerable<string> Cases() =>
        Directory.GetFiles(FixturesFolder(), "*.jsonl").Select(file => Path.GetFileNameWithoutExtension(file)).Order(StringComparer.Ordinal);

    /// <summary>Replays one case; the model is left as the recording left it.</summary>
    public static List<string> Run(string name, out StreamModel model)
    {
        var replayed = new StreamModel(Folder);
        model = replayed;
        var log = new List<string>();
        replayed.Bell += () => log.Add("    the bell");
        replayed.ResponseStarted += () => log.Add("    the response started");
        replayed.Announcement += text => log.Add("    announced: " + text);
        replayed.TurnSelfStarted += () => log.Add("    a turn Claude Code started by itself");
        var facts = Facts(replayed);
        foreach (var record in File.ReadLines(Path.Combine(FixturesFolder(), name + ".jsonl")))
        {
            using var document = JsonDocument.Parse(record);
            var root = document.RootElement;
            switch (root.GetProperty("k").GetString())
            {
                case "in":
                    var what = root.GetProperty("what").GetString();
                    if (what == "message")
                    {
                        var text = root.GetProperty("text").GetString() ?? string.Empty;
                        log.Add("> message: " + Short(text));
                        log.Add(replayed.Send(text) ? "    a block for it" : "    no block for it");
                    }
                    else
                    {
                        log.Add("> " + what);
                    }

                    break;

                case "out":
                    var line = ClaudeCodeStream.Read(root.GetProperty("line").GetRawText());
                    log.Add("< " + Words(line));
                    replayed.Take(line);
                    break;

                case "ask":
                    log.Add("? the permission tool is asked about " + root.GetProperty("tool").GetString());
                    replayed.Ask(QuestionOf(root));
                    replayed.MarkQuestion();
                    break;

                case "answer":
                    log.Add("! answered: " + root.GetProperty("behavior").GetString());
                    replayed.Answered();
                    break;

                case "exit":
                    log.Add("the process ended with code " + root.GetProperty("code").GetInt32());
                    break;
            }

            var now = Facts(replayed);
            if (now != facts)
            {
                log.Add("    now: " + now);
                facts = now;
            }
        }

        log.Add(string.Empty);
        log.Add("-- the conversation");
        foreach (var shown in replayed.Lines)
        {
            log.Add($"[{shown.Kind}{(shown.HeadingLevel > 0 ? " h" + shown.HeadingLevel : string.Empty)}] {shown.Text}");
        }

        return log;
    }

    /// <summary>The model's facts in one line: what a window's status bar and signals are made from.</summary>
    private static string Facts(StreamModel model) =>
        $"{(model.Working ? "working" : "idle")}, mode {model.Mode ?? "not known"}, {model.QueuedMessages} waiting"
        + (model.PromptPending ? $", asks \"{model.PendingQuestion?.Title}\"" : string.Empty)
        + (model.Background is null ? string.Empty : $", in the background {model.Background}")
        + (model.BackgroundAgents.Count == 0 ? string.Empty : " (" + string.Join("; ", model.BackgroundAgents.Select(agent => $"{agent.Type}: {agent.Description}")) + ")");

    /// <summary>A line in kinds and names, with a tool's subject and a result's failure.</summary>
    private static string Words(StreamLine line)
    {
        var parts = line.Parts.Select(part => part.Kind switch
        {
            StreamKind.ToolCall => $"ToolCall {part.Tool} ({part.About})",
            StreamKind.ToolResult => part.Error ? $"ToolResult, failed: \"{Short(part.Text)}\"" : "ToolResult",
            StreamKind.Note => "Note " + part.Text + (line.Status.Length > 0 ? " " + line.Status : string.Empty),
            StreamKind.Unknown => "Unknown " + part.Text,
            StreamKind.Control => part.Error ? "Control, refused" : "Control, done",
            StreamKind.Ended => $"Ended, {(line.Failed ? "failed" : "well")}, turns {line.Turns}, {line.Terminal}",
            StreamKind.Started => "Started, mode " + line.Mode,
            StreamKind.Text => (part.Error ? "Text, an error: " : "Text: ") + Short(part.Text),
            StreamKind.Message => "Message: " + Short(part.Text),
            StreamKind.CommandOutput => "CommandOutput: " + Short(part.Text),
            _ => part.Kind.ToString(),
        });
        var text = string.Join("; ", parts);
        return (text.Length == 0 ? "nothing known" : text)
            + (line.FromWorker ? " (a subagent's)" : string.Empty)
            + (line.Tasks is { } tasks ? $" [{string.Join("; ", tasks.Select(task => $"{task.Kind}: {task.Description}"))}]" : string.Empty);
    }

    /// <summary>The question a window would show for what the permission tool was asked: Claude's own question with its options, or a permission with three answers.</summary>
    private static Question QuestionOf(JsonElement asked)
    {
        var tool = asked.GetProperty("tool").GetString() ?? string.Empty;
        if (tool == ClaudeCodeStream.QuestionTool && asked.GetProperty("input").TryGetProperty("questions", out var questions) && questions.GetArrayLength() > 0)
        {
            var first = questions[0];
            var options = first.GetProperty("options").EnumerateArray()
                .Select((option, index) => new QuestionOption((index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), option.GetProperty("label").GetString() ?? string.Empty))
                .ToList();
            return new Question(first.GetProperty("question").GetString() ?? string.Empty, [], options);
        }

        return new Question("Permission: " + tool, [], [new QuestionOption("1", "No"), new QuestionOption("2", "Yes"), new QuestionOption("3", "Yes, for this session")]);
    }

    private static string Short(string text)
    {
        var first = text.Replace("\r", string.Empty).Replace('\n', ' ');
        return first.Length > 70 ? first[..70] + "…" : first;
    }
}
