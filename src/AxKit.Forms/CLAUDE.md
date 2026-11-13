# AxKit.Forms, the shared window pieces

WinForms code every app of the bundle uses (`docs/axKit/SPEC.md` AX-6.2, AX-6.4). It has no window of its own and few unit tests (`TextJump`, `AppMessage`'s text form and `BundleKeys.Text` in `tests/AxKit.Tests`, with `ListTests`, `MenuTests`, `CheckBoxTests` and `KeyTests`, which read the sources and the key tables); the apps check it with NVDA. Nothing here references an app (M2); a piece arrived here on its second use, unchanged apart from its namespace (M3), so axClaude's behaviour stayed the same.

## Where the notes are

The notes of each piece are in `src/AxKit.Forms/notes`, one file per kind of window, so that a change to one kind is made with that file alone in hand. Read the file of the piece you change before changing it, and keep its bullet current in the same commit (`NotesTests` fails on a name the sources no longer have).

- `notes/window-base.md`, the window base, its notices, menus and palette: `AxKitForm` (its partial files, the command list, `After`, the menus every window has), `ReportControls`, `OverlayPanel` with `Notice`, `OverlayChoice` and `OverlayInput`, `CommandPalette`, `RecentMenu`, `ThemePainter` with `ThemeMark`, the lists of apps, `WindowMenu`, `AppMessage` with `HandOver`, `AppHost`.
- `notes/controls.md`, the shared controls, keys and sounds: `Sounds`, `EditKeys.Page`, `EditKeys.Paragraph`, `EditKeys`, `AxKitTextBox`, `WindowsOpenWith`, `User32`, `Announcer`, `AxKitList`, `AxKitCheckBox`, `ChoiceItems<T>`, `PrivateClipboard`, `WindowAccessibleObject`, `StatusLayout`, `BundleKeys`.
- `notes/console.md`, the console window (axClaude, axTerminal): `ConsoleForm`, `TranscriptView` with `TranscriptKeys` and `QuickKey`, `ControlArea` with `ListKeys`.
- `notes/editor.md`, the text editor shell (axDown, axJSON, axPreview): `MarkdownKeys`, `TextJump`, `TextEditorForm`, `EditorApp`, `EditorKeys`, `EditorHost`.
- `notes/item.md`, the item window (axTask, axPass, axMCP, axAutomate) and the list views: `ContextBarMenu`, `ListViewState`, `ItemForm`, `ItemKeys`, `ItemRow`, `ItemDetail`, `ItemDetailLine`.
- `notes/hotkey.md`, the hotkey window (axStart, axClip, axHelp): `HotkeyForm`.
- `notes/ai-and-automation.md`, AI help, the AI tools and the automation channel: `AutomationChannel`, AI help (`AxKitForm.Ai.cs`), the AI tools (`McpChannel`, `McpBroker`, `AxKitForm.Mcp.cs`), `ThisProgram`, `UserPipe`, `WindowReading`.

## Rules

- Nothing here references an app (M2); a piece arrives here on its second use, unchanged apart from its namespace (M3).
- A menu item states what it does where it is made (`CommandPalette.Marked`, or a `Command` with its `Mark`; docs/axKit/app-kit.md AK-2.5); a submenu that fills itself as it opens is `RefreshedOnOpening` or `NoEntry` (`notes/window-base.md`).
- A one-shot timer is the base's `After`; a Windows dialog goes through `ShowSystemDialog`; a question, an error or a help text is a notice (`ShowNotice`), never a second window.

Gotchas that live here are axClaude's and are kept in `src/AxClaude/CLAUDE.md` (the EDIT control, NVDA's reading after a key press, the notice state as a flag in the form, `AcceptButton` for Enter, Alt and F10 as `SC_KEYMENU`).
