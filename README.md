# axKit

[![Build](https://github.com/KyleKeane/AxKit/actions/workflows/build.yml/badge.svg)](https://github.com/KyleKeane/AxKit/actions/workflows/build.yml)
[![Latest release](https://img.shields.io/github/v/release/KyleKeane/AxKit?label=release)](https://github.com/KyleKeane/AxKit/releases/latest)
[![Licence: AGPL-3.0-only](https://img.shields.io/badge/licence-AGPL--3.0--only-blue)](LICENSE)

The tools I use every day as a blind scientist, working with the NVDA screen reader, shared in case they help someone else. They are personal tools, not a product or a service: nothing is sold, and there is no support and no warranty ([NOTICE.md](NOTICE.md)).

They are Windows apps in one program with one installer and one updater. Every app is made of native Windows controls that NVDA reads reliably, works without a mouse, says the result of each command once, and keeps its questions and errors inside its own window.

axKit was called Axit up to 4.0.0 and was renamed on 2026-11-05, since a commercial product carries that name; the old repository address, github.com/KyleKeane/Axit, redirects here. An installed Axit does not update into axKit by itself: close its windows and run `install.cmd` from the [latest release](https://github.com/KyleKeane/AxKit/releases/latest), which removes Axit and installs axKit, and your settings come along ([docs/axKit/README.md](docs/axKit/README.md), Install).

## The apps

### Standalone apps: Windows only, with the network off

- **axDown**: a barebones editor for Markdown and text files that writes a file back exactly as it found it, with keys for headings, links, lists, quotes and tables.
- **axJSON**: the same editor for JSON, with keys for keys, objects, arrays, items and errors, and a Format command that repairs what it can.
- **axFile**: a file manager for one folder, with an Actions list of every operation that Ctrl+Z can take back, selection, zip files, favourites and the drives.
- **axMedia**: a player for sound and video files with one queue, speed with the pitch kept, and seeking by key.
- **axPreview**: a read-only viewer for one text file, with the editors' navigation keys.
- **axStart**: a program launcher. One key from anywhere, Ctrl+Shift+Space unless you choose another, brings up a search field over the Start menu's programs; Down and Up move through the matches while the caret stays in the field.
- **axProcess**: what runs on the computer, as one list with three views: the processes with their memory and processor use, the services, and the programs that start at sign-in. End a process after a question, start or stop a service; nothing refreshes by itself.
- **axTask**: tasks and lists as a folder of small files, moved through as axFile moves through folders. Each item's fields (a due date, done, notes, links to other items) are in a pane beside the list; All tasks gathers every task not done.
- **axCalc**: a calculator as one field and a history: type an expression, Enter says the answer, and every answer stays as a line to read back, copy and recall; hexadecimal, binary and octal for development.
- **axClip**: a clipboard history. It keeps the text of every copy while it runs, in memory unless you ask; Windows+Alt+V from anywhere brings up a search field over it, Down and Up move through it with the caret kept in the field, and Enter pastes into the program you were in.
- **axAutomate**: the workflows that drive axKit's windows, starting with the automated checks of every app, read step by step; this version views the workflows and runs them, and changing them comes later.

### Access layers: through a program or service you install, which does the work under its maker's terms (`NOTICE.md` says who is responsible for what)

- **axClaude, access layer for Claude Code**: Claude Code in screen reader mode, shown as a plain-text conversation that NVDA reads line by line, with single-key navigation, a message field, and Claude's questions as lists. You are still talking to the real Claude Code.
- **axGIT, access layer for Git**: a git manager for one repository: changes, the log and the branches as lists, staging, committing, fetch, pull and push.
- **axTerminal, access layer for your shell**: PowerShell, Command Prompt or Git Bash as a transcript in axClaude's kind of window.
- **axPass, access layer for KeePassXC**: a KeePass password database, read and changed by ear: groups and entries in the same two panes, the password never spoken, copied with Ctrl+C and cleared from the clipboard after 30 seconds. It runs KeePassXC's own command-line tool, so KeePassXC must be installed.
- **axMCP, access layer for MCP servers**: the MCP servers Claude Code knows on this computer, and your own, in one list; a server started by hand shows its tools, resources and prompts, a tool's arguments are fields you fill by ear, and the result reads as text. For trying a server before giving it to Claude Code, or running a tool without a turn of Claude's.
- **axHelp, access layer for Claude Code**: your conversation with Claude from anywhere: Windows+Alt+H brings up an axClaude window titled axHelp, one conversation that goes on until you clear it, on a fast model of its own, where Claude Code can also do work on your computer, asking first; Windows+Alt+S puts a picture of the window in front into it, for a question about the screen. axDescribe, which described the screen, is part of axHelp since 5.7.0.

The apps reach each other: axFile hands a file to axMedia or axPreview, every window can show its folder in the others, and the Window menu in every window lists the open windows and starts any app.

The set grows as I find another thing worth doing by ear. `docs/axKit/plan.md` ("Still to come") lists what has been asked for or suggested and not built yet, and `docs/axKit/adding-an-app.md` says how an app is added; every new app follows the same rules, and whatever it reaches outside axKit enters the same lists.

## Install

Download [install.cmd](https://github.com/KyleKeane/AxKit/releases/latest/download/install.cmd) and run it. The link always fetches the newest release's installer; no administrator rights are needed. It adds the Start menu entries, the commands for every app (`axkit`, `axDown`, `axCalc` and the rest) and an "Open with axKit" submenu in File Explorer. Installed copies offer each new release themselves: Help, Check for updates.

The [latest release's page](https://github.com/KyleKeane/AxKit/releases/latest) has the same `install.cmd`, the zip and its notes; every earlier version is on the [releases page](https://github.com/KyleKeane/AxKit/releases). `docs/axKit/README.md` is the README inside the zip, with the rest.

Needs Windows 10 1809 or later. axClaude also needs [Claude Code](https://code.claude.com/docs/en/setup). The standalone apps work with the network off; an access layer reaches it only through the program it runs, as the next section says.

### Or from the Microsoft Store

The Store's copy is behind the releases: the 5.2.1 package is the one submitted so far. Install it with `winget install 9PK01L2K5000 -s msstore` in any terminal, or from [the Store's page](https://apps.microsoft.com/detail/9PK01L2K5000). The Store signs the package with Microsoft's certificate, which Smart App Control accepts, installs it for your account, and keeps it current by itself; Check for updates in any window says where your copy stands. The Store's copy has eight Start menu entries, the Store's limit ("axKit axClaude", axDown, axFile, axTerminal, axGIT, axTask, axStart and axClip; from the next Store package axHelp takes axGIT's place, so that it starts at sign-in there too), the commands for every app (`axkit`, `axDown`, `axCalc` and the rest) and Windows' Open with for the apps' file types; the other apps open from their command, from a file of their kind, from the Window menu's New window in any window and from axFile. File Explorer's "Open with axKit" submenu is the zip's alone ([docs/axKit/SPEC.md](docs/axKit/SPEC.md) AX-13).

Pick one of the two. A Store copy and a zip copy on the same computer are two installs with separate settings, and each updates from its own source: the Store copy from the Store, the zip copy from this repository's releases; neither knows about the other.

## Privacy, and what axKit works with

axKit collects nothing: no telemetry, no analytics, no crash reports, no accounts. Its own code makes network requests in three places, the updater, the report sender and axTask's GitHub lists, over HTTPS to GitHub only. The updater: the update check when a window starts (one request for the latest release, with the version in the User-Agent header and nothing else about you; Options, General, Check for updates at start turns it off) and, when you choose Update, the release's zip and signature, accepted from this repository's release address only and checked before they are installed. The report sender: only when you press Enter on a report (F4 in any window), it posts what the report notice showed you as a public issue of the reports repository, [AxKit-reports](https://github.com/KyleKeane/AxKit-reports), with the maintainer's reporter token, never with an account of yours, carrying a random id of your install and nothing that names you, after reading that repository's blocklist (`docs/axKit/reports.md`). axTask's GitHub lists: only when you open or refresh a list you made of a GitHub repository's issues, axTask reads those issues, and an action you choose on one of its items (a label, close or reopen, a comment, a line of the reports repository's blocklist) is sent with a token of your own GitHub account, which you gave axTask for it (`docs/axKit/reports.md` RP-7). No other code in axKit makes a network request or listens for one.

The access layers meet the network only through the program each runs, as a child process on your computer, through a pseudo console or pipes, never over the network; that program uses the network as it would without axKit, under its maker's terms, on your command:

- **axClaude, access layer for Claude Code** runs `claude --ax-screen-reader`; Claude Code talks to Anthropic's service with your account, its own update check included.
- **axGIT, access layer for Git** runs your git; fetch, pull and push reach your repository's remotes, after your consent for that server; every other command works on disk.
- **axTerminal, access layer for your shell** runs PowerShell, Command Prompt, Git Bash or PowerShell 7; your commands reach whatever they reach.
- **axPass, access layer for KeePassXC** runs `keepassxc-cli.exe` on your database file; the tool has no network feature.
- **Install it now** runs Windows' winget from its own source, or Anthropic's installer script for Claude Code, only when you choose; **the AI tools** (off unless you turn them on) read axKit's windows through named pipes on your computer, and run their menu commands and add items to your axTask lists only behind a second switch of their own; Help menus and links open in your browser.

[PRIVACY.md](PRIVACY.md) says all of it in full, with what stays on your computer; it is also in the zip and in every window: Help, About, Privacy. [docs/axKit/outside-calls.md](docs/axKit/outside-calls.md) is the exhaustive list behind both, written from the sources: everything axKit reaches outside its own process, with every call's file, line and purpose.

## Code signing policy

Every release is built by GitHub Actions from this repository's source, with GitHub's attestation of where it was built, and approved by the maintainer before it is published. Its zip is signed with axKit's release key, which never leaves the maintainer's computer; the installed apps and `install.cmd` install nothing whose signature does not check ([docs/axKit/SPEC.md](docs/axKit/SPEC.md) AX-3.4).

**Windows code signing through the Microsoft Store.** The Store's copy of axKit is an MSIX package that Microsoft signs with its own certificate after certification, installs and updates ([docs/axKit/store-distribution.md](docs/axKit/store-distribution.md)); Windows' Smart App Control accepts it. The GitHub release's `AxKit.exe` carries no Authenticode signature, every release's notes say so, and Smart App Control may refuse it; an update it refuses stops without changing anything. A signing service for the GitHub release (Azure Artifact Signing) was set up in 2026-11 and retired on 2026-11-09 before it signed a release, in favour of the Store ([docs/axKit/code-signing.md](docs/axKit/code-signing.md), the record).

Team and roles ([GOVERNANCE.md](GOVERNANCE.md)): Dr. Kyle Keane (www.kylekeane.com), author, reviewer and approver.

Privacy: axKit transfers no information to other networked systems unless the user asks it to, except the update check above, which the user can turn off. Its own code makes HTTPS requests to GitHub alone, in three places: the updater (that check and the update the user chooses), the report sender (a report the user sends) and axTask's GitHub lists (a list the user opens or refreshes, and the user's actions on its items with the user's own token); its access layers start Claude Code, git, the user's shell and KeePassXC's tool as child processes on the computer, which use the network as they would without axKit, under their makers' terms, on the user's command and after the user's consent ([PRIVACY.md](PRIVACY.md)).

Transparency and self-audit: [PRIVACY.md](PRIVACY.md) says what axKit sends and runs, for the user; [SECURITY.md](SECURITY.md) what matters for security and how to report; [GOVERNANCE.md](GOVERNANCE.md) who decides; and [docs/axKit/outside-calls.md](docs/axKit/outside-calls.md) is the exhaustive list of everything axKit reaches outside its own process, every network request, program start and pipe in the code, by what it reaches (GitHub, Claude Code, Git, the shells, KeePassXC, the installers, Windows, the programs you choose, axKit itself, the AI tools' pipes), with what each is, how axKit reaches it and its documentation, and each call's file, line, function and purpose. `tools/outside-calls.ps1` writes that page from the sources, checks it on every push and writes it again at every release; a call without its explanation fails the build.

## Build and run

Needs the .NET 10 SDK, and NVDA for the manual checks. Every tool of the development computer, how it is installed, signed in and checked, and the order to set a new computer up in: [docs/axKit/development-environment.md](docs/axKit/development-environment.md).

```
.\run-axFile.ps1 C:\path\to\folder       # build, then start axFile on that folder
.\run-axClaude.ps1 C:\path\to\project    # axClaude on a project folder
.\run.ps1 -App down C:\notes\todo.md     # any app: claude, down, json, git, terminal, file, media, preview, start, process, task, pass, calc, clip, mcpclient, help, automate
.\run.ps1 -NoBuild                       # start without building
```

There is a `run-ax<app>.ps1` for each app. Close a running axKit before building: the build writes into the folder it runs from. If PowerShell refuses to run scripts: `powershell -ExecutionPolicy Bypass -File .\run.ps1`.

## Test

```
.\test.ps1                 # every test project, in Release, one by one (GitHub Actions runs the solution in Release)
.\test.ps1 axFile          # one project
.\manual_tests\prepare.ps1 # the files of the NVDA test passes, made new
```

The unit tests cover everything that needs no window. What a window does is checked with NVDA, following each app's `docs/<app>/nvda-test-plan.md` on the files in `manual_tests/<app>`; each folder's `_main.md` says which section uses what. Every pass is written down in `docs/axKit/nvda-test-record.md`, by step, version and commit.

## Release

```
.\release.ps1 3.4.0        # checks the changelog section, tests, sets the version, commits, tags v3.4.0, pushes main and, once its build has passed, the tag
gh run watch               # follows the workflow that builds and publishes the release
```

Add a `## 3.4.0` section to `CHANGELOG.md` first: it becomes the release notes, which the apps show in their update notice. The Release workflow tests, builds the self-contained `AxKit.exe`, and publishes the zip and `install.cmd`; the Build workflow tests every push to every branch. `.\publish.ps1` makes the same zip locally and installs it for this user.

Without GitHub Actions (a runner image or an action changed under the workflow, or the workflow is gone): `.\publish.ps1 -NoInstall` builds the zip into `publish\`; `.\tools\release-notes.ps1 -Version 3.4.0 > notes.md` takes the notes from the changelog; `gh release create v3.4.0 publish\AxKit-3.4.0-win-x64.zip install.cmd --title "axKit 3.4.0" --notes-file notes.md --draft` and `gh release edit v3.4.0 --draft=false` publish it, the draft first so that no app downloads a half-uploaded zip. The practices that keep the repository stable over the years are `docs/axKit/longevity.md`.

## Where things are

- `src/AxKit`: the program. `src/AxKit.Core` and `src/AxKit.Forms`: the pieces the apps share.
- `src/<App>` and `src/<App>.Core`: each app's window and its pure code. Each folder has a `CLAUDE.md` with its notes.
- `tests/`: the unit tests, one project per app, and the recorded console fixtures.
- `docs/<app>/`: each app's `SPEC.md`, `user-guide.md` (Help, User guide in the app) and `nvda-test-plan.md`. The specifications are the source of truth.
- `docs/axKit/`: the bundle's `SPEC.md` (launch, install, update, release, the accessibility and modularity rules), `plan.md`, `adding-an-app.md`, the test record, `review-2026-10-23.md` (the review of the code and documents, what was fixed and what is proposed), `inventory-2026-10-24.md` (the copies, dead code and drift, what was cleaned up and what is open) and `longevity.md` (the practices that keep the repository stable for years, with its calendar).
- `manual_tests/`: the script that makes the files for the NVDA passes.
- `CLAUDE.md`: the map and the rules for working on this repository with Claude Code.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Report a bug or an idea with the [issue forms](https://github.com/KyleKeane/AxKit/issues/new/choose); report a security problem privately, as [SECURITY.md](SECURITY.md) says. How the project is run and what its maintainer commits to: [GOVERNANCE.md](GOVERNANCE.md); how people treat each other here: [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Licence

axKit is free software by Dr. Kyle Keane (www.kylekeane.com) under the GNU Affero General Public License, version 3 only ([LICENSE](LICENSE)), with the additional terms of its section 7 in [NOTICE.md](NOTICE.md). Use it, study it, change it and share it. A changed version that is shared, or offered to people over a network, must offer its full source under the same licence, say that it is changed and by whom, keep the line "Based on axKit by Dr. Kyle Keane", and carry a name and icons of its own: the names axKit and its apps' names are not licensed. Versions up to 3.7.6 were released under the MIT licence.

**Shared as they are.** These are personal tools, given away without charge, with no warranty and no support. If you use them, you do so at your own risk, as I do; keep backups. [NOTICE.md](NOTICE.md) has the licence's terms in full and a reminder for each tool; every window shows it under Help, About, Licence.

My copies are on this repository's [releases page](https://github.com/KyleKeane/AxKit/releases), signed as the code signing policy above says.

axKit is an independent project, not affiliated with or endorsed by Anthropic, NV Access, Microsoft or KeePassXC. What the program carries from others, the .NET runtime and Windows Forms under MIT among it: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
