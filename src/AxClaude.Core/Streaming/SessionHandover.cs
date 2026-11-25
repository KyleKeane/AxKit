using AxClaude.Core.Record;

namespace AxClaude.Core.Streaming;

/// <summary>
/// Which conversation a start of Claude Code takes up, and where its older part is read from (docs/axClaude/stream-mode.md
/// SM-8.1 to SM-8.3; stage 6). Measured with Claude Code 2.1.332 (section 13): a session is one file whichever source
/// wrote it, and either source takes the other's up by its id (<c>--resume</c>) and appends to that same file; the
/// stream writes nothing of the older conversation when it takes one up, where the screen draws it again; the
/// interactive program's <c>--continue</c> passes over every session the stream began, even after the screen has
/// taken it up, while the stream's own <c>--continue</c> takes the folder's newest session. So a window the stream
/// feeds names the session itself (the folder's newest, read before the start), and a window the screen feeds is
/// handed the id of the session a stream window last ran in the folder, when that is still the folder's newest.
/// Pure but for <see cref="Newest"/> and <see cref="Read"/>, which look at files; no id is ever shown, said or logged.
/// </summary>
public static class SessionHandover
{
    /// <summary>
    /// Whether a text is a session's id as Claude Code makes them, a UUID in its usual form: the only ids put on a
    /// command line, kept in the settings or made into a file's name, whatever a file or a line of the stream held.
    /// </summary>
    public static bool IsSessionId(string? id) => id is { Length: 36 } && Guid.TryParseExact(id, "D", out _);

    /// <summary>The project folders of a working folder, one under each projects folder a session file may lie in (<see cref="ClaudeCodeTranscript.ProjectsFolders"/>).</summary>
    public static IReadOnlyList<string> ProjectFolders(IReadOnlyList<string> projectsFolders, string workingFolder)
    {
        var name = ClaudeCodeTranscript.ProjectFolderName(Path.GetFullPath(workingFolder));
        return projectsFolders.Select(projects => Path.Combine(projects, name)).ToList();
    }

    /// <summary>
    /// The id of the folder's newest session: the session file written last, directly in one of the project folders,
    /// whose name is a session's id (never an agent's file in a folder under it, nor a copy set aside, whose name has
    /// a second dot). Null when there is none. What the stream's own <c>--continue</c> takes up (measured).
    /// </summary>
    public static string? Newest(IEnumerable<string> projectFolders)
    {
        string? newest = null;
        var newestTime = DateTime.MinValue;
        foreach (var folder in projectFolders)
        {
            foreach (var (id, written) in Sessions(folder))
            {
                if (newest is null || written > newestTime)
                {
                    newest = id;
                    newestTime = written;
                }
            }
        }

        return newest;
    }

    /// <summary>The session file of an id in the first project folder that holds it; null when none does or the id is no session's.</summary>
    public static string? FileOf(IEnumerable<string> projectFolders, string? id)
    {
        if (!IsSessionId(id))
        {
            return null;
        }

        foreach (var folder in projectFolders)
        {
            var file = Path.Combine(folder, id + ClaudeCodeTranscript.SessionFileExtension);
            if (File.Exists(file))
            {
                return file;
            }
        }

        return null;
    }

    /// <summary>The id a command line takes a session up by (<c>--resume &lt;id&gt;</c>, <c>-r &lt;id&gt;</c>), or null: also for a bare <c>--resume</c>, which opens the interactive program's own list.</summary>
    public static string? ResumedIn(IReadOnlyList<string> arguments)
    {
        for (var i = 0; i + 1 < arguments.Count; i++)
        {
            if (arguments[i] is ClaudeCodeStream.ResumeOption or ClaudeCodeStream.ResumeShortOption && IsSessionId(arguments[i + 1]))
            {
                return arguments[i + 1];
            }
        }

        return null;
    }

    /// <summary>
    /// Claude Code's arguments for a window the stream feeds (SM-8.1): each <c>--continue</c> of the window's
    /// arguments (its default, or the user's own) becomes <c>--resume</c> with the folder's newest session, so that
    /// the window knows before the start which conversation is taken up and can show it; with no session in the
    /// folder it goes, and a new conversation begins, as the stream's own <c>--continue</c> begins one. Every other
    /// argument stays as it is.
    /// </summary>
    public static IReadOnlyList<string> ForStream(IReadOnlyList<string> arguments, string? newest)
    {
        if (!arguments.Contains(ClaudeCodeStream.ContinueOption))
        {
            return arguments;
        }

        var result = new List<string>();
        foreach (var argument in arguments)
        {
            if (argument != ClaudeCodeStream.ContinueOption)
            {
                result.Add(argument);
            }
            else if (IsSessionId(newest) && ResumedIn(result) is null)
            {
                result.Add(ClaudeCodeStream.ResumeOption);
                result.Add(newest!);
            }
        }

        return result;
    }

    /// <summary>
    /// Claude Code's arguments for a window the screen feeds (SM-8.3): the interactive program's <c>--continue</c>
    /// passes over a session the stream began, so where the arguments continue the folder's last conversation (the
    /// window's default, or the user's own <c>--continue</c>) and the session a window the stream feeds last ran in
    /// the folder (<paramref name="kept"/>) is still the folder's newest, <c>--continue</c> becomes <c>--resume</c>
    /// with that session. Otherwise the arguments stay as they are, and <c>--continue</c> takes up a later
    /// conversation of the screen's, as always.
    /// </summary>
    public static IReadOnlyList<string> ForScreen(IReadOnlyList<string> arguments, string? kept, string? newest)
    {
        if (!arguments.Contains(ClaudeCodeStream.ContinueOption) || ResumedIn(arguments) is not null
            || !IsSessionId(kept) || !string.Equals(kept, newest, StringComparison.OrdinalIgnoreCase))
        {
            return arguments;
        }

        var result = new List<string>();
        foreach (var argument in arguments)
        {
            if (argument != ClaudeCodeStream.ContinueOption)
            {
                result.Add(argument);
            }
            else if (ResumedIn(result) is null)
            {
                result.Add(ClaudeCodeStream.ResumeOption);
                result.Add(kept!);
            }
        }

        return result;
    }

    /// <summary>
    /// The entries of a session file, whole, in file order (<see cref="RecordTail"/> with Claude Code's reader), for
    /// the older conversation of a session taken up (SM-8.2); off the window's thread. Null when the file could not
    /// be read: not there, or no line of it read at all while it holds bytes. Never throws.
    /// </summary>
    public static IReadOnlyList<RecordEntry>? Read(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var tail = new RecordTail(path, 0, ClaudeCodeTranscript.Parse);
            var entries = tail.Poll(0);
            var lines = tail.Read + tail.Skipped + tail.Unknown + tail.TooLong + tail.NotJson;
            return lines == 0 && new FileInfo(path).Length > 0 ? null : entries;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>The session files directly in a folder, by id, with when each was written last.</summary>
    private static IEnumerable<(string Id, DateTime Written)> Sessions(string folder)
    {
        List<(string, DateTime)> found = [];
        try
        {
            if (!Directory.Exists(folder))
            {
                return found;
            }

            foreach (var file in Directory.EnumerateFiles(folder, "*" + ClaudeCodeTranscript.SessionFileExtension, SearchOption.TopDirectoryOnly))
            {
                var id = Path.GetFileNameWithoutExtension(file);
                if (IsSessionId(id))
                {
                    found.Add((id, File.GetLastWriteTimeUtc(file)));
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // A folder that cannot be listed holds no session the window can take up.
        }

        return found;
    }
}
