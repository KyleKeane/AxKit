# axKit's listing in the Microsoft Store

The texts and settings of axKit's Store listing, one section per field of Partner Center's submission, in the order
Partner Center asks for them. This page is the source: a change to the listing is made here first and pasted there
(by hand until the submission runs from the command line, `store-distribution.md` phase 7). The identity values are
in `store-distribution.md` 3.0; the rules the texts meet are `docs/axKit/SPEC.md` AX-13.6 and Microsoft's Store
policies (10.2.4 dependencies disclosed first, 10.5.1 a privacy policy, 11.16 live generative AI disclosed). Written
2026-11-08 for the first submission; nothing here is a secret.

## Pricing and availability

- Price: Free.
- Markets: all.
- Visibility: Make this product available and discoverable in the Microsoft Store (winget's Store source needs it).
- Device families: Windows 10 and 11 desktop, x64 (the package declares `Windows.Desktop`, minimum 10.0.19041).

## Properties

- Category: Developer tools.
- Privacy policy URL: https://github.com/KyleKeane/AxKit/blob/main/PRIVACY.md
- Website: https://www.kylekeane.com
- Support contact: https://github.com/KyleKeane/AxKit-reports/issues
- Product declarations: this product has been tested to meet accessibility guidelines (yes: NVDA, every window, the
  test plans under `docs/<app>/nvda-test-plan.md`); it accesses the internet (the update check on Check for updates,
  a report the user sends; the access layers through the programs they run).
- Live generative AI: yes. axClaude shows the output of Claude Code, Anthropic's command-line program, which the user
  installs and signs in to; axKit generates nothing itself. Users report content with F4 in any window (a report to
  the maintainer) and to Anthropic through Claude Code's own means.

## Age ratings

Answer the IARC questionnaire truthfully. axKit has no violence, no purchases, no advertising, no user-to-user
communication of its own and collects no personal information. axTerminal runs the user's shell and axClaude the
user's Claude Code, so the questions about unrestricted internet access are answered yes; the resulting rating is
accepted whatever it is.

## Store listing (English)

### Description

The first sentence names what the access layers need (policy 10.2.4); the rest is the README's opening.

> axClaude needs Claude Code (Anthropic's command-line program), axGIT needs Git, axPass needs KeePassXC and
> axTerminal's PowerShell 7 and Git Bash need those programs; each is installed separately, under its maker's terms,
> and axKit offers to install one from its maker only when you choose, with winget. The standalone apps need only
> Windows.
>
> axKit is a set of accessible Windows apps for people who use a screen reader, made for NVDA and working with any
> screen reader that reads ordinary Windows text fields. They are the tools a blind scientist uses every day, shared
> as they are: personal tools, not a product or a service, free and open source (AGPL-3.0). Every app is made of native
> Windows controls, works without a mouse, says the result of each command once, and keeps its questions and errors
> inside its own window.
>
> The apps: axClaude, a front end for Claude Code that shows the conversation as plain text you move through line by
> line; axDown, a plain editor for Markdown and text files with navigation by heading, link and list; axJSON, the same
> editor for JSON files with a Format command; axGIT, a git manager for one repository; axTerminal, your shell as a
> transcript; axFile, a file manager for one folder with an Actions list of everything you can undo; axMedia, a player
> for sound and video files with one queue; axPreview, a read-only viewer for a text file; axStart, a program launcher
> behind a hotkey; axProcess, the processes, services and startup programs as one list; axTask, tasks and lists as a
> folder of small files; axPass, a KeePass password database read and changed by ear; axCalc, a calculator with a
> history; axClip, a clipboard history behind a hotkey.
>
> axClaude shows the output of a live generative AI, Claude Code, with your own Anthropic account; axKit generates
> nothing itself and never sees that traffic. Press F4 in any window to send a report to the maintainer.
>
> axKit collects nothing and has no telemetry. Its own code makes a network request only when you check for updates
> (one request to GitHub for the latest release) or send a report; the access layers reach the network only through
> the programs you installed, as those would without axKit. The privacy policy and the full list of everything the
> program contacts are in the repository: https://github.com/KyleKeane/AxKit.
>
> Updates come from the Store. The source, the guides and the specifications: https://github.com/KyleKeane/AxKit.

### What's new in this version

The `CHANGELOG.md` section of the version, as the release notes carry it.

### Product features (up to 20, each under 200 characters)

- axClaude: Claude Code as a plain-text conversation you move through line by line, with a message field, questions as lists, and every answer announced once.
- axDown: a plain editor for Markdown and text files, with navigation by heading, link, list and table.
- axJSON: the same editor for JSON files, with navigation by key and a Format command.
- axGIT: one repository's changes, log and branches as lists; commit, discard, fetch, pull and push after your consent.
- axTerminal: PowerShell, Command Prompt or Git Bash as a transcript, with the output of each command found by a key.
- axFile: a file manager for one folder, with an Actions list of every operation and Ctrl+Z for each.
- axMedia: a player for sound and video files with one queue, speed and pitch control.
- axPreview: a read-only viewer for one text file, with the same navigation keys as the editors.
- axStart: a program launcher behind a global hotkey, with the first match spoken as you type.
- axProcess: the processes, services and startup programs as one list with three views.
- axTask: tasks and lists as a folder of small files, with typed fields and GitHub lists.
- axPass: a KeePass password database read and changed by ear, through KeePassXC's own tool.
- axCalc: a calculator as one expression field and a history list.
- axClip: a clipboard history behind a hotkey, pasted into the window you came from.
- Every window: a command palette, the same keys for text size and find, notices inside the window, F4 to send a report.
- Made for NVDA: native Windows controls, every control named, every result said once, nothing needs a mouse.

### Search terms (up to 7, each under 30 characters)

screen reader, NVDA, accessible, Claude Code, Markdown editor, git manager, terminal

### Copyright and trademark info

Copyright Dr. Kyle Keane, www.kylekeane.com. AGPL-3.0-only.

### Additional license terms

GNU Affero General Public License, version 3: https://github.com/KyleKeane/AxKit/blob/main/LICENSE, with the notices
in https://github.com/KyleKeane/AxKit/blob/main/NOTICE.md.

### Developed by

Kyle Keane (the account's publisher display name is axKit).

### Screenshots (PNG, 1366 by 768 or larger, up to ten; captions under 200 characters)

One per app's window at 1920 by 1200, cropped to the window's visible frame, taken by `tools/store-screenshots.ps1`
while the maintainer is away from the keyboard (`store-distribution.md` phase 5), into `publish\store\`. The files the
windows show live in a demo folder under the Public profile for the minutes of the run, since a status bar prints the
folder's path and an account's name is personal data; checked by eye against that before the upload (2026-11-08: the
first run had the maintainer's folder in axFile's status bar and was retaken). The captions, in the files' order:

1. axDown with the axKit guide: "axDown: a Markdown file, moved through by heading, link and list."
2. axFile with a folder and its Actions list: "axFile: one folder, and every action listed to undo."
3. axTask with a list: "axTask: tasks and lists as small files, with typed fields."
4. axJSON with a JSON file: "axJSON: a JSON file, moved through by key, with a Format command."
5. axCalc with its history: "axCalc: one expression field and a history list."
6. axProcess with its list: "axProcess: the processes, services and startup programs as one list."

Two more come with `-WithAccessLayers`, once the maintainer has connected the two on this computer (until then they
show their consent reminder): axTerminal, "axTerminal: a shell as a transcript, each command's output found by a key",
and axGIT, "axGIT: one repository's changes, log and branches", the latter by hand on a repository of his choosing.
axClaude's shot, "axClaude: a Claude Code conversation as plain text, one line per key", is taken by hand on a
conversation of the maintainer's choosing, since the script would start a Claude Code session on his account.

### Store logos

The 1:1 logo, 300 by 300 pixels: `publish\msix\StoreLogo-300.png` (`tools/make-icon.ps1 -Png` writes it beside the
package's images: the letters Ax on dark blue, the bundle's icon).

## Submission options

- Notes for certification: axKit is a desktop app for screen reader users; test it with NVDA (https://www.nvaccess.org/)
  or Windows Narrator: every window is native Windows controls; F1 in any window lists its keys. The access layers
  (axClaude, axGIT, axTerminal, axPass) need Claude Code, Git, a shell and KeePassXC installed and say so with an
  Install it now button when the program is missing; the other ten apps need nothing. The package declares
  `runFullTrust` because the apps are a Windows Forms desktop application that starts those programs and reads the
  console they run in. axClaude shows the output of a live generative AI, Claude Code, under the user's own Anthropic
  account.
- Restricted capabilities, the box "Why do you need the runFullTrust capability, and how will it be used in your
  product?" (500 characters at most; the section stays Incomplete without it): runFullTrust: axKit is a Windows Forms
  desktop application packaged as MSIX; it starts the user's programs (Claude Code, git, a shell, KeePassXC's tool)
  and reads the pseudo console they run in, which needs full trust.
- The notes for certification are entered on the Additional Testing Information page (Supplemental info), not on
  the Submission options page, which only points there.

Entered as above in Submission 1 on 2026-11-08 (`store-setup.md` section 8); the keywords stand in the order screen
reader, accessible, Claude Code, Markdown editor, git manager, terminal, NVDA.
