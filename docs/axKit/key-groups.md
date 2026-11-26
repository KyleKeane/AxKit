# Key groups

Written by `tools/key-map.ps1` from `docs/axKit/key-families.json`, `docs/axKit/window-commands.json` and
`src/AxKit.Forms/BundleKeys.cs`; never edit it by hand. The keys of axKit by group: what kind of thing each
pattern of keys does, so that a key can be guessed before it is looked up. The same keys by key, with every app,
are in `key-map.md`.

The groups are settled: an exception is a command that sits on a key of a group and does another kind of thing today.

## The groups in short

From anywhere in Windows:

- Ctrl+Shift+Space and Ctrl+Shift+punctuation: bring up axKit from anywhere in Windows.

The same in every axKit window:

- Ctrl+punctuation: the in-window partner of the same key with Ctrl+Shift.
- Space and Ctrl+Space: select, or toggle.
- Ctrl+F-key: open a standalone app.
- Ctrl+Shift+F-key: open an access layer.
- F-keys (F1 to F12 alone): say or show.
- Shift+F-key, and the Applications key: the reverse or the other form of the same F-key.
- Enter: open or confirm.
- Escape and Shift+Escape: go back or close; with Shift, Escape sent to the program under the window.
- Delete, Ctrl+Delete and Ctrl+Shift+Delete: remove.
- Ctrl+Tab and Ctrl+Shift+Tab: move between panes.
- The arrows, Page Up, Page Down, Tab, Shift+Tab and Backspace: move through what you are on.
- Shift with an arrow, Home, End or Space: select.
- Ctrl+Plus and Ctrl+Minus: make the text larger or smaller.
- Alt+letter: open a menu.

Inside one app's window:

- Ctrl+letter: act on what is selected.
- Ctrl+digit: the window's own numbered action.
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
- Ctrl+Shift+Period: axHelp's screenshot.
- Ctrl+Shift+Comma: axClip's search field.
- Ctrl+Shift+Semicolon: axTemplate's search field.
- Ctrl+Shift+APOSTROPHE: Kept for axAutomate's launcher.

## The same in every axKit window

These keys do the same kind of thing in every window of axKit, so they are learned once.

### Ctrl+punctuation: the in-window partner of the same key with Ctrl+Shift

Ctrl and a punctuation key stays inside the window you are in, where the same key with Ctrl and Shift works from anywhere. Ctrl+Slash asks Claude about this window, as Shift+F1 does; with Shift added it brings up axHelp's conversation from any program. Ctrl+Period opens the Context menu, as Shift+F10 does; its partner with Shift is kept for later. Ctrl+Comma opens the command palette, as F1 does, a second key on trial; with Shift added it brings up axClip's search from anywhere.

The keys:

- Ctrl+Period, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: The Context menu.
- Ctrl+Period, axAutomate, axMCP, axPass, axTask and axTemplate: The field's menu.
- Ctrl+Period, axFile: The line's menu.
- Ctrl+Slash, every app: Ask about this window.

### Space and Ctrl+Space: select, or toggle

Space selects the thing you are on in a list of things you can act on several of at once, and deselects it again (the maintainer's rule of 2026-11-22, SPEC AX-8.16: axFile, axTask and axMedia so far). On a check box it checks and unchecks. Ctrl+Space switches between navigating and editing.

The keys:

- Space, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Select or deselect.
- Space, axAutomate, axMCP, axPass, axTask and axTemplate: Check the field.
- Space, axFile: Select or deselect this entry.

### Ctrl+F-key: open a standalone app

Ctrl with a function key opens a standalone axKit app from any axKit window, in the order of the Window menu (SPEC AX-8.18 rule 6). Which key opens which app is written on the page of the apps, apps.md, and in each window's F1 text.

The keys:

- Ctrl+F1, every app: axStart.
- Ctrl+F2, every app: axFile.
- Ctrl+F3, every app: axClip.
- Ctrl+F4, every app: axTemplate.
- Ctrl+F5, every app: axDown.
- Ctrl+F6, every app: axJSON.
- Ctrl+F7, every app: axPreview.
- Ctrl+F8, every app: axMedia.
- Ctrl+F9, every app: axTask.
- Ctrl+F10, every app: axCalc.
- Ctrl+F11, every app: axProcess.
- Ctrl+F12, every app: axAutomate.

### Ctrl+Shift+F-key: open an access layer

Ctrl+Shift with a function key opens an access layer, an app over a program you install, from any axKit window, in the order of the Window menu (SPEC AX-8.18 rule 6).

The keys:

- Ctrl+Shift+F1, every app: axTerminal, access layer for your shell.
- Ctrl+Shift+F2, every app: axGIT, access layer for Git.
- Ctrl+Shift+F3, every app: axPass, access layer for KeePassXC.
- Ctrl+Shift+F5, every app: axClaude, access layer for Claude Code.
- Ctrl+Shift+F9, every app: axHelp, access layer for Claude Code.
- Ctrl+Shift+F10, every app: axMCP, access layer for MCP servers.

### F-keys (F1 to F12 alone): say or show

A function key alone says or shows, the same in every window: F7 reads in full what you are on, F8 reads it with details, F9 says the status, F11 shows the text another way, F12 opens the field to go to a line, a folder or a time. F1 opens the command palette, palette and help in one: with nothing typed, the rules of the keys and the window's keys. F2 rename, F3 find next, F4 send a report, F5 refresh and F6 the other pane are Windows' own keys, kept as Windows taught them.

The keys:

- F1, every app: Command palette.
- F7, axAutomate, axClip, axMCP, axPass, axTask and axTemplate: Read in full.
- F7, axClaude: Claude's question again.
- F7, axCalc: Say the last answer.
- F7, axProcess: Summary.
- F7, axMedia: What is playing.
- F7, axTerminal: What is running.
- F8, axFile and axStart: Information.
- F8, axCalc: Answer in other bases.
- F8, axTerminal: Say current folder.
- F8, axFile: The favorite's folder.
- F8, axAutomate: The last run's report.
- F8, axClaude: What runs in the background.
- F9, every app: Say the status.
- F11, axDown, axJSON and axPreview: Word wrap.
- F12, axDown, axJSON and axPreview: Go to line.
- F12, axFile: Go to folder.
- F12, axMedia: Go to time.

Exceptions today, which do another kind of thing:

- F2, axAutomate, axFile, axMCP, axPass, axTask and axTemplate: Rename.
- F3, axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal: Find next.
- F3, axGIT: Find next in the text.
- F4, every app: Send a report.
- F5, axAutomate, axFile, axGIT, axMCP, axPass, axProcess, axTask and axTemplate: Refresh.
- F5, axStart: Refresh programs.
- F5, axPreview: Reload.
- F6, axAutomate, axMCP, axPass, axTask and axTemplate: Switch between the navigator and the detail pane.
- F6, axCalc, axClaude and axTerminal: Switch between the view and the field.
- F6, axMedia and axProcess: Back to the list.
- F6, axDown and axJSON: Between the text and the field under it.
- F6, axPreview: Between the text and the details.
- F6, axFile: Switch between the list and the Favorites list.
- F6, axGIT: Switch between the list and the text.

### Shift+F-key, and the Applications key: the reverse or the other form of the same F-key

Shift with a function key is that key's other form: Shift+F3 finds the previous, Shift+F1 asks Claude about the window instead of reading the help. Shift+F10 is Windows' own key for the context menu, and the Applications key does the same.

The keys:

- Shift+F1, every app: Ask about this window.
- Shift+F3, axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal: Find previous.
- Shift+F3, axGIT: Find previous in the text.

Windows' own keys, as in every program:

- Applications key, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: The Context menu.
- Applications key, axAutomate, axMCP, axPass, axTask and axTemplate: The field's menu.
- Applications key, axFile: The line's menu.
- Shift+F10, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: The Context menu.
- Shift+F10, axAutomate, axMCP, axPass, axTask and axTemplate: The field's menu.
- Shift+F10, axFile: The line's menu.

### Enter: open or confirm

Enter opens what you are on or says yes to a question.

The keys:

- Enter, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Open.
- Enter, axAutomate, axMCP, axPass, axTask and axTemplate: Change the field.
- Enter, axCalc: Answer.
- Enter, axProcess: Details.
- Enter, axTask: Open it where it lives.
- Enter, axTask: Open its form.
- Enter, axHelp: Open the conversation.
- Enter, axPreview: Open the detail.
- Enter, axFile: Open the favorite.
- Enter, axTask: Open the file in axJSON.
- Enter, axClip: Paste.
- Enter, axTemplate: Put in.
- Enter, axMCP: Read the resource.
- Enter, axGIT: Show diff.
- Enter, axStart: Start.
- Enter, axPass: Unlock.

### Escape and Shift+Escape: go back or close; with Shift, Escape sent to the program under the window

Escape only goes back one step or closes what is open; it never clears what you typed and never loses work without asking. Shift+Escape sends Escape to the terminal's program in axTerminal.

The keys:

- Escape, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Deselect all.
- Escape, axAutomate, axMCP, axPass, axTask and axTemplate: To the navigator.
- Escape, axClip, axHelp, axStart and axTemplate: Hide.
- Escape, axFile and axGIT: To the list.
- Escape, axTask: Back from the search.
- Escape, axCalc: Back: in the field, nothing to go back to.
- Shift+Escape, axTerminal: Send Escape.

### Delete, Ctrl+Delete and Ctrl+Shift+Delete: remove

Delete removes what you are on, and what can be lost asks first or can be undone. Ctrl+Delete clears what you typed: a list's filter, axCalc's field. Ctrl+Shift+Delete clears a whole history, as in a web browser: axCalc's and axClip's history, axTerminal's transcript.

The keys:

- Delete, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Delete.
- Delete, axTask: Archive.
- Delete, axGIT: Discard changes.
- Delete, axProcess: End process.
- Delete, axClip: Remove.
- Delete, axFile: Remove from Favorites.
- Delete, axPass: To the Recycle Bin.
- Ctrl+Delete, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Clear the filter.
- Ctrl+Delete, axCalc: Clear the field.
- Ctrl+Shift+Delete, axCalc and axClip: Clear history.
- Ctrl+Shift+Delete, axTerminal: Clear transcript.

### Ctrl+Tab and Ctrl+Shift+Tab: move between panes

Ctrl+Tab goes to the next part of the window and Ctrl+Shift+Tab to the one before.

The keys:

- Ctrl+Tab, axAutomate, axMCP, axPass, axTask and axTemplate: Switch between the navigator and the detail pane.
- Ctrl+Tab, axCalc, axClaude and axTerminal: Switch between the view and the field.
- Ctrl+Tab, axMedia and axProcess: Back to the list.
- Ctrl+Tab, axDown and axJSON: Between the text and the field under it.
- Ctrl+Tab, axPreview: Between the text and the details.
- Ctrl+Tab, axFile: Switch between the list and the Favorites list.
- Ctrl+Tab, axGIT: Switch between the list and the text.
- Ctrl+Shift+Tab, axAutomate, axMCP, axPass, axTask and axTemplate: Switch between the navigator and the detail pane.
- Ctrl+Shift+Tab, axCalc, axClaude and axTerminal: Switch between the view and the field.
- Ctrl+Shift+Tab, axMedia and axProcess: Back to the list.
- Ctrl+Shift+Tab, axDown and axJSON: Between the text and the field under it.
- Ctrl+Shift+Tab, axPreview: Between the text and the details.
- Ctrl+Shift+Tab, axFile: Switch between the list and the Favorites list.
- Ctrl+Shift+Tab, axGIT: Switch between the list and the text.

### The arrows, Page Up, Page Down, Tab, Shift+Tab and Backspace: move through what you are on

The arrows and Page Up and Page Down move through a list or a text, as in every Windows program. In a list of levels, Right arrow goes in and Left arrow or Backspace comes back out; on a field with a choice of values, Left and Right arrow choose the value. Tab and Shift+Tab go to the next and the previous part of the window.

Windows' own keys, as in every program:

- Backspace, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Up a level.
- Backspace, axFile: Up one level.
- Down arrow, axClip, axStart and axTemplate: Next match.
- Left arrow, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Up a level.
- Left arrow, axAutomate, axMCP, axPass, axTask and axTemplate: The previous value of the field.
- Left arrow, axProcess: Up to This computer.
- Left arrow, axGIT: Up to the repository.
- Page Down, axClip, axStart and axTemplate: Page down.
- Page Up, axClip, axStart and axTemplate: Page up.
- Right arrow, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Into the list.
- Right arrow, axAutomate, axMCP, axPass, axTask and axTemplate: The next value of the field.
- Right arrow, axTask: Open it where it lives.
- Tab, axAutomate, axMCP, axPass, axTask and axTemplate: To the detail pane.
- Tab, axAutomate, axMCP, axPass, axTask and axTemplate: To the navigator.
- Tab, axCalc, axClaude and axTerminal: Switch between the view and the field.
- Tab, axFile and axGIT: To the list.
- Tab, axFile: To the Favorites list.
- Tab, axGIT: To the text.
- Up arrow, axClip, axStart and axTemplate: Previous match.
- Shift+Tab, axAutomate, axMCP, axPass, axTask and axTemplate: To the detail pane.
- Shift+Tab, axAutomate, axMCP, axPass, axTask and axTemplate: To the navigator.
- Shift+Tab, axCalc, axClaude and axTerminal: Switch between the view and the field.
- Shift+Tab, axFile and axGIT: To the list.
- Shift+Tab, axFile: To the Favorites list.
- Shift+Tab, axGIT: To the text.

### Shift with an arrow, Home, End or Space: select

Shift with an arrow, Home or End selects as it moves, as in every Windows list and text. Shift+Space selects from the nearest selected thing to here. The keys, their words and their code are one shared piece (SPEC AX-8.16).

The keys:

- Shift+Space, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Select to here.

Windows' own keys, as in every program:

- Shift+Down arrow, axFile and axPass: Select to the next entry.
- Shift+Down arrow, axGIT and axMedia: Select to the next file.
- Shift+Down arrow, axProcess and axTask: Select to the next item.
- Shift+Down arrow, axMCP: Select to the next server.
- Shift+Down arrow, axTemplate: Select to the next template.
- Shift+Down arrow, axAutomate: Select to the next workflow.
- Shift+End, axFile and axPass: Select to the last entry.
- Shift+End, axGIT and axMedia: Select to the last file.
- Shift+End, axProcess and axTask: Select to the last item.
- Shift+End, axMCP: Select to the last server.
- Shift+End, axTemplate: Select to the last template.
- Shift+End, axAutomate: Select to the last workflow.
- Shift+Home, axFile and axPass: Select to the first entry.
- Shift+Home, axGIT and axMedia: Select to the first file.
- Shift+Home, axProcess and axTask: Select to the first item.
- Shift+Home, axMCP: Select to the first server.
- Shift+Home, axTemplate: Select to the first template.
- Shift+Home, axAutomate: Select to the first workflow.
- Shift+Up arrow, axFile and axPass: Select to the previous entry.
- Shift+Up arrow, axGIT and axMedia: Select to the previous file.
- Shift+Up arrow, axProcess and axTask: Select to the previous item.
- Shift+Up arrow, axMCP: Select to the previous server.
- Shift+Up arrow, axTemplate: Select to the previous template.
- Shift+Up arrow, axAutomate: Select to the previous workflow.

### Ctrl+Plus and Ctrl+Minus: make the text larger or smaller

Ctrl+Plus makes the text larger and Ctrl+Minus smaller, in every window, as in a web browser.

Windows' own keys, as in every program:

- Ctrl+Minus, every app: Smaller text.
- Ctrl+Plus, every app: Larger text.

### Alt+letter: open a menu

Alt and a letter opens the menu that starts with that letter, as in every Windows program.

No key of this group is in use yet.

## Inside one app's window

These keys belong to the window you are in. The pattern says what kind of thing the key does; what exactly it does depends on the app.

### Ctrl+letter: act on what is selected

Ctrl and a letter does something to what you are on, and the letter is the first letter of the command where it can be: Ctrl+I interrupts, Ctrl+K bookmarks the line, Ctrl+R runs or fetches. The keys every Windows program has (copy, paste, save, find) are the same here, and Ctrl+N makes a new one of what the window holds: a document in the editors, a session in axClaude and axTerminal, a window in axFile, axMedia and axPreview. In a text, Ctrl with NVDA's browse-mode letter jumps while you type: Ctrl+H the next heading, Ctrl+K a link.

The keys:

- Ctrl+B, axDown: Bold.
- Ctrl+B, axTemplate: Open the browser.
- Ctrl+D, axFile: Add or remove this folder.
- Ctrl+D, axFile: Add to or remove from Favorites.
- Ctrl+D, axHelp: Put a picture file into the conversation.
- Ctrl+E, axPreview: Edit in axDown.
- Ctrl+E, axGIT: Open file in editor.
- Ctrl+E, axTask: Show in axFile.
- Ctrl+E, axProcess: Show program in axFile.
- Ctrl+H, axTask: Hide done tasks.
- Ctrl+I, axClaude and axTerminal: Interrupt.
- Ctrl+J, axTask: Edit the list's configuration.
- Ctrl+J, axTask: Open in axJSON.
- Ctrl+J, axAutomate: The workflow's file as text.
- Ctrl+K, axCalc, axClaude and axTerminal: Bookmark this line.
- Ctrl+L, axPass: Lock.
- Ctrl+L, axGIT: Pull.
- Ctrl+M, axTask: Move to a list.
- Ctrl+M, axTask: Move to section.
- Ctrl+P, axClaude: Next permission mode.
- Ctrl+P, axGIT: Push.
- Ctrl+Q, axFile: Add to axMedia's queue.
- Ctrl+R, axGIT: Fetch.
- Ctrl+R, axJSON: Format.
- Ctrl+R, axAutomate: Run.
- Ctrl+T, axFile: New text file.

Windows' own keys, as in every program:

- Ctrl+A, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Select all.
- Ctrl+A, axPass: Select all entries.
- Ctrl+A, axGIT: Select all files.
- Ctrl+A, axMCP: Select all of your servers.
- Ctrl+C, axFile: Copy.
- Ctrl+C, axProcess: Copy line.
- Ctrl+C, axPreview: Copy the detail's value.
- Ctrl+F, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Find in the list.
- Ctrl+F, axCalc, axClaude, axDown, axFile, axJSON, axPreview and axTerminal: Find.
- Ctrl+F, axGIT: Find in the text.
- Ctrl+N, axAutomate, axMCP, axPass, axTask and axTemplate: New item.
- Ctrl+N, axFile, axMedia and axPreview: New window.
- Ctrl+N, axDown and axJSON: New.
- Ctrl+N, axClaude and axTerminal: New session.
- Ctrl+N, axMCP: Add server.
- Ctrl+N, axGIT: New branch.
- Ctrl+N, axPass: New entry.
- Ctrl+N, axTemplate: New template.
- Ctrl+O, axDown, axJSON and axPreview: Open.
- Ctrl+O, axMedia: Add files.
- Ctrl+O, axAutomate: Open a folder of workflows.
- Ctrl+O, axPass: Open database.
- Ctrl+O, axStart: Open file location.
- Ctrl+O, axFile: Open folder.
- Ctrl+O, axGIT: Open repository.
- Ctrl+O, axHelp: Open the last screenshot in axPreview.
- Ctrl+S, axDown and axJSON: Save.
- Ctrl+S, axCalc: Save history.
- Ctrl+V, axFile: Paste.
- Ctrl+X, axFile: Cut.
- Ctrl+Y, axDown and axJSON: Redo.
- Ctrl+Z, axAutomate, axDown, axFile, axJSON, axMCP, axPass, axTask and axTemplate: Undo.

Exceptions today, which do another kind of thing:

- Ctrl+E, axJSON: Error.
- Ctrl+H, axDown and axPreview: Heading.
- Ctrl+I, axDown and axPreview: List item.
- Ctrl+I, axJSON: Item.
- Ctrl+J, axJSON: Object.
- Ctrl+K, axDown and axPreview: Link.
- Ctrl+K, axJSON: Key.
- Ctrl+L, axDown and axPreview: List.
- Ctrl+L, axJSON: Array.
- Ctrl+M, axClaude and axTerminal: Background music on or off.
- Ctrl+P, axClip: Pause recording.
- Ctrl+Q, axDown and axPreview: Block quote.
- Ctrl+T, axDown and axPreview: Table.

### Ctrl+digit: the window's own numbered action

Ctrl and a number is the window's own numbered action (SPEC AX-8.18 rule 3). In a text or a list with headings, Ctrl and a number from 1 to 6 goes to the next heading of that level; in axTask, Ctrl and a number from 1 to 9 runs that action key of the list.

The keys:

- Ctrl+1, axDown and axPreview: Heading level 1.
- Ctrl+1, axTask: Action key 1.
- Ctrl+1, axTask: Next heading level 1.
- Ctrl+2, axDown and axPreview: Heading level 2.
- Ctrl+2, axTask: Action key 2.
- Ctrl+2, axTask: Next heading level 2.
- Ctrl+3, axDown and axPreview: Heading level 3.
- Ctrl+3, axTask: Action key 3.
- Ctrl+3, axTask: Next heading level 3.
- Ctrl+4, axDown and axPreview: Heading level 4.
- Ctrl+4, axTask: Action key 4.
- Ctrl+5, axDown and axPreview: Heading level 5.
- Ctrl+5, axTask: Action key 5.
- Ctrl+6, axDown and axPreview: Heading level 6.
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

Alt and a number goes to one of the window's main places or views: the first, the second, the third, in axClaude, axTerminal, axGIT and axProcess; Alt+0 is All tasks in axTask. A place is never on Ctrl and a number.

The keys:

- Alt+0, axTask: All tasks.
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

- Ctrl+Shift+A, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Deselect all.
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
- Ctrl+Shift+N, axPass: New group.
- Ctrl+Shift+O, axMedia: Add folder.
- Ctrl+Shift+Q, axDown and axPreview: Block quote.
- Ctrl+Shift+S, axDown and axJSON: Save as.
- Ctrl+Shift+S, axGIT: Stage.
- Ctrl+Shift+T, axDown and axPreview: Table.
- Ctrl+Shift+U, axGIT: Unstage.
- Ctrl+Shift+V, axPreview: Read the clipboard's picture.
- Ctrl+Shift+Z, axDown and axJSON: Redo.

Exceptions today, which do another kind of thing:

- Ctrl+Shift+N, axFile: New folder.
- Ctrl+Shift+R, axClaude: Force Claude to restart.
- Ctrl+Shift+R, axTerminal: Restart shell.

### Enter with Ctrl, Shift or Alt: another form of open or confirm

Enter with a modifier is another way of opening or saying yes. Ctrl+Enter does the most frequent action where you are: it sends the message or the command, commits, saves a form or a text, marks a task done in axTask, pauses and resumes in axMedia, copies the password in axPass, ends a process in axProcess after its question, runs a workflow in axAutomate, opens the conversation in axHelp, or previews in axFile. Shift+Enter opens what you are on another way, such as a preview. Alt+Enter shows its properties and Ctrl+Shift+Enter starts a program as administrator, as everywhere in Windows; in axClaude it sends the queued messages now, another form of Ctrl+Enter.

The keys:

- Ctrl+Enter, axDown and axJSON: Save.
- Ctrl+Enter, axGIT: Commit.
- Ctrl+Enter, axPass: Copy password.
- Ctrl+Enter, axTask: Done or not done.
- Ctrl+Enter, axProcess: End process.
- Ctrl+Enter, axHelp: Open the conversation.
- Ctrl+Enter, axMedia: Play, pause or resume.
- Ctrl+Enter, axFile: Preview.
- Ctrl+Enter, axTemplate: Put in, with that program's selection.
- Ctrl+Enter, axAutomate: Run.
- Ctrl+Enter, axTerminal: Send the command.
- Ctrl+Enter, axClaude: Send the message.
- Shift+Enter, axClip: Copy without pasting.
- Shift+Enter, axTemplate: Copy without putting in.
- Shift+Enter, axFile: Preview.

Windows' own keys, as in every program:

- Ctrl+Shift+Enter, axClaude: Send queued messages now.
- Ctrl+Shift+Enter, axStart: Start as administrator.
- Alt+Enter, axFile: Properties.

### Ctrl with an arrow, Home or End, and Ctrl+Shift with Left or Right arrow: a bigger step

Ctrl with an arrow takes a bigger step: Ctrl+Up and Ctrl+Down go to the previous and the next section, Ctrl+Left and Ctrl+Right go back and forward 15 seconds in a recording. Ctrl+Home and Ctrl+End go to the first and the last. With Shift too, a bigger step still: the previous or the next file.

The keys:

- Ctrl+Down arrow, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: Next section.
- Ctrl+End, axClip, axStart and axTemplate: Last match.
- Ctrl+Home, axClip, axStart and axTemplate: First match.
- Ctrl+Left arrow, axMedia: Back 15 seconds.
- Ctrl+Right arrow, axMedia: Forward 15 seconds.
- Ctrl+Up arrow, axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask: Previous section.
- Ctrl+Shift+Left arrow, axMedia: Previous file.
- Ctrl+Shift+Right arrow, axMedia: Next file.

### Alt with an arrow: the choices of what you are on

Alt with an arrow is for the choices of what you are on, in the arrows' own directions (the maintainer's rule of 2026-11-21): Alt+Right goes into the list of them, where the arrows move and Enter chooses; Alt+Down and Alt+Up change to the next and the previous one in place; Alt+Left steps out to what is around it and offers what can be added there. axMedia's speed is Alt+Up and Alt+Down, axTerminal's recent commands Alt+Down in the command field. Ctrl with an arrow moves the cursor a bigger step, and moving an item of a list up or down is Ctrl+Shift+Up and Ctrl+Shift+Down, wherever a list has it.

The keys:

- Alt+Down arrow, axJSON: Next value.
- Alt+Down arrow, axTerminal: Recent commands.
- Alt+Down arrow, axMedia: Slower.
- Alt+Left arrow, axJSON: Add a member.
- Alt+Right arrow, axJSON: Choose a value.
- Alt+Up arrow, axMedia: Faster.
- Alt+Up arrow, axJSON: Previous value.

### A letter alone: in a view you read, a quick command: go to the next thing of a kind, or say something

Where you read and do not type (a text in navigation mode, a preview), a letter alone is a command: H goes to the next heading and Shift+H to the one before, L says the line. In a text's navigation mode, a letter that moves plays a high tick first, and a letter with no command plays a wooden bonk.

The keys:

- L, axPreview: Say the line.

## Keys in no group yet

These keys of the inventory match no group's pattern. Most are the arrows and the keys of a list or a text.

- Ctrl+Shift+Down arrow: Move down in the list.
- Ctrl+Shift+End: Move to the bottom of the list.
- Ctrl+Shift+Home: Move to the top of the list.
- Ctrl+Shift+Up arrow: Move up in the list.
- Shift+Alt+Left arrow: Heading shallower.
- Shift+Alt+Right arrow: Heading deeper.
