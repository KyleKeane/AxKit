# AxKit.Core notes: The AI tools (docs/axKit/mcp.md MC-6, 2026-10-24)

- `Mcp/McpAccess` with `McpSettings` and `McpClient`: the bundle's one setting in `%APPDATA%\AxKit\mcp.json` (off
  until the user turns it on, the paired clients by their program file), read again at every connection; the key in
  `%LOCALAPPDATA%\AxKit\mcp.key` through `UserSecret`, `NewChallenge`, `Proof` and `Proves` (HMAC-SHA256). The HMAC, the comparison and the key file's reading are `SettingsSeal`'s since 2026-11-16 (`notes/safety.md`); the file, the key and the seal's value are as they were.
- `Mcp/McpPipeName` (MC-6.2, 2026-11-12): a window's pipe name after the user's prefix, the random digits, after
  "run", the run's mark and a dash in a test run (`AppPaths.RunMark`, which `OneAtATime` uses too); `SameKind`, the
  broker's filter, so that a run's broker never knocks on the user's windows. Tested in `McpPipeNameTests`.
- `Mcp/McpProtocol`: MCP's JSON-RPC as the broker speaks it, one line in, one line out, the tools from two functions;
  `Text` reads a string a client sent without trusting its type. `McpTool` and `McpResult`.
- `ClaudeCodeProgram` (axClaude FR-1.2, MC-6.8; from `AxClaude.Core/ClaudeLauncher` on its second use, 2026-11-09):
  where Claude Code is, `Candidates` (an explicit path alone, else `claude.exe` and `claude.cmd` in every full PATH
  folder, the installer's `.local\bin`, npm's folder; a PATH text can stand in for the process's in tests) and `Find`
  (the first that exists); `LineFor` (R-24, 2026-11-14), how it is started with a list of arguments, by every caller
  (axClaude's session and version, `AiRequest`, the registration): `SystemPrograms.LineFor` with `BatchAdvice` after
  a refusal. Tested in `ClaudeCodeProgramTests`, `SystemProgramsTests` and `AiTests`.
- `Mcp/McpClaudeCode` (MC-6.8): the `claude mcp add` and `remove` command lines that register this AxKit.exe for every
  folder of the user; the window runs them on the Claude Code `ClaudeCodeProgram` finds (`AxKit.Forms/AiToolsSetup.cs`).
- `Mcp/McpTools`: the tools, `AppTools` (MC-8, 2026-11-11: the typed data tools of one app by its verb, and whether each changes something; axTask's three), `NeverRead` (axPass, axClip) and `NeverOpened` (axClaude, axTerminal),
  and `OpenArguments`, the check of `axkit_open`'s app and path. `Mcp/McpAudit` (one line a call, the arguments
  hashed) and `McpRate` (ten calls a second). The pipes and the broker are `AxKit.Forms`' (`McpChannel`, `McpBroker`).
- `Mcp/McpOffer` (MC-9, 2026-11-13): the pure rules of `axkit_offer_command`, a command an AI tool offers the user to run themselves in axClaude: `Refusal` (none, a control character or line break, too long), `Answers` (Explain first, then Yes, No and Copy), `AskName`, `AskText`, `ExplainText` with `Details`, `Line` (the `!` line), `MaxWaiting`. Tested in `McpOfferTests`.
- `CommandNames` with `CommandName`, `CommandMatch` and `CommandResolution` (docs/axKit/user-workflows.md WF-1,
  2026-11-15): the one resolver of a command's canonical name, its label plain and, where two share it, the label with
  its menus as the palette lists it without its key ("Heading, Navigation, Next"). `Resolve` compares whole plain
  texts without regard to case, never searches (the palette's search is `Palette.Find`): a menu given, the label in
  those menus; else the label, and only when no label is the name, the whole line, so a label always wins; it gives
  the candidates as indexes (the commands that share an ambiguous name, at most five near an unknown one). `Line`,
  `CanonicalOf`, and `Refusal`, the checks' words; `McpRun.Find` keeps its own. Tested in `CommandNamesTests`.
- `Mcp/McpRun` with `McpCommandName` (MC-7, 2026-11-11): the pure rules of `axkit_window_run`, `NeverRun` (axPass,
  axClip) with `AppRefusal`, `SwitchOff`, `Find` (since 2026-11-15 a call of `CommandNames.Resolve` and a switch over
  its match, in the tool's own four refusals word for word; the whole line too, MC-7.1), `Plain` (`CommandNames.Plain`),
  `Speaker` (Claude Code's own names matched whole, `ClaudeCodeNames`), `Said` ("Claude ran Word wrap"), `AskTitle`, `AskText` with `Names`, `ShownTarget` and `TargetRefusal` (MC-7.3: the question names what the command would act on, and a Run after the selection changed is refused, by `RunTarget`'s identity, never its shown words), `Chimes` (MC-7.5: one chime for a burst),
  and `IsUserBusy` with `UserBusy` (MC-7.8: no run in a window in front within 3 s of the user's last input); the
  switch itself is `McpSettings.RunCommands`, sealed (its " run" in the seal only while on, so older files still
  check), `McpAccess.RunsCommands` over the pure `RunsCommandsIn`, and `SetRunCommands`; `Switched` (the access off
  takes the switch with it) and `Forgotten` (Forget: the clients and the switch) are the pure halves of `SetEnabled`
  and `ForgetAll` (MC-D9).
- `ClaudeCodeProgram`, the options of a run that obeys nothing of its folder (ai-help.md AH-2.6, 2026-11-16): `StrictMcpConfigOption`, `SettingsOption` (axClaude's `ClaudeCodeHooks.SettingsOption` is this one), `NoHooksSettings` (`{"disableAllHooks":true}`) and `WriteNoHooksSettings`, which writes the file when it holds anything else. Always a file, never the JSON on the command line: `claude.cmd` is refused an argument with a double quote. A new option of Claude Code's that AxKit.Core passes goes here.
