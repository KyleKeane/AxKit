# AxKit.Forms, the shared window pieces

WinForms code every app of the bundle uses (`docs/axKit/SPEC.md` AX-6.2, AX-6.4). It has no window of its own and few unit tests (`TextJump`, `AppMessage`'s text form and `BundleKeys.Text` in `tests/AxKit.Tests`, with `ListTests`, `MenuTests`, `CheckBoxTests` and `KeyTests`, which read the sources and the key tables); the apps check it with NVDA. Nothing here references an app (M2); a piece arrived here on its second use, unchanged apart from its namespace (M3), so axClaude's behaviour stayed the same.

## Where the notes are

The notes of each piece are in `src/AxKit.Forms/notes`, one file per kind of window, so that a change to one kind is made with that file alone in hand. Read the file of the piece you change before changing it, and keep its bullet current in the same commit (`NotesTests` fails on a name the sources no longer have).

- `notes/window-base.md`, the window base: `AxKitForm` (its partial files, the command list, `After`, the menus every window has), the services it holds (`UpdateFlow`, `ProgramInstaller`, `ReportFlow`), `ThemePainter` with `ThemeMark`, `AppHost`.
- `notes/notices.md`, the notices, the forms and the command palette: `FormControls` with `ShowForm` (every form, AX-8.10) and `ReportControls`, `OverlayPanel` with `Notice`, `OverlayChoice` and `OverlayInput`, `CommandPalette`.
- `notes/menus-and-windows.md`, the Window menu, the lists of apps and the hand-over between windows: `RecentMenu`, the lists of apps, `WindowMenu`, `AppMessage` with `HandOver`.
- `notes/controls.md`, the shared controls, keys and sounds: `EventDoor` (the one place that plays a sound: raise the kind of event), `Sounds`, `EditKeys.Page`, `EditKeys.Paragraph`, `EditKeys`, `AxKitTextBox`, `WindowsOpenWith`, `User32`, `Announcer`, `ListSelection` with `SelectionKeys` (selecting several rows of a list, the same in every window), `AxKitList`, `AxKitCheckBox`, `ChoiceItems<T>`, `PrivateClipboard`, `WindowAccessibleObject`, `StatusLayout`, `BundleKeys`.
- `notes/console.md`, the conversation window and the console window over it (axClaude, axTerminal): `ConversationForm`, `ConsoleForm` with `ConsoleKeys` and `ConsoleQuestion`, `TranscriptView` with `TranscriptKeys` and `QuickKey`, `ControlArea` with `ListKeys`.
- `notes/editor.md`, the text editor shell (axDown, axJSON, axPreview): `MarkdownKeys`, `TextJump`, `TextEditorForm`, `EditorApp`, `EditorKeys`, `EditorHost`.
- `notes/item.md`, the item window, the bundle's navigator (axTask, axPass, axMCP, axAutomate, axMedia, axProcess, axGIT, axFile, and axTemplate's browser, `BrowserForm`): `ContextBarMenu`, `ItemForm`, `ItemKeys`, `ItemRow`, `ItemDetail`, `ItemDetailLine`.
- `notes/hotkey.md`, the hotkey window (axStart, axClip, axHelp, and axTemplate's launcher, `LauncherForm`): `HotkeyForm`, `KeySender`.
- `notes/ai-and-automation.md`, AI help, the AI tools and the automation channel: `AutomationChannel`, AI help (`AxKitForm.Ai.cs`, `AiHelpFlow`), the AI tools (`McpChannel`, `McpBroker`, `AxKitForm.Mcp.cs`, `AiToolsSetup`), `UserPipe`, `WindowReading`.
- `notes/gotchas.md`, what the Windows controls and NVDA taught every window: the EDIT control's scrolling and its paragraph keys, the status strip, the sounds' devices, a burst of notifications, `AcceptButton`, the system keys.

## Rules

- Nothing here references an app (M2); a piece arrives here on its second use, unchanged apart from its namespace (M3).
- A menu item states what it does where it is made (`CommandPalette.Marked`, or a `Command` with its `Mark`; docs/axKit/app-kit.md AK-2.5); a submenu that fills itself as it opens is `RefreshedOnOpening` or `NoEntry` (`notes/notices.md`).
- A sound is never played from a window or a control: raise the kind of event at `EventDoor` (`notes/controls.md`; `EventDoorTests`).
- A one-shot timer is the base's `After`; a Windows dialog goes through `ShowSystemDialog`; a question, an error or a help text is a notice (`ShowNotice`), never a second window.
- What appears by itself holds its answering keys for a moment, each control with a hold of its own that is never shortened; a repeat of Enter, Escape or Space never answers; a press the hold took is heard as `Refused` (docs/axKit/SPEC.md AX-6.4; `AxKit.Core/KeyHold`, `AnswerKeys`; `notes/console.md`, "The hold"). The hold counts again from when the focus arrives, and it is asked once more where the answer is taken (`ControlArea.Takes`, `OverlayPanel.Choose`), so that a key path nobody thought of answers nothing. Every call that puts a control into a `ControlArea` writes `hold: true`, never a value worked out, and `hold: false` only where `ControlAreaHoldTests` lists the caller; a notice that a program, a job or a timer shows is written `.ByItself()`, the only way to make one hold. Every question is to start on its confirming answer, so these rules are the one thing between a doubled or held Enter and a yes nobody heard: add no way around them.

The gotchas of the controls and NVDA that hold for every window are `notes/gotchas.md` (the EDIT control, the status strip, the sounds, a burst of notifications, `AcceptButton` for Enter); those that are axClaude's own stay in `src/AxClaude/CLAUDE.md` (NVDA's reading after a key press, the mirror, the control area's questions).
