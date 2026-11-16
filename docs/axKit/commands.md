# Commands and scripts

Every command and script of the repository, by what it is for. The root `CLAUDE.md` names the few used every day and
points here. Each app's own commands are in its `CLAUDE.md`.

## Build

- `dotnet build AxKit.sln`. `Directory.Build.rsp` makes every build's MSBuild nodes exit with the build, and the
  scripts run `dotnet build-server shutdown` after theirs. After a bare `dotnet build` or `dotnet test`, run it
  yourself: the development computer has 7 GB, and a build otherwise leaves about 1 GB of idle build servers behind for
  ten to fifteen minutes (`development-environment.md` section 5).
- Never build into the running app's `bin`: to check a change while axKit runs, `dotnet build <project> -o <scratch
  folder>`.

## Test

- `test.ps1 [<App>]` runs every test project (or one) in Release, one by one, so that a running axKit's `bin` is never
  touched, and then checks the generated pages (below). It is what `release.ps1` and `run.ps1 -Test` call. Run it
  before a commit. The workflows `build.yml` and `release.yml` run `dotnet test AxKit.sln`, the solution, in Release.
  Never `dotnet test AxKit.sln` while axKit runs: it writes into the running program's `bin\Debug`. Debug alone misses
  what Release treats as errors, which cost 3.3.0.
- `tools/AxAutomate <script or folder> (--build | --exe <AxKit.exe>)` plays the automated window checks of
  `tests/scripts` on a desktop of their own (`automation.md`). It holds the windows' commands against the command
  inventory, `docs/axKit/window-commands.json`, and says after its summary when a window lists a command the file does
  not hold as it is (exit code 3 when every script passed). `--inventory write` merges what the windows list into the
  file, `rewrite` also drops what a whole app no longer lists, `off` leaves it alone; `--repository <folder>` names
  the repository when the program is not built inside one (`automation.md`, "The command inventory"). It also holds
  the trace of every section a script marks `# Stable` against the kept one in `tests/traces`, and a trace that
  differs or is missing fails the script; `--traces write` writes a script's kept file (read it whole before the
  commit), `--traces off` neither compares nor writes (`automation.md`, "The traces"). Exit codes: 0
  passed, 1 a script failed, 2 the run could not start, 3 the inventory is stale. Its other form,
  `tools/AxAutomate --reference [--check]`, plays nothing: it writes the workflow pages, or checks them, for
  `tools/workflows.ps1` (below). `tools/QaRun` runs Claude sessions without windows, always with `--model haiku`.
- `nvda-pass.ps1 [<app>]` runs the same scripts with NVDA listening, plus `tests/nvda-scripts`: the NVDA test plans as
  the assistant runs them. It takes the keyboard, so only after the user has stepped away.
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

Each is written by its script from its sources and checked by `test.ps1` and `build.yml` with `-Check`. Never edit a
page by hand; run its script and commit the page.

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
  changed in the same commit as its fix.
- `tools/apps-page.ps1` writes `apps.md`: the apps from `Dispatch.Apps`, each with its command and its folders.
  `tools/apps.ps1` is the reader the other scripts share.

## Publish, install, release

- `publish.ps1` makes the self-contained exe, the installer and the guide in `publish\`, zips them and installs for
  this user (`-NoInstall`; `-NoZip` and `-ZipOnly` are the release workflow's two halves).
- `publish.ps1 -Msix` makes the MSIX package for the Microsoft Store in `publish\AxKit-<version>.msix` (SPEC AX-13,
  `store-distribution.md`): a folder publish, `tools/make-manifest.ps1` for its manifest from the apps listing,
  `tools/make-icon.ps1 -Png` for its images, makeappx from Windows Kits or from `tools/get-sdk-tools.ps1`, which
  fetches the Windows SDK's packaging tools into `tools/sdk` once. A local test registers
  `publish\msix\package\AppxManifest.xml` with `Add-AppxPackage -Register`, Developer Mode on.
- `install.ps1` and `install.cmd`: the per-user install (`-Uninstall`).
- `release.ps1 <version>`, after a `## <version>` section in `CHANGELOG.md`: the pages, the tests, the version, the
  commit, the tag, the push. GitHub Actions publishes the release the apps update from. Only at the maintainer's
  word. `gh run watch` follows the workflow.
- `release.ps1 -Rehearse [<version>] [-WithTests]`, before a release: the rehearsal. It makes the real run's checks
  (the branch, behind origin, the hooks setting, uncommitted files, the changelog section and the version; of the
  given version, or of the newest released section) and asks GitHub every question the real run and
  `tools/sign-release.ps1` ask, through the same functions and so with the same command lines, about what is already
  there: the build of main's newest commit and the newest published release. It commits, tags, pushes, signs and
  publishes nothing (it only fetches origin), takes a few seconds, and runs `test.ps1` only with `-WithTests`. Exit
  code 0 when every check passed and every question was answered sensibly; uncommitted files are named as what the
  real run would refuse and do not change the exit code; a failing question is shown with its command and output.
  The parts of the script that talk to git and GitHub otherwise run only in a real release (two faults met 5.8.0
  there); `ReleaseScriptTests` reads both scripts for the shape that keeps the rehearsal harmless.
- `tools/sign-release.ps1 <version>`: signs a release's draft with the release key and publishes it; `release.ps1`
  calls it last, and it is run alone when a release stopped there (`-Rehearse` asks its questions only).
- `tools/release-key.ps1 <command>`: the release key (SPEC AX-3.4), kept in `%LOCALAPPDATA%\AxKit-release\release-key.bin`
  for the Windows account. `public` prints its public half, which must equal `AxKit.Core/Updates/ReleaseSigning`'s;
  `sign <zip>` and `verify <zip>` are `tools/sign-release.ps1`'s steps; `new` made the key once and refuses while one
  exists. `export <file>` writes a backup under a passphrase read from standard input, refusing an existing file;
  `import <file>` puts the key back from one on a computer that has none, never replacing a key. The backup lives in
  the private repository `KyleKeane/AxKit-keys`, cloned as `..\axkit-keys` (`development-environment.md` section 2):
  `Get-Content ..\axkit-keys\release-key.passphrase.txt | .\tools\release-key.ps1 import ..\axkit-keys\release-key.backup`,
  and the same with `export` and a new file name for a new backup.
- `.github/workflows`: `build.yml` tests every push to every branch and every pull request; `release.yml` builds a
  `v*` tag into a GitHub release; `dependabot.yml` keeps the actions and the test packages current.

## The Store

- `tools/store-screenshots.ps1` takes the listing's screenshots, with the user away.
- `tools/EdgeDrive` drives a Microsoft Edge window over the DevTools protocol, one command per run, for Partner
  Center's website, where every release's package is uploaded by hand (`store-setup.md` sections 5 and 6).
- No command submits to the Store: the route through Microsoft's API was built, never switched on, and removed on
  2026-11-13, since the account is an Individual account (`store-setup.md` section 2).

## Other tools

- `tools/new-app.ps1 <Name> <verb> <group> <opens>`: a new app, its folders, a first window, its documents and its
  registration places (`adding-an-app.md`).
- `tools/make-icon.ps1`: the apps' icons from `Dispatch.Apps`.
- `tools/sync-repos.ps1`: the two repositories beside this one on the maintainer's computer, `..\axkit-reports` (the
  clone of the reports repository) and `..\axkit-management` (the private axTask lists): fetched, the lists committed
  as one dated commit, pulled fast-forward, pushed, one line each, nothing forced. `-Status` only says where they
  stand. Git runs it on every push of this repository through the pre-push hook `tools/hooks/pre-push`, installed once
  per clone with `git config core.hooksPath tools/hooks`, which `release.ps1` checks. The fourth folder,
  `..\axkit-keys`, is not synced by it: it is committed and pushed by hand when a backup changes.
- `tools/item-of-commit.ps1 <message file>`: the priorities list's bookkeeping from a commit message (the root
  `CLAUDE.md`, rule 9). The message names its item on a line of its own: `Item: <words of the title>` (the item's
  Started field gets today's date), `Item done: <words>` (its Done is checked too) or `Item: none, <the reason>`. A
  message without such a line, or with words that name no item or several, is refused. Git runs it for every commit
  through the hook `tools/hooks/commit-msg`; where the lists are not beside the repository, every commit passes.
  `-Check` says what it would mark and writes nothing; `-Lists <folder>` names another list.
- `tools/reports-check.ps1`: the reports on GitHub (the issues of `KyleKeane/AxKit-reports`) against the items of the
  priorities list (the root `CLAUDE.md`, rule 9). An item points at its reports in Where it is written with
  `KyleKeane/AxKit-reports issue 8` or `issues 8 and 9`, up to the next semicolon. One line per disagreement: "new" (an
  open report no item points at), "close it" (an open report whose items are all done), "item open" (a closed report
  with an item not done); or one line saying they agree. It changes nothing. Exit code 0 when they agree, 1 when
  something is named, 2 when GitHub or the lists could not be read. `tools/sync-repos.ps1` runs it with every push,
  without minding its exit code; run it at the start of a session. `-Lists` and `-IssuesFile` name other sources.
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
