# Earcons: the classes of sound

A proposal, asked for by the user on 2026-11-14 and not built: the sounds that exist are listed as they are, and what
is new is marked "proposed". A new sound is chosen by ear before it is built: candidates are played to the user one by
one and only the one he picks goes into `AxKit.Forms/Sounds` (as the command offer's sound was chosen, 2026-11-13).

## Why classes

A screen reader user in a text field knows only that they are in a field. Nothing tells them whether Enter will
answer a question, keep one field of a longer form, or run code. Words say it, but words come after the moment and
are skipped by habit. So the kind of interaction gets a sound of its own, heard first.

The rule of the scheme: **the pattern names the class, and a small change inside the pattern names the variant.**

- The pattern is the rhythm and the kind of sound: how many sounds, how long each, struck or smooth or gliding. It is
  what the ear learns first and never confuses.
- The variant is the pitch (high or low) or the direction (rising or falling). It is learned second, and a user who
  never learns it still knows the class.
- No two classes share a pattern. A new interaction joins an existing class as a variant, or is a new class with a
  new pattern; it never takes another class's pattern with a new meaning.

## What every earcon obeys

- It comes before the words and never replaces them: every earcon is followed by speech that says the same thing.
- It is over within half a second, so that NVDA's speech is not held up. The background alert alone is longer.
- Ticks never queue behind each other; a chime cuts whatever is still sounding.
- Louder means more urgent: ticks softest, chimes in the middle, what asks for care (running code, the edge, the
  background alert) loudest.
- Each can be switched off with its group in Options, Sounds; a class that guards an action (class 5) says its words
  even when its sound is off.

## The classes

Notes are named as on a piano (C5 is the C above middle C); times are milliseconds.

### 1. Ticks: something arrived, nothing to do

Pattern: something tiny and percussive, over within a few tens of milliseconds and with no tail: a typewriter strike of noise, or a mouth pop. Never a held note.

| Variant | Sound | State |
|---|---|---|
| A line of output arrived | one strike, 1500 Hz under the noise | exists (`Sounds.Click`) |
| A tool call arrived | a tiny mouth pop, 30 ms, a round tone falling from 700 towards 300 Hz, no noise (two lower knocks, 650 then 850 Hz, until 2026-11-15) | exists (`Sounds.Tool`), chosen by ear 2026-11-15 |

### 2. Moves: where your own key took you

Pattern: two smooth notes run together, under a tenth of a second in all, right after a navigation key. The direction
is the variant.

| Variant | Sound | State |
|---|---|---|
| The edge: nothing further that way, or the key did nothing | A4 falling to A3, loud | exists (`Sounds.Edge`) |
| A command recalled into the field | E5 rising a fourth to A5 | exists (`Sounds.Recall`) |
| Moved onto a heading | G4 leaping an octave to G5 | exists (`Sounds.Heading`) |
| A letter taken as a command in navigation mode (SPEC AX-8.13) | a high glassy tick, a pure 3800 Hz of 6 ms, before the command runs (a click of 2200 Hz with noise until it was heard as too like the line tick) | exists (`Sounds.NavKey`), chosen by ear 2026-11-16 |
| A navigation letter that found nothing of its kind | a low buzz, 150 Hz with its third and fifth harmonics, 110 ms, in place of the tick | exists (`Sounds.Nothing`), chosen by ear 2026-11-16 |
| A plain key in navigation mode with no command | a wooden bonk, 70 ms, falling from 420 towards 260 Hz with a wood block's overtone | exists (`Sounds.Unassigned`), chosen by ear 2026-11-16 |
| A value changed by Right or Left in a list of fields | proposed: a rise of one step for the next value, a fall of one step for the previous | proposed, with the forms paradigm |

### 3. Turns: where the conversation stands

Pattern: one to three smooth notes at an even, unhurried pace (about ninety milliseconds a note), the last one held
when there are three. Rising opens, falling closes.

| Variant | Sound | State |
|---|---|---|
| Ready for a message | one high note, C6 | exists (`Sounds.Ready`) |
| Your message went out | one low note, A4 | exists (`Sounds.Sent`) |
| A reply begins | C5, E5, up to a held G5 | exists (`Sounds.Responding`) |
| The turn is done | G5, E5, down to a held C5 | exists (`Sounds.Done`) |

### 4. Asks: the knock scale

Everything that asks something of the user is one family, and **the number of knocks says how much is asked** (the
user's idea, 2026-11-14: the length of the sound signals the size of what is wanted; a form is sonified by its
number of elements). One thing to learn, not four.

Pattern: the same note struck again and again at an even pace, a knock at the door, about seventy milliseconds a
knock with a short gap. Only the count changes.

| Knocks | What is asked | What you do |
|---|---|---|
| One | Nothing: something to read (information, help, an error) | Read; Enter or Escape closes |
| Two | One choice from a list of answers | Up and Down, Enter on one |
| Three | One thing to type (a name, a folder, a search, a password) | Type; Enter answers; Escape cancels |
| Four | A form with two fields | Up and Down through the fields, Enter edits one, Send or Ctrl+Enter sends |
| Five | A form with three fields | the same |
| A roll, knocks too fast to count | A form with four fields or more | the same; the words say how many fields |

The ear counts four or five knocks with ease and no more, so the scale stops counting there and a longer form is a
roll. The words that follow always give the exact number ("Report to axKit, 7 fields").

The variant is the pitch, and it says who asks:

| Variant | Sound | State |
|---|---|---|
| Claude asks | the knocks on E5; two of them is the question sound of today | two knocks exist (`Sounds.Question`); the other counts are proposed |
| axKit asks (its own notices, questions and forms) | proposed: the same knocks lower, on C5 | proposed |
| An error among the one-knock notices | proposed: one knock lower still and louder, on A3 | proposed; today an error has no sound of its own |

What this replaces: the quick trill that plays today when a notice opens by itself (`Sounds.Notice`) is retired, its
work done by the count.
### 5. Code: something asks to run or to act beyond the window

What the user does: decides, with care. Running is never the first answer.

Pattern: five fast gliding swoops, eighty milliseconds each, an alarm and not a chime. The loudest class. This is the
only class that glides, so nothing else can be taken for it.

| Variant | Sound | State |
|---|---|---|
| A command offered to run on your computer, outside axKit | five swoops falling from 2400 to 900 Hz | exists (`Sounds.Offer`) |
| An AI tool asks to run a window's command that acts outside that window | proposed: the same five swoops rising, in a lower band (700 up to 1500 Hz) | proposed; today it shares the falling one |
| A command of yours that starts a program or reaches the network, where axKit asks first | proposed: joins this class only if it is ever asked for; no sound today | open |

Why falling for the first and rising for the second: falling and high is the more alarming, and it marks the wider
reach, your whole computer; rising and lower marks the narrower reach, one window of axKit.

### 6. The call from another window

Pattern: three rising notes to a held one, twice, long and loud: Claude is done or asks while its window is in the
background. The only earcon longer than half a second.

| Variant | Sound | State |
|---|---|---|
| Done, or a question, in the background | G5, C6, held E6, and again | exists (`Sounds.Alert`) |

### 7. Working

Pattern: soft sustained chords in a slow loop while Claude works (plucked notes until 2026-11-15). Not an earcon: a state that lasts.

| Variant | Sound | State |
|---|---|---|
| Claude is working | four soft chords with a faint ping in each, fourteen seconds, looping | exists (`Sounds.StartMusic`) |

## The classes side by side

| Class | Pattern | What you do next |
|---|---|---|
| 1. Tick | struck noise, one or two | nothing |
| 2. Move | two notes run together, very short | carry on |
| 3. Turn | one to three even notes, last one held | wait, or type |
| 4. Ask | knocks on one note; the count is how much is asked | one: read; two: pick; three: type; more: a form |
| 5. Code | five gliding swoops | decide with care |
| 6. Call | rising three, twice, long | go to that window |
| 7. Working | soft looping chords | wait |
## The sound of each app, and the start (asked 2026-11-14, proposed)

The user's second ask the same day: a start sequence when the program loads; each app with a sound quality of its
own, so that a window that opens plays a priming sound and brings the ear into that app; and the sounds adjustable in
bundle-wide settings, perhaps per app and per effect.

This adds a third thing a sound can say. The pattern names the class, the pitch or direction names the variant, and
**the sound quality names the app**: the same question knock is heard as a pure tone in one app and as a plucked
string in another, and is still the same knock.

### Sound qualities

A quality is how a note is made, not which note: pure (a sine, as every chime is today), plucked (as the working
music), bell (a struck tone with a long ring), hollow (a reed, like a clarinet), struck wood. The ear tells five or
six apart with ease and seventeen not at all, so a quality belongs to a family of apps, and the single app is told by
its own short motif.

| Family | Apps | Quality, proposed |
|---|---|---|
| Conversations | axClaude, axHelp | pure, as today, so that nothing learned changes |
| Shells and programs | axTerminal, axGIT, axProcess, axMCP, axAutomate | struck wood |
| Texts | axDown, axJSON, axPreview | hollow |
| Things in lists | axFile, axTask, axPass, axCalc | plucked |
| Reached by a hotkey | axStart, axClip | bell, short |
| Sound and video | axMedia | none: it must never play over the user's own media |

The classes that guard or interrupt keep one quality in every app, because they must be known at once anywhere: code
(class 5), the edge (class 2) and the call from another window (class 6).

### The priming sound: a window opened

Pattern: two or three notes of the app's own, in its family's quality, under a third of a second, before NVDA reads
the title. Each app's motif is a different interval or shape (a rise of a third, a fall of a fifth, three notes up
and back), so that inside a family the apps are told apart. It plays when a window opens or comes back from hiding,
never when focus only returns to it.

The hotkey windows open many times a day: theirs is one short note, or nothing, by default.

### The start sequence

Pattern: a longer phrase of about one second, the bundle's signature, the same every time. axKit is not one program
that stays open; each app is a process of its own. So "the program loads" means: an axKit window opens while no other
axKit window is open. The signature plays then, and the app's priming sound follows it. A window opened beside others
plays its priming sound alone. The background apps starting hidden at sign-in play nothing.

### Settings

One file for the bundle, as the theme has, in the same shape as every setting of axKit: JSON, flat, a switch and a
volume at each level, and "as above" where nothing is set.

- **Everything**: on or off, and one volume.
- **Each class** (ticks, moves, turns, asks, code, the call, working music, priming, the start): on or off, and a volume. This is "per effect".
- **Each app**: on or off, a volume, and its sound quality, which the user may change or set to plain. This is "per
  app".
- **An app's class**: only where a user wants it, for example axTerminal's ticks off and everything else on. Written
  in the file by hand or through the Options menu of that app; the menus show the common switches and the file holds
  them all.

What is fixed and has no switch: the words. Every earcon is followed by speech that says the same thing, so a user
with every sound off loses nothing but the early cue. The code class's words are said with or without its sound.

The settings that exist today (Options, Sounds in axClaude and axTerminal: chimes, ticks, working music, the sound on
headings) become entries of this one file, read by every app, and keep their menu items.

### Cautions

- A sound at every window open can become noise. The priming sound must be short, quiet, and easy to switch off for
  one app or for all.
- NVDA starts reading a new window at once: a priming sound longer than a third of a second talks over it.
- Qualities must be tested on small laptop speakers and through headphones; a low bell is lost on the first, a high
  struck sound is harsh in the second.
- axMedia never plays a sound of axKit's while media plays.

## What building this would change

- New sounds to choose by ear: the knock itself and its pace, the roll, the lower pitch for axKit and the error's
  (4), the AI tool variant (5), the value steps (2).
- `Sounds.Notice`, the trill, is retired; `Sounds.Question` becomes two knocks of the scale.
- `OverlayPanel` and `ControlArea` play the count of what they show (a text to read, a list of choices, one field, a
  form of so many fields) instead of one sound for every notice; the count is known where the notice is made.
- The specifications that name sounds change with it: `docs/axKit/SPEC.md` AX-7 and AX-11.12, `docs/axClaude/SPEC.md`
  FR-7.3 and FR-2.12, `docs/axKit/mcp.md` MC-7.3 and MC-9, and every user guide's list of sounds.
- An NVDA pass cannot hear sound; the automated scripts check that the right sound was asked for by its name, and the
  user's ear checks how it sounds.

## Candidates kept

Sounds played to the user while choosing, liked, and kept for a later use (his word, 2026-11-16: "those were all
lovely, please keep them for other purposes later"). Each is one call of `AxKit.Core.Audio.WaveTone`, so it can be
played again as it was heard.

- Ticks, heard for the navigation click; the high glassy tick was chosen and these three kept:
  - Wooden tock: `Tick(0.4, 900, 900, 0.012, 15, 400, 0.1)`, a lower tock of 12 ms.
  - Rising chirp: `Tick(0.3, 1400, 2600, 0.014, 15, 200, 0.0)`, a quick glide up, 14 ms.
  - Dry snap: `Tick(0.35, 5000, 5000, 0.005, 4, 900, 0.9)`, mostly noise, like a finger snap.
- Knocks, heard for a key with no command; the wooden bonk was chosen:
  - Plain bonk: `Knock(0.4, 320, 170, 0.012, 0.030, 2.0, 0.0, 90)`.
  - Low thud: `Knock(0.45, 200, 95, 0.015, 0.040, 2.0, 0.2, 110)`.
  - Double bonk: the plain bonk twice, 60 ms each with 40 ms between.
- Flat tones, heard for nothing found; buzzy was chosen:
  - Middle: `Flat(0.3, 180, 90, 5, buzzy: false)`, the first candidate.
  - Low and long: `Flat(0.35, 130, 150, 8, buzzy: false)`.
  - High and short: `Flat(0.3, 240, 60, 4, buzzy: false)`.
- Not liked for a key with no command: two blips at 300 Hz, three blips, two high blips at 550 Hz, two buzzy blips
  at 250 Hz ("it should be more deadpan and bonk like").

## Open questions for the user

1. Whether a plain notice needs its one knock at all, or silence and its words are enough.
2. Whether the AI tool variant of class 5 should differ by direction, by pitch, or both.
3. Whether three, four and five knocks are told apart at speed, and where the roll should begin; to be settled by
   listening, not on paper.
4. Whether the sound quality should go with the family of apps, as proposed, or each app should have a quality of its
   own although seventeen cannot be told apart.
5. Whether the hotkey windows play a priming sound at all.
6. Whether the start sequence plays at the first window after sign-in only, or whenever no other axKit window is open.
