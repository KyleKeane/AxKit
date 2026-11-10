# axDescribe, access layer for Claude Code: specification

Status: built 2026-11-09 on the branch `ai-help`, from `docs/axKit/ai-help.md` (AH-n.m, the service it is an app of)
and the user's decisions of that evening (ai-help.md section 9). Requirements are numbered DE-n.m. The bundle's
rules are `docs/axKit/SPEC.md` (AX-7 accessibility, AX-8 keys, AX-11 access layers); "SPEC.md" in axDescribe's code
means this file.

## 1. Summary

axDescribe is the window in front described by Claude: a hotkey from any program takes a picture of that window,
Claude Code reads the picture file and describes it for a blind user, and the description comes as a notice the user
reads and asks more about. A picture file of the user's own is described the same way (DE-2.7, 5.6.0). The last ten
descriptions stay in a list under a field. `AxKit.exe describe [--hidden | <picture>]`,
Start entry `axKit axDescribe`, command `axDescribe`; a background process from sign-in, as axStart and axClip. It is
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
- A photo arrived by mail, saved to Pictures. Ctrl+D in axDescribe, the file chosen in the picker: "Claude says: a
  photograph of a garden table set for four..."; Tab, "is there anything written on the card?", Enter.

## 4. Requirements

### DE-1 Launch, the background process, the hand-over

As axHelp's HE-1 (`HotkeyForm`): `AxKit.exe describe [--hidden | <picture>]`, one at a time by a named mutex, a
second start hands "show" over, or "open" with the picture file it was given (DE-2.7), Alt+F4 hides, Quit ends; in
the Store's copy no startup task (bundle AX-13.2), started from any window's New window. A start on a file that is not
a picture, or is gone, is refused before any window with the reason and the usage text (`AppHost.BadCommandLine`);
`--hidden` with a picture is refused too.

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
- DE-2.3 One field, "Ask about the picture", which has the caret when the window shows, the list of earlier
  descriptions under it (DE-3), which Tab reaches as axHelp's HE-3.4 (5.6.0). **Enter** with a question typed asks it
  about the selected description's picture, in that description's
  session; with nothing typed, the selected description again, with its session for more; with nothing kept, the
  list's one line said.
- DE-2.4 The status bar: "N descriptions kept" ("1 description kept"), or "AI help is off: axKit menu, AI help".
- DE-2.5 **Ctrl+O** opens the last picture in axPreview (the window hidden first, so that axPreview comes up in
  front; the foreground given back after three seconds if nothing came), where Windows' own reader gives the text in
  it (AP-D11); "No picture yet: the hotkey takes one" when there is none.
- DE-2.6 Escape hides at once; another program in front hides (unless a notice is open); axDescribe, Forget the
  descriptions empties the list and deletes the picture after a yes-or-keep notice (the user's own picture files
  stay).
- DE-2.7 **A picture file of the user's** (5.6.0, asked 2026-11-09 with the test pictures): **Ctrl+D**, axDescribe,
  Describe a picture file..., opens the Windows file picker on the Pictures folder, filtered to the kinds Claude Code
  reads (`PictureFile.Kinds`: .png, .jpg, .jpeg, .gif, .webp), and the chosen file is described as a window's picture
  is (`AiPrompts.Picture`: the file read, what kind of picture it is, what is in it, the text, the controls of a
  screenshot, the values of a chart), the file's folder the one folder of the call (AH-2.3), the description kept
  under the file's name with the program "picture file" and the file's path (DE-3.1), so that a follow-up's call gets
  the file's folder. `AxKit.exe describe <picture>` does the same from the command line, from axFile or from a script:
  a first start shows the window and describes the file in place of the window in front; a start while axDescribe
  runs hands the path to its window. No picture of the screen is taken for a file; the user's file is never copied,
  moved or deleted. A file that is not a picture is refused with the kinds named (`PictureFile.Refusal`), said in the
  window or shown before any window (DE-1).

### DE-3 The earlier descriptions

- DE-3.1 The last ten, newest first, in `axDescribe.json`: the window's title, its program, the description, the
  session, the time, and for a picture file (DE-2.7) its path (`picture`, null for a window's picture; a file written
  before 5.6.0 has none and reads as null) (`DescribeSettings.Remember`).
- DE-3.2 Each shown as its time and the window's title, "14:02 notes.md - axDown" (the program when the window had
  no title; a file's name for a picture file); typing narrows the list to the labels holding every typed word
  (`TextMatch`); "No earlier description matches xyz" or "No descriptions yet: the hotkey describes the window in
  front, Ctrl+D a picture file" as the one line.
- DE-3.3 The match keys as axHelp's HE-3.3, each said as "the label, n of m"; nothing is said while a question is
  typed (the file's `sayFirstMatch`, off by default, no menu item); Tab into the list and Shift+Tab back as HE-3.4.

### DE-4 The hotkey

- DE-4.1 Windows+Alt+S by default (S for the screen; ai-help.md section 9), changed in Options, Hotkey, kept in the
  settings; the hotkey while the window shows hides it. A taken hotkey: the notice (AS-4.3).

### DE-5 Settings

- DE-5.1 `%APPDATA%\AxKit\axDescribe.json` (`DescribeSettings` on `HotkeySettings`): `hotkey`, `startWithWindows`,
  `sayFirstMatch` (false here by default, DE-3.3), the font and the update check, `descriptions` (ten). AI help's
  own settings are `ai-help.json`.

### DE-6 Keys

Window-specific (`AxDescribeKeys`): Down and Up, Page Down and Page Up (the bundle's, on the list here), Ctrl+Home
and Ctrl+End through the descriptions; Enter ask; Ctrl+D a picture file (DE-2.7; axFile's Ctrl+D is Favourites, a
known difference in `KeyTests`); Ctrl+O the picture in axPreview; Escape hide; Tab and Shift+Tab between the field
and the list (Windows' own, DE-3.3). Bundle-wide (AX-8.2): F1, Ctrl+Shift+P, F4, Shift+F1, the text size keys, Alt
and Alt+F4. The F1 text names them all.

### DE-7 Notices and announcements

As axHelp's HE-7, with "The picture could not be taken: …" (logged and said, nothing asked), "No picture yet: the
hotkey takes one", "Forget the descriptions?", "Descriptions forgotten", "x is not a picture axDescribe can describe:
a .png, .jpg, .jpeg, .gif or .webp file" (DE-2.7), and the list's lines of DE-3.2.

### DE-8 Test pictures

`tests/fixtures/pictures` holds three PNG files for trying axDescribe with the real Claude, by the maintainer and by
the assistant, each drawn in code by `tools/make-test-pictures.ps1` so that nothing personal is in them and anyone
can make them again: `dialog.png` (a "Save changes to report.docx?" dialog with Yes, No and Cancel), `editor.png` (an
axDown-like window with a menu bar, a few lines of text and a status bar) and `chart.png` (a bar chart, "Sales by
quarter", four labelled bars). `AxKit.exe describe tests\fixtures\pictures\dialog.png` describes one; a good
description names the dialog, its question and its three buttons. The automation's own picture is written by its
script (`tests/scripts/axDescribe/02-picture-file.txt`, a 48 by 32 PNG as base64), so that no run depends on these.

## 5. Architecture

`src/AxDescribe`: `DescribeForm` on `HotkeyForm` (`PrepareShowing` takes the picture, or takes the file a start or
Ctrl+D gave, `AfterShowing` asks; `DescribeFile`, `ChooseFile`, the "open" hand-over), `ScreenPicture` (the capture),
`AxDescribeApp`, `AxDescribeKeys`, `HelpText`. `src/AxDescribe.Core`: `DescribeSettings` with `Description`,
`DescribeLines`, `PictureFile` (the kinds, the picker's filter, the refusal); tested in `tests/AxDescribe.Tests`. The
AI help calls are `AxKitForm.Ai.cs`; the picture prompts `AiPrompts.Screen` and `AiPrompts.Picture`.

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

`tests/AxDescribe.Tests`; `tests/scripts/axDescribe/01-start.txt` with the fake Claude (a start on the test desktop
describes that desktop's screen) and `02-picture-file.txt` (a start on a picture file, DE-2.7); the NVDA test plan
`docs/axDescribe/nvda-test-plan.md`, whose description of a real window is the user's by ear, with the test pictures
of DE-8 for the real Claude.
