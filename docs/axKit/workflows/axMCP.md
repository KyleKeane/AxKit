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
    You hear: Before axMCP starts fake.
    You hear: -NoProfile -ExecutionPolicy Bypass -File fake.ps1.
2. Press Tab.
    You hear: I understand that axMCP.
3. Press Space.
    You hear: checked.
4. Press Enter.
    You hear: fake, starting.
    You hear: fake, running, Tools 1, Resources 1, Prompts 0.

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
    You hear the sound FormOpen.
    You hear: Add server, form.
2. Press Ctrl+Q (Read in full).
    You hear: A server of your own: a name for the list.
3. Type: probe
4. Press Enter.
    You hear the sound Edge.
    You hear: Type the program and its arguments.
5. Type: cmd /c echo
6. Press Tab 2 times.
7. Press Shift+Tab.
8. Press Ctrl+Enter.
    You hear: Added probe.
9. Press Tab.
10. Press Down.
11. Press Escape.

### Rename a server and undo the rename

1. Press F2 (Rename).
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

### Remove several of your servers at once and bring them back

1. Press Ctrl+A.
    You hear: 2 selected.
2. Press Delete.
3. Press Enter.
    You hear: Removed 2 servers.
4. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: removed 2 servers.

### Start a server

1. Type: fake
2. Press Right.
    You hear: Before axMCP starts fake.
3. Press Tab.
4. Press Space.
    You hear: checked.
5. Press Enter.
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
6. Close the window.

## Fill in a number and a choice, and start a server again

Starts with: Open axMCP with the test folder.

### Read the names of a server's environment variables

1. Type: zzfake
2. Press Tab.
3. Press Escape.

### Start a server with Enter

1. Press Enter.
    You hear: Before axMCP starts zzfake.
2. Press Tab.
    You hear: I understand that axMCP.
3. Press Space.
    You hear: checked.
4. Press Enter.
    You hear: zzfake, running, Tools 2, Resources 0, Prompts 0.

### Stop the server selected at the top level

1. Press Ctrl+Shift+S.
    You hear: Stopped zzfake.

### Fill in a number

1. Press Ctrl+A.
2. Type: 3
3. Press Enter.
    You hear: count (required): 3.

### Step through a choice with Right and Left

1. Press Down.
2. Press Right.
    You hear: size: medium.
3. Press Right.
    You hear: size: large.
4. Press Right.
    You hear the sound Edge.
5. Press Left.
    You hear: size: medium.

### Choose from a choice's list

1. Press Enter.
    You hear the sound ListOpen.
2. Press Down.
3. Press Enter.
    You hear: size: large.

### Call the tool and move by the headings of its result

1. Press Ctrl+Enter.
    You hear: Calling order.
2. Press H.
    You hear: Thanks, heading level 2.
3. Press H.
    You hear: No next heading.

### Find a word in a result

1. Press Ctrl+F (Find in the list).
2. Type: made up
3. Press Enter.
4. Press Escape.

### Start a server again after it stopped

1. Press Left.
    You hear: zzfake, stopped (exit code 3), Stopped (exit code 3): Enter starts it again.
2. Press Enter.
    You hear: zzfake, running, Tools 2, Resources 0, Prompts 0.

## Find, filter and sort the servers in axMCP

Starts with: Open axMCP with the test folder.

### Find a server and go to the next and the previous match

1. Press Ctrl+F (Find in the list).
2. Type: zz
3. Press Enter.
4. Press F3 2 times (Find next).
5. Press Shift+F3 (Find previous).

### Show only the servers whose name contains a text

1. Choose Filter in the Options menu.
2. Type: zz
3. Press Enter.
    You hear: filter zz, 3 shown.
4. Press Escape.
    You hear: Filter cleared.

### Put the servers in the order of their names

1. Choose Name in the Options menu, Sort by.
    You hear: Sorted by name.
2. Choose The list's own order in the Options menu, Sort by.
    You hear: Sorted by the list's own order.
