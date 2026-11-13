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
  `tests/scripts` on a desktop of their own (`automation.md`). `tools/QaRun` runs Claude sessions without windows,
  always with `--model haiku`.
- `nvda-pass.ps1 [<app>]` runs the same scripts with NVDA listening, plus `tests/nvda-scripts`: the NVDA test plans as
  the assistant runs them. It takes the keyboard, so only after the user has stepped away.
- `manual_tests/prepare.ps1 [<app>]` makes and resets the files of the NVDA test passes in `manual_tests/<app>`, spoken
  WAV files and a scratch git repository among them. Git ignores all but the script and the `_main.md` files, so a
  file a test pass needs is added to the script, never committed.

## Run

- `run.ps1 [-App <verb>] [<path>]` builds `src/AxKit` and starts one app, axClaude on a folder by default (`-New`,
  `-NoBuild`, `-Test`). The verbs are the apps' commands (`apps.md`).
- `run-ax<Name>.ps1` is the same with `-App` filled in, one script per app.
- `AxKit.exe <verb> ...`, or a folder or a file alone, picks the app.

## The generated pages

Each is written by its script from one source and checked by `test.ps1` and `build.yml` with `-Check`. Never edit a
page by hand; run its script and commit the page.

- `tools/outside-calls.ps1` writes `outside-calls.md`: everything axKit reaches outside its process, from the
  `Outside call, <kind>, <what is reached>:` comment that every network request, program start and pipe in `src`
  carries. The check fails on a call without one. Run it before a commit that touches such a call (SPEC B-17).
- `tools/workflows.ps1` writes `workflows.md`: every automated script by app under its `# Workflow:` label, the
  workflows kept working and the start of their tutorials. A new script gets its label (axAutomate SPEC AU-5.2a).
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
  per clone with `git config core.hooksPath tools/hooks`, which `release.ps1` checks.
- `tools/item-of-commit.ps1 <message file>`: the priorities list's bookkeeping from a commit message (the root
  `CLAUDE.md`, rule 9). The message names its item on a line of its own: `Item: <words of the title>` (the item's
  Started field gets today's date), `Item done: <words>` (its Done is checked too) or `Item: none, <the reason>`. A
  message without such a line, or with words that name no item or several, is refused. Git runs it for every commit
  through the hook `tools/hooks/commit-msg`; where the lists are not beside the repository, every commit passes.
  `-Check` says what it would mark and writes nothing; `-Lists <folder>` names another list.
- `tools/PtyCapture`: records Claude Code's screen stream for a fixture (`--dump` reads a recording with personal data
  replaced; never `--raw`). The `record-fixture` skill has the steps.
