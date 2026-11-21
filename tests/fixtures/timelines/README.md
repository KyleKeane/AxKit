# Timeline fixtures

Each folder here is one recorded session of Claude Code on one clock: the console stream, the hook events, the session file's entries and what the script sent, with the times they arrived (`docs/axClaude/events-and-grounding.md` EG-11.1, section 6.6). They drive the replayer (`TimelineReplay`, EG-11.2), which feeds them through the code the window uses and compares its decisions with the folder's `expected.txt`.

A fixture is made by `tools/ClaudeProbe`, never by hand: `ClaudeProbe run` records a run into a scratch folder, and `ClaudeProbe fixture <run folder> <fixture folder>` makes it a fixture through a whitelist (`tools/ClaudeProbe/README.md` section 8). The whitelist keeps only the eight events axClaude registers with their fields of EG-2.1, the session file's entries of EG-3.4 with their fields (every other line its type alone), and masks the account's, the computer's and the domain's names length for length, in the console bytes as in every kept string, so that no position in the stream moves; then it reads every file back and refuses the fixture when an e-mail address, an account path or one of those names is still there. A fixture is never edited by hand, not even a byte of its stream: a run the tool refuses, or one that needs a change, is recorded again from a neutral folder (`C:\Temp\axclaude-fixture`).

## The files of a fixture

| File | What it holds |
| :-- | :-- |
| `meta.json` | Claude Code's version, the console's size, the day of the recording, the run it came from, and `turns`: the turns that finished (the session file's `turn_duration` entries) |
| `pty.vt`, `pty.vt.chunks.txt` | the console stream, masked length for length, and its index `offset length elapsedMs` on the recording's clock, as `StreamRecorder` writes it |
| `events.jsonl` | one line for each kept event: `{"t":7987.6,"event":{"hook_event_name":"Stop",...}}` |
| `entries.jsonl` | one line for each line the session files gained, in file order: `{"t":8121.1,"session":"<the file's session id>","entry":{...}}`, or `"entry":{"type":"api-request-blob"}` for a kind that is skipped |
| `sends.jsonl` | what the script wrote: `message` (with its text), `key` (by its name; `SendNow` for Ctrl+X Ctrl+S), `text` (written without Enter, as `/exit`) |
| `expected.txt` | the core's decisions in the recorded order, written by the replayer's tests with `AXCLAUDE_UPDATE_EXPECTED=1` and reviewed by hand the first time |

`t` is milliseconds on the recording's clock. A session file's path in an event is masked and never opened: the replayer feeds the entries itself. A string of an entry that starts with `<` (a command, a shell line, a task notification) keeps its first tag alone, and an `isMeta` or `isCompactSummary` entry has an empty content: the reader tells them by the tag and the flag.

## The recordings

All were recorded on 2026-11-15 with Claude Code 2.1.325 in screen reader mode at 240 by 50, the model Haiku, each from a fresh scratch folder under the account's temporary folder (the trust question and the session file's path name it, masked), by the probe the specification was written from. Their hooks were synchronous (no `async`), so the spinner row reads `( running Stop hook )` while one runs; with the hook plan `auto` the probe registered SessionStart as a command hook and the other events as http hooks, which arrive about 70 to 100 ms sooner than a command hook would; with `both`, the tool kept the command hook's copy. The runs of r2 and r3 wrote Claude Code's debug log, so their banner has the row `Debug mode enabled · logging to <a masked path>`.

| Fixture | From | Hooks | Shows |
| :-- | :-- | :-- | :-- |
| `plain-turns` | `r1/a` | auto | three plain turns, each "Reply with the single word ok.", then `/exit` |
| `no-hooks` | `r1/g` | none | the same with no hooks at all: no events |
| `tool-question` | `r1/b1b` | auto, `--permission-mode manual` | a permission question: a bell alone, the answer 1, the turn's end |
| `text-tool-text` | `r1/b2e` | auto, `--permission-mode manual --allowedTools Bash` | a turn of two messages with a tool call between |
| `long-reply` | `r1/cd` | auto | a reply of 40 lines, and a Markdown sample |
| `queued-one` | `r2/a1` | auto | one message queued while a reply streamed |
| `queued-two` | `r2/a2` | auto | two queued messages, one turn |
| `send-now` | `r2/a4` | auto | a reply cut by Ctrl+X Ctrl+S for the queued message |
| `interrupts` | `r2/b1` | auto, `--allowedTools Bash(ping:*)` | three interrupts with Escape: two during a reply, one during a tool |
| `questions` | `r2/c1` | auto | Claude's own questions: one, two in one call, several answers |
| `background-shell` | `r2/d1` | auto, `--allowedTools Bash(ping:*)` | a shell in the background and its end |
| `background-agents` | `r2/e1` | auto, `--allowedTools Bash(echo:*) Bash(ping:*)` | two agents, each listed by a Stop event as running in the background, and their hand-backs |
| `background-shells-330` | `bg-ps`, 2026-11-21, Claude Code 2.1.330 | auto, `--allowedTools PowerShell(Start-Sleep:*) Bash(sleep:*)` | two turns that each leave a command running, a PowerShell one and a Bash one beside it, and end while they run: the mode line's `1 shell` and `2 shells` while Claude works and after the turn (`· 1 shell still running` ends the turn's summary row); the first mode line of the session still on the screen above the current one; and the row `Removed 1 invisible character from your prompt` under the mode line. Recorded for the maintainer's report that nothing said a background command still ran |
| `continue` | `r3/c2` | both, `--continue` | `--continue`: the old conversation redrawn before SessionStart, then one turn |
| `clear` | `r3/d0` | http | `/hooks`, `/clear` (a new session id and file: SessionEnd and SessionStart), `/exit` |
| `compact` | `r3/e` | both | two turns, `/compact`, `/exit` |

`r1/f`, a run that printed `/status`, was tried too and refused by the tool: its stream held the account's e-mail address. It is not here.
