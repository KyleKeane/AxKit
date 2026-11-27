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
8. "ok and i am thinking we should make the item details for a task into a standard form that opens on enter. i
   want us to have tree and form navigation; i do not think there is anything else needed, but maybe i am missing
   something perhaps the single text rename and input prompts are something different. but i am thinking the details
   of a list are complex enough that we will want basic doem controls like radio buttons and date input that will
   require the more complex keyboard controls that would overlap with tree nac like arrows.  we might add tabular
   nav at some point or even graph navigation when those become essential, but for now let us try to fit everything
   we can into these simpler data structures and interaction models. this would be true across the axkit suite.
   within axtask and other apps whenever possible and meaningful i want any user configurable form to be a simple
   json config as a template and json for the actual data. so that any item has an inheritence from a schema config
   and then has its actaul values. this should allow us to constantly allow quick and easy reconfiguration and user
   configurable elements. honestly json is not the only text based key balue store and i am open to using something
   like yaml instead so that the underlying text document is easier to read as raw text, but i think any reliance
   on tabs for semantic grouping will be very tricky; but please do let me know if json is not the best option
   here."
9. Asked whether an item's file should become flat, as he had agreed on 2026-11-10: "yes you should have an item
   just be the key balue pairs for any fields. it should always hold a link to the schema that is meant to be
   interpreted through. this is definitely the most efficient method and keeps item files very simple, but will
   have implications for ensuring that a list of items is interpreted by the right schema. m but i think the
   alternative of having every field in an item have its own type and control and other things would be  a nearly
   impossible task . do you agree? if so do it this way. we should just convert any items or configs that are not
   updated into the new schema and format so that we do not try to maintain code to render both, just detect if it
   does not match and then offer a conversion. then all the converter code is modular and separated from the core
   code of the app"
10. "we will want to eventually have axjson be able to read a linked json that has allowed options and allow for a
    simple form editing view that takes the place of the text editor mode as an option or let's the user cycle
    options or open pickers from the text view; i am not sure how the simplest way to handle this will be, but it
    will be really nive to have simple json hold the options and field types and control types."


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
- **AF-3.3 The action keys become actions.** The `"keys"` of a configuration written before this (RP-8.1) are made
  actions by the conversion (AF-13.6): the action of digit n is an action with the key Ctrl+n. The app reads
  `"actions"` alone.
- **AF-3.4 What is not understood is never run.** An action with a kind this version does not know, a member of the
  wrong type or a missing one is shown in the menu and the form as "<name>, not understood: <why>", does nothing,
  and is kept in the file as it is (IA-4). One bad action leaves the others working.

## 4. Values

- **AF-4.1 Placeholders.** A text of an action may hold names in braces, filled in when the action runs: `{Title}`
  and `{<a field's title>}`, the selected item's field as its text; `{form:<a form field's title>}`, what was typed
  in the form; `{file}`, the item's file with its full path; `{name}`, the file's name without `.json`; `{folder}`,
  the list's folder; `{today}`, the date as 2026-11-21. `{{` and `}}` are braces themselves. These four names are written in lower case; `{field:<title>}` says an item's field whatever its title, for a field titled like one of them. A field of the list that the item's file does not hold yet is empty, not missing. A name the item or the
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
  (repair R-24). For a shell without a rule the action says so and does nothing. For both PowerShells three kinds
  of element are refused too, with the reason (2026-11-27, found by running the line through powershell.exe): one
  that holds a double quote, an empty one, and one with a space in it and a backslash at its end. The single
  quotes are how PowerShell reads the line; Windows PowerShell 5.1 then hands such an element to the program in
  pieces, joined with the next, or not at all, while PowerShell 7.3 hands it on whole, so no one line is right
  for both.

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
  actions name one key, the first in the file has it, and the second (as an action whose key is none of the eighteen) is shown as "<name>, not understood: Ctrl+1 is
  <the first's name>'s" and still runs from the menu.

## 7. What is said, asked and trusted

- **AF-7.1 One sentence says what an action does**, made from the file alone (`ListAction.Described`): "runs gh with
  7 arguments: issue, comment, {Number}, --repo, KyleKeane/AxKit-reports, --body-file, -; gives it {form:Answer} as
  input; then sets Answered: {today}" (the words a set has in the action keys, "Field: value"). The form's line, the menu's detail and the question of AF-7.2 all use it.
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
5. **An item's file is its names and values, and a link to the configuration it is read through** (section 13;
   his message 9, and his agreement of 2026-11-10): the types, the controls and everything else of a field are
   written once, in the list's configuration.
6. **One format, and a conversion** (his message 9): the app reads the one shape. A file of the earlier shape is
   recognised and its conversion offered; the converter is a module of its own, apart from the app's core.
7. **An item's details open as a standard form on Enter** (section 14; his message 8), and the bundle keeps to few
   ways of moving: a tree, a form, a text; a table or a graph only when one becomes essential.
8. **JSON stays** (his question in message 8; section 14).

Taken in writing this, his to change:

9. **Per list, in `list.json`** (his message 6), never per item: every item of a list has the same actions.
10. **Arguments, not command text** (AF-4.2). It costs the convenience of typing a command line as one text, and
    buys that no item's content can ever change what runs. A command line as text is what `terminal` is for, where
    the user reads it before he sends it.
11. **The approval stays code** (AF-8.4).
12. **The name is "actions"**; the action keys become actions with a key in the conversion (AF-3.3).

## 10. Build order

Not before what is pending is stable (his word in message 7): the checks of the audit's repairs with a live Claude
Code, the NVDA pass of the windows that changed, and the open items of axClaude. Then, in this order, each step
usable by itself:

1. The flat item and the configuration that is its schema (sections 11 and 13): the one format the app reads; hide,
   keep and restore; a configuration that does not read; Edit the list's configuration in the Context menu; the
   converter as a module of its own, and the maintainer's lists and every script of `tools` moved with it.
   `AxTask.Core`, the item window's detail pane, their tests and window scripts. This is useful before any action
   exists, and it is what makes a list safe to configure by hand: **from here the maintainer can configure a list's
   fields.** *Built on 2026-11-21 (the branch `flat-items`); what the building changed in sections 11 and 13 is said
   there in italics or as "changed while building". An NVDA pass of axTask's window is owed for it.*
2. The item's form on Enter (section 14). *Built on 2026-11-21: `AxTask.Core/ItemFormRules`, `TaskForm.ItemForm.cs`, the window script `18-item-form.txt`; axTask's SPEC TK-4.0a says it as built.*
3. *(Built on 2026-11-21 as pure code with its tests, used by no window yet.)* `AxTask.Core`: `ListActions`, `ActionValues` (the placeholders and their refusals), the acceptance over a whole
   action; `AxKit.Core/ShellQuote`. All pure, each with its tests.
4. *(Built on 2026-11-21: `TaskForm.Actions.cs`, the window script `19-list-actions.txt`; axTask's SPEC TK-7a.1a says it as built. The hand-over of a `terminal` action is axTerminal's `--command` option, its SPEC AT-1.5a: axTask passes the command's parts as arguments and axTerminal, which knows its shell, makes and quotes the line, so that axTask never joins one.)* The window: Context, Actions of this list; the eighteen keys; the form of an action's fields; the question of
   AF-7.2; the run off the UI thread with its words, its output and its stop; the hand-over to axTerminal.
5. *(Done on 2026-11-21 with steps 4 and 6, each in its own commit; the NVDA pass of axTask, axTerminal and axJSON
   at 31fe68c1 is in `docs/axKit/nvda-test-record.md`, with what is owed to the maintainer's own ear.)* `SPEC.md` rewritten for all of it, the user guide, the NVDA plan, the outside-calls page, `PRIVACY.md` and
   `SECURITY.md`; window scripts; an NVDA pass. **From here a list's actions can be configured and worked with.**
6. *(Built on 2026-11-21: the table is `AxKit.Core/ListOptions`, the file `docs/axKit/axkit-list 2.options.json`,
   axJSON's commands on Alt with an arrow (Alt+Right the list of values, Alt+Down and Alt+Up the next and the previous value in line, Alt+Left add a member; his keys, on hearing the first ones), its SPEC AJ-5; what changed
   while building is said in section 12 in italics.)* axJSON learns the options (section 12): the table of the configuration's members and their values, and
   axJSON's two commands that offer them. With axJSON's own maturing, which the maintainer expects to follow.
7. The first uses, in the management lists: Answer the report on GitHub in the outside reports list (after an entry
   is Done), and what the maintainer asks for next. *(2026-11-21: a first use is in the maintainer's own priorities
   list, four actions without a key, so that no key of his changed: Start today, Priority high, Priority normal and
   Priority low, in Context, Actions of this list; a key is one member more, `"key": "Ctrl+1"`, added in axJSON with
   Alt+Left; asked the same day, he keeps them without keys for now. Answer the report on GitHub was first only
   proposed, since it posts to GitHub and the outside reports list is the one his signature governs; he said yes
   the same day, and it is in: an entry has the field `Issue`, the number of its report's issue (the first when it
   names several; a field no approval signs), written by `tools/outside-report.ps1` with the entry and given to
   the four entries there were, a date field `Answered`, and the list the action
   `{ "name": "Answer the report on GitHub", "does": "run", "fields": [ { "title": "Type your answer", "type": "longtext" } ], "program": "gh", "arguments": ["issue", "comment", "{Issue}", "--repo", "KyleKeane/AxKit-reports", "--body-file", "-"], "input": "{form:Type your answer}", "then": { "set": { "Answered": "{today}" } } }`:
   nothing of the report's own words is an argument, the answer is what he types, and the first run asks him with
   every argument written out.)*

There is no form of axTask's own for making an action: the configuration is the file, edited in axJSON (his message
7). Action keys' form of today (Actions, Action keys) goes with the action keys, which the conversion makes actions.

## 11. The configuration is a file a person edits

- **AF-11.1 One JSON file, opened from the list.** A list's configuration is its `list.json`, plain JSON, and
  nothing of a list is configured anywhere else. Context, on a list: **Edit the list's configuration** (Ctrl+J, the
  item "Open in axJSON" of today, named for what it is on a list) opens it in axJSON. A list that has no file yet
  gets one written first, with its fields as they are and an empty list of actions, so that there is something to
  edit and its members are there to be filled in. When axTask's window comes back to the front, it looks at the
  configuration it opened this way; when the file changed, it reads the panes again and says what the file holds
  now in one line ("The list's configuration changed: 5 fields, 2 actions", with what its reader passed over; or
  that it does not read, AF-11.7). *As built on 2026-11-21: "its fields as they are" are the fields the list's items
  hold, each once, typed by JSON's own kind of the first value (true or false a check, a number a number, a text of
  several lines a long text), or Done, Start and Due for a folder without an item; the same file is written, in the
  same step, by the first item added to such a folder (`ItemFolder.StarterConfig`).*
- **AF-11.2 The members**, all there are:
  - `"format"`: `axkit-list 2` (section 13: the shape before it is converted, never read beside this one).
  - `"details"`: the list's own values, by name: `"Notes"`, `"Section"`.
  - `"item fields"`: **the fields of the list's items**, in the order they are shown; each a `title`, a `type` and
    the `value` a new item starts with, a choice with its `options` (`"new item"` until the conversion).
  - `"actions"`: section 3 (the `"keys"` of the action keys become actions in the conversion, AF-13.6).
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
- **AF-11.5 One rule for every list** (changed while building, 2026-11-21; the draft had "a list without named
  fields shows every field"). A list whose configuration names no field shows its items' Titles alone, and what an
  item holds besides is kept and named in its last line, as in any list. The exception of the draft would have made
  the last field one takes out of a list the one that brings every hidden value back into view (take Done out of a
  list that has only Done, and Done shows again, as a field of no list); one rule is simpler to hold in the head.
  A folder with no `list.json` at all is AF-13.4's case: every value as text, nothing changed, until the
  configuration is written (AF-11.1). The gathering lists (All tasks, the date lists, a search) read each item
  through its own list's configuration.
- **AF-11.6 A type that changed.** A value the field's new type cannot hold is shown as its text with "kept as
  text: the field is now a date", is not changed, and is replaced only when the user gives the field a new value.
- **AF-11.7 A configuration that does not read** (a comma too many after an edit) changes nothing in the folder:
  the list is shown as one without a configuration (every value of an item as text, no actions; AF-13.4), its row
  says "its configuration does not read", the level and the detail pane say "The list's configuration does not
  read: it is not JSON at line 12. Ctrl+J opens it", and axTask writes nothing in that list until the file reads
  again: no item changed, none added, no detail set. A name written twice in one object, anywhere in the file, is
  such a file too: "does not read: it names a member twice" (2026-11-27; an item's file with a name twice is a
  damaged item, SPEC.md TK-4.6; both are read through `AxKit.Core/JsonText`, since .NET's own reader lets such a
  file through and fails later, where nothing caught it, so that the list did not fill at all). A member of the wrong kind (actions that are no list, an item
  field without a title, two fields of one title, a field named Title or schema, a type this version does not know,
  which is read as text) is passed over with its own sentence in the list's details and in the line said on coming
  back from axJSON, and the rest of the file counts.
- **AF-11.8 axPass is left as it is**: its entries are KeePassXC's and have no list configuration. The item
  window's shared code learns a list's field order from the app (`ItemForm`), and only axTask gives one.

## 12. The options, for axJSON

- **AF-12.1 A small table, written once.** Every member of the configuration and the values it can take are one
  table in code (`AxTask.Core/ListConfigOptions`), from which axTask reads a configuration and by which a test
  holds this document's lists. Where a value is one of a few, the table names them: a field's `type` (`text`,
  `longtext`, `number`, `date`, `datetime`, `check`, `choice`, `link`: the item format's own, `FieldTypes.All`), an action's `does` (`set`, `move`, `open`, `run`,
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
- **AF-12.4 One link, followed by both apps** (his message 10: "a linked json that has allowed options"). The link
  is the one an item already carries: axJSON, opening a file with a `"schema"` member, reads the file it names and
  knows each member's type and options from it, as axTask does. For an item that is its list's configuration; for
  a configuration it is the options table of AF-12.2, which the configuration names the same way. So the options,
  the field types and the kinds of control are held in plain JSON, in one place, for every program that reads the
  data.
- **AF-12.5 How axJSON offers them: the proposal, to be settled when axJSON is taken up.** His message names three
  ways and leaves the choice open. Proposed, simplest first: (1) in the text, on a value that has options, a key
  steps to the next and the previous option, as Right and Left do on a choice in axTask's fields, and a command
  opens the list of them; a date takes `today` and `+3`. This needs no new view and is where to begin. (2) A form
  view of the whole file in place of the text, one command to go there and back: the same form axTask opens on
  Enter (AF-14.1), built from the same linked file by the same shared code, so that it is written once. The text
  stays the truth, and the form is another way to change it.
- **AF-12.6 As built on 2026-11-21** (axJSON's SPEC AJ-5 says it in full). *The table is `AxKit.Core/ListOptions`,
  not a class of axTask: axJSON reads it, and an app never references another; axTask's test holds the actions'
  side of it to the actions' reader, and the readers themselves were left as they are. The file is
  `docs/axKit/axkit-list 2.options.json`, named for the format as it is now, written from the table and held by a
  test; it is in the repository for other programs, and axJSON uses the table in code. A configuration is known by
  its `"format"`, and names no options file itself: a file shipped with axKit has no place a `list.json` could link
  to. An item is known by its `"schema"` link, which axJSON follows to the configuration beside it, on this
  computer only. Of AF-12.5's ways, the list and the step in line are built (Alt+Right and Alt+Down, Alt+Up on a value; Alt+Left for an object's missing
  members: his keys, chosen on hearing the first ones); a date's `today` and `+3` and the form view are not, and
  stay his to settle.*


## 13. An item is its values, read through its list's configuration

The maintainer's decision (messages 8 and 9; agreed in principle on 2026-11-10 and not built then). Giving every
field of every item its own type, control and options, as the files do now, is what he calls a nearly impossible
task to keep right, and it is: the same fact written in a thousand files cannot be changed in one place.

- **AF-13.1 The file.** An item is one JSON object of names and values, and one link:

  ```json
  {
    "schema": "list.json",
    "Title": "Fix the tap",
    "Done": false,
    "Due": "2026-11-30",
    "Notes": "Under the sink.\nBring the wrench."
  }
  ```

  Title first, then the configuration's fields in its order, then what is kept (AF-11.3), one member a line. A
  value is JSON's own: a text, `true` or `false` for a check, a date as `2026-11-30`, a choice as its option's text,
  a link as a path relative to the file.
- **AF-13.2 The link.** `"schema"` is always there: the path, relative to the item's file, of the configuration it
  is read through. For an item of a list that is `list.json`, its folder's own. It is the one name a field cannot
  have. An item moved to another list keeps the same words and is therefore read through its new list's
  configuration; what that one does not name is kept, and comes back if the item returns (AF-11.3). A file in the
  item's own folder is read wherever that folder is, a share or a mapped network drive included, since the folder
  was read to get the item; a link that leaves the folder (`../Shared/list.json`) is followed only on a drive of
  this computer, the rule for links between items (S-5), and never when it is a whole path
  (`ItemFolder.ConfigurationFile`, tested; until 2026-11-27 the own folder's file went by that rule too, and every
  item of a folder of lists kept on a server, Documents redirected to a share, was shown as text that nothing
  could change).
- **AF-13.3 Everything of a field but its value is the configuration's**: its type, its options, the value a new
  item starts with, its place in the order. A change there holds for every item at once, and no item's file is
  touched by it.
- **AF-13.4 Read through the right configuration, or not at all.** An item whose `"schema"` names a file that is
  not there or does not read is shown with its names and values as plain text, read-only, with one line that says
  why ("Its configuration, list.json, is not there"); nothing is guessed from the values, and nothing is written.
  The gathering lists read each item through its own list's configuration (AF-11.5). An item's kind of file is
  known by the `"schema"` member, as it was by `"format"`.
- **AF-13.5 One format in the app.** axTask reads and writes this shape and no other. There is no second reader
  kept beside it.
- **AF-13.6 The conversion.** A list in the earlier shape (`"format": "axkit-list 1"`, items with `title`, `type`
  and `value` objects, `"new item"`, `"keys"`) is recognised when it is met: its row says "earlier format: Enter
  converts it", and opening it asks once, "This list is written in axTask's earlier format, and is not opened until
  it is converted. Convert it? A copy is kept among the Archived lists", with what changes, that Ctrl+Z takes it
  back, and that a copy of axKit older than this one does not read a converted list. With more than one such list in
  the folder of lists, the question has a third answer, "Convert all N lists" (added while building: ten lists should
  not be ten questions). Convert copies the folder's files into the Archive as `<name>, before conversion` (the
  Archive and not beside the list, where the copy would itself be a list of the earlier format among the user's
  own; with several lists, every list is planned before the first copy is made, and a conversion that cannot be
  prepared or written leaves no copy behind, `ListConversion.Prepare`, 2026-11-27), then writes the configuration and every item anew, as one step that Ctrl+Z takes back
  (whole or not at all: a file that cannot be written takes the ones written before it back, `Changes.Apply`):
  the fields of `"new item"` and of the items themselves become `"item fields"` (the configuration's own first; a
  field only some items hold is added after them, so that nothing is hidden by the conversion), every item becomes
  its values, the list's `"fields"` become `"details"`, and `"keys"` become actions. A list not converted is not
  opened, and the gathering lists leave it out and say so in their name ("All tasks, 3 items, 1 list in the earlier
  format is not read: Old. Open one to convert it"). The converter is a module of its own (`AxKit.Core/Conversion`,
  a folder and a namespace apart, with its own tests and the earlier shape's reader inside it: `EarlierFormat`,
  texts in and texts out, and `ListConversion`, the folder's side): the apps' code calls it where a list is met and
  knows nothing of the earlier shape. It sits in the shared code, beside the item format, because two things meet
  earlier lists: axTask, and the report form's own list (`reports.md` RP-5.1), which is converted without a question
  the first time the form or the sender meets it, a copy kept beside it, since a report that waits there must still
  be sent. A later change of format gets a converter of its own beside it, and the app still reads one. Nothing is
  dropped by it: a name that cannot stay (a field called `schema`, a second field of a title, a field without one)
  is kept under another name, which is a field of the list too, and said; a file that is neither of the earlier
  shape nor an item of the shape now is left as it is, and said; an item already of the shape now is left without a
  word, and a list half converted (one an earlier version of axKit wrote an item into) is finished the same way.
  *Built on 2026-11-21, with the window: `ListConfig`, `FlatItem`, `ItemView`, `Conversion`, and in axTask
  `ItemFolder`, `TaskForm.Conversion.cs`; window scripts 12, 16 and 17 of `tests/scripts/axTask`.*
- **AF-13.7 What moves with it**, in the same step: the scripts of `tools` that read and write the lists
  (`items.ps1`, `item.ps1`, `approvals.ps1`, `outside-report.ps1`, `reports-check.ps1`), the AI tools of axTask
  (`TaskTools`), the smart lists' conditions, the links between items, and the maintainer's own lists, converted
  once with the same converter and committed. An approval's signature is over a report's values, not over the
  file's shape (OR-5.3), so the approvals given stand.
- **AF-13.8 axPass is not touched**: its entries are KeePassXC's. The shared item window (`ItemForm`) is handed
  fields and values by each app as before.

## 14. The item's form, and the few ways of moving

- **AF-14.1 Enter on an item opens its form** (his message 8): one standard control for each field of the list's
  configuration, in its order, by the bundle's rules for forms (bundle SPEC AX-8.10): the focus in the first
  field, Tab and Shift+Tab between the controls, each read by NVDA as what it is, Ctrl+Enter saves the whole form
  as one undo step, Escape leaves and nothing is changed. By type: a text box; a box of several lines; a check
  box; a date as a text box that takes a date, `today` or `+3` and says the day back; a choice as a list of its
  options. A choice is a list and not a group of radio buttons, by the maintainer's own earlier decision: in a
  radio group the arrows choose as they move, in a list they explore and nothing is chosen until one leaves it
  (AX-8.10 rule 2). The fields that are kept and not shown are named in the form's last line.
- **AF-14.2 The detail pane stays for reading.** Tab from the list still reaches the item's fields as lines, read
  without opening anything, with the one-key changes it has (Right and Left on a choice or a check, Space on a
  check). Enter on one of its lines opens the same form with the focus on that field. The separate notice for each
  field's change goes: one form in its place.
- **AF-14.3 The few ways of moving, in every app** (the direction, his message 8; to be written into the bundle's
  SPEC when the first app follows it). A window is made of three things and no more:
  - a **tree**: lines one moves through with the arrows, Right into a level and Left out of it (the navigators,
    the lists of every app);
  - a **form**: standard controls with Tab between them, where the arrows belong to the control one is in, which is
    why a form is its own place and never part of a tree. A question that takes one text (a new name, words to
    find, a passphrase) is a form of one field, with the same keys, and is not a third thing;
  - a **text**: read and written with the caret (the editors, a conversation, the text of a notice). This is the
    one his message does not name, and axKit has had it from the start.
  A table and a graph have ways of moving of their own, and come when something cannot be done without them.
- **AF-14.4 A form the user can change is a template and its values, both JSON**, wherever it is possible and has
  a point: a list's items (section 13), an action's form (section 5), and what comes later. One shape for a field
  everywhere: `title`, `type`, `value`, `options`, `required`, `hidden`.
- **AF-14.5 JSON, not YAML** (his question). JSON stays, for four reasons. Its structure is said by characters that
  are there, braces and brackets, while YAML's is said by how far a line is indented, which a screen reader does
  not speak unless asked and which a single space changes without a sound: his own worry, and the deciding one. A
  JSON file that is wrong does not read, and says where; a YAML file that is indented wrongly reads, as something
  else. YAML guesses types (`no` is false, `1.10` is a number, a date becomes a date), and JSON never does. And
  .NET reads JSON by itself, while a YAML reader would be a package, which the bundle does not take, or a parser
  of our own. What made the files hard to read was not JSON but their shape: three members for every field. The
  flat item of section 13 reads as a list of names and values, nearly what YAML would give, without its risks; and
  axJSON is the tool that moves through it by structure.
