# AxKit.Forms: the hotkey window (axStart, axClip, axHelp)

Part of the notes of `src/AxKit.Forms` (the index is `src/AxKit.Forms/CLAUDE.md`). One bullet per piece.

- `HotkeyForm` (docs/axStart/SPEC.md AS-1, AS-3.4, AS-4; from axStart's `StartForm` on its second use, axClip, 2026-10-23, moved unchanged): the window of a background app on `AxKitForm`:
  - `SetVisibleCore` (the first showing only makes the handle);
  - `OnHandleCreated` (the hotkey registered, or the window shown with the notice that it is taken; shown unless started hidden);
  - `OnFormClosing` (Alt+F4 and Close all windows hide; `Quit`, the update and Windows' end close);
  - `WndProc` (an activation while hidden gives the front back; `WM_HOTKEY` shows, hides, or is captured in the Hotkey notice);
  - `TakeHandOver` (`show`);
  - `ShowWindowInFront` (the foreground window kept in `_giveBackTo` when it is another program's, the app's `PrepareShowing` for its bounds and field, `Show`, `WindowMenu.Activate`, the app's `AfterShowing`);
  - `HideWindow` and `GiveForegroundBack` (the one rule of the foreground, which is `ForegroundReturn` since 2026-11-15: the window to give the front back to, `Remember` as the window shows, `GiveBack` while it is hidden; axClaude's window that hides on closing holds one too, docs/axClaude/SPEC.md FR-9.6) with `GiveForegroundBackLaterAsync`, the hide when another program but NVDA comes to the front;
  - `ShowHotkeyNotice` and its capture;
  - `FindRunning` (a second start's search by the title among the processes of this very program file, `ThisProgram.Runs`, 2026-11-09: a test run's hand-over never reaches the user's own window from another build; it finds hidden windows, which the Window menu's list does not, so axHelp finds its hidden conversation with it too; the search itself is `BundleWindows.Titled`, which in a test run finds only a window of the run's own, 2026-11-15);
  - the settings both apps share, `AxKit.Core/HotkeySettings` (2026-11-09): `CurrentHotkey` and the hotkey kept from the Hotkey notice, the Run key written at handle creation as Start with Windows says (`SignInStart.Apply`), and the menu items both apps place: `HotkeyItem`, `SayFirstMatchItem`, `AddHideAndQuit` (the separator, Hide with the app's key, Quit) and `StartWithWindowsItem` (AX-13.4, 2026-11-08: the toggle that writes the Run key, or in the Store's copy an item that opens Windows' Startup apps page, `SignInStart.OpenWindowsStartupSettings`, and says to turn the app on or off there);
  - `ForegroundBefore` is the window a paste goes to.
  - The hotkeys at first are on Ctrl+Shift since 2026-11-14 (bundle AX-8.11): axStart's Ctrl+Shift+Space, axHelp's Ctrl+Shift+Slash and, as its second hotkey, Ctrl+Shift+Comma, axClip's Ctrl+Shift+Semicolon; Ctrl+Shift+Period is kept and registered by nothing. A retired default in a settings file is taken over by the settings class before the window reads it (`HotkeySettings.TakenOver`), so nothing of it is here.
  - The search field over the matches is here too since 2026-10-24 (both apps had it line for line):
    - `SearchField` and `Matches` (never focused, AS-D2: the bundle's `AxKitList` with its `Field` set since 2026-11-22, framework step 12, so no tab stop, a focus it is given goes back to the field, and no entry sound; `notes/controls.md`), `ShowMatches` (the list's `FillMatches`);
    - the 300 ms timer that says the first match;
    - `AddMatchKeys` (six commands with the app's keys: Down, Up, Page Down, Page Up, first, last) and `SelectMatch`, which moves through the list's `MoveFromField` (the edge earcon at an end, the sound of nothing found) and says the match.
  - The app fills in `PrepareShowing`, `AfterShowing`, `OnHiding`, and for the matches `MatchCount`, `SpokenMatch`, `NothingSaid` and `Refill`.
  - The gotchas of the foreground are in `src/AxStart/CLAUDE.md`.
