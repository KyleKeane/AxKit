# axJSON NVDA test plan

The files for this plan are in `manual_tests/axJSON`, made and reset by `.\manual_tests\prepare.ps1 axjson`; its `_main.md` says which folder or file each section uses.

Manual checks with NVDA running, against `docs/axJSON/SPEC.md`. axJSON is the editor shell axDown is, so
`docs/axDown/nvda-test-plan.md` sections 1 to 7 (start, reading and writing, files, find and go to line, text size,
menus, updates) apply as they are, with "axJSON" for "axDown" and `settings.json` for the files; run them once on
axJSON after a change to the shell. What follows is axJSON's own. Prepare `settings.json`:

```json
{
  "model": "opus",
  "permissions": { "allow": ["Bash", "Read"], "deny": [] },
  "count": 3,
  "on": true
}
```

and `broken.json`, the same with the comma after `"opus"` removed and a comma added after `[]`.

## 1. Start

1. Start axJSON from the Start menu: NVDA reads "Untitled, axJSON window", then "Untitled edit multi line".
2. `axjson settings.json` in a console, and `axkit settings.json`: both open axJSON on the file, "settings.json, axJSON, <full path> window".
3. In File Explorer, right-click `settings.json`: the Open with axKit submenu lists axDown and axJSON, and axJSON opens
   the file; on a `.txt` file the submenu lists axDown only. Windows' own Open with lists axJSON for `.json`.
4. The file flows are the shell's: run axDown's section 3, step 7 (a file changed by another program) on a `.json`
   file once; the announcements name the file.

## 2. Navigation (AJ-2.2)

1. In `settings.json` with the caret at the top, Ctrl+K: "model: opus", the caret on the opening quote of `"model"`.
   Ctrl+K again: "permissions, object with 2 keys"; then "allow, array with 2 items"; "deny, empty array"; "count: 3";
   "on: true"; then "No next key", the caret staying. Ctrl+Shift+K goes back the same way; at the top "No previous key".
2. Ctrl+Home, Ctrl+J: "permissions, object with 2 keys" (the next object: the caret on the first brace is on the whole file already, as on a heading in axDown); Ctrl+Shift+J from there: "object with 4 keys" (the whole file); Ctrl+J, Ctrl+J: "permissions,
   object with 2 keys"; Ctrl+J: "No next object".
3. Ctrl+Home, Ctrl+L: "allow, array with 2 items"; Ctrl+L: "deny, empty array". Ctrl+Home, Ctrl+I: "Item 1 of 2,
   Bash"; Ctrl+I: "Item 2 of 2, Read"; Ctrl+I: "No next item".
4. Ctrl+E in `settings.json`: "No next error". In `broken.json`, Ctrl+Home, Ctrl+E: "No next error" as well: its missing
   comma and its trailing comma are repairs (AJ-2.2), which Format makes. A value left out is an error: in a copy with `"model": ,`,
   Ctrl+Home, Ctrl+E: "Error, line 2: expected a value, found ','', the caret there. Ctrl+Shift+E from the end lands there too.
5. Navigation, Next lists Key, Object, Array, Item and Error with their keys, Navigation, Previous the same; each item does
   what its key does. Help, User guide: Ctrl+K and the other keys move through the guide's text too, "No next key"
   when there is none.
6. Plain arrow keys speak nothing of axJSON's own.

## 3. Format (AJ-2.3)

1. In `settings.json`, put everything on one line (join the lines), then Ctrl+R: "Formatted"; Ctrl+Home and
   Down Arrow read `{`, then `  "model": "opus",`, two spaces per level, one key per line; Ctrl+Z puts the one line back
   ("Undone"); Ctrl+Y formats it again ("Redone"). axDown's section 2, step 6 (the history) holds here too.
2. In `broken.json`, Ctrl+R: "Formatted, repaired: 1 trailing comma, 1 missing comma"; Ctrl+E then says "No next
   error". Type `'x'` as a value somewhere and Ctrl+R: "Formatted, repaired: 1 single-quoted string".
3. Delete a value, leaving `"count": ,`, then Ctrl+R: "Not valid JSON: line 4, expected a value, found ','"; the
   text is unchanged. Actions, Format does the same as the key.
4. Ctrl+R on an already formatted file: "Already formatted", nothing changes, and the title shows no star.

## 3a. Options: choosing in place of typing (AJ-5)

The runner's script is `tests/scripts/axJSON/07-options.txt`. By hand: in axTask, Ctrl+J on a list opens its `list.json` here. The keys are Alt with an arrow (AJ-5.1a).

1. Ctrl+K to the `type` of a field ("type: check"), Alt+Right: NVDA reads the list, "type: What the field holds, list, check, 6 of 8". Down: "choice". Enter: "type: choice", the focus is back in the text and the line reads with `"type": "choice"`. Ctrl+Z: the old value is back.
2. Alt+Right, then Escape: the focus is back in the text and nothing changed.
3. On the same member, Alt+Down: "type: choice, 7 of 8" and the line reads with the new value; Alt+Down again: "type: link, 8 of 8"; once more: the edge sound and "type: link, the last of 8", nothing changed. Alt+Up: "type: choice, 7 of 8". Each press is one Ctrl+Z.
4. With the caret in an action, Alt+Left: "Add a member to an action of the list, list, key: The key that runs it; without one it runs from the menu, 1 of" the number it lacks. Enter: "Added key: The key that runs it; without one it runs from the menu"; the caret is on `"Ctrl+1"`. Alt+Right, End, Enter: "key: Ctrl+Shift+9".
5. On a field's `title`, Alt+Right and Alt+Down: "title takes any value. The field's name, as it is shown and said", no list and no sound.
6. Open an item of the list (Ctrl+J on an item in axTask). On a choice field, Alt+Right: the list's options; on a check, "true" and "false".
7. In any other JSON file, each of the four keys: "axJSON has no options for this file: it has them for a list's configuration and for an item of a list".
8. In navigation mode (Ctrl+Space), each of the four keys: the refusal of navigation mode, no list and no change.

## 4. The Window menu and About

1. Alt+K, A: Apps lists every standalone app and Alt+K, L every access layer; Alt+K, A, J starts a new axJSON window.
2. Alt+W, O: Open windows lists the other axKit windows, most recent first. Alt+H, About, Version names every app.

## The menu bar and Context (bundle AX-9.1, AX-9.11, 2026-11-11)

1. In the text, Shift+F10: Context opens on "Cut Ctrl+X"; Alt, A: Actions with Undo, Redo and Format; Alt, N: Navigation with the finds, Go to line and the jumps.
