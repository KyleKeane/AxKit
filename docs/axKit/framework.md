# The framework: a few window types on shared components

A proposal for the maintainer's reading, written on 2026-11-22 at his direction of that day. Nothing in it is built
unless it says "built". Requirements are numbered FW-n.m; what is his to decide is in section 9. The facts about
how each window is made today were read from the sources that day (a survey of every window, three of its points
checked by hand) and are in section 10.

His words: "we have not only a component library, but we also have an interaction and window layout and workflow
framework and we try to enforce a constrained set of discrete window types that all use shared infrastructure ...
this should be the smallest possible set of interfaces that can support as many programs as we can map into them
... the more effort you put into this in order to unify things, the more success we will have moving forward
keeping things simple and manageable and most importantly keeping the user interface highly consistent and easy
to tweak if something goes wrong."

And, the same day, on how far to go: "we can still completely remap certain apps to be on the new framework so
please feel free to try to force the other apps into the shared framework ... do not get too attached to the
current design of any of these apps since nobody is yet using the system seriously except myself and i want
things to consolidate as much as possible so i do not need to remember a bunch of things beyond the simplest set
of heuristics." And: "once you have spec you should likely break this into smaller achievable and manageable
chunks of work." So where today's design of an app and the simplest shared shape differ, this document takes the
shared shape and says what goes (section 9), and section 8 is the work in small pieces.

## 1. Summary

- **Components.** A window is made only of a few canonical pieces: the list, the text to read, the field, the
  choice in the field's place, the form, the notice, and the furniture every window has (menu bar, Context, status
  bar, palette). Each is one class of `AxKit.Forms`, with its keys, its words and its entry event.
- **Window types.** Every app's window is one of four types, each one piece of shared code: the conversation
  window, the editor, the navigator, the hotkey window. A fifth, the graph, comes much later and is not started.
- **An app adds only** its data (what the rows or the text are, how they are read and written), its commands on
  them, its keys, and its message passing with the program under it.
- **Today** all seventeen apps are on a type; of the five that had a window of their own when this was
  written, axMedia, axProcess, axGIT and axFile are navigators and axCalc is a conversation window since
  2026-11-22. The work left is to move the rest of axFile's commands onto the navigator's own (steps 13 to 15), to
  make every list of items the one selectable list, and to lift what two apps of one type still do each in their
  own code into the type.
- **Held by tests**, not by memory: which classes a window may derive from, which controls it may hold, and that
  every component raises its entry event.

## 2. Words

- **Component**: a control of the shared code that a window is composed of.
- **Window type**: a window's whole shape and interaction, as one shared class an app fills in.
- **Pane**: a component of a window that can hold the focus.
- **The row you are on**: the row the arrow keys chose, which NVDA reads. **Selected** rows are the ones marked
  with Space (SPEC AX-8.16). The two are never called by the same word.
- **Level**: what a list shows that one can go into and come out of (a folder, a list of items).
- **View**: another content of the same list, changed by a key, with its own row kept (Changes, Log, Branches).
- **Entry event**: the kind of event a component raises when the focus arrives in it from outside it.

## 3. The components

- FW-3.1 **The list** (`AxKitList`, with `ListSelection` for selecting): rows moved through with the arrow keys.
  Type-to-find, the edge sound at an end, rename in place and the selection are the component's. Every list of
  items a user acts on is this component with its selection on: Space selects everywhere (his decision), with the
  keys and words of AX-8.16. A row that cannot be selected says why.
- FW-3.2 **The text to read** (a read-only `AxKitTextBox`, the conversation's `TranscriptView`, a notice's text):
  the caret moves only on the user's command; letters jump by heading and the like; Ctrl+F finds.
- FW-3.3 **The field** (`AxKitTextBox`): one line or several, with the mode key (AX-8.13) and "new line" said.
- FW-3.4 **The choice in the field's place** (`ControlArea`): what a window waits on, a list of answers or a
  small field, shown where the field is, never a second window.
- FW-3.5 **The form** (`FormControls` through `ShowForm`): several standard controls for one action.
- FW-3.6 **The notice** (`OverlayPanel`): a text with its buttons over the window; a question is a notice.
- FW-3.7 **The furniture**: the menu bar in the bundle's order (AX-9.1), the Context menu of what you are on
  (`ContextBarMenu`), the status bar of two parts (`StatusLayout.Pair`), the command palette, F1.
- FW-3.8 **Entry events, raised by the component itself** (his decision of 2026-11-22: "as automatic as possible
  so it is not another coding paradigm that might be forgotten"; "Every focus entry, automatic"). When the focus
  arrives in a component from outside it (something opened, Tab between panes, back from a notice), the component
  raises its kind and the kind's sound plays (the family Openings, AX-7.11). Moving inside it is silent. No window
  raises these. A new component cannot exist without declaring its entry event. Built on 2026-11-22 (step 2 of
  section 8; SPEC AX-7.11): the raising moved from the notice, where it was first built that day, into the components.

| Component | Class | Entry event | Its sound |
|---|---|---|---|
| List | `AxKitList` | list entered | the mark, four beeps down |
| Text to read | read-only text | text entered | the mark, one low tone |
| Field | `AxKitTextBox` | field entered | the mark, one tone |
| Choice in the field's place | `ControlArea` | list or field entered, by what it holds | as those |
| Form | `FormControls` | form entered, once for the form, not per control | the mark, beeps and tones |
| Notice | `OverlayPanel` | the entry event of what holds its focus | as that |

## 4. The window types

### 4.1 The conversation window (`ConversationForm`; over a program, `ConsoleForm`)

- FW-4.1 Two panes: the text of what was said, above, and under it the field, or the choice that stands in its
  place. Every pane key switches between them; Escape in the text goes to the field. A line of the text can be
  taken into the field with Enter. For a program one talks to in turns.
  The type is two classes since 2026-11-22 (step 9, first part): `ConversationForm` is the shape alone (the text,
  the field, the choice, the pane keys, find, the bookmarks, the Context menu on a line, the status bar) and knows
  no program; `ConsoleForm` derives from it and adds a program in a pseudo console (the process, the writes, the
  recording, the signals and the music, the folders, the question before closing). The split moved code without
  changing it: axClaude's and axTerminal's windows, their scripts and their command lists are as they were. A
  window without a console program derives from `ConversationForm` (axCalc, the second part of the step).
- Apps today: axClaude and axTerminal, whose difference is the program, its keys and how messages pass; and axCalc
  (step 9, 2026-11-22), which has no program: its text is the answers given, one line an answer, in a line store of
  its own, and Enter on a line takes the line's expression into the field.
- FW-4.2 **To lift into the type**, since both apps do it in their own files in the same way today (section 10.5):
  Enter sends and Escape only announces in the field; Ctrl+Enter sends; a line taken into the field; the three
  places on Alt+1, Alt+2, Alt+3; the Session menu; the Options of sounds, speech and "when it is done"; the
  folder's handling; the restart; the question before closing; the consent before the first start.
- Other programs of this kind (a second agent, a database shell) fill the same places and get the same window
  (rule 12).

### 4.2 The editor (`TextEditorForm` with `EditorApp`)

- FW-4.3 One pane, the text, with the choice in place under it for Go to line and for the options of a place; a
  read-only variant with a list of details under it.
- Apps today: axDown, axJSON, axPreview. They share the whole shell already; each gives its jumps, its commands,
  and its offers.
- FW-4.4 **The options of a place** (`EditorOffer`: Alt+Right the list of them, Alt+Down and Alt+Up the next and
  the previous, Alt+Left add one; axJSON's AJ-5) are the type's, open to every editor. axDown has none yet; what
  it would offer (a heading's level, a link's target, a list item's check) is a later item.
- Nothing structural is left to do in this type.

### 4.3 The navigator (`ItemForm`, made general)

- FW-4.5 One list of rows, with levels to go into and out of; optionally a second pane that shows more of the row
  you are on. **One rule for moving**: Right goes into a row that holds a level, Left and Backspace go up. Enter
  opens the row (goes in, or does the row's own opening), Space selects, Ctrl+Enter is the app's main action,
  Delete removes, F2 renames where rows have names, Escape gives up a selection, then a filter, then goes back
  from the second pane. Every pane key switches panes.
- Apps today: axTask, axPass, axMCP, axAutomate, axMedia, axProcess, axGIT, axFile.
- FW-4.6 **No views.** axGIT and axProcess each show three contents in one list today, changed with Alt+1 to
  Alt+3 (and Tab in axProcess). In the navigator those are three rows of the top level that one goes into with
  Right: in axGIT Changes, History and Branches; in axProcess Processes, Services and Startup. The count of each
  is in its row, as a list's count is in axTask. So a second way of changing what a list shows goes, and with it
  its keys. Each level keeps the row one was on, as levels do.
  **Built for axProcess on 2026-11-22** (step 7): `ProcessForm` derives from `ItemForm`. The window starts inside
  Processes, so that the common use costs no key; Left goes up to "This computer, 3 lists". Tab no longer goes
  round the three. Alt+1, 2 and 3 are kept, as jumps straight into a list from anywhere, like axTask's keys to a
  list (my reading of the decision below: what goes is the second way of changing what the list shows, and a jump
  to a level is not one; they are a line of the step's item for the maintainer's word, and of the key map's
  audit, step 16). Reading off the UI thread needed nothing new in the type: the app turns the type's own way
  into a list down with an empty reason (`EntryRefused`), reads, and only then sets the level and fills. Space
  selects processes and startup programs, and Delete acts on all the selected after one question.
- FW-4.7 **What the type must gain to take them**, each from the app that has it today:
  1. Reading a level off the UI thread, a late answer dropped, "reading" as a state (axFile, axGIT, axProcess).
  2. The second pane as one of three kinds, or none: the fields of the row (today's detail pane), a text to read
     (axGIT's details), a log of lines (axFile's Actions).
  3. Filter and sort (axFile, axProcess).
  4. The selection always available, rows saying whether they can be selected (built for axTask and axFile).
  5. The line of an empty level and the line that says what is not shown (axFile).
  6. The rows offered in the command palette (axFile).
  7. An area of the app's own beside the panes (axMedia's video), and the status bar's first part free for what
     the app has to say of the moment (what plays).
  Built on 2026-11-22, with nothing changed for the four item apps (`ItemForm`: `Pane`, `PlaceWith`,
  `StatusState`, `EmptyLine`): the pane as none and as a text (of point 2: `SecondPane`, `TextPane`,
  `ShowText`), the line of an empty level (of point 5), and point 7; and a shared key the app gave to a command
  of its own that acts anywhere is left to the app. Point 1 needed nothing in the type for axProcess (FW-4.6).
  Built with axFile's move (step 13, 2026-11-22): the log kind of the pane (`SecondPane.Log`: the pane's list is
  the app's to fill, `LogLineMenu` is what Context holds on its line, `LogName` names it in the pane keys'
  commands), the line of what is not shown (`AfterRowsLine`; a command on a line that is no row says that line,
  `NoRowSaid`), and a shared key the app gave to a command of its own in the very place the shared one acts is
  left to the app (`KeyTakenWhere`), which is what lets a window move onto the type with its own commands first
  and give them up one by one. Point 1 needed nothing in the type for axFile either (`EntryRefused` starts the
  read; `UpLevel` is the app's where the level above is read too). Still to build: points 3 and 6, which axFile
  does in its own code.
- FW-4.8 After this, axTask and axFile are the same window: one shows folders of items with a pane of fields, the
  other folders of files with a pane of the operations made. Their difference is their data and their commands.
- FW-4.9 **axMedia is a queue and its controls**, a navigator of one level whose rows are what will play (the
  maintainer: "maybe it just uses axfile as the content chooser and axmedia is just a queue and control area").
  Files are chosen in axFile and handed over, as today (Ctrl+Enter plays a folder, Ctrl+Shift+Q adds); axMedia
  keeps no file browsing of its own beyond Open, which is Windows' picker. Space selects rows, Delete takes the
  selected ones out, Ctrl+Enter plays and pauses, Enter plays the row.
  **Built on 2026-11-22**: `MediaForm` derives from `ItemForm` with one level and no second pane; the list, the
  selection, find, the Context menu and the navigator's keys are the type's, and what is left in the app is the
  queue's commands and the player. What the move showed the type needs for a window like it, all built into
  `ItemForm`: Enter on an item as the app's own (`OpenItem`), the app's word for a row (`RowNoun`), a Context menu
  of one kind of row, and the navigator's keys an app has no use for left out (`KeysWithoutUse`: a queue has
  nothing to rename, to undo or to refresh, and no second list, so F2, Ctrl+Z, F5 and Ctrl+Shift+N are no keys
  of axMedia, rather than keys that say "not here"). Four of the navigator's keys stay axMedia's own until the
  key map is gone through (step 16): Ctrl+N is New window, Ctrl+Q What is playing, Ctrl+Up and Ctrl+Down the
  speed. What changed for the user: Right, Left and Backspace in the queue play the edge sound (they moved the
  row before, as in any Windows list), the Context menu's item for an empty queue reads "Queue is empty", the
  words of the empty line, and the status bar says how many files are selected.
- FW-4.10 **axGIT is made simple** (the maintainer has not used it and keeps nothing of its design): the three
  levels of FW-4.6; in Changes, Space selects files, Ctrl+Enter is the main action, commit, and staging is a
  command on the selected files; Enter shows the row's text in the second pane.
  **Built on 2026-11-22** (step 8): `GitForm` derives from `ItemForm` with the text pane. The repository's level
  holds Changes, Log and Branches (the names git's own words gave them; "History" above was this document's word
  for the Log); the window starts inside Changes; Alt+1, 2 and 3 are kept as jumps, as in axProcess. Stage is
  Ctrl+Shift+S and Unstage Ctrl+Shift+U, on the selected files or the file you are on; Stage all and Unstage all
  lost their keys, since Ctrl+Shift+A is the bundle's Deselect all. Delete discards the selected files after one
  question, Cancel the default. The trust of a repository, the consents and the one-command-at-a-time rule are
  untouched, and every option of git's is still written in the core alone.

### 4.4 The hotkey window (`HotkeyForm`)

- FW-4.11 A window that a key brings up from anywhere: a field that always holds the caret, and under it a list
  the arrow keys move through from the field. Enter does the app's thing with the row; Escape hides.
- Apps today: axStart, axClip, and axHelp, which has a text and no list.
- FW-4.12 Its list is a plain list box that never takes the focus; it becomes the canonical list in this one mode.
  Since the field has the keyboard, Space types a space there: this type's list has no selection of several, the
  one stated exception to FW-3.1 (section 9, decision 7).

### 4.5 The graph (later)

- FW-4.13 A viewer and editor of things and their links. Named by the maintainer as the one further type, "much
  later when everything else that is core is stable and deployed". Not designed here, not started.

## 5. Every app, mapped

| App | Today | Proposed | What stays the app's own |
|---|---|---|---|
| axClaude | conversation | conversation | Claude Code's adapters, its questions, background work, put away |
| axTerminal | conversation | conversation | the shell's adapter, shells of one's own, the command history |
| axDown | editor | editor | Markdown's jumps and keys |
| axJSON | editor | editor | JSON's outline, Format, its options of a place |
| axPreview | editor, read-only | editor, read-only | the renderers, the file's details |
| axTask | navigator | navigator | lists as folders, tasks, smart lists, actions, projects |
| axPass | navigator | navigator | KeePassXC's tool, the lock, the clipboard's clearing |
| axMCP | navigator | navigator | the servers, calling a tool |
| axAutomate | navigator | navigator | the workflows, running them |
| axFile | navigator | navigator, a log as second pane | file operations and their undo, the shell, the clipboard, zip, favourites, drives |
| axGIT | navigator | navigator, three levels, a text as second pane | running git, the repository's trust, its commands |
| axProcess | navigator | navigator, three levels, no second pane | reading processes, services and startup; ending and changing them |
| axMedia | navigator | navigator, one level, no second pane: a queue and its controls | the player, the hand-over of files, the video |
| axCalc | conversation | conversation | the evaluator, variables, bases |
| axStart | hotkey | hotkey | finding programs, starting them |
| axClip | hotkey | hotkey | the clipboard's history |
| axHelp | hotkey, a text | hotkey, with the conversation window's text and field | the conversation with Claude about axKit |

## 6. What an app may hold

- FW-6.1 An app's window class derives from one of the type classes and from nothing else. Only the type classes
  derive from the window base.
- FW-6.2 An app makes no control of its own kind: every control it places is a component of section 3.
- FW-6.3 An app's code is: where its rows or its text come from and how a change is written; its commands on
  them, each one entry of the window's command list; its key table; its talk with the program under it, behind
  that program's one adapter (rule 12, `events.md`).
- FW-6.4 A difference between two apps of one type is one of those, or it is removed. A difference kept is
  written in the app's specification with its reason.

## 7. Held by tests

- FW-7.1 `LayoutTests`: every window class of an app derives from a type class (FW-6.1). Until an app has moved,
  it is on a short list of the ones still owed, which only shrinks.
- FW-7.2 `ListTests` and its like: every list is the list, every text box the text box (held today for lists).
- FW-7.3 A components file, `docs/axKit/components.json`, names each component with its class, its entry event and
  its keys by family, and each window type with its panes; a page is written from it by a script, as the key map
  is; a test holds the sources to it.
- FW-7.4 `EventDoorTests`: no window raises an entry event; a component does.

## 8. The path, each step leaving every app working

The work in small pieces, in order. Each is one item of the priorities list, named "Framework step" with its
number, small enough for one sitting or two, and ends with every app working, its window scripts passed and an
NVDA pass recorded. A piece that changes what a user does updates the app's specification and guide in its commit.

**A. The ground**

1. The components file, its page and its tests (FW-7.3), and the rule that a window derives from a type class,
   with the five windows of their own on the list still owed (FW-7.1). No behaviour changes.
2. The entry events raised by the components themselves (FW-3.8): the list, the text, the field, the form.
   The sounds of what opens then come from the components, at every focus entry.

**B. The navigator made general, with nothing changing for axTask, axPass, axMCP and axAutomate**

3. A level read off the UI thread, with a late answer dropped and "reading" said (FW-4.7, point 1).
4. The second pane as fields, a text, a log, or none (point 2).
5. Filter and sort, the line of an empty level, the rows in the palette (points 3, 5, 6).

**C. The apps moved, smallest first**

6. axMedia: the queue as a navigator of one level; Space selects, Ctrl+Enter plays and pauses, Delete takes the
   selected rows out (FW-4.9). Built, 2026-11-22.
7. axProcess: three levels, Processes, Services, Startup; Space selects, Delete ends or removes the selected.
   Built, 2026-11-22.
8. axGIT, made simple: three levels and a text pane; Space selects files, staging on the selected, Ctrl+Enter
   commits (FW-4.10). Built, 2026-11-22.
9. axCalc onto the conversation window: the field and the results above it. Built, 2026-11-22, in two parts: the
   conversation window split into its shape (`ConversationForm`) and the console window over it (`ConsoleForm`),
   then `CalcForm` on the shape.
10. The selection in axPass, axMCP and axAutomate, where a command acts on several rows. Built, 2026-11-22: in
    axPass Delete moves the selected entries to the Recycle Bin with one undo; in axMCP Delete removes the selected
    servers of your own with one undo; in axAutomate F5 runs the selected workflows. Each app says which rows can
    be selected, and they are the ones its command acts on.

**D. The conversation window**

11. What axClaude and axTerminal each do the same way, lifted into the type, in two or three commits: the field's
    keys and a line taken into it; the menus and options; the folder, the restart and the closing question.
12. The hotkey window's list as the canonical list, and axHelp's text and field as the conversation's components.

**E. axFile, last and with the most care**

13. axFile's folder list on the navigator: levels, reading, the row you are on, rename, delete, find. Built,
    2026-11-22: `FileForm` derives from `ItemForm`; a folder is a level and its entries the rows; the
    two lists, the split, the status bar, the selection, the pane keys, Right and Left, the Context keys,
    Escape and the rename box with its steps are the navigator's. Enter, Backspace, Delete, find, refresh and
    undo stay axFile's own commands on the navigator's conditions, and that is where they belong: each is the
    app's part of a navigator, its commands on its rows, with the app's own mark (opening a file starts a
    program, a rename and a delete act on the disk, so an AI tool asks first) and its own words
    (docs/axFile/SPEC.md AF-D14).
14. Its Actions log as the navigator's second pane of the log kind; its selection no longer written there. Built,
    2026-11-22: the pane with step 13; the selection's lines, their section and their undo are gone from the log.
15. Its own parts placed on the type: filter and sort from the type; favourites, drives, Go to folder, the
    clipboard, zip and the shell as its commands. The old window's code goes.

**F. What it was for**

16. The workflows that result, as one inventory; then the key map; then the function keys and Ctrl with a number
    as classes of interaction (their items in the priorities list).

Pieces 1 and 2 are days. B is the foundation and has no visible change. C and D are a sitting or two each. E is
the long part.

## 9. Decisions

### Taken under his word of 2026-11-22, "do not get too attached to the current design"

Each removes something that exists today. Each can be turned back while its piece of work has not begun.

1. **Views go; levels stay** (FW-4.6). axGIT's and axProcess's three views become three rows to go into. Alt+1 to
   Alt+3 no longer change views there, and Tab in axProcess no longer cycles them.
2. **axCalc becomes a conversation window.** A field with its results above it, a result taken back into the
   field with Enter on its line. What goes: removing one history line with Delete; Clear history stays.
3. **axFile's selection is no longer written into its Actions list.** Space and Escape undo a selection; the log
   holds only what changed files. Built, 2026-11-22 (step 14).
4. **axMedia is a queue and its controls.** It keeps no browsing of its own; axFile chooses and hands over.
5. **axGIT is made simple**: staging acts on the selected files, where Space staged one file until now.
6. **Tab switches panes in every window**; where there is one pane it does nothing.

### Decided by him on 2026-11-22 ("Yes, take all four as proposed"; axCalc: "should just be using the same as axclaude and axterminal")

7. **No selection of several in the hotkey windows**, since Space types into their field: the one exception to
   "Space selects".
8. **axHelp** stays a hotkey window, since being called from anywhere is its point, with the conversation
   window's text and field inside it.
9. **The second pane of the navigator** in three kinds (fields, text, log), no more.
10. **Enter on a row** keeps the app's own opening (a form in axTask, the file in axFile, the text in axGIT, the
    details in axProcess, play in axMedia): the one key in which apps of the type differ.

No point is open.

## 10. As read from the sources on 2026-11-22

Compact; the windows' own notes hold the detail.

### 10.1 What every window has from the base

The menu bar in one order; a command list (`Add`, `AddKey`, each command with where its key acts); F1, F4, F8, the
palette, Ctrl+S and the text size handled once; notices that replace the main controls; a status bar of two parts.

### 10.2 The four types today

| Type | Panes | Pane keys | List keys of note | Reading |
|---|---|---|---|---|
| Conversation | text; field or choice | Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab, F6 | the choice's own | off the UI thread |
| Editor | text; choice; details list when read-only | Ctrl+Tab, Ctrl+Shift+Tab, F6; Tab types in the editor | Enter and Ctrl+C in details | details off the UI thread |
| Navigator | list; detail list | all five; Escape back from the detail | Enter, Right, Left, Backspace, Space, Delete, F2, Ctrl+N | on the UI thread; apps add their own |
| Hotkey | field; list that never has the focus | none | Down, Up, Page keys from the field; Enter the app's | the app's |

### 10.3 The five windows of their own

| App | Panes | Pane keys | Space | Enter | Ctrl+Enter | Levels or views | Reading | Its own part, lines |
|---|---|---|---|---|---|---|---|---|
| axFile | folder list; Actions list | all five; Escape back | selects | opens | plays the folder in axMedia | levels | off the UI thread | about 2,000 of 2,600 |
| axGIT | list; details text | Ctrl+Tab, F6; Tab native | stages | by view: diff, commit, switch | commit | three views | off the UI thread | about 750 of 1,170 |
| axProcess | list | none; Tab cycles views | nothing | details | nothing | three views | off the UI thread | about 400 of 740 |
| axMedia | list | all lead back to the list | pauses | plays | nothing | one level | hand-overs off the UI thread | about 550 of 775 |
| axCalc | history list; field | Ctrl+Tab, F6; Escape back | nothing | recalls; evaluates in the field | nothing | none | on the UI thread | about 230 of 430 |

### 10.4 The navigator and axFile side by side

Both have, under different names and code: going into a level and up; what a level change says; the row you are
on and its guards; filling and refresh; the selection (both on the shared piece since 2026-11-22); type-to-find;
rename in place; delete; new; undo; the Context menu; find in the list; a second pane; pane switching; the status
bar; the target an AI tool's command is asked about.

Only the navigator has: the pane of fields with its field commands; sections and headings; a level that refuses
entry; a refill off the user's command; a place that changes alone in the status bar.

Only axFile has: filter; sort; favourites; drives and This PC; Go to folder with completion; recent folders; the
Actions log; the clipboard; reads and operations off the UI thread; zip; the Windows shell; the hand-overs to
other apps; its rows in the palette; the lines for an empty folder and for what is not shown.

### 10.5 What axClaude and axTerminal each do in their own code, the same way

Enter sends and Escape guards in the field; Ctrl+Enter sends; a line taken into the field; the places on Alt+1 to
Alt+3 with find and bookmarks; the Session menu in one order; Options of sounds, speech and "when it is done";
choosing, setting and switching the folder; the restart with its question; the question before closing; stopping
the program; the consent before the first start; the guards of a key sent while nothing runs.

### 10.6 axGIT and axProcess

(As found on 2026-11-22, before their moves; both are navigators now and the shared record of a view is deleted.)
Both kept three views in the same way (a shared record: a name, labels and keys per view, the row kept per view),
with the same method to change view, the same per-view Context menu and the same refresh. They differ in the
second pane, in what Tab does, and in what a refresh reads.
