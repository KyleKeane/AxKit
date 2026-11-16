# AI tools: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Let an AI tool read axKit, once you allow it

Starts with: Open axDown with the file notes.md.

1. Call the tool axkit_windows.
    The tool answers: error: axKit's AI tools access is off.
2. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
3. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.
4. Call the tool axkit_windows.
5. Press Escape.
    The tool answers: error: The user did not allow axAutomate.
6. Call the tool axkit_windows.
    The tool answers: not asked again within a minute.
7. Call the tool axkit_windows.
8. Press Tab.
9. Press Space.
    You hear: checked.
10. Press Tab.
11. Press Enter.
    You hear: axAutomate may read axKit.
    The tool answers with app: axDown.
12. Call the tool axkit_window_text.
    The tool answers: First line of the notes.
13. Call the tool axkit_window_state.
    The tool answers: notes.md.
14. Call the tool axkit_window_commands.
    The tool answers with label: Save as.
15. Call the tool axkit_window_commands.
    The tool answers with menu: Session, Open recent file.
16. Call the tool axkit_window_commands.
    The tool answers with label: Word wrap.
17. Call the tool axkit_window_commands.
18. Call the tool axkit_apps.
    The tool answers with verb: pass.
19. Call the tool axkit_window_said.
    The tool answers: axAutomate may read axKit.
20. Open axPass.
21. Go to the axDown window.
22. Call the tool axkit_windows.
    The tool answers with title: notes.md - axDown.
23. Call the tool axkit_open.
    The tool answers: error: axClaude starts a program.
24. Call the tool axkit_window_text.
    The tool answers: error: No such window.
25. Call the tool axkit_open.
    The tool answers: error: The path must be a full path.
26. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
    You hear: AI tools off.
27. Call the tool axkit_windows.
    The tool answers: error: axKit's AI tools access is off.

## Let an AI tool run a window's commands

Starts with: Open axDown with the file notes.md.

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
2. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.
3. Call the tool axkit_windows.
4. Press Tab.
5. Press Space.
    You hear: checked.
6. Press Tab.
7. Press Enter.
    You hear: axAutomate may read axKit.
    The tool answers with app: axDown.
8. Call the tool axkit_window_run.
    The tool answers: error: The user has not allowed AI tools to run commands in axKit. The user allows it in any axKit window: axKit menu, AI tools, Let AI tools run commands.
9. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands: each is said as it runs.
10. Call the tool axkit_window_run.
    The tool answers: axAutomate ran Word wrap. Word wrap.
    You hear: axAutomate ran Word wrap. Word wrap.
11. Call the tool axkit_window_run.
    The tool answers with ran: Word wrap.
    You hear: axAutomate ran Word wrap.
12. Call the tool axkit_window_run.
    The tool answers: error: The window has several commands named Heading: give the menu too, one of: Navigation, Next; Navigation, Previous.
13. Call the tool axkit_window_run.
    The tool answers with ran: Heading.
    You hear: axAutomate ran Heading.
14. Call the tool axkit_window_run.
    The tool answers: error: The window has no command No such command.
15. Call the tool axkit_window_run.
    You hear: axAutomate ran Say new line. Say new line off.
16. Call the tool axkit_window_run.
    You hear: axAutomate ran Say new line. Say new line on.
17. Call the tool axkit_window_run.
    The tool answers with notice: Find.
18. Call the tool axkit_window_run.
    The tool answers: error: The window shows the user a notice.
19. Press Escape.
20. Choose Test a key in the Help menu, Diagnostics.
    You hear: Key test. Press any key.
21. Call the tool axkit_window_run.
    The tool answers: error: The window is in its key test.
22. Call the tool axkit_window_state.
    The tool answers: notes.md.
23. Press Escape.
    You hear: Key test ended.
24. Call the tool axkit_window_run.
    The tool answers with ran: Word wrap.
    You hear: ran Word wrap.
25. Call the tool axkit_window_run.
    The tool answers with ran: Word wrap.
    You hear: ran Word wrap.
26. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may no longer run commands.
27. Call the tool axkit_window_run.
    The tool answers: error: The user has not allowed AI tools to run commands.
28. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
    You hear: AI tools off.
29. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: once AI tools are on.
30. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
31. Press Escape.

## Approve or deny an AI tool before it acts

Starts with: Open axFile with the folder files.

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
2. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.
3. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands.
4. Call the tool axkit_windows.
5. Press Tab.
6. Press Space.
    You hear: checked.
7. Press Tab.
8. Press Enter.
    You hear: axAutomate may read axKit.
    The tool answers with app: axFile.
9. Call the tool axkit_window_run.
    The tool answers: The window asks the user whether to run New folder.
    You hear the sound Offer.
10. Press Enter 2 times.
11. Press Down.
12. Press Enter.
    You hear: axAutomate ran New folder.
13. Press Escape.
14. Call the tool axkit_window_run.
    The tool answers: The window asks the user whether to run Open.
15. Press Escape.
    You hear: Open not run.
16. Call the tool axkit_window_run.
    The tool answers: error: The user denied running a command a moment ago.

## Let an AI tool read and add tasks

Starts with: Open axTask with the folder tasks.

1. Type: pe
2. Press Right.
3. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
4. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.
5. Call the tool axkit_task_lists.
6. Press Tab.
7. Press Space.
    You hear: checked.
8. Press Tab.
9. Press Enter.
    You hear: axAutomate may read axKit.
    The tool answers with not done: 1.
10. Call the tool axkit_task_lists.
    The tool answers with name: alpha / Home.
11. Call the tool axkit_task_lists.
    The tool answers with place: alpha.
12. Call the tool axkit_task_items.
    The tool answers with done left out: 1.
13. Call the tool axkit_task_items.
    The tool answers with title: Old chore.
14. Call the tool axkit_task_items.
    The tool answers: is not one of the axTask window's lists.
15. Call the tool axkit_task_add.
    The tool answers: error: The user has not allowed AI tools to run commands in axKit.
16. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands: each is said as it runs.
17. Call the tool axkit_task_add.
    The tool answers: error: Priority: Not one of the options: High, Normal, Low.
18. Call the tool axkit_task_add.
    The tool answers with added: Call the dentist.
    You hear: axAutomate added Call the dentist to Personal.
19. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: axAutomate added Call the dentist to Personal.
20. Press Home.
21. Call the tool axkit_task_add.
    The tool answers with added: Post the parcel.
    You hear: axAutomate added Post the parcel to Errands.
22. Call the tool axkit_task_add.
    You hear: axAutomate added Buy a washer to alpha / Home.
23. Call the tool axkit_task_add.
    You hear: axAutomate added A new issue to Issues.
24. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may no longer run commands.
25. Call the tool axkit_task_add.
    The tool answers: error: The user has not allowed AI tools to run commands.
26. Press Alt+F4.
27. Call the tool axkit_task_lists.
    The tool answers: error: No axTask window is open.

## Answer an AI tool's request in a list, with the text still there

Starts with: Open axDown with the file notes.md.

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
2. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.
3. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands.
4. Call the tool axkit_windows.
5. Press Tab.
6. Press Space.
    You hear: checked.
7. Press Tab.
8. Press Enter.
    You hear: axAutomate may read axKit.
    The tool answers with app: axDown.
9. Call the tool axkit_window_run.
    The tool answers: The window asks the user whether to run Check for updates at start.
    You hear the sound Offer.
10. Press Enter 2 times.
11. Press Escape.
    You hear: Check for updates at start not run.
12. Call the tool axkit_window_run.
    The tool answers: error: The user denied running a command a moment ago.

## Let an AI tool read a console window's conversation while you type

Starts with: Open axTerminal with the folder myproject.

You hear the sound Ready.
1. Type: Write-Output marker-for-the-ai-tools
2. Press Enter.
    You hear the sound Done.
3. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
4. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.
5. Call the tool axkit_windows.
6. Press Tab.
7. Press Space.
    You hear: checked.
8. Press Tab.
9. Press Enter.
    You hear: axAutomate may read axKit.
    The tool answers with app: axTerminal.
10. Call the tool axkit_window_text.
    The tool answers: marker-for-the-ai-tools.
11. Call the tool axkit_window_text.
    The tool answers with name: Command.
12. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
    You hear: AI tools off.
