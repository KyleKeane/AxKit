# axClaude: the stream as a second source, behind a switch

A specification, written 2026-11-17 for the maintainer's decision of that day (the stream as a second mode behind a
switch, the screen the default and the fallback; `docs/axKit/events.md`,
section 7, step 3): Claude Code's non-interactive stream becomes a second way for axClaude's window to learn what
Claude does, off at first, with the screen as the default and the way back. Requirements are numbered SM-n.m. Stages
1 to 5 of section 11 are built (section 13 says what each came to): the seam, which changes no behaviour; the
stream's reader, model and host, from recordings of the real stream; the relay of Claude's questions; the window
fed by the stream, with its second ways to send; and the folder's trust and the questions in the control area. A
window made for the stream can hold a conversation and answer what Claude asks. Since 2026-11-25 a reply's text is
made plain (stage 7, SM-4.5), and the sessions are built (stage 6, section 13, the last part): the older conversation
shown when a session is taken up, and the hand-over between the two sources. Since 2026-11-26 the NVDA plan has its
section for the mode (stage 8; its pass by ear is owed), and the nine things to build before the stream could be the
default are built (section 13, the last parts, the two rounds of the must-build list): what a tool printed, the choice
of an earlier conversation and plan approval among them. Not done yet: the maintainer's own use (stage 8; the live scripts 48, 49
and 52 are there). The open ends stages 4 and 5 left are
built since the same day (section 13, the last part), but for the folder's yes, which is remembered for no folder
until the maintainer says so. The points of section 12 are taken as proposed until the maintainer has heard the mode.

It rests on three things made the same day: the measurements (`tools/ClaudeProbe stream`, twenty cases, all
passing; its README, section 9), the door
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
  no effect), and what the window sets of it is not kept: `PendingScreen`,
  `PromptHolds`, `FeedbackDrafts`, `DraftTitle`, `DetectScreens`, `Spinner`, `SessionName`, `StartName`,
  `TranscriptViewOpen`, `BackgroundAgentsAwaited`, `BackgroundWorkflowsAwaited`, `BackgroundUnknown`,
  `ShowProgressRows`, `HideReplay` with `ShowHiddenLines` and `MarkReplayedExchanges`, and
  `SaidNoConversationToContinue`. So the window's code for those things is never entered while the stream feeds
  it, and is not copied, branched or removed. `TextPrompt` and `AwaitsText` were on this list as planned; since
  stage 5 the stream's model fills both, for the user's own words after Other and after "No, and tell Claude why"
  (SM-7.4, SM-7.5), so that the window's own field asks for them. The other way round, what only the stream's model
  has and the screen's answers with nothing: `Asked` and `StillAsks`, the number of what waits now, by which a
  control of the window answers what it was shown for and nothing else (SM-7.4; a screen is typed its answer, and
  every control shown for it may answer).
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
  taken from the source, where they are certain. Built 2026-11-25: `Streaming/ClaudeCodeMarkdown`, its rules read off
  the fixture `long-reply` (bold, italics and code spans lose their marks; a fence goes and its code stays as written;
  a table row is "Column: cell." for each cell; a link whose text is its address is the address, another is "text
  (address)"; a quote loses its mark; lists stay), tested in `ClaudeCodeMarkdownTests`. Its patterns run on the
  window's thread, each with a limit of 200 ms for one match, and a line a pattern does not get through in that time
  is left as it came (2026-11-27: the pattern for bold took time growing with the square of a line's length on a
  long line of marks that never close).
- SM-4.6 **The turn**: working from the message taken to the turn's end line; the end is raised as the model's
  `Bell`, so that the window says Done by its existing rule, after the reply's lines, which are in the model before
  the end line arrives. An interrupted turn says no Done: it says Interrupted. A turn that failed says Done as any turn does, after its error line, as with the screen (amended 2026-11-17: said nothing, it left the user without a word). Two things that left Claude working for good until 2026-11-27: an end line whose count of turns or of waiting messages is no number (a null) was passed over whole, where it is now an end with a count of 0; and a message Claude Code gives back with another text than the window wrote (it takes characters that are not seen out of a prompt) was found by its text alone, where it is now the oldest message still awaited.
- SM-4.7 **What Claude waits on**: a question from the relay (SM-7) is the model's `PendingQuestion`, a `Question`
  as the control area shows one today: its title, its text, its options, whether several may be chosen. The area
  shows it by its existing code.
- SM-4.8 **Work in the background**: from the stream's task notes and its list of background tasks: the count, each
  task's kind and description. The window's sounds and words for it are the existing ones.
- SM-4.9 **Claude Code's own notes** become the model's `Announcement`: compacting and its end, the conversation
  emptied, the permission mode after a change. The words are chosen with the maintainer (section 12).
- SM-4.10 **Ready** is the process being up and answering (the start line comes only with the first turn): the
  stream source writes one request at its start that changes nothing, and the answer is the sign, some 0.65 s
  after the start. Measured 2026-11-17 (the probe's case initialize, the fixture `start`): the request is the one
  the Agent SDK sends first, `ClaudeCodeStream.InitializeLine`, and its answer names the permission mode in force
  and Claude Code's version before any message. So the model knows the mode when the answer is in, and "Ready" is
  said by the window's existing rule, which waits for a mode, as with the screen. The answer also lists the
  commands the run takes, with their descriptions, and the models with their effort levels, which the window's
  own lists for a command, the model and the effort can be made from (SM-5.5); and it names the account, which is
  never read. A Claude Code that does not answer the request is taken to be up once the request has waited five
  seconds while the process runs (`StreamModel.FirstRequestUnanswered`, `ClaudeModel.Up`), and Ready is said then
  by the same rule, not only after the first turn (built 2026-11-25).
- SM-4.11 **What a tool printed** (2026-11-26). The screen shows a call's preview rows under
  its tool row: measured with Claude Code 2.1.332 (section 13), the first three lines of a command's output and then
  "… +9 lines (ctrl+o to expand)", or "(No output)". The stream gives every result whole (the fixture `tool-output`). So
  under each call that went well the conversation shows the first three lines of what came back and, when there are
  more, a line "… +9 lines" (the hint goes: the window does not send Ctrl+O), as plain lines, the kind the screen's
  preview rows have, so that the keys treat them alike: a reply ends at a tool's line, so they are never spoken, and
  p does not stop on them. What a command that printed nothing gave back is shown as Claude Code gave it ("(Bash
  completed with no output)"). Each output goes under its own call, also when calls made together are answered in
  another order. A call that failed keeps its first line under its `tool error:` line, as before (stage 2). The words
  of a refusal or an interrupt meant for the model alone are not shown, and only on a result that failed, which each
  of them is: a call that went well and printed a text beginning with the same words is shown as any other
  (2026-11-27; until then the first words alone decided). The whole output is not kept (2026-11-27): the lines shown
  and the count of the rest are all the conversation has of it (`Streaming/ToolOutput`, made for a result and let go).
  Until that day every call's whole output was kept, by its call's line and by each line shown, for a key that reads
  all of it, which was never built since the screen has none; such a key would need the model to keep each call's
  text by its line again, under the conversation's cap. A result whose call's line the cap has let go adds nothing
  (until 2026-11-27 its rows went in at the top of the conversation). The older conversation of a session taken up
  shows no output (the session file's reader never reads a result's text, EG-3.4).

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
  window asks for it in the control area first. Built 2026-11-25: Choose the model and Choose the effort show a list
  of the window's own (`Streaming/StreamChoice`), the models by the names Claude Code shows with its words about each,
  the one in use marked when the start line names it by the same name, and the efforts every model takes, each once,
  low to high, none marked (the answer does not say which is in force); Enter sends `/model` or `/effort` with the
  value as the user's own typed command, Escape and the interrupt send nothing. Measured with Claude Code 2.1.332
  (the fixture `choices`): both are taken while the run goes on, each answers with one line of print and an end that
  counts no turn. Before the first request is answered there is nothing to list, and the item says so. A question
  of Claude Code's that comes meanwhile goes first, and the list closes unanswered. A skill or a custom command runs
  the model: its own line comes back as Claude Code's tags, which are read down to the command and never shown, and
  its texts are Claude's replies (the fixture `custom-command`). The turn in which that line comes back is the
  command's, also when a message was written before its start line (2026-11-27: until then such a turn's end left
  the command awaited, and Claude working, for good). What a command prints in a line of its own, in the user's
  place inside Claude Code's tag (`/compact`: "Compacted", the fixture `compact`), stands as plain rows under the
  command's echo, as a print that comes as a text does (2026-11-27; read and dropped until then).
- SM-5.6 **A command Claude offers** (the `!` line of an offered command): measured 2026-11-25 with Claude Code
  2.1.332: a `!` line sent over the stream is no shell command; the model gets it as a message, and may call its own
  shell tool for it. So an offer in stream mode says that it cannot be sent this way and how to run it (section 12).
- SM-5.7 **Not sent at all in stream mode**, each said in a few words when asked for: Send queued messages now, the
  Send key submenu (Ctrl+C, Ctrl+D, Tab, the arrows), Ctrl+O's guard. Their menu items stay, so that the menus are
  the same in both modes, and say "Not with the stream source" (the words are section 12's), raised at the door as
  the kind `not-with-source` (since 2026-11-26; `said` before), which has no sound until the maintainer has chosen one.
- SM-5.8 **A typed slash command** (2026-11-26). The answer to the first request lists the
  commands the run takes, the same as each start line (measured: 69 with 2.1.332, the user's own among them; the
  model keeps them, `StreamModel.Commands`, and they are never logged as a list). A typed text is read against that
  list (`StreamModel.TypedTextOf`): a text whose first word is the slash and a name (a path is a message) is a command
  the run takes, or one of Claude Code's own (by name or alias, `SlashCommands`) that the run does not list, or a name
  the run does not know. Measured with the probe's cases `command-lists` and `screen-commands` (section 13): a
  command of Claude Code's screen alone is answered by Claude Code itself, "/help isn't available in this
  environment.", with no turn of the model's; a name it does not know goes to Claude as a message. So:
  - a command of the screen alone is **never sent**: the window says "Not with the stream source: /help", by the door
    as `not-with-source`, and the text stays in the field;
  - a name the run does not know is **held**, as a message is held during a question (SPEC.md FR-2.10): "Not sent:
    Claude Code takes no command /name. It would go to Claude as a message. Enter again sends it so"; sent again, it
    goes as a message and gets a message's block;
  - before the run has listed its commands (a Claude Code that does not answer the first request), every command is
    sent, as before.
- SM-5.9 **A typed `!` line** (2026-11-26) is held the same way: the screen runs it in Claude Code's shell, the
  stream gives it to Claude as a message (SM-5.6). "Not sent: over the stream a ! line is no shell command. It would go
  to Claude as a message. Enter again sends it so."

## 6. What the stream does not have

The inventory found eighteen things of today's window that exist only because Claude Code draws a screen. In stream
mode each is simply absent, and the window says so where the user would look for it.

- SM-6.1 **Claude Code's own screens**: `/config`, `/status`, `/tasks`, `/help`, `/permissions`, the review of
  answers, the resume picker, `/btw`'s side answer, the feedback survey, drafts and their review. Not shown. What
  the user needs of them has other ways: the settings are axClaude's own menu already (SPEC.md FR-14), a
  conversation is resumed by the window (SM-8), and `/context` prints as text. `/status` was written here as printing
  too; the run's list of commands does not name it (measured 2026-11-26 with 2.1.332, never sent: it prints the
  account), so the window says it is not with the stream source (SM-5.8). `/usage` is listed and is sent.
- SM-6.2 **`/login`** and the other commands of the terminal alone: signing in is done with the screen source. Since
  2026-11-26: a typed `/login` is a command of the screen alone (SM-5.8); an error of Claude Code's that names
  `/login` (an error text of the service's or a failed turn's end, `ClaudeCodeStream.AsksToSignIn`; or the lines it
  wrote as errors as it ended) opens the notice that offers the folder with the screen (SM-9.3), titled "Claude Code
  asks you to sign in", once for a run; and the window's sign-in reminder (bundle AX-11.6) is not said over the
  stream, where nothing signs in. The error's words over the stream are not recorded (it would take signing out); they
  are taken to be the screen's, which name `/login`.
- SM-6.3 **The mode line's own words, the spinner's words, the session's name**: the status bar says working,
  ready or waiting, the mode, and the count of background work; no spinner text, no session name.
- SM-6.4 **The count of waiting messages while a turn runs**: the window counts the messages it wrote and that have
  not come back.
- SM-6.5 **The raw recording** of the console's bytes: with the stream source, Developer, Record raw stream records
  the stream's lines instead, to a file that holds the conversation and is never committed. Built 2026-11-25: every
  line Claude Code wrote, as it came, one a line, in a `.jsonl` file the save dialog proposes in
  `%LOCALAPPDATA%\AxKit\recordings` (outside the project and the synced Documents folder, as the console's), with the
  same index of each line's time beside it; `--record` does the same from the start. It is the raw stream, as the
  console's recording is: it holds the user's words, Claude's, the folder's paths and the account the first
  request's answer names, and is read only through a redacting tool, never committed. What the window wrote is not
  in it, as the console's recording holds no keystroke. A recording that cannot be written (a full disk, a locked
  file) stops and says so, "Recording stopped, the file could not be written", and the stream goes on being read
  (2026-11-27: until then the failed write ended the reading of Claude Code's output without a word, while Claude
  Code ran on; `ConsoleForm.Record`, and `StreamHost` no longer takes a failure of what handles a line for the pipe's).
- SM-6.6 **The console's size, screen reader mode, the detailed view**: no meaning without a screen.
- SM-6.7 **What is not promised** (Claude Code's documentation, read 2026-11-17 and 2026-11-26). No page promises the
  lines' shape; the start line's `capabilities` are there "to feature-detect instead of comparing version strings",
  and unknown values are to be ignored, so additions are expected. The requests the window writes (the first request,
  the interrupt, the mode) and the lasting yes (`updatedPermissions`) are documented as the Agent SDK's methods alone,
  not as lines. The documentation calls `-p` the "alternative for scripting": new features reach the interactive
  screen first, and the terminal's own commands are not available in print mode. What was measured is narrow (the
  smallest model, new folders, short runs). The guard is `tools\upstream.ps1 -Verify claude`, which plays the probe's
  cases and writes `stream-shape.json` again, so that a new version's change shows as a diff; the screen source stays
  the way back (SM-9.3), and with axHelp's conversation on it (SM-9.5) the screen and its parser stay maintained,
  whatever the default.

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
  Each end asks who the other is: the relay writes its secret only to a pipe this same program file serves
  (`ThisProgram.Serves`, the 2026-11-21 audit), and the window takes a question only from a connection of this same
  program file (`UserPipe.ClientOf` with `ThisProgram.Runs`, 2026-11-27), dropped before anything of it is read. The
  pipe's name and secret are in the environment of everything Claude Code starts, so until then a command Claude ran
  could connect by itself and put a forged question to the user; its answer went to the forger, never to Claude
  Code. What cannot be closed while the secret is in the environment: such a command can start `AxKit.exe ask`
  itself, with that environment, and write it a question, which the relay passes on as any other; the user is then
  asked something Claude Code never asked, and the answer still reaches only the command that asked. A question and
  its answer are read and written to one depth everywhere (`ClaudeCodeStream.AskDepth`, 1024 levels; until 2026-11-27
  the relay read to 1024 and the window to the parser's own 64, so that a call nested deeper was refused unasked),
  and the answer that hands a call's input back is written without the escapes a page would need, so that it is no
  longer than the question (until then a large call's answer could pass the frame's limit, and the user's yes came
  back as "the window could not be reached"). A question the window cannot read is answered no in words of its own,
  "axKit could not read this request, so the user was not asked and it was not allowed" (`AskRelay.NotReadWords`),
  with a line in the conversation; until 2026-11-27 Claude was told that the user refused.
- SM-7.4 **A permission question** is shown as a question with five answers, No first where a list starts: No;
  No, and tell Claude why (since 2026-11-26); Yes, once; Yes, and do not ask again for this tool in this
  session; and Explain what Claude asks to do, which answers nothing (SM-7.6). **An answer belongs to the question it
  was given to** (2026-11-27, found by the review of that day; bundle rule 11): the next request is the model's at
  once, while the control area shows the list before it until the settle, 400 ms later, and every list's Enter
  handed its key to whatever was asked then, so that Enter on the third model of Choose the model was "Yes, once"
  for a tool call that had just come. Each thing asked has a number of the model's (`StreamModel.Asked`: a request,
  each step of it, the user's own words, each list of the window's own), each list and field of the area is bound to
  the number it was shown for, and its Enter and Escape do nothing but say "Not sent: Claude asks something, and its
  question opens in a moment" once that number no longer waits (`StreamModel.StillAsks`, `MainForm.StillAsked`); a
  typed answer is held the same way. Where this is tested (the second review of 2026-11-27): the moment is the
  400 ms between a question's arrival and its list, which no script can see or hit, so the rule is tested where it
  is decided, without a clock: the model's numbers in `StreamModelTests`, and in `WindowLastResortTests` that no list
  or field reaches the control area but through the one method that asks first (`MainForm.AreaList`, `AreaField`).
  `tests/scripts/axClaude/62-stream-answer-bound.txt` plays what is certain whenever it is played: the call that
  comes while a list of models shows takes the list's place, is asked with its caution, takes no Enter in its first
  moment (bundle AX-6.4: every control of the area holds its answering keys for a moment after it is shown, whatever
  came before), and is never allowed by anything but the user. "No, and tell Claude why" answers nothing yet: the control area's field asks for the user's words as after
  Other ("You chose No, and tell Claude why. Your answer"; Escape goes back to the list), and they go back with the
  refusal, `ClaudeCodeStream.RefusedWithWords`, as the refused call's result; measured with the probe's case
  `relay-refused-why` (section 13): Claude reads them and follows them. The conversation keeps "You refused Claude's
  use of Write and told Claude: …" (`StreamAsking.ToldWhyLine`); the words are not shown again under the call, and
  words that happen to be the key of an answer are words (until 2026-11-27 the words "4" refused the call and left
  the line that says the tool was allowed). The
  lasting yes is a rule of the session's alone
  (`ClaudeCodeStream.AlwaysAnswer`): it is written to no settings file and a new session asks again. The question
  names the tool and shows what it would do (the command, the file), read from the request. Every answer carries
  the tool and its target in a few words, "1. No, Write axclaude-permission-test.txt", "3. Yes, once, Bash cat
  settings.json" (since 2026-11-26, `PermissionCore`, `Question.Core`; the tool alone when the subject is Claude's own
  description of the call; ", and more" after a target of which words were left out, "1. No, Bash ping -n 2, and
  more", since 2026-11-27), so that moving through the answers says which question they belong to; the answer that
  goes back is the key's, as before. The question is said once, by the list the focus moves to, and a character
  typed there that starts no answer reads it again (SPEC.md D33, FR-7.4b; one code with the screen's).
- SM-7.5 **Claude's own questions** (several questions, each with options, some with several answers, each with
  Other for the user's own words) are asked one after another in the control area as today, and go back in one
  answer when the last is answered. Escape answers No to the whole. The answer is written into the conversation as
  the screen's record of it (since 2026-11-25): "User answered Claude's questions:" and a row for each question with
  its answer, `LineKind.AnswerRecord`, the words `ClaudeCodeTexts`'s (`StreamAsking.AnswerRecord`).
- SM-7.6 **What an AI asks for says so** (bundle rule 11): a permission question is Claude's request to run
  something; its first words say who asks, and running is never where the list starts.
- SM-7.7 **Plan approval** (2026-11-26). In plan mode (Ctrl+P reaches it, SM-5.4) Claude
  leaves it by calling its plan tool (`ClaudeCodeStream.PlanTool`), and Claude Code asks its permission tool first, with
  the plan in the call's input (measured, section 13). The screen asks "Ready to code?". The window asks it as a
  question of its own kind (`StreamAsking.IsPlan`): its title "Claude has a plan and asks to carry it out", its text the
  caution, heard every time (`AiCaution.PlanAsked`), and then the plan, its Markdown made plain (SM-4.5); four answers,
  the screen's three with running never where the list starts: "1. No, keep planning, and tell Claude what to change",
  which asks for the user's words in the control area's field as "No, and tell Claude why" does and sends the plan back
  with them (`ClaudeCodeStream.KeepPlanning`; Claude stays in plan mode, plans again and asks anew, measured); "2. Yes,
  and manually approve edits"; "3. Yes, and use auto mode"; and "4. Read the whole plan", which answers nothing and
  opens the plan as a text, its caution first (`AiCaution.Plan`). A yes lets the call run and sets the mode the work
  goes on in, in the answer itself (`ClaudeCodeStream.AllowWithMode`, the mode `default` or `auto`): Claude Code's next
  status names it, and the window says the mode as for any change of it. Escape on the list is no, as for any tool call.
  The conversation keeps, after a yes, "You approved Claude's plan, and approve each edit yourself:" (or "with auto
  mode:") and the plan, as rows of the answer's record (`LineKind.AnswerRecord`, never spoken), and after the no that
  keeps planning "You sent Claude's plan back to keep planning and told Claude:" with the words. No answer carries a
  core (SPEC.md D33: the title names no target).

## 8. Sessions

- SM-8.1 **Continue**: the default start takes up the folder's last conversation. With the stream, `--continue` in
  a folder without one starts a new conversation and says nothing (measured), so the window's retry for the screen
  is not needed. As built, the stream's window names the session itself: every `--continue` of its arguments
  becomes `--resume` with the folder's newest session file, which is what the stream's own `--continue` takes up
  (measured), so that the window knows before the start which conversation it is. Its short form `-c` is read as
  `--continue`, and a session named as one argument, `--resume=<id>`, as a session named (2026-11-27;
  `SessionHandover.Continues`, `ResumedIn`).
- SM-8.2 **The older conversation**: a session taken up again writes nothing of what was said before (measured).
  The window fills its view from the session's file, read by the reader it has (`ClaudeCodeTranscript`), as markers
  and lines of the same kinds, before the first new line. A file it cannot read leaves the view empty with one
  system line that says so. As built, the lines are those the screen's model makes of a replayed conversation:
  `# Input n from previous session`, the `you:` line, `# Output n from previous session Reply from Claude` before the
  first reply line, the replies and the tool lines (the tool's name alone: the reader never reads a call's input),
  numbered apart from the live exchanges, and nothing of them spoken. Only into a view that holds no conversation
  yet, so that a restart in the window never shows it twice.
- SM-8.3 **Handing a conversation between the sources**: Claude Code leaves a stream's sessions out of the
  interactive program's own list and of its `--continue` (its documentation; measured: also after the screen has
  taken such a session up). So the window keeps the id of the session it last ran in each folder, and a switch of
  the source in the same folder takes that session up by its id. Measured 2026-11-25 (section 13): either source
  takes the other's session up by its id and writes on in the same file. As built, a window the stream feeds keeps
  its session for the folder (the setting `streamSessions`), and a window the screen feeds turns `--continue` into
  `--resume` with it while it is still the folder's newest session; the other way needs no id, since the stream's
  window takes the folder's newest session, which is the screen's after the screen has used it.
- SM-8.4 **`/clear`** begins a new session and says so with a line of its own (measured): the window empties the
  conversation as it does today and keeps the new id.
- SM-8.5 **Choosing an earlier conversation** (2026-11-26). A `--resume` with no id (New
  session's preset "Choose a conversation to resume", or the user's own) opens Claude Code's own list of conversations,
  a screen the stream does not have. So before the start the window asks with a list of its own
  (`Streaming/ConversationChoice`), shown by its code for a question, as Choose the model is (SM-5.5): the folder's
  session files, newest first, each named by when it was last written and the first words of its first message
  ("26 November, 14:05: Remember this word for later: kiwi."; eight words and 60 characters at most, an ellipsis when
  cut). The files are read off the window's thread, each only to its first message within its first 256 KB; a file
  without a message of the user's there is left out. The session file is Claude Code's internal and private record:
  nothing of it is logged, the log has the count alone, and no more than those first words is shown. Enter starts
  Claude Code with `--resume` and the conversation's id, which become the window's arguments (a restart takes the same
  conversation up), and the older part is shown as for every conversation taken up (SM-8.2). Escape, or the interrupt,
  starts nothing: the conversation says so and names Ctrl+Shift+R, which asks again. A folder with no earlier
  conversation begins a new one and says so. While the list waits, the status bar says "starting". A `--resume`
  followed by a word that is neither a session's id nor an option is Claude Code's search for its own list: the
  window asks with its list all the same, and the word goes with the option when a conversation is chosen
  (2026-11-27; until then it stayed, a stray argument after the chosen id).

## 9. The switch and the way back

- SM-9.1 **One setting**, `claudeSource`: `screen` (the default) or `stream`, and one option of the command line
  (as built: the check item Options, Developer, Feed the next window from Claude Code's stream, which says
  "Claude Code's stream on, from the next axClaude window. This window keeps the screen"; `--source screen|stream`)
  that names the source for one start, for the window scripts and for the way back. At first in Options, Developer,
  beside the event sources; it moves to Options when the maintainer has used the stream for a while.
- SM-9.2 **One window, one source.** A window is made for its source and keeps it for as long as it is open: the
  model is made once, with the window, and nothing of an open window is rebuilt under the user (found while the
  seam was built: the window base is handed its model as it is made). A change of the setting applies to the next
  window of axClaude, and the window says so. The conversation is found again there by the session's id (SM-8.2,
  SM-8.3).
- SM-9.3 **The way back is the switch.** A stream source that fails to start, or whose process ends in the first
  seconds, says so in a notice that offers to open the folder again with the screen source: axClaude started again
  on the same folder for the screen, and this window closed. The new window is the window this one is, for the other
  source: its title, its model and hiding on close come along (`StartupOptions.RestartArguments`, 2026-11-27; until
  then the folder alone did, and axHelp's conversation came back as a plain window with another model), and this
  window closes for good also when it hides on closing, so that one window of its title is left.
- SM-9.4 **Say the event sources** names the source in force. As built: in a window the stream feeds it says
  "Source: the stream. Claude Code runs. Lines read: 12. Of a kind not known: 1. Questions asked: 2." (or "is
  stopped"); in a window the screen feeds it says what it always said, which names the screen's own sources, so
  that no sentence of the screen source changed. Copy diagnostics names the source on a line of its own in both:
  "Source: the screen" or "Source: the stream", the latter with the stream's numbers under it.
- SM-9.5 **axHelp's conversation** stays on the screen source until the stream has been used for a while.
- SM-9.6 **The copy from the Microsoft Store** (2026-11-26): the stream does not start there, since it is not known
  that Claude Code can read the relay's file under the package's folders, and without the relay nothing could be
  asked (SM-7.2). So there the setting does not choose the stream and the default stays the screen
  (`StartupOptions.Streams` with the Store's copy told); the switch in Options, Developer only says why, "The copy of
  axKit from the Microsoft Store cannot start Claude Code over its stream yet, so every axClaude window is fed from the
  screen", and changes nothing; a `--source stream` named for one start is followed, and that window's notice says the
  same and offers the screen (SM-9.3). Moving the file where Claude Code can read it is a measurement still to make.

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
  switch on before it leaves the Developer menu. The maintainer's direction of 2026-11-25: those two hours come first;
  if the stream holds, it becomes the default and the screen's grounding in the session file
  (`events-and-grounding.md` stage 3) is parked in `plan.md`; until then no work goes into that stage. What the two
  hours should try (2026-11-26):
  - A long reply: is the wait before speech heard? The window does not ask for a reply's pieces, so each reply line
    comes whole; a reply of about 300 words is whole some 3.5 seconds after its first piece (measured), where the
    screen speaks its rows as they settle. The pieces (`--include-partial-messages`) are built only if the wait is heard.
  - A task with many tool calls: is the output under each call enough (SM-4.11)?
  - Plan mode with Ctrl+P, until Claude asks to start: what is heard (SM-7.7)?
  - Refuse a tool call with No, and tell Claude why: does Claude do as told (SM-7.4)?
  - The slash commands he uses with the screen, such as `/config`, `/status`, `/resume`, `/rewind`, `/tasks`: what is
    heard for each (SM-5.8)?
  - A `!` line, to hear the difference (SM-5.9).
  - A message sent while Claude works: Queued, then its block.
  - An interrupt during a tool call, then a new message.
  - An agent and a shell in the background: the sounds and F8.
  - Close the window and open the folder again: the older part shown. Then the screen, and back (SM-8.2, SM-8.3).
  - The trust question at each new window: is it tolerable, or is a yes to be remembered (SM-7.1; section 12, point 2)?
  - Are Claude's thinking lines or the turn's summary line ("for 5s, done 2:48 PM", the d key) missed? The stream
    shows neither: the thinking only starts Responding, and the end line holds the turn's duration.
  - "Done" right after the reply, never early: the gain the stream was built for.
  - The window in the background at a turn's end: the louder alert.

  Worth building soon after the default, not before (proposed 2026-11-26): the turn's summary line, made from the end
  line's duration and the clock under FR-14's switch, so that the d key works; a list of the commands the first
  answer names with their descriptions, Enter putting one into the message field, as Choose the model is made; a
  command Claude offers, asked still, with Copy and Open axTerminal in the folder in place of Yes, run it (SM-5.6).
  Proposed to stay the screen's: `/config` beyond FR-14's rows (a row added when one is missed), `/usage` and `/cost`,
  `/permissions`, stopping one background task, `/rewind`, the other screens of section 6; let go: `/btw` and `/goal`,
  the feedback surveys, Send queued messages now, the Send key submenu, the console's own settings, a tool call's
  input in the older conversation (EG-3.4), the spinner's word. Dynamic workflows over the stream wait on a recording
  of how Claude Code's list of background tasks gives them.

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
6. **Sessions**: the older conversation from the file, the id kept, the hand-over between the sources. Built
   2026-11-25 (section 13).
7. **What the stream does not have, said plainly** (section 6), and the reply's text made plain (SM-4.5).
8. **Live scripts, the NVDA plan's section, the user guide.** Then the maintainer's two hours. The NVDA plan's section
   is written (2026-11-26); its pass by ear is owed.

### Stage 4 in steps (planned 2026-11-17)

Stage 4 is built on a branch, `stream-window`, and comes to `main` when it works as a whole: a setting that does
nothing, or a window that starts the stream and cannot yet take a message, must not stand on `main`. Each step is
a commit on the branch with its tests.

1. **The choice.** The setting `claudeSource` (`screen` at first, `stream`; any other word is `screen`) and the
   option `--source screen|stream` of the command line, which goes before the setting for one start. The window's
   constructor makes the model the source asks for; from then on the window keeps it (SM-9.2).
2. **Two fields, one of them set.** `_screenModel` for the screen's model and `_streamModel` for the stream's, each
   asked only in its own files: the machinery beside the screen for the one, a new `MainForm.Stream.cs` for the
   other. With the stream, the machinery is not wired: no turn marks, no session file's reader, no hooks, and the
   event core is handed nothing and decides nothing, so that the window's rules for a source without the core are
   the ones in force. `SourceSeamTests` holds both fields to their files.
3. **The start and the end.** `MainForm.Stream.cs`: Claude Code's line for the stream (`ClaudeCodeStream.Options`,
   each message sent back, the relay as the permission tool with its configuration, the user's own arguments and
   the window's model), its environment with the relay's pipe, and `StreamHost`. Lines are handed to the window's
   thread in batches and from there to the model. The first request goes out at once, and its answer makes the
   mode known, which is what the window's rule for Ready waits for. The base's `ProcessRunning` answers for the
   stream's process too; stopping Claude ends it with what it started; its end is reported as the screen's is, with
   the end of what it wrote as errors shown to the user and never logged.
4. **Sending.** A message is one line (`UserLine`) and its block is the model's; Interrupt and Escape are
   `InterruptLine` while a turn runs; the mode key is `ModeLine` with the next mode; a slash command is a message.
   What has no meaning without a screen says so in one sentence (SM-5.7).
5. **The questions' pipe, answering no.** The window's end of the relay's pipe, open to this Windows account alone:
   until stage 5 shows a question in the control area, every question is answered no, which is the rule for a
   question the window cannot show (SM-7.2). So a stream window of this stage can talk, and can run nothing that
   asks.
6. **Speech and the tick.** The pass that says which reply lines are final (`Arrivals`) is told that a line of
   this model is final when it is there.
7. **A stand-in of the stream** for the window scripts, played from the stream fixtures themselves: it waits for
   what the window writes where a fixture has a line of the host's, and writes Claude Code's lines in between.
   The window scripts of stream mode run with it, without Claude Code (SM-10.2).
8. **The documents.** The app's specification and user guide (the option under Developer), the privacy statement,
   both READMEs and the security page (the stream and the questions' pipe, in these terms), the outside-calls
   page, the NVDA plan's section.

Built 2026-11-17: steps 1 to 7, with step 5 gone straight to stage 5's questions in the control area, so that no
build ever answered every question with no. The documents of step 8 are written with the release that first holds
the mode. Section 13 says what each came to.

## 12. For the maintainer to decide

Point 1 is decided. Points 2 to 7 are taken as proposed here for the building, on the assistant's own judgement
(2026-11-17), since each is a default that can be changed in one place and the mode stays behind a switch that is
off: they are the maintainer's to change by ear once the mode can be used, and each is asked then, one at a time.

1. Whether this is the seam: one model with two sources, the screen's machinery left where it is. Decided
   2026-11-17: yes, and the first stage built at once.
2. The trust question's words, and whether a yes is remembered for a folder or asked at every first start.
3. The permission question's answers and their words (four since 2026-11-26, with "No, and tell Claude why"); whether "do not ask again in this session" is offered
   at all.
4. What a menu item says that the stream cannot do: one short sentence, the same everywhere. (Choose the model and
   Choose the effort no longer say it since 2026-11-25: the stream takes their values, and they ask with a list of
   the window's own, SM-5.5. Whether that list should say that a model chosen holds for this session only, as
   Claude Code's own line after it says, is the maintainer's to hear.)
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

**Stage 2, the reader and the model (2026-11-17).** `ClaudeCodeStream.Read` gives what a model needs: a message
come back with its text, a slash command's print come back, a call's id and what it is about, a failed result's
first line, the list of work in the background, an end line's count of turns, a mode in the screen's words.
`StreamModel` is the second model on `ClaudeModel`: it takes each line, each message the window sends and each
question the relay passes on, and keeps the conversation with the screen's labels and markers. `StreamModelTests`
replays all eighteen recordings through both against a file beside each fixture, which holds what was read, what
the model raised, how its facts changed and the conversation as it stands; and it says each rule in a test of its
own. No window uses the model. Decided here, each open to the maintainer's ear when the window shows them:

- Of a tool's result nothing is shown when the call went well. A call that failed, or that the user refused or
  interrupted, has its line turned into `tool error:`, as the screen redraws it, with the first line of what came
  back under it; the words Claude Code and the relay write for the model alone are left out.
- A reply is kept as it was written, line by line; a line written as a heading is a heading, one level under the
  exchange markers'. Making the rest read as the screen's rows do (SM-4.5, point 6 of section 12) is stage 7.
- A turn Claude Code began by itself gets no line of its own in the conversation: it is told (`TurnSelfStarted`),
  for the window's "Claude goes on by itself".
- The words said for a compaction are "Compacting the conversation" and "Conversation compacted", with a line of
  the app's at the boundary; for `/clear`, the screen model's own line and words (point 5 of section 12).

**Stage 2, the host (2026-11-17).** `StreamHost` runs a program as one process over three pipes: its output
handed over line by line from a thread of its own, its input written from a queue by another, its error output
drained and kept in part for the user's eyes, never for a log; its end reported once with the exit code. It knows
no word of Claude Code's. Tested against Windows' own programs (`StreamHostTests`). With it stage 2 is whole: the
recordings, the reader, the model, the host. No window uses any of them.

Measured for stage 4 the same day: before the first message the stream names no mode by itself, and the request a
host sends first is answered with it (SM-4.10). The reader gives the mode and the version of that answer, the model
takes them, and the fixture `start` holds the exchange: nineteen recordings now.

**Stage 3, the relay (2026-11-17).** `AxKit.exe ask` (`AskRelay`, decided first in the program's entry point, as the
hooks' verb is): a server of one tool over its standard input and output, which Claude Code starts from the
configuration `AskRelay.ConfigText` gives and asks through `--permission-prompt-tool`. It passes each question as it
came to the window's pipe and the answer back as it came, both as the frames the hooks' pipe carries, under a
prefix and a variable of its own; the pipe's name and secret travel in the environment the window gives Claude Code
and are written to no file. It holds no logic and fails closed: a window that is not there, a pipe that breaks, a
wrong secret or a text that is no answer all become a no, with words that tell Claude the window could not be
reached. It waits for the user without a limit. Tested with both sides stood in and over one real pipe
(`AskRelayTests`), and live: three cases of the probe, which plays the window's end, against the installed Claude
Code: a yes (the file made), a no (not made, one refusal counted), and no window at all (a no after the two
seconds the relay waits for the pipe). That showed what the design rested on and no documentation says: the relay
Claude Code starts inherits Claude Code's environment, so the secret needs no file. The window's end of the pipe is
stage 4's; no window starts Claude Code this way yet.

**Stage 4, the window (2026-11-17).** A window made for the stream holds a conversation. The screen source is as it
was: its stand-in scripts pass unchanged, and nothing of its code runs differently.

- One window, one source (SM-9.2): `MainForm` makes the stream's model or the screen's by `--source` and the setting
  `claudeSource`. Of two fields beside `_model`, one is that object and the other null: `_screenModel` for the
  machinery beside the screen, `_streamModel` for the stream source. The machinery is not wired in a window the
  stream feeds (`FlowWired`): no marks, no hooks, no reader of the session file, and a core that is handed nothing,
  so that Done is said by the bell and the settle, as with `eventCore` set to `shadow`. The window's own code asks
  `Streams` where the two sources part. `SourceSeamTests` holds both fields to their files.
- The start (`MainForm.Stream.cs`): the line is `ClaudeCodeStream.StartArguments`: the stream's options, each
  message sent back, the relay's file and tool, the window's model, the user's own arguments last. No permission
  mode is named and the session is kept in Claude Code's store. Each line is read down on the reader's thread and
  handed to the model in order, the end last. The first request is written at once; its answer names the mode, and
  the window says Ready by its existing rule. In the Store's copy the stream is not started: it is not known that
  Claude Code can read the relay's file there.
- The end: stopping Claude closes its input, and the process is ended two seconds later when it has not ended
  (`StreamHost.End`); a restart or a confirmed close ends it at once. When Claude Code ends by itself, the last
  lines it wrote as errors are shown as lines of the conversation, and never logged. An end within five seconds of
  the start, and a start that fails, show a notice that offers the same folder with the screen (SM-9.3): Enter
  starts axClaude again there with `--source screen` and closes this window, Escape stays.
- What the window sends (section 5): a message is one line. Interrupt, while a turn runs, is the request that ends
  it, and the model then adds the line "System: Interrupted" and says "Interrupted", since no Done follows; with no
  turn running it says "Nothing to interrupt". The mode key asks for the next mode of a round: the three modes
  every run has, in the interactive program's order, then each other mode this run was itself in, so that the key
  comes back to a mode the user's own settings put the run in and never reaches one the run was not in (SM-5.4).
  A bare Enter says "Nothing to send". What only a screen takes says "Not with the stream source": Send queued
  messages now, the Send key items, Ctrl+O, Choose the model and Choose the effort (their pickers are screens,
  SM-5.5; since 2026-11-25 these two have a list of the window's own). A command an AI offers is refused at the door with words for the AI, so that nothing is offered that
  could not be sent (SM-5.6).
- The two switches of the screen's event sources stay in the menu and say, in a window the stream feeds, that the
  setting is for windows the screen feeds (SM-9.4 has what Say the event sources says).
- axHelp's conversation names `--source screen` (SM-9.5).
- The stand-in of the stream (SM-10.2): `AxAutomate standin --stream <case>` plays a recorded fixture of
  `tests/fixtures/streams` line by line and is moved on by what the window writes: it waits where the recording
  has a message or a request of the host's, puts the window's own text and request names where the recording has
  the probe's, and answers a request the recording does not have as Claude Code does. Where the recording's Claude
  Code asked its permission tool, the stand-in starts the relay from the file the window named and asks it, as
  Claude Code does. Seven window scripts use it (`tests/scripts/axClaude/37` to `43`).
- Left open by this stage, each on the priorities list: `--ai-tools` gives Claude Code nothing over the stream yet;
  how long Claude Code waits for its permission tool is not measured; a version of Claude Code that does not answer
  the first request is ready only after its first turn. The first and the last are built since 2026-11-25 (the last
  part of this section). The wait was measured the same day with 2.1.332: Claude Code waited twenty minutes for a
  permission tool that held its answer and then took the late answer, so the relay's wait without a limit meets no
  limit of Claude Code's in a person's time.

**Stage 5, the folder's trust and the questions (2026-11-17).**

- The folder's trust (SM-7.1): a notice before Claude Code's stream is first started in a folder in a window:
  "Start Claude Code in this folder?", with why the window asks and the folder. Enter and Escape are no ("Do not
  start"); the yes is its own button. A no leaves a line that says so, and the next start asks again. As built, a
  yes holds for the folder while the window is open and is not written anywhere: remembering it under the
  settings' seal (point 2 of section 12) is left for the maintainer's word, since it trades a key press for a
  stored trust.
- The pipe (SM-7.3): `AskChannel`, the window's end, new for each start of Claude Code, open to this Windows account
  alone, its name and secret given to Claude Code's environment and written nowhere. One connection is one
  question; its thread waits for the answer as long as the user takes. Stopping Claude stops the pipe, and a
  question that waited gets the relay's no. Nothing of a question or an answer is logged.
- A tool call that needs the user's word (SM-7.4, SM-7.6) is asked in the control area by the window's existing code,
  as a question of Claude's is. Its title says who asks and for what, "Claude asks to use Bash: ping -n 2
  127.0.0.1", and its text is the caution, heard every time the question is read: "Asked by Claude, an AI: read the
  explanation before you allow it". Four answers, the list starting on the first: "No"; "Yes, once"; "Yes, and do
  not ask again for Bash in this session", a rule of the session's alone; and "Explain what Claude asks to do",
  which answers nothing: it opens a text, the caution first (`AiCaution.ToolCall`), then the tool and every member
  of the call's input, and the question stays under it. A file in the project folder is named from there. Since
  2026-11-26 each answer ends in the call's core, "1. No, Bash ping -n 2, and more" (SM-7.4; the mark that words
  were left out since 2026-11-27), and "No, and tell Claude why" is the
second answer, so that the list has five (SM-7.4).
- Claude's own questions (SM-7.5) are asked one after another, "Question 1 of 2: …", each with its choices by number
  and Other last; a choice's description follows its name after a dash when it says more than the name. Other asks
  for the user's own words in the window's own field ("You chose Other. Your answer"), and Escape there goes back
  to the question's list. The answers go back together after the last. Escape on a list is no to the whole.
- The interrupt on a waiting question is no to it and says "Cancelled"; the turn goes on, and an interrupt after
  that ends the turn (SM-5.3).
- What was answered is not yet written into the conversation as Claude Code's screen writes its record of an
  answered question; the question's line ("Question: …", found with q) is. Built since 2026-11-25 (SM-7.5).

**What the review of stages 4 and 5 found, repaired the same day (2026-11-17).** Four readers went through the
branch (threads and lifetime, safety, the screen source, what is heard), and the most severe findings were checked
by a second reader each. Nothing was found that changes a window the screen feeds. Repaired:

- Every request for the user's word is asked anew (SM-7.2, SM-7.4). Two calls of one tool about one file read as
  one question, and the second came some milliseconds after the first answer: the window took it for the question
  it had just answered, said nothing, and left the first list on the answer just given, where Enter would have
  allowed a call that was never said. Now an answer that goes back marks what the control area shows as over, and
  the next question is shown and said afresh, on No (`44-stream-same-question.txt`).
- A question is always answered from the control area over the stream, whatever Options, Questions, Answer from a
  list says and whatever slash command was sent last: over the stream a question has no rows in the conversation,
  and with the list gone it had no answers and no caution anywhere.
- A tool call's question says when its title holds only the start of the command: ", and more, which the
  explanation shows". A yes runs all of it.
- A turn that failed says Responding and Done as any turn does (SM-4.6): it said nothing.
- A slash command that runs the model (a skill, a command of the user's) ends with its turn: its end line counts
  turns, where a command Claude Code runs by itself counts none, and the model waited for an end that never came,
  with Claude working for good. Not recorded yet: how such a command's own line comes back, and whether its replies
  are told from a command's print, is on the priorities list. Recorded and built since 2026-11-25 (SM-5.5).
- A line of the stream with a value of another kind than the reader takes it for is passed over and never thrown
  on: the window reads on a thread where an exception would have ended it with the conversation.
- A Claude Code over its stream is ended at once when the window closes for good, and the tree's failures to end
  are caught: the wait that would end it later dies with the window's process.
- Record raw stream is not available in a window the stream feeds, and `--record` says so: it said "Recording
  started" and wrote nothing (SM-6.5 is still to build). Built since 2026-11-25 (SM-6.5): it records the stream's lines.

**The audit of 2026-11-21.** A second reader went through the stream source as released in 5.10.0, for what the
first review had missed. Repaired the same day, each with its test:

- *The relay trusted a pipe by its name.* `AxKit.exe ask` connected to the name in its environment and wrote the
  secret to whoever served it; and the window, after twenty failures of its pipe in a row, gave it up with one line
  in the log, the name free from then on. A program of the same account could have taken the name, been handed the
  secret, and answered yes to every question, unheard. The relay now asks Windows which program serves the pipe and
  takes only this same program file (`AxKit.Core/ThisProgram.Serves`; the hooks' relay does the same), connects so
  that the server cannot act as it, and a window that loses its pipe stops Claude Code at once, says why, and offers
  the way back (`StreamQuestionsLost`).
- *A title that named nothing.* A command that began with a line break was asked about with no command in the
  title and no word of more. The title names the first line that holds something. A tool whose input names nothing
  known as the thing acted on (a server's tool with `sql`) was titled with Claude's own description as if it were
  the action: it is now said as Claude's ("and describes it as"), with the words of more; and the words of more are
  also said when the input holds another member to read. Claude Code's own file tools are named by their file and
  their name, and say nothing of more, so that those words keep their weight.
- *Control characters.* The stream brings what the screen's parser never let through. A character of code 0 ends a
  text box's text: the explanation of a call stopped there, while a yes ran all of it, and a failed command that
  printed binary output cut the conversation's view. In what Claude asks, every hidden character is written out as
  its code (`DisplayText.Visible`, a tab kept); in the conversation's lines they are dropped where a line is made.
- *A digit typed too early.* A "2" sent from the message field in the moment before a tool call's question had
  settled into its list allowed the call with nothing of it heard. Over the stream, text from the message field
  answers nothing until the question is shown; it stays in the field and the window says so.
- *The program outlived its window.* Nothing tied Claude Code to the window's life: when axKit crashed or was
  ended, calls already allowed went on with no window; commands left running in the background were never ended;
  and one that held the output's pipe kept the end from being reported at all. Claude Code now runs in a Windows
  job (`AxKit.Core/ChildJob`) that ends with this process, `Kill` ends the job, and what is left two seconds after
  Claude Code itself has ended is ended with it, as a console's programs end with their console.
- *A request with no answer.* A call nested deeper than 64 levels was not read by the relay and got no answer; it
  reads to 1,024 now. A call over 1 MB is refused in words that say so.

The three smaller points, settled the same day:

- *"Yes, and do not ask again" left no trace.* It covers the whole tool for the session, as its answer says. The
  answer now leaves a line in the conversation: "System: You allowed Claude to use Bash without asking for the rest
  of this session. A new start of Claude Code asks again." (`StreamAsking.StandingWords`), so that what was given can
  be found again with the conversation's keys.
- *The model can call the relay's tool itself* and so make the window ask a question it wrote, in the form of a real
  one. Left as it is, on purpose: the answer goes back to the model and to nothing else, so it allows nothing; Claude
  Code asks the user before the model may call that tool at all; and the relay cannot tell one caller from another.
- *Claude's own questions carry no word that an AI asks.* Left as it is: a question in a conversation with Claude is
  Claude's by its place, with the screen and with the stream alike, and answering it runs nothing. The caution is
  for what an answer lets happen (root rule 11), and a tool call's question has it every time.

**Measured with Claude Code 2.1.330 itself, the same day** (`tests/scripts/axClaude/48-stream-live.txt`, the first
script that runs the stream with Claude Code and not the stand-in). Claude Code works unchanged inside the job: it
starts, starts the relay, runs a command and ends. Its question for a command reaches the window through the relay
that asks which program serves the pipe, is answered, and is obeyed. A folder whose own `.mcp.json` names a server
`axask` does not get that server started in the relay's place: the question still came to the window, and the
folder's program was never run (a folder whose servers the user approved in Claude Code before was not measured). And
one thing that is no fault and is worth knowing: Claude Code over its stream follows the user's own settings as with
the screen, so a user whose Claude Code runs in auto mode is not asked about a command that mode allows by itself;
the script names the default mode for its run.

**The open ends after stage 5, built (2026-11-25).** Each from a recording first where Claude Code's side was not
known (two new fixtures, `custom-command` and `choices`, Claude Code 2.1.332; the probe's stream cases can now write
files into their folder first, a custom command among them):

- *A skill or a custom command* (SM-5.5): its own line comes back as a replay of Claude Code's tags
  (`<command-message>`, `<command-name>`, and `<command-args>` when it has any), just before its turn. Read as it
  came, it was a message never sent, and a block of raw tags went into the conversation. The reader makes it a
  `StreamKind.Command` with the command as typed; the model shows the echo the screen shows (`you: /name`, already
  there for the window's own command) and takes the turn's texts as Claude's replies, labelled, spoken and found
  with c, where they would have been a command's print.
- *The answer to Claude's own questions* is on record as the screen's (SM-7.5).
- *axKit's AI tools* over the stream: their file beside the relay's under the one `--mcp-config` (SPEC.md FR-9.7), in
  every window since 2026-11-26 (with `--ai-tools` until then). Not measured live with the tools on.
- *Choose the model and Choose the effort* are lists of the window's own (SM-5.5).
- *A Claude Code that does not answer the first request* is ready five seconds after its start (SM-4.10). No version
  that does not answer it was at hand, so the rule is held by a test of the model alone.
- *The raw recording* records the stream's lines (SM-6.5).
- Left as it was, by the maintainer's choice: the folder's yes is remembered for no folder (point 2 of section 12).

**Stage 6, sessions (2026-11-25).** Measured first, live, with Claude Code 2.1.332 and Haiku, in one folder under the
temporary folder, one session at a time, by a scratch program that printed kinds, counts and yes or no only (the
probe's case `resume` had already shown that the stream takes up a session of its own by its id and writes none of
it again). In order:

1. A new session over the stream: its first request's answer names no session; the start line of the first turn
   does. One session file, with an `enqueue`, a `dequeue`, a prompt without `promptSource` and the reply: no entry
   for the turn's end (the hand-written `tests/fixtures/records/stream-session.jsonl` has this shape).
2. The screen with `--continue` in that folder: Claude Code asked for the folder's trust, then said there was no
   conversation to continue and ended. The stream's session is not the interactive program's to continue.
3. The screen with `--resume` and the stream's id: no trust question (given in step 2), and the old conversation was
   drawn (a `you:` row and a `claude:` row before any message); asked for the word, Claude knew it. Still one file:
   the screen wrote on in the stream's, now with a typed prompt and a turn's end entry.
4. The screen with `--continue` again: nothing to continue, as in step 2, though the screen itself had used that
   session. So the stream's session stays out of the interactive `--continue` for good.
5. A new session with the screen: a second file.
6. The stream with `--resume` and the screen's id: the first request's answer names no session, nothing of the old
   conversation came as a line before the message, the start line named the screen's session, Claude knew what the
   screen had been told, and the screen's file grew; still two files.
7. A second new session over the stream, then the stream with `--continue`: it took the newest, the stream's own.
8. The screen with `--continue` after that: it took the screen's session of step 5 and drew it, the newest it began
   itself, though the stream had written to it since.

Built on that:

- `AxClaude.Core/Streaming/SessionHandover`, pure but for two looks at files: `IsSessionId` (only a UUID in its usual
  form goes on a command line, into the settings or into a file's name), `ProjectFolders`, `Newest` (the session
  file written last, directly in the folder's project folders, by a name that is a session's id), `FileOf`,
  `ResumedIn`, `ForStream` (each `--continue` becomes `--resume` with the newest session, or goes when there is none),
  `ForScreen` (`--continue` becomes `--resume` with the kept session while that is the newest), `Read` (the whole
  file through `RecordTail` and `ClaudeCodeTranscript.Parse`, null when it cannot be read). `ClaudeCodeStream` gained
  `ResumeShortOption`.
- `StreamModel.ShowOlder` makes the lines of SM-8.2 from the entries and `HoldsConversation` says whether the view
  holds any. `AppSettings.StreamSessions` (`KeptSession`, the last 30 folders, sanitized as the file is read) with
  `StreamSessionOf` and `KeepStreamSession`, which keeps over the list as the file has it now, since the file's
  members are merged whole (R-62) and two windows of two folders would otherwise lose each other's.
- The window (`MainForm.Stream.cs`): the start names the session before anything else of the start; when a session
  file is to be shown, it is read on a thread of the pool and the start goes on in the window's thread when it is
  read (a stop, a restart or a close meanwhile ends that start), and the older lines are added right after the run's
  start, before any line of it can come and before Ready, from which on the window speaks. Each start line whose
  session is new is kept for the folder (`KeepStreamSession`), which also follows `/clear` (SM-8.4). The screen's
  start (`MainForm.Process.cs`, `ScreenClaudeArgs`) passes `--resume` with the kept session where `ForScreen` says so;
  a restart hides the redrawn conversation as it does for `--continue`. The log has numbers and fixed words only.
- Tests: `SessionHandoverTests` (16), from the hand-written session files of `tests/fixtures/records`; the stand-in
  script `51-stream-older.txt`, for which the stand-in of the stream writes a session file of
  `tests/fixtures/records` where Claude Code keeps it (`standin --stream <case> --session <record>`, only into a
  configuration folder inside the run's folder, which the script's `claude.cmd` names), and a second window in the
  same folder shows it; the live script `52-stream-live-handover.txt`: a conversation over the stream, the screen
  taking it up, and the stream again, which shows the earlier part and goes on with it.

Left, each for a reason:

- A tool call of the older conversation is shown by its tool's name alone, where the screen's redrawn conversation
  names what it was about: the session file's reader never reads a call's input (EG-3.4), and that rule stays.
- What a window the screen feeds kept itself is not kept: its session is the folder's newest once it has written to
  it, which is what the stream's window takes up, and its own `--continue` finds it.
- A session the stream began is taken up by the screen only while it is the folder's newest: once a later session
  has been written in the folder, the screen's `--continue` takes that one, as it always did, and the interactive
  program's own list of sessions leaves the stream's out (documented). The window does not guess past the newest.
- The NVDA plan's section for stream mode and the maintainer's own use (stage 8). The section is written since
  2026-11-26.

**Round 1 of the must-build list (2026-11-26).** The maintainer decided that day to build all nine things a report of
that day listed as needed before the stream could be the default (what the stream loses against the screen, deleted
once built; last at commit fc3109e6, `docs/axClaude/stream-equivalence.md`); this round is the small ones, the
Store's copy (SM-9.6), a typed command (SM-5.8), the sign-in (SM-6.2), a `!` line (SM-5.9) and the No with words
(SM-7.4), with the NVDA plan's section. Measured first, live, with Claude Code 2.1.332 and Haiku, by three new cases of `tools/ClaudeProbe stream`,
each in a fresh folder under `C:\Temp`, one session at a time, printing kinds, counts and Claude Code's own short
words through the redaction only:

1. `command-lists`: the answer to the first request lists 69 commands, each an object with its name, and the start
   line's `slash_commands` the same 69. Of the commands of Claude Code's own screens and of the terminal alone (SM-6.1, SM-6.2), the lists hold
   `config`, `usage`, `goal`, `mcp`, `agents` and `advisor`; not `status`, `cost`, `help`, `resume`, `permissions`,
   `tasks`, `rewind`, `btw`, `diff`, `memory`, `hooks`, `ide`, `chrome`, `theme`, `export`, `mobile`, `login` or
   `logout`. The start line's `terminal_slash_commands` names four, every one also among its commands: it is not the
   list of the screen's commands, and nothing reads it.
2. `screen-commands`: ten of those not listed, written over the stream one after another (never `/status`, `/cost`,
   `/login`, `/logout`, and not `/memory`, `/ide`, `/chrome` or `/export`, which might reach an editor, a browser or the
   clipboard if run): each is answered by Claude Code itself with one text, "/help isn't available in this
   environment." (the command's name in each), and an end line that counts no turn; nothing comes back as a message
   and the model does not run. A name that is no command (`/probe-no-such-command`) comes back as a message and the
   model answers it, one turn.
3. `relay-refused-why`: a Write call refused through axKit's relay with the words "Do not write any file. Reply with
   the single word heron and nothing else." The file was not made, one denial was counted, and the reply was the
   word: Claude reads the words that come with a refusal.

Built on that, each with its tests: SM-5.8 (a typed command, `StreamModel.TypedTextOf` and `Commands`, the first
answer's commands read by `ClaudeCodeStream`), SM-5.9 (a `!` line), SM-6.2 (the sign-in, `StreamModel.SignInAsked`),
SM-7.4 ("No, and tell Claude why", `StreamAsking.RefuseWhyKey`, the keys of Yes once, Yes always and Explain moving to
3, 4 and 5), SM-9.6 (the Store's copy), and the kind `not-with-source` of `docs/axKit/families.json`. The stand-in of
the stream answers the first request with some of the commands 2.1.332 listed, `/help` not among them; the stand-in
scripts `55-stream-refused-why` and `56-stream-typed-commands` play the new answer and the typed texts. What is owed:
the NVDA pass of the section, the error of a lapsed sign-in over the stream (not recorded), and the Store's copy (no
script can run it).

**Round 2 of the must-build list (2026-11-26).** The three medium items: what a tool printed (SM-4.11), the choice of an
earlier conversation (SM-8.5) and plan approval (SM-7.7). Measured first, live, with Claude Code 2.1.332 and Haiku, one session at a time, in fresh folders under
`C:\Temp`, printing kinds, counts, keys and yes or no only:

1. The screen's preview of a tool's output, recorded with `tools/PtyCapture` (screen reader mode, 240 by 50, not
   committed) and read through its redacting dump: `seq 1 12` drew `tool: Bash (seq 1 12)`, the rows `1`, `2`, `3`,
   `… +9 lines (ctrl+o to expand)` and `Allowed by auto mode`; `true` drew `(No output)`.
2. The same two commands over the stream (`tools/ClaudeProbe stream`, the case `tool-output`, and the recording of the
   same name): each result is a line of the user's with the whole output as its content, 12 lines for the first; the
   second gives "(Bash completed with no output)". The permission tool was not asked (the user's own rules allowed
   both). The fixture keeps the results' text, the one case that does (`KeepsOutput`: commands whose output the case
   chose).
3. Plan approval, by four reported cases and the recording `plan`; each sets the mode to plan with the mode request,
   then asks for a plan of two steps for one file, shown with the plan tool, then carried out:
   - In plan mode Claude first writes its plan to a file under Claude Code's own folder (`.claude\plans`), unasked, then
     calls the plan tool. Its input holds `plan` (the plan in Markdown) and `planFilePath`; the permission tool is asked
     with the tool's name, that input and the call's id, as for any call.
   - `plan-manual`: the yes with the mode `default` given in the answer (`updatedPermissions` of the kind `setMode`):
     a status note names `default` at once, and the Write that follows is asked about. The file is made.
   - `plan-edits`: the same with `acceptEdits`: the status note names it, and the Write is not asked about. So the
     answer's mode is the mode the work goes on in.
   - `plan-auto`: the same with `auto`: the status note names `auto`, and nothing more is asked.
   - `plan-keep`: the plan sent back, refused with the user's words ("Name the file plan-kept.txt instead, and present
     the plan again"): the mode stayed plan, Claude called the plan tool again with a plan that named `plan-kept.txt`,
     which was approved with `default`; `plan-kept.txt` was made and `plan-probe.txt` not, and the turn's end counted
     one denial.

The plan recording was first refused by the fixtures' maker: the plan file's name holds the session's id. The maker now
turns an id quoted inside a kept text into a number, as it does with the fields' ids (`session-1`), and its check is
unchanged. Built on that, each with its tests: SM-4.11 (`ToolOutput`, `StreamPart.Output`, and the model's keeping of every call's whole output, taken out again on 2026-11-27),
SM-8.5 (`ConversationChoice`, `SessionHandover.AsksToChoose`, `WithChosen` and `All`), SM-7.7 (`StreamAsking.IsPlan`
and its four answers, `ClaudeCodeStream.PlanTool`, `AllowWithMode`, `KeepPlanning`, `AiCaution.PlanAsked` and
`Plan`). Nothing new is heard but through the kinds that exist: the plan and the list of conversations are questions
of the control area, and the output is never spoken. The stand-in of the stream writes several session files with
`--session a,b` (the last the newest); the stand-in scripts `57-stream-tool-output`, `58-stream-resume-choice` and
`59-stream-plan` play the three. What is owed: the NVDA pass of steps 14 to 16 of the plan's section 26, a plan
approved with auto mode in a real conversation (the stand-in plays the manual approval only), and the choice of an
earlier conversation in a folder with many long sessions.
