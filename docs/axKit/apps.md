# The apps

Written by `tools/apps-page.ps1` from `Dispatch.Apps` (`src/AxKit.Core/Dispatch.cs`); never edit it by hand.
axKit has 17 apps, in the order every list of the windows shows them. A document that needs the apps
points here. The shared pieces every app stands on are `src/AxKit.Core` (pure code) and `src/AxKit.Forms` (the
window base and the shared controls), with their notes in each folder's `CLAUDE.md`; the executable is `src/AxKit`.
A new app is made with `tools/new-app.ps1` (`docs/axKit/adding-an-app.md`).

## Files

### axFile

- What: a file manager for one folder.
- command `axkit file`; opens a file or a folder; has a Start menu entry in the Store's copy.
- Code: `src/AxFile` (the window), `src/AxFile.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxFile.Tests`.
- Documents: `docs/axFile/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axFile/`, 13 scripts (their workflows are in `docs/axKit/workflows/axFile.md`).
- Run from the sources: `scripts to run-individual-apps/run-axFile.ps1`, or `run.ps1 -App file`.

### axDown

- What: a plain editor for Markdown and text files.
- command `axkit down`; opens a file; has a Start menu entry in the Store's copy.
- Code: `src/AxDown` (the window; its pure code is in `src/AxKit.Core`); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxKit.Tests`.
- Documents: `docs/axDown/` (`nvda-test-plan.md`, `SPEC.md`, `tutorial.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axDown/`, 18 scripts (their workflows are in `docs/axKit/workflows/axDown.md`).
- Run from the sources: `scripts to run-individual-apps/run-axDown.ps1`, or `run.ps1 -App down`.

### axJSON

- What: an editor for JSON files.
- command `axkit json`; opens a file.
- Code: `src/AxJSON` (the window), `src/AxJSON.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxJSON.Tests`.
- Documents: `docs/axJSON/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axJSON/`, 6 scripts (their workflows are in `docs/axKit/workflows/axJSON.md`).
- Run from the sources: `scripts to run-individual-apps/run-axJSON.ps1`, or `run.ps1 -App json`.

### axPreview

- What: a read-only viewer for text files.
- command `axkit preview`; opens a file.
- Code: `src/AxPreview` (the window), `src/AxPreview.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxPreview.Tests`.
- Documents: `docs/axPreview/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axPreview/`, 7 scripts (their workflows are in `docs/axKit/workflows/axPreview.md`).
- Run from the sources: `scripts to run-individual-apps/run-axPreview.ps1`, or `run.ps1 -App preview`.

### axMedia

- What: a player for sound and video files.
- command `axkit media`; opens a file or a folder.
- Code: `src/AxMedia` (the window), `src/AxMedia.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxMedia.Tests`.
- Documents: `docs/axMedia/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axMedia/`, 7 scripts (their workflows are in `docs/axKit/workflows/axMedia.md`).
- Run from the sources: `scripts to run-individual-apps/run-axMedia.ps1`, or `run.ps1 -App media`.

## Organise

### axTask

- What: tasks and lists.
- command `axkit task`; opens a folder; has a Start menu entry in the Store's copy.
- Code: `src/AxTask` (the window), `src/AxTask.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxTask.Tests`.
- Documents: `docs/axTask/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axTask/`, 14 scripts (their workflows are in `docs/axKit/workflows/axTask.md`).
- Run from the sources: `scripts to run-individual-apps/run-axTask.ps1`, or `run.ps1 -App task`.

## Utilities

### axStart

- What: a program launcher behind a hotkey.
- command `axkit start`; opens nothing; runs in the background from sign-in; has a Start menu entry in the Store's copy.
- Code: `src/AxStart` (the window), `src/AxStart.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxStart.Tests`.
- Documents: `docs/axStart/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axStart/`, 4 scripts (their workflows are in `docs/axKit/workflows/axStart.md`).
- Run from the sources: `scripts to run-individual-apps/run-axStart.ps1`, or `run.ps1 -App start`.

### axClip

- What: a clipboard history behind a hotkey.
- command `axkit clip`; opens nothing; runs in the background from sign-in; has a Start menu entry in the Store's copy.
- Code: `src/AxClip` (the window), `src/AxClip.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxClip.Tests`.
- Documents: `docs/axClip/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axClip/`, 4 scripts (their workflows are in `docs/axKit/workflows/axClip.md`).
- Run from the sources: `scripts to run-individual-apps/run-axClip.ps1`, or `run.ps1 -App clip`.

### axCalc

- What: a calculator with a history.
- command `axkit calc`; opens nothing.
- Code: `src/AxCalc` (the window), `src/AxCalc.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxCalc.Tests`.
- Documents: `docs/axCalc/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axCalc/`, 4 scripts (their workflows are in `docs/axKit/workflows/axCalc.md`).
- Run from the sources: `scripts to run-individual-apps/run-axCalc.ps1`, or `run.ps1 -App calc`.

### axProcess

- What: the processes, services and startup programs.
- command `axkit process`; opens nothing.
- Code: `src/AxProcess` (the window), `src/AxProcess.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxProcess.Tests`.
- Documents: `docs/axProcess/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axProcess/`, 5 scripts (their workflows are in `docs/axKit/workflows/axProcess.md`).
- Run from the sources: `scripts to run-individual-apps/run-axProcess.ps1`, or `run.ps1 -App process`.

### axAutomate

- What: workflows that drive the windows of axKit.
- command `axkit automate`; opens a folder.
- Code: `src/AxAutomate` (the window), `src/AxAutomate.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxAutomate.Tests`.
- Documents: `docs/axAutomate/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axAutomate/`, 5 scripts (their workflows are in `docs/axKit/workflows/axAutomate.md`).
- Run from the sources: `scripts to run-individual-apps/run-axAutomate.ps1`, or `run.ps1 -App automate`.

## Access layers

### axTerminal

- What: access layer for your shell: PowerShell, Command Prompt or Git Bash as a transcript.
- Access layer for: your shell, which must be installed; the app reaches the network only through it.
- command `axkit terminal`; opens a folder; has a Start menu entry in the Store's copy.
- Code: `src/AxTerminal` (the window), `src/AxTerminal.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxTerminal.Tests`.
- Documents: `docs/axTerminal/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axTerminal/`, 12 scripts (their workflows are in `docs/axKit/workflows/axTerminal.md`).
- Run from the sources: `scripts to run-individual-apps/run-axTerminal.ps1`, or `run.ps1 -App terminal`.

### axGIT

- What: access layer for Git: one repository's changes, log and branches.
- Access layer for: Git, which must be installed; the app reaches the network only through it.
- command `axkit git`; opens a file or a folder.
- Code: `src/AxGit` (the window), `src/AxGit.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxGit.Tests`.
- Documents: `docs/axGIT/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axGIT/`, 7 scripts (their workflows are in `docs/axKit/workflows/axGIT.md`).
- Run from the sources: `scripts to run-individual-apps/run-axGIT.ps1`, or `run.ps1 -App git`.

### axPass

- What: access layer for KeePassXC: a KeePass password database.
- Access layer for: KeePassXC, which must be installed; the app reaches the network only through it.
- command `axkit pass`; opens a file.
- Code: `src/AxPass` (the window), `src/AxPass.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxPass.Tests`.
- Documents: `docs/axPass/` (`kdbx-notes.md`, `nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axPass/`, 4 scripts (their workflows are in `docs/axKit/workflows/axPass.md`).
- Run from the sources: `scripts to run-individual-apps/run-axPass.ps1`, or `run.ps1 -App pass`.

## Claude (access layers for Claude Code)

### axClaude

- What: access layer for Claude Code: its conversation as plain text.
- Access layer for: Claude Code, which must be installed; the app reaches the network only through it.
- command `axkit claude`; opens a folder; has a Start menu entry in the Store's copy.
- Code: `src/AxClaude` (the window), `src/AxClaude.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxClaude.Tests`.
- Documents: `docs/axClaude/` (`claude-screens.md`, `events-and-grounding.md`, `nvda-test-plan.md`, `SPEC.md`, `stream-mode.md`, `stream.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axClaude/`, 45 scripts (their workflows are in `docs/axKit/workflows/axClaude.md`).
- Run from the sources: `scripts to run-individual-apps/run-axClaude.ps1`, or `run.ps1 -App claude`.

### axMCP

- What: access layer for MCP servers: the tools, resources and prompts of any MCP server on this computer, started and run by hand.
- Access layer for: MCP servers, which must be installed; the app reaches the network only through it.
- command `axkit mcpclient`; opens a folder.
- Code: `src/AxMCP` (the window), `src/AxMCP.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxMCP.Tests`.
- Documents: `docs/axMCP/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axMCP/`, 3 scripts (their workflows are in `docs/axKit/workflows/axMCP.md`).
- Run from the sources: `scripts to run-individual-apps/run-axMCP.ps1`, or `run.ps1 -App mcpclient`.

### axHelp

- What: access layer for Claude Code: your conversation with Claude and a screenshot into it, from anywhere, behind two hotkeys.
- Access layer for: Claude Code, which must be installed; the app reaches the network only through it.
- command `axkit help`; opens nothing; runs in the background from sign-in; has a Start menu entry in the Store's copy.
- Code: `src/AxHelp` (the window), `src/AxHelp.Core` (pure code); notes in each folder's `CLAUDE.md`.
- Tests: `tests/AxHelp.Tests`.
- Documents: `docs/axHelp/` (`nvda-test-plan.md`, `SPEC.md`, `user-guide.md`).
- Automated checks: `tests/scripts/axHelp/`, 2 scripts (their workflows are in `docs/axKit/workflows/axHelp.md`).
- Run from the sources: `scripts to run-individual-apps/run-axHelp.ps1`, or `run.ps1 -App help`.
