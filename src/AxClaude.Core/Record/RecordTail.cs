namespace AxClaude.Core.Record;

/// <summary>
/// A program's reader of one line of its record, without its line end (for Claude Code,
/// <c>ClaudeCodeTranscript.Parse</c>, passed as a method group): the entry, or null for a line of a kind not read and
/// for one that is no JSON object, and in <paramref name="what"/> which of those the line was, for the counters and
/// the stop rules of <see cref="RecordTail"/> (docs/axClaude/events-and-grounding.md EG-3.6). <paramref name="offset"/>
/// is the line's place in the file, from which an entry without an id of its own gets one.
/// </summary>
public delegate RecordEntry? RecordLineParser(ReadOnlySpan<byte> line, long offset, out RecordLine what);

/// <summary>What <see cref="RecordTail"/> found at its last look.</summary>
public enum RecordTailState
{
    /// <summary>The file was opened and read, or it has not been looked at yet.</summary>
    Following,

    /// <summary>The file was not there: a session's file exists only from its first entry, and is followed once it does.</summary>
    Missing,

    /// <summary>Stopped for good (<see cref="RecordTail.StopReason"/>): nothing more is read from this file.</summary>
    Stopped,
}

/// <summary>Why <see cref="RecordTail"/> stopped (EG-3.6), for the one word of the diagnostics.</summary>
public enum RecordTailStop
{
    None,

    /// <summary>Three lines in a row were no JSON object.</summary>
    NotJson,

    /// <summary>More than ten of the last fifty entries of the kinds read lacked what their kind needs.</summary>
    Fields,

    /// <summary>The file could not be opened or read for two seconds.</summary>
    Unreadable,
}

/// <summary>
/// The reader of one session file as the program appends to it (docs/axClaude/events-and-grounding.md, "EG" below,
/// EG-3.3 and EG-3.6). Neutral: the program's own line reader is handed in, and this class knows only bytes, lines and
/// <see cref="RecordEntry"/>. It has no thread and no timer: the window's reader thread calls <see cref="Poll"/> with
/// its clock as often as EG-3.3 says, and one tail is used from one thread at a time. Each look opens the file anew,
/// shared for reading, writing and deleting so that the program's own handle is never in the way, reads the bytes it
/// gained, splits them at LF and hands each line to the reader; the bytes after the last LF wait for their LF, so that
/// a line is never read in two pieces (none was seen half written, but nothing promises it). A file shorter than what
/// was read (the program set it aside or wrote it anew) is read again from its start, and an entry whose id was
/// already returned is dropped. The entries come back in file order, never sorted by their times.
/// </summary>
public sealed class RecordTail
{
    /// <summary>The start for a file found without a note of its length: its end at the first look (EG-3.3); a file that is not there at the first look is new and is read from its start.</summary>
    public const long FromEnd = -1;

    /// <summary>The longest line read (EG-3.3); a longer one is passed over to its line end and counted. Claude Code's lines were measured up to 533 KB.</summary>
    public const int MaxLineBytes = 4 * 1024 * 1024;

    /// <summary>This many lines in a row that are no JSON object stop the reader (EG-3.6).</summary>
    public const int NotJsonInARow = 3;

    /// <summary>The last this many entries of the kinds read are watched for entries that lacked their fields (EG-3.6).</summary>
    public const int FieldsWindow = 50;

    /// <summary>More than this many entries without their fields among the last <see cref="FieldsWindow"/> stop the reader (EG-3.6).</summary>
    public const int FieldsFaults = 10;

    /// <summary>A file that cannot be opened or read for this long stops the reader (EG-3.6).</summary>
    public const long UnreadableMs = 2000;

    private const byte LineEnd = (byte)'\n';
    private const byte CarriageReturn = (byte)'\r';

    /// <summary>The bytes read from the file at once.</summary>
    private const int ChunkBytes = 64 * 1024;

    /// <summary>A buffer for a waiting line larger than this is given back once its line is read: such lines are rare.</summary>
    private const int KeptPendingBytes = 1024 * 1024;

    private readonly RecordLineParser _parse;

    /// <summary>Where reading stops: the end of the file, or the end <see cref="ReadAll"/> was given.</summary>
    private readonly long _end;

    /// <summary>The ids of the entries returned, so that a file read again from its start returns none of them twice.</summary>
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

    /// <summary>The last <see cref="FieldsWindow"/> entries, true for one that lacked its fields, as a ring.</summary>
    private readonly bool[] _fields = new bool[FieldsWindow];

    private int _fieldsCount;
    private int _fieldsNext;
    private int _fieldsFaults;
    private int _notJsonRun;

    /// <summary>The bytes of the file taken so far, those of the waiting line too: the next look reads from here.</summary>
    private long _read;

    /// <summary>The start is the file's end at the first look (<see cref="FromEnd"/>).</summary>
    private bool _fromEnd;

    /// <summary>The first look that opens the file checks that the start is a line's start.</summary>
    private bool _checkStart;

    private bool _firstLook = true;

    /// <summary>The rest of a line is passed over to its line end: a line too long, or a start inside a line.</summary>
    private bool _skipLine;

    /// <summary>The bytes of the line whose line end has not come yet, and where in the file that line starts.</summary>
    private byte[] _pending = [];

    private int _pendingLength;
    private long _pendingStart;

    /// <summary>When the file first could not be opened or read in a row of failed looks, or -1.</summary>
    private long _unreadableSince = -1;

    private byte[]? _chunk;

    /// <summary>
    /// A tail of the file at <paramref name="path"/> that reads from <paramref name="start"/>, in bytes: the length the
    /// file had when the program was started, 0 for a file that did not exist then, or <see cref="FromEnd"/> for a
    /// file found without that note (EG-3.3). A start that falls inside a line passes over the rest of that line.
    /// </summary>
    public RecordTail(string path, long start, RecordLineParser parse)
        : this(path, start, parse, long.MaxValue)
    {
    }

    private RecordTail(string path, long start, RecordLineParser parse, long end)
    {
        FilePath = path;
        _parse = parse;
        _end = end;
        _fromEnd = start < 0;
        _read = Math.Max(start, 0);
        _checkStart = start != 0;
    }

    /// <summary>The file followed.</summary>
    public string FilePath { get; }

    /// <summary>What the last look found (<see cref="RecordTailState"/>).</summary>
    public RecordTailState State { get; private set; } = RecordTailState.Following;

    /// <summary>Why the tail stopped, <see cref="RecordTailStop.None"/> while it follows the file.</summary>
    public RecordTailStop StopReason { get; private set; }

    /// <summary>Lines read as an entry in a shape known. A line read again after the file shrank is counted again.</summary>
    public int Read { get; private set; }

    /// <summary>Lines of a kind not read, passed over at their type (EG-3.4).</summary>
    public int Skipped { get; private set; }

    /// <summary>Lines that were no JSON object.</summary>
    public int NotJson { get; private set; }

    /// <summary>Lines over <see cref="MaxLineBytes"/>, passed over unread.</summary>
    public int TooLong { get; private set; }

    /// <summary>Entries of a kind read that lacked what their kind needs; returned all the same (EG-3.5, EG-3.6).</summary>
    public int Unknown { get; private set; }

    /// <summary>Entries dropped because an entry of the same id was returned before.</summary>
    public int Repeated { get; private set; }

    /// <summary>
    /// The entries of the whole file at <paramref name="path"/>, in file order, up to <paramref name="end"/> bytes (the
    /// length noted at a start, so that history and what a tail reads from there never overlap): one look by the same
    /// rules as <see cref="Poll"/>, off the UI thread. A line that has no line end before the end is not read; a file
    /// that is missing or cannot be opened gives no entries; the stop rules end the reading where they apply.
    /// </summary>
    public static IReadOnlyList<RecordEntry> ReadAll(string path, RecordLineParser parse, long end = long.MaxValue) =>
        new RecordTail(path, 0, parse, end).Poll(0);

    /// <summary>
    /// One look at the file: the entries of the lines it gained since the last look, in file order, none twice. The
    /// time is the caller's clock in milliseconds (the window's <c>Environment.TickCount64</c>, a test's own numbers),
    /// used only to stop the reader when the file could not be opened for <see cref="UnreadableMs"/>. Never throws.
    /// After a stop it returns nothing.
    /// </summary>
    public IReadOnlyList<RecordEntry> Poll(long nowMs)
    {
        var entries = new List<RecordEntry>();
        if (State == RecordTailState.Stopped)
        {
            return entries;
        }

        FileStream file;
        try
        {
            file = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 0);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            Missing();
            return entries;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // Held by another program without sharing, a delete under way, no right to it: tried again at the next look.
            Unreadable(nowMs);
            return entries;
        }

        using (file)
        {
            try
            {
                ReadNew(file, entries);
                _unreadableSince = -1;
                if (State != RecordTailState.Stopped)
                {
                    State = RecordTailState.Following;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // What was read before the failure stands; the next look goes on from there.
                Unreadable(nowMs);
            }
        }

        return entries;
    }

    private void Missing()
    {
        if (_firstLook && _fromEnd)
        {
            // Not there at the first look: a new file, all of which is the session's from its first line.
            _fromEnd = false;
            _checkStart = false;
            _read = 0;
        }

        _firstLook = false;
        _unreadableSince = -1;
        State = RecordTailState.Missing;
    }

    private void Unreadable(long nowMs)
    {
        if (_unreadableSince < 0)
        {
            _unreadableSince = nowMs;
        }
        else if (nowMs - _unreadableSince >= UnreadableMs)
        {
            StopFor(RecordTailStop.Unreadable);
        }
    }

    private void ReadNew(FileStream file, List<RecordEntry> entries)
    {
        _firstLook = false;
        var length = file.Length;
        if (_fromEnd)
        {
            _fromEnd = false;
            _read = length;
        }

        if (length < _read)
        {
            // Shorter than what was read: replaced or set aside. Read again from the start; what was returned is
            // dropped by its id.
            _read = 0;
            _pendingLength = 0;
            _skipLine = false;
            _checkStart = false;
        }
        else if (_checkStart && _read > 0)
        {
            // A start inside a line (the program was writing it when the length was taken) passes over its rest.
            file.Position = _read - 1;
            _skipLine = file.ReadByte() != LineEnd;
        }

        _checkStart = false;
        var limit = Math.Min(length, _end);
        if (limit <= _read)
        {
            return;
        }

        file.Position = _read;
        _chunk ??= new byte[ChunkBytes];
        while (_read < limit && State != RecordTailState.Stopped)
        {
            var count = file.Read(_chunk, 0, (int)Math.Min(_chunk.Length, limit - _read));
            if (count == 0)
            {
                break;
            }

            Take(_chunk.AsSpan(0, count), _read, entries);
            _read += count;
        }
    }

    /// <summary>The bytes of one read, which start at <paramref name="chunkStart"/> in the file, split into lines.</summary>
    private void Take(ReadOnlySpan<byte> chunk, long chunkStart, List<RecordEntry> entries)
    {
        var at = 0;
        while (at < chunk.Length && State != RecordTailState.Stopped)
        {
            var rest = chunk[at..];
            var end = rest.IndexOf(LineEnd);
            if (end < 0)
            {
                Hold(rest, chunkStart + at);
                return;
            }

            var piece = rest[..end];
            if (_skipLine)
            {
                _skipLine = false;
            }
            else if (_pendingLength == 0)
            {
                Line(piece, chunkStart + at, entries);
            }
            else if ((long)_pendingLength + piece.Length > MaxLineBytes)
            {
                _pendingLength = 0;
                PassedOver();
            }
            else
            {
                Append(piece);
                Line(_pending.AsSpan(0, _pendingLength), _pendingStart, entries);
                _pendingLength = 0;
                if (_pending.Length > KeptPendingBytes)
                {
                    _pending = [];
                }
            }

            at += end + 1;
        }
    }

    /// <summary>The bytes after the last LF of a read: kept until their LF comes, or passed over when the line is too long.</summary>
    private void Hold(ReadOnlySpan<byte> rest, long offset)
    {
        if (_skipLine)
        {
            return;
        }

        if (_pendingLength == 0)
        {
            _pendingStart = offset;
        }

        if ((long)_pendingLength + rest.Length > MaxLineBytes)
        {
            _pendingLength = 0;
            _pending = [];
            _skipLine = true;
            PassedOver();
            return;
        }

        Append(rest);
    }

    private void Append(ReadOnlySpan<byte> bytes)
    {
        var needed = _pendingLength + bytes.Length;
        if (_pending.Length < needed)
        {
            var grown = new byte[Math.Max(needed, Math.Max(_pending.Length * 2, 4096))];
            _pending.AsSpan(0, _pendingLength).CopyTo(grown);
            _pending = grown;
        }

        bytes.CopyTo(_pending.AsSpan(_pendingLength));
        _pendingLength = needed;
    }

    /// <summary>A line too long to read: counted once, and it ends a run of lines that are no JSON.</summary>
    private void PassedOver()
    {
        TooLong++;
        _notJsonRun = 0;
    }

    /// <summary>One whole line without its LF, which starts at <paramref name="offset"/> in the file.</summary>
    private void Line(ReadOnlySpan<byte> line, long offset, List<RecordEntry> entries)
    {
        if (line.Length > MaxLineBytes)
        {
            PassedOver();
            return;
        }

        if (line.IsEmpty || (line.Length == 1 && line[0] == CarriageReturn))
        {
            // An empty line says nothing and counts for nothing.
            return;
        }

        var entry = _parse(line, offset, out var what);
        switch (what)
        {
            case RecordLine.Read:
                Read++;
                break;
            case RecordLine.Skipped:
                Skipped++;
                break;
            case RecordLine.NotJson:
                NotJson++;
                break;
            default:
                Unknown++;
                break;
        }

        _notJsonRun = what == RecordLine.NotJson ? _notJsonRun + 1 : 0;
        if (_notJsonRun >= NotJsonInARow)
        {
            StopFor(RecordTailStop.NotJson);
            return;
        }

        if (entry is null)
        {
            return;
        }

        CountFields(what == RecordLine.Unknown);
        if (_seen.Add(entry.Id))
        {
            entries.Add(entry);
        }
        else
        {
            Repeated++;
        }

        if (_fieldsFaults > FieldsFaults)
        {
            StopFor(RecordTailStop.Fields);
        }
    }

    /// <summary>One more entry of a kind read into the ring of the last <see cref="FieldsWindow"/>.</summary>
    private void CountFields(bool lacked)
    {
        if (_fieldsCount == FieldsWindow)
        {
            if (_fields[_fieldsNext])
            {
                _fieldsFaults--;
            }
        }
        else
        {
            _fieldsCount++;
        }

        _fields[_fieldsNext] = lacked;
        if (lacked)
        {
            _fieldsFaults++;
        }

        _fieldsNext = (_fieldsNext + 1) % FieldsWindow;
    }

    private void StopFor(RecordTailStop reason)
    {
        State = RecordTailState.Stopped;
        StopReason = reason;
        _pending = [];
        _pendingLength = 0;
        _seen.Clear();
    }
}
