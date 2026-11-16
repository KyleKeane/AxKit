# axClaude: events and grounding

The specification of how axClaude learns what happens in a Claude Code session and how the conversation it shows is tied to Claude Code's own record. It belongs to `docs/axClaude/SPEC.md` ("SPEC.md" below) and amends it where section 9 says so. Requirement ids are EG-n.m; decisions are EG-D-n.

Status: written on 2026-11-15, at the user's request of that day, by the assistant, on decisions the assistant took on his behalf for him to confirm (section 8). Not yet built when written; section 10 is the build plan. Measured against Claude Code 2.1.325.

## 1. Summary

axClaude runs Claude Code in a pseudo console in screen reader mode. Until now it derived everything from the text Claude Code draws: whether Claude works, when a turn is done, what the conversation holds, what to speak. Text on a screen can be drawn again, folded, summarised or reordered, and it carries no identities. The user's words:

- "i thought that we were mirroring claude code cli? it seems like we are just making our own history?"
- "the spoken flow is getting really messed up. i feel like this needs to become event driven or something"
- on 2026-11-15 he asked for "the redesign of the axclaude conversation and claude code hooks for event driven management and the claude logs for grounding".

The faults this ends, each observed:

1. Old conversation and old questions spoken again after Claude Code repainted its screen.
2. A live progress row spoken on every update.
3. Announcements that arrive late and cut into what he has moved on to.
4. "Claude: ready" in the status bar while a turn runs.
5. A reply replaced in the conversation by Claude Code's own one-paragraph summary.
6. A message sent during a reply standing inside that reply.
7. "Done" said by a guess: the bell and a pause of 400 ms.

The design. axClaude takes four sources, each for what it does best:

- **The marks** that travel in the console stream with the text: the start and the end of a turn and the bell. They cost nothing and their order against the text is exact.
- **Claude Code's session file**: what was said, by whom, in what order, with identities and the Markdown source.
- **Hooks**: what neither gives. The session's file at a start and after `/clear`, the background tasks at each stop, agents starting and stopping, a permission request, the session's end.
- **The screen**: everything live and interactive, and the text the user reads. As today.

One pure class, the core, takes all of it as plain records in arrival order and returns decisions. The window carries them out. Each fact has a ranked list of evidence, so that a missing source (an older Claude Code, hooks switched off, a file that cannot be read, another program one day) leaves the window working as it does today.

What the user will notice:

- The status bar says "working" from the moment Claude takes a message up until the turn ends, also when Claude Code's screen shows no sign of work.
- "Done" comes when the turn has ended and its last reply is in the conversation, right after the reply's speech when replies are spoken, without the fixed pause.
- With Speak replies on, a reply is spoken once, in the record's order. A repaint, a replay, Claude Code's summary of an earlier reply, a progress row and a question's own rows are not spoken as replies.
- Speech that waited is dropped when he has moved on (a message sent, an answer given, a question opened). "Done" and questions are never dropped.
- A message sent while a reply is still arriving goes after that reply.
- A reply that Claude Code later replaces by its own summary stays in the conversation as it was said.
- Headings in a reply get their true levels.
- Options, Developer has two new switches and one command that says where axClaude's events come from. Session has an export of Claude Code's own record.

What does not change: the words (Ready, Sent, Queued, Responding, Done, Claude asks, Question closed), the keys, the sounds, the exchange blocks, the control area, the settings and their defaults.

## 2. Goals and non-goals

Goals:

- G1. The state of the session (working, a turn ended, a question, an interrupt) comes from Claude Code's own signals where they exist, and from the screen only as the last fallback.
- G2. Nothing is spoken twice, nothing old is spoken, and speech keeps the record's order.
- G3. The conversation keeps what was said, in the order it was said, whatever Claude Code later does to its screen.
- G4. What the user hears and presses stays as it was (root `CLAUDE.md` rule 12).
- G5. Every piece works without the others: no hooks, no session file, no marks. A second agent program fills the same places or none (rule 12).
- G6. Observing changes nothing in what Claude Code does: a hook never blocks, approves, decides or answers.
- G7. Nothing of the conversation's text leaves memory: not into a log, a report or the diagnostics.

Non-goals:

- **No listener.** Not TCP, not HTTP, not on localhost (`docs/axKit/mcp.md` section 1). A hook is a program start and a named pipe.
- **No change to what Claude Code does.** No hook in the user's or a project's settings files; no hook that returns a decision; no setting of Claude Code's changed.
- **No wholesale re-rendering of the conversation from the file.** Rewriting a turn's lines at its end would move text under the reader's caret at the moment he reads it. The file confirms, binds, pins and orders; the lines the user reads are the screen's lines.
- **Questions, pickers and the prompt stay the screen's.** Their text, their answers and their keys are read off the screen as today (SPEC.md D33). An event only says that one is coming.
- No structured live text from hooks, no hook for every tool call or every submitted prompt (section 11).
- No new sound. A new sound is chosen by ear first.

## 3. Words

- **Mark**: one of the shell integration sequences in Claude Code's console stream, OSC 133 with the letter A (a turn starts), C or D (a turn ends), and the bell (BEL). `AxKit.Core/Vt/Screen` parses them.
- **Hook**: a command Claude Code runs when something happens in a session, registered in a settings file. axClaude passes its own file for each start.
- **Event**: what a hook delivers: one JSON object, named by its `hook_event_name`.
- **The session file**: the file Claude Code writes for a session, one JSON object for each line, under its `projects` folder. Claude Code calls it the transcript; this document says session file, since "the transcript" is the conversation in axClaude's code.
- **Entry**: one line of the session file.
- **Turn**: from the moment Claude takes a prompt up (typed, queued, or one Claude Code starts by itself) to the moment it hands back.
- **The core**: the class `SessionFlow` in `AxClaude.Core`. It owns what happened.
- **Decision**: a plain record the core returns: say Done, speak these lines, the state changed.
- **Generation**: a number in the core that every action of the user and every opened question raises. What was held under an older generation is dropped unspoken.
- **Bind**: tie an entry of the session file to the lines of the conversation that show it.
- **Pin**: keep a bound reply's lines as they were said when Claude Code later redraws them as something else.
- **The gate**: the part of the core that holds each reply line the screen proposes until the record confirms it, and releases it once.
- **The record**: the session file as the core sees it, entry by entry.
- **Head**: the first row of a reply on the screen, the `claude:` row. The rows under it are its continuation rows.

## 4. What was measured

On 2026-11-15, with Claude Code 2.1.325 in screen reader mode at 240 by 50, a probe recorded the console stream, every hook event and every line of the session file on one clock, in scratch folders, with the model Haiku. The reports are M11 (the probe), M12 (the anatomy of a turn), M13 (queued messages, interrupts, questions, background work), M14 (the session's life, the transports) and M15 (the session file's format). They are not in the repository; what they established is restated here. The probe is `tools/ClaudeProbe` (EG-11.4). Times are milliseconds.

### 4.1 The moments of a turn

| Moment | Marks in the stream | Hooks axClaude registers | Session file | Screen | Report |
| :-- | :-- | :-- | :-- | :-- | :-- |
| A message sent at the idle prompt | A, 14 to 62 after Enter | none | `user` entry, `promptSource` typed, 125 to 373 after Enter | the `you:` echo row 47 to 141 after A | M12 2, 8 |
| A message sent while a turn runs | none | none | `queue-operation` enqueue with the text, 112 to 120 later | a `you:` row over `ctrl+x ctrl+s to send now` inside the working block | M13 3.1 |
| A queued message taken up | A, 45 to 53 after the bell of the turn before | none | `queue-operation` dequeue, then `user` with `promptSource` queued, about 115 after that turn's Stop | the hint row goes | M13 3.1 |
| Two queued messages | one A | none | two dequeues, two `user` entries, one turn, one reply | one hint row for both | M13 3.2 |
| Claude works | none | none | nothing | the spinner row, `esc to interrupt` on the mode line | M12 4 |
| A reply is streaming | none | none | nothing | nothing: in screen reader mode a message is printed whole when it is complete | M12 4 |
| Text before a tool call | none | none | the text entry and the tool call's entry together, 68 to 82 after the rows | the rows in one chunk; the text can be inserted above tool rows drawn 0.4 s earlier | M12 6 |
| A tool's question opens | a bell alone, no C or D | PermissionRequest 9 to 13 before the bell; Notification 6 s later if still open | the tool call's entry; no entry for the question | the question's rows 19 to 38 after PermissionRequest | M12 3.2, M13 5 |
| A turn ends | C and D in one write, the bell 0.4 to 53 after D | Stop 12 to 130 before D, with the last message's text and the background tasks | the last reply's entry 51 to 190 after the bell, then `system` `turn_duration` | the last message's rows from 7 before to 117 after the bell; the turn summary row | M12 9 |
| An interrupt (Escape) | C and D, no bell, 62 to 111 after the key | none at all | a cut `assistant` entry (`isAbortedMidStream`), then a `user` entry `[Request interrupted by user]`, 198 to 244 after the key | `Interrupted  · What should Claude do instead?` | M13 4 |
| Send now (Ctrl+X Ctrl+S) | C, D and the bell 31 after the chord, then A | no Stop for the cut turn | the cut entry, a dequeue, the queued `user` entry | the hint row goes; no summary row | M13 3.3 |
| A hand-back note with no turn | A, C and D within 1, no bell | none | a `user` entry with `queueTranscriptOnly` | ` Agent "..." finished` | M13 7 |
| A command Claude Code handles itself (`/exit`, `/clear`; `/usage` at its Escape) | A, C, D and the bell together | SessionEnd, SessionStart for `/clear` | local command entries | the echo `you: /...` | M12 7, M14 6 |
| A `!` shell line | A when the model's turn after it starts | Stop at that turn's end | two `user` entries in `<bash-input>` and `<bash-stdout>` tags | the `!` rows and the output | M12 7.1 |
| A background shell | the marks of its turns | Stop lists it while it runs; its end is a turn Claude Code starts by itself | enqueue of a `<task-notification>`, a `user` entry with `promptSource` system | the mode line counts `1 shell`; a completed row | M13 6 |
| A background agent | the marks of its turns | SubagentStart; Stop lists it; SubagentStop | the same, with an `<agent-message>` | its panel row, the waiting row; not counted on the mode line | M13 7 |
| A session starts | none | SessionStart (source `startup`), after the trust answer, 0.8 to 2.6 s after the prompt is ready | the file does not exist until the first entry | the banner, the mode line | M14 3 |
| `--continue`, `--resume` | none | SessionStart (source `resume`), after the redraw | the same file, appended | the whole old conversation in one frame, before SessionStart, with no event | M14 5 |
| `/clear` | a bell | SessionEnd (reason `clear`), SessionStart (source `clear`) with a new id and file | a new file at once | `you: /clear` | M14 6 |
| `/compact` | a bell at its end | SubagentStop with an empty agent type; SessionStart (source `compact`) | the same file: `system` `compact_boundary`, a summary `user` entry | "Conversation compacted" rows | M14 7 |
| A session ends | none | SessionEnd, cancelled by Claude Code after about 1.5 s | the exit entries | `Resume this session with:` | M14 5 |

### 4.2 Telling the ends apart

From the marks alone, in stream order:

| What arrives | What it is | What axClaude does |
| :-- | :-- | :-- |
| C, D, then the bell within 150 | a turn finished, or a command Claude Code handled itself | the turn is closed; Done (EG-5.6) |
| C, D, and no bell within 150 | an interrupt, or a note with no turn behind it | the turn is closed; nothing is said |
| C, D and the bell within 2 s of the user's Send now | the running reply was cut for the queued message | the turn is closed; nothing is said; the next A opens the queued turn |
| a bell with no D in the 150 before it | a question from a tool | the window's question flow, as today |
| A | Claude took a prompt up | a turn is open |
| A, C, D within 150 with no bell | a hand-back note | nothing: no turn was open for longer than the note |

The other sources confirm: the Stop event says a turn finished (never an interrupt, never a cut); the file's `turn_duration` says it finished; `isAbortedMidStream` says it was cut.

### 4.3 What else was established

- The order at a turn's end is always: Stop, then C and D, then the bell, then the reply's rows, then the reply's entry, then `turn_duration`. Once in 16 turns the rows came 7 before the marks (M12 9).
- The session file is append only, UTF-8 without a mark, LF, one object for each line. No line was seen half written in more than 3,000 lines. About half its lines are not conversation. Lines reach 533 KB. Timestamps go backwards at times: order is file order (M15 1, 3).
- One reply is one entry for each content block: a text block, a tool call, a thinking block. The blocks of one response arrive together (M15 1.11).
- Identities: every entry has a `uuid`; a turn's `user` entries carry a `promptId`; a tool call's `id` joins it to its result (M15 3.6). The screen has none.
- The screen's text of a reply is the Markdown source with rules applied: `#`, `>`, code fences and the inline marks removed; `- ` and `1. ` kept; `[text](url)` drawn as `text (url)`, once when both are equal; a table drawn as one row for each body row, `Header: cell. Header: cell.`; wrapped at the console width at a space (M12 5). Heading levels cannot be read off the screen.
- The session id and the file stay the same through `--continue`, `--resume` and `/compact`. `/clear` and `--fork-session` start a new id and a new file, named by SessionStart. `--session-id` names the file (M14 4 to 7).
- Hooks passed with `--settings` run in interactive screen reader mode; none runs before the workspace trust question is answered; `/hooks` never lists them; the file is read once at the start; a project's `disableAllHooks` stops them too (M14 3, 11, 13).
- SessionStart takes command hooks only. A hook on PreCompact or PostCompact prints a "completed successfully" row with its command line into the conversation: they are not registered. An event name Claude Code does not know raises a blocking "Settings Warning" question at the start (M14 7, 10).
- A command hook in exec form (no shell) is the program and a console host. Run synchronously it cost Claude Code 107 to 450 ms, and Claude Code waits for it: the turn's marks come after it, and the spinner row reads `( running Stop hook )` meanwhile, which the spinner pattern already covers. A Stop hook leaves a `system` `stop_hook_summary` entry in the session file for each turn (M11 6, 7; M14 8).
- An interrupt fires no hook. Slash commands and `!` lines fire no prompt hook. `/btw` leaves nothing in the file (M12 7, M13 4).
- Whether `async: true` is accepted on each event axClaude registers, and whether an async hook shows anything on the screen, was not measured when this was written. Work package WP-A3 measures it and writes the result here:

  > Measured on 2026-11-15 by WP-A3: Claude Code 2.1.325 in screen reader mode, the model Haiku, two sessions one after the other in scratch folders, through the probe without a debug file. The eight events of EG-2.1 as command hooks in exec form with `timeout` 5, in the first session all with `"async": true`, in the second none; in each the trust answer, "Reply with the single word ok." twice, `/exit`.
  >
  > 1. No "Settings Warning" in either session: the file with `async` on all eight was taken at the start. SessionStart and both Stops arrived in both. SessionEnd arrived without `async` (245 after its process was created; Claude Code ended 538 after that) and never with it: its process was created 674 before Claude Code ended, and its event had not come 2 s after the end. StopFailure, the subagent events, PermissionRequest and Notification did not fire in these turns.
  > 2. No row of the conversation named a hook in either session (no `completed` row, no line). The spinner row, an interface row, read `( running Stop hook )` in one frame of the async session's second turn and not in its first, and in two frames of each turn of the synchronous session.
  > 3. From the Stop hook's process being created to OSC 133 D: with `async` 40 and 17, without it 358 and 254 (median 306). With `async` the Stop event arrived 424 and 524 after D, after the bell, the reply's entry and `turn_duration` in the session file; without it, 31 and 27 before D. The times of the events in 4.1 were taken with hooks Claude Code waited for; PermissionRequest's time with `async` was not measured. A hook's process took 227 to 692 from its creation to its event at the probe, on a computer shared with other builds.
  > 4. Yes: one `system` `stop_hook_summary` entry for each turn with `async`, as without it.
  >
  > So SessionEnd is registered without `async` and the other seven with it (EG-2.5).

### 4.4 What the file holds that is never read out or copied

- `session-owner` entries and the `credential_org` attachment: account and organisation identifiers.
- The `session_context` attachment: the user's e-mail address.
- `api-request-blob`, `api-request-shape`, `api-request` and the `prompt_snapshot` attachment: the system prompt, every tool's definition and copies of what was sent to the model.
- Tool results and `cwd` fields: paths that name the Windows account.

The reader skips every kind it does not need by its `type`, before any of its text becomes a string (EG-3.4).

## 5. Requirements

Constants are named here once and used by name afterwards. All times are milliseconds on the window's clock (`Environment.TickCount64`).

| Constant | Value | What it is |
| :-- | --: | :-- |
| `BellAfterEndMs` | 150 | how long after D a bell still belongs to it (measured: at most 53) |
| `QuietAfterEndMs` | 400 | after a turn closed by its marks, the screen's sign of work does not open a new one for this long |
| `IdleContradictsMs` | 2000 | a turn opened by A alone is closed when the screen has shown Claude idle for this long |
| `CutAfterSendNowMs` | 2000 | a turn's end this soon after the user's Send now is a cut, not a finish |
| `FailedCloseMs` | 1000 | a failed turn (StopFailure) is closed after this long when no marks close it |
| `GateCapMs` | 500 | how long a proposed reply line waits for the record |
| `UnconfirmedHoldMs` | 250 | how long a proposed reply line waits when there is no record (today's hold) |
| `DoneNoRecordMs` | 300 | the end counts as confirmed this long after the bell when neither Stop nor the record confirms it |
| `DoneCapMs` | 600 | Done is said at the latest this long after the bell |

### EG-1 The marks as events of the model

- EG-1.1 `SessionModel` listens to the screen's `Mark` event (OSC 133) and raises its own event `Marked` with the letter: `A`, `C` or `D`. Other letters are ignored. The event is raised inside `Feed`, in stream order: before the frame end that follows the mark in the stream and after the one that precedes it. The bell keeps its event, `LineStore.Bell`, raised the same way.
- EG-1.2 `SessionModel.Work` says what the last frame showed of Claude's work: `Working` (a spinner row or `esc to interrupt`, as `Working` today), `Idle` (a mode line was read and neither is there), `Unknown` (no mode line was read in that frame). `SessionModel.Working` stays as it is.
- EG-1.3 `SessionModel.Epoch` is a number that rises whenever the conversation or the run starts afresh: in `StartRun` (a start, a restart, New session, a folder change) and in `ClearConversation` (`/clear`). The core forgets its turn and its gate when the number changes (EG-4.9).
- EG-1.4 `SessionModel.TurnOpen` is a `bool?` the window sets from the core. While it is null the model decides from the screen, as today. While it has a value, that value takes `Working`'s place in three rules of the model: a sent message's block is held (`Send`), held blocks without an echo are placed (`PlaceBlocksWithoutEcho`), and a pending send is forgotten after 30 idle minutes (`CompleteFrame`).
- EG-1.5 Marks, bells and frame ends reach the window on the UI thread in the order of the stream, since all three are raised inside `Feed`. Each becomes one input of the core (EG-4.2), queued in that order.
- EG-1.6 With `TurnOpen` null, nothing the model produces changes: every golden file under `tests/fixtures` stays as it is.

### EG-2 Hooks

- EG-2.1 **The table.** `ClaudeCodeHooks.Events` is the one list of the events axClaude registers, each with the Claude Code version it was verified with. No other event is registered.

  | Event | Verified with | What axClaude takes from it |
  | :-- | :-- | :-- |
  | SessionStart | 2.1.325 | `session_id`, `transcript_path`, `source` |
  | Stop | 2.1.325 | `last_assistant_message`, `background_tasks` (`id`, `type`, `status`, `description`, `agent_type`) |
  | StopFailure | 2.1.325 | that it happened |
  | SubagentStart | 2.1.325 | `agent_id`, `agent_type` |
  | SubagentStop | 2.1.325 | `agent_id`, `agent_type` |
  | PermissionRequest | 2.1.325 | `tool_name` |
  | Notification | 2.1.325 | `notification_type` |
  | SessionEnd | 2.1.325 | `reason` |

  From every event also `hook_event_name`, `session_id` and `prompt_id`. Not registered, with the reason: PreCompact and PostCompact (they print rows into the conversation); the events for every tool call, every displayed piece of text and every submitted prompt (a process for each; the marks and the file give the same); Setup and the rest (nothing axClaude needs).
- EG-2.2 **The settings file.** One file serves every window: `%LOCALAPPDATA%\AxKit\claude-hooks.json` (`AppPaths.ClaudeHooksFile`, beside `claude-mcp.json`; a test run's own folder in a test run). It holds a `hooks` object with one entry for each event of the table: one command hook in exec form, the program `PackageIdentity.ProgramPath` (the Store copy's alias, the installed file, or a development build) with the one argument `hook` (section 6.1). axClaude writes it before a start when it is missing or its text is not exactly what this program would write (`ClaudeCodeHooks.WriteSettings`), as it does for the AI tools' file. It is never written into the user's or a project's settings.
- EG-2.3 **Passing it.** At every start of Claude Code (also with `--continue` and `--resume`, which do not restore it) axClaude adds `--settings <the file>` right after `--ax-screen-reader`, before the user's own arguments and before `--mcp-config`, which stays last. Not passed, and the window works without hooks, when: the switch is off (EG-9.1); the gate of EG-2.4 says no; the user's own arguments hold `--settings` (Claude Code would get two); the file could not be written; or npm's `claude.cmd` would refuse the line because of the file's path (the line is then built without it, as for the AI tools). Each case is one line in the log and one word in the diagnostics (EG-9.4).
- EG-2.4 **The version gate.** An event name Claude Code does not know stops its start with a question. So the file is passed only when the installed Claude Code's version, the first three numbers of what `claude --version` prints (SPEC.md FR-13.2), is at least the highest "verified with" of the table (`ClaudeCodeHooks.Supports`). The version is asked once for each program file in a run of the app. At the first start the window waits for the answer, at most 2 s, before it starts Claude Code; with no answer by then that start goes without hooks. A later start in the same window uses the answer. Below the table's version, or with no answer: no hooks, and the window works from the marks, the file and the screen.
- EG-2.5 **Never waited for.** Each hook carries `"async": true`, so that Claude Code does not wait for it and its marks are not delayed, and `"timeout": 5`, but SessionEnd's: the trial of WP-A3 (4.3) found that an async SessionEnd never arrives, since Claude Code ends before the hook has delivered it, so SessionEnd is registered without `async`, and Claude Code waits for it at its end (about 1.5 s at most, M14), when no turn is delayed. `ClaudeCodeHooks.Events` says so for each event. The trial found no row of the conversation for an async hook. The spinner row's note `( running Stop hook )` came for an async Stop hook too, in one turn of two, as it comes for a synchronous one in every turn; it stays an interface row through the existing spinner pattern, and Stop keeps `async`: without it the note would still come, and each turn's end would wait 254 to 358 ms for the hook. With `async` the Stop event comes about half a second after D, after the reply's entry and `turn_duration` in the session file (4.3). A synchronous Stop hook cost 306 ms at the median in the trial, under the 500 ms of EG-D-9, so the switch of EG-9.1 starts on.
- EG-2.6 **The verb.** `AxKit.exe hook` is no app and opens no window. It is decided first in `Program.Main`, before anything else runs. It reads standard input to its end (at most 1 MB), takes the pipe's name and secret from the environment variable of EG-2.8, connects (at most 500 ms), writes one frame (section 6.2), and ends. It prints nothing, shows nothing and exits with code 0 whatever happens: a variable that is missing, a window that is gone, a pipe that refuses, input that is too long. A watchdog ends it with code 0 after 2 s. A hook must never put an error row on Claude Code's screen.
- EG-2.7 **The pipe.** While Claude Code runs with hooks, the window serves one named pipe, `AxKit-hook-<the user's SID>-<16 random hex digits>`, inbound only, for the current user alone and refused over the network (`UserPipe.Security`). A new name and a new secret for each start of Claude Code; the pipe closes when that Claude Code is stopped. One connection is one event: the client's first line must be the secret (64 hex digits, compared in constant time), or the connection is dropped and counted. A body over 1 MB is dropped and counted. The pipe's thread never touches the window: it posts each event to the UI thread, one post for a burst, and a post from a Claude Code that is no longer the window's is dropped. An exception never leaves the thread. The client is not asked which program file it runs: the secret is the check (EG-D-6).
- EG-2.8 **The environment variable.** `ClaudeLauncher.BuildEnvironment` puts `AXKIT_HOOK_PIPE=<pipe name>;<secret>` into the environment of the Claude Code it starts, which a hook's process inherits. A value inherited from an outer session is always removed first, so that a window started from inside another axClaude never reports to that one.
- EG-2.9 **What an event is and is not.** An event is a signal, no more trusted than a row on the screen: any program Claude Code runs could send one. So:
  - nothing an event carries is ever run, sent to Claude Code, typed, shown as a question's text or written to a file;
  - `transcript_path` is used only when it names a `.jsonl` file under a projects folder of EG-3.1; otherwise it is ignored and counted;
  - `last_assistant_message` is used only as a key to confirm reply lines (EG-5.3) and is never shown, spoken or logged;
  - once a SessionStart has named a session, an event of another session id is counted and ignored, but for a SessionStart itself;
  - SubagentStart and SubagentStop with an empty `agent_type` are Claude Code's own helpers (a side question, compaction) and are ignored;
  - a hook returns nothing, so it decides nothing: no permission is granted or denied, no turn is continued, no prompt is changed.
- EG-2.10 **No events.** When the file was passed and no event has arrived by the end of the first turn, the window writes one line in the log, the diagnostics say "passed, none received", and the session goes on without hooks. The core never waits for an event that has not come: an event only adds evidence. The same holds when a project's `disableAllHooks` stops them.
- EG-2.11 **The Store copy.** A packaged axKit's new files under AppData are private to the package (bundle AX-13.5), and Claude Code runs outside it, so Claude Code may not see the settings file at the path given. Until that is confirmed by use, the Store copy passes no hooks: `ClaudeCodeHooks.Supports` is not asked when `PackageIdentity.IsPackaged`. The AI tools' file has the same open point (axHelp HE-2.8); both are settled together (section 11).
- EG-2.12 Nothing of this needs a question or a consent of its own: observing changes nothing Claude Code does, and the consent to Claude Code (bundle AX-11.4) covers starting it.

### EG-3 The session file's reader

- EG-3.1 **Where session files are.** A projects folder is `<configuration folder>\projects`, the configuration folder being `CLAUDE_CONFIG_DIR` of the child's environment when it is a full path, else `%USERPROFILE%\.claude` (`ClaudeCodeSettings.UserSettingsFile` resolves the same folder). In a test run `<the run's folder>\.claude\projects` counts as one too, so that a stand-in's files never land in the user's. A session's file is `<projects folder>\<project>\<session id>.jsonl`, `<project>` being the working folder's full path with every character that is not an ASCII letter or digit replaced by `-` (`ClaudeCodeTranscript.ProjectFolderName`). Only files directly in the project folder are sessions; `subagents`, `tool-results` and `memory` under it are never opened.
- EG-3.2 **Finding the file**, in this order of evidence:
  1. The SessionStart event's `transcript_path`, when it passes EG-2.9. A later SessionStart (source `clear`, `fork`) names the new file, and the reader moves to it.
  2. Without it: the folder rule. At each start of Claude Code the window notes the session files of the project folder with their lengths. Once this window has sent a message, the reader looks every 250 ms for files there that grew or are new, reads what they gained, and takes the file whose new entries hold a prompt (EG-3.4, typed or queued) whose key equals the key of a message this window sent in this run. With no such file within 10 s of the send, the search stops until the next message is sent.
  Until a file is found there is no record: the gate works without it (EG-5.4).
- EG-3.3 **Reading.** The file is opened shared for reading, writing and deleting, read from the length it had when Claude Code was started (0 for a file that did not exist; from its end when it is found without that note), in bytes, split at LF, each line decoded as UTF-8. Bytes after the last LF wait for their LF. A line longer than 4 MB is passed over to its LF and counted. A file shorter than what was read (replaced or set aside) is read again from 0, and entries already seen are dropped by their `uuid`. The reader polls: every 50 ms while a turn is open and for 5 s after its end, every 500 ms otherwise. It runs on a thread of its own and posts entries to the UI thread in file order, one post for a burst.
- EG-3.4 **What is read.** Each line's top-level `type` is found first, skipping every other value untouched. Only these types are read further; every other type is skipped unread and counted, never copied, logged or shown:

  | `type` | Read as | From these fields only |
  | :-- | :-- | :-- |
  | `user`, content a string, not `isMeta`, not `isCompactSummary`, not starting with `<` | Prompt | `uuid`, `promptId`, `message.content`, `promptSource` (`typed`, `queued`, `system`), `timestamp`, `isSidechain` |
  | `user` with `isCompactSummary` | CompactSummary | `uuid` |
  | `user` with `isMeta`, or content starting with `<command-name>`, `<local-command-`, `<bash-`, `<task-notification>`, `<agent-message` | Note (a command, a shell line, a notification; `promptSource` `system` makes it a Prompt Claude Code started itself) | `uuid`, `promptId`, `promptSource` |
  | `user`, content an array with a `tool_result` block | ToolResult | `uuid`, `promptId`, the block's `tool_use_id`, `is_error` |
  | `user`, content an array with a `text` block starting `[Request interrupted` | Interrupt | `uuid`, `promptId` |
  | `assistant`, content block `text` | ReplyText | `uuid`, `message.id`, the block's `text`, `message.stop_reason`, `isAbortedMidStream`, `timestamp` |
  | `assistant`, content block `tool_use` | ToolCall | `uuid`, `message.id`, the block's `id` and `name` |
  | `assistant`, content block `thinking` | Thinking | `uuid` |
  | `system`, `subtype` `turn_duration` | TurnEnd | `uuid`, `durationMs` |
  | `system`, `subtype` `compact_boundary` | CompactBoundary | `uuid` |
  | `system`, any other subtype | Other | `uuid` |
  | `queue-operation` | Enqueued, Dequeued, Removed by `operation` | `content` of an enqueue |

  A tool's input, a tool's result text, `cwd`, `gitBranch`, usage and every field not named here are never read. Entries with `isSidechain` true are counted and passed on as Other.
- EG-3.5 **Tolerance.** A missing field is its default (false, null, empty); a missing `uuid` gives the entry a local id from its offset; an unknown block type or subtype is Other; a line that is no JSON object is counted and skipped; property order does not matter; a `type` named in the table with an unknown shape is Other. Order is file order, never the timestamps.
- EG-3.6 **Switching itself off.** The reader stops for the session, with one line of numbers in the log, when: three lines in a row are no JSON; or, of the last 50 entries of the types it reads, more than 10 lacked what the table needs (no `message`, no content, no `uuid`); or the file cannot be opened for 2 s. From then on there is no record for this session (EG-5.4), and the diagnostics say why in one word. The next start of Claude Code tries again.
- EG-3.7 **The screen key.** To compare a reply's source with its rows, both are reduced to their letters and digits, lower case (`RecordText.Letters`). The source is first passed through the rules of 4.3 that change letters (`ClaudeCodeTranscript.ScreenKey`): code fence lines are dropped; a table's header and separator lines are dropped and each body row becomes its header and cell pairs in order; a link whose text equals its target counts once. Everything else needs no rule, since marks are no letters.
- EG-3.8 The reader's parsing is Claude Code's own place (`ClaudeCodeTranscript`, rule 12); what it returns is a neutral record (section 6.3).

### EG-4 The core

- EG-4.1 One plain class, `SessionFlow`, in `AxClaude.Core/Flow`. Pure: no window, no file, no clock, no thread of its own. One method takes one input and the time and returns the decisions it leads to: `Handle(FlowInput input, long nowMs)`. It is called on one thread, never from inside itself. No event bus, no interface, no subscription.
- EG-4.2 **Inputs**, plain records (section 6.4): the process started or ended; a mark; a bell; a frame ended (the screen's work, whether Claude asks, the reply lines the screen proposes, whether a visible line was added, the model's epoch, whether replies are spoken); the user acted (sent, answered, escaped, sent now, a key to a screen); a question was shown; a session was named (id, file, source); a turn stopped (the last message's key, the listed tasks) or failed; a worker started or stopped; attention was asked (a permission request, a notification); the session ended; an entry was read; the record was found or lost; a tick of the clock. None of them names Claude Code.
- EG-4.3 **State**: the turn (none, open, ending) with what opened it and when; the generation; the gate's pending lines, heads and reply records; the tasks listed at the last stop; which sources are alive in this run (marks seen, events seen, record bound); the queue count from the record; the model's epoch.
- EG-4.4 **Decisions**, plain records (section 6.5): the state changed; an attention bell (the window's question flow starts its settle); say Done; speak these reply lines; follow this session file; forget the record; an entry was bound to a head; the background list changed; wake me at this time; a note for the log (numbers only). Each carries the generation it was made in.
- EG-4.5 **Evidence.** Each fact has its sources in order; the first that speaks decides, the later ones confirm or stand in.

  | Fact | Evidence, in order | With none of it |
  | :-- | :-- | :-- |
  | A turn started | mark A; a Prompt entry in the record while no turn is open; the screen showing work (`Work` is `Working`) outside the quiet after an end | the screen alone, as today |
  | A turn ended, finished | D with its bell (4.2); confirmed by the Stop event and by the record's TurnEnd | the bell and the screen showing Claude idle, as today |
  | A turn ended, interrupted or cut | D without its bell; D with its bell after Send now; an Interrupt or a cut ReplyText in the record | the screen showing Claude idle |
  | A turn failed | the StopFailure event, then `FailedCloseMs` without marks | the screen |
  | Claude works | a turn is open | the spinner and `esc to interrupt`, as today |
  | A question is coming | a bell alone; the PermissionRequest event | the screen's own question, as today |
  | A question's text, answers and keys | the screen, always | |
  | Which file is the session's | EG-3.2 | no record |
  | A reply line is real and new | the record's ReplyText; the Stop event's last message until the entry arrives | today's rules inside the turn (EG-5.4) |
  | How many messages wait | the record's Enqueued minus Dequeued and Removed | the hint rows, as today |
  | What runs in the background | EG-7.3 | the rows and the mode line, as today |
- EG-4.6 **The turn.**
  - It opens at A. It opens at a Prompt entry (typed, queued or one Claude Code started) when none is open and none ended within `BellAfterEndMs`. It opens when the screen shows work and none is open, except during `QuietAfterEndMs` after a turn closed by its marks.
  - It closes at D. With a bell within `BellAfterEndMs` it finished and is ending (EG-5.6), unless Send now was pressed within `CutAfterSendNowMs`: then it was cut and nothing is said. Without that bell it was interrupted, or was a note, and nothing is said.
  - A turn opened by A alone closes, with a note for the log and nothing said, when the screen has shown `Idle` in every frame for `IdleContradictsMs` and neither an event nor an entry of that turn has come: an A can come with no turn behind it (4.2).
  - A turn opened by the screen alone (no marks in this run) closes when the screen shows `Idle`; a bell then gives Done by today's rule, through the window's settle. Such a turn stays "ending" for `DoneCapMs` after its close, so that its last rows are still inside it (EG-5.2); no Done comes from the core for it.
  - The record's TurnEnd closes a turn that is still open `BellAfterEndMs` after it was read (a missed mark).
  - The process's end closes everything; a Done not yet said is dropped.
  - Between a turn's end and the A of a queued turn the state is not "ready" when the record or the window says a message waits: the state stays working for `BellAfterEndMs` after such an end.
- EG-4.7 **Generations.** The generation rises when the user sends a message, answers a question, sends Escape, Send now or a key to a screen of Claude's, when a question or screen is shown in the control area, when a PermissionRequest event arrives, and when the window is put away. Moving in the conversation raises nothing. What rises it drops every line pending in the gate (EG-5.7).
- EG-4.8 **The queue and the threads.** The window keeps one queue of inputs. Everything that happens puts its input at the end: the model's events on the UI thread, the hook pipe's and the reader's posts when they arrive there, the window's own actions. One loop drains it: it takes an input, calls `Handle`, carries out each decision in order, and takes the next; a decision whose carrying out raises new inputs (a system line, a question's record) finds the loop running and only queues. So the core never runs nested, and decisions come in arrival order. Hook events and entries share the UI thread's message queue with the console's frames but not a clock: the core accepts every order of them (EG-11.2).
- EG-4.9 **Starting afresh.** When a frame's epoch differs from the last one, the core closes the turn without a word, empties the gate and the reply records, and keeps the generation rising. A session named with source `clear` or `fork` does the same and asks for the new file.
- EG-4.10 **Time.** The core has no timer. When something is due later (a bell that may still come, a cap, Done) it returns "wake me at T"; the window keeps one timer for the earliest such time and sends a tick. Tests send ticks with times of their own.
- EG-4.11 **In charge, or in shadow.** The core always runs. For each family of decisions (the state, Done, reply speech, the background) the window either carries the core's decisions out or, in shadow, lets its old code act and only compares: once for each turn it writes one line of numbers to the log saying where the two differed (`FlowShadow`: Done's times, how long the working states differed, how many lines each would speak). Which families the core is in charge of is a constant of the build (section 10); the file-only setting `eventCore` set to `shadow` puts all of them back in shadow without a rebuild.

### EG-5 Speech

- EG-5.1 **Proposals.** `Arrivals` keeps proposing reply lines from the screen as today, with its own memory of what was spoken. For each proposed line it also says which head it belongs to (the line id of the nearest `claude:` or `error:` row above it in the same reply; a head names itself) and what it is: a head, a row under a head, an `error:` row or a row under one, a tool row. The window passes them to the core with the frame; it no longer holds them itself.
- EG-5.2 **Only inside a turn.** A proposed line is considered only while a turn is open or ending. Outside, it is dropped and counted: a repaint at idle, a redraw after a picker, a progress row, a replay. This needs no record and holds for every program.
- EG-5.3 **The record confirms.** While there is a record, a proposed reply line waits until the record shows the text it belongs to:
  - A **reply record** is a ReplyText entry, with its key (EG-3.7) and how far into the key the screen's lines have been matched. A Stop event's last message makes a provisional one at once; when the entry with the same key arrives it takes its place and keeps what was matched.
  - Reply records are candidates from the moment they are read until a head has taken them, a Prompt entry is read after them, or the epoch changes. So an entry of an earlier turn is never a candidate.
  - A **head** is confirmed by the first candidate, in the record's order, that no head has taken and whose key starts with the head's own letters (the row without its label; the first 24 letters, or all of them when fewer). The head is then released, the candidate is taken, and the core says which entry was bound to which line (EG-8.1). A candidate releases once.
  - A **row under a confirmed head** is released when its letters are found in that record's key at or after the place matched so far, no more than 80 letters further on unless the row has 16 letters or more; the place then moves behind it. A row without letters is released with its head.
  - A row under a head that is not confirmed waits with it.
  - Released lines of one input go out as one decision, in the conversation's order, and the window speaks them as one utterance through the path of today (`SpeechFilter`, `Utterance`, one non-interrupting announcement, SPEC.md D32).
- EG-5.4 **The cap, and no record.** A line the record has not confirmed `GateCapMs` after it was proposed is dropped and counted: it is a repaint, a replay, Claude Code's summary of an earlier reply, a question's rows, a row of Claude Code's own. When there is no record (grounding off, the file not found, the reader off) the gate is open: a line is released `UnconfirmedHoldMs` after it was proposed, which is today's hold, still only inside a turn.
- EG-5.5 **Not gated.** An `error:` row and the rows under it are released at once: they are Claude Code's own words about a failed request and stand in no ReplyText entry. A tool row, spoken only with Speak each tool call, is released at once, as today.
- EG-5.6 **Done after the last reply.** A turn that closes with its bell is ending. Done is said, with its chime and the taskbar signal as today, at the first moment when all of these hold, or `DoneCapMs` after the bell, whichever comes first:
  1. no proposed line is pending in the gate;
  2. the end is confirmed: the turn's Stop event has come, or the record has shown its TurnEnd, or `DoneNoRecordMs` have passed since the bell;
  3. a frame has ended after the bell that added a visible line to the conversation (the last reply's rows, or the turn summary row).
  Lines released in the same step are spoken before Done. After Done the turn's window is closed (EG-5.2). While the window is put away, Done is kept for the bringing up as today, one for several turns.
- EG-5.7 **What is dropped and what never.** When the generation rises, every pending line is dropped, unspoken and counted. Done is never dropped by a generation. Questions and what the window says of them are the window's and are never dropped by the core.
- EG-5.8 Before Claude is ready after a start nothing is proposed (the replay is for reading), as today. "Responding", "Ready", the ticks and the tool sound stay where they are.
- EG-5.9 Every number of the gate is in the diagnostics: released, dropped outside a turn, dropped unconfirmed, dropped as stale, pending now.

### EG-6 The status and the sounds from the core

- EG-6.1 The status bar's state word is chosen as today, in today's order, with one change: "working" is said while the core has a turn open, with the spinner's word when the screen shows one. "ready" needs a mode line read once, as today, and no open turn.
- EG-6.2 The working music plays while the core has a turn open and Claude does not ask.
- EG-6.3 Everything in the window that asks whether Claude is busy or idle reads the core: the close and restart questions, the wait before an offered command and before a feedback draft opens, the model and effort commands, the Enter that is sent again (SPEC.md FR-2.1), and `Arrivals`' rule that the last line is final when Claude is idle.
- EG-6.4 A message sent while the core has a turn open or ending is "Queued", and its block is held as D16 says (`SessionModel.TurnOpen`). So a message sent while a reply's rows are still arriving goes after that reply.
- EG-6.5 The Done chime comes with the core's Done. A bell alone starts the window's question settle as today. While the core is in charge of Done, that settle says Done by itself only when no mark has been seen in this run: the screen-only path, where the bell and the settle are all there is (EG-4.6).
- EG-6.6 "N messages waiting" is the record's count while there is a record, the hint rows' count otherwise.
- EG-6.7 No sound changes, and none is added.

### EG-7 Background work

- EG-7.1 The core keeps the tasks the last Stop event listed as running: id, type (`shell`, `subagent`, or another word), description, worker type. A later Stop replaces the list. A worker's stop (SubagentStop) takes its id off the list. A worker's start (SubagentStart) is counted for the diagnostics and adds nothing to the list by itself (EG-D-12).
- EG-7.2 The one number of SPEC.md FR-7.10 gains the list as one more place: agents are the largest of the rows shown, the waiting row's number, the mode line's own count and the listed subagents; shells are the mode line's count once a mode line has been read after the last Stop, the listed shells before that; other kinds are the larger of what the mode line counts under other words and the listed tasks of other types. Never a sum of places that count the same things.
- EG-7.3 "What runs in the background" (Ctrl+B) names each listed task the rows do not name, by its description: `1 shell in the background. Shell: Run ping in background`.
- EG-7.4 The sounds, the words, the status bar's part and the settle of 700 ms stay as they are: the rows are still drawn every second.
- EG-7.5 Without events the number is today's, exactly.

### EG-8 Grounding

- EG-8.1 **Bind.** When the gate confirms a head (EG-5.3), or when reply speech is off and a head and a candidate meet by the same rule at the turn's end, the reply's lines are bound to the entry: the head and the rows under it down to the next labelled row or marker. A bound line carries the entry's id (`Line.SourceId`) and its heading level from the source (`Line.SourceLevel`).
- EG-8.2 **Structure for navigation.** A bound line whose letters equal those of a heading line of the source (`#` to `######`) is a heading of the source's level plus one, at most 6, since the exchange markers are level 1 (SPEC.md FR-4.1); every other bound line is no heading. A bound line keeps that level whatever the heuristic of `LineClassifier.HeadingLevel` says. Unbound lines keep the heuristic. Other structure of the source (code, lists, tables, links) is not carried until a key reads it (section 11).
- EG-8.3 **Pin.** When the head of a bound reply is redrawn by Claude Code as its own summary of that reply (a `claude:` row ending in ` · summary`, `ClaudeCodeTexts.SummaryEnd`), the conversation keeps what was said: the reply's lines as they stood when they were bound are put back in front of the summary row as lines of their own, final, with their kinds, joins and levels, and the summary row is an interface row, hidden like the mode line. A bound line whose text changes in any other way loses its binding and is left as the screen has it, counted.
- EG-8.4 **The markers' order.** After a turn, when the record shows that a prompt came after a reply and the conversation has that prompt's exchange block inside the reply's bound lines, the block is moved once to behind the last of those lines, with a note for the log. EG-6.4 keeps this from happening; this is the repair when it did.
- EG-8.5 **History at a start.** When a window starts on a conversation that exists (`--continue`, `--resume`), the record's earlier entries are read once, off the UI thread, and the rows Claude Code redraws are bound to them by the rules of EG-5.3, with nothing spoken: the "from previous session" blocks stand in the record's order and headings have their levels. What the redraw shortened is restored from the record only once a renderer for the rules of 4.3 exists (section 11); until then it is counted in the diagnostics.
- EG-8.6 **Export.** Session, Export Claude Code's record of this conversation writes the session's prompts and replies from the session file as Markdown: for each turn `# Input n`, the prompt, `# Output n Reply from Claude`, then each reply's source text as Claude wrote it (headings one level deeper, as in the conversation), and one line `tool: <name>` for each tool call. No tool input, no tool result, nothing of the kinds EG-3.4 skips. The file is the user's choice, named `axclaude-record-<date>.md` at first, kept out of git as the other exports (SPEC.md D34). Without a record the command says "No session file for this conversation".
- EG-8.7 **Never a wholesale rewrite.** No rule of this section replaces a turn's lines by lines rendered from the file, but for the pin of EG-8.3, which puts back the screen's own earlier lines.
- EG-8.8 **The view's rules it keeps** (SPEC.md FR-3.3, FR-3.3a, D24, D28, D29):
  - a line's `Text` is never replaced by an equal new string;
  - binding changes no text and no separator, so it makes no edit in the view; a changed heading level makes an edit only with markers hidden and a text that starts with `#`;
  - a pin and a moved block are one edit each, held like any output while the user presses keys, reads with Say all or holds a selection, and the caret keeps its line by the mirror's rules; the view's self-check must hold after them;
  - nothing here runs in the per-frame pass: binding, pinning and ordering happen when an entry arrives or a turn ends;
  - `Line` gains only fields axTerminal can ignore, and line ids keep growing.
- EG-8.9 **Off by itself.** When the record is lost (EG-3.6) or grounding is switched off, nothing is bound, pinned or moved from then on; what was bound stays as it is.

### EG-9 Control and diagnostics

- EG-9.1 Options, Developer, **Events from Claude Code's hooks** (setting `claudeHooks`, on at first, subject to EG-2.5): off, no settings file is passed and no pipe is served, from the next start of Claude Code. Choosing it says so: "Events from Claude Code's hooks on, from the next start of Claude" or "... off, ...".
- EG-9.2 Options, Developer, **Ground the conversation in Claude Code's session file** (setting `groundInSessionFile`, on at first): off, the reader stops at once, there is no record (EG-5.4), and nothing is bound, pinned or moved. On again, the reader starts with what the file gains from then on.
- EG-9.3 Options, Developer, **Say the event sources** (a command, no key, mark "read"): says, interrupting, in numbers only, for example "Events. Marks: 14. Hooks: on, 9 received. Session file: followed, 212 entries. Core: in charge of state, Done and speech." The words for hooks are "off", "not passed, <reason in a word>", "on, N received" or "passed, none received"; for the session file "off", "not found", "followed, N entries" or "stopped, <reason in a word>"; for the core "in charge of ..." or "in shadow".
- EG-9.4 Help, Diagnostics, Copy diagnostics gains these lines, numbers and fixed words only:

  ```
  Event core: in charge of state, Done, speech; turns 5 finished, 1 interrupted, 0 cut, 0 stale
  Marks: 14 (A 5, C 4, D 4), bells 5
  Hooks: on, 9 received, 0 refused, 0 malformed, 0 of another session; Claude Code 2.1.325, table 2.1.325
  Session file: followed, 212 entries read, 1803 lines skipped, 0 not JSON, 0 too long
  Gate: 12 released, 3 outside a turn, 1 unconfirmed, 0 stale, 0 pending
  Grounding: 11 replies bound, 1 pinned, 0 blocks moved, 0 bindings lost
  ```
- EG-9.5 The log gains lines of the same kind, each starting `Events:`, at most one for each start, for each turn in shadow, for each reason a source is not used, and for each repair. Never a path of the session file, never a word of a prompt, a reply, an event's text or a tool's name: counts, times, fixed words and the first eight characters of a session id.
- EG-9.6 The file-only setting `eventCore` (`on` at first, `shadow`) is EG-4.11's.
- EG-9.7 Nothing here is a question, a notice or an automatic announcement.

### EG-10 Privacy, and what axKit contacts

- EG-10.1 axKit's own code still makes network requests in two places only and listens nowhere (rule 8). What is new: one program start that Claude Code makes at axClaude's request (`AxKit.exe hook`, for each event of EG-2.1), one named pipe of the window's on this computer, and the reading of Claude Code's session file.
- EG-10.2 The three new calls in `src` carry their comments, and `tools/outside-calls.ps1` gains the target `Claude Code events` (kind pipe) so that `docs/axKit/outside-calls.md` lists them:
  - the window's pipe: `// Outside call, pipe, Claude Code events: the window's own named pipe, which only this Windows account may open, where "AxKit.exe hook", started by Claude Code for an event of the session, says that the event happened; each connection must send the window's secret.`
  - the verb's connection: `// Outside call, pipe, Claude Code events: connects to the pipe of the axClaude window that started this Claude Code, named in the environment it gave, and writes the event; on this computer, never over the network.`
  - the verb's standard input: `// Outside call, pipe, Claude Code events: the standard input of "AxKit.exe hook", on which Claude Code writes the event it started the hook for.`
- EG-10.3 The session file holds the user's conversation. It is read into memory only. Nothing of its text, and nothing an event carries, enters a log, a report (F4), the diagnostics, the AI tools' answers beyond what the conversation already shows, or a file, but for the export the user asks for (EG-8.6).
- EG-10.4 Every document that states what axKit starts, opens or reads says this in the same commit as the code that does it (section 9): `PRIVACY.md`, `README.md`, `docs/axKit/README.md`, `SECURITY.md`, `docs/axKit/SPEC.md` (AX-5.1, AX-11.11), `docs/axKit/access-layers.md`, `docs/axKit/outside-calls.md`.
- EG-10.5 Claude Code writes the hook's command line into the session file it keeps (`stop_hook_summary`), as it does for any hook: a path of this computer in a file of this computer.
- EG-10.6 Fixtures are made from scratch sessions only, through the whitelist of EG-11.1. No file of a session of the user's is ever copied, quoted or opened by a tool of the repository.

### EG-11 Tests

- EG-11.1 **Timeline fixtures.** `tests/fixtures/timelines/<name>/` holds one recorded session on one clock (section 6.6): the console stream with its chunk index, the events, the entries, what the script sent, and the decisions expected. They are made by `ClaudeProbe fixture <run folder> <fixture folder>`, which is the whitelist that makes them safe to commit:
  1. Events: only the events of EG-2.1, each reduced to the fields of EG-2.1.
  2. Entries: only the types of EG-3.4, each reduced to the fields of EG-3.4; every other line becomes `{"type":"<its type>"}`, so that the order and the kinds stay and nothing else does.
  3. Masking, in the console bytes and in every kept string, length for length, so that no position in the stream moves: each whole-word occurrence of the account's, the computer's and the domain's name (`Redaction.DefaultNames`, the distinctive ones) becomes `x` characters of the same number of bytes, and a `Users` directly in front of a masked name becomes `xxxxx`.
  4. The check: the tool reads every file it wrote and refuses the fixture, deleting what it wrote and saying which check failed in numbers, when `Redaction.Find` finds an e-mail address or an account path, or a default name is still there.
  The banner's row naming the plan stays, as in the recordings of `tests/fixtures/README.md`. A fixture is never edited by hand; a run the tool refuses is recorded again from a neutral folder (`C:\Temp\axclaude-fixture`).
- EG-11.2 **The replayer.** `TimelineReplay` feeds a fixture's streams, merged by time, through the same code the window uses (`SessionModel`, `Arrivals`, `ClaudeCodeHooks`, `ClaudeCodeTranscript`, `FlowFeed`, `SessionFlow`) with the app's frame rule (a frame ends at the cursor shown again and after 100 ms without output). In the recorded order its decisions must equal the fixture's `expected.txt`. In hostile orders they must keep the invariants: every event 300 ms early; every event 300 ms late; every entry 1 s late; no entries; no events; neither; no marks (the screen alone). The invariants: one Done for each turn that ended with its bell and none for any other; no reply line released twice; no reply line released outside a turn or after its Done; released lines in the conversation's order; the state never ready between a turn's A and its D; no decision at all that differs between two runs of the same order.
- EG-11.3 **The stand-in.** `AxAutomate standin <scenario.json> <Claude Code's arguments>` plays a scenario (section 6.7) as Claude Code would in a window script: it answers `--version`, draws frames, marks and bells on the console, appends entries to a session file under the run's own folder, and starts the hook command of the `--settings` file it was given with the event on standard input, waited for or not as the file says. The runner puts its own program's path into `AXKIT_STANDIN` for the windows it starts; a script's `claude.cmd` is one line that calls it. It needs no Claude Code, no network and no model.
- EG-11.4 **The probe** is a tool of the repository, `tools/ClaudeProbe`: it starts the installed Claude Code as axClaude does, registers hooks for that session alone, records the three streams on one clock, prints merged timelines with the profile path replaced, and makes fixtures (EG-11.1). It is a measuring tool, never shipped and never started by axKit; while a recording runs it listens on 127.0.0.1 for the hooks it measures, which axKit itself never does (EG-D-15).
- EG-11.5 **Window scripts**, each a workflow with its label, run on a desktop of their own (`docs/axKit/automation.md`): 28 the event sources said on demand; 29 a turn played by the stand-in; 30 hooks through the stand-in; 31 hooks with Claude Code itself (no "Settings Warning", events received), which is the check `tools/upstream.ps1 -Verify` runs with the others of `tests/scripts/axClaude`; 32 the session file followed; 33 the export of the record; 34 the state from the marks, and a message sent during a reply's arrival; 35 Done after the reply, none for an interrupt; 36 the gate (a repaint, a summary, a question's rows, a real reply once); 37 background work listed by an event; 38 heading levels from the record; 39 a summary pinned; 40 a block moved behind its reply; 41 history bound at a start. Every existing script of `tests/scripts/axClaude` stays as it is and passes: the ones with a batch stand-in draw no marks and no events, so they are the test of the screen-only path.
- EG-11.6 **The NVDA plan** (`docs/axClaude/nvda-test-plan.md`) gains section 23, Events and grounding: the steps of scripts 28 to 41, plus by ear: Done right after a spoken reply with no gap that feels like a pause; nothing spoken after a `/resume` picker redraws the conversation; a status of "working" through a long tool run; a message sent in the last moment of a reply standing after it.
- EG-11.7 **Source-reading tests.** `ClaudeCodePlacesTests` fails when one of Claude Code's event names or session file field names (`SessionStart`, `SubagentStop`, `hook_event_name`, `transcript_path`, `turn_duration`, `promptSource`, `isAbortedMidStream`, `queue-operation`) stands in a string of a file of `src/AxClaude.Core` or `src/AxClaude` other than `ClaudeCodeHooks.cs` and `ClaudeCodeTranscript.cs`.

## 6. Formats

### 6.1 The settings file with the hooks

`%LOCALAPPDATA%\AxKit\claude-hooks.json`, written by `ClaudeCodeHooks.WriteSettings`, indented, UTF-8 without a mark. One entry for each event of EG-2.1, all of the same shape; the program path is the one thing that differs between computers. `"async": true` is left out for an event whose row in `ClaudeCodeHooks.Events` says so (EG-2.5).

```json
{
  "hooks": {
    "SessionStart": [
      { "hooks": [ { "type": "command", "command": "C:\\Tools\\AxKit\\AxKit.exe", "args": ["hook"], "async": true, "timeout": 5 } ] }
    ],
    "Stop": [
      { "hooks": [ { "type": "command", "command": "C:\\Tools\\AxKit\\AxKit.exe", "args": ["hook"], "async": true, "timeout": 5 } ] }
    ],
    "StopFailure": [ ... ],
    "SubagentStart": [ ... ],
    "SubagentStop": [ ... ],
    "PermissionRequest": [ ... ],
    "Notification": [ ... ],
    "SessionEnd": [ ... ]
  }
}
```

No matcher, no `shell`, no other key. Claude Code's arguments: `--ax-screen-reader --settings "<the file>" <the user's arguments> [--model <m>] [--mcp-config <file>]`.

### 6.2 An event as the pipe carries it

The environment variable: `AXKIT_HOOK_PIPE=AxKit-hook-S-1-5-21-1111111111-2222222222-3333333333-1001-9f3c2a7d5e8b4c16;<64 hex digits>`. The part before the first `;` is the pipe's name, the rest the secret.

One connection, client to server, then the client closes:

```
<the secret, 64 hex digits>\n
<the body's length in bytes, decimal>\n
<the body: the JSON object Claude Code wrote to the hook's standard input, UTF-8, without its last line end>
```

A body, as Claude Code 2.1.325 sends a Stop event (shortened; the paths written with `%USERPROFILE%`):

```json
{"session_id":"7aac8697-1111-4222-8333-444444444444","transcript_path":"%USERPROFILE%\\.claude\\projects\\C--Temp-axclaude-fixture\\7aac8697-1111-4222-8333-444444444444.jsonl","cwd":"C:\\Temp\\axclaude-fixture","hook_event_name":"Stop","prompt_id":"5d0c1f6e-aaaa-4bbb-8ccc-dddddddddddd","permission_mode":"auto","stop_hook_active":false,"last_assistant_message":"ok","background_tasks":[{"id":"b0jdigiy3","type":"shell","status":"running","description":"Run ping in background","command":"ping -n 8 127.0.0.1"}],"session_crons":[]}
```

`ClaudeCodeHooks.Parse` reads the fields of EG-2.1 and nothing else, and returns:

```csharp
public enum HookKind { SessionStarted, TurnStopped, TurnFailed, WorkerStarted, WorkerStopped, PermissionAsked, Notified, SessionEnded }

public sealed record ListedTask(string Id, string Type, string Status, string Description, string? WorkerType);

/// One event, reduced to what axClaude uses. LastReply is held in memory only and never shown or logged.
public sealed record HookEvent(
    HookKind Kind, string SessionId, string? TurnKey,
    string? RecordPath = null, string? Source = null,          // SessionStarted
    string? LastReply = null, IReadOnlyList<ListedTask>? Tasks = null,   // TurnStopped
    string? WorkerId = null, string? WorkerType = null,        // WorkerStarted, WorkerStopped
    string? Tool = null,                                       // PermissionAsked
    string? Reason = null);                                    // Notified (the notification type), SessionEnded
```

An unknown event name, a body that is no JSON object, or one without `hook_event_name` and `session_id` gives null and is counted as malformed.

### 6.3 The reader's entry record

```csharp
public enum RecordKind { Prompt, Note, CompactSummary, ToolResult, Interrupt, ReplyText, ToolCall, Thinking, TurnEnd, CompactBoundary, Enqueued, Dequeued, Removed, Other }

public enum PromptOrigin { None, Typed, Queued, SelfStarted }

/// One entry of the session file, reduced to what axClaude uses (EG-3.4). Text is held in memory only.
public sealed record RecordEntry(
    RecordKind Kind, string Id,
    string? TurnKey = null,          // promptId
    string? MessageId = null,        // message.id of an assistant entry
    string? Text = null,             // a Prompt's text, a ReplyText's source, an Enqueued's content
    string? ToolName = null, string? ToolUseId = null, bool IsError = false,
    bool Cut = false,                // isAbortedMidStream
    PromptOrigin Origin = PromptOrigin.None,
    DateTime? Time = null, long DurationMs = 0, bool Sidechain = false);
```

Example. This line of a session file (shortened):

```json
{"parentUuid":"1d63fa55-...","message":{"model":"claude-haiku-5-5","id":"msg_01Czpdnv","type":"message","role":"assistant","content":[{"type":"text","text":"# Result\n\nok"}],"stop_reason":"end_turn","usage":{...}},"requestId":"req_01...","type":"assistant","uuid":"b8142ecf-...","timestamp":"2026-11-16T00:13:04.933Z","cwd":"...","sessionId":"f835ea24-...","version":"2.1.325"}
```

is read as `new RecordEntry(RecordKind.ReplyText, "b8142ecf-...", MessageId: "msg_01Czpdnv", Text: "# Result\n\nok", Time: 2026-11-16T00:13:04.933Z)`, with the screen key `resultok`. And `{"type":"session-owner","accountUuid":"...","organizationUuid":"...","sessionId":"..."}` is skipped at its `type`: `Parse` returns null and counts one skipped line.

### 6.4 The core's inputs

All in `AxClaude.Core.Flow`. `Proposed` and `ProposedKind` are `Arrivals`' (EG-5.1).

```csharp
public enum TurnMark { Start, Output, End }                 // OSC 133 A, C, D
public enum WorkSeen { Unknown, Idle, Working }             // SessionModel.Work
public enum UserAction { Sent, Answered, Escaped, SentNow, ScreenKey, PutAway }
public enum SessionSource { Startup, Resume, Clear, Compact, Fork, Other }
public enum AttentionKind { Permission, Notification }
public enum EntryKind { Prompt, ReplyText, ToolCall, ToolResult, Interrupt, TurnEnd, Compact, Enqueued, Dequeued, Other }

public abstract record FlowInput;
public sealed record ProcessStarted : FlowInput;
public sealed record ProcessEnded : FlowInput;
public sealed record MarkSeen(TurnMark Mark) : FlowInput;
public sealed record BellSeen : FlowInput;
public sealed record FrameSeen(int Epoch, WorkSeen Work, bool Asking, bool LineAdded, bool SpeechOn, int WaitingShown, IReadOnlyList<Proposed> Proposed) : FlowInput;
public sealed record UserActed(UserAction Action, string? SentKey = null) : FlowInput;   // SentKey: the letters of a sent message
public sealed record QuestionShown : FlowInput;
public sealed record SessionNamed(string SessionId, string? RecordPath, SessionSource Source) : FlowInput;
public sealed record TurnStopped(string? LastReplyKey, IReadOnlyList<FlowTask> Tasks) : FlowInput;
public sealed record TurnFailed : FlowInput;
public sealed record WorkerStarted(string Id, string Type) : FlowInput;
public sealed record WorkerStopped(string Id, string Type) : FlowInput;
public sealed record AttentionAsked(AttentionKind Kind) : FlowInput;
public sealed record SessionEnded(string Reason) : FlowInput;
public sealed record EntryRead(EntryKind Kind, string Id, string? Key = null, bool SelfStarted = false, bool Cut = false) : FlowInput;
public sealed record RecordFound : FlowInput;
public sealed record RecordLost : FlowInput;
public sealed record Tick : FlowInput;

public sealed record FlowTask(string Id, string Type, string Description, string? WorkerType);
```

`Proposed` in `Arrivals.cs`:

```csharp
public enum ProposedKind { Head, Row, Error, Tool }
public readonly record struct Proposed(Line Line, int HeadId, ProposedKind Kind);
```

Example, a plain turn in arrival order: `MarkSeen(Start)`, `FrameSeen(... Working ...)`, `TurnStopped("ok", [])`, `MarkSeen(Output)`, `MarkSeen(End)`, `BellSeen`, `FrameSeen(... Idle, LineAdded: true, Proposed: [the claude: row, Head])`, `EntryRead(ReplyText, "b8142ecf", "ok")`, `EntryRead(TurnEnd, ...)`.

### 6.5 The core's decisions

```csharp
public enum FlowState { Idle, Working }

public abstract record FlowDecision(int Generation);
public sealed record StateChanged(int Generation, FlowState State) : FlowDecision(Generation);
public sealed record AttentionBell(int Generation, bool NoMarks) : FlowDecision(Generation);   // NoMarks: no mark was seen in this run, so the bell may be Done by the old rule
public sealed record SayDone(int Generation, int Turn, long AfterBellMs) : FlowDecision(Generation);
public sealed record SpeakReply(int Generation, IReadOnlyList<Line> Lines) : FlowDecision(Generation);
public sealed record FollowRecord(int Generation, string Path) : FlowDecision(Generation);
public sealed record ForgetRecord(int Generation) : FlowDecision(Generation);
public sealed record EntryBound(int Generation, string EntryId, int HeadLineId) : FlowDecision(Generation);
public sealed record BackgroundListed(int Generation, IReadOnlyList<FlowTask> Tasks) : FlowDecision(Generation);
public sealed record WakeAt(int Generation, long AtMs) : FlowDecision(Generation);
public sealed record Note(int Generation, string Text) : FlowDecision(Generation);
```

Each has `ToLine()`, one line of numbers and fixed words for the log and the kept files: `state working`, `bell attention`, `done turn=1 after=74`, `speak lines=1 letters=2`, `follow`, `forget`, `bound head=412`, `background tasks=1`, `note stale-turn after=2004`. Never a text of the conversation. The plain turn of 6.4 gives: `state working`, `state idle`, `speak lines=1 letters=2`, `bound head=412`, `done turn=1 after=74`.

### 6.6 A timeline fixture's files

`tests/fixtures/timelines/<name>/`:

| File | What it holds |
| :-- | :-- |
| `meta.json` | `{"claudeCode":"2.1.325","columns":240,"rows":50,"recorded":"2026-11-15","from":"r1/a","turns":3}` |
| `pty.vt`, `pty.vt.chunks.txt` | the console stream, masked length for length, and its index `offset length elapsedMs` on the fixture's clock, as `StreamRecorder` writes it |
| `events.jsonl` | one line for each kept event: `{"t":7987.6,"event":{"hook_event_name":"Stop","session_id":"...","prompt_id":"...","last_assistant_message":"ok","background_tasks":[]}}` |
| `entries.jsonl` | one line for each line of the session file, in file order: `{"t":8121.1,"entry":{"type":"assistant","uuid":"...","message":{"id":"msg_...","content":[{"type":"text","text":"ok"}],"stop_reason":"end_turn"},"timestamp":"..."}}`, or `{"t":5852.4,"entry":{"type":"api-request-blob"}}` for a kind that is skipped |
| `sends.jsonl` | what the script wrote: `{"t":5649.1,"kind":"message","text":"Reply with the single word ok."}`, `{"t":5753.2,"kind":"key","name":"Escape"}`, `{"t":7274.5,"kind":"key","name":"SendNow"}` |
| `expected.txt` | the core's decisions in the recorded order, one `ToLine()` for each, with its time: `8011 done turn=1 after=74`. Written with `AXCLAUDE_UPDATE_EXPECTED=1`, reviewed by hand the first time |

`t` is milliseconds on the recording's clock. The session file's path in an event is masked and never opened: the replayer feeds the entries itself.

### 6.7 The stand-in's scenario

A JSON file a window script writes with its `file` step. Since that step turns `\n` into a line end and `\uXXXX` into its character, no string of a scenario holds an escape: rows and lines are arrays.

```json
{
  "version": "2.1.325 (stand-in)",
  "session": "11111111-2222-4333-8444-555555555555",
  "home": "home",
  "steps": [
    { "frame": ["claude: started", "auto mode on (shift+tab to cycle)", "$"], "clear": true },
    { "hook": "SessionStart", "fields": { "source": "startup" } },
    { "waitFile": "go1" },
    { "mark": "A" },
    { "frame": ["claude: started", "you: Say ok", "Pollinating…", "auto mode on (shift+tab to cycle)  ·  esc to interrupt", "$"], "clear": true },
    { "user": ["Say ok"], "source": "typed" },
    { "wait": 300 },
    { "hook": "Stop", "last": ["ok"], "tasks": [] },
    { "mark": "C" }, { "mark": "D" }, { "bell": true },
    { "wait": 60 },
    { "frame": ["claude: started", "you: Say ok", "claude: ok", "Sautéed for 2s · done 4:07 PM", "auto mode on (shift+tab to cycle)", "$"], "clear": true },
    { "assistant": ["ok"] },
    { "turnEnd": 2345 },
    { "idle": true }
  ]
}
```

| Step | What the stand-in does |
| :-- | :-- |
| `frame` (rows), `clear` | writes `ESC[?25l`, with `clear` also `ESC[2J ESC[H`, the rows joined by CR LF, then `ESC[?25h`: one frame. The cursor ends on the last row |
| `mark` `A`, `C`, `D` | writes `ESC]133;A;redraw=0`, `ESC]133;C` or `ESC]133;D`, each ended by `ESC \` (the parser takes that ending and a BEL alike; a BEL that ends such a sequence is no bell) |
| `bell` | writes BEL |
| `hook` (name), `fields`, `last` (lines), `tasks` | builds the event (`hook_event_name`, `session_id`, `transcript_path`, `cwd`, `prompt_id`, the fields given, `last` joined by line ends as `last_assistant_message`, `tasks` as `background_tasks`) and starts the command the `--settings` file names for that event, the event on its standard input; waits for it unless the file says `async`. Without `--settings`, or without that event in it: nothing |
| `user` (lines), `source` | appends a `user` entry (`promptSource` from `source`, a new `promptId`) to the session file |
| `assistant` (lines), `cut` | appends an `assistant` text entry; with `cut`, `isAbortedMidStream` and then the interrupt's `user` entry |
| `toolUse` (name), `toolResult` | appends the tool call's entry, its result's entry |
| `turnEnd` (ms) | appends `system` `turn_duration` |
| `queue` (`enqueue`, `dequeue`), `text` | appends a `queue-operation` |
| `raw` (object) | appends the object as one line: an unknown or a skipped kind |
| `wait` (ms), `waitFile` (name in the working folder) | waits; the script makes the file |
| `idle` | waits for ever |

The session file is `<the scenario's folder>\<home>\.claude\projects\<project folder name of the working folder>\<session>.jsonl`. With `--version` as its first argument the stand-in prints `version` and ends. A script's `claude.cmd`:

```
@"%AXKIT_STANDIN%" standin "%~dp0scenario.json" %*
```

## 7. Architecture

### 7.1 New and changed code, by project

`src/AxKit.Core` (shared, pure):

- `AppPaths.ClaudeHooksFile` (new property): `%LOCALAPPDATA%\AxKit\claude-hooks.json`, a test run's own folder in a run.
- `Transcript/Line`: `public string? SourceId { get; set; }` and `public int? SourceLevel { get; set; }` (WP-A20). axTerminal never sets them.
- `Vt/Screen`: the comment on `Mark` no longer says that axClaude does not listen. No code changes.

`src/AxKit` (the executable):

- `Program.Main`: its first statement is `if (args.Length > 0 && args[0] == HookRelay.Verb) { HookRelay.Run(); return; }`; everything it did moves unchanged into a method `Start(string[] args)` that `Main` calls next, so that the hook's process loads nothing of WinForms.

`src/AxClaude.Core/Transcript` (Claude Code's screen, as today):

- `SessionModel`: `public event Action<char>? Marked;` `public ScreenWork Work { get; private set; }` `public int Epoch { get; private set; }` `public bool? TurnOpen { get; set; }` and `public enum ScreenWork { Unknown, Idle, Working }` (EG-1). Later `public void Bind(Line head, string entryId, IReadOnlyList<int?> levels)`, `public void Unbind(Line line)` (WP-A20), the pin inside `TextChanged` (WP-A21), `public bool MoveBlockBehind(Line inputMarker, Line last)` (WP-A22).
- `Arrivals`: `Arrival` gains `IReadOnlyList<Proposed> Proposed` beside `SpokenLines`; `Proposed` and `ProposedKind` (6.4).
- `ClaudeCodeTexts.SummaryEnd` (`" · summary"`, WP-A21).
- `BackgroundWork.Count` gains `listedAgents`, `listedShells`, `listedOther` and `modeLineAfterStop`; `BackgroundWork.Said` gains the listed tasks (WP-A9).

`src/AxClaude.Core/Hooks`:

- `ClaudeCodeHooks` (static; Claude Code's place): `public sealed record HookSpec(string Name, string VerifiedWith, bool Async);` `public static readonly IReadOnlyList<HookSpec> Events;` `public static bool Supports(string? versionLine);` `public static string SettingsText(string program, string verb);` `public static bool WriteSettings(string file, string program, string verb);` `public static IReadOnlyList<string> SessionArguments(string file);` `public static bool UserPassesSettings(IReadOnlyList<string> claudeArguments);` `public static HookEvent? Parse(ReadOnlySpan<byte> body);`
- `HookEvent`, `HookKind`, `ListedTask` (6.2).
- `HookFrame` (static; neutral): `public const string Variable = "AXKIT_HOOK_PIPE"; public const int MaxBody = 1_048_576;` `public static string NewPipeName(string userSid);` `public static string NewSecret();` `public static string VariableValue(string pipeName, string secret);` `public static bool TryParseVariable(string? value, out string pipeName, out string secret);` `public static void Write(Stream pipe, string secret, ReadOnlySpan<byte> body);` `public static byte[]? Read(Stream pipe, string secret, out HookFrameFault fault);` with `public enum HookFrameFault { None, WrongSecret, TooLong, Broken }`.
- `HookRelay` (static; neutral): `public const string Verb = "hook";` `public static void Run();` `internal static bool Relay(Stream input, string? variableValue, Func<string, int, Stream?> connect);`

`src/AxClaude.Core/Pty`:

- `ClaudeLauncher.BuildEnvironment(string? hookPipe = null)`: removes `HookFrame.Variable`, then sets it when a value is given (EG-2.8).

`src/AxClaude.Core/Record`:

- `ClaudeCodeTranscript` (static; Claude Code's place): `public static string ProjectFolderName(string workingFolder);` `public static IReadOnlyList<string> ProjectsFolders(string home, IReadOnlyDictionary<string, string> childEnvironment, string? runHome);` `public static bool IsSessionFile(string path, IReadOnlyList<string> projectsFolders);` `public static RecordEntry? Parse(ReadOnlySpan<byte> line, long offset, out RecordLine what);` with `public enum RecordLine { Read, Skipped, NotJson, Unknown }`; `public static string ScreenKey(string source);` `public static IReadOnlyList<(string Key, int Level)> Headings(string source);` (WP-A20).
- `RecordEntry`, `RecordKind`, `PromptOrigin` (6.3). `RecordText.Letters(ReadOnlySpan<char> text)` (neutral).
- `RecordTail` (neutral): `public RecordTail(string path, long start, Func<ReadOnlySpan<byte>, long, RecordEntry?> parse);` `public IReadOnlyList<RecordEntry> Poll();` `public RecordTailState State { get; }` (`Following`, `Missing`, `Stopped`), its counters, and `public static IReadOnlyList<RecordEntry> ReadAll(string path, ...)`.
- `RecordFinder` (neutral): `public void Note(IEnumerable<string> projectFolders);` `public string? Find(IReadOnlyCollection<string> sentKeys, Func<ReadOnlySpan<byte>, long, RecordEntry?> parse);`
- `RecordExport.Render(IEnumerable<RecordEntry> entries)` (WP-A15); `RecordBinder.Levels(RecordEntry reply, IReadOnlyList<Line> lines)` (WP-A20).

`src/AxClaude.Core/Flow` (neutral):

- `FlowInput`, `FlowDecision` and their records (6.4, 6.5).
- `SessionFlow`: `public SessionFlow(SourceHealth health);` `public IReadOnlyList<FlowDecision> Handle(FlowInput input, long nowMs);` (the list is reused: read it at once) `public bool Working { get; }` `public bool Ending { get; }` `public int Generation { get; }` `public int Waiting { get; }` `public IReadOnlyList<FlowTask> Tasks { get; }`. Three files of one partial class: `SessionFlow.cs` (the turn, the state, Done, time), `SessionFlow.Gate.cs` (EG-5), `SessionFlow.Background.cs` (EG-7).
- `SourceHealth`: the counters of EG-9.4 as plain fields, `public IReadOnlyList<string> Lines(string coreWords)` and `public string Said(string coreWords)`, the window giving the words for what the core is in charge of.
- `FlowFeed` (static): `public static FrameSeen FromFrame(SessionModel model, Arrival arrival, bool asking, bool speechOn);` `public static MarkSeen? FromMark(char letter);` `public static FlowInput? FromHook(HookEvent e);` `public static FlowInput? FromEntry(RecordEntry e);`
- `FlowShadow`: `public enum ShadowFact { SaidDone, SpokeLines, Working, Idle }` `public void Window(ShadowFact fact, int count, long nowMs);` `public void Core(FlowDecision decision, long nowMs);` `public string? TurnLine();` (the one line for a turn that both have ended, null until then).

`src/AxClaude.Core/AppSettings.cs`: `ClaudeHooks` (true), `GroundInSessionFile` (true), `EventCore` (`"on"`).

`src/AxClaude` (the window):

- `MainForm.Flow.cs`: the core, the queue, the drain loop, the one timer, the carrying out of decisions, which families the core is in charge of (`FlowCharge`), the shadow's log lines, Say the event sources, the diagnostics' lines; and the partial methods the other two files fill: `partial void RecordProcessStarted(); partial void RecordProcessEnded(); partial void RecordFollow(string path); partial void RecordForget(); partial void RecordSwitch(bool on); partial void RecordSent(string key);`
- `MainForm.Hooks.cs`: `HookChannel` (the pipe's thread, EG-2.7) and what `StartClaude` asks of it: the file written, the arguments, the variable's value.
- `MainForm.Record.cs`: `RecordHost` (the reader's thread, the folder rule's polling, EG-3) and those partial methods.
- `MainForm.cs`, `MainForm.Model.cs`, `MainForm.Process.cs`, `MainForm.Input.cs`, `MainForm.Area.cs`, `MainForm.Background.cs`, `MainForm.Ui.cs`, `MainForm.Export.cs`: the places that today decide from the screen ask the core instead, family by family (section 10).

`tools/ClaudeProbe` (EG-11.4), `tools/AxAutomate/StandIn.cs` and three lines of `tools/AxAutomate/Program.cs` (EG-11.3).

### 7.2 Threads

| Thread | What runs on it |
| :-- | :-- |
| The UI thread | the model, `Arrivals`, the core, the queue's loop, every decision carried out, the one timer |
| The console's reader thread (as today) | reads the pseudo console; one post for a burst |
| "AxKit hooks", one for each running Claude Code | serves the pipe, reads a frame, parses it (`ClaudeCodeHooks.Parse`), posts the event; one post for a burst |
| "AxKit record", one for each running Claude Code | polls the folder and the file, parses lines, posts entries; one post for a burst |
| `AxKit.exe hook`, a process for each event | standard input to the pipe, then ends |

A post from a thread whose Claude Code is no longer the window's is dropped (the check `FeedInbox` makes today). The UI thread never waits for either thread; stopping Claude Code tells them to end and does not wait for them.

### 7.3 The second-program test

Claude Code's own places (rule 12), each tested from recordings, where a change of Claude Code is absorbed: `ClaudeCodeTexts`, `ClaudeCodeKeys`, `ClaudeCodeSettings`, `ClaudeLauncher`, and now `ClaudeCodeHooks` (the events' names and fields, the settings file's shape, the versions) and `ClaudeCodeTranscript` (the session file's fields, the projects folder's rule, the screen key's rules, the headings).

Neutral, with no word of Claude Code's in it: the records of 6.2 to 6.5, `SessionFlow`, `FlowFeed`, `FlowShadow`, `SourceHealth`, `RecordText`, `RecordTail`, `RecordFinder`, `RecordExport`, `RecordBinder`, `HookFrame`, `HookRelay`, the three window files.

A second agent program supplies a table of its own of the same shape for each place it has (its marks, its events, its record) and maps them to the same records; for a place it lacks, nothing, and the evidence lists fall through to the screen. The neutral pieces move to a shared project on that second use, unchanged, one for each commit (rule 4). The marks are already neutral: OSC 133 is the shells' own convention, and axTerminal reads the same event.

### 7.4 What stays in the window

What is about NVDA and the user's own keys: the join of announcements, the wait before a question's interrupting words, the delayed focus, the hold of keys, the estimate of speech still running; the question flow and the control area with their settle after a lone bell; offers and feedback drafts; putting away and bringing up; "Ready", "Responding", the ticks and the tool sound; the background settle and the chords. All of them read the core's turn state where they asked the screen before.

## 8. Decisions

Each was taken by the assistant on 2026-11-15, on the user's word to take the recommended option on every decision, for him to confirm. The first ten are the decisions taken before this document was written, as the measurements sharpened them.

- **EG-D-1 Four sources, each for what it does best.** The marks for the turn's boundaries; the session file for what was said, in what order, with identities; hooks for what neither gives; the screen for what is live and interactive. No single source gives everything: the screen has no identities and can be redrawn; hooks arrive on another channel; the file lags the screen; the marks carry no content. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-2 A hook is a program start and a pipe, never a listener.** `docs/axKit/mcp.md` rules out TCP and HTTP also on localhost, and rule 8's statement of what axKit contacts stays true. Considered and not taken: HTTP hooks to a loopback listener (about 30 ms an event, but a listener); hooks through an MCP tool of axKit's own (no process for each event, but a tool in front of the model). (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-3 Eight events, registered for each start with `--settings`, never in a settings file of the user's.** A hook in his settings would fire in every Claude Code session of his. Not PreCompact and PostCompact, which print rows; not the events for each tool call or prompt, which the marks and the file give without a process. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-4 One pure core, plain records in, plain decisions out, one queue.** The state a dozen fields of the window kept lives in one tested class; "event driven" without an event bus. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-5 The record confirms before anything is spoken; Done follows the last reply.** In screen reader mode a reply is drawn whole at its end, so waiting for the record costs about a tenth of a second and makes a second hearing impossible. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-6 The pipe trusts the secret, not the client's program file.** The settings file is shared by every window and names whichever axKit wrote it last; a development build and another copy would refuse each other's verb. The event is a signal that runs nothing (EG-2.9), the pipe is the user's alone, and the secret is new for each start. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-7 Bind and pin; never a wholesale rewrite.** The lines the user reads are the screen's; the file ties, keeps and orders them. Rewriting a turn at its end moves text under the caret. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-8 Nothing of a session file or an event enters a log, a report or the diagnostics.** Counts and fixed words only. Fixtures from scratch sessions, through the whitelist. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-9 Two Developer switches, both on at first, no question.** Observing changes nothing Claude Code does. If the trial finds a synchronous Stop hook costing more than half a second, the hooks switch starts off (EG-2.5). (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-10 The screen-only path stays, and the work lands in three stages.** Today's rules are the last fallback of each fact, tested by the existing scripts. Stage 1 changes nothing he hears; stage 2 moves decisions to the core, one family for each commit; stage 3 is grounding. What is not solid stays in shadow or off. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-11 The Stop event's last message stands in for the record's last entry until the entry arrives.** It is the same text, byte for byte, 90 to 235 ms earlier, so the turn's last reply is confirmed before its rows are drawn and Done does not wait for the file. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-12 A worker's start adds nothing to the background number by itself.** A dynamic workflow's inner agents were not measured with hooks; if each fired SubagentStart, a workflow of fifty agents would ping fifty times. The Stop event's list is one more place in the "largest of" rule; the rows stay. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-13 Moving in the conversation raises no generation; Done and questions are never dropped.** A reader who steps through old text still wants the reply spoken; what he has moved on from is what he sent, answered or was asked. "Never silence" holds for the two things he waits for. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-14 Done is said for each turn that finished, also when a queued turn follows at once; not for a reply cut by Send now.** The first is what he hears today. The second drops one "Done" that announced an end that was a cut he asked for. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-15 The probe is a tool of the repository and keeps its listener.** It measures both of Claude Code's transports, and a measuring tool that is never shipped and never started by axKit is not axKit listening. Its README says so. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-16 Timeline fixtures are masked by the tool, length for length.** Every scratch recording holds the account's folder name; the rule of `tests/fixtures/README.md` (record again, never edit bytes) protects positions in the stream, which a replacement of the same length keeps. Never by hand, and checked by the tool after. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-17 The stand-in is a mode of the test runner.** A window script knows only its own folder, and the runner's path is the one thing every run has; a second program would need a build and a path of its own. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-18 The first start of a window waits up to 2 s for Claude Code's version.** Without the version no hooks may be passed, and a window lives for hours: a start without hooks after every update of Claude Code would be most of his days. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-19 The Store copy passes no hooks until the path question is settled.** A settings file Claude Code cannot read at the path given has an unknown effect on its start. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-20 Claude's headings are one level deeper than the source says.** The exchange markers are level 1, so that key 1 stops at every exchange; a reply's `#` is 2. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-21 A pin puts back the screen's own earlier lines, not a rendering of the file.** They are the text he heard and read, with the wraps he read them in; a renderer for the file is a later piece (section 11). (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-22 `--session-id` is not passed.** It cannot be combined with `--continue`, the default; the folder rule finds a new file as well. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-23 A command that says the event sources, and a file-only setting that puts the core in shadow.** The first is information on demand, by ear, and what the window scripts read; the second is the way back without a rebuild. (Taken by the assistant on 2026-11-15 for the user to confirm.)
- **EG-D-24 Tool rows and `error:` rows pass the gate unconfirmed.** A collapsed tool row names no tool, and an error row stands in no reply entry; inside a turn they are spoken as today. The tool rows from the file are later work. (Taken by the assistant on 2026-11-15 for the user to confirm.)

## 9. What changes in other documents and notes

Each change is made in the commit of the work package that changes the behaviour (section 10 names it).

- `docs/axClaude/SPEC.md`
  - Section 2: G3 "Faithful" gains "in the order of Claude Code's own record where it is known" (WP-A21).
  - Section 4.1: the bell no longer "arrives just before the final frame" alone; a pointer to 4.1 to 4.3 here. Section 4.4 item 6 and the "Detected but ignored" note on OSC 133: the marks are listened to (WP-A7).
  - Section 5: a new line under FR-13, "FR-15 Events and grounding: `events-and-grounding.md`", and FR-12.3 gains the two switches and the command (WP-A7).
  - FR-3.8: the second export (WP-A15). FR-4.1 and D22: "nothing in the block moves afterwards" gains "but once, when the record shows it stands inside an earlier reply" (WP-A22). FR-4.7 and D16: held while the core has a turn open or ending (WP-A16). FR-4.8: the blocks in the record's order (WP-A23).
  - FR-5 and section 7.6: heading levels from the record for bound lines; the heuristic for the rest; risk R8 narrowed (WP-A20).
  - FR-7.2 and section 6.4: Done by EG-5.6; "Claude asks" after a lone bell as before (WP-A17). FR-7.4, FR-7.4b, D32: reply speech through the gate; the 250 ms hold only without a record (WP-A18). FR-7.9: the music follows the core (WP-A16). FR-7.10: the listed tasks (WP-A19). FR-10.2: the state word (WP-A16).
  - D33: its sentence on hooks ("carry no question text, fire late, and cover neither the trust dialog nor the pickers") stays true for a question's text and keys and is corrected for timing: PermissionRequest comes 19 to 38 ms before a tool question's rows (WP-A12).
  - Section 7.1 to 7.3: the new folders, the data flow with the two new threads (WP-A7). Section 8: D38, a pointer to section 8 here (WP-A7). Section 11: the risks of section 11 here. Section 12: status. Appendix C: `claudeHooks`, `groundInSessionFile`, `eventCore` (WP-A7).
- `docs/axClaude/claude-screens.md`: the marks (4.1, 4.2 here in short); the ` · summary` row with its one sighting (WP-A21); the spinner's hook note; the rows a failed hook prints, as rows to recognise if they ever appear (WP-A12).
- `docs/axClaude/user-guide.md`: Options, Developer: the two switches and the command (WP-A7); Session: the record's export (WP-A15); one paragraph on what changed in what is heard (WP-A17, WP-A18).
- `docs/axClaude/nvda-test-plan.md`: section 23 (EG-11.6), a step with each package from WP-A7 on.
- `docs/axKit/access-layers.md`, axClaude's section: the two new places, what is read and how (events through the pipe, the session file's lines), what guards them (the timeline fixtures, the replayer, `ClaudeCodePlacesTests`, script 31), and that the neutral pieces move on the second use (WP-A12).
- `PRIVACY.md`: under axClaude, that axClaude asks Claude Code to start `AxKit.exe hook` for eight kinds of events of the session, which tells the window over a pipe on the computer that the event happened; that axClaude reads the session file Claude Code keeps for the conversation, in memory only, to check and order what it shows; that neither sends anything anywhere; under "Programs axKit runs", the hook; under "What stays on your computer", `claude-hooks.json` (WP-A12).
- `README.md`, `docs/axKit/README.md`, `SECURITY.md`: where they state what axKit starts and opens, the same two sentences in their own words; the network statement is unchanged (WP-A12).
- `docs/axKit/SPEC.md`: AX-5.1 gains `claude-hooks.json`; AX-11.11's list of what axKit starts gains the hook; AX-13.5 names it with the AI tools' file as not yet confirmed in the Store copy (WP-A12).
- `docs/axKit/outside-calls.md`: written by `tools/outside-calls.ps1`, which gains the target `Claude Code events` (WP-A4).
- `docs/axKit/mcp.md`: nothing changes; section 1's "no listener" is quoted here.
- `docs/axKit/automation.md`: section 2, the stand-in (WP-A11); section 4, that script 31 is the check of the hooks' start for a new Claude Code (WP-A12).
- `docs/axKit/commands.md`: the probe's commands (WP-A6).
- `docs/axKit/documents.md`: this document, and where events, the record and the fixtures are written (WP-A7).
- `tests/fixtures/README.md`: the timelines folder, and the masking rule beside "never edit bytes" (WP-A6).
- `.claude/skills/record-fixture/SKILL.md`: recording a timeline with `tools/ClaudeProbe` and making the fixture through its whitelist (WP-A6).
- `src/AxClaude.Core/CLAUDE.md`: "Where things are" gains `Hooks`, `Record`, `Flow` and the marks; the gotchas gain: order is file order, never timestamps; a hook must exit 0 and print nothing; the gate's rules in three lines (WP-A7, then each package for its piece).
- `src/AxClaude/CLAUDE.md`: the three new partial files, the queue's rule (the core never runs nested), which timers the core replaced and which stay (WP-A7, then each package).
- `docs/axKit/apps.md` and `docs/axKit/workflows.md` are generated: `tools\apps-page.ps1` and `tools\workflows.ps1` are run after this document and after every new script.
- `CHANGELOG.md`, "## Unreleased": one line with the prefix `axClaude:` for each package that changes what he hears or adds a command (section 10 gives the lines).

## 10. The build plan

Three stages (EG-D-10). Stage 1, the foundation: nothing the user hears changes. Stage 2, sound and state from the core, one family for each commit. Stage 3, grounding. A stage's package lands only when its unit tests and its window scripts pass; what is not solid stays in shadow or behind its switch, off.

### 10.1 Rules for every package

1. One agent builds one package in its own git worktree, from the commit that holds every package of the waves before it, and returns a patch (`git diff --cached --binary`). It does not commit.
2. The patch leaves the repository building and every test passing: `.\test.ps1` before it is returned. A package says which single projects to run while working: `.\test.ps1 AxClaude`, `.\test.ps1 AxKit`, `.\test.ps1 AxTerminal`, `.\test.ps1 AxAutomate`.
3. A package touches only the files its section lists. Packages of one wave never share a file.
4. No package touches `CHANGELOG.md` or a generated page (`docs/axKit/apps.md`, `workflows.md`, `outside-calls.md`). The orchestrating session adds the CHANGELOG line given in the package's section under `## Unreleased` and runs `tools\apps-page.ps1`, `tools\workflows.ps1` and `tools\outside-calls.ps1` at the commit. In its worktree an agent runs those scripts so that `test.ps1` passes, and leaves what they wrote out of its patch.
5. Window scripts run on a desktop of their own: `dotnet run --project tools\AxAutomate -c Release -- tests\scripts\axClaude\<script>.txt --build`, expected last line `PASS: 1 script(s), ...`. Never `nvda-pass.ps1`. A new script's first line is `# Workflow: <its label>`, a label no other script has; the second says what it needs ("Needs no Claude Code, no network and no model", or the live line of `01-startup.txt`).
6. Never build into `src\AxKit\bin`. After a bare `dotnet build` or `dotnet test`: `dotnet build-server shutdown`.
7. A live Claude Code session only where a package says so: one at a time, the model Haiku, a scratch folder, never a folder of the user's, never a settings file of his.
8. Rule 6 of the root `CLAUDE.md`: no path of the account, nothing of a session of the user's. Fixtures only through the whitelist (EG-11.1). Synthetic test files are written by hand with invented texts.
9. Plain code (bundle B-10): records, static methods, one obvious way; comments in the repository's voice saying why; every pipe and standard-input call with its `Outside call` comment (EG-10.2). PowerShell only through the PowerShell tool; source files through the Write tool.
10. `<design>` stands for `%USERPROFILE%\AppData\Local\Temp\claude\4d1aa98d0cd8ebcc\46368cbc-93dc-4f7e-972b-6d2d2495d133\scratchpad\design`, the folder of the night of the build, with the probe (`<design>\probe`, its `README.md`) and the scratch recordings (`<design>\runs`). It is not kept; what stays are the fixtures made from it.
11. The item line of the commit: `Item: speech order breaks` for stages 1 and 2; `Item: check the conversation against Claude Code's own session file` for the reader's packages and stage 3. Each section says which.
12. Where a package cannot make its behaviour solid, it returns what is solid with the family left in shadow or its switch off, and says so; it never returns a failing test or a script it had to weaken.

### 10.2 The waves

Packages of one wave can be built at the same time. A wave starts when the waves before it are committed.

| Wave | Packages | Stage |
| --: | :-- | :-- |
| 1 | WP-A1, WP-A2, WP-A3, WP-A4, WP-A5, WP-A6 | 1 |
| 2 | WP-A7, WP-A8, WP-A9, WP-A10, WP-A11 | 1 |
| 3 | WP-A12, WP-A13, WP-A14 | 1 |
| 4 | WP-A15, WP-A16 | 3 (the export), 2 |
| 5 | WP-A17 | 2 |
| 6 | WP-A18 | 2 |
| 7 | WP-A19 | 2 |
| 8 | WP-A20 | 3 |
| 9 | WP-A21 | 3 |
| 10 | WP-A22 | 3 |
| 11 | WP-A23 | 3 |

WP-A0 (Claude Code's live progress rows as interface rows, `LineKind.Progress`, a workflow counted as background work, the option that shows the rows) is built and committed (1a44032). Everything here builds on it.

Packages that change what the user hears: WP-A16 to WP-A23. WP-A7 and WP-A15 add commands and change nothing that exists. WP-A12 changes nothing he hears unless EG-2.5's fallback applies.

### WP-A1 The marks and three facts of the model

- Stage 1, wave 1, about 30 minutes. Depends on nothing. Item: speech order breaks.
- Goal: EG-1. The model raises the marks and says what the screen shows of Claude's work, with nothing else changed.
- Changes `src/AxClaude.Core/Transcript/SessionModel.cs`:
  - `public enum ScreenWork { Unknown, Idle, Working }` above the class.
  - `public event Action<char>? Marked;` In `AttachScreen`: `screen.Mark += OnMark;` with `private void OnMark(char letter, string? rest)` raising `Marked` for `A`, `C` and `D` only.
  - `public ScreenWork Work { get; private set; }`, set in `CompleteFrame` right after `Working = working`: `Working` when `working`, else `Idle` when `mode` is not null, else `Unknown`; `Unknown` in `StartRun`.
  - `public int Epoch { get; private set; }`, raised by one in `StartRun` and in `ClearConversation`.
  - `public bool? TurnOpen { get; set; }` and `private bool Busy => TurnOpen ?? Working;` used in place of `Working` in `Send` (the block is held), in `PlaceBlocksWithoutEcho`, and in the expiry at the top of `CompleteFrame`. Nowhere else.
- Changes `src/AxKit.Core/Vt/Screen.cs`: the comment on `Mark` only ("axClaude listens too: its turn marks").
- Creates `tests/AxClaude.Tests/TurnMarkTests.cs`:
  - `Marks_the_bell_and_frames_come_in_stream_order`: feeds `ESC]133;A ESC\`, a bracketed frame, `ESC]133;C ESC\ ESC]133;D ESC\ BEL`, a frame; a list filled by `Marked`, `Bell` and `Changed` reads A, frame, C, D, bell, frame.
  - `The_conversation_fixture_gives_A_then_C_D_and_the_bell_for_each_turn` (`conversation-claude2.1.277-240x50.vt`).
  - `The_permission_fixture_gives_a_bell_alone_for_the_question` (`permission-prompt-claude2.1.281-240x50.vt`: A, a bell with no D since the A, then C, D, bell).
  - `The_model_picker_fixture_gives_no_mark_and_no_bell`.
  - `Work_is_unknown_without_a_mode_line_idle_with_one_and_working_with_a_spinner`.
  - `Epoch_rises_on_a_restart_a_new_session_and_clear`.
  - `With_TurnOpen_true_a_sent_message_is_held_though_the_screen_shows_no_work`, `With_TurnOpen_false_a_message_is_placed_though_the_screen_shows_a_spinner`.
- Verify: `.\test.ps1 AxClaude` passes, `FixtureTests` and `ReplayTests` unchanged (EG-1.6); `.\test.ps1 AxTerminal` passes.
- Must not touch: any other file; no golden file changes.
- Hears: nothing. Fallback: none needed.

### WP-A2 The session file's entries

- Stage 1, wave 1, about 45 minutes. Depends on nothing. Item: check the conversation against Claude Code's own session file.
- Goal: EG-3.1, EG-3.4, EG-3.5, EG-3.7, EG-3.8: the parser, the neutral record, the keys. No file reading yet.
- Creates `src/AxClaude.Core/Record/RecordEntry.cs` (6.3), `RecordText.cs` (`public static string Letters(ReadOnlySpan<char> text)`), `ClaudeCodeTranscript.cs` (7.1: `ProjectFolderName`, `ProjectsFolders`, `IsSessionFile`, `Parse`, `ScreenKey`; the names of the types read and of every field as constants in this one file).
- `Parse` reads with `Utf8JsonReader` in two passes over the line's bytes: the first finds the top-level `type` and skips every other value with `Skip()`; only for `user`, `assistant`, `system` and `queue-operation` a second pass reads the fields of EG-3.4. No `JsonDocument` of a whole line, no string made of a field the table does not name.
- Creates `tests/fixtures/records/` with files written by hand, texts invented: `plain-turn.jsonl`, `tool-turn.jsonl`, `interrupt.jsonl`, `queued.jsonl`, `odd-lines.jsonl`, and a `README.md` of that folder saying so (`tests/fixtures/README.md` itself is WP-A6's). Lines of skipped kinds, and the fields a read kind must not read (a tool's input, a result's text, `cwd`), hold the words `MUST-NOT-BE-READ`.
- Creates `tests/AxClaude.Tests/ClaudeCodeTranscriptTests.cs`:
  - `A_typed_prompt_a_reply_and_the_turn_end_are_read_in_file_order`
  - `One_entry_for_each_content_block_text_tool_call_thinking`
  - `A_tool_result_carries_its_calls_id_and_error_flag_and_never_its_text`
  - `Skipped_kinds_return_null_and_nothing_of_their_text` (every entry parsed from every file, printed with `ToString`, never holds `MUST-NOT-BE-READ`)
  - `Property_order_does_not_matter`
  - `A_line_that_is_no_json_is_counted_not_thrown`, `A_missing_uuid_gets_a_local_id_from_the_offset`, `An_unknown_block_or_subtype_is_Other`
  - `Queue_operations_are_read_with_the_enqueued_text`
  - `An_interrupt_and_a_cut_reply_are_told`
  - `A_compact_summary_a_meta_entry_and_a_command_are_no_prompts`
  - `A_notification_prompt_is_one_Claude_Code_started_itself`
  - `The_project_folder_name_replaces_every_other_character_by_a_hyphen` (`C:\Temp\axclaude-fixture` gives `C--Temp-axclaude-fixture`)
  - `A_session_file_lies_directly_in_a_project_folder_of_a_projects_folder` (a `subagents` path, a `..`, another extension, another root are refused; the case of the root is not minded)
  - `ProjectsFolders_follow_the_configuration_folder_and_add_a_test_runs_own`
- Creates `tests/AxClaude.Tests/RecordTextTests.cs`: `Letters_keeps_letters_and_digits_in_lower_case`, `The_screen_key_drops_fence_lines`, `The_screen_key_reads_a_table_as_rows_of_header_and_cell`, `A_link_whose_text_is_its_target_counts_once`, `Every_screen_row_of_the_markdown_sample_is_found_in_the_key_in_order` (an invented source with a heading, a list, a code block, a table, a link, a quote and a paragraph of 700 characters, and the rows 4.3's rules give for it at 240 columns).
- Verify: `.\test.ps1 AxClaude`.
- Must not touch: any existing file.
- Hears: nothing.

### WP-A3 The hooks' table, the settings file, the events; the trial of `async`

- Stage 1, wave 1, about 45 minutes. Depends on nothing. Item: speech order breaks.
- Goal: EG-2.1, EG-2.2 (the text), EG-2.4 (`Supports`), EG-2.5, EG-2.9 (parsing). First the trial, then the code.
- The trial, at most 20 minutes, two live sessions one after the other (rule 7), with the probe as built (`<design>\probe\bin\probe.exe`, its README; do not rebuild it): a configuration `<design>\runs\r4\async.json` with the working folder `<design>\runs\r4\async`, the hook plan `"default": "none"`, `"events"` naming the eight events of EG-2.1 as `"command"`, `"commandForm": "exec"`, `"async"` listing all eight, `"timeoutSeconds"` 5 for each; the script: the trust answer, the message "Reply with the single word ok.", wait for the Stop hook and for ready, the same message again, `/exit`. Then `<design>\runs\r4\sync.json`, the same without `"async"`. From `probe timeline` and `probe shape` of both, answer and write into section 4.3 of this document, in place of "Not yet measured", in numbers:
  1. Did Claude Code start without a "Settings Warning", and did SessionStart, Stop and SessionEnd arrive in both runs?
  2. Did any row of the screen name a hook in the async run (`running`, `hook`, `completed`), in any frame, also in the second turn?
  3. The time from Stop's process being created to OSC 133 D, async against sync.
  4. Is there a `stop_hook_summary` entry in the session file in the async run?
  Where the answer to 1 is no for `async`, or to 2 is yes, that event's `Async` is false in the table, and EG-2.5 is amended as it says.
- Creates `src/AxClaude.Core/Hooks/HookEvent.cs` (6.2) and `ClaudeCodeHooks.cs` (7.1): the table `Events` (`HookSpec(Name, VerifiedWith, Async)`), `Supports`, `SettingsText(program, verb)`, `WriteSettings`, `SessionArguments`, `UserPassesSettings`, `Parse`; every event name and field name of Claude Code's as a constant in this one file.
- Creates `tests/AxClaude.Tests/ClaudeCodeHooksTests.cs`:
  - `The_table_names_the_eight_events_each_with_the_version_it_was_verified_with`
  - `The_settings_text_has_one_exec_command_hook_for_each_event_and_nothing_else` (parsed: the keys are the table's names; each has `type` command, the program, `args` of the one verb, `timeout` 5, `async` as the table says; no matcher, no `shell`)
  - `A_program_path_with_spaces_and_backslashes_survives_the_json`
  - `WriteSettings_writes_only_when_the_text_differs`
  - `Supports_needs_at_least_the_tables_version` (`2.1.325 (Claude Code)` yes; `2.1.324 (Claude Code)` no; `2.2.0` and `3.0.1 (Claude Code)` yes; null, empty, `0.0.0 (stand-in)`, words: no)
  - `The_users_own_settings_argument_is_seen` (`--settings x`, `--settings=x`)
  - `A_stop_event_gives_the_last_message_and_the_listed_tasks`, `A_session_start_gives_the_id_the_file_and_the_source`, `Subagent_events_give_the_workers_id_and_type`, `Permission_notification_and_session_end_give_their_one_field`
  - `An_unknown_event_or_a_body_that_is_no_object_is_null`
  - `Fields_axClaude_does_not_use_are_not_kept` (a body whose `tool_input` holds `MUST-NOT-BE-READ`)
- Changes `docs/axClaude/events-and-grounding.md`: 4.3's last item, and EG-2.5 if the trial says so. No other package of wave 1 touches this document.
- Verify: `.\test.ps1 AxClaude`.
- Must not touch: `Program.cs`, `ClaudeLauncher.cs`, `AppPaths.cs`, `Hooks/HookFrame.cs`, `Hooks/HookRelay.cs` (WP-A4's).
- Hears: nothing.

### WP-A4 The hook verb, the pipe's frame, the launcher's variable

- Stage 1, wave 1, about 45 minutes. Depends on nothing. Item: speech order breaks.
- Goal: EG-2.6, EG-2.7 (the frame), EG-2.8, EG-10.2.
- Creates `src/AxClaude.Core/Hooks/HookFrame.cs` and `HookRelay.cs` (7.1, 6.2). `HookRelay.Run()`: a watchdog thread that calls `Environment.Exit(0)` after 2 s; `Console.OpenStandardInput()` read to its end or to `HookFrame.MaxBody` plus one byte; `Relay(...)` with a connect that makes `new NamedPipeClientStream(".", name, PipeDirection.Out)` and connects within 500 ms; everything inside one `try` that swallows every exception; nothing written to standard output or error; always exit code 0. Both pipe calls and the standard input carry the comments of EG-10.2.
- Changes `src/AxKit/Program.cs`: `Main`'s first statement decides the verb (7.1); the rest of `Main` moves unchanged into `private static void Start(string[] args)`; `[STAThread]` stays on `Main`.
- Changes `src/AxKit.Core/AppPaths.cs`: `ClaudeHooksFile` beside `McpClaudeConfigFile`, the same folder.
- Changes `src/AxClaude.Core/Pty/ClaudeLauncher.cs`: `BuildEnvironment(string? hookPipe = null)` (EG-2.8).
- Changes `tools/outside-calls.ps1`: the target `Claude Code events`, kind pipe, with its What, How and What it reaches lines in the words of EG-10.1 and EG-10.2.
- Changes `src/AxKit/CLAUDE.md`: one paragraph on the verb and why it is decided first.
- Creates `tests/AxClaude.Tests/HookFrameTests.cs`: `A_frame_written_is_read_back`, `A_wrong_secret_is_refused_and_the_body_not_read`, `A_body_over_the_limit_is_refused`, `A_stream_that_ends_early_is_broken_not_thrown`, `The_variables_value_is_split_at_its_first_semicolon`, `A_missing_or_malformed_variable_is_refused`, `Pipe_names_and_secrets_differ_from_call_to_call`.
- Creates `tests/AxClaude.Tests/HookRelayTests.cs`: `Standard_input_is_relayed_as_one_frame`, `Without_the_variable_nothing_is_connected_and_nothing_thrown`, `A_connection_that_fails_is_swallowed`, `Input_over_the_limit_is_not_sent`, `A_real_pipe_carries_an_event_end_to_end` (a `NamedPipeServerStream` of the test under a random name).
- Changes `tests/AxClaude.Tests/ClaudeLauncherTests.cs`: `The_child_environment_carries_the_hook_pipe_when_given_and_never_an_inherited_one`.
- Verify: `.\test.ps1 AxClaude`; `.\test.ps1 AxKit`; `dotnet build src\AxKit\AxKit.csproj -c Release -o <a scratch folder>`, then in PowerShell `'{}' | & '<scratch>\AxKit.exe' hook; $LASTEXITCODE` prints nothing but `0` within 2 s and shows no window; `.\tools\outside-calls.ps1` lists the verb's two calls under "Claude Code events" (the window's pipe joins them with WP-A12).
- Must not touch: `Hooks/ClaudeCodeHooks.cs`, `Hooks/HookEvent.cs` (WP-A3's); `Dispatch.cs` (the verb is decided before `Dispatch`).
- Hears: nothing.

### WP-A5 The core, part 1: the records, the turn, the state, Done, time

- Stage 1, wave 1, about 50 minutes. Depends on nothing. Item: speech order breaks.
- Goal: EG-4.1 to EG-4.10 but for the gate and the background, EG-5.1, EG-5.2, EG-5.6, EG-6.6 (the count), with the constants of section 5.
- Creates `src/AxClaude.Core/Flow/FlowInput.cs` and `FlowDecision.cs` (6.4, 6.5, each decision with `ToLine()`), `SourceHealth.cs` (the counters of EG-9.4 as public fields; `Lines(...)` and `Said(...)` giving the texts of EG-9.3 and EG-9.4), `SessionFlow.cs`, `SessionFlow.Gate.cs`, `SessionFlow.Background.cs`.
- `SessionFlow.cs` holds `Handle` with one `switch` over the input's type and everything of EG-4.6, EG-4.7, EG-4.9, EG-4.10, EG-5.6. It calls into the two other files through private methods whose bodies this package writes in their simplest form:
  - `SessionFlow.Gate.cs`: `private void GateFrame(FrameSeen frame, long now)`, `private void GateEntry(EntryRead entry, long now)`, `private void GateStop(TurnStopped stop, long now)`, `private void GateTick(long now)`, `private void GateBump()`, `private void GateReset()`, `private bool GatePending { get; }`. Here: a proposed line is released at once while a turn is open or ending and dropped outside one (EG-5.2); nothing is ever pending. WP-A8 writes the gate.
  - `SessionFlow.Background.cs`: `private void BackgroundStop(TurnStopped stop)`, `private void BackgroundWorker(string id, string type, bool started)`, `private void BackgroundReset()`, and `Tasks` empty. WP-A9 writes them.
- Changes `src/AxClaude.Core/Transcript/Arrivals.cs`: `ProposedKind`, `Proposed` (6.4); `Arrival` gains `IReadOnlyList<Proposed> Proposed`, filled beside `SpokenLines` with each spoken line's head id and kind. `SpokenLines` and `Speech` stay as they are.
- Creates `tests/AxClaude.Tests/SessionFlowTests.cs`, each test a list of inputs with times and the decisions' `ToLine()`:
  - `A_turn_opens_at_A_and_the_state_is_working`
  - `D_with_its_bell_ends_the_turn_and_Done_follows_the_frame_that_adds_a_line`
  - `D_without_a_bell_closes_the_turn_and_says_nothing` (a tick at 150)
  - `A_bell_alone_is_an_attention_bell_and_the_turn_stays_open`
  - `A_C_D_at_once_without_a_bell_says_nothing`
  - `Send_now_then_D_and_the_bell_is_a_cut_without_Done`
  - `Done_waits_for_the_stop_event_or_the_records_end_or_300_ms`
  - `Done_comes_600_ms_after_the_bell_at_the_latest`
  - `Done_is_not_dropped_by_a_new_generation`
  - `A_turn_opened_by_A_alone_closes_as_stale_after_two_seconds_of_an_idle_screen`
  - `Without_marks_the_screen_opens_and_closes_the_turn_and_the_bell_is_an_attention_bell`
  - `After_an_end_by_marks_the_screens_spinner_does_not_reopen_for_400_ms`
  - `A_prompt_entry_opens_a_turn_when_no_mark_did`
  - `The_records_turn_end_closes_a_turn_whose_D_was_missed`
  - `The_process_end_closes_everything_and_drops_Done`
  - `A_new_epoch_forgets_the_turn`
  - `The_state_stays_working_between_a_turn_and_a_waiting_message`
  - `Every_due_time_is_asked_for_with_WakeAt`
  - `User_actions_a_shown_question_and_a_permission_event_raise_the_generation`
  - `A_session_named_asks_to_follow_its_file_and_clear_asks_again`
  - `Enqueued_minus_dequeued_is_the_waiting_count`
  - `A_proposed_line_outside_a_turn_is_dropped`
  - `Decisions_print_as_numbers_and_fixed_words_only`
- Changes `tests/AxClaude.Tests/ArrivalsTests.cs`: `Each_proposed_line_names_its_head_and_its_kind`; the existing tests unchanged.
- Verify: `.\test.ps1 AxClaude`.
- Must not touch: `SessionModel.cs`, `Record/`, `Hooks/`.
- Hears: nothing.

### WP-A6 The probe as a tool, the whitelist, the timeline fixtures

- Stage 1, wave 1, about 45 minutes. Depends on nothing. Item: speech order breaks.
- Goal: EG-11.1, EG-11.4.
- Creates `tools/ClaudeProbe/`: the files of `<design>\probe\src` (`Config.cs`, `HookListener.cs`, `ProcessTree.cs`, `Program.cs`, `PtyWatch.cs`, `Runner.cs`, `Timeline.cs`, `TranscriptWatcher.cs`, `Util.cs`) as they are; `ClaudeProbe.csproj` from `Probe.csproj` with the project references `..\..\src\AxKit.Core\AxKit.Core.csproj` and `..\..\src\AxClaude.Core\AxClaude.Core.csproj`; `README.md` from `<design>\probe\README.md` without the paths of that night, with the rules of its section 1, the statement of EG-D-15, and the `fixture` command; `FixtureMaker.cs` and its command in `Program.cs`: `ClaudeProbe fixture <run folder> <fixture folder>` (EG-11.1, 6.6). Of a pair of the same event by both transports it keeps one. `sends.jsonl` comes from the run's `script.jsonl`: a message as a message, Escape as the key `Escape`, Ctrl+X followed by Ctrl+S as the key `SendNow`, every other key by its name.
- Creates `tests/fixtures/timelines/README.md` and the fixtures, each made by the tool from a run folder under `<design>\runs`:

  | Fixture | From | Shows |
  | :-- | :-- | :-- |
  | `plain-turns` | `r1\a-out` | three plain turns |
  | `no-hooks` | `r1\g-out` | the same with no hooks at all |
  | `tool-question` | `r1\b1b-out` | a permission question: a bell alone, the answer, the turn's end |
  | `text-tool-text` | `r1\b2e-out` | a turn of two messages with a tool call between |
  | `long-reply` | `r1\cd-out` | a reply of 40 lines, and the Markdown sample |
  | `queued-one` | `r2\a1-out` | one message queued while a reply streamed |
  | `queued-two` | `r2\a2-out` | two queued messages, one turn |
  | `send-now` | `r2\a4-out` | a reply cut by Ctrl+X Ctrl+S |
  | `interrupts` | `r2\b1-out` | three interrupts with Escape |
  | `questions` | `r2\c1-out` | Claude's own questions, one, two, several answers |
  | `background-shell` | `r2\d1-out` | a shell in the background and its end |
  | `background-agents` | `r2\e1-out` | two agents in the background, their hand-backs |
  | `continue` | `r3\c2-out` | `--continue`: the old conversation redrawn before SessionStart |
  | `clear` | `r3\d0-out` | `/clear`: a new session id and file |
  | `compact` | `r3\e-out` | `/compact` |

  `r1\f-out` is tried too and must be refused: its console stream holds what `/status` printed. A run the tool refuses is left out and named in what the package returns; nothing is recorded again tonight.
- Changes `tests/fixtures/README.md` (the timelines, and that a timeline is masked by the tool, never by hand), `.claude/skills/record-fixture/SKILL.md` (a section "A timeline with events and the session file": record with `tools/ClaudeProbe run`, read with `timeline`, make the fixture with `fixture`), `docs/axKit/commands.md` (the tool's commands).
- Verify: `dotnet build tools\ClaudeProbe -c Release -o <a scratch folder>` builds; each `fixture` run prints its counts and "written", or its refusal; `.\test.ps1 AxClaude` passes (`PrivacyTests` reads the new files); a search of `tests\fixtures\timelines` for the account's name finds nothing; `git status` shows only the paths above; `dotnet build-server shutdown`.
- Must not touch: `src/`, `tests/AxClaude.Tests/`.
- Hears: nothing.

### WP-A7 The core in the window, in shadow

- Stage 1, wave 2, about 55 minutes. Depends on WP-A1, WP-A2, WP-A3, WP-A5. Item: speech order breaks.
- Goal: EG-4.8, EG-4.10, EG-4.11, EG-9.1 to EG-9.6. Every input reaches the core. Carried out whatever the charge: `WakeAt` (the timer), `Note` (the log), `FollowRecord` and `ForgetRecord` (through the partial methods), and the counters. Every other decision only goes to the shadow.
- Creates `src/AxClaude.Core/Flow/FlowFeed.cs` and `FlowShadow.cs` (7.1). `FlowFeed.FromHook` gives `SessionNamed`, `TurnStopped` (the key is `ClaudeCodeTranscript.ScreenKey` of the last message, null for an empty one), `TurnFailed`, `WorkerStarted`, `WorkerStopped` (null for an empty worker type), `AttentionAsked`, `SessionEnded`. `FlowFeed.FromEntry` gives `EntryRead` with the kinds of 6.4 (a Prompt's key is the letters of its text; `Removed` counts as `Dequeued`; `Note`, `Thinking`, `CompactSummary` are `Other`; `CompactBoundary` is `Compact`).
- Creates `src/AxClaude/MainForm.Flow.cs`: `_health`, `_flow`, `_shadow`, the queue, `EnqueueFlow(FlowInput? input)`, the drain loop with its guard against running nested, `Carry(FlowDecision decision)`, the one timer for `WakeAt`, `[Flags] private enum FlowCharge { None = 0, State = 1, Done = 2, Speech = 4, Background = 8 }`, `private const FlowCharge Built = FlowCharge.None;`, `private bool FlowDecides(FlowCharge family)` (false for every family while `_settings.EventCore` is `shadow`), `SayEventSources()`, `FlowDiagnostics()`, the shadow's one log line for each turn, and the partial methods of 7.1, called and not yet filled.
- Changes, a few lines each:
  - `src/AxClaude/MainForm.cs`: `_model.Marked` and `_model.Bell` also queue their inputs; `DiagnosticsLines` ends with `.. FlowDiagnostics()`.
  - `src/AxClaude/MainForm.Model.cs`: `OnTranscriptChanged` queues `FlowFeed.FromFrame(...)` right after `_arrivals.Update`; where the old code says Done and speaks held lines it tells `_shadow`.
  - `src/AxClaude/MainForm.Process.cs`: after `StartProcess` succeeded, `ProcessStarted` and `RecordProcessStarted()`; in `StopProcess` and `OnProcessExited`, `ProcessEnded` and `RecordProcessEnded()`.
  - `src/AxClaude/MainForm.Input.cs`, `MainForm.Area.cs`, `MainForm.PutAway.cs`: `UserActed` where a message is sent (with its key, and `RecordSent(key)`), an answer given, Escape or an interrupt sent, Send now, a screen key, the window put away; `QuestionShown` in `ShowInArea`.
  - `src/AxClaude/MainForm.Ui.cs`: Options, Developer gains the two switches (EG-9.1, EG-9.2; the second calls `RecordSwitch`) and Say the event sources (EG-9.3).
  - `src/AxClaude.Core/AppSettings.cs`: `ClaudeHooks`, `GroundInSessionFile`, `EventCore`; `tests/AxClaude.Tests/SettingsTests.cs`: their defaults and their names in the file.
- Creates `tests/AxClaude.Tests/FlowFeedTests.cs` (`A_frame_carries_the_models_work_epoch_and_proposals`, `Letters_A_C_D_become_marks_and_others_nothing`, `Each_hook_kind_becomes_its_neutral_input`, `Each_record_kind_becomes_its_neutral_input`) and `FlowShadowTests.cs` (`A_turn_where_both_say_Done_gives_the_two_times`, `A_Done_only_the_window_said_is_counted`, `Lines_one_spoke_and_the_other_did_not_are_counted`, `The_line_holds_numbers_and_fixed_words_only`).
- Creates `tests/scripts/axClaude/28-event-sources.txt`, label "Hear where axClaude's events come from": a batch stand-in as in `27-workflow-progress.txt` that also writes the marks (`%E%]133;A%E%\`) and a bell (the character written `\u0007` in the `file` step). Ready; the two switches are in the command list, checked; `menu Say the event sources` gives `! Events. Marks: 0`; a turn (A, a working frame; then C, D, the bell and the reply's frame) gives "Done" by the old rule; the command then says `Marks: 3`, `Hooks: not passed`, `Session file: not found`, `Core: in shadow`.
- Documents in the same patch: `docs/axClaude/SPEC.md` (FR-15, FR-12.3, 4.4, 7.1 to 7.3, D38, Appendix C; section 9), `docs/axClaude/user-guide.md` (the Developer items), `docs/axClaude/nvda-test-plan.md` (section 23, step 1), `docs/axKit/documents.md`, `src/AxClaude.Core/CLAUDE.md`, `src/AxClaude/CLAUDE.md`.
- Verify: `.\test.ps1`; scripts 28, 22 and 27 pass.
- Must not touch: `Flow/SessionFlow*.cs`, `Record/RecordTail.cs`, `Record/RecordFinder.cs`, `tools/`, `BackgroundWork.cs`.
- Hears: nothing that exists changes; one command and two switches are new. CHANGELOG: `axClaude: Options, Developer gains Events from Claude Code's hooks, Ground the conversation in Claude Code's session file and Say the event sources (docs/axClaude/events-and-grounding.md).`

### WP-A8 The gate

- Stage 1, wave 2, about 45 minutes. Depends on WP-A5, WP-A2. Item: speech order breaks.
- Goal: EG-5.3, EG-5.4, EG-5.5, EG-5.7, EG-5.9, and Done's first condition.
- Changes `src/AxClaude.Core/Flow/SessionFlow.Gate.cs` only: the bodies of the methods WP-A5 declared. State: the reply records (id, key, how far matched, provisional or not, taken or not), the heads by line id (which record, or none yet), the pending lines (line, head id, kind, when proposed, the generation). After every proposal, every ReplyText entry and every stop with a last message, pending lines are tried in the order they were proposed, by the rules of EG-5.3. A Prompt entry closes every record read before it. `RecordText.Letters` gives the letters; the head's label is taken off with `ClaudeCodeTexts.ClaudeLabel`.
- Creates `tests/AxClaude.Tests/SpeechGateTests.cs`:
  - `Without_a_record_a_line_is_released_after_250_ms_inside_a_turn`
  - `A_head_waits_for_its_entry_and_is_released_when_it_arrives`
  - `The_stop_events_last_message_confirms_the_head_before_the_entry`
  - `The_entry_that_follows_takes_the_provisional_records_place`
  - `A_head_no_entry_confirms_is_dropped_at_500_ms`
  - `An_entry_releases_once` (a second head with the same text, a repaint, is dropped)
  - `An_entry_read_before_a_later_prompt_is_never_a_candidate`
  - `Rows_under_a_head_are_released_when_their_letters_follow_in_the_key`
  - `A_row_that_is_not_in_the_entry_is_dropped` (a question's rows under a reply)
  - `A_wrapped_row_and_a_table_row_are_found`
  - `A_summary_of_an_earlier_reply_is_dropped`
  - `A_reply_of_two_letters_is_confirmed_only_at_an_entrys_start`
  - `Error_rows_and_tool_rows_are_released_at_once`
  - `A_new_generation_drops_what_is_pending_and_counts_it`
  - `Released_lines_of_one_input_are_one_decision_in_line_order`
  - `Done_waits_for_a_pending_head_and_follows_its_release`
  - `With_reply_speech_off_nothing_is_pending`
  - `Every_number_is_counted_in_the_health`
- Verify: `.\test.ps1 AxClaude`; `SessionFlowTests` unchanged and passing.
- Must not touch: `SessionFlow.cs`, `SessionFlow.Background.cs`, the window.
- Hears: nothing (the window does not carry the gate's decisions out yet).

### WP-A9 Background evidence in the core

- Stage 1, wave 2, about 30 minutes. Depends on WP-A5. Item: speech order breaks.
- Goal: EG-7.1 to EG-7.3 as pure code.
- Changes `src/AxClaude.Core/Flow/SessionFlow.Background.cs` (the list, `BackgroundListed` when it changes, emptied at a new epoch and at the process's end) and `src/AxClaude.Core/Transcript/BackgroundWork.cs`: `Count(string? modeLineCounts, int agentsShown, int agentsAwaited, int workflowsShown = 0, int workflowsAwaited = 0, int unknownShown = 0, int listedAgents = 0, int listedShells = 0, int listedOther = 0, bool modeLineAfterStop = true)` and `Said(BackgroundCount count, IReadOnlyList<BackgroundAgent> agents, IReadOnlyList<BackgroundWorkflow>? workflows = null, IReadOnlyList<FlowTask>? listed = null)`.
- Creates `tests/AxClaude.Tests/BackgroundEvidenceTests.cs`: `A_stop_replaces_the_list_with_its_running_tasks`, `A_workers_stop_takes_it_off_the_list`, `A_workers_start_adds_nothing`, `A_new_epoch_or_the_process_end_empties_the_list`. Changes `tests/AxClaude.Tests/BackgroundWorkTests.cs`: `Listed_agents_are_one_more_place_never_a_sum`, `Listed_shells_count_until_a_mode_line_was_read_after_the_stop`, `Listed_tasks_of_other_types_count_as_other`, `What_runs_names_a_listed_task_the_rows_do_not_name`; every existing test unchanged.
- Verify: `.\test.ps1 AxClaude`.
- Must not touch: `SessionFlow.cs`, `SessionFlow.Gate.cs`, `MainForm.Background.cs`.
- Hears: nothing.

### WP-A10 The record's tail and the folder rule

- Stage 1, wave 2, about 45 minutes. Depends on WP-A2. Item: check the conversation against Claude Code's own session file.
- Goal: EG-3.2 (the rule), EG-3.3, EG-3.6, as classes a thread can drive; no thread here.
- Creates `src/AxClaude.Core/Record/RecordTail.cs` and `RecordFinder.cs` (7.1). `RecordTail.Poll()` opens the file anew each time with `FileShare.ReadWrite | FileShare.Delete`, reads what is new, and returns the entries; it keeps the remainder after the last LF, the seen ids, the counters (`Read`, `Skipped`, `NotJson`, `TooLong`) and the state. `RecordFinder.Note` lists `*.jsonl` directly in each project folder with their lengths; `Find` reads what each gained since and returns the path whose new Prompt entries hold one of the keys given.
- Creates `tests/AxClaude.Tests/RecordTailTests.cs`: `Lines_appended_are_returned_once_in_order`, `A_line_without_its_line_end_waits`, `A_line_over_4_MB_is_passed_over_and_counted`, `A_file_that_shrank_is_read_again_and_seen_entries_are_dropped`, `A_file_that_does_not_exist_yet_is_followed_once_it_does`, `Reading_starts_at_the_offset_given`, `Three_lines_that_are_no_json_stop_the_reader`, `More_than_ten_of_fifty_entries_without_their_fields_stop_the_reader`, `The_file_is_read_while_another_handle_writes_it`, `ReadAll_reads_a_whole_file`. And `RecordFinderTests.cs`: `The_file_that_gained_this_windows_prompt_is_found`, `A_file_that_grew_with_another_prompt_is_not_taken`, `A_new_file_is_found_as_well_as_one_that_grew`, `Files_in_subfolders_are_never_looked_at`, `Nothing_sent_finds_nothing`.
- Verify: `.\test.ps1 AxClaude`.
- Must not touch: `ClaudeCodeTranscript.cs`, the window.
- Hears: nothing.

### WP-A11 The stand-in

- Stage 1, wave 2, about 45 minutes. Depends on WP-A3 (the settings file's shape). Item: speech order breaks.
- Goal: EG-11.3, 6.7.
- Creates `tools/AxAutomate/StandIn.cs`: `internal static int Run(string[] args)`; it reads the scenario with `System.Text.Json`, finds `--settings <file>` among Claude Code's arguments, sets the console's output to UTF-8, and plays the steps of 6.7. It never reads standard input. A hook's process is started without a shell and without a window, the event on its redirected standard input.
- Changes `tools/AxAutomate/Program.cs`: at the top of `Main`, `if (args.Length > 0 && args[0] == "standin") { return StandIn.Run(args[1..]); }`; before the first script runs, `Environment.SetEnvironmentVariable("AXKIT_STANDIN", Environment.ProcessPath);`; the header comment gains the mode. Nothing else: project B may change this file too, so the hunk stays this small.
- Creates `tests/scripts/axClaude/29-stand-in-turn.txt`, label "Follow a turn played by the stand-in": the scenario of 6.7 with Speak replies all; Ready; a message, "Sent"; the file `go1`; `expect state Claude: working`; `expect said claude: ok`; `expect said Done`; `expect text claude: ok`; `run findstr /s /c:"turn_duration" home\.claude\projects\*.jsonl`.
- Changes `docs/axKit/automation.md` section 2: the stand-in, its scenario, `AXKIT_STANDIN`.
- Verify: script 29 passes; scripts 22 and 27 pass; `.\test.ps1 AxAutomate`.
- Must not touch: `src/AxAutomate.Core`, `src/`.
- Hears: nothing.

### WP-A12 Hooks in the window

- Stage 1, wave 3, about 50 minutes. Depends on WP-A3, WP-A4, WP-A7, WP-A11. Item: speech order breaks.
- Goal: EG-2.2 to EG-2.5, EG-2.7, EG-2.9 to EG-2.12, EG-10.
- Creates `src/AxClaude/MainForm.Hooks.cs`:
  - `internal sealed class HookChannel`: `public static HookChannel Start(Control owner, Action<HookEvent> deliver, SourceHealth health)`, `public string VariableValue { get; }`, `public void Stop()`. Its thread ("AxKit hooks") serves the pipe as `McpChannel.Serve` does (a failure counted, never thrown, the pipe made again after a pause), with `NamedPipeServerStreamAcl.Create(name, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, UserPipe.Security())` under its `Outside call` comment, `HookFrame.Read`, `ClaudeCodeHooks.Parse`, and one post for a burst.
  - In `MainForm`: what `StartClaude` needs (whether hooks are wanted and why not, the file written, the arguments) and `OnHookEvent(HookEvent e)` on the UI thread: the session id's check and the path's check of EG-2.9 (`ClaudeCodeTranscript.IsSessionFile` against `ProjectsFolders(...)`, the run's folder in a test run), the counters, then `EnqueueFlow(FlowFeed.FromHook(e))`.
- Changes `src/AxClaude/MainForm.Process.cs`: `StartClaude` waits for the version at a window's first start (EG-2.4: the wait carries a token, and its continuation does nothing when the window is gone, Claude runs, or another start came meanwhile); builds the line with `--settings` (EG-2.3), and without it when that line is refused; starts the channel right before `StartProcess` and hands `ClaudeLauncher.BuildEnvironment(channel?.VariableValue)`; stops the channel when the start fails and in `StopProcess`.
- Creates `tests/scripts/axClaude/30-hooks-stand-in.txt`, label "See Claude Code's events arrive, without Claude Code": a stand-in of version `2.1.325 (stand-in)` plays SessionStart and one turn with Stop; `Say the event sources` gives `Hooks: on, 2 received`; a second start in another folder with version `2.1.324 (stand-in)` gives `Hooks: not passed, version`; with the switch off, `Hooks: off`.
- Creates `tests/scripts/axClaude/31-hooks-live.txt`, label "Start Claude Code with axClaude's hooks", the live line as its needs: a fresh folder, the trust answer, Ready; `refuse text Settings Warning`; "Reply with the single word ok."; `expect said Done`; `Say the event sources` gives `Hooks: on`, and `refuse said none received`.
- Documents in the same patch: `PRIVACY.md`, `README.md`, `docs/axKit/README.md`, `SECURITY.md`, `docs/axKit/SPEC.md`, `docs/axKit/access-layers.md`, `docs/axKit/automation.md` (section 4), `docs/axClaude/claude-screens.md`, `docs/axClaude/SPEC.md` (D33's sentence only), `src/AxClaude/CLAUDE.md` (section 9 says what each gains). The sentences on the session file go in with this patch too, since WP-A13 lands in the same wave; if WP-A13 does not land, the orchestrating session takes them out.
- Verify: `.\test.ps1`; scripts 30, 31, 01, 02, 28, 22 and 27 pass (01 and 02 run Claude Code itself with the file passed).
- Must not touch: `MainForm.Flow.cs`, `MainForm.Record.cs`, `MainForm.Ui.cs`, `AppSettings.cs`.
- Hears: nothing, when `async` is accepted. If EG-2.5's fallback applies, "Done" comes later by the hook's cost; then the package says the measured cost. Fallback if script 31, 01 or 02 fails for the hooks' sake: `ClaudeHooks` starts false, the rest lands.
- CHANGELOG: `axClaude: Claude Code tells the window when a session starts, a turn stops, an agent starts or stops and a permission is asked, through a hook for the one session and a pipe on this computer; nothing is listened for and nothing leaves the computer (PRIVACY.md).`

### WP-A13 The record in the window

- Stage 1, wave 3, about 45 minutes. Depends on WP-A7, WP-A10. Item: check the conversation against Claude Code's own session file.
- Goal: EG-3.2, EG-3.3 (the thread), EG-3.6, EG-9.2.
- Creates `src/AxClaude/MainForm.Record.cs`:
  - `internal sealed class RecordHost`: its thread ("AxKit record"); `Start(Control owner, IReadOnlyList<string> projectFolders, Action<IReadOnlyList<RecordEntry>> deliver, Action<bool> found, SourceHealth health)`; `Follow(string path)`; `Sent(string key)`; `Stop()`; and `RecordEntry? Reply(string id)`, the ReplyText entries of this session by id, the newest 2,000, for binding later. It polls as EG-3.3 says and runs the folder rule of EG-3.2 while no path is set.
  - The partial methods: `RecordProcessStarted` (the projects folders from `ClaudeCodeTranscript.ProjectsFolders`, the working folder's project folder, the note of lengths, the host started when the switch is on), `RecordProcessEnded`, `RecordFollow`, `RecordForget`, `RecordSwitch`, `RecordSent`.
  - On the UI thread: `RecordFound` or `RecordLost`, then each entry through `FlowFeed.FromEntry`.
- Creates `tests/scripts/axClaude/32-session-file.txt`, label "See the session file followed, without Claude Code": a stand-in below the hooks' version, so that the folder rule finds the file: after a turn `Say the event sources` gives `Session file: followed, 3 entries`; a file that gains three lines that are no JSON gives `Session file: stopped`; the switch off gives `Session file: off`.
- Verify: `.\test.ps1`; scripts 32, 28 and 29 pass.
- Must not touch: `MainForm.Flow.cs`, `MainForm.Hooks.cs`, `MainForm.Process.cs`, any document (WP-A12 writes this wave's).
- Hears: nothing. Fallback: `GroundInSessionFile` starts false.

### WP-A14 The replayer

- Stage 1, wave 3, about 45 minutes. Depends on WP-A1 to WP-A9. Item: speech order breaks.
- Goal: EG-11.2, EG-11.7.
- Creates `tests/AxClaude.Tests/TimelineReplay.cs` (reads a fixture's files; merges them by `t`; feeds chunks to a `SessionModel` with the frame rule, `Arrivals` with reply speech all, events through `ClaudeCodeHooks.Parse`, entries through `ClaudeCodeTranscript.Parse`, sends through `SessionModel.Send` and `UserActed`, all through `FlowFeed` into one `SessionFlow`, ticks at every `WakeAt`; an option for each hostile order), `TimelineTests.cs`, `ClaudeCodePlacesTests.cs`, and each fixture's `expected.txt`.
- Tests: `Timeline_gives_the_expected_decisions` (every fixture), `Hostile_orders_keep_the_invariants` (every fixture, every order of EG-11.2), `Plain_turns_say_Done_three_times_each_after_its_reply`, `Interrupts_say_no_Done_and_leave_the_state_idle`, `A_tool_question_is_an_attention_bell_and_Done_comes_only_at_the_turns_end`, `Two_queued_messages_are_one_turn_and_the_state_never_reads_idle_between`, `Send_now_says_no_Done_for_the_cut_reply`, `A_background_shell_is_listed_until_a_stop_lists_none`, `Clear_asks_for_the_new_file`, `Continue_releases_nothing_of_the_redrawn_conversation`, `Compaction_keeps_the_file_and_the_turn`, `Every_row_of_the_long_reply_is_confirmed_by_its_entry`, `Claude_Codes_event_and_record_words_stand_in_their_two_places_alone`.
- Where a fixture shows that `SessionFlow` does not do what section 5 says, this package corrects `SessionFlow*.cs` and says so. Where a fixture contradicts section 5 itself, the test is skipped with the reason in its `Skip` text and the case is returned as an open point.
- Verify: `.\test.ps1 AxClaude`.
- Must not touch: the window, `tools/`, the fixtures' other files.
- Hears: nothing.

### WP-A15 The export of the record

- Stage 3, wave 4, about 35 minutes. Depends on WP-A13. Item: check the conversation against Claude Code's own session file.
- Goal: EG-8.6.
- Creates `src/AxClaude.Core/Record/RecordExport.cs`: `public static string Render(IEnumerable<RecordEntry> entries)`; and `tests/AxClaude.Tests/RecordExportTests.cs`: `Each_turn_is_an_input_and_an_output_block`, `Headings_are_one_level_deeper`, `A_tool_call_is_one_line_with_its_name_alone`, `Nothing_of_a_skipped_kind_or_a_tool_result_is_written`, `Notes_summaries_and_meta_entries_are_left_out`, `A_cut_reply_says_that_it_was_interrupted`.
- Changes `src/AxClaude/MainForm.Export.cs` (the command: the followed file read off the UI thread with `RecordTail.ReadAll`, rendered, saved through the same dialog and the same "Exported" as the conversation's export; "No session file for this conversation" without one) and `src/AxClaude/MainForm.Ui.cs` (Session, Export Claude Code's record of this conversation..., mark "act").
- Creates `tests/scripts/axClaude/33-export-record.txt`, label "Export Claude Code's own record of the conversation": the command is in the list; without a session file it says so. The save dialog is Windows' own and stays manual, as for the conversation's export.
- Documents: `docs/axClaude/user-guide.md`. The lines of `docs/axClaude/SPEC.md` (FR-3.8) and of the NVDA plan are WP-A17's, since WP-A16 holds those two files in this wave.
- Verify: `.\test.ps1`; script 33 passes.
- Must not touch: `MainForm.Flow.cs`, `MainForm.Model.cs`, `docs/axClaude/SPEC.md`, `docs/axClaude/nvda-test-plan.md`.
- Hears: a new command; nothing that exists changes. CHANGELOG: `axClaude: Session, Export Claude Code's record of this conversation writes the prompts and replies from Claude Code's own session file, with the replies' Markdown as Claude wrote it.`

### WP-A16 The state from the core

- Stage 2, wave 4, about 45 minutes. Depends on WP-A7, WP-A14. Item: speech order breaks. **Changes what the user hears.**
- Goal: EG-6.1 to EG-6.4, EG-1.4 in use.
- Changes `src/AxClaude/MainForm.Flow.cs`: `Built` gains `FlowCharge.State`; `private bool ClaudeWorks => FlowDecides(FlowCharge.State) ? _flow.Working : _model.Working;`; carrying out `StateChanged` sets `_model.TurnOpen` (the core's working or ending; null in shadow) and calls `UpdateStatus()` and `UpdateMusic()`.
- Changes every place of the window that reads `_model.Working` to read `ClaudeWorks`: `MainForm.Model.cs` (`UpdateStatus`, the argument of `_arrivals.Update`), `MainForm.cs` (`Working`, `ClaudeBusy`), `MainForm.Input.cs` (the "Queued" word, which also counts the ending; the Enter sent again), `MainForm.Area.cs`, `MainForm.Mcp.cs`, `MainForm.ClaudeCodeSettings.cs` (their idle tests). A search for `_model.Working` in `src/AxClaude` then finds only `ClaudeWorks` itself.
- Creates `tests/scripts/axClaude/34-state-from-events.txt`, label "Know that Claude works, and send a message while a reply arrives" (the stand-in, with hooks and the session file): after A, a frame with neither spinner nor mode line: `expect state Claude: working`; after C, D and the bell and before the reply's frame, a message is typed and sent: `expect said Queued`; after the reply's frame and the echo, the conversation holds the first reply's last row before `# Input 2`; `Say the event sources` gives `Session file: followed` (the file the SessionStart event named) and `Core: in charge of state`.
- Changes `tests/scripts/axClaude/28-event-sources.txt`: `Core: in charge of state`.
- Documents: `docs/axClaude/SPEC.md` (FR-10.2, FR-4.7, D16, FR-7.9), `docs/axClaude/nvda-test-plan.md` (section 23), `src/AxClaude/CLAUDE.md`.
- Verify: `.\test.ps1`; scripts 34, 28, 22, 27, 06 and 08 pass (06 and 08 are live).
- Must not touch: `MainForm.Export.cs`, `MainForm.Ui.cs`, `docs/axClaude/user-guide.md` (WP-A15's in this wave).
- Fallback: `Built` stays without `State`; the status is today's.
- CHANGELOG: `axClaude: the status says working for as long as a turn runs, from Claude Code's own turn marks, and a message sent while a reply is still arriving goes after that reply.`

### WP-A17 Done from the core

- Stage 2, wave 5, about 45 minutes. Depends on WP-A16. Item: speech order breaks. **Changes what the user hears.**
- Goal: EG-5.6, EG-6.5, 4.2.
- Changes `src/AxClaude/MainForm.Flow.cs`: `Built` gains `Done`; carrying out `SayDone`: put away, the Done is owed (`_doneOwed`, `SettledWhilePutAway()`); else "Done" by its switch and `Signal(question: false)`. Carrying out `AttentionBell`: what the bell's handler did (`_bellPending`, the settle restarted).
- Changes `src/AxClaude/MainForm.cs` (the bell's handler only queues while the core decides Done), `MainForm.Model.cs` (`OnFrameSettled` says Done only in shadow, when a Done is owed from being put away, or when the bell came with `NoMarks`; the survey's "Done first" branch likewise), `MainForm.PutAway.cs` (`_doneOwed`).
- Creates `tests/scripts/axClaude/35-done-after-the-reply.txt`, label "Hear Done after the reply, and not after an interrupt" (the stand-in, Speak replies all): a turn whose reply's frame comes 80 ms after the bell: `expect said claude: ok`, then `expect said Done`, `expect said ♪ Done`; an interrupt (C, D, no bell, the `Interrupted` row): `refuse said Done`, `expect state Claude: ready`; a bell alone with a question's rows: the question, `refuse said Done`, and Done once the answer ended the turn; A, C, D at once with no bell: `expect quiet 1500`.
- Documents: `docs/axClaude/SPEC.md` (FR-7.2, 6.4; and FR-3.8 for WP-A15), `docs/axClaude/user-guide.md`, `docs/axClaude/nvda-test-plan.md` (and WP-A15's step), `src/AxClaude/CLAUDE.md`.
- Verify: `.\test.ps1`; scripts 35, 34, 28, 22, 27 and the live 02, 12, 17, 20 and 21 pass.
- Fallback: `Built` without `Done`.
- CHANGELOG: `axClaude: "Done" comes when the turn has ended and its last reply is in the conversation, without the fixed pause; an interrupted reply and a reply cut by Send queued message now say no "Done".`

### WP-A18 Reply speech through the gate

- Stage 2, wave 6, about 45 minutes. Depends on WP-A17, WP-A8, WP-A13. Item: speech order breaks. **Changes what the user hears.**
- Goal: EG-5.1 to EG-5.5, EG-5.7.
- Changes `src/AxClaude/MainForm.Flow.cs` (`Built` gains `Speech`; carrying out `SpeakReply` calls `SpeakLines`) and `src/AxClaude/MainForm.Model.cs` (`SpeakHeld`'s body becomes `SpeakLines(IReadOnlyList<Line> lines)`; while the core decides speech, `OnTranscriptChanged` adds nothing to `_heldLines`, starts no `_speechHold` and drops nothing for a question: the gate holds and the generation drops).
- Creates `tests/scripts/axClaude/36-speech-gate.txt`, label "Hear each reply once, and no repaint" (the stand-in with hooks and the session file, Speak replies all): a reply confirmed by its entry is said once; the whole conversation drawn again at idle as new rows is not said; in the next turn the first reply's head redrawn as `claude: A short summary · summary` is not said; a reply followed in the same frame by a question's rows: no announcement that does not interrupt holds the question's rows; a reply for which neither an entry nor a Stop comes is not said. A second window with the stand-in below the hooks' version and grounding off: the reply is said once after the hold.
- Documents: `docs/axClaude/SPEC.md` (FR-7.4, FR-7.4b, D32, D33's timing), `docs/axClaude/user-guide.md`, `docs/axClaude/nvda-test-plan.md`, `src/AxClaude/CLAUDE.md`, `src/AxClaude.Core/CLAUDE.md`.
- Verify: `.\test.ps1`; scripts 36, 35, 27, 22 and the live 02, 12, 17 and `two-questions` pass.
- Fallback: `Built` without `Speech`.
- CHANGELOG: `axClaude: with Speak replies on, a reply is spoken once, in the order of Claude Code's own record; a repaint, a replay, Claude Code's summary of an earlier reply and a question's rows are no longer spoken as replies, and speech that waited is dropped once you have moved on.`

### WP-A19 Background work from the Stop event's list

- Stage 2, wave 7, about 30 minutes. Depends on WP-A12, WP-A9, WP-A16. Item: speech order breaks. **Changes what the user hears.**
- Goal: EG-7.
- Changes `src/AxClaude/MainForm.Background.cs` (`BackgroundNow` passes the listed counts and whether a mode line was read after the last Stop; `SayBackgroundWork` passes the list; a changed list restarts the settle) and `MainForm.Flow.cs` (`Built` gains `Background`; carrying out `BackgroundListed`).
- Creates `tests/scripts/axClaude/37-background-from-events.txt`, label "Hear background work that an event listed" (the stand-in): a Stop that lists one shell while the mode line counts none: `♪ BackgroundStart`, `1 shell in the background`, Ctrl+B says `Shell: Run ping in background`; the next Stop lists none: `♪ BackgroundFinish`; a SubagentStart alone: `expect quiet 1500`.
- Documents: `docs/axClaude/SPEC.md` (FR-7.10), `docs/axClaude/nvda-test-plan.md`.
- Verify: `.\test.ps1`; scripts 37, 22 and 27 pass.
- Fallback: `Built` without `Background`; the number is today's.
- CHANGELOG: `axClaude: What runs in the background names the tasks Claude Code listed at its last stop.`

### WP-A20 Bind: identity and heading levels

- Stage 3, wave 8, about 50 minutes. Depends on WP-A18, WP-A13. Item: check the conversation against Claude Code's own session file. **Changes what the user hears.**
- Goal: EG-8.1, EG-8.2, EG-8.8, EG-8.9.
- Changes `src/AxKit.Core/Transcript/Line.cs`: `SourceId`, `SourceLevel`.
- Changes `src/AxClaude.Core/Record/ClaudeCodeTranscript.cs`: `Headings(string source)`, the heading lines outside code fences with their letters and levels. Creates `src/AxClaude.Core/Record/RecordBinder.cs`: `public static IReadOnlyList<int?> Levels(RecordEntry reply, IReadOnlyList<Line> lines)`: for each line, the level plus one (at most 6) of the first unused heading of the source whose letters equal the line's (the head's label taken off), else 0.
- Changes `src/AxClaude.Core/Transcript/SessionModel.cs`: `Bind(Line head, string entryId, IReadOnlyList<int?> levels)` (the head and the rows under it down to the next labelled row or marker get the id and their level), `Unbind(Line line)`, `public int BindingsLost { get; private set; }`; `Classify` gives a bound line its `SourceLevel` and never the heuristic's; `TextChanged` unbinds a bound line whose text changes.
- Changes `src/AxClaude.Core/Transcript/Arrivals.cs` and `Flow/` so that heads are seen with reply speech off: `Arrival` gains `IReadOnlyList<Line> SeenHeads` (the `claude:` rows after the newest output marker that are new or rewritten, in every speech mode, once Claude is ready); `FrameSeen` carries them; `SessionFlow.Gate.cs` matches a seen head that was not proposed by the same rule and binds it without releasing anything.
- Changes `src/AxClaude/MainForm.Flow.cs`: carrying out `EntryBound`: the head line by its id among the last 400 lines, the entry from `RecordHost.Reply`, `RecordBinder.Levels`, `_model.Bind`; nothing when either is gone.
- Creates `tests/AxClaude.Tests/RecordBinderTests.cs` (`Headings_of_the_source_give_their_level_plus_one`, `A_line_that_is_no_heading_in_the_source_gets_level_zero`, `A_heading_inside_a_code_fence_is_none`, `Two_headings_with_the_same_words_are_matched_in_order`) and `BindTests.cs` (`A_bound_line_keeps_its_level_against_the_heuristic`, `A_bound_line_whose_text_changes_loses_its_binding`, `Binding_makes_no_edit_in_the_mirror_when_no_level_changes`, `With_reply_speech_off_heads_are_still_bound`).
- Creates `tests/scripts/axClaude/38-headings-from-the-record.txt`, label "Move by the true heading levels of a reply" (the stand-in): a reply whose source has `# Title`, `## Part` and a short line without a full stop that is no heading: in the conversation, the key 2 stops at "Title" ("heading level 2"), the key 3 at "Part", and `h` does not stop at the short line.
- Documents: `docs/axClaude/SPEC.md` (FR-5, 7.6, R8), `docs/axClaude/nvda-test-plan.md`, `src/AxClaude.Core/CLAUDE.md`.
- Verify: `.\test.ps1` (with `AxTerminal`); scripts 38, 36 and 04 pass.
- Fallback: a constant in `MainForm.Flow.cs` leaves `EntryBound` not carried out; levels are the heuristic's, as today.
- CHANGELOG: `axClaude: headings in Claude's replies have their true levels, from Claude Code's own record.`

### WP-A21 Pin

- Stage 3, wave 9, about 45 minutes. Depends on WP-A20. Item: check the conversation against Claude Code's own session file. **Changes what the user reads.**
- Goal: EG-8.3.
- Changes `src/AxClaude.Core/Transcript/ClaudeCodeTexts.cs` (`SummaryEnd`, with its one sighting, 2.1.325, 2026-11-15) and `LineClassifier.cs` (`IsSummaryRow(string text)`: starts with the `claude:` label and ends with `SummaryEnd`).
- Changes `src/AxClaude.Core/Transcript/SessionModel.cs`: `Bind` keeps, for each bound reply, its rows as they stand then (text, kind, join, level); `TextChanged`, for a bound head whose new text is a summary row, pins:
  1. for each kept row a new line (`NewLine`) with its text, kind, join and level, final (`Committed`), bound to the same entry, inserted in order directly in front of the head;
  2. the head takes the new text, loses its binding and is hidden as an interface row (`ChromeHidden`, `StickyChromeText` its text);
  3. every other line of that reply that is still a row of the screen loses its binding;
  4. `public event Action<IReadOnlyList<Line>>? Pinned` tells the window, which marks the new lines as spoken (`_arrivals.Skip`) and counts one pin.
- Creates `tests/AxClaude.Tests/PinTests.cs`: `A_bound_reply_redrawn_as_a_summary_keeps_its_lines_and_hides_the_summary`, `The_lines_put_back_are_final_and_keep_kinds_joins_and_levels`, `A_summary_row_of_a_reply_that_was_never_bound_is_left_as_drawn`, `Any_other_rewrite_of_a_bound_line_only_unbinds_it`, `The_mirrors_edits_for_a_pin_give_the_rendering` (the edits applied to the old text equal `Render`), `Lines_put_back_are_not_proposed_again`.
- Changes `src/AxClaude/MainForm.cs` (the `Pinned` handler). Creates `tests/scripts/axClaude/39-summary-pinned.txt`, label "Keep a reply that Claude Code replaced by its summary" (the stand-in): a reply of three rows, bound; then a frame in which that block is one row `claude: Short summary of it · summary` with the tool row moved up: the three rows are in the conversation, `refuse text · summary`, `refuse said Short summary`.
- Documents: `docs/axClaude/claude-screens.md`, `docs/axClaude/SPEC.md` (G3, 4.4), `docs/axClaude/nvda-test-plan.md`.
- Verify: `.\test.ps1`; scripts 39, 38, 36 and 24 pass (24 is the view's self-check).
- Fallback: a constant leaves the pin off: the summary shows as today.
- CHANGELOG: `axClaude: a reply that Claude Code later replaces by its own summary stays in the conversation as it was said.`

### WP-A22 The markers' order and the waiting count

- Stage 3, wave 10, about 40 minutes. Depends on WP-A20. Item: check the conversation against Claude Code's own session file. **Changes what the user hears and reads.**
- Goal: EG-8.4, EG-6.6.
- Changes `src/AxClaude.Core/Transcript/SessionModel.cs`: `public int RepairBlockOrder()`: for each exchange block, when the visible line before its `# Input` marker and the first visible line after its last line are bound to the same entry, the block's lines move, once, to behind the last line bound to that entry; the number moved is returned. Called by the window when a turn's Done is said or its state turns idle.
- Changes `src/AxClaude/MainForm.Flow.cs` (the call, the count) and `MainForm.Model.cs`, `MainForm.cs` (the status bar's and the close question's waiting count is `_flow.Waiting` while there is a record).
- Creates `tests/AxClaude.Tests/BlockOrderTests.cs`: `A_block_inside_a_bound_reply_moves_behind_it`, `A_block_between_two_replies_stays`, `Unbound_lines_move_nothing`, `A_block_moves_once`, `The_mirror_applies_the_move_and_a_caret_on_a_kept_line_stays_on_it`.
- Creates `tests/scripts/axClaude/40-messages-waiting.txt`, label "Know how many messages wait for Claude" (the stand-in): two messages queued while a turn runs, one hint row drawn for both: `expect state 2 messages waiting`; both taken up as one turn: the part goes.
- Documents: `docs/axClaude/SPEC.md` (FR-4.1, D22, FR-4.7, FR-10.2), `docs/axClaude/nvda-test-plan.md`.
- Verify: `.\test.ps1`; scripts 40, 34 and the live 06 pass.
- Fallback: constants leave both off.
- CHANGELOG: `axClaude: the status counts every message that waits for Claude, and a message's block that ended up inside an earlier reply is moved behind it.`

### WP-A23 History at a start

- Stage 3, wave 11, about 45 minutes. Depends on WP-A20. Item: check the conversation against Claude Code's own session file. **Changes what the user hears.**
- Goal: EG-8.5, without the restoring of what a redraw shortened.
- When Claude is ready after a start on a conversation that exists, and the session file is known: the file's entries up to the length noted at the start are read once off the UI thread (`RecordTail.ReadAll`); on the UI thread the replayed `claude:` heads, in order, are matched against the ReplyText entries, in order, by EG-5.3's rule for a head, and bound (`SessionModel.Bind`) with their levels. Nothing is proposed, released or spoken. Unmatched heads and entries are counted: `History: 14 of 14 replies bound`.
- Changes `src/AxClaude/MainForm.Record.cs` (the read and the matching's call), `src/AxClaude.Core/Record/RecordBinder.cs` (`public static IReadOnlyList<(Line Head, RecordEntry Reply)> History(IReadOnlyList<Line> lines, IReadOnlyList<RecordEntry> entries)`), `SourceHealth` (the two numbers).
- Creates `tests/AxClaude.Tests/HistoryBindTests.cs` (`Replayed_heads_are_bound_to_the_records_replies_in_order`, `A_head_the_record_does_not_hold_is_skipped_and_counted`, `Nothing_is_proposed_for_history`) and `tests/scripts/axClaude/41-history-bound.txt`, label "Read an earlier conversation with its true headings" (the stand-in started on a session file that holds two turns, drawing them as Claude Code does at `--continue`): nothing is said but Ready; the heading keys find the earlier replies' headings at their levels.
- Documents: `docs/axClaude/SPEC.md` (FR-4.8), `docs/axClaude/nvda-test-plan.md`.
- Verify: `.\test.ps1`; scripts 41, 38 and the live 08 pass.
- Fallback: a constant leaves it off.
- CHANGELOG: `axClaude: an earlier conversation shown at a start has its headings' true levels.`

### 10.3 After the last package

The full pass with NVDA listening (`.\nvda-pass.ps1 claude`, only with the user away), recorded in `docs/axKit/nvda-test-record.md` by step with the version and the commit; `.\tools\upstream.ps1 -Verify claude`; the priorities items updated: what landed, what stays in shadow or off and why, what the user is asked to confirm (section 8).

## 11. Risks, and what is left for later

### 11.1 Risks

- **R1 Claude Code changes.** The marks, the events' names and fields and the session file's format are Claude Code's and move with its releases; the file's format is documented as internal. Guards: each has one place; the timeline fixtures and the replayer; `tools/upstream.ps1 -Verify` with script 31; the version gate; the reader that switches itself off; and every fact's fallback down to the screen. A new Claude Code can still raise the "Settings Warning" question if it drops an event name: the window shows it as any question, answer 1 goes on, and the upstream check says so first.
- **R2 An A with no turn behind it.** Seen once in an older recording (`background`, 2.1.281) and for hand-back notes. A turn opened by A alone is closed after 2 s of an idle screen (EG-4.6); until then the status says working. If it turns out to be common, the rule moves to "A and a sign of work".
- **R3 A reply line the record does not confirm in time is not spoken.** On a loaded computer the file can lag more than `GateCapMs`; the Stop event covers a turn's last message, not text before a tool call. The diagnostics count every such line; the constant is one line to change. The measured lags are 51 to 190 ms at ordinary load.
- **R4 The screen key's rules come from one sample.** Tables, links and fences are covered; images, HTML, nested lists and code longer than the console are not measured. A row the rules do not explain is not spoken and not bound; it is still shown.
- **R5 The summary row was seen once.** ` · summary` at the end of a `claude:` row, 2.1.325, one sighting in the user's window, no recording. The pin acts only on that shape and only for a bound reply; a different shape leaves today's behaviour.
- **R6 A hook's process for each event.** Two processes for each event on a computer with 7 GB; a few for each turn. If `async` is refused, each Stop costs the turn's end 0.1 to 0.45 s (EG-2.5).
- **R7 The hook program is gone.** If the axKit program file named in the settings file is deleted while a session runs, Claude Code prints "hook error" rows into the conversation at each event. A build in use is locked, so this needs a deliberate removal; the rows are named in `claude-screens.md` so that they are recognised.
- **R8 Workflows and hooks.** Whether a dynamic workflow's inner agents fire the subagent events was not measured; EG-D-12 keeps them from the number until it is.
- **R9 The Store copy.** No hooks there until EG-2.11's question is settled; the marks and the file's folder rule still serve it.
- **R10 The view.** A pin and a moved block are edits above the bottom of a long conversation, which re-wrap the control from there (SPEC.md D24). They are rare, held like any output while the user reads, and covered by the view's self-check.
- **R11 Two things at once.** A hook event and the frame that shows the same moment arrive in either order. The replayer's hostile orders are the guard; a rule that needs one order is a defect.
- **R12 The wait at a window's first start.** Up to 2 s for `claude --version` before Claude Code starts, once for each window (EG-D-18).

### 11.2 Left for later

- **Structured live text.** The MessageDisplay event carries a reply's text line by line while it streams, seconds before the screen draws it. It would let replies be spoken as they are written. It needs a transport without a process for each line.
- **Hooks through an MCP tool** of axKit's own, or another transport without a process for each event; then the events for each tool call and each submitted prompt become affordable.
- **The tool rows from the file**: a tool call's name, input and result by its id, so that "one line for each tool, a key to unfold" does not depend on what Claude Code folds, and so that tool rows pass the gate by identity.
- **A list of earlier sessions** of the folder, from the session files' first prompts and titles, to open one without Claude Code's picker.
- **The renderer** for the rules of 4.3, to restore from the file what a redraw at a start shortened (EG-8.5), and for an export that reads as the conversation does.
- **More structure for navigation**: code blocks, lists, tables and links from the source, each with the key that reads it; a line's time, with the key that says it.
- **"Responding" and the ticks from the core**, and the question flow's own timers (the 400 ms settle, the three seconds after the user's act) re-judged one by one against the PermissionRequest event and the tool call's entry.
- **Subagents' own session files** (`subagents/agent-<id>.jsonl`): what an agent did, on demand.
- **`--session-id`** for starts without `--continue`, if the folder rule ever proves too slow.
- **The Store copy's path** for files Claude Code must read, with the AI tools' file (axHelp HE-2.8).
- **A second agent program** (the GitHub Copilot CLI first): its table of marks, events and record, and the move of the neutral pieces to a shared project.
- **Self-voicing** is no part of this design: NVDA stays the one voice.

### 11.3 Not settled when this was written

- Whether `async` is accepted on each of the eight events and shows nothing on the screen (WP-A3 measures it).
- Whether a hook's process inherits a variable axClaude sets in Claude Code's environment, as the documentation says (WP-A12's script 31 proves it or fails).
- Whether a hook started from the Store copy's alias runs at all, and whether Claude Code can read a file in the package's private folder (R9).
- What a dynamic workflow fires (R8).
- Whether Claude Code draws the summary row for every long text before a tool call or only sometimes (R5).
- The project folder's name for a working folder with letters outside ASCII or longer than 200 characters: the folder rule then finds nothing and there is no record without hooks.
