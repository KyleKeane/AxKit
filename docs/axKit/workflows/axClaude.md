# axClaude: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence. The file names and the words
typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's
in this version of axKit.

## Start a conversation with Claude

Starts with: Open axClaude with fresh -- --model haiku.

### Move between the conversation and Claude's question, then answer it

1. Press Tab.
2. Press Tab.
3. Press Shift+Tab.
4. Press Shift+Tab.
5. Press Enter.

### Switch between the message field and the conversation

1. Press Ctrl+Tab.
2. Press Ctrl+Tab.
3. Press F6.
4. Press F6.
5. Press Ctrl+Shift+Tab.
6. Press Ctrl+Shift+Tab.

### Jump straight to the conversation or the message field

1. Press Ctrl+2.
   The focus moves to Conversation (Text).
2. Press Ctrl+2.
3. Press Ctrl+1.
   The focus moves to Message to Claude (Text).
4. Press Ctrl+1.
   You hear: Message to Claude.

### Go to the conversation from the menu

1. Choose the menu item Conversation.

### Hear the status

1. Press F8.
   You hear: Claude: ready |.
2. Press Ctrl+2.
3. Press F8.
   You hear: Claude: ready |.
4. Choose the menu item Say the status.
   You hear: Claude: ready |.

## Send Claude a message and hear the reply

Starts with: Open axClaude with plain -- --model haiku.

### Read Claude's question as lines in the conversation

   You hear: Claude asks.
1. Press Ctrl+2.
2. Press Ctrl+Home.
3. Press Down.
4. Press Down.
5. Press Down.

### Answer a question by typing in the message field

1. Press Ctrl+Tab.
2. Type y.
3. Press Enter.

### Send a message of two lines

1. Type What is 2+2?
2. Press Shift+Enter.
3. Type Answer in one line.
4. Press Enter.
   You hear the sound Sent.
   You hear the sound Responding.
   You hear the sound Done.

### Go to Claude's reply and read it

1. Press Ctrl+Tab.
2. Press Shift+O.
   You hear: Output 1 Reply from Claude heading level 1.
3. Press Down.
4. Press Down.
5. Press Down.
6. Press Up, Up, Up, Up.
7. Press Up.
8. Press Up.
9. Press Up.

### Go back to the message you sent

1. Press Ctrl+End.
2. Press I.
   You hear: No next input.
3. Press Shift+I.
   You hear: Input 1 heading level 1.

### Move through a long message by page

1. Type alpha.
2. Press Pagedown.
3. Press Pageup.
4. Press Ctrl+A, Delete.
5. Type l1.
6. Press Shift+Enter.
7. Type l2.
8. Press Shift+Enter.
9. Type l3.
10. Press Shift+Enter.
11. Type l4.
12. Press Shift+Enter.
13. Type l5.
14. Press Ctrl+Home.
15. Press Pagedown.
16. Press Pagedown.
17. Press Pagedown.
18. Press Pageup.
19. Press Pageup.
20. Press Pageup.
21. Press End.
22. Press Home.
23. Press Ctrl+End.
24. Press Ctrl+Home.
25. Press Ctrl+A, Delete.

### Move through a message by paragraph

1. Type one.
2. Press Shift+Enter, Shift+Enter.
3. Type two.
4. Press Shift+Enter.
5. Type three.
6. Press Shift+Enter, Shift+Enter.
7. Type four.
8. Press Ctrl+Home.
9. Press Ctrl+Down.
10. Press Ctrl+Down.
11. Press Ctrl+Down.
12. Press Ctrl+Up.
13. Press Ctrl+Up.
14. Press Ctrl+Up.
15. Press Ctrl+Up.

### Read the keyboard shortcuts

1. Press F1.
2. Press H.
   You hear: Anywhere, heading level 2.
3. Press H.
   You hear: Message field, heading level 2.
4. Press H.
   You hear: Conversation, heading level 2.
5. Press Shift+H.
   You hear: Message field, heading level 2.
6. Press Escape.

## Read the conversation while Claude is writing

Starts with: Open axClaude with read -- --model haiku.

### Read the conversation while Claude writes

1. Type Write the numbers 1 to 100, one per line, nothing else.
2. Press Enter.
3. Press Ctrl+Tab.
4. Press Ctrl+Home, Down.
5. Press Down.

### Go to Claude's latest reply

1. Press Ctrl+3.
   You hear: Output 1 Reply from Claude heading level 1.
2. Press Ctrl+1.
3. Press Ctrl+3.
   You hear: Output 1 Reply from Claude heading level 1.
4. Press Down.
5. Press Down.

### Quote a line of a reply in your next message

1. Press Ctrl+1.
2. Type Write one paragraph of 120 words about rivers, with no line breaks.
3. Press Enter.
4. Press Ctrl+3.
   You hear: Output 2.
5. Press C.
6. Press L.
7. Press Enter.
8. Press Ctrl+A, Delete.

### Select lines of a reply and clear the selection

1. Press Ctrl+2.
2. Press Ctrl+Home.
3. Press O.
   You hear: Output 1.
4. Press Down, Down.
5. Press Shift+Down, Shift+Down.
6. Press Escape.
   You hear: Selection cleared.
7. Press Escape.

## Move through a reply by headings and quick keys

Starts with: Open axClaude with keys -- --model haiku.

### Move through the conversation by headings

1. Press Ctrl+2.
2. Press Ctrl+Home.
3. Press Backspace.
   You hear: Nothing to go back to.
4. Press H.
   You hear: Input 1 heading level 1.
5. Press H.
   You hear: Output 1 Reply from Claude heading level 1.
6. Press H.
   You hear: Alpha heading level 2.
7. Press H.
   You hear: Beta heading level 2.
8. Press H.
   You hear: Input 2 heading level 1.
9. Press Shift+H.
   You hear: Beta heading level 2.
10. Press Ctrl+Home.
11. Press 1.
   You hear: Input 1 heading level 1.
12. Press 1.
   You hear: Output 1 Reply from Claude heading level 1.
13. Press 1.
   You hear: Input 2 heading level 1.

### Move by heading level

1. Press Ctrl+Home.
2. Press 2.
   You hear: Alpha heading level 2.
3. Press 2.
   You hear: Beta heading level 2.
4. Press 3.
   You hear: No next heading level 3.

### Hide the markers in the text

1. Choose the menu item Markers in the text.
2. Press Ctrl+Home.
3. Press H.
   You hear: Input 1 heading level 1.
4. Choose the menu item Markers in the text.

### Move through the conversation by paragraph

1. Press Ctrl+Home.
2. Press P.
   You hear: Output 1 Reply from Claude.
3. Press Shift+P.
   You hear: [Screen Reader Mode.
4. Press Ctrl+End.
5. Press P.
   You hear: No next paragraph.

### Jump between replies, messages, tools, errors and system lines

1. Press Ctrl+Home.
2. Press O.
   You hear: Output 1 Reply from Claude heading level 1.
3. Press R.
   You hear: Output 2 Reply from Claude heading level 1.
4. Press Shift+R.
   You hear: Output 1 Reply from Claude heading level 1.
5. Press Shift+O.
   You hear: No previous response.
6. Press I.
   You hear: Input 2 heading level 1.
7. Press Shift+I.
   You hear: Input 1 heading level 1.
8. Press C.
9. Press T.
10. Press E.
   You hear: No next error.
11. Press D.
12. Press S.
   You hear: No next system line.

### Go back to the message field from the conversation

1. Press Ctrl+2.
2. Press Escape.

### Move through a long reply a screen at a time

1. Type Write 60 numbered lines, 1. to 60., each with just its number, nothing else.
2. Press Enter.
3. Press Ctrl+Tab.
4. Press Pageup.
5. Press Pagedown.
6. Press Pagedown.
7. Press Ctrl+Home, Pageup.

### Skip over a run of lines in a reply

1. Press Ctrl+3.
   You hear: Output 3 Reply from Claude heading level 1.
2. Press Ctrl+Down.
3. Press Ctrl+Down.
4. Press Ctrl+Up.
5. Press Ctrl+Up.
6. Press Ctrl+Shift+Down.
7. Press Escape.
   You hear: Selection cleared.

### Hear the line number

1. Press L.
2. Press Ctrl+Home.
3. Press L.
   You hear: Line 1 of.
4. Press Ctrl+End.
5. Press L.

### Find text in the conversation

1. Press Ctrl+F.
2. Type single word.
3. Press Enter.
   You hear: From the top, Use the Bash tool.
4. Press F3.
   You hear: From the top.
5. Press Shift+F3.
   You hear: From the end.

### Find the next match from the message field

1. Press Ctrl+1.
2. Press Ctrl+F.
3. Type single word.
4. Press Enter.
5. Press Ctrl+1.
6. Press F3.
   You hear: Use the Bash tool.

### Go back to where you were before a jump

1. Press Ctrl+Home.
2. Press H, H, H, P, P.
3. Press Backspace.
   You hear: Back to.
4. Press Backspace.
   You hear: Back to.
5. Press Backspace.
   You hear: Back to.
6. Press Backspace.
   You hear: Back to.
7. Press Backspace.
   You hear: Back to [Screen Reader Mode.
8. Press Pagedown, Backspace.
   You hear: Back to [Screen Reader Mode.

### Bookmark lines and move between them

1. Press Ctrl+3.
2. Press Down, Down, Down.
3. Press M.
   You hear: Bookmark 1 added.
4. Press Up.
5. Press Down.
6. Press Down, Down, Down.
7. Press M.
   You hear: Bookmark 2 added.
8. Press Ctrl+Home.
9. Press K.
   You hear: Bookmark 1, 2.
10. Press K.
   You hear: Bookmark 2, 5.
11. Press K.
   You hear: No next bookmark.
12. Press Shift+K.
   You hear: Bookmark 1, 2.
13. Press Down.
14. Press M.
   You hear: Bookmark 1 removed.
15. Press Up.
16. Press K.
   You hear: Bookmark 2, 5.
17. Press M.
   You hear: Bookmark 2 removed.
18. Press Ctrl+1.
19. Press Ctrl+Shift+K.
   You hear: Bookmark 3 added.
20. Press Down.
21. Choose the menu item Bookmark this line.
   You hear: Bookmark 4 added.
22. Press Ctrl+Home.
23. Choose the menu item Next bookmark.
   You hear: Bookmark 3.
24. Choose the menu item Next bookmark.
   You hear: Bookmark 4.
25. Choose the menu item Previous bookmark.
   You hear: Bookmark 3.

### Hear a reply's paragraphs, headings or bookmarks without moving

1. Press Ctrl+3.
2. Press Ctrl+Alt+P.
   You hear: claude: 1.
3. Press Ctrl+Home, O.
4. Press Ctrl+Alt+H.
   You hear: Beta heading level 2.
5. Press Ctrl+1.
6. Press Ctrl+Alt+C.
7. Press Ctrl+Alt+K.
   You hear: Bookmark 3.
8. Press Ctrl+2, Ctrl+End.
9. Press Ctrl+Alt+P.
   You hear: No next paragraph.
10. Press Ctrl+Home.
11. Press Ctrl+Alt+O.
   You hear: Output 1 Reply from Claude heading level 1.
12. Press Ctrl+Alt+L, Ctrl+Alt+M.

### Quote two lines in a message and send it

1. Press Ctrl+1.
2. Type Reply with the single word ok.
3. Press Ctrl+2, Ctrl+Home.
4. Press I.
5. Press Enter.
6. Press Ctrl+2, Ctrl+Home.
7. Press C.
8. Press Enter.
9. Press Ctrl+Home.
10. Press Down.
11. Press Down.
12. Press Enter.
13. Press Ctrl+3.
14. Press Shift+I.
   You hear: Input 4 heading level 1.
15. Press Down.
16. Press Down.
17. Press Down.
18. Press Down.
19. Press Down.
20. Press Down.
21. Press Down.
22. Press Down.

### Quote a long paragraph whole

1. Press Enter.

### Send the quote with your comment

1. Press Ctrl+1.
2. Type Reply with the single word yes.
3. Press Enter.
4. Press Ctrl+2.

## Change the permission mode and interrupt Claude

Starts with: Open axClaude with modes -- --model haiku --permission-mode default.

### Change the permission mode

1. Press Ctrl+Shift+M.
   You hear: accept edits on.
2. Press Ctrl+Shift+M.
   You hear: plan mode on.
3. Press Ctrl+Shift+M.
   You hear: auto mode on.
4. Press Ctrl+Shift+M.
   You hear: manual mode on.

### Read a permission question in the conversation

1. Type Create a file named a.txt containing hi, with the Write tool.
2. Press Enter.
   You hear: Claude asks.
   You hear the sound Question.
3. Press Ctrl+2, Ctrl+End.
4. Press Up.
5. Press Up, Up.
6. Press Up, Up, Up.
7. Press Up.

### Answer a permission question by its number

1. Press Ctrl+Tab.
2. Type 1.
3. Press Enter.

### Interrupt Claude while it works

1. Type Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the single word pinged.
2. Press Enter.
   You hear: Claude asks.
3. Type 1.
4. Press Enter.
5. Press Escape.
   You hear: Shift+Escape interrupts.
6. Press Ctrl+O.
   You hear: Ctrl+O is off. For tool output, start with --verbose.
7. Press Shift+Escape.
8. Press Ctrl+2, Ctrl+End.

### Interrupt Claude from the menu or the message field

1. Press Ctrl+1.
2. Type Write a story of 1500 words about a lighthouse keeper.
3. Press Enter.
4. Choose the menu item Ctrl+C.
5. Type Write a story of 1500 words about a lighthouse keeper.
6. Press Enter.
7. Press Ctrl+Shift+C.

### Send Claude a key from the menu

1. Choose the menu item Shift+Up.
2. Choose the menu item Shift+Down.

## Send messages while Claude is still working

Starts with: Open axClaude with queue -- --model haiku --permission-mode default.

### Send a message while Claude works

1. Type Use your Bash tool to run: ping -n 20 127.0.0.1 then reply with the words OK DONE.
2. Press Enter.
   You may hear the sound Notice.
3. If that came: Press 1, Enter.
   You hear: Sent: 1.
4. Type Reply with the single word second.
5. Press Enter.
   You hear the sound Sent.
6. Press Ctrl+Tab, Ctrl+End.
7. Press Shift+I.
   You hear: Input 1 heading level 1.
8. Press Shift+I.
   You hear: No previous input.
9. Press Ctrl+Tab.
10. Press Up.

### Send a waiting message at once

1. Press Ctrl+1.
2. Type Use your Bash tool to run: ping -n 20 127.0.0.1 then reply with the words OK DONE.
3. Press Enter.
   You may hear the sound Notice.
4. If that came: Press 1, Enter.
   You hear: Sent: 1.
5. Type Reply with the single word fourth.
6. Press Enter.
7. Press Ctrl+Shift+S.
   You hear: Sending now.
8. Press Ctrl+2.
9. Press Ctrl+Shift+S.
   You hear: Nothing queued.

## Change Claude's project folder

Starts with: Open axClaude with proj -- --model haiku. Close the window.

### Continue the last conversation in a folder

1. Open axClaude with proj -- --model haiku --continue.
2. Press Ctrl+2, Ctrl+Home.
3. Press I.
   You hear: Input 1 from previous session heading level 1.
4. Press Down.
5. Press Down.
6. Press Down.
7. Press Down.
8. Press Down.
9. Press I.
   You hear: Input 2 from previous session heading level 1.
10. Press I.
   You hear: No next input.

### Restart Claude

1. Press Ctrl+Shift+R.
2. Press Enter.
3. Press Ctrl+2, Ctrl+End.
4. Press Shift+I.
   You hear: Input 1 heading level 1.
5. Press Shift+I.
   You hear: Input 2 from previous session heading level 1.
6. Press Ctrl+1.
7. Type Reply with the single word four.
8. Press Enter.
9. Press Ctrl+2, Ctrl+End.
10. Press Shift+I.
   You hear: Input 2 heading level 1.

### Start a new session with arguments of your own

1. Press Ctrl+N.
2. Press Down.
3. Press Down.
4. Press Tab.
5. Press Tab.
6. Press Tab.
7. Press Shift+Tab.
8. Press Up.
9. Press Up.
10. Press Up.
11. Press Up.
12. Press Down, Down, Down, Down.
13. Press Tab.
14. Press Ctrl+A, Delete.
15. Type --model haiku.
16. Press Shift+Enter.
17. Type --continue --name demo.
18. Press Enter.
   You hear: New session in proj.
19. Press Ctrl+2, Ctrl+Home.
20. Press I.
   You hear: from previous session heading level 1.

## Restart, stop and close a conversation

Starts with: Open axClaude with life -- --model haiku --permission-mode default.

### Stop Claude from the message field

1. Type /exit.
2. Press Enter.
   You hear: Claude stopped.
3. Type hello.
4. Press Enter.
   You hear: Claude stopped. Ctrl+Shift+R starts it.
5. Press Ctrl+A, Delete.

### Start a stopped Claude again

1. Press Ctrl+Shift+R.

### Restart Claude while it runs

1. Press Ctrl+Shift+R.
2. Press Escape.
3. Press Ctrl+Shift+R.
4. Press Enter.

### Decide whether to close the window while Claude works

1. Type Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the word pinged.
2. Press Enter.
   You may hear the sound Notice.
3. If that came: Press 1, Enter.
4. Press Alt+F4.
5. Press Tab.
6. Press Tab.
7. Press Escape.

### Carry on the conversation after closing

1. Open axClaude with life -- --model haiku --continue.
2. Press Ctrl+2, Ctrl+Home.
3. Press I.
   You hear: from previous session heading level 1.

### Close the window while Claude is ready

1. Close the window.

## Choose axClaude's speech, sounds and text

Starts with: Open axClaude with opts --record raw.vt -- --model haiku.

### Make the text larger or smaller

1. Choose the menu item Larger text.
   You hear: Text size 10.
2. Choose the menu item Smaller text.
   You hear: Text size 9.
3. Choose the menu item Larger text.
   You hear: Text size 10.

### Turn off the ticks for new lines and tool calls

1. Choose the menu item Ticks for new lines and tool calls.
2. Type Use the Bash tool to run: echo hi. Then reply with the single word again.
3. Press Enter.
   You hear the sound Sent.
4. Choose the menu item Ticks for new lines and tool calls.

### Turn off the chimes

1. Choose the menu item Chimes.
2. Type Reply with the single word quiet.
3. Press Enter.
   You hear the sound Click.
4. Choose the menu item Chimes.

### Play background music while Claude works

1. Press Ctrl+M.
   You hear: Background music on, nothing is running.
2. Type Use the Bash tool to run: echo hi. Then reply with the single word music.
3. Press Enter.
   You hear the sound StartMusic.
   You hear the sound StopMusic.
   You hear the sound Done.
4. Press Ctrl+M.
   You hear: Background music off.
5. Type Write a story of 1500 words about a lighthouse keeper.
6. Press Enter.
7. Press Ctrl+2.
8. Press Ctrl+M.
   You hear the sound StartMusic.
   You hear: Background music on.
9. Press Ctrl+M.
   You hear the sound StopMusic.
   You hear: Background music off.

### Stop recording the raw stream

1. Choose the menu item Stop recording.
   You hear: Recording stopped.
2. Press Ctrl+2.

### Have Claude's replies spoken

1. Choose the menu item Speak replies: all lines.
   You hear: Speak replies: all lines.
2. Press Ctrl+1.
3. Type Markdown only: ## Alpha, a five-word sentence, ## Beta, three one-word bullets.
4. Press Enter.
   You hear: claude: Alpha.
5. Choose the menu item Speak replies: first lines.
   You hear: Speak replies: first lines.
6. Type Reply with two lines: the word first, then on the next line the word second.
7. Press Enter.
   You hear: claude: first.

### Hear each tool call as it starts

1. Choose the menu item Speak replies: none.
   You hear: Speak replies: none.
2. Choose the menu item Speak each tool call.
3. Type Use the Bash tool to run: echo hi. Then reply with the single word tooled.
4. Press Enter.
   You hear: tool: Bash.
5. Choose the menu item Speak each tool call.

### Read the keyboard shortcuts

1. Press F1.
2. Press H.
   You hear: Anywhere, heading level 2.
3. Press H.
   You hear: Message field, heading level 2.
4. Press Shift+H.
   You hear: Anywhere, heading level 2.
5. Press 2.
   You hear: Message field, heading level 2.

### Find a word in the keyboard shortcuts

1. Press Ctrl+F.
2. Press Ctrl+A.
3. Type palette.
4. Press Enter.
5. Press F3.
6. Press Shift+F3.
7. Press Ctrl+F.
8. Press Escape.

### Read the user guide

1. Choose the menu item User guide.
2. Press H.
   You hear: heading level 2.
3. Press Shift+H.
   You hear: heading level.
4. Press Ctrl+End, H.
   You hear: No next heading.
5. Press Escape.

### Read the version, the licence and the axKit guide

1. Choose the menu item Version.
2. Press Escape.
3. Choose the menu item Licence.
4. Press Escape.
5. Choose the menu item Attribution.
6. Press Escape.
7. Choose the menu item axKit guide.
8. Press H.
   You hear: Install, heading level 2.
9. Press H.
10. Press H.
   You hear: If a key does nothing.
11. Press H.
12. Press Escape.

### Show the time on Input and Output lines

1. Choose the menu item Time stamps on Input and Output lines.
2. Press Ctrl+1.
3. Type Reply with the single word stamped.
4. Press Enter.
5. Press Ctrl+2, Ctrl+End.
6. Press Shift+I.
   You hear: Input 11 (.
7. Press Shift+O.
   You hear: Output 10 Reply from Claude heading level 1.
8. Press O.
   You hear: Output 11 Reply from Claude (.
9. Choose the menu item Time stamps on Input and Output lines.

## Install Claude Code when it is missing

Starts with: Open axClaude with gone --claude C:\nowhere\claude.exe -- --model haiku.

### Hear the ways to install Claude Code

1. Press Tab.
2. Press Tab.
3. Press Tab.
4. Press Tab.
5. Press Tab.

### Close the notice that Claude Code is missing

1. Press Enter.
2. Press Ctrl+2.

## Check for an update from a conversation

Starts with: Open axClaude with upd -- --model haiku.

### Check for an update

1. Choose the menu item Check for updates (axKit.
2. Press Escape.
3. Choose the menu item Version.
4. Press Escape.

### Turn off the update check at start

1. Choose the menu item Check for updates at start.
2. Close the window.
3. Open axClaude with upd -- --model haiku.
4. Choose the menu item Version.
5. Press Escape.

## Answer Claude's questions and screens

Starts with: Open axClaude with ask -- --model haiku.

### Answer the trust question from the list

   The focus moves to Message to Claude.
   You hear the sound Notice.
   The focus moves to Permission Required: Accessing workspace.
   You hear: Permission Required: Accessing workspace.
1. Press Y, Enter.
   You hear: Sent: y, Yes, I trust this folder.
   The focus moves to Message to Claude.

### Move between a question and the conversation

1. Type Use your AskUserQuestion tool once to ask me which flower I like: rose, tulip or daisy.
2. Press Enter.
   You hear the sound Notice.
3. Press Ctrl+Tab.
4. Press Ctrl+Tab.
5. Press Tab.
6. Press Shift+Tab.

### Hear the question again

1. Press Down, Down.
2. Press Ctrl+Q.
   You hear: 3. Daisy.
3. Press Ctrl+Tab.
4. Press Ctrl+Q.

### Cancel a question

1. Press Escape.

### Answer a question that follows a reply

1. Type Write two sentences about gardens, then use your AskUserQuestion tool to ask me which flower I like: rose, tulip or daisy.
2. Press Enter.
3. Press Ctrl+2.
   You hear the sound Notice.
4. Press Ctrl+Tab.
5. Press Ctrl+Tab.
6. Press 1, Enter.
   You hear: Sent: 1.

### Have questions come without interrupting speech

1. Choose the menu item Go to them at once, interrupting speech.
2. Press Ctrl+1.
3. Type Write two sentences about gardens, then use your AskUserQuestion tool to ask me which flower I like: rose, tulip or daisy.
4. Press Enter.
5. Press Ctrl+2.
   You hear the sound Notice.
6. Press 1, Enter.
   You hear: Sent: 1.
7. Choose the menu item Go to them at once, interrupting speech.

### Open and close Claude's list of background tasks

1. Type /tasks.
2. Press Enter.
   You may hear: Claude asks.
3. If that came: Press Shift+Escape.
   You may hear the sound Notice.
4. If that came: Press Escape.

### Answer a question in your own words

1. Type Use the AskUserQuestion tool to ask me which flower I enjoy thinking about, with three options.
2. Press Enter.
   You hear the sound Notice.
3. Press 4, Enter.
   You hear: Sent: 4, Other. Your words next.
4. Type tulips in the garden by the old stone wall where the morning light falls first and the bees arrive early.
5. Press Enter.
   You hear: Sent: tulips in the garden by the old stone wall where the morning light falls first and the bees arrive early.
6. Press Ctrl+2, Ctrl+End.
7. Press Ctrl+1.
8. Type Use the AskUserQuestion tool to ask me which flower I enjoy thinking about, with three options.
9. Press Enter.
   You hear the sound Notice.
10. Press 4, Enter.
11. Press Escape.
12. Press 1, Enter.

### Move through Claude's questions in the conversation

1. Press Ctrl+2, Ctrl+Home.
2. Press Q.
   You hear: Question: Permission Required: Accessing workspace.
3. Press Q.
4. Press Ctrl+End.
5. Press Q.
   You hear: No next question.
6. Press Shift+Q.

### Close a question without answering it

1. Press Ctrl+1.
2. Type Use your AskUserQuestion tool once to ask me which flower I like: rose, tulip or daisy.
3. Press Enter.
   You hear the sound Notice.
4. Press Shift+Escape.

### Interrupt Claude from a permission question

1. Press Ctrl+Shift+M.
   You hear: manual mode on.
2. Type Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the word pinged.
3. Press Enter.
   You hear the sound Notice.
4. Press Shift+Escape.
5. Press Ctrl+Shift+M, Ctrl+Shift+M, Ctrl+Shift+M.
   You hear: auto mode on.

### Answer a set of two questions

1. Type Reply with one sentence about tulips, then use the AskUserQuestion tool to ask me two questions, which colour I prefer and which season.
2. Press Enter.
   You hear the sound Notice.
3. Press Enter.
   You hear: Next question.
4. Press Enter.
5. Press Ctrl+Shift+R.
6. Press Enter.

### Open and close Claude's permissions screen

1. Type /permissions.
2. Press Enter.
   You hear the sound Notice.
3. Press Escape.

### Answer a question with several answers

1. Press Ctrl+1.
2. Type Use the AskUserQuestion tool once with two questions: my favourite colour (red, green, blue), and, with multiSelect, which fruits I like (apple, banana, cherry).
3. Press Enter.
   You hear the sound Notice.
4. Press 2, Enter.
   You hear: Sent: 2, Green. Next question.
5. Press Home, Space, Down, Down, Space, Enter.
   You hear: Sent: 1, Apple, 3, Cherry.
6. Press Ctrl+2, Ctrl+End.
7. Press Ctrl+1.

### Review answers before they go to Claude

1. Choose the menu item Review answers before they go to Claude.
2. Type Use the AskUserQuestion tool once with two questions: my favourite colour (red, green, blue), and, with multiSelect, which fruits I like (apple, banana, cherry).
3. Press Enter.
   You hear the sound Notice.
4. Press 2, Enter.
   You hear: Next question.
5. Press Home, Space, Enter.
   You hear: Review next.
6. Press Y, Enter.
7. Choose the menu item Review answers before they go to Claude.

## Use the axKit menu from a conversation

Starts with: Open axClaude with menu -- --model haiku.

### Close every window at once

1. Open axDown with notes.md.
2. Press Ctrl+End.
3. Type more.
4. Use window 1.
5. Press Ctrl+1.
6. Type Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the word pinged.
7. Press Enter.
   You may hear the sound Notice.
8. If that came: Press 1, Enter.
9. Choose the menu item Close all windows.
10. Use window 2.
11. Close the window.
12. Press Tab.
13. Press Tab.
14. Press Enter.
15. Use window 1.
16. Close the window.
17. Press Enter.

## Read a side answer and other information screens

Starts with: Open axClaude with side -- --model haiku.

### Ask a side question and hear the answer

1. Type /btw what is two plus two? answer in one word.
2. Press Enter.
   You hear the sound Notice.

### Close a side answer

1. Press Escape.

### Browse earlier side answers

1. Type /btw what is three plus three? answer in one word.
2. Press Enter.
   You hear the sound Notice.
3. Press Tab, Tab.
4. Press Enter.
5. Press Escape.

### Look at Claude's goal

1. Type /goal.
2. Press Enter.
   You hear the sound Notice.
3. Press Enter.

### Find side answers in the conversation

1. Press Ctrl+2, Ctrl+Home.
2. Press B.
   You hear: btw: what is two plus two? answer in one word.
3. Press B.
   You hear: btw: what is three plus three? answer in one word.
4. Press B.
   You hear: No next btw.
5. Press Shift+B.
   You hear: btw: what is two plus two? answer in one word.

## Agree to the reminder before Claude Code first starts

Starts with: Open axClaude with fresh -- --model haiku.

### Agree to the reminder before Claude Code starts

   You hear: Before axClaude connects to Claude Code.
1. Press Tab.
   You hear: I understand that axClaude.
2. Press Enter.
   You hear: Check the box first.
3. Press Space.
4. Press Enter.
5. Press Enter.
6. Close the window.

## Report a bug or an idea from a conversation

Starts with: Open axClaude with report -- --model haiku.

### Open the report form

1. Press F4.
   You hear: A report goes to axKit's maintainer. It is encrypted on your computer.

### Type a report of several lines

1. Type The list loses my place.
2. Press Enter.
   You hear: new line.
3. Type every time.

### Add detail that may name your files

1. Press Shift+Tab.
2. Press Space.
3. Press Tab.
4. Press Shift+Tab.
5. Press Space.
   You hear: not checked.
6. Press Tab.

### Send the report

1. Press Shift+Tab, Shift+Tab, Shift+Tab, Shift+Tab, Shift+Tab.
2. Press Ctrl+Enter.
3. Press Escape.

### Cancel a report

1. Press F4.
2. Press Escape.
3. Press F4.
4. Type Draft words.
5. Press Escape.

## Choose which words axClaude says by itself

Starts with: Open axClaude with spoken -- --model haiku --permission-mode default.

### Silence Claude's own notices

1. Press Ctrl+Shift+M.
2. Choose the menu item Claude's own notices, such as "accept edits on".
3. Press Ctrl+Shift+M.
   You hear: plan mode on.
4. Press Ctrl+Shift+M.
5. Press Ctrl+Shift+M.

### Turn the words Sent and Responding back on

1. Choose the menu item "Sent" and "Queued" after a message.
2. Choose the menu item "Responding" when a reply starts.
3. Type Reply with the single word two.
4. Press Enter.

### Keep reply lines with chosen words silent

1. Choose the menu item Speak replies: all lines.
   You hear: Speak replies: all lines.
2. Choose the menu item Words that keep a reply line silent.
3. Type pelican; walrus.
4. Press Enter.
   You hear: 2 words keep a line silent.
5. Type Reply with exactly two lines and nothing else: first line the words pelican feathers, second line the words green apples.
6. Press Enter.
   You hear: green apples.
7. Press Ctrl+2, Ctrl+End.

### Have every reply line spoken again

1. Choose the menu item Words that keep a reply line silent.
2. Press Ctrl+A, Delete, Enter.
   You hear: Every reply line is spoken.
3. Press Ctrl+1.
4. Type Reply with exactly one line: the words pelican again.
5. Press Enter.
   You hear: pelican again.

## Run a command Claude offers you

Starts with: Open axClaude with offer -- --model haiku.

### Read the explanation of an offered command

1. Call the tool axkit_offer_command.
   The tool answers: "offered": true.
   You hear the sound Offer.
2. Press Ctrl+2.
3. Press Ctrl+1.
4. Press Enter.
5. Press Enter.

### Hear the offered command again

1. Press Ctrl+Q.
   You hear: offers a command for you to run: echo offered-first. Written by axAutomate, an AI: read the explanation before you run it. Its reason: A check of the question.

### Turn down an offered command

1. Press Up, Up.
2. Press Enter.
   You hear: Not run.

### Run an offered command

1. Call the tool axkit_offer_command.
   The tool answers: "offered": true.
2. Press Down.
3. Press Enter.
4. Press Ctrl+2.

### Let an AI tool run a window command

1. Choose the menu item Let AI tools run commands.
   You hear: AI tools may run commands.
2. Call the tool axkit_window_run.
   The tool answers: The window asks the user whether to run Check for updates at start.
   You hear the sound Offer.
3. Press Down.
4. Press Enter.
   You hear: axAutomate ran Check for updates at start.

## See an offered command wait its turn

Starts with: Open axClaude with waits -- --model haiku.

### Turn down waiting offers one after another

1. Press Escape.
2. Press Escape.
   You hear: Not run.
3. Press Escape.
   You hear: Not run.
4. Press Escape.
   You hear: Not run.

## A conversation window that starts hidden and hides on closing

Starts with: Open axClaude with "home\axKit Claude" --title axHelp --hide-on-close --hidden -- --model haiku.

### Bring up the hidden conversation

1. Open axHelp.
2. Choose the menu item Open the conversation.
3. Use window 1.
   You may hear: trust.
4. If that came: Press Enter.

### Hide the conversation again

1. Close the window.
2. Use window 2.
3. Choose the menu item Open the conversation.
4. Use window 1.

### End Claude and close the window for good

1. Choose the menu item End Claude and close this window.

## See what happened while a hidden conversation was away

Starts with: Open axClaude with away --title axHelp --hide-on-close -- --model haiku. Open axHelp.

### Bring up the window and hear what happened while it was away

1. Use window 2.
2. Choose the menu item Open the conversation.
3. Use window 1.
   You hear the sound Offer.
4. Press Escape.
   You hear: Not run.

## Hear and ask what Claude runs in the background

Starts with: Open axClaude with bg --claude claude.cmd -- --model haiku.

### Ask what runs in the background

   You hear the sound BackgroundStart.
1. Press Ctrl+B.
   You hear: 3 in the background: 1 shell, 2 agents. general-purpose: Checking callers. Explore: Reading the spec.
2. Press F8.
   You hear: Claude: ready | 3 in the background: 1 shell, 2 agents | auto mode on.
3. Choose the menu item What runs in the background.
   You hear: 3 in the background: 1 shell, 2 agents. general-purpose: Checking callers. Explore: Reading the spec.
   You hear the sound BackgroundChord 3.

### Hear background work as it starts and ends

1. Choose the menu item Say background work as it starts and ends.
   You hear the sound BackgroundFinish.
   You hear: Checking callers finished.
   You hear the sound BackgroundChord 2.

## Find out whether a key arrives and what it does, in axClaude

Starts with: Open axClaude with kt --claude claude.cmd -- --model haiku.

### Hear what a key does without doing it

1. Press F8.
   You hear: F8: Say the status.
2. Press F4.
   You hear: F4: Send a report.
3. Press Ctrl+B.
   You hear: Ctrl+B: What runs in the background.
4. Press Ctrl+2.
   You hear: Ctrl+2: Conversation.
5. Press Shift+Escape.
   You hear: Shift+Escape: Interrupt.
6. Press A.
   You hear: A: types the letter.
7. Press Enter.
   You hear: Enter: sends what the field holds.
8. Press F9.
   You hear: F9: no command of this window.

### End the key test

1. Press Escape.
   You hear: Key test ended.

### Hear what the conversation's keys do

1. Choose the menu item Test a key.
   You hear: Key test. Press any key.
2. Press H.
   You hear: H: moves to the next heading.
3. Press Shift+H.
   You hear: Shift+H: moves to the previous heading.
4. Press Ctrl+Alt+H.
   You hear: Ctrl+Alt+H: reads every heading from the caret's line to the end.
5. Press L.
   You hear: L: says the line number.
6. Press Y.
   You hear: Y: no command of this window.
7. Press Escape.
   You hear: Key test ended.
8. Press L.

## Read back in the conversation while Claude writes on, and find every line in its place

Starts with: Open axClaude with order --claude claude.cmd -- --model haiku.

## Read back a long message before sending it, and turn the music on with Ctrl+M

Starts with: Open axClaude with em --claude claude.cmd -- --model haiku.

### Move through a message by its headings, links and lists

1. Press Ctrl+Space.
   You hear: Navigation mode.
2. Press K.
   You hear: https://example.com, link.
3. Press I.
4. Press I.
5. Press Shift+H.
   You hear: Plan, heading level 1.

### Go back to editing the message

1. Press Ctrl+1.
2. Press Ctrl+Space.
   You hear: Edit mode.
3. Press End.
4. Type !

### Turn the background music on and off

1. Press Ctrl+M.
   You hear: Background music on, nothing is running.
2. Press Ctrl+2, Ctrl+M.
   You hear: Background music off.

## Block Claude Code's feedback requests from the Options menu, and turn it off again

Starts with: Open axClaude with cs --claude claude.cmd -- --model haiku.

### Block Claude Code's feedback requests

1. Choose the menu item Block Claude Code's feedback requests.
   You hear: Claude Code's feedback requests blocked, applies at once.

### Turn the block on feedback requests off again

1. Choose the menu item Block Claude Code's feedback requests.
   You hear: Claude Code's feedback requests back to Claude Code's own default, applies at once.

### Choose how Claude-drafted feedback comes

1. Choose the menu item Claude-drafted feedback: quiet.
   You hear: Claude-drafted feedback: quiet, counted without a card, applies at once.
2. Choose the menu item Claude-drafted feedback: a card for each.
   You hear: Claude-drafted feedback: a card for each, Claude Code.

## Follow a workflow Claude runs in the background without hearing its live row

Starts with: Open axClaude with wf --claude claude.cmd -- --model haiku.

### Hear how far a workflow is

1. Press Ctrl+B.
   You hear: 1 workflow in the background. Workflow axkit-design-map-and-verify: 88 percent, 8 of 9 done, 20m18s.
2. Press F8.
   You hear: 1 workflow in the background.

### Show Claude Code's live progress rows in the conversation

1. Choose the menu item Show Claude Code's live progress rows in the conversation.
   You hear the sound BackgroundStart.
2. Press Ctrl+2, Ctrl+End.
   The focus moves to Conversation.

### Hide the live progress rows again

1. Choose the menu item Show Claude Code's live progress rows in the conversation.

## Hear where axClaude's events come from

Starts with: Open axClaude with es --claude claude.cmd -- --model haiku.

1. Choose the menu item Say the event sources.
   You hear: Events. Marks: 0. Hooks: not passed. Session file: not found. Core: in shadow.
2. Type Say ok.
3. Press Enter.
4. Choose the menu item Say the event sources.
   You hear: Events. Marks: 3. Hooks: not passed. Session file: not found. Core: in shadow.
5. Choose the menu item Ground the conversation in Claude Code's session file.
   You hear: Grounding in Claude Code's session file off.
6. Choose the menu item Say the event sources.
   You hear: Events. Marks: 3. Hooks: not passed. Session file: off. Core: in shadow.
7. Choose the menu item Ground the conversation in Claude Code's session file.
   You hear: Grounding in Claude Code's session file on.
8. Choose the menu item Say the event sources.
   You hear: Events. Marks: 3. Hooks: not passed. Session file: not found. Core: in shadow.
9. Choose the menu item Events from Claude Code's hooks.
   You hear: Events from Claude Code's hooks off, from the next start of Claude.
10. Choose the menu item Say the event sources.
   You hear: Events. Marks: 3. Hooks: not passed. Session file: not found. Core: in shadow.
11. Press Ctrl+Shift+R.
12. Press Enter.
13. Choose the menu item Say the event sources.
   You hear: Events. Marks: 0. Hooks: off. Session file: not found. Core: in shadow.
14. Choose the menu item Events from Claude Code's hooks.
   You hear: Events from Claude Code's hooks on, from the next start of Claude.

## Follow a turn played by the stand-in

Starts with: Open axClaude with st --claude claude.cmd -- --model haiku.

1. Type Say ok.
2. Press Enter.
   You hear: claude: ok.
3. Press Ctrl+2.

## Have a short exchange with Claude

Starts with: Open axClaude with a -- --model haiku.

   You may hear: trust.
1. If that came: Press Enter.
2. Type Reply with the single word hello.
3. Press Enter.
4. Press Ctrl+2.
5. Press Ctrl+3.
   You hear: Output 1.

## Answer a set of two questions from Claude

Starts with: Open axClaude with q -- --model haiku.

   You may hear: trust.
1. If that came: Press Enter.
2. Type Use your AskUserQuestion tool once, with two questions: which colour I prefer, red or blue, and which season I prefer, summer or winter.
3. Press Enter.
   You hear: Question 1 of 2.
4. Press Enter.
   You hear: Question 2 of 2.
5. Press Enter.
