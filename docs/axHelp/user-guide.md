# axHelp user guide

axHelp, access layer for Claude Code, is your conversation with Claude from anywhere on your computer: one hotkey
brings it up, another drops a picture of the screen into it. It is part of axKit, made for screen reader users. axHelp
only brings the conversation up and passes your words and pictures to Claude Code; Claude Code, Anthropic's program,
answers and works under your own Anthropic account and Anthropic's terms, which are yours to follow.

## The conversation

axHelp runs in the background from sign-in (Options, General, Start with Windows). From any program, press
Windows+Alt+H: an axClaude window titled axHelp comes up with the focus in its message field. Type and press Enter;
the reply comes as in axClaude, and every key of axClaude reads it: h for headings, the reply keys, Ctrl+Home. Whether
replies are spoken as they arrive is axClaude's setting, Options, Speech, Speak replies.

It is one conversation that goes on: press Windows+Alt+H tomorrow and it is there, the earlier answers with it. To
start afresh, Session, New session in that window. Claude Code's own `/resume` lists the earlier conversations of
axHelp's folder.

The first time, axClaude's reminder says what goes to Anthropic, and Claude Code asks whether to trust its folder:
answer yes. Claude Code must be installed and logged in.

## Doing things on your computer

Claude Code in the conversation can read files, change them and run commands, as in axClaude. It asks before each
change: answer from the list that comes up. For work of several steps it hands over to its agent, which runs on a
stronger model, and tells you so.

## Pictures of the screen

Press Windows+Alt+S in any program: axHelp takes a picture of the window in front, the conversation comes up with
"Screenshot of" and the picture's path in its message field, and the path is on the clipboard too. Type your question
after it ("which button is the default?") and press Enter; with no question, Claude says what the window shows.

A picture of your own goes the same way: in axHelp's window, Ctrl+D chooses it in the Windows file picker, or from a
terminal, `axHelp C:\Pictures\garden.jpg`. Three test pictures come with axKit's source, in `tests\fixtures\pictures`.

The screenshots are kept in axHelp's folder, the twenty newest; Ctrl+O in axHelp's window opens the newest in
axPreview, which reads the text in it without Claude.

## axHelp's folder and model

Claude works in its own folder, Documents, axKit Claude (axHelp, Claude's working folder opens it in axFile). Its
conversations, the permissions you give with "always" and its instructions stay there, apart from your projects. The
file CLAUDE.md there holds Claude's standing instructions for axHelp, short spoken replies among them; change it as you
like. The folder `.claude\agents` holds the agent.

The conversation runs on the fast model by default. Options, Conversation model chooses Fast, Balanced or Strongest; it
counts from the conversation's next start, so close the axHelp conversation window and press Windows+Alt+H. Choose the
model here rather than with `/model` in the conversation: Claude Code saves `/model` as its default for every new
session, axClaude's too.

## axHelp's window

Start menu, axKit axHelp, or the command `axHelp`, shows axHelp's own small window: one text that says what the two
hotkeys do. Enter opens the conversation; Escape hides the window. axHelp, Quit axHelp ends axHelp.

Options, Hotkey changes Windows+Alt+H. The screenshot's key is `screenshotHotkey` in the settings file
(`%APPDATA%\AxKit\axHelp.json`, Options, Developer, Open settings file); start axHelp again after changing it.

- **Window**, **Claude** and **Help**: the bundle's three menus, the same in every axKit window.

## If something goes wrong

"The screenshot could not be taken": the window in front gave no picture; try again with it restored. "x is not a
picture axHelp can pass to Claude": only .png, .jpg, .jpeg, .gif and .webp files are read. Claude Code not found, or
not logged in: the conversation says so, as axClaude does, with Install Claude Code now. Options, Developer, Copy
diagnostics copies what helps a bug report; F4 sends one.

## About

Help, About.

## Disclaimer

axHelp comes with no warranty; see the licence in Help, About. What you type and the pictures you send go to Anthropic
under your account and Anthropic's terms, and a screenshot shows whatever was on your screen: check what is in front
before you press the key.
