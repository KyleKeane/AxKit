# axClip NVDA test plan

Run with NVDA before a release, after `scripts to run-individual-apps\run-axClip.ps1` (or `.\run.ps1 -App clip`), with axDown open on a scratch file. Quit an installed axClip first. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axClip/SPEC.md` (CP-n.m). axStart's plan (`docs/axStart/nvda-test-plan.md`) is run in the same pass while the two share `HotkeyForm`.

## 1. Start and the hotkey (CP-1)

1. Start axClip: "axClip window", then "Search the clipboard, edit". Escape: axClip hides and NVDA reads the window you were in; typing goes there.
2. In axDown, copy three different lines (Ctrl+C on each). Nothing is spoken by axClip.
3. Ctrl+Shift+Comma: "axClip window, Search the clipboard, edit". Down: the third line you copied, "…, 1 of 3". Ctrl+Shift+Comma again: axClip hides.
4. Run `axClip` in a console while axClip runs: the running window comes to the front; no second axClip starts.
5. Alt+F4 in axClip: it hides, it does not end; Ctrl+Shift+Comma brings it back with the items.

## 2. The items (CP-2, CP-3)

1. Down, Up: each item read with its place; at the last one, Down plays the low edge sound and reads it again. Ctrl+Home, Ctrl+End: the first and the last.
2. Type a word of one item: after a moment "<that item>, 1 of 1". Backspace the word: the list shows all items again.
3. Copy the first line again in axDown, then Ctrl+Shift+Comma, Down: that line is first, and it is in the list once.
4. Copy a text of three lines: its item reads "<first line>, 3 lines". Ctrl+Q: the notice "Clipboard text, 3 lines" with the whole text; Escape.
5. Ctrl+P: "Recording paused"; copy a line in axDown; Ctrl+Shift+Comma: that line is not in the list. Ctrl+P: "Recording". NVDA+End: "N items" and "Recording".
6. Copy a password from a password manager that marks it private (KeePassXC, Bitwarden, 1Password): it is not in the list.

## 3. Pasting (CP-4)

1. In axDown, put the caret on an empty line. Ctrl+Shift+Comma, Down to an item, Enter: axClip hides, NVDA reads axDown, and the item is pasted at the caret.
2. Ctrl+Shift+Comma, an item, Shift+Enter: axClip hides and NVDA reads the window you were in; nothing is said by axClip and nothing is pasted; Ctrl+V in axDown pastes it.
3. Delete on an item: "Removed". Ctrl+Shift+Delete: the notice "Clear the clipboard history?"; Escape: nothing changes; again and Enter: "Clipboard history cleared"; Down: the low sound and "The clipboard history is empty".

## 4. Settings and the rest (CP-5, CP-6)

1. Options, Hotkey: the notice "Press the key that brings axClip up…"; press Ctrl+Alt+Shift+V: the field says it; Enter: "Hotkey Ctrl+Alt+Shift+V"; the new key shows axClip. Set it back to Ctrl+Shift+Comma.
1a. The key taken over (CP-D1, 2026-11-14): with axClip ended, set `"hotkey": "Windows+Alt+V"` in `%APPDATA%\AxKit\axClip.json`, as 5.8.0 left it. Start axClip: Windows+Alt+V does nothing, Ctrl+Shift+Comma brings it up, and Options, Hotkey says "Now: Ctrl+Shift+Comma". Then choose Ctrl+Alt+Shift+V, quit and start again: that key still brings axClip up. Set it back.
2. Options, Keep history between runs (checked), copy two lines, Session, Quit axClip, start it again: the two lines are there. Turn the option off, quit, start again: the history is empty.
2a. A kept history that cannot be opened (CP-3.6, 2026-11-16): with axClip ended and Keep history on, put `"protectedHistory": "bm90IHByb3RlY3RlZA=="` into `%APPDATA%\AxKit\axClip.json` in place of the value there. Start axClip and bring it up: after the field's name NVDA says "The kept clipboard history could not be read on this computer and starts empty." once; the list is empty, `axClip.json.bad` is beside the file, and Keep history between runs is still checked. Hide it, quit, start again: nothing is said.
3. F1: the Keyboard shortcuts notice; Escape. Help, User guide: this guide.
4. Alt+H, About, Version names axClip among the apps.

## The menu bar and Context (bundle AX-9.1, AX-9.11, 2026-11-11)

1. With a clip selected, Alt, C: Context opens on "Paste Enter"; Alt, A: "Clear history".
