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
- **The session file and the hooks**, built on 2026-11-15 beside the screen (`events-and-grounding.md`): the file
  gives each message whole, the hooks give the moments of a turn. Since 2026-11-17 they decide the state (whether
  Claude works); Done and what is spoken are still the screen's, with the rest in shadow. Claude Code's
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

Read again the same day for the cases of section 3 that were still missing (the headless page, sessions, the CLI
reference, commands, and the Agent SDK's pages on permissions, user input and streaming):

- Slash commands: "User-invoked skills and custom commands work"; "Built-in commands that only run in the terminal
  interface, such as /login, aren't available"; `/model`, `/effort` and a few others "accept the value as an
  argument". The start line lists the commands a run takes. Not documented: that a message written on standard
  input is run as a command when it starts with a slash, and what `/clear` does to the session.
- Sessions: a session of a `-p` run is resumed by its id from anywhere (`--resume <id>`), and "Claude Code leaves
  sessions created with claude -p or the Agent SDK out of the session picker and out of claude --continue". So the
  interactive program and a stream hand a conversation to each other by its id, and by nothing else. Not documented:
  whether the earlier conversation is written again as lines.
- `--replay-user-messages`: "Re-emit user messages from stdin back on stdout for acknowledgment."
- A yes that lasts: documented for the Agent SDK's callback, as an answer that carries `updatedPermissions`, a rule
  with a destination, of which `session` is written to no file. Not documented: whether the raw permission tool's
  answer takes the same field, and whether its question brings ready-made suggestions.
- The requests a host may write beside the interrupt (the permission mode, the model) are documented as methods of
  the SDK only; their lines are not.
- A status note says `compacting` while Claude Code shortens the conversation and nothing when that is over; a
  note of its own marks the boundary.
- Stability: no page promises the lines' shape. The start line's `capabilities` are there "to feature-detect instead
  of comparing version strings", and unknown values are to be ignored.

## 3. What was measured

`tools/ClaudeProbe stream` plays twenty-three cases (twenty when this page was written; the three of axKit's own
relay came with it, below), each a process of its own in a folder under the temporary folder,
with the smallest model, no session saved (but in the one case that takes a session up again, which deletes its
session's file afterwards), and no MCP server but its own stand-in permission tool. It prints kinds, names and times
only: never a text Claude wrote, a path or an id. Every case passed on 2026-11-17. The first eleven:

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

The nine added the same day, for what section 6 had listed as not yet measured:

- **long-reply**: a reply of about 300 words came in some 160 pieces, the first 1.6 s after the start, never more
  than about a ninth of a second apart, at close to 500 characters a second; the whole message's line followed 5 ms
  after the last piece. That is some ten times the pace of speech: a reply is whole long before it can be heard.
- **resume**: a second process started with `--resume` and the session's id knew what the first had been told, and
  its start line named the same session. Nothing of the earlier conversation was written again: no line of the
  user's, no reply. What a window shows of an older conversation has to come from somewhere else.
- **replay**: with `--replay-user-messages` the message came back as a line of the user's, but only with the reply,
  some 3 ms before it. It acknowledges; it is no early sign that the message was taken.
- **continue-nothing**: `--continue` in a folder with no session started a new one and answered. No error, where the
  interactive program says that there is nothing to continue and ends.
- **interrupt-idle**: an interrupt with no turn running is answered as done and changes nothing; the next message
  is answered.
- **mode**: the permission mode set by the host (the interactive program's Shift+Tab) was taken 0.6 s after the
  start, before any message; a status note followed, the next start line named the new mode, and a file was then
  written without the permission tool being asked.
- **permission-always**: the raw permission tool's answer with a rule of the session's (`updatedPermissions`, the
  destination `session`) held: two files written, the tool asked once. The question itself brings three keys, the
  tool's name, its input and the call's id, and no suggestion: the rule is the host's to write.
- **slash-command**: a message that is a slash command is run as that command, with no call of the model. `/context`
  gave its print as a text of Claude's and a line that ends it like a turn, with no word for how it ended, 5 ms
  after the start line. `/model` with a name answered with one short text. `/clear` gave a line of a kind of its own
  that says the conversation was emptied, and the next start line named a new session. The start line listed 68
  commands on the development computer, the user's own among them.
- **compact**: `/compact` gave a status note that says compacting, 3.6 s later a status note that says nothing, then
  a note that marks the boundary and a line that ends it; the word given before it was still known after.

The three added with axKit's own relay (`AxKit.exe ask`, `docs/axClaude/stream-mode.md` SM-7.3), the probe playing
the window's end of the relay's pipe:

- **relay**: Claude Code started the relay from the configuration a window will write, asked it about the tool,
  and the relay, which had the pipe's name and secret from Claude Code's own environment and from no file, passed
  the question to the stand-in of the window and its yes back: the file was made, some 100 ms from the call to its
  result.
- **relay-refused**: the window's no came back as the call's result marked as an error; the file was not made, and
  the turn's end counted one denial.
- **relay-gone**: with no window to ask, the relay waited its two seconds for the pipe and answered no; the file
  was not made.

Seen across all cases: the start line comes at the beginning of every turn, not once at the process's start; and
every case ran in a folder Claude Code had never seen, and none was asked whether the folder is trusted.

The shape of every kind of line seen, key names without values, is `stream-shape.json` beside this page, written by
the probe. `tools/upstream.ps1 -Verify claude` runs the probe with the other checks of Claude Code and writes the
file again, so a new version shows what it changed as a diff.

## 4. The stream in the kinds of event axKit already names

The kinds are those of `docs/axKit/families.json`.

- `ready`: the process being up. The `Started` line, with Claude Code's version, the permission mode in force and
  the session's id, comes with each turn, the first included, so it cannot say ready before the first message.
- `program-said`: a status note (compacting, and when that is over), the note that the conversation was emptied,
  and the permission mode a status note or a start line names after a change.
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

Measured since (section 3, the nine cases added on 2026-11-17): compaction, slash commands sent as messages, a
session taken up again, a long reply's pieces against the speed of speech, a permission answered "always", the
permission mode set by the host, an interrupt with nothing running, `--continue` with nothing to continue.

What those measurements ask of a window over the stream, beyond the list above:

- **The older conversation.** A session taken up again writes nothing of what was said before. The window shows it
  from the session's file, which axClaude reads already (`ClaudeCodeTranscript`), or keeps what it showed.
- **The folder's trust.** The interactive program asks before it works in a folder it has not seen; the stream
  does not. A window over the stream asks that question itself, before the first start in a folder, since a
  project's own settings can name programs to run.
- **Ready.** The start line comes with each turn, so it cannot say that Claude Code is ready before the first
  message. The process being up is the sign; a request of the host's is answered within about 0.6 s of the start.
- **Sent.** The acknowledgement of a message comes with its reply, so "Sent" is said when the line is written.
- **Handing a conversation over.** A session of the stream is left out of the interactive program's own list and of
  its `--continue`: each side takes the other's conversation up by the session's id, which the start line gives.
- **`/login`** and the other commands of the terminal alone are not there: signing in stays the interactive
  program's.

Still not measured: a session handed from the interactive program to the stream and back (the documentation says
by its id; the probe has only taken a stream's session up with a stream), and two hours of real use, which need
the mode to exist.

## 7. Ways to go, for the maintainer to choose

- **(a) A second mode behind a switch.** The stream feeds the same window when the user turns it on; the screen
  stays the default and the fallback. Both are kept working by their own checks. Nothing is removed.
- **(b) The stream as the main way, the screen as the fallback**, once (a) has been used for a while and has shown
  what is missing.
- **(c) Not built.** The event core takes Done and the state (it is ready for those), and its speech gate is
  repaired for the replies between tool calls.

Whichever is chosen, Done and the state from the event core help today and do not depend on the choice.

**Decided on 2026-11-17: (a)**, by the maintainer through the question tool, together with the event schema
(`docs/axKit/events.md`, section 9 point 1): the stream is built as the second adapter of that schema, behind a
switch that is off at first, after the sounds have gone through the door. What section 6 lists as not yet measured
is measured before it is built. (b) is looked at again once (a) has been used for a while.
