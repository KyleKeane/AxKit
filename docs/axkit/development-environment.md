# The development environment (2026-11-06)

What the maintainer's development computer has, what each tool does in this repository, how it is installed, signed in
and checked, and the order to set a new computer up in. Written so that a new computer, or a new assistant, reproduces
it without the maintainer's memory. The versions are the ones in use on the date above; what matters is the band each
belongs to, and the one place a version is pinned is `global.json` for the .NET SDK (`docs/axkit/longevity.md`).
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
| GitHub CLI, `gh` | `release.ps1` (waits for the build, pushes the tag), `tools/sign-release.ps1` (downloads the draft, checks the attestation, uploads the signature, publishes), `gh run watch`, and the repository's environment, secrets and variables (`docs/axkit/code-signing.md`) | `GitHub.cli` | `gh --version` (2.96); `gh auth status` is signed in as the repository's owner, whose GitHub account has two-factor sign-in |
| Azure CLI, `az`, with the extension `artifact-signing` | Reads the signing account, and would sign a file by hand if that were ever needed; the one-time setup of `docs/axkit/code-signing.md` section 2 was done with it. Not needed to build, test or release: the Windows signing runs in GitHub Actions | `Microsoft.AzureCLI`, then `az extension add --name artifact-signing` | `az version` (2.92); `az extension list` has `artifact-signing`; `az account show --query user.name -o tsv` names `axkit-signer`, the user that can only sign (code-signing.md section 1). The subscription's Owner is never left signed in here |
| NVDA, with the add-on `axkitCapture` | Every manual check; `nvda-pass.ps1` and `tools/AxAutomate --nvda` read what NVDA speaks through the add-on (`docs/axkit/automation.md` section 3). NVDA refuses to be started from the assistant's process, so the user starts it | `NVAccess.NVDA` (2025.3); the add-on is the folder `tools/nvda-capture` copied to `%APPDATA%\nvda\addons\axkitCapture`, then NVDA restarted | NVDA is running; `nvda-pass.ps1` stops with a message when the add-on is missing or differs from `tools/nvda-capture` |
| Claude Code, `claude` | axClaude's program (`claude --ax-screen-reader` in a pseudo console), and the assistant that works in this repository from `CLAUDE.md`; `tools/QaRun` and `tools/AxAutomate` drive it without windows, always with `--model haiku`. axKit's MCP server is registered in it as `axkit` from any window's axKit menu, AI tools | Anthropic's installer, `irm https://claude.ai/install.ps1 \| iex`, into `%USERPROFILE%\.local\bin` (the folder axKit's console commands use too) | `claude --version` (2.1); `claude mcp list` names `axkit` once the AI tools are on |
| KeePassXC, with `keepassxc-cli` | axPass's program, and its NVDA pass (`manual_tests/prepare.ps1 axPass` makes the test database) | `KeePassXCTeam.KeePassXC` (2.8) | `keepassxc-cli --version` |
| Windows Terminal | The maintainer's console; nothing in the repository needs it | `Microsoft.WindowsTerminal` | - |

## 2. Sign-ins, keys and where they live

Nothing of this is in the repository, and nothing of it is written into a session's output or a document.

- **GitHub**: `gh auth login` (HTTPS, through the browser) as the repository's owner; `gh` keeps its token in Windows'
  credential store, and git pushes through Git Credential Manager with the same account. The repository
  `KyleKeane/AxKit` has the environment `release`, which only the branch `main` and tags `v*` may use, with the
  secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID`, and, once signing is on, the repository
  variable `AZURE_CERTIFICATE_PROFILE` (`docs/axkit/code-signing.md`). `gh secret list --env release` and
  `gh variable list` show the names.
- **Azure**: `az login --use-device-code` as `axkit-signer`, the user that holds the signer role on the signing account
  and nothing else; the tokens live in `%USERPROFILE%\.azure`. The subscription's Owner, the maintainer's own account,
  is used in a browser, and signed in to `az` only on the maintainer's word for one administrative command, then
  `az logout`. The assistant's permission layer refuses role grants and identity creation; the maintainer runs such a
  script himself.
- **The release key**: `%LOCALAPPDATA%\AxKit-release\release-key.bin`, an ECDSA P-256 key encrypted for the Windows
  account that made it (`tools/release-key.ps1`; SPEC AX-3.4). `release.ps1` signs every release's zip with it; the
  public half is built into `AxKit.Core/Updates/ReleaseSigning` and `install.cmd`. A new computer gets it only by the
  maintainer's own copy; a copy of the file alone is useless under another account. Losing it means one release
  installed by hand with a new key's public half in it.
- **Claude Code**: its own sign-in, the maintainer's Anthropic account.
- **Windows**: the development computer's account is the maintainer's; a second computer, with Smart App Control on
  and axKit installed by `install.cmd`, is where updates and the Windows signature are checked for real
  (`docs/axkit/code-signing.md`).

## 3. What runs where

| Task | Command | Tools it uses |
|---|---|---|
| Build and start one app | `run.ps1 -App <app> [<path>]`, `run-ax<app>.ps1` | .NET SDK, PowerShell. A running app locks its `bin`: a change is checked from a build elsewhere (`dotnet build <project> -o <scratch dir>`), and the development axStart runs from `src\AxKit\bin\Debug` on this computer, where no axKit is installed |
| Tests | `test.ps1 [<App>]` | .NET SDK: every test project in Release, one by one, as the workflows do. Before every commit |
| The outside-calls page | `tools/outside-calls.ps1` (`-Check`) | PowerShell; the sources' `Outside call` comments (SPEC B-17) |
| NVDA passes | `manual_tests/prepare.ps1 [<app>]`, then `nvda-pass.ps1 [<app>]` after the user has stepped away | NVDA with the add-on, `tools/AxAutomate`; takes the keyboard |
| Automated checks and Claude sessions | `tools/AxAutomate <script>`, `tools/QaRun` | .NET SDK, Claude Code with `--model haiku` |
| Publish and install locally | `publish.ps1 [-NoInstall]`, `install.ps1 [-Uninstall]` | .NET SDK; not on the development computer, which keeps no installed copy |
| Release | `release.ps1 <version>` after a `## <version>` section in `CHANGELOG.md` | git, gh, `test.ps1`, `tools/outside-calls.ps1`, the .NET SDK; then GitHub Actions (`release.yml`: build, sign, draft) and `tools/sign-release.ps1` with gh and the release key |
| The Windows signature on `AxKit.exe` | Nothing local: the release workflow's sign job, in Azure, when `AZURE_CERTIFICATE_PROFILE` is set | GitHub Actions, azure/login, azure/artifact-signing-action |
| Azure administration | The maintainer in the Azure portal; `az` as the Owner only on the maintainer's word | Browser; Azure CLI |

## 4. A new computer, in order

1. Windows 11 with winget. Turn two-factor sign-in on for the GitHub account if it is not on yet.
2. `winget install` of `Git.Git`, `GitHub.cli`, `Microsoft.DotNet.SDK.10`, `Microsoft.AzureCLI`, `NVAccess.NVDA`,
   `KeePassXCTeam.KeePassXC`; Claude Code with Anthropic's installer. Open a new shell.
3. `git clone https://github.com/KyleKeane/AxKit` into a folder of the maintainer's choosing; `git config user.name`
   and `git config user.email` with GitHub's no-reply address.
4. `gh auth login` as the owner. `gh auth status` says so.
5. `az extension add --name artifact-signing`; `az login --use-device-code` as `axkit-signer`. `az account show` names
   that user.
6. Copy `tools/nvda-capture` to `%APPDATA%\nvda\addons\axkitCapture`, restart NVDA.
7. `dotnet build AxKit.sln`, then `.\test.ps1`: every project green. `.\manual_tests\prepare.ps1` makes the files of
   the NVDA passes.
8. To release from this computer: the release key, brought over by the maintainer and protected again for this
   account (`tools/release-key.ps1`), and `gh` signed in. `tools/release-key.ps1 public` prints the public half, which
   must equal the one in `AxKit.Core/Updates/ReleaseSigning`.
9. Claude Code opened in the repository's folder reads `CLAUDE.md`; the AI tools of axKit are turned on from any
   window's axKit menu when wanted, which registers the `axkit` MCP server in Claude Code.

## 5. Gotchas of this computer, and keeping it current

- Windows Magnifier, when it runs, takes Ctrl+Alt+H, K, L and M, which axClaude's reading keys use; check for
  `Magnify.exe` before an NVDA run of axClaude.
- `az` is a batch file: a `--query` with parentheses and no spaces, such as `length(@)`, is torn apart by `cmd`; put a
  space in the expression or count in PowerShell. A PowerShell string with `$name:` in it is a parse error; write
  `${name}:`.
- The Bash tool of the assistant mangles backslashes in heredocs and backticks in one-liners; PowerShell goes through
  the PowerShell tool, source files through the Write tool (`CLAUDE.md`, Environment gotchas).
- `winget upgrade` keeps the tools current; a new .NET SDK feature band is a change to `global.json` and the
  workflows, in a commit of its own (`docs/axkit/longevity.md`); the yearly check there covers the signing too
  (`docs/axkit/code-signing.md` section 5). A tool's version in this page is updated when it changes the band.
