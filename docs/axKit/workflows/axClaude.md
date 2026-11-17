# axClaude: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Start a conversation with Claude

Starts with: Open axClaude with the folder fresh.

### Move between the conversation and Claude's question, then answer it

You hear: trust.
1. Press Tab 2 times.
2. Press Shift+Tab 2 times.
3. Press Enter.
    You hear: Ready.

### Move between the message field and the conversation

1. Press Ctrl+Tab 2 times (Switch between the view and the field).
2. Press F6 2 times (Switch between the view and the field).
3. Press Ctrl+Shift+Tab 2 times (Switch between the view and the field).

### Jump straight to the conversation or the message field

1. Press Alt+2 (Conversation; Navigation menu).
    The focus moves to Conversation (Text).
2. Press Alt+2 (Conversation; Navigation menu).
3. Press Alt+1 (Message field; Navigation menu).
    The focus moves to Message to Claude (Text).
4. Press Alt+1 (Message field; Navigation menu).
    You hear: Message to Claude.

### Go to the conversation from the menu

1. Choose Conversation in the Navigation menu (key: Alt+2).

### Hear the status

1. Press F8 (Say the status; Window menu).
    You hear words such as: Claude: ready.
2. Press Alt+2 (Conversation; Navigation menu).
3. Press F8 (Say the status; Window menu).
    You hear words such as: Claude: ready.
4. Choose Say the status in the Window menu (key: F8).
    You hear words such as: Claude: ready.

## Send Claude a message and hear the reply

Starts with: Open axClaude with the folder plain.

### Read Claude's question as lines in the conversation

You hear: Claude asks.
1. Press Alt+2 (Conversation; Navigation menu).
2. Press Ctrl+Home.
3. Press Down 3 times.

### Answer a question by typing in the message field

1. Press Ctrl+Tab (Switch between the view and the field).
2. Type: y
3. Press Enter.
    You hear: Sent.
    You hear: Ready.

### Send a message of two lines

1. Type: What is 2+2?
2. Press Shift+Enter.
3. Type: Answer in one line, with no tool.
4. Press Enter.
    You hear: Sent.
    You hear the sound Sent.
    You hear: Responding.
    You hear the sound Responding.
    You hear: Done.
    You hear the sound Done.

### Go to Claude's reply and read it

1. Press Ctrl+Tab (Switch between the view and the field).
2. Press Shift+O.
    You hear: Output 1 Reply from Claude heading level 1.
3. Press Down 3 times.
4. Press Up 7 times.

### Go back to the message you sent

1. Press Ctrl+End.
2. Press I.
    You hear: No next input.
3. Press Shift+I.
    You hear: Input 1 heading level 1.

### Move through a long message by page

1. Type: alpha
2. Press Page Down.
3. Press Page Up.
4. Press Ctrl+A, Delete.
5. Type: l1
6. Press Shift+Enter.
7. Type: l2
8. Press Shift+Enter.
9. Type: l3
10. Press Shift+Enter.
11. Type: l4
12. Press Shift+Enter.
13. Type: l5
14. Press Ctrl+Home.
15. Press Page Down 3 times.
16. Press Page Up 3 times.
17. Press End.
18. Press Home.
19. Press Ctrl+End.
20. Press Ctrl+Home.
21. Press Ctrl+A, Delete.

### Move through a message by paragraph

1. Type: one
2. Press Shift+Enter 2 times.
3. Type: two
4. Press Shift+Enter.
5. Type: three
6. Press Shift+Enter 2 times.
7. Type: four
8. Press Ctrl+Home.
9. Press Ctrl+Down 3 times.
10. Press Ctrl+Up 4 times.

### Read the keyboard shortcuts

1. Press F1 (Keyboard shortcuts; Help menu).
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

Starts with: Open axClaude with the folder read.

### Read the conversation while Claude writes

1. Type: Write the numbers 1 to 100, one per line, nothing else.
2. Press Enter.
    You hear: Sent.
3. Press Ctrl+Tab (Switch between the view and the field).
4. Press Ctrl+Home, Down.
5. Press Down.

### Go to Claude's latest reply

1. Press Alt+3 (Latest response; Navigation menu).
    You hear: Output 1 Reply from Claude heading level 1.
2. Press Alt+1 (Message field; Navigation menu).
3. Press Alt+3 (Latest response; Navigation menu).
    You hear: Output 1 Reply from Claude heading level 1.
4. Press Down 2 times.

### Quote a line of a reply in your next message

1. Press Alt+1 (Message field; Navigation menu).
2. Type: Write one paragraph of 120 words about rivers, with no line breaks.
3. Press Enter.
    You hear: Done.
4. Press Alt+3 (Latest response; Navigation menu).
    You hear: Output 2.
5. Press C.
6. Press L.
    You hear: Line.
7. Press Enter.
    You hear: copied.
8. Press Ctrl+A, Delete.

### Select lines of a reply and clear the selection

1. Press Alt+2 (Conversation; Navigation menu).
2. Press Ctrl+Home.
3. Press O.
    You hear: Output 1.
4. Press Down 2 times.
5. Press Shift+Down 2 times.
6. Press Escape.
    You hear: Selection cleared.
7. Press Escape.

## Move through a reply by headings and quick keys

Starts with: Open axClaude with the folder keys.

### Move through the conversation by headings

1. Press Alt+2 (Conversation; Navigation menu).
2. Press Ctrl+Home.
3. Press Backspace.
    You hear words such as: Nothing to go back.
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

1. Choose Markers in the text in the Options menu, Text.
2. Press Ctrl+Home.
3. Press H.
    You hear: Input 1 heading level 1.
4. Choose Markers in the text in the Options menu, Text.

### Move through the conversation by paragraph

1. Press Ctrl+Home.
2. Press P.
    You hear: Output 1 Reply from Claude.
3. Press Shift+P.
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
    You hear words such as: claude.
9. Press T.
    You hear: hi.
10. Press E.
    You hear: No next error.
11. Press D.
    You hear: done.
12. Press S.
    You hear: No next system line.

### Go back to the message field from the conversation

1. Press Alt+2 (Conversation; Navigation menu).
2. Press Escape.

### Move through a long reply by page

1. Type: Write 60 numbered lines, 1. to 60., each with just its number, nothing else.
2. Press Enter.
    You hear: Sent.
    You hear: Done.
3. Press Ctrl+Tab (Switch between the view and the field).
4. Press Page Up.
5. Press Page Down 2 times.
6. Press Ctrl+Home, Page Up.

### Skip over a run of lines in a reply

1. Press Alt+3 (Latest response; Navigation menu).
    You hear: Output 3 Reply from Claude heading level 1.
2. Press Ctrl+Down 2 times.
3. Press Ctrl+Up 2 times.
4. Press Ctrl+Shift+Down.
5. Press Escape.
    You hear: Selection cleared.

### Hear the line number

1. Press L.
    You hear: Line.
2. Press Ctrl+Home.
3. Press L.
    You hear words such as: Line 1.
4. Press Ctrl+End.
5. Press L.

### Find text in the conversation

1. Press Ctrl+F (Find; Navigation menu).
2. Type: single word
3. Press Enter.
    You hear: From the top, Use the Bash tool.
4. Press F3 (Find next; Navigation menu).
    You hear words such as: From the top.
5. Press Shift+F3 (Find previous; Navigation menu).
    You hear words such as: From the end.

### Find the next match from the message field

1. Press Alt+1 (Message field; Navigation menu).
2. Press Ctrl+F (Find; Navigation menu).
3. Type: single word
4. Press Enter.
5. Press Alt+1 (Message field; Navigation menu).
6. Press F3 (Find next; Navigation menu).
    You hear: Use the Bash tool.

### Go back to where you were before a jump

1. Press Ctrl+Home.
2. Press H, H, H, P, P.
3. Press Backspace.
    You hear words such as: Back to.
4. Press Backspace.
    You hear words such as: Back to.
5. Press Backspace.
    You hear words such as: Back to.
6. Press Backspace.
    You hear words such as: Back to.
7. Press Backspace.
    You hear words such as: Back to.
8. Press Page Down, Backspace.
    You hear words such as: Back to.

### Bookmark lines and move between them

1. Press Alt+3 (Latest response; Navigation menu).
2. Press Down 3 times.
3. Press M.
    You hear: Bookmark 1 added.
4. Press Up.
5. Press Down 4 times.
6. Press M.
    You hear: Bookmark 2 added.
7. Press Ctrl+Home.
8. Press K.
    You hear: Bookmark 1, 2.
9. Press K.
    You hear: Bookmark 2, 5.
10. Press K.
    You hear: No next bookmark.
11. Press Shift+K.
    You hear: Bookmark 1, 2.
12. Press Down.
13. Press M.
    You hear: Bookmark 1 removed.
14. Press Up.
15. Press K.
    You hear: Bookmark 2, 5.
16. Press M.
    You hear: Bookmark 2 removed.
17. Press Alt+1 (Message field; Navigation menu).
18. Press Ctrl+Shift+K, or M in the conversation (Bookmark this line; Context menu).
    You hear: Bookmark 3 added.
19. Press Down.
20. Choose Bookmark this line in the Context menu (key: Ctrl+Shift+K, or M in the conversation).
    You hear: Bookmark 4 added.
21. Press Ctrl+Home.
22. Choose Next bookmark in the Navigation menu (key: K in the conversation).
    You hear words such as: Bookmark 3.
23. Choose Next bookmark in the Navigation menu (key: K in the conversation).
    You hear words such as: Bookmark 4.
24. Choose Previous bookmark in the Navigation menu (key: Shift+K in the conversation).
    You hear words such as: Bookmark 3.

### Hear a reply's paragraphs, headings or bookmarks without moving

1. Press Alt+3 (Latest response; Navigation menu).
2. Press Ctrl+Alt+P.
    You hear: claude: 1.
3. Press Ctrl+Home, O.
4. Press Ctrl+Alt+H.
    You hear: Beta heading level 2.
5. Press Alt+1 (Message field; Navigation menu).
6. Press Ctrl+Alt+C.
    You hear: finished.
7. Press Ctrl+Alt+K.
    You hear: Bookmark 3.
8. Press Alt+2, Ctrl+End.
9. Press Ctrl+Alt+P.
    You hear: No next paragraph.
10. Press Ctrl+Home.
11. Press Ctrl+Alt+O.
    You hear: Output 1 Reply from Claude heading level 1.
12. Press Ctrl+Alt+L, Ctrl+Alt+M.

### Quote two lines in a message and send it

1. Press Alt+1 (Message field; Navigation menu).
2. Type: Reply with the single word ok.
3. Press Alt+2, Ctrl+Home.
4. Press I.
5. Press Enter.
    You hear: copied.
6. Press Alt+2, Ctrl+Home.
7. Press C.
8. Press Enter.
9. Press Ctrl+Home.
10. Press Down 2 times.
11. Press Enter.
    You hear: Sent.
    You hear: Done.
12. Press Alt+3 (Latest response; Navigation menu).
13. Press Shift+I.
    You hear: Input 4 heading level 1.
14. Press Down 8 times.

### Quote a long paragraph whole

1. Press Enter.
    You hear: copied.

### Send the quote with your comment

1. Press Alt+1 (Message field; Navigation menu).
2. Type: Reply with the single word yes.
3. Press Enter.
    You hear: Sent.
    You hear: Done.
4. Press Alt+2 (Conversation; Navigation menu).

## Change the permission mode and interrupt Claude

Starts with: Open axClaude with the folder modes.

### Change the permission mode

1. Press Ctrl+Shift+M (Next permission mode; Actions menu).
    You hear: accept edits on.
2. Press Ctrl+Shift+M (Next permission mode; Actions menu).
    You hear: plan mode on.
3. Press Ctrl+Shift+M (Next permission mode; Actions menu).
    You hear: auto mode on.
4. Press Ctrl+Shift+M (Next permission mode; Actions menu).
    You hear: manual mode on.

### Read a permission question in the conversation

1. Type: Create a file named a.txt containing hi, with the Write tool.
2. Press Enter.
    You hear: Sent.
    You hear: Claude asks.
    You hear the sound Question.
3. Press Alt+2, Ctrl+End.
4. Press Up 7 times.

### Answer a permission question by its number

1. Press Ctrl+Tab (Switch between the view and the field).
2. Type: 1
3. Press Enter.
    You hear: Sent.
    You hear: Done.

### Interrupt Claude while it works

1. Type: Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the single word pinged.
2. Press Enter.
    You hear: Claude asks.
3. Type: 1
4. Press Enter.
    You hear: Sent.
5. Press Escape.
    You hear: Shift+Escape interrupts.
6. Press Ctrl+O.
    You hear: Ctrl+O is off. For tool output, start with --verbose.
7. Press Shift+Escape.
8. Press Alt+2, Ctrl+End.

### Interrupt Claude from the menu or the message field

1. Press Alt+1 (Message field; Navigation menu).
2. Type: Write a story of 1500 words about a lighthouse keeper.
3. Press Enter.
    You hear: Sent.
    You hear: Responding.
4. Choose Ctrl+C in the Actions menu, Send key (key: Ctrl+Shift+C).
5. Type: Write a story of 1500 words about a lighthouse keeper.
6. Press Enter.
    You hear: Sent.
    You hear: Responding.
7. Press Ctrl+Shift+C (Ctrl+C; Actions menu, Send key).

### Send Claude a key from the menu

1. Choose Shift+Up in the Actions menu, Send key.
2. Choose Shift+Down in the Actions menu, Send key.

## Send messages while Claude is still working

Starts with: Open axClaude with the folder queue.

### Send a message while Claude works

1. Type: Use your Bash tool to run: ping -n 20 127.0.0.1 then reply with the words OK DONE.
2. Press Enter.
    You hear: Sent.
    You may hear the sound Notice.
3. If that came: Press 1, Enter.
    You hear: Sent: 1.
4. Type: Reply with the single word second.
5. Press Enter.
    You hear: Queued.
    You hear the sound Sent.
6. Press Ctrl+Tab, Ctrl+End.
7. Press Shift+I.
    You hear: Input 1 heading level 1.
8. Press Shift+I.
    You hear: No previous input.
9. Press Ctrl+Tab (Switch between the view and the field).
10. Press Up.

### Send a waiting message at once

1. Press Alt+1 (Message field; Navigation menu).
2. Type: Use your Bash tool to run: ping -n 20 127.0.0.1 then reply with the words OK DONE.
3. Press Enter.
    You hear: Sent.
    You may hear the sound Notice.
4. If that came: Press 1, Enter.
    You hear: Sent: 1.
5. Type: Reply with the single word fourth.
6. Press Enter.
    You hear: Queued.
7. Press Ctrl+Shift+S (Send queued messages now; Actions menu).
    You hear: Sending now.
8. Press Alt+2 (Conversation; Navigation menu).
    You hear: Done.
9. Press Ctrl+Shift+S (Send queued messages now; Actions menu).
    You hear: Nothing queued.

## Change Claude's project folder

Starts with: Open axClaude with the folder proj. Close the window.

### Continue the last conversation in a folder

1. Open axClaude with the folder proj.
    You hear: Ready.
2. Press Alt+2, Ctrl+Home.
3. Press I.
    You hear: Input 1 from previous session heading level 1.
4. Press Down 5 times.
5. Press I.
    You hear: Input 2 from previous session heading level 1.
6. Press I.
    You hear: No next input.

### Restart Claude

1. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
2. Press Enter.
    You hear: Restarting.
    You hear: Ready.
3. Press Alt+2, Ctrl+End.
4. Press Shift+I.
    You hear: Input 1 heading level 1.
5. Press Shift+I.
    You hear: Input 2 from previous session heading level 1.
6. Press Alt+1 (Message field; Navigation menu).
7. Type: Reply with the single word four.
8. Press Enter.
    You hear: Done.
9. Press Alt+2, Ctrl+End.
10. Press Shift+I.
    You hear: Input 2 heading level 1.

### Start a new session with arguments of your own

1. Press Ctrl+N (New session; Session menu).
2. Press Down 2 times.
3. Press Tab 3 times.
4. Press Shift+Tab.
5. Press Up 4 times.
6. Press Down 4 times.
7. Press Tab.
8. Press Ctrl+A, Delete.
9. Type: --model haiku
10. Press Shift+Enter.
11. Type: --continue --name demo
12. Press Enter.
    You hear: New session in proj.
    You hear: Ready.
13. Press Alt+2, Ctrl+Home.
14. Press I.
    You hear: from previous session heading level 1.

## Restart, stop and close a conversation

Starts with: Open axClaude with the folder life.

### Stop Claude from the message field

1. Type: /exit
2. Press Enter.
    You hear: Claude stopped.
3. Type: hello
4. Press Enter.
    You hear: Claude stopped. Ctrl+Shift+R starts it.
5. Press Ctrl+A, Delete.

### Start a stopped Claude again

1. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
    You hear: Ready.

### Restart Claude while it runs

1. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
2. Press Escape.
3. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
4. Press Enter.
    You hear: Restarting.
    You hear: Ready.

### Decide whether to close the window while Claude works

1. Type: Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the word pinged.
2. Press Enter.
    You may hear the sound Notice.
3. If that came: Press 1, Enter.
4. Press Alt+F4.
5. Press Tab 2 times.
6. Press Escape.

### Carry on the conversation after closing

1. Open axClaude with the folder life.
    You hear: Ready.
2. Press Alt+2, Ctrl+Home.
3. Press I.
    You hear: from previous session heading level 1.

### Close the window while Claude is ready

1. Close the window.

## Choose axClaude's speech, sounds and text

Starts with: Open axClaude with the folder opts.

### Change the text size

1. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size 10.
2. Choose Smaller text in the Options menu, Text (key: Ctrl+Minus).
    You hear: Text size 9.
3. Choose Larger text in the Options menu, Text (key: Ctrl+Plus).
    You hear: Text size 10.

### Turn off the ticks for new lines and tool calls

1. Choose Ticks for new lines and tool calls in the Options menu, Sounds.
2. Type: Use the Bash tool to run: echo hi. Then reply with the single word again.
3. Press Enter.
    You hear the sound Sent.
    You hear: Done.
4. Choose Ticks for new lines and tool calls in the Options menu, Sounds.

### Turn off the chimes

1. Choose Chimes: ready, sent, replying, done, question in the Options menu, Sounds.
2. Type: Reply with the single word quiet.
3. Press Enter.
    You hear the sound Click.
    You hear: Done.
4. Choose Chimes: ready, sent, replying, done, question in the Options menu, Sounds.

### Play background music while Claude works

1. Press Ctrl+M (Background music on or off).
    You hear: Background music on, nothing is running.
2. Type: Use the Bash tool to run: echo hi. Then reply with the single word music.
3. Press Enter.
    You hear the sound StartMusic.
    You hear the sound StopMusic.
    You hear the sound Done.
4. Press Ctrl+M (Background music on or off).
    You hear: Background music off.
5. Type: Write a story of 1500 words about a lighthouse keeper.
6. Press Enter.
    You hear: Sent.
7. Press Alt+2 (Conversation; Navigation menu).
8. Press Ctrl+M (Background music on or off).
    You hear the sound StartMusic.
    You hear: Background music on.
9. Press Ctrl+M (Background music on or off).
    You hear the sound StopMusic.
    You hear: Background music off.
    You hear: Done.

### Stop recording the raw stream

1. Choose the menu item Stop recording.
    You hear: Recording stopped.
2. Press Alt+2 (Conversation; Navigation menu).

### Have Claude's replies spoken

1. Choose Speak replies: all lines in the Options menu, Speech.
    You hear: Speak replies: all lines.
2. Press Alt+1 (Message field; Navigation menu).
3. Type: Markdown only: ## Alpha, a five-word sentence, ## Beta, three one-word bullets.
4. Press Enter.
    You hear: claude: Alpha.
    You hear: Done.
5. Choose Speak replies: first lines in the Options menu, Speech.
    You hear: Speak replies: first lines.
6. Type: Reply with two lines: the word first, then on the next line the word second.
7. Press Enter.
    You hear: claude: first.
    You hear: Done.

### Hear each tool call as it starts

1. Choose Speak replies: none in the Options menu, Speech.
    You hear: Speak replies: none.
2. Choose Speak each tool call in the Options menu, Speech.
3. Type: Use the Bash tool to run: echo hi. Then reply with the single word tooled.
4. Press Enter.
    You hear: tool: Bash.
    You hear: Done.
5. Choose Speak each tool call in the Options menu, Speech.

### Read the keyboard shortcuts

1. Press F1 (Keyboard shortcuts; Help menu).
2. Press H.
    You hear: Anywhere, heading level 2.
3. Press H.
    You hear: Message field, heading level 2.
4. Press Shift+H.
    You hear: Anywhere, heading level 2.
5. Press 2.
    You hear: Message field, heading level 2.

### Find a word in the keyboard shortcuts

1. Press Ctrl+F (Find; Navigation menu).
2. Press Ctrl+A.
3. Type: palette
4. Press Enter.
5. Press F3 (Find next; Navigation menu).
6. Press Shift+F3 (Find previous; Navigation menu).
7. Press Ctrl+F (Find; Navigation menu).
8. Press Escape.

### Read the user guide

1. Choose User guide in the Help menu.
2. Press H.
    You hear: heading level 2.
3. Press Shift+H.
    You hear: heading level.
4. Press Ctrl+End, H.
    You hear: No next heading.
5. Press Escape.

### Read the version, the licence and the axKit guide

1. Choose Version in the Help menu, About.
2. Press Escape.
3. Choose Licence in the Help menu, About.
4. Press Escape.
5. Choose Attribution in the Help menu, About.
6. Press Escape.
7. Choose axKit guide in the Help menu, About.
8. Press H.
    You hear: Install, heading level 2.
9. Press H.
    You hear: Start.
10. Press H.
    You hear: If a key does nothing.
11. Press H.
    You hear: Update.
12. Press Escape.

### Show the time on Input and Output lines

1. Choose Time stamps on Input and Output lines in the Options menu, Text.
2. Press Alt+1 (Message field; Navigation menu).
3. Type: Reply with the single word stamped.
4. Press Enter.
    You hear: Done.
5. Press Alt+2, Ctrl+End.
6. Press Shift+I.
    You hear words such as: Input 11.
7. Press Shift+O.
    You hear: Output 10 Reply from Claude heading level 1.
8. Press O.
    You hear words such as: Output 11 Reply from Claude.
9. Choose Time stamps on Input and Output lines in the Options menu, Text.

## Install Claude Code when it is missing

Starts with: Open axClaude with the folder gone --claude C:\nowhere\claude.exe.

### Hear the ways to install Claude Code

1. Press Tab 5 times.

### Close the notice that Claude Code is missing

1. Press Enter.
2. Press Alt+2 (Conversation; Navigation menu).

## Check for an update from a conversation

Starts with: Open axClaude with the folder upd.

### Check for an update

1. Choose the menu item Check for updates.
2. Press Escape.
3. Choose Version in the Help menu, About.
4. Press Escape.

### Turn off the update check at start

1. Choose Check for updates at start in the Options menu, General.
2. Close the window.
3. Open axClaude with the folder upd.
    You hear: Ready.
4. Choose Version in the Help menu, About.
5. Press Escape.

## Answer Claude's questions and screens

Starts with: Open axClaude with the folder ask.

### Answer the trust question from the list

The focus moves to Message to Claude.
You hear the sound Notice.
The focus moves to Permission Required: Accessing workspace.
You hear: Permission Required: Accessing workspace.
1. Press Y, Enter.
    You hear: Sent: y, Yes, I trust this folder.
    The focus moves to Message to Claude.
    You hear: Ready.

### Move between a question and the conversation

1. Type: Use your AskUserQuestion tool once to ask me which flower I like: rose, tulip or daisy.
2. Press Enter.
    You hear the sound Notice.
3. Press Ctrl+Tab 2 times (Switch between the view and the field).
4. Press Tab.
5. Press Shift+Tab.

### Hear the question again

1. Press Down 2 times.
2. Press Ctrl+Q (Claude's question again; Navigation menu).
    You hear: 3. Daisy.
3. Press Ctrl+Tab (Switch between the view and the field).
4. Press Ctrl+Q (Claude's question again; Navigation menu).
    You hear: 3.

### Cancel a question

1. Press Escape.
    You hear: Cancelled.
    You hear: Done.

### Answer a question that follows a reply

1. Type: Write two sentences about gardens, then use your AskUserQuestion tool to ask me which flower I like: rose, tulip or daisy.
2. Press Enter.
    You hear: Sent.
3. Press Alt+2 (Conversation; Navigation menu).
    You hear the sound Notice.
4. Press Ctrl+Tab 2 times (Switch between the view and the field).
5. Press 1, Enter.
    You hear: Sent: 1.
    You hear: Done.

### Have questions come without interrupting speech

1. Choose Go to them at once, interrupting speech in the Options menu, Questions.
2. Press Alt+1 (Message field; Navigation menu).
3. Type: Write two sentences about gardens, then use your AskUserQuestion tool to ask me which flower I like: rose, tulip or daisy.
4. Press Enter.
    You hear: Sent.
5. Press Alt+2 (Conversation; Navigation menu).
    You hear the sound Notice.
6. Press 1, Enter.
    You hear: Sent: 1.
    You hear: Done.
7. Choose Go to them at once, interrupting speech in the Options menu, Questions.

### Open and close Claude's list of background tasks

1. Type: /tasks
2. Press Enter.
    You may hear: Claude asks.
3. If that came: Press Shift+Escape.
    You may hear the sound Notice.
4. If that came: Press Escape.
    You hear: Closed.

### Answer a question in your own words

1. Type: Use the AskUserQuestion tool to ask me which flower I enjoy thinking about, with three options.
2. Press Enter.
    You hear the sound Notice.
3. Press 4, Enter.
    You hear: Sent: 4, Other. Your words next.
4. Type: tulips in the garden by the old stone wall where the morning light falls first and the bees arrive early
5. Press Enter.
    You hear: Sent: tulips in the garden by the old stone wall where the morning light falls first and the bees arrive early.
    You hear: Done.
6. Press Alt+2, Ctrl+End.
7. Press Alt+1 (Message field; Navigation menu).
8. Type: Use the AskUserQuestion tool to ask me which flower I enjoy thinking about, with three options.
9. Press Enter.
    You hear the sound Notice.
10. Press 4, Enter.
11. Press Escape.
12. Press 1, Enter.
    You hear: Done.

### Move through Claude's questions in the conversation

1. Press Alt+2, Ctrl+Home.
2. Press Q.
    You hear: Question: Permission Required: Accessing workspace.
3. Press Q.
    You hear words such as: Question.
4. Press Ctrl+End.
5. Press Q.
    You hear: No next question.
6. Press Shift+Q.
    You hear words such as: Question.

### Close a question without answering it

1. Press Alt+1 (Message field; Navigation menu).
2. Type: Use your AskUserQuestion tool once to ask me which flower I like: rose, tulip or daisy.
3. Press Enter.
    You hear the sound Notice.
4. Press Shift+Escape.

### Interrupt Claude from a permission question

1. Press Ctrl+Shift+M (Next permission mode; Actions menu).
    You hear: manual mode on.
2. Type: Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the word pinged.
3. Press Enter.
    You hear the sound Notice.
4. Press Shift+Escape.
5. Press Ctrl+Shift+M 3 times.
    You hear: auto mode on.

### Answer a set of two questions

1. Type: Reply with one sentence about tulips, then use the AskUserQuestion tool to ask me two questions, which colour I prefer and which season.
2. Press Enter.
    You hear the sound Notice.
3. Press Enter.
    You hear: Next question.
4. Press Enter.
    You hear: Done.
5. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
6. Press Enter.
    You hear: Ready.

### Open and close Claude's permissions screen

1. Type: /permissions
2. Press Enter.
    You hear the sound Notice.
3. Press Escape.
    You hear: Closed.

### Answer a question with several answers

1. Press Alt+1 (Message field; Navigation menu).
2. Type: Use the AskUserQuestion tool once with two questions: my favourite colour (red, green, blue), and, with multiSelect, which fruits I like (apple, banana, cherry).
3. Press Enter.
    You hear the sound Notice.
4. Press 2, Enter.
    You hear: Sent: 2, Green. Next question.
5. Press Home, Space, Down, Down, Space, Enter.
    You hear: Sent: 1, Apple, 3, Cherry.
    You hear: Done.
6. Press Alt+2, Ctrl+End.
7. Press Alt+1 (Message field; Navigation menu).

### Review answers before they go to Claude

1. Choose Review answers before they go to Claude in the Options menu, Questions.
2. Type: Use the AskUserQuestion tool once with two questions: my favourite colour (red, green, blue), and, with multiSelect, which fruits I like (apple, banana, cherry).
3. Press Enter.
    You hear the sound Notice.
4. Press 2, Enter.
    You hear: Next question.
5. Press Home, Space, Enter.
    You hear: Review next.
6. Press Y, Enter.
    You hear: Done.
7. Choose Review answers before they go to Claude in the Options menu, Questions.

## Use the axKit menu from a conversation

Starts with: Open axClaude with the folder menu.

### Close every window at once

1. Open axDown with the file notes.md.
2. Press Ctrl+End.
3. Type: more
4. Go to the axClaude window.
5. Press Alt+1 (Message field; Navigation menu).
6. Type: Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the word pinged.
7. Press Enter.
    You hear: Sent.
    You may hear the sound Notice.
8. If that came: Press 1, Enter.
9. Choose Close all windows in the Window menu.
10. Go to the axDown window.
11. Close the window.
12. Press Tab 2 times.
13. Press Enter.
14. Go to the axClaude window.
15. Close the window.
16. Press Enter.

## Read a side answer and other information screens

Starts with: Open axClaude with the folder side.

### Ask a side question and hear the answer

1. Type: /btw what is two plus two? answer in one word
2. Press Enter.
    You hear the sound Notice.

### Close a side answer

1. Press Escape.
    You hear: Closed.

### Browse earlier side answers

1. Type: /btw what is three plus three? answer in one word
2. Press Enter.
    You hear the sound Notice.
3. Press Tab 2 times.
4. Press Enter.
5. Press Escape.
    You hear: Closed.

### Look at Claude's goal

1. Type: /goal
2. Press Enter.
    You hear the sound Notice.
3. Press Enter.
    You hear: Closed.

### Find side answers in the conversation

1. Press Alt+2, Ctrl+Home.
2. Press B.
    You hear: btw: what is two plus two? answer in one word.
3. Press B.
    You hear: btw: what is three plus three? answer in one word.
4. Press B.
    You hear: No next btw.
5. Press Shift+B.
    You hear: btw: what is two plus two? answer in one word.

## Agree to the reminder before Claude Code first starts

Starts with: Open axClaude with the folder fresh.

### Agree to the reminder before Claude Code starts

You hear: Before axClaude connects to Claude Code.
1. Press Tab.
    You hear: I understand that axClaude.
2. Press Enter.
    You hear: Check the box first.
3. Press Space.
    You hear: checked.
4. Press Enter.
    You hear: trust.
5. Press Enter.
    You hear: Ready.
6. Close the window.

## Report a bug or an idea from a conversation

Starts with: Open axClaude with the folder report.

### Open the report form

1. Press F4 (Send a report; Help menu).
    You hear: A report goes to axKit's maintainer. It is encrypted on your computer.

### Type a report of several lines

1. Type: The list loses my place
2. Press Enter.
    You hear: new line.
3. Type: every time

### Add detail that may name your files

1. Press Shift+Tab.
2. Press Space.
    You hear: checked.
3. Press Tab.
4. Press Shift+Tab.
5. Press Space.
    You hear: not checked.
6. Press Tab.

### Send the report

1. Press Shift+Tab 5 times.
2. Press Ctrl+Enter (Send the message).
    You may hear: Saved.
3. Press Escape.

### Cancel a report

1. Press F4 (Send a report; Help menu).
2. Press Escape.
    You hear: Cancelled.
3. Press F4 (Send a report; Help menu).
4. Type: Draft words
5. Press Escape.
    You hear: Cancelled.

## Choose which words axClaude says by itself

Starts with: Open axClaude with the folder spoken.

### Silence Claude's own notices

1. Press Ctrl+Shift+M (Next permission mode; Actions menu).
2. Choose Claude's own notices, such as "accept edits on" in the Options menu, Announcements.
3. Press Ctrl+Shift+M (Next permission mode; Actions menu).
    You hear: plan mode on.
4. Press Ctrl+Shift+M 2 times (Next permission mode; Actions menu).

### Turn the words Sent and Responding back on

1. Choose "Sent" and "Queued" after a message in the Options menu, Announcements.
2. Choose "Responding" when a reply starts in the Options menu, Announcements.
3. Type: Reply with the single word two.
4. Press Enter.
    You hear: Sent.
    You hear: Responding.
    You hear: Done.

### Keep reply lines with chosen words silent

1. Choose Speak replies: all lines in the Options menu, Speech.
    You hear: Speak replies: all lines.
2. Choose Words that keep a reply line silent in the Options menu, Speech.
3. Type: pelican; walrus
4. Press Enter.
    You hear: 2 words keep a line silent.
5. Type: Reply with exactly two lines and nothing else: first line the words pelican feathers, second line the words green apples.
6. Press Enter.
    You hear: green apples.
    You hear: Done.
7. Press Alt+2, Ctrl+End.

### Have every reply line spoken again

1. Choose Words that keep a reply line silent in the Options menu, Speech.
2. Press Ctrl+A, Delete, Enter.
    You hear: Every reply line is spoken.
3. Press Alt+1 (Message field; Navigation menu).
4. Type: Reply with exactly one line: the words pelican again.
5. Press Enter.
    You hear: pelican again.
    You hear: Done.

## Run a command Claude offers you

Starts with: Open axClaude with the folder offer.

### Read the explanation of an offered command

1. Call the tool axkit_offer_command.
    The tool answers with offered: true.
    You hear the sound Offer.
2. Press Alt+2 (Conversation; Navigation menu).
3. Press Alt+1 (Message field; Navigation menu).
4. Press Enter 2 times.

### Hear the offered command again

1. Press Ctrl+Q (Claude's question again; Navigation menu).
    You hear: offers a command for you to run: echo offered-first. Written by axAutomate, an AI: read the explanation before you run it. Its reason: A check of the question.

### Turn down an offered command

1. Press Up 2 times.
2. Press Enter.
    You hear: Not run.

### Run an offered command

1. Call the tool axkit_offer_command.
    The tool answers with offered: true.
2. Press Down.
3. Press Enter.
    You hear: Sent.
4. Press Alt+2 (Conversation; Navigation menu).

### Let an AI tool run a window command

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands.
2. Call the tool axkit_window_run.
    The tool answers: The window asks the user whether to run Check for updates at start.
    You hear the sound Offer.
3. Press Down.
4. Press Enter.
    You hear: axAutomate ran Check for updates at start.

## See an offered command wait its turn

Starts with: Open axClaude with the folder waits.

### Turn down waiting offers one after another

1. Press Escape 2 times.
    You hear: Not run.
2. Press Escape.
    You hear: Not run.
3. Press Escape.
    You hear: Not run.

## A conversation window that starts hidden and hides on closing

Starts with: Open axClaude with the folder home\axKit Claude --title axHelp --hide-on-close --hidden.

### Bring up the hidden conversation

1. Open axHelp.
2. Choose Open the conversation in the Actions menu (key: Enter).
3. Go to the axClaude window.
    You may hear: trust.
4. If that came: Press Enter.
    You hear: Ready.

### Hide the conversation again

1. Close the window.
2. Go to the axHelp window.
3. Choose Open the conversation in the Actions menu (key: Enter).
4. Go to the axClaude window.

### End Claude and close the window for good

1. Choose the menu item End Claude and close this window.

## See what happened while a hidden conversation was away

Starts with: Open axClaude with the folder away --title axHelp --hide-on-close. Open axHelp.

### Bring up the window and hear what happened while it was away

1. Go to the axHelp window.
2. Choose Open the conversation in the Actions menu (key: Enter).
3. Go to the axClaude window.
    You hear: Done.
    You hear the sound Offer.
4. Press Escape.
    You hear: Not run.

## Hear and ask what Claude runs in the background

Starts with: Open axClaude with the folder bg.

### Ask what runs in the background

You hear the sound BackgroundStart.
1. Press Ctrl+B (What runs in the background; Actions menu).
    You hear: 3 in the background: 1 shell, 2 agents. general-purpose: Checking callers. Explore: Reading the spec.
2. Press F8 (Say the status; Window menu).
    You hear: Claude: ready | 3 in the background: 1 shell, 2 agents | auto mode on.
3. Choose What runs in the background in the Actions menu (key: Ctrl+B).
    You hear: 3 in the background: 1 shell, 2 agents. general-purpose: Checking callers. Explore: Reading the spec.
    You hear the sound BackgroundChord 3.

### Hear background work as it starts and ends

1. Choose Say background work as it starts and ends in the Options menu, Background work.
    You hear the sound BackgroundFinish.
    You hear: Checking callers finished.
    You hear the sound BackgroundChord 2.

## Find out whether a key arrives and what it does, in axClaude

Starts with: Open axClaude with the folder kt.

### Hear what a key does without doing it

1. Press F8 (Say the status; Window menu).
    You hear: F8: Say the status.
2. Press F4 (Send a report; Help menu).
    You hear: F4: Send a report.
3. Press Ctrl+B (What runs in the background; Actions menu).
    You hear: Ctrl+B: What runs in the background.
4. Press Alt+2 (Conversation; Navigation menu).
    You hear: Alt+2: Conversation.
5. Press Ctrl+2 (Conversation; Navigation menu).
    You hear: Ctrl+2: Conversation.
6. Press Shift+Escape.
    You hear: Shift+Escape: Interrupt.
7. Press A.
    You hear: A: types the letter.
8. Press Enter.
    You hear: Enter: sends what the field holds.
9. Press F9.
    You hear: F9: no command of this window.

### End the key test

1. Press Escape.
    You hear: Key test ended.

### Hear what the conversation's keys do

1. Choose Test a key in the Help menu, Diagnostics.
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
    You hear: Line.

## Read back in the conversation while Claude writes on, and find every line in its place

Starts with: Open axClaude with the folder order.

## Read back a long message before sending it, and turn the music on with Ctrl+M

Starts with: Open axClaude with the folder em.

### Move through a message by its headings, links and lists

1. Press Ctrl+Space.
    You hear: Navigation mode.
2. Press K.
    You hear: https://example.com, link.
3. Press I.
    You hear: one.
4. Press I.
    You hear: two.
5. Press Shift+H.
    You hear: Plan, heading level 1.

### Go back to editing the message

1. Press Alt+1 (Message field; Navigation menu).
2. Press Ctrl+Space.
    You hear: Edit mode.
3. Press End.
4. Type: !

### Turn the background music on and off

1. Press Ctrl+M (Background music on or off).
    You hear: Background music on, nothing is running.
2. Press Alt+2 (Conversation; Navigation menu).
3. Press Ctrl+M (Background music on or off).
    You hear: Background music off.

## Block Claude Code's feedback requests from the Options menu, and turn it off again

Starts with: Open axClaude with the folder cs.

### Block Claude Code's feedback requests

1. Choose Block Claude Code's feedback requests in the Options menu, Claude Code settings.
    You hear: Claude Code's feedback requests blocked, applies at once.

### Turn the block on feedback requests off again

1. Choose Block Claude Code's feedback requests in the Options menu, Claude Code settings.
    You hear: Claude Code's feedback requests back to Claude Code's own default, applies at once.

### Choose how Claude-drafted feedback comes

1. Choose Claude-drafted feedback: quiet, counted without a card in the Options menu, Claude Code settings.
    You hear: Claude-drafted feedback: quiet, counted without a card, applies at once.
2. Choose Claude-drafted feedback: a card for each, Claude Code's default in the Options menu, Claude Code settings.
    You hear: Claude-drafted feedback: a card for each, Claude Code.

## Follow a workflow Claude runs in the background without hearing its live row

Starts with: Open axClaude with the folder wf.

### Hear how far a workflow is

1. Press Ctrl+B (What runs in the background; Actions menu).
    You hear: 1 workflow in the background. Workflow axkit-design-map-and-verify: 88 percent, 8 of 9 done, 20m18s.
2. Press F8 (Say the status; Window menu).
    You hear: 1 workflow in the background.

### Show Claude Code's live progress rows in the conversation

1. Choose Show Claude Code's live progress rows in the conversation in the Options menu, Background work.
    You hear the sound BackgroundStart.
2. Press Alt+2 (Conversation; Navigation menu).
3. Press Ctrl+End.
    The focus moves to Conversation.

### Hide the live progress rows again

1. Choose Show Claude Code's live progress rows in the conversation in the Options menu, Background work.

## Hear where axClaude's events come from

Starts with: Open axClaude with the folder es.

### Hear where the window's events come from

1. Choose Say the event sources in the Options menu, Developer.
    You hear: Events. Marks: 0. Hooks: not passed, version. Session file: not found. Core: in charge of state.

### Turn the grounding in Claude Code's session file off and on again

1. Choose Ground the conversation in Claude Code's session file in the Options menu, Developer.
    You hear: Grounding in Claude Code's session file off.
2. Choose Say the event sources in the Options menu, Developer.
    You hear: Events. Marks: 3. Hooks: not passed, version. Session file: off. Core: in charge of state.
3. Choose Ground the conversation in Claude Code's session file in the Options menu, Developer.
    You hear: Grounding in Claude Code's session file on.
4. Choose Say the event sources in the Options menu, Developer.
    You hear: Events. Marks: 3. Hooks: not passed, version. Session file: not found. Core: in charge of state.

### Turn Claude Code's events off and on again

1. Choose Events from Claude Code's hooks in the Options menu, Developer.
    You hear: Events from Claude Code's hooks off, from the next start of Claude.
2. Choose Say the event sources in the Options menu, Developer.
    You hear: Events. Marks: 3. Hooks: not passed, version. Session file: not found. Core: in charge of state.
3. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
4. Press Enter.
    You hear: Restarting.
    You hear: Ready.
5. Choose Say the event sources in the Options menu, Developer.
    You hear: Events. Marks: 0. Hooks: off. Session file: not found. Core: in charge of state.
6. Choose Events from Claude Code's hooks in the Options menu, Developer.
    You hear: Events from Claude Code's hooks on, from the next start of Claude.

## Follow a turn played by the stand-in

Starts with: Open axClaude with the folder st.

You hear: Ready.
1. Type: Say ok
2. Press Enter.
    You hear: Sent.
    You hear: Done.
3. Press Alt+2 (Conversation; Navigation menu).

## See Claude Code's events arrive, without Claude Code

Starts with: Open axClaude with the folder a.

### Hear whether Claude Code's events reach the window

1. Choose Say the event sources in the Options menu, Developer.
    You hear: Hooks: on, 1 received.

### Turn Claude Code's events off and on again

1. Go to the other axClaude window.
2. Choose Events from Claude Code's hooks in the Options menu, Developer.
    You hear: Events from Claude Code's hooks off, from the next start of Claude.
3. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
4. Press Enter.
    You hear: Restarting.
    You hear: Ready.
5. Choose Say the event sources in the Options menu, Developer.
    You hear: Hooks: off.
6. Choose Events from Claude Code's hooks in the Options menu, Developer.
    You hear: Events from Claude Code's hooks on, from the next start of Claude.
7. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
8. Press Enter.
    You hear: Restarting.
    You hear: Ready.
9. Choose Say the event sources in the Options menu, Developer.
    You hear: Hooks: on, 1 received.

## Start Claude Code with axClaude's hooks

Starts with: Open axClaude with the folder hooked.

### Hear that Claude Code's events arrived after a turn

1. Type: Reply with the single word ok.
2. Press Enter.
    You hear: Sent.
    You hear: Done.
3. Choose Say the event sources in the Options menu, Developer.
    You hear words such as: Hooks: on.

## See the session file followed, without Claude Code

Starts with: Open axClaude with the folder sf.

### Hear whether Claude Code's session file is followed

1. Type: Say ok
2. Press Enter.
    You hear: Sent.
    You hear: Done.
3. Choose Say the event sources in the Options menu, Developer.
    You hear: Session file: followed, 3 entries.

### Turn the grounding in Claude Code's session file off and on again

1. Choose Ground the conversation in Claude Code's session file in the Options menu, Developer.
    You hear: Grounding in Claude Code's session file off.
2. Choose Say the event sources in the Options menu, Developer.
    You hear: Session file: off.
3. Choose Ground the conversation in Claude Code's session file in the Options menu, Developer.
    You hear: Grounding in Claude Code's session file on.
4. Choose Say the event sources in the Options menu, Developer.
    You hear: Session file: not found.

## Start axClaude with a place of Claude Code another program wrote

Starts with: Open axClaude with the folder st. Close the window.

## Know that Claude works, and send a message while a reply arrives

Starts with: Open axClaude with the folder sw.

### Know that Claude works while the screen shows no sign of it

1. Type: Say ok
2. Press Enter.
    You hear: Sent.

### Send a message while a reply's rows are still arriving

1. Type: Say more
2. Press Enter.
    You hear: Queued.

## Have a short exchange with Claude

Starts with: Open axClaude with the folder a.

You may hear: trust.
1. If that came: Press Enter.
    You hear: Ready.
2. Type: Reply with the single word hello.
3. Press Enter.
    You hear: Done.
4. Press Alt+2 (Conversation; Navigation menu).
5. Press Alt+3 (Latest response; Navigation menu).
    You hear: Output 1.

## Answer a set of two questions from Claude

Starts with: Open axClaude with the folder q.

You may hear: trust.
1. If that came: Press Enter.
    You hear: Ready.
2. Type: Use your AskUserQuestion tool once, with two questions: which colour I prefer, red or blue, and which season I prefer, summer or winter.
3. Press Enter.
    You hear: Question 1 of 2.
4. Press Enter.
    You hear: Question 2 of 2.
5. Press Enter.
