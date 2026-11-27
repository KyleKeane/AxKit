# The plan: what is still to come

What has been asked for or suggested and is not built, and the order of the work in hand. What was built is
described by the specifications (`docs/axKit/SPEC.md`, `docs/ax<Name>/SPEC.md`) and held in `CHANGELOG.md` and the
commit history. Until 2026-11-27 this page was "How axClaude became axKit, and what is still to come"; its record
of the transition of 2026-10-02 and of each app's first plan is last at commit 4d50ef2b
(`git show 4d50ef2b:docs/axKit/plan.md`). Work in hand has its items in the priorities list (root `CLAUDE.md`,
rule 9); an idea on this page comes back by a new item that points here.

## The direction

- **The core mechanics first** (the maintainer, after 5.9.1, 2026-11-15): stable through his own use; his reports
  and the problems he meets first, faults in what exists before anything new, new features only where they remove
  friction he met. New apps and large features wait.
- **Held in code, for decades** (the maintainer, 2026-11-27): what is finished is locked in by a test, a check or a
  generated page, never by a practice. Every window is held to account by the workflow graph
  (`workflow-graph.md`) and the automated window checks (`automation.md`), so that a drift shows itself.
- **No goal over four actions, three where it can be** (bundle SPEC G7; `over-the-bounds.md` lists the goals that
  are over, and the list never grows).

## The order of the work in hand

The maintainer's order of 2026-11-27, each on a branch once 6.0.0 is prepared:

1. **6.0.0**: prepared and released at his word.
2. **The workflow graph's remaining steps** (`workflow-graph.md`, section 6): the scripts as walks (step 4), the
   map held still with the `Map change:` line (step 5), and teaching from the map (step 7). The shortenings of
   step 6 are `shortenings.md`.
3. **axChat** (`docs/axChat/SPEC.md`; `docs/axChat/spin-out.md`, sections 7 to 10): one contract for a
   conversation's source in the shared projects, the app on Claude Code's stream as its first endpoint, then the
   HTTP endpoint.
4. **axAutomate's macro launcher** (`docs/axAutomate/SPEC.md` AU-D4; `command-language.md`, section 8 (f)): a
   specification first; a macro is one more origin of the command gate.
5. **What is left of the event schema and the command language** (`events.md`, section 7; `command-language.md`,
   section 8).

Waiting on the maintainer and on nothing else: the stream as axClaude's default source (after his own two hours
with it, `docs/axClaude/stream-mode.md`), and a sound for each new kind of event (root rule 13).

## Still to come

- **axChat, a chat app over REST chat APIs** (asked 2026-11-25, high priority; specified in `docs/axChat/SPEC.md`,
  nothing built): a conversation like axClaude's over an HTTP chat API (llama.cpp's server, any OpenAI-compatible
  server, Anthropic, Ollama), the user picking a named API dialect, giving its address and key, and pressing Go,
  one step; dialects as data files mapping each API's requests and streamed events onto axKit's own events, with
  presets and the user's own. Decided since (2026-11-26 and 27, `spin-out.md`): Claude Code's stream, which
  axClaude has behind a switch, is its first endpoint and stays an option there; the window is the conversation
  window axClaude has, on one shared class. To settle in its build: a third network contact of axKit's own code
  (rule 8) and the AI caution on every reply (rule 11).

- **Guided learning, step by step** (the user's direction, 2026-11-16; recorded as a direction, not to be built
  yet): new users are coming, and on some days a person wants a thing done for them, on others they have the
  capacity to learn it. Three ways to one goal, all from the same source, the goals of `docs/axKit/workflows.md`
  (`user-workflows.md`): read the steps (built: the how-to pages, axHelp); have Claude do it (built 2026-11-24:
  axHelp offers once after the steps and runs nothing before a yes, ai-help.md AH-7.3b, rule 11); and be talked
  through it, one step at a time, each step marked done as the user does it, the next one said only then. The
  third is this entry. What it would rest on, all of which exists or is being built: a goal's steps as data (the
  scripts' goal lines and `do` steps), the windows' own events to tell that a step happened (the automation
  channel's trace lines, WF-5.1, show what a window said and where the focus went), and the workflow record's shape
  for "done" per step. What it needs first: the foundations stable (the converter, the AI tools mode of the checks,
  axClaude and axHelp in the command inventory), the goal titles heard and settled by the user, and a specification
  of its own with its cautions. Its heuristics for learners are the bundle's rules already: a key has one meaning in
  every window (rule 5, AX-8), every action says a short message, nothing happens that the user did not ask for.
- **What is left of things that are built.** axClaude's open items are in `docs/axClaude/SPEC.md`, section 12, the
  one list for them. axJSON: structured mode, a form of real input fields from templates (AJ-3), and the caret's
  path in the status bar (AJ-2.4); by the user's direction (2026-10-24) structured mode follows the item apps'
  field editing (`item-apps.md`) and gets a focused pass of its own, not started before it. AI help
  (`ai-help.md`, section 8): axDown's and axJSON's AI items with Offer a change (AH-3.2), and a path in an answer
  opened with Enter (AH-3.3). The editors: a watcher for live reload while the window stays active, only if the
  check at activation (AD-3.6) turns out not enough. Path completion (2026-10-23): asked as a shared resource for
  every path field of the bundle; it moves from `AxFile.Core.PathCompletion` to the shared projects with the first
  other app that has a path field, untangled from `FolderListing` then. The screen's picture in axHelp
  (`docs/axHelp/SPEC.md` HE-3): on that foundation, a parser that segments the screen into objects and actions on
  them (move the mouse to an object, click it), an agent the user steers by speech and keys; his thought is that
  these may be add-ons run from the launcher, each a script with a clear interface, rather than apps.
- **A gutter for the transcript views** (the user's idea, 2026-10-19, agreed as a later bundle feature): two narrow
  columns to the left of a transcript, the line number and the jump letter of the line's kind (`i`, `o`, `h2`), for
  sighted helpers, drawn next to the view and never spoken, since in an edit control what is in the text is what NVDA
  reads; and, as two options off by default, the same two as text in front of each line, which NVDA then reads on
  every arrow. What is spoken stays on demand: `l` for the line number, the jump keys with their announcements, and
  Options → Text → Markers in the text (axClaude FR-4.6a, built 2026-10-19) for the `#` markers. The spoken-but-
  invisible variant would need a rich edit control and is not planned.
- **Custom keys to scripts** (the user's thought, 2026-10-21, kept for later rather than built): a key the user binds
  to one of their own programs or scripts, so that one shortcut runs it on the entry or the folder in hand, with the
  key table and the collision test knowing the binding. The custom programs list (axFile AF-4.11) is what such
  keys would point at; what is open is where a user's bindings live (the settings file, a submenu) and how they show in F1.

- **The years ahead.** `longevity.md` is the plan for keeping the repository a stable resource for decades: what
  already holds, what was adopted on 2026-10-23, the proposals with their cost, what breaks when, and the calendar
  (every release, every month, every October, every LTS of .NET). It came out of the review of 2026-10-23, whose
  rewrites and fixes are all in the code; that review, the inventories of 2026-10-24 and 2026-11-09 and the cleanup
  run are finished and in the git history. Open from them: the longevity items for the maintainer (`longevity.md`
  3.2 the organization, 3.13 the archive branch, 3.14 the yearly pass).

- **axKit's own folder and file picker** (asked 2026-10-23; kept for later with the later apps, its priority to be chosen; spec first): in place of the Windows Open, Save as and folder pickers, an owned modal window in front of the window it serves (as the Windows picker stays), with a menu bar for the extras (favourites, sort, hidden files, "Use the Windows picker instead"). The view is axFile's folder list with its keys (arrows, Enter, Backspace, Left and Right, type-ahead; Home and End stay the first and last entry); Ctrl+Left goes to This PC, in axFile too; Ctrl+G opens the Go to field with the path completion, which also completes favourites and the known folders by name (places by name without crowding the list). The Save version adds a name field; every version has buttons for its action and Cancel. It takes axFile's list, `FolderListing` and the path completion into the shared pieces. The user weighed the Windows dialogs' familiarity against axKit's consistency; the choice was axKit's own picker with a way back to Windows'. **Asked again on 2026-11-22**, as a longer-term goal he thinks is small enough to do fairly quickly inside axKit, in two stages: first axKit's own Open and Save as in place of the Windows ones inside axKit's windows; later, as something the user turns on, the same for the Open and Save as dialogs of other programs, which have no hook he knows of, "so we mught need to do something like monitor for those system dialogs and swap them for ours by capturing user input and passing it onto the system dialog". To settle in its spec: the picker as a place inside the window, since a window has no second window of its own (AX-7.2), or as the one exception the Windows pickers are today; and for the second stage, what watching other programs' dialogs and typing into them needs (no global keyboard hook is installed today, and such a watcher reads what other programs show), which is a setting off at first and a line in `PRIVACY.md` and `SECURITY.md` if it is ever built.

## Parked on 2026-11-13: asked for, not built

Before the work on a second agent window (GitHub Copilot's CLI beside Claude Code) the priorities list was cleared of
everything that is an idea and not work in hand, at the user's word. Each idea is kept here in a line, with the day it
was asked; its full notes (the scope, the open questions, the proposal made) stay in its closed item of the
priorities list, found there by its title. An idea comes back by a new item that points here. None of these is
started, and each needs its spec first.

Features of the apps that exist:

- **axAutomate: workflows as JSON** (asked 2026-11-10): one file per workflow, each step a small fixed record with its
  operation, what to do when it fails and the message to report. **Values from the system in a workflow**, such as
  the clipboard's contents (asked the same day), waits on that format.
- **One shared shape for axList, axTask and axAutomate's workflows** (asked 2026-11-10): templates with actions,
  conditions and links, separate windows over as much shared code as possible; the automated tests as workflows the
  user can open and change.
- **axKit's lists: a filter field, symbol jumps and nesting** (asked 2026-11-12): any list filtered by typed text or a
  pattern, "filtered" said before the list's name, a symbol jumping to the next item that holds it, nested lists.
- **axGitHub: an access layer over the `gh` CLI for issues** (asked 2026-11-14, not built), on the shared item window:
  a repository's issues as items, read and changed through `gh`, which signs in and reaches GitHub in its one place
  (rule 12). It takes the place of axTask's GitHub lists, removed the same day when axTask became local only
  (`docs/axKit/reports.md` RP-7), and may reuse their components; spec first.
- **axTask: @ as assignment to people** (asked 2026-11-11), with notification of their items and a sort by assignee;
  the smart tags already gather one list per @name.
- **axTask: lists across projects gathered, notified and searched** (asked 2026-11-10): the axTask folders found under
  folders the user names, one gathered view, notifications.
- **Every operation of axKit through its AI tools** (asked 2026-11-10). Much of it exists since: the tools run a
  window's commands by name and add an axTask item (`mcp.md` MC-7, MC-8); what is left is the typed data tools of the
  other apps.
- **A remind-me skill** (asked 2026-11-10, not to build yet): "remind me next week ..." in a conversation makes an
  axTask item with its Due date, through the AI tools.
- **axClaude: a backlog of questions** (asked 2026-11-12) that Claude keeps and asks when the time is right, proposed
  as an axTask list Claude adds to instead of asking at once.
- **axClaude: say when text was not spoken** before a question took the focus (asked 2026-11-12); knowing what NVDA
  spoke needs an add-on installed for the user.
- **axClaude: the lines of a tab screen in the conversation**, and the newer screens of Claude Code (from its SPEC,
  section 12, which keeps axClaude's own open items).
- **axJSON: structured mode and the caret path**, after the item apps' field editing (the user's direction,
  2026-10-24).
- **axKit's own folder and file picker** (asked 2026-10-23), in place of the Windows pickers, with axFile's list and
  keys. Asked again on 2026-11-22 and back in the priorities list: its entry under "Still to come" says how.
- **A gutter for the transcript views** and **custom keys to scripts**: described under "Still to come".

New apps: **axList** (lists to remember as Markdown files; a draft is on the tag `archive/axlist-draft`), **axSQL**
(a SQLite browser), **axHTTP** (a REST client), **axMail** (mail over IMAP and SMTP, the largest build), **AxVault**
(Cryptomator vaults; AxCryptomator below), and, on record only, **AxWeb**, **AxCalendar** and **AxSettings**. Each is
described under "Later apps". Of axTemplate, built on 2026-11-23, the placeholders and a form behind a template are
left, paused at his word the same day (below, under 2026-11-25).

## Parked on 2026-11-25: far future, not to build yet

The priorities list was cleared a second time at the user's word (2026-11-25, through the question tool: "Move them to
plan.md"), so that it holds only work to be finished. Each of these he had called far future or not to be built yet;
each is a line here, and its full notes (his words, the scope, the first thoughts) stay in its closed item of the
priorities list, found by the title in the line. An idea comes back by a new item that points here.

- **Installing the programs under an access layer** (2026-11-22): whether axKit installs KeePassXC or Claude Code
  itself or separate helper scripts do, with the boundary of what axKit vouches for. Item: "Access layers: whether
  axKit installs the program underneath".
- **axClaude: a self-voicing option for replies** (2026-11-14, a reflection): replies spoken by axKit's own voice so
  that their order is axKit's. Item: "axClaude: a self-voicing option for replies".
- **axDown: a comment on a line, and Claude rewriting a line in place** (2026-11-14). Item: "axDown: a comment on a
  line".
- **axGitHub over gh** (asked again 2026-11-14, above under 2026-11-13): issues synchronized and acted on from axTask.
  Item: "axGitHub: an app of its own for GitHub issues".
- **axGithubCopilot** (2026-11-13): an access layer over the GitHub Copilot CLI sharing axClaude's code and window.
  Item: "axGithubCopilot: an access layer".
- **axHelp on the item in focus** (2026-11-15): "move this to my documents" said to axHelp about the focused item,
  done for the user, with how to do it oneself as a follow-up. Item: "axHelp: a request about the item I am on".
- **axHelp: a search index or grammar rules, and a log of questions and whether the answer served** (2026-11-17).
  Item: "axHelp: a stronger way to find answers".
- **axHelp and AI help for another AI service** (2026-11-22). Item: "axHelp and AI help: easy to configure".
- **Natural-language operations through Shift+F1**, and what Shift+F1 is for beside axHelp (2026-11-24). Item:
  "axHelp and Shift+F1: natural-language operations".
- **A class and an id for every element, recent items and actions, analytics** (2026-11-24): the scaffolding for
  localization, Shift+F1's context and unambiguous automation. Item: "axKit: a unique id for every item".
- **An action history with undo across the open apps** (2026-11-23). Item: "axKit: an action history with undo".
- **axKit's own Open and Save as**: its entry under "Still to come" says how. Item: "axKit: axKit's own Open and Save
  as".
- **Guided learning**: its entry under "Still to come". Item: "axKit: guided learning".
- **Localization into other languages**, tied to the ids above. Item: "axKit: localization into other languages".
- **Sound as events over OSC to a separate audio engine** (2026-11-14). Item: "axKit: sound as events sent over OSC".
- **The keyboard actions of the focused item as a list on one key** (2026-11-21), beside the context menu and F1.
  Item: "axKit: the keyboard actions of the item in focus".
- **axTask: AI operations over a list**, such as grouping by theme (2026-11-22, from the old feature list). Item:
  "axTask: AI operations over a list".
- **axTask: a portable copy of a list or of all lists, and bringing one in** (2026-11-22). Item: "axTask: save a
  copy of a list".
- **axTask: the approval of outside reports made simpler** (2026-11-24), to discuss what felt slow first. Item:
  "axTask: the approval of outside reports made simpler".
- **axTask's lists on a phone**, synced by any service, with an open format anyone could write a viewer for
  (2026-11-22). Item: "axTask: the lists on a phone".
- **Placeholders in a template and a form behind it** (paused 2026-11-23). Item: "Editors: placeholders to fill in a
  template".
- **The item window's file field** (proposed 2026-11-14 for the earcon lists, which were removed on 2026-11-17;
  parked at his word on 2026-11-25): a field holding a file beside the item, opened in the axKit app for its kind or
  played in place with Enter. Item: "Item window: a file field".

## Later apps

Each would be one more app of the bundle, a window per thing worked on, added the way `adding-an-app.md` says, with
no library dependency (B-10), local first (B-15), on plain files in clear formats that an AI system or a script can
read and change as easily as the app does. The order the user set for any of them: the specification, then the
infrastructure, then the build.

- **AxCryptomator** (asked 2026-10-18; no spec yet; Cryptomator is a product name too, AxVault the alternative): a
  window to unlock and lock Cryptomator vaults, since the desktop app's JavaFX window reads poorly with NVDA: a list
  of known vaults, unlock with the password asked in a notice, the unlocked vault as a drive or folder with "Open in
  axFile" on it, lock. The engine: the project's own `cryptomator-cli` as a process per vault, kept running while the
  vault is unlocked and ended to lock it; it takes the password on standard input or from a file and mounts the vault
  (WinFsp, which the desktop installer brings) or serves it on a local address. The vault format (scrypt, AES-SIV, the
  rest) is never reimplemented. To verify before the spec: the current release's Windows support and mount options,
  and how it lives beside a running desktop app. Local first (B-15): the vault's cloud copy is the user's sync client's
  business.
- **axList** (asked 2026-10-23 as AxList, with AxTask, placeholder names; the user keeps the two in view as separate
  apps, 2026-11-06): a list manager for things to remember and to arrange, with bullet, numbered and checkbox lists
  nested in plain Markdown files, one file per list; later, links between items for mind maps, curriculum planning,
  recall in plain language and structured storage. A spec with its guide and test plan was brought from the branch into
  `main` on 2026-11-06; its section 10 held what to settle with the user before the build (what a checkbox item means
  beside axTask, whether the Tasks view across lists stays, the first step of links). Parked since 2026-11-13, the
  three were deleted on 2026-11-26 and are last at commit fc3109e6 (`git show fc3109e6:docs/axList/SPEC.md`). Nothing
  is built on `main`; the branch's draft code is the tag `archive/axlist-draft`,
  and the branch is gone. AxTask, as first asked, was the checkbox lists as a task manager with reminders, automations
  and smart lists, perhaps one app with AxList at first; it became axTask. Other names considered: AxNotes,
  AxTodo. The rule the user set for both: plain text files on disk with a clear, documented format, so that an AI
  system or a script can read and change them as easily as the app does; no database the user cannot open in axDown
  (axTask's JSON files meet it one way, axList's Markdown another).
- **axHTTP**, a REST client: requests with a history, the reply as a transcript, a JSON body opened in axJSON.
  Postman and the like are web views. .NET's own HTTP client; network use only on the user's command. axChat
  (above) is the specialized app for chat APIs and comes first.
- **axSQL**, a SQLite browser: the tables, a query field, the result as a list read row by row with a key for the
  whole row. Windows ships SQLite (`winsqlite3.dll`), so no library.
- **axMail**, a mail reader and writer: the largest daily need and the largest build (IMAP, SMTP, OAuth sign-in for
  Gmail and Outlook), so it waits until the small apps have proved their patterns.
- **On record only** (2026-10-23): **AxWeb**, a simple browser for reading the web as a transcript of headings,
  links and text (the hard part is the engine: WebView2 is a web view, which B-10 rules out, so a reader over
  fetched HTML is the likely shape); **AxCalendar**, dates and reminders (shared with axTask's due dates);
  **AxSettings**, a plain list of the Windows settings a user changes most.
- **Not planned**: a full browser (a web view, B-10; AxWeb is a reader) and a PDF reader (Windows has no built-in
  way to pull text from a PDF; axPreview's picture renderer reads scanned pages through Windows' own OCR).
