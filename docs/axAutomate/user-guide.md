# axAutomate user guide

axAutomate shows workflows: lists of steps that drive axKit's windows, such as "press Ctrl+Shift+P, then expect
Command palette to be said". Its first workflows are the automated checks of every axKit app, so that you can read
what each check does, step by step. It is part of axKit, a bundle of apps for people who use a screen reader; it was
built for and with NVDA. This version views the workflows and runs them; changing them comes later.

## Install

axAutomate is part of axKit. Installing axKit (see the axKit guide: Help, About, axKit guide) gives you a Start menu entry
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

## Running workflows

F5 inside a workflow runs it; F5 in a folder runs every workflow of that folder, one after another (not the folders
inside it). The axKit windows of a run open on a desktop of their own: they never come to the front, never take your
keyboard and play no sound, so you can go on working, in axAutomate or anywhere else. Each workflow gets fresh
windows with settings of their own, never yours, and its steps that make files and folders make them in a folder of
the run's own: a workflow with such a step that writes anywhere else cannot run from the window.

A workflow's `run` steps are different: their command lines run through Windows' command prompt on your own
computer, with your files, as if you typed them. So before a run with one, a notice names them: "This workflow runs
commands on your computer: git init. Run it?". Cancel is the default, so Enter and Escape both decline and you hear
"Not run"; Alt+R, or Tab to Run and Enter, runs it, and only then is "Running ..." said. The command lines are named
as they will run, with the run's own folder called "the run's folder".

These two guards catch slips; they do not make a workflow safe. A workflow drives axKit's windows, and those
reach what you can: it can open one of your files in axDown and save it changed, use axFile's operations, or type a
command into axTerminal, without a question. A workflow is a program: run only workflows you wrote or trust.

While steps pass, nothing is said. A step that fails is said as it ends, with what was expected and what the run saw:
"Step 2 failed: Expect the focus: Nothing like this. Not within 10 s; seen: ...". When a workflow ends you hear one
line, "01-passes passed, 4 steps" or "02-fails failed at step 2", and after a folder's run, "Run finished: 3 of 5
workflows passed, 1 failed, 1 could not run". A folder's run you stop says where it stopped and how many it did not
reach: "Run stopped at 03-waits: 1 passed, 1 failed, 3 not run".

Afterwards the rows say how the last run went: "02-fails, workflow, 3 steps, failed at step 2", and inside it the step
that failed ends with "failed". Tab on either gives "Last run:" with what the run saw. Escape during a run stops it
after the step going on ("03-waits stopped"), from either pane; closing the window stops it the same way, and its run's
windows are gone within half a minute.

Ctrl+R on a workflow, or inside one, shows the report of its last run: how it ended, then every step played with what
the runner saw ("ok  3: ..." or "FAIL 4: ...  -> ...", numbered by the line of the script's file, not by step), and the timeline of what the run's windows said and where the
focus went. It is a text: the arrows read it, Ctrl+F finds in it and Escape closes it. The reports are kept until the
window closes.

A workflow this version cannot run says why when you press F5, for example one with "otherwise" steps; its details
say "Cannot run". So does one with a step whose number is not one, such as "wait 2s". The checks that need NVDA listening run only in the NVDA pass, not from the window.

## Keys

- Ctrl+O: open a folder of workflows.
- Ctrl+J: the workflow's file as text, a script in axDown, a workflow file in axJSON.
- F5: run the workflow, or every workflow of the folder. Escape during a run: stop it.
- Ctrl+R: the report of the last run.
- Session, Refresh: read the folder or the workflow again, after the file changed.
- F1: every key of the window.
- Ctrl+N, F2, Delete and the other keys that change items in axTask say "This version views and runs the workflows;
  changing them comes later".

## The files

A workflow is a `.workflow.json` file, or one of the scripts the checks use today (a `.txt` file, one step per line),
read as a workflow. The format is in `docs/axAutomate/SPEC.md` section 3.

## Settings

Options, Text sets the font and its size; Options, General the update check.

## Updates

Help, Check for updates says whether a newer axKit exists.

## If something goes wrong

Options, Developer, Copy diagnostics copies what helps a bug report; F4 sends a report.

## About

axAutomate is part of axKit by Dr. Kyle Keane, www.kylekeane.com. Help, About has the licence and the version.

## Disclaimer

axAutomate comes with no warranty; see the licence (Help, About, Licence).
