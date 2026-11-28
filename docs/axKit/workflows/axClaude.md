# axClaude: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives what axKit says there; "something with" and words in quotes give a part of it. After "Type"
the text to type stands in quotes: the quotes and the full stop after them are not typed. The file names and the
words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Start a conversation with Claude

Starts with: Open axClaude with the folder fresh.

### Move between the conversation and Claude's question, then answer it

You hear: trust.
1. Press Tab twice.

### Move between the message field and the conversation

1. Press Ctrl+Tab twice (Switch between the view and the field).

### Jump straight to the conversation or the message field

1. Press Alt+2 (Conversation; Navigation menu).
    The focus moves to Conversation (Text).

### Go to the conversation from the menu

1. Choose Conversation in the Navigation menu (key: Alt+2).

### Hear the status

1. Press F9 (Say the status; Window menu).
    You hear something with "Claude: ready" in it.

## Send Claude a message and hear the reply

Starts with: Open axClaude with the folder plain.

### Read Claude's question as lines in the conversation

You hear: Claude asks.
1. Press Alt+2 (Conversation; Navigation menu).
2. Press Ctrl+Home.
3. Press Down three times.

### Answer a question by typing in the message field

1. Press Ctrl+Tab (Switch between the view and the field).
2. Type "y".
3. Press Enter.
    You hear: Sent.
    You hear: Ready.

### Send a message of two lines

1. Type "What is 2+2?".
2. Press Shift+Enter.
3. Type "Answer in one line, with no tool.".
4. Press Enter.
    You hear the Sent sound and Sent.
    You hear the Responding sound and Responding.
    You hear the Done sound and Done.

### Go to Claude's reply and read it

1. Press Ctrl+Tab (Switch between the view and the field).
2. Press Shift+O.
    You hear: Output 1 Reply from Claude heading level 1.
3. Press Down three times.
4. Press Up seven times.

### Go back to the message you sent

1. Press Ctrl+End.
2. Press I.
    You hear: No next input.
3. Press Shift+I.
    You hear: Input 1 heading level 1.

### Move through a long message by page

1. Type "alpha".
2. Press Page Down.
3. Press Page Up.
4. Press Ctrl+A, Delete.

### Move through a message by paragraph

1. Type "one".
2. Press Shift+Enter twice.
3. Type "two".
4. Press Shift+Enter.
5. Type "three".
6. Press Shift+Enter twice.
7. Type "four".
8. Press Ctrl+Home.
9. Press Ctrl+Down twice.

### Read the keyboard shortcuts

1. Choose Keyboard shortcuts in the Help menu.
2. Press H.
    You hear: Anywhere, heading level 2.

## Read the conversation while Claude is writing

Starts with: Open axClaude with the folder read.

### Read the conversation while Claude writes

1. Type "Write the numbers 1 to 100, one per line, nothing else.".
2. Press Enter.
    You hear: Sent.
3. Press Ctrl+Tab (Switch between the view and the field).
4. Press Ctrl+Home, Down.
5. Press Down.

### Go to Claude's latest reply

1. Press Alt+3 (Latest response; Navigation menu).
    You hear: Output 1 Reply from Claude heading level 1.

### Quote a line of a reply in your next message

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Write one paragraph of 120 words about rivers, with no line breaks.".
3. Press Enter.
    You hear: Done.
4. Press Alt+3 (Latest response; Navigation menu).
    You hear: Output 2.
5. Press C.

### Select lines of a reply and clear the selection

1. Press Alt+2 (Conversation; Navigation menu).
2. Press Ctrl+Home.
3. Press O.
    You hear: Output 1.
4. Press Down twice.
5. Press Shift+Down twice.
6. Press Escape.
    You hear: Selection cleared.

## Move through a reply by headings and quick keys

Starts with: Open axClaude with the folder keys.

### Move through the conversation by headings

1. Press Alt+2 (Conversation; Navigation menu).
2. Press Ctrl+Home.
3. Press Backspace.
    You hear something with "Nothing to go back" in it.
4. Press H.
    You hear: Input 1 heading level 1.

### Move through the markers alone

1. Press Ctrl+Home.
2. Press 1.
    You hear: Input 1 heading level 1.

### Move by heading level

1. Press Ctrl+Home.
2. Press 2.
    You hear: Alpha heading level 2.
3. Press 2.
    You hear: Beta heading level 2.

### Hide the markers in the text

1. Choose Markers in the text in the Options menu, Text.
2. Press Ctrl+Home.
3. Press H.
    You hear: Input 1 heading level 1.

### Move through the conversation by paragraph

1. Press Ctrl+Home.
2. Press P.
    You hear: Output 1 Reply from Claude.
3. Press Shift+P.
    You hear: [Screen Reader Mode: on via flag].

### Jump between replies, messages, tools, errors and system lines

1. Press Ctrl+Home.
2. Press O.
    You hear: Output 1 Reply from Claude heading level 1.
3. Press R.
    You hear: Output 2 Reply from Claude heading level 1.
4. Press Shift+R.
    You hear: Output 1 Reply from Claude heading level 1.

### Jump between your messages

1. Press I.
    You hear: Input 2 heading level 1.
2. Press Shift+I.
    You hear: Input 1 heading level 1.

### Jump to Claude's lines and to the tool lines

1. Press C.
    You hear: claude: Alpha heading level 2.
2. Press T.
    You hear: hi.

### Go back to the message field from the conversation

1. Press Alt+2 (Conversation; Navigation menu).
2. Press Escape.

### Move through a long reply by page

1. Type "Write 60 numbered lines, 1. to 60., each with just its number, nothing else.".
2. Press Enter.
    You hear: Sent.
    You hear: Done.
3. Press Ctrl+Tab (Switch between the view and the field).
4. Press Page Up.
5. Press Page Down twice.
6. Press Ctrl+Home, Page Up.

### Skip over a run of lines in a reply

1. Press Alt+3 (Latest response; Navigation menu).
    You hear: Output 3 Reply from Claude heading level 1.
2. Press Ctrl+Down twice.
3. Press Ctrl+Up twice.
4. Press Ctrl+Shift+Down.
5. Press Escape.
    You hear: Selection cleared.

### Hear the line number

1. Press L.
    You hear: Line.

### Find text in the conversation

1. Press Ctrl+F (Find; Navigation menu).
2. Type "single word".
3. Press Enter.
    You hear: From the top, Use the Bash tool.

### Find the next match from the message field

1. Press Alt+1 (Message field; Navigation menu).
2. Press Ctrl+F (Find; Navigation menu).
3. Type "single word".
4. Press Enter.
5. Press Alt+1 (Message field; Navigation menu).
6. Press F3 (Find next; Navigation menu).
    You hear: Use the Bash tool.

### Go back to where you were before a jump

1. Press Ctrl+Home.
2. Press H, H, H, P, P.
3. Press Backspace.
    You hear something with "Back to" in it.

### Bookmark lines and move between them

1. Press Alt+3 (Latest response; Navigation menu).
2. Press Down three times.
3. Press M.
    You hear: Bookmark 1 added.

### Move between the bookmarks

1. Press Ctrl+Home.
2. Press K.
    You hear: Bookmark 1, 2.
3. Press K.
    You hear: Bookmark 2, 5.

### Remove a bookmark

1. Press Down.
2. Press M.
    You hear: Bookmark 1 removed.

### Bookmark a line from the message field

1. Press Alt+1 (Message field; Navigation menu).
2. Press Ctrl+K, or M in the conversation (Bookmark this line; Context menu).
    You hear: Bookmark 3 added.

### Hear a reply's paragraphs, headings or bookmarks without moving

1. Press Alt+3 (Latest response; Navigation menu).
2. Press Ctrl+Alt+P.
    You hear: claude: 1.

### Quote two lines in a message and send it

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Reply with the single word ok.".
3. Press Alt+2, Ctrl+Home.
4. Press I.
5. Press Enter.
    You hear: copied.
6. Press Alt+2, Ctrl+Home.
7. Press C.
8. Press Enter.
9. Press Ctrl+Home.
10. Press Down twice.
11. Press Enter.
    You hear: Sent.
    You hear: Done.

### Quote a long paragraph whole

1. Press Enter.
    You hear: copied.

### Send the quote with your comment

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Reply with the single word yes.".
3. Press Enter.
    You hear: Sent.
    You hear: Done.
4. Press Alt+2 (Conversation; Navigation menu).

## Change the permission mode and interrupt Claude

Starts with: Open axClaude with the folder modes.

### Change the permission mode

1. Press Ctrl+P (Next permission mode; Actions menu).
    You hear: accept edits on.

### Read a permission question in the conversation

1. Type "Create a file named a.txt containing hi, with the Write tool.".
2. Press Enter.
    You hear: Sent.
    You hear: Claude asks.
    You hear the sound Question.
3. Press Alt+2, Ctrl+End.
4. Press Up seven times.

### Answer a permission question by its number

1. Press Ctrl+Tab (Switch between the view and the field).
2. Type "1".
3. Press Enter.
    You hear: Sent.
    You hear: Done.

### Interrupt Claude while it works

1. Type "Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the single word pinged.".
2. Press Enter.
    You hear: Claude asks.
3. Type "1".
4. Press Enter.
    You hear: Sent.
5. Press Escape.
    You hear: Ctrl+I interrupts.
6. Press Ctrl+O.
    You hear: Ctrl+O is off. For tool output, start with --verbose.
7. Press Ctrl+I (Interrupt; Actions menu).

### Interrupt Claude from the menu or the message field

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Write a story of 1500 words about a lighthouse keeper.".
3. Press Enter.
    You hear: Sent.
    You hear: Responding.
4. Choose Ctrl+C in the Actions menu, Send key.

### Send Claude a key from the menu

1. Choose Shift+Up in the Actions menu, Send key.

## Send messages while Claude is still working

Starts with: Open axClaude with the folder queue.

### Send a message while Claude works

1. Type "Use your Bash tool to run: ping -n 20 127.0.0.1 then reply with the words OK DONE.".
2. Press Enter.
    You hear: Sent.
    You may hear the sound Notice.
3. If that came: Press 1, Enter.
    You hear: Sent: 1.
4. Type "Reply with the single word second.".
5. Press Enter.
    You hear: Queued.
    You hear the sound Sent.

### Send a waiting message at once

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Use your Bash tool to run: ping -n 20 127.0.0.1 then reply with the words OK DONE.".
3. Press Enter.
    You hear: Sent.
    You may hear the sound Notice.
4. If that came: Press 1, Enter.
    You hear: Sent: 1.
5. Type "Reply with the single word fourth.".
6. Press Enter.
    You hear: Queued.
7. Press Ctrl+Shift+Enter (Send queued messages now; Actions menu).
    You hear: Sending now.

## Change Claude's project folder

Starts with: Open axClaude with the folder proj. Close the window.

### Continue the last conversation in a folder

1. Open axClaude with the folder proj, taking up the last conversation.
    You hear: Ready.

### Restart Claude

1. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
2. Press Enter.
    You hear: Restarting.
    You hear: Ready.

### Start a new session with arguments of your own

1. Press Ctrl+N (New session; Session menu).
2. Press Down twice.
3. Press Tab three times.

### Type the arguments and start the session

1. Type "--model haiku".
2. Press Shift+Enter.
3. Type "--continue --name demo".
4. Press Enter.
    You hear: New session in proj.
    You hear: Ready.

## Restart, stop and close a conversation

Starts with: Open axClaude with the folder life.

### Stop Claude from the message field

1. Type "/exit".
2. Press Enter.
    You hear: Claude stopped.
3. Type "hello".
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

1. Type "Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the word pinged.".
2. Press Enter.
    You may hear the sound Notice.
3. If that came: Press 1, Enter.
4. Press Alt+F4.
5. Press Tab twice.
6. Press Escape.

### Carry on the conversation after closing

1. Open axClaude with the folder life, taking up the last conversation.
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
2. Type "Use the Bash tool to run: echo hi. Then reply with the single word again.".
3. Press Enter.
    You hear the sound Sent.
    You hear: Done.

### Turn off the chimes

1. Choose Chimes: ready, sent, replying, done, question in the Options menu, Sounds.
2. Type "Reply with the single word quiet.".
3. Press Enter.
    You hear the sound Click.
    You hear: Done.

### Play background music while Claude works

1. Press Ctrl+M (Background music on or off).
    You hear: Background music on, nothing is running.
2. Type "Use the Bash tool to run: echo hi. Then reply with the single word music.".
3. Press Enter.
    You hear the sound StartMusic.
    You hear the sound StopMusic.
    You hear the sound Done.
4. Press Ctrl+M (Background music on or off).
    You hear: Background music off.

### Stop recording the raw stream

1. Choose the menu item Stop recording.
    You hear: Recording stopped.
2. Press Alt+2 (Conversation; Navigation menu).

### Have Claude's replies spoken

1. Choose Speak replies: all lines in the Options menu, Speech.
    You hear: Speak replies: all lines.
2. Press Alt+1 (Message field; Navigation menu).
3. Type "Markdown only: ## Alpha, a five-word sentence, ## Beta, three one-word bullets.".
4. Press Enter.
    You hear: Done. claude: Alpha.

### Have only the first line of each reply spoken

1. Choose Speak replies: first lines in the Options menu, Speech.
    You hear: Speak replies: first lines.
2. Type "Reply with two lines: the word first, then on the next line the word second.".
3. Press Enter.
    You hear: Done. claude: first.

### Hear each tool call as it starts

1. Choose Speak replies: none in the Options menu, Speech.
    You hear: Speak replies: none.
2. Choose Speak each tool call in the Options menu, Speech.
3. Type "Use the Bash tool to run: echo hi. Then reply with the single word tooled.".
4. Press Enter.
    You hear: tool: Bash.
    You hear: Done.

### Read the keyboard shortcuts

1. Choose Keyboard shortcuts in the Help menu.
2. Press H.
    You hear: Anywhere, heading level 2.

### Find a word in the keyboard shortcuts

1. Press Ctrl+F (Find; Navigation menu).
2. Press Ctrl+A.
3. Type "palette".
4. Press Enter.

### Read the user guide

1. Choose User guide in the Help menu.
2. Press H.
    You hear: heading level 2.

### Read the version, the licence and the axKit guide

1. Choose Version in the Help menu, About.
2. Press Escape.

### Read the licence

1. Choose Licence in the Help menu, About.
2. Press Escape.

### Read the attribution

1. Choose Attribution in the Help menu, About.
2. Press Escape.

### Read the axKit guide

1. Choose axKit guide in the Help menu, About.
2. Press H.
    You hear: Install, heading level 2.

### Show the time on Input and Output lines

1. Choose Time stamps on Input and Output lines in the Options menu, Text.
2. Press Alt+1 (Message field; Navigation menu).
3. Type "Reply with the single word stamped.".
4. Press Enter.
    You hear: Done.

## Install Claude Code when it is missing

Starts with: Open axClaude with the folder gone.

### Hear the ways to install Claude Code

1. Press Tab five times.

### Close the notice that Claude Code is missing

1. Press Enter.
2. Press Alt+2 (Conversation; Navigation menu).

## Check for an update from a conversation

Starts with: Open axClaude with the folder upd.

### Check for an update

1. Choose the menu item Check for updates.
2. Press Escape.

### Turn off the update check at start

1. Choose Check for updates at start in the Options menu.

## Answer Claude's questions and screens

Starts with: Open axClaude with the folder ask.

### Answer the trust question from the list

The focus moves to Message to Claude.
You hear the sound Notice.
You hear: Question.
The focus moves to Permission Required: Accessing workspace.
1. Press Enter.
    You hear: Sent: y, Yes, I trust this folder.
    The focus moves to Message to Claude.
    You hear: Ready.

### Move between a question and the conversation

1. Type "Use your AskUserQuestion tool once to ask me which flower I like: rose, tulip or daisy.".
2. Press Enter.
    You hear the sound Notice.
3. Press Ctrl+Tab twice (Switch between the view and the field).

### Hear the question again

1. Press Down twice.
2. Press F7 (Claude's question again; Navigation menu).
    You hear: 3. Daisy.

### Cancel a question

1. Press Escape.
    You hear: Cancelled.
    You hear: Done.

### Answer a question that follows a reply

1. Type "Write two sentences about gardens, then use your AskUserQuestion tool to ask me which flower I like: rose, tulip or daisy.".
2. Press Enter.
    You hear: Sent.
3. Press Alt+2 (Conversation; Navigation menu).
    You hear the sound Notice.
4. Press Ctrl+Tab twice (Switch between the view and the field).
5. Press Home.
6. Press Enter.
    You hear: Sent: 1.
    You hear: Done.

### Have questions come without interrupting speech

1. Choose Go to them at once, interrupting speech in the Options menu, Questions.
2. Press Alt+1 (Message field; Navigation menu).
3. Type "Write two sentences about gardens, then use your AskUserQuestion tool to ask me which flower I like: rose, tulip or daisy.".
4. Press Enter.
    You hear: Sent.
5. Press Alt+2 (Conversation; Navigation menu).
    You hear the sound Notice.
6. Press Home, Enter.
    You hear: Sent: 1.
    You hear: Done.

### Open and close Claude's list of background tasks

1. Type "/tasks".
2. Press Enter.
    You may hear: Claude asks.
3. If that came: Press Ctrl+I (Interrupt; Actions menu).
    You may hear the sound Notice.
4. If that came: Press Escape.
    You hear: Closed.

### Answer a question in your own words

1. Type "Use the AskUserQuestion tool to ask me which flower I enjoy thinking about, with three options.".
2. Press Enter.
    You hear the sound Notice.
3. Press Home, Down, Down, Down.
4. Press Enter.
    You hear: Sent: 4, Other. Your words next.
5. Type "tulips in the garden by the old stone wall where the morning light falls first and the bees arrive early".
6. Press Enter.
    You hear: Sent: tulips in the garden by the old stone wall where the morning light falls first and the bees arrive early.
    You hear: Done.

### Move through Claude's questions in the conversation

1. Press Alt+2, Ctrl+Home.
2. Press Q.
    You hear: Question: Permission Required: Accessing workspace.

### Close a question without answering it

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Use your AskUserQuestion tool once to ask me which flower I like: rose, tulip or daisy.".
3. Press Enter.
    You hear the sound Notice.
4. Press Ctrl+I (Interrupt; Actions menu).

### Interrupt Claude from a permission question

1. Press Ctrl+P (Next permission mode; Actions menu).
    You hear: manual mode on.
2. Type "Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the word pinged.".
3. Press Enter.
    You hear the sound Notice.
4. Press Ctrl+I (Interrupt; Actions menu).

### Answer a set of two questions

1. Type "Reply with one sentence about tulips, then use the AskUserQuestion tool to ask me two questions, which colour I prefer and which season.".
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

1. Type "/permissions".
2. Press Enter.
    You hear the sound Notice.
3. Press Escape.
    You hear: Closed.

### Answer a question with several answers

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Use the AskUserQuestion tool once with two questions: my favourite colour (red, green, blue), and, with multiSelect, which fruits I like (apple, banana, cherry).".
3. Press Enter.
    You hear the sound Notice.
4. Press Home, Down, Enter.
    You hear: Sent: 2, Green. Next question.
5. Press Home, Space, Down, Down, Space, Enter.
    You hear: Sent: 1, Apple, 3, Cherry.
    You hear: Done.

### Review answers before they go to Claude

1. Choose Review answers before they go to Claude in the Options menu, Questions.
2. Type "Use the AskUserQuestion tool once with two questions: my favourite colour (red, green, blue), and, with multiSelect, which fruits I like (apple, banana, cherry).".
3. Press Enter.
    You hear the sound Notice.
4. Press Home, Down, Enter.
    You hear: Next question.
5. Press Home, Space, Enter.
    You hear: Review next.
6. Press Home, Enter.
    You hear: Done.

## Use the axKit menu from a conversation

Starts with: Open axClaude with the folder menu.

### Close every window at once

1. Open axDown with the file notes.md.
2. Press Ctrl+End.
3. Type "more".
4. Go back to the first window.
5. Press Alt+1 (Message field; Navigation menu).
6. Type "Use your Bash tool to run: ping -n 30 127.0.0.1 then reply with the word pinged.".
7. Press Enter.
    You hear: Sent.
    You may hear the sound Notice.
8. If that came: Press 1, Enter.
9. Choose Close all windows in the Window menu.
10. Go to the axDown window.
11. Close the window.
12. Press Tab twice.
13. Press Enter.
14. Go back to the first window.
15. Close the window.
16. Press Enter.

## Read a side answer and other information screens

Starts with: Open axClaude with the folder side.

### Ask a side question and hear the answer

1. Type "/btw what is two plus two? answer in one word".
2. Press Enter.
    You hear the sound Notice.

### Close a side answer

1. Press Escape.
    You hear: Closed.

### Browse earlier side answers

1. Type "/btw what is three plus three? answer in one word".
2. Press Enter.
    You hear the sound Notice.
3. Press Tab twice.
4. Press Enter.
5. Press Escape.
    You hear: Closed.

### Look at Claude's goal

1. Type "/goal".
2. Press Enter.
    You hear the sound Notice.
3. Press Enter.
    You hear: Closed.

### Find side answers in the conversation

1. Press Alt+2, Ctrl+Home.
2. Press B.
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

## Report a bug or an idea from a conversation

Starts with: Open axClaude with the folder report.

### Open the report form

1. Press F4 (Send a report; Help menu).
    You hear: A report goes to axKit's maintainer. It is encrypted on your computer.

### Type a report of several lines

1. Type "The list loses my place".
2. Press Enter.
    You hear: new line.
3. Type "every time".

### Add detail that may name your files

1. Press Shift+Tab.
2. Press Space.
    You hear: checked.
3. Press Tab.

### Send the report

1. Press Shift+Tab five times.
2. Press Ctrl+Enter (Send the message).
    You may hear: Saved.
3. Press Escape.

### Cancel a report

1. Press F4 (Send a report; Help menu).
2. Press Escape.
    You hear: Cancelled.

## Choose which words axClaude says by itself

Starts with: Open axClaude with the folder spoken.

### Silence Claude's own notices

1. Press Ctrl+P (Next permission mode; Actions menu).
2. Choose Claude's own notices, such as "accept edits on" in the Options menu, Announcements.
3. Press Ctrl+P (Next permission mode; Actions menu).
    You hear: plan mode on.

### Turn the words Sent and Responding back on

1. Choose "Sent" and "Queued" after a message in the Options menu, Announcements.
2. Choose "Responding" when a reply starts in the Options menu, Announcements.
3. Type "Reply with the single word two.".
4. Press Enter.
    You hear: Sent.
    You hear: Responding.
    You hear: Done.

### Keep reply lines with chosen words silent

1. Choose Speak replies: all lines in the Options menu, Speech.
    You hear: Speak replies: all lines.
2. Choose Words that keep a reply line silent in the Options menu, Speech.
3. Type "pelican; walrus".
4. Press Enter.
    You hear: 2 words keep a line silent.

### Have every reply line spoken again

1. Choose Words that keep a reply line silent in the Options menu, Speech.
2. Press Ctrl+A, Delete, Enter.
    You hear: Every reply line is spoken.

## Run a command Claude offers you

Starts with: Open axClaude with the folder offer.

### Read the explanation of an offered command

1. Ask Claude to offer you the command "echo offered-first".
    The tool answers with offered: true.
    You hear the sound Offer.
2. Press Alt+2 (Conversation; Navigation menu).
3. Press Alt+1 (Message field; Navigation menu).
4. Press Enter twice.

### Hear the offered command again

1. Press F7 (Claude's question again; Navigation menu).
    You hear: offers a command for you to run: echo offered-first. Written by axAutomate, an AI: read the explanation before you run it. Its reason: A check of the question.

### Turn down an offered command

1. Press Up twice.
2. Press Enter.
    You hear: Not run.

### Run an offered command

1. Ask Claude to offer you the command "echo offered-$((40+2))".
    The tool answers with offered: true.
2. Press Down.
3. Press Enter.
    You hear: Sent.
4. Press Alt+2 (Conversation; Navigation menu).

### Let an AI tool run a window command

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands.
2. Ask Claude to run the command Check for updates at start.
    The tool answers: The window asks the user whether to run Check for updates at start.
    You hear the sound Offer.
3. Press Down.
4. Press Enter.
    You hear: axAutomate ran Check for updates at start.

## See an offered command wait its turn

Starts with: Open axClaude with the folder waits.

### Turn down waiting offers one after another

1. Press Escape twice.
    You hear: Not run.

## A conversation window that starts hidden and hides on closing

Starts with: Open axClaude with the folder home\Documents\axKit\Claude, titled axHelp, hiding when it is closed, hidden.

### Bring up the hidden conversation

1. Open axHelp.
2. Choose Open the conversation in the Actions menu (key: Enter).
3. Go back to the first window.
    You may hear: trust.
4. If that came: Press Enter.
    You hear: Ready.

### Hide the conversation again

1. Close the window.
2. Go to the axHelp window.
3. Choose Open the conversation in the Actions menu (key: Enter).
4. Go back to the first window.

### End Claude and close the window for good

1. Choose the menu item End Claude and close this window.

## See what happened while a hidden conversation was away

Starts with: Open axClaude with the folder away, titled axHelp, hiding when it is closed. Open axHelp. Go back to the first window. Close the window.

### Bring up the window and hear what happened while it was away

1. Go to the axHelp window.
2. Choose Open the conversation in the Actions menu (key: Enter).
3. Go back to the first window.
    You hear: Done.
    You hear the sound Offer.
4. Press Escape.
    You hear: Not run.

## Hear and ask what Claude runs in the background

Starts with: Open axClaude with the folder bg.

### Ask what runs in the background

You hear the sound BackgroundStart.
1. Press F8 (What runs in the background; Actions menu).
    You hear: 3 in the background: 1 shell, 2 agents. general-purpose: Checking callers. Explore: Reading the spec.
2. Press F9 (Say the status; Window menu).
    You hear: Claude: ready | 3 in the background: 1 shell, 2 agents | auto mode on.
3. Choose What runs in the background in the Actions menu (key: F8).
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

1. Press F9 (Say the status; Window menu).
    You hear: F9: Say the status.

### End the key test

1. Press Escape.
    You hear: Key test ended.

### Hear what the conversation's keys do

1. Choose Test a key in the Help menu, Diagnostics.
    You hear: Key test. Press any key.
2. Press H.
    You hear: H: moves to the next heading.

## Read back a long message before sending it, and turn the music on with Ctrl+M

Starts with: Open axClaude with the folder em.

### Move through a message by its headings, links and lists

1. Press Ctrl+Space.
    You hear: Navigation mode.
2. Press K.
    You hear: https://example.com, link.
3. Press I.
    You hear: one.

### Go back to editing the message

1. Press Alt+1 (Message field; Navigation menu).
2. Press Ctrl+Space.
    You hear: Edit mode.

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

## Follow a workflow Claude runs in the background without hearing its live row

Starts with: Open axClaude with the folder wf.

### Hear how far a workflow is

1. Press F8 (What runs in the background; Actions menu).
    You hear: 1 workflow in the background. Workflow axkit-design-map-and-verify: 88 percent, 8 of 9 done, 20m18s.
2. Press F9 (Say the status; Window menu).
    You hear: 1 workflow in the background.

### Show Claude Code's live progress rows in the conversation

1. Choose Show Claude Code's live progress rows in the conversation in the Options menu, Background work.
    You hear the sound BackgroundStart.

### Hide the live progress rows again

1. Choose Show Claude Code's live progress rows in the conversation in the Options menu, Background work.

## Hear where axClaude's events come from

Starts with: Open axClaude with the folder es.

### Hear where the window's events come from

1. Choose Say the event sources in the Options menu, Developer.
    You hear: Events. Marks: 0. Hooks: not passed, version. Session file: not found. Core: in charge of state and Done.

### Turn the grounding in Claude Code's session file off and on again

1. Choose Ground the conversation in Claude Code's session file in the Options menu, Developer.
    You hear: Grounding in Claude Code's session file off.

### Turn Claude Code's events off and on again

1. Choose Events from Claude Code's hooks in the Options menu, Developer.
    You hear: Events from Claude Code's hooks off, from the next start of Claude.

## Follow a turn played by the stand-in

Starts with: Open axClaude with the folder st.

You hear: Ready.
1. Type "Say ok".
2. Press Enter.
    You hear: Sent.
    You hear: Done. claude: ok.
3. Press Alt+2 (Conversation; Navigation menu).

## See Claude Code's events arrive, without Claude Code

Starts with: Open axClaude with the folder a.

### Hear whether Claude Code's events reach the window

1. Choose Say the event sources in the Options menu, Developer.
    You hear: Hooks: on, 1 received.

### Turn Claude Code's events off and on again

1. Go back to the first window.
2. Choose Events from Claude Code's hooks in the Options menu, Developer.
    You hear: Events from Claude Code's hooks off, from the next start of Claude.
3. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
4. Press Enter.
    You hear: Restarting.
    You hear: Ready.

## Start Claude Code with axClaude's hooks

Starts with: Open axClaude with the folder hooked.

### Hear that Claude Code's events arrived after a turn

1. Type "Reply with the single word ok.".
2. Press Enter.
    You hear: Sent.
    You hear: Done.
3. Choose Say the event sources in the Options menu, Developer.
    You hear something with "Hooks: on" in it.

## See the session file followed, without Claude Code

Starts with: Open axClaude with the folder sf.

### Hear whether Claude Code's session file is followed

1. Type "Say ok".
2. Press Enter.
    You hear: Sent.
    You hear: Done.
3. Choose Say the event sources in the Options menu, Developer.
    You hear: Session file: followed, 3 entries.

### Turn the grounding in Claude Code's session file off and on again

1. Choose Ground the conversation in Claude Code's session file in the Options menu, Developer.
    You hear: Grounding in Claude Code's session file off.

## Know that Claude works, and send a message while a reply arrives

Starts with: Open axClaude with the folder sw.

### Know that Claude works while the screen shows no sign of it

1. Type "Say ok".
2. Press Enter.
    You hear: Sent.

### Send a message while a reply's rows are still arriving

1. Type "Say more".
2. Press Enter.
    You hear: Queued.

## Hear Done before the reply, and not after an interrupt

Starts with: Open axClaude with the folder dn.

### Hear Done, then the reply's words

1. Type "Say ok".
2. Press Enter.
    You hear: Sent.
    You hear: Done. claude: ok.
    You hear the sound Done.

## Hear that Claude goes on by itself after background work

Starts with: Open axClaude with the folder by.

### Stop hearing that Claude goes on by itself

1. Choose "Claude goes on by itself" when a turn starts that you did not send in the Options menu, Announcements.
    You hear: "Claude goes on by itself" when a turn starts that you did not send, not checked.

## Follow a turn of Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Send a message and hear the reply and Done

You hear: Ready.
1. Type "Say ok".
2. Press Enter.
    You hear: Sent.
    You hear: Done. claude: ok.
3. Press Alt+2 (Conversation; Navigation menu).

## Change Claude Code's mode and use the window's commands over the stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Go to Claude Code's next permission mode

You hear: Ready.
1. Press Ctrl+P (Next permission mode; Actions menu).
    You hear: accept edits on.

### Send a message in the new mode

1. Type "Write the file".
2. Press Enter.
    You hear: Sent.
    You hear: Done. claude: written.
3. Press Alt+2 (Conversation; Navigation menu).

### Restart Claude Code

1. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
2. Press Enter.
    You hear: Restarting.
    You hear: Ready.
3. Press Alt+2 (Conversation; Navigation menu).

## Hear that Claude Code's stream ended, and be offered the screen

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Learn that Claude Code ended and stay in the window

You hear: Ready.
1. Type "Say ok".
2. Press Enter.
    You hear: Sent.
    You hear: Claude stopped.
3. Press Escape.
4. Press Alt+2 (Conversation; Navigation menu).

## Answer what Claude asks to do, over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Say yes to Claude Code in this folder

You hear: Start Claude Code in this folder?
1. Press Tab.
2. Press Enter.
    You hear: Ready.

### Hear what Claude asks to do, with no first

1. Type "Run the ping".
2. Press Enter.
    You hear: Sent.
    You hear: Question.
    The focus moves to Claude asks to use Bash: ping -n 2 127.0.0.1. Asked by Claude, an AI: read the explanation before you allow it.

### Read everything Claude asks to do before answering

1. Press End.
2. Press Enter twice.

### Allow it once

1. Press Up twice.
2. Press Enter.
    You hear: Sent: 3, Yes, once.
    You hear: Done. claude: pinged.
3. Press Alt+2 (Conversation; Navigation menu).

## Say no to Claude Code in a folder, and to what Claude asks to do

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Be asked again and say yes

1. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
    You hear: Start Claude Code in this folder?
2. Press Tab.
3. Press Enter.
    You hear: Ready.

### Refuse what Claude asks to do

1. Press Enter.
    You hear: Sent.
    You hear: Question.
    The focus moves to Claude asks to use Write: C:\work\probe-denied.txt. Asked by Claude, an AI: read the explanation before you allow it.
2. Press Enter.
    You hear: Sent: 1, No.
    You hear: Done. claude: No, it was not created.
3. Press Alt+2 (Conversation; Navigation menu).

## Answer Claude's own questions over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Answer the first question in your own words

1. Type "Ask me two things".
2. Press Enter.
    You hear: Sent.
    You hear: Question.
    The focus moves to Question 1 of 2: What is your favourite colour?

### Choose Other and type your own answer

1. Press End.
2. Press Enter.
    You hear: Sent: 4, Other. Your words next.
    You hear: You chose Other. Your answer.
3. Press Escape.
    You hear: Question 1 of 2: What is your favourite colour?
4. Press End.
5. Press Enter.
    You hear: You chose Other. Your answer.
6. Type "teal".
7. Press Enter.
    You hear: Sent: teal. Next question.

### Tick several answers of the second question

You hear: Question 2 of 2: Which fruits do you like?
1. Press Space.
2. Press Down twice.
3. Press Space.
4. Press Enter.
    You hear: Sent: 1, Apple, 3, Cherry.
    You hear: Done. claude: thanks.

## Interrupt Claude over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Refuse a waiting question with the interrupt key

1. Type "Run the long ping".
2. Press Enter.
    You hear: Sent.
    You hear: Claude asks to use Bash: ping -n 20 127.0.0.1.
3. Press Ctrl+I (Interrupt; Actions menu).
    You hear: Cancelled.

### Interrupt the turn that runs

1. Press Ctrl+I (Interrupt; Actions menu).
    You hear: Interrupted.

### Go on after the interrupt

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Still there?".
3. Press Enter.
    You hear: Sent.
    You hear: Done. claude: still here.
4. Press Alt+2 (Conversation; Navigation menu).

## Be asked again when Claude asks the same thing twice, over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Allow the first of two calls that read the same

1. Type "Change the notes".
2. Press Enter.
    You hear: Sent.
    You hear: Question.
    The focus moves to Claude asks to use Edit: notes.txt. Asked by Claude, an AI: read the explanation before you allow it.
3. Press Down twice.
4. Press Enter.
    You hear: Sent: 3, Yes, once.

### Hear the second call asked anew, with no first

You hear: Question.
The focus moves to Claude asks to use Edit: notes.txt. Asked by Claude, an AI: read the explanation before you allow it.
1. Press Enter.
    You hear: Sent: 1, No.
    You hear: Done. claude: one of two.

## Hear a reply spoken in a new install

Starts with: Open axClaude with the folder st.

You hear: Ready.
1. Type "Say ok".
2. Press Enter.
    You hear: Sent.
    You hear: Done. claude: ok.
3. Press Alt+2 (Conversation; Navigation menu).

## Let Claude run a command over its stream, with Claude Code itself

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Start Claude Code over its stream in a folder

You hear: Start Claude Code in this folder?
1. Press Tab.
2. Press Enter.
    You hear: Ready.

### Answer Claude Code's own question for a command

1. Type "Use your shell tool to run exactly this command, then tell me in one short sentence what happened: ping -n 2 127.0.0.1".
2. Press Enter.
    You hear: Sent.
    You hear: Claude asks to use.
3. Press Down twice.
4. Press Enter.
    You hear: Sent: 3, Yes, once.
    You hear: Done.
5. Press Alt+2 (Conversation; Navigation menu).

## Refuse a command, stop a reply and answer Claude's own question over the stream, with Claude Code itself

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Refuse a command Claude asks to run

1. Type "Use your shell tool to run exactly this command, then tell me in one short sentence what happened: ping -n 2 127.0.0.1".
2. Press Enter.
    You hear: Sent.
    You hear: Claude asks to use.
3. Press Enter.
    You hear: Sent: 1, No.
    You hear: Done.

### Stop a reply Claude is writing

1. Type "Write a numbered list of eighty different kinds of fruit, one on each line, with a short sentence about each.".
2. Press Enter.
    You hear: Sent.
3. Press Ctrl+I (Interrupt; Actions menu).
    You hear: Interrupted.

### Answer a question Claude asks you

1. Type "Use your AskUserQuestion tool to ask me one question: which colour I prefer, with the two options Red and Blue. Then reply with only the colour I chose.".
2. Press Enter.
    You hear: Sent.
3. Press Enter.
    You hear: Sent: 1.
    You hear: Done.
4. Press Alt+2 (Conversation; Navigation menu).

## Choose the model and the effort over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Choose the model from the list Claude Code gave

1. Choose Choose the model in the Actions menu.
    The focus moves to Choose the model.
2. Press Down four times.
3. Press Enter.
    You hear: Sent: 5, Haiku 5.5.

### Choose the effort

1. Press Alt+1 (Message field; Navigation menu).
2. Choose Choose the effort in the Actions menu.
    The focus moves to Choose the effort.
3. Press Enter.
    You hear: Sent: 1, low.

### Send a message after the choices

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Say ok".
3. Press Enter.
    You hear: Done. claude: ok.

## See the earlier conversation when Claude Code's stream takes it up

Starts with: Open axClaude with the folder st, fed by Claude Code's stream. Close the window.

### Read the earlier conversation in a new window

1. Open axClaude with the folder st, fed by Claude Code's stream.
    You hear: Start Claude Code in this folder?
2. Press Tab.
3. Press Enter.
    You hear: Ready.
4. Press Alt+2 (Conversation; Navigation menu).

### Go on with the conversation

1. Press Alt+1 (Message field; Navigation menu).
2. Type "Say ok".
3. Press Enter.
    You hear: Sent.
    You hear: Done.
4. Press Alt+2 (Conversation; Navigation menu).

## Hand a conversation from Claude Code's stream to its screen and back, with Claude Code itself

Starts with: Open axClaude with the folder hand, fed by Claude Code's stream, taking up the last conversation. Close the window.

### Take the stream's conversation up with the screen

1. Open axClaude with the folder hand, taking up the last conversation.
    You hear: trust.
2. Press Enter.
    You hear: Ready.
3. Press Alt+2 (Conversation; Navigation menu).
4. Close the window.

### Take the conversation up over the stream again and read its earlier part

1. Open axClaude with the folder hand, fed by Claude Code's stream, taking up the last conversation.
    You hear: Start Claude Code in this folder?
2. Press Tab.
3. Press Enter.
    You hear: Ready.
4. Press Alt+2 (Conversation; Navigation menu).

## Search the rules of Claude Code's permissions screen

Starts with: Open axClaude with the folder rules.

### Search the rules of the permissions screen

1. Type "/permissions".
2. Press Enter.
    You hear the sound Notice.
3. Press End, Up.
4. Press Enter.
5. Type "git".
6. Press Enter.
    You hear: Question.
7. Press Down.

### Leave the search

1. Press Escape twice.
2. Press Ctrl+I (Interrupt; Actions menu).

## Answer Claude's question in your own words

Starts with: Open axClaude with the folder ow.

### Choose Other and hear it named

You hear the sound Notice.
1. Press Down twice.
    You hear the sound Notice.
2. Press Enter.
    You hear: Sent: 3, Other. Your words next.

### Give your own words

1. Type "every key reads the question and it should also emit a distinct earcon".
2. Press Enter.
    You hear: Sent: every key reads the question and it should also emit a distinct earcon.

## Refuse what Claude asks to do and tell Claude why, over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Hear both refusals before any yes

1. Type "Write the file".
2. Press Enter.
    You hear: Sent.
    You hear: Question.

### Choose to tell Claude why, then go back to the list

1. Press Down.
2. Press Enter.
    You hear: Sent: 2, No, and tell Claude why. Your words next.
3. Press Escape.

### Refuse with your own words

1. Press Down.
2. Press Enter.
3. Type "Use notes.txt instead".
4. Press Enter.
    You hear: Sent: Use notes.txt instead.
    You hear: Done. claude: No, it was not created.
5. Press Alt+2 (Conversation; Navigation menu).

## Type a slash command or a ! line over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Send a name that is no command as a message

1. Type "/probe-no-such-command".
2. Press Enter.
    You hear: Not sent: Claude Code takes no command /probe-no-such-command. It would go to Claude as a message. Enter again sends it so.
3. Press Enter.
    You hear: Sent.
    You hear: Done. claude: ok.
4. Press Alt+2 (Conversation; Navigation menu).

## Read what a tool printed, over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Hear the reply, and not what the tools printed

1. Type "Run the two commands".
2. Press Enter.
    You hear: Sent.
    You hear: Done. claude: listed.

### Read the first lines of what each command printed under its call

1. Press Alt+2 (Conversation; Navigation menu).

## Choose an earlier conversation to resume, over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream. Close the window.

### Hear the folder's conversations, newest first

1. Open axClaude with the folder st, fed by Claude Code's stream.
    You hear: Start Claude Code in this folder?
2. Press Tab.
3. Press Enter.

### Start nothing when no conversation is chosen

1. Press Escape.
    You hear: Cancelled.
2. Press Alt+2 (Conversation; Navigation menu).

### Choose the conversation and go on with it

1. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
2. Press Enter.
    You hear something with "Sent: 1" in it.
    You hear: Ready.
3. Press Alt+2 (Conversation; Navigation menu).

## Approve Claude's plan, over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Go to plan mode

1. Press Ctrl+P (Next permission mode; Actions menu).
    You hear: plan mode on.

### Hear the plan with its caution, the no that keeps planning first

1. Type "Plan the file".
2. Press Enter.
    You hear: Sent.
    You hear: Question.
    The focus moves to Claude has a plan and asks to carry it out. Written by Claude, an AI: read the whole plan before you approve it. Approving lets Claude carry it out.

### Read the whole plan before answering

1. Press End.
2. Press Enter twice.

### Approve the plan and approve each edit yourself

1. Press Up twice.
2. Press Enter.
    You hear: Sent: 2, Yes, and manually approve edits.
    You hear: manual mode on.
3. Press Down twice.
4. Press Enter.
    You hear: Done. claude: carried out.
5. Press Alt+2 (Conversation; Navigation menu).

## Give Claude axKit's AI tools from the start

Starts with: Open axClaude with the folder tools.

### Turn on the AI tools and have Claude know them

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
2. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.
3. Press Ctrl+Shift+R (Force Claude to restart; Actions menu).
4. Press Enter.
    You hear: Restarting.
    You hear: Ready.

## Be asked again when Claude asks the same thing twice, with Claude Code's screen

Starts with: Open axClaude with the folder sq.

### Allow the first of two files that are asked alike

You hear the sound Notice.
You hear: Question.
The focus moves to Permission Required: Create file. alpha.txt.
1. Press Enter.
    You hear: Sent: 1, Yes.

### Allow the second file once its question was heard

1. Press Enter.
    You hear: Sent: 1, Yes.

### Send a plan back and be asked for your words

1. Press End.
2. Press Enter.
    You hear: Sent: 3, No, keep planning. Your words next.
3. Type "Smaller steps".
4. Press Enter.
    You hear the sound Sent.
    You hear: Sent: Smaller steps.

## Have an answer go only to the question it was given to, over Claude Code's stream

Starts with: Open axClaude with the folder st, fed by Claude Code's stream.

### Refuse a first call

1. Type "Change the notes".
2. Press Enter.
    You hear: Sent.
    You hear: Question.
3. Press Enter.
    You hear: Sent: 1, No.
    You hear: Done. claude: left as it was.

## Have an AI tool's request wait for your answer, and go when Claude stops

Starts with: Open axClaude with the folder rq.

### Be asked before an AI tool runs a command that acts

1. Ask Claude to run the command Next permission mode.
    The tool answers: The window asks the user whether to run Next permission mode.

## Interrupt a message right after sending it, and have it not sent again

Starts with: Open axClaude with the folder im.

### Interrupt a message right after sending it

1. Type "second words".
2. Press Enter.
    You hear: Sent.
3. Press Ctrl+I (Interrupt; Actions menu).

## Have a typed answer wait until Claude's question was said, with Claude Code's screen

Starts with: Open axClaude with the folder ta.

### Answer in your own words from the message field

You hear: Claude asks.
1. Type "Please split the work into smaller steps and show me each one first, with the tests for it, before you go on".
2. Press Enter.
    You hear the sound Sent.

### Answer the file's question once it was said

1. Type "1".
2. Press Enter.
    You hear the sound Sent.

## Be asked while reading the conversation, with the settings of a new install

Starts with: Open axClaude with the folder wr.

### Allow the file once its question was heard

1. Press Enter.
    You hear: Sent: 1, Yes.

## Find a question waiting when a notice closes, with the settings of a new install

Starts with: Open axClaude with the folder nq.

### Read the keyboard shortcuts

1. Choose Keyboard shortcuts in the Help menu.

### Allow the file once its question was heard

1. Press Enter.
    You hear: Sent: 1, Yes.

## Keep a message that Claude's question cut off as it was being typed

Starts with: Open axClaude with the folder mq.

### Answer the question and find the message in the field again

1. Press Enter.
    You hear: Sent: 1, Yes.

## Be asked again when the command changes under the same question

Starts with: Open axClaude with the folder cq.

### Hear a command's question and move to No

1. Press Down.

### Refuse the command that was asked last

1. Press Enter.
    You hear: Sent: 2, No.

## Hear that Claude Code keeps a message on its prompt row, and send it from there

Starts with: Open axClaude with the folder nt.

### Send the draft Claude Code kept

1. Press Ctrl+A.
2. Press Delete.
3. Press Enter.

## Have a command of the menus wait until Claude Code has taken the message before it

Starts with: Open axClaude with the folder cf.

### Choose the model once Claude Code has taken the message

1. Choose Choose the model in the Actions menu.
    You hear: Sent.

## Send two messages while Claude Code's prompt row is not drawn after it took the first

Starts with: Open axClaude with the folder nd.

### Send a message, and the next one at once, while Claude Code's prompt row is not drawn

1. Type "first message".
2. Press Enter.
    You hear the sound Sent.
3. Type "second message".
4. Press Enter.
    You hear the sound Sent.

## Answer a question Claude asks while its prompt row still shows the message just sent

Starts with: Open axClaude with the folder qh.

### Answer a question Claude asks right after a message, with nothing given back

1. Type "held message".
2. Press Enter.
    You hear: Sent.

## Open a screen of Claude Code's with a slash command that draws it in place of the prompt row

Starts with: Open axClaude with the folder sr.

### Open the permissions screen, drawn in the prompt row's place, with nothing given back

1. Type "/permissions".
2. Press Enter.

## Have a short exchange with Claude

Starts with: Open axClaude with the folder a.

You may hear: trust.
1. If that came: Press Enter.
    You hear: Ready.
2. Type "Reply with the single word hello.".
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
2. Type "Use your AskUserQuestion tool once, with two questions: which colour I prefer, red or blue, and which season I prefer, summer or winter.".
3. Press Enter.
    The focus moves to Question 1 of 2.
4. Press Enter.
    The focus moves to Question 2 of 2.
5. Press Enter.
