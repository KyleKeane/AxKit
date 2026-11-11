using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace AxAutomate.Core;

internal static class Native
{
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    public static extern bool PlaySoundW(string? sound, nint module, uint flags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public nint lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public nint hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    /// <summary>This process's environment block as Windows keeps it, the hidden per-drive folders (=C:) included.</summary>
    [DllImport("kernel32.dll")]
    public static extern nint GetEnvironmentStringsW();

    [DllImport("kernel32.dll")]
    public static extern bool FreeEnvironmentStringsW(nint block);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateDesktopW(string name, nint device, nint devmode, int flags, uint access, nint attributes);

    [DllImport("user32.dll")]
    public static extern bool CloseDesktop(nint desktop);

    private delegate bool EnumWindowsProc(nint window, nint parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumDesktopWindows(nint desktop, EnumWindowsProc callback, nint parameter);

    /// <summary>The processes with a top-level window on the desktop.</summary>
    public static HashSet<int> DesktopProcesses(nint desktop)
    {
        var ids = new HashSet<int>();
        EnumDesktopWindows(desktop, (window, _) =>
        {
            GetWindowThreadProcessId(window, out var id);
            ids.Add(id);
            return true;
        }, 0);
        return ids;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PROCESSENTRY32W
    {
        public int dwSize;
        public int cntUsage;
        public int th32ProcessID;
        public nint th32DefaultHeapID;
        public int th32ModuleID;
        public int cntThreads;
        public int th32ParentProcessID;
        public int pcPriClassBase;
        public int dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint CreateToolhelp32Snapshot(uint flags, int processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Process32FirstW(nint snapshot, ref PROCESSENTRY32W entry);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Process32NextW(nint snapshot, ref PROCESSENTRY32W entry);

    /// <summary>
    /// Every living descendant of the process, from one snapshot of the processes and their parents. A child must have
    /// started after its parent, so that a parent's number reused by a newer process never adopts an older one.
    /// </summary>
    public static HashSet<int> Descendants(int root)
    {
        var parents = new Dictionary<int, int>();
        var snapshot = CreateToolhelp32Snapshot(0x2, 0);
        if (snapshot == -1)
        {
            return [];
        }

        try
        {
            var entry = new PROCESSENTRY32W { dwSize = Marshal.SizeOf<PROCESSENTRY32W>() };
            for (var more = Process32FirstW(snapshot, ref entry); more; more = Process32NextW(snapshot, ref entry))
            {
                parents[entry.th32ProcessID] = entry.th32ParentProcessID;
            }
        }
        finally
        {
            CloseHandle(snapshot);
        }

        var family = new HashSet<int> { root };
        for (var grew = true; grew;)
        {
            grew = false;
            foreach (var (child, parent) in parents)
            {
                if (!family.Contains(child) && family.Contains(parent) && StartedAfter(child, parent))
                {
                    family.Add(child);
                    grew = true;
                }
            }
        }

        family.Remove(root);
        return family;
    }

    private static bool StartedAfter(int child, int parent)
    {
        try
        {
            using var first = Process.GetProcessById(parent);
            using var second = Process.GetProcessById(child);
            return second.StartTime >= first.StartTime;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }


    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CreateProcessW(string? application, StringBuilder commandLine, nint processAttributes, nint threadAttributes,
        bool inheritHandles, int flags, nint environment, string? directory, ref STARTUPINFO startup, out PROCESS_INFORMATION info);

    [DllImport("kernel32.dll")]
    public static extern bool CloseHandle(nint handle);

    [DllImport("user32.dll")]
    public static extern bool AllowSetForegroundWindow(int processId);

    // ---- the NVDA tier's keyboard: SendInput, as a keyboard types ----

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct INPUT
    {
        [FieldOffset(0)]
        public int type;

        [FieldOffset(8)]
        public KEYBDINPUT ki;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, INPUT[] inputs, int size);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(nint window, out int processId);

    /// <summary>The process of the window in front.</summary>
    public static int ForegroundProcess()
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out var id);
        return id;
    }

    /// <summary>
    /// Key presses as the channel's keys step spells them, "ctrl+shift+p escape down", each one pressed and released with
    /// its modifiers. <paramref name="inFront"/> is asked before each press: a key goes only to a window of the run, never
    /// to the user's own (keys reached the user's axClaude window between scripts on 2026-10-31).
    /// </summary>
    public static void Keys(string presses, Func<bool> inFront)
    {
        foreach (var press in presses.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!inFront())
            {
                throw new InvalidOperationException("no window of the run is in front; the key was not sent");
            }

            var parts = press.Split('+');
            var modifiers = parts[..^1].Select(part => part.ToLowerInvariant() switch
            {
                "ctrl" => (ushort)0x11,
                "shift" => (ushort)0x10,
                "alt" => (ushort)0x12,
                // NVDA's own modifier, Insert in its desktop layout: "nvda+1" turns input help on and off, which names
                // what NVDA does with the next key (a probe, 2026-11-01).
                "nvda" or "insert" => (ushort)0x2D,
                _ => throw new ArgumentException($"unknown modifier {part}"),
            }).ToList();
            var key = VirtualKey(parts[^1]);
            var before = Speech.LogLength();
            // As fingers do it: the modifiers held a moment before the key, the key held a moment, the modifiers let go
            // after; NVDA takes the keys it has scripts for (Ctrl with an arrow moves by word or paragraph) off the
            // hook and sends them on itself, and one of two in the same instant was lost now and then (2026-11-01).
            Send(modifiers.Select(vk => Key(vk, up: false)));
            Send([Key(key, up: false)]);
            Send([Key(key, up: true)]);
            Send(Enumerable.Reverse(modifiers).Select(vk => Key(vk, up: true)));
            Speech.SettleAfterKey(before, 400);
        }
    }

    private static void Send(IEnumerable<INPUT> inputs)
    {
        var batch = inputs.ToArray();
        if (batch.Length > 0)
        {
            SendInput((uint)batch.Length, batch, Marshal.SizeOf<INPUT>());
            Thread.Sleep(30);
        }
    }

    /// <summary>Characters as typed, each a Unicode key press, each only while a window of the run is in front.</summary>
    public static void Type(string text, Func<bool> inFront)
    {
        var before = Speech.LogLength();
        foreach (var c in text)
        {
            if (!inFront())
            {
                throw new InvalidOperationException("no window of the run is in front; the character was not sent");
            }

            INPUT[] inputs =
            [
                new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE } },
                new INPUT { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wScan = c, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } },
            ];
            SendInput(2, inputs, Marshal.SizeOf<INPUT>());
            Thread.Sleep(20);
        }

        Speech.SettleAfterKey(before, 400);
    }

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKeyW(uint code, uint mapType);

    /// <summary>
    /// One key as a keyboard sends it: its scan code, and the extended flag for Page Up to Delete and the arrows. Without
    /// the flag those are the numeric keypad's keys, which NVDA's desktop layout takes for its review cursor and never
    /// passes on (the first full NVDA pass, 2026-10-31: no caret moved).
    /// </summary>
    private static INPUT Key(ushort vk, bool up)
    {
        var extended = vk is >= 0x21 and <= 0x2E;
        var flags = (up ? KEYEVENTF_KEYUP : 0) | (extended ? KEYEVENTF_EXTENDEDKEY : 0);
        return new() { type = INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = vk, wScan = (ushort)MapVirtualKeyW(vk, 0), dwFlags = flags } };
    }

    private static ushort VirtualKey(string name)
    {
        if (name.Length == 1 && char.IsLetterOrDigit(name[0]))
        {
            return char.ToUpperInvariant(name[0]);
        }

        return name.ToLowerInvariant() switch
        {
            "enter" => 0x0D,
            "escape" or "esc" => 0x1B,
            "tab" => 0x09,
            "space" => 0x20,
            "backspace" => 0x08,
            "delete" => 0x2E,
            "insert" => 0x2D,
            "up" => 0x26,
            "down" => 0x28,
            "left" => 0x25,
            "right" => 0x27,
            "home" => 0x24,
            "end" => 0x23,
            "pageup" => 0x21,
            "pagedown" => 0x22,
            _ when name.StartsWith('f') && int.TryParse(name[1..], out var f) && f is >= 1 and <= 12 => (ushort)(0x70 + f - 1),
            _ => throw new ArgumentException($"unknown key {name}"),
        };
    }
}
