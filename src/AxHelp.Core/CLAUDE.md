# AxHelp.Core, the pure code

Everything of axHelp that needs no window: the exchanges kept and the matches of the list. Unit tested in `tests/AxHelp.Tests`. The spec is `docs/axHelp/SPEC.md` ("SPEC.md" below); the window is `src/AxHelp`, with its own `CLAUDE.md`. AI help's own pure code (the settings, the request, the answer, the prompts) is the bundle's, `src/AxKit.Core/Ai`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxHelp
```

## Where things are

- `HelpSettings` with `HelpExchange` (SPEC.md HE-3.1, HE-5): on the bundle's `HotkeySettings` (the hotkey, Windows+Alt+H by default, Start with Windows, Say the first match); `Exchanges`, the last ten questions with their answers and sessions, newest first; `Remember` moves a question asked again to the front.
- `HelpLines` (SPEC.md HE-3.2, HE-3.3): `Matches` (every typed word in the question, through the bundle's `TextMatch`), `Spoken`, the list's one line for nothing kept and for no match.

## Gotchas

- A `HelpExchange` is a positional record: System.Text.Json fills it through its constructor, so the property names stay as they are (question, answer, sessionId, when in the file).
