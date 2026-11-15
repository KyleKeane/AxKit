# AxKit.Core notes: Text and the editors

- `TypedLineBreak` (AX-7.8, 2026-11-11): whether a key put a line break into a field, from the text and selection as
  the key went down and the text and caret after it (the selection replaced by a line break, an indent after it
  allowed, the caret after that), and `Said`, "new line"; `AxKit.Forms/AxKitTextBox` asks it. The setting is the theme
  file's `display.sayNewLine` (`Theme.SayNewLine`, `SetSayNewLine`, TH-6.5); the heading sound's switch beside it is `display.headingSound` (`Theme.HeadingSound`, `SetHeadingSound`, TH-6.6). Tested in `TypedLineBreakTests`.
- `Paragraphs`: where Ctrl+Up and Ctrl+Down go in a text with `\r\n` line ends: the starts of its paragraphs, the
  first line or a line with text after a blank line, for `AxKit.Forms.EditKeys.Paragraph`.
- `TextLines` (docs/axDown/SPEC.md AD-6.4, AD-3.6; 2026-10-23): the caret's line and column in an edit control's text,
  and the index of a line and column put back, clamped to the line and never between a CR and its LF.
- `MarkdownOutline` (docs/axDown/SPEC.md §9; from AxDown.Core): the elements of a Markdown text for navigation keys,
  outside fenced code blocks: headings with a level filter, links, list items and lists, block quotes and tables,
  each with `Next…` and `Previous…` from a caret position returning an `Element` with `Start` and `Spoken`; block
  kinds by line, links by position; `HeadingLineAt`, the caret's line when it is a heading, for the heading sound (AX-7.9).
- `TextDocument` (docs/axDown/SPEC.md AD-3; from AxDown.Core, axJSON): a text file as an editor window holds it, with
  the encoding, the byte order mark and the line ending it came with, so that `ToBytes` writes it back as it was,
  every line ending of the text the file's (a lone LF pasted from a Unix clipboard included); `FromBytes` and
  `ToBytes` are the pure core, `Read` and `Write` add the file and the 32 MB limit.
- `EditorSettings` (AD-8; from AxDown.Core, axJSON): what an editor window remembers between runs (font, word wrap,
  recent files, window, the update check), read and written through `SettingsFile` into the app's own file.
- `EditHistory` and `EditStep` (AD-2.5, 2026-10-21): the editor shell's undo and redo history: `Diff` finds the one
  step between the text before and after a change by the common prefix and suffix, the caret settling an ambiguous
  place ("a" typed next to an "a"); `Record` merges a change into the open run when its key was typed at `RunCaret`
  (more typing, a Backspace at the run's end, a Delete at its caret) and makes any other change (`typedAt` -1: a
  paste, a cut, a command, a reload) a step of its own that closes the run; `Break` is the window's second of silence;
  `Undo` and `Redo` hand the step back for the window to apply through the selection; four million characters of
  deltas at most, the oldest dropped.
- `ItemDocument`, `ItemField`, `FieldTypes` and `Parsed` (docs/axKit/item-apps.md IA-4; from AxTask.Core, axPass,
  2026-10-24): the item apps' format, an item as a list of fields of a title, a type and a value kept whole as JSON,
  the types read out and typed (dates as tomorrow or +3); a list's template (`NewList`, `WithTemplate`) is written
  with its values, which a new item starts with (IA-4.3; emptied until 2026-11-09, so `ReportFiles.ListText` wrote
  its own and a GitHub list's new items had no kind or state).
- `FieldStep` (AX-8.10 rule 4, IA-5.6): Right and Left on a field: a choice's next or previous value, a check
  checked or unchecked, null (the edge sound) otherwise, never a `Protected` type.
- `ItemFiles` (IA-4.5; from AxTask.Core's `ItemFolder` on its second use, the reports list, 2026-11-07): `FileStem`,
  the file name a Title gives, and `FreePath`, a free name beside the taken ones ("name - 2.json").
- `Reports/` (docs/axKit/reports.md, 2026-11-07): `Report` with `ReportKind`, `ReportKinds` (the names, the GitHub
  labels, `Parse`) and `ReportStatus` (Draft, Not sent yet with its reason, Sent, `AwaitsSending`); `ReportFiles`, the
  Reports list as an axTask folder (the template, `ListText` with the action keys, `ToItem`, `FromItem`, `Save`,
  `Write`, `WithStatus`, `WithSent`, `Unsent`; `EnsureList` also replaces the Notes of a list made when reports
  were public, `ListNotesBefore`, by `ListNotes`); `ReportBody`: `Markdown`, the plain report with the reporter line
  last, and `ParseReporterLine`; `Plain` (the title and the kind, then `Markdown`), which is what is sealed;
  `IssueTitle` (the kind and the version, never the user's words) and `Sealed`, the issue's body, the envelope alone
  in a `json` code fence. Tested in `ReportsTests`. `ReportSeal` with `ReportEnvelope` and `ReportSealException`
  (RP-6.1a, 2026-11-14): a report encrypted on the user's computer for the maintainer's key, since the reporter token
  in every copy can read the reports repository: `Seal` (a fresh AES-256-GCM key and nonce, the key wrapped with
  RSA-OAEP-SHA256) gives the JSON envelope, in the clear `format`, `note`, `reporter`, `version`, `kind`, `key` (the
  `Fingerprint` of the public key, sixteen hexadecimal characters) and `method`, sealed `wrappedKey`, `nonce`, `tag`
  and `body`; `Read` gives the envelope of a text without any key, null where there is none; `Open` refuses an
  unknown format or method and another key's fingerprint before it decrypts, and a changed member after;
  `PublicKey` is the maintainer's public half, whose private half is on his computer alone (`tools/ReportKey`,
  docs/axKit/reports-setup.md section 5). Tested in `ReportSealTests` with pairs made in memory. `ReportPrivacy` with `ReportContext` (RP-4.2a, 2026-11-14): what a
  report carries by itself, nothing that can name the user's files: `AppLine` without the window's title, `Commands`
  (the commands alone of `RecentActions.Entries`), `Errors` (the log's errors of the last day, masked) and `Mask`
  (every path and quoted text, on top of `Redaction`); the fuller form is the user's check box, `DetailBox`; `Shown`, either form as the lines the report form shows. Tested
  in `ReportPrivacyTests`. `GitHubIssues` (RP-6), GitHub's REST API as the report sender uses
  it, the client handed in: `CreateAsync` (an issue as the reporter account), `ReadBlocklistAsync` (the list at
  `BlocklistUrl`, the one place that names its address: `blocklist.json` of the public repository
  `ReportSender.BlocklistRepository`, KyleKeane/AxKit-blocklist, which holds only the list, since 2026-11-14, RP-D12;
  read without a token, 404 is an empty list; the listing of issues and every write
  but the report went on 2026-11-14 with axTask's GitHub lists, RP-7, and the browser's form the same day, RP-6.5),
  `ReasonOf` (an exception as a few words for the Status field); `GitHubException` with its `Reason`;
  `Blocklist` (`Parse`, `Blocks`, `BlocksReporter`, `BlocksVersion`, `ToJson`, the copy the sender keeps a day); `IssueCreated`. Tested in
  `GitHubIssuesTests` against canned answers. `ReportSender` (RP-6.3 to RP-6.7): `ReporterId` (made once, kept in
  `AppPaths.ReportsFile` as `ReportSettings`), `SendUnsentAsync` (no token: every item says so; the blocklist read
  before a send and kept a day, a listed install or version posting nothing, `BlockedInstallReason` or
  `BlockedVersionReason`, and asked again after the day; `BlocklistIssuesUrl`, where a blocked install asks; the
  half-minute gap; each item sealed for the public key handed in, then posted
  under `ReportBody.IssueTitle` and marked Sent with its issue, or marked with the reason, `SealReason` for one that
  could not be sealed, which is never sent plain; the first failure stops the run), `SendOutcome`; `ReporterToken` (RP-6.2): the token the release build
  embeds from `Reports/reporter.token`, `Encode` and `Decode` (the mixing `tools/reporter-token.ps1` repeats), null in
  a build without it. Tested in `ReportSenderTests`.
- `AxKitHttp` (AX-11.11, 2026-11-07): the one HTTP client of axKit's own code, the updater's and the report sender's,
  with the User-Agent `AxKit/<version>`; from the updater on its second use.
- `MediaFiles` (docs/axMedia/SPEC.md AM-4; from AxFile.Core, axMedia, 2026-10-22): the rules of sound and video files
  that need no engine, the extension sets with `IsMedia`, the time format and the typed time, the speed steps.
