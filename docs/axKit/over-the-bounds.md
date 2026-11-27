# Over the bounds

Written by `tools/graph-check.ps1` from the scripts of `tests/scripts`, the command inventory and
`docs/axKit/workflow-graph.json`; never edit it by hand. The bounds are SPEC.md G7 and `docs/axKit/workflow-graph.md`
WG-3: a goal takes four actions at most and ideally three, and every waypoint of a window is two actions or fewer
from any of its places. A goal's actions are counted as WG-3.3 says: the app's own key is free, the steps that
only prepare or look are not counted, and neither is the example's text a goal types before its first command.
The counts are held in `docs/axKit/goal-counts.json`: a goal may get shorter, never longer (WG-3.4).

805 goals counted: 242 over four actions, 116 at four, 447 within three.
The map covers 7 apps and 58 nodes: 0 nodes with a waypoint more than two actions away (0 in all, a node and a waypoint each).

## axAutomate

### Over four actions

- **Run a workflow and hear how it went: Read how a workflow's last run went**, 7 actions (`tests/scripts/axAutomate/02-run.txt`): Down; Tab; Escape; Right, Down (one move); Tab; Down; Escape.
- **Find, filter and sort the workflows of a folder: Find a workflow and go to the next and the previous match**, 5 actions (`tests/scripts/axAutomate/06-find-filter-sort.txt`): Find in the list (Ctrl+F); Type "ap" and Enter; Find next (F3); Find next (F3); Find previous (Shift+F3).

### At four actions, over the ideal three

- **Browse a folder of workflows and read their steps: Read a step's details**, 4 actions (`tests/scripts/axAutomate/01-start.txt`): Up; Tab; Down; Escape.
- **Run a workflow and hear how it went: Run one workflow and stop it partway**, 4 actions (`tests/scripts/axAutomate/02-run.txt`): Left, Down, Right (one move); Ctrl+R (Run); Escape; Left.
- **Stop the run of a folder and see which checks need NVDA: Run only the workflows you selected**, 4 actions (`tests/scripts/axAutomate/05-stop-a-folder-run.txt`): Space; End; Space; Ctrl+R (Run).
- **Find, filter and sort the workflows of a folder: Show only the workflows whose name contains a text**, 4 actions (`tests/scripts/axAutomate/06-find-filter-sort.txt`): Filter (F1, its name and Enter: 2); Type "ap" and Enter; Ctrl+Delete (Clear the filter).
- **Find, filter and sort the workflows of a folder: Put the workflows in the order of their names**, 4 actions (`tests/scripts/axAutomate/06-find-filter-sort.txt`): Name, Options, Sort by (F1, its name and Enter: 2); The list's own order, Options, Sort by (F1, its name and Enter: 2).

## axCalc

### Over four actions

- **Use variables, earlier answers and the history in axCalc: Change an entry of the history and answer it again**, 8 actions (`tests/scripts/axCalc/02-answers-variables-history.txt`): Shift+Tab; Up, Enter; Backspace; Backspace; Backspace; Backspace; Backspace; Type "30)" and Enter.
- **Calculate and reuse earlier answers: Go back from the history to the field**, 6 actions (`tests/scripts/axCalc/01-basics.txt`): Tab; Escape (Back: in the field, nothing to go back to); Tab; Delete; Escape (Back: in the field, nothing to go back to); Ctrl+Delete (Clear the field).
- **Calculate and reuse earlier answers: Get to the end and the start of the field with the page keys**, 6 actions (`tests/scripts/axCalc/01-basics.txt`): Home, Page Down (one move); Type "0"; Page Down, Page Up (one move); Type "9"; Page Up, Page Up (one move); Ctrl+Delete (Clear the field). Not counted, the example: Type "12+34".
- **Use variables, earlier answers and the history in axCalc: See the variables and clear them**, 6 actions (`tests/scripts/axCalc/02-answers-variables-history.txt`): Variables (F1, its name and Enter: 2); Escape (Back: in the field, nothing to go back to); Clear variables (F1, its name and Enter: 2); Enter.
- **Use variables, earlier answers and the history in axCalc: Stop keeping the history between runs**, 6 actions (`tests/scripts/axCalc/02-answers-variables-history.txt`): Tab; Type "1+1" and Enter; Keep history between runs (F1, its name and Enter: 2); Close the window (Alt+F4); Shift+Tab.
- **Calculate and reuse earlier answers: Empty the field**, 5 actions (`tests/scripts/axCalc/01-basics.txt`): Ctrl+Delete (Clear the field); Ctrl+Delete (Clear the field); Type "3"; Escape (Back: in the field, nothing to go back to); Ctrl+Delete (Clear the field). Not counted, the example: Type "2+2".
- **Calculate and reuse earlier answers: Read the keys in the command palette**, 5 actions (`tests/scripts/axCalc/01-basics.txt`): F1 (Command palette); Down, Enter; Escape (Back: in the field, nothing to go back to); Ctrl+Comma; Escape (Back: in the field, nothing to go back to).
- **Use variables, earlier answers and the history in axCalc: Switch angles between degrees and radians**, 5 actions (`tests/scripts/axCalc/02-answers-variables-history.txt`): Radians (F1, its name and Enter: 2); Type "sin(pi/2)" and Enter; Degrees (F1, its name and Enter: 2). Not counted, the example: Type "sin(30)"; Enter.
- **Move through axCalc's history and find in it: Find the next and the previous match in the history**, 5 actions (`tests/scripts/axCalc/03-history-keys.txt`): Ctrl+F (Find); Type "10" and Enter; F3 (Find next); F3 (Find next); Shift+F3 (Find previous).
- **Put a template into axCalc's field from the command palette: Type a template into the field with a workflow step**, 5 actions (`tests/scripts/axCalc/05-templates.txt`): template circle area; Type "4"; Ctrl+Delete (Clear the field); template Answer twice | 5; Enter.

### At four actions, over the ideal three

- **Use variables, earlier answers and the history in axCalc: Go on from the last answer and use ans**, 4 actions (`tests/scripts/axCalc/02-answers-variables-history.txt`): Type "1920*1080*4" and Enter; Type "*2" and Enter; Type "-3" and Enter; Type "ans*2" and Enter.
- **Use variables, earlier answers and the history in axCalc: Clear the history**, 4 actions (`tests/scripts/axCalc/02-answers-variables-history.txt`): Ctrl+Shift+Delete (Clear history); Escape (Back: in the field, nothing to go back to); Ctrl+Shift+Delete (Clear history); Enter.
- **Move through axCalc's history and find in it: Hear the last answer in other bases**, 4 actions (`tests/scripts/axCalc/03-history-keys.txt`): F8 (Answer in other bases); Type "10+1" and Enter; Type "2.5*2" and Enter; Type "10*3" and Enter.
- **Put a template into axCalc's field from the command palette: Put a template into the field and type at its caret**, 4 actions (`tests/scripts/axCalc/05-templates.txt`): Command palette (F1); Type "circle" and Enter; Type "3"; Ctrl+Delete (Clear the field).
- **Put a template into axCalc's field from the command palette: Put a template in around the selected text**, 4 actions (`tests/scripts/axCalc/05-templates.txt`): Shift+Home; Command palette (F1); Type "answer twice" and Enter; Enter. Not counted, the example: Type "21".

## axClaude

### Over four actions

- **Move through a reply by headings and quick keys: Bookmark lines and move between them**, 23 actions (`tests/scripts/axClaude/04-headings.txt`): Alt+3 (Latest response); Down, Down, Down (one move); M; Up, Down, Down, Down, Down (one move); M; Ctrl+Home; K; K; K; Shift+K; Down; M; Up; K; M; Alt+1 (Message field); Ctrl+K (Bookmark this line); Down; Bookmark this line (Ctrl+K, or M in the conversation); Ctrl+Home; Next bookmark (K in the conversation); Next bookmark (K in the conversation); Previous bookmark (Shift+K in the conversation).
- **Change Claude's project folder: Start a new session with arguments of your own**, 16 actions (`tests/scripts/axClaude/07-folder.txt`): Ctrl+N (New session); Down, Down (one move); Tab; Tab; Tab; Shift+Tab; Up, Up, Up, Up, Down, Down, Down, Down (one move); Tab; Ctrl+A; Delete; Type "--model haiku"; Shift+Enter; Type "--continue --name demo" and Enter; Alt+2 (Conversation); Ctrl+Home; I.
- **Choose axClaude's speech, sounds and text: Read the version, the licence and the axKit guide**, 16 actions (`tests/scripts/axClaude/09-options.txt`): Version (F1, its name and Enter: 2); Escape; Licence (F1, its name and Enter: 2); Escape; Attribution (F1, its name and Enter: 2); Escape; axKit guide (F1, its name and Enter: 2); H; H; H; H; Escape.
- **Send Claude a message and hear the reply: Move through a long message by page**, 15 actions (`tests/scripts/axClaude/02-conversation.txt`): Page Down, Page Up (one move); Ctrl+A; Delete; Type "l1"; Shift+Enter; Type "l2"; Shift+Enter; Type "l3"; Shift+Enter; Type "l4"; Shift+Enter; Type "l5"; Ctrl+Home, Page Down, Page Down, Page Down, Page Up, Page Up, Page Up, End, Home, Ctrl+End, Ctrl+Home (one move); Ctrl+A; Delete. Not counted, the example: Type "alpha".
- **Move through a reply by headings and quick keys: Hear a reply's paragraphs, headings or bookmarks without moving**, 15 actions (`tests/scripts/axClaude/04-headings.txt`): Alt+3 (Latest response); Ctrl+Alt+P; Ctrl+Home; O; Ctrl+Alt+H; Alt+1 (Message field); Ctrl+Alt+C; Ctrl+Alt+K; Alt+2 (Conversation); Ctrl+End; Ctrl+Alt+P; Ctrl+Home; Ctrl+Alt+O; Ctrl+Alt+L; Ctrl+Alt+M.
- **Move through a reply by headings and quick keys: Quote two lines in a message and send it**, 14 actions (`tests/scripts/axClaude/04-headings.txt`): Alt+1 (Message field); Type "Reply with the single word ok."; Alt+2 (Conversation); Ctrl+Home; I; Enter; Alt+2 (Conversation); Ctrl+Home; C; Enter; Ctrl+Home, Down, Down (one move), Enter; Alt+3 (Latest response); Shift+I; Down, Down, Down, Down, Down, Down, Down, Down (one move).
- **Move through a reply by headings and quick keys: Move through the conversation by headings**, 13 actions (`tests/scripts/axClaude/04-headings.txt`): Alt+2 (Conversation); Ctrl+Home; Backspace; H; H; H; H; H; Shift+H; Ctrl+Home; 1; 1; 1.
- **Move through a reply by headings and quick keys: Go back to where you were before a jump**, 13 actions (`tests/scripts/axClaude/04-headings.txt`): Ctrl+Home; H; H; H; P; P; Backspace; Backspace; Backspace; Backspace; Backspace; Page Down; Backspace.
- **Move through a reply by headings and quick keys: Jump between replies, messages, tools, errors and system lines**, 12 actions (`tests/scripts/axClaude/04-headings.txt`): Ctrl+Home; O; R; Shift+R; Shift+O; I; Shift+I; C; T; E; D; S.
- **Use the axKit menu from a conversation: Close every window at once**, 12 actions (`tests/scripts/axClaude/13-axkit-menu.txt`): Ctrl+End; Type "more"; Alt+1 (Message field); Type "Use your Bash tool to run: pin..." and Enter; Close all windows (F1, its name and Enter: 2); Close the window (Alt+F4); Tab; Tab; Enter; Close the window (Alt+F4); Enter.
- **See Claude Code's events arrive, without Claude Code: Turn Claude Code's events off and on again**, 12 actions (`tests/scripts/axClaude/30-hooks-stand-in.txt`): Events from Claude Code's hooks (F1, its name and Enter: 2); Force Claude to restart (Ctrl+Shift+R); Enter; Say the event sources (F1, its name and Enter: 2); Events from Claude Code's hooks (F1, its name and Enter: 2); Force Claude to restart (Ctrl+Shift+R); Enter; Say the event sources (F1, its name and Enter: 2).
- **Change Claude's project folder: Restart Claude**, 11 actions (`tests/scripts/axClaude/07-folder.txt`): Ctrl+Shift+R (Force Claude to restart); Enter; Alt+2 (Conversation); Ctrl+End; Shift+I; Shift+I; Alt+1 (Message field); Type "Reply with the single word fou..." and Enter; Alt+2 (Conversation); Ctrl+End; Shift+I.
- **Choose axClaude's speech, sounds and text: Show the time on Input and Output lines**, 11 actions (`tests/scripts/axClaude/09-options.txt`): Time stamps on Input and Output lines (F1, its name and Enter: 2); Alt+1 (Message field); Type "Reply with the single word sta..." and Enter; Alt+2 (Conversation); Ctrl+End; Shift+I; Shift+O; O; Time stamps on Input and Output lines (F1, its name and Enter: 2).
- **Answer Claude's questions and screens: Answer a question with several answers**, 11 actions (`tests/scripts/axClaude/12-questions.txt`): Alt+1 (Message field); Type "Use the AskUserQuestion tool o..." and Enter; Home, Down (one move), Enter; Home; Space; Down, Down (one move); Space; Enter; Alt+2 (Conversation); Ctrl+End; Alt+1 (Message field).
- **Send Claude a message and hear the reply: Move through a message by paragraph**, 10 actions (`tests/scripts/axClaude/02-conversation.txt`): Type "one"; Shift+Enter; Shift+Enter; Type "two"; Shift+Enter; Type "three"; Shift+Enter; Shift+Enter; Type "four"; Ctrl+Home, Ctrl+Down, Ctrl+Down, Ctrl+Down, Ctrl+Up, Ctrl+Up, Ctrl+Up, Ctrl+Up (one move).
- **Answer Claude's questions and screens: Review answers before they go to Claude**, 10 actions (`tests/scripts/axClaude/12-questions.txt`): Review answers before they go to Claude (F1, its name and Enter: 2); Type "Use the AskUserQuestion tool o..." and Enter; Home, Down (one move), Enter; Home; Space; Enter; Home, Enter; Review answers before they go to Claude (F1, its name and Enter: 2).
- **Hear where axClaude's events come from: Turn Claude Code's events off and on again**, 10 actions (`tests/scripts/axClaude/28-event-sources.txt`): Events from Claude Code's hooks (F1, its name and Enter: 2); Say the event sources (F1, its name and Enter: 2); Force Claude to restart (Ctrl+Shift+R); Enter; Say the event sources (F1, its name and Enter: 2); Events from Claude Code's hooks (F1, its name and Enter: 2).
- **Answer Claude's questions and screens: Answer a question in your own words**, 9 actions (`tests/scripts/axClaude/12-questions.txt`): Home, Down, Down, Down (one move), Enter; Type "tulips in the garden by the ol..." and Enter; Alt+2 (Conversation); Ctrl+End; Alt+1 (Message field); Type "Use the AskUserQuestion tool t..." and Enter; Home, Down, Down, Down (one move), Enter; Escape; Home, Enter. Not counted, the example: Type "Use the AskUserQuestion tool t..."; Enter.
- **Find out whether a key arrives and what it does, in axClaude: Hear what the conversation's keys do**, 9 actions (`tests/scripts/axClaude/23-key-test.txt`): Test a key (F1, its name and Enter: 2); H; Shift+H; Ctrl+Alt+H; L; Y; Escape; L.
- **Give Claude axKit's AI tools from the start: Turn on the AI tools and have Claude know them**, 9 actions (`tests/scripts/axClaude/60-ai-tools-at-start.txt`): Let AI tools read and open axKit (F1, its name and Enter: 2); Enter; Ctrl+Shift+R (Force Claude to restart); Enter; Type "Look at the tools you have now..." and Enter; Alt+2 (Conversation); Ctrl+End; Alt+1 (Message field).
- **Read the conversation while Claude is writing: Quote a line of a reply in your next message**, 8 actions (`tests/scripts/axClaude/03-reading.txt`): Alt+1 (Message field); Type "Write one paragraph of 120 wor..." and Enter; Alt+3 (Latest response); C; L; Enter; Ctrl+A; Delete.
- **Answer Claude's questions and screens: Have questions come without interrupting speech**, 8 actions (`tests/scripts/axClaude/12-questions.txt`): Go to them at once, interrupting speech (F1, its name and Enter: 2); Alt+1 (Message field); Type "Write two sentences about gard..." and Enter; Alt+2 (Conversation); Home, Enter; Go to them at once, interrupting speech (F1, its name and Enter: 2).
- **Choose which words axClaude says by itself: Keep reply lines with chosen words silent**, 8 actions (`tests/scripts/axClaude/17-what-is-spoken.txt`): Speak replies: all lines (F1, its name and Enter: 2); Words that keep a reply line silent (F1, its name and Enter: 2); Type "pelican; walrus" and Enter; Type "Reply with exactly two lines a..." and Enter; Alt+2 (Conversation); Ctrl+End.
- **Find out whether a key arrives and what it does, in axClaude: Hear what a key does without doing it**, 8 actions (`tests/scripts/axClaude/23-key-test.txt`): F9 (Say the status); F4 (Send a report); F8 (What runs in the background); Alt+2 (Conversation); Ctrl+I (Interrupt); A; Enter; F11.
- **Hear where axClaude's events come from: Turn the grounding in Claude Code's session file off and on again**, 8 actions (`tests/scripts/axClaude/28-event-sources.txt`): Ground the conversation in Claude Code's session file (F1, its name and Enter: 2); Say the event sources (F1, its name and Enter: 2); Ground the conversation in Claude Code's session file (F1, its name and Enter: 2); Say the event sources (F1, its name and Enter: 2).
- **See the session file followed, without Claude Code: Turn the grounding in Claude Code's session file off and on again**, 8 actions (`tests/scripts/axClaude/32-session-file.txt`): Ground the conversation in Claude Code's session file (F1, its name and Enter: 2); Say the event sources (F1, its name and Enter: 2); Ground the conversation in Claude Code's session file (F1, its name and Enter: 2); Say the event sources (F1, its name and Enter: 2).
- **Send Claude a message and hear the reply: Read the keyboard shortcuts**, 7 actions (`tests/scripts/axClaude/02-conversation.txt`): Keyboard shortcuts (F1, its name and Enter: 2); H; H; H; Shift+H; Escape.
- **Change the permission mode and interrupt Claude: Interrupt Claude from the menu or the message field**, 7 actions (`tests/scripts/axClaude/05-modes.txt`): Alt+1 (Message field); Type "Write a story of 1500 words ab..." and Enter; Ctrl+C (F1, its name and Enter: 2); Type "Write a story of 1500 words ab..." and Enter; Ctrl+C (F1, its name and Enter: 2).
- **Choose axClaude's speech, sounds and text: Play background music while Claude works**, 7 actions (`tests/scripts/axClaude/09-options.txt`): Ctrl+M (Background music on or off); Type "Use the Bash tool to run: echo..." and Enter; Ctrl+M (Background music on or off); Type "Write a story of 1500 words ab..." and Enter; Alt+2 (Conversation); Ctrl+M (Background music on or off); Ctrl+M (Background music on or off).
- **Choose axClaude's speech, sounds and text: Have Claude's replies spoken**, 7 actions (`tests/scripts/axClaude/09-options.txt`): Speak replies: all lines (F1, its name and Enter: 2); Alt+1 (Message field); Type "Markdown only: ## Alpha, a fiv..." and Enter; Speak replies: first lines (F1, its name and Enter: 2); Type "Reply with two lines: the word..." and Enter.
- **Choose axClaude's speech, sounds and text: Hear each tool call as it starts**, 7 actions (`tests/scripts/axClaude/09-options.txt`): Speak replies: none (F1, its name and Enter: 2); Speak each tool call (F1, its name and Enter: 2); Type "Use the Bash tool to run: echo..." and Enter; Speak each tool call (F1, its name and Enter: 2).
- **Choose axClaude's speech, sounds and text: Find a word in the keyboard shortcuts**, 7 actions (`tests/scripts/axClaude/09-options.txt`): Ctrl+F (Find); Ctrl+A; Type "palette" and Enter; F3 (Find next); Shift+F3 (Find previous); Ctrl+F (Find); Escape.
- **Choose axClaude's speech, sounds and text: Read the user guide**, 7 actions (`tests/scripts/axClaude/09-options.txt`): User guide (F1, its name and Enter: 2); H; Shift+H; Ctrl+End; H; Escape.
- **Answer Claude's questions and screens: Move through Claude's questions in the conversation**, 7 actions (`tests/scripts/axClaude/12-questions.txt`): Alt+2 (Conversation); Ctrl+Home; Q; Q; Ctrl+End; Q; Shift+Q.
- **Report a bug or an idea from a conversation: Send the report**, 7 actions (`tests/scripts/axClaude/16-report.txt`): Shift+Tab; Shift+Tab; Shift+Tab; Shift+Tab; Shift+Tab; Ctrl+Enter (Send the message); Escape.
- **Choose which words axClaude says by itself: Have every reply line spoken again**, 7 actions (`tests/scripts/axClaude/17-what-is-spoken.txt`): Words that keep a reply line silent (F1, its name and Enter: 2); Ctrl+A; Delete; Enter; Alt+1 (Message field); Type "Reply with exactly one line: t..." and Enter.
- **Answer Claude's own questions over Claude Code's stream: Answer the first question in your own words**, 7 actions (`tests/scripts/axClaude/42-stream-questions.txt`): Type "Ask me two things" and Enter; Type "x"; Type "3"; End, Enter; Escape; End, Enter; Type "teal" and Enter.
- **Start a conversation with Claude: Move between the message field and the conversation**, 6 actions (`tests/scripts/axClaude/01-startup.txt`): Ctrl+Tab (Switch between the view and the field); Ctrl+Tab (Switch between the view and the field); F6 (Switch between the view and the field); F6 (Switch between the view and the field); Ctrl+Shift+Tab (Switch between the view and the field); Ctrl+Shift+Tab (Switch between the view and the field).
- **Read the conversation while Claude is writing: Select lines of a reply and clear the selection**, 6 actions (`tests/scripts/axClaude/03-reading.txt`): Alt+2 (Conversation); Ctrl+Home; O; Down, Down, Shift+Down, Shift+Down (one move); Escape; Escape.
- **Move through a reply by headings and quick keys: Hide the markers in the text**, 6 actions (`tests/scripts/axClaude/04-headings.txt`): Markers in the text (F1, its name and Enter: 2); Ctrl+Home; H; Markers in the text (F1, its name and Enter: 2).
- **Send messages while Claude is still working: Send a message while Claude works**, 6 actions (`tests/scripts/axClaude/06-queue.txt`): Ctrl+Tab (Switch between the view and the field); Ctrl+End; Shift+I; Shift+I; Ctrl+Tab (Switch between the view and the field); Up. Not counted, the example: Type "Use your Bash tool to run: pin..."; Enter; Type "Reply with the single word sec..."; Enter.
- **Send messages while Claude is still working: Send a waiting message at once**, 6 actions (`tests/scripts/axClaude/06-queue.txt`): Alt+1 (Message field); Type "Use your Bash tool to run: pin..." and Enter; Type "Reply with the single word fou..." and Enter; Ctrl+Shift+Enter (Send queued messages now); Alt+2 (Conversation); Ctrl+Shift+Enter (Send queued messages now).
- **Change Claude's project folder: Continue the last conversation in a folder**, 6 actions (`tests/scripts/axClaude/07-folder.txt`): Alt+2 (Conversation); Ctrl+Home; I; Down, Down, Down, Down, Down (one move); I; I.
- **Choose axClaude's speech, sounds and text: Read the keyboard shortcuts**, 6 actions (`tests/scripts/axClaude/09-options.txt`): Keyboard shortcuts (F1, its name and Enter: 2); H; H; Shift+H; 2.
- **Check for an update from a conversation: Check for an update**, 6 actions (`tests/scripts/axClaude/11-updates.txt`): Check for updates (axKit (F1, its name and Enter: 2); Escape; Version (F1, its name and Enter: 2); Escape.
- **Check for an update from a conversation: Turn off the update check at start**, 6 actions (`tests/scripts/axClaude/11-updates.txt`): Check for updates at start (F1, its name and Enter: 2); Close the window (Alt+F4); Version (F1, its name and Enter: 2); Escape.
- **Answer Claude's questions and screens: Interrupt Claude from a permission question**, 6 actions (`tests/scripts/axClaude/12-questions.txt`): Ctrl+P (Next permission mode); Type "Use your Bash tool to run: pin..." and Enter; Ctrl+I (Interrupt); Ctrl+P (Next permission mode); Ctrl+P (Next permission mode); Ctrl+P (Next permission mode).
- **Read a side answer and other information screens: Find side answers in the conversation**, 6 actions (`tests/scripts/axClaude/14-side-answer.txt`): Alt+2 (Conversation); Ctrl+Home; B; B; B; Shift+B.
- **Agree to the reminder before Claude Code first starts: Agree to the reminder before Claude Code starts**, 6 actions (`tests/scripts/axClaude/15-consent.txt`): Tab; Enter; Space; Enter; Enter; Close the window (Alt+F4).
- **Report a bug or an idea from a conversation: Add detail that may name your files**, 6 actions (`tests/scripts/axClaude/16-report.txt`): Shift+Tab; Space; Tab; Shift+Tab; Space; Tab.
- **Choose which words axClaude says by itself: Silence Claude's own notices**, 6 actions (`tests/scripts/axClaude/17-what-is-spoken.txt`): Ctrl+P (Next permission mode); Claude's own notices, such as "accept edits on" (F1, its name and Enter: 2); Ctrl+P (Next permission mode); Ctrl+P (Next permission mode); Ctrl+P (Next permission mode).
- **Hand a conversation from Claude Code's stream to its screen and back, with Claude Code itself: Take the conversation up over the stream again and read its earlier part**, 6 actions (`tests/scripts/axClaude/52-stream-live-handover.txt`): Tab; Enter; Conversation (Alt+2); Message field (Alt+1); Type "Which word did I ask you to re..." and Enter; Conversation (Alt+2).
- **Start a conversation with Claude: Move between the conversation and Claude's question, then answer it**, 5 actions (`tests/scripts/axClaude/01-startup.txt`): Tab; Tab; Shift+Tab; Shift+Tab; Enter.
- **Move through a reply by headings and quick keys: Move through the conversation by paragraph**, 5 actions (`tests/scripts/axClaude/04-headings.txt`): Ctrl+Home; P; Shift+P; Ctrl+End; P.
- **Move through a reply by headings and quick keys: Hear the line number**, 5 actions (`tests/scripts/axClaude/04-headings.txt`): L; Ctrl+Home; L; Ctrl+End; L.
- **Move through a reply by headings and quick keys: Find the next match from the message field**, 5 actions (`tests/scripts/axClaude/04-headings.txt`): Alt+1 (Message field); Ctrl+F (Find); Type "single word" and Enter; Alt+1 (Message field); F3 (Find next).
- **Change the permission mode and interrupt Claude: Interrupt Claude while it works**, 5 actions (`tests/scripts/axClaude/05-modes.txt`): Escape; Ctrl+O; Ctrl+I (Interrupt); Alt+2 (Conversation); Ctrl+End. Not counted, the example: Type "Use your Bash tool to run: pin..."; Enter; Type "1"; Enter.
- **Choose axClaude's speech, sounds and text: Turn off the ticks for new lines and tool calls**, 5 actions (`tests/scripts/axClaude/09-options.txt`): Ticks for new lines and tool calls (F1, its name and Enter: 2); Type "Use the Bash tool to run: echo..." and Enter; Ticks for new lines and tool calls (F1, its name and Enter: 2).
- **Choose axClaude's speech, sounds and text: Turn off the chimes**, 5 actions (`tests/scripts/axClaude/09-options.txt`): Chimes (F1, its name and Enter: 2); Type "Reply with the single word qui..." and Enter; Chimes (F1, its name and Enter: 2).
- **Install Claude Code when it is missing: Hear the ways to install Claude Code**, 5 actions (`tests/scripts/axClaude/10-missing.txt`): Tab; Tab; Tab; Tab; Tab.
- **Read a side answer and other information screens: Browse earlier side answers**, 5 actions (`tests/scripts/axClaude/14-side-answer.txt`): Type "/btw what is three plus three?..." and Enter; Tab; Tab; Enter; Escape.
- **Report a bug or an idea from a conversation: Cancel a report**, 5 actions (`tests/scripts/axClaude/16-report.txt`): F4 (Send a report); Escape; F4 (Send a report); Type "Draft words"; Escape.
- **Choose which words axClaude says by itself: Turn the words Sent and Responding back on**, 5 actions (`tests/scripts/axClaude/17-what-is-spoken.txt`): "Sent" and "Queued" after a message (F1, its name and Enter: 2); "Responding" when a reply starts (F1, its name and Enter: 2); Type "Reply with the single word two..." and Enter.
- **Read back a long message before sending it, and turn the music on with Ctrl+M: Move through a message by its headings, links and lists**, 5 actions (`tests/scripts/axClaude/25-edit-modes.txt`): Ctrl+Space; K; I; I; Shift+H.
- **Answer Claude's own questions over Claude Code's stream: Tick several answers of the second question**, 5 actions (`tests/scripts/axClaude/42-stream-questions.txt`): Space; Down, Down (one move); Space; Enter; Conversation (Alt+2).
- **Choose the model and the effort over Claude Code's stream: Choose the effort**, 5 actions (`tests/scripts/axClaude/50-stream-choices.txt`): Message field (Alt+1); Choose the effort (F1, its name and Enter: 2); Enter; Conversation (Alt+2).
- **Search the rules of Claude Code's permissions screen: Leave the search**, 5 actions (`tests/scripts/axClaude/53-permissions-search.txt`): Escape; Escape; Ctrl+I (Interrupt); Alt+2 (Conversation); Ctrl+End.

### At four actions, over the ideal three

- **Start a conversation with Claude: Jump straight to the conversation or the message field**, 4 actions (`tests/scripts/axClaude/01-startup.txt`): Alt+2 (Conversation); Alt+2 (Conversation); Alt+1 (Message field); Alt+1 (Message field).
- **Start a conversation with Claude: Hear the status**, 4 actions (`tests/scripts/axClaude/01-startup.txt`): F9 (Say the status); Alt+2 (Conversation); F9 (Say the status); Say the status (F9).
- **Read the conversation while Claude is writing: Go to Claude's latest reply**, 4 actions (`tests/scripts/axClaude/03-reading.txt`): Alt+3 (Latest response); Alt+1 (Message field); Alt+3 (Latest response); Down, Down (one move).
- **Move through a reply by headings and quick keys: Move by heading level**, 4 actions (`tests/scripts/axClaude/04-headings.txt`): Ctrl+Home; 2; 2; 3.
- **Move through a reply by headings and quick keys: Find text in the conversation**, 4 actions (`tests/scripts/axClaude/04-headings.txt`): Ctrl+F (Find); Type "single word" and Enter; F3 (Find next); Shift+F3 (Find previous).
- **Change the permission mode and interrupt Claude: Change the permission mode**, 4 actions (`tests/scripts/axClaude/05-modes.txt`): Ctrl+P (Next permission mode); Ctrl+P (Next permission mode); Ctrl+P (Next permission mode); Ctrl+P (Next permission mode).
- **Change the permission mode and interrupt Claude: Send Claude a key from the menu**, 4 actions (`tests/scripts/axClaude/05-modes.txt`): Shift+Up (F1, its name and Enter: 2); Shift+Down (F1, its name and Enter: 2).
- **Restart, stop and close a conversation: Restart Claude while it runs**, 4 actions (`tests/scripts/axClaude/08-lifecycle.txt`): Ctrl+Shift+R (Force Claude to restart); Escape; Ctrl+Shift+R (Force Claude to restart); Enter.
- **Restart, stop and close a conversation: Decide whether to close the window while Claude works**, 4 actions (`tests/scripts/axClaude/08-lifecycle.txt`): Alt+F4; Tab; Tab; Escape. Not counted, the example: Type "Use your Bash tool to run: pin..."; Enter.
- **Answer Claude's questions and screens: Move between a question and the conversation**, 4 actions (`tests/scripts/axClaude/12-questions.txt`): Ctrl+Tab (Switch between the view and the field); Ctrl+Tab (Switch between the view and the field); Tab; Shift+Tab. Not counted, the example: Type "Use your AskUserQuestion tool ..."; Enter.
- **Answer Claude's questions and screens: Hear the question again**, 4 actions (`tests/scripts/axClaude/12-questions.txt`): Down, Down (one move); F7 (Claude's question again); Ctrl+Tab (Switch between the view and the field); F7 (Claude's question again).
- **Answer Claude's questions and screens: Answer a question that follows a reply**, 4 actions (`tests/scripts/axClaude/12-questions.txt`): Alt+2 (Conversation); Ctrl+Tab (Switch between the view and the field); Ctrl+Tab (Switch between the view and the field); Home, Enter. Not counted, the example: Type "Write two sentences about gard..."; Enter.
- **Run a command Claude offers you: Read the explanation of an offered command**, 4 actions (`tests/scripts/axClaude/18-offered-command.txt`): Alt+2 (Conversation); Alt+1 (Message field); Enter; Enter.
- **See an offered command wait its turn: Turn down waiting offers one after another**, 4 actions (`tests/scripts/axClaude/19-offered-command-waits.txt`): Escape; Escape; Escape; Escape.
- **Read back a long message before sending it, and turn the music on with Ctrl+M: Go back to editing the message**, 4 actions (`tests/scripts/axClaude/25-edit-modes.txt`): Message field (Alt+1); Ctrl+Space; End; Type "!".
- **Block Claude Code's feedback requests from the Options menu, and turn it off again: Choose how Claude-drafted feedback comes**, 4 actions (`tests/scripts/axClaude/26-claude-code-settings.txt`): Claude-drafted feedback: quiet (F1, its name and Enter: 2); Claude-drafted feedback: a card for each (F1, its name and Enter: 2).
- **Follow a workflow Claude runs in the background without hearing its live row: Show Claude Code's live progress rows in the conversation**, 4 actions (`tests/scripts/axClaude/27-workflow-progress.txt`): Show Claude Code's live progress rows in the conversation (F1, its name and Enter: 2); Conversation (Alt+2); Ctrl+End.
- **Choose the model and the effort over Claude Code's stream: Choose the model from the list Claude Code gave**, 4 actions (`tests/scripts/axClaude/50-stream-choices.txt`): Choose the model (F1, its name and Enter: 2); Down, Down, Down, Down (one move), Enter; Conversation (Alt+2).
- **Search the rules of Claude Code's permissions screen: Search the rules of the permissions screen**, 4 actions (`tests/scripts/axClaude/53-permissions-search.txt`): End, Up (one move), Enter; Type "git" and Enter; Down; F7 (Claude's question again). Not counted, the example: Type "/permissions"; Enter.

## axClip

### Over four actions

- **Copy again, read a long copy and keep the history between runs: Keep the history between runs**, 7 actions (`tests/scripts/axClip/02-history-kept.txt`): Keep history between runs (F1, its name and Enter: 2); Quit axClip (F1, its name and Enter: 2); Type "axkitkeep"; Down; Last match (Ctrl+End).
- **Narrow the clipboard history with several words: Type a second word to narrow the list**, 6 actions (`tests/scripts/axClip/04-several-words.txt`): First match (Ctrl+Home); Type "red"; First match (Ctrl+Home); Down; Type "apple"; First match (Ctrl+Home). Not counted, the example: Type "axkitword".

### At four actions, over the ideal three

- **Find and paste something you copied earlier: Jump to the newest or oldest item, or by a page**, 4 actions (`tests/scripts/axClip/01-basics.txt`): Last match (Ctrl+End); Down; First match (Ctrl+Home); Page Down, Page Up (one move).

## axDown

### Over four actions

- **Move through a Markdown file by headings, lists and links: Move by list and by list item**, 11 actions (`tests/scripts/axDown/08-navigation.txt`): Ctrl+Home; List, Navigation, Next (Ctrl+L); List item, Navigation, Next (Ctrl+I); List item, Navigation, Next (Ctrl+I); List item, Navigation, Next (Ctrl+I); List item, Navigation, Next (Ctrl+I); Ctrl+Home; List, Navigation, Next (Ctrl+L); List, Navigation, Next (Ctrl+L); List, Navigation, Next (Ctrl+L); List, Navigation, Previous (Ctrl+Shift+L).
- **Find out whether a key arrives and what it does: Hear what a key does without doing it**, 10 actions (`tests/scripts/axDown/15-key-test.txt`): F9 (Say the status); F4 (Send a report); F1 (Command palette); Ctrl+Comma; F12 (Go to line); Ctrl+H (Heading); A; Down, Enter; Ctrl+Down; Shift+F9.
- **Move through a file with single letters without changing it, then type again: Move by single letters**, 10 actions (`tests/scripts/axDown/16-navigation-mode.txt`): H; H; H; Shift+H; Ctrl+Home; 2; K; L; I; Shift+I.
- **Find a command in the menus, the palette and the help: Read the licence, the attribution and the axKit guide**, 9 actions (`tests/scripts/axDown/06-menus-and-help.txt`): Licence (F1, its name and Enter: 2); Escape; Attribution (F1, its name and Enter: 2); Escape; axKit guide (F1, its name and Enter: 2); Escape.
- **Move through a Markdown file by headings, lists and links: Move to the next heading**, 9 actions (`tests/scripts/axDown/08-navigation.txt`): Heading, Navigation, Next (Ctrl+H); Heading, Navigation, Next (Ctrl+H); Heading, Navigation, Next (Ctrl+H); Heading, Navigation, Next (Ctrl+H); Heading, Navigation, Next (Ctrl+H); Heading, Navigation, Next (Ctrl+H); Heading, Navigation, Next (Ctrl+H); Heading, Navigation, Next (Ctrl+H); Heading, Navigation, Next (Ctrl+H).
- **Move through a Markdown file by headings, lists and links: Move back to the previous heading**, 9 actions (`tests/scripts/axDown/08-navigation.txt`): Heading, Navigation, Previous (Ctrl+Shift+H); Heading, Navigation, Previous (Ctrl+Shift+H); Heading, Navigation, Previous (Ctrl+Shift+H); Heading, Navigation, Previous (Ctrl+Shift+H); Heading, Navigation, Previous (Ctrl+Shift+H); Heading, Navigation, Previous (Ctrl+Shift+H); Heading, Navigation, Previous (Ctrl+Shift+H); Heading, Navigation, Previous (Ctrl+Shift+H); Heading, Navigation, Previous (Ctrl+Shift+H).
- **Move through a Markdown file by headings, lists and links: Move to a heading of one level**, 9 actions (`tests/scripts/axDown/08-navigation.txt`): Ctrl+Home; Heading level 2, Navigation, Next (Ctrl+2); Heading level 2, Navigation, Next (Ctrl+2); Heading level 2, Navigation, Next (Ctrl+2); Heading level 2, Navigation, Next (Ctrl+2); Heading level 2, Navigation, Next (Ctrl+2); Heading level 2, Navigation, Previous (Ctrl+Shift+2); Heading level 4, Navigation, Next (Ctrl+4); Heading level 4, Navigation, Previous (Ctrl+Shift+4).
- **Give a line, a word and several lines their Markdown format, and take it away again: Make a line a heading and change its level**, 7 actions (`tests/scripts/axDown/17-format.txt`): Alt+Shift+Right (Heading deeper); Alt+Shift+Right (Heading deeper); Alt+Shift+Left (Heading shallower); Heading 4, Actions, Format (F1, its name and Enter: 2); Heading 4, Actions, Format (F1, its name and Enter: 2).
- **Open, save and switch files in axDown: Close a window without saving its changes**, 6 actions (`tests/scripts/axDown/03-files.txt`): Close the window (Alt+F4); Escape; Close the window (Alt+F4); Tab; Tab; Enter. Not counted, the example: Type "More.".
- **Check for updates and remove axKit: Check for updates**, 6 actions (`tests/scripts/axDown/07-updates-and-uninstall.txt`): Check for updates (axKit (F1, its name and Enter: 2); Escape; Version (F1, its name and Enter: 2); Escape.
- **Move through a Markdown file by headings, lists and links: Move to a block quote or a table**, 6 actions (`tests/scripts/axDown/08-navigation.txt`): Ctrl+Home; Block quote, Navigation, Next (Ctrl+Q); Block quote, Navigation, Next (Ctrl+Q); Ctrl+Home; Table, Navigation, Next (Ctrl+T); Table, Navigation, Next (Ctrl+T).
- **Ask Claude about the window you are in: Turn Claude on by agreeing to the reminder**, 6 actions (`tests/scripts/axDown/11-ask-claude.txt`): Ask about this window (Shift+F1); Tab; Enter; Space; Enter; Escape.
- **Hear a line break as you type it: Turn the spoken new line off and on**, 6 actions (`tests/scripts/axDown/12-say-new-line.txt`): Say new line (F1, its name and Enter: 2); End, Enter; Say new line (F1, its name and Enter: 2); Enter.
- **Hear the heading sound as you move: Turn the heading sound off and on**, 6 actions (`tests/scripts/axDown/13-heading-sound.txt`): Sound on headings (F1, its name and Enter: 2); Down, Down, Down, Down (one move); Sound on headings (F1, its name and Enter: 2); Up, Down (one move).
- **Find a command in the menus, the palette and the help: Move through the keyboard shortcuts by heading**, 5 actions (`tests/scripts/axDown/06-menus-and-help.txt`): H; Ctrl+H (Heading); Shift+H; H; H.
- **Find a command in the menus, the palette and the help: Find a word in the keyboard shortcuts**, 5 actions (`tests/scripts/axDown/06-menus-and-help.txt`): Ctrl+F (Find); Type "wrap" and Enter; F3 (Find next); Ctrl+F (Find); Escape.
- **Find a command in the menus, the palette and the help: Read the user guide**, 5 actions (`tests/scripts/axDown/06-menus-and-help.txt`): User guide (F1, its name and Enter: 2); Ctrl+H (Heading); Ctrl+T (Table); Escape.
- **Move through a Markdown file by headings, lists and links: Move by heading from the menu**, 5 actions (`tests/scripts/axDown/08-navigation.txt`): Ctrl+Home; Heading level 3, Navigation, Next (Ctrl+3); Heading level 1, Navigation, Previous (Ctrl+Shift+1); Heading, Navigation, Next (Ctrl+H); Heading, Navigation, Previous (Ctrl+Shift+H).
- **Move through a Markdown file by headings, lists and links: Move to the next link**, 5 actions (`tests/scripts/axDown/08-navigation.txt`): Ctrl+Home; Link, Navigation, Next (Ctrl+K); Link, Navigation, Next (Ctrl+K); Link, Navigation, Next (Ctrl+K); Link, Navigation, Next (Ctrl+K).

### At four actions, over the ideal three

- **Open, save and switch files in axDown: Start a new document when the text has changes**, 4 actions (`tests/scripts/axDown/03-files.txt`): New (Ctrl+N); Escape; New (Ctrl+N); Enter. Not counted, the example: Type "Added.".
- **Open, save and switch files in axDown: Reload a file that another program changed**, 4 actions (`tests/scripts/axDown/03-files.txt`): Save (Ctrl+S); Tab; Tab; Enter.
- **Find text and go to a line: Find a word in the text**, 4 actions (`tests/scripts/axDown/04-find-and-go-to-line.txt`): Find (Ctrl+F); Type "apple" and Enter; Find next (F3); Find previous (Shift+F3).
- **Change the text size and the font: Change the text size**, 4 actions (`tests/scripts/axDown/05-text-size-and-font.txt`): Larger text (Ctrl+Plus); Smaller text (Ctrl+Minus); Larger text (Ctrl+Plus); Close the window (Alt+F4).
- **Find a command in the menus, the palette and the help: Turn the update check at start on or off**, 4 actions (`tests/scripts/axDown/06-menus-and-help.txt`): Command palette (F1); Type "check for updates at start" and Enter; Command palette (F1); Type "check for updates at start" and Enter.
- **Find a command in the menus, the palette and the help: Run a command found by its name**, 4 actions (`tests/scripts/axDown/06-menus-and-help.txt`): Ctrl+A; Type "wrap"; Down, Enter; Word wrap (F11).
- **Change the colours with the theme file: Turn the theme off and on**, 4 actions (`tests/scripts/axDown/10-theme.txt`): Theme on (F1, its name and Enter: 2); Theme on (F1, its name and Enter: 2).
- **Ask Claude about the window you are in: Turn Claude off and on**, 4 actions (`tests/scripts/axDown/11-ask-claude.txt`): Use Claude in axKit (F1, its name and Enter: 2); Use Claude in axKit (F1, its name and Enter: 2).
- **Move through a file with single letters without changing it, then type again: Go back to edit mode and type**, 4 actions (`tests/scripts/axDown/16-navigation-mode.txt`): Ctrl+Space; Home; Type "X"; Undo (Ctrl+Z).
- **Give a line, a word and several lines their Markdown format, and take it away again: Make several lines list items and undo it in one step**, 4 actions (`tests/scripts/axDown/17-format.txt`): Ctrl+Down, Shift+Down, Shift+End (one move); List item, Actions, Format (F1, its name and Enter: 2); Ctrl+Z (Undo).
- **Put a template in at the caret: a link around the selected words, a table, and a template of your own: Make the selected word a link and type its address**, 4 actions (`tests/scripts/axDown/18-templates.txt`): Shift+End; Link, Actions, Insert template (F1, its name and Enter: 2); Type "https://example.org".
- **Put a template in at the caret: a link around the selected words, a table, and a template of your own: Put in a table and type into its first cell**, 4 actions (`tests/scripts/axDown/18-templates.txt`): Ctrl+End; Table, Actions, Insert template (F1, its name and Enter: 2); Type "Name".
- **Put a template in at the caret: a link around the selected words, a table, and a template of your own: Put in a template of your own from the command palette, with the selected words carried into it**, 4 actions (`tests/scripts/axDown/18-templates.txt`): Ctrl+Home, Down, Down, Shift+End (one move); Command palette (F1); Type "meeting" and Enter; Type "of Monday".

## axFile

### Over four actions

- **Browse folders and files: Show hidden files and names that start with a period**, 12 actions (`tests/scripts/axFile/01-browse.txt`): Go to folder (F12); Type "{temp}\hid" and Enter; End, Enter; Up; Show files and folders starting with a period (F1, its name and Enter: 2); Show hidden files and folders (F1, its name and Enter: 2); Show hidden files and folders (F1, its name and Enter: 2); Show files and folders starting with a period (F1, its name and Enter: 2).
- **Browse folders and files: Find a file by part of its name**, 12 actions (`tests/scripts/axFile/01-browse.txt`): Go to folder (F12); Type "{temp}\fs" and Enter; Find (Ctrl+F); Type "find" and Enter; Home; Find (Ctrl+F); Type "notes" and Enter; Find next (F3); Find previous (Shift+F3); Find (Ctrl+F); Type "zzz" and Enter; Escape.
- **Browse folders and files: Show only the entries whose names contain a word**, 12 actions (`tests/scripts/axFile/01-browse.txt`): Home; Filter (F1, its name and Enter: 2); Type "chap" and Enter; Down; Information (F8); Ctrl+Delete (Clear the filter); Filter (F1, its name and Enter: 2); Type "zzz" and Enter; Enter; Ctrl+Delete (Clear the filter).
- **Browse folders and files: Select several entries**, 12 actions (`tests/scripts/axFile/01-browse.txt`): Go to folder (F12); Type "{temp}\sel" and Enter; Down, Down (one move); Space; Down, Up, Down (one move); Space; Information (F8); Space; Select all (Ctrl+A); Down; Deselect all (Ctrl+Shift+A); Down.
- **Find out whether a key arrives and what it does, in a list: Hear what a key does without doing it**, 12 actions (`tests/scripts/axFile/09-key-test.txt`): F9 (Say the status); F4 (Send a report); F5 (Refresh); F12 (Go to folder); Delete; Enter; Backspace; Ctrl+F (Find); Down; B; Shift+F9; Alt+F4.
- **Keep favorite folders and go back to recent operations: Go to an operation done in another folder**, 10 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Find (Ctrl+F); Type "subfolder" and Enter; Enter; New folder (Ctrl+Shift+N); Type "inner" and Enter; Backspace; Recent operations (F1, its name and Enter: 2); Enter; Backspace.
- **Browse folders and files: Sort the list by date, size or name**, 9 actions (`tests/scripts/axFile/01-browse.txt`): Date, Options, Sort by (F1, its name and Enter: 2); Home, Down (one move); Size, Options, Sort by (F1, its name and Enter: 2); Home, Down (one move); Name, Options, Sort by (F1, its name and Enter: 2); Home.
- **Copy, move, rename and delete files: Move selected files into a new folder**, 8 actions (`tests/scripts/axFile/03-operations.txt`): Space; Down, Down, Down (one move); Space; Move to folder, Context (F1, its name and Enter: 2); Type "bundle" and Enter; Enter; Backspace.
- **Copy, move, rename and delete files: Extract a zip file here**, 8 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "docs.zip" and Enter; Extract here, Context (F1, its name and Enter: 2); Enter; Enter; Backspace; Backspace.
- **Copy, move, rename and delete files: Undo a move to a folder**, 8 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "move-two" and Enter; Space; Shift+Down; Move to folder, Context (F1, its name and Enter: 2); Type "bundle" and Enter; Undo (Ctrl+Z).
- **Keep favorite folders and go back to recent operations: Add a folder to the Favorites list**, 8 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Enter; Ctrl+D; Backspace; Down, Enter; Ctrl+D; Tab; Escape; Backspace.
- **Take back a new text file: Make a new Markdown file and a new JSON file**, 8 actions (`tests/scripts/axFile/12-undo-new-text-file.txt`): New Markdown file (F1, its name and Enter: 2); Escape; New JSON file (F1, its name and Enter: 2); Escape; Ctrl+Z (Undo); Ctrl+Z (Undo).
- **Copy, move, rename and delete files: Make a new folder**, 7 actions (`tests/scripts/axFile/03-operations.txt`): New folder (Ctrl+Shift+N); Enter; Type "a?b" and Enter; Backspace; Backspace; Backspace; Type "notes" and Enter.
- **Copy, move, rename and delete files: Rename a folder**, 7 actions (`tests/scripts/axFile/03-operations.txt`): Home, Down, Down (one move); Rename (F2); Type "Notes" and Enter; Rename (F2); Type "Notes2" and Enter; Rename (F2); Type "Notes" and Enter.
- **Copy, move, rename and delete files: Compress a folder to a zip file**, 7 actions (`tests/scripts/axFile/03-operations.txt`): Down; Compress to zip, Context (F1, its name and Enter: 2); Find (Ctrl+F); Type "docs" and Enter; Compress to zip, Context (F1, its name and Enter: 2).
- **Copy, move, rename and delete files: Undo the delete of several entries**, 7 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "delete-two" and Enter; Space; Shift+Down; Delete; Enter; Undo (Ctrl+Z).
- **Keep favorite folders and go back to recent operations: Move to the Favorites list from the menu**, 7 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): The Favorites list, Navigation (F1, its name and Enter: 2); Shift+Tab; F6 (Switch between the list and the Favorites list); Shift+Tab; Ctrl+Tab (Switch between the list and the Favorites list); Shift+Tab.
- **Keep favorite folders and go back to recent operations: Select entries without an operation**, 7 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Find (Ctrl+F); Type "start-here" and Enter; Space; Recent operations (F1, its name and Enter: 2); Escape; Space.
- **Keep favorite folders and go back to recent operations: Select every entry from the marked one to here**, 7 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Home, Down (one move); Space; Down, Down, Down (one move); Shift+Space; Deselect all (Ctrl+Shift+A); Shift+Space; Shift+Space.
- **Browse folders and files: Move between the list and the Favorites list**, 6 actions (`tests/scripts/axFile/01-browse.txt`): Tab; Shift+Tab; F6 (Switch between the list and the Favorites list); Escape; Ctrl+Tab (Switch between the list and the Favorites list); Escape.
- **Browse folders and files: Go to the drives of This PC**, 6 actions (`tests/scripts/axFile/01-browse.txt`): Go to folder (F12); Type "C:\" and Enter; Backspace; Information (F8); New folder (Ctrl+Shift+N); Backspace.
- **Browse folders and files: Complete a path from its first letters**, 6 actions (`tests/scripts/axFile/01-browse.txt`): Go to folder (F12); Type "{temp}\nav\lev"; Down, Right, Down, Up, Up (one move); Type "level-2-f"; Down; Escape.
- **Browse folders and files: Add a folder to the favorites and remove it**, 6 actions (`tests/scripts/axFile/01-browse.txt`): Ctrl+D; Tab; Shift+Tab; Ctrl+D; Tab; Shift+Tab.
- **Browse folders and files: Clear the selection**, 6 actions (`tests/scripts/axFile/01-browse.txt`): Space; Down; Space; Escape; Up; Escape.
- **Copy, move, rename and delete files: Compress selected files into one zip file**, 6 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "move-into" and Enter; Space; Shift+Down; Compress to zip, Context (F1, its name and Enter: 2).
- **Keep favorite folders and go back to recent operations: Read the recent operations again**, 6 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Rename (F2); Type "notes" and Enter; Recent operations (F1, its name and Enter: 2); Down; Escape.
- **Keep favorite folders and go back to recent operations: Undo the newest operation from either list**, 6 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Tab; Ctrl+Z (Undo); Escape; Recent operations (F1, its name and Enter: 2); Escape.
- **Change settings in two windows of one app: Change one setting in each of two windows**, 6 actions (`tests/scripts/axFile/11-two-windows-save.txt`): Show hidden files and folders (F1, its name and Enter: 2); Show files and folders starting with a period (F1, its name and Enter: 2); Close the window (Alt+F4); Close the window (Alt+F4).
- **Browse folders and files: Open a completed path**, 5 actions (`tests/scripts/axFile/01-browse.txt`): Go to folder (F12); Type "{temp}\nav\level-1-enter-me\le..."; Down, Enter; Backspace; Backspace.
- **Browse folders and files: Select a run of entries in a row**, 5 actions (`tests/scripts/axFile/01-browse.txt`): Shift+Down, Shift+Down, Shift+Down, Shift+Up (one move); Information (F8); Down, Shift+End (one move); Information (F8); Deselect all (Ctrl+Shift+A).
- **Copy, move, rename and delete files: Delete a folder to the Recycle Bin**, 5 actions (`tests/scripts/axFile/03-operations.txt`): Home, Down, Down (one move); Delete; Escape; Delete; Enter.
- **Copy, move, rename and delete files: Undo a rename**, 5 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "rename-then" and Enter; Rename (F2); Type "renamed" and Enter; Undo (Ctrl+Z).
- **Copy, move, rename and delete files: Undo a delete**, 5 actions (`tests/scripts/axFile/03-operations.txt`): Find (Ctrl+F); Type "delete-then" and Enter; Delete; Enter; Undo (Ctrl+Z).
- **Use axFile's text, notices and axKit menu: Run a command from the command palette**, 5 actions (`tests/scripts/axFile/05-text-notices-axkit-menu.txt`): Command palette (F1); Type "oth"; Escape; Command palette (F1); Type "refresh" and Enter.

### At four actions, over the ideal three

- **Copy, move, rename and delete files: Undo a compress**, 4 actions (`tests/scripts/axFile/03-operations.txt`): Home; Compress to zip, Context (F1, its name and Enter: 2); Undo (Ctrl+Z).
- **Use axFile's text, notices and axKit menu: Change the text size**, 4 actions (`tests/scripts/axFile/05-text-notices-axkit-menu.txt`): Larger text (Ctrl+Plus); Smaller text (Ctrl+Minus); Windows text size (F1, its name and Enter: 2).
- **Keep favorite folders and go back to recent operations: Open a favorite from the Favorites list**, 4 actions (`tests/scripts/axFile/06-favorites-and-recent-operations.txt`): Tab; Home, Enter; Tab; F8.
- **Move files to a folder you type: Move a file into a folder by typing its name**, 4 actions (`tests/scripts/axFile/07-move-to-folder.txt`): Move to folder, Context (F1, its name and Enter: 2); Type "do"; Down, Up (one move), Enter.
- **Open a recent folder in axFile from the command palette: Open a recent folder in axFile from the command palette**, 4 actions (`tests/scripts/axFile/08-recent-from-the-palette.txt`): Go to folder (F12); Type "{temp}\cellar" and Enter; Command palette (F1); Type "orch" and Enter.

## axGIT

### Over four actions

- **Review and commit changes: Select files with Space and stage them together**, 7 actions (`tests/scripts/axGIT/03-changes.txt`): Space; Shift+Down; Ctrl+Shift+S; Ctrl+Shift+S; Ctrl+Shift+U; Escape; Home.
- **Review and commit changes: Commit the staged changes**, 7 actions (`tests/scripts/axGIT/03-changes.txt`): Up; Ctrl+Shift+S; Ctrl+Enter (Commit); Enter; Type "Test commit" and Enter; Alt+2 (Log); Alt+1 (Changes).
- **Review and commit changes: Discard the changes to a file**, 6 actions (`tests/scripts/axGIT/03-changes.txt`): F5 (Refresh); Home; Delete; Escape; Delete; Enter.
- **Review and commit changes: Discard the changes to the selected files**, 6 actions (`tests/scripts/axGIT/03-changes.txt`): F5 (Refresh); Ctrl+A; Delete; Enter; Delete; Alt+D.
- **Read the history and switch branches: Switch branches**, 6 actions (`tests/scripts/axGIT/04-log-branches.txt`): Alt+3 (Branches); Enter; Down, Enter; Alt+1 (Changes); Alt+3 (Branches); Up, Enter.
- **Read the history and switch branches: Fetch, pull and push**, 6 actions (`tests/scripts/axGIT/04-log-branches.txt`): Ctrl+R (Fetch); Ctrl+L (Pull); Ctrl+L (Pull); F5 (Refresh); Ctrl+P (Push); Ctrl+P (Push).
- **Use axGIT's text, notices and axKit menu: Move by paragraph in a commit's details**, 6 actions (`tests/scripts/axGIT/05-text-notices.txt`): Ctrl+Tab (Switch between the list and the text); Alt+2 (Log); Enter; Ctrl+Down, Ctrl+Down (one move); Ctrl+Tab (Switch between the list and the text); Alt+1 (Changes).
- **Use axGIT's text, notices and axKit menu: Read the keyboard shortcuts and the user guide**, 6 actions (`tests/scripts/axGIT/05-text-notices.txt`): Keyboard shortcuts (F1, its name and Enter: 2); Escape; User guide (F1, its name and Enter: 2); Escape.
- **Agree to the reminder before the first repository opens: Agree to the reminder and open the repository**, 6 actions (`tests/scripts/axGIT/06-consent.txt`): Tab; Enter; Space; Space; Space; Enter.
- **Open a repository in axGIT: Move between the list and the details**, 5 actions (`tests/scripts/axGIT/01-start.txt`): Down; Tab; Shift+Tab; F6 (Switch between the list and the text); Ctrl+Tab (Switch between the list and the text).
- **Review and commit changes: Stage and unstage a file**, 5 actions (`tests/scripts/axGIT/03-changes.txt`): Ctrl+Shift+S; Ctrl+Shift+S; Down, Up (one move); Ctrl+Shift+U; Ctrl+Shift+U.
- **Use axGIT's text, notices and axKit menu: Find a word in a diff**, 5 actions (`tests/scripts/axGIT/05-text-notices.txt`): Enter; Ctrl+F; Type "marker" and Enter; F3; F3.

### At four actions, over the ideal three

- **Open a repository in axGIT: Switch between the lists by their keys**, 4 actions (`tests/scripts/axGIT/01-start.txt`): Alt+1 (Changes); Alt+2 (Log); Alt+3 (Branches); Alt+1 (Changes).
- **Review and commit changes: Stage all and unstage all**, 4 actions (`tests/scripts/axGIT/03-changes.txt`): Stage all (F1, its name and Enter: 2); Unstage all (F1, its name and Enter: 2).
- **Read the history and switch branches: Read a commit of the log**, 4 actions (`tests/scripts/axGIT/04-log-branches.txt`): Alt+2 (Log); Enter; Down, Down, Down, Down, Down, Down (one move); Ctrl+Tab (Switch between the list and the text).
- **Read the history and switch branches: Make a new branch**, 4 actions (`tests/scripts/axGIT/04-log-branches.txt`): Ctrl+N (New branch); Type "scratch-test" and Enter; Alt+3 (Branches); Home, Enter.
- **Use axGIT's text, notices and axKit menu: Change the text size**, 4 actions (`tests/scripts/axGIT/05-text-notices.txt`): Larger text (Ctrl+Plus); Smaller text (Ctrl+Minus); Windows text size (F1, its name and Enter: 2).
- **Agree to the reminder before the first repository opens: Read what was agreed to on this computer**, 4 actions (`tests/scripts/axGIT/06-consent.txt`): Licence (F1, its name and Enter: 2); Escape; Close the window (Alt+F4).
- **Open a repository that runs programs of its own: Trust a repository**, 4 actions (`tests/scripts/axGIT/07-own-programs.txt`): Tab; Tab; Space; Close the window (Alt+F4).

## axHelp

### At four actions, over the ideal three

- **Ask Claude for help from anywhere, with a picture of the screen: Quit axHelp**, 4 actions (`tests/scripts/axHelp/01-start.txt`): Quit axHelp (F1, its name and Enter: 2); Tab; Enter.

## axJSON

### Over four actions

- **Write a list's configuration by choosing: Add a key to an action and choose it**, 18 actions (`tests/scripts/axJSON/07-options.txt`): Ctrl+Home; Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Alt+Left (Add a member); Enter; Choose a value (Alt+Right); End, Enter.
- **Move through a JSON file by its structure: Move by the structure from the Navigation menu**, 12 actions (`tests/scripts/axJSON/02-navigation.txt`): Ctrl+Home; Key, Navigation, Next (Ctrl+K); Object, Navigation, Next (Ctrl+J); Array, Navigation, Next (Ctrl+L); Item, Navigation, Next (Ctrl+I); Item, Navigation, Previous (Ctrl+Shift+I); Array, Navigation, Previous (Ctrl+Shift+L); Object, Navigation, Previous (Ctrl+Shift+J); Object, Navigation, Previous (Ctrl+Shift+J); Key, Navigation, Previous (Ctrl+Shift+K); Error, Navigation, Next (Ctrl+E); Error, Navigation, Previous (Ctrl+Shift+E).
- **Format a JSON file: Repair a single-quoted value**, 10 actions (`tests/scripts/axJSON/03-format.txt`): Ctrl+Home; Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); End; Type ", "q": 'x'"; Ctrl+R (Format).
- **Write a list's configuration by choosing: Choose a value from the values a member can take**, 9 actions (`tests/scripts/axJSON/07-options.txt`): Ctrl+Home; Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Choose a value (Alt+Right); Down, Enter.
- **Save, find, go to a line and wrap in axJSON: Close axJSON when the file has changes**, 8 actions (`tests/scripts/axJSON/06-files-find-wrap.txt`): End; Type ","; Close the window (Alt+F4); Escape; Close the window (Alt+F4); Tab; Tab; Enter.
- **Move through a JSON file by its structure: Move through arrays and their items**, 7 actions (`tests/scripts/axJSON/02-navigation.txt`): Ctrl+Home; Ctrl+L (Array); Ctrl+L (Array); Ctrl+Home; Ctrl+I (Item); Ctrl+I (Item); Ctrl+I (Item).
- **Write a list's configuration by choosing: Choose an item's value through its link to the list's configuration**, 7 actions (`tests/scripts/axJSON/07-options.txt`): Ctrl+Home; Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Alt+Right (Choose a value); Up, Enter.
- **Open a JSON file in axJSON: Reload a file that another program changed**, 6 actions (`tests/scripts/axJSON/01-start.txt`): Down; Type "X"; Ctrl+S (Save); Tab; Tab; Enter.
- **Move through a JSON file by its structure: Move from key to key**, 6 actions (`tests/scripts/axJSON/02-navigation.txt`): Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key); Ctrl+K (Key).
- **Move through a JSON file by its structure: Go to the next and the previous error**, 6 actions (`tests/scripts/axJSON/02-navigation.txt`): Ctrl+Home; Ctrl+E (Error); Ctrl+Home; Ctrl+E (Error); Ctrl+End; Ctrl+Shift+E (Error).
- **Move through a JSON file by its structure: Move back from key to key**, 5 actions (`tests/scripts/axJSON/02-navigation.txt`): Ctrl+Shift+K (Key); Ctrl+Shift+K (Key); Ctrl+Shift+K (Key); Ctrl+Shift+K (Key); Ctrl+Shift+K (Key).
- **Save, find, go to a line and wrap in axJSON: Turn word wrap off and on in axJSON**, 5 actions (`tests/scripts/axJSON/06-files-find-wrap.txt`): Go to line (F12); Type "6" and Enter; Right, Right, Right (one move); Word wrap (F11); Word wrap (F11).
- **Save, find, go to a line and wrap in axJSON: Save a change to a JSON file**, 5 actions (`tests/scripts/axJSON/06-files-find-wrap.txt`): Go to line (F12); Type "4" and Enter; End, Left (one move); Type "0"; Save (Ctrl+S).

### At four actions, over the ideal three

- **Move through a JSON file by its structure: Move to the next object and back to the whole file**, 4 actions (`tests/scripts/axJSON/02-navigation.txt`): Ctrl+Home; Ctrl+J (Object); Ctrl+J (Object); Ctrl+Shift+J (Object).
- **Save, find, go to a line and wrap in axJSON: Find text in a JSON file**, 4 actions (`tests/scripts/axJSON/06-files-find-wrap.txt`): Find (Ctrl+F); Type "apple" and Enter; Find next (F3); Find previous (Shift+F3).

## axMCP

### Over four actions

- **Add, rename and remove a server of your own in axMCP: Add a server of your own**, 11 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): Ctrl+N; F7 (Read in full); Type "probe" and Enter; Type "cmd /c echo"; Tab; Tab; Shift+Tab; Ctrl+Enter; Tab; Down; Escape.
- **Add, rename and remove a server of your own in axMCP: Tick a yes-or-no argument and call the tool**, 9 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): Right; Tab; Down, Down (one move); Space; Up, Enter; Type "hello" and Enter; Ctrl+Enter; Escape; Escape.
- **Add, rename and remove a server of your own in axMCP: Start a server**, 5 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): Type "fake"; Right; Tab; Space; Enter.
- **Add, rename and remove a server of your own in axMCP: Fill in a prompt and fetch it**, 5 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): Enter; Type "Ada" and Enter; Ctrl+Enter; Escape; Close the window (Alt+F4).
- **Find, filter and sort the servers in axMCP: Find a server and go to the next and the previous match**, 5 actions (`tests/scripts/axMCP/04-find-filter-sort.txt`): Find in the list (Ctrl+F); Type "zz" and Enter; Find next (F3); Find next (F3); Find previous (Shift+F3).

### At four actions, over the ideal three

- **Start an MCP server and use its tools: Start a server and open its tools**, 4 actions (`tests/scripts/axMCP/01-start.txt`): Right; Tab; Space; Enter.
- **Start an MCP server and use its tools: Fill in an argument and call the tool**, 4 actions (`tests/scripts/axMCP/01-start.txt`): Enter; Type "hello" and Enter; Ctrl+Enter; Escape.
- **Add, rename and remove a server of your own in axMCP: Rename a server and undo the rename**, 4 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): F2 (Rename); Ctrl+A; Type "probe two" and Enter; Ctrl+Z (Undo).
- **Add, rename and remove a server of your own in axMCP: Remove several of your servers at once and bring them back**, 4 actions (`tests/scripts/axMCP/02-your-servers-errors-prompts.txt`): Ctrl+A; Delete; Enter; Ctrl+Z (Undo).
- **Fill in a number and a choice, and start a server again: Start a server with Enter**, 4 actions (`tests/scripts/axMCP/03-numbers-choices-restart-agreement.txt`): Enter; Tab; Space; Enter.
- **Find, filter and sort the servers in axMCP: Show only the servers whose name contains a text**, 4 actions (`tests/scripts/axMCP/04-find-filter-sort.txt`): Filter (F1, its name and Enter: 2); Type "zz" and Enter; Ctrl+Delete (Clear the filter).
- **Find, filter and sort the servers in axMCP: Put the servers in the order of their names**, 4 actions (`tests/scripts/axMCP/04-find-filter-sort.txt`): Name, Options, Sort by (F1, its name and Enter: 2); The list's own order, Options, Sort by (F1, its name and Enter: 2).

## axMedia

### Over four actions

- **Build and move through a play queue: Find a file by part of its name**, 11 actions (`tests/scripts/axMedia/01-queue.txt`): Ctrl+F (Find in the list); Type "elt" and Enter; Ctrl+F (Find in the list); Type "ta.w" and Enter; F3 (Find next); F3 (Find next); Ctrl+F (Find in the list); Type "a.wav" and Enter; Shift+F3 (Find previous); Shift+F3 (Find previous); F3 (Find next).
- **Play, pause and seek: Change the playing speed**, 10 actions (`tests/scripts/axMedia/03-playing.txt`): Alt+Up (Faster); Alt+Up (Faster); Faster (Alt+Up); Alt+Down (Slower); Slower (Alt+Down); Alt+Up (Faster); Normal speed (F1, its name and Enter: 2); Faster (Alt+Up); Faster (Alt+Up).
- **Build and move through a play queue: Remove files and clear the queue**, 6 actions (`tests/scripts/axMedia/01-queue.txt`): Delete; Up; Delete; Clear queue (F1, its name and Enter: 2); F7 (What is playing).
- **Use axMedia's text, notices and axKit menu: Read the keyboard shortcuts and the user guide**, 6 actions (`tests/scripts/axMedia/04-text-notices.txt`): Keyboard shortcuts (F1, its name and Enter: 2); Escape (Deselect all); User guide (F1, its name and Enter: 2); Escape (Deselect all).
- **Filter and sort axMedia's queue: Put the queue in the order of the files' names**, 6 actions (`tests/scripts/axMedia/09-filter-the-queue.txt`): Name, Options, Sort by (F1, its name and Enter: 2); Home, Enter; The order added, Options, Sort by (F1, its name and Enter: 2); Close the window (Alt+F4).
- **Play, pause and seek: Move forward, back and to a time**, 5 actions (`tests/scripts/axMedia/03-playing.txt`): Ctrl+Enter (Play, pause or resume); Ctrl+Right, Ctrl+Left (one move); F12 (Go to time); Type "0:30" and Enter; Ctrl+Enter (Play, pause or resume).
- **Use axMedia's Context menu: Hear a file's information from the Context menu**, 5 actions (`tests/scripts/axMedia/05-context-menu.txt`): Information, Context (F1, its name and Enter: 2); Down; Information, Context (F1, its name and Enter: 2).

### At four actions, over the ideal three

- **Play, pause and seek: Play, pause and resume**, 4 actions (`tests/scripts/axMedia/03-playing.txt`): Ctrl+Enter (Play, pause or resume); Ctrl+Enter (Play, pause or resume); Ctrl+Enter (Play, pause or resume); Ctrl+Enter (Play, pause or resume).
- **Use axMedia's text, notices and axKit menu: Change the text size**, 4 actions (`tests/scripts/axMedia/04-text-notices.txt`): Larger text (Ctrl+Plus); Smaller text (Ctrl+Minus); Windows text size (F1, its name and Enter: 2).
- **Select files in the queue and take several out: Select files with Space and with Shift and an arrow**, 4 actions (`tests/scripts/axMedia/07-selection.txt`): Down; Space; Shift+Down; F8.
- **Select files in the queue and take several out: Select every file, and give the selection up**, 4 actions (`tests/scripts/axMedia/07-selection.txt`): Ctrl+A (Select all); Escape (Deselect all); Shift+Space; Ctrl+Shift+A (Deselect all).

## axPass

### Over four actions

- **Read an entry in full and come back to the same database: Choose when axPass locks by itself**, 7 actions (`tests/scripts/axPass/03-read-and-reopen.txt`): Ctrl+L (Lock); Escape; 5 minutes (F1, its name and Enter: 2); Never (F1, its name and Enter: 2); Close the window (Alt+F4).
- **Unlock a password database and change an entry: Move an entry to another group**, 6 actions (`tests/scripts/axPass/01-unlock-read-change.txt`): Home, Right (one move); Type "g"; Ctrl+M; Down, Enter; Ctrl+Z (Undo); Left.
- **Agree to the reminder before the first unlock: Agree to the reminder and unlock the database**, 6 actions (`tests/scripts/axPass/02-consent.txt`): Tab; Enter; Space; Enter; Type "test-master-1" and Enter; Close the window (Alt+F4).
- **Change an entry's fields, keep a backup and delete for good: Write an entry's notes over two lines**, 6 actions (`tests/scripts/axPass/04-fields-backups-recycle-bin.txt`): Enter; End, Enter; Type "First line of the notes" and Enter; Type "Second line"; Tab; Enter.
- **Unlock a password database and change an entry: Set a generated password**, 5 actions (`tests/scripts/axPass/01-unlock-read-change.txt`): Up; Tab; Home, Down, Down (one move), Enter; Alt+G; Enter.
- **Find, filter, sort and select entries of a password database: Find an entry and go to the next and the previous match**, 5 actions (`tests/scripts/axPass/05-find-filter-sort-select.txt`): Find in the list (Ctrl+F); Type "bank" and Enter; Find next (F3); Find next (F3); Find previous (Shift+F3).

### At four actions, over the ideal three

- **Unlock a password database and change an entry: Move between the entries and the fields**, 4 actions (`tests/scripts/axPass/01-unlock-read-change.txt`): Ctrl+Tab (Switch between the navigator and the detail pane); Ctrl+Shift+Tab (Switch between the navigator and the detail pane); F6 (Switch between the navigator and the detail pane); F6 (Switch between the navigator and the detail pane).
- **Unlock a password database and change an entry: Move several entries to the Recycle Bin at once and undo it**, 4 actions (`tests/scripts/axPass/01-unlock-read-change.txt`): Ctrl+A; Delete; Ctrl+Z (Undo); End.
- **Find, filter, sort and select entries of a password database: Show only the entries whose name contains a text**, 4 actions (`tests/scripts/axPass/05-find-filter-sort-select.txt`): Filter (F1, its name and Enter: 2); Type "bank" and Enter; Ctrl+Delete (Clear the filter).
- **Find, filter, sort and select entries of a password database: Put the entries in the order of their names**, 4 actions (`tests/scripts/axPass/05-find-filter-sort-select.txt`): Name, Options, Sort by (F1, its name and Enter: 2); The list's own order, Options, Sort by (F1, its name and Enter: 2).

## axPreview

### Over four actions

- **Preview a text file: Move by headings, links, lists, quotes and tables**, 14 actions (`tests/scripts/axPreview/01-start-and-the-text.txt`): Ctrl+Home; Ctrl+H (Heading); Ctrl+Shift+H (Heading); Ctrl+2 (Heading level 2); Ctrl+2 (Heading level 2); Ctrl+Home; Ctrl+K (Link); Ctrl+K (Link); Ctrl+L (List); Ctrl+I (List item); Ctrl+Q (Block quote); Ctrl+Q (Block quote); Ctrl+T (Table); Ctrl+T (Table).
- **Preview a text file: Find a word in the text**, 6 actions (`tests/scripts/axPreview/01-start-and-the-text.txt`): Ctrl+Home; Ctrl+F (Find); Type "update" and Enter; F3 (Find next); F3 (Find next); Shift+F3 (Find previous).
- **Use axPreview's text, notices and axKit menu: Read the user guide**, 5 actions (`tests/scripts/axPreview/03-text-notices-axkit-menu.txt`): User guide (F1, its name and Enter: 2); Ctrl+H (Heading); Ctrl+H (Heading); Escape.

### At four actions, over the ideal three

- **Preview a text file: Go to a line by its number**, 4 actions (`tests/scripts/axPreview/01-start-and-the-text.txt`): Ctrl+Home, Down (one move); L; F12 (Go to line); Type "5" and Enter.
- **Preview a text file: Turn word wrap off and on**, 4 actions (`tests/scripts/axPreview/01-start-and-the-text.txt`): F12 (Go to line); Type "5" and Enter; F11 (Word wrap); F11 (Word wrap).
- **Preview a text file: Read the keyboard shortcuts**, 4 actions (`tests/scripts/axPreview/01-start-and-the-text.txt`): Keyboard shortcuts (F1, its name and Enter: 2); Ctrl+H (Heading); Escape.

## axProcess

### Over four actions

- **End a process with the processes it started: Find a process and go to the next and the previous match**, 8 actions (`tests/scripts/axProcess/05-end-process-tree-find-next.txt`): Find in the list (Ctrl+F); Type "axkitscript05" and Enter; Find next (F3); Find next (F3); Find next (F3); Find previous (Shift+F3); Find previous (Shift+F3); Find previous (Shift+F3).
- **Find and end a process: Sort the processes by name, processor or memory**, 6 actions (`tests/scripts/axProcess/01-start.txt`): Name (F1, its name and Enter: 2); Processor (F1, its name and Enter: 2); Memory (F1, its name and Enter: 2).
- **Find a process by typing its name and read a startup program's details: Find a process by typing its name**, 5 actions (`tests/scripts/axProcess/03-type-to-find-own-window-startup.txt`): Name (F1, its name and Enter: 2); Home; Type "svc"; Type "s".
- **Turn a startup program off, on again, or remove it: Turn a Startup folder shortcut off and on again from its details**, 5 actions (`tests/scripts/axProcess/04-startup-turn-off-remove.txt`): Down; Turn off (F1, its name and Enter: 2); Enter; Alt+N.

### At four actions, over the ideal three

- **Find and end a process: Find a process and read its details**, 4 actions (`tests/scripts/axProcess/01-start.txt`): Ctrl+F (Find in the list); Type "axkitscript01" and Enter; Enter; Escape (Deselect all).
- **Manage services and startup programs: Read a service's details**, 4 actions (`tests/scripts/axProcess/02-services-startup.txt`): Ctrl+F (Find in the list); Type "(EventLog)" and Enter; Enter; Escape (Deselect all).
- **Manage services and startup programs: Go up to This computer and into another list**, 4 actions (`tests/scripts/axProcess/02-services-startup.txt`): Alt+1 (Processes); Left, Left, Down, Right (one move); Backspace; End, Enter.
- **Select several processes or startup programs and end or remove them together: Select two processes with Space and Shift+Down**, 4 actions (`tests/scripts/axProcess/06-select-several.txt`): Find in the list (Ctrl+F); Type "axkitscript06" and Enter; Space; Shift+Down.
- **Select several processes or startup programs and end or remove them together: Select two startup programs and remove them together**, 4 actions (`tests/scripts/axProcess/06-select-several.txt`): Alt+3 (Startup); Space; Shift+Down; Delete.

## axStart

### Over four actions

- **Hear that nothing matches and turn the spoken match off: Turn the spoken first match off and on**, 8 actions (`tests/scripts/axStart/03-no-match-and-options.txt`): Say the first match while typing (F1, its name and Enter: 2); Ctrl+A; Type "calculato"; Information (F8); Say the first match while typing (F1, its name and Enter: 2); Backspace.
- **Find out whether a key arrives and what it does, in a hotkey window: Test what a key does**, 8 actions (`tests/scripts/axStart/04-key-test.txt`): Test a key (F1, its name and Enter: 2); F9 (Say the status); F5 (Refresh programs); F8 (Information); Down, Enter; A; Shift+F9.
- **Search programs as you type and change axStart's hotkey: Change the hotkey**, 5 actions (`tests/scripts/axStart/02-typing-and-the-hotkey.txt`): Hotkey (F1, its name and Enter: 2); A; Ctrl+Alt+Shift+F9; Enter.

### At four actions, over the ideal three

- **Find and start a program: Jump to the first or last match, or by a page**, 4 actions (`tests/scripts/axStart/01-basics.txt`): Last match (Ctrl+End); Down; First match (Ctrl+Home); Page Down, Page Up (one move).

## axTask

### Over four actions

- **Change an item in its form: Change several fields of an item and save them as one step**, 18 actions (`tests/scripts/axTask/18-item-form.txt`): Enter; Tab; Space; Tab; Ctrl+A (Select all); Type "2099-11-02"; Tab; Down, Down (one move); Tab; Ctrl+A (Select all); Type "Hall"; Tab; Type "First line" and Enter; Type "Second line"; Tab; Type "Washer"; Ctrl+Enter (Done or not done); Undo (Ctrl+Z).
- **Move through tasks and change an item's fields: Move among the lists by their headings**, 12 actions (`tests/scripts/axTask/01-navigate-and-fields.txt`): Previous section (Ctrl+Up); Next section (Ctrl+Down); Down; Next heading level 1 (Ctrl+1); Down, Down, Down, Down (one move); Next heading level 1 (Ctrl+1); Next heading level 2 (Ctrl+2); Next section (Ctrl+Down); Previous section (Ctrl+Up); Previous heading level 1 (Ctrl+Shift+1); Previous heading level 1 (Ctrl+Shift+1); Right, Down, Down (one move).
- **Make lists, link items and use smart lists: Make a checklist a plain list and back**, 12 actions (`tests/scripts/axTask/02-lists-links-smart.txt`): Left; Remove Done from every item (F1, its name and Enter: 2); Undo (Ctrl+Z); Remove Done from every item (F1, its name and Enter: 2); Remove Done from every item (F1, its name and Enter: 2); Add Done to every item (F1, its name and Enter: 2); Add Done to every item (F1, its name and Enter: 2).
- **Change an item in its form: Rename an item in its form**, 12 actions (`tests/scripts/axTask/18-item-form.txt`): Enter; Tab; Tab; Tab; Tab; Tab; Tab; Type "Washer"; Ctrl+Enter (Done or not done); Down, Enter; Ctrl+A (Select all); Type "Washer 15 mm" and Enter.
- **Add project folders and search every list: Mark a project's task done from All tasks**, 11 actions (`tests/scripts/axTask/05-projects.txt`): Down, Right, Down (one move); Ctrl+Enter (Done or not done); Undo (Ctrl+Z); Left; Next heading level 1 (Ctrl+1); End; Previous heading level 1 (Ctrl+Shift+1); Previous heading level 1 (Ctrl+Shift+1); Previous heading level 1 (Ctrl+Shift+1); Next heading level 2 (Ctrl+2); Down.
- **Set or clear fields of several items at once: Set fields of the selected items**, 11 actions (`tests/scripts/axTask/28-set-and-clear-fields.txt`): Space; Down; Space; Set fields (F1, its name and Enter: 2); Tab; Down; Tab; Type "Garage"; Ctrl+Enter (Done or not done); Undo (Ctrl+Z).
- **Find out whether a key arrives and what it does, in an item window: Test what a key does**, 10 actions (`tests/scripts/axTask/09-key-test.txt`): Test a key (F1, its name and Enter: 2); F9 (Say the status); F1 (Command palette); Ctrl+Period; Ctrl+Slash (Ask about this window); Ctrl+N (New item); Ctrl+Up, Left, Down (one move); W; Shift+F9.
- **Give a list its configuration: Give a list its configuration**, 10 actions (`tests/scripts/axTask/17-list-without-configuration.txt`): Left; Tab; End; Shift+Tab; Edit the list's configuration (Ctrl+J); Right; Tab; Down, Down, Down (one move); Shift+Tab; Ctrl+Enter (Done or not done).
- **Change an item in its form: Open an item's form and move through its fields**, 10 actions (`tests/scripts/axTask/18-item-form.txt`): Enter; F7 (Read in full); Tab; Tab; Tab; Tab; Tab; Tab; Tab; Tab.
- **Select several items and act on all of them: Mark several tasks done at once**, 10 actions (`tests/scripts/axTask/20-selection.txt`): Home; Space; Down; Space; End; Space; Ctrl+Enter (Done or not done); Ctrl+Enter (Done or not done); Undo (Ctrl+Z); Undo (Ctrl+Z).
- **Hear what kind of thing opened: Turn the entry sounds off and on**, 10 actions (`tests/scripts/axTask/21-what-opens.txt`): Entry sounds (F1, its name and Enter: 2); Keyboard shortcuts (F1, its name and Enter: 2); Escape; Entry sounds (F1, its name and Enter: 2); Keyboard shortcuts (F1, its name and Enter: 2); Escape.
- **Add project folders and search every list: Add a project folder by its path**, 9 actions (`tests/scripts/axTask/05-projects.txt`): Add a project folder (F1, its name and Enter: 2); Type "{temp}\nowhere" and Enter; Ctrl+A (Select all); Type "{temp}\tasks" and Enter; Ctrl+A (Select all); Type "{temp}\more" and Enter; Refresh (F5); Down.
- **Add project folders and search every list: Search every list**, 9 actions (`tests/scripts/axTask/05-projects.txt`): Find in every list (Ctrl+Shift+F); Type "zebra" and Enter; Find in every list (Ctrl+Shift+F); Ctrl+A (Select all); Type "TAP" and Enter; Down; Tab; Shift+Tab; Escape.
- **Give a list its configuration: Add an item to a list that has no configuration yet**, 9 actions (`tests/scripts/axTask/17-list-without-configuration.txt`): Left; Type "em"; Right; New item (Ctrl+N); Type "First thing" and Enter; Tab; End; Shift+Tab; Undo (Ctrl+Z).
- **Run the actions of a list: Put a command into axTerminal's command field**, 9 actions (`tests/scripts/axTask/19-list-actions.txt`): Look in the terminal (F1, its name and Enter: 2); Enter; Look in the terminal (F1, its name and Enter: 2); Tab; Tab; Enter; Close the window (Alt+F4).
- **Move through tasks and change an item's fields: Move between the list and the fields**, 7 actions (`tests/scripts/axTask/01-navigate-and-fields.txt`): Escape; Tab; Shift+Tab; Switch between the navigator and the detail pane (Ctrl+Tab); F6 (Switch between the navigator and the detail pane); Ctrl+Shift+Tab (Switch between the navigator and the detail pane); Escape.
- **Change an item in its form: Open the form on a field from the detail pane**, 7 actions (`tests/scripts/axTask/18-item-form.txt`): Up; Tab; Down, Down, Down, Down (one move), Enter; Ctrl+A (Select all); Type "Bathroom" and Enter; Home, Down (one move); Space.
- **Run the actions of a list: Fill in an action's form**, 7 actions (`tests/scripts/axTask/19-list-actions.txt`): Find what I type (F1, its name and Enter: 2); Enter; Type "the tap" and Enter; Tab; Tab; Enter.
- **Set or clear fields of several items at once: Clear fields of the selected items**, 7 actions (`tests/scripts/axTask/28-set-and-clear-fields.txt`): Clear fields (F1, its name and Enter: 2); Ctrl+Enter (Done or not done); End; Space; Ctrl+Enter (Done or not done); Undo (Ctrl+Z).
- **Make lists, link items and use smart lists: Write notes of several lines**, 6 actions (`tests/scripts/axTask/02-lists-links-smart.txt`): Up, Enter; Type "First line" and Enter; Type "Second line"; Tab; Enter; Read in full (F7).
- **Group lists into sections: Move among the sections by their headings**, 6 actions (`tests/scripts/axTask/03-sections.txt`): Previous section (Ctrl+Up); Next section (Ctrl+Down); Next section (Ctrl+Down); Next heading level 1 (Ctrl+1); Next heading level 1 (Ctrl+1); Previous heading level 2 (Ctrl+Shift+2).
- **Group lists into sections: Put a list in a section from its details**, 6 actions (`tests/scripts/axTask/03-sections.txt`): Previous section (Ctrl+Up); Down; Tab; Enter; Type "Jobs" and Enter; Escape.
- **Add project folders and search every list: Open a project and mark a task done in it**, 6 actions (`tests/scripts/axTask/05-projects.txt`): Right; Next section (Ctrl+Down); Down, Home, Down, Right (one move); Ctrl+Enter (Done or not done); Undo (Ctrl+Z); Left, Left, Left (one move).
- **Group smart lists and find items by tag: Find the smart lists that share a first word**, 6 actions (`tests/scripts/axTask/06-smart-tags.txt`): Previous heading level 2 (Ctrl+Shift+2); Previous heading level 2 (Ctrl+Shift+2); Down, Down (one move); Previous heading level 1 (Ctrl+Shift+1); Down, Down (one move); Previous heading level 3 (Ctrl+Shift+3).
- **Convert a list written in axTask's earlier format: Convert every list of the earlier format at once**, 6 actions (`tests/scripts/axTask/16-convert-earlier-format.txt`): Enter; Tab; Tab; Enter; Down; All tasks (Alt+0).
- **Run the actions of a list: Run a program on an item, and hear how it ends**, 6 actions (`tests/scripts/axTask/19-list-actions.txt`): Action key Shift+4 (Ctrl+Shift+4); Tab; Tab; Tab; Enter; Action key Shift+4 (Ctrl+Shift+4).
- **Select several items and act on all of them: Archive several items at once**, 6 actions (`tests/scripts/axTask/20-selection.txt`): Home; Space; Down; Space; Delete; Undo (Ctrl+Z).
- **Select several items and act on all of them: Move the selected items to a list chosen in a form**, 6 actions (`tests/scripts/axTask/20-selection.txt`): Space; Down; Space; Ctrl+M; Enter; Undo (Ctrl+Z).
- **Filter and sort a list of axTask: Put a list's items in the order of their names**, 6 actions (`tests/scripts/axTask/22-filter-and-sort.txt`): Right; Name, Options, Sort by (F1, its name and Enter: 2); The list's own order, Options, Sort by (F1, its name and Enter: 2); Close the window (Alt+F4). Not counted, the example: Type "h".
- **Move through tasks and change an item's fields: Open an item's fields**, 5 actions (`tests/scripts/axTask/01-navigate-and-fields.txt`): Enter; Enter; Escape; Tab; Down, Down, Down (one move).
- **Move through tasks and change an item's fields: Add an item**, 5 actions (`tests/scripts/axTask/01-navigate-and-fields.txt`): Left; Type "s"; Right; New item (Ctrl+N); Type "Bread" and Enter.
- **Make lists, link items and use smart lists: Link one item to another**, 5 actions (`tests/scripts/axTask/02-lists-links-smart.txt`): Escape; Right; Tab; End, Enter; Type "Washer" and Enter.
- **Add project folders and search every list: Find the project folders by their headings**, 5 actions (`tests/scripts/axTask/05-projects.txt`): Next heading level 1 (Ctrl+1); Next heading level 2 (Ctrl+2); Down, Down (one move); Next heading level 2 (Ctrl+2); Next heading level 1 (Ctrl+1).
- **Rename a list, and change the fields of its items: Take a field out of a list and keep its values**, 5 actions (`tests/scripts/axTask/12-rename-list-and-fields.txt`): Refresh (F5); Right; Tab; Down, Down (one move), Enter; Escape.
- **Convert a list written in axTask's earlier format: Convert a list of the earlier format**, 5 actions (`tests/scripts/axTask/16-convert-earlier-format.txt`): Type "ho"; Right; Escape; Enter; Enter.
- **Run the actions of a list: Stop a running action**, 5 actions (`tests/scripts/axTask/19-list-actions.txt`): Wait (F1, its name and Enter: 2); Tab; Tab; Enter.
- **Select several items and act on all of them: Select items one by one with Space**, 5 actions (`tests/scripts/axTask/20-selection.txt`): Space; Down, Down (one move); Space; Space; Space.
- **Select several items and act on all of them: Move every selected item to another list with an action of the list**, 5 actions (`tests/scripts/axTask/20-selection.txt`): Action key 2 (Ctrl+2); Action key 2 (Ctrl+2); Ctrl+Enter (Done or not done); Undo (Ctrl+Z); Undo (Ctrl+Z).
- **Put a list in your own order: Put new items at the top of a list**, 5 actions (`tests/scripts/axTask/27-list-order.txt`): New items at the top of this list (F1, its name and Enter: 2); Ctrl+N (New item); Type "Dates" and Enter; Home.
- **Keep a history of a list's changes and read an item's: Read what changed in an item**, 5 actions (`tests/scripts/axTask/29-history.txt`): Ctrl+Enter (Done or not done); Undo (Ctrl+Z); History (F1, its name and Enter: 2); Escape.

### At four actions, over the ideal three

- **Move through tasks and change an item's fields: Set a due date by typing a weekday**, 4 actions (`tests/scripts/axTask/01-navigate-and-fields.txt`): Tab; Enter; Ctrl+A (Select all); Type "friday" and Enter.
- **Group lists into sections: Move a list into a new section**, 4 actions (`tests/scripts/axTask/03-sections.txt`): Move to section (Ctrl+M); End, Enter; Type "Jobs" and Enter; Up. Not counted, the example: Type "w".
- **Group lists into sections: Move a list back to Your lists**, 4 actions (`tests/scripts/axTask/03-sections.txt`): Previous section (Ctrl+Up); Type "h"; Move to section (Ctrl+M); Home, Enter.
- **Group lists into sections: Archive a list and undo it**, 4 actions (`tests/scripts/axTask/03-sections.txt`): Left; Delete; End, Up (one move); Undo (Ctrl+Z).
- **Act on items with action keys: Run an action key on an item**, 4 actions (`tests/scripts/axTask/04-action-keys.txt`): Left, Home, Down, Right (one move); Action key 3 (Ctrl+3); Action key 1 (Ctrl+1); Undo (Ctrl+Z).
- **Add project folders and search every list: Remove a project folder**, 4 actions (`tests/scripts/axTask/05-projects.txt`): 1. projects (F1, its name and Enter: 2); 1. more (F1, its name and Enter: 2).
- **Group smart lists and find items by tag: Move among the tags by their symbols**, 4 actions (`tests/scripts/axTask/06-smart-tags.txt`): Next heading level 3 (Ctrl+3); Down; Next heading level 3 (Ctrl+3); Down.
- **Group smart lists and find items by tag: Gather a tag's items from every list**, 4 actions (`tests/scripts/axTask/06-smart-tags.txt`): Right, Down, Left (one move); Next heading level 3 (Ctrl+3); Down; Next heading level 3 (Ctrl+3).
- **Close axTask and come back to the list you had: Come back to the list you had**, 4 actions (`tests/scripts/axTask/08-reopen.txt`): Right, Down (one move); Close the window (Alt+F4); Left; Close the window (Alt+F4). Not counted, the example: Type "s".
- **Rename a list, and change the fields of its items: Read which fields a list's items have**, 4 actions (`tests/scripts/axTask/12-rename-list-and-fields.txt`): Tab; End, Enter; Escape; Shift+Tab.
- **Rename a list, and change the fields of its items: Put a field back and find its values again**, 4 actions (`tests/scripts/axTask/12-rename-list-and-fields.txt`): Escape; Refresh (F5); Tab; Home, Down, Down, End (one move).
- **Set a date in words, and add an item with its list's fields: Add an item with its list's fields**, 4 actions (`tests/scripts/axTask/13-date-in-words-and-new-item-fields.txt`): Ctrl+N (New item); Type "Paint the door" and Enter; Tab; Down, Down, Down (one move).
- **Make a smart list of your own: Make a smart list and see its file open in axJSON**, 4 actions (`tests/scripts/axTask/14-new-smart-list.txt`): New smart list (F1, its name and Enter: 2); Type "Soon" and Enter; Close the window (Alt+F4).
- **Approve or decline a report from outside with the approval passphrase: A wrong passphrase approves nothing**, 4 actions (`tests/scripts/axTask/15-approve-outside-report.txt`): Approve this report (F1, its name and Enter: 2); Tab; Type "not the passphrase at all" and Enter.
- **Approve or decline a report from outside with the approval passphrase: Approve an entry with the passphrase**, 4 actions (`tests/scripts/axTask/15-approve-outside-report.txt`): Approve this report (F1, its name and Enter: 2); Tab; Type "a long test passphrase" and Enter.
- **Select several items and act on all of them: Move an item to a new list**, 4 actions (`tests/scripts/axTask/20-selection.txt`): Ctrl+M; End, Enter; Type "Someday" and Enter; Undo (Ctrl+Z).
- **Hear what kind of thing opened: Hear that a field to type in opened**, 4 actions (`tests/scripts/axTask/21-what-opens.txt`): Ctrl+F (Find in the list); Escape; F2 (Rename); Escape.

## axTemplate

### Over four actions

- **Delete several templates, and meet a file that is no template, in axTemplate's browser: Archive several templates at once**, 7 actions (`tests/scripts/axTemplate/02-several-and-not-a-template.txt`): Home; Space; Down; Space; Delete; Enter; Ctrl+Z (Undo).
- **Make, write, rename and delete a template in axTemplate's browser: Rename a template from its Title field**, 5 actions (`tests/scripts/axTemplate/01-start.txt`): Up, Enter; Ctrl+A (Select all); Type "Greeting"; Ctrl+Enter (Put in, with that program's selection); Escape.

### At four actions, over the ideal three

- **Make, write, rename and delete a template in axTemplate's browser: Write the text of a template in its form**, 4 actions (`tests/scripts/axTemplate/01-start.txt`): Enter; Tab; Type "Thank you, {clipboard}.{caret}"; Ctrl+Enter (Put in, with that program's selection).
- **Find a template in the launcher and use it: Find a template by a word of its name or its text**, 4 actions (`tests/scripts/axTemplate/03-launcher.txt`): Down, Up (one move); Ctrl+A (Select all); Type "zzz"; Down. Not counted, the example: Type "sprint".
- **Find a template in the launcher and use it: Read a template before using it**, 4 actions (`tests/scripts/axTemplate/03-launcher.txt`): Ctrl+A (Select all); Type "greet"; Read in full (F7); Escape.
- **Find a template in the launcher and use it: Copy a template without putting it in**, 4 actions (`tests/scripts/axTemplate/03-launcher.txt`): Ctrl+A (Select all); Type "thanks"; Copy without putting in (Shift+Enter); Down, Up (one move).

## axTerminal

### Over four actions

- **Add a shell of your own: Add a shell of your own**, 13 actions (`tests/scripts/axTerminal/08-user-shells.txt`): Add a shell (F1, its name and Enter: 2); F7 (What is running); Enter; Type "Command Prompt" and Enter; Ctrl+A; Type "Test cmd" and Enter; Type "cmd.exe /k"; Tab; Tab; Shift+Tab; Shift+Tab; Ctrl+Enter (Send the command).
- **Switch shells and folders: Start a new session in another shell**, 12 actions (`tests/scripts/axTerminal/03-shells-and-folders.txt`): New session (Ctrl+N); Tab; Tab; Tab; Tab; Tab; Tab; Tab; Down, Enter; Terminal (Alt+2); Command field (Alt+1); Type "echo hello" and Enter.
- **Run commands and read their output: Turn the heading marks in the text off and on**, 9 actions (`tests/scripts/axTerminal/02-commands.txt`): Markers in the text (F1, its name and Enter: 2); Shift+I; Down, Up (one move); H; Markers in the text (F1, its name and Enter: 2); Shift+I; Command field (Alt+1).
- **Run commands and read their output: Send a command without recording it**, 8 actions (`tests/scripts/axTerminal/02-commands.txt`): Send without recording (F1, its name and Enter: 2); Type "Start-Sleep 5"; Send without recording (F1, its name and Enter: 2); What is running (F7); Recent commands (Alt+Down); Escape. Not counted, the example: Type "echo secret".
- **Switch shells and folders: Restart the shell**, 8 actions (`tests/scripts/axTerminal/03-shells-and-folders.txt`): Restart shell (Ctrl+Shift+R); Terminal (Alt+2); Command field (Alt+1); Type "ping -n 30 127.0.0.1" and Enter; Restart shell (Ctrl+Shift+R); Escape; Restart shell (Ctrl+Shift+R); Enter.
- **Run commands and read their output: Run a command and read its output by headings**, 7 actions (`tests/scripts/axTerminal/02-commands.txt`): Latest output (Alt+3); Down, Down (one move); Shift+I; Down, Down, Down, Down (one move); Shift+I; 2; 2. Not counted, the example: Type "Write-Output hello"; Enter.
- **Run commands and read their output: Copy a command line of the terminal into the field**, 7 actions (`tests/scripts/axTerminal/02-commands.txt`): Latest output (Alt+3); Shift+I; 2; 2; Down, Enter; Ctrl+A; Delete.
- **Add a shell of your own: Switch to a shell of your own**, 7 actions (`tests/scripts/axTerminal/08-user-shells.txt`): 1. Test cmd (F1, its name and Enter: 2); Terminal (Alt+2); Command field (Alt+1); Type "echo hello-user-shell" and Enter; Terminal (Alt+2); Command field (Alt+1).
- **Run commands and read their output: Put a recent command back in the field**, 6 actions (`tests/scripts/axTerminal/02-commands.txt`): Recent commands (Alt+Down); Down, Enter; Ctrl+A; Delete; Recent commands (Alt+Down); Escape.
- **Use axTerminal's text, notices and axKit menu: Close the window while a command runs**, 6 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): Command field (Alt+1); Type "ping -n 30 127.0.0.1" and Enter; Alt+F4; Escape; Close the window (Alt+F4); Enter.
- **Agree to the reminder before a shell first starts: Agree to the reminder and start the shell**, 6 actions (`tests/scripts/axTerminal/07-consent.txt`): Tab; Enter; Space; Space; Space; Enter.
- **Use a full-screen program: Read the screen and go back to the program**, 6 actions (`tests/scripts/axTerminal/14-screen-view.txt`): Ctrl+Space; Ctrl+Home; Alt+1 (Command field); Ctrl+F (Find); Type "More" and Enter; Ctrl+Space.
- **Use axTerminal's text, notices and axKit menu: Set a bookmark and go back to it**, 5 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): Latest output (Alt+3); Down, Down (one move); M; Ctrl+Home; K.

### At four actions, over the ideal three

- **Start a terminal: Move between the command field and the terminal**, 4 actions (`tests/scripts/axTerminal/01-startup.txt`): Terminal (Alt+2); Terminal (Alt+2); Command field (Alt+1); Command field (Alt+1).
- **Hear a command's output as it arrives: Turn the word Done off**, 4 actions (`tests/scripts/axTerminal/02b-speak-output.txt`): Announce "Done" (F1, its name and Enter: 2); Type "Write-Output again" and Enter; Type "cmd /c exit 3" and Enter.
- **Switch shells and folders: Switch to another shell**, 4 actions (`tests/scripts/axTerminal/03-shells-and-folders.txt`): Windows PowerShell (F1, its name and Enter: 2); Terminal (Alt+2); Command field (Alt+1).
- **Switch shells and folders: Start the shell again after it ended**, 4 actions (`tests/scripts/axTerminal/03-shells-and-folders.txt`): Terminal (Alt+2); Command field (Alt+1); Type "echo kept" and Enter; Restart shell (Ctrl+Shift+R). Not counted, the example: Type "exit"; Enter.
- **Use axTerminal's text, notices and axKit menu: Clear the window**, 4 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): Clear transcript (Ctrl+Shift+Delete); Command field (Alt+1); Type "echo first" and Enter; Terminal (Alt+2).
- **Use axTerminal's text, notices and axKit menu: Find text in the terminal**, 4 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): Ctrl+Home; Find (Ctrl+F); Type "first" and Enter; Find next (F3).
- **Use axTerminal's text, notices and axKit menu: Read the keyboard shortcuts**, 4 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): Keyboard shortcuts (F1, its name and Enter: 2); H; Escape.
- **Use axTerminal's text, notices and axKit menu: Read the user guide**, 4 actions (`tests/scripts/axTerminal/04-text-notices-menu.txt`): User guide (F1, its name and Enter: 2); H; Escape.
- **Repeat an earlier command: Go back to edit mode in the field**, 4 actions (`tests/scripts/axTerminal/06-history-keys.txt`): Command field (Alt+1); Ctrl+Space; End; Type "!".
- **Use axTerminal's Context menu: Run a Context command from the palette**, 4 actions (`tests/scripts/axTerminal/09-context-menu.txt`): Command palette (F1); Type "bookmark this" and Enter; Command palette (F1); Type "copy this line" and Enter.
- **Start a shell in a recent folder from the command palette: Start a shell in a recent folder from the command palette**, 4 actions (`tests/scripts/axTerminal/10-recent-from-the-palette.txt`): Close the window (Alt+F4); Command palette (F1); Type "orch" and Enter; Say current folder (F8).
- **Use a full-screen program: Get the terminal back from a program that ends on the whole screen**, 4 actions (`tests/scripts/axTerminal/14-screen-view.txt`): Command field (Alt+1); Type "[Console]::Write([char]27 + '[..." and Enter; Type "echo ('after' + '-screen')" and Enter; Terminal (Alt+2).

## mcp

### At four actions, over the ideal three

- **Let an AI tool read axKit, once you allow it: Allow an AI tool to use axKit**, 4 actions (`tests/scripts/mcp/01-read-and-pair.txt`): Tab; Space; Tab; Enter.
