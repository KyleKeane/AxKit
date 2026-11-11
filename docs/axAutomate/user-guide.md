# axAutomate user guide

axAutomate shows workflows: lists of steps that drive axKit's windows, such as "press Ctrl+Shift+P, then expect
Command palette to be said". Its first workflows are the automated checks of every axKit app, so that you can read
what each check does, step by step. It is part of axKit, a bundle of apps for people who use a screen reader; it was
built for and with NVDA. This first version views the workflows: running and changing them come later.

## Install

axAutomate is part of axKit. Installing axKit (see the axKit guide in the Help, About) gives you a Start menu entry
`axKit axAutomate` and a console command `axAutomate`. Any axKit window starts one too: Alt+W, New window,
axAutomate.

## Start

- Console: `axAutomate C:\path\to\a\folder`, or the Start menu entry and then Ctrl+O to choose the folder. axKit's
  own checks are the folder `tests\scripts` of its repository. axAutomate opens the folder again at the next start.
- The window is two panes, as axTask's: the navigator on the left, the detail pane on the right, and a status bar.
  The focus starts in the navigator.

## The folder

The navigator lists the folder's folders first, then its workflows: "axDown, folder, 7 workflows", "01-start,
workflow, 42 steps". Right or Enter goes into a folder or a workflow; Left or Backspace comes back up. Type the start
of a name to jump to it.

## A workflow's steps

Inside a workflow, each row is one step, numbered, in the order it runs, as a sentence:

- "3. Press Ctrl+Shift+P"
- "4. Expect said: Command palette"
- "5. Expect said: trust, may not come, then 2 steps"
- "5.1 then: Wait 1.5 seconds"
- "Note: 2.2 The trust question may come." A note is a sentence for the reader, never run.

After the sentence come small marks:

- **may not come** (for an expectation) or **goes on if it fails**: a failure here does not stop the workflow.
- **reports**: the step has its own message for when it fails.
- **then 2 steps**: steps that run only when this one passed. They follow right after it, numbered under it, 5.1 and
  5.2, and start with "then".
- **otherwise 1 step**: steps that run only when this one failed, the same way.

Tab or Enter on a step goes to its details: its number, what it does, each of its values (the keys, the text), what
happens when it fails, what it reports, its blocks, its note, and the line of the file it comes from. Escape or Tab
goes back. Ctrl+Q reads every line at once.

## Keys

- Ctrl+O: open a folder of workflows.
- Ctrl+J: the workflow's file as text, a script in axDown, a workflow file in axJSON.
- F5: read the folder or the workflow again, after the file changed.
- F1: every key of the window.
- Ctrl+N, F2, Delete and the other keys that change items in axTask say "This version only views the workflows".

## The files

A workflow is a `.workflow.json` file, or one of the scripts the checks use today (a `.txt` file, one step per line),
read as a workflow. The format is in `docs/axAutomate/SPEC.md` section 3.

## Settings

Options, Text sets the font and its size; Options, General the update check.

## Updates

The Help, Check for updates.

## If something goes wrong

Options, Developer, Copy diagnostics copies what helps a bug report; F4 sends a report.

## About

The Help, About.

## Disclaimer

axAutomate comes with no warranty; see the licence in the Help, About.
