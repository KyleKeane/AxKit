# axDescribe, access layer for Claude Code: specification

Status: built 2026-11-09 on the branch `ai-help`, from `docs/axkit/ai-help.md` (AH-n.m, the service it is an app of)
and the user's decisions of that evening (ai-help.md section 9). Requirements are numbered DE-n.m. The bundle's
rules are `docs/axkit/SPEC.md` (AX-7 accessibility, AX-8 keys, AX-11 access layers); "SPEC.md" in axDescribe's code
means this file.

## 1. Summary

axDescribe is the window in front described by Claude: a hotkey from any program takes a picture of that window,
Claude Code reads the picture file and describes it for a blind user, and the description comes as a notice the user
reads and asks more about. The last ten descriptions stay in a list under a field. `AxKit.exe describe [--hidden]`,
Start entry `axKit axDescribe`, command `axdescribe`; a background process from sign-in, as axStart and axClip. It is
an access layer (AX-11.1): Claude Code, Anthropic's program, does the work under the user's own account and
Anthropic's terms; AI help must be on and the consent given once (AH-1.2). Its local counterpart is axPreview's
reader of a picture's text (AP-D11), which Ctrl+O reaches.

## 2. Goals and non-goals

- G1. One key from anywhere, and the screen is described; nothing to open first.
- G2. Nothing of AI help lives here (as axHelp's G2); axDescribe adds the picture, the prompt and the list.
- G3. The picture stays on this computer but for Claude Code's reading of it: one file, written over by the next,
  deleted when axDescribe ends.
- Non-goal: acting on the screen (moving the mouse, clicking); `plan.md` keeps that idea.

## 3. Scenarios

- A dialog pops up in a program NVDA reads badly. Windows+Alt+S: "Asking Claude", then "Claude says": "This is a
  dialog from Adobe Reader asking whether to save changes to report.pdf. It has three buttons: Yes, No and Cancel.
  Yes is the default." Escape, and Enter in the dialog.
- Windows+Alt+S on a chart in the browser: the description names the axes and the trend; Tab, "what is the highest
  value?", Enter: the same picture answers.
- Ctrl+O: the picture opens in axPreview, whose reader lists the words in it.

## 4. Requirements

### DE-1 Launch, the background process, the hand-over

As axHelp's HE-1 (`HotkeyForm`): `AxKit.exe describe [--hidden]`, one at a time by a named mutex, a second start hands
"show" over, Alt+F4 hides, Quit ends; in the Store's copy no startup task (bundle AX-13.2), started from any window's
New window.

### DE-2 The window and the picture

- DE-2.1 **The hotkey takes the picture first**, before axDescribe's own window shows (`PrepareShowing`), so that
  nothing covers the window in front: the screen inside that window's rectangle (`GetWindowRect`,
  `Graphics.CopyFromScreen`, physical pixels under per-monitor DPI), or the whole screen it is on when the window
  gives no rectangle; then the window shows and asks Claude (`AiPrompts.Screen`: read the file, describe the window,
  what it shows, the controls) with the picture's folder as the one folder of the call (AH-2.3), the program and the
  title of the window named (`WindowOwner`). The description comes as AH-3.1, with the field for more (AH-6.3).
- DE-2.2 One picture, `%LOCALAPPDATA%\AxKit\axdescribe\last.png`, written over by the next and deleted when the
  process ends (Quit, the update, Windows' end) and by Forget the descriptions; a hide keeps it for Ctrl+O and the
  follow-ups.
- DE-2.3 One field, "Ask about the picture", with the caret always in it, the list of earlier descriptions under it
  (DE-3). **Enter** with a question typed asks it about the selected description's picture, in that description's
  session; with nothing typed, the selected description again, with its session for more; with nothing kept, the
  list's one line said.
- DE-2.4 The status bar: "N descriptions kept" ("1 description kept"), or "AI help is off: axKit menu, AI help".
- DE-2.5 **Ctrl+O** opens the last picture in axPreview (the window hidden first, so that axPreview comes up in
  front; the foreground given back after three seconds if nothing came), where Windows' own reader gives the text in
  it (AP-D11); "No picture yet: the hotkey takes one" when there is none.
- DE-2.6 Escape hides at once; another program in front hides (unless a notice is open); axDescribe, Forget the
  descriptions empties the list and deletes the picture after a yes-or-keep notice.

### DE-3 The earlier descriptions

- DE-3.1 The last ten, newest first, in `axdescribe.json`: the window's title, its program, the description, the
  session, the time (`DescribeSettings.Remember`).
- DE-3.2 Each shown as its time and the window's title, "14:02 notes.md - axDown" (the program when the window had
  no title); typing narrows the list to the labels holding every typed word (`TextMatch`); "No earlier description
  matches xyz" or "No descriptions yet: press the hotkey from the window to describe" as the one line.
- DE-3.3 The match keys as axHelp's HE-3.3, each said as "the label, n of m".

### DE-4 The hotkey

- DE-4.1 Windows+Alt+S by default (S for the screen; ai-help.md section 9), changed in Options, Hotkey, kept in the
  settings; the hotkey while the window shows hides it. A taken hotkey: the notice (AS-4.3).

### DE-5 Settings

- DE-5.1 `%APPDATA%\AxKit\axdescribe.json` (`DescribeSettings` on `HotkeySettings`): `hotkey`, `startWithWindows`,
  `sayFirstMatch`, the font and the update check, `descriptions` (ten). AI help's own settings are `ai-help.json`.

### DE-6 Keys

Window-specific (`AxDescribeKeys`): Down and Up, Page Down and Page Up (the bundle's, on the list here), Ctrl+Home
and Ctrl+End through the descriptions; Enter ask; Ctrl+O the picture in axPreview; Escape hide. Bundle-wide (AX-8.2):
F1, Ctrl+Shift+P, F4, Shift+F1, the text size keys, Alt and Alt+F4. The F1 text names them all.

### DE-7 Notices and announcements

As axHelp's HE-7, with "The picture could not be taken: …" (logged and said, nothing asked), "No picture yet: the
hotkey takes one", "Forget the descriptions?", "Descriptions forgotten", and the list's lines of DE-3.2.

## 5. Architecture

`src/AxDescribe`: `DescribeForm` on `HotkeyForm` (`PrepareShowing` takes the picture, `AfterShowing` asks),
`ScreenPicture` (the capture), `AxDescribeApp`, `AxDescribeKeys`, `HelpText`. `src/AxDescribe.Core`: `DescribeSettings`
with `Description`, `DescribeLines`; tested in `tests/AxDescribe.Tests`. The AI help calls are `AxKitForm.Ai.cs`.

## 6. Decisions

- **DE-D1 CopyFromScreen, not PrintWindow.** What the user would see is what is on the screen; `PrintWindow` asks
  the window to draw itself, which some windows (browsers, games, hardware-accelerated surfaces) do blank, and the
  screen copy needs no flag per kind of window. The one cost, that the window must be in front, is met by taking the
  picture before axDescribe shows.
- **DE-D2 One picture file, kept until the next.** Claude Code reads it once; the follow-ups ride on the session;
  Ctrl+O and a crash's diagnosis want the file; the next hotkey or Quit removes it, so nothing of the screen
  accumulates on disk.
- **DE-D3 A separate app from axHelp**, the user's words ("axdescribe and axHelp"); one hotkey per `HotkeyForm`,
  and the two prompts and lists differ in kind.

## 7. Testing

`tests/AxDescribe.Tests`; `tests/scripts/axdescribe/01-start.txt` with the fake Claude (a start on the test desktop
describes that desktop's screen); the NVDA test plan `docs/axdescribe/nvda-test-plan.md`, whose description of a real
window is the user's by ear.
