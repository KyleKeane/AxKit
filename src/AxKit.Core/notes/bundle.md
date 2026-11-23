# AxKit.Core notes: The bundle

- `Dispatch` (AX-1): which app a command line starts; `Apps`, each app's name, verb, project, `AppGroup`, what it
  `Opens` and whether Show in offers it, in the order every list of apps shows (AX-9.2), the one source of the axKit
  menu, Open in axKit, the Show in submenus and the inventory test; `Of` (an app by its verb), `PathFor` (the path an
  app is started on from a file or folder in hand), `EditorOf`; `AtSignIn` marks the apps that run from sign-in
  (axStart, axClip) for the installer and the package; `WithApplicationVerb` (docs/axKit/store-distribution.md 3.2,
  2026-11-08) puts the verb of the package entry a process was started from first ("AxDown" is down, "AxStartSignIn"
  is start --hidden; tools/make-manifest.ps1 names the entries) when the command line carries none.
- `PackageIdentity` (2026-11-08): whether this process runs from the MSIX package, the Store's copy (`IsPackaged`,
  `FullName`), and the id of the package entry that started it (`ApplicationId`), through kernel32's
  `GetCurrentPackageFullName` and `GetCurrentApplicationUserModelId`; null and false outside a package, as in the
  tests and the zip's copy; `AliasPath` and `ProgramPath`, the path recorded for later use (the package's `axKit.exe`
  alias in the Store's copy, the process path otherwise), since the package folder changes with every update.
- `StoreFirstStart` (AX-13.4a, 2026-11-14): the Store's copy right after its install: `Due` (packaged, no test run,
  no marker), `Verbs` (the apps that run from sign-in but the one starting), `MarkerFile` and `Mark`; `Program.Main`
  starts those apps hidden once per install, since Windows runs their startup tasks only from the next sign-in.
  Tested in `StoreFirstStartTests`.
- `StoreListing` (AX-13, 2026-11-08): the Store ID and the addresses made from it (the Store page `ms-windows-store:`,
  the web page, the `winget install` line).
- `BundleInfo`: the version, read from the executable.
- `AppPaths` (AX-5.1): settings, logs, updates and the recently closed list under the `AxKit` folders; `ConsentFile`;
  `SettingsFolder` and `SettingsSealFile` (AX-5.6);
  `TakeOverOldFolders` (AX-5.4, 2026-11-05), the Axit folders of up to 4.0.0 moved into them on the first start, entry
  by entry through `TakeOverFolder`, tested in `AppPathsTests`; `OneAtATime` (2026-11-09), the name of the mutex that
  keeps one process of a background app (axStart, axClip, axHelp, axDescribe), marked with the run's folder in a test
  run so that the run's app and the user's own never count as one.
- `Ai` (docs/axKit/ai-help.md, 2026-11-09): `AiSettings` (AH-1.1: `ai-help.json`, the switch, the folders Claude
  may read, the model, `claudePath`, and `WorkingFolder` (the switch, the folders, `claudePath` and the working
  folder under the bundle's seal, AH-1.1a, `notes/safety.md`; a refused file gives Claude off and no folders) with `DefaultWorkingFolder`, `Documents\axKit Claude` or the
  test run's home, `EnsureWorkingFolder`, AH-2.1, AH-D6, 5.6.0), `AiRequest` (the command line of `claude -p`, the
  process started in the working folder with every allowed folder as `--add-dir`, the run with the prompt on standard
  input; `Line` is `ClaudeCodeProgram.LineFor` over `Arguments`, and a refused line, a `claude.cmd` with a folder
  holding `%`, is an error answer without a run, AH-2.1a), `AiAnswer` (the JSON result read), `AiPrompts` with `WindowContext` (the prompts: about this window, from
  anywhere, a screen's picture, a picture file, a follow-up); tested in `AiTests`. `AiDocumentation` (AH-7): axKit's guides in
  the working folder; a new app needs its `Guide.<name>.md` line in the csproj. Beside them the workflow pages
  (`docs/axKit/user-workflows.md` WF-8): `WorkflowsName`, `WorkflowPages` (each group's page, found by trying the
  names of `Dispatch.Apps`, ai-tools and nvda-only, never by listing resources) as `WorkflowPageName`, and
  `WindowCommandsName`, embedded from `docs/axKit` as `tools/workflows.ps1` wrote them when axKit is built (a new group
  that is not an app needs its name in `WorkflowPages`; `AiDocumentationTests` fails on a page left out); `FurtherPages`
  (2026-11-17: `families.md`, `cases.md`, `outside-calls.md`, `privacy.md`, `changelog.md`, each a line of the table and
  a line of the csproj; a new Markdown document of `docs/axKit` or the repository's top fails `AiDocumentationTests`
  until it is added there or named in that test as a developer's); `Rule`
  tells Claude how to answer from them and ends with `Manner` (AH-7.3a, 2026-11-16: no remark on the user's rest,
  health or the time of day unless asked; in axKit's rule file, which is kept at its text, never in the user's
  CLAUDE.md). The tools' rule (`AiToolsRule`) has `ListedNotLoaded`, one bullet: tools listed and not yet loaded are
  loaded without a word to the user. A sentence added to either rule is a constant the tests compare.
- `AiCaution` (AX-11.12, root rule 11, 2026-11-13): the words that go with anything an AI wrote or asks to do, one place for every window: `Written` (under an answer of AI help), `Short` (in the name of an offered command's question), `Command` (the first section of its explanation), `Asked` (in the name of the list an AI tool's run of a window command asks through) and `Request` (in that question's text and explanation). A new feature of the kind takes its words from here.
- `Consent` with `ConsentRecord` and `ConsentTarget` (AX-11.4, AX-11.5, 2026-10-29): what the user agreed to, one file
  for the bundle, `%APPDATA%\AxKit\consent.json`: `Given`, `Record` (a file that cannot be written is logged and the
  window keeps the agreement for its session), `Withdraw`, `WithdrawAll` (Forget AI tools), `Line` for About, Licence;
  the targets' words and addresses (`ClaudeCode`, `Git`, `KeePassXC`, `Shell` per shell, `Server` per host, `AiTool`,
  `For` from an installed program's name, which throws for a name it does not know). Tested in `ConsentTests`.
  Under the bundle's seal since 2026-11-16 (AX-5.6, R-53; `notes/safety.md`): `Read` gives nothing when the seal does
  not check, `Problem` gives the sentence `NotUsed` for the question that follows, every write seals (app, target,
  version; not the date); `CommandLineShell`, the target of axTerminal's custom shell and the user's shells, names
  the command line, so that one line's agreement is not another's. Tested in `SettingsSealTests`. `ConsentTarget.Name`
  (R-61) is what the notice's sentences call the program: `Program` itself unless the target is longer than a name
  (axMCP's `ServerConsent`, whose target holds the whole of what would run).
- `Theme` with `ThemeColor` (docs/axKit/theme.md, 2026-11-09): the theme file `%APPDATA%\AxKit\theme.json`, one for the
  bundle, a colour (`#RRGGBB`) or `system` for every class of object the windows draw (`Classes`, `Keys`); `Load` and
  `Read` give the theme or null with the reason, `Template` the whole file with `system` everywhere, `SetEnabled` writes
  the switch and keeps every other key; values not understood are `system` and listed. Colours are integers here, so
  the project stays off the desktop assemblies. Tested in `ThemeTests`, which also fails on a colour named outside
  `AxKit.Forms/ThemePainter.cs` and axMedia's video surface (TH-5.2).
- `SettingsFile` (AX-5.2, AX-5.3): how every app's settings file is read and written, an unreadable one copied to
  `.bad` once, and the take-over of an old file. A value that does not read costs that value alone
  (`WithoutWhatDoesNotRead`); a file that is there and cannot be opened is tried again, then never saved over in
  that process (`NotRead`, R-51). `Heading` is the sentence a window says before a load's error text: that the file
  was read except for the settings named (`PartlyReadHeading`), or that it could not be read and the defaults are
  used (`NotReadHeading`). `Save` returns whether it wrote, for `SettingsSeal.Remember`. A save keeps what
  another window saved meanwhile (AX-5.7, R-62): `Views` holds this window's settings per file as JSON (set by `Load`
  and by each `Save`), `Merged` takes the file's value of every top-level member this window did not change, whole
  (no list merging); a sealed class hands `Save` its seal check for the file and its sealing for what is written.
  `NotWritten` and `NotWrittenOnce` are the sentence of a file that could not be written (AX-5.9, R-63).
- `FileMutex` (AX-5.7; repairs.md R-62; from `RecentlyClosed` on its second use, the settings files, 2026-11-22): the mutex of a file several windows read, change and write. `Name(kind, file)` is the same for every process that writes the file, whatever the case of its path; `Held(mutex, wait)` waits and counts a mutex left by an ended process as held. `SettingsFile.Save` does its read of what the file gained, its merge and its write under it (`MutexWaitMilliseconds`; a window that does not get it in time saves as before, so a save is never lost for the window itself and never hangs): two windows closed in the same instant each read the file, and the second could find it being put in place, give up the merge and write its own settings alone. The check is `tests/scripts/axFile/11-two-windows-save.txt`, which failed twice in a day before and passed 25 times in a row after.
- `FileCase` (AX-5.1, 2026-11-10): `Match(path)` renames a file that exists under another case to the path's own
  spelling, so that a settings or log file an earlier version left as `axdown.json` becomes `axDown.json`, the name
  NVDA says as the app is said; `SettingsFile.Load` and `Log.UseApp` call it first. Tested in `FileCaseTests`.
- `WindowSettings` (AX-6.5): the base of every app's settings class, the font, the update check and the window's
  place with their sanitising; `WindowPlacement` is the window's place and size. `OtherMembers` keeps the file's
  members this version does not know (AX-5.8, R-67); the consent list and the recently closed list have the same.
- `ConsoleSettings` (AX-6.5, 2026-10-23): on `WindowSettings`, the base of axClaude's and axTerminal's classes: the
  console size, the recent folders (`RememberRecentFolder`), the sounds and the signals, the transcript options, with
  `SanitizeConsole`; the JSON names are the ones the two files had.
- `HotkeySettings` (2026-11-09): on `WindowSettings`, the base of axStart's and axClip's classes: the hotkey as text
  (`HotkeyText`, read as `Key`, the app's `DefaultHotkey` when the text is not a usable one), Start with Windows and
  Say the first match while typing, with `SanitizeHotkey`; the JSON names are the ones the two files had.
  `RetiredDefault` with `TakenOver` (AX-8.11): a file's retired default becomes the new one on loading.
  `AxKit.Forms/HotkeyForm` reads it without knowing the app.
- `Log`: one file per app; each app calls `Log.UseApp` first thing in its `Run`.
- `Updates/UpdateCheck` (the latest GitHub release: JSON parsing, version comparison; the repository name lives here)
  and `Updates/Updater` (the download and the hand-over to `install.ps1`, AX-3); `Updates/ReleaseSigning`, the release
  key's public half and the check of a zip's signature before anything is installed (AX-3.4, S-27).
- `Events/EventKind`, `EventSound`, `EventPresenter` (docs/axKit/events.md EV-3, EV-5.2, 2026-11-17): the kinds of event of axKit as one closed list, 52 values in the order and the six classes of docs/axKit/families.json, where each is described for people; the sounds by name; and the one table that gives a kind its sound or none (`SoundsOf`, `Allows`; made once, so that asking allocates nothing), with a kind's id as the file writes it (`Id`, `Of`). Three kinds still have two sounds, and a place that raises one names which it plays; `EventKindTests` pins exactly those and holds the kinds, the sounds and the table to the file both ways. A kind is added in the enumeration and in the file together, at its place in the file's order; a sound is given to a kind here and in the file, only by the maintainer's ear. The door that plays them is `AxKit.Forms/EventDoor`.
- `Audio/WaveTone`: every sound's WAV bytes, made in code; `Audio/OpeningSounds` (AX-7.11, 2026-11-22): the four sounds of what opens, `Form`, `List`, `Field`, `Text`, each one call of `WaveTone.Pings` beginning with the same mark, as the maintainer chose them by ear, tested in `OpeningSoundsTests`; `Audio/AmbientSounds`: axClaude's music and the sounds of background work (the start, the finish, the recurring chords, `BackgroundChord`, made off the UI thread since each takes some tens of milliseconds), each one method; `WaveTone.Pop` is the tool call's pop.
- `BundleAbout` (AX-9.9): the About sections of the bundle, each a notice's title and text: Version (with the window's
  latest-release state), Licence, Attribution, and the axKit guide, `docs/axKit/README.md` embedded by
  `AxKit.Core.csproj` as `AxKitGuide.md`, read through `EmbeddedText`, which every app's `HelpText.UserGuide` uses for
  its own guide.
- `BundleHelp` (docs/axKit/app-kit.md AK-2.3, AX-7.2; 2026-10-24): the start and the end of every F1 text, written once (`NoticesSection`, the Notices section every text ends with, the same words in every window since 2026-11-09, with the app's name and what its notices cover): `Section`
  takes the app's name and its own lines of the section (what Tab moves between, what Ctrl+F finds, what Ctrl+S does;
  all the window keys in axClaude and axTerminal, whose heading is "Anywhere") and returns the first line naming the
  text's keys and the section with F1, the palette, `ReportLine`, `MainActionLine`, the app's lines, the text size, the menu with Alt+F4 (`altF4` when
  it does more than close the app) and `BundleMenusLine`; each `HelpText.Shortcuts` adds its own sections after it.
- `RecentActions` with `RecentActionKind` (docs/axKit/reports.md RP-4.1, 2026-11-07): the last twenty actions of one
  window for a report, the commands run, what it said and the notices it showed, each with its time; `Lines(now)`
  gives the report's lines oldest first ("12 s ago: Find, Ctrl+F", "12 s ago, said: 3 matches") and `Age` the ages as
  people say them. The window base fills one (`AxKitForm.Recent`). Tested in `RecentActionsTests`.
- `Ai/AiFolderCheck` (docs/axKit/ai-help.md AH-7.8, repairs R-54, 2026-11-16): the files in Claude's working folder that Claude Code obeys and axKit did not write, found by name and place (`.mcp.json`; the two settings files for their `hooks` member alone, hashed by that member's text; every file under `.claude\skills`, `commands`, `output-styles`, `plugins`, `agents`, `rules` but axKit's two rules and a file the caller wrote that still holds its text), each with a SHA-256. `NewOrChanged` compares them with the accepted list (`AppPaths.AiFolderFilesFile`, by folder, under `SettingsSeal`; a refused list is empty), `Accept` writes the user's Start anyway, `NoticeTitle` and `NoticeText` are the notice's words. axHelp's window asks (HE-2.9); never flag `CLAUDE.md`, or a settings file for its permission answers. `AiRequest.Arguments` carries `--strict-mcp-config` and `--settings <AppPaths.ClaudeNoHooksFile>` (AH-2.6), so a question obeys no hook and no server of the folder. Tested in `AiFolderCheckTests` and `AiTests`.
