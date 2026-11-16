# axProcess: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence. The file names and the words
typed are the check's examples: use your own. Where a step names a command after its key, the key is that command's
in this version of axKit.

## Find and end a process

Starts with: Open axProcess.

1. Press Ctrl+Q (Summary).
   You hear: GB in use.
2. Press F5 (Refresh).
   You hear: Refreshed: Processes.
3. Choose Name in the Options menu, Sort processes by.
   You hear: Sorted by name.
4. Choose Processor in the Options menu, Sort processes by.
   You hear: Sorted by processor.
5. Choose Memory in the Options menu, Sort processes by.
   You hear: Sorted by memory.
6. Press Ctrl+F (Find in the list).
7. Type PING.
8. Press Enter.
9. Press Enter.
10. Press Escape.
11. Press Delete.
12. Press Escape.
13. Press Delete.
14. Press Enter.
   You hear: Ended PING.
15. Press Ctrl+S.
   You hear: Nothing to save here. F5 refreshes, Delete ends a process.

## Manage services and startup programs

Starts with: Open axProcess.

1. Press Ctrl+2 (Services, Navigation).
2. Press Ctrl+F (Find in the list).
3. Type (EventLog).
4. Press Enter.
5. Press Enter.
6. Press Escape.
7. Press Delete.
   You hear: Processes view first: Ctrl+1.
8. Press Ctrl+E.
   You hear: Processes view first: Ctrl+1.
9. Press Ctrl+2 (Services, Navigation).
10. Press Ctrl+1 (Processes).
11. Press Ctrl+3 (Startup).
   You hear: Startup, 1. Morning, enabled, your Startup folder.
12. Press F1 (Keyboard shortcuts).
13. Press Escape.
14. Press Ctrl+1 (Processes).
15. Press Tab.
16. Press Tab.
17. Press Tab.
18. Press Shift+Tab.
19. Press Ctrl+2 (Services, Navigation).
20. Press Home.
21. Press Up.
   You hear the sound Edge.
22. Press End.
23. Press Down.
   You hear the sound Edge.
24. Press Ctrl+2 (Services, Navigation).
25. Type windows event l.
26. Press End.
27. Type w.

## Jump to a process by typing its name and read a startup program's details

Starts with: Open axProcess.

1. Choose Name in the Options menu, Sort processes by.
   You hear: Sorted by name.
2. Press Home.
3. Type svc.
4. Type s.
5. Press Ctrl+3 (Startup).
   You hear: Startup, 1. Morning notes, enabled, your Startup folder.
6. Press Enter.
7. Press Escape.

## Turn a startup program off, on again, or remove it

Starts with: Open axProcess.

1. Press Ctrl+3 (Startup).
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
21. Press Ctrl+2 (Services, Navigation).
22. Press Delete.
   You hear: Processes view first: Ctrl+1.
