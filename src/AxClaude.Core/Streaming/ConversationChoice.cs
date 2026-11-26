using System.Globalization;
using AxClaude.Core.Record;
using AxClaude.Core.Transcript;
using AxKit.Core;

namespace AxClaude.Core.Streaming;

/// <summary>
/// One earlier conversation of a folder, as a list of them names it (docs/axClaude/stream-mode.md SM-8.5): its session's
/// id, which is never shown, said or logged; when its file was written last; and the first words of its first message.
/// </summary>
public sealed record EarlierConversation(string Session, DateTime Written, string FirstWords);

/// <summary>
/// The choice of an earlier conversation to take up (docs/axClaude/stream-mode.md SM-8.5; stream-equivalence.md L4):
/// Claude Code's own list of them is a screen the stream does not have, so a window asks with a list of its own of the
/// folder's conversations, newest first, each named by when it was last written and the first words of its first
/// message, and the choice starts Claude Code with <c>--resume</c> and the conversation's id. A session file is
/// Claude Code's internal and private record: only its first message is read, of it no more than the first words are
/// shown, and nothing of it is logged. <see cref="Find"/> reads the files, off the window's thread; the rest is pure,
/// as <see cref="StreamChoice"/> is: the window shows <see cref="Question"/> in its control area and hands back the key.
/// </summary>
public sealed class ConversationChoice
{
    /// <summary>The title of the list.</summary>
    public const string Title = "Choose a conversation to resume";

    /// <summary>As many words of the first message as name a conversation.</summary>
    public const int Words = 8;

    /// <summary>As many characters as those words may take: a word can be a long path.</summary>
    public const int Characters = 60;

    /// <summary>How much of a session file is read for its first message: it stands at the start (a file can be many megabytes).</summary>
    public const long ReadBytes = 256 * 1024;

    /// <summary>The ids of the answers, in the list's order.</summary>
    private readonly IReadOnlyList<string> _sessions;

    private ConversationChoice(Question question, IReadOnlyList<string> sessions)
    {
        Question = question;
        _sessions = sessions;
    }

    /// <summary>The list the window shows.</summary>
    public Question Question { get; }

    /// <summary>
    /// The folder's earlier conversations, newest first: each session file directly in its project folders, named by the
    /// first words of the first message the user wrote in it; a file in which none is found in its first
    /// <see cref="ReadBytes"/> is no conversation to take up and is left out. Reads files: off the window's thread.
    /// Never throws; a file that cannot be read is left out.
    /// </summary>
    public static IReadOnlyList<EarlierConversation> Find(IEnumerable<string> projectFolders)
    {
        var found = new List<EarlierConversation>();
        foreach (var (id, file, written) in SessionHandover.All(projectFolders))
        {
            try
            {
                if (FirstWordsOf(RecordTail.ReadAll(file, ClaudeCodeTranscript.Parse, ReadBytes)) is { } words)
                {
                    found.Add(new EarlierConversation(id, written, words));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                // A file that cannot be read is no conversation the window can name.
            }
        }

        return found;
    }

    /// <summary>
    /// The first words of the first message the user wrote, of a session file's entries: its first line, at most
    /// <see cref="Words"/> words and <see cref="Characters"/> characters, an ellipsis when cut, every hidden character
    /// written out. Null when no message of the user's is among them (a turn Claude Code began by itself, a command, a
    /// note in the user's place are none).
    /// </summary>
    public static string? FirstWordsOf(IEnumerable<RecordEntry> entries)
    {
        var first = entries.FirstOrDefault(entry => entry.Kind == RecordKind.Prompt && !entry.Sidechain
            && entry.Origin != PromptOrigin.SelfStarted && !string.IsNullOrWhiteSpace(entry.Text));
        if (first?.Text is not { } text)
        {
            return null;
        }

        var line = text.Trim().Replace("\r\n", "\n").Split('\n')[0].Trim();
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var shown = string.Join(' ', words.Take(Words));
        var cut = words.Length > Words || text.Trim().Contains('\n');
        if (shown.Length > Characters)
        {
            shown = shown[..Characters].TrimEnd();
            cut = true;
        }

        return DisplayText.Visible(shown) + (cut ? "…" : string.Empty);
    }

    /// <summary>A conversation's name in the list: when it was last written, in the time of <paramref name="now"/>'s zone, then its first words ("26 November, 14:05: Remember this word…"); the year when it is not this one.</summary>
    public static string Name(EarlierConversation conversation, DateTime now)
    {
        var written = conversation.Written.Kind == DateTimeKind.Utc ? conversation.Written.ToLocalTime() : conversation.Written;
        var date = FieldTypes.ShortDate(DateOnly.FromDateTime(written), DateOnly.FromDateTime(now));
        return $"{date}, {written.ToString("HH:mm", CultureInfo.InvariantCulture)}: {conversation.FirstWords}";
    }

    /// <summary>The list of the conversations, newest first, as they were found; null when there is none to choose.</summary>
    public static ConversationChoice? Of(IReadOnlyList<EarlierConversation> conversations, DateTime now)
    {
        if (conversations.Count == 0)
        {
            return null;
        }

        var options = conversations.Select((conversation, i) => new QuestionOption((i + 1).ToString(CultureInfo.InvariantCulture), Name(conversation, now))).ToList();
        return new ConversationChoice(new Question(Title, [], options), [.. conversations.Select(conversation => conversation.Session)]);
    }

    /// <summary>The session's id of the answer of this key; null for a key that is no answer here. An id: never shown, said or logged.</summary>
    public string? SessionFor(string key)
    {
        if (!int.TryParse(key.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 1 || number > _sessions.Count)
        {
            return null;
        }

        return _sessions[number - 1];
    }
}
