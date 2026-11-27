# AxMCP.Core, the pure code

Everything of axMCP that needs no window: the servers from Claude Code's files and the user's own list, the Model Context Protocol as a client over any reader and writer, a server as a process, a tool's schema to the item apps' fields and back, an answer to the notice's text. Unit tested in `tests/AxMCP.Tests` (the client against a fake server over two anonymous pipes). The spec is `docs/axMCP/SPEC.md` ("SPEC.md" below); the window is `src/AxMCP`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\test.ps1 AxMCP
```

## Where things are

- `McpServer` (SPEC.md MP-1): one server as listed: name, kind (stdio, http, sse, unknown), command and arguments, environment, address, source, folder, whether it is the user's own; `Key` for the navigator, `CommandLine` for the detail pane.
- `ServerSources` (MP-1.1): `Read(folder)` reads `%USERPROFILE%\.claude.json` (the user scope and the folder's local scope) and the folder's `.mcp.json` (the project scope); the pure `FromClaudeCode`, `FromProjectFile`, `Parse` and `One` take the texts. Claude Code's files are read, never written (MP-D3).
- `CommandLines` (MP-1.3): `Split` a typed command line as Windows splits one; `Quote` a word back.
- `McpClient` (MP-2, MP-3, MP-7): JSON-RPC 2.0, one message per line, over a `TextReader` and a `TextWriter`; the reader loop on its own thread; `RequestAsync` a task completed by the answer of the same id, `McpError` for an error answer, cancellation sending `notifications/cancelled`; `InitializeAsync` and the paged lists; server requests answered with -32601; `Closed` when the output ends. The records `ServerTool`, `ServerResource`, `PromptArgument`, `ServerPrompt`, `ServerInfo`.
- `StdioServer` (MP-2.1, MP-2.3): the server's process with its standard input and output wired to a client, its standard error kept for the window in `ErrorLines` (`ServerErrorLines`: the last 50 lines in memory, each cut at 2000 characters; `Add` returns what the log holds, the server's name with the line's number and length and never its text, MP-10, bundle repairs R-74; never hand a server's line to the log), `Stop` closing the input and ending the tree after two seconds, `Exited` with the exit code and whether Stop asked. The one outside call (`// Outside call, program, MCP servers`).
- `ServerConsent` (MP-2.1a, R-61): `For(server, program, folder)` is the consent's target for the whole of what would run (name, resolved program, arguments, the environment's names, the folder, and `Mark`, a SHA-256 over all of them and the environment's values); a value is never in a text. `StdioServer.Resolve` and `StdioServer.FolderOf` give the program and the folder, the same ones `Start` uses. `ServerConsentTests`.
- `ArgumentFields` (MP-3.2, MP-3.3, MP-5.2): `ForTool` and `ForPrompt` make the detail pane's document (Description first, fixed, then the arguments, " (required)" in the title), `ToArguments` and `ToPromptArguments` the call's object, `MissingRequired` the first required argument left empty, `TypeOf` a schema property's field type and options.
- `ResultText` (MP-3.5, MP-4.2, MP-5.2): `OfCall`, `OfResource`, `OfPrompt`, `Item`.
- `MCPSettings` with `OwnServer` (MP-8): the window base's settings and Your servers (`name`, `commandLine`, `folder`), cleaned on load; `OwnServers()` as `McpServer`s. The servers are under the bundle's seal (MP-8.2, `ServersNotUsed`; `src/AxKit.Core/notes/safety.md`); `MCPSealTests`.

## Gotchas

- A JSON-RPC id from this client is an integer; the fake server in the tests echoes it as given, and a server's own request id (a string) is cloned back into the refusal.
- `resources/templates/list` may be "method not found" on an older server: the client takes that as no templates.
- The review of 2026-11-27, `StdioServer`: `Resolve` takes the folder the server starts in and gives a full path always (a path that is not full is taken from that folder, never from axKit's own current folder, SPEC.md MP-2.1b). `Stop` closes the input and returns; the two seconds and the end of the tree run on a task (`Ending`, `Grace`), since the caller is the window's thread. The process is in a `AxKit.Core/ChildJob` from its start, so that Windows ends it and what it started when axKit ends, however it ends; `Dispose` waits for nothing. The second pass: a server that ended by itself in its two seconds ends alone (no end of the job, and the job stays open until axKit ends, since closing it would end what the server left, a browser for a sign-in among it); only a server that had to be ended takes its job with it (SPEC.md MP-2.3).
