# axMCP NVDA test plan

Run with NVDA before a release, after `run-axMCP.ps1` (or `.\run.ps1 -App mcpclient`). Every step says what NVDA should
read; a step that reads differently is a defect. The specification is `docs/axMCP/SPEC.md` (MP-n.m). The automation's
script `tests/scripts/axMCP/01-start.txt` runs sections 1 to 4 and 6 against a fake server it writes itself;
`nvda-pass.ps1 axMCP` runs it with NVDA listening. A server of Claude Code's own on this computer (axKit's broker,
`AxKit.exe mcp`, once the AI tools are on and paired) is the one to hear section 2 and 3 with by hand.

## 1. Start and the servers (MP-1)

1. Start axMCP: NVDA reads the title "Servers - axMCP, access layer for MCP servers window", then the navigator
   "Servers, N servers, list" and its first row, a section header ("Claude Code, section, 2 servers") or the one row
   "No servers: Ctrl+N adds one…". Nothing else is spoken.
2. Down through the rows: each server as "name, stdio, stopped" (or "http, not started by this version"). Ctrl+Down
   and Ctrl+Up jump between the two section headers; the edge sound past the last.
3. Tab on a server: "Details of name" and the first line "Source: Claude Code, user scope"; Down: Command, Folder,
   Environment (the names, never a value), State. Escape back to the row.
4. Right on an http server: "http servers need a network connection of axKit's own, which this version does not
   make"; the level does not change.

## 2. A server started (MP-2)

1. Right on a stdio server never started before: the consent notice, "<name> (<program>) asks…", its check box,
   Connect disabled until the box is checked, Not now: "Not connected to name". Right again, check the box, Connect.
2. "name, starting", then within a few seconds "name, running, Tools N, Resources N, Prompts N"; the navigator reads
   "Tools, N" selected; the title ends "name - axMCP, access layer for MCP servers"; NVDA+End reads the state in the
   status bar.
3. Left: back to the top, the server's row now "name, stdio, running". Right again: the level at once, no new start.
4. Ctrl+Shift+S: "Stopped name"; the level's one row "Stopped (exit code N): Enter starts it again"; Enter starts it
   again.

## 3. A tool (MP-3)

1. Right on Tools: "Tools, N tools, <first tool>, tool, <first sentence>". Down through the tools.
2. Tab on a tool: "Arguments of name, Description, long text, <text>"; Enter on it reads the description whole. Down:
   each argument "name (required), text, " or its type and default.
3. Ctrl+Enter with a required argument empty: "<argument> is required"; nothing is called.
4. Enter on a text argument: the notice "Change <argument>", the field; type a value, Enter: "<argument>: value", the
   pane on the field. A check: Space, "<argument>: done", and Space again, "<argument>: not done" (the item apps' words, `item-apps.md`); a choice: Enter, the list of options, Enter.
5. Ctrl+Enter: "Calling name", then the notice "Result of name" with its text focused; the arrows read it, h moves by
   heading where the text has them, Ctrl+F finds; Escape: the pane is back on the same field.
6. Ctrl+J: axJSON opens on `last-result.json` with the whole answer; close it. Ctrl+J before any call: "No result yet".
7. A tool that reports an error: the result's first line "The tool reported an error". A server error: the notice
   "name failed" with its message.

## 4. Resources and prompts (MP-4, MP-5)

1. Right on Resources: each row "name, resource, address". Enter: "Reading name", then the notice named after it
   with its text. Escape back on the row. On a template: the notice "Read a resource" asking the address, prefilled.
2. Right on Prompts: "name, prompt, <sentence>"; Tab: its arguments; Ctrl+Enter: "Fetching name", the notice with
   "user: …" paragraphs.

## 5. Your servers (MP-1.3)

1. At the top, Ctrl+N: "Add server", "Name"; a name, Enter; "Command line"; a command line, Enter; "Folder"; Enter
   on the prefilled folder: "Added name", the row selected under "Your servers".
2. F2 on it: the rename box; a new name, Enter: "Renamed to …". Ctrl+Z: "Undone: renamed …".
3. Delete on it: "Remove name?", Enter: "Removed name". Ctrl+Z: "Undone: removed name", the row back.
4. F2 or Delete on a server of Claude Code's: the sentence naming `claude mcp`.

## 6. Stopping and closing (MP-2.3)

1. With a server running, Alt+F4: the window closes without a question, and the server's process is gone (axProcess,
   or Task Manager).

## 7. F1 and notices

1. F1: "Keyboard shortcuts", the text's first line; h through the headings: In every window of axKit, The navigator,
   The detail pane, Everywhere in axMCP, Notices. Escape back where the focus was.
2. Every notice of this plan: Tab reaches its buttons, Escape closes it, the focus goes back to the control that
   had it.
