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

    /// <summary>The repository open and what git is run with in it (AG-1.6, <see cref="GitSession"/>): every run goes through it; null with no repository.</summary>
    private GitSession? _session;

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
        SetTitle(null);

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
        var explorer = new Command("File &Explorer", Keys.None, ShowInExplorer) { Mark = CommandMark.Open };
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
        Git(Add(actions, new Command("&Fetch", AxGitKeys.Fetch, () => Do(() => ToServerAsync(push: false, "Fetch", FetchAsync))) { Mark = CommandMark.Act }));
        Git(Add(actions, new Command("&Pull", AxGitKeys.Pull, () => Do(() => ToServerAsync(push: false, "Pull", PullAsync))) { Mark = CommandMark.Act }));
        Git(Add(actions, new Command("Pus&h", AxGitKeys.Push, () => Do(() => ToServerAsync(push: true, "Push", PushAsync))) { Mark = CommandMark.Act }));
        actions.DropDownItems.Add(new ToolStripSeparator());
        actions.DropDownItems.Add(Item("Select all &files", SelectAllRows, SelectionKeys.SelectAll, CommandMark.Change));
        actions.DropDownItems.Add(Item("&Deselect all", SelectNoRows, SelectionKeys.SelectNone, CommandMark.Change));

        // Navigation (AX-9.1): the three lists, each a jump from anywhere, and the way up to the repository.
        var navigation = new ToolStripMenuItem();
        // On Alt+1, 2 and 3 (bundle AX-8.14; the older Ctrl keys went on 2026-11-26, AX-8.18 rule 3).
        Add(navigation, new Command("&Changes", AxGitKeys.ViewChanges, () => Do(() => ShowListAsync(View.Changes))) { Mark = CommandMark.Open });
        Add(navigation, new Command("&Log", AxGitKeys.ViewLog, () => Do(() => ShowListAsync(View.Log))) { Mark = CommandMark.Open });
        Add(navigation, new Command("&Branches", AxGitKeys.ViewBranches, () => Do(() => ShowListAsync(View.Branches))) { Mark = CommandMark.Open });
        navigation.DropDownItems.Add(new ToolStripSeparator());
        navigation.DropDownItems.Add(Item("&Up to the repository", UpLevel, ItemKeys.Up, CommandMark.Open));

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
    /// and a row has nothing to read in full beyond its line. F2, Ctrl+Z, Ctrl+Shift+N and F7 are no commands of
    /// the window. Ctrl+N is New branch, axGIT's own.
    /// </summary>
    protected override IReadOnlyCollection<Keys> KeysWithoutUse => [ItemKeys.Rename, ItemKeys.Undo, ItemKeys.NewList, ItemKeys.ReadAll];

    protected override void NewItem() => NewBranch();

    /// <summary>
    /// The level shown: at the top the three lists, each with its count once it has been read; in a list its rows
    /// as last read from git. Without a repository there are no rows, and the list and the title say so. The
    /// title is "folder" and the repository's full path after the app's name; the status bar's second part is its path.
    /// </summary>
    protected override (IReadOnlyList<ItemRow> Rows, string Name, Opened? Title, string Place) ReadLevel() =>
        _root is null
            ? ([], "No repository", null, string.Empty)
            : (LevelRows(), LevelName(_root), Opened.At(_root), _root);

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
    protected override void SaveCommand() => Refuse($"Nothing to save here. {BundleKeys.Text(AxGitKeys.Stage).TrimEnd('.')} stages, {BundleKeys.Text(BundleKeys.MainAction)} commits");

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

                Refuse($"Not connected to Git. {BundleKeys.Text(AxGitKeys.OpenRepository)} opens a repository");
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

        // AG-1.6 (the review's fourth pass, T1): the repository that holds the folder is found by axKit's own code,
        // off the window's thread: no git command runs in a folder before what it would run is known.
        var root = await Task.Run(() => GitTrustScan.RootOf(target));
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

            ShowNotice(Notice.Plain("Not a git repository", $"{target} is not inside a git repository. Session, Open repository ({BundleKeys.Text(AxGitKeys.OpenRepository)}) takes another folder.").ByItself());
            return;
        }

        await OpenRepositoryAsync(root);
    }

    /// <summary>
    /// AG-1.6 (security audit S-2; the reviews of 2026-11-27 and 2026-11-28): what the repository's own settings,
    /// hooks and files, and those of the repositories inside it, would run is read before the repository becomes the
    /// window's, as one piece of work, so that no command runs in it meanwhile. When that cannot be read the
    /// repository is not opened. When it holds risks the user has not trusted, the notice comes first, and the
    /// repository becomes the window's only when the user trusts it (T1): Keep it off opens nothing, and no git
    /// command has run in it.
    /// </summary>
    private async Task OpenRepositoryAsync(string root)
    {
        if (!BeginWork("Working: opening…"))
        {
            return;
        }

        SetGitItems(false);
        UpdateStatusBar();
        var session = new GitSession(root, mark => TrustsNow(root, mark));
        TrustScan? scan = null;
        try
        {
            scan = await session.CheckAsync();
        }
        catch (GitNotFoundException)
        {
            // Said below, once the work is over.
        }
        finally
        {
            EndWork();
            if (!IsDisposed)
            {
                SetGitItems(true);
                UpdateStatusBar();
            }
        }

        if (IsDisposed)
        {
            return;
        }

        if (scan is null)
        {
            GitMissing();
            return;
        }

        if (!scan.Known)
        {
            if (_root is null)
            {
                SetNoRepository();
            }

            Log.Info("A repository was not opened: what it would run is not known");
            ShowNotice(Notice.Plain("Repository not opened", $"What {root} would run could not be read, so axGIT runs nothing in it. {scan.Problem}").ByItself());
            return;
        }

        if (!session.Trusted)
        {
            // T1: the notice first; the repository is the window's only once trusted.
            AskTrust(root, session, opening: true);
            return;
        }

        await BecomeAsync(root, session);
    }

    /// <summary>The repository the window's, with the session it was checked in, and its lists read: after a check that found it trusted, or after the user trusted it from the notice.</summary>
    private async Task BecomeAsync(string root, GitSession session)
    {
        _session = session;
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
        SetTitle(Opened.At(root));
        _settings.RememberRepository(root);
        SaveSettings();
        Log.Info($"Opened {root}");
        await RefreshAsync(announce: false, checkedNow: true);
    }

    /// <summary>
    /// AG-1.6b (the review's second pass of 2026-11-27): the check again, before the lists are read after every
    /// command and at F5. The check was one look when the repository opened, and the ground moves under it: a
    /// switch makes a settings file count that is included for one branch alone, a pull or a discard writes a
    /// file the settings include, Stage all makes a folder with a repository of its own an entry git then runs
    /// itself in, a switch brings another hook. So whether it is what the user trusted is worked out again from
    /// what is there now, and when it is not, the notice asks before anything runs (T1). False when nothing may
    /// run: git is missing, another command runs, the repository is not trusted now, or the check could not be
    /// made, and then the repository is closed, since nothing is known of it any more.
    /// </summary>
    private async Task<bool> CheckAgainAsync(string root)
    {
        if (_session is not { } session || !BeginWork("Working: checking…"))
        {
            return false;
        }

        SetGitItems(false);
        var checkedNow = false;
        try
        {
            await session.CheckAsync();
            checkedNow = true;
        }
        catch (GitNotFoundException)
        {
            // Said below, once the work is over.
        }
        finally
        {
            EndWork();
            if (!IsDisposed)
            {
                SetGitItems(true);
            }
        }

        if (IsDisposed || _root != root)
        {
            return false;
        }

        if (!checkedNow)
        {
            GitMissing();
            return false;
        }

        return AfterCheck(root, session);
    }

    /// <summary>
    /// What the window does with a check just made (AG-1.6b; after F5, and after every command that changes the
    /// repository, D8, and before every fetch, pull and push, T4): when what the repository would run is no longer
    /// known it is closed, said in a notice; when it holds risks the user has not trusted (other ones than he
    /// trusted, a hook or an included file that changed) the notice asks, and nothing runs until he answers (T1).
    /// False in both cases (the caller reads no list and says nothing more; <paramref name="failed"/>, a failed
    /// command's sentence, went into the notice).
    /// </summary>
    private bool AfterCheck(string root, GitSession session, string? failed = null)
    {
        if (IsDisposed || _root != root || !ReferenceEquals(_session, session))
        {
            return false;
        }

        if (!session.Scan.Known)
        {
            CloseRepository();
            Log.Info("The repository was closed: what it would run is no longer known");
            ShowNotice(Notice.Plain("Repository closed", $"What {root} would run could no longer be read, so axGIT runs nothing more in it and has closed it. {session.Scan.Problem}").ByItself());
            return false;
        }

        if (!session.Trusted)
        {
            // T1: nothing more runs in it until the user trusts it; Keep it off closes it.
            AskTrust(root, session, opening: false, failed);
            return false;
        }

        return true;
    }

    /// <summary>
    /// AG-1.6: whether the user trusts the repository with exactly this mark (the session asks at each check). A
    /// trust from before the marks (2026-11-16) is bound to what the repository runs now, and saved.
    /// </summary>
    private bool TrustsNow(string root, string mark)
    {
        if (!_settings.Trusts(root, mark))
        {
            return false;
        }

        if (_settings.Trust(root, mark))
        {
            SaveSettings();
        }

        return true;
    }

    /// <summary>
    /// AG-1.6: the notice for a repository whose last check found risks the user has not trusted (T1): what it
    /// holds, and the two answers. Nothing runs in it meanwhile. Keep it off, the default, closes it (as it opens,
    /// <paramref name="opening"/>, it is never opened); Trust this repository opens it, or reads its lists again.
    /// <paramref name="failed"/>: a failed command's sentence, said first in it, since a second notice would take
    /// this one's place.
    /// </summary>
    private void AskTrust(string root, GitSession session, bool opening, string? failed = null, bool changedWhileAsked = false)
    {
        var scan = session.Scan;
        var risks = scan.Risks;
        Log.Info($"Untrusted repository: {risks.Count} risks, {GitTrust.Programs(risks).Count} of them programs; nothing runs in it");
        // A trust is for what axGIT found when it was given; anything else is asked about again. The sentence
        // does not say the repository changed: after an update of axGIT the check may be the thing that did.
        var changed = changedWhileAsked ? "What axGIT finds in this repository changed while you were asked, so it is not trusted; here is what it holds now. "
            : _settings.WasTrusted(root) ? "What axGIT finds in this repository differs from what you trusted, so it is not trusted now. " : string.Empty;
        var programs = GitTrust.Programs(risks);
        var places = GitTrust.Places(risks);
        var notKnown = GitTrust.NotKnown(risks);
        var runs = programs.Count == 0 ? string.Empty : $"Its settings or hooks name programs git would run: {string.Join(", ", programs)}. ";
        // P5 (the review's fifth pass): each program setting with its whole value, written out as a command is, and
        // the limit of trust said as it is.
        var values = scan.Programs is not { Count: > 0 } named ? string.Empty
            : $"The programs as its settings write them: {string.Join("; ", named.Select(DisplayText.VisibleCommand))}. ";
        var limit = programs.Count == 0 ? string.Empty : GitTrust.TrustLimit + " ";
        var other = notKnown.Count == 0 ? string.Empty : $"It has settings axGIT does not know to be harmless: {string.Join(", ", notKnown)}. ";
        var elsewhere = places.Count == 0 ? string.Empty : $"Its files lead git to places axGIT does not read: {string.Join(", ", places)}. ";
        // T4: said once, here, since a repository with a program in its work tree is asked about by that setting.
        var inTree = scan.InTree is not { Count: > 0 } tree ? string.Empty
            : $"Its hooks folder or programs lie in its work tree, where a switch or a pull can change them while git runs ({string.Join(", ", tree)}): even trusted, axGIT stages, commits, switches, fetches, pulls and pushes without them, and you run those that need them in a terminal. ";
        var start = opening ? $"axGIT has run nothing in {root}. " : "axGIT runs nothing more in this repository until you choose. ";
        ShowNotice(new Notice(
            programs.Count > 0 ? "This repository runs programs of its own" : places.Count > 0 ? "This repository leads git elsewhere" : "This repository has settings axGIT does not know",
            $"{(failed is null ? string.Empty : failed + "\n\n")}{changed}{start}{runs}{values}{other}{elsewhere}{inTree}{limit}Trust it only if you know where it comes from: then git runs all of it as it would in a console. Keep it off closes the repository.",
        [
            new OverlayChoice("Keep it &off", () => KeepOff(root, session, opening), IsDefault: true, IsCancel: true),
            new OverlayChoice("&Trust this repository", () => TrustRepository(root, session, opening)),
        ]).ByItself());
    }

    /// <summary>Keep it off (T1): a repository being opened is not opened, one open is closed; either way no git command runs in it.</summary>
    private void KeepOff(string root, GitSession session, bool opening)
    {
        if (opening)
        {
            Announce("Not opened", true);
            return;
        }

        if (_root == root && ReferenceEquals(_session, session))
        {
            CloseRepository();
            Log.Info("The repository was closed: the user kept it off");
            Announce("Repository closed", true);
        }
    }

    /// <summary>
    /// Trust this repository (P3 of the review's fifth pass): what the notice showed is checked again, and the trust
    /// holds only when the repository is still what it showed; then it is kept with that mark, and the repository
    /// opened, or its lists read again. When the repository changed while the notice was open (a filter added), the
    /// notice comes again with what it holds now, and nothing has run.
    /// </summary>
    private void TrustRepository(string root, GitSession session, bool opening)
    {
        var mark = session.Scan.Mark;
        Do(async () =>
        {
            if (!BeginWork("Working: checking…"))
            {
                return;
            }

            SetGitItems(false);
            bool? trusted = null;
            try
            {
                trusted = await session.TrustAsync(mark);
            }
            catch (GitNotFoundException)
            {
                // Said below, once the work is over.
            }
            finally
            {
                EndWork();
                if (!IsDisposed)
                {
                    SetGitItems(true);
                }
            }

            if (IsDisposed || (!opening && (_root != root || !ReferenceEquals(_session, session))))
            {
                return;
            }

            if (trusted is null)
            {
                GitMissing();
                return;
            }

            if (!session.Scan.Known)
            {
                if (!opening)
                {
                    AfterCheck(root, session);
                    return;
                }

                Log.Info("A repository was not opened: what it would run is not known");
                ShowNotice(Notice.Plain("Repository not opened", $"What {root} would run could not be read, so axGIT runs nothing in it. {session.Scan.Problem}").ByItself());
                return;
            }

            if (trusted == false)
            {
                AskTrust(root, session, opening, changedWhileAsked: true);
                return;
            }

            _settings.Trust(root, session.Scan.Mark);
            SaveSettings();
            Announce("Trusted", true);
            if (opening)
            {
                await BecomeAsync(root, session);
            }
            else
            {
                await RefreshAsync(announce: false, checkedNow: true);
            }
        });
    }

    /// <summary>The repository open no longer the window's: no list, no title, the status bar says so.</summary>
    private void CloseRepository()
    {
        SetNoRepository();
        SetTitle(null);
        UpdateStatusBar();
    }

    private void SetNoRepository()
    {
        _root = null;
        _session = null;
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

    /// <summary>Repository → Show current repository in → File Explorer (no key; Ctrl+W until 2026-11-26): the folder opened in File Explorer, one of the system's own windows.</summary>
    private void ShowInExplorer()
    {
        if (_root is null)
        {
            Refuse("No repository");
            return;
        }

        OpenInExplorer(_root);
    }

    /// <summary>Repository → Show current repository in → axClaude, axTerminal, axFile (AG-1.4): a new window of that app on the repository's folder.</summary>
    private string? RepositoryOrSay()
    {
        if (_root is null)
        {
            Refuse("No repository");
        }

        return _root;
    }

    /// <summary>Changes → Open file in editor (Ctrl+E): the selected file in axJSON for .json, axDown otherwise (AG-1.4).</summary>
    private void OpenInEditor()
    {
        if (_root is null)
        {
            Refuse("No repository");
            return;
        }

        if (CurrentFile() is not { } file)
        {
            Refuse("No file selected");
            return;
        }

        var path = Path.Combine(_root, file.Path.Replace('/', Path.DirectorySeparatorChar));
        WindowMenu.Start(Dispatch.EditorOf(path), path);
    }
}
