# axCalc, the window

The WinForms side of axCalc: the history as a text, the expression field under it, a status bar, the bundle's notices. Since 2026-11-22 the window is the bundle's conversation window (`AxKit.Forms/ConversationForm`, `src/AxKit.Forms/notes/console.md`; `docs/axKit/framework.md` step 9), the shape axClaude and axTerminal have without a program behind it; until then it was a window of its own with the history as a list. The pure code (the evaluator, the answer's text and bases, the history entry, the settings) is `src/AxCalc.Core`, with its own `CLAUDE.md`. The spec is `docs/axCalc/SPEC.md` ("SPEC.md" below); the guide `docs/axCalc/user-guide.md` is embedded by `AxCalc.csproj` for Help → User guide; the manual test plan is `docs/axCalc/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
& '.\scripts to run-individual-apps\run-axCalc.ps1'                  # build AxKit and start axCalc (run.ps1 -App calc)
.\test.ps1 AxCalc                 # the tests in Release, as CI runs them
```

## Where things are

- `AxCalcApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run`; axCalc takes no arguments, and any is reported with the usage text.
- `CalcForm`: the window (SPEC.md AC-2) on `AxKit.Forms/ConversationForm`: the history is its `Transcript`, the expression field its `Input`, and the keys between them, find, the bookmarks and the Context menu on a line are the conversation window's. The menus (AC-7.1; Navigation with `AddFindItems` and `AddBookmarkItems`, Context from `CreateTranscriptContext`); the menus and keys as one command list (docs/axKit/app-kit.md AK-2) from `BundleKeys` and `AxCalcKeys`: Enter in the field is the Answer command and Escape there clears it, each with a `When` on the field; Enter and Escape in the history are the view's own (`Transcript.LineChosen` calls `Recall`; Escape goes back to the field). `Evaluate` (one `Calculator.Evaluate`, the entry added and its line through `CalcTranscript.AddAnswer`, the field emptied, the answer announced; an error announced with the field kept); `EntryOf` (the entry a line is, by its words: the newest of several that read the same) and `CurrentValue` (the answer of the line you are on while the history has the focus, through `TranscriptView.CaretLineText`; the last answer otherwise), which Ctrl+Shift+C, Ctrl+I and Copy answer in act on; the history kept in `_entries`, with a line of `CalcTranscript` for each, the two changed together; `UpdateStatus` for the status bar; the settings written on closing through `CalcSettings.Keep`, and at once after a clear. The announcements are on the history, as in every conversation window.
- `AxCalcKeys`: the window's keys (SPEC.md AC-7.2; bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys; `Transcript` is the history's keys for the view, the same letters as axTerminal's and axClaude's.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; nothing speaks on an arrow key; an answer is one announcement, interrupting.
2. The arrow keys in the field are the edit control's own and never pull a history line into it (SPEC.md AC-D5); the history and Enter are the way back.
3. No second window (bundle AX-7.2): questions are notices; the save dialog is the system's own.
4. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan.

## Gotchas

- Enter in the field reaches `ProcessCmdKey` as `Keys.Return` and is the Answer command, which runs while the field has the focus; the field is the conversation window's, of several lines, so Shift+Enter is a new line in it. Enter in the history is the view's key, not a command of the list.
- A line of the history is told to be an entry by its words alone (`EntryOf`): a system line or a bookmark matches none. Two entries that read the same hold the same expression and answer, so the newest serves for both.
