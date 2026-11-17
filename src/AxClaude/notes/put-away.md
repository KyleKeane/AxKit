# axClaude's window: a window that hides on closing

Notes of `MainForm.PutAway.cs`. The index and the rest of the window's notes are `src/AxClaude/CLAUDE.md`.

## A window that hides on closing (SPEC.md FR-9.6, 2026-11-15)

`MainForm.PutAway.cs`, for axHelp's conversation; axClaude knows only the options `--hide-on-close` and `--hidden` and the hand-overs "show", "message" and "end".

- `OnFormClosing` there runs before the console window's: with `--hide-on-close` a user's close calls `PutAwayNow` (hide, `EventDoor.Muted`, reply speech dropped, settings saved, the foreground given back through `ForegroundReturn`) and returns without the base, so the closing question never shows for a hide. A real close is `_ending` (`EndForGood`, the "end" hand-over), `CloseWasConfirmed` (Close anyway), an update's or Windows' own.
- `BringUp` is the one way back: `Show()` of the same form, `WindowMenu.Activate`, the focus to `BottomControl` unless a notice shows. Never rebuild or resync anything there: the conversation view went on taking Claude's lines while hidden.
- While `_putAway`, the attention timer's tick does not call `OnFrameSettled`; it sets `_attentionWaits`, and `BringUp` restarts the timer, so that a question (the trust question of a hidden start) is asked as one that has just arrived. `PutAway` makes the window base drop every announcement meanwhile.
- A hidden start is shown once and hidden in `OnShown`, not kept from ever showing: a form never shown has no child controls made, and the view would fill only at the first showing. It shows with `ShowWithoutActivation`, opacity 0 and at normal size (WinForms shows a maximized form with a call that activates it); the saved state is put back at the first `BringUp`.
- The foreground was not checked on the user's own desktop by a script (the test desktop has none): the user's ear owes it (nvda-test-plan section 20).
- The "end" hand-over is a request (`EndRequested`): it closes at once only with nothing to lose; otherwise `BringUp` and FR-1.7's question, or "Close this conversation?" for a typed message or an unanswered offer, Keep it open the default. A window kept after it sets `_keeperLeft` and closes from then on as any window; "show" and "message" clear it. Never make "end" a forced close again (it cut Claude mid-turn and dropped a draft, the review of 2026-11-15).
- What happens while put away is kept for `BringUp`, in its order: `ShowNoticeRaisedWhilePutAway` (the base shows the notice again), `_stoppedWhilePutAway` (from `SaidWhilePutAway`), the settle, then `ShowOfferWhenIdle`; `OfferMayShow` is false while `_putAway`, since an offer shown unheard would lose its caution (bundle rule 11). A new thing the window says by itself while hidden needs its place in that order.
- `SavesWindowPlace` is false until a hidden start was brought up once: its forced normal size must never reach the settings file. `DoneChimeWhilePutAway` is the one switch for a Done chime in a hidden window.
- A Done the core says while the window is put away is owed (`_doneOwed`, set by `FlowDone` in `MainForm.Flow.cs`, with `_attentionWaits`): one for however many turns ended meanwhile, said by the settle that `BringUp` restarts (`DoneBySettle`), unless a question waits then, which takes its place as it took a bell's. With the core in shadow the bell and `_bellPending` keep it, as before.
