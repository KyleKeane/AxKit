using System.Runtime.InteropServices;
using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// Keys sent to the program in front, as a keyboard types them (docs/axClip/SPEC.md CP-D3; from axClip's
/// <c>ClipForm</c> on its second use, axTemplate's launcher, 2026-11-23, moved unchanged): the paste key, sent
/// after a window of axKit's has hidden and given the front back. The caller checks first that the window it
/// means is the one in front, so that nothing is typed into a window the user did not come from.
/// </summary>
public static class KeySender
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventUp = 0x0002;
    private const ushort VkControl = 0x11;
    private const ushort VkV = 0x56;

    /// <summary>Ctrl+V to the program in front: Ctrl down, V down, V up, Ctrl up, in one <c>SendInput</c>.</summary>
    public static void SendCtrlV()
    {
        Input[] inputs =
        [
            Key(VkControl, 0),
            Key(VkV, 0),
            Key(VkV, KeyEventUp),
            Key(VkControl, KeyEventUp),
        ];
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
        {
            Log.Info("The paste's keys were not all sent");
        }
    }

    private static Input Key(ushort key, uint flags) => new() { Type = InputKeyboard, Keyboard = new KeyboardInput { Key = key, Flags = flags } };

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort Key;
        public ushort Scan;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    /// <summary>INPUT with its keyboard member; the padding makes it as large as the union's largest member, the mouse's.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public KeyboardInput Keyboard;
        public long Padding;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);
}
