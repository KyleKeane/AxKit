# axKit — Bundle Specification

Describes axKit, the bundle the axClaude repository became on 2026-10-02 (plan: `docs/axkit/plan.md`). Every
requirement is built unless its text says otherwise; this document is kept in step with the code (a behaviour change
updates it in the same commit), and what each release brought is section 7. It covers only what crosses apps. Each
app has its own specification, `docs/<app>/SPEC.md` (section 3). Requirements here are numbered `AX-n.m` and
decisions `B-n`, so that they never clash with an app's `FR-n.m` and `D-n`.

## 1. Summary

axKit is one installed Windows program, `AxKit.exe`, that holds several accessible apps for a blind developer who uses
the NVDA screen reader. Each app is a separate window that starts on its own, has its own Start menu entry, its place in
File Explorer's Open with axKit submenu and its own console command, and all of them share one installer, one updater, one
version number and one release workflow. The first apps were **axClaude**, the front end for the Claude Code CLI, and
**axDown**, a barebones editor for Markdown and text files; **axJSON**, **axGIT**, **axTerminal**, **axFile**,
**axMedia**, **axPreview**, **axStart**, **axProcess**, **axTask**, **axPass**, **axCalc** and **axClip** followed (section 3). Later apps are planned (a password manager, a vault
manager: `docs/axkit/plan.md`) and follow the same pattern without changes to the bundle.

## 2. Goals and non-goals

- **G1 One download, one install, one update.** The bundle is a single self-contained executable with the .NET
  runtime inside it, so the zip stays one file of about 70 MB whatever the number of apps.
- **G2 Apps that start apart.** Windows+`axc` and Windows+`axd` each find one app; a folder opens in axClaude, a file
  in axDown; each app has its own command word. Nothing about the bundle is in the user's way.
- **G3 Modular code and documents.** A change to one app needs only that app's folders, spec and notes in context.
  This is what makes the repository workable for smaller AI models and cheap to extend.
- **G4 Plain code.** Zero third-party runtime dependencies, as axClaude always had, and no coding paradigm that a
  smaller model would struggle to edit: plain classes, static methods, one obvious way to do each thing.
- **G5 Nothing breaks.** axClaude behaves as released; every commit builds and passes the tests, and a change to a window is checked with NVDA before it is released.
- **G6 Local first.** Every standalone app has its full utility with the network off. An access layer has the utility
  of its program: axGIT's and axPass's work on the disk, axTerminal's is the commands typed, and axClaude's needs
  Claude Code's service, so axClaude is the one app that needs the network. axKit's own code makes network requests in
  two places, both over HTTPS to GitHub: the updater (AX-3), the update check, a setting that can be off, and the
  download the user chooses; and the report sender (AX-12, docs/axkit/reports.md), only when the user sends a report
  (F4), the report saved on the computer first. A network failure is never more than a missing update or a report that
  waits for the next. An access layer reaches the network only
  through the program it runs (AX-11.11). Any AI help in the other apps is one shared, optional service (B-15), never
  a dependency of a feature that works without it.

Non-goals: a plugin system or loading apps from separate files (apps are compiled in); more than one window per
process; an installer that needs administrator rights; setting Windows default programs silently (Windows does not
allow it, see B-9).

## 3. The apps

The apps are named ax and a word, the ax in lower case and the word as it is written: axClaude, axDown, axJSON, axGIT, axTerminal, axFile, axMedia, axPreview, axStart, axProcess, axTask, axPass, axCalc, axClip (2026-10-23, the user's rule; until then AxClaude and the like). A screen reader then says the name as ax and a word it knows, and an acronym keeps its capitals. Nothing depends on the case: the Start menu, the command palette, axStart and the console commands (xclaude) find a name typed in any case, and the code that tells the apps apart by name (a window's title, the Recently closed list, the hand-over) ignores case, so an entry saved under the old spelling still opens. The code keeps the C# spelling of its projects, folders and classes (`src/AxClaude`, `AxClaudeApp`), which no screen reader speaks; `Dispatch.App` holds both, `Name` and `Project`.

- **axClaude** (built): `AxKit.exe claude [<folder>] [options]`, Start entry `axKit axClaude`, Open with axKit on
  folders, command `axclaude`. Specification `docs/axclaude/SPEC.md`.
- **axDown**: `AxKit.exe down [<file>]`, Start entry `axKit axDown`,
  Open with axKit on files and Open with for `.md`, `.markdown`, `.txt`, command `axdown`. Specification
  `docs/axdown/SPEC.md`, guide `docs/axdown/user-guide.md`, test plan `docs/axdown/nvda-test-plan.md`.
- **axJSON**: `AxKit.exe json [<file>]`, Start entry `axKit axJSON`, Open with axKit on `.json` files and Open with
  for `.json`, command `axjson`. The text editor shell axDown is, with JSON's navigation keys and a Format command.
  Specification `docs/axjson/SPEC.md`, guide `docs/axjson/user-guide.md`, test plan `docs/axjson/nvda-test-plan.md`.
- **axGIT** (first draft, 2026-10-18): `AxKit.exe git [<folder or file>]`, Start entry `axKit axGIT`, Open with axKit
  on folders, command `axgit`. A git manager for one repository, a window per repository, opened from the other
  windows' "Show current … in ▸" submenus on the repository of the folder or file in hand. Specification
  `docs/axgit/SPEC.md`, guide `docs/axgit/user-guide.md`, test plan `docs/axgit/nvda-test-plan.md`.
- **axTerminal** (first draft, 2026-10-18): `AxKit.exe terminal [<folder>]`, Start entry `axKit axTerminal`, Open with
  axKit on folders, command `axterminal`. A shell (PowerShell, or cmd) in axClaude's window: the output as a
  transcript with exchange blocks, the shell's integration marks for Done and Failed. Specification
  `docs/axterminal/SPEC.md`, guide `docs/axterminal/user-guide.md`, test plan `docs/axterminal/nvda-test-plan.md`.
- **axFile** (first draft, 2026-10-19): `AxKit.exe file [<folder or file>]`, Start entry `axKit axFile`, Open with axKit
  on folders and on files, command `axfile`. A file manager for one folder as one list, Enter the verb (a folder
  here, a file in the user's default program, as in File Explorer), the file operations with Undo and an Actions
  list of every operation; its player and preview became axMedia and axPreview on 2026-10-22
  (`docs/axkit/axfile-split.md`). Specification `docs/axfile/SPEC.md`, guide `docs/axfile/user-guide.md`, test plan
  `docs/axfile/nvda-test-plan.md`.
- **axMedia** (2026-10-22): `AxKit.exe media [<file or folder>...] [--add] [--new]`, Start entry `axKit axMedia`, Open
  with axKit on sound and video files and Open with for their kinds, command `axmedia`. A player with one queue, the
  player axFile carried until the split (`docs/axkit/axfile-split.md`); a start with paths hands them to the open
  axMedia window (`AppMessage`). Specification `docs/axmedia/SPEC.md`, guide `docs/axmedia/user-guide.md`, test plan
  `docs/axmedia/nvda-test-plan.md`.
- **axPreview** (2026-10-22): `AxKit.exe preview [<file>] [--new]`, Start entry `axKit axPreview`, Open with axKit on
  every file and Open with for every file type (the text kinds alone up to 3.7.1), command `axpreview`. A read-only viewer for
  one text file: the text editor shell in its read-only mode, with axDown's navigation keys; a start with a file
  hands it to the open axPreview window. Specification `docs/axpreview/SPEC.md`, guide `docs/axpreview/user-guide.md`,
  test plan `docs/axpreview/nvda-test-plan.md`.
- **axStart** (2026-10-22, 3.4.0): `AxKit.exe start [--hidden]`, Start entry `axKit axStart`, command
  `axstart`, no File Explorer entry. A program launcher: a background process from sign-in (the Run key) with a
  global hotkey, Ctrl+Shift+Space unless the user chose another, that shows one search field over the Start menu's
  programs, Down and Up through the matches with the caret kept in the field. Specification `docs/axstart/SPEC.md`,
  guide `docs/axstart/user-guide.md`, test plan `docs/axstart/nvda-test-plan.md`.
- **axProcess** (2026-10-23, branch `axprocess`): `AxKit.exe process`, Start entry `axKit axProcess`, command `axprocess`,
  no File Explorer entry. What runs on the computer as one list with three views (processes, services, startup
  programs), read on F5 and never on a timer; a process ended after a question, a service started or stopped.
  Specification `docs/axprocess/SPEC.md`, guide `docs/axprocess/user-guide.md`, test plan
  `docs/axprocess/nvda-test-plan.md`.
- **axTask** (2026-10-24, branch `axtask`): `AxKit.exe task [<folder>]`, Start entry `axKit axTask`, command `axtask`,
  folders in File Explorer's Open with axKit submenu. Tasks and lists as a folder of JSON files, one per item, in a
  navigator like axFile's with a detail pane of the item's typed fields; the item apps' shared shape is
  `docs/axkit/item-apps.md`. Specification `docs/axtask/SPEC.md`, guide `docs/axtask/user-guide.md`, test plan
  `docs/axtask/nvda-test-plan.md`.
- **axPass** (2026-10-24, branch `axtask`): `AxKit.exe pass [<database>]`, Start entry `axKit axPass`, command `axpass`,
  `.kdbx` files in File Explorer's Open with axKit submenu and Open with. A KeePass database in axTask's two panes,
  through KeePassXC's command-line tool, which must be installed. Specification `docs/axpass/SPEC.md`, guide
  `docs/axpass/user-guide.md`, test plan `docs/axpass/nvda-test-plan.md`.
- **axCalc** (2026-10-23, branch `axcalc`): `AxKit.exe calc`, Start entry `axKit axCalc`, command `axcalc`, no File
  Explorer entry. A calculator as one expression field and a history list: Enter says the answer, the history keeps
  every answer to read back, copy and recall, and Ctrl+I says an answer in hexadecimal, binary and octal.
  Specification `docs/axcalc/SPEC.md`, guide `docs/axcalc/user-guide.md`, test plan `docs/axcalc/nvda-test-plan.md`.
- **axClip** (2026-10-23, branch `axclip`): `AxKit.exe clip [--hidden]`, Start entry `axKit axClip`, command `axclip`, no File
  Explorer entry. A clipboard history behind a hotkey (Windows+Alt+V unless the user chose another): a background
  process from sign-in, as axStart, on the bundle's `HotkeyForm`, with one search field over the text of every copy;
  Enter pastes into the program that was in front. Specification `docs/axclip/SPEC.md`, guide
  `docs/axclip/user-guide.md`, test plan `docs/axclip/nvda-test-plan.md`.

## 4. Functional requirements

### AX-1 Launch and dispatch

- AX-1.1 The first argument may be a verb naming an app: `claude`, `down`, `json`, `git`, `terminal`, `file`, `media`, `preview`, `start`, `process`, `task`, `pass`, `calc`, `clip` (the one list is `Dispatch.Apps`). The rest of the arguments go to that app
  unchanged, so `AxKit.exe claude C:\src --record x.vt` is what `AxClaude.exe C:\src --record x.vt` was.
- AX-1.2 Without a verb, a first argument that is an existing folder goes to axClaude and an existing file to axDown,
  or to axJSON when its extension is `.json`, to axPass when it is `.kdbx`, or to axMedia when it is a sound or video
  file (`MediaFiles.IsMedia`),
  with the remaining arguments. A path that does not exist is reported with the usage text (a message box, since no
  window exists yet, as `--help` does today).
- AX-1.3 Without any argument, `AxKit.exe` starts axClaude on the current folder, which is what `axclaude` does today
  (B-8). axClaude keeps its own rule for a current folder that is the program's own folder.
- AX-1.4 `--help` and `--version` answer for the bundle and list the verbs; `AxKit.exe claude --help` is the app's own
  usage text. Two verbs are not apps: `mcp`, the AI tools' broker an MCP client starts, with no window
  (`docs/axkit/mcp.md` MC-1.1); and `apps`, with no window either, which writes `Dispatch.Apps` as JSON to standard
  output (each app's name, verb, project, Start menu description, File Explorer submenu entries, file types and Open
  with types), which `install.ps1` builds its entries from (AX-2.2, AX-2.3; `docs/axkit/app-kit.md` AK-4.1). The
  installation keeps that listing as `apps.json`, which an uninstall reads when the program cannot be run.
- AX-1.5 The dispatch is pure code in `AxKit.Core` (`Dispatch`): arguments in, app name and arguments out. It is unit
  tested and has no knowledge of the apps beyond their names.
- AX-1.6 Each process sets a Windows application id before its window opens (`AxKit.AxClaude`, `AxKit.AxDown`, `AxKit.AxJSON`, `AxKit.AxGit`, `AxKit.AxTerminal`, `AxKit.AxFile`, `AxKit.AxMedia`, `AxKit.AxPreview`, `AxKit.AxStart`, `AxKit.AxProcess`, `AxKit.AxTask`, `AxKit.AxPass`, `AxKit.AxCalc`, `AxKit.AxClip`), so the
  taskbar shows the apps apart and Win+number keys reach the right one.
- AX-1.7 An app is a class library with one entry point, `static void Run(string[] args)` on a class named
  `<App>App`, plus a `Usage` text. `AxKit`'s `Program` only dispatches; the entry point is one call into
  `AxKit.Forms/AppHost.Run`, which owns the log, the crash handling (an exception in a UI handler is logged and shown
  in the window as a notice, or in a message box when no window exists yet), the start and exit lines in the log,
  `--help` and `--version`, and runs the window the app makes from the rest of its arguments (`AppHost.OnePath`
  for an app that takes one file or folder, `AppHost.BadCommandLine` for the rest).
- AX-1.8 In the Store's copy (AX-13), a process started from one of the package's entries takes the entry's verb when
  the command line carries none (`Dispatch.WithApplicationVerb` with `PackageIdentity.ApplicationId`, before `Decide`).

### AX-2 Installation

- AX-2.1 `install.ps1` installs for the current user into `%LOCALAPPDATA%\Programs\AxKit` with no administrator
  rights, from the extracted zip or from `publish\win-x64`; `install.cmd` runs it with the execution policy bypassed
  and, downloaded alone, fetches the latest release first. `-Uninstall` removes everything of axKit and keeps the
  settings and the logs. This is axClaude's installer today (`docs/axclaude/SPEC.md` FR-8.7, D18) with the names changed.
- AX-2.2 Start menu: one shortcut per app, named `axKit <App>`, each starting `AxKit.exe <verb>` with the app's icon.
  Windows search matches the start of each word, so `axc`, `axd` and `axj` find them and `axkit` lists them (checked).
- AX-2.3 File Explorer: one submenu, **Open with axKit**, in the right-click menu of folders, of a folder's background
  and of files, listing the apps that open what was right-clicked, by name, in the order of their names. It is a
  static cascading menu (`HKCU\Software\Classes\Directory\shell\AxKit`, `Directory\Background\shell\AxKit` and
  `*\shell\AxKit`, each with `MUIVerb`, an empty `SubCommands` and the apps as verbs under its `shell` subkey, running
  `AxKit.exe <verb> "%1"`, `%V` on the background). An app that opens only some file types says so with `AppliesTo`
  (`System.FileExtension:=".json"`), so the registry shows an app only where it belongs and no app checks the path
  it is given. The table is held once, in `install.ps1`'s `$apps`, one block per app; an app that is also a program
  for its file types, under Windows' own Open with and in Settings, Default apps (where the user, never the
  installer, makes it the default, B-9), says so in the same block:

  | App | Folders, and a folder's background | Files | Program for (Open with, Default apps) |
  | --- | --- | --- | --- |
  | axClaude | yes | | |
  | axDown | | every file | `.md`, `.markdown`, `.txt` (`AxKit.AxDown`) |
  | axJSON | | `.json` | `.json` (`AxKit.AxJSON`) |
  | axGIT | yes | | |
  | axTerminal | yes | | |
  | axFile | yes | every file (its folder, the file selected) | |
  | axMedia | | the sound and video kinds (`MediaFiles.Extensions`, listed again in the block) | the same kinds (`AxKit.AxMedia`) |
  | axPreview | | every file | every file (`*`, `AxKit.axPreview`; the text types alone up to 3.7.1) |
  | axStart | | | |
  | axProcess | | | |
  | axTask | folders | | |
  | axPass | | `.kdbx` | `.kdbx` (`AxKit.AxPass`) |
  | axCalc | | | |
  | axClip | | | |

  Windows' Open with lists one program per program file, under the name of the last ProgID registered for it
  (measured 2026-10-24: axPreview alone was offered for `.md`, `.txt` and `.json`). So each app of the last column
  has a hard link of `AxKit.exe` under its own name in the installation folder (`axDown.exe`, `axJSON.exe`,
  `axPreview.exe`, `axMedia.exe`, `axPass.exe`), made again on every install, and its ProgID runs
  `<app>.exe <verb> "%1"`. Started under any name but `AxKit.exe`, the program starts the `AxKit.exe` beside it with
  the same arguments, lets it take the foreground and ends, so every window runs as `AxKit.exe` (`Program.cs`). A
  link still running at an update is renamed aside as `<app>.old.<time>.exe`, as the program file is (AX-2.8). A
  type's `OpenWithProgids` key is made only when it is missing and axKit's value is added to it: `New-Item -Force` on
  an existing key empties it, which up to 3.7.1 removed the other programs' values of the user's own registry for
  these types, and axDown's when axPreview's came next.

  On Windows 11 the submenu is in the full menu (Shift+F10, or Show more options), as every program's own entries
  are. Up to 2.13.0 each app had an entry of its own (`Open in axClaude` and the like); the installer removes them.
- AX-2.4 Console commands: `axkit.cmd` and one `<app>.cmd` per app (`axclaude.cmd`, `axdown.cmd`, `axjson.cmd`,
  `axgit.cmd`, `axterminal.cmd`, `axfile.cmd`, `axmedia.cmd`, `axpreview.cmd`, `axstart.cmd`, `axprocess.cmd`, `axtask.cmd`, `axpass.cmd`, `axcalc.cmd`, `axclip.cmd`) in `%USERPROFILE%\.local\bin`, one line each, passing their arguments to `AxKit.exe`
  and `AxKit.exe <verb>`. They work from cmd and from PowerShell.
- AX-2.5 Settings, Apps lists one entry, `axKit`, with the version, the publisher and an Uninstall that runs
  `install.cmd -Uninstall`.
- AX-2.6 Installing over an installation of an earlier name removes it completely first: Axit, the bundle's name up to
  4.0.0 (B-16): its folder `%LOCALAPPDATA%\Programs\Axit`, its Start menu entries (`Axit <App>` by title, by name or by
  project, as the versions made them), `axit.cmd`, the `Open with Axit` keys, the ProgIDs `Axit.<App>` with their Open
  with values, the sign-in values `Axit axStart` and `Axit axClip`, and its Apps entry; and axClaude 1.x (any version):
  its folder, Start entry, `axclaude.cmd`, registry keys and Apps entry. The installer says what it found, and stops an
  `Axit.exe` running in the background as it stops `AxKit.exe`. Axit's settings and logs move on the first start
  (AX-5.4); the axClaude settings file is left where it is, and the app copies it on its first start (AX-5.2). An
  installed Axit up to 4.0.0 cannot take the update to axKit itself: its updater accepts a release's files only under
  the old repository address, which GitHub rewrites to the new one once the repository is renamed (B-16), so its update
  notice shows these release notes and its download fails with "no Windows zip attached"; the way over is
  `install.cmd` from the release page, with the Axit windows closed.
- AX-2.7 The hand-over parameters stay: `install.ps1 -WaitForProcess <pid> -Start <folder> [-ContinueConversation]
  -LogFile <file>` installs after that process ends and starts axClaude on the folder, as the 1.7.0 updater expects.
  `-StartFile <file>` starts axDown on the file the same way; nothing is renamed. Copies up to 2.2.0 update
  this way; from 2.3.0 the apps use AX-2.8.
- AX-2.8 In place: `install.ps1 -InPlace -LogFile <file>` installs while the app keeps running. The new files are
  written over the old ones, and what earlier versions left goes, as on a fresh install: their File Explorer
  entries, axClaude 1.x's folder, Apps entry and Start menu entry, and files in the folder this version does not ship
  (from 3.0.0; before, nothing was removed in place); the running `AxKit.exe` is first renamed `AxKit.old.exe` (a running
  program file can be renamed but not overwritten or deleted), and `AxKit.exe` deletes any `AxKit.old*.exe` next to it
  when it starts. The shortcuts, menus, shims and the Apps entry are written again, so the version shown in Settings,
  Apps is the new one. The app, not the installer, starts the new version afterwards (AX-3.2).
- AX-2.9 **The programs an app needs and Windows lacks** (the user's ask, 2026-10-24): Git (axGIT, and Git Bash in
  axTerminal), KeePassXC (axPass), Claude Code (axClaude) and PowerShell 7 (axTerminal). axKit's installer installs
  none of them, so that it stays a per-user install without an administrator prompt and installs nothing nobody asked
  for. Each app checks for its program and, when it is missing, its notice says so and offers **Install it now**
  (`AxKit.Core.ProgramInstall`, `AxKitForm.InstallProgram`): the maker's own package through winget by its exact id
  (`Git.Git`, `KeePassXCTeam.KeePassXC`, `Microsoft.PowerShell`), or the maker's own installer (Claude Code's, for the
  user only), never a copy of axKit's; every question answered, silent, no window. The notice says beforehand whether
  Windows will ask for an administrator's yes. When it ends the PATH is read again from Windows and the app checks
  for the program itself: found, "Git is installed" and the app goes on (axGIT opens the repository, axPass asks for
  the master password, axClaude starts Claude, axTerminal switches to the shell); not found, a notice with the
  installer's exit code and last line, Try again and the download page. Without winget (an older Windows 10) the
  download page opens instead. Only on the user's command, never on a timer or at start (B-15); each install is
  logged. Removing such a program is Settings, Apps, as for any program. A shell that is not installed is not offered
  in axTerminal (AT-1.2a). winget is the one Windows puts in the user's WindowsApps folder, never one on the PATH,
  and installs from its own source (`--source winget`, security audit S-10); Claude Code's installer runs in
  Windows PowerShell by its full path. One install at a time in a window: a second Install it now, while one runs or
  after the window stopped waiting for one (it is never killed), says so. Install it now is never the notice's
  default button: Enter is Close, as the key line says (the first default of a notice is its Enter, OverlayPanel).
  The end closes only its own progress notice; a failure while the user is in another notice is announced, not shown
  over it. The installer's output is read under the same deadline, so a helper left running cannot hold it.
- AX-2.10 The Microsoft Store's copy installs nothing of the above itself: its package declares the Start menu entries,
  the console aliases, the file types and the startup tasks, and the Store installs, updates and removes it (AX-13).

### AX-3 Updates

- AX-3.1 One version for the bundle, `<Version>` in `src/AxKit/AxKit.csproj`. A newer GitHub release is offered as
  axClaude does (`docs/axclaude/SPEC.md` FR-1.10, D25): asked once at startup when the setting is on, a notice with
  the release notes and four buttons: I accept all terms, install and restart (Enter), I accept all terms, install and close, Open release page, Later
  (Escape). A development build (Debug, as `run.ps1` makes it) does not ask at startup, since its version is whatever
  the working tree said when it was last built; Check for updates still asks. Every window's axKit menu ends with Check for updates (axKit <version>), always, naming the version in use
  and asking GitHub again every time, Update to axKit <version> below it while a newer release is known, and About, the
  bundle's About in sections (AX-9.9, `AxKit.Core/BundleAbout`), the same in every window. Every text of the update
  flow names axKit, never an app: the version is the bundle's.
- AX-3.2 Either update runs behind one notice with one progress bar (`Notice.Progress`; NVDA reports a progress bar
  by its own settings, so nothing is spoken about the progress itself): the zip is downloaded into
  `%LOCALAPPDATA%\AxKit\updates\<version>` (the bar to 80 percent; Cancel keeps the installed version), then the
  downloaded version's `install.ps1 -InPlace` runs while the app keeps running (AX-2.8; "Downloaded. Installing";
  the installer's lines move the bar to 100; Cancel no longer applies and says so), then the window closes
  ("Installed. Restarting" or "Installed. Closing") and, after I accept all terms, install and restart, starts the installed program file
  itself with what it had open (axClaude: the folder, and `-- --continue` when a message was sent; axDown: the file,
  or nothing). A failed installer leaves the running version in place and shows its output in a notice. A download
  counts as complete when it holds `install.ps1` and a program file. Cancel counts until the download is unpacked, a
  download that is not a zip is a failed update like any other, and an installed program file that cannot be
  started as the window closes is logged (`StartInstalled`), the window closing all the same; an installer that
  cannot be started (PowerShell blocked by a policy) or an update that passes ten minutes is a failed update like
  any other, a notice and the running version kept (2026-10-23: both escaped as a crash notice over the progress).
  The window's close for an installed update is a real close in every window, the one that otherwise hides on
  closing included (axStart, `ClosingForUpdate`). Each app's close
  question after I accept all terms, install and restart (axClaude's Keep working, axDown's Cancel in the save question) keeps the window
  on the old program file and says "Update installed. It starts with the next axClaude" (or axDown).
- AX-3.6 **A failed update's notice** (2026-10-24, the user's ask after 3.7.1's update failed on one machine with
  "the list of apps could not be read" and nothing more): what failed, then the details to send (the versions,
  Windows' version, and the installer's output or the error with its kind), with **Copy details** (D; the details on
  the clipboard, "Copied the details", the notice staying), Open release page and Close. The details are generic:
  the account's folder in every path, the account name and any e-mail address are replaced by `<user>`, `<name>` and
  `<email>` (`Updater.FailureDetails` over `Redaction`, tested); the app's log and `update.log` keep the whole text on
  the machine. The installer says where it failed: its last error names the line of `install.ps1` and the
  statement, and its first line names the version installed, in place or not, and Windows' and PowerShell's versions;
  when the new program answers its list of apps (`AxKit.exe apps`) wrongly, it says why and reads the zip's `apps.json`; when
  Windows will not start the new program at all (3.7.4 on one machine: Smart App Control, "An Application Control policy has
  blocked this file"), it stops before anything is changed, the running version kept, and says so; an uninstall reads
  `apps.json` then. A background app that cannot be started again after an install is said, not a failure.
- AX-3.3 The 1.7.0 bridge (B-6): axClaude 1.7.0 accepts a download whose program file is not `AxClaude.exe`, so an
  installed axClaude updates into axKit through Help, Update now. The first axKit release is 2.0.0.
- AX-3.4 The update check and the version comparison are pure code in `AxKit.Core/Updates/UpdateCheck`, unit tested;
  the download and the hand-over are `AxKit.Core/Updates/Updater`. **Signed releases** (2026-10-24, the user's
  decision; security audit S-27 to S-31): every release's zip is signed with the release key, an ECDSA P-256 key that
  lives only on the maintainer's machine, encrypted for its Windows account (`tools/release-key.ps1`), never on
  GitHub. The release workflow leaves a draft (a build job that can only read, then a job that uploads and attests
  the build's provenance); `release.ps1` then runs `tools/sign-release.ps1`, which checks the provenance, signs the
  zip's SHA-256, uploads `<zip>.sig` and publishes. The updater takes assets only from this repository's releases
  address, downloads afresh every time, and extracts nothing unless the zip matches GitHub's recorded checksum and its
  signature checks against the public half in `AxKit.Core/Updates/ReleaseSigning`; `install.cmd` alone does the same
  check in Windows PowerShell, into a folder of a random name. A release without a signature is never installed by
  the apps. Losing the key means one release installed by hand with a new key's public half in it.
  **Signed for Windows** (Azure Artifact Signing, 2026-11-06; `docs/axkit/code-signing.md`): the release workflow has
  `AxKit.exe` signed with an Authenticode signature through Microsoft's signing service, with a certificate Microsoft
  issues for each signing in the maintainer's verified name, which Windows' Smart App Control accepts: a job of its
  own signs in to Azure with the run's OpenID Connect token as an identity that holds the signer role on the signing
  account and nothing else (no secret is stored anywhere; the identity's ids are the `release` environment's secrets,
  which only `main` and `v*` tags can use), signs, and zips nothing unless Windows finds the signature valid. The
  repository variable `AZURE_CERTIFICATE_PROFILE` names the certificate profile and is the switch: a release made with
  it unset says in its notes that the program is unsigned (`tests/AxKit.Tests/ReleaseWorkflowTests.cs` keeps the
  order, the switch and the notice). The release key signs the zip as before.
- AX-3.7 The Store's copy has no updater: the Microsoft Store installs its updates, signed with Microsoft's
  certificate, and Check for updates there says where the copy stands and opens the Store page (AX-13.3).

### AX-4 Releases

- AX-4.1 `release.ps1 <version>` checks that `CHANGELOG.md` has a `## <version>` section, fetches origin and stops
  when `origin/main` has commits that `main` does not (nothing is released over work pushed from elsewhere; skipped
  with `-NoPush`, which pushes nothing and may run offline), runs
  every test project in Release through `test.ps1` (3.3.0 was tagged and never published because a test passed in
  Debug and failed in Release), sets `<Version>`, commits `Release <version>`, tags `v<version>` and pushes. `.github/workflows/release.yml` checks the
  tag against the version, tests, runs `publish.ps1 -NoInstall` and creates the GitHub release `axKit <version>` with
  `AxKit-<version>-win-x64.zip` and `install.cmd` attached and the CHANGELOG section as notes, as a draft that is
  published once the files are up (since 2026-10-21: the update check reads the latest published release, and one
  made while the zip was still uploading had a window download a half-written zip, "The installer failed").
- AX-4.2 `publish.ps1` publishes `src/AxKit` self-contained, single file, into `publish\win-x64` with `install.cmd`,
  `install.ps1`, `LICENSE`, `NOTICE.md`, `PRIVACY.md`, `THIRD-PARTY-NOTICES.md` and `docs/axkit/README.md` as `README.md`,
  and zips it.
- AX-4.3 One `CHANGELOG.md`; every bullet starts with the app it concerns (`axClaude:`, `axDown:`, `axKit:` for the
  bundle itself, `For developers:` for the repository, the scripts and the tests); every released section's heading
  carries its date.
- AX-4.4 `build.yml` runs the tests of the whole solution in Release on every push to any branch and on every pull
  request, so that a branch fails before it is merged rather than at its release (since 2026-10-22; it ran on `main`
  alone). `test.ps1` runs the same tests locally (every `tests/*.Tests.csproj`, Release, one by one so that a running
  axKit's `bin\Debug` is never written); `release.ps1` and `run.ps1 -Test` call it.
- AX-4.5 The repository's own pages (2026-10-22): `README.md` (what axKit is, the apps, install, build, test, release,
  where things are), `CONTRIBUTING.md` (how a change is made: spec first, one app's folders, tests, the NVDA plan and
  its record, the changelog, the licence of contributions), `SECURITY.md` (private reports through GitHub's security
  advisories), and since 2026-10-25 `PRIVACY.md` (every network contact and outside program, also in the zip and About),
  `GOVERNANCE.md` (the roles, how releases are made, the maintainer's commitments), `CODE_OF_CONDUCT.md` and
  `THIRD-PARTY-NOTICES.md` (the .NET runtime and Windows Forms in the program, under MIT), since 2026-10-29
  `NOTICE.md` (the licence's section 7 terms, the disclaimer and the risks app by app; `LICENSE` is the GNU AGPL 3 only,
  and `CONTRIBUTING.md` holds the contributor licence), with the README's code signing
  policy linked from every release page (`docs/axkit/code-signing.md`); issue forms in
  `.github/ISSUE_TEMPLATE` (a bug asks for the app, the versions and the test plan step), a pull request checklist,
  and Dependabot (`.github/dependabot.yml`) keeping the workflow actions and the test packages current, monthly; the
  apps themselves have no packages (G4, B-10).

### AX-5 Settings, logs and paths

- AX-5.1 Each app has its own settings file, `%APPDATA%\AxKit\<app>.json` (`axclaude.json`, `axdown.json`), and its own
  log, `%LOCALAPPDATA%\AxKit\logs\<app>.log`, each line with the time, the level and the process id in brackets, so
  that two windows of one app writing one log can be told apart (2026-10-23). The updater's log is `%LOCALAPPDATA%\AxKit\logs\update.log`. The bundle
  has one file of its own, `%APPDATA%\AxKit\recently-closed.json`, the Recently closed list of the axKit menu (AX-9.4).
- AX-5.2 On its first start under axKit, axClaude copies `%APPDATA%\AxClaude\settings.json` to `axclaude.json` when the
  new file does not exist; the old file is left alone.
- AX-5.3 The load-and-save pattern of a settings file (JSON, a missing or unreadable file falls back to the defaults
  and the error is reported once) is one piece of shared code, `AxKit.Core/SettingsFile`; each app's settings class is
  its own plain record of properties. A file that is not valid JSON (a typo after Options → Developer → Open settings file) is
  copied to `<app>.json.bad` before the defaults are used, and the error names the copy: the app saves its settings
  soon after loading, which would otherwise write the defaults over every other setting in the file.
- AX-5.4 The first start after the rename (B-16, 5.0.0) moves what Axit, the bundle's name up to 4.0.0, kept in
  `%APPDATA%\Axit` and `%LOCALAPPDATA%\Axit` into the AxKit folders (`AppPaths.TakeOverOldFolders`, called by `Program`
  before anything reads a setting), entry by entry: a file the new folder lacks is moved, one it has stays, a subfolder
  is taken over the same way, and the old folder goes once it is empty, so that a start that could not move everything
  (a file the old program still held) finishes the next time. The settings, the recently closed list, the consents,
  the AI tools' pairings and their key, the logs and the audit log all come along. Never in a test run. Tested in
  `AppPathsTests`.
- AX-5.5 In the Store's copy the same paths are Windows' to redirect: a new file under AppData lands in the package's
  private folder and goes with the package when it is uninstalled, an existing one is modified in place (AX-13.5).

### AX-6 Shared pieces

- AX-6.1 `AxKit.Core` (pure, unit tested): `Dispatch` (with `Apps`, the name and verb of each app), `BundleInfo`, `AppPaths`, `SettingsFile`, `WindowSettings` (what every window's settings hold, AX-6.5), `ConsoleSettings` (what the two console windows' settings hold besides: the console size, the recent folders, the sounds and signals, the transcript options; axClaude's and axTerminal's classes derive from it under the JSON names they had), `TextMatch` (a typed text against a name, AX-10.3) and `Accents` (the European letters taken apart and stripped without the globalization data: the executable runs with invariant globalization, where `string.Normalize` does nothing, and the test projects run the same way so that such a dependence fails in a test and not in the program; 2026-10-23),
  `Log` (per-app file name), `Updates/` (`UpdateCheck`, the release check, and `Updater`, the download and the
  hand-over to `install.ps1`; neither needs a window), `Audio/WaveTone` (the sounds as WAV bytes), `Paragraphs` (the
  paragraph starts of a text, for Ctrl+Up and Ctrl+Down), `WindowPlacement`, `EmbeddedText` (the embedded guides),
  `NaturalOrder` and `MediaFiles` (File Explorer's order of names, and the sound and video kinds with the time and
  speed rules; from axFile on their second use, axMedia), `Palette` (the command palette's lines and search, AX-10), `MenuText` (a menu item's or a button's text as spoken: no mnemonic marker, no trailing dots; the palette, the notices' key line and the menu choices that announce themselves), `MarkdownOutline` (the headings, links, lists, block quotes and tables of a Markdown text, with next and previous from a caret position; axDown SPEC §9, shared since its second use), `AxKitWindows` (the window lists of the axKit menu from the windows the system names, AX-9), `RecentlyClosed` (the bundle's list of closed files and folders, AX-9.4), `RecentPaths` (what every settings class does to its recent list: the newest first, once, ten at most, cleaned after loading; AX-9.5), `PathName` (the name of a path for a title or a label: the last part, or a drive's root itself), `BundleAbout` (the About sections and the embedded axKit guide, AX-9.9), and the console-to-transcript pieces that came from axClaude on their second use, axTerminal (2026-10-18, docs/axterminal/SPEC.md §5): `Pty/` (`PtyHost`, the ConPTY host, and `StreamRecorder`), `Vt/` (`VtParser`, `Screen`, `Wcwidth`) and `Transcript/` (`Line` and `LineKind`, `LineStore`, the base of each app's model: the lines the screen writes to, the decoder, the parser and the screen itself, the bookmarks and the cap, since 2026-10-23, `TranscriptMirror` and `Utterance`), tested in `tests/AxClaude.Tests` against its recordings.
- AX-6.2 `AxKit.Forms` (WinForms, checked with NVDA; the pure parts, `AppMessage`'s text form, `BundleKeys.Text` and
  `TextJump`, have unit tests in `tests/AxKit.Tests`): `OverlayPanel` (the notices in the
  window, with the `Notice` record callers pass), `ControlArea` and `ListKeys` (a question's answers as a list or a
  field in the message field's place, with the number and page keys of every such list), `Sounds` (wave-out devices kept
  open for the life of the process; `Notice` for every window, `Edge`, one short low note, for a key that moves nothing), `EditKeys.Page` (Page Up and Page Down in an edit control), `EditKeys.Paragraph`
  (Ctrl+Up and Ctrl+Down by paragraph, with Shift selecting), `EditKeys` (the KeyDown handler that gives an edit
  control both), `Announcer` (the UI Automation notification, AX-7.1), `TextJump` (a window's key pairs for the kinds of element in a text and the jump itself on an edit control, used by every notice text and by axDown's editor, AX-7.2), `StatusLayout`, the font
  handling (text size and the font picker), `BundleKeys` (AX-8), `CommandPalette` (the menu bar read into the
  palette's entries, AX-10), `WindowAccessibleObject` (the window's client area as its UI Automation rectangle, so
  that NVDA+End finds the status bar), `AxKitMenu` (the axKit menu every window has, AX-9), `RecentMenu` (the "Open recent …" submenus, AX-9.5), `AppMessage` (the hand-over from a starting process to the open window of an app that keeps one window, one `WM_COPYDATA` message with a verb and paths, found through the axKit menu's window list; axMedia and axPreview, docs/axmedia/SPEC.md AM-1.3, 2026-10-22; `SendTo` a window the caller found, axStart's hidden one, docs/axstart/SPEC.md AS-1.2), `TranscriptView` with `TranscriptKeys` and `QuickKey` (the read-only transcript of a console with its single-key navigation, the keys handed in by the window from its table; from axClaude on its second use, axTerminal, docs/axterminal/SPEC.md §5),
  and the text editor shell: `TextEditorForm` (the window every text editor app is: the file, the menus, find, go to
  line, the status bar, the notices, the fonts, the update flow; axDown's `EditorForm` until axJSON, the second
  editor), `EditorApp` (what differs between the editor apps, as data: name, verb, settings file, file types, F1 text,
  guide, the `TextJump` pairs of its outline, its resources pages and its commands on the text; `ReadOnly` and
  `Refuse` make the shell a viewer, axPreview, docs/axpreview/SPEC.md AP-D4, 2026-10-22: no Edit menu, no Save and
  no history, Reload and Edit in the editor in the File menu, `l` for the line, the hand-over of a file received),
  `EditorKeys` (the shell's keys, the same in every editor window, and `Viewer`, the read-only set) and `EditorHost`
  (the entry point every editor app's `Run` is one call into); `AppHost` (every app's entry point, AX-1.7); `AxKitForm`, the window base (AX-6.5); and `ConsoleForm` (2026-10-23), the window of a console app on it, which axClaude's and axTerminal's windows derive from: the transcript view, the field, the control area, the status bar pair, the pty bridge, the paced writes with one pump, the recording, the export, find, the signals and the music, with the hooks for what is the program's (docs/axterminal/SPEC.md AT-2).
- AX-6.3 A piece moves into a shared project only when a second app needs it, in a commit of its own, with the
  accessibility review; it moves unchanged where possible (B-3).
- AX-6.4 The notices and the question area are the bundle's one way of asking the user something, so that every
  window asks the same way: a notice has a title, a text, named buttons that Enter and Escape reach, the key line and
  the chime; a question in the message field's place is a list or a field named with the question, Enter answers,
  Escape cancels. An app that needs to ask uses these and does not draw its own dialog.
- AX-6.5 **The window base.** Every window of an app derives from `AxKit.Forms/AxKitForm`, which holds what the
  windows share and no app copies: the notice flow (`ShowNotice`, `CloseNotice`, `Fail`, `ShowError`; Alt, F10 and
  the window's shortcuts swallowed while a notice shows), `Announce`, the text font with its size keys and the
  Windows font picker (the choice said), the window's place and size, the settings saving and the notice for a
  settings file that could not be read, the axKit menu with the
  update check, the update notice and the install in place (AX-3), find in a text box, in a list, in a transcript
  and in a notice's text (one Find notice, one last text), Copy diagnostics (the account's name, its paths, e-mail addresses and ids replaced by `Redaction`, since it is meant for a public bug report, security audit S-20), `OpenUrl`, `OpenInExplorer`, the
  Windows folder picker (`ChooseFolder`), `Do` (a command's task with an escaped exception turned into a notice),
  the hand-over from a starting process of the app (`AppMessage`, answered at once and applied after), the submenus
  and the menu every window has (Options → Text, General and Developer; Help: `CreateTextMenu`,
  `CreateGeneralMenu`, `CreateDeveloperMenu`, `CreateHelpMenu`, each with the window's own items in their place;
  `Toggle` for a checked setting; `CreateExitItem` for the last item of the first menu), the
  window's pipe for the AI tools (`McpChannel`, MC-6.2), the window role for NVDA
  (AX-7.1) and the common part of the key switch (the text size keys everywhere, F1, the palette, Ctrl+S). A
  window adds its controls (the `Overlay` first) and fills in the hooks: the F1 text, what to hide for a notice and
  what to show after, the control the focus falls back to after one (`FocusTarget`), whether the chime follows a
  setting, what gets the font, how the new version starts it again (`RestartArguments`, `UpdateRestartText`), what
  to note of an update (`UpdateNoted`), its diagnostics lines, the control its announcements are raised on
  (`AnnounceSource`), its own palette entries (`PaletteEntries`, AX-10.5), what Ctrl+S does (`SaveCommand`;
  "Nothing to save here" by default), what it does with a hand-over (`TakeHandOver`), how it reports a settings file
  it could not read (`SettingsNotRead`; a notice by default, a system line in the console windows), and its own
  keys, as commands (docs/axkit/app-kit.md AK-2; `ProcessWindowKey` for what must come before them). **The focus across a notice is one rule, the base's** (2026-10-23): when the first
  notice of a chain opens, the control that has the focus is remembered and `OnNoticeOpened` is called with it; a
  notice a notice's button opens, or one shown while one is open, continues the chain; when the last one closes,
  `OnNoticeClosed` is called and that control gets the focus back if it is still there and visible (the Actions
  list of axFile, the details of axGIT, a transcript), and `FocusTarget` otherwise (an answered question's list, the
  message field behind a question). No window decides where the focus goes after a notice. The
  settings each window needs are an abstract class in `AxKit.Core`, `WindowSettings` (font, update check, place;
  `Save`), which every app's settings class derives from while its file keeps its own shape. The base is one class
  in partial files, one per section (the hooks and the menus, the notices, find, the fonts, the updates, the palette).
  It grew out of the editor shell on the third copy, axGIT (2026-10-18); axClaude's `MainForm` moved onto it the same
  day, its own extras (the sound setting for the chime, the reader's kept caret, system lines for the update's steps)
  as hooks; the notice and focus core was written again, and what every window still copied moved in, on 2026-10-23
  (`docs/axkit/review-2026-10-23.md`, 5.3 and 6).

### AX-8 Keys

- AX-8.1 Every key an app handles is one of two kinds: **bundle-wide**, meaning the same in every window of axKit, or
  **window-specific**, meaning something in one window only. Each app's keyboard table in its spec marks every key
  with its kind.
- AX-8.2 Bundle-wide keys are defined once, in `AxKit.Forms/BundleKeys`, a plain static class of constants, and every
  window uses those constants. The list: F1 the keyboard shortcuts; Tab,
  Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the window's controls (and between a notice's text, field
  and buttons); Ctrl+F find, F3 and Shift+F3 next and previous, in a notice's text as well (AX-7.2); Ctrl+S save (what the window holds: the editors' file; axClaude and axTerminal, which have no
  document of their own, export their transcript to a text file with it, from the bottom of their first menu, so that
  Save and Save as keep their place in the editors' and the first menus share one order; a window with nothing to
  save says "Nothing to save here", axGIT adding the keys that do something, through the base's `SaveCommand`); Ctrl+Plus and Ctrl+Minus text size; Enter and Escape on a notice; Page Up and Page Down one screen in an
  edit control; Ctrl+Up and Ctrl+Down the start of the previous and next paragraph in an edit control, a paragraph
  starting at the first line or at a line with text directly after a blank line, and Ctrl+Shift+Up and
  Ctrl+Shift+Down the selection grown to there, as in Word (`EditKeys.Paragraph`; the EDIT control's own keys scroll a
  row and leave the caret where it is, as Notepad's do, which a screen reader, reading at the caret after the key,
  reports as the same text again); Ctrl+Shift+P the command palette (AX-10); F4 a report to axKit's maintainer (docs/axkit/reports.md RP-2.1, 2026-11-07); Alt+F4 exit; Alt and F10 the menu. Everything else is window-specific.
- AX-8.3 Window-specific keys are defined once per window, in one table (`AxClaudeKeys`, `AxDownKeys`, `AxJSONKeys`, `AxGitKeys`, `AxTerminalKeys`, `AxFileKeys`, `AxMediaKeys`, `AxPreviewKeys`, `AxStartKeys`, `AxProcessKeys`, `AxTaskKeys`, `AxPassKeys`, `AxCalcKeys`, `AxClipKeys`), as constants
  with names that say what the key does. Changing a key is a change to that one line: the menu item and the key
  handler read the table, and the F1 text names the same keys and is changed with it. Single-letter quick keys
  (axClaude's `h`, `i`, `o`) are in the table too. The text editor shell's keys (`AxKit.Forms/EditorKeys`: Ctrl+N new,
  Ctrl+O open, Ctrl+Shift+S save as, Ctrl+G go to line, Ctrl+Shift+W word wrap, Ctrl+W show in File Explorer) are
  defined once for every editor window, and each editor app's table includes them, so that one table per window
  still holds every key and the collision test sees them.
- AX-8.4 A unit test in `tests/AxKit.Tests` fails when a window-specific key equals a bundle-wide key, and lists the
  keys that two windows use for different actions, so that a collision between apps is seen before it is felt. A
  window key may take a bundle key's place in one control only by the user's decision, listed by name in the test's
  `Overrides` with its spec line: axTerminal's command field, Ctrl+Up and Ctrl+Down for the history (AT-3.9, 2026-10-23).
- AX-8.5 A key is never handled anywhere but through its table, and no key is intercepted that NVDA uses (AX-7.4).
  The text a menu item shows for its key comes from the tables too (`BundleKeys.Text`: "Ctrl+Shift+S", "Shift+Escape"),
  so that a key changed in its table changes in the menus by itself; only Windows' own keys (Alt+F4, the edit
  control's Ctrl+Z, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A) and descriptions ("Enter in the message field") are written out.
  A key of the number row ends in a full stop ("Ctrl+1."): NVDA speaks the shortcut directly before the position of
  the item in the menu ("1 of 9"), and the synthesizer runs "Ctrl+1  1 of 9" together into "Ctrl+11 of 9"; the full
  stop is a pause between the two.
- AX-8.6 Type-to-find in every list a user moves through by typing (2026-10-24; File Explorer's way, axFile's AF-3.6
  since 2026-10-22): letters typed within a second of each other build one prefix and select the first item that starts
  with it from the one in hand, round the end; the same letter again goes on to the next item starting with it; Shift
  with a letter searches the same way backwards (the user's ask; the case of the letters never matters, so a capital
  only says the direction); the edge earcon when no item starts with it; nothing is said but what NVDA reads of the
  selection. Up or Home on the first item and Down or End on the last play the edge earcon too, since the key moves
  nothing. Both are built into one control, `AxKit.Forms/AxKitList`, the list of every app (`AxKit.Core/TypeAhead` the
  search, tested): a window makes an `AxKitList` and has them, and gives it the names to search only when its items
  are not all names (`Names`). `ListTests` fails on a plain `ListBox` in an app; the lists that never take a typed
  letter are its named exceptions: axStart's and axClip's matches and the palette's, never focused, and a question's
  answers, whose letters are the screen's keys (axClaude D33).
- AX-8.7 F2 renames in place in every list whose items can be renamed (the user's rule, 2026-10-24; axFile's AF-4.2
  since 2026-10-20): a box named "New name" over the item, holding its name with the part that changes selected (a
  file's name before its extension); Enter takes it, and a name the window refuses is said and the box stays; Escape,
  or the focus leaving the box, keeps the old name; Ctrl+Z in the box undoes the typing. Built into `AxKitList`
  (`BeginRename`, `EndRename`, `Renaming`), so that a window only says what renaming means for its items. In axFile's
  folder list, axTask's and axPass's navigators, and the item apps' field titles.
- AX-8.8 A check box says its new state when the user toggles it, "checked" or "not checked", as one announcement
  (2026-10-31: NVDA said nothing on Space in the consent notice's box). Built into `AxKit.Forms/AxKitCheckBox`, the one
  check box of the bundle; `CheckBoxTests` fails on a plain `CheckBox`.

### AX-9 The axKit menu

- AX-9.1 Every window has an **axKit** menu (mnemonic A) between its own menus and Help, the same in every window: one
  class, `AxKit.Forms/AxKitMenu`, which the window constructs with its Command palette item, its two update items, its
  latest-release state and its notice method. In order: Command palette… (P, AX-10; the bundle's own command, so it
  sits in the bundle's menu, asked 2026-10-23); Send a report… (S, F4) and Your reports (Y, the Reports list in
  axTask; docs/axkit/reports.md RP-2.2, 2026-11-07); a separator; New window ▸ and Recently closed ▸; a separator; Open
  windows ▸, Open windows by type ▸ and Close all windows; a separator; AI tools ▸ (I, `mcp.md` MC-6.1: the bundle's
  own settings live here, an app's Options hold only the app's); Check for updates, Update to (while a newer
  release is known) and About ▸ (AX-9.9). The lists are filled each time the menu opens, so that they show the
  windows and the files of that moment.
- AX-9.2 **New window ▸** (N): one item per app of the bundle (`Dispatch.Apps`), "axClaude", "axDown", with the letter
  after ax as the mnemonic. It starts `AxKit.exe <verb>` with no arguments: a new process and a new window (B-2), the
  app starting as it does from its Start menu entry. The apps stand in four groups, a separator between them
  (2026-10-24, the user's choice of a flat list over a submenu per group; since 2026-10-29 the access layers last,
  AX-11.3): files (axFile, axDown, axJSON, axPreview, axMedia), one's own lists (axTask), utilities (axStart,
  axClip, axCalc, axProcess), then the access layers, each by its title (axClaude, axTerminal, axGIT, axPass). Every list of apps in the windows has this order and these separators: New window, Open
  windows by type, axFile's Open in axKit (`AxKitMenu.AddApps`). A new app is one line in `Dispatch.Apps`, with its
  group, what it opens and whether Show in offers it; every list, Show in (AX-9.10) and Recently closed (AX-9.4)
  follow from that line.
- AX-9.3 **Open windows ▸** (O): every open window of the bundle's apps except this one, the most recently used first.
  The order is the one Windows keeps its top-level windows in, which is Alt+Tab's, so nothing is stored and Alt+A, O,
  1 is always the window the user was in before this one. **Open windows by type ▸** (B) holds one submenu per app,
  "axClaude (2)", "axDown (1)", the number being its open windows: that app's windows in the same order, this window
  included and checked, so that the list also says where the user is.
- AX-9.4 **Recently closed ▸** (R): the last ten files and folders a window of any app held when it closed or replaced
  them (axDown's file, axClaude's project folder), the newest first, each app and path once, those that still exist
  (checked as the menu opens for a path on a drive of this computer only, since a share or a mapped network drive
  can take the network's timeout to answer, which no menu may wait for; a path elsewhere is listed as it is and
  found missing when chosen; `AxKit.Core/LocalPaths`, tested, 2026-10-23; the apps' recent lists follow the same
  rule, AX-9.5); Enter starts that app on it in a new window. An app that holds no file or folder (axProcess, axCalc,
  axClip, axStart) is listed by its name alone when its window closes, once, and Enter starts it (2026-10-24). The
  window base does it for every window as it closes (`AxKitForm.PathInHand`: the file or folder, empty for an app that
  holds none, which is the default, so that a new app is listed without a line of its own; nothing for a window with
  nothing to open again, an untitled text or axFile at This PC); a window that swaps its file or folder remembers the
  old one itself. A background app's window (axStart, axClip) hides on Alt+F4 and is listed when Quit ends it. The list is one file for the bundle, `%APPDATA%\AxKit\recently-closed.json`
  (AX-5.1), written whole by the window that closes or swaps (`AxKit.Core/RecentlyClosed`, through `SettingsFile`; two
  windows closing in the same instant leave the last writer's list). Nothing else about windows is stored.
- AX-9.5 A window's or a file's item is its number, a full stop and a title, "1. notes.md (C:\notes\notes.md) - axDown":
  the number is the mnemonic (1 to 9, then a to z), so that Alt+A, O, 1 or Alt+A, R, 1 acts without an arrow key, and
  the full stop is a pause before the name (AX-8.5). A window's title is the window's own; a closed item's is the
  file's or folder's name, its full path in parentheses, and the app. The apps' own recent lists have the same shape,
  "1. myproject (C:\src\myproject)" (axClaude FR-8.5, axDown AD-4.1). Enter brings a window to the
  front, restored when it was minimized. A list with nothing in it holds one item that says so and does nothing, enabled
  ("No other open windows", "No open axDown windows", "Nothing recently closed"; the apps' own lists likewise, "No
  recent folders", "No recent files"): WinForms skips disabled items when the keyboard moves through a menu, so a
  submenu of only a disabled item opens with nothing selected and leaves the screen reader with nothing to read.
- AX-9.6 The windows are the visible, unowned, titled top-level windows of every process named as this one, told
  apart by the end of their title (" - axClaude", " - axDown"; AD-10, axClaude's `<folder> (<path>) - axClaude`); a window of
  none of the apps (the bundle's own message box, AX-1.4) is listed nowhere. The lists are pure code in
  `AxKit.Core/AxKitWindows` and `AxKit.Core/RecentlyClosed`, unit tested; only the listing and the switch (user32
  `EnumWindows`, `SetForegroundWindow`) and the menu itself are in `AxKitMenu`.
- AX-9.7 Only axKit's windows are listed; Alt+Tab stays the way to everything else. Alt+A is a menu mnemonic, not a key
  of the tables (AX-8); the F1 texts name it. A separator carries no text and NVDA passes over it; the groups are
  told apart by their names, not by headers, which would be one more stop on every pass through the menu.
- AX-9.8 **Close all windows** (C) posts the close command Alt+F4 sends (`WM_SYSCOMMAND` `SC_CLOSE`) to every other
  open axKit window and then to this one; each window closes as it does on Alt+F4 and asks there what it would ask
  then (axDown's Save changes?, axClaude's Close axClaude? while Claude works), so nothing is lost without a question.
- AX-9.9 **Check for updates (axKit n)…** (U) and **Update to axKit n…** (T, while a newer release is known) are the
  window's own update items (AX-3.1), placed here by the window. **About ▸** (A) is the bundle's About in sections,
  the same in every window (`AxKit.Core/BundleAbout`), each shown as a notice of the window: **Version** (V; the
  version, the apps, the latest release as the window knows it, the releases page, the settings and logs folders),
  **Licence** (L; `NOTICE.md`, embedded: the GNU AGPL 3 notice, the additional terms of its section 7, the warranty
  and liability disclaimer in full words, and who is responsible for what: the standalone apps, the access layers
  with what axKit, the other program and the user each do, and the bundle's services; since 2026-10-29; `LICENSE`
  beside it is the AGPL),
  **Attribution** (A; the maker, the Anthropic note, what axKit is built with), **Privacy** (P; `PRIVACY.md`, embedded:
  everything axKit contacts over the network and when, the outside programs it runs, what stays on the computer; since
  2026-10-25), **Third-party notices** (N; `THIRD-PARTY-NOTICES.md`, embedded: the .NET runtime and Windows Forms in
  the program under MIT, the Unicode data), **axKit guide** (G; `docs/axkit/README.md`,
  the zip's guide, embedded by `AxKit.Core`: install, start, update, remove; its headings take the window's heading
  keys, AX-7.2), and **axKit on GitHub** (H; the repository page, `UpdateCheck.RepositoryPage`, in the browser).
- AX-9.10 **Show current folder in ▸** (2026-10-24, one builder, `AxKitForm.CreateShowInMenu`): the window's File
  Explorer item (Ctrl+W), then the apps that work in a folder (`App.ShowIn`: axFile, then the access layers axClaude, axTerminal, axGIT, in
  that order) but the window's own, each a new window on the file or folder in hand: axClaude and axTerminal on a
  file's folder, axGIT on the repository that holds it, axFile on the folder or with the file selected
  (`Dispatch.PathFor`). The windows that hold a folder or a file have it: axClaude and axTerminal ("Show current folder
  in"), axGIT ("Show current repository in"), axFile ("Show current folder in"), the editors and axPreview ("Show
  current file in") and axMedia ("Show current file in"). With nothing in hand the window says why ("No project
  folder", "No repository", "No file yet", the empty queue).

- AX-6.6 **Menus are the standard ones.** A menu bar item's drop-down is the one WinForms makes for it; no window
  assigns a drop-down of its own (a `ContextMenuStrip` or any `ToolStripDropDown` set as `DropDown`). A hand-made
  drop-down has its owner item cleared when it closes, after which the bar no longer leaves auto-expansion on Escape,
  and Left and Right open the neighbours' menus instead of moving along the bar (axFile, 2026-10-21). `MenuTests` in
  `tests/AxKit.Tests` scans the sources for the construct. A key that must work inside an open menu goes in a context
  menu subclass (axFile's `ItemMenu`), which has no owner item. In menu mode WinForms routes the keys to the menu
  bar, which hands the ones it does not use up to the window's `ProcessCmdKey`; the window base gives a key that
  comes from a `ToolStrip` to the menu alone, never to the window's keys, F1 and the palette included (axFile's
  Right opened the entry in the list instead of moving along the bar, 2026-10-22; F1 opened the shortcuts over an
  open menu, 2026-10-23).

### AX-10 The command palette

The user's ask of 2026-10-21, specified 2026-10-22: one key in every window finds any of the window's commands by
name, so that a command is reached without knowing its menu or its key, and its key is learnt on the way.

- AX-10.1 **Ctrl+Shift+P** (`BundleKeys.CommandPalette`, AX-8.2) in any window opens the palette as a notice of the
  window (AX-6.4, AX-7.2), titled "Command palette": one field named "Search commands" with the caret in it, and
  under it a list of the matches named "Commands, n" that never takes the focus, axStart's shape (docs/axstart/SPEC.md
  AS-3.1, AS-D2). axKit → Command palette… (the first item of the axKit menu, AX-9.1) opens it too, so that it is found
  from the menus. While another notice shows, or in menu mode (AX-6.6), the key does nothing.
- AX-10.2 **The entries** are the window's menu bar as it stands when the palette opens, the axKit menu and Help
  included: every enabled item that does something when chosen, in menu order. An entry reads its name, then its menu
  path, then its key when it has one, then "checked" for a checked item: "Save as, File, Ctrl+Shift+S", "Larger text,
  Options, Text, Ctrl+Plus", "Word wrap, Edit, Ctrl+Shift+W, checked". The name is the item's text without its
  mnemonic ampersand and its trailing dots. Left out: separators, items that only open a submenu, disabled items, and
  the lists a menu fills only as it opens (Open recent, Open windows, Recently closed), whose state the palette would
  have to guess; a window marks such a submenu where it attaches its filling (`CommandPalette.FilledOnOpening`), and
  the fixed items such a submenu also holds (axFile's Add or remove this folder under Favorites, This PC and Eject
  under Drives) are left out with it, their keys still working. A menu whose items change their text or check mark as
  it opens (axFile's Undo named after what it would undo, axTerminal's Switch shell with the shell in use checked)
  is marked where its refreshing is attached (`CommandPalette.RefreshedOnOpening`), and the palette runs the
  refresh before reading (until 2026-10-23 such an item read as it last stood). The palette reads
  the menus and keeps no list of its own: a command added to a menu is in the palette.
- AX-10.3 **Matching** is axStart's (AS-2.2), moved to `AxKit.Core` on this second use: the typed text, trimmed, case
  and accents ignored, against the entry's whole line; an entry matches when its line holds every typed word (`text
  larger` finds "Larger text, Options, Text"). The order: a line that starts with the first word, then a line with a
  word that starts with it, then any other match; within each, menu order. An empty field lists every entry.
- AX-10.4 **The keys in the field** are axStart's (AS-3.2, AS-3.3): typing refills the list and selects the first
  match, and 300 ms after the last change one notification says it and the count, "Save as, File, Ctrl+Shift+S, 1 of
  3", or "No commands match xyz"; Down and Up, Page Down and Page Up, Ctrl+Home and Ctrl+End move the selection and
  say it; at an end, where nothing moves, the edge earcon and then the match again, "Save as, File, Ctrl+Shift+S, 1
  of 1", so that the last thing heard is the position and not the typed text (asked 2026-10-23; the earcon alone had
  left NVDA's reading of the field as the last thing heard), and with nothing matching the earcon and "No commands
  match …"; the field's editing keys stay its own. **Enter** closes the palette, gives the
  focus back to the control that had it, and then runs the selected entry as a click on its menu item would, so that
  the command acts where the user was; a checked setting run this way says its new state, "Word wrap, not checked",
  since no menu shows it. Enter with nothing matching says "No commands match …" and the palette stays. **Escape**
  closes the palette with the focus back and nothing run.
- AX-10.5 **A window's own entries.** A window may add entries after the commands through one hook of the window base,
  each a name and an action. axFile adds the folder's entries, "notes.md, file", "docs, folder", and Enter on one acts
  as Enter on it in the folder list (docs/axfile/SPEC.md). Other windows add none.
- AX-10.6 The palette is the window base's (`AxKitForm`, AX-6.5) with the matching in `AxKit.Core` (unit tested); no app
  writes code for it beyond AX-10.5. Every window's F1 text names Ctrl+Shift+P. axGIT's Push, which held
  Ctrl+Shift+P, moved to Ctrl+Shift+H (docs/axgit/SPEC.md AG-5.4).

### AX-11 Access layers and consent

The user's ask of 2026-10-29, specified and built the same day (NVDA checks owed): so that no one can be confused about what axKit
is, every app that works through another program or service says so wherever its name appears, and the user agrees,
inside the window and before the first connection, that axKit is only the access layer and that the other program's
terms are theirs to know. `NOTICE.md` ("Who is responsible for what") is the full text this section points to.

- AX-11.1 **Two kinds of app.** A **standalone app** needs only Windows and works with the network off (axDown,
  axJSON, axFile, axMedia, axPreview, axStart, axProcess, axTask, axCalc, axClip). An **access layer** shows what
  another program does and passes the user's commands to it; that program does the work, under its maker's terms:
  axClaude for Claude Code (and through it Anthropic's service), axGIT for Git (and the servers of a repository's
  remotes), axTerminal for the user's shells (PowerShell, Command Prompt, Git Bash, PowerShell 7), axPass for
  KeePassXC. `Dispatch.Apps` holds it: `App.AccessLayerFor`, the program's name ("Claude Code", "Git", "your shell",
  "KeePassXC"), empty for a standalone app. A new app that starts or talks to a program axKit does not include is an
  access layer.
- AX-11.2 **The keyword.** An access layer's name is never shown alone: wherever the user meets it, it reads
  "<name>, access layer for <program>" ("axClaude, access layer for Claude Code"), from one place,
  `App.Title` (the name alone for a standalone app; the phrase "access layer" is one constant). That is: the window
  title (wherever the app's name stands in it today), the axKit menu's New
  window, Open windows, Show current folder in and Recently closed lists, axFile's Open in axKit, the command palette,
  About, Version, the Start menu entry's name and description, File Explorer's Open with axKit submenu, Windows' Open
  with list, the installer's lines, the first line of each access layer's user guide and F1 text, both READMEs and
  `NOTICE.md`. The console commands (`axclaude`) and the verbs stay single words.
- AX-11.3 **Grouped apart.** Every list of apps shows the standalone apps first, in their groups (AX-9.2), then the
  access layers, with a separator before them (in File Explorer's submenu, the separator flag of its entry); the docs
  list them the same way under "Standalone apps" and "Access layers". `Dispatch.Apps` is in that order, and a test
  fails when an access layer stands before a standalone app.
- AX-11.4 **The consent notice.** Before an access layer connects to a program or service for the first time, it shows
  a notice in its own window (AX-7.2; the window's input waits behind it), titled "Before <app> connects to
  <program>". A plain reminder, not a contract (2026-10-31, the user's ask: personal tools, not a product): "A reminder:
  axClaude is only an access layer. It shows what Claude Code does and passes your commands to it; Claude Code is not
  part of axKit", what the other program does, that its terms are between the user and its maker with their address in
  the text, and that axKit is a set of personal tools shared as they are. Below the text, one check box, unchecked:
  "I understand that <app> is only an access layer, and that following the terms of <program> is up to me". Two
  buttons, the fewest that work (2026-10-31): **Connect** (Enter; with the box unchecked it says "Check the box first"
  and nothing else happens) and **Not now** (Escape: the window stays, not connected, with one line
  saying so and how to connect, the app's Restart or Open). The check box is a real `CheckBox` with that text as its
  name, after the text and before the buttons in the Tab order; Space toggles it. `OverlayPanel` gains the check box
  as an optional part of a notice (`Notice.Consent`), for every app.
- AX-11.5 **When it is asked.** Once per access layer and program, the first time it connects, kept with its date and
  axKit's version in `%APPDATA%\AxKit\consent.json` (`AxKit.Core/Consent`, unit tested); never again for the same pair,
  updates included (AX-11.7 covers those). The pairs:
  - axClaude, Claude Code: before it first starts Claude Code.
  - axTerminal, each shell on its own: before it first starts PowerShell, Command Prompt, Git Bash or PowerShell 7,
    those that come with Windows included.
  - axGIT, Git: on first opening a repository; and each server on its own (github.com, a company's server), before
    the first fetch, pull or push that reaches it, by the host of the remote's address, since that is a new service
    with its own terms.
  - axPass, KeePassXC: before the first master password goes to it (unlocking a database) and before File, New
    database.
  - The AI tools (AX-9, `docs/axkit/mcp.md`): the pairing notice (MC-6.4) carries the check box, which Allow needs;
    Deny stays its default. Forget AI tools withdraws these consents with the pairings.
  - Every window, axKit menu, AI tools, Add axKit to Claude Code (MC-6.8): it runs Claude Code's program (`claude mcp
    add`), so the agreement for Claude Code comes first, under the window's own app (2026-11-02).
  - A program installed from Install it now (AX-2.9) is agreed to in the same step, under the name the app connects to
    it by (axTerminal installs Git for Windows as its shell Git Bash), so that the connection asks nothing more.
  - **Install it now** (AX-2.9): before an installer runs, the same notice for the program it installs, titled
    "Before <app> installs <program>", with **Install** in place of Connect; its acceptance counts for the app's first
    connection, so the user agrees once, in the step they were already taking.
  The notice is the window base's (`AxKitForm.HasConsent`, the one check: true goes on, false has shown the reminder, whose Connect runs the caller again), the programs' words and addresses `AxKit.Core/ConsentTarget`.
  A custom shell has no address for its terms: its notice says so.
- AX-11.6 **Reminders at sign-in.** One line says whose account it is, without a check box: axClaude, on sending
  `/login`: "You are signing in to Anthropic through Claude Code; the account and its terms are between you and
  Anthropic", spoken after "Sent" and kept as a system line. axGIT, the first time a window reaches each server
  after its consent (git's credential helper may then ask for a sign-in in its own window, which axKit cannot see
  coming): "Connecting to github.com; if Git asks you to sign in, the account is between you and github.com". Still
  to come: Claude Code's own login screen at its first start, recognised once it is recorded
  (`docs/axclaude/claude-screens.md`).
- AX-11.7 **Updates.** The update notice (AX-3.1) says first: "A reminder: installing means you accept axKit
  <version>'s licence and NOTICE.md, linked in the notes below". Its first
  two buttons read **I accept all terms, install and restart** (Enter) and **I accept all terms, install and close**;
  Open release page and Later stay. The acceptance is kept in `consent.json` with the version and the date. Every
  release's notes carry the same sentence with the links to that tag's `LICENSE` and `NOTICE.md` (`release.yml`,
  before the code signing policy line), so the update notice shows them too.
- AX-11.8 **About.** About, Version lists the apps in the two kinds ("Standalone apps, which need only Windows: axFile,
  … Access layers, which only show and pass on the work of a program you install, under its maker's terms: axClaude
  for Claude Code, …"). About, Licence (AX-9.9) is `NOTICE.md` followed by "What you have agreed to on this computer",
  one line each ("axClaude, Claude Code, 29 October 2026, axKit 4.0.0"; "Update to axKit 4.0.0, 29 October 2026"),
  "Nothing yet." when empty, and how to be asked again: delete consent.json in the settings folder. No menu of its
  own (2026-10-31: a Consents submenu with Withdraw was built on 2026-10-29 and folded in here, to keep the controls
  few).
- AX-11.10 **The framing** (2026-10-31, the user's decision): axKit is presented as the tools its maker uses every day
  as a blind scientist, shared in case they help others, and never as a product or a service: no customers, no
  support, no warranty. The texts the user meets (the consent reminder, the update notice, F1's access layer line, the
  installer, the README, the guides, `NOTICE.md` outside the licence's own terms) are worded as plain reminders, to
  the maker as the main user and to anyone else using the tools, not as a vendor's legal notices. The licence and its
  section 7 terms keep their full words, since the licence needs them.
- AX-11.9 **Tests.** The access layers are the last group and carry the keyword (`DispatchTests`); window titles are
  read back to their app (`AxKitWindowsTests`); `Consent` records, reads, withdraws and takes a damaged file as nothing
  agreed (`ConsentTests`); the servers' hosts (`GitRemoteTests`); every program Install it now offers has its
  consent target with an address (`ConsentTests`).
- AX-11.11 **How an access layer meets the network (2026-11-05).** An access layer starts its program as a child
  process of the window (Claude Code and the shells in a ConPTY pseudo console, git and `keepassxc-cli.exe` as
  processes with their standard streams on pipes) and talks to it over that pseudo console or those pipes, on the
  computer, never over the network. The program's network use is its own, as from a terminal: Claude Code talks to
  Anthropic's service and checks for its own updates; git reaches a remote on fetch, pull and push alone (AX-11.5 asks
  the server's consent first; its credential helper may ask for a sign-in in its own window, AX-11.6); a shell's
  commands reach what they reach; KeePassXC's tool has no network feature. axKit does not see, record or change that
  traffic; it reads what the program prints and passes on what the user types. axKit's own code makes network requests
  in two places (AX-12, 2026-11-07), over HTTPS to GitHub: the updater (AX-3), the update check and the download the
  user chooses, which the updater takes only from this repository's release address; and the report sender
  (docs/axkit/reports.md RP-6), which posts a report the user sends as an issue of the reports repository and reads
  that repository's blocklist first, only on the user's Enter; nothing else in the code makes a network request or
  listens for one, and the remaining addresses in the code are the Help menus' links, opened in the browser, and the
  installers' (AX-2.9: winget from its own source, Anthropic's installer script). `PRIVACY.md`, `README.md`,
  `docs/axkit/README.md` and `SECURITY.md` say this in these terms, and a change here changes them in the same commit;
  `docs/axkit/outside-calls.md` lists every such call in the code, written from the sources (B-17).

### AX-12 Reports from every window

The design is `docs/axkit/reports.md` (RP-n, 2026-11-07); this section fixes what the bundle promises.

- AX-12.1 **F4 in every window** (`BundleKeys.Report`, AX-8.2) and the axKit menu's Send a report… open the notice
  "Report to axKit" with the window's own fields (`ReportControls`): the user's words first and focused, Enter sends,
  Shift+Enter breaks a line; then the kind (Feedback, Bug, Feature request), the app and version, the window's last
  twenty actions (`RecentActions`: its commands and notices, and its announcements in the apps whose announcements
  never carry the user's content, so not in axClaude, axTerminal, axPass and axClip), where the focus was by name and place and never by text, the
  log's last lines and a title, every prefilled field redacted (`Redaction`) and editable. Your reports opens the
  Reports list in axTask.
- AX-12.2 **Saved first, sent second.** Enter writes the report as an item of the axTask list `%APPDATA%\AxKit\reports`
  (RP-5), then sends it and every unsent one, oldest first, as an issue of the reports repository `KyleKeane/AxKit-reports`
  over GitHub's REST API (RP-6). Nothing is sent at a window's start; the unsent go with the next report and when Your
  reports opens (RP-6.7). Escape with words typed keeps a draft.
- AX-12.3 **axKit's own reporter account.** The issue is posted by a machine account of the maintainer's whose token
  the release build carries and which can reach that repository's issues alone (RP-6.2); every report ends with the
  install's random reporter id and the version (RP-6.3). A blocklist in that repository, read before a send, the
  repository's own workflow, and the token's revocation at a release are the levers against abuse (RP-6.4); a report
  that cannot go stays saved, and the notice offers it for copying and GitHub's own form (RP-6.5).
- AX-12.4 **Consent once** (AX-11.4's shape): before the first report goes, a notice says that it becomes a public
  issue under axKit's account with nothing that names the user, with the check box; recorded in `consent.json` under
  the app name "axKit" and listed in About, Licence (RP-6.6). The report sender is the second place axKit's own code
  meets the network, beside the updater (G6, AX-11.11, `PRIVACY.md`).

### AX-13 The Microsoft Store copy

- AX-13.1 **One package.** axKit also ships as an MSIX package through the Microsoft Store (`docs/axkit/store-distribution.md`,
  2026-11-08): the Store ID 9PK01L2K5000 (`AxKit.Core/StoreListing`), the identity `axKit.axKit`, the publisher
  `CN=F1B55F68-5EAE-4338-B94B-73044EAE40DA`, the publisher display name axKit, as Partner Center gave them. `publish.ps1
  -Msix` builds it: a folder publish (every file as a file), `AppxManifest.xml` written by `tools/make-manifest.ps1` from
  the apps listing (AX-1.4), the images by `tools/make-icon.ps1 -Png`, packed by the Windows SDK's `makeappx` (Windows
  Kits, or the tools `tools/get-sdk-tools.ps1` fetches into `tools/sdk` from Microsoft's package, their signatures
  checked) into `publish\AxKit-<version>.msix`, unsigned: the Store signs it with Microsoft's certificate after
  certification, which Smart App Control accepts. The version is the bundle's with `.0` appended; the fourth part is
  the Store's. The package needs Windows 10 version 2004 (10.0.19041, the `uap10` manifest namespace); the zip keeps
  1809 (5.4). Its one capability is `runFullTrust`.
- AX-13.2 **The entries.** The Store allows eight `<Application>` entries per package and no hidden one (its package
  validation of 2026-11-09: "declares 17 apps, which is more than the limit of 8", and `AppListEntry="none"` needs a
  waiver the account has not), so the package has eight entries, the apps marked `StoreEntry` in `Dispatch.Apps`
  (axClaude, axDown, axFile, axTerminal, axGIT, axTask, axStart, axClip; the maintainer's choice, changed there and
  nowhere else; `DispatchTests` keeps the count), all running `AxKit.exe`, named as AX-2.2's Start menu entries
  (`axKit axClaude, access layer for Claude Code`), each with its own images, so that Windows search finds them as
  before. The axClaude entry comes first, has the id `AxKit` and no parameters, so that it starts axClaude as
  `AxKit.exe` does with nothing (AX-1.3), and carries what the whole bundle needs: every app's console command,
  `axkit.exe` and `<name>.exe` for all fourteen (`axdown.exe`, `axcalc.exe`), on the PATH through the user's
  WindowsApps folder in place of AX-2.4's `.cmd` shims, and the file types of the apps without an entry. Windows gives
  a process started through a command the command's own path as the first word of its command line (measured
  2026-11-09: `"…\WindowsApps\axdown.exe"`), so `Dispatch.VerbOfProgramName` reads the app off it, and a file opened
  with a type of the first entry reaches its app by its kind (AX-1.2). Every other entry runs `AxKit.exe` with its verb
  as the entry's parameters (`uap10:Parameters`, which Windows passes; measured 2026-11-08), and an app with Open with
  types (AX-2.3's last column) declares them under its own entry, so that Windows' Open with lists it on them; a type
  is declared once per package, by the first app of the list that has it, and axPreview, whose Open with says every
  file, declares none: a package cannot claim every file, and AX-2.3's submenu (the apps on folders, axDown and
  axPreview on any file) has no form in a package without a COM server, which this copy does not have. Each app with
  `AtSignIn` (axStart, axClip) carries a startup task on its entry with `<verb> --hidden` as the task's own
  parameters, which Windows lists under Settings, Apps, Startup. The six apps without an entry (axJSON, axMedia,
  axPreview, axProcess, axPass, axCalc) are reached by their command, by a file of their kind, by the axKit menu's New
  window in any window and from axFile; axStart lists the Start menu's programs and so knows the eight.
  `AxKit.Core/PackageIdentity` tells whether the process runs from the package (`GetCurrentPackageFullName`) and which
  entry started it (`GetCurrentApplicationUserModelId`); `Dispatch.WithApplicationVerb` puts the verb first when the
  command line carries none, the command's name before the entry's, nothing for the first entry, so that the dispatch
  holds whether or not Windows passes the parameters (AX-1.8; tested in `DispatchTests`).
- AX-13.3 **No updater.** The Store installs, updates and uninstalls the package. The Store's copy makes no update check
  at start, has no Check for updates at start setting (Options, General), downloads nothing and never shows Update to.
  Check for updates (axKit menu) still asks GitHub for the latest release, so that About, Version and the notice can say
  where this copy stands; the notice says that the Microsoft Store installs updates by itself, usually within a day of
  a release, and offers Open axKit in the Microsoft Store (`ms-windows-store://pdp/?productid=9PK01L2K5000`, where
  Update appears as soon as the Store has the release), Open release page and Close. About, Version names the Store and
  the package. `install.ps1`, the Run key, the File Explorer keys, the shims and the Settings, Apps entry are not
  written, the package providing their equivalents; `Program.DeleteOldProgramFiles` does nothing outside the install
  folder; the release key's check of the zip does not apply.
- AX-13.4 **Start with Windows** is Windows' own page in the Store's copy: `SignInStart.Apply` writes nothing (a Run
  value written by a packaged process goes to the package's private registry and would never run), and the Options,
  General item of axStart and axClip opens Settings, Apps, Startup (`ms-settings:startupapps`,
  `SignInStart.OpenWindowsStartupSettings`, `HotkeyForm.StartWithWindowsItem`) and says to turn the app on or off there.
- AX-13.5 **AppData and the programs axKit starts.** A packaged desktop app's new files under AppData Local and Roaming
  land in the package's private folder (`%LOCALAPPDATA%\Packages\axKit.axKit_<hash>\LocalCache`), its writes under HKCU
  in a private hive, and its package folder is read-only (measured 2026-11-08). So axKit's own files of AX-5 go with the
  package when it is uninstalled; a file that already exists under `%APPDATA%\AxKit` from an unpackaged install stays
  readable and is modified in place, a new one is private, and a development copy and the Store copy on one computer
  share only what existed before the Store copy's first start. The programs axKit starts run outside the package, as
  the children of a packaged desktop app do by default (measured 2026-11-08 with a probe started as a package entry:
  Windows reports no package for a child started by `CreateProcess`, by .NET's `Process.Start` or through the shell, and
  their AppData writes land in the user's folders): Claude Code and the shells in the pseudo console, git, KeePassXC's
  tool, winget and the installers, the programs the user starts from axStart and axFile, and the browser all behave as
  they do without axKit. A child that runs a program file of the package keeps the identity (New window, the AI tools'
  window). `PackageIdentity.ProgramPath` is the path recorded for later use in the Store's copy, the alias
  `%LOCALAPPDATA%\Microsoft\WindowsApps\axkit.exe`, since the package folder changes with every update (the AI tools'
  `claude mcp add` command line, AxKitForm.Mcp.cs). The capability that turns virtualization off,
  `unvirtualizedResources`, is restricted to games and not used.
- AX-13.6 **The listing and the release.** The listing (name axKit, category Developer tools, free, every market,
  discoverable; its texts in `docs/axkit/store-listing.md`) begins with the programs the access layers need (Store
  policy 10.2.4), discloses that axClaude shows the output of a live generative AI (11.16; the F4 report is the means
  to report content, AX-12) and links `PRIVACY.md` (10.5.1). The release workflow builds the package beside the zip and
  attaches it to the draft release (`ReleaseWorkflowTests` keeps it). The first publication goes through Partner
  Center's website by hand, as Microsoft requires (`docs/axkit/store-setup.md` section 3); every later one goes by
  itself: when a GitHub release is published and the repository variable `STORE_SUBMIT` is `on`, the workflow
  `store.yml` downloads that release's attested `AxKit-<version>.msix`, never a rebuild, and `tools/store-submit.ps1`
  posts it to Partner Center through Microsoft's submission API over HTTPS with the Partner Center application's
  three secrets of the `release` environment (a new submission as a copy of the last published one, the old package
  marked for deletion and the new one added, What's new from the release notes, the zip uploaded to the address the
  API gives, the commit, then the wait until the Store has taken it in or refused it, the result in the run's
  summary); the same script reads the state (`-Status`) and runs by hand with the credentials
  `tools/store-credentials.ps1` keeps encrypted for the maintainer's account (`store-setup.md` sections 4 to 6;
  `ReleaseWorkflowTests` keeps the switch, the environment and the secrets' names). Certification runs at Microsoft's
  afterwards and the Store updates every installed copy by itself. `winget install 9PK01L2K5000 -s msstore`
  (`StoreListing.WingetCommand`) installs the Store's copy. The zip of AX-2 and AX-3 stays, for developers and for a
  computer without the Store.

### AX-7 Accessibility rules every app follows

These are the rules of `docs/axclaude/SPEC.md` that are not about Claude; app specifications refer to them by number.

- AX-7.1 Every control has an `AccessibleName`; every action works from the keyboard; menu items have mnemonics and
  shortcuts. Native Win32 controls (EDIT, menus, status bar, `ListBox`) are used because NVDA reads them reliably;
  no WPF, no web view, no owner-drawn controls.
- AX-7.2 No second window of the app's own: questions, errors and help texts are notices drawn inside the window
  (`OverlayPanel`), never a `Form` or a `MessageBox` once a window exists. A notice's button may open the next notice
  (the update's progress, an error, axDown's changed-on-disk question after Save), which then stays in view. The
  Windows folder, file and font pickers, and the shell's own dialogs and menu an app hands an entry to (axFile's Open
  with, Properties and Windows menu, docs/axfile/SPEC.md AF-4.7), are the only separate windows. The pickers are
  shown through the window base's `ShowSystemDialog`, which marks the window while one is up (`DialogShowing`),
  since a dialog's closing activates the window before the command that opened it has gone on (2026-10-23: the
  editors said "Reloaded" in the middle of Save as, and axFile read its folder again).
  A notice's text takes the heading letters in every window (`TextJump.HeadingLetters`, the default of
  `OverlayPanel.Jumps`; the letters of axClaude's transcript): h and Shift+H the next and the previous heading, 1 to 6
  and Shift with them a heading of that level, by the text's Markdown headings; an editor's notices take the editor's
  own Ctrl+letter pairs as well (every kind of axDown's section 9, JSON's kinds in axJSON), so that its convention
  holds in its notices too. The jump is `TextJump`, the code the editors use on their own text; the result is
  announced on the notice, "No next heading" and the like when there is none, and the caret stays. A notice's text
  is searched with the bundle's find keys (AX-8.2): Ctrl+F puts the Find notice in the notice's place, and Find next
  brings the notice back with the match selected and the focus in its text (Cancel brings it back as it was); F3 and
  Shift+F3 find the next and the previous match there, "Not found" when there is none. The F1 texts are written under
  Markdown headings, one per group of keys, with a first line naming the text's own keys, so that the letters and the
  find walk them (asked 2026-10-23).
- AX-7.3 The caret never moves except as the direct result of the user's own command. The result of a navigation
  command is announced through a UI Automation notification, joined into one notification when several things arrive
  at once. Nothing speaks on a plain arrow key; information is given on demand. Two notifications raised back to back
  are not both spoken (NVDA drops the second), so what a window has to say at one moment goes as one. What a window
  announces before it has been activated for the first time waits for the activation and is said then, in order
  (`AxKitForm.Announce`, 2026-11-01): NVDA ignores a window that is not in front, and a window started by a program in
  the background comes up behind until it is activated, so what it said as it opened was lost.
  A paste into an editable text, by Ctrl+V, Shift+Insert or an Edit menu's Paste, is said once the control has done it
  (2026-10-25, the user's ask: the edit control's own paste is silent): "Pasted", "Pasted, 3 lines" for several into a
  text of several lines, "Nothing to paste as text" when the clipboard holds none; never the text itself, which may be a
  password (`AxKitForm.SayPasteAfter`). A read-only text pastes nothing and says nothing.
- AX-7.4 Enter and Escape reach a notice's buttons from every control (`AcceptButton`, `CancelButton`); handled keys
  set `SuppressKeyPress`; NVDA's own key combinations are never intercepted and no global keyboard hook is installed.
- AX-7.5 The UI thread never blocks on I/O; background work is marshalled to the UI thread and batched.
- AX-7.6 Any change to focus handling, key handling, announcements or an edit control is checked with NVDA against
  the app's test plan before a release.
- AX-7.7 Colours are the system colours and follow the Windows app theme: the entry point calls
  `Application.SetColorMode(SystemColorMode.System)` before any window exists, so every window is light or dark as
  Windows is when the app starts, and a high contrast theme applies as it always has. No app sets a colour of its own,
  with one exception: axMedia's video area is black (AM-2.4), the colour of a video surface everywhere, so that a
  picture's letterbox is not the window's colour (decided 2026-10-23).

## 5. Architecture

### 5.1 Projects and references

```
src/AxKit/            executable (net10.0-windows): Program (crash handling, version, log start, dispatch), icons
src/AxKit.Core/       class library (net10.0), no WinForms, unit tested
src/AxKit.Forms/      class library (net10.0-windows), WinForms pieces the apps use, among them the text editor shell
                     (TextEditorForm, EditorApp, EditorKeys, EditorHost)
src/AxClaude/        class library (net10.0-windows): AxClaudeApp, MainForm, ... (axClaude's window)
src/AxClaude.Core/   class library (net10.0): Vt, Transcript, Pty, Privacy, AppSettings (axClaude's pure code)
src/AxDown/          class library (net10.0-windows): AxDownApp (its EditorApp), AxDownKeys, HelpText; the window is
                     the shell's, and its pure code (TextDocument, EditorSettings, MarkdownOutline) is AxKit.Core's
src/AxJSON/          class library (net10.0-windows): AxJSONApp (its EditorApp), AxJSONKeys, HelpText (axJSON's app side)
src/AxJSON.Core/     class library (net10.0): JsonScanner, JsonParser, JsonOutline, JsonFormat (axJSON's pure code)
src/AxGit/           class library (net10.0-windows): AxGitApp, GitForm, AxGitKeys, HelpText (axGIT's window)
src/AxGit.Core/      class library (net10.0): GitRunner, GitStatus, GitLog, GitBranches, GitSettings (axGIT's pure code)
src/AxTerminal/      class library (net10.0-windows): AxTerminalApp, TerminalForm, AxTerminalKeys, HelpText (axTerminal's window)
src/AxTerminal.Core/ class library (net10.0): ShellLauncher, TerminalModel, TerminalSettings, … (axTerminal's pure code)
src/AxFile/          class library (net10.0-windows): AxFileApp, FileForm, AxFileKeys, HelpText (axFile's window)
src/AxFile.Core/     class library (net10.0): FolderListing, FileOperations, Archives, FileSettings, … (axFile's pure code)
src/AxMedia/         class library (net10.0-windows): AxMediaApp, MediaForm, AxMediaKeys, HelpText (axMedia's window)
src/AxMedia.Core/    class library (net10.0): MediaPlayer, PlayQueue, MediaFolder, MediaSettings (axMedia's pure code
                     and its one seam to Windows' media stack); MediaFiles and NaturalOrder are AxKit.Core's
src/AxPreview/       class library (net10.0-windows): AxPreviewApp (its EditorApp, read-only), AxPreviewKeys, HelpText;
                     the window is the shell's
src/AxPreview.Core/  class library (net10.0): TextSniff (whether a file reads as text; axPreview's pure code)
src/AxStart/         class library (net10.0-windows): AxStartApp, StartForm, AppsFolder, AxStartKeys, HelpText
                     (axStart's window on HotkeyForm, and the shell's Applications folder)
src/AxStart.Core/    class library (net10.0): ProgramEntry, ProgramMatch, StartSettings (axStart's pure code)
src/AxProcess/       class library (net10.0-windows): AxProcessApp, ProcessForm, AxProcessKeys, HelpText (axProcess's window)
src/AxProcess.Core/  class library (net10.0): ProcessTable, ServiceManager, StartupList, SystemInfo, ProcessSettings (axProcess's pure code)
src/AxTask/          class library (net10.0-windows): AxTaskApp, TaskForm, AxTaskKeys, HelpText (axTask's window)
src/AxTask.Core/     class library (net10.0): ItemFolder, Changes, Links, TaskRules, SmartList, TaskSettings (axTask's pure code)
src/AxPass/          class library (net10.0-windows): AxPassApp, PassForm, AxPassKeys, HelpText (axPass's window)
src/AxPass.Core/     class library (net10.0): KeePassCli, PassTree, EntryFields, PassSettings (axPass's pure code)
src/AxCalc/          class library (net10.0-windows): AxCalcApp, CalcForm, AxCalcKeys, HelpText (axCalc's window)
src/AxCalc.Core/     class library (net10.0): Calculator, NumberText, CalcEntry, CalcSettings (axCalc's pure code)
src/AxClip/          class library (net10.0-windows): AxClipApp, ClipForm, AxClipKeys, HelpText (axClip's window, on HotkeyForm)
src/AxClip.Core/     class library (net10.0): ClipHistory, ClipEntry, ClipPolicy, ClipSettings (axClip's pure code)
tests/AxKit.Tests, tests/AxClaude.Tests, tests/AxJSON.Tests, tests/AxGit.Tests, tests/AxTerminal.Tests, tests/AxFile.Tests, tests/AxMedia.Tests, tests/AxPreview.Tests, tests/AxStart.Tests, tests/AxProcess.Tests, tests/AxTask.Tests, tests/AxPass.Tests, tests/AxCalc.Tests, tests/AxClip.Tests
```

References point one way: `AxKit` references every app; an app's window project references its own `.Core`,
`AxKit.Core` and `AxKit.Forms`; an app's `.Core` references `AxKit.Core` and never `AxKit.Forms`; the shared projects
reference nothing of the apps. `tests/AxKit.Tests/LayoutTests` reads the project files and fails when this is broken,
and `InventoryTests` fails when an app lacks one of its places (5.3 M1, `adding-an-app.md`).

The .NET version is one property, `AxKitDotNet` in `Directory.Build.props`, from which every project's
`TargetFramework` is built (`$(AxKitDotNet)` for the pure projects, `$(AxKitDotNet)-windows` for WinForms), and the SDK
band is `global.json`, which the workflows read too (2026-10-23). .NET 10 is an LTS release supported until
November 2028; the move to the next LTS is those two files and a run of the tests (`docs/axkit/longevity.md`).

### 5.2 Documents

```
CLAUDE.md               the map of the bundle and its rules (short); each project has its own CLAUDE.md with its notes
README.md, CONTRIBUTING.md, SECURITY.md, PRIVACY.md, GOVERNANCE.md, CODE_OF_CONDUCT.md, THIRD-PARTY-NOTICES.md   the repository's own pages (AX-4.5)
docs/axkit/              SPEC.md (this), README.md (the zip's guide: install, the apps, uninstall), adding-an-app.md, plan.md,
                        nvda-test-record.md (every NVDA pass, by step), axfile-split.md (the inventory of the split),
                        review-2026-10-23.md (the review of the code and documents), inventory-2026-10-24.md (the copies and drift, and what is open) and longevity.md (the practices for the years ahead)
docs/<app>/             SPEC.md, user-guide.md (embedded by the app's project), nvda-test-plan.md, for each app of section 3;
                        docs/axclaude also claude-screens.md, docs/axdown also tutorial.md
```

### 5.3 Modularity rules

- M1 One app, one set of folders: `src/<App>`, `src/<App>.Core`, `docs/<app>/`, `tests/<App>.Tests`. Nothing of the
  app lives elsewhere.
- M2 References point one way (5.1), enforced by a test.
- M3 Shared code moves on the second use, unchanged where possible, one piece per commit.
- M4 Notes next to the code: `src/<Project>/CLAUDE.md` holds that project's gotchas and points to its spec. Claude
  Code reads a nested `CLAUDE.md` when it works on files in that folder. The root `CLAUDE.md` is the map and the
  bundle rules, about 60 lines.
- M5 One spec per app, complete on its own; the bundle spec covers only what crosses apps and the accessibility
  rules (AX-7), which app specs cite by number. In axClaude's code and documents, "SPEC.md" means
  `docs/axclaude/SPEC.md`.
- M6 One version, one changelog with app prefixes, one release.
- M7 An app's texts belong to the app: its user guide is embedded by its own project, its F1 text lives in its
  `HelpText`, its usage text in its `StartupOptions`.
- M8 A new app follows `docs/axkit/adding-an-app.md`.
- M9 Keys live in tables (AX-8): one bundle-wide table in `AxKit.Forms`, one table per window; a key is changed in its
  table and nowhere else.

### 5.4 The Windows APIs depended on

Every call into Windows outside WinForms is behind one file, listed here with what it is for and what to do when
Windows retires it (`docs/axkit/longevity.md` 3.6; the list is checked against Windows' deprecation notices once a
year, section 5 of that page). WinForms itself (the EDIT control, the menus, the status bar, the ListBox, the common
dialogs) is .NET's, shipped inside the executable, and every project is marked as Windows code for the analyzer
(`Directory.Build.props`).

- **ConPTY**, `kernel32` (`src/AxKit.Core/Pty/PtyHost.cs`): `CreatePseudoConsole`, `ClosePseudoConsole`,
  `CreatePipe`, `CreateProcess` with a thread attribute list, `GetStdHandle`, `SetStdHandle`, `WaitForSingleObject`,
  `GetExitCodeProcess`, `TerminateProcess`, `CloseHandle`. The console axClaude and axTerminal run their programs
  in. Windows 10 1809 and later: the one API without a fallback, hence the Windows requirement (axClaude's FR-13.1
  names it when it is missing).
- **`GetACP`**, `kernel32` (`src/AxKit.Core/TextDocument.cs`): the system code page, for a text file that is neither
  UTF-8 nor UTF-16 (axDown's AD-3.1). Stable; `Encoding.Default` is no replacement, being UTF-8 on .NET.
- **`StrCmpLogicalW`**, `shlwapi` (`src/AxKit.Core/NaturalOrder.cs`): Explorer's order of names, numbers compared as
  numbers, for axFile's entries and axMedia's queue. Stable; the fallback is a comparer of our own under the same
  tests.
- **The EDIT control's messages**, `user32` (`SendMessage` and `GetCaretPos` in `src/AxKit.Forms/EditKeys.Page.cs`,
  `EditKeys.Paragraph.cs` and `TranscriptView.cs`): line and caret positions and the scrolling of the views. As old as
  Windows; WinForms' TextBox is the same control.
- **`FlashWindowEx`**, `user32` (`src/AxKit.Forms/ConsoleForm.Signals.cs`): the taskbar flash when Claude or the
  shell wants attention while the window is in the background. Stable; without it the sounds remain.
- **The window list**, `user32` (`EnumWindows`, `GetWindow`, `GetWindowText`, `GetWindowTextLength`,
  `GetWindowThreadProcessId`, `IsIconic`, `IsWindowVisible`, `ShowWindow`, `SetForegroundWindow`, `PostMessage` in
  `src/AxKit.Forms/AxKitMenu.cs`): the axKit menu's list of the bundle's windows and the switch to one. Stable.
- **`WM_COPYDATA`**, `user32` (`SendMessageTimeoutW`, `AllowSetForegroundWindow`, `GetWindowThreadProcessId` in
  `src/AxKit.Forms/AppMessage.cs`): the hand-over from a second start to the open window of axMedia or axPreview
  (AX-7.2). Stable.
- **`RegisterHotKey`**, `user32` (with `UnregisterHotKey`, `GetForegroundWindow`, `SetForegroundWindow`,
  `FindWindowExW`, `IsWindow`, `IsWindowVisible`, `GetKeyState`, `GetWindowThreadProcessId` in
  `src/AxStart/StartForm.cs`): axStart's system-wide key and the foreground given back (AS-4.1). Stable.
- **`waveOut`**, `winmm` (`waveOutOpen`, `waveOutPrepareHeader`, `waveOutWrite`, `waveOutReset`,
  `waveOutUnprepareHeader`, `waveOutClose` in `src/AxKit.Forms/Sounds.cs`): the earcons, each a buffer queued behind
  the one playing. Legacy, kept by Windows for compatibility (`PlaySound` and .NET's `SoundPlayer` rest on it); the
  fallback is WASAPI through COM in the same file.
- **`SHFileOperationW`**, `shell32` (`src/AxFile.Core/FileOperations.cs`): copy, move, rename, delete to the Recycle
  Bin, with the shell's own progress, conflicts and undo (axFile's AF-4). Superseded by `IFileOperation` (Vista),
  still shipped; the fallback is `IFileOperation` through COM, in the same file.
- **The shell's folders and menus**, `shell32`, `shlwapi`, `user32` (`SHGetDesktopFolder`, `SHParseDisplayName`,
  `SHBindToParent`, `SHGetKnownFolderIDList`, `SHOpenWithDialog`, `ShellExecuteExW`, `StrRetToBufW`, the
  interfaces `IShellFolder` and `IContextMenu`, and the menu functions `CreatePopupMenu`, `TrackPopupMenuEx`,
  `GetMenuItemCount`, `GetMenuItemID`, `DestroyMenu`, `PostMessageW` in `src/AxFile/Shell.cs`): an entry's Windows
  menu, Open with, Properties, Eject (AF-4.7). Stable: the interfaces Explorer itself is built on.
- **`shell:AppsFolder`**, `shell32` (`SHCreateItemFromParsingName`, `SHGetKnownFolderPath`, the interfaces
  `IShellItem` and `IEnumShellItems` in `src/AxStart/AppsFolder.cs`): the Start menu's programs for axStart
  (AS-2.1). The folder's parsing name is not promised by Microsoft; the fallback is the Start menu's shortcut folders
  (the user's and the common one), which lose the Store apps.
- **`SetCurrentProcessExplicitAppUserModelID`**, `shell32` (`src/AxKit/Program.cs`): one taskbar group per app
  (AX-1.6). Stable.
- **MFPlay**, `mfplay` (`MFPCreateMediaPlayer`, the interfaces `IMFPMediaPlayer`, `IMFPMediaItem` and
  `IMFPMediaPlayerCallback` in `src/AxMedia.Core/MediaPlayer.cs`): axMedia's playback. Its documentation carries a
  deprecation notice while it is still shipped, Windows 11 included; the fallback is the Media Session (AM-D5),
  behind the same seam.
- **UI Automation notifications** (`AccessibleObject.RaiseAutomationNotification`, WinForms over
  `UiaRaiseNotificationEvent`, in `src/AxKit.Forms/Announcer.cs`): everything the apps say (AX-7.3). Windows 10
  1709 and later; NVDA has read them since 2018.
- **The package identity**, `kernel32` (`GetCurrentPackageFullName`, `GetCurrentApplicationUserModelId`,
  `GetCommandLineW` in `src/AxKit.Core/PackageIdentity.cs`): whether this process runs from the MSIX package, the
  Store's copy, which of the package's entries started it, and the console command it was started through, the
  command line's first word as Windows holds it, which .NET replaces by the program file's path (AX-13.2). Windows 8
  and later, the command line as old as Windows; stable. Without them every copy would behave as the zip's, which is
  the fallback.

## 6. Design decisions

- **B-1 One executable with a verb per app.** A self-contained .NET executable carries the runtime, about 70 MB; one
  per app would multiply the download, and two executables sharing a runtime folder would need a multi-file publish
  or an installed .NET, which the project has always avoided. One executable keeps the zip, the installer, the Apps
  entry and the updater as they are. A later app is another verb.
- **B-2 Apps are class libraries with one entry point.** `AxKit`'s `Program` stays the only `Main`; each app is
  `<App>App.Run(args)`. One process shows one window; that keeps the apps apart for the taskbar, Alt+Tab and NVDA.
- **B-3 Shared code moves on the second use, unchanged.** Nothing is generalised on a guess, and moving code as it is
  kept axClaude's behaviour identical when it became one app of the bundle (G5).
- **B-4 Names.** The bundle is axKit (Axit from 2.0.0 to 4.0.0, B-16); the apps keep their names; Start entries are
  `axKit <App>` so that the bundle name groups them and the app name is still a word that search matches.
- **B-5 Settings and logs per app, under `AxKit`.** One folder for the bundle, one file per app, so an app's settings
  code knows only its own record. The old axClaude settings file is copied, not moved, so a return to 1.7.0 still
  finds it.
- **B-6 A bridge release before the rename.** The 1.6.0 updater finds a release's zip by its `-win-x64.zip` ending
  but refuses a download without `AxClaude.exe`. 1.7.0 relaxes that check, so every installed copy can take the 2.0.0
  update instead of failing with an error.
- **B-7 The GitHub repository is renamed with the 2.0.0 release**, to `Axit` then and to `AxKit` with 5.0.0 (B-16).
  GitHub redirects the old web and API addresses, so installed 1.7.0 copies still find the releases;
  `UpdateCheck.Repository`, the installer and the guides then name the new address.
- **B-8 `axkit` without an argument starts axClaude on the current folder.** That is what `axclaude` does today, and a
  console inside a project is where the command is typed. The usage text is one `--help` away.
- **B-9 No silent default program.** Windows protects the user's choice of default program; an installer cannot set
  it without a prompt. axDown is registered as a program for its file types and offered in the right-click menu; the
  guide says how to make it the default in Settings, Default apps.
- **B-10 Plain code only.** Plain classes, static methods, records for data, one way to do each thing. No dependency
  injection, no interfaces with one implementation, no reflection beyond the two reads WinForms needs, no source
  generators, no NuGet packages. This is what lets a smaller model make a change without reading the whole code.
- **B-11 Releases go from `main`; larger work runs on a branch.** `main` is always releasable; a branch merges when
  its NVDA checks have passed. The transition to the bundle ran this way on the branch `axkit`.
- **B-12 Keys are tables, in two kinds.** Several windows will want the same letters, and a key that means one thing
  in one window and another in the next is what a screen reader user feels first. Every key is either bundle-wide or
  window-specific (AX-8); the tables make a collision visible in a test and a change a one-line edit, which is what a
  smaller model needs when the keys have to move.
- **B-13 The notices and the question area are shared, not copied.** They work and they are what the user knows;
  every window asks the same way (AX-6.4). They moved to `AxKit.Forms` unchanged when axDown needed them (2.0.0).
- **B-14 An axKit menu instead of tabs.** One window holds one file or one conversation (B-2), and axJSON will keep it
  so. Tabs in one window were considered for an editor of several files (2026-10-18) and dropped: a tab needs a
  modified state of its own, a close of its own against the window's, a title and a status bar that follow it, and
  keys whose meaning changes with the tab under the user's fingers, the thing a screen reader user feels first (B-12).
  The axKit menu (AX-9) gives the one thing tabs give, a list of what is open in one place, by recency and by app;
  Windows' own order of its windows is the recency, so nothing is stored or kept in step, and Alt+Tab is unchanged.
- **B-15 Local first; AI help is one shared, optional service (2026-10-18).** The apps are tools over files, folders,
  repositories and a shell on the user's machine, and they stay useful on a plane or behind a firewall: every
  standalone app works entirely offline, the access layers work offline except for what their programs reach on the
  user's command (AX-11.11), every network request of axKit's own can be turned off without loss of any app's utility,
  and the update check is the only call a window makes on its own (G6). AI-based editing and feedback, asked for first as
  a feature of axDown, are not built into any one app: they are a shared service every app can call from inside its
  window to get things done there, written once and optional, so that the apps stay plain and complete without it.
  The bundle is to have a shared database as well, which the service and the apps share. The service's design (how it
  reaches Claude, what it may change in a window, the database) is a specification of its own, not yet written
  (`docs/axkit/plan.md`).
- **B-16 The bundle is axKit (2026-11-05, 5.0.0).** Axit, its name from 2.0.0 to 4.0.0, is a commercial product's name
  elsewhere, so the bundle took a new one before applying for code signing (`docs/axkit/code-signing.md`): ax, the
  prefix every app carries, and kit, the kit of them; checked free of software products, packages, GitHub names and
  trademarks. Written axKit where it is shown, spoken or written, as the apps are (axClaude), and AxKit in code,
  files, paths, namespaces, registry names and the program file (`AxKit.exe`), as the apps' code is (AxClaude): the
  console command `axkit`, the settings folders `%APPDATA%\AxKit` and `%LOCALAPPDATA%\AxKit`, the Explorer submenu
  `Open with axKit`, the MCP server `axkit` with the tools `axkit_…`, the repository `KyleKeane/AxKit` (the old address
  redirects). The rename was one pass over every file, path and name, and the version a major one, 5.0.0: the program
  file, the install folder and the zip's name changed, so an installed Axit cannot update into it on its own (AX-2.6)
  and is installed over with `install.cmd`, its settings moving on the first start (AX-5.4). B-6 had a bridge release
  for 1.7.0; none could serve here, since the updater of 4.0.0 trusts a release's files only under the old repository
  address, which GitHub rewrites for every release once the repository is renamed. The recorded fixtures keep the
  name of the folder they were recorded in.
- **B-17 Every outside call is listed from the sources (2026-11-05).** Everything axKit reaches outside its own
  process, the network requests of its own code, the programs it starts and the pipes it opens, is on one page,
  `docs/axkit/outside-calls.md`, written by `tools/outside-calls.ps1` and never by hand: by what is reached (GitHub,
  Claude Code, Git, the shells, KeePassXC, the installers, Windows, the user's programs, axKit itself, the AI tools'
  pipes, the test automation's pipe), each with what it is, where it is (the program file or the endpoint), how axKit
  reaches it, what it may reach in turn and its documentation, then every call under it with its app, file and line,
  the function it stands in, the function it calls and what the call does for the user; and the functions themselves,
  where each is defined and used, and what is watched for and not used. The script finds the calls by the functions
  that reach outside (.NET's HttpClient, Process.Start, the bundle's ProcessRun and PtyHost, Windows' CreateProcess,
  ShellExecuteEx, IContextMenu.InvokeCommand, SHOpenWithDialog and StartService, the named pipes, the MCP server's
  standard input) in every `.cs` file under `src`, and reads each call's explanation from the comment it carries, on
  the line above it or at its end: `// Outside call, <kind>, <what is reached>: <what it does and why>`, the kind one
  of network, program and pipe, what is reached a key of the script's table (several, separated by commas, for a
  shared piece). A call without that comment, with the wrong kind or with an unknown key fails the script; `build.yml`
  runs it with `-Check` on every push, which also fails when the page is not what the sources give; `release.ps1`
  writes the page before the tests and commits it with the release, so it is exact at every tag. A new way of
  reaching outside joins the script's table of functions, a new program or service its table of targets, in the same
  commit as the code. Why: the user asked (2026-11-05) for the interface between axKit and the programs and services
  it works through to be defined, audited and watched automatically, so that a call added later cannot go unlisted;
  `PRIVACY.md`, `SECURITY.md` and the README stay the plain-language statements and link to the page (AX-11.11, G6).

## 7. Status

- 2.0.0 (2026-10-02): the bundle, called Axit until 5.0.0 (B-16; the names below are today's). `AxKit.exe` with
  axClaude unchanged inside it and axDown new; `AxKit.Core` and `AxKit.Forms`; the installer, publish and release
  scripts for the bundle; the repository renamed `KyleKeane/Axit`. Preceded by axClaude 1.7.0, the bridge (B-6). Both
  apps checked with NVDA by the user before the release.
- 2.1.0: the key tables (`AxClaudeKeys`, `AxDownKeys`, `BundleKeys`) with the collision test (AX-8); heading
  navigation in axDown. 2.1.1: axDown's window spoken on opening (`AccessibleRole.Window`).
- 2.2.0: the update notice with I accept all terms, install and restart, I accept all terms, install and close, and a progress bar for the download.
- 2.3.0: the in-place install with one bar for download and install and the restart by the app itself (AX-2.8,
  AX-3.2); the user ran the full path from an older build with NVDA. 2.3.1: the documents pruned after the transition.
- 2.9.0 (2026-10-18), developed on the branch `axkit` and checked with NVDA by the user: `MarkdownOutline` and
  `TextJump` shared for axDown's letters and every notice text (AX-7.2); the axKit menu in every window (AX-9, B-14)
  with About in sections and the embedded axKit guide; axClaude's Session menu and axDown's menus in the same shape;
  an enabled item in every empty list (AX-9.5).
- 2.9.1 (2026-10-18): axJSON, raw mode (`docs/axjson/SPEC.md`), on the text editor shell shared with axDown the same
  day (`TextEditorForm`, `EditorApp`, `EditorKeys`, `EditorHost`; AX-1, AX-2, AX-6.2, AX-8.3); released at the user's
  ask before its NVDA pass.
- 2.10.0 (2026-10-18): every numbered list item reads "N. name (path)" (AX-9.5, FR-8.5, AD-4.1); axJSON marked beta
  until its NVDA pass.
- 2.11.0 (2026-10-18), developed on `axkit`: axClaude's handling of the feedback drafts Claude Code queues by itself
  (the review comes up like a question, `/feedback` lists the drafts, a screen's keys from anywhere in its list, the
  edit field, Ctrl+Q to read a question or screen again; axClaude D33), the first menu of every window in the standard
  order of a File menu with axClaude's Save conversation as renamed Export conversation (AX-8.2, AD-4.1); the draft
  flow and Ctrl+Q heard under NVDA by the user, the rest covered by recordings and tests.
- 2.12.0 (2026-10-18), developed on `axkit`: axGIT, the fourth app, as a first draft (`docs/axgit/SPEC.md`; section
  3), reached from the other windows' "Show current … in ▸" submenus; the window base `AxKitForm` (AX-6.5) with the
  editor shell, axGIT and axClaude's `MainForm` on it. Heard under NVDA by the user: the notices in axClaude and
  axDown on the base, axGIT's start, views, diff, staging and notices, and the way to and from it; axGIT's commit,
  discard, switch and remote commands await their pass.
- 3.0.0 to 3.2.2 (2026-10-19 to 2026-10-21): axTerminal with the console pieces shared (AX-6.1), the in-place update
  clearing old remnants (AX-2.8), one Open with axKit submenu (AX-2.3); axFile (3.1.0) with its operations, Undo and
  pane; the delayed question focus in axClaude (3.2.1); the editors' undo and redo (3.2.2). `CHANGELOG.md` has each.
- 3.3.1 (2026-10-22), developed on `split`: the split of axFile (`docs/axkit/axfile-split.md`): axMedia, the player
  with one queue, and axPreview, the viewer as the editor shell's read-only mode, as apps of their own with a
  hand-over from a starting process to the open window (`AxKit.Forms/AppMessage`, AX-6.2); axFile lean, Enter the
  user's default program, the pane its Actions list; `MediaFiles`, `NaturalOrder` and the find in a list shared
  (AX-6.3); the dispatcher's media rule (AX-1.2) and the installer's Open with registrations for both apps (AX-2.3).
  Every plan passed under NVDA by the user the same day (`docs/axkit/nvda-test-record.md`); 3.3.0 was tagged from the
  same work and never published (a test the Release build refused).
- 3.4.0 (2026-10-22): axStart, the program launcher (section 3, `docs/axstart/SPEC.md`), its NVDA pass still owed;
  `test.ps1` running the tests as CI does (AX-4.4); the repository's pages (AX-4.5).
- Unreleased (2026-10-23), developed on `palette`: the command palette in every window (AX-10); the heading letters
  and Ctrl+F in every notice's text (AX-7.2); the review of the code and documents of 2026-10-18 to 2026-10-22
  (`docs/axkit/review-2026-10-23.md`) with its fixes, the tests run without the globalization data as the program
  runs (AX-6.1, `Accents`), and the practices for the years ahead (`docs/axkit/longevity.md`). The same day, the
  rewrites the review asked for (one console window for axClaude and axTerminal with one settings class, AX-6.1;
  the notice and focus core of the window base as one rule, AX-6.5; axClaude's wait as one record; axFile's
  operation runner and marks; axTerminal's prompt rows by identity; the smaller ones), the duplication moved into
  the shared projects, the tests asked for, and the longevity items that needed no account (the Windows APIs
  listed in 5.4, the actions pinned, the installer off the GitHub API and putting the commands on the PATH, Claude
  Code's version logged, the release rules, the lock files). Not yet checked with NVDA (the review's section 9).
- 5.0.0 (2026-11-05): the bundle renamed axKit (B-16): every name, file, path and document in one pass, the
  installer's removal of an Axit installation (AX-2.6), the folders taken over on the first start (AX-5.4), the
  repository `KyleKeane/AxKit`.
