# axMCP specification

axMCP, access layer for MCP servers (bundle `docs/axkit/SPEC.md` AX-11): a window that starts a Model Context Protocol
server the way an AI client does and lets the user do by hand what the client does: see the server's tools, resources
and prompts, fill a tool's arguments as typed fields, call it and read the result. Asked by the user on 2026-11-09
("manually explore and execute any available MCP servers"); specified and built the same night. Requirements are
numbered MP-n.m, decisions MP-Dn. The window is the item apps' (`docs/axkit/item-apps.md`): a navigator of servers,
their capabilities and their tools, and a detail pane of fields. In axMCP's code and documents, "SPEC.md" means this
file. axKit's own MCP server, the broker that lets an AI client read axKit (`docs/axkit/mcp.md`), is the mirror of
this app: there axKit serves, here axKit calls.

## 1. Summary

`AxKit.exe mcpclient [<folder>]`, Start entry `axKit axMCP`, command `axmcp`. The navigator's top level lists every MCP
server this computer knows: the servers in Claude Code's own configuration (the user's `.claude.json`, and a folder's
`.mcp.json` when axMCP is started on a folder) and the servers the user adds in axMCP itself. Right or Enter on a
server starts it as a child process (its program does the work under its maker's terms; AX-11.4 asks once), speaks the
protocol's handshake, and shows what the server offers: Tools, Resources and Prompts, each a level of its own. A tool
is an item: Tab to its fields, one per argument, made from the tool's input schema with the item apps' types (text,
number, check, choice, a long text for anything structured); Enter changes a field, Ctrl+Enter calls the tool. The
result opens as a notice text with the bundle's reading keys; Ctrl+J opens the raw JSON in axJSON. A resource is read
with Enter; a prompt is filled and fetched like a tool.

## 2. Goals and non-goals

- G1. Every server Claude Code knows on this computer, plus the user's own, in one list, by name, with where it came
  from.
- G2. A tool's arguments as fields filled by ear, with the types the item apps already have; nothing typed as JSON
  unless the schema is structured.
- G3. Results read like every text of the bundle: a notice text with the heading letters and Ctrl+F, and the raw JSON
  a key away in axJSON.
- G4. Local first (bundle G6, AX-11.11): axMCP itself makes no network request. A server is a program on this
  computer that reaches whatever it reaches, as Claude Code's servers do; the consent notice says so before the first
  start.
- Non-goals of the first version: HTTP and SSE transports (remote servers, OAuth sign-in), which are a new network use
  of axKit's own to state in `PRIVACY.md` and the outside-calls page before they exist (MP-D2); the server's requests
  to the client (sampling, elicitation, roots), answered with "not supported"; subscriptions and progress; writing
  Claude Code's configuration (MP-D3); environment variables of the user's own servers (MP-8).

## 3. Scenarios

- The user has just added a server to Claude Code and wants to know what it offers before giving Claude a task. axMCP:
  Down to the server, Right; "axkit, starting", then "axkit, running, Tools 8, Resources 0, Prompts 0"; Right into
  Tools; the eight tools by name with the first sentence of each description; Tab on one reads its arguments.
- A tool is worth running without a turn of Claude's: Tab, Enter on the argument, type the value, Escape back,
  Ctrl+Enter: "Calling axkit_windows", then the result text, read line by line, Ctrl+F to find a word, Escape back on
  the tool.
- A server fails to start: "Could not start weather: The system cannot find the file specified" as a notice; the
  server's row reads "weather, stdio, stopped".
- A server in `.claude.json` is remote (`"type": "http"`): its row reads "github, http, not started by this version";
  Right says why and starts nothing.

## 4. Requirements

### MP-1 The servers

- MP-1.1 **Sources**, read when the window opens and on F5, shown in two sections (item-apps IA-2.3a): **Claude
  Code**: the `mcpServers` object of `%USERPROFILE%\.claude.json` (the user scope), then, when axMCP was started on a
  folder, that folder's entry in the same file's `projects` (the local scope) and the folder's `.mcp.json` (the
  project scope), each server with the scope that defines it; **Your servers**: axMCP's own list in its settings file.
  The same file formats Claude Code documents: a server has `command` and `args` (and `env`, `cwd`), or `type` with
  `url` for `http` and `sse`. A section with no server is left out; with no server at all the one row says how to add
  one. The navigator is named "Servers, N servers".
- MP-1.2 A row reads the name, the kind and the state: "axkit, stdio, stopped", "github, http, not started by this
  version", "axkit, stdio, running". The detail pane on a server is lines, not fields: Source ("Claude Code, user
  scope"), Command (the program and the arguments as one line, or Address for a remote one), Folder, Environment ("2
  variables" and their names, never their values, which may be secrets), State, and the server's own name and version
  once it runs. Claude Code's entries are read only in axMCP (MP-D3): F2 and Delete on one say to use `claude mcp`.
- MP-1.3 **Your servers**: Ctrl+N at the top level asks the name, then the command line (split as Windows splits one,
  `CommandLines`), then the folder (optional; the folder in hand otherwise, or the profile folder); Delete removes one
  after the bundle's yes-or-cancel question; F2 renames; Ctrl+Z takes the last of these back. Kept in `axmcp.json`
  (MP-8). A name already in the list is refused with the notice kept open. On any other level Ctrl+N says "Servers
  are added at the top level"; Ctrl+Shift+N says that Ctrl+N adds one.
- MP-1.4 A server whose kind is not `stdio` is listed and never started in this version; Right or Enter says "http
  servers need a network connection of axKit's own, which this version does not make" (MP-D2).

### MP-2 Starting and stopping

- MP-2.1 Right or Enter on a stopped stdio server shows its level and starts it: the consent notice first, once per
  server (AX-11.4, AX-11.5; the target is the server's name with its program's file name, "github (npx)"), then the
  process (`// Outside call, program, MCP servers`) with the server's `env` added to its environment, in the server's
  folder (or the folder in hand, or the user's profile), its standard error to axMCP's log line by line; then
  `initialize` (protocol version 2025-06-18, `clientInfo` axMCP and its version, no client capabilities),
  `notifications/initialized`, and `tools/list`, `resources/list`, `resources/templates/list` and `prompts/list` for
  the capabilities the server declared, each page followed by its cursor until the last. While it starts the level
  reads "Starting <name>"; then it is filled anew and announced once: "axkit, running, Tools 8, Resources 2, Prompts
  0". The status bar's first label is the level's name, the second the server's state.
- MP-2.2 The server's level shows one row per capability it declared: "Tools, 8", "Resources, 2" (the templates
  counted in), "Prompts, 0"; Right into one. Left goes back up and leaves the server running.
- MP-2.3 **Stop**: Ctrl+Shift+S on a running server (the level's, or the one selected at the top), or closing the
  window, which stops every server: the standard input is closed, then after two seconds the process is ended with its
  children. "Stopped <name>". A server that ends by itself is announced, "axkit stopped (exit code 1)", its row reads
  stopped with the code, and Enter on its level starts it again.
- MP-2.4 Every request waits at most 60 s; after that the call is abandoned with "No answer from axkit in 60 s", a
  `notifications/cancelled` is sent, and the server is left running. One call at a time: a second Ctrl+Enter meanwhile
  says that a call is running.
- MP-2.5 The UI thread never waits on the server (AX-7.5): the client reads on its own thread, every answer completes
  a task the window awaits on its own thread (the base's `Do`), and no key is blocked meanwhile.

### MP-3 Tools

- MP-3.1 A tool's row: its title (its name when the server gives none), "tool", and the first sentence of its
  description ("axkit_windows, tool, Lists the open axKit windows."), or "no description".
- MP-3.2 The detail pane on a tool, named "Arguments of axkit_windows": Description first (a long text, fixed: Enter
  on it reads it whole), then one field per property of the input schema's object, in the schema's order, the type
  from the schema (MP-3.3), the title followed by " (required)" for a required argument, the default value filled in,
  else empty. A tool with no arguments has the one Description field. The fields are the server's: New field, Rename,
  Remove and Move say so (`FieldsFixed`).
- MP-3.3 **Schema to field** (`ArgumentFields`): `string` → `text`; `string` with `enum` → `choice`, the first option
  chosen when there is no default; `string` with `format` `date` → `date`, `date-time` → `datetime`; `integer` or
  `number` → `number`; `boolean` → `check`; `array`, `object` and anything else → `longtext` holding JSON, read back as
  JSON; a schema with no `properties` → one `longtext` named "Arguments (JSON)", whose object is the call's arguments.
  A long text that is not JSON is refused at the call with "Not JSON: …".
- MP-3.4 **Call**: Ctrl+Enter on the tool's row or in its fields: a required argument still empty is said and the call
  is not made ("path is required"); otherwise "Calling axkit_windows", then `tools/call` with the fields' values typed
  as the schema says and the empty optional ones left out. The values typed are kept for the window's life per
  server, tool and prompt (MP-D6), so that a second call is one key.
- MP-3.5 **The result** opens as a notice text titled "Result of axkit_windows": every `text` content item in order, a
  blank line between; an `image` or `audio` item as one line "[image, image/png, 20 KB]"; an embedded `resource` as
  its text, or "[resource, uri]" for a blob; `isError` true puts "The tool reported an error" as the first line. The
  bundle's notice keys (the heading letters, Ctrl+F, Escape back to where the focus was). A result with no content:
  "No content". A JSON-RPC error: a notice "axkit_windows failed" with the error's message.
- MP-3.6 **Ctrl+J** after a call opens the last raw answer (the whole result object) in a new axJSON window, written
  to `%LOCALAPPDATA%\AxKit\axmcp\last-result.json` first (beside the logs, never a shared temp folder); before any
  call it says "No result yet".

### MP-4 Resources

- MP-4.1 A resource's row: its name, "resource", and its address (URI); a template's row its name, "resource
  template", and its address template.
- MP-4.2 Enter on a resource reads it (`resources/read`), "Reading readme", and opens a notice titled with its name:
  text contents as they are, a blob as "[blob, image/png, 20 KB]". Enter on a template asks for the address in a
  one-field notice prefilled with the template, then reads it.
- MP-4.3 The detail pane on a resource: Name, Address, Type, Description, Size when given, and what Enter does; lines.

### MP-5 Prompts

- MP-5.1 A prompt's row: its title, "prompt", and the first sentence of its description.
- MP-5.2 The detail pane, "Arguments of summarize": Description, then one `text` field per argument, " (required)"
  marked; Ctrl+Enter fetches it (`prompts/get`), "Fetching summarize", and opens a notice titled with the prompt: its
  description, then each message as a paragraph starting with its role and a colon ("user: …"), a blank line between.

### MP-6 Keys

Bundle-wide keys (AX-8) as in every window; the item apps' keys (item-apps section 3, `ItemKeys`) for the panes,
Enter, Right, Left, Tab, F2, Delete, Ctrl+N, Ctrl+Shift+N, Ctrl+Z, Ctrl+Q and F5. Window-specific, in `AxMCPKeys`:

| Key | Does |
| --- | --- |
| Ctrl+Enter | Call the tool or fetch the prompt selected (MP-3.4, MP-5.2) |
| Ctrl+J | Open the last result as JSON in axJSON (MP-3.6) |
| Ctrl+Shift+S | Stop the server (MP-2.3) |

Enter on a resource reads it (MP-4.2), before the item apps' Enter, which moves to the fields; Enter on a tool or a
prompt moves to its arguments, as item-apps IA-D2 says. F5 reads the files again and, on a running server's level,
its lists again.

### MP-7 Announcements and notices

Every sentence spoken, in one list: "<server>, starting"; "<server>, running, Tools N, Resources N, Prompts N";
"<server> stopped (exit code N)"; "Stopped <server>"; "<server> is not running" and "<server> is not running: Enter
starts it"; "Calling <tool>"; "Fetching <prompt>"; "Reading <resource>"; "<argument> is required"; "Not JSON: …"; "A
call is running; it is given 60 seconds"; "No result yet"; "No answer from <server> in 60 s"; "<kind> servers need a
network connection of axKit's own, which this version does not make"; "Not connected to <server>" (Not now in the
consent notice); "Servers are added at the top level"; "Servers are added with Ctrl+N at the top level"; "Added
<name>", "Renamed to <name>", "Removed <name>", "Undone: …", "Nothing to undo"; "Only Your servers are removed here; a
server of Claude Code's goes with claude mcp remove"; "A server of Claude Code's is renamed with claude mcp remove and
claude mcp add"; "Names here are the server's"; "Select a server first", "Select a tool first: Right into a server,
then into Tools", "Select a tool or a prompt first", "Select a resource first"; "No description" or the description
whole (Enter on it); a field's change as the item apps say it ("path (required): C:\work"). Notices: the consent notice
(AX-11.4); "Could not start <server>"; "Result of <tool>"; "<tool> failed"; a resource's text under its name; a prompt's
messages under its title; "Remove <name>?" (yes or cancel); the three one-field notices of Add server and the one of a
resource template; "Last result" when the file cannot be written.

### MP-8 Settings

`%APPDATA%\AxKit\axmcp.json`: the window base's settings (`WindowSettings`) and `servers`, a list of `{ "name",
"commandLine", "folder" }`; no environment variables, since a token belongs in the user's own environment or in
Claude Code's file. The consents are the bundle's (`consent.json`, app axMCP, target "<name> (<program>)").

### MP-9 Title and status bar

The title is "Servers - axMCP, access layer for MCP servers" at the top, "<server> - axMCP, access layer for MCP
servers" on a server's level and "<Tools> of <server> - …" inside one (AX-11.2). The status bar: the level's name on
the left ("Tools, 8 tools"), the server's state on the right (the folder in hand at the top).

### MP-10 Errors

A server that does not start, or fails its handshake, is a notice naming the server and the system's or the server's
message; the row stays stopped. A line from the server that is not JSON is logged and skipped. A server that answers
a request with an error is the notice of MP-3.5. The log holds every method sent and answered with its id and the
answer's size, and the server's standard error lines, never the arguments or the results, which may hold secrets.

## 5. Architecture

- `src/AxMCP.Core` (net10.0, references `AxKit.Core` alone): `McpServer` (the record of MP-1.1 with its source and
  kind), `ServerSources` (the three files to a list of servers; the pure parts take the files' texts), `CommandLines`
  (a typed command line split as Windows splits one, and a word quoted back), `McpClient` (the protocol over any
  `TextReader` and `TextWriter`: `InitializeAsync`, `ListToolsAsync`, `ListResourcesAsync`, `ListPromptsAsync`,
  `CallToolAsync`, `ReadResourceAsync`, `GetPromptAsync`, each a request with an id matched to its answer, the reader
  loop on its own thread, server requests answered with -32601, notifications logged and dropped), `StdioServer` (the
  process with its pipes wired to a client, the stop of MP-2.3; the one outside call), `ArgumentFields` (a schema to an
  `ItemDocument` and the document back to the call's arguments, MP-3.3), `ResultText` (an answer to the notice's text,
  MP-3.5), `MCPSettings` with `OwnServer`. Tests in `tests/AxMCP.Tests` drive `McpClient` over two anonymous pipes
  against a fake server written in the test.
- `src/AxMCP`: `AxMCPApp`, `MCPForm : ItemForm` (the levels of MP-1 to MP-5 as `ReadLevel`, the fields as `DetailOf`,
  the starts and calls awaited through the base's `Do`), `AxMCPKeys`, `HelpText`.
- The one outside call is the server's process start, with its comment (B-17); `tools/outside-calls.ps1`'s tables
  carry the kind "MCP servers".

## 6. Decisions

- **MP-D1 The item apps' window.** Servers, capabilities and tools are lists of typed things, and a tool's arguments are
  fields: exactly item-apps.md's shape, with its keys the user knows from axTask and axPass. A transcript window (the
  console shape) was the alternative; a result is one text, not a stream.
- **MP-D2 stdio alone in version one.** An HTTP server is a network connection axKit's own code would make, which the
  bundle states exhaustively (B-17, `PRIVACY.md`) and which brings OAuth sign-in with it; the first version is useful
  without it (every local server, axKit's own among them) and says plainly why a remote one is not started.
- **MP-D3 Claude Code's files are read, never written.** Adding a server to Claude Code is `claude mcp add` in
  axTerminal or axClaude; axMCP changing another program's configuration would be a second place for it.
- **MP-D4 Structured arguments as JSON text.** An `object` or `array` argument is one long text holding JSON: one
  way, read back exactly, axJSON a key away for a large one; a form built from nested schemas was judged complexity
  without a user asking for it.
- **MP-D5 Results as a notice text.** The bundle's reading keys, Ctrl+F and Escape are what the user knows from
  every other text; Ctrl+J gives the raw JSON to axJSON for the structured view.
- **MP-D6 Argument values live with the window.** Kept per server and tool while the window is open, never saved: a
  value may be a secret, and a tool's arguments change with the server's version.
- **MP-D7 The verb is `mcpclient`.** `AxKit.exe mcp` is the broker an AI client starts (`docs/axkit/mcp.md` MC-1.1),
  configured in users' files; the app's verb had to be another word. The console command stays `axmcp`, since the
  commands are named after the apps.

## 7. Testing

- Pure: `ServerSources` on the three files (a user scope, a local scope, `.mcp.json`, a remote server, a server that
  is not an object); `CommandLines` on quotes and spaces; `McpClient` against the fake server (the handshake, a paged
  list, a call, an error answer, a server request answered with -32601, a wait given up, a server that ends);
  `ArgumentFields` on the schema kinds of MP-3.3 and back; `ResultText` on text, image, resource and error results;
  `MCPSettings` cleaned.
- The window: `tests/scripts/axmcp` drives it against a fake server written in Windows PowerShell by the script
  itself (the sections, a start, the three levels, a tool's fields, a required argument refused, a call and its
  result notice, Ctrl+J, a resource read, Stop).
- NVDA: `docs/axmcp/nvda-test-plan.md`, run through `nvda-pass.ps1 axMCP`.

## 8. Status

Specified and built 2026-11-09 (the night run); its NVDA pass through `nvda-pass.ps1 axMCP` the same night, recorded
in `docs/axkit/nvda-test-record.md`.
