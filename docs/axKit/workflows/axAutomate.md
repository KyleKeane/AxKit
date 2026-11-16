# axAutomate: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Browse a folder of workflows and read their steps

### Open a folder of workflows

1. Open axAutomate with the folder flows.
2. Press Down.

### Open a workflow and read its steps

1. Press Up, Right.
    You hear: axDown, 1 workflow, 0 folders, 02-palette, workflow, 6 steps.
2. Press Enter.
    You hear: 02-palette, 6 steps, 1. Start down.
3. Press Down 4 times.

### Read a step's details

1. Press Up, Tab.
2. Press Down.
3. Press Escape.

## Run a workflow and hear how it went

### Run every workflow of a folder

1. Open axAutomate with the folder flows.
2. Press F5.
    You hear: Running 6 workflows.
    You hear: 01-passes passed, 4 steps.
    You hear: Step 2 failed: Expect the focus: Nothing like this. Not within 10 s.
    You hear: 02-fails failed at step 2.
    You hear: 03-waits passed, 4 steps.
    You hear: A workflow file passed, 2 steps.
    You hear: Later cannot run: step 2 has otherwise steps.
    You hear: 06-bad cannot run: the step "wait 2s" needs a whole number of milliseconds.
    You hear: Run finished: 3 of 6 workflows passed, 1 failed, 2 could not run.

### Read how a workflow's last run went

1. Press Down.
2. Press Tab.
3. Press Escape, Right.
    You hear: 02-fails, 3 steps, 1. Start down.
4. Press Down.
5. Press Tab, Down.
6. Press Escape.

### Read the report of a workflow's last run

1. Press Ctrl+R (The last run's report; Actions menu).
2. Press Escape.

### Run one workflow and stop it partway

1. Press Left, Down.
2. Press Right, F5.
    You hear: Running 03-waits.
3. Press Escape.
    You hear: Stopping after the current step.
    You hear: 03-waits stopped.
4. Press Left.

### Run a workflow that runs commands

1. Press F5.
2. Press Tab.
3. Press Space.
    You hear: Running 07-commands; Escape stops it.
    You hear: 07-commands passed, 1 step.

## Close axAutomate and come back to the folder you had

### Open a folder of workflows

1. Open axAutomate with the folder flows.
2. Close the window.

### Come back to the folder used last

1. Open axAutomate.
