# axMCP user guide

axMCP shows the MCP servers your computer knows and lets you run them by hand: start one, see what it offers, fill a
tool's arguments and read the result. It is part of axKit, a bundle of apps for people who use a screen reader; it was
built for and with NVDA. It is an access layer: the servers are programs of their makers', which axMCP starts and talks
to for you.

## Install

axMCP is part of axKit. Installing axKit (see the axKit guide in the axKit menu, About) gives you a Start menu entry
`axKit axMCP` and a console command `axMCP`. Any axKit window starts one too: Alt+A, New window, axMCP.

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
  rights. Then "starting", and once it answers, "running, Tools 8, Resources 2, Prompts 0".
- Right on Tools, Resources or Prompts shows its list. Left or Backspace goes back up; the server keeps running.
- Ctrl+Shift+S stops the server of the level you are in, or the one selected at the top. Closing the window stops
  every server.
- A remote server (http or sse in Claude Code's file) is listed and not started in this version; Right says so.

## Tools

A tool's row reads its name and the first sentence of what it does. Tab moves to its arguments: the description first
(Enter reads it whole), then one field per argument with its type, "(required)" after the ones the tool needs. Enter
changes a field, as in axTask: a text, a number, a date, a check with Space, a choice from a list, or a long text
holding JSON for anything structured. Ctrl+Enter calls the tool with what the fields hold: "Calling …", then the result
opens as a text in the window's place, read with the arrows, h for the headings and Ctrl+F to find; Escape closes it
and the focus is back where it was. Ctrl+J opens the whole answer as JSON in axJSON. The values you typed stay while
the window is open, so a second call is one key.

## Resources and prompts

Enter on a resource reads it and opens its text; on a resource template it asks for the address first. A prompt is
filled like a tool and fetched with Ctrl+Enter; its messages open as a text, each starting with its role.

## Your own servers

At the top level, Ctrl+N adds a server: a name, the command line that starts it as a terminal would, and a folder to
start it in (empty for the folder axMCP was started on, or your profile folder). F2 renames one of yours, Delete removes
one after a question, Ctrl+Z takes the last of these back. Claude Code's own servers are changed in Claude Code, with
`claude mcp add` and `claude mcp remove`; axMCP only reads its files.

## Settings

Options, Text sets the font and its size; Options, General the update check. Your servers are kept in
`%APPDATA%\AxKit\axMCP.json`, which Options, Developer, Open settings file opens in axJSON. Nothing a server answers is
kept, apart from the last result Ctrl+J writes beside the logs.

## Updates

The axKit menu, Check for updates.

## If something goes wrong

A server that does not start says why in a notice; its standard error goes to axMCP's log, which Options, Developer,
Copy diagnostics includes. A server that stops by itself is announced with its exit code; Enter on its level starts it
again. A call that gets no answer in 60 seconds is given up and said.

## About

The axKit menu, About.

## Disclaimer

axMCP comes with no warranty; see the licence in the axKit menu, About. The servers are their makers' programs under
their makers' terms; what a tool does when you call it is between you and that program.
