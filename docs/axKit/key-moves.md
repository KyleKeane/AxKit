# Key moves, proposed

A page of proposals, written 2026-11-25. It ends when every family below is settled. Then the moves are in the key
tables, `key-families.json` holds what is left, and this page is deleted.

## What this page is

The key families (`key-families.json`) list 80 keys that do not fit their family's rule. Each one has a bullet here,
with a proposal. Nothing moves before the maintainer has read its family (his decision of 2026-11-25).

The rules, from SPEC AX-8.18, one line each:

1. A key alone moves and reads where you are. With Shift: the other way, or select as you move.
2. Ctrl with a letter acts on what you are on. Ctrl+Shift with the same letter: its reverse or its wider form.
3. Ctrl with a number is the window's own numbered action. Ctrl+Shift with it: the reverse.
4. Alt with a letter opens a menu. Alt with a number goes to a place. Alt with an arrow: the choices of what you are on.
5. A function key alone says or shows. Shift with it: the reverse or another form.
6. Ctrl with a function key opens a standalone app. Ctrl+Shift with one opens an access layer.
7. Ctrl+Shift with Space or punctuation works from anywhere in Windows.

How it is settled:

- One family per sitting, in the order of the sections below.
- The assistant asks through the question tool, one key or one group of keys at a time.
- The assistant's recommendation is the first answer.
- A move is made after its family is read: the key table, the spec, the guide and the F1 text in one commit.

Each bullet gives the key, the apps, what it does today, and the proposal. "Move to" names a key its rule gives.
"Free" means checked on 2026-11-25: in the window's key table, the shared tables (`ItemKeys`, `EditorKeys`,
`ConsoleKeys`, `SelectionKeys`, `MarkdownKeys`), `BundleKeys` and the command inventory. "Keep" gives the reason.

## Ctrl with a number

The rule: the window's own numbered action, such as a heading level in the editors, or an action key in axTask.

The four windows with places got Alt+1, 2 and 3 on 2026-11-16. Their Ctrl keys stayed beside them for a while. The
proposal is to take the Ctrl keys away. Alt+1, 2 and 3 stay as they are.

- Ctrl+1, axClaude, Message field. Move to Alt+1, which it already has. The Ctrl key goes.
- Ctrl+2, axClaude, Conversation. Move to Alt+2, which it already has. The Ctrl key goes.
- Ctrl+3, axClaude, Latest response. Move to Alt+3, which it already has. The Ctrl key goes.
- Ctrl+1, axTerminal, Command field. Move to Alt+1, which it already has. The Ctrl key goes.
- Ctrl+2, axTerminal, Terminal. Move to Alt+2, which it already has. The Ctrl key goes.
- Ctrl+3, axTerminal, Latest output. Move to Alt+3, which it already has. The Ctrl key goes.
- Ctrl+1, axGIT, Changes. Move to Alt+1, which it already has. The Ctrl key goes.
- Ctrl+2, axGIT, Log. Move to Alt+2, which it already has. The Ctrl key goes.
- Ctrl+3, axGIT, Branches. Move to Alt+3, which it already has. The Ctrl key goes.
- Ctrl+1, axProcess, Processes. Move to Alt+1, which it already has. The Ctrl key goes.
- Ctrl+2, axProcess, Services. Move to Alt+2, which it already has. The Ctrl key goes.
- Ctrl+3, axProcess, Startup. Move to Alt+3, which it already has. The Ctrl key goes.
- Ctrl+0, axTask, All tasks. Move to Alt+0, free. All tasks is a view of the window, and Alt with a number is a
  place. Zero stays its number.

A note: axTask's nine action keys, Ctrl+1 to Ctrl+9, fit rule 3, which names them, so they are no misfits. The
family in `key-families.json` follows rule 3's words since 2026-11-25.

Count: 13 moves, 0 keeps.

**Settled by the maintainer on 2026-11-26**, through the question tool: the twelve Ctrl place keys go, Alt+1 to 3
alone stay; axTask's All tasks moves to Alt+0.

## The function keys alone

The rule: say or show. Several of these keys are Windows' own, and the proposal keeps those.

- F2, axAutomate, axFile, axMCP, axPass, axTask and axTemplate, Rename. Keep: Windows' own key for rename.
- F3, every window but axClip, axHelp and axStart, Find next. Keep: Windows' own key for find next, as in Notepad.
- F3, axGIT, Find next in the text. Keep: the same key, in the text pane.
- F4, every window, Send a report. Keep: it shows the report's form, which rule 5 allows. It is a bundle key, and
  every F1 text names it.
- F5, axFile, axGIT, axMCP, axPass, axProcess, axTask and axTemplate, Refresh. Keep: Windows' own key for refresh.
- F5, axStart, Refresh programs. Keep: Windows' own key for refresh.
- F5, axPreview, Reload. Keep: Windows' own key for refresh, as in a web browser.
- F5, axAutomate, Run. Move to Ctrl+R: R for run, and running acts on the workflow you are on (rule 2). Ctrl+R is
  free once the last run's report leaves it for F9 (section Ctrl with a letter). The case for keeping: F5 runs a
  program in Visual Studio.
- F6, axAutomate, axMCP, axPass, axTask and axTemplate, Switch between the navigator and the detail pane. Keep:
  Windows' own key for the next pane, as in File Explorer and a web browser.
- F6, axPreview, Between the text and the details. Keep: Windows' own key for the next pane.
- F6, axDown and axJSON, Between the text and the field under it. Keep: Windows' own key for the next pane.
- F6, axCalc, axClaude and axTerminal, Switch between the view and the field. Keep: Windows' own key for the next pane.
- F6, axGIT, Switch between the list and the text. Keep: Windows' own key for the next pane.
- F6, axMedia and axProcess, Back to the list. Keep: Windows' own key for the next pane.
- F6, axFile, Switch between the list and the Favorites list. Keep: Windows' own key for the next pane.

Collides with: the F5 move needs the move of axAutomate's Ctrl+R, in the section Ctrl with a letter.

Count: 1 move, 14 keeps.

**Settled by the maintainer on 2026-11-26**, as proposed: the fourteen Windows keys kept as listed exceptions;
axAutomate's Run moves to Ctrl+R once the Ctrl+letter family has moved its report key.

## Ctrl+Shift with a letter

The rule: the reverse or the wider form of the same letter with Ctrl.

- Ctrl+Shift+C, axTerminal, Interrupt. Keep: Ctrl+C stays the window's copy, Windows' own key. C names what is sent
  to the shell.
- Ctrl+Shift+C, axClaude, Ctrl+C (sends it to Claude Code). Keep: the same reason as axTerminal's.
- Ctrl+Shift+D, axTask, Hide done tasks. Move to Ctrl+H, free: H for hide. It has no Ctrl+D to be the reverse of.
- Ctrl+Shift+F, axJSON, Format. Keep: Ctrl+F is the bundle's find, so no Ctrl key with F is free. F for format.
- Ctrl+Shift+F, axGIT, Fetch. Keep: Ctrl+F is the bundle's find. Fetch reaches the whole repository, a wider form.
- Ctrl+Shift+H, axGIT, Push. Move to Ctrl+P, free: P for push. Ctrl+P is Windows' print, and axGIT prints nothing.
- Ctrl+Shift+K, axCalc, axClaude and axTerminal, Bookmark this line. Move to Ctrl+K, free in all three: it acts on the
  line you are on.
- Ctrl+Shift+L, axGIT, Pull. Move to Ctrl+L, free: the letter it has now, without Shift.
- Ctrl+Shift+M, axClaude, Next permission mode. Move to Ctrl+P, free: P for permission. Ctrl+M there is the music.
- Ctrl+Shift+N, axFile, New folder. Keep: File Explorer's own key for a new folder.
- Ctrl+Shift+P, every window, Command palette. Keep: the command palette's key in Visual Studio Code, and a bundle key.
- Ctrl+Shift+Q, axFile, Add to axMedia's queue. Move to Ctrl+Q, free in axFile: Q for queue, and it acts on the file
  you are on.
- Ctrl+Shift+R, axTerminal, Restart shell. Keep: as in a web browser, Ctrl+Shift+R is the heavier reload. Ctrl+R there
  is the shell's history key.
- Ctrl+Shift+R, axClaude, Force Claude to restart. Keep: the same reason, and the same key in both console windows.
- Ctrl+Shift+S, axClaude, Send queued messages now. Move to Ctrl+Shift+Enter, free in axClaude: another form of
  Ctrl+Enter, which sends. Ctrl+S is the bundle's save, so no Ctrl key with S is free.
- Ctrl+Shift+T, axFile, New text file. Keep: it pairs with File Explorer's Ctrl+Shift+N for a new folder. Ctrl+T is
  File Explorer's new tab.
- Ctrl+Shift+W, axDown, axJSON and axPreview, Word wrap. Keep: W for wrap, and Ctrl+W there is File Explorer.

Collides with:

- Ctrl+P is proposed twice, in axGIT for Push and in axClaude for the permission mode. They are different windows, so
  there is no collision. In each window the key has one meaning.
- axFile's Ctrl+Q fits best if Ctrl+Q's reading keys move to F7 (section Ctrl with a letter). While a notice shows,
  Ctrl+Q reads the notice in every window today. If he keeps Ctrl+Q for reading, keep this one on Ctrl+Shift+Q.
- Ctrl+Shift+Enter is Windows' start as administrator, which axStart has. axClaude starts no program that way.

Count: 7 moves, 10 keeps.

**Settled by the maintainer on 2026-11-26**, through the question tool: axTask's Hide done tasks to Ctrl+H; Bookmark
this line to Ctrl+K in axCalc, axClaude and axTerminal; axClaude's Next permission mode to Ctrl+P and Send queued
messages now to Ctrl+Shift+Enter; axGIT's three remote commands to Ctrl alone, his own choice ("move everything to
ctrl only so pick another letter for f"): Push Ctrl+P, Pull Ctrl+L, Fetch Ctrl+R (R as in reload). The ten keeps
stay. axFile's Add to axMedia's queue waits on Ctrl+Q, in the section Ctrl with a letter.

## Ctrl with a letter

The rule: act on what you are on, with the first letter of the command where it can be. Windows' own keys (copy,
paste, save, find, undo) are not counted.

Three groups need one decision each.

The reading keys. Rule 5 gives saying and showing to the function keys alone. Two new keys are proposed, beside F8,
which says the status:

- F7: say in full what you are on. It takes what Ctrl+Q says in eleven windows today, and Ctrl+Q in every notice and
  form. It would be a bundle key, the same in every window. Free in every window and in the bundle.
- F9: say more about it. It takes Ctrl+I, Information, and the one other key to say something that some windows have.
  It is a key of each window. Free in every window.
- So F7, F8 and F9 are the three keys that say, side by side.
- In axTerminal's screen view, with the keys going to the program, F7 and F9 go to the program, as Ctrl+P does today.
- If he keeps Ctrl+Q for reading, the six Ctrl+Q bullets below become keeps together.

The editors' jump keys. Ctrl with NVDA's browse-mode letter jumps to the next heading, link, list and the rest, and
works while you type. The letter alone, rule 1's key, already jumps in navigation mode. In edit mode a letter types,
and no rule gives a key that jumps while typing. So they are kept. The other way: rule 2 gains a line for them.

The switches. A switch of the window (the music, the pause) is taken as an act on the window (rule 2), so they are
kept on their first letter.

- Ctrl+B, axClaude, What runs in the background. Move to F9, free: it says something. Ctrl+B then is free in axClaude.
- Ctrl+E, axJSON, Error. Keep: a jump key that works while typing.
- Ctrl+G, axDown, axJSON and axPreview, Go to line. Keep: Windows' own key for go to line, as in Notepad.
- Ctrl+G, axFile, Go to folder. Keep: Windows' own key for go to, the same as in the editors.
- Ctrl+H, axDown and axPreview, Heading. Keep: a jump key that works while typing.
- Ctrl+I, axDown and axPreview, List item. Keep: a jump key that works while typing.
- Ctrl+I, axJSON, Item. Keep: a jump key that works while typing.
- Ctrl+I, axFile and axStart, Information. Move to F9, free in both. axMedia has the same command on Ctrl+I in its
  table, and it would move with them; F9 is free there too.
- Ctrl+I, axFile, The favorite's folder. Move to F9, free: the same key as Information, in the Favorites list.
- Ctrl+I, axCalc, Answer in other bases. Move to F9, free: it says more about the answer.
- Ctrl+J, axJSON, Object. Keep: a jump key that works while typing.
- Ctrl+K, axDown and axPreview, Link. Keep: a jump key that works while typing.
- Ctrl+K, axJSON, Key. Keep: a jump key that works while typing.
- Ctrl+L, axDown and axPreview, List. Keep: a jump key that works while typing.
- Ctrl+L, axJSON, Array. Keep: a jump key that works while typing.
- Ctrl+M, axClaude and axTerminal, Background music on or off. Keep: a switch, M for music.
- Ctrl+P, axTerminal, Say current folder. Move to F9, free: it says something.
- Ctrl+P, axClip, Pause recording. Keep: a switch, P for pause.
- Ctrl+Q, axAutomate, axClip, axMCP, axPass, axTask and axTemplate, Read in full. Move to F7, free: it says
  something.
- Ctrl+Q, axDown and axPreview, Block quote. Keep: a jump key that works while typing.
- Ctrl+Q, axProcess, Summary. Move to F7, free.
- Ctrl+Q, axTerminal, What is running. Move to F7, free.
- Ctrl+Q, axMedia, What is playing. Move to F7, free.
- Ctrl+Q, axClaude, Claude's question again. Move to F7, free.
- Ctrl+Q, axCalc, Say the last answer. Move to F7, free.
- Ctrl+R, axTerminal, Recent commands. Keep: the shells' own key for their history.
- Ctrl+R, axAutomate, The last run's report. Move to F9, free: it shows something. Ctrl+R then takes Run (section The
  function keys alone).
- Ctrl+T, axDown and axPreview, Table. Keep: a jump key that works while typing.
- Ctrl+T, axMedia, Go to time. Keep: the media player VLC's own key for going to a time.

Collides with:

- F7 and F9 never meet in one window with two meanings. axCalc gets both: F7 the last answer, F9 other bases. axMedia
  gets both: F7 what is playing, F9 information. axAutomate gets both: F7 read in full, F9 the report.
- axAutomate's Ctrl+R is proposed for Run only after the report leaves it.
- axFile's Ctrl+Shift+Q moves to Ctrl+Q only if the reading keys leave Ctrl+Q.

Count: 12 moves, 17 keeps.

**Settled by the maintainer on 2026-11-26**, through the question tool, with his own order of the three saying keys
("f7 can be the announce, f8 can be read with details, and f9 can be status"):

- F7, a bundle key: read in full what you are on, taking every Ctrl+Q reading above and Ctrl+Q in every notice and
  form. Ctrl+Q is then free for acts: axFile's Add to axMedia's queue moves there from Ctrl+Shift+Q.
- F8, a key of each window: read with details, taking Ctrl+I Information (axFile, axStart, axMedia), the favorite's
  folder, axCalc's other bases, axClaude's Ctrl+B, axTerminal's Ctrl+P (the current folder) and axAutomate's Ctrl+R
  report; axAutomate's Run then takes Ctrl+R.
- F9, a bundle key: say the status, moved from F8.
- The editors' jump keys stay, and rule 2 gains their line: in a text, Ctrl with NVDA's browse-mode letter jumps
  while you type.
- Go to line (axDown, axJSON, axPreview) and Go to folder (axFile) move from Ctrl+G to F12, a function key that shows
  the go-to field; axMedia's Go to time moves from Ctrl+T to F12 too.
- The switches stay: Ctrl+M the music, axClip's Ctrl+P pause.
- axTerminal's Recent commands moves from Ctrl+R to Alt+Down, the command field's choices (rule 4).

## The small families

### Escape

The rule: go back or close.

- Escape, axAutomate, axFile, axGIT, axMCP, axMedia, axPass, axProcess, axTask and axTemplate, Clear the filter. Keep:
  Escape clears a search box, as in Windows.
- Escape, axCalc, Clear the field. Keep: Escape clears a field, as in Windows.

Count: 0 moves, 2 keeps.

### Ctrl with an arrow

The rule: a bigger step.

- Ctrl+Up arrow, axMedia, Faster. Move to Alt+Up arrow, free: Alt with Up changes to the previous choice in place.
  Ctrl+Up then is the previous section, as in the other item windows.
- Ctrl+Down arrow, axMedia, Slower. Move to Alt+Down arrow, free: Alt with Down changes to the next choice in place.
  Ctrl+Down then is the next section.

A note: the speed is a value of what plays, not of the row you are on. The case for keeping: Ctrl+Up and Ctrl+Down
are easy to find while you listen.

Count: 2 moves, 0 keeps.

### Alt with an arrow

The rule: the choices of what you are on.

- Alt+Up arrow, axFile, Up one level. Keep: File Explorer's own key for up one level.

Count: 0 moves, 1 keep.

### Alt with a letter

The rule: open a menu.

- Alt+D, axFile, Go to folder. Keep: File Explorer's own key for its address bar.

Count: 0 moves, 1 keep.

**The small families settled by the maintainer on 2026-11-26**, through the question tool:

- Escape only goes back or closes ("esc should go back or close. please move clearing to another key and make it
  consistent"): clearing the filter in the ten list windows and axCalc's field moves to Ctrl+Delete, in the remove
  family, the same in every window; in those fields Windows' Ctrl+Delete, the next word deleted, gives way.
- axMedia's Faster and Slower move to Alt+Up and Alt+Down; its Ctrl+Up and Down then go between sections, as in the
  other item windows.
- axFile's Alt+Up and Alt+D are removed: Backspace goes up one level, and F12 goes to the folder. No exception is left
  in these families.

## All the families

35 moves, 45 keeps, 80 keys.
