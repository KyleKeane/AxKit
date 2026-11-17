# axClaude: the stream as a second source, behind a switch

A specification, written 2026-11-17 for the maintainer's decision of that day (`stream.md`, section 7 (a); `docs/axKit/events.md`,
section 7, step 3): Claude Code's non-interactive stream becomes a second way for axClaude's window to learn what
Claude does, off at first, with the screen as the default and the way back. Requirements are numbered SM-n.m. Stage 1
of section 11, the seam, is built and changes no behaviour (section 13); nothing of the stream is built. The stages
after it wait for the decisions of section 12, where each says so.

It rests on three things made the same day: the measurements (`stream.md`, twenty cases, all passing), the door
(`events.md`, section 10), and an inventory of the window, member by member, kept beside the lists until the
window's second ways to send are built (5,900 lines in 19 files; every member sorted into window logic, bound to the
screen, or mixed).

## 1. Summary

- **One window, one model, two sources.** The window reads its source through one object, the screen model, in 41
  named facts and about 150 places. That object gets a plain base class that lists those facts. The screen model
  fits it as it stands. A second model, fed by the stream's lines, stands in its place when the switch is on. The
  window's reading places do not change.
- **What the window sends** goes through a handful of places today (a message, an answer, Escape, a few keys). Each
  gets its second way: a line of the stream in place of keystrokes.
- **The machinery beside the screen** (the turn marks, the session file's reader, the hooks, the event core: some
  1,900 lines) is the screen's own. It is not wired when the stream feeds the window, which needs none of it: a
  turn's end is one line.
- **What the stream cannot give** is said plainly where the user meets it: Claude Code's own screens, the keys sent
  to it, the count of waiting messages while a turn runs.
- **Three things the window does itself**, because the stream does not: it asks whether a folder is trusted, it
  shows the older conversation from the session's file, and it hands a conversation between the two sources by the
  session's id.
- **The screen stays the default**, untouched in behaviour, proven after stage 1 by the whole live set and an NVDA
  pass before anything of the stream is added.

## 2. Words

- **Source**: what feeds the window. The **screen source**: Claude Code in a pseudo console in screen reader mode,
  read row by row, with the marks, the session file, the hooks and the event core beside it. The **stream source**:
  Claude Code as one process over pipes, read line by line (`ClaudeCodeStream`).
- **The model**: the object that holds the conversation's lines and the facts the window asks (`SessionModel` today).
- **The relay**: a small program Claude Code starts and asks before a tool call that needs the user's word, and for
  Claude's own questions; it passes the question to the window and the answer back.

## 3. The seam: one model, two sources

- SM-3.1 **`ClaudeModel`**, an abstract class on the bundle's `LineStore`, in `AxClaude.Core`: the facts and events
  the window reads of its source, by the names it reads them by today. The window's field `_model` is of this
  type. Nothing else about the window's reads changes: not a name, not a line. The class keeps nothing itself: it
  only names.
- SM-3.2 **`SessionModel` derives from it** and is otherwise as it is: each of its members that the base names is
  the base's member filled in, with the code it had.
- SM-3.3 **The facts every source has**, which a model must fill in:
  - whether Claude waits for an answer and on what: `PromptPending`, `PendingQuestion`;
  - the turn: `Working`, `QueuedMessages`, `Mode`;
  - work in the background: `Background`, `BackgroundAgents`, `BackgroundWorkflows`;
  - the conversation itself: `Send` (the block of a sent message), `MarkQuestion`, `Clear`, `ResetScreen` (a new
    run in the same window), and from the bundle's `LineStore` the lines, `AddSystemLine` and the end of a frame;
  - what happens: `ResponseStarted`, `Announcement`, and from `LineStore` the change of the lines and the bell.
- SM-3.4 **What only a drawn screen has**, which a model without a screen answers with nothing (null, false, zero,
  no effect), and what the window sets of it is not kept: `PendingScreen`, `TextPrompt` and `AwaitsText`,
  `PromptHolds`, `FeedbackDrafts`, `DraftTitle`, `DetectScreens`, `Spinner`, `SessionName`, `StartName`,
  `TranscriptViewOpen`, `BackgroundAgentsAwaited`, `BackgroundWorkflowsAwaited`, `BackgroundUnknown`,
  `ShowProgressRows`, `HideReplay` with `ShowHiddenLines` and `MarkReplayedExchanges`, and
  `SaidNoConversationToContinue`. So the window's code for those things is never entered while the stream feeds
  it, and is not copied, branched or removed.
- SM-3.5 **The console window base** (`ConsoleForm`, shared with axTerminal) keeps its pseudo console as it is. Two
  members stop assuming it: `ProcessRunning` is virtual, and the music asks `ProcessRunning` instead of the
  pseudo console's host. axTerminal fills in nothing new and behaves as it did.
- SM-3.6 **The machinery beside the screen is the screen source's**: `MainForm.Flow.cs`, `MainForm.Record.cs`,
  `MainForm.Hooks.cs`. They alone ask the screen's own model, through a field of its own (`_screenModel`), for the
  four things that are not on the base class: the turn marks (`Marked`), the screen's work and the run's number
  handed to the core with a frame (`Work`, `Epoch`), and the turn the core hands back (`TurnOpen`). The machinery
  is wired only for the screen source. With the stream, whether Claude works is the model's `Working`, and a
  turn's end is the model's own (SM-4.6); the window's existing rule for a source without the core (the setting
  `eventCore` set to `shadow` runs it today) is the one in force.
- SM-3.7 **A test holds the seam** (`SourceSeamTests`), read off the window's sources: the window names
  `SessionModel` only where it makes the model, in `MainForm.cs`; `_screenModel` is asked only in the machinery's
  three files; `_model` is held as `ClaudeModel`; and the machinery's four members are not on the base class.

## 4. The stream source

- SM-4.1 **The process**: `StreamHost`, in `AxClaude.Core`, beside `ClaudeCodeStream`: Claude Code started with the
  stream's options over three pipes, no console window, its output read line by line on a thread of its own and
  posted to the window's thread in batches (bundle rule 3), its end reported with the exit code. It writes lines;
  it knows no word of Claude Code's.
- SM-4.2 **The line**: `ClaudeCodeStream.Options`, the option that sends each message back when it is taken
  (`ReplayOption`), the relay's two options (SM-7.3), the model and the session options (SM-8), and the user's own
  arguments. Never an option that keeps the session out of the store: this is the user's conversation.
- SM-4.3 **`StreamModel`**, on `ClaudeModel`, in `AxClaude.Core`: given each `StreamLine`, it keeps the
  conversation's lines and the facts. Pure and tested from lines, as `ClaudeCodeStreamTests` is.
- SM-4.4 **The conversation's lines look as they do today**: `you:` for a message, `claude:` for a reply, `tool:` for
  a tool call, the Input and Output markers around an exchange, headings as headings, so that every quick key lands
  where it lands today. A message's block is placed when the stream sends the message back, which is when Claude
  takes it up: a message sent while Claude works stays out of the conversation until then, as today (SPEC.md D16).
- SM-4.5 **A reply's text.** The stream gives a reply as Markdown source, where the screen gives rows that Claude
  Code has already made plain. One function makes the source read as the screen's rows do; it is tested against
  pairs taken from the timeline fixtures, which hold the screen and the source of the same replies. Headings are
  taken from the source, where they are certain.
- SM-4.6 **The turn**: working from the message taken to the turn's end line; the end is raised as the model's
  `Bell`, so that the window says Done by its existing rule, after the reply's lines, which are in the model before
  the end line arrives. A failed or interrupted turn says no Done.
- SM-4.7 **What Claude waits on**: a question from the relay (SM-7) is the model's `PendingQuestion`, a `Question`
  as the control area shows one today: its title, its text, its options, whether several may be chosen. The area
  shows it by its existing code.
- SM-4.8 **Work in the background**: from the stream's task notes and its list of background tasks: the count, each
  task's kind and description. The window's sounds and words for it are the existing ones.
- SM-4.9 **Claude Code's own notes** become the model's `Announcement`: compacting and its end, the conversation
  emptied, the permission mode after a change. The words are chosen with the maintainer (section 12).
- SM-4.10 **Ready** is the process being up and answering (the start line comes only with the first turn): the
  stream source writes one request at its start that changes nothing, and the answer is the sign, some 0.6 s after
  the start as measured. Which request is settled in stage 2 by measurement. "Ready" is then said by the window's
  existing rule; the mode's name for the status bar comes with the first start line or status note.

## 5. What the window sends

Each place that writes to the pseudo console today, with its second way. The window calls one method for each;
the method writes keystrokes for the screen source and a line for the stream source.

- SM-5.1 **A message** (`QueueWrites`, the one door of every message, typed today as paced writes): one line,
  `ClaudeCodeStream.UserLine`. "Sent" is said when it is written; "Queued" while a turn runs, as today.
- SM-5.2 **An answer** (`SendAnswer`, typed today one keystroke a time): the relay's answer for the question that
  waits (SM-7.4).
- SM-5.3 **Escape and Interrupt** (`SendEscape`, `Interrupt`): on a waiting question, the relay's No; in a running
  turn, `ClaudeCodeStream.InterruptLine`; with nothing running, nothing is sent (measured: harmless either way).
- SM-5.4 **The next permission mode** (Shift+Tab today): `ClaudeCodeStream.ModeLine` with the next of Claude Code's
  modes in the interactive program's order; never the mode that asks nothing, unless the user started Claude Code
  with it (SPEC.md FR-14.4).
- SM-5.5 **Slash commands** (`/model`, `/effort`, `/compact`, `/clear`, the user's own): a message, which the stream
  runs as the command (measured). `/model` and `/effort` need their value with them, since no picker opens: the
  window asks for it in the control area first.
- SM-5.6 **A command Claude offers** (the `!` line of an offered command): not measured in the stream. Until it is,
  an offer in stream mode says that it cannot be sent this way and how to run it (section 12).
- SM-5.7 **Not sent at all in stream mode**, each said in a few words when asked for: Send queued messages now, the
  Send key submenu (Ctrl+C, Ctrl+D, Tab, the arrows), Ctrl+O's guard. Their menu items stay, so that the menus are
  the same in both modes, and say "Not with the stream source" (the words are section 12's).

## 6. What the stream does not have

The inventory found eighteen things of today's window that exist only because Claude Code draws a screen. In stream
mode each is simply absent, and the window says so where the user would look for it.

- SM-6.1 **Claude Code's own screens**: `/config`, `/status`, `/tasks`, `/help`, `/permissions`, the review of
  answers, the resume picker, `/btw`'s side answer, the feedback survey, drafts and their review. Not shown. What
  the user needs of them has other ways: the settings are axClaude's own menu already (SPEC.md FR-14), a
  conversation is resumed by the window (SM-8), and `/status` and `/context` print as text.
- SM-6.2 **`/login`** and the other commands of the terminal alone: signing in is done with the screen source.
- SM-6.3 **The mode line's own words, the spinner's words, the session's name**: the status bar says working,
  ready or waiting, the mode, and the count of background work; no spinner text, no session name.
- SM-6.4 **The count of waiting messages while a turn runs**: the window counts the messages it wrote and that have
  not come back.
- SM-6.5 **The raw recording** of the console's bytes: with the stream source, Developer, Record raw stream records
  the stream's lines instead, to a file that holds the conversation and is never committed.
- SM-6.6 **The console's size, screen reader mode, the detailed view**: no meaning without a screen.

## 7. Safety: the folder's trust and the questions

- SM-7.1 **The folder's trust.** The interactive program asks before it works in a folder it has not seen; the
  stream does not ask (measured: twenty cases in new folders, no question). A project's own settings can name
  programs for Claude Code to run. So before the first start of the stream source in a folder, the window asks,
  in a notice, with No as the default, and remembers a yes for that folder under the settings' seal, as axGIT
  remembers a trusted repository. The words are section 12's.
- SM-7.2 **Nothing runs without the user's word that would have asked with the screen source.** The stream source
  is started with the permission mode the user's own Claude Code settings and arguments give, as the screen source
  is, and axClaude adds no option that asks less. Every question Claude Code then asks goes to the window through
  the relay. A question the window cannot show is answered No.
- SM-7.3 **The relay** is `AxKit.exe` with a verb of its own, started by Claude Code as the one server of a
  configuration file axClaude writes for the start (as it writes the hooks' file today). It speaks to this window
  alone, over a named pipe with a secret of the start, the pipe open to the user alone (as the hooks' pipe is). It
  holds no logic: a question in, an answer out. When the window is gone or does not answer, its answer is No.
- SM-7.4 **A permission question** is shown as a question with three answers, No first where a list starts: No;
  Yes, once; Yes, and do not ask again for this tool in this session. The last is a rule of the session's alone
  (`ClaudeCodeStream.AlwaysAnswer`): it is written to no settings file and a new session asks again. The question
  names the tool and shows what it would do (the command, the file), read from the request.
- SM-7.5 **Claude's own questions** (several questions, each with options, some with several answers, each with
  Other for the user's own words) are asked one after another in the control area as today, and go back in one
  answer when the last is answered. Escape answers No to the whole.
- SM-7.6 **What an AI asks for says so** (bundle rule 11): a permission question is Claude's request to run
  something; its first words say who asks, and running is never where the list starts.

## 8. Sessions

- SM-8.1 **Continue**: the default start takes up the folder's last conversation. With the stream, `--continue` in
  a folder without one starts a new conversation and says nothing (measured), so the window's retry for the screen
  is not needed.
- SM-8.2 **The older conversation**: a session taken up again writes nothing of what was said before (measured).
  The window fills its view from the session's file, read by the reader it has (`ClaudeCodeTranscript`), as markers
  and lines of the same kinds, before the first new line. A file it cannot read leaves the view empty with one
  system line that says so.
- SM-8.3 **Handing a conversation between the sources**: Claude Code leaves a stream's sessions out of the
  interactive program's own list and of its `--continue` (its documentation). So the window keeps the id of the
  session it last ran in each folder, and a switch of the source in the same folder takes that session up by its
  id. To be measured before stage 6: the screen source started with the id of a stream's session, and the other
  way round.
- SM-8.4 **`/clear`** begins a new session and says so with a line of its own (measured): the window empties the
  conversation as it does today and keeps the new id.

## 9. The switch and the way back

- SM-9.1 **One setting**, `claudeSource`: `screen` (the default) or `stream`, and one option of the command line
  that names the source for one start, for the window scripts and for the way back. At first in Options, Developer,
  beside the event sources; it moves to Options when the maintainer has used the stream for a while.
- SM-9.2 **One window, one source.** A window is made for its source and keeps it for as long as it is open: the
  model is made once, with the window, and nothing of an open window is rebuilt under the user (found while the
  seam was built: the window base is handed its model as it is made). A change of the setting applies to the next
  window of axClaude, and the window says so. The conversation is found again there by the session's id (SM-8.2,
  SM-8.3).
- SM-9.3 **The way back is the switch.** A stream source that fails to start, or whose process ends in the first
  seconds, says so in a notice that offers to open the folder again with the screen source: axClaude started again
  on the same folder for the screen, and this window closed.
- SM-9.4 **Say the event sources** names the source in force first: "Source: the screen" or "Source: the stream".
- SM-9.5 **axHelp's conversation** stays on the screen source until the stream has been used for a while.

## 10. Checks

- SM-10.1 **The stream's model from recordings** (root `CLAUDE.md`, rule 12): the probe records each of its cases
  as a stream fixture: every line Claude Code wrote, reduced to the fields the reader takes, and what the host
  wrote between them, on one clock. The recorder is the whitelist that makes a fixture safe to commit, as the
  timeline fixtures' maker is: a field not named is not kept, every kept string is masked, the session's id and
  the other ids become numbers, and a file that still holds a name, an account path or an address is refused and
  deleted. The model is replayed over each fixture in the tests, and what it shows, its facts after each line and
  what it raised stand in a file beside the fixture, read through when it changes. The cases: a turn, a tool
  call, a question, a permission each way, an interrupt, a message sent mid-turn, a message that waits behind a
  turn, compaction, `/clear`, a slash command's print, a subagent with its hand-back, a failure. What the model
  does with a kind of line is settled from these, never from the documentation alone.
- SM-10.2 **A stand-in of the stream** for the window scripts, as the screen has one (`AxAutomate standin`): it plays
  a scenario of lines and answers what the window writes. The window scripts of stream mode run with it, without
  Claude Code.
- SM-10.3 **The seam's proof** (stage 1): the whole test set, every stand-in script and the whole live set pass
  unchanged, and the timeline replays give the same files.
- SM-10.4 **Live**: the probe's cases already hold Claude Code's side. Live window scripts for stream mode join
  `tools\upstream.ps1 -Verify claude`: a turn, a permission answered each way, a question of Claude's, an
  interrupt, a resume.
- SM-10.5 **By ear**: a section of the NVDA plan for stream mode, and two hours of the maintainer's own use with the
  switch on before it leaves the Developer menu.

## 11. The path in stages

Each stage is small, leaves every app working, and is committed by itself. The screen source is the default
through all of them.

1. **The seam, nothing else.** `ClaudeModel`; `SessionModel` derives from it; `_model` takes the type;
   `ProcessRunning` virtual. No behaviour changes. Proven by SM-10.3 and an NVDA pass. Built 2026-11-17 (section 13).
2. **The stream's model and host**, in `AxClaude.Core`, with their tests. No window uses them.
3. **The relay**: the verb, the pipe, the configuration file; tested with a stand-in of Claude Code's side.
4. **The window's second ways to send** (section 5) and the start of the stream source, behind the switch, with the
   stand-in scripts. Off.
5. **The folder's trust, the questions in the control area, always for the session.**
6. **Sessions**: the older conversation from the file, the id kept, the hand-over between the sources.
7. **What the stream does not have, said plainly** (section 6), and the reply's text made plain (SM-4.5).
8. **Live scripts, the NVDA plan's section, the user guide.** Then the maintainer's two hours.

## 12. For the maintainer to decide

1. Whether this is the seam: one model with two sources, the screen's machinery left where it is. Decided
   2026-11-17: yes, and the first stage built at once.
2. The trust question's words, and whether a yes is remembered for a folder or asked at every first start.
3. The permission question's three answers and their words; whether "do not ask again in this session" is offered
   at all.
4. What a menu item says that the stream cannot do: one short sentence, the same everywhere.
5. The words for Claude Code's own notes: compacting, the conversation emptied, the mode after a change.
6. A reply's text: made plain as the screen shows it today (proposed), or shown as the Markdown it was written in.
7. A command Claude offers, in stream mode: said to be unavailable until measured (proposed), or measured first.

## 13. As built

**Stage 1, the seam (2026-11-17).** No behaviour changed.

- `AxClaude.Core/Transcript/ClaudeModel`: 33 members, 14 that every source fills in and 19 that only a drawn
  screen has. It keeps nothing itself.
- `SessionModel`: 34 declarations changed by a word or two each (the class's own line, and 33 members that gained
  `override`, a private setter becoming a protected one); no other line of its 1,638.
- The window: `_model` is a `ClaudeModel`. The compiler then named five places that ask for the screen's own
  model, every one of them in the machinery beside the screen, as section 3 had said; they ask `_screenModel`. One
  line moved: the core's listening to the turn marks, from the constructor into `WireFlow`, still ahead of the
  hooks'.
- `ConsoleForm.ProcessRunning` is virtual, and the music asks it. No window fills it in yet.
- `SourceSeamTests`, 24 tests (SM-3.7).
- The proof (SM-10.3): the 17 test projects, with every golden transcript of the fixtures and every timeline
  replay unchanged; the frame pass of the screen model at 18,000 lines, timed with the pure code as built before
  and after: the same within the computer's noise, and the same bytes allocated; and the whole live set. The
  NVDA pass is owed with the next release's.
- Found on the way: a window is made for its source and keeps it (SM-9.2), since the window base is handed its
  model as it is made. And the notes of axClaude's window, which stood at their size limit, have two files of
  their own in `src/AxClaude/notes`.
- The live set found, the same day, that Claude Code had updated itself to 2.1.327 two minutes into the run and
  no longer showed its permissions screen in the window: a new key on that screen's row of keys, repaired in
  Claude Code's own table (`claude-screens.md`, Panels). The pure code as built before the seam read the recording
  the same way, which is how the two were told apart.

**Stage 2, begun: the recordings (2026-11-17).** The probe records each of eighteen cases as a stream fixture
(`tools/ClaudeProbe stream --record`, `StreamFixtureMaker`; `tests/fixtures/streams`, with a page that says what a
file holds and what the recordings showed). What they showed that the model must follow, beyond section 4:

- A message's line comes back just before its reply, never earlier. Written while a tool runs, it is taken inside
  the running turn and one end line ends both. Written while a reply is being written, it starts a turn of its own
  right after, and the first turn's end line says that nothing waits. So whether Claude works cannot be read off
  the end line's count: it works from a message written while idle, or a start line, until an end line that leaves
  no message of the user's still to come back.
- A slash command's text does not come back. What it prints comes as a text of the assistant's in a turn that counts
  no turn; so a text is a command's print, and no reply of Claude's, when the turn was started by a slash command
  the window sent and no message has come back in it.
- A turn Claude Code starts by itself (work in the background ended) is a start line with no message before it,
  then a reply and an end, with no line of the user's.
- A tool call that failed or was refused has its result marked as an error with the words; a refusal is also
  counted on the turn's end line. A question of Claude's is a call of its own tool, so its line is no tool line.
- A failure of the service ends the process.

Still to build of stage 2: what the reader gives a model (a message come back with its text, a call's id and what
it is about, a result's failure, the list of work in the background), the model itself replayed over the fixtures,
and the host.
