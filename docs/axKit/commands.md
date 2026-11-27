# Commands and scripts

Every command and script of the repository, by what it is for. The root `CLAUDE.md` names the few used every day and
points here. Each app's own commands are in its `CLAUDE.md`.

## Build

- `dotnet build AxKit.sln`. `Directory.Build.rsp` makes every build's MSBuild nodes exit with the build, and the
  scripts run `dotnet build-server shutdown` after theirs. After a bare `dotnet build` or `dotnet test`, run it
  yourself: the development computer has 7 GB, and a build otherwise leaves about 1 GB of idle build servers behind for
  ten to fifteen minutes (`development-environment.md` section 5).
- To check a change while axKit runs, `dotnet build <project> -o <scratch folder>`. A build of `src/AxKit` over an
  axKit that runs from the output folder stops in one line, before anything is compiled or copied (the target
  `StopOverRunningAxKit` in `src/AxKit/AxKit.csproj`).

## Test

- `test.ps1 [<App>]` first runs every quick check at once, `tools/pages.ps1 -Check` (the generated pages, the menu
  paths the documents name, the record of the outside programs, the scripts; not with an app named), then every test
  project in one `dotnet test` over a solution filter of the test projects, written for the run, or one app's
  project, in Release; no test project references `src/AxKit`, so a running axKit's `bin` is never touched. After
  the run it counts the tests' own folders in the temporary folder (the table `$testFolderPrefixes`; every
  `TempFolder` starts with `axkit-test-`) and fails when there are more than before: a test that leaves its folder
  shows at once (2026-11-27, L7). It is what `release.ps1` and `run.ps1 -Test`
  call. Run it before a commit. The workflows `build.yml` and `release.yml` run `dotnet test AxKit.sln`, the solution,
  in Release, and `build.yml` also builds the Store package (`publish.ps1 -Msix`); `test.ps1` does neither.
  Never `dotnet test AxKit.sln` while axKit runs: it builds into the running program's `bin`. Debug alone misses
  what Release treats as errors, which cost 3.3.0. On the computer that has the management lists beside the
  repository, it first refuses a clone whose hooks are not installed and names the command
  (`git config core.hooksPath tools/hooks`).
- `tools/script-lint.ps1 -Check`, run by `test.ps1` and `build.yml`: every `.ps1` is ASCII, parses, and assigns no
  local variable named like one of its parameters, and a script that takes `-Check` (the page scripts) sorts with
  `Sort-Ordinal` of `tools/ordinal.ps1`, never with `Sort-Object`, whose order differs between Windows PowerShell and
  PowerShell 7; its header says what each rule cost.
- The rules of the bundle that the tests hold, read off the sources in `tests/AxKit.Tests`: `LayoutTests` (references
  point one way), `ListTests` (every list is `AxKitList`), `SecondWindowTests` (no message box, dialog or bare form
  outside its list of files), `StackTests` (no package, WPF or web view outside the tests), `KeyTests` (no key
  collides, and its tables name exactly the apps of `Dispatch.Apps`), `InventoryTests` (every app in every place),
  `NotesTests` (the notes name what exists and stay small), `PrivacyTests` (no e-mail address, account path, GitHub
  token or private key in any file, `.claude` included), `HookTests` and `ReleaseScriptTests` (the hooks' and the
  release scripts' shape), `ChangelogTests` (every bullet of `## Unreleased` names its apps as the release reads them), `PlainCodeTests` (no interface but Windows' COM ones, no window code in a Core project, UI Automation notifications raised by `Announcer` alone).
- `tools/AxAutomate <script or folder> (--build | --exe <AxKit.exe>)` plays the automated window checks of
  `tests/scripts` on a desktop of their own (`automation.md`). The run of an app's folder after a change to a window is
  given `--record`: `dotnet run --project tools\AxAutomate -c Release -- tests\scripts\<app> --build --record` enters
  each goal's result into `docs/axKit/workflow-record.json`, which is committed with the change. It holds the windows' commands against the command
  inventory, `docs/axKit/window-commands.json`, and says after its summary when a window lists a command the file does
  not hold as it is (exit code 3 when every script passed). `--inventory write` merges what the windows list into the
  file, `rewrite` also drops what a whole app no longer lists, `off` leaves it alone; `--repository <folder>` names
  the repository when the program is not built inside one (`automation.md`, "The command inventory"). It also holds
  the trace of every section a script marks `# Stable` against the kept one in `tests/traces`, and a trace that
  differs or is missing fails the script; `--traces write` writes a script's kept file (read it whole before the
  commit), `--traces off` neither compares nor writes (`automation.md`, "The traces"). Exit codes: 0
  passed, 1 a script failed, 2 the run could not start, 3 the inventory is stale. Its other form,
  `tools/AxAutomate --reference [--check]`, plays nothing: it writes the workflow pages, or checks them, for
  `tools/workflows.ps1` (below). A third form is never typed by hand: `AxAutomate standin <scenario.json> <Claude
  Code's arguments>` is the stand-in of Claude Code, which a script's `claude.cmd` calls through `AXKIT_STANDIN` to
  play a scenario of frames, marks, hook events and session file entries with no Claude Code, no network and no
  model, and `AxAutomate standin --prepare <scenario.json> <folder>` writes a session's earlier turns before a start
  (`tools/AxAutomate/StandIn.cs` has the scenario's keys; `automation.md` section 2, "The stand-in of Claude Code").
  `tools/QaRun` runs Claude sessions without windows, always with `--model haiku`.
- `tools/window-passes.ps1 [-Check]` says which apps' window code has a commit newer than the app's last recorded
  pass on the desktop tier or with NVDA listening (`workflow-record.json`), and, where an app's newest pass is
  current, which goals of its scripts are behind all the same: a goal whose last result on either tier is not
  "passed", a goal with no pass since the window's newest change, a script with no goal in the record. Each such
  goal is said in the app's line, script by script; the goals of a live script (its `# Needs:` line names Claude Code
  signed in, the network or GitHub) are said apart, after "Live, run with -Live", since a plain run never plays
  them: they are run with `tools/run-scripts.ps1 <scripts> -Live -Record`. `release.ps1` and its rehearsal run it with `-Check` and refuse
  while it names an app; `test.ps1` does not run it, since during ordinary work the windows are ahead of their
  record. It reads and asks git; it writes nothing. `-Run` does what its lines ask for: AxKit built once into a
  scratch folder, then the scripts folder of each app it names run on the hidden desktop with `--record`, one line per
  app (passed, or the failing scripts), last what is still named; `-Run -Only <app>, ...` runs the named apps, changed
  or not. A live script is never run by it: such an app is run script by script without them and its line says how
  many were left out, and an app named for its live scripts alone is not run at all. It refuses while `src` or
  `tests` hold a change not committed, since a pass recorded over such work counts as changed again with the commit.
  The record it changes stays in the working tree, to be committed. Exit codes: 0 passed, 1 a script failed, 2
  refused or the build failed. `tools/window-run.ps1` holds the functions it shares with the next.
- `tools/nightly.ps1`, for the maintainer to schedule (its header has the one `schtasks` line; the script makes no
  task): the same run over every app, changed or not, with the same scripts left out. It writes a short dated report
  to `%LOCALAPPDATA%\AxKit\logs\nightly.txt` (each app passed, or its failing scripts; no path of the computer) and
  leaves the record's changes in the working tree only when the tree was clean before and every script passed;
  otherwise it puts that one file back as it was, so a working session never finds changes it did not make. It never
  commits or pushes.
- `nvda-pass.ps1 [<app>]` runs the same scripts with NVDA listening, plus `tests/nvda-scripts`: the NVDA test plans as
  the assistant runs them. It takes the keyboard, so only after the user has stepped away: it says so in Windows' voice from its
  start, counts from ten to one aloud before it takes the keyboard, and does not start when a key or the mouse is
  used during that countdown (`-WarningOnly` plays the warning alone), when NVDA is not running, or
  when the installed capture add-on differs from `tools/nvda-capture`. `-Skip <app>, ...` leaves apps out and
  `-NoBuild` runs the last build again. `-Only <pattern>, ...` plays only the scripts whose name, or folder and
  name, match (`-Only '*-stream-*', 'axMedia\03-playing.txt'`): what a pass failed is heard again after its repair
  without the whole pass. `-Inventory check` or `off` gives the runner's `--inventory` mode; `-Inventory write` and
  `rewrite` are refused with words, since they also crawl the workflow graph, which must never run on the user's own
  desktop: the inventory is written on the hidden desktop, live scripts included
  (`tools\run-scripts.ps1 tests\scripts\axHelp -Live -Inventory write`, then `tools\workflows.ps1`). The spoken
  warning and the countdown are `tools/spoken-countdown.ps1`, which `tools/store-screenshots.ps1` shares; the runner
  itself (`tools/AxAutomate --nvda`, typed by hand) has no countdown and takes the keyboard at once, so it is started
  through this script. The build servers end with its two builds, and the owed page is written after the pass
  without its trouble changing the pass's exit code.
- `tools/upstream.ps1` is the upstream watch (`automation.md`, "When an outside program changes"): for each outside
  program (Claude Code, Git, KeePassXC's tool, the shells) the installed version beside the one the checks last passed
  with, from `upstream.json`, and "changed" where they differ; exit code 1 when one did. Quick, no build: run it at
  the start of a session. `-Verify [<program>]` runs the checks of every changed program (or the one named) and
  writes the new version and the date into `upstream.json` where they pass; exit code 1 when a check failed, 2 when
  the checks could not run (not installed, Claude Code signed out, no network, no build). `-Check` only checks the
  file's shape, for `test.ps1` and `build.yml`. `-File <path>` reads and writes another file.
- `manual_tests/prepare.ps1 [<app>]` makes and resets the files of the NVDA test passes in `manual_tests/<app>`, spoken
  WAV files and a scratch git repository among them. Git ignores all but the script and the `_main.md` files, so a
  file a test pass needs is added to the script, never committed.

## Run

- `run.ps1 [-App <verb>] [<path>]` builds `src/AxKit` and starts one app, axClaude on a folder by default (`-New`,
  `-NoBuild`, `-Test`). The verbs are the apps' commands (`apps.md`). It also keeps the three background apps
  running from the same build: axStart, axClip and axHelp are ended before the build, since they hold its folder, and
  each that is not running is started hidden afterwards, whether it ran before or not, so that the hotkeys work and
  run the code just built. Before a build it also ends axHelp's conversation when that runs on hidden, with Claude
  Code and the AI tools' broker under it, and a broker left behind by a Claude Code that has ended: each holds the
  build folder unseen (2026-11-15). If a window of this folder's axKit is still open then, it names it in one line and
  stops, in place of the compiler's errors about files in use.
- `scripts to run-individual-apps\run-ax<Name>.ps1` is the same with `-App` filled in, one script per app.
- `AxKit.exe <verb> ...`, or a folder or a file alone, picks the app.

## The generated pages

Each is written by its script from its sources and checked with `-Check`. The checks have one list,
`tools/pages.ps1 -Check`, and `test.ps1`, the push check and `build.yml` all run that one command (until 2026-11-27
`build.yml` ran each check as a step of its own, and lacked the two the list had gained, the map and the bounds of
the workflow graph). Never edit a page by hand: `tools/pages.ps1` writes every one of them in one run (each page's
own script writes it alone, and each ends with an exit code of its own, so that one that fails is the one named),
and the pages it wrote are committed.

- `tools/outside-calls.ps1` writes `outside-calls.md`: everything axKit reaches outside its process, from the
  `Outside call, <kind>, <what is reached>:` comment that every network request, program start and pipe in `src`
  carries. The check fails on a call without one. Run it before a commit that touches such a call (SPEC B-17).
- `tools/workflows.ps1` writes the workflow pages (`user-workflows.md` WF-7) from the scripts of `tests/scripts` and
  `tests/nvda-scripts` and the command inventory `window-commands.json`: `workflows.md`, the index, by app every
  workflow's label and under it its goals; `workflows/<app>.md`, each goal's steps with their keys and what is heard;
  `window-commands.md`, every window's commands. The generator is C#, run as `tools/AxAutomate --reference`; it
  builds the runner and plays nothing. A new script gets its label (axAutomate SPEC AU-5.2a). After a run with
  `--inventory write`, or a change to a script, run it and commit the pages with that change (WF-11.4).
- `tools/repairs.ps1` writes `repairs.md` from `repairs.json`: what an audit found and what became of each finding,
  with the principles the repairs follow. `-Open` prints what is open. A defect found is entered there, and its status
  changed in the same commit as its fix. Every run holds the list to its fields: a done finding says how it was
  checked, a deferred or declined one why, and a commit that is named exists in the repository; a done finding that
  is still owed a hearing or a live check says so in `owed`, each entry with its kind in front, for `tools/owed.ps1`.
- `tools/apps-page.ps1` writes `apps.md`: the apps from `Dispatch.Apps`, each with its command and its folders.
- `tools/doc-menus.ps1 -Check` reads every menu path the documents name ("Alt+K, Apps", "the Window menu's Open windows") and fails on one the windows do not have, the names from the command inventory and the window base's code.
- `tools/guide-menus.ps1` writes the Menus section of each app's user guide, between its two marker lines, from `window-commands.json`: every menu with its commands and keys, so that no guide lists them by hand.
- `tools/apps.ps1` is the reader of `Dispatch.Apps` the other scripts share.
- `tools/components.ps1` writes `components.md` from `components.json`: the window types, their components and
  which app is on which.
- `tools/map.ps1` writes `map.md` from `workflow-graph.json` (`workflow-graph.md` WG-2.5): by app each waypoint and
  place, how it is reached and what can be done there. Run it after an inventory run with `--inventory write` has
  written the graph; `-Check` says whether the page is current.
- `tools/graph-check.ps1` writes `over-the-bounds.md` and the baseline `goal-counts.json` (`workflow-graph.md` WG-3.3,
  WG-3.4) through `AxAutomate --bounds`: every goal of `tests/scripts` counted, the waypoint bound over
  `workflow-graph.json`. Without a switch a goal that grew keeps its old count in the baseline; `-Check` fails on a
  stale page or baseline and names every goal longer than the baseline holds; `-Write` takes those in, only at the
  maintainer's word. `tools/pages.ps1` runs it, and `test.ps1` its check.
- `tools/cases.ps1` writes `cases.md` from `cases.json`, the table of how each unexpected situation is handled (SPEC
  AX-5.10). A change to how a situation is handled changes its row, then the script is run; the fields are described
  at the top of the script.
- `tools/key-map.ps1` writes `key-map.md` (every key, by key) and `key-groups.md` (the same keys by group and context, the page a user learns the patterns from) from the command inventory `window-commands.json`, `BundleKeys.cs` and the
  proposed `key-families.json`: every key, its scope and what it does in each app. Every run holds the families file
  to the inventory: a key named there as not fitting is a command the inventory has. `tools/workflows.ps1` runs it after
  its own pages, so writing the pages after an inventory change writes the key map too; alone it is run after a
  change to `BundleKeys.cs` or the families file.
- `tools/families.ps1` writes `families.md` from `families.json`: the four families (what starts something, the
  functions, the events, the sounds) and the links between them. Every run holds the file to the code: a new sound in
  `Sounds.cs`, a new file that plays one, a new menu in the inventory or a new family of keys fails until the file
  names it. It is run after a change to `families.json`, to `Sounds.cs` or to where a sound is played;
  `tools/workflows.ps1` runs it after an inventory change.
- `tools/owed.ps1` writes `owed.md`: everything tested in code and not yet heard or tried for real, by kind (a
  hearing with NVDA, a live check with a real outside program or situation, a window check on the hidden desktop, a
  test in code), from the rows of `cases.json` that carry `owed`, the `owed` entries of `repairs.json` and the Owed
  sections of `nvda-test-record.md`. An entry leaves when its source changes; then the script is run.
- Each of these scripts takes `-Check`, which writes nothing and fails when its page is stale; `test.ps1`, the push
  check and the Build workflow run them all through `tools/pages.ps1 -Check`.

## Publish, install, release

- `publish.ps1` makes the self-contained exe, the installer and the guide in `publish\`, zips them and, run without
  a switch, installs: it replaces the axKit installed for this Windows account and stops and starts again its
  background apps. `-NoInstall` only builds, which is what the development computer uses, where no installed copy is
  kept; `-NoZip` and `-ZipOnly` are the release workflow's two halves.
- `publish.ps1 -Msix` makes the MSIX package for the Microsoft Store in `publish\AxKit-<version>.msix` (SPEC AX-13,
  `store-distribution.md`): a folder publish, `tools/make-manifest.ps1` for its manifest from the apps listing,
  `tools/make-icon.ps1 -Png` for its images, makeappx from Windows Kits or from `tools/get-sdk-tools.ps1`, which
  fetches the Windows SDK's packaging tools into `tools/sdk` once. A local test registers
  `publish\msix\package\AppxManifest.xml` with `Add-AppxPackage -Register`, Developer Mode on.
- `install.ps1` and `install.cmd`: the per-user install (`-Uninstall`). Before it changes anything it stops the
  background apps that run from the installed folder (or from an earlier name's), and those alone: a copy that runs
  from another folder, a build from the source, is left alone and named.
- `release.ps1 <version>`, after a `## <version>` section in `CHANGELOG.md`: its checks (below), the pages, the
  tests, the version, the commit, the tag, the push; then it waits for the draft GitHub Actions builds, signs its zip
  with the release key and publishes it (`tools/sign-release.ps1`), and that release is what the apps update from.
  Only at the maintainer's word. It refuses while the newest build of `origin/main` on GitHub is not a success: that
  build compiles the whole solution, `src/AxKit` and the tools among it, which no check on the development computer
  does. When a push inside it is refused (the pre-push hook runs the tests) or fails, the commit and the tag are in
  the clone and nothing is on GitHub: it says so and prints the two ways on as commands to type (push `main`, wait
  for its build, push the tag, `tools\sign-release.ps1 <version>`; or delete the tag, `git reset --soft HEAD~1` and
  put the project file back), and a second run of the same version says them again.
- `release.ps1 -Rehearse [<version>] [-WithTests]`, before a release: the rehearsal. It makes the real run's checks
  (the branch, behind origin, the hooks setting, the release key's file, uncommitted files, the changelog section,
  the windows' recorded passes, the version and the newest build of `origin/main`; of the given version, or of the newest released section) and asks
  GitHub every question the real run and
  `tools/sign-release.ps1` ask, through the same functions and so with the same command lines, about what is already
  there: the build of main's newest commit and the newest published release. It commits, tags, pushes, signs and
  publishes nothing (it only fetches origin), takes a few seconds, and runs `test.ps1` only with `-WithTests`. Exit
  code 0 when every check passed and every question was answered sensibly; uncommitted files are named as what the
  real run would refuse and do not change the exit code; a failing question is shown with its command and output.
  The parts of the script that talk to git and GitHub otherwise run only in a real release (two faults met 5.8.0
  there); `ReleaseScriptTests` reads both scripts for the shape that keeps the rehearsal harmless.
- `tools/sign-release.ps1 <version>`: signs a release's draft with the release key and publishes it; `release.ps1`
  calls it last, and it is run alone when a release stopped there (`-Rehearse` asks its questions only). Before
  anything else it refuses a key that is not the apps': the public half of this computer's key must equal the
  constant in `AxKit.Core/Updates/ReleaseSigning` (`install.cmd` holds the same; `ReleaseKeyTests`). It waits for the
  release workflow by asking every 20 seconds, 40 minutes at most, and takes the zip only with GitHub's provenance
  from the workflow `release.yml` itself, not from any workflow of the repository.
- `tools/store-submit.ps1 <version> -WhatsNew <file> [-Submit]`, after a published release and only at the
  maintainer's word: the release's `AxKit-<version>.msix` from GitHub, its provenance checked as the zip's is, into a
  new submission in Partner Center through
  `tools/EdgeDrive` (store-setup.md sections 5 and 6): Start update, the package dropped and saved, "What's new" written
  from the file and read back; `-Submit` also sends it for certification, without it the draft is left ready. It stops
  with what it saw at any step that does not hold, and at a sign-in page, which is the maintainer's. It drives only
  the Edge it starts, on the release profile: when the debugging port 9222 already answers, the Edge of an earlier
  run on that profile is used again and said, and any other program stops the script, named. `-Rehearse` reads
  only: the package on GitHub and the overview's state.
- `tools/release-key.ps1 <command>`: the release key (SPEC AX-3.4), kept in `%LOCALAPPDATA%\AxKit-release\release-key.bin`
  for the Windows account. `public` prints its public half, which must equal `AxKit.Core/Updates/ReleaseSigning`'s;
  `sign <zip>` and `verify <zip>` are `tools/sign-release.ps1`'s steps; `new` made the key once and refuses while one
  exists. `export <file>` writes a backup under a passphrase that is piped in (never an argument, never printed),
  refusing an existing file;
  `import <file>` puts the key back from one on a computer that has none, never replacing a key. The backup lives in
  the private repository `KyleKeane/AxKit-keys`, cloned as `..\axkit-keys` (`development-environment.md` section 2):
  `Get-Content ..\axkit-keys\release-key.passphrase.txt | .\tools\release-key.ps1 import ..\axkit-keys\release-key.backup`,
  and the same with `export` and a new file name for a new backup.
- `tools/release-notes.ps1 <version>` prints a version's section of `CHANGELOG.md`: the release notes, for the release
  workflow and `release.ps1`'s check. `tools/reporter-token.ps1` writes the reporter token's file for a release build
  from the workflow's secret and removes it after (`reports.md` RP-6.2); `publish.ps1` calls it.
- `.github/workflows`: `build.yml` tests every push to every branch and every pull request; `release.yml` builds a
  `v*` tag into a draft GitHub release. `.github/dependabot.yml` keeps the actions and the test packages current.

## The Store

- `tools/store-screenshots.ps1` takes the listing's screenshots, with the user away: its windows open in front, so
  Windows' voice says so and counts from ten to one first, and a key or a mouse move during the countdown stops it
  (`tools/spoken-countdown.ps1`, the countdown of `nvda-pass.ps1`).
- `tools/EdgeDrive` drives a Microsoft Edge window over the DevTools protocol, one command per run, for Partner
  Center's website (`store-setup.md` sections 5 and 6); `tools/store-submit.ps1`, above, does a release's
  submission with it. Microsoft's API route was built, never switched on, and removed on 2026-11-13, since the
  account is an Individual account (`store-setup.md` section 2).

## The assistant's tools

Each removes a mistake the assistant made again and again, found from the sessions' history on 2026-11-25 (about
54,000 tool calls, 887 failed): the mistake is in the script's header. The assistant uses these in place of a command
made up for the occasion.

- `tools/replace.ps1 -File <path> -Old <text> -New <text> [-All]`, or `-Pairs <file.json>` (an array of `file`, `old`,
  `new`, `all`): exact replacements, each text found once (or every time, with `all`) or nothing is written; the
  texts take the file's own line ends, and the file keeps its byte order mark and line ends. In place of a hand-made
  `ReadAllText`, `Replace`, `WriteAllText` (846 uses, about 30 of them missed their text and went on).
- `tools/commit.ps1 -Subject <line> -Body <text> -Item <words> | -ItemDone <words> | -ItemNone <reason> -Paths
  <paths> | -All`: a commit with the item line of rule 9 and the attribution, the message as UTF-8 without a byte
  order mark, only the paths named staged; the commit-msg hook still runs.
- `tools/repair.ps1 <R-id> [-Status done -Commit <hash> -Verify <how>] [-Note <text>] [-Owed <kind: text>] [-Fix
  <text>]`: one item of `repairs.json` changed in the file's own form, and `repairs.md` written again.
- `tools/run-scripts.ps1 <scripts or folders>... [-Exe <AxKit.exe>] [-Live] [-Record] [-Inventory <mode>] [-Full]`:
  window scripts on the hidden desktop from a runner built into a folder of its own under `%TEMP%`, so that a build
  of `tools/AxAutomate` beside it (`upstream.ps1 -Verify`) meets no lock; several at once, the failures and one line a
  script, redacted.
- `tools/native.ps1 <program> <arguments>`: a program's error output as plain lines and its own exit code, last, as
  `exit <code>`; Windows PowerShell 5.1 turns a program's error output under `2>&1` into errors and reports exit 1
  although the program succeeded.
- `tools/redact.ps1`: a filter for any output: the profile folder, the account name and e-mail addresses replaced
  (rule 6).
- `tools/doc-checks.ps1`: the page checks and the tests of notes, documents, keys, menu marks, the changelog and the
  bundle's rules, in about a minute: what the assistant's edits failed most (about 110 failures of full runs). Before
  `.\test.ps1`, not in its place.
- `tools/hooks/claude-powershell-check.ps1`: Claude Code's check before each command of its PowerShell tool
  (`.claude/settings.json`, PreToolUse): a command that does not parse as Windows PowerShell 5.1 is refused with the
  parser's message and how to write it, before any of it runs.
- `tools/item.ps1` and the commit-msg hook name the three nearest titles when words name no item.

## Other tools

- `tools/new-app.ps1 <Name> <verb> <group> <opens>`: a new app, its folders, a first window on the navigator, its
  documents and its registration places (`adding-an-app.md`). `tools/check-new-app.ps1` makes the throwaway app axZeta
  with it in a clean copy of the last commit, builds it and runs its tests and the bundle's checks of every app's
  places, so that the script cannot fall behind the framework unseen; run it after a change to either, and before a
  release.
- `tools/push-check.ps1`: what the pre-push hook runs before anything leaves the computer: first, where the
  priorities list is beside the repository, that every commit of the push that is no merge names its item (rule 9,
  with the commit hook's passes; the hook itself runs only where it is installed); then the page checks,
  `tools/upstream.ps1 -Check` and `tools/script-lint.ps1`, every one, so that all the stale pages are named at once and
  in seconds; then `tests/AxKit.Tests` always and the test projects of the apps the pushed commits changed (every
  project when shared code changed); a failure refuses the push. With a change not committed, or a file never added
  under `src`, `tests`, `tools` or `docs` (it names them), the checks run on a clean copy of HEAD, where such a file
  is missing as it will be on GitHub.
- `tools/pages.ps1`: every generated page written again in one run, the same scripts the checks run; run it before a
  commit that changed a script, a command, a key or a document. With `-Check` it is the one list of the quick checks,
  for `test.ps1`, the push check and `build.yml`.
- `tools/run-folder.ps1` (dot-sourced): a run's own folder under the temporary folder, named by the checkout and the
  process, for the scripts that build or copy into one (`workflows.ps1`, `graph-check.ps1`, `push-check.ps1`,
  `check-new-app.ps1`, `upstream.ps1`), so that two sessions in one checkout never meet in it; each removes its
  folder at its end. `tools/run-scripts.ps1` keeps its runner between runs on purpose and names it otherwise.
- `tools/make-icon.ps1`: the apps' icons from `Dispatch.Apps`. `tools/make-test-pictures.ps1`: the three test
  pictures of axHelp's screenshots in `tests/fixtures/pictures`, run only to change them.
- `tools/session-start.ps1`: the start of a working session in one run, its checks in order, each by the script that
  owns it: `git fetch origin` and whether `main` (and the branch in hand) lacks commits of `origin/main`, every
  worktree's branch that holds commits `main` lacks (work finished beside main and never merged), and how GitHub's
  build of `origin/main` ended; `tools/sync-repos.ps1 -Status`; `tools/reports-check.ps1`;
  `tools/outside-check.ps1`; `tools/upstream.ps1`. One line each, starting with what
  was checked and ending with "ok" or with what to do ("code: main is 2 commits behind origin/main: git pull
  --ff-only"; "reports: 1 open report without an item: read it and write its item"), the owning script's own lines
  indented under a line that asks for something. It commits and pushes nothing, here or beside, and says the same
  when run twice; without the network it says so and compares with what was fetched last; a folder that is not
  beside the repository is said and passed over; no path of the computer is printed. Exit code 0 when nothing is
  asked, 1 otherwise.
- `tools/sync-repos.ps1`: the two repositories beside this one on the maintainer's computer, `..\axkit-reports` (the
  clone of the reports repository) and `..\axkit-management` (the private axTask lists): fetched, the lists committed
  as one dated commit, pulled fast-forward, pushed, one line each, nothing forced. `-Status` only says where they
  stand. Git runs it on every push of this repository through the pre-push hook `tools/hooks/pre-push`, installed once
  per clone with `git config core.hooksPath tools/hooks`, which `test.ps1` and `release.ps1` check. The fourth folder,
  `..\axkit-keys`, is not synced by it: it is committed and pushed by hand when a backup changes.
- `tools/item-of-commit.ps1 <message file>`: the priorities list's bookkeeping from a commit message (the root
  `CLAUDE.md`, rule 9). The message names its item on a line of its own: `Item: <words of the title>` (the item's
  Started field gets today's date), `Item done: <words>` (its Done is checked too) or `Item: none, <the reason>`. A
  message without such a line, or with words that name no item or several, is refused. Git runs it for every commit
  through the hook `tools/hooks/commit-msg`; where the lists are not beside the repository, every commit passes.
  What passes without a line: git's own revert and fixup messages, the release script's "Release 1.2.3", and "Merge
  ..." only while git has a merge to commit (the hook asks for `MERGE_HEAD`). Its guard on the pinned approval key
  asks the index the commit is made from, which the hook hands on, so that `git commit -a` and `git commit --
  <path>` are seen as a staged change is.
  `-Check` says what it would mark and writes nothing; `-Lists <folder>` names another list.
- `tools/item.ps1`: an item of the priorities list written, noted and finished, in place of a script made up for the
  occasion. `new -Title -App -Priority -Where -Notes` writes a new item with the list's own fields, its file named
  from the title as axTask names it, and refuses a name that exists and a priority the list does not have; `note
  -Name <words of the title> -Text` puts the text, dated today, in front of the item's Notes; `done -Name <words>`
  checks its Done; `archive` moves every done item into the top folder's `Archive\priorities`, out of the list,
  which the sync of every push does by itself (the maintainer's ask of 2026-11-23). The words must name exactly one item, as a commit message's (`tools/items.ps1` is the matching
  both scripts use). One line says what was written, with the title and nothing else of the item. It never commits
  or pushes; without the lists beside the repository it says so, exit code 1. `-Lists <folder>` names another list.
- `tools/reports-check.ps1`: the reports on GitHub (the issues of `KyleKeane/AxKit-reports`) against the items of the
  priorities list (the root `CLAUDE.md`, rule 9). An item points at its reports in Where it is written with
  `KyleKeane/AxKit-reports issue 8` or `issues 8 and 9`, up to the next semicolon. One line per disagreement: "new" (an
  open report no item points at), "close it" (an open report whose items are all done), "item open" (a closed report
  with an item not done); or one line saying they agree. It changes nothing. Exit code 0 when they agree, 1 when
  something is named, 2 when GitHub or the lists could not be read. `tools/sync-repos.ps1` runs it with every push,
  without minding its exit code; run it at the start of a session. `-Lists` and `-IssuesFile` name other sources.
  Since 2026-11-21 a report with an entry in the outside reports list is judged by that entry, and a new report is
  named by its number alone.
- The outside reports' scripts (`docs/axKit/outside-reports.md`, section 7): `tools/outside-report.ps1` writes a
  report's triage entry (`new`, `review`, `ready`, `done`, `reopen`) and never an approval; `tools/outside-check.ps1`
  holds the work to the maintainer's signed approvals, asks no network, and is run by the pre-push hook, which refuses
  the push when it names anything, and by `tools/session-start.ps1`; `tools/pin-approval-key.ps1 -Trailer <line>` pins
  the approval key the maintainer set or changed in axTask, in a commit of its own; `tools/approvals.ps1` is the
  functions they share and runs nothing.
  It reads numbers, states and titles alone and needs no key; a report's text is read with `tools/ReportKey`.
- `tools/ReportKey` (`dotnet run --project tools\ReportKey -c Release -- <command>`): the maintainer's key for the
  sealed reports, one command per run (`reports-setup.md` section 5, `reports.md` RP-6.1a). `issues` prints every
  open report of `KyleKeane/AxKit-reports`, asked of `gh`, opened with the key: number, labels, state, title, the
  envelope's members in the clear, the plain report ("not sealed" for an issue of 5.8.0 or earlier). `open <file>`
  does so for one issue body or envelope, from a file or standard input. `public` prints the key's public half and
  fingerprint and says whether this build seals for it; `seal <file>` seals a text as a copy of axKit does, for
  trying `open`. `new` made the key once (it refuses while one exists); `export <file>` writes a backup of the
  private half under a passphrase (refusing an existing file), and `import <file>` reads one back (refusing while a
  key exists); both read the passphrase from standard input. The private half is in
  `%LOCALAPPDATA%\AxKit-release\reports-key.bin` on the maintainer's computer and is never printed; its backup is in
  the private repository `KyleKeane/AxKit-keys`, cloned as `..\axkit-keys`:
  `Get-Content ..\axkit-keys\reports-key.passphrase.txt | dotnet run --project tools\ReportKey -c Release -- import ..\axkit-keys\reports-key.backup`.
  Without the key or its backup, no sealed report can be read.
- `tools/PtyCapture`: records Claude Code's screen stream for a fixture (`--dump` reads a recording with personal data
  replaced; never `--raw`). The `record-fixture` skill has the steps.
- `tools/ClaudeProbe stream [--only <case>] [--shape <file>] [--keep <folder>]` measures Claude Code's
  non-interactive stream (`docs/axClaude/stream-mode.md` rests on it): twenty-four cases played against the installed Claude Code, each a
  process of its own with the smallest model, no session saved (the one case that takes a session up again saves it
  and deletes its file from Claude Code's store afterwards) and no MCP server but the probe's own stand-in
  permission tool (the same program, `permission-tool`); kinds, names and times are printed, never a text, a path or
  an id; exit code 1 when a case fails. `tools/upstream.ps1 -Verify claude` runs it as Claude Code's probe
  (`"probes": ["stream"]` in `upstream.json`) and writes `docs/axClaude/stream-shape.json` again.
- `tools/ClaudeProbe` (built into a scratch folder: `dotnet build tools\ClaudeProbe -c Release -o <folder>`, then
  `dotnet build-server shutdown`): the measuring tool of axClaude's events (`docs/axClaude/events-and-grounding.md`
  EG-11.4), never shipped and never started by axKit; while a recording runs it listens on 127.0.0.1 for the hooks it
  measures. `run <config.json>` records one live session of Claude Code (the console stream, every hook event, every
  line of the session file, on one clock) into a scratch folder; `timeline <run folder>` prints the streams merged by
  time and `shape <run folder>` the fields of the events and entries, the profile folder, e-mail addresses and the
  account name replaced; `settings <config.json>` prints the hooks a configuration gives; `keys` the key names of a
  script; `fixture <run folder> <fixture folder>` makes a run a timeline fixture through the whitelist of EG-11.1,
  printing counts and "written", or refusing it (exit code 4) when it still holds an e-mail address, an account path
  or the account's name. Its `README.md` has the configuration and the script; the `record-fixture` skill the steps
  of a timeline.
