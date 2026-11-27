# For your return

Written by the assistant (Fable) during the three days from 2026-11-27, at the maintainer's word to work alone and
to keep one record for the walk-through afterwards. Newest state first. This page is deleted once it has been gone
through together; what must last is in the specifications, the priorities list and the NVDA test record.

## Where things stand

Updated 2026-11-27, evening.

- **Blocked on you: nothing.**
- **The release review** (item "the release review of 2026-11-27"): ten reviewers read everything since 5.10.0, about
  150 findings, no certain blocker. Fixed, tested, committed and pushed so far: privacy and the AI tools channel (18),
  the tools and the release path (23), the changelog rewritten to the end state, both of axClaude's sources (33).
  Running: the apps' data paths (33). To come: the access layers, the window base and the automation engine, the
  seal's counter.
- **Then**: questions as lists; the shortenings with their keys; 6.0.0 prepared and not published; then, on branches,
  the workflow graph's remaining steps, axChat, axAutomate's macro launcher.

## What you decided before leaving (2026-11-27)

- Every question is a list like Claude's questions in axClaude: any key reads it, the arrows choose, it starts on the
  confirming option and Enter takes it, Escape declines, one option opens the full text. No exception for an AI's
  request: root rule 11 and AX-11.12 change with it.
- Escape keeps deselecting in a list.
- The gap in the AI tools' seal (an older sealed copy put back) is closed now.
- 6.0.0 is prepared and not published. NVDA passes at any time. llama.cpp's server and a small model installed
  outside the repository. All the shortenings built, their proposed keys included.
- After the release is ready: the workflow graph, then axChat, then the macro launcher.

## What I decided, for you to turn back

Each is one line to undo unless it says otherwise.

- **axTemplate's launcher does not put a private clipboard copy back.** After a template is put in, a password copied
  in axPass is not returned to the clipboard (ordinary text is). Giving it back would keep it past the 30 seconds
  axPass promises. The cost: you copy the password again. (axTemplate TP-D6.)
- **A damaged `bundle.json` gives no update check for that run**, where it gave one. A network contact you turned off
  must not come back by a damaged file.
- **Your lists move from `Documents\Tasks` whole or not at all** on an update: a file held by another program now
  keeps the whole folder where it was until the next start.
- **The AI tools' log names what happened**: "done: ran", "asked the user", "not run", "not known", with the
  command's real name.
- **axClaude over the stream no longer keeps a tool's whole output** in memory, only the three lines shown and the
  count; a key to read all of it would need it kept again (stream-mode.md SM-4.11).
- **A permission answer's label ends ", and more"** when the command is longer than the label ("1. Yes, Bash command
  git status, and more").
- **A day and month typed without a year is the next such day**, today included (in the batch running now).
- **axChat's first endpoint is Claude Code's stream**, the HTTP endpoint second; the reasons and the rest of my
  answers to the brief's questions are in `docs/axChat/spin-out.md` section 7.
- **The stream stays behind its switch in axClaude.** Making it the default waits for your own two hours with it.

## Owed your ear

Kept in `docs/axKit/nvda-test-record.md` and `docs/axKit/owed.md`; here only what is new since you left.

- The kinds without a sound yet: question-read-again, not-with-source.
- (More as the passes run.)

## What to try first

- (Written when 6.0.0 is prepared.)

## Things only you can do

- The file `approval-passphrase.md` in the folder beside the repositories: if it holds the passphrase that signs
  outside-report approvals, move it somewhere no session can read.
- A stash in this repository from 2026-11-09 ("Edits made in the wrong checkout ... drop when convenient"): yours to
  drop.
- Recordings of Anthropic's own API for axChat need a key that costs money: owed.
- The four outside reports that await your signature.
