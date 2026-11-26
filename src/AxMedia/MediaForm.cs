using AxKit.Core;
using AxKit.Forms;
using AxMedia.Core;

namespace AxMedia;

/// <summary>
/// axMedia's window (SPEC.md AM-2): a menu bar, the video area shown only while a video plays, one list with the
/// queue, and a status bar with what plays and where. Enter plays the file you are on and the rest follow; the
/// transport keys work from anywhere in the window. The engine is <see cref="MediaPlayer"/>, its events posted
/// back to this thread; the queue is <see cref="PlayQueue"/>. A start with paths while this window is open arrives as
/// a hand-over message (AM-1.2, <see cref="AppMessage"/>) in <see cref="WndProc"/>.
/// The window is a navigator (docs/axKit/framework.md FW-4.9, step 6, 2026-11-22; a window of its own until then,
/// grown from axFile's player and queue pane on 2026-10-22): the bundle's item window (<see cref="ItemForm"/>) with
/// one level, the queue, whose rows are the files, and no second pane. The list, its selection, find, the Context
/// menu and the keys of a navigator are the item window's; this class holds the queue's commands and the player.
/// </summary>
public sealed partial class MediaForm : ItemForm
{
    private readonly MediaSettings _settings;
    private readonly HandOver? _start;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _recentItem = new("Open &recent");
    private readonly Panel _video = new();
    private readonly System.Windows.Forms.Timer _clock = new() { Interval = 1000 };

    /// <summary>The title of a start on one file, until the first file plays (AM-2.3).</summary>
    private string? _startTitle;
    private readonly PlayQueue _queue = new();
    private MediaPlayer? _player;

    /// <summary>How the file now opening was asked for: by the user (said, interrupting), or by the queue moving on (said when the setting says, not interrupting).</summary>
    private bool _prompted;

    /// <summary>The hand-overs waiting to be applied, in the order they came, one at a time (AM-1.3); <see cref="_applying"/> while one is.</summary>
    private readonly Queue<HandOver> _handOvers = new();
    private bool _applying;

    /// <param name="start">What the command line asked for (AM-1.1): a play or an add with its paths; null for an empty queue.</param>
    /// <param name="settingsPath">Where the settings are saved; the app's own file unless a probe says otherwise, so that a probe never writes the user's.</param>
    public MediaForm(HandOver? start, MediaSettings settings, string? settingsError, string? settingsPath = null)
        : base(AxMediaApp.Name, settings, settingsPath ?? MediaSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        _start = start;
        // A start on one file is named after it from the first moment, so that NVDA reads the file's window as it opens (AM-2.3).
        _startTitle = start is { Verb: AppMessage.Play, Paths: [var only] } && File.Exists(only) ? Path.GetFileName(only) : null;
        SetTitle(_startTitle);

        // ---- menus and keys (AM-5.1, AM-5.2): one command list (docs/axKit/app-kit.md AK-2), each key from the tables ----
        // Queue is the thing worked on, in the standard order of a first menu (bundle AX-6): New and Open with the
        // recent list, the items on the file, Show current file in, Exit. The keys of a navigator (Enter, Delete,
        // the selection keys, find, the Context menu's) are the item window's commands, registered after these by
        // PlaceWith; the menu items for them here only show the keys.
        var queue = new ToolStripMenuItem("&Session");
        Add(queue, new Command("&New window", AxMediaKeys.NewWindow, NewWindow) { Mark = CommandMark.Open });
        // Add, since they add to the queue ("Added 3"): Open folder is axFile's, which opens the folder in its list
        // (docs/axKit/command-exceptions.json, one name one thing, 2026-11-25; Open files and Open folder until then).
        Add(queue, new Command("&Add files...", AxMediaKeys.OpenFiles, OpenFiles) { Mark = CommandMark.Open });
        Add(queue, new Command("Add &folder...", AxMediaKeys.OpenFolder, OpenFolder) { Mark = CommandMark.Open });
        CommandPalette.RefreshedOnOpening(_recentItem, FillRecent);
        FillRecent();
        queue.DropDownItems.Add(_recentItem);
        queue.DropDownItems.Add(new ToolStripSeparator());
        queue.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("&Clear queue", null, (_, _) => ClearQueue()), CommandMark.Change));
        queue.DropDownItems.Add(new ToolStripSeparator());
        // AM-1.4: the ways out, on the file playing or the one under the selection; the File Explorer item is the
        // first of the submenu the base makes, so its command is made here and its key added to the list.
        var explorer = new Command("File &Explorer", AxMediaKeys.ShowInExplorer, ShowInExplorer) { Mark = CommandMark.Open };
        queue.DropDownItems.Add(CreateShowInMenu("&Show current file in", Add(explorer), CurrentFile, FileMissing));
        queue.DropDownItems.Add(new ToolStripSeparator());
        queue.DropDownItems.Add(CreateExitItem());

        // Actions (AX-9.1): the player, the same wherever the focus is. Ctrl+Enter, the bundle's main action key,
        // pauses and resumes from anywhere in the window (AM-D7 as of 2026-11-22, the maintainer: "space in axmedia
        // should select and ctrl+enter should become play/pause"; Space and Ctrl+P until then).
        var play = new ToolStripMenuItem();
        Add(play, new Command("Play, p&ause or resume", BundleKeys.MainAction, PlayPauseResume) { Mark = CommandMark.Open });
        Add(play, new Command($"&Forward {_settings.SeekSeconds} seconds", AxMediaKeys.Forward, () => Seek(_settings.SeekSeconds)) { Mark = CommandMark.Open });
        Add(play, new Command($"&Back {_settings.SeekSeconds} seconds", AxMediaKeys.Back, () => Seek(-_settings.SeekSeconds)) { Mark = CommandMark.Open });
        play.DropDownItems.Add(new ToolStripSeparator());
        Add(play, new Command("Fa&ster", AxMediaKeys.Faster, () => ChangeSpeed(MediaFiles.Faster(_settings.Speed))) { Mark = CommandMark.Change });
        Add(play, new Command("S&lower", AxMediaKeys.Slower, () => ChangeSpeed(MediaFiles.Slower(_settings.Speed))) { Mark = CommandMark.Change });
        play.DropDownItems.Add(CommandPalette.Marked(new ToolStripMenuItem("N&ormal speed", null, (_, _) => ChangeSpeed(1)), CommandMark.Change));
        play.DropDownItems.Add(new ToolStripSeparator());
        Add(play, new Command("&What is playing", AxMediaKeys.WhatIsPlaying, SayWhatIsPlaying) { Mark = CommandMark.Read });
        play.DropDownItems.Add(new ToolStripSeparator());
        // The bundle's selection (AX-8.16, AM-3.5): every file of the queue, or none; the row's own two are in Context.
        play.DropDownItems.Add(Item("S&elect all", SelectAllRows, SelectionKeys.SelectAll, CommandMark.Change));
        play.DropDownItems.Add(Item("&Deselect all", SelectNoRows, SelectionKeys.SelectNone, CommandMark.Change));

        // Navigation (AX-9.1): along the queue and in the file.
        var navigation = new ToolStripMenuItem();
        Add(navigation, new Command("&Next file", AxMediaKeys.NextFile, () => MoveInQueue(1)) { Mark = CommandMark.Open });
        Add(navigation, new Command("Pre&vious file", AxMediaKeys.PreviousFile, () => MoveInQueue(-1)) { Mark = CommandMark.Open });
        Add(navigation, new Command("&Go to time...", AxMediaKeys.GoToTime, GoToTime) { Mark = CommandMark.Open });

        // Context (AX-9.11): what acts on the file you are on in the queue. Enter, Delete and the selection keys are
        // the navigator's (the item window's commands, which act in the list only); Remove takes the selected files
        // out when there are any (AM-3.5). Without a file, Context has the one item "Queue is empty".
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(Item("&Play", PlaySelected, ItemKeys.Open, CommandMark.Open));
        file.DropDownItems.Add(Item("R&emove", () => WithRow(DeleteRow), ItemKeys.Delete, CommandMark.Change));
        Add(file, new Command("&Information", AxMediaKeys.Information, SayInformation) { Mark = CommandMark.Read });
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("&Select or deselect", SelectRow, SelectionKeys.Select, CommandMark.Change));
        file.DropDownItems.Add(Item("Select to &here", SelectToHere, SelectionKeys.SelectTo, CommandMark.Change));

        // Options, the app's own first (AX-9.1): Say each file as it starts, the play menu's until 2026-11-11.
        var options = new ToolStripMenuItem();
        options.DropDownItems.Add(Toggle("Say &each file as it starts", _settings.SayEachFile, v => _settings.SayEachFile = v));
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "&Media resources",
        [
            ("&Formats Windows plays, at Microsoft", "https://learn.microsoft.com/windows/win32/medfound/supported-media-formats-in-media-foundation"),
            ("Free &codecs in the Microsoft Store", "https://apps.microsoft.com/search?query=media+extensions"),
        ]);

        // The bundle's menus (AX-9.1), Window, axKit and Help, end the bar in every window.
        PlaceMenuBar(_menu, queue, play, navigation, CreateContextMenu(_ => file, file), options, help);

        // ---- the list (AM-2.1): the navigator, the one control; its own keys stay native ----
        Navigator.DoubleClick += (_, _) => PlaySelected();

        // ---- the video area (AM-2.4): a plain surface at the top, shown while a video plays, never focused ----
        _video.Dock = DockStyle.Top;
        _video.Height = 0;
        _video.Visible = false;
        // The one colour an app sets (AX-7.7, AM-2.4): a video surface is black everywhere, so that a picture's
        // letterbox is not the window's colour (decided 2026-10-23).
        _video.BackColor = Color.Black;
        ThemePainter.Mark(_video, ThemePainter.Fixed);
        _video.TabStop = false;
        _video.AccessibleName = "Video";
        _video.AccessibleRole = AccessibleRole.Graphic;
        _video.Resize += (_, _) => _player?.UpdateVideo();
        _video.Paint += (_, _) => _player?.UpdateVideo();

        // ---- the status bar (AM-2.2): the item window's pair; the first part is what plays (StatusState) ----
        _clock.Tick += (_, _) => UpdateStatusBar();
        SizeChanged += (_, _) => FitVideo();

        // Docked from the last control to the first: the menu takes the top, the status bar the bottom, the video
        // area the strip under the menu while it shows, and the list (or the notice over it) what is left.
        PlaceWith(_menu, _video);
        Fill(select: null);
    }

    // ---- what the navigator asks for (the item window, docs/axKit/framework.md FW-4.9) ----

    /// <summary>The queue is the one level and its files are all there is to a row: no second pane.</summary>
    protected override SecondPane Pane => SecondPane.None;

    /// <summary>Space selects a file (AM-3.5), and Delete then takes the selected files out.</summary>
    protected override bool HasSelection => true;

    protected override string RowNoun => "file";

    /// <summary>The empty queue's one line, which is no file.</summary>
    protected override string? EmptyLine => PlayQueue.EmptyText;

    /// <summary>One level for ever: the selection is kept while the queue is, and given up by a play that replaces it.</summary>
    protected override string LevelIdentity() => "queue";

    /// <summary>The selection keys on the empty queue's line: that line.</summary>
    protected override void NothingToSelect(ItemRow? row) => Announce(PlayQueue.EmptyText, true);

    /// <summary>
    /// The queue as the navigator's level (AM-2.1): a row for each file, by its name, the one playing marked; the
    /// list named with its count; the title and the status bar's second part as <see cref="UpdateTitle"/> and
    /// <see cref="UpdateStatusBar"/> have them.
    /// </summary>
    protected override (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel() =>
        (QueueRows(), _queue.ListName, TitleNow() ?? string.Empty, PlaceNow());

    /// <summary>
    /// The queue's files as rows. A file queued twice is two rows, so each has a key of its own, its path and which
    /// of its occurrences it is; the files still there keep their marks (AM-3.5). A row carries its file's place
    /// in the queue (<see cref="QueueIndex"/>): under the item window's filter the list shows fewer rows than the
    /// queue has, and a row's place in the list is not its file's place in the queue.
    /// </summary>
    private List<ItemRow> QueueRows()
    {
        var rows = new List<ItemRow>(_queue.Count);
        if (_queue.Count == 0)
        {
            return rows;
        }

        var lines = _queue.Lines(PlayerActive);
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < _queue.Count; i++)
        {
            var path = _queue.Paths[i];
            seen.TryGetValue(path, out var before);
            seen[path] = before + 1;
            rows.Add(new ItemRow(IsList: false, $"{path}\n{before}", Path.GetFileName(path), lines[i], Tag: i));
        }

        return rows;
    }

    /// <summary>The place in the queue of the file whose row you are on; -1 on the line of an empty queue or of a filter nothing passes.</summary>
    private int QueueIndex() => SelectedRow()?.Tag is int index ? index : -1;

    // ---- Options → Sort by (AM-3.6): the item window's menu and words, and a sort that reorders the queue ----
    // The queue is the order the files play in, so a list shown in another order than it plays in would mislead
    // (the maintainer's choice, 2026-11-22: "Sort that reorders the queue"). Name puts the queue itself in the
    // order of the names, and the files then play in that order; The order added puts it back. The queue does the
    // ordering (PlayQueue.OrderBy), so the rows come in the chosen order. Kept for the window, not saved.

    protected override IReadOnlyList<string> SortNames => ["the order added", "name"];

    protected override bool OrdersRowsItself => true;

    protected override void SortChanged() => _queue.OrderBy(SortChoice == 1);

    /// <summary>Enter in the queue, the navigator's Open: the file plays.</summary>
    protected override void OpenItem(ItemRow row) => PlaySelected();

    /// <summary>Delete in the queue, the navigator's Delete: the file, or the selected files, taken out.</summary>
    protected override void DeleteRow(ItemRow row) => RemoveSelected();

    /// <summary>
    /// A queue has nothing to rename, to undo or to read again, and a window has the one queue: F2, Ctrl+Z, F5 and
    /// Ctrl+Shift+N are no commands here. The four methods below are what the item window asks every app for;
    /// none of them is reached through a key.
    /// </summary>
    protected override IReadOnlyCollection<Keys> KeysWithoutUse => [ItemKeys.Rename, ItemKeys.Undo, ItemKeys.Refresh, ItemKeys.NewList];

    /// <summary>Ctrl+N is New window here, the app's own command; the navigator's New item is never registered.</summary>
    protected override void NewItem() => NewWindow();


    /// <summary>Ctrl+S, as before the window was a navigator.</summary>
    protected override void SaveCommand() => Refuse("Nothing to save here");

    // ---- what the window base asks for ----

    /// <summary>A window opened by an add (axFile's Ctrl+Shift+Q with no axMedia open) stays behind: the user asked to add, not to come here (AM-D3).</summary>
    protected override bool ShowWithoutActivation => _start is { Verb: AppMessage.Add };

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override void HideForNotice()
    {
        base.HideForNotice();
        _video.Visible = false;
    }

    protected override void ShowAfterNotice()
    {
        base.ShowAfterNotice();
        FitVideo();
    }

    protected override string UpdateRestartText => $"closes {AxMediaApp.Name}, installs it and starts it again on the file playing";

    protected override IEnumerable<string> DiagnosticsLines =>
    [
        $"Queue: {_queue.Count} files, playing {_queue.PlayingName ?? "(none)"}",
        $"Player: {_player?.State.ToString() ?? "not started"}, speed {Speed}",
    ];

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_start is { } start)
        {
            Apply(start);
        }
    }

    /// <summary>How many files the add that opened this window brought, until it has been said (AM-D3); 0 otherwise.</summary>
    private int _addedAtStart;

    /// <summary>Whether this window is the active one, as Windows last told it (the activation and its end).</summary>
    private bool _active;

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        _active = true;
        SayAddedAtStart();
    }

    protected override void OnDeactivate(EventArgs e)
    {
        _active = false;
        base.OnDeactivate(e);
    }

    /// <summary>"Added 1", once, for the add that opened this window, when the window is the one in front (AM-D3).</summary>
    private void SayAddedAtStart()
    {
        if (_addedAtStart == 0 || !_active)
        {
            return;
        }

        var added = _addedAtStart;
        _addedAtStart = 0;
        // Posted, so that it follows what NVDA reads of the window as the focus arrives.
        BeginInvoke(() => Announce($"Added {added}", true));
    }

    /// <summary>The file in hand, for Recently closed (AX-9.4); chosen there, it plays with its folder again.</summary>
    protected override string? PathInHand => FileInHand;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // The base first, while the file playing is still the one in hand; AM-4.4: closing the window stops the player.
        base.OnFormClosing(e);
        StopPlayer();
    }

    /// <summary>AM-1.2, AM-1.3: a start with paths while this window is open hands them here; the base answers the message at once and the paths are applied after, nothing read before the answer.</summary>
    protected override Action? TakeHandOver(HandOver message) => () => Apply(message);

    /// <summary>A hand-over (or the start) queued and applied after the ones before it, in the order they came (one queue drained in turn since 2026-10-23; a chain of tasks before); one that fails is its own notice, and the next applies.</summary>
    private void Apply(HandOver message)
    {
        _handOvers.Enqueue(message);
        if (!_applying)
        {
            Do(DrainAsync);
        }
    }

    private async Task DrainAsync()
    {
        _applying = true;
        try
        {
            while (_handOvers.TryDequeue(out var message))
            {
                await ApplyAsync(message);
            }
        }
        finally
        {
            _applying = false;
            if (_handOvers.Count > 0)
            {
                // The one applying failed, which the Do reports; the rest go on.
                Do(DrainAsync);
            }
        }
    }

    // ---- the queue (AM-3) ----

    /// <summary>
    /// What a start or a hand-over asks (AM-1.1, AM-3.2): a play replaces the queue (one file with its folder's media
    /// files around it, a folder's files, several paths expanded), plays, and brings the window to the front, the
    /// starting process having let it (AppMessage); an add appends. An add to a window that is open says nothing
    /// here, since axFile, where the user is, says it (AM-D3). An add that opens the window is said here, "Added
    /// 1", when the user arrives in it (<see cref="OnActivated"/>): Windows may bring a new window to the front
    /// though it is shown without activation, NVDA then reads that window, and what axFile said is lost (the
    /// maintainer, 2026-11-22: "when there is only one item added into axMedia it is not announced"). A single
    /// path is remembered for Open recent.
    /// </summary>
    private async Task ApplyAsync(HandOver message)
    {
        if (message.Verb == AppMessage.Add)
        {
            var paths = await Task.Run(() => MediaFolder.Expand(message.Paths));
            var added = _queue.Add(paths);
            FillList();
            RememberPath(message.Paths);
            if (ReferenceEquals(message, _start) && added > 0)
            {
                // Said once the window is the one in front; at once when it is already.
                _addedAtStart = added;
                SayAddedAtStart();
            }

            return;
        }

        var (queue, start) = await Task.Run(() => MediaFolder.Queue(message.Paths));
        if (queue.Count == 0)
        {
            WindowMenu.Activate(Handle);
            Refuse($"No sound or video files in {Describe(message.Paths)}");
            return;
        }

        _queue.Set(queue, start);
        // Another queue: nothing selected (AM-3.5), as another folder in axFile.
        Selection.Clear();
        FillList(select: start);
        PlayCurrent(prompted: true);
        WindowMenu.Activate(Handle);
        RememberPath(message.Paths);
    }

    /// <summary>One path handed over, remembered for Open recent (AM-3.2); several are not, since the list would not say what they were.</summary>
    private void RememberPath(IReadOnlyList<string> paths)
    {
        if (paths.Count == 1)
        {
            _settings.Remember(paths[0]);
            SaveSettings();
        }
    }

    private static string Describe(IReadOnlyList<string> paths) => paths.Count == 1 ? PathName.Of(paths[0]) : $"{paths.Count} paths";

    /// <summary>
    /// The list's lines made again from the queue (AM-2.1): each line changed in place while the same files are
    /// there, so that the row you are on stays and nothing but the changed line moves under NVDA (the item window's
    /// Refill); the whole list otherwise, on the row of the same place, or on <paramref name="select"/> (a play
    /// that replaces the queue). The list is named with its count.
    /// </summary>
    private void FillList(int? select = null)
    {
        var rows = QueueRows();
        var keys = rows.Select(row => row.Key).ToList();
        var same = keys.SequenceEqual(_filledKeys, StringComparer.OrdinalIgnoreCase);
        // The place in the queue of the row you are on, read before the rows are made again.
        var at = Math.Clamp(select ?? QueueIndex(), 0, Math.Max(rows.Count - 1, 0));
        _filledKeys = keys;
        if (select is null && same)
        {
            Refill();
            return;
        }

        Fill(rows.Count == 0 ? null : rows[at].Key);
    }

    /// <summary>The queue's files, by their rows' keys, as the list was last made from them: the same files again change their lines in place.</summary>
    private List<string> _filledKeys = [];

    private bool PlayerActive => _player is { State: PlayerState.Playing or PlayerState.Paused or PlayerState.Opening };

    /// <summary>The speed shown: the player's rate once it exists (a file that refuses the rate plays at 1, AM-4.2), the setting before.</summary>
    private double Speed => _player?.Rate ?? _settings.Speed;

    /// <summary>The paths appended (Add files, Add folder, a recent item): "Added n", or why nothing was; each path remembered.</summary>
    private void AddPaths(IReadOnlyList<string> paths, string? noneText)
    {
        foreach (var path in paths)
        {
            _settings.Remember(path);
        }

        SaveSettings();
        Do(async () =>
        {
            var expanded = await Task.Run(() => MediaFolder.Expand(paths));
            if (expanded.Count == 0)
            {
                Announce(noneText ?? "No sound or video files to add", true);
                return;
            }

            _queue.Add(expanded);
            FillList();
            Announce($"Added {expanded.Count}", true);
        });
    }

    /// <summary>Queue → Add files… (Ctrl+O): the Windows file picker on the media kinds, several at once, appended in the picker's order.</summary>
    private void OpenFiles()
    {
        var extensions = string.Join(";", MediaFiles.Extensions.Order().Select(extension => "*" + extension));
        using var dialog = new OpenFileDialog
        {
            Title = "Choose sound or video files",
            Filter = $"Sound and video files ({extensions})|{extensions}|All files (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true,
        };
        if (_queue.PlayingPath is { } playing && Path.GetDirectoryName(playing) is { } folder)
        {
            dialog.InitialDirectory = folder;
        }

        var chosen = ShowSystemDialog(dialog) == DialogResult.OK ? dialog.FileNames : [];
        if (chosen.Length > 0)
        {
            AddPaths(chosen, null);
        }
    }

    /// <summary>Queue → Add folder… (Ctrl+Shift+O): the bundle's folder picker; the folder's media files appended in name order.</summary>
    private void OpenFolder()
    {
        var chosen = ChooseFolder("Choose the folder whose sound and video files to add");
        if (chosen is not null)
        {
            AddPaths([chosen], $"No sound or video files in {PathName.Of(chosen)}");
        }
    }

    /// <summary>Queue → Open recent (AM-3.2): the last ten files and folders added or handed over, those that still exist, "1. name (path)" (AX-9.5); one chosen is appended again.</summary>
    private void FillRecent() =>
        RecentMenu.Fill(_recentItem, _settings.Recent, path => File.Exists(path) || Directory.Exists(path), PathName.Of, "&No recent files or folders", CommandMark.Open,
            target => AddPaths([target], Directory.Exists(target) ? $"No sound or video files in {PathName.Of(target)}" : null));

    /// <summary>Queue → New window (Ctrl+N): another axMedia window, empty; the Window menu's New window does the same (AX-9).</summary>
    private void NewWindow() => WindowMenu.Start(Dispatch.AxMedia, null);

    /// <summary>The file in hand (AM-1.4): the one playing or paused, else the one under the selection; null with an empty queue.</summary>
    private string? FileInHand =>
        PlayerActive && _queue.PlayingPath is { } playing ? playing
        : _queue.IsFile(QueueIndex()) ? _queue.Paths[QueueIndex()]
        : null;

    /// <summary>The file in hand for a command; null, and said, when the queue is empty.</summary>
    private string? CurrentFile()
    {
        if (FileInHand is { } file)
        {
            return file;
        }

        Announce(PlayQueue.EmptyText, true);
        return null;
    }

    /// <summary>Queue → Show current file in → File Explorer (Ctrl+W): the file selected in File Explorer, one of the system's own windows.</summary>
    private void ShowInExplorer()
    {
        if (CurrentFile() is { } file)
        {
            OpenInExplorer(file, missing: FileMissing);
        }
    }

    /// <summary>Enter in the list, Queue → Play: the file under the selection plays, "Playing <name>"; the empty line is read again.</summary>
    private void PlaySelected()
    {
        if (!_queue.Select(QueueIndex()))
        {
            Announce(PlayQueue.EmptyText, true);
            return;
        }

        PlayCurrent(prompted: true);
    }

    /// <summary>Delete in the list, Queue → Remove: the file taken out, "Removed <name>"; the one playing goes on to the next, or stops at the end.</summary>
    private void RemoveSelected()
    {
        if (Selection.Count > 0)
        {
            // AM-3.5: with files selected, Delete takes them all out, whatever row you are on: "Removed 3 files".
            // From the last to the first, so that each index still names its file; the one playing goes on to the
            // next file left, or stops at the end.
            var rows = SelectedRows().Select(row => (int)row.Tag!).OrderByDescending(row => row).ToList();
            var playingGoes = PlayerActive && rows.Contains(_queue.Playing);
            foreach (var row in rows)
            {
                _queue.Remove(row);
            }

            Selection.Clear();
            if (playingGoes)
            {
                if (_queue.PlayingPath is not null)
                {
                    PlayCurrent(prompted: false);
                }
                else
                {
                    StopPlayback();
                }
            }

            FillList();
            Announce(rows.Count == 1 ? "Removed 1 file" : $"Removed {rows.Count} files", true);
            return;
        }

        var index = QueueIndex();
        var wasPlaying = _queue.IsFile(index) && index == _queue.Playing && PlayerActive;
        if (_queue.Remove(index) is not { } removed)
        {
            Announce(PlayQueue.EmptyText, true);
            return;
        }

        if (wasPlaying)
        {
            if (_queue.PlayingPath is not null)
            {
                PlayCurrent(prompted: false);
            }
            else
            {
                StopPlayback();
            }
        }

        FillList();
        Announce($"Removed {removed}", true);
    }

    /// <summary>Queue → Clear queue: the files gone and the player stopped; "Queue cleared".</summary>
    private void ClearQueue()
    {
        _queue.Clear();
        StopPlayback();
        FillList();
        Announce("Queue cleared", true);
    }

    /// <summary>Ctrl+I, Queue → Information: the file's name and place, "03 Chapter 3.mp3, 3 of 12", with the position while it is the one playing; the empty line is read again.</summary>
    private void SayInformation()
    {
        var index = QueueIndex();
        if (!_queue.IsFile(index))
        {
            Announce(PlayQueue.EmptyText, true);
            return;
        }

        var text = _queue.Information(index);
        if (index == _queue.Playing && _player is { State: PlayerState.Playing or PlayerState.Paused } player)
        {
            text += $", {(player.State == PlayerState.Playing ? "playing" : "paused")}, {MediaFiles.FormatTime(player.Position)} of {MediaFiles.FormatTime(player.Duration)}";
        }

        if (Selection.Count > 0)
        {
            text += $", {Selection.Count} selected";
        }

        Announce(text, true);
    }
}
