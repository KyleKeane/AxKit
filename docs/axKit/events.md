# axKit: one event schema, and the boundary that holds it

Written 2026-11-17 at the maintainer's question of that day, and taken as the direction by him the same day
(section 9, point 1). Step 2 of section 7, the sounds through the door, was built that day; section 10 says
exactly what was built and what was left for the later steps. Requirements are numbered EV-n.m; decisions left to him are in
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
2. **Sounds through the door** (built 2026-11-17: section 10, where the count is exact). The enumeration of kinds; the presenter's table; `Raise`; then the 69 places that
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

1. Whether this is the direction. If so, steps 2 and 3 are planned from here. **Decided on 2026-11-17: yes**, by
   the maintainer through the question tool, in the order of section 7: the sounds through the door first, then
   axClaude's two adapters. For step 3 he chose the stream as a second mode behind a switch, off at first, the
   screen the default and the fallback (`docs/axClaude/stream.md`, section 7 (a)).
2. The kinds' names, heard: `families.md` reads them in one sitting. A name he would not say is changed before it is
   code.
3. The ten sound choices on the earcon item, met in step 2: for each, one sound, two kinds, or as it is.
4. Whether an event that has only words and no sound still goes through the door from the first day (so that the
   checks see every event), or only sounds first. **Decided on 2026-11-25: through the door from the first day**,
   by the maintainer through the question tool. Built the same day at the base (section 10).
5. Whether axKit keeps a log of events on the user's computer (kinds and times, no text), off unless switched on:
   it is what a later item needs (axHelp learning from the questions asked), and what would show, as the shadow
   lines did for axClaude, where an app behaves oddly. **Decided on 2026-11-25: yes**, a switch under Options, off
   until the user turns it on, nothing written while it is off, the log on the user's computer alone. Built the same
   day (section 10), in the axKit menu rather than an app's Options, since AX-9.1 puts every bundle-wide switch there.
6. Whether each app's own sound quality, proposed in `earcons.md`, is a column of the presenter's table. **Left on
   2026-11-25 for the earcon sitting**: no column until the maintainer has heard the qualities.
7. A kind the list lacks, found on 2026-11-17 by his own report: a turn that the program begins by itself. Claude
   Code starts a turn when work in the background finishes, and axClaude plays the music, speaks the reply and says
   Done with no word of why, which he took for axClaude restarting. Claude Code's record names such a turn. Whether
   it gets words ("Claude goes on: background work finished"), a sound, or neither; the item is "axClaude: a turn
   that starts by itself says why". Decided by him on 2026-11-17 and built the same day: one short line, "Claude
   goes on by itself", never interrupting, no sound, with a switch under Options, Announcements (axClaude's SPEC.md
   FR-7.3b; the kind is `turn-self-started` in `families.json`). The words and their moment are owed his ear
   (`nvda-test-record.md`, the Owed section of the state from the event core).

## 10. As built

### The words through the door, and the log of events (2026-11-25)

Section 9 points 4 and 5. Nothing a user hears changed: the same words, in the same order, at the same moment, with
the same sounds.

- **Every word said goes through the door.** The window's `Announce(text, interrupt)` is
  `EventDoor.Say(EventKind.Said, window, text, interrupt)`; a place that knows its kind says
  `Announce(EventKind.<kind>, text, interrupt)`. The door notes the kind and hands the words to the window's own
  speaking (`AxKitForm.Speak`: joined within 300 ms, held until the window is in front, dropped while it is put away,
  all as before). `AnnounceBefore` ("Claude ran Word wrap") is `SayAhead(EventKind.AiRan, ...)`. A control that speaks
  on itself at once calls `EventDoor.SayOn(kind, control, ...)`. No word path plays a sound: a kind's sound is still
  raised by its own `Raise`, where it was, so that order and timing stay.
- **The kinds named at the base and in the shared controls**: `Refuse` (refused), `Failed`'s one line (failed),
  "Pasted" (done), a refused field of a notice and "Check the box first" (refused), a form's "title, form" and a
  notice's title said again over its field (said: the entry kinds are raised by the components alone,
  `ComponentsTests`, and these are only their words), the check box's and the form lists' new state
  and a check item's state from the palette (changed), `TranscriptView`'s own words (nothing-found, edge, changed,
  done, failed, landed, said). Every other announcement (about 540 places in the apps and in the shared flows) names
  none and is raised as **said**, until its place is given its kind; no kind was added.
- **The boundary**: `Announcer`, the one UI Automation notification, is internal and called by the door alone
  (`EventDoorTests`: no other file names it but the two channels that listen to `Raised`); the door's internal
  `Speak`, for words raised already and said later, is called only by `AxKitForm.Notices.cs` and `OverlayPanel.cs`.
- **The log of events** (`AxKit.Core/Events/EventLogger`): a switch of the bundle, Options, **Keep a log of
  events**, in `bundle.json` (`BundleSettings.KeepEventLog`), off by default. While it is on, every event at the door,
  by its sound (`Raise`, `Working` on, `BackgroundRunning`, `TurnDoneThroughMute`) or its words, is one line of
  `%LOCALAPPDATA%\AxKit\logs\events.log`: the local time and the kind's id, never a word. One kind is noted once in
  one turn of the window's thread, so that a refusal's sound and its words are one line and a batch of arriving lines
  one. Nothing is noted or written while it is off. Past 1 MB the file becomes `events.old.log`, replacing the one
  before. The lines are written on a thread of the pool; each window reads the switch as it is made and at each
  activation when `bundle.json` changed. On this computer alone: a report does not carry it.

Left, on purpose:

- The kinds of the places that say **said** today, place by place, as each app is next worked on.
- Events whose raise a switch or a place still skips (the chime of a notice with the chime off, an entry sound with
  Entry sounds off, a notice opened at the user's command, which NVDA reads by the focus) are not at the door; the
  switches are the presenter's later column (EV-5.2).
- The automation channel records `say` without the kind (EV-5.5).

### Step 2, the sounds through the door (2026-11-17)

Nothing a user hears changed with it: each place plays the sound it played before, at the same moment, by the same
switch. What changed is that no place plays a sound any more; it names what happened.

- **The kinds in code** (EV-3.2): `AxKit.Core/Events/EventKind`, 52 values in the order and the six classes of
  `families.json`; a kind's id in the file is its name in small letters with hyphens (`EventPresenter.Id`). The sounds
  by name are `EventSound`, the 20 of the file. `EventKindTests` holds both lists to the file, both ways.
- **The presenter's table** (EV-5.2): `EventPresenter.SoundsOf(kind)`: none for 26 kinds (question-read-again among them since 2026-11-26, waiting on the maintainer's ear), one sound for 28, and two
  for the three kinds that sounded two ways before the door (EV-3.3): nothing-found (the buzz, the edge), no-command
  (the bonk, the edge), program-asks (the trill, the two notes). The test holds the table to the file, and pins
  exactly those three, so that none joins them unnoticed.
- **The door** (EV-5.1): `AxKit.Forms/EventDoor`. `Raise(EventKind)` for a kind with one sound.
  `Raise(EventKind, EventSound)` for the three kinds with two: the place names the sound it played until now, so
  that every undecided place reads `EventSound.` in the code and can be listed; when one is decided by ear, its
  second sound leaves the table and the name leaves the calls. `Working(on)` for the working music and
  `BackgroundRunning(chord, things)` for the chords of background work, the two sounds that last;
  `TurnDoneThroughMute()` and `Muted` for a window that is put away.
- **Every place moved**: the 65 places that played a sound and the 3 that muted the sounds, in 29 files of the
  shared window pieces, axClaude, axTerminal, axTask, axFile and axMedia, each read in its context and given its
  kind; 17 of the raises name a sound, the places of the three kinds with two. A notice says its own kind (`Notice.Kind`: opened by itself,
  a failure, a pairing), and the window base raises it.
- **The boundary** (EV-6.2): `Sounds` is internal to `AxKit.Forms`, so the compiler refuses an app that names it.
  Inside the shared pieces `EventDoorTests` reads the sources: no file but the door plays a sound (the automation
  channel alone listens to what was played); every raise is written whole on one line and names its kind in so many
  words, never a variable, so that each can be found and checked; a sound is named at a raise only for the three
  kinds; the door sends each sound to the method of its own name.
- **Held to the inventory** (EV-6.4): `tools/families.ps1` fails when a file raises a kind that `families.json`
  does not have; which files raise each kind it reads from the code itself (a "where" list per kind was kept by
  hand in the file until 2026-11-23, when a file it did not name broke two builds). Run after the move, it found nothing: every choice of a kind agrees with
  the inventory of 2026-11-16, made by reading the same places a day earlier. And the page it writes came out
  unchanged, which shows that every sound is played from the same files as before.

Left for the later steps, on purpose:

- **The words** (EV-5.3, section 9 point 4). Each window still says its words through its own `Announce`, in the
  order it had with its sound. An event does not carry its words yet. (Through the door since 2026-11-25, above.)
- **The switches** (EV-5.2). Whether an event is raised at all (the chimes' switch, the ticks' switch, the sound on
  headings, a window that is not in front) is still decided where it happens; the table has no switch column.
- **The record of kinds for the automated checks** (EV-5.5). A script still expects a sound by its name.
- **The three kinds with two sounds** (EV-3.3, section 9 point 3), which wait for the maintainer's ear. The other
  inconsistencies noted on the earcon item are places that say words and play no sound, or one sound with two
  meanings; they are met when the words go through the door.
- **EV-6.1, EV-6.3 and EV-6.5**, which are the adapters' (steps 3 and 4).
