# The documents

Where each thing is written. The apps and their folders are in `apps.md`; this page is the bundle's own documents, in
`docs/axKit/` unless a path is given. Read the one that covers what you are changing; none needs the others.

## Start here

- The root `CLAUDE.md`: the rules of the bundle, the constraints and the environment's gotchas.
- `apps.md` (generated): the apps, each with its command, its code, its tests, its documents and its checks.
- `commands.md`: every command and script.
- An app's own `docs/<app>/SPEC.md`, `user-guide.md` and `nvda-test-plan.md`, and the `CLAUDE.md` notes beside its code.

## Specifications

- `SPEC.md`: the bundle. Launch, install, update, release, the shared pieces, the accessibility rules (AX-7), the
  keys (AX-8), the menus (AX-9), the command palette (AX-10), the access layers (AX-11), the Store's copy (AX-13), the
  modularity rules.
- `app-kit.md` (AK-n): the shape of an app's window and commands; one source for the menus, the palette and the AI
  tools (AK-2.5, AK-2.6).
- `item-apps.md` (IA-n): the shape axTask, axPass, axMCP and axAutomate share, the navigator and the detail pane.
- `mcp.md` (MC-n): the AI tools, what an AI client may read and run in axKit's windows and how the user stays in
  control.
- `ai-help.md` (AH-n): AI help, Claude Code in print mode with read-only tools, Shift+F1 in every window, and axHelp.
- `reports.md` (RP-n): the reports from every window (F4), sent to GitHub as issues of the reports repository;
  axTask's GitHub lists and action keys.
- `theme.md` (TH-n): the theme file, every colour from one JSON file, and the display settings.
- `adding-an-app.md`: how a new app is made and every place it is registered.

## Checks and quality

- `automation.md`: the automated checks of the windows, the runner, and the NVDA pass.
- `workflows.md` (generated): every workflow the automated checks keep working, by app.
- `nvda-test-record.md`: every NVDA pass, by step, newest first.
- `repairs.md` (generated from `repairs.json`): what the audits found and what became of each finding.
- `security-audit-2026-10-24.md` (S-n): the security audit and its fixes.
- `outside-calls.md` (generated): everything axKit reaches outside its process (SPEC B-17).
- `longevity.md`: the practices that keep the code maintainable for decades, and the yearly check.

## Release and distribution

- `README.md`: the bundle's guide, shipped in the zip.
- `store-distribution.md`: the plan for the Microsoft Store and winget, what the Store's copy changes, the steps and
  their state.
- `store-listing.md`: the Store listing's texts.
- `store-setup.md`: the Store account's runbook.
- `reports-setup.md`: the reports repository, the reporter account, its token and the release secret.
- `development-environment.md`: every tool of the development computer and how to set a new one up.

## Plans and history

- `plan.md`: how the repository became the bundle, and the apps asked for and not built yet.
- The priorities list of the private management repository (`..\axkit-management\priorities`) is the one place for
  open work (root `CLAUDE.md`, rule 9).
- Dated records, kept for what they decided: `review-2026-10-23.md` (the review and its rewrites),
  `inventory-2026-10-24.md` and `inventory-2026-11-09.md` (copies, dead code and drift found), `cleanup-run.md` with
  its report, `axfile-split.md` (how axMedia and axPreview came out of axFile), `code-signing.md` (the Windows signing
  set up and retired for the Store's signature; its section 7 lists what the maintainer deletes on Azure).

## The repository's pages

- `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `GOVERNANCE.md`, `CODE_OF_CONDUCT.md`, `THIRD-PARTY-NOTICES.md`,
  `CHANGELOG.md` at the root.
- `PRIVACY.md`: everything axKit contacts. It changes in the same commit as any new network contact or program axKit
  starts (rule 8).
- `docs/feature_requests/README.md`: points to the reports repository `KyleKeane/AxKit-reports`, where feature
  requests and bugs are issues.
- `.github/`: the workflows, the issue forms and the pull request checklist.
- `.claude/skills/accessibility-review` (run on any change to a window before committing) and
  `.claude/skills/record-fixture` (axClaude's recordings).
