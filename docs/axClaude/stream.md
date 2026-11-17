# axClaude: Claude Code's non-interactive stream, read and measured

A report, not a specification: it decides nothing and changes no behaviour. It answers the maintainer's question of
2026-11-16, whether axClaude should also have a non-interactive mode, as a fallback for the day Claude Code changes
its screen, or as the main way. Written 2026-11-17 from two sources: Claude Code's own documentation as it stood
that day, and measurements made that day against Claude Code 2.1.326 with `tools/ClaudeProbe stream`.

The decision is the maintainer's and is not taken here. What is built so far is only what measures: the reader of
the stream's lines (`src/AxClaude.Core/Streaming/ClaudeCodeStream.cs`, with `ClaudeCodeStreamTests`), which no
window uses, and the probe.

## 1. The three ways axClaude can learn what Claude does

- **The screen.** Claude Code runs in a pseudo console in screen reader mode, and axClaude reads its rows: which are
  the reply, which are Claude Code's own (the mode line, the spinner, a progress row), where a question waits. This
  is what the window runs on today. It gives everything Claude Code shows, its own screens included, and every
  release can move a row: 2.1.326 moved the queued message's hint.
- **The session file and the hooks**, built on 2026-11-15 beside the screen and running in shadow
  (`events-and-grounding.md`): the file gives each message whole, the hooks give the moments of a turn. Claude Code's
  documentation says of the file's format that it is internal and "changes between versions, so scripts that parse
  these files directly can break on any release."
- **The stream.** Claude Code started with `-p --input-format stream-json --output-format stream-json --verbose` is
  one process with no screen: it takes one JSON line for each message on standard input and writes one JSON line for
  each thing that happens. The documentation calls `-p` the "alternative for scripting"; the Agent SDK is built on
  the same lines.

None of the three is promised stable: the documentation has no stability statement for the stream's shape and none
for the screen.

## 2. What the documentation says

Read on 2026-11-17 at code.claude.com/docs (the CLI reference, the headless page, permission modes, the Agent SDK's
pages, hooks, sessions, accessibility, errors).

- The lines: `system` with `init` (the model, the tools, the MCP servers), `assistant` and `user` messages (with
  `parent_tool_use_id` for a subagent's), a `result` line that ends a turn with its cost, duration and denials, and
  `stream_event` lines only with `--include-partial-messages`.
- A long-lived process takes several messages. A message sent during a turn "reaches Claude after those tool calls
  finish". A turn is ended by an interrupt; the process stays.
- Permissions: with no mode set, a `-p` run denies what needs approval, unless a rule, a hook or
  `--permission-prompt-tool` answers. That option names an MCP tool Claude Code calls with the question.
- Hooks, `CLAUDE.md`, settings, `--mcp-config` and `--append-system-prompt` work in `-p`. Sessions of `-p` runs are in
  the same store as the interactive program's and can be resumed either way.
- Screen reader mode "doesn't change non-interactive mode".
- Not documented: the exact line a host writes for a message when it does not use the SDK, the reply format of a
  control request, whether Claude's own questions reach the permission tool, whether `--verbose` is required.

## 3. What was measured

`tools/ClaudeProbe stream` plays eleven cases, each a process of its own in a folder under the temporary folder,
with the smallest model, no session saved, and no MCP server but its own stand-in permission tool. It prints kinds,
names and times only: never a text Claude wrote, a path or an id. Every case passed on 2026-11-17.

- **turn**: one message, one reply, the turn's end. The process was up 0.7 s after its start and the turn over 1 s
  later. Without `--verbose` the run ends at once with exit code 1 and a line that says so.
- **two-messages**: a second message written after the first turn's end is answered by the same process, 1.2 s from
  the message to the end. No start per turn.
- **tool**: a tool call allowed by a rule comes as three lines in order: the call with the tool's name, its result,
  the reply.
- **permission-allowed**, **permission-denied**: with `--permission-mode default` and the permission tool, the tool
  is called with `tool_name`, `input` and `tool_use_id`. A yes with the input lets the call run (the file was made); a
  no gives a result marked as an error, the file is not made, and the turn's end counts one denial. A command Claude
  Code holds safe (`echo`) is not asked about.
- **question**: Claude's own question (`AskUserQuestion`) reaches the same tool: its input holds the questions, each
  with a header, the question, its options and whether several may be chosen. The answer goes back as the same input
  with an `answers` map, and Claude replies with the choice. So a question and a permission are one request.
- **interrupt**: the control request is answered 12 ms later, the running tool's result comes back as an error, the
  turn ends as failed with Claude Code's word `aborted_tools` 25 ms after that, and the process answers the next
  message.
- **message-mid-turn**: a message written while a tool runs is taken up inside the running turn, after the tool's
  result, and steers it: one turn ends, and the smallest model answered the newer message alone. This is what the
  interactive program does with a message sent while Claude works.
- **partial**: with `--include-partial-messages` a reply arrives piece by piece before its whole line.
- **worker**: work handed to a subagent: the subagent's lines are marked as its own; Claude Code says when a task
  started, changed and ended, and gives the list of background tasks when it changes
  (`system/background_tasks_changed`). After the turn's end a further turn started by itself, for the subagent's
  hand-back: a turn can begin without a message of the user's.
- **unknown-model**: a failure looks like this: the error's words as a text line, a turn's end marked failed with
  Claude Code's word `api_error`, exit code 1.

The shape of every kind of line seen, key names without values, is `stream-shape.json` beside this page, written by
the probe. `tools/upstream.ps1 -Verify claude` runs the probe with the other checks of Claude Code and writes the
file again, so a new version shows what it changed as a diff.

## 4. The stream in the kinds of event axKit already names

The kinds are those of `docs/axKit/families.json`.

- `ready`: the `Started` line, with Claude Code's version and the permission mode in force.
- `working`: from the message written to the turn's end; nothing to infer from a spinner.
- `responding`: the first line of a turn that is Claude's, or with pieces, the first piece.
- `line` and `reply-spoken`: a `Text` part, whole, in order. A reply between two tool calls is a line of its own.
- `tool-call`: a `ToolCall` part with the tool's name, and its `ToolResult`, with whether it failed.
- `turn-done` and `failed`: the `Ended` line, once, last, with how it ended.
- `program-asks`: the permission tool is called; for Claude's own question the options come as data.
- `background-started`, `background-finished`: the task notes and the list of background tasks.
- `program-ended`: the process's exit.
- The messages still waiting are a number on the `Ended` line.

What the stream does not have: Claude Code's own screens (the `/config` and `/status` screens, the review of
answers, the resume picker), the mode line, and whatever a release adds to the interactive program first.

## 5. What this says about the faults of today

The shadow data of 2026-11-16 (59 turns of the maintainer's own use, the event core beside the window) shows where
the screen costs: the window said Done before it had finished speaking in 23 turns and its state went idle and back
167 times, while the core, fed by the marks, the file and the hooks, never did either; and the core in turn drops
the short replies Claude writes between tool calls, because it must match rows of the screen to entries of the file.

In the stream none of the three exists as a problem: the end of a turn is one line that comes last, the state does
not have to be read off a redrawn screen, and each reply is its own line in order.

## 6. What a window over the stream would need, and what it would not

It would not need: the screen parser and its table of Claude Code's texts, the rule that tells chrome rows from the
conversation, the settle timers, the session file's reader, the hooks, the gate that holds speech until the file
confirms a line.

It would need:

- the process and its two pipes in place of the pseudo console (the start is `ClaudeCodeProgram`'s as today);
- a permission tool: axKit already serves MCP, and a question would arrive as a request and be shown as the answer
  list the window has;
- the conversation built from typed parts in place of rows: the same view, the same quick keys;
- the things Claude Code's screens do today offered another way: the model and the effort are options or commands
  (`/model` works as a message), settings are axClaude's own menu already (FR-14), resuming is an option of the
  start;
- an answer to what the interactive program gives that the stream does not, before anything is taken away.

Not yet measured, and to be measured before any of it is built: compaction, a slash command sent as a message, a
session resumed from the interactive program and back, a long reply's pieces against the speed of speech, a
permission answered "always", and two hours of real use.

## 7. Ways to go, for the maintainer to choose

- **(a) A second mode behind a switch.** The stream feeds the same window when the user turns it on; the screen
  stays the default and the fallback. Both are kept working by their own checks. Nothing is removed.
- **(b) The stream as the main way, the screen as the fallback**, once (a) has been used for a while and has shown
  what is missing.
- **(c) Not built.** The event core takes Done and the state (it is ready for those), and its speech gate is
  repaired for the replies between tool calls.

Whichever is chosen, Done and the state from the event core help today and do not depend on the choice.
