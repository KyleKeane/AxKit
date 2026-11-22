# axMCP, the window

The WinForms side of axMCP: the item apps' window (`AxKit.Forms/ItemForm`, docs/axKit/item-apps.md) over MCP servers. The pure code is `src/AxMCP.Core`, with its own `CLAUDE.md`. The spec is `docs/axMCP/SPEC.md` ("SPEC.md" below); the guide `docs/axMCP/user-guide.md` is embedded by `AxMCP.csproj` for Help, User guide; the manual test plan is `docs/axMCP/nvda-test-plan.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
& '.\scripts to run-individual-apps\run-axMCP.ps1' [<folder>]       # build axKit and start axMCP (run.ps1 -App mcpclient)
.\test.ps1 AxMCP                 # the tests in Release, as CI runs them
dotnet run --project tools\AxAutomate -c Release -- tests\scripts\axMCP --build   # the window driven against the fake server
```

## Where things are

- `AxMCPApp`: the entry point `Run` that axKit's `Program` calls (bundle AX-1.7), one call into `AxKit.Forms/AppHost.Run` with the one optional folder.
- `MCPForm : ItemForm`: the levels (`ReadLevel`: the top in two sections, a server's with Tools, Resources and Prompts or its start row, a section's items), `EnterList` starting a server (consent first, `HasConsent` with `ServerConsent.For`, the whole of what would run, MP-2.1a), `DetailOf` (a server's lines, among them what it wrote to its standard error since its start, the count and the newest `ErrorLinesShown` through `DisplayText.Visible`, SPEC.md MP-10: the one place those lines are, the log has their lengths alone; a tool's or a prompt's arguments as a document kept per server in `Running.Arguments`), `SaveFields` keeping a change, the calls (`CallSelected`, `Request`: one awaited request through the base's `Do`, 60 s), the result notices, Ctrl+J, Stop, and Your servers (Ctrl+N, F2, Delete, one-step Ctrl+Z). The selection (SPEC.md MP-1.5a, bundle AX-8.16, 2026-11-22): `HasSelection`, `CanSelectRow` (one of Your servers), `LevelIdentity` (the server gone into and its section), `RemoveServers` (Delete with several selected: one question, each stopped and removed, one undo that puts all back where they stood). Its commands are `Add` and `AddKey` calls (docs/axKit/app-kit.md AK-2); "Read the resource" is registered before the shared keys so that Enter on a resource reads it.
- `AxMCPKeys`: `ItemKeys.All` and the bundle's selection keys plus Ctrl+J and Ctrl+Shift+S (bundle AX-8; Ctrl+Enter, the call, is a command on the bundle's `BundleKeys.MainAction`, AX-8.10), the one place a key is changed; `All` feeds the collision test and the F1 text names the same keys.
- `HelpText`: the F1 text and the embedded guide; About is the bundle's.

## Rules

1. Every control gets an `AccessibleName`; nothing speaks on an arrow key.
2. No second window (bundle AX-7.2): questions and results are notices.
3. Run the `accessibility-review` skill on a change here before committing, and check it with NVDA following the test plan (`nvda-pass.ps1 axMCP`).

## Gotchas

- The verb is `mcpclient`, since `AxKit.exe mcp` is the broker an AI client starts (docs/axKit/mcp.md); the console command is `axMCP` as the commands are named after the apps (SPEC.md MP-D7).
- A server's `Exited` event comes on a thread pool thread: `BeginInvoke` onto the window, and only while the handle exists.
- The fake server of `tests/scripts/axMCP` is Windows PowerShell reading standard input line by line; it takes a second to start, which the 60 s wait covers.
