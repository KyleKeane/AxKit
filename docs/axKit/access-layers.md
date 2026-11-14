# The access layers and the programs under them

Rule 12 of the root `CLAUDE.md` (2026-11-14): an access layer holds the user's experience still while the program
under it changes. Everything read from an outside program or sent to it goes through one place of axKit's own per
program: its launcher, its parser, the table of its texts and keys, its protocol, each tested from recordings. A
window never reads a program's text itself. When the program changes, that place is adjusted, and what the user hears
and presses stays.

This page says, layer by layer, which program it is, where its one place is, what is read from it and how, what
guards it, and what is still outside the one place and why. It was written from an audit of the code on 2026-11-14.
Change it in the same commit as a change to any of these places.

What every layer shares:

- A program is found and started by code in a `.Core` project, never by a window. The start carries an
  `Outside call` comment, and `outside-calls.md` lists them all.
- Each program has a source-reading test that fails when one of its most characteristic words is written in a string
  outside its place. The tests read string literals only, so comments may quote a program freely. The reader is
  `tests/Shared/SourceTexts.cs`; axClaude's test has its own copy of the same reader.
- No table is an abstraction over programs. A second program of the same kind gets a table of its own of the same
  shape.

## axClaude: Claude Code

- The program: Claude Code, the `claude` command, run on a pseudo console in its screen reader mode.
- Its one place: `src/AxClaude.Core/Transcript/ClaudeCodeTexts.cs` holds every label, row, hint, mark and pattern
  of Claude Code's screen, with the version or fixture that shows each. `SlashCommands.All` is the table of the slash
  command names. `src/AxClaude.Core/Pty/ClaudeLauncher.cs` builds the command line and the environment, and
  `src/AxKit.Core/ClaudeCodeProgram.cs` finds the program.
- What is read and how: screen text. The byte stream goes through the bundle's parser and screen into lines, and
  `SessionModel`, `LineClassifier`, `QuestionReader`, `ScreenReader`, `ScreenActions` and `SurveyReader` recognise
  rows by the texts of the table.
- What guards it: the recordings under `tests/fixtures` with their expected transcripts, replayed whole and chunk by
  chunk; `ClaudeCodeTextsTests`, which fails on a watched text outside the table; the check scripts in
  `tests/scripts/axClaude`. `docs/axClaude/claude-screens.md` is the catalogue of the screens in prose.
- Still outside the table: the key sequences sent to Claude Code (`ScreenReader` for a hint row's keys, the window's
  menus), and the session presets of the window, which name command line options. They are few and sit next to the
  commands that send them. The F1 text names Claude's labels for the user and is allowed by name in the test.

## axGIT: Git

- The program: the user's installed `git`, one process per command, its output on pipes.
- Its one place: the folder `src/AxGit.Core`. `GitRunner` starts git: the program, the fixed configuration in front
  of every command, the environment (`GIT_TERMINAL_PROMPT=0`, `LC_ALL=C.UTF-8`, `GIT_PAGER=cat`), the timeout.
  `GitStatus`, `GitLog`, `GitBranches`, `GitTrust` and `GitRemote` each hold the arguments of one command next to the
  parser of its output. The records they make (`ChangedFile`, `Commit`, `Branch`) hold the arguments of the commands
  on one file, commit or branch. `GitTexts` holds the rest: the commands on the whole repository (stage all, unstage
  all, commit, fetch, pull, push) and the two texts of git's output that an announcement is chosen by.
- What is read and how: formats made for programs. `status --porcelain=v2 --branch` (without `-z`; a quoted path is
  unquoted by `GitStatus.Unquote`), `log` and `branch --list` with `--format` strings whose fields are separated by
  tabs, `config --name-only --list`, `rev-parse --show-toplevel` and `rev-parse --git-path hooks`, `remote get-url`.
  Two texts made for people are read: "Already up to date" after a pull (`GitTexts.SaysUpToDate`) and the
  "[main 3a1f2c0] Subject" line after a commit (`GitTexts.CommittedHash`). If git changes either, the announcement
  falls back to "Pulled" or "Committed" and nothing fails. The texts of a diff and of `show` are shown as they are.
  No error text is matched: a failure is the exit code, and git's own words go into the notice
  (`GitResult.FirstLine`, `AllText`). No prompt is read: git's terminal prompts are off.
- What guards it: the parser tests from recorded git output (`GitStatusTests`, `GitLogTests`, `GitBranchesTests`,
  `GitTrustTests`, `GitRemoteTests`), the runner tests with a fake `git.cmd` (`GitRunnerProcessTests`), and
  `GitTextsTests`, which fails when a window file holds an option or a word of git's, when the window matches git's
  output itself, or when a characteristic word leaves its file in the Core. The check scripts in `tests/scripts/axGIT`
  run the window against the real git on throwaway repositories.
- Before 2026-11-14 the window held six argument lists (`add -A`, `restore --staged .`, `commit -m`, `fetch --prune`,
  `pull --ff-only`, `push --set-upstream origin`), the search for "Already up to date", the pattern of the commit
  line, and the reading of the hooks folder line. All moved to `GitTexts` and `GitTrust.HooksFolder`.
- Still outside: nothing that is git's. The window passes a short word of its own with each command ("fetch",
  "status") for the status bar and the log; those are axKit's words. The recorded outputs are written inside the
  tests, not as files under `tests/fixtures`; a new git version is pinned by the check scripts, which run the
  installed git.

## axPass: KeePassXC's command-line tool

- The program: `keepassxc-cli.exe` of the user's installed KeePassXC, one process per command, the passwords on its
  standard input.
- Its one place: `src/AxPass.Core/KeePassCliTexts.cs` holds the tool's words: the commands (`ls`, `show`, `edit`,
  `add`, `rm`, `rmdir`, `mkdir`, `mv`, `generate`, `db-create`), the options, the marks of its paths (the slash, the
  root, `[empty]`, the Recycle Bin's name) and the names `show` prints. `KeePassCli` finds and runs the tool and
  builds a command's arguments (`On`). `PassTree` reads `ls -R -f`, `EntryFields` reads `show` and builds the options
  of `edit`, and `Backups.Changes` knows which commands change the database.
- What is read and how: the plain output of `ls` (one path per line) and of `show` ("Name: value" lines), the one
  line of `generate`, and the one value of `show -s -a Password`. No prompt and no error text is matched. Every
  command runs with `-q`, so the password prompt goes nowhere; the passwords go in on standard input in a fixed
  order. A failure is the exit code, and the tool's own words are said as they are (`CliResult.Why`). A wrong master
  password is known by a failure without words (`CliResult.SaidNothing`), not by the tool's "Invalid credentials".
- What guards it: `PassTests` with outputs as the tool printed them and one test that runs the real tool on a
  throwaway database when KeePassXC is installed; `UntrustedEntryTests`; `KeePassCliTextsTests`, which fails when a
  command's name, an option or a mark of the tool's is a string of any file but the table, or when something searches
  the tool's error text. The check scripts in `tests/scripts/axPass` run the window against the real tool.
  `docs/axPass/kdbx-notes.md` section 8 is the record of what the tool was seen to do.
- Before 2026-11-14 the window wrote the command names and options itself in about twenty places, and `Backups`,
  `EntryFields`, `PassTree` and `KeePassCli` each wrote the ones they needed. All read the table now.
- Still outside: the window works with the tool's paths as text. It splits a group's path at the slash for the
  level's name and for speaking ("Email, Gmail"), and its own key for a group ends in a slash. These are in
  `src/AxPass/PassForm.cs` and `PassForm.Commands.cs`. Moving them means giving `PassTree` the path operations and
  changing how the window keeps its place, which is larger than a move of words. The window also trims the line of
  `generate` and the value of the password before use. The file picker for the tool names `keepassxc-cli.exe` in its
  title and filter, as words for the user.

## axTerminal: the shells

- The programs: PowerShell 7, Windows PowerShell, Command Prompt, Git Bash, and any command line the user adds, each
  run on a pseudo console.
- Its one place: `src/AxTerminal.Core/ShellLauncher.cs` finds each shell and builds its command line, its
  environment, the prompt texts that make the shell print the integration marks, and its continuation prompt.
  `src/AxTerminal.Core/ShellTexts.cs` holds the keys sent (Enter, Ctrl+C, Ctrl+D, Escape, Up, Down), the letters of
  the marks, the field of the session's secret, and the facts about one shell or another that decide what is sent or
  said: PowerShell ends a block on an empty line, its prompt text gives -1 for an error without a code, and what the
  end of a row that asks looks like.
- What is read and how: signals, not screen text. Each shell is made to print the marks OSC 133 A, B and D (with the
  exit code, and with the session's secret so that a program's output cannot forge one) and OSC 9;9 with the
  directory. The bundle's screen (`src/AxKit.Core/Vt/Screen.cs`) parses the two sequences, and `TerminalModel` gives
  the marks their meaning. The shell's output itself is the transcript and is matched against nothing, with two
  exceptions that compare against known text: the continuation rows are hidden by the shell's continuation prompt
  followed by a line that was sent, and "a program asks" is a heuristic over the last character of the cursor row.
- What guards it: the recorded sessions `tests/fixtures/shell-*.vt` of Windows PowerShell, Command Prompt and Git
  Bash with their expected transcripts (`ShellFixtureTests`), `TerminalModelTests` with hand-written streams,
  `ShellLauncherTests`, and `ShellTextsTests`, which fails when a window file holds a key sequence, a mark or an
  option of a shell's, when a mark's word leaves the launcher, or when a prompt text stops printing the secret's
  field the model reads. The check scripts in `tests/scripts/axTerminal` run the real shells.
- Before 2026-11-14 the window wrote the key sequences, the test for PowerShell's extra Enter, the -1 code and the
  question heuristic itself, and the model wrote the mark letters and the secret's field. All read `ShellTexts` now.
- Still outside: the parsing of the two OSC sequences is in the bundle's screen, which both console windows share; it
  is the terminal protocol, not one shell's text. The secret's field is written inside the three prompt texts of
  `ShellLauncher` as well as in `ShellTexts`, since the texts are whole scripts; a test keeps them alike. PowerShell 7
  has no recording of its own; it shares Windows PowerShell's prompt text. A custom shell prints no marks and gets no
  state, by design.

## AI help: Claude Code in print mode

- The program: `claude -p`, one process per question, the prompt on its standard input.
- Its one place: `src/AxKit.Core/Ai`. `AiRequest` holds the command line (`-p --output-format json --allowedTools`,
  `--add-dir`, `--model`, `--resume`), the working folder, the time limit and the run. `AiAnswer.Parse` reads the
  answer. `AiPrompts` writes the prompt. `ClaudeCodeProgram` finds the program.
- What is read and how: JSON. The last JSON object on standard output, and of it three fields: `result`, `is_error`
  and `session_id`. No error text is matched: when there is no readable object, what was printed on either stream is
  said as it is, or the exit code.
- What guards it: `AiTests` in `tests/AxKit.Tests`, with answers as Claude Code printed them. The window code
  (`src/AxKit.Forms/AiHelpFlow.cs`) calls `AiRequest.AskAsync` and reads the `AiAnswer` record only.
- Still outside: nothing was found in a window. There is no source-reading test for the three field names yet; they
  stand in one method, and the audit left the folder as it was because other work was changing it that day.

## axMCP: MCP servers

axMCP's servers speak a protocol, not screen text, so the one place is the protocol code. `McpClient` in
`src/AxMCP.Core/McpClient.cs` is the Model Context Protocol as a client speaks it: JSON-RPC 2.0, one message per
line, the protocol version, every method name (`initialize`, `tools/list`, `tools/call`, `resources/read`,
`prompts/get` and the rest) and the reading of each answer into records. `StdioServer` starts a server and wires its
pipes to the client, and `ResultText` turns a result's content into text. The window reads the records. It is
guarded by `MCPTests`, which drive the client with a fake server in memory, `StdioServerTests`, and the check scripts
in `tests/scripts/axMCP`. The other direction, axKit's own AI tools that an AI client calls, is
`src/AxKit.Core/Mcp/McpProtocol.cs` (`mcp.md`), guarded by `McpTests` and `tests/scripts/mcp`.
