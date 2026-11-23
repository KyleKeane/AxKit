# Workflows

Written by `tools/workflows.ps1` from the scripts of `tests/scripts` and `tests/nvda-scripts` and the command
inventory (`docs/axKit/window-commands.json`); never edit it by hand. What a user can do in axKit, by app: each
workflow is one script of the automated checks, found by its label, a task in the user's words, and under it are its
goals. The steps of every goal, with the keys as they are now, are in the app's page. The checks run on the
developer's computer (`tools/AxAutomate`, `nvda-pass.ps1`), never in CI (`docs/axKit/automation.md`,
`docs/axKit/user-workflows.md`).

## axAutomate

Steps and keys: `docs/axKit/workflows/axAutomate.md` (in Claude's working folder: `workflows-axAutomate.md`).

1. **Browse a folder of workflows and read their steps**
    - Open a folder of workflows
    - Open a workflow and read its steps
    - Read a step's details
2. **Run a workflow and hear how it went**
    - Run every workflow of a folder
    - Read how a workflow's last run went
    - Read the report of a workflow's last run
    - Run one workflow and stop it partway
    - Run a workflow that runs commands
3. **Close axAutomate and come back to the folder you had**
    - Open a folder of workflows
    - Come back to the folder used last
4. **Read a workflow's marks and open its file as text**
    - Find a workflow by typing the start of its name
    - Read what a workflow covers and whether it can run
    - Read the marks after a step's sentence
    - Read what a step does when it fails and what it reports
    - Read the folder again after a workflow was added
    - Read every key of the window
    - Open a workflow file as text in axJSON
    - Open a script as text in axDown
5. **Stop the run of a folder and see which checks need NVDA**
    - Stop the run of a folder partway
    - Read on the rows how far a stopped run came
    - Run only the workflows you selected

## axCalc

Steps and keys: `docs/axKit/workflows/axCalc.md` (in Claude's working folder: `workflows-axCalc.md`).

1. **Calculate and reuse earlier answers**
    - Calculate an expression
    - Empty the field
    - Go on from the last answer
    - Hear the last answer in other bases
    - Put an earlier expression back in the field
    - Go back from the history to the field
    - Read the keyboard shortcuts
2. **Use variables, earlier answers and the history in axCalc**
    - Go on from the last answer and use ans
    - Set a variable and use it
    - Switch angles between degrees and radians
    - Change an entry of the history and answer it again
    - Find an entry in the history
    - See the variables and clear them
    - Clear the history
    - Stop keeping the history between runs
3. **Move through axCalc's history and find in it**
    - Hear the last answer in other bases
    - Move through the history one line at a time
    - Hear the answer of an entry in other bases
    - Move from answer to answer with a letter
    - Find the next and the previous match in the history
    - Go back to the field from the history
    - Change the text size
    - Hear the bases from the command palette
4. **Use powers, factorials, remainders and other number forms**
    - Raise a number to a power
    - Take a factorial
    - Take a remainder
    - Round to a number of places
    - Type numbers with separators, in hexadecimal and in binary

## axClaude

Steps and keys: `docs/axKit/workflows/axClaude.md` (in Claude's working folder: `workflows-axClaude.md`).

1. **Start a conversation with Claude**
    - Move between the conversation and Claude's question, then answer it
    - Move between the message field and the conversation
    - Jump straight to the conversation or the message field
    - Go to the conversation from the menu
    - Hear the status
2. **Send Claude a message and hear the reply**
    - Read Claude's question as lines in the conversation
    - Answer a question by typing in the message field
    - Send a message of two lines
    - Go to Claude's reply and read it
    - Go back to the message you sent
    - Move through a long message by page
    - Move through a message by paragraph
    - Read the keyboard shortcuts
3. **Read the conversation while Claude is writing**
    - Read the conversation while Claude writes
    - Go to Claude's latest reply
    - Quote a line of a reply in your next message
    - Select lines of a reply and clear the selection
4. **Move through a reply by headings and quick keys**
    - Move through the conversation by headings
    - Move by heading level
    - Hide the markers in the text
    - Move through the conversation by paragraph
    - Jump between replies, messages, tools, errors and system lines
    - Go back to the message field from the conversation
    - Move through a long reply by page
    - Skip over a run of lines in a reply
    - Hear the line number
    - Find text in the conversation
    - Find the next match from the message field
    - Go back to where you were before a jump
    - Bookmark lines and move between them
    - Hear a reply's paragraphs, headings or bookmarks without moving
    - Quote two lines in a message and send it
    - Quote a long paragraph whole
    - Send the quote with your comment
5. **Change the permission mode and interrupt Claude**
    - Change the permission mode
    - Read a permission question in the conversation
    - Answer a permission question by its number
    - Interrupt Claude while it works
    - Interrupt Claude from the menu or the message field
    - Send Claude a key from the menu
6. **Send messages while Claude is still working**
    - Send a message while Claude works
    - Send a waiting message at once
7. **Change Claude's project folder**
    - Continue the last conversation in a folder
    - Restart Claude
    - Start a new session with arguments of your own
8. **Restart, stop and close a conversation**
    - Stop Claude from the message field
    - Start a stopped Claude again
    - Restart Claude while it runs
    - Decide whether to close the window while Claude works
    - Carry on the conversation after closing
    - Close the window while Claude is ready
9. **Choose axClaude's speech, sounds and text**
    - Change the text size
    - Turn off the ticks for new lines and tool calls
    - Turn off the chimes
    - Play background music while Claude works
    - Stop recording the raw stream
    - Have Claude's replies spoken
    - Hear each tool call as it starts
    - Read the keyboard shortcuts
    - Find a word in the keyboard shortcuts
    - Read the user guide
    - Read the version, the licence and the axKit guide
    - Show the time on Input and Output lines
10. **Install Claude Code when it is missing**
    - Hear the ways to install Claude Code
    - Close the notice that Claude Code is missing
11. **Check for an update from a conversation**
    - Check for an update
    - Turn off the update check at start
12. **Answer Claude's questions and screens**
    - Answer the trust question from the list
    - Move between a question and the conversation
    - Hear the question again
    - Cancel a question
    - Answer a question that follows a reply
    - Have questions come without interrupting speech
    - Open and close Claude's list of background tasks
    - Answer a question in your own words
    - Move through Claude's questions in the conversation
    - Close a question without answering it
    - Interrupt Claude from a permission question
    - Answer a set of two questions
    - Open and close Claude's permissions screen
    - Answer a question with several answers
    - Review answers before they go to Claude
13. **Use the axKit menu from a conversation**
    - Close every window at once
14. **Read a side answer and other information screens**
    - Ask a side question and hear the answer
    - Close a side answer
    - Browse earlier side answers
    - Look at Claude's goal
    - Find side answers in the conversation
15. **Agree to the reminder before Claude Code first starts**
    - Agree to the reminder before Claude Code starts
16. **Report a bug or an idea from a conversation**
    - Open the report form
    - Type a report of several lines
    - Add detail that may name your files
    - Send the report
    - Cancel a report
17. **Choose which words axClaude says by itself**
    - Silence Claude's own notices
    - Turn the words Sent and Responding back on
    - Keep reply lines with chosen words silent
    - Have every reply line spoken again
18. **Run a command Claude offers you**
    - Read the explanation of an offered command
    - Hear the offered command again
    - Turn down an offered command
    - Run an offered command
    - Let an AI tool run a window command
19. **See an offered command wait its turn**
    - Turn down waiting offers one after another
20. **A conversation window that starts hidden and hides on closing**
    - Bring up the hidden conversation
    - Hide the conversation again
    - End Claude and close the window for good
21. **See what happened while a hidden conversation was away**
    - Bring up the window and hear what happened while it was away
22. **Hear and ask what Claude runs in the background**
    - Ask what runs in the background
    - Hear background work as it starts and ends
23. **Find out whether a key arrives and what it does, in axClaude**
    - Hear what a key does without doing it
    - End the key test
    - Hear what the conversation's keys do
24. **Read back in the conversation while Claude writes on, and find every line in its place**
25. **Read back a long message before sending it, and turn the music on with Ctrl+M**
    - Move through a message by its headings, links and lists
    - Go back to editing the message
    - Turn the background music on and off
26. **Block Claude Code's feedback requests from the Options menu, and turn it off again**
    - Block Claude Code's feedback requests
    - Turn the block on feedback requests off again
    - Choose how Claude-drafted feedback comes
27. **Follow a workflow Claude runs in the background without hearing its live row**
    - Hear how far a workflow is
    - Show Claude Code's live progress rows in the conversation
    - Hide the live progress rows again
28. **Hear where axClaude's events come from**
    - Hear where the window's events come from
    - Turn the grounding in Claude Code's session file off and on again
    - Turn Claude Code's events off and on again
29. **Follow a turn played by the stand-in**
30. **See Claude Code's events arrive, without Claude Code**
    - Hear whether Claude Code's events reach the window
    - Turn Claude Code's events off and on again
31. **Start Claude Code with axClaude's hooks**
    - Hear that Claude Code's events arrived after a turn
32. **See the session file followed, without Claude Code**
    - Hear whether Claude Code's session file is followed
    - Turn the grounding in Claude Code's session file off and on again
33. **Start axClaude with a place of Claude Code another program wrote**
34. **Know that Claude works, and send a message while a reply arrives**
    - Know that Claude works while the screen shows no sign of it
    - Send a message while a reply's rows are still arriving
35. **Hear Done before the reply, and not after an interrupt**
    - Hear Done, then the reply's words
36. **Hear that Claude goes on by itself after background work**
    - Stop hearing that Claude goes on by itself
37. **Follow a turn of Claude Code's stream**
    - Send a message and hear the reply and Done
38. **Change Claude Code's mode and use the window's commands over the stream**
    - Go to Claude Code's next permission mode
    - Send a message in the new mode
    - Restart Claude Code
39. **Hear that Claude Code's stream ended, and be offered the screen**
    - Learn that Claude Code ended and stay in the window
40. **Answer what Claude asks to do, over Claude Code's stream**
    - Say yes to Claude Code in this folder
    - Hear what Claude asks to do, with no first
    - Read everything Claude asks to do before answering
    - Allow it once
41. **Say no to Claude Code in a folder, and to what Claude asks to do**
    - Be asked again and say yes
    - Refuse what Claude asks to do
42. **Answer Claude's own questions over Claude Code's stream**
    - Answer the first question in your own words
    - Tick several answers of the second question
43. **Interrupt Claude over Claude Code's stream**
    - Refuse a waiting question with the interrupt key
    - Interrupt the turn that runs
    - Go on after the interrupt
44. **Be asked again when Claude asks the same thing twice, over Claude Code's stream**
    - Allow the first of two calls that read the same
    - Hear the second call asked anew, with no first
45. **Hear a reply spoken in a new install**
46. **Let Claude run a command over its stream, with Claude Code itself**
    - Start Claude Code over its stream in a folder
    - Answer Claude Code's own question for a command
47. **Have a short exchange with Claude**
48. **Answer a set of two questions from Claude**

## axClip

Steps and keys: `docs/axKit/workflows/axClip.md` (in Claude's working folder: `workflows-axClip.md`).

1. **Find and paste something you copied earlier**
    - Move through the copied items
    - Jump to the newest or oldest item, or by a page
    - Read an item's whole text
    - Pause the recording and start it again
    - Remove an item
    - Clear the clipboard history
    - Hide the window and show it again
    - Quit axClip
2. **Copy again, read a long copy and keep the history between runs**
    - Read a copy of several lines in full
    - Keep the history between runs
3. **Start with a kept history that can no longer be read**
4. **Narrow the clipboard history with several words**
    - Type a second word to narrow the list

## axDown

Steps and keys: `docs/axKit/workflows/axDown.md` (in Claude's working folder: `workflows-axDown.md`).

1. **Open a text file in axDown**
    - Open axDown with an empty document
    - Open a file in axDown
    - Open a text file without naming an app
    - Start a new file by its name
2. **Read and write text in axDown**
    - Read the text line by line and word by word
    - Move by paragraph
    - Hear the line and column
    - Select text a paragraph at a time
    - Turn word wrap off and on
    - Move by page
    - Type text, then undo and redo it
    - Undo and redo from the menu
3. **Hear why a file that is not text will not open**
4. **Open, save and switch files in axDown**
    - Save a change to a file
    - Open a new empty window
    - Start a new document when the text has changes
    - Reload a file that another program changed
    - Save over the changes of another program
    - Create a file by saving it
    - Close a window without saving its changes
5. **Find text and go to a line**
    - Find a word in the text
    - Go to a line by its number
6. **Change the text size and the font**
    - Change the text size
7. **Find a command in the menus, the palette and the help**
    - Open the keyboard shortcuts
    - Move through the keyboard shortcuts by heading
    - Find a word in the keyboard shortcuts
    - Close the keyboard shortcuts and go back to the text
    - Read the user guide
    - Hear the version of axKit
    - Read the licence, the attribution and the axKit guide
    - Turn the update check at start on or off
    - Move through the commands of the palette
    - Run a command found by its name
8. **Check for updates and remove axKit**
    - Hear the version of axKit
    - Check for updates
9. **Move through a Markdown file by headings, lists and links**
    - Move to the next heading
    - Move back to the previous heading
    - Move to a heading of one level
    - Move by heading from the menu
    - Move to the next link
    - Move back to the previous link
    - Move by list and by list item
    - Move to a block quote or a table
10. **Show the current file in another app**
11. **Change the colours with the theme file**
    - Make a theme file and open it
    - Reload the theme
    - Turn the theme off and on
12. **Ask Claude about the window you are in**
    - Turn Claude on by agreeing to the reminder
    - Turn Claude off and on
    - Ask Claude about this window
    - Ask more about an answer
    - See the folders Claude may read
13. **Hear a line break as you type it**
    - Start a new line and hear it said
    - Split a line in two
    - Turn the spoken new line off and on
14. **Hear the heading sound as you move**
    - Hear the sound when the caret reaches a heading
    - Turn the heading sound off and on
15. **Open a recent file or window from the command palette**
    - Open a recent file from the palette
    - Find another open window from the palette
    - Reopen a window that was closed
16. **Find out whether a key arrives and what it does**
    - Start the key test
    - Hear what a key does without doing it
    - End the key test
17. **Move through a file with single letters without changing it, then type again**
    - Turn on navigation mode
    - Move by single letters
    - Go back to edit mode and type
18. **Give a line, a word and several lines their Markdown format, and take it away again**
    - Make a line a heading and change its level
    - Make the word at the caret bold and take it off again
    - Make the selected words italic
    - Make several lines list items and undo it in one step
19. **Put a template in at the caret: a link around the selected words, a table, and a template of your own**
    - Make the selected word a link and type its address
    - Put in a table and type into its first cell
    - Put in a template of your own from the command palette, with the selected words carried into it
20. **Answer a notice and use the command palette**
    - Read the keyboard shortcuts
    - Find a command in the command palette

## axFile

Steps and keys: `docs/axKit/workflows/axFile.md` (in Claude's working folder: `workflows-axFile.md`).

1. **Browse folders and files**
    - Open a folder in axFile
    - Move through the list of files
    - Find a file by typing its name
    - Move between the list and the Actions list
    - Hear the status bar
    - Open a folder in the list
    - Go back to the folder above
    - Go to a folder by its path
    - Go to the drives of This PC
    - Open a drive
    - Move down and up the folder tree
    - Hear the details of a folder or a file
    - Go to a folder named by an environment variable
    - Go to a file by its path
    - Complete a path from its first letters
    - Open a completed path
    - Add a folder to the favorites and remove it
    - Refresh the folder after a change from outside
    - Show hidden files and names that start with a period
    - Find a file by part of its name
    - Show only the entries whose names contain a word
    - Sort the list by date, size or name
    - Select several entries
    - Clear the selection
    - Select a run of entries in a row
2. **Open a folder in axFile from another app**
    - Show the file of an axDown window in axFile
3. **Copy, move, rename and delete files**
    - Make a new folder
    - Make a new text file
    - Move selected files into a new folder
    - Rename a folder
    - Delete a folder to the Recycle Bin
    - Compress a folder to a zip file
    - Compress selected files into one zip file
    - Extract a zip file here
    - Undo a rename
    - Undo a new folder
    - Undo a move to a folder
    - Undo a compress
    - Undo a delete
    - Undo the delete of several entries
4. **Play or read a file from axFile**
    - Read a file in axPreview
    - Read another file in the same axPreview window
    - Open a folder in a new axFile window
5. **Use axFile's text, notices and axKit menu**
    - Change the text size
    - Read the user guide
    - Hear the version of axKit
    - Run a command from the command palette
    - Open axFile on the last folder
6. **Undo a file operation from the Actions list**
    - Move to the Actions list from the menu
    - See the last operation in the Actions list
    - Go to the folder of an operation from its line
    - Undo an operation from the Actions list
    - Go to an operation done in another folder
    - Undo the newest operation from the folder list
    - Select entries without a line in the Actions list
    - Select every entry from the marked one to here
7. **Move files to a folder you type**
    - Move a file into a folder by typing its name
    - Undo the move
8. **Open a recent folder in axFile from the command palette**
9. **Find out whether a key arrives and what it does, in a list**
    - Start the key test
    - Hear what a key does without doing it
    - End the key test
10. **Start axFile with settings another program changed**
11. **Change settings in two windows of one app**
    - Change one setting in each of two windows
12. **Take back a new text file**
    - Undo a new text file
13. **Hear why a folder a program runs in cannot be renamed**
    - Rename the folder once the program has ended

## axGIT

Steps and keys: `docs/axKit/workflows/axGIT.md` (in Claude's working folder: `workflows-axGIT.md`).

1. **Open a repository in axGIT**
    - Open a repository
    - Move between the list and the details
    - Go up to the repository and into another list
    - Switch between the lists by their keys
    - Close the window
2. **Open axGIT from another app**
3. **Review and commit changes**
    - Stage and unstage a file
    - Select files with Space and stage them together
    - Read the diff of a changed file
    - Read an untracked file
    - Stage all and unstage all
    - Commit the staged changes
    - Discard the changes to a file
    - Discard the changes to the selected files
4. **Read the history and switch branches**
    - Read a commit of the log
    - Switch branches
    - Make a new branch
    - Fetch, pull and push
5. **Use axGIT's text, notices and axKit menu**
    - Find a word in a diff
    - Move down the details by page
    - Move by paragraph in a commit's details
    - Change the text size
    - Read the keyboard shortcuts and the user guide
    - Hear the version of axKit
    - Open the last repository again
6. **Agree to the reminder before the first repository opens**
    - Agree to the reminder and open the repository
    - Read what was agreed to on this computer
7. **Open a repository that runs programs of its own**
    - Open a repository with its own programs kept off
    - Trust a repository

## axHelp

Steps and keys: `docs/axKit/workflows/axHelp.md` (in Claude's working folder: `workflows-axHelp.md`).

1. **Ask Claude for help from anywhere, with a picture of the screen**
    - Turn on the AI tools
    - Open a conversation with Claude
    - Hand a picture to a new conversation
    - Quit axHelp
2. **Close axHelp's conversation and bring the same one back**
    - Hide the conversation
    - Bring the same conversation back
    - Hand a picture to the hidden conversation

## axJSON

Steps and keys: `docs/axKit/workflows/axJSON.md` (in Claude's working folder: `workflows-axJSON.md`).

1. **Open a JSON file in axJSON**
    - Open axJSON with an empty document
    - Open a JSON file
    - Reload a file that another program changed
2. **Move through a JSON file by its structure**
    - Move from key to key
    - Move back from key to key
    - Move to the next object and back to the whole file
    - Move through arrays and their items
    - Move by the structure from the Navigation menu
    - Go to the next and the previous error
3. **Format a JSON file**
    - Format a file written on one line
    - Format and repair a broken file
    - Repair a single-quoted value
    - Undo and redo a format
4. **Use axJSON's axKit menu and About**
    - Hear the version of axKit
5. **Use axJSON's Context menu**
    - Select all the text
    - Select all the text from the command palette
6. **Save, find, go to a line and wrap in axJSON**
    - Find text in a JSON file
    - Go to a line of a JSON file by its number
    - Turn word wrap off and on in axJSON
    - Save a change to a JSON file
    - Close axJSON when the file has changes
7. **Write a list's configuration by choosing**
    - Choose a value from the values a member can take
    - Change a value in line to the next and the previous
    - Add a member an object can have
    - Add a key to an action and choose it
    - Choose an item's value through its link to the list's configuration

## axMCP

Steps and keys: `docs/axKit/workflows/axMCP.md` (in Claude's working folder: `workflows-axMCP.md`).

1. **Start an MCP server and use its tools**
    - Find a server by its name
    - Read a server's details
    - Start a server and open its tools
    - Read a tool's description and its arguments
    - Fill in an argument and call the tool
    - Open a tool's answer in axJSON
    - Read a resource
    - Stop a server
2. **Add, rename and remove a server of your own in axMCP**
    - Add a server of your own
    - Rename a server and undo the rename
    - Remove a server
    - Undo the removal of a server
    - Remove several of your servers at once and bring them back
    - Start a server
    - Tick a yes-or-no argument and call the tool
    - Read a resource from a template
    - Read a prompt's arguments
    - Fill in a prompt and fetch it
3. **Fill in a number and a choice, and start a server again**
    - Read the names of a server's environment variables
    - Start a server with Enter
    - Stop the server selected at the top level
    - Fill in a number
    - Step through a choice with Right and Left
    - Choose from a choice's list
    - Call the tool and move by the headings of its result
    - Find a word in a result
    - Start a server again after it stopped

## axMedia

Steps and keys: `docs/axKit/workflows/axMedia.md` (in Claude's working folder: `workflows-axMedia.md`).

1. **Build and move through a play queue**
    - Open axMedia with an empty queue
    - Play a folder of sound files
    - Move through the queue
    - Find a file by part of its name
    - Play another file of the queue
    - Hear the information of a file
    - Remove files and clear the queue
2. **Add files to the queue from another app**
    - Add a folder to the queue with no window open
    - Start playing with the main action key
    - Play a file with its folder around it
    - Play a file in the open window
    - Add a folder to the open window's queue
    - Open a second window on a file
3. **Play, pause and seek**
    - Play, pause and resume
    - Move forward, back and to a time
    - Change the playing speed
    - Hear what is playing and go to the next or the previous file
    - Stop saying each file as it starts
4. **Hear why a file cannot be played**
    - Hear that a file cannot be played, and the next one plays
5. **Use axMedia's text, notices and axKit menu**
    - Change the text size
    - Read the keyboard shortcuts and the user guide
6. **Use axMedia's Context menu**
    - Hear a file's information from the Context menu
    - Play the selected file from the Context menu
    - Remove a file found in the command palette
7. **Add a recent folder to the queue from the command palette**
    - Add a recent folder to the queue from the palette
8. **Select files in the queue and take several out**
    - Pause and resume with Ctrl+Enter
    - Select files with Space and with Shift and an arrow
    - Take the selected files out of the queue
    - Select every file, and give the selection up
9. **Filter and sort axMedia's queue**
    - Narrow the queue to the files whose name contains a text
    - Clear the filter
    - Put the queue in the order of the files' names

## axPass

Steps and keys: `docs/axKit/workflows/axPass.md` (in Claude's working folder: `workflows-axPass.md`).

1. **Unlock a password database and change an entry**
    - Unlock a password database
    - Refresh the database
    - Open a group and go back up
    - Read an entry's fields
    - Move between the entries and the fields
    - Change a user name
    - Undo a change
    - Rename an entry
    - Add an entry
    - Move an entry to the Recycle Bin and undo it
    - Move several entries to the Recycle Bin at once and undo it
    - Set a generated password
    - Move an entry to another group
    - Lock the database
2. **Agree to the reminder before the first unlock**
    - Agree to the reminder and unlock the database
3. **Read an entry in full and come back to the same database**
    - Hear one field of an entry
    - Hear every field of an entry at once
    - Choose when axPass locks by itself
    - Open the last database again without naming it
4. **Change an entry's fields, keep a backup and delete for good**
    - Make a backup now
    - Show the backups in axFile
    - Open a group with Enter and go back up with Backspace
    - Write an entry's notes over two lines
    - Change an entry's URL
    - Set a password typed twice
    - Change an entry's title in its fields
    - Move an entry to the Recycle Bin
    - Make a new group
    - Move a group to the Recycle Bin
    - Delete an entry for good from the Recycle Bin

## axPreview

Steps and keys: `docs/axKit/workflows/axPreview.md` (in Claude's working folder: `workflows-axPreview.md`).

1. **Preview a file that is almost text**
2. **Preview a text file**
    - Open a text file in axPreview
    - Read the text line by line
    - Go to a line by its number
    - Move by headings, links, lists, quotes and tables
    - Find a word in the text
    - Move by page and by paragraph
    - Turn word wrap off and on
    - Read the keyboard shortcuts
    - Reload the file after a change
3. **Preview a file from another app**
    - Show a file in the open window
    - Open a second window
4. **Use axPreview's text, notices and axKit menu**
    - Change the text size
    - Read the user guide
    - Close the window
5. **Read a file's details**
    - Read a file's details
    - Find a line of the details by typing its first letters
    - Move between the text and the details
    - Read a Word document
    - Read the contents of a zip
    - Read the text in a picture
    - Read the text of a picture on the clipboard
    - Preview a sound file
    - Read the text inside any other file
6. **Use axPreview's Context menu**
    - Select all the text
    - Select all the text from the command palette
7. **Open a recent file in axPreview from the command palette**
    - Open a recent file from the palette

## axProcess

Steps and keys: `docs/axKit/workflows/axProcess.md` (in Claude's working folder: `workflows-axProcess.md`).

1. **Find and end a process**
    - Hear how much memory is in use
    - Refresh the list
    - Sort the processes by name, processor or memory
    - Find a process and read its details
    - End a process
2. **Manage services and startup programs**
    - Show the services
    - Read a service's details
    - Go back from the services to the processes
    - Show the startup programs
    - Read the keyboard shortcuts
    - Go up to This computer and into another list
    - Find a service by typing its name
3. **Find a process by typing its name and read a startup program's details**
    - Find a process by typing its name
    - Read a startup program's details
4. **Turn a startup program off, on again, or remove it**
    - Show the startup programs
    - Turn a startup program off
    - Turn a startup program on again
    - Remove a startup program
    - Turn a Startup folder shortcut off and on again from its details
    - Remove a shortcut from the Startup folder
5. **End a process with the processes it started**
    - Find a process and go to the next and the previous match
    - End a process with the processes it started
6. **Select several processes or startup programs and end or remove them together**
    - Select two processes with Space and Shift+Down
    - End the selected processes together
    - Select two startup programs and remove them together

## axStart

Steps and keys: `docs/axKit/workflows/axStart.md` (in Claude's working folder: `workflows-axStart.md`).

1. **Find and start a program**
    - Refresh the list of programs
    - Move through the matches
    - Jump to the first or last match, or by a page
    - Hear the selected match's kind and path
    - Hide the window and show it again
    - Quit axStart
2. **Search programs as you type and change axStart's hotkey**
    - Hear the first match as you type
    - Hear the new first match after deleting a letter
    - Search by several words
    - Change the hotkey
    - Quit axStart
3. **Hear that nothing matches and turn the spoken match off**
    - Turn the spoken first match off and on
    - Quit axStart
4. **Find out whether a key arrives and what it does, in a hotkey window**
    - Test what a key does

## axTask

Steps and keys: `docs/axKit/workflows/axTask.md` (in Claude's working folder: `workflows-axTask.md`).

1. **Move through tasks and change an item's fields**
    - Move among the lists by their headings
    - Open a list
    - Hear the status bar
    - Open an item's fields
    - Move between the list and the fields
    - Mark a task done or not done
    - Set a due date by typing a weekday
    - Give a field its first value
    - Change a choice to its previous or next value
    - Set a check field on or off
    - Rename an item
    - Add an item
    - Archive an item and undo it
    - Hear every field of an item
    - Show all tasks, the soonest due first
2. **Make lists, link items and use smart lists**
    - Read a list's details
    - Write notes of several lines
    - Link one item to another
    - Make a checklist a plain list and back
    - Make a new list
    - Hide and show done tasks
3. **Group lists into sections**
    - Move a list into a new section
    - Move among the sections by their headings
    - Make a new list in a section
    - Put a list in a section from its details
    - Move a list back to Your lists
    - Archive a list and undo it
4. **Act on items with action keys**
    - Run an action key on an item
    - Move an item to another list with an action key
    - Give a list another action
5. **Add project folders and search every list**
    - Find the project folders by their headings
    - Mark a project's task done from All tasks
    - Open a project and mark a task done in it
    - Add a project folder by its path
    - Search every list
    - Remove a project folder
6. **Group smart lists and find items by tag**
    - Move among the tags by their symbols
    - Gather a tag's items from every list
    - Find the smart lists that share a first word
7. **Hear the heading sound in the item apps**
8. **Close axTask and come back to the list you had**
    - Come back to the list you had
9. **Find out whether a key arrives and what it does, in an item window**
    - Test what a key does
    - End the key test
10. **Find and mend a task whose file is damaged**
    - Hear a damaged item in its list
    - Open a damaged item's file in axJSON
    - Hear how many items a gathering list could not read
11. **Change a setting of axTask when its settings file cannot be written**
12. **Rename a list, and change the fields of its items**
    - Rename a list
    - Read which fields a list's items have
    - Take a field out of a list and keep its values
    - Put a field back and find its values again
13. **Set a date in words, and add an item with its list's fields**
    - Give a task a date and time in words
    - Add an item with its list's fields
14. **Make a smart list of your own**
    - Make a smart list and see its file open in axJSON
15. **Approve or decline a report from outside with the approval passphrase**
    - A wrong passphrase approves nothing
    - Approve an entry with the passphrase
16. **Convert a list written in axTask's earlier format**
    - Convert a list of the earlier format
    - Take a conversion back
    - Convert every list of the earlier format at once
17. **Give a list its configuration**
    - Give a list its configuration
    - Add an item to a list that has no configuration yet
18. **Change an item in its form**
    - Open an item's form and move through its fields
    - Change several fields of an item and save them as one step
    - Rename an item in its form
    - Open the form on a field from the detail pane
19. **Run the actions of a list**
    - Set fields with an action's key
    - Read what a program would do before its first run, and cancel it
    - Run a program on an item, and hear how it ends
    - Fill in an action's form
    - Stop a running action
    - Put a command into axTerminal's command field
20. **Select several items and act on all of them**
    - Select items one by one with Space
    - Select a range as you move, with Shift and an arrow
    - Give the selection up with Escape, and select every item with Ctrl+A
    - Mark several tasks done at once
    - Set a field of every selected item with an action of the list
    - Move every selected item to another list with an action of the list
    - Archive several items at once
    - Move the selected items to a list chosen in a form
    - Move an item to a new list
21. **Hear what kind of thing opened**
    - Hear that a text to read opened
    - Hear that a field to type in opened
    - Hear that a form opened, and its explanation only on demand
    - Turn the entry sounds off and on
22. **Filter and sort a list of axTask**
    - Narrow the lists to those whose name contains a text
    - Put a list's items in the order of their names
23. **Hear the end of a list that holds one item or none**
    - Hear both ends of a list of one item
    - Hear that a list with no item has nowhere to go

## axTemplate

Steps and keys: `docs/axKit/workflows/axTemplate.md` (in Claude's working folder: `workflows-axTemplate.md`).

1. **Make, write, rename and delete a template in axTemplate's browser**
    - Make a template
    - Write the text of a template
    - Rename a template
    - Delete a template and bring it back
    - Rename a template from its Title field
2. **Delete several templates, and meet a file that is no template, in axTemplate's browser**
    - Delete several templates at once
3. **Find a template in the launcher and use it**
    - Move through the templates
    - Find a template by a word of its name or its text
    - Read a template before using it
    - Copy a template without putting it in
    - Open the browser from the launcher
    - Quit the launcher

## axTerminal

Steps and keys: `docs/axKit/workflows/axTerminal.md` (in Claude's working folder: `workflows-axTerminal.md`).

1. **Start a terminal**
    - Move between the command field and the terminal
    - Hear the state, the shell and the prompt
2. **Run commands and read their output**
    - Run a command and read its output by headings
    - Turn the heading marks in the text off and on
    - Answer a program that asks
    - Send a command of several lines
    - Send a command without recording it
    - Put a recent command back in the field
    - Copy a command line of the terminal into the field
    - Interrupt a long command
    - Play music while a command runs
3. **Hear a command's output as it arrives**
    - Hear Done before a command's output
    - Turn the word Done off
4. **Switch shells and folders**
    - Start a new session in another shell
    - Switch to another shell
    - Hear the current folder's full path
    - Restart the shell
    - Start the shell again after it ended
5. **Use axTerminal's text, notices and axKit menu**
    - Clear the window
    - Set a bookmark and go back to it
    - Find text in the terminal
    - Read the keyboard shortcuts
    - Read the user guide
    - Close the window while a command runs
6. **Repeat an earlier command**
    - Go back through earlier commands
    - Come forward again to what was typed
    - Read the field in navigation mode
    - Go back to edit mode in the field
7. **Agree to the reminder before a shell first starts**
    - Agree to the reminder and start the shell
8. **Add a shell of your own**
    - Add a shell of your own
    - Switch to a shell of your own
    - Remove a shell of your own
9. **Use axTerminal's Context menu**
    - Bookmark a line and remove the bookmark
    - Copy a line into the command field
    - Run a Context command from the palette
10. **Start a shell in a recent folder from the command palette**
11. **Read a file that imitates the shell's marks**
12. **Change a setting of axTerminal when its settings file cannot be written**
13. **Take a command another window hands over**
    - Read a handed command in the field before sending it
    - Send the handed command yourself

## AI tools

Steps and keys: `docs/axKit/workflows/ai-tools.md` (in Claude's working folder: `workflows-ai-tools.md`).

1. **Let an AI tool read axKit, once you allow it**
    - Turn on the AI tools
    - Allow an AI tool to use axKit
    - Let an AI tool read a window's text, state and commands
    - Turn the AI tools off
2. **Let an AI tool run a window's commands**
    - Let AI tools run commands
    - Let an AI tool run a window's command
    - Let an AI tool change a setting
    - Stop AI tools from running commands
3. **Approve or deny an AI tool before it acts**
    - Read the explanation of a command an AI tool asks to run
    - Allow a command an AI tool asks to run
    - Deny a command an AI tool asks to run
4. **Let an AI tool read and add tasks**
    - Let an AI tool read the task lists
    - Let an AI tool read a list's items
    - Let AI tools run commands
    - Let an AI tool add a task
    - Undo a task an AI tool added
    - Let an AI tool add a task to another list
    - Let an AI tool add a task to a project's list
    - Stop AI tools from running commands
5. **Answer an AI tool's request in a list, with the text still there**
    - Read the explanation of a command an AI tool asks to run
    - Deny a command an AI tool asks to run
6. **Let an AI tool read a console window's conversation while you type**
    - Let an AI tool read the terminal while you type
    - Turn the AI tools off

## NVDA only

Steps and keys: `docs/axKit/workflows/nvda-only.md` (in Claude's working folder: `workflows-nvda-only.md`).

1. **Hear an offered command as NVDA reads it**
2. **Read a long reply with Say all while Claude writes**
3. **Come back to the right place after a Windows dialog**
4. **Hear axDown's notices and palette with NVDA**
5. **Find a name backwards with Shift and a letter**
    - Go back to the name before with Shift and its letter
6. **Put a template into another window from axTemplate's launcher, with NVDA**
7. **Hear the caution of a command another program put into the command field**
    - Hear that another program put the command there, before the command, and again on coming back to the field
