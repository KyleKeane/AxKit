# axTask: actions and forms of a list (specification)

Specified on 2026-11-21 from the maintainer's requests of that day, which section 1 keeps word for word. Not built
yet, and not to be built before what is pending is stable (section 10). Section 9 lists the decisions: four he
took the same day, asked one by one, and four taken in the writing, his to change. The
priorities list's item "axTask: a general framework of buttons and forms in items that build and run a command, a
request or a tool call" points here. Requirement ids are AF-n.m. It grows out of the action keys (`SPEC.md` TK-7a,
`docs/axKit/reports.md` RP-8), which it takes in whole; its first relative is the approval of outside reports
(`docs/axKit/outside-reports.md`, TK-9), which stays code of its own (AF-8.4).

## 1. His words, as sent (2026-11-21)

1. On outside reports, which started it: "please ensure we have differentiated pathways for these external issues and
   my internal development priorities. i need to ensure we have extra security for external issues so nobody can
   submit malicious requests that are automatically executed or impleented. can you setup special code enforced
   processes to ensure that every external report requires a review by you for potential harms and categorizing into
   known issues or adding as a small addition to an existing issue, and ensure that i also must give a manual review
   and approval to any processed external requests."
2. Asked how his approval is proven: "can you hook this to a button in axtask that pops a dialog that requres a
   passphrase that only i know?"
3. "we will want a general framework for adding buttons and forms into an axtask database, so this is just a single
   instance of a larger framework that lets any task include fields of this type so that i have a unified interface
   for managing and approving complex processes. but keep the specification of these forms simple and clean. so that
   a button of form can be linked to a terminal command that is constructed by the form and run in a powershell or
   axterminal. i am thinking we want to keep things simple and easy for you to manage and consistent. i am not sure
   what tools dotnet has for running programs and whether we should be using internal tooling to run commands that are
   built in these axtask action hooks or whether we should support different types of protocols so that i could make
   a orm that sends restful requests or mcp or local code or run in powershell. let me know if this broader feature
   should be built or speced by fable"
4. "oh we should have custom keyboard shortcuts that an axtask item can have a user choose to configure by hooking
   into their buttons."
5. "it would also be nice to the option to route natural language requests through ai tools, but perhaps this is more
   of an axautomate feature than axtask."
6. "please ensure the queued messages i have been sending get recordedinto a record so that we can discuss the more
   general build out of axtask with the user configurable form specifications for an item that can be built out per
   task list in the task list configuration. also please let me know if fable is helpful for all of this"
7. After the specification below was written, the same day: "walk me through the decisions that you want my input
   on. i do want you to ensure that a list specification is json so that i can easily edit it in axjson and that
   should be an option on the context menu of a list. we need to handle edits to the list config in a graveful way
   such as keeping old fields with their values so that they are restored if later enabled again. we should also
   have a bery simple schema for the list config with a simple set of options for the values that axjson will be
   able to load so that editing does not require a lot of memory of field values. much of this might already be
   true; i haven;t configured a list yet because i want you to stabilizze things before i commit things to memory.
   let me know when you think that things are ready for me to work with. i think when we get to that point we will
   quickly get to needing to mature axjson so i do want us to stabilize the other stuff that is pending first."


## 2. What it is, in one paragraph

A list's `list.json` declares **actions**. An action has a name, may have a key, may have a **form** of fields the
user fills in, and does one thing, of a closed list of kinds. It acts on the item selected in the list. The three
kinds axTask has today as action keys are the first three: `set` (write fields), `move` (to another list) and `open`
(a field's address, or an app on the item's file). Two new kinds start a program: `run` (a program with its
arguments, without a window, its result said and its output shown when asked for) and `terminal` (the command line
put into axTerminal's command field, for the user to read there and send himself). What else was named (a web
request, an MCP tool call, a request in words to an AI) is reached through these two in the first build, and gets a
kind of its own when a real use shows that it needs one (section 8). The rules are few and the same in every list,
so that a person and the assistant can both write an action by hand and know what it will do.

## 3. The file

- **AF-3.1 Where.** `"actions"` in a list's `list.json` (`ItemDocument.Extra`, beside `"new item"`): a list of
  objects, in the order the menu shows them. Per list, never per item (message 6). An example:

  ```json
  "actions": [
    { "name": "Answer the report on GitHub",
      "key": "Ctrl+Shift+1",
      "fields": [ { "title": "Answer", "type": "longtext" } ],
      "does": "run",
      "program": "gh",
      "arguments": ["issue", "comment", "{Number}", "--repo", "KyleKeane/AxKit-reports", "--body-file", "-"],
      "input": "{form:Answer}",
      "then": { "set": { "Answered": "{today}" } } }
  ]
  ```

- **AF-3.2 An action's members.** `name` (needed; one of its name in the list; no control character; 60 characters
  at most), `key` (AF-6), `fields` (AF-5), `does` (`set`, `move`, `open`, `run`, `terminal`), and the kind's own:
  - `set`: `"set"`, fields and their values, as today (`ActionKeys.ApplySet`); values may hold placeholders.
  - `move`: `"to"`, a list by a path relative to this one, as today.
  - `open`: `"open"`, a field that holds a web address, or an app's verb, as today (`ActionKeys.OpenRefusal`).
  - `run`: `"program"`, a program's name (found as `SystemPrograms` finds one, never in the list's own folder) or its
    full path; `"arguments"`, a list of texts; `"input"`, a text written to the program's standard input (the place
    for a text of several lines); `"folder"`, where it runs: `list` (the default), or a path; `"seconds"`, how long
    it may take (60 by default, 600 at most); `"then"`, with `"set"`: fields written when the program ended with exit
    code 0, as one undo step.
  - `terminal`: `"command"`, a list of texts, the program first; `"folder"` as for `run`.
- **AF-3.3 The action keys are read on.** `"keys"` (RP-8.1) is still read: the action of digit n is an action with
  the key Ctrl+n. Where both name a key, `"actions"` counts. The first time axTask writes a list's actions, the keys
  are written as actions and `"keys"` is removed, as one undo step.
- **AF-3.4 What is not understood is never run.** An action with a kind this version does not know, a member of the
  wrong type or a missing one is shown in the menu and the form as "<name>, not understood: <why>", does nothing,
  and is kept in the file as it is (IA-4). One bad action leaves the others working.

## 4. Values

- **AF-4.1 Placeholders.** A text of an action may hold names in braces, filled in when the action runs: `{Title}`
  and `{<a field's title>}`, the selected item's field as its text; `{form:<a form field's title>}`, what was typed
  in the form; `{file}`, the item's file with its full path; `{name}`, the file's name without `.json`; `{folder}`,
  the list's folder; `{today}`, the date as 2026-11-21. `{{` and `}}` are braces themselves. A name the item or the
  form does not have refuses the action before anything happens: "Answer the report on GitHub names {Number}, which
  this item does not have".
- **AF-4.2 A value is one argument, never a piece of a command.** In `run`, each element of `arguments` is one
  argument of the program after its placeholders are filled. Nothing is ever joined into a command line by axTask,
  split again, or handed to a shell: a title or a note that holds quotes, an ampersand or a semicolon is that text,
  as one argument. This is the framework's first safety rule, because an item's fields can hold text from outside (a
  report's words, what an AI wrote), and such text must never become a second command.
- **AF-4.3 The program is fixed in the file.** `program`, and the first element of a terminal's `command`, hold no
  placeholder: what an action starts is read in the file, and only what it is given comes from the item.
- **AF-4.4 What a value may not hold.** A control character, a line break included, in an argument or a terminal
  command refuses the action: "The field Notes holds a line break, which an argument cannot; an action gives such a
  text as its input". `input` takes any text. A hidden form field (AF-5.3) may fill `input` alone.
- **AF-4.5 Quoting for the terminal.** A `terminal` action's elements become one command line for the shell the
  axTerminal window runs, each element quoted by that shell's rule, in one tested function per shell
  (`AxKit.Core/ShellQuote`): PowerShell, single quotes with a single quote doubled; cmd.exe, refused when an element
  holds a character cmd.exe would act on (`%`, `!`, `"`, `^`, `&`, `|`, `<`, `>`), as Claude Code's command line is
  (repair R-24). For a shell without a rule the action says so and does nothing.

## 5. Forms

- **AF-5.1 A form before the action.** An action with `fields` opens one form first (the bundle's `FormControls`,
  AX-8.10, as Action keys' own form): a control for each field in order, then a line that says what the action will
  do (AF-7.1), then a button with the action's name and Cancel. Ctrl+Enter runs it; Escape leaves and nothing
  happened.
- **AF-5.2 A field** has a `title`, said as an instruction ("Type the answer": the title is written that way, as
  every field of the bundle is named), a `type` of the item format's own (`text`, `longtext`, `choice` with
  `options`, `date`), a first `value` that may hold placeholders (a field filled from the item, to be changed), and
  `required`, true unless the file says false: an empty needed field keeps the form open and says which.
- **AF-5.3 A hidden field** (`"hidden": true`) is typed unseen and read as protected, never kept, logged or said, and
  never for the AI tools; it may fill a `run` action's `input` alone, since a program's arguments can be read by
  other programs and a terminal's command line is shown.

## 6. Keys

- **AF-6.1 Eighteen keys, and no other.** An action's key is Ctrl+1 to Ctrl+9 or Ctrl+Shift+1 to Ctrl+Shift+9. No
  action can therefore take a key of the window or the bundle (AX-8.4), by construction; `AxTaskKeys` holds the
  eighteen, and `KeyTests` the collisions. At the top level Ctrl+1 to Ctrl+3 and their Shift forms stay the heading
  keys: actions act in a list.
- **AF-6.2 Every action is in the menu.** Context, Actions of this list, lists each action by its name with its key,
  so that the command palette and F1 hold them; an action without a key is run from there.
- **AF-6.3 The user gives the key** in the list's configuration (section 11), as one of eighteen options. Where two
  actions name one key, the first in the file has it, and the second is shown as "<name>, not understood: Ctrl+1 is
  <the first's name>'s" and still runs from the menu.

## 7. What is said, asked and trusted

- **AF-7.1 One sentence says what an action does**, made from the file alone (`ListAction.Described`): "runs gh with
  7 arguments: issue, comment, {Number}, --repo, KyleKeane/AxKit-reports, --body-file, -; gives it {form:Answer} as
  input; then sets Answered to {today}". The form's line, the menu's detail and the question of AF-7.2 all use it.
- **AF-7.2 An action that starts a program is asked about before its first run.** A `set` is one undo step and is
  never asked about. `move` and `open` are asked about as today (TK-7a.3: said at the first press, done at the
  second). `run` and `terminal` open a notice before the first run on this computer, and again whenever anything of
  the action changed in the file: its title "Run <name>?"; its text, where it comes from ("This action is in the list's
  configuration file. axKit did not write it and checked nothing of it; you, another program or an AI may have
  written it."), the sentence of AF-7.1, the
  program as it was found with its full path, and every argument as it is filled for the item selected, one on a
  line, with hidden characters written out (`DisplayText.Visible`); "axKit does not know what this program does or
  reaches". The answers: Cancel, where Enter is; Run it once; Run it, and do not ask again for this action. The
  acceptance is the bundle's seal over the list's folder and the whole action as the file holds it
  (`TaskSettings.AcceptedActionKeys`, grown), so that a changed argument asks again and a line written into the
  settings accepts nothing.
- **AF-7.3 Every run is heard.** "Running <name>" as it starts; at its end "<name>: done" (with the fields a `then`
  set, "Answered: 2026-11-21") or "<name> failed, exit code 1", with the sound of a failure (`EventKind.Failed`). Context,
  Actions of this list, Read the last output shows what the program wrote, both streams, the last 64 KB, as a
  read-only notice; a failure opens it by itself. Nothing of the output is logged.
- **AF-7.4 A terminal action never sends.** axTerminal opens in the folder with the command line in its command
  field. The user reads it there, changes it if he likes, and presses Enter himself. axTask says "<name> is in
  axTerminal's command field". Only in a folder on a drive of this computer (S-5), as `open` with `terminal` today.
- **AF-7.5 The AI tools.** A `run` or `terminal` action is marked act: an AI tool's request to run it asks the user
  first, as every act command does (`CommandGate`, mcp.md MC-7.3), with the question of AF-7.2 whatever was accepted
  before. An action with a hidden field is never offered to the AI tools.
- **AF-7.6 One at a time, never on the window's thread.** A program runs off the UI thread (bundle rule 3) in a job
  that ends with the window (`AxKit.Core/ChildJob`), one action at a time in a window; the status bar says "Running
  <name>". Actions, Stop the running action ends it with what it started; its time limit does the same and says so.
- **AF-7.7 Outside calls.** `run` is a program start and `terminal` hands a line to axTerminal: each carries its
  `Outside call` comment and its place on the outside-calls page (B-17). axTask's own code still makes no network
  request (TK-7a.2 stands): what a program of the user's choosing reaches is that program's doing, and the question
  of AF-7.2 says that axKit does not know it. `PRIVACY.md`, `SECURITY.md` and axTask's guide say so in the commit
  that builds it.

## 8. What the first build leaves out, and how it is reached meanwhile

- **AF-8.1 A web request.** Through `run`: `curl.exe`, which Windows includes, or a program made for the service
  (`gh api`). A kind of axTask's own that sends the request with axKit's code would end "axTask is local only"
  (TK-7a.2, the maintainer's decision of 2026-11-14) and is not proposed.
- **AF-8.2 An MCP tool call.** No kind. axKit's MCP pieces let an AI tool into axKit's windows; a call outward to a
  server would be a new protocol client. It waits for a use that `run` cannot serve.
- **AF-8.3 A request in words to an AI.** Not axTask's (message 5; root rule 10: one shared AI service that no
  app's feature depends on). When it comes, it is a kind `ask` that hands the filled text to AI help, with
  `AiCaution`'s words and AI help's own consent, and what the AI proposes is never run by the action.
- **AF-8.4 The approval of outside reports stays code.** Approve this report and Decline this report sign with a key
  and a passphrase; nothing of that may be read from a list's file. They are listed with the list's actions in
  Context, so that the user finds one place, and are no actions of the file.
- **AF-8.5 An axAutomate workflow as an action.** Later, a kind `workflow` that starts one in axAutomate.

## 9. The decisions

Decided by the maintainer on 2026-11-21, asked one by one after the specification was written:

1. **A list's configuration decides which fields its items show: hide, keep, restore** (section 11). A field the
   configuration no longer names is hidden, kept in each file with its value, and shown again when it is named
   again. Until then a configuration shaped new items only.
2. **Eighteen fixed keys** (AF-6.1), not any key combination: no collision can be made, no key of the bundle can be
   lost to a list's file, and the choice is a short list of options.
3. **A program is asked about once, and again after a change** (AF-7.2), not at every run.
4. **`run` and `terminal` first** (five kinds in all); a web request goes through a program the action names, so
   that axTask's own code still makes no network request (AF-8.1); a kind for a protocol comes later, each by his
   decision.

Taken in writing this, his to change:

5. **Per list, in `list.json`** (his message 6), never per item: every item of a list has the same actions.
6. **Arguments, not command text** (AF-4.2). It costs the convenience of typing a command line as one text, and
   buys that no item's content can ever change what runs. A command line as text is what `terminal` is for, where
   the user reads it before he sends it.
7. **The approval stays code** (AF-8.4).
8. **The name is "actions"**; the action keys become actions with a key, and their files keep working (AF-3.3).

## 10. Build order

Not before what is pending is stable (his word in message 7): the checks of the audit's repairs with a live Claude
Code, the NVDA pass of the windows that changed, and the open items of axClaude. Then, in this order, each step
usable by itself:

1. The configuration as a file a person edits (section 11): the fields a list shows, hide, keep and restore; a
   configuration that does not read; Edit the list's configuration in the Context menu. `AxTask.Core`, the item
   window's detail pane, their tests and a window script. This is useful before any action exists, and it is what
   makes a list safe to configure by hand.
2. `AxTask.Core`: `ListActions` (the file read and written, `"keys"` read on), `ActionValues` (the placeholders and
   their refusals), the acceptance over a whole action; `AxKit.Core/ShellQuote`. All pure, each with its tests.
3. The window: Context, Actions of this list; the eighteen keys; the form of an action's fields; the question of
   AF-7.2; the run off the UI thread with its words, its output and its stop; the hand-over to axTerminal.
4. `SPEC.md` section 7a rewritten for actions, the user guide, the NVDA plan, the outside-calls page, `PRIVACY.md`
   and `SECURITY.md`; window scripts; an NVDA pass. From here a list can be configured and worked with.
5. axJSON learns the options (section 12): the table of the configuration's members and their values, and
   axJSON's two commands that offer them. With axJSON's own maturing, which the maintainer expects to follow.
6. The first uses, in the management lists: Answer the report on GitHub in the outside reports list (after an entry
   is Done), and what the maintainer asks for next.

There is no form of axTask's own for making an action: the configuration is the file, edited in axJSON (his message
7). Action keys' form of today (Actions, Action keys) stays for the three kinds it knows.

## 11. The configuration is a file a person edits

- **AF-11.1 One JSON file, opened from the list.** A list's configuration is its `list.json`, plain JSON, and
  nothing of a list is configured anywhere else. Context, on a list: **Edit the list's configuration** (Ctrl+J, the
  item "Open in axJSON" of today, named for what it is on a list) opens it in axJSON. A list that has no file yet
  gets one written first, with its fields as they are and an empty list of actions, so that there is something to
  edit and its members are there to be filled in. axTask reads the file again when its window comes to the front,
  as it reads every file, and says what changed in one line ("The list's configuration changed: 2 actions, 5
  fields").
- **AF-11.2 The members**, all there are:
  - `"format"`: `axkit-list 1`.
  - `"fields"`: the list's own details (Notes, Section), as today.
  - `"new item"`: **the fields of the list's items**, in the order they are shown; each a `title`, a `type` and the
    `value` a new item starts with, a choice with its `options`. The member keeps its name, so that every file
    written until now reads as it did.
  - `"actions"`: section 3. `"keys"`: read on (AF-3.3).
  - Anything else is kept as it is and means nothing (IA-4), as today.
- **AF-11.3 Hide, keep, restore** (the maintainer's decision of 2026-11-21). An item of a list shows its Title and
  then the fields the list's configuration names, in the configuration's order. A field the configuration names and
  the item's file does not hold is shown empty, and is written into the file when it is first given a value. A
  field the file holds and the configuration does not name is **kept**: it stays in the file with its value, is
  not shown among the fields, and is shown again, with the value it had, as soon as the configuration names it
  again. Nothing a change of the configuration does ever removes or rewrites a value in an item's file.
- **AF-11.4 What is kept is said.** The detail pane's last line of such an item is "2 more fields kept: Sprint,
  Old priority"; Enter on it reads them with their values, read-only. So a hidden value is never a surprise, and a
  field renamed in the configuration (a new name is a new field) is seen at once: the old name stands among the
  kept ones.
- **AF-11.5 A list without named fields shows every field**, as all lists did until now: a folder with no
  `list.json`, or one whose `"new item"` is empty. So do the gathering lists (All tasks, the date lists, a search),
  item by item, each by its own list's configuration.
- **AF-11.6 A type that changed.** A value the field's new type cannot hold is shown as its text with "kept as
  text: the field is now a date", is not changed, and is replaced only when the user gives the field a new value.
- **AF-11.7 A configuration that does not read** (a comma too many after an edit) changes nothing in the folder:
  the list is shown as one without a configuration (every field, no actions), its row and its detail pane say "The
  list's configuration does not read: <what>, line 12. Ctrl+J opens it", and axTask writes nothing into that file
  until it reads again. A member of the wrong kind (actions that are no list) is passed over in the same way, with
  its own sentence, and the rest of the file counts.
- **AF-11.8 axPass is left as it is**: its entries are KeePassXC's and have no list configuration. The item
  window's shared code learns a list's field order from the app (`ItemForm`), and only axTask gives one.

## 12. The options, for axJSON

- **AF-12.1 A small table, written once.** Every member of the configuration and the values it can take are one
  table in code (`AxTask.Core/ListConfigOptions`), from which axTask reads a configuration and by which a test
  holds this document's lists. Where a value is one of a few, the table names them: a field's `type` (`text`,
  `longtext`, `date`, `datetime`, `check`, `choice`, `link`), an action's `does` (`set`, `move`, `open`, `run`,
  `terminal`), its `key` (the eighteen), `folder` (`list`, or a path), `required` and `hidden` (`true`, `false`).
- **AF-12.2 As a file axJSON loads.** The table is written out as one small JSON file of axKit's own shape,
  `axkit-list 1.options.json`, shipped with axKit: for each kind of object (the configuration, a field, an action,
  a form's field) its members in order, each with a line that says what it is and, where it has them, its values.
  It is not JSON Schema, which stays a non-goal of axJSON (its SPEC, G3): a list of names and choices, nothing that
  validates.
- **AF-12.3 What axJSON does with it** (built with axJSON's maturing, step 5 of the build order). For a file whose
  `"format"` axJSON has such a table for: at a value that has choices, a command lists them and writes the one
  chosen; in an object, a command lists the members it can have and does not have yet, with what each is, and adds
  the one chosen with a first value. So a configuration is written by choosing, and nothing has to be remembered.
  Until then axJSON edits the file as any JSON, and the file axTask writes for a new configuration (AF-11.1) holds
  every member with a first value, which is the next best thing to a list of choices.
