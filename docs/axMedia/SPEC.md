# axMedia — Specification

axMedia is an app of the axKit bundle (`docs/axKit/SPEC.md`): a player for sound and video files with one queue, for a
blind developer who uses the NVDA screen reader. Specified and built 2026-10-22 at the user's ask, released in 3.3.1 and checked with NVDA the same day (the record: `docs/axKit/nvda-test-record.md`): the player
that axFile had carried since 2026-10-19 (`docs/axFile/SPEC.md` AF-5) moved here whole, so that axFile is file
operations and management only and playing, like viewing, is another app's with a tight hand-over (AM-D1). The
document is kept in step with the code (a behaviour change updates it in the same commit).
Requirements are numbered `AM-n.m` and decisions `AM-Dn`; the bundle's rules, AX-7 (accessibility), AX-8 (keys) and
AX-9 (the Window, axKit and Help menus), are cited by number and not repeated. In axMedia's code and documents, "SPEC.md" means this
file. The inventory of the split of axFile, what leaves it, what is shared and what is new, with the order of the
commits, is the split's inventory of 2026-10-22, in the git history; axPreview (`docs/axPreview/SPEC.md`) is the other half of it.

## 1. Summary

One queue in one window: a list of the files to play, in order, the one playing marked, named with its count. Enter
plays the file under the selection and the rest follow; the transport keys pause, seek, skip, change the speed and say
the position on demand; the keyboard's own keys set the volume; what plays is in the status bar and the title. The
window is reached from axFile (Enter on a sound or video file once axMedia is the user's default program for it,
Play folder, Add to queue, Open with axKit apps), from File Explorer (Open with axKit on a media file, or as the default
program), from the Start menu, the Window menu and the command line. A start with files while an axMedia window is
open hands them to that window rather than opening another (AM-1.2). A video's picture is drawn in an area at the
top of the window. It works entirely offline (bundle G6).

## 2. Goals and non-goals

- **G1 One list, native.** A list box NVDA reads item by item, a status bar, the bundle's notices (AX-7.1).
- **G2 The queue is the thing.** A folder of files in order is a playlist; so is what was added from anywhere. The
  queue is fixed when play starts and the user is free to roam elsewhere (AM-D2).
- **G3 One player.** axFile and the others hand files to the axMedia window that is open; a second window is a
  deliberate act (the axKit menu's Apps, or its key).
- **G4 Streamlined.** What axFile's player had and no more (AF-5): the transport, the speed, the position; the rest
  waits (§9).
- **G5 Nothing spoken unprompted** (AX-7.3) but two things the user asked for by playing the queue: the name of the
  next file when the player moves on (a setting, AM-4.3), and "End of queue" with the done chime when it ends (AM-3.3).
- **Non-goals.** Tags (ID3), album art, an equalizer, shuffle and repeat, streaming addresses, a library or a
  search, reordering the queue by key (§9), playlist files (§9), a picture beyond a plain area, a preview or a viewer
  of anything that is not sound or video (another app's, when one exists).

## 3. Scenarios

1. In axFile on a folder of chapters, with axMedia the default program for `.mp3` in Windows' settings, Enter on
   `03 Chapter 3.mp3`: axMedia opens with the folder's media files as the queue, the third playing, the window in
   front: NVDA reads "03 Chapter 3.mp3 - axMedia window", then "Queue, 12 items, 03 Chapter 3.mp3 (playing), 3 of
   12". Ctrl+Enter: "Paused at 12:40". Ctrl+Enter: "Playing". Ctrl+Left: "12:25". Alt+Up: "Speed 1.25". F7:
   "Playing 03 Chapter 3.mp3, 12:52 of 45:30, speed 1.25". Without that default, Enter opens the file in whatever
   plays it on the machine, and Context → Open with axKit apps → axMedia does the same as above.
2. The chapter ends: "Playing 04 Chapter 4.mp3" and the next plays; the window can be left in the background, Alt+Tab
   or the Window menu brings the keys back.
3. Back in axFile, Space on two folders and Ctrl+Shift+Q: "Added 24 to axMedia's queue"; the sound goes on and the
   focus stays in axFile.
4. Start menu, "axKit axMedia": the window opens with "Queue is empty"; Ctrl+O, pick three files: "Added 3", the first
   selected; Enter: "Playing <name>". Ctrl+Shift+O on a folder: its media files are appended.
5. In File Explorer, right-click an `.mp3`, Open with axKit, axMedia: the file plays with its folder's media files
   after it.
6. Enter on a video file: "Playing <name>", the picture appears at the top of the window; Ctrl+Enter pauses it.
7. A file Windows cannot decode: "Cannot play clip.ogg: this kind of file is not supported" and the next plays.

## 4. Functional requirements

### AM-1 Launch and the hand-over

- AM-1.1 `AxKit.exe media [<file or folder>...] [--add] [--new]`: a file is played with its folder's other media files
  after it, in name order (AM-3.2); a folder's media files are played from the first; several paths are appended in
  that order and the first plays; with `--add` the paths are appended and nothing starts; no path opens the window
  with an empty queue (§9 for the last queue). Start menu entry `axKit axMedia`, console command `axMedia`, File
  Explorer's Open with axKit ▸ axMedia on files of the media kinds (AM-4.1) and axMedia under Windows' own Open with for
  them (type name "Media file (axMedia)"), so that the user can make it the default program for a kind in Windows'
  settings (the installer cannot; the guide says how), the axKit menu's Apps (AX-9). A media file alone on the
  command line (`AxKit song.mp3`) starts axMedia (AX-1.2).
- AM-1.2 **One player.** A start with paths, from axFile, from File Explorer (a double click once axMedia is the
  default, or Open with axKit) or from the command line, hands them to the axMedia window that is open, the most
  recent by Z order when there are several, and ends; it opens a window of its own only when there is none, or with
  `--new` (the axKit menu's Apps passes it). A play brings the window to the front, since the user chose to
  open the file in its app; an add leaves the focus where it is, and axFile says "Added 24 to axMedia's queue"
  (AM-D3). axFile does nothing special: it starts the app on the paths, with `--add` for Add to queue; Enter on a
  media file is Windows' default program, axMedia when the user has chosen it (`docs/axFile/SPEC.md` section 9).
- AM-1.3 **The hand-over** between the starting process and the open window is one window message, `WM_COPYDATA`,
  to the axMedia window found by its title's ending through the same window list the Window menu reads
  (`AxKit.Core/AxKitWindows`), carrying a verb (`play`, `add`; axPreview's `open`, axStart's `show`) and the paths, one
  per line; the window answers 1 once it has taken the message, and the starting process, which waits up to five
  seconds for the answer, ends on it (AM-D4). The sending and the receiving are
  one shared piece, `AxKit.Forms/AppMessage`, which axPreview uses the same way. Nothing listens on the network (G6).
  Hand-overs that arrive while one is being applied wait in a queue and apply in the order they came (one queue
  drained in turn since 2026-10-23; a chain of tasks before).
- AM-1.4 Ways out: Show current file in ▸ File Explorer (no key, Ctrl+W until 2026-11-26: the playing file, or the file under the selection,
  selected in File Explorer; "File missing" when it is gone, bundle AX-9.10), axClaude and axTerminal (in its folder), axGIT (the repository that holds it) and axFile
  (the same file selected in its folder, a new axFile window; `docs/axFile/SPEC.md` AF-1.1), the bundle's one submenu
  (AX-9.10). The axKit menu (AX-9).

### AM-2 The window

- AM-2.1 One list, "Queue", named with its count ("Queue, 12 items"; "Queue is empty" as its one item when empty,
  AX-9.5): the files in order as "03 Chapter 3.mp3", the one playing or paused marked "03 Chapter 3.mp3 (playing)".
  The Up and Down arrow keys, Home, End, Page Up and Page Down and a typed letter are the list's own; nothing is
  spoken on them (AX-7.3). The window is a navigator, the bundle's item window (`docs/axKit/framework.md` FW-4.9,
  2026-11-22; a window of its own until then): the queue is its one level, the files its rows, and it has no
  second pane. So the list, its selection (AM-3.5), find, the Context menu and the keys of a navigator are the
  shared ones: Right, Left and Backspace, which go into a list and up a level in other navigators, play the edge
  sound here, where there is neither; F2, Ctrl+Z, F5 and Ctrl+Shift+N, a navigator's rename, undo, refresh and new
  list, are no keys of this window, since a queue has no use for them.
- AM-2.2 The status bar: on the left the state, the file and the time while something plays, "Playing 03 Chapter
  3.mp3 12:40 / 45:30", moving every second; on the right the place and the speed, "3 of 12, speed 1.25"; both empty
  otherwise. While files are selected the left part ends in how many, ", 3 selected" (AX-8.16; "3 selected" alone
  while nothing plays). The labels have no accessible name and no spring (AX-6.2).
- AM-2.3 The title: "<name> - axMedia" while a file plays or is paused, "axMedia" otherwise; the Window menu names the
  window by it (AX-9).
- AM-2.4 A video's picture is drawn in an area at the top of the window (not focusable; never a window of its own,
  AX-7.2), two fifths of the height while a video plays or is paused, hidden otherwise; its sound goes the same way.
  The area is black, the colour of a video surface everywhere, so that a picture's letterbox is not the window's
  colour: the one colour an app sets (AX-7.7; decided 2026-10-23).

### AM-3 The queue

- AM-3.1 **In the list:** Enter plays the file under the selection ("Playing <name>"); Delete takes it out ("Removed
  <name>"; the one playing goes on to the next, or stops at the end); F8 (Ctrl+I until 2026-11-26) says the file's place, "03 Chapter
  3.mp3, 3 of 12", with the position while it is the one playing ("…, playing, 12:52 of 45:30"); Ctrl+F finds by name
  (the bundle's find, AX-8.2). Queue → Clear queue empties it and stops, "Queue cleared".
- AM-3.2 **Adding:** Queue → Add files… (Ctrl+O), the Windows file picker on the media kinds, several at once,
  appended in the picker's order, "Added 3"; Queue → Add folder… (Ctrl+Shift+O), the Windows folder picker, the
  folder's media files appended in name order, hidden ones left out, "Added 12", "No sound or video files in
  <folder>" when none; Queue → Open recent ▸, the last ten files and folders added by command or handed over as
  one path (a start, a play or an add from axFile), "1. name (path)" (AX-9.5), one chosen appended again. A start
  with paths (AM-1.2): a file replaces the queue with its folder's media files from that file
  (AM-D2) and plays; a folder replaces it with the folder's from the first; `--add` appends the paths, a folder as
  its media files. Nothing starts playing on an add when nothing was playing.
- AM-3.3 When a file ends the next plays, to the end of the queue: "End of queue" with the done chime (the shared
  `Sounds`). Files already played stay in the queue until it is cleared or they are removed.
- AM-3.4 The queue is the window's: closing the window drops it (§9 for keeping it).
- AM-3.5 **Selecting files** (2026-11-22; the bundle's selection, AX-8.16, which states the keys and the words:
  the maintainer, "space in axmedia should select"; framework step 6). **Space** selects the file you are on, or
  deselects it again, its line then ending in ", selected" ("beta.wav, selected"; the one playing, "alpha.wav
  (playing), selected"); **Shift+Down, Shift+Up, Shift+Home, Shift+End** select as they move; **Shift+Space** from
  the nearest selected file to here; **Ctrl+A** every file (Actions, Select all); **Ctrl+Shift+A**, and **Escape**
  while any is selected, none. With files selected, **Delete** takes them all out of the queue, whatever file
  you are on, "Removed 3 files"; when the file playing is among them the next file left plays, or the sound stops
  at the end. With nothing selected Delete takes out the file you are on, as before. Enter, F8 and the ways
  out stay on the file you are on; F8 adds ", n selected" while any are. A new queue (a play that replaces
  it, Clear queue) starts with none selected; files added keep what was selected. A file queued twice is two
  rows, each selected by itself. On the empty queue's line the keys say that line. The window's part is
  `MediaForm`'s `Selection`, the bundle's `ListSelection`; the script is `tests/scripts/axMedia/07-selection.txt`.
- AM-3.6 **Filter and sort** (2026-11-22; the navigator's, bundle AX-8.17). Options → **Filter…** shows only the files whose name contains a text; a row under the filter is still its own file of the queue, so Enter plays that file and F8 says its place in the whole queue, and Ctrl+Delete in the queue clears the filter (Escape, once nothing was selected, until 2026-11-26). Options → **Sort by ▸ Name** reorders the queue itself (the maintainer's choice of that day, "Sort that reorders the queue": a list shown in another order than it plays in would mislead), in Windows' natural order; the file playing stays the one playing, at its new place, and what plays next is what follows it there; files added later take their place by name. **The order added** puts the queue back in the order the files came. The choice is kept for the window, not saved (`PlayQueue.OrderBy`, tested).

### AM-4 Playing

- AM-4.1 **Media files**, by extension (`MediaFiles.Extensions`, tested): sound `.mp3`, `.wav`, `.wma`, `.m4a`,
  `.m4b`, `.aac`, `.flac`, `.ogg`, `.opus`, `.aiff`; video `.mp4`, `.m4v`, `.mov`, `.avi`, `.wmv`, `.mkv`, `.webm`.
  Whether Windows plays one depends on its decoders (the Store's extensions for some); a file that cannot be opened
  says "Cannot play <name>: <reason>" and the player moves to the next.
- AM-4.2 **Engine**: Windows' own media stack through one P/Invoke seam, `MediaPlayer` in `AxMedia.Core`, no package
  (B-10): Media Foundation through MFPlay (AM-D5). The file is opened synchronously, so that one that cannot be opened
  says so at once; playing starts when the engine reports the item set; its events come back on the window's thread.
  A rate other than 1 keeps the pitch. A rate the engine refuses: "Speed n is not supported here", and the speed
  stays: the rate is kept only once the engine has accepted it, and a rate accepted for one file and refused by the
  next falls back to 1 for that file, which the status bar and F7 show, since they read the player's rate (until
  2026-10-23 the refused rate was kept, and the next file played at 1 under a status bar showing the old speed). An
  event of a file replaced before it was set (two plays in quick succession) is told from the new file's by the item
  it names, and left alone (2026-10-23; the old file's failure skipped the new one). A Windows without the media components (an N edition without the Media Feature Pack has no `mfplay.dll`)
  gives "Cannot play <name>: the media player could not be created: Windows' media components are missing…", not a
  crash (2026-10-23).
- AM-4.3 **Keys**, from anywhere in the window: **Ctrl+Enter**, the bundle's main action key, plays, pauses or resumes (AM-D7): with nothing playing it plays the file you are on, "Playing <name>" (since 2026-11-22, the maintainer: "axMedia should also use ctrl+enter to play, pause, or resume"; "Nothing playing" until then), and "Queue is empty" with nothing queued; otherwise it pauses or resumes
  ("Paused at 12:40" / "Playing"); **Ctrl+Right** and **Ctrl+Left** move 15 seconds forward or back (`seekSeconds`),
  saying the position reached, "12:25", the file's end at most (until 2026-10-23 the position asked for, past the
  end included); **Ctrl+Shift+Right** and **Ctrl+Shift+Left** the next and the previous file ("Playing <name>"; "First
  file" and "End of queue" at the ends); **Alt+Up** and **Alt+Down** faster and slower by 0.25
  between 0.5 and 3 ("Speed 1.25"), Play → Normal speed back to 1, the speed kept in the settings for the next file
  and the next window; **F12** Go to time…, the notice with the field "Time" (`12:40`, `1:02:10`, or `750` for
  seconds), the position reached, "12:40", when done; **F7** What is playing: "Playing <name>, 12:52 of 45:30, speed 1.25", "Paused …"
  or "Nothing playing". "Say each file as it starts" (`sayEachFile`, default on; Options) is the one
  unprompted announcement (G5). The volume is Windows' own (AM-D6). Stop as a command is not offered: Ctrl+Enter pauses,
  Clear queue stops.
- AM-4.4 Closing the window stops the player.
- AM-4.5 The time format is `m:ss` under an hour and `h:mm:ss` over (`MediaFiles.FormatTime`, tested).

### AM-5 Menus, keys, texts

- AM-5.1 Menus (AX-9.1, the bundle's eight since 2026-11-11): **Session (S):** New window Ctrl+N | Add files… Ctrl+O |
  Add folder… Ctrl+Shift+O (Open files and Open folder until 2026-11-25: they add to the queue, and Open folder is
  axFile's for a folder opened in its list, docs/axKit/command-exceptions.json) | Open recent ▸ | ─ | Clear queue | ─ | Show current file in ▸ (File Explorer |
  axClaude | axTerminal | axGIT | axFile) | ─ | Exit Alt+F4. **Actions (A):** Play, pause or resume Ctrl+Enter | Forward 15
  seconds Ctrl+Right | Back 15 seconds Ctrl+Left | ─ | Faster Alt+Up | Slower Alt+Down | Normal
  speed | ─ | What is playing F7. **Navigation (N):** Next file Ctrl+Shift+Right | Previous file Ctrl+Shift+Left |
  Go to time… F12. **Context (C)**, the file selected (AX-9.11; Shift+F10, the Applications key and a right click in
  the queue): Play Enter | Remove Delete | Information F8 | ─ | Select or deselect Space | Select to here Shift+Space; one
  item, "Queue is empty", the empty line's words, while it is empty ("Nothing in the queue" until 2026-11-22). **Options
  (O):** Say each file as it starts ✓ | Text ▸ | Developer ▸ (Open settings
  file) | ─ | the settings of all of axKit (AX-9.1, 2026-11-26). **Window (W)**, **axKit (K)** (AX-9.1). **Help (H):** Command palette… F1 | Keyboard shortcuts | User guide | ─ | Media resources ▸ (Formats Windows plays, Microsoft's
  page on Media Foundation; Free codecs in the Microsoft Store, a Store search for media extensions; each in the
  browser). Every item has a mnemonic (AX-7.4).
- AM-5.2 Keys (AX-8). Bundle-wide: F1 and Ctrl+Comma, the command palette with the keys (Ctrl+Shift+P until
  2026-11-26), the control keys (one control, so they stay in the list), Ctrl+F, F3, Shift+F3,
  Ctrl+S ("Nothing to save here"), Ctrl+Plus, Ctrl+Minus, Alt+F4. Window-specific (`AxMediaKeys`): Enter, Delete,
  Space and the other selection keys, and Shift+F10 in the list (AM-3.5), which are the navigator's keys
  (`ItemKeys`, `SelectionKeys`), named in the table by what they do in a queue, Ctrl+Up and Ctrl+Down between sections among them (the queue has
  none, so they play the edge sound) and Ctrl+Delete, clear the filter; Ctrl+Enter, F8, Ctrl+O, Ctrl+Shift+O, Ctrl+N (New window, AX-9; Ctrl+W, File Explorer, until 2026-11-26), Ctrl+Right, Ctrl+Left,
  Ctrl+Shift+Right, Ctrl+Shift+Left, F12, Alt+Up, Alt+Down from anywhere in the window, with the bundle's F7 What is
  playing (in a notice's field they are the field's own, AX-7.2). The collision test covers the table.
- AM-5.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axMedia/user-guide.md` is embedded (Help → User
  guide); About is the bundle's (AX-9.9).

### AM-6 Announcements

One UI Automation notification each, on the list, or on the notice while one shows (AX-7.3): "Playing <name>",
"Paused at m:ss", "Playing" (resumed), "m:ss" after a seek or Go to time, "Speed n", "Speed n is not supported here",
"First file", "End of queue", "Nothing playing", "Playing <name>, m:ss of m:ss, speed n", "Paused <name>, …", "Cannot
play <name>: <reason>", "Removed <name>", "Queue cleared", "Added n", "No sound or video files in <folder>" (a folder
opened or handed over; "… in <file>" for one file that is none, "… in n paths" for several), "No sound or video files
to add" (a recent file that is gone), "<name>, n of m", "<name>, n of m, playing, m:ss of m:ss" and "…, paused, m:ss of
m:ss" (F8), "Queue is empty" (a command on the empty line), "Not a time", "Not found" (find), "Nothing to save
here", "Text size n", "Windows text size", "Copied diagnostics". In axFile: "Added n to axMedia's queue", "No sound or
video files to add", "No sound or video files here"
(`docs/axFile/SPEC.md`). A new selection is NVDA's own reading.

### AM-7 Settings, log, errors

- AM-7.1 `%APPDATA%\AxKit\axMedia.json` (AX-5.3; `MediaSettings` on `WindowSettings`): `speed` 1, `seekSeconds` 15,
  `sayEachFile` on, `recent` (the last ten files and folders added by command or handed over), and the window base's font, update
  check and window fields. axFile's `speed`, `seekSeconds` and `sayEachFile` are not carried over: the defaults are
  theirs.
- AM-7.2 The log is `%LOCALAPPDATA%\AxKit\logs\axMedia.log`: the start, the exit, the player's errors with Windows'
  message; no names of files.
- AM-7.3 Errors are one announcement when the reason is one line ("Cannot play …") and a notice otherwise (AX-7.2);
  a crash is the bundle's report (AX-1.7).

## 5. User interface

The menu bar; the video area at the top while a video plays; the queue list filling the window; the status bar. The
notices (Go to time, Keyboard shortcuts, User guide, About, the update notice) are the bundle's overlay in place of
the list (AX-7.2). The window has the accessible role Window, so that NVDA names it on opening (AX-7.1).

## 6. Architecture

```
src/AxMedia.Core/   class library (net10.0): MediaPlayer (MFPlay through P/Invoke: open, play, pause, position,
                    length, speed, the started, ended and failed events, a window handle for video; moved from
                    AxFile.Core unchanged), PlayQueue (the paths, the index playing, Set, Add, Remove, MoveOn, the
                    lines; moved from AxFile.Core without the status lines, which stay axFile's), MediaFolder (a
                    folder's media files in name order, hidden ones left out), MediaSettings; references AxKit.Core
src/AxMedia/        class library (net10.0-windows): AxMediaApp (Run, Usage), MediaForm (the window, on ItemForm, the
                    bundle's navigator, since 2026-11-22: the queue as its one level, the video area, the status bar and its clock, the menus, the keys, the player's
                    events, the hand-over received through AppMessage; grown from axFile's FileForm.Player.cs and the
                    queue part of FileForm.Pane.cs), AxMediaKeys, HelpText; references AxMedia.Core, AxKit.Core,
                    AxKit.Forms
tests/AxMedia.Tests/ PlayQueueTests (moved), MediaFolderTests over a temporary folder, MediaSettingsTests, the
                    player's reason texts; the player itself is checked by ear
docs/axMedia/       this spec, user-guide.md (embedded), nvda-test-plan.md; the inventory of the split is in the git history
```

Shared on the second use (AX-6.3), unchanged: `MediaFiles` (the extensions, `IsMedia`, the time format and the
typed time, the speed steps) and `NaturalOrder` move from `AxFile.Core` to `AxKit.Core`, since axFile keeps asking
which entries are media (Enter, Add to queue) and axMedia orders a folder's files as axFile does; `MediaFiles.Queue`
over axFile's entries becomes `MediaFolder` in AxMedia.Core. New in `AxKit.Forms`: `AppMessage`, the `WM_COPYDATA`
hand-over (AM-1.3), sent by axFile and received by `MediaForm`. The player is driven from the UI thread and its
events are posted back to it (AX-7.5).

## 7. Design decisions

- **AM-D1 A separate app after all.** axFile's AF-D2 (2026-10-18) made the player axFile's feature, since a folder's
  files are a playlist and the list was already there. The user's direction of 2026-10-22: axFile is file operations
  and management; viewing and playing are handled elsewhere, with tight integration. The costs are known and
  accepted: the transport keys are in another window (Alt+Tab, or the Window menu's most recent window, brings them
  back), and a hand-over mechanism exists (AM-1.3). The gains: one player for the whole bundle, reached from File
  Explorer and the command line as well, a queue that outlives the folder list, and an axFile with one job.
- **AM-D2 The queue is what was handed over** (AF-D6 kept): a play from axFile replaces the queue with the folder's
  media files from the chosen one, in name order, and nothing changes it while the user browses elsewhere; an add
  appends. axFile's own order and its hidden-files setting are not carried: axMedia reads the folder itself, in name
  order, hidden files left out, so that the hand-over is a path, not a list.
- **AM-D3 Play takes the focus, Add does not.** Opening a media file in axMedia is opening it in its app, as opening a
  Markdown file in axDown is; the keys are then where the sound is. Add to queue is a background act, answered in
  axFile; a window that an add opens, when none was open, shows without activation and is meant to stay behind. Windows may bring a new window to the front all the same, and NVDA then reads that window and what axFile said is lost: so the window an add opened says "Added 1" itself, once, when the user arrives in it (2026-11-22, the maintainer: "when there is only one item added into axMedia it is not announced"). Enter in axFile itself is Windows' default program (the user's decision of 2026-10-22, `docs/axPreview/SPEC.md`
  AP-D2): axMedia gets Enter when the user has made it the default for the kind, as in File Explorer.
- **AM-D4 The hand-over is a window message, sent by the app to itself.** A second axMedia process started with
  paths finds the open window and hands them over with `WM_COPYDATA`, Win32 of the plainest kind, synchronous and
  one-way, needing no server, no pipe, no file, and nothing of it is on the network (G6); it works the same from
  axFile, from File Explorer and from a console, and axFile needs to know nothing of it. The window list already
  exists for the axKit menu (AX-9). The message carries text only.
- **AM-D5 Windows' own decoders through one seam: MFPlay.** From axFile's AF-D3, unchanged: no codec ships with the
  bundle (B-10); MFPlay was chosen over MCI because Media Foundation keeps the pitch at a changed rate and decodes
  FLAC, AAC in MP4 and Opus; `mfplay.dll` ships with Windows 11; the Media Engine is the fallback should a Windows drop
  it. The user's NVDA pass of 2026-10-21 played MP3 chapters and an MP4 video through this engine with the pitch kept.
- **AM-D6 Volume is Windows'.** The keyboard's volume keys change the system volume from any window and are read by
  NVDA (from AF-D4).
- **AM-D7 Ctrl+Enter pauses; Space selects** (the maintainer's decision of 2026-11-22: "space in axmedia should
  select and ctrl+enter should become play/pause"). Space selects in every list of axKit (bundle AX-8.16), so a
  user never has to remember which lists mark and which play; Ctrl+Enter is the bundle's main action key, and
  the main action of a player is pause and resume, from anywhere in the window. One key for it: Ctrl+P, the
  second key until then, goes. Until 2026-11-22 Space paused in the list, "what every player's pause is", and
  Ctrl+P from anywhere. While a notice shows, the keys are the notice's, as in every window (AX-6.5).
- **AM-D8 Alt+Up is faster, Alt+Down slower** (the maintainer's decision of 2026-11-26, bundle AX-8.18: Alt with an
  arrow chooses what plays in place). Ctrl+Up and Ctrl+Down were the speed from 2026-11-14 (the user's choice then)
  until 2026-11-26, and go between sections now, as in the other item windows; before that the speed was on
  Ctrl+Shift+Period and Ctrl+Shift+Comma, which became the bundle's system-wide keys, axHelp's screenshot and the
  one kept for a later help launcher (bundle AX-8.11), which never reach a window. Ctrl+Left and Ctrl+Right stay
  the player's way through the file.

## 8. Testing

- `tests/AxMedia.Tests`: the queue (moved tests), a folder's media files in order, the settings' defaults and
  sanitising, the player's reason texts; `tests/AxKit.Tests` takes `MediaFilesTests` with the move and gains
  `AppMessage`'s text form (the verb and the paths in and out).
- Playing is checked by ear and with NVDA, `docs/axMedia/nvda-test-plan.md`: the hand-over from axFile (Open with axKit apps,
  Play folder, Add to queue, and Enter once axMedia is the default, with and without an axMedia window open), from
  File Explorer, the transport keys, the speed and the pitch, a video, a file Windows cannot decode, the background
  announcement, two axMedia windows.

## 9. Open questions

- Keeping the queue for the next start, with a resume position per file (an audiobook resumed where it stopped; per
  file in the settings, with a cap).
- Reordering the queue by key (Alt+Up and Alt+Down are the speed; a Move up / Move down pair).
- A `.m3u` or `.pls` file as the queue, from axFile's Enter or axMedia's Add files.
- Whether axFile should carry remote keys for the player (pause, what is playing) sent through the hand-over; decided
  against for the first build, since an answer back would need a second message (AM-D4).

## 10. Status

- 2026-10-22: specified at the user's ask, with the inventory of the split, which is in the git history.
- 2026-10-22, later the same day, on the branch `split`: built. `MediaFiles` and `NaturalOrder` to `AxKit.Core`,
  `AppMessage` in `AxKit.Forms`, `AxMedia.Core` (the player and the queue from AxFile.Core, `MediaFolder`,
  `MediaSettings`, 13 tests), the window (`MediaForm`), the keys, the texts, the guide, the test plan and the
  wiring (the dispatcher with AX-1.2's media rule, the installer's block with the Open with registration, the
  icon, `scripts to run-individual-apps\run-axMedia.ps1`). The window and the hand-over are not yet checked with NVDA or heard; axFile hands over
  in the next step of the inventory and keeps its own player until then.
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axKit/nvda-test-record.md`.
