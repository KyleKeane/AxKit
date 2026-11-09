# axMCP, the window

The WinForms side of axMCP: the item apps' window (`AxKit.Forms/ItemForm`, docs/axkit/item-apps.md) over MCP servers. The pure code is `src/AxMCP.Core`, with its own `CLAUDE.md`. The spec is `docs/axmcp/SPEC.md` ("SPEC.md" below); the guide `docs/axmcp/user-guide.md` is embedded by `AxMCP.csproj` for Help, User guide; the manual test plan is `docs/axmcp/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
.\run-axmcp.ps1 [<folder>]       # build axKit and start axMCP (run.ps1 -App mcpclient)
.\test.ps1 AxMCP                 # the tests in Release, as CI runs them
dotnet run --project tools\AxAutomate -c Release -- tests\scripts\axmcp --build   # the window driven against the fake server
```

## Where things are

- `AxMCPApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run` with the one optional folder.
- `MCPForm : ItemForm`: the levels (`ReadLevel`: the top in two sections, a server's with Tools, Resources and Prompts or its start row, a section's items), `EnterList` starting a server (consent first, `HasConsent`), `DetailOf` (a server's lines, a tool's or a prompt's arguments as a document kept per server in `Running.Arguments`), `SaveFields` keeping a change, the calls (`CallSelected`, `Request`: one awaited request through the base's `Do`, 60 s), the result notices, Ctrl+J, Stop, and Your servers (Ctrl+N, F2, Delete, one-step Ctrl+Z). Its commands are `Add` and `AddKey` calls (docs/axkit/app-kit.md AK-2); "Read the resource" is registered before the shared keys so that Enter on a resource reads it.
- `AxMCPKeys`: `ItemKeys.All` plus Ctrl+Enter, Ctrl+J and Ctrl+Shift+S (bundle AX-8), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; nothing speaks on an arrow key.
2. No second window (bundle AX-7.2): questions and results are notices.
3. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan (`nvda-pass.ps1 axMCP`).

## Gotchas

- The verb is `mcpclient`, since `AxKit.exe mcp` is the broker an AI client starts (docs/axkit/mcp.md); the console command is `axmcp` as the commands are named after the apps (SPEC.md MP-D7).
- A server's `Exited` event comes on a thread pool thread: `BeginInvoke` onto the window, and only while the handle exists.
- The fake server of `tests/scripts/axmcp` is Windows PowerShell reading standard input line by line; it takes a second to start, which the 60 s wait covers.
