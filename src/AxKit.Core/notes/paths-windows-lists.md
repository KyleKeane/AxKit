# AxKit.Core notes: Paths, windows and lists

- `LocalPaths` (AX-9.4, AX-9.5; 2026-10-23): whether a path is on a drive of this computer, where asking whether it
  exists answers at once, and `ExistsQuickly`, which asks of a local path only, since a share can take the network's
  timeout, which no menu may wait for. `AtStart` (AX-9.5a, R-64, 2026-11-16) is the path a window remembered, to
  start on: asked about only on a drive of this computer, and `NotOpenedAtStart`, the sentence for one that is
  not; `LocalPathsTests` reads the sources for a remembered path asked about another way.
- `FileSize` (2026-10-24; from AxFile.Core's `FolderListing.FormatSize`, axPreview): a size as people say it,
  "512 bytes", "12 KB", "1.2 MB", "3.5 GB".
- `PathName` (AX-9.5): `Of`, the name of a path for a title, a label or an announcement, the last part or the drive's
  root itself (four copies until 2026-10-23).
- `RecentPaths` (AX-9.5): what every settings class does to its recent-path list, `Remember` (the newest first, once
  whatever the case, ten at most) and `Sanitize` after loading, which keeps whole paths only (`IsWhole`, repairs R-68:
  an entry is handed to an app as an argument and must never read as an option); the list property stays in each
  class so that its file keeps its shape.
- `RecentlyClosed` (AX-9.4): the bundle's list of the last ten files and folders the windows held when they closed or
  swapped them, in `%APPDATA%\AxKit\recently-closed.json` through `SettingsFile`: `Remember` from a window, `Load` for
  the menu (entries whose path still exists), `Title` as "notes.md (C:\notes\notes.md) - axDown". Since 2026-11-16
  (R-68): `Keeps`, the entries the list holds (an app's name, and a whole path or none), applied by `Read` and by
  `Remember`; and one writer at a time, `Remember` reading, adding and writing under a mutex named after the file
  (`MutexName`), given up after `WaitMilliseconds` with a line in the log and nothing written, never a hang. Never
  write the file another way.
- `AxKitWindows` (AX-9): the window lists of the axKit menu from the windows `AxKit.Forms.WindowMenu` reads off the system
  in Z order, which is recency: the app from a title's ending (" - axDown"), Open windows without the current window,
  each app's windows with it, and the item labels with the number as the mnemonic and a full stop as a pause
  ("&1. notes.md - axDown"; the apps' recent lists use the same `Label`).
- `MenuText` (AX-6.4, AX-10.2): `Plain`, a menu item's or a button's text as spoken or searched, the mnemonic marker
  and the trailing dots gone; the palette's names, the notices' key line and the Speech menu choices read it.
  `CheckSaid` (2026-11-16): what a check item says after it was chosen where no menu shows it, "Word wrap, checked",
  the one place for the words, which the palette and the automation channel's choose each had a copy of.
- `SaidTogether` (AX-7.3, 2026-11-16; out of `AxKit.Forms`' window base, where it had no test): `Sentence`, several
  announcements as one, joined by a full stop and a space; a piece another follows loses its own trailing full stops,
  the last arrives as given, so a command's words after "Claude ran ..." end as they do by its key (MC-7.5).
- `Palette` and `PaletteEntry` (AX-10): the command palette's lines: `Text` (a command's name with its menu path and
  key), `Find` (the matches for a typed text through `TextMatch`), `Spoken` ("Save as, File, Ctrl+Shift+S, 1 of 3")
  and `NoMatch`.
- `KeyChord` (docs/axKit/user-workflows.md WF-2.2, 2026-11-15): the one chord vocabulary, the names a key press is
  written with, each with its virtual key code: a letter or a digit by itself, enter, escape (also esc), tab, space,
  backspace, delete, insert, the arrows, home, end, pageup, pagedown, plus, minus, period, comma, apps, f1 to f12,
  slash, semicolon, and add and subtract for the numeric keypad's plus and minus. `Code` (a name's code, in any case,
  null for none), `Name` (a code's first name), `Text` (a chord, the modifiers in the order ctrl, shift, alt and the
  key, in lower case: "ctrl+shift+w"; empty for a code without a name), `Normal` (a press put in that order, as given
  when a part is unknown) and `IsCommandChord` (it carries ctrl or alt, or its key is a function key). The automation
  channel's keys step reads a key's code here and the window writes its commands' keys with it
  (`AxKit.Forms/BundleKeys.Chord`); the names and codes are the ones the channel's and the NVDA tier's own tables
  held, pinned by `KeyChordTests`. A new name is added to the table `Named` with its code, and `KeyTests` fails when
  a key table holds a key without one.
- `CommandListing` (WF-2.3 to WF-2.5, section 5.2; 2026-11-15): a window's commands as data. `Rows` makes them from
  the menu bar's entries (`MenuCommand`) and every command of the window's list that has a key (`KeyCommand`, with
  whether its key acts now and whether it is a key with no menu item): the menu rows in menu order, then the keys with
  no menu item folded by label, without regard to case (a menu row of that label with the same chord takes the key in,
  else the first row of that label gains the chord in `Also`; keys of one label with no menu row are one row, the
  first its key), and for each row `Now`, the first of its chords that would run it at this moment (a menu row's chord
  acts when no command has it or one that has it acts; a row of keys alone has a chord only when the first command
  acting on it carries the row's label). `Ai` is false for every row of an app of `McpTools.NeverRead`. `Write` gives
  the one line the automation channel answers `commands data` with (`WindowCommands`, its rows `ListedCommand`) and
  `Read` reads it back, null for any answer that is no listing (the palette's lines, an error, a JSON string). The
  window side is `AxKit.Forms/AxKitForm.Commands.cs`.
- `TextMatch` (AX-10.3): whether a name holds every word of a typed text, case and accents folded away (`Fold`,
  `Words`), and its `Rank` by the first word (the name starts with it, a word does, anywhere), null when a word is
  missing; from AxStart.Core's `ProgramMatch`, the command palette (2026-10-22).
- `TypeAhead` (docs/axFile/SPEC.md AF-3.6; from AxFile.Core, axProcess, 2026-10-24): where letters typed in a list
  jump, File Explorer's way: the prefix from the item in hand round the end, a run of one letter cycling from the next
  item.
- `NaturalOrder`: `StrCmpLogicalW` of shlwapi as an `IComparer<string>`, File Explorer's order of names (2 before 10);
  from AxFile.Core, axMedia (2026-10-22).
- `Marks`, with `MarkChange` and `MarkRow` (docs/axKit/SPEC.md AX-8.16; from AxFile.Core, the item windows,
  2026-11-22): the selection of a list as a set of marks on the rows it shows, and the Shift range being made. One
  method per key of `AxKit.Forms/SelectionKeys` (`Toggle`, `All`, `None`, `ToHere`, `Extend`; `Keep` for a list made
  again, `Clear` for another level), each over the shown rows (a
  `MarkRow`: the key its mark is kept by, its name, and whether it can be selected at all, a heading cannot and is
  passed over by the keys that select many) and returning a `MarkChange`: the rows whose mark flipped and the words
  said (until 2026-11-22 also the line axFile's history of operations kept for the change and what its undo
  needed; that history holds only what changed files since). The words of a selection ("Selected, 3 selected", "None
  selected") are made here and nowhere else. `Items` is "1 item", "3 items". The window's side is
  `AxKit.Forms/ListSelection`.
- `ListArrange`, with `ArrangeRow` (docs/axKit/framework.md FW-4.10a, 2026-11-22; the filter from AxFile.Core's `FolderListing`, for every navigator): the filter and the sort of a navigator's list, as positions in the rows given (an `ArrangeRow`: its name, whether it is a list one goes into, whether it is a heading). `ByName` (Windows' natural order, lists first, a heading left in its place and the rows under it ordered among themselves), `Filter` (the names that contain a text in any case; a heading stays while a row under it passes, its own name not looked at), `Name` (the list's name under a filter, "Home, 4 items, filter tap, 1 shown") and `NoMatch` ("No names contain chap"). The window's side is `AxKit.Forms/ItemForm.Arrange.cs`.
- `Accents` (2026-10-23): the accented letters of Latin-1 and Latin Extended-A taken apart (`Split`), put together
  (`Compose`) and stripped (`Strip`) from tables of their own, because the executable runs with invariant
  globalization, where `string.Normalize` returns its input; `TextMatch.Fold` and axFile's lookalike names use it. The
  test projects run in invariant mode too (`tests/Directory.Build.props`), which is what found both.
