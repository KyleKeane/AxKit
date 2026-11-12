# Reports: a bug, a feature request or feedback from any window

Specified 2026-11-07 at the user's ask; built the same day on the branch `reports`, section 12's steps 1 to 7
(`CHANGELOG.md` 5.1.0); step 8, the reports repository, the reporter account and the token, is `reports-setup.md`,
and the NVDA passes are owed before the branch merges (`nvda-test-record.md`). The user's words, in short: one simple keystroke in every window
opens a report with the last few actions and the current focus already in it; the user either types their idea and
presses Enter, or Tabs through every field to review and add what makes the report unambiguous to act on; three
kinds, feedback, bug and feature request, recategorised later as needed; a feature for every user of axKit, not the
maintainer alone, so it goes to GitHub over its own API, is saved on the computer first and sent when there is a
network, with anything unsent going along the next time; reviewed in axTask through a template, transparent; the
list of reports and the report itself in the Help menu; and in axTask, Ctrl+number keys the user assigns to actions
on the GitHub items (feature requests, bugs, feedback). Numbered RP-n for the bundle spec to cite; the axTask part is
TK and IA material and moves into those specs when built.

## 1. Summary

- **F4 in every window** (`BundleKeys.Report`, AX-8.2) opens the notice **Report to axKit** in the window, with the
  field for the user's words focused. Enter sends; Tab reaches the kind, the context fields and the buttons. The axKit
  menu has it as "Report a bug or an idea…" and, beside it, "Your reports", which opens the list of reports in axTask;
  the command palette lists both (AX-10.2).
- **Saved first, sent second.** Enter writes the report as an item of the axTask list `Reports` under the user's axKit
  data folder, then sends it to GitHub as an issue of the reports repository over GitHub's REST API, through axKit's
  own reporter account, whose token ships inside the executable: nothing to sign in to (RP-D2). No network or a refusal
  leaves the item marked "Not sent yet", and it goes with the next report or when the list is opened (RP-D3).
- **Nothing to sign in to, and a way to cut off abuse.** Every report carries the install's reporter id, a random id
  made on first use. A blocklist the sender reads before sending, the reports repository's own workflow, and the
  token's revocation and rotation at a release let the maintainer stop nonsense from one install or one version
  (RP-6.4). A version whose token was revoked says so and offers the report for copying and GitHub's own form.
- **axTask shows it all.** The Reports list is an ordinary axTask list with a template, so the user reads, edits and
  deletes reports as items. The maintainer's lists of the repository's issues are axTask lists too (GitHub lists,
  section 7), and Ctrl+1 to Ctrl+9 run the actions the user assigned to that list (section 8): make it a bug, close it,
  open it in axClaude.

## 2. The key and the menu

- **RP-2.1 F4.** Checked 2026-11-07: no window's key table uses F4 (Alt+F4 is Windows'). Ctrl+P, which the user also
  asked about, is three windows' own (axClip's pause, axMedia's pause, axTerminal's say-folder), and Ctrl+Shift+P is
  the palette; so F4, bundle-wide. `BundleKeys.Report = Keys.F4`, named "Report" in the keys table; `KeyTests` keeps the
  windows off it.
- **RP-2.2 The Help menu** (AX-9.1; the axKit menu until 5.7.0) gets, after the command palette and the guide: "&Send a report…" with F4 shown (S: R is
  Recently closed's letter), and "&Your reports", which opens `%APPDATA%\AxKit\reports` in
  axTask (a new axTask window through `WindowMenu.Start`, the folder and its `list.json` made first), then a separator (since 5.6.0 the three stand after the Claude submenu, AX-9.1). Both carry their words into every window's palette and F1 text through the
  window base; nothing is added to an app's own menus.
- **RP-2.3 While a notice shows,** F4 does nothing, as every window key (AX-6.5). The report's recent actions record
  the notice that showed last, so a problem with a notice is reported after closing it.

## 3. The form: the notice "Report to axKit"

One notice (`OverlayPanel`, bundle rule 2), its controls in Tab order, each named as an instruction (the user's rule),
one announcement per move:

1. **"Type your report; Enter sends it, Shift+Enter starts a new line"**: a multi-line field, focused when the notice
   opens, empty. Enter sends (the message field's rule, axClaude D11); Shift+Enter a line break. Enter on an empty field:
   the edge earcon and "Type a few words first".
2. **"Kind"**: a list of three, Feedback (selected), Bug, Feature request; type-to-find reaches them (AX-8.6). NVDA
   reads the selected kind as the list moves, and the app adds nothing (AX-7).
3. **"App and version"**: read-only text, "axClaude 5.0.1, Windows 11 build 26200, NVDA 2025.3" (NVDA's version from
   its running process, "NVDA not running" otherwise; the window's title after it, redacted).
4. **"What you were doing: the last actions"**: a multi-line field, editable, filled with the window's last twenty
   actions (section 4), oldest first, one per line: "12 s ago: Find, Ctrl+F" and "12 s ago, said: 3 matches".
5. **"Where the focus was"**: a one-line field, editable: the focused control's name and place, never its text:
   "Conversation, line 54 of 120", "Message to Claude, 2 lines typed (not included)", "Files, item 7 of 40, notes.md".
6. **"Log lines, the last twenty"**: a multi-line field, editable, the app's log tail, redacted; the user deletes what
   they do not want sent.
7. **"Title"**: a one-line field, filled from the first line of the report (eighty characters at most) when the user
   Tabs past the first field or presses Enter there; editable.
8. **Send** (Enter anywhere but a multi-line field, the notice's default button) and **Cancel** (Escape).

- **RP-3.1 What is said.** Opening: "Report to axKit" and the first field's name, as every notice. Sending: "Sent as
  issue 12" with the done chime; or "Saved; not sent yet, no network. It goes with the next report"; or, when this
  build or version cannot send (RP-6.4, RP-6.5), the notice that says why, with Copy report and Open GitHub's form. Escape with words typed: "Kept as a draft in Your
  reports" (RP-D6); with none: "Cancelled".
- **RP-3.2 Nothing is lost.** The report is written to the Reports list before any network is touched, so a failed send
  is a saved item with its reason in the Status field, and a crash mid-way leaves the file.

## 4. What is gathered, and what never is

- **RP-4.1 The recent actions.** The window base keeps a ring of the last twenty actions of the window: every command
  run through its command list (the menu path and key the palette shows, AX-10.2) and every announcement raised
  (`Announcer.Raised`, which already feeds the MCP channel's "said" list while a client records; the ring becomes the
  one copy both read). Each entry has a time; the report shows the age.
- **RP-4.2 Never the user's text.** The content of an edit field (the message to Claude, a document, a password), a
  conversation line of axClaude, a clipboard entry of axClip or a field value of axPass is never in a report. The
  focus line names the control and its place only. axPass's and axClip's windows (the MCP's never-read apps, `McpTools.NeverRead`,
  docs/axKit/mcp.md section 5) and the console windows, axClaude and axTerminal, whose spoken replies and output are
  the user's conversation (the first real report, 2026-11-08, carried a spoken reply of Claude's), put only their
  commands and notices in the ring, not their announcements (`AxKitForm.RecordsAnnouncements`).
- **RP-4.3 Redaction.** Every prefilled field goes through `Redaction` (AxKit.Core, S-20) before it is shown: the
  account's paths read `<user>`, an e-mail address `<email>`. The user reads what will be sent.
- **RP-4.4 Public.** An issue is public on GitHub, posted by axKit's reporter account with the install's reporter id
  and nothing that names the user; the consent notice says so in its first line (section 6), and the form's title row
  says "public issue" after the first time: "Report to axKit (a public issue on GitHub)". A user who wants an answer
  writes how to reach them in the report, knowing it is public; the form never asks for it.

## 5. Saving: the Reports list

- **RP-5.1 The folder.** `%APPDATA%\AxKit\reports` is an axTask list (docs/axKit/item-apps.md section 4): `list.json`
  with the template below, written when the folder is first made, and one JSON file per report, named after its Title
  (IA-4.5). axTask opens it as any folder; the Help menu's "Your reports" opens it (RP-2.2). axJSON opens any item.
- **RP-5.2 The fields** (the template's order): Title; Kind (choice: Feedback, the template's value, Bug or Feature
  request); Report (text, the user's words); App (text, as RP-3 item 3); Where (text); Actions (text, one action per
  line); Log (text); Created (date and time); Status (text, Draft as the template's value: Draft, Not sent yet, or Sent, with the reason after a colon when not sent: "Not sent
  yet: no network", "Not sent yet: this version can no longer send reports, update axKit", "Not sent yet: blocked"); Issue (link, the issue's web address once sent);
  Number (number, the issue's).
- **RP-5.3 Editing.** Until it is sent, every field edited in axTask goes into the issue as edited. After, edits stay
  local; the Issue link opens the issue in the browser (a link whose value is a web address opens in the browser, an
  addition to IA-4.2's link type). Delete archives as any item (IA-D4) and never touches GitHub.

## 6. Sending: GitHub's REST API, axKit's own reporter account

GitHub takes issues from signed-in accounts only, in the browser as in its API, and a sign-in would keep many people
from reporting at all (the user, 2026-11-07). So the sender signs in as axKit itself: a reporter account of the
maintainer's, whose token travels inside the executable. That token can be extracted by anyone who takes the trouble,
which is why it can do little, why every report names its install, and why the maintainer can revoke it and block an
install or a version at any time (RP-6.2 to RP-6.4). The protocols for a compromise are settled when one happens; the
levers are built now.

- **RP-6.1 The request.** `POST https://api.github.com/repos/KyleKeane/AxKit-reports/issues` with the title, the body
  and one label from the kind (`feedback`, `bug`, `feature`), `Authorization: Bearer <reporter token>`,
  `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2022-11-28`. The answer's number and web address go into
  the item (Status "Sent"). The body is Markdown: the user's words first; then "### Context" with App and version and
  Where the focus was; "### Last actions" as a code block; the log lines inside a `<details>` block titled "Log lines";
  last, one line the workflow and the maintainer filter on: "Sent from axKit 5.1.0 with F4; reporter a1b2c3d4-…". The
  labels exist in the reports repository; recategorising is changing the label (section 7).
- **RP-6.2 The reporter token.** A fine-grained personal access token of the maintainer's own GitHub account, with
  access to the reports repository alone and the permission Issues: read and write, nothing else: a leaked token can
  file, edit, label and close issues there, and nothing anywhere else (RP-D8). The issues it posts carry the
  maintainer's name as their author and the reporter line as their truth. (A reporter account of its own was the first
  thought; GitHub lets a fine-grained token reach only its own account's repositories, so such an account would need a
  classic token, which reaches everything it can see, or an organization; `reports-setup.md` section 1 says so, and an
  organization can come later without a change to the code.) It is never in the source: `release.yml`'s sign job, the "release" environment, which runs no test package, reads it
  from that environment's secret `AXKIT_REPORTER_TOKEN` and has `publish.ps1` write it, mixed with a fixed key and as
  base64, into `src/AxKit.Core/Reports/reporter.token` (`tools/reporter-token.ps1`), a file git ignores, which the
  build embeds when it exists and which the script removes again; `ReporterToken` reads it back at run time. The mixing
  keeps the token from a glance at the program file and stops nobody who looks for it, and this page says so. The build
  job, which runs the tests and their packages, never sees the token (S-30, `ReleaseWorkflowTests`). A build without the secret (a developer's build, a fork's) has no token: its sender says
  "This build cannot send reports" and offers RP-6.5's two buttons. GitHub's terms ask that credentials be kept
  confidential; the machine user with one repository and one permission is the closest a token in a program comes to
  that, and a revocation by GitHub costs one release (RP-6.4). The setup page `docs/axKit/reports-setup.md` records
  the account, the repository, the token's scope and the secret, as `code-signing.md` does for signing.
- **RP-6.3 The reporter id.** A random GUID made on the first report and kept in `%APPDATA%\AxKit\reports.json`,
  in every report's last line. It names the install, not the person: no account, no name, no hardware id, and
  deleting the file gives a new one. The maintainer filters and, if need be, blocks by it (RP-6.4). `PRIVACY.md` says
  so.
- **RP-6.4 Abuse: the levers.** Three, cheapest first. The **blocklist**, `blocklist.json` in the reports repository
  (`https://raw.githubusercontent.com/KyleKeane/AxKit-reports/main/blocklist.json`, a list of reporter ids and of
  axKit versions), fetched by the sender before a send and kept a day: a listed install or version does not send and
  says "Reports from this install are blocked; see the reports repository". The **workflow** in the reports repository
  (`issues: opened`) reads the last line of every new issue and closes and locks one from a blocked id or version at
  once, so the lever holds even against a sender that skips the blocklist. The **token**: revoked on GitHub and
  replaced, the secret updated, a release made; every earlier version gets GitHub's 401 and says "This version can no
  longer send reports: update axKit from the Help menu, or copy the report", with RP-6.5's buttons. Revoking also at
  least once a year goes into `docs/axKit/longevity.md`'s yearly check. One report per thirty seconds per install, from
  the sender's side, keeps a stuck key from flooding.
- **RP-6.5 When sending cannot happen.** The report is saved whatever happens (RP-3.2). The notice that says why has
  two buttons: **Copy report** (the whole body to the clipboard, to paste anywhere) and **Open GitHub's form** (the
  browser on the reports repository's new-issue form, prefilled by its query string, which needs the user's own GitHub
  account; the whole address is kept within 2000 characters, the user's words first and a note where the rest is, since
  the Windows shell handed a longer one to the browser cut off and the page did not load, found by the user
  2026-11-07). No e-mail route: an address cannot live in the repository (`PrivacyTests`, bundle rule 6), and the user
  chose the token over it.
- **RP-6.6 Consent** (AX-11.4, AX-11.5): before the first send, the consent notice for GitHub, a new
  `ConsentTarget.GitHubReports`: "Reports go to GitHub as public issues of axKit's reports repository, posted by axKit's
  own account with a random id of this install and nothing that names you; what you write is public, and GitHub's
  terms apply to it" with the check box; recorded in `consent.json` with its date, shown in About, Licence, withdrawn
  by "Forget AI tools" like the others.
- **RP-6.7 When sending runs.** Right after a report is saved, and then every "Not sent yet" item of the list, oldest
  first; when "Your reports" opens the list from the Help menu (axTask says "2 reports sent" or the reason); never at
  a window's start: a standalone app stays off the network until the user acts, the update check being the bundle's
  one automatic request (G6, AX-3). Each send has a ten-second limit; its reason, in a few words, goes into Status.
- **RP-6.8 The rule changes, in one commit with the code** (bundle rule 8, B-15, G6, AX-11.11, `PRIVACY.md`,
  `README.md`, `docs/axKit/README.md`, `SECURITY.md`): axKit's own code makes network requests in two places, the
  updater and the report sender, both over HTTPS to GitHub, the report sender only when the user acts (RP-D2). The
  two endpoints (the issue, the blocklist) carry `Outside call, network, GitHub:` comments and appear on
  `docs/axKit/outside-calls.md` (B-17). The security audit gets entries for the token in the build, the reporter id
  and the redaction of what is sent.

## 7. The maintainer's side: GitHub lists in axTask

- **RP-7.1 A GitHub list** is an axTask list whose `list.json` has `"github": {"repository": "KyleKeane/AxKit-reports",
  "state": "open"}`. Opening it, and F5, fill it from `GET /repos/{repository}/issues?state=open&per_page=100` (paged,
  pull requests left out), with no token: a public repository's issues are read without one, sixty requests an hour,
  enough for a list. One item per issue with Title, Number, Kind (the label among feedback, bug and feature, else
  "unlabelled"), State, Opened (date), Reporter (the reporter id from the body's last line, or the GitHub login of an
  issue filed in the browser), Version (from the same line), Updated, Issue (link), Body (text) and Notes (text,
  local, never sent, kept across refreshes by Number). Items whose issue GitHub no longer returns move to the list's
  archive (IA-D4). The refresh says "Issues, 14 items, 2 new". A second list with `"state": "closed"` shows the rest.
- **RP-7.2 Actions on an item** (Context on an item, and the action keys of section 8): Make it a bug, Make it a feature
  request, Make it feedback (`PATCH` the labels), Close, Reopen (`PATCH` the state), Comment… (a notice with a field,
  `POST` a comment), Block this reporter and Block this version (a line added to `blocklist.json` through the contents
  API, RP-6.4), Open on GitHub (the browser), Open in axClaude (axClaude on the code repository's folder with the issue
  as the message field's text, "Issue 12: <title>" and the body, not sent, for the user to read and send: the issues
  made actionable). The actions use the **user's own token**, never the reporter's: Actions → "GitHub account…" takes
  a fine-grained token of the user's once (Issues and Contents on the reports repository), kept protected as
  `mcp.key` is (`UserSecret`), forgotten from the same menu; without one the actions say "Set your GitHub token in
  Actions first: GitHub account". A change refused by GitHub (403) is said and nothing changes locally.
- **RP-7.3 The maintainer's folder.** `docs/feature_requests` (the lists Features and Bugs) becomes GitHub issues: the
  assistant creates one issue per item, labelled by its list, through `gh` once, and the folder goes, its README
  replaced by one line pointing to the issues and to this page. GitHub's issues are the tracker from then on, for the
  maintainer as for everyone (RP-D1).

## 8. Ctrl+number: the user's own actions on items (axTask, every list)

- **RP-8.1 The keys.** Ctrl+1 to Ctrl+9 in axTask, anywhere in the window, run the action the current list assigns to
  that number, on the item selected in the navigator (or the item whose fields the pane shows). The list's `list.json`
  holds them under `"keys"`, one entry per digit:

  ```json
  "keys": {
    "1": { "name": "Make it a bug", "set": { "Kind": "Bug" } },
    "2": { "name": "Make it a feature request", "set": { "Kind": "Feature request" } },
    "3": { "name": "Make it feedback", "set": { "Kind": "Feedback" } },
    "4": { "name": "Close", "set": { "State": "closed" } },
    "5": { "name": "Open on GitHub", "open": "Issue" },
    "6": { "name": "Open in axClaude", "open": "axClaude" },
    "7": { "name": "Move to Later", "move": "../Later" }
  }
  ```

  One kind of action per key: `set` (fields to values, one undo step, IA-5.4), `move` (to a list by a path relative to
  this one, IA-D3), `open` (a field's link, or an app's verb on the item's file; in a GitHub list `axClaude` means
  RP-7.2's Open in axClaude). In a GitHub list a `set` of Kind or State is also sent to GitHub (RP-7.2). A key without
  an entry: the edge earcon and "No action on Ctrl+3 in this list; Actions, Action keys sets one". An `open` of
  `claude` or `terminal`, and Open in axClaude, start their program only in a folder on a drive of this computer: on
  a share or a mapped drive the key says "Not started: axTerminal would run in a folder that is not on a drive of this
  computer" and starts nothing, since a `list.json` may come from a list the user did not write (security audit S-5's
  rule; the AI tools' `axkit_open` never starts the two at all, mcp.md; 2026-11-09).
- **RP-8.2 Assigning them.** Actions → "Action keys…": a notice listing the nine keys with their names ("Ctrl+1.
  Make it a bug", the full stop as AX-8.5's pause); Enter on one opens its form: Name; What (Set fields, Move to a list,
  Open); the value; Delete clears it. Written into `list.json`, so axJSON (Ctrl+J) edits them too.
- **RP-8.3 Shown everywhere a command is.** Context on an item lists the assigned actions with their keys, so the palette and
  F1 list them (AX-10.2); the names are the user's.
- **RP-8.4 All tasks moves to Ctrl+0** (TK-7), so the nine digits are free for actions in every list (RP-D7).
- **RP-8.5 Defaults.** A GitHub list's `list.json` comes with the keys above (1 to 3 the kinds, 4 Close, 5 Open on
  GitHub, 6 Open in axClaude); the Reports list's (RP-5.1) with 1 to 3 the kinds and 4 Open on GitHub, since its items
  are the user's own reports; an ordinary list has none until the user sets them.

## 9. Privacy and safety

- Everything sent is in fields the user can read and change before Enter (RP-3), redacted first (RP-4.3), never the
  text of an edit field or a conversation (RP-4.2). Nothing names the user: the reporter account is axKit's, the
  reporter id is a random id of the install (RP-6.3). An issue is public (RP-4.4), said at consent and in the form's
  title. The sender touches the network only on the user's action (RP-6.7). The reporter token can reach one
  repository's issues and nothing else, and is revoked and replaced at will (RP-6.2, RP-6.4); the maintainer's own
  token stays on the maintainer's computer (RP-7.2). `PrivacyTests` keeps the repository free of what a report may
  contain, since nothing of a report is committed, and the token never enters the source (RP-6.2).

## 10. Decisions

- **RP-D1 GitHub's issues are the tracker for everyone** (the user, 2026-11-07), the maintainer's folder included
  (RP-7.3). Until then GitHub waited for users (`docs/feature_requests/README.md`, 2026-11-06).
- **RP-D2 Sent by axKit's own code over GitHub's REST API, as axKit's own reporter account** (the user, 2026-11-07,
  twice: a feature for all users, as an allowed network action, not through the GitHub CLI, a second program every
  user would have to install and sign in to; then no sign-in at all, since a sign-in "will make it impossible for
  many people": the token in the executable, a reporter id per install, and the protocols for a compromise settled if
  one comes, the levers built now, RP-6.4). The report sender is the bundle's second network service beside the
  updater, and only the user's action starts it. Weighed and set aside: GitHub's device sign-in (the hurdle), e-mail
  (an address cannot be in the repository, and one more program to pass through), a relay the maintainer runs (a
  service to keep alive for decades, and nonsense straight into the issues).
- **RP-D8 A reports repository of its own**, `KyleKeane/AxKit-reports`: the reporter token can reach its issues alone,
  so a leak or a flood never touches the code repository, whose issues stay for contributors; the maintainer triages
  in the reports repository and transfers what becomes work with GitHub's own Transfer issue.
- **RP-D9 The fallback is copying and GitHub's form**, offered by the notice that says why a send cannot happen
  (RP-6.5); nothing is ever lost (RP-3.2).
- **RP-D3 Saved first, sent second** (the user): a report is an item on disk before any request, unsent ones go with
  the next, and nothing is sent at a window's start.
- **RP-D4 F4, bundle-wide**; Ctrl+P is three windows' own (RP-2.1).
- **RP-D5 One key, one kind of action, as data in `list.json`**: set, move or open, no scripting (B-10).
- **RP-D6 Escape keeps a draft** when words were typed: a blind user's Escape by habit must not lose a paragraph.
- **RP-D7 All tasks to Ctrl+0**, the digits to the list's actions.

## 11. Open questions, for the user when reading this

- The default kind: Feedback, as written, or Bug.
- Whether the log lines (RP-3 item 6) go into the form by default, or only on a button "Add the log lines".
- Whether the Reports list also appears in axTask's top level under a fixed "axKit" section, or through the menu only.
- The menu's words: "Report a bug or an idea…" or "Feedback…" ("Report…" alone is too close to axClaude's feedback
  drafts).
- The names: the machine user `axkit-reporter` and the repository `AxKit-reports`, both to be made under the
  maintainer's account (RP-6.2, RP-D8), or the code repository's own issues after all, accepting that a leaked token
  reaches them.
- Whether blocking by reporter id should also be offered to the user as a way to see what was sent from their
  install: the reports repository's issues searched for the id (a link in the Reports list's `list.json`).

## 12. Build order

1. The ring of recent actions in the window base, the MCP's "said" reading it (RP-4.1); tested.
2. `Reports` in AxKit.Core: the folder, the template, the item writer and the body as Markdown (RP-5, RP-6.1); tested.
3. The notice, F4, the menu items, the palette and F1 words (RP-2, RP-3); the accessibility review; the NVDA step in
   the bundle section of one app's plan and `tests/nvda-scripts`.
4. `GitHub` in AxKit.Core: create an issue, read the blocklist, list issues, patch labels and state, add a comment,
   write the blocklist, with recorded answers as fixtures (RP-6.1, RP-6.4, RP-7.1, RP-7.2).
5. The reports repository, the machine user, its token and the secret, the workflow that closes blocked reports, the
   token's place in the build (`release.yml`), the setup page (RP-6.2 to RP-6.4).
6. The sender with its retry, the reporter id, the consent target, the fallback buttons, the rule changes and the
   pages (RP-6.3, RP-6.5 to RP-6.8).
7. axTask: GitHub lists, the action keys and their notice, the user's token, Ctrl+0 (RP-7, RP-8); TK-7 and IA-3 updated.
8. The labels, the issue forms (a bug form and an idea form in the reports repository too), the maintainer's folder
   to issues (RP-6.1, RP-7.3).
9. Released as 5.1.0, after the NVDA passes.
