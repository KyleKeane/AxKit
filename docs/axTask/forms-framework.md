# axTask forms: buttons, forms and keys of a list's own (the record before a specification)

Not a specification yet. This page keeps the maintainer's requests of 2026-11-21 word for word, in the order he sent
them while the approval of outside reports was being built, and the questions they raise, so that the specification
can be written and discussed from them. The priorities list's item "axTask: a general framework of buttons and forms
in items that build and run a command, a request or a tool call" points here. Its first concrete instance is the
approval of outside reports (`docs/axKit/outside-reports.md`, axTask TK-9), built the same day.

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

## 2. What they ask for, in one paragraph

A list's configuration (its `list.json`) can declare forms: named buttons on its items, each with fields the user
fills (or that are taken from the item), a key the user may assign, and one action the form builds and runs: a
command in PowerShell or in axTerminal, a web request, an MCP tool call, code of axKit's own, or a request in words to
an AI that proposes one of those. Approving an outside report with a passphrase is one such form. The forms are simple
to write, the same in every list, and easy for the assistant to maintain.

## 3. Questions for the specification

- **Where forms are declared.** In `list.json` beside the template and the action keys (TK-7a), so that every item of a
  list has them; or also in an item for itself. The maintainer's message 6 says per list.
- **The kinds of action, and which at first.** PowerShell or a program (`System.Diagnostics.Process`, as axKit's
  other program starts), axTerminal (the command typed into a window the user watches), a web request (`HttpClient`),
  an MCP tool call (axKit's own MCP pieces), axKit's own code (a closed list of verbs, never code from the file), and a
  request in words to an AI. .NET has all of these built in; the question is how few to start with.
- **Trust in a list's file.** A `list.json` can come from elsewhere (a shared folder, a sync, a report's suggestion,
  another program). The action keys already ask at the first press when this install did not write the action
  (TK-7a.3, R-72). A form that runs a program or reaches the network needs at least that, and perhaps the
  passphrase's signature over the form itself, as the approval of outside reports signs its scope.
- **What is run is shown first.** The command, request or call the form built is read to the user before it runs,
  with the bundle's words for a command (rule 11's care for code), and runs only at the user's own choice.
- **Outside calls** (root rule 8, B-17): every new kind of contact is named in the outside-calls page and the
  privacy documents in the commit that adds it; a form that reaches the network is the user's own act, and axKit's
  local-first rule stands.
- **Keys.** A key the user assigns to a form's button, within the bundle's key rules (AX-8: a window's own keys, no
  collision with a bundle key, `KeyTests`), as the action keys assign Ctrl+1 to Ctrl+9 today.
- **AI requests.** Whether a request in words belongs to axTask's forms or to axAutomate's workflows, with a form only
  starting a workflow (message 5); rule 11's caution on everything an AI proposes, rule 10's one shared AI service.
- **The approval as the first instance.** Whether Approve and Decline become the first forms of the framework once it
  exists, the passphrase becoming a field type of its own.

## 4. Who writes the specification

Recommended on 2026-11-21: Fable (the maintainer asks it in message 3 and 6). The approval of outside reports was
specified and is built narrow, so that the framework can take it in.
