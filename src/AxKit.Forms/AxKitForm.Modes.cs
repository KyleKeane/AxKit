using AxKit.Core;

namespace AxKit.Forms;

public abstract partial class AxKitForm
{
    // ---- edit mode and navigation mode (docs/axKit/SPEC.md AX-8.13, the user's decision of 2026-11-15) ----
    //
    // The mode key (BundleKeys.ToggleMode, Ctrl+Space) means one thing in every window, so the window base handles it,
    // for the control the key message is addressed to: an editable text of several lines (AxKitTextBox.HasModes)
    // toggles between edit mode and navigation mode and says which; a text that cannot be edited says TextModes.ReadOnly;
    // anything else plays the edge sound. In navigation mode the text hands its keys here before anything else sees
    // them (TakeNavigationKey): the jump letters move, the paragraph keys move (ahead of axTerminal's history on the same
    // keys), and a key that would change the text is refused with the edge sound (TextModes.InNavigation).

    /// <summary>The mode key, from <see cref="ProcessCmdKey"/>; true when the key was taken.</summary>
    private bool TextModeKey(Control? target, Keys keyData)
    {
        if (target is ToolStrip)
        {
            // Menu mode: the keys are the menu's (AX-6.6).
            return false;
        }

        if (keyData == BundleKeys.ToggleMode)
        {
            switch (target)
            {
                case AxKitTextBox { HasModes: true } box:
                    Announce(box.ToggleMode(), true);
                    break;
                case TextBoxBase { ReadOnly: true }:
                    Announce(TextModes.ReadOnly, true);
                    break;
                default:
                    Sounds.Edge();
                    break;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// A key down in <paramref name="text"/> in navigation mode, from the text's own <c>ProcessCmdKey</c>, the first to
    /// see the key, ahead of the text box's own shortcuts (Ctrl+Backspace) and the window's commands; true when taken.
    /// </summary>
    internal bool TakeNavigationKey(AxKitTextBox text, Keys keyData)
    {
        if (keyData == BundleKeys.ToggleMode)
        {
            return false;
        }

        var modifiers = keyData & Keys.Modifiers;
        // One sound for each plain key (AX-8.13, 2026-11-16), before anything moves or is said: the tick when the
        // letter's command has somewhere to go, the buzz alone when there is nothing of its kind (the words
        // "No next heading" follow as ever), and further down the bonk for a key with no command at all.
        if (modifiers is Keys.None or Keys.Shift && TextJump.Handle(text, text.NavigationJumps, keyData, Announce, NavigationSound))
        {
            return true;
        }

        if (keyData is BundleKeys.ParagraphUp or BundleKeys.ParagraphDown or BundleKeys.SelectParagraphUp or BundleKeys.SelectParagraphDown)
        {
            EditKeys.Paragraph(text, (keyData & Keys.KeyCode) == Keys.Down ? 1 : -1, keyData.HasFlag(Keys.Shift));
            return true;
        }

        if (TextModes.InNavigation((int)(keyData & Keys.KeyCode), keyData.HasFlag(Keys.Control), keyData.HasFlag(Keys.Shift), keyData.HasFlag(Keys.Alt)) == NavigationKey.Refused)
        {
            // A plain key that would type has no command here: the blips. An edit refused keeps the edge sound.
            if (TextModes.Unassigned((int)(keyData & Keys.KeyCode), keyData.HasFlag(Keys.Control), keyData.HasFlag(Keys.Alt)))
            {
                Sounds.Unassigned();
            }
            else
            {
                Sounds.Edge();
            }

            return true;
        }

        return false;
    }

    /// <summary>The sound of a navigation letter's command: the tick when it has somewhere to go, the buzz when it has not.</summary>
    private static void NavigationSound(bool found)
    {
        if (found)
        {
            Sounds.NavKey();
        }
        else
        {
            Sounds.Nothing();
        }
    }

    /// <summary>
    /// A menu command that would change <paramref name="text"/> (Undo, Redo, Cut, Paste, an editor's command on the
    /// whole text): true, with the edge sound and "Navigation mode: press Ctrl+Space to edit" said, while the text is
    /// in navigation mode. The sound is the one the command's key gives there (<see cref="TakeNavigationKey"/>), so a
    /// refusal sounds the same from the menu, from the palette and for an AI tool (2026-11-16; words alone until then).
    /// </summary>
    protected bool RefusedInNavigation(AxKitTextBox text)
    {
        if (!text.NavigationMode)
        {
            return false;
        }

        Sounds.Edge();
        Announce(TextModes.Refused(BundleKeys.Text(BundleKeys.ToggleMode)), true);
        return true;
    }

    /// <summary>The key test's words for a key of the focused text in navigation mode (AX-8.12): a jump letter, a paragraph key, a refused key; null otherwise.</summary>
    private static string? KeyTestInMode(Keys key)
    {
        var focus = Control.FromChildHandle(User32.GetFocus());
        if (key == BundleKeys.ToggleMode)
        {
            return focus switch
            {
                AxKitTextBox { HasModes: true, NavigationMode: true } => "Edit mode or navigation mode: to edit mode",
                AxKitTextBox { HasModes: true } => "Edit mode or navigation mode: to navigation mode",
                TextBoxBase { ReadOnly: true } => "Edit mode or navigation mode: says this text is read only",
                _ => "Edit mode or navigation mode: there is no text with modes where you are",
            };
        }

        if (focus is not AxKitTextBox { NavigationMode: true } text)
        {
            return null;
        }

        foreach (var jump in text.NavigationJumps)
        {
            if (key == jump.Next || key == jump.Previous)
            {
                return TextModes.KeyTestJump(jump.Name, key == jump.Previous);
            }
        }

        if (key is BundleKeys.ParagraphUp or BundleKeys.ParagraphDown or BundleKeys.SelectParagraphUp or BundleKeys.SelectParagraphDown)
        {
            return BundleKeys.All.First(named => named.Key == key).Name;
        }

        return TextModes.InNavigation((int)(key & Keys.KeyCode), key.HasFlag(Keys.Control), key.HasFlag(Keys.Shift), key.HasFlag(Keys.Alt)) == NavigationKey.Refused
            ? TextModes.KeyTestRefused
            : null;
    }
}
