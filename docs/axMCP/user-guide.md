# axMCP user guide

axMCP shows the MCP servers your computer knows and lets you run them by hand: start one, see what it offers, fill a
tool's arguments and read the result. It is part of axKit, a bundle of apps for people who use a screen reader; it was
built for and with NVDA. It is an access layer: the servers are programs of their makers', which axMCP starts and talks
to for you.

## Install

axMCP is part of axKit. Installing axKit (see the axKit guide: Help, About, axKit guide) gives you a Start menu entry
`axKit axMCP` and a console command `axMCP`. Any axKit window starts one too: Alt+K, Access layers, axMCP.

## Start

- Start menu: `axKit axMCP`. Console: `axMCP`, or `axmcp C:\my\project` for the servers a project folder adds (its
  `.mcp.json` and Claude Code's entry for that folder).
- The window is two panes, as axTask's: the navigator on the left, the detail pane on the right, and a status bar.
  The focus starts in the navigator, on the first server.

## The servers

The top level is in sections: **Claude Code**, the servers of Claude Code's own configuration on this computer, and
**Your servers**, the ones you add here. Each row reads the server's name, how it is reached (stdio, http) and its
state (stopped, starting, running). Tab reads where it is defined, its command line, its folder, the names of its
environment variables (never their values) and its state.

- Right or Enter on a server starts it. The first time, a notice asks your agreement: the server is a program on your
  computer that does whatever its maker built it to do, on this computer and on any network it reaches, with your
  rights. The notice reads what would run: the program's full path with its arguments, the folder it starts in, and
  the names of the environment variables its definition sets, never their values. Your agreement is for exactly
  that: when the program, an argument, an environment variable or the folder of a server changes, in Claude Code's
  files or in your own list, axMCP asks again before it starts it. Then "starting", and once it answers, "running,
  Tools 8, Resources 2, Prompts 0".
- Right on Tools, Resources or Prompts shows its list. Left or Backspace goes back up; the server keeps running.
- Ctrl+Shift+S stops the server of the level you are in, or the one selected at the top. Closing the window stops
  every server.
- A remote server (http or sse in Claude Code's file) is listed and not started in this version; Right says so.

## Tools

A tool's row reads its name and the first sentence of what it does. Tab moves to its arguments: the description first
(Enter reads it whole), then one field per argument with its type, "(required)" after the ones the tool needs. Enter
changes a field, as in axTask: a text, a number, a date, a check with Space, a choice from a list (Right and Left also step through a choice, and check or uncheck a check), or a long text
holding JSON for anything structured. Ctrl+Enter calls the tool with what the fields hold: "Calling …", then the result
opens as a text in the window's place, read with the arrows, h for the headings and Ctrl+F to find; Escape closes it
and the focus is back where it was. Ctrl+J opens the whole answer as JSON in axJSON. The values you typed stay while
the window is open, so a second call is one key.

## Resources and prompts

Enter on a resource reads it and opens its text; on a resource template it asks for the address first. A prompt is
filled like a tool and fetched with Ctrl+Enter; its messages open as a text, each starting with its role.

## Your own servers

At the top level, Ctrl+N adds a server in one form: type its name, Tab, type the command line that starts it as a
terminal would, Tab, and a folder to start it in (empty for the folder axMCP was started on, or your profile folder);
Enter or Ctrl+Enter adds it. What is missing is said, with the focus in its field. F2 renames one of yours, Delete removes
one after a question, Ctrl+Z takes the last of these back. Space selects one of your servers (Ctrl+A all of yours,
Escape none); Delete then removes all the selected after one question, and one Ctrl+Z brings them all back. Claude Code's own servers are changed in Claude Code, with
`claude mcp add` and `claude mcp remove`; axMCP only reads its files.

## Menus

Each menu with its commands and their keys, as the window lists them:

<!-- menus: written by tools/guide-menus.ps1 from docs/axKit/window-commands.json; never edit by hand -->
- **Session**: Exit (Alt+F4); Refresh (F5).
- **Actions**: Add server (Ctrl+N); Deselect all (Ctrl+Shift+A); Select all of your servers (Ctrl+A); Undo (Ctrl+Z).
- **Navigation**: Nothing to move to here yet.
- **Context**: A heading: nothing to act on.
- **Options**: Check for updates at start; Edit app keys file; Filter; Keep a log of events; Open the sounds folder; Developer (Open settings file); Sort by (Name, The list's own order); Text (Entry sounds, Font, Larger text (Ctrl+Plus), Say new line, Smaller text (Ctrl+Minus), Sound on headings, Windows text size); Theme (Edit theme file, Reload theme, Theme on).
- Keys without a menu item: Enter (Change the field); Space (Check the field); Ctrl+Delete (Clear the filter); Delete (Delete); Ctrl+F (Find in the list); F3 (Find next); Shift+F3 (Find previous); Right (Into the list); Ctrl+N (New item); Ctrl+Shift+N (New list); Ctrl+Down (Next section); Enter (Open); Ctrl+Up (Previous section); F7 (Read in full); Enter (Read the resource); F2 (Rename); Ctrl+A (Select all); Space (Select or deselect); Shift+Space (Select to here); Shift+Home (Select to the first server); Shift+End (Select to the last server); Shift+Down (Select to the next server); Shift+Up (Select to the previous server); Ctrl+Tab (Switch between the navigator and the detail pane); Shift+F10 (The Context menu); Shift+F10 (The field's menu); Right (The next value of the field); Left (The previous value of the field); Tab (To the detail pane); Tab (To the navigator); Left (Up a level).
- **Window**, **axKit** and **Help**: the menus of the bundle, the same in every axKit window; the axKit guide describes them, and F1, the command palette, lists every key and command. Options ends with the settings of all of axKit.
<!-- end of the menus -->

## Settings

Options, Text sets the font and its size; Options, General the update check. Your servers are kept in
`%APPDATA%\AxKit\axMCP.json`, which Options, Developer, Open settings file opens in axJSON. Nothing a server answers is
kept, apart from the last result Ctrl+J writes beside the logs.

Your servers (`servers`) are sealed in that file: they name programs axMCP starts, so they count only as axMCP wrote
them. Add, rename and remove them in the window (Ctrl+N, F2, Delete). If you change one in the file by hand, or
another program does, axMCP lists none of them, keeps your other settings, and says once: "Your servers in axMCP's
settings were changed outside axKit and are not used. Add them again with Ctrl+N at the top level." The file as
it was found is kept beside it, with `.changed` at the end of its name (`axMCP.json.changed`). The seal's key is
kept by Windows for your account; a program running as you could ask Windows for it, so the seal protects against a
file changed by another program or by hand, a profile copied or synced from another computer, and an AI tool writing
files, and not against a program that sets out to forge it.

## Updates

Help, Check for updates says whether a newer axKit exists.

## If something goes wrong

A server that does not start says why in a notice. What a server writes to its standard error, its own error lines,
is in its details: at the top, go to the server's row and press Tab; after the state the details say "Error lines:"
with their number, then the newest ten. They stay in the window and are gone when it closes: axMCP's log, which a
report can carry, holds only how long each line was, since a server may write a path or a token there. A server that stops by itself is announced with its exit code; Enter on its level starts it
again. A call that gets no answer in 60 seconds is given up and said.

## Filter and sort

Options, Filter (Alt+O, then F) shows only what has a text in its name: type part of a name, Enter, and the list holds only those rows and says so in its name, "Home, 4 items, filter tap, 1 shown". An empty text clears the filter, and so does Ctrl+Delete in the list; going into another list drops it. Options, Sort by puts the list in the order of its names, or back in its own order. Both are the same in every list of axKit's navigators, and neither changes anything that is stored.

## About

axMCP is part of axKit by Dr. Kyle Keane, www.kylekeane.com. Help, About has the licence and the version.

## Disclaimer

axMCP comes with no warranty; see the licence (Help, About, Licence). The servers are their makers' programs under
their makers' terms; what a tool does when you call it is between you and that program.
