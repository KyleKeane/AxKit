# AxKit.Forms: the text editor shell (axDown, axJSON, axPreview)

Part of the notes of `src/AxKit.Forms` (the index is `src/AxKit.Forms/CLAUDE.md`). One bullet per piece.

- `MarkdownKeys` (docs/axDown/SPEC.md §9; from axDown on its second use, axPreview, 2026-10-24): the Markdown navigation keys, Ctrl and NVDA's browse-mode letter for the next element, Ctrl+Shift for the previous; `Named` for a window's `All`, `Jumps` for the shell.
- `TextJump` (AX-7.2; one type with the jump itself since 2026-10-25, it was `TextJump` and `TextJumps`): a window's key pair for one kind of element in a text, with the kind's spoken name and the two functions that find the next and the previous element (`TextJump.Markdown` makes a pair from `AxKit.Core.MarkdownOutline` with the level for headings; JSON's outline brings its own functions), and the jump itself on any edit control (the pair for a key, the element from the pair's functions, the caret, one announcement) plus the menu label of a pair with the key's letter as the mnemonic. `TextJump.HeadingLetters` (h, Shift+H, 1 to 6 with Shift) is what every notice's text takes by default (2026-10-23; axClaude's and axTerminal's tables held a copy each); an editor builds its pairs from its own key table (`AxDownKeys.Jumps`, which is `MarkdownKeys.Jumps`) for its text and sets `OverlayPanel.Jumps` to the letters and its pairs together, and builds its Next and Previous submenus from the same list. `TextJumpsTests` in `tests/AxKit.Tests` covers it.
- `TextEditorForm`, `EditorApp`, `EditorKeys`, `EditorHost` (AX-6.2; docs/axDown/SPEC.md, docs/axJSON/SPEC.md AJ-D4): the text editor shell, axDown's `EditorForm` until axJSON, the second editor.
  - The files:
    - `TextEditorForm.Files.cs` holds the file (the read and the showing, new, open, save, save as, the disk check, the reload, the question before a change is lost) since 2026-10-24;
    - `TextEditorForm.Viewer.cs` what the viewer alone does.
  - `TextEditorForm` (on `AxKitForm`) is the window every editor app is, with the app as data (`EditorApp`: name, verb, settings file, file filter and default extension, F1 text, guide, the `TextJump` pairs of its outline, the Help resources submenu, and `EditorCommand`s on the whole text, each a function from the text to a `Rewrite`, applied as one undo step through the selection).
  - `EditorKeys` are the shell's keys, included in every editor app's table.
  - `EditorHost.Run` is the editors' entry point, one call into `AppHost.Run` with the one file of the command line, the settings and the window.
  - The undo history (docs/axDown/SPEC.md AD-2.5, 2026-10-21):
    - every `TextChanged` outside a load or a replay records the text before and after it into `AxKit.Core.EditHistory` with where the key was typed (`KeyPress` for characters, Backspace, Enter and Tab, `KeyDown` for Delete; -1 for a paste, a cut, a command), so that runs merge;
    - a one-second timer breaks the run;
    - Undo and Redo (Ctrl+Z, Ctrl+Y, Ctrl+Shift+Z, `EditorKeys`) apply the step through `Select` and `SelectedText` with the `_replaying` flag set and put the caret after the text put back;
    - a reload is recorded as a step, Open and New clear the history.
  - The whole text is read on every change (`_editor.Text`), accepted for the files the editors hold.
  - `LoadDocument` is the read (`ReadDocument`: what the window keeps on a refusal, a missing file in the viewer, a failed read) and the showing (`ShowDocument`).
  - The text's context menu is the bar's Context (Cut, Copy, Paste, Select all; `ContextFromControl`, AX-9.11, 2026-11-11) and the control's undo buffer is emptied after each change (AD-D1); Actions holds Undo, Redo and the app's commands, Navigation the finds, Go to line and the jumps.
  - `OnActivated` waits while a dialog closes (`DialogShowing`); the caret arithmetic is `AxKit.Core.TextLines`.
  - The viewer's own part (the read-only setup, its File items, its keys, the hand-over, Reload, Edit in axDown or axJSON) is `TextEditorForm.Viewer.cs` (2026-10-23).
    - Since 2026-10-24 the read-only mode also takes `EditorApp.Render` (a `ViewerPage`: the document, the renderer's name for the status bar, the Contents lines and the `ViewerWayOut`) and `EditorApp.Details` (the lines of a Details list, `AxKitList`, docked under the text, read on a task, the same file's lines kept until the new ones come);
    - `SwitchPane` takes Ctrl+Tab, Ctrl+Shift+Tab and F6 between the text and the list;
    - Ctrl+E goes by the page;
    - Session → Open with is filled from `Dispatch.Apps` as it opens (docs/axPreview/SPEC.md AP-6, AP-7).
