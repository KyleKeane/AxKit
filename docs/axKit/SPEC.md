# axKit — Bundle Specification

Describes axKit, the bundle the axClaude repository became on 2026-10-02 (plan: `docs/axKit/plan.md`). Every
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
manager: `docs/axKit/plan.md`) and follow the same pattern without changes to the bundle.

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
  download the user chooses; and the report sender (AX-12, docs/axKit/reports.md), only when the user sends a report
  (F4), the report saved on the computer first and encrypted there for the maintainer's key before it goes (AX-12.3a) (axTask's GitHub lists, a third place from 2026-11-07, were removed
  on 2026-11-14: axTask is local only, docs/axKit/reports.md RP-7). A network failure is never more than a missing update or a report that
  waits for the next. An access layer reaches the network only
  through the program it runs (AX-11.11). Any AI help in the other apps is one shared, optional service (B-15), never
  a dependency of a feature that works without it.
- **G7 Three steps, four at most.** No goal takes more than four actions, and ideally none more than three (the
  maintainer, 2026-11-26: "it should not take more than 4 actions at the most to accomplish a goal and in an ideal
  world nothing will require more than three steps"). An action is one key, one chord or one choice in a list; typing
  a name counts as one. A goal that needs more is a fault of the design, to be shortened, never a matter for the guide.

Non-goals: a plugin system or loading apps from separate files (apps are compiled in); more than one window per
process; an installer that needs administrator rights; setting Windows default programs silently (Windows does not
allow it, see B-9).

## 3. The apps

The apps are named ax and a word, the ax in lower case and the word as it is written: axClaude, axDown, axJSON, axGIT, axTerminal, axFile, axMedia, axPreview, axStart, axProcess, axTask, axPass, axCalc, axClip (2026-10-23, the user's rule; until then AxClaude and the like). A screen reader then says the name as ax and a word it knows, and an acronym keeps its capitals. Nothing depends on the case: the Start menu, the command palette, axStart and the console commands (xclaude) find a name typed in any case, and the code that tells the apps apart by name (a window's title, the Recently closed list, the hand-over) ignores case, so an entry saved under the old spelling still opens. The code keeps the C# spelling of its projects, folders and classes (`src/AxClaude`, `AxClaudeApp`), which no screen reader speaks; `Dispatch.App` holds both, `Name` and `Project`.

- **axClaude** (built): `AxKit.exe claude [<folder>] [options]`, Start entry `axKit axClaude`, Open with axKit on
  folders, command `axClaude`. Specification `docs/axClaude/SPEC.md`.
- **axDown**: `AxKit.exe down [<file>]`, Start entry `axKit axDown`,
  Open with axKit on files and Open with for `.md`, `.markdown`, `.txt`, command `axDown`. Specification
  `docs/axDown/SPEC.md`, guide `docs/axDown/user-guide.md`, test plan `docs/axDown/nvda-test-plan.md`.
- **axJSON**: `AxKit.exe json [<file>]`, Start entry `axKit axJSON`, Open with axKit on `.json` files and Open with
  for `.json`, command `axJSON`. The text editor shell axDown is, with JSON's navigation keys and a Format command.
  Specification `docs/axJSON/SPEC.md`, guide `docs/axJSON/user-guide.md`, test plan `docs/axJSON/nvda-test-plan.md`.
- **axGIT** (first draft, 2026-10-18): `AxKit.exe git [<folder or file>]`, Start entry `axKit axGIT`, Open with axKit
  on folders, command `axGIT`. A git manager for one repository, a window per repository, opened from the other
  windows' "Show current … in ▸" submenus on the repository of the folder or file in hand. Specification
  `docs/axGIT/SPEC.md`, guide `docs/axGIT/user-guide.md`, test plan `docs/axGIT/nvda-test-plan.md`.
- **axTerminal** (first draft, 2026-10-18): `AxKit.exe terminal [<folder>]`, Start entry `axKit axTerminal`, Open with
  axKit on folders, command `axTerminal`. A shell (PowerShell, or cmd) in axClaude's window: the output as a
  transcript with exchange blocks, the shell's integration marks for Done and Failed. Specification
  `docs/axTerminal/SPEC.md`, guide `docs/axTerminal/user-guide.md`, test plan `docs/axTerminal/nvda-test-plan.md`.
- **axFile** (first draft, 2026-10-19): `AxKit.exe file [<folder or file>]`, Start entry `axKit axFile`, Open with axKit
  on folders and on files, command `axFile`. A file manager for one folder as one list, Enter the verb (a folder
  here, a file in the user's default program, as in File Explorer), the file operations with Undo and an Actions
  list of every operation; its player and preview became axMedia and axPreview on 2026-10-22
  (the split's inventory of 2026-10-22, in the git history). Specification `docs/axFile/SPEC.md`, guide `docs/axFile/user-guide.md`, test plan
  `docs/axFile/nvda-test-plan.md`.
- **axMedia** (2026-10-22): `AxKit.exe media [<file or folder>...] [--add] [--new]`, Start entry `axKit axMedia`, Open
  with axKit on sound and video files and Open with for their kinds, command `axMedia`. A player with one queue, the
  player axFile carried until the split (the split's inventory of 2026-10-22, in the git history); a start with paths hands them to the open
  axMedia window (`AppMessage`). Specification `docs/axMedia/SPEC.md`, guide `docs/axMedia/user-guide.md`, test plan
  `docs/axMedia/nvda-test-plan.md`.
- **axPreview** (2026-10-22): `AxKit.exe preview [<file>] [--new]`, Start entry `axKit axPreview`, Open with axKit on
  every file and Open with for every file type (the text kinds alone up to 3.7.1), command `axPreview`. A read-only viewer for
  one text file: the text editor shell in its read-only mode, with axDown's navigation keys; a start with a file
  hands it to the open axPreview window. Specification `docs/axPreview/SPEC.md`, guide `docs/axPreview/user-guide.md`,
  test plan `docs/axPreview/nvda-test-plan.md`.
- **axStart** (2026-10-22, 3.4.0): `AxKit.exe start [--hidden]`, Start entry `axKit axStart`, command
  `axStart`, no File Explorer entry. A program launcher: a background process from sign-in (the Run key) with a
  global hotkey, Ctrl+Shift+Space unless the user chose another, that shows one search field over the Start menu's
  programs, Down and Up through the matches with the caret kept in the field. Specification `docs/axStart/SPEC.md`,
  guide `docs/axStart/user-guide.md`, test plan `docs/axStart/nvda-test-plan.md`.
- **axProcess** (2026-10-23, branch `axProcess`): `AxKit.exe process`, Start entry `axKit axProcess`, command `axProcess`,
  no File Explorer entry. What runs on the computer as one list with three views (processes, services, startup
  programs), read on F5 and never on a timer; a process ended after a question, a service started or stopped, a
  startup program of the user's own turned off, on again or removed after a question (2026-11-14).
  Specification `docs/axProcess/SPEC.md`, guide `docs/axProcess/user-guide.md`, test plan
  `docs/axProcess/nvda-test-plan.md`.
- **axTask** (2026-10-24, branch `axTask`): `AxKit.exe task [<folder>]`, Start entry `axKit axTask`, command `axTask`,
  folders in File Explorer's Open with axKit submenu. Tasks and lists as a folder of JSON files, one per item, in a
  navigator like axFile's with a detail pane of the item's typed fields; the item apps' shared shape is
  `docs/axKit/item-apps.md`. Specification `docs/axTask/SPEC.md`, guide `docs/axTask/user-guide.md`, test plan
  `docs/axTask/nvda-test-plan.md`.
- **axPass** (2026-10-24, branch `axTask`): `AxKit.exe pass [<database>]`, Start entry `axKit axPass`, command `axPass`,
  `.kdbx` files in File Explorer's Open with axKit submenu and Open with. A KeePass database in axTask's two panes,
  through KeePassXC's command-line tool, which must be installed. Specification `docs/axPass/SPEC.md`, guide
  `docs/axPass/user-guide.md`, test plan `docs/axPass/nvda-test-plan.md`.
- **axCalc** (2026-10-23, branch `axCalc`): `AxKit.exe calc`, Start entry `axKit axCalc`, command `axCalc`, no File
  Explorer entry. A calculator as one expression field and a history list: Enter says the answer, the history keeps
  every answer to read back, copy and recall, and Ctrl+I says an answer in hexadecimal, binary and octal.
  Specification `docs/axCalc/SPEC.md`, guide `docs/axCalc/user-guide.md`, test plan `docs/axCalc/nvda-test-plan.md`.
- **axClip** (2026-10-23, branch `axClip`): `AxKit.exe clip [--hidden]`, Start entry `axKit axClip`, command `axClip`, no File
  Explorer entry. A clipboard history behind a hotkey (Ctrl+Shift+Comma unless the user chose another): a background
  process from sign-in, as axStart, on the bundle's `HotkeyForm`, with one search field over the text of every copy;
  Enter pastes into the program that was in front. Specification `docs/axClip/SPEC.md`, guide
  `docs/axClip/user-guide.md`, test plan `docs/axClip/nvda-test-plan.md`.
- **axMCP** (asked and built 2026-11-09; an access layer, AX-11, for MCP servers): `AxKit.exe mcpclient [<folder>]`, Start entry `axKit axMCP`, command `axMCP`: the MCP servers Claude Code knows on this computer and the user's own, in the item apps' window (`item-apps.md`); a server started by hand as a child process on its standard input and output shows its tools, resources and prompts, a tool's arguments are the detail pane's fields, Ctrl+Enter calls it and the result is a notice text; remote servers are listed and not started (its MP-D2). Specification `docs/axMCP/SPEC.md`, guide `docs/axMCP/user-guide.md`, test plan `docs/axMCP/nvda-test-plan.md`.
- **axHelp** (asked 2026-10-23 as AxAsk, built 2026-11-09; rebuilt in 5.7.0 as two hotkeys into one conversation in axClaude, with axDescribe, asked as AxSee, merged in; an access layer, AX-11, for Claude Code): `AxKit.exe help [--hidden | <picture>]` (`describe`, the former verb, the same), Start entry `axKit axHelp`, command `axHelp`: a background process whose hotkey, Ctrl+Shift+Slash (the question mark's key, AX-8.11), brings up the conversation, an axClaude window on Claude's working folder titled axHelp, on a model of axHelp's own, and whose second hotkey, Ctrl+Shift+Period, puts a picture of the window in front into it (`docs/axHelp/SPEC.md`).

- **axAutomate** (asked 2026-11-10, built 2026-11-11 as a viewer first): `AxKit.exe automate [<folder>]`, Start entry `axKit axAutomate`, command `axAutomate`: the workflows that drive axKit's windows, the automated checks of every app first, read step by step in the item apps' window (`docs/axAutomate/SPEC.md`).
- **axTemplate** (asked 2026-11-23, its browser and its launcher built the same day): `AxKit.exe template [--launcher | --hidden]`, Start entry `axKit axTemplate`, command `axTemplate`: the user's templates, each an item of the item format in one folder, with a browser on the item apps' window and a launcher behind a hotkey (Ctrl+Shift+Semicolon unless the user chose another), a background process as axClip's, that puts a template into the program in front (`docs/axTemplate/SPEC.md`). The one app with two windows, each of a bundle type (TP-D8).
## 4. Functional requirements

### AX-1 Launch and dispatch

- AX-1.1 The first argument may be a verb naming an app: `claude`, `down`, `json`, `git`, `terminal`, `file`, `media`, `preview`, `start`, `process`, `task`, `pass`, `calc`, `clip`, `mcpclient`, `help`, `automate`, `template` (`describe` starts axHelp since 5.7.0; the one list is `Dispatch.Apps`). The rest of the arguments go to that app
  unchanged, so `AxKit.exe claude C:\src --record x.vt` is what `AxClaude.exe C:\src --record x.vt` was.
- AX-1.2 Without a verb, a first argument that is an existing folder goes to axClaude and an existing file to axDown,
  or to axJSON when its extension is `.json`, to axPass when it is `.kdbx`, or to axMedia when it is a sound or video
  file (`MediaFiles.IsMedia`),
  with the remaining arguments. A path that does not exist is reported with the usage text (a message box, since no
  window exists yet, as `--help` does today).
- AX-1.3 Without any argument, `AxKit.exe` starts axClaude on the current folder, which is what `axClaude` does today
  (B-8). axClaude keeps its own rule for a current folder that is the program's own folder.
- AX-1.4 `--help` and `--version` answer for the bundle and list the verbs; `AxKit.exe claude --help` is the app's own
  usage text. Two verbs are not apps: `mcp`, the AI tools' broker an MCP client starts, with no window
  (`docs/axKit/mcp.md` MC-1.1); and `apps`, with no window either, which writes `Dispatch.Apps` as JSON to standard
  output (each app's name, verb, project, Start menu description, File Explorer submenu entries, file types and Open
  with types), which `install.ps1` builds its entries from (AX-2.2, AX-2.3; `docs/axKit/app-kit.md` AK-4.1). The
  installation keeps that listing as `apps.json`, which an uninstall reads when the program cannot be run.
- AX-1.5 The dispatch is pure code in `AxKit.Core` (`Dispatch`): arguments in, app name and arguments out. It is unit
  tested and has no knowledge of the apps beyond their names.
- AX-1.6 Each process sets a Windows application id before its window opens (`AxKit.AxClaude`, `AxKit.AxDown`, `AxKit.AxJSON`, `AxKit.AxGit`, `AxKit.AxTerminal`, `AxKit.AxFile`, `AxKit.AxMedia`, `AxKit.AxPreview`, `AxKit.AxStart`, `AxKit.AxProcess`, `AxKit.AxTask`, `AxKit.AxPass`, `AxKit.AxCalc`, `AxKit.AxClip`, `AxKit.AxMCP`, `AxKit.AxHelp`, `AxKit.AxAutomate`, `AxKit.AxTemplate`), so the
  taskbar shows the apps apart and Win+number keys reach the right one.
- AX-1.7 An app is a class library with one entry point, `static void Run(string[] args)` on a class named
  `<App>App`, plus a `Usage` text. `AxKit`'s `Program` only dispatches; the entry point is one call into
  `AxKit.Forms/AppHost.Run`, which owns the log, the crash handling (an exception in a UI handler is logged and shown
  in the window as a notice, or in a message box when no window exists yet), the start and exit lines in the log,
  `--help` and `--version`, and runs the window the app makes from the rest of its arguments (`AppHost.OnePath`
  for an app that takes one file or folder, `AppHost.BadCommandLine` for the rest).
- AX-1.8 In the Store's copy (AX-13), a process started from one of the package's entries takes the entry's verb when
  the command line carries none (`Dispatch.WithApplicationVerb` with `PackageIdentity.ApplicationId`, before `Decide`).
- AX-1.9 **A background app that does not answer** (2026-11-16, `repairs.md` R-65; `AxKit.Core/BackgroundStart`,
  tested in `BackgroundStartTests`, and `AxKit.Forms/AppHost.OneCopy`, the one start of axStart, axClip, axHelp and axTemplate's launcher).
  Each runs one copy, hidden, held by a named mutex; a second start hands its wish to the first copy's window ("show",
  or axHelp's picture) and ends. When the first copy hung, its hotkey was dead and starting the app again did
  nothing, without a word. Now the second start waits up to three seconds for the first copy's window (a copy that
  is still starting has none yet), hands over, and takes the hand-over's answer within its five seconds as the sign
  that the copy lives; a start with `--hidden`, which has nothing to hand over, sends an empty message instead.
  - The copy answered: this start ends, as before.
  - The window is there and did not answer, or there is no window although the mutex is held: one message box (no
    window of the app exists yet, AX-7.2), titled with the app's name: "axClip is running but does not answer. End
    it and start again?", Yes and No, No the default. No, Escape or closing it ends this start and changes nothing.
  - Yes ends every process of this very program file (`ThisProgram.Runs`, the image path) in this Windows session
    whose command line, as Windows keeps it, names the app's verb as its first argument (`ThisProgram.CommandLineOf`,
    `BackgroundStart.IsCopyOf`): never a process by its name alone, never another app of the bundle, never another
    program. Then this process takes the mutex and starts as the app. If the mutex is still held after three seconds
    (the copy runs another program file, or would not end), a message box says "axClip could not be ended, so it was
    not started again. Sign out of Windows and in again, or end its AxKit.exe in Task Manager, then start axClip."
  - Nothing is asked by a start with `--hidden` (the sign-in's: nobody asked for anything) or inside a test run;
    such a start ends, and the log says that the copy did not answer.
  - What the ended copy had only in memory is lost: axClip's history when it is not kept on disk.
  - The limits: a copy whose command line does not carry the verb is not ended and the second message box comes: a
    process the Store's package started from an entry without parameters (AX-1.8), and an axHelp started as
    `AxKit.exe describe`, its former verb. Not checked against a copy that really hangs by any script: a test run
    never asks.

### AX-2 Installation

- AX-2.1 `install.ps1` installs for the current user into `%LOCALAPPDATA%\Programs\AxKit` with no administrator
  rights, from the extracted zip or from `publish\win-x64`; `install.cmd` runs it with the execution policy bypassed
  and, downloaded alone, fetches the latest release first. `-Uninstall` removes everything of axKit and keeps the
  settings and the logs. This is axClaude's installer today (`docs/axClaude/SPEC.md` FR-8.7, D18) with the names changed.
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
- AX-2.4 Console commands: `axKit.cmd` and one `<app>.cmd` per app (`axClaude.cmd`, `axDown.cmd`, `axJSON.cmd`,
  `axGIT.cmd`, `axTerminal.cmd`, `axFile.cmd`, `axMedia.cmd`, `axPreview.cmd`, `axStart.cmd`, `axProcess.cmd`, `axTask.cmd`, `axPass.cmd`, `axCalc.cmd`, `axClip.cmd`) in `%USERPROFILE%\.local\bin`, one line each, passing their arguments to `AxKit.exe`
  and `AxKit.exe <verb>`. They work from cmd and from PowerShell.
- AX-2.5 Settings, Apps lists one entry, `axKit`, with the version, the publisher and an Uninstall that runs
  `install.cmd -Uninstall`.
- AX-2.6 Installing over an installation of an earlier name removes it completely first: Axit, the bundle's name up to
  4.0.0 (B-16): its folder `%LOCALAPPDATA%\Programs\Axit`, its Start menu entries (`Axit <App>` by title, by name or by
  project, as the versions made them), `axit.cmd`, the `Open with Axit` keys, the ProgIDs `Axit.<App>` with their Open
  with values, the sign-in values `Axit axStart` and `Axit axClip`, and its Apps entry; and axClaude 1.x (any version):
  its folder, Start entry, `axClaude.cmd`, registry keys and Apps entry. The installer says what it found, and stops an
  `Axit.exe` running in the background as it stops `AxKit.exe`. Axit's settings and logs move on the first start
  (AX-5.4); the axClaude 1.x settings file is left where it is (AX-5.2). An
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
  **An install adds no start at sign-in and opens no window of the program** (2026-11-14, after KeePassXC, installed
  this way, started its window at every sign-in of the maintainer's computer and blocked the screen reader there):
  KeePassXC's installer writes a value `KeePassXC` under the user's Run key and offers to start KeePassXC when it ends
  unless told otherwise (its user guide, "Installer Options": `AUTOSTARTPROGRAM` and `LAUNCHAPPONEXIT`, both on by
  default), so winget passes it `AUTOSTARTPROGRAM=0 LAUNCHAPPONEXIT=0` beside its own silent options (`--custom`,
  `ProgramInstall.InstallerArguments`; the other programs have none). And whatever an installer's options are named:
  for a program with a window of its own (`ProgramInstall.ProgramFile`, KeePassXC's) the window reads the user's Run
  key before and after the install (`SignInEntries`, read only), and when a value that was not there now names the
  program's file (`AddedSignInEntry`), the end of the install is a notice, "KeePassXC is installed", that says so and
  where to turn it off: "KeePassXC's installer set it to start with Windows. axPass does not need that. To turn it
  off: KeePassXC, Tools, Settings, General, Automatically launch KeePassXC at system startup." The app goes on when
  the notice closes; while another notice shows, and after an install that failed, the same words are said or added
  to the notice. axKit never removes the value: the setting is the user's and the program's.
- AX-2.10 The Microsoft Store's copy installs nothing of the above itself: its package declares the Start menu entries,
  the console aliases, the file types and the startup tasks, and the Store installs, updates and removes it (AX-13).

### AX-3 Updates

- AX-3.1 One version for the bundle, `<Version>` in `src/AxKit/AxKit.csproj`. A newer GitHub release is offered as
  axClaude does (`docs/axClaude/SPEC.md` FR-1.10, D25): asked once at startup when the setting is on, a notice with
  the release notes. The setting is the bundle's, one switch for every app (the maintainer's answer of 2026-11-24,
  "One switch for all"): Options, Check for updates at start (the axKit menu's until 2026-11-26), on by default, in `%APPDATA%\AxKit\bundle.json`
  (`AxKit.Core/BundleSettings`); until then each app had its own in Options, General, and the first time the bundle's
  file is not there the switch starts off if any app had turned its own off (the file's second switch, Keep a log of
  events, is AX-9.1's). A file that is there and does not read whole (held by another program, damaged, one value
  not understood) gives no check at start for that run (`BundleSettings.Load`, 2026-11-27): the default is on, and a
  network contact the user turned off never comes back through a damaged file (AX-11.11); the menu item then shows
  unchecked, and Check for updates by hand still asks. The notice has
  four buttons: I accept all terms, install and restart (Enter), I accept all terms, install and close, Open release page, Later
  (Escape). A development build (Debug, as `run.ps1` makes it) does not ask at startup, since its version is whatever
  the working tree said when it was last built; Check for updates still asks. Every window's Help menu ends with Check for updates (axKit <version>), always, naming the version in use
  and asking GitHub again every time (a check whose first try fails because GitHub's name is not found or no
  connection is made asks once more two seconds later before it says "GitHub could not be reached", since right after
  the Microsoft Store's update the network can still be coming up, `UpdateCheck.FetchWithRetryAsync`, `KyleKeane/AxKit-reports issue 11`;
  the Store's copy's notice of a failure adds that the Store installs its updates by itself and the check only asks
  GitHub for the newest version), Update to axKit <version> below it while a newer release is known, and About, the
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
  question after I accept all terms, install and restart (axClaude's Keep working, axTerminal's Keep running, axDown's Cancel in the save question) keeps the window
  on the old program file and says "Update installed. It starts with the next axClaude" (or axTerminal, axDown).
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
  the download and the hand-over are `AxKit.Core/Updates/Updater`. **No way back** (`Updater.VersionRefusal`,
  `docs/axKit/repairs.json` R-23): the signature covers the zip and not the number its release carries, so once the
  signature holds and the zip is extracted, the program inside is asked its own version, which must be the one the
  release names and newer than the one running; otherwise the download is deleted and the notice says which of the
  two failed. An older signed zip offered under a newer number is thus never installed. **Signed releases** (2026-10-24, the user's
  decision; security audit S-27 to S-31): every release's zip is signed with the release key, an ECDSA P-256 key that
  lives on the maintainer's development computer, encrypted for its Windows account (`tools/release-key.ps1`).
  Since 2026-11-15 (the maintainer's decision, S-27's note) a passphrase-encrypted backup of it lies beside its
  passphrase in the private repository `KyleKeane/AxKit-keys`, from which a second development computer imports it:
  the key is then protected by the maintainer's GitHub sign-in alone, which the original decision had avoided. The release workflow leaves a draft (a build job that can only read, then a job that uploads and attests
  the build's provenance); `release.ps1` then runs `tools/sign-release.ps1`, which checks the provenance, signs the
  zip's SHA-256, uploads `<zip>.sig` and publishes. The updater takes assets only from this repository's releases
  address, downloads afresh every time, and extracts nothing unless the zip matches GitHub's recorded checksum and its
  signature checks against the public half in `AxKit.Core/Updates/ReleaseSigning`; `install.cmd` alone does the same
  check in Windows PowerShell, into a folder of a random name. A release without a signature is never installed by
  the apps. Losing the key means one release installed by hand with a new key's public half in it.
  **Not signed for Windows.** `AxKit.exe` in the GitHub release carries no Authenticode signature, and every
  release's notes say so (`tests/AxKit.Tests/ReleaseWorkflowTests.cs` keeps the sentence); Windows' Smart App
  Control may refuse it, and an update it refuses stops without changing anything (AX-3.6). The signed copy of axKit
  is the Microsoft Store's, which Microsoft signs after certification (AX-13; `docs/axKit/store-distribution.md`
  4.4). Azure Artifact Signing was set up for the release workflow on 2026-11-06 and retired on 2026-11-09 before it
  signed a release, on the user's decision to sign through the Store (the code signing record of 2026-11, in the git history). The
  release key signs the zip as before.
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
  `release.ps1 -Rehearse` is run before a release: it makes the checks and asks git and GitHub the real run's
  questions, through the same functions, about the build of main's newest commit and the newest published release,
  and changes nothing (`docs/axKit/commands.md`; since 2026-11-14, when 5.8.0 met two faults in those parts).
- AX-4.2 `publish.ps1` publishes `src/AxKit` self-contained, single file, into `publish\win-x64` with `install.cmd`,
  `install.ps1`, `LICENSE`, `NOTICE.md`, `PRIVACY.md`, `THIRD-PARTY-NOTICES.md` and `docs/axKit/README.md` as `README.md`,
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
  policy linked from every release page (the code signing record of 2026-11, in the git history); issue forms in
  `.github/ISSUE_TEMPLATE` (a bug asks for the app, the versions and the test plan step), a pull request checklist,
  and Dependabot (`.github/dependabot.yml`) keeping the workflow actions and the test packages current, monthly; the
  apps themselves have no packages (G4, B-10).

### AX-5 Settings, logs and paths

- AX-5.1 Each app has its own settings file, `%APPDATA%\AxKit\<app>.json` (`axClaude.json`, `axDown.json`), and its own
  log, `%LOCALAPPDATA%\AxKit\logs\<app>.log`, each line with the time, the level and the process id in brackets, so
  that two windows of one app writing one log can be told apart (2026-10-23). The updater's log is `%LOCALAPPDATA%\AxKit\logs\update.log`. The bundle
  has one file of its own, `%APPDATA%\AxKit\recently-closed.json`, the Recently closed list of the Window menu (AX-9.4).
  **Every file and folder name that carries an app's name is written in the app's own case** (5.6.0, the user's ask
  of 2026-11-10: NVDA reads an all-lower-case `axdown` oddly): the settings and log files (`axDown.json`,
  `axDown.log`), the console commands and the package's aliases (`axDown.cmd`, `axDown.exe`), the documents' folders
  (`docs/axDown`), the test folders and the run scripts (`scripts to run-individual-apps\run-axDown.ps1`). Windows finds a file typed in any case;
  the spelling is what a screen reader says in a folder listing. A settings or log file an earlier version left in
  lower case is renamed to the app's case the first time it is read (`FileCase`). The bundle's own folders and the
  program file keep `AxKit`, the repository's spelling.
  Three files name this program file for Claude Code and so never roam, all in `%LOCALAPPDATA%\AxKit`:
  `claude-mcp.json`, the AI tools for every axClaude window's Claude Code (docs/axKit/mcp.md MC-6.9), `claude-hooks.json`
  (2026-11-15), the hooks axClaude registers for each start of Claude Code (docs/axClaude/events-and-grounding.md
  EG-2.2), and `claude-ask.json` (2026-11-17), which names `AxKit.exe ask` as the program Claude Code asks its
  questions of when axClaude runs it over its stream (docs/axClaude/stream-mode.md SM-7.3; it holds nothing of a
  window's pipe); each is written before a start when it is missing or names another program file.
- AX-5.2 (Withdrawn on 2026-11-26.) Until then axClaude copied axClaude 1.x's `%APPDATA%\AxClaude\settings.json` to
  `axClaude.json` on its first start under axKit; no install of 1.x is left to take over.
- AX-5.3 The load-and-save pattern of a settings file (JSON, a missing or unreadable file falls back to the defaults
  and the error is reported once) is one piece of shared code, `AxKit.Core/SettingsFile`; each app's settings class is
  its own plain record of properties. A file that is not valid JSON (a typo after Options → Developer → Open settings file) is
  copied to `<app>.json.bad` before the defaults are used, and the error names the copy: the app saves its settings
  soon after loading, which would otherwise write the defaults over every other setting in the file. Two cases lose
  less than that (the resilience audit of 2026-11-16, `repairs.md` R-51 and R-52). A file one or more of whose values
  do not read (a word where a number belongs, a kind that does not exist) loses those values alone: each is left at
  its default and named in the error, the rest of the file is loaded, and the copy is kept as before; the window says
  before the error "The settings file was read, except for the settings named here.", and "The settings file could
  not be read, so the defaults are used." only of a file nothing could be taken from (`SettingsFile.Heading`). A file that is
  there and cannot be opened (another program holds it: a sync client, a backup, a virus scanner) is tried again
  three times, a tenth of a second apart; if it still cannot be opened, the defaults serve that run and are never
  saved over it: `SettingsFile.Save` passes that path over until a later load has read the file, and the error says
  that settings are not saved in this run and to close the window and start it again.
- AX-5.4 The first start after the rename (B-16, 5.0.0) moves what Axit, the bundle's name up to 4.0.0, kept in
  `%APPDATA%\Axit` and `%LOCALAPPDATA%\Axit` into the AxKit folders (`AppPaths.TakeOverOldFolders`, called by `Program`
  before anything reads a setting), entry by entry: a file the new folder lacks is moved, one it has stays, a subfolder
  is taken over the same way, and the old folder goes once it is empty, so that a start that could not move everything
  (a file the old program still held) finishes the next time. The settings, the recently closed list, the consents,
  the AI tools' pairings and their key, the logs and the audit log all come along. Never in a test run. Tested in
  `AppPathsTests`.
- AX-5.5 In the Store's copy the same paths are Windows' to redirect: a new file under AppData lands in the package's
  private folder and goes with the package when it is uninstalled, an existing one is modified in place (AX-13.5).
- AX-5.6 **The seal on the settings that decide what is run or what Claude may read** (2026-11-16, `repairs.md` R-53;
  `AxKit.Core/SettingsSeal`, the one piece, tested in `SettingsSealTests` and in each app's tests). Such a setting
  counts only as axKit wrote it. The sealed settings: axTerminal's `shell`, `customShell` and `userShells`
  (docs/axTerminal/SPEC.md AT-8.1a); axGIT's trusted repositories (docs/axGIT/SPEC.md AG-8.1a); AI help's `enabled`,
  `folders`, `claudePath` and `workingFolder` (docs/axKit/ai-help.md AH-1.1a); axClaude's `claudePath`
  (docs/axClaude/SPEC.md Appendix C); axMCP's `servers` (docs/axMCP/SPEC.md MP-8.2); axFile's `programs`
  (docs/axFile/SPEC.md AF-8.1a); and the agreements in `consent.json` (AX-11.5). The AI tools' setting has had its own
  seal since 3.7.0 (docs/axKit/mcp.md MC-6.1), with its own file and key, unchanged; it takes the HMAC from this piece.
  axTask's accepted action keys (docs/axTask/SPEC.md TK-7a.3, `repairs.md` R-72) use the same key another way: each
  entry of `acceptedActionKeys` is itself a seal, of one list's key and action, so there is no field to refuse and
  an entry that is no seal of this install's simply accepts nothing.
  - The seal is the file's key `seal`: an HMAC-SHA256, under a key of 32 random bytes kept with Windows' protection
    for this user (DPAPI), over the file's name and the sealed fields in a fixed order, each with its length in front,
    so that no field can run into the next. Each settings class lists its sealed fields in one small method
    (`SealedText`) and writes the seal at every save, so a change through axKit's own menus is sealed as it is saved.
  - At every load the seal is checked. When it checks, nothing changes. When it does not, the sealed fields are put
    to their defaults and not used, every other setting of the file is kept, and the load's error text says in one
    sentence what was not used and where to add it again ("Your shells in axTerminal's settings were changed outside
    axKit and are not used. Add them again from Session, Add a shell."); the window says it the way it says a file
    that could not be read (a notice at its first showing, a system line in the console windows, AX-5.3) and without
    the words "could not be read", and the log holds it once. The window's next save writes the file as it is now in
    use, sealed, so the sentence is heard once. Sealed fields that are all at their defaults need no seal: there is
    nothing in them to refuse.
  - **A file from before the seal.** A file that carries no seal and that this install has never sealed is accepted
    as it is and sealed at its next save, so that the shells, repositories, folders and agreements of 5.9.0 and
    earlier stay. So that taking the seal out of a file is no way around it, the names of the files this install has
    sealed are kept with the key, in the one file `%LOCALAPPDATA%\AxKit\settings-seal.key`, whole under Windows'
    protection: a file on that list without its seal, or with a wrong one, was changed outside axKit. A key file that
    is there and cannot be read (made for another user or computer, damaged) refuses every sealed setting; the next
    save makes a new key, and from then on every file counts as sealed before. A sealed file on a computer without
    the key file (the settings roam, the key does not) is refused the same way.
  - **Changing a sealed setting by hand** (Options, Developer, Open settings file) is therefore refused at the next
    start, with the same sentence: it is set in the app's menus. AI help's `claudePath` and `workingFolder` and
    axClaude's `claudePath` when Claude Code is found have no menu item yet; until they have, they can be set by hand
    only in a file axKit has not sealed yet.
  - **The limit.** A program running as the user can ask Windows for the same key; the seal holds against a file
    edited by another program or by hand, a profile copied or synced from elsewhere, and an AI tool writing files.
    Deleting the key file together with the seals puts the files back to "from before the seal"; that takes a program
    that knows axKit, not a file changed in passing.
  - A test never touches the user's key: for a settings file outside `%APPDATA%\AxKit` the key file is beside the
    settings file (`SettingsSeal.KeyFileOf`), and a test run's home holds both in one folder.
- AX-5.7 **Two windows of one app keep each other's settings** (2026-11-16, `repairs.md` R-62; `SettingsFile.Save`,
  `SettingsFile.Merged`, tested in `SettingsFileTests` and `TerminalSealTests`). `SettingsFile` remembers, per file,
  the settings of this window as JSON: as it loaded them, then as each of its saves was given them. A save reads the
  file as it is now, and for each top-level setting it writes the file's value, unless this window changed the
  setting, which is when its value differs from the remembered one. So a favourite, a shell, a trusted repository or
  a server added in one window is no longer lost when a window opened earlier saves anything; before, each save wrote
  the whole of one window's memory over the file. Nothing is asked and nothing is said.
  - The limits. A setting is taken whole: a list that both windows changed is this window's list, and the other
    window's addition to that same list is lost; lists are not merged. A window does not take the other's change
    into its own memory, so it shows it only after its next start. A setting the file no longer has keeps this
    window's value. A member the file gained that this window never held is kept; one this window held as loaded
    and writes no longer (axGIT's `trustedRepositories` of before the seal) stays gone.
  - A file that is not a JSON object, or one of whose values does not read, gives nothing: this window's settings
    are written as they are, as before.
  - Under the seal (AX-5.6): nothing at all is taken from a file whose sealed fields do not check (the file as found
    is kept as `.changed`), so a setting written into the file while a window is open is not carried into that
    window's save. What is written after a merge is sealed anew over the merged fields, and checks at the next load.
- AX-5.8 **Settings a version does not know are kept** (2026-11-16, `repairs.md` R-67). The base of the windows'
  settings classes (`WindowSettings`), the consent list and the recently closed list each hold the members of their
  file that this version has no property for (`OtherMembers`, the serializer's extension data) and write them back
  as they were at every save, so a setting a newer version added survives an older copy's save. It helps from this
  version on: a copy older than this one still drops them, the seal among them (the file then counts as changed
  outside axKit, AX-5.6). Such members are under no seal. The two lists are read again as they are written, and
  only their entries replaced. A setting an app itself retired is not such a member: the app takes it out as it
  loads (axClaude's `backgroundPulseSeconds`), so it goes with the next save. AI help's settings, the AI tools'
  settings and the reports' settings are not on that base and keep only what they know.
- AX-5.9 **A settings file that cannot be written is said** (2026-11-16, `repairs.md` R-63). When a save fails (a
  read-only file, a full disk, a folder that refuses), the window says once in a run, as a notice, or as a system
  line in the console windows: "Settings could not be saved: <the system's reason>. What you change lasts until this
  window closes." (`SettingsFile.NotWritten`). Every failure is logged; the save of a closing window says nothing.
  Until this the failure was only logged.
- AX-5.10 **The table of cases** (2026-11-16, the maintainer's ask for a stability reference). How axKit handles
  each unexpected situation is written once, as data: `docs/axKit/cases.json`, one row per case, with the thing acted
  on and the situation (each from a closed list in the file's head), the apps, what axKit does, the exact words it
  says, the test or window script that proves it, and the requirement it comes from; `tools/cases.ps1` writes
  `cases.md` from it for reading. A change to how an unexpected situation is handled changes its row in the same
  commit, and a new kind of failure that is handled gets a row. `CasesTests` holds this in part: a row whose proof
  names a test method or a script's goal that no longer exists fails, a row whose sentence is a constant of the code
  fails when the constant's text changes, a row without a proof must say what is owed, and every settings file of
  `AppPaths` and every settings class has a row. What it cannot hold is that a change of behaviour whose test and
  words stay the same reaches its row: that is the commit's to do.
- AX-5.11 **What the user writes is in Documents, in a folder `axKit`** (the maintainer's decision of 2026-11-23:
  "appdata if a user is not meant to interact with it and … axKit documents folder if it is a user facing set of
  content that they will not break by deleting things"; `AppPaths.UserFolder`). Two kinds of file, two places:
  - Under AppData, as AX-5.1 has it: settings, logs, downloaded updates, and whatever axKit keeps for itself. The
    user is not meant to open them.
  - Under `Documents\axKit`: what the user makes with an app and would want to keep, find in File Explorer or
    axFile, back up or sync. Today axTemplate's templates, `Documents\axKit\Templates` (TP-2.1), and axTask's
    lists for someone who set no top folder, `Documents\axKit\Tasks` (an earlier `Documents\Tasks` stays in use
    while it is there and the new folder is not; a folder the user set stays), and AI help's working folder,
    `Documents\axKit\Claude`, where Claude Code keeps its notes for the user (ai-help.md AH-D6; `Documents\axKit
    Claude` until 2026-11-23, which stays in use on the same rule). A later app with content of the
    user's own puts it in a folder of its own beside them.
  - The update moves them in (the maintainer's choice of 2026-11-23, "Move both on update";
    `AppPaths.TakeOverUserFolders`, at the start of every process before anything reads a setting, never in a test
    run): `Documents\Tasks` into `Tasks` when it holds axTask's lists and axTask's settings do not name it as a
    folder the user chose; `Documents\axKit Claude` into `Claude` unless AI help's settings name it. The whole
    folder in one step when the new one is not there yet (`AppPaths.TakeOverFolder`, 2026-11-27: until then the new
    folder was made first and filled entry by entry, so that a move held up half way by a file in use left the
    lists in two folders, the new one already the one in use), and entry by entry only into a folder that exists;
    a move held up by a file in use is made at a later start, the old folder whole and in use meanwhile (the rule
    above). Claude Code finds its conversations by the folder's path, so axHelp starts a new one after the move.
  - Where Windows names no Documents folder, the folder is `Documents\axKit` in the profile folder
    (`AppPaths.DocumentsFolderFor`; Windows is asked without a look at the disk, so that a redirected folder that
    is away for the moment is still the one), and a folder that is no full path is never moved: until 2026-11-27
    the empty answer made a path relative to whatever folder the process was started in.
  - The test of which place: nothing under `Documents\axKit` is needed for axKit to run, and deleting any of it
    breaks nothing (a list's `list.json` is written again); and the content is the user's, not axKit's.
  - Why it matters most in the Store's copy: AppData is the package's private folder there (AX-5.5, AX-13.5),
    which Windows removes with an uninstall; Documents is never redirected and never removed. Where the user's
    Documents is synced by OneDrive, this folder is synced with it, by the user's own setup; axKit itself makes
    no network request for it (rule 8).

### AX-6 Shared pieces

- AX-6.1 `AxKit.Core` (pure, unit tested): `Dispatch` (with `Apps`, the name and verb of each app), `BundleInfo`, `AppPaths`, `Theme` (the theme file, `docs/axKit/theme.md`), `Ai` (AI help: the settings, the request to Claude Code in print mode, the answer, the prompts; `docs/axKit/ai-help.md`), `SettingsFile`, `WindowSettings` (what every window's settings hold, AX-6.5), `ConsoleSettings` (what the two console windows' settings hold besides: the console size, the recent folders, the sounds and signals, the transcript options; axClaude's and axTerminal's classes derive from it under the JSON names they had), `TextMatch` (a typed text against a name, AX-10.3) and `Accents` (the European letters taken apart and stripped without the globalization data: the executable runs with invariant globalization, where `string.Normalize` does nothing, and the test projects run the same way so that such a dependence fails in a test and not in the program; 2026-10-23),
  `Log` (per-app file name), `Updates/` (`UpdateCheck`, the release check, and `Updater`, the download and the
  hand-over to `install.ps1`; neither needs a window), `Audio/WaveTone` (the sounds as WAV bytes), `Paragraphs` (the
  paragraph starts of a text, for Ctrl+Up and Ctrl+Down), `WindowPlacement`, `EmbeddedText` (the embedded guides),
  `NaturalOrder` and `MediaFiles` (File Explorer's order of names, and the sound and video kinds with the time and
  speed rules; from axFile on their second use, axMedia), `Palette` (the command palette's lines and search, AX-10), `MenuText` (a menu item's or a button's text as spoken: no mnemonic marker, no trailing dots; the palette, the notices' key line and the menu choices that announce themselves), `MarkdownOutline` (the headings, links, lists, block quotes and tables of a Markdown text, with next and previous from a caret position; axDown SPEC §9, shared since its second use), `AxKitWindows` (the window lists of the Window menu from the windows the system names, AX-9), `RecentlyClosed` (the bundle's list of closed files and folders, AX-9.4), `RecentPaths` (what every settings class does to its recent list: the newest first, once, ten at most, cleaned after loading; AX-9.5), `PathName` (the name of a path for a title or a label: the last part, or a drive's root itself), `BundleAbout` (the About sections and the embedded axKit guide, AX-9.9), and the console-to-transcript pieces that came from axClaude on their second use, axTerminal (2026-10-18, docs/axTerminal/SPEC.md §5): `Pty/` (`PtyHost`, the ConPTY host, and `StreamRecorder`), `Vt/` (`VtParser`, `Screen`, `Wcwidth`) and `Transcript/` (`Line` and `LineKind`, `LineStore`, the base of each app's model: the lines the screen writes to, the decoder, the parser and the screen itself, the bookmarks and the cap, since 2026-10-23, `TranscriptMirror` and `Utterance`), tested in `tests/AxClaude.Tests` against its recordings.
- AX-6.2 `AxKit.Forms` (WinForms, checked with NVDA; the pure parts, `AppMessage`'s text form, `BundleKeys.Text` and
  `TextJump`, have unit tests in `tests/AxKit.Tests`): `OverlayPanel` (the notices in the
  window, with the `Notice` record callers pass), `ControlArea` and `ListKeys` (a question's answers as a list or a
  field in the message field's place, with the number and page keys of every such list), `Sounds` (wave-out devices kept
  open for the life of the process; `Notice` for every window, `Edge`, one short low note, for a key that moves nothing; internal since 2026-11-17, played through `EventDoor` alone), `EventDoor` (the door, `docs/axKit/events.md` EV-5: a window or a control never plays a sound itself, it raises the kind of event that happened, one of the closed list `AxKit.Core/Events/EventKind`, and the kind's sound comes from the one table `EventPresenter`, so that the same kind sounds the same in every window; `EventKindTests` holds the kinds and the table to `docs/axKit/families.json`, `EventDoorTests` holds that no other file plays a sound, and the compiler refuses an app that names `Sounds`), `EditKeys.Page` (Page Up and Page Down in an edit control), `EditKeys.Paragraph`
  (Ctrl+Up and Ctrl+Down by paragraph, with Shift selecting), `EditKeys` (the KeyDown handler that gives an edit
  control both), `Announcer` (the UI Automation notification, AX-7.1), `TextJump` (a window's key pairs for the kinds of element in a text and the jump itself on an edit control, used by every notice text and by axDown's editor, AX-7.2), `StatusLayout`, the font
  handling (text size and the font picker), `BundleKeys` (AX-8), `CommandPalette` (the menu bar read into the
  palette's entries, AX-10), `WindowAccessibleObject` (the window's client area as its UI Automation rectangle, so
  that NVDA+End finds the status bar), `WindowMenu` (the Window menu every window has, AX-9.1), `RecentMenu` (the "Open recent …" submenus, AX-9.5), `AppMessage` (the hand-over from a starting process to the open window of an app that keeps one window, one `WM_COPYDATA` message with a verb and paths, found through the Window menu's window list; axMedia and axPreview, docs/axMedia/SPEC.md AM-1.3, 2026-10-22; `SendTo` a window the caller found, axStart's hidden one, docs/axStart/SPEC.md AS-1.2), `TranscriptView` with `TranscriptKeys` and `QuickKey` (the read-only transcript of a console with its single-key navigation, the keys handed in by the window from its table; from axClaude on its second use, axTerminal, docs/axTerminal/SPEC.md §5),
  and the text editor shell: `TextEditorForm` (the window every text editor app is: the file, the menus, find, go to
  line, the status bar, the notices, the fonts, the update flow; axDown's `EditorForm` until axJSON, the second
  editor), `EditorApp` (what differs between the editor apps, as data: name, verb, settings file, file types, F1 text,
  guide, the `TextJump` pairs of its outline, its resources pages and its commands on the text; `ReadOnly` and
  `Refuse` make the shell a viewer, axPreview, docs/axPreview/SPEC.md AP-D4, 2026-10-22: no commands in Actions, no Save and
  no history, Reload and Edit in the editor in the Session menu, `l` for the line, the hand-over of a file received),
  `EditorKeys` (the shell's keys, the same in every editor window, and `Viewer`, the read-only set) and `EditorHost`
  (the entry point every editor app's `Run` is one call into); `AppHost` (every app's entry point, AX-1.7); `AxKitForm`, the window base (AX-6.5); and `ConsoleForm` (2026-10-23), the window of a console app on it, which axClaude's and axTerminal's windows derive from: the transcript view, the field, the control area, the status bar pair, the pty bridge, the paced writes with one pump, the recording, the export, find, the signals and the music, with the hooks for what is the program's (docs/axTerminal/SPEC.md AT-2).
- AX-6.3 A piece moves into a shared project only when a second app needs it, in a commit of its own, with the
  accessibility review; it moves unchanged where possible (B-3).
- AX-6.4 The notices and the question area are the bundle's one way of asking the user something, so that every
  window asks the same way: a notice has a title, a text, named buttons that Enter and Escape reach, the key line and
  the chime; a question in the message field's place is a list or a field named with the question, Enter answers,
  Escape cancels. An app that needs to ask uses these and does not draw its own dialog.
  - **A question that arrives by itself holds its answering keys** (the rule is written once, in
    `AxKit.Core/KeyHold`, and tested with a clock and with the key messages Windows sends: `KeyHoldTests`,
    `ControlAreaHoldTests`, `ControlAreaKeyMessageTests`, `OverlayPanelHoldTests`). A question or a notice
    the user did not open with a command of their own has not been heard when it appears, and a key that reaches it
    then was meant for what stood there before: a message being typed, or the question just answered, whose Enter
    was pressed twice. So for a moment after it is shown (one second) its answering
    keys do nothing: in a list of the question area Enter, Escape, Space and everything typed (a letter, a digit, a
    punctuation mark, with Shift or without); in a field there Enter and Escape, what is typed going into the field;
    in a notice Enter and Escape (each also with Shift, which a window takes for the same press), Ctrl+Enter, Space
    on a button or a check box, a button's letter with Alt, and a letter the notice gives a meaning. Escape is held
    like Enter because it answers too: it presses the notice's cancel button, and for some notices cancelling acts.
    The keys that only move (the arrows, Page Up and Down, Home, End, Tab) are never held. Five things hold, and no
    caller can turn them off:
    1. *Whatever came before.* A question that follows the user's own answer arrives by itself too: that the user
       acted a moment ago says nothing about the new question. Whether the user acted lately may decide whether a
       chime plays, never whether the keys are held. (Until 2026-11-27 axClaude passed "the user caused this" for
       the question after an answer, the area cleared its hold for it, and Enter pressed twice at "1. Yes" for the
       first of two files approved the second before NVDA had said more than "Question".)
    2. *One hold for each control of the question area, never shortened.* A control's hold is its own, new with the
       control: a second from when it appears, and, for the control that was answered, as long as the window says
       after the answer went (three seconds in axClaude, so that a second Enter does not send it twice). There is
       no call that ends a hold early. The control shown next has a hold of its own and takes over nothing of the
       one before: its own second is what protects it. (For an hour on 2026-11-27 the next control inherited what
       was left of the answer's three seconds; every list that followed the user's own key, each step of a screen
       included, then took Enter three seconds late and in silence, which bought nothing.) A notice shown while
       another notice's hold runs still takes over what is left of that second.
    3. *Every control the question area shows holds*, with one exception that its caller must write out as
       `hold: false`: a control the user opened with a command of their own a moment ago, whose first keys are meant
       for it (Go to line, an editor's offer, axTerminal's recent commands). `ControlAreaHoldTests` lists those
       callers and fails on any other, and on a hold that is worked out instead of written. One opened by a command
       run for an AI tool is held all the same (`AxKitForm.RunForMcp`, `ControlArea.HoldAsArrived`). A notice is
       held when it is made with `Notice.ByItself()`, which is the only way: whether a notice holds cannot be handed
       a value worked out, so everything a program, a job or a timer shows is written `.ByItself()`, whatever the
       user did before it, and what may be worked out is only its chime (`Notice.Silent`). (Until the third review
       of 2026-11-27 it was a switch, and three places worked it out: axClaude's information screen from whether
       the user had acted in the last three seconds, the update notice from whether the user had asked for the
       check, axGIT's missing git from whether it was the first time. A screen of Claude Code's that followed the
       user's own key had no hold, and Enter was the update notice's install.)
    4. *The hold counts again from when the focus arrives.* The moment is there so that the question is heard before
       a key answers it, and NVDA says a question when the focus reaches it, not when it is drawn. What arrived by
       itself therefore starts its second when it is shown and again each time the focus comes to it, whoever moved
       the focus: the window's own timer (axClaude puts the focus on a question a second after showing it when the
       user was in the conversation, which was the very moment the hold ended: an Enter typed in the conversation
       then approved a file unheard, found by the third review of 2026-11-27 and played by
       `tests/scripts/axClaude/66-question-while-reading.txt`), a notice that closes and hands the focus to the
       question under it (script 67), the window becoming the active one again, a menu that closes, and the user's
       own focus key. The last is accepted: after F6 or Tab into a question that arrived by itself, its answering
       keys wait the second once more; the arrows move at once. A notice does the same when the focus comes back
       into it or its window becomes the active one. A control the user opened themselves (`hold: false`) starts no
       hold when the focus arrives either. And the field of a conversation window holds its Enter for the same
       second when the window itself puts the focus back into it because a question that arrived by itself has
       closed (`ConversationForm.CloseArea`, an AI's request answered): an Enter meant for the question's list would
       otherwise send whatever the field holds, in axTerminal a command. After a list the user opened (axTerminal's
       recent commands, whose Enter puts a command into the field for the next Enter to run) the field holds nothing.
    5. *The hold is asked where an answer is taken, not only where a key is read.* In the question area every
       callback that takes an answer (the list's choice, its Escape, a letter of the list's own, a typed character,
       a field's Enter and Escape) passes one gate after its key was read (`ControlArea.Takes`), and in a notice the
       one method that runs a button (`OverlayPanel.Choose`), whatever pressed it: a key, a click, a letter with
       Alt, the window's own press of the default button, UI Automation's Invoke. Held, the gate raises the refusal
       and nothing happens. So a key path that the reading of the keys does not know is harmless: until the third
       review of 2026-11-27 Shift+Enter and Shift+Escape answered a notice inside its hold, since only the plain
       keys were looked for and a window presses its buttons for either. The conversation window's field asks its
       hold the same way where Enter and Ctrl+Enter send (`ConversationForm.FieldTakesEnter`).
    Two rules stand beside the hold, for the question area and the notices alike (`AxKit.Forms/AnswerKeys`):
    - **A repeat never answers.** A key that answers and arrives as the repeat of a key held down (Windows marks
      such a key message: the key was down already) answers nothing, held or not, and makes no sound: a repeat is
      not a press. Without this a key held down longer than any hold answered whatever came in the meantime, the
      next question on the answer its list starts on. In a list of the question area that is Enter, Escape, Space
      and every letter and digit, a screen's own letter among them (d to discard sent its key again and again); in
      a field there Enter and Escape; in a notice every key named above. In the field of a conversation window a
      repeat of Enter or Ctrl+Enter never sends, in axClaude, axTerminal and axCalc alike, so that an Enter held a
      moment too long does not answer what the program asks next. The arrows repeat, and so does everything typed
      into a field.
    - **A held key is heard.** A fresh press of an answering key that a hold takes raises the kind of event
      `Refused` at the door (bundle rule 13: the kind and its sound are the ones there were; no words, which would
      cut off NVDA's reading of the question), at each press, with no limit. A key that did nothing is never
      silence: whatever a held list takes is heard, Shift with a letter and a punctuation mark included (they were
      swallowed without a sound in the list of Claude's question until the third review of 2026-11-27). Whether
      the sound stays is the maintainer's to hear.
    This matters more than it did: every question of axKit is to become a list that starts on its confirming answer
    (the maintainer's decision of 2026-11-27, "Enter is always go forward, Escape is go back", no exception for an
    AI's request). These rules are then the one thing between a doubled, held or typed-ahead Enter and a yes the
    user never heard, so a change to them is a change to that protection, and a new way for a question to appear
    goes through the question area or a notice and nowhere else: a question built on them has rules 4 and 5
    without writing them. A notice the window opens for the user's own command, and which is that command's first
    result, is shown as it is and holds nothing.
  - **Nothing unseen in what is asked.** The name and the answers of every question in the question area are shown
    with each character that is neither seen nor heard written out as its code (`AxKit.Core/DisplayText`,
    `DisplayTextTests`), by the area itself, so that it holds for every window. Which characters these are is said
    by Unicode's own classes, not by a list that each review found short (the third review of 2026-11-27): every
    code point whose general category is Format, Control, Surrogate (half a pair, alone), PrivateUse or
    OtherNotAssigned, the line and paragraph separators, and the default ignorable code points that have another
    category (the fillers, the variation selectors, the combining grapheme joiner; the Unicode Character Database,
    DerivedCoreProperties.txt). One exception keeps an emoji an emoji: U+FE0F passes right after a symbol, and
    U+200D passes between two symbols, the first of which may carry its U+FE0F; anywhere else both are written out.
    In a command offered to run and in the label of a permission question, a space that is not the plain one (a
    no-break space, an em space) is written as its code as well (`DisplayText.VisibleCommand`), since it reads as a
    space and a shell may take it for part of a word.
- AX-6.5 **The window base.** Every window of an app derives from `AxKit.Forms/AxKitForm`, which holds what the
  windows share and no app copies: the notice flow (`ShowNotice`, `CloseNotice`, `Fail`, `ShowError`; Alt, F10 and
  the window's shortcuts swallowed while a notice shows), `Announce`, the text font with its size keys and the
  Windows font picker (the choice said), the window's place and size, the settings saving and the notice for a
  settings file that could not be read, the Window, axKit and Help menus with the
  update check, the update notice and the install in place (AX-3), find in a text box, in a list, in a transcript
  and in a notice's text (one Find notice, one last text), Copy diagnostics (the account's name, its paths, e-mail addresses and ids replaced by `Redaction`, since it is meant for a public bug report, security audit S-20), `OpenUrl`, `OpenInExplorer`, the
  Windows folder picker (`ChooseFolder`), `Do` (a command's task with an escaped exception turned into a notice),
  the hand-over from a starting process of the app (`AppMessage`, answered at once and applied after), the submenus
  and the menu every window has (Options → Text, General and Developer; Help: `CreateTextMenu`,
  `CreateGeneralMenu`, `CreateDeveloperMenu`, `CreateHelpMenu`, each with the window's own items in their place;
  `Toggle` for a checked setting; `CreateExitItem` for the last item of the first menu), the
  window's pipe for the AI tools (`McpChannel`, MC-6.2), the window role for NVDA
  (AX-7.1) and the common part of the key switch (the text size keys everywhere, the palette on F1 and Ctrl+Comma, Ctrl+S). A
  window adds its controls (the `Overlay` first) and fills in the hooks: the F1 text, what to hide for a notice and
  what to show after, the control the focus falls back to after one (`FocusTarget`), whether the chime follows a
  setting, what gets the font, how the new version starts it again (`RestartArguments`, by default the app's verb and `PathInHand`; `UpdateRestartText`), what
  to note of an update (`UpdateNoted`), its diagnostics lines, the control its announcements are raised on
  (`AnnounceSource`), what Ctrl+S does (`SaveCommand`;
  "Nothing to save here" by default), what it does with a hand-over (`TakeHandOver`), how it reports a settings file
  it could not read (`SettingsNotRead`; a notice by default, a system line in the console windows), and its own
  keys, as commands (docs/axKit/app-kit.md AK-2; `ProcessWindowKey` for what must come before them). **The focus across a notice is one rule, the base's** (2026-10-23): when the first
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
  (the review of that day, in the git history).

### AX-8 Keys

- AX-8.1 Every key an app handles is one of two kinds: **bundle-wide**, meaning the same in every window of axKit, or
  **window-specific**, meaning something in one window only. Each app's keyboard table in its spec marks every key
  with its kind.
- AX-8.2 Bundle-wide keys are defined once, in `AxKit.Forms/BundleKeys`, a plain static class of constants, and every
  window uses those constants. The list: F1 the command palette with the keys, palette and help in one, and
  Ctrl+Comma the same, a second key on trial (AX-10; F1 the keyboard shortcuts and Ctrl+Shift+P the palette until
  2026-11-26); Tab,
  Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the window's controls (and between a notice's text, field
  and buttons); Ctrl+F find, F3 and Shift+F3 next and previous, in a notice's text as well (AX-7.2); Ctrl+S save (what the window holds: the editors' file; axClaude and axTerminal, which have no
  document of their own, export their transcript to a text file with it, from the bottom of their first menu, so that
  Save and Save as keep their place in the editors' and the first menus share one order; a window with nothing to
  save says "Nothing to save here", axGIT adding the keys that do something, through the base's `SaveCommand`); Ctrl+Plus and Ctrl+Minus text size; Enter and Escape on a notice; Page Up and Page Down one screen in an
  edit control, then the very start or end of its text, so that repeated presses always get there, and in a field
  of one line its start or end at once, the edge sound when already there (the maintainer's ask of 2026-11-23;
  `EditKeys.Page`; a window that gives these keys to a list under its field, the palette and the hotkey windows,
  keeps them); Ctrl+Up and Ctrl+Down the start of the previous and next paragraph in an edit control, a paragraph
  starting at the first line or at a line with text directly after a blank line, and Ctrl+Shift+Up and
  Ctrl+Shift+Down the selection grown to there, as in Word (`EditKeys.Paragraph`; the EDIT control's own keys scroll a
  row and leave the caret where it is, as Notepad's do, which a screen reader, reading at the caret after the key,
  reports as the same text again); F4 a report to axKit's maintainer (docs/axKit/reports.md RP-2.1, 2026-11-07); F9 the status bar said by axKit itself, in a notice too (`BundleKeys.SayStatus`, AX-7.10, 2026-11-15; F8 until 2026-11-26); F7 read in full what you are on, in a notice its text (`BundleKeys.ReadInFull`, AX-8.18 rule 5, 2026-11-26); Ctrl+Space edit mode or navigation mode in an editable text of several lines (`BundleKeys.ToggleMode`, AX-8.13, 2026-11-15); Ctrl+Enter the main action of where the focus is (`BundleKeys.MainAction`, AX-8.10 rule 3, 2026-11-14); Shift+F1 a question to Claude about this window, Windows' own key for help about the thing in front (`docs/axKit/ai-help.md` AH-6, 2026-11-09), and Ctrl+Slash a further key of the same command (`BundleKeys.AskClaudeAlso`, AX-8.15, 2026-11-16); Ctrl+Period the context menu where the focus is, as Shift+F10 and the Applications key open it (`BundleKeys.ContextMenu`, AX-8.15, 2026-11-16); Alt+F4 exit; Alt and F10 the menu. Everything else is window-specific.
- AX-8.3 Window-specific keys are defined once per window, in one table (`AxClaudeKeys`, `AxDownKeys`, `AxJSONKeys`, `AxGitKeys`, `AxTerminalKeys`, `AxFileKeys`, `AxMediaKeys`, `AxPreviewKeys`, `AxStartKeys`, `AxProcessKeys`, `AxTaskKeys`, `AxPassKeys`, `AxCalcKeys`, `AxClipKeys`), as constants
  with names that say what the key does. Changing a key is a change to that one line: the menu item and the key
  handler read the table, and the F1 text's list of every key is made from the window's commands (AX-7.2a). Single-letter quick keys
  (axClaude's `h`, `i`, `o`) are in the table too. The text editor shell's keys (`AxKit.Forms/EditorKeys`: Ctrl+N new,
  Ctrl+O open, Ctrl+Shift+S save as, F12 go to line, F11 word wrap; Ctrl+Shift+N new window and Ctrl+W show in File
  Explorer until 2026-11-26, which keep their menu items without a key) are
  defined once for every editor window, and each editor app's table includes them, so that one table per window
  still holds every key and the collision test sees them.
- AX-8.4 A unit test in `tests/AxKit.Tests` fails when a window-specific key equals a bundle-wide key, and lists the
  keys that two windows use for different actions, so that a collision between apps is seen before it is felt. A
  window key may take a bundle key's place in one control only by the user's decision, listed by name in the test's
  `Overrides` with its spec line: axTerminal's command field, Ctrl+Up and Ctrl+Down for the history (AT-3.9, 2026-10-23);
  the item apps' navigator, the same keys by section (item-apps IA-2.3a, 2026-10-24); axMedia, the same keys for
  faster and slower (AM-D8, 2026-11-14), whose window has a list and no text outside a notice.
- AX-8.5 A key is never handled anywhere but through its table, and no key is intercepted that NVDA uses (AX-7.4).
  The text a menu item shows for its key comes from the tables too (`BundleKeys.Text`: "Ctrl+Shift+S", "Shift+Escape"),
  so that a key changed in its table changes in the menus by itself; only Windows' own keys (Alt+F4, the edit
  control's Ctrl+Z, Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A) and descriptions ("Enter in the message field") are written out.
  A key of the number row ends in a full stop ("Ctrl+1.", "Alt+1."): NVDA speaks the shortcut directly before the position of
  the item in the menu ("1 of 9"), and the synthesizer runs "Ctrl+1  1 of 9" together into "Ctrl+11 of 9"; the full
  stop is a pause between the two.
- AX-8.6 Type-to-find in every list a user moves through by typing (2026-10-24; File Explorer's way, axFile's AF-3.6
  since 2026-10-22): letters typed within a second of each other build one prefix and select the first item that starts
  with it from the one in hand, round the end; the same letter again goes on to the next item starting with it; Shift
  with a letter searches the same way backwards (the user's ask; the case of the letters never matters, so a capital
  only says the direction); the edge earcon when no item starts with it; nothing is said but what NVDA reads of the
  selection. Up or Home on the first item and Down or End on the last play the edge earcon too, since the key moves
  nothing; in a list that holds nothing, each of the four plays it (2026-11-23: an empty list of axTask was silent
  on Up and Down, the maintainer's report). Both are built into one control, `AxKit.Forms/AxKitList`, the list of every app (`AxKit.Core/TypeAhead` the
  search, tested): a window makes an `AxKitList` and has them, and gives it the names to search only when its items
  are not all names (`Names`). `ListTests` fails on a plain `ListBox` in an app; its one named exception is a
  question's answers, whose letters are the screen's keys (axClaude D33), and where, on a question of Claude's,
  every character typed but Space reads the question again and none jumps to an answer (2026-11-26). The lists under a field that keeps the
  caret (axStart's and axClip's matches and the palette's, never focused) are the same control in its mode for
  that since 2026-11-22 (`AxKitList.Field`, docs/axKit/framework.md FW-4.12): the field's keys move the selection,
  with the edge earcon at an end, and nothing typed reaches the list.
- AX-8.7 F2 renames in place in every list whose items can be renamed (the user's rule, 2026-10-24; axFile's AF-4.2
  since 2026-10-20): a box named "New name" over the item, holding its name with the part that changes selected (a
  file's name before its extension); Enter takes it, and a name the window refuses is said and the box stays; Escape,
  or the focus leaving the box, keeps the old name; Ctrl+Z in the box undoes the typing. Built into `AxKitList`
  (`BeginRename`, `EndRename`, `Renaming`), so that a window only says what renaming means for its items. In axFile's
  folder list, axTask's and axPass's navigators, and the item apps' field titles.
- AX-8.8 A check box says its new state when the user toggles it, "checked" or "not checked", as one announcement
  (2026-10-31: NVDA said nothing on Space in the consent notice's box). Built into `AxKit.Forms/AxKitCheckBox`, the one
  check box of the bundle; `CheckBoxTests` fails on a plain `CheckBox`.
- AX-8.9 Every edit control of the bundle is `AxKit.Forms/AxKitTextBox` (2026-11-09; the user: "ensure the shared text
  fields are all coming from a single component"): no length cap (the control's own 32 767 characters cut a long paste
  short), and in a multi-line control the bundle's page and paragraph keys (AX-8.2) after the window's own handler, so
  that a window's key on the same stroke comes first (axTerminal's Ctrl+Up is its history). Word wrap is the
  control's, on, everywhere but the editors, which take it from the theme file's display settings
  (`docs/axKit/theme.md` TH-6). `TextBoxTests` fails on a plain `TextBox` and on a second subscription of the keys,
  which would move the caret twice.
- AX-8.10 **Fields and forms: standard controls, used the standard way** (the user's decisions of 2026-11-14). The
  reason: a screen reader user in a field knows only that they are in a field. No visual clue tells what kind of
  form it is, and an interface used rarely must not need a remembered name or a change of mode. So a form is made of
  the controls every Windows user already knows, behaving as they do everywhere, so that what the user expects holds
  and good habits are taught. The rules:
  1. **A form opens with the focus in its first field, ready to type.**
  2. **Tab and Shift+Tab move between the fields and the buttons, in order.** Each is a real control with its
     `AccessibleName`, read by NVDA itself: a text box (`AxKitTextBox`), a list for a choice (`AxKitList`), a check
     box that Space toggles (`AxKitCheckBox`), buttons. The window adds no speech of its own to a move.
  3. **Ctrl+Enter does the main action, in every window and every form** (`BundleKeys.MainAction`, bundle-wide,
     AX-8.2): it sends a form from any of its controls. The table below says what it is in each app.
  4. **In the item window's fields pane, which is an item editor and not a form, Right and Left change the field in
     focus** (docs/axKit/item-apps.md IA-5.6). On a choice, Right is the next value and Left the previous, stopping
     at the ends with the edge sound, never wrapping; a choice whose value is none of its options takes the first
     on Right. On a check, Right checks and Left unchecks, a direction and not a toggle; already in that state, the
     edge sound. Every change is spoken once, as the field's name and its new value ("Priority: Low"), and is one
     Ctrl+Z step where the window has undo. Space still toggles a check and Enter still opens a choice. On any
     other kind of field Right and Left play the edge sound and change nothing. Never on axPass's protected fields,
     the user name and the password (`AxKit.Core/FieldStep`, tested).
  5. **Enter in a text box does what it does in every text box.** In a box of several lines it makes a new line and
     never sends (chosen over "Enter sends, Shift+Enter breaks a line", by which a stray Enter sent an unfinished
     report on the morning of 2026-11-14). In a one-line box of a form with several fields, and in a list, on a
     check box or in a text that is only read, Enter presses the form's default button, as Windows does; on a
     button it presses that button.
  6. **A question with one text field keeps the short way**: Enter answers at once (`AxKitForm.AskText`).
  7. **Escape cancels the form and keeps nothing**: no draft, and the window says "Cancelled". In a one-field
     question Escape cancels that question.
  8. **A plain list of answers stays where no form is needed**: the question interface (`ControlArea`'s list of
     answers) is unchanged.
  9. **Ctrl+Enter is the main action of where the focus is, and never two things in one press.** While a notice
     shows it presses the notice's default button from whichever control has the focus (`OverlayPanel.PressMain`):
     a form's Send, a long text's Save from inside its box, any other notice's main button, as Enter does there. In
     a window it does the window's one main action where the focus is, when there is one. Where there is none it
     does nothing and plays the edge sound; it never reaches the control, so it puts no line break into a text.
  10. **A form's explanation and disclaimer are the notice's own text at the top**, never a row or a field of their
     own and never shortened away. The text names the form's keys itself (`Notice.KeyLine` false: the line made
     from the buttons, "Enter: Send", would be untrue where Enter is a new line). Since the focus starts in the
     first field, the window says the title and the word "form" as the form opens, after NVDA has read the field,
     and the text on demand, F7 (AX-7.11, 2026-11-22, Ctrl+Q until 2026-11-26; the whole text at opening until then); the report's
     form and an action's form of an axTask list still say their text at opening, which is what will happen.
  11. **A required field left empty**: Send and Ctrl+Enter move the focus to that field, play the edge sound and
     say what is missing ("Type a few words first").
  12. **The value steps of rule 4 have no sound of their own**: the edge sound at the ends, speech for the change.
  13. **Built and withdrawn, 2026-11-14: a form as a list of fields.** The first answer of that day made a form one
     list, a row per field read with its value ("Kind: Bug") and a Send row at the end, with Enter acting on the
     row in focus and opening an edit box for a text. It was built for the report the same day; the user tried it
     and withdrew it at once: it did not open ready to type, sending took too many steps, too much was read
     aloud, the disclaimer was buried, and it did not feel like a form. Rules 1, 2, 5 and 10 are what he decided
     in its place. Do not build it again; the first form is the report (docs/axKit/reports.md RP-3).
  14. **Whatever asks more than one thing for one action is one form, never one question after another** (the
     user, 2026-11-14: "implement this everywhere so we go back to standard interaction paradigms"). Every form is
     made of one piece: `AxKit.Forms/FormControls`, the controls in Tab order (a text box of one line or of several,
     a list for a choice of one, a check box; never radio buttons), each under its caption and named with an
     instruction ("Type the name of the server"), shown by `AxKitForm.ShowForm` with the form's explanation, its
     default button and Cancel. The window's check runs before the button acts: what is missing or refused is said
     with the edge sound and the focus in that control, and the form stays open. The forms: the report (F4); axTerminal,
     Add a shell (AT-1.6a); axMCP, Add server (MP-1.3); axTask, Action
     keys (TK-7a.1) and Move to section (TK-3a.3). Left as they are: a question with one field (rule 6); a list to
     choose one thing from, which is one question; and a password asked twice, axPass's master password for a new
     database and an entry's new password, each in the notice's own hidden field, which is emptied as the notice
     closes (S-17).
  15. **Ctrl+Down and Ctrl+Up move to the next and the previous field of any form** (the maintainer, 2026-11-23:
     "ctrl+up and down should go to next field in a form"), from any of its controls, the edge sound past the first or
     the last; in a form a box of several lines gives up its paragraph keys, which are the same keys
     (`FormControls.ProcessCmdKey`). Every form has it, since every form is `FormControls`.
  16. **Every field of a form has a key, its name and its number**, by which it is focused, its text selected, or the
     form started in it (`FormControls.Key`, `Focus`, `StartIn`; the maintainer, 2026-11-23: "it should be easy
     to mark each field in a form with an uuid or number or label that can be used to select it and select its
     contents or place focus there"). The AI tools, a find by a field's name and a navigation mode in a form build on
     these keys (their item in the priorities list).
  17. **The item window's form is one for every app of it** (the maintainer, 2026-11-23: "a form is a form and a detail
     pane list is a detail pane list and the item list is an item list ... easy to adjust without manual updates in
     multiple places"): Enter on a row opens the row's form, every field in its document's order, and Enter on a field
     of the detail pane opens the same form in that field (`AxKit.Forms/ItemForm.Form.cs`, the rules
     `AxKit.Core/ItemFormRules`). An app says only which rows have a form, with the item's document and what a save
     writes (`FormOf`, an `ItemFormSource`); axTask and axTemplate do (TK-4.0a, TP-8.3a). The detail pane stays for
     reading, with its one-key changes (rule 4). A new item's form is the same form, empty (`OpenItemForm` with a
     fresh document, its own title and Add; axTemplate's Ctrl+N, TP-8.4).
  18. **Ctrl+Space in a form is the form's navigation mode** (the maintainer, 2026-11-23: "having user search in a form
     for field names would be useful. can we use ctrl+space to toggle nav mode in a form in the same way we have it in
     axdown"): from any field, nothing is typed; Down and Up move between the fields; the start of a field's name goes
     to it, as type-ahead in a list (`AxKit.Core/TypeAhead`, AX-8.6, over each field's first key or its caption), and
     its number goes to it where the form numbers its fields; Space still checks a check box; Enter, or Ctrl+Space
     again, goes back to typing in that field, "Edit mode" said; leaving the form ends it. In a form the mode key is the
     form's, so a box of several lines there has no navigation mode of its own. Every form's explanation (F7) ends
     with these keys, written from the key table (`FormControls.KeysLine`).
  19. **A group of checks of one kind is one list** (the maintainer's choice of 2026-11-23, "One list, Space checks", over
     a check box each, after his question whether checks take a Tab each or the arrows; both are Windows' own, a check
     box each for a few unlike options, one list for many of a kind): one Tab stop, Down and Up through the lines, Space
     checks or unchecks the line, Ctrl+A checks every line and Ctrl+Shift+A none, each line saying "checked" or "not
     checked"; Check all and Uncheck all buttons just before it (`AxKit.Forms/FormCheckList`,
     `FormControls.AddCheckList`, `AddButton`). The item form makes one of two or more checks in a row
     (`ItemForm.Form.cs`), named by the source's `ChecksName`; in navigation mode a typed name lands on its line,
     and Space presses a button there too.
  20. **A choice of one can be a list where Space chooses** (the maintainer, 2026-11-23: "please also make a radio
     button list since that will be needed eventually"): one Tab stop, Down and Up move without choosing, as the
     arrows explore in every list of the bundle, Space chooses the line and the one chosen before is chosen no longer,
     each line saying "chosen" or "not chosen" (`AxKit.Forms/FormRadioList`, `FormControls.AddRadioList`). Built
     ahead of its first use; a plain choice where the line selected is the answer stays `AddList`.

  What Ctrl+Enter does, per app. A window gives the key its main action as a command on `BundleKeys.MainAction`;
  no window's key table holds Ctrl+Enter, and `KeyTests` fails on one that does.

  | App | Until 2026-11-14 | Now |
  | --- | --- | --- |
  | Every window, while a notice shows | nothing of axKit's; a line break in a notice's text box of several lines | the notice's default button from any of its controls: a form's Send, a long text's Save, a plain notice's Close, as Enter there |
  | axClaude | in the message field, a line break (the edit control's own) | in the message field, sends the message, as Enter; elsewhere the edge sound |
  | axTerminal | in the command field, send without recording (AT-3.4) | in the command field, sends the command, as Enter; elsewhere the edge sound. Send without recording has no key: Actions menu and the palette |
  | axGIT | commit what is staged (AG-3.5) | the same |
  | axMCP | call the tool or fetch the prompt selected (MP-3.4) | the same |
  | axFile | in the folder list, play the folder in axMedia (AF-5.1) | in the folder list, Preview, as Shift+Enter (AF-5.3; the maintainer, 2026-11-26; Play folder in axMedia until then, in the Context menu without a key since); outside the folder list the edge sound |
  | axTemplate | not there (begun 2026-11-23) | in the browser, copies the template's text, its marks filled with nothing (TP-8.4); in the launcher, puts the template in around what is selected in the program in front, which is sent the copy key first (TP-4.2) |
  | axDown, axJSON | a line break in the text (the edit control's own) | Save, as Ctrl+S (2026-11-26; the edge sound until then); Enter makes the line break |
  | axTask | nothing of axKit's | Done or not done, on an item (TK-4.4) |
  | axMedia | nothing of axKit's | play, pause or resume, from anywhere in the window (AM-4.3, AM-D7) |
  | axPass | nothing of axKit's | Copy password, as Ctrl+C, on an entry of an unlocked database (PW-7, 2026-11-26); elsewhere the edge sound |
  | axProcess | nothing of axKit's | End process on a process, with the question Delete asks (PR-6.2, 2026-11-26); elsewhere the edge sound |
  | axAutomate | nothing of axKit's | Run, as Ctrl+R (AU-4.4, 2026-11-26) |
  | axHelp | nothing of axKit's | Open the conversation, as Enter (HE-6, 2026-11-26) |
  | axPreview, axCalc, axStart, axClip | nothing of axKit's (the focused control's own, if any) | the edge sound (axPreview by the maintainer's choice of 2026-11-26: reading needs no command) |
- AX-8.11 **The system-wide keys are on Ctrl+Shift** (the user's decision of 2026-11-14). A system-wide key is a
  background app's hotkey: Windows takes the combination from every program while that app runs, axKit's own windows
  among them. Each is a setting of its app, and these are what they are at first:

  | Key | What it does | Where it is set |
  | --- | --- | --- |
  | Ctrl+Shift+Space | axStart's search field (AS-4.1) | `AxKit.Core/Hotkey.Default`; Options, Hotkey in axStart |
  | Ctrl+Shift+Slash, the question mark's key | axHelp's conversation with Claude (HE-4.1) | `AxHelp.Core/HelpSettings.DefaultHotkey`; Options, Hotkey in axHelp |
  | Ctrl+Shift+Period, the greater-than sign's key | a screenshot of the window in front into axHelp's conversation (HE-4.1, HE-4.3) | `HelpSettings.DefaultScreenshotHotkey`; `screenshotHotkey` in `axHelp.json` |
  | Ctrl+Shift+Comma, the less-than sign's key | axClip's search field over the clipboard history (CP-1.3) | `AxClip.Core/ClipSettings.DefaultHotkey`; Options, Hotkey in axClip |
  | Ctrl+Shift+Semicolon | axTemplate's search field over the templates (TP-1.3) | `AxTemplate.Core/TemplateSettings.DefaultHotkey`; Options, Hotkey in axTemplate's launcher |
  | Ctrl+Shift+Apostrophe | kept for axAutomate's launcher, to be built (its item); nothing registers it | written here alone |

  The Windows+Alt keys, axHelp's Windows+Alt+H and Windows+Alt+S and axClip's Windows+Alt+V, are retired: the user
  expected other programs to take them or to act on them. A settings file that still holds one of them is taken over to the new key as it is
  read (`AxKit.Core/HotkeySettings.TakenOver`, tested; an app names its `RetiredDefaults`), and a key the user chose
  stays. No table, the bundle's or a window's, holds one of the six keys, the reserved one included: the key would
  never reach the window. `BundleKeys.SystemWide` lists them, and `KeyTests` fails on a table that holds one and on
  a default that left the list. axMedia's Faster and Slower, on Ctrl+Shift+Period and Ctrl+Shift+Comma until then,
  are Ctrl+Up and Ctrl+Down (AM-D8). Outside axKit, Word and Outlook lose their larger and smaller font on the comma
  and period keys while axHelp runs; the user was told. A hotkey's key is one of `Hotkey.Names`: a letter, a digit,
  a function key, a navigation key, Slash, Comma, Semicolon, Apostrophe or Period, Period only with Shift, since
  Ctrl+Period is the Context menu in every window (AX-8.15). axClip's key joined the
  same day by a second decision of the user's (CP-D1), on Semicolon. axTemplate's came on 2026-11-23 on Apostrophe,
  the key to the right, and the same day the user swapped them: templates are used more, so axTemplate has
  Ctrl+Shift+Semicolon and axClip Ctrl+Shift+Apostrophe (CP-D1, TP-D1). On 2026-11-24 the user moved them again for
  axAutomate's launcher (its item): Apostrophe kept for it, axClip on Comma, axHelp's screenshot on Period, the key
  kept until then for a later help launcher. No window uses any of them. A settings file that
  holds an app's old default is taken over to the new one as it is read (`HotkeySettings.RetiredDefaults`, and
  `HelpSettings.RetiredScreenshotHotkeys` for the screenshot).
- AX-8.12 **The key test: whether a key reaches the window, and what it does there** (2026-11-15, the user's
  decision, after a function key that never reached axClaude: the keyboard had made it another key). Every window
  has **Help, Diagnostics, Test a key**, in the menu and the command palette, with no key of its own; it is not
  offered while a notice shows, as no menu command is. It is built once, in the window base
  (`AxKitForm.KeyTest.cs`), and its words are pure code (`AxKit.Core/KeyTest`, `KeyTestSession`, tested in
  `KeyTestTests`).
  - **Starting** says, as one interrupting announcement of the window (AX-7.3): "Key test. Press any key to hear its
    name and what it does here. Press Escape to end."
  - **While it runs, every key press that reaches the window is said and not carried out**: no command runs, no
    character is typed, no selection or caret moves, Alt and F10 open no menu, Alt+F4 closes nothing. Each press is
    one interrupting announcement, once per press (a held key's repeats say nothing): the key as the bundle spells
    it (`BundleKeys.Text` without the number row's full stop: "Ctrl+Comma", "F9", "Alt+Left"; "PageDown",
    "CapsLock" and the bracket, quote and backslash keys by plain names), a colon, and what it does in this window
    where the focus is, the first of these that applies:
    1. a bundle key the window base handles itself in every window (F1, Ctrl+Comma, F4, Shift+F1, F9, Ctrl+S, the
       text size keys): the name of the menu item that shows the key, "F9: Say the status", "F4: Send a report",
       "Ctrl+Plus: Text, Larger text"; and the two further keys of AX-8.15, which no menu shows, by the key each
       stands beside: "Ctrl+Slash: Ask Claude about this window, as Shift+F1", "Ctrl+Period: The Context menu, as
       Shift+F10";
    2. a command of the window's list (docs/axKit/app-kit.md AK-2) on that key whose `When` holds now, the first in
       the list's order, by its menu item's name, with its submenu in front when it is in one: "Ctrl+B: What runs in
       the background", "Ctrl+H: Next, Heading", "Ctrl+Up: Previous section" in an item window's navigator, where
       the same key in a text is the bundle's "Ctrl+Up: Paragraph up" (the overrides of AX-8.4); then a key a menu
       item shows that is no command of the list ("Alt+F4: Exit"), and "Alt+H: opens the Help menu" for each menu of
       the bar by its letter;
    3. Windows' own keys for a window: "Alt+F4: closes this window" (where no menu item shows the key), "F10: opens
       the menu bar", "Shift+F10: opens the Context menu" (and the Applications key), "Alt+Space: opens the window's
       own menu: restore, move, size, close", "Ctrl+Enter: the main action; there is none where you are";
    4. what the window says of a key its focused control handles itself (`KeyTestOwn`; the console windows: "H:
       moves to the next heading", "Shift+H: moves to the previous heading", "Ctrl+Alt+H: reads every heading from
       the caret's line to the end", "L: says the line number", and in the field "Enter: sends what the field
       holds");
    5. what a control of the focused kind does with the key: in a text that can be typed in, "A: types the letter",
       "Space: types a space", "5: types the character", "Enter: makes a new line" (a text of several lines),
       "Backspace: deletes the character before the caret", "Delete: deletes the character after the caret",
       "Ctrl+V: pastes", "Ctrl+X: cuts the selected text", "Ctrl+Z: undoes the last change of the text"; in any
       text, "Down: moves in the text", "Shift+Right: selects text", "Ctrl+A: selects all the text", "Ctrl+C: copies
       the selected text", and in a text of several lines the bundle's paragraph keys by their names; in a list,
       "Down: moves in the list" and "A: finds the next item that starts with what you type" (AX-8.6); "Space:
       checks or unchecks the box", "Enter: presses the button"; "Tab: moves to the next control", "Shift+Tab: moves
       to the previous control"; where the kind of control is not known, "Down: moves in the list or the text";
    6. otherwise "<key>: no command of this window". The line is kept honest rather than complete: a key a window
       handles in a control's own handler and outside its command list is said this way unless rule 4 names it.
  - **A modifier alone** is named once, when it is released with no other key pressed since it went down: "Alt
    alone: opens the menu bar", "Ctrl alone: no command of this window", "Ctrl+Shift alone: no command of this
    window", "Windows key alone: opens the Start menu, Windows' own key"; in a combination only the combination is
    said. A character that arrives with no key press (a program that sends text) is said as "z: a character sent
    without a key press; it types the character" and is not typed.
  - **It ends** on Escape, at once, with nothing held (the user's decision of 2026-11-15, after trying it: "just
    leave plain Escape as the close of the key test"). Escape itself is therefore never named by the test, nor carried
    out: in axStart, where Escape is Hide, it ends the test and hides nothing. Shift+Escape, Ctrl+Escape and
    Alt+Escape are keys of their own, said like any other. It also ends when the window is no longer in front or is hidden, since its keys go elsewhere then, when a notice arrives
    (the notice's keys must answer it), and when the window closes. Every end says "Key test ended", at once, but
    the closing window's. Nothing was changed meanwhile: the focus, the caret, the selection and the text are as
    they were, and the same keys act again.
  - **What it cannot catch is the point.** A key that is not said did not reach axKit: the keyboard itself (an Fn
    layer), Windows (a system-wide hotkey, the Windows key's combinations, Alt+Tab, Ctrl+Alt+Delete, the Print
    Screen key's snipping tool), NVDA (its own commands and the NVDA key) or another program took it. The guide
    says so and names NVDA's input help, NVDA+1, which says whether NVDA saw the key. The background apps' own
    system-wide keys (AX-8.11) are taken by Windows the same way and are not said. Two things Windows does with a
    key before any window sees it happen all the same: Caps Lock, Num Lock and Scroll Lock switch their state (the
    key is still said), and a key that takes the window out of the foreground (the Windows key) ends the test.
  - **How the keys are caught**: a message filter on the window's thread while the test runs
    (`Application.AddMessageFilter`, removed as it ends), ahead of `ProcessCmdKey`, the menus and every control: it
    takes every keyboard message (key down and up, system key down and up, the character messages) addressed to the
    window or a control in it. The automation channel's `keys` and `type` arrive the same way, so the window
    scripts play it (`15-key-test` of axDown, `09-key-test` of axFile and axTask, `04-key-test` of axStart,
    `23-key-test` of axClaude); a modifier alone needs the real keyboard and is in the unit tests and the NVDA plans.
  - **The AI tools** (docs/axKit/mcp.md MC-7.3): a window in its key test is read as ever, and nothing is run in
    it: `axkit_window_run` and a typed data tool that changes are refused with "The window is in its key test: the
    user is trying keys, and nothing is carried out meanwhile. Commands run once the user has ended it."
    (`KeyTest.AiToolsRefusal`). Test a key itself is marked act, so an AI tool that asks for it asks the user first:
    it takes the window's keys until the user ends it.
  - **What the key test does not hold: the automation channel's `menu`.** It clicks a menu item by the start of its
    palette line and sends no key, so a command sent that way runs while the test is on; the scripts use it so (a
    notice opened through the menu ends the test). Only a test run has the channel (its own secret,
    docs/axKit/automation.md); no user and no AI tool can reach it. The channel's `choose` is held since 2026-11-25
    (the maintainer's decision, docs/axKit/command-language.md section 8 (a) 4): `AxKitForm.Choose` asks the gate
    (`CommandGate.Window`) and refuses in the key test, "the window is in its key test: nothing is carried out until
    it ends"; a script's `do` step never sends it there anyway, since the listing gives no key that acts
    (docs/axKit/user-workflows.md WF-2.5), and a test fails on a `do` step inside a key test (R-60).
  - In every F1 text (`BundleHelp.KeyTestLine`) and in the bundle's guide ("If a key does nothing").
- AX-8.13 **Edit mode and navigation mode: Ctrl+Space, the mode key** (the user's decision of 2026-11-15: "the magic
  key"; he is not sure it is the right key and will take his own confusion in use as the sign to change it, so it is
  one line, `BundleKeys.ToggleMode`). The words and the rule of the keys are `AxKit.Core/TextModes` (tested in
  `TextModesTests`); the control is `AxKitTextBox` and the key is handled by the window base (`AxKitForm.Modes.cs`).
  - **Which texts have the two modes: every editable text of several lines, that is every `AxKitTextBox` that is
    multi-line and not read-only** (`AxKitTextBox.HasModes`), wherever it is, a notice included. That covers: the
    editors' text (axDown, axJSON); axClaude's message field and axTerminal's command field ("in case the user is
    constructing some long reply"); and the fields of several lines of a notice or a form (the report's words, F4;
    a long text field of the item window, IA-4.2; axTask's Action keys value, TK-7a.1; the custom arguments of the
    console windows' New session). A field of one line has no modes, nor does a text that cannot be edited; a new
    field of several lines anywhere has them by being made.
  - **The key, in an editable text of several lines**, toggles between **edit mode**, as every text has always been
    (keys type; axDown's and axJSON's Ctrl+letter jumps move), and **navigation mode**, and says the new mode as one
    interrupting announcement: "Navigation mode", "Edit mode". Where the window has a status bar it shows the mode:
    the editors add ", navigation mode" to the file's part ("notes.md, unsaved changes, navigation mode"), the console
    windows ", field in navigation mode" to the state, so that F9 says it too. **Every text starts in edit mode**, at
    every start of a window and in every new notice; the mode is not remembered between runs (the plainest answer to
    the question the item left open: nothing is lost by it, and the user never finds a window that will not type).
    The mode belongs to the text: a field hidden by the control area or a notice keeps its mode until it is shown
    again.
  - **In a text that cannot be edited at all** (axClaude's conversation, axTerminal's terminal, axPreview's text,
    axGIT's details, axHelp's text, a notice's text), the key changes nothing and says "Read only: there is no edit mode here".
  - **Anywhere else** (a list, a button, a field of one line, a window whose focus is in no text) it plays the edge
    sound and does nothing more. No window gives Ctrl+Space a meaning of its own: axClaude's and axTerminal's
    background music, on Ctrl+Space until then, moved to Ctrl+M the same day (`AxClaudeKeys.ToggleMusic`,
    `AxTerminalKeys.ToggleMusic`); a command of the window, so it is taken before the field, where Ctrl+M would be a
    line break. In menu mode the key is the menu's.
  - **In navigation mode**, every key down goes to the text before anything else (the text box's own shortcuts, a
    notice's keys, the window's commands):
    1. **The jump letters**: the window's own Ctrl jumps without Ctrl (`TextJump.Letters`), the next element and with
       Shift the previous one, spoken as the Ctrl key's jump is ("Install, heading level 2", "No next link"). One
       table serves both modes, so a Ctrl key changed changes its letter. In Markdown (axDown, and every other text
       of the rule, whose letters are Markdown's): h heading, 1 to 6 a heading of that level, k link, l list, i list
       item, q block quote, t table, as NVDA's browse mode and axDown's Ctrl keys (AD-D7). In axJSON: k key, j object,
       l array, i item, e error. The conversation's quick key table was not taken: its kinds are the transcript's
       lines (input, output, tool lines), which a text being written has not; the editors' jump pairs over
       `MarkdownOutline` are the shared machinery for a text, and every notice's text already used their letters.
       **Each letter has one sound, before anything moves or is said** (2026-11-16): a letter whose command has
       somewhere to go gives a high glassy tick (`Sounds.NavKey`: 6 ms, a pure 3800 Hz sine, its loudest sample 0.3
       of full scale, a rise over 10 samples, dying away as exp(-700 t), over before the screen reader speaks); a
       letter whose command finds nothing of its kind gives a low buzz instead of the tick (`Sounds.Nothing`: 150 Hz
       with its third and fifth harmonics, 110 ms, 0.3, faded over 6 ms at each end), and "No next heading" is said
       as before. All three sounds of this rule were chosen by the maintainer by ear on 2026-11-16 (the first click,
       2200 Hz with noise, was too like axClaude's line tick; docs/axKit/earcons.md keeps the candidates heard). The
       window finds out whether there is somewhere to go before it plays either
       (`TextJump.Jump`'s `before`), so a key never gives both. Where the letter lands on a heading, the heading
       sound (AX-7.9) follows and takes the tick's place on the device. Never in edit mode, never for a key with
       Ctrl or Alt, never for the moves of rule 2.
    2. **The moves**: the arrows, Home, End, Page Up and Page Down, Ctrl with them, and the paragraph keys move, and
       with Shift select, as in edit mode; Ctrl+Up and Ctrl+Down go by paragraph in every text, axTerminal's command
       field included, whose history they are in edit mode (the history would replace the text).
    3. **Copy and the commands**: Ctrl+C, Ctrl+Insert and Ctrl+A act as ever; every other key with Ctrl or Alt goes on
       to the window and the menus (the Ctrl jumps, F1, F7, F8, F9, Ctrl+M, Ctrl+Alt with a letter in a console window), and
       Escape, Tab and the function keys too.
    4. **A key that would change the text is refused with the edge sound and changes nothing**: every key that types
       a character (a letter that is no jump, a digit that is no jump, Space, a sign, the number pad), Enter (it never
       sends a message or a command in navigation mode), Backspace, Delete, Ctrl+Backspace, Ctrl+Delete,
       Alt+Backspace, Ctrl+V, Ctrl+X, Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z, Shift+Insert, Shift+Delete, and Ctrl+Enter, the
       main action, which would send what the field holds. A character that still reaches the control (Tab in an
       editor, a character a program sends, one that Ctrl+Alt makes on some keyboards) and a paste, cut, clear or
       undo message are stopped by the control itself, with the edge sound. The edge sound, not a sentence: it comes
       on every stray key, where words would be noise (`Sounds.Edge`, AX-8.6). **A plain key with no command at
       all** has a sound of its own since 2026-11-16 (`Sounds.Unassigned`: a wooden bonk, 70 ms, falling from 420
       towards 260 Hz with a wood block's overtone, 0.4; "more deadpan and bonk like" than the blips first heard):
       a key that would type a character with no Ctrl
       or Alt held, that is a letter or a digit that is no jump, Space, a sign, the number pad
       (`TextModes.Unassigned`). In navigation mode the plain keys are commands, and this one is not assigned. The
       edge sound stays for an edit refused (Enter, Backspace, Delete, the keys with Ctrl or Alt, Shift+Insert,
       Shift+Delete, the messages the control stops) and for the ends of lists and texts.
    5. **A menu command that would change the text** (the editors' Undo, Redo, Cut, Paste and axJSON's Format) does
       nothing, plays the edge sound and says "Navigation mode: press Ctrl+Space to edit", since a command chosen
       from a menu needs its reason. The sound is the one the command's key gives (Ctrl+Z, rule 4), so the refusal
       sounds the same from the menu, from the command palette and when an AI tool runs the command (MC-7.5), which
       gives what the menu item gives (2026-11-16; words alone until then).
  - The key test (AX-8.12) says the mode key by what it does where the focus is ("Ctrl+Space: Edit mode or
    navigation mode: to navigation mode", "...: says this text is read only", "...: there is no text with modes where
    you are"), and in a text in navigation mode its letters and refused keys by what they do there ("H: moves to the
    next heading", "A: changes nothing in navigation mode").
  - In every F1 text (`BundleHelp.ModeLine`), the editors' own section, the console windows' field sections, and the
    guides.
- AX-8.14 **Alt and a number: the window's places and views** (the maintainer's decision of 2026-11-16, from the key
  groups, `docs/axKit/key-groups.md`: Ctrl and a number is a heading of that level, so a place is not on it). Alt+1,
  Alt+2 and Alt+3 go to the first, second and third place or view of a window: axClaude's message field,
  conversation and latest response; axTerminal's command field, terminal and latest output; axGIT's Changes, Log
  and Branches; axProcess's Processes, Services and Startup. The Alt key is the command's key: the menus, the F1
  texts and the guides show it, and a sentence that names the key names it ("Changes view first: Alt+1", made from
  the table). Ctrl+1, Ctrl+2 and Ctrl+3 kept working beside them as further keys of the same commands until
  2026-11-26, when they went (AX-8.18 rule 3). Ctrl and a number stays the heading of that level in
  axDown, axPreview and axTask. A menu shows the key with the number row's full stop, "Alt+1." (AX-8.5). The key
  reaches the window as Alt with an arrow does: the window base takes the key press before Windows makes a menu
  character of it, so no menu opens and nothing beeps, in a text field as in a list (checked on the test desktop in
  axTerminal's field and terminal, axGIT and axProcess; with the real keyboard and NVDA it is owed a hearing).
- AX-8.15 **Ctrl and a punctuation key: inside the window** (2026-11-16), the partner of the same key with Ctrl and
  Shift, which is system-wide (AX-8.11). Two keys, both the bundle's (`BundleKeys`), in every window:
  - **Ctrl+Slash** runs Ask about this window, as Shift+F1 does, which stays the key the menu shows; Ctrl+Shift+Slash
    brings up axHelp's conversation from anywhere.
  - **Ctrl+Period** opens the context menu where the focus is: exactly what Shift+F10 does there, which stays, as
    does the Applications key. One place, the window base (`AxKitForm.ProcessCmdKey`): it runs the window's command
    on Shift+F10 whose `When` holds (a list's Context menu in the item windows, axFile, axGIT, axMedia and
    axProcess), never while a notice shows; where no command takes that, it posts Windows' own request for a context
    menu (`WM_CONTEXTMENU`) to the focused control, as Windows does for Shift+F10: a text whose stand-in opens the
    bar's Context menu opens it (AX-9.11), a plain edit control shows Windows' own menu, and a control with neither
    does nothing. No window gets a menu it did not have. The bar's menu opens once Ctrl and the period key are up,
    as it waits for Shift and F10. Not in menu mode, where the keys are the menu's. Ctrl+Shift+Period is
    axHelp's screenshot (AX-8.11), and Ctrl+Period can be no hotkey.
  - Neither is in a window's table or is a system-wide key (`KeyTests`); both are in every F1 text, in the key test
    (AX-8.12) and in the window's list of its commands as further keys of the commands they run.
- AX-8.16 **Selecting things in a list: Space, and one piece of code for every window** (the maintainer's decision
  of 2026-11-22: "we should use space to mean select an item everywhere in axkit", "that way we can use the
  standard item selection mechanics everywhere so that shift and space extends selection and other standard list
  operations are managed in a unified way", "share code across apps to make the list elements and selection
  mechanics and selection actions all work the same and be easy to adjust across the suite"; axFile's selection of
  2026-10-21, AF-3.7, is the model and moved here on its second use).
  - **What a selection is.** A list stays a single-selection list box, where the arrow keys choose the current
    row and NVDA reads it; the selection the commands act on is a set of marks on top of it. A selected row's item
    reads "<its words>, selected", so that an arrow key onto it says so and nothing is said on the others.
  - **The keys**, the same in every window that has a selection, defined once in `AxKit.Forms/SelectionKeys`:
    **Space** selects the current row, or deselects it again, "Selected" or "Deselected", with ", n selected" when
    more than that one is ("Selected, 3 selected"). **Shift+Down**, **Shift+Up**, **Shift+Home** and **Shift+End**
    move the current row and select every row from where the range began to there, as in every Windows list;
    moving back with Shift shrinks the range, the rows selected before it stay, and nothing more is said than each
    row reached, which reads ", selected" by itself; at an end of the list, where the key cannot move, the edge
    sound, as for the arrow alone (AX-8.6). **Shift+Space** selects every row from the nearest selected
    one to the current one, "Selected, n selected"; with no other row selected it is Space. **Ctrl+A** selects
    every row the list shows, "n selected"; **Ctrl+Shift+A** deselects them all, "None selected", and so does
    **Escape** in the list while any row is selected, before Escape does anything else there. A row that cannot be
    selected (a heading) is passed over by the keys that select many, and Space on it says why in the window's
    words.
  - **What the commands act on.** A command that can act on several things acts on the selected rows when there
    are any, and on the current row alone otherwise; a command that is about one thing (Enter, F2, the ways to
    open) stays on the current row. Each window's specification names which of its commands are which. "When
    there are any" is one selected row too, wherever the current row is: in a navigator this is decided in one
    place, the item window's `RowsInHand`, and a window has no other way to the selected rows (the review of
    2026-11-27: axPass, axProcess and axMCP each took the selected rows only when there were two or more, so that
    Delete with one row selected and the arrow keys moved on acted on the row under the arrow keys;
    `RowsInHandTests`).
  - **How long it lasts.** The selection lives in one level (a folder, a list): showing another drops it; the same
    level read again keeps the rows still there. The marks are on rows the list shows, so that the count said and
    the rows acted on are one set.
  - **The code.** `AxKit.Core/Marks` is the marks and the words (each key one method over the shown rows, each a
    `MarkRow` of a key, a name and whether it can be selected; pure, tested in `MarksTests`).
    `AxKit.Forms/ListSelection` is one list's selection in a window: it rewrites the items whose mark flipped in
    place, so that the list is not made again under NVDA, says what the key says, and makes the four commands
    that select as they move. A window gives it its rows and its words for an item, makes its commands on
    `SelectionKeys`, and asks it what is selected. No window keeps marks, their keys or their words of its own.
  - **The windows on it**: axFile's folder list (AF-3.7), which also records each change as a line of its Actions
    list; and the item window's navigator (`ItemForm`: `HasSelection`, `CanSelectRow`, `NothingToSelect`,
    `RowsInHand`), switched on by axTask, whose items it selects (docs/axTask/SPEC.md TK-4.7; its status bar says
    how many are selected, and the target an AI tool's command is asked about names the selection too). An item
    app says it has a selection only once its commands act on one, so that nothing is ever selected that a
    command then passes by, and it says which rows: since 2026-11-22 (framework step 10) axPass selects entries
    outside the Recycle Bin, which Delete moves there with one undo (docs/axPass/SPEC.md PW-6.4a), axMCP selects
    Your servers, which Delete removes with one undo (docs/axMCP/SPEC.md MP-1.5a), and axAutomate selects the
    workflows of a folder, which F5 runs (docs/axAutomate/SPEC.md AU-4.4a); on any other row Space is the edge
    sound. The navigators that came onto the type the same day select the same way: axMedia's files, axProcess's
    processes and startup programs, axGIT's changed files. Every
    other list with a use for a selection follows, one app at a time (the priorities list). axMedia's queue is
    on it since the same day (docs/axMedia/SPEC.md AM-3.5): Space selects files, Delete takes the selected ones
    out, and pause moved from Space to Ctrl+Enter there (AM-D7).
  - **Space no longer marks a task done** in axTask: that is Ctrl+Enter, the bundle's main action key, there
    (TK-4.4).
- AX-8.17 **Filter and sort in every navigator's list** (the maintainer, 2026-11-22: "i want any list to be able
  to be filtered or sorted"; from axFile, AF-3.8, where both were until then; docs/axKit/framework.md FW-4.10a).
  In every window on the navigator (axTask, axPass, axMCP, axAutomate, axMedia, axProcess, axGIT, axFile) the
  Options menu begins with **Filter…** and **Sort by ▸**. The item window puts them there itself, so that no
  app adds them and none can be without them. Neither has a key (his choice of the same day, when the one key
  axFile had, Ctrl+Shift+F, was Find everywhere in axTask and Fetch in axGIT); both are in the command palette.
  - **Filter…** opens the notice "Filter" with the field "Name contains". The level then shows only the rows
    whose name contains the text, in any case (a text with `*` or `?` is a pattern over the whole name instead,
    `*.mp3` or `chapter ?`, so that Select all after it chooses the entries by a pattern for one operation on them
    all; `ListArrange.Passes`, asked 2026-11-22), and the list's name says so after the level's own: "Home, 4
    items, filter tap, 1 shown" ("3 of 24 items, filter chap" in axFile until then). What is said is the
    list's new name and the row landed on. A heading stays while a row under it, up to the next heading,
    passes; a heading's own name is not looked at. A level whose rows all fail the filter shows the one line
    "No names contain <text>". An empty text clears the filter, "Filter cleared", and so does **Ctrl+Delete** in
    the list (`ItemKeys.ClearFilter`, AX-8.18; Escape, once nothing was selected, until 2026-11-26). The filter lives in one
    level: going into a list, up a level or to another folder drops it; the same level read again keeps it.
    The selection is on shown rows only, so a row the filter hides loses its mark.
  - **Sort by ▸** holds the orders of the list, one checked; choosing one says "Sorted by <order>" and keeps
    the row you are on. By default they are **The list's own order**, as the app gives its rows, and **Name**:
    Windows' natural order (2 before 10), lists before the other rows as folders come before files, a heading
    left in its place and the rows under it ordered among themselves. An app with orders of its own names them
    and keeps the chosen one in its settings: axFile Name, Date and Size (AF-3.8), axProcess Memory, Processor
    and Name, the order of its processes (PR-3.2). The default choice is kept for the window, not saved. A list
    whose order is what it does keeps the sort true to it: in axMedia Name reorders the queue itself, so that the files play in the order shown, and The order added puts it back (AM-3.6). One order alone would leave Sort by out.
  - In code: the rules are `AxKit.Core/ListArrange` (tested), the window's side
    `AxKit.Forms/ItemForm.Arrange.cs`; an app gives its level's rows (`ReadLevel`) and the item window decides
    which are shown and in which order, so a row's place in the list is never its place in the app's own data.
- AX-8.18 **The rules of the keys, on one page** (the maintainer, 2026-11-23: "it is too much for my working memory
  to process all those which is why i need us to settle on simple heuristics"; settled through the question tool
  the same day). What a combination means comes from its modifier, whatever the key, so that a key never learnt
  can be guessed. Every key of a table falls under one rule; the key map (`key-map.md`, `key-families.json`) is
  the same rules by family, and a key that fits none is a fault of the table. The maintainer's principle, 2026-11-26:
  "we want absolute simplicity on consistency whenever possible. the fewest rules that have the fewest possible
  exceptions, none is ideal. but we want things to be reasonable and match user expectations when there is muscle
  memory from main windows interactions. we should be very intentional." So an exception stands only for a key
  Windows itself taught (copy, rename, refresh, the next pane), each with its reason in `key-families.json`.
  1. No modifier: move and read where you are. Shift with the same key: the other direction, or select as you move.
  2. Ctrl with a letter: act on what you are on. Ctrl+Shift with the same letter: its reverse or wider form. In a
     text, Ctrl with NVDA's browse-mode letter jumps while you type (h a heading, k a link, l a list, i a list item,
     q a block quote, t a table in the editors; axJSON's k, j, l, i and e), since the letter alone types there and
     moves only in navigation mode (the maintainer's decision of 2026-11-26).
  3. Ctrl with a number: the window's own numbered action (a heading level in the editors, an action key in
     axTask). Ctrl+Shift with the same number: its reverse. Nothing on numbers works outside the window. A place is
     Alt with a number, never Ctrl (rule 4): the Ctrl+1 to 3 kept beside Alt+1 to 3 in axClaude, axTerminal, axGIT
     and axProcess went on 2026-11-26, and axTask's All tasks moved from Ctrl+0 to Alt+0.
  4. Alt with a letter: a menu. Alt with a number: a place of the window (AX-8.14). Alt with an arrow: the choices of
     what you are on (axJSON's values, axMedia's speed, axTerminal's recent commands on Alt+Down).
  5. A function key alone: say or show. Shift with it: the reverse or another form. Five of them mean the same in
     every window, in the maintainer's order of 2026-11-26 ("f7 can be the announce, f8 can be read with details,
     and f9 can be status"), with F1 before them: **F1** shows the command palette with the keys, palette and help
     in one (AX-10; Ctrl+Comma the same, a second key on trial; Ctrl+Shift+P until then); **F7** reads in full what you are on, a bundle key (`BundleKeys.ReadInFull`: in a
     notice or a form its text, AX-7.11; in a window its own reading, which it registers as a command on the key,
     the edge sound where it has none; Ctrl+Q until then); **F8** reads with details, a key of each window
     (Information in axFile, axStart and axMedia, the favorite's folder, axCalc's other bases, axClaude's background
     work, axTerminal's current folder, axAutomate's last report); **F9** says the status, a bundle key (AX-7.10;
     F8 until then); **F11** shows the text another way (the editors' word wrap, Ctrl+Shift+W until then);
     **F12** goes to: the go-to field (Go to line in the editors, Go to folder in axFile, Go to time in axMedia;
     Ctrl+G, Alt+D and Ctrl+T until then). The keys Windows taught stay as listed exceptions: F2 rename, F3 find
     next, F4 the report, F5 refresh and reload, F6 the next pane.
  6. Ctrl with a function key: open a standalone app; Ctrl+Shift with a function key: open an access layer, so that
     the key says which kind of app it opens. Each group of apps (AX-9.2) takes one block of four function keys, as
     the keyboard groups them, F1 to F4, F5 to F8, F9 to F12, the groups in the bundle's order and the apps within a
     group too (the maintainer, 2026-11-23: "they should be grouped into similar apps on the function keys so that i
     can have a heuristic on that also"); which app has which key is written only on the generated `apps.md` and in
     the F1 texts, both made from the code. The same in every axKit window, shown in Apps and Access layers, handled
     by the base before the window's own keys (`AxKit.Core/AppLaunchKeys`, `BundleKeys.LaunchKey`). Only axKit's
     windows take them, nothing for all of Windows. Those are the defaults; **the user's own table** (the
     maintainer's decision of 2026-11-25) is `app-keys.json` in the settings folder (`AppPaths.AppKeysFile`,
     `%APPDATA%\AxKit`, beside the theme file, since it is a setting and deleting it gives back the defaults,
     AX-5.11), a JSON object of an app's name and its key in the words the menus show, `"axFile": "Ctrl+F11"`, or
     `"none"`, which takes the app's key away; an app the file leaves out keeps its default, and an `about` member
     holds the file's one sentence of explanation, since JSON has no comments. Options, Edit app keys file (AX-9.1)
     writes the file with every app's key as it holds when there is none, and opens it in axJSON. The file is read
     once as a process starts, before any window exists (`BundleKeys.LoadAppKeys`; each window is its own process,
     B-2), so a change holds in every window opened after it, and in a background app from its next start. The
     shape of this rule stays: a standalone app takes Ctrl with a function key, an access layer Ctrl+Shift with one,
     so that the key still says which kind of app it opens. An entry is skipped, its app keeping its default, and a
     line of `axKit.log` says why, never a notice and never a crash, when it names no app, names an app a second
     time, is no key that can be read, is not the shape of the rule, is a bundle key, or is the key another app ends
     up with (AX-8.4: two apps may swap their keys in one file; an entry that takes the key of an app the file leaves
     out is refused); a file that is no JSON object gives every default. Everything that shows the keys follows the
     table that holds: Apps and Access layers, the F1 texts (the default blocks, then the keys the file changed) and
     what F1 lists from the window's commands; `apps.md` and the guides give the defaults. No window's table holds a
     Ctrl or Ctrl+Shift function key (`KeyTests`), so a key the user gives an app ends no window's command.
  7. Ctrl+Shift with Space or a punctuation key: the few keys that work anywhere in Windows, one per background app
     (AX-8.11). Not Ctrl+Alt, which NVDA's table keys, Windows Magnifier and the AltGr letters of many keyboards take.

  Beside the seven, the keys that end, take away and switch (the maintainer's decisions of 2026-11-26: "esc should
  go back or close. please move clearing to another key and make it consistent"): **Escape** only goes back or
  closes; **Delete** removes what you are on; **Ctrl+Delete** clears what you typed, the same key everywhere
  (`ItemKeys.ClearFilter`: the navigator's filter, FW-4.10a, and axCalc's field, both Escape until then; in those
  places Windows' Ctrl+Delete, the next word deleted, gives way); **Ctrl+Space** turns between the two modes
  (AX-8.13), and **Ctrl+Enter** does the most frequent action where you are (AX-8.10 rule 3).

  Every window's F1 text begins with these rules, one short line each, for the ear (`BundleHelp.RulesSection`, the
  maintainer's ask of 2026-11-26: "so that people learn the principles and can guess the keys"); the text is
  written once there and every window's F1 text starts with it.

  The moves of 2026-11-26 that brought the tables to the rules, besides the function keys above: Bookmark this
  line Ctrl+K (axCalc, axClaude, axTerminal; Ctrl+Shift+K); Interrupt Ctrl+I (axClaude from Shift+Escape, axTerminal
  from Ctrl+Shift+C; axClaude's raw Ctrl+C stays in the palette without a key); axClaude's Next permission mode
  Ctrl+P (Ctrl+Shift+M) and Send queued messages now Ctrl+Shift+Enter (Ctrl+Shift+S); axGIT's Fetch Ctrl+R, Pull
  Ctrl+L and Push Ctrl+P (Ctrl+Shift+F, L, H); axTask's Hide done tasks Ctrl+H (Ctrl+Shift+D); axFile's Add to
  axMedia's queue Ctrl+Q (Ctrl+Shift+Q) and New text file Ctrl+T (Ctrl+Shift+T), its Alt+Up and Alt+D taken away
  (Backspace goes up, F12 to a folder); axJSON's Format Ctrl+R (Ctrl+Shift+F); axAutomate's Run Ctrl+R (F5, which
  is Refresh there now as in the other navigators); axTerminal's Recent commands Alt+Down (Ctrl+R); axMedia's
  Faster and Slower Alt+Up and Alt+Down (Ctrl+Up and Ctrl+Down, which go between sections there now). What stays
  an exception, each with its reason, is `key-families.json`.

  The consolidations of the same day (the maintainer: "i agree with all of your recommendations, please implement
  them"): **Ctrl+N** makes a new one of what the window holds, a document in the editors, a session in axClaude and
  axTerminal, a window in axFile, axMedia and axPreview (the editors' Ctrl+Shift+N New window gave way; Session,
  New window stays there without a key, and axKit, Apps opens a new window of any app); **Ctrl+Shift+Delete** clears
  a history, axCalc's and axClip's as before and axTerminal's transcript (Ctrl+L until then); **Ctrl+W** opens File
  Explorer in no window (Show current folder in, File Explorer stays without a key, and axFile's Context menu keeps
  Show in File Explorer, Ctrl+Shift+W until then), so Ctrl+W and Ctrl+Shift+W are free in every window; **Enter**
  opens or confirms and **Ctrl+Enter** does the most frequent action, each window that differs a question for the
  maintainer; one **Options** menu holds the window's settings and, at its end, those of all of axKit (AX-9.1).

### AX-9 The bundle's menus: Options, Window, axKit, Help

- AX-9.1 Every window's menu bar is the app's own menus first, then four of the bundle's, in this order, the same in
  every window and each built once in the window base, so that no app has a copy (5.7.0, 2026-11-10, the user:
  "organize things ... into clear categories of user intent", "make sure these menus are inherited by all axKit
  apps", then "navigate then windows then options then axkit then help", and the same evening Options before Window: "the options are more about the app itself and the window starts to bridge into the axkit stuff"):
  - **Options** (O), the settings (AX-6.4), right after the app's own menus (Navigate the last of them where the app
    has one): the app's own submenus, then Text, General and Developer; then, after a separator, **the settings of
    all of axKit**, the same in every window and added by the base (`AxKitForm.AddBundleOptions`; the consolidations
    of 2026-11-26, the maintainer's "one Options menu in every window", in the axKit menu until then): Theme ▸ (H;
    `theme.md` TH-4), Open the sounds folder (O, AX-7.12), Edit app keys file… (K, AX-8.18 rule 6), and the bundle's
    two switches, both in `%APPDATA%\AxKit\bundle.json` (`AxKit.Core/BundleSettings`): Check for updates at start (U,
    AX-3.1; not in the Store's copy) and **Keep a log of events** (E; `events.md` section 9 point 5, the maintainer's
    decision of 2026-11-25 for a switch "under Options"; off by default). While it is on, every event raised at the
    door (`AxKit.Forms/EventDoor`), by its sound or by its words, is a line of `%LOCALAPPDATA%\AxKit\logs\events.log`:
    its local time and its kind's id, "2026-11-25 14:03:05.123 refused", never a word of what was said or shown; one
    kind once in one turn of the window's thread. Nothing is written while it is off; past 1 MB the file becomes
    `events.old.log`, replacing the one before; it stays on the computer (`AxKit.Core/Events/EventLogger`). Turned
    in one window, it holds in the others from their next activation; the item says "Keep a log of events,
    checked" or "not checked". A new bundle-wide setting joins this section, never an app's own part of Options.
  - **Window** (W; `AxKit.Forms/WindowMenu`, `CreateWindowMenu`), after Options: Open windows ▸ (O),
    Recently closed ▸ (R) and Close all windows (C); then, after a separator, Say the status (S, F9, AX-7.10), the
    one item about this window alone.
  - **axKit** (K since 2026-11-11, A before; `AxKitForm.CreateAxKitMenu`): what is the bundle's rather than the app's and is no setting, each a submenu: Apps ▸ (A) and Access layers ▸ (L), the bundle's apps
    (AX-9.2; in the Window menu until 2026-11-23, the maintainer: "you should move the apps and access layets into
    the axkit menu rather than the window menu"), then after a separator Claude ▸ (C;
    `CreateClaudeMenu`, `ai-help.md` AH-1.1: Use Claude in axKit, the switch; Ask about this window…, Shift+F1; Folders
    Claude may read… and Claude's working folder), AI tools ▸ (I; `mcp.md` MC-6.1: Let AI tools read and open axKit, Let AI tools run commands (MC-7.2,
    2026-11-11, `axkit_window_run`, a window's command run by its name, said in the window, an act one asked first,
    never in a window in front the user is typing in, and an item added to axTask's lists, MC-8; turned off with the access and by Forget AI tools; a run may move
    the caret or the focus as the command's key would, the one exception to AX-7's caret rule, `mcp.md` MC-7.8, MC-D8),
    Add axKit to Claude Code, Forget AI tools, Read the log). The two services keep their switches with their
    commands, one subject each. What stood here until 2026-11-26 and is a setting moved to the end of Options: Theme
    ▸ (Options, Text, Theme until 5.7.0), Open the sounds folder, Edit app keys file…, Check for updates at start and
    Keep a log of events. A new bundle-wide action that is no setting joins here; a setting joins Options' section
    for all of axKit; neither ever joins an app's own menus.
  - **Help** (H; `AxKitForm.CreateHelpMenu`, which the window calls with its guide and pages): Command palette… (P,
    F1, AX-10; Ctrl+Shift+P until 2026-11-26), Keyboard shortcuts (K, the F1 text as a text of its own, with its
    heading keys and find; F1 until 2026-11-26, no key since), User guide (G), the app's resources ▸; Send a report… (S, F4),
    Your reports (Y, docs/axKit/reports.md RP-2.2) and Diagnostics ▸ (D; `CreateDiagnosticsMenu`, 2026-11-15, the
    user's submenu: what finds out why something does not work, the same two items in every window: Test a key, T,
    AX-8.12, and Copy diagnostics, C, which was Options, Developer, Copy diagnostics until then; Developer keeps Open
    settings file and the window's own items, such as the consoles' Record raw stream, which are settings and files
    of the app and not a diagnosis of the window); Check for updates (U), Update to (T, while a newer release is
    known) and About ▸ (A, AX-9.9).

  The lists are filled each time the menu opens, so that they show the windows and the files of that moment.

  **Every bar is the same eight menus, in this order, each with its letter** (2026-11-11, the user's word, B-19):
  **Session** (S), **Actions** (A), **Navigation** (N), **Context** (C), **Options** (O), **Window** (W), **axKit**
  (K) and **Help** (H). The window base puts the bar together (`AxKitForm.PlaceMenuBar`, `BarMenus`), naming the
  window's five menus itself, so that no window spells one differently; `MenuTests` fails on another order or a letter
  used twice. Every item of an app goes in one of them by one rule:
  - **Session**: what concerns the window's file, folder, database, session or connection as a whole: new, open,
    recent, save, show in, refresh, close, Exit last.
  - **Actions**: everything else the app does, the same wherever the focus is: new item, new list, undo, lock, run,
    the app's own commands on the whole thing ("Operations" in 5.7.0).
  - **Navigation**: what moves the focus or the selection, or finds: go to, find, next and previous heading or
    section, bookmarks, levels, the views, the panes ("Navigate" until 2026-11-11).
  - **Context**: what acts on the thing the focus is on, the selection or the caret, and changes with the focus
    (AX-9.11).
  - **Options**: what changes how things are shown or behave, a setting: the app's own items or submenus first, then
    Text, General and Developer (what View menus held until 2026-11-11 came here or to Navigation).
  Within a menu, items stand in groups parted by separators. No item is deeper than the bar's menu, a submenu and
  an item (axKit, Access layers, axClaude is the deepest; `AxKit.Forms/MenuShape`, a deeper one logged as an error as
  the bar is placed, `MenuTests`; 2026-11-11, when axTask's Remove a project folder, axFile's Remove a program and
  axTerminal's Remove a shell came up a level). A submenu holds one kind where it can, commands or check items
  (toggles and choices); the mixes kept are Options' Text (the app's display toggles beside the text size), axKit's
  Claude, AI tools and Theme (a switch with its commands), each one subject.
  Actions, Navigation and Context are never empty: one with nothing yet holds one enabled item that says so ("No
  actions here yet", "Nothing to move to here yet", "Nothing for the focus here yet"), since WinForms skips a disabled
  item and NVDA reads nothing. The keys are the same as before; only where an item stands changed.
  History: 5.7.0 named the first menu Session and the second Operations (P), the item apps and axProcess keeping menus
  named for what they act on (List, Item, Field; Process, Service, Startup), only the one of the focus enabled; 5.6.0
  had one axKit menu (A) holding the window lists, a Claude submenu, the palette, the reports, the updates and About;
  until 5.6.0 Open windows by type ▸ (one submenu per app) stood after Open windows.
- AX-9.2 **Apps ▸** (A) **and Access layers ▸** (L), in the axKit menu (AX-9.1): one item per app of the bundle (`Dispatch.Apps`), the standalone
  apps under Apps and the access layers under Access layers (the maintainer, 2026-11-23: "we should split axKit Apps
  and axKit Access Layers in the menus and everywhere in the menus and documenntation. these are very importantly
  different things"; one New window ▸ held both until then), each item with the letter after ax as the mnemonic and
  its key (AX-8.18 rule 6). It starts `AxKit.exe <verb>` with no arguments: a new process and a new window (B-2), the
  app starting as it does from its Start menu entry. The apps stand in groups, a separator between them, each group
  on its block of four function keys (`AppGroup`; the groups of 2026-11-23, by what the apps are for): the apps
  getting around, documents and media, and tools; the access layers programs, AI, and utilities (the maintainer,
  2026-11-23: "make the ai function keys be there own group ... we want complete separation of standalone apps from
  access layers ... axhelp and axMCP are probably more in the utilities and ... c+s+f5-8 can be saved for other ai
  access layers that will come later"; the Claude apps were one group, last, from the ask of 2026-11-09). Which app is in which group is written only on the
  generated `apps.md`. Every list of apps in the windows has this order and these separators (`WindowMenu.AddApps`);
  axFile has the same split as two entries of its Context menu, Open with axKit apps and Open with axKit access
  layers (AF-4.9). A new app is one line in `Dispatch.Apps`,
  with its group, what it opens and whether Show in offers it; every list, its key, Show in (AX-9.10) and Recently
  closed (AX-9.4) follow from that line.
- AX-9.3 **Open windows ▸** (O): every open window of the bundle's apps except this one, the most recently used first.
  The order is the one Windows keeps its top-level windows in, which is Alt+Tab's, so nothing is stored and Alt+W, O,
  1 is always the window the user was in before this one. (Open windows by type ▸, one submenu per app with that
  app's windows and this one checked, stood here until 5.6.0, AX-9.1.)
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
  (AX-5.1), written whole by the window that closes or swaps (`AxKit.Core/RecentlyClosed`, through `SettingsFile`).
  One window writes at a time (2026-11-16, `repairs.md` R-68): the reading, the adding and the writing happen under a
  mutex named after the file, so that windows closing in the same instant are all on the list; a window that does
  not get its turn within half a second writes nothing, logs that and closes, and never waits longer. Only whole
  paths are kept, in this list and in the apps' recent lists (`RecentPaths.IsWhole`: a drive or a share and what
  follows): an entry that is anything else, written into a file by hand or by another program, is neither listed nor
  handed to an app, where text such as `--secret` would be read as an option. Nothing else about windows is stored.
- AX-9.5 A window's or a file's item is its number, a full stop and a title, "1. notes.md, axDown, C:\notes\notes.md":
  the number is the mnemonic (1 to 9, then a to z), so that Alt+W, O, 1 or Alt+W, R, 1 acts without an arrow key, and
  the full stop is a pause before the name (AX-8.5). A window's title is the window's own; a closed item's is made by
  the same rule as a window's (AX-9.6, since 2026-11-26): the file's or folder's name, the app, the full path
  (`RecentlyClosed.Title`; until then the name, the full path in parentheses, " - " and the app's title), and axStart
  lists what was closed in the same words (AS-2.5). The apps' own recent lists have the shape of before,
  "1. myproject (C:\src\myproject)" (axClaude FR-8.5, axDown AD-4.1). Enter brings a window to the
  front, restored when it was minimized. A list with nothing in it holds one item that says so and does nothing, enabled
  ("No other open windows", "No open axDown windows", "Nothing recently closed"; the apps' own lists likewise, "No
  recent folders", "No recent files"): WinForms skips disabled items when the keyboard moves through a menu, so a
  submenu of only a disabled item opens with nothing selected and leaves the screen reader with nothing to read.
- AX-9.5a **A remembered path that is not on this computer is not looked at during a start** (2026-11-16,
  `repairs.md` R-64; `LocalPaths.AtStart`, tested in `LocalPathsTests`). A window that starts without a path opens
  what it remembered: axFile its last folder, axGIT its last repository (the newest of the recent ones that is
  there), axTerminal and axClaude their last folder, axPass its last database, axTask the list of the last run,
  axAutomate its folder. Asking whether such a path exists can take the network's time limit when it is on a share
  or a mapped network drive that is away, and that wait was on the window's thread before the window showed, in
  silence. So the path is asked about only when it is on a drive of this computer (`LocalPaths.IsLocal`, which
  touches no network). One that is not is not looked at: the window opens on what it opens when nothing is
  remembered (the profile folder, "No repository", the current folder, the folder question, no database, the top
  level, no folder) and says, once it shows, without interrupting, or as a system line in the console windows:
  "<path> is on the network and was not opened at start; Open recent opens it." (`LocalPaths.NotOpenedAtStart`).
  Where the window has no Open recent submenu the sentence names its own command: "Open database" in axPass, "Open a
  folder of workflows" in axAutomate, "Right on its list" in axTask. Nothing is asked.
  - A path given on the command line is the user's own ask and is opened. axFile and axGIT look at it off the
    window's thread; axPass takes a database that is not on this computer as given, and KeePassXC's tool says so
    when it is not there. Left as they were, each a wait before the window when the path is on a share that is away:
    the folder or file given to axTask and axAutomate, which read their folder on the window's thread in any case,
    the folder given to axClaude and axTerminal, and `AxKit.exe <path>` without a verb, which must know whether the
    path is a folder or a file to choose the app (AX-1.2).
  - `LocalPathsTests` reads the sources and fails on a line that asks `Directory.Exists` or `File.Exists` of a path
    the settings remember (`settings.Last…`, `settings.Recent…`, `settings.Folder`) outside `LocalPaths` and
    `RecentMenu`. It reads one line at a time; a check written over two lines is not seen.
  - axTask's limit: its top folder is read on the window's thread whatever it is on (docs/axTask/SPEC.md TK-D3), so
    a top folder on a share that is away still waits.
- AX-9.6 The windows are the visible, unowned, titled top-level windows of every process named as this one, told
  apart by the app their title names. **Every window's title is one rule** (the maintainer's word of 2026-11-26:
  "the window of any axkit app should announce the open directory name before naming the app ... project axclaude
  c:\path\to\project ... axdown should name the file then app then path to file"; `AxKitWindows.Title`, through
  `AxKitForm.SetTitle` with an `Opened`): what the window holds open first (a folder's or a file's name, a list's,
  a database's or a repository's), then the app's name alone (the access layers too, without "access layer for",
  AX-11.2), then the full path, a comma and a space between the parts so that NVDA reads three parts with a pause:
  "notes.md, axDown, C:\work\notes.md", "myproject, axClaude, C:\src\myproject", "music, axFile, C:\work\music",
  "Home, axTask, C:\lists\Home". A window with nothing open has the app's name alone
  ("axGIT" with no repository, "axFile" before its first folder, "axCalc", "axMedia" while nothing plays); an
  editor's untitled text is a thing open, "Untitled, axDown"; a thing with no path has no third part ("This PC, axFile", "This computer,
  axProcess", a smart list of axTask), and a path that is its own name (a drive's root) is said once ("C:\,
  axFile"). The editors' star of unsaved changes stays where it stood: before the full path, "notes.md, axDown,
  *C:\work\notes.md", and before the name of an untitled text, "*Untitled, axDown". axTerminal adds its shell as a
  fourth part ("work, axTerminal, C:\work, PowerShell"); axHelp's conversation keeps its own title, "axHelp"
  (axClaude FR-9.2), and the hotkey windows (axStart, axClip, axHelp's hotkeys) theirs, the name alone, by which a
  second start finds them. The app is read off a title (`AxKitWindows.AppOf`) as its first part after the first that
  is an app's name, so that a name with a comma ("Smith, John.docx") still finds its app; a title of one part is the
  app. From 2026-11-24 to 2026-11-26 a title was the app's title first ("axFile folder C:\work\music", the
  maintainer's ask of 2026-11-23); before that it ended with the app (" - axDown"). A window of
  none of the apps (the bundle's own message box, AX-1.4) is listed nowhere. The lists are pure code in
  `AxKit.Core/AxKitWindows` and `AxKit.Core/RecentlyClosed`, unit tested; only the listing and the switch (user32
  `EnumWindows`, `SetForegroundWindow`) and the menu itself are in `WindowMenu`.
- AX-9.7 Only axKit's windows are listed; Alt+Tab stays the way to everything else. Alt+W, Alt+A and Alt+H are menu mnemonics, not keys
  of the tables (AX-8); the F1 texts name it. A separator carries no text and NVDA passes over it; the groups are
  told apart by their names, not by headers, which would be one more stop on every pass through the menu.
- AX-9.8 **Close all windows** (C) posts the close command Alt+F4 sends (`WM_SYSCOMMAND` `SC_CLOSE`) to every other
  open axKit window and then to this one; each window closes as it does on Alt+F4 and asks there what it would ask
  then (axDown's Save changes?, axClaude's Close axClaude? while Claude works), so nothing is lost without a question.
- AX-9.9 **Check for updates (axKit n)…** (U) and **Update to axKit n…** (T, while a newer release is known) are the
  window's own update items (AX-3.1), at the end of Help with About. **About ▸** (A) is the bundle's About in sections,
  the same in every window (`AxKit.Core/BundleAbout`), each shown as a notice of the window: **Version** (V; the
  version, the apps, the latest release as the window knows it, the releases page, the settings and logs folders),
  **Licence** (L; `NOTICE.md`, embedded: the GNU AGPL 3 notice, the additional terms of its section 7, the warranty
  and liability disclaimer in full words, and who is responsible for what: the standalone apps, the access layers
  with what axKit, the other program and the user each do, and the bundle's services; since 2026-10-29; `LICENSE`
  beside it is the AGPL),
  **Attribution** (A; the maker, the Anthropic note, what axKit is built with), **Privacy** (P; `PRIVACY.md`, embedded:
  everything axKit contacts over the network and when, the outside programs it runs, what stays on the computer; since
  2026-10-25), **Third-party notices** (N; `THIRD-PARTY-NOTICES.md`, embedded: the .NET runtime and Windows Forms in
  the program under MIT, the Unicode data), **axKit guide** (G; `docs/axKit/README.md`,
  the zip's guide, embedded by `AxKit.Core`: install, start, update, remove; its headings take the window's heading
  keys, AX-7.2), and **axKit on GitHub** (H; the repository page, `UpdateCheck.RepositoryPage`, in the browser).
- AX-9.10 **Show current folder in ▸** (2026-10-24, one builder, `AxKitForm.CreateShowInMenu`): the window's File
  Explorer item (no key; Ctrl+W until 2026-11-26, the consolidations of that day), then the apps that work in a folder (`App.ShowIn`: axFile, then the access layers axClaude, axTerminal, axGIT, in
  that order) but the window's own, each a new window on the file or folder in hand: axClaude and axTerminal on a
  file's folder, axGIT on the repository that holds it, axFile on the folder or with the file selected
  (`Dispatch.PathFor`). The windows that hold a folder or a file have it: axClaude and axTerminal ("Show current folder
  in"), axGIT ("Show current repository in"), axFile ("Show current folder in"), the editors and axPreview ("Show
  current file in") and axMedia ("Show current file in"). With nothing in hand the window says why ("No project
  folder", "No repository", "No file yet", the empty queue). A path that is gone says "Folder missing" or "File
  missing", by what the window holds, and starts nothing, File Explorer included (`SaidMissing`, `OpenInExplorer`;
  one wording in every window since 2026-11-09, axClaude's and axTerminal's before, while the others started the app
  on the gone path or File Explorer on its parent).
- AX-9.11 **Context follows the focus** (2026-11-11, B-19; `AxKit.Forms/ContextBarMenu`): the bar's Context menu holds
  what acts on the thing the focus is on, and only that: in the item apps the list's, the item's or the field's
  commands by where the focus is and what is selected (`ItemForm.CreateContextMenu`); in axProcess the view's process,
  service or startup commands. The window keeps one menu of items for each kind of thing, never on the bar, and
  Context takes the items of the one for now each time it opens, as the palette reads it and as an AI tool asks
  (every reader reads the bar alone, `CommandPalette.Entries`, which refreshes Context first; an item held off the bar is not among the entries), so
  that the palette and `axkit_window_commands` offer the commands of the focus and no other. With nothing to act on
  it holds one enabled item that says why ("Nothing selected", "A heading: nothing to act on", axPass's "Locked: Enter
  unlocks the database"). **Shift+F10, the Applications key and a right click open Context** in every window
  (`OpenBarMenu` with C).

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

- AX-6.6a **The window's own keys work from an open menu** (the maintainer, 2026-11-23: "ctrl+function and
  ctrl+shift+function keys do not work when focus is on a menu in axkit ... any hotkey that does not need a selection
  or caret should work when focus is on the menu"). In menu mode the keys belong to the menu (AX-6.6), except the keys
  of the whole window, which need no selection and no caret: the command palette (F1 and Ctrl+Comma), F4, asking Claude, Ctrl+S and
  the app keys (AX-8.18 rule 6). The menu closes first and the key runs once it has, so that nothing opens over an
  open menu (F1 did that on 2026-10-23 and left the bar unusable). F9 says the status and leaves the menu open; the
  text size keys worked there already. `AxKitForm.IsWindowKey`, `RunWindowKey`, `CloseMenus`. With a drop-down
  open, WinForms hands the keys to the drop-down, a window of its own, and the form never sees them (found by the
  maintainer on 2026-11-24, when the first version worked on the bar alone): a filter of the thread's messages
  (`AxKitForm.MenuKeyFilter`, added once with the first window) takes such a key before the drop-down does, for the
  window whose bar the drop-down hangs from. Menu mode opens only in the window in front, so the check is an NVDA-tier
  script (`tests/nvda-scripts/axCalc-keys-from-a-menu.txt`).

### AX-10 The command palette

The user's ask of 2026-10-21, specified 2026-10-22: one key in every window finds any of the window's commands by
name, so that a command is reached without knowing its menu or its key, and its key is learnt on the way. Since
2026-11-26 it is the keys' help as well, palette and help in one (the maintainer: "is there another key we can use
for the command palette? that seems like something very important and i want it to be easy to use frequently").

- AX-10.1 **F1** (`BundleKeys.CommandPalette`, AX-8.2) in any window opens the palette as a notice of the window
  (AX-6.4, AX-7.2), titled "Command palette": one field named "Search commands" with the caret in it, and under it a
  list that never takes the focus, axStart's shape (docs/axStart/SPEC.md AS-3.1, AS-D2), named "Keys and commands,
  n" while the field is empty and "Commands, n" once something is typed. **Ctrl+Comma** (`CommandPaletteAlso`)
  opens the same, a second key on trial (his word: "it cannot hurt to have both f1 with the palette and help, plus
  ctrl+, as a second option that launches the same thing"; after his use of both, one stays or both do).
  Ctrl+Shift+P, the palette's key until then, gave way; F1 showed the keyboard shortcuts' text until then, which is
  Help → Keyboard shortcuts now, without a key (AX-7.2a), and stays the text that Ask about this window and AI
  help's documentation read. Help → Command palette… (the first item of Help, AX-9.1) opens the palette too, so
  that it is found from the menus. While another notice shows the key does nothing; from an open menu it closes the
  menu first (AX-6.6a). In axTerminal's screen view with the keys going to the program, F1 still reaches the
  window, Ctrl+Comma goes to the program.
- AX-10.2 **The entries** are the window's menu bar as it stands when the palette opens, the Window, axKit and Help menus
  included: every enabled item that does something when chosen, in menu order. An entry reads its name, then its menu
  path, then its key when it has one, then "checked" for a checked item: "Save as, File, Ctrl+Shift+S", "Larger text,
  Options, Text, Ctrl+Plus", "Word wrap, Edit, Ctrl+Shift+W, checked". The name is the item's text without its
  mnemonic ampersand and its trailing dots. Left out: separators, items that only open a submenu and disabled items.
  The recent lists are filled before the palette reads them, as on opening, so that each is one search away (the
  user's ask of 2026-11-13; left out before): every Open recent submenu, axFile's Favorites, and the Window menu's
  Open windows and Recently closed, an entry reading "1. notes.md (C:\...), Open recent file, Session". The lists that
  are slow to fill are still left out, their keys working: axFile's Drives, the Open with submenus and axFile's
  Remove a program; a window marks such a submenu where it attaches its filling (`CommandPalette.NoEntry`). A
  menu whose items change their text or check mark as
  it opens (axFile's Undo named after what it would undo, axTerminal's Switch shell with the shell in use checked)
  is marked where its refreshing is attached (`CommandPalette.RefreshedOnOpening`), and the palette runs the
  refresh before reading (until 2026-10-23 such an item read as it last stood). The palette reads
  the menus and keeps no list of its own: a command added to a menu is in the palette.
  **With nothing typed** (2026-11-26) the list is first the window's F1 text, each line that holds a word a line of
  the list, its indent and a heading's marks taken off: the rules of the keys (`BundleHelp.RulesSection`) first, then
  the bundle's section and the app's own; then the line "Every command of this window, by menu: Enter runs the one
  you are on" and every entry above, in menu order, in place of the text's generated "Every key of this window";
  then the keys with no menu item as that section gives them (`KeyReference.AloneLines`). A line of the text runs
  nothing (`PaletteEntry.IsCommand`); the entries run their commands (`Palette.WithNothingTyped`, tested).
- AX-10.3 **Matching** is axStart's (AS-2.2), moved to `AxKit.Core` on this second use: the typed text, trimmed, case
  and accents ignored, against the entry's whole line; an entry matches when its line holds every typed word (`text
  larger` finds "Larger text, Options, Text"). The order: a line that starts with the first word, then a line with a
  word that starts with it, then any other match; within each, menu order; only the entries are matched, never the
  lines of the text. An empty field lists the text and every entry (AX-10.2).
- AX-10.4 **The keys in the field** are axStart's (AS-3.2, AS-3.3): typing refills the list and selects the first
  match, and 300 ms after the last change one notification says it and the count, "Save as, File, Ctrl+Shift+S, 1 of
  3", or "No commands match xyz"; Down and Up, Page Down and Page Up, Ctrl+Home and Ctrl+End move the selection and
  say it; at an end, where nothing moves, the edge earcon and then the match again, "Save as, File, Ctrl+Shift+S, 1
  of 1", so that the last thing heard is the position and not the typed text (asked 2026-10-23; the earcon alone had
  left NVDA's reading of the field as the last thing heard), and with nothing matching the earcon and "No commands
  match …"; the field's editing keys stay its own. **Enter** closes the palette, gives the
  focus back to the control that had it, and then runs the selected entry as a click on its menu item would, so that
  the command acts where the user was; a checked setting run this way says its new state, "Word wrap, not checked",
  since no menu shows it. Enter on a line of the text says the line again and the palette stays. Enter with nothing
  matching says "No commands match …" and the palette stays. **Escape**
  closes the palette with the focus back and nothing run.
- AX-10.5 **Commands only.** The palette holds the window's commands and nothing else, in every app (the maintainer's
  decision of 2026-11-23). Until then a window could add entries of its own after the commands, and axFile added the
  folder's entries, "notes.md, file", "docs, folder". Asked whether every navigator should offer its rows there, he
  answered "Remove it from axFile too": a row is found with the navigator's filter (AX-8.17) or by typing its first
  letters (AX-8.6), and the palette stays the one list of what a window can do. What a window offers by name
  through the palette it offers as a command, in a menu: the templates of an editor are such commands. The lines of
  the F1 text with nothing typed (AX-10.2) are the bundle's help, the same in every window, and no window's entries.
- AX-10.6 The palette is the window base's (`AxKitForm`, AX-6.5) with the matching in `AxKit.Core` (unit tested); no app
  writes code for it. Every window's F1 text names F1 and Ctrl+Comma (`BundleHelp.PaletteLine`). axGIT's Push, which
  held Ctrl+Shift+P, moved to Ctrl+Shift+H (docs/axGIT/SPEC.md AG-5.4), and to Ctrl+P on 2026-11-26.

### AX-11 Access layers and consent

The user's ask of 2026-10-29, specified and built the same day (NVDA checks owed): so that no one can be confused about what axKit
is, every app that works through another program or service says so wherever its name appears, and the user agrees,
inside the window and before the first connection, that axKit is only the access layer and that the other program's
terms are theirs to know. `NOTICE.md` ("Who is responsible for what") is the full text this section points to.

- AX-11.1 **Two kinds of app.** A **standalone app** needs only Windows and works with the network off. An **access
  layer** shows what another program does and passes the user's commands to it; that program does the work, under
  its maker's terms (axClaude for Claude Code and through it Anthropic's service, axGIT for Git and the servers of a
  repository's remotes, axTerminal for the user's shells, axPass for KeePassXC, axMCP for MCP servers, axHelp for
  Claude Code through axClaude). Which app is which is written once, in `Dispatch.Apps`: `App.AccessLayerFor`, the
  program's name ("Claude Code", "Git", "your shell", "KeePassXC"), empty for a standalone app; `docs/axKit/apps.md`
  lists the two kinds apart, written from it, and no other list of them is kept by hand. A new app that starts or
  talks to a program axKit does not include is an access layer.
- AX-11.1a **The principles of each kind** (the maintainer's words of 2026-11-22: "axkit standalone apps should be as
  barebones functional as possible and avoid any network requests or other things that might impact performance and
  reliability. the access layers should serve as stable interfaces and have very clear boundaries about how they
  function and when responsibility is handed to the user to perform an action on the underlying program"; and "a
  clear distinction between these apps that will just work out of the box and those that require a bit of
  configuration and installation"). Each new feature is held to its kind's:
  - A standalone app works out of the box: nothing to install or sign in to, no network request of its own (root
    rule 8; the update check and the report sender are the bundle's, never an app's), nothing that costs speed or
    reliability for an ornament; its data is plain files the user can read, copy and keep (AX-5.11).
  - An access layer is a stable interface over a program that changes: what the user hears and presses stays when
    the program changes (root rule 12), everything read from or sent to the program goes through the layer's one
    place for it, and the boundary is said in words: before the program first runs, the consent notice (AX-11.4);
    when the program is missing, the notice that offers its maker's install (AX-2.9); when a step is the program's
    own and not axKit's (a sign-in, a permission the program asks, a command offered to run), the user is told that
    it is the program's and decides, and axKit never answers for the program or for the user.
  - Where they meet: a standalone app never needs an access layer to do its work; an access layer may hand its
    results to a standalone app (axGIT's file to axDown) as the user's own opening would.
- AX-11.2 **The keyword.** An access layer's name is never shown alone: wherever the user meets it, it reads
  "<name>, access layer for <program>" ("axClaude, access layer for Claude Code"), from one place,
  `App.Title` (the name alone for a standalone app; the phrase "access layer" is one constant). That is: the axKit menu's Apps and Access layers,
  the Window menu's app submenus and Show current folder in list, axFile's Open with axKit apps and access layers, the command palette,
  About, Version, the Start menu entry's name and description, File Explorer's Open with axKit submenu, Windows' Open
  with list, the installer's lines, the first line of each access layer's user guide and F1 text, both READMEs and
  `NOTICE.md`. The console commands (`axClaude`) and the verbs stay single words. A window's title is the one
  exception (the maintainer's word of 2026-11-26, AX-9.6): it names the app by its name alone, "myproject, axClaude,
  C:\src\myproject", and so do the lists made of titles, the Window menu's Open windows and Recently closed and
  axStart's recently closed matches; until then the title carried the keyword.
- AX-11.3 **Grouped apart.** Every list of apps shows the standalone apps first, in their groups (AX-9.2), then the
  access layers, with a separator before them (in File Explorer's submenu, the separator flag of its entry), the
  AI access layers and the utilities among them last (`AppGroup.Ai`, `AppGroup.Utilities`; one Claude group from 5.6.0 to 2026-11-23); the axKit menu has them
  as two submenus, Apps and Access layers (AX-9.2), and the docs list them the same way under "Apps" and "Access
  layers" (`apps.md`, written from the code). `Dispatch.Apps` is in that order, and a test
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
    those that come with Windows included. A command line of the user's own (Custom, one of the user's shells) is
    agreed to as that command line (`ConsentTarget.CommandLineShell`, docs/axTerminal/SPEC.md AT-1.6b, since
    2026-11-16): the agreement for one command line is not one for another, and the question names the line.
  - axGIT, Git: on first opening a repository; and each server on its own (github.com, a company's server), before
    the first fetch, pull or push that reaches it, by the host of the remote's address (a share's by the computer it lies
    on), since that is a new service with its own terms.
  - Where the program is found (2026-11-16, `repairs.md` R-70). The agreements for Git and for PowerShell 7 name the
    program, not its path, and neither the question nor axTerminal's "Shell started" line says a path. So
    `git.exe` and `pwsh.exe` are looked for first in their install folders, which only an administrator can write
    (`Git\cmd` and `PowerShell\7` under Program Files and Program Files (x86), asked of Windows, not of the
    environment), and only then in the PATH's folders, of which those that are not on a drive of this computer are
    passed over (`SystemPrograms.Installed`, `LocalPaths.IsLocal`; tested). A program of the same name in a folder
    earlier in the PATH, or on a share, is then not what runs under the agreement while the real one is installed.
    A copy installed for the user alone or from the Store is still found through the PATH.
  - axPass, KeePassXC: before the first master password goes to it (unlocking a database) and before File, New
    database.
  - The AI tools (AX-9, `docs/axKit/mcp.md`): the pairing notice (MC-6.4) carries the check box, which Allow needs;
    Deny stays its default. Forget AI tools withdraws these consents with the pairings.
  - Every window, axKit menu, AI tools, Add axKit to Claude Code (MC-6.8): it runs Claude Code's program (`claude mcp
    add`), so the agreement for Claude Code comes first, under the window's own app (2026-11-02).
  - A program installed from Install it now (AX-2.9) is agreed to in the same step, under the name the app connects to
    it by (axTerminal installs Git for Windows as its shell Git Bash), so that the connection asks nothing more.
  - **Install it now** (AX-2.9): before an installer runs, the same notice for the program it installs, titled
    "Before <app> installs <program>", with **Install** in place of Connect; its acceptance counts for the app's first
    connection, so the user agrees once, in the step they were already taking.
  The notice is the window base's (`AxKitForm.HasConsent`, the one check: true goes on, false has shown the reminder, whose Connect runs the caller again), the programs' words and addresses `ConsentTarget` in `AxKit.Core/Consent.cs`.
  A custom shell has no address for its terms: its notice says so.
  The file is under the bundle's seal (AX-5.6, 2026-11-16): an agreement that something other than axKit wrote into
  it is not the user's, so a file whose seal does not check holds no agreement at all, every app asks again, and the
  question starts with "The record of what you agreed to was changed outside axKit and is not used, so axKit asks
  again." (`Consent.Problem`). The seal covers each agreement's app, program and version, not its date. A file from
  before the seal counts as it is until the next agreement is recorded, which seals it.
- AX-11.6 **Reminders at sign-in.** One line says whose account it is, without a check box: axClaude, on sending
  `/login`: "You are signing in to Anthropic through Claude Code; the account and its terms are between you and
  Anthropic", spoken after "Sent" and kept as a system line. axGIT, the first time a window reaches each server
  after its consent (git's credential helper may then ask for a sign-in in its own window, which axKit cannot see
  coming): "Connecting to github.com; if Git asks you to sign in, the account is between you and github.com". Still
  to come: Claude Code's own login screen at its first start, recognised once it is recorded
  (`docs/axClaude/claude-screens.md`).
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
  made by AX-9.6's rule, what is open, the app's name alone, the path, and read back to their app, a name with a
  comma in it included (`AxKitWindowsTests`); a closed item's title follows the same rule (`RecentlyClosedTests`); `Consent` records, reads, withdraws and takes a damaged file as nothing
  agreed (`ConsentTests`); the servers' hosts (`GitRemoteTests`); every program Install it now offers has its
  consent target with an address (`ConsentTests`).
- AX-11.11 **How an access layer meets the network (2026-11-05).** An access layer starts its program as a child
  process of the window (Claude Code and the shells in a ConPTY pseudo console, git and `keepassxc-cli.exe` as
  processes with their standard streams on pipes) and talks to it over that pseudo console or those pipes, on the
  computer, never over the network. The program's network use is its own, as from a terminal: Claude Code talks to
  Anthropic's service and checks for its own updates; git reaches a remote on fetch, pull and push alone, in a trusted
  repository or one that names no program of its own, never by itself for a missing object, and Git LFS through it
  never (docs/axGIT/SPEC.md AG-5.4; AX-11.5 asks the server's consent first; its credential helper may ask for a
  sign-in in its own window, AX-11.6); a shell's commands reach what they reach; KeePassXC's tool has no network feature. axKit does not see, record or change that
  traffic; it reads what the program prints and passes on what the user types. axKit's own code makes network requests
  in two places (AX-12; three from 2026-11-07 to 2026-11-14, while axTask had GitHub lists, docs/axKit/reports.md
  RP-7), over HTTPS to GitHub: the updater (AX-3), the update check and the download the
  user chooses, which the updater takes only from this repository's release address; and the report sender
  (docs/axKit/reports.md RP-6), which posts a report the user sends, encrypted on the user's computer for the
  maintainer's key (AX-12.3a), as an issue of the private reports repository, only on the user's Enter, and so
  contacts two addresses of GitHub: first the block list's file in the public repository `KyleKeane/AxKit-blocklist`
  (`raw.githubusercontent.com`; a read without a token that sends nothing of the user's but the request, at most
  once a day), then the API that takes the sealed report (`api.github.com`); nothing else in the code makes a network request or
  listens for one, and the remaining addresses in the code are the Help menus' links, opened in the browser, and the
  installers' (AX-2.9: winget from its own source, Anthropic's installer script). `PRIVACY.md`, `README.md`,
  `docs/axKit/README.md` and `SECURITY.md` say this in these terms, and a change here changes them in the same commit;
  `docs/axKit/outside-calls.md` lists every such call in the code, written from the sources (B-17).
  Since 2026-11-15 axClaude can follow a session, while the user has its option Events from Claude Code's hooks turned on (off at first), through one more program start and one more pipe, both on the
  computer and neither a network contact (docs/axClaude/events-and-grounding.md EG-2, EG-10): for eight kinds of
  events of the session Claude Code starts `AxKit.exe hook`, at axClaude's request, registered for that one start
  with `--settings` and axKit's own file and never in a settings file of the user's or a project's; that process
  writes the event to a named pipe of the window's own, for this Windows account alone, with a secret new for each
  start, and ends. A hook returns nothing and so decides nothing Claude Code does. axKit still listens on no port,
  also not on this computer (docs/axKit/mcp.md section 1).
  axClaude also reads the session file Claude Code keeps for the conversation, in memory only (EG-3, EG-10.3).
  Since 2026-11-17 axClaude can also run Claude Code a second way, while the user has the setting `claudeSource` set
  to `stream` (the screen at first; docs/axClaude/stream-mode.md): as one process over pipes in place of the pseudo
  console, with no window of its own, in which Claude Code talks to Anthropic exactly as it does in the pseudo
  console. One more verb goes with it, `AxKit.exe ask`, which that Claude Code starts as its permission tool, named
  for that one start with `--mcp-config` and axKit's own file and never in a settings file of the user's or a
  project's: it passes each question of Claude Code's (a tool call that needs the user's word, a question of
  Claude's own) to the window over a named pipe of the window's own, for this Windows account alone, with a secret
  new for each start, and passes the user's answer back; where the window cannot be asked its answer is no. Again
  a program start and a pipe on the computer, no network contact of axKit's own. The four documents named above
  say it.

- AX-11.12 **What an AI wrote or asks for says so (2026-11-13, the user's decision).** Wherever a window shows text an
  AI wrote for the user to act on, or lets something an AI asked for happen, it says in plain words who wrote it, that
  it can be wrong, that axKit checked nothing of it, and that acting on it is the user's own choice. The words are one
  class, `AxKit.Core/AiCaution`, so that every window uses the same ones. Code and commands get the most care: the
  caution is the first thing in their explanation and part of the question's name, heard every time it is read; it
  says that a command may be offered because the AI's own permission or safety checks refused it, to run it with
  care, to read every detail first and not to run what is not understood; running is never the answer a list starts
  on or a notice's default; and the AI's own words about its command are labelled as the AI's. Where it stands today:
  a command Claude offers (`docs/axKit/mcp.md` MC-9: the question's name and "Read this first" in the explanation);
  an AI tool's run of a window's command (MC-7.3: asked through the same list as an offered command, named "Asked by
  Claude, an AI: read the explanation before you allow it", its explanation starting with "Read this first" and showing the text the command is run with whole, MC-7.9, since 2026-11-27); AI help's answers ("Claude says" ends with who wrote it and that it can be wrong; `docs/axKit/ai-help.md`
  AH-3.1); the pairing of an AI tool (AX-11.4's reminder and check box); a tool call Claude asks leave for in an
  axClaude window fed by Claude Code's stream (`docs/axClaude/stream-mode.md` SM-7.4, SM-7.6, 2026-11-17: the question
  names who asks and for what, "Asked by Claude, an AI: read the explanation before you allow it" is its text, heard
  every time; the list starts on No; and its explanation, everything Claude asks to do, begins with
  `AiCaution.ToolCall`: who asks, that it can be wrong, that axKit checked none of it, to read all of it and not to
  allow what is not understood, and that allowing it is the user's own act); an action of an axTask list that runs a
  program or puts a command into axTerminal (`docs/axTask/SPEC.md` TK-7a.1a, the maintainer's decision of 2026-11-21:
  who wrote an action in a list's file is not known, and it may be an AI, so the question's name ends "An AI may have
  written it: read the explanation before you run it" and its text begins with `AiCaution.Action`; Cancel is where
  Enter is); axHelp's offer to carry out the steps it gave (`docs/axKit/ai-help.md` AH-7.3b, 2026-11-24: Claude says
  `AiCaution.OfferToDo` with the offer, word for word, and runs nothing before the user's yes; each command then
  runs as MC-7 says, heard, an act asked first); a report an AI tool drafted (`docs/axKit/mcp.md` MC-9.11,
  2026-11-24: the form's text starts with `AiCaution.Drafted`, its words with who drafted them, and only the user's
  Send sends); an AI tool's opening of axClaude, axTerminal or axTask on a folder (`docs/axKit/mcp.md` MC-6.5a,
  2026-11-24: the notice "Open axTask?" begins with `AiCaution.OpenAsked`, then what opening does and where; No is
  first, the default and Escape, and only the user's Open opens); Claude's plan for approval in an axClaude window fed
  by Claude Code's stream (`docs/axClaude/stream-mode.md` SM-7.7, 2026-11-26: the question's text begins with
  `AiCaution.PlanAsked`, who wrote the plan and that approving lets Claude carry it out, heard every time, then the
  plan; the list starts on "No, keep planning"; "Read the whole plan" opens it with `AiCaution.Plan` first: who wrote
  it, that it can be wrong, that axKit checked none of it, what manual approval and auto mode let happen, and that
  approving is the user's own act). With the screen, Claude Code draws
  that question itself, and the window shows it as Claude Code wrote it. axClaude's conversation itself is Claude
  Code's own output, shown as it comes, under the access layer's consent (AX-11.4). These are reminders in plain
  words, as the consent notices are (AX-11.10), not legal advice. A new feature of this kind brings its caution in
  the same commit, in these terms, and a line here.
### AX-12 Reports from every window

The design is `docs/axKit/reports.md` (RP-n, 2026-11-07); this section fixes what the bundle promises.

- AX-12.1 **F4 in every window** (`BundleKeys.Report`, AX-8.2) and the Help menu's Send a report… open the notice
  "Report to axKit", a form of standard controls (AX-8.10; `ReportControls`): the user's words first and focused,
  where Enter makes a new line; then the kind (Feedback, Bug, Feature request), a check box for more detail, and one
  text that is only read, what is sent with the report: the app and version, the commands last
  run, the kind of control in focus and its place and the log's errors of the last day masked: by itself
  the window adds nothing that can name the user's files, folders, lists or items (`ReportPrivacy`, reports.md
  RP-4.2a); the check box, not checked, adds the window's title, the names, what the window said (`RecentActions`;
  never in axClaude, axTerminal, axPass and axClip) and the log's last lines, redacted (`Redaction`). The report's
  title is the first line of the words. Send, or Ctrl+Enter in any control, sends; Escape cancels the form and keeps
  nothing (RP-D13). Your reports opens the
  Reports list in axTask.
- AX-12.2 **Saved first, sent second.** Send writes the report as an item of the axTask list `%APPDATA%\AxKit\reports`
  (RP-5), then sends it and every unsent one, oldest first, as an issue of the reports repository `KyleKeane/AxKit-reports`
  over GitHub's REST API (RP-6). Nothing is sent at a window's start; the unsent go with the next report and when Your
  reports opens (RP-6.7). Escape cancels the form and keeps nothing (RP-D13; until 2026-11-14 words typed were kept as a draft, and those drafts stay in the list).
- AX-12.3 **axKit's own reporter account.** The issue is posted by a machine account of the maintainer's whose token
  the release build carries and which can reach that repository's issues alone (RP-6.2); every report carries the
  install's random reporter id and the version (RP-6.3). A block list, read before a send from a public repository
  of its own that holds only random install ids and versions (`KyleKeane/AxKit-blocklist`, 2026-11-14), the reports
  repository's own workflow, and the token's revocation at a release are the levers against abuse (RP-6.4). A
  blocked install or version posts nothing and is told so in a notice: an install with its install id, Copy install
  id and Open the block list page, where to ask if it is a mistake; a version to update axKit. A report
  that cannot go stays saved, and the notice offers it for copying (RP-6.5).
- AX-12.3a **Sealed on the user's computer, a private repository (2026-11-14, the user's decision).** The reporter
  token is in every copy and can read the repository's issues, so nothing of a report leaves the computer in the
  clear: the sender encrypts the report with the maintainer's public key, a constant in the code (`ReportSeal`: a
  fresh AES-256-GCM key per report, wrapped with RSA 3072 OAEP-SHA256), before any request, and a report that cannot
  be encrypted is not sent. The issue's title is the kind and the version; its body is one JSON object, the envelope
  (RP-6.1a): in the clear the format, the reporter id, the version, the kind, a fingerprint of the key and the
  method; sealed, the report. Only the private key, on the maintainer's computer alone (`tools/ReportKey`,
  `reports-setup.md` section 5), opens it. The report saved in Your reports stays as the user wrote it.
- AX-12.4 **Consent once** (AX-11.4's shape): before the first report goes, a notice says that the report is
  encrypted on the user's computer, stored as an issue in a private repository on GitHub that only the maintainer
  can open and only the maintainer's key can read, that GitHub stores the encrypted text and sees when and from
  which network address it came, under GitHub's terms, that nothing in it names the user unless they write it or
  check the detail box, and that the maintainer may quote or summarise a report in the public repository without
  anything that identifies the user; with the check box; recorded in `consent.json` under
  the app name "axKit" and the target "GitHub, encrypted reports", and listed in About, Licence (RP-6.6). An
  agreement given to the words of 5.1.0 to 5.8.0, which said "public issue" (target "GitHub"), does not count: the
  user is asked again. The report sender is the second place axKit's own code
  meets the network, beside the updater (G6, AX-11.11, `PRIVACY.md`).

### AX-13 The Microsoft Store copy

- AX-13.1 **One package.** axKit also ships as an MSIX package through the Microsoft Store (`docs/axKit/store-distribution.md`,
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
  (axClaude, axDown, axFile, axTerminal, axTask, axStart, axClip, axHelp; axHelp in axGIT's place since 2026-11-11,
  the package of 5.2.1 still carrying axGIT; the maintainer's choice, changed there and
  nowhere else; `DispatchTests` keeps the count), all running `AxKit.exe`, named as AX-2.2's Start menu entries
  (`axKit axClaude, access layer for Claude Code`), each with its own images, so that Windows search finds them as
  before. The axClaude entry comes first, has the id `AxKit` and no parameters, so that it starts axClaude as
  `AxKit.exe` does with nothing (AX-1.3), and carries what the whole bundle needs: every app's console command,
  `axKit.exe` and `<name>.exe` for every app (`axDown.exe`, `axCalc.exe`), on the PATH through the user's
  WindowsApps folder in place of AX-2.4's `.cmd` shims, and the file types of the apps without an entry. Windows gives
  a process started through a command the command's own path as the first word of its command line (measured
  2026-11-09: `"…\WindowsApps\axDown.exe"`), so `Dispatch.VerbOfProgramName` reads the app off it, and a file opened
  with a type of the first entry reaches its app by its kind (AX-1.2). Every other entry runs `AxKit.exe` with its verb
  as the entry's parameters (`uap10:Parameters`, which Windows passes; measured 2026-11-08), and an app with Open with
  types (AX-2.3's last column) declares them under its own entry, so that Windows' Open with lists it on them; a type
  is declared once per package, by the first app of the list that has it, and axPreview, whose Open with says every
  file, declares none: a package cannot claim every file, and AX-2.3's submenu (the apps on folders, axDown and
  axPreview on any file) has no form in a package without a COM server, which this copy does not have. Each app with
  `AtSignIn` and an entry (axStart, axClip, axHelp) carries a startup task on its entry with `<verb> --hidden` as the
  task's own parameters, which Windows lists under Settings, Apps, Startup; an app with `AtSignIn` and no entry (axHelp
  from 2026-11-09 until it took axGIT's entry on 2026-11-11, the user's choice; axDescribe until 5.7.0) has no startup
  task in the package: the Store's copy starts it from another window (its Start with Windows item says so,
  `HotkeyForm`), the GitHub install from the Run key as before, and `DispatchTests` fails on one, so that such an app
  is added on purpose. The apps without an entry (axJSON, axGIT, axMedia, axPreview, axProcess, axPass, axCalc, axMCP,
  axAutomate) are reached by their command, by a file of their
  kind, by the axKit menu's Apps and Access layers in any window, by their keys, and from axFile; axStart lists the Start menu's programs and so
  knows the eight.
  `AxKit.Core/PackageIdentity` tells whether the process runs from the package (`GetCurrentPackageFullName`) and which
  entry started it (`GetCurrentApplicationUserModelId`); `Dispatch.WithApplicationVerb` puts the verb first when the
  command line carries none, the command's name before the entry's, nothing for the first entry, so that the dispatch
  holds whether or not Windows passes the parameters (AX-1.8; tested in `DispatchTests`).
- AX-13.3 **No updater.** The Store installs, updates and uninstalls the package. The Store's copy makes no update check
  at start, has no Check for updates at start setting (Options), downloads nothing and never shows Update to.
  Check for updates (Help menu) still asks GitHub for the latest release, so that About, Version and the notice can say
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
- AX-13.4a **The background apps right after the install** (the user's report of 2026-11-14 from a second computer:
  axStart's hotkey did nothing until Windows was restarted). Windows runs a package's startup tasks at a sign-in, and
  only after an app of the package was started once. So the first start of any app of the Store's copy starts axStart,
  axClip and axHelp itself, hidden, once per install (`StoreFirstStart`, `Program.StartBackgroundApps`): a marker
  file in the package's private folder says it was done and goes with an uninstall. Afterwards Windows' Startup apps
  page alone decides, so an app turned off there is never started by axKit. Never in a test run. Nothing is said:
  the apps wait behind their hotkeys as they do after a sign-in. When the app the user started shows a window, they
  are started once that window is in front (at most five seconds later), so that each gives the foreground back to it
  as its hidden window is made (AS-3.4); started earlier, they gave it to the window in front before, and axStart's
  first start said nothing (`KyleKeane/AxKit-reports issue 15`). Should that app end within those seconds, they wait
  for the next sign-in.
- AX-13.4b **A background app without a startup task** (2026-11-23; the maintainer's word that axTemplate's hotkey
  is on for everyone after an installation and after an update, docs/axTemplate/SPEC.md TP-1.4). axTemplate's
  launcher runs in the background as axClip does, and has no entry in the package (the eight are taken, AX-13.2),
  so Windows has no startup task for it and no line on its Startup apps page (`Dispatch.App.InBackground`). axKit
  starts it itself in the Store's copy, hidden, at three moments: with the first start after the install
  (AX-13.4a); with the first start of any app after an update, which the marker tells by holding the version it
  was written for (a marker of before this rule is empty, so the update that brings the launcher starts it); and
  whenever a startup task starts its app hidden, which is the sign-in. After the first start, its own setting
  says no: Options, General, Start with Windows in its window is the toggle of that setting there, not Windows'
  page (`SignInStart.TurnedOff`, read from its settings file as the process starts). A start of it while it runs
  ends by itself, as every second start of a background app. In the copy from GitHub the installer starts it with
  the others, and its first run writes its Run value.
- AX-13.4c **The background apps after an update** (`KyleKeane/AxKit-reports issue 13`, 2026-11-18: after the Store
  installed 5.10.0, axStart's hotkey did nothing until axDown was opened). The Store ends the package's running
  processes to update it, and its startup tasks run only at a sign-in. So each background app of the hotkey window
  (axStart, axClip, axHelp) registers with Windows, as its window's handle is made, to be started again after an
  update only (`RegisterApplicationRestart` with its verb and `--hidden`, not after a crash, a hang or a restart of
  Windows; `HotkeyForm.RestartAfterUpdate`), in the Store's copy alone and never in a test run. Windows starts again
  only what was running; an app the user had quit or turned off stays so. Should Windows not start it, the first
  app opened after the update does no more than before (AX-13.4b).
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
  they do without axKit. A child that runs a program file of the package keeps the identity (Apps and Access layers, the AI tools'
  window). `PackageIdentity.ProgramPath` is the path recorded for later use in the Store's copy, the alias
  `%LOCALAPPDATA%\Microsoft\WindowsApps\axKit.exe`, since the package folder changes with every update (the AI tools'
  `claude mcp add` command line, AiToolsSetup.cs). The capability that turns virtualization off,
  `unvirtualizedResources`, is restricted to games and not used.
  Not yet confirmed by use in the Store's copy (2026-11-15): the two files that name this program for Claude Code,
  `claude-mcp.json` (the AI tools of every axClaude window, docs/axKit/mcp.md MC-6.9) and `claude-hooks.json`
  (axClaude's hooks, AX-5.1), are new files under AppData and so private to the package, while Claude Code runs
  outside it and may not find them at the path it is given. Until that is settled, the Store's copy passes no hooks
  to Claude Code (docs/axClaude/events-and-grounding.md EG-2.11), and axClaude works there from the screen, the
  marks and the session file. For the same reason the Store's copy does not start Claude Code over its stream
  (docs/axClaude/stream-mode.md, 2026-11-17): the file that names `AxKit.exe ask` is a third such file,
  `claude-ask.json`, and without the relay nothing could be asked. A window made for the stream says so there and
  offers the same folder with the screen.
- AX-13.6 **The listing and the release.** The listing (name axKit, category Developer tools, free, every market,
  discoverable; its texts in `docs/axKit/store-listing.md`) begins with the programs the access layers need (Store
  policy 10.2.4), discloses that axClaude shows the output of a live generative AI (11.16; the F4 report is the means
  to report content, AX-12) and links `PRIVACY.md` (10.5.1). The release workflow builds the package beside the zip and
  attaches it to the draft release (`ReleaseWorkflowTests` keeps it). The first publication goes through Partner
  Center's website by hand, as Microsoft requires (`docs/axKit/store-setup.md` section 3), and so does every later
  one: once a GitHub release is published, that release's attested `AxKit-<version>.msix`, never a rebuild, is
  uploaded in Partner Center as a new submission (a copy of the last published one, the old package removed and the
  new one added, What's new from the release notes), by the maintainer or through `tools/EdgeDrive`, which drives his
  signed-in browser on the development computer and holds no credential (`store-setup.md` sections 5 and 6). No
  workflow and no script of the repository submits to the Store: a route through Microsoft's submission API was
  built on 2026-11-09, never switched on, and removed on 2026-11-13 at the maintainer's decision, since Partner
  Center opens that API to no Individual account (`store-setup.md` section 2). Certification runs at Microsoft's
  afterwards and the Store updates every installed copy by itself. `winget install 9PK01L2K5000 -s msstore`
  installs the Store's copy. The zip of AX-2 and AX-3 stays, for developers and for a
  computer without the Store.

### AX-7 Accessibility rules every app follows

These are the rules of `docs/axClaude/SPEC.md` that are not about Claude; app specifications refer to them by number.

- AX-7.1 Every control has an `AccessibleName`; every action works from the keyboard; menu items have mnemonics and
  shortcuts. Native Win32 controls (EDIT, menus, status bar, `ListBox`) are used because NVDA reads them reliably;
  no WPF, no web view, no owner-drawn controls.
- AX-7.1a **The title names what is open, then the app, then the path** (the maintainer's word of 2026-11-26): every
  window's title is "<what is open>, <app>, <full path>", "myproject, axClaude, C:\src\myproject", "notes.md, axDown,
  C:\work\notes.md", and the app's name alone with nothing open; NVDA reads it as the focus lands on the window, the
  place first. One rule in the window base for every app (`AxKitForm.SetTitle`); the details are AX-9.6.
- AX-7.2 No second window of the app's own: questions, errors and help texts are notices drawn inside the window
  (`OverlayPanel`), never a `Form` or a `MessageBox` once a window exists. A notice's button may open the next notice
  (the update's progress, an error, axDown's changed-on-disk question after Save), which then stays in view. The
  Windows folder, file and font pickers, and the shell's own dialogs and menu an app hands an entry to (axFile's Open
  with, Properties and Windows menu, docs/axFile/SPEC.md AF-4.7), are the only separate windows. The pickers are
  shown through the window base's `ShowSystemDialog`, which marks the window while one is up (`DialogShowing`),
  since a dialog's closing activates the window before the command that opened it has gone on (2026-10-23: the
  editors said "Reloaded" in the middle of Save as, and axFile read its folder again), and which puts the focus back
  on the control that had it when the dialog closes, OK or cancel alike, one rule for every window (2026-11-09; before,
  each caller did its own, the editor's Open only after OK and its Save as only after cancel, some windows never). A
  dialog opened from a notice's button leaves the focus to the notice (axClaude's Locate claude.exe, the New session
  notice's Choose folder).
  A notice's text takes the heading letters in every window (`TextJump.HeadingLetters`, the default of
  `OverlayPanel.Jumps`; the letters of axClaude's transcript): h and Shift+H the next and the previous heading, 1 to 6
  and Shift with them a heading of that level, by the text's Markdown headings; an editor's notices take the editor's
  own Ctrl+letter pairs as well (every kind of axDown's section 9, JSON's kinds in axJSON), so that its convention
  holds in its notices too. The jump is `TextJump`, the code the editors use on their own text; the result is
  announced on the notice, "No next heading" and the like when there is none, and the caret stays. A notice's text
  is searched with the bundle's find keys (AX-8.2): Ctrl+F puts the Find notice in the notice's place, and Find next
  brings the notice back with the match selected and the focus in its text (Cancel brings it back as it was); F3 and
  Shift+F3 find the next and the previous match there, "Not found" when there is none. The F1 texts are written under
  Markdown headings, one per group of keys, starting with the rules of the keys ("How the keys go", AX-8.18,
  2026-11-26) and then a line naming the text's own keys, so that the letters and the
  find walk them (asked 2026-10-23), and every text ends with the bundle's Notices section, where the app's notices
  appear and a notice's keys, the same words in every window (`BundleHelp.NoticesSection`, 2026-11-09; five texts had a
  shorter section or none).
- AX-7.2a Every key of the window is in its F1 text by construction (the maintainer, 2026-11-23: one source of truth
  for the keys, nothing kept by hand): after the app's own text comes "## Every key of this window", made as the text
  opens (Help, Keyboard shortcuts; F1 until 2026-11-26, which is the command palette now, whose list with nothing
  typed is the same text's lines with the commands in place of this section, AX-10.2) from the window's command list as it stands (`AxKit.Core/KeyReference`, from `AxKitForm.CommandRows`): a heading per
  top menu ("### File menu") with a line per command that has a key ("Ctrl+O: Open", a submenu after the label:
  "Ctrl+H: Heading (Next)"), then "### Keys with no menu item". Ask about this window hands Claude the same whole
  text. The app's own part says only what the generated list cannot (the maintainer, 2026-11-16, "Generate the
  whole text", user-workflows.md WF-D-25; trimmed in every app on 2026-11-25): what a key does in context (what is
  said, where the focus goes, what a refusal means), the order of a task, the words to expect, the keys no command
  carries (a list's letters, a hotkey, the edit control's own keys) and an expression's syntax, in plain sentences
  under its section headings; a line that only named a key and its command's label is gone, since the list gives
  it. No check holds that part to naming every key (`check commands` did until 2026-11-23, `KeyTests` until
  2026-11-25); its key names come from the key tables (`HandKeyTests`). Under F1's line in the bundle's section, one
  sentence (`BundleHelp.HowToLine`) points to the user guide in the Help menu and, with AI help on, to Ask about
  this window, which answers from the app's how-to page (`workflows-<app>.md`, ai-help.md AH-7.1).
- AX-7.3 The caret never moves except as the direct result of the user's own command. The result of a navigation
  command is announced through a UI Automation notification, joined into one notification when several things arrive
  at once. Nothing speaks on a plain arrow key; information is given on demand. Two notifications raised back to back
  are not both spoken (NVDA drops the second), so what a window has to say at one moment goes as one: the pieces
  joined by a full stop and a space, each piece that another follows without its own trailing full stop, the last
  piece exactly as it was given, its own full stop included (`AxKit.Core/SaidTogether`; until 2026-11-16 the last
  piece lost its full stop as well, so a sentence ended differently joined than said alone). What a window
  announces before it has been activated for the first time waits for the activation and is said then, in order
  (`AxKitForm.Announce`, 2026-11-01): NVDA ignores a window that is not in front, and a window started by a program in
  the background comes up behind until it is activated, so what it said as it opened was lost. It is said once the
  window has been in front for a second and its focus has held still for 0.7 seconds, three seconds at most (lengthened on 2026-11-25 from half a second, 0.4 and two, which NVDA's reading of the window outlasted in that day's pass)
  (2026-11-25): said while NVDA had yet to read where the focus landed, the reading cut it off (axMedia's "Cannot
  play" as its window opened).
  A paste into an editable text, by Ctrl+V, Shift+Insert or an Edit menu's Paste, is said once the control has done it
  (2026-10-25, the user's ask: the edit control's own paste is silent): "Pasted", "Pasted, 3 lines" for several into a
  text of several lines, "Nothing to paste as text" when the clipboard holds none; never the text itself, which may be a
  password (`AxKitForm.SayPasteAfter`). A read-only text pastes nothing and says nothing.
- AX-7.3c One way to refuse, to fail and to say a switch (the maintainer's choice of 2026-11-25, "same everywhere"; repairs
  R-95): a command that cannot act now (nothing selected, nothing to undo, nothing to paste, not in this list) plays
  the refused sound and says why, interrupting, through the window base's `Refuse`, inside a notice too (a required
  field left empty, the consent box not checked, a text the notice's own check refuses, since 2026-11-27; `EventDoorTests`
  fails on words of a refusal without the sound); an error notice is of the failure
  kind in every window (`ShowError`, `Fail`); a switch toggled says its menu item's name with "checked" or "not
  checked" (`MenuText.CheckSaid`), Word wrap, Hide done tasks and the theme's switches included, which said "on" and
  "off" or words of their own until then. An answer that is empty ("No variables", "No changes") is no refusal.
- AX-7.4 Enter and Escape reach a notice's buttons from every control (`AcceptButton`, `CancelButton`); handled keys
  set `SuppressKeyPress`; NVDA's own key combinations are never intercepted and no global keyboard hook is installed.
- AX-7.5 The UI thread never blocks on I/O; background work is marshalled to the UI thread and batched.
- AX-7.6 Any change to focus handling, key handling, announcements or an edit control is checked with NVDA against
  the app's test plan before a release.
- AX-7.7 Colours are the system colours and follow the Windows app theme: the entry point calls
  `Application.SetColorMode(SystemColorMode.System)` before any window exists, so every window is light or dark as
  Windows is when the app starts, and a high contrast theme applies as it always has. No app sets a colour of its own,
  with one exception: axMedia's video area is black (AM-2.4), the colour of a video surface everywhere, so that a
  picture's letterbox is not the window's colour (decided 2026-10-23). Since 2026-11-09 a theme file of the user's,
  `%APPDATA%\AxKit\theme.json`, may give each class of object a colour of its own, applied by the window base alone
  (`docs/axKit/theme.md`); without the file, or with `system` everywhere in it, nothing in this rule changes.
- AX-7.8 A line break typed is said (2026-11-11; the reports repository's issue 1, the user: spoken everywhere, B-18).
  NVDA says nothing for Enter, so in every field where a key puts a line break into the text, the editors of axDown
  and axJSON, an item app's long text, axClaude's message and axTerminal's command with Shift+Enter, the report's
  words, the New session notice's arguments, any field of several lines, the window says "new line" through its one
  announcement (`AxKitForm.Announce`, not interrupting, so that NVDA's echo of the word just typed comes first). It is
  said only when a line break went in: the field (`AxKitTextBox`) keeps its text and selection as Enter goes down and
  compares them once the key is handled (`AxKit.Core.TypedLineBreak`), so Enter that sends a message, opens, presses a
  notice's button or meets a read-only text says nothing of it, and a line break with an editor's indent after it
  counts. One setting for every window, on by default: Options, Text, Say new line, in every window's menu, palette
  and F1 text (`BundleHelp.Section`), kept in the theme file's display settings as `display.sayNewLine`
  (`docs/axKit/theme.md` TH-6.5), so that a change in one window holds in all; it says "Say new line, checked" or "Say new
  line off".
- AX-7.9 A heading reached is heard before it is read (2026-11-11, the user: "an earcon as an option for each
  navigation event that moves into a heading across all apps so that down arrow gives a sound before speaking a heading
  title"). When the user's own key takes the selection onto a heading row of a list (a section of the item apps'
  navigator, `ItemRow.IsSection`: axTask, axPass, axMCP, axAutomate) or the caret onto a Markdown heading's line in a
  text (axDown's and axPreview's text, every notice's text), the window plays the heading sound (`Sounds.Heading`, a
  low note leaping an octave, 85 ms, over before NVDA starts reading) as the key is handled, so that NVDA's reading
  follows it: the arrows, the page keys, Home and End, type-to-find, the section and heading jumps alike. Only the
  user's key counts (`AxKitList.UserMovedSelection`, `AxKitTextBox.UserMovedLine`, each measured around the key's own
  handling): a fill, a refill or a move the window makes never sounds (AX-7.3), nor a key that changes the text or
  selects; a heading wrapped over two rows sounds once. axJSON's text has no headings; the consoles' transcripts are
  left out (their headings are line kinds the text does not mark unless Markers in the text is on). One setting for
  every window, on by default: Options, Text, Sound on headings, in every window's menu, palette and F1 text, kept in
  the theme file's display settings as `display.headingSound` (`docs/axKit/theme.md` TH-6.6); it says "Sound on
  headings on" or "Sound on headings, not checked".
- AX-7.10 **Say the status: F9 in every window** (2026-11-15; the key is the user's choice; F8 until 2026-11-26, AX-8.18 rule 5). Every window's status
  bar is two labels of a native status strip (`StatusLayout.Pair`), which NVDA reads with NVDA+End. NVDA finds a
  status bar by a lookup on the screen, at the bottom-left of the foreground window (the reason for
  `WindowAccessibleObject`), and that lookup can miss: the user heard "blank" in axClaude while the status bar held
  its full text, exposed to UI Automation, at 150 percent scaling in a maximized window, and a restart of NVDA did not
  help; the cause was not found. So the status has a command of axKit's own that does not depend on it: **F9**
  (`BundleKeys.SayStatus`), and Window, Say the status, say the status bar's texts as one interrupting announcement
  of the window (`AxKitForm.SayStatus`, AX-7.3): the labels' texts in order, an empty one left out, each without its
  own final full stop and parted by one, so that they are heard as separate parts ("Claude: ready | 1 shell in the
  background | auto mode on. C:\work"); **"No status"** for a window with no status bar or no text in it
  (`AxKitForm.StatusWords`, tested). The texts are read in one place, `AxKitForm.StatusTexts`, off the window's
  status strip, so no window does anything for it; the automation channel's and the AI tools' `state` read the same.
  The caret and the focus stay. It works from a text field and from a list, and **while a notice shows** too, said
  on the notice: the status is still true behind it, the command only informs, and so it stands with the text size
  keys, the bundle keys that work in a notice, where F1, F4, the palette and Shift+F1, which each open a notice, wait
  (AX-7.2). An open menu keeps the key. A window put away says nothing (axClaude FR-9.6). In every F1 text
  (`BundleHelp.StatusLine`), the command palette and the menu, marked read for the AI tools.
- AX-7.11 **What the focus enters says its kind: a sound, one word for a form, and the explanation on demand**
  (2026-11-22, the maintainer: "are we able to announce the type of interface a user is in? so that when they are
  in a form it announces so they can know to switch keyboard interaction styles ... i would like to minimize those
  messages so that it is just the essential info when the cursor moves"; "make a form opening earcon so that there
  is a cue that the mode has switched"; "an earcon for the other interaction modes starting ... indicative of an
  interaction change and have some character about the type of interaction"; and on how it is raised, "we want
  that process to be as automatic as possible so it is not another coding paradigm that might be forgotten":
  "Every focus entry, automatic").
  - **What NVDA says by itself and what axKit adds.** NVDA names the kind of each control as the focus lands on
    it: "list", "edit", "check box", "button". That is NVDA's and stays. A form of axKit is a part of the window
    and no dialog (AX-7.2), so NVDA never says that a form opened; that is axKit's to say.
  - **Four kinds of entry**, by the way of working that starts, each a kind of event of its own (`EventKind`,
    `docs/axKit/families.json`): the focus entered a **form** (several things asked for one action, Tab between
    them), a **list**, a **field** to type in, or a **text** to read.
  - **Raised by the component itself, at every entry of the focus from outside it** (framework FW-3.8, built
    2026-11-22; the same day's first form raised it in the window base when a notice opened): the list
    (`AxKitList.OnEnter`), the text box (`AxKitTextBox.OnEnter`: a text to read when it is read-only, the
    conversation's text among them, a field otherwise), the form (`FormControls.OnEnter`, once for all its
    controls, which are silent as the focus moves between them) and the answers in the field's place
    (`ControlArea`'s list). So it sounds when something opens, when Tab or F6 moves to another pane, when the
    focus comes back from a notice, and when a window starts; a window of a hotkey (axStart, axClip, axHelp,
    axTemplate's launcher) clears its active control as it hides, so that every showing enters its field afresh and
    sounds, not only the first (the maintainer's choice, 2026-11-24); moving inside a component is silent. No window
    raises an entry, and a component cannot be used without it. The rename box in a list is a field and sounds as
    one.
  - **One sound for one command** (`AxKitForm.SoundOnEntry`): the entry is played once the window's thread has
    finished what it was doing. A focus that passes through one component on its way to another sounds only
    where it ends. When the same key press or the same arrival already played another sound (a refusal, nothing
    found, a notice that opened by itself with its notice sound, a question a program asks, an AI's request),
    that sound stands alone and the entry is silent. "The same" is exact and no matter of time: the door counts
    the turns of the window's thread, one for each key, tick or posted call it takes up, and an entry is silent
    only when a sound of a moment played in the very turn that asked for it (`EventDoor.Turn`, `SoundedIn`; the
    ticks of arriving lines do not count). A first form of this, "another sound within 150 ms", silenced the
    entry of a second key pressed right after a first, and was replaced the same day.
  - **The sound** plays before NVDA reads where the focus arrived (through the door,
    rule 13). The four are one family, **Openings**, in one voice used for nothing else, small bell pings
    (`AxKit.Core/Audio/OpeningSounds`, `WaveTone.Pings`): every one begins with the same mark, three quick beeps
    running up a chord, which says that the way of working changed; then it goes down as its kind. An extended
    tone stands for text, discrete beeps stand for items, and a form has both: a field is the mark and one
    extended tone, the simplest; a text the same an octave lower; a list the mark and four quick beeps going down
    by steps; a form the mark, then beep, beep, tone, and the same lower, the most complex, as a form is. Chosen
    by the maintainer's ear on 2026-11-22 from sets played to him (the knock scale of `docs/axKit/earcons.md`
    class 4 was one of them and was not taken for this). The form's sound is 0.7 s, the one sound of a moment
    beside the call from another window that is longer than half a second, as he chose it; the others are under
    half a second.
  - **The switch**: Options, Text, **Entry sounds**, on at first, the same in every window
    (`display.openingSounds` of the theme file, `docs/axKit/theme.md` TH-6.7); "Entry sounds, checked" or "not checked".
  - **The words of a form.** The focus starts in the form's first control and never on its text. After NVDA has
    read that control, the window says the title and the one word: "Move 2 items, form". The explanation is not
    said at opening (until 2026-11-22 it was, whole, and partly a second time as the focus passed over the
    text). It stays the notice's text at the top (AX-8.10 rule 10): **F7** reads it, and Shift+Tab from the
    first control goes to it. Two forms say their text after the word, because it is what will happen and not
    how a form is used (`ShowForm`'s `sayText`): the report, which says what is sent and to whom
    (`docs/axKit/reports.md`), and the form of an action of an axTask list, which says what the action written
    in the list's file will do (rule 11, AX-11.12). A list, a field and a text get no word from axKit: NVDA's
    own "list" and "edit" say it, and the sound came first.
  - **F7 in a notice** (Ctrl+Q until 2026-11-26, AX-8.18 rule 5) reads the notice's whole text again, any notice's, from any of its controls
    (`OverlayPanel.ReadText`, taken by the window base while a notice shows, before the window's commands): the
    same key that reads what you are on in the windows that have such a key. In every F1 text
    (`BundleHelp.NoticeLine`).
  - **Not done, open to his word**: no sound for leaving.
  - Tested: `OpeningSoundsTests` (the sounds), `ThemeTests` (the switch), `EventKindTests` and `EventDoorTests`
    (the kinds, the table and the door), `ComponentsTests` (only the components name the four kinds);
    `tests/scripts/axTask/21-what-opens.txt` hears each of the four and the switch, `18-item-form.txt` and
    `20-selection.txt` the form's sound, its words and F7.
- AX-7.12 **Your own sounds** (asked 2026-11-15, "I want the folder"; built at the maintainer's word of 2026-11-24):
  a WAV file in `%APPDATA%\AxKit\sounds` named as one of axKit's sounds (`edge.wav`, `notice.wav`, `working-music.wav`;
  the 23 names are `AxKit.Core/Audio/CustomSounds.Names`) plays in place of the built-in one, from the next start of a
  window, since every sound is made once as the first one plays; taking the file away brings the built-in one back.
  Options, Open the sounds folder makes the folder when it is missing, writes `sound names.txt` there with every
  name and what it is, and shows the folder in axFile. A file of any common shape is taken (PCM of 8, 16, 24 or 32
  bits, or 32-bit float, mono or stereo, any rate) and made into the one shape the sound devices play, 16-bit mono at
  the bundle's rate; a file that is not one, or longer than 60 seconds, or over 64 MB (refused by its length
  before any of it is read, 2026-11-27: a minute of the largest shape taken is 46 MB), is passed over, the built-in
  sound plays, and the log says why. Which kind of event plays which sound is unchanged (rule 13, `EventPresenter`); only the sound's
  samples are the user's. The background chords, made by count, have no file. Tested in `CustomSoundsTests`.

## 5. Architecture

### 5.1 Projects and references

```
src/AxKit/            executable (net10.0-windows): Program (crash handling, version, log start, dispatch), icons
src/AxKit.Core/       class library (net10.0), no WinForms, unit tested
src/AxKit.Forms/      class library (net10.0-windows), WinForms pieces the apps use, among them the text editor shell
                     (TextEditorForm, EditorApp, EditorKeys, EditorHost)
src/AxClaude/        class library (net10.0-windows): AxClaudeApp, MainForm, ... (axClaude's window)
src/AxClaude.Core/   class library (net10.0): Transcript (SessionModel and the readers), Pty/ClaudeLauncher, Privacy/GitExclude, AppSettings, StartupOptions (axClaude's pure code)
src/AxDown/          class library (net10.0-windows): AxDownApp (its EditorApp), AxDownKeys, HelpText; the window is
                     the shell's, and its pure code (TextDocument, EditorSettings, MarkdownOutline) is AxKit.Core's
src/AxJSON/          class library (net10.0-windows): AxJSONApp (its EditorApp), AxJSONKeys, HelpText (axJSON's app side)
src/AxJSON.Core/     class library (net10.0): JsonScanner, JsonParser, JsonOutline, JsonFormat (axJSON's pure code)
src/AxGit/           class library (net10.0-windows): AxGitApp, GitForm, AxGitKeys, HelpText (axGIT's window)
src/AxGit.Core/      class library (net10.0): GitRunner, GitStatus, GitLog, GitBranches, GitRemote, GitTrust, GitSettings (axGIT's pure code)
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
tests/AxKit.Tests, tests/AxClaude.Tests, tests/AxJSON.Tests, tests/AxGit.Tests, tests/AxTerminal.Tests, tests/AxFile.Tests, tests/AxMedia.Tests, tests/AxPreview.Tests, tests/AxStart.Tests, tests/AxProcess.Tests, tests/AxTask.Tests, tests/AxPass.Tests, tests/AxCalc.Tests, tests/AxClip.Tests, tests/AxMCP.Tests, tests/AxHelp.Tests, tests/AxAutomate.Tests
```

References point one way: `AxKit` references every app; an app's window project references its own `.Core`,
`AxKit.Core` and `AxKit.Forms`; an app's `.Core` references `AxKit.Core` and never `AxKit.Forms`; the shared projects
reference nothing of the apps. `tests/AxKit.Tests/LayoutTests` reads the project files and fails when this is broken,
and `InventoryTests` fails when an app lacks one of its places (5.3 M1, `adding-an-app.md`).

The .NET version is one property, `AxKitDotNet` in `Directory.Build.props`, from which every project's
`TargetFramework` is built (`$(AxKitDotNet)` for the pure projects, `$(AxKitDotNet)-windows` for WinForms), and the SDK
band is `global.json`, which the workflows read too (2026-10-23). .NET 10 is an LTS release supported until
November 2028; the move to the next LTS is those two files and a run of the tests (`docs/axKit/longevity.md`).

### 5.2 Documents

```
CLAUDE.md               the map of the bundle and its rules (short); each project has its own CLAUDE.md with its notes
README.md, CONTRIBUTING.md, SECURITY.md, PRIVACY.md, GOVERNANCE.md, CODE_OF_CONDUCT.md, THIRD-PARTY-NOTICES.md   the repository's own pages (AX-4.5)
docs/axKit/              SPEC.md (this), README.md (the zip's guide: install, the apps, uninstall), adding-an-app.md, plan.md,
                        nvda-test-record.md (what every NVDA pass found; when
                        each goal last passed is in workflow-record.json)
                        and longevity.md (the practices for the years ahead); a finished review, inventory or
                        report is deleted and stays in the git history
docs/<app>/             SPEC.md, user-guide.md (embedded by the app's project), nvda-test-plan.md, for each app of section 3;
                        docs/axClaude also claude-screens.md, docs/axDown also tutorial.md
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
  `docs/axClaude/SPEC.md`.
- M6 One version, one changelog with app prefixes, one release.
- M7 An app's texts belong to the app: its user guide is embedded by its own project, its F1 text lives in its
  `HelpText`, its usage text in its `StartupOptions`.
- M8 A new app follows `docs/axKit/adding-an-app.md`.
- M9 Keys live in tables (AX-8): one bundle-wide table in `AxKit.Forms`, one table per window; a key is changed in its
  table and nowhere else.

### 5.4 The Windows APIs depended on

Every call into Windows outside WinForms is behind one file, listed here with what it is for and what to do when
Windows retires it (`docs/axKit/longevity.md` 3.6; the list is checked against Windows' deprecation notices once a
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
- **The window calls more than one file makes**, `user32` (`SendMessage`, `PostMessage`, `SetForegroundWindow`,
  `GetForegroundWindow`, `IsWindowVisible`, `GetWindowThreadProcessId`, `GetFocus` in `src/AxKit.Forms/User32.cs`,
  declared once since 2026-10-24 and used through `using static`): the bullets below name the file whose call it is.
  Stable.
- **The EDIT control's messages**, `user32` (`GetCaretPos` in `src/AxKit.Forms/EditKeys.cs`, `SendMessage` from
  `User32.cs`, `TranscriptView.cs`'s own `SendLineMessage`): line and caret positions and the scrolling of the views. As
  old as Windows; WinForms' TextBox is the same control.
- **`FlashWindowEx`**, `user32` (`src/AxKit.Forms/ConsoleForm.Signals.cs`): the taskbar flash when Claude or the
  shell wants attention while the window is in the background. Stable; without it the sounds remain.
- **The window list**, `user32` (`EnumWindows`, `GetWindow`, `GetWindowText`, `GetWindowTextLength`, `IsIconic`,
  `ShowWindow` in `src/AxKit.Forms/WindowMenu.cs`; `GetWindowThreadProcessId`, `IsWindowVisible`, `SetForegroundWindow`
  and `PostMessage` from `User32.cs`): the Window menu's list of the bundle's windows and the switch to one. Stable.
- **`WM_COPYDATA`**, `user32` (`SendMessageTimeoutW`, `AllowSetForegroundWindow` in `src/AxKit.Forms/AppMessage.cs`;
  `GetWindowThreadProcessId` from `User32.cs`): the hand-over from a second start to the open window of axMedia or
  axPreview (AX-7.2). Stable.
- **`RegisterHotKey`**, `user32` (with `UnregisterHotKey`, `FindWindowExW`, `IsWindow`, `GetKeyState` in
  `src/AxKit.Forms/HotkeyForm.cs`; `GetForegroundWindow`, `SetForegroundWindow`, `IsWindowVisible`,
  `GetWindowThreadProcessId` from `User32.cs`): axStart's and axClip's system-wide key and the foreground given back
  (AS-4.1). Stable.
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
  `GetMenuItemCount`, `GetMenuItemID`, `DestroyMenu` in `src/AxFile/Shell.cs`, `PostMessage` from `User32.cs`): an entry's Windows
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
- **B-8 `axkit` without an argument starts axClaude on the current folder.** That is what `axClaude` does today, and a
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
  The Window menu (AX-9) gives the one thing tabs give, a list of what is open in one place, by recency and by app;
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
  (`docs/axKit/plan.md`).
- **B-16 The bundle is axKit (2026-11-05, 5.0.0).** Axit, its name from 2.0.0 to 4.0.0, is a commercial product's name
  elsewhere, so the bundle took a new one before applying for code signing (the code signing record of 2026-11, in the git history): ax, the
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
  `docs/axKit/outside-calls.md`, written by `tools/outside-calls.ps1` and never by hand: by what is reached (GitHub,
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
- **B-18 "New line" is said by the field, in every window, behind one setting (2026-11-11).** The user's report 1 asked
  to hear the line break as it is typed in axDown, and the user decided on 2026-11-11 that it is spoken everywhere a
  key puts one into a text. Decisions taken on the user's word to use best judgement while away: it lives in
  `AxKitTextBox`, the edit control under every field since 5.5.0, so that every field has it by being made and no
  window repeats it; it is said by comparing the text before and after the key rather than by the key alone, since the
  same Enter sends, opens or presses a button elsewhere; it goes through the window's one announcement and does not
  interrupt; the words are "new line", lower case, NVDA's own word for a line break; the setting is on by default and
  is one for the bundle, in the theme file's display settings beside the editors' word wrap (TH-6.4: a setting the user
  wants the same in every window is a key there, never a setting per app), with its item in every window's Options,
  Text, since it is about the text. Pasted text with line breaks is said by the paste's own sentence ("Pasted, 3
  lines", AX-7.3), not as new lines.
- **B-19 One bar of eight menus in every window, and Context follows the focus (2026-11-11).** The reminder of
  2026-11-10 asked whether the apps whose menus followed the focus (axTask's List, Item and Field; axPass's, axMCP's;
  axProcess's views) should take the names the other apps took in 5.7.0. The user chose, in three steps the same
  evening: one menu whose items follow the focus; then that menu beside an Actions menu whose items never change
  ("Operations" until then) in every app with a selection, "it will be needed eventually" in the editors and the
  consoles too; then the whole bar fixed, "Session, Actions, Navigation, Context, Options, Window, axKit, Help", each
  with its letter, every other menu folded into these by what its items do (AX-9.1). Why: the same menu in the same
  place with the same letter in every window is what a screen reader user learns once; a menu that is greyed out is
  skipped by the keys and read as nothing, while Context with one item saying why tells the user where they are; and
  the commands of the focus in one menu let Shift+F10 mean the same thing everywhere. The window base builds the bar
  so that no window can drift from it; the keys did not move.

## 7. Status

What each release brought is `CHANGELOG.md`, one section per version, and the release notes on GitHub. This section
listed the releases from 2.0.0 to 5.0.0 a second time until 2026-11-23 (the maintainer's cleanup); the git history
keeps that list.
