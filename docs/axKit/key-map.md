# Key map

Written by `tools/key-map.ps1` from `docs/axKit/window-commands.json`, `src/AxKit.Forms/BundleKeys.cs` and
`docs/axKit/key-families.json`; never edit it by hand. Every key of every window and what it does in each app.

The inventory holds 1023 commands with a key in 18 apps, on 144 different keys, counting the
second keys some commands answer. 68 keys have one meaning wherever they are; 76 have several.
21 of them are bundle keys, the ones of `BundleKeys`, which are the same in every window; the others
are window keys, each in its window's own table.

A key has one meaning when every command on it carries the same name in every app. The inventory lists the
commands of each app's window; the keys of a text box or a list itself (the arrows, typing) are not in it.

## Keys with one meaning everywhere

In this order: plain keys, F-keys, Ctrl, Ctrl+Shift, Alt, others.

- L. Window key, in axPreview alone: Say the line.
- Down arrow. Window key, in axClip, axStart and axTemplate: Next match.
- Page Down. Bundle key, in axClip, axStart and axTemplate: Page down.
- Page Up. Bundle key, in axClip, axStart and axTemplate: Page up.
- Up arrow. Window key, in axClip, axStart and axTemplate: Previous match.
- F1. Bundle key, in every app: Keyboard shortcuts.
- Ctrl+F1. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axFile.
- Ctrl+Shift+F1. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axTerminal, access layer for your shell.
- Shift+F1. Bundle key, in every app: Ask about this window.
- F2. Window key, in axAutomate, axFile, axMCP, axPass, axTask and axTemplate: Rename.
- Ctrl+F2. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axDown.
- Ctrl+Shift+F2. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axGIT, access layer for Git.
- Ctrl+F3. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axJSON.
- Ctrl+Shift+F3. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axPass, access layer for KeePassXC.
- F4. Bundle key, in every app: Send a report.
- Ctrl+F4. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axPreview.
- Ctrl+Shift+F4. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axClaude, access layer for Claude Code.
- Ctrl+F5. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axMedia.
- Ctrl+Shift+F5. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axMCP, access layer for MCP servers.
- Ctrl+F6. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axTask.
- Ctrl+Shift+F6. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axHelp, access layer for Claude Code.
- Ctrl+F7. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axStart.
- F8. Bundle key, in every app: Say the status.
- Ctrl+F8. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axClip.
- Ctrl+F9. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axCalc.
- Ctrl+F10. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axProcess.
- Ctrl+F11. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axAutomate.
- Ctrl+F12. Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask, axTemplate and axTerminal: axTemplate.
- Ctrl+0. Window key, in axTask alone: All tasks.
- Ctrl+7. Window key, in axTask alone: Action key 7.
- Ctrl+8. Window key, in axTask alone: Action key 8.
- Ctrl+9. Window key, in axTask alone: Action key 9.
- Ctrl+H. Window key, in axDown and axPreview: Heading.
- Ctrl+V. Window key, in axFile alone: Paste.
- Ctrl+X. Window key, in axFile alone: Cut.
- Ctrl+Y. Window key, in axDown and axJSON: Redo.
- Ctrl+End. Window key, in axClip, axStart and axTemplate: Last match.
- Ctrl+Home. Window key, in axClip, axStart and axTemplate: First match.
- Ctrl+Left arrow. Window key, in axMedia alone: Back 15 seconds.
- Ctrl+Minus. Bundle key, in every app: Smaller text.
- Ctrl+Plus. Bundle key, in every app: Larger text.
- Ctrl+Right arrow. Window key, in axMedia alone: Forward 15 seconds.
- Ctrl+Slash. Bundle key, in every app: Ask about this window.
- Ctrl+Shift+7. Window key, in axTask alone: Action key Shift+7.
- Ctrl+Shift+8. Window key, in axTask alone: Action key Shift+8.
- Ctrl+Shift+9. Window key, in axTask alone: Action key Shift+9.
- Ctrl+Shift+A. Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Deselect all.
- Ctrl+Shift+D. Window key, in axTask alone: Hide done tasks.
- Ctrl+Shift+E. Window key, in axJSON alone: Error.
- Ctrl+Shift+J. Window key, in axJSON alone: Object.
- Ctrl+Shift+M. Window key, in axClaude alone: Next permission mode.
- Ctrl+Shift+O. Window key, in axMedia alone: Open folder.
- Ctrl+Shift+P. Bundle key, in every app: Command palette.
- Ctrl+Shift+U. Window key, in axGIT alone: Unstage.
- Ctrl+Shift+V. Window key, in axPreview alone: Read the clipboard's picture.
- Ctrl+Shift+Z. Window key, in axDown and axJSON: Redo.
- Ctrl+Shift+Delete. Window key, in axCalc and axClip: Clear history.
- Ctrl+Shift+Enter. Window key, in axStart alone: Start as administrator.
- Ctrl+Shift+Left arrow. Window key, in axMedia alone: Previous file.
- Ctrl+Shift+Right arrow. Window key, in axMedia alone: Next file.
- Alt+D. Window key, in axFile alone: Go to folder.
- Alt+Down arrow. Window key, in axJSON alone: Next value.
- Alt+Enter. Window key, in axFile alone: Properties.
- Alt+Left arrow. Window key, in axJSON alone: Add a member.
- Shift+Alt+Left arrow. Window key, in axDown alone: Heading shallower.
- Alt+Right arrow. Window key, in axJSON alone: Choose a value.
- Shift+Alt+Right arrow. Window key, in axDown alone: Heading deeper.
- Shift+Space. Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Select to here.

## Keys whose meaning differs

The most spread first: the number of meanings, then the number of apps.

### Enter

Window key, in axAutomate, axCalc, axClip, axFile, axGIT, axHelp, axMCP, axMedia, axPass, axPreview, axProcess, axStart, axTask and axTemplate. 18 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Open.
- axAutomate, axMCP, axPass, axTask and axTemplate: Change the field.
- axCalc: Answer.
- axTask: Change the item in its form.
- axProcess: Details.
- axFile: Go to the action's place.
- axTask: Open it where it lives.
- axTask: Open its form.
- axHelp: Open the conversation.
- axPreview: Open the detail.
- axTask: Open the file in axJSON.
- axTask: Open the item's form.
- axClip: Paste.
- axTemplate: Put in.
- axMCP: Read the resource.
- axGIT: Show diff.
- axStart: Start.
- axPass: Unlock.

### Ctrl+N

Window key, in axAutomate, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axTask, axTemplate and axTerminal. 8 meanings.

- axAutomate, axMCP, axPass, axTask and axTemplate: New item.
- axFile, axMedia and axPreview: New window.
- axDown and axJSON: New.
- axClaude and axTerminal: New session.
- axMCP: Add server.
- axGIT: New branch.
- axPass: New entry.
- axTemplate: New template.

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

### Escape

Window key, in axAutomate, axCalc, axClip, axFile, axGIT, axHelp, axMCP, axMedia, axPass, axProcess, axStart, axTask and axTemplate. 7 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Clear the filter.
- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Deselect all.
- axAutomate, axMCP, axPass, axTask and axTemplate: To the navigator.
- axClip, axHelp, axStart and axTemplate: Hide.
- axFile and axGIT: To the list.
- axTask: Back from the search.
- axCalc: Clear the field.

### Ctrl+Q

Window key, in axAutomate, axCalc, axClaude, axClip, axDown, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal. 7 meanings.

- axAutomate, axClip, axMCP, axPass, axTask and axTemplate: Read in full.
- axDown and axPreview: Block quote.
- axClaude: Claude's question again.
- axCalc: Say the last answer.
- axProcess: Summary.
- axMedia: What is playing.
- axTerminal: What is running.

### Ctrl+Z

Window key, in axAutomate, axDown, axFile, axJSON, axMCP, axPass, axTask and axTemplate. 7 meanings.

- axAutomate, axDown, axFile, axJSON, axMCP, axPass, axTask and axTemplate: Undo.
- axFile: Undo compress.
- axFile: Undo delete.
- axFile: Undo move.
- axFile: Undo new folder.
- axFile: Undo rename.
- axFile: Undo the action.

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

### Ctrl+Enter

Bundle key, in axClaude, axFile, axGIT, axMedia, axTask, axTemplate and axTerminal. 7 meanings.

- axGIT: Commit.
- axTask: Done or not done.
- axFile: Play folder in axMedia.
- axMedia: Play, pause or resume.
- axTemplate: Put in, with that program's selection.
- axTerminal: Send the command.
- axClaude: Send the message.

### F6

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal. 6 meanings.

- axAutomate, axMCP, axPass, axTask and axTemplate: Switch between the navigator and the detail pane.
- axDown, axJSON and axPreview: Between the text and the details.
- axCalc, axClaude and axTerminal: Switch between the view and the field.
- axMedia and axProcess: Back to the list.
- axFile: Switch between the list and the Actions list.
- axGIT: Switch between the list and the text.

### Ctrl+Tab

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal. 6 meanings.

- axAutomate, axMCP, axPass, axTask and axTemplate: Switch between the navigator and the detail pane.
- axDown, axJSON and axPreview: Between the text and the details.
- axCalc, axClaude and axTerminal: Switch between the view and the field.
- axMedia and axProcess: Back to the list.
- axFile: Switch between the list and the Actions list.
- axGIT: Switch between the list and the text.

### Ctrl+Shift+Tab

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal. 6 meanings.

- axAutomate, axMCP, axPass, axTask and axTemplate: Switch between the navigator and the detail pane.
- axDown, axJSON and axPreview: Between the text and the details.
- axCalc, axClaude and axTerminal: Switch between the view and the field.
- axMedia and axProcess: Back to the list.
- axFile: Switch between the list and the Actions list.
- axGIT: Switch between the list and the text.

### Delete

Window key, in axAutomate, axClip, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 6 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Delete.
- axTask: Archive.
- axGIT: Discard changes.
- axProcess: End process.
- axClip: Remove.
- axPass: To the Recycle Bin.

### Tab

Window key, in axAutomate, axCalc, axClaude, axFile, axGIT, axMCP, axPass, axTask, axTemplate and axTerminal. 6 meanings.

- axAutomate, axMCP, axPass, axTask and axTemplate: To the detail pane.
- axAutomate, axMCP, axPass, axTask and axTemplate: To the navigator.
- axCalc, axClaude and axTerminal: Switch between the view and the field.
- axFile and axGIT: To the list.
- axFile: To the Actions list.
- axGIT: To the text.

### Shift+Tab

Window key, in axAutomate, axCalc, axClaude, axFile, axGIT, axMCP, axPass, axTask, axTemplate and axTerminal. 6 meanings.

- axAutomate, axMCP, axPass, axTask and axTemplate: To the detail pane.
- axAutomate, axMCP, axPass, axTask and axTemplate: To the navigator.
- axCalc, axClaude and axTerminal: Switch between the view and the field.
- axFile and axGIT: To the list.
- axFile: To the Actions list.
- axGIT: To the text.

### Shift+Down arrow

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 6 meanings.

- axFile and axPass: Select to the next entry.
- axGIT and axMedia: Select to the next file.
- axProcess and axTask: Select to the next item.
- axMCP: Select to the next server.
- axTemplate: Select to the next template.
- axAutomate: Select to the next workflow.

### Shift+End

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 6 meanings.

- axFile and axPass: Select to the last entry.
- axGIT and axMedia: Select to the last file.
- axProcess and axTask: Select to the last item.
- axMCP: Select to the last server.
- axTemplate: Select to the last template.
- axAutomate: Select to the last workflow.

### Shift+Home

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 6 meanings.

- axFile and axPass: Select to the first entry.
- axGIT and axMedia: Select to the first file.
- axProcess and axTask: Select to the first item.
- axMCP: Select to the first server.
- axTemplate: Select to the first template.
- axAutomate: Select to the first workflow.

### Shift+Up arrow

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 6 meanings.

- axFile and axPass: Select to the previous entry.
- axGIT and axMedia: Select to the previous file.
- axProcess and axTask: Select to the previous item.
- axMCP: Select to the previous server.
- axTemplate: Select to the previous template.
- axAutomate: Select to the previous workflow.

### Ctrl+E

Window key, in axFile, axGIT, axJSON, axPreview, axProcess and axTask. 6 meanings.

- axPreview: Edit in axDown.
- axFile: Editor.
- axJSON: Error.
- axGIT: Open file in editor.
- axTask: Show in axFile.
- axProcess: Show program in axFile.

### Ctrl+I

Window key, in axCalc, axDown, axFile, axJSON, axPreview and axStart. 5 meanings.

- axFile and axStart: Information.
- axDown and axPreview: List item.
- axCalc: Answer in other bases.
- axJSON: Item.
- axFile: The action's number and time.

### Ctrl+Shift+C

Window key, in axCalc, axClaude, axFile and axTerminal. 5 meanings.

- axCalc: Copy answer.
- axFile: Copy path.
- axFile: Copy paths.
- axClaude: Ctrl+C.
- axTerminal: Interrupt.

### F5

Window key, in axAutomate, axFile, axGIT, axMCP, axPass, axPreview, axProcess, axStart, axTask and axTemplate. 4 meanings.

- axFile, axGIT, axMCP, axPass, axProcess, axTask and axTemplate: Refresh.
- axStart: Refresh programs.
- axPreview: Reload.
- axAutomate: Run.

### Left arrow

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 4 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Up a level.
- axAutomate, axMCP, axPass, axTask and axTemplate: The previous value of the field.
- axProcess: Up to This computer.
- axGIT: Up to the repository.

### Ctrl+A

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 4 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Select all.
- axPass: Select all entries.
- axGIT: Select all files.
- axMCP: Select all of your servers.

### Ctrl+Shift+N

Window key, in axAutomate, axDown, axFile, axJSON, axMCP, axPass and axTask. 4 meanings.

- axAutomate, axMCP, axPass and axTask: New list.
- axDown and axJSON: New window.
- axFile: New folder.
- axPass: New group.

### Ctrl+L

Window key, in axDown, axJSON, axPass, axPreview and axTerminal. 4 meanings.

- axDown and axPreview: List.
- axJSON: Array.
- axTerminal: Clear transcript.
- axPass: Lock.

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

### Ctrl+J

Window key, in axAutomate, axJSON and axTask. 4 meanings.

- axTask: Edit the list's configuration.
- axJSON: Object.
- axTask: Open in axJSON.
- axAutomate: The workflow's file as text.

### Ctrl+F

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal. 3 meanings.

- axAutomate, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Find in the list.
- axCalc, axClaude, axDown, axFile, axJSON, axPreview and axTerminal: Find.
- axGIT: Find in the text.

### Applications key

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 3 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: The Context menu.
- axAutomate, axMCP, axPass, axTask and axTemplate: The field's menu.
- axFile: The line's menu.

### Right arrow

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 3 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Into the list.
- axAutomate, axMCP, axPass, axTask and axTemplate: The next value of the field.
- axTask: Open it where it lives.

### Space

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 3 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Select or deselect.
- axAutomate, axMCP, axPass, axTask and axTemplate: Check the field.
- axFile: Select or deselect this entry.

### Shift+F10

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 3 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: The Context menu.
- axAutomate, axMCP, axPass, axTask and axTemplate: The field's menu.
- axFile: The line's menu.

### Ctrl+Period

Bundle key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 3 meanings.

- axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: The Context menu.
- axAutomate, axMCP, axPass, axTask and axTemplate: The field's menu.
- axFile: The line's menu.

### Ctrl+Shift+K

Window key, in axCalc, axClaude, axDown, axJSON, axPreview and axTerminal. 3 meanings.

- axCalc, axClaude and axTerminal: Bookmark this line.
- axDown and axPreview: Link.
- axJSON: Key.

### Ctrl+Shift+L

Window key, in axDown, axGIT, axJSON and axPreview. 3 meanings.

- axDown and axPreview: List.
- axJSON: Array.
- axGIT: Pull.

### Ctrl+Shift+S

Window key, in axClaude, axDown, axGIT and axJSON. 3 meanings.

- axDown and axJSON: Save as.
- axClaude: Send queued messages now.
- axGIT: Stage.

### Ctrl+B

Window key, in axClaude, axDown and axTemplate. 3 meanings.

- axDown: Bold.
- axTemplate: Open the browser.
- axClaude: What runs in the background.

### Ctrl+C

Window key, in axFile, axPreview and axProcess. 3 meanings.

- axFile: Copy.
- axProcess: Copy line.
- axPreview: Copy the detail's value.

### Ctrl+M

Window key, in axClaude, axTask and axTerminal. 3 meanings.

- axClaude and axTerminal: Background music on or off.
- axTask: Move to a list.
- axTask: Move to section.

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

### Ctrl+Shift+F

Window key, in axGIT, axJSON and axTask. 3 meanings.

- axGIT: Fetch.
- axTask: Find in every list.
- axJSON: Format.

### Shift+Enter

Window key, in axClip, axFile and axTemplate. 3 meanings.

- axClip: Copy without pasting.
- axTemplate: Copy without putting in.
- axFile: Preview.

### Ctrl+D

Window key, in axFile and axHelp. 3 meanings.

- axFile: Add or remove this folder.
- axFile: Add to or remove from Favorites.
- axHelp: Put a picture file into the conversation.

### F3

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal. 2 meanings.

- axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal: Find next.
- axGIT: Find next in the text.

### Shift+F3

Bundle key, in axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal. 2 meanings.

- axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask, axTemplate and axTerminal: Find previous.
- axGIT: Find previous in the text.

### Backspace

Window key, in axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate. 2 meanings.

- axAutomate, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate: Up a level.
- axFile: Up one level.

### Ctrl+W

Window key, in axClaude, axDown, axFile, axGIT, axJSON, axMedia, axPreview and axTerminal. 2 meanings.

- axClaude, axDown, axFile, axGIT, axJSON, axMedia, axPreview and axTerminal: File Explorer.
- axFile: Show the folder in File Explorer.

### Ctrl+Down arrow

Bundle key, in axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask. 2 meanings.

- axAutomate, axGIT, axMCP, axPass, axProcess and axTask: Next section.
- axMedia: Slower.

### Ctrl+Up arrow

Bundle key, in axAutomate, axGIT, axMCP, axMedia, axPass, axProcess and axTask. 2 meanings.

- axAutomate, axGIT, axMCP, axPass, axProcess and axTask: Previous section.
- axMedia: Faster.

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

### Ctrl+Shift+T

Window key, in axDown, axFile and axPreview. 2 meanings.

- axDown and axPreview: Table.
- axFile: New text file.

### Ctrl+P

Window key, in axClip and axTerminal. 2 meanings.

- axClip: Pause recording.
- axTerminal: Say current folder.

### Ctrl+R

Window key, in axAutomate and axTerminal. 2 meanings.

- axTerminal: Recent commands.
- axAutomate: The last run's report.

### Ctrl+Shift+R

Window key, in axClaude and axTerminal. 2 meanings.

- axClaude: Force Claude to restart.
- axTerminal: Restart shell.

### Alt+Up arrow

Window key, in axFile and axJSON. 2 meanings.

- axJSON: Previous value.
- axFile: Up one level.

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

### Space and Ctrl+Space: select, or toggle

1 key of the inventory. No misfits.

### Ctrl+letter: act on what is selected

25 keys of the inventory. 29 misfits.

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

22 keys of the inventory. 17 misfits.

- Ctrl+Shift+C, axTerminal: Interrupt. Its kind: sends Ctrl+C to the program.
- Ctrl+Shift+C, axClaude: Ctrl+C. Its kind: sends Ctrl+C to the program.
- Ctrl+Shift+D, axTask: Hide done tasks. Its kind: toggle or play.
- Ctrl+Shift+F, axJSON: Format. Its kind: act on what is selected.
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

### Ctrl+F-key: open a standalone app

12 keys of the inventory. No misfits.

### Ctrl+Shift+F-key: open an access layer

6 keys of the inventory. No misfits.

### F-keys (F1 to F12 alone): say or show

7 keys of the inventory. 14 misfits.

- F2, axAutomate, axFile, axMCP, axPass and axTask: Rename. Its kind: act on what is selected; Windows' own key for it.
- F3, axAutomate, axCalc, axClaude, axDown, axFile, axGIT, axJSON, axMCP, axMedia, axPass, axPreview, axProcess, axTask and axTerminal: Find next. Its kind: go to a place; a bundle key.
- F3, axGIT: Find next in the text. Its kind: go to a place.
- F4, every app: Send a report. Its kind: open or confirm; a bundle key.
- F5, axFile, axGIT, axMCP, axPass, axProcess and axTask: Refresh. Its kind: reads again; Windows' own key for it.
- F5, axStart: Refresh programs. Its kind: reads again.
- F5, axPreview: Reload. Its kind: reads again.
- F5, axAutomate: Run. Its kind: act on what is selected.
- F6, axAutomate, axMCP, axPass and axTask: Switch between the navigator and the detail pane. Its kind: move between panes; a bundle key.
- F6, axDown, axJSON and axPreview: Between the text and the details. Its kind: move between panes.
- F6, axCalc, axClaude and axTerminal: Switch between the view and the field. Its kind: move between panes.
- F6, axGIT: Switch between the list and the text. Its kind: move between panes.
- F6, axMedia: Back to the list. Its kind: move between panes.
- F6, axFile: Switch between the list and the Actions list. Its kind: move between panes.

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

- Escape, axFile: Clear the filter. Its kind: remove: it clears and stays where it is.
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

### Alt with an arrow: the choices of what you are on

4 keys of the inventory. 1 misfit.

- Alt+Up arrow, axFile: Up one level. Its kind: go to a place; File Explorer's own key for it.

### Ctrl+Plus and Ctrl+Minus: make the text larger or smaller

2 keys of the inventory. No misfits.

Windows' own keys, not counted: Ctrl+Plus and Ctrl+Minus.

### A letter alone: in a view you read, a quick command: go to the next thing of a kind, or say something

1 key of the inventory. No misfits.

### Alt+letter: open a menu

1 key of the inventory. 1 misfit.

- Alt+D, axFile: Go to folder. Its kind: go to a place; File Explorer's own key for its address bar.
