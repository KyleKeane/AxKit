# The workflow graph

A proposal of 2026-11-26, its decisions settled by the maintainer the same day (section 9); its step 2, the places
and the graph written, is built (2026-11-26, section 6; what building it decided is section 10), and so is its step 3,
the bounds checked (2026-11-26, WG-3.3, WG-3.4; section 11). It answers his ask of
2026-11-26: waypoints, any app's right place in two actions from anywhere, any goal in two actions from its
waypoint, consent gathered into one form, the user's actions as the edges of a graph, the workflows as walks through
it, and a system that holds every change of the code against the graph and the NVDA passes, so that the user's
mental map stays as it was and can be taught. The bundle's rule it serves is SPEC.md G7 (three steps, four at most).
Section 9 holds the decisions that are his; until they are made, the rest is the assistant's recommendation.

## 1. Summary

Every place the user can be is a node; every action the user can take there is an edge to the place it lands on. A
few nodes are waypoints, the places a user aims for. Two bounds follow from G7, and a computer can check both:
every waypoint is at most two actions from any node, and every goal at most two actions from its waypoint. The graph
is written by axKit itself into one file, from the windows as they are, and every test run compares it with the
committed one: a change to a key, a landing or the words of an edge stops the push until it is a change the
maintainer approved. The scripts become walks through the graph, each step checked for the node it lands on; the
NVDA passes record when each edge was last heard. The graph then teaches: the palette and AI help can say the
shortest way from here to a goal.

Much of this exists already: the command inventory (`window-commands.json`, every command with its key, menu and
mark), the window channel (`commands data`, the focused control, the state), the traces (every input with what
followed it, WF-5) and the workflow record (when each goal last passed, by tier, WF-6). The graph joins them.

## 2. Words

- **Place**: where the focus is inside one window, named by the window: axClaude's message field, its conversation,
  its question list; axFile's file list; axDown's text. A place has a stable id, `axClaude.message`, that no title,
  file name or text changes.
- **Over it**: what stands over a place and takes the keys: nothing, a menu, a notice, the palette, a form.
- **Mode**: a state of the window that changes what the keys of a place do: axClaude's screen and stream, axTerminal's
  screen view, a list's selection, Claude busy or ready. Only a mode that changes an edge is a mode here.
- **Node**: a place, what is over it, and the window's modes: `axClaude.message | none | ready`. The desktop with no
  axKit window in front is one node of its own, `outside`.
- **Edge**: one action from a node, and the node it lands on, with what the user hears there (the words, the kind of
  event of EventKind) and the command it runs (the inventory's row), when it runs one.
- **Action**: G7's: one key, one chord, one choice in a list, or one typed name with its Enter.
- **Waypoint**: a node marked as one a user aims for: each app's main place with nothing over it, and the few more an
  app names (axClaude's conversation, axFile's folder list). Every window has at least one.
- **Goal**: a section of a script under a `Goal:` line (WF-3.1), now also the node it ends on and the action that
  finishes it.
- **Walk**: the nodes and edges a script passes through, step by step.

## 3. Goals and non-goals

Goals:
- WG-G1 Every place the user can reach is in one file, with every action from it: nothing the user can do is
  missing from the map, and nothing on the map is gone from the windows.
- WG-G2 The two bounds are checked by a computer at every test run, and every goal over them is listed with its
  shortest path.
- WG-G3 A change to the map is never an accident: it stops the push until approved, and the approval says why.
- WG-G4 Every edge carries when a script last walked it and when NVDA last spoke it.
- WG-G5 The map teaches: from any node, the shortest way to any goal can be said in words.

Non-goals:
- A model of everything a window shows. The graph holds where the focus is and what the keys do, not the text.
- Edges of the user's own content: moving through the lines of a file or the rows of a list is one place, not a node
  per line. Home, End and the arrows stay inside their place.
- A second source of keys. The keys stay in their tables (rule 5); the graph is written from the windows and never
  read to make a key.
- A crawl of commands that act outside the window (the inventory's mark `act`): those edges come only from scripts.

## 4. Requirements

### WG-1 Places

- WG-1.1 **Each window names its places** in one table, `<App>Places` beside its key table (`AxClaudePlaces`), as
  `AxClaudeKeys` names its keys: the id, the words a user would use ("the message field") and the control that is
  the place. A control that takes the focus and has no place fails a test, as a key without a table does
  (`KeyTests`); the shared window types (`ConversationForm`, `ItemForm`, the text editor shell) name their own places
  once, so that an app on one of them gets them without a table.
- WG-1.2 **The channel says the place.** `state` and every focus move of a trace carry the place's id beside the
  focused control's name and role, so that a script, a trace and the AI tools all know where the focus is by the
  same name.
  Built 2026-11-26: `AxKit.Forms/Place` (an id, the words, whether a waypoint) and one table per kind of window,
  `ConversationPlaces` (`text`, `field`), `EditorPlaces` (`text`, `details`), `ItemPlaces` (`list`, and the second
  pane by its kind: `fields`, `details`, `log`) and `HotkeyPlaces` (`field`); the kind's class gives each place its
  control (`AxKitForm.Places`), and a place's id is the app's name and the table's id, `axCalc.field`. One app has a
  table of its own, `AxHelpPlaces` (`text`, the text axHelp's window shows in place of the field). `PlacesTests`, beside
  `KeyTests`: every kind of `components.json` has its table and gives it, every app is on a kind with places, an app
  whose folder holds a control of its own that takes the focus has its table (from the sources), and the graph file
  lists no control on view that takes the focus and has no place (from the windows, through the crawl, WG-2.2).
- WG-1.3 **What is over it** is the window's own: a menu, the notice (`OverlayPanel`), the palette, the control area's
  question (`ControlArea`), each with its id. A notice names its own kind (an error, a question, a form).
  Built 2026-11-26 (`AxKitForm.OverId`): `none`, `menu`, `dialog` (a Windows dialog, while `DialogShowing`),
  `palette`, `form`, `error`, `question` (a notice with a field, a list or more than one button), `notice`, and
  `choice` (the focus inside a `ControlArea`, which stands in a place: axClaude's question, Go to line). While
  something is over the places, the place is the one the focus was last in. The channel's `state` carries `place` and
  `over`, and every focus move of a trace ends in `  @ <place>|<over>` (`user-workflows.md` section 5.4).

### WG-2 The graph file

- WG-2.1 **`docs/axKit/workflow-graph.json`**, written by the inventory run (`--inventory write`), never by hand:
  the nodes by app, and from each node its edges, each with the chord or the choice, the command's label when there
  is one, the landing node, the kind of event and the words heard there, with `{temp}` and `{home}` for paths as the
  traces write them (WF-5.3).
- WG-2.2 **Where the edges come from**, in this order: the inventory's commands, each tried at every node of its
  window where its `When` lets it act, on the hidden desktop, its landing read from the channel; the bundle keys,
  tried the same way at one node of each window type, since they are the same everywhere (AX-8); and the scripts'
  walks for whatever a crawl does not try (WG-3). A command marked `act` is never tried by the crawl: it can send,
  delete or run.
  Built 2026-11-26 (`AxAutomate.Core/GraphCrawl`, `tools/AxAutomate/GraphRun.cs`; `automation.md`, "The command
  inventory"): under `--inventory write` and `rewrite`, after the first script of an app that passed and has a
  `check commands` after its `start`, the window is started afresh as that script starts it and crawled. Every place
  on view is reached (the channel's `goto`); at each, every chord a row of `commands data` would run now and every
  bundle key no row lists (the window names them in its `places`: the bundle table's keys but the moves inside a
  place, and Escape, Tab, Shift+Tab) is pressed once, in the order of the chords, and its landing read. Escape is
  pressed once on whatever an action landed on over a place: that node's edge. Never pressed: a chord an `act` row
  holds, an app's own key (they are `outside`'s), New window, and the Context menu's keys. Between two keys the window
  is brought back: Escape, `goto`, and a fresh start when those do not do it, when the window hid or ended (an edge
  to `outside`), or when its status bar differs from the place's first (a mode left on by the key before).
- WG-2.3 **`outside` has its edges too**: the hotkeys of axStart and every app's launch key (Ctrl+F-key,
  Ctrl+Shift+F-key, AX-8.18), each landing on the app's first waypoint.
- WG-2.4 **One order**, as the inventory has: the apps by name, a window's nodes by id, a node's edges by chord, so
  that what is written is what is compared.
- WG-2.5 **A page for people**, `docs/axKit/map.md`, written by `tools/map.ps1` from the file (this page stays the
  specification): by app, each waypoint, how it is reached from anywhere, and what can be done from it.

### WG-3 The bounds

- WG-3.1 **Reaching a waypoint**: from every node of a window, every waypoint of that window in two actions or fewer
  (Alt+number, AX-8.18; Escape from anything over a place is one of the two). The key that opens an app is free (the
  maintainer, 2026-11-26): from another app or from `outside`, the app's key brings its window up on its first
  waypoint and is not counted, and the two actions start there.
- WG-3.2 **Reaching a goal**: from its waypoint, every goal in two actions or fewer, with typing a name and Enter as
  one (G7). So no goal is more than four actions from anywhere, and the usual goal, started from its waypoint, is
  two.
- WG-3.3 **What a goal's count holds**: only the actions the user takes for the goal. The steps a script takes to make
  an example (typing a file's text, making a folder) and those that only look are not counted: a goal line marks its
  first counted step and its last, or the counted steps are the ones after the goal reaches its waypoint.
  Built 2026-11-26 (`AxAutomate.Core/GoalCounts`), with a rule in place of marks, from the script's own steps:
  - **What is counted**: every `# Goal:` section of every script of `tests/scripts` (a script without a goal line is
    one goal, WF-3.3); never a `# Check:` section, the setup or `tests/nvda-scripts`.
  - **One action each**: a press of a `keys` step (`keys ctrl+s f2` is two), a `type`, a `close` (Alt+F4), a
    `template`; a `do` or a `menu` when its command has a key in the command inventory (a chord, or a key text such
    as "K in the conversation"), and two when it has none: the palette, F1 and then the name typed with its Enter
    (section 10, point 8). A `do` whose name the inventory lacks is one, the key the step presses.
  - **Free**: `start` and `launch` (the app's own key, WG-3.1), `adopt` and `use` (a window coming up, or the app's
    key back to it), `mcp call` (Claude's action, not the user's).
  - **Not counted**: the steps that prepare or look (`file`, `mkdir`, `run`, `wait`, `silence`, `expect`, `refuse`,
    `try`, `check`, `allow`, the reads), and a step after `iffound`, which answers what may come and is no part of
    the way.
  - **Joined into one**: a typed text and the Enter right after it (a typed name with its Enter, G7); a run of moves
    inside a place, one after another (the arrows, Home, End, Page Up and Page Down, with Ctrl or Shift or neither,
    not Alt), with the Enter right after it: the choice in a list, or the place in a text.
  - **The example**: a goal whose first action is a `type`, and in which a command comes after the typing (a `do`, a
    `menu`, or a chord with Ctrl, Alt or a function key), types the example's text first: those first steps, the
    typing and the typing keys between (Enter, Tab, Space, Backspace, Delete, a letter or a digit), are not counted,
    and the page names them after the goal's steps ("Not counted, the example: ..."). Typing with no command after it
    is the goal itself (axCalc's `type 2+2`, `keys enter` is one action). The rule does not see an example typed in the
    middle of a goal (axClaude's five lines typed to move through by page): those are counted, and the page shows
    them in the goal's steps.
- WG-3.4 **The check** (`tools/graph-check.ps1`, in `test.ps1`, without a window): the shortest paths over the
  committed file, the bounds checked for every node and every goal, and every goal over them named with its count
  and its path. Until the shortenings are made (section 6, step 6), the list is a page, `docs/axKit/over-the-bounds.md`,
  and the check fails only on a goal that was within them and is no longer: the map may get shorter, never longer.
  Built 2026-11-26: `AxAutomate --bounds` (`tools/AxAutomate/BoundsRun.cs`, the counts of WG-3.3 and
  `AxAutomate.Core/GraphBounds`, a breadth-first search over the window's own nodes, an edge one action; an edge that
  leaves the window is not followed) writes the page (by app: the goals over four with their count and their steps,
  the most first, those at four, the nodes over the waypoint bound; the totals at the top) and the baseline
  `docs/axKit/goal-counts.json` (each goal's workflow, title, script and count). `tools/graph-check.ps1` runs it and
  `tools/pages.ps1` runs the script with the other pages, `-Check` with theirs. Written without a switch, the baseline
  keeps a count that grew at the count it had, and leaves out a new goal over four; `-Check` fails while the page or
  the baseline is not what a write gives, and names each goal longer than the baseline holds: one within four that is
  over now, one whose count grew, a new one over four. `-Write` takes them in, at the maintainer's word, and the
  commit shows the change. A goal is known by its workflow's label and its title, as the workflow record knows it, so
  a renamed goal over four is a new one.
- WG-3.5 **A consent form is one node and one action** (the maintainer's ask): several questions that come together
  (axClaude's permissions at a start, AX-11.5's reminders) are one form with a check box per question, each box
  starting at the answer most users give; Enter takes them all. The form is one action whatever is changed on it
  (the maintainer, 2026-11-26). A goal that meets two consent questions one after the other is over the bounds by
  that, and the fix is the form.

### WG-4 The scripts as walks

- WG-4.1 **Each input step's landing is read** from the channel after it settles (the place, what is over it, the
  modes) and written into the trace beside the input: the walk.
- WG-4.2 **A step whose landing is no edge of the graph fails**, with the node it came from, the action and where it
  landed: either the window changed (the map changed, WG-5) or the graph lacks the edge (the crawl missed it, added
  at the next write).
- WG-4.3 **The record by edge**: beside the record of goals (WF-6), `workflow-record.json` gets for each edge the last
  run that walked it, by tier, the desktop and NVDA. An edge no script walks is on `owed.md` as unwalked; one NVDA
  never spoke there is owed a hearing. So the NVDA passes say, edge by edge, what the user has heard and what not.
- WG-4.4 **The scripts keep their language.** A script's step stays `keys` or `do`; the walk is what the runner reads,
  not something written in the script. A new step `at <place>` (an expectation: the focus is on that place) is
  added, so that a script can say where it is by the place's name rather than the control's words.

### WG-5 The map held still

- WG-5.1 **The gate**: every test run writes the graph from the windows' tables and inventory as they are and compares
  it with the committed file. A node added or gone, an edge whose chord, landing, words or kind of event changed,
  fails the run with each difference said in words ("axDown, the text: Ctrl+R used to land on Find in the text and
  now lands on Replace").
- WG-5.2 **The approval**: the difference is committed with the new file, and the commit message has a line `Map
  change: <why>` (the commit-msg hook refuses a commit that changes the file without one, as it refuses one without
  an Item line). The line is written from the maintainer's own decision, named in the item.
- WG-5.3 **What a change owes**: every changed edge is owed a hearing until an NVDA pass walks it (WG-4.3), and the
  guides and F1 texts that name it change in the same commit, as a key change does today.
- WG-5.4 **Words that only move**: a changed word of an announcement is a change like any other. The traces' rule that
  an announcement and a focus move may change places with timing (WF-5.7) holds here too: the edge holds the words,
  not their order against a focus move.

### WG-6 Teaching

- WG-6.1 **The palette says the way**: F1, then typing a goal's words, finds the goal and says its shortest path from
  where the focus is ("Ctrl+F2, then Ctrl+N"); Enter walks the whole path and Shift+Enter takes its first step (the
  maintainer, 2026-11-26).
- WG-6.2 **The how-to pages** (`workflows/`) give each goal its shortest path from its waypoint first, and the
  script's example after it.
- WG-6.3 **AI help** answers "how do I" from the file rather than from the guides' prose, with its caution (rule 11).

## 5. The file's format

```json
{
  "format": "axkit-workflow-graph 1",
  "about": "The workflow graph (docs/axKit/workflow-graph.md): ... never by hand; ...",
  "apps": [
    {"app": "axCalc", "type": "conversation", "start": "axCalc.field|none"}
  ],
  "unplaced": [],
  "nodes": [
    {"id": "outside", "app": "", "place": "", "over": "none", "modes": [], "waypoint": false, "words": "the desktop, no axKit window in front",
     "edges": [
       {"action": "ctrl+f10", "command": "Open axCalc", "to": "axCalc.field|none", "kind": "", "said": "", "free": true}
     ]},
    {"id": "axCalc.field|none", "app": "axCalc", "place": "axCalc.field", "over": "none", "modes": [], "waypoint": true, "words": "the field",
     "edges": [
       {"action": "ctrl+delete", "command": "Clear the field", "to": "axCalc.field|none", "kind": "said", "said": "Empty"},
       {"action": "f1", "command": "Command palette", "to": "axCalc.field|palette", "kind": "field-entered", "said": ""}
     ]}
  ],
  "goals": [
    {"workflow": "Calculate and reuse earlier answers", "goal": "Empty the field", "waypoint": "axCalc.field|none",
     "actions": 1}
  ]
}
```

As built (2026-11-26): `apps` names the apps the file covers, each with its kind of window and the node it opens on;
`unplaced` the controls on view that take the focus and have no place ("axHelp: Name (Role)"), which `PlacesTests`
fails on; `kind` is the id of the first kind of event (`EventKind`, as `families.json` writes it) the action raised,
empty for none; `said` the window's announcements, joined, cut at 200 characters; `free` marks `outside`'s edges, the
apps' own keys, which no bound counts; an edge of no command has no `command`. The goals are written empty: their
counts are `docs/axKit/goal-counts.json` (WG-3.4), which changes with the scripts, and a goal gets its waypoint here
with the walks of step 4. One edge a line, the nodes and edges in the order of WG-2.4, `outside` first.

The record of edges (WG-4.3) stays in `workflow-record.json` with the goals', never in this file, so that a run
changes one file and the graph changes only with the windows.

## 6. The steps, in order

1. **This specification**, read and settled by the maintainer (section 9). Nothing else before it.
2. **The places and the graph written**: the places tables for the shared window types, then app by app; the channel
   says the place; the inventory run writes `workflow-graph.json` and `map.md`. Nothing a user meets changes.
   **Built 2026-11-26** (WG-1.1, WG-1.3, WG-2.2, section 5): the tables, the channel's place, the crawl, the file and
   `tools/map.ps1` (in `tools/pages.ps1` and its `-Check`). What the file covers is its own `apps`: the first write
   crawled every app the crawl may write, axAutomate, axCalc, axDown, axJSON, axMCP, axPreview and axTask (59 nodes,
   489 edges with `outside`'s). Left: the eleven apps the crawl must not write (section 10, point 5) get their nodes
   from the scripts' walks of step 4, and with them the hotkey window, none of whose apps may be crawled; and the
   modes of section 10, point 9.
3. **The bounds checked**: `graph-check.ps1` and the page of goals over the bounds, with each goal's own count
   (WG-3.3). The item "every goal in three steps" becomes this page.
   **Built 2026-11-26** (WG-3.3, WG-3.4, section 11): the first write counted 805 goals of `tests/scripts`, 242 over
   four actions and 116 at four, and found no node of the seven apps on the map more than two actions from a
   waypoint. The apps not on the map have their goals counted and no waypoint checked until step 4.
4. **The scripts as walks**: the landing of every step read and checked; the record by edge; owed hearings by edge.
5. **The gate**: the comparison at every run, the `Map change:` line in the hook.
6. **The shortenings, one app per sitting**: the maintainer decides each goal over the bounds from the page, the
   consent forms first. **Prepared 2026-11-26**: the scripts re-marked first, the steps that only test under check
   lines (`user-workflows.md` WF-3.1a, WF-3.11), which left 862 goals, 87 over four actions and 77 at four;
   `shortenings.md` holds a proposal for each of them, the working page of the sittings.
7. **Teaching**: the palette's way, the pages' shortest paths, AI help from the file.

Steps 2 and 3 are a few days of work; steps 4 and 5 about as much again; step 6 runs as long as the sittings do.

## 7. What stays as it is

The keys' tables (rule 5), the events (rule 13), the scripts' language but for `at`, the workflow record's goals,
the traces' rules (WF-5), the release's refusal while a window changed after its last pass. The graph adds a check;
it takes none away.

## 8. Risks

- **A window's places named wrong** makes a map that is tidy and false. The test of WG-1.1 holds that every focusable
  control has a place; whether the words are a user's is the maintainer's to hear.
- **The crawl on the hidden desktop** takes time: about 18 windows, a few nodes each, tens of commands. It runs with
  `--inventory write` only, never with every test run; the gate compares the tables and the committed file, and the
  crawl writes landings.
- **Modes multiply nodes.** Only a mode that changes an edge is one (section 2); a window with more than a few is
  itself a sign of keys that mean too many things.

## 9. Decisions for the maintainer

**Settled by the maintainer on 2026-11-26**, one by one:

1. Places are named in a table per window, beside its keys (WG-1.1 as written).
2. The key that opens an app is free: it is not counted, and the two actions to a waypoint start inside the window
   (WG-3.1 reads so from now on: from inside any window, every place of it in two actions; from outside, the app's
   key and then the same).
3. A consent form is one action, whatever is changed on it (WG-3.5 reads so).
4. A map change is approved by a `Map change:` line in the commit (WG-5.2 as written).
5. Goals over the bounds today are listed and may never grow longer, shortened app by app at his sittings (WG-3.4
   as written).
6. In the palette, Enter walks the whole way and Shift+Enter takes its first step (WG-6.1 reads so).

The questions as they were put, each with the assistant's recommendation first:

1. **Where places are named**: in a table per window beside its keys (recommended), or found from the controls'
   names alone (no table, but a renamed control is a new place).
2. **Does the launch key count as one of the two actions to a waypoint**: yes (recommended), so that reaching any
   app's main place is one key and its other places two.
3. **A consent form's boxes**: each box changed from where it starts is one action, Enter one more (recommended); or
   the form is one action whatever is changed.
4. **The approval of a map change**: a `Map change:` line in the commit, from his decision (recommended); or a list
   in axTask he signs, as the outside reports are.
5. **When a goal is over the bounds today**: listed and never allowed to grow longer, shortened app by app at his
   sittings (recommended); or the check fails at once and the work stops until all are shortened.
6. **What the palette does with a way** (WG-6.1): says it, and Enter takes its first step (recommended); or Enter
   walks the whole path.

## 10. Decided while building step 2 (2026-11-26)

The assistant's, each for the maintainer to turn back:

1. **A place's id is the app's name and the table's id** (`axCalc.field`): the kinds of window name their places once,
   in their own words ("the field", "the conversation"); an app gives other words only with a table of its own, which
   none needed. The words a user would use for one app's field ("the expression field") are left for a later table.
2. **The choice in a place is over it, not a place** (`choice`), as WG-1.3 lists it: axClaude's question and Go to
   line stand where the field or the text is. The second pane of the navigator is three places, one per kind
   (`fields`, `details`, `log`), since their keys differ.
3. **The bundle's keys are tried at every place of every window**, not at one node of each kind (WG-2.2): it costs a
   few seconds a place, and the map then holds every edge where it is pressed, with no second table of what a kind
   shares. They come from the window's own `places` answer, so the crawl holds no list of keys of its own.
4. **What the crawl never presses**: besides `act`, an app's own key (`outside`'s edges, free), New window (a second
   process), and the Context menu's keys: Windows' own popup menu held axStart's thread for 30 seconds on the hidden
   desktop, where menu mode never comes (`automation.md`, "What stays manual"). Their edges are for the walks of step 4.
5. **Which apps the crawl writes**: the graph is axKit's own words, as a kept trace is (`user-workflows.md` WF-5.6), so
   an app whose scripts may keep no trace is never crawled (`Traces.MayKeep`: axClip, axStart, axProcess, axTerminal,
   axGIT, axMedia, and the apps whose scripts need an outside program, axClaude, axHelp, axPass); nor axFile (Enter
   opens a file in the computer's own program, Left at the top lists the drives) or axTemplate (Enter types into the
   program in front). The first crawl of axStart put the count of the computer's programs into the words.
6. **A window brought back by a fresh start** when its status bar differs from what the place first showed: Ctrl+Space
   left axCalc's field in navigation mode, and every key after it was tried in that mode. The status bar is the only
   thing every window shows of its state; a key that changes nothing there is taken to change nothing that matters.
7. **The words are cut at 200 characters** and the kind is the first kind raised: the edge says what one hears first;
   the file is for the map and the check, not a record of everything said.
8. **The menu items without a key** are not tried: they are reached through the palette (F1, the name and Enter, two
   actions), whose node is on the map with its way back; the walks of step 4 add the rest.
9. **Modes**: none yet, as the windows report none in their state. The ones that change an edge, for a later step:
   the edit and navigation mode of a text (AX-8.13: the letters type or jump), axClaude's screen and stream and Claude
   busy or ready, axTerminal's screen view, the navigator's selection (Escape gives up a selection before it goes
   back) and its filter (Ctrl+Delete), and the level of a navigator where a key acts only at the top (Left).
10. **A Windows dialog is a node**, `dialog` over the place: the crawl presses its keys through the channel's `press`,
    which does not wait for what a key opens (Open, Save as and the font picker hold the window's thread in a loop of
    their own, and a `keys` request waited 30 seconds for each in the first crawl of axDown), and closes the dialog
    with `dismiss`, as its Cancel does; the edge back is written as the user's Escape.

## 11. Decided while building step 3 (2026-11-26)

The assistant's, each for the maintainer to turn back:

1. **A rule in place of marks** (WG-3.3): the spec let a goal line mark its first and last counted step; a rule read
   off the steps needs no change to 800 goals and says the same thing for each, so the example is told apart by
   where it stands (typing first, a command after it), and what the rule misses is on the page to see.
2. **A command without a key is two actions**, the palette's way (F1, then the name typed with its Enter), as section
   10, point 8 counts it; with a key, one, whatever way the script chooses it.
3. **A run of moves is one action**, as the non-goals of section 3 keep moving inside a place off the map: the lines
   of a text and the rows of a list are where the user already is, and G7's "one choice in a list" is that run with
   its Enter.
4. **A new goal over four is held as a grown one is**: the map may get shorter, never longer (decision 5), and a goal
   written today over the bounds is a longer way; `-Write` takes it in at the maintainer's word.
5. **The waypoint bound stays inside the window**: an edge to another app or to `outside` is not followed, since the
   app's free key back would make every place one action from anywhere.
