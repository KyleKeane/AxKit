using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AxAutomate.Core;

/// <summary>What NVDA spoke, from the capture add-on's log (tools/nvda-capture): "speak" lines, and "[cancel]" where NVDA cut its speech.</summary>
public sealed class Speech
{
    public static readonly string FlagPath = Path.Combine(Path.GetTempPath(), "axkit-nvda-capture.on");
    public static readonly string LogPath = Path.Combine(Path.GetTempPath(), "axkit-nvda-speech.log");
    private readonly List<string> _all = [];
    // Each script's reader starts where the log ends, so that one script never matches what NVDA said in the one before.
    private long _read = File.Exists(LogPath) ? new FileInfo(LogPath).Length : 0;
    private int _taken;

    /// <summary>
    /// Waits until NVDA has started nothing new for <paramref name="quietMs"/> (the log's length unchanged), at most
    /// 30 s: Windows' voice never speaks over NVDA, whose ducking lowered it, and the end is said only after the windows
    /// closing have been read (the user heard NVDA after "finished" on 2026-10-31). The log holds when a sequence starts,
    /// not how long it lasts, so the quiet is long.
    /// </summary>
    public static void WaitForQuiet(int quietMs)
    {
        var clock = Stopwatch.StartNew();
        var length = -1L;
        var since = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < 30000)
        {
            var now = File.Exists(LogPath) ? new FileInfo(LogPath).Length : 0;
            if (now != length)
            {
                length = now;
                since.Restart();
            }
            else if (since.ElapsedMilliseconds >= quietMs)
            {
                return;
            }

            Thread.Sleep(200);
        }
    }

    public static long LogLength() => File.Exists(LogPath) ? new FileInfo(LogPath).Length : 0;

    /// <summary>
    /// After a key of the NVDA tier: until NVDA has started to say something for it (the log longer than
    /// <paramref name="lengthBefore"/>), at most <paramref name="maxMs"/>, as a user listens before the next key. A key
    /// sent while NVDA was still at the one before was lost now and then (the second of two Ctrl+Right presses, a
    /// Ctrl+Down after Ctrl+Up; 2026-11-01).
    /// </summary>
    public static void SettleAfterKey(long lengthBefore, int maxMs)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < maxMs)
        {
            if (LogLength() != lengthBefore)
            {
                // A moment more once it spoke, for the key itself to land in the window.
                Thread.Sleep(80);
                return;
            }

            Thread.Sleep(30);
        }
    }

    public List<string> Pending()
    {
        if (File.Exists(LogPath))
        {
            using var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            stream.Seek(_read, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, new UTF8Encoding(false));
            var text = reader.ReadToEnd();
            var complete = text.LastIndexOf('\n') + 1;
            _read += Encoding.UTF8.GetByteCount(text[..complete]);
            foreach (var line in text[..complete].Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.TrimEnd('\r').Split('\t');
                _all.Add(parts.Length >= 3 && parts[1] == "cancel" ? "[cancel]" : parts.Length >= 3 ? parts[2] : line);
            }
        }

        return _all.GetRange(_taken, _all.Count - _taken);
    }

    public List<string>? Take(string text)
    {
        var pending = Pending();
        var at = pending.FindIndex(said => said.Contains(text, StringComparison.OrdinalIgnoreCase));
        if (at < 0)
        {
            return null;
        }

        _taken += at + 1;
        return pending.GetRange(0, at + 1);
    }

    public List<string> TakeAll()
    {
        var pending = Pending();
        _taken = _all.Count;
        return pending;
    }
}
