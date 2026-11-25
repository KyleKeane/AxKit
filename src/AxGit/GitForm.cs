using AxGit.Core;
using AxKit.Core;
using AxKit.Forms;

namespace AxGit;

/// <summary>
/// axGIT's window (SPEC.md AG-2): a menu bar, one list, one read-only text for the details (a diff, a commit), and a
/// status bar with the branch. The window is a navigator, the bundle's item window (<see cref="ItemForm"/>;
/// docs/axKit/framework.md FW-4.10, step 8, 2026-11-22; a window of its own with three views of one list until
/// then): the top level, the repository, holds three lists, Changes, Log and Branches, and the window starts
/// inside Changes; its second pane is a text. Space selects files, and staging, unstaging and discarding act on
/// the selected files. Every action is one git command run on a background task (AG-D6); the result is one
/// announcement, or a notice with git's whole answer. The list, its selection, the text pane with its keys, find,
/// the Context menu and a navigator's keys are the item window's; this class holds the repository, its trust, the
/// reads and the commands.
/// </summary>
public sealed partial class GitForm : ItemForm
{
    /// <summary>The three lists of the repository (AG-2.2); their names are the rows' keys at the top level.</summary>
    private enum View
    {
        Changes,
        Log,
        Branches,
    }

    private const int UntrackedLimit = 200 * 1024;

    /// <summary>The repository's own programs set to nothing while it is not trusted (AG-1.6); empty for a trusted one.</summary>
    private IReadOnlyList<string> _overrides = [];

    private readonly GitSettings _settings;
    private readonly string? _startPath;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _recentItem = new("Open &recent repository");
    private readonly ToolStripMenuItem _listMenu = new("&List");
    private readonly ToolStripMenuItem _changeMenu = new("&Change");
    private readonly ToolStripMenuItem _commitMenu = new("C&ommit");
    private readonly ToolStripMenuItem _branchMenu = new("&Branch");

    /// <summary>The menu items that run git, disabled while a command runs and when git is missing (AG-5.4).</summary>
    private readonly List<ToolStripMenuItem> _gitItems = [];

    private string? _root;

    /// <summary>The level shown: one of the three lists, or null at the top, the repository.</summary>
    private View? _level = View.Changes;

    /// <summary>Which of the three lists has been read since it was last made stale by a commit, a switch or a fetch.</summary>
    private readonly bool[] _loaded = new bool[3];

    /// <summary>The row each list was left on, by its key (the file's path, the commit's hash, the branch's name), to come back to.</summary>
    private readonly string?[] _kept = new string?[3];

    private RepositoryStatus _repo = RepositoryStatus.Empty;
    private IReadOnlyList<Commit> _commits = [];
    private IReadOnlyList<Branch> _branches = [];

    private bool _gitMissing;

    public GitForm(string? path, GitSettings settings, string? settingsError)
        : base(AxGitApp.Name, settings, GitSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        _startPath = path;
        SetTitle("No repository");

        // ---- menus and keys (AG-6.1, AG-6.2): one command list (docs/axKit/app-kit.md AK-2), each key from the tables ----
        // Repository is the thing worked on, in the standard order of a first menu (bundle AX-6): Open and the recent
        // repositories, then the slot the editors have Save in (Refresh here), then the extras, then Exit. The keys act
        // while a menu item is disabled, as before: the command says what stops it.
        var repository = new ToolStripMenuItem("&Session");
        Add(repository, new Command("&Open repository...", AxGitKeys.OpenRepository, OpenRepositoryDialog) { Mark = CommandMark.Open });
        CommandPalette.RefreshedOnOpening(_recentItem, FillRecent);
        FillRecent();
        repository.DropDownItems.Add(_recentItem);
        repository.DropDownItems.Add(new ToolStripSeparator());
        Git(Add(repository, new Command("Re&fresh", AxGitKeys.Refresh, () => Do(() => RefreshAsync(announce: true))) { Mark = CommandMark.Read }));
        repository.DropDownItems.Add(new ToolStripSeparator());
        // Show current repository in ▸ File Explorer, axClaude, axTerminal, axFile: the way back to where the user came
        // from (AG-1.4). The base makes the submenu around its first item, so File Explorer is a key of the list and that item.
        var explorer = new Command("File &Explorer", AxGitKeys.ShowInExplorer, ShowInExplorer) { Mark = CommandMark.Open };
        repository.DropDownItems.Add(CreateShowInMenu("Show current repository &in", Add(explorer), RepositoryOrSay, FolderMissing));
        repository.DropDownItems.Add(new ToolStripSeparator());
        repository.DropDownItems.Add(CreateExitItem());

        // Actions (AX-9.1): what axGIT does to the repository, the same wherever the focus is. Stage and Unstage act
        // on the selected files of Changes, or on the file you are on when none is selected (AG-3.2, 2026-11-22;
        // Space staged one file until then, and selects now, bundle AX-8.16). Stage all and Unstage all have no
        // key any more: Ctrl+Shift+A, Stage all's, is the bundle's Deselect all, and Ctrl+A then Stage does it.
        var actions = new ToolStripMenuItem();
        var stage = new Command("&Stage", AxGitKeys.Stage, () => Do(() => StageFilesAsync(stage: true))) { Mark = CommandMark.Act };
        var unstage = new Command("&Unstage", AxGitKeys.Unstage, () => Do(() => StageFilesAsync(stage: false))) { Mark = CommandMark.Act };
        Git(Add(actions, stage));
        Git(Add(actions, unstage));
        Git(Add(actions, new Command("Stage &all", Keys.None, () => Do(() => StageAllAsync(stage: true))) { Mark = CommandMark.Act }));
        Git(Add(actions, new Command("U&nstage all", Keys.None, () => Do(() => StageAllAsync(stage: false))) { Mark = CommandMark.Act }));
        Git(Add(actions, new Command("&Commit...", BundleKeys.MainAction, Commit) { Mark = CommandMark.Act }));
        actions.DropDownItems.Add(new ToolStripSeparator());
        Git(Add(actions, new Command("New &branch...", AxGitKeys.NewBranch, NewBranch) { Mark = CommandMark.Act }));
        Git(Add(actions, new Command("&Fetch", AxGitKeys.Fetch, () => Do(() => ToServerAsync(FetchAsync))) { Mark = CommandMark.Act }));
        Git(Add(actions, new Command("&Pull", AxGitKeys.Pull, () => Do(() => ToServerAsync(PullAsync))) { Mark = CommandMark.Act }));
        Git(Add(actions, new Command("Pus&h", AxGitKeys.Push, () => Do(() => ToServerAsync(PushAsync))) { Mark = CommandMark.Act }));
        actions.DropDownItems.Add(new ToolStripSeparator());
        actions.DropDownItems.Add(Item("Select all &files", SelectAllRows, SelectionKeys.SelectAll, CommandMark.Change));
        actions.DropDownItems.Add(Item("&Deselect all", SelectNoRows, SelectionKeys.SelectNone, CommandMark.Change));

        // Navigation (AX-9.1): the three lists, each a jump from anywhere, and the way up to the repository.
        var navigation = new ToolStripMenuItem();
        // On Alt+1, 2 and 3, each with its older Ctrl key as a further key of the same command (bundle AX-8.14).
        Place(new Command("&Changes", AxGitKeys.ViewChanges, () => Do(() => ShowListAsync(View.Changes))) { Mark = CommandMark.Open }, AxGitKeys.ViewChangesAlso);
        Place(new Command("&Log", AxGitKeys.ViewLog, () => Do(() => ShowListAsync(View.Log))) { Mark = CommandMark.Open }, AxGitKeys.ViewLogAlso);
        Place(new Command("&Branches", AxGitKeys.ViewBranches, () => Do(() => ShowListAsync(View.Branches))) { Mark = CommandMark.Open }, AxGitKeys.ViewBranchesAlso);
        navigation.DropDownItems.Add(new ToolStripSeparator());
        navigation.DropDownItems.Add(Item("&Up to the repository", UpLevel, ItemKeys.Up, CommandMark.Open));
        void Place(Command command, Keys also)
        {
            Add(navigation, command);
            AddKey(command with { Key = also });
        }

        // Context (AX-9.11): what acts on the row you are on, by its kind. Enter, Delete and the selection keys are
        // the navigator's (the item window's commands, registered after these by PlaceControls); the menu items
        // for them here only show the keys, and state how far they reach: Switch to branch and Discard act.
        Git(Placed(_changeMenu, Item("Show &diff", OpenSelected, ItemKeys.Open, CommandMark.Read)));
        Git(Add(_changeMenu, stage));
        Git(Add(_changeMenu, unstage));
        Add(_changeMenu, new Command("&Open file in editor", AxGitKeys.OpenInEditor, OpenInEditor) { Mark = CommandMark.Open });
        Git(Placed(_changeMenu, Item("Disca&rd changes...", () => WithRow(DeleteRow), ItemKeys.Delete, CommandMark.Act)));
        _changeMenu.DropDownItems.Add(new ToolStripSeparator());
        _changeMenu.DropDownItems.Add(Item("Se&lect or deselect", SelectRow, SelectionKeys.Select, CommandMark.Change));
        _changeMenu.DropDownItems.Add(Item("Select to &here", SelectToHere, SelectionKeys.SelectTo, CommandMark.Change));
        Git(Placed(_commitMenu, Item("&Show the commit", OpenSelected, ItemKeys.Open, CommandMark.Open)));
        Git(Placed(_branchMenu, Item("&Switch to branch", OpenSelected, ItemKeys.Open, CommandMark.Act)));
        _listMenu.DropDownItems.Add(Item("&Open", OpenSelected, ItemKeys.Open, CommandMark.Open));

        // Options, as the other windows' (AX-6): every setting in the submenu for what it is about.
        var options = new ToolStripMenuItem();
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "Git &resources",
            [("&git-scm.com", "https://git-scm.com"), ("&Pro Git, the book", "https://git-scm.com/book/en/v2")]);

        // The list and the details (AG-2.1) are the item window's navigator and its text pane, with their keys:
        // the pane keys, find where the focus is (AG-2.4), the Context menu's keys.
        PlaceMenuBar(_menu, repository, actions, navigation, CreateContextMenu(MenuOf, _listMenu, _changeMenu, _commitMenu, _branchMenu), options, help);
        PlaceControls(_menu);
        Fill(select: null);
    }

    /// <summary>A menu item placed in its menu and given back, for <see cref="Git"/>.</summary>
    private static ToolStripMenuItem Placed(ToolStripMenuItem menu, ToolStripMenuItem item)
    {
        menu.DropDownItems.Add(item);
        return item;
    }

    // ---- what the navigator asks for (the item window, docs/axKit/framework.md FW-4.10) ----

    /// <summary>The second pane is a text: a file's diff, a commit.</summary>
    protected override SecondPane Pane => SecondPane.Text;

    /// <summary>Space selects files in Changes (AX-8.16): Stage, Unstage and Discard act on all the selected.</summary>
    protected override bool HasSelection => true;

    /// <summary>A changed file can be selected. A commit, a branch and a list cannot: nothing here acts on several of them.</summary>
    protected override bool CanSelectRow(ItemRow row) => row.Tag is ChangedFile;

    protected override string RowNoun => "file";

    /// <summary>The level by the repository and the list, not by the status bar's second part.</summary>
    protected override string LevelIdentity() => $"{_root}\n{_level}";

    /// <summary>
    /// A navigator's keys with no use here: nothing is renamed or undone, there is no second kind of new thing,
    /// and a row has nothing to read in full beyond its line. F2, Ctrl+Z, Ctrl+Shift+N and Ctrl+Q are no commands of
    /// the window. Ctrl+N is New branch, axGIT's own.
    /// </summary>
    protected override IReadOnlyCollection<Keys> KeysWithoutUse => [ItemKeys.Rename, ItemKeys.Undo, ItemKeys.NewList, ItemKeys.ReadAll];

    protected override void NewItem() => NewBranch();

    /// <summary>
    /// The level shown: at the top the three lists, each with its count once it has been read; in a list its rows
    /// as last read from git. Without a repository there are no rows, and the list and the title say so. The
    /// title is "folder" and the repository's full path after the app's name; the status bar's second part is its path.
    /// </summary>
    protected override (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel() =>
        _root is null
            ? ([], "No repository", "No repository", string.Empty)
            : (LevelRows(), LevelName(_root), FolderTitle(_root), _root);

    private IReadOnlyList<ItemRow> LevelRows() => _level switch
    {
        View.Changes => [.. _repo.Files.Select(file => new ItemRow(IsList: false, file.Path, file.Path, file.Label, file))],
        View.Log => [.. _commits.Select(commit => new ItemRow(IsList: false, commit.Hash, commit.Subject, commit.Label, commit))],
        View.Branches => [.. _branches.Select(branch => new ItemRow(IsList: false, branch.Name, branch.Name, branch.Label, branch))],
        _ => [TopRow(View.Changes), TopRow(View.Log), TopRow(View.Branches)],
    };

    /// <summary>A list as a row of the top level: "Changes, 3 files" once read, "Changes" before or when stale.</summary>
    private ItemRow TopRow(View view) => new(IsList: true, view.ToString(), view.ToString(), _loaded[(int)view] ? ListName(view) : view.ToString());

    private string LevelName(string root) =>
        _level is not { } view ? $"{PathName.Of(root)}, 3 lists"
        : _loaded[(int)view] ? ListName(view)
        : $"{view}, reading";

    private string ListName(View view) => view switch
    {
        View.Changes => _repo.ListName,
        View.Log => GitLog.ListName(_commits.Count),
        _ => GitBranches.ListName(_branches.Count),
    };

    /// <summary>
    /// Right and Enter on a list of the top level: the list is read through git off the UI thread, then shown and
    /// said once (<see cref="ShowListAsync"/>). The item window's own way in fills at once, so it is turned down
    /// with an empty reason, which says nothing: the app does the going in itself.
    /// </summary>
    protected override string? EntryRefused(ItemRow row)
    {
        if (Enum.TryParse<View>(row.Key, out var view))
        {
            Do(() => ShowListAsync(view));
        }

        return string.Empty;
    }

    /// <summary>Left and Backspace in a list: up to the repository, on the list left; at the top, and without a repository, nothing, the edge sound.</summary>
    protected override string? LeaveList()
    {
        if (_root is null || _level is not { } view)
        {
            return null;
        }

        _kept[(int)view] = SelectedKey();
        _level = null;
        return view.ToString();
    }

    /// <summary>
    /// Enter on a row of a list, the navigator's Open, by its kind (AG-3.3, AG-4.2, AG-5.2): a file's diff or a
    /// commit in the text pane, with the focus moved there; a branch switched to.
    /// </summary>
    protected override void OpenItem(ItemRow row)
    {
        switch (row.Tag)
        {
            case ChangedFile:
                Do(ShowDiffAsync);
                break;
            case AxGit.Core.Commit:
                Do(ShowCommitAsync);
                break;
            default:
                Do(SwitchBranchAsync);
                break;
        }
    }

    /// <summary>
    /// Delete, the navigator's key, by the kind of row (AG-3.4): the changes to a file are discarded after a
    /// question, and with files selected those of all of them after one question; a commit and a branch are not
    /// removed here, and a list of the top level is no thing to discard.
    /// </summary>
    protected override void DeleteRow(ItemRow row)
    {
        switch (row.Tag)
        {
            case ChangedFile:
                Discard();
                return;
            case AxGit.Core.Commit:
                Announce("A commit is not removed here", true);
                return;
            case Branch:
                Announce("A branch is not deleted here", true);
                return;
            default:
                Announce("A list: Right goes into it", true);
                return;
        }
    }

    /// <summary>The menu of the row you are on, whose items Context holds (AG-6.1, AX-9.11).</summary>
    private ToolStripMenuItem? MenuOf(ItemRow row) => row.Tag switch
    {
        ChangedFile => _changeMenu,
        AxGit.Core.Commit => _commitMenu,
        Branch => _branchMenu,
        _ => _listMenu,
    };

    /// <summary>The status bar's first part (AG-2.3): the command running, that git is missing, that no repository is open, or the branch and the counts.</summary>
    protected override string? StatusState =>
        Working is { } work ? work
        : _gitMissing ? "git was not found"
        : _root is null ? "No repository"
        : _repo.StatusText;

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    /// <summary>
    /// What an AI tool's act command would act on (docs/axKit/mcp.md MC-7.3): the file, commit or branch you are
    /// on, and the selected files with it while there are any, so that the target changes when the selection does;
    /// with none in hand (an empty list, the top level) the item window's.
    /// </summary>
    protected override AxKit.Core.Mcp.RunTarget McpTarget()
    {
        var text = SelectedRow()?.Tag switch
        {
            ChangedFile file => file.Path,
            AxGit.Core.Commit commit => $"the commit {commit.Subject}, {commit.Hash}",
            Branch branch => $"the branch {branch.Name}",
            _ => string.Empty,
        };
        return text.Length == 0 ? base.McpTarget()
            : Selection.Count == 0 ? AxKit.Core.Mcp.RunTarget.Of(text)
            : new AxKit.Core.Mcp.RunTarget($"{text}, and {Selection.Count} selected", text + "\n" + string.Join("\n", Selection.Keys.Order(StringComparer.OrdinalIgnoreCase)));
    }

    /// <summary>Ctrl+S, the bundle's save key: nothing to save in a git window, and the keys that do something said (AG-6.2).</summary>
    protected override void SaveCommand() => Announce($"Nothing to save here. {BundleKeys.Text(AxGitKeys.Stage).TrimEnd('.')} stages, {BundleKeys.Text(BundleKeys.MainAction)} commits", true);

    protected override string UpdateRestartText => $"closes {AxGitApp.Name}, installs it and starts it again on the same repository";

    protected override IEnumerable<string> DiagnosticsLines => [$"Repository: {_root ?? "(none)"}"];

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Do(() => StartAsync(_startPath));
    }

    /// <summary>The repository, for Recently closed (AX-9.4).</summary>
    protected override string? PathInHand => _root;

    // ---- the repository (AG-1.2) ----

    private async Task StartAsync(string? path)
    {
        // Bundle AX-11.5: Git runs nothing before the user's consent; Connect opens the repository.
        if (!HasConsent(ConsentTarget.Git, () => Do(() => StartAsync(path)), () =>
            {
                if (_root is null)
                {
                    SetNoRepository();
                }

                Announce($"Not connected to Git. {BundleKeys.Text(AxGitKeys.OpenRepository)} opens a repository", true);
            }))
        {
            return;
        }

        var target = path;
        if (target is null)
        {
            // AX-9.5a: the remembered repositories are looked at only on a drive of this computer; when the last one
            // is on the network that is said, and Open recent repository opens it.
            target = _settings.RecentRepositories.FirstOrDefault(recent => LocalPaths.AtStart(recent, Directory.Exists, out _) is not null);
            LocalPaths.AtStart(_settings.RecentRepositories.FirstOrDefault(), Directory.Exists, out var notOpened);
            SayAtStart(notOpened);
        }

        if (target is null)
        {
            SetNoRepository();
            return;
        }

        // A path given (the command line, Open recent repository) is the user's own ask: looked at off the window's thread (R-64).
        if (!await Task.Run(() => Directory.Exists(target)))
        {
            // A recent repository on a share that is gone, listed without the check (AX-9.5): said, the open one kept.
            Fail($"{target} no longer exists.");
            return;
        }

        string? root;
        try
        {
            root = await GitRunner.RootAsync(target);
        }
        catch (GitNotFoundException)
        {
            GitMissing();
            return;
        }

        if (IsDisposed)
        {
            return;
        }

        if (root is null)
        {
            // AG-1.2: the notice names the folder; a repository already open stays (until 2026-10-23 it was dropped first).
            if (_root is null)
            {
                SetNoRepository();
            }

            ShowNotice(Notice.Plain("Not a git repository", $"{target} is not inside a git repository. Session, Open repository ({BundleKeys.Text(AxGitKeys.OpenRepository)}) takes another folder.") with { Unprompted = true });
            return;
        }

        await OpenRepositoryAsync(root);
    }

    private async Task OpenRepositoryAsync(string root)
    {
        _root = root;
        Array.Clear(_loaded);
        Array.Clear(_kept);
        _level = View.Changes;
        _repo = RepositoryStatus.Empty;
        _commits = [];
        _branches = [];
        // Another repository: nothing selected, and the text of the one before gone.
        Selection.Clear();
        TextPane.Clear();
        TextPane.AccessibleName = "Details";
        SetTitle(FolderTitle(root));
        _settings.RememberRepository(root);
        SaveSettings();
        Log.Info($"Opened {root}");
        await CheckTrustAsync(root);
        await RefreshAsync(announce: false);
    }

    /// <summary>
    /// AG-1.6 (security audit S-2): what in the repository's own configuration and hooks runs a program, read before
    /// any other command; unless the user trusts the repository, every command sets each of them to nothing, and a
    /// notice says what is set aside, with the way to trust it.
    /// </summary>
    private async Task CheckTrustAsync(string root)
    {
        _overrides = [];
        try
        {
            var config = await GitRunner.RunAsync(root, GitTrust.ConfigArguments, GitRunner.Short);
            var hooks = await GitRunner.RunAsync(root, GitTrust.HooksArguments, GitRunner.Short);
            var hooksFolder = hooks.Ok ? GitTrust.HooksFolder(hooks.Output, root) : null;
            var risks = GitTrust.Risks(config.Ok ? config.Output : string.Empty, hooksFolder);
            if (risks.Count == 0 || IsDisposed)
            {
                return;
            }

            if (_settings.Trusts(root, risks))
            {
                // A trust from before the marks (2026-11-16) is bound to what the repository runs now.
                if (_settings.Trust(root, risks))
                {
                    SaveSettings();
                }

                return;
            }

            // AG-1.6: a trust is for what the repository ran when it was given; anything else is asked about again.
            var changed = _settings.WasTrusted(root) ? "What this repository would run has changed since you trusted it, so it is not trusted now. " : string.Empty;
            _overrides = GitTrust.Overrides(risks);
            Log.Info($"Untrusted repository: {risks.Count} programs of its own set aside");
            ShowNotice(new Notice("This repository runs programs of its own",
                $"{changed}Its settings or hooks name programs git would run: {string.Join(", ", risks)}. axGIT runs git without them. Trust the repository only if you know where it comes from: then git runs them as it would in a console.",
            [
                new OverlayChoice("Keep them &off", IsDefault: true, IsCancel: true),
                new OverlayChoice("&Trust this repository", () => TrustRepository(root, risks)),
            ]) { Unprompted = true });
        }
        catch (GitNotFoundException)
        {
            // git missing is said by the first command that needs it.
        }
    }

    private void TrustRepository(string root, IReadOnlyList<string> risks)
    {
        _settings.Trust(root, risks);
        SaveSettings();
        _overrides = [];
        Announce("Trusted", true);
    }

    private void SetNoRepository()
    {
        _root = null;
        _repo = RepositoryStatus.Empty;
        _level = View.Changes;
        Array.Clear(_loaded);
        Fill(select: null);
    }

    /// <summary>Repository → Open repository… (Ctrl+O): the bundle's folder picker, one of the system's own windows; any folder inside a repository will do.</summary>
    private void OpenRepositoryDialog()
    {
        var chosen = ChooseFolder("Choose a folder inside the repository", _root);
        if (chosen is not null)
        {
            Do(() => StartAsync(chosen));
        }
    }

    /// <summary>Repository → Open recent repository (AG-6.1): the last ten, those that still exist, "1. name (path)" (AX-9.5).</summary>
    private void FillRecent() =>
        RecentMenu.FillFolders(_recentItem, _settings.RecentRepositories, "&No recent repositories", CommandMark.Open, target => Do(() => StartAsync(target)));

    /// <summary>Repository → Show current repository in → File Explorer (Ctrl+W): the folder opened in File Explorer, one of the system's own windows.</summary>
    private void ShowInExplorer()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        OpenInExplorer(_root);
    }

    /// <summary>Repository → Show current repository in → axClaude, axTerminal, axFile (AG-1.4): a new window of that app on the repository's folder.</summary>
    private string? RepositoryOrSay()
    {
        if (_root is null)
        {
            Announce("No repository", true);
        }

        return _root;
    }

    /// <summary>Changes → Open file in editor (Ctrl+E): the selected file in axJSON for .json, axDown otherwise (AG-1.4).</summary>
    private void OpenInEditor()
    {
        if (_root is null)
        {
            Announce("No repository", true);
            return;
        }

        if (CurrentFile() is not { } file)
        {
            Announce("No file selected", true);
            return;
        }

        var path = Path.Combine(_root, file.Path.Replace('/', Path.DirectorySeparatorChar));
        WindowMenu.Start(Dispatch.EditorOf(path), path);
    }
}
