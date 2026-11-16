# Session file lines, written by hand

The files here are lines in the shape of Claude Code's session file (`<configuration folder>\projects\<project>\<session id>.jsonl`), written by hand for `tests/AxClaude.Tests/ClaudeCodeTranscriptTests.cs`. Nothing in them comes from a recorded session: every text, id and time is invented, and the shapes follow what was measured with Claude Code 2.1.325 (`docs/axClaude/events-and-grounding.md` section 4 and EG-3.4). The recorded timelines with their session files are under `tests/fixtures/timelines`, made by a tool through its whitelist (EG-11.1).

Every line of a kind the reader skips, and every field that a kind it reads must not read (a tool's input, a tool result's text, `cwd`, the account's ids, the copies of what was sent to the model), holds the words `MUST-NOT-BE-READ`. `ClaudeCodeTranscriptTests` parses every line of every file and fails when one of the entries it returns holds those words.

| File | Shows |
| :-- | :-- |
| `plain-turn.jsonl` | A typed prompt, its reply in Markdown, the Stop hook's summary and the turn's end, with the metadata lines and the copies of the request around them; then `/exit` as Claude Code writes it: a caveat marked `isMeta`, the command, its output. |
| `tool-turn.jsonl` | One response split into three entries of one `message.id` (thinking, text, a tool call), its result, a second call whose result is an error with an array as its content, and the closing text. |
| `interrupt.jsonl` | A reply cut by Escape (`isAbortedMidStream`, a null stop reason) and the interrupt entry after it; a tool call refused by Escape, its error result and the interrupt "for tool use". |
| `queued.jsonl` | A message queued while a turn ran (`enqueue` with its text, `dequeue`, the prompt with `promptSource` `queued`), a message queued and removed, and a background task's hand-back: the enqueue, the `isMeta` prompt of source `system` that opens Claude Code's own turn, the note kept for the conversation, the reply. |
| `odd-lines.jsonl` | Lines that are no JSON object; types not read; a prompt with its properties in another order and escapes in its text; an entry without a uuid; entries without a message, with an unknown block, an empty block list, an unknown subtype or operation; an agent's entry (`isSidechain`); a command written with escaped tags; a compaction (boundary, summary, the command's output); a shell line; an agent's message; an unknown tag; a prompt of a text and a picture; a null content; a duration written as a decimal. |
