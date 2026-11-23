# The four families: what starts something, the functions, the events, the sounds

Written by `tools/families.ps1` from `docs/axKit/families.json`; never by hand. One data structure for the four families of axKit and the links between them, asked by the maintainer on 2026-11-16 so that he and the assistant name the same things by the same short names. It describes what the code does today, read from the code that day; it changes no behaviour. Since 2026-11-17 the kinds of event and the sounds are in code too (AxKit.Core/Events: EventKind, EventSound and the table EventPresenter), held to this file both ways by EventKindTests, and every sound is played by raising its kind at the door (AxKit.Forms/EventDoor; docs/axKit/events.md, section 10). Each of the four lists has the same shape, a family with its members. The keys stay in docs/axKit/key-families.json and the commands in docs/axKit/window-commands.json; this file names them and does not copy them. Where the same kind of event sounds one way in one place and another way elsewhere, the event's note says so: those are the open choices, and a sound is added or changed only when the maintainer has chosen it by ear (docs/axKit/earcons.md).

Status: proposed. In numbers: 56 kinds of event, 31 with a sound, 25 without; 24 sounds; 10 families of functions; 13 kinds of start.

## How to read it

Read it as a chain. Something starts (your own hands, the program underneath, an AI, axKit itself, Windows). When it is your hands, a key or a menu runs a function, a command of the window. The function ends in an event, a kind of thing that happened. The event has a sound or none, and words or none. Events that nobody's hands started enter the chain at the event.

Each thing has a short name to say or to search for, written `like this`: an event, a sound, a way of starting.

## 1. What starts something

### Your own hands

What you do yourself, in the window that has the focus or from anywhere in Windows. Only these run a command without anything being asked or said about who ran it.

- `key`: A key of the window. A key of one of the families of keys (docs/axKit/key-groups.md). It runs the command that holds it, or it is the control's own key, as the arrows are.
- `typing`: Typing. Characters typed into a field. Typing is no command: NVDA echoes it and axKit says nothing, except in a text's navigation mode, where a letter is a command or is refused.
- `menu`: A menu item. A command chosen from the menu bar or the Context menu.
- `palette`: The command palette. Ctrl+Shift+P, words typed, a line chosen with Enter. It runs what the menu item runs, and says a check item's new state since no menu shows it.
- `hotkey`: A key from anywhere in Windows. Ctrl+Shift with Space or a punctuation key, in any program: it brings up axStart, axClip or axHelp.

### The program under an access layer

A program axKit runs and reads for you: Claude Code, a shell, Git, KeePassXC's tool, an MCP server, the media engine, an installer. What it prints, asks and ends with reaches you as events you did not start at that moment.

- `program`: The program underneath. Its output, its questions, its state (working, ready), its end.

### An AI

An AI client the user paired, or AI help. What an AI starts always says so (rule 11): the words name who ran or asks, and anything that acts outside the window asks first.

- `ai-tool`: An AI tool's call. A paired AI client runs a command of a window by name, offers a command for you to run, adds a task, opens an app, or asks to be paired.
- `ai-help`: AI help answering. Claude's answer to Shift+F1 or Ask about this window, which comes up to a minute after you asked.

### axKit itself

What axKit does on its own: something it started for you ends later, a wait runs out, or another axKit window hands something over.

- `job`: Work of axKit's that ends later. A copy, a git command, a download, an install, a report being sent, a workflow run, a search of every list: started by your command, finished in the background.
- `timer`: A wait that ran out. A timer: axPass locking after idle minutes, the repeating chord of background work, a question shown once Claude is idle.
- `other-window`: Another axKit window. A second start that hands its file or text to the window already open, or a hotkey app bringing a window up.

### Windows

The system around axKit. axKit looks at these when its window comes to the front, not all the time.

- `system`: Windows. The clipboard changed, a file or folder changed on disk, the window came to the front or went behind, the computer was locked, the colours changed.

### The automated checks

Only in a test run, on a desktop of its own, never in your windows.

- `script`: A window script. The runner presses the key a command has now, or chooses its menu item (docs/axKit/command-language.md CL-2.1).

## 2. The functions

A function is a command of a window (`docs/axKit/command-language.md`): 1866 in the inventory, of which 274 are a key with no menu item. The menu a command is in is its family; its mark says how far it reaches.

### The marks, and the events a command of each mark ends in

- **read**: It says or shows something and changes nothing.
  - Ends in: `said`, `opened`.
- **open**: It opens a window or a file, or moves within the window.
  - Ends in: `landed`, `landed-on-heading`, `opened`, `nothing-found`, `edge`.
- **change**: It changes the window's own state or the file the window holds.
  - Ends in: `changed`, `done`, `refused`.
- **act**: It reaches outside the window: it deletes, moves, sends, starts a program, writes the clipboard or uses the network. An AI tool must ask before one runs.
  - Ends in: `done`, `sent`, `asked`, `busy`, `failed`, `cancelled`, `job-finished`, `late-notice`.

### The families, by menu

- `session`, the Session menu: What the window holds as a whole: open, new, save, refresh, restart, hide, exit, and the same place opened in another app.
  - 111 commands in 18 apps: read 10, open 40, change 15, act 46.
- `navigation`, the Navigation menu: Go somewhere in what the window holds: find, the next heading, link or list, go to a line, a bookmark, the next file.
  - 118 commands in 18 apps: read 88, open 29, change 1.
- `actions`, the Actions menu: Do something with the whole of what the window holds: undo and redo, run, push and pull, a new item, interrupt.
  - 126 commands in 18 apps: read 10, open 7, change 59, act 50.
- `context`, the Context menu: Do something with the one thing the focus is on: copy, cut, paste, rename, delete, open, information. Shift+F10 and Ctrl+Period open it.
  - 109 commands in 18 apps: read 14, open 28, change 21, act 46.
- `options`, the Options menu: Settings, each said with its new state: text, speech, sounds, announcements, what happens when something is done.
  - 285 commands in 18 apps: read 1, open 23, change 222, act 39.
- `window`, the Window menu: The other windows of axKit: a new window of any app, close all windows, say the status.
  - 343 commands in 18 apps: read 18, open 271, act 54.
- `axkit`, the axKit menu: The services of the whole bundle: the AI tools and what they may do, Claude for AI help, the theme.
  - 216 commands in 18 apps: open 36, change 36, act 144.
- `help`, the Help menu: The keys (F1), the user guide, the command palette, sending a report (F4), about, diagnostics and the key test.
  - 284 commands in 18 apps: read 162, open 18, act 104.
- `keys-alone`: Commands that are a key with no menu item, because their meaning depends on where the focus is: Tab between panes, Enter on a row, Right into a list, F3 for find next. An AI tool never runs these.
  - 274 rows of the inventory.
- `control-keys`: Not commands at all: the keys a control handles itself, the arrows in a list or a text and typing in a field. The inventory does not hold them; the families of keys do.

## 3. The events

### What came of your own command

You pressed a key, typed, or chose a command, and this is how it ended. Every command ends in one of these kinds, so nothing you do is ever silent. An AI tool that runs a command gets the same ending, with the words that say who ran it in front.

- `typed`: You typed a character into a field.
  - Started by: `typing`.
  - Heard: no sound.
  - Said: None from axKit: NVDA echoes what you type.
  - In: Every field.
- `landed`: The key or command moved you, and there is something there.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: no sound.
  - Said: NVDA reads the new place itself, or axKit says it: 'docs, 5 items, plan.md', 'Line 12 of 80', 'Install, heading level 2'.
  - In: Every window.
- `landed-on-heading`: The move landed on a heading of a text or on a section row of a list.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: sound `Heading`.
  - Said: The heading itself, read after the sound.
  - In: Texts with Markdown headings (axDown, axPreview), the text of every notice, and the section rows of the item windows (axTask, axPass, axMCP, axAutomate).
  - Note: Not in the conversations of axClaude and axTerminal, and not in axJSON.
- `taken`: In a text's navigation mode, a letter was taken as a command that has somewhere to go.
  - Started by: `key`.
  - Heard: sound `NavKey`.
  - Said: What the jump lands on, after the sound.
  - In: Every text of several lines that you can type in, while it is in navigation mode.
  - Note: Only for a letter alone. The same jump with Ctrl, from the menu or from the palette has no sound before it.
- `nothing-found`: The command ran and there is nothing of that kind to go to: no next heading, no match, nothing found.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: sound `Nothing` or `Edge`.
  - Said: Often the reason: 'No next heading', 'No commands match x', 'Nothing found for x', 'Not found'. Sometimes none.
  - In: Every window.
  - Note: Not one sound today. Nothing (the low buzz) only for a letter in navigation mode. Edge for a typed name that no entry starts with, for the palette and the hotkey windows with no match, for a section or heading jump in the item windows, and for axTask's search of every list. No sound at all, only words, for a Ctrl jump in an editor, for Find ('Not found') and for axFile's filter.
- `edge`: Nothing further that way: the first or last entry of a list, the top level, a row that cannot be gone into, the end of the history, a key that selects as it moves at an end of the list.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: sound `Edge`.
  - Said: None in a plain list. In the palette, the hotkey windows and axFile's folder field, the entry you are on is said again. 'Top of the fields', 'A section: Down goes into it'.
  - In: Every list of every window.
  - Note: Two places give words and no sound for the same thing: axMedia's 'First file' and 'End of queue' on Ctrl+Shift+Left and Right, and axFile's 'Top' on Backspace at This PC, where Left at the same place plays the edge.
- `no-command`: The key has no command where you are.
  - Started by: `key`.
  - Heard: sound `Unassigned` or `Edge`.
  - Said: None, or the reason: 'No action on Ctrl+4 in this list'.
  - In: Navigation mode of a text; the item windows; every window for Ctrl+Enter.
  - Note: Unassigned (the wooden bonk) only for a plain key that would type in navigation mode. Edge for Ctrl+Enter where the window has no main action, for Ctrl+Space where there is no text with modes, for Space on a row that cannot be checked, and for an action key with no action. A key no window knows gets nothing from axKit.
- `refused`: The change you asked for is not allowed here and nothing changed: navigation mode, a read-only text, a locked database, a form with a field still empty.
  - Started by: `key`, `typing`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: sound `Edge`.
  - Said: None for a key in navigation mode. From a menu, the palette or an AI tool the reason: 'Navigation mode: press Ctrl+Space to edit'. 'Not a task', 'Select an item first', 'Type the field's title'.
  - In: Every window.
  - Note: Some refusals have words and no sound: 'Read only: there is no edit mode here', axPass's 'Locked: Enter unlocks', axAutomate's 'This version views and runs the workflows'.
- `said`: A command that only tells you something said it: the status, the line number, the current folder.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: no sound.
  - Said: The information itself.
  - In: Every window.
- `changed`: A setting or a state of the window changed.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: no sound.
  - Said: The new state: 'Word wrap off', 'Text size 14', 'Navigation mode', 'checked'. From the palette, 'Word wrap, checked'.
  - In: Every window.
  - Note: A plain menu toggle says nothing by itself where its own code does not; run by an AI tool, only 'Claude ran Word wrap' is sure to be said.
- `done`: The command did its work at once.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: no sound.
  - Said: A few words: 'Saved', 'Copied', 'Undone: rename', 'Refreshed: Processes', 'Pasted, 3 lines'.
  - In: Every window.
- `sent`: Your message, command or answer went out to the program underneath.
  - Started by: `key`, `menu`.
  - Heard: sound `Sent`.
  - Said: 'Sent', 'Queued', 'Sent, not recorded', 'Sent: 1, Green'. In axClaude the word is a setting; in axTerminal it is always said.
  - In: axClaude and axTerminal.
  - Note: In axClaude the sound plays a second time, with no words, when axClaude itself confirms the review after your last answer.
- `recalled`: An earlier command came back into the field from the history.
  - Started by: `key`.
  - Heard: sound `Recall`.
  - Said: The command, or 'blank'.
  - In: axTerminal.
- `asked`: The command asks before it acts: 'Delete a.txt?', 'Save changes to notes.md?', a consent before the first connection.
  - Started by: `key`, `menu`, `palette`, `script`.
  - Heard: no sound.
  - Said: The question, read as the notice opens; the answers are a list or buttons.
  - In: Every window.
- `opened`: Something opened at your command: a help text, the palette, a field or a form in the window, another axKit window, an outside program.
  - Started by: `key`, `menu`, `palette`, `hotkey`, `ai-tool`, `script`.
  - Heard: no sound.
  - Said: NVDA reads what opened. 'Started Notepad', 'Opening x' for an outside program.
  - In: Every window.
  - Note: The focus arriving in a list, a text, a field or a form has a kind of its own since 2026-11-22, raised by the component itself: form-entered, list-entered, field-entered, text-entered. This kind is what is left: another window, an outside program.
- `form-entered`: The focus entered a form from outside it: several things asked for one action, Tab between them (SPEC AX-7.11). Once for the form; moving between its controls is silent.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: sound `FormOpen`.
  - Said: After NVDA has read the first control: the title and the one word, 'Move 2 items, form'. The explanation is not said: Ctrl+Q reads it. Two forms say their text after the word, since it is what will happen and not how a form is used: the report, and an action of an axTask list.
  - In: Every window with a form: the report (F4), axTask's item form, Move to section, Move to a list and an action's form, axMCP's Add server, axTerminal's Add a shell, the item windows' New field, axClaude's New session.
- `list-entered`: The focus entered a list from outside it: one opened, Tab or F6 came to it, a notice closed over it, the window started on it (SPEC AX-7.11). Moving inside it is silent.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: sound `ListOpen`.
  - Said: NVDA reads the list and its line.
  - In: Every list of every window, and the answers in the field's place.
- `field-entered`: The focus entered a field to type in from outside it: a message field, a document, a name asked for, Find, the command palette, a rename in place (SPEC AX-7.11).
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: sound `FieldOpen`.
  - Said: NVDA reads the field.
  - In: Every field of every window.
- `text-entered`: The focus entered a text to read from outside it: a conversation, a help text, a question's words with its buttons, an answer, a file shown read-only (SPEC AX-7.11).
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: sound `TextOpen`.
  - Said: NVDA reads the text.
  - In: Every read-only text of every window.
  - Note: When the same command just played another sound (a notice that opened by itself, a refusal, a program's question), that sound stands alone and the entry is silent. So for all four kinds.
- `busy`: The command started work that takes time.
  - Started by: `key`, `menu`, `palette`, `script`.
  - Heard: no sound.
  - Said: 'Unlocking x', 'Calling x', 'Searching every list', then after a second 'x, please wait'. Pressed again: 'Working'.
  - In: axFile, axGIT, axPass, axMCP, axTask, axAutomate, AI help, the updater.
  - Note: No sound for a start or an end outside axClaude; the end is the event job-finished.
- `failed`: The command could not do it.
  - Started by: `key`, `menu`, `palette`, `ai-tool`, `script`.
  - Heard: sound `Notice`.
  - Said: One line is spoken with no sound: 'Failed: the reason'. Several lines, or an error with a title, open a notice, and that notice plays the notice sound.
  - In: Every window.
  - Note: The error notice chimes even though your own command caused it, because every error notice is marked as coming by itself. A program that an action of an axTask list ran and that failed, or could not be started, plays the same sound with its one line, 'Miss failed, exit code 1', since it ends some time after the key; its output then opens as a plain notice.
- `cancelled`: You said no, pressed Escape, or the thing was called off.
  - Started by: `key`, `menu`.
  - Heard: no sound.
  - Said: 'Cancelled', 'Delete not run', 'Not started'.
  - In: Every window.

### Something arrived

Something came in that you did not ask for at that moment: a line from the program underneath, a file changed by another program, a copy on the clipboard. Mostly there is nothing to do.

- `line`: A line of output arrived from the program.
  - Started by: `program`.
  - Heard: sound `Click`.
  - Said: None with the sound; the line may be spoken after it (reply-spoken).
  - In: axClaude and axTerminal.
- `tool-call`: Claude called a tool.
  - Started by: `program`.
  - Heard: sound `Tool`.
  - Said: The tool call's row, when Speak tool calls is on.
  - In: axClaude.
- `reply-spoken`: Lines of a reply or of a command's output became final and are spoken.
  - Started by: `program`.
  - Heard: no sound.
  - Said: The lines, joined into one announcement that does not interrupt.
  - In: axClaude (Speak replies) and axTerminal (Speak output).
- `program-said`: The program announced something of its own: Claude's bracketed rows such as accept edits on, its detailed view on or off, the conversation cleared.
  - Started by: `program`.
  - Heard: no sound.
  - Said: The row, or 'Conversation cleared', 'Claude's detailed view is on'.
  - In: axClaude.
- `status-changed`: The status bar or the title followed the program: working, waiting for your answer, 2 messages waiting, the mode line, the shell's folder.
  - Started by: `program`, `timer`.
  - Heard: no sound.
  - Said: None. F8 says the status when you ask.
  - In: Every window with a status bar.
- `clipboard-entry`: Something was copied in any program while axClip keeps the history.
  - Started by: `system`.
  - Heard: no sound.
  - Said: None; the list is refilled with your place kept.
  - In: axClip.
- `changed-on-disk`: Another program changed the file or the folder the window shows. axKit looks when the window comes to the front.
  - Started by: `system`.
  - Heard: no sound.
  - Said: 'Reloaded, notes.md changed on disk', or 'notes.md changed on disk; Save will ask'. axFile refills silently.
  - In: axDown, axJSON, axPreview, axFile; the item windows find it at a save.
- `handed-over`: Another axKit window handed something to this one: a file to open, files to play, text for the message.
  - Started by: `other-window`.
  - Heard: no sound.
  - Said: 'Playing song.mp3', 'Text added to your message from another window, not sent', or NVDA reads the file that opened.
  - In: axPreview, axMedia, axClaude, the hotkey apps.

### Where the program stands

The steps of a turn with the program underneath: it is ready, it began to answer, it is working, it is done, it ended. Rising opens, falling closes.

- `ready`: The program is ready for your first message or command of this run.
  - Started by: `program`.
  - Heard: sound `Ready`.
  - Said: 'Ready'.
  - In: axClaude and axTerminal.
- `responding`: Claude began its reply.
  - Started by: `program`.
  - Heard: sound `Responding`.
  - Said: 'Responding'.
  - In: axClaude.
  - Note: axTerminal has no such moment.
- `working`: The program is working: Claude on a turn, a command running in the shell.
  - Started by: `program`.
  - Heard: sound `Music`.
  - Said: None; the status bar says working.
  - In: axClaude and axTerminal.
- `turn-self-started`: The program began a turn by itself: work in the background ended and Claude Code took it up, with no message of yours.
  - Started by: `program`.
  - Heard: no sound.
  - Said: 'Claude goes on by itself'.
  - In: axClaude.
  - Note: Words only, by a switch of Options, Announcements; the working music has started just before them.
- `turn-done`: The turn is over: Claude finished, a shell command finished, a program that an action of an axTask list ran ended without an error, the last file of axMedia's queue ended.
  - Started by: `program`.
  - Heard: sound `Done`.
  - Said: 'Done'; in axTerminal also 'Failed' or 'Failed, exit code 2'; in axMedia 'End of queue'; in axTask the action's name and 'done'.
  - In: axClaude, axTerminal, axMedia, axTask.
  - Note: A shell command that failed plays the same sound as one that worked. axMedia's sound has no switch. The call in MainForm.PutAway.cs, for a turn ending while axClaude is put away, is switched off in code.
- `next-track`: The next file of the queue started playing.
  - Started by: `program`.
  - Heard: no sound.
  - Said: 'Playing song.mp3', when Say each file as it starts is on.
  - In: axMedia.
- `program-ended`: The program underneath ended or crashed.
  - Started by: `program`.
  - Heard: no sound.
  - Said: 'Claude stopped', 'The shell ended', 'server stopped (exit code 1)'.
  - In: axClaude, axTerminal, axMCP.
  - Note: No sound today, although it ends everything the turn sounds announce.

### Something asks you

Something wants an answer or your attention, and you did not start it at that moment. What asks to run code is the loudest of all.

- `program-asks`: The program asks a question or a permission.
  - Started by: `program`.
  - Heard: sound `Notice` or `Question`.
  - Said: The question and the answer you are on, or 'Claude asks', or in axTerminal 'Asks: the row'.
  - In: axClaude and axTerminal.
  - Note: Two sounds for one thing. Notice (the trill) when axClaude shows the question as a list in the window. Question (two notes) when it cannot be shown as a list, and for axTerminal's guess that a program asks.
- `program-screen`: Claude shows a screen that only informs: a side answer, a goal.
  - Started by: `program`.
  - Heard: sound `Notice`.
  - Said: The title and the first line.
  - In: axClaude.
- `question-closed`: The program took its question back by itself.
  - Started by: `program`.
  - Heard: no sound.
  - Said: 'Question closed'.
  - In: axClaude.
- `command-offer`: Claude offers a command for you to run yourself on your computer.
  - Started by: `ai-tool`.
  - Heard: sound `Offer`.
  - Said: 'Claude offers a command for you to run', the command, and the caution. Running is never where the list starts.
  - In: axClaude.
  - Note: Its sound plays wherever the window is, and the taskbar button flashes when the window is behind; the louder background alert never takes its place.
- `ai-asks-to-run`: An AI tool asks to run a command of this window that acts outside the window.
  - Started by: `ai-tool`.
  - Heard: sound `Offer`.
  - Said: 'Command offer. Claude asks to run this window's command Delete on a.txt', with the caution; No is the default.
  - In: Every window but axPass and axClip.
  - Note: The same sound as command-offer; docs/axKit/earcons.md proposes a variant of its own.
- `pairing`: A new AI client asks to use axKit.
  - Started by: `ai-tool`.
  - Heard: sound `Notice`.
  - Said: 'Claude asks to use axKit', with a consent box; Deny is the default.
  - In: Every window.
- `notice-by-itself`: A notice opened that no command of yours asked for: an update was found at start, the settings could not be read or saved or were changed outside axKit, a hotkey is taken, a program is missing, the folder is gone, a repository wants to be trusted, the window met an unexpected error.
  - Started by: `job`, `timer`, `system`, `program`, `other-window`.
  - Heard: sound `Notice`.
  - Said: The notice's own text, and Enter is held for a second so that a key already on its way answers nothing.
  - In: Every window.
  - Note: The sound can be switched off only in axClaude and axTerminal (with their chimes). In those two windows the settings notes are a line of the conversation and make no sound.
- `late-notice`: A notice that comes some time after your command, when its work is finished: 'Claude says', 'x is installed', 'Result of x', 'Report not sent', the unlock question after axPass locked itself.
  - Started by: `job`, `ai-help`, `timer`, `system`.
  - Heard: no sound.
  - Said: The notice's own text.
  - In: Every window.
  - Note: No sound, although you may have moved on to something else: these notices are not marked as coming by themselves.
- `called-from-behind`: The program is done or asks while its window is not in front.
  - Started by: `program`.
  - Heard: sound `Alert`.
  - Said: Those of the event behind it; the taskbar button flashes.
  - In: axClaude and axTerminal.
  - Note: Off at first. When on, it takes the place of the done and question sounds, and plays even with the chimes off.

### Work that goes on behind

Work that runs while you do something else, and what is done on your behalf without your hands.

- `background-started`: Claude started work in the background: an agent, a shell, a workflow.
  - Started by: `program`.
  - Heard: sound `BackgroundStart`.
  - Said: 'Agent started: the description', when Say background work is on.
  - In: axClaude.
- `background-running`: Work is still running in the background.
  - Started by: `timer`.
  - Heard: sound `BackgroundChord`.
  - Said: None; the status bar counts what runs.
  - In: axClaude.
- `background-finished`: Work in the background finished.
  - Started by: `program`.
  - Heard: sound `BackgroundFinish`.
  - Said: 'The description finished', when Say background work is on.
  - In: axClaude.
- `job-finished`: Work of axKit's own that you started a while ago ended: a copy, a push, an install, a report sent, a server started, a workflow run, a picture's text read.
  - Started by: `job`, `program`.
  - Heard: no sound.
  - Said: The result: 'Pasted 3 items', 'Pushed', 'Git is installed', 'Sent, report 12', 'Text read: 40'. A failure is 'Failed: the reason' or an error notice.
  - In: axFile, axGIT, axPass, axMCP, axAutomate, axPreview, the updater, the report sender.
  - Note: No sound for the end of any work outside axClaude.
- `ai-ran`: An AI tool ran a command of this window or added a task.
  - Started by: `ai-tool`.
  - Heard: sound `Notice`.
  - Said: 'Claude ran Word wrap', joined to what the command itself says; 'Claude added Buy milk to Home'.
  - In: Every window but axPass and axClip.
  - Note: The sound and a flash of the taskbar only when the window is not in front, once for a burst, since NVDA does not speak for a window behind. This is the one use of the notice sound that no switch turns off.
- `locked-itself`: axPass locked the database: the idle minutes ran out, or Windows was locked.
  - Started by: `timer`, `system`.
  - Heard: no sound.
  - Said: 'Locked', then the unlock question.
  - In: axPass.
- `upkeep`: axKit did something small for you without a word: a list refilled, the theme repainted, a copied password cleared from the clipboard after 30 seconds, an entry's details read.
  - Started by: `timer`, `system`, `job`.
  - Heard: no sound.
  - Said: None, on purpose.
  - In: Every window.

### The window itself

A window opening, coming back, or changing how its keys work.

- `window-opened`: A window opened or came to the front for the first time.
  - Started by: `key`, `menu`, `hotkey`, `ai-tool`, `other-window`.
  - Heard: no sound.
  - Said: NVDA reads the title and the focus; what the window had to say before it was in front follows as one sentence.
  - In: Every window.
  - Note: No sound today. docs/axKit/earcons.md proposes a short sound of each app's own, and one for the start of axKit.
- `brought-up`: A hotkey brought axClaude's window back while the message field still held unsent text.
  - Started by: `hotkey`, `other-window`.
  - Heard: sound `Recall`.
  - Said: 'Unsent message in the field'.
  - In: axClaude.
  - Note: The recall sound has a second meaning here; its first is a command come back from the history.
- `start-note`: The window had something to tell at its start: a remembered folder on the network was not opened, no project folder is chosen.
  - Started by: `job`.
  - Heard: no sound.
  - Said: The note, said when the window comes to the front, or a line of the conversation in axClaude and axTerminal.
  - In: Every window.

## 4. The sounds

### Ticks: something arrived, nothing to do

Tiny and struck, over within a few hundredths of a second, never a held note. Ticks never wait behind each other.

- `Click`: A typewriter strike: a burst of noise over a 1500 Hz tone, 10 ms.
  - Chosen: The sound of 1.3.0, which the maintainer likes; softer ones tried in 2.4.0 went unheard under speech.
  - Switch: Options, Sounds, Ticks for new lines, in axClaude and axTerminal.
  - The sound of: `line`.
  - Played from 2 files: AxClaude/MainForm.Model.cs, AxTerminal/TerminalForm.Model.cs.
- `Tool`: A tiny mouth pop, 30 ms, a round tone falling from 700 towards 300 Hz.
  - Chosen: By ear, 2026-11-15, the first of three pops.
  - Switch: Options, Sounds, Ticks for new lines and tool calls, in axClaude.
  - The sound of: `tool-call`.
  - Played from 1 file: AxClaude/MainForm.Model.cs.

### Moves: where your own key took you

Very short, right after your key and over before NVDA speaks. Each says how the move ended.

- `Edge`: A4 falling to A3, loud, about an eighth of a second.
  - Chosen: Asked 2026-10-22; made louder and given its upper note on 2026-10-23 after it was lost under speech. Not chosen from candidates.
  - Switch: None: it always plays.
  - The sound of: `nothing-found`, `edge`, `no-command`, `refused`.
  - Played from 18 files: AxFile/FileForm.Browse.cs, AxFile/FileForm.Completion.cs, AxKit.Forms/AxKitForm.cs, AxKit.Forms/AxKitForm.Modes.cs, AxKit.Forms/AxKitForm.Notices.cs, AxKit.Forms/AxKitList.cs, AxKit.Forms/AxKitTextBox.cs, AxKit.Forms/ItemForm.cs, AxKit.Forms/ItemForm.Fields.cs, AxKit.Forms/ListSelection.cs, AxKit.Forms/OverlayPanel.cs, AxKit.Forms/TextEditorForm.cs, AxTask/TaskForm.Actions.cs, AxTask/TaskForm.Approvals.cs, AxTask/TaskForm.Commands.cs, AxTask/TaskForm.cs, AxTask/TaskForm.Projects.cs, AxTerminal/TerminalForm.Input.cs.
- `Heading`: G4 leaping an octave to G5, 85 ms.
  - Chosen: Asked 2026-11-11. Not chosen from candidates.
  - Switch: Options, Text, Sound on headings, in every window.
  - The sound of: `landed-on-heading`.
  - Played from 1 file: AxKit.Forms/AxKitForm.Theme.cs.
- `NavKey`: A high glassy tick: a pure 3800 Hz, 6 ms.
  - Chosen: By ear, 2026-11-16, from five ticks; the first click was too like the line tick.
  - Switch: None: it always plays.
  - The sound of: `taken`.
  - Played from 1 file: AxKit.Forms/AxKitForm.Modes.cs.
- `Nothing`: A low buzz: 150 Hz with its third and fifth harmonics, 110 ms.
  - Chosen: By ear, 2026-11-16, from four flat tones.
  - Switch: None: it always plays.
  - The sound of: `nothing-found`.
  - Played from 1 file: AxKit.Forms/AxKitForm.Modes.cs.
- `Unassigned`: A wooden bonk, 70 ms, falling from 420 towards 260 Hz with a wood block's overtone.
  - Chosen: By ear, 2026-11-16, from four bonks, after four kinds of blip were turned down.
  - Switch: None: it always plays.
  - The sound of: `no-command`.
  - Played from 2 files: AxKit.Forms/AxKitForm.Modes.cs, AxKit.Forms/ItemForm.cs.
- `Recall`: E5 rising a fourth to A5, 85 ms.
  - Chosen: Asked 2026-10-23. Not chosen from candidates.
  - Switch: None: it always plays.
  - The sound of: `recalled`, `brought-up`.
  - Played from 2 files: AxClaude/MainForm.PutAway.cs, AxTerminal/TerminalForm.Input.cs.

### Openings: a way of working starts

Bell pings, a voice used for nothing else. Every one begins with the same mark, three quick beeps running up, which says the way you work has changed; then it goes down as its kind. An extended tone is text, discrete beeps are items, and a form has both.

- `FormOpen`: The mark, then beep, beep, tone, and the same lower: D6 twice and a held D5, A5 twice and a held A4. 0.7 s, the most complex, as a form is.
  - Chosen: By ear, 2026-11-22: 'Mark 4 with the descending kinds' and 'Form B', after three sets and a second round in one voice.
  - Switch: Options, Text, Entry sounds, in every window.
  - The sound of: `form-entered`.
  - Played from 1 file: AxKit.Forms/AxKitForm.Theme.cs.
- `ListOpen`: The mark, then four quick beeps in a line going down by steps: A5, F sharp 5, E5, D5. 0.4 s.
  - Chosen: By ear, 2026-11-22, with the others of its family.
  - Switch: Options, Text, Entry sounds, in every window.
  - The sound of: `list-entered`.
  - Played from 1 file: AxKit.Forms/AxKitForm.Theme.cs.
- `FieldOpen`: The mark, then one extended tone, A4: the simplest. 0.44 s.
  - Chosen: By ear, 2026-11-22, with the others of its family.
  - Switch: Options, Text, Entry sounds, in every window.
  - The sound of: `field-entered`.
  - Played from 1 file: AxKit.Forms/AxKitForm.Theme.cs.
- `TextOpen`: The mark, then one extended tone an octave lower, A3, a little softer. 0.44 s.
  - Chosen: By ear, 2026-11-22, with the others of its family.
  - Switch: Options, Text, Entry sounds, in every window.
  - The sound of: `text-entered`.
  - Played from 1 file: AxKit.Forms/AxKitForm.Theme.cs.

### Turns: where the conversation stands

One to three smooth notes at an even pace. Rising opens, falling closes.

- `Ready`: One high note, C6.
  - Chosen: From the first versions. Not chosen from candidates.
  - Switch: Options, Sounds, Chimes, in axClaude and axTerminal.
  - The sound of: `ready`.
  - Played from 2 files: AxClaude/MainForm.Model.cs, AxTerminal/TerminalForm.Model.cs.
- `Sent`: One low note, A4.
  - Chosen: From the first versions. Not chosen from candidates.
  - Switch: Options, Sounds, Chimes, in axClaude and axTerminal.
  - The sound of: `sent`.
  - Played from 4 files: AxClaude/MainForm.Area.cs, AxClaude/MainForm.Input.cs, AxClaude/MainForm.Mcp.cs, AxTerminal/TerminalForm.Input.cs.
- `Responding`: C5, E5, up to a held G5.
  - Chosen: From the first versions. Not chosen from candidates.
  - Switch: Options, Sounds, Chimes, in axClaude.
  - The sound of: `responding`.
  - Played from 1 file: AxClaude/MainForm.cs.
- `Done`: G5, E5, down to a held C5.
  - Chosen: From the first versions. Not chosen from candidates.
  - Switch: Options, Sounds, Chimes, in axClaude and axTerminal. No switch in axMedia.
  - The sound of: `turn-done`.
  - Played from 4 files: AxClaude/MainForm.PutAway.cs, AxKit.Forms/ConsoleForm.Signals.cs, AxMedia/MediaForm.Player.cs, AxTask/TaskForm.Actions.cs.

### Asks: something wants an answer

Short figures on one or two notes. docs/axKit/earcons.md proposes to make these one scale of knocks, where the number of knocks says how much is asked.

- `Question`: The same note twice, E5.
  - Chosen: From the first versions. Not chosen from candidates.
  - Switch: Options, Sounds, Chimes, in axClaude and axTerminal.
  - The sound of: `program-asks`.
  - Played from 1 file: AxKit.Forms/ConsoleForm.Signals.cs.
- `Notice`: A quick trill, C6 and E6 twice.
  - Chosen: Not chosen from candidates. The earcons proposal retires it for the knocks.
  - Switch: Options, Sounds, Chimes, in axClaude and axTerminal. No switch in the other windows.
  - The sound of: `failed`, `program-asks`, `program-screen`, `pairing`, `notice-by-itself`, `ai-ran`.
  - Played from 4 files: AxClaude/MainForm.Area.cs, AxKit.Forms/AxKitForm.Mcp.cs, AxKit.Forms/AxKitForm.Notices.cs, AxTask/TaskForm.Actions.cs.

### Code: something asks to run or to act beyond the window

An alarm and not a chime, the loudest class, and the only one that glides. Decide with care.

- `Offer`: Five swoops falling from 2400 to 900 Hz, 400 ms in all.
  - Chosen: By ear, 2026-11-13, played one by one.
  - Switch: Options, Sounds, Chimes, in axClaude and axTerminal. No switch in the other windows; the words are said either way.
  - The sound of: `command-offer`, `ai-asks-to-run`.
  - Played from 2 files: AxClaude/MainForm.Mcp.cs, AxKit.Forms/AxKitForm.Mcp.cs.

### The call from another window

Long and loud, the only sound longer than half a second: go to that window.

- `Alert`: G5, C6 up to a held E6, and once more.
  - Chosen: Not chosen from candidates.
  - Switch: Options, When Claude is done (in axTerminal, When a command is done), Louder alert when the window is in the background. Off at first.
  - The sound of: `called-from-behind`.
  - Played from 1 file: AxKit.Forms/ConsoleForm.Signals.cs.

### Lasting sounds: work that goes on

Not a signal of a moment: sound that tells you something is still going on, soft enough to speak over.

- `Music`: Four slow soft chords with a faint ping in each, 14 seconds, looping without a seam.
  - Chosen: Made to the maintainer's description, 2026-11-15: gentler, like the lift of a building of the future.
  - Switch: Options, Sounds, Background music, or Ctrl+M, in axClaude and axTerminal. Off at first.
  - The sound of: `working`.
  - Played from 1 file: AxKit.Forms/ConsoleForm.Signals.cs.
- `BackgroundStart`: Three quick bell notes rising, with a sparkle.
  - Chosen: By ear, 2026-11-15: what arrives rises.
  - Switch: Options, Background work, Play a sound when background work starts and ends, in axClaude.
  - The sound of: `background-started`.
  - Played from 1 file: AxClaude/MainForm.Background.cs.
- `BackgroundFinish`: Three quick bell notes falling, with a sparkle.
  - Chosen: By ear, 2026-11-15: what leaves falls.
  - Switch: Options, Background work, Play a sound when background work starts and ends, in axClaude.
  - The sound of: `background-finished`.
  - Played from 1 file: AxClaude/MainForm.Background.cs.
- `BackgroundChord`: A slow soft chord with a long tail, one of four at random, with a tone more for each thing running.
  - Chosen: By ear, 2026-11-15.
  - Switch: Options, Background work, Play a sound while background work runs, in axClaude; the pause between chords is chosen there too.
  - The sound of: `background-running`.
  - Played from 1 file: AxClaude/MainForm.Background.cs.

## 5. The keys, and what you can hear after one

Each family of keys (`docs/axKit/key-groups.md`) with the kinds of event its keys end in, and so the sounds that can follow a key of that family.

- `system-wide`, Ctrl+Shift+Space and Ctrl+Shift+punctuation: bring up axKit from anywhere in Windows.
  - `window-opened`: no sound.
  - `brought-up`: sound `Recall`.
- `ctrl-punct`, Ctrl+punctuation: the in-window partner of the same key with Ctrl+Shift.
  - `opened`: no sound.
  - `busy`: no sound.
  - `late-notice`: no sound.
  - Note: Ctrl+Period opens the Context menu. Ctrl+Slash asks Claude about the window: the answer is a late notice.
- `space`, Space and Ctrl+Space: select, or toggle.
  - `changed`: no sound.
  - `no-command`: sound `Unassigned` or `Edge`.
  - Note: Ctrl+Space says the new mode. Space on a row that cannot be checked plays the edge.
- `ctrl-letter`, Ctrl+letter: act on what is selected.
  - `done`: no sound.
  - `changed`: no sound.
  - `said`: no sound.
  - `opened`: no sound.
  - `asked`: no sound.
  - `busy`: no sound.
  - `failed`: sound `Notice`.
  - `refused`: sound `Edge`.
- `ctrl-digit`, Ctrl+digit: go to the next heading of that level.
  - `landed`: no sound.
  - `landed-on-heading`: sound `Heading`.
  - `nothing-found`: sound `Nothing` or `Edge`.
  - `no-command`: sound `Unassigned` or `Edge`.
  - `refused`: sound `Edge`.
  - Note: In axTask, Ctrl with a digit is also an action key of a list, which is where no-command and refused come from.
- `ctrl-shift-digit`, Ctrl+Shift+digit: go to the previous heading of that level.
  - `landed`: no sound.
  - `landed-on-heading`: sound `Heading`.
  - `nothing-found`: sound `Nothing` or `Edge`.
- `alt-digit`, Alt+digit: go to a place or view of the window.
  - `landed`: no sound.
- `ctrl-shift-letter`, Ctrl+Shift+letter: the reverse or the wider form of the same letter with Ctrl.
  - `done`: no sound.
  - `changed`: no sound.
  - `opened`: no sound.
  - `asked`: no sound.
  - `busy`: no sound.
  - `failed`: sound `Notice`.
- `f-keys`, F-keys (F1 to F12 alone): say or show.
  - `said`: no sound.
  - `opened`: no sound.
  - `landed`: no sound.
  - `nothing-found`: sound `Nothing` or `Edge`.
  - `done`: no sound.
  - Note: F1 and F4 open, F3 lands or says Not found, F5 refreshes, F6 lands in the other pane, F8 says the status.
- `shift-f-keys`, Shift+F-key, and the Applications key: the reverse or the other form of the same F-key.
  - `opened`: no sound.
  - `landed`: no sound.
  - `nothing-found`: sound `Nothing` or `Edge`.
  - `busy`: no sound.
  - `late-notice`: no sound.
- `enter`, Enter: open or confirm.
  - `opened`: no sound.
  - `landed`: no sound.
  - `sent`: sound `Sent`.
  - `asked`: no sound.
  - `edge`: sound `Edge`.
  - `refused`: sound `Edge`.
- `enter-forms`, Enter with Ctrl, Shift or Alt: another form of open or confirm.
  - `sent`: sound `Sent`.
  - `done`: no sound.
  - `opened`: no sound.
  - `no-command`: sound `Unassigned` or `Edge`.
  - `refused`: sound `Edge`.
  - Note: Ctrl+Enter where the window has no main action plays the edge.
- `escape`, Escape and Shift+Escape: go back or close; with Shift, stop the program under the window.
  - `cancelled`: no sound.
  - `landed`: no sound.
  - `said`: no sound.
  - `done`: no sound.
  - Note: Escape in axClaude's message field only speaks; Shift+Escape interrupts the program.
- `delete`, Delete and Ctrl+Shift+Delete: remove.
  - `asked`: no sound.
  - `done`: no sound.
  - `refused`: sound `Edge`.
- `ctrl-tab`, Ctrl+Tab and Ctrl+Shift+Tab: move between panes.
  - `landed`: no sound.
- `moves`, The arrows, Page Up, Page Down, Tab, Shift+Tab and Backspace: move through what you are on.
  - `landed`: no sound.
  - `landed-on-heading`: sound `Heading`.
  - `edge`: sound `Edge`.
- `shift-select`, Shift with an arrow, Home, End or Space: select.
  - `landed`: no sound.
  - `edge`: sound `Edge`.
- `ctrl-arrows`, Ctrl with an arrow, Home or End, and Ctrl+Shift with Left or Right arrow: a bigger step.
  - `landed`: no sound.
  - `landed-on-heading`: sound `Heading`.
  - `nothing-found`: sound `Nothing` or `Edge`.
  - `edge`: sound `Edge`.
  - `recalled`: sound `Recall`.
  - Note: In axTerminal's command field Ctrl+Up and Ctrl+Down go through the history: recalled, or the edge at its end.
- `alt-arrows`, Alt with an arrow: the choices of what you are on.
  - `done`: no sound.
  - `edge`: sound `Edge`.
- `text-size`, Ctrl+Plus and Ctrl+Minus: make the text larger or smaller.
  - `changed`: no sound.
- `letter`, A letter alone: in a view you read, a quick command: go to the next thing of a kind, or say something.
  - `taken`: sound `NavKey`.
  - `landed`: no sound.
  - `landed-on-heading`: sound `Heading`.
  - `nothing-found`: sound `Nothing` or `Edge`.
  - `no-command`: sound `Unassigned` or `Edge`.
  - `refused`: sound `Edge`.
  - Note: The three sounds of navigation mode belong to this family: the tick, the buzz and the bonk.
- `alt-letter`, Alt+letter: open a menu.
  - `opened`: no sound.

## 6. Events with no sound

Every kind of event that has words or a change on the screen and no sound of its own today, by class. A sound is added only when the maintainer has chosen it by ear.

- What came of your own command:
  - `typed`: You typed a character into a field.
  - `landed`: The key or command moved you, and there is something there.
  - `said`: A command that only tells you something said it: the status, the line number, the current folder.
  - `changed`: A setting or a state of the window changed.
  - `done`: The command did its work at once.
  - `asked`: The command asks before it acts: 'Delete a.txt?', 'Save changes to notes.md?', a consent before the first connection.
  - `opened`: Something opened at your command: a help text, the palette, a field or a form in the window, another axKit window, an outside program.
  - `busy`: The command started work that takes time.
  - `cancelled`: You said no, pressed Escape, or the thing was called off.

- Something arrived:
  - `reply-spoken`: Lines of a reply or of a command's output became final and are spoken.
  - `program-said`: The program announced something of its own: Claude's bracketed rows such as accept edits on, its detailed view on or off, the conversation cleared.
  - `status-changed`: The status bar or the title followed the program: working, waiting for your answer, 2 messages waiting, the mode line, the shell's folder.
  - `clipboard-entry`: Something was copied in any program while axClip keeps the history.
  - `changed-on-disk`: Another program changed the file or the folder the window shows. axKit looks when the window comes to the front.
  - `handed-over`: Another axKit window handed something to this one: a file to open, files to play, text for the message.

- Where the program stands:
  - `turn-self-started`: The program began a turn by itself: work in the background ended and Claude Code took it up, with no message of yours.
  - `next-track`: The next file of the queue started playing.
  - `program-ended`: The program underneath ended or crashed.

- Something asks you:
  - `question-closed`: The program took its question back by itself.
  - `late-notice`: A notice that comes some time after your command, when its work is finished: 'Claude says', 'x is installed', 'Result of x', 'Report not sent', the unlock question after axPass locked itself.

- Work that goes on behind:
  - `job-finished`: Work of axKit's own that you started a while ago ended: a copy, a push, an install, a report sent, a server started, a workflow run, a picture's text read.
  - `locked-itself`: axPass locked the database: the idle minutes ran out, or Windows was locked.
  - `upkeep`: axKit did something small for you without a word: a list refilled, the theme repainted, a copied password cleared from the clipboard after 30 seconds, an entry's details read.

- The window itself:
  - `window-opened`: A window opened or came to the front for the first time.
  - `start-note`: The window had something to tell at its start: a remembered folder on the network was not opened, no project folder is chosen.
