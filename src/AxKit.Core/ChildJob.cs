using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AxKit.Core;

/// <summary>
/// A Windows job that ties what a program started to this process's life (the 2026-11-21 audit of the stream source):
/// every process put into it, and every process those start, is ended when the job is ended and when this process
/// ends, however it ends, since Windows closes the job's handle with the process and the job is made to end its
/// processes then. A pseudo console does this for a console program by itself; a program run over pipes has nothing
/// of the kind, and went on without its window when axKit crashed or was ended.
///
/// Plain: one handle under one lock, every member safe to call from any thread and after <see cref="Dispose"/>. Where
/// Windows refuses a job, <see cref="TryCreate"/> gives null and the caller runs the program without one, as before.
/// </summary>
public sealed class ChildJob : IDisposable
{
    private readonly object _gate = new();
    private nint _handle;

    private ChildJob(nint handle) => _handle = handle;

    /// <summary>A new job that ends its processes when its handle closes; null when Windows gives none.</summary>
    public static ChildJob? TryCreate()
    {
        var handle = CreateJobObjectW(0, null);
        if (handle == 0)
        {
            return null;
        }

        var limits = new ExtendedLimits { Basic = new BasicLimits { LimitFlags = KillOnJobClose } };
        if (!SetInformationJobObject(handle, ExtendedLimitInformation, ref limits, Marshal.SizeOf<ExtendedLimits>()))
        {
            CloseHandle(handle);
            return null;
        }

        return new ChildJob(handle);
    }

    /// <summary>Puts a process into the job; what it starts from now on is in it too. False when Windows refuses or the process is gone.</summary>
    public bool Add(Process process)
    {
        lock (_gate)
        {
            try
            {
                return _handle != 0 && AssignProcessToJobObject(_handle, process.Handle);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
            {
                return false;
            }
        }
    }

    /// <summary>Ends every process of the job, at once. Nothing when the job is closed.</summary>
    public void Terminate()
    {
        lock (_gate)
        {
            if (_handle != 0)
            {
                TerminateJobObject(_handle, 1);
            }
        }
    }

    /// <summary>Closes the job, which ends what is still in it.</summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_handle != 0)
            {
                CloseHandle(_handle);
                _handle = 0;
            }
        }
    }

    private const int ExtendedLimitInformation = 9;
    private const uint KillOnJobClose = 0x2000;

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        public BasicLimits Basic;
        public IoCounters Io;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateJobObjectW(nint attributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(nint job, int informationClass, ref ExtendedLimits information, int length);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(nint job, nint process);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateJobObject(nint job, uint exitCode);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint handle);
}
