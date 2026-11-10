# axHelp, access layer for Claude Code: specification

Status: built 2026-11-09 on the branch `ai-help` as a question field over Claude Code's print mode; rebuilt
2026-11-10 (5.7.0) at the user's word as two hotkeys into one conversation in axClaude, with axDescribe merged in.
Requirements are numbered HE-n.m. The bundle's rules are `docs/axKit/SPEC.md` (AX-7 accessibility, AX-8 keys, AX-11
access layers); axClaude's are `docs/axClaude/SPEC.md` (FR-n.m), whose window the conversation is; "SPEC.md" in axHelp's
code means this file.

## 1. Summary

axHelp is a conversation with Claude from anywhere on the computer. One hotkey (Windows+Alt+H) brings it up: an
axClaude window on Claude's own working folder, titled "axHelp", with the focus in its message field. It is one
conversation that goes on until the user clears it, read with axClaude's keys, its replies spoken as axClaude speaks
them, and Claude Code in it may do work on the computer, asking before each change. A second hotkey (Windows+Alt+S)
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

- In Word, Windows+Alt+H: "axHelp" and the message field. "how do I make a table of contents", Enter; the reply is
  spoken as it comes. Later, Windows+Alt+H again: the same conversation, the earlier answer still there under h and the
  reply keys.
- A dialog NVDA reads badly: Windows+Alt+S. The conversation comes up with "Screenshot of "Save changes?": C:\...\
  2026-11-10 14.02.11.png " in its field; type "which button is the default?", Enter.
- "Move every PDF in my Downloads folder into a folder named Papers": the conversation hands it to its agent, Claude
  Code asks before it moves anything, and says what it did.
- Start menu, axKit axHelp: axHelp's own window, a text naming the two hotkeys; Enter opens the conversation.

## 4. Requirements

### HE-1 Launch, the background process, the hand-over

- HE-1.1 `AxKit.exe help` shows axHelp's window and keeps running; `--hidden` runs it hidden until a hotkey, as at
  sign-in (`HotkeyForm`, axStart AS-1.1, AS-1.3); `AxKit.exe help <picture>` puts the picture into the conversation
  (HE-3.3) without showing the window. Start with Windows (Options, General) writes the Run key. In the Store's copy
  axHelp has no startup task (bundle AX-13.2) and is started from any window's Window menu, New window.
- HE-1.2 One axHelp at a time (a named mutex, `AppPaths.OneAtATime`): a second start hands "show", or "open" with its
  picture, to the running one's window (`AppMessage`), found by its title "axHelp hotkeys" (never "axHelp", which is the
  conversation's), and ends; with `--hidden` it just ends.
- HE-1.3 Alt+F4 and Close all windows hide axHelp's window; axHelp, Quit axHelp ends the process. The conversation is an
  axClaude window of its own and closes as any axClaude window.
- HE-1.4 `AxKit.exe describe`, axDescribe's verb until 5.7.0, starts axHelp with the same arguments (`Dispatch.
  FormerDescribe`), so that an older sign-in start or script still works; axHelp removes axDescribe's Run value at its
  start.

### HE-2 The conversation

- HE-2.1 **The conversation's hotkey** (and Enter in axHelp's window) brings up the conversation: the open axKit window
  titled "axHelp" to the front (`WindowMenu.Activate`), or, when none is open, a new axClaude window, `AxKit.exe claude
  <working folder> --title axHelp -- --continue --model <model>` (`HelpSettings.ConversationArguments`), which continues
  the folder's last conversation (axClaude FR-9.1). axHelp's own window hides first. Everything in the conversation is
  axClaude's: the message field, the transcript and its keys, the replies spoken by Options, Speech, Speak replies, the
  questions Claude asks, Session, New session to clear it, Claude Code's `/resume` for the folder's earlier
  conversations.
- HE-2.2 **axHelp's window** is one read-only text, named "axHelp", that says what the two hotkeys do as they are set,
  where Claude works, and whether the screenshot's hotkey is taken; the menus hold the rest (HE-6). It is shown by a
  start without the hotkey, never by a hotkey.
- HE-2.3 **The model.** The conversation starts with axHelp's own model, `model` in `axHelp.json`, the fast one
  (`haiku`) by default; Options, Conversation model offers Fast (Haiku), Balanced (Sonnet), Strongest (Opus). Given as
  `--model` at the start, it wins over Claude Code's saved default, so a model chosen elsewhere never changes axHelp's;
  a change counts from the conversation's next start. `/model` typed inside the conversation changes that session and
  is saved by Claude Code as its default for new sessions everywhere, axClaude's included; the guide says to choose
  axHelp's model in its Options instead.
- HE-2.4 **The working folder** is AI help's (`ai-help.md` AH-2.1, AH-D6): `Documents\axKit Claude` unless
  `ai-help.json` names another. Claude Code keeps per folder the conversations, the "always allow" answers and the
  CLAUDE.md, so nothing of axHelp reaches the user's projects. Before the conversation's start axHelp writes, each only
  when missing (`WorkingFolderFiles`): `CLAUDE.md` (short spoken replies; a message starting "Screenshot of" or
  "Picture" names a picture file to read first; work of several steps on the computer goes to the subagent) and
  `.claude\agents\agent.md`, a Claude Code subagent named agent on the strongest model (`model: opus`). The user's own
  changes to either stay.
- HE-2.5 **Permissions.** The conversation is Claude Code in its interactive mode: it may read, write and run commands
  on the computer, and asks before each change unless the user allowed it (axClaude's questions, FR-2.x; the mode with
  Ctrl+Shift+M). This is the agent the user asked for on 2026-11-10; its power is Claude Code's, under the user's eye.

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

- HE-4.1 Windows+Alt+H for the conversation (changed in Options, Hotkey, AS-4.2) and Windows+Alt+S for the screenshot
  (axDescribe's until 5.7.0; `screenshotHotkey` in `axHelp.json`), each kept in the settings. The conversation's hotkey
  while axHelp's own window is in front hides it. A taken conversation hotkey shows the window with the notice (AS-4.3);
  a taken screenshot hotkey is said in the window's text.

### HE-5 Settings

- HE-5.1 `%APPDATA%\AxKit\axHelp.json` (`HelpSettings` on `HotkeySettings`): `hotkey`, `screenshotHotkey`, `model`,
  `startWithWindows`, the font and the update check. A file of 5.6.0 holds `exchanges`, no longer read.

### HE-6 Menus and keys

axHel&p (P, since H is Help): Open the conversation (Enter), Put a picture file into the conversation… (Ctrl+D), Open
the last screenshot in axPreview (Ctrl+O), Claude's working folder, Hide (Escape), Quit axHelp. Options: Hotkey…,
Conversation model ▸, Text ▸, General ▸ (Start with Windows), Developer ▸. Then the bundle's Window, axKit and Help
(AX-9.1). Bundle-wide keys (AX-8.2): F1, Ctrl+Shift+P, F4, Shift+F1, the text size keys, Alt and Alt+F4. The F1 text
names them all (`HelpText.Shortcuts`).

## 5. Architecture

`src/AxHelp`: `HelpForm` on `AxKit.Forms/HotkeyForm` (`OnHotkey` and the second hotkey, `OpenConversation`,
`TakeScreenshot`, `PutPicture`), `ScreenPicture` (from axDescribe), `AxHelpApp`, `AxHelpKeys`, `HelpText`.
`src/AxHelp.Core`: `HelpSettings` (the two hotkeys, the model, `ConversationArguments`), `PictureFile`,
`WorkingFolderFiles`; tested in `tests/AxHelp.Tests`. axClaude's part: `--title` and the "message" hand-over
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
- Until 5.7.0 (`docs/axKit/ai-help.md` section 7): a field over a list of ten earlier questions, each answered by Claude
  Code's print mode as a notice; and axDescribe, a field over a list of ten descriptions.

## 7. Testing

`tests/AxHelp.Tests` (the settings, the conversation's command line, the working folder's files, the picture files);
`tests/scripts/axHelp` (the window, the conversation brought up from it and from a picture start, against the real
axClaude with Haiku); the NVDA test plan `docs/axHelp/nvda-test-plan.md`, whose real conversation and screenshots are
the user's by ear, with the test pictures of `tests/fixtures/pictures`.
