# axHelp, the window

The WinForms side of axHelp: a question to Claude from anywhere, on the bundle's background window (`AxKit.Forms/HotkeyForm`, as axStart and axClip), the AI help itself being the window base's (`AxKit.Forms/AxKitForm.Ai.cs`, `docs/axKit/ai-help.md`). The pure code is `src/AxHelp.Core`, with its own `CLAUDE.md`. The spec is `docs/axHelp/SPEC.md` ("SPEC.md" below); the guide `docs/axHelp/user-guide.md` is embedded by `AxHelp.csproj` for Help, User guide; the manual test plan is `docs/axHelp/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axHelp.ps1                  # build axKit and start axHelp in front (run.ps1 -App help; a running axHelp is ended and started again hidden)
.\test.ps1 AxHelp                 # the tests in Release, as CI runs them
.\nvda-pass.ps1 axHelp            # tests\scripts\axHelp with NVDA listening, against the fake Claude
```

## Where things are

- `AxHelpApp`: the entry point (bundle AX-1.7; SPEC.md HE-1): the mutex `Local\AxKit.AxHelp`, `--hidden`, the hand-over of a second start, as `AxStartApp`.
- `HelpForm` (SPEC.md HE-2, HE-3): on `HotkeyForm`; the field "Ask Claude", the list of earlier questions as the matches, the status bar; `Ask` (Enter: the typed question through `AskClaude` with `AiPrompts.Anywhere` and the program in front from `WindowOwner.Describe(ForegroundBefore)`, or the selected exchange's answer again through `ShowAnswer` with its session), `Answered` (the exchange remembered and saved, the list refilled), `ForgetQuestions`. Every other behaviour (the hotkey, showing and hiding, the foreground rule, the match keys, the notices, AI help's checks and notices) is the two bases'.
- `AxHelpKeys` (SPEC.md HE-6): the window's keys, the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Nothing of AI help here: the switch, the folders, the program, the consent, the request, the waiting and answer notices are `AxKitForm.Ai.cs`; a change to how Claude is asked goes there, for every window at once.
2. Every control gets an `AccessibleName`; nothing speaks on an arrow key but the match said (AS-3.3).
3. No second window (bundle AX-7.2): questions are notices.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- The window stays while a notice is open even when another program comes to the front (`HotkeyForm.HideIfAnotherProgramIsInFront`), so a long "Asking Claude" keeps the window on top until the answer or Cancel; by design, the question is in progress.
- `HelpSettings.Remember` is called before `SaveSettings()`: the base saves the settings object it was given, which is the same `HelpSettings`.
