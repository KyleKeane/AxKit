using System.ComponentModel;
using System.Diagnostics;
using AxKit.Core;
using AxKit.Forms;
using AxProcess.Core;

namespace AxProcess;

/// <summary>
/// axProcess's window (SPEC.md PR-2): a menu bar, one list and a status bar. The window is a navigator, the bundle's
/// item window (<see cref="ItemForm"/>; docs/axKit/framework.md FW-4.6, step 7, 2026-11-22; a window of its own with
/// three views of one list until then): the top level, "This computer", holds three lists, Processes, Services and
/// Startup, and Right or Enter goes into one, Left or Backspace back. The window starts inside Processes. It has no
/// second pane: Enter on a row shows its details as a notice with what can be done to it. Every read runs on a
/// background task (AX-7.5) and nothing refreshes by itself (PR-D2). The list, its selection, find, the Context
/// menu and a navigator's keys are the item window's; this class holds the three reads and the commands on their
/// rows.
/// </summary>
public sealed class ProcessForm : ItemForm
{
    /// <summary>The three lists of the top level (PR-2.2); their names are the rows' keys there.</summary>
    private enum View
    {
        Processes,
        Services,
        Startup,
    }

    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);

    private readonly ProcessSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _listMenu = new("&List");
    private readonly ToolStripMenuItem _processMenu = new("&Process");
    private readonly ToolStripMenuItem _serviceMenu = new("S&ervice");
    private readonly ToolStripMenuItem _startupMenu = new("S&tartup");

    /// <summary>The level shown: one of the three lists, or null at the top, "This computer".</summary>
    private View? _level = View.Processes;

    /// <summary>Which of the three lists has been read.</summary>
    private readonly bool[] _loaded = new bool[3];

    /// <summary>The row each list was left on, by its key, to come back to (PR-2.2).</summary>
    private readonly string?[] _kept = new string?[3];

    private IReadOnlyList<ProcessRow> _processes = [];
    private IReadOnlyList<ServiceRow> _services = [];
    private IReadOnlyList<StartupEntry> _startup = [];
    private double? _processor;

    private bool _announcedFirstRead;

    public ProcessForm(ProcessSettings settings, string? settingsError)
        : base(AxProcessApp.Name, settings, ProcessSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        SetTitle("This computer");

        // ---- menus and keys (PR-6.1, PR-6.2): one command list (docs/axKit/app-kit.md AK-2), each key from the tables ----
        // The first menu holds what every level has (AX-6: the thing worked on, then Exit); each kind of row has a
        // menu of its own, off the bar, and Context holds the one of the row you are on, so that a process command
        // is never at hand among the services (the user's ask, 2026-10-24; AX-9.11, 2026-11-11). Enter, Delete, the
        // selection keys, find and the Context menu's keys are the navigator's (the item window's commands,
        // registered after these by PlaceControls); the menu items for them here only show the keys.
        var computer = new ToolStripMenuItem("&Session");
        Add(computer, new Command("&Refresh", AxProcessKeys.Refresh, () => Do(() => RefreshAsync(announce: true))) { Mark = CommandMark.Read });
        computer.DropDownItems.Add(new ToolStripSeparator());
        computer.DropDownItems.Add(CreateExitItem());

        // Actions: what is about the computer as a whole (AX-9.1), and the selection's two that act on every row.
        var actions = new ToolStripMenuItem();
        Add(actions, new Command("&Summary", AxProcessKeys.Summary, SaySummary) { Mark = CommandMark.Read });
        actions.DropDownItems.Add(new ToolStripSeparator());
        actions.DropDownItems.Add(Item("Select &all", SelectAllRows, SelectionKeys.SelectAll, CommandMark.Change));
        actions.DropDownItems.Add(Item("D&eselect all", SelectNoRows, SelectionKeys.SelectNone, CommandMark.Change));

        // Copy line and Show program are in more than one menu: one command each, placed in every menu that has it.
        var copyLine = new Command("&Copy line", AxProcessKeys.CopyLine, CopyLine) { Mark = CommandMark.Act };
        var showProgram = new Command("Show program in ax&File", AxProcessKeys.ShowProgram, ShowProgram) { Mark = CommandMark.Open };

        _listMenu.DropDownItems.Add(Item("&Open", OpenSelected, ItemKeys.Open, CommandMark.Open));
        Add(_listMenu, copyLine);

        // Delete is the navigator's key and, by the kind of row, two things (PR-6.2): End process on a process,
        // Remove on a startup program; with rows selected, all of them, after one question (PR-3.3a, PR-5.6a).
        _processMenu.DropDownItems.Add(Item("&Details", OpenSelected, ItemKeys.Open, CommandMark.Read));
        _processMenu.DropDownItems.Add(Item("&End process...", () => WithRow(DeleteRow), ItemKeys.Delete, CommandMark.Act));
        Add(_processMenu, new Command("End process &tree...", Keys.None, () => EndProcess(tree: true)) { Mark = CommandMark.Act });
        Add(_processMenu, showProgram);
        Add(_processMenu, copyLine);
        AddSelectionItems(_processMenu);

        _serviceMenu.DropDownItems.Add(Item("&Details", OpenSelected, ItemKeys.Open, CommandMark.Read));
        Add(_serviceMenu, new Command("&Start", Keys.None, () => ControlSelectedService(ServiceAction.Start)) { Mark = CommandMark.Act });
        Add(_serviceMenu, new Command("S&top", Keys.None, () => ControlSelectedService(ServiceAction.Stop)) { Mark = CommandMark.Act });
        Add(_serviceMenu, new Command("&Restart", Keys.None, () => ControlSelectedService(ServiceAction.Restart)) { Mark = CommandMark.Act });
        Add(_serviceMenu, copyLine);

        // Turn off, Turn on and Remove change what starts with Windows (PR-5.4 to PR-5.6): act, so an AI tool asks first.
        _startupMenu.DropDownItems.Add(Item("&Details", OpenSelected, ItemKeys.Open, CommandMark.Read));
        Add(_startupMenu, new Command("Turn &off", Keys.None, () => ChangeSelectedStartup(StartupAction.TurnOff)) { Mark = CommandMark.Act });
        Add(_startupMenu, new Command("Turn o&n", Keys.None, () => ChangeSelectedStartup(StartupAction.TurnOn)) { Mark = CommandMark.Act });
        _startupMenu.DropDownItems.Add(Item("&Remove...", () => WithRow(DeleteRow), ItemKeys.Delete, CommandMark.Act));
        Add(_startupMenu, showProgram);
        Add(_startupMenu, copyLine);
        AddSelectionItems(_startupMenu);

        // Navigation: the three lists, each a jump from anywhere, and the way up (AX-9.1).
        var view = new ToolStripMenuItem();
        // On Alt+1, 2 and 3, each with its older Ctrl key as a further key of the same command (bundle AX-8.14).
        Place(new Command("&Processes", AxProcessKeys.ViewProcesses, () => Do(() => ShowListAsync(View.Processes))) { Mark = CommandMark.Open }, AxProcessKeys.ViewProcessesAlso);
        Place(new Command("&Services", AxProcessKeys.ViewServices, () => Do(() => ShowListAsync(View.Services))) { Mark = CommandMark.Open }, AxProcessKeys.ViewServicesAlso);
        Place(new Command("Start&up", AxProcessKeys.ViewStartup, () => Do(() => ShowListAsync(View.Startup))) { Mark = CommandMark.Open }, AxProcessKeys.ViewStartupAlso);
        view.DropDownItems.Add(new ToolStripSeparator());
        view.DropDownItems.Add(Item("Up to This &computer", UpLevel, ItemKeys.Up, CommandMark.Open));
        void Place(Command command, Keys also)
        {
            Add(view, command);
            AddKey(command with { Key = also });
        }

        // Options begins with Filter and Sort by, the item window's as in every navigator (PR-3.2: the orders of the
        // processes are this app's; "Sort processes by" was a menu item of its own until 2026-11-22).
        var options = new ToolStripMenuItem();
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Windows &resources",
        [
            ("&Task Manager", "https://learn.microsoft.com/en-us/windows/win32/taskman/task-manager"),
            ("&Services", "https://learn.microsoft.com/en-us/windows/win32/services/services"),
        ]);

        // The bundle's menus (AX-9.1), Window, axKit and Help, end the bar in every window.
        PlaceMenuBar(_menu, computer, actions, view, CreateContextMenu(MenuOf, _listMenu, _processMenu, _serviceMenu, _startupMenu), options, help);

        PlaceControls(_menu);
        Fill(select: null);
    }

    private enum ServiceAction
    {
        Start,
        Stop,
        Restart,
    }

    /// <summary>The selection's two items of a row's menu; the keys are the item window's commands.</summary>
    private void AddSelectionItems(ToolStripMenuItem menu)
    {
        menu.DropDownItems.Add(new ToolStripSeparator());
        menu.DropDownItems.Add(Item("Se&lect or deselect", SelectRow, SelectionKeys.Select, CommandMark.Change));
        menu.DropDownItems.Add(Item("Select to &here", SelectToHere, SelectionKeys.SelectTo, CommandMark.Change));
    }

    // ---- what the navigator asks for (the item window, docs/axKit/framework.md FW-4.6) ----

    /// <summary>A row has no more to show than its line; its details are a notice, on Enter.</summary>
    protected override SecondPane Pane => SecondPane.None;

    /// <summary>Space selects processes and startup programs (AX-8.16): Delete then ends or removes all the selected.</summary>
    protected override bool HasSelection => true;

    /// <summary>What Delete acts on can be selected: a process, a startup program. A list and a service cannot, since nothing here acts on several of them.</summary>
    protected override bool CanSelectRow(ItemRow row) => row.Tag is ProcessRow or StartupEntry;

    protected override void NothingToSelect(ItemRow? row)
    {
        if (row?.Tag is ServiceRow)
        {
            Announce("Services are not selected: nothing here acts on several of them", true);
        }
        else
        {
            base.NothingToSelect(row);
        }
    }

    /// <summary>The level by its own name, not by the status bar's second part, which is the memory in use and changes.</summary>
    protected override string LevelIdentity() => _level?.ToString() ?? "This computer";

    /// <summary>
    /// A navigator's keys with no use here: nothing is renamed, undone or made new. F2, Ctrl+Z, Ctrl+N and
    /// Ctrl+Shift+N are no commands of the window; the four methods below are what the item window asks every app
    /// for, and none is reached through a key.
    /// </summary>
    protected override IReadOnlyCollection<Keys> KeysWithoutUse => [ItemKeys.Rename, ItemKeys.Undo, ItemKeys.New, ItemKeys.NewList];

    /// <summary>
    /// The level shown: at the top the three lists, each with its count once it has been read; in a list its rows
    /// as last read. The list is named with its count, "Processes, 214", or "Processes, reading" before its first
    /// read; the title is the computer; the status bar's second part is the memory in use.
    /// </summary>
    protected override (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel() =>
        (LevelRows(), LevelName(), "This computer", MemoryText());

    private IReadOnlyList<ItemRow> LevelRows() => _level switch
    {
        View.Processes => [.. _processes.Select(row => new ItemRow(IsList: false, row.Id.ToString(), row.Name, row.Label, row))],
        View.Services => [.. _services.Select(row => new ItemRow(IsList: false, row.Name, row.DisplayName, row.Label, row))],
        View.Startup => [.. _startup.Select(entry => new ItemRow(IsList: false, entry.Key, entry.Name, entry.Label, entry))],
        _ => [TopRow(View.Processes), TopRow(View.Services), TopRow(View.Startup)],
    };

    /// <summary>A list as a row of the top level: "Processes, 214" once read, "Processes" before.</summary>
    private ItemRow TopRow(View view) => new(IsList: true, view.ToString(), view.ToString(), _loaded[(int)view] ? ListName(view) : view.ToString());

    private string LevelName() =>
        _level is not { } view ? "This computer, 3 lists"
        : _loaded[(int)view] ? ListName(view)
        : $"{view}, reading";

    private string ListName(View view) => view switch
    {
        View.Processes => ProcessTable.ListName(_processes.Count),
        View.Services => ServiceRow.ListName(_services.Count),
        _ => StartupEntry.ListName(_startup.Count),
    };

    /// <summary>
    /// Right and Enter on a list of the top level: the list is read off the UI thread on its first visit, then
    /// shown and said once (<see cref="ShowListAsync"/>). The item window's own way in fills at once, so it is
    /// turned down with an empty reason, which says nothing: the app does the going in itself.
    /// </summary>
    protected override string? EntryRefused(ItemRow row)
    {
        if (Enum.TryParse<View>(row.Key, out var view))
        {
            Do(() => ShowListAsync(view));
        }

        return string.Empty;
    }

    /// <summary>Left and Backspace in a list: up to This computer, on the list left; at the top nothing, the edge sound.</summary>
    protected override string? LeaveList()
    {
        if (_level is not { } view)
        {
            return null;
        }

        _kept[(int)view] = SelectedKey();
        _level = null;
        return view.ToString();
    }

    /// <summary>Enter on a row of a list, the navigator's Open: its details, with what can be done to it.</summary>
    protected override void OpenItem(ItemRow row) => Do(ActAsync);

    /// <summary>
    /// Delete, the navigator's key, by the kind of row (PR-6.2): a process is ended, a startup program removed,
    /// each after a question, and with rows selected all of them after one question; a service is not ended, and
    /// a list of the top level is no thing to end.
    /// </summary>
    protected override void DeleteRow(ItemRow row)
    {
        var selected = SelectedRows();
        switch (row.Tag)
        {
            case ProcessRow when selected.Count > 1:
                EndProcesses([.. selected.Select(one => one.Tag).OfType<ProcessRow>()]);
                return;
            case ProcessRow:
                EndProcess(tree: false);
                return;
            case StartupEntry when selected.Count > 1:
                RemoveStartups([.. selected.Select(one => one.Tag).OfType<StartupEntry>()]);
                return;
            case StartupEntry:
                ChangeSelectedStartup(StartupAction.Remove);
                return;
            case ServiceRow:
                Announce("A service is not ended: Enter shows it, with Stop where it can be stopped", true);
                return;
            default:
                Announce("A list: Right goes into it", true);
                return;
        }
    }

    /// <summary>The menu of the row you are on, whose items Context holds (PR-6.1, AX-9.11).</summary>
    private ToolStripMenuItem? MenuOf(ItemRow row) => row.Tag switch
    {
        ProcessRow => _processMenu,
        ServiceRow => _serviceMenu,
        StartupEntry => _startupMenu,
        _ => _listMenu,
    };

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    /// <summary>
    /// What an AI tool's act command would act on (docs/axKit/mcp.md MC-7.3): the process with its ID, the service or
    /// the startup program you are on, and the selection with it while there is one, so that the target changes when
    /// the selection does.
    /// </summary>
    protected override AxKit.Core.Mcp.RunTarget McpTarget()
    {
        var text = SelectedRow()?.Tag switch
        {
            ProcessRow process => $"the process {process.Name}, ID {process.Id}",
            ServiceRow service => $"the service {service.DisplayName}",
            StartupEntry startup => $"the startup program {startup.Name}",
            _ => string.Empty,
        };
        return text.Length == 0 ? AxKit.Core.Mcp.RunTarget.None
            : Selection.Count == 0 ? AxKit.Core.Mcp.RunTarget.Of(text)
            : new AxKit.Core.Mcp.RunTarget($"{text}, and {Selection.Count} selected", text + "\n" + string.Join("\n", Selection.Keys.Order(StringComparer.OrdinalIgnoreCase)));
    }

    protected override IEnumerable<string> DiagnosticsLines =>
        [$"Level: {LevelIdentity()}, {Rows.Count} rows, processes sorted by {ProcessTable.SortName(_settings.SortBy)}"];

    /// <summary>Ctrl+S, the bundle's save key: nothing to save, and the keys that act said (PR-6.2).</summary>
    protected override void SaveCommand() => Announce("Nothing to save here. F5 refreshes, Delete ends a process", true);

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Do(() => RefreshAsync(announce: false));
    }

    // ---- the levels and their reads (PR-2.2, PR-2.3) ----

    /// <summary>
    /// F5, the first showing and every command's end: the list shown read again on a background task and filled
    /// on the row it was on; at the top, all three lists, so that each row has its count.
    /// </summary>
    private async Task RefreshAsync(bool announce, string? select = null)
    {
        if (RefusedWhileWorking())
        {
            return;
        }

        var level = _level;
        foreach (var view in level is { } one ? new[] { one } : [View.Processes, View.Services, View.Startup])
        {
            if (!await LoadAsync(view))
            {
                return;
            }
        }

        // The row you are on now, which may have moved during the read; the one asked for when the level is still the same.
        Fill(_level == level && select is not null ? select : SelectedKey());
        if (announce)
        {
            Announce("Refreshed: " + Navigator.AccessibleName, true);
        }
        else if (!_announcedFirstRead)
        {
            // The list was empty when the window took the focus; its name and first row are said once, not interrupting.
            _announcedFirstRead = true;
            Announce(NameAndRow(), false);
        }
    }

    /// <summary>"Processes, 214. chrome, 350 MB, process 1234": the list's name, a full stop, the row you are on.</summary>
    private string NameAndRow()
    {
        var name = Navigator.AccessibleName ?? string.Empty;
        return Navigator.SelectedItem is string item ? $"{name}. {item}" : name;
    }

    /// <summary>Reads one list into its rows; false when another read runs.</summary>
    private async Task<bool> LoadAsync(View view)
    {
        if (RefusedWhileWorking())
        {
            return false;
        }

        BeginWork(ReadingState);
        UpdateStatusBar();
        try
        {
            switch (view)
            {
                case View.Processes:
                    var (rows, processor) = await Task.Run(ReadProcesses);
                    _processor = processor;
                    _processes = ProcessTable.Sorted(rows, _settings.SortBy);
                    break;
                case View.Services:
                    _services = await Task.Run(ServiceManager.List);
                    break;
                default:
                    _startup = await Task.Run(StartupList.Read);
                    break;
            }

            _loaded[(int)view] = true;
            return true;
        }
        finally
        {
            EndWork();
            if (!IsDisposed)
            {
                UpdateStatusBar();
            }
        }
    }

    /// <summary>Two samples a second apart (PR-D3), with the whole computer's busy share over the same second.</summary>
    private static (IReadOnlyList<ProcessRow> Rows, double? Processor) ReadProcesses()
    {
        var timesBefore = SystemInfo.Times();
        var first = ProcessTable.Sample();
        var watch = Stopwatch.StartNew();
        Thread.Sleep(SampleInterval);
        var second = ProcessTable.Sample();
        var timesAfter = SystemInfo.Times();
        return (ProcessTable.Rows(first, second, watch.Elapsed, Environment.ProcessorCount), SystemTimes.Busy(timesBefore, timesAfter));
    }

    /// <summary>
    /// Into a list: Right or Enter on its row at the top, or Alt+1, 2 and 3 from anywhere. Read on the first
    /// visit, then shown on the row it was left on, and its name said with that row. The level changes only once
    /// the read is in, so that the rows shown are always the level's.
    /// </summary>
    private async Task ShowListAsync(View view)
    {
        if (RefusedWhileWorking())
        {
            return;
        }

        if (!_loaded[(int)view] && !await LoadAsync(view))
        {
            return;
        }

        if (_level is { } from)
        {
            _kept[(int)from] = SelectedKey();
        }

        _level = view;
        Fill(_kept[(int)view]);
        Announce(NameAndRow(), true);
    }

    private ProcessRow? CurrentProcess() => SelectedRow()?.Tag as ProcessRow;

    private ServiceRow? CurrentService() => SelectedRow()?.Tag as ServiceRow;

    private StartupEntry? CurrentStartup() => SelectedRow()?.Tag as StartupEntry;

    // ---- Options → Sort by (PR-3.2): the item window's menu and words, the processes' three orders ----
    // The orders are of the processes: by memory, by processor time, by name. Services and startup programs are
    // listed by name whatever is chosen. The choice is kept in the settings, and the rows are put in the new
    // order without a new read.

    private static readonly ProcessSort[] Orders = [ProcessSort.Memory, ProcessSort.Processor, ProcessSort.Name];

    protected override IReadOnlyList<string> SortNames => [.. Orders.Select(ProcessTable.SortName)];

    protected override int SortChoice
    {
        get => Array.IndexOf(Orders, _settings.SortBy);
        set
        {
            _settings.SortBy = Orders[value];
            SaveSettings();
        }
    }

    protected override bool OrdersRowsItself => true;

    protected override void SortChanged()
    {
        if (_loaded[(int)View.Processes])
        {
            _processes = ProcessTable.Sorted(_processes, _settings.SortBy);
        }
    }

    // ---- the rows (PR-3, PR-4, PR-5) ----

    /// <summary>Enter, or the Details items: the notice of the row you are on, by its kind.</summary>
    private async Task ActAsync()
    {
        switch (SelectedRow()?.Tag)
        {
            case ProcessRow row:
                var lines = await Task.Run(() => SystemInfo.Details(row));
                ShowNotice(new Notice(row.Name, string.Join("\n", lines),
                [
                    new OverlayChoice("&End process...", () => EndProcess(row, tree: false)),
                    new OverlayChoice("Show program in ax&File", ShowProgram),
                    new OverlayChoice("&Close", IsDefault: true, IsCancel: true),
                ]));
                return;
            case ServiceRow service:
                ShowServiceDetails(service);
                return;
            case StartupEntry entry:
                ShowNotice(new Notice(entry.Name, string.Join("\n", entry.Details),
                [
                    entry.Enabled
                        ? new OverlayChoice("Turn &off", () => ChangeStartup(entry, StartupAction.TurnOff))
                        : new OverlayChoice("Turn o&n", () => ChangeStartup(entry, StartupAction.TurnOn)),
                    new OverlayChoice("&Remove...", () => ChangeStartup(entry, StartupAction.Remove)),
                    new OverlayChoice("Show program in ax&File", ShowProgram),
                    new OverlayChoice("&Close", IsDefault: true, IsCancel: true),
                ]));
                return;
            default:
                Announce("Nothing selected", true);
                return;
        }
    }

    /// <summary>Delete on a process, Context → End process tree (PR-3.3): the process you are on.</summary>
    private void EndProcess(bool tree)
    {
        if (CurrentProcess() is not { } row)
        {
            Announce("Nothing selected", true);
            return;
        }

        EndProcess(row, tree);
    }

    /// <summary>A question, then the end on a background task and a new read (PR-3.3).</summary>
    private void EndProcess(ProcessRow row, bool tree)
    {
        if (row.Id == Environment.ProcessId)
        {
            Announce("That is this window: close it with Alt+F4", true);
            return;
        }

        var title = tree ? $"End {row.Name} and the processes it started?" : $"End {row.Name} (process {row.Id})?";
        ShowNotice(Notice.Ask(title, "Anything unsaved in it is lost.", tree ? "End process &tree" : "&End process", () => Do(async () =>
        {
            var (_, said) = await Task.Run(() => Kill(row, tree));
            Announce(said, true);
            await RefreshAsync(announce: false);
        })));
    }

    /// <summary>
    /// Delete with several processes selected (PR-3.3a, AX-8.16): one question that names them, then each ended
    /// on a background task, one sentence for all, "Ended 3 processes", with what could not be ended after it;
    /// the selection given up and the list read again. This window is never among them.
    /// </summary>
    private void EndProcesses(IReadOnlyList<ProcessRow> rows)
    {
        if (rows.Any(row => row.Id == Environment.ProcessId))
        {
            Announce("This window is among the selected: deselect it first, or close it with Alt+F4", true);
            return;
        }

        var names = string.Join(", ", rows.Select(row => $"{row.Name} (process {row.Id})"));
        ShowNotice(Notice.Ask($"End {rows.Count} processes?", $"Anything unsaved in them is lost.\n{names}", "&End processes", () => Do(async () =>
        {
            var results = await Task.Run(() => rows.Select(row => Kill(row, tree: false)).ToList());
            var ended = results.Count(result => result.Ended);
            var failed = results.Where(result => !result.Ended).Select(result => result.Said).ToList();
            Announce(failed.Count == 0 ? $"Ended {ended} processes" : $"Ended {ended} of {rows.Count} processes. {string.Join(". ", failed)}", true);
            Selection.Clear();
            await RefreshAsync(announce: false);
        })));
    }

    /// <summary>The end itself, off the UI thread: whether it ended, and what to say about it. The ID must still be the same program's, since Windows reuses IDs.</summary>
    private static (bool Ended, string Said) Kill(ProcessRow row, bool tree)
    {
        try
        {
            using var process = Process.GetProcessById(row.Id);
            if (!process.ProcessName.Equals(row.Name, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"{row.Name} has already ended");
            }

            process.Kill(entireProcessTree: tree);
            process.WaitForExit(5000);
            Log.Info($"Ended {row.Name} ({row.Id}){(tree ? " with its tree" : string.Empty)}");
            return (true, $"Ended {row.Name}");
        }
        catch (ArgumentException)
        {
            return (false, $"{row.Name} has already ended");
        }
        catch (InvalidOperationException)
        {
            return (false, $"{row.Name} has already ended");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
        {
            return (false, $"Could not end {row.Name}: access is denied. It may need administrator rights");
        }
        catch (Win32Exception ex)
        {
            return (false, $"Could not end {row.Name}: {ex.Message}");
        }
    }

    /// <summary>Ctrl+E (PR-3.5, PR-5.3): the program file in axFile, selected in its folder.</summary>
    private void ShowProgram()
    {
        string? path;
        string name;
        switch (SelectedRow()?.Tag)
        {
            case ProcessRow row:
                path = SystemInfo.ProgramPath(row.Id);
                name = row.Name;
                break;
            case StartupEntry entry:
                path = CommandLine.ProgramOf(entry.Command);
                name = entry.Name;
                break;
            case ServiceRow:
                Announce("Show program is for a process or a startup program", true);
                return;
            default:
                Announce("Nothing selected", true);
                return;
        }

        if (path is null || !File.Exists(path))
        {
            Announce($"Windows does not say where {name}'s program is", true);
            return;
        }

        WindowMenu.Start(Dispatch.AxFile, path);
    }

    /// <summary>Ctrl+C: the row's line, without the ", selected" its item may end in.</summary>
    private void CopyLine()
    {
        if (SelectedRow() is not { } row)
        {
            Announce("Nothing selected", true);
            return;
        }

        CopyText(row.Label, "Copied");
    }

    /// <summary>Ctrl+Q (PR-2.5): the processes of the last read, or counted now, the memory now, the processor of the last read.</summary>
    private void SaySummary()
    {
        var count = _loaded[(int)View.Processes] ? _processes.Count : Process.GetProcesses().Length;
        var (total, available) = SystemInfo.Memory();
        Announce(ProcessTable.Summary(count, total, available, _processor), true);
    }

    // ---- services (PR-4) ----

    private void ShowServiceDetails(ServiceRow row)
    {
        var choices = new List<OverlayChoice>();
        if (row.State == ServiceState.Stopped)
        {
            choices.Add(new OverlayChoice("&Start", () => ControlService(row, ServiceAction.Start)));
        }

        if (row.State == ServiceState.Running && row.CanStop)
        {
            choices.Add(new OverlayChoice("S&top", () => ControlService(row, ServiceAction.Stop)));
            choices.Add(new OverlayChoice("&Restart", () => ControlService(row, ServiceAction.Restart)));
        }

        choices.Add(new OverlayChoice("&Close", IsDefault: true, IsCancel: true));
        ShowNotice(new Notice(row.DisplayName, string.Join("\n", row.Details), choices));
    }

    private void ControlSelectedService(ServiceAction action)
    {
        if (CurrentService() is not { } row)
        {
            Announce("Nothing selected", true);
            return;
        }

        ControlService(row, action);
    }

    /// <summary>Start, Stop, Restart (PR-4.3) on a background task, with the wait for the new state, then a new read.</summary>
    private void ControlService(ServiceRow row, ServiceAction action) => Do(async () =>
    {
        if (RefusedWhileWorking())
        {
            return;
        }

        var verb = action switch { ServiceAction.Start => "start", ServiceAction.Stop => "stop", _ => "restart" };
        BeginWork(action switch { ServiceAction.Start => "Starting ", ServiceAction.Stop => "Stopping ", _ => "Restarting " } + row.DisplayName + "…");
        UpdateStatusBar();
        ServiceResult result;
        try
        {
            result = await Task.Run(() => action switch
            {
                ServiceAction.Start => ServiceManager.Start(row.Name),
                ServiceAction.Stop => ServiceManager.Stop(row.Name),
                _ => Restart(row.Name),
            });
        }
        finally
        {
            EndWork();
            UpdateStatusBar();
        }

        Log.Info($"Service {row.Name} {verb}: {(result.Done ? "done" : result.Denied ? "denied" : result.TimedOut ? "timed out" : result.Error)}");
        Announce(result switch
        {
            { Done: true } => action switch { ServiceAction.Start => "Started ", ServiceAction.Stop => "Stopped ", _ => "Restarted " } + row.DisplayName,
            { TimedOut: true } => $"{row.DisplayName} is still {(action == ServiceAction.Stop ? "stopping" : "starting")}",
            { Denied: true } => $"Could not {verb} {row.DisplayName}: access is denied. Services need administrator rights to start and stop",
            _ => $"Could not {verb} {row.DisplayName}: {result.Error}",
        }, true);
        await RefreshAsync(announce: false);
    });

    /// <summary>A stop, then a start once the stop is done; a stop that did not finish is the answer.</summary>
    private static ServiceResult Restart(string name)
    {
        var stop = ServiceManager.Stop(name);
        return stop.Done ? ServiceManager.Start(name) : stop;
    }

    // ---- startup (PR-5.4 to PR-5.7) ----

    private void ChangeSelectedStartup(StartupAction action)
    {
        if (CurrentStartup() is not { } entry)
        {
            Announce("Nothing selected", true);
            return;
        }

        ChangeStartup(entry, action);
    }

    /// <summary>
    /// Turn off, Turn on, Remove: what is refused is said before anything else (an entry of the whole computer, one
    /// already off or on); a removal asks first, Cancel the default (PR-5.6); the others act at once.
    /// </summary>
    private void ChangeStartup(StartupEntry entry, StartupAction action)
    {
        if (StartupChange.Refusal(action, entry, StartupPlaces.Current.Administrator) is { } refused)
        {
            Announce(StartupChange.Said(action, entry, new StartupResult(refused)), true);
            return;
        }

        if (action != StartupAction.Remove)
        {
            ApplyStartup(entry, action);
            return;
        }

        ShowNotice(Notice.Ask(StartupChange.RemoveQuestion(entry), StartupChange.RemoveText(entry), "&Remove", () => ApplyStartup(entry, action), cancelIsDefault: true));
    }

    /// <summary>
    /// The change on a background task, its one sentence, then the list read again on the same entry, or after a
    /// removal on the one that followed it (PR-5.7). In a test run the places are the run's own (PR-5.8).
    /// </summary>
    private void ApplyStartup(StartupEntry entry, StartupAction action) => Do(async () =>
    {
        if (RefusedWhileWorking())
        {
            return;
        }

        var at = _startup.ToList().IndexOf(entry);
        var neighbour = at < 0 ? null : at + 1 < _startup.Count ? _startup[at + 1].Key : at > 0 ? _startup[at - 1].Key : null;
        BeginWork((action switch { StartupAction.TurnOff => "Turning off ", StartupAction.TurnOn => "Turning on ", _ => "Removing " }) + entry.Name + "…");
        UpdateStatusBar();
        StartupResult result;
        try
        {
            result = await Task.Run(() => StartupList.Apply(StartupPlaces.Current, entry, action));
        }
        finally
        {
            EndWork();
            UpdateStatusBar();
        }

        Log.Info($"Startup entry {entry.Name} ({StartupEntry.PlaceText(entry.Place)}) {action}: {result.Outcome}{(result.Error is { } error ? ", " + error : string.Empty)}");
        Announce(StartupChange.Said(action, entry, result), true);
        if (result.Outcome is not (StartupOutcome.Done or StartupOutcome.Gone))
        {
            return;
        }

        // The list read again, in place: the row stays on the entry, or goes to its neighbour when it is gone.
        var gone = action == StartupAction.Remove || result.Outcome == StartupOutcome.Gone;
        var select = _level == View.Startup && gone && SelectedKey() == entry.Key ? neighbour : null;
        if (await LoadAsync(View.Startup))
        {
            Fill(_level == View.Startup && select is not null ? select : SelectedKey());
        }
    });

    /// <summary>
    /// Delete with several startup programs selected (PR-5.6a, AX-8.16): what cannot be removed here (an entry of
    /// the whole computer without administrator rights) is said and stays; one question names the others, Cancel
    /// the default; each is removed on a background task; one sentence for all, "Removed 3 startup programs", with
    /// what could not be removed after it; the selection given up and the list read again.
    /// </summary>
    private void RemoveStartups(IReadOnlyList<StartupEntry> entries)
    {
        var administrator = StartupPlaces.Current.Administrator;
        var allowed = entries.Where(entry => StartupChange.Refusal(StartupAction.Remove, entry, administrator) is null).ToList();
        var refused = entries.Except(allowed).ToList();
        if (allowed.Count == 0)
        {
            var first = refused[0];
            Announce(StartupChange.Said(StartupAction.Remove, first, new StartupResult(StartupChange.Refusal(StartupAction.Remove, first, administrator)!.Value)), true);
            return;
        }

        var text = "Each entry is deleted for good from its Run key or its Startup folder, not sent to the Recycle Bin. The programs themselves stay on the computer.\n"
            + string.Join(", ", allowed.Select(entry => entry.Name));
        if (refused.Count > 0)
        {
            text += $"\nNot removed, since they are the whole computer's and need administrator rights: {string.Join(", ", refused.Select(entry => entry.Name))}";
        }

        ShowNotice(Notice.Ask($"Remove {Count(allowed.Count, "startup program")} from the programs that start with Windows?", text, "&Remove", () => Do(async () =>
        {
            if (RefusedWhileWorking())
            {
                return;
            }

            BeginWork($"Removing {Count(allowed.Count, "startup program")}…");
            UpdateStatusBar();
            List<(StartupEntry Entry, StartupResult Result)> results;
            try
            {
                results = await Task.Run(() => allowed.Select(entry => (entry, StartupList.Apply(StartupPlaces.Current, entry, StartupAction.Remove))).ToList());
            }
            finally
            {
                EndWork();
                UpdateStatusBar();
            }

            foreach (var (entry, result) in results)
            {
                Log.Info($"Startup entry {entry.Name} ({StartupEntry.PlaceText(entry.Place)}) Remove: {result.Outcome}{(result.Error is { } error ? ", " + error : string.Empty)}");
            }

            var done = results.Count(one => one.Result.Outcome is StartupOutcome.Done or StartupOutcome.Gone);
            var failed = results.Where(one => one.Result.Outcome is not (StartupOutcome.Done or StartupOutcome.Gone))
                .Select(one => StartupChange.Said(StartupAction.Remove, one.Entry, one.Result).TrimEnd('.')).ToList();
            Announce(failed.Count == 0 ? $"Removed {Count(done, "startup program")}" : $"Removed {done} of {Count(allowed.Count, "startup program")}. {string.Join(". ", failed)}", true);
            Selection.Clear();
            if (await LoadAsync(View.Startup))
            {
                Fill(SelectedKey());
            }
        }), cancelIsDefault: true));
    }

    // ---- the status bar (PR-2.4) ----

    /// <summary>The status bar shown again: what is being done or the level's name on the left, the memory in use on the right.</summary>
    private void UpdateStatusBar()
    {
        SetPlace(MemoryText());
        ShowStatus();
    }

    private static string MemoryText()
    {
        var (total, available) = SystemInfo.Memory();
        return total > 0 ? $"Memory {Math.Round((total - available) * 100d / total):0}% in use" : string.Empty;
    }
}
