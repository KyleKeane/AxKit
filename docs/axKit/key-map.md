# Key map

Written by `tools/key-map.ps1` from `docs/axKit/window-commands.json`, `src/AxKit.Forms/BundleKeys.cs` and
`docs/axKit/key-families.json`; never edit it by hand. Every key of every window and what it does in each app.

The inventory holds 588 commands with a key in 17 apps, on 122 different keys, counting the
second keys some commands answer. 50 keys have one meaning wherever they are; 72 have several.
21 of them are bundle keys, the ones of `BundleKeys`, which are the same in every window; the others
are window keys, each in its window's own table.

A key has one meaning when every command on it carries the same name in every app. The inventory lists the
commands of each app's window; the keys of a text box or a list itself (the arrows, typing) are not in it.

## Keys with one meaning everywhere

In this order: plain keys, F-keys, Ctrl, Ctrl+Shift, Alt, others.

- L. Window key, in axPreview alone: Say the line.
- Down arrow. Window key, in axClip and axStart: Next match.
- Page Down. Bundle key, in axClip and axStart: Page down.
- Page Up. Bundle key, in axClip and axStart: Page up.
- Up arrow. Window key, in axClip and axStart: Previous match.
- F1. Bundle key, in every app: Keyboard shortcuts.
- Shift+F1. Bundle key, in every app: Ask about this window.
- F4. Bundle key, in every app: Send a report.
- F8. Bundle key, in every app: Say the status.
- Ctrl+0. Window key, in axTask alone: All tasks.
- Ctrl+7. Window key, in axTask alone: Action key 7.
- Ctrl+8. Window key, in axTask alone: Action key 8.
- Ctrl+9. Window key, in axTask alone: Action key 9.
- Ctrl+A. Window key, in axFile alone: Select all.
- Ctrl+B. Window key, in axClaude alone: What runs in the background.
- Ctrl+H. Window key, in axDown and axPreview: Heading.
- Ctrl+V. Window key, in axFile alone: Paste.
- Ctrl+X. Window key, in axFile alone: Cut.
- Ctrl+Y. Window key, in axDown and axJSON: Redo.
- Ctrl+End. Window key, in axClip and axStart: Last match.
- Ctrl+Home. Window key, in axClip and axStart: First match.
- Ctrl+Left arrow. Window key, in axMedia alone: Back 15 seconds.
- Ctrl+Minus. Bundle key, in every app: Smaller text.
- Ctrl+Plus. Bundle key, in every app: Larger text.
- Ctrl+Right arrow. Window key, in axMedia alone: Forward 15 seconds.
- Ctrl+Slash. Bundle key, in every app: Ask about this window.
- Ctrl+Shift+7. Window key, in axTask alone: Action key Shift+7.
- Ctrl+Shift+8. Window key, in axTask alone: Action key Shift+8.
- Ctrl+Shift+9. Window key, in axTask alone: Action key Shift+9.
- Ctrl+Shift+D. Window key, in axTask alone: Hide done tasks.
- Ctrl+Shift+E. Window key, in axJSON alone: Error.
- Ctrl+Shift+J. Window key, in axJSON alone: Object.
- Ctrl+Shift+M. Window key, in axClaude alone: Next permission mode.
- Ctrl+Shift+O. Window key, in axMedia alone: Open folder.
- Ctrl+Shift+P. Bundle key, in every app: Command palette.
- Ctrl+Shift+U. Window key, in axGIT alone: Unstage all.
- Ctrl+Shift+V. Window key, in axPreview alone: Read the clipboard's picture.
- Ctrl+Shift+Z. Window key, in axDown and axJSON: Redo.
- Ctrl+Shift+Delete. Window key, in axCalc and axClip: Clear history.
- Ctrl+Shift+Enter. Window key, in axStart alone: Start as administrator.
- Ctrl+Shift+Left arrow. Window key, in axMedia alone: Previous file.
- Ctrl+Shift+Right arrow. Window key, in axMedia alone: Next file.
- Alt+D. Window key, in axFile alone: Go to folder.
- Alt+Down arrow. Window key, in axAutomate, axMCP, axPass and axTask: Move the field down.
- Alt+Enter. Window key, in axFile alone: Properties.
- Shift+Down arrow. Window key, in axFile alone: Select to the next entry.
- Shift+End. Window key, in axFile alone: Select to the last entry.
- Shift+Home. Window key, in axFile alone: Select to the first entry.
- Shift+Space. Window key, in axFile alone: Select to here.
- Shift+Up arrow. Window key, in axFile alone: Select to the previous entry.

## Keys whose meaning differs

The most spread first: the number of meanings, then the number of apps.

### Enter

Window key, in axAutomate, axCalc, axClip, axFile, axGIT, axHelp, axMCP, axPass, axPreview, axProcess, axStart and axTask. 18 meanings.

- axAutomate, axFile, axMCP, axPass and axTask: Open.
- axAutomate, axMCP, axPass and axTask: Change the field.
- axCalc: Answer.
- axTask: Change the item in its form.
- axProcess: Details.
- axFile: Go to the action's place.
- axTask: Open it where it lives.
- axHelp: Open the conversation.
- axPreview: Open the detail.
- axTask: Open the file in axJSON.
- axTask: Open the item's form.
- axClip: Paste.
- axMCP: Read the resource.
- axCalc: Recall the entry.
- axGIT: Show diff.
- axGIT: Show the commit.
- axStart: Start.
- axPass: Unlock.

### Ctrl+N

Window key, in axAutomate, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axTask and axTerminal. 8 meanings.

- axAutomate, axMCP, axPass and axTask: New field.
- axAutomate, axMCP, axPass and axTask: New item.
- axFile, axMedia and axPreview: New window.
- axDown and axJSON: New.
- axClaude and axTerminal: New session.
- axMCP: Add server.
- axGIT: New branch.
- axPass: New entry.

### Ctrl+O

Window key, in axAutomate, axDown, axFile, axGIT, axHelp, axJSON, axMedia, axPass, axPreview and axStart. 8 meanings.

- axDown, axJSON and axPreview: Open.
- axAutomate: Open a folder of workflows.
- axPass: Open database.
- axStart: Open file location.
- axMedia: Open files.
- axFile: Open folder.
- axGIT: Open repository.
- axHelp: Open the last screenshot in axPreview.

### F6

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axTask and axTerminal. 7 meanings.

- axAutomate, axMCP, axPass and axTask: Switch between the navigator and the detail pane.
- axDown, axJSON and axPreview: Between the text and the details.
- axClaude and axTerminal: Switch between the view and the field.
- axMedia: Back to the list.
- axFile: Switch between the folder list and the Actions list.
- axCalc: Switch between the history and the field.
- axGIT: Switch between the list and the details.

### Ctrl+Tab

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axTask and axTerminal. 7 meanings.

- axAutomate, axMCP, axPass and axTask: Switch between the navigator and the detail pane.
- axDown, axJSON and axPreview: Between the text and the details.
- axClaude and axTerminal: Switch between the view and the field.
- axMedia: Back to the list.
- axFile: Switch between the folder list and the Actions list.
- axCalc: Switch between the history and the field.
- axGIT: Switch between the list and the details.

### Ctrl+Shift+Tab

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axTask and axTerminal. 7 meanings.

- axAutomate, axMCP, axPass and axTask: Switch between the navigator and the detail pane.
- axDown, axJSON and axPreview: Between the text and the details.
- axClaude and axTerminal: Switch between the view and the field.
- axMedia: Back to the list.
- axFile: Switch between the folder list and the Actions list.
- axCalc: Switch between the history and the field.
- axGIT: Switch between the list and the details.

### Ctrl+Q

Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal. 7 meanings.

- axAutomate, axClip, axMCP, axPass and axTask: Read in full.
- axDown and axPreview: Block quote.
- axClaude: Claude's question again.
- axCalc: Say the last answer.
- axProcess: Summary.
- axMedia: What is playing.
- axTerminal: What is running.

### Delete

Window key, in axAutomate, axCalc, axClip, axFile, axGIT, axMCP, axPass, axProcess and axTask. 7 meanings.

- axAutomate, axFile, axMCP, axPass and axTask: Delete.
- axAutomate, axMCP, axPass and axTask: Remove the field.
- axGIT: Discard changes.
- axProcess: End process.
- axClip: Remove.
- axCalc: Remove the entry.
- axPass: To the Recycle Bin.

### Escape

Window key, in axAutomate, axCalc, axClip, axFile, axHelp, axMCP, axPass, axStart and axTask. 7 meanings.

- axAutomate, axMCP, axPass and axTask: To the navigator.
- axClip, axHelp and axStart: Hide.
- axTask: Back from the search.
- axCalc: Back to the field.
- axFile: Back to the folder list.
- axCalc: Clear the field.
- axFile: Clear the selection or the filter.

### Ctrl+1

Window key, in axClaude, axDown, axGIT, axPreview, axProcess, axTask and axTerminal. 7 meanings.

- axDown and axPreview: Heading level 1.
- axTask: Action key 1.
- axGIT: Changes.
- axTerminal: Command field.
- axClaude: Message field.
- axTask: Next heading level 1.
- axProcess: Processes.

### Ctrl+2

Window key, in axClaude, axDown, axGIT, axPreview, axProcess, axTask and axTerminal. 7 meanings.

- axDown and axPreview: Heading level 2.
- axTask: Action key 2.
- axClaude: Conversation.
- axGIT: Log.
- axTask: Next heading level 2.
- axProcess: Services.
- axTerminal: Terminal.

### Ctrl+3

Window key, in axClaude, axDown, axGIT, axPreview, axProcess, axTask and axTerminal. 7 meanings.

- axDown and axPreview: Heading level 3.
- axTask: Action key 3.
- axGIT: Branches.
- axTerminal: Latest output.
- axClaude: Latest response.
- axTask: Next heading level 3.
- axProcess: Startup.

### Ctrl+Z

Window key, in axAutomate, axDown, axFile, axJSON, axMCP, axPass and axTask. 7 meanings.

- axAutomate, axDown, axFile, axJSON, axMCP, axPass and axTask: Undo.
- axFile: Undo compress.
- axFile: Undo delete.
- axFile: Undo move.
- axFile: Undo new folder.
- axFile: Undo rename.
- axFile: Undo the action.

### Ctrl+F

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal. 5 meanings.

- axClaude, axDown, axFile, axJSON, axPreview and axTerminal: Find.
- axAutomate, axGIT, axMCP, axPass, axProcess and axTask: Find in the list.
- axGIT: Find in the details.
- axCalc: Find in the history.
- axMedia: Find in the queue.

### Tab

Window key, in axAutomate, axClaude, axFile, axMCP, axPass, axProcess, axTask and axTerminal. 5 meanings.

- axAutomate, axMCP, axPass and axTask: To the detail pane.
- axAutomate, axMCP, axPass and axTask: To the navigator.
- axClaude and axTerminal: Switch between the view and the field.
- axProcess: Next view.
- axFile: Switch between the folder list and the Actions list.

### Shift+Tab

Window key, in axAutomate, axClaude, axFile, axMCP, axPass, axProcess, axTask and axTerminal. 5 meanings.

- axAutomate, axMCP, axPass and axTask: To the detail pane.
- axAutomate, axMCP, axPass and axTask: To the navigator.
- axClaude and axTerminal: Switch between the view and the field.
- axProcess: Previous view.
- axFile: Switch between the folder list and the Actions list.

### Space

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass and axTask. 5 meanings.

- axAutomate, axMCP, axPass and axTask: Check.
- axAutomate, axMCP, axPass and axTask: Check the field.
- axMedia: Pause or resume.
- axFile: Select or deselect this entry.
- axGIT: Stage or unstage.

### Ctrl+I

Window key, in axCalc, axDown, axFile, axJSON, axPreview and axStart. 5 meanings.

- axFile and axStart: Information.
- axDown and axPreview: List item.
- axCalc: Answer in other bases.
- axJSON: Item.
- axFile: The action's number and time.

### Ctrl+E

Window key, in axFile, axGIT, axJSON, axPreview and axProcess. 5 meanings.

- axPreview: Edit in axDown.
- axFile: Editor.
- axJSON: Error.
- axGIT: Open file in editor.
- axProcess: Show program in axFile.

### Ctrl+Shift+C

Window key, in axCalc, axClaude, axFile and axTerminal. 5 meanings.

- axCalc: Copy answer.
- axFile: Copy path.
- axFile: Copy paths.
- axClaude: Ctrl+C.
- axTerminal: Interrupt.

### F5

Window key, in axAutomate, axFile, axGIT, axMCP, axPass, axPreview, axProcess, axStart and axTask. 4 meanings.

- axAutomate, axFile, axGIT, axMCP, axPass, axProcess and axTask: Refresh.
- axStart: Refresh programs.
- axPreview: Reload.
- axAutomate: Run.

### Ctrl+Shift+N

Window key, in axAutomate, axDown, axFile, axJSON, axMCP, axPass and axTask. 4 meanings.

- axAutomate, axMCP, axPass and axTask: New list.
- axDown and axJSON: New window.
- axFile: New folder.
- axPass: New group.

### Right arrow

Window key, in axAutomate, axFile, axMCP, axPass and axTask. 4 meanings.

- axAutomate, axMCP, axPass and axTask: Into the list.
- axAutomate, axMCP, axPass and axTask: The next value of the field.
- axFile: Into the folder.
- axTask: Open it where it lives.

### Ctrl+L

Window key, in axDown, axJSON, axPass, axPreview and axTerminal. 4 meanings.

- axDown and axPreview: List.
- axJSON: Array.
- axTerminal: Clear transcript.
- axPass: Lock.

### Ctrl+Enter

Bundle key, in axClaude, axFile, axGIT and axTerminal. 4 meanings.

- axGIT: Commit.
- axFile: Play folder in axMedia.
- axTerminal: Send the command.
- axClaude: Send the message.

### Ctrl+Shift+F

Window key, in axFile, axGIT, axJSON and axTask. 4 meanings.

- axGIT: Fetch.
- axFile: Filter.
- axTask: Find in every list.
- axJSON: Format.

### Alt+1

Window key, in axClaude, axGIT, axProcess and axTerminal. 4 meanings.

- axGIT: Changes.
- axTerminal: Command field.
- axClaude: Message field.
- axProcess: Processes.

### Alt+2

Window key, in axClaude, axGIT, axProcess and axTerminal. 4 meanings.

- axClaude: Conversation.
- axGIT: Log.
- axProcess: Services.
- axTerminal: Terminal.

### Alt+3

Window key, in axClaude, axGIT, axProcess and axTerminal. 4 meanings.

- axGIT: Branches.
- axTerminal: Latest output.
- axClaude: Latest response.
- axProcess: Startup.

### Applications key

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess and axTask. 3 meanings.

- axFile, axGIT, axMedia and axProcess: Context menu.
- axAutomate, axMCP, axPass and axTask: The Context menu.
- axAutomate, axMCP, axPass and axTask: The field's menu.

### Shift+F10

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess and axTask. 3 meanings.

- axFile, axGIT, axMedia and axProcess: Context menu.
- axAutomate, axMCP, axPass and axTask: The Context menu.
- axAutomate, axMCP, axPass and axTask: The field's menu.

### Ctrl+Period

Bundle key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess and axTask. 3 meanings.

- axFile, axGIT, axMedia and axProcess: Context menu.
- axAutomate, axMCP, axPass and axTask: The Context menu.
- axAutomate, axMCP, axPass and axTask: The field's menu.

### Left arrow

Window key, in axAutomate, axFile, axMCP, axPass and axTask. 3 meanings.

- axAutomate, axMCP, axPass and axTask: The previous value of the field.
- axAutomate, axMCP, axPass and axTask: Up a level.
- axFile: Up one level.

### Ctrl+Shift+K

Window key, in axClaude, axDown, axJSON, axPreview and axTerminal. 3 meanings.

- axClaude and axTerminal: Bookmark this line.
- axDown and axPreview: Link.
- axJSON: Key.

### Ctrl+Shift+L

Window key, in axDown, axGIT, axJSON and axPreview. 3 meanings.

- axDown and axPreview: List.
- axJSON: Array.
- axGIT: Pull.

### Ctrl+C

Window key, in axFile, axPreview and axProcess. 3 meanings.

- axFile: Copy.
- axProcess: Copy line.
- axPreview: Copy the detail's value.

### Ctrl+J

Window key, in axAutomate, axJSON and axTask. 3 meanings.

- axTask: Edit the list's configuration.
- axJSON: Object.
- axAutomate: The workflow's file as text.

### Ctrl+P

Window key, in axClip, axMedia and axTerminal. 3 meanings.

- axMedia: Pause or resume.
- axClip: Pause recording.
- axTerminal: Say current folder.

### Ctrl+Shift+1

Window key, in axDown, axPreview and axTask. 3 meanings.

- axDown and axPreview: Heading level 1.
- axTask: Action key Shift+1.
- axTask: Previous heading level 1.

### Ctrl+Shift+2

Window key, in axDown, axPreview and axTask. 3 meanings.

- axDown and axPreview: Heading level 2.
- axTask: Action key Shift+2.
- axTask: Previous heading level 2.

### Ctrl+Shift+3

Window key, in axDown, axPreview and axTask. 3 meanings.

- axDown and axPreview: Heading level 3.
- axTask: Action key Shift+3.
- axTask: Previous heading level 3.

### Ctrl+D

Window key, in axFile and axHelp. 3 meanings.

- axFile: Add or remove this folder.
- axFile: Add to or remove from Favorites.
- axHelp: Put a picture file into the conversation.

### F3

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal. 2 meanings.

- axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal: Find next.
- axGIT: Find next in the list.

### Shift+F3

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal. 2 meanings.

- axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal: Find previous.
- axGIT: Find previous in the list.

### Ctrl+W

Window key, in axClaude, axDown, axFile, axGIT, axJSON, axMedia, axPreview and axTerminal. 2 meanings.

- axClaude, axDown, axFile, axGIT, axJSON, axMedia, axPreview and axTerminal: File Explorer.
- axFile: Show the folder in File Explorer.

### Backspace

Window key, in axAutomate, axFile, axMCP, axPass and axTask. 2 meanings.

- axAutomate, axMCP, axPass and axTask: Up a level.
- axFile: Up one level.

### F2

Window key, in axAutomate, axFile, axMCP, axPass and axTask. 2 meanings.

- axAutomate, axFile, axMCP, axPass and axTask: Rename.
- axAutomate, axMCP, axPass and axTask: Rename the field.

### Ctrl+Down arrow

Bundle key, in axAutomate, axMCP, axMedia, axPass and axTask. 2 meanings.

- axAutomate, axMCP, axPass and axTask: Next section.
- axMedia: Slower.

### Ctrl+Up arrow

Bundle key, in axAutomate, axMCP, axMedia, axPass and axTask. 2 meanings.

- axAutomate, axMCP, axPass and axTask: Previous section.
- axMedia: Faster.

### Alt+Up arrow

Window key, in axAutomate, axFile, axMCP, axPass and axTask. 2 meanings.

- axAutomate, axMCP, axPass and axTask: Move the field up.
- axFile: Up one level.

### Ctrl+G

Window key, in axDown, axFile, axJSON and axPreview. 2 meanings.

- axDown, axJSON and axPreview: Go to line.
- axFile: Go to folder.

### Ctrl+Shift+W

Window key, in axDown, axFile, axJSON and axPreview. 2 meanings.

- axDown, axJSON and axPreview: Word wrap.
- axFile: Show in File Explorer.

### Ctrl+4

Window key, in axDown, axPreview and axTask. 2 meanings.

- axDown and axPreview: Heading level 4.
- axTask: Action key 4.

### Ctrl+5

Window key, in axDown, axPreview and axTask. 2 meanings.

- axDown and axPreview: Heading level 5.
- axTask: Action key 5.

### Ctrl+6

Window key, in axDown, axPreview and axTask. 2 meanings.

- axDown and axPreview: Heading level 6.
- axTask: Action key 6.

### Ctrl+K

Window key, in axDown, axJSON and axPreview. 2 meanings.

- axDown and axPreview: Link.
- axJSON: Key.

### Ctrl+M

Window key, in axClaude, axTask and axTerminal. 2 meanings.

- axClaude and axTerminal: Background music on or off.
- axTask: Move to section.

### Ctrl+S

Bundle key, in axCalc, axDown and axJSON. 2 meanings.

- axDown and axJSON: Save.
- axCalc: Save history.

### Ctrl+T

Window key, in axDown, axMedia and axPreview. 2 meanings.

- axDown and axPreview: Table.
- axMedia: Go to time.

### Ctrl+Shift+4

Window key, in axDown, axPreview and axTask. 2 meanings.

- axDown and axPreview: Heading level 4.
- axTask: Action key Shift+4.

### Ctrl+Shift+5

Window key, in axDown, axPreview and axTask. 2 meanings.

- axDown and axPreview: Heading level 5.
- axTask: Action key Shift+5.

### Ctrl+Shift+6

Window key, in axDown, axPreview and axTask. 2 meanings.

- axDown and axPreview: Heading level 6.
- axTask: Action key Shift+6.

### Ctrl+Shift+H

Window key, in axDown, axGIT and axPreview. 2 meanings.

- axDown and axPreview: Heading.
- axGIT: Push.

### Ctrl+Shift+I

Window key, in axDown, axJSON and axPreview. 2 meanings.

- axDown and axPreview: List item.
- axJSON: Item.

### Ctrl+Shift+Q

Window key, in axDown, axFile and axPreview. 2 meanings.

- axDown and axPreview: Block quote.
- axFile: Add to axMedia's queue.

### Ctrl+Shift+S

Window key, in axClaude, axDown and axJSON. 2 meanings.

- axDown and axJSON: Save as.
- axClaude: Send queued messages now.

### Ctrl+Shift+T

Window key, in axDown, axFile and axPreview. 2 meanings.

- axDown and axPreview: Table.
- axFile: New text file.

### Ctrl+R

Window key, in axAutomate and axTerminal. 2 meanings.

- axTerminal: Recent commands.
- axAutomate: The last run's report.

### Ctrl+Shift+A

Window key, in axFile and axGIT. 2 meanings.

- axFile: Deselect all.
- axGIT: Stage all.

### Ctrl+Shift+R

Window key, in axClaude and axTerminal. 2 meanings.

- axClaude: Force Claude to restart.
- axTerminal: Restart shell.

### Shift+Enter

Window key, in axClip and axFile. 2 meanings.

- axClip: Copy without pasting.
- axFile: Preview.

### Shift+Escape

Window key, in axClaude and axTerminal. 2 meanings.

- axClaude: Interrupt.
- axTerminal: Send Escape.

## Bundle keys with no command in the inventory

These are keys of `BundleKeys` that work inside a text or a control and are no command of a window's list.

- Ctrl+Space: `BundleKeys.ToggleMode`.
- Ctrl+Shift+Up arrow: `BundleKeys.SelectParagraphUp`.
- Ctrl+Shift+Down arrow: `BundleKeys.SelectParagraphDown`.

## Key families, proposed

From `docs/axKit/key-families.json`. A family is a pattern of keys and the one kind of thing such a key would do;
a misfit is a command on a key of the pattern that does another kind of thing today. The families are a draft
for the maintainer to settle: the first keys moved for them on 2026-11-16 (the places and views, to Alt+1, 2 and 3), and no test holds a key to its family.

### Ctrl+Shift+Space and Ctrl+Shift+punctuation: bring up axKit from anywhere in Windows

0 keys of the inventory. No misfits.

### Ctrl+punctuation: the in-window partner of the same key with Ctrl+Shift

2 keys of the inventory. No misfits.

### Space and Ctrl+Space: toggle or play

1 key of the inventory. No misfits.

### Ctrl+letter: act on what is selected

25 keys of the inventory. 30 misfits.

Windows' own keys, not counted: Ctrl+A, Ctrl+C, Ctrl+F, Ctrl+N, Ctrl+O, Ctrl+S, Ctrl+V, Ctrl+X, Ctrl+Y and Ctrl+Z.

- Ctrl+B, axClaude: What runs in the background. Its kind: say or show.
- Ctrl+E, axJSON: Error. Its kind: go to a place.
- Ctrl+G, axDown, axJSON and axPreview: Go to line. Its kind: go to a place.
- Ctrl+G, axFile: Go to folder. Its kind: go to a place.
- Ctrl+H, axDown and axPreview: Heading. Its kind: go to a place.
- Ctrl+I, axDown and axPreview: List item. Its kind: go to a place.
- Ctrl+I, axJSON: Item. Its kind: go to a place.
- Ctrl+I, axFile and axStart: Information. Its kind: say or show.
- Ctrl+I, axFile: The action's number and time. Its kind: say or show.
- Ctrl+I, axCalc: Answer in other bases. Its kind: say or show.
- Ctrl+J, axJSON: Object. Its kind: go to a place.
- Ctrl+K, axDown and axPreview: Link. Its kind: go to a place.
- Ctrl+K, axJSON: Key. Its kind: go to a place.
- Ctrl+L, axDown and axPreview: List. Its kind: go to a place.
- Ctrl+L, axJSON: Array. Its kind: go to a place.
- Ctrl+M, axClaude and axTerminal: Background music on or off. Its kind: toggle or play.
- Ctrl+P, axTerminal: Say current folder. Its kind: say or show.
- Ctrl+P, axMedia: Pause or resume. Its kind: toggle or play.
- Ctrl+P, axClip: Pause recording. Its kind: toggle or play.
- Ctrl+Q, axAutomate, axClip, axMCP, axPass and axTask: Read in full. Its kind: say or show.
- Ctrl+Q, axDown and axPreview: Block quote. Its kind: go to a place.
- Ctrl+Q, axProcess: Summary. Its kind: say or show.
- Ctrl+Q, axTerminal: What is running. Its kind: say or show.
- Ctrl+Q, axMedia: What is playing. Its kind: say or show.
- Ctrl+Q, axClaude: Claude's question again. Its kind: say or show.
- Ctrl+Q, axCalc: Say the last answer. Its kind: say or show.
- Ctrl+R, axTerminal: Recent commands. Its kind: say or show.
- Ctrl+R, axAutomate: The last run's report. Its kind: say or show.
- Ctrl+T, axDown and axPreview: Table. Its kind: go to a place.
- Ctrl+T, axMedia: Go to time. Its kind: go to a place.

### Ctrl+digit: go to the next heading of that level

10 keys of the inventory. 22 misfits.

- Ctrl+0, axTask: All tasks. Its kind: go to a place or view.
- Ctrl+1, axClaude: Message field. Its kind: go to a place or view; the older key, kept for now beside Alt+1 (2026-11-16).
- Ctrl+2, axClaude: Conversation. Its kind: go to a place or view; the older key, kept for now beside Alt+2 (2026-11-16).
- Ctrl+3, axClaude: Latest response. Its kind: go to a place or view; the older key, kept for now beside Alt+3 (2026-11-16).
- Ctrl+1, axTerminal: Command field. Its kind: go to a place or view; the older key, kept for now beside Alt+1 (2026-11-16).
- Ctrl+2, axTerminal: Terminal. Its kind: go to a place or view; the older key, kept for now beside Alt+2 (2026-11-16).
- Ctrl+3, axTerminal: Latest output. Its kind: go to a place or view; the older key, kept for now beside Alt+3 (2026-11-16).
- Ctrl+1, axGIT: Changes. Its kind: go to a place or view; the older key, kept for now beside Alt+1 (2026-11-16).
- Ctrl+2, axGIT: Log. Its kind: go to a place or view; the older key, kept for now beside Alt+2 (2026-11-16).
- Ctrl+3, axGIT: Branches. Its kind: go to a place or view; the older key, kept for now beside Alt+3 (2026-11-16).
- Ctrl+1, axProcess: Processes. Its kind: go to a place or view; the older key, kept for now beside Alt+1 (2026-11-16).
- Ctrl+2, axProcess: Services. Its kind: go to a place or view; the older key, kept for now beside Alt+2 (2026-11-16).
- Ctrl+3, axProcess: Startup. Its kind: go to a place or view; the older key, kept for now beside Alt+3 (2026-11-16).
- Ctrl+1, axTask: Action key 1. Its kind: act on what is selected.
- Ctrl+2, axTask: Action key 2. Its kind: act on what is selected.
- Ctrl+3, axTask: Action key 3. Its kind: act on what is selected.
- Ctrl+4, axTask: Action key 4. Its kind: act on what is selected.
- Ctrl+5, axTask: Action key 5. Its kind: act on what is selected.
- Ctrl+6, axTask: Action key 6. Its kind: act on what is selected.
- Ctrl+7, axTask: Action key 7. Its kind: act on what is selected.
- Ctrl+8, axTask: Action key 8. Its kind: act on what is selected.
- Ctrl+9, axTask: Action key 9. Its kind: act on what is selected.

### Ctrl+Shift+digit: go to the previous heading of that level

9 keys of the inventory. No misfits.

### Alt+digit: go to a place or view of the window

3 keys of the inventory. No misfits.

### Ctrl+Shift+letter: the reverse or the wider form of the same letter with Ctrl

22 keys of the inventory. 18 misfits.

- Ctrl+Shift+C, axTerminal: Interrupt. Its kind: sends Ctrl+C to the program.
- Ctrl+Shift+C, axClaude: Ctrl+C. Its kind: sends Ctrl+C to the program.
- Ctrl+Shift+D, axTask: Hide done tasks. Its kind: toggle or play.
- Ctrl+Shift+F, axJSON: Format. Its kind: act on what is selected.
- Ctrl+Shift+F, axFile: Filter. Its kind: narrows the list, where Ctrl+F finds in it.
- Ctrl+Shift+F, axGIT: Fetch. Its kind: acts on the repository.
- Ctrl+Shift+H, axGIT: Push. Its kind: acts on the repository.
- Ctrl+Shift+K, axClaude and axTerminal: Bookmark this line. Its kind: act on what is selected.
- Ctrl+Shift+L, axGIT: Pull. Its kind: acts on the repository.
- Ctrl+Shift+M, axClaude: Next permission mode. Its kind: toggle or play; Ctrl+M there is the background music.
- Ctrl+Shift+N, axFile: New folder. Its kind: makes a new thing; Ctrl+N there is New window.
- Ctrl+Shift+P, every app: Command palette. Its kind: say or show; a bundle key.
- Ctrl+Shift+Q, axFile: Add to axMedia's queue. Its kind: act on what is selected.
- Ctrl+Shift+R, axTerminal: Restart shell. Its kind: acts on the program; Ctrl+R there is Recent commands.
- Ctrl+Shift+R, axClaude: Force Claude to restart. Its kind: acts on the program.
- Ctrl+Shift+S, axClaude: Send queued messages now. Its kind: acts on the program; Ctrl+S there saves the conversation.
- Ctrl+Shift+T, axFile: New text file. Its kind: makes a new thing.
- Ctrl+Shift+W, axDown, axJSON and axPreview: Word wrap. Its kind: toggle or play.

### F-keys (F1 to F12 alone): say or show

7 keys of the inventory. 16 misfits.

- F2, axAutomate, axFile, axMCP, axPass and axTask: Rename. Its kind: act on what is selected; Windows' own key for it.
- F2, axAutomate, axMCP, axPass and axTask: Rename the field. Its kind: act on what is selected.
- F3, axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal: Find next. Its kind: go to a place; a bundle key.
- F3, axGIT: Find next in the list. Its kind: go to a place.
- F4, every app: Send a report. Its kind: open or confirm; a bundle key.
- F5, axAutomate, axFile, axGIT, axMCP, axPass, axProcess and axTask: Refresh. Its kind: reads again; Windows' own key for it.
- F5, axStart: Refresh programs. Its kind: reads again.
- F5, axPreview: Reload. Its kind: reads again.
- F5, axAutomate: Run. Its kind: act on what is selected.
- F6, axAutomate, axMCP, axPass and axTask: Switch between the navigator and the detail pane. Its kind: move between panes; a bundle key.
- F6, axDown, axJSON and axPreview: Between the text and the details. Its kind: move between panes.
- F6, axClaude and axTerminal: Switch between the view and the field. Its kind: move between panes.
- F6, axGIT: Switch between the list and the details. Its kind: move between panes.
- F6, axMedia: Back to the list. Its kind: move between panes.
- F6, axCalc: Switch between the history and the field. Its kind: move between panes.
- F6, axFile: Switch between the folder list and the Actions list. Its kind: move between panes.

### Shift+F-key, and the Applications key: the reverse or the other form of the same F-key

4 keys of the inventory. No misfits.

Windows' own keys, not counted: Shift+F10 and Applications key.

### Enter: open or confirm

1 key of the inventory. No misfits.

### Enter with Ctrl, Shift or Alt: another form of open or confirm

4 keys of the inventory. No misfits.

Windows' own keys, not counted: Alt+Enter and Ctrl+Shift+Enter.

### Escape and Shift+Escape: go back or close; with Shift, stop the program under the window

2 keys of the inventory. 2 misfits.

- Escape, axFile: Clear the selection or the filter. Its kind: remove: it clears and stays where it is.
- Escape, axCalc: Clear the field. Its kind: remove: it clears and stays where it is.

### Delete and Ctrl+Shift+Delete: remove

2 keys of the inventory. No misfits.

### Ctrl+Tab and Ctrl+Shift+Tab: move between panes

2 keys of the inventory. No misfits.

### The arrows, Page Up, Page Down, Tab, Shift+Tab and Backspace: move through what you are on

9 keys of the inventory. No misfits.

Windows' own keys, not counted: Up arrow, Down arrow, Left arrow, Right arrow, Page Up, Page Down, Tab, Shift+Tab and Backspace.

### Shift with an arrow, Home, End or Space: select

5 keys of the inventory. No misfits.

Windows' own keys, not counted: Shift+Up arrow, Shift+Down arrow, Shift+Home and Shift+End.

### Ctrl with an arrow, Home or End, and Ctrl+Shift with Left or Right arrow: a bigger step

8 keys of the inventory. 2 misfits.

- Ctrl+Up arrow, axMedia: Faster. Its kind: change a value: the speed of playing.
- Ctrl+Down arrow, axMedia: Slower. Its kind: change a value: the speed of playing.

### Alt+Up arrow and Alt+Down arrow: move what you are on up or down

2 keys of the inventory. 1 misfit.

- Alt+Up arrow, axFile: Up one level. Its kind: go to a place; File Explorer's own key for it.

### Ctrl+Plus and Ctrl+Minus: make the text larger or smaller

2 keys of the inventory. No misfits.

Windows' own keys, not counted: Ctrl+Plus and Ctrl+Minus.

### A letter alone: in a view you read, a quick command: go to the next thing of a kind, or say something

1 key of the inventory. No misfits.

### Alt+letter: open a menu

1 key of the inventory. 1 misfit.

- Alt+D, axFile: Go to folder. Its kind: go to a place; File Explorer's own key for its address bar.
