# axHelp, the window

The WinForms side of axHelp: two hotkeys into one conversation with Claude, on the bundle's background window (`AxKit.Forms/HotkeyForm`, as axStart and axClip). The conversation itself is an axClaude window, started by this program file with axClaude's verb; nothing of the chat is here. The pure code is `src/AxHelp.Core`, with its own `CLAUDE.md`. The spec is `docs/axHelp/SPEC.md` ("SPEC.md" below); the guide `docs/axHelp/user-guide.md` is embedded by `AxHelp.csproj` for Help, User guide; the manual test plan is `docs/axHelp/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`. axDescribe merged into axHelp in 5.7.0 (SPEC.md HE-D2).

## Commands

```
& '.\scripts to run-individual-apps\run-axHelp.ps1'                  # build axKit and start axHelp, its window in front (run.ps1 -App help)
.\test.ps1 AxHelp                 # the tests in Release, as CI runs them
.\nvda-pass.ps1 axHelp            # tests\scripts\axHelp with NVDA listening
```

## Where things are

- `AxHelpApp`: the entry point (bundle AX-1.7; SPEC.md HE-1): `--hidden` or one picture file's path (refused before any window when it is not a picture, `PictureFile.Refusal`), the mutex (`AppPaths.OneAtATime`), the hand-over of a second start ("show", or "open" with the picture) to the window found by its title `WindowTitle`, "axHelp hotkeys", never "axHelp", the conversation's. `AxKit.exe describe` reaches here too (`Dispatch.FormerDescribe`). The mutex and the hand-over are one call, `AppHost.OneCopy`, which asks about a running axHelp that does not answer and ends only processes started with the verb `help` (bundle AX-1.9), so never the conversation's.
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
- axKit's guides in the working folder (SPEC.md HE-2.6) are written by the window base's `AiWorkingFolder`, which `OpenConversation` and `TakeScreenshot` call first; `PrepareWorkingFolder` writes only the two files that are the user's. Anything new axHelp puts into the working folder gets a subfolder, as `screenshots` has.
- A model chosen in Options counts from the conversation's next start: `--model` is a start argument of Claude Code. Closing the conversation only hides it (SPEC.md HE-1.3), so the next start comes after its Session, End Claude and close this window, or after Quit axHelp.
- The conversation hides on closing and may be hidden from axHelp's start (SPEC.md HE-1.3, HE-2.1, HE-2.7; axClaude's `--hide-on-close` and `--hidden`). `FindConversation` is `HotkeyForm.FindRunning`, which finds hidden windows; never go back to `WindowMenu.OpenWindows` for it. `OpenConversation` hands the window "show" or the "message" and the window brings itself up; `OnFormClosing` hands it "end" when axHelp really ends. `StartConversationHidden` runs only for a start with `--hidden` and only by `HelpSettings.StartsConversationHidden`: never start Claude Code before the user's agreement is in the consent file.
- No script starts axHelp hidden (a hidden hotkey window has no automation channel), so `StartConversationHidden`'s wiring is checked by hand: the setting Keep the conversation ready in the background on (it is off at first, SPEC.md HE-D9, and stays so: no prompt or reminder about it), `run.ps1`, then the log line "The conversation started hidden".

- Before a start, `OpenConversation` asks `AiFolderCheck.NewOrChanged` (SPEC.md HE-2.9): with a file Claude Code would obey that axKit did not write and the user has not accepted, `AskAboutFiles` shows this window with the notice, Cancel its default; `StartConversationHidden` never starts in such a folder and logs "not started ahead". No script covers it: starting the conversation needs Claude Code. Checked by hand: put a `.mcp.json` into the working folder, press the hotkey.
