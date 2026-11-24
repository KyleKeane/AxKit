# AI tools: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives words of what axKit says there, not always the whole sentence; after "words such as" they are
cut short. After "Type:" comes the text to type, to the end of the line, with no full stop added. The file names and
the words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Let an AI tool read axKit, once you allow it

Starts with: Open axDown with the file notes.md.

### Turn on the AI tools

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
2. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.

### Allow an AI tool to use axKit

1. Call the tool axkit_windows.
2. Press Tab.
3. Press Space.
    You hear: checked.
4. Press Tab.
5. Press Enter.
    You hear: axAutomate may read axKit.
    The tool answers with app: axDown.

### Let an AI tool read a window's text, state and commands

1. Call the tool axkit_window_text.
    The tool answers: First line of the notes.
2. Call the tool axkit_window_state.
    The tool answers: notes.md.
3. Call the tool axkit_window_commands.
    The tool answers with label: Save as.
4. Call the tool axkit_window_commands.
    The tool answers with menu: Session, Open recent file.
5. Call the tool axkit_window_commands.
    The tool answers with label: Word wrap.
6. Call the tool axkit_window_commands.
7. Call the tool axkit_apps.
    The tool answers with verb: pass.
8. Call the tool axkit_window_said.
    The tool answers: axAutomate may read axKit.

### Turn the AI tools off

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
    You hear: AI tools off.
2. Call the tool axkit_windows.
    The tool answers: error: axKit's AI tools access is off.

## Let an AI tool run a window's commands

Starts with: Open axDown with the file notes.md.

### Let AI tools run commands

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands: each is said as it runs.

### Let an AI tool run a window's command

1. Call the tool axkit_window_run.
    The tool answers: axAutomate ran Word wrap. Word wrap.
    You hear: axAutomate ran Word wrap. Word wrap.

### Let an AI tool change a setting

1. Call the tool axkit_window_run.
    You hear: axAutomate ran Say new line. Say new line off.
2. Call the tool axkit_window_run.
    You hear: axAutomate ran Say new line. Say new line on.

### Let an AI tool run a command with its text

1. Call the tool axkit_window_run.
    The tool answers with ran: Find first.
    You hear: axAutomate ran Find first with the text second.

### Stop AI tools from running commands

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may no longer run commands.
2. Call the tool axkit_window_run.
    The tool answers: error: The user has not allowed AI tools to run commands.

## Approve or deny an AI tool before it acts

Starts with: Open axFile with the folder files.

### Read the explanation of a command an AI tool asks to run

1. Press Enter 2 times.

### Allow a command an AI tool asks to run

1. Press Down.
2. Press Enter.
    You hear: axAutomate ran New folder.
3. Press Escape.

### Deny a command an AI tool asks to run

1. Call the tool axkit_window_run.
    The tool answers: The window asks the user whether to run Open.
2. Press Escape.
    You hear: Open not run.

## Let an AI tool read and add tasks

Starts with: Open axTask with the folder tasks.

### Let an AI tool read the task lists

1. Call the tool axkit_task_lists.
    The tool answers with name: alpha / Home.
2. Call the tool axkit_task_lists.
    The tool answers with place: alpha.

### Let an AI tool read a list's items

1. Call the tool axkit_task_items.
    The tool answers with done left out: 1.
2. Call the tool axkit_task_items.
    The tool answers with title: Old chore.

### Let AI tools run commands

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands: each is said as it runs.

### Let an AI tool add a task

1. Call the tool axkit_task_add.
    The tool answers with added: Call the dentist.
    You hear: axAutomate added Call the dentist to Personal.

### Undo a task an AI tool added

1. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: axAutomate added Call the dentist to Personal.

### Let an AI tool add a task to another list

1. Press Home.
2. Call the tool axkit_task_add.
    The tool answers with added: Post the parcel.
    You hear: axAutomate added Post the parcel to Errands.

### Let an AI tool add a task to a project's list

1. Call the tool axkit_task_add.
    You hear: axAutomate added Buy a washer to alpha / Home.

### Stop AI tools from running commands

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may no longer run commands.
2. Call the tool axkit_task_add.
    The tool answers: error: The user has not allowed AI tools to run commands.

## Answer an AI tool's request in a list, with the text still there

Starts with: Open axDown with the file notes.md.

### Read the explanation of a command an AI tool asks to run

1. Press Enter 2 times.

### Deny a command an AI tool asks to run

1. Press Escape.
    You hear: Check for updates at start not run.

## Let an AI tool read a console window's conversation while you type

Starts with: Open axTerminal with the folder myproject.

### Let an AI tool read the terminal while you type

1. Call the tool axkit_window_text.
    The tool answers: marker-for-the-ai-tools.
2. Call the tool axkit_window_text.
    The tool answers with name: Command.

### Turn the AI tools off

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
    You hear: AI tools off.

## Let an AI tool read a form's fields and move to one

Starts with: Open axTemplate.

### Let an AI tool read the fields of a form

1. Press Ctrl+N.
2. Type: Thanks
3. Call the tool axkit_window_state.
    The tool answers with value: Thanks.
4. Call the tool axkit_window_state.
    The tool answers with kind: text of several lines.

### Let an AI tool move to a field of a form

1. Call the tool axkit_window_field.
    The tool answers with focused: Text.
    You hear: axAutomate moved to Text.
