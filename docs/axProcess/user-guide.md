# axProcess user guide

axProcess shows what runs on your computer, made for screen reader users: the running programs with how much memory and processor time they use, the Windows services, and the programs that start when you sign in. One window, one list, three views. Nothing changes while you read: the list is read again only when you press F5 or after something you did.

## Install

axProcess is part of axKit. Installing axKit (see the axKit guide: Help, About, axKit guide) gives you a Start menu entry `axKit axProcess` and a console command `axProcess`. Any axKit window starts one too: Alt+W, New window, axProcess.

## Start

The window is called "This computer - axProcess". It opens on the Processes view; the list fills after about a second, the time it takes to measure the processor, and axProcess then says its name and the first item.

## The views

- **Processes** (Ctrl+1): every running program, "chrome, 350 MB, 3% CPU, process 9120": its name, the memory it uses, its share of the processor over the last second, and its process ID. The largest first; View, Sort processes by, changes the order to processor or name.
- **Services** (Ctrl+2): every Windows service, "Windows Update (wuauserv), running, manual": its name, its short name, whether it runs, and how it starts.
- **Startup** (Ctrl+3): what starts when you sign in, "OneDrive, enabled, current user's Run key": its name, whether it is turned off ("disabled"), and where it is set. You can turn an entry off, on again, or remove it: see "Programs that start with Windows" below.

Type the start of a name to jump to it, as in File Explorer: letters typed within a second of each other build one search, the same letter again goes to the next name starting with it, and Shift with a letter goes backwards. Tab and Shift+Tab move to the next and the previous view, round from Startup back to Processes. Pressing a view's key while the view already shows says its name again. At the first or the last item, Up, Down, Home and End play a short low sound, since there is nowhere to move. **F5** reads the view again and says "Refreshed". **Ctrl+Q** says the summary: how many processes run, how much memory is in use, and how busy the processor was.

## Acting on an item

- **Enter** shows the item's details in a notice, with buttons for what can be done to it. Escape closes it.
- **Delete** on a process ends it, after a question: "End notepad (process 4410)? Anything unsaved in it is lost." Context, End process tree ends it with the programs it started. In the Startup view, Delete removes the startup program instead, also after a question.
- **Ctrl+E** opens axFile on the program's file, selected in its folder.
- **Shift+F10**, or the Applications key, opens Context: what acts on the line selected in the view you are in.
- **Ctrl+C** copies the line. **Ctrl+F** finds an item by its text; F3 finds the next.
- A service's details offer **Start**, **Stop** and **Restart**. Windows lets only an administrator start and stop most services, and axProcess runs as you, so expect "access is denied" for those; axProcess says so plainly.

## Programs that start with Windows

In the Startup view (Ctrl+3) you can stop a program from starting when you sign in. The commands are in Context (Shift+F10) and on the details notice (Enter).

- **Turn off**: the program no longer starts with Windows. Nothing is deleted: this sets the same switch as Task Manager's Startup apps tab, and the line then reads "disabled". axProcess says "KeePassXC turned off: it no longer starts with Windows". The program itself is not closed if it runs now; it stays away from the next sign-in on.
- **Turn on**: the program starts with Windows again. "KeePassXC turned on: it starts with Windows again".
- **Remove** (Delete): takes the entry out for good, after a question that names the program, its command and where it is set. Cancel is the default, so Enter and Escape change nothing; Alt+R, or Tab to Remove and Enter, removes it. The program itself stays installed. For a file in your Startup folder, the file is deleted and does not go to the Recycle Bin; the question says so. Turn off is the gentler way: it can be undone with Turn on.

After each of them the list stays where you were; after a removal you are on the next entry.

Entries that read "computer's Run key" or "all users' Startup folder" are set for every user of the computer. Changing them needs administrator rights, which axProcess does not have and does not ask for; it says so and changes nothing. Turn those off in Windows' own places: Task Manager's Startup apps tab, or Settings, Apps, Startup.

Some programs write their entry again every time they run. If a removed program is back in the list later, use Turn off instead, which such programs leave alone.

## Menus

The menu bar is the same eight menus in every axKit window.

- **Session**: Refresh (F5), Exit.
- **Actions**: Summary (Ctrl+Q).
- **Navigation**: Processes (Ctrl+1), Services (Ctrl+2), Startup (Ctrl+3).
- **Context**: what acts on the line selected, for the view you are in. Processes: Details (Enter), End process (Delete), End process tree, Show program in axFile (Ctrl+E), Copy line (Ctrl+C). Services: Details, Start, Stop, Restart, Copy line. Startup: Details, Turn off, Turn on, Remove (Delete), Show program in axFile, Copy line. So you never end a process from the services or the reverse. Shift+F10 opens it.
- **Options**: Sort processes by, Text (font and size), General (the update check), Developer (the settings file, diagnostics).
- **Window**, **axKit** and **Help**: the bundle's menus, the same in every axKit window, with Options just before Window. Window (Alt+W) starts a new window of any app, lists the open axKit windows (the one you were in last first: Alt+W, O, 1 takes you back to it), reopens a file or folder closed lately and closes all windows. axKit (Alt+K) holds what is the whole bundle's: Claude (the switch Use Claude in axKit, Ask about this window with Shift+F1, Folders Claude may read, Claude's working folder), the AI tools and the theme. Help (Alt+H) has the command palette (Ctrl+Shift+P), the keyboard shortcuts (F1), this guide, the app's resource pages, Send a report (F4), Your reports, Check for updates and About.

## Settings

axProcess keeps the font, the text size, the update check, the window's place and the order of the Processes view in `%APPDATA%\AxKit\axProcess.json`. Options, Developer, Open settings file opens it in axJSON.

## Updates

axProcess checks for a newer axKit when it starts and offers it in a notice; the Help menu has Check for updates at any time. One update brings every app of axKit up to date.

## If something goes wrong

axProcess's log is `%LOCALAPPDATA%\AxKit\logs\axProcess.log`; it names every process you ended, every service you started or stopped and every startup program you turned off, turned on or removed. Options, Developer, Copy diagnostics copies its end with the versions, for a bug report.

## About axProcess

axProcess is one app of axKit, by Dr. Kyle Keane (www.kylekeane.com), GNU AGPL 3 licence. The Help menu's About names the version and the licence.

## Disclaimer

Ending a process loses anything unsaved in it, and ending a part of Windows can make the computer unstable until you sign in again. axProcess asks before it ends anything; read the question. A startup program you remove is gone from the programs that start with Windows, and axProcess cannot bring it back; a program you turn off may be one you rely on without knowing it, such as a driver's helper or a backup, so turn it on again if something stops working after the next sign-in.
