# AxMCP.Core, the pure code

Everything of axMCP that needs no window: the servers from Claude Code's files and the user's own list, the Model Context Protocol as a client over any reader and writer, a server as a process, a tool's schema to the item apps' fields and back, an answer to the notice's text. Unit tested in `tests/AxMCP.Tests` (the client against a fake server over two anonymous pipes). The spec is `docs/axmcp/SPEC.md` ("SPEC.md" below); the window is `src/AxMCP`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxMCP
```

## Where things are

- `McpServer` (SPEC.md MP-1): one server as listed: name, kind (stdio, http, sse, unknown), command and arguments, environment, address, source, folder, whether it is the user's own; `Key` for the navigator, `CommandLine` for the detail pane.
- `ServerSources` (MP-1.1): `Read(folder)` reads `%USERPROFILE%\.claude.json` (the user scope and the folder's local scope) and the folder's `.mcp.json` (the project scope); the pure `FromClaudeCode`, `FromProjectFile`, `Parse` and `One` take the texts. Claude Code's files are read, never written (MP-D3).
- `CommandLines` (MP-1.3): `Split` a typed command line as Windows splits one; `Quote` a word back.
- `McpClient` (MP-2, MP-3, MP-7): JSON-RPC 2.0, one message per line, over a `TextReader` and a `TextWriter`; the reader loop on its own thread; `RequestAsync` a task completed by the answer of the same id, `McpError` for an error answer, cancellation sending `notifications/cancelled`; `InitializeAsync` and the paged lists; server requests answered with -32601; `Closed` when the output ends. The records `ServerTool`, `ServerResource`, `PromptArgument`, `ServerPrompt`, `ServerInfo`.
- `StdioServer` (MP-2.1, MP-2.3): the server's process with its standard input and output wired to a client, its standard error logged, `Stop` closing the input and ending the tree after two seconds, `Exited` with the exit code and whether Stop asked. The one outside call (`// Outside call, program, MCP servers`).
- `ArgumentFields` (MP-3.2, MP-3.3, MP-5.2): `ForTool` and `ForPrompt` make the detail pane's document (Description first, fixed, then the arguments, " (required)" in the title), `ToArguments` and `ToPromptArguments` the call's object, `MissingRequired` the first required argument left empty, `TypeOf` a schema property's field type and options.
- `ResultText` (MP-3.5, MP-4.2, MP-5.2): `OfCall`, `OfResource`, `OfPrompt`, `Item`.
- `MCPSettings` with `OwnServer` (MP-8): the window base's settings and Your servers (`name`, `commandLine`, `folder`), cleaned on load; `OwnServers()` as `McpServer`s.

## Gotchas

- A JSON-RPC id from this client is an integer; the fake server in the tests echoes it as given, and a server's own request id (a string) is cloned back into the refusal.
- `resources/templates/list` may be "method not found" on an older server: the client takes that as no templates.
