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
4. Build axChat on them: the Claude Code endpoint first, moved from axClaude with its scripts (section 7, answer 1),
   then the HTTP endpoint (its spec's stages 1 and 2).
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
- **Scope.** axChat's spec is a small HTTP app; A makes it the bundle's main app. Its specification (CH-1 to CH-9)
  is written for the HTTP endpoint alone and gains a section for the Claude Code endpoint before the build starts:
  what of axClaude's FR and SM requirements axChat takes as they are.

## 6. The maintainer's leaning, 2026-11-26

"i think calude code cli is a good first client for the axChat access layer, but if you think it is different enough
that it warrants staying in axlaude then we can leabe it here. if it is in axclaude i still think it should be
available in axchat as an option. that is probably the way to go". The assistant's answer the same day: shape A, with
the Claude Code endpoint's code in the shared projects (section 4, step 3), so that axChat offers it from its first
release and axClaude keeps its stream mode on the same code until axChat's endpoint has held, then keeps the screen
alone. One code, two windows for a while, never two copies.

## 7. The questions, with Fable's answers (2026-11-27)

Written after the release review of that day, in which the stream's code was read whole by a reviewer of its own.
Each answer is a recommendation; what is the maintainer's to settle says so.

1. **Shape A or B? If A, which endpoint first?** Shape A, and the Claude Code endpoint first. The stream's code
   exists, is recorded in 23 fixtures and was heard with NVDA; no HTTP protocol is written. The shortest way to a
   working axChat is to give the conversation it already has a window of its own, and to add the HTTP endpoint
   (openai-chat against a real llama.cpp server, CH-3.1) on the same contract next. That is also the maintainer's
   leaning of section 6.
2. **One vocabulary or two?** One contract, and it is not a list of events: it is the model the window reads
   (section 8). An agent's extras are optional members that answer nothing for a plain model, exactly as
   `ClaudeModel` already does for what only a screen has. `ChatEvent` of CH-3.4 stays, as the name of what an HTTP
   protocol hands its model, never something the window sees.
3. **Where Claude Code's shared places live.** A pure project of their own, `AxKit.ClaudeCode`: the stream, the
   relay, the session file's reader, the texts, the launcher. `AxKit.Core` stays free of any one program's words
   (rule 12), and a second agent later gets a project of the same shape.
4. **Which window.** Neither as put. `ConversationForm` stays the shape (axCalc and the console stand on it). What
   axClaude's `MainForm` does for any source, the questions in the control area, the reply's speech, Done, the
   status bar's state, moves down into one shared class between `ConversationForm` and the apps, and both axChat
   and axClaude derive from it. `ConsoleForm` stays the program in a pseudo console, for axClaude's screen and
   axTerminal.
5. **What axClaude is afterwards.** The same app and name, with one job: Claude Code's own screen, for the commands
   that only it has, opened from axChat on the same conversation by one key, and handing it back by the same key
   (the hand-over by session id exists, SM-8.3).
6. **axHelp's conversation.** It moves to the Claude Code endpoint once axChat has been the maintainer's daily
   window for a while; until then it stays where it is. It is one argument of a start, not a design.
7. **The workflow graph.** Reviewed with the code: sound in its counting and its bounds; three faults of the crawl
   are in the review's fixes (it would have crawled axClaude and axHelp in a whole run, and the real desktop under
   the NVDA tier; it pressed a copy command and one that starts a program). Before step 6.1 of it is built (the
   palette walking a way), read section 9 below: a way walked for the user is a command run by something other
   than his hands, and has one place to pass through.

## 8. The one contract, and what must change before the move

Three vocabularies stand in the documents for one thing: the kinds of event at the door (`events.md`, what reaches
the user), the model a conversation window reads (`ClaudeModel`, stream-mode.md SM-3), and axChat's `ChatEvent`
(CH-3.4). They are three layers, and the documents now say so the same way (`events.md` EV-4.2 as amended
2026-11-27):

- **A source's adapter reads its program** (a screen, a stream of lines, an HTTP stream) by that program's one
  place (rule 12), and fills **a model**: the conversation's lines, and a few facts (working or not, what is asked,
  what waits, the background's work).
- **The window reads the model and nothing else**, and raises what happens **as kinds of event at the door** (rule
  13). An adapter never raises one and never speaks.

So the neutral conversation of section 4, step 2, is this: `ClaudeModel`'s abstract part, under a name that is no
program's (`ConversationModel`), in `AxKit.Core`, with `ClaudeModel` deriving from it for what only Claude Code has
(the permission mode, the agents and workflows, the screens). Four things must change in axClaude first, each
without a change a user meets, each held by the scripts and fixtures that exist:

1. **A question is answered by itself, not by a number.** Today every list's Enter hands a key ("3") to one
   function, which gives it to whatever asks at that moment; the review found two ways this approves a question
   that was never heard. A request becomes an object the list is shown for and answered through, and an answer can
   reach no other request. The review's repair does the first half; the move finishes it.
2. **The stream's questions stop travelling through the screen's forms.** `StreamAsking` builds the screen's prompt
   row for the user's own words, and the window parses it back; the request carries "asks for words" itself.
3. **The shared plumbing leaves screen-named places**: the pipe's frame (`Hooks/HookFrame`), the timed pipe read
   (in `MainForm.Hooks.cs`), the launcher's environment (`Pty/ClaudeLauncher`), the session file's reader
   (`Record`).
4. **The window's end of the questions' pipe gets tests** (`AskChannel`, in the window's file today, with no test of
   a wrong secret, the time limit or a stop during a wait), and moves first.

The order of section 4 holds, with these four as its step 2 and the review's fixes before them.

## 9. One place for everything that runs a command for the user

Three planned things do the same act, a command of a window run by something other than the user's hands: the AI
tools' run (built, mcp.md MC-7), axAutomate's launcher of macros (planned, `docs/axAutomate/SPEC.md` AU-D4), and
the palette walking a way through the workflow graph (planned, `workflow-graph.md` WG-6.1). The command language
already has the one place they must pass, `CommandGate.Decide(origin, mark, state)`, with a table of who may run
what (`command-language.md` section 3). So: a macro and a walked way are each an origin of that gate, with their row
of the table (a read or a change runs and is said; an act asks first, as it does for an AI tool), written before
either is built; a macro's steps are edges of the graph, so the graph is also what checks a macro before it runs
(the place it expects to be on, the command that exists there). The three documents point here.

## 10. Files to read for the sitting

`docs/axChat/SPEC.md`; `docs/axClaude/stream-mode.md` (SM-3, the seam; SM-6, what the stream does not have; SM-10.5, the two hours);
`src/AxClaude.Core/Transcript/ClaudeModel.cs`; `src/AxClaude.Core/notes/stream.md`; `docs/axKit/framework.md` and
`components.json`; the root `CLAUDE.md`, rules 4, 8, 11 and 12.
