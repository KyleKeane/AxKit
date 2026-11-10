# axHelp, the window

The WinForms side of axHelp: two hotkeys into one conversation with Claude, on the bundle's background window (`AxKit.Forms/HotkeyForm`, as axStart and axClip). The conversation itself is an axClaude window, started by this program file with axClaude's verb; nothing of the chat is here. The pure code is `src/AxHelp.Core`, with its own `CLAUDE.md`. The spec is `docs/axHelp/SPEC.md` ("SPEC.md" below); the guide `docs/axHelp/user-guide.md` is embedded by `AxHelp.csproj` for Help, User guide; the manual test plan is `docs/axHelp/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`. axDescribe merged into axHelp in 5.7.0 (SPEC.md HE-D2).

## Commands

```
.\run-axHelp.ps1                  # build axKit and start axHelp, its window in front (run.ps1 -App help)
.\test.ps1 AxHelp                 # the tests in Release, as CI runs them
.\nvda-pass.ps1 axHelp            # tests\scripts\axHelp with NVDA listening
```

## Where things are

- `AxHelpApp`: the entry point (bundle AX-1.7; SPEC.md HE-1): `--hidden` or one picture file's path (refused before any window when it is not a picture, `PictureFile.Refusal`), the mutex (`AppPaths.OneAtATime`), the hand-over of a second start ("show", or "open" with the picture) to the window found by its title `WindowTitle`, "axHelp hotkeys", never "axHelp", the conversation's. `AxKit.exe describe` reaches here too (`Dispatch.FormerDescribe`).
- `HelpForm` (SPEC.md HE-2, HE-3): on `HotkeyForm`; one read-only text (`HelpText.Window`) and the menus. `OnHotkey` (the conversation's hotkey: `OpenConversation`, never this window), `SecondHotkey` and `OnSecondHotkey` (the screenshot: `TakeScreenshot` of `GetForegroundWindow()` at that moment), `OpenConversation` (the open window titled `ConversationTitle` to the front, with a message through `AppMessage.Message`; else `PrepareWorkingFolder` and `WindowMenu.StartWith` axClaude with `HelpSettings.ConversationArguments`), `PutPicture`, `ChoosePicture` (Ctrl+D), `OpenLastScreenshot` (Ctrl+O), Options, Conversation model (`ChoiceItems`). `OnHandleCreated` removes axDescribe's old Run value. The match members `HotkeyForm` asks for are empty: there is no list.
- `ScreenPicture` (HE-3.1, from axDescribe): `GetWindowRect` and `Graphics.CopyFromScreen` into a PNG; the whole screen when the window gives no rectangle.
- `AxHelpKeys` (SPEC.md HE-6): the window's keys; `HelpText`: the window's text, the F1 text and the embedded guide.

## Rules

1. The conversation is axClaude's: a change to how it reads, speaks or asks goes to axClaude, for every conversation at once. axHelp adds the hotkeys, the folder, the model and the screenshots.
2. Every control gets an `AccessibleName`; no second window (AX-7.2).
3. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- Two titles: the conversation is "axHelp" (axClaude's `--title`), axHelp's own window "axHelp hotkeys". `HotkeyForm.FindRunning` finds a window by its title; with one title a second start would hand "show" to axClaude.
- The screenshot is taken in `OnSecondHotkey`, while the window in front is still in front; axHelp's own window is not shown for it.
- A model chosen in Options counts from the conversation's next start: `--model` is a start argument of Claude Code.

