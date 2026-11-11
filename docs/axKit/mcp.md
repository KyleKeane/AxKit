# axKit over MCP: every feature to a client the user allowed

Asked by the user on 2026-10-24: "programmatic access to all the features with very well defined MCP APIs but with
absolute protection". Specification first (the user's order: spec, infrastructure, build); the first stage, read and
open, is built (section 7, 2026-10-24), and since 2026-11-11 one tool runs a window's command (section 8); typed data tools are not.
It depends on two things before it: the security fixes of `security-audit-2026-10-24.md` (S-11 to S-13 above all),
and the command lists of `app-kit.md`, which are what the tools are made from. Requirements are numbered MC-n.m.

## 1. Goals and non-goals

- G1. An AI client (Claude Code first, any MCP client) can do in axKit what the user can: open an app on a path, list
  and read what a window shows, run any command a window's menus have, and hear back what the window said.
- G2. Only a client the user paired, only the commands the user allowed for it, every change heard as it happens,
  every call in an audit log the user can read in a window.
- G3. One definition: a tool is a window's command (`app-kit.md`), so a new app's commands are tools without a line of
  MCP code, and a command the user cannot see in a menu cannot be called.
- Non-goal: raw keys, typed characters or screen reading as tools (the test channel's `keys` and `type`). They make
  every safeguard below moot.
- Non-goal: any network listener. Not TCP, not HTTP, not on localhost: a web page can reach localhost.

## 2. Shape

- MC-1.1 **The broker**: `AxKit.exe mcp`, started by the MCP client as its child, speaking MCP over standard input and
  output (JSON-RPC 2.0, the stdio transport). It is not a window and holds no state of the apps; it is configured in
  the client as `{"command": "<installed AxKit.exe>", "args": ["mcp"]}`.
- MC-1.2 **The windows' side**: every axKit window serves one named pipe, `\\.\pipe\AxKit-<user SID>-<random>`, made
  with an access list for the current user's SID alone, remote clients refused, written with its random name into a
  per-user file only the user can read. The pipe answers only a client whose process is the installed `AxKit.exe`
  (`GetNamedPipeClientProcessId`, its image path), that is, the broker. The same pipe replaces the hand-over message
  of a second start (S-13).
- MC-1.3 The test channel (`automation.md`) stays a separate, test-only tool behind its own secret (S-11); MCP is never
  built on it.

## 3. Pairing and authentication

- MC-2.1 **Off by default.** axKit menu → AI tools → "Let AI tools read and open axKit" in every window (MC-6.1), one setting for the
  bundle. Off: the broker answers every call with "axKit's MCP access is off" and nothing else.
- MC-2.2 **Pairing**: the first call of a client that is not paired shows a notice in the axKit window in front (or
  opens axStart's): "Claude Code asks to use axKit. It can: <the scopes asked>. Allow?", with Allow and Deny; Enter on
  Allow pairs, nothing else does. The client is recorded by its process's program file and its command line. The
  pairing key (256 bits) is kept for the user with DPAPI in `%LOCALAPPDATA%\AxKit\mcp.key`; each connection proves it by
  a challenge and HMAC, never by the key itself.
- MC-2.3 **Forget**: axKit menu → AI tools → Forget AI tools lists the paired clients with what each may do; Delete forgets one.
  Let AI tools read and open axKit, unchecked in the axKit menu's AI tools, turns the setting off at once and ends every connection.

## 4. Tools and what each may do

- MC-3.1 **Scopes**, granted per client and per app at pairing and changed in "MCP clients…":
  - **read**: list the apps and their open windows; a window's title, status and focused item; a list's items; a
    text's lines (an editor's, a viewer's, a transcript's); what the window said lately.
  - **open**: start an app on a path or none, bring a window to the front without taking the keyboard from the user.
  - **change**: a command that changes the app's own state or a file the user opened in it (save, rename, stage, add to
    the queue, a field of an axTask item).
  - **act**: a command that acts outside the window: delete, move, run in axTerminal, send a message to Claude in
    axClaude, push, start a program, install, update.
- MC-3.2 **Tools are commands.** Every command of a window's command list (`app-kit.md`) is one tool, named
  `<app>.<command>` from its menu name ("axfile.delete", "axgit.commit"), with its scope from the command's own mark,
  its arguments typed (a path, a name, a text), its description from its help text. `tools/list` lists only the tools
  the client's scopes allow. Arguments are checked as the menu checks them: the same refusal says the same words.
- MC-3.3 **Confirmation**: an **act** tool is never run on the client's word alone. The window shows a notice naming
  the client, the command and its arguments ("Claude Code asks to delete notes.md in C:\work. Allow?"); only the user's
  own Enter on Allow runs it, a key the client cannot press (no key tool exists). "Allow for this session" is offered
  per tool, never for delete, install or update.
- MC-3.4 **Paths**: a path argument must be on this computer (`LocalPaths`), never a share, and inside the folders the
  user granted the client ("MCP clients…", folders); a path outside asks as an act tool does.
- MC-3.5 **Limits**: at most 10 calls a second per client; a text read is at most 1 MB per call.

## 5. What is never exposed

- MC-4.1 axPass: no tool reads or copies a password, the master password, a key file, notes, a generated password or a
  field marked secret. With the read scope a client may know whether a database is open and locked, and the titles of
  entries in the group shown, only after a pairing that names axPass explicitly. No tool unlocks.
- MC-4.2 axClip's history contents; the clipboard itself.
- MC-4.3 Any field the window marks secret (`IsSecret`, S-12), in any app.
- MC-4.4 The settings files, the logs, the pairing key, the audit log's writing.
- MC-4.5 A test fails when a tool of axPass exists beyond MC-4.1, or when a command marked secret is listed.

## 6. Heard and recorded

- MC-5.1 Every change or act tool run is announced in the window it acts on, as a user's command is, prefixed with the
  client's name: "Claude Code: deleted notes.md". Reads are silent, counted in the status bar ("AI read 3 times").
- MC-5.2 The audit log, `%LOCALAPPDATA%\AxKit\mcp-audit.log`: the time, the client, the tool, a hash of the arguments
  and their kinds, the result, whether the user confirmed. axKit menu → AI tools → Read the log opens it in
  axPreview.

## 7. The first stage as built (2026-10-24, the branch `app-kit`): read and open

Step 3 of section 8, with the choices it needed; what is not here waits for the later steps.

- MC-6.1 **The setting**: `%APPDATA%\AxKit\mcp.json`, `{"enabled": false, "clients": []}`, one for the bundle, so it
  is in the bundle's menu, never an app's Options (the user's rule of 2026-10-24: Options are the app's, the axKit
  menu holds what is the bundle's): axKit → AI tools ▸ (I) in every window, with "Let AI tools read and open axKit"
  (checked while on), "Add axKit to Claude Code", "Forget AI tools" and "Read the log". Turning it on asks once, a
  notice saying what it allows, with **Turn on and add to Claude Code** (the default, when Claude Code is found),
  Turn on only, and Cancel; turning it on makes the key (MC-6.3). Adding to Claude Code (MC-6.8) runs Claude Code's
  own `claude mcp add` for this user's every folder with this very `AxKit.exe` and the argument `mcp`; without
  Claude Code the notice gives that command line to copy. Turning it off needs no question and says "AI tools off";
  since 2026-11-11 it turns Let AI tools run commands (MC-7.2) off with it, and says so, so that turning the access on
  again never brings running commands back unsaid.
  "Forget AI tools" forgets every paired client at once and turns Let AI tools run commands off (every permission back,
  MC-D9); "Read the log" opens the audit log in axPreview. Off, every window refuses every
  connection before anything else, so that turning it off in one window ends every client's access at its next call.
  The file carries a seal, HMAC-SHA256 of the setting and the clients under the key (MC-6.3); a file whose seal does
  not check, written by hand or by an AI tool's own file writing, reads as off with no clients, so that turning the
  access on or pairing takes code that runs as the user, not a written file (the review of 2026-10-24).
- MC-6.2 **The windows' pipes**: every window serves its own pipe, `\\.\pipe\AxKit-mcp-<user SID>-<random>`, for the
  current user alone and refused over the network (the access list of S-11). The broker finds them by listing the
  pipes with that prefix, so no file names them. A window of axPass or axClip serves none (MC-4.1, MC-4.2): those apps
  are not reachable at all in this stage. The test channel stays apart (MC-1.3).
- MC-6.3 **Both ends prove who they are**: the window answers only a client whose process runs this very program file
  (`GetNamedPipeClientProcessId`, its image path, as the hand-over checks, S-13), and the broker talks only to a pipe
  whose server process runs it (`GetNamedPipeServerProcessId`): a broker reads only the windows of its own
  AxKit.exe, so a checkout's build and an installed one, side by side, each have their own windows and client entry. Then the window sends a fresh random challenge and the
  broker answers it with HMAC-SHA256 under the key, `%LOCALAPPDATA%\AxKit\mcp.key`: 32 random bytes kept with DPAPI for
  this user (`UserSecret`); then the broker sends a challenge of its own and the window answers it, so that a pipe of
  another user's copy of the same `AxKit.exe` cannot pass for this user's window. The key itself never crosses the
  pipe; the broker connects at the Identification level, so a window can never act as the user it serves. A line
  longer than 64 KB is not read; a request waits at most ten seconds for the window's thread; whatever fails is the
  call's answer and never ends the window's process or the broker.
- MC-6.4 **Pairing**: a client is known by the program file of the broker's parent process (the client that started
  it), with the name it gives in `initialize` for display only. The first tool call of a client not paired asks the
  newest axKit window: a notice "<name> asks to use axKit", its text "<name> asks to use axKit. It runs as <program>.
  It can read what the axKit windows show (their text, lists and status) and open apps. It runs a window's commands only
  while AI tools, Let AI tools run commands is on, each said as it runs, and it never reads axPass or axClip." (until
  2026-11-11 the text said "it cannot change or delete anything"; a client paired under those words keeps its pairing,
  since running commands needs the second switch, off by default, which the user turns on with its own announcement,
  MC-7.2), with Allow and Deny, and since 2026-10-29 (bundle AX-11.5) the
  text that axKit is only the access layer and the check box "I understand that axKit is only an access layer for
  <name>, and I take full responsibility for knowing and following the terms of <name>", which Allow needs (Deny stays
  the default), the consent kept in consent.json and withdrawn by Forget AI tools; the window flashing on the taskbar when it
  is not in front. The broker waits up to two minutes for the answer; Deny, or no answer, refuses the call. With no
  axKit window open, the call is refused with "Open any axKit window to allow <name>"; a window that shows a notice
  already shows no pairing (a notice replaces the one shown, which the user may be answering) and the call is refused
  with "ask again once it is answered". An unanswered notice is closed when the call stops waiting, and its Allow then
  pairs nothing; after a Deny the same broker asks no more for a minute. A program that runs whatever script it is
  given (cmd, PowerShell, Windows Script Host, Python, mshta, rundll32, Explorer: `McpAccess.RefusedHosts`) is never
  paired. The name is the client's own word, its hidden characters made visible (`DisplayText`). What a window
  announces is kept for `axkit_window_said` only from the first call that proved the key, never from before.
  The limits, said plainly: a pairing trusts every process of that program file (every script `node` runs, when the
  client is a `node` program), and a program of the same user can start `AxKit.exe mcp` itself, even with a forged
  parent process that names a paired client. Against code already running as the user there is no absolute
  protection here, nor anywhere on Windows: such code can read every window through UI Automation without axKit at all.
  This is why this stage reads and opens only, and why change and act tools will ask at every call.
- MC-6.5 **The tools** (all `read` but the last): `axkit_apps` (the apps and what each opens), `axkit_windows` (the
  open windows that serve a pipe: an id, the app, the title), and for one window by its id `axkit_window_state` (title,
  status bar, the notice shown and its text, the focused control and its value, a secret field as "protected"),
  `axkit_window_list` (the focused list's items, or the window's first list), `axkit_window_text` (the focused text's
  lines, or the window's main text, at most 1 MB), `axkit_window_commands` (the window's command list: label, key,
  mark, and since 2026-11-11 the menus that hold it; MC-7 runs one) and `axkit_window_said` (what the window announced since the last call, the
  last 50). `axkit_open` (`open`): an app by its verb, on a path or none, but axClaude and axTerminal, which start a program as they open (an act, MC-3.1); the path fully qualified, on this computer
  (`LocalPaths`), through no link (a junction or a symbolic link, which may lead to a share; asked part by part
  before anything follows it), existing, and of a kind the app opens (`Dispatch.PathFor`); started as
  `AxKit.exe <verb> <path>` with the arguments as a list, through ShellExecute on the program file itself, never through
  a command interpreter (ShellExecute hands the new window none of the broker's handles).
- MC-6.6 **Limits and the log**: at most 10 calls a second; every call in `%LOCALAPPDATA%\AxKit\mcp-audit.log`, one
  line: the time, the client's name and program, the tool, the first 16 hex digits of the SHA-256 of its arguments,
  and the result (done, refused and why, failed). The arguments themselves are not written.
- MC-6.8 **Added to Claude Code**: Claude Code found where axClaude finds it (`AxKit.Core/ClaudeCodeProgram`, one
  search for both since 2026-11-09, axClaude FR-1.2: `claude.exe` or `claude.cmd` in every PATH folder, then its
  installer's `%USERPROFILE%\.local\bin\claude.exe`, then npm's `%APPDATA%\npm\claude.cmd`; a `.cmd` runs through
  cmd); run without a window, off the window's
  thread, with the arguments as a list: `claude mcp remove --scope user axkit` (an earlier entry, perhaps of another
  build, gone; its failure when there is none is ignored), then `claude mcp add --scope user axkit -- <AxKit.exe> mcp`.
  The result is said: "axKit is added to Claude Code: a new Claude session can use it", or Claude Code's own error
  line. Never in a test run, which must not touch the user's Claude Code settings.
- MC-6.7 **Not in this stage**: change and act tools and their confirmation (MC-3.3; since 2026-11-11 section 8), the "MCP clients…" list with
  scopes per app and folders (MC-2.3, MC-3.4), the reads counted in the status bar (MC-5.1), axPass's titles (MC-4.1).

## 8. Running a window's command (2026-11-11, the branch `push2/mcp-run`)

The first part of the user's request of 2026-11-10, "everything in axkit should be very clearly operational via mcp
servers so that you can perform any and all operations yourself directly": one tool that runs a named command of a
window, so that every menu command of every app is reachable at once. Typed tools for data without a window (an
axTask item made in a folder) are the next part.

- MC-7.1 **The tool** `axkit_window_run`: a window's id from `axkit_windows`, a command's label as
  `axkit_window_commands` lists it (and the palette shows it; the letter's marker, the trailing dots and case do not
  matter), and, where two commands share a label (Heading under Next and under Previous), `menu`, the menus that hold
  it as `axkit_window_commands` now lists them ("Navigate, Next"). Only the window's own command list (app-kit AK-2)
  is searched, and only a command with a menu item the palette would offer now (its menus refreshed as on opening,
  the item and every menu over it available and enabled): a key with no item, or an item disabled at the moment, is
  refused with its reason. It runs on the window's thread as its menu item would (`PerformClick`), posted so that the
  answer never waits on a command that shows a Windows dialog; the answer, after the window has been quiet for 0.7 s
  or at most 2.5 s, gives what ran (`ran`, only for a command the window took up and ran; `notRun` with the reason when
  a notice, a dialog, the switch or a disabled item stopped it in between; `unknown` when the window had not taken it up
  yet), what the window said meanwhile (as `axkit_window_said` reads it), the notice it
  now shows the user with its text, or that a Windows dialog waits for the user. The window's pipe answers it as
  every request (MC-6.3); the broker waits up to 25 s for it.
- MC-7.2 **The switch**: axKit menu, AI tools, **Let AI tools run commands** (U), off by default, checked while on, one
  for the bundle, kept in the AI tools' sealed setting (`mcp.json`, `runCommands`, part of the seal: a file turned on
  by hand or by an AI tool's own file writing reads as off, MC-6.1). Turning it on or off is said at once ("AI tools
  may run commands: each is said as it runs, and one that acts outside its window asks you first"; "AI tools may no
  longer run commands"); it counts only while the access itself is on. Off, the tool answers "The user has not allowed
  AI tools to run commands in axKit. The user allows it in any axKit window: axKit menu, AI tools, Let AI tools run
  commands." The pairing (MC-6.4) applies first, as for every tool. The switch is an item of the bundle's menu, never
  a command of a window's list, so that no AI tool can run it, nor Let AI tools read and open axKit, nor Forget.
- MC-7.3 **What it never does**: it never answers a question for the user. While the window shows a notice (a
  question, an error, the palette, a pairing) or a Windows dialog, every run is refused ("The window shows the user a
  notice, <title>: commands run once the user has answered it, and AI tools never answer it"); a command that asks
  (delete, discard, close with changes, New folder's name) shows its question as for the user and the answer says the
  window asks it. A command marked **act** (app-kit's mark, MC-3.1: it deletes, moves, sends, starts or ends
  something outside the window) is asked of the user first, in the window: the notice "Claude asks to run Delete",
  its text naming the command's menus and what an act may do, unprompted with its chime, the window flashing on the
  taskbar when it is not in front, **Deny** the default and Escape, **Run** running it once (and only while the switch,
  read again off the window's thread, is still on and the item still available; otherwise "<command> not run:
  <reason>" is said); after a Deny the window refuses an act's run without asking for a minute, as the pairing does; the tool's answer says
  "The window asks the user whether to run Delete". No key tool exists, so only the user's own key answers.
- MC-7.4 **Never in axPass** (and axClip): their windows serve no pipe (MC-6.2), so they are not reachable at all;
  should one ever be, the window refuses with its reason ("axPass's commands are never run by AI tools: its windows
  hold passwords"), `McpRun.NeverRun`.
- MC-7.5 **Heard**: as the command runs, the window says who ran it, "Claude ran Word wrap" ("Claude" for Claude Code
  by one of its own client names, `claude-code`, `Claude Code` or `claude` in any case, matched whole, so that
  "Claudette" is said as Claudette; otherwise the name the pairing recorded, its hidden characters made visible,
  at most 40 characters), joined to the command's own words in one notification ("Claude ran Word wrap. Word wrap
  off", AX-7.3), so that the command's interrupting announcement never cuts it off; alone when the command says
  nothing. NVDA does not speak the notifications of a window that is not in front, so a run in such a window also
  plays the notice chime and flashes the window on the taskbar until the user comes to it; its words are then in the
  window's recent actions and in `axkit_window_said`, not spoken. The run is in the window's recent actions as a
  command is (reports RP-4.1), and every call is a line of the audit log (MC-6.6).
- MC-7.6 **What a client can reach**: nothing it could not already read. The tool goes through the same pipe, the same
  proof of the key both ways and the same pairing as the reads; a window id is a pipe's random part; a window of
  another build or another user is never asked (MC-6.3). The broker checks the switch for its answer's words, and
  the window's process checks it again from the sealed file at every run (on the pipe's thread, so that the window's
  thread never waits on the file, bundle rule 3), which is the check that counts. A command that would bring into a
  window what the tools could not read before (axPreview's Read the clipboard's picture) is marked act, so it runs only
  on the user's Run (MC-D7).
- MC-7.8 **Never under the user's hands**: a command runs as its key would, so it may move the caret or the focus
  (Navigate's jumps, Message field, Find). While the window is in front and the user pressed a key or moved the mouse
  within the last 3 seconds (`GetLastInputInfo`), every run is refused: "The user is working in this window right
  now ...; try again in a moment" (`McpRun.IsUserBusy`). A window behind is never typed in, so it runs there, heard as
  MC-7.5 says.
- MC-7.7 **Not in this part**: arguments for a command (a path, a name, a text: a command that needs one asks the
  user in its own notice), typed data tools without a window, the scopes per client and app of MC-3.1, "Allow for this
  session" (MC-3.3).

Decisions taken for the user while away (2026-11-11), each one line to reverse:

- MC-D1 One tool, `axkit_window_run`, by the command's label (and its menus where two share one), rather than one
  tool per command (MC-3.2): the commands differ by window and change with the app, and one tool keeps `tools/list`
  short; MC-3.2 stays the later shape if clients need typed arguments.
- MC-D2 Off by default behind its own switch in the axKit menu's AI tools, sealed with the access setting; the pairing
  applies on top. Turning it on asks nothing more than the menu item, since it is the user's own key and is said.
- MC-D3 Never while a notice or a Windows dialog shows; an act command asks the user first with Deny the default
  (MC-3.3 as specified); everything else runs on the client's word and is heard.
- MC-D4 The window says "Claude ran <command>" (or the paired client's name) joined to the command's own words.
- MC-D5 Only commands with a menu item the palette would offer now; keys without an item (Tab between panes) are not
  run, since their meaning depends on the focus, which the client does not hold.
- MC-D6 No new network contact and no new program start: the run is a command of the window, which may itself reach
  what the user's own command would (git's Push, Claude Code's restart), and an act command asks first.

After the review of 2026-11-11:

- MC-D7 **Every command states its mark, and a program start is an act.** `Command.Mark` still defaults to change,
  but `tests/AxKit.Tests/CommandMarkTests` fails on a `new Command(...)` without a `Mark`, so a new command never runs
  unasked by forgetting it; the same test pins the act commands that start a program or reach past the window:
  axFile's Open (a file opens in its program, a script or a program run itself; a client that wants a folder opens
  axFile on it with `axkit_open`), axHelp's Open the conversation and Put a picture file into the conversation (they
  start Claude Code, as MC-6.5 refuses for `axkit_open`), axPreview's Read the clipboard's picture (MC-7.6), axGIT's
  Stage or unstage, Stage all and Unstage all (the repository changes), axStart's Start, the console windows' New
  session and restarts, axMCP's Call. Copies to the clipboard are act too (the user's clipboard is outside the window).
- MC-D8 **An exception to AX-7.1's "the caret moves only on the user's own command"**: a run moves the caret or the
  focus as the command's key would, since the user allowed it with the switch and hears who ran it; MC-7.8 keeps it
  from happening under the user's hands. Said in the guide.
- MC-D9 **Off and Forget take the switch with them**: turning the access off and Forget AI tools both turn Let AI
  tools run commands off and say so; turning the access on again leaves it off.

## 9. Built in this order

1. The security fixes S-11 to S-13 (the channel's secret, `IsSecret`, the hand-over pipe).
2. The command lists of `app-kit.md` in the first apps, with their scope marks.
3. The windows' pipe and the broker with read and open tools only; pairing; the audit log.
4. Change tools, then act tools with their confirmation notice:
   `axkit_window_run` (section 8, 2026-11-11) runs every menu command, an act one after the user's Run; typed data
   tools next.
5. An NVDA pass of pairing, confirmation and the announcements; then on by the user's choice.
