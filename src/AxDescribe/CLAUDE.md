# axDescribe, the window

The WinForms side of axDescribe: the window in front described by Claude, on the bundle's background window (`AxKit.Forms/HotkeyForm`, as axStart and axClip), the AI help itself being the window base's (`AxKit.Forms/AxKitForm.Ai.cs`, `docs/axkit/ai-help.md`). The pure code is `src/AxDescribe.Core`, with its own `CLAUDE.md`. The spec is `docs/axdescribe/SPEC.md` ("SPEC.md" below); the guide `docs/axdescribe/user-guide.md` is embedded by `AxDescribe.csproj` for Help, User guide; the manual test plan is `docs/axdescribe/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axdescribe.ps1              # build axKit and start axDescribe (run.ps1 -App describe; a start describes what is in front)
.\test.ps1 AxDescribe             # the tests in Release, as CI runs them
.\nvda-pass.ps1 axDescribe        # tests\scripts\axdescribe with NVDA listening, against the fake Claude
```

## Where things are

- `AxDescribeApp`: the entry point (bundle AX-1.7; SPEC.md DE-1): the mutex `Local\AxKit.AxDescribe`, `--hidden`, the hand-over of a second start, as `AxStartApp`.
- `DescribeForm` (SPEC.md DE-2, DE-3): on `HotkeyForm`; `PrepareShowing` takes the picture of `ForegroundBefore` before the window shows (`TakePicture`, `ScreenPicture.Capture` into `PicturePath`) and keeps the program and title for `AfterShowing`, which asks through `AskClaude` with `AiPrompts.Screen` and the picture's folder as the call's folder; `Described` remembers and shows; `Ask` (Enter: a follow-up in the selected description's session, or the description again); `OpenPicture` (Ctrl+O, axPreview); `ForgetDescriptions`; `OnFormClosed` deletes the picture.
- `ScreenPicture` (DE-2.1, DE-D1): `GetWindowRect` and `Graphics.CopyFromScreen` into a PNG; the whole screen when the window gives no rectangle.
- `AxDescribeKeys` (SPEC.md DE-6): the window's keys; `HelpText`: the F1 text and the embedded guide.

## Rules

1. Nothing of AI help here (as axHelp's rule 1).
2. The picture is taken only in `PrepareShowing`, while the window in front is still in front; a later capture would show axDescribe's own window.
3. Every control gets an `AccessibleName`; no second window (AX-7.2).
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- `ShowWindowInFront` runs `PrepareShowing` for every showing, a second start's hand-over too: each takes a new picture and asks again, which is what "describe the window in front" means for a start; `keepText` (a start that failed, HotkeyForm) and an open notice skip it.
- On the automation's test desktop nothing is in front: `GetWindowRect` fails or gives the desktop, and the whole screen is captured; the script's fake answer does not depend on the picture.
