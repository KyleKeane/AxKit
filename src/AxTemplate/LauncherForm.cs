using System.Runtime.InteropServices;
using AxKit.Core;
using AxKit.Forms;
using AxTemplate.Core;
using static AxKit.Forms.User32;

namespace AxTemplate;

/// <summary>
/// axTemplate's launcher (SPEC.md TP-1.2, TP-3, TP-4): the bundle's background window behind a hotkey
/// (<see cref="HotkeyForm"/>), as axClip's: one field with the caret always in it, "Search templates", and under it
/// the list of the templates that match, never focused. Enter puts the template into the program that was in
/// front, by the clipboard and that program's own paste key (TP-D3); Shift+Enter only copies it; Ctrl+Q reads it;
/// Ctrl+B opens the browser. The launcher makes, changes and deletes nothing (TP-4.6).
/// </summary>
public sealed class LauncherForm : HotkeyForm
{
    /// <summary>How long after the window hid a paste waits for the program to be in front again (TP-4.1, axClip's CP-4.1).</summary>
    private const int PasteDelayMs = 150;

    private readonly TemplateSettings _settings;
    private readonly string _folder = AppPaths.TemplatesRoot;
    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _countLabel;
    private readonly ToolStripStatusLabel _keyLabel;

    private IReadOnlyList<Template> _templates = [];
    private IReadOnlyList<Template> _matches = [];

    /// <summary>The clipboard's text at the moment the launcher came up (TP-2.3): what <c>{clipboard}</c> stands for.</summary>
    private string _clipboard = string.Empty;

    /// <summary>The latest read of the folder: an earlier one that ends later is dropped.</summary>
    private int _reads;

    public LauncherForm(bool hidden, TemplateSettings settings, string? settingsError)
        : base(AxTemplateApp.Name, settings, TemplateSettings.DefaultPath, settingsError, hidden)
    {
        _settings = settings;

        // ---- menus and keys (TP-7.1): one command list (docs/axKit/app-kit.md AK-2), each key from the table; the
        // match keys first, as in axClip ----
        AddMatchKeys(AxTemplateKeys.NextMatch, AxTemplateKeys.PreviousMatch, AxTemplateKeys.FirstMatch, AxTemplateKeys.LastMatch);
        var session = new ToolStripMenuItem();
        Add(session, new Command("Open the &browser", AxTemplateKeys.OpenBrowser, OpenBrowser) { Mark = CommandMark.Open });
        AddHideAndQuit(session, AxTemplateKeys.Hide);
        var context = new ToolStripMenuItem();
        Add(context, new Command("&Put in", AxTemplateKeys.PutIn, PutIn) { Mark = CommandMark.Act });
        Add(context, new Command("&Copy without putting in", AxTemplateKeys.CopyOnly, CopyOnly) { Mark = CommandMark.Act });
        Add(context, new Command("&Read in full", AxTemplateKeys.ReadInFull, ReadInFull) { Mark = CommandMark.Read });

        var options = new ToolStripMenuItem();
        options.DropDownItems.Add(HotkeyItem());
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu(StartWithWindowsItem(), SayFirstMatchItem()));
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "axTemplate &resources",
            [("The &marks of a template", "https://github.com/KyleKeane/AxKit/blob/main/docs/axTemplate/user-guide.md")]);

        PlaceMenuBar(_menu, session, new ToolStripMenuItem(), new ToolStripMenuItem(), context, options, help);
        MainMenuStrip = _menu;

        // ---- the field and the matches (TP-3.1): HotkeyForm's, named here ----
        SearchField.AccessibleName = "Search templates";
        Matches.DoubleClick += (_, _) => PutIn();

        (_countLabel, _keyLabel) = StatusLayout.Pair(_status);

        Controls.AddRange([Overlay, Matches, SearchField, _status, _menu]);
        ApplyTextFont();
        Fill();
        _ = ReadFolderAsync();
    }

    // ---- what the window bases ask for ----

    protected override string Shortcuts => HelpText.LauncherShortcuts;

    protected override void HideForNotice()
    {
        SearchField.Visible = false;
        Matches.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        SearchField.Visible = true;
        Matches.Visible = true;
    }

    protected override Control FocusTarget => SearchField;

    protected override void ApplyFont(Font font)
    {
        SearchField.Font = font;
        Matches.Font = font;
    }

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Templates: {_templates.Count}",
        $"Hotkey: {_settings.Key}, {(HotkeyRegistered ? "registered" : "not registered")}",
    ];

    protected override IReadOnlyList<string> RestartArguments() => [Dispatch.AxTemplate, AxTemplateApp.LauncherArgument];

    /// <summary>
    /// The field emptied, the clipboard's text taken for <c>{clipboard}</c> (TP-2.3), the folder read again off the
    /// UI thread (TP-2.5), and the window centred on the screen of the program that had the focus.
    /// </summary>
    protected override void PrepareShowing(Rectangle area, bool keepText)
    {
        if (!NoticeOpen && !keepText)
        {
            SearchField.Clear();
        }

        _clipboard = ClipboardText();
        Fill();
        _ = ReadFolderAsync();
        var width = Math.Max(MinimumSize.Width, area.Width / 3);
        var height = Math.Max(MinimumSize.Height, Height - ClientSize.Height + _menu.Height + SearchField.Height + Matches.ItemHeight * 12 + _status.Height + 4);
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
    }

    protected override void AfterShowing()
    {
        if (!NoticeOpen)
        {
            SearchField.Select();
        }
    }

    // ---- the templates (TP-2.5, TP-3) ----

    /// <summary>The folder read off the UI thread, then the list made again with the selection kept where its template still is.</summary>
    private async Task ReadFolderAsync()
    {
        var read = ++_reads;
        TemplateFolder folder;
        try
        {
            var path = _folder;
            folder = await Task.Run(() =>
            {
                TemplateStore.Open(path);
                return TemplateStore.Read(path);
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // TP-7.3: that it failed, never a name.
            Log.Error("The templates' folder could not be read: " + ex.GetType().Name);
            return;
        }

        if (read != _reads || IsDisposed)
        {
            return;
        }

        var keep = Selected?.Path;
        _templates = folder.Templates;
        Fill(keep);
    }

    private Template? Selected => Matches.SelectedIndex >= 0 && Matches.SelectedIndex < _matches.Count ? _matches[Matches.SelectedIndex] : null;

    protected override int MatchCount => _matches.Count;

    protected override string SpokenMatch(int index) => TemplateMatch.Spoken(_matches[index], index, _matches.Count);

    protected override string NothingSaid => TemplateMatch.Nothing(SearchField.Text, _templates.Count);

    protected override void Refill() => Fill();

    /// <summary>The list made again from the typed text (TP-3.2), the selection on the template of <paramref name="keep"/> when it still matches, the first otherwise.</summary>
    private void Fill(string? keep = null)
    {
        _matches = TemplateMatch.Find(_templates, SearchField.Text, _settings.RecentTemplates);
        var index = keep is null ? 0 : Math.Max(0, _matches.ToList().FindIndex(template => string.Equals(template.Path, keep, StringComparison.OrdinalIgnoreCase)));
        ShowMatches(_matches.Select(template => template.Title).ToList(), NothingSaid, $"Templates, {_matches.Count}", index);
        var count = _templates.Count;
        _countLabel.Text = count == 1 ? "1 template" : $"{count} templates";
        _keyLabel.Text = _settings.Key.ToString();
        StatusLayout.Fit(_status, _countLabel, _keyLabel);
    }

    // ---- using a template (TP-4) ----

    /// <summary>
    /// The selected template with its marks filled (TP-2.3), its file read again off the UI thread so that a change
    /// made in the browser a moment ago is in (TP-2.5), and its name put first among the ones used last; null, with
    /// the reason said, when no template is selected or its text makes nothing (the clipboard takes no empty text).
    /// </summary>
    private async Task<TemplateText?> MadeAsync(string selection)
    {
        if (Selected is not { } template)
        {
            Announce(NothingSaid, true);
            return null;
        }

        var clipboard = _clipboard;
        var text = template.Text;
        try
        {
            text = await Task.Run(() =>
                Changes.ReadOrNull(template.Path) is { } now && TemplateStore.Parse(template.Path, now) is { } fresh ? fresh.Text : template.Text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // The text as the folder's read gave it.
        }

        var made = TextTemplate.Expand(text, selection, clipboard);
        if (made.Text.Length == 0)
        {
            Announce(TemplateStore.NoTextYet(template.Title) + ": Ctrl+B opens the browser, where it is written", true);
            return null;
        }

        _settings.RecentTemplates = TemplateMatch.Used(_settings.RecentTemplates, template.Title);
        SaveSettings();
        return made;
    }

    /// <summary>Enter (TP-4.1): the filled text on the clipboard, the window hidden with the front given back, then the paste key once that program is in front again.</summary>
    private void PutIn() => Do(async () =>
    {
        if (await MadeAsync(string.Empty) is not { } made || !CopyText(made.Text, null))
        {
            return;
        }

        var target = ForegroundBefore;
        // TP-7.3: that a template was put in, never its name or its text.
        Log.Info("Hidden: a template put in");
        HideWindow();
        await Task.Delay(PasteDelayMs);
        if (target != 0 && GetForegroundWindow() == target)
        {
            KeySender.SendCtrlV();
            Log.Info("Pasted");
        }
        else
        {
            // TP-4.1: nothing typed into a window the user did not come from; the text waits on the clipboard.
            Log.Info("The paste was left undone: another window is in front");
        }
    });

    /// <summary>Shift+Enter (TP-4.4).</summary>
    private void CopyOnly() => Do(async () =>
    {
        if (await MadeAsync(string.Empty) is { } made && CopyText(made.Text, "Copied"))
        {
            HideWindow();
        }
    });

    /// <summary>Ctrl+Q (TP-4.5): the template's own text, marks and all, in a notice.</summary>
    private void ReadInFull()
    {
        if (Selected is not { } template)
        {
            Announce(NothingSaid, true);
            return;
        }

        var lines = template.Text.ReplaceLineEndings("\n").Split('\n').Length;
        ShowNotice(Notice.Plain($"{template.Title}, {lines} {(lines == 1 ? "line" : "lines")}", template.Text));
    }

    /// <summary>Ctrl+B (TP-4.6): the browser, a window of its own; the launcher hides and leaves the front to it.</summary>
    private void OpenBrowser()
    {
        Log.Info("Hidden: the browser opened");
        HideWindow(giveBack: false);
        WindowMenu.Start(Dispatch.AxTemplate, null);
    }

    /// <summary>The clipboard's text now; empty when it holds none or another program holds it open.</summary>
    private static string ClipboardText()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
        }
        catch (ExternalException)
        {
            return string.Empty;
        }
    }
}
