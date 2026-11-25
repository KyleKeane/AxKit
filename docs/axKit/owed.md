# Owed

Written by `tools/owed.ps1` from `docs/axKit/cases.json`, `docs/axKit/repairs.json` and the Owed sections of
`docs/axKit/nvda-test-record.md`; never edit it by hand. Everything that is tested in code and not yet heard or
tried for real, by the kind of check it waits for. An entry leaves when its source changes: a case gets its proof,
a repair's `owed` entry is taken out and its `verify` says how it was checked, a section of the record becomes a
dated pass; then the script writes this page again.

## Where it stands

- 33 owed in all.
- Owed a hearing with NVDA: 17.
- Owed a live check with a real outside program or situation: 7.
- Owed a window check on the hidden desktop: 8.
- Owed a test in code: 1.

## Owed a hearing with NVDA

- The NVDA test record: what the closed items of 2026-11-25 left for the ear (the backlog cleared at the maintainer's word). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: the maintainer's choices of 2026-11-24, by ear. The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: the Store's copy after its install and after its update (2026-11-24, bundle AX-13.4a, AX-13.4c and AX-3.1; `KyleKeane/AxKit-reports issues 11, 13 and 15`). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: the state from the event core, 2026-11-17 (`docs/axClaude/events-and-grounding.md` EG-6.1 to EG-6.4, WP-A16). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: the keys and sounds of 2026-11-16 (Alt+1 to 3, Ctrl+Period, Ctrl+Slash, navigation mode's three sounds; `docs/axKit/SPEC.md` AX-8.13 to AX-8.15). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: a menu's refusal in navigation mode and the last full stop of joined words (2026-11-16, `docs/axKit/SPEC.md` AX-8.13 and AX-7.3). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: the AI tools run a window's command (the branch `push2/mcp-run`, 2026-11-11, `docs/axKit/mcp.md` MC-7). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: the twelve decided changes (the branch `improvements`, 2026-11-09; the inventory of that day is in the git history). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: the reports, what the scripts leave (2026-11-08, `docs/axKit/reports.md`). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: axClaude, `/clear` empties the window (2026-11-07, `docs/axClaude/SPEC.md` FR-8.8). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- The NVDA test record: access layers and consent (the branch `licence`, 2026-10-29, `docs/axKit/SPEC.md` AX-11). The steps are in `docs/axKit/nvda-test-record.md`, under the heading "Owed:" with these words.
- R-54: the notice about a changed file in Claude's working folder.
- R-56: the sentence of the AI tools submenu when the setting was changed outside axKit or its key is gone.
- R-57: the end of the sentence after a report is sent, which says how many waiting reports were not sent.
- R-61: axMCP's notice that reads a server's whole command line before it is agreed to.
- R-95: the refused sound in the apps that refused in silence until 2026-11-25, and the switches' checked and not checked.
- R-101: axFile's Windows menu opens with its first item read, now tracked through NativeMenu.Track.

## Owed a live check with a real outside program or situation

- R-54: a print-mode question to the real Claude Code with a hook and a .mcp.json in Claude's working folder (tools/upstream.ps1 -Verify).
- R-59: a real update at the next release, since nothing else reaches the check between the hash and the extraction.
- R-61: a start of a server from Claude Code's own files that has environment variables.
- R-64: a start of each app with a remembered path on a network share that is away.
- R-65: a second start against a background app that really hangs (the message box, No, Yes, the process ended and the app started), and the read of the other process's command line run for real.
- R-70: a start of axGIT, and of PowerShell 7 in axTerminal, on a computer where the program is installed for the user alone and so found through the PATH.
- C-62, every app, an update changed outside axKit: a run of a real update at the next release; no test reaches the extraction.

## Owed a window check on the hidden desktop

- R-54: the notice about a changed file in Claude's working folder, which no script reaches without a live Claude Code.
- C-34, axStart, axClip and axHelp, a key held by another program: a window script in which another process holds the hotkey before the app starts; no script registers a key of its own yet.
- C-35, axStart, axClip and axHelp, a key held by another program: a window script in which another process holds the key that is chosen in the Hotkey notice.
- C-39, axDown, axJSON and axPreview, a data folder or file changed outside axKit: a window script that activates the window after the change (a test window is never activated), or the step by ear of docs/axDown/nvda-test-plan.md.
- C-40, axDown, axJSON and axPreview, a data folder or file gone while the window is open: a window script that deletes the open file, then saves, and finds the file written again.
- C-48, axGIT, an outside program missing: a window script that starts axGIT with git's program pointed at a file that is not there.
- C-49, axPass, an outside program missing: a window script that starts axPass in a run where the tool is not found; the scripts of axPass need KeePassXC installed.
- C-50, axTerminal, an outside program missing: a window script that asks for a shell whose program is not installed in the run.

## Owed a test in code

- C-55, every app, Claude's working folder cannot be written: a test of AiRequest's run with a no-hooks file that cannot be written.

## Heard since, by the workflow record

No longer owed: the script each names passed with NVDA listening (`docs/axKit/workflow-record.json`).

- R-53: axTerminal's refusal of shells in a settings file whose seal does not check. Heard: `tests/scripts/axTerminal/08-user-shells.txt` passed with NVDA at e8536af1.
- R-53: axGIT's refusal of trusted repositories in a settings file whose seal does not check. Heard: `tests/scripts/axGIT/07-own-programs.txt` passed with NVDA at e8536af1.
- R-53: axMCP's refusal of servers in a settings file whose seal does not check. Heard: `tests/scripts/axMCP/02-your-servers-errors-prompts.txt` passed with NVDA at e8536af1.
- R-53: axFile's refusal of programs of Open with in a settings file whose seal does not check. Heard: `tests/scripts/axFile/10-settings-changed-outside.txt` passed with NVDA at e8536af1.
- R-53: axClaude's refusal of claudePath in a settings file whose seal does not check. Heard: `tests/scripts/axClaude/33-settings-changed-outside.txt` passed with NVDA at e8536af1.
- R-63: the notice of an item window when the settings cannot be saved. Heard: `tests/scripts/axTask/11-settings-not-saved.txt` passed with NVDA at e8536af1.
- R-63: the system line of a console window when the settings cannot be saved. Heard: `tests/scripts/axTerminal/12-settings-not-saved.txt` passed with NVDA at e8536af1.
- R-64: the sentence that a remembered path is on the network and was not opened. Heard: `tests/scripts/axFile/10-settings-changed-outside.txt` passed with NVDA at e8536af1.
- R-66: a damaged item named in its list in axTask. Heard: `tests/scripts/axTask/10-damaged-item.txt` passed with NVDA at e8536af1.
- R-69: the sentence of axClip at its first showing when the kept history could not be read. Heard: `tests/scripts/axClip/03-history-not-read.txt` passed with NVDA at e8536af1.
- R-72: the first press of an action key of a list that moves or opens, in axTask. Heard: `tests/scripts/axTask/04-action-keys.txt` passed with NVDA at e8536af1.
- R-74: the details of a started server in axMCP with their error lines. Heard: `tests/scripts/axMCP/01-start.txt` passed with NVDA at e8536af1.
