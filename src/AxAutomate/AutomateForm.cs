using AxAutomate.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxAutomate;

/// <summary>
/// axAutomate's window (SPEC.md AU-4), on the item apps' shared window <see cref="ItemForm"/>, which holds the panes,
/// the moves between levels and the shared keys. This class holds what is axAutomate's: a folder of workflows as the
/// levels (its folders, then its workflows), a workflow's steps as rows in the order they run with the steps of a
/// block right after their step (AU-D5), and a step's parts as the detail pane's lines. This version views and runs
/// (AU-6a, the runs in <c>AutomateForm.Run.cs</c>): every change is refused with one sentence.
/// </summary>
public sealed partial class AutomateForm : ItemForm
{
    private const string ViewOnly = "This version views and runs the workflows; changing them comes later";

    private readonly AutomateSettings _settings;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _sessionMenu = new("&Session");
    private readonly ToolStripMenuItem _actionsMenu = new();

    /// <summary>The folder open, and the folders gone into from it; empty when none is open.</summary>
    private readonly List<string> _folders = [];

    /// <summary>The workflow gone into, or null at a folder's level.</summary>
    private Workflow? _workflow;

    private IReadOnlyList<StepRow> _steps = [];

    /// <summary>How many workflows the folder level shown holds, counted as its rows were filled, so that F5 and the menu need no second read.</summary>
    private int _levelWorkflows;

    /// <summary>Run and Stop the run in the Actions menu, made available as the menu opens (<see cref="UpdateRunItems"/>).</summary>
    private readonly ToolStripMenuItem _runItem;

    private readonly ToolStripMenuItem _stopItem;

    public AutomateForm(string? path, AutomateSettings settings, string? settingsError)
        : base(AxAutomateApp.Name, settings, AutomateSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        if ((path ?? settings.Folder) is { } folder && Directory.Exists(folder))
        {
            _folders.Add(Path.GetFullPath(folder));
        }

        // ---- menus and keys: one command list (docs/axKit/app-kit.md AK-2), each key from the tables (AU-4.4) ----
        Add(_sessionMenu, new Command("&Open a folder of workflows...", AxAutomateKeys.OpenFolder, OpenFolder) { When = NotRenaming, Mark = CommandMark.Open });
        // F5 runs here (AU-D8): Refresh is the menu's alone, and a run reads its file again first.
        _sessionMenu.DropDownItems.Add(Item("Re&fresh", () => Refresh(announce: true), Keys.None, CommandMark.Read));
        _sessionMenu.DropDownItems.Add(new ToolStripSeparator());
        _sessionMenu.DropDownItems.Add(CreateExitItem());

        // Context (AX-9.11, B-19): what acts on the folder, workflow, step or part the focus is on; Actions the runs.
        var listMenu = new ToolStripMenuItem("&Workflow");
        listMenu.DropDownItems.Add(Item("&Open", OpenSelected, ItemKeys.Open, CommandMark.Open));
        listMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll, CommandMark.Read));
        Add(listMenu, new Command("The workflow's file as &text", AxAutomateKeys.OpenFile, OpenFile) { When = NotRenaming, Mark = CommandMark.Open });
        var stepMenu = new ToolStripMenuItem("S&tep");
        stepMenu.DropDownItems.Add(Item("&Open", OpenSelected, ItemKeys.Open, CommandMark.Open));
        stepMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll, CommandMark.Read));
        stepMenu.DropDownItems.Add(Item("The workflow's file as &text", OpenFile, AxAutomateKeys.OpenFile, CommandMark.Open));
        var partMenu = new ToolStripMenuItem("&Part");
        partMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll, CommandMark.Read));

        _runItem = Add(_actionsMenu, new Command("R&un", AxAutomateKeys.Run, RunCommand) { When = NotRenaming, Mark = CommandMark.Act });
        _stopItem = Add(_actionsMenu, new Command("&Stop the run", AxAutomateKeys.StopRun, StopRun) { When = () => Running, Mark = CommandMark.Act });
        Add(_actionsMenu, new Command("The last run's &report", AxAutomateKeys.Report, ShowRunReport) { When = NotRenaming, Mark = CommandMark.Read });
        // Running keys only on what can run (AU-3.3.2): the items say so as the menu opens, and the command palette
        // reads them after the same refresh.
        CommandPalette.RefreshedOnOpening(_actionsMenu, UpdateRunItems);
        FormClosed += (_, _) => AbandonOnClose();

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "axAutomate &resources",
        [
            ("The automated checks of axKit", "https://github.com/KyleKeane/AxKit/blob/main/docs/axKit/automation.md"),
        ]);

        // The bundle's menus (AX-9.1), Options, Window, axKit and Help, end the bar in every window.
        PlaceMenuBar(_menu, _sessionMenu, _actionsMenu, new ToolStripMenuItem(), CreateContextMenu(listMenu, stepMenu, partMenu), options, help);
        PlaceControls(_menu);
        Fill(select: null);
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override IEnumerable<string> DiagnosticsLines => [$"Folder: {(_folders.Count == 0 ? "none" : _folders[^1])}, workflow: {_workflow?.Title ?? "none"}"];

    /// <summary>The folder open, for Recently closed (AX-9.4); the app alone when none.</summary>
    protected override string? PathInHand => _folders.Count > 0 ? _folders[0] : string.Empty;

    protected override IReadOnlyList<string> RestartArguments() => _folders.Count == 0 ? [Dispatch.AxAutomate] : [Dispatch.AxAutomate, _folders[0]];

    protected override string? FieldsFixed => ViewOnly;

    // ---- the levels (AU-4.2) ----

    protected override (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel()
    {
        if (_folders.Count == 0)
        {
            const string none = "No folder open: Ctrl+O opens a folder of workflows";
            return ([new ItemRow(false, "none", none, none)], "Workflows, no folder open", "Workflows", "No folder");
        }

        var folder = _folders[^1];
        if (_workflow is { } workflow)
        {
            var rows = _steps.Select((row, i) => new ItemRow(false, "step:" + i, row.Label, row.Label + StepMark(row), row)).ToList();
            if (rows.Count == 0)
            {
                rows.Add(new ItemRow(false, "empty", "No steps", "No steps"));
            }

            var problem = workflow.Problem is null ? string.Empty : ", not all understood: " + workflow.Problem;
            return (rows, $"{workflow.Title}, {StepText.Count(workflow.StepCount, "step")}{problem}", workflow.Title, folder);
        }

        var entries = WorkflowFolder.Read(folder);
        _levelWorkflows = entries.Count(entry => !entry.IsFolder);
        var items = entries.Select(entry => entry.IsFolder
            ? new ItemRow(true, entry.Path, entry.Name, $"{entry.Name}, folder, {StepText.Count(WorkflowFolder.CountWorkflows(entry.Path), "workflow")}", entry)
            : WorkflowRow(entry)).ToList();
        if (items.Count == 0)
        {
            items.Add(new ItemRow(false, "empty", "No workflows here", "No workflows here"));
        }

        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)) is { Length: > 0 } own ? own : folder;
        return (items, $"{name}, {StepText.Count(entries.Count(entry => !entry.IsFolder), "workflow")}, {StepText.Count(entries.Count(entry => entry.IsFolder), "folder")}", name, folder);
    }

    /// <summary>A workflow's row by its title (AU-4.2): a script's is its file's name, a workflow file's its own; after a run, its outcome.</summary>
    private ItemRow WorkflowRow(FolderEntry entry)
    {
        var workflow = WorkflowFolder.ReadWorkflow(entry.Path);
        var problem = workflow.Problem is null ? string.Empty : ", not all understood";
        return new ItemRow(true, entry.Path, workflow.Title, $"{workflow.Title}, workflow, {StepText.Count(workflow.StepCount, "step")}{problem}{OutcomeMark(workflow.Path)}", entry);
    }

    protected override void EnterList(ItemRow row)
    {
        if (row.Tag is not FolderEntry entry)
        {
            return;
        }

        if (entry.IsFolder)
        {
            _folders.Add(entry.Path);
            return;
        }

        _workflow = WorkflowFolder.ReadWorkflow(entry.Path);
        _steps = StepText.Rows(_workflow);
    }

    protected override string? LeaveList()
    {
        if (_workflow is { } workflow)
        {
            _workflow = null;
            _steps = [];
            return workflow.Path;
        }

        if (_folders.Count > 1)
        {
            var left = _folders[^1];
            _folders.RemoveAt(_folders.Count - 1);
            return left;
        }

        return null;
    }

    /// <summary>Session, Refresh: a workflow open is read again from its file.</summary>
    protected override void BeforeRefresh()
    {
        if (_workflow is { } workflow)
        {
            _workflow = WorkflowFolder.ReadWorkflow(workflow.Path);
            _steps = StepText.Rows(_workflow);
        }
    }

    protected override ItemDetail DetailOf(ItemRow? row)
    {
        switch (row?.Tag)
        {
            case StepRow step when _workflow is { } open:
                var stepLines = StepText.Details(step, open).Select(line => new ItemDetailLine(line)).ToList();
                if (OutcomeLine(open.Path, step) is { } failedLine)
                {
                    stepLines.Insert(1, failedLine);
                }

                return new ItemDetail(step.Step.IsNote ? "A note" : "Step " + step.Number, row.Key, null, stepLines);
            case FolderEntry { IsFolder: true } folder:
                return new ItemDetail("Details of " + folder.Name, row.Key, null,
                [
                    new ItemDetailLine("Folder: " + folder.Path),
                    new ItemDetailLine("Workflows: " + WorkflowFolder.CountWorkflows(folder.Path) + ", its folders' included"),
                    new ItemDetailLine("Right or Enter goes into it"),
                ]);
            case FolderEntry file:
                var workflow = WorkflowFolder.ReadWorkflow(file.Path);
                var lines = new List<ItemDetailLine>
                {
                    new("Title: " + workflow.Title),
                    new("Covers: " + (workflow.Covers.Length > 0 ? workflow.Covers : "not said")),
                    new($"Steps: {workflow.StepCount}"),
                    new("File: " + file.Path),
                    new(file.Path.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase) ? "Format: a workflow file" : "Format: a script of the runner, read as a workflow"),
                };
                if (workflow.Problem is { } problem)
                {
                    lines.Add(new ItemDetailLine("Not understood: " + problem));
                }

                lines.Add(ScriptLines.CannotRun(workflow, nvda: false, confined: true) is { } refusal
                    ? new ItemDetailLine("Cannot run: " + refusal)
                    : OutcomeLine(workflow.Path, null) ?? new ItemDetailLine("Not run yet"));
                lines.Add(new ItemDetailLine("Right or Enter goes into its steps"));
                return new ItemDetail("Details of " + workflow.Title, row.Key, null, lines);
            default:
                return new ItemDetail("Details", row?.Key, null, [new ItemDetailLine(row?.Label ?? "Nothing selected")]);
        }
    }

    // ---- what this version refuses (AU-6) ----

    protected override void SaveFields(ItemDetail detail, ItemDocument changed, string said, int selectField) => Announce(ViewOnly, true);

    protected override void RenameItem(ItemRow row, string title) => Announce(ViewOnly, true);

    protected override string? RenameRefused(ItemRow row) => ViewOnly;

    protected override string? RefusedName(ItemRow row, string name) => ViewOnly;

    protected override void DeleteRow(ItemRow row) => Announce(ViewOnly, true);

    protected override void NewItem() => Announce(ViewOnly, true);

    protected override void NewList() => Announce(ViewOnly, true);

    protected override void Undo() => Announce("Nothing to undo: nothing changes here", true);

    // ---- its own commands ----

    /// <summary>Ctrl+O (AU-4.1): a folder of workflows with Windows' folder picker, remembered for the next start.</summary>
    private void OpenFolder()
    {
        if (ChooseFolder("Choose a folder of workflows", _folders.Count > 0 ? _folders[0] : null) is not { } folder)
        {
            return;
        }

        _folders.Clear();
        _folders.Add(folder);
        _workflow = null;
        _steps = [];
        _settings.Folder = folder;
        SaveSettings();
        Fill(select: null);
        AnnounceLevel();
    }

    /// <summary>Ctrl+J: the workflow in hand, or the one selected, as text: a script in axDown, a workflow file in axJSON.</summary>
    private void OpenFile()
    {
        var path = _workflow?.Path ?? (SelectedRow()?.Tag is FolderEntry { IsFolder: false } entry ? entry.Path : null);
        if (path is null)
        {
            Announce("No workflow selected", true);
            return;
        }

        WindowMenu.Start(path.EndsWith(WorkflowFile.Extension, StringComparison.OrdinalIgnoreCase) ? Dispatch.AxJSON : Dispatch.AxDown, path);
        Announce("Opening " + Path.GetFileName(path), true);
    }
}
