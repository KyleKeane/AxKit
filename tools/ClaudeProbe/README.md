# ClaudeProbe: Claude Code under axKit's console pieces, with hooks, on one clock

A measuring tool for axClaude (`docs/axClaude/events-and-grounding.md` EG-11.4). It starts the installed Claude Code the way axClaude does (the same pseudo console code, the same program search, the same environment), registers hooks for that one session through `--settings`, and records three streams on one clock: what the pseudo console prints, every hook event, and every line the session's file gains. A fourth and a fifth stream come free: what the script wrote to Claude Code, and what the repository's own screen model (`SessionModel`) concluded from the screen. Its last command makes a recorded run into a timeline fixture through the whitelist of EG-11.1 (section 8).

It was the scratch probe of 2026-11-15, from whose recordings the specification was written; its files are kept as they were, and its usage text still calls it `probe`. The program is `ClaudeProbe.exe`.

## What it is, and what it is not

It is a measuring tool of the repository: never shipped, never part of `AxKit.exe`, never started by axKit. While a recording runs it listens on 127.0.0.1 for the hooks it measures, by both of Claude Code's transports (an http hook posts to it; a command hook runs `ClaudeProbe relay`, which posts its standard input). axKit itself never listens (`docs/axKit/mcp.md` section 1; EG-D-2): axClaude's hooks are a program start and a named pipe (EG-2.6, EG-2.7). A measuring tool that is never shipped and never started by axKit is not axKit listening (EG-D-15). The listener answers on the loopback address alone and only while `run` runs.

## 1. Rules for whoever runs it

1. Build it into a scratch folder, never into a folder of the repository: `dotnet build tools\ClaudeProbe -c Release -o <scratch folder>`, then `dotnet build-server shutdown`. The build compiles `AxKit.Core` and `AxClaude.Core` too; it never touches `src\AxKit\bin`, which a running axKit holds.
2. One live session at a time, the model `haiku` (the default), short prompts: the user's account pays.
3. The working folder (`cwd`) is a scratch folder, a fresh one per scenario unless the scenario is about resuming, under a neutral folder such as `C:\Temp\axclaude-fixture`, so that no account path is on the screen. Never a folder of the user's.
4. Hooks come only from the settings file the probe writes into the output folder and passes with `--settings`. Never put a hook into any settings file.
5. What you read from a run comes from `timeline` and `shape`, which replace the profile folder by `%USERPROFILE%`, e-mail addresses by `<email>` and the account name by `<name>` in everything they print. The files of a run are not redacted: they hold the profile path (in `cwd`, `transcript_path` and the like) and copies of what was sent to the model. A run folder is never committed and never copied into the repository; a fixture is made from it with `fixture` alone (section 8). The screen's banner names the account's plan beside the model.
6. Never run `nvda-pass.ps1`, the window scripts, or anything that takes the keyboard. The probe opens no window: the pseudo console is headless.

## 2. Running it

PowerShell, through the PowerShell tool:

```powershell
dotnet build tools\ClaudeProbe -c Release -o "$env:TEMP\claude-probe"
dotnet build-server shutdown
$p = "$env:TEMP\claude-probe\ClaudeProbe.exe"
& $p run C:\Temp\axclaude-fixture\my-scenario.json
& $p timeline C:\Temp\axclaude-fixture\my-scenario-out
& $p shape C:\Temp\axclaude-fixture\my-scenario-out
& $p fixture C:\Temp\axclaude-fixture\my-scenario-out tests\fixtures\timelines\my-scenario
```

The commands:

- `run <config.json>`: one session. Progress goes to standard error (one line per thing sent), the summary (the content of `summary.json`) to standard output. Exit code 0: the script ran to its end. 3: a wait timed out and the rest of the script was dropped (the session was still ended and everything recorded). 2: Claude Code was not found, or the command line was wrong. 1: an error, a configuration that cannot be read included; the message is on standard error.
- `timeline <output folder> [options]`: the streams merged by time (section 6).
- `shape <output folder> [hooks|file] [--values]`: for every hook event seen, how often and by which transport it came and the keys of its input with their value types and one value; for the session file the same per entry type, values only with `--values`.
- `settings <config.json>`: prints the settings file the configuration gives, without running anything (the port shows as `PORT`, in a relay's arguments as `0`).
- `keys`: the key names the `keys` step takes.
- `fixture <output folder> <fixture folder>`: a run made into a timeline fixture (section 8).
- `relay <port> <event>`: what a command hook runs. Not for you.

A run takes the script's time plus about 3 seconds (2 seconds of waiting for late events after the end, set by `lingerMs`).

## 3. The configuration file

JSON; `//` comments and trailing commas are allowed. In `cwd`, `out`, `claude`, `args`, `env` values and `program`, `%NAME%` becomes that environment variable. Forward slashes are fine in paths.

| Key | Default | Meaning |
|---|---|---|
| `cwd` | required | Working folder for Claude Code. Made if missing. |
| `out` | required | Output folder. Made if missing; files in it are overwritten. One run per output folder. |
| `model` | `"haiku"` | `--model`. An empty string leaves the option out. |
| `args` | `[]` | More arguments for Claude Code, added last (`["--continue"]`, `["--session-id", "<uuid>"]`, `["--permission-mode", "default"]`). |
| `cols`, `rows` | 240, 50 | The console's size, the app's. |
| `debugFile` | `true` | Adds `--debug-file <out>\claude-debug.txt`. It changes the screen: see section 7. Set `false` when the screen itself is what you measure. |
| `screenReader` | `true` | `--ax-screen-reader`. |
| `passSettings` | `true` | `false` leaves `--settings` out altogether. |
| `env` | `{}` | More environment variables for Claude Code. The tool itself always sets Claude Code's variable against background updates for what it starts (`ClaudeCodeProgram.NoBackgroundUpdateVariable`), so that one run or one set of cases meets one version (docs/axKit/automation.md, section 4). |
| `claude` | found as the app finds it | Path of the Claude Code program. |
| `port` | 0 | The listener's port; 0 takes a free one. |
| `maxRunMs` | 300000 | After this the script is dropped and the session ended. |
| `lingerMs` | 2000 | How long the listener and the file watcher stay after Claude Code has ended. |
| `hooks` | plan `auto` | The hook plan, below. `"none"` as a string: no hooks at all. |
| `settings` | none | More top-level keys for the settings file, written as given (for one, `{"disableAllHooks": true}`). |
| `script` | `[]` | The input script, section 4. |
| `program` | none | Testing only: another program in the pseudo console in place of Claude Code (a batch file or `cmd.exe`, which costs nothing). |

How Claude Code is started: `<claude> --ax-screen-reader --model haiku --strict-mcp-config --settings <out>\settings.json [--debug-file <out>\claude-debug.txt] <args>`, in `cwd`, with `ClaudeLauncher.BuildEnvironment()` (no `CLAUDE*` variable of the calling session, `TERM=xterm-256color`, `CLAUDE_AX_STARTUP_QUIET_MS=0`, `CLAUDE_AX_PREPARK_MS=0`, `CLAUDE_CODE_DISABLE_ALTERNATE_SCREEN=1`) plus `DISABLE_AUTOUPDATER=1` plus `env`. `--strict-mcp-config` with no `--mcp-config` means no MCP servers. The user's own settings, plugins and permission mode still apply to the session: the probe adds to them and reads none of them.

### The hook plan

```json
"hooks": {
  "default": "auto",
  "events": { "Stop": "both", "MessageDisplay": "none" },
  "delayMs": { "PreToolUse": 800, "*": 0 },
  "timeoutSeconds": { "PreToolUse": 5 },
  "async": ["PostToolUse"],
  "matchers": { "FileChanged": "watched.txt" },
  "replies": { "Stop": { "status": 200, "body": { "decision": "block", "reason": "..." }, "exit": 2, "stderr": "..." } },
  "extra": { "Stop": { "statusMessage": "..." }, "*": { } },
  "closedPort": false,
  "commandForm": "exec"
}
```

- `default`: what every event gets that `events` does not name. `auto` (the default): an http hook, and a command hook for `SessionStart` and `Setup`, which take no http hook. `http` and `both`: the same two get a command hook only. `command`: a command hook for all. `none`: nothing.
- `events`: per event `http`, `command`, `both` or `none`, written exactly as asked, even where the documentation says the event takes no such hook, so that this can be tried. A name not in the probe's list of 33 is registered too.
- `WorktreeCreate` and `WorktreeRemove` get no hook unless `events` names them: a hook there does the work itself and is no observer.
- `delayMs`: the listener waits this long before it answers the event (`"*"` for every event). The relay waits for the answer, so the delay holds a command hook as well.
- `timeoutSeconds`: the handler's `timeout` field. `async`: `"async": true` on the event's command handler. `matchers`: the group's `matcher`. `extra`: more fields for the event's handlers, written as given (`statusMessage`, `onFailure`, `asyncRewake`, `headers`).
- `replies`: instead of the empty 200. For an http hook: `status` and `body` (an object is sent as JSON, a string as it is). For a command hook the relay prints `body` on standard output, `stderr` on standard error, and exits with `exit`. Without a reply the answer is an empty 200 and the relay prints nothing and exits 0: nothing is added to the conversation.
- `closedPort`: the http hooks point at a port on 127.0.0.1 that nothing listens on; command hooks still reach the listener.
- `commandForm`: `exec` (the default: `"command": "<ClaudeProbe.exe>", "args": ["relay", "<port>", "<event>"]`, no shell; the form axClaude uses), `bash` and `powershell` (a command string with `"shell"` set), `shell` (a command string, Claude Code's default shell). Only `exec` had been run against Claude Code when this was written.
- `"hooks": { "mode": "none" }` or `"hooks": "none"`: the settings file is `{}`.

The 33 event names the probe knows, from the documentation reference: SessionStart, Setup, UserPromptSubmit, UserPromptExpansion, PreToolUse, PermissionRequest, PermissionDenied, PostToolUse, PostToolUseFailure, PostToolBatch, Notification, MessageDisplay, SubagentStart, SubagentStop, TaskCreated, TaskCompleted, Stop, StopFailure, TeammateIdle, InstructionsLoaded, ConfigChange, CwdChanged, DirectoryAdded, FileChanged, WorktreeCreate, WorktreeRemove, PreCompact, PostCompact, PreModelSwitch, PostModelSwitch, Elicitation, ElicitationResult, SessionEnd.

The address of an http hook is `http://127.0.0.1:<port>/hook/<Event>?via=http`; the relay posts to `/hook/<Event>?via=command`. So every request says which registration it came from and by which transport.

## 4. The script

An array of steps, run in order. Every step is one JSON object. A wait that times out drops the rest of the script (the session is then closed) unless the step says `"onTimeout": "continue"` or has an `else`. The default `timeout` of a wait is 30000 ms.

Waits:

- `{ "wait": 1500 }`: milliseconds.
- `{ "waitStatus": "asking|ready", "timeout": 60000, "stableMs": 300 }`: until the app's model has one of these statuses at a frame's end, and has had it for `stableMs`. The statuses: `starting` (no mode line read yet), `asking` (a question or a screen waits: `PromptPending`), `working` (a spinner row or "esc to interrupt"), `ready`, and `exited` (the process ended).
- `{ "waitFor": "<regex>", "timeout": 30000, "kind": "ClaudeReply", "visible": true }`: until the regular expression matches the screen text as the model holds it: its last 120 lines, one per line, hidden rows (the prompt row, the mode line, the spinner) included unless `visible` is true; `kind` keeps only lines of that `LineKind`. Case is ignored; `^` and `$` match at line ends.
- `{ "if": "<regex>", "then": [ ... ], "else": [ ... ] }`: the same match, now, without waiting.
- `{ "waitHook": "Stop|StopFailure", "count": 1, "timeout": 60000 }`: until that many hook requests for an event of this name (a regular expression matched against the whole name) have arrived since the last input or `mark` step. Both transports count: with `both`, one event is two requests.
- `{ "waitFile": "<regex>", "timeout": 30000 }`: until a line matching the expression has arrived in a followed session file since the last input or `mark` step. Matched against the raw line, so `"\"type\":\"assistant\""`.
- `{ "waitRaw": "<regex>", "timeout": 30000 }`: against the raw output since the last `mark` (or the start), escape sequences and all, case-sensitive. `"\\x07"` is the bell.
- `{ "waitQuiet": 1000, "timeout": 30000 }`: until the console has printed nothing for that long.
- `waitFor`, `waitRaw`, `waitStatus`, `waitHook`, `waitFile` and `waitQuiet` take `then` and `else` arrays: `then` runs when the wait was met, `else` when it timed out (and then the script goes on).

Input, each write logged with its time:

- `{ "message": "text" }`: a message as the window sends it: the app's exchange block is put into the model (`SessionModel.Send`), the text is written, 100 ms later Enter, and Enter again while the prompt row still holds the text 1.5 seconds later, three times at most (the app's rule for a dropped Enter, FR-2.1). So the step takes at least 1.5 seconds. `"retryEnter": false` returns right after the Enter; `"enterDelayMs"` changes the 100; `"appBlock": false` leaves the model alone.
- `{ "send": "text" }`: the text in one write (to Claude Code that is a paste). No Enter.
- `{ "type": "text", "perCharMs": 30 }`: one write per character, as keystrokes.
- `{ "keys": "Enter" }` or `{ "keys": ["Down", "Down", "Enter"], "gapMs": 150 }`: named keys, one write each. The names are those of `src/AxClaude.Core/Transcript/ClaudeCodeKeys.cs` (Enter, NewLine, Escape, Tab, ShiftTab, Up, Down, Right, Left, ShiftUp, ShiftDown, ShiftRight, ShiftLeft, CtrlC, CtrlD, CtrlO, CtrlX, CtrlS) plus Space, Backspace, Delete, Home, End, PageUp, PageDown, any single character (`"y"`, `"1"`) and `Ctrl+<letter>`.
- `{ "bytes": "1b5b41" }`: bytes in hexadecimal.

Other steps:

- `{ "mark": "label" }`: a mark in the timeline; `waitHook`, `waitFile` and `waitRaw` count from here.
- `{ "writeFile": { "path": "notes.txt", "text": "...", "append": false } }`: writes a file inside the working folder (for `FileChanged`, `InstructionsLoaded` and the like).
- `{ "end": "exit", "timeout": 20000 }`: types `/exit`, Enter, and waits for the process to end. `"close"`: the probe closes the console, as closing the window does. `"kill"`: the process is terminated. Without an `end` step the console is closed when the script ends. Steps after `end` are not run.

The workspace trust question comes in a fresh folder and not in one already trusted, so answer it conditionally. In screen reader mode it reads `Enter y/n:` and is answered with `y`, then Enter, not within its first second:

```json
{ "waitStatus": "asking|ready", "timeout": 60000 },
{ "if": "trust", "then": [
  { "wait": 1500 },
  { "keys": ["y", "Enter"], "gapMs": 300 },
  { "waitStatus": "ready", "timeout": 60000, "stableMs": 300 }
] }
```

After a `message`, do not wait for `ready` at once: until Claude Code starts, the status still is `ready`. Wait for a hook (`waitHook` `Stop`), or for `working` first.

Do not script `/status`, `/usage` or `/cost` in a run meant for a fixture: they print the account's e-mail address, and `fixture` refuses such a run.

## 5. Two complete examples

### Example 1: one short turn, every event by both transports

The scenario that validated the probe on 2026-11-15, with Claude Code 2.1.325. It took 12.8 seconds in all, for one turn with a one-word reply.

```json
{
  "cwd": "C:/Temp/axclaude-fixture/validate",
  "out": "C:/Temp/axclaude-fixture/validate-out",
  "model": "haiku",
  "maxRunMs": 200000,
  "hooks": { "default": "both" },
  "script": [
    { "waitStatus": "asking|ready", "timeout": 60000 },
    { "if": "trust", "then": [
      { "wait": 1500 },
      { "keys": ["y", "Enter"], "gapMs": 300 },
      { "waitStatus": "ready", "timeout": 60000, "stableMs": 300 }
    ] },
    { "wait": 1000 },
    { "message": "Reply with the single word ok." },
    { "waitHook": "Stop", "timeout": 60000, "else": [
      { "waitStatus": "ready", "timeout": 60000, "stableMs": 1000 }
    ] },
    { "waitStatus": "ready", "timeout": 30000, "stableMs": 500, "onTimeout": "continue" },
    { "wait": 1500 },
    { "end": "exit", "timeout": 20000 }
  ]
}
```

### Example 2: a long reply interrupted, a per-event plan, no debug file

It parses (`settings` prints its settings) but it had not been run when this was written: take it as the pattern, and expect to adjust the waits.

```json
{
  "cwd": "C:/Temp/axclaude-fixture/interrupt",
  "out": "C:/Temp/axclaude-fixture/interrupt-out",
  "debugFile": false,
  "maxRunMs": 180000,
  "hooks": {
    "default": "auto",
    "events": { "Stop": "both", "MessageDisplay": "none" },
    "delayMs": { "PreToolUse": 800 },
    "timeoutSeconds": { "PreToolUse": 5 }
  },
  "script": [
    { "waitStatus": "asking|ready", "timeout": 60000 },
    { "if": "trust", "then": [
      { "wait": 1500 },
      { "keys": ["y", "Enter"], "gapMs": 300 },
      { "waitStatus": "ready", "timeout": 60000, "stableMs": 300 }
    ] },
    { "wait": 1000 },
    { "message": "Count from 1 to 40, one number per line, no other text.", "retryEnter": false },
    { "waitStatus": "working", "timeout": 15000 },
    { "wait": 1500 },
    { "mark": "escape" },
    { "keys": "Escape" },
    { "waitHook": "Stop|StopFailure", "timeout": 8000, "onTimeout": "continue" },
    { "waitStatus": "ready", "timeout": 30000, "stableMs": 1000 },
    { "wait": 1000 },
    { "end": "exit", "timeout": 20000 }
  ]
}
```

A configuration with `program` (a batch file or `cmd.exe` in place of Claude Code) costs nothing and checks the probe's own plumbing: both transports, a delayed reply, a configured reply through the relay, the file watcher, the input steps.

## 6. The output files and the timeline

Every record has `t`: milliseconds since the probe started, from one `Stopwatch`, to a tenth. Records of `hooks.jsonl`, `transcript-arrivals.jsonl` and `script.jsonl` also carry `wall`, the wall clock (UTC) at that moment. `run.json` has `startedWall`, the wall clock at `t` = 0.

| File | What it holds |
|---|---|
| `run.json` | The command line, the folders, the ports, the resolved hook plan, Claude Code's version, `startedWall`. |
| `settings.json` | The settings file given to Claude Code. |
| `pty.vt`, `pty.vt.chunks.txt` | The raw console output and its index (`offset length elapsedMs`, the repository's `StreamRecorder` format, on the probe's clock). `tools/PtyCapture --dump` reads `pty.vt`; only its redacting dump, never `--raw`. |
| `pty-events.jsonl` | `k` = `chunk` (offset, bytes), `bel`, `title` (text), `osc` (code, data: `133` with `A;...`, `C`, `D`), `cursor-hide`, `cursor-show`, `mode` (another private mode set or reset), `clear`, `home`, and `frame`: the end of a frame of the model, `why` = `feed` (the cursor was shown again) or `quiet` (100 ms without output) or `send`, with `rows`: every row whose text the frame changed (`id`, `hidden`, `kind`, `text`), chrome included. |
| `app.jsonl` | The model's conclusions. `k` = `status` (status, working, promptPending, mode, spinner, queued, question, questionOptions, screen, awaitsText, background, agents, agentsAwaited, detailedView, sessionName), written when any of these but the spinner changes; `line` (`op` = add, change, show, hide, kind, commit; id, kind, committed, text) for visible lines with text; `bell`; `response-started`; `announcement`. |
| `hooks.jsonl` | One line per request: `t` (the request's body was complete), `wall`, `event` (the body's `hook_event_name`), `via` (`http` or `command`), `path`, `method`, `conn` and `req` (the connection and the request's number on it), `tAccept`, `tRequestLine`, `tReply`, `delayMs`, `status`, `replyWritten`, `replyBytes`, `bodyBytes`, `utf8` (the body was valid UTF-8), `headers`, `body` (the body unchanged, as one string). For `via` = `command` also `relay`: `pid`, `tCreated` (when the hook's process was created), `tMain`, `tStdinEnd`, `cwd`, and `env`: the names of the `CLAUDE*` and `ANTHROPIC*` variables the hook process had, with values only for a short list of harmless ones. |
| `transcript-arrivals.jsonl` | `k` = `line`: `t` (the poll that found the line complete), `wall`, `file`, `offset`, `bytes`, `line` (the line unchanged). `first: true`: the line was already in the file when the folder scan found the file, so `t` is an upper bound. `tornMs`: the line was seen incomplete for that long. Other kinds: `root`, `tracked` (a file is followed from now), `other-file` (a file that is no `.jsonl` appeared), `truncated`, `replaced`, `open-failed`. |
| `transcript\` | A copy of each followed file as it stood at the end. |
| `script.jsonl` | `started`, `send` (what, text, hex, `t` before the write, `tWritten` after), `step`, `waited` (verb, ok, ms), `mark`, `write-file`, `exit` (code), `closing`. |
| `processes.jsonl` | Every process that appeared under Claude Code (name, pid, parent, `tStarted`), sampled every 100 ms; `ended` for one the probe had to end. |
| `claude-debug.txt` | Claude Code's debug log, when `debugFile` is true. Claude Code also leaves an empty file `latest` beside it. Never print it whole: use `timeline --dbg`. |
| `summary.json` | Duration, exit code, time-outs, the session id, counts per event and transport, and per session file: lines, bytes, and `appendOnly` (the file's final content equals the logged lines in order, so it only ever grew by appends). |

`timeline <output folder>` prints one line per event: the time, the source, a description.

- `PTY`: `frame end (cursor shown | 100 ms quiet), N rows changed:` and the three bottom-most changed rows, each cut at 100 characters, a hidden (chrome) row marked `~`; `BEL`; `OSC 133;C`; `title "..."`; `screen cleared`.
- `HOOK`: the event, `[http]` or `[command]`, the telling fields that are present (source, reason, trigger, notification_type, tool_name, tool_use_id, agent_type, agent_id, permission_mode, stop_hook_active, is_interrupt, load_reason, model, index, final, error, message, prompt, last_assistant_message, delta, file_path and a few more), `tool_input{keys}`, `session=` (8 characters); for http `conn=<connection>.<request>`; for a command hook when its process was created; a delayed or configured reply.
- `FILE`: the file (8 characters of its name), the entry's type and subtype, `role=`, `blocks=[...]` (a tool call with its name and the end of its id), `stop_reason=`, `uuid=` and `parent=` (8 characters), flags, how many milliseconds after the entry's own `timestamp` the line was in the file, the first 50 characters of its text, its size.
- `SEND`: what the script wrote. `STEP`: the process's start and end, marks, timed-out waits.
- `APP`: the model's status changes and visible lines.
- `DBG` (only with `--dbg`): the hook lines of Claude Code's debug log, put on the probe's clock through the wall clock.

Options: `--from <ms>`, `--to <ms>`, `--only PTY,HOOK,FILE` (any of PTY, HOOK, FILE, SEND, STEP, APP, DBG), `--three` (PTY, HOOK and FILE only), `--all` (also chunks, cursor hide and show, modes, met waits), `--dbg`, `--width N` (lines are cut at 240 characters).

## 7. Limits and things to know

- **The clock.** `t` of a PTY chunk is when the probe's reader thread got it; of a hook, when its request body was complete at the listener; of a file line, the poll that saw it (every 10 ms; new files are looked for every 50 ms). A command hook's `t` includes the start of the relay process (70 to 100 ms on the development computer, about 220 ms for the first one of a session); `relay.tCreated` is when Claude Code created the hook's process. Times that come from another process's wall clock (`relay.*`, `DBG`, the `timestamp` of a file entry) are right to about a millisecond; the debug log stamps a line when it writes it, to the millisecond.
- **The debug file changes the screen.** With `debugFile` true the banner has a row more, `Debug mode enabled · logging to <path>`. Hooks themselves show on the screen: while synchronous hooks run, the spinner row reads `( running Stop hooks… 1/2 )`.
- **The model is fed at once.** The window feeds its model through a timer on the user interface thread and adds timers of its own ("Done" 400 ms after the bell, held speech and so on): the `APP` rows are the model's state, not the window's announcements. The 100 ms quiet frame is checked every 20 ms, so it comes 100 to 120 ms after the last output.
- **`message` holds 1.5 seconds** for the dropped-Enter rule unless `retryEnter` is false.
- **`waitRaw` sees escape sequences.** Text that looks adjacent on the screen is often not adjacent in the bytes. Prefer `waitFor`.
- **Session files.** Only the project folder that belongs to `cwd` is followed (`<Claude's configuration folder>\projects\<cwd with every other character a hyphen>`), with everything under it, and any file a hook names in `transcript_path` or `agent_transcript_path`. A file that was there before the run is followed from its end. The session file does not exist until the first message is sent.
- **What a run leaves behind.** The session's files stay in Claude Code's projects folder, and Claude Code records the trust answer for the scratch folder in its own configuration. The probe deletes nothing there.
- **Ending.** The probe ends the process tree it started, by the process ids it recorded, and nothing else. If the probe itself is killed from outside, the pseudo console should close with it and Claude Code end (not tested): give `maxRunMs` a value below your tool's own time limit so that the probe ends the session itself.
- **Not run against Claude Code on 2026-11-15:** `commandForm` other than `exec`, `closedPort`, `replies`, `timeoutSeconds`, `async`, `matchers`, `extra`, `delayMs` (the delay and a reply were checked with batch-file runs only), `end` `close` and `kill`, `--dbg` on anything but the validation run. `docs/axClaude/events-and-grounding.md` section 4.3 says what was measured of `async` since.
- **Costs.** A command hook is two processes each time (the relay and a console host). With `both`, every event is two requests, and Claude Code waits for the slower one.

## 8. Making a timeline fixture

`fixture <output folder> <fixture folder>` makes one recorded run into a timeline fixture, `tests/fixtures/timelines/<name>/` (EG-11.1; the files in section 6.6 of the specification and in `tests/fixtures/timelines/README.md`). It is the whitelist that makes a fixture safe to commit (`FixtureMaker.cs`):

1. **Events** (`events.jsonl`): only the eight events axClaude registers (EG-2.1: SessionStart, Stop, StopFailure, SubagentStart, SubagentStop, PermissionRequest, Notification, SessionEnd), each with `hook_event_name`, `session_id`, `prompt_id` and its own fields of EG-2.1 alone (a listed task with `id`, `type`, `status`, `description`, `agent_type`). Of a pair of the same event by both transports (equal once reduced, within 2 s) the one by command stays, the transport axClaude uses, with its time.
2. **Entries** (`entries.jsonl`): every line the session files gained in the run, in file order, each with the poll's time and its session's id (the file's name: a run with `/clear` has two). A session file is a `.jsonl` directly in the project folder; subagents' and tool results' files are left out. The kinds of EG-3.4 keep their fields of EG-3.4 alone; every other line is `{"type":"<its type>"}`, and a line without a type, or no JSON object, `{}`. A string that starts with `<` (a command, a shell line, a task notification: Claude Code's own wrapping) keeps its first tag alone, and the content of an `isMeta` or `isCompactSummary` entry is empty: the reader decides by the tag and the flag.
3. **Sends** (`sends.jsonl`): what the script wrote. A message as `message` with its text (its Enter, and the window's Enter again, are part of it); Ctrl+X followed by Ctrl+S as the key `SendNow`; Escape as the key `Escape`; every other key by the name the script gave it; a text written without Enter (a `send` step, `/exit` of an `end` step, a `type` step's characters) as `text`; a `bytes` step as `bytes`.
4. **The console stream** (`pty.vt`, `pty.vt.chunks.txt`): the bytes and the index, the index unchanged.
5. **Masking**, in the console bytes and in every kept string, length for length, so that no position in the stream moves: each whole-word occurrence of the account's, the computer's and the domain's name (`Redaction.DefaultNames`, the distinctive ones) becomes as many `x` as it has bytes, and a `Users` with one separator right in front of a masked name becomes `xxxxx`. In the stream an escape sequence counts as a word's edge (text in bold follows one directly) and a title's text as text.
6. **meta.json**: Claude Code's version, the console's size, the day of the recording, the run it came from (`r1/a` for a run folder `r1\a-out`) and `turns`, the turns that finished (the session file's `turn_duration` entries).
7. **The check**: every file written is read back; when `Redaction.Find` finds an e-mail address or an account path in one, or one of the masked names is still anywhere in it (the console stream also with its escape sequences taken out), the fixture is refused, what was written is deleted, and the tool says in numbers which file held what. Exit code 0 written, 4 refused, 2 not a run folder.

It prints counts and fixed words, never a text of the run. `expected.txt` is not its: the replayer's tests write it (`AXCLAUDE_UPDATE_EXPECTED=1`), and a remade fixture keeps it. A fixture is never edited by hand; a run the tool refuses is recorded again from a neutral folder (`C:\Temp\axclaude-fixture`), with nothing in its script that prints the account (`/status`, `/usage`, `/cost`).

## 9. The stream probe

`probe stream` is a second measurement, of Claude Code's non-interactive stream and with no pseudo console
(`StreamProbe.cs`; the findings are `docs/axClaude/stream.md`). It plays a fixed set of cases, each a process started
with `ClaudeCodeStream.Options`: messages are written on standard input, every line of standard output is read
through `ClaudeCodeStream.Read`, and what a case expects is held to the lines.

- It prints kinds, names and times only. `--keep <folder>` copies the raw lines there for the developer's eyes; they
  hold what Claude wrote and the session's ids, and are never committed.
- It saves no session, loads no MCP server but its own stand-in permission tool (`probe permission-tool`, started by
  Claude Code, answering as the case says), works in a folder of its own under the temporary folder and deletes it.
- It uses the smallest model, so its cost is a few short turns.
- `--shape <file>` writes the keys of every kind of line seen. The repository's copy is
  `docs/axClaude/stream-shape.json`, written by `tools\upstream.ps1 -Verify claude`.
- A new case is a `Case` in `StreamProbe.cs`: its options, its steps, what must hold, and what is only reported. What
  the model may or may not do (follow an instruction, hand work to a subagent) is reported, not required. A case's
  `Files` (2026-11-25) are written into its own folder before its process starts, each by its path under the folder
  and its text: a custom command (`.claude/commands/<name>.md`, the recording `custom-command`) or a folder's
  settings; the folder is deleted with the run.
- Two cases measure the commands of Claude Code's screen alone (2026-11-26, docs/axClaude/stream-mode.md SM-5.8):
  `command-lists` says, for each command `stream-equivalence.md` names, whether the first request's answer and the
  start line list it (only names the probe gives are asked about: the lists hold the user's own commands);
  `screen-commands` writes those the lists did not name, one after another, and says how each came back, Claude Code's
  own words through the redaction when no turn of the model's ran. Never `/status`, `/usage`, `/cost`, `/login` or
  `/logout`, and not a command that could reach an editor, a browser or the clipboard if the stream ran it.
- Four cases are of axKit's own relay (`relay`, `relay-refused`, `relay-refused-why`, `relay-gone`; `AskRelay`, the verb
  `AxKit.exe ask`); `relay-refused-why` refuses with the user's words (`ClaudeCodeStream.RefusedWithWords`) and checks
  that Claude follows them.
  Claude Code is given the relay's own configuration and the pipe's name and secret in its environment, as a window
  will give them, and starts this program with the verb `ask`, which runs the same code as `AxKit.exe ask`; the probe
  plays the window's end of the pipe on a thread of its own and answers as the case says, or nobody does.
- Five cases of 2026-11-26 (docs/axClaude/stream-mode.md SM-4.11, SM-7.7): `tool-output` says in numbers what two
  commands' results hold; `plan-manual`, `plan-edits`, `plan-auto` and `plan-keep` set the mode to plan, ask for a plan
  shown with the plan tool, and say which tools the permission tool was asked about, the keys of the plan's input, the
  modes the stream named after it and which files were made. The stand-in permission tool answers a plan as the case
  says (`ClaudeCodeStream.AllowWithMode` with `default`, `acceptEdits` or `auto`; `plan-keep` sends the first plan back
  with `KeepPlanning` and approves the next), and every other tool of such a case yes.
- `--record <folder>` (2026-11-17) plays another set of cases, the recordings (`Recordings` in `StreamProbe.cs`), and
  writes each as a stream fixture, `<case>.jsonl`: every line Claude Code wrote and what the host wrote between them,
  on one clock (docs/axClaude/stream-mode.md SM-10.1; `tests/fixtures/streams/README.md` says what a file holds and
  what the recordings showed). They are played with what axClaude's window will start Claude Code with: the stream's
  own options, each message sent back when it is taken, and the permission tool for every question. The maker,
  `StreamFixtureMaker.cs`, is the whitelist, as `FixtureMaker.cs` is for a timeline: a line is reduced to the fields
  named for its kind, ids become numbers, the case's folder becomes `C:\work`, a tool's result is kept only when it
  failed, every kept string is masked, and a file that still holds an address, an account path or a name is refused
  and deleted. The raw lines are never written to a file. It prints counts and, once at the end, the names of the
  keys the fixtures do not keep, so that a field the model turns out to need is added by name. The repository's
  fixtures are recorded with `--record tests\fixtures\streams`; read each through before it is committed. Since
  2026-11-25 the maker also keeps the start line's model and, of the first request's answer, the models with their
  names, words and efforts (`Models`), and counts the dropped keys of the start line, a user line and that answer too.
  Since 2026-11-26 a recording whose `KeepsOutput` is set (`tool-output`: commands whose output the case chose) keeps
  a result's text also when the call went well, and an id quoted inside a kept text becomes a number as the fields'
  ids do (a plan file is named after its session; the recording `plan`).