# AxKit.Core, the shared pure code

Code every app may use and no window needs (`docs/axKit/SPEC.md` AX-6.1). One bullet per piece; where a piece came from
another project on its second use, the bullet says so.

## Where the notes are

The notes of each piece are in `src/AxKit.Core/notes`, one file per kind (split on 2026-11-15, when this file outgrew NotesTests' limit), so that a change to one kind is made with that file alone in hand. Read the file of the piece you change before changing it, and keep its bullet current in the same commit.

- `notes/bundle.md`, the bundle: Dispatch, PackageIdentity, the Store, AppPaths, Ai, AiCaution, Consent, Theme, the settings files, Log, Updates, Events (the kinds of event and the table that gives each its sound), Audio, BundleAbout, BundleHelp, RecentActions.
- `notes/paths-windows-lists.md`, paths, windows and lists: LocalPaths, FileSize, PathName, RecentPaths, RecentlyClosed, AxKitWindows, MenuText, Palette, TextMatch, TypeAhead, NaturalOrder, Marks, ListArrange, Accents, KeyChord, CommandListing.
- `notes/safety.md`, safety: SystemPrograms, ProgramInstall, AutomationEnvironment, ProcessRun, DisplayText, Signatures, Redaction, SettingsSeal, ThisProgram, ChildJob.
- `notes/ai-tools.md`, the AI tools: McpAccess, McpPipeName, McpProtocol, ClaudeCodeProgram, McpClaudeCode, McpTools, McpOffer, CommandNames, CommandGate, McpRun.
- `notes/background-apps.md`, background apps: SignInStart, Hotkey.
- `notes/text-and-editors.md`, text and the editors: TypedLineBreak, Paragraphs, TextLines, MarkdownOutline, MarkdownEdit, TextDocument, EditorSettings, EditHistory, the item format, FieldStep, ItemFiles, Reports, AxKitHttp, MediaFiles.
- `notes/console-and-transcript.md`, the console and the transcript: PtyHost, VtParser, Screen, Line, LineStore, TranscriptMirror, Utterance.

## Tests

Every piece is tested in `tests/AxKit.Tests`, one `<Piece>Tests.cs` file per piece (`VtScreenTests`,
`VtScreenOperationTests` and `ScreenMarkTests` for the screen); the transcript pieces are tested further in
`tests/AxClaude.Tests`, where the recorded fixtures drive them. The same project holds the bundle's own checks of the
sources: `LayoutTests` (the project references point one way, M2), `InventoryTests` (every app in every place it must
be), `KeyTests` (no window key equals a bundle key, AX-8.4), `ListTests`, `MenuTests`, `CheckBoxTests`, `CommandMarkTests` (every `new Command` states its `Mark`, and the act commands that start a program or reach past the window stay act, docs/axKit/mcp.md MC-D7) and `ReleaseWorkflowTests` (the workflows' shape).

## Gotchas

- `TextDocument`: the Windows code pages come from `CodePagesEncodingProvider`, registered in its static constructor;
  the system's ANSI page is asked with `GetACP`, since the executable runs with invariant globalization and the
  culture cannot tell; an answer of 65001 falls back to 1252, since the bytes were just found not to be UTF-8; the
  encodings are constructed with their marks so that `GetPreamble` knows them; `GetBytes` never writes a mark,
  `ToBytes` adds it when the file had one.

## Rules

Nothing here references an app (M2); a piece arrives here only when a second app needs it, unchanged where possible
(M3); plain static classes and records, no frameworks (B-10). An app's settings class stays in the app; only the file
handling is shared.
