# Shortenings

A working page for the maintainer's sittings, by hand, 2026-11-26: the goals still over four actions, or at four,
after the scripts were re-marked (`docs/axKit/user-workflows.md` WF-3.1a, WF-3.11), each with its count, its steps
in a few words and the assistant's proposal to bring it to three or four (SPEC.md G7, `workflow-graph.md` WG-3).
The counts are those of `over-the-bounds.md` on that day: 862 goals, 87 over four actions, 77 at four. This page is
deleted once each app's sitting has decided; what is decided goes into the app's SPEC.md and key table, and the
scripts follow.

"Example" marks a goal that is long because of what the script does around the user's work (a refusal tested
before the task, a selection or a search made first, text typed to have something to work on), not because of the
window. Those steps cannot be put under a check without moving them, which the re-marking did not do (WF-3.11): a
later pass that may move them into a check before the goal, at the maintainer's word, brings most of them within
four with nothing changed in a window. The other proposals change what the user presses; each says what.

## Changes that shorten many goals at once

- **Notice answers by letter** (group *letters*): the notices that ask a question take the Alt letter of each
  answer, as axGIT's discard question (Alt+D) and axProcess's details already do: Reload and lose mine, Don't save,
  Close anyway, Run it once, Put it there once, all lists. Two Tabs and an Enter become one key. Running a command
  stays off the default answer (rule 11). About 10 goals. Recommended.
- **Reminders open on their box** (group *reminder*): the consent and agreement reminders open with the focus on the
  "I understand" box, the reminder's text read as it opens, so that Space and Enter agree. One Tab less in 10 goals
  (axClaude, axGIT, axTerminal, axPass, axDown, axMCP three times, mcp). Recommended.
- **A field opened for a change has its text selected** (group *selected*): typing replaces the value, and Ctrl+A
  goes. About 7 goals (axTask, axTemplate, axClaude). Recommended for the forms' fields and the find fields that
  open with the last search.
- **Keys for the commands that only the palette reaches** (group *palette*): a command without a key costs two
  actions (F1, its name and Enter). About 40 goals reach one; most are settings switched once (axClaude's Options,
  axFile's hidden files, axCalc's history). Recommended: keys only for the commands used often (axFile's Recent
  operations, Filter, Move to folder; axTask's Set fields and History; axDown's List item), each in its key family
  (AX-8.18), and for the switches one key that cycles axClaude's Speak replies; the rarely used settings stay in the
  palette and their goals are accepted at four, or given a direct Options place in the sitting.
- **Keyboard shortcuts and the user guide by F1** (group *help*): F1's palette with nothing typed reads the rules of
  the keys and the window's keys first (AX-10.2), so the goals that open Keyboard shortcuts or User guide by name
  (two actions) have a one-key way already. Recommended: no new key; the guides name F1 for the keys, and the
  scripts keep the named commands as checks of the second route.
- **The scripts take the shortest way the window has** (group *script*): Shift+Down to select a range, Ctrl+Enter
  to save a form from any field. Nothing the user presses changes; about 6 goals.

## axTask (24 over four, 11 at four)

- **Change several fields of an item and save them as one step** (18), 17: six fields changed in one form, then
  Ctrl+Enter. Example: the form already gathers them; each field is one or two actions. Accept.
- **Rename an item in its form** (18), 12: a link set first through six Tabs and Ctrl+Enter, then Down, Enter,
  Ctrl+A, the title. Example; the rename is 4, and 3 with *selected*.
- **Set fields of the selected items** (28), 10: Space, Down, Space, Set fields (2), Tab, Down, Tab, Garage,
  Ctrl+Enter. *script* (Shift+Down) and *palette* (a key for Set fields): 7; the rest is the example's two fields.
- **Search every list** (05), 9: a search that finds nothing first, then Ctrl+Shift+F, Ctrl+A, TAP, Down, Tab,
  Shift+Tab, Escape. Example; the user's search is 3, and *selected* removes the Ctrl+A.
- **Put a command into axTerminal's command field** (19), 8: Look in the terminal (2), Enter (cancel), again (2),
  Tab, Tab, Enter. Example (the cancel) and *letters*: 3.
- **Add a project folder by its path** (05), 7: Add a project folder (2), two refused paths each with Ctrl+A, the
  path. Example; 3 without the refusals, 2 with *palette*.
- **Fill in an action's form** (19), 7: Find what I type (2), Enter (refused, empty), the text, Tab, Tab, Enter.
  Example (the refusal) and *letters*: 4.
- **Mark several tasks done at once** (20), 7: Home, Space, Down, Space, End, Space, Ctrl+Enter. Example: three
  scattered items selected. Accept.
- **Put a list in a section from its details** (03), 6: Ctrl+Up, Down, Tab, Enter, Jobs, Escape. The long way on
  purpose; Ctrl+M on the list is 3. Accept, or drop the goal for Ctrl+M's.
- **Clear fields of the selected items** (28), 6: Clear fields (2), Ctrl+Enter (refused), End, Space, Ctrl+Enter.
  Example (the refusal); *palette*: 3.
- **Open an item's fields** (01), 5: Enter, Enter (form), Escape, Tab, Down x3. Example: the form opened and left
  first; 3 without.
- **Add an item** (01), 5: Left, s, Right (reaching Shopping), Ctrl+N, Bread. Example; 2 once in the list.
- **Write notes of several lines** (02), 5: Up Enter, a line and Enter, a line, Tab, Enter (Save). *script*
  (Ctrl+Enter saves): 4.
- **Link one item to another** (02), 5: Escape, Right, Tab, End Enter, Washer. Example (reaching the item); 3.
- **Take a field out of a list and keep its values** (12), 5: Refresh, Right, Tab, Down Down Enter, Escape. The
  configuration file is written by the script; axTask reading the file again when it changes removes Refresh: 4.
  For the sitting (group *watch*).
- **Convert a list of the earlier format** (16), 5: ho, Right, Escape (cancel), Enter, Enter. Example; 3.
- **Give a list its configuration** (17), 5: Left, Tab, End, Shift+Tab (reading the details), Ctrl+J. Example; 2.
- **Add an item to a list that has no configuration yet** (17), 5: Left, em, Right, Ctrl+N, the name. Example; 2.
- **Open the form on a field from the detail pane** (18), 5: Up, Tab, Down x4 Enter, Ctrl+A, Bathroom.
  *selected*: 4.
- **Run a program on an item, and hear how it ends** (19), 5: the action key, Tab x3, Enter. *letters* (A for do
  not ask again): 2.
- **Stop a running action** (19), 5: Wait (2), Tab, Tab, Enter. *letters*: 3.
- **Archive several items at once** (20), 5: Home, Space, Down, Space, Delete. *script* (Shift+Down): 3.
- **Move the selected items to a list chosen in a form** (20), 5: Space, Down, Space, Ctrl+M, Enter. *script*: 3.
- **Read what changed in an item** (29), 5: Ctrl+Enter and Undo (making the history), History (2), Escape. Example;
  *palette* (a key for History): 2.
- At four: **Move among the lists by their headings** (01) and **Move among the sections by their headings** (03):
  a tour of the heading keys, an edge test first. Example; accept.
- At four: **Set a due date by typing a weekday** (01): Tab, Enter, Ctrl+A, friday. *selected*: 3.
- At four: **Move a list back to Your lists** (03): Ctrl+Up, h, Ctrl+M, Home Enter. Example; 2 once on the list.
- At four: **Open a project and mark a task done in it** (05): a tour into the project. Example; accept.
- At four: **Read which fields a list's items have** (12): Tab, End Enter, Escape, Shift+Tab. F7 on a list could
  read its details with the fields: 1. For the sitting.
- At four: **Put a field back and find its values again** (12): Escape, Refresh, Tab, moves. *watch*: 3.
- At four: **Make a smart list and see its file open in axJSON** (14): New smart list (2), Soon, closing axJSON.
  *palette*: 3; the close is the script's.
- At four: **A wrong passphrase approves nothing** (15): its title is a check's (a refusal). Recommended: the
  maintainer makes its line a check line.
- At four: **Approve an entry with the passphrase** (15): Approve this report (2), Tab, the passphrase. *palette*:
  3; OR-5.1 keeps the focus on the entry's text first, so the Tab stays.
- At four: **Convert every list of the earlier format at once** (16): Enter, Tab, Tab, Enter. *letters*: 2.

## axClaude (18 over four, 19 at four)

Most of axClaude's scripts need Claude Code and a live model; their marks were not run (see the report of the
re-marking).

- **Close every window at once** (13), 12: axDown opened and typed in, Claude working, Close all windows (2), then
  each window's question answered. Gather the questions into one notice in the window that asked ("1 unsaved
  document, Claude working: Save and close all, Close all without saving, Keep"): 3. Recommended.
- **Quote two lines in a message and send it** (04), 11: twice Alt+2, Ctrl+Home, I or C, Enter, then the field's
  moves and Enter. A quote key that keeps the caret in the conversation saves the second Alt+2; the Ctrl+Home
  moves are the script reaching its lines: about 7. For the sitting.
- **Move through a message by paragraph** (02), 10: nine typing steps write four paragraphs, then one move.
  Example; the user's part is one move. Accept.
- **Answer a question with several answers** (12), 8: the message, Home Down Enter, then Home, Space, Down Down,
  Space, Enter. Partly example (Home for safety); accept.
- **Review answers before they go to Claude** (12), 8: Review answers (2), the message, three answers each with
  Home. *palette*; the Home presses are the script's. Accept.
- **Go back to where you were before a jump** (04), 7: Ctrl+Home and five jumps build the history, then Backspace.
  Example; the user's part is 1.
- **Send the report** (16), 7: Shift+Tab x5 back to the words, Ctrl+Enter, Escape. Ctrl+Enter sends from any
  control of the form, so the walk back is the script's: 2. *script*.
- **Have questions come without interrupting speech** (12), 6: Go to them at once (2), the message, Alt+2, Home
  Enter. *palette*; the message is the example's.
- **Select lines of a reply and clear the selection** (03), 5: Alt+2, Ctrl+Home, O, Shift+arrows, Escape. The
  script goes to the first reply on purpose; with Alt+3 to the latest reply: 3. Example.
- **Find the next match from the message field** (04), 5: Alt+1, Ctrl+F, the word, Alt+1, F3. Example (the find
  set up again); the user's part is 2.
- **Start a new session with arguments of your own** (07), 5: Ctrl+N, Down Down (reading), Tab x3 to the arguments.
  Open the New session notice on the chosen way to start with one Tab to the arguments: 2. Recommended.
- **Hear each tool call as it starts** (09), 5: Speak replies: none (2, undoing the goal before), Speak each tool
  call (2), a message. *palette* (the cycling Speak replies key); the first is the script's reset.
- **Hear the ways to install Claude Code** (10), 5: Tab five times through the notice's buttons. Make the notice's
  ways one list moved by arrows: 1 or 2. Recommended.
- **Browse earlier side answers** (14), 5: the /btw message, Tab, Tab to Browse, Enter, Escape. If the notice takes
  the key its button names (Shift+Left), the script presses it: 3. To check before the script changes.
- **Turn the words Sent and Responding back on** (17), 5: two palette items (2 and 2), a message. One item for the
  turn's words (Sent, Queued and Responding together): 3. For the sitting.
- **Keep reply lines with chosen words silent** (17), 5: Speak replies: all lines (2, setup), the words item (2),
  the words. Example and *palette*: 3.
- **Have every reply line spoken again** (17), 5: the words item (2), Ctrl+A, Delete, Enter. *selected*: 4; a
  "Speak every line" answer in the notice: 3.
- **Turn on the AI tools and have Claude know them** (60), 5: the item (2), Enter, Ctrl+Shift+R, Enter. Let the
  turn-on question's Enter also restart Claude, its words saying so: 3 (group *restart*). Recommended.
- At four: **Turn Claude Code's events off and on again** (30): the switch (2), Ctrl+Shift+R, Enter. *restart*: 3.
- At four: **Quote a line of a reply in your next message** (03): Alt+1, the message, Alt+3, C. Alt+3 landing on
  the reply's first line, not its Output line, removes C: 3. For the sitting.
- At four: **Move through the conversation by headings** (04): a Backspace test before the task. Example; 3.
- At four: **Hide the markers in the text** (04): Markers in the text (2), Ctrl+Home, H. *palette*: 3.
- At four: **Jump between replies, messages, tools, errors and system lines** (04): Ctrl+Home starts the example
  from the top. Example; accept.
- At four: **Interrupt Claude from the menu or the message field** (05): the menu way on purpose (2); Ctrl+I is 1.
  Accept.
- At four: **Send a waiting message at once** (06): two messages first. Example; the user's part is 1.
- At four: **Restart Claude while it runs** (08): Escape refusing first. Example; 2.
- At four: **Decide whether to close the window while Claude works** (08): Alt+F4, Tab, Tab (reading the buttons),
  Escape. Example; 2.
- At four: **Have Claude's replies spoken** (09) and **Show the time on Input and Output lines** (09): the option
  (2), Alt+1, a message. *palette* (Speak replies cycling for the first): 3.
- At four: **Answer a question that follows a reply** (12): Alt+2 and two Ctrl+Tab looking first. Example; 1.
- At four: **Agree to the reminder before Claude Code starts** (15): Tab, Enter (refused), Space, Enter. Example and
  *reminder*: 2.
- At four: **Silence Claude's own notices** (17): Ctrl+P before and after show the effect. Example; 2.
- At four: **Read the explanation of an offered command** (18): Alt+2 and Alt+1 first. Example; 2.
- At four: **Choose Other and type your own answer** (42): an Escape test inside. Example; 2.
- At four: **Tick several answers of the second question** (42): Space, Down Down, Space, Enter. The user's own;
  accept.
- At four: **Choose the effort** (50): Alt+1 back from the check before, Choose the effort (2), Enter. *palette*
  (keys for Choose the effort and Choose the model): 2.
- At four: **Search the rules of the permissions screen** (53): /permissions, End Up Enter, git, Down. Claude
  Code's own screen; accept.

## axFile (15 over four, 8 at four)

- **Go to an operation done in another folder** (06), 9: six steps make the operation, Recent operations (2),
  Enter. Example and *palette* (a key for Recent operations): 2.
- **Show hidden files and names that start with a period** (01), 8: F12, the path, End Enter, Up, two options
  (2 each). Enter on the "Not shown: ..." line could turn on what it names: one key for both. Recommended.
- **Undo a move to a folder** (03), 8; **Undo the delete of several entries** (03), 7; **Undo a rename** (03), 5;
  **Undo a delete** (03), 5; at four **Undo a compress** (03): each makes its operation, then Ctrl+Z. Example: the
  undo is 1. The sitting may retitle them ("Rename a file and take it back") or let the operation move into a
  check before the goal.
- **Make a new folder** (03), 7: Ctrl+Shift+N, Enter refused, a?b refused, Backspace x3, the name. Example; 2.
- **Select several entries** (01), 6: F12 and the path, Down Down, Space, Down Up Down, Space. Example; 3.
- **Move selected files into a new folder** (03), 6: Space, Down x3, Space, Move to folder (2), the name.
  *palette* (a direct key for Move to folder; today Shift+F10 then V, not in the inventory): 5, and 3 for one file.
- **Compress selected files into one zip file** (03), 6: Find, the name, Space, Shift+Down, Compress to zip (2).
  Example (Find) and *palette*: 3.
- **Read the recent operations again** (06), 6: F2 and a name (the operation), Recent operations (2), Down,
  Escape. Example and *palette*: 3.
- **Select entries without an operation** (06), 6: Find, the name, Space, Recent operations (2), Escape. Example and
  *palette*: 3.
- **Clear the selection** (01), 5: Space, Down, Space, Escape, Up. Example: the task is Escape.
- **Delete a folder to the Recycle Bin** (03), 5: moves, Delete, Escape (kept), Delete, Enter. Example; 3.
- **Run a command from the command palette** (05), 5: F1, "oth" with no match, Escape, F1, the name. Example; 2.
- At four: **Find a file by part of its name** (01): F12 and the path first. Example; 2.
- At four: **Go to the next and the previous match** (01): Home, Ctrl+F, the text, F3 (the previous is its own
  goal now). Example (Home); 3.
- At four: **Show only the entries whose names contain a word** (01): Home, Filter (2), the word. *palette* (the
  Ctrl+Shift+F the script's comment names): 3.
- At four: **Extract a zip file here** (03): Find, the name, Extract here (2). *palette*: 3.
- At four: **Select every entry from the marked one to here** (06): Home Down, Space, Down x3, Shift+Space. Accept.
- At four: **Move a file into a folder by typing its name** (07): Move to folder (2), "do", Down Up Enter.
  *palette*: 3.
- At four: **Change one setting in each of two windows** (11): two options (2 each). Example (two windows);
  *palette* would make it 2.

## axJSON (8 over four, 1 at four)

- **Add a key to an action and choose it** (07), 16; **Repair a single-quoted value** (03), 10; **Choose a value
  from the values a member can take** (07), 9; **Choose an item's value through its link to the list's
  configuration** (07), 7: Ctrl+Home and a run of Ctrl+K walk to the key, then the user's own 2 or 3. Example
  (group *walk*): Find (Ctrl+F and the key's name) reaches a key in 2. Recommended: the scripts find the key.
- **Close axJSON when the file has changes** (06), 8: a change typed, Alt+F4, Escape (refused), Alt+F4, Tab, Tab,
  Enter. Example and *letters* (Alt+N Don't save): 2 after the change.
- **Reload a file that another program changed** (01), 6: Down, X, Ctrl+S, Tab, Tab, Enter. *letters* (Alt+R
  reload): 4, of which the change is the example's.
- **Go to the next and the previous error** (02), 6: a file without errors tried first, then Ctrl+Home Ctrl+E,
  Ctrl+End Ctrl+Shift+E. Example; 2 or 3.
- **Save a change to a JSON file** (06), 5: F12, 4, End Left, 0, Ctrl+S. Example: saving is 1. Accept.
- At four: **Turn word wrap off and on in axJSON** (06): F12, 6, Right x3, F11. Example: placing the caret; 1.

## axTerminal (6 over four, 4 at four)

- **Add a shell of your own** (08), 13: Add a shell (2), F7, refusals, the name again, the command, a tour of the
  form, Ctrl+Enter. Example; the user's path is 4, 3 with *palette*.
- **Start a new session in another shell** (03), 9: Ctrl+N, Tab x7 (a tour of the controls), Down Enter. Example;
  the user's path is 4; the notice opening on the shell choice, the folder filled, makes it 2. Recommended.
- **Copy a command line of the terminal into the field** (02), 7: Alt+3, Shift+I, 2, 2, Down Enter, Ctrl+A,
  Delete. Enter on an "Input n" heading could copy that block's command: 3. For the sitting; Recent commands
  (Alt+Down) is the shorter way already.
- **Close the window while a command runs** (04), 6: a command started, Alt+F4, Escape (kept), Alt+F4, Enter.
  Example; 2.
- **Agree to the reminder and start the shell** (07), 6: Tab, Enter refused, Space x3, Enter. Example and
  *reminder*: 2.
- **Set a bookmark and go back to it** (04), 5: Alt+3, Down Down, M, Ctrl+Home, K. Example (reaching the line and
  going away); the user's part is M and K.
- At four: **Restart the shell while a command runs** (03): Escape refusing first. Example; 2.
- At four: **Start the shell again after it ended** (03): a look and a refused command first. Example; 1.
- At four: **Read the keyboard shortcuts** (04) and **Read the user guide** (04): the command (2), H, Escape.
  *help*: 3.

## axGIT (5 over four, 5 at four)

- **Discard the changes to a file** (03), 6: F5, Home, Delete, Escape (kept), Delete, Enter. Example; 2.
- **Discard the changes to the selected files** (03), 6: F5, Ctrl+A, Delete, Enter (kept, Cancel the default),
  Delete, Alt+D. Example; 3.
- **Read the keyboard shortcuts and the user guide** (05), 6: Keyboard shortcuts (2), Escape, User guide (2),
  Escape. Two tasks under one title, and *help*. The sitting may split the title.
- **Agree to the reminder and open the repository** (06), 6: Tab, Enter refused, Space x3, Enter. Example and
  *reminder*: 2.
- **Commit the staged changes** (03), 5: Up, Ctrl+Shift+S, Ctrl+Enter, Enter (empty message refused), the
  message. Example; 2.
- At four: **Stage all and unstage all** (03): Stage all (2), Unstage all (2). Ctrl+A and Ctrl+Shift+S already
  stage everything; recommended: the guide says so, no new key.
- At four: **Read a commit of the log** (04): Alt+2, Enter, Down x6, Ctrl+Tab. Accept.
- At four: **Move by paragraph in a commit's details** (05): a Ctrl+Tab first. Example; 3.
- At four: **Read what was agreed to on this computer** (06): Licence (2), Escape, and the close of the next
  check. Example; 3.
- At four: **Trust a repository** (07): Tab, Tab, Space, Alt+F4. The button is "&Trust this repository": Alt+T in
  one key. Recommended.

## axMCP (3 over four, 4 at four)

- **Add a server of your own** (02), 8: Ctrl+N, F7, the name, Enter refused, the command, Tab Tab Shift+Tab,
  Ctrl+Enter. The command line first, the name taken from the program's name: Ctrl+N, the command, Ctrl+Enter = 3.
  Recommended.
- **Tick a yes-or-no argument and call the tool** (02), 8: Right, Tab, moves, Space, the required text,
  Ctrl+Enter, Escape. Right on a tool landing on its first argument saves the Tab; partly example. For the sitting.
- **Start a server** (02), 5: fake typed, Right, Tab, Space, Enter. *reminder*: 4.
- At four: **Start a server and open its tools** (01) and **Start a server with Enter** (03): Tab, Space, Enter to
  agree. *reminder*: 3.
- At four: **Fill in an argument and call the tool** (01) and **Fill in a prompt and fetch it** (02): Enter, the
  text, Ctrl+Enter, Escape. Accept.

## axPass (3 over four, 2 at four)

Its scripts need KeePassXC.

- **Agree to the reminder and unlock the database** (02), 6: Tab, Enter refused, Space, Enter, the password, the
  close. Example and *reminder*: 3.
- **Write an entry's notes over two lines** (04), 6: Enter, End Enter on Notes, two lines, Tab, Enter (Save).
  *script* if Ctrl+Enter saves from a field of several lines, as in the forms: 5; recommended that it does.
- **Set a generated password** (01), 5: Up, Tab, moves to Password, Alt+G, Enter. A Password command on the entry
  in the list opens its field directly: 3 or 4. For the sitting.
- At four: **Move an entry to another group** (01): finding the entry first. Example; 2.
- At four: **Choose when axPass locks by itself** (03): the lock first, then 5 minutes (2). Example; 2.

## axDown (2 over four, 6 at four)

- **Close a window without saving its changes** (03), 6: Alt+F4, Escape (cancel), Alt+F4, Tab, Tab, Enter. Example
  and *letters*: 2 after the cancel.
- **Turn Claude on by agreeing to the reminder** (11), 6: Shift+F1, Tab, Enter refused, Space, Enter, Escape.
  Example and *reminder*: 3.
- At four: **Start a new document when the text has changes** (03): Escape refusing first. Example; 2.
- At four: **Reload a file that another program changed** (03): Ctrl+S, Tab, Tab, Enter. *letters*: 2.
- At four: **Make several lines list items and undo it in one step** (17): select, List item (2), Ctrl+Z.
  *palette*: 3.
- At four: **Make the selected word a link and type its address** (18) and **Put in a table and type into its
  first cell** (18): a template by name (2). *palette* (keys for the Link and Table templates): 3.
- At four: **Put in a template of your own from the command palette** (18): the selection is the example's; accept.

## axCalc (1 over four, 2 at four)

- **Change an entry of the history and answer it again** (02), 8: Shift+Tab, Up Enter, five Backspaces, 30).
  Example: the editing is the user's own. Accept.
- At four: **Clear the history** (02): Escape keeping it first. Example; 2.
- At four: **Stop keeping the history between runs** (02): an entry made first. Example; 2.

## axStart (1 over four)

- **Change the hotkey** (02), 5: Hotkey (2), A (refused), the new chord, Enter. Example; 4.

## axTemplate (1 over four, 3 at four)

- **Archive several templates at once** (02), 6: Home, Space, Down, Space, Delete, Enter. *script* (Shift+Down): 5;
  archiving without the question, since Ctrl+Z brings it back (as axPass's Recycle Bin): 4. The second for the
  sitting.
- At four: **Write the text of a template in its form** (01): Enter, Tab to Text, the text, Ctrl+Enter. The form
  opening in Text when the template has a name and no text: 3. Recommended.
- At four: **Rename a template from its Title field** (01): Up Enter, Ctrl+A, the title, Ctrl+Enter. *selected*: 3.
- At four: **Read a template before using it** (03): Ctrl+A (the last search) is the script's. Example; 3.

## axProcess (5 at four)

- **Find a process and read its details** (01) and **Read a service's details** (02): Ctrl+F, the name, Enter,
  Escape. Example: type-to-find in the list already makes it 3.
- **Find a process by typing its name** (03): the sort by name (2) is the script's. Example; 1 or 2.
- **Select two processes with Space and Shift+Down** (06): finding the first is the script's. Example; 2.
- **Select two startup programs and remove them together** (06): Alt+3, Space, Shift+Down, Delete. Accept; the
  question after is a safety.

## axAutomate (3 at four)

- **Read a step's details** (01): Up, Tab to the details, Down, Escape; and **Read why a step of the run failed**
  (02): Escape, Right Down, Tab, Down. Enter or F7 on a step reading its details in place: 3 each (group
  *details*). Recommended.
- **Run only the workflows you selected** (05): Space, End, Space, Ctrl+R. Accept.

## axPreview (2 at four)

- **Go to a line by its number** (01): the first two steps are the script's. Example; 2.
- **Read the keyboard shortcuts** (01): Keyboard shortcuts (2), Ctrl+H, Escape. *help*: 3.

## axHelp (1 at four)

- **Quit axHelp** (01): Quit axHelp (2), Tab to Close anyway, Enter. *letters* (Alt+C Close anyway): 3.

## The AI tools (1 at four)

- **Allow an AI tool to use axKit** (mcp 01): Tab to the box, Space, Tab to Allow, Enter. *reminder*: 3.
