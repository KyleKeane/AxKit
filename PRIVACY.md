# Privacy

axKit collects nothing. It has no telemetry, no analytics, no crash reporting, no accounts and no advertising. The
developer receives no data from it, and has no server it could send data to. Everything below is checked against the
code; a change to what axKit sends anywhere changes this page in the same commit. `docs/axKit/outside-calls.md` in the
repository is the exhaustive list behind this page, written from the sources: everything axKit reaches outside its own
process, every network request, program start and pipe in the code, by what it reaches, with each call's file, line and
purpose.

## What axKit sends over the network on its own

axKit's own code makes network requests in two places, the updater (`src/AxKit.Core/Updates`) and the report sender
(`src/AxKit.Core/Reports`), over HTTPS, to GitHub only. No
other code in axKit makes a network request or listens for one; the other addresses in the code are the Help menus'
links and the installers' addresses below. The updater's two requests, the first a setting; the report sender's only
when you send a report:

- **The update check.** When an axKit window starts, it asks GitHub for the latest release of this repository: one GET
  of `https://api.github.com/repos/KyleKeane/AxKit/releases/latest` with the headers `User-Agent: AxKit/<version>` and
  `Accept: application/vnd.github+json`. It carries the version you have and, as every web request does, your network
  address; nothing else. GitHub's privacy statement applies to it:
  <https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement>. Turn it off in any window,
  for every app at once: axKit menu, Check for updates at start. You can still check by hand (Help, Check for updates). A copy
  installed from the Microsoft Store makes no check at start and has no such setting; Check for updates by hand makes
  the same one request, and its notice opens axKit's page in the Store app, which is Microsoft's and runs under
  Microsoft's privacy statement, <https://privacy.microsoft.com/privacystatement>, as the Store's own updating of the
  package does. That copy downloads nothing itself.
- **The update you choose.** axKit downloads the release's zip and its signature from
  `https://github.com/KyleKeane/AxKit/releases/download/`, the only address the updater accepts a download from (a
  release whose files are elsewhere is reported as having no Windows zip). It installs the zip only if its SHA-256
  digest matches GitHub's digest of the file and the signature made with axKit's release key (ECDSA P-256), and only for
  your Windows account, without administrator rights.
- **A report you send** (F4 in any window, or Help, Send a report; `docs/axKit/reports.md`). Your report is
  saved on your computer first, as you wrote it, as an item of the axTask list under Help, Your reports. Then it is
  encrypted on your computer, with the public half of a key whose private half only axKit's maintainer has, on his
  own computer (a fresh AES-256-GCM key for each report, wrapped with RSA 3072 OAEP-SHA256), and the encrypted text
  is posted as an issue of the reports repository `KyleKeane/AxKit-reports`, which is private: only the maintainer
  can open it, and only his key can read a report. The report sender contacts two addresses of GitHub. First the
  block list: one `GET` of `https://raw.githubusercontent.com/KyleKeane/AxKit-blocklist/main/blocklist.json`, a file
  of a public repository that holds nothing but the list of random install ids and axKit versions whose reports are
  not accepted; a read without a token that sends nothing of yours but the request itself (the version you have in
  the User-Agent header and, as every web request does, your network address), made at most once a day. If the
  list names your install or your version, nothing is posted, and a notice says so, with your install id and, for a
  blocked install, the page where to ask if it is a mistake, `https://github.com/KyleKeane/AxKit-blocklist/issues`,
  which opens in your browser only if you choose it. Then the report: one `POST` to
  `https://api.github.com/repos/KyleKeane/AxKit-reports/issues`. GitHub, a Microsoft company, stores the encrypted
  text and sees when and from which network address it was sent. In the clear, beside the encrypted text, the issue holds
  only: a random id of this install, axKit's version, the report's kind, a short fingerprint of the maintainer's
  public key and the name of the encryption method; its title is the kind and the version, never your words. The
  maintainer may quote or summarise a report in axKit's public repository when fixing what it reports, without
  anything that identifies you. Copies of axKit up to 5.8.0 send their reports unencrypted into the same repository,
  which was public until 2026-11-14, and their own texts still say "public"; they ask for the block list at its old
  address in that repository, which now answers that nothing is there, so their own check finds an empty list. The encrypted report holds exactly what the report notice showed you and let you edit: your words, the kind, the
  app and version with Windows' build and NVDA's version, the commands you last ran in the window, each by its name and key and never with what you typed (a recent file or a window chosen from a list is written by its menu and its number, "Open recent file, entry 2"), the kind of control the focus was in and its
  place (never its name, an item's name or its text), the log's errors of the last day with every path and every quoted text masked, and a title.
  Nothing of this names your files, folders, lists or items. Only if you check the box "Add detail that may name your files" does the report also
  carry the window's title, the focused control's and item's names, what the window announced and the notices it showed (never in axClaude,
  axTerminal, axPass and axClip, which record no announcements), and the log's last lines as they are;
  paths of your account read `<user>` before you see them. It is posted with the maintainer's reporter token, never with an account of yours, and
  carries a random id of this install (`%APPDATA%\AxKit\reports.json`; delete the file for a new one), which names
  the install and nothing else; it carries no name, e-mail address or account of yours unless you write one into the
  report. The first report asks your agreement once. A report that cannot be sent (no network, a blocked install, a
  build without the sending token) stays in Your reports as "Not sent yet" with the reason, and goes with the next
  one. Only reports that axKit's report form saved are sent: any other file in the reports folder stays where it is;
  where this copy cannot send at all, the notice offers to copy the report, not encrypted, to your own
  clipboard. GitHub's privacy statement applies to what is posted: <https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement>.

axTask is local only and makes no network request. A list can keep a history of its changes, off until you turn it
on in Options for that list: a file beside the list, on your computer, that nothing sends anywhere. A list's own actions can start a program: an action written in
the list's configuration file (`list.json`) may name a program on your computer, which axTask runs when you choose
the action, with what the selected item holds as its arguments. axTask asks you before such an action's first run and
after every change to it, shows the program and every argument, and does not know what the program does or reaches:
a program that contacts a server does so by itself, as it would from a terminal (`docs/axTask/forms-framework.md`
section 7). An action can also put a command into axTerminal's command field: axTerminal opens with it there and
never sends it; you read it and press Enter yourself, and what the command then reaches is its own doing. Versions 5.1.0 to 5.8.0 had GitHub lists in axTask, which read a
repository's issues and could change one with a token of your own GitHub account; they were removed
(`docs/axKit/reports.md` RP-7). A token such a version kept is no longer read or sent; its file,
`%LOCALAPPDATA%\AxKit\github.token`, protected for your Windows account, is yours to delete, and so is the token
itself on GitHub.

## What axKit does on the network only when you ask

The access layers (axClaude, axGIT, axTerminal, axPass, axMCP) are the other way axKit meets the network. An access layer
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
  While you have Options, Developer, Events from Claude Code's hooks turned on (it is off at first), axClaude also asks Claude Code to start `AxKit.exe hook` for eight kinds of events of it (the
  session started or ended, a turn stopped or failed, an agent started or stopped, a permission was asked, a
  notification): that process tells the axClaude window, over a pipe on your computer that only your Windows account
  may open, that the event happened, and ends. The hooks are registered for that one start of Claude Code with a file
  of axKit's own, never in a settings file of yours or of a project, and they only observe: none answers, allows or
  changes anything Claude Code does. This sends nothing anywhere, axKit still listens on no port, and nothing an
  event carries enters a log, a report or the diagnostics. Options, Developer, Events from Claude Code's hooks turns
  it off (`docs/axClaude/events-and-grounding.md`).
  axClaude also reads the session file Claude Code keeps for the conversation (in Claude Code's `projects` folder in
  your profile), in memory only, to check and order what it shows. This sends nothing anywhere either, and nothing
  of that file enters a log, a report or the diagnostics. Options, Developer, Ground the conversation in Claude
  Code's session file turns it off.
  A second way to run Claude Code is being built and is off unless you turn it on (Options, Developer, Feed the next
  window from Claude Code's stream; `docs/axClaude/stream-mode.md`): Claude Code as one process without
  a console, which takes your messages and writes what happens, line by line, over pipes on your computer. Claude
  Code talks to Anthropic exactly as it does the other way; axKit adds no network contact. Run this way, Claude Code
  does not draw its own questions, so it starts `AxKit.exe ask`, a process without a window, and asks it before a
  tool call that needs your word and for Claude's own questions: that process passes each question to the axClaude
  window, over a pipe on your computer that only your Windows account may open, and passes your answer back. Where
  the window cannot be asked, its answer is no. It is named for that one start of Claude Code with a file of
  axKit's own, never in a settings file of yours or of a project. Nothing of a question or an answer enters a log,
  a report or the diagnostics. Before Claude Code is started this way in a folder, axClaude asks whether you trust
  the folder, since Claude Code itself does not ask then.
- **axGIT, access layer for Git** runs git, your own installed program, one process per command, with its prompts
  turned off (`GIT_TERMINAL_PROMPT=0`). Three commands reach a remote (GitHub, or wherever your repository's remotes
  point): fetch, pull and push, each only when you choose it and, the first time axKit reaches that server, after your
  consent; git's credential helper may then ask you to sign in, in its own window, and that account is between you and
  the server. Every other command reads or changes the repository on disk.
- **axTerminal, access layer for your shell** runs your own shell (PowerShell, Command Prompt, Git Bash or PowerShell 7)
  in a pseudo console. What the commands you type do, on your computer and on any network they reach, is up to them.
- **axPass, access layer for KeePassXC** runs `keepassxc-cli.exe`, KeePassXC's command-line tool, on your database
  file, with your master password on a pipe. The tool has no network feature.
- **axMCP, access layer for MCP servers** starts an MCP server you choose, from Claude Code's configuration
  (`%USERPROFILE%\.claude.json`, a folder's `.mcp.json`, which axMCP reads and never writes) or from your own list, as a
  child process on this computer, and talks to it on its standard input and output; the first time, after your
  agreement. The server is its maker's program: it does whatever it was built to do, on this computer and on any
  network it reaches, with your rights, under its maker's terms. Remote servers (http, sse) are listed and not
  started; axMCP itself makes no network request.
- **AI help** (`docs/axKit/ai-help.md`; off until your first question, when a reminder asks you to turn it on; the
  switch is the axKit menu, Claude, Use Claude in axKit) runs `claude -p`,
  Claude Code in its print mode, as a child process with read-only tools, once per question, in a working folder of its
  own (`Documents\axKit\Claude` unless `ai-help.json` names another; Claude Code keeps its own notes and sessions for
  the service there and reads a `CLAUDE.md` you write there), and reads its answer from its standard output. What goes
  to Claude Code and, through it, to Anthropic under your account and Anthropic's terms:
  your question; for Shift+F1 in an axKit window, that window's name and title, the name and place of the control you
  are in, your last actions there, the window's keyboard shortcuts and its user guide, never the text of what you are
  working on; and the files Claude reads in its working folder (axKit's own guides are there, public texts) and in the
  folders you listed under Folders Claude may read, none by default. Such a question starts Claude Code with the
  servers a folder's `.mcp.json` names left out and with its hooks turned off for that run (`--strict-mcp-config`, and
  `--settings` on axKit's file `%LOCALAPPDATA%\AxKit\claude-no-hooks.json`, which holds that one switch and nothing of
  yours), so that no file in the working folder starts a program or reaches a server when you ask.
- **axHelp, access layer for Claude Code** is a conversation in axClaude (above) on Claude's working folder,
  `Documents\axKit\Claude`, started with a model of axHelp's own; what goes to Anthropic is what axClaude sends.
  Closing that conversation hides it, and Claude Code keeps running until you end it there (Session, End Claude and
  close this window) or quit axHelp. Claude Code starts for it at your first press of the key, never before you act, unless
  you turn on Options, Keep the conversation ready in the background, in axHelp's window, which is off at first. With
  it on, axHelp started in the background, as at sign-in, also starts Claude Code for that conversation, hidden, so
  that it is ready at your first key: the one case in which axKit starts Claude Code without your key press. Claude
  Code may contact Anthropic as it starts (its sign-in and its own update check; none of your words or files, which
  go only with a message you send). Even with the setting on it happens only after you have agreed to Claude Code in
  axClaude's reminder. Its
  screenshot key takes a picture of the window in front into that folder's `screenshots` folder (the twenty newest are
  kept) and puts its path on the clipboard and into the conversation's message field; Claude Code reads the picture
  only when you send the message. A picture file of yours (Ctrl+D, or `axHelp <picture>`) is passed by its path and
  read where it is. axHelp writes two files into that folder once, where they are missing: `CLAUDE.md`, Claude's
  instructions, and `.claude\agents\agent.md`, a helper on the strongest model; both are yours to change. axKit also
  keeps its own guides there, in the folder `documentation`, with a note for Claude in `.claude\rules`, and puts
  them back as axKit has them at every use, so a change you make to one of them does not stay: they are axKit's public texts, hold nothing of yours, and Claude Code reads them on
  this computer when a question is about axKit; what it reads for an answer goes to Anthropic as any file it reads.
  Before axHelp starts its conversation, axKit looks in that folder for files Claude Code would obey that axKit did not
  write (a `.mcp.json`, hooks in the folder's settings files, skills, commands, plugins, agents, rules), by their
  names and a hash, on this computer alone. When one is new or changed, a notice names them and Claude Code starts
  only when you choose Start anyway and remember these files; nothing of this is sent anywhere.
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
Claude Code, can read what axKit's windows show and open apps (`docs/axKit/mcp.md`). This happens through named pipes
for your Windows account alone, never over the network; what the tool then does with what it read is under its own
terms (Claude Code: Anthropic's). While the AI tools are on, axHelp's conversation starts Claude Code with them loaded: a file on your computer, `claude-mcp.json` in `%LOCALAPPDATA%\AxKit`, names this axKit as the program Claude Code starts for them; while they are off, nothing is loaded. This adds no network contact of axKit's: the tools are local pipes, and what Claude reads through them becomes part of the conversation, which goes to Anthropic as every conversation with Claude Code does. axPass and axClip are never read. A second switch, AI tools, Let AI tools run
commands, off unless you turn it on, lets a paired tool run a window's menu commands by name through the same pipes;
each is said in the window as it runs, one that acts outside the window asks you first, and none answers a question the
window asks you; with an axTask window open, the same switch lets the tool add an item to your axTask lists, said in the window, and the tool reads those lists through that window. It adds no network contact and no program of its own: a command does what it does when you run it. Every call is written to `mcp-audit.log` on your
computer, with its arguments hashed. Forget AI tools removes every pairing and turns Let AI tools run commands off;
turning the access off ends access at the next call and turns Let AI tools run commands off too.

## What stays on your computer

- **Settings**: `%APPDATA%\AxKit`, one file per app.
- **The settings' seal**: `%LOCALAPPDATA%\AxKit\settings-seal.key`, a key made on this computer and kept encrypted for
  your Windows account, with the names of the settings files it has sealed. With it axKit knows whether a setting that
  decides what is run or read was written by axKit. A file refused for a changed setting is kept beside the settings
  file with `.changed` at the end of its name.
- **The files you accepted in Claude's working folder**: `%APPDATA%\AxKit\ai-folder-files.json`, with the folder, each
  file's path and a hash of it, never a file's text.
- **The list of reports axKit wrote**: `%APPDATA%\AxKit\reports-written.json`, the file name and the id of each report
  the report form saved, so that only those are ever sent; a report's file name holds its title.
- **Logs**: `%LOCALAPPDATA%\AxKit\logs`. They hold starts, errors and what an update did; never what you type, copy,
  read or say to Claude. Each rolls over at 1 MB.
- **The log of events**, off unless you turn it on (axKit menu, Keep a log of events): `%LOCALAPPDATA%\AxKit\logs\events.log`,
  one line for each thing that reached you (a refusal, a line arriving, a turn done), its time and its kind, never a
  word of what was said, typed, shown or read, and no file name. Nothing is written while it is off, and turning it off
  leaves what was written until you delete the file. It rolls over at 1 MB into `events.old.log`, which replaces the one
  before. It stays on your computer; nothing sends it anywhere, and a report does not carry it.
- **The hooks' file**: `%LOCALAPPDATA%\AxKit\claude-hooks.json`, which axClaude writes before it starts Claude Code.
  It names this axKit as the program Claude Code starts for an event of that session (axClaude, above) and holds
  nothing of yours but that program's path.
- **The questions' file**: `%LOCALAPPDATA%\AxKit\claude-ask.json`, which axClaude writes before it starts Claude Code
  over its stream (axClaude, above). It names this axKit as the program Claude Code asks its questions of and holds
  nothing of yours but that program's path; nothing of the window's pipe is in it.
- **axClip** keeps its history in memory; on disk only when you choose Keep history, and then encrypted for your
  Windows account.
- **axPass** never stores your passwords: KeePassXC's own tool reads and writes your database, and the master password
  goes to it on a pipe. A copied password is marked private for Windows' clipboard history and cleared after 30
  seconds.
- **Copy diagnostics** (Help, Diagnostics) and **Copy details** (a failed update's notice) put text on your clipboard
  only when you choose them, with your account's name and folders, your computer's name and any e-mail address
  replaced, for you to read before you share it.

## Programs axKit runs

axKit starts only Windows' own programs by their full paths (PowerShell for the installer, File Explorer, cmd for
batch files), the programs you installed and chose (Claude Code, git, your shells, KeePassXC's tool, the MCP servers you start in
axMCP, the programs of Open with and of axStart, the programs that the actions of your axTask lists name, each asked
about before its first run, and the installers of Install it now), and itself. It never runs anything with administrator rights; an installer you choose in Install it now
may ask Windows for them, and Windows asks you.

One start of axKit is made by Claude Code, at axClaude's request: for an event of an axClaude session, Claude Code
starts `AxKit.exe hook`, a process without a window that passes the event to the axClaude window over the pipe named
above and ends, after two seconds at the latest. It starts nothing itself and prints nothing.

A second start of axKit is made by Claude Code, at axClaude's request, when you have Claude Code run over its stream
(axClaude, above): Claude Code starts `AxKit.exe ask`, a process without a window that lives as long as that Claude
Code does, passes each of its questions to the axClaude window over the pipe named above, and prints your answer
for Claude Code to read. It starts nothing itself and decides nothing: without an answer of yours, its answer is no.

Windows PowerShell also runs axPreview's own small script when a picture opens there, which asks Windows' text reader
(`Windows.Media.Ocr`, part of Windows) for the text in the picture: the reading happens on this computer and nothing
leaves it.

When you run a workflow in axAutomate (F5), axKit starts its own windows for the run on a desktop of the run's own,
with settings and logs in a folder of the run's under your temporary folder, and runs the command lines of the
workflow's `run` steps through Windows' cmd, by its full path, in that folder. Those commands act on your own desktop
and files as any command you type would, so before a run with one, axAutomate names them and runs them only if you
choose Run. A workflow also drives axKit's own windows, which reach your files as they do when you use them (axDown
saving a file, axFile's operations, axTerminal's commands), without a question: run only workflows you wrote or
trust. Nothing of the run reaches the network unless such a command does.

## Children, changes, contact

axKit is not directed at children and collects nothing from anyone. This page changes when axKit's behaviour changes; the
history is in this repository. Questions: open an issue
(<https://github.com/KyleKeane/AxKit/issues>), or for a security matter a private advisory, as `SECURITY.md` says.
