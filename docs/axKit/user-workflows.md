# User workflows as one source

What a user gets done in axKit, written once and used four times: the automated checks play it, a person reads it as
a how-to with the keys as they are now, axHelp's Claude answers from it, and a run through the AI tools exercises the
same commands. Specified on 2026-11-15, the night run, from the code at commit f56b1ee, for build agents who know only
this document and the code. Requirements are numbered WF-n.m and decisions WF-D-n. The decisions were taken by the
assistant on the user's behalf that night, each for the user to confirm (section 7). Every requirement here is
built; each names the work package that built it, whose plan is in the git history (section 8 and 9).

Revised the same evening after two reviews, one against the code and one against the user's requirements and the
project's principles. What they found is worked into the text. Where the two pulled against each other, the choice
was made by the project's principles (stability first, the simplest thing, the user in control, no redundancy) and
is a decision of section 7: WF-D-23 to WF-D-30, and the reworded WF-D-1, 6, 7, 9, 13, 19, 20 and 22.

The words of the other documents hold: `docs/axKit/automation.md` (the checks and their tiers),
`docs/axAutomate/SPEC.md` (the script language and the engine, AU-n), `docs/axKit/app-kit.md` (the command list,
AK-n), `docs/axKit/ai-help.md` (Claude's working folder, AH-n), `docs/axKit/mcp.md` (the AI tools, MC-n).

## 1. Summary

The user's ask, in substance (the priorities item "axKit workflows as one source for users, axHelp, the AI tools and
the tests"): take the workflows the tests already have and make them axHelp's knowledge, so that when a user asks
what axKit can do, Claude names the workflows, and when asked how, gives the steps with the keyboard shortcuts;
everything uses the same canonical name, so that steps are built automatically and follow when someone changes a
shortcut; monitor and test workflows through the AI tools, but even more the keyboard workflows, built from commands
that happen to be triggered by shortcuts; avoid redundancy, do not bloat the tests; the stabilisation of user
workflows is critical to the whole project and needs infrastructure. And on 2026-11-15: "the axhelp workflow
reference and the addition of user workflows in the testing suite that keeps track of keyboard level events that
accomplish goals that someone would want to do in axkit and things that we want to ensure we keep stable; this would
be similar to the existing nvda pass and workflow automations."

What exists (at commit f56b1ee, on 2026-11-15, before the build; every count in this document is of that commit and
moves with each script added): 133 scripts in `tests/scripts` and `tests/nvda-scripts`, each with one label
(`# Workflow: ...`), about ten thousand steps, 2,997 of them `keys` lines that name a key and not a command; a page
that lists the labels; a record of passes written by hand. A script of fourteen goals has one label. A changed key
breaks every script that presses it, and no generated text names a key at all.

What this adds, in one paragraph. A command has one name everywhere, its label, with its menus where two share it
(WF-1). Every window can list its commands as data, each with its key as a value and not as display text (WF-2), and
the list is kept in the repository as the command inventory. A comment line `# Goal:` inside a script names what the
user gets done by the steps under it, and a line `# Check:` what the steps under it only test (WF-3). A step
`do <name>` presses the named command's own key, whatever it is today (WF-4). A run collects, for every goal, the keys
that went in and everything the window said; for the goals marked `# Stable`, and only for them, that trace is kept
in the repository, and a run fails when it changes (WF-5). A recorded run writes when each goal last passed (WF-6).
From the scripts and the command inventory a generator writes the reference a person reads, by app, goal by goal,
with the current keys and nothing of the checks' bookkeeping (WF-7), and axKit puts the same pages into Claude's
working folder with rules for answering from them (WF-8). The same scripts run through the AI tools, step by step
where a step is a command the tools may run (WF-9), and the existing scripts move to `do` by a converter (WF-10).

## 2. Goals and non-goals

- G1. One definition per goal: the steps of a script section. The how-to, axHelp's file, the keyboard check and the
  run through the AI tools all come from it; none is written twice.
- G2. One name per command, the one the user hears: the label. The palette, the AI tools, the scripts, the
  inventory and the reference use it, through one resolver.
- G3. A key changes in its key table and nowhere else: scripts that use `do` keep passing, and the reference follows
  when the inventory and the pages are written again. A check says when they are stale.
- G4. The keyboard path is what is checked: `do` presses the key through the tier's own key path, as `keys` does.
- G5. No second suite and no duplicate script: goals are marked inside the scripts that exist; all 133 stay valid as
  they are until they are marked.
- G6. Infrastructure for stability: a trace per goal in every run's report, kept in the repository and held equal
  for the goals marked stable, and a record per goal and tier of the last result and the last pass.
- G7. Local first, plain code, no dependency: files in the repository and in Claude's working folder; no index, no
  database, no network (bundle G6, B-10, AH-7.5).
- Non-goal: a command identifier beside the label (WF-D-1).
- Non-goal: a new AI tool (WF-D-9), raw keys or typing through the AI tools (`mcp.md`, non-goals).
- The F1 texts written from the command list (WF-D-25), a non-goal of the night's work, were reversed by the user on
  2026-11-16 and built later: the key list since 2026-11-23, the hand-written part trimmed on 2026-11-25.
- Non-goal: a Help menu item or a section of a user guide that shows the how-to (WF-D-23), the user's to decide.
  axHelp offering to do what it explains (WF-D-24) was decided on 2026-11-16 and built on 2026-11-24.
- Non-goal: goals, traces or the workflow record in axAutomate's window. The window reads a goal line as the note it
  is and writes no outcome to a file (AU-D10 stands); what a window says does not change (section 10 names what
  could follow).
- Non-goal: window checks in CI. CI has no desktop; it checks what can be read from files (WF-11).
- Non-goal: any change to what an app's window says or does. The additions are to the automation channel, which is
  open only in a test run, and to the files axHelp receives.

## 3. Words

- **Workflow**: one script of `tests/scripts` or `tests/nvda-scripts`, named by its label, the line
  `# Workflow: <a task in the user's words>` (AU-5.2a). It is the title its goals are listed under.
- **Goal**: what a user gets done by a run of steps inside a workflow: "Find a word in the text". A goal starts at a
  `# Goal:` line and runs to the next goal line or check line, or to the script's end. It holds at least one input
  step. It is the unit that passes or fails, that has a trace, an entry in the workflow record and a how-to.
- **Check**: steps inside a workflow that only test: a refusal, a wrong input, a repeat, a regression, a state. A
  check starts at a `# Check:` line and runs as far as a goal does. It passes or fails by itself and has its trace and
  its entry in the workflow record as a goal has; it is no how-to and is on no page of the reference.
- **Section**: a goal or a check. What this document says of a goal's result, trace and record holds for a check
  too, unless it says otherwise.
- **Setup**: the steps of a script before its first section: the files, the settings, the first window.
- **Command**: one thing a window can do, as its command list holds it: an item of the menu bar, or a key with no
  menu item (`AxKitForm.Add`, `AxKitForm.AddKey`; `app-kit.md` AK-2).
- **Canonical name**: how a command is named everywhere: its label, plain (`MenuText.Plain`: no letter marker, no
  trailing dots), and where the label is not unique in its window, the label followed by its menus as the palette
  lists it: `Word wrap`; `Heading, Navigation, Next`.
- **Chord**: a key press as data, in the script language's own words: `ctrl+shift+w`, `f3`, `ctrl+1`.
- **Key text**: a key as a menu shows it: `Ctrl+Shift+S`, `Enter in the message field`.
- **Step**: one line of a script. An **input step** is one the user would do at the keyboard: `do`, `keys`, `type`,
  `menu`, the steps that open, choose and close windows (`start`, `launch`, `adopt`, `use`, `close`) and `mcp call`
  (`ScriptReader.IsInput`).
- **Command inventory**: the commands of every app's window as the windows listed them, kept in
  `docs/axKit/window-commands.json`. Always by both words in what a run prints and in the other documents:
  the gap list is another inventory. Inside this document "the inventory"
  is this one.
- **Trace**: for one section of one run, each input in order and everything the window said in between, without
  times. Every run can write its traces into its report; a trace is kept in the repository only for a section marked
  `# Stable` (WF-5.4).
- **Workflow record**: `docs/axKit/workflow-record.json`: for each section and tier, the last result and the last
  pass. Always by both words in what a run prints and in the other documents: `docs/axKit/nvda-test-record.md` is
  another record. Inside this document "the record" is this one.
- **Tier**: where a run plays: `desktop` (the hidden desktop, tier 2 of `automation.md`), `nvda` (the user's desktop
  with NVDA listening, tier 3), `ai-tools` (the hidden desktop, commands through `axkit_window_run`).
- **Reference**: the generated pages: `docs/axKit/workflows.md`, `docs/axKit/workflows/<group>.md`,
  `docs/axKit/window-commands.md`, and their copies in Claude's working folder.
- **Group**: the folder a script is in: an app's name (`axDown`), `AI tools` for `tests/scripts/mcp`, `NVDA only`
  for `tests/nvda-scripts`.

## 4. Requirements

### WF-1 Canonical names and the resolver

- WF-1.1 **The name.** A command's canonical name is its label, plain. Where two commands of one window share a
  label, the name is the label, a comma and a space, and the command's menus from the bar down joined by a comma and
  a space: exactly the palette's line (`Palette.Text`) without its key and without "checked". A key with no menu
  item has no menus; its name is its label. (WP-B1)
- WF-1.2 **One resolver**, `AxKit.Core/CommandNames.Resolve(commands, name, menu)`, for every caller that names a
  command: the step `do`, `axkit_window_run` (through `McpRun.Find`), the reference generator, the inventory's
  check and the converter. It compares plain texts without regard to case (`StringComparison.OrdinalIgnoreCase` on
  `MenuText.Plain(...).Trim()`), whole text against whole text; it never searches. (WP-B1)
- WF-1.3 **The rule**, in this order:
  1. An empty name is refused.
  2. With a menu given (the AI tools' second argument): the commands whose label is the name and whose menus are
     the menu.
  3. Without a menu: the commands whose label is the name. Only when there is none, the commands whose label,
     comma, space and menus together are the name (the whole line of WF-1.1). A label always wins over a line, so
     every name that resolved before this document resolves to the same command after it.
  4. One command left: found. None: unknown. Several: ambiguous.
- WF-1.4 **An unknown or ambiguous name is an error that names the candidates.** Ambiguous: the commands that share
  the name, each as its whole line. Unknown: up to five commands whose label holds the name or is held by it,
  without regard to case, in the list's order; none when there is none. The resolver returns the candidates as
  indexes; each caller words its own sentence (`CommandNames.Refusal` for the checks; `McpRun.Find` keeps its own
  sentences, word for word, since scripts and tests expect them).
- WF-1.5 **Commands no name tells apart** (axTask's nine items "Not set" in Context, Action keys, until a list names
  them: `ActionKeys.First` to `ActionKeys.Last`) are ambiguous also with their menus; the refusal says that the name
  cannot tell them apart. Nothing runs. The AI tools' refusal names their menus too (the maintainer's decision of
  2026-11-25), since "give the menu too" would not help: "The window has several commands named Not set, all in the
  menu Context, Action keys: neither the name nor the menu tells them apart, and none was run." ("all with no menu"
  for keys alone); where the menus differ, it names each menu once.
- WF-1.6 **The palette's own search stays a search** (`Palette.Find`: words, accents folded, ranked). It is how a
  person finds a command; the resolver is how a text names one.
- WF-1.7 **The AI tools.** `McpRun.Find` calls the resolver and so also takes the whole line in its `command`
  argument (`Heading, Navigation, Next`), which it refused before; every other answer is unchanged, and so are the
  gates of `mcp.md` MC-7. `mcp.md` MC-7.1 says so in the same commit. (WP-B1)
- WF-1.8 **The step `menu` keeps its matching for now**: the first palette line that starts with the given text
  (`AutomationChannel.Menu`); 277 lines depend on it. It moves to the resolver when the converter has rewritten
  every `menu` line to the canonical name its text resolves to today (WF-10.6); until then a `menu` line is not
  checked against the inventory.

### WF-2 The command inventory

- WF-2.1 **A key as data.** A menu item knows the key it shows as a value: `AxKitForm.Item` gives the item its key
  (`CommandPalette.Keyed`), which covers every command made by `Add` and every item made by `Item`; an item made by
  hand with a display string of a real key is given it where it is made (the two shared ones in WP-B2; section 10
  lists the ones left). An item whose display string describes a place ("Enter in the message field", "Ctrl+C in
  the view", "Alt+F4") has a key text and no key as data. So has an item whose key is the focused control's own and
  not the window's: the editor shell's Cut, Copy, Paste and Select all (`TextEditorForm.AddClipboardItems`;
  WF-D-30). The display text is never parsed. (WP-B2)
- WF-2.2 **One chord vocabulary**, `AxKit.Core/KeyChord`: the names a chord may hold, each with its virtual key
  code, read by the channel's `keys` (`AutomationChannel.ParseKey`), by the NVDA tier's (`Native.VirtualKey`) and
  written by the window for a key as data (`BundleKeys.Chord`). The names are the union of what the two parsers knew
  on 2026-11-15, each with the code it had, and four more that the key tables use: `slash`, `semicolon`, `add`,
  `subtract` (the numeric keypad's plus and minus). A key the vocabulary has no name for has no chord; the listing
  then gives its key text alone. (WP-B2 for the vocabulary and the channel, WP-B4 for the NVDA tier)
- WF-2.3 **The window lists its commands as data**: the channel's request `commands data` (section 5.2). One row
  per menu item that the palette would list (`CommandPalette.Entries`: available, enabled, not `NoEntry`, each menu
  refreshed first), then one row per label of the keys with no menu item. Each row: `label`, `menu`, `key` (a
  chord, or empty), `also` (further chords of the same command), `keyText`, `mark`, `checked`, `keyOnly`, `ai`
  (whether the AI tools may see and run it: false for every row of an app of `McpTools.NeverRead`, axPass and
  axClip, whose windows the tools never read and whose commands they never run (`McpRun.NeverRun`), and for a key
  with no menu item, which the tools never list; otherwise `CommandPalette.ForAiTools`), and `now`: the chord that
  would run this command if pressed at this moment, or empty. The request `commands` without a word keeps answering
  the palette's lines; 166 `expect commands` steps read them. (WP-B2)
- WF-2.4 **Keys with no menu item are folded by label**, so that a name is not made ambiguous by a second key
  (`CommandListing.Rows`): a key whose label is a menu row's label is that row's key (when it is the same chord) or
  one of its `also` chords (axDown's Redo: `ctrl+y`, also `ctrl+shift+z`); keys of one label with no menu row are
  one row with the first registered as `key` and the rest as `also` ("Between the text and the details":
  `ctrl+tab`, also `ctrl+shift+tab`, `f6`).
- WF-2.5 **`now`.** For each chord of the row, first `key`, then `also` in order: the commands of the window's list
  that have the chord are asked whether they act now (`Command.When`), in registration order, as
  `AxKitForm.RunCommandKey` asks them. A menu row's chord acts when no command of the list has it (the window base
  or the window handles it itself: F1, Ctrl+S) or when one that has it acts. A row of keys alone acts by a chord
  when the first command that acts on it carries the row's label. `now` is the first chord that acts; empty when
  none does or the row has no chord. While the window's key test runs (AX-8.12) a key is named and nothing is run:
  every row's `now` is empty and the listing gives "Key test" where a notice's title stands, so a `do` step waits
  there as it does at a notice, the converter converts no press of a key test, and a test fails on a `do` step
  between a script's `menu Test a key` and the Escape that ends it (2026-11-16, `repairs.md` R-60). The channel's
  `choose` refuses in the key test itself since 2026-11-25 (`AxKitForm.Choose` asks the gate); its `menu` is not held,
  and only a test run has the channel (`SPEC.md` AX-8.12).
- WF-2.6 **The inventory**, `docs/axKit/window-commands.json` (section 5.3): by app, the rows the windows listed,
  without what is state (`checked`, `now`). It is produced from live windows by a run of the automation on a
  desktop of its own, never by constructing a form in a test (a hotkey window's handle registers a system hotkey
  and touches the sign-in start entry, `HotkeyForm.OnHandleCreated`). The readable page beside it is
  `docs/axKit/window-commands.md` (WF-7.6). (WP-B5, WP-B6)
- WF-2.7 **Where the rows come from.** A run takes a snapshot of the current window's listing at every
  `check commands` step, which every app's first script has after its start (23 scripts hold one at f56b1ee), and of
  the one resolved row at every `do` step. A script places a further `check commands` where a window offers commands
  its start does not (an item selected, a field in hand, the viewer): the step is as cheap as before and checks the
  marks there too. A snapshot taken while a notice shows holds no menu row (a notice disables the bar,
  `CommandPalette.Entries`): it is checked and merged for its rows of keys alone and never makes its app whole for a
  rewrite (WF-2.9). axPass's only `check commands` stands in its unlock notice, so its first script gets a second
  one after the unlock (WP-B6).
- WF-2.8 **What never enters the command inventory**: a numbered entry of a list (`1. notes.md (...)`, the form
  `AxKitWindows.Label` gives recent files, open windows and recently closed ones: one character of 1 to 9 or a to z,
  a full stop, a space); a row whose label, menus or key text name the run's folder or the profile folder; a row
  whose label is not a text of axKit's own sources but the name of a program, a device, a folder or a shell of the
  computer the run was made on (it would be the user's, and it would make the file stale on the next computer):
  `CommandInventory.Kept` holds one named rule for each such row found, and whoever writes the file reads it whole
  first (WP-B6); and everything of `checked` and `now`.
- WF-2.9 **Writing.** `--inventory write` merges each snapshot into the app's rows: the rows of the snapshot that
  share a label and menus replace the inventory's rows of that label and menus, so a changed key replaces the row
  and several commands of one name (WF-1.5) stay side by side; other rows stay. A run that writes plays only the
  scripts that hold a `check commands` or a `do` step. `--inventory rewrite` does the same and then drops, for
  every whole app, the rows this run did not see; an app is whole when every script that gave a snapshot of it
  passed and at least one of those snapshots was taken with no notice showing. It is how a command that left a
  window leaves the inventory, and it is run over a whole folder of `tests/scripts` or over all of it. The files a
  writing run changed (the inventory, and then the pages by `tools\workflows.ps1`) are committed with the change
  that made them differ, never left changed in the working tree (WF-11.4).
- WF-2.9a **A write of the whole suite removes what no window lists** (2026-11-24, the key documents item's step 5):
  when `--inventory write` plays every script under `tests/scripts` that lists a window's commands (the live scripts
  aside, which a run without `--live` leaves out and whose windows the others list too), it writes as
  `rewrite` does, each whole app's rows only those the run saw, and says so in one line; a renamed or removed menu
  item's old row then goes by itself, where it needed a remembered `rewrite` before (`InventoryRun.Playing`). A write
  of part of the suite keeps the rows it did not see, since another folder's scripts list the same windows in other
  states, and says so in one line with the two runs that remove them (2026-11-24, repairs R-85).
- WF-2.10 **Checking, whenever the window checks run** (`--inventory check`, the default when the repository is
  found): every row of a snapshot must be in the command inventory under its app with the same label, menus, key,
  also, key text, mark, keyOnly and ai. A row that is missing or differs is a line of the run's end, under words
  that say by themselves what they mean and what to do, since whoever runs the scripts may not know this document:
  "The command inventory is stale (docs/axKit/window-commands.json): a window lists a command that the file does
  not hold as it is. Every script's own result stands. If you changed a menu, a key or a label, run the same
  scripts with --inventory write, then tools\workflows.ps1, and commit both with your change; if you did not, say
  so and change neither (docs/axKit/user-workflows.md WF-11.4)." The scripts keep their own results, and the exit
  code is 3 when every script passed (WF-11.5): never the 1 of a failed script. An app the command inventory holds
  no row for is not checked: the run says once "The command inventory has no rows for <app> yet: its commands were
  not checked", and the exit code does not change. That is how an app joins later: axClaude and axHelp, whose
  windows the redesign was changing in the first night (WF-D-22), joined so on 2026-11-16 (WP-B17). `--inventory off` is for a run against a build that is not the
  repository's, and for `tools/upstream.ps1 -Verify`, whose question is the outside program and not the windows'
  menus (WF-D-13). (WP-B6)
- WF-2.11 **The cheap check**, in `test.ps1` and CI, without a window: every `do` step of every script names a
  command that the inventory resolves (WF-1.3) under the app of the window the step reaches, read off the script's
  `start`, `use` and `adopt` steps (`ScriptApps.ByLine`). Where the app cannot be read (after `adopt`, a start by a
  file alone), the name must resolve in at least one app, each app's rows taken by themselves: never in the union
  of all apps, where Find, Undo and Keyboard shortcuts would each be ambiguous. A step whose app the inventory holds
  no row for is passed over, as the live check passes that app over. Among an app's rows, a row of keys alone whose
  label a menu row also has is passed over: it is the same command, listed in a state where its menu item was not
  offered. And the file is well formed and holds no account path. That the inventory is current is not checked
  there. (WP-B6)

### WF-3 Goals in scripts

- WF-3.1 **A goal line**: a comment line whose text, after the `#`, starts with `Goal:` (any case) and then holds
  the goal's title. It may stand anywhere after the header, also above a comment that comes before the script's
  first start (the maintainer's decision of 2026-11-25): the start is then the section's, and the pages take the
  window steps before the first goal as how the workflow starts (WF-7.5). The goal runs from its line to the line
  before the next goal line or check line, or to the script's end. (WP-B3)
- WF-3.1a **A check line**: a comment line whose text starts with `Check:` (any case) and then holds a title. It
  starts a section exactly as a goal line does, with its own result, its own trace and its own entry in the workflow
  record, and it is no how-to: a check is not in the index and its steps are on no page (WF-7.5). A goal keeps only
  the steps a user takes to get the thing done; the steps that test a refusal, a wrong input, a repeat, a
  regression or a state stand under a check line after it. (WP-B3)
- WF-3.2 **The setup** is every line before the first goal line or check line. It belongs to no section.
- WF-3.3 **A script with no goal line and no check line is one goal**, titled by its `# Workflow:` label and
  covering the whole script. All 133 scripts are valid as they are.
- WF-3.4 **The title** of a goal says what the user gets done, in the user's words, starting with a verb, without a
  key name and without a plan's section number: "Find a word in the text", "Go to a line by its number". The title
  of a check says what is checked, as a plain sentence: "A word that is not in the text is said as not found". No
  two sections of one script share a title, without regard to case, and a goal that a goal line starts holds at
  least one input step: a section that only looks is a check (WF-D-28). A test fails on either. A section is known
  everywhere by its workflow's label and its title. The titles are the user's vocabulary: each is one comment line,
  his to change (WF-D-26).
- WF-3.5 **`# Stable`**: a comment line whose whole text is `Stable`, in the run of comment lines that begins with
  the goal line or check line (for a script with neither: in the header). It marks a section whose trace is kept in
  the repository and must not change (WF-5.4 to WF-5.7).
- WF-3.6 **The header keeps its lines**: `# Workflow:` once, the title; `# Needs:` what the script depends on
  outside axKit, now read by the engine's reader too (`Workflow.Needs`) and no longer by a script of its own; the
  other header comments are what the workflow covers. A goal line or check line ends the header and is never among
  what the workflow covers.
- WF-3.7 **axAutomate's window is unchanged**: it reads a goal line or check line as it reads every comment after
  the header, as part of a note row ("Note: Goal: Find a word in the text ..."). `ScriptReader.Parse` keeps its steps
  and notes as they are; the sections are read beside them (`Workflow.Goals`).
- WF-3.8 **A workflow file** (`.workflow.json`) marks a goal with a note-only step whose note starts `Goal:`, a
  check with one whose note starts `Check:`, and `Stable` with a note-only step `Stable` right after it. No workflow
  file exists in the repository; this is built with the converter of AU-5.2, not here.
- WF-3.9 **A failing step still stops its script** (AU-D2, `ScriptRunner.Play`). The report then names, for a
  script with goal or check lines, each section with its result: passed (every step of it passed), failed (with the
  line), not run (the script stopped before it). A step that fails in the setup runs no section. A failure after the
  steps that is no one section's (a window that ended by itself, an ERROR in a log) fails every section that had
  passed: each is then reported and recorded as failed, with that said. A stable section's trace that differs or is
  missing (WF-5.5) fails that section alone, and so its script. (WP-B3; the trace's part in WP-B11)
- WF-3.10 **The report's lines** (section 5.1): a line `-- Goal: <title>` or `-- Check: <title>` before a section's
  first step (`-- Setup` before the setup when the script has sections), and after the last step one line of counts
  and one line per section. A script with no goal or check line prints as it does today. The run's last lines add
  the counts over all scripts.
- WF-3.11 **Marking the existing scripts** goes app by app, the most used first (axClaude, axDown, axFile, axTask,
  axStart, axClip, axHelp, axTerminal), from the step comments the scripts carry: a goal line is put above the
  comment that opens a user's task, a check line above the comment that opens steps that only test; the comment
  stays. Only comment lines are added; no step changes and no step moves. Where a user's steps resume after a
  check, they get a goal line of their own or stay with the check. (WP-B14)

### WF-4 The step `do`

- WF-4.1 `do <canonical name>`: the runner asks the current window for its commands (`commands data`), resolves
  the name (WF-1.3) and presses the row's `now` chord through the tier's key path: the channel's `keys` on the
  hidden desktop, `Native.Keys` with NVDA. What is checked is the keyboard path a user takes: `ProcessCmdKey`, the
  key's `When`, the key test, NVDA's own handling. (WP-B4)
- WF-4.2 **The key comes from the window as data** (WF-2.1), at the moment of the step. A key changed in a key
  table needs no change in any script that uses `do`.
- WF-4.3 **A command with no chord** (no key at all, or a key text and no key as data, WF-2.1) runs by its menu item,
  the channel's `choose` (section 5.2), found by its exact label and menus, and the step's detail and the trace say
  "menu item".
- WF-4.4 **A command whose chords do not act where the focus is** fails the step, with the command, its key text
  and the focused control said: "the key of <label> (<key text>) does not act where the focus is: <the focused
  control's name>". Nothing is pressed.
- WF-4.5 **A name that is unknown or ambiguous** fails the step with the resolver's refusal and its candidates. A
  window that shows a notice runs no command of its list (its bar is disabled and its shortcuts wait,
  `AxKitForm.ProcessCmdKey`): the step fails with "the window shows the notice <title>: its commands wait",
  whatever the name; a notice's own keys, and the few keys that work in a notice, are `keys` steps.
- WF-4.6 **The detail** of a passed step is `key <chord>` or `menu item`. The step waits as `keys` does (60 ms on
  the hidden desktop, NVDA's settling with NVDA). What the command says is for the expectations after it.
- WF-4.7 **Where it reads**: `ScriptLines.Known` holds `do`; `ScriptReader` gives it the value key `command`;
  `StepText.Sentence` reads it "Do the command Find"; in a workflow file it is `{"do": "do", "command": "Find"}`;
  `iffound do <name>` works as any step does. It runs from axAutomate's window too.
- WF-4.8 **What `do` is not for**: a command that ends the window (Exit): the script uses `close` or
  `keys alt+f4`, which tell the run that the window's end is meant; a key of a control (the arrows, typing) or of a
  notice: `keys` and `type`; a system-wide hotkey: Windows makes those from real input alone (the scripts use the
  window's own command as before).
- WF-4.9 **A window that cannot list its commands as data** (an AxKit.exe from before WP-B2, given with `--exe`)
  fails the step with that said.

### WF-5 Traces

- WF-5.1 **What a trace holds** (section 5.4): for each section of a run, in order, each input step as a line (`do`
  with the command's name and the chord pressed; `keys` with its chords; `type` with its text; `menu`; and the
  steps that open, choose and close windows: `start`, `launch`, `adopt`, `use`, `close`; `mcp call` with its tool),
  and under each input everything every window of the run recorded on its timeline since the input before it:
  announcements, sounds, focus moves, value changes, as the report writes them. No times, no process numbers. The
  setup is a section of its own in a run's trace. (WP-B10, WP-B11)
- WF-5.2 **What a trace leaves out**: the steps that prepare (`file`, `mkdir`, `silence`, `run`, `wait`,
  `allow error`, `mcp start`) and the steps that only look (`expect`, `refuse`, `try`, the reads,
  `check commands`). What they waited for is on the timeline and so in the trace.
- WF-5.3 **No account path.** Every line is written with the run's folder as `{temp}` and the profile folder as
  `{home}`, in either spelling of the separators and without regard to case, and then passed through
  `AxKit.Core.Redaction.Redact`. An input line is the script's own line as written, before `{temp}` is filled. This
  cleaning is the second guard, not the first: it knows an account path, an account name and an e-mail address; it
  does not know a clipboard's text, a program's name or a shell's output. What a kept trace may hold at all is
  WF-5.6.
- WF-5.4 **Kept traces are opt-in, section by section.** A trace is kept in the repository, in
  `tests/traces/<folder>/<script>.trace.txt` (the folder and the name those of the script under `tests/scripts`),
  only for a section marked `# Stable`. Every other section's trace, and the setup's, goes to the run's report alone
  (`--report`), which is never committed. Only the `desktop` tier writes and compares kept traces.
- WF-5.5 **Comparing** (`--traces check`, the default on the `desktop` tier when the repository is found): after a
  script whose steps passed, the trace of each `# Stable` section is compared with its kept section, line by line.
  There is no difference that is only a note. A kept section that differs fails its section, and so its script,
  after the steps: "FAIL after the steps: the trace of the stable goal <title> differs at its line <n>: kept <line>;
  now <line>" (the first differing line; "check" in place of "goal" for a check). A `# Stable` section with no kept
  section fails the same way: "FAIL after the steps: the stable goal <title> has no kept trace: run with --traces
  write". The run's end counts the stable sections that were equal and those that failed.
- WF-5.6 **What may be marked**: only a section in which the windows say axKit's own words and the script's own
  texts. Never a section of a window that reads the computer or the user's session. By rule (`Traces.MayKeep`; the
  cheap check fails on a `# Stable` line there): no script of `tests/scripts/axClip` (the test desktop shares the
  clipboard with the user's session), `axStart` (the programs installed), `axProcess` (the programs running),
  `axTerminal` and `axGIT` (a shell's and git's output) or `axMedia` (the sound devices), no script of
  `tests/nvda-scripts`, and no script with a `# Needs:` line (a model's words, a program's output). By reading: no
  section that shows axFile's drives or its Open with list, or anything else that is the computer's or the user's.
  Whoever marks a section reads its whole kept section before the commit and says in the commit message that it was
  read.
- WF-5.7 **Earning the mark.** A section gets `# Stable` only after its trace came out identical in three runs:
  the line is added, one run writes the section (`--traces write`), and two runs compare it and pass. A section
  that differs in either loses the line again, and the next `--traces write` removes its kept section: an
  announcement and a focus move can change places with the timing of a run, and such a section is not one to hold
  equal. The same holds later: a kept section that differs in one run and is equal in the next two shows timing,
  not a change; whoever meets it takes the mark and the kept section away in the same commit and says so.
- WF-5.8 **Writing** (`--traces write`): after a script that passed, its trace file is written whole: the header
  and the traces of its `# Stable` sections, in the script's order. A script with no `# Stable` section has no file,
  and a file it had is deleted. A deliberate change is reviewed as the file's difference in git. `--traces off`
  neither writes nor compares; it is what `--nvda`, `--ai-tools` and `--convert` run with.
- WF-5.9 **The cheap check** in `test.ps1` and CI, without a window: every `# Stable` section has a kept section,
  and every kept section belongs to a `# Stable` section of a script that exists; no script that may keep none
  (WF-5.6) holds a `# Stable` line; `PrivacyTests` reads the files as it reads every file.
- WF-5.10 **Which goals are kept stable is the user's to name** (the requirements' open question on "the core
  mechanics"). Tonight's marks, in one script of axDown, prove the mechanism and nothing more (WF-D-6).

### WF-6 The record

- WF-6.1 `docs/axKit/workflow-record.json` (section 5.5): for each section and each tier it ran in, the last result
  (passed, failed, not run, with the version, the commit and the date) and the last pass (the version, the commit,
  the date). (WP-B12, WP-B13)
- WF-6.2 **Written by the command line with `--record`**, after its last script: every section of every script it
  played is entered for the run's tier. A section is known by its workflow's label and its title, so that a pass
  played from a copy of the scripts (`nvda-pass.ps1` copies them) finds its entry; the script's path in the
  repository is written beside it, found by the label. Entries whose label or title no script has any more are
  dropped at each write; until then they fail no check (a renamed title must not stop a commit).
- WF-6.3 **The values.** The version is AxKit.exe's product version up to a `+`. The commit is
  `git rev-parse --short HEAD` in the repository, with a `+` after it when `git status --porcelain -- src tests`
  prints anything, read once per run by the command line (`tools/AxAutomate`, not the engine); "unknown" when git
  does not answer. The date is the local date, `yyyy-MM-dd`.
- WF-6.4 **No account path**: the workflow record holds labels, titles, repository paths of scripts, versions,
  commits and dates, nothing else.
- WF-6.5 **Recording does not depend on memory.** `nvda-pass.ps1` always records: it passes `--record` and the
  repository's folder. On the hidden desktop, the run of WF-11.4, an app's folder after a change to a window, is
  given `--record`: `docs/axKit/commands.md` and the root `CLAUDE.md`'s Commands line show that run with the option.
  A run of one script during work on a window is given none and records nothing.
- WF-6.6 **A recorded run changes one file**, the workflow record, and no page: the reference is written from the
  scripts and the command inventory alone (WF-7.1), so `tools/workflows.ps1 -Check` never fails because of a run.
  The changed record is committed with the change the run checked; a pass that checked no change of its own is
  committed alone, with the line `Item: none, the workflow record of the <tier> run of <date>`. It is never left
  changed in the working tree for a later commit. Where two branches changed the file, either side's lines are
  taken: the next recorded run writes those sections again.
- WF-6.7 **`docs/axKit/nvda-test-record.md` stays** for what a pass found: the failures, their causes, what was
  heard for the first time, what is owed to the user's ear. When a goal last passed is no longer written there by
  hand, and the texts that tell the assistant to write it there say so in the same commit: the root `CLAUDE.md`
  ("Manual verification"), `docs/axKit/SPEC.md`'s list of documents and `docs/axKit/documents.md` (section 8,
  WP-B13). axAutomate's own window still writes no outcome to a file (AU-D10): this is the command line's record.

### WF-7 The reference for people

- WF-7.1 **Written by a generator in C#**, `AxAutomate.Core/Reference`, from two inputs and nothing else: the
  scripts and the command inventory. Started by `tools/workflows.ps1`, which keeps its name and its contract:
  without an option it writes the pages, with `-Check` it writes nothing and exits with 1 when a page on disk is
  not what the inputs give, line ends aside. `test.ps1` and `build.yml` call it as before. (WP-B7, WP-B8)
- WF-7.2 **The pages**: `docs/axKit/workflows.md`, the index: by group, each workflow's label and under it its
  goals' titles (section 5.6); `docs/axKit/workflows/<group>.md`, one page per group: each workflow with how it
  starts, and each goal with its steps and what is heard; `docs/axKit/window-commands.md`, every command of the
  inventory. One page of everything would be several hundred kilobytes, more than a person follows and more than
  Claude's Read tool takes at once; the index stays small enough to read whole (WF-D-8).
- WF-7.3 **A goal's steps** are its input steps as numbered sentences, each ended by a full stop (typed text ends
  its line itself). Each says what a person does, never the runner's mechanics (WF-7.9):
  - `do`: with the command's key and menus from the inventory. The menus are written from the bar down, the word
    "menu" after the first: "Press Ctrl+F (Find; Navigation menu).", "Press Ctrl+H (Heading; Navigation menu,
    Next)."; for a key with no menu item, "Press Ctrl+Tab (Between the text and the details)."; for a command with
    no chord, "Choose Send message in the Actions menu.", "Choose Heading in the Navigation menu, Next."; for a
    name the inventory does not resolve, "Do the command Find."
  - The key's words: a chord is written as a keyboard names its keys ("Ctrl+Shift+Z", "Page Down", "Page Up",
    "NVDA+A"), in `Reference`'s own key words, for `keys` lines and for the commands page's "Also:"
    (`StepText.Keys` keeps "Pagedown" and "Nvda+A" for axAutomate's window). For a `do` row the sentence prints the
    row's key text where that differs from the chord's words: the window's own spelling is what its menu shows, and
    the pages never change it.
  - `keys`: "Press Ctrl+F." A line of one chord that carries Ctrl or Alt or is a function key, and that exactly one
    command of the app has in the inventory (as `key` or among `also`), names that command exactly as a `do` step
    does: "Press F12 (Go to line; Navigation menu)." So one key reads one way on a page, whichever step pressed
    it. The key printed is the one pressed: the row's key text when the chord is the row's key, else the chord in
    key words. This is how a script not yet moved to `do` still names its commands; it is a hint from the
    inventory, since a script's state is not known to the generator. A plain key (Enter, Backspace, a letter)
    names no command: it is a field's or a list's own key far more often.
  - Equal presses in a row are one line: presses of one key that follow each other with nothing heard between them
    read "Press Tab seven times.", with a command named after the count, "Press F3 three times (Find next;
    Navigation menu)."; the count in words to twelve, "twice" for two, in figures from 13 on (the maintainer's
    decision of 2026-11-25); a `keys` line of one key several times counts the same way. A line of several
    different keys is never counted, and neither is typed text or a step after "If that came: ".
  - `menu`: a line whose text exactly one row of the app answers (the row's label, or its label, a comma, a space
    and its menus, starts with the text, without regard to case) names that row's whole label and menus, and its
    key when it has one: "Choose Larger text in the Options menu, Text (key: Ctrl+Plus)." It is written as the
    choice it is, since the check chose the item and did not press the key. Otherwise "Choose the menu item
    <text>.", the text as the script has it, without a bracket that opens and never closes: a script that names an
    item by its first words, `menu Check for updates (axKit`, reads "Choose the menu item Check for updates."
  - `start`: "Open <the app's name> with <what>." for a start whose first word is a verb of `Dispatch.Apps`
    ("Open axDown with the file find.md."), "Open <the app's name>." without arguments, "Open <what>." for a start
    by a file alone. What only serves the check is not in the sentence (the maintainer's decision of 2026-11-25):
    in a start of axClaude or axHelp everything after a bare `--` (Claude Code's own options, by which a check picks
    its model and its permission mode) but `--continue`, `--record` with its file, `--model` with its value, and
    `--claude` with its program (the check's stand-in for Claude Code, or one that is not there). A path in the
    run's folder is named by what it is, "the folder notes" or "the file find.md" (a name with an extension is a
    file), and the run's folder itself is "the test folder". The options a person gives are in words, each after a
    comma: `--add` "added to the list", `--new` "in a new window", `--title <text>` "titled <text>",
    `--hide-on-close` "hiding when it is closed", `--hidden` "hidden", `--source stream` "fed by Claude Code's
    stream" (`--source screen`, the default, has none), `--continue` after the `--` "taking up the last
    conversation", `--command` and what follows it "handed the command <it>"; `--launcher` is said in the opening,
    "Open axTemplate's launcher." So: "Open axMedia with the folder ch, added to the list.", "Open axPreview with
    the file README.md, in a new window." Any other argument stands as the script has it. `StepText.Sentence`
    keeps its own words ("Start down find.md") for axAutomate's window.
  - `launch` hands over to a window that is open, which for a person is opening the app again: "Open axClip
    again.", "Open axMedia again with the folder ch, added to the list."; by a file alone, "Open the file b.md
    again." (the maintainer's decision of 2026-11-25).
  - `adopt`: "Go to the window that opened." (the maintainer's decision of 2026-11-25).
  - `use`: window 1 is "Go back to the first window."; another is "Go to the axFile window.", the app read off the
    script's own start lines (`ScriptApps.ByLine`), when the window left is of another app, and "Go to the window
    that opened." where the lines do not say whose window it is or it is of the same app as the one left (the
    maintainer's decision of 2026-11-25). A window's number is the run's, and no person counts windows.
  - `type`: "Type "<text>".", the text in quotes, so that a listener knows where it ends and that the full stop is
    not typed; a piece of JSON typed is in the quotes too (the maintainer's decision of 2026-11-25).
  - `mcp call`: what a person asks Claude, which the tool then does (the maintainer's decision of 2026-11-25),
    from the tool and the arguments the check gave it, the window's id never said: "Ask Claude to list the open
    windows.", "Ask Claude to read the window's text.", "Ask Claude to run the command Go to line with the text
    "2".", "Ask Claude to run the command Heading in the Navigation menu, Next.", "Ask Claude to offer you the
    command "echo first".", "Ask Claude to add "Call the dentist" to the list Personal, with Due 2026-11-18 and
    Priority High."; a line break in a text is " (new line) "; a tool it does not know, "Ask Claude to use the tool
    <tool>." `StepText` keeps "Call the tool <tool>" for axAutomate's window.
  - `close`: its sentence of `StepText` as it is ("Close the window.").
  - `iffound <step>`: the step's sentence after "If that came: ".
- WF-7.4 **What you hear.** Each expectation of speech is a line under the input step before it, indented by the
  width of the step's number, its full stop and a space, and four spaces at the least, so that it stays inside its
  item in Markdown from item 10 on and from item 100 on (the maintainer's decision of 2026-11-25); one that stands
  before the goal's first input step is a line of its own before step 1, without the spaces (spaces there would
  make it a code block). `expect said
  <text>` reads "You hear: <text>." (`! ` dropped; `♪ <name>` as "You hear the sound <name>."; `→ <text>` as "The
  focus moves to <text>."; `· <text>` as "It reads <text>."); a word heard and the sound of its name, one right
  after the other in either order and neither of them one that may not come, are one line, "You hear the Sent sound
  and Sent." (the maintainer's decision of 2026-11-25; the word after its sound is then no repeat of the line
  before it); `try said` reads "You may hear: <text>."; `expect
  spoken` "NVDA says: <text>."; `expect mcp` "The tool answers: <text>.", and a text the check wrote as a piece of
  data, `"app": "axDown"`, as a sentence, "The tool answers with app: axDown." (the name in plain words: `notDone`
  is "not done"). The scripts match parts of announcements (`expect said , 2 of`), so a text is read by one rule:
  - What is dropped: a comma, colon, semicolon, bar, full stop or space at either end; a bracket that opens and
    never closes, with what follows it ("2. orchard (" is "2. orchard"); a backslash first; and a joining word at
    the end (of, to, the, and, a, an, or, at) when two words stay without it ("Before axTerminal connects to" is
    "Before axTerminal connects"; "2 of" stays).
  - Whole or cut short: a text from which nothing but a full stop at its end was dropped, and that ends with no
    joining word, reads as a whole announcement, "You hear: <text>." Every other text was cut short in the script
    and says so, the part in quotes so that it never sounds whole (the maintainer's decision of 2026-11-25): "You
    hear something with "2 of" in it." (also "You may hear something with" and "NVDA says something with"). Whether
    a text ends in the middle of a word the scripts do not say: such a text reads as it stands.
  - Left out: a text with no letter and no digit left ("@@"), and a joining word alone.
  - One word is kept, since one word or one number can be the whole point ("You hear: 8294400." after a sum). It
    is left out only when the line before it already holds that word and it so only repeats it ("apple" said back
    under `Type "apple".`). The words of a `try said` with `iffound` steps under it always stay.
  Every other step is left out: the set-up, the waits, the checks of state, the refusals.
- WF-7.5 **Nothing of the checks' bookkeeping.** A group's page holds, under a workflow, "Starts with:" and the
  window steps before its first goal as sentences, those of the setup and of any check before that goal (when it
  has any; WF-3.1), then its goals, and under a goal its numbered steps and what
  is heard: nothing else. No script path, no needs line, no tier, date, version or commit, no sentence about the AI
  tools: NVDA would read them aloud after every how-to, Claude would read them with every answer, and every
  recorded run would rewrite every page. A script is found by its workflow's label (`# Workflow: <label>`); when a
  goal last passed is in `docs/axKit/workflow-record.json`, one section a line, found by searching for the goal's
  title. A check (WF-3.1a) is on no page, and neither is a section the generator finds without an input step. A
  workflow left with no goal on a page and no steps, a script of checks alone (axClaude 24, axTask 07, axTerminal
  11, axGIT 02 and six more on 2026-11-25), is in no index and on no page, and a group left with no workflow has no
  page (the maintainer's decision of 2026-11-25): a label with nothing under it is no how-to.
- WF-7.6 **The commands page**: by app, one line per row of the inventory: the label, "Menu:" its menus (or "none,
  a key alone"), "Key:" its key text (or "none"), "Also:" the further chords in key words (WF-7.3) when
  there are any, and its mark; "Never run by AI tools." for a row with `ai` false. One command a line, so that a
  search for a label finds it. A row that the apps share is written once: under "## In every window" when every
  app of the inventory has it, under "## In every window but <the apps without it>" when three in four of the apps
  or more have it (and at least two); under an app's own heading are then the rows that are its own. A row is the
  same in two apps when its whole line is, so a command with another key or another mark in one app stands under
  that app. The inventory file itself does not change: it keeps every row under its app. (Built on 2026-11-15 and
  confirmed by the maintainer's decision of 2026-11-25; the rows a kind of window shares, the navigator's or the
  text window's, stay under each app, since a section per set of apps would split the page into some twenty.)
- WF-7.7 **No path of a run**: `{temp}\` is dropped from every sentence and a `{temp}` left is written "the test
  folder"; in a start a path in the run's folder is named "the folder <name>" or "the file <name>" (WF-7.3);
  `\uXXXX` escapes are filled in.
- WF-7.8 **Order**: the groups as today (the apps by name without regard to case, then AI tools, then NVDA only);
  in a group the scripts by their path, ordinal, lower case; in a script the goals as they stand.
- WF-7.9 **The how-to reads well aloud** (WF-D-31). The pages are heard: by a blind user through NVDA, and through
  axHelp's Claude, which answers from them. Every sentence is short and linear and says what a person does or
  hears: no window by its number and no word of the runner (use, adopt, launch); no option of a start that only
  serves the check; no key in the script's spelling; one spelling for one key on a page; no piece of JSON; no text
  that sounds finished where the script cut it short; no full stop after typed text; no run of equal steps. Where
  the scripts do not hold what a better sentence would need, the line stays as it is. The words are `Reference`'s
  own: `StepText`'s sentences, which axAutomate's window shows, do not change. `ReferenceTests` pins every
  sentence form. (WP-B8b)

### WF-8 axHelp's reference and rules

- WF-8.1 **The same pages, embedded as the guides are** (`AxKit.Core.csproj`, AH-7.2): the index, every group's
  page and the commands page. `AiDocumentation.Files` writes them into Claude's working folder, directly in
  `documentation` (so that `AiDocumentation.IsOurs` covers them): `workflows.md`, `workflows-<group>.md`
  (`workflows-axFile.md`, `workflows-ai-tools.md`, `workflows-nvda-only.md`) and `window-commands.md`, the name the
  page has in the repository (never `commands.md`, which in the repository is the developer's commands), written
  and kept current as the guides are (AH-7.1), each with a line in `index.md`. (WP-B9)
- WF-8.2 **A test holds them to the repository**: `AiDocumentationTests` fails when an embedded page differs from
  the repository's, or when a group's page is not among the files.
- WF-8.3 **The rule file** (`.claude\rules\axkit-documentation.md`, `AiDocumentation.Rule`) keeps its sentences and
  gains these, in these words:
  - what the files are: "workflows.md lists what a user can do in each app: its workflows, and under each its
    goals. The steps of an app's goals, with the keys as they are in this version, are in workflows-<app>.md
    (workflows-axFile.md). window-commands.md lists every command of every window with its menu and its key, one a
    line."
  - "When the user asks what axKit or one of its apps can do, answer from workflows.md: name the workflows and
    goals that fit."
  - "When the user asks how to do something, find its goal in the app's workflows file (search for the goal's
    title with your Grep tool, the files are long) and give its steps as they are written there, keys included."
  - "The steps come from axKit's own checks: a file name or a word they type is an example. Say so in a few words
    and put the user's own in its place; change nothing else, not a key, not a menu, not the order."
  - "Never invent a key or a menu: a key you name comes from a workflows file, window-commands.md or a guide. Where
    a guide and window-commands.md name different keys for one command, window-commands.md is right: it is written
    from the windows themselves."
  - "When no goal covers the question, look in the app's guide and in window-commands.md, and say when you are not
    sure."

  Since 2026-11-24 the rule also lets Claude offer once to carry out the steps, with the caution of
  `AiCaution.OfferToDo` and nothing run before the user's yes (WF-D-24, ai-help.md AH-7.3b); what a run may do
  stays what the AI tools' own rule file says.
- WF-8.4 **The AI tools' rule** (`AiToolsRule.Text`) changes one entry, which becomes two lines, since a
  hand-written line of the file holds at most 112 characters (`AiToolsRuleTests`): the line
  `- "How do I ... in axFile", "which key ...": the workflows and` and, indented by two spaces as the file's other
  second lines are, `the guides in the folder documentation, not a tool.` The phrase the test pins, "the guides in
  the folder documentation, not a tool", stays whole on the second line. The file's other limit holds (at most 170
  lines).
- WF-8.5 **A new working folder's CLAUDE.md** (`WorkingFolderFiles.Instructions`) is not changed: it sends Claude
  to `index.md` and to the rules, which name the new files. An existing CLAUDE.md is the user's and is never
  touched (HE-2.8).
- WF-8.6 **The cautions are unchanged** (bundle AX-11.12, root rule 11, AH-7.6): an answer of Shift+F1 ends with
  `AiCaution.Written`; the conversation is axClaude's with its own. The pages themselves are axKit's own text,
  generated from the scripts, and carry no caution; what Claude says from them is AI text and is heard where it
  always is.
- WF-8.7 **Local first** (AH-7.5): plain files read by Claude Code's own Read and Grep; no index, no service, no
  network request of axKit's. The pages hold nothing of the user's.

### WF-9 Runs through the AI tools

- WF-9.1 **A mode of the command line**, `--ai-tools`, on the hidden desktop only (never with `--nvda`): no new AI
  tool. The runner is the MCP client it already is (`Broker`). (WP-B16)
- WF-9.2 **Which scripts the mode plays**: a script that holds at least one `do` line and no `mcp` step, that opens
  one window (one `start`; no `launch`, `adopt` or `use`), and whose app is not one of `McpTools.NeverRead` (an
  axPass or axClip window gives the tools nothing, so the pairing would never end). Every other script is passed
  over, with one line at the run's start saying how many: the keyboard run is where those are checked, and playing
  them a second time by the keyboard would prove nothing (`AiToolsMode.Covers`).
- WF-9.3 **The access is turned on as a user turns it on**, once per script, right before its first input step
  after its `start`, when the script's own expectations of its start have been played. The runner plays a fixed run
  of steps, which are lines of `tests/scripts/mcp/02-run-commands.txt`: the access on in the menu, the runner
  paired in the window's notice, Let AI tools run commands on (`AiToolsMode.Preamble`, section 6.3; a test holds
  them to that script). What was pending on the window's timeline before them stays pending for the script's own
  expectations, what they said is taken, and the result of the script's last `try` is as it was (`Window.Hold`,
  `Window.Resume`). Every gate of `mcp.md` stays as it is; nothing is seeded into a settings file. A step of the
  preamble that fails fails the script there: "the AI tools could not be turned on: <the line>: <its detail>".
- WF-9.4 **Step by step.** Every `do` step whose command the tools may run without asking (the row is not a key
  alone, its `ai` is true, its mark is not act: `AiToolsMode.MayRun`) is run with `axkit_window_run` by the row's
  label and menus, in the script's one window, which the preamble's `axkit_windows` answer named, and the step waits
  for the tool's answer. A refusal fails the step with the tool's words; only the refusal of `mcp.md` MC-7.8 ("The
  user is working in this window right now", which counts the whole session's last key press, the user's own in his
  own windows included) is waited out: the call is sent again after 3.5 seconds, three times at most. The same
  check made a second time as the window takes the run up gives no refusal but an answer whose `notRun` ends "the
  user was working in the window": nothing ran, and the call is sent again once, after the same wait
  (`AiToolsMode.IsBusyAtTakeUp`, 2026-11-16). A `do` step in the window's key test never goes through the tools:
  the window's listing names the key test where a notice's title stands, so `MayRun` is false, and the step is the
  keyboard's, where it fails as under any notice (WF-4, R-60); the scripts press the key test's keys with `keys`.
  Every other
  step plays by the keyboard as written, so the script's state holds. The expectations stay as they are: what the
  window says of the command is on its timeline as ever, after the words that say who ran it. One expectation
  knows of the mode: `expect quiet` does not count an announcement that is those words alone, for a command the mode
  ran ("axAutomate ran Find": by its key the command says nothing, which is what the step checks), and its detail
  names what it left out; the command's own words joined to them are counted (`AiToolsMode.IsOwnWords`,
  2026-11-16). The detail of such a step is `AI tools: <label>`.
- WF-9.5 **What is reported and recorded.** Each section's line of the report ends with how many of its input
  steps went through the tools: "through the AI tools: 3 of 8 inputs". A section is entered in the workflow record
  for the `ai-tools` tier when at least one of its steps ran through the tools; one with none gets no entry, and a
  run in the mode enters nothing for the `desktop` tier (its windows are in another state: the access on, a client
  paired).
- WF-9.6 **A step that fails only through the tools is the tier's finding**: it is recorded as failed for
  `ai-tools`, said in the report, and written as an item of the priorities list. No script and no window is changed
  to make the mode pass. The reference says nothing of the tier (WF-7.5).

### WF-10 Moving existing scripts

- WF-10.1 **A converter**, a mode of the command line (`--convert`), on the hidden desktop: it plays a script and
  asks the live window at each key press it might convert. A chord of a `keys` line becomes a `do` line only when
  the chord carries Ctrl or Alt or is a function key (`KeyChord.IsCommandChord`), and exactly one row of the
  window's listing at that moment has it as its `key` and as its `now`. Everything else stays `keys`: plain
  letters, the arrows, Enter, Escape, Tab, a chord that is only an `also` of its command (the press would change),
  a chord in a notice or the palette (no menu command is listed there). (WP-B15)
- WF-10.2 **The live window decides, not the inventory** (WF-D-10): the inventory cannot say which state a script
  is in when it presses Ctrl+F. The rows the converter used are merged into the inventory (WF-2.9), so the cheap
  check passes for the lines it wrote.
- WF-10.3 **The rewrite**: a `keys` line of several presses is written as several lines in the same order, each
  converted press a `do <canonical name>` line (`CommandNames.CanonicalOf` over the listing's rows) and each run of
  other presses one `keys` line; an `iffound` before the line stands before each line it becomes. Comments, blank
  lines and line ends stay.
- WF-10.4 **Proved by running**: the script must pass before it is converted; the converted script is played once
  more, and when it does not pass the file is put back as it was and the script is reported. An app's scripts are
  converted together and run whole before and after.
- WF-10.5 **Until a script is converted**, the reference names the command of a chord from the inventory (WF-7.3).
- WF-10.6 **`menu` lines**, later: the same mode records, for each `menu` line, the row its text ran (the channel
  answers it), and rewrites the line to `menu <canonical name>`; when every script is rewritten and passes,
  `AutomationChannel.Menu` resolves through `CommandNames.Resolve` in place of its prefix search. Not tonight
  (section 10).
- WF-10.7 **In the first night the converter passed over `tests/scripts/axClaude` and `tests/scripts/axHelp`**: the
  redesign was changing their steps the same night (WF-D-22). After it had merged, the scripts of axClaude that need
  no live model were converted (WP-B17, 2026-11-16); axHelp's two scripts hold no `do` line yet.

### WF-11 The checks

- WF-11.1 **`test.ps1` and CI** (no window): the unit tests of the resolver, the chord vocabulary, the listing's
  rows, the reader of goals and checks and their results, the traces, the workflow record, the command inventory
  and the generator; over the repository's own files: every script reads without a problem and has its label (as
  today), no script has two sections of one title, every goal holds an input step, every `do` names a command of
  the inventory (WF-2.11), the inventory and the workflow record are well formed (WF-6.4), every stable section has
  its kept trace and no other trace is kept (WF-5.9), the embedded pages are the repository's (WF-8.2); and
  `tools/workflows.ps1 -Check`: the pages are what the scripts and the inventory give.
- WF-11.2 **The run on the hidden desktop** (`dotnet run --project tools\AxAutomate -c Release -- <script or
  folder> --build`, any time, also while the user works): every `do` presses its key in the live window; every
  snapshot is held against the inventory (WF-2.10); every `# Stable` section's trace against its kept one
  (WF-5.5); the report names the goals and checks. One window run plays at a time on a computer: a second one waits
  for the first to end (`RunLock`, WF-D-29), so the runs of several agents never meet on the clipboard, a system
  hotkey or a one-window app.
- WF-11.3 **`nvda-pass.ps1`** (only with the user away): the same scripts with NVDA listening, `do` through the
  real keyboard path; the inventory check; no trace comparison; the workflow record written for the `nvda` tier
  (WF-6.5).
- WF-11.4 **After a change to a window, a key or a label**: the app's folder on the hidden desktop with `--record`;
  when the run says the command inventory is stale, the same run with `--inventory write`, then
  `tools\workflows.ps1`. The commit holds the code, the inventory, the pages and the workflow record together: a
  generated file never waits changed in the working tree for a later commit, where the next `-Check` or the next
  package's `.\test.ps1` would stumble on it.
- WF-11.5 **Exit codes** of the command line: 0 when every script passed and nothing is stale; 1 when a script
  failed (a `# Stable` section's changed or missing trace fails its script); 2 when the run could not start (the
  command line, the build, the program, another window run that did not end within the lock's hour); 3 when every
  script passed and the command inventory is stale. `tools/upstream.ps1 -Verify` runs with `--inventory off
  --traces off` and so sees 0, 1 and 2 with the meanings it knows; `nvda-pass.ps1` passes the code on.

## 5. Formats

Each format has its grammar and an example. The examples of 5.1 and 5.6 are
`tests/scripts/axDown/04-find-and-go-to-line.txt`, whole, as it reads once it is marked and moved to `do` (WP-B4); the
rows of 5.2 and 5.3 are axDown's commands as its window makes them (`TextEditorForm`, `EditorKeys`) and as
`tests/scripts/axDown/06-menus-and-help.txt` expects their palette lines. The trace of 5.4 and the record of 5.5 are
put together from that script's own lines and expectations, not from a recorded run: the first kept trace and the
first workflow record replace them as the true examples.

### 5.1 The script language's additions

```
goal line     # Goal: <title>            a comment line; "Goal:" in any case; anywhere after the header
check line    # Check: <title>           a comment line; "Check:" in any case; starts a section as a goal line does
stable line   # Stable                   a comment line of that one word, in the comment lines that begin with the goal or check line
needs line    # Needs: <what>            a header line, as before; now read by ScriptReader as well
do step       do <name>                  <name> is a label, or a label, ", " and its menus joined by ", "
```

Everything else of the language is as `tools/AxAutomate/Program.cs` lists it. Below is the script as it reads with
goals, checks and `do`. Every line of today's file stands where it stood; its eight `keys ctrl+f`, `keys f3`,
`keys shift+f3` and `keys ctrl+g` lines have become `do` lines; two goal lines and five check lines are added, each
above the comment that opens its steps, and nothing else:

```
# Workflow: Find text and go to a line
# axDown, test plan section 4 (Find and go to line), driven from inside the window (docs/axDown/nvda-test-plan.md).
# The run's own settings: no update check, whose network call logs an ERROR when GitHub cannot be reached.
file home\AxKit\axDown.json {"checkForUpdates": false}
file find.md # Find and go to line\n\nThe first apple is on line 3.\nNothing here.\nThe second apple is on line 5.\nLine 6.\nThe last line, line 7.

start down {temp}\find.md
expect focus find.md

# Goal: Find a word in the text
# 4.1: Ctrl+F, the field "Text to find"; Enter lands on the match and the focus is back in the text there.
do Find
expect focus Text to find
type apple
keys enter
expect focus {"name":"find.md","role":"Text","value":"The first apple is on line 3."}
expect state Line 3, Column 11 \u00B7
# F3 the next match; Shift+F3 back.
do Find next
expect focus The second apple is on line 5.
do Find previous
expect focus The first apple is on line 3.
expect quiet 1000
# Check: A word that is not in the text is said as not found
# A word that is not there: "Not found".
do Find
expect focus Text to find
type pear
keys enter
expect said Not found
expect focus find.md

# Goal: Go to a line by its number
# 4.2: F12, the field named with the line and column; a number, Enter: "Line n of m", the caret at that line's start.
do Go to line
expect focus Go to line (now line 3, column 11)
type 5
keys enter
expect said ! Line 5 of 7
expect focus {"name":"find.md","role":"Text","value":"The second apple is on line 5."}
expect state Line 5, Column 1 \u00B7
# Check: A number past the end lands on the last line
# A number past the end lands on the last line.
do Go to line
expect focus Go to line (now line 5, column 1)
type 99
keys enter
expect said ! Line 7 of 7
expect focus The last line, line 7.
# Check: A word in place of a number is refused and the caret stays
# A word: "Not a line number", the caret where it was.
do Go to line
type five
keys enter
expect said ! Not a line number
expect focus The last line, line 7.
expect state Line 7, Column 1 \u00B7
# Check: Escape in the field returns to the text where it was
# Escape returns to the text where it was.
do Go to line
expect focus Go to line (now line 7, column 1)
keys escape
expect focus {"name":"find.md","role":"Text","value":"The last line, line 7."}

# Check: The status bar holds the name, the line and column, the encoding and the line ending
# 4.3: the status bar holds the name, "Line n, Column m", the encoding and the line ending (NVDA+End reads it).
expect state "status":["find.md","Line 7, Column 1 \u00B7 UTF-8 \u00B7 CRLF"]
```

The report of a run of it in which line 39 fails (the lines of the steps are as today, and not every one is shown;
the lines that start `--` and the lines from `Goals:` on are new, WF-3.10):

```
== axDown\04-find-and-go-to-line.txt
-- Setup
ok   4: file home\AxKit\axDown.json {"checkForUpdates": false}
ok   7: start down {temp}\find.md  -> process 4321
-- Goal: Find a word in the text
ok   12: do Find  -> key ctrl+f
ok   14: type apple
-- Check: A word that is not in the text is said as not found
ok   26: do Find  -> key ctrl+f
-- Goal: Go to a line by its number
ok   35: do Go to line  -> key ctrl+g
FAIL 39: expect said ! Line 5 of 7  -> not within 60 s; seen:
Goals: 2 passed, 1 failed, 4 not run
  passed   Find a word in the text
  passed   check: A word that is not in the text is said as not found
  FAILED   Go to a line by its number, at line 39
  not run  check: A number past the end lands on the last line
  not run  check: A word in place of a number is refused and the caret stays
  not run  check: Escape in the field returns to the text where it was
  not run  check: The status bar holds the name, the line and column, the encoding and the line ending
```

The grammar of the new lines: `-- Setup`; `-- Goal: <title>`; `-- Check: <title>`;
`Goals: <n> passed[, <n> failed][, <n> not run]`, which counts goals and checks together; then one line per section
in the script's order: two spaces, the result in a column of nine characters (`passed`, `FAILED`, `not run`), the
title, a check's after the word `check: `, and for a failed one `, at line <n>` or `, after the steps: <the first
problem>`. Under `--ai-tools` a section's line ends `, through the AI tools: <n> of <m> inputs` (WF-9.5). A step that
fails in the setup gives `Goals: 0 passed, <n> not run` and, before it, `The setup failed at line <n>`. The run's last
lines gain `Goals: <n> passed, <n> failed, <n> not run` when any script of the run has a goal or check line.

### 5.2 The channel's command listing, as extended

Requests of the automation channel (one line each, `AutomationChannel.Execute`):

```
commands                     as before: the palette's lines, a JSON array of texts
commands data                new: the window's commands as data, one JSON object
choose <json>                new: runs one menu item, {"label": "<label>", "menu": "<menus>"}, found by exactly both
```

The answer of `commands data`, on one line:

```
{ "app": <the app's name as Dispatch.Apps has it>,
  "notice": <the title of the notice the window shows, "Key test" while the key test runs, or null>,
  "commands": [ <row>, ... ] }

row = { "label":   <the plain label>,
        "menu":    <the menus from the bar down, joined by ", "; "" for a key with no menu item>,
        "key":     <a chord of KeyChord's names, lower case, modifiers in the order ctrl, shift, alt; "" for none>,
        "also":    [ <further chords of the same command> ],
        "keyText": <the key as the menu shows it, without the full stop after a number-row key; "" for none>,
        "mark":    "read" | "open" | "change" | "act",
        "checked": true | false | null,      null for an item that is no check item
        "keyOnly": true | false,             true for a key with no menu item
        "ai":      true | false,             false for what the AI tools never see or never run: a key alone, every row of axPass and axClip
        "now":     <the chord that would run it if pressed now; "" when none would> }
```

axDown's window with a file open and the focus in the text gives, among its rows:

```json
{"app":"axDown","notice":null,"commands":[
{"label":"Undo","menu":"Actions","key":"ctrl+z","also":[],"keyText":"Ctrl+Z","mark":"change","checked":null,"keyOnly":false,"ai":true,"now":"ctrl+z"},
{"label":"Redo","menu":"Actions","key":"ctrl+y","also":["ctrl+shift+z"],"keyText":"Ctrl+Y","mark":"change","checked":null,"keyOnly":false,"ai":true,"now":"ctrl+y"},
{"label":"Find","menu":"Navigation","key":"ctrl+f","also":[],"keyText":"Ctrl+F","mark":"read","checked":null,"keyOnly":false,"ai":true,"now":"ctrl+f"},
{"label":"Go to line","menu":"Navigation","key":"f12","also":[],"keyText":"F12","mark":"read","checked":null,"keyOnly":false,"ai":true,"now":"f12"},
{"label":"Heading","menu":"Navigation, Next","key":"ctrl+h","also":[],"keyText":"Ctrl+H","mark":"read","checked":null,"keyOnly":false,"ai":true,"now":"ctrl+h"},
{"label":"Heading","menu":"Navigation, Previous","key":"ctrl+shift+h","also":[],"keyText":"Ctrl+Shift+H","mark":"read","checked":null,"keyOnly":false,"ai":true,"now":"ctrl+shift+h"},
{"label":"Heading level 2","menu":"Navigation, Next","key":"ctrl+2","also":[],"keyText":"Ctrl+2","mark":"read","checked":null,"keyOnly":false,"ai":true,"now":"ctrl+2"},
{"label":"Word wrap","menu":"Options, Text","key":"f11","also":[],"keyText":"F11","mark":"change","checked":true,"keyOnly":false,"ai":true,"now":"f11"},
{"label":"Keyboard shortcuts","menu":"Help","key":"f1","also":[],"keyText":"F1","mark":"read","checked":null,"keyOnly":false,"ai":true,"now":"f1"},
{"label":"Between the text and the field under it","menu":"","key":"ctrl+tab","also":["ctrl+shift+tab","f6"],"keyText":"Ctrl+Tab","mark":"open","checked":null,"keyOnly":true,"ai":false,"now":"ctrl+tab"}
]}
```

(The answer is one line; it is broken here to be read. The marks and menus are the ones the sources give on
2026-11-15; the first inventory written from the window is the reference for the rest.)

The channel sends this answer as `CommandListing.Write` wrote it. Every other answer of the channel is an object that
`AutomationChannel.Execute` passes through `JsonSerializer.Serialize`; this one is JSON text already, and serialized
once more it would arrive as a JSON string literal, which `CommandListing.Read` reads as no listing (6.2).

`choose` answers `{"ok":true}` after the item's click, with a check item's new state announced as the palette
announces it ("Word wrap, checked"), or `{"error":"no menu item <label> in <menus> now"}`.

### 5.3 The inventory, `docs/axKit/window-commands.json`

```
{ "format": "axkit-window-commands 1",
  "apps": [ { "app": <name>, "commands": [ <row>, ... ] }, ... ] }

row = the row of 5.2 without "checked" and "now"
```

The apps are in the order of their names without regard to case. An app's rows: the menu rows first, by their menus
and then their label, ordinal without regard to case, rows equal in both in the order the window listed them; then
the keys with no menu item, by label. The file is written with two spaces of indentation, one row on one line, UTF-8
without a byte order mark, so that a changed key is one changed line:

```json
{
  "format": "axkit-window-commands 1",
  "apps": [
    {
      "app": "axDown",
      "commands": [
        {"label": "Redo", "menu": "Actions", "key": "ctrl+y", "also": ["ctrl+shift+z"], "keyText": "Ctrl+Y", "mark": "change", "keyOnly": false, "ai": true},
        {"label": "Undo", "menu": "Actions", "key": "ctrl+z", "also": [], "keyText": "Ctrl+Z", "mark": "change", "keyOnly": false, "ai": true},
        {"label": "Keyboard shortcuts", "menu": "Help", "key": "f1", "also": [], "keyText": "F1", "mark": "read", "keyOnly": false, "ai": true},
        {"label": "Find", "menu": "Navigation", "key": "ctrl+f", "also": [], "keyText": "Ctrl+F", "mark": "read", "keyOnly": false, "ai": true},
        {"label": "Go to line", "menu": "Navigation", "key": "f12", "also": [], "keyText": "F12", "mark": "read", "keyOnly": false, "ai": true},
        {"label": "Heading", "menu": "Navigation, Next", "key": "ctrl+h", "also": [], "keyText": "Ctrl+H", "mark": "read", "keyOnly": false, "ai": true},
        {"label": "Heading", "menu": "Navigation, Previous", "key": "ctrl+shift+h", "also": [], "keyText": "Ctrl+Shift+H", "mark": "read", "keyOnly": false, "ai": true},
        {"label": "Word wrap", "menu": "Options, Text", "key": "f11", "also": [], "keyText": "F11", "mark": "change", "keyOnly": false, "ai": true},
        {"label": "Between the text and the details", "menu": "", "key": "ctrl+tab", "also": ["ctrl+shift+tab", "f6"], "keyText": "Ctrl+Tab", "mark": "open", "keyOnly": true, "ai": false}
      ]
    }
  ]
}
```

### 5.4 A trace file, `tests/traces/<folder>/<script>.trace.txt`

```
file      = header, { section }
header    = "# Trace of " <the script's path from the repository's root, with forward slashes>, newline,
            "# Written by tools/AxAutomate --traces write; never by hand (docs/axKit/user-workflows.md WF-5).", newline
section   = newline, "== " heading, newline, { input | event }
heading   = "Goal: " <title> | "Check: " <title> | "Setup"
input     = <an input step>, newline                              at the line's start
event     = "  " [ <window number> ": " ] <the event>, newline     two spaces; the number only in a script of several windows

an input step, by its step word:
  do       do <name> = <chord>          or   do <name> = menu item
  keys     the script's line            keys ctrl+f
  type     the script's line            type apple
  template the script's line            template Thanks | | the report   (docs/axTemplate/SPEC.md TP-6)
  menu     the script's line            menu Larger text
  start, launch, adopt, use, close, mcp call        the script's line ("mcp call <tool>" without its arguments)
  iffound <step>                        the inner step's line, only when it ran

the event, as the report's timeline writes it (Event.Spoken):
  <text>          an announcement             ! <text>     one that interrupts
  ♪ <name>        a sound by its name          → <name> (<role>): <value>     the focus moved to a control
  · <value>       the focused control's value changed
  a focus move ends in "  @ <place>|<over>" when the window gave its place (docs/axKit/workflow-graph.md WG-1.2):
                  → Text to find (Text):  @ axDown.text|choice
```

A kept file holds the sections of the script's `# Stable` goals and checks, in the script's order, and never the
setup; a run's report holds every section, the setup among them, in the same form. UTF-8 without a byte order mark;
`{temp}` and `{home}` in place of the run's folder and the profile folder. An example: the kept file of the script of
5.1 once its two goals carry `# Stable` (assembled, see above):

```
# Trace of tests/scripts/axDown/04-find-and-go-to-line.txt
# Written by tools/AxAutomate --traces write; never by hand (docs/axKit/user-workflows.md WF-5).

== Goal: Find a word in the text
do Find = ctrl+f
  → Text to find (Text): 
type apple
  · apple
keys enter
  → find.md (Text): The first apple is on line 3.
do Find next = f3
  · The second apple is on line 5.
do Find previous = shift+f3
  · The first apple is on line 3.

== Goal: Go to a line by its number
do Go to line = ctrl+g
  → Go to line (now line 3, column 11) (Text): 
type 5
  · 5
keys enter
  ! Line 5 of 7
  → find.md (Text): The second apple is on line 5.
```

### 5.5 The record, `docs/axKit/workflow-record.json`

```
{ "format": "axkit-workflow-record 1",
  "goals": [ { "workflow": <the workflow's label>,
               "goal":     <the goal's or the check's title; the label again for a script with no goal line>,
               "script":   <the script's path from the repository's root, with forward slashes>,
               "tiers":    { <tier>: { "last": <run>, "passed": <pass> | null }, ... } }, ... ] }

tier = "desktop" | "nvda" | "ai-tools"
run  = { "result": "passed" | "failed" | "not run", "version": <text>, "commit": <text>, "date": "yyyy-MM-dd" }
pass = { "version": <text>, "commit": <text>, "date": "yyyy-MM-dd" }
```

The entries, one for each goal and each check, are in the order of their script's path (ordinal, lower case) and then
of the script. A tier a section never ran in has no member. The file is written with two spaces of indentation, one
entry on one line:

```json
{
  "format": "axkit-workflow-record 1",
  "goals": [
    {"workflow": "Find text and go to a line", "goal": "Find a word in the text", "script": "tests/scripts/axDown/04-find-and-go-to-line.txt", "tiers": {"desktop": {"last": {"result": "passed", "version": "5.9.1", "commit": "f56b1ee", "date": "2026-11-15"}, "passed": {"version": "5.9.1", "commit": "f56b1ee", "date": "2026-11-15"}}}},
    {"workflow": "Find text and go to a line", "goal": "Go to a line by its number", "script": "tests/scripts/axDown/04-find-and-go-to-line.txt", "tiers": {"desktop": {"last": {"result": "failed", "version": "5.9.1", "commit": "f56b1ee+", "date": "2026-11-15"}, "passed": null}}}
  ]
}
```

### 5.6 A goal as it appears in the reference

The three preambles, word for word, each followed by a blank line:

```
# Workflows

Written by `tools/workflows.ps1` from the scripts of `tests/scripts` and `tests/nvda-scripts` and the command
inventory (`docs/axKit/window-commands.json`); never edit it by hand. What a user can do in axKit, by app: each
workflow is one script of the automated checks, found by its label, a task in the user's words, and under it are its
goals. The steps of every goal, with the keys as they are now, are in the app's page. The checks run on the
developer's computer (`tools/AxAutomate`, `nvda-pass.ps1`), never in CI (`docs/axKit/automation.md`,
`docs/axKit/user-workflows.md`).
```

```
# <group>: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives what axKit says there; "something with" and words in quotes give a part of it. After "Type"
the text to type stands in quotes: the quotes and the full stop after them are not typed. The file names and the
words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.
```

```
# The commands of every window

Written by `tools/workflows.ps1` from `docs/axKit/window-commands.json`, which the automated checks write from the
windows themselves; never edit it by hand. One line a command: its name, its menu, its key, and its mark, what it
does beyond reading (read, open, change, act). A command that all or nearly all windows have stands once, under "In
every window" or under a heading that names the windows without it; under an app's name are the commands of its own.
```

The index, `docs/axKit/workflows.md`: its preamble, then for each group:

```
## <group>

Steps and keys: `docs/axKit/workflows/<page>.md` (in Claude's working folder: `workflows-<page>.md`).

<n>. **<the workflow's label>**
    - <a goal's title>                   four spaces; one line per goal; none for a check, none for a script with no goal line
```

`<page>` is the group's name for an app (`axDown`), `ai-tools` and `nvda-only` for the two others. A goal's line
and a line under a step start with as many spaces as the number, its full stop and a space are wide, and four at
the least: the text of an item numbered 10 or more starts four characters in, one of 100 or more five, and a line
with fewer falls out of its item in a Markdown viewer (the maintainer's decision of 2026-11-25; every page has
four today, its steps going to 24). A workflow of checks alone (no goal on a page, no steps) is in no index and on
no page, and a group left with no workflow has neither heading nor page. For the example:

```
## axDown

Steps and keys: `docs/axKit/workflows/axDown.md` (in Claude's working folder: `workflows-axDown.md`).

1. **Open a text file in axDown**
5. **Find text and go to a line**
    - Find a word in the text
    - Go to a line by its number
```

A group's page, `docs/axKit/workflows/<page>.md`: its preamble, then for each workflow:

```
## <the workflow's label>

Starts with: <the window steps before the first goal>.   only when it has any: the setup's and the checks' before it

### <a goal's title>                                      no such heading for a script with no goal line; none for a check

<what you hear before the first step>.                    no spaces; only when an expectation of speech stands before it
<n>. <an input step's sentence>.                          typed text in quotes
    <what you hear after it>.                             the number's width; one line per expectation of speech
```

Nothing else stands under a workflow or a goal (WF-7.5). A script with no goal line has its numbered steps right
under "Starts with:". For the example:

```
## Find text and go to a line

Starts with: Open axDown with the file find.md.

### Find a word in the text

1. Press Ctrl+F (Find; Navigation menu).
2. Type "apple".
3. Press Enter.
4. Press F3 (Find next; Navigation menu).
5. Press Shift+F3 (Find previous; Navigation menu).

### Go to a line by its number

1. Press F12 (Go to line; Navigation menu).
2. Type "5".
3. Press Enter.
    You hear: Line 5 of 7.
```

Every sentence form the generator writes (WF-7.3, WF-7.4, WF-7.9), each pinned in `ReferenceTests`:

```
Press <keys>.                                             Press Ctrl+F.  Press Alt+2, Ctrl+Home.  Press Page Down.
Press <key> (<label>; <menus>).                           Press Ctrl+H (Heading; Navigation menu, Next).
Press <key> (<label>).                                    a key with no menu item
Press <key> <count>.                                      twice, seven times, 13 times; also with the command after it: Press F3 three times (Find next; Navigation menu).
Choose <label> in the <menus>.                            a command with no chord; after a menu step also: (key: <key>).
Choose the menu item <text>.
Do the command <name>.                                    a name the inventory does not resolve
Type "<text>".                                            the quotes and the full stop are not typed
Open <app>.  Open <app> with <what>.  Open <what>.        <what>: the folder <name>, the file <name>, the test folder
Open <app> with <what>, <option in words>.                added to the list, in a new window, titled <text>, hidden, ...
Open <app>'s launcher.
Open <app> again.  Open <app> again with <what>.  Open <what> again.
Go to the window that opened.  Go back to the first window.  Go to the <app> window.
Close the window.
Ask Claude to <what the tool does>.                       Ask Claude to list the open windows.
If that came: <any of the sentences above>

You hear: <text>.                                         You hear something with "<text>" in it.
You may hear: <text>.                                     You may hear something with "<text>" in it.
NVDA says: <text>.                                        NVDA says something with "<text>" in it.
You hear the sound <name>.                                You may hear the sound <name>.
You hear the <name> sound and <name>.                     a word and the sound of its name, either order
The focus moves to <text>.                                The focus may move to <text>.
It reads <text>.                                          It may read <text>.
The tool answers: <text>.                                 The tool answers with <name>: <value>.
```

The script's five checks (the word that is not in the text, the number past the end, the word in place of a number,
Escape in the field, the status bar) are on no page: they are in the script, in the report and in the workflow
record.

The commands page, `docs/axKit/window-commands.md` (under the same name in Claude's working folder): its preamble,
then `## <app>` and one line per row:

```
- Find. Menu: Navigation. Key: Ctrl+F. Mark: read.
- Redo. Menu: Actions. Key: Ctrl+Y. Also: Ctrl+Shift+Z. Mark: change.
- Between the text and the details. Menu: none, a key alone. Key: Ctrl+Tab. Also: Ctrl+Shift+Tab, F6. Mark: open. Never run by AI tools.
```

## 6. Architecture

Plain classes, static methods, records (B-10). Nothing here references an app; `AxAutomate.Core` references
`AxKit.Core` alone, as today; the command line `tools/AxAutomate` references `AxAutomate.Core`. No process is started
and no pipe opened in `src` beyond the ones there are; the one new program start (git, for the commit) is in
`tools/AxAutomate`, outside what `tools/outside-calls.ps1` reads.

### 6.1 `src/AxKit.Core`

`CommandNames.cs` (new, WP-B1), the resolver:

```csharp
public sealed record CommandName(string Label, string Menu);
public enum CommandMatch { Found, NoName, Unknown, UnknownInMenu, Ambiguous }
public sealed record CommandResolution(CommandMatch Match, int Index, string Wanted, string WantedMenu, IReadOnlyList<int> Candidates);

public static class CommandNames
{
    public static string Plain(string text);                         // MenuText.Plain(text).Trim()
    public static CommandResolution Resolve(IReadOnlyList<CommandName> commands, string? name, string? menu = null);   // WF-1.3
    public static string Line(CommandName command);                  // "Heading, Navigation, Next"; the label alone without menus
    public static string CanonicalOf(IReadOnlyList<CommandName> commands, int index);   // the label when it is the only one, else Line
    public static string Refusal(CommandResolution resolution, IReadOnlyList<CommandName> commands);   // WF-1.4, for do and the checks
}
```

`Index` is -1 unless found. `Refusal` gives: `no command is named`; `no command "<wanted>"` with `; nearest: <line>;
<line>` when there are candidates; `no command "<wanted>" in the menu <menu>`; `several commands are named
"<wanted>": write one of: <line>; <line>`; and, when the candidates' lines are all the same, `several commands are
named "<line>" and the name cannot tell them apart`.

`Mcp/McpRun.cs` (changed, WP-B1): `McpRun.Find(commands, name, menu)` keeps its signature, its record
`McpCommandName` and its four sentences, with a fifth for commands that share their menus too (WF-1.5); its body is a call of `CommandNames.Resolve` and a `switch` over the match.
`McpRun.Plain` stays and calls `CommandNames.Plain`.

`KeyChord.cs` (new, WP-B2), the chord vocabulary:

```csharp
public static class KeyChord
{
    public static int? Code(string name);            // "enter" 0x0D, "f3" 0x72, "a" 0x41, "1" 0x31; null for a name it does not know
    public static string? Name(int code);            // the first name of a code; null for a code without a name
    public static string Text(int code, bool ctrl, bool shift, bool alt);   // "ctrl+shift+w"; "" when the code has no name
    public static string Normal(string press);       // "Shift+Ctrl+P" as "ctrl+shift+p"; the press as given when a part is unknown
    public static bool IsCommandChord(string press); // carries ctrl or alt, or its key is f1 to f12
}
```

The names: a letter or a digit by itself; `enter`, `escape` (also `esc`), `tab`, `space`, `backspace`, `delete`,
`insert`, `up`, `down`, `left`, `right`, `home`, `end`, `pageup`, `pagedown`, `plus` (0xBB), `minus` (0xBD), `period`
(0xBE), `comma` (0xBC), `apps` (0x5D), `f1` to `f12`, and new: `slash` (0xBF), `semicolon` (0xBA), `apostrophe`
(0xDE), `add` (0x6B), `subtract` (0x6D). The modifiers stay each parser's own (`ctrl`, `shift`, `alt`; the NVDA tier's `nvda` and `insert`).

`CommandListing.cs` (new, WP-B2), the listing's rows, pure:

```csharp
public sealed record MenuCommand(string Label, string Menu, string Key, string KeyText, string Mark, bool? Checked, bool Ai);
public sealed record KeyCommand(string Label, string Key, string KeyText, string Mark, bool Applies, bool KeyOnly);
public sealed record ListedCommand(string Label, string Menu, string Key, IReadOnlyList<string> Also, string KeyText,
    string Mark, bool? Checked, bool KeyOnly, bool Ai, string Now);
public sealed record WindowCommands(string App, string? Notice, IReadOnlyList<ListedCommand> Commands);

public static class CommandListing
{
    public static IReadOnlyList<ListedCommand> Rows(IReadOnlyList<MenuCommand> menus, IReadOnlyList<KeyCommand> keys);   // WF-2.4, WF-2.5
    public static bool Ai(bool forAiTools, string appVerb);   // WF-2.3: false for an app of McpTools.NeverRead, else the item's own answer
    public static string Write(WindowCommands listing);       // the one line of 5.2, which the channel sends as it is
    public static WindowCommands? Read(string json);          // null for an answer that is not a listing: an array, an error, a JSON string
}
```

`menus` is the menu bar's entries in menu order. `keys` is every command of the window's list that has a key, in
registration order, a command made by `Add` as well as one made by `AddKey` (`KeyOnly` tells them apart), each with
whether its `When` holds now: `Rows` needs them all to say what a chord would run (WF-2.5).

`Ai/AiDocumentation.cs` (changed, WP-B9): `WorkflowsName = "workflows.md"`,
`WindowCommandsName = "window-commands.md"`,
`WorkflowPageName(string page)` (`workflows-axFile.md`), `WorkflowPages()` (the pages embedded, found by trying each
name of `Dispatch.Apps` and the two other pages, never by listing resources); `Files` adds them with their index
lines; `Rule` gains the sentences of WF-8.3. `AxKit.Core.csproj` embeds `docs/axKit/workflows.md` as `Workflows.md`,
`docs/axKit/window-commands.md` as `WindowCommands.md` and `docs/axKit/workflows/*.md` as
`Workflows.%(Filename).md`. `Ai/AiToolsRule.cs`: the one entry of WF-8.4, now two lines.

### 6.2 `src/AxKit.Forms`

- `BundleKeys.Chord(Keys key)` (new, WP-B2): `KeyChord.Text` of the key's code and modifiers; "" for `Keys.None` and
  for a key without a name.
- `CommandPalette` (WP-B2): the item's class `ItemInfo` gains `Key`; `Keyed(ToolStripMenuItem item, Keys key)`
  gives it and returns the item; `KeyOf(ToolStripItem item)` reads it (`Keys.None` when none was given, and the
  item's `ShortcutKeys` when that is set); `MenuEntry` gains the member `KeyData => CommandPalette.KeyOf(Item)`, no
  change to its constructor.
- `AxKitForm.Item(text, action, key, mark)` (WP-B2): calls `CommandPalette.Keyed(item, key)` where it sets the
  display string. `AxKitForm.AddKey(command)`: also keeps the command in a second list, `_keyOnly`. The Help menu's
  "Send a report" item and the Window menu's "Say the status" item (`WindowMenu`), which set their display strings
  by hand from a bundle key, are given that key with `Keyed`, as is each app's item under Apps and Access layers
  (`WindowMenu.NewWindowItem`, its launch key). The editor shell's four clipboard items
  (`TextEditorForm.AddClipboardItems`) are not: their keys are the EDIT control's own (WF-2.1, WF-D-30).
- `AxKitForm.Commands.cs` (new partial file, WP-B2): `internal WindowCommands CommandRows()`: the entries of
  `CommandPalette.Entries(MainMenuStrip)` as `MenuCommand`s (label `entry.Name`, menus `entry.Menu`, key
  `BundleKeys.Chord(entry.KeyData)`, key text `entry.Key` without a trailing full stop, mark as `McpChannel` writes
  it, checked as `McpChannel` decides it, ai `CommandListing.Ai(CommandPalette.ForAiTools(item), AppVerb)`, which is
  false in a window of axPass or axClip), the commands of `_commands` with a key as `KeyCommand`s (label
  `MenuText.Plain`, `Applies` from `When` and false while a notice shows, `KeyOnly` from `_keyOnly`), with two
  further keys alone (Ctrl+Period beside the command of Shift+F10, and Ctrl+Slash for Ask about this window,
  AX-8.15), through `CommandListing.Rows`, with `AppName` and the open notice's title; while the key test runs
  (AX-8.12), the title is "Key test" and every row's `now` is empty. And
  `internal string? Choose(string label, string menus)`: the entry is found by
  `CommandPalette.Find(MainMenuStrip, label, menus)`, the one function that finds an entry by exactly its name and
  its menus (`PaletteEntry`, which `CommandPalette.Commands` gives, carries neither). Null gives the
  refusal's text, "no menu item <label> in <menus> now". Otherwise the entry's item is clicked by the window's one
  click, `ClickFor(CommandOrigin.Choose, ...)` (CL-4.7), as the channel's `Menu` clicks it: `PerformClick`, and for
  an item with `CheckOnClick`, `Announcer.Raise` of `MenuText.CheckSaid` ("<name>, checked" or "<name>, not
  checked"); `Choose` then returns null.
- `AutomationChannel` (WP-B2): `Execute` answers the line `commands data` before its `switch`, with the text of
  `CommandListing.Write(_form.CommandRows())` itself. That text is the JSON object already; through
  `JsonSerializer.Serialize(result, Plain)`, where every other answer goes, it would become a JSON string literal,
  which `CommandListing.Read` reads as none, and every `do` would fail. `choose <json>` answers an object through
  the serializer as the other requests do: `new { ok = true }` when `_form.Choose` returned null,
  `new { error = <its text> }` otherwise. `ParseKey` takes a key's code from `KeyChord.Code` in place of its own
  table (the same names and codes, and the five new names).

No window's behaviour changes: the menu items, the keys, the palette, the AI tools' list and `check commands` read
what they read before.

### 6.3 `src/AxAutomate.Core`

Changed files:

- `Workflow.cs` (WP-B3): `public sealed record Goal(string Title, int FirstLine, int LastLine, bool Stable, bool
  Named, bool Check)` (`Named` false for the one goal of a script with no goal or check line; `Check` true for a
  section that a check line starts); `Workflow` gains the members `IReadOnlyList<Goal> Goals { get; init; } = []`
  (the sections, goals and checks, in the script's order) and `string Needs { get; init; } = ""`, no change to its
  constructor.
- `ScriptReader.cs` (WP-B3): `GoalPrefix = "Goal:"`, `GoalOf(string comment)`, `CheckPrefix = "Check:"`,
  `CheckOf(string comment)`, `StableWord = "Stable"`, `NeedsPrefix = "Needs:"`, `NeedsOf(string comment)`;
  `Goals(IReadOnlyList<string> lines, string title)` (WF-3.1 to WF-3.5, lines numbered from 1);
  `GoalAt(IReadOnlyList<Goal> goals, int line)` (null in the setup); `IsInput(string line)` (whether a script's line
  is an input step, section 3: its step word, after an `iffound`, is `do`, `keys`, `type`, `menu`, `start`,
  `launch`, `adopt`, `use` or `close`, or the line starts `mcp call`); `Parse` fills `Goals` and `Needs`, ends the
  header at a goal or check line and keeps a header's `Stable` line out of `Covers`. `Covers` still holds the needs
  line, as the window shows it today. (WP-B4): `Values` gives `do` the key `command`.
- `ScriptLines.cs` (WP-B4): `Known` holds `do`.
- `StepText.cs` (WP-B4): `"do" => "Do the command " + step.Value("command")`.
- `ScriptRunner.cs` (WP-B3, WP-B4, WP-B11, WP-B15, WP-B16): `public sealed record PlayOptions(IReadOnlyList<Goal>?
  Goals = null, bool Trace = false, bool Convert = false, bool AiTools = false)`; `Play` gains a last parameter
  `PlayOptions? options = null`; `ScriptResult` gains the members `Goals` (`IReadOnlyList<GoalResult>`), `Commands`
  (`IReadOnlyList<WindowCommands>`, the snapshots), `Trace` (`IReadOnlyList<TraceSection>`) and `Conversions`
  (`IReadOnlyList<Conversion>`), each empty by default, no change to its constructor. `Play` prints the section
  lines of 5.1 through `output` and into the log.
- `Run.cs` (WP-B4): the step `do` (`Do(string name)`, through `DoStep.Decide`); `check commands` also asks
  `commands data` and keeps the listing; `IReadOnlyList<WindowCommands> Snapshots`. (WP-B11):
  `IReadOnlyList<string> TakeTraceEvents()`, every window's events since the last call as trace lines. (WP-B15):
  `bool Converting` and `IReadOnlyList<(int Press, string Name)> TakeConversions()`. (WP-B16): `bool AiTools`, the
  preamble before the first input step, and the tools' call in the case `do`; as built, also `bool TakeThroughTools()`
  (whether the line just played made a call of the tool) and `IReadOnlyList<string> TakePreambleLines()` (the
  preamble's steps as the report shows them, once), which `ScriptRunner.Play` reads after each line, and in
  `Broker.cs` `string? TakeFirst()`, the first answer waiting, whatever it holds.
- `Window.cs` (WP-B16): `public sealed record TakeState(...)`, `TakeState Hold()` and `void Resume(TakeState held)`.
  `Hold` fetches and gives what the expectations have taken so far: the taken indexes, the last match and where it
  ended, the last batch, the last text's time, and how many events there are. `Resume` puts all of that back and
  then takes every event recorded after the held count. A run of steps played between the two leaves the script's
  pending events pending and its own events taken.
- `Native.cs` (WP-B4): `VirtualKey` takes a key's code from `KeyChord.Code`.

New files:

```csharp
// GoalResults.cs (WP-B3)
public sealed record GoalResult(Goal Goal, string Outcome, int FailedLine, string Detail)
{
    public int Inputs { get; init; }          // WP-B16: the section's input steps that were played
    public int ToolInputs { get; init; }      // WP-B16: those of them that ran through the AI tools
}
public static class GoalResults
{
    public const string Passed = "passed", Failed = "failed", NotRun = "not run";
    public static IReadOnlyList<GoalResult> Of(IReadOnlyList<Goal> goals, int failedLine, int lastLinePlayed, IReadOnlyList<string> problemsAfter);   // WF-3.9
    public static IReadOnlyList<GoalResult> WithFailed(IReadOnlyList<GoalResult> results, string title, string detail);   // WP-B11: one section failed after the steps (its trace)
    public static IReadOnlyList<string> Lines(IReadOnlyList<GoalResult> results, int setupFailedLine);   // the lines of 5.1 from "Goals:" on
    public static string Counts(IEnumerable<GoalResult> results);    // "1 passed, 1 failed, 1 not run"
}

// ScriptApps.cs (WP-B3)
public static class ScriptApps
{
    // For each line of a script, numbered from 1 at index 0: the name of the app whose window a step on that line reaches, or null when
    // it cannot be read: "start <verb> ..." names the app of Dispatch.Apps with that verb and makes its window the current one, a start
    // whose first word is no verb and "adopt" make an unknown one, "use <n>" the n-th of them, an "iffound" before a step is passed over.
    public static IReadOnlyList<string?> ByLine(IReadOnlyList<string> fileLines);
}

// DoStep.cs (WP-B4)
public enum DoKind { Press, Choose, NoData, Notice, Name, NoAct }
public sealed record DoDecision(DoKind Kind, string Chord, ListedCommand? Row, string Detail);
public static class DoStep
{
    public static DoDecision Decide(WindowCommands? listing, string name);   // WF-4.1 to WF-4.5, WF-4.9
}

// CommandInventory.cs (WP-B5)
public sealed record InventoryApp(string App, IReadOnlyList<ListedCommand> Commands);
public static class CommandInventory
{
    public const string Format = "axkit-window-commands 1";
    public static IReadOnlyList<InventoryApp> Read(string json);                     // an unreadable text gives none
    public static string Write(IReadOnlyList<InventoryApp> apps);                    // 5.3, sorted
    public static bool Kept(ListedCommand row, string temp, string profile);         // WF-2.8
    public static ListedCommand Stored(ListedCommand row);                           // without checked and now
    public static IReadOnlyList<InventoryApp> Merge(IReadOnlyList<InventoryApp> apps, WindowCommands snapshot);        // WF-2.9; of a snapshot with a notice, its rows of keys alone
    public static IReadOnlyList<InventoryApp> Rewritten(IReadOnlyList<InventoryApp> apps, IReadOnlyList<WindowCommands> seen, IReadOnlySet<string> wholeApps);
    public static bool Holds(IReadOnlyList<InventoryApp> apps, string app);          // whether the app has a row: one without is not checked, WF-2.10
    public static IReadOnlyList<string> Stale(IReadOnlyList<InventoryApp> apps, WindowCommands snapshot);              // WF-2.10, one line a row; none for an app without rows
    public static IReadOnlyList<ListedCommand> Of(IReadOnlyList<InventoryApp> apps, string app);                       // an app's rows
    public static CommandResolution Resolve(IReadOnlyList<InventoryApp> apps, string? app, string name, out ListedCommand? row);   // WF-2.11; for null, app by app
    public static ListedCommand? OfChord(IReadOnlyList<InventoryApp> apps, string? app, string chord);                 // the one row of the app that holds it, WF-7.3; null for null
    public static ListedCommand? OfMenuText(IReadOnlyList<InventoryApp> apps, string? app, string text);               // the one row of the app a menu step's text answers, WF-7.3
}

// Traces.cs (WP-B10)
public sealed record TraceSection(string Heading, IReadOnlyList<string> Lines);   // Heading: "Setup", "Goal: <title>" or "Check: <title>"
public sealed record TraceDifference(string Heading, int Line, string Kept, string Now);   // Line 0: the section has no kept trace
public static class Traces
{
    public const string Setup = "Setup";
    public static string Heading(Goal goal);                                         // "Goal: <title>" or "Check: <title>"
    public static bool MayKeep(string scriptFromRepository, string needs);           // WF-5.6: by the script's folder and whether it has a needs line
    public static string? InputLine(string scriptLine, string? detail);              // 5.4; null for a step that is no input, and for a skipped iffound
    public static string EventLine(int window, int windows, Event e);                // 5.4
    public static string Clean(string text, string temp, string profile);            // WF-5.3
    public static IReadOnlyList<TraceSection> Stable(IReadOnlyList<TraceSection> sections, IReadOnlyList<Goal> goals);   // the sections of the goals and checks marked stable, WF-5.4
    public static string Write(string script, IReadOnlyList<TraceSection> sections); // the file of 5.4
    public static IReadOnlyList<TraceSection> Read(string text);
    public static IReadOnlyList<TraceDifference> Compare(IReadOnlyList<TraceSection> kept, IReadOnlyList<TraceSection> now);   // WF-5.5: at most one for each section of now
    public static string PathFor(string scriptFromRepository);                       // tests/scripts/axDown/04-x.txt as tests/traces/axDown/04-x.trace.txt
}

// WorkflowRecord.cs (WP-B12)
public sealed record RecordedRun(string Result, string Version, string Commit, string Date);
public sealed record RecordedTier(RecordedRun Last, RecordedRun? Passed);
public sealed record RecordedGoal(string Workflow, string Goal, string Script, IReadOnlyDictionary<string, RecordedTier> Tiers);
public static class WorkflowRecord
{
    public const string Format = "axkit-workflow-record 1";
    public const string Desktop = "desktop", Nvda = "nvda", AiTools = "ai-tools";
    public static IReadOnlyList<RecordedGoal> Read(string json);
    public static string Write(IReadOnlyList<RecordedGoal> goals);                   // 5.5, sorted
    public static IReadOnlyList<RecordedGoal> With(IReadOnlyList<RecordedGoal> goals, string workflow, string goal, string script, string tier, RecordedRun run);   // WF-6.1
    public static IReadOnlyList<RecordedGoal> Pruned(IReadOnlyList<RecordedGoal> goals, IReadOnlyList<(string Workflow, string Goal, string Script)> existing);    // WF-6.2
}

// Reference.cs (WP-B7)
public sealed record ReferenceScript(string Path, IReadOnlyList<string> Lines);     // Path from the repository's root, forward slashes
public sealed record ReferencePage(string Path, string Text);                       // Path from the repository's root; Text with line feeds
public static class Reference
{
    public static IReadOnlyList<ReferencePage> Pages(IReadOnlyList<ReferenceScript> scripts, IReadOnlyList<InventoryApp> inventory);
    public static string Group(string scriptPath);                                  // "axDown", "AI tools", "NVDA only"
    public static string PageName(string group);                                    // "axDown", "ai-tools", "nvda-only"
    public static IReadOnlyList<string> GoalLines(IReadOnlyList<WorkflowStep> steps, IReadOnlyList<string?> apps, IReadOnlyList<InventoryApp> inventory);   // WF-7.3, WF-7.4
    public static string Heard(string text);                                        // WF-7.4: the words as read: without its mark, its edges, an open bracket, a joining word last; "" when no letter or digit is left
    public static bool IsWhole(string text);                                        // WF-7.4: reads as a whole announcement; false for a text cut short ("something with") (WP-B8b)
    public const string EveryWindow = "In every window";                            // WF-7.6: the commands page's heading for the rows of every app (WP-B8b)
}

// Converter.cs (WP-B15)
public sealed record Conversion(int Line, int Press, string Name);                 // the line of the file, the press on it from 0, the canonical name
public static class Converter
{
    public static string? NameFor(WindowCommands? listing, string chord);          // WF-10.1; null when the press stays
    public static IReadOnlyList<string> Rewrite(IReadOnlyList<string> fileLines, IReadOnlyList<Conversion> conversions);   // WF-10.3
}

// AiToolsMode.cs (WP-B16)
public static class AiToolsMode
{
    public static readonly IReadOnlyList<string> Preamble;                         // WF-9.3, the lines below
    public static bool Covers(IReadOnlyList<string> fileLines);                    // WF-9.2: which scripts the mode plays
    public static bool MayRun(WindowCommands listing, ListedCommand row);          // WF-9.4: no notice, not a key alone, ai true, mark not act
    public static bool IsBusyRefusal(string answer);                               // WF-9.4: the refusal of MC-7.8, which is waited out
    public static bool IsBusyAtTakeUp(string answer);                              // WF-9.4: accepted, then not taken up for the user at work; sent again once
    public static string Call(ListedCommand row);                                  // the mcp call line of WF-9.4, for the window the preamble's answer named
}
```

The preamble's lines. Each is a line of `tests/scripts/mcp/02-run-commands.txt` as it stands, in that script's
order; the test `The_preamble_is_the_scripts_own_lines` reads the script from the repository and fails when one is
not, so a changed notice is changed in the script and shows here at once:

```
menu Let AI tools read and open axKit
expect state "notice":"Turn on AI tools?"
keys enter
expect said AI tools on
try said Not in a test run
mcp start
mcp call axkit_windows
expect state "notice":"axAutomate asks to use axKit"
wait 1500
keys tab
expect focus I understand
keys space
expect said ! checked
keys tab
expect focus &Allow
keys enter
expect said axAutomate may read axKit
expect mcp "app":
menu Let AI tools run commands
expect said AI tools may run commands: each is said as it runs
```

The line `expect mcp "app":` is the script's `expect mcp "app": "axDown"` without the app's name: the test takes a
line of the preamble for the script's when the script's line starts with it.

### 6.4 `tools/AxAutomate` and the scripts

`Program.cs` gains the lock and the options (WP-B0, B6, B8, B11, B13, B15, B16), the options read into one record
`RunOptions` in `Options.cs`:

```
AxAutomate <script or folder> (--exe <AxKit.exe> | --build) [--report <file.md>] [--keep] [--nvda]
           [--repository <folder>] [--inventory check|write|rewrite|off] [--traces check|write|off]
           [--record] [--convert] [--ai-tools] [--live]
AxAutomate --reference [--check] [--repository <folder>]
```

`--live` (2026-11-22): a run of a folder leaves out its live scripts, the ones whose `# Needs:` line names Claude
Code signed in, the network or GitHub (`ScriptReader.IsLive`), and says how many, since they start the real Claude
Code on the user's account or ask a server; `--live` plays them too. A script named by its own file runs whatever
it needs. `nvda-pass.ps1` and `tools/upstream.ps1 -Verify` pass it, since they mean every script they gather;
`tools/window-passes.ps1` and `tools/nightly.ps1` never run a live script.

The repository is `--repository`, or the folder above the program that holds `AxKit.sln`. `FindRepository` returns
null when no folder above the program holds it. Today it falls back to the current directory; that must go, since a
runner built into `%TEMP%`, as `nvda-pass.ps1` builds it, would read and write the inventory, the traces and the
workflow record under whatever folder it was started in. The `--build` step keeps its own fallback to the current
directory. WP-B6 makes this change, in `Options.cs`. Without a repository the inventory, the traces and the
workflow record are off, and `--record`, `--convert` and `--reference` are refused with exit code 2. `--inventory`
and `--traces` are `check` by default; `--traces` is off under `--nvda`, `--ai-tools` and `--convert`. The exit
codes are those of WF-11.5.

`--reference` plays nothing: it reads every script of `tests/scripts` and `tests/nvda-scripts` and the command
inventory, calls `Reference.Pages`, and writes each page (or, with `--check`, compares each with the file, deletes
nothing, and exits with 1 naming the first page that differs or is missing or is left over in
`docs/axKit/workflows`). One sentence holds for the line ends, in the generator, the writer and the check:
`Reference.Pages` gives lines that end with a line feed; `ReferenceRun` writes each page with CRLF, UTF-8 without a
byte order mark; `--check` compares with line ends aside.

Each feature's code is a file of its own beside `Program.cs` (`RunLock.cs`, `Options.cs`, `InventoryRun.cs`,
`TraceRun.cs`, `RecordRun.cs`, `ReferenceRun.cs`, `ConvertRun.cs`; `GraphRun.cs` and `BoundsRun.cs` for the workflow
graph), static classes that `Main` calls after each script and at the run's end, so that `Main` stays a list of calls.

`RunLock.cs` (WP-B0): one window run at a time on a computer (WF-D-29). `public static Mutex? Take(TimeSpan limit,
Action<string> say)` waits for the named system mutex `AxKit.AxAutomate.WindowRun`. When it does not get it at once
it says, once, "Waiting for another window run to end: one plays at a time, since the clipboard and the system
hotkeys are shared." A mutex abandoned by a run that was ended counts as taken. After the limit it gives null.
`Main` takes the lock after the build and before the first script, on its own thread, and releases it in the
`finally` that puts the clipboard back; null ends the run with "Another window run did not end within 60 minutes."
and exit code 2. `--reference` takes no lock. The NVDA tier takes the same one, so no hidden run starts under a
pass and no pass starts under a hidden run.

`tools/workflows.ps1` (WP-B8) becomes a few lines. It keeps its `$root` line, which finds the repository from the
script's own path (`test.ps1` starts it by its full path from whatever folder it was started in), and runs
`dotnet run --project (Join-Path $root 'tools\AxAutomate\AxAutomate.Runner.csproj') -c Release --nologo -v q --
--reference --repository $root` (with `--check` for `-Check`), the exit code passed on, the build servers shut down
at its end. Its page-writing code in PowerShell goes; a script without a label still fails it (the generator refuses
one).

`tools/upstream.ps1` (WP-B6, and WP-B11 for the second option): its one call of the runner, in `-Verify`, gains
`--inventory off --traces off`. It asks whether an outside program's checks still pass and reads the exit codes 0,
1 and 2; the windows' menus and the kept traces are not its question (WF-D-13).

`nvda-pass.ps1` (WP-B13) adds `--record --repository $root` to its call of the runner, and its filter of the
runner's lines also lets through the lines that start `Goals:`, `The command inventory` and `Recorded` (its first
pattern gains `|Goals:|The command inventory|Recorded`). What `FrontTests.Nvda_pass_shows_the_stopped_runs_lines`
pins stays.

### 6.5 How a key reaches a how-to line

1. The key table: `EditorKeys.GoToLine = Keys.F12` (`src/AxKit.Forms/EditorKeys.cs`), the one place
   the key is written.
2. The command: `new Command("&Go to line...", EditorKeys.GoToLine, ShowGoToLine)` in `TextEditorForm`, added with
   `Add`, which makes its menu item through `AxKitForm.Item`: the item shows "F12" and, new, holds the key
   (`CommandPalette.Keyed`).
3. The listing: `AxKitForm.CommandRows` reads the item (`CommandPalette.Entries`, `KeyOf`) and the command list, and
   `CommandListing.Rows` gives the row `Go to line`, menus `Navigation`, key `f12`.
4. The step: `do Go to line` asks `commands data`, `CommandNames.Resolve` finds the row, the runner presses
   `f12` through the tier's keys. The script never names the key.
5. The inventory: the same run, with `--inventory write`, puts the row into `docs/axKit/window-commands.json`; a run
   without it says the inventory is stale when the row differs.
6. The page: `tools/workflows.ps1` runs `Reference.Pages`, which writes the step as "Press F12 (Go to line;
   Navigation menu)." into `docs/axKit/workflows/axDown.md`.
7. axHelp: the next build embeds the page; `AiDocumentation.Write` puts it into Claude's working folder as
   `documentation\workflows-axDown.md`; the rule file tells Claude to give the goal's steps as written.

When the constant becomes another key: steps 2 to 4 follow by themselves and the script passes; step 5's check says
the inventory is stale; writing it and the pages again (WF-11.4) makes steps 6 and 7 follow. No script, no how-to and
no rule is edited by hand.

### 6.6 What is tested where

- `tests/AxKit.Tests` (unit): `CommandNamesTests`, `KeyChordTests`, `CommandListingTests`; `McpTests` (the sentences
  of `McpRun.Find`, unchanged, and the whole line); `KeyTests`, which tests `BundleKeys.Text` today (`Chord`);
  `CommandPaletteTests` (`Keyed`, `KeyOf`); `AiDocumentationTests`, `AiToolsRuleTests` (WF-8).
- `tests/AxAutomate.Tests` (unit): `GoalTests` (the reader of goals and checks, the results, the report's lines,
  `ScriptApps`), `DoStepTests`, `CommandInventoryTests`, `TraceTests`, `WorkflowRecordTests`, `ReferenceTests`,
  `ConverterTests`, `AiToolsModeTests`; and over the repository's files: `GoalTests` (no script has two sections of
  one title; every goal holds an input step), `RepositoryWorkflowTests` (every `do` in the inventory; the inventory
  and the workflow record well formed; every stable section with its kept trace and no other trace kept) and
  `AiToolsModeTests` (the preamble's lines are lines of `tests/scripts/mcp/02-run-commands.txt`).
- The hidden desktop (never CI): `do` against live windows, the inventory against live listings, the traces of
  stable sections against kept ones; first for `tests/scripts/axDown/04-find-and-go-to-line.txt`, then app by app.
- NVDA (`nvda-pass.ps1`, the user away): `do` through `SendInput`, heard by NVDA as every `expect said` is. The
  pass this work owes is written down by WP-B4 and made in WP-B18.
- By reading alone: the foreground rules are untouched; nothing of this needs the foreground.

## 7. Decisions

Every decision here was taken by the assistant on 2026-11-15 for the user to confirm: the user asked that the
recommended option be taken on each and written down with its reason. WF-D-1 to WF-D-10 were taken before this
document was written; WF-D-11 to WF-D-22 while writing it, from what the code allows; WF-D-23 to WF-D-30 after two
reviews of it, where a review found a decision of the user's taken without being written down, or where the two
reviews pulled against each other. The reviews also changed WF-D-1, 6, 7, 9, 13, 19, 20 and 22; each of those says
what it was before. One item of the priorities list holds them all for the user to confirm (section 8, WP-B0).

- **WF-D-1 One canonical name: the label, with its menus where the label is not unique; no second identifier.** The
  user hears labels; the palette and the AI tools already name commands by them; a second name would be one more
  thing to keep in step, and nothing would ever say it aloud. One resolver in one place for every caller. The user's
  item proposes an identifier that survives a label's edit; this declines it for now, and the review asked that the
  cost be said: a renamed label is changed by hand in every `do` line that names it (the cheap check names each),
  starts a new entry in the workflow record, and changes the name the AI tools take. To reverse: a field on
  `Command` and on the menu item's record, carried through the listing. (Taken by the assistant on 2026-11-15 for
  the user to confirm.)
- **WF-D-2 A generated inventory of the windows' commands, from live windows, checked in.** Constructing forms in a
  test is not safe (a hotkey window's handle registers a system hotkey and writes the sign-in start entry) and not
  true (the menus differ by state); the channel can list a window as it is. CI checks only what a file can show,
  that every command a script names is in the inventory; that the inventory is current is checked whenever the
  window checks run. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-3 The unit is a goal, a comment line inside a script.** A script of fourteen goals has one label today; a
  goal line gives each its name without a second file, a second suite or a duplicated step, and a script without one
  stays one goal, so nothing has to move at once. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-4 A failing step still stops its script.** The goals of a script share their windows and their state; a
  goal played after a failed one would fail for the wrong reason. The report names what passed, what failed and what
  was not run. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-5 `do` presses the command's own key, read from the window as data.** It checks the path a user takes, and
  a changed key needs no change in a script. A command with no key runs by its menu item, and the trace says so.
  (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-6 A trace per section in every run's report; kept in the repository only for the sections marked
  `# Stable`, and there any difference fails.** The first form of this decision kept every goal's trace and called a
  difference a note. The review found three faults in it: a kept trace of everything a window says can carry the
  user's clipboard, the names of his installed programs and a shell's output into a committed file (root rule 6); a
  note over 133 files is a count nobody reads; and a stable goal without a kept trace passed silently. So keeping is
  opt-in, a mark is earned by three equal runs and refused where a window reads the computer or the user's session,
  and a kept trace that differs or is missing fails. Tonight's marks, in one script of axDown, prove the mechanism:
  which goals are the core mechanics to keep stable is the user's to name. (Taken by the assistant on 2026-11-15 for
  the user to confirm.)
- **WF-D-7 A record per section and tier, written by the command line; the narrative record stays for what a pass
  found.** When a goal last passed should not depend on a sentence written by hand, nor on remembering an option:
  `nvda-pass.ps1` always records, and the documented run of an app's folder carries `--record`. The first form wrote
  the pages again after every recorded run, since they named each goal's last pass; now no page is written from the
  record, and a recorded run changes that one file. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-8 The reference is generated in C# beside the engine, and is an index with one page per group.** The
  generator reads scripts through the engine's own reader, so the page and the run cannot read a script differently,
  and it is unit tested. One page was the decision; it cannot hold: the steps of 133 scripts are several hundred
  kilobytes, and Claude's Read tool takes a file of that size only in pieces. So `docs/axKit/workflows.md` is the
  index, small enough to read whole, and `docs/axKit/workflows/<group>.md` hold the steps; the same split serves a
  person moving by headings. `tools/workflows.ps1` keeps its name and its `-Check`. (Taken by the assistant on
  2026-11-15 for the user to confirm.)
- **WF-D-9 No new AI tool; a run through the AI tools is a mode of the command line, step by step.**
  `axkit_window_run` already runs a command by its name. The first form ran a goal through the tools only when every
  input step of it was a command; real goals type and press Enter, so none qualified, and the mode would have
  monitored nothing the user does. Now every `do` step the tools may run goes through them and every other step by
  the keyboard, and the report says how many did. Tonight the mode plays scripts of one window, where the pairing's
  notice has one place to show; scripts of several windows wait until that has been tried. (Taken by the assistant
  on 2026-11-15 for the user to confirm.)
- **WF-D-10 Existing scripts move to `do` by a converter, chord by chord, only where exactly one command has the
  chord; the live window decides.** The decision named the inventory as the converter's source. The inventory is the
  union of a window's states and cannot say which state a script is in at a press (Ctrl+F in a notice is the
  notice's find, not the menu's command), so the converter asks the window at the moment of the press and merges
  the rows it used into the inventory. The rule is the decision's. (Taken by the assistant on 2026-11-15 for the
  user to confirm.)
- **WF-D-11 The inventory is by app, not by window state.** Each app has one kind of window; its commands differ
  with the focus (the Context menu) and the mode (the viewer). A level of named states would need a script per state
  to visit it; the union of what the snapshots saw needs none, and a name is checked against the union. What is
  state (`checked`, `now`) is not kept. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-12 Snapshots at `check commands` and at `do`, and a numbered list entry is told by its label.** Both steps
  have the listing in hand, so no step word is added. The entries of recent files, open windows and recently closed
  ones are the user's things, not commands; they all carry `AxKitWindows.Label`'s number, so one rule in one pure
  function leaves them out, with no mark to add where each list is made. A list past its thirty-fifth entry has no number
  and would enter; no list is that long in a test run. (Taken by the assistant on 2026-11-15 for the user to
  confirm.)
- **WF-D-13 A stale command inventory has an exit code of its own, 3: never a script's result, and never the 1 of a
  failed script.** A script that passed did pass; the inventory is a generated file, and a stale one is said as a
  stale page is said by `test.ps1`. The first form used exit code 1. The review found that `tools/upstream.ps1
  -Verify` would then record Claude Code or Git as failed with an empty list of failures, and that the agents of the
  axClaude redesign, who do not have this document, would read it as a failed script. So: the code 3; words at the
  run's end that explain themselves; an app without rows in the inventory is not checked, which keeps axClaude out
  of the check until the redesign has merged; and `tools/upstream.ps1` runs with `--inventory off`, since its
  question is the outside program. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-14 Merge by default, rewrite on request.** A merge can be made by any run and follows a changed key at
  once; only a rewrite over an app's scripts can know that a command is gone. A row that lingers names a command no
  window has until the next rewrite; `do` against the live window still fails on it. (Taken by the assistant on
  2026-11-15 for the user to confirm.)
- **WF-D-15 Keys with no menu item are folded by label, and `now` says which chord acts.** Otherwise "Redo" would be
  two commands and `do Redo` ambiguous, and a key that acts only in one pane would be pressed where it does nothing.
  For a menu row the label of the command behind a chord is not compared (the Context menu's item and the key's
  command may be worded differently for the same thing); for a row of keys alone it is. (Taken by the assistant on
  2026-11-15 for the user to confirm.)
- **WF-D-16 `axkit_window_run` also takes the whole line as its command.** The canonical name is one text; a how-to
  says "Heading, Navigation, Next", and Claude can pass what it reads. Nothing that resolved before resolves
  differently; no gate changes. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-17 A goal is known in the record by its workflow's label and its title.** `nvda-pass.ps1` plays copies of
  the scripts in another folder, so a path would not find the entry; labels are unique by test. A renamed label or
  title starts a new entry and the old one is dropped. (Taken by the assistant on 2026-11-15 for the user to
  confirm.)
- **WF-D-18 A failure after the steps fails every goal that had passed.** An ERROR in a log or a window that ended
  by itself belongs to no one goal; recording a pass beside it would be untrue. (Taken by the assistant on
  2026-11-15 for the user to confirm.)
- **WF-D-19 Traces are kept on the hidden desktop alone, and never where a window reads the computer, the user's
  session or a model.** The NVDA tier adds what the foreground does, the AI tools tier changes the inputs, and a
  model's words differ every run. The clipboard, the installed and the running programs, a shell's and git's output
  and the sound devices are the user's or the computer's: the folders whose windows show them, and every script with
  a `# Needs:` line, may keep no trace. The first form decided by the words "Claude Code" in a script's free-text
  needs line; now it is a rule in code by folder, with a check. (Taken by the assistant on 2026-11-15 for the user
  to confirm.)
- **WF-D-20 The pages hold the how-to and nothing of the checks.** What a script covers is in its header, written
  for the developer; the pages are for a user and for Claude, and each kilobyte of them is read by a model and heard
  through a screen reader. The first form still put the script's path, its needs line, each tier's last result with
  date, version and commit, and a sentence about the AI tools under every goal. Now none of that stands on a page,
  and a check's steps are on none: the index names workflows and goals, a script is found by its label, and when a
  goal last passed is in the workflow record. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-21 axAutomate's window does not change tonight.** It reads a goal line as a note, plays `do`, and says what
  it said; goals in its rows and its end sentence are a change to what a window says and wait for the user. (Taken by
  the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-22 The files of the axClaude redesign are not touched, and axClaude and axHelp join after it.** That work
  runs the same night in `src/AxClaude`, `src/AxClaude.Core` and the console window's shared files, and changes
  axClaude's menus and scripts. The menu items made by hand there and in axTerminal and axTask with a display string
  of a real key keep a key text without a key as data until it has landed (section 10). The review added the rest:
  the first command inventory and the converter pass over `tests/scripts/axClaude` and `tests/scripts/axHelp`, and
  an app without rows is not checked (WF-2.10), so the redesign's own window runs are never told of a stale file
  they know nothing of. Both apps join in one package after the redesign has merged (WP-B17). (Taken by the
  assistant on 2026-11-15 for the user to confirm.)
- **WF-D-23 The user meets the how-to through axHelp's Claude and as the files themselves.** The pages are
  `documentation\workflows-<app>.md` in Claude's working folder, plain files that open in axDown or axPreview. No
  Help menu item and no section of a user guide tonight: either changes what a window shows or what a guide says,
  and where he wants to meet the how-to is his to say (the requirements' open question). To reverse: a Help item
  that shows the app's page in a notice, as User guide does. (Taken by the assistant on 2026-11-15 for the user to
  confirm.)
- **WF-D-24 axHelp explains, then offers to do the steps.** Taken by the assistant on 2026-11-15 as "explains only";
  reversed by the user on 2026-11-16 through the question tool ("Explain, then offer to do it") and built on
  2026-11-24: the guides' rule lets Claude offer once, with `AiCaution.OfferToDo`, and run nothing before the
  user's yes (ai-help.md AH-7.3b).
- **WF-D-25 The F1 texts are generated whole from the commands; the hand-written part keeps only what they cannot
  say.** Taken by the assistant on 2026-11-15 as "stay written by hand tonight"; reversed by the user on 2026-11-16
  ("Generate the whole text"). Built in two steps: since 2026-11-23 every F1 text ends with "Every key of this
  window", made as F1 opens from the window's command list (bundle SPEC.md AX-7.2a, `KeyReference`), so no key can
  be missing; on 2026-11-25 each app's own part was trimmed to what that list does not say (what a key does in
  context, the order of a task, the words to expect, the keys no command carries), and every text gained one
  sentence, under F1's line, pointing to the user guide in the Help menu and, with AI help on, to Ask about this
  window, which answers from the app's how-to page (`workflows-<app>.md`, WF-8.1; `BundleHelp.HowToLine`). The check that
  held the hand-written part to every key of the table (`KeyTests`) went with it. A pass by ear of the new texts is
  owed.
- **WF-D-26 The goals' titles, and where each goal begins, were chosen by the build agents from the scripts'
  comments.** They are the user's vocabulary: a title is one comment line in a script and his to change, and the
  index (`docs/axKit/workflows.md`) is where he reads them all at once. (Taken by the assistant on 2026-11-15; on
  2026-11-16 the user chose a consistency pass by the assistant before he hears them, and it gave one wording to
  each action that several apps share: refresh, undo, find by typing, move by page and by paragraph, the key test,
  the text size, the keyboard shortcuts, the startup programs, 38 lines in all.)
- **WF-D-27 The work runs on a branch and reaches `main` when its checks have passed, the NVDA pass among them.**
  One review asked that this document be committed to `main` and every worktree made from there; the other that the
  work stay on a branch, unpushed, until the user's word. The root notes settle it: larger work runs on a branch and
  merges when its checks have passed, and a change to key handling is checked with NVDA. So the work is on the
  branch `workflows`; `main` does not move while the packages land; the merge follows a whole `.\test.ps1`, the
  window runs and a pass of `nvda-pass.ps1`, made by the session that integrates the night's work while the user is
  away (he was told that NVDA would speak; were he at the keyboard, the pass would wait for his word).
  The merge does not wait for his word beyond that: he asked to find the work done in the morning, and his word
  gates a release, not a merge. If no pass can be made, the branch waits and the pass stays written down as owed.
  (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **WF-D-28 A section without an input step is a check, and a check is no how-to.** One review asked that such a
  goal be shown with its "You hear" lines, the other that it be left out. A how-to with nothing to do is no how-to,
  and the same holds for the steps that test a refusal or a regression: they stand under `# Check:` lines, pass,
  fail and are recorded as goals are, and are on no page. A test fails on a goal without an input step, so the
  choice is made where the script is written, not by the generator. (Taken by the assistant on 2026-11-15 for the
  user to confirm.)
- **WF-D-29 One window run at a time on a computer, by a lock in the runner.** One review asked that all window
  runs take turns, the other that only some folders do. Every run reads the clipboard before its first script and
  puts it back after its last, so any two runs at once can lose the user's clipboard and fail each other's copy
  steps; axStart's scripts take a system hotkey; axMedia and axPreview keep one window. Agents in separate worktrees
  cannot see each other, so a rule they would have to keep by arrangement is a rule that fails at night: the runner
  itself waits for a named lock. It is no throttle: builds and tests run side by side as before. (Taken by the
  assistant on 2026-11-15 for the user to confirm.)
- **WF-D-30 The edit control's own keys are no keys as data.** The review found the editor shell's Cut, Copy, Paste
  and Select all without a key as data and offered either to give them one or to leave them. They are left:
  Ctrl+X, Ctrl+C, Ctrl+V and Ctrl+A are the EDIT control's keys, not the window's, as the source says where the
  items are made; and Ctrl+C is also the key of "Copy the detail's value" where the details pane has the focus, so
  the listing would say that Copy's key does not act in the text, and `do Copy` would fail where the key works. They
  keep their key text, the commands page shows it, scripts press them with `keys`, and `do Copy` chooses the menu
  item. To reverse: a `When` on such an item, so that `now` can know where the focus is. (Taken by the assistant on
  2026-11-15 for the user to confirm.)
- **WF-D-31 The how-to is written for the ear, in words of its own.** The first pages were read after the generator
  was wired, and they read like a test: "Use window 4.", "Take the window the app opened.", "Open axClaude with read
  -- --model haiku.", "Press Pagedown.", seven "Press Tab." in a row, "You hear: 2. orchard (.", "Type }." (is the
  full stop typed?), "The tool answers: "app": "axDown".", and a calculator's how-to of nineteen steps with not one
  "You hear", because one number was too short to keep. A blind user has these pages read to him and axHelp's
  Claude answers from them, so each of these is a sentence he would have to decode. The choices (WF-7.3, WF-7.4,
  WF-7.6, WF-7.9): a window is named by its app, since no person counts windows; what a start carries only for the
  check is left out; a key is named as a keyboard names it, and one key reads one way on a page; typed text ends
  its line with nothing added; equal presses in a row are counted; a text the script cut short says "words such
  as" instead of sounding whole, by one rule a reader can hold (nothing but a final full stop dropped, and no
  joining word last); one word is kept unless it only repeats the line before it; the rows every window shares
  stand once on the commands page, which falls from 103 KB to 58 KB. The words are `Reference`'s own and
  `StepText` is untouched, so axAutomate's window says what it said. Two limits are accepted: the rule for a text
  cut short knows only what the script wrote, so a whole announcement that ends with "to" reads as cut ("Nothing
  to go back"), and a text cut in the middle of a word is not seen; and an option the user could give himself
  (`--add`, `--new`, `--title`) stays in the sentence as the script has it. To reverse any one choice: its switch
  in `Reference` and the test that pins its sentence. (Taken by the assistant on 2026-11-15 for the user to
  confirm.) The maintainer's decision of 2026-11-25 took every proposal of the pages' review and went further
  where this left a limit: the window steps are "Go to the window that opened.", "Go back to the first window.",
  "Open axClip again."; the options a person gives are in words and the check's own are gone (`--claude` too); a
  tool call is what a person asks Claude; a word and the sound of its name are one line; a text cut short is in
  quotes, "something with ... in it"; typed text is in quotes; a count is in words; a workflow of checks alone is
  on no page; a check line may stand above the comment before the first start; a line under a step is indented by
  the number's width. Each is pinned in `ReferenceTests`.

## 8. and 9. What changed elsewhere, and the build plan

Built on 2026-11-15 and 2026-11-16 in the packages WP-B0 to WP-B18, every one done and merged, and the changes
they made to other documents and notes made. Both sections were taken out on 2026-11-23 (the maintainer's
cleanup): the git history keeps them (commit 3f4a9925 and before), and a code comment or a record that names a
package (WP-B13) points there.

## 10. Risks, and what is left for later

Risks:

- **The listing reads the menus as the palette does**, so `do` and `check commands` run every menu's refresh
  (recent lists read settings, axClaude's reads Claude Code's settings file). The palette and `menu` do the same
  today; a refresh that is slow or moves the focus would show first as a script that passes with `keys` and fails
  with `do`. The converter's second run (WF-10.4) is the guard.
- **A row's `now` trusts the commands' `When`.** A window that handles a key in its own `ProcessWindowKey` before
  the command list is not asked; `do` then presses a key the window may take for something else. The step's
  expectations catch it; the row cannot.
- **The Applications key and Shift+F10** open a menu, which needs the foreground on the hidden desktop; a `do` of a
  "Context menu" command there does what the `keys` step did, nothing useful. Scripts keep using `menu` for what
  the menu holds.
- **A stale command inventory is found only by a window run**, never by CI. A key changed without the app's scripts
  run leaves the pages wrong until the next run; WF-11.4 is the rule, and a full pass before a release rewrites it.
- **Rows that linger** (WF-D-14): a renamed or removed command stays in the inventory and on the commands page
  until a rewrite.
- **A row that exists only where a program is installed** (a shell of axTerminal, an install item) makes the
  inventory stale on another computer, and its label would be the computer's. WP-B6 reads the first inventory for
  such rows and names each in `CommandInventory.Kept`; the first one that turns up later is where a mark on the menu
  item itself, beside `CommandPalette.NoEntry`, should take the rules' place.
- **A kept trace is a committed file of what windows said.** The guard is what may be marked at all (WF-5.6), then
  the cleaning, then `PrivacyTests`, and a person reading the file before it is committed. Tonight one file exists.
  Every later mark is a decision, and which goals deserve one is the user's to name.
- **A kept trace fails on any difference.** A section marked too early fails now and then, and a check that fails
  for timing teaches everyone to look past it. Three equal runs are the least; a section that then fails once in
  ten loses the mark.
- **The how-to is as good as the marking.** A goal's steps are read to a user as the way to do the thing. Steps
  that test a refusal belong under a check line; where the agents of WP-B14 left one in a goal, the page tells a
  user to type a wrong value. The index is where the user reads the goals, and a title or a line is his to move.
- **The chord hint can name the wrong command.** A `keys` line not yet moved to `do` is named from the inventory
  alone (WF-7.3), and the inventory does not know where the focus was: Ctrl+C in a field would be named after the
  one command of the app that has Ctrl+C. The converter asks the live window and removes the guess; until a script
  is converted its hints are hints.
- **The pages' size**: axClaude's page may pass 100 KB; Claude reads it by searching for a goal's title, as the rule
  says. If answers suffer, the page splits by workflow; the index's form allows it.
- **Key texts**: `BundleKeys.Text` writes a key's name with `Keys.ToString()`, so Ctrl+Enter may read "Ctrl+Return"
  and Page Down "Next" on a page as they do in a menu today. The reference prints what the window shows; if that
  reads badly, the fix is in `BundleKeys.Text`, a change to what menus say, and so the user's to decide.
- **One window run at a time** (WF-D-29): the runs of a wave queue, and a run that hangs holds the lock until its
  process is ended or its hour is over; a waiting run then ends with exit code 2 and is started again. The redesign's
  agents run from `main`, which has no lock unless its one commit is brought there (WP-B0): until then their runs
  can still meet this work's on the clipboard.
- **Builds side by side**: several agents building `tools\AxAutomate` in one checkout lock each other's `bin`; each
  agent has its own worktree.
- **One main checkout, two projects.** The integrating session commits both this work and the redesign there, and a
  checkout is on one branch at a time. 9.0 lets one branch carry both; if the redesign lands on `main` directly, the
  branch of this work lives in a second checkout or is switched to between patches, and nothing is built or run in
  the checkout while it switches.
- **The night's other work**: the axClaude redesign changes axClaude's scripts and menus while this marks goals in
  them. WP-B14 (a) adds comment lines only and leaves a script whose patch no longer applies; the first inventory
  and the converter pass over axClaude and axHelp, an app without rows is not checked, and both join in WP-B17. If
  the redesign changes a menu of the console window that axTerminal shares, a run of axTerminal's scripts ends with
  exit code 3 and the words that say what to do; WP-B17 rewrites axTerminal's rows as well.
- **The AI tools mode turns the access on in the script's own window**, with twenty-one steps before the first input
  step (a wait of a second added on 2026-11-27: the menu's notice holds its keys, bundle AX-6.4). A script whose first input depends on the exact state its start left (a notice already open, a field in
  hand) may play differently in the mode; such a failure is the tier's finding, not the script's (WF-9.6). Scripts
  of several windows are not covered. The refusal for a user at work (MC-7.8) counts the whole session's last key
  press, so the mode waits it out; a run made while the user types is slower, not wrong.
- **`commands data` is first asked by a live window in WP-B4**, a wave after WP-B2 built it: the runner's read
  steps send bare verbs only. A defect in the request shows there, and WP-B4 may then change the two files that
  answer it.

Left for later, each with where it would start:

- The F1 texts written from the command list (AK-2.3's plan): built (WF-D-25); a pass by ear of the new texts is
  owed.
- A place in a window where the user meets the how-to: a Help item that shows the app's page in a notice, or a
  section of each user guide (WF-D-23).
- Goals in axAutomate's window: a goal row above its steps, "failed at step 7, in the goal Go to a line" at a run's
  end (`StepText.Rows`, `RunSentences.Ended`); the user's to decide (WF-D-21).
- The `menu` step on the resolver (WF-1.8, WF-10.6).
- A key as data on the items made by hand with a real key's display string: axClaude's and axTerminal's Export
  items, the console window's music item, axTask's action keys (WF-D-22), one `CommandPalette.Keyed` call each; and
  the editor shell's Cut, Copy, Paste and Select all, once an item can say when its key acts (WF-D-30).
- Scripts split where a file holds unrelated tasks (goals and checks are marked in every app's scripts since
  WP-B14), and `do` lines in the ten apps' scripts that still press their command keys through `keys` (section 9's
  first paragraph names them).
- Shift+F1's prompt with the current window's goals beside the F1 text (`AiPrompts`); it reads the same folder
  today, so the rule already reaches it.
- Workflow files (`.workflow.json`) with goals (WF-3.8), with the converter of AU-5.2.
- The AI tools tier for scripts of several windows, and for goals with an act command, answered by the run as the
  user would answer; and a tool that plays a goal by its title, if the user wants axHelp to do what it explains.
- The record written by axAutomate's window (AU-D10 would have to change).
- A command identifier that survives a renamed label (WF-D-1), if labels turn out to change often.
- Which workflows are "the core mechanics" to keep stable: named with the user; `# Stable` is where the answer is
  written (WF-5.10).
