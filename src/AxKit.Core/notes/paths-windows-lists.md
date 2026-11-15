# AxKit.Core notes: Paths, windows and lists

- `LocalPaths` (AX-9.4, AX-9.5; 2026-10-23): whether a path is on a drive of this computer, where asking whether it
  exists answers at once, and `ExistsQuickly`, which asks of a local path only, since a share can take the network's
  timeout, which no menu may wait for.
- `FileSize` (2026-10-24; from AxFile.Core's `FolderListing.FormatSize`, axPreview): a size as people say it,
  "512 bytes", "12 KB", "1.2 MB", "3.5 GB".
- `PathName` (AX-9.5): `Of`, the name of a path for a title, a label or an announcement, the last part or the drive's
  root itself (four copies until 2026-10-23).
- `RecentPaths` (AX-9.5): what every settings class does to its recent-path list, `Remember` (the newest first, once
  whatever the case, ten at most) and `Sanitize` after loading; the list property stays in each class so that its file
  keeps its shape.
- `RecentlyClosed` (AX-9.4): the bundle's list of the last ten files and folders the windows held when they closed or
  swapped them, in `%APPDATA%\AxKit\recently-closed.json` through `SettingsFile`: `Remember` from a window, `Load` for
  the menu (entries whose path still exists), `Title` as "notes.md (C:\notes\notes.md) - axDown".
- `AxKitWindows` (AX-9): the window lists of the axKit menu from the windows `AxKit.Forms.WindowMenu` reads off the system
  in Z order, which is recency: the app from a title's ending (" - axDown"), Open windows without the current window,
  each app's windows with it, and the item labels with the number as the mnemonic and a full stop as a pause
  ("&1. notes.md - axDown"; the apps' recent lists use the same `Label`).
- `MenuText` (AX-6.4, AX-10.2): `Plain`, a menu item's or a button's text as spoken or searched, the mnemonic marker
  and the trailing dots gone; the palette's names, the notices' key line and the Speech menu choices read it.
- `Palette` and `PaletteEntry` (AX-10): the command palette's lines: `Text` (a command's name with its menu path and
  key), `Find` (the matches for a typed text through `TextMatch`), `Spoken` ("Save as, File, Ctrl+Shift+S, 1 of 3")
  and `NoMatch`.
- `TextMatch` (AX-10.3): whether a name holds every word of a typed text, case and accents folded away (`Fold`,
  `Words`), and its `Rank` by the first word (the name starts with it, a word does, anywhere), null when a word is
  missing; from AxStart.Core's `ProgramMatch`, the command palette (2026-10-22).
- `TypeAhead` (docs/axFile/SPEC.md AF-3.6; from AxFile.Core, axProcess, 2026-10-24): where letters typed in a list
  jump, File Explorer's way: the prefix from the item in hand round the end, a run of one letter cycling from the next
  item.
- `NaturalOrder`: `StrCmpLogicalW` of shlwapi as an `IComparer<string>`, File Explorer's order of names (2 before 10);
  from AxFile.Core, axMedia (2026-10-22).
- `Accents` (2026-10-23): the accented letters of Latin-1 and Latin Extended-A taken apart (`Split`), put together
  (`Compose`) and stripped (`Strip`) from tables of their own, because the executable runs with invariant
  globalization, where `string.Normalize` returns its input; `TextMatch.Fold` and axFile's lookalike names use it. The
  test projects run in invariant mode too (`tests/Directory.Build.props`), which is what found both.
