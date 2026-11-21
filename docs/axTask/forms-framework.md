# axTask: actions and forms of a list (specification)

Specified on 2026-11-21 from the maintainer's requests of that day, which section 1 keeps word for word. Not built
yet. The decisions taken in writing it are listed in section 9, for him to change before or after the build. The
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
- **AF-6.3 The user gives the key** in Actions, Actions of this list (AF-10.3): a key two actions would share is
  refused there, with the name of the action that has it.

## 7. What is said, asked and trusted

- **AF-7.1 One sentence says what an action does**, made from the file alone (`ListAction.Described`): "runs gh with
  7 arguments: issue, comment, {Number}, --repo, KyleKeane/AxKit-reports, --body-file, -; gives it {form:Answer} as
  input; then sets Answered to {today}". The form's line, the menu's detail and the question of AF-7.2 all use it.
- **AF-7.2 An action that starts a program is asked about before its first run.** A `set` is one undo step and is
  never asked about. `move` and `open` are asked about as today (TK-7a.3: said at the first press, done at the
  second). `run` and `terminal` open a notice before the first run on this computer, and again whenever anything of
  the action changed in the file: its title "Run <name>?"; its text, who wrote it ("This action came with the list's
  file. axKit did not write it and checked nothing of it; another program or an AI may have written it." or, for an
  action made in axTask's own form on this computer, "You made this action here."), the sentence of AF-7.1, the
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

## 9. Decisions taken in writing this, for the maintainer to change

1. **Per list, in `list.json`** (his message 6), never per item: every item of a list has the same actions.
2. **Five kinds at first**: the three there are, `run` and `terminal`. .NET has all it needs for these built in
   (`System.Diagnostics.Process`, through the bundle's `ProcessRun`); no new protocol code.
3. **Arguments, not command text** (AF-4.2). It costs the convenience of typing a command line as one text, and
   buys that no item's content can ever change what runs. A command line as text is what `terminal` is for, where
   the user reads it before he sends it.
4. **Eighteen fixed keys** (AF-6.1) in place of any key the user likes: no collision can be made, and no key of the
   bundle can be lost to a list's file.
5. **A program is asked about once, with everything shown** (AF-7.2), not at every run: a question at every run is
   answered without being read.
6. **The approval stays code** (AF-8.4), and no web request of axTask's own (AF-8.1).
7. **The name is "actions"**; the action keys become actions with a key, and their files keep working (AF-3.3).

## 10. Build order

1. `AxTask.Core`: `ListActions` (the file read and written, `"keys"` read on), `ActionValues` (the placeholders and
   their refusals), the acceptance over a whole action; `AxKit.Core/ShellQuote`. All pure, each with its tests.
2. The window: Context, Actions of this list; the eighteen keys; the form of an action's fields; the question of
   AF-7.2; the run off the UI thread with its words, its output and its stop; the hand-over to axTerminal.
3. Actions, Actions of this list: the form that makes and changes an action. Name, key, kind and the kind's members
   as lines, as Action keys' form has them today; an action's form fields are written in the list's file with axJSON
   at first, one Enter away (Open the list's file in axJSON), and get controls of their own when that proves too
   little.
4. `SPEC.md` section 7a rewritten for actions, the user guide, the NVDA plan, the outside-calls page, `PRIVACY.md`
   and `SECURITY.md`; window scripts; an NVDA pass.
5. The first uses, in the management lists: Answer the report on GitHub in the outside reports list (after an entry
   is Done), and what the maintainer asks for next.
