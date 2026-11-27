# axTerminal: workflows

Written by `tools/workflows.ps1`; never edit it by hand. Every goal below is played by axKit's automated checks as it
is written here: the steps are what the check presses and types, and the lines under a step are what axKit says. A
line "You hear" gives what axKit says there; "something with" and words in quotes give a part of it. After "Type"
the text to type stands in quotes: the quotes and the full stop after them are not typed. The file names and the
words typed are the check's examples: use your own. Where a step names a command after its key, the key is that
command's in this version of axKit.

## Start a terminal

Starts with: Open axTerminal with the folder myproject.

### Move between the command field and the terminal

1. Press Alt+2 (Terminal; Navigation menu).
    The focus moves to Terminal.

### Go back to the command field

1. Press Alt+1 (Command field; Navigation menu).
    The focus moves to Command.

### Hear the state, the shell and the prompt

1. Press F7 (What is running; Actions menu).
    You hear: PowerShell, PS myproject>.

## Run commands and read their output

Starts with: Open axTerminal with the folder myproject.

### Run a command and read its output by headings

1. Type "Write-Output hello".
2. Press Enter.
    You hear the Sent sound and Sent.
    You hear the Done sound and Done.
3. Press Alt+3 (Latest output; Navigation menu).
    You hear: Output 1 heading level 1.
4. Press Down twice.

### Turn the heading marks in the text off and on

1. Choose Markers in the text in the Options menu, Text.

### Turn the heading marks back on

1. Choose Markers in the text in the Options menu, Text.

### Answer a program that asks

1. Type "Read-Host "Continue? (y/n)"".
2. Press Enter.
    You hear: Sent.
    You hear something with "Asks: Continue? (y/n)" in it.
    You hear the sound Question.
3. Type "y".
4. Press Enter.
    You hear: Sent.
    You hear the Done sound and Done.
5. Press Alt+2 (Terminal; Navigation menu).
6. Press Alt+1 (Command field; Navigation menu).

### Send a command of several lines

1. Type "if ($true) {".
2. Press Shift+Enter.
    You hear: new line.
3. Type ""yes"".
4. Press Shift+Enter.
5. Type "}".
6. Press Enter.
    You hear the Done sound and Done.
7. Press Alt+3 (Latest output; Navigation menu).
    You hear: Output 6 heading level 1.
8. Press Down twice.
9. Press Alt+1 (Command field; Navigation menu).

### Send a command without recording it

1. Type "echo secret".
2. Choose Send without recording in the Actions menu.
    You hear: Sent, not recorded.
    You hear the Done sound and Done.

### Put a recent command back in the field

1. Press Alt+Down (Recent commands; Actions menu).
2. Press Down.
3. Press Enter.

### Copy a command line of the terminal into the field

1. Press Alt+3 (Latest output; Navigation menu).
    You hear: Output 7 heading level 1.
2. Press Shift+I.
    You hear: Input 7 heading level 1.
3. Press 2 twice.
    You hear: Command heading level 2.
4. Press Down.
5. Press Enter.
    You hear: Line copied.
6. Press Ctrl+A.
7. Press Delete.

### Interrupt a long command

1. Type "Start-Sleep 30".
2. Press Enter.
    You hear: Sent.
3. Press F7 (What is running; Actions menu).
    You hear something with "Running: Start-Sleep 30" in it.
4. Press Ctrl+I (Interrupt; Actions menu).
    You hear: Interrupt sent.
    You hear the sound Done.
5. Press Shift+Escape.
    You hear: Escape sent.

### Play music while a command runs

1. Choose Background music while a command runs in the Options menu, Sounds (key: Ctrl+M).
2. Type "Start-Sleep 3".
3. Press Enter.
    You hear the sound StartMusic.
    You hear the sound StopMusic.
    You hear the Done sound and Done.
4. Press Ctrl+M (Background music on or off).
    You hear: Background music off.

## Hear a command's output as it arrives

Starts with: Open axTerminal with the folder myproject.

### Hear Done before a command's output

1. Choose Speak output: all lines in the Options menu, Speech.
    You hear: Speak output: all lines.
2. Type "Write-Output hello".
3. Press Enter.
    You hear: Done. hello.
    You hear the sound Done.

### Turn the word Done off

1. Choose Announce "Done" in the Options menu, When a command is done.
    You hear: Announce "Done", not checked.
2. Type "Write-Output again".
3. Press Enter.
    You hear the sound Done.
    You hear: again.

## Switch shells and folders

Starts with: Open axTerminal with the folder work\myproject.

### Start a new session in another shell

1. Press Ctrl+N (New session; Session menu).
2. Press Tab seven times.
3. Press Down.
4. Press Enter.
    You hear: New session in myproject.
    You hear: Ready.

### Switch to another shell

1. Choose the menu item Windows PowerShell.
    You hear: Windows PowerShell.
    You hear: Ready.

### Hear the current folder's full path

1. Type "cd ..".
2. Press Enter.
    You hear the Done sound and Done.
3. Press F8 (Say current folder; Session menu).
    You hear: work.
4. Type "cd myproject".
5. Press Enter.
    You hear the Done sound and Done.

### Restart the shell

1. Press Ctrl+Shift+R (Restart shell; Session menu).
    You hear: Restarting.
    You hear: Ready.

### Restart the shell while a command runs

1. Type "ping -n 30 127.0.0.1".
2. Press Enter.
    You hear: Sent.
3. Press Ctrl+Shift+R (Restart shell; Session menu).
4. Press Escape.
5. Press Ctrl+Shift+R (Restart shell; Session menu).
6. Press Enter.
    You hear: Restarting.
    You hear: Ready.

### Start the shell again after it ended

1. Type "exit".
2. Press Enter.
    You hear: The shell ended.
3. Press Alt+2 (Terminal; Navigation menu).
4. Press Alt+1 (Command field; Navigation menu).
5. Type "echo kept".
6. Press Enter.
    You hear: The shell ended. Ctrl+Shift+R starts it again.
7. Press Ctrl+Shift+R (Restart shell; Session menu).
    You hear: Ready.

## Use axTerminal's text, notices and axKit menu

Starts with: Open axTerminal with the folder myproject.

### Clear the window

1. Press Ctrl+Shift+Delete (Clear transcript; Session menu).
    You hear: Cleared.

### Set a bookmark and go back to it

1. Press Alt+3 (Latest output; Navigation menu).
    You hear: Output 1 heading level 1.
2. Press Down twice.
3. Press M.
    You hear: Bookmark 1 added.
4. Press Ctrl+Home.
5. Press K.
    You hear: Bookmark 1, first.

### Find text in the terminal

1. Press Ctrl+Home.
2. Press Ctrl+F (Find; Navigation menu).
3. Type "first".
4. Press Enter.
    You hear: echo first.

### Read the keyboard shortcuts

1. Choose Keyboard shortcuts in the Help menu.
2. Press H.
    You hear: heading level 2.
3. Press Escape.

### Read the user guide

1. Choose User guide in the Help menu.
2. Press H.
    You hear: heading level 2.
3. Press Escape.

### Close the window while a command runs

1. Press Alt+1 (Command field; Navigation menu).
2. Type "ping -n 30 127.0.0.1".
3. Press Enter.
    You hear: Sent.
4. Press Alt+F4.
5. Press Escape.
6. Close the window.
7. Press Enter.

## Repeat an earlier command

Starts with: Open axTerminal with the folder myproject.

### Go back through earlier commands

1. Type "half".
2. Press Ctrl+Up.
    You hear: echo two.
3. Press Ctrl+Up.
    You hear: echo one.
4. Press Ctrl+Up.
    You hear the sound Edge.

### Come forward again to what was typed

1. Press Ctrl+Down.
    You hear: echo two.
2. Press Ctrl+Down.
    You hear: half.
3. Press Ctrl+Down.
    You hear the sound Edge.

### Read the field in navigation mode

1. Press Ctrl+Space.
    You hear: Navigation mode.
2. Press Ctrl+Up.
3. Press Enter.
    You hear the sound Edge.
4. Type "zz".
    You hear the sound Edge.

### Go back to edit mode in the field

1. Press Alt+1 (Command field; Navigation menu).
2. Press Ctrl+Space.
    You hear: Edit mode.

## Agree to the reminder before a shell first starts

Starts with: Open axTerminal with the folder myproject.

### Agree to the reminder and start the shell

You hear something with "Before axTerminal connects" in it.
1. Press Tab.
    You hear: I understand that axTerminal.
2. Press Enter.
    You hear: Check the box first.
3. Press Space.
    You hear: checked.
4. Press Space.
    You hear: not checked.
5. Press Space.
    You hear: checked.
6. Press Enter.
    You hear: Ready.

## Add a shell of your own

Starts with: Open axTerminal with the folder work\myproject.

### Add a shell of your own

1. Choose Add a shell in the Session menu.
    You hear the sound FormOpen.
    You hear: Add a shell, form.
2. Press F7 (What is running; Actions menu).
    You hear: A shell of your own: the name Switch shell and the New session notice show for it.
3. Press Enter.
    You hear the sound Edge.
    You hear: Type a name.
4. Type "Command Prompt".
5. Press Enter.
    You hear: A shell named Command Prompt is already there.
6. Press Ctrl+A.
7. Type "Test cmd".
8. Press Enter.
    You hear: Type the command line.
9. Type "cmd.exe /k".
10. Press Tab twice.
11. Press Shift+Tab twice.
12. Press Ctrl+Enter (Send the command).
    You hear: Added Test cmd.

### Switch to a shell of your own

1. Choose the menu item 1. Test cmd.
    You hear: Test cmd.

### Remove a shell of your own

1. Choose the menu item 1. Remove Test cmd.
    You hear: Removed Test cmd.
2. Close the window.

## Use axTerminal's Context menu

Starts with: Open axTerminal with the folder myproject.

### Bookmark a line and remove the bookmark

1. Choose Bookmark this line in the Context menu (key: Ctrl+K, or M in the terminal).
    You hear: Bookmark 1 added.
2. Choose Bookmark this line in the Context menu (key: Ctrl+K, or M in the terminal).
    You hear: Bookmark 1 removed.

### Copy a line into the command field

1. Choose Copy this line into the command field in the Context menu (key: Enter in the terminal).
    You hear: Line copied.
2. Press Ctrl+A.
3. Press Delete.

### Run a Context command from the palette

1. Press F1 (Command palette; Help menu).
2. Type "bookmark this".
    You hear: Bookmark this line, Context, Ctrl+K, or M in the terminal.
3. Press Enter.
    You hear: Bookmark 2 added.

## Start a shell in a recent folder from the command palette

Starts with: Open axTerminal with the folder work\orchard. Close the window. Open axTerminal with the folder work\cellar.

### Start a session in a recent folder from the command palette

1. Press F1 (Command palette; Help menu).
2. Type "orch".
    You hear something with "2. orchard" in it.
    You hear: Session, Open recent folder.
3. Press Enter.
    You hear: Folder orchard.
    You hear the sound Ready.

## Take a command another window hands over

Starts with: Open axTerminal with the folder myproject, handed the command Write-Output "It's two words; Write-Output more".

### Read a handed command in the field before sending it

You hear the sound Ready.
1. Press Alt+2 (Terminal; Navigation menu).

### Send the handed command yourself

1. Press Enter.
    You hear: Sent.
    You hear: Done.
2. Press Alt+3 (Latest output; Navigation menu).
    You hear: Output 1 heading level 1.
3. Press Down twice.

## Use a full-screen program

Starts with: Open axTerminal with the folder myproject.

### Open a full-screen program and type into it

1. Type "edit notes.txt".
2. Press Enter.
    You hear: Sent.
3. Press End.
4. Type "More".

### Read the screen and go back to the program

1. Press Ctrl+Space.
    You hear: Reading the screen.
2. Press Ctrl+Home.

### Find text on the screen

1. Press Ctrl+F (Find; Navigation menu).
2. Type "More".
3. Press Enter.
4. Press Ctrl+Space.
    You hear: Keys to edit.

### Hear a menu's selected entry

1. Press Alt+F.
    You hear: New File, Ctrl+N.
2. Press Escape.

### Leave the program and come back to the field

1. Press Ctrl+Q.
    You hear: [Save].
2. Press N.
    You hear: Done.
3. Press Alt+2 (Terminal; Navigation menu).

### Get the terminal back from a program that ends on the whole screen

1. Press Alt+1 (Command field; Navigation menu).
2. Type "[Console]::Write([char]27 + '[?1049h')".
3. Press Enter.
    You hear: Done.
