# Reports: a bug, a feature request or feedback from any window

Specified 2026-11-07 at the user's ask; not built. The user's words, in short: one simple keystroke in every window
opens a report with the last few actions and the current focus already in it; the user either types their idea and
presses Enter, or Tabs through every field to review and add what makes the report unambiguous to act on; three
kinds, feedback, bug and feature request, recategorised later as needed; a feature for every user of axKit, not the
maintainer alone, so it goes to GitHub over its own API, is saved on the computer first and sent when there is a
network, with anything unsent going along the next time; reviewed in axTask through a template, transparent; the
list of reports and the report itself in the axKit menu; and in axTask, Ctrl+number keys the user assigns to actions
on the GitHub items (feature requests, bugs, feedback). Numbered RP-n for the bundle spec to cite; the axTask part is
TK and IA material and moves into those specs when built.

## 1. Summary

- **F4 in every window** (`BundleKeys.Report`, AX-8.2) opens the notice **Report to axKit** in the window, with the
  field for the user's words focused. Enter sends; Tab reaches the kind, the context fields and the buttons. The axKit
  menu has it as "Report a bug or an idea…" and, beside it, "Your reports", which opens the list of reports in axTask;
  the command palette lists both (AX-10.2).
- **Saved first, sent second.** Enter writes the report as an item of the axTask list `Reports` under the user's axKit
  data folder, then sends it to GitHub as an issue of this repository over GitHub's REST API, as the user's own GitHub
  account. No network, no sign-in or a refusal leaves the item marked "Not sent yet", and it goes with the next report
  or when the list is opened (RP-D3).
- **One sign-in, once.** GitHub takes issues only from an account. The first send shows the consent notice (AX-11.4)
  and then GitHub's device sign-in: a short code to type on github.com/login/device, which the notice opens. The token
  is kept on this computer, protected as the MCP key is, and forgotten from the axKit menu.
- **axTask shows it all.** The Reports list is an ordinary axTask list with a template, so the user reads, edits and
  deletes reports as items. The maintainer's lists of the repository's issues are axTask lists too (GitHub lists,
  section 7), and Ctrl+1 to Ctrl+9 run the actions the user assigned to that list (section 8): make it a bug, close it,
  open it in axClaude.

## 2. The key and the menu

- **RP-2.1 F4.** Checked 2026-11-07: no window's key table uses F4 (Alt+F4 is Windows'). Ctrl+P, which the user also
  asked about, is three windows' own (axClip's pause, axMedia's pause, axTerminal's say-folder), and Ctrl+Shift+P is
  the palette; so F4, bundle-wide. `BundleKeys.Report = Keys.F4`, named "Report" in the keys table; `KeyTests` keeps the
  windows off it.
- **RP-2.2 The axKit menu** (AX-9) gets, after the command palette item: "&Report a bug or an idea…" with F4 shown, and
  "Your &reports", which opens `%APPDATA%\AxKit\reports` in axTask (a new axTask window, or the open one on that folder,
  through `Dispatch`), then a separator. Both carry their words into every window's palette and F1 text through the
  window base; nothing is added to an app's own menus.
- **RP-2.3 While a notice shows,** F4 does nothing, as every window key (AX-6.5). The report's recent actions record
  the notice that showed last, so a problem with a notice is reported after closing it.

## 3. The form: the notice "Report to axKit"

One notice (`OverlayPanel`, bundle rule 2), its controls in Tab order, each named as an instruction (the user's rule),
one announcement per move:

1. **"Type your report; Enter sends it, Shift+Enter starts a new line"**: a multi-line field, focused when the notice
   opens, empty. Enter sends (the message field's rule, axClaude D11); Shift+Enter a line break. Enter on an empty field:
   the edge earcon and "Type a few words first".
2. **"Kind"**: a list of three, Feedback (selected), Bug, Feature request; type-to-find and the number keys of the list
   reach them (AX-8.6). Changing it says the kind.
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
  issue 12" with the done chime; or "Saved; not sent yet, no network. It goes with the next report"; or "Saved; sign in
  to GitHub to send it", which opens the sign-in (section 6). Escape with words typed: "Kept as a draft in Your
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
  docs/axkit/mcp.md section 5) put only their command names in the ring, not their announcements.
- **RP-4.3 Redaction.** Every prefilled field goes through `Redaction` (AxKit.Core, S-20) before it is shown: the
  account's paths read `<user>`, an e-mail address `<email>`. The user reads what will be sent.
- **RP-4.4 Public.** An issue is public on GitHub under the user's GitHub name; the consent notice says so in its
  first line (section 6), and the form's title row says "public issue" after the first time: "Report to axKit (a
  public issue on GitHub)".

## 5. Saving: the Reports list

- **RP-5.1 The folder.** `%APPDATA%\AxKit\reports` is an axTask list (docs/axkit/item-apps.md section 4): `list.json`
  with the template below, written when the folder is first made, and one JSON file per report, named after its Title
  (IA-4.5). axTask opens it as any folder; the axKit menu's "Your reports" opens it (RP-2.2). axJSON opens any item.
- **RP-5.2 The fields** (the template's order): Title; Kind (text: Feedback, Bug or Feature request); Report (text, the
  user's words); App (text, as RP-3 item 3); Where (text); Actions (text, one action per line); Log (text); Created
  (date and time); Status (text: Draft, Not sent yet, or Sent, with the reason after a colon when not sent: "Not sent
  yet: no network", "Not sent yet: GitHub said 401, sign in again"); Issue (link, the issue's web address once sent);
  Number (number, the issue's).
- **RP-5.3 Editing.** Until it is sent, every field edited in axTask goes into the issue as edited. After, edits stay
  local; the Issue link opens the issue in the browser (a link whose value is a web address opens in the browser, an
  addition to IA-4.2's link type). Delete archives as any item (IA-D4) and never touches GitHub.

## 6. Sending: GitHub's REST API, the user's account

- **RP-6.1 The request.** `POST https://api.github.com/repos/KyleKeane/AxKit/issues` with the title, the body and one
  label from the kind (`feedback`, `bug`, `feature`), `Authorization: Bearer <token>`, `Accept: application/vnd.github+json`,
  `X-GitHub-Api-Version: 2022-11-28`. The answer's number and web address go into the item (Status "Sent"). The body is
  Markdown: the user's words first; then "### Context" with App and version and Where the focus was; "### Last actions"
  as a code block; the log lines inside a `<details>` block titled "Log lines"; last, "Sent from axKit 5.1.0 with F4".
  The labels `feedback` and `feature` are created in the repository; the issue forms (`.github/ISSUE_TEMPLATE`) move from
  `idea` to `feature`, so the browser's reports and the window's carry the same labels, and recategorising is changing
  the label (section 7).
- **RP-6.2 Sign-in** is GitHub's device flow (RFC 8628) for an OAuth app the maintainer registers ("axKit reports",
  device flow on, scope `public_repo`; its client id is public and lives in the code, a setup page like
  `docs/axkit/code-signing.md` records the registration). `POST https://github.com/login/device/code` gives the user
  code and the verification address; the notice "Sign in to GitHub" shows "The code is ABCD-1234. Enter it on
  github.com/login/device, which opens now; this notice closes by itself when GitHub confirms", with Copy code and
  Cancel; the browser opens on the address; the window polls `POST https://github.com/login/oauth/access_token` at
  GitHub's interval until the token comes or the user cancels. The token is written to `%LOCALAPPDATA%\AxKit\github.token`
  protected for this Windows account as `mcp.key` is (`UserSecret`), never into a settings file. "Forget GitHub
  sign-in" in the axKit menu, beside "Forget AI tools", deletes it (and a 401 from GitHub says to sign in again).
- **RP-6.3 No account.** GitHub takes issues from accounts only, in the browser too. The sign-in notice has a third
  button, "Create a GitHub account", which opens github.com/signup; the report waits as "Not sent yet: not signed in".
- **RP-6.4 Consent** (AX-11.4, AX-11.5): before the first sign-in, the consent notice for GitHub, a new
  `ConsentTarget.GitHubReports`: "Reports go to GitHub as public issues of axKit's repository under your GitHub name;
  the account and its terms are between you and GitHub" with the check box; recorded in `consent.json` with its date,
  shown in About, Licence, withdrawn by "Forget AI tools" like the others.
- **RP-6.5 When sending runs.** Right after a report is saved, and then every "Not sent yet" item of the list, oldest
  first; when "Your reports" opens the list from the axKit menu (axTask says "2 reports sent" or the reason); never at
  a window's start: a standalone app stays off the network until the user acts, the update check being the bundle's
  one automatic request (G6, AX-3). Each send has a ten-second limit; its reason, in a few words, goes into Status.
- **RP-6.6 The rule changes, in one commit with the code** (bundle rule 8, B-15, G6, AX-11.11, `PRIVACY.md`,
  `README.md`, `docs/axkit/README.md`, `SECURITY.md`): axKit's own code makes network requests in two places, the
  updater and the report sender, both over HTTPS to GitHub, the report sender only when the user acts (RP-D2). The
  three endpoints carry `Outside call, network, GitHub:` comments and appear on `docs/axkit/outside-calls.md` (B-17).
  The security audit gets entries for the token file and the redaction of what is sent.

## 7. The maintainer's side: GitHub lists in axTask

- **RP-7.1 A GitHub list** is an axTask list whose `list.json` has `"github": {"repository": "KyleKeane/AxKit",
  "state": "open"}`. Opening it, and F5, fill it from `GET /repos/{repository}/issues?state=open&per_page=100` (paged,
  pull requests left out) with the user's token: one item per issue with Title, Number, Kind (the label among feedback,
  bug and feature, else "unlabelled"), State, Opened (date), By (the GitHub login), Updated, Issue (link), Body (text)
  and Notes (text, local, never sent, kept across refreshes by Number). Items whose issue GitHub no longer returns move
  to the list's archive (IA-D4). The refresh says "Issues, 14 items, 2 new".
- **RP-7.2 Actions on an item** (the Item menu, and the action keys of section 8): Make it a bug, Make it a feature
  request, Make it feedback (`PATCH` the labels), Close, Reopen (`PATCH` the state), Comment… (a notice with a field,
  `POST` a comment), Open on GitHub (the browser), Open in axClaude (axClaude on the repository's folder with the issue
  as the message field's text, "Issue 12: <title>" and the body, not sent, for the user to read and send: the issues
  made actionable). A change refused by GitHub (403: the account cannot change this repository's issues) is said and
  nothing changes locally.
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
    "6": { "name": "Open in axClaude", "open": "axclaude" },
    "7": { "name": "Move to Later", "move": "../Later" }
  }
  ```

  One kind of action per key: `set` (fields to values, one undo step, IA-5.4), `move` (to a list by a path relative to
  this one, IA-D3), `open` (a field's link, or an app's verb on the item's file; in a GitHub list `axclaude` means
  RP-7.2's Open in axClaude). In a GitHub list a `set` of Kind or State is also sent to GitHub (RP-7.2). A key without
  an entry: the edge earcon and "No action on Ctrl+3 in this list; List menu, Action keys sets one".
- **RP-8.2 Assigning them.** List menu → "Action keys…": a notice listing the nine keys with their names ("Ctrl+1.
  Make it a bug", the full stop as AX-8.5's pause); Enter on one opens its form: Name; What (Set fields, Move to a list,
  Open); the value; Delete clears it. Written into `list.json`, so axJSON (Ctrl+J) edits them too.
- **RP-8.3 Shown everywhere a command is.** The Item menu lists the assigned actions with their keys, so the palette and
  F1 list them (AX-10.2); the names are the user's.
- **RP-8.4 All tasks moves to Ctrl+0** (TK-7), so the nine digits are free for actions in every list (RP-D7).
- **RP-8.5 Defaults.** The Reports list's template (RP-5.1) and a GitHub list's `list.json` come with the keys above;
  an ordinary list has none until the user sets them.

## 9. Privacy and safety

- Everything sent is in fields the user can read and change before Enter (RP-3), redacted first (RP-4.3), never the
  text of an edit field or a conversation (RP-4.2). The token has the `public_repo` scope and lives in one protected
  file (RP-6.2); "Forget GitHub sign-in" removes it. An issue is public (RP-4.4), said at consent and in the form's
  title. The sender touches the network only on the user's action (RP-6.5). `PrivacyTests` keeps the repository free
  of what a report may contain, since nothing of a report is committed.

## 10. Decisions

- **RP-D1 GitHub's issues are the tracker for everyone** (the user, 2026-11-07), the maintainer's folder included
  (RP-7.3). Until then GitHub waited for users (`docs/feature_requests/README.md`, 2026-11-06).
- **RP-D2 Sent by axKit's own code over GitHub's REST API** (the user, 2026-11-07: a feature for all users, as an allowed
  network action), not through the GitHub CLI, a second program every user would have to install and sign in to. The
  report sender is the bundle's second network service beside the updater, and only the user's action starts it.
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
- The OAuth app: registered under the maintainer's GitHub account, device flow on, scope `public_repo`; the client id
  into the code and a setup page.

## 12. Build order

1. The ring of recent actions in the window base, the MCP's "said" reading it (RP-4.1); tested.
2. `Reports` in AxKit.Core: the folder, the template, the item writer and the body as Markdown (RP-5, RP-6.1); tested.
3. The notice, F4, the menu items, the palette and F1 words (RP-2, RP-3); the accessibility review; the NVDA step in
   the bundle section of one app's plan and `tests/nvda-scripts`.
4. `GitHub` in AxKit.Core: the device flow, create, list and patch, with recorded answers as fixtures (RP-6.2, RP-7.1).
5. The sender with its retry, the consent target, the token file, the rule changes and the pages (RP-6.4 to RP-6.6).
6. axTask: GitHub lists, the action keys and their notice, Ctrl+0 (RP-7, RP-8); TK-7 and IA-3 updated.
7. The labels, the issue forms' label, the maintainer's folder to issues (RP-6.1, RP-7.3).
8. Released as 5.1.0, after the NVDA passes.
