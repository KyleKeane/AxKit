# axAutomate: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Browse a folder of workflows and read their steps

Starts with: Open axAutomate with the folder flows.

1. Press Down.
2. Press Up, Right.
    You hear: axDown, 1 workflow, 0 folders, 02-palette, workflow, 6 steps.
3. Press Enter.
    You hear: 02-palette, 6 steps, 1. Start down.
4. Press Down 4 times.
5. Press Up, Tab.
6. Press Down.
7. Press Escape.
8. Press Delete.
    You hear: This version views and runs the workflows.
9. Press Left 2 times.
10. Close the window.

## Run a workflow and hear how it went

Starts with: Open axAutomate with the folder flows.

1. Press F5.
    You hear: Running 6 workflows.
    You hear: 01-passes passed, 4 steps.
    You hear: Step 2 failed: Expect the focus: Nothing like this. Not within 10 s.
    You hear: 02-fails failed at step 2.
    You hear: 03-waits passed, 4 steps.
    You hear: A workflow file passed, 2 steps.
    You hear: Later cannot run: step 2 has otherwise steps.
    You hear: 06-bad cannot run: the step "wait 2s" needs a whole number of milliseconds.
    You hear: Run finished: 3 of 6 workflows passed, 1 failed, 2 could not run.
2. Press Down.
3. Press Tab.
4. Press Escape, Right.
    You hear: 02-fails, 3 steps, 1. Start down.
5. Press Down.
6. Press Tab, Down.
7. Press Escape.
8. Press Ctrl+R (The last run's report; Actions menu).
9. Press Escape.
10. Press Left, Down.
11. Press Right, F5.
    You hear: Running 03-waits.
12. Press Escape.
    You hear: Stopping after the current step.
    You hear: 03-waits stopped.
13. Press Left.
14. Press End, Right, F5.
    You hear: This workflow cannot run: the step "wait 2s" needs a whole number of milliseconds.
15. Press Left, Up, Right, F5.
    You hear: This workflow cannot run: step 2 has otherwise steps.
16. Close the window.
17. Open axAutomate with the folder cmds.
18. Press Right.
    You hear: 07-commands, 1 step.
19. Press F5.
20. Press Enter.
    You hear: Not run.
21. Press F5.
22. Press Tab.
23. Press Space.
    You hear: Running 07-commands; Escape stops it.
    You hear: 07-commands passed, 1 step.
24. Press Left, Down.
25. Press Tab.
26. Press Escape, Right, F5.
    You hear: This workflow cannot run: step 1 writes outside the run's folder.
27. Close the window.

## Close axAutomate and come back to the folder you had

Starts with: Open axAutomate with the folder flows. Close the window. Open axAutomate.

1. Press Right.
    You hear: 02-two, 3 steps, 1. Start down.
2. Close the window.
3. Open axAutomate.
