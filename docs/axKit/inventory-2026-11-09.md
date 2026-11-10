# The inventory and consolidation run of 2026-11-09

Asked by the user on 2026-11-09 morning, for a day he is away: "do a complete inventory of the repo and codebase to
get things into a really optimized and efficiently shared codebase so that we can start improving things in a simple
and coherent way using shared components... be very thorough and thoughtful. Make a plan first to stay on track
throughout the day." This page is that plan, then the inventory, then the record of what was done. It is the run
`cleanup-run.md` specified on 2026-10-25, carried out by one assistant session with five read-only helpers for the
sweep rather than the agent workflow that page names; its ground rules (section 1), its gate (section 2), its work
list (section 3) and its order (section 4) apply word for word. `inventory-2026-10-24.md` is the previous inventory;
its twelve items are done, so this one starts from a fresh search.

**The one rule over everything: nothing the user can do today stops working, and nothing they hear changes.** A
cleanup that would change a key, a menu item, an announcement, a notice text, a file format, a setting name, a
command-line option or a default is not made; it goes to section 11 for the user.

## 0. The plan of the day

Where: the branch `consolidation`, cut from `main`, in a worktree under the session's scratchpad (the user's checkout
runs axKit; a build there would write into the running program's `bin`). One step per commit, imperative subject,
the reason in the body; every commit passes the gate below. The branch is pushed as it grows and never merged by the
assistant: the user merges after the NVDA checks of section 12.

The gate for every commit: `.\test.ps1` (every test project, Release), `dotnet build src\AxKit\AxKit.csproj -c Release`
with no warning, `tools\outside-calls.ps1 -Check`, and every PowerShell script parsing. The final gate adds the
automated scripts (`tools\AxAutomate` on `tests\scripts`) and `publish.ps1 -NoInstall` in the worktree.

| Phase | What | State |
|---|---|---|
| 0 | The branch and the worktree; this plan written and committed; a tracker item in the management lists | done |
| 1 | The baseline gate: 1,125 tests green, the Release build with no warning, the outside-calls page current, every script parsing; the unused-code analyzers run once (IDE0005 58, CA1859 42, CA1822 14, CA1812 4, IDE0060 4; IDE0051 and IDE0052 none) | done |
| 2 | The sweep: five read-only passes in parallel (the shared window pieces and the windows' own code; the pure code and its tests; the three window families and the list windows side by side; hacks and the attack surface; the drift of notes, specs and scripts), written up under the session's `scratchpad\inventory`, verified by the assistant before anything is changed, and condensed into sections 2 to 8 here with the ranked work list of section 9 | done |
| 3 | Unused code (3.1), one project at a time, shared projects last | done |
| 4 | Copies to fold (3.2), the lowest risk first, each piece moved in a commit of its own with every note that named it | done; the medium-risk pieces left (W15, W16, W21) |
| 5 | Hacks to make plain (3.3), only the ones whose reason is understood from the code or the notes | done; one for the user (the asks poll) |
| 6 | Attack surface (3.4), each finding with a test, added to the audit as S-54 onwards | done: S-54 to S-57; three questions for the user |
| 7 | Drift: notes, specs and scripts brought back to the code, no behaviour touched | done |
| 8 | The final gate, the report (`cleanup-run-report.md`), the branch pushed, the tracker items for what needs the user, the NVDA sections worth hearing again, and the recommendation of what to do next | done |

A phase that runs long is cut, not rushed: whatever is left of it goes to section 9 as open, with its reason.

### What went wrong on the way, and the fix

The run began on the branch `cleanup`, the name `cleanup-run.md` gives. Its first push from the worktree ran the
pre-push hook made that morning (`tools/hooks/pre-push`, `tools/sync-repos.ps1`): git gives a hook's process `GIT_DIR`
and its kin, the sync script's git calls inherited them, and so every call acted on the code repository whatever
folder `-C` named. The script committed the code's index as "the management lists" (the private priorities items and
the issues list's `list.json`, which holds the computer's path) and pushed that as `ef4fc36` on `cleanup`, in the
public repository. The main checkout's pushes had been fine by luck: there `GIT_DIR` is the relative `.git`, which
resolves to each folder's own repository. Fixed within minutes on `main` (95324d4): the hook unsets the variables,
the script drops them too, and before touching a folder it proves that git answers for that very folder and that
origin is the repository of that name, else it does nothing and says so. The assistant neither force-pushes nor
deletes branches, so `cleanup` was covered with a commit that restores the code tree (870543d) and left for the user
to delete (`git push origin --delete cleanup`; the lists stay in that commit's history until then, and GitHub support
can purge it on request; nothing in them is a secret). The work went on as `consolidation`, rebased onto the fixed
`main`, from a fresh worktree. The tracker holds the item.

## 1. The repository, measured (main at 3d180f1)

419 C# files, 57,422 lines, in 48 projects; 73 Markdown documents; 45 scripts.

| Part | Projects | Lines | Note |
|---|---|---|---|
| `AxKit.Forms`, the shared window pieces | 1 | 10,302 | 58 files: the window base and its partials, the lists, notices, the console view, the editor shell, the item form, the hotkey form, the channels |
| `AxKit.Core`, the shared pure code | 1 | 7,982 | 73 files: settings, paths, the apps listing, the updater, reports, MCP, the pseudo console and the screen, text helpers |
| The fourteen window projects | 14 | 13,523 | axFile 3,046; axClaude 2,266; axTask 1,419; axPass 1,194; axGIT 1,116; axTerminal 1,054; axMedia 831; axProcess 650; axClip 567; axStart 535; axCalc 476; axPreview 138; axJSON 136; axDown 95 |
| The thirteen app Core projects | 13 | 11,321 | axClaude 2,900; axFile 1,475; axTask 1,013; axPreview 880; axTerminal 861; axJSON 755; axMedia 753; axProcess 703; axGIT 692; axCalc 604; axPass 391; axClip 158; axStart 136 (axDown's pure code is in `AxKit.Core`) |
| `AxKit`, the executable | 1 | 183 | `Program.cs` |
| The tools | 4 | 2,592 | AxAutomate 1,606; PtyCapture 357; EdgeDrive 352; QaRun 277 |
| The tests | 14 | 11,519 | AxKit.Tests 4,161 (415 tests); AxClaude.Tests 3,163; the rest 134 to 1,228 each |

The three smallest window projects (axDown, axJSON, axPreview) are records over the editor shell: the shape every
app should tend towards. The three largest (axFile, axClaude, axTask) are where the sweep looked hardest.

## 2. The shared window pieces (`AxKit.Forms`) and who uses them

Sixty files. Twenty-seven are reached by every window through the bases (`AxKitForm` and its partials: notices,
find, fonts, the palette, consent, the update flow, reports, the MCP registration; `AxKitList`; `OverlayPanel`;
`Announcer`; `StatusLayout`; `BundleKeys`; `Command`; `AppHost`; `AppMessage`'s receiver; `Sounds`; `User32`;
`WindowReading`; `WindowAccessibleObject`). Twelve are used by exactly two apps (the console family: `ConsoleForm`
and its partials, `TranscriptView`, `TranscriptKeys`, `SessionControls`, `ControlArea`, `KeyHold`; the item family:
`ItemForm` and its partials, `ItemKeys`; the hotkey family: `HotkeyForm`; `ListViewState` for axGIT and axProcess).
The editor shell (`TextEditorForm` and its partials, `EditorApp`, `EditorHost`, `EditorKeys`, `MarkdownKeys`,
`TextJump`, `EditKeys`) serves axDown, axJSON and axPreview, which are records over it. One piece has one app:
`PrivateClipboard` (axPass). Nine are internal to the project (`AutomationChannel`, `AxKitCheckBox`, `McpBroker`,
`McpChannel`, `UserPipe`, `ReportControls`, `RecentMenu`, `ThisProgram`, `WindowsOpenWith` among them).

Members of the bases that only one window uses are legitimate hooks (axClaude's `RestartPending`, `StartRecording`,
the notice hooks `ConsoleForm` uses, axTask's `BeforeRefresh`, axPass's `FieldsFixed`, axClip's `ForegroundBefore`)
except three: `ConsoleForm.Verb` repeats the base's `AppVerb`, `ConsoleForm.Post` could be private, and axStart's
`UpdateRestartText` override equals the default (section 4, item F2).

## 3. The shared pure code (`AxKit.Core`) and the app Cores

Every file of `AxKit.Core` has a user outside it except as noted: `Signatures` is used by `AxPass.Core` alone (S-19
is written as a bundle rule, so it stays shared); `Hotkey.Default`, `FieldTypes.UserName` and `Password` sit in shared
classes by documented choice; `McpClaudeCode` is shared because the window base registers the MCP server. Used by
`AxKit.Forms` alone, which is shared too: `WaveTone`, `DangerousFiles`, `EditHistory`, `TextLines`, `Paragraphs`,
`TypeAhead`, `RecentActions`, `Palette`, `TranscriptMirror`, `Updater`, the MCP pieces, `ReportSender`. The earlier
inventory's moves hold: `FileSize`, `TypeAhead`, `NaturalOrder`, `Redaction`, `RecentPaths`, `PathName`, `MenuText`,
`ProcessRun` each have one implementation, and every child process not run through `ProcessRun` has its reason
(streaming the installer's lines, a process left running, the shell execute of a user's program, the pseudo console).

The twelve settings classes repeat the same three lines each (`Load` with `Sanitize`, `Save`, `DefaultPath`) on
purpose: the notes say an app's settings class stays in the app and only the file handling is shared; recorded, not
changed.

## 4. Copies: the same thing written twice

Each with its places (the main checkout's paths at 3d180f1), what differs, the shared home, the risk to behaviour and
what NVDA could hear. "F" items come from the window sweep and the family comparison, "C" items from the pure code.

- **F1 A notice with one field and a check of the typed text, eight times**, while the shared `AskText` sits in
  `ItemForm.Fields.cs:359-379`: axGIT's Commit and New branch (`GitForm.Commands.cs:193-222, 301-329`), axFile's New
  folder, Move to folder, Add a program and Filter (`FileForm.Operations.cs:229-247, 309-359`, `FileForm.Programs.cs:101-120`,
  `FileForm.Browse.cs:302-309`), axMedia's Go to time (`MediaForm.Player.cs:211-234`), axTask's GitHub account
  (`TaskForm.GitHub.cs:419-440`, a hidden field). The empty message differs per copy and `AskText` has no hidden
  option. Home: `AxKitForm.AskText(title, label, initial, emptyMessage, hidden, accept)`. Six copies behave as
  `AskText` does (the notice stays open on a refused text); axFile's New folder and Add a program announce the problem
  from a closing button, so they keep their own code (changing them would change what is heard, section 11). Risk low.
- **F2 Eleven `RestartArguments` overrides equal `[AppVerb, PathInHand]`**; `ConsoleForm.Verb` repeats `AppVerb`;
  axStart's `UpdateRestartText` equals the default. Home: a default on the base. Risk none; NVDA nothing.
- **F3 The status bar's tail (two labels set, `StatusLayout.Fit`) nine times**, axStart never calling `Fit`. Home:
  `AxKitForm.SetStatus(left, right)` keeping `ConsoleForm`'s no-change guard. Risk none.
- **F4 The constructor's tail (`Controls.AddRange`, `MainMenuStrip`, `ApplyTextFont`, `RestoreWindow`) in eleven
  windows**; `ItemForm` already names it `PlaceControls`. Home: `AxKitForm.PlaceControls`. Risk low (the docking
  order is identical everywhere).
- **F5 A yes-or-cancel notice from the same two choices at twelve places.** Home: `Notice.Ask` beside `Notice.Plain`.
  Risk none.
- **F6 A menu of one-checked-item choices built and re-checked six times** (axFile's Sort by, axProcess's Sort
  processes by, axClaude's Speak replies, axTerminal's Switch shell and Speak output, axPass's Lock after, axCalc's
  Angles). Home: a small `ChoiceItems<T>` in `AxKit.Forms` that makes and re-checks the items; the window keeps the
  setting, the save and its words. Risk low.
- **F7 axGIT's three-views flow written again in axProcess** (`GitForm.Views.cs:14-159`, `ProcessForm.cs:213-351`):
  the 2026-10-24 inventory moved the record (`ListViewState`), not the flow. Home: `AxKit.Forms/ListViews` holding
  the states, the current index and the list, the windows keeping their loaders and words. Risk low to medium.
- **F8 Every Show-in window writes the same guard twice** (`...OrSay` and `ShowInExplorer`); axClaude alone says
  "Folder missing" on a gone path. Folding would give that message to windows that start Explorer on a gone path
  today: a change of what is heard, so the common part only (section 11 for the message).
- **F9 Three clipboard writes inside `AxKit.Forms` bypass the base's `CopyText`**: Copy report
  (`AxKitForm.Report.cs:201-205`, no catch: a held clipboard would be a crash), Copy details
  (`AxKitForm.Updates.cs:340-351`) and the viewer's Copy value (`TextEditorForm.Viewer.cs:326-342`), the last two with
  their own "The clipboard is busy; try again". Copy report goes through `CopyText` (a message where a crash was);
  the two keep their words.
- **F10 The console windows' folder and session code, round two**: `PickFolder`, `SetFolder` with Recently closed,
  the "folder no longer exists" guard six times, `ForceRestart`, `Relaunch`, the New session notice and its start,
  the Escape guard, the opening half of the control area (`MainForm.Folder.cs`, `MainForm.NewSession.cs`,
  `MainForm.Process.cs`, `MainForm.Input.cs`, `MainForm.Area.cs` against `TerminalForm.NewShell.cs`,
  `TerminalForm.Process.cs`, `TerminalForm.Input.cs`). Home: `ConsoleForm` holds the frame, the apps their state
  (`_launchedFolder`, `HideReplay`). Risk medium; the words stay.
- **F11 axStart and axClip still hold about seventy identical lines around `HotkeyForm`** (`PrepareShowing`'s
  bounds, `KeepHotkey`, `SignInStart.Apply` twice, Hide, Quit, Hotkey..., Say the first match, `Selected`, `Fill`),
  and their settings share five members (C3). Home: `HotkeyForm` with an `AxKit.Core/HotkeySettings` base. Risk low.
- **F12 Filling a list from labels in five shapes**, `DisplayText.Visible` (S-42) applied in three. Home:
  `AxKitList.Fill(labels, name, select)`; S-42 then reaches two more lists (the viewer's details, the hotkey matches),
  recorded as S-54. Risk low.
- **F13 Right click selecting the item and opening the bar's menu, three times.** Home: `AxKitList.OpenMenuOnRightClick`.
  Risk none.
- **F14 The F1 texts' Notices section has drifted** (nine the same, three shorter, two missing). A generator would
  change five F1 texts: what is heard, section 11.
- **F15 "Select the main control unless a notice shows" in `OnShown`, seven windows**; the family comparison verified
  that the control is the window's `FocusTarget` in every case. Home: the base's `OnShown`, `HotkeyForm` opting out.
  Risk low once the automation channel's start order is checked.
- **F16 "One line is an announcement, more is a notice"** in axGIT and axFile. Home: `AxKitForm.Failed(title, text)`.
  Risk none.
- **F17 The title assembled in three spellings** (`AppTitle`, the app's `Name`, `_app.Name`), equal today. Home:
  `AxKitForm.SetTitle(first)`. Risk none.
- **F18 The 300 ms "say the first match" field exists twice inside `AxKit.Forms`** (`HotkeyForm`, the palette). Home:
  one `MatchSpeaker`. Risk low to medium; later.
- **F19 The focus put back after a Windows dialog by each caller**; the editor's two cases are asymmetric, so a shared
  rule would change them: section 11.
- **F20 Six windows register the same three find-in-list keys.** Home: `AxKitForm.AddFindInListKeys`. Risk none.
- **F21 The closing question of the console windows** differs: axTerminal's "Keep running" does not call
  `KeepWorkingAfterUpdate`, so a pending update restart survives the cancel and "Update installed" is never said
  there (a fault, heard today). A fix changes behaviour: section 11.
- **F22 The item apps lack the bundle's control keys** (Ctrl+Tab, Ctrl+Shift+Tab, F6 do nothing in axTask and
  axPass, where every other window has them). A fix changes behaviour towards AX-8: section 11.
- **C1 Claude Code is located twice with different candidates** (`ClaudeLauncher.cs:21-63`, `McpClaudeCode.cs:20-34`;
  the MCP side misses an npm install). Home: one locator in `AxKit.Core` with axClaude's order. Risk medium (the
  order on a machine with both files); section 11 for the npm fallback the MCP side would gain.
- **C3 axStart's and axClip's settings share five hotkey members.** Home: `HotkeySettings` (with F11). Risk low; the
  JSON names stay.
- **C4 Three "name - 2" loops** (`FileOperations.cs:147-158`, `ItemFiles.cs:23-38`, `Backups.cs:27-31`). Home:
  `ItemFiles.FreePath`, which gains a list of taken names. Risk low.
- **C5 JSON string helpers four times and `GetString` twice.** Home: `FieldTypes.TextOf` and one internal reader.
  Risk none.
- **C6 The item date formats written by literal in three projects.** Home: `FieldTypes.DateText`, `DateTimeText`.
  Risk none.
- **C8 `ShellLauncher` re-derives Windows' programs** that `SystemPrograms` has, and wraps `OnPath`. cmd is also taken
  from `ComSpec`, which S-1 questions: the PowerShell path and the wrapper go; `ComSpec` is section 11.
- **C9, C10, C18, C20, C21** The WindowsApps folder spelled twice, the PATH parsed twice in `SystemPrograms`, a file's
  head read twice in axPreview, a share tested twice in `McpTools`, two constants equal to `RecentPaths.Max`. Risk none.
- **C13 `ItemDocument.TemplateArray` drops the template's values**, so `ReportFiles` writes `list.json` by hand and a
  GitHub list's written template loses its defaults. A fix changes the `list.json` written for new lists: section 11
  (IA-4.3 favours it).
- **C15 `TextSniff` repeats the editors' file types** from `Dispatch`. Home: `Dispatch`. Risk low.
- Recorded, not changed: C2 the settings classes (by rule), C7 clock stamps, C11 line splitting, C12 "N items",
  C14 the two models' exchange block, C16 line starts, C17 ordinal against natural order (a question), C19 "is under",
  C22 single users.

## 5. Unused code

The analyzers found no unused private member (IDE0051, IDE0052 none). Found and done: 36 using lines naming a
namespace the file never used (IDE0005; the blocks sorted with System first as `.editorconfig` asks), two
parameters nothing reads (`FieldTypes.Empty`'s options, `SmartList.Order`'s date). Twelve public members or types are
used in their own file only and become private or internal: `McpAccess.MakeKey`, `ProgramInstall.Winget`,
`FieldTypes.ParseDateTime` and `SpokenDate`, `TranscriptMirror.SeparatorBefore`, `ReportBody.ReporterLine`,
`SignInStart.ValueName`, `GitHubIssues.IssuesApi`, `BlocklistUrl` and `BlocklistApi`, `ConsentList`,
`RecentlyClosedList`; `ConsoleForm.Post` likewise. `KeePassCli.StartLasting` has no caller (axPass copies through
`PrivateClipboard` since S-15; its note still said the tool's `clip`) and goes. Kept public on purpose:
`Dispatch.AppIcon` (read as text by `tools/apps.ps1`), `MarkdownList` (a result type a test reaches). Left: CA1859
(concrete types in place of interfaces, a style the code does not follow), CA1822 on `ActionLog`'s entry methods
(the log is the object one talks to), CA1812 on `TempFolder` (compiled into every test project, used in most),
`src/AxList.Core` and `tests/AxList.Tests` folders holding build leftovers of the archived draft (untracked).

## 6. Hacks

Twenty WinForms timers, no threading timer: nineteen keep (each a gotcha, a spec clause or a wait for something
without an event), one could be plainer (axTerminal's 500 ms `_asks` poll as a one-shot quiet timer, AT-5.4's words;
it would be heard again, so section 11). Twenty `BeginInvoke` sites, none outwaiting another; four get a one-line
reason. Fifteen re-entrance flags: fourteen keep, one is a parameter smuggled through a flag
(`MainForm.Area.cs` `_screenNoticeOpening` with a `NoticeChime` override; a `Chime` field on `Notice` is plain). Ten
catch-alls (six with a reason, four get one) and thirteen empty catch blocks (three explained, ten get one line); none
to narrow safely. No reflection, `dynamic`, `unsafe`, `#pragma` or `SuppressMessage` anywhere.

## 7. Attack surface

After `security-audit-2026-10-24.md` (S-1 to S-53 decided). Twenty-four process starts, none by bare name, all with a
deliberate `UseShellExecute`; five with a string argument, four justified. New, fixed in this run with a test each:

- **S-54** `AppHost` logs every argument of the start line, so `--message <text>` (an issue's body from GitHub, up to
  8,000 characters) lands in `axClaude.log` and from there in F4 reports and Copy diagnostics: the value is redacted
  in the log line.
- **S-55** `ItemFiles.FileStem` lets a title be a reserved device name (`con`, `nul`, `com1`...), so the item file
  cannot be written and a list's refresh keeps failing: such a stem gets a suffix.
- **S-56** `AutomationChannel` reads the first line without a length cap before the secret check, where `McpChannel`
  caps at 64 KB: the same cap (test runs only).
- **S-57** `ClaudeLauncher`'s two `cmd /c` lines lack the `HasCmdSpecials` refusal that the MCP registration applies to
  the same path: the same refusal.
- The audit's text says both "receivers refuse network paths" and "the receiver's side accepted" of the `WM_COPYDATA`
  hand-over; the code accepts any signed path: the document says one thing.

Needs the user (section 11): S-1's `NoDefaultCurrentDirectoryInExePath` never landed in `Program.cs`; a `list.json`
`open` action may start claude or a terminal on a shared folder's list, which the MCP tool refuses; a zip's entry
count has no cap in axPreview (every file opens).

## 8. Drift: notes, specs and scripts

Notes: eighteen stale or wrong statements in eleven files (axStart's note describes `HotkeyForm`'s members as
`StartForm`'s; `FolderInHand` for `PathInHand` in the Forms and axTerminal notes; axCalc's `ProcessWindowKey` that does
not exist; `OpenIn` and `OpenInAxFile` gone from axFile's and axMedia's; `TaskRules.Ordered` that does not exist;
axPass's clipboard by the tool's `clip`; `src/AxKit/CLAUDE.md`'s Run list stopping at axStart; the root's docs list
missing four pages and saying axStart's pass is still to come); eight code files no note names (`DisplayText`,
`Signatures`, `ReleaseSigning`, `McpClaudeCode`, `PrivateClipboard`, `WindowAccessibleObject`, `Command`,
`GitTrust`); about forty-five "since <date>" stamps that only repeat the changelog. Specs: sixteen places naming code
that moved (the bundle spec's 5.1 project table and 5.4 file placements, `Redaction`'s folder in axClaude's,
`ViewState` in axGIT's, `OnListKeyPress` and `AtEdge` in axFile's, `Updater.LaunchInstaller` in `adding-an-app.md`).
Scripts: no unused parameter in 25 scripts; six stale comments; `release.ps1` accepts any `docs/` folder as a
changelog prefix. The root note's "`$PSScriptRoot` is empty while parameter defaults are evaluated" did not reproduce
on this computer's PowerShell 5.1; `tools/store-screenshots.ps1` relies on the opposite and has run. No test pins the
notes to the code; the sweep's scan is the shape of one.

## 9. The work list, ranked by payoff against risk

In the order of the work; the state column is kept as the day goes. "W" numbers are this list's.

| W | What | From | State |
|---|---|---|---|
| 1 | Remove the using lines nothing needs | 5 | done, a27f6b0 |
| 2 | Drop two parameters nothing reads | 5 | done, 2a9a127 |
| 3 | Twelve members and types private or internal; `ConsoleForm.Post` private; `KeePassCli.StartLasting` gone with its note fixed | 5 | done, b27ced4 |
| 4 | `RestartArguments` by default from `PathInHand`; `ConsoleForm.Verb` and axStart's `UpdateRestartText` go | F2 | done, b5af56c |
| 5 | `Notice.Ask` for the yes-or-cancel notices | F5 | done, 42cc6f5 |
| 6 | `SetStatus` on the base | F3 | left: nine windows read their two labels by name, so a pair object would touch every one for two lines each |
| 7 | `PlaceControls` on the base | F4 | done, 9490e8e |
| 8 | `Failed`, `SetTitle`, `AddFindInListKeys`, `OpenMenuOnRightClick`: small base helpers | F16, F17, F20, F13 | done, 9490e8e |
| 9 | Small folds in `AxKit.Core`: JSON text, item dates, the WindowsApps folder, the PATH parser, the file head, the share test, the recent constants | C5, C6, C9, C10, C18, C20, C21 | done, 680ff97 |
| 10 | The free-name loops onto `ItemFiles.FreePath` | C4 | done, 16b84a0 |
| 11 | `TextSniff` reads the editors' types from `Dispatch` | C15 | done, 680ff97 (with W9) |
| 12 | `HotkeySettings` in `AxKit.Core`; `HotkeyForm` takes what axStart and axClip wrote twice | C3, F11 | done, f5890a7 |
| 13 | `ChoiceItems` for the one-checked menus | F6 | done, 59aeeb0 |
| 14 | `AskText` on the base, the six same-behaving copies folded | F1 | done, b9bd4c5 (six of eight; axFile's two stay, section 11) |
| 15 | `ListViews` for axGIT and axProcess | F7 | left: the two flows share a shape but differ in their loads, fills and first-read words; the shared piece would be hooks for fifteen lines |
| 16 | The console windows' folder and session frame on `ConsoleForm` | F10 | left: medium risk and both console plans to hear again; a session of its own |
| 17 | One Claude Code locator | C1 | left for the user (section 11, item 7) |
| 18 | `ShellLauncher` through `SystemPrograms` (PowerShell), its `FindOnPath` gone | C8 | done, 915fdc0 (ComSpec left, section 11) |
| 19 | `AxKitList.Fill` (S-54 reaches two more lists) | F12 | done, 339c04c (four of five; the hotkey windows' matches list is a plain list box by design, AS-D2) |
| 20 | Copy report through `CopyText` | F9 | done, 72ecdf4 |
| 21 | First focus in the base's `OnShown` | F15 | left: the automation channel's start order was not checked in time |
| 22 | `Chime` on `Notice` in place of axClaude's flag | 6 | done, 9399ee9 |
| 23 | One-line reasons on the catches and the posts that lack one | 6 | done, e249e49 |
| 24 | S-54 to S-57 with their tests; the audit's hand-over sentence | 7 | done, 1b501ab, 1f522c8, de78fe3 (S-54 to S-56; S-57 with W19) |
| 25 | Notes back to the code; the eight unnamed files named; the history stamps that only repeat the changelog | 8 | done, 2cdb06b, e5c61a4, 13319b1 |
| 26 | Specs back to the code | 8 | done, 2cdb06b, e5c61a4, 13319b1 |
| 27 | Scripts: the stale comments, `release.ps1`'s prefixes, the `$PSScriptRoot` note narrowed | 8 | done, 2cdb06b, e5c61a4, 13319b1 |
| 28 | A test that pins the notes' identifiers to the code | 8 | left: the sweep's scan is the shape of the test, with an allow list of .NET names as its cost |
| 29 | The cheap tests: the three settings round trips, `UserSecret`, `Signatures`, a `PtyHost` smoke, `GitNotFoundException` | 10 | done, 87b6250 |
| 30 | `MatchSpeaker` shared by `HotkeyForm` and the palette | F18 | left |
## 10. Tests

1,125 tests at the baseline. Of the seventy-three Core types named in no test file, forty-four are exercised through
a tested method, seventeen are thin wrappers over Windows or the runtime (not worth a unit test), and twelve are pure
and untested; of those the cheap and worthwhile ones are W29. The checks that read the sources (`ListTests`,
`MenuTests`, `KeyTests`, `InventoryTests`, `LayoutTests`, `PrivacyTests`, `ReleaseWorkflowTests`) pin the rules of
the notes; none pins a note's identifiers to the code (W28).

## 11. Needs the user

Cleanups and fixes that would change what is heard or done, decided with the user on 2026-11-09; the answer ends each line:

1. axFile's New folder and Add a program keep their notice open on a refused name (as `AskText` does elsewhere) instead
   of closing and announcing the problem (F1). Decided: yes. Built in 3c10c1b (branch improvements).
2. "Folder missing" or "File missing" in every Show-in window on a gone path, as axClaude says today (F8). Decided: yes. Built in 9c85afa.
3. The F1 texts' Notices section the same in every window: five texts gain or lengthen it (F14). Decided: yes. Built in 97ede32.
4. The focus after a Windows dialog put back by one rule; the editor's Open (only on OK) and Save as (only on cancel)
   become alike (F19). Decided: yes, the focus always back where it was. Built in 461003e.
5. axTerminal's closing question: "Keep running" calls `KeepWorkingAfterUpdate` as axClaude's does, so a pending update
   restart is dropped and "Update installed" is said (F21; a fault today). Decided: yes. Built in f336e22.
6. Ctrl+Tab, Ctrl+Shift+Tab and F6 in axTask and axPass as in every other window (F22; AX-8 says they are bundle-wide). Decided: yes. Built in 17c916f.
7. The AI tools registration finds an npm-only Claude Code, as axClaude does (C1). Decided: yes. Built in ac8b12a.
8. cmd for axTerminal from `SystemPrograms.Cmd` alone, no longer from `ComSpec` (C8; S-1's intent against a user's
   deliberate `ComSpec`). Decided: yes, System32 alone. Built in 1f5d17e.
9. `list.json`'s template keeps the values given (C13): a new item in a GitHub list starts as "unlabelled" and "open";
   the hand-written `list.json` of the Reports list goes. Decided: yes. Built in e0b7df4.
10. S-1's `NoDefaultCurrentDirectoryInExePath` set for the process (children inherit it), or the audit amended. Decided: the switch set. Built in 55523df.
11. A `list.json` `open` action that would start claude or a terminal on a shared folder refused, as the MCP tool does. Decided, narrowed: an action starts either app on a folder of this computer, since the user presses the key, and refuses a folder off it (a share or a mapped drive, S-5's rule), spoken. Built in e2386ff.
12. A cap on a zip's entries in axPreview, "and N more" after it. Decided: yes. Built in 00fc490.
13. axTerminal's `_asks` poll as a one-shot quiet timer (AT-5.4 heard again). Decided: no, left to the assistant and left as it is: the half-second tick is a few comparisons and one row read, and a one-shot timer would have to be restarted on every output batch; the simpler piece is the robust one.
14. Delete the branch `cleanup` on GitHub (section 0). Done by the user on 2026-11-09.

## 12. NVDA sections worth hearing again

Nothing a user hears was meant to change, and the tests say the words are the same; these are the steps of the test
plans that a moved piece now runs, for one pass by ear before the merge (`nvda-pass.ps1 <app>` runs each app's
scripts; the rest by hand):

- Every window's title as it opens and changes (`SetTitle`): axGIT ("No repository", the repository's name), axFile
  (the folder's name), axMedia (the file playing), axProcess, axCalc, the editors (the document's name with the
  unsaved star), axTask and axPass, axClaude (the folder's name and path).
- The yes-or-cancel questions (`Notice.Ask`): axCalc's Clear the history and Clear the variables, axClip's Clear the
  clipboard history, axFile's Delete, axGIT's Discard, axProcess's End process, axPass's Delete for good and Move the
  group (Enter is Cancel there), axClaude's Close and Force Claude to restart, axTerminal's close, restart and switch.
- The one-field notices (`AskText`): axGIT's Commit and New branch, axFile's Filter, axMedia's Go to time (a wrong
  time keeps the notice), axTask's GitHub account (a hidden field).
- The one-checked menus (`ChoiceItems`): axFile's Sort by, axProcess's Sort processes by, axClaude's Speak replies,
  axTerminal's Speak output, axPass's Lock after, axCalc's Angles.
- The lists filled anew (`AxKitList.Fill`): axFile's folder list on a folder change and a filter, the navigator of
  axTask and axPass on every level, axPreview's Details list, the views of axGIT and axProcess with the selection
  kept across a refresh.
- axStart and axClip: the hotkey registered at start, Options (Hotkey..., Start with Windows, Say the first match while
  typing), the Hide and Quit items.
- axClaude: an information screen's notice (no chime).
- axCalc, axMedia, axProcess: Ctrl+F, F3 and Shift+F3 in the list.
- Copy report on a held clipboard says the base's failure notice, where it was a crash.

## 13. What to do next

(Written at the end of the run and mirrored in the priorities list.)
