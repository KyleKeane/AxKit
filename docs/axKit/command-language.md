# The command language: one name, one mark, one table of who may run what

Asked by the maintainer on 2026-11-16 (his words are in section 9): the commands of every window as one language that
the menus, the keys, the command palette, the automated checks and the AI tools all use, "with permissions about where
the action comes from and whether extra permission is needed", so that a change is "small adjustments to singular
places in code" and "the path to execute the code is well defined".

Most of the ground is built and written in four documents. This one adds no behaviour: it states in one place what is
built, by reading the code, and names what is not. Requirements are numbered CL-n.m; each says where it is built and
what holds it. Where this document and the code disagree, the code is right and this document is wrong; where another
specification and the code disagree, section 10 lists it. The other specifications keep their own text: `app-kit.md`
AK-2 (the command and its mark), `mcp.md` MC-6 and MC-7 (the AI tools), `SPEC.md` AX-8 (keys), AX-10 (the palette) and
AX-11.12 (AI content says so), `user-workflows.md` WF-1 (names), WF-2 (the inventory), WF-4 (`do`) and WF-9 (the AI
tools mode).

## 1. What a command is

- CL-1.1 **A command is one thing a window can do, with a name.** Its parts:
  - its **label**, the words its menu item shows, plain: without the letter's marker and the trailing dots
    (`AxKit.Core/MenuText.Plain`);
  - its **menus**, from the bar down, joined by a comma and a space ("Navigation, Next"); none for a key with no menu
    item;
  - its **canonical name** (CL-5.1): the label, or the label with its menus where two commands of the window share
    the label;
  - its **key**, from the window's key table or the bundle's (`SPEC.md` AX-8), and its **further keys** (axDown's
    Redo: Ctrl+Y, also Ctrl+Shift+Z);
  - its **mark** (CL-1.2);
  - its **state** at a moment: whether it is checked (a setting), whether its menu item is offered (enabled, in a
    menu as it stands), and whether its key acts now (`Command.When`: the list has the focus, the field has it).
- CL-1.2 **The mark says how far a command reaches**, in the user's words (`AxKit.Core/CommandMark.cs`, `CommandMark`;
  `mcp.md` MC-3.1):
  - **read**: it says or shows something and changes nothing (Say the status, Keyboard shortcuts);
  - **open**: it opens a window or a file, or moves within the window (Go to line, a recent file);
  - **change**: it changes the window's own state or the file the window holds (Word wrap, Save, a setting);
  - **act**: it reaches outside the window: it deletes, moves, sends, starts a program, writes the clipboard, uses the
    network, or its effect is in doubt. A menu item that states no mark counts as act
    (`CommandPalette.MarkOf`).
  Held by `tests/AxKit.Tests/CommandMarkTests` (every `new Command` states its mark; the pinned act commands stay act;
  no menu item with a click is made without a mark) and by `check commands` in every app's first window script
  (`AxKitForm.CommandProblems`).
- CL-1.3 **A command is defined in one place: its window.** Either a `Command` record added to the window's list
  (`AxKitForm.Add`, which makes the menu item and handles the key; `AxKitForm.AddKey` for a key with no menu item), or
  a menu item made where the menu is filled, with its mark (`AxKitForm.Item`, `Toggle`, `ChoiceItems`,
  `CommandPalette.Marked`). Nothing else defines a command: the palette, the F1 text, the AI tools and the checks
  read the menu bar through one function, `CommandPalette.Entries` (`app-kit.md` AK-2.5). Held by
  `tests/AxKit.Tests/CommandPaletteTests`.
- CL-1.4 **Two kinds of command stand outside the menu bar**, and so outside every reader but the key: a key with no
  menu item (`AddKey`: Tab between panes, a list's Enter; 198 rows of the inventory), and the keys a control handles
  itself (the arrows, typing), which are not commands at all.
- CL-1.5 **The inventory is the list of all of them**: `docs/axKit/window-commands.json`, written by the runner from
  live windows and never by hand (`user-workflows.md` WF-2.6 to WF-2.10): 1605 rows in 17 apps on 2026-11-16, each
  with its label, menus, key, further keys, key text, mark, whether it is a key alone and whether the AI tools may see
  it; without what is state. `window-commands.md` is the same to read, and `key-map.md` is the same by key. Held by
  every window run (`--inventory check`) and by `tests/AxAutomate.Tests/CommandInventoryTests`.

## 2. Where an action can come from

- CL-2.1 **The origins, as built.** Three are the user's own hands, two are the automated checks', one is an AI's:
  1. **The key**: the user presses the command's key in the window.
  2. **The menu**: the user chooses the menu item.
  3. **The command palette** (Ctrl+Shift+P, `SPEC.md` AX-10): the user types words, picks a line, presses Enter.
  4. **A window script's `do` step** (`user-workflows.md` WF-4): the runner names a command; the window says which
     key runs it now; the runner presses that key, or chooses the menu item of a command that has no key.
  5. **The automation channel's `choose`** (and its older `menu`): the runner names a menu item and the window
     clicks it. The channel exists only in a window the test runner started with its secret (`AXKIT_AUTOMATION`,
     security audit S-11), on a desktop of its own; never in the user's windows.
  6. **An AI tool**, `axkit_window_run` (`mcp.md` MC-7): an AI client the user paired names a command of a window.
- CL-2.2 **Not built: speech.** No origin takes spoken or typed sentences. Section 8 (e) says where it would join.
- CL-2.3 **AI tools that are not a window's command**, named here so that the table is not read as covering them;
  each keeps its own rules in `mcp.md`: `axkit_open` (opens an app on a path; never axClaude or axTerminal, never
  axTask on a path, MC-6.5, MC-D12), `axkit_task_add` (adds an item to a list of the open axTask window, behind Let
  AI tools run commands, said, never asked, MC-8.3) and `axkit_offer_command` (a shell command offered to the user
  in axClaude, asked every time, MC-9). The reading tools read and run nothing.
- CL-2.4 **Whoever starts a command, what runs is the window's own command**: the same code the menu item runs.
  No origin has a second implementation of a command. Section 4 says where that is held by the code's shape and where
  only by habit.

## 3. The table: origin against mark

How to read it. Each origin has four cells, one per mark. A cell says one of four things: **runs** (the command runs
and the window says what the command itself says); **runs and is said** (the window also says who ran it); **asks
first** (with the question's words and its default); **never** (with the reason). The numbered conditions under the
table hold before any cell. axKit asks nothing because of a mark when the user's own hands start the command: a
command's own question (Delete's "Delete a.txt?", a close with changes) is part of the command and is asked whatever
the origin.

- **The key** (the user)
  - read: runs.
  - open: runs.
  - change: runs.
  - act: runs.
  - Conditions: 1, 2, 3.
- **The menu** (the user)
  - read: runs.
  - open: runs.
  - change: runs.
  - act: runs.
  - Conditions: 1 (a notice disables the menu bar), 2.
- **The command palette** (the user)
  - read: runs, as a click on its menu item.
  - open: runs.
  - change: runs; a check item then says its new state, "Word wrap, checked", since no menu shows it.
  - act: runs.
  - Conditions: 1, 2 (the palette does not open), 4.
- **A script's `do` step** (the automated checks)
  - read, open, change, act: runs, by the very key the user would press now, so every condition of the key holds; a
    command with no key at all runs by `choose`, the next row.
  - Conditions: 1, 2 and 3 fail the step with nothing pressed ("the window shows the notice <title>: its commands
    wait"; "the key of <label> (<key>) does not act where the focus is"); 5; 6.
- **The channel's `choose`** (the automated checks)
  - read, open, change, act: runs, as a click on its menu item; a check item says its new state.
  - Conditions: 1 ("no menu item <label> in <menus> now"); 5. Not held by condition 2 in code (section 4, CL-4.8).
- **An AI tool, `axkit_window_run`**
  - read: runs and is said: "Claude ran Say the status", joined to the command's own words in one announcement.
  - open: runs and is said: "Claude ran Go to line".
  - change: runs and is said: "Claude ran Word wrap. Word wrap off".
  - act: **asks first**. The question is a list, with the offer's chime, named "Command offer. Claude asks to run this
    window's command Delete (Actions) on a.txt in files. Asked by Claude, an AI: read the explanation before you allow
    it". Its answers: **Explain this command**, where the list starts and which only explains; **Yes, run it**;
    **No, do not run it**, which is Escape too. Running is never where the list starts. On Yes it runs once and is
    said, "Claude ran Delete". Where the window has no place for the list the same question is a notice whose default
    is No.
  - never, whatever the mark: conditions 7, 8 and 9.
  - Conditions: 1, 2, 7 to 15.
- **Speech**: not built (CL-2.2).

The conditions:

1. **A notice is open** (a question, an error, a help text, the palette, a pairing). No command of the window runs
   from any origin: the notice has the keys, the menu bar is disabled, and the AI tool is refused with "The window
   shows the user a notice, <title>: commands run once the user has answered it, and AI tools never answer it." The
   same for a Windows dialog the window shows, for the AI tool. The few bundle keys that are information alone work
   in a notice too (larger and smaller text, Say the status, the mode key; find keys in a notice's text). Built:
   `AxKitForm.ProcessCmdKey`, `AxKitForm.McpChangeRefusal`.
2. **The window's key test runs** (`SPEC.md` AX-8.12). Every key is named and none is carried out, so the key, the
   palette and the menu by the keyboard run nothing; the listing gives no key that acts, so `do` waits; the AI tool is
   refused with "The window is in its key test: the user is trying keys, and nothing is carried out meanwhile.
   Commands run once the user has ended it." Built: `AxKitForm.KeyTest.cs`, `AxKitForm.CommandRows`,
   `McpChangeRefusal`.
3. **The key acts only where its command says** (`Command.When`). Among the commands that hold a key, the first in
   the window's order whose `When` holds runs; when none does, the key is the focused control's. The menu item always
   runs. Built: `AxKitForm.RunCommandKey`.
4. **The palette offers what the menus offer at that moment**: every enabled item, the recent lists filled; not the
   lists that are slow to fill (axFile's Drives, Open with). axFile adds the folder's entries to its palette, which
   are no commands (`SPEC.md` AX-10.5).
5. **Only in a test run.** The automation channel answers only a runner that started the window with its secret, on
   its own desktop.
6. **In the AI tools mode** (`--ai-tools`, `user-workflows.md` WF-9) a `do` step whose command the tools may run
   without asking goes through the AI tool's row instead, as the paired client the runner is; an act command stays
   on the keyboard, since nothing answers the question for the user.
7. **axPass and axClip answer no AI tool.** Their windows serve no pipe; should one be reached, it refuses: "axPass's
   commands are never run by AI tools: its windows hold passwords. The user runs them." (`McpRun.NeverRun`.)
8. **Never for AI tools: the switches that decide what an AI may do.** The whole axKit, AI tools submenu (Let AI
   tools read and open axKit, Let AI tools run commands, Add axKit to Claude Code, Forget AI tools, Read the log) and
   AI help's Use Claude in axKit and Folders Claude may read are not listed to an AI tool and cannot be named by one:
   "The window has no command <name>". A client never widens its own access. (`CommandPalette.NeverForAiTools`.)
9. **Never for AI tools: a key with no menu item.** Its meaning depends on the focus, which the client does not hold
   (`mcp.md` MC-D5). It is not listed.
10. **The two switches.** Let AI tools read and open axKit must be on, its sealed file whole, and the client paired by
    the user's own Allow; otherwise every tool is refused. Let AI tools run commands must be on as well; otherwise:
    "The user has not allowed AI tools to run commands in axKit. The user allows it in any axKit window: axKit menu,
    AI tools, Let AI tools run commands." Both are off at first, and turning the first off turns the second off.
11. **Never under the user's hands.** While the window is in front and the user pressed a key or moved the mouse
    within the last 3 seconds, the run is refused: "The user is working in this window right now ...: Try again in a
    moment." The user's own Yes to an act is not held to this. A window that is hidden by its own close answers
    nothing at all.
12. **The name must name one command the menus offer now** (section 5). Unknown, ambiguous or disabled: refused, in
    the tool's words.
13. **After a No, the window asks no more for a minute**, whichever client asks: "The user denied running a command a
    moment ago; this window asks no more within a minute."
14. **A Yes runs only what was asked.** At the Yes the window checks again: the selection is the one the question
    named (by its identity, not its few words), Let AI tools run commands is still on, no notice came, the menu item
    is still offered. Otherwise it says "<command> not run: <reason>" ("Delete not run: the selection changed").
15. **Heard and kept.** Every run by an AI tool is said in its window; a window that is not in front also chimes
    and flashes on the taskbar, once for a burst; every call is a line of the user's log. No AI tool presses a key or
    answers a question, since no such tool exists.

- CL-3.1 The table and its conditions are built as stated. Held by: `tests/AxKit.Tests/McpTests` (the pure rules of
  `McpRun`: the never-run apps, the switch's words, the busy rule, the question's words, the selection's identity, the
  minute after a No), `tests/AxKit.Tests/CommandPaletteTests` (an item with no mark is act; a withheld menu withholds
  everything under it from the AI tools and nothing from the palette), `tests/AxKit.Tests/CommandMarkTests`,
  `tests/AxAutomate.Tests/DoStepTests` and `AiToolsModeTests`, `tests/AxKit.Tests/KeyTestTests`, and the window
  scripts `tests/scripts/mcp/02-run-commands.txt`, `03-run-asks-first.txt` and `05-run-asks-in-a-list.txt`.
- CL-3.2 **The table is a test** (2026-11-16). One pure function answers every cell,
  `CommandGate.Decide(origin, mark, state)` (`src/AxKit.Core/CommandGate.cs`): the origin (`CommandOrigin`: Key, Menu,
  Palette, Script, Choose, AiTool), the mark, and a record of the facts the conditions ask about (`CommandState`: a
  notice, a Windows dialog, the key test, no test run, the app that serves no AI tool, the two switches, the user's
  hands, a key that does not act where the focus is, a bundle key that is information alone, a key with no menu item,
  an item never for AI tools, an item the menus do not offer now, a No within the last minute). It answers one of the
  four things a cell says (`CommandVerdict`: runs, runs and is said, asks first, never), and for never the reason as a
  code (`CommandRefusal`); the words of a refusal stay where they are written, and each caller gives its own for the
  code. `tests/AxKit.Tests/CommandGateTests` has one test per cell and per condition of each origin, named after it:
  the origin, then the mark or `condition` and its number (`Key_read_runs`,
  `AiTool_condition_11_never_under_the_user_s_hands`). Two of its tests read this section: a cell named here that no
  test is named after fails, and so does an origin written here that the tests do not know. How the lines above are
  read: an origin is a line that starts with its name in bold; its marks are the lines under it that start with
  `read`, `open`, `change`, `act` or several of them and a colon; its conditions are the numbers of its `Conditions:`
  line, "7 to 15" being every number between, texts in quotes and in brackets left out. Where the code asks the gate
  and where the gate only describes is CL-4.9.

## 4. The path a command takes

Each origin, by function and file, down to the code that runs the command.

- CL-4.1 **The key.** Windows' key message; `AxKitForm.ProcessCmdKey` (`src/AxKit.Forms/AxKitForm.cs`): the bundle
  keys it handles itself (text size, Say the status, the mode key; then, with no notice open, Keyboard shortcuts, the
  palette, the report, Ask about this window, Save); then the window's own keys (`ProcessWindowKey`); then
  `RunCommandKey`, which finds the first command with the key whose `When` holds; then `Run(Command)`, which writes
  the command into the window's recent actions; then `Command.Run`. While the key test runs,
  `KeyTestFilter.PreFilterMessage` (`AxKitForm.KeyTest.cs`) takes the key message before any of this.
- CL-4.2 **The menu.** The menu item's click. For an item made from a `Command` (`AxKitForm.Add`): the same
  `Run(Command)`, then `Command.Run`. For an item made where its menu is filled (`Item`, `Toggle`, `ChoiceItems`,
  `CommandPalette.Marked`): the item's own action, directly.
- CL-4.3 **The palette.** `AxKitForm.ShowCommandPalette` (`AxKitForm.Palette.cs`) reads `CommandPalette.Commands`
  (`CommandPalette.cs`; the lines of `CommandPalette.Entries`) and the window's own `PaletteEntries`; `Palette.Find`
  (`src/AxKit.Core/Palette.cs`) gives the matches; Enter is `RunPaletteEntry`, which closes the notice and posts the
  line's action: the menu item found again if it was made anew, then `PerformClick`. From there, CL-4.2.
- CL-4.4 **A script's `do`.** `Run.Do` (`src/AxAutomate.Core/Run.cs`) asks the window `commands data`
  (`AutomationChannel`, `AxKitForm.CommandRows` in `AxKitForm.Commands.cs`, `CommandListing.Rows` in
  `src/AxKit.Core/CommandListing.cs`); `DoStep.Decide` (`src/AxAutomate.Core/DoStep.cs`) resolves the name
  (`CommandNames.Resolve`) and decides. A key: the runner's own `keys` step, which posts the key to the window; from
  there, CL-4.1. No key: the channel's `choose`, CL-4.5. In the AI tools mode: `Run.ThroughTools`, CL-4.6.
- CL-4.5 **The channel's `choose`.** `AutomationChannel.Choose` (`AutomationChannel.cs`), `AxKitForm.Choose`
  (`AxKitForm.Commands.cs`), `CommandPalette.Find` by exactly the label and the menus, `PerformClick`. From there,
  CL-4.2. The older `menu` step: `AutomationChannel.Menu`, the first line of `CommandPalette.Commands` that starts
  with the text, its action, `PerformClick`.
- CL-4.6 **An AI tool.** The client starts `AxKit.exe mcp` (`McpBroker`), which reaches the window's pipe
  (`McpChannel`: both ends proved, the setting, the pairing); `McpChannel.Run` reads the switch from the sealed file
  and calls `AxKitForm.McpRunCommand` (`AxKitForm.Mcp.cs`) on the window's thread: the gate's conditions of the
  window (`CommandGate.Window` with the facts of `McpGateState`: the app, the switch, the key test, a notice, a
  Windows dialog, the user's hands, in that order; the words of each from `McpRun.Refusal`), then `ToolEntries`
  (`CommandPalette.Entries` without the withheld items) and `McpRun.Find` (`src/AxKit.Core/Mcp/McpRun.cs`, which calls
  `CommandNames.Resolve`), then the cell for the entry's mark (`CommandGate.Decide`, with the minute after a No). An
  act: `AskToRunMcp`, the question (`ShowOfferedRequest`, or `ShowNotice`), and on Yes `ToolItem` and `RunForMcp`. Any
  other mark: posted, checked again (`WhyNotRunForMcp`, the busy rule), `ToolItem` (`CommandPalette.Find`),
  `RunForMcp`, which is the window's one click for this origin (`ClickFor`): the words that say who ran it
  (`McpRun.Said`), the chime for a window behind, `PerformClick`. From there, CL-4.2.
- CL-4.7 **Where the paths join.**
  - Every origin but the key ends in a click on the menu item (`PerformClick`), found through
    `CommandPalette.Entries`. So what a menu item does is written once, and no origin can do something a menu item
    does not.
  - **Every click axKit itself makes is one function** (2026-11-16), `AxKitForm.ClickFor(origin, item, name, client)`
    (`AxKitForm.Commands.cs`): the palette's line and the channel's `menu` (through `AxKitForm.MenuLines`, which hands
    `CommandPalette.Commands` the click), the channel's `choose` (`AxKitForm.Choose`) and the AI tool's run
    (`RunForMcp`). Each caller keeps its own checks before it. The function does for an origin what that origin did
    before, no more: for the AI tool the words that say who ran it and the chime, ahead of the click; for the palette
    and the channel a check item's new state after it. The user's own choice in a menu is WinForms' click and does not
    pass there.
  - **The words of a check item's new state are written once**, `MenuText.CheckSaid` (`src/AxKit.Core/MenuText.cs`):
    "Word wrap, checked", "Word wrap, not checked". The palette says them through the window's `Announce`, the channel
    straight through `Announcer.Raise`, as each did before (`AxKitForm.SayFor`).
  - The key and the menu item of a `Command` join one step later, in `AxKitForm.Run(Command)`.
  - The names of `do` and of the AI tool are resolved by one function, `CommandNames.Resolve`.
  - **One function knows the origin and the mark together**, `CommandGate.Decide` (CL-3.2, CL-4.9).
- CL-4.8 **Where they do not join yet.**
  1. **Only the AI tool's path asks the gate.** The other conditions of section 3 are enforced where they were: the
     key's in `ProcessCmdKey` and the key test's filter, the menu's by the disabled bar, the palette's in
     `ShowCommandPalette`, `do`'s by the listing, none in `Choose`. So `choose` is still not held by the key test (the
     `do` step never sends it there, by the listing; the channel itself would run it), which the gate says as it is
     (`Choose_condition_2_the_key_test_does_not_hold_it`). A new origin starts at the gate and its tests, and still
     has to call it.
  2. **Two kinds of click**: WinForms' own, for the user's choice in a menu, and `AxKitForm.ClickFor`, for every
     click axKit makes. The menu and the AI tool do not say a check item's new state; the palette and the channel do.
     The AI tool's second checks, at the moment a posted run or the user's Yes runs (`WhyNotRunForMcp`, the busy rule,
     `ToolItem`, `McpRun.TargetRefusal`), are their own function with their own words and their own order (a notice
     or a dialog before the key test), not the gate's.
  3. **A menu item not made from a `Command` has no run function of axKit's.** Its click is the end. So the window's
     recent actions (for a report) hold "Find, Ctrl+F" for a command of the list, by key, menu, palette or AI tool,
     and nothing for a setting or a recent file chosen.
  4. **The bundle keys handled in `ProcessCmdKey` itself** (F1, the palette, F4, Shift+F1, Ctrl+S, the text size
     keys, F8) do not pass `RunCommandKey`; most also have a menu item, whose click is a second way into the same
     method.
  5. **A key with no menu item** runs by its key alone (and by `do`, which presses it): no other origin reaches it.
  6. **axFile's palette entries** (the folder's files) are in the palette alone: no menu, no name, no mark.
- CL-4.9 **Where the gate decides and where it describes** (2026-11-16).
  - **It decides for the AI tool.** `AxKitForm.McpRunCommand` asks `CommandGate.Window` before it reads the menus
    (reading them fills the lists that fill as they open, so nothing is read for a call that is refused anyway) and
    `CommandGate.Decide` once the name gave an entry and its mark. A typed data tool that changes something asks the
    same conditions of the window through `McpChangeRefusal`, after its own checks of the app and the switch. The
    order in which the gate asks is the order the code asked before, and decides which words a client gets when two
    conditions hold at once: the app, Let AI tools run commands, the key test, a notice, a Windows dialog, the user's
    hands, then the name, then the minute after a No. `CommandGateTests` holds every pair
    (`Of_two_conditions_that_hold_at_once_the_AI_tool_gets_the_earlier_one_s_refusal`), and each reason's sentence,
    by its constant or word for word.
  - **It describes the user's key, menu and palette, and the checks' `do` and `choose`.** For those the table says
    "runs" in every cell, and their conditions (a notice, the key test, `Command.When`, what the menus offer) are
    enforced by WinForms' message handling, the key test's filter, the disabled menu bar and the window's listing.
    Moving that enforcement behind the gate is a larger change with a real risk to what the user's keys do, and was
    left out on purpose. The tests hold the description: a change of the table changes a test.
  - **What the gate cannot see.** A Windows dialog is asked for the AI tool alone: the user's own keys go to the
    dialog. The key test holds the menu and the palette by the keyboard, since its filter takes key messages; a
    mouse click on the menu bar is not a key message.

## 5. Finding a command by name

- CL-5.1 **The canonical name** is the label, plain; where two commands of one window share a label, the label, a
  comma, a space and the menus: "Heading, Navigation, Next" (`CommandNames.CanonicalOf`, `CommandNames.Line`;
  `user-workflows.md` WF-1.1). Held by `tests/AxKit.Tests/CommandNamesTests`.
- CL-5.2 **The exact match**, `AxKit.Core/CommandNames.Resolve`: whole text against whole text, without regard to
  case, the letter's marker and trailing dots gone; a label wins over a line; one command left is found, none is
  unknown, several are ambiguous. It never searches. For an unknown name it gives up to five commands whose label
  holds the name or is held by it, for the refusal to name. Used by `do` (`DoStep.Decide`), by the AI tool
  (`McpRun.Find`), by the inventory's checks and the converter.
- CL-5.3 **The palette's matching**, `AxKit.Core/Palette.Find` over `AxKit.Core/TextMatch`: the typed words, case
  and accents ignored, each to be held somewhere in the line (name, menus, key); lines that start with the first word
  first, then lines with a word that starts with it, then the rest. It is a search for a person, who hears the match
  and its count and presses Enter on the one chosen (`SPEC.md` AX-10.3). Held by `PaletteTests` and `TextMatchTests`.
  It does not forgive a misspelled word: no matching by likeness of spelling exists anywhere in axKit.
- CL-5.4 **What each origin accepts.**
  - The key and the menu: no name.
  - The palette: words, CL-5.3; the person picks.
  - `do`: the canonical name, CL-5.2; an unknown or ambiguous name fails the step and names the candidates.
  - The AI tool: the label, with `menu` where two share it, or the whole line; CL-5.2, with the refusals in the
    tool's own sentences, which do not name the nearest commands.
  - `choose`: exactly the label and exactly the menus, letter for letter (`CommandPalette.Find`); it is given both by
    the listing, never by a person.
  - The older `menu` step: the first palette line that starts with the text, without regard to case
    (`user-workflows.md` WF-1.8: kept until its lines are converted).
- CL-5.5 **What shares code today**: `do` and the AI tool share `CommandNames.Resolve`. The palette (`TextMatch`),
  `choose` (an exact comparison of its own) and `menu` (a prefix of its own) each have their own.
- CL-5.6 **Code matches a name; it never guesses an intent and runs it** (the rule the maintainer accepted,
  2026-11-16). A matcher may be loose in what it offers and never in what it runs:
  1. A command runs from a name only when the name is exactly one command's (CL-5.2), or when a person picked it from
     the matches offered and heard which it was (CL-5.3).
  2. A text that is no command's name is answered with candidates, to the person or to the caller. Nothing runs.
  3. Working out what a person means from a loose sentence is an AI's part, never axKit's code. The AI then asks for
     a named command through the AI tool's path, under section 3: the same switches, the same question for an act,
     the same words that say who ran it.
  Rules 1 and 2 are built (CL-5.2, CL-5.3). Rule 3 is how the AI tools are used today; no code of axKit's reads a
  sentence. A later name matcher or speech origin (section 8) is held to all three.

## 6. The boundary

- CL-6.1 **axKit's promise is about axKit.** What this document promises is what axKit's own windows and tools do,
  say and ask. It is not a promise about what an AI does on the computer: Claude Code has file and shell tools of its
  own, under Claude Code's own permissions, and can do with them most of what a window's command does, without the
  window. The maintainer, 2026-11-16: "we cannot stop the user from doing things, or Claude might use something other
  than the MCP that we give it to accomplish something in the command line"; "we should not even try to constrain
  actions since they will just happen a different way, so we better just give good paths".
- CL-6.2 **A limit in an axKit tool is justified only when it protects something inside axKit**:
  1. a secret axKit holds (axPass's fields, axClip's history; condition 7);
  2. a question meant for the user (no tool answers a notice or presses a key; condition 1);
  3. a setting that decides what is run or read (the AI tools' own switches, AI help's folders; condition 8);
  4. the user's hands on the keyboard (conditions 2 and 11);
  5. the user's knowledge of what happens in the user's own windows (said, chimed, logged; condition 15; the
     question before an act, which names what it would act on).
- CL-6.3 **A limit that only pushes an AI to another route is to be replaced by a good path.** When the other route
  is a shell command or a written file, the user gets no announcement, no question, no undo and no line in axKit's
  log: the limit made things worse. The good path is an axKit tool that does the thing in the open, with the mark's
  rule and a confirmation where the table asks one.
- CL-6.4 **Good paths asked for and not built**, each its own piece of work later:
  1. Add an item, or make a list, in a folder the user names (today `axkit_task_add` reaches only the lists of the
     open axTask window, MC-D12).
  2. Open axTask on a folder (today refused by `axkit_open`, MC-D12, since it would make the folder the top folder).
  3. Commands that take an argument: move to a folder, rename to a name, go to a line (today a command that needs one
     asks the user in its own notice, MC-7.7).
  4. A report drafted by an AI for the user to review and send (today only the user writes a report, F4).

## 7. Key families

- CL-7.1 **What is held today**: every key is the bundle's or one window's, written in one table and nowhere else
  (`SPEC.md` AX-8.1 to AX-8.3, AX-8.5), and no key of a window equals a bundle key but the overrides the user decided,
  listed by name; the keys two windows use for different actions are a known list, so that a new one fails. Held by
  `tests/AxKit.Tests/KeyTests` (AX-8.4).
- CL-7.2 **What is a proposal**: `docs/axKit/key-map.md` (generated: every key and what it does in each app, the keys
  with one meaning first, then the keys whose meaning differs) and `docs/axKit/key-families.json` (by hand: what each
  kind of key is for, and the keys that do not fit). The families are `"status": "proposed"` until the maintainer
  settles them: no test holds a key to its family and no key moved.

## 8. Not built, in the order to build it

- (a) **One function every origin calls to run a command**, with the table of section 3 as its test. **Built as far
  as it changes nothing a user meets** (2026-11-16): the pure decision, `CommandGate.Decide`, with the table as its
  test (CL-3.2); the one click axKit makes, `AxKitForm.ClickFor`, with the one place for a check item's words
  (CL-4.7); and the AI tool's path asking the gate in place of its own chain (CL-4.9). No word, no order of
  announcements, no question and no refusal changed: the AI tools' window scripts say the same lines before and
  after. **Left, each for its stated reason:**
  1. The user's key and menu do not pass the gate or `ClickFor` (CL-4.9): the enforcement would move out of WinForms'
     message handling and the key test's filter, which is where a mistake costs the user his keys. To be done alone,
     with every app's window checks and an NVDA pass.
  2. The bundle keys handled in `ProcessCmdKey` itself still do not pass `RunCommandKey` (CL-4.8, 4), for the same
     reason.
  3. The recent actions still hold commands of the list alone (CL-4.8, 3): writing every click there changes what a
     report holds (`reports.md` RP-4.1), which is a change a user meets, so it is a decision and not a consolidation.
  4. `choose` is still not held by the key test (CL-4.8, 1): holding it changes which origin is refused when. Only a
     test run reaches it.
  5. The AI tool's second checks (CL-4.8, 2) keep their own function: their words and their order differ from the
     first check's, so joining them changes which sentence is said when a notice and the key test hold at once.
- (b) **One name matcher** shared by the palette, `do`, the AI tool and later speech: `CommandNames` for the exact
  name, `TextMatch` for the offer, and one rule for the nearest candidates, so that the AI tool's refusal names them
  as `do`'s does; `choose` and the `menu` step move onto it (WF-1.8). Touches `AxKit.Core` (`CommandNames`, `Palette`,
  `McpRun.Find`), `CommandPalette.Find`, `AutomationChannel.Menu`. Small, but the tool's refusal sentences are pinned
  by scripts and change with them.
- (c) **Checks over the inventory**, in `test.ps1`, without a window: one name means one thing across apps or is a
  listed exception (Undo is change in the editors and act in axFile); one chord, one family, once the families are
  settled (CL-7.2); names that change with the state (axFile's Undo, which the inventory holds as Undo, Undo rename,
  Undo move, Undo delete, Undo compress, Undo new folder) settled as one command with one name. Touches
  `tools/key-map.ps1` or a test of `tests/AxAutomate.Tests`, `key-families.json`, and axFile for the names. Small for
  the checks; the decisions are the maintainer's.
- (d) **The good paths of CL-6.4**, each its own piece: its tool, its mark, its question, its section of `mcp.md`,
  its window script. Each small to middle; the argument-taking commands are the largest, since a command gains a
  typed argument in `Command` and in every reader.
- (e) **Speech**: an origin that hears a name and offers the matches as the palette does (CL-5.6 rules 1 and 2), and
  for a loose sentence hands the words to an AI, which asks through the AI tool's path (rule 3). It waits on (a) and
  (b). Large, and not started; what recognises the speech is not chosen.

## 9. Decisions

- CL-D1 (the maintainer, 2026-11-16) **One language for the commands**: "should we combine all the commands into a
  language spec that is used by the mcp and menus and keyboard shortcuts and becomes the keywords that the user needs
  to speak with some fuzzy matching and intent inference? I do not want us to have a proliferation of functionality
  and everything should be hardened into a simple language spec with permissions about where the action comes from
  and whether extra permission is needed to protect the user and ensure informed consent to the various actions. We
  need to consolidate and clarify things as much as possible. We need a simple management system that means you are
  only making small adjustments to singular places in code and that the path to execute the code is well defined."
- CL-D2 (the assistant's proposal, accepted by the maintainer, 2026-11-16) **Matching a name is code; working out an
  intent is the AI's.** Most of the ground is built; what is missing is this written specification and, later, one
  path in code. Code never guesses an intent and runs it; an AI that worked one out asks for a named command through
  the same path and the same consent rules (CL-5.6).
- CL-D3 (the maintainer, 2026-11-16) **The boundary**: "we should hold clear boundaries on what axKit will do and how
  it will request user confirmation, but we cannot stop the user from doing things, or Claude might use something
  other than the MCP that we give it to accomplish something in the command line"; "we should not even try to
  constrain actions since they will just happen a different way, so we better just give good paths" (section 6).
- CL-D4 (the assistant, 2026-11-16, for the maintainer to confirm) **This document changes no behaviour and moves no
  text.** `app-kit.md` AK-2 and `mcp.md` MC-7 keep their requirements and point here for the rules read together.
- CL-D5 (the assistant, 2026-11-16, for the maintainer to confirm) **The order of section 8**: the one run function
  first, since the table can only be a test once one function holds it; speech last.

## 10. Differences found between the specifications and the code

Written as the code behaves; no code was changed for this document. All seven were settled on 2026-11-16 by
correcting the documents to the code, which decided; each line says where. Nothing is open here.

1. **`mcp.md` MC-5.1 said a change or act run is announced and "Reads are silent".** In code every command an AI tool
   runs is said, a read or an open as well ("Claude ran Say the status"): `AxKitForm.RunForMcp` asks no mark. Settled
   2026-11-16: MC-5.1 now says what MC-7.5 and the code do, and names the first design's text as not built.
2. **`app-kit.md` AK-2.6, exception 3, named three items withheld from the AI tools.** The code withholds the whole
   AI tools submenu, so also Add axKit to Claude Code and Read the log, and AI help's Use Claude in axKit and Folders
   Claude may read (`AxKitForm.CreateMcpMenu`, `AxKitForm.Ai.cs`). Settled 2026-11-16: exception 3 names the submenu
   with its five items and AI help's two.
3. **`app-kit.md` AK-2.5 said "nothing is added to one reader alone".** The palette holds axFile's folder entries,
   which no menu, no AI tool and no listing has (`FileForm.PaletteEntries`), as `SPEC.md` AX-10.5 specifies. Settled
   2026-11-16: AK-2.6 lists the palette's own entries among the exceptions that are not for security, and AK-2.5
   points there.
4. **`app-kit.md` AK-2.5 named `CommandPalette.Commands` as the one function every reader goes through.** In code it
   is `CommandPalette.Entries`; `Commands` is the palette's lines made from it, which the channel's `menu` and
   `commands` also read. Settled 2026-11-16: AK-2.5 names `Entries` and says what `Commands` is.
5. **The recent actions hold commands of the list alone.** The comment on `AxKitForm.Recent` and `reports.md` RP-4.1
   spoke of every command run by key, menu or palette; a setting, a recent file or any other item not made from a
   `Command` record is not written there when chosen (CL-4.8, 3). Settled 2026-11-16: the comment and RP-4.1 say what
   is recorded.
6. **The key test holds every origin but the channel's `choose` and `menu`.** `user-workflows.md` WF-2.5 has the `do`
   step wait there, which it does by the listing; `AxKitForm.Choose` itself asks nothing about the key test. Only a
   test run can send it. Settled 2026-11-16 by stating it as it is: `SPEC.md` AX-8.12 and WF-2.5 say that the
   channel's two steps are not held and why no user or AI tool can reach them. That one run function should hold
   the condition for every origin stays proposed in CL-4.8 and section 8.
7. **`mcp.md` sections 3 to 6 (MC-2 to MC-5) describe the first design**: scopes per client and app, one tool per
   command, "Allow for this session", folders granted to a client, reads counted in the status bar. Settled
   2026-11-16: each of the four sections says at its head that it is the first design, not built as written, and
   points to MC-6 and MC-7 for what the code does.
