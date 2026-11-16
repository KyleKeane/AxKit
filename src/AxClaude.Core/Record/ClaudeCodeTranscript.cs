using System.Buffers.Text;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AxClaude.Core.Record;

/// <summary>
/// The one place of Claude Code's session file in axClaude (root CLAUDE.md rule 12; docs/axClaude/events-and-grounding.md,
/// "EG" below, EG-3 and 7.3): where Claude Code keeps the files (EG-3.1), what a line of one is read as (EG-3.4,
/// EG-3.5), and the rules of Claude Code's screen that change a reply's letters (EG-3.7). Every name of a type and of a
/// field of the file is a constant of this file and stands in no other file of axClaude's (EG-11.7); what it returns
/// is the neutral <see cref="RecordEntry"/>. Claude Code's documentation calls the format internal ("changes between
/// versions"), so when a release moves a field this file is adjusted and nothing that reads the entries. Measured with
/// the version of <see cref="CheckedWith"/> (EG section 4): one entry for each content block, UTF-8 without a mark,
/// LF, about half the lines no conversation, lines up to 533 KB, timestamps that go backwards (order is file order).
/// A line is never read whole into a document, and no string is made of a field the table of EG-3.4 does not name:
/// the first look finds the top-level <c>type</c> with every other value skipped; only the four types read get a
/// second look, which notes where each string it may need starts and compares the few enum-like values in place;
/// the strings are taken from their offsets once the entry's kind is known. A tool's input, a result's text,
/// <c>cwd</c>, the account's ids and the copies of what was sent to the model are never made into strings.
/// </summary>
public static partial class ClaudeCodeTranscript
{
    /// <summary>The Claude Code version the file's format was measured with (2026-11-15).</summary>
    public const string CheckedWith = "2.1.325";

    /// <summary>A session's file: <c>&lt;session id&gt;.jsonl</c>, one JSON object for each line.</summary>
    public const string SessionFileExtension = ".jsonl";

    /// <summary>The folder of the configuration folder that holds a folder for each project.</summary>
    public const string ProjectsFolderName = "projects";

    // ---- The types read (EG-3.4). Every other type is skipped at its type: mode, session-owner, attachment, the
    // api-request copies, last-prompt, ai-title, file-history-snapshot, cost-state and those to come. ----

    private const string UserType = "user";
    private const string AssistantType = "assistant";
    private const string SystemType = "system";
    private const string QueueType = "queue-operation";

    // ---- The top-level fields read; every other one is skipped unread ----

    private const string TypeField = "type";
    private const string UuidField = "uuid";
    private const string PromptIdField = "promptId";
    private const string PromptSourceField = "promptSource";
    private const string IsMetaField = "isMeta";
    private const string IsCompactSummaryField = "isCompactSummary";
    private const string IsSidechainField = "isSidechain";
    private const string IsAbortedField = "isAbortedMidStream";
    private const string TimestampField = "timestamp";
    private const string SubtypeField = "subtype";
    private const string DurationField = "durationMs";
    private const string OperationField = "operation";
    private const string MessageField = "message";

    /// <summary>The message's content (a string or an array of blocks), and the text of an enqueue.</summary>
    private const string ContentField = "content";

    // ---- The fields of the message and of a content block ----

    /// <summary>The message's id (<c>msg_…</c>) and a tool call's id (<c>toolu_…</c>).</summary>
    private const string IdField = "id";
    private const string TextField = "text";
    private const string NameField = "name";
    private const string ToolUseIdField = "tool_use_id";
    private const string IsErrorField = "is_error";

    // ---- The values compared ----

    private const string TextBlock = "text";
    private const string ToolUseBlock = "tool_use";
    private const string ThinkingBlock = "thinking";
    private const string ToolResultBlock = "tool_result";

    private const string TypedSource = "typed";
    private const string QueuedSource = "queued";

    /// <summary>A prompt Claude Code wrote itself: the hand-back of a background task or agent (EG 4.1).</summary>
    private const string SystemSource = "system";

    private const string TurnDurationSubtype = "turn_duration";
    private const string CompactBoundarySubtype = "compact_boundary";

    private const string EnqueueOperation = "enqueue";
    private const string DequeueOperation = "dequeue";
    private const string RemoveOperation = "remove";

    // ---- How a user entry's text starts when Claude Code wrote it in the user's place ----

    /// <summary>A slash command Claude Code handled itself (<c>/exit</c>, <c>/compact</c>).</summary>
    private const string CommandTag = "<command-name>";

    /// <summary>A command's caveat and its output, <c>&lt;local-command-caveat&gt;</c> and <c>&lt;local-command-stdout&gt;</c>.</summary>
    private const string LocalCommandTag = "<local-command-";

    /// <summary>A <c>!</c> shell line and its output, <c>&lt;bash-input&gt;</c>, <c>&lt;bash-stdout&gt;</c>, <c>&lt;bash-stderr&gt;</c>.</summary>
    private const string ShellTag = "<bash-";

    private const string TaskNotificationTag = "<task-notification>";
    private const string AgentMessageTag = "<agent-message";

    /// <summary>A text that starts with a tag none of the above names: Claude Code's own, read as Other.</summary>
    private const string AnyTag = "<";

    /// <summary><c>[Request interrupted by user]</c>, and <c>… for tool use]</c> after a tool was cut.</summary>
    private const string InterruptStart = "[Request interrupted";

    private static readonly string[] NoteTags = [CommandTag, LocalCommandTag, ShellTag, TaskNotificationTag, AgentMessageTag];

    /// <summary>Where a string value was not found.</summary>
    private const int Absent = -1;

    /// <summary>The prefix of an entry's local id, made from its line's offset: no uuid starts with it.</summary>
    private const string LocalIdPrefix = "@";

    private static ReadOnlySpan<byte> ByteOrderMark => [0xEF, 0xBB, 0xBF];

    private static readonly IReadOnlyDictionary<string, string> NoEnvironment = new Dictionary<string, string>();

    // ---- Where the files are (EG-3.1) ----

    /// <summary>
    /// The name of a working folder's project folder: the folder's full path with every character that is not an
    /// ASCII letter or digit replaced by a hyphen (Claude Code's documentation, sessions, "Where transcripts are
    /// stored"; measured: <c>C:\Temp\axclaude-fixture</c> gives <c>C--Temp-axclaude-fixture</c>). One hyphen for each
    /// UTF-16 character, as Claude Code's own replacement counts them. A name over 200 characters Claude Code cuts and
    /// ends with a hash axKit cannot make; for such a folder only the hook's path finds the session (EG-3.2).
    /// </summary>
    public static string ProjectFolderName(string workingFolder)
    {
        var folder = Path.TrimEndingDirectorySeparator(workingFolder);
        var name = new StringBuilder(folder.Length);
        foreach (var c in folder)
        {
            name.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');
        }

        return name.ToString();
    }

    /// <summary>
    /// The projects folders a session file of this Claude Code may lie in: <c>projects</c> in Claude Code's
    /// configuration folder, which is the folder of <see cref="ClaudeCodeSettings.UserSettingsFile"/> (its
    /// <c>CLAUDE_CONFIG_DIR</c> of <paramref name="childEnvironment"/> when that is a full path, else <c>.claude</c>
    /// under <paramref name="home"/>), and in a test run also the one under the run's own folder
    /// <paramref name="runHome"/>, so that a stand-in's files never land among the user's. Full paths, each once.
    /// </summary>
    public static IReadOnlyList<string> ProjectsFolders(string home, IReadOnlyDictionary<string, string> childEnvironment, string? runHome)
    {
        var folders = new List<string> { ProjectsIn(home, childEnvironment) };
        if (!string.IsNullOrWhiteSpace(runHome))
        {
            var run = ProjectsIn(runHome, NoEnvironment);
            if (!folders.Contains(run, StringComparer.OrdinalIgnoreCase))
            {
                folders.Add(run);
            }
        }

        return folders;
    }

    /// <summary>
    /// Whether the path names a session's file: a <c>.jsonl</c> file directly in a project folder of one of the
    /// projects folders (EG-3.1), compared without regard to case. Refused: a relative path, a path with <c>..</c>
    /// in it, another extension, a file in a folder under the project folder (<c>subagents</c>, <c>tool-results</c>,
    /// <c>memory</c>), a file directly in the projects folder, another root, and a name with a dot before
    /// <c>.jsonl</c>, which is a copy Claude Code set aside (<c>&lt;session&gt;.orphaned-….jsonl</c>, documentation,
    /// claude-directory) and never the session's own file. A path an event names is checked here before it is used (EG-2.9).
    /// </summary>
    public static bool IsSessionFile(string? path, IReadOnlyList<string> projectsFolders)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)
            || path.Split('\\', '/').Any(part => part == ".."))
        {
            return false;
        }

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }

        var name = Path.GetFileName(full);
        if (!name.EndsWith(SessionFileExtension, StringComparison.OrdinalIgnoreCase)
            || name.Length == SessionFileExtension.Length
            || name[..^SessionFileExtension.Length].Contains('.'))
        {
            return false;
        }

        var project = Path.GetDirectoryName(full);
        var projects = project is null ? null : Path.GetDirectoryName(project);
        return projects is not null
            && projectsFolders.Any(folder => string.Equals(Path.TrimEndingDirectorySeparator(folder), projects, StringComparison.OrdinalIgnoreCase));
    }

    private static string ProjectsIn(string home, IReadOnlyDictionary<string, string> environment) =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ClaudeCodeSettings.UserSettingsFile(home, environment))!, ProjectsFolderName));

    // ---- What a line is read as (EG-3.4, EG-3.5) ----

    /// <summary>
    /// One line of a session file, without its line end, read as an entry (EG-3.4), or null for a line of a type not
    /// read and for one that is no JSON object; <paramref name="what"/> says which, for the reader's counters (EG-3.6).
    /// <paramref name="offset"/> is the line's place in the file, from which an entry without a uuid gets its local id.
    /// Tolerant (EG-3.5): property order does not matter, a missing field is its default, an unknown block, subtype or
    /// operation is Other, an entry of an agent of the session (<c>isSidechain</c>) is Other. Never throws.
    /// </summary>
    public static RecordEntry? Parse(ReadOnlySpan<byte> line, long offset, out RecordLine what)
    {
        if (line.StartsWith(ByteOrderMark))
        {
            line = line[ByteOrderMark.Length..];
        }

        try
        {
            var type = TypeOf(line);
            if (type == LineType.NotObject)
            {
                what = RecordLine.NotJson;
                return null;
            }

            if (type == LineType.NotRead)
            {
                what = RecordLine.Skipped;
                return null;
            }

            var found = Look(line);
            return type switch
            {
                LineType.User => UserEntry(line, offset, found, out what),
                LineType.Assistant => AssistantEntry(line, offset, found, out what),
                LineType.System => SystemEntry(line, offset, found, out what),
                _ => QueueEntry(line, offset, found, out what),
            };
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // Broken JSON, or a string that is no UTF-8: the reader counts the line and goes on.
            what = RecordLine.NotJson;
            return null;
        }
    }

    /// <summary>What the first look found: the line's type among those read, another type, or no JSON object.</summary>
    private enum LineType
    {
        NotObject,
        NotRead,
        User,
        Assistant,
        System,
        Queue,
    }

    private enum ContentShape
    {
        None,
        Text,
        Blocks,
        Other,
    }

    private enum BlockType
    {
        None,
        Text,
        ToolUse,
        Thinking,
        ToolResult,
        Other,
    }

    private enum Subtype
    {
        None,
        TurnDuration,
        CompactBoundary,
        Other,
    }

    private enum Operation
    {
        None,
        Enqueue,
        Dequeue,
        Remove,
        Other,
    }

    /// <summary>
    /// What the second look found in a line of a type read: where the strings it may need start (their offsets in the
    /// line, <see cref="Absent"/> when missing), and the flags, numbers and enum-like values, compared in place.
    /// </summary>
    private sealed class Found
    {
        public int Uuid = Absent;
        public int PromptId = Absent;
        public int Timestamp = Absent;
        public int QueueContent = Absent;
        public PromptOrigin Origin;
        public bool IsMeta;
        public bool IsCompactSummary;
        public bool IsSidechain;
        public bool IsAborted;
        public Subtype Subtype;
        public Operation Operation;
        public long DurationMs;

        public bool HasMessage;
        public int MessageId = Absent;
        public ContentShape Content;
        public int ContentText = Absent;

        public int Blocks;
        public BlockType FirstBlock;
        public int FirstText = Absent;
        public int FirstId = Absent;
        public int FirstName = Absent;
        public bool ToolResult;
        public int ToolUseId = Absent;
        public bool IsError;
        public bool InterruptText;
    }

    /// <summary>The first look: the top-level <c>type</c> among the types read; every other value is skipped untouched.</summary>
    private static LineType TypeOf(ReadOnlySpan<byte> line)
    {
        var reader = new Utf8JsonReader(line);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            return LineType.NotObject;
        }

        var type = LineType.NotRead;
        var seen = false;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (seen || !reader.ValueTextEquals(TypeField))
            {
                reader.Skip();
                continue;
            }

            seen = true;
            reader.Read();
            type = reader.TokenType != JsonTokenType.String ? LineType.NotRead
                : reader.ValueTextEquals(UserType) ? LineType.User
                : reader.ValueTextEquals(AssistantType) ? LineType.Assistant
                : reader.ValueTextEquals(SystemType) ? LineType.System
                : reader.ValueTextEquals(QueueType) ? LineType.Queue
                : LineType.NotRead;
            reader.Skip();
        }

        // Past the object's end there must be nothing but white space; the reader throws on anything else.
        return reader.Read() ? LineType.NotObject : type;
    }

    /// <summary>The second look, for the four types read only.</summary>
    private static Found Look(ReadOnlySpan<byte> line)
    {
        var found = new Found();
        var reader = new Utf8JsonReader(line);
        reader.Read();
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals(UuidField))
            {
                found.Uuid = StringStart(ref reader);
            }
            else if (reader.ValueTextEquals(PromptIdField))
            {
                found.PromptId = StringStart(ref reader);
            }
            else if (reader.ValueTextEquals(TimestampField))
            {
                found.Timestamp = StringStart(ref reader);
            }
            else if (reader.ValueTextEquals(ContentField))
            {
                found.QueueContent = StringStart(ref reader);
            }
            else if (reader.ValueTextEquals(PromptSourceField))
            {
                found.Origin = OriginOf(ref reader);
            }
            else if (reader.ValueTextEquals(IsMetaField))
            {
                found.IsMeta = IsTrue(ref reader);
            }
            else if (reader.ValueTextEquals(IsCompactSummaryField))
            {
                found.IsCompactSummary = IsTrue(ref reader);
            }
            else if (reader.ValueTextEquals(IsSidechainField))
            {
                found.IsSidechain = IsTrue(ref reader);
            }
            else if (reader.ValueTextEquals(IsAbortedField))
            {
                found.IsAborted = IsTrue(ref reader);
            }
            else if (reader.ValueTextEquals(SubtypeField))
            {
                found.Subtype = SubtypeOf(ref reader);
            }
            else if (reader.ValueTextEquals(OperationField))
            {
                found.Operation = OperationOf(ref reader);
            }
            else if (reader.ValueTextEquals(DurationField))
            {
                found.DurationMs = NumberOf(ref reader);
            }
            else if (reader.ValueTextEquals(MessageField))
            {
                LookAtMessage(ref reader, line, found);
            }
            else
            {
                reader.Skip();
            }
        }

        return found;
    }

    /// <summary>The message object: its id and its content; its model, usage, stop reason and the rest are skipped.</summary>
    private static void LookAtMessage(ref Utf8JsonReader reader, ReadOnlySpan<byte> line, Found found)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return;
        }

        found.HasMessage = true;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals(IdField))
            {
                found.MessageId = StringStart(ref reader);
            }
            else if (reader.ValueTextEquals(ContentField))
            {
                LookAtContent(ref reader, line, found);
            }
            else
            {
                reader.Skip();
            }
        }
    }

    /// <summary>The message's content: a string (a prompt, a command, a note) or an array of blocks.</summary>
    private static void LookAtContent(ref Utf8JsonReader reader, ReadOnlySpan<byte> line, Found found)
    {
        reader.Read();
        if (reader.TokenType == JsonTokenType.String)
        {
            found.Content = ContentShape.Text;
            found.ContentText = (int)reader.TokenStartIndex;
            return;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            found.Content = ContentShape.Other;
            reader.Skip();
            return;
        }

        found.Content = ContentShape.Blocks;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                LookAtBlock(ref reader, line, found);
            }
            else
            {
                reader.Skip();
            }
        }
    }

    /// <summary>
    /// One content block: its type and where its text, id, name and the id of the call it answers start, and whether
    /// it is an error. A tool's input, a result's content and a thinking block's text and signature are skipped.
    /// </summary>
    private static void LookAtBlock(ref Utf8JsonReader reader, ReadOnlySpan<byte> line, Found found)
    {
        var type = BlockType.None;
        int text = Absent, id = Absent, name = Absent, toolUseId = Absent;
        var isError = false;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals(TypeField))
            {
                type = BlockTypeOf(ref reader);
            }
            else if (reader.ValueTextEquals(TextField))
            {
                text = StringStart(ref reader);
            }
            else if (reader.ValueTextEquals(IdField))
            {
                id = StringStart(ref reader);
            }
            else if (reader.ValueTextEquals(NameField))
            {
                name = StringStart(ref reader);
            }
            else if (reader.ValueTextEquals(ToolUseIdField))
            {
                toolUseId = StringStart(ref reader);
            }
            else if (reader.ValueTextEquals(IsErrorField))
            {
                isError = IsTrue(ref reader);
            }
            else
            {
                reader.Skip();
            }
        }

        found.Blocks++;
        if (found.Blocks == 1)
        {
            found.FirstBlock = type;
            found.FirstText = text;
            found.FirstId = id;
            found.FirstName = name;
        }

        if (type == BlockType.ToolResult && !found.ToolResult)
        {
            found.ToolResult = true;
            found.ToolUseId = toolUseId;
            found.IsError = isError;
        }

        if (type == BlockType.Text && text != Absent && StartsWithAt(line, text, InterruptStart))
        {
            found.InterruptText = true;
        }
    }

    /// <summary>
    /// A user entry (EG-3.4): an agent's entry is Other; then a compaction's summary; an entry Claude Code marks as
    /// its own (<c>isMeta</c>) is a Note; an array of blocks is a tool's result, an interrupt or Other; a text is a
    /// Note when it starts with one of Claude Code's tags, an interrupt, Other when it starts with another tag, and
    /// otherwise the user's prompt with its text. A Note whose source is <c>system</c> opened a turn Claude Code
    /// started itself: it is a Prompt of that origin, without text.
    /// </summary>
    private static RecordEntry UserEntry(ReadOnlySpan<byte> line, long offset, Found found, out RecordLine what)
    {
        var id = IdOf(line, offset, found, out what);
        if (found.IsSidechain)
        {
            return new(RecordKind.Other, id, Sidechain: true);
        }

        if (!found.HasMessage || found.Content is ContentShape.None or ContentShape.Other
            || (found.Content == ContentShape.Blocks && found.Blocks == 0))
        {
            what = RecordLine.Unknown;
            return new(RecordKind.Other, id);
        }

        if (found.IsCompactSummary)
        {
            return new(RecordKind.CompactSummary, id);
        }

        if (found.IsMeta)
        {
            return Note(id, StringOrNull(line, found.PromptId), found.Origin);
        }

        if (found.Content == ContentShape.Blocks)
        {
            return found.ToolResult
                ? new(RecordKind.ToolResult, id, StringOrNull(line, found.PromptId), ToolUseId: StringOrNull(line, found.ToolUseId), IsError: found.IsError)
                : found.InterruptText
                    ? new(RecordKind.Interrupt, id, StringOrNull(line, found.PromptId))
                    : new(RecordKind.Other, id);
        }

        foreach (var tag in NoteTags)
        {
            if (StartsWithAt(line, found.ContentText, tag))
            {
                return Note(id, StringOrNull(line, found.PromptId), found.Origin);
            }
        }

        if (StartsWithAt(line, found.ContentText, InterruptStart))
        {
            // Measured as a text block; the same words as a plain text are taken the same way.
            return new(RecordKind.Interrupt, id, StringOrNull(line, found.PromptId));
        }

        if (StartsWithAt(line, found.ContentText, AnyTag))
        {
            return new(RecordKind.Other, id);
        }

        return new(RecordKind.Prompt, id, StringOrNull(line, found.PromptId), Text: StringAt(line, found.ContentText),
            Origin: found.Origin, Time: TimeAt(line, found.Timestamp));
    }

    private static RecordEntry Note(string id, string? turn, PromptOrigin origin) =>
        origin == PromptOrigin.SelfStarted
            ? new(RecordKind.Prompt, id, turn, Origin: PromptOrigin.SelfStarted)
            : new(RecordKind.Note, id, turn, Origin: origin);

    /// <summary>
    /// An assistant entry: one content block (EG 4.3), read by its first block: a text with its source, the message's
    /// id, whether it was cut (<c>isAbortedMidStream</c>) and its time; a tool call with its id and the tool's name; a
    /// thinking block with nothing; any other block as Other. The stop reason is not kept: the cut is the flag's.
    /// </summary>
    private static RecordEntry AssistantEntry(ReadOnlySpan<byte> line, long offset, Found found, out RecordLine what)
    {
        var id = IdOf(line, offset, found, out what);
        if (found.IsSidechain)
        {
            return new(RecordKind.Other, id, Sidechain: true);
        }

        if (!found.HasMessage || found.Content != ContentShape.Blocks || found.Blocks == 0)
        {
            what = RecordLine.Unknown;
            return new(RecordKind.Other, id);
        }

        var message = StringOrNull(line, found.MessageId);
        return found.FirstBlock switch
        {
            BlockType.Text => new(RecordKind.ReplyText, id, MessageId: message, Text: StringOrNull(line, found.FirstText) ?? "",
                Cut: found.IsAborted, Time: TimeAt(line, found.Timestamp)),
            BlockType.ToolUse => new(RecordKind.ToolCall, id, MessageId: message, ToolName: StringOrNull(line, found.FirstName),
                ToolUseId: StringOrNull(line, found.FirstId)),
            BlockType.Thinking => new(RecordKind.Thinking, id),
            _ => new(RecordKind.Other, id),
        };
    }

    /// <summary>A system entry: the turn's end with its duration, a compaction's boundary, or Other (the Stop hook's summary, a local command's output).</summary>
    private static RecordEntry SystemEntry(ReadOnlySpan<byte> line, long offset, Found found, out RecordLine what)
    {
        var id = IdOf(line, offset, found, out what);
        if (found.IsSidechain)
        {
            return new(RecordKind.Other, id, Sidechain: true);
        }

        return found.Subtype switch
        {
            Subtype.TurnDuration => new(RecordKind.TurnEnd, id, DurationMs: found.DurationMs),
            Subtype.CompactBoundary => new(RecordKind.CompactBoundary, id),
            _ => new(RecordKind.Other, id),
        };
    }

    /// <summary>A queue operation: a message put in the queue, with its text, taken up, or taken out. It has no uuid: its id is always the local one.</summary>
    private static RecordEntry QueueEntry(ReadOnlySpan<byte> line, long offset, Found found, out RecordLine what)
    {
        what = RecordLine.Read;
        var id = found.Uuid == Absent ? LocalId(offset) : StringAt(line, found.Uuid);
        switch (found.Operation)
        {
            case Operation.Enqueue:
                return new(RecordKind.Enqueued, id, Text: StringOrNull(line, found.QueueContent));
            case Operation.Dequeue:
                return new(RecordKind.Dequeued, id);
            case Operation.Remove:
                return new(RecordKind.Removed, id);
            case Operation.None:
                what = RecordLine.Unknown;
                return new(RecordKind.Other, id);
            default:
                return new(RecordKind.Other, id);
        }
    }

    /// <summary>The entry's uuid, or its local id when it has none, which the reader counts (EG-3.5, EG-3.6).</summary>
    private static string IdOf(ReadOnlySpan<byte> line, long offset, Found found, out RecordLine what)
    {
        if (found.Uuid == Absent)
        {
            what = RecordLine.Unknown;
            return LocalId(offset);
        }

        what = RecordLine.Read;
        return StringAt(line, found.Uuid);
    }

    private static string LocalId(long offset) => LocalIdPrefix + offset.ToString(CultureInfo.InvariantCulture);

    /// <summary>Where the property's value starts when it is a string, else <see cref="Absent"/>; the value is passed over either way.</summary>
    private static int StringStart(ref Utf8JsonReader reader)
    {
        reader.Read();
        if (reader.TokenType == JsonTokenType.String)
        {
            return (int)reader.TokenStartIndex;
        }

        reader.Skip();
        return Absent;
    }

    private static bool IsTrue(ref Utf8JsonReader reader)
    {
        reader.Read();
        reader.Skip();
        return reader.TokenType == JsonTokenType.True;
    }

    private static long NumberOf(ref Utf8JsonReader reader)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.Number)
        {
            reader.Skip();
            return 0;
        }

        return reader.TryGetInt64(out var whole) ? whole : reader.TryGetDouble(out var real) ? (long)real : 0;
    }

    private static PromptOrigin OriginOf(ref Utf8JsonReader reader)
    {
        reader.Read();
        var origin = reader.TokenType != JsonTokenType.String ? PromptOrigin.None
            : reader.ValueTextEquals(TypedSource) ? PromptOrigin.Typed
            : reader.ValueTextEquals(QueuedSource) ? PromptOrigin.Queued
            : reader.ValueTextEquals(SystemSource) ? PromptOrigin.SelfStarted
            : PromptOrigin.None;
        reader.Skip();
        return origin;
    }

    private static Subtype SubtypeOf(ref Utf8JsonReader reader)
    {
        reader.Read();
        var subtype = reader.TokenType != JsonTokenType.String ? Subtype.None
            : reader.ValueTextEquals(TurnDurationSubtype) ? Subtype.TurnDuration
            : reader.ValueTextEquals(CompactBoundarySubtype) ? Subtype.CompactBoundary
            : Subtype.Other;
        reader.Skip();
        return subtype;
    }

    private static Operation OperationOf(ref Utf8JsonReader reader)
    {
        reader.Read();
        var operation = reader.TokenType != JsonTokenType.String ? Operation.None
            : reader.ValueTextEquals(EnqueueOperation) ? Operation.Enqueue
            : reader.ValueTextEquals(DequeueOperation) ? Operation.Dequeue
            : reader.ValueTextEquals(RemoveOperation) ? Operation.Remove
            : Operation.Other;
        reader.Skip();
        return operation;
    }

    private static BlockType BlockTypeOf(ref Utf8JsonReader reader)
    {
        reader.Read();
        var type = reader.TokenType != JsonTokenType.String ? BlockType.Other
            : reader.ValueTextEquals(TextBlock) ? BlockType.Text
            : reader.ValueTextEquals(ToolUseBlock) ? BlockType.ToolUse
            : reader.ValueTextEquals(ThinkingBlock) ? BlockType.Thinking
            : reader.ValueTextEquals(ToolResultBlock) ? BlockType.ToolResult
            : BlockType.Other;
        reader.Skip();
        return type;
    }

    /// <summary>The string value that starts at the offset, its escapes undone; the first look has checked the whole line.</summary>
    private static string StringAt(ReadOnlySpan<byte> line, int start)
    {
        var reader = new Utf8JsonReader(line[start..]);
        reader.Read();
        return reader.GetString() ?? "";
    }

    private static string? StringOrNull(ReadOnlySpan<byte> line, int start) => start == Absent ? null : StringAt(line, start);

    /// <summary>An ISO 8601 time (<c>2026-11-16T00:13:01.942Z</c>) in UTC, or null; for showing, never for order.</summary>
    private static DateTime? TimeAt(ReadOnlySpan<byte> line, int start)
    {
        if (start == Absent)
        {
            return null;
        }

        var reader = new Utf8JsonReader(line[start..]);
        reader.Read();
        return reader.TokenType == JsonTokenType.String && reader.TryGetDateTimeOffset(out var time) ? time.UtcDateTime : null;
    }

    /// <summary>
    /// Whether the string value at the offset starts with the ASCII prefix, read off its bytes as written with JSON's
    /// escapes undone for as many characters as the prefix has (Claude Code writes <c>&lt;</c> as <c>\u003c</c> in
    /// some fields); nothing of the rest of the value is read or copied.
    /// </summary>
    private static bool StartsWithAt(ReadOnlySpan<byte> line, int start, string prefix)
    {
        var reader = new Utf8JsonReader(line[start..]);
        if (!reader.Read() || reader.TokenType != JsonTokenType.String)
        {
            return false;
        }

        var raw = reader.ValueSpan;
        var at = 0;
        foreach (var expected in prefix)
        {
            if (at >= raw.Length)
            {
                return false;
            }

            int actual = raw[at++];
            if (actual == '\\' && at < raw.Length)
            {
                var escape = raw[at++];
                if (escape == 'u')
                {
                    if (at + 4 > raw.Length || !Utf8Parser.TryParse(raw.Slice(at, 4), out ushort code, out var used, 'X') || used != 4)
                    {
                        return false;
                    }

                    actual = code;
                    at += 4;
                }
                else
                {
                    actual = escape switch
                    {
                        (byte)'n' => '\n',
                        (byte)'r' => '\r',
                        (byte)'t' => '\t',
                        (byte)'b' => '\b',
                        (byte)'f' => '\f',
                        _ => escape,
                    };
                }
            }

            if (actual != expected)
            {
                return false;
            }
        }

        return true;
    }

    // ---- The screen key (EG-3.7) ----

    /// <summary>
    /// The letters of a reply's Markdown source as Claude Code's screen in screen reader mode shows them, for the
    /// comparison with the rows (EG-3.7; measured, EG 4.3): the rules that change letters are applied, then
    /// <see cref="RecordText.Letters"/>. A code fence's lines are dropped with the language named on them, and the
    /// lines between them kept as written; a table's header and separator lines are dropped and each body row becomes
    /// its header and cell pairs in order (<c>Header: cell. Header: cell.</c> on the screen); a link whose text is its
    /// target counts once (<c>[t](u)</c> is drawn <c>t (u)</c>, and only <c>u</c> when both are equal). Everything else
    /// needs no rule: headings' and quotes' marks, emphasis, backticks and list marks are no letters, and a row wrapped
    /// at the console width keeps its letters in order. Where the screen's rule is not known, more letters are kept
    /// rather than fewer, since the gate looks for a row's letters in the key and passes over letters the screen left out.
    /// </summary>
    public static string ScreenKey(string source)
    {
        var text = new StringBuilder(source.Length);
        var lines = source.Split('\n');
        var fence = "";
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var run = FenceRun(line.Trim());
            if (fence.Length > 0)
            {
                if (run.Length >= fence.Length && run[0] == fence[0] && line.Trim().Length == run.Length)
                {
                    fence = "";
                }
                else
                {
                    text.Append(line).Append('\n');
                }

                continue;
            }

            if (run.Length > 0)
            {
                fence = run;
                continue;
            }

            if (i + 1 < lines.Length && line.Contains('|') && IsTableSeparator(lines[i + 1]))
            {
                i = AppendTable(text, lines, i);
                continue;
            }

            text.Append(SameTargetLink().Replace(line, "$1")).Append('\n');
        }

        return RecordText.Letters(text.ToString());
    }

    /// <summary>The run of three or more backticks or tildes a fence line starts with, or an empty string.</summary>
    private static string FenceRun(string trimmed)
    {
        if (trimmed.Length < 3 || (trimmed[0] != '`' && trimmed[0] != '~'))
        {
            return "";
        }

        var length = 0;
        while (length < trimmed.Length && trimmed[length] == trimmed[0])
        {
            length++;
        }

        return length >= 3 ? trimmed[..length] : "";
    }

    /// <summary>A table's separator line, <c>| --- | :-: |</c>: pipes, hyphens, colons and spaces, with a pipe and a hyphen.</summary>
    private static bool IsTableSeparator(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Contains('|') && trimmed.Contains('-') && trimmed.All(c => c is '|' or '-' or ':' or ' ' or '\t');
    }

    /// <summary>A table's body rows as header and cell pairs, from the header line at <paramref name="header"/>; returns the index of its last row.</summary>
    private static int AppendTable(StringBuilder text, string[] lines, int header)
    {
        var headers = Cells(lines[header]);
        var row = header + 2;
        for (; row < lines.Length && lines[row].Contains('|'); row++)
        {
            var cells = Cells(lines[row]);
            for (var k = 0; k < Math.Max(headers.Length, cells.Length); k++)
            {
                if (k < headers.Length)
                {
                    text.Append(headers[k]).Append(' ');
                }

                if (k < cells.Length)
                {
                    text.Append(cells[k]).Append(' ');
                }
            }

            text.Append('\n');
        }

        return row - 1;
    }

    private static string[] Cells(string line)
    {
        var cells = SameTargetLink().Replace(line.Trim(), "$1");
        if (cells.StartsWith('|'))
        {
            cells = cells[1..];
        }

        if (cells.EndsWith('|'))
        {
            cells = cells[..^1];
        }

        return cells.Split('|');
    }

    /// <summary>A Markdown link whose text is its target, <c>[https://example.com](https://example.com)</c>.</summary>
    [GeneratedRegex(@"\[([^\]\r\n]+)\]\(\1\)")]
    private static partial Regex SameTargetLink();
}
