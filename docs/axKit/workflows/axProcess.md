# axProcess: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Find and end a process

Starts with: Open axProcess.

You hear words such as: Processes.
1. Press Ctrl+Q (Summary; Actions menu).
    You hear: GB in use.
2. Press F5 (Refresh; Session menu).
    You hear words such as: Refreshed: Processes.
3. Choose Name in the Options menu, Sort processes by.
    You hear: Sorted by name.
4. Choose Processor in the Options menu, Sort processes by.
    You hear: Sorted by processor.
5. Choose Memory in the Options menu, Sort processes by.
    You hear: Sorted by memory.
6. Press Ctrl+F (Find in the list).
7. Type: PING
8. Press Enter 2 times.
9. Press Escape.
10. Press Delete.
11. Press Escape.
12. Press Delete.
13. Press Enter.
    You hear: Ended PING.
14. Press Ctrl+S.
    You hear: Nothing to save here. F5 refreshes, Delete ends a process.

## Manage services and startup programs

Starts with: Open axProcess.

1. Press Ctrl+2 (Services; Navigation menu).
2. Press Ctrl+F (Find in the list).
3. Type: (EventLog)
4. Press Enter 2 times.
5. Press Escape.
6. Press Delete.
    You hear: Processes view first: Ctrl+1.
7. Press Ctrl+E.
    You hear: Processes view first: Ctrl+1.
8. Press Ctrl+2 (Services; Navigation menu).
9. Press Ctrl+1 (Processes; Navigation menu).
10. Press Ctrl+3 (Startup; Navigation menu).
    You hear: Startup, 1. Morning, enabled, your Startup folder.
11. Press F1 (Keyboard shortcuts; Help menu).
12. Press Escape.
13. Press Ctrl+1 (Processes; Navigation menu).
14. Press Tab.
    You hear words such as: Services.
15. Press Tab.
    You hear words such as: Startup.
16. Press Tab.
    You hear words such as: Processes.
17. Press Shift+Tab.
    You hear words such as: Startup.
18. Press Ctrl+2 (Services; Navigation menu).
19. Press Home.
20. Press Up.
    You hear the sound Edge.
21. Press End.
22. Press Down.
    You hear the sound Edge.
23. Press Ctrl+2 (Services; Navigation menu).
24. Type: windows event l
25. Press End.
26. Type: w

## Jump to a process by typing its name and read a startup program's details

Starts with: Open axProcess.

You hear words such as: Processes.
1. Choose Name in the Options menu, Sort processes by.
    You hear: Sorted by name.
2. Press Home.
3. Type: svc
4. Type: s
5. Press Ctrl+3 (Startup; Navigation menu).
    You hear: Startup, 1. Morning notes, enabled, your Startup folder.
6. Press Enter.
7. Press Escape.

## Turn a startup program off, on again, or remove it

Starts with: Open axProcess.

1. Press Ctrl+3 (Startup; Navigation menu).
    You hear: Startup, 4. Check Alpha, enabled, current user's Run key.
2. Choose the menu item Turn off.
    You hear: Check Alpha turned off: it no longer starts with Windows.
3. Choose the menu item Turn off.
    You hear: Check Alpha is already turned off.
4. Choose the menu item Turn on.
    You hear: Check Alpha turned on: it starts with Windows again.
5. Choose the menu item Turn on.
    You hear: Check Alpha is already turned on.
6. Press Delete.
7. Press Enter.
8. Press Delete.
9. Press Escape.
10. Press Delete.
11. Press Alt+R.
    You hear: Check Alpha removed from the programs that start with Windows.
12. Press Down.
13. Choose the menu item Turn off.
    You hear: Check Everyone is set for every user of this computer: turning it off needs administrator rights. Use Task Manager's Startup apps tab, or Settings, Apps, Startup.
14. Press Delete.
    You hear: Check Everyone is set for every user of this computer: removing it needs administrator rights. Task Manager's Startup apps tab, or Settings, Apps, Startup, turns it off.
15. Press Down.
16. Choose the menu item Turn off.
    You hear: Check Morning turned off: it no longer starts with Windows.
17. Press Enter.
18. Press Alt+N.
    You hear: Check Morning turned on: it starts with Windows again.
19. Press Delete.
20. Press Alt+R.
    You hear: Check Morning removed from the programs that start with Windows.
21. Press Ctrl+2 (Services; Navigation menu).
22. Press Delete.
    You hear: Processes view first: Ctrl+1.
