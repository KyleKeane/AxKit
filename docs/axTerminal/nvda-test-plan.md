# axTerminal NVDA test plan

The files for this plan are in `manual_tests/axTerminal`, made and reset by `.\manual_tests\prepare.ps1 axterminal`; its `_main.md` says which folder or file each section uses.

Manual checks with NVDA running, before a release that changes axTerminal (`docs/axTerminal/SPEC.md`). Each line is what to do and what must be heard.

## 1. Start (AT-1, AT-2)

- `axterminal C:\src\myproject` (or Start menu, axKit axTerminal): NVDA says the window ("myproject - axTerminal window"), then "Command, edit, multi line". After a moment: the ping and "Ready".
- Ctrl+2: "Terminal". The first lines: a system line "Shell started: Windows PowerShell in …" and, with NVDA running, PowerShell's line that it turned PSReadLine off. No prompt row anywhere. Ctrl+1: "Command".
- NVDA+End (the status bar): "Idle" and the prompt text "PS C:\src\myproject>".
- Ctrl+Q: "Idle, PS C:\src\myproject>".

## 2. Commands (AT-3, AT-5)

- Type `Write-Output hello`, Enter: "Sent" with its note; a tick; the chime and "Done". Ctrl+3: "Output 1 heading level 1"; Down: "hello". Shift+I: "Input 1 heading level 1"; Down: "## Folder"; Down: the folder; Down: "## Command"; Down: "Write-Output hello". `2`: "Folder heading level 2"; `2`: "Command heading level 2". Options → Text → Markers in the text off: Up and Down read "Input 1" and "Folder" without the hashes, `h` still says "heading level 1"; on again.
- `cmd /c exit 3`, Enter: "Failed, exit code 3". `Get-ChildItem`, Enter: ticks, "Done"; Ctrl+3 and read down: the listing, no prompt rows.
- `Read-Host "Continue? (y/n)"`, Enter: after two seconds of quiet, the question chime and "Asks: Continue? (y/n):". Type `y`, Enter: "Sent", then "Done"; the terminal shows the question row with the answer, then `# Input 2` with y.
- Type `if ($true) {`, Shift+Enter, `"yes"`, Shift+Enter, `}`, Enter: "Done"; Ctrl+3, Down: "yes"; no `>>` rows.
- Ctrl+Enter with `echo secret`: "Sent, not recorded"; no Input line for it; Ctrl+R does not list it. Ctrl+Enter with `Start-Sleep 5`, then NVDA+End: "Running: a command sent without recording"; Ctrl+Q says the same, not the command.
- Ctrl+R: "Recent commands, list" with the newest first; Down, Enter: the command is in the field ("Command" with the text); Ctrl+R, Escape: back in the field.
- In the terminal, on a command line, Enter: "Line copied", the focus in the field with the line.
- `Get-Content forged-marks.txt; Start-Sleep 5`, Enter (security audit S-37, S-38): the file's line is read, no "Done"
  and no question chime at once; NVDA+End: "Running: …" and the folder still `start-here`; "Done" only after the five
  seconds.
- `Start-Sleep 30`, Enter; NVDA+End: "Running: Start-Sleep 30"; Ctrl+Q: "Running: Start-Sleep 30, n seconds, …"; Ctrl+Shift+C: "Interrupt sent", then "Done" or "Failed". Shift+Escape: "Escape sent".
- Options, Speech, Speak output: all lines; `Get-ChildItem`, Enter: the lines spoken as one utterance, then "Done". Back to none.
- Options, Sounds, Background music while a command runs on; `Start-Sleep 5`: the music plays until "Done". Ctrl+Space: "Background music off".

## 3. Shells and folders (AT-1.2, AT-1.6)

- New line (bundle AX-7.8): Shift+Enter in the command field says "new line", Enter, which runs the command, does not; axDown's plan, step 2.5a.

- Ctrl+N: the notice "New session", its text, "Folder: …", Choose folder button, the Shell group with the radio buttons and Custom, Start session and Cancel. Choose Command Prompt, Enter: "New session in …"; the window empties; "Ready"; the title ends "- Command Prompt - axTerminal"; `echo hello`, Enter: "Done" (no code). Alt+S, W: the Switch shell submenu, Command Prompt checked; choose Windows PowerShell: "Windows PowerShell", a system line, "Ready", the transcript kept.
- Alt+S, W, G: "Git Bash", the status bar shows the prompt, ending in `$` (Git Bash's prompt is its whole PS1, "user@host MINGW64 /path $"), the title names the folder and Git Bash, nothing of the prompt in the terminal. `ls`, Enter: the listing, then "Done". `(exit 3)`, Enter: "Failed, exit code 3". `cd ..`, Enter: the title follows. Ctrl+Q: "Idle, Git Bash, $". Alt+S, W: back to the shell you had.
- `cd ..`, Enter: the title and the status bar name the parent folder. Ctrl+P: its full path. Ctrl+W: File Explorer opens on it. Alt+S, I, C: axClaude opens on it; Alt+W, O, 1: back.
- Ctrl+Shift+R while idle: "Restarting", a system line, "Ready", the transcript kept. While `Start-Sleep 30` runs: the notice "Restart the shell?"; Escape keeps it; again, Enter: restarts.
- Session, Open recent folder: the folders as "1. name (path)"; Enter on one: a new session there.
- `exit`, Enter: "The shell ended"; the system line; Enter in the field: "The shell ended. Ctrl+Shift+R starts it again"; Ctrl+Shift+R: "Ready".
- Shells of your own (AT-1.6a; script `08-user-shells`): Alt+S, W, A: the notice "Add a shell", the field "Type the shell's name"; Enter on it empty: "Type a name"; `Command Prompt`, Enter: "A shell named Command Prompt is already there"; Ctrl+A, `Test cmd`, Enter: the notice names Test cmd, the field "Type the command line that starts it"; `cmd.exe /k`, Enter: "Added Test cmd". Alt+S, W: "1. Test cmd (cmd.exe /k)" after Git Bash; Enter on it: "Test cmd", the title ends "- Test cmd - axTerminal"; `echo hi`, Enter: "hi" in the terminal. Ctrl+N: the Shell group has Test cmd selected, before Custom; Escape. Close and start axTerminal again: Test cmd starts. Alt+S, W, R: "1. Remove Test cmd"; Enter: "Removed Test cmd", then the default shell's name; the title ends "- PowerShell 7 - axTerminal" or "- Windows PowerShell - axTerminal"; Alt+S, W, R: "No shells of your own yet"; Ctrl+N, Tab to Custom: its command line is what it was. A name with an ampersand (`R&D`, line `cmd.exe /k`): Switch shell shows "R&D", the New session radio button reads "R&D".

## 4. Text, notices, the Window, axKit and Help menus (AT-4, AT-6)

- `cls`, Enter: "Done"; the lines before stay; `echo after`, Enter: "after" at the end. Ctrl+L: "Cleared"; the terminal is empty; the next command's lines come as the first.
- m on a line: "Bookmark 1 added"; k from the top: "Bookmark 1, …". Ctrl+F, text, Enter: the line; F3: the next; "Not found" otherwise.
- Ctrl+S: the file picker with a name `axterminal-<date>.md` in the current folder; save: "Exported"; the file holds the Input and Output lines as Markdown headings.
- F1: the shortcuts as a notice; h moves by its headings; Escape closes it. Help, User guide likewise.
- Alt+F4 while `Start-Sleep 30` runs: the notice "A command is still running"; Escape keeps it; Alt+F4, Enter: closes.
- After I accept all terms, install and restart (Help, Check for updates) while a command runs: the notice says the updated axTerminal starts again; Escape (Keep running): "Update installed. It starts with the next axTerminal", the command keeps running; the window closed later does not restart by itself.
- Alt+W: the Window menu as in every window; New window lists axTerminal among the access layers; Open windows lists the other axKit windows.

## The menu bar and Context (bundle AX-9.1, AX-9.11, 2026-11-11)

1. In the terminal on a line, Shift+F10: Context opens on "Copy this line into the command field" and NVDA reads it; Escape: back on the line. Alt, then Right along the bar: Session, Actions, Navigation, Context, Options, Window, axKit, Help.
