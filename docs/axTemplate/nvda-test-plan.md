# axTemplate NVDA test plan

Run with NVDA before a release, after `scripts to run-individual-apps\run-axTemplate.ps1` (or `.\run.ps1 -App template`). Every step says what NVDA should read; a step that reads differently is a defect. The specification is `docs/axTemplate/SPEC.md` (TP-n.m). Sections 1 to 6 are the browser and are run by `tests/scripts/axTemplate` too; the launcher's sections come with it (SPEC.md section 8, step 2).

The steps change your own templates' folder: use a template made for the test, and delete it at the end.

## 1. Start (TP-1.1, TP-8.1, TP-8.2)

1. Start axTemplate: NVDA reads the title "Templates - axTemplate window", then "Templates, N templates, list" and the first template's name; with no template, the line "No templates yet: Ctrl+N makes one". Nothing else is spoken.
2. F1: the keyboard shortcuts as a notice; Escape closes it and the list has the focus again.
3. Alt+F4 closes the window without a question.

## 2. A new template (TP-8.4)

1. Ctrl+N: the form's sound, "New template, form", then "Type the name of the new template, edit". Type `Thanks`, Enter: "Added Thanks", and the list is on Thanks.
2. Ctrl+N, type `thanks`, Enter: "There is a template called thanks already", the notice still open. Escape: the list, on Thanks.

## 3. The fields (TP-8.3, TP-2.2, TP-2.3)

1. Enter on Thanks: "Fields of Thanks, list", "Title, text, Thanks". Down: "Text, long text, empty".
2. Enter on Text: a field of several lines. Type `Thank you, {clipboard}.{caret}`, Ctrl+Enter: the pane again, "Text, long text, Thank you, {clipboard}.{caret}".
3. Down: "Every axKit app, check, not done". Space: "done". Down: "axDown, check, not done", then the other apps' checks.
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
