# axKit: one event schema, and the boundary that holds it

A proposal for the maintainer to read, written 2026-11-17 at his question of that day. Nothing here is built, and
nothing is built from it before he has read it. Requirements are numbered EV-n.m; decisions left to him are in
section 9.

His words: "should we abstract our own event schema and then focus on forcing anything we are interfacing with to
tie into our event schema, so that we are always managing a clear event driven system that runs axKit, and then we
just need to focus on holding the boundary of ensuring that other systems only trigger events in our schema, and we
manage their system as a separate abstraction compatibility layer."

## 1. Summary

Everything that reaches the user in axKit is one of a short list of kinds of event: you landed somewhere, nothing was
found, a line arrived, Claude asks, the turn is done. Today each window decides by itself, in 69 places, what such a
moment sounds like and says, and each access layer reads its outside program in its own way. The proposal:

- **One schema.** A closed list of kinds of event, axKit's own, with a few plain fields. The list exists already as
  `docs/axKit/families.json`, which describes what the code does today.
- **Adapters.** Each outside program (Claude Code, a shell, Git, KeePassXC's tool, an MCP server) is behind one
  adapter: the only code that knows that program's words, and whose only output is events of the schema. This is
  rule 12 of the root `CLAUDE.md`, with its other half added.
- **One door.** A window never plays a sound or speaks for something that happened except by raising an event. One
  function turns a kind of event into its sound and its words, so the same kind sounds the same everywhere.
- **A boundary held by tests**, not by memory: where an outside program's words may stand, and where a sound may be
  played.

It is not a framework. An event is a record, the door is a method call on the UI thread, an adapter is a plain class.

## 2. Words

- **Event**: something that happened which the user may need to know of. It has a kind.
- **Kind**: one of the closed list of section 3. A new kind is added to the list on purpose, with its sound or none,
  never invented where it is raised.
- **Source**: who or what started it: the user's hands, the program underneath, an AI, axKit itself, Windows. The
  sources are the "actions" of `families.json`.
- **Adapter**: the code between one outside program and axKit. It reads the program by whatever way that program
  offers (a screen, a stream of lines, a file, exit codes) and raises events.
- **The door**: the one function a window raises an event through (working name `Raise`).
- **Presenter**: the table behind the door: for each kind, its sound, whether its words interrupt speech, and which
  switch turns its sound off.

## 3. The schema

- EV-3.1 **An event is plain data**: its kind; its source; the words to say, already in axKit's wording, or none;
  and, for the few kinds that carry something to act on, that thing: the line of text for `line`, the question with
  its options for `program-asks`, the command for `command-offer`. No field holds a word of an outside program's
  own protocol.
- EV-3.2 **The kinds are those of `docs/axKit/families.json`**, in its six classes: what came of your own command;
  something arrived; where the program stands; something asks you; work that goes on behind; the window itself. 51
  kinds on 2026-11-17. The list in code (an enumeration in `AxKit.Core`) and the list in the file are held to each
  other by `tools/families.ps1`, as the sounds are held to `Sounds.cs` today.
- EV-3.3 **A kind has one meaning.** Where today one kind sounds two ways (`nothing-found`: the buzz, the edge, or
  words alone; `program-asks`: the trill or two notes), either the kind is split in two on purpose, each with its
  meaning, or it gets one sound. Which, is settled kind by kind with the maintainer's ear (section 9).
- EV-3.4 **Small enough to hold in the head.** The list is spoken in `families.md` in one sitting. A proposal to add
  a kind says which existing kind it is not.

## 4. Adapters

- EV-4.1 **One adapter per outside program, and it alone knows the program's words.** For Claude Code that is
  already so for the texts (`ClaudeCodeTexts`, `ClaudeCodeKeys`, `ClaudeCodeHooks`, `ClaudeCodeTranscript`,
  `ClaudeCodeStream`), held by `ClaudeCodeTextsTests` and `ClaudeCodePlacesTests`. The same shape for the shell
  (`ShellTexts`), Git, KeePassXC's tool and MCP.
- EV-4.2 **An adapter's output is events and nothing else.** It never plays a sound, never calls the window's
  `Announce`, never touches a control. What the window shows (the conversation's lines, a list of answers) it builds
  from events.
- EV-4.3 **Two adapters for one program are allowed, and are the point.** Claude Code read off its screen and Claude
  Code read off its non-interactive stream (`docs/axClaude/stream.md`) raise the same kinds. The window does not
  know which one feeds it; a setting chooses, and one can stand in when the other breaks.
- EV-4.4 **What an adapter must work out stays inside it.** The event core of axClaude (`SessionFlow`: when a turn
  is open, when Done is due, which reply lines are confirmed) is the screen adapter's own logic. The stream adapter
  needs almost none of it: a turn's end is one line.
- EV-4.5 **An adapter is tested from recordings of its program**, with no window: a recording in, the events out,
  compared with a kept list. The timeline fixtures and their replayer are this already for the core.

## 5. The door

- EV-5.1 **A window raises an event; it does not play a sound.** After the move of section 7 step 2, `Sounds` is
  called from one place, the door, and a test fails on a call anywhere else.
- EV-5.2 **The door gives the kind its sound and its words by one table**, the presenter: the sound or none; whether
  the words interrupt; the switch that turns the sound off (chimes, ticks, the sound on headings, background work).
  A sound is added or changed in that table alone, and only when the maintainer has chosen it by ear.
- EV-5.3 **The words stay what they are.** The door does not rewrite a sentence: each event carries its words, and
  every announcement that exists today is said as it is. What changes is where the sound is decided.
- EV-5.4 **The rules that hold for every announcement hold at the door**: one notification on the main control,
  several joined into one within a moment, nothing on a plain arrow key (bundle rules 1, AX-7.3).
- EV-5.5 **The automated checks see events.** The automation channel records the kind beside the sound and the
  words, so a script can expect "the event edge" and no longer the sound's name alone; the workflow pages can then
  say what happens in a person's words.
- EV-5.6 **What is not an event stays out**: a redraw, a list refilled with the place kept, a theme repainted. The
  kind `upkeep` names them so that their silence is a decision, and they are never raised.

## 6. Holding the boundary

Each line is a test over the sources, of the kind the repository has (`LayoutTests`, `SecondWindowTests`,
`ClaudeCodePlacesTests`):

- EV-6.1 A word of an outside program's protocol stands only in that program's adapter files.
- EV-6.2 `Sounds.` is called only by the door (after step 2).
- EV-6.3 An adapter's files reference no window type and call no `Announce`.
- EV-6.4 Every kind of the enumeration is in `families.json` and the other way round; every kind is raised
  somewhere or is named as never raised.
- EV-6.5 A window's event for something an outside program did comes from that program's adapter, never from the
  window reading the program's text (rule 12's last sentence, made checkable).

## 7. The path, in four steps, each small and each leaving every app working

1. **This document read and changed by the maintainer.** No code.
2. **Sounds through the door.** The enumeration of kinds; the presenter's table; `Raise`; then the 69 places that
   play a sound today, moved a window at a time, each raising its kind. `tools/families.ps1` knows every place
   already. Here the ten inconsistencies noted on the earcon item are met one by one and settled by ear, and the
   kinds with no sound stay silent until one is chosen. Checked by every window's scripts and an NVDA pass. No
   outside program is touched.
3. **axClaude's two adapters.** The screen adapter is what exists (the model, the event core), with its output
   named as events; the stream adapter is new, over `ClaudeCodeStream`. A setting chooses, off at first. This is the
   second mode behind a switch of `stream.md`, section 7 (a), built on the schema.
4. **The other access layers**, one at a time, when each is next worked on: the shell (axTerminal), Git, MCP
   servers, KeePassXC's tool.

Steps 2 and 3 do not depend on each other and are the next work on sounds and on axClaude in any case.

## 8. What this is not, and what it costs

- No message bus, no subscribers, no events crossing threads by themselves: an adapter that reads on a background
  thread posts to the UI thread as today (rule 3), and the door is called there.
- No plug-ins and no interfaces with one implementation (SPEC B-10). An adapter is chosen by a setting in one place.
- Not a rewrite. Step 2 changes about 69 lines' worth of calls and adds one table; nothing a user hears changes
  except where a choice of section 9 changes it on purpose.
- The cost is discipline: a new moment in any app needs its kind first. That is the point, and the tests of section
  6 are what make it hold when nobody remembers.

## 9. For the maintainer to decide

1. Whether this is the direction. If so, steps 2 and 3 are planned from here.
2. The kinds' names, heard: `families.md` reads them in one sitting. A name he would not say is changed before it is
   code.
3. The ten sound choices on the earcon item, met in step 2: for each, one sound, two kinds, or as it is.
4. Whether an event that has only words and no sound still goes through the door from the first day (so that the
   checks see every event), or only sounds first.
5. Whether axKit keeps a log of events on the user's computer (kinds and times, no text), off unless switched on:
   it is what a later item needs (axHelp learning from the questions asked), and what would show, as the shadow
   lines did for axClaude, where an app behaves oddly.
6. Whether each app's own sound quality, proposed in `earcons.md`, is a column of the presenter's table.
