using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AxKit.Core;

namespace AxProcess.Core;

/// <summary>The whole computer's busy and idle times at one moment, for the processor use between two (SPEC.md PR-2.5).</summary>
public readonly record struct SystemTimes(long Idle, long Kernel, long User)
{
    /// <summary>The share of the time between the two readings that the processors were busy, 0 to 100; null when no time passed. The kernel time includes the idle time.</summary>
    public static double? Busy(SystemTimes before, SystemTimes after)
    {
        var total = after.Kernel - before.Kernel + (after.User - before.User);
        if (total <= 0)
        {
            return null;
        }

        var idle = after.Idle - before.Idle;
        return Math.Clamp((total - idle) * 100d / total, 0, 100);
    }
}

/// <summary>
/// What Windows says about the computer and one process beyond what .NET reads (SPEC.md PR-2.5, PR-3.4): the memory
/// in use, the processors' times, a process's program file by its ID. P/Invoke on kernel32, which every user may call.
/// </summary>
public static class SystemInfo
{

    /// <summary>The physical memory, total and available, in bytes; zeros when Windows does not answer.</summary>
    public static (long Total, long Available) Memory()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        return GlobalMemoryStatusEx(ref status) ? ((long)status.TotalPhys, (long)status.AvailPhys) : (0, 0);
    }

    public static SystemTimes Times() =>
        GetSystemTimes(out var idle, out var kernel, out var user) ? new SystemTimes(idle, kernel, user) : default;

    /// <summary>The program file of a running process, which works for most processes a user cannot open fully; null when Windows refuses or it has ended.</summary>
    public static string? ProgramPath(int processId) => processId < 0 ? null : ThisProgram.ImageOf((uint)processId);

    /// <summary>The lines of a process's details notice (PR-3.4): what Windows lets the user read, each on its own line.</summary>
    public static IReadOnlyList<string> Details(ProcessRow row)
    {
        var lines = new List<string>
        {
            $"Process ID {row.Id}",
            $"Memory {FileSize.Text(row.Memory)}",
        };
        if (row.Processor is { } cpu)
        {
            lines.Add($"Processor {Math.Round(cpu):0}%");
        }

        if (ProgramPath(row.Id) is { } path)
        {
            lines.Add($"Program {path}");
        }

        try
        {
            using var process = Process.GetProcessById(row.Id);
            try
            {
                lines.Add($"Started {process.StartTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex) when (ex is Win32Exception or NotSupportedException or InvalidOperationException)
            {
                // A protected process gives neither; the line is left out.
            }

            try
            {
                lines.Add($"Threads {process.Threads.Count}");
            }
            catch (Exception ex) when (ex is Win32Exception or NotSupportedException or InvalidOperationException)
            {
                // A protected process gives neither; the line is left out.
            }
        }
        catch (ArgumentException)
        {
            lines.Add("It has ended.");
        }

        return lines;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

}
