# AxKit.Core, the shared pure code

Code every app may use and no window needs (`docs/axKit/SPEC.md` AX-6.1). One bullet per piece; where a piece came from
another project on its second use, the bullet says so.

## The bundle

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
- `StoreListing` (AX-13, 2026-11-08): the Store ID and the addresses made from it (the Store page `ms-windows-store:`,
  the web page, the `winget install` line).
- `BundleInfo`: the version, read from the executable.
- `AppPaths` (AX-5.1): settings, logs, updates and the recently closed list under the `AxKit` folders; `ConsentFile`;
  `TakeOverOldFolders` (AX-5.4, 2026-11-05), the Axit folders of up to 4.0.0 moved into them on the first start, entry
  by entry through `TakeOverFolder`, tested in `AppPathsTests`; `OneAtATime` (2026-11-09), the name of the mutex that
  keeps one process of a background app (axStart, axClip, axHelp, axDescribe), marked with the run's folder in a test
  run so that the run's app and the user's own never count as one.
- `Ai` (docs/axKit/ai-help.md, 2026-11-09): `AiSettings` (AH-1.1: `ai-help.json`, the switch, the folders Claude
  may read, the model, `claudePath`, and `WorkingFolder` with `DefaultWorkingFolder`, `Documents\axKit Claude` or the
  test run's home, `EnsureWorkingFolder`, AH-2.1, AH-D6, 5.6.0), `AiRequest` (the command line of `claude -p`, the
  process started in the working folder with every allowed folder as `--add-dir`, the run with the prompt on standard
  input), `AiAnswer` (the JSON result read), `AiPrompts` with `WindowContext` (the prompts: about this window, from
  anywhere, a screen's picture, a picture file, a follow-up); tested in `AiTests`.
- `Consent` with `ConsentRecord` and `ConsentTarget` (AX-11.4, AX-11.5, 2026-10-29): what the user agreed to, one file
- `Theme` with `ThemeColor` (docs/axKit/theme.md, 2026-11-09): the theme file `%APPDATA%\AxKit\theme.json`, one for the
  bundle, a colour (`#RRGGBB`) or `system` for every class of object the windows draw (`Classes`, `Keys`); `Load` and
  `Read` give the theme or null with the reason, `Template` the whole file with `system` everywhere, `SetEnabled` writes
  the switch and keeps every other key; values not understood are `system` and listed. Colours are integers here, so
  the project stays off the desktop assemblies. Tested in `ThemeTests`, which also fails on a colour named outside
  `AxKit.Forms/ThemePainter.cs` and axMedia's video surface (TH-5.2).
  for the bundle, `%APPDATA%\AxKit\consent.json`: `Given`, `Record` (a file that cannot be written is logged and the
  window keeps the agreement for its session), `Withdraw`, `WithdrawAll` (Forget AI tools), `Line` for About, Licence;
  the targets' words and addresses (`ClaudeCode`, `Git`, `KeePassXC`, `Shell` per shell, `Server` per host, `AiTool`,
  `For` from an installed program's name, which throws for a name it does not know). Tested in `ConsentTests`.
- `SettingsFile` (AX-5.2, AX-5.3): how every app's settings file is read and written, an unreadable one copied to
  `.bad` once, and the take-over of an old file.
- `FileCase` (AX-5.1, 2026-11-10): `Match(path)` renames a file that exists under another case to the path's own
  spelling, so that a settings or log file an earlier version left as `axdown.json` becomes `axDown.json`, the name
  NVDA says as the app is said; `SettingsFile.Load` and `Log.UseApp` call it first. Tested in `FileCaseTests`.
- `WindowSettings` (AX-6.5): the base of every app's settings class, the font, the update check and the window's
  place with their sanitising; `WindowPlacement` is the window's place and size.
- `ConsoleSettings` (AX-6.5, 2026-10-23): on `WindowSettings`, the base of axClaude's and axTerminal's classes: the
  console size, the recent folders (`RememberRecentFolder`), the sounds and the signals, the transcript options, with
  `SanitizeConsole`; the JSON names are the ones the two files had.
- `HotkeySettings` (2026-11-09): on `WindowSettings`, the base of axStart's and axClip's classes: the hotkey as text
  (`HotkeyText`, read as `Key`, the app's `DefaultHotkey` when the text is not a usable one), Start with Windows and
  Say the first match while typing, with `SanitizeHotkey`; the JSON names are the ones the two files had.
  `AxKit.Forms/HotkeyForm` reads it without knowing the app.
- `Log`: one file per app; each app calls `Log.UseApp` first thing in its `Run`.
- `Updates/UpdateCheck` (the latest GitHub release: JSON parsing, version comparison; the repository name lives here)
  and `Updates/Updater` (the download and the hand-over to `install.ps1`, AX-3); `Updates/ReleaseSigning`, the release
  key's public half and the check of a zip's signature before anything is installed (AX-3.4, S-27).
- `Audio/WaveTone`: the chimes, the ticks and the working music as WAV bytes, generated in code; no media files ship.
- `BundleAbout` (AX-9.9): the About sections of the bundle, each a notice's title and text: Version (with the window's
  latest-release state), Licence, Attribution, and the axKit guide, `docs/axKit/README.md` embedded by
  `AxKit.Core.csproj` as `AxKitGuide.md`, read through `EmbeddedText`, which every app's `HelpText.UserGuide` uses for
  its own guide.
- `BundleHelp` (docs/axKit/app-kit.md AK-2.3, AX-7.2; 2026-10-24): the start and the end of every F1 text, written once (`NoticesSection`, the Notices section every text ends with, the same words in every window since 2026-11-09, with the app's name and what its notices cover): `Section`
  takes the app's name and its own lines of the section (what Tab moves between, what Ctrl+F finds, what Ctrl+S does;
  all the window keys in axClaude and axTerminal, whose heading is "Anywhere") and returns the first line naming the
  text's keys and the section with F1, the palette, the app's lines, the text size, the menu with Alt+F4 (`altF4` when
  it does more than close the app) and `BundleMenusLine`; each `HelpText.Shortcuts` adds its own sections after it.
- `RecentActions` with `RecentActionKind` (docs/axKit/reports.md RP-4.1, 2026-11-07): the last twenty actions of one
  window for a report, the commands run, what it said and the notices it showed, each with its time; `Lines(now)`
  gives the report's lines oldest first ("12 s ago: Find, Ctrl+F", "12 s ago, said: 3 matches") and `Age` the ages as
  people say them. The window base fills one (`AxKitForm.Recent`). Tested in `RecentActionsTests`.

## Paths, windows and lists

- `LocalPaths` (AX-9.4, AX-9.5; 2026-10-23): whether a path is on a drive of this computer, where asking whether it
  exists answers at once, and `ExistsQuickly`, which asks of a local path only, since a share can take the network's
  timeout, which no menu may wait for.
- `FileSize` (2026-10-24; from AxFile.Core's `FolderListing.FormatSize`, axPreview): a size as people say it,
  "512 bytes", "12 KB", "1.2 MB", "3.5 GB".
- `PathName` (AX-9.5): `Of`, the name of a path for a title, a label or an announcement, the last part or the drive's
  root itself (four copies until 2026-10-23).
- `RecentPaths` (AX-9.5): what every settings class does to its recent-path list, `Remember` (the newest first, once
  whatever the case, ten at most) and `Sanitize` after loading; the list property stays in each class so that its file
  keeps its shape.
- `RecentlyClosed` (AX-9.4): the bundle's list of the last ten files and folders the windows held when they closed or
  swapped them, in `%APPDATA%\AxKit\recently-closed.json` through `SettingsFile`: `Remember` from a window, `Load` for
  the menu (entries whose path still exists), `Title` as "notes.md (C:\notes\notes.md) - axDown".
- `AxKitWindows` (AX-9): the window lists of the axKit menu from the windows `AxKit.Forms.WindowMenu` reads off the system
  in Z order, which is recency: the app from a title's ending (" - axDown"), Open windows without the current window,
  each app's windows with it, and the item labels with the number as the mnemonic and a full stop as a pause
  ("&1. notes.md - axDown"; the apps' recent lists use the same `Label`).
- `MenuText` (AX-6.4, AX-10.2): `Plain`, a menu item's or a button's text as spoken or searched, the mnemonic marker
  and the trailing dots gone; the palette's names, the notices' key line and the Speech menu choices read it.
- `Palette` and `PaletteEntry` (AX-10): the command palette's lines: `Text` (a command's name with its menu path and
  key), `Find` (the matches for a typed text through `TextMatch`), `Spoken` ("Save as, File, Ctrl+Shift+S, 1 of 3")
  and `NoMatch`.
- `TextMatch` (AX-10.3): whether a name holds every word of a typed text, case and accents folded away (`Fold`,
  `Words`), and its `Rank` by the first word (the name starts with it, a word does, anywhere), null when a word is
  missing; from AxStart.Core's `ProgramMatch`, the command palette (2026-10-22).
- `TypeAhead` (docs/axFile/SPEC.md AF-3.6; from AxFile.Core, axProcess, 2026-10-24): where letters typed in a list
  jump, File Explorer's way: the prefix from the item in hand round the end, a run of one letter cycling from the next
  item.
- `NaturalOrder`: `StrCmpLogicalW` of shlwapi as an `IComparer<string>`, File Explorer's order of names (2 before 10);
  from AxFile.Core, axMedia (2026-10-22).
- `Accents` (2026-10-23): the accented letters of Latin-1 and Latin Extended-A taken apart (`Split`), put together
  (`Compose`) and stripped (`Strip`) from tables of their own, because the executable runs with invariant
  globalization, where `string.Normalize` returns its input; `TextMatch.Fold` and axFile's lookalike names use it. The
  test projects run in invariant mode too (`tests/Directory.Build.props`), which is what found both.

## Safety (docs/axKit/security-audit-2026-10-24.md)

- `SystemPrograms` (S-1): Windows' own programs by full path, and `OnPath`, the PATH searched in its full folders only; every process an app starts is named through it or by a full path it found itself. `KeepCurrentFolderOffSearches` sets Windows' `NoDefaultCurrentDirectoryInExePath` for the process, which the shells and programs it starts inherit (`Program.Main` calls it first; in axTerminal's cmd a program in the current folder is `.\name` since 2026-11-09).
- `ProgramInstall` (AX-2.9, 2026-10-24): the programs an app needs and Windows lacks (Git, KeePassXC, Claude Code, PowerShell 7), each with its winget id or its maker's own installer and its download page; `StartInfo` (the install without a window, every question answered; Windows PowerShell by its full path), `Winget` (only Windows' own, never one on the PATH, S-10) with `WingetArguments` (`--source winget`), `RefreshPath` (the PATH read again from Windows after an install); the PATH is searched through `SystemPrograms.OnPath`. Tested in `ProgramInstallTests`.
- `AutomationEnvironment` (S-11): a test run's secret and folder, read once in `Main` and taken out of the environment; only a new axKit window of the run gets them (`PassTo`).
- `ProcessRun` and `ProcessResult` (2026-10-25; from axGIT's `GitRunner`, Claude Code's version check and axPass's `KeePassCli` on their third use, inventory item 3): a program run without a window to its end under a time limit, input lines then closed, both outputs read under the limit, the whole tree ended on a timeout, `Finished` telling whether the program itself had ended (git's case of a child holding the pipes). Tested in `ProcessRunTests`; the callers' own tests cover their cases.
$1- `DisplayText` (S-42, 2026-10-25): text from outside shown as a list line, its bidi and zero-width characters made
  visible; `AxKit.Forms/AxKitList.Fill` applies it to every list it fills.
- `Signatures` (S-19): whether a program file carries a valid Authenticode signature (`WinVerifyTrust`, the file alone,
  no network), asked before a secret is handed to a program found outside an administrator's folders; axPass's tool.
- `Redaction` (S-20; from AxClaude.Core on its second use, Copy diagnostics): e-mail addresses, the account's paths, pipe names, ids and the account name replaced by placeholders, for text to be shown or shared.

## The AI tools (docs/axKit/mcp.md MC-6, 2026-10-24)

- `Mcp/McpAccess` with `McpSettings` and `McpClient`: the bundle's one setting in `%APPDATA%\AxKit\mcp.json` (off
  until the user turns it on, the paired clients by their program file), read again at every connection; the key in
  `%LOCALAPPDATA%\AxKit\mcp.key` through `UserSecret`, `NewChallenge`, `Proof` and `Proves` (HMAC-SHA256).
- `Mcp/McpProtocol`: MCP's JSON-RPC as the broker speaks it, one line in, one line out, the tools from two functions;
  `Text` reads a string a client sent without trusting its type. `McpTool` and `McpResult`.
- `ClaudeCodeProgram` (axClaude FR-1.2, MC-6.8; from `AxClaude.Core/ClaudeLauncher` on its second use, 2026-11-09):
  where Claude Code is, `Candidates` (an explicit path alone, else `claude.exe` and `claude.cmd` in every full PATH
  folder, the installer's `.local\bin`, npm's folder; a PATH text can stand in for the process's in tests) and `Find`
  (the first that exists). Tested in `ClaudeCodeProgramTests`.
- `Mcp/McpClaudeCode` (MC-6.8): the `claude mcp add` and `remove` command lines that register this AxKit.exe for every
  folder of the user; the window runs them on the Claude Code `ClaudeCodeProgram` finds (`AxKit.Forms/AxKitForm.Mcp.cs`).
- `Mcp/McpTools`: the first stage's tools, `NeverRead` (axPass, axClip) and `NeverOpened` (axClaude, axTerminal),
  and `OpenArguments`, the check of `axkit_open`'s app and path. `Mcp/McpAudit` (one line a call, the arguments
  hashed) and `McpRate` (ten calls a second). The pipes and the broker are `AxKit.Forms`' (`McpChannel`, `McpBroker`).

## Background apps

- `SignInStart` (docs/axStart/SPEC.md AS-1.3; from axStart, axClip, 2026-10-23): a background app's value under the
  user's Run key, "axKit <app>" with `"…\AxKit.exe" <verb> --hidden`, written only by the installed program, and not at all by the Store's copy, where `OpenWindowsStartupSettings` opens Windows' Startup apps page instead (AX-13.4, 2026-11-08). Named so
  since axProcess has a `StartupEntry` of its own, an entry of the startup list.
- `Hotkey` (docs/axStart/SPEC.md AS-4; from AxStart.Core, axClip, 2026-10-23): a system-wide key as `RegisterHotKey`
  takes it, its text both ways ("Ctrl+Shift+Space", any order and case on reading), `Refusal` (Ctrl, Alt or the
  Windows key, and a key of `Names`), axStart's `Default`; each app names its own default.

## Text and the editors

- `Paragraphs`: where Ctrl+Up and Ctrl+Down go in a text with `\r\n` line ends: the starts of its paragraphs, the
  first line or a line with text after a blank line, for `AxKit.Forms.EditKeys.Paragraph`.
- `TextLines` (docs/axDown/SPEC.md AD-6.4, AD-3.6; 2026-10-23): the caret's line and column in an edit control's text,
  and the index of a line and column put back, clamped to the line and never between a CR and its LF.
- `MarkdownOutline` (docs/axDown/SPEC.md §9; from AxDown.Core): the elements of a Markdown text for navigation keys,
  outside fenced code blocks: headings with a level filter, links, list items and lists, block quotes and tables,
  each with `Next…` and `Previous…` from a caret position returning an `Element` with `Start` and `Spoken`; block
  kinds by line, links by position.
- `TextDocument` (docs/axDown/SPEC.md AD-3; from AxDown.Core, axJSON): a text file as an editor window holds it, with
  the encoding, the byte order mark and the line ending it came with, so that `ToBytes` writes it back as it was,
  every line ending of the text the file's (a lone LF pasted from a Unix clipboard included); `FromBytes` and
  `ToBytes` are the pure core, `Read` and `Write` add the file and the 32 MB limit.
- `EditorSettings` (AD-8; from AxDown.Core, axJSON): what an editor window remembers between runs (font, word wrap,
  recent files, window, the update check), read and written through `SettingsFile` into the app's own file.
- `EditHistory` and `EditStep` (AD-2.5, 2026-10-21): the editor shell's undo and redo history: `Diff` finds the one
  step between the text before and after a change by the common prefix and suffix, the caret settling an ambiguous
  place ("a" typed next to an "a"); `Record` merges a change into the open run when its key was typed at `RunCaret`
  (more typing, a Backspace at the run's end, a Delete at its caret) and makes any other change (`typedAt` -1: a
  paste, a cut, a command, a reload) a step of its own that closes the run; `Break` is the window's second of silence;
  `Undo` and `Redo` hand the step back for the window to apply through the selection; four million characters of
  deltas at most, the oldest dropped.
- `ItemDocument`, `ItemField`, `FieldTypes` and `Parsed` (docs/axKit/item-apps.md IA-4; from AxTask.Core, axPass,
  2026-10-24): the item apps' format, an item as a list of fields of a title, a type and a value kept whole as JSON,
  the types read out and typed (dates as tomorrow or +3); a list's template (`NewList`, `WithTemplate`) is written
  with its values, which a new item starts with (IA-4.3; emptied until 2026-11-09, so `ReportFiles.ListText` wrote
  its own and a GitHub list's new items had no kind or state).
- `ItemFiles` (IA-4.5; from AxTask.Core's `ItemFolder` on its second use, the reports list, 2026-11-07): `FileStem`,
  the file name a Title gives, and `FreePath`, a free name beside the taken ones ("name - 2.json").
- `Reports/` (docs/axKit/reports.md, 2026-11-07): `Report` with `ReportKind`, `ReportKinds` (the names, the GitHub
  labels, `Parse`) and `ReportStatus` (Draft, Not sent yet with its reason, Sent, `AwaitsSending`); `ReportFiles`, the
  Reports list as an axTask folder (the template, `ListText` with the action keys, `ToItem`, `FromItem`, `Save`,
  `Write`, `WithStatus`, `WithSent`, `Unsent`); `ReportBody`, the issue's Markdown with the reporter line last and
  `ParseReporterLine`. Tested in `ReportsTests`. `GitHubIssues` (RP-6, RP-7), GitHub's REST API as the reports use
  it, the client handed in: `CreateAsync` (an issue as the reporter account), `ReadBlocklistAsync` (404 is an empty
  list), `ListAsync` (paged, pull requests left out, a token optional), `PatchAsync`, `CommentAsync`,
  `WriteBlocklistAsync` (the contents API, the file's sha read first), `NewIssueFormUrl` (the browser's form,
  prefilled), `ReasonOf` (an exception as a few words for the Status field); `GitHubException` with its `Reason`;
  `Blocklist` (`Parse`, `Blocks`, `ToJson`, `WithReporter`, `WithVersion`); `IssueCreated`, `IssueInfo`. Tested in
  `GitHubIssuesTests` against canned answers. `ReportSender` (RP-6.3 to RP-6.7): `ReporterId` (made once, kept in
  `AppPaths.ReportsFile` as `ReportSettings`), `SendUnsentAsync` (no token: every item says so; the blocklist read
  before a send and kept a day; the half-minute gap; each item posted and marked Sent with its issue, or marked with
  the reason; the first failure stops the run), `SendOutcome`; `ReporterToken` (RP-6.2): the token the release build
  embeds from `Reports/reporter.token`, `Encode` and `Decode` (the mixing `tools/reporter-token.ps1` repeats), null in
  a build without it. Tested in `ReportSenderTests`.
- `AxKitHttp` (AX-11.11, 2026-11-07): the one HTTP client of axKit's own code, the updater's and the report sender's,
  with the User-Agent `AxKit/<version>`; from the updater on its second use.
- `MediaFiles` (docs/axMedia/SPEC.md AM-4; from AxFile.Core, axMedia, 2026-10-22): the rules of sound and video files
  that need no engine, the extension sets with `IsMedia`, the time format and the typed time, the speed steps.

## The console and the transcript

From AxClaude.Core on their second use, axTerminal (2026-10-18; docs/axTerminal/SPEC.md §5, AT-D6); the gotchas are in
`src/AxClaude.Core/CLAUDE.md`.

- `Pty/PtyHost`: ConPTY through P/Invoke, the one implementation; `tools/PtyCapture` records through it.
  `Pty/StreamRecorder`: the raw stream to a `.vt` file plus chunk index.
- `Vt/VtParser`: byte stream to print, control, CSI and OSC, written into the `Screen` it is given. `Vt/Wcwidth`.
- `Vt/Screen`: cells, cursor, scroll region; rows carry their transcript `Line`, found again by identity with `RowOf`
  and made ahead of text with `EnsureLine`; `PullRowText` at a frame's end; `DetachRows` commits every row's line and
  blanks the rows; events for the frame end, the bell, the title, the cursor homed, the screen erased or reset.
- `Transcript/Line` and `LineKind`: one logical line with its kind, heading level, join flag and hidden flags; the
  Claude-specific kinds and the echo, replay and repaint flags stay here because the class moved whole.
- `Transcript/LineStore`: the transcript of a console: the lines and what a screen does to them, create, commit,
  remove, the text of a rewritten row; since 2026-10-23 also the UTF-8 decoder with `Feed`, the parser and the screen
  made by `NewScreen`, to which each model attaches its own handlers, `EndFrame` over the abstract `CompleteFrame`,
  `ToggleBookmark`, `VisibleBefore`, `Trim`, `ExchangeCount`, the `Changed` and `Bell` events with `RaiseChanged`;
  each app's model derives from it: axClaude's `SessionModel`, axTerminal's `TerminalModel`.
- `Transcript/TranscriptMirror`: visible lines to edit-control edits, the caret's place afterwards, display line
  numbers, the find search, the reading breaks; `ShowMarkers` off shows heading lines as `Line.BareText`, without the
  `#`, the export unchanged.
- `Transcript/Utterance`: how spoken lines join into one notification.

## Tests

Every piece is tested in `tests/AxKit.Tests`, one `<Piece>Tests.cs` file per piece (`VtScreenTests`,
`VtScreenOperationTests` and `ScreenMarkTests` for the screen); the transcript pieces are tested further in
`tests/AxClaude.Tests`, where the recorded fixtures drive them. The same project holds the bundle's own checks of the
sources: `LayoutTests` (the project references point one way, M2), `InventoryTests` (every app in every place it must
be), `KeyTests` (no window key equals a bundle key, AX-8.4), `ListTests`, `MenuTests`, `CheckBoxTests` and `ReleaseWorkflowTests` (the workflows' shape).

## Gotchas

- `TextDocument`: the Windows code pages come from `CodePagesEncodingProvider`, registered in its static constructor;
  the system's ANSI page is asked with `GetACP`, since the executable runs with invariant globalization and the
  culture cannot tell; an answer of 65001 falls back to 1252, since the bytes were just found not to be UTF-8; the
  encodings are constructed with their marks so that `GetPreamble` knows them; `GetBytes` never writes a mark,
  `ToBytes` adds it when the file had one.

## Rules

Nothing here references an app (M2); a piece arrives here only when a second app needs it, unchanged where possible
(M3); plain static classes and records, no frameworks (B-10). An app's settings class stays in the app; only the file
handling is shared.
