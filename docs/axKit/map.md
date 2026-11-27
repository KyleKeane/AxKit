# The map of the windows

Written by `tools/map.ps1` from `docs/axKit/workflow-graph.json`, which the inventory run writes from the windows
themselves (`tools/AxAutomate --inventory write`); never edit it by hand. The design is `docs/axKit/workflow-graph.md`.
Each window has a few places where the focus can be; a waypoint is a place you aim for. Under each place: how it is
reached in one action, and every key that does something there, with the command it runs, where you land and what
you hear. Moving inside a place (the arrows, Home, End, the lines of a text) is not on the map.

The map covers 7 of 18 apps: axAutomate, axCalc, axDown, axJSON, axMCP, axPreview, axTask.
Not on it yet: axClaude, axClip, axFile, axGIT, axHelp, axMedia, axPass, axProcess, axStart, axTemplate, axTerminal.

## From anywhere

Each app's own key opens its window on its first waypoint. It is free: no count of actions includes it.

- **Ctrl+F10**: Open axCalc, on the field.
- **Ctrl+F12**: Open axAutomate, on the list.
- **Ctrl+F5**: Open axDown, on the text.
- **Ctrl+F6**: Open axJSON, on the text.
- **Ctrl+F7**: Open axPreview, on the text.
- **Ctrl+F9**: Open axTask, on the list.
- **Ctrl+Shift+F10**: Open axMCP, on the list.

## axAutomate

On the navigator. It opens on the list.

### The list, a waypoint

Reached from anywhere: Ctrl+F12, free; from the fields of the row: Ctrl+Shift+Tab, Ctrl+Tab, Escape, F6, Shift+Tab, Tab; from a Windows dialog over the list: Escape; from a form over the list: Escape; from the command palette over the list: Escape; from a question over the list: Escape.

What can be done here:

- **Ctrl+A**, Select all: stays here; says "1 selected".
- **Ctrl+Comma**: to the command palette over the list.
- **Ctrl+Down**, Next section: stays here.
- **Ctrl+F**, Find in the list: to a question over the list.
- **Ctrl+J**, The workflow's file as text: stays here; says "No workflow selected".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+N**, New item: stays here; says "This version views and runs the workflows; changing them comes later".
- **Ctrl+O**, Open a folder of workflows: to a Windows dialog over the list.
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+S**: stays here; says "Nothing to save here: every change is written at once".
- **Ctrl+Shift+A**, Deselect all: stays here; says "None selected".
- **Ctrl+Shift+N**, New list: stays here; says "This version views and runs the workflows; changing them comes later".
- **Ctrl+Shift+Tab**: to the fields of the row.
- **Ctrl+Space**: stays here.
- **Ctrl+Tab**, Switch between the navigator and the detail pane: to the fields of the row.
- **Ctrl+Up**, Previous section: stays here.
- **Ctrl+Z**, Undo: stays here; says "Nothing to undo: nothing changes here".
- **Enter**, Open: stays here; says "axDown, 1 workflow, 0 folders, 02-palette, workflow, 6 steps".
- **Escape**: stays here.
- **F1**, Command palette: to the command palette over the list.
- **F2**, Rename: stays here; says "This version views and runs the workflows; changing them comes later".
- **F3**, Find next: to a question over the list.
- **F4**, Send a report: to a form over the list; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F5**, Refresh: stays here; says "Refreshed: flows, 1 workflow, 1 folder".
- **F6**: to the fields of the row.
- **F7**, Read in full: stays here; says "axDown, folder, 1 workflow. Folder: {temp}\flows\axDown. Workflows: 1, its folders' included. Right or Enter goes into it".
- **F8**, The last run's report: stays here; says "No workflow selected".
- **F9**, Say the status: stays here; says "flows, 1 workflow, 1 folder. {temp}\flows".
- **Left**, Up a level: stays here.
- **Right**, Into the list: stays here; says "axDown, 1 workflow, 0 folders, 02-palette, workflow, 6 steps".
- **Shift+Down**, Select to the next workflow: stays here.
- **Shift+End**, Select to the last workflow: stays here.
- **Shift+F3**, Find previous: to a question over the list.
- **Shift+Home**, Select to the first workflow: stays here.
- **Shift+Space**, Select to here: stays here.
- **Shift+Tab**: to the fields of the row.
- **Shift+Up**, Select to the previous workflow: stays here.
- **Space**, Select or deselect: stays here.
- **Tab**, To the detail pane: to the fields of the row.

### The fields of the row, a place

Reached from a Windows dialog over the fields of the row: Escape; from a form over the fields of the row: Escape; from the command palette over the fields of the row: Escape; from a question over the fields of the row: Escape; from the list: Ctrl+Shift+Tab, Ctrl+Tab, F6, Shift+Tab, Tab.

What can be done here:

- **Ctrl+Comma**: to the command palette over the fields of the row.
- **Ctrl+F**, Find in the list: to a question over the fields of the row.
- **Ctrl+J**, The workflow's file as text: stays here; says "No workflow selected".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+O**, Open a folder of workflows: to a Windows dialog over the fields of the row.
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+S**: stays here; says "Nothing to save here: every change is written at once".
- **Ctrl+Shift+N**, New list: stays here; says "This version views and runs the workflows; changing them comes later".
- **Ctrl+Shift+Tab**: to the list.
- **Ctrl+Space**: stays here.
- **Ctrl+Tab**, Switch between the navigator and the detail pane: to the list.
- **Ctrl+Z**, Undo: stays here; says "Nothing to undo: nothing changes here".
- **Enter**, Open: stays here; says "Folder: {temp}\flows\axDown".
- **Escape**, Deselect all: to the list.
- **F1**, Command palette: to the command palette over the fields of the row.
- **F3**, Find next: to a question over the fields of the row.
- **F4**, Send a report: to a form over the fields of the row; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F5**, Refresh: stays here; says "Refreshed: flows, 1 workflow, 1 folder".
- **F6**: to the list.
- **F7**, Read in full: stays here; says "Folder: {temp}\flows\axDown".
- **F8**, The last run's report: stays here; says "No workflow selected".
- **F9**, Say the status: stays here; says "flows, 1 workflow, 1 folder. {temp}\flows".
- **Left**, The previous value of the field: stays here.
- **Right**, The next value of the field: stays here.
- **Shift+F3**, Find previous: to a question over the fields of the row.
- **Shift+Tab**: to the list.
- **Space**, Select or deselect: stays here; says "Not a check: Enter changes the field".
- **Tab**, To the navigator: to the list.

### What stands over the places

- A Windows dialog over the fields of the row: Escape to the fields of the row.
- A form over the fields of the row: Escape to the fields of the row.
- The command palette over the fields of the row: Escape to the fields of the row.
- A question over the fields of the row: Escape to the fields of the row.
- A Windows dialog over the list: Escape to the list.
- A form over the list: Escape to the list.
- The command palette over the list: Escape to the list.
- A question over the list: Escape to the list.

## axCalc

On the conversation window. It opens on the field.

### The field, a waypoint

Reached from anywhere: Ctrl+F10, free; from a form over the field: Escape; from the command palette over the field: Escape; from a question over the field: Escape; from the conversation: Ctrl+Shift+Tab, Ctrl+Tab, Escape, F6, Shift+Tab, Tab.

What can be done here:

- **Ctrl+Comma**: to the command palette over the field.
- **Ctrl+Delete**, Clear the field: stays here; says "Empty".
- **Ctrl+Enter**: stays here.
- **Ctrl+F**, Find: to a question over the field.
- **Ctrl+K**, Bookmark this line: to the conversation; says "Empty".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+S**, Save history: stays here; says "History, empty".
- **Ctrl+Shift+Tab**: to the conversation.
- **Ctrl+Space**: stays here; says "Navigation mode".
- **Ctrl+Tab**: to the conversation.
- **Enter**, Answer: stays here; says "Type an expression".
- **Escape**, Back: in the field, nothing to go back to: stays here.
- **F1**, Command palette: to the command palette over the field.
- **F3**, Find next: to a question over the field.
- **F4**, Send a report: to a form over the field; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F6**: to the conversation.
- **F7**, Say the last answer: stays here; says "No answer yet".
- **F8**, Answer in other bases: stays here; says "No answer yet".
- **F9**, Say the status: stays here; says "No answer yet. Degrees, no variables".
- **Shift+F3**, Find previous: to a question over the field.
- **Shift+Tab**: to the conversation.
- **Tab**, Switch between the view and the field: to the conversation.

### The conversation, a place

Reached from the field: Ctrl+K, Ctrl+Shift+Tab, Ctrl+Tab, F6, Shift+Tab, Tab; from a form over the conversation: Escape; from the command palette over the conversation: Escape; from a question over the conversation: Escape.

What can be done here:

- **Ctrl+Comma**: to the command palette over the conversation.
- **Ctrl+Enter**: stays here.
- **Ctrl+F**, Find: to a question over the conversation.
- **Ctrl+K**, Bookmark this line: stays here; says "Empty".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+S**, Save history: stays here; says "History, empty".
- **Ctrl+Shift+Tab**: to the field.
- **Ctrl+Space**: stays here; says "Read only: there is no edit mode here".
- **Ctrl+Tab**: to the field.
- **Escape**: to the field.
- **F1**, Command palette: to the command palette over the conversation.
- **F3**, Find next: to a question over the conversation.
- **F4**, Send a report: to a form over the conversation; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F6**: to the field.
- **F7**, Say the last answer: stays here; says "No answer yet".
- **F8**, Answer in other bases: stays here; says "No answer yet".
- **F9**, Say the status: stays here; says "No answer yet. Degrees, no variables".
- **Shift+F3**, Find previous: to a question over the conversation.
- **Shift+Tab**: to the field.
- **Tab**, Switch between the view and the field: to the field.

### What stands over the places

- A form over the field: Escape to the field.
- The command palette over the field: Escape to the field.
- A question over the field: Escape to the field.
- A form over the conversation: Escape to the conversation.
- The command palette over the conversation: Escape to the conversation.
- A question over the conversation: Escape to the conversation.

## axDown

On the editor. It opens on the text.

### The text, a waypoint

Reached from anywhere: Ctrl+F5, free; from a choice in its place over the text: Escape; from a Windows dialog over the text: Escape; from a form over the text: Escape; from the command palette over the text: Escape; from a question over the text: Escape.

What can be done here:

- **Ctrl+1**, Heading level 1: stays here; says "No next heading level 1".
- **Ctrl+2**, Heading level 2: stays here; says "No next heading level 2".
- **Ctrl+3**, Heading level 3: stays here; says "No next heading level 3".
- **Ctrl+4**, Heading level 4: stays here; says "No next heading level 4".
- **Ctrl+5**, Heading level 5: stays here; says "No next heading level 5".
- **Ctrl+6**, Heading level 6: stays here; says "No next heading level 6".
- **Ctrl+B**, Bold: stays here; says "Bold on".
- **Ctrl+Comma**: to the command palette over the text.
- **Ctrl+Enter**: to a Windows dialog over the text.
- **Ctrl+F**, Find: to a question over the text.
- **Ctrl+H**, Heading: stays here; says "No next heading".
- **Ctrl+I**, List item: stays here; says "No next list item".
- **Ctrl+K**, Link: stays here; says "No next link".
- **Ctrl+L**, List: stays here; says "No next list".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+N**, New: stays here.
- **Ctrl+O**, Open: to a Windows dialog over the text.
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+Q**, Block quote: stays here; says "No next block quote".
- **Ctrl+S**, Save: to a Windows dialog over the text.
- **Ctrl+Shift+1**, Heading level 1: stays here; says "No previous heading level 1".
- **Ctrl+Shift+2**, Heading level 2: stays here; says "No previous heading level 2".
- **Ctrl+Shift+3**, Heading level 3: stays here; says "No previous heading level 3".
- **Ctrl+Shift+4**, Heading level 4: stays here; says "No previous heading level 4".
- **Ctrl+Shift+5**, Heading level 5: stays here; says "No previous heading level 5".
- **Ctrl+Shift+6**, Heading level 6: stays here; says "No previous heading level 6".
- **Ctrl+Shift+H**, Heading: stays here; says "No previous heading".
- **Ctrl+Shift+I**, List item: stays here; says "No previous list item".
- **Ctrl+Shift+K**, Link: stays here; says "No previous link".
- **Ctrl+Shift+L**, List: stays here; says "No previous list".
- **Ctrl+Shift+Q**, Block quote: stays here; says "No previous block quote".
- **Ctrl+Shift+S**, Save as: to a Windows dialog over the text.
- **Ctrl+Shift+T**, Table: stays here; says "No previous table".
- **Ctrl+Shift+Tab**: stays here.
- **Ctrl+Space**: stays here; says "Navigation mode".
- **Ctrl+T**, Table: stays here; says "No next table".
- **Ctrl+Tab**, Between the text and the field under it: stays here.
- **Ctrl+Y**, Redo: stays here; says "Nothing to redo".
- **Ctrl+Z**, Undo: stays here; says "Nothing to undo".
- **Escape**: stays here.
- **F1**, Command palette: to the command palette over the text.
- **F11**, Word wrap: stays here; says "Word wrap, not checked".
- **F12**, Go to line: to a choice in its place over the text.
- **F3**, Find next: to a question over the text.
- **F4**, Send a report: to a form over the text; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F6**: stays here.
- **F7**: stays here.
- **F9**, Say the status: stays here; says "Untitled. Line 1, Column 1 · UTF-8 · CRLF".
- **Shift+Alt+Left**, Heading shallower: stays here; says "No heading".
- **Shift+Alt+Right**, Heading deeper: stays here; says "Heading 1".
- **Shift+F3**, Find previous: to a question over the text.
- **Shift+Tab**: stays here.
- **Tab**: stays here.

### What stands over the places

- A choice in its place over the text: Escape to the text.
- A Windows dialog over the text: Escape to the text.
- A form over the text: Escape to the text.
- The command palette over the text: Escape to the text.
- A question over the text: Escape to the text.

## axJSON

On the editor. It opens on the text.

### The text, a waypoint

Reached from anywhere: Ctrl+F6, free; from a choice in its place over the text: Escape; from a Windows dialog over the text: Escape; from a form over the text: Escape; from the command palette over the text: Escape; from a question over the text: Escape.

What can be done here:

- **Alt+Down**, Next value: stays here; says "axJSON has no options for this file: it has them for a list's configuration and for an item of a list".
- **Alt+Left**, Add a member: stays here; says "axJSON has no options for this file: it has them for a list's configuration and for an item of a list".
- **Alt+Right**, Choose a value: stays here; says "axJSON has no options for this file: it has them for a list's configuration and for an item of a list".
- **Alt+Up**, Previous value: stays here; says "axJSON has no options for this file: it has them for a list's configuration and for an item of a list".
- **Ctrl+Comma**: to the command palette over the text.
- **Ctrl+E**, Error: stays here; says "No next error".
- **Ctrl+Enter**: to a Windows dialog over the text.
- **Ctrl+F**, Find: to a question over the text.
- **Ctrl+I**, Item: stays here; says "No next item".
- **Ctrl+J**, Object: stays here; says "No next object".
- **Ctrl+K**, Key: stays here; says "No next key".
- **Ctrl+L**, Array: stays here; says "No next array".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+N**, New: stays here.
- **Ctrl+O**, Open: to a Windows dialog over the text.
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+R**, Format: stays here; says "Nothing to format".
- **Ctrl+S**, Save: to a Windows dialog over the text.
- **Ctrl+Shift+E**, Error: stays here; says "No previous error".
- **Ctrl+Shift+I**, Item: stays here; says "No previous item".
- **Ctrl+Shift+J**, Object: stays here; says "No previous object".
- **Ctrl+Shift+K**, Key: stays here; says "No previous key".
- **Ctrl+Shift+L**, Array: stays here; says "No previous array".
- **Ctrl+Shift+S**, Save as: to a Windows dialog over the text.
- **Ctrl+Shift+Tab**: stays here.
- **Ctrl+Space**: stays here; says "Navigation mode".
- **Ctrl+Tab**, Between the text and the field under it: stays here.
- **Ctrl+Y**, Redo: stays here; says "Nothing to redo".
- **Ctrl+Z**, Undo: stays here; says "Nothing to undo".
- **Escape**: stays here.
- **F1**, Command palette: to the command palette over the text.
- **F11**, Word wrap: stays here; says "Word wrap, not checked".
- **F12**, Go to line: to a choice in its place over the text.
- **F3**, Find next: to a question over the text.
- **F4**, Send a report: to a form over the text; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F6**: stays here.
- **F7**: stays here.
- **F9**, Say the status: stays here; says "Untitled. Line 1, Column 1 · UTF-8 · CRLF".
- **Shift+F3**, Find previous: to a question over the text.
- **Shift+Tab**: stays here.
- **Tab**: stays here.

### What stands over the places

- A choice in its place over the text: Escape to the text.
- A Windows dialog over the text: Escape to the text.
- A form over the text: Escape to the text.
- The command palette over the text: Escape to the text.
- A question over the text: Escape to the text.

## axMCP

On the navigator. It opens on the list.

### The list, a waypoint

Reached from anywhere: Ctrl+Shift+F10, free; from the fields of the row: Ctrl+Shift+Tab, Ctrl+Tab, Escape, F6, Shift+Tab, Tab; from a form over the list: Escape; from the command palette over the list: Escape; from a question over the list: Escape.

What can be done here:

- **Ctrl+A**, Select all of your servers: stays here; says "1 selected".
- **Ctrl+Comma**: to the command palette over the list.
- **Ctrl+Down**, Next section: stays here.
- **Ctrl+Enter**: stays here; says "Select a tool first: Right into a server, then into Tools".
- **Ctrl+F**, Find in the list: to a question over the list.
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+N**, Add server: to a form over the list; says "Add server, form".
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+S**: stays here; says "Nothing to save here: every change is written at once".
- **Ctrl+Shift+A**, Deselect all: stays here; says "None selected".
- **Ctrl+Shift+N**, New list: stays here; says "Servers are added with Ctrl+N at the top level".
- **Ctrl+Shift+Tab**: to the fields of the row.
- **Ctrl+Space**: stays here.
- **Ctrl+Tab**, Switch between the navigator and the detail pane: to the fields of the row.
- **Ctrl+Up**, Previous section: stays here.
- **Ctrl+Z**, Undo: stays here; says "Nothing to undo".
- **Enter**, Open: stays here; says "A section: Down goes into it".
- **Escape**: stays here.
- **F1**, Command palette: to the command palette over the list.
- **F2**, Rename: stays here; says "Names here are the server's".
- **F3**, Find next: to a question over the list.
- **F4**, Send a report: to a form over the list; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F5**, Refresh: stays here; says "Refreshed: Servers, 2 servers".
- **F6**: to the fields of the row.
- **F7**, Read in full: stays here; says "Claude Code, section, 1 server. Claude Code, section, 1 server".
- **F9**, Say the status: stays here; says "Servers, 2 servers. {temp}".
- **Left**, Up a level: stays here.
- **Right**, Into the list: stays here; says "A section: Down goes into it".
- **Shift+Down**, Select to the next server: stays here.
- **Shift+End**, Select to the last server: stays here.
- **Shift+F3**, Find previous: to a question over the list.
- **Shift+Home**, Select to the first server: stays here.
- **Shift+Space**, Select to here: stays here.
- **Shift+Tab**: to the fields of the row.
- **Shift+Up**, Select to the previous server: stays here.
- **Space**, Select or deselect: stays here.
- **Tab**, To the detail pane: to the fields of the row.

### The fields of the row, a place

Reached from a form over the fields of the row: Escape; from the command palette over the fields of the row: Escape; from a question over the fields of the row: Escape; from the list: Ctrl+Shift+Tab, Ctrl+Tab, F6, Shift+Tab, Tab.

What can be done here:

- **Ctrl+Comma**: to the command palette over the fields of the row.
- **Ctrl+Enter**: stays here; says "Select a tool first: Right into a server, then into Tools".
- **Ctrl+F**, Find in the list: to a question over the fields of the row.
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+S**: stays here; says "Nothing to save here: every change is written at once".
- **Ctrl+Shift+N**, New list: stays here; says "Servers are added with Ctrl+N at the top level".
- **Ctrl+Shift+Tab**: to the list.
- **Ctrl+Space**: stays here.
- **Ctrl+Tab**, Switch between the navigator and the detail pane: to the list.
- **Ctrl+Z**, Undo: stays here; says "Nothing to undo".
- **Enter**, Change the field: stays here; says "Claude Code, section, 1 server".
- **Escape**, Deselect all: to the list.
- **F1**, Command palette: to the command palette over the fields of the row.
- **F3**, Find next: to a question over the fields of the row.
- **F4**, Send a report: to a form over the fields of the row; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F5**, Refresh: stays here; says "Refreshed: Servers, 2 servers".
- **F6**: to the list.
- **F7**, Read in full: stays here; says "Claude Code, section, 1 server".
- **F9**, Say the status: stays here; says "Servers, 2 servers. {temp}".
- **Left**, The previous value of the field: stays here.
- **Right**, The next value of the field: stays here.
- **Shift+F3**, Find previous: to a question over the fields of the row.
- **Shift+Tab**: to the list.
- **Space**, Check the field: stays here; says "Not a check: Enter changes the field".
- **Tab**, To the navigator: to the list.

### What stands over the places

- A form over the fields of the row: Escape to the fields of the row.
- The command palette over the fields of the row: Escape to the fields of the row.
- A question over the fields of the row: Escape to the fields of the row.
- A form over the list: Escape to the list.
- The command palette over the list: Escape to the list.
- A question over the list: Escape to the list.

## axPreview

On the editor. It opens on the text.

### The text, a waypoint

Reached from anywhere: Ctrl+F7, free; from a choice in its place over the details: Escape; from the details: Ctrl+Shift+Tab, Ctrl+Tab, F6, Shift+Tab, Tab; from a choice in its place over the text: Escape; from a Windows dialog over the text: Escape; from a form over the text: Escape; from the command palette over the text: Escape; from a question over the text: Escape.

What can be done here:

- **Ctrl+1**, Heading level 1: stays here; says "No next heading level 1".
- **Ctrl+2**, Heading level 2: stays here; says "Install, heading level 2".
- **Ctrl+3**, Heading level 3: stays here; says "No next heading level 3".
- **Ctrl+4**, Heading level 4: stays here; says "No next heading level 4".
- **Ctrl+5**, Heading level 5: stays here; says "No next heading level 5".
- **Ctrl+6**, Heading level 6: stays here; says "No next heading level 6".
- **Ctrl+Comma**: to the command palette over the text.
- **Ctrl+E**, Edit in axDown: stays here.
- **Ctrl+Enter**: stays here.
- **Ctrl+F**, Find: to a question over the text.
- **Ctrl+H**, Heading: stays here; says "Install, heading level 2".
- **Ctrl+I**, List item: stays here; says "first item of the list".
- **Ctrl+K**, Link: stays here; says "axKit on GitHub, link".
- **Ctrl+L**, List: stays here; says "List with 3 items, first item of the list".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+O**, Open: to a Windows dialog over the text.
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+Q**, Block quote: stays here; says "Block quote, A block quote, for Ctrl+Q.".
- **Ctrl+S**: stays here; says "Nothing to save here".
- **Ctrl+Shift+1**, Heading level 1: stays here; says "No previous heading level 1".
- **Ctrl+Shift+2**, Heading level 2: stays here; says "No previous heading level 2".
- **Ctrl+Shift+3**, Heading level 3: stays here; says "No previous heading level 3".
- **Ctrl+Shift+4**, Heading level 4: stays here; says "No previous heading level 4".
- **Ctrl+Shift+5**, Heading level 5: stays here; says "No previous heading level 5".
- **Ctrl+Shift+6**, Heading level 6: stays here; says "No previous heading level 6".
- **Ctrl+Shift+H**, Heading: stays here; says "No previous heading".
- **Ctrl+Shift+I**, List item: stays here; says "No previous list item".
- **Ctrl+Shift+K**, Link: stays here; says "No previous link".
- **Ctrl+Shift+L**, List: stays here; says "No previous list".
- **Ctrl+Shift+Q**, Block quote: stays here; says "No previous block quote".
- **Ctrl+Shift+T**, Table: stays here; says "No previous table".
- **Ctrl+Shift+Tab**: to the details.
- **Ctrl+Space**: stays here; says "Read only: there is no edit mode here".
- **Ctrl+T**, Table: stays here; says "Table with 3 rows and 2 columns".
- **Ctrl+Tab**, Between the text and the details: to the details.
- **Escape**: stays here.
- **F1**, Command palette: to the command palette over the text.
- **F11**, Word wrap: stays here; says "Word wrap, not checked".
- **F12**, Go to line: to a choice in its place over the text.
- **F3**, Find next: to a question over the text.
- **F4**, Send a report: to a form over the text; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F5**, Reload: stays here; says "Reloaded".
- **F6**: to the details.
- **F7**: stays here.
- **F9**, Say the status: stays here; says "README.md. Line 1, Column 1 · UTF-8 · CRLF".
- **L**, Say the line: stays here; says "Line 1 of 32".
- **Shift+F3**, Find previous: to a question over the text.
- **Shift+Tab**: to the details.
- **Tab**: to the details.

### The details, a place

Reached from a Windows dialog over the details: Escape; from a form over the details: Escape; from the command palette over the details: Escape; from a question over the details: Escape; from the text: Ctrl+Shift+Tab, Ctrl+Tab, F6, Shift+Tab, Tab.

What can be done here:

- **Ctrl+1**, Heading level 1: stays here; says "No next heading level 1".
- **Ctrl+2**, Heading level 2: stays here; says "Install, heading level 2".
- **Ctrl+3**, Heading level 3: stays here; says "No next heading level 3".
- **Ctrl+4**, Heading level 4: stays here; says "No next heading level 4".
- **Ctrl+5**, Heading level 5: stays here; says "No next heading level 5".
- **Ctrl+6**, Heading level 6: stays here; says "No next heading level 6".
- **Ctrl+C**, Copy the detail's value: stays here; says "Copied".
- **Ctrl+Comma**: to the command palette over the details.
- **Ctrl+E**, Edit in axDown: stays here.
- **Ctrl+Enter**: stays here.
- **Ctrl+F**, Find: to a question over the details.
- **Ctrl+H**, Heading: stays here; says "Install, heading level 2".
- **Ctrl+I**, List item: stays here; says "first item of the list".
- **Ctrl+K**, Link: stays here; says "axKit on GitHub, link".
- **Ctrl+L**, List: stays here; says "List with 3 items, first item of the list".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+O**, Open: to a Windows dialog over the details.
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+Q**, Block quote: stays here; says "Block quote, A block quote, for Ctrl+Q.".
- **Ctrl+S**: stays here; says "Nothing to save here".
- **Ctrl+Shift+1**, Heading level 1: stays here; says "No previous heading level 1".
- **Ctrl+Shift+2**, Heading level 2: stays here; says "No previous heading level 2".
- **Ctrl+Shift+3**, Heading level 3: stays here; says "No previous heading level 3".
- **Ctrl+Shift+4**, Heading level 4: stays here; says "No previous heading level 4".
- **Ctrl+Shift+5**, Heading level 5: stays here; says "No previous heading level 5".
- **Ctrl+Shift+6**, Heading level 6: stays here; says "No previous heading level 6".
- **Ctrl+Shift+H**, Heading: stays here; says "No previous heading".
- **Ctrl+Shift+I**, List item: stays here; says "No previous list item".
- **Ctrl+Shift+K**, Link: stays here; says "No previous link".
- **Ctrl+Shift+L**, List: stays here; says "No previous list".
- **Ctrl+Shift+Q**, Block quote: stays here; says "No previous block quote".
- **Ctrl+Shift+T**, Table: stays here; says "No previous table".
- **Ctrl+Shift+Tab**: to the text.
- **Ctrl+Space**: stays here.
- **Ctrl+T**, Table: stays here; says "Table with 3 rows and 2 columns".
- **Ctrl+Tab**, Between the text and the details: to the text.
- **Enter**, Open the detail: stays here; says "Enter opens the Folder line in axFile".
- **Escape**: stays here.
- **F1**, Command palette: to the command palette over the details.
- **F11**, Word wrap: stays here; says "Word wrap, checked".
- **F12**, Go to line: to a choice in its place over the details.
- **F3**, Find next: to a question over the details.
- **F4**, Send a report: to a form over the details; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F5**, Reload: stays here; says "Reloaded".
- **F6**: to the text.
- **F7**: stays here.
- **F9**, Say the status: stays here; says "README.md. Line 1, Column 1 · UTF-8 · CRLF".
- **Shift+F3**, Find previous: to a question over the details.
- **Shift+Tab**: to the text.
- **Tab**: to the text.

### What stands over the places

- A choice in its place over the details: Escape to the text.
- A Windows dialog over the details: Escape to the details.
- A form over the details: Escape to the details.
- The command palette over the details: Escape to the details.
- A question over the details: Escape to the details.
- A choice in its place over the text: Escape to the text.
- A Windows dialog over the text: Escape to the text.
- A form over the text: Escape to the text.
- The command palette over the text: Escape to the text.
- A question over the text: Escape to the text.

## axTask

On the navigator. It opens on the list.

### The list, a waypoint

Reached from anywhere: Ctrl+F9, free; from the fields of the row: Alt+0, Ctrl+Shift+Tab, Ctrl+Tab, Escape, F6, Shift+Tab, Tab; from a form over the list: Escape; from the command palette over the list: Escape; from a question over the list: Escape.

What can be done here:

- **Alt+0**, All tasks: stays here; says "All tasks, 3 items, Fix the tap, due 30 October 2099, in Home".
- **Ctrl+A**, Select all: stays here; says "No items here to select: go into a list, Right".
- **Ctrl+Comma**: to the command palette over the list.
- **Ctrl+Down**, Next section: stays here.
- **Ctrl+Enter**: stays here.
- **Ctrl+F**, Find in the list: to a question over the list.
- **Ctrl+H**, Hide done tasks: stays here; says "Hide done tasks, checked".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+N**, New item: stays here; says "Items go in a list: Right on a list, then Ctrl+N".
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+S**: stays here; says "Nothing to save here: every change is written at once".
- **Ctrl+Shift+A**, Deselect all: stays here; says "None selected".
- **Ctrl+Shift+F**, Find in every list: to a question over the list.
- **Ctrl+Shift+N**, New list: to a question over the list.
- **Ctrl+Shift+Tab**: to the fields of the row.
- **Ctrl+Space**: stays here.
- **Ctrl+Tab**, Switch between the navigator and the detail pane: to the fields of the row.
- **Ctrl+Up**, Previous section: stays here.
- **Ctrl+Z**, Undo: stays here; says "Nothing to undo".
- **Enter**, Open: stays here; says "A section: Down goes into it".
- **Escape**: stays here.
- **F1**, Command palette: to the command palette over the list.
- **F2**, Rename: stays here; says "This section keeps its name".
- **F3**, Find next: to a question over the list.
- **F4**, Send a report: to a form over the list; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F5**, Refresh: stays here; says "Refreshed: Lists, 4 lists".
- **F6**: to the fields of the row.
- **F7**, Read in full: stays here; says "Your lists, heading level 1, 4 lists. Your lists, heading level 1, 4 lists. Lists in no section of your own, then your sections; Ctrl+M on a list moves it to a section".
- **F9**, Say the status: stays here; says "Lists, 4 lists. {temp}\tasks".
- **Left**, Up a level: stays here.
- **Right**, Into the list: stays here; says "A section: Down goes into it".
- **Shift+Down**, Select to the next item: stays here.
- **Shift+End**, Select to the last item: stays here.
- **Shift+F3**, Find previous: to a question over the list.
- **Shift+Home**, Select to the first item: stays here.
- **Shift+Space**, Select to here: stays here; says "A heading: nothing to select".
- **Shift+Tab**: to the fields of the row.
- **Shift+Up**, Select to the previous item: stays here.
- **Space**, Select or deselect: stays here; says "A heading: nothing to select".
- **Tab**, To the detail pane: to the fields of the row.

### The fields of the row, a place

Reached from a form over the fields of the row: Escape; from the command palette over the fields of the row: Escape; from a question over the fields of the row: Escape; from the list: Ctrl+Shift+Tab, Ctrl+Tab, F6, Shift+Tab, Tab.

What can be done here:

- **Alt+0**, All tasks: to the list; says "All tasks, 3 items, Fix the tap, due 30 October 2099, in Home".
- **Ctrl+Comma**: to the command palette over the fields of the row.
- **Ctrl+Enter**: stays here.
- **Ctrl+F**, Find in the list: to a question over the fields of the row.
- **Ctrl+H**, Hide done tasks: stays here; says "Hide done tasks, not checked".
- **Ctrl+Minus**, Smaller text: stays here; says "Text size 8".
- **Ctrl+Plus**, Larger text: stays here; says "Text size 9".
- **Ctrl+S**: stays here; says "Nothing to save here: every change is written at once".
- **Ctrl+Shift+F**, Find in every list: to a question over the fields of the row.
- **Ctrl+Shift+N**, New list: to a question over the fields of the row.
- **Ctrl+Shift+Tab**: to the list.
- **Ctrl+Space**: stays here.
- **Ctrl+Tab**, Switch between the navigator and the detail pane: to the list.
- **Ctrl+Z**, Undo: stays here; says "Nothing to undo".
- **Enter**, Change the field: stays here; says "Your lists, heading level 1, 4 lists".
- **Escape**, Deselect all: to the list.
- **F1**, Command palette: to the command palette over the fields of the row.
- **F3**, Find next: to a question over the fields of the row.
- **F4**, Send a report: to a form over the fields of the row; says "Report to axKit, form. A report goes to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the maintainer can read it. Nothing in it names you unle...".
- **F5**, Refresh: stays here; says "Refreshed: Lists, 4 lists".
- **F6**: to the list.
- **F7**, Read in full: stays here; says "Your lists, heading level 1, 4 lists".
- **F9**, Say the status: stays here; says "Lists, 4 lists. {temp}\tasks".
- **Left**, The previous value of the field: stays here.
- **Right**, The next value of the field: stays here.
- **Shift+F3**, Find previous: to a question over the fields of the row.
- **Shift+Tab**: to the list.
- **Space**, Check the field: stays here; says "Not a check: Enter changes the field".
- **Tab**, To the navigator: to the list.

### What stands over the places

- A form over the fields of the row: Escape to the fields of the row.
- The command palette over the fields of the row: Escape to the fields of the row.
- A question over the fields of the row: Escape to the fields of the row.
- A form over the list: Escape to the list.
- The command palette over the list: Escape to the list.
- A question over the list: Escape to the list.
