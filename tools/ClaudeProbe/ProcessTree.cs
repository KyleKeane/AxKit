using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace Probe;

/// <summary>
/// The processes under the Claude Code the probe started: sampled every 100 ms from the system's process list by
/// parent id (a child must be younger than its parent, so a reused id is not taken for one), each new one logged with
/// its name, and at the end those still alive are ended, by the ids recorded here and by nothing else.
/// </summary>
internal sealed class ProcessTree
{
    private readonly object _gate = new();
    private readonly Dictionary<int, (DateTime Start, string Name)> _known = new();
    private readonly Jsonl _log;
    private readonly Thread _thread;
    private volatile bool _stop;

    public ProcessTree(int rootPid, Jsonl log)
    {
        _log = log;
        DateTime start;
        try
        {
            using var process = Process.GetProcessById(rootPid);
            start = process.StartTime;
        }
        catch (Exception)
        {
            start = DateTime.Now;
        }

        _known[rootPid] = (start, "(root)");
        _log.Write(new JsonObject { ["t"] = Clock.Ms, ["k"] = "root", ["pid"] = rootPid });
        _thread = new Thread(Loop) { IsBackground = true, Name = "process-tree" };
        _thread.Start();
    }

    public int Seen
    {
        get
        {
            lock (_gate)
            {
                return _known.Count - 1;
            }
        }
    }

    public void Stop()
    {
        _stop = true;
        _thread.Join(1000);
    }

    /// <summary>Ends what is left of the tree; returns how many processes that was.</summary>
    public int EndLeftovers()
    {
        Sample();
        var ended = 0;
        List<KeyValuePair<int, (DateTime Start, string Name)>> known;
        lock (_gate)
        {
            known = _known.ToList();
        }

        foreach (var pair in known)
        {
            if (pair.Key == Environment.ProcessId)
            {
                continue;
            }

            try
            {
                using var process = Process.GetProcessById(pair.Key);
                if (!process.HasExited && process.StartTime == pair.Value.Start)
                {
                    process.Kill();
                    ended++;
                    _log.Write(new JsonObject { ["t"] = Clock.Ms, ["k"] = "ended", ["pid"] = pair.Key, ["name"] = pair.Value.Name });
                }
            }
            catch (Exception)
            {
                // Gone already, or not ours to end.
            }
        }

        return ended;
    }

    private void Loop()
    {
        while (!_stop)
        {
            Sample();
            Thread.Sleep(100);
        }
    }

    private void Sample()
    {
        var snapshot = CreateToolhelp32Snapshot(SnapProcess, 0);
        if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
        {
            return;
        }

        var entries = new List<(int Pid, int Parent, string Name)>();
        try
        {
            var entry = new ProcessEntry { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
            if (Process32FirstW(snapshot, ref entry))
            {
                do
                {
                    entries.Add(((int)entry.ProcessId, (int)entry.ParentProcessId, entry.ExeFile));
                }
                while (Process32NextW(snapshot, ref entry));
            }
        }
        finally
        {
            CloseHandle(snapshot);
        }

        lock (_gate)
        {
            bool added;
            do
            {
                added = false;
                foreach (var entry in entries)
                {
                    if (_known.ContainsKey(entry.Pid) || !_known.TryGetValue(entry.Parent, out var parent))
                    {
                        continue;
                    }

                    DateTime start;
                    try
                    {
                        using var process = Process.GetProcessById(entry.Pid);
                        start = process.StartTime;
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (start < parent.Start.AddSeconds(-1))
                    {
                        continue;
                    }

                    _known[entry.Pid] = (start, entry.Name);
                    added = true;
                    _log.Write(new JsonObject
                    {
                        ["t"] = Clock.Ms, ["k"] = "process", ["pid"] = entry.Pid, ["parent"] = entry.Parent, ["name"] = entry.Name,
                        ["tStarted"] = Clock.FromTicks(start.ToUniversalTime().Ticks),
                    });
                }
            }
            while (added);
        }
    }

    private const uint SnapProcess = 0x00000002;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public IntPtr DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int PriorityClassBase;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool Process32FirstW(IntPtr snapshot, ref ProcessEntry entry);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool Process32NextW(IntPtr snapshot, ref ProcessEntry entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
