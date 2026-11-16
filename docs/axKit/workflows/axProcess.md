# axProcess: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Find and end a process

### Hear how much memory is in use

1. Press Ctrl+Q (Summary; Actions menu).
    You hear: GB in use.

### Read the list again

1. Press F5 (Refresh; Session menu).
    You hear words such as: Refreshed: Processes.

### Sort the processes by name, processor or memory

1. Choose Name in the Options menu, Sort processes by.
    You hear: Sorted by name.
2. Choose Processor in the Options menu, Sort processes by.
    You hear: Sorted by processor.
3. Choose Memory in the Options menu, Sort processes by.
    You hear: Sorted by memory.

### Find a process and read its details

1. Press Ctrl+F (Find in the list).
2. Type: PING
3. Press Enter 2 times.
4. Press Escape.

### End a process

1. Press Delete.
2. Press Enter.
    You hear: Ended PING.

## Manage services and startup programs

Starts with: Open axProcess.

### Show the services

1. Press Ctrl+2 (Services; Navigation menu).

### Read a service's details

1. Press Ctrl+F (Find in the list).
2. Type: (EventLog)
3. Press Enter 2 times.
4. Press Escape.

### Go back from the services to the processes

1. Press Ctrl+2 (Services; Navigation menu).
2. Press Ctrl+1 (Processes; Navigation menu).

### Show the programs that start with Windows

1. Press Ctrl+3 (Startup; Navigation menu).
    You hear: Startup, 1. Morning, enabled, your Startup folder.

### Read the keyboard shortcuts

1. Press F1 (Keyboard shortcuts; Help menu).
2. Press Escape.

### Go round the views one after another

1. Press Ctrl+1 (Processes; Navigation menu).
2. Press Tab.
    You hear words such as: Services.
3. Press Tab.
    You hear words such as: Startup.
4. Press Tab.
    You hear words such as: Processes.
5. Press Shift+Tab.
    You hear words such as: Startup.

### Jump to a service by typing its name

1. Press Ctrl+2 (Services; Navigation menu).
2. Type: windows event l

## Jump to a process by typing its name and read a startup program's details

Starts with: Open axProcess.

### Jump to a process by typing its name

1. Choose Name in the Options menu, Sort processes by.
    You hear: Sorted by name.
2. Press Home.
3. Type: svc
4. Type: s

### Read a startup program's details

1. Press Ctrl+3 (Startup; Navigation menu).
    You hear: Startup, 1. Morning notes, enabled, your Startup folder.
2. Press Enter.
3. Press Escape.

## Turn a startup program off, on again, or remove it

Starts with: Open axProcess.

### List the programs that start with Windows

1. Press Ctrl+3 (Startup; Navigation menu).
    You hear: Startup, 4. Check Alpha, enabled, current user's Run key.

### Turn off a program that starts with Windows

1. Choose the menu item Turn off.
    You hear: Check Alpha turned off: it no longer starts with Windows.

### Turn a startup program on again

1. Choose the menu item Turn on.
    You hear: Check Alpha turned on: it starts with Windows again.

### Remove a program from those that start with Windows

1. Press Delete.
2. Press Alt+R.
    You hear: Check Alpha removed from the programs that start with Windows.

### Turn a Startup folder shortcut off and on again from its details

1. Press Down.
2. Choose the menu item Turn off.
    You hear: Check Morning turned off: it no longer starts with Windows.
3. Press Enter.
4. Press Alt+N.
    You hear: Check Morning turned on: it starts with Windows again.

### Remove a shortcut from the Startup folder

1. Press Delete.
2. Press Alt+R.
    You hear: Check Morning removed from the programs that start with Windows.
