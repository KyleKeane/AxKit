---
name: record-fixture
description: Record a new ConPTY fixture of Claude Code in screen reader mode with tools/PtyCapture, review it for personal data, and add it to tests/fixtures with an expected transcript. Use when a parser bug needs a reproduction or a new Claude Code version needs pinning; also for a timeline fixture (hook events and the session file on one clock) made with tools/ClaudeProbe.
---

# Record a fixture

When to record one: a parser bug that needs a reproduction, and every Claude Code release whose notes touch screen reader mode (`--ax-screen-reader`, the questions, the screens), with the version in the fixture's name, so that the parser is checked against each version that changed what it draws (docs/axKit/longevity.md 3.10). The version axClaude runs is in its log (`Claude Code version:`) and in Options, Developer, Copy diagnostics.

1. Write a script in the scratchpad (one command per line): `send`, `mark`, `wait <ms> <text>`, `iffound <command>`, `quiet <ms> <max>`, `sleep <ms>`, `exit <ms>`. Answer startup dialogs conditionally, for example:

   ```
   mark
   wait 8000 Yes, I trust this folder
   iffound send y\r
   mark
   quiet 2500 30000
   send Reply with the single word hello.
   sleep 150
   send \r
   mark
   wait 90000 \x07
   quiet 2000 20000
   send /exit\r
   exit 15000
   ```

   Send the text and the `\r` as separate writes. Use a scratch folder as `--cwd` so the recording does not touch a real project.

2. Run: `dotnet run --project tools/PtyCapture -- --cwd <scratch folder> --out <name>.vt --cols 240 --rows 50 --script <script> -- "%USERPROFILE%\.local\bin\claude.exe" --ax-screen-reader`. The tool starts the command through the app's own `PtyHost` with the app's child environment (`ClaudeLauncher.BuildEnvironment`: no `CLAUDE*` variables, so it can be run from inside a Claude Code session, and the startup pauses set to 0 as in the app), so the recording shows what the app sees. When the recording ends, session ids and the subscription tier are masked in the file byte for byte (`Redaction.MaskRecording`), so a fixture holds neither; the older fixtures lose theirs when they are recorded again.

3. Inspect with `dotnet run --project tools/PtyCapture -- --dump <name>.vt`. The dump shows e-mail addresses, account paths, pipe names, session ids and the account name as `<email>`, `<user>`, `<pipe>`, `<id>` and `<name>`: any such placeholder means the recording holds personal data, and `--raw` (never printed into a Claude session) shows it. Do not record `/status`, `/usage` or `/cost` for a fixture: they print the account's e-mail address. Look also for anything else personal. `PrivacyTests` fails when a committed file holds an e-mail address or an account path. If present, re-record with a neutral folder or note the lines to redact in the fixture README; never hand-edit the bytes of a `.vt` file, because the cursor positions in it depend on the original text lengths.

4. Copy the `.vt` file to `tests/fixtures/`, name it `<topic>-claude<version>-<cols>x<rows>.vt`, and add its expected transcript `<same name>.expected.txt` produced by the parser once it exists (review the transcript by hand the first time).

5. Add a line to `tests/fixtures/README.md` with the date, Claude Code version, script used and what the fixture demonstrates. Commit fixture and expected file together.

## A timeline with events and the session file

A timeline fixture (`tests/fixtures/timelines/<name>/`, `docs/axClaude/events-and-grounding.md` EG-11.1) holds the console stream, Claude Code's hook events, the session file's entries and what was sent, on one clock, for the replayer. It is recorded with `tools/ClaudeProbe` (its `README.md` has the configuration, the script steps and the rules), never with PtyCapture.

1. Build the tool into a scratch folder, then shut the build servers down: `dotnet build tools\ClaudeProbe -c Release -o <scratch>\claude-probe` and `dotnet build-server shutdown`.
2. Write a configuration in the scratchpad: `cwd` a fresh folder under `C:\Temp\axclaude-fixture`, `out` a folder beside it, the model Haiku, the hook plan, and a script that answers the trust question conditionally. Nothing that prints the account (`/status`, `/usage`, `/cost`). One live session at a time.
3. Record: `<scratch>\claude-probe\ClaudeProbe.exe run <config.json>`.
4. Read what happened with `ClaudeProbe.exe timeline <out>` (and `shape <out>`), which replace the profile folder, e-mail addresses and the account name in what they print. Never print a run's files themselves: they are not redacted.
5. Make the fixture: `ClaudeProbe.exe fixture <out> tests\fixtures\timelines\<name>`. It prints counts and "written", or "refused" with the file and what it held in numbers, and then has deleted what it wrote. A refused run is recorded again; the fixture is never edited by hand.
6. Read the stream with `dotnet run --project tools/PtyCapture -- --dump tests\fixtures\timelines\<name>\pty.vt` and look for anything personal the check cannot know (a name in a reply, a file's content).
7. Add the fixture's row to `tests/fixtures/timelines/README.md`. Its `expected.txt` comes from the replayer's tests (`AXCLAUDE_UPDATE_EXPECTED=1`), reviewed by hand the first time.
