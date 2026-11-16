using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace Probe;

/// <summary>
/// Follows the session's transcript file, and every other .jsonl file of the scratch folder's project folder (a
/// session after /clear, subagents), by polling each file's length every 10 ms. A file is opened anew for each poll,
/// shared for reading, writing and deleting, so Claude Code is never held and a file replaced by a rename is seen
/// (its file index changes). Every complete line that appears is one record of transcript-arrivals.jsonl with the
/// probe's clock: the line unchanged, its offset and length. The folder is scanned for new files every 50 ms.
/// At the end each file is compared with what was logged: equal means the file only ever grew by whole appends.
/// </summary>
internal sealed class TranscriptWatcher
{
    private sealed class Tracked
    {
        public required string Path;
        public required string Label;
        public long Consumed;
        public long StartOffset;
        public byte[] Partial = [];
        public double PartialSince;
        public bool FoundWithContent;
        public bool FirstRead = true;
        public int Rewrites;
        public ulong Index;
        public long Lines;
        public MemoryStream Logged = new();
        public string? OpenError;
        public int FailedOpens;
    }

    private readonly object _gate = new();
    private readonly Dictionary<string, Tracked> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _roots = [];
    private readonly HashSet<string> _others = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(double T, string Line)> _lines = [];
    private readonly Jsonl _log;
    private readonly string _projectFolderName;
    private readonly string[] _scratch;
    private Thread? _thread;
    private volatile bool _stop;

    /// <param name="projectFolderName">The name Claude Code gives the working folder's project folder: only a folder of this name is followed whole.</param>
    /// <param name="scratch">Folders of the run itself (the working folder, the output folder): a file under them may be followed with its folder too.</param>
    public TranscriptWatcher(Jsonl log, string projectFolderName, string[] scratch)
    {
        _log = log;
        _projectFolderName = projectFolderName;
        _scratch = scratch;
    }

    /// <summary>The project folder name Claude Code makes of a working folder: every character that is no letter or digit becomes a hyphen (sessions page).</summary>
    public static string ProjectFolderName(string cwd) => Regex.Replace(cwd.TrimEnd('\\', '/'), "[^A-Za-z0-9]", "-");

    /// <summary>A folder to follow whole. With <paramref name="initial"/>, what is in it now was there before the run: followed from its present end.</summary>
    public void AddRoot(string folder, bool initial)
    {
        lock (_gate)
        {
            // A folder under one already followed is covered by it.
            if (_roots.Any(root => Under(folder, root)))
            {
                return;
            }

            _roots.Add(folder);
            _log.Write(new JsonObject { ["t"] = Clock.Ms, ["wall"] = Clock.Wall, ["k"] = "root", ["folder"] = folder, ["exists"] = Directory.Exists(folder), ["initial"] = initial });
            Scan(initial);
        }
    }

    /// <summary>A file a hook named (transcript_path): followed at once, existing or not, and its folder with it when it is the scratch folder's own.</summary>
    public void Track(string path, string why)
    {
        path = System.IO.Path.GetFullPath(path);
        var folder = System.IO.Path.GetDirectoryName(path);
        lock (_gate)
        {
            if (folder is not null && Allowed(folder))
            {
                AddRoot(folder, false);
            }

            if (!_files.ContainsKey(path))
            {
                TrackLocked(path, why, false);
            }
        }
    }

    public void Start()
    {
        _thread = new Thread(Loop) { IsBackground = true, Name = "transcript-watch" };
        _thread.Start();
    }

    /// <summary>Whether a line that arrived at or after <paramref name="since"/> matches.</summary>
    public bool AnySince(Regex pattern, double since)
    {
        lock (_gate)
        {
            for (var i = _lines.Count - 1; i >= 0 && _lines[i].T >= since; i--)
            {
                if (pattern.IsMatch(_lines[i].Line))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Stops, reads what is left, compares each file with what was logged and copies it into the output folder.</summary>
    public JsonArray Finish(string outputFolder)
    {
        _stop = true;
        _thread?.Join(2000);
        var summary = new JsonArray();
        lock (_gate)
        {
            Scan(false);
            PollAll();
            var copies = System.IO.Path.Combine(outputFolder, "transcript");
            foreach (var tracked in _files.Values.OrderBy(tracked => tracked.Label, StringComparer.Ordinal))
            {
                var entry = new JsonObject
                {
                    ["file"] = tracked.Label,
                    ["lines"] = tracked.Lines,
                    ["startOffset"] = tracked.StartOffset,
                    ["rewrites"] = tracked.Rewrites,
                    ["partialBytesAtEnd"] = tracked.Partial.Length,
                    ["failedOpens"] = tracked.FailedOpens,
                };
                try
                {
                    var final = ReadShared(tracked.Path);
                    entry["finalBytes"] = final.Length;
                    var logged = tracked.Logged.ToArray();
                    var expected = final.AsSpan((int)Math.Min(tracked.StartOffset, final.Length));
                    var same = expected.Length == logged.Length + tracked.Partial.Length
                        && expected[..logged.Length].SequenceEqual(logged)
                        && expected[logged.Length..].SequenceEqual(tracked.Partial);
                    entry["appendOnly"] = same && tracked.Rewrites == 0;
                    Directory.CreateDirectory(copies);
                    File.WriteAllBytes(System.IO.Path.Combine(copies, tracked.Label.Replace('\\', '_').Replace('/', '_')), final);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    entry["finalBytes"] = -1;
                    entry["error"] = ex.GetType().Name;
                }

                summary.Add(entry);
            }
        }

        return summary;
    }

    private bool Allowed(string folder)
    {
        if (System.IO.Path.GetFileName(folder.TrimEnd('\\', '/')).Equals(_projectFolderName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _scratch.Any(scratch => Under(folder, scratch)) || _roots.Any(root => Under(folder, root));
    }

    /// <summary>The path is the folder or lies under it.</summary>
    private static bool Under(string path, string folder)
    {
        folder = folder.TrimEnd('\\', '/');
        return path.Equals(folder, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(folder + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private void Loop()
    {
        var round = 0;
        while (!_stop)
        {
            lock (_gate)
            {
                if (round++ % 5 == 0)
                {
                    Scan(false);
                }

                PollAll();
            }

            Thread.Sleep(10);
        }
    }

    private void Scan(bool initial)
    {
        foreach (var root in _roots.ToList())
        {
            List<string> files;
            try
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files)
            {
                if (file.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_files.ContainsKey(file))
                    {
                        TrackLocked(file, initial ? "there before the run" : "folder scan", initial);
                    }
                }
                else if (_others.Add(file) && !initial)
                {
                    long length = -1;
                    try
                    {
                        length = new FileInfo(file).Length;
                    }
                    catch (IOException)
                    {
                    }

                    _log.Write(new JsonObject { ["t"] = Clock.Ms, ["wall"] = Clock.Wall, ["k"] = "other-file", ["file"] = LabelOf(file), ["bytes"] = length });
                }
            }
        }
    }

    private void TrackLocked(string path, string why, bool preexisting)
    {
        var tracked = new Tracked { Path = path, Label = LabelOf(path) };
        long length = 0;
        try
        {
            if (File.Exists(path))
            {
                length = ReadLength(path);
            }
        }
        catch (IOException)
        {
        }

        if (preexisting)
        {
            tracked.Consumed = tracked.StartOffset = length;
        }
        else
        {
            tracked.FoundWithContent = length > 0;
        }

        _files[path] = tracked;
        _log.Write(new JsonObject
        {
            ["t"] = Clock.Ms, ["wall"] = Clock.Wall, ["k"] = "tracked", ["file"] = tracked.Label, ["why"] = why,
            ["bytesWhenFound"] = length, ["startOffset"] = tracked.StartOffset,
        });
    }

    private string LabelOf(string path)
    {
        foreach (var root in _roots)
        {
            if (Under(path, root))
            {
                var parent = System.IO.Path.GetDirectoryName(root.TrimEnd('\\', '/')) ?? root;
                return path[parent.Length..].TrimStart('\\', '/');
            }
        }

        return System.IO.Path.GetFileName(path);
    }

    private void PollAll()
    {
        foreach (var tracked in _files.Values)
        {
            Poll(tracked);
        }
    }

    private void Poll(Tracked tracked)
    {
        try
        {
            using var stream = new FileStream(tracked.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1);
            var length = stream.Length;
            var index = FileIndex(stream.SafeFileHandle);
            var now = Clock.Ms;
            if (tracked.Index != 0 && index != 0 && index != tracked.Index)
            {
                _log.Write(new JsonObject { ["t"] = now, ["wall"] = Clock.Wall, ["k"] = "replaced", ["file"] = tracked.Label, ["bytes"] = length });
                Reset(tracked);
            }

            tracked.Index = index;
            if (length < tracked.Consumed)
            {
                _log.Write(new JsonObject { ["t"] = now, ["wall"] = Clock.Wall, ["k"] = "truncated", ["file"] = tracked.Label, ["from"] = tracked.Consumed, ["to"] = length });
                Reset(tracked);
            }

            if (length == tracked.Consumed)
            {
                return;
            }

            var count = (int)(length - tracked.Consumed);
            var buffer = new byte[count];
            stream.Seek(tracked.Consumed, SeekOrigin.Begin);
            var read = 0;
            while (read < count)
            {
                var got = stream.Read(buffer, read, count - read);
                if (got <= 0)
                {
                    break;
                }

                read += got;
            }

            var lineStart = tracked.Consumed - tracked.Partial.Length;
            var data = new byte[tracked.Partial.Length + read];
            Buffer.BlockCopy(tracked.Partial, 0, data, 0, tracked.Partial.Length);
            Buffer.BlockCopy(buffer, 0, data, tracked.Partial.Length, read);
            var hadPartial = tracked.Partial.Length > 0;
            tracked.Consumed += read;
            var position = 0;
            while (true)
            {
                var newline = Array.IndexOf(data, (byte)'\n', position);
                if (newline < 0)
                {
                    break;
                }

                var bytes = newline - position + 1;
                var text = Encoding.UTF8.GetString(data, position, bytes).TrimEnd('\n', '\r');
                var record = new JsonObject
                {
                    ["t"] = now,
                    ["wall"] = Clock.Wall,
                    ["k"] = "line",
                    ["file"] = tracked.Label,
                    ["offset"] = lineStart + position,
                    ["bytes"] = bytes,
                };
                if (tracked.FirstRead && tracked.FoundWithContent)
                {
                    // Already in the file when the folder scan found it: the time is an upper bound (the scan runs every 50 ms).
                    record["first"] = true;
                }

                if (tracked.Rewrites > 0)
                {
                    record["afterRewrite"] = tracked.Rewrites;
                }

                if (position == 0 && hadPartial)
                {
                    // The line's first bytes were seen in an earlier poll: it was not written in one piece.
                    record["tornMs"] = Math.Round(now - tracked.PartialSince, 1);
                }

                record["line"] = text;
                _log.Write(record);
                tracked.Logged.Write(data, position, bytes);
                tracked.Lines++;
                _lines.Add((now, text));
                position = newline + 1;
            }

            tracked.Partial = data[position..];
            if (tracked.Partial.Length > 0 && (!hadPartial || position > 0))
            {
                tracked.PartialSince = now;
            }

            tracked.FirstRead = false;
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            tracked.FailedOpens++;
            if (tracked.OpenError != ex.Message)
            {
                tracked.OpenError = ex.Message;
                _log.Write(new JsonObject { ["t"] = Clock.Ms, ["wall"] = Clock.Wall, ["k"] = "open-failed", ["file"] = tracked.Label, ["error"] = ex.GetType().Name + ": " + ex.Message });
            }
        }
    }

    private static void Reset(Tracked tracked)
    {
        tracked.Consumed = 0;
        tracked.StartOffset = 0;
        tracked.Partial = [];
        tracked.Logged = new MemoryStream();
        tracked.Rewrites++;
    }

    private static long ReadLength(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1);
        return stream.Length;
    }

    private static byte[] ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static ulong FileIndex(SafeFileHandle handle) =>
        GetFileInformationByHandle(handle, out var info) ? ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow : 0;

    // FILETIME is two DWORDs: packed at 4 so that the 64-bit fields sit where Windows puts them.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out ByHandleFileInformation info);
}
