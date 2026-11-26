# axClaude: what the stream mode loses, and what would replace it

A report for the maintainer's decision, written 2026-11-26. It answers his question of that day: "do we really know
how much we lose by going with stream mode and what we would need to replace to offer equivalence?" It decides
nothing and changes no behaviour.

## 1. The two modes, and why the stream was built

- **The screen mode.** Claude Code runs in a pseudo console in its screen reader mode. axClaude reads the rows it
  draws. This is the default, and what axClaude has always done.
- **The stream mode.** Claude Code runs as one process with no screen. It writes one JSON line for each thing that
  happens. axClaude reads those lines (`stream-mode.md`). It is behind a switch in Options, Developer.
- **Why the stream was built** (`stream.md`):
  - Every Claude Code release can move a row of the screen, and the screen's parser is mended each time. 2.1.326
    moved the queued message's hint. 2.1.327 added a key to the permissions screen, and that screen went silent.
  - The faults measured in the maintainer's own use, 59 turns on 2026-11-16: "Done" before the reply had been spoken
    in 23 turns. The state went idle and back 167 times. The core beside the screen dropped the short replies between
    tool calls.
  - In the stream none of the three exists: a turn's end is one line that comes last, and each reply is its own line.
  - A fallback for the day Claude Code changes its screen too far.
- **What this page does not decide.**
  - Whether the stream becomes the default. That is the maintainer's, after his two hours of use (SM-10.5).
  - The words and defaults of `stream-mode.md` section 12, the folder's trust among them.
  - Which of the losses below are built. Each recommendation is a proposal.

The cost words used below:

- **Small**: one commit with its tests, no new recording.
- **Medium**: a measurement or a recording first, then a few commits.
- **Large**: a new part of the window or of the stream's model.

## 2. The inventory

Read from `stream-mode.md` sections 5, 6, 12 and 13, from `SPEC.md` FR-1 to FR-16 with the menus of FR-10, and from
the code: `src/AxClaude/MainForm.Stream.cs` and every place the window asks `Streams`.

### 2.1 Replaced already

Eighteen things the screen gave that the stream does not give by itself, each with its replacement built.

- R1. The model and effort pickers: lists of the window's own, from Claude Code's first answer (SM-5.5).
- R2. The count of waiting messages: the window counts what it wrote and has not come back (SM-6.4).
- R3. The raw recording: the stream's lines, as a `.jsonl` file (SM-6.5).
- R4. Carrying on the folder's last conversation (SM-8.1).
- R5. The older part of that conversation, shown from the session file (SM-8.2).
- R6. Handing a conversation between the two modes by its id (SM-8.3).
- R7. The next permission mode, Ctrl+P (SM-5.4).
- R8. The interrupt, Ctrl+I (SM-5.3).
- R9. A tool call's permission question, with the AI caution and Explain (SM-7.4, SM-7.6).
- R10. Claude's own questions, with Other and the record of the answers (SM-7.5).
- R11. Ready, from the answer to the first request (SM-4.10).
- R12. Compaction and `/clear`, said in the screen's words (SM-4.9, SM-8.4).
- R13. Background shells and agents: the count, the sounds and F8 (SM-4.8).
- R14. "Claude goes on by itself" for a turn Claude Code began (FR-7.3b).
- R15. A reply's text made plain, with its headings (SM-4.5).
- R16. A failed tool call, as a `tool error:` line (section 13, stage 2).
- R17. axKit's AI tools with `--ai-tools` (FR-9.7).
- R18. Say the event sources and Copy diagnostics, naming the source (SM-9.4).

### 2.2 Still missing, in whole or in part

Twenty-nine. Four of them are replaced in part: L1, L2, L6 and L15.

Claude Code's own screens:

- L1. `/config`, the settings screen.
- L2. `/status`, `/usage` and `/cost`.
- L3. `/help`, and a list of the commands.
- L4. A picker of the folder's earlier conversations.
- L5. `/permissions`, the permission rules.
- L6. `/tasks`: the list of background work, and stopping one.
- L7. `/rewind`: going back on Claude's file edits.
- L8. `/btw` and `/goal`, with the b key.
- L9. The other screens: `/diff`, `/memory`, `/mcp`, `/hooks`, `/agents`, `/ide`, `/chrome`, `/theme`, `/advisor`,
  `/export`, `/mobile`.
- L10. The feedback surveys, cards and drafts.
- L11. `/login`, and the other commands of the terminal alone.
- L12. A typed slash command that only the screen has.

What Claude Code asks:

- L13. Plan approval.
- L14. The fuller answers to a permission question.
- L15. The folder's trust, remembered.

What the window sends:

- L16. A command Claude offers.
- L17. A `!` line the user types.
- L18. Send queued messages now.
- L19. The Send key submenu, and a bare Enter.
- L20. Ctrl+O and the detailed view, the console's size, screen reader mode, the live progress rows.

What the conversation shows:

- L21. What a tool printed.
- L22. Claude's thinking lines.
- L23. The turn's summary line, and the d key.
- L24. Speech that starts before a long reply is whole.
- L25. What a tool call of the older conversation was about.

The status bar and background work:

- L26. The spinner's word and the session's name.
- L27. Dynamic workflows.

Where the stream cannot run:

- L28. The copy of axKit from the Microsoft Store.
- L29. axHelp's conversation.

### 2.3 What the stream gives that the screen does not

- "Done" after the reply, every time: the turn's end is one line that comes last.
- A state that never flickers between working and ready.
- Every short reply between tool calls, each as its own line.
- No parser of rows to mend when Claude Code redraws its screen.
- Headings taken from the reply's source, where they are certain.
- The exact tool names, and a tool's failure marked as one.
- Background work counted from Claude Code's own list, not read off a redrawn panel.
- No feedback surveys, tips or session recaps between the lines.
- A start in under a second, with no trust prompt to time.
- None of the screen's workarounds: no Enter sent again, no restored message to clear, no Ctrl+O hazard.
- The whole output of every tool is in the stream; the screen shows a preview. Nothing uses it yet (L21).
- The list of every command the run takes, with descriptions. Nothing uses it yet (L3, L12).

## 3. Each loss, and what would replace it

### L1. `/config`, the settings screen

- Today: Options, Claude Code settings has seven of its settings (FR-14). The model and effort have lists (R1).
- Would: more rows of the FR-14 table, one at a time.
- Cost: small for each.
- Recommendation: leave to the screen mode. Add a row when the maintainer misses one.

### L2. `/status`, `/usage` and `/cost`

- Today: `/status` and `/context` print as text over the stream (SM-6.1; `/context` measured).
- Would: measure what `/usage` and `/cost` give over the stream.
- Cost: small, inside the measurement of L12.
- Recommendation: leave to the screen mode, after that measurement.

### L3. `/help`, and a list of the commands

- Today: Help, Claude Code resources opens the documentation's pages.
- Would: a list of the window's own of the commands the first answer names, with their descriptions, as Choose the
  model is made. Enter puts the command into the message field.
- Cost: small.
- Recommendation: build, later. It would be better than the screen's tabbed `/help`.

### L4. A picker of the folder's earlier conversations

- Checked in the code: there is no picker. The stream's window only carries on the folder's newest session
  (`SessionHandover.ForStream`). Another session is taken up only by its id, typed after `--resume` in New session's
  Custom field.
- New session's preset "Choose a conversation to resume" sends a bare `--resume` to the stream unchanged. That is
  not measured, and Claude Code's own picker is a screen. At best the start fails and the window offers the screen
  (SM-9.3).
- Would: a list of the window's own of the folder's session files, newest first, each named by its date and its
  first message, read off the window's thread. The choice starts the stream with `--resume` and the id, and the
  older part is shown as today (R5).
- Cost: medium. The files are read already; a name from the first message is new.
- Recommendation: build before the default. Until then, the preset says "Not with the stream source" (small).

### L5. `/permissions`, the permission rules

- Today: nothing.
- Recommendation: leave to the screen mode. Rules are changed rarely.

### L6. `/tasks`: the list of background work, and stopping one

- Today: F8 says what runs, from the stream's own list (R13). Stopping one task: nothing.
- Would: a request that stops one task. Not looked for in the documentation of the command line; to be measured.
- Cost: medium.
- Recommendation: leave to the screen mode. The interrupt still ends a turn.

### L7. `/rewind`: going back on Claude's file edits

- Today: nothing.
- Would: not looked for in the stream's documentation; to be measured first.
- Cost: medium to large.
- Recommendation: leave to the screen mode. Git, through axGIT, also takes a change back.

### L8. `/btw` and `/goal`, with the b key

- Today: nothing. The b key finds nothing.
- Recommendation: let go. A side question can be asked as a message.

### L9. The other screens

- `/diff`, `/memory`, `/mcp`, `/hooks`, `/agents`, `/ide`, `/chrome`, `/theme`, `/advisor`, `/export`, `/mobile`.
- Today: nothing. The window's own Export conversation stands in for `/export`.
- Recommendation: leave to the screen mode. Each is used seldom.

### L10. The feedback surveys, cards and drafts

- Today: none is shown. The FR-14 switches still write Claude Code's file.
- Recommendation: let go. Fewer interruptions is a gain.

### L11. `/login`, and the other commands of the terminal alone

- Today: signing in is done in the screen mode (SM-6.2).
- When the sign-in lapses, Claude's error says to run `/login`, and nothing offers the screen.
- The window's sign-in reminder after `/login` is also said over the stream (`MainForm.Input.cs`), where it is not
  true.
- Would: an error that names `/login` offers the folder with the screen, by the notice that exists (`OfferScreen`).
  The reminder is not said over the stream.
- Cost: small.
- Recommendation: build before the default.

### L12. A typed slash command that only the screen has

- SM-6 says the window says so where the user looks. For a typed command it does not.
- Today: the command goes to Claude Code as a message. What Claude Code answers for a screen's command is not
  measured. Its documentation says only that the terminal's own commands "aren't available".
- Would: the command is checked against the list the first answer names. One not on it is not sent, and the window
  says "Not with the stream source" and the command. Measured first for each command of L1 to L9.
- Cost: small.
- Recommendation: build before the default. It is the one place where the window can mislead without a word.

### L13. Plan approval

- The screen: "Ready to code?" with the plan, then Yes with auto mode, Yes with manual approval, and No, keep
  planning, which asks for words.
- The stream: not measured. By the relay's rule it would come as "Claude asks to use" the plan tool, with No, Yes
  once and Yes always, and the plan only under Explain.
- Ctrl+P reaches plan mode over the stream, so this question will come.
- Would: the plan tool known by name in `StreamAsking`. The plan is the question's text, the three answers are the
  screen's, No carries the user's words, and a yes sends the chosen mode.
- Cost: medium.
- Recommendation: measure, then build before the default.

### L14. The fuller answers to a permission question

- The screen offers a yes that Claude Code keeps for the project, and a No that asks "What should Claude do
  instead?".
- The stream: Yes always holds for the session alone. No carries no words.
- Would, first: a fourth answer, "No, and tell Claude why", with a field. The words go back with the refusal. Not
  measured whether Claude reads them.
- Would, second: a lasting yes, the same answer written for the project. Only the session's form is measured.
- Cost: small for each.
- Recommendation: build the No with words before the default. The lasting yes is the maintainer's decision
  (`stream-mode.md` section 12, point 3).

### L15. The folder's trust, remembered

- The screen: Claude Code asks once for a folder, and remembers.
- The stream: the window asks in every new window (SM-7.1). The yes is kept nowhere, by the maintainer's choice so
  far.
- Would: the yes kept under the settings' seal, as axGIT keeps a trusted repository.
- Cost: small.
- Recommendation: the maintainer's decision, after he has heard the question in his two hours.

### L16. A command Claude offers

- Today: refused at the door (SM-5.6). The AI tool is told to give the user the command.
- The code's comment in `MainForm.Mcp.cs` still says "not measured"; SM-5.6 has it measured on 2026-11-25.
- Would: the offer still asked, with Copy and Open axTerminal in the folder in place of Yes, run it.
- Cost: small to medium.
- Recommendation: build, later. axHelp's conversation, where offers matter most, stays on the screen (L29).

### L17. A `!` line the user types

- The screen: Claude Code runs it in its shell at once, with no question. What it prints joins the conversation and
  Claude's context.
- The stream: it goes to Claude as a message. Claude may then run it with its own shell tool, after asking (measured,
  SM-5.6).
- Today: nothing says that the line means something else.
- Would: a typed `!` line is held, as FR-2.10 holds a message during a question. The window says that it would go to
  Claude as a message, and Enter again sends it so.
- Cost: small.
- Recommendation: build before the default.

### L18. Send queued messages now

- The stream takes a message sent mid-turn after the running tool's result, by itself (measured).
- Recommendation: let go.

### L19. The Send key submenu, and a bare Enter

- Ctrl+C and Ctrl+D have their meanings in Interrupt and in stopping Claude. The arrows, Tab and a bare Enter move
  through screens the stream does not have.
- Recommendation: let go.

### L20. Ctrl+O, the detailed view, and the console's own settings

- The console's size, screen reader mode and the live progress rows have no meaning without a screen (SM-6.6).
- Recommendation: let go.

### L21. What a tool printed

- The screen: Claude Code's preview rows under each tool line.
- The stream: nothing of a call that went well. The first line of one that failed (stage 2's decision).
- Would: the first lines of each result under its call, as tool output, never spoken. The stream holds the whole
  output, so a later key could read all of it, which the screen cannot.
- Cost: medium.
- Recommendation: build before the default. A developer reads what a test run printed.

### L22. Claude's thinking lines

- The screen: thinking lines of their own kind.
- The stream: the thinking's text is not read. It only starts Responding.
- Would: a thinking line when Claude Code gives text.
- Cost: small.
- Recommendation: ask in the two hours. Let go if they are not missed.

### L23. The turn's summary line, and the d key

- The screen: "for 5s, done 2:48 PM" after each turn, unless FR-14's switch hides it.
- The stream: none. The end line holds the turn's duration.
- Would: a line of the app's from that duration and the clock, under the same switch.
- Cost: small.
- Recommendation: build. It keeps a quick key working.

### L24. Speech that starts before a long reply is whole

- The window does not ask for a reply's pieces, so each reply line comes whole.
- Measured: a reply of about 300 words is written at about 500 characters a second, so it is whole some 3.5 seconds
  after its first piece (`stream.md`, long-reply). The screen speaks its rows as they settle.
- Would: the pieces (`--include-partial-messages`), and each line spoken once it is complete.
- Cost: medium.
- Recommendation: listen in the two hours. Build only if the wait is heard.

### L25. What a tool call of the older conversation was about

- The older part names each call by its tool alone. The session file's reader never reads a call's input (EG-3.4).
- Recommendation: let go. The rule protects what the file holds.

### L26. The spinner's word and the session's name

- The status bar says working, ready or waiting, and the mode. No spinner word, no session name (SM-6.3).
- Recommendation: let go.

### L27. Dynamic workflows

- The screen: each workflow's name, percent and steps.
- The stream's model counts none (`StreamModel`, its comment on workflows).
- Would: workflows read from Claude Code's list of background tasks, once a recording shows how they come.
- Cost: small, after a recording.
- Recommendation: build when the maintainer uses workflows over the stream.

### L28. The copy from the Microsoft Store

- Today: the stream does not start at all in the Store's copy. It is not known that Claude Code can read the relay's
  file under the package's folders (`MainForm.Stream.cs`).
- Would, first: the default stays the screen in the Store's copy.
- Would, second: a measurement, as for the hooks' file (EG-2.11), and the file moved where Claude Code can read it.
- Cost: small for the first, medium for the second.
- Recommendation: build the first before the default.

### L29. axHelp's conversation

- It stays on the screen until the stream has been used for a while (SM-9.5).
- So the screen and its parser stay maintained, whatever the default.
- Recommendation: move it after the stream has held as the default for a while.

## 4. The risks that cannot be measured ahead

- **New features reach the interactive screen first.** Most of L1 to L10 are screens of that kind: `/btw`, `/goal`,
  `/rewind`, the feedback drafts, the dynamic workflows' row, the advisor. The documentation says the commands of the
  terminal alone are not available in print mode.
- **No promise for the lines' shape.** The documentation has no stability statement for the stream.
- **The start line's `capabilities`** are there "to feature-detect instead of comparing version strings", and unknown
  values are to be ignored. So additions are expected.
- **The requests the window writes are undocumented as lines.** The first request, the interrupt and the mode are
  documented as methods of the Agent SDK only. The window depends on their lines.
- **The lasting yes** (`updatedPermissions`) is documented for the SDK's callback only. The session's form was
  measured; nothing else.
- **The session file** is internal, and "changes between versions". The older conversation reads it, and a picker
  (L4) would too.
- **The direction of the product.** The documentation calls `-p` the "alternative for scripting". The SDK is built on
  the same lines, which helps them last, but the interactive program gets the user-facing work.
- **What was measured is narrow.** The smallest model, new folders under the temporary folder, short runs. Claude
  Code moved from 2.1.326 to 2.1.332 while it was measured.
- **The guard.** `tools\upstream.ps1 -Verify claude` plays the probe's cases and writes `stream-shape.json` again, so a
  new version's change shows as a diff. The screen mode stays the way back (SM-9.3).

## 5. What must be built before the stream could be the default

In order:

1. The Store's copy keeps the screen as its default (L28). Small.
2. A typed slash command that only the screen has is said, not sent, after measuring each (L12). Small.
3. A lapsed sign-in offers the screen, and the sign-in reminder is not said over the stream (L11). Small.
4. A typed `!` line is held, with what it would do (L17). Small.
5. What a tool printed, under its call (L21). Medium.
6. A picker of the folder's earlier conversations, and the preset's bare `--resume` handled (L4). Medium.
7. Plan approval, measured, then built (L13). Medium.
8. "No, and tell Claude why" on a permission question (L14). Small.
9. The NVDA plan's section for the stream mode (stage 8, owed already). Small.

The total: six small and three medium. Nothing large.

Worth building soon after, not before: the turn's summary line (L23), the list of commands (L3), the offered command
with Copy and axTerminal (L16).

## 6. What the two hours of use should try

- A long reply: is the wait before speech heard (L24)?
- A task with many tool calls: is the missing tool output felt (L21)?
- Plan mode with Ctrl+P, until Claude asks to start: what is heard (L13)?
- Refuse a tool call: does Claude ask what to do instead (L14)?
- The slash commands he uses with the screen, such as `/config`, `/status`, `/resume`, `/rewind`, `/tasks`: what is
  heard for each (L12)?
- A `!` line, to hear the difference (L17).
- A message sent while Claude works: Queued, then its block.
- An interrupt during a tool call, then a new message.
- An agent and a shell in the background: the sounds and F8.
- Close the window and open the folder again: the older part shown. Then the screen, and back (the hand-over).
- The trust question at each new window: is it tolerable (L15)?
- Are the thinking lines or the turn's summary missed (L22, L23)?
- "Done" right after the reply, never early: the gain the stream was built for.
- The window in the background at a turn's end: the louder alert.
