# axHelp, access layer for Claude Code: specification

Status: built 2026-11-09 on the branch `ai-help` as a question field over Claude Code's print mode; rebuilt
2026-11-10 (5.7.0) at the user's word as two hotkeys into one conversation in axClaude, with axDescribe merged in.
Requirements are numbered HE-n.m. The bundle's rules are `docs/axKit/SPEC.md` (AX-7 accessibility, AX-8 keys, AX-11
access layers); axClaude's are `docs/axClaude/SPEC.md` (FR-n.m), whose window the conversation is; "SPEC.md" in axHelp's
code means this file.

## 1. Summary

axHelp is a conversation with Claude from anywhere on the computer. One hotkey (Ctrl+Shift+Slash, the question mark's key) brings it up: an
axClaude window on Claude's own working folder, titled "axHelp", with the focus in its message field. It is one
conversation that goes on until the user clears it, read with axClaude's keys, its replies spoken as axClaude speaks
them, and Claude Code in it may do work on the computer, asking before each change. A second hotkey (Ctrl+Shift+Period)
takes a picture of the window in front and drops its path into that conversation's message field, so that a question
about the screen is one key and a sentence. `AxKit.exe help [--hidden | <picture>]`, Start entry `axKit axHelp`,
command `axHelp`; a background process from sign-in, as axStart and axClip. It is an access layer (AX-11.1): Claude
Code, Anthropic's program, does the work under the user's own account and Anthropic's terms, with axClaude's own
agreement (AX-11.4) asked the first time.

## 2. Goals and non-goals

- G1. One key from anywhere, and the conversation is there, in the state the user left it.
- G2. Just like axClaude, because it is axClaude: no second chat window to build, learn or keep right.
- G3. Fast by default: axHelp's own model, the fast one, whatever Claude Code's default is; a stronger model only for
  work that needs it, through a subagent (HE-2.4).
- G4. Decoupled from the user's projects: its own folder, its own conversations, its own permissions.
- Non-goal: a print-mode answer in a notice. That is Shift+F1 in every window (`docs/axKit/ai-help.md` AH-6).

## 3. Scenarios

- In Word, Ctrl+Shift+Slash: "axHelp" and the message field. "how do I make a table of contents", Enter; the reply is
  spoken as it comes. Later, Ctrl+Shift+Slash again: the same conversation, the earlier answer still there under h and the
  reply keys.
- A dialog NVDA reads badly: Ctrl+Shift+Period. The conversation comes up with "Screenshot of "Save changes?": C:\...\
  2026-11-10 14.02.11.png " in its field; type "which button is the default?", Enter.
- "Move every PDF in my Downloads folder into a folder named Papers": the conversation hands it to its agent, Claude
  Code asks before it moves anything, and says what it did.
- Start menu, axKit axHelp: axHelp's own window, a text naming the two hotkeys; Enter opens the conversation.

## 4. Requirements

### HE-1 Launch, the background process, the hand-over

- HE-1.1 `AxKit.exe help` shows axHelp's window and keeps running; `--hidden` runs it hidden until a hotkey, as at
  sign-in (`HotkeyForm`, axStart AS-1.1, AS-1.3); `AxKit.exe help <picture>` puts the picture into the conversation
  (HE-3.3) without showing the window. Start with Windows (Options, General) writes the Run key. In the Store's copy
  axHelp has no startup task (bundle AX-13.2) and is started from any window's Window menu, Access layers, or by its key.
- HE-1.2 One axHelp at a time (a named mutex, `AppPaths.OneAtATime`): a second start hands "show", or "open" with its
  picture, to the running one's window (`AppMessage`), found by its title "axHelp hotkeys" (never "axHelp", which is the
  conversation's), and ends; with `--hidden` it just ends. A running axHelp that does not answer is asked about:
  "axHelp is running but does not answer. End it and start again?", No the default (bundle AX-1.9); only axHelp's own
  process is ended, never the conversation's window, which is axClaude's.
- HE-1.3 Alt+F4 and Close all windows hide axHelp's window; Session, Quit axHelp ends the process. **The conversation
  hides too** (2026-11-15; the fault the user found: Alt+F4 on the conversation, then the hotkey, started a new
  axClaude that loaded Claude Code and replayed the conversation). It is an axClaude window started with
  `--hide-on-close` (axClaude FR-9.6): Alt+F4, its close command and Close all windows hide it, without a question,
  Claude Code running on with everything as it was, and the hotkeys bring that same window back (HE-2.1). While
  hidden it is in no Window menu, not in Alt+Tab and not counted by Close all windows; the AI tools do not list it;
  it says nothing and plays nothing: a reply that arrives is read in the conversation when it is shown, and a
  question of Claude's waits and is asked then. Its own Session menu ends it: End Claude and close this window, with
  axClaude's question while Claude is in the middle of something (FR-1.7). **Quit axHelp asks the conversation to end
  with it**, hidden or on view (the "end" hand-over), as does axHelp's close for an update: with axHelp gone nothing
  would bring a hidden conversation back. It is a request, and nothing is lost without a word (axClaude FR-9.6): a
  conversation with nothing to lose ends at once, unheard; one where Claude is at work or waits for an answer comes
  to the front with axClaude's "Close axClaude?" (FR-1.7), and one with a message typed and not sent, or an offered
  command not answered, with "Close this conversation?", whose Enter keeps it open. axHelp itself ends either way,
  since its own process holds nothing; a conversation the user kept stays on view as a window of its own and from
  then on closes on Alt+F4 as any axClaude window. A conversation axHelp started a moment ago whose window does not
  exist yet has its process ended with axHelp (`Conversation.AtQuit`), so that none is left running unseen. Windows'
  own end closes both. The cost, said in the guide:
  Claude Code stays in memory while the conversation is hidden.
- HE-1.4 `AxKit.exe describe`, axDescribe's verb until 5.7.0, starts axHelp with the same arguments (`Dispatch.
  FormerDescribe`), so that an older sign-in start or script still works; axHelp removes axDescribe's Run value at its
  start.

### HE-2 The conversation

- HE-2.1 **The conversation's hotkey** (and Enter in axHelp's window) brings up the conversation. The window titled
  "axHelp" of this program, on view or hidden (`HotkeyForm.FindRunning`, which finds hidden windows; the Window
  menu's list does not), is handed "show" (`AppMessage`, which passes on the hotkey's right to the front) and brings
  itself up: made visible when it was hidden, in front, **the focus in the message field, always**, whether it was
  hidden, behind other windows or in front with the focus in the conversation (the user, 2026-11-15: "the global
  hotkey always places the caret in the message field"). While Claude waits on an answer, the question's list stands
  in the message field's place and takes the focus; a notice on view keeps it. Showing is the same window made
  visible: nothing is started, rebuilt, replayed or read out again, and NVDA says only what it says for a window
  coming to the front (the user: "as snappy and responsive as possible, the window loading with content filling
  should not be involved"). Coming back to the window another way, Alt+Tab or the taskbar, changes nothing: the focus
  and the caret are where they were ("alt+tab back to it holds his position"). When no such window exists, a new
  axClaude window is started, `AxKit.exe claude <working folder> --title axHelp --model <model> --hide-on-close
  --ai-tools --source screen` (`HelpSettings.ConversationArguments`; `--ai-tools` is HE-2.8; `--source screen` keeps
  the conversation on Claude Code's screen whatever axClaude's own setting says, docs/axClaude/stream-mode.md
  SM-9.5), which continues the folder's last conversation (axClaude FR-9.1); axHelp
  keeps the process it started, and a press while that process runs and its window is not there yet waits for the
  window, however long the start takes, and never starts a second (`Conversation.Next`: hand over to the window,
  wait for a started process, start only when there is neither; one conversation at a time in every order of start,
  press and quit). axHelp's own window hides first.
  Everything in the conversation is
  axClaude's: the message field, the transcript and its keys, the replies spoken by Options, Speech, Speak replies, the
  questions Claude asks, Session, New session to clear it, Claude Code's `/resume` for the folder's earlier
  conversations.
- HE-2.2 **axHelp's window** is one read-only text, named "axHelp", that says what the two hotkeys do as they are set,
  where Claude works, and whether the screenshot's hotkey is taken; the menus hold the rest (HE-6). It is shown by a
  start without the hotkey, never by a hotkey.
- HE-2.3 **The model.** The conversation starts with axHelp's own model, `model` in `axHelp.json`, the fast one
  (`haiku`) by default; Options, Conversation model offers Fast (Haiku), Balanced (Sonnet), Strongest (Opus). Given as
  `--model` at the start, it wins over Claude Code's saved default, so a model chosen elsewhere never changes axHelp's;
  a change counts from the conversation's next start (its Session, End Claude and close this window, then the
  hotkey: closing alone hides it, HE-1.3). `/model` typed inside the conversation changes that session and
  is saved by Claude Code as its default for new sessions everywhere, axClaude's included; the guide says to choose
  axHelp's model in its Options instead. The file's `model` is kept only when it is a model's name
  (`HelpSettings.IsModelName`: `^[A-Za-z0-9][A-Za-z0-9._\[\]-]*$`, a hundred characters at most, so `haiku`,
  `claude-sonnet-4-5` and `opus[1m]`); anything else is the default, with a line in the log that holds the value's
  length and not the value, and nothing said. The name goes onto Claude Code's command line, where a value that
  starts with a hyphen would be read as an option (bundle `repairs.md` R-71, 2026-11-16);
  `HelpSettings.ConversationArguments` checks it once more.
- HE-2.4 **The working folder** is AI help's (`ai-help.md` AH-2.1, AH-D6): `Documents\axKit\Claude` unless
  `ai-help.json` names another. Claude Code keeps per folder the conversations, the "always allow" answers and the
  CLAUDE.md, so nothing of axHelp reaches the user's projects. Before the conversation's start axHelp writes, each only
  when missing (`WorkingFolderFiles`): `CLAUDE.md` (short spoken replies; a message starting "Screenshot of" or
  "Picture" names a picture file to read first; work of several steps on the computer goes to the subagent) and
  `.claude\agents\agent.md`, a Claude Code subagent named agent on the strongest model (`model: opus`). The user's own
  changes to either stay. What axHelp itself puts into the folder has a place: the two files above, the folder
  `screenshots` (HE-3.1) and axKit's `documentation` (HE-2.6) with its two rule files under `.claude\rules`
  (the guides', HE-2.6, and the AI tools', HE-2.8); nothing of
  axHelp's lands loose at the top. (The conversation's Export, axClaude's, proposes the folder in the Windows save
  dialog, as for any project; where the file goes is the user's choice there.)
- HE-2.5 **Permissions.** The conversation is Claude Code in its interactive mode: it may read, write and run commands
  on the computer, and asks before each change unless the user allowed it (axClaude's questions, FR-2.x; the mode with
  Ctrl+Shift+M). This is the agent the user asked for on 2026-11-10; its power is Claude Code's, under the user's eye.
- HE-2.6 **axKit's guides** (the user's ask of 2026-11-15: "a better developed RAG system by copying the relevant
  documentation for axKit into the axHelp folder"; the whole of it is `ai-help.md` AH-7, since Shift+F1 uses the same
  folder). The working folder holds `documentation`: `index.md`, `axKit.md` (the bundle's guide) and one guide per
  app under its name (`axFile.md`); beside them the workflow pages (`docs/axKit/user-workflows.md` WF-8):
  `workflows.md`, what a user can do in each app as workflows and their goals, one `workflows-<page>.md` per group
  with the steps of its goals and their keys (`workflows-axFile.md`, `workflows-ai-tools.md`,
  `workflows-nvda-only.md`), and `window-commands.md`, every command of every window with its menu and its key; the
  two key pages; and, since 2026-11-17, `families.md` (what each sound and event means), `cases.md` (what axKit
  does when something goes wrong), `outside-calls.md` and `privacy.md` (what axKit contacts and runs) and
  `changelog.md` (what changed in each version), which the rule names by the question each answers (`ai-help.md`
  AH-7.1, AH-7.3); and
  `.claude\rules\axkit-documentation.md`, which Claude Code loads at every start beside CLAUDE.md and which tells
  Claude: for a question about axKit or one of its apps, read the index and the fitting guide first, answer from it,
  and say when the guides do not cover it; what an app can do, from `workflows.md`, naming the workflows and goals
  that fit; how to do something, by the goal's steps as written in the app's workflows file, keys included, the
  checks' example names said to be examples and replaced by the user's own, nothing else changed; never a key or a
  menu that no workflows file, `window-commands.md` or guide names, `window-commands.md` deciding where a guide
  differs; and, where no goal covers the question, the app's guide and `window-commands.md`, saying when it is not
  sure; then, once, an offer to carry the steps out through the AI tools, with the caution of
  `AiCaution.OfferToDo`, nothing run before the user's yes (`ai-help.md` AH-7.3b). These files are axKit's, unlike the
  two of HE-2.4: written where missing, written again where one no longer holds axKit's text (`ai-help.md` AH-7.7:
  compared with the text built into axKit, in code alone, so that a file a user, another program or Claude itself
  changed cannot mislead an answer) and written again by each new version of axKit (the marker
  `documentation\axkit-version.txt`), at every use of the folder (the hotkeys, Enter, Claude's working folder); a
  file of the user's own beside them is left alone, and the user's CLAUDE.md is never read or changed for it. A new
  folder's CLAUDE.md names the guides too (`WorkingFolderFiles.Instructions`). Nothing is indexed and nothing leaves
  the computer for it: Claude Code's own Read, Grep and Glob find the text, and a guide goes to Anthropic only as
  any file Claude reads for an answer does. `AxKit.Core/Ai/AiDocumentation`; `AiHelpFlow.AiWorkingFolder` writes.
- HE-2.7 **Ready before the first press, when the user turns it on** (2026-11-15). The setting Options, Keep the
  conversation ready in the background (`keepConversationReady`) is **off at first** (HE-D9): Claude Code starts at
  the first press of the hotkey and never before the user acts. There is no prompt, no first-run question and no
  reminder about the setting: it is in Options and in the guide. With it on, when axHelp starts hidden (`--hidden`:
  sign-in, `run.ps1`), it starts the conversation's window hidden as well (`--hide-on-close --hidden`, axClaude
  FR-9.6), so that Claude Code is loaded when the hotkey is first pressed. Only when all of these hold
  (`HelpSettings.StartsConversationHidden`): the setting is on; the user has already agreed to axClaude's reminder for Claude Code (read
  from the bundle's consent file, nothing shown: Claude Code is never started before that agreement, and axClaude
  checks it again itself); Claude Code is found where axKit looks; and no conversation window exists yet. Otherwise
  nothing is started until the first press, which starts it on view as before, and closing still hides it. A hidden
  start shows nothing, says nothing, plays nothing and never takes the focus or the foreground; Claude Code's trust
  question for a new folder, or any other question at its start, waits unspoken, and is asked when the window is
  first shown, as a question that has just arrived. **With the setting on, this is a moment at which Claude Code runs
  without the user's key**: at sign-in, and Claude Code may contact Anthropic as it starts (its sign-in check, its
  own update check; nothing of the user's is sent until a message is). It exists only when the user turned the
  setting on; `PRIVACY.md`, the READMEs and the guide say so. The cost: Claude Code in memory from sign-in while the
  setting is on.
- HE-2.8 **axKit's AI tools, by themselves, with instructions** (the user's ask of 2026-11-15: "make axhelp load the
  mcp automatically and provide lots of instructions along with some language rules to help it know when to use
  which tool"). Two parts.
  - **The tools.** The conversation is started with `--ai-tools` (`HelpSettings.ConversationArguments`), an option of
    axClaude's (FR-9.7). At every start of Claude Code in that window, axClaude reads the AI tools' sealed setting
    (`mcp.md` MC-6.1). **On**: it writes `%LOCALAPPDATA%\AxKit\claude-mcp.json`, which names this very program file
    (`PackageIdentity.ProgramPath`: the Store copy's alias, the installed file, or a development build) as the server
    `axkit` with the argument `mcp`, when the file is missing or names another program file, and adds
    `--mcp-config <that file>` to Claude Code's command line (`mcp.md` MC-6.9). **Off: nothing is written, nothing is
    added, nothing is asked**, and the conversation is as it was before this requirement; the guide and the rule
    file say where the tools are turned on. The existing switch stays the one gate: axHelp turns nothing on, shows
    no new question, and the pairing (MC-6.4), the run switch (MC-7.2) and every question of the tools are
    unchanged. A change of the switch counts from Claude Code's next start in the window (Session, End Claude and
    close this window, then the hotkey; or Force Claude to restart).
  - **What the user meets the first time**, with the tools on: nothing at the start (a server given on the command
    line has no approval question, unlike a `.mcp.json` in the folder). At Claude's first use of a tool, Claude Code's
    own permission question for that tool (with its "don't ask again" answer, kept per folder by Claude Code), which
    reaches the user through axClaude's question flow as every permission question does (FR-2.x; this one for an
    MCP tool is not yet among the recorded fixtures, so its reading as a list is owed a pass); and, if Claude Code was never paired with axKit, the
    pairing notice in an axKit window (MC-6.4), Deny its default. Neither is new, and neither is answered for the
    user.
  - **The instructions.** A second rule file of axKit's, `<working folder>\.claude\rules\axkit-tools.md`
    (`ai-help.md` AH-8, `AxKit.Core/Ai/AiToolsRule`), written and kept current with the guides (HE-2.6: the same
    marker, the same list of files, never a file of the user's). It says what the tools are for and that the user
    hears every action; the language rules, what the user is likely to say and which tool or guide answers it; each
    tool with its own description from `McpTools`, when to use it, when not, and what to say before and after; the
    caution of an offered command in `AiCaution`'s words; the limits (axPass and axClip never readable, no answer to
    a window's question, a refused call never retried another way); and what to say when the tools are not there.
    It is written whether the tools are on or off, so that Claude can say where they are turned on. A new folder's
    CLAUDE.md points at the rules (`WorkingFolderFiles.Instructions`); an existing one is never edited.
  - **Shift+F1 stays without the tools.** Its print-mode questions allow `Read`, `Glob` and `Grep` alone
    (`ai-help.md` AH-2.2), and a tool not allowed is refused in print mode; it gets the window's text from axKit
    itself already (AH-6).
- HE-2.9 **Files in the working folder that Claude Code would obey are asked about before a start** (2026-11-16,
  `repairs.md` R-54; `ai-help.md` AH-7.8, which says which files and why). The conversation is Claude Code in its
  interactive mode: once the folder is trusted, a hook in its settings files runs without a further word, and a
  skill, a command, an agent or a rule file there leads Claude. Before axHelp starts the conversation
  (`OpenConversation`, after `PrepareWorkingFolder`), `AxKit.Core/Ai/AiFolderCheck.NewOrChanged` lists such files
  that axKit did not write and the user has not accepted (`WorkingFolderFiles.Obeyed` names the agent file as
  axHelp writes it, which is not asked about while it holds that text). When there is one, axHelp's own window
  comes up with the notice "Claude's working folder holds files that can run commands" (AH-7.8's text), before
  Claude Code starts: Open the folder (the notice stays open), Start anyway and remember these files, and Cancel,
  the answer to Enter and Escape, which starts nothing and puts the window back out of sight when it was. When
  there is none, nothing is said and nothing is asked. The start ahead of HE-2.7 never starts in such a folder: it
  writes one log line and leaves the start to the user's next ask, which shows the notice. A conversation that is
  already running is brought up as before, without a check. Pure part tested in `HelpTests` and
  `tests/AxKit.Tests/AiFolderCheckTests`; the notice itself is owed a window check and a hearing with NVDA (no
  script starts the conversation without a live Claude Code).

### HE-3 Screenshots and pictures

- HE-3.1 **The screenshot's hotkey** takes a picture of the window in front at that moment (`ScreenPicture`:
  `GetWindowRect`, `Graphics.CopyFromScreen`, the whole screen it is on when the window gives no rectangle) into
  `<working folder>\screenshots\<yyyy-MM-dd HH.mm.ss>.png`, puts the path on the clipboard, and brings up the
  conversation with `Screenshot of "<title>": <path> ` in its message field, the caret at the end (axClaude's
  "message" hand-over or `--message`), for the user to type the question and press Enter. A picture that could not be
  taken is said, and nothing else happens.
- HE-3.2 The working folder keeps the twenty newest screenshots (`HelpSettings.ScreenshotsKept`); a new one deletes the
  oldest beyond them. Claude Code reads them where they are, inside its own folder, without a question.
- HE-3.3 **A picture file of the user's**: Ctrl+D in axHelp's window (the Windows file picker on the Pictures folder,
  the kinds Claude Code reads, `PictureFile.Kinds`) or `AxKit.exe help <picture>` puts `Picture <name>: <path> ` into
  the conversation's field the same way. A file that is not a picture is refused with the kinds named, said in the
  window or shown before any window. Claude Code asks before it reads a file outside its folder.
- HE-3.4 Ctrl+O opens the newest screenshot in axPreview, whose reader gives the text in it (AP-D11); "No screenshot yet"
  when there is none.

### HE-4 The hotkeys

- HE-4.1 Ctrl+Shift+Slash, the question mark's key, for the conversation (changed in Options, Hotkey, AS-4.2) and
  Ctrl+Shift+Period, the greater-than sign's key, for the screenshot (`screenshotHotkey` in `axHelp.json`), each kept in
  the settings (HE-D5; the bundle's system-wide keys, AX-8.11). The conversation's hotkey
  while axHelp's own window is in front hides it. A taken conversation hotkey shows the window with the notice (AS-4.3);
  a taken screenshot hotkey is said in the window's text.
- HE-4.2 The keys until 2026-11-14 were Windows+Alt+H and Windows+Alt+S. A settings file that still holds one of them
  is taken over to the new key as it is read, each of the two on its own (`HelpSettings.Load` through
  `HotkeySettings.TakenOver`), and written with the next save; a key the user chose is kept. A user who had chosen
  Windows+Alt+H themselves cannot be told from one who never chose: they get the new key and choose again.
- HE-4.3 Ctrl+Shift+Period, the greater-than sign's key, kept for a later help launcher until 2026-11-24, is the
  screenshot's since the maintainer's move of that day, which gave Ctrl+Shift+Comma, the screenshot's until then, to
  axClip (AX-8.11); a file that holds Ctrl+Shift+Comma or Windows+Alt+S for the screenshot is taken over to it
  (`HelpSettings.RetiredScreenshotHotkeys`). Ctrl+Period without Shift cannot be a hotkey: it is the Context menu in
  every axKit window.

### HE-5 Settings

- HE-5.1 `%APPDATA%\AxKit\axHelp.json` (`HelpSettings` on `HotkeySettings`): `hotkey`, `screenshotHotkey`, `model`,
  `keepConversationReady` (HE-2.7), `startWithWindows`, the font and the update check. A file of 5.6.0 holds `exchanges`, no longer read.

### HE-6 Menus and keys

The bundle's eight menus (AX-9.1, 2026-11-11): Session (S): Hide (Escape), Quit axHelp. Actions (A): Open the
conversation (Enter), Put a picture file into the conversation… (Ctrl+D), Open the last screenshot in axPreview
(Ctrl+O), Claude's working folder. Navigation and Context hold one item each that says they have nothing yet. Options:
Hotkey…, Conversation model ▸, Keep the conversation ready in the background (HE-2.7), Text ▸, General ▸ (Start with Windows), Developer ▸. Then the bundle's Window, axKit and Help
(AX-9.1). Bundle-wide keys (AX-8.2): F1, Ctrl+Shift+P, F4, Shift+F1, the text size keys, Alt and Alt+F4. The F1 text
names them all (`HelpText.Shortcuts`).

## 5. Architecture

`src/AxHelp`: `HelpForm` on `AxKit.Forms/HotkeyForm` (`OnHotkey` and the second hotkey, `OpenConversation`,
`StartConversationHidden`, `TakeScreenshot`, `PutPicture`), `ScreenPicture` (from axDescribe), `AxHelpApp`, `AxHelpKeys`, `HelpText`.
`src/AxHelp.Core`: `HelpSettings` (the two hotkeys, the model, `ConversationArguments`), `PictureFile`,
`WorkingFolderFiles`; tested in `tests/AxHelp.Tests`. The guides (HE-2.6) are the bundle's, `AxKit.Core/Ai/AiDocumentation`, and so are the AI tools' rule file
(`AxKit.Core/Ai/AiToolsRule`) and config file (`AxKit.Core/Mcp/McpClaudeCode`), HE-2.8. axClaude's part: `--ai-tools`
(`MainForm.AiToolsArguments`, FR-9.7), `--title` and the "message" hand-over
(`MainForm.TakeHandOver`, FR-9.2). `HotkeyForm`'s part: `OnHotkey`, `SecondHotkey`, `OnSecondHotkey`.

## 6. Decisions

- **HE-D1 axClaude is the conversation.** The user asked for axHelp "just like axClaude ... a seamless conversation",
  with past conversations and agent work. axClaude has every one of these, checked with NVDA over many releases; a
  second chat window would copy it and drift. axHelp adds only what is its own: the hotkeys, the folder, the model.
- **HE-D2 One app, two hotkeys** (the user's choice of 2026-11-10 over two apps into one conversation): axDescribe's
  only work left was the picture, which is a message of the same conversation.
- **HE-D3 The model at the start, not `/model`.** A command-line model is the one setting Claude Code's saved default
  cannot override, so the fast chat stays fast; the subagent carries the strong model only where it is needed.
- **HE-D4 Ongoing, cleared by the user** (the user, 2026-11-10: "leave it ongoing and then let the user clear it"), so
  that old answers stay reachable with the heading and reply keys.
- **HE-D5 The hotkeys are on Ctrl+Shift** (the user's decision of 2026-11-14, with axStart's Ctrl+Shift+Space): the
  question mark's key says "help", and the less-than sign's key beside it is the screenshot. Windows+Alt+H and
  Windows+Alt+S are retired: the user expected other programs to take them or to act on them. Not Ctrl+Shift+H or
  Ctrl+Shift+I, which are keys of axKit's editors and axGIT and of every browser; not Ctrl+Shift+Enter, Excel's and
  several chat programs'. axMedia gave up Ctrl+Shift+Comma and Period for it (its AM-D8). The cost outside axKit:
  Word and Outlook lose smaller and larger font on those keys while axHelp runs.
- **HE-D6 The guides as files, told by a rule file** (2026-11-15; `ai-help.md` AH-D7): the retrieval is Claude
  Code's own reading of a folder, and the instruction is a file of axKit's in `.claude\rules`, not a line in the
  user's CLAUDE.md (which is theirs and written once) and not a system prompt option on the command line (which
  Claude Code records with a conversation's first request, so it would not reach the conversation that goes on).
- **HE-D7 The conversation hides; it does not close** (2026-11-15). A conversation the user returns to all day
  should cost nothing to return to. Closing ended Claude Code, and the next press paid for its start and for the
  replay of the conversation, with the reading that goes with it. Hiding keeps the window, the caret, the text typed
  and Claude Code as they were; axClaude learns one option for it and knows nothing of axHelp. Ending is still one
  command, in the conversation's own Session menu, and Quit axHelp ends everything of axHelp's.
- **HE-D8 The hotkey means "let me type"** (2026-11-15): it always puts the focus in the message field, since that
  is what the key is pressed for; every other way back to the window leaves the user where they were reading.
- **HE-D9 Claude Code is started ahead only when the user turned that on; the setting is off at first** (the
  user's decision, 2026-11-15). By default Claude Code never starts, and so never contacts Anthropic, before the user
  acts: the first press of the hotkey. The instant return after that comes from hiding on close (HE-D7) and costs no
  new contact. A user who wants the first press instant too turns the setting on, and the guide says what it costs:
  memory, and Claude Code starting at sign-in. No prompt, no first-run question, no reminder: the setting is only in
  Options and in the guide. The other conditions stay: axClaude's agreement for Claude Code given, Claude Code
  installed.
- **HE-D10 The tools on the conversation's own command line; the instructions as a rule file** (2026-11-15,
  HE-2.8). Three ways to give the conversation the tools were weighed against Claude Code's documentation
  (code.claude.com/docs/en/mcp and /cli-reference, read 2026-11-15).
  (a) A `.mcp.json` in the working folder, project scope: "Claude Code prompts for approval in interactive sessions
  before using project-scoped servers from `.mcp.json` files", a question at the start that is Claude Code's own
  security prompt for a file anyone could have put there; the file would sit loose at the top of the user's folder
  (HE-2.4), and a file in a folder cannot follow the consent, which lives in axKit's sealed setting.
  (b) `--mcp-config`, "Load MCP servers from JSON files or strings", on the conversation's command line: no approval
  question, decided anew at every start from the sealed setting, in the one place that builds Claude Code's command
  line (root rule 12), and always this very program file. **Chosen.**
  (c) Nothing new, relying on Add axKit to Claude Code (MC-6.8, `claude mcp add --scope user`), which does make the
  tools available in every folder, the working folder included (checked 2026-11-15: a session in another folder had
  them). It stays as it is for the user's other Claude Code sessions, but it is not enough for axHelp: a user who
  chose Turn on only has no entry; the entry names the program file of the day it was made, and a broker reads only
  the windows of its own program file (MC-6.3), so an entry of another copy (a development build beside the Store's)
  sees no window and cannot offer a command (MC-9.2). The server on the command line has the same name, `axkit`, as
  that entry, so that the tools' names are the same either way and Claude Code can hold one axKit server: its
  documentation says of the scopes that "Claude Code matches duplicates ... by name" and "connects to it once"; it
  does not say so of a server given on the command line beside a user-scope entry of the same name, which is owed a
  check on a Claude Code that has both (the nvda test plan's step).
  The file is a file, not the JSON on the command line, because npm's `claude.cmd` runs through cmd.exe, where an
  argument holding a double quote is refused (axClaude FR-1.2). It is in axKit's own folder, not the working folder.
  Claude Code's permission question for each tool is left as it is, not allowed ahead with `--allowedTools`: what a
  tool reads goes into the conversation and so to Anthropic, and skipping a question about that is the user's to
  choose ("don't ask again"), never axKit's default.
  The instructions are a rule file for HE-D6's reasons, and generated from `McpTools`, so that they cannot name a
  tool that is gone.
- Until 5.7.0 (`docs/axKit/ai-help.md` section 7): a field over a list of ten earlier questions, each answered by Claude
  Code's print mode as a notice; and axDescribe, a field over a list of ten descriptions.

## 7. Testing

`tests/AxHelp.Tests` (the settings, the conversation's command line, the working folder's files, the picture files);
`tests/AxKit.Tests/AiDocumentationTests` (the guides: every app's among them, written where missing, again by another
version, a file of the user's kept); `AiToolsRuleTests` (HE-2.8: every tool of `McpTools` in the rule file and no
tool that does not exist, the caution's words, the length) and `McpTests` (the config file, and nothing while the
tools are off);
`tests/scripts/axHelp` (`01-start.txt` turns the AI tools on and checks the rule file and the config file; the window, the conversation brought up from it and from a picture start, against the real
axClaude with Haiku; `02-hide-and-show.txt`: closing hides, the same window comes back with the focus in the message
field, coming back otherwise keeps the focus, Quit axHelp ends a hidden conversation); the hidden start itself is
`tests/scripts/axClaude/20-hide-on-close.txt`, and its conditions `HelpTests`: no script starts axHelp hidden, since
a hidden hotkey window has no automation channel; the NVDA test plan `docs/axHelp/nvda-test-plan.md`, whose real conversation and screenshots are
the user's by ear, with the test pictures of `tests/fixtures/pictures`.
