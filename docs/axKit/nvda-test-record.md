# NVDA test record

What was checked with NVDA, against which version, and with what result. When each goal last passed is in `docs/axKit/workflow-record.json`, written by the pass itself from 2026-11-16 on; this page keeps what a pass found. The passes before the full pass of 5.10.0 (2026-11-18), from 2026-10-22 on, were taken out on 2026-11-17 and 2026-11-23 and are in the git history of this file; what they left owed is in the Owed sections below. The plans are each app's `docs/<app>/nvda-test-plan.md`; the files they use are made by `manual_tests/prepare.ps1` (`manual_tests/_main.md`).

## How to add a pass

Add a section under this one, above the earlier passes, so that the newest comes first: the date, the version, the commit tested, who ran it and how the result was given, and the environment: the NVDA version, the synthesizer and the Windows build (so that a regression is dated to an NVDA release rather than to some time). Under it, one table per app with a line per section of its plan: the steps run and the result, Passed, Failed (with what was heard) or Not run. A step that fails gets a line of its own with what NVDA said and what it should have said, and the bug's fix names the step. When a plan gains or loses steps, the next pass lists them as the plan has them then.

## 2026-11-26, afternoon: the stream's scripts after its second round; by the assistant with NVDA listening (`main` 94e87522)

`.\nvda-pass.ps1 axClaude -Only '*-stream-*'`, the maintainer still away at his word, the same NVDA, voice and
Windows as below: 19 scripts, 56 goals, all passed, among them the new 55 to 59 (a command of the screen alone, No
and tell Claude why, what a tool printed, choosing an earlier conversation, plan approval) and the NVDA-only
`axClaude-stream-refused-why`, and the live 48, 49 and 52 with Claude Code 2.1.332.

## 2026-11-26: the full pass after the key moves, the titles and the stream's first round; by the assistant with NVDA listening (`main` 27581744)

`.\nvda-pass.ps1`, every app's scripts and the NVDA-only scripts, the maintainer away for four hours at his word ("i
am away for the next four hours so please work autonomously"), behind the spoken countdown, the same NVDA, voice and
Windows as below. 222 scripts: 217 passed; 1,362 goals passed, 5 failed, 2 not run. Heard in it for the first time:
the key moves of 2026-11-26 in every window (F7, F8, F9, Ctrl+Delete, Ctrl+Enter, F1 and Ctrl+Comma), the windows'
titles of d91881b8, and the stream's first round (scripts 55 and 56).

| What | Result |
|---|---|
| `axClaude/30-hooks-stand-in` | Failed once ("Events from Claude Code's hooks off" not spoken in 15 s); passed in the rerun, 7 goals: timing |
| `axClaude/53-permissions-search` (live, Claude Code 2.1.332) | Failed: Enter on "Search the rules (/)" gives no field; its item, "the permissions search live script fails at its search step" |
| `zz-nvda/axClaude-own-words`, `zz-nvda/axClaude-stream-refused-why` | Failed twice: each checked one sentence with `expect said` and then `expect spoken`, and `expect said` already waits for NVDA's speech (automation.md), so the second found nothing new. The repeated lines removed; both passed |
| `zz-nvda/axDown-notices` | Failed twice at "Command palette": the palette's title is cut by the focus landing in its field, and NVDA reads "Search commands" after the notice's sound, as an opening is heard (a sound, not words). The script now expects the field; it then failed once at its last Escape (NVDA said nothing but a cancel) and passed in the next run |

## 2026-11-25, evening: axTerminal's screen view; by the assistant with NVDA listening (`main` 5a68687d)

- `tests/scripts/axTerminal/14-screen-view.txt` with Windows' edit 1.2.1: 4 goals passed, every expected
  announcement also spoken by NVDA: the view's name and the program's line on the switch, "Reading the screen" and
  "Keys to edit" on Ctrl+Space, "New File, Ctrl+N" for the open menu's entry, the field back after edit ends.
- Not settled by the pass, still owed: whether Up and Down in edit are read in time, whether the marked entry is cut
  by NVDA's line reading, how the box characters sound (the Owed section of the screen view).

## 2026-11-25: the day's changes; by the assistant with NVDA listening (`main` e8de38c8)

`.\nvda-pass.ps1 -Only` ten scripts, the maintainer away for eight hours at his word ("i will go away for eight
hours, please work autonomously to finish everything you can"), behind the spoken countdown, the same NVDA, voice
and Windows as below. 23 goals passed, 1 failed, 5 not run; 8 of 10 scripts passed.

| What | Scripts | Result |
|---|---|---|
| axTask, Ctrl+N typing the title in the list (TK-4.3b) and the title's date and !priority (TK-4.3c): `13`, `26` | 2 | Passed |
| axStart, what was closed lately among the matches (AS-2.5): `05-recently-closed` | 1 | Passed |
| axClaude over the stream, Claude's questions with the answer's record in the conversation: `42-stream-questions` | 1 | Passed |
| axClaude, Ready as the window opens: `25-edit-modes`, `29-stand-in-turn` | 2 | Passed |
| A template in a form's field (TP-5.5): `axTask-template-in-a-form` | 1 | Passed |
| axDown's notices: `axDown-notices` | 1 | Passed |
| axMedia, a first file that cannot play as the window opens: `03b-cannot-play` | 1 | Failed: "Cannot play a-broken.mp3" was raised after the hold and not spoken; NVDA said the window, the list, the row and "Playing b-good.wav" |
| axClaude over the stream, the trust question as the window opens, then Choose the model and the effort: `50-stream-choices` | 1 | Failed at its first step: the notice's title "Start Claude Code in this folder?" not spoken (NVDA read the window, the message field and the notice's text); the four goals after it not run |

Both failures are one cause: NVDA read the newly active window later than the hold of 2026-11-25 waits, and that
reading cancelled the words. Its item: "what a window says as it opens is still cut off by NVDA's late reading".
The template in a form's field, whose menu NVDA had not read in three passes, passed here. Later the same day, with
the hold lengthened (8619dd26), `03b-cannot-play` and `50-stream-choices` passed with NVDA, 6 goals; and the search
of the permissions screen (`53-permissions-search`, live, Claude Code 2.1.332) passed with NVDA, 3 goals.

## 2026-11-24, evening: the day's changes and the afternoon's failures; by the assistant with NVDA listening (`main` 5d86519e and the fix after it)

`.\nvda-pass.ps1 -Only` eleven scripts, the maintainer away for the evening at his word ("i will be away for eight
hours and want you to work autonomously"), behind the spoken countdown, the same NVDA, voice and Windows as below.
50 goals passed, 2 failed, 1 not run; then the one script again after its fix.

| What | Scripts | Result |
|---|---|---|
| axClaude after an interrupt (FR-2.3a, Claude Code 2.1.332), its options with F1's every key, `25-edit-modes`, `29-stand-in-turn`, `axClaude-say-all` | 5 | Passed |
| axTask's Set and Clear fields (`28`) and the history of a list (`29`, TK-4.11) | 2 | Passed |
| axDown's Go to line and Find (`04`, the field under the text), `axDown-notices` | 2 | Passed |
| `axTask-template-in-a-form` (TP-5.5), the templates as Windows' own menu | 1 | Failed, then **passed** after the fix |
| `axMedia\03b-cannot-play` | 1 | Failed, below |

Found:
- `axTask-template-in-a-form.txt` step 24: an unexpected error as Shift+F10 opened the menu: `NativeMenu` declared
  `PostMessage` with exact spelling, and user32 has only `PostMessageW`. It uses `User32.PostMessage` now; heard again
  alone: "Thanks" was spoken in the field's menu, Enter put it in. The forms' templates are heard for the first time.
- `axMedia\03b-cannot-play.txt` step 20, a second time: "Cannot play a-broken.mp3" said as the window opens is cut off
  by NVDA's reading of the window and its list, while "Playing b-good.wav" a moment later is heard. Written on the
  item of announcements NVDA leaves out.

## 2026-11-24: every app and the NVDA-only scripts; by the assistant with NVDA listening (`main` e8536af1)

`.\nvda-pass.ps1`, behind the spoken countdown, after the maintainer's yes through the question tool, the same NVDA,
voice and Windows as the passes below. 204 scripts, 1287 goals passed, 7 failed, 19 not run; 14:19 to 15:41.

| What | Scripts | Result |
|---|---|---|
| The day's changes: Saved alone (axTask, axTemplate), the entering sound at every showing (axStart, axClip, axHelp), Set fields and Clear fields (axTask `28`), the AI tools' Open question (mcp `01`), axFile's new files of each kind, the list guard (axTerminal `02-commands`) | 14 | Passed |
| Every other app's window scripts | 179 | Passed but five, below |
| NVDA-only | 8 | Passed but three, below |

Found:
- `axTerminal\02-commands.txt`, which failed in the pass below on Windows Forms' list error, passed: the guard in
  `AxKitList` holds. Its item is done.
- NVDA did not speak an announcement made as the window opened, while it read the window and the focus:
  `axClaude\25-edit-modes.txt` step 13 and `axClaude\29-stand-in-turn.txt` step 20 ("Ready"),
  `axMedia\03b-cannot-play.txt` step 20 ("Cannot play a-broken.mp3", which passed in the pass below), and
  `axDown-notices.txt` step 15 ("notes.md" after Escape from the palette, passed below). The window said each one;
  the same race as axTerminal's `13-handed-command` on 2026-11-19, which passed three times when run alone. Heard
  again in the next pass.
- `axClaude\05-modes.txt` step 124 and `axClaude\09-options.txt` step 133, live turns of Claude Code: the second
  reply did not begin within 60 seconds, and with "first lines" the reply's first line was heard as "Alpha", the
  first line of the reply before. Run again on the hidden desktop: 05 failed at the same step with Claude Code
  2.1.331 and passed with 2.1.330. A recording showed why: 2.1.331 puts an interrupted message back on its prompt,
  and the next message was typed after it, the two sent as one. The window empties that prompt first now (axClaude
  SPEC.md FR-2.3a); 05 passes with 2.1.331. The "Alpha" of 09 did not come again; 09 then failed at step 200, where
  F1 now ends with every key of the window (AX-7.2a): the script steps back over that part, and 09 passes.
- `axClaude-say-all.txt` step 35: Claude Code still working 10 seconds after its reply was read, as in the pass
  below; a live script, waiting on the network.
- `axTask-template-in-a-form.txt` step 24: the field's menu opened ("Context menu") and held no template, as it
  should while the forms' templates are switched off (TP-5.5); the script waits for them.
- The command inventory lacked four rows of axHelp's axKit menu (Claude's working folder and program, the sounds
  folder, the update switch); written from axHelp's scripts.

## 2026-11-24: the moved hotkeys and a template of several lines; by the maintainer, by ear (`main` a1066a05)

After his rebuild. Passed:
- Ctrl+Shift+Comma brings up axClip, and Ctrl+Shift+Period takes axHelp's screenshot (bundle AX-8.11).
- A template of two lines from the launcher into PowerShell: the launcher asked how to put it in before pasting
  (axTemplate TP-4.8).
- The same template into axTerminal's command field went in with both lines, as TP-4.8 says for a field of several
  lines where Enter sends; Enter then ran both commands, and both echoes were heard.
- Ctrl+F1 with a menu of the window open opens axStart (bundle AX-6.6a), the bug he found on 2026-11-24.

## 2026-11-24: every app but axClaude, and the NVDA-only scripts; by the assistant with NVDA listening (`main` 96a4f3c3)

`.\nvda-pass.ps1 -Skip axClaude`, behind the spoken countdown, after the maintainer's yes through the question tool,
the same NVDA, voice and Windows as the passes below. 155 scripts, 1017 goals passed, 11 failed, 11 not run.

| What | Scripts | Result |
|---|---|---|
| Every app's window scripts but axClaude's: forms, Favorites, the list's own order, axHelp's offer, the drafted report, the titles | 147 | Passed but three, below |
| NVDA-only: `axDown-dialog-focus`, `axDown-notices`, `axFile-letter-backwards`, `axTemplate-put-in`, `axTerminal-handed-command` | 5 | Passed |
| NVDA-only: `axCalc-keys-from-a-menu` (AX-6.6a), `axTask-template-in-a-form` (TP-5.5), `axClaude-say-all` | 3 | Failed, below |

Found:
- `axMedia\04-text-notices.txt` step 48, `mcp\05-run-asks-in-a-list.txt` step 40 (and `axClaude\18-offered-command.txt`
  step 128, not run): the scripts named the update switch under Options, General, where it was until it became the
  bundle's one switch in the axKit menu (77b6345f). The scripts mended; both pass on the hidden desktop.
- `axCalc-keys-from-a-menu.txt` step 12: Alt+S opened Session on its first item, Save history, where the script
  expected Exit. The key under test was never reached; the script mended, heard again in the next pass.
- `axTask-template-in-a-form.txt` step 24: Shift+F10 in a form's field opened nothing. The window now opens the
  field's own menu itself while a notice shows (`AxKitForm.ShowFieldMenu`); the hidden desktop cannot open a menu,
  so it is heard again in the next pass.
- `axTerminal\02-commands.txt` step 148: an unexpected error inside Windows Forms' list code, as a list was destroyed
  while NVDA read it (Collection was modified, `ListBoxAccessibleObject.ResetListItemAccessibleObjects`). The script
  passes on the hidden desktop. Its item: "axTerminal: an unexpected error in Windows Forms' list as a list closes,
  with NVDA listening".
- `axClaude-say-all.txt` step 35: Claude Code was still working 10 seconds after its reply was read; a live script,
  waiting on the network. Not mended.

Two short passes after, the same day, of `axCalc-keys-from-a-menu` and `axTask-template-in-a-form` alone, each after
the maintainer's yes:
- b99ba578: both failed again. The runner's focus does not see a menu's item, while NVDA said it ("Session expanded
  ... Save history"): menu checks read `expect spoken` since (d781e3d8). The form's menu still opened nothing: F10 is
  the menu key, and its release closed a menu shown at once; it waits for the keys to be up since.
- d781e3d8: `axCalc-keys-from-a-menu` **passed**: Ctrl+F5 from an open menu opens axDown, F1 from an open menu shows
  the keys (bundle AX-6.6a). `axTask-template-in-a-form` failed a third time, NVDA heard only cancels after
  Shift+F10; the forms' templates (TP-5.5) are switched off until a pass hears them.

## 2026-11-23: axTemplate's launcher, axClip, axStart; by the assistant with NVDA listening (branch `axfile-navigator` 89d4ac77 to 941301c4)

`.\nvda-pass.ps1 -Only` axTemplate's, axClip's and axStart's scripts and `zz-nvda\axTemplate-put-in.txt`, from the
branch's own folder, behind the spoken countdown, each pass after the maintainer's yes through the question tool,
the same NVDA, voice and Windows as the passes below. Three passes, the third of the paste script alone.

| What | Scripts | Result |
|---|---|---|
| axStart, all four scripts | 4 | Passed (89d4ac77) |
| axClip, all four scripts | 4 | Passed (941301c4) |
| axTemplate's browser and launcher, `01-start` to `03-launcher` | 3 | Passed (941301c4) |
| axTemplate's paste into axDown, `axTemplate-put-in.txt`: Enter puts the template in, the caret at `{caret}`, the clipboard's text of before given back, Ctrl+Enter around a selection | 1 | Passed (941301c4 with the script's waits) |

Found and mended on the way:
- 89d4ac77, `03-launcher.txt` step 68: Shift+Enter said "Copied" as the window hid, and NVDA never spoke it, since
  it reads the window the front goes back to. The maintainer chose "Hide, say nothing" for axTemplate and axClip
  (941301c4; axClip CP-4.2, axTemplate TP-4.4).
- 89d4ac77, `axTemplate-put-in.txt` step 21: the launcher was started with `launch`, which waits for a start that
  hands over and ends; it is the run's second window, so `start`.
- 941301c4, the same script, step 32: the caret landed three characters short. The runner's `use` takes the front
  with Alt held for 50 ms, and it ran while the launcher sent its Left keys, which became Alt+Left. The script waits
  for the paste to end before `use`; the launcher is unchanged.

By ear, and the maintainer's: the paste, the caret and the clipboard in his own programs (Word, a browser, a
terminal), where a program that indents or pastes late can differ (TP-D5, TP-D6).

## 2026-11-23: axTemplate's browser; by the assistant with NVDA listening (branch `axfile-navigator` e8120737)

`.\nvda-pass.ps1 axTemplate`, from the branch's own folder, behind the spoken countdown, the maintainer away at his
word, the same NVDA, voice and Windows as the pass below, the build 5.10.0 of commit e8120737, the tree clean: 2
scripts, 13 goals and checks, all passed at the first run.

| Plan section (`docs/axTemplate/nvda-test-plan.md`) | Scripts | Result |
|---|---|---|
| 1 Start, 2 A new template, 3 The fields, 4 Rename, delete, undo: the empty list's line, F1, Ctrl+N with the name asked and a taken name refused, Text written in the field of several lines, a check set, Ctrl+Enter "Copied", F2 and the Title field "Renamed", Delete after its question, Ctrl+Z for the deletion and for the rename | `01-start.txt` | Passed |
| 5 Several at once, and reading again; 6 A file that is no template: "broken, not a template" and its refusals, a kept value's line, two selected and deleted with one Ctrl+Z, F5 | `02-several-and-not-a-template.txt` | Passed |

Not run, since no script reaches it: 5.3, a change made in axJSON shown on coming back to the window; 1.3, Alt+F4.
By ear, and the maintainer's: how the pane of eleven fields reads (Title, Text, nine checks), and the words of the
delete question.

## 2026-11-23: templates in the editors; by the assistant with NVDA listening (branch `axfile-navigator` 07b23771)

`.\nvda-pass.ps1 -Only` every script of axDown, axJSON and axPreview, from the branch's own folder, behind the spoken
countdown, the maintainer away at his word, NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the
build 5.10.0 of commit 07b23771, the tree clean: 34 scripts, 219 goals and checks, all passed at the first run.

| What | Scripts | Result |
|---|---|---|
| axDown, `18-templates.txt`: Link around a selected word with the address typed between the round brackets, Table with a word typed into its first cell, a template of the user's own from the command palette with the selection carried in, "insert template" in the palette, one undo, the refusal in navigation mode; "Link inserted", "Table inserted", "Meeting inserted" spoken | 1 | Passed |
| axDown's other scripts, axJSON and axPreview on the editor shell with the templates menu and the Templates folder item | 33 | Passed |

By ear, and the maintainer's: whether "inserted" is the right word, and how the caret's place after a template feels.

## 2026-11-23: the navigators after four changes from the maintainer's answers; by the assistant with NVDA listening (branch `axfile-navigator` a99a6c08)

`.\nvda-pass.ps1 -Only` every script of the eight navigators (axFile, axGIT, axProcess, axTask, axPass, axMCP,
axAutomate, axMedia), from the branch's own folder, behind the spoken countdown, the maintainer away at his word
("i am away for awhile so please work autonomously"), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro
10.0.26200, the build 5.10.0 of commit a99a6c08, the tree clean: 70 scripts, 539 goals and checks, all passed at
the first run.

| What | Scripts | Result |
|---|---|---|
| The edge sound in a list that holds nothing, and in a list of one item (axTask, `23-edge-in-small-lists.txt`): Up, Down, Home and End | 1 | Passed |
| The command palette with commands only: in axFile the start of a folder's name says "No commands match …", a command's name runs it (`05-text-notices-axkit-menu.txt`) | 1 | Passed |
| Ctrl+N, F2 and Delete in the details pane of axTask and axPass: the knock of a key with no command, nothing opened or changed | 2 | Passed |
| axFile, axGIT and axProcess on the navigator's shared work state (one piece of work at a time, a read whose late answer is dropped): every script | 26 | Passed |
| The other scripts of the eight navigators | 40 | Passed |

## 2026-11-22: the caution of a handed command as axTerminal's field name; by the assistant with NVDA listening (branch `axfile-navigator` 656663e1)

`.\nvda-pass.ps1 -Only` every script of axTerminal, axTask's `19-list-actions.txt` and the new NVDA script
`tests/nvda-scripts/axTerminal-handed-command.txt`, from the branch's own folder, behind the spoken countdown, the
maintainer away at his word, NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of
commit 656663e1: 15 scripts, 83 goals and checks.

| What | Scripts | Result |
|---|---|---|
| axTerminal, every script; in `13-handed-command.txt` the field's name is the caution while the line stands there as it came, "Command" once a letter is typed, the caution again when the letter is taken back, "Command" after the line was sent | 13 | Passed |
| axTask, `19-list-actions.txt`: the action that hands a command to axTerminal; the new window's field under its caution | 1 | Passed |
| The NVDA script: what NVDA itself speaks as the window opens | 1 | Failed at the first run on an expectation of the script's own, corrected, then passed |

What NVDA said as the window opened, from the report: the window's title, then "Command, put here by another program
and not sent. axKit checked nothing of it: read it, and press Enter only if you want to run it edit multi line" and
the command, in one piece of speech: the caution before the command, with nothing that could come between or cut
it off. The script had asked for the command's text as a second piece and found it already spoken with the first;
it asks for the name alone now. With a letter typed and taken back NVDA said the letter twice and no word about
the field's name changing; after Enter, "Sent" and "Done" and no more. The caution is spoken once, not a second
time as an announcement. This closes the failure of the pass at f8f3c2e6 below.

## 2026-11-22: Ctrl+Q on a row with detail lines, and axClaude's keys while Claude is stopped; by the assistant with NVDA listening (branch `axfile-navigator` 60954e9f)

`.\nvda-pass.ps1 -Only` every script of axAutomate, axTask, axPass and axMCP and axClaude's `08-lifecycle.txt`, from
the branch's own folder, behind the spoken countdown, the maintainer away at his word, NVDA 2025.3.3 with the OneCore
voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit 60954e9f, the tree clean: 35 scripts, 279 goals and
checks, all passed at the first run.

| What | Scripts | Result |
|---|---|---|
| axAutomate: every script; in `02-run.txt` the new check, Read in full on a step says the step and every line of its details ("2. Expect the focus: Nothing like this, failed" and "Last run: failed. Not within 10 s" in one announcement, spoken) | 5 | Passed |
| axTask, axPass and axMCP on the item window whose Ctrl+Q reads a row with its detail lines | 29 | Passed |
| axClaude, `08-lifecycle.txt`, live with the real Claude Code: with Claude stopped, the interrupt says "Claude stopped. Ctrl+Shift+R starts it", spoken, as a message sent then does | 1 | Passed |

## 2026-11-22: the list under a field in the hotkey windows and the palette, and axDown's format commands; by the assistant with NVDA listening (branch `axfile-navigator` ef8f3671)

`.\nvda-pass.ps1 -Only` every script of axStart, axClip, axHelp, axDown, axJSON and axPreview, from the branch's
own folder, behind the spoken countdown, the maintainer away at his word (NVDA passes without his approval), NVDA
2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit ef8f3671, the tree clean: 43
scripts, 270 goals and checks, all passed at the first run.

| What | Scripts | Result |
|---|---|---|
| axStart and axClip with the list under the field as the bundle's list (framework step 12): typing, the first match said, Down, Up, the page keys, first and last, the edge sound at an end, nothing found | 8 | Passed |
| axHelp, its two scripts, which are live: the window, the hotkey, the conversation started with the real Claude Code | 2 | Passed |
| axDown, every script, the command palette among them (its list is the same list now), and the new `17-format.txt`: a heading by Shift+Alt+Right and Shift+Alt+Left and from the menu, bold on a word by Ctrl+B, italic on a selection, list items on two lines and one undo, the refusal in navigation mode; every sentence spoken by NVDA ("Heading 1", "No heading", "Bold on", "List item on, 2 lines") | 17 | Passed |
| axJSON and axPreview on the editor shell that takes an app's format commands | 16 | Passed |

The pass ended with the note that the command inventory was stale for axHelp: "Entry sounds", an item of Options,
Text since the entry sounds came (step 2), was never written there, since axHelp's scripts are live and are left
out of every run of the folder. Written after the pass by one more run of axHelp's first script, on the hidden
desktop. By ear, and the maintainer's: the three keys of the format commands and the words "on" and "off"
(docs/axDown/SPEC.md AD-12.6).

## 2026-11-22: the console window's shared field, menus and session steps; by the assistant with NVDA listening (branch `axfile-navigator` f8f3c2e6)

`.\nvda-pass.ps1 -Only` every script of axClaude, axTerminal and axCalc, from the branch's own folder, behind the
spoken countdown, the maintainer away at his word (NVDA passes without his approval; the real Claude Code for this
step, with the small model), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of
commit f8f3c2e6, the tree clean: 65 scripts, 377 goals and checks; 64 scripts passed at the first run.

| What | Scripts | Result |
|---|---|---|
| axClaude on the console window's shared field keys, places, Sounds and done menus, closing question, restart and folder switch (framework step 11): the stand-in scripts and the live ones with the real Claude Code | 48 | Passed |
| axTerminal on the same: every script but one | 12 | Passed |
| axTerminal, `13-handed-command.txt`, step 15: the window said the caution of a handed command ("A command is in the command field, not sent. Another program put it there, and axKit checked nothing of it...") and NVDA did not speak it: it read the window's title, the field with the command, the shell's first lines and "Ready" | 1 | Failed once; passed three times in a row when run alone right after |
| axCalc: every script | 4 | Passed |

The failure is a race that is older than this step: what a window says before it is active is raised half a second
after its first activation, and NVDA's own reading of the window can come after that and cut it. It has its own
item in the priorities list, High, with two ways to make that caution certain. Before the pass, six live scripts of
axClaude (start, a message and its reply, reading, the folder, the lifecycle, the options) passed on the hidden
desktop with the real Claude Code, three after the first part of the step and three after the last.

## 2026-11-22: Filter and Sort by in every navigator, and axMedia's Ctrl+Enter and add; by the assistant with NVDA listening (branch `axfile-navigator` c22a5fd4)

`.\nvda-pass.ps1 -Only` every script of the eight apps on the navigator, from the branch's own folder, behind the
spoken countdown, the maintainer away at his word ("you are clear to do whatever gets the work done, including nvda
pass without my approval"), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of
commit c22a5fd4, the tree clean: 69 scripts, 537 goals and checks, none failed and none run twice.

| What | Scripts | Result |
|---|---|---|
| axFile: the filter as the navigator's (the list named "N items, filter text, n shown", the line of a filter nothing passes, Escape after the selection, a mark the filter hides dropped), Sort by Name, Date and Size; every other script | 13 | Passed |
| axTask: `22-filter-and-sort.txt`, new (the filter under headings, Escape, Sort by Name and back); every other script | 22 | Passed |
| axMedia: `09-filter-the-queue.txt`, new (the filter, Enter under it plays the file shown, Sort by Name reorders the queue and The order added puts it back); Ctrl+Enter plays with nothing playing, and the add that opened the window said as you arrive (`02-hand-over.txt`); every other script | 9 | Passed |
| axProcess: Sort by with its three orders under its new name; every other script | 6 | Passed |
| axPass, axMCP, axAutomate, axGIT: every script | 19 | Passed |

Not heard by a person: how the list's name under a filter reads, and whether the filter should look at more than
the name. Not covered by a script: the filter in axPass, axMCP, axAutomate and axGIT beyond the command being there
(the code is the navigator's one piece, heard in axFile, axTask and axMedia).

## 2026-11-22: axFile's rename on the navigator's rename box; by the assistant with NVDA listening (branch `axfile-navigator` b80893ce)

`.\nvda-pass.ps1 -Only` every script of axFile, from the branch's own folder, behind the spoken countdown, the
maintainer away at his word, NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of
commit b80893ce, the tree clean: 13 scripts, 116 goals and checks, none failed and none run twice.

| What | Scripts | Result |
|---|---|---|
| axFile: the rename in place through the item window's `RenameSelected` (F2, the name before the extension selected, a refused name with the box kept open, an unchanged name, a new text file's whole name, a file in use); every other script | 13 | Passed |

On the hidden desktop before the pass, one run of `11-two-windows-save.txt` failed once and passed in the four runs
after it: two windows closed 52 ms apart and one setting change was lost. It has an item of its own in the
priorities list; nothing of the rename touches it.

## 2026-11-22: the eight navigator apps, after axFile's selection left its Actions list; by the assistant with NVDA listening (branch `axfile-navigator` 51d76e58)

`.\nvda-pass.ps1 -Only` every script of the eight apps on the navigator, from the branch's own folder beside the
checkout, behind the spoken countdown, the maintainer away at his word (NVDA passes "Yes, for this whole absence"),
NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit 51d76e58, the tree
clean: 67 scripts, 529 goals and checks, none failed and none run twice.

| What | Scripts | Result |
|---|---|---|
| axFile (framework step 14): a selection writes no line into the Actions list, the status bar counts the selected, Ctrl+Z in the folder list undoes an operation and never the selection; every other script | 13 | Passed |
| axTask: every script | 21 | Passed |
| axPass: every script | 4 | Passed |
| axMCP: every script | 3 | Passed |
| axAutomate: every script | 5 | Passed |
| axMedia: every script | 8 | Passed |
| axProcess: every script | 6 | Passed |
| axGIT: every script | 7 | Passed |

This pass also hears, for the seven apps beside axFile, the navigator's groundwork of step 13 (commit 231c4a3b)
and the shared selection without its recorded line (commit 51d76e58), which the pass before it left to the hidden
desktop. Not heard by a person, and a line of the item of step 14 for the maintainer's word: that the undo of a
selection is gone with its lines.

## 2026-11-22: axFile as a navigator; by the assistant with NVDA listening (branch `axfile-navigator` c1cc505c)

`.\nvda-pass.ps1 -Only` every script of axFile, from the branch's own folder beside the checkout, behind the spoken
countdown, the maintainer away at his word ("i will revuild while you migrate axfile"; NVDA passes "Yes, for this
whole absence"), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit
c1cc505c, the tree clean: 13 scripts, 116 goals and checks, none failed and none run twice.

| What | Scripts | Result |
|---|---|---|
| axFile on `ItemForm` (framework step 13, first part): browsing, the lines that are no entry, the filter and the orders, the operations and their undo, the selection and its lines in the Actions list, the Actions list as the second pane (Tab and the pane keys, Escape back, Enter on a line), the notices, the palette, the hand-overs, the rename box | 13 | Passed |

Not run with NVDA: the seven other navigator apps, for the navigator's groundwork (commit 231c4a3b); their 54
window scripts passed on the hidden desktop with the command inventory checked, and nothing in the inventory
changed for them. Not heard by a person, and lines of the item of step 13 for the maintainer's word: the status
bar's count of the selected, Context on a line that is no entry, Shift+F10 in the Actions list, and Tab from an
open rename box.

## 2026-11-22: axCalc as a conversation window, and the conversation window in two classes; by the assistant with NVDA listening (`main` 3c652252)

`.\nvda-pass.ps1 -Only` every script of axCalc and axTerminal, behind the spoken countdown, the maintainer away at
his word ("i am stepping away so you can run ncda pass and get everythig completed with axcalc"), NVDA 2025.3.3 with
the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit 3c652252, the tree clean: 17 scripts, 110
goals and checks, none failed and none run twice.

| What | Scripts | Result |
|---|---|---|
| axCalc on `ConversationForm` (framework step 9): the answer, the field cleared, the history as a text read line by line, Enter on a line taking its expression into the field, Ctrl+I on the line you are on, a and l in the history, find, Clear history, the history kept between runs | 4 | Passed |
| axTerminal, the console window over the split conversation window: every script | 13 | Passed |

Not run with NVDA: axClaude, whose stand-in scripts (24) passed on the hidden desktop at the split's commit; its
live scripts start the real Claude Code and are run only when meant. Not heard by a person, and lines of the item
of step 9 for the maintainer's ear: how the history reads as a text, and whether the field should be one line.

## 2026-11-22: the selection in axPass, axMCP and axAutomate; by the assistant with NVDA listening (`main` cfe68be7)

`.\nvda-pass.ps1 -Only` every script of axAutomate, axMCP and axPass, behind the spoken countdown, the maintainer
away at his word (NVDA passes "Yes, for this whole absence"), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro
10.0.26200, the build 5.10.0 of commit cfe68be7, the tree clean: 12 scripts, 128 goals and checks, none failed and
none run twice.

| What | Scripts | Result |
|---|---|---|
| axAutomate: every script; in `05-stop-a-folder-run.txt` Space on two workflows and F5 runs those two alone (AU-4.4a) | 5 | Passed |
| axMCP: every script; in `02-your-servers-errors-prompts.txt` two servers of your own selected, removed after one question, and brought back by one Ctrl+Z (MP-1.5a) | 3 | Passed |
| axPass: every script; in `01-unlock-read-change.txt` two entries selected, moved to the Recycle Bin, and brought back by one Ctrl+Z (PW-6.4a) | 4 | Passed |

Not heard by a person, and a line of the item of step 10 for the maintainer's word: that Delete on several entries
in axPass asks no question, as Delete on one entry asks none.

## 2026-11-22: axGIT as a navigator, and the navigator's text pane; by the assistant with NVDA listening (`main` 0e040525)

`.\nvda-pass.ps1 -Only` every script of axGIT, axTask, axPass, axMCP, axAutomate, axMedia and axProcess, the seven
apps on the navigator, behind the spoken countdown, the maintainer away at his word (NVDA passes "Yes, for this
whole absence"), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit
0e040525, the tree clean: 54 scripts, 409 goals and checks, none failed and none run twice.

| What | Scripts | Result |
|---|---|---|
| axGIT on `ItemForm` with the text pane (framework step 8): the start, up to the repository and into another list, the diff and the commit in the details with Tab, Shift+Tab and Escape back, Stage and Unstage on the file you are on and on two selected files, the commit, Discard on one file and on the selected, the consents, a repository's own programs | 7 | Passed |
| axTask, axPass, axMCP, axAutomate: every script, after the text pane came into the item window | 33 | Passed |
| axMedia, axProcess: every script | 14 | Passed |

The run ended with the error code for a stale command inventory, and nothing else: axGIT's inventory had been
written while the repository was still being read, when Context holds "Nothing selected" in place of a file's
commands. The script `01-start.txt` asks for the command list after the list has its files now, and the inventory
was written again; a fault of the script's timing, not of the window.

Not heard by a person, and lines of the item of step 8 for the maintainer's word: the staging keys Ctrl+Shift+S
and Ctrl+Shift+U, and whether Delete on one file should have Cancel as its default as it has for several.

## 2026-11-22: axProcess as a navigator, its three views three lists; by the assistant with NVDA listening (`main` 6524dfc8)

`.\nvda-pass.ps1 -Only` every script of axProcess and axMedia, behind the spoken countdown, the maintainer away at
his word (NVDA passes "Yes, for this whole absence"), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro
10.0.26200, the build 5.10.0 of commit 6524dfc8, the tree clean: 14 scripts, 99 goals and checks, none failed and
none run twice.

| What | Scripts | Result |
|---|---|---|
| axProcess on `ItemForm` (framework step 7): the start inside Processes, up to This computer and into another list, the details, the words on a service, Turn off, Turn on and Remove on the stand-in's startup entries | 5 | Passed |
| axProcess: Space selects two processes the script started and two of the stand-in's startup entries; Delete ends or removes them after one question (`06-select-several.txt`, new) | 1 | Passed |
| axMedia, again after the Context menu's change in the item window | 8 | Passed |

Not heard by a person, and lines of the item of step 7 for the maintainer's word: that Tab no longer goes round the
three lists, that Alt+1, 2 and 3 stay as jumps, and the words on Delete on a service.

## 2026-11-22: axMedia as a navigator on the item window, and the item window's hooks for it; by the assistant with NVDA listening (`main` 89362d2c)

`.\nvda-pass.ps1 -Only` every script of axMedia, axTask, axPass, axMCP and axAutomate, behind the spoken countdown,
the maintainer away at his word (NVDA passes "Yes, for this whole absence"), NVDA 2025.3.3 with the OneCore voice,
Windows 11 Pro 10.0.26200, the build 5.10.0 of commit 89362d2c, the tree clean: 41 scripts, 313 goals and checks,
none failed and none run twice.

| What | Scripts | Result |
|---|---|---|
| axMedia on `ItemForm` (framework step 6): the queue, playing, the Context menu with "Queue is empty", the selection with its count in the status bar, Right, Left and Backspace as the edge sound | 8 | Passed |
| axTask: every script | 21 | Passed |
| axPass, axMCP, axAutomate: every script | 12 | Passed |

Not heard by a person: whether the edge sound on Right and Left in the queue is what the maintainer wants there
(they moved the row before); it is a line of the item of step 6 for his ear.

## 2026-11-22: the entry sounds raised by the components, and axMedia's selection; by the assistant with NVDA listening (`main` 4f0f309b)

`.\nvda-pass.ps1 -Only` every script of axMedia, axTask, axFile, axMCP, axCalc and axProcess, and axTerminal's
`08-user-shells`, behind the spoken countdown, the maintainer away at his word (NVDA passes "Yes, for this whole
absence"), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit 4f0f309b,
the tree clean: 55 scripts, 442 goals and checks.

| What | Scripts | Result |
|---|---|---|
| axMedia: Space selects, Ctrl+Enter pauses, Delete takes the selected files out; the new `07-selection.txt` | 8 | Passed |
| axTask: every script | 21 | Passed |
| axFile: every script | 13 | Passed, `03-operations` at its second run |
| axMCP, axCalc, axProcess: every script | 12 | Passed |
| axTerminal: Add a shell's form | 1 | Passed |

One step failed in the first run and passed in a second run of its script alone, straight after: in axFile's
`03-operations`, the window announced "None selected" after Deselect all, and NVDA did not speak it; it had spoken
the "Selected" of the step before late, after that step had ended, and the next announcement was lost with it.
The script runs two menu commands one right after the other there, faster than a person does. Taken as NVDA
speaking late once: the window's own announcement was right both times, and the same step passed with NVDA in two
earlier passes of the day. If it is seen again it gets an item.

The speech was as before for every app: the entry sounds are sounds, and NVDA's words did not change with them.
His by ear, as written under the pass of 598ab96b: the four sounds in real use, now also at every Tab between
panes and at a window's start.

## 2026-11-22: what opens says its kind, and a form says its title and "form"; by the assistant with NVDA listening (`main` 598ab96b)

`.\nvda-pass.ps1 -Only` every script of axTask and of axMCP, axTerminal's `08-user-shells` and axFile's
`03-operations`, behind the spoken countdown, the maintainer away at his word ("i am away so you can run nvda pass
when you are ready"), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit
598ab96b, the tree clean: 26 scripts, 219 goals and checks, all passed with every announcement also spoken by NVDA.

| What | Scripts | Result |
|---|---|---|
| axTask: every script, the new `21-what-opens.txt` and the forms of `18-item-form` and `20-selection` | 21 | Passed |
| axMCP: Add server's form, a choice's list | 3 | Passed |
| axTerminal: Add a shell's form | 1 | Passed |
| axFile: the operations, with the rename box in place | 1 | Passed |

What was heard when a form opens: the first control as NVDA reads it, then the one short line, and nothing of the
explanation: "Choose the list, list. Later. Move 2 items, form"; "Type the title, edit, selected, Fix the tap.
Fields of Fix the tap, form"; "Type the name of the server, edit, blank. Add server, form"; "Add a shell, form".
Ctrl+Q then reads the explanation whole, "The fields of Fix the tap, in the order of its list. Tab moves from one
to the next. …". A text still reads as NVDA reads it, "Keyboard shortcuts, edit, read only, multi line, In this
text: …", and a field "Name contains, edit". The four sounds were raised where expected (the runner hears that a
sound was played, by its name; it does not hear the sound itself), and none with the switch off.

His by ear, not heard here: the four sounds in real use (whether each is told from the others and from the
heading, edge, question and background sounds at speed, and whether the form's, at 0.7 s, sits well under NVDA's
reading of the first control); and whether the title and "form" coming after the first control, not before it,
is the order he wants. It comes after because a line said before the focus moves is cut off by NVDA's reading of
the control.

## 2026-11-22: Space selects, Ctrl+Enter is Done in axTask, on the shared selection; by the assistant with NVDA listening (`main` 7543da40)

`.\nvda-pass.ps1 -Only` every script of axTask and the four of axFile that select (`01-browse`, `03-operations`,
`06-actions-list`, `07-move-to-folder`), behind the spoken countdown, the maintainer away at his word ("i am
stepping away for awhile so you can work autonomously and run everything you need"), NVDA 2025.3.3 with the OneCore
voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit 7543da40, the tree clean: 24 scripts, 218 goals and
checks, all passed with every announcement also spoken by NVDA.

| What | Scripts | Goals | Result |
|---|---|---|---|
| axFile: the selection on the shared code (`ListSelection` over `Marks`), unchanged for the ear | 4 | 88 | Passed |
| axTask: every script, Done on Ctrl+Enter, and the new `20-selection.txt` | 20 | 130 | Passed |

What was heard in axTask: Space on an item says "Selected" and NVDA does not read the row a second time after it;
an arrow onto a selected row reads "Buy paint, selected"; Shift+Down says nothing but the row reached, "Oil the
hinge, selected"; "Selected, 2 selected", "Deselected, 1 selected", "Selected, 4 selected" after Shift+Space, "None
selected", "5 selected"; on a heading "A heading: nothing to select", on a list "Items are selected, not lists:
Right goes into this one"; "Done: 2 tasks. 1 was done already.", "Not done: 3 tasks", "Priority high: 3 items",
"Later: 3 items moved to Later", "Archived 2 items", and each "Undone: …". With nothing selected Ctrl+Enter says
"Done: Fix the tap" as Space did before.

The same sitting at 14262c85 (Ctrl+M, Move to a list): `axTask\20-selection.txt` again, 13 goals and checks, all
passed and spoken. Heard: "Choose the list list, Later", then the form's explanation once; "2 items moved to
Later"; in the name's field "Type the new list's name, when you chose A new list, edit, blank", then "Buy paint
moved to Someday"; each "Undone: …". One thing for his ear: in a form's list NVDA sometimes adds its own "not
selected" to a line ("Later not selected"), which is the list box's state and has nothing to do with the items
selected with Space; it is so in every form's list, Move to section too.

Not heard here, his by ear: how Space as select and Ctrl+Enter as Done feel under the fingers in his own lists.
Not run yet at this commit: the window checks of the other apps, which the shared window code changed under; they
are recorded before the next release.

## 2026-11-22: Done before the result, in axClaude and axTerminal; by the assistant with NVDA listening (`main` 37472e06)

`.\nvda-pass.ps1 axTerminal`, then `-Only` six scripts of axClaude that hear the end of a turn (`29`, `35`, `36`
and `37` with the stand-in, `09-options` and `17-what-is-spoken` with the real Claude Code, Haiku), each behind the
spoken countdown, the maintainer away from the VM's keyboard at his word ("Start now"), NVDA 2025.3.3 with the
OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit 37472e06, the tree clean: 19 scripts, 109
goals and checks, all passed with every announcement also spoken by NVDA.

| What | Scripts | Goals | Result |
|---|---|---|---|
| axTerminal: "Done" before the output, the switch Announce "Done", a failure said all the same | 13 | 69 | Passed |
| axClaude: "Done" and then the reply as one utterance, with the stand-in and with the real Claude Code | 6 | 40 | Passed |

What was heard: in axTerminal "Done. hello", "Done" and then a whole listing with none of its rows before the word,
with the switch off the chime and "again" alone, "Failed, exit code 3"; in axClaude the done chime and, a third of
a second later, "Done. claude: Alpha. The quick brown fox jumps. Beta. …" as one utterance, "Done. claude: first",
"Done. green apples" with the silent line left out. Before the change the log of the maintainer's own window showed
Done decided 0.2 to 0.3 seconds after the work ended and heard only after NVDA had read the whole reply.

The same sitting, without NVDA: axClaude's sixteen stand-in scripts and three live ones (`09`, `17`, `27`) on the
hidden desktop, all passed; every app's window checks are recorded at the same commit.

The countdown stopped the first start of this pass too, when input reached the VM during its sentence: on the host
the mouse pointer over the VM's window is input to the VM.

Not heard with NVDA here: axClaude's other live scripts, which have no reply speech and expect "Done" alone as
before. Owed to his own ear: whether "Done" and the reply in one breath reads well after a long turn, and whether a
third of a second of hold is felt in axTerminal's output.

## 2026-11-22: the maintainer's decisions on the keys, the caution and the chime; by the assistant with NVDA listening (`main` cb898468)

`.\nvda-pass.ps1 axTask`, then `-Only` axJSON, axPass, axMCP and axAutomate, each behind the pass's new spoken
countdown, the maintainer at the computer and keeping his hands off at his word ("Start it now"), NVDA 2025.3.3 with
the OneCore voice, Windows 11 Pro 10.0.26200, KeePassXC 2.8.0, the build 5.10.0 of commit cb898468, the tree clean:
38 scripts, 291 goals and checks, all passed with every announcement also spoken by NVDA.

| What | Scripts | Goals | Result |
|---|---|---|---|
| axTask: the AI caution in the question before a run and a hand-over, the done chime, the field move gone | 19 | 121 | Passed |
| axJSON: Alt+Right the list, Alt+Down and Alt+Up the value in line, Alt+Left a member | 7 | 170 together | Passed |
| axPass, axMCP, axAutomate: the field move gone from the item window | 12 | with axJSON's | Passed |

What was heard: "Run Find the kind? An AI may have written it: read the explanation before you run it"; the done
chime before "Find what I type: done"; in axJSON "type: link, 8 of 8", the edge sound with "type: link, the last
of 8", "type: choice, 7 of 8", and the list of values on Alt+Right and of members on Alt+Left.

The pass's own warning, new that day (101307d8): the first start was stopped by a key or the mouse during the
spoken sentence, said as "Stopped. No test pass", and nothing was taken; asked again, the maintainer chose to start
it, and both parts ran after the count from ten.

Owed to his own ear: the four Alt keys in use on his own lists; whether stepping in line should say less than the
member's name and the place each time; whether the question's longer name is right to hear on every opening.

## 2026-11-21: axAutomate's Refresh and axPass's field that cannot be emptied; by the assistant with NVDA listening (`main` fb38565f)

`.\nvda-pass.ps1 axAutomate`, then `axPass`, the user away at his word of the same day, NVDA 2025.3.3 with the
OneCore voice, Windows 11 Pro 10.0.26200, KeePassXC 2.8.0, the build 5.10.0 of commit fb38565f, the tree clean: 9
scripts, 76 goals and checks, all passed with every announcement also spoken by NVDA.

| What | Scripts | Goals | Result |
|---|---|---|---|
| axAutomate, `04` choosing Session, Refresh by its name again (ceca8f15) | 5 | 30 | Passed |
| axPass, `04` with a field emptied and an undo into an empty field (fb38565f) | 4 | 46 | Passed |

What was heard: in axAutomate "Refreshed: flows, 3 workflows, 0 folders" from the menu's Refresh, and no run
started; in axPass "URL cannot be emptied here: KeePassXC's tool takes an empty value as no change. Empty it in
KeePassXC's own window" with the URL read as before, and "Not undone: Notes: First line of the notes, and 1 more
line. Notes cannot be emptied here: …", then "Nothing to undo".

Owed to his own ear: whether the sentence of the field that cannot be emptied is the right length, and whether it
should carry the refusal's sound, which it does not, as axPass's other sentences of this kind do not.

The same evening, at 69c52c86: `.\nvda-pass.ps1 axProcess`, 5 scripts, 42 goals and checks, all passed, after
`01-start.txt` was changed to end only a copy of ping under a name of its own (axkitscript01); no such process was
left after the run. Before it took the keyboard the pass found key or mouse input of that moment and waited for its
20 seconds without input, as built; whose input it was is not known, and no further pass was run that sitting.

## 2026-11-21: axTask's actions, axTerminal's handed command, axJSON's options; by the assistant with NVDA listening (`main` 31fe68c1)

`.\nvda-pass.ps1 axJSON`, then `axTerminal` and `axTask`, the user away at his word of the same day, NVDA 2025.3.3
with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit 31fe68c1, the tree clean: 39 scripts,
231 goals and checks, all passed with every announcement also spoken by NVDA. The first hearing of steps 4 and 6
of `docs/axTask/forms-framework.md` (3be92544, afbcb2dd, 2fbec3e1).

| What | Scripts | Goals | Result |
|---|---|---|---|
| axTask, with `19-list-actions.txt` new and `04-action-keys.txt` rewritten | 19 | 120 | Passed |
| axTerminal, with `13-handed-command.txt` new | 13 | 66 | Passed |
| axJSON, with `07-options.txt` new | 7 | 45 | Passed |

What was heard. axTask: an action by its key and from Context, Actions of this list ("Mark it a bug: Kind: Bug");
an action that is not understood, by its reason; the question "Run Find the kind?" read as a notice, its focus on
Cancel's side of Enter; "Running Find the kind", "Find the kind: done", "Miss failed, exit code 1" with the notice
sound and its output opened; the form of an action; "Wait stopped"; "Put Look in the terminal into axTerminal?" and
"Look in the terminal is in axTerminal's command field". axTerminal: the command field read with the handed line in
it, and "A command is in the command field, not sent. Another program put it there, and axKit checked nothing of
it: read it, and press Enter only if you want to run it"; "No output yet" before Enter, "Sent" and "Done" after.
axJSON: the list of a member's values by its name ("type: What the field holds"), the value chosen ("type:
choice"), the list of an object's missing members, "Added key: …", an item's choices through its link, and the
sentence of a file without options.

An earlier pass of axTask the same day, at 029af1bb, passed the same 120 goals; it was recorded over files of the
next step that were being written, so its record was set aside and the pass run again here on a clean tree.

Owed to his own ear: whether the question before a program's first run is the right length to listen to, and reads
well from the top with the arguments one on a line; whether "Running" and "done" are enough for a program that
takes a while, or a sound should mark the end; whether Alt+Down and Ctrl+Shift+M are the keys he wants in axJSON,
and whether the lists' names ("type: What the field holds") are right to hear on every opening; and how the handed
command's sentence sits with NVDA's own reading of the window as it opens.

## 2026-11-21: axTask, the item's form; by the assistant with NVDA listening (`main` bbf77601)

`.\nvda-pass.ps1 axTask`, the user away at his word of the same day, NVDA 2025.3.3 with the OneCore voice, Windows 11
Pro 10.0.26200, the build 5.10.0 of commit bbf77601: axTask's 18 scripts, 110 goals and checks, all passed with every
announcement also spoken by NVDA. The first hearing of step 2 of `docs/axTask/forms-framework.md` (f10eeb0f): Enter
on an item opens its form.

| What | Scripts | Result |
|---|---|---|
| `18-item-form.txt`, new | 1 | Passed |
| The seventeen others, `01` and `13` following the form | 17 | Passed |

What was heard: the form opening as every form of the bundle does, NVDA reading its title and explanation ("Fields
of Fix the tap … The fields of Fix the tap, in the order of its list. …") and then the focus in the title's box;
each control by its name, an instruction; "Saved Fix the tap. Done: done. Due: 2 November 2099, Monday. Priority:
Normal. …" once; the refusal of a date with the focus in its box; "Nothing changed"; the form opened on a field
from the detail pane, and the focus back on that field after it.

Owed to his own ear: how the form feels with a list of many fields (the priorities list has ten), and whether the
controls' names, which are instructions with what a date takes, are the right length to hear at every Tab.

## 2026-11-21: axTask, the lists' new files; by the assistant with NVDA listening (`main` c7f61977)

`.\nvda-pass.ps1 axTask`, the user still away at his word of the same day, NVDA 2025.3.3 with the OneCore voice,
Windows 11 Pro 10.0.26200, the build 5.10.0 of commit c7f61977: axTask's 17 scripts, 102 goals and checks, all
passed with every announcement also spoken by NVDA. It is the first hearing of step 1 of
`docs/axTask/forms-framework.md` (b89207a6): an item as its values read through its list's configuration.

| What | Scripts | Result |
|---|---|---|
| The fifteen scripts there were, their lists and items in the new shape | 15 | Passed |
| `16-convert-earlier-format.txt` and `17-list-without-configuration.txt`, new | 2 | Passed |

What was heard for the first time: the field keys' sentence ("The fields are the list's configuration's: Ctrl+J on
the list opens it in axJSON"); a field shown empty and given its first value; "1 more field kept: Room" and the
notice "Fields kept"; a configuration that does not read, in the level's name, the row and the item's last line, and
the refusal of a change; the row "earlier format: Enter converts it", the question "Convert Home?" with its three
answers, "Converted Home: 2 items. A copy is among the Archived lists" and its undo; an item of a folder without a
`list.json` shown as text, and "Written the configuration of Notes".

Not heard by this pass and owed to his own use: the line said on coming back from axJSON ("The list's configuration
changed: 5 fields, 2 actions"), which needs the two windows on the real desktop (test plan 2.5), and the pass of
section 9 of the plan by hand on `manual_tests/axTask`. The other item apps (axPass, axMCP, axAutomate) passed their
window checks on the hidden desktop at b89207a6; the shared window changed by one line for them.

## 2026-11-21: every app, after the audit's repairs; by the assistant with NVDA listening (`main` 7a683b1b)

`.\nvda-pass.ps1`, the user away at his own word ("i am going away for a long time so please just move forward
autonomously"), NVDA 2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.10.0 of commit 7a683b1b
(the record says `7a683b1b+`: the working folder held new pure code of axTask's lists that no window uses yet): the
170 scripts of `tests/scripts` and the five of `tests/nvda-scripts`, 175 in all. 174 passed with every announcement
also spoken by NVDA; 1115 goals and checks passed, 1 failed and 24 were not run after it. Then
`.\nvda-pass.ps1 -NoBuild -Only 'axClaude\04-headings.txt'`, the one script again on the same build: 25 goals, all
passed. Every goal and check of the record, 1140, has a pass with NVDA as its last result. It is the first pass with
NVDA of what the audit of 2026-11-21 changed: the approval notices of axTask (the confirmation that names itself, the
focus in the entry's text), the stream source's repairs, the new settings of a first start of axClaude, the sound of
sending played once, and the count of commands left running.

| What | Scripts | Result |
|---|---|---|
| Every script but one | 174 | Passed in the whole pass |
| `axClaude/04-headings.txt` | 1 | One step failed in the whole pass, by the live model's words; passed when run again |

What it found:

- The headings script asks Haiku for a reply with the headings Alpha and Beta and then moves through them with `h`.
  In the whole pass `h` went from "claude: Alpha heading level 2" straight to "Input 2 heading level 1": the reply
  held no second heading that time. The script's own header says a step that waits for Claude's words can fail when
  the model words it differently; no code lies between the two runs, and the second reply held both headings. Nothing
  to repair in axClaude.
- This run was started without the question tool, on his word that he was away for a long time and wanted no
  questions; the pass's own guard (20 seconds without a key or the mouse) held both times. The rule of 2026-11-18
  stands for every run while he is at the computer.

## 2026-11-18: every app, before 5.10.0; by the assistant with NVDA listening (`main` 4830f1f0, then e10f28cf for ten scripts)

`.\nvda-pass.ps1` of 2026-11-17 to 18, the user away at his own word ("do the nvda pass"), NVDA
2025.3.3 with the OneCore voice, Windows 11 Pro 10.0.26200, the build 5.9.1 of commit 4830f1f0: the 167 scripts of
`tests/scripts` and the five of `tests/nvda-scripts`, 172 in all, in an hour and a quarter. 164 passed with every
announcement also spoken by NVDA; 1102 goals and checks passed, 6 failed and 25 were not run after them. Then, with
the user away from the keys at the question's answer, two short runs at e10f28cf (no window's code lies
between the two commits): `.\nvda-pass.ps1 -Only '*-stream-*', 'axMedia\03-playing.txt'`, the eight scripts of the
stream and axMedia's, 38 goals, all passed; and `.\nvda-pass.ps1 axHelp -Only '01-start.txt' -Inventory write`, for
the command inventory. Every goal and check of the record, 1133, now has a pass with NVDA as its last result. The
passes wrote each result into `docs/axKit/workflow-record.json` themselves.

| What | Scripts | Result |
|---|---|---|
| Every script but eight | 164 | Passed in the whole pass |
| `axClaude/37` to `43`, the stream scripts that name a recording by its case | 7 | Failed in the whole pass at the stand-in of Claude Code, a fault of the test tool; passed in the second run |
| `axMedia/03-playing.txt` | 1 | One step failed in the whole pass, by NVDA's own timing; passed in the second run |

What it found:

- The seven stream scripts failed before their first step: "Claude Code ended right after it was started over its
  stream (exit code 2)". The stand-in of the stream looked for its recordings only under the repository above its
  own program, and the runner of an NVDA pass is built into `%TEMP%`. `44-stream-same-question.txt`, which gives its
  recording by a path, passed in the same pass, so the window itself was heard. The runner now hands its repository
  on (`AXKIT_STANDIN_REPOSITORY`, 5ad84529), and all eight stream scripts passed with NVDA in the second run: the
  folder's question, Ready, a turn with Done, the keys, the end of Claude Code, a tool's question each way with its
  caution, Claude's own questions, the interrupt, and the second question that reads as the first.
- axMedia's step sends Faster twice with nothing between and expects "Speed 1.5". The window said "Speed 1.25" and
  "Speed 1.5", 58 ms apart; NVDA spoke the first and not the second within 15 s. In the second run the two were 36 ms
  apart and NVDA spoke both. The script and axMedia are as they were when the step passed on 2026-11-16: of two
  announcements some tens of milliseconds apart, NVDA sometimes speaks only the first. No user's two key presses
  come that close; a key held down could, and that is on the priorities list.
- The pass told nobody how long it would take: the user came back into it and waited without a word of its length.
  Windows' voice now says the test's number, how many remain and about how long they take (e10f28cf; section 3 of
  `automation.md`), heard by him in the second run. And the second run was first started without the question, on
  the belief that he was away; it refused by itself when it saw his keys, which is what its guard is for. Every
  NVDA run is asked for through the question tool again.
- `nvda-pass.ps1` gained `-Only`, by which the second run took two and a half minutes where the whole pass takes
  an hour and a quarter.
- The command inventory was stale in one row, as the Owed section of 2026-11-16 said it would be until a pass
  wrote it: axHelp's "Ask about this window" lacked its further key Ctrl+Slash, since only live scripts list
  axHelp's commands and no run on the hidden desktop plays them. `nvda-pass.ps1` gained `-Inventory write` for it;
  the third run (`axHelp/01-start.txt`, 6 goals, passed) replaced that one row and no other.
- Heard for the first time, and taken off `docs/axKit/owed.md`: the refusals of a settings file whose seal does not
  check in axGIT, axMCP, axFile and axClaude (R-53), the settings that cannot be saved in an item window and in a
  console window (R-63), the remembered path on the network (R-64) and a server's error lines in axMCP (R-74). And
  of the Owed sections below, their parts for a pass: the state, Done and a turn Claude Code begins by itself, from
  the event core (`axClaude/34`, `35` and `36`, the plan's section 25, steps 4 to 6); the sentences that name Alt+1
  to 3, Ctrl+Period and Ctrl+Slash (`axGIT/01`, `axClaude/23`, `axTask/09`); a menu's refusal in navigation mode
  (`axDown/16`). What those sections still hold is for the user's ear.
- Still owed a hearing, since no script reaches them: the notice about a changed file in Claude's working folder
  (R-54), the AI tools submenu after a change from outside (R-56), the count of waiting reports (R-57) and the
  command line in axMCP's notice before a server starts (R-61), as on 2026-11-16.

## Owed: axClaude over the stream, what a tool printed, choosing a conversation, plan approval (2026-11-26)

Round 2 of the stream's must-build list (`docs/axClaude/stream-mode.md` SM-4.11, SM-8.5, SM-7.7).
Heard with NVDA on 2026-11-26 (94e87522): `57-stream-tool-output`, `58-stream-resume-choice` and `59-stream-plan`
passed with every expected announcement spoken, among the 19 stream scripts (56 goals). Still owed:

1. By the maintainer's ear: whether a long plan is too long to hear as the list's name.
2. With Claude Code itself: a plan approved with auto mode in a real conversation (the stand-in plays the manual
   approval only), and the list of conversations in a folder with many long sessions.

## Owed: Other in Claude's question, the answer you are on kept, your own words (2026-11-26)

The maintainer's report of 2026-11-26: he chose Other in a question of two answers and heard the first answer named,
and Claude received it. Repaired the same day (`docs/axClaude/SPEC.md` D33, Answers): a question the window shows
anew while the user has not answered keeps the answer he is on, and an answer typed out a keystroke at a time counts
as his doing until its last key has gone (no "Question closed" after his own words). Passing the stand-in window
script `tests/scripts/axClaude/54-own-words.txt` on the runner's desktop, which failed on the morning's build at the
list put back on the first answer. Heard with NVDA on 2026-11-26 (c0b2f2c2): `tests/nvda-scripts/axClaude-own-words.txt`
passed, "Other" spoken at the arrows and after the redraw, "Sent: 3, Other. Your words next", the field by its name,
the words as sent, the first answer never named; the live script 12 passed on a fresh build. Still owed: whether
Claude Code 2.1.332 really draws a question anew while it waits (the redraw the stand-in plays is the one window path
found from Other to the first answer, not yet seen in a recording).

## Owed: axClaude's questions said once, typing in a question's list, a permission question's core (2026-11-26)

Built on the maintainer's asks of 2026-11-26 (`docs/axClaude/SPEC.md` FR-7.4b and D33, Answers) and passing the unit
tests and the stand-in window scripts 35, 40 to 44 and 50 on the runner's desktop. Heard with NVDA on 2026-11-26 in
the full pass (27581744) and the stream pass (94e87522): the scripts of section 12 and of the stream passed with NVDA
speaking "Question" and the focus's reading; the live scripts 12, 48 and 49 passed on a fresh build, the key alone
sent. Still owed, by the maintainer's ear: whether the core makes a permission question's answers too long to move
through, whether the screen's kind ("Create file", "Bash command") reads well beside the stream's tool name, and
whether a reply still being read is cut off by "Question" as before. The live script `two-questions` passed the same
day on the hidden desktop.

## Owed: axTerminal's screen view of a full-screen program, by ear (built 2026-11-25)

Built on the maintainer's decisions of 2026-11-25 (docs/axTerminal/SPEC.md AT-9.9) and passing its unit tests and
the window script `tests/scripts/axTerminal/14-screen-view.txt` on the runner's desktop, where nothing is spoken;
owed a pass with NVDA listening, `docs/axTerminal/nvda-test-plan.md` section 5, with Windows' `edit`:

1. The opening: the text's sound and "Screen of edit" with NVDA's words for a read-only text and the cursor's line, after "Sent", and nothing
   of the menu bar or the scroll bar by itself; how NVDA reads the box characters ("vertical line") on each line,
   kept as drawn by the maintainer's decision, and whether that is bearable in a line read often.
2. Down and Up in keys to edit: NVDA reads the line edit's cursor lands on, not the one it left. The caret follows
   the cursor at the frame's end (the cursor shown again, or 100 ms of quiet); whether NVDA's wait after a caret key
   is long enough for that is heard only with NVDA.
3. A menu's entry and a dialog's button: "New File, Ctrl+N" and "[Save]" heard once, after NVDA's own reading of the
   caret's line and not cut by it, or cutting it (they are not interrupting); whether "[Save]" reads well with its
   brackets.
4. Ctrl+Space: "Reading the screen" and "Keys to edit" heard each time, and F8 saying the same; the menu bar on Alt
   alone, F1's section, and the way back to the field with "Done" after edit ends.
5. What the first build left (AT-9.10, the same evening): in reading the screen, Alt+1 heard as "Screen of edit. The
   terminal and the command field come back when edit leaves the whole screen" and not cut by NVDA; Ctrl+F's notice
   and its match on the page heard as NVDA reads a selection, and the view's reading after the notice closes; edit's
   first key taken at once now that its colour questions are answered, and edit's colours from those answers
   (black, light text) leaving its menu's entry and its dialog's button said as before; a program that ends on the
   whole screen (`[Console]::Write([char]27 + '[?1049h')` in PowerShell): the view opens for the switch and the
   field comes back at the prompt with "Done"; whether that flicker of focus is heard, and how.

## Owed: what the closed items of 2026-11-25 left for the ear (the backlog cleared at the maintainer's word)

Each was built and passed its scripts; its item was closed so that the priorities list holds only work to finish,
and what it still waits on is here, by ear with NVDA:

1. A window that says something as it opens (axMedia with a file it cannot play as its first; axClaude's Ready):
   the words are heard after NVDA's reading of the window and its focus, not cut off (AX-7.1, the hold of
   2026-11-25; item "of two announcements some tens of milliseconds apart").
2. axHelp asked what a sound means, how to do a task in two apps, and what an app can do: the answer comes from the
   pages it carries (items "every document that says what axKit does" and "workflows as one source").
3. axClaude in daily use: a long streaming reply read with NVDA+A from the top; a feedback survey of Claude Code's as
   a question; in an offered command the copy answer, the concern to its end and the breakdown section; a real
   /login, its last screen as a list; whether the reading place ever jumps (the time and the line count); speech
   order at a turn's end and on the way back from being put away (items "the things only the user's ear can check"
   and "speech order breaks").
4. The forms (the item "the new forms by ear"): the field first, then the explanation, as a form opens; whether the
   explanation at every opening is too much for axTask's Action keys and axTerminal's Add a shell; Enter in a
   one-line field with later fields empty; Action keys refilling the fields as the key list moves; whether axPass's
   new entry password should be a form.
5. At a sign-in: axStart, started hidden, leaves the focus where Windows put it, and the first key reaches that
   window (AS-3.4; the item "axStart: whether it takes the focus").
6. A live check of the blocked report (RP-6.4): the maintainer's own install id (Help, Your reports, or the notice's
   Copy install id) put on `KyleKeane/AxKit-blocklist`'s list, a day after the last read or with the reports
   settings' read time cleared, F4 and send: "Report not sent", the install id, Copy install id and Open the block
   list page heard; then the id taken off the list. The sender's side is held by `ReportSenderTests`; no window
   script can reach it, since a build made for the scripts carries no sending token (item "a private repository
   and reports encrypted").
8. Every app's F1 text after its trim (2026-11-25, WF-D-25 built): F1 in each window, the text read through, what a
   key does in context still there, the line "How to do a task" under F1's, the generated "Every key of this window"
   after it; nothing a user needs gone.
7. axTemplate in the maintainer's own use: a template put in from the launcher in Word, a browser and a terminal,
   where the caret was (docs/axTemplate/SPEC.md; the item "axTemplate: templates as an app of their own", closed
   2026-11-25 when the template in a form's field passed with NVDA).

## Owed: the maintainer's choices of 2026-11-24, by ear

Built and passing on the runner's desktop, which plays no sound:

1. axStart's hotkey three times, Escape between: the entering sound with every showing, not only the first; the
   same in axClip and axHelp.
2. axTask, an item's form, a field changed, Ctrl+Enter: "Saved", then the item where the focus lands, read once
   with what changed.

## Owed: the Store's copy after its install and after its update (2026-11-24, bundle AX-13.4a, AX-13.4c and AX-3.1; `KyleKeane/AxKit-reports issues 11, 13 and 15`)

Built from the reports and by reasoning; the Store's install cannot be made on the runner's desktop. On a computer
where axKit was never installed, after installing it from the Store, by ear with NVDA:

1. Open axStart from the Start menu: NVDA reads its list at once, the first time.
2. Ctrl+Shift+Comma brings up axClip and Ctrl+Shift+Slash axHelp, with no other app opened first.
3. After the Store installs the next release: Check for updates says the version and does not say GitHub could not
   be reached; axStart's hotkey works without opening another app first.

## Owed: the state from the event core, 2026-11-17 (`docs/axClaude/events-and-grounding.md` EG-6.1 to EG-6.4, WP-A16)

Built and passing: every test, the stand-in script 34, and all 36 scripts of axClaude against Claude Code 2.1.326 on
the runner's desktop. Whether Claude works now comes from the marks Claude Code writes at a turn's start and end,
no longer from the spinner on the screen. None of it is heard in the user's own use yet.

By the user's ear, in axClaude over a day of ordinary use:

1. F8, or the status bar, while Claude works through several tool calls: it says working for the whole turn, and no
   longer goes ready and back in the middle (the old state did so 167 times in 59 turns of 2026-11-16).
2. The working music, when it is on: it plays for the whole turn without stopping between tool calls.
3. A message sent while Claude works: "Queued", as before. A message sent in the first moment after an interrupt
   (Shift+Escape), within about half a second: also "Queued" now, and it goes after the interrupted reply's last rows.
   Whether that word is right there, or "Sent" would serve better.
4. Options, Developer, Say the event sources: "Core: in charge of state and Done".

Done from the event core, the same day (EG-5.6, EG-6.5, WP-A17), built and passing the same checks, script 35 and
the 37 scripts of axClaude against Claude Code 2.1.326. By the user's ear, in the same day of use:

5. "Done" and its chime come after the last words of the reply, never before them or in the middle of a reply
   (the old rule said Done early in 23 of 59 turns of 2026-11-16).
6. After an interrupt (Shift+Escape) there is no "Done".
7. A question of Claude's at a bell is heard as the question, and "Done" comes when the answer has ended the turn.
8. A feedback survey at a turn's end: "Done" used to come before it by rule; now it comes at the core's moment,
   usually before. Whether the order is still right when one shows.
9. axHelp's conversation put away and brought back after a turn ended meanwhile: one "Done" on the way back.

A turn Claude Code begins by itself, the same day (`docs/axClaude/SPEC.md` FR-7.3b, EG-4.6), built and passing every
test and, on the runner's desktop, the stand-in script 36 with scripts 34 and 35 beside it, each alone; axClaude's
whole folder of scripts was not run for it. By the user's ear, in the same use:

10. The line "Claude goes on by itself" when work in the background wakes Claude (a shell or an agent ends and
    Claude Code starts a turn nobody sent): whether the words are right, whether it comes at the right moment (after
    the music starts, before the reply) and never in a turn he sent himself, and whether its switch under Options,
    Announcements is where he would look for it.

With NVDA, in a pass: `nvda-test-plan.md` section 25, steps 4, 5 and 6. Heard in the pass of 2026-11-18
(`axClaude/34`, `35` and `36`, `main` 4830f1f0); what is left above is for the user's ear.

## Owed: the keys and sounds of 2026-11-16 (Alt+1 to 3, Ctrl+Period, Ctrl+Slash, navigation mode's three sounds; `docs/axKit/SPEC.md` AX-8.13 to AX-8.15)

Built and passing on the runner's desktop, which posts a key's message and plays no sound, so none of this is heard
or pressed for real yet.

On the real keyboard, by the user:

1. Alt+1, Alt+2 and Alt+3 in axClaude, axTerminal, axGIT and axProcess: the place or view changes, no menu opens,
   nothing beeps, and the menu bar stays closed when Alt is let go. Ctrl+1 to 3 still do the same.
2. Ctrl+Period in a list (axFile, axTask), in an editor's text (axDown) and in a notice's field: the Context menu
   opens once both keys are up, as with Shift+F10.
3. Ctrl+Slash in any window: the question of "Ask about this window" opens, as with Shift+F1.

By the user's ear, in axDown's navigation mode:

4. A letter that jumps (H with a next heading): the high glassy tick, then the move and the words. Chosen by ear on
   2026-11-16 from a script; owed in the app itself: that it is over before NVDA speaks, and that landing on a
   heading the heading sound follows at once and cuts it.
5. A letter with nothing of its kind (H on the last heading): the low buzz, then "No next heading". Chosen by ear
   the same day; owed in the app.
6. A plain key with no command (a letter that is no jump, Space, a digit): the wooden bonk. Chosen by ear the same
   day; owed in the app. Enter, Backspace and Delete keep the edge sound.
7. In axClaude, with Options, When Claude is done, Louder alert when the window is in the background switched on:
   go to another program and have Claude offer a command (ask it to offer one through axKit). The alarm of five
   falling swoops plays and the taskbar button flashes; before 2026-11-16 nothing was heard. The rule is tested in
   code (`AreaChimeTests`); a window behind another cannot be made on the runner's desktop.
With NVDA, in a pass:

7. The sentences that name the new keys: "Changes view first: Alt+1" and its siblings in axGIT and axProcess; the
   key test's "Alt+2: Conversation", "Ctrl+Period: The Context menu, as Shift+F10" and "Ctrl+Slash: Ask Claude about
   this window, as Shift+F1"; the menus' shortcut read as "Alt+1."; the F1 texts' new lines. Heard in the pass of
   2026-11-18 as far as the scripts expect them: "Changes view first: Alt+1" (`axGIT/01`) and the three sentences
   of the key test (`axClaude/23`, `axTask/09`). A menu's shortcut as NVDA reads it and the F1 texts' new lines are
   in no script, and stay for the ear.
8. axHelp's row "Ask about this window" in the command inventory lacks its further key Ctrl+Slash until a pass
   writes it: its only scripts start the real Claude Code. Written on 2026-11-18 by
   `.\nvda-pass.ps1 axHelp -Only '01-start.txt' -Inventory write`, the way for it from now on.

## Owed: a menu's refusal in navigation mode and the last full stop of joined words (2026-11-16, `docs/axKit/SPEC.md` AX-8.13 and AX-7.3)

Two changes that are heard, made with the repair of what axDown answered an AI tool differently than a key; the
scripts of axDown, axJSON, axPreview, axTerminal, axTask and the AI tools pass on the runner's desktop. With NVDA:

1. In axDown, in navigation mode, choose Edit, Undo from the menu: the edge sound, then "Navigation mode: press
   Ctrl+Space to edit". Before, the words came without the sound. The words were heard in the pass of 2026-11-18
   (`axDown/16`); the script does not ask for the sound, which stays for the ear.
2. Any announcement that joins several pieces, such as the words after an AI tool ran a command: the last piece keeps
   its full stop, so the sentence ends as it does when said alone. Listen for a changed pause at the end.

## Owed: the AI tools run a window's command (the branch `push2/mcp-run`, 2026-11-11, `docs/axKit/mcp.md` MC-7)

New steps, none heard yet; `tests/scripts/mcp/02-run-commands.txt` and `03-run-asks-first.txt` drive them on the
runner's desktop and pass. With NVDA, in axDown and axFile:

1. axKit menu (Alt+A), AI tools (I): "Let AI tools run commands" (U) read with its checked state; Enter on it says "AI
   tools may run commands: each is said as it runs, and one that acts outside its window asks you first", and again
   "AI tools may no longer run commands".
2. With it off, a run is refused and the window says nothing.
3. A run of Word wrap in axDown: "axAutomate ran Word wrap. Word wrap off" as one announcement, nothing cut off; a run
   of Heading in Navigate, Next: "axAutomate ran Heading. Second heading, heading level 2".
4. A run of Find...: the Find notice opens with the focus in "Text to find", then "axAutomate ran Find"; a run while it
   shows is refused, and the window says nothing more.
5. In axFile, a run of New folder (marked act): the notice "axAutomate asks to run New folder" with its chime, the text
   read, Enter at once does nothing for a moment; Tab to Run, Enter: "axAutomate ran New folder" with the folder's
   name field focused; asked again, Escape says "New folder not run", and a third run within the minute asks nothing.
6. A real Claude Code session, paired, asked to run a command of an axDown window: "Claude ran ..." heard (the user's,
   by ear: the runner cannot be Claude Code).
7. (After the review of 2026-11-11.) In axFile with a file selected, a run of Open asks first ("axAutomate asks to run
   Open"); Escape says "Open not run" and nothing opens. With the window in front, typing in it and a run within 3
   seconds: refused, nothing moves. A run in an axDown window behind another: the notice chime and the taskbar flash,
   no speech. Turning AI tools off says "... and Let AI tools run commands is off too", and on again the switch reads
   not checked; Forget AI tools says the switch is off.
8. (After the second review of 2026-11-11.) In axFile on notes.md, a run of Open: the question's text reads "axAutomate
   asks to run Open (Operations) on notes.md in files in this window"; with two files selected it names both. In axProcess,
   a run of Copy line asks first. Turning AI tools on with Let AI tools run commands already on: the notice says it is
   on; with it off, that it is off. Three runs within 5 seconds in an axDown window behind another: one chime and
   one flash, not three.

## Owed: the twelve decided changes (the branch `improvements`, 2026-11-09; the inventory of that day is in the git history)

The branch's twelve commits each change something heard, and none has been heard yet. Once, with NVDA: axFile's New
folder with `a?b` as the name (the notice stays, the reason spoken; `tests/scripts/axFile/03-operations.txt` 3.1 has
it); Show current folder in on a folder deleted meanwhile ("Folder missing", nothing starts) and an editor's Show
current file in on a file gone ("File missing"); F1 in axCalc, axClip, axProcess, axTask and axPass (the Notices
section at the end, two lines) and in axClaude (after Claude Code itself); Ctrl+O then Escape in axDown and Ctrl+O in
axGIT (the focus back where it was); axTask and axPass Ctrl+Tab, Ctrl+Shift+Tab and F6 between the panes
(`tests/scripts/axTask/01-navigate-and-fields.txt` 1.5 presses them); axTerminal's Command Prompt (`.\name` for a
program in the current folder); axTerminal's closing question after an update (Keep running: "Update installed. It
starts with the next axTerminal"); axTask Ctrl+N in a GitHub list (the item starts as unlabelled and open); axPreview
on a zip of more than 1,000 entries ("and N more" as the last line, "Shown: the first 1,000 entries" in the details).
The one search for Claude Code and cmd's source change nothing heard.

## Owed: the reports, what the scripts leave (2026-11-08, `docs/axKit/reports.md`)

The pass of 2026-11-08 below covered the two scripts. Still to hear: a report sent from a standalone app (axDown's F4),
"Your reports" opening axTask on the list, the cannot-send notice of a build without the token with its Copy report and
Open GitHub's form, and the GitHub actions of axTask with the user's own token (Make it a bug, Close, Comment, Block).

## Owed: axClaude, `/clear` empties the window (2026-11-07, `docs/axClaude/SPEC.md` FR-8.8)

Section 2 step 9 of `docs/axClaude/nvda-test-plan.md`, new with the change: "Sent" then "Conversation cleared" spoken, the
one System line left, the session name gone from the status bar, the next block "# Input 1" with no blank line above
it. Built against the `clear` fixture (Claude Code 2.1.319); to be run through `nvda-pass.ps1 axclaude` once a script
for it exists, and heard by the user in a real session. The same day, section 12 step 4b: one question under
2.1.319's `Question: Colour` header, named by the question alone and not by the reply's last line (the `one-question`
fixture); the user heard the fault in a real session, the fix is to be heard the same way.

## Owed: access layers and consent (the branch `licence`, 2026-10-29, `docs/axKit/SPEC.md` AX-11)

Scripted since 2026-10-31 and heard through `nvda-pass.ps1` (`docs/axKit/automation.md` section 3; passed 2026-11-01,
the entry below): the access layers'
names in every list (the axKit menu and Show in steps of the apps' scripts); the reminders of axClaude
(`axclaude/15-consent.txt`), axTerminal (`axterminal/07-consent.txt`: the first shell's, then Command Prompt's from a
new session, each shell its own consent), axGIT (`axgit/06-consent.txt`, with About,
Licence ending in what was agreed to) and axPass (`axpass/02-consent.txt`), each with the title, Tab to the box, Enter
before checking saying "Check the box first", Space checking, Enter connecting, and the second start asking nothing;
and the AI tools' pairing notice with its box (`mcp/01-read-and-pair.txt`).

Still by ear, before the branch merges:

- Install it now in each app: "Before <app> installs <program>" with Install, then the installer (needs a computer without the program).
- The update notice: the terms sentence first, and the buttons "I accept all terms, install and restart" and "… install and close" (axClaude plan steps 138 to 140, axDown's update steps; needs a release newer than the build).
- axClaude: /login says "Sent. You are signing in to Anthropic …"; axGIT: the first fetch to github.com asks the server's reminder, and a later window says "Connecting to github.com; …" with the result still heard (needs a remote on the network).
