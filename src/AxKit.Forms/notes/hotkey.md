# AxKit.Forms: the hotkey window (axStart, axClip, axHelp)

Part of the notes of `src/AxKit.Forms` (the index is `src/AxKit.Forms/CLAUDE.md`). One bullet per piece.

- `HotkeyForm` (docs/axStart/SPEC.md AS-1, AS-3.4, AS-4; from axStart's `StartForm` on its second use, axClip, 2026-10-23, moved unchanged): the window of a background app on `AxKitForm`:
  - `SetVisibleCore` (the first showing only makes the handle);
  - `OnHandleCreated` (the hotkey registered, or the window shown with the notice that it is taken; shown unless started hidden);
  - `OnFormClosing` (Alt+F4 and Close all windows hide; `Quit`, the update and Windows' end close);
  - `WndProc` (an activation while hidden gives the front back; `WM_HOTKEY` shows, hides, or is captured in the Hotkey notice);
  - `TakeHandOver` (`show`);
  - `ShowWindowInFront` (the foreground window kept in `_giveBackTo` when it is another program's, the app's `PrepareShowing` for its bounds and field, `Show`, `WindowMenu.Activate`, the app's `AfterShowing`);
  - `HideWindow` and `GiveForegroundBack` (the one rule of the foreground) with `GiveForegroundBackLaterAsync`, the hide when another program but NVDA comes to the front;
  - `ShowHotkeyNotice` and its capture;
  - `FindRunning` (a second start's search by the title among the processes of this very program file, `ThisProgram.Runs`, 2026-11-09: a test run's hand-over never reaches the user's own window from another build);
  - the settings both apps share, `AxKit.Core/HotkeySettings` (2026-11-09): `CurrentHotkey` and the hotkey kept from the Hotkey notice, the Run key written at handle creation as Start with Windows says (`SignInStart.Apply`), and the menu items both apps place: `HotkeyItem`, `SayFirstMatchItem`, `AddHideAndQuit` (the separator, Hide with the app's key, Quit) and `StartWithWindowsItem` (AX-13.4, 2026-11-08: the toggle that writes the Run key, or in the Store's copy an item that opens Windows' Startup apps page, `SignInStart.OpenWindowsStartupSettings`, and says to turn the app on or off there);
  - `ForegroundBefore` is the window a paste goes to.
  - The search field over the matches is here too since 2026-10-24 (both apps had it line for line):
    - `SearchField` and `Matches` (never focused, AS-D2: no tab stop, and a focus it is given goes back to the field), `ShowMatches`;
    - the 300 ms timer that says the first match;
    - `AddMatchKeys` (six commands with the app's keys: Down, Up, Page Down, Page Up, first, last) and `SelectMatch` with the edge earcon.
  - The app fills in `PrepareShowing`, `AfterShowing`, `OnHiding`, and for the matches `MatchCount`, `SpokenMatch`, `NothingSaid` and `Refill`.
  - The gotchas of the foreground are in `src/AxStart/CLAUDE.md`.
