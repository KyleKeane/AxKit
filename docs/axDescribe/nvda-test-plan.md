# axDescribe NVDA test plan

The manual checks of axDescribe (`docs/axDescribe/SPEC.md`, DE-n.m), run with NVDA before a release. Before starting:
AI help on (axKit menu, AI help, Let Claude help in the apps, the consent agreed), Claude Code installed and logged
in, `run-axDescribe.ps1` (or quit a running axDescribe first). The automated part is
`tests/scripts/axDescribe/01-start.txt` and `02-picture-file.txt`, which `nvda-pass.ps1 axDescribe` runs with a fake
Claude; the steps marked "(real)" need the real one and are the user's by ear, with the test pictures of
`tests/fixtures/pictures` (SPEC DE-8) for section 4.

## 1. The window in front described (DE-2.1, DE-2.2; real)

1. Open Notepad with a line of text, then press Windows+Alt+S: "axDescribe, access layer for Claude Code window",
   then the notice "Asking Claude"; a few seconds later "Claude says" and a description that names Notepad, the text
   and the menu bar. Down reads it line by line.
2. Tab: "Ask more about this answer, edit"; type "is there a menu bar?", Enter: "Asking Claude", then a new "Claude
   says" about the same picture.
3. Escape: "Ask about the picture, edit", the field empty; the status bar (NVDA+End) says "1 description kept".
4. Escape: axDescribe hides and Notepad has the focus again.
5. Windows+Alt+S on the desktop (Windows+D first): a description of the desktop and its icons.

## 2. The field and the picture (DE-2.3, DE-2.5)

1. Windows+Alt+S, Escape to close the description: Down: "14:xx Untitled - Notepad, 1 of 2" (the newest first); Up:
   the low note and the same again.
2. Type "notepad": Down says the Notepad description, 1 of 1. Ctrl+A, type "zzqq", Down: the low note, "No earlier
   description matches zzqq".
3. Ctrl+A, Delete, Down to the Notepad description, type "how many lines of text?", Enter: "Asking Claude", then
   "Claude says" about that picture (real).
4. Ctrl+O: axDescribe hides and axPreview opens on `last.png`, saying "Text read: N lines" after a moment, the words
   of the picture under the line naming it; Alt+F4.

## 3. Forget, off, keys (DE-2.6, DE-7)

1. Windows+Alt+S, Escape; axDescribe, Forget the descriptions: "Forget the descriptions?", Enter: "Descriptions
   forgotten"; Down: the low note, "No descriptions yet: the hotkey describes the window in front, Ctrl+D a picture
   file". Ctrl+O: "No picture yet: the hotkey takes one".
2. In another axKit window, AI help off; Windows+Alt+S: "AI help is off: axKit menu, AI help, Let Claude help in the
   apps", the window shown with its field; turn it on again.
3. F1: the keyboard shortcuts, with the hotkey, Ctrl+D, Ctrl+O and the field's keys; Escape. axDescribe, Quit
   axDescribe: the process ends and `%LOCALAPPDATA%\AxKit\axDescribe\last.png` is gone.

## 4. A picture file (DE-2.7; real)

1. Windows+Alt+S, Escape to close the description; Ctrl+D: the Windows file picker, "Choose a picture for Claude to
   describe", on the Pictures folder, the file type "Pictures (*.png, *.jpg, *.jpeg, *.gif, *.webp)". Type the path of
   `tests\fixtures\pictures\dialog.png` in the repository and Enter: "Asking Claude", then "Claude says" and a
   description that names a dialog asking to save changes to report.docx with the buttons Yes, No and Cancel.
2. Tab, "which button is the default?", Enter: the same picture answers (Yes, drawn with the blue frame).
3. Escape: the field; Down: "14:xx dialog.png, 1 of 2". Enter: the description again; Escape.
4. In a terminal: `axDescribe <repository>\tests\fixtures\pictures\chart.png`: the running axDescribe comes up and
   describes the chart (four quarters, Q4 the highest at 210). Escape, Escape.
5. In a terminal: `axDescribe <repository>\README.md`: a message box "README.md is not a picture axDescribe can
   describe: a .png, .jpg, .jpeg, .gif or .webp file" with the usage text; Enter closes it, no window changes.
6. Ctrl+D, Escape in the picker: the field has the focus and nothing was asked.
