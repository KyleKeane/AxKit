# For your return

Written by the assistant (Fable) during the three days from 2026-11-27, at the maintainer's word to work alone and
to keep one record for the walk-through afterwards. Newest state first. This page is deleted once it has been gone
through together; what must last is in the specifications, the priorities list and the NVDA test record.

## Where things stand

Updated 2026-11-28, early morning.

- **Blocked on you: nothing.**
- **The release review** (item "the release review of 2026-11-27"): ten reviewers read everything since 5.10.0, about
  150 findings, no certain blocker. Fixed, tested, committed and pushed so far: privacy and the AI tools channel (18),
  the tools and the release path (23), the changelog rewritten to the end state, both of axClaude's sources (33),
  the apps' data paths (33), the access layers (18).
- **A second look at each fix batch**: a fresh reader checks every batch's commit for faults the fixes brought.
  It found 11 in the apps' batch (one a data-loss path in a list conversion on a full disk), 13 small ones in
  the privacy batch, and 8 in axClaude's (one grave: a doubled Enter could approve the next question unheard).
  Each set is its own fix batch. The second look at the access layers found four ways a repository's own program
  could still run in axGIT, and six ways a fetch or push could reach a server other than the one asked about;
  all fixed (the next decisions below). The second look at axClaude's corrections found that the one-second hold
  ended just as the window moved the focus onto a question; that fix batch is running. A second look at the
  axGIT fixes then found two more ways a repository's program could run (at opening, in a partial clone; on a
  push to a folder), so the next axGIT batch works by rules that need no list: a repository not trusted is
  never connected, git never fetches by itself, a folder remote is checked before a push.
- **Fable and Opus**: on 2026-11-28 you said Fable's credits were nearly spent. Every remaining fix batch has
  its written brief with the decisions in it, and the state, the order and the process are in
  `C:\Temp\axkit-tools\HANDOFF.md`, so that Opus can run them; the design work on branches (the graph's
  steps, axChat, the launcher) waits for Fable.
- **Then**: questions as lists; the shortenings with their keys; 6.0.0 prepared and not published; then, on branches,
  the workflow graph's remaining steps, axChat, axAutomate's macro launcher.

## What you decided before leaving (2026-11-27)

- Every question is a list like Claude's questions in axClaude: any key reads it, the arrows choose, it starts on the
  confirming option and Enter takes it, Escape declines, one option opens the full text. No exception for an AI's
  request: root rule 11 and AX-11.12 change with it.
- Escape keeps deselecting in a list.
- The gap in the AI tools' seal (an older sealed copy put back) is closed now.
- 6.0.0 is prepared and not published. NVDA passes at any time. llama.cpp's server and a small model installed
  outside the repository. All the shortenings built, their proposed keys included.
- After the release is ready: the workflow graph, then axChat, then the macro launcher.

## What I decided, for you to turn back

Each is one line to undo unless it says otherwise.

- **axTemplate's launcher does not put a private clipboard copy back.** After a template is put in, a password copied
  in axPass is not returned to the clipboard (ordinary text is). Giving it back would keep it past the 30 seconds
  axPass promises. The cost: you copy the password again. (axTemplate TP-D6.)
- **A damaged `bundle.json` gives no update check for that run**, where it gave one. A network contact you turned off
  must not come back by a damaged file.
- **Your lists move from `Documents\Tasks` whole or not at all** on an update: a file held by another program now
  keeps the whole folder where it was until the next start.
- **The AI tools' log names what happened**: "done: ran", "asked the user", "not run", "not known", with the
  command's real name.
- **axClaude over the stream no longer keeps a tool's whole output** in memory, only the three lines shown and the
  count; a key to read all of it would need it kept again (stream-mode.md SM-4.11).
- **A permission answer's label ends ", and more"** when the command is longer than the label ("1. Yes, Bash command
  git status, and more"). A lone file path never gets it; on the screen nearly every Bash question does, since
  Claude Code prints its description as a further row.
- **Every question holds its own keys for one second.** A question, list or field in the message field's place
  takes no Enter, Escape or Space for a second after it appears, also when it follows your own answer (a doubled
  Enter approved the next file unheard). The builder first made each list wait three seconds after your last
  answer; I turned that back, as `/config` would have felt dead. A key kept held down never answers, in any
  window. `AxKit.Core/KeyHold` has no way to end a hold early, and a test holds that.
- **A key the hold swallows makes the edge sound** (the kind "refused", which had that sound already). Yours to
  hear: one line in `ControlArea` and `OverlayPanel` (`AnswerKeys.HeldBack`) takes it out.
- **An answer typed into the message field before its question was said is not sent.** "Not sent: Claude asks
  something, and its question opens in a moment"; the text stays, and Enter sends it once the question was said.
- **After your own Ctrl+I, "Question closed" is not said**, and a question within three seconds of it comes
  without the chime.
- **An offered command with characters that cannot be seen is shown with their codes, not refused**: emoji use
  such characters too.
- **A day and month typed without a year is the next such day**, today included; 29 February is the next leap year's.
- **A save that would turn a character into a question mark asks "Save as UTF-8?"**, and Enter saves as UTF-8.
  The other way would be to refuse the save; this one keeps your text whole and says in the status bar what it did.
- **An item titled "list", or with a title that starts with a full stop, gets a file name with an underscore
  before it** (`_list.json`, `_.NET upgrade.json`). The title you read is unchanged.
- **Undo in axFile refuses when what it would remove has changed** since the operation made it, by size and time.
  It also refuses a copied folder into which Windows itself wrote a file (`desktop.ini`) afterwards.
- **A network drive with a Recycle Bin of its own (a redirected folder) is treated as having none**: the question
  says "for good" although Windows would have kept it. Windows gives no sure answer for such a drive.
- **A sound file of your own over 64 MB is not read.**
- **axPass asks before the first Delete in a group after each unlock.** KeePassXC's tool does not say which group
  is its Recycle Bin, and a group can merely be named so; axPass knows the bin only by seeing a deleted entry
  arrive in it, and until then cannot promise that an entry is kept. Remembering the seen bin per database would
  cut that to once in a database's life, at the price of trusting a memory. To turn back: `PassBin.Fate`.
- **axGIT passes over only settings it knows to be harmless** (`GitTrust.Inert`, 44 names, each with its reason).
  A list of the settings that run a program can never be complete, as the second look showed four times. So any
  other setting of a repository's own is named in the notice until you trust the repository: likely once each in
  repositories of yours with `commit.gpgsign`, `user.signingkey`, an alias, an `http.` or `url.` setting. A trust
  you gave earlier is asked again once where such a setting exists. A harmless name is added to the table with
  its one-line reason.
- **axGIT checks again before every refresh** (four git runs at once, about 0.15 seconds here), so a hook that
  appears while the window is open is asked about; with hundreds of submodules that is seconds per command.
- **While a repository is not trusted, axGIT runs no credential helper** (a private server then refuses), and
  no filters when a setting is not known (Git LFS files can show as changed; the notice says so). Git's
  automatic maintenance never runs through axGIT.
- **Fetch, Pull and Push run only when git can name the server**: on a branch whose remote is written as an
  address (as `gh pr checkout` of a fork's pull request writes it), with a group of remotes, and for a push
  without a branch, nothing runs and the notice says "Fetch not started" with the reason. Item written for the
  address case.
- **A command handed to a PowerShell window is refused when a part holds a double quote, is empty, or has a
  space and ends in a backslash**: Windows PowerShell 5.1 would split or join it, shown by a real run.
- **What a server that ended by itself had started lives until the axMCP window closes.**
- **axGIT's fetch, pull and push no longer go into submodules** (`--no-recurse-submodules`): the reminder before
  a server is contacted could not name the submodules' servers. With `submodule.recurse` set, Pull no longer
  updates them.
- **axGIT does not open a repository whose check for programs of its own fails**, where it opened it as running
  none; and a key pressed while a repository opens says "Working".
- **axMCP's servers end with the window at once** (a Windows job), where they had two seconds; and a command
  written as a relative path is the file in the folder the server starts in.
- **A hand-written mistake in the Templates `list.json` stops axKit from rewriting that file** until it is mended,
  so that nothing you wrote is lost; the window says what does not read.
- **axChat's first endpoint is Claude Code's stream**, the HTTP endpoint second; the reasons and the rest of my
  answers to the brief's questions are in `docs/axChat/spin-out.md` section 7.
- **The stream stays behind its switch in axClaude.** Making it the default waits for your own two hours with it.

## Owed your ear

Kept in `docs/axKit/nvda-test-record.md` and `docs/axKit/owed.md`; here only what is new since you left.

- The kinds without a sound yet: question-read-again, not-with-source.
- The edge sound on a key pressed within a question's first second (new; see the decision above).
- (More as the passes run.)

## What to try first

- (Written when 6.0.0 is prepared.)

## Things only you can do

- The file `approval-passphrase.md` in the folder beside the repositories: if it holds the passphrase that signs
  outside-report approvals, move it somewhere no session can read.
- A stash in this repository from 2026-11-09 ("Edits made in the wrong checkout ... drop when convenient"): yours to
  drop.
- Recordings of Anthropic's own API for axChat need a key that costs money: owed.
- The four outside reports that await your signature.
