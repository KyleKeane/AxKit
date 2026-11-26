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

- **F4 in every window** (`BundleKeys.Report`, AX-8.2) opens the notice **Report to axKit** in the window, a form of
  standard controls with the field for the user's words focused (section 3; bundle AX-8.10). Enter there makes a new
  line; Tab reaches the kind, the box for more detail, what is sent with the report and the buttons; Ctrl+Enter or
  the Send button sends; Escape cancels and keeps nothing. The axKit
  menu has it as "Report a bug or an idea…" and, beside it, "Your reports", which opens the list of reports in axTask;
  the command palette lists both (AX-10.2).
- **Saved first, sent second.** Send writes the report as an item of the axTask list `Reports` under the user's axKit
  data folder, then sends it to GitHub as an issue of the reports repository over GitHub's REST API, through axKit's
  own reporter account, whose token ships inside the executable: nothing to sign in to (RP-D2). No network or a refusal
  leaves the item marked "Not sent yet", and it goes with the next report or when the list is opened (RP-D3).
- **Sealed on the user's computer, read by the maintainer alone** (2026-11-14, RP-D11). The reports repository is
  private, and before a report leaves the computer it is encrypted with the maintainer's public key: the issue holds
  a JSON envelope with the encrypted report and, in the clear, the install's random id, axKit's version and the
  report's kind (RP-6.1a). Only the maintainer's private key, which never leaves his computer, opens it. The copy in
  the user's Reports list stays plain: it is the user's own file.
- **Nothing to sign in to, and a way to cut off abuse.** Every report carries the install's reporter id, a random id
  made on first use. A block list the sender reads before sending, from a public repository of its own that holds
  only random ids and versions (RP-D12), the reports repository's own workflow, and the
  token's revocation and rotation at a release let the maintainer stop nonsense from one install or one version
  (RP-6.4). A version whose token was revoked says so and offers the report for copying.
- **axTask shows it all.** The Reports list is an ordinary axTask list with a template, so the user reads, edits and
  deletes reports as items, and Ctrl+1 to Ctrl+9 run the actions the user assigned to a list (section 8): set a
  field, move the item, open a field's address. axTask itself is local only: its GitHub lists of
  the repository's issues, built with the reports, were removed on 2026-11-14 (section 7, RP-D10).

## 2. The key and the menu

- **RP-2.1 F4.** Checked 2026-11-07: no window's key table uses F4 (Alt+F4 is Windows'). Ctrl+P, which the user also
  asked about, is three windows' own (axClip's pause, axMedia's pause, axTerminal's say-folder), and Ctrl+Shift+P was
  the palette; so F4, bundle-wide. `BundleKeys.Report = Keys.F4`, named "Report" in the keys table; `KeyTests` keeps the
  windows off it.
- **RP-2.2 The Help menu** (AX-9.1; the axKit menu until 5.7.0) gets, after the command palette and the guide: "&Send a report…" with F4 shown (S: R is
  Recently closed's letter), and "&Your reports", which opens `%APPDATA%\AxKit\reports` in
  axTask (a new axTask window through `WindowMenu.Start`, the folder and its `list.json` made first), then a separator (since 5.6.0 the three stand after the Claude submenu, AX-9.1). Both carry their words into every window's palette and F1 text through the
  window base; nothing is added to an app's own menus.
- **RP-2.3 While a notice shows,** F4 does nothing, as every window key (AX-6.5). The report's recent actions record
  the notice that showed last, so a problem with a notice is reported after closing it.

## 3. The form: the notice "Report to axKit"

A form of standard controls, used the standard way (docs/axKit/SPEC.md AX-8.10; the user's decisions of 2026-11-14,
RP-D14): one notice (`OverlayPanel`, bundle rule 2) with its text at the top, then real controls in Tab order, each
read by NVDA itself, then the buttons. The focus starts in the first control, ready to type.

The notice's text, the explanation and disclaimer, never shortened away and never a field of its own: "A report goes
to axKit's maintainer. It is encrypted on your computer and stored in a private repository on GitHub; only the
maintainer can read it. Nothing in it names you unless you write it. Type your words; Enter makes a new line. Tab goes
on to the kind, the box for more detail and what is sent with your report. Ctrl+Enter or the Send button sends. Escape
cancels." It says the form's keys itself; no line made from the buttons follows it.

The controls, in Tab order: four stops, then the buttons. The numbers are the ones this document and the code cite
(RP-3 item n), kept from the form of 2026-11-07:

1. **"Your report"**: a text box of several lines, focused when the form opens, empty. Enter makes a new line, as in
   every text box, and never sends; the window says "new line" (bundle AX-7.8).
2. **"Kind"**: a list of three, Feedback (selected), Bug, Feature request; Up and Down move, type-to-find reaches
   them (AX-8.6). NVDA reads the selected kind as the list moves, and the app adds nothing (AX-7).
2a. **"Add detail that may name your files: the window's title, the focused item, what the window said, the log as it
   is"**: a check box, not checked; Space toggles it and it says "checked" or "not checked" (AX-8.8). It decides
   which form of the next control's content is shown and sent.

Items 3 to 6 are one control, the fourth stop: **"What is sent with your report"**, a text box of several lines that
is only read. Under four short labels, with a blank line between them and "none" for an empty part, it shows what the
report carries of the window (`ReportPrivacy.Shown`):

3. **"App and version:"** "axClaude 5.0.1, Windows 11 build 26200, NVDA 2025.3" (NVDA's version from its running
   process, "NVDA not running" otherwise). The fuller form adds the window's title, redacted.
4. **"Last actions:"** oldest first, one per line: the commands run, "12 s ago: Find, Ctrl+F". The fuller form adds
   what the window said and the notices it showed, "12 s ago, said: 3 matches" (section 4).
5. **"Where the focus was:"** the kind of control and the place, "a list, item 7 of 40", "a text field, line 1 of 1".
   The fuller form names the control and a list's item: "Files, item 7 of 40: notes.md", "Message to Claude, line 1
   of 1 (the text is not included)"; never an edit field's text.
6. **"Log lines:"** the errors of the last day, at most twenty lines, every path and every quoted text masked. The
   fuller form holds the log's last twenty lines as they are, redacted.

While the check box is not checked the box holds the plain form (RP-4.2a); once it is checked, the fuller form;
checking or unchecking fills it afresh. The report carries exactly the context the box shows: the four parts are not
edited in the form (until 2026-11-14 each was a field of its own that could be changed). A user who does not want a
part sent leaves the box unchecked, or cancels.

7. **No title field** (there was one until 2026-11-14). The report's title is the first line of the user's words,
   eighty characters at most (`Report.TitleFrom`). It is sealed inside the report, and the issue's own title never
   used it (RP-6.1a).
8. **Send**, the notice's default button, and **Cancel**.

- **RP-3.1 What is said.** Opening: NVDA reads "Your report", the control the focus lands in; then the window says the
  notice's text once, so that the disclaimer is heard without leaving the field. Sending: "Sent, report 12" with the
  done chime (the number is the report's in the maintainer's list, to quote to him; "issue" is not said, since it
  would name a place the user cannot open); or "Saved; not sent yet, no network. It goes with the next report"; or,
  when this build or version cannot send (RP-6.4, RP-6.5), the notice that says why, with Copy report. Escape:
  "Cancelled", whatever was typed; nothing is kept (RP-D13).
- **RP-3.2 Nothing sent is lost.** The report is written to the Reports list before any network is touched, so a
  failed send is a saved item with its reason in the Status field, and a crash mid-way leaves the file. A form the
  user cancels is not a report and leaves nothing.
- **RP-3.3 Drafts of before.** Until 2026-11-14 Escape with words typed saved the report with the status Draft
  (RP-D6). Those items stay in Your reports with that status, to be read, changed or deleted there; no new draft is
  made.
- **RP-3.4 The keys.** Tab and Shift+Tab move through the controls and the buttons in the order above, and
  Shift+Tab from the first control reaches the notice's text. Ctrl+Enter presses Send from any control (the bundle's
  main action, AX-8.10). Enter makes a new line in "Your report"; in the kind's list, on the check box and in "What is
  sent with your report" it presses Send, the default button, as Windows does in a dialog; on a button it presses that
  button. The form has no one-line text box. Escape cancels the form from anywhere. Send with no words typed, however
  it was pressed: the focus goes to "Your report" with the edge earcon and "Type a few words first"; nothing is sent.

## 4. What is gathered, and what never is

- **RP-4.1 The recent actions.** The window base keeps a ring of the last twenty actions of the window: every command
  run through its command list (the menu path and key the palette shows, AX-10.2) and every announcement raised
  (`Announcer.Raised`, which already feeds the MCP channel's "said" list while a client records; the ring becomes the
  one copy both read). Each entry has a time; the report shows the age. **Every command run is written, by its name,
  never with what the user typed** (the maintainer's decision of 2026-11-25, `command-language.md` section 8 (a) 3):
  a command of the window's command list by `AxKitForm.Run`, by its key or its menu item; every other menu item (a
  setting, a recent file, a window of the Window menu) as it is picked, by the user, the palette, a script or an AI
  tool, from the one place every pick passes, the menu's `ItemClicked` (`CommandPalette.Marked` hooks it,
  `AxKitForm.WritePick`), before the item's own click so that it comes ahead of what the command says; and the keys
  of the whole window (F1, the palette, F4, Shift+F1, Ctrl+S, an app's launch key, the text size keys, F9) by the name
  of the item that shows the key (`AxKitForm.WriteKey`). One function words them all, `RecentActions.CommandText`:
  the name and the key as the menu shows it, "Find, Ctrl+F", "Word wrap", "Keyboard shortcuts, F1"; an entry of a
  numbered list of the user's own things (a recent file, a window, a folder, a template of the user's) by its menus
  and its number alone, "Session, Open recent file, entry 2", since its name may name a file (RP-4.2a). Until
  2026-11-25 only a command of the list was written. The title of each notice shown is written too, and a window
  whose announcements may carry the user's content (axPass, axClip, the console windows) writes its commands and
  notices alone (RP-4.2).
- **RP-4.2 Never the user's text.** The content of an edit field (the message to Claude, a document, a password), a
  conversation line of axClaude, a clipboard entry of axClip or a field value of axPass is never in a report. The
  focus line names the control and its place only. axPass's and axClip's windows (the MCP's never-read apps, `McpTools.NeverRead`,
  docs/axKit/mcp.md section 5) and the console windows, axClaude and axTerminal, whose spoken replies and output are
  the user's conversation (the first real report, 2026-11-08, carried a spoken reply of Claude's), put only their
  commands and notices in the ring, not their announcements (`AxKitForm.RecordsAnnouncements`).
- **RP-4.2a The plain form names nothing of the user's** (the user, 2026-11-14, after his own first two reports from
  a second computer showed a file's name and a folder's in public issues; both were deleted from GitHub). A report
  was public then; since the same day it is sealed for the maintainer alone (RP-4.4), who has no need of such names
  either, and may quote from a report in public (RP-4.4). Most are sent by typing and Enter, without a look at the other fields. So what the window adds by
  itself holds nothing that can name the user's files, folders, lists or items (`AxKit.Core/Reports/ReportPrivacy`):
  the app and the versions without the window's title; the kind of control in focus and its place without its name
  or the item's (`AxKitForm.FocusDescription(named: false)`); the commands alone, never what the window said nor a
  notice's title; of the log, only the errors of the last day with the lines of their stack, every path as `<path>`
  and every quoted text as `<text>` (`ReportPrivacy.Mask`, on top of `Redaction`), so no line of a start, which names
  the folder or file an app was started on, and nothing older. The fuller form (the title, the names, the spoken
  lines, the log's last lines as they are, each through `Redaction` as before) is the user's own choice, the check
  box of RP-3 item 2a, whose name says that it may name their files. What cannot be ruled out in the plain form: an
  error's message that names something without quotes and without a path; what is sent stays readable in the form before Send, in its one box (RP-3).
  Copies released before this (5.8.0 and earlier) send the fuller form.
- **RP-4.3 Redaction.** Every prefilled field goes through `Redaction` (AxKit.Core, S-20) before it is shown: the
  account's paths read `<user>`, an e-mail address `<email>`. The user reads what will be sent.
- **RP-4.4 Private and sealed** (until 2026-11-14: Public). A report is encrypted on the user's computer before it
  is sent (RP-6.1a). It is stored as an issue in a private repository on GitHub that only axKit's maintainer can
  open, posted by axKit's reporter account with the install's reporter id and nothing that names the user, and only
  the maintainer's key can read it. GitHub (Microsoft) stores the encrypted text and sees when and from which
  network address it was sent; GitHub's terms apply. Nothing in a report names the user unless they write it or
  check the detail box (RP-4.2a). The maintainer may quote or summarise a report in axKit's public repository when
  fixing it, without anything that identifies the user. The consent notice says all of this (RP-6.6), the form's
  text says the first three in two sentences, and the form's title stays "Report to axKit". A user who wants an
  answer writes how to reach them in the report; the form never asks for it. Copies released before this (5.8.0 and
  earlier) go on sending their reports as plain text into the same repository, private now, and go on saying
  "public issue" in their own texts; nothing in them can be changed, and what they say promises less privacy than
  they now get, apart from the reporter token's reach (RP-6.1a), which their reports are open to.

## 5. Saving: the Reports list

- **RP-5.1 The folder.** `%APPDATA%\AxKit\reports` is an axTask list (docs/axKit/item-apps.md section 4): `list.json`,
  the list's configuration with the fields below as its `item fields`, written when the folder is first made, and one
  JSON file per report, its values with the link to that configuration, named after its Title (IA-4.5). axTask opens
  it as any folder; the Help menu's "Your reports" opens it (RP-2.2). axJSON opens any item. A list an earlier
  version of axKit made, in the format before 2026-11-21, is converted the first time the report form or the sender
  meets it (`ReportFiles.EnsureList`; docs/axTask/forms-framework.md AF-13.6), a copy of the folder kept beside it as
  `reports, before conversion`, without a question: the list is the form's own, and a report that waits in it would
  otherwise never be read and sent. The report's id (RP-6.9) is a value of the file that is no field of the list: it
  is kept, and axTask names it in the item's last line ("1 more field kept: reportId").
- **RP-5.2 The fields** (the template's order): Title; Kind (choice: Feedback, the template's value, Bug or Feature
  request); Report (text, the user's words); App (text, as RP-3 item 3); Where (text); Actions (text, one action per
  line); Log (text); Created (date and time); Status (text, Draft as the template's value: Draft, Not sent yet, or Sent, with the reason after a colon when not sent: "Not sent
  yet: no network", "Not sent yet: this version can no longer send reports, update axKit", "Not sent yet: reports from
  this install are not being accepted", "Not sent yet: the report could not be encrypted"); Issue (link, the issue's web address once sent, which opens
  for the maintainer alone since the repository is private: for anyone else GitHub says the page is not there);
  Number (number, the report's, to quote to the maintainer). The item is plain text, as the user wrote it: it is the
  user's own file on the user's own computer, and only what is sent is sealed. The list's own Notes say where a
  report goes; a list made by 5.1.0 to 5.8.0, whose Notes said "public issue", gets the new words the next time a
  report is saved or Your reports opens, when the Notes are still that version's own (`ReportFiles.EnsureList`).
- **RP-5.3 Editing.** Until it is sent, every field edited in axTask goes into the report as edited. After, edits stay
  local. Delete archives as any item (IA-D4) and never touches GitHub.

## 6. Sending: GitHub's REST API, axKit's own reporter account

GitHub takes issues from signed-in accounts only, in the browser as in its API, and a sign-in would keep many people
from reporting at all (the user, 2026-11-07). So the sender signs in as axKit itself: a reporter account of the
maintainer's, whose token travels inside the executable. That token can be extracted by anyone who takes the trouble,
which is why it can do little, why every report names its install, why the maintainer can revoke it and block an
install or a version at any time (RP-6.2 to RP-6.4), and, since the token can also read the repository's issues, why
a report's content is sealed on the user's computer for the maintainer's key (RP-6.1a). The protocols for a compromise are settled when one happens; the
levers are built now.

- **RP-6.1 The request.** `POST https://api.github.com/repos/KyleKeane/AxKit-reports/issues` with the title, the body
  and one label from the kind (`feedback`, `bug`, `feature`), `Authorization: Bearer <reporter token>`,
  `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2022-11-28`. The answer's number and web address go into
  the item (Status "Sent"). The title is the kind and the version, "Bug report, axKit 5.9.0", "Feature request,
  axKit 5.9.0", "Feedback, axKit 5.9.0" (`ReportBody.IssueTitle`), never the user's words. The body is the envelope
  of RP-6.1a and nothing else. The
  labels exist in the reports repository; recategorising is changing the label (section 7). Until 5.8.0 the title
  was the report's own and the body the plain report.
- **RP-6.1a The envelope: a report sealed for the maintainer's key** (the user's decisions of 2026-11-14, RP-D11:
  "local encryption on the client side", and "a plaintext json with uid, axkit version, public key, encryption method,
  and body"). The reporter token is inside every copy of axKit and can read the repository's issues, so whoever
  takes it out of the program could read every report; making the repository private does not change that. So the
  sender encrypts the report on the user's computer, before any request, with the maintainer's public key, a
  constant in the code (`ReportSeal.PublicKey`, RSA 3072 bits, SubjectPublicKeyInfo in base64, as
  `Updates/ReleaseSigning` holds the release key's public half). Only the private key opens it, and that key is on
  the maintainer's computer, with a passphrase-encrypted backup beside its passphrase in the private repository
  `KyleKeane/AxKit-keys` since 2026-11-15 (`reports-setup.md` section 5). A report that cannot be encrypted is not sent
  ("Not sent yet: the report could not be encrypted").

  The issue's body is one JSON object, indented, inside a `json` code fence so that GitHub shows it as it is (the
  fence's two lines are the only thing in the body that is not the object). Its members, in this order, each a
  string:

  | Member | In the clear? | What it is |
  | --- | --- | --- |
  | `format` | yes | `axkit-report 1`: the envelope's name and version |
  | `note` | yes | "Encrypted for axKit's maintainer; only the maintainer's key opens it." |
  | `reporter` | yes | the install's random reporter id (RP-6.3), a UUID that names nobody |
  | `version` | yes | axKit's version |
  | `kind` | yes | the report's kind as its label: `feedback`, `bug` or `feature` |
  | `key` | yes | which public key the report was sealed for: a fingerprint, the first sixteen hexadecimal characters of the SHA-256 of the key's SubjectPublicKeyInfo. Not the key itself and nothing of the user's; it tells which private key opens the report, so that the maintainer's key can be replaced one day |
  | `method` | yes | `RSA-OAEP-SHA256+AES-256-GCM` |
  | `wrappedKey` | sealed | the report's own AES-256 key, fresh and random for each report, encrypted with the public key (RSA, OAEP with SHA-256), base64 |
  | `nonce` | sealed | AES-GCM's nonce, twelve random bytes, base64 |
  | `tag` | sealed | AES-GCM's check, sixteen bytes, base64 |
  | `body` | sealed | the report, UTF-8, encrypted with AES-256-GCM, base64 |

  The check covers the body and, as associated data, the members `format`, `reporter`, `version`, `kind`, `key` and
  `method`: a changed byte anywhere, or a body moved under another reporter id, version or kind, does not open. What
  is sealed (`ReportBody.Plain`): a line "Title:" with the report's title, a line "Kind:", an empty line, and the
  plain report as before (`ReportBody.Markdown`): the user's words first; then "### Context" with App and version
  and Where the focus was; "### Last actions" as a code block; the log lines inside a `<details>` block titled "Log
  lines"; last the line "Sent from axKit 5.1.0 with F4; reporter a1b2c3d4-…", kept inside so that the plain report
  is the same text Copy report gives. The body of the issue has no such line any more: the workflow and the
  maintainer read `reporter` and `version` from the envelope (`ReportSeal.Read`, which needs no key). Opening
  (`ReportSeal.Open`, used by `tools/ReportKey`) refuses, each with its own sentence and before anything is
  decrypted, a text without an envelope, a `format` or a `method` it does not know, and a `key` that is not the
  private key's fingerprint; then a member that is not base64 or has the wrong length, a wrapped key that does not
  unwrap, and a check that does not hold. What an envelope still tells whoever can read the repository: the members
  in the clear, when the issue was made, and about how long the report is.
- **RP-6.2 The reporter token.** A fine-grained personal access token of the maintainer's own GitHub account, with
  access to the reports repository alone and the permission Issues: read and write, nothing else: a leaked token can
  read, file, edit, label and close issues there, and nothing anywhere else (RP-D8); what it reads of a report sent
  since 2026-11-14 is the envelope, not the report (RP-6.1a). The issues it posts carry the
  maintainer's name as their author and the reporter line as their truth. (A reporter account of its own was the first
  thought; GitHub lets a fine-grained token reach only its own account's repositories, so such an account would need a
  classic token, which reaches everything it can see, or an organization; `reports-setup.md` section 1 says so, and an
  organization can come later without a change to the code.) It is never in the source: `release.yml`'s sign job, the "release" environment, which runs no test package, reads it
  from that environment's secret `AXKIT_REPORTER_TOKEN` and has `publish.ps1` write it, mixed with a fixed key and as
  base64, into `src/AxKit.Core/Reports/reporter.token` (`tools/reporter-token.ps1`), a file git ignores, which the
  build embeds when it exists and which the script removes again; `ReporterToken` reads it back at run time. The mixing
  keeps the token from a glance at the program file and stops nobody who looks for it, and this page says so. The build
  job, which runs the tests and their packages, never sees the token (S-30, `ReleaseWorkflowTests`). A build without the secret (a developer's build, a fork's) has no token: its sender says
  "This build cannot send reports" and offers RP-6.5's Copy report. GitHub's terms ask that credentials be kept
  confidential; the machine user with one repository and one permission is the closest a token in a program comes to
  that, and a revocation by GitHub costs one release (RP-6.4). The setup page `docs/axKit/reports-setup.md` records
  the account, the repository, the token's scope and the secret, as the code signing record of 2026-11 did for signing.
- **RP-6.3 The reporter id.** A random GUID made on the first report and kept in `%APPDATA%\AxKit\reports.json`,
  in every report's last line. It names the install, not the person: no account, no name, no hardware id, and
  deleting the file gives a new one. The maintainer filters and, if need be, blocks by it (RP-6.4). `PRIVACY.md` says
  so.
- **RP-6.4 Abuse: the levers.** Three, cheapest first. The **block list**, `blocklist.json` of the public repository
  `KyleKeane/AxKit-blocklist`, which holds nothing but the list (RP-D12; until 2026-11-14 a file of the reports
  repository): `{"reporters": ["<install id>"], "versions": ["5.1.0"]}`, random install ids and axKit versions. The
  sender reads it before a send, with one `GET` of
  `https://raw.githubusercontent.com/KyleKeane/AxKit-blocklist/main/blocklist.json` (the address is written once in
  the code, `GitHubIssues.BlocklistUrl`, from `ReportSender.BlocklistRepository`), without a token and with nothing
  of the user's but the request, and keeps the answer a day; a missing file is an empty list. A listed install or
  version posts nothing: not the report just written, not an older unsent one; each stays in Your reports as "Not
  sent yet" with the reason. After a day the list is asked again, so an install taken off it sends again by itself,
  with the next report or when Your reports opens. The user is told plainly, in the notice **"Report not sent"**:
  - a blocked install: "Reports from this install of axKit are not being accepted. Nothing was sent. Your report
    stays saved in Your reports on this computer." "If this is a mistake, ask on the block list's page: open an
    issue there and give your install id. The id is random and names nobody." "Your install id: <id>". Its choices:
    **Copy install id** (the notice stays, "Copied"), **Open the block list page**
    (`https://github.com/KyleKeane/AxKit-blocklist/issues` in the browser, `ReportSender.BlocklistIssuesUrl`; the
    user asks there with a GitHub account of their own, never in the reports repository, which no user can open),
    and **Close**, Enter's and Escape's choice;
  - a blocked version: the same first three sentences with "this version of axKit", then "Update axKit from the
    Help menu, or from the Microsoft Store if you installed it there. After the update, open Your reports from the
    Help menu, and the report is sent."; Close alone.

  No form in the app asks to be unblocked, and there is no rate limit beyond the half-minute gap: for real abuse
  the lever is the token (below). The maintainer changes the file on GitHub
  itself, in the browser or with `gh` (`reports-setup.md` section 3): axKit only reads it, and nothing in axTask writes it (RP-D10; Block this
  reporter and Block this version were removed 2026-11-14). Copies up to 5.8.0 ask for the list at its old address
  in the reports repository, which answers "not found" since that repository is private, so their own check finds
  an empty list; the workflow stops what they send. The **workflow** in the reports repository
  (`issues: opened`) reads `reporter` and `version` from the envelope in every new issue's body (and, for an issue
  of 5.8.0 or earlier, from its last line), fetches the same public list, and closes and locks an issue from a blocked id or version at
  once, so the lever holds whatever the sender did. The **token**: revoked on GitHub and
  replaced, the secret updated, a release made; every earlier version gets GitHub's 401 and says "This version can no
  longer send reports", with RP-6.5's button. Revoking also at
  least once a year goes into `docs/axKit/longevity.md`'s yearly check. One report per thirty seconds per install, from
  the sender's side, keeps a stuck key from flooding.
- **RP-6.5 When sending cannot happen.** The report is saved whatever happens (RP-3.2). The notice that says why has
  one button beside Close: **Copy report** (the plain report, `ReportBody.Markdown`, to the user's own clipboard, to
  paste anywhere; the notice says that it is not encrypted). **Open GitHub's form**, the browser on the reports
  repository's new-issue form, was offered until 2026-11-14 and removed: the form posts under the user's own GitHub
  account into a repository that is private now, which no user can reach, and it would post plain text. No e-mail route: an address cannot live in the repository (`PrivacyTests`, bundle rule 6), and the user
  chose the token over it.
- **RP-6.6 Consent** (AX-11.4, AX-11.5): before the first send, the consent notice for GitHub, a new
  `ConsentTarget.GitHubReports`, titled "Before the first report goes": "Your report is encrypted on your computer
  before it is sent. It is stored as an issue in a private repository on GitHub that only axKit's maintainer can
  open, and only the maintainer's key can read it. GitHub, a Microsoft company, stores the encrypted text and sees
  when and from which network address it was sent. It is posted with axKit's reporter token, never with an account
  of yours, and carries a random id of this install. Nothing in it names you or your files unless you write it or
  check the box for more detail. The maintainer may quote or summarise a report in axKit's public repository when
  fixing it, without anything that identifies you." Then: "GitHub's terms and privacy statement apply to what GitHub
  stores, between you and GitHub:" with the address, and "Your report is saved in Your reports whatever you choose
  here; Not now leaves it there, unsent, and asks again with the next one." The check box: "I understand that my
  report is sent encrypted to a private repository on GitHub, that only axKit's maintainer can read it, and that
  GitHub's terms apply to what GitHub stores". Recorded in `consent.json` with its date under the app "axKit" and the
  target "GitHub, encrypted reports", shown in About, Licence. **Asked again after the change of 2026-11-14:** the
  agreement of 5.1.0 to 5.8.0 was recorded under the target "GitHub" for words that said "public issue". `Consent`
  has no version of a text; an agreement is found by its app and its target, so the target's name changed with the
  terms, the old record does not count, the notice shows again before the next report, and the old record is taken
  out of the file when the new one is written.
- **RP-6.7 When sending runs.** Right after a report is saved, and then every "Not sent yet" item of the list, oldest
  first; when "Your reports" opens the list from the Help menu (axTask says "2 reports sent" or the reason); never at
  a window's start: a standalone app stays off the network until the user acts, the update check being the bundle's
  one automatic request (G6, AX-3). Each send has a ten-second limit; its reason, in a few words, goes into Status.
- **RP-6.8 The rule changes, in one commit with the code** (bundle rule 8, B-15, G6, AX-11.11, `PRIVACY.md`,
  `README.md`, `docs/axKit/README.md`, `SECURITY.md`): axKit's own code makes network requests in two places, the
  updater and the report sender, both over HTTPS to GitHub, the report sender only when the user acts (RP-D2). From
  2026-11-07 to 2026-11-14 axTask's GitHub lists were a third place (section 7, RP-D10). The
  two endpoints (the issue, at `api.github.com`; the block list, at `raw.githubusercontent.com`, in a public
  repository of its own since 2026-11-14) carry `Outside call, network, GitHub:` comments and appear on
  `docs/axKit/outside-calls.md` (B-17). The security audit gets entries for the token in the build, the reporter id
  and the redaction of what is sent. Since 2026-11-14 those pages also say that the report is encrypted on the
  user's computer and that the repository is private (RP-6.1a, RP-4.4).
- **RP-6.9 Only what the report form wrote is sent** (2026-11-16, `repairs.md` R-57). The sender used to send every
  waiting item file it found in the reports folder, so an item another program put there, holding anything, would
  have gone out with the user's next report. axKit now keeps the names of the report files its own form saved, in
  `%APPDATA%\AxKit\reports-written.json` under the settings' seal (bundle AX-5.6, `SettingsSeal`;
  `AxKit.Core/Reports/ReportWritten`): the name is written when the report is saved (`ReportFiles.Save`, the name
  first, then the item; when the list cannot be written the report is saved all the same and waits). The sender
  sends only waiting items whose file name is on the list (`ReportFiles.UnsentOfTheForm`); the others stay where
  they are, are not marked and are never sent, and the outcome counts them (`SendOutcome.NotOfTheForm`). When the
  count is not zero, what the window says after a send ends with one sentence: "2 files in the reports folder were
  not written by axKit's report form and were not sent." ("1 file ... was not ..."). The list is by name, not by
  content: a report the user changes in axTask is still sent. **By its id as well** (2026-11-16, the leftover of
  R-57): axTask names an item's file after its title, so a waiting report whose title the user changed there got a
  new file name, fell off the list and was never sent. The form now writes a random id into each report's file as
  it saves it (the root key `reportId`, `ReportFiles.IdKey`, which axTask keeps through every change and rename)
  and puts it on the list beside the name, as an entry "id:" and the id, which no file name can be. A waiting item
  is the form's when its file name or its id is on the list. The id stays on the computer: it is no part of what is
  sent. Its limits: a report an earlier version saved has no id and is known by its name alone; a file that copies
  a listed report's id counts as that report, as a file put in the place of a listed name does.
  **Reports from before the list.** A report an earlier version saved and could not send is on no list. The list
  counts as "not kept yet" until the first report this version saves; until then the sender sends as before, and
  that first save takes the names of the reports that wait in the folder at that moment onto the list, so that
  none is left behind. After that, a list that is deleted, or whose seal does not check, counts as kept and empty
  (`SettingsSeal.WasSealed`): taking the list away does not bring the earlier time back. No network contact is
  added or changed. `ReportSenderTests`, `SettingsSealTests`.

## 7. The maintainer's side: the GitHub lists of axTask, removed

What happens to a report once it is in, its harm review and the maintainer's signed approval before any work, is
`docs/axKit/outside-reports.md` (2026-11-21).

- **RP-7.1 What they were.** From 2026-11-07 to 2026-11-14 (5.1.0 to 5.8.0) axTask had GitHub lists: a list whose
  `list.json` had `"github": {"repository": "owner/name", "state": "open"}` filled itself from that repository's
  issues when it opened and on F5, one item per issue with the user's Notes kept, and Context → GitHub on an item
  changed the issue with a token of the user's own (the kinds, Close, Reopen, Comment…, Block this reporter, Block
  this version), opened it on GitHub or opened it in axClaude.
- **RP-7.2 Why they went** (the user's decisions of 2026-11-14, RP-D10). First the actions and the token: they were
  placeholders for operating GitHub from a task list, never used in earnest. Then the reading too: "axTask is local
  only". A task list that also speaks to a service is two things, and a complex interaction such as GitHub belongs in
  an app of its own, where the program that signs in and writes is one place (rule 12). So nothing in axTask
  contacts GitHub or any other host, and axTask keeps no token; `GitHubIssues` lost its listing, patch, comment and
  blocklist-writing calls and keeps what the report sender uses. A list made as a GitHub list stays as an ordinary
  local list with its items, its `"github"` member kept and meaning nothing (docs/axTask/SPEC.md TK-7a.2). A token
  an earlier version kept stays in `%LOCALAPPDATA%\AxKit\github.token`, unused, for the user to delete.
- **RP-7.3 How the reports are read now.** The reports are the issues of `KyleKeane/AxKit-reports`, private since
  2026-11-14. On GitHub and through `gh` a sealed report shows its envelope alone; its text is read on the
  maintainer's computer with `tools/ReportKey issues` (every open report: number, labels, state, title, the
  envelope's members in the clear, then the plain report; an issue of 5.8.0 or earlier is shown as it is and marked
  "not sealed") or `tools/ReportKey open <file>` for one body, by the maintainer and, on that computer, the
  assistant. `tools/reports-check.ps1`, which reads numbers, states and titles alone, run with every push, names each open
  report that no item of the priorities list points at, each open report whose items are done, and each closed
  report with an item that is not (root `CLAUDE.md` rule 9). An issue is relabelled, closed, answered or blocked on
  GitHub itself (RP-6.4). The folder `docs/feature_requests` went to issues on 2026-11-07.
- **RP-7.4 The later plan.** axGitHub: an access layer over the `gh` CLI for issues, on the shared item window, which
  may reuse what the GitHub lists had (`docs/axKit/plan.md`, Still to come; asked 2026-11-14, not built).

## 8. Ctrl+number: the user's own actions on items (axTask, every list)

- **RP-8.1 The keys.** Ctrl+1 to Ctrl+9 in axTask, anywhere in the window, run the action the current list assigns to
  that number, on the item selected in the navigator (or the item whose fields the pane shows). The list's `list.json`
  holds them among its `"actions"`, each with its name, its key and what it does (since 2026-11-21,
  docs/axTask/forms-framework.md section 3; under `"keys"` by digit before, `"1": { "name": "Done", "set": { "Done":
  true } }`, which the conversion of a list turns into these, the same keys and the same actions):

  ```json
  "actions": [
    { "name": "Done", "key": "Ctrl+1", "does": "set", "set": { "Done": true } },
    { "name": "Make it a bug", "key": "Ctrl+2", "does": "set", "set": { "Kind": "Bug" } },
    { "name": "Move to Later", "key": "Ctrl+3", "does": "move", "to": "../Later" },
    { "name": "Open on GitHub", "key": "Ctrl+4", "does": "open", "open": "Issue" }
  ]
  ```

  One kind of action per key: `set` (fields to values, one undo step, IA-5.4), `move` (to a list by a path relative to
  this one, IA-D3), `open` (a field's web address, handed to the browser, or an app's verb on the item's file). Every action works on
  the files of the computer: axTask sends nothing anywhere (RP-7.2). A key without
  an entry: the edge earcon and "No action on Ctrl+3 in this list; Actions, Action keys sets one". An `open` of
  `claude` or `terminal` starts its program only in a folder on a drive of this computer: on
  a share or a mapped drive the key says "Not started: axTerminal would run in a folder that is not on a drive of this
  computer" and starts nothing, since a `list.json` may come from a list the user did not write (security audit S-5's
  rule; the AI tools' `axkit_open` starts the two only after the user's yes in a window, mcp.md MC-6.5a; 2026-11-09, asked since 2026-11-24). For the same reason a `move` or
  an `open` that this install neither wrote nor accepted is said at the first press and done at the second
  (docs/axTask/SPEC.md TK-7a.3, 2026-11-16).
- **RP-8.2 Assigning them.** *(Until 2026-11-21; since then an action is written in the list's configuration, `list.json`, edited in axJSON from the list, and axTask has no form for it: docs/axTask/forms-framework.md section 10.)* Actions → "Action keys…": one form of standard controls (bundle AX-8.10, since
  2026-11-14; a list of the keys and then up to three notices before): the list of the nine keys with their names
  ("Ctrl+1. Done", the full stop as AX-8.5's pause); the name, where an empty one removes the key; a list of what the
  key does (Set fields, Move to a list, Open); and the value in a box of several lines. Moving through the keys fills
  the other fields; Ctrl+Enter or Keep writes it. Written into `list.json`, so axJSON (Ctrl+J) edits them too.
- **RP-8.3 Shown everywhere a command is.** Context on an item lists the assigned actions with their keys, so the palette and
  F1 list them (AX-10.2); the names are the user's.
- **RP-8.4 All tasks moves to Ctrl+0** (TK-7), so the nine digits are free for actions in every list (RP-D7).
- **RP-8.5 Defaults.** The Reports list's `list.json` (RP-5.1) comes with 1 to 3 the kinds, since
  its items are the user's own reports (until 2026-11-14 also 4, Open on GitHub, which a new list no longer gets:
  the issue is in a private repository; a list made earlier keeps its keys, the user's to change); an ordinary list has none until the user sets them. A list made as a GitHub
  list keeps the keys it was given, which act on its files alone (RP-7.2).

## 9. Privacy and safety

- Everything sent is shown in the form before Send, the user's words in a field they type and the rest in a box they can read (RP-3), redacted first (RP-4.3), never the
  text of an edit field or a conversation (RP-4.2). Nothing names the user: the reporter account is axKit's, the
  reporter id is a random id of the install (RP-6.3). A report is sealed on the user's computer for the maintainer's
  key and stored in a private repository (RP-4.4, RP-6.1a), said at consent and in the form's text. The sender touches the network only on the user's action (RP-6.7). The reporter token can reach one
  repository's issues and nothing else, and is revoked and replaced at will (RP-6.2, RP-6.4); axTask
  holds no token and asks the network nothing (RP-7.2). `PrivacyTests` keeps the repository free of what a report may
  contain, since nothing of a report is committed, and the token never enters the source (RP-6.2).

## 10. Decisions

- **RP-D1 GitHub's issues are the tracker for everyone** (the user, 2026-11-07), the maintainer's folder included
  (RP-7.3). Until then (2026-11-06) GitHub waited for users.
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
- **RP-D9 The fallback is copying** (and, until 2026-11-14, GitHub's form), offered by the notice that says why a
  send cannot happen (RP-6.5); nothing is ever lost (RP-3.2).
- **RP-D3 Saved first, sent second** (the user): a report is an item on disk before any request, unsent ones go with
  the next, and nothing is sent at a window's start.
- **RP-D4 F4, bundle-wide**; Ctrl+P is three windows' own (RP-2.1).
- **RP-D5 One key, one kind of action, as data in `list.json`**: set, move or open, no scripting (B-10).
- **RP-D6 Escape keeps a draft** when words were typed: a blind user's Escape by habit must not lose a paragraph.
  **Reversed on 2026-11-14 by RP-D13**; kept here as the record of what the form did from 5.1.0 until then.
- **RP-D7 All tasks to Ctrl+0**, the digits to the list's actions.
- **RP-D10 axTask is local only; the GitHub lists are removed** (the user, 2026-11-14, in two steps the same day).
  First: the GitHub lists only read; the actions on an issue (the kinds, Close, Reopen, Comment, Block this reporter,
  Block this version) and the user's token were removed, not hidden. Then: "axTask is local only", so the reading
  went too, with New GitHub list, the GitHub submenu and Open in axClaude; nothing in axTask contacts GitHub. axKit's
  own code makes network requests in two places from then on, the updater and the report sender. A complex
  interaction such as GitHub gets an app of its own later, axGitHub, an access layer over the `gh` CLI on the shared
  item window (rule 12), which may reuse components (RP-7.4).
- **RP-D11 2026-11-14: the reports repository is private and every report is sealed on the user's computer with the
  maintainer's public key** (the user: "i want the issue reporting repo made private and i want the local encryption
  on the client side. we should still keep disclaimers and try to respect every level of their privacy and try to
  remove all personal info"; and, on the form of what is sent: "they could send a plaintext json with uid, axkit
  version, public key, encryption method, and body. so that we are sending json with metadata to keep things useful
  and can then decrypt"). Private alone is not enough, because the reporter token in every copy can read the
  repository; so the content is encrypted for a key whose private half is on the maintainer's computer alone, and
  the issue holds a JSON envelope with the metadata in the clear (RP-6.1a; "public key" there is the key's
  fingerprint, not the key). The title names the kind and the version alone. Every text says so (RP-4.4), the
  agreement is asked again (RP-6.6), GitHub's own form is gone (RP-6.5), and the maintainer reads reports with
  `tools/ReportKey` (RP-7.3). Released copies, 5.8.0 and earlier, cannot be changed: they send plain text into the
  now private repository and say "public".
- **RP-D12 2026-11-14: the block list lives in a public repository of its own**, `KyleKeane/AxKit-blocklist`, that
  holds only random install ids and versions (the user, the same day; first weighed: removing the sender's check,
  since a private repository's file cannot be read without a sign-in). The app goes on checking it before sending.
  A blocked install is told so in the app, with its install id and the page where to ask if it is a mistake, the
  block list's own issues ("the user should file their issue on the repo that holds the block list not on the main
  repo"); a blocked version is told to update. No request form in the app and no further rate limit (the
  half-minute gap stays); for real abuse the lever is revoking the reporter token (RP-6.4). The report sender
  therefore contacts two addresses of GitHub: the list's raw file, a read without a token, and the API that takes
  the sealed report.
- **RP-D13 2026-11-14: Escape cancels the report whole, and no draft is kept** (the user; bundle AX-8.10 rule 7).
  Escape on a form cancels it all, as Windows does: this reverses RP-D6. Reports saved as drafts before stay in Your
  reports with the status Draft (RP-3.3); Escape makes no new one, and the window says "Cancelled".
- **RP-D14 2026-11-14, late afternoon: the report form is standard controls** (the user, after trying the form built
  that morning as one list of fields with a Send row, which he withdrew: it did not open ready to type, sending took
  too many steps, too much was read aloud, the disclaimer was buried, and it did not feel like a form; "we need to
  just go with standard form controls so that user expectations are met and that we are teaching good habits"). So:
  the form opens in the words, ready to type; Tab moves through real controls; plain Enter in the words is a new line
  and never sends (a stray Enter had sent an unfinished report that morning, when Enter sent and Shift+Enter broke a
  line); Ctrl+Enter or the Send button sends; the explanation and disclaimer are the notice's text, said as the form
  opens; what the window adds is one box that is only read, four stops before the buttons where there were eight,
  and the report carries that context as it is; the title field is gone, the title being the first line of the words.
  The list form is not to be built again (bundle AX-8.10 rule 13).

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
   (Steps 4 and 7 as built on 2026-11-07; the GitHub lists, with the listing, the patch, the comment, the blocklist's
   writing and the user's token, were removed on 2026-11-14, RP-D10; the action keys and Ctrl+0 stay.)
8. The labels, the issue forms (a bug form and an idea form in the reports repository too), the maintainer's folder
   to issues (RP-6.1, RP-7.3).
9. Released as 5.1.0, after the NVDA passes.
