# NVDA test record

What was checked with NVDA, against which version, and with what result. When each goal last passed is in `docs/axKit/workflow-record.json`, written by the pass itself from 2026-11-16 on; this page keeps what a pass found. The passes before 2026-11-15 (from 2026-10-22 on) were taken out on 2026-11-17 and are in the git history of this file; what they left owed is in the Owed sections below. The plans are each app's `docs/<app>/nvda-test-plan.md`; the files they use are made by `manual_tests/prepare.ps1` (`manual_tests/_main.md`).

## How to add a pass

Add a section under this one, above the earlier passes, so that the newest comes first: the date, the version, the commit tested, who ran it and how the result was given, and the environment: the NVDA version, the synthesizer and the Windows build (so that a regression is dated to an NVDA release rather than to some time). Under it, one table per app with a line per section of its plan: the steps run and the result, Passed, Failed (with what was heard) or Not run. A step that fails gets a line of its own with what NVDA said and what it should have said, and the bug's fix names the step. When a plan gains or loses steps, the next pass lists them as the plan has them then.

## 2026-11-16: every app, after the day of consolidation; by the assistant with NVDA listening (`main` f93f1e64)

`.\nvda-pass.ps1`, the user away at his own word, Windows 11 Pro 10.0.26200, the build 5.9.1 of commit f93f1e64:
137 scripts of `tests/scripts` and the four of `tests/nvda-scripts`, 141 in all. 139 passed with every announcement
also spoken by NVDA; 978 goals and checks passed, 2 failed and 12 were not run after them. The pass wrote each result
into `docs/axKit/workflow-record.json` itself.

| What | Scripts | Result |
|---|---|---|
| Every script but two | 139 | Passed |
| `axAutomate/02-run.txt`, `axCalc/01-basics.txt` | 2 | Failed in the pass, passed on the runner's own desktop afterwards |

What it found:

- The two failures were a key from outside the scripts. In both, the window's Keyboard shortcuts notice opened
  where no step presses F1 (in axCalc one second after the start, in axAutomate right after the Escape that stops a
  run), and in axAutomate NVDA also spoke a single letter no step types. Both were within the first minutes of the
  pass, and the user's own axClaude window stood with its Keyboard shortcuts notice open through the whole pass. The
  user said afterwards that he may have pressed a key by accident; nothing in axKit is suspected. Both scripts passed whole on the runner's own desktop right after (9 and 8 goals),
  so their goals are recorded at that tier and not as heard in this pass.
- The command inventory was stale for axProcess: the five commands of a process's context menu (Details, End
  process, End process tree, Show program in axFile, Copy line) were not in it. The snapshot of its first script is
  taken while the first read runs, when the menu holds only "Nothing selected"; with NVDA the read was over by then.
  `axProcess/01-start.txt` now takes a second snapshot once the list is read, and the five rows are in the inventory.
- Heard for the first time, and taken off `docs/axKit/owed.md`: a damaged item named in its list in axTask (R-66),
  axClip's sentence when the kept history could not be read (R-69), and the first press of an action key that moves
  (R-72).
- Still owed a hearing, since no script reaches them: the refusals of a settings file whose seal does not check
  (R-53), the notice about a changed file in Claude's working folder (R-54), the AI tools submenu after a change from
  outside (R-56), the count of waiting reports (R-57), the settings that cannot be saved (R-63), the remembered path
  on the network (R-64) and a server's error lines in axMCP (R-74). axMCP's notice before a server starts was heard
  by its title and first lines; its command line further down was not read out by NVDA in the pass, so R-61 stays
  owed.

## 2026-11-15, night: every app, after the workflows work and axClaude's event sources; by the assistant with NVDA listening (branch `workflows` 2ef04f4)

`.\nvda-pass.ps1`, the user away at his own word (asleep, and told that NVDA would speak), Windows 11 Pro
10.0.26200, NVDA 2025.3.3, Claude Code 2.1.325, the build 5.9.1 of commit 2ef04f4: 135 scripts of `tests/scripts`
and the four of `tests/nvda-scripts`, 139 in all (3650 s). All 139 passed in the one full run, with every
announcement also spoken by NVDA, and with them every section the scripts mark: 576 goals (15 of them scripts that
mark none and count whole) and 399 checks. The runner took the foreground itself; the user's own axClaude window was
open throughout and was left as it was.

| What | Scripts | Result |
|---|---|---|
| Every script | 139 | Passed |

After the pass the 135 scripts of `tests/scripts` were played on the runner's own desktop with the same build: 134
passed. `axClaude/03-reading.txt` failed at step 69, by a race of the script: "Done" is said 0.4 s after the bell, and
when Haiku needed longer for the numbers than the script's reads, the first reply's Done was still on its way when
the status bar said ready, and the script took it for the second reply's. The script now waits a second there and
takes what was said; with that it passed three times on that desktop, each time in the slow case. That wait is the
one line of the scripts this pass has not heard.

This is the pass that the section "Owed after the workflows work" asked for, and it stands in that section's place.
Of what that section listed as built without NVDA:

- The NVDA tier's key codes, read from `AxKit.Core/KeyChord`: proven for every key name the scripts press. The names
  new to this tier (`period`, `comma`, `apps`, `slash`, `semicolon`, `add`, `subtract`) are still pressed by no
  script, so they are still unheard.
- `do` with NVDA listening: heard in `axDown/04-find-and-go-to-line.txt`, eight steps: Find (Ctrl+F), Find next
  (F3), Find previous (Shift+F3) and Go to line (Ctrl+G), each pressed through `SendInput` and what it says spoken
  by NVDA. It is the one script that uses the step so far.
- `do` for a command with no key (through the channel's `choose`): still unheard, since no script uses it.
- The window listing its commands at every `do` and `check commands` step (23 scripts have the check): NVDA said
  nothing of it; the speech taken around those steps holds only what the window itself said.
- The workflow record for the `nvda` tier was not written. The option that writes it (`--record`, WP-B13 of
  `docs/axKit/user-workflows.md`) is not built, so `docs/axKit/workflow-record.json` does not exist yet and what
  this pass found is in this section alone.

What this pass is the first to hear:

- A background workflow's live progress rows kept out of the conversation and never said, the count of what runs in
  the status bar, how far the workflow is on Ctrl+B, and Options, Background work, "Show Claude Code's live progress
  rows in the conversation" (`axClaude/27`).
- Options, Developer: "Say the event sources" ("Events. Marks: 3. Hooks: on, 2 received. Session file: followed, 4
  entries. Core: in shadow."), and "Events from Claude Code's hooks" and "Ground the conversation in Claude Code's
  session file" with what each says when it is turned off and on (`axClaude/28`, `30`, `32`).
- A turn against the stand-in of Claude Code, with its marks, its events and its session file (`axClaude/29`, `30`,
  `32`), and a turn of the real Claude Code with the hooks' file passed (`axClaude/31`): no settings warning, two
  events received, the session file followed, the stop event 63 ms after the turn's end mark.
- The goals and checks that the scripts now mark: they change nothing that is said.

Still unheard after this pass: `do` for a command without a key, and the seven key names above.

Owed to the user's ear: a real background workflow with the live rows hidden (that nothing of them is heard, and
Ctrl+B when he asks); "Say the event sources" in his own session, and whether its sentence is good aloud; the how-to
pages of `docs/axKit/workflows` read with NVDA, and axHelp's answers from them; his own session with "Events from
Claude Code's hooks" turned on, when he chooses to (that nothing is heard differently, and whether Claude Code's end
at a restart or a close feels slower).

## 2026-11-15, evening: every app, after 5.9.1; by the assistant with NVDA listening (`main` 1db380e, then the fix of axClip's scripts)

`.\nvda-pass.ps1`, the user away at his own word, Windows 11 Pro 10.0.26200, NVDA 2025.3.3, Claude Code 2.1.325:
129 scripts of `tests/scripts` and the four of `tests/nvda-scripts`, 133 in all. 131 passed in the full run, with
every announcement also spoken by NVDA. The runner took the foreground itself, and the user's own axKit windows were
left as they were.

| What | Scripts | Result |
|---|---|---|
| Every script but two | 131 | Passed |
| `axClip/01-basics.txt` step 28, `axClip/02-history-kept.txt` step 27 | 2 | Failed, as in the early morning: a copy made by the script was missing from the history. Run again, a different copy went missing each time, and reading the clipboard for longer in axClip (ten tries, not three) changed nothing, so it was undone. The cause is the script's copy: clip.exe gives up without a word when another program holds the clipboard open, and the user's own axClip and NVDA read it too. The scripts now copy through a small `copy.ps1` that reads the clipboard back and tries again; axClip's folder then passed three times running with NVDA |

What this pass is the first to hear: Ctrl+Space between edit mode and navigation mode (`axDown/16`, `axClaude/25`),
the music on Ctrl+M (`axClaude/09`), Options, Claude Code settings on a stand-in settings file (`axClaude/26`), the
unsent message said when axHelp's conversation comes back (`axHelp/02`), and one Escape ending the key test.

Heard by the user the same evening, on his own build: the modes in axDown and axClaude, a /btw answer in a real
session, Block Claude Code's feedback requests on his own settings, the unsent message in axHelp and one Escape
ending the key test, all as written. His main axClaude window closed one second after he put axHelp's conversation
away with Alt+F4: an ordinary close, no error in the log, most likely a second or repeated Alt+F4 that reached the
window given the foreground back. Left as it is, at his word.

## 2026-11-15, afternoon: every app, before 5.9.0; by the assistant with NVDA listening (`main` 3b39748, then the fix of axDown/15)

`.\nvda-pass.ps1`, the user away at his own word, Windows 11 Pro 10.0.26200, NVDA 2025.3.3, Claude Code 2.1.325:
126 scripts of `tests/scripts` and the four of `tests/nvda-scripts`, 130 in all. 129 passed in the full run, with
every announcement also spoken by NVDA. The runner took the foreground itself and touched no window outside the run:
the user's axClaude and axHelp windows were open throughout and were left as they were (axClaude/13's Close all
windows reached only the run's own two windows).

| What | Scripts | Result |
|---|---|---|
| Every script but one | 129 | Passed |
| `axDown/15-key-test.txt` step 49, `type z` | 1 | Failed: under the NVDA tier a typed character arrives as SendInput's Unicode input, whose key-down is VK_PACKET; the key test took that key-down and named it ("Packet: no command of this window"), and no character followed. Fixed the same hour: the key test translates a VK_PACKET key-down into its character and says it as one sent without a key press. Then `axDown/15` passed on the hidden desktop and axDown's 19 scripts passed with NVDA |

What this pass is the first to hear: the key test (Help, Diagnostics, Test a key) in axDown, axFile, axTask, axStart
and axClaude; the soft chords and the tool pop (by their earcon names); the view's self-check in every axClaude
script (an ERROR line fails a script, and none was written); Shift+Up and Shift+Down in Send key (`axClaude/05`); a
single several-answer question's review submitted (`axClaude/12` step 8a); `axkit_window_text` giving a console's
text (`mcp/06`).

Owed to the user's ear: the chords and the pop in a real session; the key test with the real keyboard (modifiers
alone, Alt+Tab ending it); F8 in a notice and in the hotkey windows; axHelp started hidden with "Keep the conversation
ready" on; the new Ctrl+Shift hotkeys pressed from another program.

## 2026-11-15, early morning: every app; by the assistant with NVDA listening (`main` 84d3f86 with the F8 key and axClaude's background work not yet committed)

`.\nvda-pass.ps1`, the user away at his own word, Windows 11 Pro 10.0.26200, NVDA 2025.3.3: 119 scripts of
`tests/scripts` and the four of `tests/nvda-scripts`, 123 in all (3311 s).

| What | Scripts | Result |
|---|---|---|
| Every script but three | 120 | Passed in the full run, every announcement also spoken by NVDA |
| `axClip/01-basics.txt` step 28, `axClip/02-history-kept.txt` step 27 | 2 | Failed in the full run: of the copies made half a second apart one was not in the history. Passed when axClip's folder was run again at once. The same was seen once without NVDA the evening before; the user's own axClip was running beside the test's and reads the same clipboard. Not known to be a fault of axClip; watch it |
| `axFile/03-operations.txt` step 385 | 1 | Failed once in the full run: after an undo the focus was still on the list when the script looked for "Name contains". Passed when axFile's folder was run again at once |

A first attempt that night failed 120 of 123 from the first script on, "no window of the run is in front": the
desktop was the foreground window when the pass started, and Windows let none of the run's windows come to the front.
With the user's axClaude window brought to the front first, the pass ran. An item of the priorities list.

Found by this pass, older than this work: `axClaude/13-axkit-menu.txt` runs Close all windows, which on the real
desktop reaches the user's own axKit windows too. His axClaude window asked "Close axClaude?" and stayed open, since
Claude had work running; an idle window would have closed. An item of the priorities list, High.

What this pass is the first to hear:

- axHelp's conversation that hides on closing and comes back (`axHelp/02`, `axClaude/20` and `21`; axClaude FR-9.6),
  and axKit's guides in Claude's working folder (`axHelp/01`).
- The hotkey windows after the foreground rule moved into `ForegroundReturn` (axStart, axClip, axHelp).
- F8, Say the status, in axClaude, axFile, axDown and axTask (bundle AX-7.10).
- axClaude's background work: the status, the start and finish sounds, the recurring sound, Ctrl+B, the spoken lines
  as an option (`axClaude/22`, FR-7.10), and the panel's rows hidden from a reply (FR-7.4b's neighbour, 607e2fb).

Owed to the user's ear: the three background sounds (provisional) and their interval; that the hotkey brings axHelp
back at once with the caret in the message field, and that hiding gives the keyboard back to the program before; F8
inside a notice and in the hotkey windows; axHelp started hidden with "Keep the conversation ready" on.

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

With NVDA, in a pass: `nvda-test-plan.md` section 25, steps 4, 5 and 6.
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
   this window, as Shift+F1"; the menus' shortcut read as "Alt+1."; the F1 texts' new lines.
8. axHelp's row "Ask about this window" in the command inventory lacks its further key Ctrl+Slash until a pass
   writes it: its only scripts start the real Claude Code.

## Owed: a menu's refusal in navigation mode and the last full stop of joined words (2026-11-16, `docs/axKit/SPEC.md` AX-8.13 and AX-7.3)

Two changes that are heard, made with the repair of what axDown answered an AI tool differently than a key; the
scripts of axDown, axJSON, axPreview, axTerminal, axTask and the AI tools pass on the runner's desktop. With NVDA:

1. In axDown, in navigation mode, choose Edit, Undo from the menu: the edge sound, then "Navigation mode: press
   Ctrl+Space to edit". Before, the words came without the sound.
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
