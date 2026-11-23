using AxKit.Forms;

namespace AxKit.Tests;

/// <summary>
/// The key tables (docs/axKit/SPEC.md AX-8.4): no window-specific key equals a bundle-wide key, no table repeats a
/// key, and the keys two windows use for different actions are listed so that a collision between apps is seen
/// before it is felt. The text a menu shows for a key (AX-8.5) is checked here too, and the key as a chord, the words
/// a script presses it with (docs/axKit/user-workflows.md WF-2.2).
/// </summary>
public sealed class KeyTests
{
    /// <summary>The name of the one table that is no app's first window: axTemplate's launcher, beside its browser (TP-D8).</summary>
    private const string TemplateLauncher = "axTemplate launcher";

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<(string Name, Keys Key)>> Windows =
        new Dictionary<string, IReadOnlyList<(string Name, Keys Key)>>
        {
            ["axDown"] = AxDown.AxDownKeys.All,
            ["axJSON"] = AxJSON.AxJSONKeys.All,
            ["axClaude"] = AxClaude.AxClaudeKeys.All,
            ["axGIT"] = AxGit.AxGitKeys.All,
            ["axTerminal"] = AxTerminal.AxTerminalKeys.All,
            ["axFile"] = AxFile.AxFileKeys.All,
            ["axMedia"] = AxMedia.AxMediaKeys.All,
            ["axPreview"] = AxPreview.AxPreviewKeys.All,
            ["axStart"] = AxStart.AxStartKeys.All,
            ["axProcess"] = AxProcess.AxProcessKeys.All,
            ["axTask"] = AxTask.AxTaskKeys.All,
            ["axPass"] = AxPass.AxPassKeys.All,
            ["axCalc"] = AxCalc.AxCalcKeys.All,
            ["axClip"] = AxClip.AxClipKeys.All,
            ["axMCP"] = AxMCP.AxMCPKeys.All,
            ["axHelp"] = AxHelp.AxHelpKeys.All,
            ["axAutomate"] = AxAutomate.AxAutomateKeys.All,
            ["axTemplate"] = AxTemplate.AxTemplateKeys.All,
            // axTemplate's second window, the launcher behind the hotkey (docs/axTemplate/SPEC.md TP-D8): a table of its own.
            [TemplateLauncher] = AxTemplate.AxTemplateKeys.Launcher,
        };

    /// <summary>
    /// The window keys that take a bundle key's place on purpose, each with its spec line (bundle AX-8.4). axTerminal's
    /// command field: Ctrl+Up and Ctrl+Down go through the history instead of by paragraph (AT-3.9, the user's choice
    /// on 2026-10-23 over Up and Down on the field's first and last line). The item apps' navigator: the same keys go by
    /// section (docs/axKit/item-apps.md IA-2.3a, the user's ask of 2026-10-24); a list has no paragraphs.
    /// </summary>
    private static readonly HashSet<(string Window, Keys Key)> Overrides =
    [
        ("axTerminal", Keys.Control | Keys.Up),
        ("axTerminal", Keys.Control | Keys.Down),
        ("axTask", Keys.Control | Keys.Up),
        ("axTask", Keys.Control | Keys.Down),
        ("axPass", Keys.Control | Keys.Up),
        ("axPass", Keys.Control | Keys.Down),
        ("axMCP", Keys.Control | Keys.Up),
        ("axMCP", Keys.Control | Keys.Down),
        ("axAutomate", Keys.Control | Keys.Up),
        ("axAutomate", Keys.Control | Keys.Down),
        // axMedia: faster and slower (docs/axMedia/SPEC.md AM-D8, the user's choice of 2026-11-14); a list, no text.
        ("axMedia", Keys.Control | Keys.Up),
        ("axMedia", Keys.Control | Keys.Down),
    ];

    /// <summary>
    /// The table above and the F1 texts below are written by hand, so they are held to <c>Dispatch.Apps</c>, the one
    /// list of the apps: every app has a window, and every window its key table and its F1 text here. An app added to
    /// <c>Dispatch.Apps</c> and forgotten here would have its keys checked by nothing (the root CLAUDE.md, rule 5); an
    /// app without a window, should one come, is taken out of the expected names in this test, by name.
    /// </summary>
    [Fact]
    public void The_tables_name_exactly_the_apps()
    {
        // And the one second window of an app, which has a table and an F1 text of its own.
        var apps = AxKit.Core.Dispatch.Apps.Select(app => app.Name).Append(TemplateLauncher).OrderBy(name => name, StringComparer.Ordinal).ToList();
        Assert.Equal(apps, Windows.Keys.OrderBy(name => name, StringComparer.Ordinal));
        Assert.Equal(apps, F1Texts.Keys.OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void No_window_key_equals_a_bundle_key()
    {
        var bundle = BundleKeys.All.ToDictionary(k => k.Key, k => k.Name);
        var collisions = new List<string>();
        foreach (var (window, keys) in Windows)
        {
            foreach (var (name, key) in keys)
            {
                if (bundle.TryGetValue(key, out var bundleName) && !Overrides.Contains((window, key)))
                {
                    collisions.Add($"{window}: {name} is {key}, the bundle's {bundleName}.");
                }
            }
        }

        Assert.Empty(collisions);
    }

    [Fact]
    public void No_table_repeats_a_key()
    {
        var tables = new Dictionary<string, IReadOnlyList<(string Name, Keys Key)>>(Windows) { ["Bundle"] = BundleKeys.All };
        var repeats = new List<string>();
        foreach (var (table, keys) in tables)
        {
            foreach (var group in keys.GroupBy(k => k.Key).Where(g => g.Count() > 1))
            {
                repeats.Add($"{table}: {group.Key} is {string.Join(" and ", group.Select(k => k.Name))}.");
            }
        }

        Assert.Empty(repeats);
    }

    /// <summary>
    /// AX-8.10 rule 3: Ctrl+Enter is the bundle's main action and no window's own key. A window gives it its main
    /// action as a command on <see cref="BundleKeys.MainAction"/> (axGIT's commit, axMCP's call, axFile's play the
    /// folder, the console windows' send); a table that names Ctrl+Enter for anything else fails the collision test
    /// above, and this one says why in plain words. axTerminal's "send without recording" lost the key on 2026-11-14.
    /// </summary>
    [Fact]
    public void Ctrl_Enter_is_the_bundles_main_action_and_in_no_windows_table()
    {
        Assert.Equal(Keys.Control | Keys.Return, BundleKeys.MainAction);
        Assert.Contains(BundleKeys.All, key => key.Key == BundleKeys.MainAction);
        var own = Windows.Where(window => window.Value.Any(key => key.Key == BundleKeys.MainAction)).Select(window => window.Key).ToList();
        Assert.True(own.Count == 0, "Ctrl+Enter is the bundle's main action (AX-8.10), not a window's own key: " + string.Join(", ", own));
    }

    /// <summary>
    /// AX-8.11: a system-wide key, a background app's hotkey as it is at first or the one kept for a later help
    /// launcher, is in no table: Windows takes it from every program, so a window's command on it would never run.
    /// </summary>
    [Fact]
    public void No_table_holds_a_system_wide_key()
    {
        var tables = new Dictionary<string, IReadOnlyList<(string Name, Keys Key)>>(Windows) { ["Bundle"] = BundleKeys.All };
        var taken = new List<string>();
        foreach (var (table, keys) in tables)
        {
            foreach (var (name, key) in keys)
            {
                var systemWide = BundleKeys.SystemWide.FirstOrDefault(k => k.Key == key);
                if (systemWide.Name is not null)
                {
                    taken.Add($"{table}: {name} is {BundleKeys.Text(key)}, system-wide: {systemWide.Name}.");
                }
            }
        }

        Assert.Empty(taken);
    }

    /// <summary>
    /// AX-8.11: the list the test above reads holds the hotkeys the background apps really start with (a default
    /// changed in its settings class must change here), Ctrl+Shift+Period, kept and registered by nothing, and no
    /// Windows+Alt key of axHelp's or axClip's, retired on 2026-11-14.
    /// </summary>
    [Fact]
    public void The_system_wide_keys_are_the_background_apps_defaults_and_the_reserved_one()
    {
        // New settings, as an app has them without a file.
        var start = new AxStart.Core.StartSettings().Key;
        var help = new AxHelp.Core.HelpSettings();
        var clip = new AxClip.Core.ClipSettings().Key;
        var template = new AxTemplate.Core.TemplateSettings().Key;
        Keys[] expected =
        [
            AsKeys(start),
            AsKeys(help.Key),
            AsKeys(help.ScreenshotKey),
            AsKeys(clip),
            AsKeys(template),
            Keys.Control | Keys.Shift | Keys.OemPeriod,
        ];
        Assert.Equal(expected, BundleKeys.SystemWide.Select(k => k.Key));
        Assert.Equal(["Ctrl+Shift+Space", "Ctrl+Shift+Slash", "Ctrl+Shift+Comma", "Ctrl+Shift+Apostrophe", "Ctrl+Shift+Semicolon"], new[] { start, help.Key, help.ScreenshotKey, clip, template }.Select(key => key.ToString()));

        static Keys AsKeys(AxKit.Core.Hotkey hotkey) =>
            (Keys)hotkey.Key
            | ((hotkey.Modifiers & AxKit.Core.Hotkey.Control) != 0 ? Keys.Control : Keys.None)
            | ((hotkey.Modifiers & AxKit.Core.Hotkey.Shift) != 0 ? Keys.Shift : Keys.None)
            | ((hotkey.Modifiers & AxKit.Core.Hotkey.Alt) != 0 ? Keys.Alt : Keys.None);
    }

    private static readonly IReadOnlyDictionary<string, string> F1Texts =new Dictionary<string, string>
    {
        ["axDown"] = AxDown.HelpText.Shortcuts,
        ["axJSON"] = AxJSON.HelpText.Shortcuts,
        ["axClaude"] = AxClaude.HelpText.Shortcuts,
        ["axGIT"] = AxGit.HelpText.Shortcuts,
        ["axTerminal"] = AxTerminal.HelpText.Shortcuts,
        ["axFile"] = AxFile.HelpText.Shortcuts,
        ["axMedia"] = AxMedia.HelpText.Shortcuts,
        ["axPreview"] = AxPreview.HelpText.Shortcuts,
        ["axStart"] = AxStart.HelpText.Shortcuts,
        ["axProcess"] = AxProcess.HelpText.Shortcuts,
        ["axTask"] = AxTask.HelpText.Shortcuts,
        ["axPass"] = AxPass.HelpText.Shortcuts,
        ["axCalc"] = AxCalc.HelpText.Shortcuts,
        ["axClip"] = AxClip.HelpText.Shortcuts,
        ["axMCP"] = AxMCP.HelpText.Shortcuts,
        ["axHelp"] = AxHelp.HelpText.Shortcuts,
        ["axAutomate"] = AxAutomate.HelpText.Shortcuts,
        ["axTemplate"] = AxTemplate.HelpText.Shortcuts,
        [TemplateLauncher] = AxTemplate.HelpText.LauncherShortcuts,
    };

    /// <summary>
    /// The F1 text of every window names every key of its table that has a modifier or is a function key (AX-8.3:
    /// the text is changed with the table), so that a key change never leaves the help text behind. Plain letters
    /// and digits are left out: a letter is named by its word in the text ("h: the next heading").
    /// </summary>
    [Fact]
    public void Every_window_key_is_named_in_its_F1_text()
    {
        var missing = new List<string>();
        foreach (var (window, keys) in Windows)
        {
            foreach (var (name, key) in keys)
            {
                var code = key & Keys.KeyCode;
                var modified = (key & Keys.Modifiers) != 0 && code != Keys.None;
                var function = code is >= Keys.F1 and <= Keys.F24;
                // A number row key's text ends in a full stop for speech ("Ctrl+1."); the help text writes it bare.
                var text = BundleKeys.Text(key).TrimEnd('.');
                if ((modified || function) && !F1Texts[window].Contains(text, StringComparison.OrdinalIgnoreCase))
                {
                    missing.Add($"{window}: {name} ({text})");
                }
            }
        }

        Assert.True(missing.Count == 0, "Not in the F1 text: " + string.Join("; ", missing));
    }

    /// <summary>Every F1 text names the axKit menu with its items in the bundle's words, the whole line (AX-9; only its start until 2026-10-24, when two texts had drifted), so that the menu is found from any window's help and the texts do not drift apart.</summary>
    [Fact]
    public void Every_F1_text_names_the_AxKit_menu_and_its_first_items()
    {
        const string line = "Alt+W: the Window menu: a new window of any app, the open axKit windows, most recent first (Alt+W, O, 1 is the one you were in last), the files and folders closed lately, Close all windows, Say the status. Alt+A: the axKit menu: Claude (Use Claude in axKit, Ask about this window, the folders Claude may read, Claude's working folder), the AI tools and the theme. Alt+H: Help: the command palette, this list, the user guide, Send a report, Your reports, Diagnostics (Test a key, Copy diagnostics), Check for updates, and About";
        var missing = F1Texts.Where(pair => !pair.Value.Contains(line, StringComparison.Ordinal)).Select(pair => pair.Key).ToList();
        Assert.True(missing.Count == 0, "Without the axKit menu line: " + string.Join(", ", missing));
    }

    /// <summary>Every F1 text names the bundle keys every window has (AX-8.2, AX-10.6): F1, the palette, find, the text size.</summary>
    [Fact]
    public void Every_F1_text_names_the_keys_every_window_has()
    {
        Keys[] everywhere = [BundleKeys.Shortcuts, BundleKeys.CommandPalette, BundleKeys.Report, BundleKeys.AskClaude, BundleKeys.AskClaudeAlso, BundleKeys.ContextMenu, BundleKeys.SayStatus, BundleKeys.MainAction, BundleKeys.ToggleMode, BundleKeys.Find, BundleKeys.FindNext, BundleKeys.FindPrevious, BundleKeys.LargerText, BundleKeys.SmallerText];
        var missing = new List<string>();
        foreach (var (window, text) in F1Texts)
        {
            foreach (var key in everywhere)
            {
                if (!text.Contains(BundleKeys.Text(key), StringComparison.OrdinalIgnoreCase))
                {
                    missing.Add($"{window}: {BundleKeys.Text(key)}");
                }
            }
        }

        Assert.True(missing.Count == 0, "Not in the F1 text: " + string.Join("; ", missing));
    }

    /// <summary>
    /// AX-8.4: the keys two windows use for different actions, held here as the known list, so that a new one fails
    /// this test and is added on purpose rather than felt under the fingers. Most are one gesture with the window's
    /// own meaning (Ctrl+N new, Ctrl+O open, Enter act on the item). The list is by key, so that renaming an action
    /// changes nothing here; the test's message names the windows and actions of a key not in the list.
    /// </summary>
    [Fact]
    public void Keys_used_differently_by_two_windows_are_the_known_ones()
    {
        string[] known =
        [
            "Alt+1", "Alt+2", "Alt+3", "Alt+Up", "Ctrl+Down", "Ctrl+J", "Ctrl+M", "Ctrl+Shift+N", "Ctrl+Up", "F2", "Left", "Right",
            // Tab and Shift+Tab left this list on 2026-11-22: they went round axProcess's three views until it became a navigator.
            "Apps", "Backspace", "Ctrl+1", "Ctrl+B", "Ctrl+2", "Ctrl+3", "Ctrl+4", "Ctrl+5", "Ctrl+6", "Ctrl+C", "Ctrl+D", "Ctrl+E", "Ctrl+G", "Ctrl+I", "Ctrl+K", "Ctrl+L",
            // Ctrl+Shift+1 to 6: the previous heading of a level in the editors, an action of the list in axTask (forms-framework.md AF-6.1).
            "Ctrl+Shift+1", "Ctrl+Shift+2", "Ctrl+Shift+3", "Ctrl+Shift+4", "Ctrl+Shift+5", "Ctrl+Shift+6",
            // Ctrl+Shift+A left this list on 2026-11-22: it staged all in axGIT, and is Deselect all in every window now.
            "Ctrl+N", "Ctrl+O", "Ctrl+P", "Ctrl+Q", "Ctrl+R", "Ctrl+Shift+C", "Ctrl+Shift+F", "Ctrl+Shift+H", "Ctrl+Shift+I",
            "Ctrl+Shift+K", "Ctrl+Shift+L", "Ctrl+Shift+Q", "Ctrl+Shift+R", "Ctrl+Shift+S", "Ctrl+Shift+T", "Ctrl+Shift+W", "Ctrl+T",
            "Ctrl+W", "Ctrl+Z", "Delete", "Down", "Enter", "Escape", "F5", "O", "Shift+Enter", "Shift+Escape", "Shift+F10", "Space", "Up",
        ];
        var differing = Windows.SelectMany(w => w.Value.Select(k => (Window: w.Key, k.Name, k.Key)))
            .GroupBy(k => k.Key)
            .Where(g => g.Select(k => k.Name).Distinct().Count() > 1)
            .ToDictionary(g => BundleKeys.Text(g.Key).TrimEnd('.'), g => string.Join(", ", g.Select(k => $"{k.Window} {k.Name}")));
        var unexpected = differing.Where(pair => !known.Contains(pair.Key)).Select(pair => $"{pair.Key}: {pair.Value}").ToList();
        Assert.True(unexpected.Count == 0, "Used differently by two windows and not in the known list: " + string.Join("; ", unexpected));
        Assert.Equal(known.OrderBy(text => text, StringComparer.Ordinal), differing.Keys.OrderBy(text => text, StringComparer.Ordinal));
    }

    /// <summary>
    /// AX-8.5: the menu text of a key; a key of the number row ends in a full stop, so that NVDA does not run
    /// "Ctrl+1" and the item's position "1 of 9" together into "Ctrl+11 of 9".
    /// </summary>
    [Theory]
    [InlineData(Keys.Control | Keys.D1, "Ctrl+1.")]
    [InlineData(Keys.Control | Keys.D3, "Ctrl+3.")]
    [InlineData(Keys.Alt | Keys.D1, "Alt+1.")]
    [InlineData(Keys.Control | Keys.OemPeriod, "Ctrl+Period")]
    [InlineData(Keys.Control | Keys.OemQuestion, "Ctrl+Slash")]
    [InlineData(Keys.Control | Keys.Shift | Keys.S, "Ctrl+Shift+S")]
    [InlineData(Keys.F3, "F3")]
    [InlineData(Keys.Shift | Keys.Escape, "Shift+Escape")]
    [InlineData(Keys.Control | Keys.Oemplus, "Ctrl+Plus")]
    [InlineData(Keys.Control | Keys.Shift | Keys.OemPeriod, "Ctrl+Shift+Period")]
    [InlineData(Keys.Control | Keys.Shift | Keys.Oemcomma, "Ctrl+Shift+Comma")]
    [InlineData(Keys.Control | Keys.Shift | Keys.OemQuestion, "Ctrl+Shift+Slash")]
    [InlineData(Keys.Control | Keys.Shift | Keys.OemSemicolon, "Ctrl+Shift+Semicolon")]
    [InlineData(Keys.Control | Keys.Up, "Ctrl+Up")]
    [InlineData(Keys.Back, "Backspace")]
    [InlineData(Keys.Return, "Enter")]
    [InlineData(Keys.Alt | Keys.Return, "Alt+Enter")]
    [InlineData(Keys.PageDown, "Page Down")]
    [InlineData(Keys.Control | Keys.PageUp, "Ctrl+Page Up")]
    public void Menu_text_of_a_key(Keys key, string expected)
    {
        Assert.Equal(expected, BundleKeys.Text(key));
    }

    /// <summary>
    /// docs/axKit/user-workflows.md WF-2.2: a key as data, in the words a script's <c>keys</c> step presses it with
    /// (<c>AxKit.Core/KeyChord</c>): lower case, the modifiers in the order ctrl, shift, alt, no full stop after a
    /// number-row key; empty for no key and for a key the vocabulary has no name for.
    /// </summary>
    [Theory]
    [InlineData(Keys.Control | Keys.Shift | Keys.W, "ctrl+shift+w")]
    [InlineData(Keys.Control | Keys.D1, "ctrl+1")]
    [InlineData(Keys.Control | Keys.Return, "ctrl+enter")]
    [InlineData(Keys.Control | Keys.Oemplus, "ctrl+plus")]
    [InlineData(Keys.Control | Keys.Add, "ctrl+add")]
    [InlineData(Keys.Control | Keys.Shift | Keys.OemQuestion, "ctrl+shift+slash")]
    [InlineData(Keys.Control | Keys.Shift | Keys.OemSemicolon, "ctrl+shift+semicolon")]
    [InlineData(Keys.Alt | Keys.Shift | Keys.Control | Keys.Up, "ctrl+shift+alt+up")]
    [InlineData(Keys.F3, "f3")]
    [InlineData(Keys.Shift | Keys.Escape, "shift+escape")]
    [InlineData(Keys.Back, "backspace")]
    [InlineData(Keys.Apps, "apps")]
    [InlineData(Keys.None, "")]
    [InlineData(Keys.Control | Keys.Scroll, "")]
    public void A_key_is_a_chord_in_the_scripts_words(Keys key, string expected)
    {
        Assert.Equal(expected, BundleKeys.Chord(key));
    }

    /// <summary>
    /// WF-2.2: every key of the bundle's table and of every window's has a chord, and the chord names that very key,
    /// so that a window can list each of its commands' keys as data and a script can press what the window listed. A
    /// key without a name in <c>KeyChord</c> would be listed with its menu text alone, and no step could press it by
    /// the command's name: give the vocabulary its name, with its code, when a table gains such a key.
    /// </summary>
    [Fact]
    public void Every_key_of_the_tables_has_a_chord_that_names_it()
    {
        var tables = new Dictionary<string, IReadOnlyList<(string Name, Keys Key)>>(Windows) { ["Bundle"] = BundleKeys.All };
        var wrong = new List<string>();
        foreach (var (table, keys) in tables)
        {
            foreach (var (name, key) in keys)
            {
                // Modifiers alone stand for several keys (the console windows' Ctrl+Alt with a quick key letter): no
                // one key, and so no chord.
                if ((key & Keys.KeyCode) == Keys.None)
                {
                    continue;
                }

                var chord = BundleKeys.Chord(key);
                var code = AxKit.Core.KeyChord.Code(chord.Split('+')[^1]);
                if (chord.Length == 0 || code != (int)(key & Keys.KeyCode))
                {
                    wrong.Add($"{table}: {name} ({BundleKeys.Text(key)}) has the chord \"{chord}\"");
                }
            }
        }

        Assert.True(wrong.Count == 0, "A key of a table without a chord of its own (AxKit.Core/KeyChord): " + string.Join("; ", wrong));
    }
}
