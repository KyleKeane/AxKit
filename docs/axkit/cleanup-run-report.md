# The cleanup run's report (2026-11-09)

The run `cleanup-run.md` specifies, carried out on 2026-11-09 as the consolidation run of `inventory-2026-11-09.md`
(the plan, the inventory, the work list with every item's state, what needs the user and what to hear again). This is
the report its section 6 asks for; the inventory page holds the detail. Nothing a user can do stopped working and
nothing they hear changed, by construction and by the tests; section 5 lists the places to hear once before the merge.

## 1. The gates

| Check | Baseline (main at 3d180f1) | Final (the branch `consolidation`) |
|---|---|---|
| `.\test.ps1`, every project, Release | 1,125 tests, all green | 1,140 tests, all green (seven new tests, S-55's four cases, S-54's two, S-56's two) |
| `dotnet build src\AxKit\AxKit.csproj -c Release` | no warning | no warning |
| `tools\outside-calls.ps1` | the page current | the page current (rewritten four times for lines that moved; 41 calls in 26 files, `KeePassCli.StartLasting`'s gone with the method) |
| Every PowerShell script parses | yes | yes |
| The unused-code analyzers (IDE0051, IDE0052, IDE0059, IDE0060, IDE0005, CA1812, CA1822, CA1823, CA1859) | 58 IDE0005, 4 IDE0060, 14 CA1822, 4 CA1812, 42 CA1859; no unused private member | IDE0005 and IDE0060 at zero; CA1822, CA1812 and CA1859 left on purpose (section 3) |
| `tools\AxAutomate` on `tests\scripts`, `publish.ps1 -NoInstall` | not run at the baseline | section 6 of the inventory page records the final run |

Every commit passed the first four checks before it was made (the gate script of the run, outside the repository,
ran them on the worktree; a failing one was fixed before the commit, never committed and patched).

## 2. The commits, by section of `cleanup-run.md`

The branch `consolidation`, cut from `main` at 3d180f1 and rebased onto 95324d4 (the sync fix); 29 commits.

The plan and the record: `724e297` the plan; `edeb5b4` the gate runs in a worktree (the privacy test skips a `.git`
file; `test.ps1` goes on past a failing test under redirection); `11e3f59` the sweep's findings and the work list.

Section 3.1, unused code: `a27f6b0` 36 using lines nothing needed, the blocks sorted; `2a9a127` two parameters nothing
read; `b27ced4` twelve own-file members and types private or internal, `ConsoleForm.Post` private,
`KeePassCli.StartLasting` gone with the notes and the spec's command list that still named the tool's `clip`.

Section 3.2, copies to fold, each piece moved not copied, every note that named it changed in the same commit:
`b5af56c` `RestartArguments` by default from the verb and `PathInHand` (eleven overrides and `ConsoleForm.Verb` gone);
`42cc6f5` `Notice.Ask` for twelve yes-or-cancel questions; `16b84a0` one free-file-name rule (`ItemFiles.FreePath`, from
axFile's and axPass's loops); `680ff97` the small copies of the pure code (the item dates' formats, the JSON string
readers, the WindowsApps folder, the PATH parser, a file's head, the editors' file types from `Dispatch`, the recent
constants); `9490e8e` `PlaceControls`, `Failed`, `SetTitle`, `AddFindInListKeys` and `AxKitList.OpenMenuOnRightClick`
on the shared pieces; `f5890a7` `HotkeySettings` in `AxKit.Core` and `HotkeyForm` owning the hotkey, the Run key and
the menu items both apps wrote; `59aeeb0` `ChoiceItems` for six one-checked menus; `b9bd4c5` `AskText` on the base for
six one-field notices; `915fdc0` the shells' programs from `SystemPrograms`; `339c04c` `AxKitList.Fill` for four fills;
`72ecdf4` Copy report through `CopyText`.

Section 3.3, hacks: `9399ee9` a `Silent` notice in place of axClaude's flag around `ShowNotice`; `e249e49` a one-line
reason on ten empty catch blocks, four catch-alls and three posts.

Section 3.4, attack surface, each with its test and its row in `security-audit-2026-10-24.md`: `1b501ab` S-54 the
`--message` text logged as its length; `1f522c8` S-55 an item's file kept off Windows' device names; `de78fe3` S-56
the automation channel's lines capped as the MCP channel's, and the audit's one sentence about the hand-over's
receiver; S-57 (S-42's rule reaching every list `AxKitList.Fill` fills) came with `339c04c`.

Tests: `87b6250` the settings round trips of axPass, axProcess and axTask, the secret store, signatures, the pseudo
console and a missing git.

Drift: `13319b1` the notes back to the code; `e5c61a4` the specs; `2cdb06b` the scripts' words and the changelog
prefixes.

## 3. The lines of code, before and after

| Part | Before | After | Change |
|---|---|---|---|
| The fourteen window projects | 13,523 | 13,231 | -292 (axFile -58, axGIT -41, axProcess -31, axClaude -28, axClip -23, axStart -23, axTerminal -22, axMedia -21, axCalc -21, axPass -15, axTask -9) |
| The thirteen app Core projects | 11,321 | 11,249 | -72 (axPass -46, axFile -11, axStart -9; axPreview +4, axProcess +3 for the comments) |
| `AxKit.Forms` | 10,302 | 10,405 | +103 (the shared pieces the windows now call) |
| `AxKit.Core` | 7,982 | 8,053 | +71 (`HotkeySettings`, `CommandLineText`, `JsonElements`, the device names, the comments) |
| The tests | 11,519 | 11,731 | +212 (seven new test files, S-55's cases) |
| The executable and the tools | 2,775 | 2,775 | 0 |
| All | 57,422 | 57,444 | +22: the apps shed 364 lines, the shared projects took 174, the tests grew by 212 |

The count is of lines, comments among them; the one-line reasons of `e249e49` and the summaries of the new pieces are
in it. What matters is the direction: the same thing written once where it was written in six to twelve windows.

## 4. Looked at and left, with the reason

- `StatusLayout` (W6): nine windows set two labels and call `Fit`; a pair object would touch every one for two lines
  each, and the labels are read by name elsewhere. Left.
- The views flow of axGIT and axProcess (W15): the same shape, but the loads, the fills and the first-read words
  differ; the shared piece would be hooks for fifteen lines. Left.
- The console windows' folder and session frame (W16): medium risk, axClaude's state (`_launchedFolder`,
  `HideReplay`) to keep out of the base, both console plans to hear again. A session of its own.
- The first focus in the base's `OnShown` (W21): the automation channel starts in `OnShown` too, and its order against
  the focus was not checked in the time. Left.
- `MatchSpeaker` (W30), the 300 ms first-match speaker written in `HotkeyForm` and the palette: two hosts, a form and a
  notice; left for a later pass.
- One Claude Code locator (W17): the MCP side would gain the npm fallback axClaude has, a change of what the
  registration finds; the user's (section 11, item 7).
- CA1859 (concrete types for interfaces) and CA1822 on `ActionLog`'s entry methods: style the code does not follow
  (read-only contracts on purpose; the log is the object one talks to). CA1812 on `TempFolder`: compiled into every test
  project, used in most.
- The twelve settings classes' three lines each (`Load`, `Sanitize`, `Save`): by the notes' rule, an app's settings
  class stays in the app.
- axFile's New folder and Add a program (two of eight `AskText` copies): they announce the problem from a closing
  button, where `AskText` keeps the notice; folding them changes what is heard (section 11, item 1).
- The hotkey windows' matches list: a plain list box by design (AS-D2, never focused), so `AxKitList.Fill` does not
  reach it.
- `ComSpec` as cmd's source in axTerminal, S-1's `NoDefaultCurrentDirectoryInExePath`, S-53's refusal in axClaude's
  own `claude.cmd` lines, a cap on a zip's entries, `list.json`'s `open` actions, the `_asks` poll, the F1 Notices
  section, the focus after a dialog, the console windows' control keys in the item apps, the template values of
  `list.json`: each a behaviour question (section 11 of the inventory page).
- A test that pins the notes' identifiers to the code (W28): the sweep's scan is its shape; the allow list of .NET
  names is its cost. Recommended as the next drift stop.

## 5. Needs the user

The fourteen lines of `inventory-2026-11-09.md` section 11, one word each; and the branch `cleanup` on GitHub to
delete (section 0 of that page says why).

## 6. The NVDA sections to hear again

Section 12 of `inventory-2026-11-09.md`: the titles, the yes-or-cancel questions, the one-field notices, the one-checked
menus, the lists filled anew, the hotkey apps' options, axClaude's silent screen notice, the find keys of three list
windows, Copy report on a held clipboard.

## 7. Then the user decides

Merge `consolidation` into `main` after those checks, and release (`CHANGELOG.md`'s 5.2.2 section carries the bullets;
5.3.0 is the fitting number, since the shared pieces are a feature for every later change). The assistant does not
merge or release.
