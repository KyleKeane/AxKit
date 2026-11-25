# Stream fixtures

Each `*.jsonl` file here is one recorded case of Claude Code's non-interactive stream (`claude -p --input-format stream-json --output-format stream-json --verbose`), for the tests that replay the stream's model (docs/axClaude/stream-mode.md SM-10.1). They are written by the probe and never by hand:

```
dotnet build tools\ClaudeProbe -c Release -o <scratch>\claude-probe
<scratch>\claude-probe\ClaudeProbe.exe stream --record tests\fixtures\streams [--only <case>]
```

Every case is played against the installed Claude Code with the smallest model, with what axClaude's window will start it with: the stream's own options, each message sent back when it is taken (`--replay-user-messages`), and a permission tool for every question, which a stand-in answers as the case says. No session is saved.

## What a file holds

The first line is the recording's own: Claude Code's version, the day, and how the stand-in answered. Then one object a line, in the order of the clock, with `t`, the milliseconds since the process was started, and `k`:

- `in`: what the host wrote. `what` is `message` (with its `text`), `interrupt` or `mode` (with the request's `id`), or `closed` for standard input closed.
- `out`: a line Claude Code wrote, as `line`, reduced to the fields named for its kind in `tools/ClaudeProbe/StreamFixtureMaker.cs`. A kind not named there keeps its type alone.
- `ask` and `answer`: the permission tool was asked about a `tool` with this `input`, and the stand-in answered `allow`, `deny`, `always`, or `answered` for a question of Claude's (the first option of each question).
- `exit`: the process ended, with its `code`.

## The expected files

`<case>.expected.txt` beside a recording is what the stream's model makes of it (`StreamModelTests`, through `StreamReplay`): each thing the host wrote and Claude Code wrote, in order and in kinds, with what the model raised under it (the bell, the response started, an announcement, a turn Claude Code started by itself) and its facts after every change; then the conversation as it stands, every line with its kind and, for a heading, its level. The tests fail when the two disagree. After a deliberate change of the model, or a new recording, `AXCLAUDE_UPDATE_EXPECTED=1` writes them again: read the difference through. What a recording's model says is its own each time it is recorded, so a new recording changes the texts in these files, never what the model does with them.

## What a file does not hold

The maker is the whitelist that makes a fixture safe to commit. A field it does not name is not kept. Ids are numbers in the order they were met (`session-1`, `call-1`, `task-1`). The folder a case ran in is `C:\work`. Of a tool's result only whether it failed is kept, and its text when it did. Thinking is kept as a kind without its text. The start line keeps the version, the permission mode and the session's number: nothing of the user's commands, servers, skills or paths. A file that still held an e-mail address, an account path, the account's or the computer's name would have been refused and deleted.

## The cases

The window scripts use the same files: the stand-in of the stream (`tools/AxAutomate/StreamStandIn.cs`, `AxAutomate standin --stream <case>`) plays a recording to a window made for the stream, so a change to a file here changes what `tests/scripts/axClaude/37` to `43` meet.

| File | Shows |
| :-- | :-- |
| `start` | The request a window sends first, answered before any message with the permission mode and the version; then a turn. |
| `turn` | One message, its reply, the turn's end. |
| `two-turns` | A second message after the first turn's end: a start line for each turn. |
| `headings` | A reply written in Markdown: two headings, a word in bold, a list, a fenced line of code, as one text. |
| `tool` | A tool call the user allows: the call, the question to the permission tool, the result, the reply. |
| `denied` | A tool call the user refuses: the result is an error with the answer's words, and the end counts the refusal. |
| `always` | A yes for the rest of the session: two calls of the same tool, one question. |
| `question` | A question of Claude's: the call carries the question, and so does the request to the permission tool. |
| `two-questions` | Two questions in one call, the second with several answers allowed. |
| `interrupt` | A running tool interrupted by the host, then a message that is answered. |
| `mid-turn` | A second message written while a tool runs. |
| `while-writing` | A second message written while a reply is being written. |
| `mode` | The permission mode set by the host before the first message. |
| `compact` | A turn, `/compact`, and a message after it. |
| `clear` | A turn, `/clear`, and a message after it. |
| `model` | `/model` with a name, which answers with one line, then a message. |
| `worker` | Work handed to a subagent, and the turn Claude Code starts by itself when it hands back. |
| `background` | A command run in the background, and the turn Claude Code starts by itself when it ends. |
| `failure` | A model that does not exist: the error's words, a failed end, the process gone. |
| `custom-command` | A custom command of the folder's own (`.claude/commands/probe-greet.md`, written by the probe first), which runs the model, then a message (2.1.332). |
| `choices` | The first request's answer with the models and their efforts, then `/model haiku`, `/effort low` and a message (2.1.332). |

## What the recordings showed (Claude Code 2.1.327, 2026-11-17)

These are what the model is built on. Each is in the files; none is taken from the documentation alone.

- **Before the first message** the stream writes nothing by itself. A request of the host's, the one the Agent SDK sends first, is answered some 650 ms after the start, and the answer names the permission mode in force and Claude Code's version (`start`). It also names the account and lists the user's commands, agents and models: the fixture keeps the mode and the version alone.
- **The start line** comes at the beginning of every turn, also of a turn Claude Code starts by itself, and names the session and the permission mode.
- **A message comes back** as a line of the user's marked as a replay, a few milliseconds before the first line of the reply it starts. It is no early sign that the message was taken.
- **A message written while a tool runs** is taken up inside the running turn, after the tool's result: its replay line comes there, and one end line ends the turn for both messages (`mid-turn`).
- **A message written while a reply is being written** is not taken into that turn. The turn ends, its end line saying that nothing waits, and a turn of its own starts some 16 ms later (`while-writing`). So the count of waiting messages on an end line cannot say whether another turn follows.
- **A slash command** brings no replay of its own text. `/model` answers with one text of the assistant's and an end line that counts no turn (`num_turns` 0). `/clear` gives a line that says the conversation was emptied, a start line with a new session, and such an end line. `/compact` gives a status line that says compacting, some seconds later a status line that says nothing, a start line, the boundary, a line written in the user's place that holds the summary, a replay line that holds the command's own output in a tag, and such an end line.
- **A permission question** reaches the permission tool some 25 to 40 ms after the line of the call. A no comes back as the call's result marked as an error, with the words the tool answered, and the turn's end names the tool refused.
- **A question of Claude's** is a call of a tool of its own: the call's line carries the questions, each with its header, its options and whether several may be chosen, and the permission tool is asked with the same. Its result is a sentence for the model.
- **An interrupt** is answered at once; the running tool's result comes back as an error, then a line of the user's that says the request was interrupted, then an end line marked as an error with Claude Code's word for it. The process answers the next message.
- **Work in the background** is listed whole each time it changes: each task with its kind (a shell's command, an agent), its description and, for an agent, its type. A task's start, change and end come as lines of their own. When a task in the background ends, Claude Code starts a turn by itself: a start line, a reply, an end, with no line of the user's before it.
- **A subagent's own lines** carry the id of the call that started it; the lines of the conversation itself carry none.
- **A failure of the service** (a model that does not exist) is a text of the assistant's marked as an error with a word for its kind, an end line marked as an error, and the process ends with code 1.
- **The permission mode** set by the host is answered, a status line names the new mode, and the next start line names it too.

## What the recordings of 2026-11-25 showed (Claude Code 2.1.332)

- **A command that runs the model** (a custom command) comes back, unlike a command Claude Code runs by itself: a line of the user's marked as a replay, whose text is Claude Code's tags, `<command-message>probe-greet</command-message>` and `<command-name>/probe-greet</command-name>`, just before the turn's thinking and texts. The turn's end counts one turn (`custom-command`).
- **The first request's answer** lists the models, each with the name Claude Code takes (`haiku`), the name it shows, its words, and the efforts it takes, low to high; one model listed none. The start line names the model in use by its full name (`choices`). The fixture keeps these and the mode and the version; since this day it also keeps the start line's model.
- **`/model` and `/effort` with a value** are taken while the run goes on, as messages: each brings no replay of its own text, one text of the assistant's that says what was set "for this session only", and an end that counts no turn (`choices`).
