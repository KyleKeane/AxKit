# axGIT user guide

**axGIT, access layer for Git.** axGIT only shows what Git does and passes your commands to it; Git, which you install, does the work, and a fetch, pull or push reaches the server your repository's remote names, under its terms. Before axGIT first runs Git, and before the first fetch, pull or push to each server, it shows a short reminder with a check box: you confirm that axGIT is only an access layer and that following those terms is up to you. Help, About, Licence has these reminders and what you agreed to.

axGIT is a git manager for one repository, made for screen reader users. One window shows one repository: a list of its changed files, its log or its branches, a text with the details of what you pick, and a status bar with the branch. Everything it does, it does with the git program you already have, so its messages are git's own.

## Install

axGIT is part of axKit. Installing axKit (see the axKit guide: Help, About, axKit guide) gives you a Start menu entry `axKit axGIT`, a console command `axGIT`, and axGIT in the `Open with axKit` submenu when you right-click a folder in File Explorer. You also need git itself, from https://git-scm.com; if it is missing, axGIT says so when it starts.

## Start

- From axClaude: Session, Show current folder in, axGIT (Alt+S, I, G). axGIT opens on the repository that holds the project folder.
- From axDown or axJSON: Session, Show current file in, axGIT (Alt+S, I, G). axGIT opens on the repository that holds the file.
- From the Start menu, `axKit axGIT`, or the console: `axgit C:\my\project`. Without a folder, axGIT opens the last repository you had, or asks for one with Session, Open repository (Ctrl+O).
- To go back: Alt+W, O lists the open axKit windows, most recent first, so Alt+W, O, 1 is the window you came from. From axGIT, Session, Show current repository in, axClaude opens a new axClaude on the repository, axTerminal a shell there, axFile the folder's files, and File Explorer (Ctrl+W) shows the folder.

The window is named after the repository's folder: "myproject - axGIT".

A repository can name programs of its own that git runs, hooks for example. When the one you open does, axGIT says so in a notice, "This repository runs programs of its own", with their names, and runs git without them. Choose Trust this repository only for a repository you know, your own for example; from then on git runs them as it would in a console. Your trust is for the programs the repository named when you gave it: if it later names another, a new hook for example, axGIT sets them aside again and asks again, saying that what the repository would run has changed.

## The three lists

A repository has three lists here, and the list at the top of the window shows one of them at a time. The window starts inside Changes. Left, or Backspace, goes up to the repository, where the three lists are three rows, each with its count; Right, or Enter, on one goes into it. Alt+1, Alt+2 and Alt+3 go straight into a list from anywhere. It works like the lists of axTask.

- **Changes** (Alt+1): one item per changed file, "Modified src/Foo.cs, unstaged". The last words say whether the file is staged for the next commit, unstaged, or both; a new file not yet in git reads "Untracked", a file in conflict after a merge reads "Conflict".
- **Log** (Alt+2): the last 200 commits, "3a1f2c0 2026-10-18 Kyle: Release 2.11.0".
- **Branches** (Alt+3): your branches, "main, current, origin/main, 1 ahead".

Up and Down move through the rows and nothing happens until you press a key. Type the start of a row to jump to it; the same letter again goes to the next one, and Shift with a letter goes backwards. Pressing a list's key while that list already shows says its name again, so you always know where you are. F5 reads the list again after you changed something outside axGIT.

Under the list is the details text, which Tab, Ctrl+Tab or F6 reach. It holds the last thing you asked to see: a diff, a commit, or what git answered. Arrow keys read it line by line; Ctrl+F finds text in it; Page Up and Page Down move a screen; Ctrl+Up and Ctrl+Down move by paragraph. In the list, Ctrl+F finds a line of the list instead, and F3 and Shift+F3 go to the next and previous one.

## Changes

- **Ctrl+Shift+S** stages the file you are on, and **Ctrl+Shift+U** unstages it; axGIT says "Staged src/Foo.cs". (Until now Space did both. Space selects now, as in axFile and axTask.)
- **Space** selects the file you are on, and deselects it again; a selected file reads ", selected", and the status bar counts them. Shift+Down and Shift+Up select as you move, Shift+Space selects from the nearest selected file to this one, Ctrl+A selects every file, Ctrl+Shift+A or Escape deselects all. With files selected, Ctrl+Shift+S and Ctrl+Shift+U act on all of them, "Staged 3 files", and the selection stays, so Ctrl+Enter can commit them next. Actions, Stage all and Unstage all do every file without selecting.
- **Enter** shows the file's diff in the details and puts you on its first line, so Down Arrow reads it. Lines starting with a plus were added, with a minus removed. Ctrl+Tab goes back to the list. For a new file, the details show the file itself.
- **Ctrl+E** opens the file in axDown, or axJSON for a `.json` file.
- **Delete** discards the changes to a file after a question; they cannot be brought back. A new, untracked file is not deleted from axGIT. With files selected, Delete asks once for all of them and names each; there Cancel is the default, so Enter keeps everything and Alt+D discards.
- **Ctrl+Enter** commits what is staged: a notice asks for the message, Enter commits, and axGIT says "Committed" with the commit's short hash. With nothing staged it says so instead.

## Log

Enter on a commit shows its message and the list of files it changed, in the details.

## Branches

Enter on a branch switches to it. Actions, New branch (Ctrl+N) asks for a name and switches to the new branch. Fetch (Ctrl+Shift+F), Pull (Ctrl+Shift+L) and Push (Ctrl+Shift+H) talk to the remote; axGIT says "Fetched", "Pulled" or "Pushed", and when git refuses, its whole answer comes as a notice you can read. The status bar says "Working" while one of them runs; the window stays usable.

axGIT does not merge, rebase or resolve conflicts. For those, use the terminal or ask Claude in axClaude; axGIT shows you the result.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Exit (Alt+F4); Open repository (Ctrl+O); Refresh (F5); Show current repository in (axClaude, access layer for Claude Code, axFile, axTerminal, access layer for your shell, File Explorer (Ctrl+W)).
- **Actions**: Commit (Ctrl+Enter); Deselect all (Ctrl+Shift+A); Fetch (Ctrl+Shift+F); New branch (Ctrl+N); Pull (Ctrl+Shift+L); Push (Ctrl+Shift+H); Select all files (Ctrl+A); Stage (Ctrl+Shift+S); Stage all; Unstage (Ctrl+Shift+U); Unstage all.
- **Navigation**: Branches (Alt+3); Changes (Alt+1); Log (Alt+2); Up to the repository (Left).
- **Context**: Discard changes (Delete); Open file in editor (Ctrl+E); Select or deselect (Space); Select to here (Shift+Space); Show diff (Enter); Stage (Ctrl+Shift+S); Unstage (Ctrl+Shift+U).
- **Options**: Filter; Developer (Open settings file); General (Check for updates at start); Sort by (Name, The list's own order); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size).
- Keys without a menu item: Escape (Clear the filter); Delete (Delete); Ctrl+F (Find in the list); Ctrl+F (Find in the text); F3 (Find next); F3 (Find next in the text); Shift+F3 (Find previous); Shift+F3 (Find previous in the text); Right (Into the list); Ctrl+Down (Next section); Enter (Open); Ctrl+Up (Previous section); Ctrl+A (Select all); Shift+Home (Select to the first file); Shift+End (Select to the last file); Shift+Down (Select to the next file); Shift+Up (Select to the previous file); Ctrl+Tab (Switch between the list and the text); Shift+F10 (The Context menu); Tab (To the list); Tab (To the text); Left (Up a level).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1 lists every key.
<!-- end of the menus -->

## Settings

Everything you change is kept in `%APPDATA%\AxKit\axGIT.json`: the font and text size, the update check and the recent repositories. Options, Developer, Open settings file opens it in axJSON.

The repositories you trusted (`trusted`) are sealed in that file: they decide whether git runs a repository's own programs, so they count only as axGIT wrote them. If you change them in the file by hand, or another program does, axGIT trusts none, keeps your other settings, and says once: "The repositories you trusted in axGIT's settings were changed outside axKit and are not used: git runs none of their own programs. To trust one again, open it and choose Trust this repository." The file as it was found is kept beside it, with `.changed` at the end of its name (`axGIT.json.changed`). The seal's key is kept by Windows for your account; a program running as you could ask Windows for it, so the seal protects against a file changed by another program or by hand, a profile copied or synced from another computer, and an AI tool writing files, and not against a program that sets out to forge it.

## Updates

axGIT checks for a newer axKit when it starts and offers it in a notice; the Help menu has Check for updates at any time. One update brings every app of axKit up to date.

## If something goes wrong

axGIT's log is `%LOCALAPPDATA%\AxKit\logs\axGIT.log`; Help, Diagnostics, Copy diagnostics copies its end with the versions, for a bug report. "Git was not found" means git is neither in Git for Windows' folder under Program Files nor in a folder of your PATH on this computer: install it from https://git-scm.com and start axGIT again. "Not a git repository" means the folder you gave is not inside one; Open repository (Ctrl+O) takes another.

## About axGIT

axGIT is one app of axKit, by Dr. Kyle Keane (www.kylekeane.com), GNU AGPL 3 licence. The Help menu's About names the version and the licence.

## Disclaimer

axGIT runs git for you and shows what it answers; a command that changes the repository (discard, commit, switch, push) does what git does, and git's own safeguards apply. Keep a terminal for what axGIT does not offer.
