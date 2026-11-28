namespace AxKit.Forms;

/// <summary>
/// Which key of the user's a window is handling at this moment, so that a notice it opens knows whether the user's own
/// key opened it (<see cref="Notice.AsShown"/>; the fifth review of 2026-11-27, E3: a fact of the moment, not of the
/// source). The window base's key dispatch runs every key through <see cref="Handle"/>, and every modal loop of a
/// window (the Windows pickers, the shell's dialogs and menu, any ShowDialog) through <see cref="ModalLoop{T}"/>: a
/// key handled inside the loop is the loop's own, and once the loop is over no notice is the first result of the key
/// that opened it (the sixth review of 2026-11-27, F3: the Enter that closed Save as, pressed twice, answered "Save as
/// UTF-8?", which the key Ctrl+Shift+S had "opened" seconds before).
/// </summary>
public sealed class UserKeyMoment
{
    /// <summary>The key whose handling runs now; null at every other time, and for the rest of a key's handling once a modal loop ran inside it.</summary>
    public Keys? Current { get; private set; }

    /// <summary>Runs the handling of <paramref name="key"/> with it as <see cref="Current"/>, and gives back what was before it when the handling ends.</summary>
    public bool Handle(Keys key, Func<bool> handling)
    {
        var outer = Current;
        Current = key;
        try
        {
            return handling();
        }
        finally
        {
            Current = outer;
        }
    }

    /// <summary>
    /// A modal loop run inside whatever is handled now: no key of the user's is being handled from its start, and none
    /// is for the rest of the key's handling after it, whatever the keys inside it were (each restores what it found,
    /// which is none).
    /// </summary>
    public T ModalLoop<T>(Func<T> loop)
    {
        Current = null;
        try
        {
            return loop();
        }
        finally
        {
            Current = null;
        }
    }
}
