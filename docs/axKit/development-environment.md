# The development environment (2026-11-06)

What the maintainer's development computer has, what each tool does in this repository, how it is installed, signed in
and checked, and the order to set a new computer up in. Written so that a new computer, or a new assistant, reproduces
it without the maintainer's memory. The versions are the ones in use on the date above; what matters is the band each
belongs to, and the one place a version is pinned is `global.json` for the .NET SDK (`docs/axKit/longevity.md`).
Nothing here holds a credential or an id: those live where section 2 says, never in the repository (`PrivacyTests`
fails on an e-mail address or an account path).

## 1. The tools

Every install is winget's, from the `winget` source, unless the table says otherwise; `winget install --id <id>
--source winget --accept-source-agreements --accept-package-agreements`. A shell opened before an install does not see
the new program until it is opened again (the PATH is read at start).

| Tool | What it does here | Install | Check |
|---|---|---|---|
| Windows 11, 64-bit | The platform. axKit runs on Windows 10 1809 and later; the development computer is Windows 11 | - | `winver` |
| winget (App Installer) | Installs the rest, and upgrades it (`winget upgrade`) | Comes with Windows | `winget --version` (1.29) |
| Windows PowerShell 5.1 | Every script of the repository: `run.ps1`, `test.ps1`, `publish.ps1`, `install.ps1`, `release.ps1`, `nvda-pass.ps1`, `manual_tests/prepare.ps1`, `tools/*.ps1`. Its pitfalls are in `CLAUDE.md` (Environment gotchas). PowerShell 7 is not needed; the workflows use it on GitHub's runners only | Comes with Windows | `$PSVersionTable.PSVersion` reads 5.1 |
| .NET SDK 10 | Build, test and publish; the only build dependency (`GOVERNANCE.md`): no NuGet package in the apps, the test packages alone | `Microsoft.DotNet.SDK.10` | `dotnet --version` is `global.json`'s version or a later patch of its feature band (10.0.300 today, `rollForward: latestFeature`) |
| Git, with Git Bash and Git Credential Manager | The repository; Git Bash is one of axTerminal's shells and the source of the `shell-*` fixtures | `Git.Git` | `git --version` (2.53); `git config user.email` is GitHub's no-reply address for the account, never a personal one; `git config credential.helper` reads `manager` |
| GitHub CLI, `gh` | `release.ps1` (waits for the build, pushes the tag), `tools/sign-release.ps1` (downloads the draft, checks the attestation, uploads the signature, publishes), `gh run watch`, and the repository's environment, secrets and variables (`docs/axKit/store-setup.md`) | `GitHub.cli` | `gh --version` (2.96); `gh auth status` is signed in as the repository's owner, whose GitHub account has two-factor sign-in |
| NVDA, with the add-on `axkitCapture` | Every manual check; `nvda-pass.ps1` and `tools/AxAutomate --nvda` read what NVDA speaks through the add-on (`docs/axKit/automation.md` section 3). NVDA refuses to be started from the assistant's process, so the user starts it | `NVAccess.NVDA` (2025.3); the add-on is the folder `tools/nvda-capture` copied to `%APPDATA%\nvda\addons\axkitCapture`, then NVDA restarted | NVDA is running; `nvda-pass.ps1` stops with a message when the add-on is missing or differs from `tools/nvda-capture` |
| Claude Code, `claude` | axClaude's program (`claude --ax-screen-reader` in a pseudo console), and the assistant that works in this repository from `CLAUDE.md`; `tools/QaRun` and `tools/AxAutomate` drive it without windows, always with `--model haiku`. axKit's MCP server is registered in it as `axkit` from any window's axKit menu, AI tools | Anthropic's installer, `irm https://claude.ai/install.ps1 \| iex`, into `%USERPROFILE%\.local\bin` (the folder axKit's console commands use too) | `claude --version` (2.1); `claude mcp list` names `axkit` once the AI tools are on |
| KeePassXC, with `keepassxc-cli` | axPass's program, and its NVDA pass (`manual_tests/prepare.ps1 axPass` makes the test database) | `KeePassXCTeam.KeePassXC` (2.8) | `keepassxc-cli --version` |
| Windows Terminal | The maintainer's console; nothing in the repository needs it | `Microsoft.WindowsTerminal` | - |
| Windows SDK packaging tools, `makeappx.exe` | `publish.ps1 -Msix` packs the MSIX package for the Microsoft Store (`docs/axKit/store-distribution.md`). The GitHub runners have the Windows SDK; this computer has none, so `tools/get-sdk-tools.ps1` fetches Microsoft's package `Microsoft.Windows.SDK.BuildTools` (a pinned version) from nuget.org once into `tools/sdk`, checking Microsoft's signature on every program and library; git ignores the folder, and deleting it removes the tools | `publish.ps1 -Msix` runs the fetch itself when Windows Kits has no makeappx; or `.	oolsget-sdk-tools.ps1` | `toolssdk<version>dmakeappx.exe` exists; the script prints its path |

## 2. Sign-ins, keys and where they live

Nothing of this is in the repository, and nothing of it is written into a session's output or a document.

- **GitHub**: `gh auth login` (HTTPS, through the browser) as the repository's owner; `gh` keeps its token in Windows'
  credential store, and git pushes through Git Credential Manager with the same account. The repository
  `KyleKeane/AxKit` has the environment `release`, which only the branch `main` and tags `v*` may use, with the
  secret `AXKIT_REPORTER_TOKEN` (`docs/axKit/reports-setup.md`). `gh secret list --env release` shows the names.
  (The three `AZURE_*` secrets of the retired signing were deleted on 2026-11-09, `docs/axKit/code-signing.md`
  section 7.)
- **Partner Center**: the maintainer's personal Microsoft account, in a browser only (the name reservation and every
  submission; `docs/axKit/store-setup.md` sections 3 and 5). `tools/EdgeDrive` drives that signed-in browser, an Edge
  window on a profile of its own, `%LOCALAPPDATA%\AxKit-release\edge-profile`, and holds no credential (section 6
  there). No program of the repository signs in to Partner Center: the route through Microsoft's submission API,
  with a Microsoft Entra application's three values as secrets, was built, never switched on, and removed on
  2026-11-13, since the account is an Individual account (section 2 there).
- **The release key**: `%LOCALAPPDATA%\AxKit-release\release-key.bin`, an ECDSA P-256 key encrypted for the Windows
  account that made it (`tools/release-key.ps1`; SPEC AX-3.4). `release.ps1` signs every release's zip with it; the
  public half is built into `AxKit.Core/Updates/ReleaseSigning` and `install.cmd`. A new computer gets it only by the
  maintainer's own copy; a copy of the file alone is useless under another account. Losing it means one release
  installed by hand with a new key's public half in it.
- **Claude Code**: its own sign-in, the maintainer's Anthropic account.
- **Windows**: the development computer's account is the maintainer's; a second computer, with Smart App Control on,
  is where the Store's copy and its updates are checked for real (`docs/axKit/store-distribution.md`).

## 3. What runs where

| Task | Command | Tools it uses |
|---|---|---|
| Build and start one app | `run.ps1 -App <app> [<path>]`, `run-ax<app>.ps1` | .NET SDK, PowerShell. A running app locks its `bin`: a change is checked from a build elsewhere (`dotnet build <project> -o <scratch dir>`), and the development axStart runs from `src\AxKit\bin\Debug` on this computer, where no axKit is installed |
| Tests | `test.ps1 [<App>]` | .NET SDK: every test project in Release, one by one, as the workflows do. Before every commit |
| The outside-calls page | `tools/outside-calls.ps1` (`-Check`) | PowerShell; the sources' `Outside call` comments (SPEC B-17) |
| The repositories beside this one | `tools/sync-repos.ps1` (`-Status`); git runs it on every push through `tools/hooks/pre-push` | git. `..\axkit-reports` (the clone of the public reports repository: the issue forms, the workflow, the blocklist) and `..\axkit-management` (the private repository of axTask's lists: the issues read from GitHub and the maintainer's priorities, `CLAUDE.md` rule 9) are fetched, the lists committed as one dated commit, pulled fast-forward and pushed; one line each, nothing forced, a sibling's trouble never stops the push. A clone without the two folders beside it skips them |
| NVDA passes | `manual_tests/prepare.ps1 [<app>]`, then `nvda-pass.ps1 [<app>]` after the user has stepped away | NVDA with the add-on, `tools/AxAutomate`; takes the keyboard |
| Automated checks and Claude sessions | `tools/AxAutomate <script>`, `tools/QaRun` | .NET SDK, Claude Code with `--model haiku` |
| Publish and install locally | `publish.ps1 [-NoInstall]`, `install.ps1 [-Uninstall]` | .NET SDK; not on the development computer, which keeps no installed copy |
| The MSIX package for the Store | `publish.ps1 -Msix` | .NET SDK, `tools/make-manifest.ps1`, `tools/make-icon.ps1 -Png`, makeappx (Windows Kits, or `tools/sdk` through `tools/get-sdk-tools.ps1`). A local test registers the published folder as the package, `Add-AppxPackage -Register publishmsixpackageAppxManifest.xml`, which needs Developer Mode (Settings, System, For developers); `Remove-AppxPackage` removes it |
| The Store submission | No command: after `release.ps1`, the release's `AxKit-<version>.msix` is uploaded in Partner Center's website by hand, or with `tools/EdgeDrive` driving the maintainer's signed-in Edge window | A browser, `tools/EdgeDrive` (`docs/axKit/store-setup.md` sections 5 and 6) |
| Release | `release.ps1 <version>` after a `## <version>` section in `CHANGELOG.md`. Rehearse before a release: `release.ps1 -Rehearse` makes the checks and asks git and GitHub the real run's questions without releasing (`docs/axKit/commands.md`) | git, gh, `test.ps1`, `tools/outside-calls.ps1`, the .NET SDK; then GitHub Actions (`release.yml`: build, package, draft) and `tools/sign-release.ps1` with gh and the release key |
| The Windows signature | None on the GitHub release's `AxKit.exe`; the Store signs its package after certification (`docs/axKit/store-distribution.md`). The Azure signing was retired on 2026-11-09 (`docs/axKit/code-signing.md` section 7) | Partner Center |

## 4. A new computer, in order

1. Windows 11 with winget. Turn two-factor sign-in on for the GitHub account if it is not on yet.
2. `winget install` of `Git.Git`, `GitHub.cli`, `Microsoft.DotNet.SDK.10`, `NVAccess.NVDA`,
   `KeePassXCTeam.KeePassXC`; Claude Code with Anthropic's installer. Open a new shell.
3. `git clone https://github.com/KyleKeane/AxKit` into a folder of the maintainer's choosing, as `axkit`, and beside it
   `git clone https://github.com/KyleKeane/AxKit-reports axkit-reports` and
   `git clone https://github.com/KyleKeane/axkit-management` (private; the maintainer's account): the three folders of
   axKit's development, which `tools/sync-repos.ps1` finds by name. In the code's clone, `git config user.name` and
   `git config user.email` with GitHub's no-reply address, and `git config core.hooksPath tools/hooks`, so that every
   push also syncs the two beside it (`release.ps1` refuses to run until it is set) and every commit names its item
   of the priorities list and marks it started or done there (`tools/hooks/commit-msg`, `tools/item-of-commit.ps1`;
   the root `CLAUDE.md`, rule 9).
4. `gh auth login` as the owner. `gh auth status` says so.
5. (Nothing since 2026-11-13: this step set up the Azure CLI for code signing, which was retired on 2026-11-09 and
   whose Azure resources are deleted, `code-signing.md` section 7. The Azure CLI is not needed.)
6. Copy `tools/nvda-capture` to `%APPDATA%\nvda\addons\axkitCapture`, restart NVDA.
7. `dotnet build AxKit.sln`, then `.\test.ps1`: every project green. `.\manual_tests\prepare.ps1` makes the files of
   the NVDA passes.
8. To release from this computer: the release key, brought over by the maintainer and protected again for this
   account (`tools/release-key.ps1`), and `gh` signed in. `tools/release-key.ps1 public` prints the public half, which
   must equal the one in `AxKit.Core/Updates/ReleaseSigning`.
9. Claude Code opened in the repository's folder reads `CLAUDE.md`; the AI tools of axKit are turned on from any
   window's axKit menu, AI tools, when wanted, which registers the `axkit` MCP server in Claude Code.

## 5. Gotchas of this computer, and keeping it current

- The computer is a Hyper-V virtual machine with 7 GB of memory, seven logical processors and no graphics adapter
  (Hyper-V Video and the Remote Display Adapter of the enhanced session), so the Desktop Window Manager composes every
  frame on the CPU, at the enhanced session's 2880 by 1800 pixels. Measured on 2026-11-08 from the process's own
  clock (the `\Process(dwm)` performance counter named the wrong one of the two dwm processes and read zero): with the
  Windows Magnifier running in full-screen mode at 100 percent with colours inverted, dwm used 70 to 90 percent of one
  core all the time, the screen static or not, and had used 68 hours of CPU in seven days; the Magnifier's own process
  used nothing, since full-screen magnification is done inside dwm. Closing the Magnifier (Win+Esc) brought dwm to
  about a quarter of a core at once; what is left is the enhanced session's own display pipeline and NVDA's focus
  highlight (a full-screen layered window, Vision settings), not yet told apart. Stopping the caret from blinking
  changed nothing. What keeps it light, in order of effect: no full-screen Magnifier unless it is wanted (Win+Plus
  brings it back), a smaller display in the Connect dialog of Hyper-V Manager (the Display configuration slider,
  "Save my settings"; 1280 by 800 is a quarter of the pixels), NVDA's Vision settings without the focus highlight
  unless it is wanted, and the user's visual effects off (transparency, animations, fades and shadows, a solid-colour
  background; set on 2026-11-08 through `SystemParametersInfo` and the user's registry, "Adjust for best performance"
  with font smoothing kept). GPU partitioning on the host (`Add-VMGpuPartitionAdapter`) would make dwm
  hardware-accelerated and is the structural fix.
- Memory on the same computer: a build leaves six MSBuild nodes and the Roslyn compiler server idle for ten to fifteen
  minutes, about 1 GB together; `Directory.Build.rsp` ends the nodes with the build and the scripts run
  `dotnet build-server shutdown` after theirs (run it after a bare `dotnet build`). Edge's startup boost kept seven
  Edge processes resident with no window (460 MB), and the Start menu's web search kept six WebView2 processes under
  SearchHost (450 MB): the first is off in Edge's own files and its sign-in entry, both are off for good through the
  Edge and Explorer policies, which an administrator sets (the user-level Policies keys are protected from the user).
  With 7 GB Windows runs every service in a process of its own (89 service hosts, 1.2 GB); raising
  `SvcHostSplitThresholdInKB` above the installed memory groups them again after a restart. Defender exclusions for
  the build processes and the `bin` and `obj` folders take Defender out of every build. SysMain and DiagTrack can go.
- Windows Magnifier, when it runs, takes Ctrl+Alt+H, K, L and M, which axClaude's reading keys use; check for
  `Magnify.exe` before an NVDA run of axClaude.
- NVDA's speech pausing on this computer (looked into 2026-11-08, no fault found in axKit's code): two separate
  causes, both outside axKit's control. First, NVDA 2025.3.3 is a 32-bit process (`nvda.exe` is not
  large-address-aware, so a 2 GB address-space ceiling) and sat around 840 MB of private bytes; its OneCore speech
  driver threw `Exception in OcSpeech::speak: bad allocation` (`build\x86\localWin10\oneCoreSpeech.cpp`, in `%TEMP%\nvda.log`)
  more than once, which drops the utterance being spoken and reads as a pause. With the VM's 7 GB of memory and about
  1.8 GB free, paging under load (`\Memory\Pages/sec` spiked to ~570) makes it worse; the memory levers in this
  section (no full-screen Magnifier, a smaller display, Edge and SearchHost off) leave NVDA more room. Second, NVDA's
  continuous reading (Say all) over axClaude's live transcript aborts with `RuntimeError: Could not clone range` and
  `[WinError 1775] A null context handle` (UIA, `speech\sayAll.pyc` `updateCaret`) when Claude's output arrives and
  moves the caret while NVDA is reading it: NVDA moves the caret to follow its reading, the incoming output moves it
  too, and the two collide and cut the utterance. Measured against axClaude: the transcript control answers NVDA in
  under 8 ms (`WM_GETTEXTLENGTH` round trips, median 0.4 ms), and it raises about four accessibility events a second
  while Claude streams (value-change and text-selection-change, the caret kept on the reader's line), so the window is
  not slow; the collision is inherent to reading a document that is being written. axClaude already holds output for
  250 ms after a key press and while a selection stands (`TranscriptView`), but a Say all has no key press to hang the
  hold on, so it was not covered until 2026-11-09, when the view started to hold output for 4 s after a caret move that
  neither a key nor the view made (the EM_SETSEL message NVDA sends as its Say all reads on; axClaude FR-3.3): output
  waits while the reading goes on and comes once it has stopped. Heard by the NVDA tier that night
  (`tests/nvda-scripts/axClaude-say-all.txt`); how it feels over a long reading is the user's to say. What helps besides: close and reopen NVDA when speech starts dropping (it
  clears the synth's memory), and New session or a fresh window keeps the transcript from growing toward its
  20 000-line cap, which NVDA re-reads. The structural fix is more memory for the VM or a 64-bit screen reader.
- `az` is a batch file: a `--query` with parentheses and no spaces, such as `length(@)`, is torn apart by `cmd`; put a
  space in the expression or count in PowerShell. A PowerShell string with `$name:` in it is a parse error; write
  `${name}:`.
- The Bash tool of the assistant mangles backslashes in heredocs and backticks in one-liners; PowerShell goes through
  the PowerShell tool, source files through the Write tool (`CLAUDE.md`, Environment gotchas).
- `winget upgrade` keeps the tools current; a new .NET SDK feature band is a change to `global.json` and the
  workflows, in a commit of its own (`docs/axKit/longevity.md`); the yearly check there covers the signing too
  (`docs/axKit/code-signing.md` section 5). A tool's version in this page is updated when it changes the band.
