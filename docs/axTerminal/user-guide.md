# axTerminal user guide

**axTerminal, access layer for your shell.** axTerminal only shows what your shell prints and passes your commands to it; the shell, and every command you type, does the work, with your rights. Before axTerminal first starts each shell (PowerShell, Command Prompt, Git Bash, PowerShell 7, those that come with Windows included) it shows a short reminder with a check box: you confirm that axTerminal is only an access layer and that following the shell's terms is up to you. Help, About, Licence has these reminders and what you agreed to.

axTerminal is a terminal for a blind developer who uses a screen reader: a shell (PowerShell by default) runs in a hidden console, and everything it prints comes into a plain text window, one line at a time, the way axClaude shows Claude Code. You type a command in a field at the bottom, press Enter, and the command and what it printed stand in the text under their own Input and Output lines, so that the keys you know from axClaude find them again. When a command is done you hear it, and whether it failed. It is one of the apps of axKit and shares its installation, updates and the Window, axKit and Help menus with them.

## What you need

- Windows 10 version 1809 or later, or Windows 11.
- NVDA, or another screen reader that reads ordinary Windows text fields.
- A shell: Windows PowerShell is part of Windows; PowerShell 7 is used when it is installed; Command Prompt is always there.

## Install

axKit's installer installs axTerminal with the other apps: see the axKit guide (Alt+H, About, axKit guide) or the README that came with the download. It adds the Start menu entry `axKit axTerminal`, the console command `axTerminal` and axTerminal in the `Open with axKit` submenu on folders in File Explorer.

## Start

- Start menu: `axKit axTerminal`. Press the Windows key and type `axt`.
- Console: `axTerminal`, or `axterminal C:\src\myproject` to start the shell in that folder.
- File Explorer: right-click a folder, open `Open with axKit` and choose axTerminal.
- From axClaude: Session, Show current folder in, axTerminal. From axDown or axJSON: Session, Show current file in, axTerminal (the file's folder). From axGIT: Session, Show current repository in, axTerminal. From axFile: Session, Show current folder in, axTerminal. Each opens a new axTerminal window.

The window opens with the command field focused. When the shell's first prompt appears you hear "Ready" with a short ping. The status bar shows the state on the left (Idle, or Running with the command) and the shell's prompt on the right, which is where you are. The title is the name of the current folder and the shell. F8 says the current folder, the full path.

With a screen reader running, PowerShell turns its line editor (PSReadLine) off and prints a line saying so. That is expected: axTerminal's field is the editor.

## The window

From the top: the menu bar (Alt), the terminal (the text of everything so far), the command field (three lines), and the status bar.

- Alt+1 goes to the command field, Alt+2 to the terminal, Alt+3 to the latest output. Each says where you are when you are there already.
- Tab, Shift+Tab, Ctrl+Tab or F6 switch between the terminal and the field.

## Run a command

Type the command in the field and press Enter. You hear "Sent" with a low note. The terminal gets a `# Input 1` heading, `## Folder` with the folder the command runs in under it, `## Command` with your command under it, then `# Output 1`, and what the shell prints follows, with a short tick for each new line. When the prompt comes back you hear the falling chime and "Done", or "Failed, exit code 1" when the command failed (PowerShell says "Failed" for an error of its own; Command Prompt gives no code). With Speak output on, the word comes before the output: "Done. hello" for a quick command, and for one that takes longer, whose output you have been hearing as it came, "Done" and then what was left. Options, When a command is done, Announce "Done" turns the word off: a command that ends well then says its output alone, with the chime; a failure is always said. Alt+3 then takes you to the start of the output; read down with the arrow keys.

- Shift+Enter starts a new line in the field. A command of several lines goes to the shell line by line; for PowerShell one more Enter follows, which its console host wants to end a block.
- An empty field sends a plain Enter.
- Ctrl+Enter sends the command too, exactly as Enter does: it is the main action in every axKit window.
- Send without recording, in the Actions menu (Alt+A, then W) and in the command palette, sends the text without recording it: no Input and Output lines, not in the recent commands, not in the log. Use it for a passphrase or a token a program asks for. It has no key of its own.
- Alt+Down lists the recent commands of this window, newest first; Enter puts one back in the field, Escape goes back to the field. Ctrl+Up brings back the previous command and Ctrl+Down the next one, each with two quick rising notes; past the newest, Ctrl+Down gives back what you had typed, or the last command you changed and left. The plain arrows only move the caret.
- Enter on a line of the terminal copies that line into the field, to run it again or change it.
- Ctrl+I interrupts (it sends Ctrl+C to the shell). Shift+Escape sends Escape. The Actions menu's Send key sends Up, Down or Ctrl+D for a program that wants them.
- Escape in the field does nothing but remind you of those two keys.
- Ctrl+Space in the field: "Navigation mode", for reading a long command back without changing it. The arrows move and select; Ctrl+Up and Ctrl+Down go by paragraph instead of through the history; Enter sends nothing and plays the low edge sound, as Backspace and Delete do; a letter that moves gives a high tick first, one that finds nothing of its kind a low buzz, and a letter with no command a wooden bonk. Ctrl+Space again: "Edit mode". In the terminal, which you cannot type in, Ctrl+Space says "Read only: there is no edit mode here".

A program that asks a question (y/n, a passphrase) prints it as a line; when the output has been quiet for a couple of seconds and the last line ends like a question, you hear "Asks:" and the line, with the question chime. Type the answer in the field and press Enter. F7 says what is running, for how long, and the prompt, at any time. "Asks:" is always the program's own line, never axTerminal asking: a program, or a file you print, can word a line to look like any question, a password prompt included, so answer only what you know the program you ran asks.

### A command another window put into the field

An action of an axTask list can hand a command to axTerminal. A new axTerminal window opens in the action's folder with the command in the field, and the field itself carries the warning: where NVDA says "Command" for the field on other days, it now says "Command, put here by another program and not sent. axKit checked nothing of it: read it, and press Enter only if you want to run it", and then reads the command. You hear that every time you come back to the field, for as long as the line stands there as it came; once you change it, send it or delete it, the field is "Command" again. The conversation above holds the same words as a system line. Read the field (it has the focus), change the line if you like, and press Enter to run it, or close the window and nothing has happened. axTerminal never sends such a line by itself. Each part of the command is quoted for your shell, so a title with a quote or a semicolon in it stays one text. Where your shell is Git Bash or a shell of your own, for which axKit has no quoting rule, the field stays empty and you are told why.

## Read the terminal

The terminal is an ordinary read-only text: arrows, Home and End, Page Up and Page Down, Shift with them to select, Ctrl+C to copy ("Copied"). Nothing moves your caret while you read, and nothing is spoken on an arrow key. Single letters jump, Shift with the letter jumps back:

- i: the next Input line (a command you sent). o: the next Output line.
- h: the next heading (the Input and Output lines are headings). 1 to 6: a heading of that level.
- p: the next paragraph, the first line after a blank line. s: the next system line (the shell started, ended, a recording).
- m: bookmark the line (a "Bookmark 1" line appears above it); k: the next bookmark. Ctrl+K bookmarks from anywhere.
- l: the line number. Backspace: back to the line you jumped from.
- Ctrl+Alt with one of those letters reads every line of that kind from here to the end in one go; Control stops the reading.
- Ctrl+F finds text; F3 and Shift+F3 the next and previous match.
- Escape clears a selection, or goes to the field.

Options, Speech, Speak output: all lines, the default, reads the lines as they arrive, joined into one utterance for each third of a second. Speak output: none leaves only the ticks, and you read the output when you want.

## A full-screen program

Some programs take the whole screen: Windows' `edit`, an editor such as vim or nano, a pager such as `less`, a monitor. While one does, its screen stands in place of the terminal and the field, as one text called "Screen of edit" (the program's name), with the focus in it. Each row of the screen is a line of the text, drawn as the program drew it, its box and scroll bar characters too. When the program ends, or leaves its screen, the terminal and the field come back as they were, and the focus goes back to where it was. Nothing of the program's screen enters the terminal, its export or Find.

The text has two ways to be in, and Ctrl+Space, the key that switches between editing and navigating in every axKit window, turns between them. The status bar says which one you are in.

- Keys to the program (the status bar says "Keys to edit"): every key goes to the program, as a terminal sends it, except Ctrl+Space, F1 and Alt+F4. The caret stands where the program's cursor stands, so NVDA reads the line you are on as you move with the program's own keys. Alt alone still opens axTerminal's menu bar; Alt with a letter goes to the program (Alt+F opens edit's File menu).
- Reading the screen (the status bar says "Reading the screen"): the keys stay in the text. Read it with the arrows and NVDA's reading keys; nothing reaches the program, and axTerminal's own keys work again. Ctrl+Space goes back to keys to the program, with the caret on the program's cursor.

The switch is said in two words, "Reading the screen" or "Keys to edit". Nothing else is said when the program redraws, except one thing: where the program marks what is selected (an open menu's selected entry, a dialog's button that has the focus), that entry is said once, when it moves, "New File, Ctrl+N". Colours are not shown or said, and the mouse is not passed on.

While the screen shows, the command field and the terminal are behind it. In reading the screen, Alt+1, Alt+2 and Alt+3 leave the focus where it is and say so: "Screen of edit. The terminal and the command field come back when edit leaves the whole screen". Find (Ctrl+F), Find next (F3) and Find previous (Shift+F3) search the screen's rows, and the match is selected; chosen from the Navigation menu while the keys go to the program, Find says "Find works in reading the screen: Ctrl+Space turns to it".

A program asks the terminal for its colours as it starts (Windows' `edit` does); axTerminal answers as a terminal with a black background and light text, so the program starts at once. If a program ends without giving its screen back (it was killed, or it crashed), the terminal and the field come back when the shell's prompt does.

## The shell

Session, New session (Ctrl+N) opens a notice with the folder, a Choose folder button, the shells (PowerShell 7, Windows PowerShell, Command Prompt, Git Bash, or Custom with a command line of your own) and Start session. Starting ends the current shell and starts the new one in the folder; the terminal is cleared. Session, Switch shell lists the shells: choosing one starts it in the same folder and keeps the text. The title names the shell you are in, and F7 says it while nothing runs. Open recent folder does what New session does for a folder you had before. Restart shell (Ctrl+Shift+R) starts the same shell again in its current folder and keeps the text. While a command runs, all of them ask first.

Shells of your own: Session, Add a shell opens one form: type a name ("Type the shell's name"), Tab, and type the command line that starts it, as you would type it at a prompt ("Type the command line that starts it", for example `cmd.exe /k` or `pwsh -NoProfile`); Enter or Ctrl+Enter adds it, and what is missing is said with the focus in its field. It is then listed in Switch shell after the built-in shells, as "1. name (command line)", and in the New session notice before Custom; choosing it starts it as a custom shell starts, under its own name in the title. Switch shell, Remove a shell lists them as "1. Remove name" and takes the one you choose off the list; if it is the shell you are in, your default shell (PowerShell 7 when it is installed, otherwise Windows PowerShell) starts in its place in the same folder, after a question while a command runs. The command line under Custom is never changed. Like a custom shell, a shell of your own gives no Ready or Done unless its prompt prints axTerminal's marks.

The shell's prompt is not shown in the terminal: it is in the status bar, and the folder it names follows your `cd`. The prompt rows are recognised through marks the shell prints for terminals (the Windows Terminal convention), which axTerminal adds to PowerShell's prompt, to Command Prompt's PROMPT and to Git Bash's prompt command for the session; your profile is not touched. A shell of your own gives no marks unless its prompt prints them, and then the window shows every row and says nothing when a command ends.

`exit` ends the shell: a system line says so, and Ctrl+Shift+R starts it again. Closing the window ends the shell and whatever runs in it, after a question while a command runs.

Clear transcript (Ctrl+Shift+Delete, the key that clears a history in every axKit window) empties the window; the shell's own `cls` clears only its screen.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Add a shell; Clear transcript (Ctrl+Shift+Delete); Custom shell; Exit (Alt+F4); Export transcript (Ctrl+S); New session (Ctrl+N); Restart shell (Ctrl+Shift+R); Say current folder (F8); Show current folder in (axClaude, access layer for Claude Code, axFile, axGIT, access layer for Git, File Explorer).
- **Actions**: Interrupt (Ctrl+I); Recent commands (Alt+Down); Send Escape (Shift+Escape); Send without recording; What is running (F7); Send key (Ctrl+D (end of input), Down, Up).
- **Navigation**: Command field (Alt+1); Find (Ctrl+F); Find next (F3); Find previous (Shift+F3); Latest output (Alt+3); Next bookmark (K in the terminal); Previous bookmark (Shift+K in the terminal); Terminal (Alt+2).
- **Context**: Bookmark this line (Ctrl+K, or M in the terminal); Copy the selection (Ctrl+C in the terminal); Copy this line into the command field (Enter in the terminal).
- **Options**: Check for updates at start; Edit app keys file; Keep a log of events; Open the sounds folder; Developer (Open settings file, Record raw stream for a bug report); Sounds (Background music while a command runs (Ctrl+M), Chimes: ready, sent, done, question, Ticks for new lines); Speech (Say when a program asks, Speak output: all lines, Speak output: none); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Markers in the text, Say new line, Smaller text (Ctrl+Minus), Sound on headings, Time stamps on Input and Output lines, Windows text size); Theme (Edit theme file, Reload theme, Theme on); When a command is done (Announce "Done", Flash the taskbar button, Louder alert when the window is in the background).
- Keys without a menu item: Ctrl+M (Background music on or off); Ctrl+Enter (Send the command); Tab (Switch between the view and the field).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1, the command palette, lists every key and command. Options ends with the settings of all of axKit.
<!-- end of the menus -->

## Settings

Every option in the menus is saved as you change it, in `%APPDATA%\AxKit\axTerminal.json`, which Options, Developer, Open settings file opens in axJSON. The console is 240 columns by 50 rows; `ptyColumns` and `ptyRows` in the file change it.

The shells in that file (`shell`, `customShell`, `userShells`) are sealed: they decide what axTerminal starts, so they count only as axTerminal wrote them. Change them in the menus (Session, New session, Switch shell, Add a shell, Remove a shell). If you change one in the file by hand, or another program does, axTerminal starts your default shell, uses none of them, keeps your other settings, and says once, as a system line: "Your shells in axTerminal's settings were changed outside axKit and are not used. Add them again from Session, Add a shell." The file as it was found is kept beside it, with `.changed` at the end of its name (`axTerminal.json.changed`). The seal's key is kept by Windows for your account; a program running as you could ask Windows for it, so the seal protects against a file changed by another program or by hand, a profile copied or synced from another computer, and an AI tool writing files, and not against a program that sets out to forge it.

Before a custom shell or a shell of your own first starts, axTerminal asks once for that command line; a different command line asks again, and the system line of each start names the command line.

## Updates

Alt+H, Check for updates asks GitHub for a newer axKit; Update to installs it and starts axTerminal again with a new shell in the same folder. Options, Check for updates at start turns the check at start off, for every app.

## If something goes wrong

- "PowerShell 7 was not found": Ctrl+N and choose Windows PowerShell or Command Prompt, or install PowerShell 7.
- Nothing is spoken when a command ends: the shell gives no marks (a custom shell, or a shell started inside the shell). F7 says what the window knows.
- A program took the whole screen and will not let go: Ctrl+Space turns to reading the screen, where axTerminal's menus and keys work; Actions, Interrupt sends Ctrl+C, and once the shell's prompt is back the terminal and the field come back too.
- Options, Developer, Record raw stream writes everything the shell prints to a file, for a bug report; Help, Diagnostics, Copy diagnostics copies the state and the last log lines. The log is `%LOCALAPPDATA%\AxKit\logs\axTerminal.log`; it never holds a command or a line of output.

## About axTerminal

axTerminal is part of axKit, made by Dr. Kyle Keane, www.kylekeane.com, free under the GNU AGPL 3 licence. Alt+H, About has the version, the licence and the axKit guide.

## Disclaimer

The software is provided "as is" and "as available", without warranty of any kind, express or implied. The developer has no obligation to provide support, maintenance, updates or corrections, and, to the fullest extent permitted by law, is not liable for any claim, damages or other liability arising from or in connection with the software or its use, including loss of data or work and anything a command run through it does. You use axTerminal at your own risk.
