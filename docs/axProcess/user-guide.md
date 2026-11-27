# axProcess user guide

axProcess shows what runs on your computer, made for screen reader users: the running programs with how much memory and processor time they use, the Windows services, and the programs that start when you sign in. One window, one list, which shows one of the three at a time. Nothing changes while you read: the list is read again only when you press F5 or after something you did.

## Install

axProcess is part of axKit. Installing axKit (see the axKit guide: Help, About, axKit guide) gives you a Start menu entry `axKit axProcess` and a console command `axProcess`. Any axKit window starts one too: Alt+K, Apps, axProcess.

## Start

The window is called "This computer - axProcess". It opens in Processes; the list fills after about a second, the time it takes to measure the processor, and axProcess then says its name and the first row.

## The three lists

The computer has three lists here: Processes, Services and Startup. The window starts inside Processes. Left, or Backspace, goes up to "This computer", where the three lists are three rows, each with its count; Right, or Enter, on one goes into it. Alt+1, Alt+2 and Alt+3 go straight into a list from anywhere. It works like the lists of axTask.

- **Processes** (Alt+1): every running program, "chrome, 350 MB, 3% CPU, process 9120": its name, the memory it uses, its share of the processor over the last second, and its process ID. The largest first; View, Sort processes by, changes the order to processor or name.
- **Services** (Alt+2): every Windows service, "Windows Update (wuauserv), running, manual": its name, its short name, whether it runs, and how it starts.
- **Startup** (Alt+3): what starts when you sign in, "OneDrive, enabled, current user's Run key": its name, whether it is turned off ("disabled"), and where it is set. You can turn an entry off, on again, or remove it: see "Programs that start with Windows" below.

Type the start of a name to jump to it, as in File Explorer: letters typed within a second of each other build one search, the same letter again goes to the next name starting with it, and Shift with a letter goes backwards. Pressing a list's key while that list already shows says its name again. At the first or the last row, Up, Down, Home and End play a short low sound, since there is nowhere to move; so do Right on a row that is no list and Left at This computer. **F5** reads the list again and says "Refreshed"; at This computer it reads all three. **F7** says the summary: how many processes run, how much memory is in use, and how busy the processor was.

## Acting on an item

- **Enter** shows the item's details in a notice, with buttons for what can be done to it. Escape closes it.
- **Delete** on a process ends it, after a question: "End notepad (process 4410)? Anything unsaved in it is lost." **Ctrl+Enter** on a process does the same, after the same question. Context, End process tree ends it with the programs it started; on a program that started axProcess itself it says so and ends nothing, since that would end this window too. In Startup, Delete removes the startup program instead, also after a question. On a service Delete does nothing but say so: a service is stopped from its details.
- **Space** selects the process or the startup program you are on, and deselects it again; a selected row reads ", selected", and the status bar counts them. Shift+Down and Shift+Up select as you move, Shift+Space selects from the nearest selected row to this one, Ctrl+A selects every row, Ctrl+Shift+A or Escape deselects all. **Delete** then acts on the selected rows, one alone too and wherever the row you are on is, after one question that names them: "End 3 processes?", and afterwards "Ended 3 processes". Services are not selected.
- **Ctrl+E** opens axFile on the program's file, selected in its folder.
- **Shift+F10**, or the Applications key, opens Context: what acts on the row you are on.
- **Ctrl+C** copies the line. **Ctrl+F** finds an item by its text; F3 finds the next.
- A service's details offer **Start**, **Stop** and **Restart**. Windows lets only an administrator start and stop most services, and axProcess runs as you, so expect "access is denied" for those; axProcess says so plainly.

## Programs that start with Windows

In Startup (Alt+3) you can stop a program from starting when you sign in. The commands are in Context (Shift+F10) and on the details notice (Enter).

- **Turn off**: the program no longer starts with Windows. Nothing is deleted: this sets the same switch as Task Manager's Startup apps tab, and the line then reads "disabled". axProcess says "KeePassXC turned off: it no longer starts with Windows". The program itself is not closed if it runs now; it stays away from the next sign-in on.
- **Turn on**: the program starts with Windows again. "KeePassXC turned on: it starts with Windows again".
- **Remove** (Delete): takes the entry out for good, after a question that names the program, its command and where it is set. Cancel is the default, so Enter and Escape change nothing; Alt+R, or Tab to Remove and Enter, removes it. The program itself stays installed. For a file in your Startup folder, the file is deleted and does not go to the Recycle Bin; the question says so. Turn off is the gentler way: it can be undone with Turn on.

After each of them the list stays where you were; after a removal you are on the next entry.

Entries that read "computer's Run key" or "all users' Startup folder" are set for every user of the computer. Changing them needs administrator rights, which axProcess does not have and does not ask for; it says so and changes nothing. Turn those off in Windows' own places: Task Manager's Startup apps tab, or Settings, Apps, Startup.

Some programs write their entry again every time they run. If a removed program is back in the list later, use Turn off instead, which such programs leave alone.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Exit (Alt+F4); Refresh (F5).
- **Actions**: Deselect all (Ctrl+Shift+A); Select all (Ctrl+A); Summary (F7).
- **Navigation**: Processes (Alt+1); Services (Alt+2); Startup (Alt+3); Up to This computer (Left).
- **Context**: Copy line (Ctrl+C); Details (Enter); End process (Delete); End process tree; Nothing selected; Select or deselect (Space); Select to here (Shift+Space); Show program in axFile (Ctrl+E).
- **Options**: Check for updates at start; Edit app keys file; Filter; Keep a log of events; Open the sounds folder; Developer (Open settings file); Sort by (Memory, Name, Processor); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size); Theme (Edit theme file, Reload theme, Theme on).
- Keys without a menu item: Ctrl+Tab (Back to the list); Ctrl+Delete (Clear the filter); Delete (Delete); Ctrl+Enter (End process); Ctrl+F (Find in the list); F3 (Find next); Shift+F3 (Find previous); Right (Into the list); Ctrl+Down (Next section); Enter (Open); Ctrl+Up (Previous section); Space (Select or deselect); Shift+Space (Select to here); Shift+Home (Select to the first item); Shift+End (Select to the last item); Shift+Down (Select to the next item); Shift+Up (Select to the previous item); Shift+F10 (The Context menu); Left (Up a level).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1, the command palette, lists every key and command. Options ends with the settings of all of axKit.
<!-- end of the menus -->

## Settings

axProcess keeps the font, the text size, the update check, the window's place and the order of the processes in `%APPDATA%\AxKit\axProcess.json`. Options, Developer, Open settings file opens it in axJSON.

## Updates

axProcess checks for a newer axKit when it starts and offers it in a notice; the Help menu has Check for updates at any time. One update brings every app of axKit up to date.

## If something goes wrong

axProcess's log is `%LOCALAPPDATA%\AxKit\logs\axProcess.log`; it names every process you ended, every service you started or stopped and every startup program you turned off, turned on or removed. Help, Diagnostics, Copy diagnostics copies its end with the versions, for a bug report.

## About axProcess

axProcess is one app of axKit, by Dr. Kyle Keane (www.kylekeane.com), GNU AGPL 3 licence. The Help menu's About names the version and the licence.

## Disclaimer

Ending a process loses anything unsaved in it, and ending a part of Windows can make the computer unstable until you sign in again. axProcess asks before it ends anything; read the question. A startup program you remove is gone from the programs that start with Windows, and axProcess cannot bring it back; a program you turn off may be one you rely on without knowing it, such as a driver's helper or a backup, so turn it on again if something stops working after the next sign-in.
