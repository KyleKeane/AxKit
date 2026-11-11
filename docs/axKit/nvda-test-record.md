# NVDA test record

What was checked with NVDA, against which version, and with what result. When a bug comes up, this says when its step last passed, and so which changes since then to look at. The plans are each app's `docs/<app>/nvda-test-plan.md`; the files they use are made by `manual_tests/prepare.ps1` (`manual_tests/_main.md`).

## How to add a pass

Add a section under this one, above the earlier passes, so that the newest comes first: the date, the version, the commit tested, who ran it and how the result was given, and the environment: the NVDA version, the synthesizer and the Windows build (so that a regression is dated to an NVDA release rather than to some time). Under it, one table per app with a line per section of its plan: the steps run and the result, Passed, Failed (with what was heard) or Not run. A step that fails gets a line of its own with what NVDA said and what it should have said, and the bug's fix names the step. When a plan gains or loses steps, the next pass lists them as the plan has them then.

## 2026-11-10: every app for 5.7.0, by the assistant through nvda-pass.ps1 (branch `conversation`, e917645)

Run by the assistant with NVDA 2025.3.3 listening after the user stepped away at his word, Claude Code 2.1.322 and
`--model haiku`, Windows 11 Pro 10.0.26200, a development build of the branch `conversation` at e917645, the state
released as 5.7.0: the bundle's menus at the end of every bar, Window, Options, axKit (Claude, AI tools, Theme) and
Help with the command palette first (AX-9.1); every first menu Session and every second Operations; axHelp rebuilt as
two hotkeys into one conversation in axClaude, with axDescribe merged in (`docs/axHelp/SPEC.md`); axClaude's `--title`,
`--model` and "message" hand-over. `nvda-pass.ps1` over every app, 82 scripts (axDescribe's two gone, axHelp's rewritten):
all 82 passed whole in the one run, 2246 s, every announcement also spoken by NVDA, which covers: the F1 line of the
bundle's menus in every window (`check commands`, `KeyTests`), the palette's paths under Session and Operations
(`axDown/06`, `axFile/05`, `axGIT/02`, the editors), the axKit menu's Claude switch and folders (`axDown/11`), and
axHelp's window, the conversation brought up from it on Haiku, titled axHelp, with its working folder's CLAUDE.md and
agent written, and a picture start handed into the conversation's message field (`axHelp/01`). The user's own axHelp,
axStart and axClip kept running from their older build throughout.

Not heard by this pass and the user's by ear: the menus with Alt and the arrows (menu mode needs the foreground the
runner does not take), the hotkeys Windows+Alt+H and Windows+Alt+S from another program, a real screenshot described,
a real conversation with its agent at work, and Options, Conversation model across a restart.

## 2026-11-09, late evening: every app for 5.6.0, by the assistant through nvda-pass.ps1 (`main` a9de848)

Run by the assistant with NVDA 2025.3.3 listening after the user stepped away at his word, Claude Code 2.1.322 and
`--model haiku`, Windows 11 Pro 10.0.26200, a development build of `main` at a9de848, the state released as 5.6.0:
every name a user hears in the app's own case (AX-5.1), axDescribe's picture file (DE-2.7) with the test pictures
(DE-8), axHelp's and axDescribe's silent typing and Tab into the list (HE-3.3, HE-3.4), Claude's working folder
(AH-2.1, AH-D6), the axKit menu's new order with the Claude submenu and without Open windows by type (AX-9.1), the
Claude apps as the last group of every list (AX-9.2), and the reminder opened at once by any ask while Claude is off
(AH-1.3). `nvda-pass.ps1` over every app, 84 scripts (the 83 of the evening's pass plus `axDescribe/02-picture-file.txt`):
all 84 passed whole in the one run, 2264 s, every announcement also spoken by NVDA, which covers for this release:
the axKit menu line of every window's F1 text (`check commands` in every script; the whole line in `KeyTests`), About,
Version with the access layers in their new order (`axDown/06`, `axJSON/04`), Show current folder in with axClaude
last (`axFile/02`), the reminder at once from Shift+F1 with Claude off and no agreement, Escape as "Claude stays off",
Space on the box and Turn on as "Claude on" going straight on to the question field, the switch "Use Claude in axKit"
as "Claude off" and "Claude on", the folders notice naming Claude's working folder (`axDown/11`), axHelp's Tab into
the list and Shift+Tab back, Enter in the list showing the answer (`axHelp/01`), axDescribe's start on a picture file
described at once with no picture of the screen taken, the description kept under the file's name, Tab into the list
(`axDescribe/02`), and axDescribe's 01 on the user's desktop, where the picture is taken (the script's second path).
The user's own axHelp, axDescribe and axStart kept running from their older build throughout: a test run's app holds a
mutex of its own (`AppPaths.OneAtATime`) and a second start finds only a window of its own program file
(`HotkeyForm.FindRunning`, `ThisProgram.Runs`), so the environment failure of the evening's `axstart/01` is gone.

Not heard by this pass and the user's by ear: a real description of a test picture (`docs/axDescribe/nvda-test-plan.md`
section 4, `tests/fixtures/pictures`), Ctrl+D's Windows file picker (a separate window the automation does not drive),
a real question from axHelp with Claude Code starting in `Documents\axKit Claude`, how NVDA reads the focused list of
earlier questions on Down (the automation hears the window, not NVDA's own reading of a list box), and the menus of
the other apps, untouched by this release.

## 2026-11-09, the evening: every app for 5.5.0, by the assistant through nvda-pass.ps1 (`main` 34d3db5)

Run by the assistant with NVDA 2025.3.3 listening after the user stepped away at his word, Claude Code 2.1.322 and
`--model haiku`, Windows 11 Pro 10.0.26200, a development build of `main` at 34d3db5, the state released as 5.5.0:
the shared edit control of every window (`AxKitTextBox`, AX-8.9) and word wrap in the theme file (theme.md TH-6), the
Azure signing retired, and AI help with axHelp and axDescribe (`ai-help.md`). `nvda-pass.ps1` over every app, 83
scripts (the 80 of the full passes before plus `axdown/11-ask-claude.txt`, `axhelp/01-start.txt` and
`axdescribe/01-start.txt`, each against the fake Claude of its script): 80 passed whole in the one run, every
announcement also spoken by NVDA, which covers for this release: every field and text of every window typed into and
read (the shared control under them all), the page and paragraph keys of the editors and the console fields, word
wrap off and on in axDown and axPreview with the caret kept and the value written into the run's theme file
(`axdown/02`, `06`, `10`, `axpreview/01`), the Shift+F1 line and the AI help entry of the axKit menu line in every
window's F1 text (`check commands` in every script, the menu line in `KeyTests`), About, Version naming axHelp and
axDescribe, and the AI help flows: off said, the switch, Shift+F1's question notice, the "Asking Claude" wait, the
answer with the focus on its text, Tab to "Ask more about this answer", the second answer, the folders notice, axHelp's
list of earlier questions with the match keys and the edge, axDescribe's kept description reopened, its follow-up, its
list. Three did not pass in the run:

- `axdescribe/01-start.txt` step 16: the script expected the hidden desktop's "The picture could not be taken" where
  the user's desktop has a screen, so the picture was taken (of the axClaude window in front) and the fake described
  it at once. The script now takes either path (`try said`, then `try state Claude says`) and passed on its rerun.
- `axclaude/15-consent.txt` step 26: the window said "Ready" and NVDA, still reading the new window's title and field,
  did not speak it within 15 s; a timing of the NVDA tier, seen before (2026-11-01). Passed whole on its rerun.
- `axstart/01-basics.txt`: the user's own axStart runs from another program file, so the test's start found its window
  and handed nothing over; the known environment failure of a bare runner, recorded in the priorities list. axStart's
  shared pieces (the field, the list, the hotkey window) were heard through axClip, axHelp and axDescribe, which
  passed.

The two reruns took 45 s, the user away again at his word. Not heard by this pass and the user's by ear: a real
question to Claude through Shift+F1 and axHelp (`docs/axHelp/nvda-test-plan.md`, the steps marked real), a real
description of a window through axDescribe (`docs/axDescribe/nvda-test-plan.md` section 1), the hotkeys
Windows+Alt+H and Windows+Alt+S from another program (the scripts use keys nobody holds and `launch`), how a long
answer reads in the notice, and the Say all hold of the earlier entry.

## 2026-11-09, the night run: every app, by the assistant through nvda-pass.ps1 (branch `improvements`, a140ca9 and after)

Run by the assistant with NVDA 2025.3.3 listening while the user slept, having cleared the night's work, with Claude
Code 2.1.319 and `--model haiku`, Windows 11 Pro 10.0.26200, a development build of the branch `improvements` at
a140ca9 (the consolidation run of 2026-11-09 and the twelve changes of its inventory's section 11). `nvda-pass.ps1`
over every app, 75 scripts: 72 passed whole in the one run, every announcement also spoken by NVDA, which covers the
sections of `inventory-2026-11-09.md` section 12 as the apps' scripts have them (the titles, the yes-or-cancel
questions, the one-field notices, the one-checked menus, the lists filled anew, axStart's and axClip's options,
axClaude's silent screen notice, the find keys of the list windows); and the passes owed before it: axStart, axCalc
and axClip (merged before their pass), the reports (`axclaude/16-report.txt`: with the cannot-send notice of a build
made without the sending token; the consent notice and the sending itself were heard by the user on 2026-11-08) and
axClaude's `/clear` and one-question header in its scripts. The three that failed passed on their reruns the same hour:

- `axclaude/12-questions.txt` step 139, `/tasks` with nothing running: Claude Code 2.1.319 shows it as a question
  list, which does not take the focus from the field after a typed command, so the step (9c03533, then its rerun) takes
  either the old information screen or the list, closing the list with Shift+Escape, and passed whole (289 s).
- `axclaude/16-report.txt` step 49 expected the consent notice; the pass's build has no sending token and showed
  "This build cannot send reports" (RP-6.5), which the step now takes as well; passed whole.
- `axstart/01-basics.txt`: `start start` found the user's own axStart running (its hotkey holds the window's name), so
  the run ended that process for the script and started it again from the same program file after; passed whole
  (22 s).

### The second full pass, from `main` after the merges (be88c50)

`nvda-pass.ps1` again from `main` once the branches `improvements` and `axMCP` were merged there (13:12 to 13:50), 79
scripts: the 75 of the first pass with the steps added for the twelve changes, axDown's `09-show-in-missing.txt` and
`10-theme.txt`, axMCP's `01-start.txt` against its fake server, and the NVDA-only `axDown-dialog-focus.txt`. 71 passed
whole, every announcement also spoken by NVDA, among them: axPass's `a?b` and pane keys; File missing from axDown;
the Notices section at the end of F1 in axCalc and axClip and after Claude Code itself in axClaude; a zip of 1,001
entries in axPreview ("and 1 more", "Shown: the first 1,000 entries"); the focus back in axDown's text after Ctrl+O
and Escape; `/tasks` as the list and the token-less report notice in axClaude; the theme items of axDown
(`10-theme.txt` apart from one expectation below); and the whole of axMCP (the servers in sections, the fake started
with its counts said, the tool's arguments as fields, a required argument refused, the call and its result notice,
Ctrl+J in axJSON, the resource read, Stop). Eight failed, and what each was:

- `axdown/03-files.txt` step 49, Ctrl+Shift+N: a fault of the night's New window: it passed the viewer's `--new`
  option to axDown, which refused it ("Unknown option --new"); fixed in the commit after the pass, the option going to
  the viewer alone.
- `axdown/06-menus-and-help.txt` step 67 and `axjson/04-axkit-menu-and-about.txt` step 25: the About text now ends its
  access layers with "axMCP for MCP servers"; the scripts expected the old sentence.
- `axdown/10-theme.txt` step 14: the theme file's first line is its brace; the step expected the format name there.
- `axpass/01-unlock-read-change.txt` step 63: Ctrl+Tab brought the pane back on the field it had (URL), as the item
  apps keep it; the step expected the first field.
- `axtask/04-action-keys.txt` step 93: a new issue item's fields start with Title and Number before Kind; the step was
  one Down short. The item did start as unlabelled and open, as the change says.
- `axterminal/03-shells-and-folders.txt` step 61: the planted `hello.cmd` was written beside the session's folder, not
  in it; the bare name was refused as the change says, `.\hello` found nothing.
- `axstart/01-basics.txt`: the user's own axStart was running again (started again after its first rerun), so the
  script's start found another program's window; the script passed whole on its rerun an hour before on the same
  code.

The seven scripts were rerun with the fixes. The first rerun taught one thing about the runner: the failed Ctrl+Shift+N
had left the refused axDown process alive with its notice in front, outside the run that started it, and the four
scripts that followed could not bring their windows to the front ("no window of the run is in front"); the leftover
ended by hand, the reruns went on (an item for the runner is in the priorities list). The reruns (13:55 to 14:20): with
the fault fixed (e54dd65) and the expectations put right, `axjson/04`, `axpass/01`, `axtask/04`, `axterminal/03` and all
fourteen axDown scripts passed whole, every announcement spoken by NVDA, two axDown scripts needing a second look at
what the New window item moved: `use 4` for the window the files script comes back to, and the palette's second entry.
So every script of the bundle passed on `main` at the release of 5.3.0, axStart's on its rerun of the same hour.

The twelve changes themselves are heard by steps added in 9c03533 (axPass's pane keys, the Notices section at the end
of F1 in axCalc, axClip and axClaude, `.\hello` in Command Prompt, a zip of 1,001 entries, Ctrl+N in a GitHub list,
File missing in axDown, the focus after a Windows dialog as an NVDA-only script) and by axFile's `a?b` steps, which
the full run already had; those steps ran in the second full pass below, with the theme (`docs/axKit/theme.md`) and
File, New window in the editors (`axdown/10-theme.txt`, `axdown/03-files.txt`).
## 2026-11-09, later still: the Say all hold of the transcript views, by the assistant (branch `say-all`, unreleased)

Run after 5.4.0 with NVDA 2025.3.3 listening, the user still away: the new NVDA-only script
`tests/nvda-scripts/axClaude-say-all.txt` against the branch's build (axClaude FR-3.3: output held for 4 s after a caret
move that neither a key nor the view made, NVDA's Say all moving the caret as it reads). A first reply of forty lines,
then a second of 120 lines asked for, and NVDA+A (Say all in the laptop layout, which this computer uses) from the top of
the conversation 1.5 s after Enter. The script passed: NVDA read the banner, the first input and, after the second
turn's Responding and Done, the first reply's lines on; every one of the 120 lines was in the text at the end, no error
in the log. What the capture cannot settle is whether the reading went on uncut through the streaming itself: for 20 s
after the first input NVDA's log shows nothing spoken, then the window's Responding and Done (which interrupt speech,
as they always have) and the reading again. So the change is merged for the user to hear by ear, unreleased: start a
long reply, NVDA+A from the top, and say whether the reading now holds (before, it cut off with "Could not clone
range"); the window's own Responding and Done still interrupt it, which is a separate question.
## 2026-11-09, later: axPreview's picture reader, by the assistant through nvda-pass.ps1 (branch `ocr`, 95092f0)

Run after the release of 5.3.0 with NVDA 2025.3.3 listening, the user still away: `nvda-pass.ps1 axPreview` from the
branch `ocr`, its five scripts, with the picture steps new in `04-details-and-kinds.txt` (docs/axPreview/SPEC.md
AP-7.2 item 4): a picture drawn by the script opened, NVDA reading "text.png is a picture (PNG). Reading its text with
Windows' own reader..." and then "Text read: 2 lines", the text's first line naming the picture, Ctrl+Down to "Hello
from axKit" and Down to "Second line 2026", "Text lines: 2" in the Details list; then Ctrl+Shift+V with the same
picture on the clipboard, "Text read: 2 lines" again and the title naming the saved `clipboard-<date>-<time>.png`.
Four scripts passed in the one run; the fourth's first attempt expected the first page's text by focus, which under
NVDA the finished page had replaced by the time the start had been read (the step dropped: the first page is the unit
tests' to check, `PictureTextTests`), and it passed whole on its rerun (34 s). Released in 5.4.0.
## 2026-11-08: the reports, by the assistant through nvda-pass.ps1 (branch `reports`, ec166c0)

Run by the assistant with NVDA 2025.3.3 listening, the user away at its question, with Claude Code 2.1.319 and
`--model haiku`, Windows 11 Pro 10.0.26200: `nvda-pass.ps1 axClaude` (18 scripts) and `nvda-pass.ps1 axTask` (4). The
two scripts of this branch passed whole, every announcement also heard from NVDA:

- `axclaude/16-report.txt` (plan section 13 step 0): F4 and "Report to axKit", the words field named as an instruction
  and focused, Enter on it empty refused with the edge sound and "Type a few words first", the fields in Tab order
  (Kind with Feedback read by NVDA alone, App and version naming axClaude, the actions, Where the focus was naming the
  message field, the log lines, Title holding the first line typed, Send, Cancel), Enter saving and asking the
  agreement, Not now leaving it unsent, Escape cancelling with nothing typed and keeping a draft with words, the two
  menu items by their names. The sending itself, "Sent as issue N", was heard by the user in a real session the same
  day (issues 5 and 6 of the reports repository).
- `axtask/04-action-keys.txt` (plan section 5): Ctrl+1 outside a list and Ctrl+3 without an action refused with the
  edge sound and their sentences, Ctrl+0 to All tasks, Ctrl+1 setting Done and Ctrl+Z taking it back, Ctrl+2 moving
  the item to Later and back, the Item menu's Action keys naming the list's actions, List → Action keys making Ctrl+4
  as "Finish" through its three notices, the token notice's protected field, New GitHub list refusing a bare name and
  making "AxKit-reports issues", which read its six issues from GitHub as it opened, Open in axClaude asking for the
  folder.

Three other scripts failed, none from this branch:

- `axclaude/04-headings.txt` step 303, Ctrl+Alt+H: Windows Magnifier was running again and keeps the key (the entry of
  2026-11-01 below); to rerun with Magnifier closed.
- `axclaude/12-questions.txt` step 139, `/tasks` with nothing running: Claude Code 2.1.319 showed it as a question
  (the trill "Question", "Claude asks"), no longer as the information screen 2.1.309 to 2.1.316 showed (FR-2.11). To
  record with the record-fixture skill and settle on `main`: a change of Claude Code's, not of this branch.
- `axtask/01-navigate-and-fields.txt` step 104: the letters "Hig" of "High, Low" typed into the new field's options
  were lost between the Type list and the options field ("h, Low" reached the list), a timing of the NVDA tier's
  typing after Enter; passed on 2026-11-01. To rerun; a `wait` after that Enter if it repeats.

Still the user's by ear: a report sent from a standalone app (axDown), and "Your reports" opening axTask on the list.

## 2026-11-01, later: axClaude, by the assistant through nvda-pass.ps1 (branch `licence`, 1339f6f to 6cedc57)

Run by the assistant with NVDA listening, as below, with Claude Code 2.1.316 and `--model haiku`: the 17 scripts of
`tests/scripts/axClaude`, the reminder of AX-11.4 among them (15-consent). All 17 passed: 14 in the one run, three on
their reruns after what they found was fixed:

- 12-questions: the runner's own `expect said !` (any interrupting announcement) had nothing for NVDA to be heard
  with; the runner now takes "!" alone as that.
- 05-modes: a Ctrl+C sent in the first moment of a turn did not hold on Claude Code 2.1.316, which went on by itself a
  few seconds later, while one sent once Claude is responding does (the probe confirmed Ctrl+Shift+C reaches Claude
  and makes it ready); the script now interrupts once "Responding" has been said. Claude Code also does not ask before
  a Bash command on every run; the scripts answer the question when it comes.
- 04-headings: Ctrl+Alt+H never reached the window, with Ctrl+Alt+P fine. Not NVDA (its input help named the key with
  no command of its own) and not axKit: Windows Magnifier was running, and it keeps Ctrl+Alt+H and K for its reading
  and Ctrl+Alt+L and M for its views, system-wide. With Magnifier closed the script passed; the user guide says so, and
  the plan's old "Ctrl+Alt+L and Ctrl+Alt+M: nothing" had the same cause. A deleted NVDA plugin of the user's own,
  which bound Ctrl+Alt+H as well, was the first thing found and removed.

Still the user's by ear for axClaude: /login's line (AX-11.6), the update notice's terms (needs a release newer than
the build), and the steps the scripts leave manual (the Start menu, Copy diagnostics on the clipboard, /model).

## 2026-11-01: every app but axClaude, by the assistant through nvda-pass.ps1 (branch `licence`, 13f4209 and e590b5d)

Run by the assistant with NVDA listening (`docs/axKit/automation.md` section 3), the user away at its question: every
announcement, focus move and sound a script expects was also heard from NVDA, through the capture add-on. NVDA with
the oneCore synthesizer (its version not readable from the protected process), Windows 11 Pro 10.0.26200. 56 scripts,
every app's `tests/scripts` folder but axClaude's and the NVDA-only script of `tests/nvda-scripts`: all 56 passed, 55
in the one run and axGIT's 04 on its rerun after a fix to the runner's matching (two neighbours in either order).
axClaude's 17 scripts are still to run with NVDA; they need the network and about 45 minutes.

| App | Scripts | Result |
|---|---|---|
| axCalc | 01 | Passed |
| axClip | 01 | Passed |
| axDown | ten scripts, 01 to 08 | Passed |
| axFile | seven scripts, 01 to 07 | Passed |
| axGIT | 01 to 06, the reminder among them | Passed (04 on its rerun) |
| axJSON | 01 to 04 | Passed |
| axMedia | five scripts, 01 to 04 | Passed |
| axPass | 01 and 02, the reminder | Passed |
| axPreview | five scripts, 01 to 04 | Passed |
| axProcess | 01 and 02 | Passed |
| axStart | 01 | Passed |
| axTask | 01 to 03 | Passed |
| axTerminal | seven scripts, 01 to 07, the reminders among them | Passed |
| The AI tools, with the pairing reminder | mcp 01 | Passed |
| NVDA-only | axDown-notices | Passed |

What the pass found and what was done, in the order found; the hidden-desktop tier sees none of these:

- axTerminal: "Done" after a command's last line and "Ready" after a shell's banner were never spoken, since NVDA
  drops a notification that reaches it while the one before still waits its turn. The word is now said with the
  frame's speech as one announcement (AT-5.1), and the bundle says two non-interrupting announcements within a third
  of a second as one (AX-7.3).
- axMedia: "No sound or video files in …" and "Cannot play …" as the window opened were never spoken, since NVDA
  ignores a window that is not in front, and a window started by a program in the background comes up behind. What a
  window says before its first activation now waits half a second past it (AX-7.3).
- axClaude: Claude Code 2.1.316 draws several questions in one call under a "Questions:" row; the first was named
  "Questions: Colour, Fruit. Which colour do you prefer?" and nothing said "Next question" (found by the same pass's
  hidden-desktop run; fixed with the `several-questions-claude2.1.316` fixture).
- axClip: one of three copies made by clip.exe in quick succession was missed once; a copy with no text yet is now
  read again (CP-3.3).
- The runner: the navigation keys went as the numeric keypad's, which NVDA keeps for its review cursor; keys reached
  the user's own window between scripts; a window behind another's, or not yet activated, said things NVDA never
  spoke; a key sent while NVDA was still at the one before was lost; two neighbours come in either order. All in
  `docs/axKit/automation.md` section 3.

Not heard by this pass and still the user's by ear: the Owed section above, sound and video quality, the Start menu
and Explorer, and how a change feels to use.

## 2026-10-31: the consent box's state, by the assistant (branch `licence`, after c634215)

Run by the assistant through the NVDA tier (a script since folded into `tests/scripts/axTerminal/07-consent.txt`, `docs/axKit/automation.md`
section 3), the user away at its question; NVDA's speech read from the capture add-on. The user had reported that
Space in the box said nothing.

| Step | Result |
|---|---|
| axTerminal's first start: the reminder's title and text spoken | Passed |
| Tab: "I understand that … check box not checked" | Passed |
| Space: "checked", once | Passed |
| Space again: "not checked", once | Passed |

## Owed: the twelve decided changes (the branch `improvements`, 2026-11-09, `docs/axKit/inventory-2026-11-09.md` section 11)

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
## 2026-10-25: every owed pass (main acae61f, released as 3.7.4)

Run by the user from the checkout on the branch `inventory` (acae61f), the result given as "all nvda tests are complete"; NVDA
version, synthesizer and Windows build not given. Every section that was owed, as it stood, passed; each keeps its steps
below as the record of what was checked.

### the inventory and the review of 3.7.x (the branch `inventory`, 2026-10-25): Passed

Automated: every app's scripts pass on the branch. By ear:

- Every window: Ctrl+V into axClaude's message field, axDown's text, axTerminal's command field, a Find or Go to line field says "Pasted" ("Pasted, 3 lines" for several lines into a text of several); with a picture on the clipboard, "Nothing to paste as text"; in axPreview's text, nothing.
- axProcess: a process's size reads "350.4 MB" with one decimal.
- axGIT: Ctrl+F in the list says "Name contains" and finds a line of the list; F3 the next; in the details, the text find as before.
- axTask and axPass: Shift+F10 (and the Applications key) opens the List or Group menu on a list, the Item or Entry menu on an item, the Field menu in the detail pane, and NVDA reads its first item; a locked axPass's menus stay unavailable.
- axPreview: Ctrl+E on an `.exe` or a `.lnk` gives the notice "Not run from axPreview"; on a picture, its program still opens.
- axMedia and axFile: the queue and the Actions list still change one line at a time while something plays or copies, the line you are on kept.
- Page Up, Page Down, Ctrl+Up and Ctrl+Down in axDown, the conversation and the notices, and the heading keys in a notice: as before (their code moved).

### axClaude's side answer as read-only text (the branch `kit-j`, docs/axClaude/SPEC.md FR-2.11): Passed

`/btw` and the other screens that only inform (`/goal`, `/tasks` with nothing running) are a notice in the window, not
a list in the control area; automated in `tests/scripts/axClaude/14-side-answer.txt`. By ear, axClaude's plan section
12, step 22 and the new step 6:

- `/btw <question>` from the message field: the trill, the notice's title and the answer read; Down Arrow reads
  "Enter or Escape: Close."; NVDA+End "Claude: showing a side answer"; Tab: Close, Copy (c), Fork (f).
- Escape and Enter both close it, "Closed", the message field back, NVDA+End "Claude: ready".
- A second `/btw`: Browse (Shift+Left) changes the text with the focus kept on the button.
- From the conversation, the notice comes a second later; with Go to them at once off, nothing is cut off.
- `/tasks` with nothing running: the notice "Background"; with a task, still the list.
- Since 2026-10-25: c in the answer's text copies it, as the Copy button does (paste it somewhere); f forks. A long
  answer that arrives in parts keeps its first lines in the text.

### the command list of every window (the branch `app-kit`, `docs/axKit/app-kit.md` AK-2): Passed

Every window's keys moved from its key switch onto one list of commands, meant to change nothing a key does; the
automated scripts of every app pass. Heard with NVDA, a short round in each app's plan, and these steps above all:

- axFile: F2 starts the rename a moment after the key, as the menu's Rename always did: the box and its text read.
- axClaude: with the menu bar open (Alt), Ctrl+N, Ctrl+Shift+R and the other keys that were menu shortcuts do nothing
  until the menu closes, as every other key of the window already did (AX-6.6); outside the menu they act as before.
- axTask, axPass: in the rename box (F2), Ctrl+Z and Ctrl+C act on the box's text, never on the list.
- axPass: F5 says "Refreshed" and shows an entry added in KeePassXC's own window meanwhile.
- axStart, axClip: the six match keys, Escape hiding, and the window coming back on the hotkey with the field empty.
- axGIT: Enter in each view: the diff in Changes, the switch in Branches, the commit in Log.

### the AI tools, first stage (the branch `app-kit`, `docs/axKit/mcp.md` MC-6): Passed

Automated in `tests/scripts/mcp` (the runner as the client); by ear with NVDA, in any window:

- axKit menu (Alt+A), AI tools (I): "Let AI tools read and open axKit" read with its checked state; turning it on asks
  "Turn on AI tools?", Enter turning it on and adding axKit to Claude Code ("axKit is added to Claude Code");
  turning it off says "AI tools off".
- Then a new Claude Code session, asked to list the axKit windows: its first call brings the
  pairing notice with its chime into the newest axKit window, which flashes when it is not in front; the title names
  the client, the text its program; Enter at once does nothing for a moment, then Deny; Tab to Allow, Enter: "Claude
  Code may read axKit".
- A pairing while another notice is open shows nothing and the client hears "ask again".
- Forget AI tools: "AI tools forgotten"; the next call asks again.

### the installer reads the apps from AxKit.exe (3.7.0, `docs/axKit/app-kit.md` AK-4.1): Passed

- On a computer with axKit installed (not the development machine), update to 3.7.0 or run install.cmd from its zip:
  every app is in the Start menu ("axKit axCalc" and the rest), the console commands start them, File Explorer's
  "Open with axKit" lists the same apps as before on a folder, a file, a .json file and a sound file, and Settings,
  Apps, axKit, Uninstall removes it all.

### Install it now (3.7.0, `docs/axKit/SPEC.md` AX-2.9, after the review of 2026-10-24): Passed

- axGIT without Git, axPass without KeePassXC, axClaude without Claude Code: the notice's last line says "Enter:
  Close" and Enter closes; Tab to Install it now and Enter installs, the progress notice reads, and the app goes on.
- During an install, Escape hides the progress notice; open another notice (F1): the install's end does not close it,
  and a failure is said, not shown over it; Install it now again says it is being installed already.
- axTerminal: Session, Switch shell opens at once and lists only the shells there; after Install PowerShell 7 it
  lists it.

### merged to main on 2026-10-24 before their pass (the branch `calc-clip`): Passed

Merged at the user's word ahead of the rule, so that main holds them; no release until these have passed.

- axCalc, `docs/axCalc/nvda-test-plan.md`: every section (new app; its history became the bundle's list at the merge).
- axClip, `docs/axClip/nvda-test-plan.md`: every section (new app).
- axStart, `docs/axStart/nvda-test-plan.md`: the typing, the keys in the field and the edge sound, hiding and the
  hotkey (its window moved onto `HotkeyForm`, and the search field into it).
- The cleanups of `docs/axKit/inventory-2026-10-24.md`, meant to change nothing a key does: axGIT's and axProcess's
  views (`ListViewState`), the copy keys of axFile, axPass, axProcess, axCalc and axClaude (`CopyText`), the Show
  current folder in submenus of every window (the names now axGIT, axTerminal, axFile), axDown's and axPreview's
  Markdown keys (`MarkdownKeys`), axFile's Go to folder completion and Open with (moved files).
- The app lists and Show in (AX-9.2, AX-9.10): Alt+A, N and Alt+A, B in any window, the four groups in order with the
  separators skipped; axFile's Item, Open in axKit (no utilities); Show current file in from axDown, axPreview and
  axMedia (axClaude now there); Recently closed after closing axCalc and quitting axClip.

### the security fixes of 2026-10-24 (the branch `security`, `security-audit-2026-10-24.md`): Passed

- axGIT: open `manual_tests/axGIT/scratch-repository-with-a-hook` (plan section 1, step 7): the notice names the hook;
  Keep them off, then Trust this repository.
- axPass: Ctrl+C, then Windows+V: the password is not in Windows' clipboard history; after 30 seconds the clipboard is
  empty; Windows+L locks axPass ("Locked" when you come back); Ctrl+U on an entry whose URL is not a web address says
  "Not opened".
- axTerminal: `type` a file holding the shell marks (the plan's new step): no "Done", the command still running.
- axClip: Keep history between runs on, quit, open `axClip.json`: no copied text in it; start again: the history back.
- Every window: Copy diagnostics, paste it into axDown: the account's name reads `<user>`.
- axClaude's `tests/scripts/axClaude/09-options.txt` fails at step 191 on main as on this branch (the second line of the
  conversation after a forced restart is the recording's line): an older break, most likely Claude Code's output, to be
  looked at with a recording.
## 2026-10-24: axPreview as the general viewer (the branch `preview`, before 3.7.2)

Commit 99ba45a, run from the checkout by the user, the result given as "nvda tests pass"; NVDA version, synthesizer and
Windows build not given. axPreview's plan section 5 (the Details list and the kinds of file) and step 1.9, with the
automated 	ests/scripts/axpreview (five scripts) passing on the same build.

| App | Section | Result |
| --- | --- | --- |
| axPreview | 1.9 (every file opens) | Passed |
| axPreview | 5 (Details list, kinds, Ctrl+E, Open with) | Passed |

Afterwards, Tab to the Details list was reported missing on a build made a minute before the merge (it had no list);
on 3.7.2's code, rebuilt from main, the user confirmed it: "success".

## 2026-10-24, axTask and axPass (the branch `axTask`, before its merge)

- Run by the user with NVDA on builds of `axTask` up to cffd363, over the day's rounds of asks; the result given as
  "all tests pass for all pending requests". NVDA version, synthesizer and Windows build not given.
- axTask, `docs/axTask/nvda-test-plan.md`: Passed, after its rounds were built and heard again: the sections of the
  top level with Ctrl+Up and Ctrl+Down (1.1), Ctrl+M and the Section field (1.9), one level of lists, F2 in place.
- axPass, `docs/axPass/nvda-test-plan.md`: Passed, with KeePassXC 2.8.0: unlocking, the fields, the copies, Ctrl+M,
  the password fields named as instructions (4.9) and the backups (4.10).
- The shared pieces on the branch, heard through these apps: `AxKitList` (type-to-find, the edge earcon, the rename in
  place moved from axFile), `ItemForm`, the hidden notice field. axFile's rename in place and the edge sound in axGIT
  and axMedia were among the pending requests the user's words cover; not listed step by step.
- Automated alongside: `tests/scripts/axTask` (three scripts), `tests/scripts/axPass`, `tests/scripts/axFile`, all
  passing; every test project.

## 2026-10-24, axProcess (the branch `axProcess`, before its merge)

- Run by the user with NVDA on a Debug build of `axProcess` at 479792e, started from a scratch folder; the result given
  as "axprocess seems to be working and all my tests pass".
- axProcess, `docs/axProcess/nvda-test-plan.md`, every section: Passed, after the first round's asks were built and
  heard again: the edge earcon at the ends of the lists (1.7), Tab and Shift+Tab round the views (1.8), Shift+F10 and
  the Applications key opening the view's menu (1.9), only the view's menu enabled (1.9a), type-to-find with Shift and a
  letter backwards (1.9b), axProcess in Recently closed (1.10).
- In the same build and so heard with it: type-to-find with Shift backwards in axFile's folder and Actions lists,
  axGIT's list and axMedia's queue (AX-8.6), and axFile's Shift+F10 through the window base's `OpenBarMenu`; not run
  as their own plans' steps.
- Automated alongside: `tests/scripts/axProcess` (two scripts) and the axFile, axGIT and axMedia scripts, all passing.

## 2026-10-23, axKit 3.5.0 (the branch `palette`)

- Run by the user with NVDA, step by step in batches, on `palette` from 0d46e82 to the release; the fixes the pass asked for went in as it ran, each heard again. The user stopped tired before the last apps and released; still owed, for the next pass: axGIT steps 4a and 5, axMedia steps 1a and 2a, axPreview step 9a (all passing in the automated scripts), axStart section 1 step 7 and section 5 steps 1, 2 and 4 (the Start menu, sign-in and the update in place, on the installed release), the help section's steps 5 and 8 of axClaude.

Run so far, by the user with NVDA, 2026-10-23, on `palette` after 0d46e82:

- axStart section 1 steps 1 to 6 and 5a, section 2 steps 2, 3, 5 and 9: Passed. The first run was against a stale
  axStart from a scratch build of 2026-10-22, which kept the hotkey; on the current build all passed, but the edge
  note was "a bit soft and swallowed by the speech". The note was made louder and two notes falling an octave, and
  the steps passed again with it.
- axStart section 2 steps 1, 4, 6, 7 and 8, section 3 steps 1 to 8: Passed. Section 1 step 7 (the Start menu
  shortcut) waits for the release, since the shortcut starts the installed copy.
- axStart section 4 steps 1 to 4 and 6 (step 5 with Windows+E), section 5 steps 3 and 5: Passed. Section 5 steps 1, 2
  and 4 (sign-in and an update in place) wait for the release.
- axClaude section 12 steps 4a, 18, 19, 20 and 21: Passed, in a second window on
  `manual_tests/axClaude/project-for-conversations`.
- axClaude Session, Open folder in new window and Open recent folder in new window (new, dfe9ec1): Passed.
- axDown section 6 step 6 (the palette) and section 2 step 6's Alt+Backspace and Shift+F10: Passed.
- axFile section 3 step 7: Failed at first (Shift+F10 opened no menu, then a context menu silent on its first opening); now Shift+F10, the Applications key and a right click open the bar's Item menu as Alt+I does (AF-D13): Passed, "good enough for now". Alt+F4 on axFile left axClaude's Session menu open: fixed in the window base, Passed.
- axFile, by ear: the menu bar walk (3.7a), Open in axKit (3.9), Copy and Paste (3.6, with the paths as text and File Explorer both ways), two copies side by side (3.6), Move to folder with completion (3.1a, new), the palette (5.6): Passed.
- axTerminal: Speak output all lines by default (AT-5.3, decided at this pass); Ctrl+Up and Ctrl+Down through the history with the recall earcon and the changed command kept as the last item (AT-3.9, new): Passed, "working perfectly".
- axTerminal: the unrecorded command's status, an output line that starts like the prompt, a two-row prompt, New session's fresh screen, Open recent folder in new window, the stop at once, the quick keys reading one row (016d23a): Passed.
- axFile steps 2a (type-ahead), 10a (filter and sort), 12b (Ctrl+N at This PC), 13a (marks under a filter): Passed.

## 2026-10-22, axKit 3.3.1

- Commit: the `split` branch at its merge into `main` (8504ede), released as 3.3.1; 3.3.0 was tagged from it but not published (a test the Release build refused, fixed in 2b91f7a, no change to the apps).
- Run by the user, with NVDA. The user asked for every step of every plan to be marked as tested, and will report anything that comes up; no step was reported failing. The results below are that word, not a step-by-step log.
- In it, among the rest: the split of AxFile into AxFile, AxMedia and AxPreview; the Actions list with its sections and selection lines; Shift+Space and the Selection menu; Escape deselecting; Left and Right in the folder list; completion in Go to folder; the Drives submenu and Alt+D; AxDown's and AxJSON's undo and redo from 3.2.2.

### AxClaude

Plan: `docs/axClaude/nvda-test-plan.md`. Files: `manual_tests/axClaude`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Startup and focus | 1.1, 1.2, 1.3, 1.4, 1.4a, 1.5, 1.6, 1.7 | Passed |
| 2. Dialogs and plain conversation | 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.7a, 2.8 | Passed |
| 3. Reading while output arrives | 3.1, 3.2, 3.3, 3.4, 3.5 | Passed |
| 4. Headings and quick keys | 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.7a, 4.8, 4.9, 4.10, 4.11, 4.12, 4.13, 4.14, 4.15 | Passed |
| 5. Permission mode, prompts and interruption | 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7 | Passed |
| 6. Sending while Claude works | 6.1, 6.2, 6.3 | Passed |
| 7. Project folder | 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8 | Passed |
| 8. Lifecycle | 8.1, 8.2, 8.2a, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8 | Passed |
| 9. Options and diagnostics | 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9, 9.10 | Passed |
| 10. Claude Code missing, and the installer | 10.1, 10.2, 10.3 | Passed |
| 11. Updates | 11.1, 11.2, 11.3, 11.4, 11.5, 11.6 | Passed |
| 12. Claude's questions and screens: the control area (D33) | 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 12.9, 12.10, 12.11, 12.12, 12.13, 12.14, 12.15, 12.16, 12.17 | Passed |
| 13. The axKit menu (bundle AX-9) | 13.1, 13.2, 13.3, 13.4, 13.5, 13.6 | Passed |

### AxDown

Plan: `docs/axDown/nvda-test-plan.md`. Files: `manual_tests/axDown`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Install and start | 1.1, 1.2, 1.3, 1.4 | Passed |
| 2. Reading and writing | 2.1, 2.2, 2.3, 2.3a, 2.4, 2.5, 2.6 | Passed |
| 3. Files | 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11 | Passed |
| 4. Find and go to line | 4.1, 4.2, 4.3 | Passed |
| 5. Text size and font | 5.1, 5.2 | Passed |
| 6. Menus and help | 6.1, 6.2, 6.3, 6.4, 6.5 | Passed |
| 7. Updates and uninstall | 7.1, 7.2, 7.3 | Passed |
| 8. Navigation (from the version with section 9 of the spec) | 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7 | Passed |

### AxJSON

Plan: `docs/axJSON/nvda-test-plan.md`. Files: `manual_tests/axJSON`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start | 1.1, 1.2, 1.3, 1.4 | Passed |
| 2. Navigation (AJ-2.2) | 2.1, 2.2, 2.3, 2.4, 2.5, 2.6 | Passed |
| 3. Format (AJ-2.3) | 3.1, 3.2, 3.3, 3.4 | Passed |
| 4. The axKit menu and About | 4.1, 4.2 | Passed |

### AxGit

Plan: `docs/axGIT/nvda-test-plan.md`. Files: `manual_tests/axGIT`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start (AG-1, AG-2) | 1.1, 1.2, 1.3, 1.4, 1.5, 1.6 | Passed |
| 2. From and to the other windows (AG-1.4) | 2.1, 2.2, 2.3, 2.4, 2.5 | Passed |
| 3. Changes (AG-3) | 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7 | Passed |
| 4. Log and branches (AG-4, AG-5) | 4.1, 4.2, 4.3, 4.4, 4.5 | Passed |
| 5. Text, notices, the axKit menu (AG-2.4, AG-6) | 5.1, 5.2, 5.3, 5.4, 5.5, 5.6 | Passed |

### AxTerminal

Plan: `docs/axTerminal/nvda-test-plan.md`. Files: `manual_tests/axTerminal`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start (AT-1, AT-2) | 4 checks | Passed |
| 2. Commands (AT-3, AT-5) | 10 checks | Passed |
| 3. Shells and folders (AT-1.2, AT-1.6) | 6 checks | Passed |
| 4. Text, notices, the axKit menu (AT-4, AT-6) | 6 checks | Passed |

### AxFile

Plan: `docs/axFile/nvda-test-plan.md`. Files: `manual_tests/axFile`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start and the list (AF-1, AF-2, AF-3) | 1.1, 1.2, 1.3, 1.3a, 1.4, 1.4b, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10, 1.10a, 1.11, 1.12, 1.12a, 1.13 | Passed |
| 2. From and to the other windows (AF-1.3) | 2.1, 2.2, 2.3, 2.4 | Passed |
| 3. File operations and the entry's menu (AF-4) | 3.1, 3.1a, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.7a, 3.7b, 3.8, 3.9, 3.10, 3.11, 3.12, 3.13, 3.14 | Passed |
| 4. Sound, video and reading: the hand-overs (AF-5) | 4.1, 4.2, 4.3, 4.4, 4.5 | Passed |
| 5. Text, notices, the axKit menu (AF-6) | 5.1, 5.2, 5.3, 5.4, 5.5 | Passed |
| 6. The Actions list (AF-9) | 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 6.8, 6.9 | Passed |

### AxMedia

Plan: `docs/axMedia/nvda-test-plan.md`. Files: `manual_tests/axMedia`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start and the queue (AM-1, AM-2, AM-3) | 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8 | Passed |
| 2. The hand-over (AM-1.2, AM-1.3) | 2.1, 2.2, 2.3, 2.4, 2.5, 2.6 | Passed |
| 3. Playing (AM-4) | 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9 | Passed |
| 4. Text, notices, the axKit menu (AM-5) | 4.1, 4.2, 4.3, 4.4 | Passed |

### AxPreview

Plan: `docs/axPreview/nvda-test-plan.md`. Files: `manual_tests/axPreview`.

| Section | Steps | Result |
| --- | --- | --- |
| 1. Start and the text (AP-1, AP-2) | 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10, 1.11, 1.12 | Passed |
| 2. The hand-over (AP-1.2, AP-1.3) | 2.1, 2.2, 2.3, 2.4, 2.5, 2.6 | Passed |
| 3. Text, notices, the axKit menu (AP-3) | 3.1, 3.2, 3.3, 3.4 | Passed |

