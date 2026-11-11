using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The theme on this window (docs/axKit/theme.md TH-3, TH-4): the file read as the window is made (so that a window's
/// constructor can take a display setting from it, TH-6) and again when it has changed by the time the window is
/// activated, painted by <see cref="ThemePainter"/> over the whole window, over a notice as it opens and over a control
/// area as it fills; and the three items of axKit menu, Theme. Without a theme file every window is painted with the
/// classes' defaults, which is what it was before (AX-7.7).
/// </summary>
public abstract partial class AxKitForm
{
    private readonly ToolStripMenuItem _themeOnItem = new("Theme &on");
    private Theme? _theme;
    private DateTime _themeStamp;
    private bool _themeProblemLogged;

    /// <summary>Paints <paramref name="root"/> from the theme; the whole window when it is this form.</summary>
    protected void PaintTheme(Control root) => ThemePainter.Apply(root, _theme);

    /// <summary>TH-6.1: whether the editors wrap long lines, from the theme file's display settings; on without a file.</summary>
    protected bool WordWrapPreference => _theme?.WordWrap ?? true;

    /// <summary>
    /// Writes the display setting into the theme file (the template first when there is none) and reads the file
    /// back, so that this window's stamp is current; every other window takes it when next activated (TH-3.2). The
    /// reason when the file could not be changed, null when it was.
    /// </summary>
    protected string? SetWordWrapPreference(bool on)
    {
        if (Theme.SetWordWrap(AppPaths.ThemeFile, on) is { } problem)
        {
            return problem;
        }

        LoadTheme();
        return null;
    }

    /// <summary>
    /// Options, Text, Say new line (AX-7.8, TH-6.5, 2026-11-11): one item in every window, the bundle's one setting
    /// in the theme file's display settings, as the editors' word wrap is; checked from the file as the menu opens, so
    /// that a change in another window shows here, in the menu and in the palette alike.
    /// </summary>
    private ToolStripMenuItem CreateSayNewLineItem(ToolStripMenuItem textMenu)
    {
        var item = new ToolStripMenuItem("Say &new line", null, (_, _) => ToggleSayNewLine()) { Checked = _theme?.SayNewLine ?? true };
        CommandPalette.RefreshedOnOpening(textMenu, () => item.Checked = _theme?.SayNewLine ?? true);
        return item;
    }

    private void ToggleSayNewLine()
    {
        var on = !(_theme?.SayNewLine ?? true);
        if (Theme.SetSayNewLine(AppPaths.ThemeFile, on) is { } problem)
        {
            Failed("Theme file", "The theme file could not be changed: " + problem);
            return;
        }

        LoadTheme();
        Announce(on ? "Say new line on" : "Say new line off", true);
    }

    /// <summary>
    /// A field of this window took a line break from a key (<see cref="AxKitTextBox"/>, AX-7.8): "new line" through
    /// the window's one announcement, unless Say new line is off. It does not interrupt, so that NVDA's own echo of
    /// the word just typed is still heard first.
    /// </summary>
    internal void SayTypedLineBreak()
    {
        // The file as read at the window's last activation (TH-3.2): the setting changes only from a menu of a window
        // in front, so no file is touched on the key itself.
        if (_theme?.SayNewLine ?? true)
        {
            Announce(TypedLineBreak.Said, false);
        }
    }

    /// <summary>
    /// After the theme has been read again and painted (an activation with the file changed, Reload theme, Theme on):
    /// a window that takes a display setting from it applies it here (the editors' word wrap, TH-6.2). Nothing by default.
    /// </summary>
    protected virtual void OnThemeChanged()
    {
    }

    /// <summary>axKit, Theme ▸ (axKit menu, Theme until 5.7.0): Edit theme file, Reload theme, Theme on (TH-4).</summary>
    private ToolStripMenuItem CreateThemeMenu()
    {
        var menu = new ToolStripMenuItem("&Theme");
        menu.DropDownItems.Add(new ToolStripMenuItem("&Edit theme file...", null, (_, _) => EditThemeFile()));
        menu.DropDownItems.Add(new ToolStripMenuItem("&Reload theme", null, (_, _) => ReloadTheme()));
        _themeOnItem.Click += (_, _) => ToggleTheme();
        menu.DropDownItems.Add(_themeOnItem);
        return menu;
    }

    private void LoadTheme()
    {
        var path = AppPaths.ThemeFile;
        _themeStamp = ThemeStamp(path);
        _theme = Theme.Load(path, out var problem);
        if (problem is not null && !_themeProblemLogged)
        {
            Log.Error("The theme file was not read: " + problem);
            _themeProblemLogged = true;
        }

        _themeOnItem.Checked = _theme is { Enabled: true };
    }

    /// <summary>TH-3.2: on activation, a file changed since it was read is read and painted again, silently.</summary>
    private void PaintThemeIfChanged()
    {
        if (ThemeStamp(AppPaths.ThemeFile) != _themeStamp)
        {
            LoadTheme();
            PaintTheme(this);
            OnThemeChanged();
        }
    }

    private static DateTime ThemeStamp(string path) => File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;

    /// <summary>axKit menu, Theme, Reload theme (TH-3.4): the one time the theme speaks.</summary>
    private void ReloadTheme()
    {
        LoadTheme();
        PaintTheme(this);
        OnThemeChanged();
        var path = AppPaths.ThemeFile;
        Announce(_theme switch
        {
            null when !File.Exists(path) => "No theme file: axKit menu, Theme, Edit theme file makes one",
            null => "No theme: the file is not a theme file; the log says why",
            { NotUnderstood.Count: 0 } => "Theme reloaded",
            { NotUnderstood: var keys } => $"Theme reloaded: {keys.Count} {(keys.Count == 1 ? "value" : "values")} not understood, {string.Join(", ", keys)}",
        }, true);
    }

    /// <summary>axKit menu, Theme, Edit theme file (TH-4): the template written when there is no file, then axJSON on it.</summary>
    private void EditThemeFile()
    {
        var path = AppPaths.ThemeFile;
        try
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, Theme.Template());
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Failed("Theme file", "The theme file could not be written: " + ex.Message);
            return;
        }

        WindowMenu.Start(Dispatch.AxJSON, path);
    }

    /// <summary>axKit menu, Theme, Theme on (TH-1.5): the file's switch, written; the template first when there is none.</summary>
    private void ToggleTheme()
    {
        var on = _theme is not { Enabled: true };
        if (Theme.SetEnabled(AppPaths.ThemeFile, on) is { } problem)
        {
            Failed("Theme file", "The theme file could not be changed: " + problem);
            return;
        }

        LoadTheme();
        PaintTheme(this);
        OnThemeChanged();
        Announce(on ? "Theme on" : "Theme off", true);
    }
}
