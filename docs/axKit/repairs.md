# Repairs

Written by `tools/repairs.ps1` from `docs/axKit/repairs.json`; never edit it by hand. The list of what the audits
found and what became of each finding: change the JSON in the same commit as the fix, then run the script.
`tools/repairs.ps1 -Open` prints what is still open.

## Where it stands

- **done**: 41 (7 high, 19 medium, 15 low)
- **deferred**: 3 (1 high, 2 medium)
- **declined**: 5 (4 medium, 1 low)

## The principles

- **P1. Say the effect where the thing is made.** Whoever makes a menu item, a command or a run states what it does and what it acts on; nothing important rests on a default. What states nothing is treated as the most careful case. (9 findings)
- **P2. One source, generated views.** A fact lives in one place (the menu bar, Dispatch.Apps, a JSON list); every page or list that repeats it is written by a script and checked by the same gate on the developer's computer and in CI. (6 findings)
- **P3. A failure says what, why and where to read.** Every refusal, error and failed check names the thing, the reason and the document or command that helps; nothing fails silently and nothing passes silently against the wrong thing. (7 findings)
- **P4. Bounded and cleaned up.** Every wait has a limit, every held resource is released on every path (try/finally), every input from outside has a size limit. (11 findings)
- **P5. An entry is its name, never a held object.** What is found in one moment and used in another (a menu item, a file, a selection) is found again by its identity when it is used, and refused when it is gone or changed. (4 findings)
- **P6. Writes are whole or not at all.** A file a user or the app depends on is written through a temporary file and put in place in one step; an undo step is dropped only once it has been applied. (2 findings)
- **P7. Tests check the built thing.** What matters is checked on the running window or the built data; a test that reads source text is lint, a help and never the proof. (1 finding)
- **P8. Documents describe the present.** Reference text says what is true now, in small files a reader with little context can hold; history lives in the changelog and git; open work lives in the priorities list. (9 findings)

## Done

### R-09, high, framework: Palette marks lived in ToolStripItem.Tag, one slot: a second refresh on a menu replaced the first

- Where: src/AxKit.Forms/CommandPalette.cs; src/AxKit.Forms/AxKitForm.Theme.cs; src/AxKit.Forms/ContextBarMenu.cs
- Principle: P2
- Fix: One record per item in one table (MenuItemInfo: its mark, withheld, left out, its refreshes as a list); Tag is no longer used for menus.
- Checked by: tests/AxKit.Tests/CommandPaletteTests (two refreshes both run; marks; withheld through owners)
- Commit: af0fac3
- Note: Shown bug: the palette listed Say new line's old check after Sound on headings registered its refresh on the same Text menu.

### R-11, high, framework: The one command list is not one: items are made by Add(Command), by Item() and by bare ToolStripMenuItem

- Where: src/AxKit.Forms/AxKitForm.cs; src/AxKit.Forms/ItemForm.cs; src/AxTask/TaskForm.cs; src/AxPass; src/AxMCP; src/AxAutomate; src/AxClaude/MainForm.Ui.cs; src/AxTerminal/TerminalForm.Ui.cs; src/AxFile
- Principle: P2
- Fix: In steps, one app per commit: (1) ItemForm's shared actions become Command records the apps place with Add; (2) Item() becomes private to the base; (3) each app's static raw items become commands, its filled lists state a mark; (4) the test counts every item made without a mark and its limit falls to zero.
- Checked by: tests/AxKit.Tests/CommandMarkTests' count falls with each step; every tests/scripts folder of the app passes
- Commit: 5e56218
- Note: About 131 items in the apps and 28 in the base state no mark (they ask before an AI tool runs them; safe, but a question more than needed, and their menu path is not in the report's recent actions). docs/axKit/app-kit.md AK-2 describes MenuSpec, Scope and Help, which do not exist: the document follows the code in the same steps. Settled 2026-11-13: all 160 judged and marked; Item(text, action, key, mark) with the mark required; items filled as a menu opens wrapped in CommandPalette.Marked; the empty-list lines NoEntry. Not done, by choice: ItemForm's shared actions stay AddKey commands with the apps' Context items beside them (the items now carry the same marks), since moving them would change every item app's menus for no gain a user or an AI tool would meet. docs/axKit/app-kit.md AK-2 says what is built.

### R-30, high, tooling: The local gate did not run the page checks that failed CI six times in three days

- Where: test.ps1; release.ps1; tools/outside-calls.ps1
- Principle: P2
- Fix: test.ps1 ends with the three page checks (outside calls, workflows, repairs); release.ps1 writes all three pages before its commit; the outside-calls page names the file and function of a call, no line number, so an edit above a call no longer makes it stale.
- Checked by: test.ps1 fails on a stale page; gh run list shows no page failure after it
- Commit: af0fac3

### R-32, high, tooling: The runner drove whatever build lay in src\AxKit\bin\Release when none was named

- Where: tools/AxAutomate/Program.cs; nvda-pass.ps1
- Principle: P1
- Fix: No default: --build or --exe is required; the console and the report's first lines give the program's version and when it was built; the exit codes are 0, 1 (a script failed), 2 (could not start).
- Checked by: a run without either option ends with the message; the report's header
- Commit: af0fac3
- Note: Cost two wrong diagnoses on 2026-11-13.

### R-33, high, tooling: An NVDA run that ended abnormally left NVDA's speech capture on

- Where: tools/AxAutomate/Program.cs; tools/nvda-capture/globalPlugins/axkitCapture.py
- Principle: P4
- Fix: The run's end is a finally; every run deletes an earlier run's flag first; the add-on ignores a flag older than two hours.
- Checked by: reading; nvda-pass.ps1 once
- Commit: af0fac3
- Note: Privacy: the capture writes what NVDA speaks into a file. The add-on installed in NVDA is a copy: tools/nvda-capture is copied to %APPDATA%\nvda\addons\axkitCapture and NVDA restarted for the two-hour rule to hold there.

### R-43, high, docs: Twenty-nine statements in the overview and user documents are false today

- Where: CLAUDE.md; README.md; docs/axKit/README.md; docs/axKit/plan.md; docs/axKit/SPEC.md; SECURITY.md; the user guides and test plans
- Principle: P8
- Fix: Each corrected: axAutomate runs workflows; seventeen apps; the menus by their names since 2026-11-11; About and Check for updates in Help; what plan.md calls unbuilt and is built; the broken sentence in ten user guides.
- Checked by: the list of the audit, each line read again
- Commit: af0fac3
- Note: All 25 groups of the audit's list corrected and read again; six further false statements seen on the way are in R-45 and R-46.

### R-44, high, docs: The list of apps is written by hand in at least eight places

- Where: CLAUDE.md; README.md; docs/axKit/README.md; docs/axKit/SPEC.md; run.ps1; src/AxKit/CLAUDE.md
- Principle: P2
- Fix: tools/apps.ps1 writes docs/axKit/apps.md from Dispatch.Apps (name, verb, group, what it opens, its folders and documents); the hand-written lists point to it or are checked against it; a test fails on an app count written as a word.
- Checked by: tools/apps.ps1 -Check in test.ps1 and build.yml
- Commit: af0fac3
- Note: tools/apps-page.ps1 writes docs/axKit/apps.md; the root CLAUDE.md, tools/new-app.ps1 and InventoryTests use it. Still by hand, and checked by InventoryTests for every app: the user-facing lists of README.md and docs/axKit/README.md, which are prose for readers.

### R-01, medium, security: Recent folders of axClaude and axTerminal started Claude Code or a shell for an AI tool without asking

- Where: src/AxKit.Forms/RecentMenu.cs; src/AxKit.Forms/ConsoleForm.cs
- Principle: P1
- Fix: RecentMenu.Fill and FillFolders take the mark from the caller, with no default; the two console lists pass act, the others open.
- Checked by: tests/AxKit.Tests/CommandMarkTests (the console lists pinned act); tests/scripts/mcp
- Commit: af0fac3
- Note: Introduced 2026-11-13 by the palette parity (070a005), found the same day.

### R-02, medium, security: The test automation channel is gated by an environment variable alone

- Where: src/AxKit.Core/AutomationEnvironment.cs; src/AxKit.Forms/AutomationChannel.cs
- Principle: P1
- Fix: The secret counts only together with the run's home folder, and never when either variable is set in the user's or the computer's persisted environment; a window serving the channel says Test run at its first activation.
- Checked by: tests/AxKit.Tests/AutomationEnvironmentTests; every tests/scripts run still starts
- Commit: af0fac3
- Note: Not done: the words Test run at a window's first activation, which every script's timeline would then hold; the two conditions close the hole without them.

### R-08, medium, framework: A menu item found for an AI tool was held and clicked later, passing its availability check by accident

- Where: src/AxKit.Forms/AxKitForm.Mcp.cs; src/AxKit.Forms/CommandPalette.cs
- Principle: P5
- Fix: A run keeps the entry's name and menus and finds it again among the entries at the moment it runs, posted or after the user's Run; Offered, MenuPath and the command-to-items table go.
- Checked by: tests/scripts/mcp (02 runs, 03 asks first); tests/scripts/axDown/14-recent-from-the-palette.txt
- Commit: af0fac3

### R-12, medium, framework: A run for an AI tool could still execute after the client was told it timed out

- Where: src/AxKit.Forms/McpChannel.cs
- Principle: P4
- Fix: Run gives up as App does: a flag both threads swap, so a request not taken up in time never runs.
- Checked by: reading against App(); tests/scripts/mcp
- Commit: af0fac3
- Note: In OnWindow itself, so every request of the pipe has it; a timeout now answers in words the client can act on.

### R-14, medium, framework: A notice shown over another replaces it and the replaced notice's cancel never runs

- Where: src/AxKit.Forms/AxKitForm.Notices.cs; src/AxKit.Forms/HotkeyForm.cs
- Principle: P4
- Fix: ShowNotice runs the cancel of the notice it replaces, unless it is chained from that notice's own button.
- Checked by: a script: the Hotkey notice replaced by an error, the hotkey then hides the window
- Commit: af0fac3
- Note: Fixed where it bit, not in ShowNotice: running a replaced notice's cancel in general would break the notices that are replaced on purpose and come back (Find inside a notice). The hotkey capture now asks which notice is on view instead of trusting a flag. The rule for new code: state that outlives a notice is read from the notice showing, never from a flag its buttons clear.

### R-18, medium, changes: With Dismiss surveys on, a turn that ended in a survey lost its Done, its chime and its flash

- Where: src/AxClaude/MainForm.Area.cs; src/AxClaude/MainForm.Model.cs
- Principle: P3
- Fix: A survey the app dismisses leaves the bell pending, so Done is said once the survey has gone; the key is sent only while no write of the user's is on its way.
- Checked by: tests/AxClaude.Tests (the survey fixture through the window's rule) and reading; by ear when a survey comes
- Commit: af0fac3
- Note: From e71f67d (2026-11-12). Done is said as the survey arrives, shown or dismissed.

### R-19, medium, changes: A survey arriving within the speech hold dropped the end of the reply unspoken

- Where: src/AxClaude/MainForm.Area.cs; src/AxClaude/MainForm.Model.cs
- Principle: P3
- Fix: A survey is not Claude's question about the turn: reply lines held when it arrives are spoken.
- Checked by: reading; by ear when a survey comes
- Commit: af0fac3
- Note: Suspected from the timing; the fix is safe either way.

### R-21, medium, core: A row rewritten and scrolled away within one frame kept its hidden flags and kind

- Where: src/AxKit.Core/Vt/Screen.cs; src/AxClaude.Core/Transcript/SessionModel.cs
- Principle: P5
- Fix: The screen hands the new text to the store's TextChanged before it commits the line, as it does for a row that stays.
- Checked by: tests/AxClaude.Tests fixtures unchanged, and a new test of a row rewritten and scrolled in one frame
- Commit: af0fac3
- Note: The mechanism is verified; whether Claude Code produces that frame is not. Every fixture's expected transcript is unchanged by it.

### R-22, medium, core: Only the settings were written through a temporary file; documents, items, the theme and the key were written in place

- Where: src/AxKit.Core/TextDocument.cs; src/AxKit.Core/Theme.cs; src/AxKit.Core/Reports/ReportFiles.cs; src/AxKit.Core/Mcp/McpAccess.cs; src/AxKit.Core/SettingsFile.cs
- Principle: P6
- Fix: One SafeFile.Write (a temporary file of its own name beside the target, then replaced in one step, the target's attributes kept) used by all five; the settings' fixed .tmp name, which two windows shared, goes.
- Checked by: tests/AxKit.Tests/SafeFileTests; the editors' and axTask's scripts
- Commit: af0fac3
- Note: Where the swap is refused (another program holds the file open for writing but not for replacing) the file is written in place as before: never stricter than it was. axClaude's git exclude file and axTask's GitHub token too.

### R-23, medium, core: The update signature covers the zip alone, not its version: an older signed release could be offered as a newer one

- Where: src/AxKit.Core/Updates/ReleaseSigning.cs; src/AxKit.Core/Updates/Updater.cs; tools/sign-release.ps1; install.ps1
- Principle: P1
- Fix: The updater refuses a zip whose own version (read from the AxKit.exe inside it, after the signature holds) is not the version the release names and not newer than the one running.
- Checked by: tests/AxKit.Tests/UpdaterTests with a zip of an older version
- Commit: 36c603c
- Note: Needs no change to the signature format, so earlier releases still verify. Threat: the GitHub account taken over; the signing key stays offline. Updater.VersionRefusal, asked after the signature holds and the zip is extracted; UpdaterTests.

### R-25, medium, core: axMCP starts a server by its configured command name without resolving it

- Where: src/AxMCP.Core/StdioServer.cs
- Principle: P1
- Fix: A bare name is found through SystemPrograms.OnPath (full PATH folders only, .exe and .cmd); not found is said with the name.
- Checked by: tests/AxMCP.Tests
- Commit: af0fac3
- Note: A batch program with an argument cmd.exe would act on is refused with the reason.

### R-31, medium, tooling: CI runs no window script, and no document said so

- Where: docs/axKit/automation.md; tools/workflows.ps1
- Principle: P8
- Fix: automation.md and the workflows page say that the window scripts run on the developer's computer (nvda-pass.ps1 and tools/AxAutomate), never in CI, and what CI does prove.
- Checked by: reading
- Commit: af0fac3
- Note: Running the model-free scripts in CI is a later question: whether the runner's own desktop works on a hosted Windows runner is not known.

### R-34, medium, tooling: A window whose thread hung blocked a run forever, in the NVDA tier with the keyboard taken

- Where: src/AxAutomate.Core/Window.cs
- Principle: P4
- Fix: A read of the window's pipe waits 30 seconds at most and then fails the step, naming the window.
- Checked by: tests/AxAutomate.Tests
- Commit: af0fac3

### R-35, medium, tooling: Scripts that depend on a live model or the network read like any other when they fail

- Where: tests/scripts/axClaude; src/AxAutomate.Core/ScriptReader.cs; tools/workflows.ps1
- Principle: P3
- Fix: A script says what it needs in its header (# Needs: Claude Code, the network, KeePassXC); the runner's summary and the workflows page give those apart, so a model's change of wording is not taken for a regression.
- Checked by: tests/AxAutomate.Tests; the summary of a run
- Commit: af0fac3
- Note: 257 fixed waits in 56 scripts stand in for conditions; each is replaced by an expectation when its script is next touched. Done: the # Needs: line on 29 scripts and on the workflows page. Open as part of R-37: the runner's summary giving such scripts apart, and the fixed waits.

### R-36, medium, tooling: Two scripts name KeePassXC by one computer's path, and one overwrites the user's clipboard

- Where: tests/scripts/axPass; tests/scripts/axPreview/04-details-and-kinds.txt; docs/axKit/automation.md
- Principle: P1
- Fix: The path comes from where the tool is found; the clipboard step saves and puts back what was there, or goes.
- Checked by: the scripts pass
- Commit: af0fac3
- Note: KeePassXC's place is stated as the scripts' need; the two scripts that write the clipboard on purpose are named in automation.md.

### R-37, medium, tooling: Tests that read source text can be passed by ordinary code they do not see

- Where: tests/AxKit.Tests/CommandMarkTests.cs; tests/AxKit.Tests/ListTests.cs
- Principle: P7
- Fix: The runner's check commands step, which every app's first script runs on the built window, also fails on a menu entry with no stated mark once R-11 is done; the text tests stay as lint and say so.
- Checked by: check commands in each app's script
- Commit: 5e56218
- Note: AxKitForm.CommandProblems fails on any entry of the built window that states no mark and is not withheld; the first full run found six items the source test could not see (Exit, Check for updates, Update to, Record raw stream, Hotkey, two empty-list lines). The source tests say they are lint.

### R-39, medium, apps: axPass and axTask dropped an undo step before applying it: a failed undo lost it

- Where: src/AxPass/PassForm.Commands.cs; src/AxTask/TaskForm.Commands.cs
- Principle: P6
- Fix: The step is looked at, applied, and removed only when the undo succeeded.
- Checked by: tests/AxTask.Tests; reading for axPass
- Commit: af0fac3
- Note: axPass fixed. axTask left as it is, on purpose: a step that can no longer be undone (its files changed since) is dropped so that the steps before it can still be reached, and says why.

### R-45, medium, docs: What a reader must hold to change one app is about 90,000 tokens; the root instructions alone are 31 KB of three paragraphs

- Where: CLAUDE.md; src/AxKit.Forms/CLAUDE.md; src/AxKit.Core/CLAUDE.md; docs/axKit/SPEC.md
- Principle: P8
- Fix: Root CLAUDE.md is the rules, the gotchas and pointers, 8 KB at most, the commands on a page of their own; the AxKit.Forms notes split by kind of window; reference text loses its dates and until-clauses.
- Checked by: a size check in tools/docs-check.ps1
- Commit: 5e56218
- Note: The root file is loaded into every session: it is rewritten in one commit of its own and read by the maintainer. Done 2026-11-13: the root CLAUDE.md from 32 KB to 14 KB, its long paragraphs moved whole into docs/axKit/commands.md, documents.md and the generated apps.md; the rules, the gotchas and the manual verification unchanged. Open: src/AxKit.Forms/CLAUDE.md (53 KB) split by kind of window, and the dates and until-clauses out of the reference text of the specs. 2026-11-13: src/AxKit.Forms/CLAUDE.md from 54 KB to a 3 KB index over src/AxKit.Forms/notes, one file per kind of window (NotesTests reads them). Open: the dates and until-clauses out of the reference text of the specs; the size check. NotesTests holds the sizes (the root at most 20 KB, any notes file 32 KB). Declined: taking the dates and until-clauses out of the specifications. They record when a decision was made and by whom, which is what a reader needs when a rule is questioned; the load problem this finding named is met by the root file's 15 KB and the split notes.

### R-47, medium, docs: PRIVACY.md and rule 8 name two places where axKit's own code reaches the network; axTask's GitHub lists are a third

- Where: PRIVACY.md; CLAUDE.md; README.md
- Principle: P8
- Fix: The three places stated in the same terms everywhere: the updater, the report sender, and axTask's GitHub lists with the user's own token.
- Checked by: reading against docs/axKit/outside-calls.md
- Commit: af0fac3

### R-04, low, security: Settings whose effect is more than a setting were marked change

- Where: src/AxKit.Forms/HotkeyForm.cs; src/AxKit.Forms/AxKitForm.cs; src/AxClaude/MainForm.Ui.cs
- Principle: P1
- Fix: Toggle takes a mark; Start with Windows, Check for updates at start, Review answers before they go to Claude and Keep recordings out of git are act.
- Checked by: tests/AxKit.Tests/CommandMarkTests pins them
- Commit: af0fac3

### R-05, low, security: Your reports was marked open but sends unsent reports to GitHub first

- Where: src/AxKit.Forms/AxKitForm.cs
- Principle: P1
- Fix: Marked act.
- Checked by: tests/AxKit.Tests/CommandMarkTests pins it
- Commit: af0fac3

### R-06, low, security: The AI tools' log holds refused paths and no name of what was run

- Where: src/AxKit.Forms/McpBroker.cs; src/AxKit.Core/Mcp/McpAudit.cs
- Principle: P3
- Fix: A refusal is logged by its class, not its text; a run logs the label it matched.
- Checked by: tests/AxKit.Tests McpAudit tests; tests/scripts/mcp reads the log
- Commit: af0fac3
- Note: A run's line names the command, without the path a recent file's entry carries; a refusal's first line goes through Redaction.

### R-07, low, security: A hand-over's text reaches a window from any program of the user without a word

- Where: src/AxKit.Forms/AppMessage.cs; src/AxClaude/MainForm.cs
- Principle: P3
- Fix: The window says what arrived (Text added to the message, from another window).
- Checked by: tests/scripts/axHelp/01-start.txt hears it
- Commit: ddd8dcb
- Note: Said: Text added to your message from another window, not sent. tests/scripts/axHelp/01-start.txt hears it.

### R-10, low, framework: Reading the entries rebuilt a menu the user had open, and listed placeholders that do nothing

- Where: src/AxKit.Forms/CommandPalette.cs; src/AxKit.Forms/RecentMenu.cs
- Principle: P5
- Fix: A menu whose list is showing is read as it stands; the placeholder of an empty list is left out of the entries.
- Checked by: tests/AxKit.Tests/CommandPaletteTests
- Commit: af0fac3

### R-16, low, framework: Dead code and stale notes in the command layer

- Where: src/AxKit.Forms/AxKitForm.cs; src/AxKit.Forms/CommandPalette.cs; src/AxKit.Forms/Command.cs; src/AxKit.Forms/CLAUDE.md
- Principle: P8
- Fix: Commands, the command-to-items table and MenuPath removed with R-08; the comments corrected.
- Checked by: the Release build (warnings are errors)
- Commit: af0fac3

### R-17, low, framework: Timers are never disposed and one touches the handle without a disposed check

- Where: src/AxKit.Forms/AxKitForm.cs; src/AxClaude/MainForm.Input.cs; src/AxTask/TaskForm.Projects.cs
- Principle: P4
- Fix: One After(ms, action) in the base with the disposed check, used by the three one-shot timers.
- Checked by: the Release build; tests/scripts of axClaude and axTask
- Commit: ddd8dcb
- Note: Harmless today: one window per process. AxKitForm.After; the menu timer of OpenBarMenu keeps its own, since it repeats.

### R-20, low, changes: The silent words tested each screen row alone, so a wrapped line was half spoken

- Where: src/AxClaude/MainForm.Model.cs; src/AxClaude.Core/Transcript/SpeechFilter.cs
- Principle: P5
- Fix: The filter works on whole lines: a row and the rows joined to it are tested and silenced together (SpeechFilter.Spoken).
- Checked by: tests/AxClaude.Tests/SpeechFilterTests
- Commit: af0fac3

### R-26, low, core: A program that exits before reading its input made the run throw instead of giving its error text

- Where: src/AxKit.Core/ProcessRun.cs
- Principle: P3
- Fix: A broken pipe while writing the input is not an error of the run: the outputs and the exit code are returned.
- Checked by: tests/AxKit.Tests/ProcessRunTests
- Commit: af0fac3

### R-27, low, core: A run of combining characters grew one screen cell without limit

- Where: src/AxKit.Core/Vt/Screen.cs
- Principle: P4
- Fix: A cell takes at most eight combining characters.
- Checked by: tests/AxKit.Tests/VtScreenTests
- Commit: af0fac3

### R-29, low, core: Smaller core items: PtyHost.Kill after Dispose, JsonDocument not disposed, a report posted before it is marked sent, the seal's unescaped separators, AppPaths' uneven test-run home

- Where: src/AxKit.Core/Pty/PtyHost.cs; src/AxKit.Core/Reports/GitHubIssues.cs; src/AxKit.Core/Reports/ReportSender.cs; src/AxKit.Core/Mcp/McpAccess.cs; src/AxKit.Core/AppPaths.cs
- Principle: P4
- Fix: Each fixed where it stands.
- Checked by: the unit tests of each piece
- Commit: 5e56218
- Note: PtyHost.Kill ends the process under the lock Dispose takes; GitHubIssues disposes its JsonDocument; ReportSender never marks a posted report as not sent when its item cannot be written (it stops and says so); McpAccess.SealOf reads each field with control characters replaced, and Pair cleans the name (McpTests); AppPaths.UpdatesFolder is under a test run's own folder.

### R-38, low, tooling: Hygiene: release.ps1 can wait on CI forever; empty AxList folders; merged branches; two forgotten worktrees; tools/EdgeDrive in no solution

- Where: release.ps1; src/AxList; tools/EdgeDrive
- Principle: P4
- Fix: A time limit on the wait; the empty folders removed; the branches and worktrees listed for the maintainer, who deletes branches himself.
- Checked by: reading
- Commit: ddd8dcb
- Note: Done: the empty AxList folders and two stale worktrees removed (both clean or holding only a generated page). The seventeen merged branches deleted by the maintainer on 2026-11-13, here and on GitHub. Open: the time limit on release.ps1's wait; the two unmerged branches cleanup and night-check, to be read and merged or dropped; tools/EdgeDrive is in no solution. 2026-11-13: release.ps1 asks for the build every 20 s, 40 minutes at most. The branches cleanup and night-check hold nothing main lacks (four commits already in main by content, one accidental commit of the lists hook, one merge of two merged branches): the maintainer deletes them with git branch -D. tools/EdgeDrive stays outside the solution by choice: a maintainer tool for Partner Center, built on its own.

### R-42, low, apps: The same small piece written two or three times: one instance of a hotkey app, the busy guard and its words, the item apps' undo stack

- Where: src/AxStart; src/AxClip; src/AxHelp; src/AxGit/GitForm.Run.cs; src/AxProcess/ProcessForm.cs; src/AxTask; src/AxPass; src/AxMCP
- Principle: P2
- Fix: AppHost.OneInstance; one Busy helper in the base with one wording; the undo stack in ItemForm.
- Checked by: the apps' scripts
- Commit: 5e56218
- Note: With R-11, app by app. AppHost.FirstInstance replaces the three copies of the one-instance mutex. Left as they are, by choice: the busy guards of axGIT, axProcess and axPass hold different things (a verb for the status bar, a phrase in the list's name, a flag) in about ten lines each, and the item apps' undo stacks hold each app's own kind of step; one shared piece would need the generics rule B-10 rules out.

### R-48, low, docs: The Unreleased changelog is 17 KB with entries that later ones replaced and with internals

- Where: CHANGELOG.md
- Principle: P8
- Fix: Replaced entries merged, each entry a user's sentence or two; internals under For developers.
- Checked by: reading
- Commit: ddd8dcb
- Note: 19 KB to 12 KB: replaced entries merged, the AI tools together, internals in one For developers entry.

### R-49, low, docs: Stale names in the notes and a broken bullet in AxKit.Core's

- Where: src/AxKit.Forms/CLAUDE.md; src/AxFile/CLAUDE.md; src/AxKit.Core/CLAUDE.md; docs/axKit/adding-an-app.md
- Principle: P8
- Fix: Corrected; a test pins every backticked identifier of the notes against the sources.
- Checked by: tests/AxKit.Tests/NotesTests
- Commit: af0fac3
- Note: Done: the names and the broken bullet corrected (R-43). Open: the test that pins the notes' identifiers against the sources. tests/AxKit.Tests/NotesTests pins every name the notes give in backticks against the sources, with a short list of names that are Windows' or .NET's.

## Deferred

### R-28, high, core: What axClaude knows of Claude Code's screen is English text spread over seven files

- Where: src/AxClaude.Core/Transcript/LineClassifier.cs; src/AxClaude.Core/Transcript/SessionModel.cs; src/AxClaude.Core/Transcript/QuestionReader.cs; src/AxClaude.Core/Transcript/SurveyReader.cs; src/AxClaude.Core/Transcript/ScreenReader.cs
- Principle: P2
- Fix: Every text and pattern of Claude Code's moves into one table (ClaudeText), each with the Claude Code version it was last seen in and the fixture that shows it; a test fails on such a literal elsewhere in the project; tools/claude-text.ps1 writes the page that is compared when a new Claude Code version comes.
- Checked by: the fixtures unchanged; the new test
- Note: The largest risk to axClaude over years, since every Claude Code release can change a text. A session of its own, with the full axClaude scripts before and after.

### R-15, medium, framework: AxKitForm is one class of about 3,400 lines in twelve files that every app inherits whole

- Where: src/AxKit.Forms/AxKitForm*.cs
- Principle: P8
- Fix: The services with their own state move out into plain classes the base holds (the updater flow, the installer, the report sender, AI help, the AI tools' window side), one per commit, the base keeping the hooks; no behaviour changes.
- Checked by: every tests/scripts folder after each move
- Note: After R-11: moving code while the command layer changes would double the risk. Not a defect, a cost of every later change.

### R-24, medium, core: cmd.exe's special characters are checked in two callers that start a .cmd and missed in two

- Where: src/AxKit.Core/Ai/AiRequest.cs; src/AxClaude.Core/Pty/ClaudeLauncher.cs
- Principle: P1
- Fix: One check in the one place that builds a command line for claude.cmd; a folder or argument with such a character is refused with the reason.
- Checked by: tests for both callers with a folder named R&D
- Note: Only when Claude Code is the npm install (claude.cmd); the native claude.exe is not affected. Deferred: the launch paths throw nothing today, and their callers must be read before a refusal is added. Plan: SystemPrograms gets the operators that matter in a quoted argument (% and ! alone) and in an unquoted one (& | < > ^); ClaudeLauncher quotes an argument holding the second set when the program is a .cmd and refuses the first; AiRequest the same for its folders.

## Declined

### R-13, medium, framework: Menu refreshes read files on the window's thread at every palette opening and AI call

- Where: src/AxKit.Forms/AxKitForm.Mcp.cs; src/AxKit.Forms/AxKitForm.Ai.cs; src/AxKit.Forms/WindowMenu.cs
- Principle: P4
- Fix: The switches' states are read once per reading of the entries and cached for a second; the Window menu's lists likewise.
- Checked by: a count of file reads per Entries call in CommandPaletteTests
- Note: The reads are of small files in the user's own settings folder, a few milliseconds, as every opening of these menus always did; rule 3 is about I/O that can block (a network, a program). Counted: three files per reading of the entries, at most ten readings a second from an AI tool (the rate limit). A cache would add stale checks to save nothing a user can perceive.

### R-40, medium, apps: Windows ask whether their last folder exists on the window's thread, without the local-drive guard

- Where: src/AxFile/FileForm.Browse.cs; src/AxGit/GitForm.cs; src/AxClaude/MainForm.cs; src/AxTerminal/TerminalForm.cs; src/AxPass/PassForm.cs; src/AxMedia/MediaForm.cs
- Principle: P4
- Fix: One LocalPaths.ExistingOr(last, fallback) for the start-up folder, asking of a local path only.
- Checked by: tests/AxKit.Tests/LocalPathsTests
- Note: A last folder on an unreachable share would stall the start. Kept as it is, on purpose. The guard that fits a menu (list a network path without asking) is wrong for a start: skipping a remembered folder because it is on a network drive would cost everyone who works on one their folder at every start, to spare a wait that only comes when the share cannot be reached and that Windows bounds. The right fix, if it is ever reported, is the start-up check off the window's thread, one app at a time.

### R-41, medium, apps: axPass reads an entry in an async void outside Do

- Where: src/AxPass/PassForm.cs
- Principle: P3
- Fix: Through Do, so that any failure is a notice.
- Checked by: tests/scripts/axPass
- Note: The read is a background convenience: its failures are logged, not shown, on purpose, and an exception outside the filter reaches the crash handler, which shows it as a notice in the window. A notice for every failed read would interrupt the user for nothing they asked for.

### R-46, medium, docs: plan.md mixes built and unbuilt work; dated reviews stand among the reference documents

- Where: docs/axKit/plan.md; docs/axKit/review-2026-10-23.md; docs/axKit/inventory-2026-10-24.md; docs/axKit/inventory-2026-11-09.md; docs/axKit/cleanup-run.md
- Principle: P8
- Fix: plan.md keeps the unbuilt apps only; the dated documents move to docs/axKit/archive once their open lines are in the priorities list.
- Checked by: reading
- Note: plan.md now marks what was built (R-43). The dated documents stay where they are: code comments, specs and the priorities list point at their paths in dozens of places, and moving them would break more than it tidies. docs/axKit/documents.md lists them apart as dated records.

### R-03, low, security: The pairing Deny cool-down and the rate limit live in the broker, which a client can start again

- Where: src/AxKit.Forms/McpBroker.cs; src/AxKit.Forms/AxKitForm.Mcp.cs
- Principle: P4
- Fix: The window keeps the denied-until time by client program, as it does for a denied run.
- Checked by: tests/scripts/mcp/01-read-and-pair.txt, a second broker after Deny
- Note: Declined 2026-11-13: that a new session of a client is asked again is the design (MC-6.4, held by tests/scripts/mcp/01-read-and-pair.txt): a user who denied by mistake restarts the client to be asked. The question is unprompted with Deny as default and a moment in which keys do nothing, so a client that restarts its broker gains a repeated question, never an answer.
