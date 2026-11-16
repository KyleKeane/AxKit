# axMCP: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Start an MCP server and use its tools

### Find a server by its name

1. Open axMCP with the test folder.
2. Type: fake

### Read a server's details

1. Press Tab.
2. Press Down.
3. Press Escape.

### Start a server and open its tools

1. Press Right.
    You hear: fake, starting.
    You hear: fake, running, Tools 1, Resources 1, Prompts 0.
2. Press Right.
    You hear: Tools, 1 tool, echo, tool, Says the text back.

### Read a tool's description and its arguments

1. Press Tab.
2. Press Down.

### Fill in an argument and call the tool

1. Press Enter.
2. Type: hello
3. Press Enter.
    You hear: text (required): hello.
4. Press Ctrl+Enter.
    You hear: Calling echo.
5. Press Escape.

### Open a tool's answer in axJSON

1. Press Ctrl+J.
2. A new window opens: go to it.
3. Close the window.
4. Go to the axMCP window.

### Read a resource

1. Press Escape.
2. Press Left.
3. Press Down.
4. Press Right.
    You hear: Resources, 1 resource, readme, resource, fake://readme.
5. Press Enter.
    You hear: Reading readme.
6. Press Escape.

### Stop a server

1. Press Ctrl+Shift+S.
    You hear: Stopped fake.
2. Press Left 2 times.

## Add, rename and remove a server of your own in axMCP

Starts with: Open axMCP with the test folder.

### Add a server of your own

1. Press Ctrl+N.
    You hear: A server of your own: a name for the list.
2. Type: probe
3. Press Enter.
    You hear the sound Edge.
    You hear: Type the program and its arguments.
4. Type: cmd /c echo
5. Press Tab 2 times.
6. Press Shift+Tab.
7. Press Ctrl+Enter.
    You hear: Added probe.
8. Press Tab.
9. Press Down.
10. Press Escape.

### Rename a server and undo the rename

1. Press F2.
2. Press Ctrl+A.
3. Type: probe two
4. Press Enter.
    You hear: Renamed to probe two.
5. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: renamed probe to probe two.

### Remove a server

1. Press Delete.
2. Press Enter.
    You hear: Removed probe.

### Undo the removal of a server

1. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: removed probe.

### Start a server

1. Type: fake
2. Press Right.
    You hear: fake, running, Tools 4, Resources 2, Prompts 1.

### Tick a yes-or-no argument and call the tool

1. Press Right.
    You hear: Tools, 4 tools, echo, tool, Says the text back.
2. Press Tab.
3. Press Down 2 times.
4. Press Space.
    You hear: loud: done.
5. Press Up.
6. Press Enter.
7. Type: hello
8. Press Enter.
    You hear: text (required): hello.
9. Press Ctrl+Enter.
    You hear: Calling echo.
10. Press Escape 2 times.

### Read a resource from a template

1. Press Left.
2. Press Down.
3. Press Right.
    You hear: Resources, 2 resources, readme, resource, fake://readme.
4. Press Down.
5. Press Enter 2 times.
    You hear: Reading page.
6. Press Escape.

### Read a prompt's arguments

1. Press Left.
2. Press Down.
3. Press Right.
    You hear: Prompts, 1 prompt, greet, prompt, Greets someone.
4. Press Tab.
5. Press Down.

### Fill in a prompt and fetch it

1. Press Enter.
2. Type: Ada
3. Press Enter.
    You hear: who (required): Ada.
4. Press Ctrl+Enter.
    You hear: Fetching greet.
5. Press Escape.
