namespace AxClaude.Core.Record;

/// <summary>
/// The folder rule (docs/axClaude/events-and-grounding.md, "EG" below, EG-3.2 item 2): which session file is this
/// window's when no event named it. At each start of the program the window notes the files of its project folders
/// with their lengths (<see cref="Note"/>); once it has sent a message, its reader thread looks every 250 ms
/// (<see cref="Find"/>) at the files that grew or are new, reads only what each gained since the last look, and takes
/// the file whose new prompts hold one whose letters equal those of a message this window sent in this run. The
/// window stops looking 10 s after a send with no file found and starts again at the next send; the offsets read and
/// the prompts seen stay, so that a later look reads only what is new. <see cref="StartOf"/> says where a tail of a
/// file starts reading, whichever way the file was found. Neutral: it knows files, lines and
/// <see cref="RecordEntry"/>; the program's line reader is handed in. No thread and no timer of its own; used from
/// one thread at a time.
/// </summary>
public sealed class RecordFinder
{
    /// <summary>A record's file: one JSON object for each line, named by its session.</summary>
    public const string Extension = ".jsonl";

    /// <summary>The project folders noted, full paths without a trailing separator, each once.</summary>
    private readonly List<string> _folders = [];

    /// <summary>The length of each file of the project folders when they were noted.</summary>
    private readonly Dictionary<string, long> _noted = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The length of each file at the look that last read it, so that a file that did not change is not opened.</summary>
    private readonly Dictionary<string, long> _lengths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>A tail for each file that grew or is new, which keeps where its reading stopped and the bytes after its last LF.</summary>
    private readonly Dictionary<string, RecordTail> _tails = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The letters of the prompts each file gained since the note, so that a prompt read before its key was given still counts.</summary>
    private readonly Dictionary<string, HashSet<string>> _prompts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Notes the record files directly in each project folder with their lengths, as the program starts; a folder that
    /// does not exist yet is noted too, so that a file made there later is new. Forgets every earlier note and look.
    /// </summary>
    public void Note(IEnumerable<string> projectFolders)
    {
        _folders.Clear();
        _noted.Clear();
        _lengths.Clear();
        _tails.Clear();
        _prompts.Clear();
        foreach (var folder in projectFolders)
        {
            var full = FullFolder(folder);
            if (full is null || _folders.Contains(full, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            _folders.Add(full);
            foreach (var file in RecordFiles(full))
            {
                var length = LengthOf(file);
                if (length >= 0)
                {
                    _noted[file] = length;
                }
            }
        }
    }

    /// <summary>
    /// Where a tail of the file at <paramref name="path"/> starts reading (EG-3.3): the length noted for a file that was
    /// there at the note, 0 for a file in a noted project folder that was not (a new session, <c>/clear</c>), and
    /// <see cref="RecordTail.FromEnd"/> for any other path, which is read from its end.
    /// </summary>
    public long StartOf(string path)
    {
        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return RecordTail.FromEnd;
        }

        if (_noted.TryGetValue(full, out var length))
        {
            return length;
        }

        var folder = Path.GetDirectoryName(full);
        return folder is not null && _folders.Contains(folder, StringComparer.OrdinalIgnoreCase) ? 0 : RecordTail.FromEnd;
    }

    /// <summary>
    /// One look: the path of the file whose prompts since the note hold one whose letters (<see cref="RecordText.Letters"/>)
    /// equal one of <paramref name="sentKeys"/>, the letters of the messages this window sent in this run; null when
    /// none does yet, and at once when nothing was sent. Only the files that changed since the last look are read, and
    /// only what they gained. A prompt the program started by itself is never the window's. The files of one folder
    /// are looked at in the order of their names, so that two runs of a look give the same answer.
    /// </summary>
    public string? Find(IReadOnlyCollection<string> sentKeys, RecordLineParser parse)
    {
        var keys = sentKeys.Where(key => key.Length > 0).ToHashSet(StringComparer.Ordinal);
        if (keys.Count == 0)
        {
            return null;
        }

        foreach (var folder in _folders)
        {
            foreach (var file in RecordFiles(folder))
            {
                ReadGained(file, parse);
                if (_prompts.TryGetValue(file, out var prompts) && prompts.Overlaps(keys))
                {
                    return file;
                }
            }
        }

        return null;
    }

    /// <summary>Reads what the file gained since the last look, when its length changed, and keeps the letters of its prompts.</summary>
    private void ReadGained(string file, RecordLineParser parse)
    {
        var length = LengthOf(file);
        var before = _lengths.TryGetValue(file, out var looked) ? looked : _noted.GetValueOrDefault(file);
        if (length < 0 || length == before)
        {
            return;
        }

        _lengths[file] = length;
        if (!_tails.TryGetValue(file, out var tail))
        {
            tail = new RecordTail(file, StartOf(file), parse);
            _tails[file] = tail;
        }

        // The finder has no clock: a file it cannot open now is read at the next look at which it has grown, from
        // where its tail stopped, and never given up.
        foreach (var entry in tail.Poll(0))
        {
            if (entry.Kind == RecordKind.Prompt && entry.Origin != PromptOrigin.SelfStarted && entry.Text is not null)
            {
                if (!_prompts.TryGetValue(file, out var prompts))
                {
                    prompts = new HashSet<string>(StringComparer.Ordinal);
                    _prompts[file] = prompts;
                }

                prompts.Add(RecordText.Letters(entry.Text));
            }
        }
    }

    /// <summary>
    /// The record files directly in the folder, by name: <c>&lt;session&gt;.jsonl</c>. Never a file in a folder under
    /// it (an agent's record, a tool's result, memory), and never a name with a second dot, which is a copy the program
    /// set aside (<c>&lt;session&gt;.orphaned-….jsonl</c> for Claude Code) and not a session's own file.
    /// </summary>
    private static List<string> RecordFiles(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateFiles(folder, "*" + Extension, SearchOption.TopDirectoryOnly)
                .Where(IsRecordName)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static bool IsRecordName(string path)
    {
        var name = Path.GetFileName(path);
        return name.Length > Extension.Length
            && name.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)
            && !name[..^Extension.Length].Contains('.');
    }

    /// <summary>
    /// The file's length as the file system has it now, or -1 when it is gone or cannot be asked. Asked of the file
    /// itself, never taken from the folder's listing, whose sizes can lag behind a file another program holds open.
    /// </summary>
    private static long LengthOf(string file)
    {
        try
        {
            return new FileInfo(file).Length;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return -1;
        }
    }

    private static string? FullFolder(string folder)
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
