# Privacy

axKit collects nothing. It has no telemetry, no analytics, no crash reporting, no accounts and no advertising. The
developer receives no data from it, and has no server it could send data to. Everything below is checked against the
code; a change to what axKit sends anywhere changes this page in the same commit. `docs/axkit/outside-calls.md` in the
repository is the exhaustive list behind this page, written from the sources: everything axKit reaches outside its own
process, every network request, program start and pipe in the code, by what it reaches, with each call's file, line and
purpose.

## What axKit sends over the network on its own

axKit's own code makes network requests in two places, the updater (`src/AxKit.Core/Updates`) and the report sender
(`src/AxKit.Core/Reports`), over HTTPS, to GitHub only. No other code in axKit makes a network request or listens for
one; the other addresses in the code are the Help menus' links and the installers' addresses below. The updater's two
requests, the first a setting; the report sender's only when you send a report:

- **The update check.** When an axKit window starts, it asks GitHub for the latest release of this repository: one GET
  of `https://api.github.com/repos/KyleKeane/AxKit/releases/latest` with the headers `User-Agent: AxKit/<version>` and
  `Accept: application/vnd.github+json`. It carries the version you have and, as every web request does, your network
  address; nothing else. GitHub's privacy statement applies to it:
  <https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement>. Turn it off in any window:
  Options, General, Check for updates at start. You can still check by hand (axKit menu, Check for updates).
- **The update you choose.** axKit downloads the release's zip and its signature from
  `https://github.com/KyleKeane/AxKit/releases/download/`, the only address the updater accepts a download from (a
  release whose files are elsewhere is reported as having no Windows zip). It installs the zip only if its SHA-256
  digest matches GitHub's digest of the file and the signature made with axKit's release key (ECDSA P-256), and only for
  your Windows account, without administrator rights.
- **A report you send** (F4 in any window, or axKit menu, Send a report; `docs/axkit/reports.md`). Your report is
  saved on your computer first, as an item of the axTask list under axKit menu, Your reports. Then it is posted as a
  public issue of the reports repository, `https://github.com/KyleKeane/AxKit-reports`, with one `POST` to
  `https://api.github.com/repos/KyleKeane/AxKit-reports/issues`, after one `GET` of that repository's
  `blocklist.json` from `raw.githubusercontent.com`, which says whether reports from this install or version are
  blocked. The issue holds exactly what the report notice showed you and let you edit: your words, the kind, the
  app and version with Windows' build and NVDA's version, the window's title, the window's last twenty actions and
  announcements, where the focus was (a control's name and place, never its text), the log's last lines, and a title;
  paths of your account read `<user>` before you see them. It is posted by axKit's own GitHub account, not yours, and
  carries a random id of this install (`%APPDATA%\AxKit\reports.json`; delete the file for a new one), which names
  the install and nothing else; it carries no name, e-mail address or account of yours unless you write one into the
  report. The first report asks your agreement once. A report that cannot be sent (no network, a blocked install, a
  build without the sending token) stays in Your reports as "Not sent yet" with the reason, and goes with the next
  one; the notice then also offers to copy the report or to open GitHub's own form in your browser. GitHub's privacy
  statement applies to what is posted: <https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement>.

## What axKit does on the network only when you ask

The access layers (axClaude, axGIT, axTerminal, axPass) are the other way axKit meets the network. An access layer
starts a program you installed as a child process of its window and talks to it through a pseudo console or pipes on
your computer, never over the network; that program uses the network as it would without axKit, under its maker's
terms, and axKit neither sees nor changes that traffic: it reads what the program prints and passes on what you type.

- **Help pages.** Each app's Help menu has links to documentation (Microsoft, GitHub, CommonMark, JSON, git, KeePassXC,
  Claude Code). Choosing one opens it in your browser; axKit itself fetches nothing.
- **axClaude, access layer for Claude Code** runs `claude --ax-screen-reader`, Anthropic's program, which you install
  yourself, in a pseudo console. Claude Code talks to Anthropic's service with your account, and does whatever else it
  does from a terminal, its own update check included. What you type in axClaude goes to Claude Code and, through it,
  to Anthropic under Anthropic's own terms and privacy policy (<https://www.anthropic.com/legal/privacy>). axKit does
  not read, keep or send your conversation anywhere else; it shows what Claude Code prints, and records it to a file
  only when you turn recording on (Options, Developer).
- **axGIT, access layer for Git** runs git, your own installed program, one process per command, with its prompts
  turned off (`GIT_TERMINAL_PROMPT=0`). Three commands reach a remote (GitHub, or wherever your repository's remotes
  point): fetch, pull and push, each only when you choose it and, the first time axKit reaches that server, after your
  consent; git's credential helper may then ask you to sign in, in its own window, and that account is between you and
  the server. Every other command reads or changes the repository on disk.
- **axTerminal, access layer for your shell** runs your own shell (PowerShell, Command Prompt, Git Bash or PowerShell 7)
  in a pseudo console. What the commands you type do, on your computer and on any network they reach, is up to them.
- **axPass, access layer for KeePassXC** runs `keepassxc-cli.exe`, KeePassXC's command-line tool, on your database
  file, with your master password on a pipe. The tool has no network feature.
- **Install it now.** When an app needs a program that is missing (Git, KeePassXC, Claude Code, PowerShell 7), its
  notice offers to install it, and does so only when you choose to. Git (`Git.Git`), KeePassXC
  (`KeePassXCTeam.KeePassXC`) and PowerShell 7 (`Microsoft.PowerShell`) come through Windows' own package manager,
  winget, from its community repository alone (`--source winget`), which downloads each maker's installer. Claude Code
  comes from Anthropic's own installer (`irm https://claude.ai/install.ps1 | iex`). Each program then has its maker's
  terms.
- **Links you open**: a link in axTask's fields, in a Markdown file or in a notice opens in your browser when you press
  Enter on it.
- **axFile** works on the folders you open, network shares among them when you open one, through Windows' file system
  as File Explorer does. Its Open with, Properties and Windows menu hand the file to Windows and to the programs you
  choose.

## The AI tools (off unless you turn them on)

axKit menu, AI tools, Let AI tools read and open axKit. When it is on, an AI tool on your computer that you pair, such as
Claude Code, can read what axKit's windows show and open apps (`docs/axkit/mcp.md`). This happens through named pipes
for your Windows account alone, never over the network; what the tool then does with what it read is under its own
terms (Claude Code: Anthropic's). axPass and axClip are never read. Every call is written to `mcp-audit.log` on your
computer, with its arguments hashed. Forget AI tools removes every pairing; turning it off ends access at the next
call.

## What stays on your computer

- **Settings**: `%APPDATA%\AxKit`, one file per app.
- **Logs**: `%LOCALAPPDATA%\AxKit\logs`. They hold starts, errors and what an update did; never what you type, copy,
  read or say to Claude. Each rolls over at 1 MB.
- **axClip** keeps its history in memory; on disk only when you choose Keep history, and then encrypted for your
  Windows account.
- **axPass** never stores your passwords: KeePassXC's own tool reads and writes your database, and the master password
  goes to it on a pipe. A copied password is marked private for Windows' clipboard history and cleared after 30
  seconds.
- **Copy diagnostics** (Options, Developer) and **Copy details** (a failed update's notice) put text on your clipboard
  only when you choose them, with your account's name and folders, your computer's name and any e-mail address
  replaced, for you to read before you share it.

## Programs axKit runs

axKit starts only Windows' own programs by their full paths (PowerShell for the installer, File Explorer, cmd for
batch files), the programs you installed and chose (Claude Code, git, your shells, KeePassXC's tool, the programs of
Open with), and itself. It never runs anything with administrator rights; an installer you choose in Install it now
may ask Windows for them, and Windows asks you.

## Children, changes, contact

axKit is not directed at children and collects nothing from anyone. This page changes when axKit's behaviour changes; the
history is in this repository. Questions: open an issue
(<https://github.com/KyleKeane/AxKit/issues>), or for a security matter a private advisory, as `SECURITY.md` says.
