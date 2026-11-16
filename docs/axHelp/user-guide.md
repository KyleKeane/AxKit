# axHelp user guide

axHelp, access layer for Claude Code, is your conversation with Claude from anywhere on your computer: one hotkey
brings it up, another drops a picture of the screen into it. It is part of axKit, made for screen reader users. axHelp
only brings the conversation up and passes your words and pictures to Claude Code; Claude Code, Anthropic's program,
answers and works under your own Anthropic account and Anthropic's terms, which are yours to follow.

## The conversation

axHelp runs in the background from sign-in (Options, General, Start with Windows). From any program, press
Ctrl+Shift+Slash, the key with the question mark: an axClaude window titled axHelp comes up with the focus in its message field. Type and press Enter;
the reply comes as in axClaude, and every key of axClaude reads it: h for headings, the reply keys, Ctrl+Home. Whether
replies are spoken as they arrive is axClaude's setting, Options, Speech, Speak replies.

It is one conversation that goes on: press Ctrl+Shift+Slash tomorrow and it is there, the earlier answers with it. To
start afresh, Session, New session in that window. Claude Code's own `/resume` lists the earlier conversations of
axHelp's folder.

Alt+F4 in the conversation hides it; it does not end it. Claude Code keeps running, and Ctrl+Shift+Slash brings the
same window back at once, as you left it: nothing is loaded or read again. Ctrl+Shift+Slash always puts the caret in
the message field, ready to type, also when the window is already open and you were reading in the conversation.
Coming back with Alt+Tab changes nothing: you are where you were. While it is hidden the conversation says nothing
and plays no sound, and it is not in Alt+Tab or the Window menu; a reply that came meanwhile is in the conversation
when you bring it up ("Done" is said then), and a question of Claude's waits and is asked then. To end it for real,
in the conversation: Session, End Claude and close this window. Quit axHelp ends it too; if Claude is still working, or you typed a
message you have not sent, the conversation comes up and asks first, and stays open if you keep it. The cost of all this: Claude
Code stays in your computer's memory while the conversation is hidden.

Claude Code starts at your first press of the key, never before you act; that first press takes a few seconds, and
every press after it is at once. If you want the first press at once too, turn on Options, Keep the conversation
ready in the background, in axHelp's window; it is off unless you turn it on. axHelp then starts the conversation
hidden when axHelp itself starts in the background, as at sign-in, so that Claude Code is loaded before you press
anything. It does so only after you have agreed to Claude Code in axClaude's reminder, and only when Claude Code is
installed. What it costs: Claude Code starts at every sign-in and may contact Anthropic then, to check your sign-in
and for its own updates (your words and files go only with a message you send), and it stays in memory from
sign-in on.

The first time, axClaude's reminder says what goes to Anthropic, and Claude Code asks whether to trust its folder:
answer yes. Claude Code must be installed and logged in.

## Doing things on your computer

Claude Code in the conversation can read files, change them and run commands, as in axClaude. It asks before each
change: answer from the list that comes up. For work of several steps it hands over to its agent, which runs on a
stronger model, and tells you so.

## Pictures of the screen

Press Ctrl+Shift+Comma, the key with the less-than sign, in any program: axHelp takes a picture of the window in front, the conversation comes up with
"Screenshot of" and the picture's path in its message field, and the path is on the clipboard too. Type your question
after it ("which button is the default?") and press Enter; with no question, Claude says what the window shows.

A picture of your own goes the same way: in axHelp's window, Ctrl+D chooses it in the Windows file picker, or from a
terminal, `axHelp C:\Pictures\garden.jpg`. Three test pictures come with axKit's source, in `tests\fixtures\pictures`.

The screenshots are kept in axHelp's folder, the twenty newest; Ctrl+O in axHelp's window opens the newest in
axPreview, which reads the text in it without Claude.

## axHelp's folder and model

Claude works in its own folder, Documents, axKit Claude (Actions, Claude's working folder opens it in axFile). Its
conversations, the permissions you give with "always" and its instructions stay there, apart from your projects. The
file CLAUDE.md there holds Claude's standing instructions for axHelp, short spoken replies among them; change it as you
like. The folder `.claude\agents` holds the agent.

Some files in that folder can make Claude Code run commands by itself: a hook in a settings file, a file of servers,
a skill, a command, an agent or a rule. When axHelp finds one there that axKit did not write, it asks before it
starts the conversation, in the notice "Claude's working folder holds files that can run commands", which names the
files. Open the folder shows them in axFile; Start anyway and remember these files starts the conversation and does
not ask again until one of them changes or a new one comes; Cancel, which Enter and Escape choose, starts nothing.
The files may well be your own.

You can ask about axKit itself: "how do I select several files in axFile?" The folder `documentation` there holds
axKit's own guides, one file per app and an index, and Claude reads the fitting one before it answers, and says when
the guides do not cover your question. Beside the guides are axKit's workflow pages, plain files that open in axDown,
from which Claude can say what an app can do and give the steps of a task with their keys as they are in your
version: `workflows.md` lists what you can do in each app, `workflows-axFile.md` and its like hold the steps, and
`window-commands.md` every window's commands. axKit writes these guides again with each new version, and puts back any of them that was changed or deleted the next time you use Claude, so do not keep your own
changes in them; a file of your own that you add beside them stays. Nothing is sent anywhere for this: Claude Code
reads the files on your computer as it reads any file for an answer. The screenshots have their own folder there too,
`screenshots`, so the top of the folder stays short: CLAUDE.md, `documentation` and `screenshots`.

Claude can also work with your axKit windows, when you have turned axKit's AI tools on: axKit menu, AI tools, Let AI
tools read and open axKit, in any window. The conversation then gets the tools by itself at its next start, and
you can say "what is in my axDown window", "what did it just say", "open my Downloads folder in axFile" or "remind
me on Friday to call the dentist". While the tools are off, nothing changes: Claude has none of them, and tells you
where to turn them on if you ask for something that needs them. Running a window's commands and adding tasks need
the second switch too, Let AI tools run commands. The first time Claude uses a tool, Claude Code asks whether it
may, as it asks before any tool, and an axKit window may ask once whether Claude Code may use axKit. What Claude
reads from a window becomes part of the conversation, as a file it reads does. axKit also keeps a file of
instructions for Claude about these tools, `.claude\rules\axkit-tools.md` in the folder; it is axKit's, written
again with each version, like the guides.

The conversation runs on the fast model by default. Options, Conversation model chooses Fast, Balanced or Strongest; it
counts from the conversation's next start, so end the conversation (its Session, End Claude and close this window) and press Ctrl+Shift+Slash. Choose the
model here rather than with `/model` in the conversation: Claude Code saves `/model` as its default for every new
session, axClaude's too.

## axHelp's window

Start menu, axKit axHelp, or the command `axHelp`, shows axHelp's own small window: one text that says what the two
hotkeys do. Enter opens the conversation; Escape hides the window. Session, Quit axHelp ends axHelp.

Options, Hotkey changes Ctrl+Shift+Slash. The screenshot's key is `screenshotHotkey` in the settings file
(`%APPDATA%\AxKit\axHelp.json`, Options, Developer, Open settings file); start axHelp again after changing it.

Up to version 5.8.0 the two keys were Windows+Alt+H and Windows+Alt+S. If you never changed them, axHelp moves you to the new
keys by itself; a key you chose stays yours. While axHelp runs, its two keys are taken from every program: Word and
Outlook use them for a smaller and a larger font. Choose other keys if you need those.

- **Window**, **axKit** and **Help**: the bundle's menus, the same in every axKit window, with Options just before Window. Window (Alt+W) starts a new window of any app, lists the open axKit windows (the one you were in last first: Alt+W, O, 1 takes you back to it), reopens a file or folder closed lately and closes all windows. axKit (Alt+K) holds what is the whole bundle's: Claude (the switch Use Claude in axKit, Ask about this window with Shift+F1, Folders Claude may read, Claude's working folder), the AI tools and the theme. Help (Alt+H) has the command palette (Ctrl+Shift+P), the keyboard shortcuts (F1), this guide, the app's resource pages, Send a report (F4), Your reports, Diagnostics (Test a key and Copy diagnostics), Check for updates and About.

## If something goes wrong

"The screenshot could not be taken": the window in front gave no picture; try again with it restored. "x is not a
picture axHelp can pass to Claude": only .png, .jpg, .jpeg, .gif and .webp files are read. Claude Code not found, or
not logged in: the conversation says so, as axClaude does, with Install Claude Code now. Options, Developer, Copy
diagnostics copies what helps a bug report; F4 sends one.

If the hotkeys do nothing, start axHelp again from the Start menu. When the running axHelp has stopped answering, the
new start asks "axHelp is running but does not answer. End it and start again?". No is the default and changes
nothing; Yes ends that axHelp alone and starts it again; the conversation's window is not ended.

## About

Help, About.

## Disclaimer

axHelp comes with no warranty; see the licence in Help, About. What you type and the pictures you send go to Anthropic
under your account and Anthropic's terms, and a screenshot shows whatever was on your screen: check what is in front
before you press the key.
