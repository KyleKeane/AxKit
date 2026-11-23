# axTemplate NVDA test plan

Run with NVDA before a release, after `scripts to run-individual-apps\run-axTemplate.ps1` (or `.\run.ps1 -App template`), with axDown open on a scratch file. Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axTemplate/SPEC.md` (TP-n.m). Sections 1 to 6 are the browser, 7 to 9 the launcher; `tests/scripts/axTemplate` runs them too, but for the paste into another window (8.1), which `tests/nvda-scripts/axTemplate-put-in.txt` runs with NVDA.

The steps change your own templates' folder: use a template made for the test, and delete it at the end.

## 1. Start (TP-1.1, TP-8.1, TP-8.2)

1. Start axTemplate: NVDA reads the title "Templates - axTemplate window", then "Templates, N templates, list" and the first template's name; with no template, the line "No templates yet: Ctrl+N makes one". Nothing else is spoken.
2. F1: the keyboard shortcuts as a notice; Escape closes it and the list has the focus again.
3. Alt+F4 closes the window without a question.

## 2. A new template (TP-8.4)

1. Ctrl+N: the form's sound, "New template, form", then "Type the title, edit": the template's whole form, empty. Type `Thanks`, Enter: "Added Thanks", and the list is on Thanks. Again Ctrl+N: Tab to the Text, type a line, Ctrl+Space: "Navigation mode"; T: the focus on the title; 2: on the Text; Enter: "Edit mode"; Escape leaves, nothing added.
2. Ctrl+N, type `thanks`, Enter: "There is a template called thanks already", the notice still open. Escape: the list, on Thanks.

## 3. The fields (TP-8.3, TP-2.2, TP-2.3)

1. Enter on Thanks: "Fields of Thanks, list", "Title, text, Thanks". Down: "Text, long text, empty".
2. Enter on Text: a field of several lines. Type `Thank you, {clipboard}.{caret}`, Ctrl+Enter: the pane again, "Text, long text, Thank you, {clipboard}.{caret}".
3. Down: "axDown, check, not done", then the other apps' checks; Space: "done". Enter on one: the form, the focus in the list of apps on that app, "axDown, not checked"; Space: "checked"; Shift+Tab: "Uncheck all, button"; Space: "None checked"; Shift+Tab: "Check all, button"; Space: "All 5 checked"; Escape.
4. Escape: the list, on Thanks. Ctrl+Enter: "Copied"; pasting in axDown gives `Thank you, .`

## 4. Rename, delete, undo (TP-8.4)

1. F2, type `Thank you note`, Enter: "Renamed Thank you note", the list on it.
2. Delete: the notice "Delete Thank you note?" with "Ctrl+Z brings back what was deleted, for as long as this window is open." Enter: "Deleted Thank you note".
3. Ctrl+Z: "Undone: Deleted Thank you note", the list on it. Ctrl+Z again: "Undone: Renamed Thank you note", the list on Thanks, its Text as it was.
4. Tab, Enter on Title, type `Greeting`, Enter: "Renamed Greeting"; Escape: the list on Greeting.

## 5. Several at once, and reading again (TP-8.2, TP-8.5)

1. With three templates: Space on two of them, each "…, selected". Delete: "Delete 2 templates?"; Enter: "Deleted 2 templates". Ctrl+Z: "Undone: Deleted 2 templates", both back.
2. F5: "Refreshed: Templates, 3 templates".
3. Ctrl+J on a template: axJSON opens its file. Change its Text there, save, and come back to axTemplate: the pane shows the new text without a key.

## 6. A file that is no template (TP-2.4)

1. Put a file `broken.json` holding `[1, 2]` into the templates' folder, F5: the list has "broken, not a template".
2. F2 on it: "Not a template: Ctrl+J opens the file in axJSON to mend". Tab: its details say the same.
3. A template whose file holds a value no field names: its pane ends with "1 more field kept: …".

## 7. The launcher and its hotkey (TP-1, TP-3)

1. With axTemplate's browser started once, and closed again: in axDown, Ctrl+Shift+Semicolon: "axTemplate window, Search templates, edit". Escape: the launcher hides and NVDA reads axDown; typing goes there.
2. Ctrl+Shift+Semicolon, Down: the second template, "…, 2 of N". At the last one, Down plays the low edge sound and reads it again. Ctrl+Home, Ctrl+End: the first and the last.
3. Type a word that only one template's text holds: after a moment that template, "…, 1 of 1". Type a word nothing holds, Down: "No template matches …".
4. Ctrl+Shift+Semicolon while the launcher shows: it hides. Alt+F4 in the launcher: it hides, it does not end; the hotkey brings it back.

## 8. Using a template (TP-4)

1. In axDown, put the caret on an empty line. Ctrl+Shift+Semicolon, type the start of a template's name, Enter: the launcher hides, NVDA reads axDown, and the template's text is pasted at the caret.
2. Copy a word in axDown, then use a template whose text holds `{clipboard}`: the word stands in its place. Ctrl+V afterwards pastes the word again: the clipboard was given back.
2a. Use a template whose text holds `{caret}` in its middle, then type a letter: it lands where the mark was.
2b. Select a word in axDown, Ctrl+Shift+Semicolon, a template whose text is `[{selection}]({caret})`, Ctrl+Enter: the word is now in square brackets, and what you type goes between the round ones.
2c. With axClip running: after any of these, Ctrl+Shift+Apostrophe: the template's text is not among axClip's items.
2d. A new template, with no text yet: Enter says "… has no text yet: Ctrl+B opens the browser, where it is written", and the launcher stays.
3. Ctrl+Shift+Semicolon, a template, Shift+Enter: the launcher hides and NVDA reads the window you were in; nothing is said by the launcher and nothing is pasted; Ctrl+V in axDown pastes it.
4. Ctrl+Q on a template: the notice titled with its name and "1 line" or its count, the text with its marks; Escape.
5. Ctrl+Shift+Semicolon with nothing typed: the template used last is first.

## 9. The browser, settings and the rest (TP-4.6, TP-7)

1. Ctrl+B in the launcher: it hides and the browser opens, "Templates - axTemplate window".
2. Options, Hotkey in the launcher: the notice that takes the next key; press Ctrl+Alt+Shift+T, Enter: "Hotkey Ctrl+Alt+Shift+T", and the new key shows the launcher. Set it back to Ctrl+Shift+Semicolon.
3. F1 in the launcher: its own keys, Ctrl+B among them; Escape.
4. Session, Quit axTemplate: the launcher ends, and the hotkey does nothing until axTemplate is started again.

By ear, and the maintainer's: how the launcher comes up over other programs, whether the paste lands every time, and the key Ctrl+Shift+Semicolon under the fingers.
