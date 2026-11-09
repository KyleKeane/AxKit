using AxKit.Core;

namespace AxKit.Forms;

/// <summary>
/// The theme on this window (docs/axkit/theme.md TH-3, TH-4): the file read as the window loads and again when it has
/// changed by the time the window is activated, painted by <see cref="ThemePainter"/> over the whole window, over a
/// notice as it opens and over a control area as it fills; and the three items of Options, Text, Theme. Without a
/// theme file every window is painted with the classes' defaults, which is what it was before (AX-7.7).
/// </summary>
public abstract partial class AxKitForm
{
    private readonly ToolStripMenuItem _themeOnItem = new("Theme &on");
    private Theme? _theme;
    private DateTime _themeStamp;
    private bool _themeProblemLogged;

    /// <summary>Paints <paramref name="root"/> from the theme; the whole window when it is this form.</summary>
    protected void PaintTheme(Control root) => ThemePainter.Apply(root, _theme);

    /// <summary>Options, Text, Theme ▸: Edit theme file, Reload theme, Theme on (TH-4).</summary>
    private ToolStripMenuItem CreateThemeMenu()
    {
        var menu = new ToolStripMenuItem("T&heme");
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
        }
    }

    private static DateTime ThemeStamp(string path) => File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;

    /// <summary>Options, Text, Theme, Reload theme (TH-3.4): the one time the theme speaks.</summary>
    private void ReloadTheme()
    {
        LoadTheme();
        PaintTheme(this);
        var path = AppPaths.ThemeFile;
        Announce(_theme switch
        {
            null when !File.Exists(path) => "No theme file: Options, Text, Theme, Edit theme file makes one",
            null => "No theme: the file is not a theme file; the log says why",
            { NotUnderstood.Count: 0 } => "Theme reloaded",
            { NotUnderstood: var keys } => $"Theme reloaded: {keys.Count} {(keys.Count == 1 ? "value" : "values")} not understood, {string.Join(", ", keys)}",
        }, true);
    }

    /// <summary>Options, Text, Theme, Edit theme file (TH-4): the template written when there is no file, then axJSON on it.</summary>
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

        AxKitMenu.Start(Dispatch.AxJSON, path);
    }

    /// <summary>Options, Text, Theme, Theme on (TH-1.5): the file's switch, written; the template first when there is none.</summary>
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
        Announce(on ? "Theme on" : "Theme off", true);
    }
}
