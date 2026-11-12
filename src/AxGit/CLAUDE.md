# axGIT, the window

The WinForms side of axGIT: one list with three views, one read-only details text, a status bar, the bundle's notices. The pure code (git as a process, the parsers, the settings) is `src/AxGit.Core`, with its own `CLAUDE.md`. The spec is `docs/axGIT/SPEC.md` ("SPEC.md" below); the guide `docs/axGIT/user-guide.md` is embedded by `AxGit.csproj` for Help → User guide; the manual test plan is `docs/axGIT/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axGIT.ps1 C:\path\to\repo   # build AxKit and start axGIT on the repository that holds the path (run.ps1 -App git)
.\run-axGIT.ps1                   # the last repository opened
dotnet build AxKit.sln
dotnet test tests/AxGit.Tests/AxGit.Tests.csproj
```

## Where things are

- `AxGitApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run` with the one folder or file of the command line, the settings and the window; the log (`axGIT.log`), the crash handling, `--help` and `--version` are the host's.
- `GitForm`: the window (SPEC.md AG-2), in five partial files since 2026-10-29:
  - `GitForm.cs` (the controls, the menus, the hooks, the keys, the repository);
  - `GitForm.Run.cs` (running git);
  - `GitForm.Views.cs` (the views, the details, the status bar);
  - `GitForm.Commands.cs` (changes, history, branches);
  - `GitForm.Consent.cs` (bundle AX-11.5, AX-11.6: `ToServerAsync`, the consent before the first fetch, pull or push that reaches a server, by the remote's host through `GitRemote`, and the sign-in line the first time a window reaches each server; the consent before the first repository is in `StartAsync`, both through the base's `HasConsent`).
  - The menus and keys (AG-6.1, AG-6.2) as one command list (`docs/axKit/app-kit.md` AK-2) from `BundleKeys` and `AxGitKeys`:
    - each menu item and its key one `Add(menu, new Command(...))` (wrapped in `Git` when it runs git);
    - the keys with no item (Tab between the list and the details, find, Enter on a commit, and File Explorer, whose item the base's Show in submenu is made around) `AddKey`;
    - no `ProcessWindowKey` of its own.
    - the bar is the bundle's eight (`PlaceMenuBar`, AX-9.1, 2026-11-11): Actions with the staging, Commit, New branch, Fetch, Pull and Push; Navigation with the three views; Context (`_context`, a `ContextBarMenu`, AX-9.11) over three menus off the bar, the Changes view's line, the Log's commit and the Branches view's branch, opened by Shift+F10, the Applications key and a right click in the list (`OpenContext`).
  - The views, one `AxKit.Forms/ListViewState` each (the list's name, the items, their keys, whether read since last made stale, the selected key):
    - `LoadAsync` reads one from git, `ShowViewAsync` and `FillList` show it;
    - `RememberSelection` keeps the key before the list changes, `Stale` marks views to read again;
    - `InView` says "Changes view first: Ctrl+1" for a command of another view.
  - The commands, each one `RunGitAsync` call with the argument list the Core's records give (`StageToggleAsync`, `ShowDiffAsync`, `Discard`, `Commit`, `ShowCommitAsync`, `SwitchBranchAsync`, `NewBranch`, `FetchAsync`, `PullAsync`, `PushAsync`); `ShowDetails`.
  - The notice flow, the fonts, the window placement, find and the update flow are the window base's (`AxKit.Forms/AxKitForm`, bundle AX-6.5), which `GitForm` fills in with its hooks:
    - the list and the details hidden for a notice;
    - the list as the focus target (the details keep the focus across a notice by the base's rule);
    - the font on both;
    - `git` plus the repository as the restart arguments;
    - Ctrl+S's "Nothing to save here. Space stages, Ctrl+Enter commits" (`SaveCommand`).
- `AxGitKeys`: the window's keys (SPEC.md AG-6.2; bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; the list is named with its view and count; nothing speaks on a plain arrow key; a command's result is one announcement (AG-7), on the list, or on the notice while one shows.
2. git never runs on the UI thread: `RunGitAsync` awaits the Core's process on a task, one command at a time (`_busy`), the status bar saying what runs and the git menu items disabled meanwhile.
3. No second window (bundle AX-7.2): questions and errors are notices; the folder picker and File Explorer are the system's own windows.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas already learned

- Enter in a `ListBox` reaches `ProcessCmdKey` as `Keys.Return`; its commands (Show diff, the commit, Switch to branch, one per view) carry `When = () => _list.Focused && _view == ...`, so that Enter in the details stays the control's own, and a command run keeps the list from dinging.
- The base's `Do(Func<Task>)` wraps every command: an exception out of an awaited task would otherwise be unobserved; there it becomes an error notice.
- Changing the list's `AccessibleName` while it has the focus is silent for NVDA; a view switch therefore announces the name and the focused item itself (AG-2.2).
- The focus moves to the details after Enter on a file or a commit (AG-D5); the announcement is raised after the focus change and without interrupting, so that NVDA's reading of the control is not cut off.
