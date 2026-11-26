# axPass user guide

**axPass, access layer for KeePassXC.** axPass only shows what KeePassXC's command-line tool reads and passes your changes and your master password to it; KeePassXC, which you install, does all the encryption, under its own licence. Before axPass first unlocks or makes a database it shows a short reminder with a check box: you confirm that axPass is only an access layer and that following KeePassXC's terms is up to you. Help, About, Licence has these reminders and what you agreed to.

axPass opens a KeePass password database (a `.kdbx` file) in a window that NVDA reads well: its groups and entries
on the left, the selected entry's fields on the right. It is part of axKit, a bundle of apps for people who use a
screen reader. It reads and writes the database through KeePassXC's own command-line tool, so KeePassXC must be
installed; axPass never decrypts the file itself.

## Before you start

- Install KeePassXC: when it is missing, axPass's first notice offers Install KeePassXC now, which installs it without setting it to start with Windows. Or install it by hand from https://keepassxc.org/, or in a console: `winget install KeePassXCTeam.KeePassXC --custom "AUTOSTARTPROGRAM=0 LAUNCHAPPONEXIT=0"`.
- KeePassXC's own installer, run by hand or without those options, sets KeePassXC to start with Windows, and its window then opens at every sign-in. axPass does not need that. To turn it off: KeePassXC, Tools, Settings, General, Automatically launch KeePassXC at system startup. If an install from axPass ever sets it, the notice "KeePassXC is installed" says so.
- A database made by KeePass, KeePassXC or KeePassium opens in axPass, and the other way round.
- Start menu: `axKit axPass`. Console: `axPass`, or `axpass C:\Vault\mine.kdbx`. In File Explorer, a `.kdbx` file's
  Open with axKit submenu has axPass.

## Unlocking and locking

- axPass opens the database you had last, or Ctrl+O chooses one with the Windows file picker; Session, New database
  makes one, asking its master password twice.
- The notice "Unlock mine" asks the master password. Its field hides what you type; NVDA calls it protected.
- A wrong password says "The password does not open mine" and asks again.
- Ctrl+L locks: axPass forgets the master password and everything it read, and asks again. It also locks after ten
  minutes without a key or a mouse move anywhere in Windows (Options, Lock after), when you lock Windows (Windows+L) or switch user, when the computer goes to sleep, and when the window closes.

## The window

- **The navigator**, on the left: the groups, then the entries, of the level shown. Right goes into a group, Left or
  Backspace back up; Right on an entry plays the edge sound. Enter opens: a group shows what is in it, an entry moves
  to its fields. Type the start of a name to jump to it.
- **The detail pane**, on the right: the entry's fields, "Title, text, Gmail", "UserName, user name, sam",
  "Password, password, hidden", "URL, text, …", "Notes, long text, …". Tab, Ctrl+Tab or F6 go there; Escape, Tab,
  Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab or F6 come back. A row reads its user name once the entry has been read. Right
  and Left change nothing in this pane and play the edge sound: in the other item apps they step through a choice and
  check or uncheck a check, and a user name or a password never changes on an arrow key.

The password is never spoken, never shown and never written anywhere by axPass.

## Using a password

- Ctrl+B copies the user name.
- Ctrl+C copies the password. axPass marks the copy private, so Windows clipboard history (Windows+V), the cloud
  clipboard and axClip keep no copy of it, and clears the clipboard after 30 seconds, when it locks and when it closes,
  unless you copied something else meanwhile.
- Ctrl+U opens the URL in your browser, when it is a web address; anything else (a file, a network folder) is not
  opened.

What travels where: the master password and every password go to KeePassXC's tool through a private pipe only. When
you save an entry's notes, URL, user name or title, KeePassXC's tool can only take them on its command line, where
other programs you run, and Windows' process auditing if it is on, can see them. Keep secrets such as recovery codes
in a password field rather than in the notes.

## Changing entries


- Enter on a field changes it: Title, UserName, URL and Notes in a notice; Notes is a field of several lines, where
  Enter makes a new line and Tab then Enter on Save keeps it. A field cannot be emptied here: KeePassXC's tool
  takes an empty value as no change, so axPass says "URL cannot be emptied here" and the field keeps its text; empty
  it in KeePassXC's own window. For the same reason Ctrl+Z cannot take back what you wrote into an empty field: it
  says so once, and the next Ctrl+Z takes back the change before it.
- Enter on Password: type a new one, then type it again; or press Generate (Alt+G) for one of 20 letters, digits and
  signs, then Enter. Ctrl+Z does not bring an old password back.
- Every change is written to the database at once, by KeePassXC's tool; there is nothing to save.
- Backups: before the first change after you unlock, axPass copies the database file into a folder beside it, called
  "mine backups" for mine.kdbx, named with the date and time. The newest 20 are kept. Session, Back up now makes one
  whenever you like; Session, Show backups in axFile opens the folder. To use one, open it with Ctrl+O and its master
  password; each is the encrypted file itself. A backup keeps what the database held then, under the master password
  it had then: after you change the master password, or delete an entry because it leaked, delete the older backups
  too (Session, Show backups in axFile).
- Ctrl+M moves the entry to another group, chosen from a list; Ctrl+Z moves it back.
- Ctrl+N makes a new entry here, Ctrl+Shift+N a new group. F2 renames an entry in place: type the new title over the old and press Enter, or Escape. A title cannot hold a slash.
- Delete moves an entry to the Recycle Bin group; Ctrl+Z brings it back. A group moves there after a question, and
  only KeePassXC's own window brings a group back. In the Recycle Bin, Delete removes for good after a question.
- Space selects the entry you are on, and deselects it again; a selected entry reads ", selected", and the status bar
  counts them. Shift+Down and Shift+Up select as you move, Ctrl+A selects every entry of the group, Ctrl+Shift+A or
  Escape deselects all. Delete then moves all the selected entries to the Recycle Bin, "Moved 3 entries to the
  Recycle Bin", and one Ctrl+Z brings them all back. Groups, and what is in the Recycle Bin, are not selected.
- Fields other than KeePass's own ones are shown and are changed in KeePassXC's own window. A group is renamed there
  too.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Back up now; Exit (Alt+F4); New database; Open database (Ctrl+O); Refresh (F5); Show backups in axFile.
- **Actions**: Deselect all (Ctrl+Shift+A); Lock (Ctrl+L); New entry (Ctrl+N); New group (Ctrl+Shift+N); Select all entries (Ctrl+A); Undo (Ctrl+Z).
- **Navigation**: Nothing to move to here yet.
- **Context**: Open (Enter); To the Recycle Bin (Delete).
- **Options**: Filter; KeePassXC's tool; Developer (Open settings file); Lock after (10 minutes, 30 minutes, 5 minutes, 60 minutes, Never); Sort by (Name, The list's own order); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size).
- Keys without a menu item: Ctrl+Slash (Ask about this window); Enter (Change the field); Space (Check the field); Ctrl+Delete (Clear the filter); Delete (Delete); Ctrl+Shift+A (Deselect all); Ctrl+F (Find in the list); F3 (Find next); Shift+F3 (Find previous); Right (Into the list); Ctrl+N (New item); Ctrl+Shift+N (New list); Ctrl+Down (Next section); Enter (Open); Ctrl+Up (Previous section); F7 (Read in full); F2 (Rename); Ctrl+A (Select all); Space (Select or deselect); Shift+Space (Select to here); Shift+Home (Select to the first entry); Shift+End (Select to the last entry); Shift+Down (Select to the next entry); Shift+Up (Select to the previous entry); Ctrl+Tab (Switch between the navigator and the detail pane); Shift+F10 (The Context menu); Shift+F10 (The field's menu); Right (The next value of the field); Left (The previous value of the field); Tab (To the detail pane); Tab (To the navigator); Ctrl+Z (Undo); Enter (Unlock); Left (Up a level).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1 lists every key.
<!-- end of the menus -->

## If something goes wrong

- "KeePassXC is not installed": install it, then start axPass again. If it is installed somewhere unusual, Options,
  KeePassXC's tool tells axPass where `keepassxc-cli.exe` is. A tool outside Program Files is used only when it
  carries the signature of KeePassXC's publisher; any other file is not used and never gets your master password,
  and the log says who signed it.
- "Still working on the last command": a large database takes a moment for each command; wait for the last one.
- Help, Diagnostics, Copy diagnostics puts what is needed for a bug report on the clipboard. It never holds a
  password or anything from the database.

## Filter and sort

Options, Filter (Alt+O, then F) shows only what has a text in its name: type part of a name, Enter, and the list holds only those rows and says so in its name, "Home, 4 items, filter tap, 1 shown". An empty text clears the filter, and so does Ctrl+Delete in the list; going into another list drops it. Options, Sort by puts the list in the order of its names, or back in its own order. Both are the same in every list of axKit's navigators, and neither changes anything that is stored.

## About

axPass is part of axKit by Dr. Kyle Keane, www.kylekeane.com. KeePassXC is a separate project; axPass only runs its
command-line tool. Help, About has the licence and the version.

## Disclaimer

axPass is provided as it is, without warranty of any kind. Keep a backup of your database.
