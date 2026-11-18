# Automated checks of the apps

The user's direction, 2026-10-23: the assistant drives every axKit app itself, with the keyboard events a user
presses, and reads back what the app and NVDA say, so that the NVDA test plans run as scripts, before releases, at
major moments and as spot checks during development. The user's own passes become sanity checks and experience
design. A workflow may change by choice; a script that fails says what else changed with it.

Three tiers, each usable alone.

## 1. Claude sessions without windows: `tools/QaRun`

Built 2026-10-23. Several Claude Code sessions at once through the app's console pieces (`PtyHost`,
`ClaudeLauncher`, `SessionModel`), each in a fresh folder under `%TEMP%\axkit-qa`: the trust prompt, a message, its
reply, `/exit`. The model is chosen on Claude's command line (`--model haiku`, as `axclaude <folder> -- --model
haiku`), never with `/model`, which saves the choice as the user's default for every session. It found on its first
day that Claude drops an early Enter (axClaude FR-2.1).

## 2. The windows driven from inside: the automation channel

Built 2026-10-23: `AxKit.Forms/AutomationChannel` (started by the window base when it first shows), `AXKIT_HOME` in
`AppPaths`, `tools/AxAutomate` (axAutomate; called AxKitDrive until 2026-10-24) with the test desktop, and the first scripts in `tests/scripts` (axDown's notices and
palette, axClaude's message and its set of two questions), all passing.

Run one:

```
dotnet run --project tools\AxAutomate -c Release -- tests\scripts --build --report report.md
```

The run names the program it drives: `--build` builds the sources as they are into the run's own folder, `--exe
<AxKit.exe>` names a built one (to run several folders against one build, build once with `dotnet build
src\AxKit\AxKit.csproj -c Release -o <folder>` and give `--exe <folder>\AxKit.exe`). There is no default: until
2026-11-13 a run with neither drove whatever lay in `src\AxKit\bin\Release`, and a pass or a failure against a stale
build read like a real one. The console and the report's first lines say the program's version and when it was
built. Exit code 0 when every script passed and nothing is stale, 1 when one failed (also by a `# Stable` section
whose trace differs from the kept one; below, "The traces"), 2 when the run could not start,
3 when every script passed and the command inventory is stale (below, "The command inventory"). One window run
plays at a time on a computer: a second run waits for the first, for an hour at most, and says once that it waits,
since the clipboard and the system hotkeys are shared (`tools/AxAutomate/RunLock.cs`, `user-workflows.md` WF-D-29);
the builds before the scripts run side by side. A window that does
not answer a step within 30 seconds fails the step, and the run goes on to its end. The run's folder is deleted at
the end unless `--keep` is given; the windows' logs of a script that failed are copied first to
`%TEMP%\axkit-automate-failed\<run>\<script>`, and the summary's last lines say where, so that a failure seen once
leaves its exception to read (the newest ten runs stay; `AxAutomate.Core/FailedLogs.cs`).

**Where these checks run.** On the developer's computer, by hand or by the assistant: `tools/AxAutomate` for one
script or folder, `nvda-pass.ps1` for all of them with NVDA listening. Never in CI: `build.yml` builds the solution,
runs the unit tests and checks the generated pages, and that is all it proves. A workflow of
`docs/axKit/workflows.md` is kept working by the runs recorded in `docs/axKit/workflow-record.json`, so a change to a
window is followed by its scripts before it is pushed, and a release by a full pass.

**The workflow record.** A run given `--record` enters every goal and check it played into
`docs/axKit/workflow-record.json` after its last script (user-workflows.md WF-6): for the run's tier (`desktop`,
`nvda` under `--nvda`, `ai-tools` under `--ai-tools`) the last result with the version, the commit (a `+` after it when `src` or `tests` held a
change not committed) and the date, and the last pass. `nvda-pass.ps1` always records. On the desktop of their own,
the run of an app's folder after a change to a window is given `--record`; a run of one script during work is not. A
recorded run changes that one file and no page, and the file is committed with the change the run checked, or alone
with the line `Item: none, the workflow record of the <tier> run of <date>`. An entry whose label or title no script
has any more is dropped by the next recorded run. What a pass found stays in `docs/axKit/nvda-test-record.md`.

**The converter.** A run given `--convert` moves a script's command keys to `do` lines (user-workflows.md WF-10), on
the desktop of their own alone. Each script is played with the window asked for its commands before every press of a
`keys` line that carries Ctrl or Alt or is a function key (Alt+F4 aside, which tells the run that the end is meant). A
press becomes `do <name>` only when exactly one row of the window's list at that moment has it as its key and would run
by it now, and `do` with that name would press that very key; the name is the command's canonical one. Everything else
stays `keys`: plain letters, the arrows, Enter, Escape, Tab, a chord that is only a further key of its command, a chord
pressed while a notice shows. A `keys` line of several presses is written as several lines in the same order, an
`iffound` before it on each; comments, blank lines and line ends stay. A script that did not pass is left alone. A
rewritten script is played once more as it then stands, and when it does not pass the file is put back byte for byte and
named after the summary. The rows the `do` lines ran are merged into the command inventory, written at the run's end
when it changed. Each script gets one line (`04-find.txt: 3 presses converted`, `no press to convert`, `left as it is:
it did not pass`, `put back as it was: ...`), and the run's last lines count them. The mode compares and writes no
trace, and is refused with `--nvda`, with `--inventory write` or `rewrite`, and without a repository. An app's folder
is converted whole: `tests\scripts\<app> --build --convert`, then the same folder without the option and with
`--record`, then `tools\workflows.ps1`, and the scripts, the inventory, the pages and the record go into one commit.

**The AI tools mode.** A run given `--ai-tools` runs the scripts' `do` steps through the AI tools (user-workflows.md
WF-9), on the desktop of their own alone, with the runner as the MCP client it is in a script's own `mcp` steps. It
plays only a script of one window (one `start`; no `launch`, `adopt` or `use`), of an app the AI tools read (not
axPass, not axClip), with a `do` step and no `mcp` step; one line at the start says how many scripts were passed over,
and the keyboard run is where those are checked. Right before such a script's first input step after its `start`, the
access is turned on as a user turns it on, by twenty steps that are lines of `tests/scripts/mcp/02-run-commands.txt`
(`AiToolsMode.Preamble`; a test holds them to that script): Let AI tools read and open axKit in the menu, the pairing
answered in the window's notice with its check box and Allow, Let AI tools run commands. The report shows them once,
each marked `AI tools:`; what they made the window say is taken, and what the script's own expectations had yet to
take stays. Nothing is written into a settings file and no gate of `mcp.md` is passed by: the run's broker reads the
run's own home folder and key and lists only the run's own windows (MC-6.2). Then every `do` step whose command the
tools may run without asking (a menu command that is for them and is not marked act, while no notice shows) is run
with `axkit_window_run` by its label and menus, and the step waits for the tool's answer; its detail is
`AI tools: <label>`. A refusal fails the step with the tool's words, but the one for a user at work ("The user is
working in this window right now", which counts the whole session's last key press, the user's own in his own windows
included): the call is sent again 3.5 seconds later, three times at most. Every other step plays by the keyboard as
written. A section's line of the report ends `, through the AI tools: 3 of 5 inputs`; with `--record` only the
sections with a step that went through the tools are entered, for the tier `ai-tools`, and nothing for `desktop`,
since the windows are in another state. A step that fails only in this mode is the tier's finding: it is recorded and
written as an item of the priorities list, and no script and no window is changed to make the mode pass. The mode
compares and writes no trace, and is refused with `--nvda` and with `--convert`.

The steps, the run on its own desktop and the report are the engine in `src/AxAutomate.Core` (`ScriptRunner`, `Run`,
`Window`; moved there on 2026-11-11, docs/axAutomate/SPEC.md AU-5.1), which axAutomate's window plays workflows
through as well; `tools/AxAutomate` is the command line over it, with the same arguments and output. It runs a
`.workflow.json` file as well as a `.txt` script.

For the windows' own behaviour: keys, focus, announcements, notices, menus, the control area. Runs any time,
including while the user works, because nothing takes the foreground.

- **On only for a test run, and only for the runner** (security audit S-11, 2026-10-24). The runner makes a secret of
  256 random bits for each run and gives it to the windows it starts as `AXKIT_AUTOMATION`; a window opens its channel
  only with a secret of 32 characters or more, and the client's first line must be that secret (compared in constant
  time), or the client is let go. `AxKit.Core/AutomationEnvironment` reads the variables once, as the process starts,
  and takes them out of its environment: a shell in axTerminal, Claude, git or the user's own programs never inherit
  them; only a new axKit window of the same run gets them (`WindowMenu.StartWith`). Without a secret, no pipe exists and
  nothing is recorded. A window in a test run is shown without activation, so it never takes the keyboard from the
  user, and focus moves inside it with the thread's own active window.
- **Its own files.** The runner sets `AXKIT_HOME` to a folder of the run's, under which the windows keep their
  settings, their logs and the recently closed list (`AppPaths`), so that a run neither reads nor writes the user's;
  the folder counts only together with the run's secret. The background apps' one-at-a-time mutex carries a mark of
  that folder (`AppPaths.OneAtATime`), so that a run's axStart, axClip or axHelp never takes the user's
  own, running from sign-in, for a second start and ends at once (which every axDescribe script did on 2026-11-09
  while the user's axDescribe ran; axDescribe is part of axHelp since 5.7.0).
- **Only the run's own windows** (2026-11-15). A window of a run lists, messages and closes only windows of the same
  run: the Window menu's Open windows, Close all windows, the hand-over to an app's open window (axMedia, axPreview)
  or to a background app's hidden one (axStart, axClip, axHelp and its conversation), and the AI tools' order of
  windows. Every process of a run holds a named mutex of Windows for as long as it lives,
  `Local\AxKit.Run.<the run's mark>.<process id>` (`AxKit.Core/RunMembership`, taken in `Program.Main` before any
  window exists), and the one place that finds other processes' windows (`AxKit.Forms/BundleWindows`) leaves out, in
  a run, every process whose name it cannot open. A named object because it is the plainest mark that holds for
  every window: no right beyond the user's own (reading another process's environment needs more), no file of
  process ids to keep current, nothing for each window to set (a window property would miss hidden and message-only
  windows), and Windows removes it when the process ends. It fails safe: in a run, a window without the mark is never
  listed, messaged or closed, and a hand-over from a process without it is refused by the window it reaches. Outside a
  run nothing is asked and nothing changes. On the run's own desktop the windows were the run's anyway; the rule is
  for the NVDA tier, on the user's desktop, where `axClaude/13-axkit-menu`'s Close all windows posted the close
  command to the user's own axClaude window (it only asked "Close axClaude?" because Claude was working; an idle
  window would have closed). `WindowReachTests` fails when another file of `src` finds windows or processes by name
  itself. Not covered, by choice: axProcess lists every process of the computer, as it is for (its scripts end only
  what they started), and a window that hides gives the foreground back to the window that had it.
- **Its own start-up places** (2026-11-14, docs/axProcess/SPEC.md PR-5.8). axProcess's Startup view turns entries off
  and removes them, so in a test run it reads and changes a stand-in and never what starts with Windows on the
  computer: the registry key `HKEY_CURRENT_USER\Software\AxKit\Runs\<the run's mark>\Startup` (`AppPaths.RunMark`) and
  the folders `AxKit\startup\yours` and `everyone` under the run's folder (`AxProcess.Core/StartupPlaces`). A script
  seeds it (a `file` step for a folder entry, a PowerShell `run` step for the registry) and deletes the key at its
  end. A view of a run therefore lists the stand-in's entries and no other.
- **One named pipe per window**, `\\.\pipe\axkit-auto-<process id>`, for the current user alone and refused to
  clients over the network (its access list); the runner connects with `CurrentUserOnly`, so a pipe another program
  made under the name is refused. A pipe already taken ends the channel, never the window. One command per line, one
  answer per line (JSON).
- **Secrets never leave**: a password field reads "protected" in `focus`, `text` and the timeline, as NVDA says it
  (S-12).
- **The timeline** (2026-10-23, the user's ask for full diagnostics): every window records, with the milliseconds
  since it opened, each announcement (with whether it interrupts), each sound by name (`Sounds.Played`; in a test
  run the sounds are recorded and not played, so a run is silent on the user's speakers), each move of the focus to
  another control with its name, role and value, and each change of the focused control's value (the caret's line,
  the selected item), which is what NVDA reads by itself. The runner's report lists it whole, and an expectation can
  wait for any of it.
- **The run's own checks**: after the last step a run fails when a window's process ended by itself or a log of the
  run holds an ERROR line, so a crash or a logged exception never passes unseen.
- **Nothing of the run left running** (2026-11-11): after its windows close, the runner ends every other process of
  the run's own `AxKit.exe` that it made, such as a window that a window of the run started and the script never
  adopted (or whose adopt failed), with its process tree, and says so in the report ("after the steps: ended process
  ..."), without failing the script. A process is the run's only when its program file is the run's exe by its full
  path, it started at or after the runner, and it descends from the runner (read before the windows close and again
  after) or has a window on the run's own desktop; each number is checked against the start time read when it was
  found, so a number reused meanwhile is never ended, and nothing is chosen by its name alone, so the user's own axKit
  is never touched. Under `--nvda` there is no desktop of the run's own and only the descent counts: a stray started
  through the shell, whose parent can be another process than its starter, is not found there. A window left behind once
  stayed in front in the NVDA tier and the four scripts after it failed at their first key (2026-11-09).
- **The user's clipboard kept** (2026-11-14): the run's desktop shares the clipboard with the user's session, and a
  few scripts write it. The command line (`tools/AxAutomate`, with `ClipboardText` of `src/AxAutomate.Core`) reads the
  clipboard's text before its first script and puts it back after its last, also when a script fails or the run stops
  on an error. A clipboard that held no text before the run has a script's text taken off it again, so that a test text is
  not what the user pastes next; a picture or files it held before cannot be kept, since a script's text replaced
  them. Not covered: a runner that is killed, what the user copies during the run (the text of
  before the run replaces it), Windows' own clipboard history and the user's running axClip, which both record the
  test texts as they pass, and a workflow played in axAutomate's window, which goes through the engine and not this
  command line.
- **Commands**:
  - `keys <keys>`: key presses as a user makes them, `ctrl+shift+p`, `escape`, `down`, posted to the focused
    control through the window's own message queue with the modifiers set for the window's thread, so that
    `ProcessCmdKey`, the menus' shortcuts and the controls see them as typed. A key's name is one of the chord
    vocabulary's (`AxKit.Core/KeyChord`, `user-workflows.md` WF-2.2): a letter or a digit, `enter`, `escape` or
    `esc`, `tab`, `space`, `backspace`, `delete`, `insert`, `up`, `down`, `left`, `right`, `home`, `end`, `pageup`,
    `pagedown`, `plus`, `minus`, `period`, `comma`, `apps`, `f1` to `f12`, `slash`, `semicolon`, and `add` and
    `subtract` for the numeric keypad's plus and minus; any other name is an error of the step.
  - `type <text>`: characters into the focused control, as typed.
  - `focus`: the focused control's accessible name and role, its text or the selected item, the caret's line.
  - `said`: what the window announced since the last `said`, each notification with its kind (interrupting or not),
    in order; the announcements are recorded where they are raised (`AxKitForm.Announce`).
  - `state`: the title, the status bar's texts, whether a notice shows and its title, the control area's name, and
    `visible`, false for a window hidden by its own close (axClaude FR-9.6), which `activate` leaves hidden.
  - `menu <name>`: runs a menu command by the name the command palette knows it by.
  - `commands`: the menus' commands as the command palette lists them, each a line with its menus, its key and
    "checked" (what `expect commands` reads).
  - `commands data` (2026-11-15, `user-workflows.md` WF-2.3 to WF-2.5 and section 5.2): the window's commands as
    data, one JSON object on one line: the app's name, the title of the notice the window shows or null, and a row
    per menu item the palette would list, then a row per label of the keys with no menu item. A row holds `label`,
    `menu` (the menus from the bar down; empty for a key alone), `key` (the command's key as a chord in the words
    `keys` takes, lower case, the modifiers in the order ctrl, shift, alt; empty for none), `also` (further chords of
    the same command: axDown's Redo is `ctrl+y`, also `ctrl+shift+z`), `keyText` (the key as the menu shows it),
    `mark` (read, open, change, act), `checked` (null for an item that is no check item), `keyOnly`, `ai` (whether
    the AI tools may see and run it: never a key alone, never a row of axPass or axClip) and `now`, the chord that
    would run the command if pressed at this moment, empty when none would (the focus is elsewhere, a notice
    shows). The key is the one its menu item was given where it was made (`CommandPalette.Keyed`), never read out of
    the text the menu shows: an item whose text describes a place ("Alt+F4", "Enter in the message field"), and the
    editor shell's Cut, Copy, Paste and Select all, whose keys are the edit control's own, have a `keyText` and no
    `key`. A window built before this request answers `commands data` with the palette's lines, which is no listing.
  - `choose <json>` (2026-11-15): runs one menu item, found by exactly its label and its menus as the bar stands
    now, `choose {"label": "Word wrap", "menu": "Options, Text"}`, where `menu` takes the first palette line that
    starts with a text. The item is clicked as the palette clicks it: a command says what it says (axDown's "Word
    wrap off"), and a setting that is a check item has its new state said as the palette says it ("Check for updates
    at start, checked"). The answer is `{"ok":true}`, or the error "no menu item <label> in <menus> now" (no such
    item, a disabled one, or a notice that holds the window).
  - `close`: closes the window as Alt+F4 does.
  - `problems`: what is wrong with the window's command list (app-kit AK-2.4): a key of its own the F1 text does not
    name, a command an earlier one shadows. Every app's first script runs it as `check commands`, after its start.
- **The AI tools**: the runner can be the MCP client itself (`mcp start`, `mcp call`, `expect mcp`, `refuse mcp`):
  `AxKit.exe mcp` as its child with the run's secret and folder, so that it reads the run's settings and key and never
  the user's. `tests/scripts/mcp` turns the access on, pairs the runner in the window's notice and reads the window
  (`docs/axKit/mcp.md` MC-6). It never opens an app through `axkit_open`, which would start it on the user's desktop.
- **Scripts**: `tools/AxAutomate` starts an app with the channel on and runs a script of steps (`mkdir`, `start`,
  `use`, `keys`, `type`, `menu`, `said`, `expect said|focus|state <text>`, `try said <text>` with `iffound <step>` for
  what may not come, such as Claude's trust question, `wait <ms>`, `close`; `file <path> <text>` writes a file, its
  path in quotes when it holds a space; the header of its `Program.cs` lists them),
  each step's result in a report; what a window said stays queued until an expectation takes it; a failing step prints what was said and focused instead. The NVDA test plans become scripts in
  `tests/scripts/<app>/`, one per section, with Claude always on `--model haiku`.
  - `do <name>` (2026-11-15, `user-workflows.md` WF-4): runs a command of the current window by its name, the way a
    user runs it. The runner asks the window for its commands (`commands data`), finds the one command the name gives
    and presses the chord the window says would run it at this moment (the row's `now`), through the same key path as
    `keys`: `ProcessCmdKey`, the key's own condition and, with NVDA, NVDA's handling of the key are what is checked.
    The step's detail says what was pressed: `do Find  -> key ctrl+f`. The name is the command's label ("Find"), or,
    where two commands of the window share a label, the label with its menus as the palette lists them ("Heading,
    Navigation, Next"); it is compared whole, without regard to case, by the one resolver (`AxKit.Core/CommandNames`),
    never as the start of a name. The key is the window's own, read at the moment of the step, so a key changed in a
    key table changes no script. A command with no key as data (none at all, or one its menu item only shows as a
    text) runs by its menu item through `choose`, and the detail says `menu item`. The step fails, with nothing
    pressed: when the command has keys and none acts where the focus is ("the key of Find next (F3) does not act
    where the focus is: " and the focused control's name); when the window shows a notice ("the window shows the
    notice Find: its commands wait"; a notice's own keys are `keys` steps); when the name is unknown or ambiguous,
    with the nearest commands or what to write named; and against an AxKit.exe from before `commands data`. It is not
    for a command that ends the window (`close` and `keys alt+f4` tell the run that the end is meant), nor for a
    control's own keys (`keys`, `type`) or a system-wide hotkey. `menu <name>` is as it was: the first palette line
    that starts with the text.
  - Goals and checks (2026-11-15, `user-workflows.md` WF-3): a comment line `# Goal: <title>` starts a goal, what a
    user gets done by the steps under it, and `# Check: <title>` a check, steps that only test (a refusal, a wrong
    input, a state). Each runs to the next such line or to the script's end; the lines before the first are the
    setup. A failing step still stops its script, and the run says for each section whether it passed, failed (with
    the line) or was not run: `-- Goal: <title>` or `-- Check: <title>` before a section's first step, and after the
    last step `Goals: 2 passed, 1 failed, 4 not run` with one line per section; the run's last lines add the counts
    over all scripts. A script without such a line is one goal, titled by its `# Workflow:` label, and prints as
    before. `tests/scripts/axDown/04-find-and-go-to-line.txt` is the first script written this way: two goals and
    five checks, its commands run by `do`.
  - What a run keeps of the windows' commands (WF-2.7): the current window's whole listing at every
    `check commands` step, and the one row its name came to at every `do` step (`ScriptResult.Commands`), held
    against the command inventory (below).
  - `# Stable` (2026-11-15, WF-3.5): a comment line of that one word among the comment lines that begin with a goal
    or check line marks a section whose trace is kept in the repository and must not change ("The traces", below).

### The command inventory (2026-11-15)

`docs/axKit/window-commands.json` holds, by app, every command the windows list as data (`commands data`): its label,
its menus, its key and further keys, the key as the menu shows it, its mark, whether it is a key with no menu item,
and whether the AI tools may run it; never what is state (checked, which key acts now). It is written from live
windows by the runner and never by hand (`user-workflows.md` WF-2.6 to WF-2.11, format in section 5.3), and the
reference pages, axHelp and the cheap check read it. The rows come from the snapshots a run takes at every
`check commands` and `do` step (every app's first script has a `check commands` after its start; a script places a
further one where a window offers commands its start does not). Never in it (WF-2.8, `CommandInventory.Kept`): a
numbered entry of a list (recent files, open windows), a row that names the run's folder or the profile folder, a
row whose label holds axKit's version ("Check for updates (axKit 5.9.1)", stale with every release), and a row whose
label is the name of something of the computer the run was made on (a program, a device, a folder, a shell): each
such row found gets a rule of its own there, and whoever writes the file reads it whole first. The first inventory
found two: axTerminal's Session, Switch shell (the shells this computer has, and the user's own) and its Session,
Install PowerShell 7 and Install Git, for Git Bash (shown only where that shell is missing).

The option `--inventory` of the runner says what a run does with it:

- `check`, the default whenever the runner finds the repository (the folder above the program that holds
  `AxKit.sln`, or `--repository <folder>`): every snapshot is held against the file. A row a window lists that the
  file does not hold as it is (a label, menus, key, further keys, key text, mark, key alone or AI flag that differs)
  is named after the run's summary, each once, under "The command inventory is stale (docs/axKit/window-commands.json):
  ...", and the exit code is 3 when every script passed; a failed script's 1 stands. An app the file holds no row
  for is not checked, and the run says so once ("The command inventory has no rows for axClaude yet: its commands
  were not checked"): that is how an app joins later. Every app of the bundle has its rows: axClaude and axHelp, left
  out of the first inventory while their windows were being redesigned (`user-workflows.md` WF-D-22), joined on
  2026-11-16.
- `write`: every snapshot merged in (a row of the same label and menus replaced, other rows kept), the file written at
  the end when it changed, and one line saying how many rows were added, replaced and removed. Only the scripts with
  a `check commands` or a `do` step play; a line says how many were passed over.
- `rewrite`: as `write`, and an app is written with only the rows this run saw when it is whole: every script that
  gave a snapshot of it passed, and one of those snapshots was taken with no notice showing. It is how a command that
  left a window leaves the file; run it over a whole folder of `tests/scripts` or over all of it.
- `off`: nothing. For a run against a build that is not the repository's, and for `tools\upstream.ps1 -Verify`.
  A runner built outside the repository (`nvda-pass.ps1` builds it into `%TEMP%`) finds none and checks nothing.

When a run says the inventory is stale: if you changed a menu, a key or a label, run the same scripts with
`--inventory write`, then `tools\workflows.ps1`, and commit the file with the change that made it differ; if you did
not, the window changed by itself, which is the finding. The cheap check, in `test.ps1` and CI without a window
(`tests/AxAutomate.Tests/RepositoryWorkflowTests.cs`): the file is well formed and written as the writer writes it,
holds no row the inventory never keeps and no account path, and every `do` step of every script names a command of
it under the app of the window the step reaches (a step whose app has no rows yet is passed over). That the file is
current is the window runs' check, not this one.

### The traces (2026-11-15)

A trace is what one section of one run came to, without times (`user-workflows.md` WF-5, format in section 5.4): each
input step as a line (`do Find = ctrl+f` with the key the window gave, `type apple`, `keys enter`, the steps that
open, choose and close windows) and, under it and indented by two spaces, everything the run's windows recorded up to
the next input: announcements, sounds, focus moves, value changes, as the report's timeline writes them. The steps
that prepare and the steps that only look are no lines; what they waited for is. Every line is written with the run's
folder as `{temp}` and the profile folder as `{home}`, then redacted (`Traces.Clean`). The setup is a section of its
own.

A trace is kept in the repository only for a section marked `# Stable`, in
`tests/traces/<folder>/<script>.trace.txt`, the folder and the name those of the script under `tests/scripts`; the
file is written by the runner and never by hand. A kept trace must not change: there is no difference that is only a
note. The engine collects a trace only where one is used: for a script with a `# Stable` section, and, in a run
given both `--report` and `--traces check` or `write` by name, for every script, whose report then holds each
section's trace under the script's steps. Collecting asks every window once more after each line for what it
recorded and changes nothing else of a run; that one request more can still shift a script that races a timer of its
window (axDown's script 04 does, at its `expect state` after Find: on 2026-11-15 it failed there in 4 of 10 traced
plays and in 1 of 7 untraced ones, on a busy computer), which is why it is done only where asked for: a run that
names no `--traces` plays every script without a `# Stable` section exactly as it did before there were traces.

The option `--traces` of the runner:

- `check`, the default on the hidden desktop when the runner finds the repository: after a script whose steps
  passed, the trace of each `# Stable` section is held against its kept section, line by line. One that differs
  fails its section, and so its script, after the steps: "FAIL after the steps: the trace of the stable goal Find a
  word in the text differs at its line 2: kept ...; now ..." (the first line that differs; "check" for a check); one
  with no kept section fails the same way: "FAIL after the steps: the stable goal ... has no kept trace: run with
  --traces write". The sections' lines are then said once more as they stand, the failed one with "after the steps:
  its trace differs from the kept one at line 2". The run's last lines count them: `Traces: 2 stable sections equal,
  0 differ, 0 without a kept trace`. The exit code is 1, as for any failed script.
- `write`: after a script that passed, its file is written whole, the header and the traces of its `# Stable`
  sections in the script's order; a script with no `# Stable` section has no file, and one it had is deleted. A
  deliberate change is reviewed as the file's difference in git.
- `off`: nothing is collected, compared or written. Always so on the NVDA tier (`--nvda`; `check` and `write` are
  refused there) and for a runner that finds no repository; `tools\upstream.ps1 -Verify` runs with it.

The runner finds a script's kept file by the script's own path in the repository; a copy played from elsewhere is
the repository's script of the same `# Workflow:` label, and a script with no such label keeps and compares nothing.

What may be marked (WF-5.6): only a section in which the windows say axKit's own words and the script's own texts.
By rule (`Traces.MayKeep`), never a script of `tests/scripts/axClip` (the test desktop shares the clipboard with the
user's session), `axStart` (the programs installed), `axProcess` (the programs running), `axTerminal` and `axGIT` (a
shell's and git's output) or `axMedia` (the sound devices), none of `tests/nvda-scripts`, and none with a `# Needs:`
line. By reading: no section that shows axFile's drives or its Open with list, or anything else that is the
computer's or the user's. A mark is earned (WF-5.7): add the `# Stable` line, run the script once with
`--traces write`, read the kept file whole, then run it twice with the default; a section that differs in either
loses the line again, and the next `--traces write` removes its kept section, since an announcement and a focus
move can change places with a run's timing and such a section is not one to hold equal. Whoever marks a section
says in the commit message that the kept section was read. Which goals are kept stable is the user's to name
(WF-5.10). No section is marked yet, and `tests/traces` holds no file. The two goals of
`tests/scripts/axDown/04-find-and-go-to-line.txt` were marked on 2026-11-15 to prove the mechanism, their file was
written, and both lost the mark in the compare runs that followed, as WF-5.7 has it: a window puts a focus move and a
value change on its timeline from a watch that looks every 50 ms, and the script presses Enter right after
`type apple` and `type 5`, so the line `· apple` stood under `type apple` in one run, under `keys enter` in the next
and nowhere in a third. A section to hold equal waits for what each input caused before the next input
(`expect focus` with the value, `expect said`); a `type` step followed at once by the key that closes its field is
the case to look for before a section is marked.

The cheap check, in `test.ps1` and CI without a window (`RepositoryWorkflowTests`): every `# Stable` section has its
kept section, every kept section and every file of `tests/traces` belongs to a `# Stable` section of a script that
exists, and no script that may keep none holds a `# Stable` line; `PrivacyTests` reads the kept files as it reads
every file. That a kept trace is what a window says today is the window runs' check.

### What the scripts cover, and what stays manual (2026-10-23)

The NVDA test plans of axDown, axJSON, axPreview, axFile, axGIT, axTerminal, axMedia and axProcess (2026-10-24; the services only read, never started or stopped, since a run with administrator rights would do it for real) are scripts in
`tests/scripts/<app>`, one per section of the plan. Every app of the bundle has its folder there now
(`docs/axKit/apps.md` gives each one's count), axClaude among them, whose scripts with a live Claude Code run on
Haiku. Every step a script cannot drive is a `#` line naming the plan step and why. What stays manual, for now:

- The menu bar and context menus opened with Alt, F10 or Shift+F10: menu mode needs the foreground; `menu` runs the
  same command by its name. Submenus that fill as they open (Open recent, Recently closed, Open windows) are not in
  the palette's list either.
- The Windows pickers (Open, Save as, Font, the folder picker) and the shell's own windows (Open with, Properties,
  File Explorer, a default program).
- The clipboard: the test desktop shares it with the user's session, so copy and paste steps are not run. Two scripts
  write it on purpose, since the clipboard is what they check: axClip's `01-basics` (three test texts) and axPreview's
  `04-details-and-kinds` (one test picture, for Ctrl+Shift+V). The runner puts the user's text back after its last
  script ("The user's clipboard kept" above); a picture or files the user had there are not kept and stay replaced.
- Coming back to a window with Alt+Tab (the files changed on disk); F5 is used instead where the app has it.
- What only NVDA shows, and sound and video quality: tier 3 and the user's ear.

The first runs found, and the same day fixed: axGIT's commit while a stage ran, axTerminal's "Done" before a
command's last line, two undo steps for Format, axPreview's size in whole megabytes, axMedia's status after a pause or
a clear and Go to time closing on a wrong time, and in axClaude the banner shown twice and the refused Enter of
Claude's first seconds. They also found plan steps the apps had outgrown, corrected in the plans.

### The stand-in of Claude Code (2026-11-15)

For axClaude's scripts that follow a turn by Claude Code's own signals (`docs/axClaude/events-and-grounding.md`
EG-11.3, section 6.7, decision EG-D-17): a mode of the runner itself, `tools/AxAutomate/StandIn.cs`, whose header is
its manual. It plays a scenario as Claude Code would in the window's pseudo console: frames, the turn's marks (OSC 133
A, C, D) and bells on the console, entries appended to a session file, and the command hooks of the `--settings` file
the window gave it, each started without a shell or a window with the event on its standard input, not waited for
when the hook says `async`. It needs no Claude Code, no network and no model, and never reads standard input.

- **`AXKIT_STANDIN`.** The runner puts its own program's path into this variable as it starts (`Program.Main`), so
  every window of the run, and the cmd.exe and the stand-in under them, inherit it. A script's `claude.cmd` is one
  line: `@"%AXKIT_STANDIN%" standin "%~dp0scenario.json" %*`, started as the batch stand-ins are, `start claude
  {temp}\<folder> --claude {temp}\claude.cmd`. With `--version` first the stand-in prints the scenario's version and
  ends, which is what the window's version gate reads. Only the command line sets the variable: a stand-in script
  says in its `# Needs:` line that it needs `tools/AxAutomate`, and does not run in axAutomate's window.
- **The scenario**: a JSON file the script writes with its `file` step, beside `claude.cmd`. That step turns `\n` into
  a line end and `\uXXXX` into its character, so no string in it holds an escape: rows and lines are arrays of strings.
  It names the `version`, the `session` id and the `home` folder, and holds `steps`: `frame` (rows, `clear`), `mark`,
  `bell`, `hook` (the event's name, `fields`, `last`, `tasks`), `user`, `assistant` (`cut`), `toolUse`, `toolResult`,
  `turnEnd`, `queue`, `raw`, `wait`, `waitFile` (a file the script makes in the working folder: the moment Claude
  would take the message up) and `idle`. `tests/scripts/axClaude/29-stand-in-turn.txt` plays one whole turn.
- **The session file** is `<the scenario's folder>\home\.claude\projects\<the working folder's project name>\<session>.jsonl`:
  with `home` the run's own folder (`AXKIT_HOME`, `{temp}\home`), whose `.claude\projects` axClaude counts as a
  projects folder in a test run alone (EG-3.1), so nothing of a run lands among the user's sessions. A script reads it
  with a `run` step (`findstr /s /c:"turn_duration" home\.claude\projects\*.jsonl`).
- **A session with a history**: `run "%AXKIT_STANDIN%" standin --prepare scenario.json <folder>` before the start
  appends the scenario's `history` steps to that folder's session file and ends, so that a window starts on turns
  already recorded, as `--continue` does.
- **What it did**: `standin.log` beside the scenario, kept with the run's folder by `--keep`: each step's kind, each
  hook's program and how it ended, never a text. A scenario it cannot play ends it with exit code 2 and one line,
  `stand-in: ...`, drawn on the window's screen.
- The batch stand-ins of the earlier scripts (22, 27 and the others) stay as they are: they draw no marks and send no
  events, so they are the test of the window that has only the screen.
- **The stand-in of the stream** (2026-11-17, `docs/axClaude/stream-mode.md` SM-10.2; `tools/AxAutomate/StreamStandIn.cs`,
  whose header is its manual): for a window the stream feeds, the stand-in is a process of lines, as Claude Code is
  with its stream options. A script's `claude.cmd` is `@"%AXKIT_STANDIN%" standin --stream <case> %*`, and the window
  is started with `--source stream`. It writes no scenario: `<case>` names a recording of the real stream under
  `tests/fixtures/streams` of the run's repository (a path to a `.jsonl` file is taken as it is), so a
  script follows what Claude Code 2.1.327 really wrote. The repository is the runner's own (`--repository`, or the
  folder above it that holds `AxKit.sln`), which the runner hands on in `AXKIT_STANDIN_REPOSITORY` as it hands its
  program on in `AXKIT_STANDIN`: the runner of an NVDA pass is built into `%TEMP%` and has no repository above itself
  (2026-11-18; the first NVDA pass of the stream scripts ended each of them with "Claude Code ended right after it
  was started"). The lines Claude Code wrote go to standard output in their
  order, 150 ms apart at most. Where the recording has something the host wrote (a message, a request, the end of
  the input), the stand-in waits until the window has written that kind of thing, so the window's own writing
  moves the play on: the script types a message, and the turn follows. The window's own text is put where Claude
  Code's line sends the message back, and the name the window gave a request where the recording answers it. A
  request the recording does not have, the first request of every start among them, is answered as Claude Code
  answers it. Where the recording's Claude Code asked its permission tool, the stand-in starts the one server of
  the `--mcp-config` file the window named, which is `AxKit.exe ask` of the run's own build, and asks it, as Claude
  Code does; so the question reaches the window's own pipe, and the script answers it in the control area. The
  lines that follow in the recording are those of the recorded answer, so a script answers as the recording did
  (`tool` was allowed, `denied` refused). A recording that ends with a code that is not 0 ends the stand-in there,
  as Claude Code ended. `stream-standin.log` in the working folder says what it did, kinds and numbers only, and
  what the window answered (`run findstr /c:"the window's answer was allow" <folder>\stream-standin.log`). Every
  such script first answers the folder's question (`expect said Start Claude Code in this folder?`, `wait 1500`,
  `keys tab`, `keys enter`), and waits 1500 ms before it answers a question the user did not cause, as a notice's
  keys are held that long. `tests/scripts/axClaude/37` to `43` are the scripts.

## 3. NVDA in the loop

For what only NVDA can show: what it reads on a focus change, whether a notification cut it off, the order of
speech. NVDA follows the foreground window and the real keyboard, so this tier takes the machine: it runs when the
user says so and steps away, never on its own.

The add-on was installed and its capture checked on 2026-10-23: NVDA's speech and its cancellations were
recorded, and the first capture found a quote marker NVDA was given in every question's name (fixed the same day).
NVDA refuses to be started from the assistant's process ("Access is denied"), so the user installs it: the folder
`tools/nvda-capture` copied to `%APPDATA%\nvda\addons\axkitCapture`, then NVDA restarted.

- **An NVDA add-on of the bundle's**, `tools/nvda-capture`, which records every speech sequence NVDA speaks to
  `%TEMP%\axkit-nvda-speech.log`, only while the file `%TEMP%\axkit-nvda-capture.on` exists, so that nothing the user
  hears in daily use is recorded. Installed once with the user's consent through NVDA's add-on store dialog.
- **The same scripts**, run with `--nvda`: the window is activated, the keys go through `SendInput` as a keyboard's
  do, and `expect spoken <text>` reads the capture file. A `do` step presses its command's key the same way, and a
  command with no key runs through the channel's `choose`, as `menu` does; the key's name is read from the same
  vocabulary as the channel's (`AxKit.Core/KeyChord`, since 2026-11-15), so a chord a window lists is one this tier
  can press. NVDA's own modifier (`nvda+1`) is this tier's alone.

Built 2026-10-23: `--nvda` in `tools/AxAutomate` (the windows on the user's desktop, brought to the front through the
channel's `activate`, keys and characters through `SendInput`, `expect spoken`, `refuse spoken` and `spoken` over the
capture log, the capture switched on for the run and its log deleted after it). The NVDA scripts live apart, in
`tests/nvda-scripts`, so that a run of `tests/scripts` never takes the machine:

```
dotnet run --project tools\AxAutomate -c Release -- tests\nvda-scripts --build --nvda --report nvda-report.md
```

### From a Claude session (2026-10-31)

The assistant runs this tier itself to hear a change as NVDA speaks it, in the same session as the change:

1. The add-on is installed and current: `%APPDATA%\nvda\addons\axkitCapture\globalPlugins\axkitCapture.py` the
   same lines as `tools/nvda-capture`'s (a changed add-on is copied by the user and NVDA restarted); `nvda-pass.ps1`
   compares them and stops when they differ. Nothing else of
   NVDA's is needed: not the Speech Viewer, not the user's own plugins in `%APPDATA%\nvda\globalPlugins` or the
   scratchpad.
2. Build `AxKit.exe` and the runner into scratch folders (never the running app's `bin`; `nvda-pass.ps1` does both),
   and write the steps into the app's script in `tests/scripts/<app>`, or into a script of `tests/nvda-scripts` for
   what only NVDA shows; a first run without `--nvda` on the test desktop finds the focus order and the texts without
   taking the machine.
3. Ask the user through the question tool to step away, saying how long. The answer is the go: the run is started at
   once. `nvda-pass.ps1` itself holds the rule: right before the runner takes the keyboard it asks Windows when the
   last key or mouse input was, waits up to a minute for 20 seconds without any, and refuses to start otherwise.
4. The run records "Running NVDA tests. Please do not touch the keyboard or mouse." in Windows' own voice (SAPI, not
   NVDA, so the capture holds only what the windows made NVDA say, and no window takes the focus) into a wave file and
   plays it in a loop for the whole run (the user's ask), then takes the foreground and the keys; when it is over the
   loop stops and Windows' voice says "NVDA tests finished. The keyboard is yours." Each line waits until NVDA has
   started nothing new for a few seconds (3 before, 4 after, at most 30): NVDA's audio ducking lowered Windows' voice
   when they overlapped, and NVDA read the closing windows after "finished" until the end waited (2026-10-31).
5. Read the result lines, filtered of account paths: each `expect spoken` prints what NVDA said up to the match,
   `[cancel]` where NVDA cut its speech (a key press does). The capture is on only during the run and its log is
   deleted after it.

The first such run, the consent reminder's check box (its steps now in `tests/scripts/axTerminal/07-consent.txt`), heard NVDA say
"checked" and "not checked" once each after the fix of AX-8.8.

### The NVDA pass of every app: `nvda-pass.ps1` (2026-10-31)

The NVDA test plans the user used to run by ear are run by the assistant: `.\nvda-pass.ps1 [<app>] [-NoBuild]` builds
into `%TEMP%\axkit-nvda`, gathers `tests/scripts/<app>` (every app without a name) and the scripts of
`tests/nvda-scripts` whose names start with the app's, and runs them with `--nvda`. Under `--nvda` every
`expect said` is an NVDA check as well: what the window announced must also be spoken by NVDA within 5 s, a focus
move by the control's name (NVDA says the role and the value in its own words), while a value and a sound are not
speech of the window's and pass as they are; the step's line ends with "|| NVDA: " and what NVDA said, and the run's
last lines name the scripts where NVDA did not speak what the window said, apart from the window's own failures.
"Spoken" means NVDA started the text: a key press cuts speech, and `[cancel]` in what NVDA said marks where. So the app scripts, written from the plans, are the plans as heard; a step only NVDA can show (a cancellation,
the order of speech, a state NVDA reads from the control) goes into `tests/nvda-scripts` as `<app>-<what>.txt` with
`expect spoken` and `refuse spoken`. The report is `%TEMP%\axkit-nvda\report.md`; the console shows the result lines.

A part of the pass again (2026-11-18): `-Only <pattern>, ...` plays only the scripts whose name, or folder and name,
match (`.\nvda-pass.ps1 -Only '*-stream-*', 'axMedia\03-playing.txt'`), so that what a whole pass failed is heard
again after its repair in minutes, and each run enters its goals in the workflow record as the whole pass does. And
`-Inventory write` hands the runner's `--inventory` mode on: the scripts of an app that are all live (axHelp's) list
its commands on no hidden desktop, so its rows of the command inventory are written by a pass
(`.\nvda-pass.ps1 axHelp -Only '01-start.txt' -Inventory write`, then `tools\workflows.ps1`) whenever a pass names
one of them as stale; the pass prints the rows it names.

Two things the first full pass taught (2026-10-31): a key is sent only while a window of the run is in front (the
runner brings the current one back, and fails the step rather than type into the user's own window, which the
foreground reached between scripts); and the navigation keys go with the extended flag and their scan codes, since
without it Home, End, the arrows and the page keys are the numeric keypad's, which NVDA keeps for its review cursor.
A `start`, `use` or `adopt` brings its window to the front and waits until NVDA has finished reading it: NVDA reads a
window that comes up whole, and ignores what a window behind another program's announces, so a script's second
window must be `use`d before its steps. `-Skip axClaude` leaves out the slowest app, whose replies need the network.

What a step that the window passed and NVDA failed ("|| NVDA did not speak it") has meant so far, in this order of
likelihood: two notifications raised back to back, of which NVDA drops the second (axTerminal's "Done" after the
command's last line, its "Ready" after a shell's banner; the fix is one notification, AX-7.3); an announcement made
before the window was activated, which NVDA ignores (axMedia's "No sound or video files" as it opened; since
2026-11-01 `AxKitForm.Announce` keeps it for the activation); a window behind another program's (the script's second
window, not `use`d). "NVDA spoke it only after N s" means NVDA was slow, not silent: the step fails, and the cause is
worth a look at what NVDA was reading. A failure with the keys at fault reads "no window of the run is in front" (another program came up and stayed, although the runner took the foreground twice). A
key the window never answered, with NVDA naming it in its input help (`keys nvda+1` before and after it) and no command
of its own, is taken by another program's hook or hotkey: Windows Magnifier keeps Ctrl+Alt+H, K, L and M while it
runs (2026-11-01); `Get-Process Magnify` tells, and a `RegisterHotKey` probe tells for registered hotkeys. A failed
`expect said` prints what NVDA said meanwhile, which is where such a program shows first.

How the keys and the matching are paced to NVDA (2026-11-01): a key goes as fingers press it, the modifiers a moment
before it and let go a moment after, and the next key waits until NVDA has started to say something for this one (at
most 400 ms), since NVDA takes the keys it has scripts for off the hook (Ctrl with an arrow, Home, End) and sends them
on itself, and one of two sent in the same instant was lost now and then. An `expect said` matches the sounds and the
speech as two streams, since a sound plays at once while its text may wait to be joined to another (AX-7.3); a text
later in the announcement the last expectation matched counts ("Sent. hello. Done" answers Sent, then Done); two
neighbours may come in either order, on the window's side (a Fetch and a Pull pressed at once say "Working…" and
"Fetched" in the order the keys' pace decides) and on NVDA's (a notice closing says "Not found" and then gives the
focus back; NVDA says the focus first), since what the last expectation took is looked at again.

What the tier guarantees on the user's desktop (2026-11-15, after a full pass that failed 120 of 123 scripts and
reached the user's own window):

- **No window outside the run is listed, messaged or closed** by a window of the run ("Only the run's own windows" in
  section 2), whatever a script asks: Close all windows closes the run's windows, a hand-over finds the run's window
  or none.
- **No key goes to a window outside the run**, with one exception. Before every key the runner looks at the
  foreground (`Run.InFront`, the rules in `AxAutomate.Core/Front`): with a window of the run in front the key is
  sent. Otherwise the current window is asked to activate itself, with the runner's leave, which is enough whenever
  Windows grants the runner the foreground. It does not when the desktop is in front as the pass starts, or the
  runner's terminal is behind another program: half a second later, and once more a second after that, the runner
  takes the foreground itself (`Native.TakeForeground`): Alt goes down, the runner sets the run's window in front,
  Alt goes up. That Alt going down is the one key that reaches a window outside the run (the desktop, or the user's
  window that came up between scripts); a window that only sees Alt go down opens no menu and loses the foreground at
  once. The Alt going up reaches the run's window, which saw none go down and so opens no menu; the runner asks it
  all the same (the channel's `activate` answers `menu`), and where its menu bar is active presses Escape, only
  while that window is in front, until it is not. After three seconds without a window of the run in front the step
  fails with "no window of the run is in front; the key was not sent", as before.
- **The run gives up at once when it cannot start.** When the first window of the whole run that shows cannot be
  brought to the front, the run stops there: one line, "The NVDA tier could not bring its window to the front: ..."
  with the program whose window stayed in front (never a title), then "STOPPED", no further script, exit code 2 (the
  run could not start), and Windows' voice says the keyboard is free. A locked session is the likely cause. Once a
  window of the run has been in front, a later failure of the kind is one step's and the pass goes on. A background
  app started hidden shows no window and stops nothing.

None of the foreground part can be run while the user is at the keyboard, so it is checked by its unit tests
(`FrontTests`) and by reading; the first pass after 2026-11-15 confirms it by its first script.

Side effects of the tier on the user's own session: axClip's script puts three test texts on the real clipboard (the
runner puts the user's text back after the last script), and axPass's leaves the copies to the pass by ear for the same reason; the windows of the run come
to the front of the user's desktop and go again.

When to run it: after a change to focus, keys, announcements or an edit control (the app's alone), and the whole pass
before a merge or a release, in place of asking the user for a pass by ear. A pass writes when each goal
last passed into `docs/axKit/workflow-record.json` by itself, and what it found is written into
`docs/axKit/nvda-test-record.md` as "run by the assistant through nvda-pass.ps1" with the commit. What stays the
user's: sound and video quality, the Start menu and Explorer (outside the windows), and how a change feels to use.

## 4. When an outside program changes: `tools/upstream.ps1` (2026-11-14)

The programs under the access layers update themselves, Claude Code sometimes daily. When one changes what it prints,
the place of axKit that reads it can break (the root `CLAUDE.md`, rule 12), and the user would be the first to notice,
in daily work. The upstream watch lets the assistant notice first. It has no side the user sees.

- **The record**, `upstream.json`: one entry per program (Claude Code, Git, KeePassXC's tool, Windows PowerShell,
  PowerShell 7, Command Prompt, Git Bash) with `places` (where it is looked for, in the app's own order; a bare file
  name is searched in the PATH's full folders), `arguments` (its version question), `verified` and `date` (the version
  the checks last passed with, and when; "not installed" for a program this computer lacks), `tests` (test projects)
  and `scripts` (folders or scripts of `tests/scripts`). Claude Code's entry also has `needs`, `signedIn` (the
  arguments that end with 0 when it is signed in) and `host`. A new access layer's program gets its entry;
  `UpstreamTests` fails while Claude Code, Git, KeePassXC's tool or a shell is missing.
- **At the start of every session**: `.\tools\upstream.ps1`. One line per program, the installed version and the
  verified one, "changed" where they differ; no build, a few seconds. Exit code 0 when none changed, 1 when one did.
- **When one changed**: `.\tools\upstream.ps1 -Verify` before other work (or `-Verify <id>` for one program, changed
  or not). Each check of the changed programs runs once: the test projects through `test.ps1 <App>`, the scripts
  through `tools\AxAutomate ... --build --inventory off --traces off` on the desktop of their own (the windows' menus
  and the kept traces are not the question here, the outside program is: the runner's exit codes stay 0, 1 and 2). Where all pass, the new version and the date are
  written into the record: commit it. Where one fails, the record keeps the program as it was, the last lines name the
  script and the failing step, and the exit code is 1: fix it in that program's one place (its launcher, its parser,
  its table of texts), with a new recording where the parser reads one (the `record-fixture` skill for Claude Code),
  and verify again. What the user hears and presses stays as it was.
- **When the checks cannot run** (the program is not installed, Claude Code is signed out, its host does not answer,
  the build failed), the output says so, nothing is written, and the exit code is 2. That is not a failure of a
  parser. Claude Code's scripts need the network, a signed-in Claude Code and a live model, and take about a quarter
  of an hour; a step that waits for the model's words can fail on the wording, so a failure there is read at its step
  before the parser is blamed.
- **One run, one version** (2026-11-17): Claude Code checks for updates as it starts and while it runs, and installed
  2.1.327 two minutes into a run of its checks that day; the scripts after that ran another program than the one whose
  version the run had asked, and the one step that failed had to be told apart from a change made the same hour. So
  the two tools that start Claude Code for a check, `tools\AxAutomate` and `tools\ClaudeProbe`, set Claude Code's own
  variable against background updates for what they start (`ClaudeCodeProgram.NoBackgroundUpdateVariable`; no app
  sets it, and the user's own sessions update as ever), and the watch asks the version again when a program's checks
  are over: where it still changed, a pass is not written (exit code 2: run it again), and a failure says that the
  failing step may be the new version's. To tell a new version's change from one's own: the first rows of a script's
  conversation name the version it ran; the older program files stay in the versions folder beside Claude Code's
  data, and the same recording script played with the old one and the new one shows what the new one draws.
- **`-Check`**, in `test.ps1` and `build.yml`: only that the record is well formed (every field, every named check
  exists). It never asks an installed version: CI's computer has other versions.
- **Claude Code's hooks** (2026-11-15, `docs/axClaude/events-and-grounding.md` EG-2.4, EG-11.5):
  `tests/scripts/axClaude/31-hooks-live.txt` is the check of the hooks' start for a new Claude Code, run by `-Verify`
  with the other scripts of that folder. It starts the installed Claude Code with axKit's own settings file and one
  turn, and fails when Claude Code asks its "Settings Warning" question (an event name it no longer knows), when the
  file was not passed, or when no event has arrived after the turn. The place to adjust then is
  `src/AxClaude.Core/Hooks/ClaudeCodeHooks.cs`: the events' table with the version each event was verified with.
  `30-hooks-stand-in.txt` checks the window's side without Claude Code, with the stand-in of section 2.

Windows' own programs change with Windows' monthly updates, so Command Prompt and Windows PowerShell are "changed"
about once a month; their check is axTerminal's, run once for all the shells.

## Rules

- Claude sessions in tests use Haiku through `--model haiku` and leave the user's settings alone.
- A test never activates a window or sends keys to the system outside tier 3, and tier 3 runs only at the user's word.
- Test folders are fresh under `%TEMP%`; recordings and reports follow the privacy rule (Redaction, never `--raw`).
- A window that changed is checked by its scripts, run with `--record`, before it is released: `release.ps1` and its
  rehearsal refuse while an app's window code (`src/<App>`, `src/<App>.Core`, `src/AxKit.Forms`, `src/AxKit.Core`) has
  a commit newer than the app's last recorded pass on the desktop tier or with NVDA listening, and name the apps
  (`tools/window-passes.ps1 -Check`, whose header says how it decides; run it without `-Check` at any time to see
  what a release would say). `test.ps1` does not run it: during ordinary work the windows are ahead of their record.
- `nvda-pass.ps1` does not start while the last key or mouse input is under 20 seconds old, nor with a capture add-on
  that differs from `tools/nvda-capture`.
