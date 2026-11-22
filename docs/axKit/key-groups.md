# Key groups

Written by `tools/key-map.ps1` from `docs/axKit/key-families.json`, `docs/axKit/window-commands.json` and
`src/AxKit.Forms/BundleKeys.cs`; never edit it by hand. The keys of axKit by group: what kind of thing each
pattern of keys does, so that a key can be guessed before it is looked up. The same keys by key, with every app,
are in `key-map.md`.

The groups are proposed: an exception is a command that sits on a key of a group and does another kind of thing today.

## The groups in short

From anywhere in Windows:

- Ctrl+Shift+Space and Ctrl+Shift+punctuation: bring up axKit from anywhere in Windows.

The same in every axKit window:

- Ctrl+punctuation: the in-window partner of the same key with Ctrl+Shift.
- Space and Ctrl+Space: select, or toggle.
- F-keys (F1 to F12 alone): say or show.
- Shift+F-key, and the Applications key: the reverse or the other form of the same F-key.
- Enter: open or confirm.
- Escape and Shift+Escape: go back or close; with Shift, stop the program under the window.
- Delete and Ctrl+Shift+Delete: remove.
- Ctrl+Tab and Ctrl+Shift+Tab: move between panes.
- The arrows, Page Up, Page Down, Tab, Shift+Tab and Backspace: move through what you are on.
- Shift with an arrow, Home, End or Space: select.
- Ctrl+Plus and Ctrl+Minus: make the text larger or smaller.
- Alt+letter: open a menu.

Inside one app's window:

- Ctrl+letter: act on what is selected.
- Ctrl+digit: go to the next heading of that level.
- Ctrl+Shift+digit: go to the previous heading of that level.
- Alt+digit: go to a place or view of the window.
- Ctrl+Shift+letter: the reverse or the wider form of the same letter with Ctrl.
- Enter with Ctrl, Shift or Alt: another form of open or confirm.
- Ctrl with an arrow, Home or End, and Ctrl+Shift with Left or Right arrow: a bigger step.
- Alt with an arrow: the choices of what you are on.
- A letter alone: in a view you read, a quick command: go to the next thing of a kind, or say something.

## From anywhere in Windows

These keys work in any program, with no axKit window open: they bring an axKit app up. Each is a setting of its app and can be changed there.

### Ctrl+Shift+Space and Ctrl+Shift+punctuation: bring up axKit from anywhere in Windows

Hold Ctrl and Shift and press Space or a punctuation key, in any program: an axKit app comes up. These are the only keys of axKit that work outside its windows (SPEC AX-8.11).

The keys:

- Ctrl+Shift+Space: axStart's search field.
- Ctrl+Shift+Slash: axHelp's conversation.
- Ctrl+Shift+Comma: axHelp's screenshot.
- Ctrl+Shift+Semicolon: axClip's search field.
- Ctrl+Shift+Period: Kept for a later help launcher.

## The same in every axKit window

These keys do the same kind of thing in every window of axKit, so they are learned once.

### Ctrl+punctuation: the in-window partner of the same key with Ctrl+Shift

Ctrl and a punctuation key stays inside the window you are in, where the same key with Ctrl and Shift works from anywhere. Ctrl+Slash asks Claude about this window, as Shift+F1 does; with Shift added it brings up axHelp's conversation from any program. Ctrl+Period opens the Context menu, as Shift+F10 does; its partner with Shift is kept for later.

The keys:

- Ctrl+Period, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: The Context menu.
- Ctrl+Period, axAutomate, axMCP, axPass and axTask: The field's menu.
- Ctrl+Period, axFile: Context menu.
- Ctrl+Slash, every app: Ask about this window.

### Space and Ctrl+Space: select, or toggle

Space selects the thing you are on in a list of things you can act on several of at once, and deselects it again (the maintainer's rule of 2026-11-22, SPEC AX-8.16: axFile, axTask and axMedia so far). On a check box it checks and unchecks. Ctrl+Space switches between navigating and editing.

The keys:

- Space, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: Select or deselect.
- Space, axAutomate, axMCP, axPass and axTask: Check the field.
- Space, axFile: Select or deselect this entry.

### F-keys (F1 to F12 alone): say or show

The function keys are the same in every window: F1 help, F3 find next, F4 send a report, F6 the other pane. F2 and F5 are Windows' own rename and refresh.

The keys:

- F1, every app: Keyboard shortcuts.
- F6, axProcess: Back to the list.
- F8, every app: Say the status.

Exceptions today, which do another kind of thing:

- F2, axAutomate, axFile, axMCP, axPass and axTask: Rename.
- F2, axAutomate, axMCP, axPass and axTask: Rename the field.
- F3, axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal: Find next.
- F3, axGIT: Find next in the text.
- F4, every app: Send a report.
- F5, axFile, axGIT, axMCP, axPass, axProcess and axTask: Refresh.
- F5, axStart: Refresh programs.
- F5, axPreview: Reload.
- F5, axAutomate: Run.
- F6, axAutomate, axMCP, axPass and axTask: Switch between the navigator and the detail pane.
- F6, axDown, axJSON and axPreview: Between the text and the details.
- F6, axMedia: Back to the list.
- F6, axClaude and axTerminal: Switch between the view and the field.
- F6, axFile: Switch between the folder list and the Actions list.
- F6, axCalc: Switch between the history and the field.
- F6, axGIT: Switch between the list and the text.

### Shift+F-key, and the Applications key: the reverse or the other form of the same F-key

Shift with a function key is that key's other form: Shift+F3 finds the previous, Shift+F1 asks Claude about the window instead of reading the help. Shift+F10 is Windows' own key for the context menu, and the Applications key does the same.

The keys:

- Shift+F1, every app: Ask about this window.
- Shift+F3, axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal: Find previous.
- Shift+F3, axGIT: Find previous in the text.

Windows' own keys, as in every program:

- Applications key, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: The Context menu.
- Applications key, axAutomate, axMCP, axPass and axTask: The field's menu.
- Applications key, axFile: Context menu.
- Shift+F10, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: The Context menu.
- Shift+F10, axAutomate, axMCP, axPass and axTask: The field's menu.
- Shift+F10, axFile: Context menu.

### Enter: open or confirm

Enter opens what you are on or says yes to a question.

The keys:

- Enter, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess and axTask: Open.
- Enter, axAutomate, axMCP, axPass and axTask: Change the field.
- Enter, axCalc: Answer.
- Enter, axTask: Change the item in its form.
- Enter, axProcess: Details.
- Enter, axFile: Go to the action's place.
- Enter, axTask: Open it where it lives.
- Enter, axTask: Open its form.
- Enter, axHelp: Open the conversation.
- Enter, axPreview: Open the detail.
- Enter, axTask: Open the file in axJSON.
- Enter, axTask: Open the item's form.
- Enter, axClip: Paste.
- Enter, axMCP: Read the resource.
- Enter, axCalc: Recall the entry.
- Enter, axGIT: Show diff.
- Enter, axStart: Start.
- Enter, axPass: Unlock.

### Escape and Shift+Escape: go back or close; with Shift, stop the program under the window

Escape goes back one step or closes what is open. It never loses work without asking. Shift+Escape is for the program under an access layer: it interrupts Claude, or sends Escape to the terminal's program.

The keys:

- Escape, axGIT, axMedia, axProcess and axTask: Deselect all.
- Escape, axAutomate, axMCP, axPass and axTask: To the navigator.
- Escape, axClip, axHelp and axStart: Hide.
- Escape, axTask: Back from the search.
- Escape, axCalc: Back to the field.
- Escape, axFile: Back to the folder list.
- Escape, axGIT: To the list.
- Shift+Escape, axClaude: Interrupt.
- Shift+Escape, axTerminal: Send Escape.

Exceptions today, which do another kind of thing:

- Escape, axCalc: Clear the field.
- Escape, axFile: Clear the selection or the filter.

### Delete and Ctrl+Shift+Delete: remove

Delete removes what you are on, and what can be lost asks first or can be undone. Ctrl+Shift+Delete clears a whole history, as in a web browser.

The keys:

- Delete, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess and axTask: Delete.
- Delete, axAutomate, axMCP, axPass and axTask: Remove the field.
- Delete, axTask: Archive.
- Delete, axGIT: Discard changes.
- Delete, axProcess: End process.
- Delete, axClip: Remove.
- Delete, axCalc: Remove the entry.
- Delete, axPass: To the Recycle Bin.
- Ctrl+Shift+Delete, axCalc and axClip: Clear history.

### Ctrl+Tab and Ctrl+Shift+Tab: move between panes

Ctrl+Tab goes to the next part of the window and Ctrl+Shift+Tab to the one before.

The keys:

- Ctrl+Tab, axAutomate, axMCP, axPass and axTask: Switch between the navigator and the detail pane.
- Ctrl+Tab, axDown, axJSON and axPreview: Between the text and the details.
- Ctrl+Tab, axMedia and axProcess: Back to the list.
- Ctrl+Tab, axClaude and axTerminal: Switch between the view and the field.
- Ctrl+Tab, axFile: Switch between the folder list and the Actions list.
- Ctrl+Tab, axCalc: Switch between the history and the field.
- Ctrl+Tab, axGIT: Switch between the list and the text.
- Ctrl+Shift+Tab, axAutomate, axMCP, axPass and axTask: Switch between the navigator and the detail pane.
- Ctrl+Shift+Tab, axDown, axJSON and axPreview: Between the text and the details.
- Ctrl+Shift+Tab, axMedia and axProcess: Back to the list.
- Ctrl+Shift+Tab, axClaude and axTerminal: Switch between the view and the field.
- Ctrl+Shift+Tab, axFile: Switch between the folder list and the Actions list.
- Ctrl+Shift+Tab, axCalc: Switch between the history and the field.
- Ctrl+Shift+Tab, axGIT: Switch between the list and the text.

### The arrows, Page Up, Page Down, Tab, Shift+Tab and Backspace: move through what you are on

The arrows and Page Up and Page Down move through a list or a text, as in every Windows program. In a list of levels, Right arrow goes in and Left arrow or Backspace comes back out; on a field with a choice of values, Left and Right arrow choose the value. Tab and Shift+Tab go to the next and the previous part of the window.

Windows' own keys, as in every program:

- Backspace, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: Up a level.
- Backspace, axFile: Up one level.
- Down arrow, axClip and axStart: Next match.
- Left arrow, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: Up a level.
- Left arrow, axAutomate, axMCP, axPass and axTask: The previous value of the field.
- Left arrow, axFile: Up one level.
- Left arrow, axProcess: Up to This computer.
- Left arrow, axGIT: Up to the repository.
- Page Down, axClip and axStart: Page down.
- Page Up, axClip and axStart: Page up.
- Right arrow, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: Into the list.
- Right arrow, axAutomate, axMCP, axPass and axTask: The next value of the field.
- Right arrow, axFile: Into the folder.
- Right arrow, axTask: Open it where it lives.
- Tab, axAutomate, axMCP, axPass and axTask: To the detail pane.
- Tab, axAutomate, axMCP, axPass and axTask: To the navigator.
- Tab, axClaude and axTerminal: Switch between the view and the field.
- Tab, axFile: Switch between the folder list and the Actions list.
- Tab, axGIT: To the list.
- Tab, axGIT: To the text.
- Up arrow, axClip and axStart: Previous match.
- Shift+Tab, axAutomate, axMCP, axPass and axTask: To the detail pane.
- Shift+Tab, axAutomate, axMCP, axPass and axTask: To the navigator.
- Shift+Tab, axClaude and axTerminal: Switch between the view and the field.
- Shift+Tab, axFile: Switch between the folder list and the Actions list.
- Shift+Tab, axGIT: To the list.
- Shift+Tab, axGIT: To the text.

### Shift with an arrow, Home, End or Space: select

Shift with an arrow, Home or End selects as it moves, as in every Windows list and text. Shift+Space selects from the nearest selected thing to here. The keys, their words and their code are one shared piece (SPEC AX-8.16).

The keys:

- Shift+Space, axFile, axGIT, axMedia, axProcess and axTask: Select to here.

Windows' own keys, as in every program:

- Shift+Down arrow, axGIT and axMedia: Select to the next file.
- Shift+Down arrow, axProcess and axTask: Select to the next item.
- Shift+Down arrow, axFile: Select to the next entry.
- Shift+End, axGIT and axMedia: Select to the last file.
- Shift+End, axProcess and axTask: Select to the last item.
- Shift+End, axFile: Select to the last entry.
- Shift+Home, axGIT and axMedia: Select to the first file.
- Shift+Home, axProcess and axTask: Select to the first item.
- Shift+Home, axFile: Select to the first entry.
- Shift+Up arrow, axGIT and axMedia: Select to the previous file.
- Shift+Up arrow, axProcess and axTask: Select to the previous item.
- Shift+Up arrow, axFile: Select to the previous entry.

### Ctrl+Plus and Ctrl+Minus: make the text larger or smaller

Ctrl+Plus makes the text larger and Ctrl+Minus smaller, in every window, as in a web browser.

Windows' own keys, as in every program:

- Ctrl+Minus, every app: Smaller text.
- Ctrl+Plus, every app: Larger text.

### Alt+letter: open a menu

Alt and a letter opens the menu that starts with that letter, as in every Windows program.

Exceptions today, which do another kind of thing:

- Alt+D, axFile: Go to folder.

## Inside one app's window

These keys belong to the window you are in. The pattern says what kind of thing the key does; what exactly it does depends on the app.

### Ctrl+letter: act on what is selected

Ctrl and a letter does something to what you are on, and the letter is the first letter of the command where it can be. The keys every Windows program has (copy, paste, save, find) are the same here.

The keys:

- Ctrl+D, axFile: Add or remove this folder.
- Ctrl+D, axFile: Add to or remove from Favorites.
- Ctrl+D, axHelp: Put a picture file into the conversation.
- Ctrl+E, axPreview: Edit in axDown.
- Ctrl+E, axFile: Editor.
- Ctrl+E, axGIT: Open file in editor.
- Ctrl+E, axTask: Show in axFile.
- Ctrl+E, axProcess: Show program in axFile.
- Ctrl+J, axTask: Edit the list's configuration.
- Ctrl+J, axTask: Open in axJSON.
- Ctrl+J, axAutomate: The workflow's file as text.
- Ctrl+L, axTerminal: Clear transcript.
- Ctrl+L, axPass: Lock.
- Ctrl+M, axTask: Move to a list.
- Ctrl+M, axTask: Move to section.
- Ctrl+W, axClaude, axDown, axFile, axGIT, axJSON, axMedia, axPreview and axTerminal: File Explorer.
- Ctrl+W, axFile: Show the folder in File Explorer.

Windows' own keys, as in every program:

- Ctrl+A, axFile, axGIT, axMedia, axProcess and axTask: Select all.
- Ctrl+A, axGIT: Select all files.
- Ctrl+C, axFile: Copy.
- Ctrl+C, axProcess: Copy line.
- Ctrl+C, axPreview: Copy the detail's value.
- Ctrl+F, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: Find in the list.
- Ctrl+F, axClaude, axDown, axFile, axJSON, axPreview and axTerminal: Find.
- Ctrl+F, axCalc: Find in the history.
- Ctrl+F, axGIT: Find in the text.
- Ctrl+N, axAutomate, axMCP, axPass and axTask: New field.
- Ctrl+N, axAutomate, axMCP, axPass and axTask: New item.
- Ctrl+N, axFile, axMedia and axPreview: New window.
- Ctrl+N, axDown and axJSON: New.
- Ctrl+N, axClaude and axTerminal: New session.
- Ctrl+N, axMCP: Add server.
- Ctrl+N, axGIT: New branch.
- Ctrl+N, axPass: New entry.
- Ctrl+O, axDown, axJSON and axPreview: Open.
- Ctrl+O, axAutomate: Open a folder of workflows.
- Ctrl+O, axPass: Open database.
- Ctrl+O, axStart: Open file location.
- Ctrl+O, axMedia: Open files.
- Ctrl+O, axFile: Open folder.
- Ctrl+O, axGIT: Open repository.
- Ctrl+O, axHelp: Open the last screenshot in axPreview.
- Ctrl+S, axDown and axJSON: Save.
- Ctrl+S, axCalc: Save history.
- Ctrl+V, axFile: Paste.
- Ctrl+X, axFile: Cut.
- Ctrl+Y, axDown and axJSON: Redo.
- Ctrl+Z, axAutomate, axDown, axFile, axJSON, axMCP, axPass and axTask: Undo.
- Ctrl+Z, axFile: Undo compress.
- Ctrl+Z, axFile: Undo delete.
- Ctrl+Z, axFile: Undo move.
- Ctrl+Z, axFile: Undo new folder.
- Ctrl+Z, axFile: Undo rename.
- Ctrl+Z, axFile: Undo the action.

Exceptions today, which do another kind of thing:

- Ctrl+B, axClaude: What runs in the background.
- Ctrl+E, axJSON: Error.
- Ctrl+G, axDown, axJSON and axPreview: Go to line.
- Ctrl+G, axFile: Go to folder.
- Ctrl+H, axDown and axPreview: Heading.
- Ctrl+I, axFile and axStart: Information.
- Ctrl+I, axDown and axPreview: List item.
- Ctrl+I, axCalc: Answer in other bases.
- Ctrl+I, axJSON: Item.
- Ctrl+I, axFile: The action's number and time.
- Ctrl+J, axJSON: Object.
- Ctrl+K, axDown and axPreview: Link.
- Ctrl+K, axJSON: Key.
- Ctrl+L, axDown and axPreview: List.
- Ctrl+L, axJSON: Array.
- Ctrl+M, axClaude and axTerminal: Background music on or off.
- Ctrl+P, axClip: Pause recording.
- Ctrl+P, axTerminal: Say current folder.
- Ctrl+Q, axAutomate, axClip, axMCP, axPass and axTask: Read in full.
- Ctrl+Q, axDown and axPreview: Block quote.
- Ctrl+Q, axClaude: Claude's question again.
- Ctrl+Q, axCalc: Say the last answer.
- Ctrl+Q, axProcess: Summary.
- Ctrl+Q, axMedia: What is playing.
- Ctrl+Q, axTerminal: What is running.
- Ctrl+R, axTerminal: Recent commands.
- Ctrl+R, axAutomate: The last run's report.
- Ctrl+T, axDown and axPreview: Table.
- Ctrl+T, axMedia: Go to time.

### Ctrl+digit: go to the next heading of that level

Ctrl and a number from 1 to 6 goes to the next heading of that level in a text or a list with headings.

The keys:

- Ctrl+1, axDown and axPreview: Heading level 1.
- Ctrl+1, axTask: Next heading level 1.
- Ctrl+2, axDown and axPreview: Heading level 2.
- Ctrl+2, axTask: Next heading level 2.
- Ctrl+3, axDown and axPreview: Heading level 3.
- Ctrl+3, axTask: Next heading level 3.
- Ctrl+4, axDown and axPreview: Heading level 4.
- Ctrl+5, axDown and axPreview: Heading level 5.
- Ctrl+6, axDown and axPreview: Heading level 6.

Exceptions today, which do another kind of thing:

- Ctrl+0, axTask: All tasks.
- Ctrl+1, axTask: Action key 1.
- Ctrl+1, axGIT: Changes.
- Ctrl+1, axTerminal: Command field.
- Ctrl+1, axClaude: Message field.
- Ctrl+1, axProcess: Processes.
- Ctrl+2, axTask: Action key 2.
- Ctrl+2, axClaude: Conversation.
- Ctrl+2, axGIT: Log.
- Ctrl+2, axProcess: Services.
- Ctrl+2, axTerminal: Terminal.
- Ctrl+3, axTask: Action key 3.
- Ctrl+3, axGIT: Branches.
- Ctrl+3, axTerminal: Latest output.
- Ctrl+3, axClaude: Latest response.
- Ctrl+3, axProcess: Startup.
- Ctrl+4, axTask: Action key 4.
- Ctrl+5, axTask: Action key 5.
- Ctrl+6, axTask: Action key 6.
- Ctrl+7, axTask: Action key 7.
- Ctrl+8, axTask: Action key 8.
- Ctrl+9, axTask: Action key 9.

### Ctrl+Shift+digit: go to the previous heading of that level

Adding Shift to Ctrl and a number goes backwards: the previous heading of that level.

The keys:

- Ctrl+Shift+1, axDown and axPreview: Heading level 1.
- Ctrl+Shift+1, axTask: Action key Shift+1.
- Ctrl+Shift+1, axTask: Previous heading level 1.
- Ctrl+Shift+2, axDown and axPreview: Heading level 2.
- Ctrl+Shift+2, axTask: Action key Shift+2.
- Ctrl+Shift+2, axTask: Previous heading level 2.
- Ctrl+Shift+3, axDown and axPreview: Heading level 3.
- Ctrl+Shift+3, axTask: Action key Shift+3.
- Ctrl+Shift+3, axTask: Previous heading level 3.
- Ctrl+Shift+4, axDown and axPreview: Heading level 4.
- Ctrl+Shift+4, axTask: Action key Shift+4.
- Ctrl+Shift+5, axDown and axPreview: Heading level 5.
- Ctrl+Shift+5, axTask: Action key Shift+5.
- Ctrl+Shift+6, axDown and axPreview: Heading level 6.
- Ctrl+Shift+6, axTask: Action key Shift+6.
- Ctrl+Shift+7, axTask: Action key Shift+7.
- Ctrl+Shift+8, axTask: Action key Shift+8.
- Ctrl+Shift+9, axTask: Action key Shift+9.

### Alt+digit: go to a place or view of the window

Alt and a number goes to one of the window's main places or views: the first, the second, the third. Built on 2026-11-16 in axClaude, axTerminal, axGIT and axProcess. Ctrl+1, 2 and 3, the keys of these places until then, still work there for now.

The keys:

- Alt+1, axGIT: Changes.
- Alt+1, axTerminal: Command field.
- Alt+1, axClaude: Message field.
- Alt+1, axProcess: Processes.
- Alt+2, axClaude: Conversation.
- Alt+2, axGIT: Log.
- Alt+2, axProcess: Services.
- Alt+2, axTerminal: Terminal.
- Alt+3, axGIT: Branches.
- Alt+3, axTerminal: Latest output.
- Alt+3, axClaude: Latest response.
- Alt+3, axProcess: Startup.

### Ctrl+Shift+letter: the reverse or the wider form of the same letter with Ctrl

Adding Shift to Ctrl and a letter gives the reverse or the bigger form of that command. A letter or a number is always inside the window; Space or punctuation with Ctrl+Shift is always the whole of axKit.

The keys:

- Ctrl+Shift+A, axFile, axGIT, axMedia, axProcess and axTask: Deselect all.
- Ctrl+Shift+C, axCalc: Copy answer.
- Ctrl+Shift+C, axFile: Copy path.
- Ctrl+Shift+C, axFile: Copy paths.
- Ctrl+Shift+E, axJSON: Error.
- Ctrl+Shift+F, axTask: Find in every list.
- Ctrl+Shift+H, axDown and axPreview: Heading.
- Ctrl+Shift+I, axDown and axPreview: List item.
- Ctrl+Shift+I, axJSON: Item.
- Ctrl+Shift+J, axJSON: Object.
- Ctrl+Shift+K, axDown and axPreview: Link.
- Ctrl+Shift+K, axJSON: Key.
- Ctrl+Shift+L, axDown and axPreview: List.
- Ctrl+Shift+L, axJSON: Array.
- Ctrl+Shift+N, axAutomate, axMCP, axPass and axTask: New list.
- Ctrl+Shift+N, axDown and axJSON: New window.
- Ctrl+Shift+N, axPass: New group.
- Ctrl+Shift+O, axMedia: Open folder.
- Ctrl+Shift+Q, axDown and axPreview: Block quote.
- Ctrl+Shift+S, axDown and axJSON: Save as.
- Ctrl+Shift+S, axGIT: Stage.
- Ctrl+Shift+T, axDown and axPreview: Table.
- Ctrl+Shift+U, axGIT: Unstage.
- Ctrl+Shift+V, axPreview: Read the clipboard's picture.
- Ctrl+Shift+W, axFile: Show in File Explorer.
- Ctrl+Shift+Z, axDown and axJSON: Redo.

Exceptions today, which do another kind of thing:

- Ctrl+Shift+C, axClaude: Ctrl+C.
- Ctrl+Shift+C, axTerminal: Interrupt.
- Ctrl+Shift+D, axTask: Hide done tasks.
- Ctrl+Shift+F, axGIT: Fetch.
- Ctrl+Shift+F, axFile: Filter.
- Ctrl+Shift+F, axJSON: Format.
- Ctrl+Shift+H, axGIT: Push.
- Ctrl+Shift+K, axClaude and axTerminal: Bookmark this line.
- Ctrl+Shift+L, axGIT: Pull.
- Ctrl+Shift+M, axClaude: Next permission mode.
- Ctrl+Shift+N, axFile: New folder.
- Ctrl+Shift+P, every app: Command palette.
- Ctrl+Shift+Q, axFile: Add to axMedia's queue.
- Ctrl+Shift+R, axClaude: Force Claude to restart.
- Ctrl+Shift+R, axTerminal: Restart shell.
- Ctrl+Shift+S, axClaude: Send queued messages now.
- Ctrl+Shift+T, axFile: New text file.
- Ctrl+Shift+W, axDown, axJSON and axPreview: Word wrap.

### Enter with Ctrl, Shift or Alt: another form of open or confirm

Enter with a modifier is another way of opening or saying yes. Ctrl+Enter does the window's main thing: it sends the message or the command, commits, saves a form, marks a task done in axTask, or pauses and resumes in axMedia. Shift+Enter opens what you are on another way, such as a preview. Alt+Enter shows its properties and Ctrl+Shift+Enter starts a program as administrator, as everywhere in Windows.

The keys:

- Ctrl+Enter, axGIT: Commit.
- Ctrl+Enter, axTask: Done or not done.
- Ctrl+Enter, axMedia: Pause or resume.
- Ctrl+Enter, axFile: Play folder in axMedia.
- Ctrl+Enter, axTerminal: Send the command.
- Ctrl+Enter, axClaude: Send the message.
- Shift+Enter, axClip: Copy without pasting.
- Shift+Enter, axFile: Preview.

Windows' own keys, as in every program:

- Ctrl+Shift+Enter, axStart: Start as administrator.
- Alt+Enter, axFile: Properties.

### Ctrl with an arrow, Home or End, and Ctrl+Shift with Left or Right arrow: a bigger step

Ctrl with an arrow takes a bigger step: Ctrl+Up and Ctrl+Down go to the previous and the next section, Ctrl+Left and Ctrl+Right go back and forward 15 seconds in a recording. Ctrl+Home and Ctrl+End go to the first and the last. With Shift too, a bigger step still: the previous or the next file.

The keys:

- Ctrl+Down arrow, axAutomate, axGIT, axMCP, axPass, axProcess and axTask: Next section.
- Ctrl+End, axClip and axStart: Last match.
- Ctrl+Home, axClip and axStart: First match.
- Ctrl+Left arrow, axMedia: Back 15 seconds.
- Ctrl+Right arrow, axMedia: Forward 15 seconds.
- Ctrl+Up arrow, axAutomate, axGIT, axMCP, axPass, axProcess and axTask: Previous section.
- Ctrl+Shift+Left arrow, axMedia: Previous file.
- Ctrl+Shift+Right arrow, axMedia: Next file.

Exceptions today, which do another kind of thing:

- Ctrl+Down arrow, axMedia: Slower.
- Ctrl+Up arrow, axMedia: Faster.

### Alt with an arrow: the choices of what you are on

Alt with an arrow is for the choices of what you are on, in the arrows' own directions (the maintainer's rule of 2026-11-21): Alt+Right goes into the list of them, where the arrows move and Enter chooses; Alt+Down and Alt+Up change to the next and the previous one in place; Alt+Left steps out to what is around it and offers what can be added there. Ctrl with an arrow moves the cursor a bigger step, and moving an item of a list up or down is Ctrl+Shift+Up and Ctrl+Shift+Down, wherever a list has it.

The keys:

- Alt+Down arrow, axJSON: Next value.
- Alt+Left arrow, axJSON: Add a member.
- Alt+Right arrow, axJSON: Choose a value.
- Alt+Up arrow, axJSON: Previous value.

Exceptions today, which do another kind of thing:

- Alt+Up arrow, axFile: Up one level.

### A letter alone: in a view you read, a quick command: go to the next thing of a kind, or say something

Where you read and do not type (a text in navigation mode, a preview), a letter alone is a command: H goes to the next heading and Shift+H to the one before, L says the line. In a text's navigation mode, a letter that moves plays a high tick first, and a letter with no command plays a wooden bonk.

The keys:

- L, axPreview: Say the line.

## Keys in no group yet

None: every key of the inventory is in a group.
