# AI tools: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives what axKit says there; "something with" and words in quotes give a part of it. After "Type"
the text to type stands in quotes: the quotes and the full stop after them are not typed. The file names and the
words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Let an AI tool read axKit, once you allow it

Starts with: Open axDown with the file notes.md.

### Turn on the AI tools

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
2. Press Enter.
    You hear: AI tools on.
    You may hear: Not in a test run.

### Allow an AI tool to use axKit

1. Ask Claude to list the open windows.
2. Press Tab.
3. Press Space.
    You hear: checked.
4. Press Tab.
5. Press Enter.
    You hear: axAutomate may read axKit.
    The tool answers with app: axDown.

### Let an AI tool read a window's text, state and commands

1. Ask Claude to read the window's text.
    The tool answers: First line of the notes.
2. Ask Claude to read the window's state.
    The tool answers: notes.md.
3. Ask Claude to list the window's commands.
    The tool answers with label: Save as.
4. Ask Claude to list the window's commands.
    The tool answers with menu: Session, Open recent file.
5. Ask Claude to list the window's commands.
    The tool answers with label: Word wrap.
6. Ask Claude to list the window's commands.
7. Ask Claude to list axKit's apps.
    The tool answers with verb: pass.
8. Ask Claude to read what the window said.
    The tool answers: axAutomate may read axKit.

### Turn the AI tools off

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
    You hear: AI tools off.
2. Ask Claude to list the open windows.
    The tool answers: error: axKit's AI tools access is off.

## Let an AI tool run a window's commands

Starts with: Open axDown with the file notes.md.

### Let AI tools run commands

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands: each is said as it runs.

### Let an AI tool run a window's command

1. Ask Claude to run the command Word wrap.
    The tool answers: axAutomate ran Word wrap. Word wrap.
    You hear: axAutomate ran Word wrap. Word wrap.

### Let an AI tool change a setting

1. Ask Claude to run the command Say new line.
    You hear: axAutomate ran Say new line. Say new line, not checked.
2. Ask Claude to run the command Say new line.
    You hear: axAutomate ran Say new line. Say new line, checked.

### Let an AI tool run a command with its text

1. Ask Claude to run the command Find first with the text "second".
    The tool answers with ran: Find first.
    You hear: axAutomate ran Find first with the text second.

### Stop AI tools from running commands

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may no longer run commands.
2. Ask Claude to run the command Word wrap.
    The tool answers: error: The user has not allowed AI tools to run commands.

## Approve or deny an AI tool before it acts

Starts with: Open axFile with the folder files.

### Read the explanation of a command an AI tool asks to run

1. Press Enter twice.

### Allow a command an AI tool asks to run

1. Press Down.
2. Press Enter.
    You hear: axAutomate ran New folder.
3. Press Escape.

### Deny a command an AI tool asks to run

1. Ask Claude to run the command Open.
    The tool answers: The window asks the user whether to run Open.
2. Press Escape.
    You hear: Open not run.

## Let an AI tool read and add tasks

Starts with: Open axTask with the folder tasks.

### Let an AI tool read the task lists

1. Ask Claude to list the task lists.
    The tool answers with name: alpha / Home.
2. Ask Claude to list the task lists.
    The tool answers with place: alpha.

### Let an AI tool read a list's items

1. Ask Claude to read the items of the list personal.
    The tool answers with done left out: 1.
2. Ask Claude to read the items of the list Personal, the done ones too.
    The tool answers with title: Old chore.

### Let AI tools run commands

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may run commands: each is said as it runs.

### Let an AI tool add a task

1. Ask Claude to add "Call the dentist" to the list Personal, with Due 2026-11-18 and Priority High.
    The tool answers with added: Call the dentist.
    You hear: axAutomate added Call the dentist to Personal.

### Undo a task an AI tool added

1. Press Ctrl+Z (Undo; Actions menu).
    You hear: Undone: axAutomate added Call the dentist to Personal.

### Let an AI tool add a task to another list

1. Press Home.
2. Ask Claude to add "Post the parcel" to the list Errands.
    The tool answers with added: Post the parcel.
    You hear: axAutomate added Post the parcel to Errands.

### Let an AI tool add a task to a project's list

1. Ask Claude to add "Buy a washer" to the list home.
    You hear: axAutomate added Buy a washer to alpha / Home.

### Stop AI tools from running commands

1. Choose Let AI tools run commands in the axKit menu, AI tools.
    You hear: AI tools may no longer run commands.
2. Ask Claude to add "Call the dentist" to the list Personal.
    The tool answers: error: The user has not allowed AI tools to run commands.

## Answer an AI tool's request in a list, with the text still there

Starts with: Open axDown with the file notes.md.

### Read the explanation of a command an AI tool asks to run

1. Press Enter twice.

### Deny a command an AI tool asks to run

1. Press Escape.
    You hear: Check for updates at start not run.

## Let an AI tool read a console window's conversation while you type

Starts with: Open axTerminal with the folder myproject.

### Let an AI tool read the terminal while you type

1. Ask Claude to read the window's text.
    The tool answers: marker-for-the-ai-tools.
2. Ask Claude to read the window's text.
    The tool answers with name: Command.

### Turn the AI tools off

1. Choose Let AI tools read and open axKit in the axKit menu, AI tools.
    You hear: AI tools off.

## Let an AI tool read a form's fields and move to one

Starts with: Open axTemplate.

### Let an AI tool read the fields of a form

1. Press Ctrl+N.
2. Type "Thanks".
3. Ask Claude to read the window's state.
    The tool answers with value: Thanks.
4. Ask Claude to read the window's state.
    The tool answers with kind: text of several lines.

### Let an AI tool move to a field of a form

1. Ask Claude to put the focus on the field Text.
    The tool answers with focused: Text.
    You hear: axAutomate moved to Text.
