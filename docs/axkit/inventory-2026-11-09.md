# The inventory and consolidation run of 2026-11-09

Asked by the user on 2026-11-09 morning, for a day he is away: "do a complete inventory of the repo and codebase to
get things into a really optimized and efficiently shared codebase so that we can start improving things in a simple
and coherent way using shared components... be very thorough and thoughtful. Make a plan first to stay on track
throughout the day." This page is that plan, then the inventory, then the record of what was done. It is the run
`cleanup-run.md` specified on 2026-10-25, carried out by one assistant session with read-only helpers for the sweep
rather than the agent workflow that page names; its ground rules (section 1), its gate (section 2), its work list
(section 3) and its order (section 4) apply word for word. `inventory-2026-10-24.md` is the previous inventory; its
twelve items are done, so this one starts from a fresh search.

**The one rule over everything: nothing the user can do today stops working, and nothing they hear changes.** A
cleanup that would change a key, a menu item, an announcement, a notice text, a file format, a setting name, a
command-line option or a default is not made; it goes to section 11 for the user.

## 0. The plan of the day

Where: the branch `cleanup`, cut from `main` at 3d180f1, in a worktree under the session's scratchpad (the user's
checkout runs axKit; a build there would write into the running program's `bin`). One step per commit, imperative
subject, the reason in the body; every commit passes the gate below. The branch is pushed as it grows and never
merged by the assistant: the user merges after the NVDA checks of section 12.

The gate for every commit: `.\test.ps1` (every test project, Release; in a worktree `PrivacyTests` fails on `.git`
alone, so its Collection line is read every run), `dotnet build src\AxKit\AxKit.csproj -c Release` with no warning,
`tools\outside-calls.ps1 -Check` when a source near an outside call changed, and every PowerShell script parsing when
a script changed. The final gate adds the automated scripts (`tools\AxAutomate` on `tests\scripts`) and
`publish.ps1 -NoInstall` in the worktree.

| Phase | What | State |
|---|---|---|
| 0 | The branch and the worktree; this plan written and committed; a tracker item in the management lists | done |
| 1 | The baseline gate: tests, the Release build, the unused-code analyzers run once (IDE0051, IDE0052, IDE0059, IDE0060, IDE0005, CA1812, CA1822, CA1823 and kin, as warnings on a Debug build of the worktree) | running |
| 2 | The sweep: five read-only passes in parallel (the shared window pieces and the windows' own code; the pure code and its tests; the three window families and the list windows side by side; hacks and the attack surface; the drift of notes, specs and scripts), each written up under `scratchpad\inventory`, verified by the assistant before anything is changed, and folded into sections 1 to 8 here with the ranked work list of section 9 | to do |
| 3 | Unused code (3.1), one project at a time, shared projects last | to do |
| 4 | Copies to fold (3.2), the lowest risk first, each piece moved in a commit of its own with every note that named it | to do |
| 5 | Hacks to make plain (3.3), only the ones whose reason is understood from the code or the notes | to do |
| 6 | Attack surface (3.4), each finding with a test, added to the audit as S-54 onwards | to do |
| 7 | Drift: notes, specs and scripts brought back to the code, no behaviour touched | to do |
| 8 | The final gate, the report (`cleanup-run-report.md`), the branch pushed, the tracker items for what needs the user, the NVDA sections worth hearing again, and the recommendation of what to do next | to do |

A phase that runs long is cut, not rushed: whatever is left of it goes to section 9 as open, with its reason.

## 1. The repository, measured (main at 3d180f1)

419 C# files, 57,422 lines, in 48 projects; 73 Markdown documents; 45 scripts.

| Part | Projects | Lines | Note |
|---|---|---|---|
| `AxKit.Forms`, the shared window pieces | 1 | 10,302 | 58 files: the window base and its partials, the lists, notices, the console view, the editor shell, the item form, the hotkey form, the channels |
| `AxKit.Core`, the shared pure code | 1 | 7,982 | 73 files: settings, paths, the apps listing, the updater, reports, MCP, the pseudo console and the screen, text helpers |
| The fourteen window projects | 14 | 13,523 | axFile 3,046; axClaude 2,266; axTask 1,419; axPass 1,194; axGIT 1,116; axTerminal 1,054; axMedia 831; axProcess 650; axClip 567; axStart 535; axCalc 476; axPreview 138; axJSON 136; axDown 95 |
| The thirteen app Core projects | 13 | 11,321 | axClaude 2,900; axFile 1,475; axTask 1,013; axPreview 880; axTerminal 861; axJSON 755; axMedia 753; axProcess 703; axGIT 692; axCalc 604; axPass 391; axClip 158; axStart 136 (axDown's pure code is in `AxKit.Core`) |
| `AxKit`, the executable | 1 | 183 | `Program.cs` |
| The tools | 4 | 2,592 | AxAutomate 1,606; PtyCapture 357; EdgeDrive 352; QaRun 277 |
| The tests | 14 | 11,519 | AxKit.Tests 4,161 (415 tests); AxClaude.Tests 3,163; the rest 134 to 1,228 each |

The three smallest window projects (axDown, axJSON, axPreview) are records over the editor shell: the shape every
app should tend towards. The three largest (axFile, axClaude, axTask) are where the sweep looks hardest.

## 2. The shared window pieces (`AxKit.Forms`) and who uses them

(Filled in phase 2.)

## 3. The shared pure code (`AxKit.Core`) and the app Cores

(Filled in phase 2.)

## 4. Copies: the same thing written twice

(Filled in phase 2: each with its places, what differs, the shared home, the risk.)

## 5. Unused code

(Filled in phase 2 from the analyzers and the reference search: members, types, parameters, settings properties,
usings, leftover comments.)

## 6. Hacks

(Filled in phase 2: timers, BeginInvoke chains, flags, broad catches, reflection, pragmas; each with its reason or
its plain replacement. The gotchas of the notes are facts and stay.)

## 7. Attack surface

(Filled in phase 2, after `security-audit-2026-10-24.md`: processes, outside paths, parsers, pipes, logs.)

## 8. Drift: notes, specs and scripts

(Filled in phase 2.)

## 9. The work list, ranked by payoff against risk

(Filled in phase 2; each line gets its commit or its reason for being left, as the day goes.)

## 10. Tests

(Filled in phase 2: what is pinned by a test, what the run adds, what stays untested and why.)

## 11. Needs the user

(Cleanups that would change what is heard or done, and decisions.)

## 12. NVDA sections worth hearing again

(The test plan sections that a moved piece touches, for the user's pass before the merge.)

## 13. What to do next

(The recommendation for the days after this one, written at the end of the run and mirrored in the priorities list
of the management repository.)
