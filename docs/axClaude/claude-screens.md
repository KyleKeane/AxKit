# Claude's screens and the app's dialogs

What each slash command of Claude Code shows in screen reader mode, and where the app lets you answer it (SPEC.md D33). The code form of this list is `SlashCommands.All` in `src/AxClaude.Core/Transcript/SlashCommands.cs`; keep the two in step.

The texts themselves (the line labels, the prompt rows, the hint rows and their key names, the mode line, the survey rows, every pattern) are in one table, `ClaudeCodeTexts` in `src/AxClaude.Core/Transcript/ClaudeCodeTexts.cs`, each with a comment saying what it is and, where known, the Claude Code version and the fixture that show it. When Claude Code changes a word, that table is adjusted and this page with it; the classes that read the screen and the window take the texts from there, and `ClaudeCodeTextsTests` fails when one of the most characteristic ones is written in another file. A second agent program would get a table of its own of the same shape.

Recorded: 33 commands were opened bare with Claude Code 2.1.281 in `C:\Temp\axclaude-fixture`, each in its own Claude process, and cancelled with Escape (2026-09-24). The others come from https://code.claude.com/docs/en/commands only and were not opened, mostly because typing them alone already acts (`/clear`, `/logout`, `/compact`, `/init`) or opens another program. Skills and plugins also appear as slash commands; they are not listed here.

## Where each one is answered

The app does not need to know the command: the shape of Claude's screen decides. The command only decides whether a list goes to the control area or stays in the message field. The control area is the message field's place at the bottom of the window, which holds one list or one field while Claude waits (SPEC.md D33).

1. **None.** The command prints text into the conversation and Claude is back at the `$` prompt, or the session changes or ends, or another program opens. Nothing to answer.
2. **A list of answers.** The cursor sits on `Select with numbers [1-N]. …` or `Enter y/n:` under numbered or `y.`/`n.` answers. The control area lists the answers, named with the question.
3. **A screen's lines and keys.** The cursor sits on a key-hint row (`↑/↓ to select · Enter to view · Esc to close`) or on a tab row (`Settings  Status   Config   Usage   Stats`). The control area lists the screen's lines, then one item per key the hint row names. A screen whose hint row names no Enter and that has no tabs only informs (`/btw`, `/goal`, `/mobile`, `/tasks` with nothing running): it is a read-only notice instead, its title and lines with Close and a button per other key (SPEC.md FR-2.11).
4. **Typed input.** A command that wants text after it goes in the message field, guarded so that a message is not sent into a question by mistake (SPEC.md FR-2.10). An `Enter text for` row (after "Other") gets a field in the control area; the message field also sends your words there without the guard, one keystroke at a time.

## One gap in the app today

- **Tab screens are hidden from the conversation** (still so; the control area lists their lines). `/status`, `/usage`, `/cost`, `/help` and `/permissions` put the cursor on the tab row at the top, and every row below the cursor is treated as chrome (FR-3.5), so their content never reaches the conversation.

## Lists of answers

- `/model`: "Select model", five models with the current one marked. Hints: `←/→` adjusts effort on the same screen, `s` uses the model for this session only, Enter sets it as the default.
- `/effort`: "Effort", six levels, current marked, no text rows.
- `/advisor`: "Advisor (experimental)", three models and "No advisor", current marked; a long explanation above and a recommendation below.
- `/theme`: "Theme", eight themes, current marked. Answer 8 ("New custom theme…") leads further. `Ctrl+T` turns syntax highlighting off.
- `/export`: "Export conversation", copy to clipboard or save to a file. Saving probably asks for a file name next (not recorded).
- `/memory`: "Memory", user instructions, project instructions, open the auto-memory folder. The answers open an editor or a folder.
- `/resume`: "Resume session", the recent conversations of the folder with a search row above. Typing searches; `Ctrl+A` shows all projects, `Ctrl+B` only the current branch.
- `/chrome`: "Claude in Chrome", four actions; the last is a toggle ("Enabled by default: No"). The prompt row says only "Then Enter to submit".
- `/mcp`: "Manage MCP servers", one answer per server with its state. An answer leads to that server's screen, another list (recorded with 2.1.282).
- `/hooks`: "Hooks", 33 hook events. An answer leads to that event's screen, which waits on `Esc to go back`: a screen (recorded with 2.1.282). Two-digit answers are typed quickly, as the list's own type-ahead.
- `/config` (also `/settings`): 44 numbered settings under the tab row (`Settings  Status   Config   Usage   Stats`) and the prompt row "Enter a number to change [1-44], or Escape to save and close:", printed after the frame's end. A number flips a true-or-false setting, or opens a numbered list of the setting's values ("Project instructions"), whose Escape returns to the settings; either way the list comes back. Escape on the settings saves and closes (recorded with 2.1.282). The app lists it as "Claude's settings": Enter on a true-or-false setting shows True and False first and sends the number only for a change; Enter on any other setting opens Claude's list of values.
- `/feedback`: "Feedback drafts", first "Loading…" over `Enter to write new feedback · Esc to close` for a moment (a spinner row, so not a screen), then the drafts Claude Code has queued on this machine, newest first: this session's under a "This session" heading, the others under "Other sessions" ("1. <title>, idea · 1m · <folder> · transcript available"), and "+ Write new feedback" last, with the prompt row "Select with numbers [1-N]. Then Enter to submit:" (recorded with 2.1.302). The headings are left out of the list of answers. A draft leads to its review screen: Type, Title, Area, Failure mode, Task, Details, "Send transcript: yes · sends this conversation to Anthropic", the environment line, "❯  Send feedback", waiting on `↑/↓ to move · Enter to send · d to discard · Esc to later`, which the area lists as Move (Up), Move (Down), Send (Enter), Discard (d) and Later (Escape). The ❯ marks Claude's highlight, which Up and Down move through the rows, and the hint row changes with it: on Send transcript, Task and Failure mode (`◀ yes ▶`) it is `↑/↓ to move · ←/→ to change · Enter to next · d to discard · Esc to later`; on Details, Area and Title the row becomes a field, with the cursor at the end of its text and the hint `↑/↓ to move · Enter to next · ctrl+j to new line · Esc to cancel edit` under the rest of the review, typed keys going into the field (the reader finds that hint row under the cursor, so the screen still waits, and the area shows a field named with the row: Enter sends the typed text to the end of the row and moves on, Escape cancels the edit). Enter on Send feedback: `Sending feedback…`, then `done: Feedback sent (receipt <id>). Thanks!` over `Any key to continue`, a screen with Continue (Enter), then the list without the draft. Discard and Later both bring the list back, Discard without the draft; Escape on the list closes it. (Recorded 2026-10-18, not kept as fixtures: the lists named other sessions' folders.) "Write new feedback" leads to typed text. With no draft queued the panel shows only `Enter to write new feedback · Esc to close`, a screen.

Extra keys are not offered. `/model` names `s` (this session only) and `←/→` (effort), `/resume` Ctrl+A and Ctrl+B, `/theme` Ctrl+T. They act on the highlighted row, and a typed number answers at once without moving the highlight: `s` tried in `/model` with 2.1.282 applied to the model already highlighted. The app would first have to move Claude's highlight with the arrow keys, more machinery than these keys are worth; the message field still reaches them.

## Lists of answers: questions from Claude's tools

Recorded with Claude Code 2.1.281 (2026-09-24); every answer is typed one keystroke per character, then Enter.

- Workspace trust at startup: "Permission Required: Accessing workspace", y or n.
- A tool's permission question (`--permission-mode default`): "Permission Required: Create file" or "Bash command", the command or file and a preview, "Do you want to proceed?", then Yes, Yes and always allow / switch mode, and No as the last answer. A `Tip:` row inside it is skipped. "Tab to amend" draws nothing in screen reader mode and is not offered. No returns to the prompt with "What should Claude do instead?".
- Plan approval (`--permission-mode plan`): "Permission Required: Ready to code?", Claude's plan as text, then Yes with auto mode, Yes with manual approval, and "No, keep planning", which asks for feedback on an "Enter text for option 3" row: a field in the control area, as after Other.
- Claude's own question (AskUserQuestion): a header row (" ☐ Colour"), the question, the answers, then Other and "Chat about this". Other asks for your words on an "Enter text for option 4 (Other)" row: the control area holds a field named with the question (Enter sends, Escape goes back to the list). "Chat about this" ends the question like Escape. Seen again with 2.1.307 in an off-screen replay of the window (2026-10-21, `src/AxClaude/CLAUDE.md`), the same shape; no fixture of that version is kept, so a change in the reader is checked against the 2.1.281 fixtures until one is recorded (the `record-fixture` skill). Since 2.1.319 the header row is `Question: Colour`, drawn right under the reply's rows, and each answer carries its description after a dash (`2. Green — Green as your preferred colour`); the `one-question-claude2.1.319` fixture records it (2026-11-07, after the reader, not knowing the header, named the question by the reply's last line).
- The record of an answered question (2.1.281 to 2.1.319; the `one-question-claude2.1.319`, `several-questions` and `other-answer` fixtures): once the answers are submitted, Claude Code draws, in the question's place, `●  User answered Claude's questions:` (a no-break space and a space after the bullet) and under it one row for each question of the call, `· Which colour do you prefer? → Green`: the whole question, an arrow, and the answer's name, the names of several joined with commas (`→ Apple, Cherry`), or the words typed after Other (`→ tulip`). It comes at once after the last answer (after the review's y for several questions), before Claude's next `claude:` row, and stays in the conversation. These are Claude Code's words, not Claude's reply: the app shows them, as `LineKind.AnswerRecord`, and never speaks them as reply lines (SPEC.md FR-7.4b; `ClaudeCodeTexts.AnsweredLabel` and `AnsweredRowStart`). Not recorded: what stands there for a question cancelled with Escape or ended with "Chat about this", and how a question longer than the console or one with line breaks is laid out (a row wrapped at the console width is taken as the rest of the row above).
- Several questions in one call: a tab row ("←   ☐ Colour   ☐ Fruit   ✔ Submit   →") above each, one list per question ("Question 1 of 2"), then "Review your answers" with y or n. Since 2.1.316 the row above is "Questions: Colour (answered), Fruit" instead, and each question's row starts with "> " (the review's lines too); the reader takes both shapes (`several-questions-claude2.1.316` fixture).
- Several answers allowed ("comma- or space-separated for several"): the list has check boxes; the ticked keys go joined with commas. Such a question has a Submit step even when it is asked alone (2.1.282, `wrapped-answers` fixture: `←   ☐ Next work   ✔ Submit   →` over the one question), so "Review your answers" follows it, which the app submits unless Review answers is on. On 2.1.325 the review still follows such a question (seen by the window script `12-questions`, step 8a, 2026-11-15: the conversation holds "Question: Review your answers" after it, and the app submitted it); since 2.1.319 a question alone stands under `Question: <header>`, which numbers nothing, so the reader's step alone no longer told that a review follows (`Question.ReviewFollows`). Not recorded as a fixture yet: a recording should show the header row over a question alone with several answers, the review's rows (its title, its header row, y and n), and what follows a typed Other among the ticked answers.
- An answer longer than the console width ("1. Label — description") wraps onto the rows under it. The reader joins those rows to the answer when the numbers show they belong to it: the answer above is numbered just before the one below, or the last answer is the one the prompt row names ("[1-5]"). Recorded in `wrapped-answers`.

## Screens to read

- `/status`, `/usage`, `/cost`: one tabbed screen, tabs Settings, Status, Config, Usage, Stats; `/cost` and `/usage` open on Usage. Cursor on the tab row; `Esc to cancel` at the bottom; the Usage tab also takes `d` (day) and `w` (week).
- `/help`: tabs Help, General, Commands, Custom commands; shortcuts and pointers. Cursor on the tab row.
- `/diff`: "Uncommitted changes (git diff HEAD)". Hint: `↑/↓ to select · Enter to view · Esc to close`.
- `/tasks`: "Background", the running tasks. Hint: `↑/↓ to select · Enter to view · Esc to close`. With nothing running, 2.1.309 to 2.1.316 offered `Esc to close` alone (an information screen, FR-2.11); since 2.1.319 the hint names Enter there too, so it is a question list, which Escape cancels (seen by the NVDA pass of 2026-11-08 and 2026-11-09; the questions script takes either). A task's details (seen in the background fixture) take `← to go back · Esc/Enter/Space to close · x to stop`.
- `/goal`: "Goal", the current goal or "No goal set". Hint: `Esc to dismiss`. An information screen: a notice.
- `/btw <question>`: the side answer, answered without entering the conversation (recorded with 2.1.310 on 2026-10-24, `--model haiku`). The screen's first line is the question (`/btw what is two plus two? answer in one word`), then the answer (`Four`), over the hint `↑/↓ to scroll · c to copy · f to fork · Esc to close`. A second `/btw` in the session keeps a history: the first question stays the first line, the newest question and its answer follow, and the hint is `⇧←/→ to browse · c to copy · f to fork · x to clear history · Esc to close`, Shift+Left showing an earlier answer under the same rows. An information screen: a notice titled with the first line, the rest as its text, buttons Close, Copy (c), Fork (f) (and Browse (Shift+Left), Browse (Shift+Right), Clear history (x) for the second). In the conversation it is recorded as `btw: <the question>` (the newest question: the first line under the title that starts with `/btw `, else the title), found with b, not as a `Question:` line (SPEC.md D33, 2026-11-14). No fixture holds a `/btw` yet: these rows are the ones read live on 2026-10-24 and pinned in `ScreenActionsTests` and `QuestionTests`; a recording should confirm the second question's rows. An answer longer than the console is drawn whole, not scrolled in a box: the console scrolls and its first rows, the question among them, are only in the conversation (80 numbered lines left the screen starting at 32, and Down changed nothing).
- `/mobile`: where to get the phone app. Hint: `Esc to close`, with the cursor on the row under it ("Then run /rc to continue this session from your phone"), which the list shows as its last line (recorded with 2.1.282).
- `/login`: "Login", a numbered list of three sign-in methods (a question), then "Opening browser to sign in…" over `Esc to cancel`, then the address to open by hand over "Paste code here if prompted >" and `Esc to cancel`. Its last screen has no hint row: "Logged in as" and the account, then "Login successful. Press Enter to continue…" with the cursor at that row's end. A row that ends with "Press Enter to continue" is a screen with Enter for its one key, titled with the row's words before it ("Login successful"), a list like the receipt of a sent feedback report (`ScreenReader.PressEnterTitle`; recorded with 2.1.323, 2026-11-13). Until then the message field stayed, and the Enter of the user's next message closed the screen: the message never reached Claude and nothing said so.
- `/rewind`: "Rewind", "Nothing to rewind to yet." in a new conversation. Hint: `Esc to cancel`. With checkpoints the docs describe a list, probably numbered (not recorded).
- `/ide`: "Select IDE", "No available IDEs detected." Hint: `Enter to confirm · Esc to cancel`. With an IDE running, probably a list.

## The feedback card

Claude Code drafts feedback for Anthropic by itself (its SendFeedback tool, the `feedbackDrafts` setting in `/config`) and prints a card above the prompt at the end of the turn, with the cursor back on the `$` row (recorded with 2.1.302 in `feedback-card`):

```
Product feedback drafted: <title>
Draft preview, 3 lines:
> - **What happened:** …
> - **What the user said:** …
> - **Repro:** …
End of preview. Review the full draft with /feedback.
1 to review · 2 to send · 0 to dismiss
auto mode on (shift+tab to cycle)  ·  1 feedback draft
$
```

In a repository the branch row `  ● main` can sit between the mode line and the prompt. The card's keys cannot be pressed from the app: a digit typed in the message field goes to Claude as text with Enter (a 2 went as a message), and at Claude's own prompt a digit lands in the prompt as text too, a 0 also asking `Turn off Claude-drafted feedback? 0 to turn off · Esc to keep` under it. Claude draws the card again after every turn while the draft waits, and `/exit` asks "You have 1 unsent feedback draft" with `Enter to review & send · Esc to discard and exit`, a screen. The way that works is `/feedback`, the list above, and the app goes through it itself for a draft made in a turn of this session: when the mode line's count is higher after a turn than before it and Claude is idle, the app sends `/feedback`, answers the list with the draft the card named (`SessionModel.DraftTitle`) and shows the review as a screen, like a question; the list that comes back after Send, Discard or Later is closed for the user (SPEC.md D33). Drafts already counted when Claude is ready, and a draft put off with Later, wait for `/feedback` typed by hand, which shows the list. The card's rows stay out of the conversation, and the count is not background work. With Options → Questions → Answer from a list off, nothing opens and the card stays in the conversation as printed, less its hint row.

## The rows under a reply while background agents run

Heard live on 2026-11-15 over the Claude Code of that day, through axClaude's own speech; no recording yet, a fixture is owed (the `record-fixture` skill, a turn that starts a background agent and ends before it does). When a turn ends while background agents still run, the rows under the reply were:

```
Waiting for 2 background agents to finish
auto mode on (shift+tab to cycle)  ·  ↓ to manage
  ● main
  ◯  general-purpose <the agent's description> running
  ◯  general-purpose <the agent's description> running
```

The agent rows end in the word `running` and name no time and no token count, unlike the rows above the spinner while the Agent tool runs (`◯  general-purpose Checking callers 2m 15s · ↓ 1.2k tokens`, 2.1.300). All of them are interface rows, out of the conversation and never spoken: `ClaudeCodeTexts.BackgroundWait` (the whole row; the singular `1 background agent` is assumed), `ModeLine`, `BranchRow` and `IdleAgentRow`. Not seen, and to be read off the recording: where the prompt row stands among them (the tests put it under the agent rows, as in the 2.1.300 recording), the exact leading spaces, whether a finished or failed agent's row ends in another word (only `running` is read as an agent; since the workflow's row, another word makes a progress row of a kind not known, below), and whether the waiting row can carry anything after `finish`. With a background shell as well the mode line was `auto mode on  ·  1 shell  ·  ↓ to manage`.

The panel is read as well as hidden, for a window that tells the user about it without speech (the window's signals are SPEC.md FR-7.10, since 2026-11-15: the number in the status bar, a start and a finish sound, a recurring sound, Ctrl+B; their rules are `BackgroundWork`, which adds the shells the mode line counts, `1 shell`, to the agents and takes the largest of the rows, the waiting row's number and a mode line's own count of agents, `1 shell, 2 agents`, a form assumed and not yet seen with agents). Each agent row, in either form, gives the agent's type, its first word, and its description, the words after it without the mark, the word `running` or the time and the tokens (`LineClassifier.AgentOf`, from the named groups of `AgentRow` and `IdleAgentRow`); the waiting row gives its number (`LineClassifier.BackgroundWaitCount`). `SessionModel.BackgroundAgents` is the list the latest frame with a mode line shows, top to bottom, empty when the panel is gone, and `SessionModel.BackgroundAgentsAwaited` the waiting row's number, zero without the row; both are kept while a screen such as `/tasks` covers the mode line, as the background count is. `AgentPanel.Changes(before, after)` says which agents were newly shown and which are no longer shown, compared by type and description, so a redraw, another order or a new time is no change. Assumed until a recording says otherwise: a type is one word (a custom agent's name with a space would give its first word as the type and the rest in the description).

## A dynamic workflow's live row

Seen in the user's own axClaude window on 2026-11-15, Claude Code 2.1.325 in screen reader mode; no recording yet, a fixture is owed (the `record-fixture` skill: a turn that starts a dynamic workflow and ends while it runs, then the workflow to its end). While a workflow runs in the background, Claude Code draws one live row as the lowest row of the screen, under the spinner and the mode line, and redraws it as the workflow goes on. Exactly as the window held them, at two moments, and the row after the turn had ended:

```
   ◯  axkit-design-map-and-verify 14%  1/7 · 9m27s · ↓ 1.7m tokens · ⚠ Large workflow · /workflows to stop
   ◯  axkit-design-map-and-verify 88%  8/9 · 20m18s · ↓ 2.5m tokens · ⚠ Large workflow · /workflows to stop
Waiting for 1 dynamic workflow to finish
```

The code points: three spaces, U+25EF (large circle, the mark the agents' rows have), two spaces, the workflow's name, a space, the percent with `%`, two spaces, the steps done, `/`, the steps in all; then parts joined by ` · ` (U+00B7): the time (`9m27s`), the tokens after U+2193 (`↓ 1.7m tokens`, a small `m` for millions, where the agents' rows of 2.1.300 had `1.2k`), the warning after U+26A0 (`⚠ Large workflow`) and the hint `/workflows to stop`. The view's last rows at that moment were a tool row, `Jitterbugging…`, `auto mode on (shift+tab to cycle)  ·  esc to interrupt`, then the live row: matched by nothing and the lowest row above the prompt, the row closed the chrome block, so the spinner and the mode line were in the conversation, and with Speak replies on all lines each redraw of the row was spoken.

All three are interface rows now, out of the conversation and never spoken (SPEC.md FR-7.10, FR-3.5 c): `ClaudeCodeTexts.WorkflowRow` (named groups name, percent, done, total and elapsed; any parts after the time are taken, since the tokens, the warning and the hint may change or go), `BackgroundWait`, which reads the number and the kind waited for (`background agents`, `dynamic workflow`, and any other `background` or `dynamic` word, counted as work of a kind not known), and `ProgressRow`, the general class: the large circle after spaces or none, two spaces or more, then anything. A row of that class whose details match no known form is a progress row still, out of the conversation and counted as one thing running of a kind not known (`SessionModel.BackgroundUnknown`), so that the next kind Claude Code invents does not become reply text; one space after the mark (`◯ running`) or anything in front of it (`- ◯ …`, `1. ◯ …`) is a reply's own row. The cost: an agent's row in a state not seen (`◯  general-purpose <description> done`) now counts as one thing running until a recording shows what a finished agent's row looks like. The workflow is read into `SessionModel.BackgroundWorkflows` (`BackgroundWorkflow`: name, percent, done, total, the time as written) and the waiting row's number into `BackgroundWorkflowsAwaited`, kept like the agents while a screen covers the mode line; `AgentPanel.WorkflowChanges` compares two readings by name, so a redraw at another percent is no change. With Options → Background work → Show Claude Code's live progress rows in the conversation the rows stay in the conversation as `LineKind.Progress` lines, never spoken, while the spinner and the mode line above them stay hidden.

Not seen, and to be read off the recording before anything else is built on them: the row at 0 percent and whether it shows before the first step; a finished workflow's row and a failed one's (another word, a last frame at 100 percent, or the row simply gone); two workflows at once (one row each, as the agents have, or one row that counts them); the row without the warning (a workflow not large); where the prompt row and the cursor stand among these rows (the tests put the prompt under the live row; the cursor on the live row itself is allowed); whether the mode line ever counts workflows (`1 workflow` is assumed and counted like agents, never added to the rows); and whether the waiting row can name agents and workflows together.

## The marks of a turn, and what hooks put on the screen

Measured on 2026-11-15 with Claude Code 2.1.325 in screen reader mode; `events-and-grounding.md` section 4 has the whole table, with the times.

The marks. Claude Code writes the shells' own marks of a turn into the console stream, OSC 133 with a letter, and the bell. They carry no text and draw nothing; `SessionModel.Marked` and `Bell` raise them in the stream's order.

- A (`ESC ] 133 ; A ; redraw=0`): Claude took a prompt up, 14 to 62 ms after Enter at the idle prompt, or right after the bell of the turn before for a message that waited. Two messages that waited give one A.
- C and D in one write, then the bell within 150 ms (measured: at most 53): a turn finished, or a command Claude Code handled itself (`/exit`, `/clear`, `/usage` at its Escape).
- C and D and no bell: an interrupt (Escape), or a hand-back note with no turn behind it (` Agent "..." finished`: A, C and D within a millisecond).
- C, D and the bell right after Send now (Ctrl+X Ctrl+S): the running reply was cut for the waiting message, and the next A opens its turn.
- A bell with no D before it: a question from a tool.
- At a turn's end the order is C and D, the bell, the reply's rows, the turn summary row; once in 16 turns the rows came 7 ms before the marks.

The spinner's note while a hook runs. With a hook registered for a turn's end, the spinner row carries a note for a frame or two as the turn ends: `Pollinating…   ( running Stop hook )`, and with two hooks `( running Stop hooks… 0/2 )`, then `1/2`. It is the spinner row still, an interface row by the spinner's pattern, never in the conversation and never spoken. It comes in every turn for a hook Claude Code waits for, and in some turns for one it does not wait for, as axClaude's are: in one turn of two when it was tried.

The rows a failed hook prints. A hook of axClaude's does not fail: `AxKit.exe hook` prints nothing and ends with code 0 whatever happens. Should rows like these stand in a conversation, a hook failed, axClaude's or one of the user's own. Seen with hooks whose receiver was gone (the reason is the transport's own text):

```
UserPromptSubmit hook error
<the reason>
PreToolUse:Read hook error
<the reason>
claude: alpha
Ran 1 stop hook (ctrl+o to expand)
Stop hook error: <the reason>
Churned for 3s · done 4:22 PM
Stop hook error occurred · ctrl+o to see
```

The last row stayed under the input row to the session's end. The model takes each for a plain line of the conversation today; none is a pattern of `ClaudeCodeTexts`, and none becomes one before a recording shows them. A hook on PreCompact or PostCompact, which axClaude does not register for this reason, prints a row after a compaction with the hook's whole command line: `PreCompact [<the command line>] completed successfully`.

The settings warning. An event name in a settings file that this Claude Code does not know stops its start, after the trust answer, with a question: the title `Settings Warning`, the file's path, `hooks.<name>: Unknown hook event "<name>" was ignored. Valid events: ...` with every name it knows, `The values listed above were skipped; the rest of the file is in effect.`, then `1. Continue`, `2. Fix with Claude` and `3. Exit and fix manually` over `Select with numbers [1-3]. Then Enter to submit:`. It is a list of answers like any other and shows in the control area. axClaude passes its hooks' file only to a Claude Code at least as new as the events it names (`events-and-grounding.md` EG-2.4), so the question does not come from that file; the script `tests/scripts/axClaude/31-hooks-live.txt` fails when it does.

## The feedback surveys

Now and then Claude Code asks how the session is going, above its prompt, with the cursor on the bare `$` row (recorded with 2.1.323 in the `survey` fixture, forced with the environment variable `CLAUDE_FORCE_DISPLAY_SURVEY=1`):

```
● How is Claude doing this session? (optional)
1: Bad 2: Fine 3: Good 0: Dismiss
auto mode on (shift+tab to cycle)
effort: medium · /effort
$
```

A key alone answers it, without Enter, and only into an empty prompt and after a pause: a key with more typing after it within a moment is the start of a message, lands in the prompt as text, and the survey closes (a message typed over it closes it too). Read from Claude Code's source, not recorded: after an answer it may ask "Can Anthropic look at your session transcript to help us improve Claude Code?" over a link row and `y: Yes n: No d: Don't ask again`, or a short follow-up in a field of its own; "How was Claude's recollection?" after a recalled memory adds `4: Unsure`; with text in the prompt the survey dims and wants Ctrl+X before the key.

The app reads each of these as a question (`SurveyReader`, SPEC.md D33): the title without the bullet, the answers as a list in the control area, the answer sent as its key alone, and Escape sent as Dismiss (`0`) or No (`n`), since an Escape at Claude's prompt answers nothing and a second one opens Rewind. A message from the message field meets the guard of any question (FR-2.10); sent anyway, it closes the survey. With Options → Questions → Answer from a list off, the rows stay in the conversation and nothing is offered.

## Panels

- `/permissions`: tabs Permissions, Recently denied, Allow, Ask, Deny, Auto mode, Workspace; a search row; a numbered list starting with "Add a new rule…". Hint: `←/→ to switch · ↓ to select · Esc to cancel`. Cursor on the tab row.
- `/autocompact`: "Auto-compact window", a value adjusted with `←/→`; `Enter to apply · Esc to cancel`.
- `/artifacts`: "Artifacts", "Loading artifacts…" at first; a list with actions after (not recorded).
- `/auto-mode-setup`, `/bug` (docs only): a draft to review and save; a consent screen before a report is sent.

## None: text in the conversation

- `/agents`: says the wizard was removed and where subagent files live.
- `/output-style` bare: the available styles and "Usage: /output-style <style>". With a style it switches.
- `/import`: "No other AI coding agents detected" here; with one installed the docs describe a picker.
- `/copy`: "No assistant message to copy" in a new conversation; with replies the docs describe a picker of code blocks, `w` to write a file.
- `/btw` bare: "Usage: /btw <your question>". With a question, a screen (above).
- `/context`: works for a moment, then prints the context use.
- `/list-agents`: this session's name and the other Claude sessions.
- `/tui`: the current renderer and "Usage: /tui <default|fullscreen>".
- `/color`, `/heapdump` (docs only): a random prompt colour; a heap snapshot written to a file.

## Typed input

- `/add-dir <path>`, `/cd <path>` (docs only): the path follows the command in the message.

## None: the session changes or ends

- `/clear` (recorded with 2.1.319, 2026-11-07, the `clear` fixture): the completion row `/clear  Start a new session with empty context; previous session stays on disk (resumable with /resume)`; Enter prints `you: /clear`, draws the mode line and the prompt again, blanks the rows below and sets the title back to `✳ Claude Code`. Nothing says the conversation was cleared, so the window empties its own and says so (SPEC.md FR-8.8).
- `/compact`, `/exit`, `/logout`, `/plan`, `/branch`, `/fork`, `/background`, `/subtask`, `/teleport` (docs only). Several act at once and some start work that costs tokens; none was opened.

## None: another program opens

- `/login`, `/keybindings`, `/desktop`, `/insights`, `/install-github-app`, `/install-slack-app` (docs only).

## Seen with 2.1.323 (2026-11-12, not kept as fixtures)

Opened bare with `--model haiku` in `C:\Temp\axclaude-fixture` and closed with Escape. The recording was not kept: `/passes` shows the account's own referral link.

- `/powerup`: "Power-ups  0/10 unlocked  0%", a line on what they are, ten numbered lessons ("1. ◯ Talk to your codebase — @ files, line refs"), the prompt row "Select with numbers [1-10]. Then Enter to submit or Escape to cancel:" and the hint `↑/↓ to select · Enter to open · Esc to close` under it: a list of answers. Escape prints "Power-ups closed". A lesson's own screen was not opened.
- `/passes`: "Loading guest pass information…" over `Esc to cancel` for a moment, then "Guest passes · 3 left", three ticket drawings in box characters, the referral link, a sentence on what a pass gives, and `Enter to copy link · Esc to cancel`: a screen. Escape prints "Guest passes dialog dismissed".
- `/rename` bare in a new conversation: "Could not generate a name: no conversation context yet. Usage: /rename <name>", text in the conversation.

## Not known yet

- `/init`, `/fast`, `/voice`, `/upgrade`, `/remote-control` (also `/rc`), `/autofix-pr` (docs only; the docs do not say what the bare command shows).
- `/claim-credit`, `/usage-credits`: named in hints on recorded screens, not in the docs' list.
