# axFile — Specification

axFile is an app of the axKit bundle (`docs/axKit/SPEC.md`): a file manager for one folder, for a blind developer who
uses the NVDA screen reader. Specified 2026-10-18 and built as a first draft 2026-10-19; on 2026-10-22 the player it
had carried became axMedia and the preview pane axPreview, so that axFile is file operations and management only
(section 8; the split's inventory of 2026-10-22, in the git history). The document is kept in step with the code (a behaviour change updates it in
the same commit). Requirements are numbered `AF-n.m` and decisions `AF-Dn`; the bundle's rules, AX-7
(accessibility), AX-8 (keys) and AX-9 (the Window, axKit and Help menus), are cited by number and not repeated. In axFile's code and
documents, "SPEC.md" means this file.

## 1. Summary

One folder in one window: a list of its entries, folders first, named with its count (the folder is the title). Enter
does what File Explorer does: a folder opens in the list, a file opens in the program the user has made its default in
Windows (axDown, axJSON, axMedia, axPreview or any other, once chosen there); Shift+Enter reads a file in axPreview;
Backspace goes up. The operations, each one notice or one announcement: new folder, new text file, rename in place,
delete to the Recycle Bin, cut, copy and paste as files, move to a new folder, compress to zip and extract, copy the
path; marks with Space for several at once; sort, filter, Favorites, This PC and the drives. Two panes: the folder
list and, under it, the **Favorites** list of the folders pinned with Ctrl+D, where Enter shows one. Every operation is
kept in the window's history: the status bar shows the newest, Ctrl+Z takes them back newest first, and Actions →
**Recent operations** reads them again and goes to where one left things. Sound and video go to axMedia (Play folder, Add to queue), reading to axPreview, everything else in Windows
is one item away (Open with, Properties, the Windows menu). The window is reached from the other windows on the
folder of the file or project in hand, and sends back. It works entirely offline (bundle G6).

## 2. Goals and non-goals

- **G1 One list, native.** A list box NVDA reads item by item, a status bar, the bundle's notices; no tree, no grid,
  no icons that carry meaning (AX-7.1).
- **G2 Enter is the verb.** A folder opens here; a file opens as it would in File Explorer, in the user's own default
  program; the user learns one key, and the same key in both places.
- **G3 One job.** Files and folders: listing, choosing, moving, naming, deleting, and undoing. Playing is axMedia's,
  reading is axPreview's, editing is the editors'; axFile hands over and says nothing more than it has to (AF-D2).
- **G4 Streamlined.** The plan's worry on record: a file tree's behaviour is endless. The operations are the ones
  named here and no more (AF-D5).
- **G5 Nothing spoken unprompted** (AX-7.3), and nothing answered with silence: a command's result is one short
  announcement, a change of folder names the folder, a key that moves nothing says so (AF-3.1, AF-3.6); Recent
  operations is where the operations can be read again when wanted (AF-9.3).
- **Non-goals.** A tree or a second folder pane (the second pane is the Favorites list, AF-9.1, not a folder view); drag and drop; a properties dialog of our own; search across folders;
  file type associations of our own (Windows' own, AF-D2); a player, a viewer or an editor inside the window (the
  other apps'); a permanent delete (§7).

## 3. Scenarios

1. Start menu, "axKit axFile": the last folder opens, the list focused; NVDA reads "24 items, list, 01 Chapter
   1.mp3, 1 of 24". Up: one short low note, nothing moves. Down twice, Enter: the file opens in the program Windows has
   for `.mp3`, axMedia when the user has made it so, with the folder's other chapters after it. Backspace: "Music, 8
   items, Dune, folder".
2. In axClaude on a project, Alt+S, I, F: axFile opens on the project folder. Down to `docs`, Enter: "docs, 5 items,
   plan.md"; Shift+Enter on `plan.md`: axPreview shows it to read. Backspace:
   "myproject, 12 items, docs, folder".
3. Ctrl+Shift+N, type `notes`, Enter: "Created notes", the folder selected. F2 on it, type `Notes`, Enter: "Renamed to
   Notes". Delete: "Delete Notes? It goes to the Recycle Bin with everything in it.", Enter: "Deleted Notes".
   Ctrl+Z: "Undone: Notes restored", the folder list on Notes; Actions → Recent operations: the first line reads
   "Deleted Notes, undone, 21:04".
4. Ctrl+Shift+C on `report.pdf`: "Copied path"; Enter on it: the PDF opens in the program Windows has for it.
5. Ctrl+F, type `chap`, Enter: the first entry whose name holds it is selected; F3 the next.
6. F12, type `%USERPROFILE%\Downloads`, Enter: the list is the Downloads folder.
7. Space on `a.txt`: "Selected"; Down, Space: "Selected, 2 selected"; Up: "a.txt, selected"; Ctrl+X: "Cut 2 items";
   Enter on `docs`, Ctrl+V: "Pasted 2 items". Later, in another folder, Actions → Recent operations, Down to "Moved
   2 items, 21:05", Enter: the list is `docs` with `a.txt` selected.
8. Space on two folders of chapters, Ctrl+Q: "Added 24 to axMedia's queue"; the focus stays here and the sound
   goes on in axMedia.
9. In `D:\Downloads`, Ctrl+D: "Added to Favorites". Later, in another folder, Tab: "Favorites, list, Downloads
   (D:\Downloads), 1 of 1"; Enter: "Downloads, 12 items, setup.exe", the focus in the folder list.

## 4. Functional requirements

### AF-1 Launch

- AF-1.1 `AxKit.exe file [<folder or file>]` (a file opens its folder with the file selected); the console command
  `axFile`; the Start menu entry "axKit axFile"; axFile under "Open with axKit" on folders and on files in File
  Explorer (AX-2.3). A folder or file given alone to `AxKit.exe` still means axClaude or an editor (AX-1.2); axFile is
  always named by its verb. Without a path: the last folder opened, or the user's profile folder. A last folder that
  is not on a drive of this computer is not looked at during the start (bundle AX-9.5a): the profile folder opens
  and the window says "<path> is on the network and was not opened at start; Open recent opens it." A path given is
  looked at off the window's thread.
- AF-1.2 `AxFileApp.Run(args)` is the entry point (AX-1.7), one call into `AppHost.Run` with the log name `axFile`,
  the usage text, the settings (`axFile.json`) and the window. The process moves its working directory to the
  program's own folder first (the path of the command line already being full): a folder that is some process's
  current directory cannot be renamed or moved while that process runs (Windows' "being used by another process"),
  so a file manager holds none, and the apps it starts inherit none (found 2026-10-22: a folder renamed once could
  not be renamed again while a program had it as its current directory; AF-4.5).
- AF-1.3 **To and from the other windows.** axClaude's Session → Show current folder in ▸ **axFile**, the editors'
  File → Show current file in ▸ **axFile** (the file's folder, the file selected), axGIT's Repository → Show current
  repository in ▸ **axFile**, axTerminal's Session → Show current folder in ▸ **axFile** and axMedia's Queue → Show
  current file in ▸ **axFile** each start a new axFile window (`WindowMenu.Start`, found again in the axKit menu, AX-9.3).
  From axFile, Session → Show current folder in ▸ **File Explorer** (Ctrl+W), **axClaude**, **axTerminal**, **axGIT** (bundle AX-9.10;
  "Folder missing" when the folder shown is gone);
  Context → **Show in File Explorer** (Ctrl+Shift+W) opens File Explorer with the selected entry selected ("Folder
  missing" or "File missing" when the entry is gone); Context → **Preview** (Shift+Enter) and the axMedia items
  are AF-5.

### AF-2 The window

- AF-2.1 From the top: the menu bar; the folder list, named with its count alone, "N items" ("24 items"; "1 item";
  "Empty" for none; "24 items, filter chap, 3 shown" under a filter, AF-3.8); the Favorites list under it, always
  (AF-9.1); the status bar. A notice takes the place of both lists while it shows (AX-6.4). Tab, Shift+Tab, Ctrl+Tab,
  Ctrl+Shift+Tab and F6 move between the two lists (AX-8.2), the window's two controls. The window is the
  bundle's navigator (AF-D14): a folder is a level, its entries the rows, and the Favorites list the second pane.
- AF-2.2 Items: a file by its name as it is; a folder as "<name>, folder"; a drive of This PC as "<label> (C:), drive"
  (AF-3.10). Folders first, then files, each group in
  Windows' natural order (`StrCmpLogicalW`, so that `2` comes before `10`). Hidden and system entries are left out,
  and so are entries whose name starts with a period (`.git`, `.env`), each behind a switch in Options → General,
  **Show hidden files and folders** (`showHidden`) and **Show files and folders starting with a period**
  (`showDotFiles`), both off; a flip reads the folder again. What was left out is counted, and the list ends with one
  line that says so, "Not shown: 3 hidden, 2 starting with a period" (one part when one kind; no line when nothing was
  left out; `FolderListing.NotShownText`, tested), so that the count is there when wanted and never spoken otherwise
  (asked 2026-10-21). The list is read on a background task (AX-7.5); a folder of ten thousand entries fills in one
  update. An empty folder shows one item, "No items in this folder", selected, so that the list is never empty and the
  reason is read (the bundle's rule for empty lists, AX-9.5). Neither that item nor the not-shown line is an entry: a
  command on either says the line itself (AF-7).
- AF-2.3 The title is `axFile folder <the folder's full path>` (the app first, the maintainer's ask of 2026-11-23, bundle AX-9.6; until then `<folder name> - axFile`); `axFile This PC` on This PC. The status bar's left label: the newest file operation's line (AF-9.2)
  ("Copied 3 items", "Compressing, 40 of 120 files"), empty before the first operation; while a folder is being read,
  "Reading…" in its place (the navigator's state for work off the UI thread, framework FW-4.7 point 1, 2026-11-23),
  and after it, while entries are selected, how many, as in every navigator ("Copied 3 items, 2 selected"; "2 selected"
  alone before the first operation; AX-8.16, since 2026-11-22); the right label: the folder's
  full path. No AccessibleName, no Spring (axClaude §6.2).
- AF-2.4 **Information** (F8, the bundle's read with details; Ctrl+I until 2026-11-26; Context → Information): the selected entry on demand, "report.pdf, 1.2 MB, modified
  2026-10-18 14:02" or "docs, folder, modified 2026-10-18 14:02"; the items themselves stay names (AF-D7).

### AF-3 Browsing

- AF-3.1 **Enter** on a folder opens it in the list with its first entry selected. **Backspace** goes to the
  parent with the folder just left selected; at a drive's root, to This PC with that drive selected (AF-3.10), and at
  This PC, "Top". **Right Arrow** on a folder or a drive opens it, as Enter; **Left Arrow** goes up, as Backspace
  (asked 2026-10-22: the keys of a tree on the flat list, AF-D12). Right never opens a file: on a file, or on a line
  that is not an entry, it sounds the edge earcon (AF-3.6), as Left does at This PC. Every change of folder (Enter, Backspace, Go to folder, Open folder, a recent folder or a favorite,
  a drive, This PC, Enter in the Favorites list, an operation chosen in Recent operations) is announced as the folder's name, the list's count and the
  entry landed on, "docs, 5 items, plan.md" ("This PC, 3 items, Local Disk (C:), drive"; "temp, No items in this
  folder"), through one notification that cuts short NVDA's own reading of the new entry (asked 2026-10-22: the entry
  alone did not tell the folder landed in from the folder under the focus; the first build left it to NVDA). The
  start is NVDA's own reading of the window, the list and its entry, and an undo says what it undid instead (AF-4.14).
- AF-3.2 **Enter** on a file opens it with the program the user has made its default in Windows (`ShellExecute`, as
  Enter in File Explorer; AF-D2); when Windows has none, the Windows dialog that picks a program, as File Explorer
  shows it (AF-4.7). The axKit apps register as programs for their kinds when axKit is installed (AX-2.3), so that
  axDown, axJSON, axMedia or axPreview can be chosen there (the guide says how; an installer cannot set a default).
  **Shift+Enter** on a file opens it in axPreview to read (Context → Preview; AF-5.3); on a folder it is File
  Explorer's Shift+Enter, the folder in a new axFile window. Editor and its key Ctrl+E were removed on 2026-11-23 at the maintainer's word ("you can remove editor because i do not even know what that is"): Open with axKit apps, axDown or axJSON, does the same.
- AF-3.3 **Open folder…** (Ctrl+O): the Windows folder picker. **Open recent folder ▸**: the last ten folders opened
  by command, "1. name (path)" (AX-9.5): the start, Open folder, Go to folder, a recent item, and the folder the
  window closed on; Enter and Backspace browse without joining the list, so that a walk through a tree does not fill
  it. **Go to folder…** (F12; Ctrl+G, with Alt+D, File Explorer's key for its address bar, until 2026-11-26): the notice "Go to folder" with the field "Path" holding the current folder with a backslash at its end (empty at This PC), which
  takes a path with environment variables (`%USERPROFILE%\Downloads`) and a file's path (its folder, the file
  selected); "Not found: <path>" when there is no such place. In the field, **Down Arrow** and **Up Arrow** complete the
  path (asked 2026-10-22, AF-D12): the text up to its last backslash is a folder (with environment variables; a
  name without one is in the folder shown; an empty field is the drives), the rest the start of a name; the entries
  of that folder whose names start with it, folders first in natural order and hidden or period-named ones as the
  list shows them, replace the text one per press, a folder with a backslash at its end, each said as "Documents, folder, 2 of 5" or "notes.md, 3 of 5"; past either end the edge earcon and then the
  completion again, as the command palette and axStart do at an end (AX-10.4, 2026-10-23); when no name starts with
  the text, the edge earcon alone. **Right Arrow** at the end of a completed folder lists its own entries, the first one taken, as Right in the
  list goes into a folder. What is typed in between is what the next Down completes. The folder is
  read on a task (`PathCompletion` in the pure code, tested). **New window** (Ctrl+N, File Explorer's key; Session →
  New window): another axFile window on the folder shown, through the bundle's starter (AX-9.2), so that two folders
  can be worked between; Ctrl+N is the other windows' New session, New document and New branch, and a new window of
  any app is the axKit menu's (AX-9).
- AF-3.4 **Refresh** (F5) reads the folder again keeping the selection by name; "Refreshed". Coming back to the window
  after another program changed the folder refreshes it the same way, silently (the list is a view and holds nothing
  unsaved; axDown AD-3.6 is the editors' rule).
- AF-3.5 A folder that is gone when it is asked for (deleted, an unplugged drive) gives the notice "Folder not found"
  naming it, and the list shows the nearest parent that exists.
- AF-3.6 The list's own keys stay native: arrows, Home, End, Page Up and Page Down. **Typing a name** jumps to it as
  in File Explorer (asked 2026-10-22): letters typed within a second of each other build one prefix, "cha", and the
  first entry whose name starts with it is selected, searched from the entry in hand and round the end, NVDA reading
  it; a new prefix begins after a second's pause and is searched from the next entry on; a run of the same letter ("a",
  "aa") cycles through the names starting with it, from the next entry on, as File Explorer does, while any other
  prefix is searched whole, so that a name with a doubled letter inside it ("aab") is found (`AxKit.Core/TypeAhead`,
  tested, the bundle's since 2026-10-24, AX-8.6; until 2026-10-23 the doubled letter made the one-letter cycle); Shift
  with a letter searches the same way backwards (2026-10-24); the edge earcon when no name starts with it (both lists are
  the bundle's `AxKitList` since 2026-10-24, which has the type-ahead and the edge earcon built in). The Favorites
  list takes typed letters the same way, over its lines. The list box's own
  one-letter jump gives way to this (`AxKit.Forms/AxKitList`, AX-8.6); Space is the selection's key (AF-3.7), so a name is typed up
  to its first space. At the ends, where a key moves nothing, the window sounds the **edge earcon** rather than
  nothing (asked 2026-10-22, an earcon rather than words, since "First item" and the entry again were noise): one
  short low note (`Sounds.Edge`, A3 for 60 ms) on Up or Home at the first entry and Down or End at the last, in the
  folder list and the Favorites list alike (`AxFileKeys.FirstItem`, `LastItem` and their seconds; the keys are the
  list's own everywhere else, `AxKitList`). **Find** (Ctrl+F, the bundle's Find notice with the field "Name contains") selects the first entry
  whose name holds the text, F3 and Shift+F3 the next and the previous. NVDA reads the selected entry; the app adds nothing.
- AF-3.7 **Selection** (asked 2026-10-21). The list stays a single-selection list box, where the arrow keys choose
  the current entry (AF-3.6); the selection the operations act on is a set of marks on top of it. **Space** marks the
  current entry, or unmarks it again: its item reads "<name>, selected" while it is marked ("notes.md, selected";
  "docs, folder, selected"), so that an arrow key onto it says so and nothing is said on the others; the key answers
  "Selected" or "Deselected", with ", n selected" when more than that one is marked ("Selected, 3 selected";
  "Deselected, 2 selected"). **Shift+Down**, **Shift+Up**, **Shift+Home** and **Shift+End** (asked 2026-10-21, "the
  Shift multiselect File Explorer has") move the current entry and mark every entry from where the range began to
  there: the range begins where the first Shift key finds the current entry, moving back with Shift shrinks it, and
  the marks made before it stay; an arrow without Shift ends it, as does a change of the list's order or filter or
  a read of the folder, and the next Shift key begins a new one. Each entry passed reads ", selected" by itself and
  the keys say nothing more (only the items whose mark changed are rewritten); on a line after the entries (an
  empty folder's, the not-shown line) the keys say that line. **Shift+Space** (asked 2026-10-22) marks every entry
  from the nearest marked entry to the current one, above or below, the one above when both are as near, "Selected, n
  selected"; with no other entry marked it is Space: the current entry marked, or unmarked again (decided 2026-10-23; it said "1 selected" on the one marked entry and changed nothing). **Ctrl+A** marks every entry, "n selected";
  **Ctrl+Shift+A** unmarks them all, "None selected", and so does **Escape** in the list while any are marked (asked 2026-10-22). F8 adds ", n selected" to the entry's information while
  any are marked (AF-2.4). Cut, Copy, Delete, Copy path, Move to folder, Compress to zip and Add to axMedia's queue
  act on the marked entries when there are any, and on the current entry alone otherwise (AF-4.3, AF-4.4, AF-4.8,
  AF-5.2; kept 2026-10-22 over File Explorer's right-click rule, AF-D10); Enter, Rename, the ways to open and the
  Windows items stay on the current entry. Selection, a submenu of Context (Select all and Deselect all in Actions, since 2026-11-11; a menu of the bar, L, before), is the marks from anywhere, whatever
  entry is current: Select all, Deselect all, Select or deselect this entry, Select to here (Shift+Space), then the
  operations that take the marked entries, Cut, Copy, Delete, Move to folder…, Compress to zip, Add to axMedia's
  queue, Copy path; its operations never fall back to the current entry: "None selected" when nothing is marked (asked
  2026-10-22: one menu for the entry, Item, one for the marks, Selection, one for the folder, Folder, so that the
  target is the menu's name and no mode is needed). A change of the marks is no operation of the window's history
  (it was one, taken back on its own line, from 2026-10-22 to 2026-11-22; AF-9.2): Space on an entry and
  Escape take a selection back, and the status bar says how many are selected (AF-2.3). The marks live in one
  folder: showing another folder drops them, a refresh of the same folder keeps those still there, and a renamed or
  deleted entry loses its mark. The marks are on the entries the list shows: a filter, a new order or a read drops
  the marks on the entries it leaves out, with no line, so that the count said and the entries acted on are one set
  (until 2026-10-23 a mark hidden by the filter was counted and not acted on). The marks, their keys and
  their words are the bundle's since 2026-11-22 (bundle AX-8.16, on their second use, the item windows): `Marks` in
  AxKit.Core (each key one method over the shown rows, returning the rows whose mark flipped and the
  words; tested), the keys in `AxKit.Forms/SelectionKeys`, and `AxKit.Forms/ListSelection`, which rewrites the
  items and says the words; nothing of them is axFile's own since the window is a navigator (AF-D14). A mouse click chooses the current entry as before and marks nothing. The marks are
  text in the items rather than the list box's own multiple selection (AF-D9).
- AF-3.8 **Sort and filter** (asked 2026-10-21). **Options → Sort by ▸** Name, Date, Size, one checked: folders first
  in every order, then by name (the natural order), by date newest first, or by size largest first, ties by name
  (`FolderListing.Sort`, tested); the entry in hand stays selected; "Sorted by date"; the order is a setting
  (`sortBy`) and holds for every folder and the next window. **Filter…** (Options → Filter…; Ctrl+Shift+F until
  2026-11-22, when the filter became the navigator's, the same in every app, and by the maintainer's choice
  without a key: bundle AX-8.17, which is where both are specified now, axFile giving its three orders): the notice
  "Filter" with the field "Name contains" holding the filter in force; Filter lists only the entries whose name
  contains the text, any case, names the list "24 items, filter chap, 3 shown" so that the count is read with the
  list ("3 of 24 items, filter chap" until then),
  and says the name and the entry at once, "24 items, filter chap, 3 shown, Chapter 1.mp3"; an empty field clears it,
  "Filter cleared", as does **Ctrl+Delete** in the list (Escape, once no entry was marked, until 2026-11-26; Escape now only clears the marks, AF-3.7). A filter that nothing passes shows the one item "No names contain <text>", and a command on
  it says the same. The filter lives in the folder: another folder drops it, a refresh keeps it. What the list shows
  is what the commands see: Select all marks the shown entries, find moves among the shown entries, and a mark on an entry the filter hides is gone (AF-3.7). Find (AF-3.6)
  jumps, the filter narrows; both are kept. Play folder (AF-5.1) hands the folder itself to axMedia, filter or not.
- AF-3.9 **Favorites** (asked 2026-10-21). **Ctrl+D** (Navigation → Favorites ▸ → Add or remove this folder) pins the
  folder shown, "Added to Favorites", or unpins it when it is one, "Removed from Favorites"; the browsers' bookmark
  key. **Navigation → Favorites ▸** lists them as "1. name (path)" (AX-9.5) in the order they were added, those that
  still exist, or "No favorites yet", with the key's item under a separator; choosing one shows the folder and it
  joins the recent list. The favorites are a setting (`favorites`, AF-8.1, not sealed), so every window and the next
  start have them. The same folders are the **Favorites list**, the window's second pane (AF-9.1, since 2026-11-23),
  where a folder Ctrl+D adds is selected, so that Tab lands on it.
- AF-3.10 **This PC** (asked 2026-10-21): the level above the drives. Backspace or Left at a drive's root shows it,
  the drive just left selected; its entries are the ready drives in letter order, "Local Disk (C:), drive" (the
  volume's label, or the kind's name, Local Disk, Removable Disk, CD Drive, Network Drive, when it has none;
  `FolderListing.Drives`, tested), each a folder entry marked as a drive; Enter opens the drive's root; F8 says
  "Local Disk (C:), 120 GB free of 476 GB"; the title is "This PC" and the list is named by its count, as everywhere (AF-2.1); Backspace there, "Top". It
  is not a folder: the last folder, the recent list and the Recently closed list keep the folder before it (and Ctrl+N and the restart after an
  update start the new window on the last folder, since This PC is no path; until 2026-10-23 the new window said
  it does not exist), the sort
  order leaves the letter order alone, and the commands that need a folder say "Choose a drive first" (New folder,
  Paste, Rename, Delete, Cut, Copy, Ctrl+D, Play folder, Add to queue, Show current folder in ▸ the other apps); Show
  current folder in ▸ File Explorer opens File Explorer's own This PC. The other items of the entry's menu work on a
  drive as on a folder (Open with axKit apps ▸, Open with axKit access layers ▸, Show in File Explorer, Properties, Windows menu…).
- AF-3.11 **The Drives menu** (asked 2026-10-21, for moving things to a stick and taking it out): **Navigation → Drives ▸**
  (its own menu on Alt+D until 2026-10-22, when it became a submenu at the user's ask and Alt+D went to Go to folder, until
  2026-11-26, AF-3.3) lists the ready drives as "1. Local Disk (C:)" (AX-9.5), each opening the drive's root and joining the
  recent list, then **This PC** (the drives list, AF-3.10, the folder's drive selected), then **Eject this drive**
  and **Drive properties**, both for the drive in hand: the entry at This PC, or the folder's own drive. Eject is
  the shell's own: the drive's context menu is built as for Windows menu… (AF-4.7) and its item whose verb is
  `eject` is invoked (`Shell.Eject`), so a stick, a card or a disc comes out the way File Explorer's Eject does and
  Windows says the rest (safe to remove, or in use); "Ejecting <name>", and the list goes to This PC when it was on
  that drive; "Failed: This drive has no Eject" on a fixed disk, "Failed: <reason>" when Windows refuses. Drive
  properties is the Windows sheet of the drive (space, tools). The menu is refilled each time it opens, since a stick
  may have come or gone. Without a folder, "No folder".
- AF-3.12 **The command palette** (bundle AX-10) holds the window's commands and nothing else, as in every app. From
  2026-10-22 to 2026-11-23 it also listed the entries the folder list shows, and Enter on one opened it; the
  maintainer took them out ("Remove it from axFile too", asked whether every navigator should offer its rows
  there): an entry is found with Options, Filter (AX-8.17) or by typing its first letters in the list (AX-8.6).

### AF-4 Operations

Every operation here is an entry of the window's history (AF-9.2) as well as its announcement.

- AF-4.1 **New folder…** (Ctrl+Shift+N): the notice "New folder" with the field "Name" and the buttons Create and
  Cancel; Create makes it in the current folder, selects it and says "Created <name>". A name refused by AF-4.2's rules
  (the characters, `.` and `..`, a lookalike) is said and the notice stays, so that it can be put right, as every
  one-field notice of the bundle does (`AskText`; until 2026-11-09 the notice closed and the problem was announced
  after it). **New text file**
  (Ctrl+T, Ctrl+Shift+T until 2026-11-26; asked 2026-10-21): an empty file "New file.txt" ("New file - 2.txt" when that is taken,
  `FileOperations.CreateFile`, tested) is made in the current folder and selected, and the rename box (AF-4.2) opens
  on it at once with the whole name selected, not the stem alone, so that the name typed with its extension
  replaces it; Enter renames it, Escape keeps "New file.txt". When the box cannot open (the file hidden by the
  filter, the focus not on the list), "Created <name>" instead. **New Markdown file** and **New JSON file** (Actions,
  no key; asked 2026-11-22: a new file of each kind axKit edits) do the same with "New file.md" and "New file.json",
  empty too, so that Undo removes them as it removes a new text file.
- AF-4.2 **Rename** (F2): in place, as in File Explorer: a box ("New name") opens over the entry holding its name, the
  part before the extension selected (the whole name for a folder), so that typing replaces the name and keeps the
  extension; Enter renames, "Renamed to <new name>", the entry selected under its new name; Escape, or leaving the box
  (a click elsewhere, a notice opening, a change of folder or a refresh, which until 2026-10-23 left the box open over
  the new list), keeps the name with no announcement, the list's reading of the entry being the feedback; the same
  name again, "Name unchanged". A change of case alone is a rename too. The box is a text box
  that is a child of the list, shown only while a rename runs; the first build (2026-10-19) asked in a notice. A name
  that **reads the same** as another entry's without being it, differing only in case plus a trailing or doubled
  space, a different kind of space, an invisible character (a zero-width space, a joiner, a mark) or an accent
  written as one or two characters, is refused with "Reads the same as <that name>, which is already here" and the
  box stays (`FileOperations.Lookalike`, tested; the same for New folder…, AF-4.1, and Move to folder…,
  AF-4.13): Windows would keep both files, and the list would then read one name twice, which the user met on
  2026-10-21 and took for a rename onto an existing name. A name that is exactly taken is Windows' own refusal,
  "Failed: There is already an entry named <name>".
- AF-4.3 **Delete…** (Delete): the notice "Delete <name>? It goes to the Recycle Bin." (for a folder: "… with
  everything in it.") with the buttons Delete and Cancel; the deletion goes through the shell (`SHFileOperationW` with
  `FOF_ALLOWUNDO`, silent and without the shell's own confirmation), so that the Recycle Bin takes it; "Deleted
  <name>", the selection moves to the next entry. With entries marked (AF-3.7): "Delete n items? They go to the
  Recycle Bin." ("…, the folders with everything in them." when one is a folder), one shell operation for them all,
  "Deleted n items", and the first entry after the current one that stays is selected, else the last one before it.
  A permanent delete is not offered (§7).
- AF-4.4 **Copy path** (Ctrl+Shift+C): the full path to the clipboard, "Copied path"; with entries marked, each path
  on a line, "Copied n paths". Not an operation on disk: no entry in the history.
- AF-4.5 A failure (a name taken, a file in use, access denied) is one announcement, "Failed: <Windows' message>",
  when it is one line, a notice otherwise (axGIT AG-7), and the operation's line reads "Failed: <reason>". Two of
  Windows' messages are said with what they mean here, since both come from something outside axFile: "In use by
  another program: a terminal or an app runs in it, or the file is open" (a folder that is a process's current
  directory, axTerminal or axClaude opened on it, a program started from Open with ▸ in it, or a file a program holds;
  verified 2026-10-22, when a folder renamed once could not be renamed again for this reason), and "Access denied: a
  file inside is open in another program, or Windows protects the place". **Every operation runs on a thread of its
  own** (a task for the reads, the rename and the new folder; a single-threaded-apartment thread per shell operation
  for copy, move, delete and the Recycle Bin's Restore) and the window never waits for one: a second paste, delete or
  zip starts while the first runs, each with its own entry in the history (AF-9.2). One runner, `FileForm.RunOperation`, runs each of them:
  its line from Begin to End, the paths it writes or moves away held in flight while it runs, "<doing>, please
  wait" when it passes a second (Pasting, Deleting, Moving, Compressing, Extracting), its failure on the line and
  said, and its result shown and said; an operation that ends at once (a rename, a new folder or file) goes through
  `RunAtOnceAsync` the same way (2026-10-23; five hand-written runners before, Delete and Move to folder without
  the wait notice). Two operations at once are
  kept apart: a paste chooses its names around the paths a running paste is still writing ("notes - 2.md" for the
  same clipboard pasted twice, `FileOperations.Destinations` with the reserved paths), a cut whose files a paste is
  still moving says "Already pasting" rather than moving them twice, and when only some of its files are still
  on their way the others are pasted and the ones left out are counted, "Pasted 2 items, 1 already pasting" (until
  2026-10-23 they were left out without a word), and a compress takes its zip's name at once so
  that a second compress picks the next (AF-4.12). An operation that ends in the folder shown reads the list again
  with its result selected; one that ends while another folder is shown says its result and leaves the list where
  the user is (2026-10-23: it pulled the list back to its own folder, a move the user did not make, AX-7.3).
- AF-4.6 **The entry's menu** (Alt+C; Shift+F10, the Applications key and a right click open the same menu bar menu, AF-D13): the bar's Context menu (AX-9.11) as the list's context
  menu, the same items in the same order, on the selected entry (a right click selects the entry under it first, as
  File Explorer does). It is the window's own menu, fixed, with mnemonics and listed in F1, not the shell's (AF-D8).
  Its shape (AF-6.1; reshaped 2026-10-21 at the user's ask for File Explorer's plain words and one flat list): Open,
  Open with ▸, Open with axKit apps ▸, Open with axKit access layers ▸, Preview, Play folder in axMedia, Add to axMedia's queue | Cut, Copy | Rename,
  Delete | Move to folder…, Compress to zip, Extract here | Copy path, Show in File
  Explorer, Information, Properties | Windows menu… | Selection ▸; Paste, New folder and New text file act on the folder and are in Actions (2026-11-11). There is one menu, the bar's Context menu, the standard drop-down
  (AX-6.6); the list has no context menu of its own (AF-D13). On a line that is no entry (an empty folder's, the
  not-shown line) Context holds one item, that line's words, in place of commands that could only say them (the
  navigator's rule, since 2026-11-22; the entry's keys still say the line there, AF-7).
- AF-4.7 **Windows' own ways**, items of the entry's menu, each one of the system's own windows like the folder picker
  (AX-7.2): Open with ▸ **Windows default** opens the entry with the program Windows has for it (a folder in File Explorer),
  or, when Windows has none, the Windows dialog that picks a program and runs it, as File Explorer does on Enter;
  **Choose a program…** shows that dialog always ("Not a file" on a folder); **Properties** (Alt+Enter, File
  Explorer's key) shows the Windows Properties sheet of a file or a folder; **Windows menu…**, the last item,
  shows the shell's own context menu for the entry, File Explorer's classic menu with every installed program's
  entries (`IShellFolder` and `IContextMenu` through P/Invoke, tracked as a native popup the window owns, its first
  item selected, the shell's submenus such as Send to filled through the window's messages), and runs what is chosen.
  A refusal by Windows is "Failed: <reason>"; closing the dialog or the menu is not one.
- AF-4.8 **Cut, Copy, Paste** (Ctrl+X, Ctrl+C, Ctrl+V, in the list; Context): the entry, or the marked
  entries (AF-3.7), go on the clipboard as files in File Explorer's own formats (`CF_HDROP` and "Preferred
  DropEffect", move for a cut, copy otherwise), with the paths as text too, one per line, so that a text field pastes them, 2026-10-23), "Cut <name>" or "Copied <name>", "Cut n items" or "Copied n items",
  so that File Explorer can paste them and axFile can paste what File Explorer cut or copied (cut and copy touch no
  disk and have no line, AF-9). Paste copies the clipboard's files into the folder shown, or moves them after a cut,
  through the shell (`SHFileOperationW`, undo allowed; the names are chosen first, `FileOperations.Destinations`, a
  name already there becoming "name - 2", "name - 3", one destination per source with `FOF_MULTIDESTFILES`, rather
  than a question, nothing overwritten; `FileOperations.CopyOrMoveAsync`, tested): "Pasted <name>" or "Pasted n
  items", the first one selected, the line "Copied n items" or "Moved n items"; "Pasting, please wait" when it takes
  longer than a second; after a move the clipboard is cleared, as File Explorer does. "Nothing to paste" without
  files on the clipboard; "Already in this folder" for a cut entry pasted where it is; "Already pasting" for a cut
  whose files a paste is still moving (AF-4.5).
- AF-4.9 **Open with axKit apps ▸** (A) **and Open with axKit access layers ▸** (S; one Open in axKit ▸ until
  2026-11-23, the maintainer: "you can call open with axKit Apps and open with axKit Access Layers as separate
  things"): one item per app of the bundle that opens a path (`Dispatch.Apps` whose `Opens` is not Nothing), the
  standalone apps in the first and the access layers in the second, in the bundle's groups with their separators
  (AX-9.2), a new window of that app on the entry by what it opens (`Dispatch.PathFor`): axDown, axJSON,
  axPreview and axPass on the file ("Not a file" on a folder), axMedia on the file with its folder's media files after
  it, or on a folder's (axMedia hands them to its open window itself), axGIT on the repository that holds the entry,
  axFile on the folder (or the file in its folder), axClaude, axTerminal and axTask on the folder, or on the entry's
  folder for a file.
- AF-4.10 (Ctrl+I inside the open context menu) went with the context menu on 2026-10-23 (AF-D13): the bar's Item
  menu is a standard drop-down and does not take it; F8 in the list says the entry.
- AF-4.11 **The user's own programs in Open with ▸** (asked 2026-10-21): a list of programs and scripts, kept in the
  settings (`programs`, a name and a path each, AF-8.1), shown under Windows default and Choose a program… (AF-4.7)
  as "1. name (path)" (AX-9.5), or "No programs of your own yet" while the list is empty. Options holds **Add a program to Open with…**
  and **Remove a program from Open with ▸** (under Open with until 2026-11-11, a menu three deep, AX-9.1). Add a program… opens the
  Windows file picker on programs and scripts (`.exe`, `.cmd`, `.bat`, `.ps1`, `.py`; every file behind the second
  filter), then the notice "Add a program" with the field "Name" holding the file's name without its extension and
  the buttons Add and Cancel; "Added <name>"; a blank name says "Type a name" and the notice stays (`AskText`,
  2026-11-09). Remove a program ▸ lists them by name; choosing one takes it off,
  "Removed <name>". Choosing a program runs it once on the selected entries, or the current one (AF-3.7), every path
  an argument, in the first entry's folder: a `.ps1` through Windows PowerShell by its full path (`-NoProfile
  -ExecutionPolicy Bypass -File <script>`), since Windows opens a script file in an editor; a `.bat` or `.cmd` program
  is refused an entry whose name holds one of cmd.exe's special characters (`& | < > ^ % ! ( ) , ; =` or a quote),
  which cmd would run as a command ("<name> not started", security audit S-3); anything else as the shell starts it
  (`ShellExecute` with the arguments, so a batch file and a script whose type has a program registered run too);
  "Started <name>", or "Failed: <reason>" when it cannot start (the file gone, no program for the type). The list is
  refilled each time the submenu opens, so two windows see each other's additions after a restart only (the settings
  are the window's, AF-8.1). It is the shape the user-defined shells of axTerminal took (docs/axTerminal/SPEC.md
  AT-1.6a, 2026-11-11): a list in the app's settings, added through a notice for the name (here after the file
  picker; there followed by a notice for the command line), removed from a submenu.
  `UserPrograms` in `AxFile.Core` is tested; the menu and the start are the window's.
- AF-4.12 **Compress to zip, Extract here** (Context; asked 2026-10-21), through .NET's own `ZipFile`, no
  package (`Archives` in `AxFile.Core`, tested). **Compress to zip** puts the selected entries, or the current one
  (AF-3.7), into a new zip in the folder shown, named after the one entry without its extension ("notes.zip") or
  after the folder for several ("Dune.zip"), "name - 2.zip" when the name is taken (the name is taken the moment the
  compress starts, so two at once never share one), a folder's contents under its name; "Compressed to <name>", the
  zip selected; "Compressing, please wait" when it passes a second, the operation's line in the status bar
  counting the files meanwhile (AF-9.2). **Extract here** on a `.zip` makes a new folder named after it next to it
  ("Dune", or "Dune - 2" when taken) and extracts into it, so that nothing is written over; "Extracted to <name>",
  the folder selected; "Not a zip file" on anything else. A failure is "Failed: <reason>" (AF-4.5). Other archive
  kinds are the shell's (Windows menu…) or an installed program's (AF-4.11).
- AF-4.13 **Move to folder…** (Context; asked 2026-10-21, "like zip but not compressed"; named Move to new
  folder until 2026-10-23, when the user asked for Go to folder's completion in it): the notice "Move to folder" with
  the field "Folder" and the buttons Move and Cancel, its text naming what moves ("a.txt will move into the folder
  typed …"). Down and Up complete the text as in Go to folder (AF-3.3), folders and drives alone, Right going into a
  completed folder. Move: a folder that exists (a name here, a relative or a full path) takes the selected entries, or
  the current one (AF-3.7); any other name is a new folder made here first (the name rules and "Reads the same as …"
  of New folder); a path to a folder that does not exist elsewhere is "Not found: <text>"; a folder moved into itself
  or its own subfolder is "Cannot move a folder into itself". The move is the shell's (AF-4.8, undo allowed, names
  chosen first); "Moved <name> into <folder>" or "Moved n items into <folder>", the folder selected when it is here.
- AF-4.14 **Undo** (asked 2026-10-21). **Ctrl+Z** (Actions → Undo, one name whatever it would take back; it was named
  after what it would undo, "Undo rename", until the maintainer's decision of 2026-11-25: one Undo that says what it
  undid, so that one name means one thing across apps, docs/axKit/command-language.md section 8 (c)) takes back the newest file operation of the window's history that can be taken back (AF-9.2; never a
  change of the marks, decided 2026-10-22, so that Ctrl+Z after a paste does not unmark an entry instead), one per
  press, from either list (since 2026-11-23, AF-D3), and selects the entry it touched: Rename, renamed back; New folder and New text file, deleted while still empty ("Not undone:
  notes has files now" otherwise); a paste that moved and Move to folder, moved back where they were, the new
  folder removed when it is empty; a paste that copied, Compress to zip and Extract here, the copies, the zip or the
  extracted folder sent to the Recycle Bin; Delete, brought back from the Recycle Bin, the shell's own undelete on the
  Recycle Bin's item, the one step that needs the shell again (the bin's items are matched by the original path their
  `$I` record holds, the latest deletion of each, and restored in one Restore command; "Not in the Recycle Bin any
  more" when none is there). "Undone: <what>" each time ("Undone: renamed back to <name>", "Undone: <name> removed",
  "Undone: moved back", "Undone: <what> sent to the Recycle Bin", "Undone: <what> restored"), its line reading ",
  undone" afterwards (in the status bar and Recent operations); an entry changed since by anything else (renamed in File Explorer meanwhile), "Not undone:
  <name> has changed", "Not undone: <name> has files now" / "has content now" (a new folder or file filled
  meanwhile), "Not undone: <name> is already here" (the place it would go back to is taken; a rename that changed
  only the case is not "already here"), the line saying so and dropped from what Undo takes next, as is a delete
  whose entries are "Not in the Recycle Bin any more" (emptied meanwhile); a failure of the undo itself is "Failed:
  <reason>" (AF-4.5) and the line stays to try again (a zip open in another program, closed and Ctrl+Z again); a second Ctrl+Z while one runs says "Undo is still running"; in the rename box Ctrl+Z is the
  box's own (AF-6.2). Eject, Open, the hand-overs, Favorites and the settings are not operations and have no undo;
  there is no redo. The last twenty operations of this window can be undone, gone with the window. The lines and the
  operations are `ActionLog`, `Operation` and `FileOperations.CheckUndo` and `UndoAsync` in `AxFile.Core`, tested
  over a temporary folder; the Recycle Bin's records are `RecycleBin`, tested on synthetic records; the Restore itself
  is `Shell.Undelete`, checked by a console run on this machine.

### AF-5 Sound, video and reading: the hand-overs

axFile plays nothing and shows no text (G3, AF-D2): it starts the app on the paths, as the Window menu starts a window,
and the started process finds its open window and hands the paths over with the bundle's window message
(`AxKit.Forms/AppMessage`; axMedia AM-1.2, axPreview AP-1.2). Nothing of it is on the network (G6).

- AF-5.1 **Play folder in axMedia** (Ctrl+Enter in the list; Context): axMedia on the folder shown, its sound
  and video files in name order from the first, the axMedia window in front; "No sound or video files here" when the
  list shows none (`MediaFiles.IsMedia` over the shown entries); "Choose a drive first" at This PC.
- AF-5.2 **Add to axMedia's queue** (Ctrl+Q in the list, Ctrl+Shift+Q until 2026-11-26; Context): the entry, or the marked entries
  (AF-3.7), to the end of axMedia's queue, a folder as its media files, with `--add`; the focus stays here and axFile
  says "Added n to axMedia's queue", n being its own count of the media files among the entries (a folder read on a
  task, the hidden ones as the settings say); "No sound or video files to add" when the count is none. With no axMedia
  window open, one opens behind this window, without taking the focus (AM-D3), holding the queue for later.
- AF-5.3 **Preview** (Shift+Enter in the list; Context): the file read in axPreview, the window in front; a
  folder in a new axFile window (AF-3.2). axPreview shows every file through its renderer (axPreview AP-7): a file
  that is no text as the readable text inside it, with the Details list under it (AP-7.2, AP-7.5); it said "Not a
  text file" until 2026-10-24.
- AF-5.4 **Enter** on a sound or video file is Windows' default program (AF-3.2): axMedia when the user has made it
  so, which plays the file with its folder's other media files after it (AM-D2).

### AF-6 Menus, keys, texts

- AF-6.1 Menus (AX-9.1, the bundle's eight since 2026-11-11): **Session (S):** New window Ctrl+N | Open folder…
  Ctrl+O | Open recent folder ▸ | ─ | Refresh F5 | ─ | Show current folder in ▸ (File Explorer Ctrl+W | axClaude |
  axTerminal | axGIT) | ─ | Exit Alt+F4. **Actions (A):** Undo Ctrl+Z (AF-4.14) | Recent operations… (AF-9.3) | ─ | New folder… Ctrl+Shift+N |
  New text file Ctrl+T | New Markdown file | New JSON file | Paste Ctrl+V | ─ | Select all Ctrl+A | Deselect all Ctrl+Shift+A. **Navigation (N):**
  Favorites ▸ (1. name (path) … or No favorites yet | ─ | Add or remove this folder Ctrl+D) | Drives ▸ (1. Local Disk
  (C:) … | ─ | This PC | ─ | Eject this drive | Drive properties; AF-3.11) | Go to folder… F12 | Up one level
  Backspace | ─ | The Favorites list (AF-9.1). **Context (C)**, by the list that last had the focus (AX-9.11): in the
  folder list, the entry's menu (AF-4.6): Open Enter | Open with ▸ (Windows default | Choose a program… | ─ | 1. name
  (path) … or No programs of your own yet) | Open with axKit apps ▸ (the standalone apps that open a path, by group) |
  Open with axKit access layers ▸ (the access layers that open a path, by group) | Preview
  Shift+Enter | Play folder in axMedia Ctrl+Enter | Add to axMedia's queue Ctrl+Q | ─ | Cut Ctrl+X | Copy Ctrl+C |
  ─ | Rename F2 | Delete Delete | ─ | Move to folder… | Compress to zip | Extract here | ─ | Copy path Ctrl+Shift+C |
  Show in File Explorer Ctrl+Shift+W | Information F8 | Properties Alt+Enter | ─ | Windows menu… | ─ | Selection ▸
  (Select or deselect this entry Space | Select to here Shift+Space | ─ | Cut Ctrl+X | Copy Ctrl+C | Delete Delete | ─ |
  Move to folder… | Compress to zip | Add to axMedia's queue Ctrl+Q | Copy paths Ctrl+Shift+C; AF-3.7, the
  operations on the marks alone); in the Favorites list, its line (AF-9.1): Open Enter | Remove from Favorites
  Delete. **Options (O):** Filter… | Sort by ▸ (Name ✓ | Date | Size) | ─ | Add a program to Open with… | Remove a program from Open with ▸ | ─
  | Text ▸ (Font… | Larger text Ctrl+Plus | Smaller text Ctrl+Minus | Windows text size) | General ▸ (Check for updates
  at start ✓ | Show hidden files and folders | Show files and folders starting with a period) | Developer ▸ (Open
  settings file). **Window (W)**, **axKit (K)** (AX-9.1). **Help (H):** Keyboard shortcuts F1 | User
  guide | ─ | File resources ▸ (Keyboard shortcuts in Windows, the Microsoft page, in the browser). Every item has a
  mnemonic (AX-7.4).
- AF-6.2 Keys (AX-8). Bundle-wide: F1, the control keys (Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6, between
  the folder list and the Favorites list, AF-9.1), Ctrl+F, F3, Shift+F3, Ctrl+S ("Nothing to save here"), Ctrl+Plus,
  Ctrl+Minus, Page Up and Page Down (the list's own here), Alt+F4. Window-specific (`AxFileKeys`): Enter,
  Shift+Enter, Backspace, Left, Right, F5, F2, Delete, Ctrl+N, Ctrl+O, F12, Ctrl+D, Ctrl+W, Ctrl+Shift+W,
  F8, Ctrl+Shift+N, Ctrl+T, Ctrl+Shift+C, Escape, Ctrl+Delete (clear the filter), Shift+Up, Shift+Down, Shift+Home,
  Shift+End, Space, Shift+Space, Ctrl+A, Ctrl+Shift+A, Ctrl+Q (AF-5), Shift+F10, the Applications key,
  Alt+Enter, Ctrl+Z (AF-4.14), Ctrl+X, Ctrl+C, Ctrl+V. Enter, Shift+Enter, Backspace, Left, Right, Space, Escape, F2, Delete,
  Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+A, Ctrl+Shift+A, Shift+Space, Ctrl+Enter, Ctrl+Q and the Shift arrows
  act only while the folder list has the focus (in the Favorites list, Enter, Delete, F8 and Escape
  act on its line, AF-9.1; Ctrl+Z is Undo in either list; in a notice's field or the rename box they are the field's);
  Shift+F10 and the Applications key open the bar's Context menu from either list, on the entry or on the Favorites
  list's line (AF-D13, AX-9.11; in the second pane since 2026-11-22). The keys between the two lists, Right and Left, the
  selection's keys, the two menu keys and Escape are the navigator's own commands (`ItemKeys`, `SelectionKeys`; AF-D14);
  `AxFileKeys` names them for this window. The collision test covers the table and lists the keys other windows use
  otherwise (Ctrl+N, F12, Ctrl+C, Ctrl+Shift+C). Ctrl+Enter is the bundle's main action
  (AX-8.10, `BundleKeys.MainAction`), here Play folder in axMedia (AF-5.1) while the folder list has the focus, the edge
  sound elsewhere.
- AF-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axFile/user-guide.md` is embedded (Help → User
  guide); About is the bundle's (AX-9.9).

### AF-7 Announcements

One UI Automation notification each, on the list that has the focus, or on the notice while one shows (AX-7.3):
"<folder>, <N items>, <entry>" (a change of folder, AF-3.1), "<N of M items, filter text>, <entry>" (AF-3.8), "Created
<name>", "Moved <what> into <name>" (AF-4.13), "Renamed to <name>", "Deleted <name>", "Deleted n items",
"Copied path", "Copied n paths", "Cut <name>", "Copied <name>", "Cut n items", "Copied n items", "Selected",
"Deselected", "Selected, n selected", "Deselected, n selected", "n selected", "None selected" (AF-3.7), "Sorted by
<order>", "Filter cleared" (AF-3.8), "Added to Favorites", "Removed from Favorites" (AF-3.9), "Top" (This PC,
AF-3.10), "Ejecting <name>" (AF-3.11), "Added <name>", "Removed <name>", "Started <name>" (AF-4.11), "Compressed to
<name>", "Extracted to <name>", "Compressing, please wait", "Extracting, please wait" (AF-4.12), "Pasted <name>",
"Pasted n items", "Pasted n items, m already pasting" (AF-4.5), "Pasting, please wait" (a paste longer than a second), "Deleting, please wait", "Moving, please wait" (the same, AF-4.5), "Added n to axMedia's queue", "No sound or
video files here", "No sound or video files to add" (AF-5), "Undone: …", "Not undone: …", "Nothing to undo", "Undo is
still running" (AF-4.14), "No operations yet", "Still running", "Folder not found" (Recent operations, AF-9.3),
"No favorites yet", "<path>, n of m" (F8 in the Favorites list, AF-9.1), "Refreshed", "Not found: <path>", "Failed: <message>", "<name>, <size>,
modified <date>" (F8), "Not found" (find), "Nothing to save here", "Text size n", "Windows text size", "Copied
diagnostics", "<name>, <size>, modified <date>, n selected" (F8 with marks), "Failed: In use by another program: a
terminal or an app runs in it, or the file is open", "Failed: Access denied: a file inside is open in another program,
or Windows protects the place" (AF-4.5); and the guards "No folder" (nothing
listed yet), "No items in this folder" (a command on an empty folder's one item, AF-2.2), "No names contain <text>"
(the same under a filter, AF-3.8), "Not shown: …" (the same on the list's last line, AF-2.2), "Nothing selected",
"Choose a drive first" (a folder command at This PC, AF-3.10), "Not a file" (Open with… on a folder), "Not
a zip file" (Extract here, AF-4.12), "Name unchanged" (a rename to the same name), "Nothing to paste" (no files on the
clipboard), "Already in this folder" (a cut entry pasted where it is), "Already pasting" (a cut still on its way,
AF-4.5), "Reads the same as <name>, which is already here" (AF-4.2), "Type a name" and the other reasons of `FileOperations.CheckName`. A new selection is NVDA's own
reading. One earcon, the edge (`Sounds.Edge`, one short low note): a key that moves nothing at either end of a list,
and a typed name no entry starts with (AF-3.6).

### AF-8 Settings, log, status

- AF-8.1 `%APPDATA%\AxKit\axFile.json` (AX-5.3; `FileSettings` on `WindowSettings`): `lastFolder`, `recentFolders`
  (ten), `favorites` (the pinned folders in the order added, AF-3.9; blanks and repeats dropped), `showHidden` off,
  `showDotFiles` off (AF-2.2), `sortBy` name (`name`, `date` or `size`, AF-3.8), `programs` (the user's own, each a
  `name` and a `path`, AF-4.11; an entry without either is dropped), and the window base's font, update check and
  window fields. The player's keys of an older file (`speed`, `seekSeconds`, `sayEachFile`) are axMedia's now
  (`axMedia.json`, with its own defaults) and are ignored here.
- AF-8.1a **The user's own programs are sealed** (bundle AX-5.6, 2026-11-16, `repairs.md` R-53): the file's `seal`
  covers every program's name and path. A file whose seal does not check offers none of them, keeps every other
  setting, and says once, as a notice at the start: "Your programs of Open with in axFile's settings were changed
  outside axKit and are not used. Add them again from Options, Add a program to Open with." A file from before the
  seal is accepted and sealed at its next save. `FileSettings`, tested in `FileSealTests`.
- AF-8.2 The log is `%LOCALAPPDATA%\AxKit\logs\axFile.log`: the start, the exit, the errors of the operations with
  Windows' message; no names of files or folders.

### AF-9 The Favorites list and the history of operations

The maintainer, 2026-11-23: "axfile should have two panes i think it should have a favorites and the current file
tree. you can get rid of the actions." The second pane is the Favorites list since that day; the history of the
operations stays, behind a command (AF-D3).

- AF-9.1 **The Favorites list.** A second list under the folder list, "Favorites", with the folders pinned with
  Ctrl+D (AF-3.9), one line each in the order added: the folder's name, then its full path, "Music (C:\Music)"; a
  drive's or a share's root by its path alone (`FileSettings.FavoriteLine`, tested). It is there from the first moment,
  with the one line "No favorites yet" until the first (AX-9.5; a pane that appears later leaves Tab and Shift+Tab
  with nowhere to go), hidden only while a notice shows; a third of the window. **Tab**, **Shift+Tab**, **Ctrl+Tab**,
  **Ctrl+Shift+Tab** and **F6** move between the folder list and it (AX-8.2), **Escape** in it goes back to the
  folder list, Navigation → **The Favorites list** puts the focus in it. **Enter** (or a double click) shows the
  folder in the folder list with its first entry selected and moves the focus there before the folder is said
  (AF-3.1), so that NVDA's reading of the list does not cut the announcement short; the folder joins the recent list,
  as a favorite chosen from the submenu does; a favorite that is gone gives the "Folder not found" notice and its
  nearest parent (AF-3.5). **Delete** takes the favorite out of the favorites, "Removed from Favorites"; the folder
  itself is not touched, and there is no undo (the favorites are a setting, AF-4.14). **F8** (Ctrl+I until 2026-11-26) says the
  favorite's full path and place, "C:\Music, 2 of 3". On "No favorites yet", Enter, Delete and F8 say that line.
  Shift+F10 and the Applications key open Context with the line's Open and Remove from Favorites (AX-9.11). Type-ahead
  goes over the lines (AF-3.6). Ctrl+D anywhere in the window adds the folder shown and selects it here, so that Tab
  lands on it, or takes it out again; nothing is spoken for the list filling again (the command's own words are what
  is heard). The list is filled from the settings without looking at the disk, so that a favorite on the network
  never holds the window; a second window sees a favorite added in the first at its next start (the settings are the
  window's, AF-8.1). Navigation → Favorites ▸ lists the same folders with numbers (AF-3.9).
- AF-9.2 **The history of operations.** Every file operation of this window is an entry of its history
  (`ActionLog`, tested), in time order, the last fifty kept and the last twenty undoable, gone with the window: "Renamed
  notes.md to todo.md", "Created temp", "Created New file.txt", "Moved 3 items into bundle", "Copied 3 items", "Moved
  2 items", "Compressed to docs.zip", "Extracted to docs - 2", "Deleted notes.md", "Deleted 2 items". A running
  operation reads what it is doing, "Copying 3 items", "Deleting 2 items", "Compressing, 40 of 120 files" (a few times
  a second at most; a paste through the shell has no count to give), and its line is replaced when it ends; a
  failure, "Failed: <reason>"; an entry that was undone, "Renamed notes.md to todo.md, undone"; one that could not be
  undone when asked, "…, not undone: notes.md has changed". The status bar's left label is the newest entry's line
  (AF-2.3), so that a running zip's count is there with NVDA+End. Nothing is spoken for an entry (the operation's own
  announcement, AF-7, is what is heard). A change of the selection has no entry (AF-3.7); Cut, Copy, Copy path, Open,
  the hand-overs and Favorites change nothing on disk and have none. Ctrl+Z takes back the newest entry that can be,
  one per press, in either list (AF-4.14).
- AF-9.3 **Recent operations.** Actions → **Recent operations…** shows the history in a notice "Recent operations"
  with the bundle's list, newest first, each line with its time, "Copied 3 items, 21:04" (a running one "Copying 3
  items, running"); "No operations yet" is said instead before the first. **Enter** (the notice's Choose) goes to where
  the chosen operation left things: the folder list shows the folder and selects the entry it made, renamed or moved
  (the new folder for a move into one, the first entry for a paste; the folder alone for a delete), and takes the
  focus before the folder is said (AF-3.1); once the operation is undone, where the undo put things back; "Still
  running" for a running one, "Folder not found" when the place is gone. **Escape** closes the notice where the focus
  was. The list is what the history held when it opened. An operation is taken back with Ctrl+Z only, newest first;
  there is no undo of one line out of order and no redo.

## 5. Architecture

```
src/AxFile.Core/    class library (net10.0): FolderListing (a folder's entries as records, folders first in natural
                    order, hidden left out; where a typed name jumps, the editor for a file; the list's name, the
                    information line, the parent, a typed path, the drives of This PC), FileOperations (the name
                    check, new folder, rename, delete to the Recycle Bin, copy and move into a folder, the last three
                    through SHFileOperationW, the undo checks and inverses, the messages), Operation and
                    OperationKind (one operation with what its undo needs and where it left things), ActionLog and
                    ActionEntry (the history of operations, its states, the limits), RecycleBin (the $I records; the
                    marks on the shown entries are AxKit.Core's Marks since 2026-11-22, AX-8.16),
                    UserPrograms and UserProgram (the user's own programs; AF-4.11), Archives (a zip from entries, a
                    zip into a new folder, free names; AF-4.12), FileSettings (with the favorites); references AxKit.Core (MediaFiles for
                    the hand-over counts, NaturalOrder, Dispatch for the editor's verb)
src/AxFile/         class library (net10.0-windows): AxFileApp (Run, Usage), FileForm (the window, on ItemForm, AF-D14,
                    in seven partial files: the controls, menus, hooks and keys; Browse; Completion; Favorites;
                    Operations; Programs; History), AxFileKeys,
                    HelpText (F1 text, the embedded guide), Shell (Windows' own ways with an entry: the
                    Open with dialog, the Properties sheet, the shell's context menu through IShellFolder and
                    IContextMenu, Eject, the Recycle Bin's Restore; AF-4.7); references AxFile.Core, AxKit.Core, AxKit.Forms
tests/AxFile.Tests/ FolderListingTests, FileOperationsTests, UndoTests, ArchivesTests, PathCompletionTests, ActionLogTests,
                    UserProgramsTests and FileSettingsTests over temporary folders (the
                    Recycle Bin is not exercised), ActionLogTests, UserProgramsTests, FileSettingsTests
docs/axFile/        this spec, user-guide.md (embedded), nvda-test-plan.md
```

`FileForm` derives from the window base (AX-6.5): the notices, the fonts, the window's place, find in a list and the
update flow are the base's. The folder reads and the operations run on background tasks, the shell's delete on a
single-threaded-apartment thread of its own (AX-7.5). The player seam, the queue and the text rule live in AxMedia.Core
and AxPreview.Core since the split; `MediaFiles` and `NaturalOrder` moved to `AxKit.Core` on their second use.

## 6. Design decisions

- **AF-D1 One list, no tree; two lists, no tabs.** A tree view asks a screen reader user to track depth and expansion;
  a list of one folder with Backspace for up is what File Explorer's main pane is, and NVDA reads a list box best. The
  path in the status bar is the depth. The Favorites list is a second list under it rather than a tab control: the
  bundle's control keys already move between a window's controls, and a tab control would be a third thing to learn.
- **AF-D2 Enter is Windows'; playing and reading are other apps'.** The first build routed Enter itself (`.md` to
  axDown, `.json` to axJSON, media to its own player) and carried the player and later a preview pane as features of
  this window (the first AF-D2: a folder's files are a playlist and the list was already here). The user's direction
  of 2026-10-22: axFile is file operations and management only; playing is axMedia's and viewing axPreview's, each an
  app with a tight hand-over; and Enter on a file opens it in the program the user has made its default in Windows,
  exactly as a double click in File Explorer does, the axKit apps registering as programs for their kinds so that they
  can be chosen there (an installer cannot set a default: Windows lets only the user). The costs are known and
  accepted: a second window for the sound, and a Windows setting to make before Enter plays in axMedia. The gains:
  one player for the whole bundle, a viewer with the whole window and the editors' keys, a file manager with one job,
  and a list that mirrors what Explorer does. Shift+Enter is the viewer's own key, since it has no other meaning on a
  file in a list; the hand-overs are one window message (`AppMessage`, AM-D4), nothing on the network.
- **AF-D3 The favorites in the pane, the history behind a command.** Undo came first as a hidden history of twenty
  operations (2026-10-21); the user's ask of 2026-10-22, a list of every operation taken, to review, undo and jump
  to, made the history visible as the second pane under the folder list: one line per operation with its state,
  Ctrl+Z on any line, Enter to its place. On 2026-11-23 the maintainer asked for two panes, the favorites and the
  current file tree, and let the pane of operations go. The second pane is now the Favorites list (AF-9.1): the
  places one goes back to, which were a submenu only (AF-3.9). The history stays, without a pane (AF-9.2, AF-9.3):
  Ctrl+Z takes back the newest operation, as before, now from either list; the status bar shows the newest line, a
  running count included; Actions → Recent operations lists the operations newest first in the bundle's notice with a
  list, where Enter goes to the chosen one's place. A notice was chosen over a submenu because a submenu filled as it
  opens cannot be reached by the automated checks nor read by the AI tools as a list, and over a third pane because
  the maintainer asked for two. What went with the pane: Ctrl+Z on one line whatever its place (undo is in order,
  newest first), Ctrl+I's section and number (the time is in the line), and the running line under the focus (its
  count is in the status bar).
- **AF-D5 Streamlined operations.** New folder, rename, delete and copy the path were the first four; cut, copy and
  paste came at the user's ask on 2026-10-20, as files on the clipboard in File Explorer's own formats (`CF_HDROP`
  and the preferred drop effect) so that the two exchange them, with the shell doing the copy or the move (a dash and
  a number on a name already there rather than a question, §7); the zip, the move into a new folder and Undo on
  2026-10-21. File Explorer stays one key away for the rest (Ctrl+W, Ctrl+Shift+W).
- **AF-D7 Items are names; details on demand.** "report.pdf, 1.2 MB, 2026-10-18" on every arrow key is noise for a
  user who wants the name; F8 says the rest when asked (AX-7.3).
- **AF-D8 Our own context menu, Windows' behind one item.** Asked 2026-10-20: the shell's context menu on every entry,
  or a menu of our own. The shell's menu is built by every installed program's shell extension, loaded into this
  process: a slow or broken one hangs or crashes the window, and its contents and order change with what is
  installed and with Windows updates, under the user's fingers. The window's own entry menu (Context since 2026-11-11) is fixed, has mnemonics,
  is listed in F1 and is covered by the collision test, so it is the context menu (AF-4.6), and the two things people
  reach for in the shell's menu, Open with and Properties, are items of it (AF-4.7). The whole shell menu stays one
  explicit item away, Windows menu…, for the rest (Send to, an archiver's entries), chosen knowingly.
- **AF-D9 Marks in the items, not the list box's multiple selection.** Asked 2026-10-21: select and deselect with
  Space. A multiple-selection list box (`LBS_MULTIPLESEL`) does that natively, but its arrow keys move a caret the
  app has to read and set through messages for everything "current" (Enter, F2, the folder's first entry, find), a
  screen reader may add "not selected" to every unmarked entry, and whether it speaks the toggle at all was not
  known. The marks are kept by the window and written into the items (", selected"), which NVDA reads as part of
  the name, so that the single-selection list the NVDA pass covered stays as it was and the feedback is one
  announcement of the app's own; a sighted helper sees the word.
- **AF-D10 The marks win; three menus name three targets.** Asked 2026-10-22, "how do you recommend we manage" the
  target of a key when some entries are marked and the current one is not, without File Explorer's modes. Offered: the
  entry you hear decides (a marked current entry takes the whole selection, an unmarked one itself alone, File
  Explorer's right-click rule, audible on every arrow key since the item says ", selected"); the marks win when there
  are any (the rule since 2026-10-21); the keys on the current entry only, the marks through the menu. The user chose
  to keep the marks winning, and the menus carry the rest: Item is the entry, Selection is the marks (its operations
  say "None selected" rather than fall back), Folder is the folder. The decision is recorded so that the first rule
  is not proposed again.
- **AF-D11 Sections as header lines in one list** (2026-10-22 to 2026-11-23). The pane of operations had the running
  operations pinned at the top under a header line, then the operations, and until 2026-11-22 the changes of the
  marks (they left it then, docs/axKit/framework.md section 9: the selection is the navigator's, the same in every
  app). With the pane gone (AF-D3), Recent operations is one list, newest first, a running operation marked ",
  running" in its line rather than in a section of its own.

- **AF-D12 No tree; the tree's keys on the list, and completion in Go to folder.** Asked 2026-10-22: a tree to drive
  through folders and drives, or an AxOpen app with a tree and typing. A tree (always, or as a pane beside the list as
  File Explorer has it) adds a control to lose one's place in, and its one gain of its own, selecting across folders,
  is rare. The flat list takes the tree's Left and Right instead, and typing a path gets completion where a path is
  typed, the Go to folder field. No separate app: the other windows reach axFile through Show current folder in; if
  an editor wants a quick-open, the completion moves to the shared pieces on its second use.

- **AF-D13 One Item menu, opened by every way.** 2026-10-23: the list's own context menu (Shift+F10, the Applications key, a right click) never read its first item the first time it opened, whether shown from code, with its first item selected, with a Down posted into it, or through the list's own WM_CONTEXTMENU (which leaves nothing selected), while Alt+I read the bar's Item menu every time. At the user's word the three ways open the bar's Item menu (the bar's Context menu, Alt+C, since 2026-11-11, AX-9.11): the window posts SC_KEYMENU and then the letter I, as Alt and I typed, once Shift, F10 and the Applications key are up (with them down F10 closed the menu and the letter was Shift+I, a beep). Ctrl+I inside the menu (AF-4.10) went with the context menu.

- **AF-D14 The window is the bundle's navigator.** 2026-11-22 (docs/axKit/framework.md FW-4.8, step 13; the
  maintainer: every app on one of a few window types, axFile unified with the item navigator). axFile had a window
  of its own, the last app that did. Since then `FileForm` derives from `ItemForm`: a folder is a level and each
  shown entry a row (a folder a list, a file an item, the row's key its path); the folder list is the navigator,
  the pane of operations its second pane as a log (the Favorites list since 2026-11-23, AF-D3); the two lists, the split between them, the status bar, the marks of
  the selection (AX-8.16), the pane keys, Right and Left, the Context keys and Escape are the navigator's. What
  changed for the user is small and named where it applies: the status bar ends with how many entries are selected
  (AF-2.3), Context on a line that is no entry holds that line's words (AF-4.6), and Shift+F10 and the Applications
  key open the line's menu in the second pane (AF-6.2). Enter, Backspace, F2, Delete, find,
  refresh and undo stay axFile's own commands, each on the navigator's condition for its place, with the words,
  the order and the marks they had (opening a file, a rename and a delete are acts an AI tool asks about first,
  where the navigator's own Open and Rename are not); the rename box and its steps are the navigator's, axFile
  saying how much of the name is selected, which names it refuses, and renaming (AF-4.2). The folder is still read
  on a background task and shown when the read is in (`EntryRefused` starts the read and the level stays until
  then).

AF-D4 (the volume) and AF-D6 (the queue fixed at Enter) went to axMedia with the player (AM-D6, AM-D2).

## 7. Open questions

- A question on a name collision when pasting (replace, keep both, skip), where axFile now adds a dash and a number
  (AF-4.8); a paste of many items that names them.
- Shift+Delete, a permanent delete, after its own question.
- A "media only" filter of the list (the name filter and the sort orders came 2026-10-21, AF-3.8).
- Keeping the history of operations for the next start of the window on the same folder.
- A key for Recent operations (none yet, 2026-11-23); moving a favorite up or down in the Favorites list; the
  Favorites list of one window following a favorite added in another without a restart.

## 8. Status

- Planned 2026-10-18 at the user's ask (as axMedia, then as axFile's player).
- Built 2026-10-19 as a first draft, on the branch `axFile`: the window, the four operations, the player on MFPlay, the
  wiring into the bundle, the tests of the pure code, the guide and the test plan.
- 2026-10-20: the entry's menu (AF-4.6) and Windows' own ways (AF-4.7, AF-D8); File operations with cut, copy and
  paste (AF-4.8), the rename in place (AF-4.2), Open with axKit apps and access layers (AF-4.9), Enter's fallback to the Open with dialog
  (AF-3.2, AF-4.7), Ctrl+I inside the open menu (AF-4.10).
- Released as 3.1.0 on 2026-10-21; the user's NVDA pass of the whole test plan passed the same day.
- 2026-10-21, after the release: the one item of an empty folder (AF-2.2), Ctrl+N (AF-3.3), the selection with Space
  (AF-3.7, AF-D9), the user's own programs (AF-4.11), Sort by and the filter (AF-3.8), Favorites (AF-3.9), This PC
  (AF-3.10), Compress to zip and Extract here (AF-4.12), the list named by its count (AF-2.1), the two switches with
  the not-shown line (AF-2.2), "name - 2" (AF-4.8), New text file (AF-4.1), Move to folder… (AF-4.13), the entry's
  menu as one flat list (AF-4.6, AX-6.6), the Drives menu (AF-3.11); then the queue pane, Undo (AF-4.14), the preview
  pane, the lookalike guard (AF-4.2) and the Shift range (AF-3.7); released in 3.2.0, 3.2.1 and 3.2.2. Not yet checked
  with NVDA from Ctrl+N on.
- 2026-10-22: the split, on the branch `split` (the split's inventory of 2026-10-22, in the git history): the player became axMedia and the
  preview axPreview; Enter follows Windows' default program (AF-D2); the pane became the list of operations (AF-D3)
  with Enter to the action's place and Ctrl+Z on a line, at the user's ask; the Player menu, its keys, the video area,
  the queue and the settings `speed`, `seekSeconds` and `sayEachFile` left the window; `MediaFiles` and `NaturalOrder`
  moved to `AxKit.Core`. The window shrank by a third. Not yet checked with NVDA (test plan sections 3.14, 4 and 6).
- 2026-10-22, later: the operations side by side (AF-4.5), at the user's ask: a second paste chooses its names around
  a running one, a cut on its way says "Already pasting", a compress takes its name at once; the process's working
  directory moved off the user's folders (AF-1.2) and Windows' "being used by another process" said with its cause,
  after the user found a folder renamed once could not be renamed again: verified by experiment that a process whose
  current directory the folder is (a terminal or an app started in it) is what refuses, and that axFile itself holds
  nothing (renaming twice in a row works, test `A_folder_is_renamed_twice_in_a_row_and_a_current_directory_says_in_use`).
- 2026-10-22, later: a change of folder announced with the folder's name, count and entry (AF-3.1), "First item" and
  "Last item" at the ends of either list (AF-3.6), the filter's name and entry said (AF-3.8), at the user's ask: the
  entry alone did not tell the folder landed in from the folder under the focus, and silence at an end told nothing;
  then File Explorer's type-ahead, several letters into one prefix (AF-3.6), at the user's ask.
- 2026-10-22, later: the edge earcon in place of "First item", "Last item" and "No names start with" (AF-3.6, the
  bundle's `Sounds.Edge`), the list of operations there from the start with its one line until the first (AF-D3), so that Tab and
  Shift+Tab always have the second control, and a pass over the F1 text, the guide and the notes to put every feature
  in one place each, at the user's ask ("I have lost track of all the features").
- 2026-10-22, later: Shift+Space (AF-3.7), the Selection menu with the operations on the marks (AF-3.7, AF-6.1,
  AF-D10), the changes of the marks as lines of the list of operations, taken back on their line (AF-D11), and the
  list in three sections under header lines, the running operations first (AF-D11), at the user's ask;
  specified first, built the same day. Not yet checked with NVDA (test plan 3.7b, 6.8, 6.9).
- 2026-10-22, later: Escape deselects (AF-3.7); Left and Right as a tree's keys and completion in the Go to folder
  field (AF-3.1, AF-3.3, AF-D12), at the user's ask; then the Drives menu as a submenu of Folder and Alt+D for Go to
  folder (AF-3.11, AF-6.1). Not yet checked with NVDA (test plan 1.4b, 1.7, 1.12a, 3.7a).
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axKit/nvda-test-record.md`.
- 2026-11-23, on the branch `autonomous/2026-11-23`, at the maintainer's word ("axfile should have two panes i think it
  should have a favorites and the current file tree. you can get rid of the actions."): the second pane is the
  Favorites list (AF-9.1), Enter to a favorite and Delete to take one out; the pane of operations went, the history
  staying behind Ctrl+Z, now from either list, the status bar and Actions → Recent operations (AF-9.2, AF-9.3, AF-D3).
  Not yet checked with NVDA (test plan section 6).
