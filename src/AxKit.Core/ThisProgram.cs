using System.Runtime.InteropServices;
using System.Text;

namespace AxKit.Core;

/// <summary>
/// Whether another process runs this very program file (security audit S-13): asked of Windows by the process's image
/// path, which a look-alike program cannot copy the way it can copy a window title or a pipe name. From
/// AppMessage on its second use, the MCP pipes (docs/axKit/mcp.md MC-6.3), 2026-10-24; in AxKit.Core since 2026-11-21,
/// when the relay of Claude Code's questions, which loads nothing of the windows, came to ask it too.
/// </summary>
public static class ThisProgram
{
    /// <summary>Whether the process runs the same AxKit.exe as this one.</summary>
    public static bool Runs(uint processId) =>
        ImageOf(processId) is { } path && string.Equals(path, Environment.ProcessPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the pipe this process connected to is served by a process of this same program file: asked before a
    /// secret is written to a pipe known by its name alone, since another program of the same account can make a pipe
    /// of any name that is free (the questions' relay, docs/axClaude/stream-mode.md SM-7.3; the 2026-11-21 audit).
    /// False when Windows cannot say.
    /// </summary>
    public static bool Serves(System.IO.Pipes.NamedPipeClientStream pipe) =>
        GetNamedPipeServerProcessId(pipe.SafePipeHandle.DangerousGetHandle(), out var server) && Runs(server);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetNamedPipeServerProcessId(nint pipe, out uint processId);

    /// <summary>The program file a process runs, or null when it is gone or may not be asked.</summary>
    public static string? ImageOf(uint processId)
    {
        var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process == 0)
        {
            return null;
        }

        try
        {
            var size = 1024;
            var path = new StringBuilder(size);
            return QueryFullProcessImageNameW(process, 0, path, ref size) ? path.ToString() : null;
        }
        finally
        {
            CloseHandle(process);
        }
    }

    /// <summary>The process that started this one, by the id Windows kept for it; null when it cannot be read. The id may since name another process: the caller checks what it runs.</summary>
    public static uint? ParentId()
    {
        var info = new ProcessBasicInformation();
        return NtQueryInformationProcess(GetCurrentProcess(), 0, ref info, Marshal.SizeOf<ProcessBasicInformation>(), out _) == 0
            ? (uint)info.InheritedFromUniqueProcessId
            : null;
    }

    /// <summary>The process that started another one, as <see cref="ParentId"/>; null when it is gone or may not be asked.</summary>
    public static uint? ParentOf(uint processId)
    {
        var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process == 0)
        {
            return null;
        }

        try
        {
            var info = new ProcessBasicInformation();
            return NtQueryInformationProcess(process, 0, ref info, Marshal.SizeOf<ProcessBasicInformation>(), out _) == 0
                ? (uint)info.InheritedFromUniqueProcessId
                : null;
        }
        finally
        {
            CloseHandle(process);
        }
    }

    /// <summary>
    /// The command line a process was started with, as Windows keeps it (docs/axKit/SPEC.md AX-1.9; repairs.md R-65);
    /// null when the process is gone or may not be asked. For telling which app of the bundle a process of this
    /// program file is, by its verb (<see cref="AxKit.Core.BackgroundStart.IsCopyOf"/>).
    /// </summary>
    public static string? CommandLineOf(uint processId)
    {
        var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process == 0)
        {
            return null;
        }

        try
        {
            // The first call says how many bytes the answer takes: a UNICODE_STRING, then the text it points into.
            NtQueryInformationProcess(process, ProcessCommandLineInformation, 0, 0, out var needed);
            if (needed <= 0)
            {
                return null;
            }

            var buffer = Marshal.AllocHGlobal(needed);
            try
            {
                if (NtQueryInformationProcess(process, ProcessCommandLineInformation, buffer, needed, out _) != 0)
                {
                    return null;
                }

                // UNICODE_STRING: the length in bytes (two bytes), the capacity (two bytes), then, at the pointer's own alignment, the text's address.
                var bytes = (ushort)Marshal.ReadInt16(buffer);
                var text = Marshal.ReadIntPtr(buffer, IntPtr.Size);
                return text == 0 ? null : Marshal.PtrToStringUni(text, bytes / 2);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            CloseHandle(process);
        }
    }

    /// <summary>NtQueryInformationProcess's class for the command line (Windows 8.1 and later).</summary>
    private const int ProcessCommandLineInformation = 60;

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(nint process, int informationClass, nint information, int length, out int returned);

    private const uint ProcessQueryLimitedInformation = 0x1000;

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation
    {
        public nint ExitStatus;
        public nint PebBaseAddress;
        public nint AffinityMask;
        public nint BasePriority;
        public nint UniqueProcessId;
        public nint InheritedFromUniqueProcessId;
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(nint process, int informationClass, ref ProcessBasicInformation information, int length, out int returned);

    [DllImport("kernel32.dll")]
    private static extern nint GetCurrentProcess();

    [DllImport("kernel32.dll")]
    private static extern nint OpenProcess(uint access, bool inherit, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageNameW(nint process, uint flags, StringBuilder path, ref int size);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint handle);
}
