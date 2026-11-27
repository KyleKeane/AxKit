# Over the bounds

Written by `tools/graph-check.ps1` from the scripts of `tests/scripts`, the command inventory and
`docs/axKit/workflow-graph.json`; never edit it by hand. The bounds are SPEC.md G7 and `docs/axKit/workflow-graph.md`
WG-3: a goal takes four actions at most and ideally three, and every waypoint of a window is two actions or fewer
from any of its places. A goal's actions are counted as WG-3.3 says: the app's own key is free, the steps that
only prepare or look are not counted, and neither is the example's text a goal types before its first command.
The counts are held in `docs/axKit/goal-counts.json`: a goal may get shorter, never longer (WG-3.4).

880 goals counted: 87 over four actions, 77 at four, 716 within three.
The map covers 7 apps and 58 nodes: 0 nodes with a waypoint more than two actions away (0 in all, a node and a waypoint each).

## axAutomate

### At four actions, over the ideal three

- **Browse a folder of workflows and read their steps: Read a step's details**, 4 actions (`tests/scripts/axAutomate/01-start.txt`): Up; Tab; Down; Escape.
- **Run a workflow and hear how it went: Read why a step of the run failed**, 4 actions (`tests/scripts/axAutomate/02-run.txt`): Escape; Right, Down (one move); Tab; Down.
- **Stop the run of a folder and see which checks need NVDA: Run only the workflows you selected**, 4 actions (`tests/scripts/axAutomate/05-stop-a-folder-run.txt`): Space; End; Space; Ctrl+R (Run).

## axCalc

### Over four actions

- **Use variables, earlier answers and the history in axCalc: Change an entry of the history and answer it again**, 8 actions (`tests/scripts/axCalc/02-answers-variables-history.txt`): Shift+Tab; Up, Enter; Backspace; Backspace; Backspace; Backspace; Backspace; Type "30)" and Enter.

### At four actions, over the ideal three

- **Use variables, earlier answers and the history in axCalc: Clear the history**, 4 actions (`tests/scripts/axCalc/02-answers-variables-history.txt`): Ctrl+Shift+Delete (Clear history); Escape (Back: in the field, nothing to go back to); Ctrl+Shift+Delete (Clear history); Enter.
- **Use variables, earlier answers and the history in axCalc: Stop keeping the history between runs**, 4 actions (`tests/scripts/axCalc/02-answers-variables-history.txt`): Tab; Type "1+1" and Enter; Keep history between runs (F1, its name and Enter: 2).

## axClaude

### Over four actions

- **Use the axKit menu from a conversation: Close every window at once**, 12 actions (`tests/scripts/axClaude/13-axkit-menu.txt`): Ctrl+End; Type "more"; Alt+1 (Message field); Type "Use your Bash tool to run: pin..." and Enter; Close all windows (F1, its name and Enter: 2); Close the window (Alt+F4); Tab; Tab; Enter; Close the window (Alt+F4); Enter.
- **Move through a reply by headings and quick keys: Quote two lines in a message and send it**, 11 actions (`tests/scripts/axClaude/04-headings.txt`): Alt+1 (Message field); Type "Reply with the single word ok."; Alt+2 (Conversation); Ctrl+Home; I; Enter; Alt+2 (Conversation); Ctrl+Home; C; Enter; Ctrl+Home, Down, Down (one move), Enter.
- **Send Claude a message and hear the reply: Move through a message by paragraph**, 10 actions (`tests/scripts/axClaude/02-conversation.txt`): Type "one"; Shift+Enter; Shift+Enter; Type "two"; Shift+Enter; Type "three"; Shift+Enter; Shift+Enter; Type "four"; Ctrl+Home, Ctrl+Down, Ctrl+Down (one move).
- **Answer Claude's questions and screens: Answer a question with several answers**, 8 actions (`tests/scripts/axClaude/12-questions.txt`): Alt+1 (Message field); Type "Use the AskUserQuestion tool o..." and Enter; Home, Down (one move), Enter; Home; Space; Down, Down (one move); Space; Enter.
- **Answer Claude's questions and screens: Review answers before they go to Claude**, 8 actions (`tests/scripts/axClaude/12-questions.txt`): Review answers before they go to Claude (F1, its name and Enter: 2); Type "Use the AskUserQuestion tool o..." and Enter; Home, Down (one move), Enter; Home; Space; Enter; Home, Enter.
- **Move through a reply by headings and quick keys: Go back to where you were before a jump**, 7 actions (`tests/scripts/axClaude/04-headings.txt`): Ctrl+Home; H; H; H; P; P; Backspace.
- **Report a bug or an idea from a conversation: Send the report**, 7 actions (`tests/scripts/axClaude/16-report.txt`): Shift+Tab; Shift+Tab; Shift+Tab; Shift+Tab; Shift+Tab; Ctrl+Enter (Send the message); Escape.
- **Answer Claude's questions and screens: Have questions come without interrupting speech**, 6 actions (`tests/scripts/axClaude/12-questions.txt`): Go to them at once, interrupting speech (F1, its name and Enter: 2); Alt+1 (Message field); Type "Write two sentences about gard..." and Enter; Alt+2 (Conversation); Home, Enter.
- **Read the conversation while Claude is writing: Select lines of a reply and clear the selection**, 5 actions (`tests/scripts/axClaude/03-reading.txt`): Alt+2 (Conversation); Ctrl+Home; O; Down, Down, Shift+Down, Shift+Down (one move); Escape.
- **Move through a reply by headings and quick keys: Find the next match from the message field**, 5 actions (`tests/scripts/axClaude/04-headings.txt`): Alt+1 (Message field); Ctrl+F (Find); Type "single word" and Enter; Alt+1 (Message field); F3 (Find next).
- **Change Claude's project folder: Start a new session with arguments of your own**, 5 actions (`tests/scripts/axClaude/07-folder.txt`): Ctrl+N (New session); Down, Down (one move); Tab; Tab; Tab.
- **Choose axClaude's speech, sounds and text: Hear each tool call as it starts**, 5 actions (`tests/scripts/axClaude/09-options.txt`): Speak replies: none (F1, its name and Enter: 2); Speak each tool call (F1, its name and Enter: 2); Type "Use the Bash tool to run: echo..." and Enter.
- **Install Claude Code when it is missing: Hear the ways to install Claude Code**, 5 actions (`tests/scripts/axClaude/10-missing.txt`): Tab; Tab; Tab; Tab; Tab.
- **Read a side answer and other information screens: Browse earlier side answers**, 5 actions (`tests/scripts/axClaude/14-side-answer.txt`): Type "/btw what is three plus three?..." and Enter; Tab; Tab; Enter; Escape.
- **Choose which words axClaude says by itself: Turn the words Sent and Responding back on**, 5 actions (`tests/scripts/axClaude/17-what-is-spoken.txt`): "Sent" and "Queued" after a message (F1, its name and Enter: 2); "Responding" when a reply starts (F1, its name and Enter: 2); Type "Reply with the single word two..." and Enter.
- **Choose which words axClaude says by itself: Keep reply lines with chosen words silent**, 5 actions (`tests/scripts/axClaude/17-what-is-spoken.txt`): Speak replies: all lines (F1, its name and Enter: 2); Words that keep a reply line silent (F1, its name and Enter: 2); Type "pelican; walrus" and Enter.
- **Choose which words axClaude says by itself: Have every reply line spoken again**, 5 actions (`tests/scripts/axClaude/17-what-is-spoken.txt`): Words that keep a reply line silent (F1, its name and Enter: 2); Ctrl+A; Delete; Enter.
- **Give Claude axKit's AI tools from the start: Turn on the AI tools and have Claude know them**, 5 actions (`tests/scripts/axClaude/60-ai-tools-at-start.txt`): Let AI tools read and open axKit (F1, its name and Enter: 2); Enter; Ctrl+Shift+R (Force Claude to restart); Enter.

### At four actions, over the ideal three

- **Read the conversation while Claude is writing: Quote a line of a reply in your next message**, 4 actions (`tests/scripts/axClaude/03-reading.txt`): Alt+1 (Message field); Type "Write one paragraph of 120 wor..." and Enter; Alt+3 (Latest response); C.
- **Move through a reply by headings and quick keys: Move through the conversation by headings**, 4 actions (`tests/scripts/axClaude/04-headings.txt`): Alt+2 (Conversation); Ctrl+Home; Backspace; H.
- **Move through a reply by headings and quick keys: Hide the markers in the text**, 4 actions (`tests/scripts/axClaude/04-headings.txt`): Markers in the text (F1, its name and Enter: 2); Ctrl+Home; H.
- **Move through a reply by headings and quick keys: Jump between replies, messages, tools, errors and system lines**, 4 actions (`tests/scripts/axClaude/04-headings.txt`): Ctrl+Home; O; R; Shift+R.
- **Change the permission mode and interrupt Claude: Interrupt Claude from the menu or the message field**, 4 actions (`tests/scripts/axClaude/05-modes.txt`): Alt+1 (Message field); Type "Write a story of 1500 words ab..." and Enter; Ctrl+C (F1, its name and Enter: 2).
- **Send messages while Claude is still working: Send a waiting message at once**, 4 actions (`tests/scripts/axClaude/06-queue.txt`): Alt+1 (Message field); Type "Use your Bash tool to run: pin..." and Enter; Type "Reply with the single word fou..." and Enter; Ctrl+Shift+Enter (Send queued messages now).
- **Restart, stop and close a conversation: Restart Claude while it runs**, 4 actions (`tests/scripts/axClaude/08-lifecycle.txt`): Ctrl+Shift+R (Force Claude to restart); Escape; Ctrl+Shift+R (Force Claude to restart); Enter.
- **Restart, stop and close a conversation: Decide whether to close the window while Claude works**, 4 actions (`tests/scripts/axClaude/08-lifecycle.txt`): Alt+F4; Tab; Tab; Escape. Not counted, the example: Type "Use your Bash tool to run: pin..."; Enter.
- **Choose axClaude's speech, sounds and text: Have Claude's replies spoken**, 4 actions (`tests/scripts/axClaude/09-options.txt`): Speak replies: all lines (F1, its name and Enter: 2); Alt+1 (Message field); Type "Markdown only: ## Alpha, a fiv..." and Enter.
- **Choose axClaude's speech, sounds and text: Show the time on Input and Output lines**, 4 actions (`tests/scripts/axClaude/09-options.txt`): Time stamps on Input and Output lines (F1, its name and Enter: 2); Alt+1 (Message field); Type "Reply with the single word sta..." and Enter.
- **Answer Claude's questions and screens: Answer a question that follows a reply**, 4 actions (`tests/scripts/axClaude/12-questions.txt`): Alt+2 (Conversation); Ctrl+Tab (Switch between the view and the field); Ctrl+Tab (Switch between the view and the field); Home, Enter. Not counted, the example: Type "Write two sentences about gard..."; Enter.
- **Agree to the reminder before Claude Code first starts: Agree to the reminder before Claude Code starts**, 4 actions (`tests/scripts/axClaude/15-consent.txt`): Tab; Enter; Space; Enter.
- **Choose which words axClaude says by itself: Silence Claude's own notices**, 4 actions (`tests/scripts/axClaude/17-what-is-spoken.txt`): Ctrl+P (Next permission mode); Claude's own notices, such as "accept edits on" (F1, its name and Enter: 2); Ctrl+P (Next permission mode).
- **Run a command Claude offers you: Read the explanation of an offered command**, 4 actions (`tests/scripts/axClaude/18-offered-command.txt`): Alt+2 (Conversation); Alt+1 (Message field); Enter; Enter.
- **See Claude Code's events arrive, without Claude Code: Turn Claude Code's events off and on again**, 4 actions (`tests/scripts/axClaude/30-hooks-stand-in.txt`): Events from Claude Code's hooks (F1, its name and Enter: 2); Force Claude to restart (Ctrl+Shift+R); Enter.
- **Answer Claude's own questions over Claude Code's stream: Choose Other and type your own answer**, 4 actions (`tests/scripts/axClaude/42-stream-questions.txt`): End, Enter; Escape; End, Enter; Type "teal" and Enter.
- **Answer Claude's own questions over Claude Code's stream: Tick several answers of the second question**, 4 actions (`tests/scripts/axClaude/42-stream-questions.txt`): Space; Down, Down (one move); Space; Enter.
- **Choose the model and the effort over Claude Code's stream: Choose the effort**, 4 actions (`tests/scripts/axClaude/50-stream-choices.txt`): Message field (Alt+1); Choose the effort (F1, its name and Enter: 2); Enter.
- **Search the rules of Claude Code's permissions screen: Search the rules of the permissions screen**, 4 actions (`tests/scripts/axClaude/53-permissions-search.txt`): Type "/permissions" and Enter; End, Up (one move), Enter; Type "git" and Enter; Down.

## axDown

### Over four actions

- **Open, save and switch files in axDown: Close a window without saving its changes**, 6 actions (`tests/scripts/axDown/03-files.txt`): Close the window (Alt+F4); Escape; Close the window (Alt+F4); Tab; Tab; Enter. Not counted, the example: Type "More.".
- **Ask Claude about the window you are in: Turn Claude on by agreeing to the reminder**, 6 actions (`tests/scripts/axDown/11-ask-claude.txt`): Ask about this window (Shift+F1); Tab; Enter; Space; Enter; Escape.

### At four actions, over the ideal three

- **Open, save and switch files in axDown: Start a new document when the text has changes**, 4 actions (`tests/scripts/axDown/03-files.txt`): New (Ctrl+N); Escape; New (Ctrl+N); Enter. Not counted, the example: Type "Added.".
- **Open, save and switch files in axDown: Reload a file that another program changed**, 4 actions (`tests/scripts/axDown/03-files.txt`): Save (Ctrl+S); Tab; Tab; Enter.
- **Give a line, a word and several lines their Markdown format, and take it away again: Make several lines list items and undo it in one step**, 4 actions (`tests/scripts/axDown/17-format.txt`): Ctrl+Down, Shift+Down, Shift+End (one move); List item, Actions, Format (F1, its name and Enter: 2); Ctrl+Z (Undo).
- **Put a template in at the caret: a link around the selected words, a table, and a template of your own: Make the selected word a link and type its address**, 4 actions (`tests/scripts/axDown/18-templates.txt`): Shift+End; Link, Actions, Insert template (F1, its name and Enter: 2); Type "https://example.org".
- **Put a template in at the caret: a link around the selected words, a table, and a template of your own: Put in a table and type into its first cell**, 4 actions (`tests/scripts/axDown/18-templates.txt`): Ctrl+End; Table, Actions, Insert template (F1, its name and Enter: 2); Type "Name".
- **Put a template in at the caret: a link around the selected words, a table, and a template of your own: Put in a template of your own from the command palette, with the selected words carried into it**, 4 actions (`tests/scripts/axDown/18-templates.txt`): Ctrl+Home, Down, Down, Shift+End (one move); Command palette (F1); Type "meeting" and Enter; Type "of Monday".

## axFile

### Over four actions

- **Keep favorite folders and go back to recent operations: Go to an operation done in another folder**, 9 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Find (Ctrl+F); Type "subfolder" and Enter; Enter; New folder (Ctrl+Shift+N); Type "inner" and Enter; Backspace; Recent operations (F1, its name and Enter: 2); Enter.
- **Browse folders and files: Show hidden files and names that start with a period**, 8 actions (`tests/scripts/axFile/01-browse.txt`): Go to folder (F12); Type "{temp}\hid" and Enter; End, Enter; Up; Show files and folders starting with a period (F1, its name and Enter: 2); Show hidden files and folders (F1, its name and Enter: 2).
- **Copy, move, rename and delete files: Undo a move to a folder**, 8 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "move-two" and Enter; Space; Shift+Down; Move to folder, Context (F1, its name and Enter: 2); Type "bundle" and Enter; Undo (Ctrl+Z).
- **Copy, move, rename and delete files: Make a new folder**, 7 actions (`tests/scripts/axFile/03-operations.txt`): New folder (Ctrl+Shift+N); Enter; Type "a?b" and Enter; Backspace; Backspace; Backspace; Type "notes" and Enter.
- **Copy, move, rename and delete files: Undo the delete of several entries**, 7 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "delete-two" and Enter; Space; Shift+Down; Delete; Enter; Undo (Ctrl+Z).
- **Browse folders and files: Select several entries**, 6 actions (`tests/scripts/axFile/01-browse.txt`): Go to folder (F12); Type "{temp}\sel" and Enter; Down, Down (one move); Space; Down, Up, Down (one move); Space.
- **Copy, move, rename and delete files: Move selected files into a new folder**, 6 actions (`tests/scripts/axFile/03-operations.txt`): Space; Down, Down, Down (one move); Space; Move to folder, Context (F1, its name and Enter: 2); Type "bundle" and Enter.
- **Copy, move, rename and delete files: Compress selected files into one zip file**, 6 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "move-into" and Enter; Space; Shift+Down; Compress to zip, Context (F1, its name and Enter: 2).
- **Keep favorite folders and go back to recent operations: Read the recent operations again**, 6 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Rename (F2); Type "notes" and Enter; Recent operations (F1, its name and Enter: 2); Down; Escape.
- **Keep favorite folders and go back to recent operations: Select entries without an operation**, 6 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Find (Ctrl+F); Type "start-here" and Enter; Space; Recent operations (F1, its name and Enter: 2); Escape.
- **Browse folders and files: Clear the selection**, 5 actions (`tests/scripts/axFile/01-browse.txt`): Space; Down; Space; Escape; Up.
- **Copy, move, rename and delete files: Delete a folder to the Recycle Bin**, 5 actions (`tests/scripts/axFile/03-operations.txt`): Home, Down, Down (one move); Delete; Escape; Delete; Enter.
- **Copy, move, rename and delete files: Undo a rename**, 5 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "rename-then" and Enter; Rename (F2); Type "renamed" and Enter; Undo (Ctrl+Z).
- **Copy, move, rename and delete files: Undo a delete**, 5 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "delete-then" and Enter; Delete; Enter; Undo (Ctrl+Z).
- **Use axFile's text, notices and axKit menu: Run a command from the command palette**, 5 actions (`tests/scripts/axFile/05-text-notices-axkit-menu.txt`): Command palette (F1); Type "oth"; Escape; Command palette (F1); Type "refresh" and Enter.

### At four actions, over the ideal three

- **Browse folders and files: Find a file by part of its name**, 4 actions (`tests/scripts/axFile/01-browse.txt`): Go to folder (F12); Type "{temp}\fs" and Enter; Find (Ctrl+F); Type "find" and Enter.
- **Browse folders and files: Go to the next and the previous match**, 4 actions (`tests/scripts/axFile/01-browse.txt`): Home; Find (Ctrl+F); Type "notes" and Enter; Find next (F3).
- **Browse folders and files: Show only the entries whose names contain a word**, 4 actions (`tests/scripts/axFile/01-browse.txt`): Home; Filter (F1, its name and Enter: 2); Type "chap" and Enter.
- **Copy, move, rename and delete files: Extract a zip file here**, 4 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "docs.zip" and Enter; Extract here, Context (F1, its name and Enter: 2).
- **Copy, move, rename and delete files: Undo a compress**, 4 actions (`tests/scripts/axFile/03-operations.txt`): Home; Compress to zip, Context (F1, its name and Enter: 2); Undo (Ctrl+Z).
- **Keep favorite folders and go back to recent operations: Select every entry from the marked one to here**, 4 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Home, Down (one move); Space; Down, Down, Down (one move); Shift+Space.
- **Move files to a folder you type: Move a file into a folder by typing its name**, 4 actions (`tests/scripts/axFile/07-move-to-folder.txt`): Move to folder, Context (F1, its name and Enter: 2); Type "do"; Down, Up (one move), Enter.
- **Change settings in two windows of one app: Change one setting in each of two windows**, 4 actions (`tests/scripts/axFile/11-two-windows-save.txt`): Show hidden files and folders (F1, its name and Enter: 2); Show files and folders starting with a period (F1, its name and Enter: 2).

## axGIT

### Over four actions

- **Review and commit changes: Discard the changes to a file**, 6 actions (`tests/scripts/axGIT/03-changes.txt`): F5 (Refresh); Home; Delete; Escape; Delete; Enter.
- **Review and commit changes: Discard the changes to the selected files**, 6 actions (`tests/scripts/axGIT/03-changes.txt`): F5 (Refresh); Ctrl+A; Delete; Enter; Delete; Alt+D.
- **Use axGIT's text, notices and axKit menu: Read the keyboard shortcuts and the user guide**, 6 actions (`tests/scripts/axGIT/05-text-notices.txt`): Keyboard shortcuts (F1, its name and Enter: 2); Escape; User guide (F1, its name and Enter: 2); Escape.
- **Agree to the reminder before the first repository opens: Agree to the reminder and open the repository**, 6 actions (`tests/scripts/axGIT/06-consent.txt`): Tab; Enter; Space; Space; Space; Enter.
- **Review and commit changes: Commit the staged changes**, 5 actions (`tests/scripts/axGIT/03-changes.txt`): Up; Ctrl+Shift+S; Ctrl+Enter (Commit); Enter; Type "Test commit" and Enter.

### At four actions, over the ideal three

- **Review and commit changes: Stage all and unstage all**, 4 actions (`tests/scripts/axGIT/03-changes.txt`): Stage all (F1, its name and Enter: 2); Unstage all (F1, its name and Enter: 2).
- **Read the history and switch branches: Read a commit of the log**, 4 actions (`tests/scripts/axGIT/04-log-branches.txt`): Alt+2 (Log); Enter; Down, Down, Down, Down, Down, Down (one move); Ctrl+Tab (Switch between the list and the text).
- **Use axGIT's text, notices and axKit menu: Move by paragraph in a commit's details**, 4 actions (`tests/scripts/axGIT/05-text-notices.txt`): Ctrl+Tab (Switch between the list and the text); Alt+2 (Log); Enter; Ctrl+Down, Ctrl+Down (one move).
- **Agree to the reminder before the first repository opens: Read what was agreed to on this computer**, 4 actions (`tests/scripts/axGIT/06-consent.txt`): Licence (F1, its name and Enter: 2); Escape; Close the window (Alt+F4).
- **Open a repository that runs programs of its own: Trust a repository**, 4 actions (`tests/scripts/axGIT/07-own-programs.txt`): Tab; Tab; Space; Close the window (Alt+F4).

## axHelp

### At four actions, over the ideal three

- **Ask Claude for help from anywhere, with a picture of the screen: Quit axHelp**, 4 actions (`tests/scripts/axHelp/01-start.txt`): Quit axHelp (F1, its name and Enter: 2); Tab; Enter.

## axJSON

### Over four actions

- **Write a list's configuration by choosing: Add a key to an action and choose it**, 16 actions (`tests/scripts/axJSON/07-options.txt`): Ctrl+Home; Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Alt+Left (Add a member); Enter.
- **Format a JSON file: Repair a single-quoted value**, 10 actions (`tests/scripts/axJSON/03-format.txt`): Ctrl+Home; Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); End; Type ", "q": 'x'"; Ctrl+R (Format).
- **Write a list's configuration by choosing: Choose a value from the values a member can take**, 9 actions (`tests/scripts/axJSON/07-options.txt`): Ctrl+Home; Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Choose a value (Alt+Right); Down, Enter.
- **Save, find, go to a line and wrap in axJSON: Close axJSON when the file has changes**, 8 actions (`tests/scripts/axJSON/06-files-find-wrap.txt`): End; Type ","; Close the window (Alt+F4); Escape; Close the window (Alt+F4); Tab; Tab; Enter.
- **Write a list's configuration by choosing: Choose an item's value through its link to the list's configuration**, 7 actions (`tests/scripts/axJSON/07-options.txt`): Ctrl+Home; Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Alt+Right (Choose a value); Up, Enter.
- **Open a JSON file in axJSON: Reload a file that another program changed**, 6 actions (`tests/scripts/axJSON/01-start.txt`): Down; Type "X"; Ctrl+S (Save); Tab; Tab; Enter.
- **Move through a JSON file by its structure: Go to the next and the previous error**, 6 actions (`tests/scripts/axJSON/02-navigation.txt`): Ctrl+Home; Ctrl+E (Error); Ctrl+Home; Ctrl+E (Error); Ctrl+End; Ctrl+Shift+E (Error).
- **Save, find, go to a line and wrap in axJSON: Save a change to a JSON file**, 5 actions (`tests/scripts/axJSON/06-files-find-wrap.txt`): Go to line (F12); Type "4" and Enter; End, Left (one move); Type "0"; Save (Ctrl+S).

### At four actions, over the ideal three

- **Save, find, go to a line and wrap in axJSON: Turn word wrap off and on in axJSON**, 4 actions (`tests/scripts/axJSON/06-files-find-wrap.txt`): Go to line (F12); Type "6" and Enter; Right, Right, Right (one move); Word wrap (F11).

## axMCP

### Over four actions

- **Add, rename and remove a server of your own in axMCP: Add a server of your own**, 8 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): Ctrl+N; F7 (Read in full); Type "probe" and Enter; Type "cmd /c echo"; Tab; Tab; Shift+Tab; Ctrl+Enter.
- **Add, rename and remove a server of your own in axMCP: Tick a yes-or-no argument and call the tool**, 8 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): Right; Tab; Down, Down (one move); Space; Up, Enter; Type "hello" and Enter; Ctrl+Enter; Escape.
- **Add, rename and remove a server of your own in axMCP: Start a server**, 5 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): Type "fake"; Right; Tab; Space; Enter.

### At four actions, over the ideal three

- **Start an MCP server and use its tools: Start a server and open its tools**, 4 actions (`tests/scripts/axMCP/01-start.txt`): Right; Tab; Space; Enter.
- **Start an MCP server and use its tools: Fill in an argument and call the tool**, 4 actions (`tests/scripts/axMCP/01-start.txt`): Enter; Type "hello" and Enter; Ctrl+Enter; Escape.
- **Add, rename and remove a server of your own in axMCP: Fill in a prompt and fetch it**, 4 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): Enter; Type "Ada" and Enter; Ctrl+Enter; Escape.
- **Fill in a number and a choice, and start a server again: Start a server with Enter**, 4 actions (`tests/scripts/axMCP/03-numbers-choices-restart-agreement.txt`): Enter; Tab; Space; Enter.

## axPass

### Over four actions

- **Agree to the reminder before the first unlock: Agree to the reminder and unlock the database**, 6 actions (`tests/scripts/axPass/02-consent.txt`): Tab; Enter; Space; Enter; Type "test-master-1" and Enter; Close the window (Alt+F4).
- **Change an entry's fields, keep a backup and delete for good: Write an entry's notes over two lines**, 6 actions (`tests/scripts/axPass/04-fields-backups-recycle-bin.txt`): Enter; End, Enter; Type "First line of the notes" and Enter; Type "Second line"; Tab; Enter.
- **Unlock a password database and change an entry: Set a generated password**, 5 actions (`tests/scripts/axPass/01-unlock-read-change.txt`): Up; Tab; Home, Down, Down (one move), Enter; Alt+G; Enter.

### At four actions, over the ideal three

- **Unlock a password database and change an entry: Move an entry to another group**, 4 actions (`tests/scripts/axPass/01-unlock-read-change.txt`): Home, Right (one move); Type "g"; Ctrl+M; Down, Enter.
- **Read an entry in full and come back to the same database: Choose when axPass locks by itself**, 4 actions (`tests/scripts/axPass/03-read-and-reopen.txt`): Ctrl+L (Lock); Escape; 5 minutes (F1, its name and Enter: 2).

## axPreview

### At four actions, over the ideal three

- **Preview a text file: Go to a line by its number**, 4 actions (`tests/scripts/axPreview/01-start-and-the-text.txt`): Ctrl+Home, Down (one move); L; F12 (Go to line); Type "5" and Enter.
- **Preview a text file: Read the keyboard shortcuts**, 4 actions (`tests/scripts/axPreview/01-start-and-the-text.txt`): Keyboard shortcuts (F1, its name and Enter: 2); Ctrl+H (Heading); Escape.

## axProcess

### At four actions, over the ideal three

- **Find and end a process: Find a process and read its details**, 4 actions (`tests/scripts/axProcess/01-start.txt`): Ctrl+F (Find in the list); Type "axkitscript01" and Enter; Enter; Escape (Deselect all).
- **Manage services and startup programs: Read a service's details**, 4 actions (`tests/scripts/axProcess/02-services-startup.txt`): Ctrl+F (Find in the list); Type "(EventLog)" and Enter; Enter; Escape (Deselect all).
- **Find a process by typing its name and read a startup program's details: Find a process by typing its name**, 4 actions (`tests/scripts/axProcess/03-type-to-find-own-window-startup.txt`): Name (F1, its name and Enter: 2); Home; Type "svc".
- **Select several processes or startup programs and end or remove them together: Select two processes with Space and Shift+Down**, 4 actions (`tests/scripts/axProcess/06-select-several.txt`): Find in the list (Ctrl+F); Type "axkitscript06" and Enter; Space; Shift+Down.
- **Select several processes or startup programs and end or remove them together: Select two startup programs and remove them together**, 4 actions (`tests/scripts/axProcess/06-select-several.txt`): Alt+3 (Startup); Space; Shift+Down; Delete.

## axStart

### Over four actions

- **Search programs as you type and change axStart's hotkey: Change the hotkey**, 5 actions (`tests/scripts/axStart/02-typing-and-the-hotkey.txt`): Hotkey (F1, its name and Enter: 2); A; Ctrl+Alt+Shift+F9; Enter.

## axTask

### Over four actions

- **Change an item in its form: Change several fields of an item and save them as one step**, 17 actions (`tests/scripts/axTask/18-item-form.txt`): Enter; Tab; Space; Tab; Ctrl+A (Select all); Type "2099-11-02"; Tab; Down, Down (one move); Tab; Ctrl+A (Select all); Type "Hall"; Tab; Type "First line" and Enter; Type "Second line"; Tab; Type "Washer"; Ctrl+Enter (Done or not done).
- **Change an item in its form: Rename an item in its form**, 12 actions (`tests/scripts/axTask/18-item-form.txt`): Enter; Tab; Tab; Tab; Tab; Tab; Tab; Type "Washer"; Ctrl+Enter (Done or not done); Down, Enter; Ctrl+A (Select all); Type "Washer 15 mm" and Enter.
- **Set or clear fields of several items at once: Set fields of the selected items**, 10 actions (`tests/scripts/axTask/28-set-and-clear-fields.txt`): Space; Down; Space; Set fields (F1, its name and Enter: 2); Tab; Down; Tab; Type "Garage"; Ctrl+Enter (Done or not done).
- **Add project folders and search every list: Search every list**, 9 actions (`tests/scripts/axTask/05-projects.txt`): Find in every list (Ctrl+Shift+F); Type "zebra" and Enter; Find in every list (Ctrl+Shift+F); Ctrl+A (Select all); Type "TAP" and Enter; Down; Tab; Shift+Tab; Escape.
- **Run the actions of a list: Put a command into axTerminal's command field**, 8 actions (`tests/scripts/axTask/19-list-actions.txt`): Look in the terminal (F1, its name and Enter: 2); Enter; Look in the terminal (F1, its name and Enter: 2); Tab; Tab; Enter.
- **Add project folders and search every list: Add a project folder by its path**, 7 actions (`tests/scripts/axTask/05-projects.txt`): Add a project folder (F1, its name and Enter: 2); Type "{temp}\nowhere" and Enter; Ctrl+A (Select all); Type "{temp}\tasks" and Enter; Ctrl+A (Select all); Type "{temp}\more" and Enter.
- **Run the actions of a list: Fill in an action's form**, 7 actions (`tests/scripts/axTask/19-list-actions.txt`): Find what I type (F1, its name and Enter: 2); Enter; Type "the tap" and Enter; Tab; Tab; Enter.
- **Select several items and act on all of them: Mark several tasks done at once**, 7 actions (`tests/scripts/axTask/20-selection.txt`): Home; Space; Down; Space; End; Space; Ctrl+Enter (Done or not done).
- **Group lists into sections: Put a list in a section from its details**, 6 actions (`tests/scripts/axTask/03-sections.txt`): Previous section (Ctrl+Up); Down; Tab; Enter; Type "Jobs" and Enter; Escape.
- **Set or clear fields of several items at once: Clear fields of the selected items**, 6 actions (`tests/scripts/axTask/28-set-and-clear-fields.txt`): Clear fields (F1, its name and Enter: 2); Ctrl+Enter (Done or not done); End; Space; Ctrl+Enter (Done or not done).
- **Move through tasks and change an item's fields: Open an item's fields**, 5 actions (`tests/scripts/axTask/01-navigate-and-fields.txt`): Enter; Enter; Escape; Tab; Down, Down, Down (one move).
- **Move through tasks and change an item's fields: Add an item**, 5 actions (`tests/scripts/axTask/01-navigate-and-fields.txt`): Left; Type "s"; Right; New item (Ctrl+N); Type "Bread" and Enter.
- **Make lists, link items and use smart lists: Write notes of several lines**, 5 actions (`tests/scripts/axTask/02-lists-links-smart.txt`): Up, Enter; Type "First line" and Enter; Type "Second line"; Tab; Enter.
- **Make lists, link items and use smart lists: Link one item to another**, 5 actions (`tests/scripts/axTask/02-lists-links-smart.txt`): Escape; Right; Tab; End, Enter; Type "Washer" and Enter.
- **Rename a list, and change the fields of its items: Take a field out of a list and keep its values**, 5 actions (`tests/scripts/axTask/12-rename-list-and-fields.txt`): Refresh (F5); Right; Tab; Down, Down (one move), Enter; Escape.
- **Convert a list written in axTask's earlier format: Convert a list of the earlier format**, 5 actions (`tests/scripts/axTask/16-convert-earlier-format.txt`): Type "ho"; Right; Escape; Enter; Enter.
- **Give a list its configuration: Give a list its configuration**, 5 actions (`tests/scripts/axTask/17-list-without-configuration.txt`): Left; Tab; End; Shift+Tab; Edit the list's configuration (Ctrl+J).
- **Give a list its configuration: Add an item to a list that has no configuration yet**, 5 actions (`tests/scripts/axTask/17-list-without-configuration.txt`): Left; Type "em"; Right; New item (Ctrl+N); Type "First thing" and Enter.
- **Change an item in its form: Open the form on a field from the detail pane**, 5 actions (`tests/scripts/axTask/18-item-form.txt`): Up; Tab; Down, Down, Down, Down (one move), Enter; Ctrl+A (Select all); Type "Bathroom" and Enter.
- **Run the actions of a list: Run a program on an item, and hear how it ends**, 5 actions (`tests/scripts/axTask/19-list-actions.txt`): Action key Shift+4 (Ctrl+Shift+4); Tab; Tab; Tab; Enter.
- **Run the actions of a list: Stop a running action**, 5 actions (`tests/scripts/axTask/19-list-actions.txt`): Wait (F1, its name and Enter: 2); Tab; Tab; Enter.
- **Select several items and act on all of them: Archive several items at once**, 5 actions (`tests/scripts/axTask/20-selection.txt`): Home; Space; Down; Space; Delete.
- **Select several items and act on all of them: Move the selected items to a list chosen in a form**, 5 actions (`tests/scripts/axTask/20-selection.txt`): Space; Down; Space; Ctrl+M; Enter.
- **Keep a history of a list's changes and read an item's: Read what changed in an item**, 5 actions (`tests/scripts/axTask/29-history.txt`): Ctrl+Enter (Done or not done); Undo (Ctrl+Z); History (F1, its name and Enter: 2); Escape.

### At four actions, over the ideal three

- **Move through tasks and change an item's fields: Move among the lists by their headings**, 4 actions (`tests/scripts/axTask/01-navigate-and-fields.txt`): Previous section (Ctrl+Up); Next section (Ctrl+Down); Down; Next heading level 1 (Ctrl+1).
- **Move through tasks and change an item's fields: Set a due date by typing a weekday**, 4 actions (`tests/scripts/axTask/01-navigate-and-fields.txt`): Tab; Enter; Ctrl+A (Select all); Type "friday" and Enter.
- **Group lists into sections: Move among the sections by their headings**, 4 actions (`tests/scripts/axTask/03-sections.txt`): Previous section (Ctrl+Up); Next section (Ctrl+Down); Next section (Ctrl+Down); Next heading level 1 (Ctrl+1).
- **Group lists into sections: Move a list back to Your lists**, 4 actions (`tests/scripts/axTask/03-sections.txt`): Previous section (Ctrl+Up); Type "h"; Move to section (Ctrl+M); Home, Enter.
- **Add project folders and search every list: Open a project and mark a task done in it**, 4 actions (`tests/scripts/axTask/05-projects.txt`): Right; Next section (Ctrl+Down); Down, Home, Down, Right (one move); Ctrl+Enter (Done or not done).
- **Rename a list, and change the fields of its items: Read which fields a list's items have**, 4 actions (`tests/scripts/axTask/12-rename-list-and-fields.txt`): Tab; End, Enter; Escape; Shift+Tab.
- **Rename a list, and change the fields of its items: Put a field back and find its values again**, 4 actions (`tests/scripts/axTask/12-rename-list-and-fields.txt`): Escape; Refresh (F5); Tab; Home, Down, Down, End (one move).
- **Make a smart list of your own: Make a smart list and see its file open in axJSON**, 4 actions (`tests/scripts/axTask/14-new-smart-list.txt`): New smart list (F1, its name and Enter: 2); Type "Soon" and Enter; Close the window (Alt+F4).
- **Approve or decline a report from outside with the approval passphrase: A wrong passphrase approves nothing**, 4 actions (`tests/scripts/axTask/15-approve-outside-report.txt`): Approve this report (F1, its name and Enter: 2); Tab; Type "not the passphrase at all" and Enter.
- **Approve or decline a report from outside with the approval passphrase: Approve an entry with the passphrase**, 4 actions (`tests/scripts/axTask/15-approve-outside-report.txt`): Approve this report (F1, its name and Enter: 2); Tab; Type "a long test passphrase" and Enter.
- **Convert a list written in axTask's earlier format: Convert every list of the earlier format at once**, 4 actions (`tests/scripts/axTask/16-convert-earlier-format.txt`): Enter; Tab; Tab; Enter.

## axTemplate

### Over four actions

- **Delete several templates, and meet a file that is no template, in axTemplate's browser: Archive several templates at once**, 6 actions (`tests/scripts/axTemplate/02-several-and-not-a-template.txt`): Home; Space; Down; Space; Delete; Enter.

### At four actions, over the ideal three

- **Make, write, rename and delete a template in axTemplate's browser: Write the text of a template in its form**, 4 actions (`tests/scripts/axTemplate/01-start.txt`): Enter; Tab; Type "Thank you, {clipboard}.{caret}"; Ctrl+Enter (Put in, with that program's selection).
- **Make, write, rename and delete a template in axTemplate's browser: Rename a template from its Title field**, 4 actions (`tests/scripts/axTemplate/01-start.txt`): Up, Enter; Ctrl+A (Select all); Type "Greeting"; Ctrl+Enter (Put in, with that program's selection).
- **Find a template in the launcher and use it: Read a template before using it**, 4 actions (`tests/scripts/axTemplate/03-launcher.txt`): Ctrl+A (Select all); Type "greet"; Read in full (F7); Escape.

## axTerminal

### Over four actions

- **Add a shell of your own: Add a shell of your own**, 13 actions (`tests/scripts/axTerminal/08-user-shells.txt`): Add a shell (F1, its name and Enter: 2); F7 (What is running); Enter; Type "Command Prompt" and Enter; Ctrl+A; Type "Test cmd" and Enter; Type "cmd.exe /k"; Tab; Tab; Shift+Tab; Shift+Tab; Ctrl+Enter (Send the command).
- **Switch shells and folders: Start a new session in another shell**, 9 actions (`tests/scripts/axTerminal/03-shells-and-folders.txt`): New session (Ctrl+N); Tab; Tab; Tab; Tab; Tab; Tab; Tab; Down, Enter.
- **Run commands and read their output: Copy a command line of the terminal into the field**, 7 actions (`tests/scripts/axTerminal/02-commands.txt`): Latest output (Alt+3); Shift+I; 2; 2; Down, Enter; Ctrl+A; Delete.
- **Use axTerminal's text, notices and axKit menu: Close the window while a command runs**, 6 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): Command field (Alt+1); Type "ping -n 30 127.0.0.1" and Enter; Alt+F4; Escape; Close the window (Alt+F4); Enter.
- **Agree to the reminder before a shell first starts: Agree to the reminder and start the shell**, 6 actions (`tests/scripts/axTerminal/07-consent.txt`): Tab; Enter; Space; Space; Space; Enter.
- **Use axTerminal's text, notices and axKit menu: Set a bookmark and go back to it**, 5 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): Latest output (Alt+3); Down, Down (one move); M; Ctrl+Home; K.

### At four actions, over the ideal three

- **Switch shells and folders: Restart the shell while a command runs**, 4 actions (`tests/scripts/axTerminal/03-shells-and-folders.txt`): Restart shell (Ctrl+Shift+R); Escape; Restart shell (Ctrl+Shift+R); Enter. Not counted, the example: Type "ping -n 30 127.0.0.1"; Enter.
- **Switch shells and folders: Start the shell again after it ended**, 4 actions (`tests/scripts/axTerminal/03-shells-and-folders.txt`): Terminal (Alt+2); Command field (Alt+1); Type "echo kept" and Enter; Restart shell (Ctrl+Shift+R). Not counted, the example: Type "exit"; Enter.
- **Use axTerminal's text, notices and axKit menu: Read the keyboard shortcuts**, 4 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): Keyboard shortcuts (F1, its name and Enter: 2); H; Escape.
- **Use axTerminal's text, notices and axKit menu: Read the user guide**, 4 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): User guide (F1, its name and Enter: 2); H; Escape.

## mcp

### At four actions, over the ideal three

- **Let an AI tool read axKit, once you allow it: Allow an AI tool to use axKit**, 4 actions (`tests/scripts/mcp/01-read-and-pair.txt`): Tab; Space; Tab; Enter.
