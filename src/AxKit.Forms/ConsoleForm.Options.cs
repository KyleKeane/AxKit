using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The Options submenus every console window has (docs/axKit/framework.md step 11, second part, 2026-11-22; from
/// axClaude's and axTerminal's menus, which each built the same items over the same settings under their own
/// words): Sounds, and what happens when the program is done. The settings are the console's
/// (<see cref="ConsoleSettings"/>); the words that name the program are the app's.
/// </summary>
public abstract partial class ConsoleForm
{
    /// <summary>
    /// Options → Sounds: the chimes, the ticks for new lines, and the music while the program works with its key.
    /// The app gives the three labels, which say what its chimes and ticks are for ("&amp;Chimes: ready, sent,
    /// replying, done, question"), and the music's key from its table.
    /// </summary>
    protected ToolStripMenuItem CreateSoundsMenu(string chimes, string ticks, string music, Keys musicKey)
    {
        var sounds = new ToolStripMenuItem("&Sounds");
        sounds.DropDownItems.Add(Toggle(chimes, ConsoleSettings.SoundOnBell, v => ConsoleSettings.SoundOnBell = v));
        sounds.DropDownItems.Add(Toggle(ticks, ConsoleSettings.ClickOnNewLine, v => ConsoleSettings.ClickOnNewLine = v));
        // It starts or stops at once when toggled while the program works; the key anywhere flips the same item.
        sounds.DropDownItems.Add(CreateMusicItem(music, musicKey));
        return sounds;
    }

    /// <summary>
    /// Options → When … is done: the word "Done", the taskbar button's flash, and the louder alert while the window
    /// is in the background. The app gives the menu's name ("When Claude is &amp;done") and its own switch for the
    /// word, which each app keeps in its own settings under its own name.
    /// </summary>
    protected ToolStripMenuItem CreateDoneMenu(string name, bool announceDone, Action<bool> setAnnounceDone)
    {
        var done = new ToolStripMenuItem(name);
        done.DropDownItems.Add(Toggle("&Announce \"Done\"", announceDone, setAnnounceDone));
        done.DropDownItems.Add(Toggle("&Flash the taskbar button", ConsoleSettings.FlashTaskbar, v => ConsoleSettings.FlashTaskbar = v));
        done.DropDownItems.Add(Toggle("&Louder alert when the window is in the background", ConsoleSettings.BackgroundAlert, v => ConsoleSettings.BackgroundAlert = v));
        return done;
    }
}
