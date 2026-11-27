# axTerminal — Specification

axTerminal is an app of the axKit bundle (`docs/axKit/SPEC.md`): a terminal for a shell, shown the way axClaude shows
Claude Code, for a blind developer who uses the NVDA screen reader. Specified and built 2026-10-18, released in
3.0.0, and the whole test plan passed under NVDA on 2026-10-22 (the record: `docs/axKit/nvda-test-record.md`; what
each release brought is section 8). The document is kept in step with the code (a behaviour change updates it in
the same commit). Requirements are numbered `AT-n.m` and decisions `AT-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Window, axKit and Help menus), and axClaude's requirements (`docs/axClaude/SPEC.md`, cited as
"axClaude FR-n.m" and "axClaude Dn") are cited by number and not repeated. In axTerminal's code and documents,
"SPEC.md" means this file.

## 1. Summary

An **access layer** for the user's shells (bundle AX-11, 2026-10-29): titled "axTerminal, access layer for your shell"; before each shell's first start (Windows PowerShell, Command Prompt, Git Bash, PowerShell 7, a custom shell), the consent notice for that shell (AX-11.4; Not now leaves "Not connected to <shell>. Press Ctrl+N to start a shell.").

axClaude turned Claude Code's console into a window a screen reader reads well: what the console prints becomes a
line-by-line transcript in a read-only text view, the user types in a plain field at the bottom, each exchange gets
its markers, and the end of a turn is spoken. axTerminal is the same window around a shell. The shell (PowerShell by
default) runs in a Windows pseudo console (ConPTY); every row it prints is a line of the transcript; a command typed in
the field goes to the shell on Enter and gets an exchange block, `# Input n` with the command and `# Output n` before
what it printed, so that `i`, `o`, Alt+3 and the heading keys work as they do in axClaude; the shell's prompt rows
stay out of the transcript and show in the status bar; and the shell's integration marks tell the window when a
command is done and with what exit code, which is spoken with the chimes the user knows.

It is not a terminal emulator with a screen: a program that takes the screen over (an editor, a pager, a monitor)
gets its page shown in place of the transcript while it holds the screen, and nothing of it enters the transcript
(AT-9, built 2026-11-25); programs that print and ask are the transcript's. It works entirely offline (bundle G6).

## 2. Goals and non-goals

- **G1 axClaude's window, learnt once.** The same field, view, quick keys, markers, chimes, ticks and speech options;
  a user of axClaude knows axTerminal on opening it.
- **G2 A command and its output as one block.** Every command is found again with `i`, its output with `o` and Alt+3,
  read down with the arrows, exported as Markdown.
- **G3 Prompts answered in place.** A program that asks (`y/n`, a passphrase, a choice) shows its question as lines;
  the field answers it; the window says that something asks.
- **G4 Shared, not copied.** The ConPTY host, the parser, the screen model, the transcript mirror and the view move
  from axClaude into the shared projects on this, their second use (bundle AX-6.3, B-3); axTerminal adds the shell's
  own model and nothing twice.
- **Non-goals.** Colours, mouse reporting, tabs and panes; the shell's own line editing, history recall and Tab
  completion inside the field (AT-D5, §7); running Claude Code (axClaude's job, with its screen reader mode);
  elevation (start an elevated terminal from Windows). A program that redraws the whole screen (edit, vim, nano,
  less, htop, a TUI over ssh) is shown as its page by the screen view (AT-9, built 2026-11-25), which reads it and
  sends it keys; it is not a terminal emulator with colours, a mouse or a scroll back of the program's screen.

## 3. Scenarios

1. In axClaude on a project, Alt+S, I, T: axTerminal opens on the project folder with the field focused; the status
   bar reads `PS C:\src\myproject>`. Type `dotnet test`, Enter: "Sent", a tick per frame of output, then the done
   chime and "Done". Alt+3 lands on `# Output 1`; Down reads the output line by line.
2. A command fails: the chime and "Failed, exit code 1". Alt+3, then Ctrl+End and Up for the last lines.
3. `git push` asks for a passphrase: after a moment of quiet, the question chime and "Asks: Enter passphrase for
   key". Actions, Send without recording sends the field's text without recording it; "Sent, not recorded". The push goes on; "Done".
4. `npm start` runs on: lines arrive with their ticks; Ctrl+I: "Interrupt sent"; the prompt returns, "Done".
5. Alt+Down: the window's recent commands as a list, the newest first; Enter puts `dotnet test` back in the field, Enter
   again runs it. Or `i` in the view finds `# Input 1`, `2` twice finds `## Command`, Down, and Enter on the command line copies it into the field.
6. `cd ..`: the title and the status bar follow the shell's directory. Alt+S, I, F: axFile on that folder; Alt+S, I,
   G: axGIT on its repository.
7. In axDown on `notes.md`, Alt+F, I, T: a terminal in the file's folder.

## 4. Functional requirements

### AT-1 Launch and the shell

- AT-1.1 `AxKit.exe terminal [<folder>]`; the console command `axTerminal`; the Start menu entry "axKit axTerminal";
  axTerminal under "Open with axKit" on folders in File Explorer (AX-2.3). A folder given alone to `AxKit.exe` still
  means axClaude (AX-1.2); axTerminal is always named by its verb. Without a folder: the process's current folder
  (the Start menu entry starts it in the user's profile folder, as the shells do). The window itself takes the last
  folder of the settings first when it is there; one that is not on a drive of this computer is not looked at
  during the start (bundle AX-9.5a): the shell starts in the current folder, and a system line says "<path> is on
  the network and was not opened at start; Open recent opens it."
- AT-1.2a **Only the shells there are** (the user's ask, 2026-10-24): the New session notice and Switch shell list the
  shells this computer has; Windows always has Windows PowerShell and Command Prompt. PowerShell 7 and Git Bash, when
  missing, are not listed; Session offers "Install PowerShell 7…" and "Install Git, for Git Bash…" after Switch shell instead
  (bundle AX-2.9), and switches to the shell once it is installed.
- AT-1.2 **The shell.** One of PowerShell 7 (`pwsh.exe`, found under `PowerShell\7` in Program Files or Program Files (x86), which only an
  administrator can write, and only when it is not there in the PATH's folders that are on a drive of this computer,
  `ShellLauncher.Find`, `SystemPrograms.Installed`; bundle AX-11.5 and `repairs.md` R-70, 2026-11-16: the agreement
  and the start line name PowerShell 7, not its path, so a `pwsh.exe` put earlier in the PATH or on a share must not
  be what starts; until then the PATH came first),
  Windows PowerShell (`powershell.exe`), Command Prompt (`cmd.exe` from System32 by its full path, never the `ComSpec`
  variable, which a program or a profile can point at another program; bundle security audit S-1, the user's choice of
  2026-11-09), Git Bash (Git for Windows' `Git\bin\bash.exe`
  under Program Files, Program Files (x86) or `%LOCALAPPDATA%\Programs`, or next to the `git.exe` on the PATH; never
  a `bash.exe` on the PATH, which in System32 is WSL's launcher; started `--login -i`, as Git's own shortcut does) or
  a custom command line, or one of the user's own shells (AT-1.6a); the default is PowerShell 7 when it is installed,
  otherwise Windows PowerShell. The choice is made in the New session notice and the Switch shell submenu (AT-1.6)
  and remembered in the settings (`shell`, `customShell`, `userShellName`) for the next window. The shell starts in the folder with the
  window's environment plus `TERM=xterm-256color` for the programs that look, and `GIT_PAGER=cat` and `GH_PAGER=cat`,
  which switch git's and the GitHub CLI's pager off (AT-D7); nothing is taken out of the environment. The shell also
  inherits Windows' switch `NoDefaultCurrentDirectoryInExePath`, which axKit sets for itself at start (bundle security
  audit S-1, the user's choice of 2026-11-09): in Command Prompt a program in the current folder is run as `.\name`, as
  PowerShell has always asked, and a `git.exe` planted in a downloaded repository never answers to `git`.
- AT-1.3 **Integration marks.** The window asks the shell to mark its prompt the way Windows Terminal's shell
  integration does (OSC 133 `A` the prompt starts, `B` the prompt ends and the command begins, `D;<exit code>` the
  previous command finished; OSC 9;9 the current directory), without touching the user's profile. Every mark ends in
  `;axkit=<secret>`, a random secret made for each start of the shell (`ShellLauncher.NewNonce`, the model's
  `MarkNonce`): a mark without it, printed by a program or found in a file's text, is ignored, and the directory
  counts only right after the prompt's own A mark and only for a folder on this computer (security audit S-37, S-38;
  VS Code's terminal does the same with its own marks). The shells are started so: PowerShell starts
  with `-NoLogo -NoExit -EncodedCommand` and a snippet that runs after the profile, keeps the user's `prompt` function and wraps its
  text in the marks (the shape Windows Terminal documents for oh-my-posh), written with `[char]27` so that Windows
  PowerShell takes it too, and sets `$ProgressPreference` to `SilentlyContinue`, since a progress bar drawn over the
  top rows of the screen would rewrite lines already read; Command Prompt starts with the `PROMPT` variable wrapping
  the current `%PROMPT%` in the same marks (cmd has no exit code to give); Git Bash starts with a `PROMPT_COMMAND`
  in its environment that prints `D` with the exit code (from the second prompt on, the first following no
  command), `A` and the directory as a Windows path, and appends `B` to `PS1` once the profile has set it, so the
  user's prompt text stays (a profile that sets `PROMPT_COMMAND` itself replaces it, and the shell then runs
  without marks, as a custom one). The parser reports the marks as events (an
  addition in the shared parser; axClaude, whose Claude emits 133 marks too, goes on ignoring them). A custom shell
  gets no snippet and no marks unless its own prompt emits them; a shell started inside the shell (`pwsh` typed) has
  none until it exits. With a screen reader running, PowerShell turns PSReadLine off at startup and prints a warning
  line saying so, the first line of the transcript: the input is then the console host's own, with `>> ` continuation
  rows printed by the host, which is what the window wants (the recorded fixtures show it).
- AT-1.4 **State from the marks.** One state (`TerminalModel.State`): no marks, idle, or running. The first `A`
  mark makes the shell idle and announces Ready (AT-5.1); from Enter until the next `A` mark the window counts the
  command as running: the status bar says `Running: <the command's first line>` and the music plays when it is on
  (AT-5.5); the `D` mark's code decides between "Done" and "Failed, exit code n" (AT-5.1); the `A` mark's row is the
  prompt (AT-4.2); the 9;9 directory and the shell's name make the title. Without marks the window has no state: the
  status's left label stays empty, nothing is announced at the end of a command, the cursor row shows as a line, and
  a command sent before the first mark (a shell still starting) is not counted as running, so the first prompt is
  still Ready and not Done.
- AT-1.5 `AxTerminalApp.Run(args)` is the entry point (AX-1.7), one call into `AppHost.Run` with the log name
  `axTerminal`, the usage text, the settings (`axTerminal.json`) and the window.
- AT-1.5a **A command handed over** (2026-11-21; docs/axTask/forms-framework.md AF-4.5, AF-7.4).
  `terminal [<folder>] --command <program> [<argument> ...]`: everything after `--command` is one command, its
  parts as the process's own arguments, one each, whatever they look like; the one folder stands before the option
  (`HandedCommand.Parse`). The window makes one line of them for the shell it starts, each part quoted by that
  shell's rule so that it stays one part (`HandedCommand.Line` over the bundle's `ShellQuote`: PowerShell, single
  quotes with a single quote doubled; Command Prompt, a part with a character cmd.exe acts on refused; Git Bash, a
  custom shell and a shell of the user's own have no rule; a line break in a part refused for every shell), puts
  the line into the command field with the caret after it, and sends nothing: Enter is the user's. **The caution
  is the field's own name** while the line stands in it as it came (2026-11-22, `HandedCommand.FieldName`):
  "Command, put here by another program and not sent. axKit checked nothing of it: read it, and press Enter only
  if you want to run it", in the place of "Command". NVDA reads a field's name before its text whenever the focus
  arrives there, so the warning is heard before the command, at the start and on every return to the field, and
  cannot be lost. Until that day the words were an announcement as the window showed, and NVDA's own reading of
  the opening window could cut it off: heard once in four runs by the NVDA pass of 2026-11-22, where the command
  was then read with no warning (root rule 11: such a caution is heard every time). Changed by the user, replaced
  by a recalled command, sent or deleted, the text is the user's own and the field is "Command" again; undone
  back to the line as it came, it has its caution again. The transcript holds, as a system line to read again, "A
  command is in the command field, not sent. Another program put it there, and axKit checked nothing of it: read
  it, and press Enter only if you want to run it"; where no line could be made, "A command was handed to this
  window and is not in the command field:" and why, said once the window shows, and the field is empty. The
  start line of the log holds the number of parts, never the parts (`CommandLineText.ForLog`), since they may hold
  an item's values. axTask's `terminal` actions are what hands a command over. Tested in `HandedCommandTests`,
  `CommandLineTextTests`, `tests/scripts/axTerminal/13-handed-command.txt` and, for what NVDA speaks,
  `tests/nvda-scripts/axTerminal-handed-command.txt`.
- AT-1.6 Session → **New session…** (Ctrl+N), the shape and the name of axClaude's New session (axClaude FR-8.6,
  D30): the notice "New session" with `Folder: C:\path` as its first line, "Choose the shell and the folder. Starting
  ends the current shell and starts a new session in the folder." and the key line, preceded by "<the command> is
  still running. A new session ends it." while one runs; the **Choose folder…** button (the folder picker); the group
  **Shell** with the radio buttons **PowerShell 7**, **Windows PowerShell**, **Command Prompt**, **Git Bash** and
  **Custom**, and, while Custom is selected, the field **Command line**; the buttons **Start session** (Enter) and
  **Cancel** (Escape). Start clears the transcript and gives the new shell a fresh screen, as a restart does (until
  2026-10-23 it drew on the old shell's rows), prints the system line `New session: PowerShell 7 in C:\path`,
  announces "New session in <folder name>" and remembers the folder and the shell. Session → **Switch shell ▸** lists
  the shells (PowerShell 7, Windows PowerShell, Command Prompt, Git Bash, the current one checked, then Custom…,
  which opens the New session notice): choosing one ends the shell and starts the chosen one in the current
  directory, keeping the transcript, with the system line `Switched to Git Bash in C:\path` and the shell's name
  announced; while a command runs, the notice "Switch the shell?" comes first ("Enter: Switch now. Escape: Keep it
  running."). The title names the shell (AT-2.3) and F7 says it while idle (AT-D8), so the shell in hand is
  always one key away. Session → **Restart shell** (Ctrl+Shift+R) ends the shell and starts the same
  one again in its current directory, keeping the transcript, after the notice "Restart the shell?" while a command
  runs ("Enter: Restart now. Escape: Keep it running."); a shell that ended restarts at once. "Restarting" is
  announced. A command still running when a restart, a new session or a switch goes ahead is terminated at once
  with its shell, since the notice said so; an idle shell gets the two seconds of its console closing (axClaude
  FR-1.7; 2026-10-23).
- AT-1.6a **The user's own shells** (2026-11-11; the shape of axFile's own programs, AF-4.11, docs/axKit/plan.md): a
  list in the settings (`userShells`, each `{"name", "commandLine"}`, in the order added; as the file is read the name
  and the line are trimmed, one without either is dropped, and a name taken twice, case ignored, is kept the first
  time), beside the one custom command line. Session → Switch shell ▸ lists them after the built-in shells as "1. name
  (command line)" (AX-8.5), the one in use checked, and holds **Add a shell…** and **Remove a shell ▸** after Custom…;
  the number is the item's mnemonic only while it is one digit no fixed item of the submenu has (PowerShell &7 keeps
  7), and from the tenth on there is none, so that no two items share one (AX-6; `ShellMenuLabels`, tested); an
  ampersand in a name is shown as one. The New session notice lists them as radio buttons after the
  built-in shells and before Custom, the one in use selected. **Add a shell…** opens one form (bundle AX-8.10, since 2026-11-14; two notices, one
  after the other, before), "Add a shell", with the fields "Type the shell's name" and "Type the command line that
  starts it" (as typed at a prompt, for example cmd.exe /k or pwsh -NoProfile) and the buttons **Add** (Enter or
  Ctrl+Enter) and **Cancel** (Escape); Tab moves between them. A blank name says "Type a name", a name a built-in
  shell, Custom ("Custom" or "Custom shell") or one of the user's already has (case ignored) says "A shell named
  <name> is already there", a blank command line says "Type the command line", each with the edge sound and the focus
  in that field, and the form stays; Add keeps the shell and says "Added
  <name>"; nothing is started. **Remove a shell ▸** lists them as "1. Remove name" ("No shells of your own yet" when
  there are none; the word Remove keeps their names in the command palette apart from Switch shell's); one chosen is
  taken off the list and "Removed <name>" said. When it is the shell in use, the machine's default built-in shell
  (AT-1.2: PowerShell 7 when installed, otherwise Windows PowerShell) starts in its place in the same folder, the
  transcript kept, as Switch shell does, after the notice "Remove the shell?" (**Remove now**, **Keep it**, which keeps
  the shell and the list) while a command runs; the settings' `shell` falls back to the default. The custom command
  line (`customShell`) is never changed by a removal (2026-11-11, the review: the first draft made the removed shell
  the custom shell, overwriting the line the user had typed under Custom; `TerminalSettings.RemoveUserShell`, tested).
  A user's shell starts as the custom shell
  does (no snippet, no marks unless its prompt prints them, a reminder of its own, AT-1.6b), under its own
  name in the title, the system lines (`Switched to <name> in …`, `Shell started: <name>, command line <line>, in …`),
  F7 and the announcement of Switch shell; the name in use is remembered (`userShellName`) for the next window.
- AT-1.6b **A command line of the user's own is agreed to as that command line** (2026-11-16, `repairs.md` R-53;
  bundle AX-11.5). The reminder before the custom shell or one of the user's shells first starts is for the name
  with the command line (`ConsentTarget.CommandLineShell`, the target "<name>, command line <line>"; the notice says
  "<name> is a shell of your own, started with the command line <line>"): another command line, under Custom or
  under the same name, asks again, and so does every custom or user's shell agreed to in 5.9.0 or earlier, once, since that
  agreement was for the name "Custom shell" alone. The system line of the start names the command line (AT-4.3).
  The command lines themselves are under the settings' seal (AT-8.1a).
- AT-1.7 **The shell ends** (`exit` typed, a crash): the system line `The shell ended with code 0`, the announcement
  "The shell ended", the status `Shell ended`; Enter in the field then keeps its text and announces "The shell ended.
  Ctrl+Shift+R starts it again" (axClaude FR-2.1). Closing the window ends the shell and whatever runs in it; while a
  command runs the notice "A command is still running" asks first, with **Close anyway** (Enter) and **Keep running**
  (Escape), as axClaude asks for Claude (axClaude FR-1.7); Close anyway terminates the command at once. After Update
  and restart (bundle AX-3.5) the notice adds that the updated axTerminal starts again, and Keep running keeps the
  window on the old program file and says "Update installed. It starts with the next axTerminal", as axClaude's Keep
  working does (until 2026-11-09 it dropped the restart without a word).
- AT-1.8 The console is 240 columns by 50 rows (`ptyColumns`, `ptyRows`; axClaude D9), so that programs wrap little;
  the window never resizes it.

### AT-2 The window

- AT-2.1 From the top: the menu bar; the transcript, a read-only word-wrapped text named "Terminal", the same view as
  axClaude's conversation (`TranscriptView`, axClaude FR-3.1); the command field "Command", three rows of its font high
  and never more than a third of the window, framed, with a tinted background of its own and under a header band that
  shows its name on screen, in the place of axClaude's message field (axClaude §6.1); the status bar. A notice takes the place of the view and the field (AX-6.4); the
  recent commands list (AT-3.6) takes the field's place while it shows.
- AT-2.2 Focus: Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 toggle between the view and the bottom control
  (bundle AX-8.2); Alt+1 the field, Alt+2 the view, Alt+3 the view at the latest output marker, from anywhere (Ctrl+1, Ctrl+2 and Ctrl+3, the keys until 2026-11-16, stayed as further keys until 2026-11-26, bundle AX-8.14), each
  naming the control when it has the focus already (axClaude FR-6.5, D26); while a full-screen program's screen view
  shows, they say why they wait instead (AT-9.10). The focus starts in the field and stays
  there after sending.
- AT-2.3 The title is `<directory's name>, axTerminal, <current directory's full path>, <the shell's name>`
  (`work, axTerminal, C:\work, PowerShell`; the maintainer's word of 2026-11-26, bundle AX-9.6; `axTerminal, access
  layer for your shell folder <full path> <shell>` from 2026-11-23, `<directory's name> - <shell> - axTerminal`
  before), the directory being the shell's (9;9) and the start
  folder before the first mark. The status bar's left label: `Idle`, `Running: dotnet test`, `Shell ended`,
  `Starting`; the right label: the shell's last prompt, the row's text between the `A` and `B` marks
  (`PS C:\src\myproject>`), or the full directory before the first prompt. No AccessibleName, no Spring (axClaude
  §6.2). F7 (Ctrl+Q until 2026-11-26), from anywhere, says both with the time since the command started: "Running: dotnet test, 40 seconds,
  PS C:\src\myproject>" or "Idle, Windows PowerShell, PS C:\src\myproject>" (AT-D8), the shell named while idle.

### AT-3 The command field and the sending

- AT-3.1 **Enter** sends the field's text and clears it: each line as one write, a single `\r` as a separate write
  after it, paced as axClaude's writes are (axClaude FR-2.2, FR-2.5), 60 ms after each line and 120 ms after each
  `\r`, so that the shell reads a multi-line command line by line as it would from the keyboard, PowerShell's `>>` continuation taking
  the rest; for PowerShell a command of several lines gets one more Enter at the end, since with a screen reader
  running PowerShell turns PSReadLine off and its console host ends a block only on an empty line (with PSReadLine on
  the extra Enter is an empty prompt, which announces nothing). **Shift+Enter** is a new line in the field (axClaude
  D11). An empty field sends a bare `\r` (a new prompt, or the default of a program's question). "Sent" is announced
  with the sent note (AT-5.1).
- AT-3.2 **The exchange block** (axClaude FR-4.1): `# Input n`, then `## Folder` with the directory the command runs
  in on the line under it (the shell's current directory at the moment of sending: the 9;9 mark's, or the start
  folder while a shell gives no marks; left out when neither is known; AT-D9), then `## Command` with the command's
  lines as sent under it, a blank line, `# Output n`, a blank line, inserted at the end of the transcript above the
  trailing prompt row at the moment of sending; the `##` lines are level 2 headings, so `h` and `2` find them, and
  a jump speaks a heading without its marker, "Input 1 heading level 1" (axClaude §6.4); *n* counts since the window started or the transcript was cleared; both markers are level 1 headings;
  Options → Text → Time stamps adds the time to both (axClaude FR-4.6), and Options → Text → Markers in the text (axClaude FR-4.6a) shows or hides the `#` markers of every heading line in the view, the lines, the keys and the export unchanged. The shell's echo of the command, the prompt
  row holding the command after its prompt, stays hidden: the block stands for it (axClaude D22), and so do the rows
  a command longer than the console is wide wraps onto (the rows directly under the prompt row that hold the
  command's own text at that place; they were spoken after "Sent" and stood in the Output block until 2026-11-14;
  a wrapped continuation row of a multi-line command is not covered), and the
  continuation rows of a multi-line command, the shell's continuation prompt (PowerShell's `>> `, bash's `> `, cmd's
  `More? `; a custom shell has none known, and its rows all stay) followed by one of the command's lines, or by
  nothing (the host's empty continuation after the last line), directly under the prompt rows while the command runs;
  an output line that merely starts with the continuation prompt (`echo '>> x'`) is none of the command's lines and
  stays (AT-4.2). Every Enter is an exchange, also one that answers a running program (AT-D3).
- AT-3.3 **No send lock** (axClaude D12): while a command runs, the text goes at once as input to the running program
  (an answer, or what the shell reads next), with its block; the status keeps the running command's name.
- AT-3.4 **Send without recording** (Actions → Send without recording, and the command palette; no key since
  2026-11-14, when Ctrl+Enter became the bundle's main action, AX-8.10 rule 3, and in the command field sends the
  command exactly as Enter does): the text goes as on
  Enter, with no block, no entry in the recent commands and nothing in the log; "Sent, not recorded"; while it runs,
  the status bar and F7 say "a command sent without recording" in the command's place (until 2026-10-23 the
  command itself stood there). For passphrases and tokens that a program asks for (AT-D3).
- AT-3.5 **Keys to the shell.** Ctrl+I (Ctrl+Shift+C until 2026-11-26) sends Ctrl+C, the interrupt ("Interrupt sent"; Shell → Interrupt);
  Shift+Escape sends Escape ("Escape sent"); Shell → Send key ▸ Up, Down, Ctrl+D, from the menu only. Plain Escape in
  the field sends nothing and announces "Shift+Escape sends Escape, Ctrl+I interrupts" (axClaude D20). Nothing
  else typed reaches the shell before Enter.
- AT-3.6 **Recent commands** (Alt+Down, Ctrl+R until 2026-11-26; Shell → Recent commands…): the window's last fifty commands, the newest first,
  as a list in the field's place (`ControlArea`, axClaude D33, named "Recent commands"); Enter puts the chosen one in
  the field and focuses it, Escape brings the field back; "No recent commands" when there are none. Ctrl+Up and Ctrl+Down
  in the field go through the same list (AT-3.9). The list is the window's own and is not persisted (§7).
- AT-3.7 **Enter on a line of the view** copies the line's text into the field, after a new line when the field is
  not empty, moves the focus there with the caret at the end and announces "Line copied"; no quote markers (AT-D4).
  A joined row copies as one line; a marker line copies its text like any other.
- AT-3.8 Page Up, Page Down, Ctrl+Up and Ctrl+Down, with Shift, work in the field as in every edit control of the
  bundle (AX-8.2).
- AT-3.9 **Ctrl+Up and Ctrl+Down in the field** (asked 2026-10-23): the recent list (AT-3.6) in the field, Ctrl+Up the
  command before, the newest first, and Ctrl+Down the command after; past the newest, the last item, the draft: what
  the user typed, or the last command the user changed and left, so that Ctrl+Down to the end always gets an edit
  back (a command in the list never changes). Each move plays the recall earcon (two quick rising notes) and says the
  command once ("blank" for an empty draft), the caret at its end; at either end the edge earcon (AX-10.4). A send
  empties the draft and starts again from the newest. Plain Up and Down are the caret's, so a command of several lines
  is edited line by line. Ctrl+Up and Ctrl+Down take the place of the bundle's paragraph keys in this field only
  (AX-8.4's overrides); Shift with them still selects by paragraph. The first draft the same day had Up on the first
  line and Down on the last, as PowerShell's line editor does; the user found it too subtle (where an edit went, which
  line one was on) and chose this.

### AT-4 The transcript

- AT-4.1 Every row the shell prints is a line, trimmed on the right, committed once it scrolls off the screen and
  rewritable until then (axClaude FR-3.2, §7.5); the classification is the shell's own (AT-4.2) and nothing of
  Claude's: no labels, no headings but the markers, no chrome patterns. A row that fills all 240 columns and is
  followed by a row with text is a hard wrap and is joined to it in the view (`joinWrappedLines`, axClaude FR-3.2a).
  Every row with text is transcript, the rows below the cursor included (PSReadLine, which draws its completion
  menu there, is off while a screen reader runs).
- AT-4.2 **Prompt rows are hidden.** The row that holds an `A` mark is the prompt row: hidden while the shell is idle,
  when it is the cursor row with the prompt on it; hidden still once it holds the command after its `B` mark, the echo
  the block stands for (AT-3.2). Its text between the marks goes to the status bar (AT-2.3). A `D` mark comes before
  the prompt on the same row and is nothing by itself. Without marks nothing is hidden (AT-1.4). The rows are kept by
  their lines, which the screen binds to its rows: at the `A` mark the cursor row's line is made and remembered, at
  the `B` mark the rows from that line's row (wherever a scroll in between has moved it) to the cursor row are the
  prompt, and the continuation rows of the command (AT-3.2) are looked for under the last prompt line's row at each
  frame end while the command runs. No row number is kept past the frame it was read in (2026-10-23; until then a
  two-row prompt printed from the bottom row lost its first row, and an output line that began like a continuation
  row vanished, since both were judged by row numbers that had scrolled).
- AT-4.3 **System lines** (`System: …`, found with `s`): the shell started (`Shell started: PowerShell 7 in C:\path`;
  for the custom shell and the user's own, with the command line: `Shell started: Dev cmd, command line cmd.exe /k, in C:\path`, AT-1.6b),
  ended, restarted, a new shell, the update's steps (the window base, AX-6.5), a recording started or stopped.
- AT-4.4 **Clear transcript** (Ctrl+Shift+Delete, the key that clears a history in every window, bundle AX-8.18;
  Ctrl+L until 2026-11-26; Session → Clear transcript): empties the window, the recent commands
  staying; the count of the blocks starts again; "Cleared". The lines off the screen go; the lines still on the
  screen are hidden until the shell writes over their rows, since the shell goes on drawing on that screen. The
  shell's own `cls` or `clear` clears only its screen: the window lets the rows go first (the lines keep their text,
  the way axClaude keeps a repainted conversation, and the rows start blank), so what was read stays and what the
  shell prints next goes on at the end; a program that homes the cursor to redraw the screen, or resets the terminal
  (`ESC c`), is treated the same, and prints its redraws as new lines (the alternate screen is a non-goal).
- AT-4.5 Bookmarks (`m`, `k`, Ctrl+K; axClaude FR-3.9), `l` (the line number), Backspace (back from a jump,
  axClaude FR-3.12), the reading breaks of a line being written (axClaude FR-3.10), the selection that holds output
  back (axClaude D29), Ctrl+C "Copied", Ctrl+A, find with Ctrl+F, F3 and Shift+F3: all the view's, unchanged.
- AT-4.6 The transcript is capped at 20 000 lines (`maxTranscriptLines`), trimmed from the top (axClaude FR-3.6).
- AT-4.7 **Export transcript…** (Ctrl+S, the bottom of the Session menu before Exit; axClaude FR-3.8): the file
  picker, the default name `axterminal-<yyyy-MM-dd-HHmm>.md` in the shell's current directory (the Documents folder
  when it is gone), the visible lines as text, "Exported". No git exclude of its own: axClaude's (D34) is about the
  files axClaude writes into the project it runs in; an exported transcript is the user's file where they put it.

### AT-5 Announcements and signals

- AT-5.1 **Done and Failed.** When the `A` mark follows a command: "Done" with the done chime when the last `D` mark
  carried `0` or no code (Command Prompt), "Failed, exit code 1" with the same chime when it carried another
  (PowerShell gives `-1` for an error of its own: "Failed"). Nothing for a bare Enter on an empty field: the shell
  gives a `D` without a code and no command was sent. "Sent" has the sent note; the chimes are axClaude's
  (`soundOnBell`, axClaude FR-7.3), the ready ping with "Ready" when the first prompt appears after a start. The word
  waits for the frame that ends the command, or the banner, and is said with that frame's speech as one notification
  ("Done. hello"; "… All rights reserved.. Ready"), since NVDA drops the second of two raised back to back; alone after
  half a second when no frame comes (the NVDA pass of 2026-11-01 heard "Done" and Command Prompt's "Ready" go unspoken). **The word of a command's end stands before its output** (the maintainer's decision of 2026-11-22, for axClaude, its FR-7.2a, and here alike: "a setting that speaks done before the result or does not speak done at all", with no threshold of time, which would leave the user guessing why the window behaves differently from one command to the next). Until then the last line stood first ("hello. Done"), and after a long output the word was heard only when NVDA had read all of it. So that the word stands before the whole result of a quick command, and not in the middle of it, the output's speech is held for 300 ms from its first line (AT-5.3): a command that ends within that time says "Done" and then everything it printed; one that runs on says its output in pieces a third of a second apart, and at its end "Done" with what is left. "Ready" stays after the banner it ends. **The word has a switch of its own** (`announceDone`, default on; Options → When a command is done → Announce "Done"; axClaude has its own, `announceBell`): off, a command that ends well says its output alone. The chime and the attention signals are not the word's and stay; "Failed" with its code is always said, before the output.
- AT-5.2 **Tick** (`clickOnNewLine`, axClaude FR-7.8): one tick for a frame that adds a visible line; no tool tick,
  since nothing here is a tool call.
- AT-5.3 **Speak output** (`outputSpeech`: `none` or `all` (default); Options → Speech → Speak output: none | all
  lines, checked like a radio group): the lines that became final, held for 300 ms from the first and then one notification (AT-5.1 says why; axClaude FR-7.4,
  D32), the markers and the hidden rows left out. All lines by default since 2026-10-23, decided at the NVDA pass: "axTerminal
  should speak the output of any command" (axClaude's default stays none, D10, since Claude's replies have Speak replies).
- AT-5.4 **A program asks** (`announceQuestions`, default on; Options → Speech → Say when a program asks): while a
  command runs, when output has stopped for two seconds and the cursor row has text that ends in `:`, `?`, `]` or
  `)`, with or without a trailing space, "Asks: <the row's text>" is announced once for that text, with the question
  chime, the analog of "Claude asks" (axClaude FR-7.2) without Claude's bell. A heuristic, kept until a better signal
  exists; off, the tick and the status are what tell.
- AT-5.5 axClaude's attention options, unchanged in meaning: the taskbar flash when the window is not active
  (`flashTaskbar`), the louder background alert (`backgroundAlert`) and the working music while a command runs, with
  Ctrl+M (Ctrl+Space until 2026-11-15, now the bundle's mode key, AX-8.13; `workingMusic`, axClaude FR-7.9), all
  through the shared `Sounds`.
- AT-5.6 Nothing speaks on an arrow key; a jump announces its line; Alt+1 and Alt+2 name the control they land on
  (AX-7.3, axClaude D26).

### AT-6 Menus, keys, texts

- AT-6.1 Menus, in the standard order of a first menu (AX-6): **Session (S):** New session… Ctrl+N | Open recent
  folder ▸ (ten, "1. name (path)", AX-9.5) | Open recent folder in new window ▸ (the same list, a new axTerminal window on the one chosen, this one left as it is) | Switch shell ▸ (PowerShell 7 | Windows PowerShell | Command Prompt |
  Git Bash | the user's shells, "1. name (command line)", the shells alone, AT-1.6a) | Custom shell… | Add a shell… | Remove a shell ▸ | Install PowerShell 7… | Install Git, for Git Bash… (the installers when the shell is missing; under Switch shell until 2026-11-11, a menu three deep, AX-9.1) | ─ | Restart shell Ctrl+Shift+R | Clear transcript Ctrl+Shift+Delete | ─ | Say current folder F8 |
  Show current folder in ▸ (File Explorer | axClaude | axGIT | axFile), on the shell's current directory | ─ |
  Export transcript… Ctrl+S | Exit Alt+F4. **Actions (A)** ("Operations" in 5.7.0; the bundle's eight since 2026-11-11, AX-9.1):
  Interrupt Ctrl+I | Send Escape Shift+Escape | Send key ▸ (Up | Down | Ctrl+D (end of input)) | ─ | Recent
  commands… Alt+Down | Send without recording (no key) | ─ | What is running F7.
  **Navigation (N):** axClaude's Navigation menu with the kinds the terminal has: Command field Alt+1 | Terminal
  Alt+2 | Latest output Alt+3 | ─ | Find… Ctrl+F | Find next F3 | Find previous Shift+F3 | ─ | Next bookmark |
  Previous bookmark (the other quick keys are the terminal's letters, named in the F1 text). **Context (C)** (AX-9.11): Copy
  this line into the command field (Enter in the terminal) | Bookmark this line Ctrl+K | Copy the selection; Shift+F10,
  the Applications key and a right click in the terminal or the field open it. **Options (O):** Sounds ▸ (Chimes ✓ | Ticks for new lines ✓ |
  Background music while a command runs Ctrl+M) | Speech ▸ (Speak output: none ○ | Speak output: all lines ○ |
  ─ | Say when a program asks ✓) | When a command is done ▸ (Flash the taskbar button ✓ | Louder alert when the window
  is in the background) | Text ▸ (Time stamps | Markers in the text ✓ | ─ | Font… | Larger text | Smaller text | Windows text size)
  | Developer ▸ (Open settings file | Record raw stream) | ─ | the settings of all of axKit (AX-9.1, 2026-11-26).
  **Window (W)** (before Options), **axKit (K)**
  (AX-9.1). **Help (H):** Command palette… F1 | Keyboard shortcuts | User guide | ─ | Shell resources ▸ (PowerShell documentation |
  Windows Terminal shell integration | Bash manual). Every item has a mnemonic (AX-7.4).
  The first menu is Session, as in axClaude, since what it holds is the shell in its folder, the thing the window
  is about; Shell takes E, S being Session's.
- AT-6.2 Keys (AX-8). Bundle-wide: F1 and Ctrl+Comma (the command palette with the keys, AX-10; Ctrl+Shift+P until
  2026-11-26), the control keys, Ctrl+F, F3,
  Shift+F3, Ctrl+S, Ctrl+Plus, Ctrl+Minus, Page Up, Page Down, Ctrl+Up and Ctrl+Down with Shift, Ctrl+Space (edit mode or navigation mode in the command field, and in the terminal "Read only: there is no edit mode here", AX-8.13; in navigation mode Ctrl+Up and Ctrl+Down go by paragraph in the field, not through the history, and Enter sends nothing; in a full-screen program's screen view, keys to the program or reading the screen, AT-9.3), Alt+F4. Window-specific (`AxTerminalKeys`): Ctrl+N, Ctrl+Shift+R,
  Ctrl+Shift+Delete (Ctrl+L and Ctrl+W until 2026-11-26), Alt+1, Alt+2, Alt+3, Ctrl+I, Shift+Escape, Alt+Down, F8, Ctrl+M, Ctrl+K, with the bundle's F7 as What is running; in
  the field Enter, Shift+Enter, Escape (Ctrl+Enter there is the bundle's main action, AX-8.10: it sends as Enter does); in the view the quick keys `i`, `o`, `h`, `1` to `6`, `p`, `s`,
  `k` with Shift, `m`, `l`, Enter, Escape, Backspace, and Ctrl+Alt with `i`, `o`, `h`, `p`, `s`, `k`. axClaude's `c`,
  `t`, `d`, `e`, `q` and `r` have no kind here and do nothing. The collision test covers the table and lists the keys
  axClaude uses otherwise (Ctrl+N).
- AT-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axTerminal/user-guide.md` is embedded (Help → User
  guide); About is the bundle's (AX-9.9).

### AT-7 Announcements

One UI Automation notification each, on the view, or on the notice or the list while one shows (AX-7.3): "Sent",
"Sent, not recorded", "Done", "Failed, exit code n", "Failed", "Ready", "Asks: <row>", "Interrupt sent", "Escape
sent", "Line copied", "No recent commands", "Shift+Escape sends Escape, Ctrl+I interrupts", "The shell ended",
"The shell ended. Ctrl+Shift+R starts it again", "Restarting", "New session in <folder>", the shell's name on Switch shell, "Cleared", "Exported",
"Copied", "Not found", "Line n of m", "Bookmark n added" and "Bookmark n removed", "Line gone", "No next input" and
the other jump results with the kind's name, "Command", "Terminal" and the other names when the control already has
the focus, "Running: …", "Idle, …", "Shell ended" and "No marks from this shell" on F7, the folder's full path on F8, "Up sent", "Down sent"
and "Ctrl+D sent" (the Send key menu), "Folder <name>" (a recent folder), "Folder missing", "Speak output: none" and
"Speak output: all lines" (the choice made), "Background music on" and "off" (axClaude FR-7.9), "Text size n",
"Windows text size", "Copied diagnostics", "Recording started" and "Recording stopped" (the system lines name the
file). The speech of output (AT-5.3) and the texts of the notices are the rest.

### AT-8 Settings, log, status

- AT-8.1 `%APPDATA%\AxKit\axTerminal.json` (AX-5.3; `TerminalSettings` on the bundle's `ConsoleSettings`, the console
  fields shared with axClaude under the same names, on `WindowSettings`): `shell` (`pwsh`,
  `powershell`, `cmd`, `bash`, `custom`), `customShell`, `userShells` (the user's own shells, `name` and
  `commandLine` each, trimmed, a blank one dropped and a repeated name kept once, AT-1.6a), `userShellName` (the one
  in use, with `shell` `custom`; dropped when no shell of that name is listed), `lastFolder`, `recentFolders` (ten), `ptyColumns` 240, `ptyRows` 50,
  `soundOnBell` on, `clickOnNewLine` on, `workingMusic` off, `backgroundAlert` off, `flashTaskbar` on, `outputSpeech`
  `none`, `announceQuestions` on, `announceDone` on, `markerTimeStamps` off, `showMarkers` on, `joinWrappedLines` on, `maxTranscriptLines` 20000, and the
  window base's font, update check and window fields. Unknown or out-of-range values fall back to the defaults.
- AT-8.1a **The shells are sealed** (bundle AX-5.6, 2026-11-16, `repairs.md` R-53): `shell`, `customShell` and
  `userShells` are under the file's `seal`, written at every save. A file whose seal does not check starts the
  machine's default shell, holds no custom command line and no shells of the user's, keeps every other setting, and
  says once, as a system line at the start: "Your shells in axTerminal's settings were changed outside axKit and are
  not used. Add them again from Session, Add a shell." A file from before the seal is accepted and sealed at its next
  save, which the first start of a shell is. `TerminalSettings`, tested in `TerminalSealTests`.
- AT-8.2 The log is `%LOCALAPPDATA%\AxKit\logs\axTerminal.log`: the shell's start with its command line and folder,
  its exit code, restarts, errors; never a command or a line of output (a command can hold a secret). Options →
  Developer → Record raw stream writes the console stream to a `.vt` file in the PtyCapture format for a fixture
  (axClaude FR-13), read back only through the redacting dump (bundle rule 6).

### AT-9 The screen view of a full-screen program (designed and built 2026-11-25: the core AT-9.8, the window AT-9.9)

Decided by the maintainer on 2026-11-24 through the question tool, after a tester found Windows' `edit` unintelligible
in axTerminal (2026-11-18): while a program holds the whole screen, axTerminal shows that screen as a page NVDA
reads, and goes back to the conversation when the program leaves it. Designed from a recording of `edit` 1.2.1 at
120 by 30 (2026-11-25, PtyCapture): it switches to the alternate screen (`ESC [?1049h`), turns on mouse reporting
(`?1002`, `?1006`), focus events (`?1004`), bracketed paste, and asks the terminal for its sixteen colours
(`OSC 4 ; n ; ?`); then it draws a menu bar whose letters to press are underlined (`F`ile, `E`dit, `V`iew,
`H`elp), each line of the file behind its number and a vertical bar, and a scroll bar of block characters, all in
colours of 24 bits; the keys sent after its first frame took no effect within the recording's two seconds, which is
read as `edit` still waiting for the colours' answers (to measure first, AT-9.7).

- AT-9.1 **When.** The program switches to the alternate screen (`ESC [?1049h`, also `?1047h` and `?47h`). The shared
  screen model (`AxKit.Core/Vt/Screen`) keeps the alternate buffer apart from the main one, as a terminal does, and
  raises that it changed; the conversation's lines are the main buffer's and are not touched. The view returns when the
  program switches back (`?1049l`) or ends.
- AT-9.2 **What is shown.** The conversation and the field give way to one read-only text, the screen: one line per
  row, no wrapping, the rows' trailing spaces left out, every character as the program drew it, the box-drawing and
  block characters (U+2500 to U+259F) included (the maintainer's decision of 2026-11-25, section 7: NVDA names them,
  "vertical line", as it reads, and they are where the program draws its boxes and its scroll bar). Its name says
  what it is, "Screen of edit", the program named by the first word of the command that started it ("the program"
  for one sent without recording); the way the keys go is the status bar's and the switch's (AT-9.3), never the
  name's, since NVDA says a focused control's new name by itself and it would speak twice.
- AT-9.3 **Two ways to be in it.** Keys go to the program, the edit mode (every key, as a terminal sends it, but
  Ctrl+Space and the bundle's F1 and Alt+F4); the caret of the text stands where the program's cursor stands, so
  that NVDA reads the program's line as the cursor moves, which the user's own keys caused. Ctrl+Space, the bundle's
  mode key (section 7), turns to reading the screen, the navigation mode: the keys stay in the text, the arrows and
  NVDA's own reading keys read the page, the window's commands work again, nothing reaches the program, and the same
  key goes back, the caret to the program's cursor. The status bar's left part says which way is in force; the
  switch is said in two words, "Reading the screen", "Keys to edit".
- AT-9.4 **What is said by itself.** Nothing on a redraw. After a key sent to the program, the line the cursor ends on
  is read by NVDA through the caret, as in a terminal; and a marked run of a row (reverse video, or a background that
  differs from the cells on both sides of it in that row: an open menu's selected entry, a dialog's button in focus)
  is said once when the mark moves to another row or another place, "New File, Ctrl+N" read off the run (a gap of
  two spaces or more said as a comma's pause), since that is where a full-screen program shows its focus; a page
  with no marked run forgets the places, so that a menu opened again says its entry again. The words are the event
  kind ProgramSaid, not interrupting, after the rows and the caret of their frame. Measured on `edit` (2026-11-25): its open File menu is a box of
  one grey background on the screen's black, and the selected entry a green one inside it, so a background other
  than the screen's own would mark the whole menu; the current line of its text has a lighter background across the
  whole row, which marks nothing by this rule (the caret stands there). The screen model gains per cell its
  background (or reverse video) as one number to compare, and no other colour. The rule as built (AT-9.8): a run is
  cells of one number between cells of another; it is marked when the cells on both sides of it share one number
  and that number is not the page's own (the one most cells have), so that a menu or a dialog drawn on the screen is
  not a mark and what stands out inside it is; a run in reverse video between cells that are not is marked on the
  page's own background too; a run at either end of its row is never marked. The literal "differs from both sides"
  marked three runs on the menu's row of `edit` (its right border and the text behind the menu as well).
- AT-9.5 **The rows updated** at a frame's end, as the conversation's are (AT-4), only the rows that changed, the
  caret kept on its row when the program's cursor did not move; a program that redraws without end (a clock, a
  progress bar) is said nothing about.
- AT-9.6 **What is not done.** Mouse reporting is ignored (no mouse events are sent); the program's colours are not
  shown or said; the alternate screen's rows never enter the conversation, its export or its find.
- AT-9.7 **To measure before the build**, with a recording each, in the record-fixture skill's way: whether `edit`
  waits for the answers to its colour questions (measured 2026-11-25: keys sent in its first second were not taken;
  after four seconds without answers it typed, opened its File menu and quit with Ctrl+Q and N, exit code 0; so it
  waits a few seconds and then goes on; answered, it takes keys at once: recorded the same day with the sixteen
  answers written 600 ms after its start, its File menu opened 2 ms after Alt+F at one second, AT-9.10); `edit`'s menus, a dialog (Save as, Find) and its status line, opened with the keys
  and read off the screen model; `less` or `more` over a long file; a program that leaves the alternate screen by
  ending with Ctrl+C. Each becomes a fixture under `tests/fixtures` and a window script; the build follows them.
- AT-9.8 **The core as built** (2026-11-25), in `AxKit.Core/Vt`, shared with axClaude. With `KeepsAlternateApart` on, which axTerminal's model turns on for every screen it makes, since its window shows the page (off by default, so that axClaude's conversation keeps what a full-screen program draws), `Screen` keeps the alternate buffer
  apart: `?1049h` saves the cursor and clears it, `?1047h` and `?47h` switch only, `l` switches back (1049 restoring
  the cursor, 1047 clearing the alternate buffer), a reset leaves it; `Alternate` says which is in use and
  `AlternateChangedSinceFrameEnd` that it changed. While it lasts, the main buffer's rows, their lines and the
  transcript are untouched: no line is made, written, committed or removed, and the cursor homed and the screen
  erased are not raised (they speak of the main buffer); `CursorRow`, `LineAt`, `RowOf`, `EnsureLine`,
  `PullRowText` and `DetachRows` are the main buffer's. SGR is read for the background alone (40 to 47, 100 to 107,
  `48;5;n`, `48;2;r;g;b` and their colon forms, 49, 7, 27, 0; a foreground's `38;...` read past), one number per
  cell of the alternate buffer only, and an erase takes the background in force, as a terminal's does. `ScreenPage.Read`
  gives the alternate buffer as a page: the rows' texts (AT-9.2), the cursor's row and column, and the `MarkedRun`s
  (row, first cell, length, text) by AT-9.4's rule. On the recording of `edit` (`fullscreen-edit1.2.1-120x30.vt`):
  the menu bar on the first row, "New File    Ctrl+N" the one marked run while the File menu is open, "[Save]" the
  one in the unsaved-changes dialog, and the main buffer as it was after Ctrl+Q and N. Not marked: the menu bar's
  File highlighted after Escape, which stands at the row's left end (a rule for a run at the end of a row waits on
  more recordings, AT-9.7). Tested in `ScreenAlternateTests` and `ScreenPageTests`.
- AT-9.9 **The window as built** (2026-11-25). `TerminalModel` keeps the alternate screen apart and gives its `Page`.
  At every frame's end (`TerminalForm.Screen.cs`, from the window's pass over the transcript) the window reads the
  page while the shell runs: a page and no view opens the view (the terminal, the field and the control area hidden,
  the view `ScreenBox` shown with the focus, keys to the program); a page and the view replaces the rows that
  changed (`ScreenView.Edits`, the last row first, the drawing held) and puts the caret on the cursor in keys to the
  program, on the row and column it was on in reading the screen; no page, or the shell ended or stopped, closes the
  view: the terminal and the field come back as they were and the focus goes back to the one of the two that had it
  before (the field, unless the terminal had it). A notice over the view hides it and gives it back. In keys to the
  program `ScreenBox` takes every key in its own `ProcessCmdKey`, before the window's commands: Ctrl+Space turns
  the way (`ToggleScreenMode`, said with the event kind Changed), F1 and Alt+F4 go on to the window, a key
  `ScreenKeys` names goes to the program as xterm sends it (the arrows, Home, End, Insert, Delete, the page keys
  and F1 to F12 with xterm's modifier parameter; Enter, Tab, Shift+Tab, Backspace, Escape; Ctrl with a letter as its
  control character; Alt with a letter or a digit as Escape and the character), and any other key's character
  goes as typed (Alt with a sign as Escape and the sign); the edit control's own key, character and context menu
  messages are dropped meanwhile. Ctrl+Alt, AltGr on many keyboards, sends the character it makes. Alt alone, which a
  terminal sends nothing for, opens axTerminal's menu bar as ever. The question heuristic (AT-5.4) is off while the
  view shows. Windows' console paints the main screen again when the program leaves the alternate screen (the cursor
  home, every row with an erase to its end; recorded 2026-11-25 with PowerShell running `edit`): in the frame of
  that switch the model does not let the rows go on the cursor homed (AT-4.4's `cls` rule), so the repaint lands on
  the lines already there, and the shell's banner and the prompt row with the command do not come again as output.
  A key sent at once after Escape is read with it by the console as one key with Alt, as a terminal's input is. Tested in `ScreenViewTests` (the keys; the rows, the caret and the edits; the program's name; the
  marked run once; the recording of `edit` walked as the window walks it: one opening, one closing, "New File,
  Ctrl+N" and "[Save]" said, each once, the text always the page's, nothing of edit in the transcript) and by the
  window script `tests/scripts/axTerminal/14-screen-view.txt` (Windows' `edit` itself). What this left (the colour
  questions unanswered, a program ending on the alternate screen keeping the view, the places and find aimed at the
  hidden terminal) is AT-9.10.
- AT-9.10 **What the first build left, done** (2026-11-25).
  - **The places and find while the view shows.** The command field, the terminal and the latest output are behind
    the screen, which is the window's one place while a program holds it (AX-8.14 names a window's places; none of
    the three can be reached). In reading the screen, Alt+1, Alt+2 and Alt+3, and
    their Navigation items from either way, move nothing and say where the focus is and when the places come back:
    "Screen of edit. The terminal and the command field come back when edit leaves the whole screen" (in keys to the
    program the keys are the program's, AT-9.3, and only the menu items say it). Find, Find next and Find previous
    search the page's rows in reading the screen, with the bundle's find of a text (the Find notice, the match
    selected, wrapping once, "Not found"), as in an editor; the terminal's lines are not searched while the view
    shows (AT-9.6). Chosen from the menu in keys to the program, where the caret is the program's cursor, find moves
    nothing and says "Find works in reading the screen: Ctrl+Space turns to it". The window base has the two hooks:
    `ConsoleForm.PlacesRefused` and `ConversationForm.FindDialog` and `FindNext`, which `TerminalForm.Screen.cs`
    overrides.
  - **A program that ends on the alternate screen.** A program killed or crashed, or one that never switches back,
    leaves the console on the alternate screen, and the shell's prompt comes back drawn there. The shell's own mark
    (D, A or B, AT-4, the secret's check passed) while the alternate screen is in force is taken for the program's
    end: the model leaves the alternate screen as on `ESC [?1049l` (`Screen.LeaveAlternate`: the main rows and their
    lines as they were, the cursor where it stood, and what the program set let go: its scroll region, its
    background and its reverse video, which until 2026-11-27 stayed, so that the shell's output scrolled inside the
    dead program's region), so the page is gone at that frame's end and the window closes the
    view as on the switch back, the focus where it was; the prompt and what follows reach the transcript. A reset
    (`ESC c`) from a full-screen program leaves the alternate screen too and clears the main screen's rows: the
    model hears of the erase before the switch and as an erase of its own, so the rows let go of their lines first
    and nothing already read is blanked (2026-11-27; the reset told the model after the switch until then, the model
    took it for the console's repaint, AT-9.9, and the lines the rows had were emptied). axTerminal
    sees no process of the program's own, so the mark is its end; the shell's own end closes the view as before.
  - **The colour questions answered.** While a program holds the alternate screen, its colour questions are answered
    at once in the console layer, `AxKit.Core/Vt/QueryAnswers` raised by `Screen` (on in axTerminal's model alone,
    `AnswersQueries`; axClaude's Claude Code is told nothing new), and the window writes each answer to the shell's
    input as it is: OSC 4 with a colour's number and `?` (the palette; edit asks the sixteen), OSC 10 `?` (the text)
    and OSC 11 `?` (the background), in xterm's form `ESC ] 4 ; n ; rgb:rrrr/gggg/bbbb BEL`, the colours of a plain
    black background and light text: Windows' console palette (Campbell) with its black plain black, the text light
    grey (`cccc`), the background black; the numbers 16 to 255 as xterm's cube and greys. Windows' `edit` takes keys
    at once instead of after about four seconds (AT-9.7). On the main screen no question is answered, since a shell's
    line editor would take the answer as typed text, and no answer holds a line end. A device attributes or status
    question (`ESC [c`, `ESC [6n`) never reaches axTerminal: the console answers those itself (none is in the
    recording), so none is answered here.
  - Tested in `ScreenViewTests` (the words; a hand-written stream where the prompt marks follow the page with no
    switch back: no page, the command ended with its code, the next output in the transcript; edit's recorded
    questions answered, sixteen answers, none on the main screen), `ScreenAlternateTests` (`LeaveAlternate`, an
    answer only on the alternate screen and only where asked for), `QueryAnswersTests`, and in the window script 14
    (Alt+1 and find in reading the screen; edit's first key half a second after its page; a switch written by
    PowerShell itself, `[Console]::Write([char]27 + '[?1049h')`, giving the field back at the prompt).

## 5. Architecture

```
src/AxKit.Core/Pty, Vt, Transcript  from AxClaude.Core, moved on this second use (B-3) with the namespaces of their
                          folders, unchanged apart from one seam: Pty/PtyHost, Pty/StreamRecorder; Vt/VtParser,
                          Vt/Screen (gains the OSC 133 and 9;9 events, and PullRowText), Vt/Wcwidth; Transcript/Line,
                          LineKind, TranscriptMirror, Utterance; and LineStore, the part of SessionModel that holds the
                          lines (the list, CreateLine, Commit, Remove, TextChanged), which Screen writes to; SessionModel
                          keeps that part by deriving from it. Since 2026-10-23 LineStore also holds what both models
                          had copied: the UTF-8 decoder and Feed, the parser and the screen (NewScreen, to which each
                          model attaches its own handlers), EndFrame, the bookmarks, the cap and the Changed and Bell
                          events. Tested where they were (axClaude's fixtures, the shell fixtures).
src/AxKit.Forms/TranscriptView  from axClaude, its quick-key table given by the window as data (the QuickKey records with
                          the window's keys) instead of read from AxClaudeKeys; axClaude hands it its own table.
src/AxTerminal.Core/      class library (net10.0): ShellLauncher (find the shells, the command line with the prompt
                          snippet or the PROMPT variable, the environment), TerminalModel (on LineStore: frames to lines,
                          the prompt rows from the marks, the running state and the exit code, the exchange blocks, system
                          lines, bookmarks, the join flag, the cap; the parts of SessionModel that are not Claude's, written
                          again against the shell fixtures rather than carved out of it), CommandHistory, TerminalSettings,
                          ScreenKeys and ScreenView with ScreenMarks (the screen view's keys, text and words, AT-9.9);
                          references AxKit.Core
src/AxTerminal/           class library (net10.0-windows): AxTerminalApp (Run, Usage), TerminalForm (the window, on
                          AxKit.Forms/ConsoleForm since 2026-10-23, the console window shared with axClaude, which owns
                          the view, the field, the control area, the status bar, the pty bridge, the writes, the
                          recording, the export, find, the signals and the music; in partial files by section: the
                          hooks and the key switch, the menus and the status, the field and the sending, the process,
                          what the model reports, New session, Switch shell and the folders, the screen view of a
                          full-screen program with its text ScreenBox, AT-9.9), AxTerminalKeys (with
                          Transcript, the view's keys as data), HelpText (F1 text, the embedded guide); references
                          AxTerminal.Core, AxKit.Core, AxKit.Forms
tests/AxTerminal.Tests/   fixtures recorded with tools/PtyCapture of powershell, cmd and bash under the launcher's command
                          line (a dir, a failing command, a y/n prompt, a multi-line command, cls, exit): golden
                          transcripts, the state at the end and the codes of the commands, the same transcript
                          whatever the chunk boundaries; TerminalModelTests on hand-written streams; ShellLauncherTests,
                          CommandHistoryTests, TerminalSettingsTests
docs/axTerminal/          this spec, user-guide.md (embedded), nvda-test-plan.md
```

The data flow and the threading are axClaude's (axClaude §7.2, §7.3): the reader thread posts bursts to the UI
thread, which owns the parser, the screen and the model; a frame ends at `ESC[?25h` or after 100 ms of quiet; the view
applies the mirror's edits and maps the caret; writes go through the paced queue. The order of the work, one commit
each (bundle AX-6.3): the `LineStore` seam in `Screen` and `SessionModel` with axClaude's tests green; the move of the
Core pieces; the parser's two events; `TranscriptView` to `AxKit.Forms` with its table as data; `AxTerminal.Core` with
its fixtures; the window; the wiring, the installer and the documents (`adding-an-app.md`); axClaude's spec §7.1 and
its notes record what moved.

## 6. Design decisions

- **AT-D1 axClaude's window, not a terminal emulator.** A screen reader already has a terminal with a screen, Windows
  Terminal with NVDA's console support, and it reads badly: output scrolls under the cursor, nothing marks where a
  command's output begins, and a blind user reads the screen's rows rather than the program's lines. axClaude's shape,
  a transcript of lines with markers around each exchange and a field of the app's own, is what the user asked for
  ("just like axClaude"), and the pieces exist.
- **AT-D2 The shell's integration marks tell the state; no prompt regexes.** What a prompt looks like is the user's
  business, and guessing it from text is where terminal wrappers rot. The marks are a documented convention the shells
  already support through their prompt; the app injects its wrapper for the session without touching the user's
  profile; without marks the app degrades to a plain transcript rather than guessing. The `C` mark is not needed: the
  app sends the `\r` itself and knows when a command starts.
- **AT-D3 Every Enter is an exchange; Send without recording is the one that leaves no trace.** One rule for the record (the
  transcript is the log of what was typed and what came back, a program's questions and their answers included) and
  one command for the secrets. It was Ctrl+Enter until 2026-11-14; the user gave that key to the main action in
  every window (bundle AX-8.10 rule 3), so the command is in the Actions menu and the palette with no key, and
  Ctrl+Enter in the field sends as Enter does. A separate "input to a running command" kind was considered and dropped: a second block
  shape to learn for the rare case.
- **AT-D4 Enter on a line copies the bare line.** axClaude's quote markers exist so that Claude can tell the quoted
  line from the comment; a shell wants the command itself, to run or edit it.
- **AT-D5 No history on the arrows.** The bundle's rule (axClaude D13: text appearing in a field on an arrow key
  confuses the reading) holds; the Recent commands list (Alt+Down) and `i` in the view are the history, on demand.
- **AT-D6 Shared by moving, one piece per commit.** The ConPTY host, the parser, the screen and the mirror are
  axClaude's best-tested code, and the second use is the moment the bundle's rule names (B-3). The one seam,
  `LineStore`, is the smallest cut that frees `Screen` from `SessionModel`; the terminal's own model is written
  against recorded shell sessions rather than carved out of Claude's, so that neither app's behaviour depends on the
  other's.
- **AT-D7 No pager.** A pager takes the screen over, and its output is then a page of the screen view (AT-9) instead
  of lines of the transcript to find, export and read again; `GIT_PAGER=cat` and
  `GH_PAGER=cat` are the settings those two programs document for switching theirs off, and the user's profile can set
  them otherwise. Other pagers are left alone (`q` and Enter leave them).
- **AT-D8 F7 says what the window is doing** (Ctrl+Q until 2026-11-26). On demand, from anywhere, like axClaude's F7 for a question:
  the state, the command, how long, the prompt.
- **AT-D10 The prompt rows are kept by line, not by row number** (2026-10-23). The marks arrive mid-frame while rows
  are still being written, and the screen scrolls between an `A` and its `B` and between a send and the output: a row
  number read at a mark names another row a frame later. The screen already binds a line to each row and moves it
  with the row, so the model remembers lines (`EnsureLine` at the `A` mark, `RowOf` when it needs the row again)
  and reads a row number only inside the frame that gave it. Three bugs came of the row numbers (a two-row prompt at
  the bottom losing its first row; an output line starting like a continuation row vanishing, at once and again
  during a long command); none can come of lines.
- **AT-D9 Every block names its folder.** The user asked for the directory a command ran in to be on hand in the
  transcript itself: the prompt rows that would say it are hidden, a `cd` between two commands leaves the output's
  place unknown when the text is read back or exported, and the status bar only has the directory of now. A `## Folder`
  heading with the path under it and a `## Command` heading over the command give the block the shape of a small
  document, which the heading keys walk and the export keeps; F8 (Session → Say current folder; Ctrl+P until 2026-11-26) says the directory of now, the full path, on
  demand.

## 7. Open questions

- Tab completion through the field: the field's text sent to the shell with a Tab, the completed line read back from
  the prompt row after the `B` mark and put into the field, a cheap round trip once the marks are there; not in
  version one.
- Running a program instead of a shell (`axterminal -- npm start`), a window per long-running process.
- Whether Speak output should default to all lines in a terminal (axClaude's default is none).
- The "a program asks" heuristic (AT-5.4): whether two seconds and the four end characters are right, after the NVDA
  pass.
- A history that persists across windows (`CommandHistory` to the settings file; AT-3.4 keeps the secrets out of it).
- Whether a `D` mark with a non-zero code should classify the output's last lines as errors for `e`.
- PowerShell 7 is untested here (the machine has Windows PowerShell 5.1 only); its snippet is the same.
- The screen view (AT-9): **decided by the maintainer on 2026-11-25** through the question tool. The key that turns
  between keys to the program and reading the screen is Ctrl+Space, the bundle's mode key (`BundleKeys.ToggleMode`):
  keys to the program is the edit mode, reading the screen the navigation mode, so one key means one thing everywhere
  (Ctrl+Shift with punctuation is AX-8.18 rule 7's, keys that work anywhere in Windows); the cost, a program that
  wants Ctrl+Space never gets it. Box-drawing characters are kept as the program drew them. A marked run is said by
  itself, once, when it moves. Built the same day (AT-9.9).

## 8. Status

- Planned 2026-10-18 at the user's ask, with axFile (`docs/axFile/SPEC.md`), AxKeePassium (axPass since 2026-10-23) and AxCryptomator
  (`docs/axKit/plan.md`).
- Built the same day as a first draft, in the order of §5: the `LineStore` seam and the moves into the shared
  projects (axClaude's tests unchanged), the marks as events, the view with its keys as data, `AxTerminal.Core` with
  the recorded sessions (Windows PowerShell 5.1 and cmd, then Git Bash in 3.0.0, `tests/fixtures/shell-*`), the window, the wiring. AT-1
  to AT-8 as described above, the git exclude left out (AT-4.7). Not yet heard under NVDA:
  `docs/axTerminal/nvda-test-plan.md` is to run before a release names it more than a first draft.
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axKit/nvda-test-record.md`.
