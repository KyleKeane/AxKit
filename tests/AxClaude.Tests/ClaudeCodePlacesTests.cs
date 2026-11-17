using AxClaude.Core.Hooks;

namespace AxClaude.Tests;

/// <summary>
/// Claude Code's event names and the field names of its events and its session file are written in two places,
/// <c>ClaudeCodeHooks</c> and <c>ClaudeCodeTranscript</c> (root CLAUDE.md rule 12;
/// docs/axClaude/events-and-grounding.md EG-11.7, section 7.3): when Claude Code renames an event or a field, those
/// two files are adjusted and nothing else, and what the user hears stays as it was. Read off the sources: one of the
/// words as a whole word inside a string literal of any other C# file of axClaude's two source folders fails here.
/// Comments are not looked at, since they name Claude Code's fields to explain the code, and an identifier is no
/// string: <c>HookKind.SessionStarted</c> holds an event's name and is the neutral record's own word. The reading of
/// the literals is the shared one (<see cref="SourceTexts"/>).
/// </summary>
public sealed class ClaudeCodePlacesTests
{
    private const string HooksPlace = "ClaudeCodeHooks";
    private const string RecordPlace = "ClaudeCodeTranscript";

    /// <summary>The third place since 2026-11-17: the non-interactive stream, whose lines hold some of the same words (a tool's call and result, the session's id).</summary>
    private const string StreamPlace = "ClaudeCodeStream";

    /// <summary>The fields of an event that only Claude Code names so.</summary>
    private static readonly string[] EventFields =
    [
        "hook_event_name", "session_id", "prompt_id", "transcript_path", "last_assistant_message", "background_tasks",
        "agent_id", "agent_type", "tool_name", "notification_type",
    ];

    /// <summary>The types, fields and values of the session file that only Claude Code names so.</summary>
    private static readonly string[] RecordWords =
    [
        "queue-operation", "promptId", "promptSource", "isMeta", "isCompactSummary", "isSidechain", "isAbortedMidStream",
        "queueTranscriptOnly", "durationMs", "tool_use_id", "tool_use", "tool_result", "turn_duration", "compact_boundary",
    ];

    /// <summary>
    /// The events' names, from the table itself: those made of two words (<c>SessionStart</c>, <c>SubagentStop</c>).
    /// <c>Stop</c> and <c>Notification</c> are words of plain English that a window's own text may hold.
    /// </summary>
    private static IEnumerable<string> EventNames() => ClaudeCodeHooks.Events.Select(spec => spec.Name).Where(name => name.Skip(1).Any(char.IsUpper));

    [Fact]
    public void Claude_Codes_event_and_record_words_stand_in_their_two_places_alone()
    {
        var words = EventNames().Concat(EventFields).Concat(RecordWords).ToList();
        var offenders = new List<string>();
        var inPlace = new HashSet<string>(StringComparer.Ordinal);
        foreach (var folder in new[] { "src/AxClaude.Core", "src/AxClaude" })
        {
            foreach (var literal in SourceTexts.LiteralsUnder(folder))
            {
                // The two places, and files beside them of the same name should either ever be split.
                var file = Path.GetFileName(literal.File);
                var place = file.StartsWith(HooksPlace, StringComparison.Ordinal) || file.StartsWith(RecordPlace, StringComparison.Ordinal) || file.StartsWith(StreamPlace, StringComparison.Ordinal);
                foreach (var word in words)
                {
                    if (!HoldsWord(literal.Text, word))
                    {
                        continue;
                    }

                    if (place)
                    {
                        inPlace.Add(word);
                    }
                    else
                    {
                        offenders.Add($"{literal}: {word}");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, "A word of Claude Code's events, session file or stream outside ClaudeCodeHooks, ClaudeCodeTranscript and ClaudeCodeStream, where it belongs (rule 12): " + string.Join("; ", offenders));

        // The list is no stale one: each word watched is written in one of the two places, and the table has the
        // six names of two words among its eight.
        Assert.Equal(6, EventNames().Count());
        Assert.DoesNotContain(words, word => !inPlace.Contains(word));
    }

    [Theory]
    [InlineData("SessionStart", "SessionStart", true)]
    [InlineData("on SessionStart, then", "SessionStart", true)]
    [InlineData("SessionStarted", "SessionStart", false)]
    [InlineData("PreSessionStart", "SessionStart", false)]
    [InlineData("the_transcript_path", "transcript_path", false)]
    [InlineData("\\\"transcript_path\\\":", "transcript_path", true)]
    [InlineData("type=queue-operation;", "queue-operation", true)]
    [InlineData("tool_use_id", "tool_use", false)]
    public void A_word_is_found_whole_only(string text, string word, bool found)
    {
        Assert.Equal(found, HoldsWord(text, word));
    }

    /// <summary>Whether the text holds the word with no letter, digit or underscore directly in front of it or behind it.</summary>
    private static bool HoldsWord(string text, string word)
    {
        for (var at = text.IndexOf(word, StringComparison.Ordinal); at >= 0; at = text.IndexOf(word, at + 1, StringComparison.Ordinal))
        {
            var before = at == 0 || !IsWordCharacter(text[at - 1]);
            var end = at + word.Length;
            var after = end == text.Length || !IsWordCharacter(text[end]);
            if (before && after)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';
}
