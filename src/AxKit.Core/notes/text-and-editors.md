# AxKit.Core notes: Text and the editors

- `TypedLineBreak` (AX-7.8, 2026-11-11): whether a key put a line break into a field, from the text and selection as
  the key went down and the text and caret after it (the selection replaced by a line break, an indent after it
  allowed, the caret after that), and `Said`, "new line"; `AxKit.Forms/AxKitTextBox` asks it. The setting is the theme
  file's `display.sayNewLine` (`Theme.SayNewLine`, `SetSayNewLine`, TH-6.5); the heading sound's switch beside it is `display.headingSound` (`Theme.HeadingSound`, `SetHeadingSound`, TH-6.6), and the switch of the sounds of what opens `display.openingSounds` (`Theme.OpeningSounds`, `SetOpeningSounds`, TH-6.7, bundle AX-7.11). Tested in `TypedLineBreakTests`.
- `TextModes` (AX-8.13, 2026-11-15): the words of the mode key ("Edit mode", "Navigation mode", "Read only: there is no edit mode here", the menu refusal "Navigation mode: press Ctrl+Space to edit", the status part and the key test's words) and the rule of navigation mode by virtual key code and modifiers (`InNavigation`: `Refused` for what types or deletes, pastes, cuts, undoes or sends; `Passes` for moves, copy and every other command), with `CharacterAllowed` for the control's own guard. `AxKit.Forms/AxKitTextBox` and the window base apply it. Tested in `TextModesTests`.
- `Paragraphs`: where Ctrl+Up and Ctrl+Down go in a text with `\r\n` line ends: the starts of its paragraphs, the
  first line or a line with text after a blank line, for `AxKit.Forms.EditKeys.Paragraph`.
- `TextLines` (docs/axDown/SPEC.md AD-6.4, AD-3.6; 2026-10-23): the caret's line and column in an edit control's text,
  and the index of a line and column put back, clamped to the line and never between a CR and its LF.
- `MarkdownOutline` (docs/axDown/SPEC.md §9; from AxDown.Core): the elements of a Markdown text for navigation keys,
  outside fenced code blocks: headings with a level filter, links, list items and lists, block quotes and tables,
  each with `Next…` and `Previous…` from a caret position returning an `Element` with `Start` and `Spoken`; block
  kinds by line, links by position; `HeadingLineAt`, the caret's line when it is a heading, for the heading sound (AX-7.9).
- `MarkdownEdit` with `MarkdownChange` (docs/axDown/SPEC.md AD-12, 2026-11-22): the format commands of a Markdown
  text, each a pure function from the text and the caret or the selection to one change (what replaces which
  characters, the caret after, the sentence, and whether nothing could change, for the edge sound), and each a
  toggle: `Heading` (a level set, changed, or taken away), `HeadingDeeper` and `HeadingShallower` (a number sign
  more or fewer), `Bold`, `Italic` and `Code` (around the selection, or the caret's word, or empty marks to type
  into; two or three asterisks are bold, one or three italic), `ListItem` and `BlockQuote` (on every line the
  selection touches; empty lines carried along and not counted). A line's ending is never touched.
- `TextTemplate` with `TemplateText` (docs/axDown/SPEC.md AD-13, 2026-11-23): an editor's template, plain text with three marks: `{caret}` where the caret lands (the first one; after the text without one), `{selection}` the text selected when the template was chosen, `{clipboard}` the clipboard's text. `Expand` fills them and gives the text and the caret's offset; what comes in through a mark is never read for marks, other braces are the template's own, line endings become CR LF. `NameOf` names a template file by its file name. Placeholders and forms are paused, the maintainer's word.
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
  2026-10-24): what the item window shows and changes, an item as a list of fields of a title, a type and a value,
  the types read out and typed (dates as tomorrow or +3). In memory only since 2026-11-21 (`Of`, `NewItem`; no
  file shape of its own): axPass makes it from an entry, axMCP from a tool's arguments, axTask from an item's
  values read through its list's configuration. `FieldTypes.OfKind` (a value's type by JSON's own kind), `Holds`
  (whether a type holds a value; one it does not is read "..., kept as text: the field is now a date", AF-11.6) and
  `PlainText`.
- `ListOptions` with `OptionsTable`, `OptionKind` and `OptionMember` (docs/axTask/forms-framework.md section 12,
  docs/axJSON/SPEC.md AJ-5; 2026-11-21): the options of a list's configuration as one table: every kind of object
  it holds, each member with what it is, its first value and, where the value is one of a few, its values
  (`Table`; `ActionKeys`, `ActionKinds`, `FormFieldTypes`); `ForItem` makes an item's table from its list's
  configuration; `KindAt` finds the kind of an object by the way to it; `ToJson` writes the file
  `docs/axKit/axkit-list 2.options.json`, which a test holds current (run it once with `AXKIT_UPDATE_OPTIONS=1`
  after a change of the table). It is here, and not in axTask as first specified, because axJSON reads it and an
  app never references another; axTask's `ListActionsTests` holds the actions' side to its reader. A new member of
  the configuration or of an action joins the table in the same commit. `ListOptionsTests`.
- The lists' files (docs/axTask/forms-framework.md sections 11 and 13; 2026-11-21): `ListConfig`, a list's
  `list.json` as `axkit-list 2` (`details`, `item fields`, `actions`; `Parse` with an `InvalidDataException` that
  names the line, `Problems` for a member passed over, `FormatOf`, `New`, `WithDetail`, `WithItemFields`, which
  keeps the object of a field that stays, `WithExtra`, `IndexOf`); `FlatItem`, an item as its names and values with
  its link `"schema"` (`Parse`, `New`, `With`, placing a first value in the configuration's order, `Has`, `Text`);
  `ItemView` with `ShownItem` and `KeptValue`: an item read through a configuration (`Of`: hide, keep, restore, with
  `KeptLine`), as plain text when its configuration is not there (`Plain`), as the window's `Document`, and
  `Changed`, the file after a change in the window, which writes only what changed. Used by axTask and by the
  reports list.
- `Conversion/` (AF-13.6, a namespace apart): `EarlierFormat`, the only place that reads the shape before
  (`axkit-list 1`, `axkit-item 1`: `IsEarlierList`, `IsEarlierItem`, `Convert`, texts in and texts out, nothing
  dropped, what it has to say in `Notes`), and `ListConversion`, the folder's side (`IsEarlier`, `Plan`, `KeepCopy`,
  `Apply`). axTask asks before it converts; the report form's own list is converted when it is met.
- `FieldStep` (AX-8.10 rule 4, IA-5.6): Right and Left on a field: a choice's next or previous value, a check
  checked or unchecked, null (the edge sound) otherwise, never a `Protected` type.
- `Changes`, `Step` and `FileChange` (IA-5.4; from AxTask.Core on their second use, axTemplate's browser, 2026-11-23, moved unchanged): a change on disk as parts, writes and moves, `Apply`, `CheckUndo` (every path as the step left it, so that an edit made since is never lost), `Undo` (last part first; a new list's folder goes with its `list.json`), `ReadOrNull`. Tested where they came from, `tests/AxTask.Tests` (`FolderTests`, `LinksTests`).
- `ItemFormRules` with `FormControl` and `FormValue` (docs/axTask/forms-framework.md AF-14.1, axTask's TK-4.0a, 2026-11-21; from AxTask.Core on its second use, axTemplate's form, 2026-11-23): an item's form, pure:
  `Controls` (one control per field of the item's document: its kind, text, check, lines or list, its name as an
  instruction, what it starts with; a choice that holds none of its options starts on `NotChosen`; a value its type
  does not hold is a text box), `Apply` (what the controls' values make of the item: only a control that was changed
  gives its field a value, taken as `FieldTypes.Parse` takes it, a link's title through the caller; the first value
  refused, with its control), `Said` ("Saved Fix the tap. Due: …. Priority: High", or "Nothing changed"). Tested in
  `ItemFormRulesTests`.
- `TemplateStore` (docs/axTemplate/SPEC.md TP-2; from AxTemplate.Core on its second use, the editors' Insert template, 2026-11-23, moved unchanged): the templates' folder, `AppPaths.TemplatesRoot`, as a list of the item format. `Fields` and `AppsWithField` are a template's fields (its Text, then a check for every axKit app and one per app with a field); `ConfigurationToWrite` makes the folder's `list.json` or adds the fields a newer version brings, never taking one away; `Open` makes the folder ready and reads it; `Files` gives every item file for axTemplate's browser, `Read` the templates for its launcher and the editors; `Parse` one file; `After` the files after a change of the browser's own or its undo, without the disk (a move out of the folder leaves the list); `ArchiveFolder`, where Delete moves a template (TP-D9), and `MovesAcrossFolders`, a step whose undo reads the folder again; `NameRefused`, `NoTextYet`, `LeftKeys`. `Template.ShowsInApp` says whether a template shows in an app's palette. Disk reads: call them off the UI thread. Tested in `tests/AxTemplate.Tests`.
- `ItemFiles` (IA-4.5; from AxTask.Core's `ItemFolder` on its second use, the reports list, 2026-11-07): `FileStem`,
  the file name a Title gives, and `FreePath`, a free name beside the taken ones ("name - 2.json").
- `Reports/` (docs/axKit/reports.md, 2026-11-07): `Report` with `ReportKind`, `ReportKinds` (the names, the GitHub
  labels, `Parse`) and `ReportStatus` (Draft, Not sent yet with its reason, Sent, `AwaitsSending`); `ReportFiles`, the
  Reports list as an axTask folder in the lists' one shape (`ListConfiguration` and `ListText` with the fields of a
  report and the three actions of its kinds, `ToItem`, `FromItem`, `Save`, `Write`, `WithStatus`, `WithSent`,
  `Unsent`; `EnsureList` converts a list an earlier version made, a copy kept beside it, so that a report waiting
  in it is still sent, and replaces the Notes of a list made when reports were public, `ListNotesBefore`, by
  `ListNotes`); `ReportBody`: `Markdown`, the plain report with the reporter line
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
- `Reports/ReportWritten` (reports.md RP-6.9, repairs R-57, 2026-11-16): the names of the report files the form wrote, `AppPaths.ReportsWrittenFile` under `SettingsSeal`. `ReportFiles.Save` adds the name before it writes the item; `ReportFiles.UnsentOfTheForm` gives the waiting items on the list and the count of the others, which `ReportSender.SendUnsentAsync` neither sends nor marks (`SendOutcome.NotOfTheForm`, `NotSentSentence`). `Names` is null until the first save of this version ("not kept yet": everything waiting is sent, and that save takes the waiting names in); afterwards a missing or refused list is empty, by `SettingsSeal.WasSealed`. By name and by the id the form writes into each file (`ReportFiles.IdKey`, `IdOf`; an entry `IdMark` and the id), so a report axTask renamed with its title is still the form's; never by content. Tested in `ReportSenderTests`.
