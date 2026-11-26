# axChat from axClaude's stream: a reflection and a brief

A proposal for the maintainer and for the design sitting with Fable, written 2026-11-26 after the stream's
must-build list was finished; it decides nothing and nothing of it is built. It answers the maintainer's question of that day: once axClaude's stream mode is complete,
spin it out as the foundation of axChat, leave axClaude a smaller app for Claude Code's screen, and make axChat the
main app for users; "let me know if that would be a mistake if we have a significant amount of shared infrastructure
between screen and stream mode that will not end up being ported over to axChat".

## 1. The short answer

Not a mistake, on one condition: what the two modes share is moved into the bundle's shared projects first, and both
apps stand on it. Copying axClaude's window into axChat would be the mistake: two copies of the same five thousand
lines, each mended apart, and the user's keys and sounds drifting between two windows that were meant to be one.

## 2. What the two modes share today

axClaude already has the seam this needs (`docs/axClaude/stream-mode.md` SM-3). The window reads one abstract class,
`ClaudeModel` (`src/AxClaude.Core/Transcript/ClaudeModel.cs`), built on the bundle's `LineStore`
(`AxKit.Core/Transcript`); the screen's `SessionModel` fills it from the drawn screen, the stream's `StreamModel` from
the JSON lines. What only a screen has is virtual and answers nothing for the stream. So:

- **Shared by both modes, and wanted by axChat** (axChat's spec, CH-D4: "the reading, the jump keys and the sounds are
  axClaude's"):
  - the window: the conversation view, its quick keys and jump keys, the message field, the control area's questions,
    the status bar, the sounds at a turn's end, the background alert, Export, the reply speech (`Arrivals`), most of
    `src/AxClaude/MainForm*.cs` (about 4,700 of its 7,500 lines);
  - the model's base: `ClaudeModel`, the line kinds, `ExchangeBlock`, `Question` and its answers, `PermissionCore`;
  - background work counted and said (`BackgroundWork`), the AI caution on what Claude asks (rule 11).
- **The screen's alone, and staying in axClaude**: the parser of the drawn screen (`Transcript`: `SessionModel`,
  `ScreenReader`, `LineClassifier`, `QuestionReader`, `ScreenActions`, `SurveyReader`, about 3,000 lines), the event
  core beside the screen (`Flow`, `Hooks`, about 3,500), the pseudo console (`Pty`, `MainForm.Process.cs`).
- **The stream's alone**: `Streaming` (about 3,300 lines: the stream's lines, the model, the permission relay, the
  questions, the hand-over of a session) and `MainForm.Stream.cs` (1,100).
- **Claude Code's places used by both** (root rule 12): `ClaudeCodeTexts` (the stream model shows its questions in the
  screen's words), `ClaudeCodeTranscript` (the session files, read by both for the older conversation and the
  hand-over), `ClaudeCodeSettings`, `ClaudeLauncher`'s environment.

So the shared part is large, and it is exactly what axChat needs most. That is the reason to share it, not a
reason against the spin-out.

## 3. The two shapes axChat could take

- **A. axChat is the conversation app, with endpoints** (the maintainer's direction, read as written): an endpoint is
  Claude Code over its stream (an agent: tools, permissions, background work, sessions), or a chat API over HTTP
  (a model: a reply, no tools, CH-D6), and later another agent with a stream of its own (rule 12's second program).
  axClaude keeps the screen mode alone, for Claude Code's own screens (`/rewind`, `/permissions`, `/login`, `/mcp`
  and the rest, which the stream will never have), and one key hands the conversation between them by its session
  id, as the two modes do today (SM-8.3).
- **B. axChat stays the HTTP chat app of its spec**, and axClaude keeps both modes. The stream's neutral pieces still
  move to shared projects for axChat's use, but Claude Code stays wholly in axClaude.

**Recommended: A, in stages**, because it is what the maintainer described and because the stream is already the
cleaner of the two hosts of Claude Code. But A changes three things the spec now says, each for the maintainer:
- axChat would run Claude Code as a child program (an access layer's way, rule 8), beside its own HTTP requests (a
  third network contact of axKit's own, CH-D5): two kinds of endpoint, two kinds of network statement.
- CH-D6 (no tool use) holds for an HTTP model and not for Claude Code, whose tool calls are asked through the relay
  with their caution. The tool use is then Claude Code's, never axChat's own.
- Rule 4 forbids an app referencing another: Claude Code's places used by both apps move to a shared project of
  their own (`AxKit.ClaudeCode`, pure), not left in `AxClaude.Core`.

## 4. The order that avoids the mistake

1. Finish the stream's must-build list in axClaude (`docs/axClaude/stream-mode.md` section 13, its two rounds): done on
   2026-11-26 (27581744 and 94e87522), all nine; the maintainer's two hours of use and the NVDA passes of section
   26 remain before the stream is the default. The new pieces were written for the move: `ToolOutput`,
   `ConversationChoice`, the plan's question in `StreamAsking`, each pure in `AxClaude.Core` and free of the screen's.
2. Name the neutral conversation: rename nothing yet, but write the vocabulary of events a source gives the window
   (a reply's text, a call and its result, a question with answers, a turn's end, background work, an error), which
   is `ClaudeModel`'s abstract part read as a list, and check axChat's `ChatEvent` (CH-3.4) against it.
3. Move the shared pieces one per commit (rule 4), unchanged: the model's base and line kinds to `AxKit.Core`, the
   conversation window's parts to `AxKit.Forms` (`ConversationForm` grows into it), Claude Code's shared places to
   `AxKit.ClaudeCode`. axClaude runs on the moved pieces after each commit, and its scripts and NVDA passes hold it
   still.
4. Build axChat on them: the HTTP endpoint first (its spec's stages 1 and 2), then the Claude Code endpoint, moved
   from axClaude with its scripts.
5. Then axClaude loses its stream mode, keeps the screen and the hand-over key, and its menus shrink.

Steps 2 and 3 are the cost of doing it right: several days of moving code without changing behaviour, which the
window scripts and the NVDA passes make safe.

## 5. The risks

- **Two windows of one conversation.** A user with axChat and axClaude open on one folder could write to one session
  from both. The hand-over must close one side, as the modes do now.
- **The stream has no promise of shape** (`stream-mode.md` SM-6.7), and the interactive program gets the new
  features first. axClaude's screen mode stays maintained either way.
- **The Store's copy** cannot run the stream today (SM-9.6). axChat's Claude Code endpoint would be absent there until
  measured.
- **Scope.** axChat's spec is a small HTTP app; A makes it the bundle's main app. The first users' window should not
  wait on the move: the HTTP endpoint can ship before the Claude Code endpoint moves.

## 6. The maintainer's leaning, 2026-11-26

"i think calude code cli is a good first client for the axChat access layer, but if you think it is different enough
that it warrants staying in axlaude then we can leabe it here. if it is in axclaude i still think it should be
available in axchat as an option. that is probably the way to go". The assistant's answer the same day: shape A, with
the Claude Code endpoint's code in the shared projects (section 4, step 3), so that axChat offers it from its first
release and axClaude keeps its stream mode on the same code until axChat's endpoint has held, then keeps the screen
alone. One code, two windows for a while, never two copies.

## 7. Questions for the sitting with Fable

1. Shape A or B (section 3)? If A, is the Claude Code endpoint axChat's from its first release, or after the HTTP
   endpoint has been used?
2. Is the vocabulary of events (section 4, step 2) one list for an agent and a plain model, with the agent's extras
   (permissions, background work, sessions) as optional parts, or two lists?
3. Where Claude Code's shared places live: a project of their own (`AxKit.ClaudeCode`, recommended) or in
   `AxKit.Core`?
4. Which window: `ConversationForm` grown into axClaude's whole window, or axClaude's `MainForm` moved down and made
   the conversation window type (framework.md)?
5. What axClaude is named and offered as after the move: the same app with one mode, or "Claude Code's screens"
   opened from axChat by a key.
6. Does axHelp's conversation (still on the screen, SM-9.5) move with the stream to axChat's Claude Code endpoint?

7. Beside axChat, if the sitting has time: a review of the workflow graph (`docs/axKit/workflow-graph.md`, its
   decisions settled 2026-11-26) with its first graph file and its list of goals over the bounds, before its steps 4
   and 5 (the scripts checked for where they land, the gate on map changes) are built.

## 8. Files to read for the sitting

`docs/axChat/SPEC.md`; `docs/axClaude/stream-mode.md` (SM-3, the seam; SM-6, what the stream does not have; SM-10.5, the two hours);
`src/AxClaude.Core/Transcript/ClaudeModel.cs`; `src/AxClaude.Core/notes/stream.md`; `docs/axKit/framework.md` and
`components.json`; the root `CLAUDE.md`, rules 4, 8, 11 and 12.
