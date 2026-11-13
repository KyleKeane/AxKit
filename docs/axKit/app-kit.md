# The app kit: a new app as a description

Asked by the user on 2026-10-24: "an extensive consolidation and refactor to minimize the codebase … as much reusable
composable modular infrastructure as possible … future apps extremely lightweight … through easy processes that do
not require you to ever touch core code unless there is a new control or interaction". Specification first; built
after the security fixes, one app per step. Requirements AK-n.m. The evidence (line counts, the copies) is in
`inventory-2026-10-24.md` and the review of the windows the same day.

## 1. What an app is today, and what it becomes

Today a key is written five times: a constant in the window's key table, its entry in `All`, a menu item, a `case` in
`ProcessWindowKey`, and a line in the F1 text; and a new app is added in seventeen places across the repository.
After the kit:

- AK-1.1 **An app is**: its Core (pure logic, tested), one description file `AxThingApp.cs` (its name, its archetype,
  its commands, its views or editor kind), its window only where it has behaviour no archetype has, and its documents
  (spec, guide, test plan). Nothing in `AxKit.Core` or `AxKit.Forms` changes for it unless it brings a control or an
  interaction no app has yet.
- AK-1.2 **Registered once**: its line in `Dispatch.Apps` (name, verb, project, group, what it opens, Show in) and a
  `tools/new-app.ps1` run, which writes the folders, the empty documents, the description, and the few text places that
  must stay text (the READMEs, the root notes, the issue forms, the changelog). Everything else reads `Dispatch.Apps`
  (AK-4).

## 2. Commands: one list per window

- AK-2.1 `Command(Label, Key, Scope, Run, Enabled?, Help?, Mark)`: the menu's label with its letter, the key (from the
  window's key table, so that the collision test keeps static data), where the key acts (the window, the list, the
  field, the details), what runs, when it is enabled, the F1 line, and its mark for MCP (read, open, change, act;
  `mcp.md` MC-3.1) and whether it is secret.
- AK-2.2 `MenuSpec(Label, Items)`: a menu of commands, separators and submenus. A window describes its menus as specs.
- AK-2.3 **The window base builds from them**: the menu bar with the bundle's frame, the same eight menus in every
  window (SPEC AX-9.1, B-19, since 2026-11-11 put together by `AxKitForm.PlaceMenuBar`): Session (S, what the window
  holds, ending in Exit), Actions (A, what the app does, the same wherever the focus is), Navigation (N, the moves and
  the finds), Context (C, what acts on the focus, `ContextBarMenu`, opened by Shift+F10, AX-9.11), Options (O, the
  app's own settings, then Text, General and Developer), Window (W), axKit (K) and Help (H); an item goes in one of
  them by what it does, and an empty one of Actions, Navigation and Context holds one item that says so; the key handling (`RunCommand`, one
  loop over the commands in scope order, the window's own keys first, then the focused control's); the F1 text (the
  bundle's section, the app's commands from their help, a short hand-written paragraph for the controls' own keys);
  the command palette (it already reads the menus). Menu, key, F1, palette and MCP cannot disagree, since there is one
  list.
- AK-2.4 Tests from the data: every command's key is in the F1 text; letters are unique within a menu; no key equals a
  bundle key; every act command is marked (since 2026-11-11 every command states its mark, and the act commands that
  start a program or reach past the window are pinned, `tests/AxKit.Tests/CommandMarkTests`, `mcp.md` MC-D7).

- AK-2.5 **One source, every reader the same** (the user's rule of 2026-11-13: parity between the menus, the settings,
  the command palette and the AI tools, with as few exceptions as possible, each one deliberate and for security).
  The menu bar is the one source of what a window can do: every command, every setting (a check item or a choice),
  and every list a menu fills as it opens (Open recent, Open windows, Recently closed). The palette, the AI tools'
  `axkit_window_commands` and `axkit_window_run`, the F1 text and the tests all read it through one function, the
  entries `CommandPalette.Commands` makes, each with its name, its menu path, its key, its checked state and its mark.
  A command of the list keeps its own mark (AK-2.1); an item that is not one takes its kind's: a setting is change, a
  recent file or window is open, Recently closed is open. Nothing is added to one reader alone: a new action is a menu
  item, and it is in all of them at once.
- AK-2.6 **The exceptions**, all here and nowhere else, each for security:
  1. axPass and axClip answer no AI tool (MC-4.3): secrets and the clipboard.
  2. An act entry asks the user before an AI tool runs it, naming its target (MC-7.3).
  3. The AI tools' own switches (Let AI tools read and open axKit, Let AI tools run commands, Forget) are never
     offered to an AI tool: a client never widens its own access.
  4. A setting that an AI tool changes is said in the window like any command it runs (MC-5.1); the consent and
     access settings of AX-11 are under exception 3.
  Not security, and kept as few as possible: axFile's Drives and the Open with submenus are filled only when opened,
  since reading them can stall on a network drive or the shell's handlers; the menus themselves and their keys work.
  Built so far (2026-11-13): the palette reads every menu, the recent lists included. Owed: the AI tools reading the
  same entries in place of the command list alone, with exception 3 as a mark the base sets, and a test that every
  reader's list is the palette's.

## 3. Archetypes

- AK-3.1 The windows keep their archetype bases and gain one: **`ListWindow`** (shapes of axGIT, axProcess, axMedia,
  axCalc and most of axFile): one list with views (`ListViewState` and the refresh, show, stale and cycle flow, once),
  an optional details pane, an optional field, Tab between them, find in the list or the details, the default
  bodies of the base's hooks (hide for a notice, the font, the focus).
- AK-3.2 The others stay: `TextEditorForm` with `EditorApp` (axDown is 112 lines; the model of the kit), `ConsoleForm`,
  `ItemForm`, `HotkeyForm`. Each takes commands as AK-2 does.
- AK-3.3 Kept as overrides, never flattened: the behaviours tuned under NVDA (a window's keys before the control's, the
  list that must not ding, axFile's own Tab, an announcement on the focused control, axMedia showing without
  activation, menus filled as they open).

## 4. One place for an app

- AK-4.1 Read from `Dispatch.Apps` (extended with the description, the icon's letters and colour, the file types, Open
  with): `run.ps1` (no hand-kept verb list), `install.ps1` (through an `AxKit.exe apps` listing), `make-icon.ps1`, the
  project references (an MSBuild glob), `Program.cs` (one table), `KeyTests` (one entry per app).
- AK-4.2 Kept as text, checked by `InventoryTests`: the READMEs, the root notes, the issue forms, the changelog, the
  specs' lists (which point to `Dispatch.Apps` rather than repeat it, at the user's word).
- AK-4.3 **Built 2026-10-24** (branch `app-kit`). `Dispatch.Apps` carries the description, the icon's letters and
  colour, File Explorer's entries (`Explorer`, `Types`) and Open with (`OpenWith`, `TypeName`). Read from it now:
  `Program.cs` (one table, verb to `Run`, the taskbar id from the project name); `install.ps1` through `AxKit.exe
  apps`, which prints the list as JSON (AX-1.4), since the installer runs from the zip without the source, with the
  listing kept as `apps.json` for an uninstall that cannot run the program; `run.ps1` and `make-icon.ps1` off
  `Dispatch.cs` itself through `tools/apps.ps1`, since they run before a build; the project references of `AxKit` and
  `AxKit.Tests`, by a glob over `src`. `tools/new-app.ps1` writes the rest of a new app's places: the folders and a
  first window (one list on `AxKitForm`), the documents as stubs, the run and AxAutomate scripts, the icon, the
  `Dispatch.Apps` entry with its verb and usage, the line in `Program.cs`, the `KeyTests` and `DispatchTests`
  entries, the solution, the issue forms, AX-1.1 and AX-1.6, and drafts of the README, map, spec and changelog lines;
  it prints what stays by hand (`adding-an-app.md` marks each place). Still a copy: `KeyTests`' two entries per app,
  which reference the window's classes and so are code, not data; install.ps1's `$background` table and run.ps1's
  restart of the two background apps; the prose of the documents.

## 5. Order, one step per commit, each shippable

1. The records, the builders and the tests, unused.
2. axCalc on them (the smallest), then axProcess, then axGIT moving the views flow into `ListWindow`, then axMedia.
3. axTask and axPass through `ItemForm`, axStart and axClip through `HotkeyForm`, axTerminal and axClaude, the editors.
4. axFile last (the most states).
5. In parallel, the registration places of AK-4, then `tools/new-app.ps1` (done 2026-10-24, AK-4.3).

Every step: the app's NVDA test plan passed before the next, as the bundle's rule says.
