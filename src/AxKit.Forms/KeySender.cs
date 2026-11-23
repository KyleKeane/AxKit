using System.Runtime.InteropServices;
using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// Keys sent to the program in front, as a keyboard types them (docs/axClip/SPEC.md CP-D3; from axClip's
/// <c>ClipForm</c> on its second use, axTemplate's launcher, 2026-11-23): the paste key, sent after a window of
/// axKit's has hidden and given the front back; and for axTemplate (docs/axTemplate/SPEC.md TP-4.2, TP-2.3) the
/// copy key, which takes that program's selection, and Left keys, which set the caret back after a paste. The
/// caller checks first that the window it means is the one in front, so that nothing is typed into a window the
/// user did not come from. Windows refuses such keys into a program that runs as administrator, without an error.
/// </summary>
public static class KeySender
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventUp = 0x0002;
    private const uint KeyEventExtended = 0x0001;
    private const ushort VkControl = 0x11;
    private const ushort VkC = 0x43;
    private const ushort VkV = 0x56;
    private const ushort VkLeft = 0x25;
    private const ushort VkDelete = 0x2E;

    /// <summary>Ctrl+V to the program in front: Ctrl down, V down, V up, Ctrl up, in one <c>SendInput</c>.</summary>
    public static void SendCtrlV() => Send("The paste's keys",
    [
        Key(VkControl, 0),
        Key(VkV, 0),
        Key(VkV, KeyEventUp),
        Key(VkControl, KeyEventUp),
    ]);

    /// <summary>Ctrl+C to the program in front, as <see cref="SendCtrlV"/>: its selection goes to the clipboard. In a terminal this ends the running command, so it is sent only at the user's own word (TP-D4).</summary>
    public static void SendCtrlC() => Send("The copy's keys",
    [
        Key(VkControl, 0),
        Key(VkC, 0),
        Key(VkC, KeyEventUp),
        Key(VkControl, KeyEventUp),
    ]);

    /// <summary>The Left key <paramref name="count"/> times to the program in front, in one <c>SendInput</c>: the caret back over what follows its place in a text just pasted (TP-2.3, TP-D5).</summary>
    public static void SendLeft(int count)
    {
        if (count <= 0)
        {
            return;
        }

        var inputs = new Input[count * 2];
        for (var i = 0; i < count; i++)
        {
            // An arrow key is an extended key: without the flag it is the number pad's 4.
            inputs[i * 2] = Key(VkLeft, KeyEventExtended);
            inputs[i * 2 + 1] = Key(VkLeft, KeyEventExtended | KeyEventUp);
        }

        Send("The caret's keys", inputs);
    }

    /// <summary>The Delete key to the program in front: what is selected there goes (TP-2.3, the caret's mark).</summary>
    public static void SendDelete() => Send("The delete key", [Key(VkDelete, KeyEventExtended), Key(VkDelete, KeyEventExtended | KeyEventUp)]);

    /// <summary>
    /// Waits until Ctrl, Shift, Alt and the Windows keys are all up, <paramref name="maxMs"/> at most: a key the user
    /// still holds from the hotkey or from Ctrl+Enter joins every key sent, a Left becoming Ctrl+Left, a word, or
    /// Shift+Left, a selection (the maintainer, 2026-11-23: the caret "unreliable"). False when one was still down.
    /// </summary>
    public static async Task<bool> WaitForModifiersUpAsync(int maxMs)
    {
        for (var waited = 0; waited <= maxMs; waited += 25)
        {
            if (!ModifierDown())
            {
                return true;
            }

            await Task.Delay(25);
        }

        return false;
    }

    private static bool ModifierDown() =>
        ModifierKeys.Any(key => (GetAsyncKeyState(key) & 0x8000) != 0);

    private static readonly int[] ModifierKeys = [0x10, 0x11, 0x12, 0x5B, 0x5C];

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    private static void Send(string what, Input[] inputs)
    {
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
        {
            Log.Info(what + " were not all sent");
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
