# axHelp user guide

axHelp, access layer for Claude Code, is a question to Claude from anywhere on your computer: one hotkey, one field,
one answer, and a list of your last questions. It is part of axKit, made for screen reader users. axHelp only shows
what Claude Code does and passes your question to it; Claude Code, Anthropic's program, answers under your own
Anthropic account and Anthropic's terms, which are yours to follow.

## Before the first question

AI help is one switch for every axKit app: in any axKit window, Alt+A for the axKit menu, AI help, Let Claude help in
the apps. The first time, a reminder says what goes to Anthropic (your question, what the window gathers about
itself, the files Claude reads in the folders you allow); check the box and press Enter. Claude Code must be installed
and logged in; if it is missing, the notice offers Install Claude Code now.

Claude reads nothing of yours unless you allow a folder: axKit menu, AI help, Folders Claude may read, Add a folder.
A question about a window or a screen needs no folder.

## Bringing axHelp up

axHelp runs in the background from sign-in (Options, General, Start with Windows). From any program, press
Windows+Alt+H: NVDA says "axHelp window, Ask Claude, edit". The same hotkey hides it again, and so does Escape; Alt+Tab
to another program hides it too. Options, Hotkey chooses another key: press it with Ctrl, Alt or the Windows key,
then Enter.

Start menu, axKit axHelp, or the command `axHelp`, brings the field up as well; one axHelp runs at a time.

## Asking

Type your question and press Enter. axHelp says "Asking Claude" while Claude works (Escape stops the wait), then
"Claude says" and the answer, which you read with the arrows, Page Down and the heading letters as any axKit notice;
Ctrl+F finds in it. Claude knows which program was in front when you pressed the hotkey, so "how do I do this here"
has a here.

Tab moves to the field under the answer: type more and press Enter to ask about the same answer, in the same
conversation with Claude. Copy puts the whole answer on the clipboard; Escape closes it.

## Your earlier questions

The list under the field keeps your last ten questions with their answers. Down and Up move through them, each said
with its place ("how do I make a table of contents, 1 of 3"); type part of a question to find it. Enter with nothing
typed brings the selected question's answer back, and you can ask more about it. axHelp, Forget the questions
empties the list.

## Settings

Options, Hotkey (the key), Text (the font and its size), General (Start with Windows, Say the first match while
typing, the update check), Developer (the settings file `%APPDATA%\AxKit\axHelp.json`, diagnostics). The AI help
settings, the switch, the folders and the model, are the bundle's, `%APPDATA%\AxKit\ai-help.json`, reached from the
axKit menu in every window.

## If something goes wrong

"AI help is off": turn it on in the axKit menu. "Claude Code was not found": install it from the notice, or name its
program file as `claudePath` in `ai-help.json`. "Claude could not answer": the notice holds Claude Code's own words,
such as a login that ran out; Claude Code's own `claude` command in a terminal shows the same. "No answer in two
minutes": ask again, or a shorter question. Options, Developer, Copy diagnostics copies what helps a bug report;
F4 sends one.

## About

The axKit menu, About: the version, the licence, the reminders about the programs axKit's access layers run.

## Disclaimer

axHelp comes with no warranty; see the licence in the axKit menu, About. What you ask goes to Anthropic under your
account and Anthropic's terms; axKit keeps your questions and answers only in your own settings file.
