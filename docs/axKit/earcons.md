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

Pattern: struck noise, one or two strikes of about ten milliseconds, like a typewriter. Never a note.

| Variant | Sound | State |
|---|---|---|
| A line of output arrived | one strike, 1500 Hz under the noise | exists (`Sounds.Click`) |
| A tool call arrived | two lower knocks, 650 then 850 Hz | exists (`Sounds.Tool`) |

### 2. Moves: where your own key took you

Pattern: two smooth notes run together, under a tenth of a second in all, right after a navigation key. The direction
is the variant.

| Variant | Sound | State |
|---|---|---|
| The edge: nothing further that way, or the key did nothing | A4 falling to A3, loud | exists (`Sounds.Edge`) |
| A command recalled into the field | E5 rising a fourth to A5 | exists (`Sounds.Recall`) |
| Moved onto a heading | G4 leaping an octave to G5 | exists (`Sounds.Heading`) |
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

### 4. Questions: a list of answers, pick one

What the user does: Up and Down through the answers, Enter on one. Nothing is typed.

Pattern: the same note twice with a short gap, a knock at the door.

| Variant | Sound | State |
|---|---|---|
| Claude asks | E5, E5 | exists (`Sounds.Question`) |
| axKit asks (a notice with choices of its own: save changes, delete, agree) | proposed: the same knock lower, C5, C5 | proposed |

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

### 6. One field: type, and Enter answers

What the user does: types one thing; Enter does it at once; Escape cancels.

Pattern, proposed: a short note and then a long held one on the same pitch, "da-daaa": a prompt that waits. No
existing sound has this shape.

| Variant | Sound | State |
|---|---|---|
| A text asked for (a name, a folder, a search) | proposed: C5 short, C5 held | proposed |
| A secret asked for (a password; nothing typed is spoken) | proposed: the same lower, G4 short, G4 held | proposed |

### 7. Forms: several fields, and a Send at the end

What the user does: Up and Down through the fields, Enter edits the one in focus, Send or Ctrl+Enter sends; Enter in
a text box never sends.

Pattern: a quick trill, two notes alternating four times. The existing notice trill has exactly this shape and
becomes this class.

| Variant | Sound | State |
|---|---|---|
| A form that sends something out of the computer (a report) | proposed: the trill C6, E6, C6, E6 as today | the sound exists (`Sounds.Notice`); the meaning is proposed |
| A form that changes something here (an item's fields, settings) | proposed: the same trill a fifth lower, F5, A5, F5, A5 | proposed |

### 8. Notices: something to read, nothing to choose

What the user does: reads; Enter or Escape closes.

Pattern, proposed: one soft low note with a long fade, a single bell. Today a notice that opens by itself plays the
trill of class 7; with this scheme the trill means a form and a plain notice gets its own.

| Variant | Sound | State |
|---|---|---|
| Information or help | proposed: one bell, E4 | proposed |
| An error, something failed | proposed: one bell lower and louder, A3 | proposed; today an error has no sound of its own, and a refused key plays the edge |

### 9. The call from another window

Pattern: three rising notes to a held one, twice, long and loud: Claude is done or asks while its window is in the
background. The only earcon longer than half a second.

| Variant | Sound | State |
|---|---|---|
| Done, or a question, in the background | G5, C6, held E6, and again | exists (`Sounds.Alert`) |

### 10. Working

Pattern: soft plucked notes in a slow loop while Claude works. Not an earcon: a state that lasts.

| Variant | Sound | State |
|---|---|---|
| Claude is working | sixteen plucked notes, eight seconds, looping | exists (`Sounds.StartMusic`) |

## The classes side by side

| Class | Pattern | What you do next |
|---|---|---|
| 1. Tick | struck noise, one or two | nothing |
| 2. Move | two notes run together, very short | carry on |
| 3. Turn | one to three even notes, last one held | wait, or type |
| 4. Question | the same note twice | pick an answer from a list |
| 5. Code | five gliding swoops | decide with care |
| 6. One field | short note, then held note | type, Enter answers |
| 7. Form | a trill of two notes | fill fields, then Send |
| 8. Notice | one bell | read, then close |
| 9. Call | rising three, twice, long | go to that window |
| 10. Working | soft looping plucks | wait |

## What building this would change

- New sounds to choose by ear: axKit's own question (4), the AI tool variant (5), the two of one field (6), the lower
  form trill (7), the two bells (8), the value steps (2).
- `Sounds.Notice` is renamed for what it will mean, a form, and the plain notices move to the bell.
- `OverlayPanel` and `ControlArea` play the class of what they show (a list of choices, one field, a form, a text to
  read) instead of one sound for every notice; the kind is known where the notice is made.
- The specifications that name sounds change with it: `docs/axKit/SPEC.md` AX-7 and AX-11.12, `docs/axClaude/SPEC.md`
  FR-7.3 and FR-2.12, `docs/axKit/mcp.md` MC-7.3 and MC-9, and every user guide's list of sounds.
- An NVDA pass cannot hear sound; the automated scripts check that the right sound was asked for by its name, and the
  user's ear checks how it sounds.

## Open questions for the user

1. Whether a plain notice needs a sound at all, or silence and its words are enough.
2. Whether the AI tool variant of class 5 should differ by direction, by pitch, or both.
3. Whether one field and a form are far enough apart by ear; to be settled by listening, not on paper.
