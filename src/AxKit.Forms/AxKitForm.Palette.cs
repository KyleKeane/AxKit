using AxKit.Core;
using AxKit.Core.Events;

namespace AxKit.Forms;

public abstract partial class AxKitForm
{
    // ---- the command palette (AX-10): the window's menu commands by name, in a notice ----

    private IReadOnlyList<PaletteEntry> _paletteAll = [];
    private IReadOnlyList<PaletteEntry> _paletteShown = [];

    /// <summary>The list with nothing typed: the F1 text's lines, then every command, then the keys with no menu item (AX-10.2).</summary>
    private IReadOnlyList<PaletteEntry> _paletteHelp = [];

    /// <summary>The pause after typing in the palette before its first match is said (AX-10.4, axStart's 300 ms).</summary>
    private readonly System.Windows.Forms.Timer _paletteSpeak = new() { Interval = 300 };

    /// <summary>
    /// The palette as a notice, palette and help in one (F1 and Ctrl+Comma since 2026-11-26): one field, the list
    /// under it, Enter runs the selected command with the focus back where it was, Escape closes (AX-10.1, AX-10.4).
    /// With nothing typed the list is the F1 text's lines, the key rules first, then every command (AX-10.2). The
    /// entries are read from the menu bar now, before the notice disables it, so that what the palette offers is what
    /// the menus offer at this moment.
    /// </summary>
    protected void ShowCommandPalette()
    {
        if (NoticeOpen)
        {
            return;
        }

        // Commands only (AX-10.5, the maintainer's decision of 2026-11-23): no window adds entries of its own; the
        // lines of the keys' help are the bundle's, the same in every window.
        _paletteAll = [.. MenuLines(CommandOrigin.Palette)];
        _paletteHelp = Palette.WithNothingTyped(Shortcuts.ReplaceLineEndings("\n"), _paletteAll, KeyReference.AloneLines(CommandRows().Commands));
        _paletteShown = _paletteHelp;
        ShowNotice(new Notice(PaletteTitle, string.Empty,
        [
            new OverlayChoice("&Run", RunPaletteEntry, IsDefault: true, StaysOpen: true),
            new OverlayChoice("&Close", IsCancel: true),
        ])
        {
            Input = new OverlayInput("&Search commands:", "Search commands", string.Empty, string.Empty, Required: false,
                KeyHook: OnPaletteKey, Changed: OnPaletteTyped, ListName: "Commands"),
        }.ByUser());
        FillPalette();
    }

    private void OnPaletteTyped()
    {
        _paletteSpeak.Stop();
        FillPalette();
        if (Overlay.InputText.Trim().Length > 0)
        {
            _paletteSpeak.Start();
        }
    }

    private void FillPalette()
    {
        var typed = Overlay.InputText.Trim().Length > 0;
        _paletteShown = typed ? Palette.Find(_paletteAll, Overlay.InputText) : _paletteHelp;
        var lines = _paletteShown.Count > 0 ? _paletteShown.Select(entry => entry.Text).ToList() : [Palette.NoMatch(Overlay.InputText)];
        Overlay.SetMatches(lines, $"{(typed ? "Commands" : "Keys and commands")}, {_paletteShown.Count}");
    }

    /// <summary>The first match and the count after the typing paused, or that nothing matches (AX-10.4).</summary>
    private void SayFirstPaletteMatch()
    {
        _paletteSpeak.Stop();
        if (!NoticeOpen || Overlay.InputText.Trim().Length == 0)
        {
            return;
        }

        Announce(_paletteShown.Count > 0 ? Palette.Spoken(_paletteShown[0], 0, _paletteShown.Count) : Palette.NoMatch(Overlay.InputText), true);
    }

    /// <summary>Down, Up, Page Down, Page Up, Ctrl+Home and Ctrl+End in the field move through the matches (AX-10.4).</summary>
    private bool OnPaletteKey(Keys key)
    {
        var index = Overlay.MatchIndex;
        int? target = key switch
        {
            CommandPalette.NextMatch => index + 1,
            CommandPalette.PreviousMatch => index - 1,
            BundleKeys.PageDown => index + Overlay.MatchPage,
            BundleKeys.PageUp => index - Overlay.MatchPage,
            CommandPalette.FirstMatch => 0,
            CommandPalette.LastMatch => _paletteShown.Count - 1,
            _ => null,
        };
        if (target is not { } wanted)
        {
            return false;
        }

        _paletteSpeak.Stop();
        // At an end nothing moves: the edge earcon, and then the match again, so that the last thing heard is the
        // position and not the typed text (AX-10.4); the list under the field does it, as the hotkey windows' does.
        var on = Overlay.MoveMatch(wanted, _paletteShown.Count);
        Announce(on < 0 ? Palette.NoMatch(Overlay.InputText) : Palette.Spoken(_paletteShown[on], on, _paletteShown.Count), true);
        return true;
    }

    /// <summary>Enter: the palette closes, the focus goes back to the control it came from (the notice rule), and then the entry runs where the user was (AX-10.4).</summary>
    private void RunPaletteEntry()
    {
        _paletteSpeak.Stop();
        var index = Overlay.MatchIndex;
        if (index < 0 || index >= _paletteShown.Count)
        {
            Announce(Palette.NoMatch(Overlay.InputText), true);
            return;
        }

        var entry = _paletteShown[index];
        if (!entry.IsCommand)
        {
            // A line of the keys' help runs nothing: it is said again and the palette stays (AX-10.4).
            Announce(entry.Text, true);
            return;
        }

        CloseNotice(null);
        BeginInvoke(entry.Run);
    }
}
