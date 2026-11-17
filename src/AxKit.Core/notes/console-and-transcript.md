# AxKit.Core notes: The console and the transcript

From AxClaude.Core on their second use, axTerminal (2026-10-18; docs/axTerminal/SPEC.md §5, AT-D6); the gotchas are in
`src/AxClaude.Core/CLAUDE.md`.

- `Pty/PtyHost`: ConPTY through P/Invoke, the one implementation; `tools/PtyCapture` records through it.
  `Pty/StreamRecorder`: the raw stream to a `.vt` file plus chunk index.
- `Vt/VtParser`: byte stream to print, control, CSI and OSC, written into the `Screen` it is given. `Vt/Wcwidth`.
- `Vt/Screen`: cells, cursor, scroll region; rows carry their transcript `Line`, found again by identity with `RowOf`
  and made ahead of text with `EnsureLine`; `PullRowText` at a frame's end; `DetachRows` commits every row's line and
  blanks the rows; events for the frame end, the bell, the title, the cursor homed, the screen erased or reset.
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
