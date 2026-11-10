# axDescribe user guide

axDescribe, access layer for Claude Code, describes the window in front of you: one hotkey, and Claude says what the
window is, what it shows and which controls it has. It is part of axKit, made for screen reader users. axDescribe
only takes the picture and passes it to Claude Code; Claude Code, Anthropic's program, describes it under your own
Anthropic account and Anthropic's terms, which are yours to follow.

## Before the first picture

AI help is one switch for every axKit app: in any axKit window, Alt+A for the axKit menu, AI help, Let Claude help in
the apps. The first time, a reminder says what goes to Anthropic (here, a picture of the window you point at); check
the box and press Enter. Claude Code must be installed and logged in; if it is missing, the notice offers Install
Claude Code now.

## Describing the window in front

axDescribe runs in the background from sign-in (Options, General, Start with Windows). In any program, press
Windows+Alt+S: axDescribe takes a picture of the window in front, comes up and says "Asking Claude"; a few seconds
later "Claude says" and the description, which you read with the arrows, Page Down and the heading letters as any
axKit notice. Tab moves to the field under it: type a question about the same picture ("what is the highest value
in the chart?") and press Enter. Copy puts the description on the clipboard; Escape closes it, and Escape again hides
axDescribe and puts you back where you were.

The picture stays on your computer, one file written over by the next and deleted when axDescribe ends; Claude Code
reads it to answer, as it reads any file you point it at.

Options, Hotkey chooses another key: press it with Ctrl, Alt or the Windows key, then Enter.

## Describing a picture file

A picture of your own, a photo someone sent or a screenshot you saved, is described the same way. In axDescribe, press
Ctrl+D (axDescribe, Describe a picture file): the Windows file picker opens on your Pictures folder, showing the kinds
Claude reads, .png, .jpg, .jpeg, .gif and .webp; choose the file and press Enter. "Asking Claude", then "Claude says"
and the description, with the field under it for more about the same picture. The description is kept in the list
under the file's name. From a terminal, axFile or a script, `axDescribe C:\Pictures\garden.jpg` does the same, in the
running axDescribe when there is one. Your file is only read; nothing copies, moves or deletes it.

Three test pictures come with axKit's source, in `tests\fixtures\pictures`: a dialog asking to save a document, an
editor window and a bar chart, drawn in code so that they hold nothing personal. They are for trying axDescribe out.

## Your earlier descriptions

The list under the field keeps the last ten descriptions, each by its time and the window's title ("14:02 notes.md -
axDown") or the picture file's name. Down and Up move through them; type part of a title to find one. Enter with
nothing typed brings the selected description back, and the field asks more about its picture. Tab moves into the
list itself, where Down and Up move, NVDA reads each description and Enter shows it again; Shift+Tab returns to the
field. Nothing is said while you type. Ctrl+O opens the last
picture of a window in axPreview, whose reader lists the words in it. axDescribe, Forget the descriptions empties
the list and deletes that picture; your own files stay.

## Settings

Options, Hotkey, Text, General and Developer as in axHelp; the settings file is `%APPDATA%\AxKit\axDescribe.json`.
The AI help settings are the bundle's, `%APPDATA%\AxKit\ai-help.json`, reached from the axKit menu in every window.

## If something goes wrong

"AI help is off": turn it on in the axKit menu. "The picture could not be taken": the window in front gave no
picture; try again with the window restored. "x is not a picture axDescribe can describe": only .png, .jpg, .jpeg,
.gif and .webp files are read. "Claude could not answer": the notice holds Claude Code's own words.
Options, Developer, Copy diagnostics copies what helps a bug report; F4 sends one.

## About

The axKit menu, About.

## Disclaimer

axDescribe comes with no warranty; see the licence in the axKit menu, About. The picture goes to Anthropic under your
account and Anthropic's terms, and may show whatever was on your screen: check what is in front before you press the key.
