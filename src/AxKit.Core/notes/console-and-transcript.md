# AxKit.Core notes: The console and the transcript

From AxClaude.Core on their second use, axTerminal (2026-10-18; docs/axTerminal/SPEC.md §5, AT-D6); the gotchas are in
`src/AxClaude.Core/CLAUDE.md`.

- `Pty/PtyHost`: ConPTY through P/Invoke, the one implementation; `tools/PtyCapture` records through it.
  `Pty/StreamRecorder`: the raw stream to a `.vt` file plus chunk index.
- `Vt/VtParser`: byte stream to print, control, CSI and OSC, written into the `Screen` it is given. `Vt/Wcwidth`.
- `Vt/Screen`: cells, cursor, scroll region; rows carry their transcript `Line`, found again by identity with `RowOf`
  and made ahead of text with `EnsureLine`; `PullRowText` at a frame's end; `DetachRows` commits every row's line and
  blanks the rows; events for the frame end, the bell, the title, the cursor homed, the screen erased or reset.
  Since 2026-11-25 (docs/axTerminal/SPEC.md AT-9.8) the alternate screen (`?1049`, `?1047`, `?47`) is a second set
  of rows with no lines: `Alternate`, `AlternateChangedSinceFrameEnd` (only with `KeepsAlternateApart`, which axTerminal's model turns on for its screen view, AT-9.9, and off by default, so that a full-screen program is never hidden where nothing shows the page); while it is in use nothing reaches the
  store and the homed and erased events stay quiet, and every public member that names a row (`CursorRow`,
  `LineAt`, `RowOf`, `EnsureLine`, `PullRowText`, `DetachRows`) is the main screen's. SGR is read for the background
  alone (`SelectGraphicRendition`, in place, no allocation), kept per cell in `Row.Backgrounds`, which only the
  alternate screen's rows have, so that the conversation's path pays nothing but a null check and a reference per
  row (measured 2026-11-25 on three Claude fixtures: the same allocation per frame, eight bytes more per row made
  by a scroll, and the time within the noise). An erase takes the background in force. AT-9.10 (2026-11-25):
  `LeaveAlternate` is the switch back as `?1049l` makes it, for a model that knows the program ended on the
  alternate screen (axTerminal's, at the shell's own mark); with `AnswersQueries` on (axTerminal's model alone) an
  OSC colour question on the alternate screen raises `Answer` with each of `QueryAnswers`' answers, which the window
  writes to the program's input; never on the main screen, where a shell's line editor would take it as typed text.
- `Vt/QueryAnswers` (AT-9.10): the answers to OSC 4 (`n;?`), OSC 10 and OSC 11 questions, a plain black background
  and light text (Windows' console palette with its black plain black, the numbers 16 to 255 as xterm's), in
  xterm's form ended by BEL. A device attributes or status question never reaches a window (the console answers it
  itself), so none is answered.
- `Vt/ScreenPage` with `MarkedRun` (AT-9.2, AT-9.4): the alternate screen read whole at one moment, null on the main
  one: the rows' texts (trailing spaces out, every character as drawn, box-drawing and blocks included since the maintainer's decision of 2026-11-25), the cursor, the marked runs (cells of one
  background between cells of one other that is not the page's own, or reverse video between cells that are not;
  never at a row's end). It allocates; it is read only while a full-screen program runs.
- `Transcript/Line` and `LineKind`: one logical line with its kind, heading level, join flag and hidden flags; the
  Claude-specific kinds and the echo, replay and repaint flags stay here because the class moved whole.
- `Transcript/LineStore`: the transcript of a console: the lines and what a screen does to them, create, commit,
  remove, the text of a rewritten row; since 2026-10-23 also the UTF-8 decoder with `Feed`, the parser and the screen
  made by `NewScreen`, to which each model attaches its own handlers, `EndFrame` over the abstract `CompleteFrame`,
  `ToggleBookmark`, `VisibleBefore`, `Trim`, `ExchangeCount`, the `Changed` and `Bell` events with `RaiseChanged`
  and, for a model with no screen to ring it, `RaiseBell` (2026-11-17; axClaude's model of Claude Code's stream);
  each app's model derives from it: axClaude's `SessionModel`, axTerminal's `TerminalModel`.
- `Transcript/TranscriptMirror`: visible lines to edit-control edits, the caret's place afterwards, display line
  numbers, the find search, the reading breaks; `ShowMarkers` off shows heading lines as `Line.BareText`, without the
  `#`, the export unchanged.
- `Transcript/Utterance`: how spoken lines join into one notification.

- The review of 2026-11-27: `Screen.LeaveAlternate` also lets go of the dead program's scroll region, background and reverse video. A reset (ESC c) raises `Cleared` before it leaves the alternate screen, with `AlternateChangedSinceFrameEnd` held false for the event, so that a model that keeps the rows on their lines after a switch (axTerminal's `LetRowsGo`) still lets them go and the erase blanks no line already read.
