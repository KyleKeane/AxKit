# axGIT — Specification

axGIT is the fourth app of the axKit bundle (`docs/axKit/SPEC.md`): a git manager for one repository, for a blind
developer who uses the NVDA screen reader. Built 2026-10-18 and released in axKit 2.12.0; the whole test plan passed under NVDA on
2026-10-22 (the record: `docs/axKit/nvda-test-record.md`); what each release brought is section 8. The document is kept
in step with the code (a behaviour change updates it in the same commit).
Requirements are numbered `AG-n.m` and decisions `AG-Dn`; the bundle's rules, AX-7 (accessibility), AX-8 (keys) and
AX-9 (the Window, axKit and Help menus), are cited by number and not repeated. In axGIT's code and documents, "SPEC.md" means this file.

## 1. Summary

An **access layer** for Git (bundle AX-11, 2026-10-29): titled "axGIT, access layer for Git"; before Git first runs, the consent notice (AX-11.4); before the first fetch, pull or push to each server (the host of every address git would use for that command, AG-5.4a, `AxGit.Core/GitRemote`), the consent for that server, and the first time a window reaches a server after it, the sign-in line (AX-11.6).

A developer who works in axClaude and axDown wants to know the state of the repository under them and to act on it
without a terminal: which files changed, what changed in one of them, stage some, commit with a message, look at the
log, switch a branch, pull and push. axGIT opens one repository in one window, named after it. The window is one list
that shows one of the repository's three lists (Changes, Log, Branches), one read-only text for details (a diff, a
commit), a status bar with the branch, and the bundle's notices for the commit message and every question. It is
a navigator, as axTask's and axProcess's (AG-2.2): the three lists are rows of the repository's level, the window
starts inside Changes, Space selects files, and staging, unstaging and discarding act on the selected files. Everything is the
`git` program the user already has: axGIT runs it and reads its output, and reimplements nothing (AG-D2).

The window is reached from the other windows: axClaude's Session → Show current folder in ▸ axGIT and the editors'
File → Show current file in ▸ axGIT open axGIT on the repository that holds the folder or file, and axGIT's Repository
→ Show current repository in ▸ sends the user back to axClaude on the repository, to axTerminal, to axFile or to File
Explorer; a changed file
opens in the editor for its type (AG-1.4, AG-D1).

## 2. Goals and non-goals

- **G1 The daily set, streamlined.** Status, diff, stage and unstage, discard, commit, log and a commit's details,
  branches with switch and new, fetch, pull, push. Conflicts are shown as what they are and left to the terminal or to
  Claude (AG-D4).
- **G2 Native controls only.** A list box, a read-only edit control, a status bar, the bundle's notices (AX-7.1). No
  tree, no grid, no colours that carry meaning.
- **G3 Nothing spoken unprompted** (AX-7.3; axClaude D26): arrows explore, a key acts, and the result is one
  announcement. The focus moves by itself only to the details text after Enter on a file or a commit, which is the
  user's own command to read it.
- **G4 git as it is.** Every action is one git command with its own messages; axGIT adds no state of its own beyond
  the recent repositories and the window.
- **Non-goals.** Merge and rebase, stashes, tags, remotes management, submodules, interactive hunks, conflict
  resolution, a history graph, credentials (git's own helpers do that; a prompt that git would show in a terminal
  fails here and is reported). Several repositories in one window.

## 3. Scenarios

1. In axClaude on a project, Alt+S, I, G: axGIT opens on the repository, the Changes list focused: "Changes, 3 files.
   Modified src/Foo.cs, unstaged, 1 of 3". Ctrl+Shift+S: "Staged src/Foo.cs". Ctrl+Enter, type the message, Enter:
   "Committed 3a1f2c0". Alt+W, O, 1 back to axClaude.
2. Enter on a changed file: the diff in the details text, the focus on its first line; Down Arrow reads it line by
   line; Ctrl+Tab back to the list.
3. Alt+2: "Log, 200 commits. 3a1f2c0 2026-10-18 Kyle: Release 2.11.0, 1 of 200". Enter: the commit's files and
   message in the details.
4. Alt+3: "Branches, 2. main, current, origin/main, 1 ahead". Down, Enter on `axkit`: "Switched to axkit". Ctrl+P:
   "Pushed".
5. In axDown on `docs/plan.md`, Alt+F, I, G: axGIT on the repository that holds the file. Alt+1 shows it among the
   changes if it changed.

## 4. Functional requirements

### AG-1 Launch

- AG-1.1 `AxKit.exe git [<folder or file>]`; the console command `axGIT`; the Start menu entry "axKit axGIT"; axGIT under
  "Open with axKit" on folders in File Explorer (AX-2.3). A folder or file given alone to `AxKit.exe` still means axClaude or an editor
  (AX-1.2); axGIT is always named by its verb.
- AG-1.2 The repository is the one that holds the path: `git rev-parse --show-toplevel` from the folder (or the file's
  folder). Without a path, the last repository opened, or none: the window opens with "No repository" and Open
  repository… (Ctrl+O), the Windows folder picker, which resolves the chosen folder the same way. A folder in no
  repository gives the notice "Not a git repository" naming the folder; a repository already open stays open behind
  the notice (until 2026-10-23 it was dropped first). A recent repository that is not on a drive of this computer is
  not looked at during the start (bundle AX-9.5a): the newest one on this computer opens, or none, and when the last
  repository was on the network the window says "<path> is on the network and was not opened at start; Open recent
  opens it." A path given, and one chosen from Open recent repository, is looked at off the window's thread.
- AG-1.3 `AxGitApp.Run(args)` is the entry point (AX-1.7): `Log.UseApp("axGIT")`, the crash handling, `--help` and
  `--version`, the settings (`axGIT.json`), then `GitForm`.
- AG-1.4 **To and from the other windows.** axClaude's Session → Show current folder in ▸ **axGIT** starts axGIT on
  the project folder (axClaude FR-8.2); axDown's and axJSON's File → Show current file in ▸ **axGIT** on the file
  (axDown AD-4.1). Each is a new window, a new process (`WindowMenu.Start`), found again in the Window menu's Open windows
  (AX-9.3). From axGIT, Repository → Show current repository in ▸ **File Explorer** (no key; Ctrl+W until 2026-11-26; "Folder missing" when the repository's folder is gone, bundle AX-9.10), ▸ **axClaude**
  (a new axClaude on the repository's folder), ▸ **axTerminal** (a shell there, docs/axTerminal/SPEC.md AT-1.5) and
  ▸ **axFile** (the folder listed, docs/axFile/SPEC.md AF-1.3), and Changes → **Open file in editor** (Ctrl+E) opens
  the selected file in axJSON for `.json` and axDown otherwise.
- AG-1.5a When git is missing (neither on the PATH nor in Program Files, `GitRunner.Program`), its notice offers
  Install Git now (bundle AX-2.9) and, once git is there, opens the repository.
- AG-1.5 Git itself: `git.exe` in Git for Windows' install folder (`Git\cmd` under Program Files or Program Files
  (x86), which only an administrator can write), and only when it is not there in the PATH's full folders that are
  on a drive of this computer, never the current folder and never a share (`GitRunner.Program`,
  `SystemPrograms.Installed`; bundle AX-11.5 and `repairs.md` R-70, 2026-11-16: the agreement is for Git by its
  name, so a `git.exe` put earlier in the PATH must not be what runs; until then the PATH came first; the tests
  point it at a fake `git.cmd` for the output, the timeout and the open pipe),
  run with `-C <repository>`, `-c core.quotepath=false`, `-c color.ui=never`, `-c core.fsmonitor=false`,
  `-c protocol.ext.allow=never` (security audit S-1, S-2), diffs and `show` with `--no-ext-diff --no-textconv`,
  `GIT_TERMINAL_PROMPT=0` and UTF-8 output; one command at a time, on a background task, never on the UI thread
  (AX-7.5), with a timeout (60 seconds, 10 minutes for fetch, pull and push). A command stopped at its timeout
  failed, whatever it had printed until then (the review of 2026-11-27: until then what it had printed was thrown
  away, and a run that git had finished, with a program it started still holding its output open, was a success
  with no output, so that a status read that way showed no changes): the notice has git's own complaint, then "git
  did not finish within 60 seconds and was stopped." A command git finished while a program it started (a signing
  agent, a hook's daemon) held its output open until the timeout is git's own result, with its exit code and what
  it printed; a list (the status, the log, the branches) is not read from such a run, since the other program may
  have written into the same output, and the failure says "git finished, but a program it started kept its output
  open until the time limit." When `git` is not found, the notice
  **Git was not found** says where to get it (git-scm.com) and the window stays with every command disabled.
- AG-1.6 **A repository's own programs** (2026-10-24, security audit S-2). A repository that arrives with its `.git`
  folder (an archive, a USB stick, a share) can name programs git runs: a file monitor (`git status`), diff and filter
  drivers, hooks (commit, switch), an ssh command (fetch, push), a credential helper. On opening, before any other
  command, axGIT reads the names of the repository's configuration (`git config --show-scope --includes --name-only
  --list`, which runs nothing; the names of every scope but the computer's, the user's and the command line's) and
  its hooks folder (`git rev-parse --git-path hooks --git-path modules`). Every setting that is not known to be
  harmless (AG-1.6c) and every hook is a risk (`GitTrust`). While the repository is not trusted git is run without
  them (AG-1.6c says how), and the notice **This repository runs programs of its own** names them ("Its settings
  or hooks name programs git would run: …. axGIT runs git without them."; then, where there are any, "It also has
  settings axGIT does not know to be harmless: …", with what that turns off; with such settings alone the notice
  is titled **This repository has settings axGIT does not know**), with **Keep them off** (Enter, Escape) and
  **Trust this repository**, which keeps the repository in the settings (`trusted`, AG-8.1a) and lets git run
  all of it as in a console from then on. The trust is for what axGIT found when it was given (2026-11-16,
  `repairs.md` R-53): it is kept with a mark of the risks (`GitTrust.Mark`, the SHA-256 of their names, lower
  case and sorted), and a trusted repository in which axGIT now finds something else (a hook more or fewer,
  another configured program, another setting it does not know) is not trusted: git is run without them again
  and the notice comes back, starting "What axGIT finds in this repository differs from what you trusted, so it
  is not trusted now." (the sentence does not say the repository changed: after an update of axGIT the check may
  be what did); Trust this repository then trusts what is there now. The mark is of the names: a hook's file or
  a setting's value changed under the same name is not seen.
- AG-1.6a **What the read covers, and when it runs** (the review of 2026-11-27; `GitTrust`, `GitTrustScan`).
  - *Every file git reads for the repository*: its own, what that includes (`include.path`, `includeIf`), and the
    work tree's own file where the repository has one (`extensions.worktreeConfig`, `config.worktree`). `--local`,
    asked until then, left the work tree's file out, so that an ssh command or a filter set there ran.
  - *The names the notice calls programs*, and sets to nothing by name: the core programs (`core.fsmonitor`,
    `hooksPath`, `sshCommand`, `pager`, `editor`, `askPass`, `gitProxy`, `alternateRefsCommand`); `diff.external`;
    a diff, merge or filter driver's and a tool's command (`diff.<name>.command` and `textconv`,
    `merge.<name>.driver`, `filter.<name>.clean`, `smudge` and `process`, `difftool` and `mergetool` `.cmd` and
    `.path`); `pager.<command>`; `credential.helper` and `credential.<url>.helper`; `gpg.program`,
    `gpg.<format>.program` and `gpg.ssh.defaultKeyCommand`; `sequence.editor`; `trailer.<name>.command` and `cmd`;
    `hook.<name>.command`; `remote.<name>.uploadpack`, `receivepack`, `proxy` and `vcs`;
    `uploadpack.packObjectsHook`. Since the review's second pass this list no longer decides whether a repository
    is asked about (AG-1.6c does): it is the wording of the notice and what is set to nothing by its name. A name
    whose middle part differs only in capitals (`diff.PDF.textconv`, `diff.pdf.textconv`) is two names, and both
    are set aside.
  - *The repositories inside*. git runs itself inside a submodule on a plain `git status` and obeys that
    repository's own settings there (tried with git 2.53: the submodule's file monitor ran). So each is read the
    same way: every entry of the index that is another repository and has its work tree here (`git ls-files
    --stage -z`, mode 160000; one with no `.gitmodules` to say so too); every folder with a repository of its own
    that the index does not hold yet (`git ls-files --others --exclude-standard -z` lists it as one entry with a
    slash at its end; the first Stage all makes it an entry, and git then runs itself in it on every status, with
    that repository's hooks); and every repository git keeps under `.git/modules`, with or without a work tree.
    One inside another is followed twelve levels down (`GitTrust.Deep`), and the folders under `.git/modules` as
    far; what lies deeper is named as not read. What one would run is named with it, "submodule libs/zip:
    core.sshcommand", "submodule libs/zip: hook post-checkout", is part of the trust's mark, and is set aside
    with the rest: what axGIT gives git reaches git's runs inside a submodule (tried with git 2.53). A repository
    inside whose settings git itself cannot list is named "submodule <path>: settings that could not be read".
  - *When, and where*: the read is one piece of work, before the repository becomes the window's, so no command
    runs in it meanwhile ("Working: opening…"); until then the repository was the window's first, and a key
    pressed while the read ran started git in it with nothing set aside. All of it runs off the window's thread
    (bundle rule 3; `GitTrustScan.ReadAsync` starts on a task from its first step, and its git runs go four at
    a time): the folder reads between the git runs were on the window's thread until the second pass, and 200
    submodules took 18 seconds there. A read that fails (git's error, a timeout) opens nothing: the notice
    **Repository not opened** says "What <path> would run could not be read, so axGIT runs nothing in it. Git
    said: …", and a repository already open stays. A repository is never taken as running nothing because it
    could not be asked.
  - *Files on another computer* (the review's third pass of 2026-11-27, D9; `GitTrust.PointsElsewhere`). git
    reads through a share at its first command (tried with git 2.53 through `//localhost/c$`: a work tree's `.git`
    file, an include of the settings and an alternate object store were all read there). So before the first
    git run in a folder, and again in the read of every repository inside and in every later check, axGIT reads
    in its own code, of files on this computer alone: the `.git` file of the work tree (its `gitdir:`), its
    `commondir`, the `path` lines of its settings files and of the files they include, and its
    `objects/info/alternates`. A path on another computer (a share whose host is not the one the repository
    itself lies on) opens nothing: the notice **Not opened** says "This repository points at files on another
    computer, <host>: its .git file, an include of its settings or its object store lies there, and git would
    read them at its first command. Open it on that computer, or copy it here." A guard, not a reader of git's
    settings: a line that only looks like an include counts too, and what it cannot read is left to git.
- AG-1.6b **The check again** (the review's second pass of 2026-11-27). The check was one look as the repository
  opened, and the ground moves under it: a settings file included for one branch alone (`includeIf "onbranch:…"`)
  counts after a switch (tried: its filter ran on the next diff); a pull or a discard writes a file the settings
  include; Stage all makes a folder with a repository of its own an entry of the index. So the whole read of
  AG-1.6a is made again after every command that changes the repository (stage, unstage, discard, commit, switch,
  a new branch, fetch, pull, push), whether it succeeded or failed, and at F5 ("Working: checking…"), and what git
  is run with is worked out from what is there then. Since the review's third pass (D8, L6) this is one place,
  `GitSession.ChangeAsync`, which checks in a `finally` after the command: no command runs on what was set aside
  before the command ahead of it. Until then the check came with the lists read after a command that succeeded,
  and a failed command (a pull that stopped halfway, a switch that wrote some files) left the old settings in
  force. When the risks are others than the user trusted, or than the user was last asked about and kept off,
  the notice of AG-1.6 comes again; the same risks are asked about once. After a command that failed, its
  sentence ("Switch failed. Git said: …") stands first in that notice, which a second notice would replace. When
  the read fails then, or the repository now points at another computer, the repository is closed, since
  nothing is known of it any more: the notice **Repository closed** says "What <path> would run could no longer
  be read, so axGIT runs nothing more in it and has closed it. Git said: …".
- AG-1.6c **Only what is known to be harmless is passed over** (the review's second pass of 2026-11-27;
  `GitTrust.Inert`). Until then the rule was the list of the names that run a program, and such a list is never
  complete: git and the programs it starts gain names without notice. Two were not on it and ran: Git LFS's
  `lfs.extension.<name>.clean` (on every add and diff of a file that goes through its filter) and
  `gc.recentObjectsHook` (through git's automatic maintenance after a commit). The rule is turned around.
  - *The table*. A setting is harmless only when its name is in `GitTrust.Inert`, one table, each name with the
    reason why it runs no program, reaches no server and moves no file's place (a test holds the form, and that no
    entry is a name that runs a program, names a proxy or includes a file). It holds what an everyday repository
    carries, as made by `git init`, `git clone`, `git submodule`, `git worktree`, `git sparse-checkout`, Git for
    Windows, GitHub's gh, Visual Studio Code and Git LFS: the `core` settings git writes itself
    (`repositoryformatversion`, `filemode`, `bare`, `logallrefupdates`, `symlinks`, `ignorecase`, `autocrlf`, `eol`,
    `safecrlf`, `precomposeunicode`, `longpaths`, `fscache`, `untrackedcache`, `preloadindex`, `sparsecheckout`,
    `sparsecheckoutcone`); a remote's `url`, `pushurl`, `fetch`, `tagopt` and gh's `gh-resolved`;
    `remote.pushdefault`; a branch's `remote`, `merge`, `rebase`, `pushremote`, `description` and the notes of
    Visual Studio Code and gh (`vscode-merge-base`, `github-pr-owner-number`, `gh-merge-base`); `push.default`,
    `pull.rebase`, `pull.ff`, `user.name`, `user.email`, `init.defaultbranch`; a submodule's `url` and `active`;
    `extensions.worktreeconfig`, `objectformat` and `refstorage`; Git LFS's `lfs.repositoryformatversion` and
    `lfs.<address>.access`; and, in a repository inside alone, `core.worktree`, which git writes for every
    submodule. `include.path` and `includeIf` are never in it. Every other name is a risk, named in the notice
    as a setting axGIT does not know to be harmless, and part of the mark. An ordinary repository opens without
    the notice; one with a setting of the user's own that is not in the table is asked about once, and trusted.
  - *Where the value decides the harm, the value is read* (the review's third pass, D5; `GitTrust.ValueRisks`,
    `git config --show-scope --includes --null --get-regexp`). `remote.<name>.fetch` is harmless only when every
    destination lies under `refs/remotes/` (a line without one fetches into FETCH_HEAD alone, one with a caret
    leaves branches out); otherwise it is named, "remote.origin.fetch writes outside refs/remotes" (with
    `+refs/heads/*:refs/heads/*`, axGIT's fetch with `--prune` deleted every local branch, M3). In a repository
    inside, `core.worktree` on another computer is named, "core.worktree lies on another computer". Every other
    name of the table was gone through with the question what its value can do: a number or a switch; a text
    git does not read; an address, read before anything reaches it (AG-5.4a); a remote's name, which axGIT reads
    and puts on the command line; a branch's name, which a push now names itself (AG-5.4); `pull.rebase` and
    `branch.<name>.rebase`, which even set to `interactive` open nothing under axGIT's `--ff-only` (tried with
    git 2.53).
  - *What git is run with while the repository is not trusted* (`GitTrust.Overrides`), given through git's
    environment (`GIT_CONFIG_COUNT`, `GIT_CONFIG_KEY_n`, `GIT_CONFIG_VALUE_n`), after any the user's own
    environment holds. As `-c <name>=` arguments, until then, a name with an equals sign in its middle part
    (`filter.a=b.clean`) was cut at the wrong sign and set nothing aside. Every name that runs a program or
    names a proxy (`http.proxy`, `http.<address>.proxy`, `remote.<name>.proxy`) is set to nothing; with
    `core.gitProxy` named, `protocol.git.allow=never`, since git takes the first proxy command it reads, the
    repository's, and an empty one after it set nothing aside; `core.hooksPath` set to an empty folder of
    axKit's own, a full path (`%LOCALAPPDATA%\AxKit\axGIT-no-hooks`, made when missing and emptied before each
    use; `NUL` until the review's third pass, L4, which git takes for a folder relative to the work tree), also
    for the scan's own runs but its question for the hooks folder, and an empty `credential.helper`, always
    (the second empties git's whole list of helpers, the user's own among them); and,
    when a risk is a setting axGIT does not know (or a repository inside that could not be read), every filter
    git knows, of every file, the user's own too, set to nothing and not required. A filter is a program of the
    user's own that may obey the repository's settings, as Git LFS does, and a setting that is not known cannot
    be set aside by its name; so no filter runs. A file that goes through a filter can then show as changed,
    which the notice says ("do not stage such a file"). Hooks and known programs alone leave the user's filters
    running.
  - *A repository that is not trusted is never connected* (the review's third pass, D1; `GitSession.NotConnected`).
    Setting aside by a name kept failing where a transport's settings are concerned (`remote.<name>.uploadpack`,
    whose first value git takes, the repository's; git's own transport for a folder, which clears what axGIT
    gives through the environment). So while any risk is kept off (the notice answered with Keep them off, or
    not answered), Fetch, Pull and Push do not start: the notice **Fetch not started** (Pull, Push) says "Fetch
    not started: this repository has settings you have not trusted. Fetch, Pull and Push start once you trust
    it: Show its settings names them, with Trust this repository. Nothing was sent and nothing was fetched.",
    with **Show its settings** (Enter), which shows the notice of AG-1.6 again, and Close. Reading, staging,
    committing and switching go on with what is set aside.
  - *For every run in every repository* (`GitRunner.Command`, `GitRunner.FixedEnvironment`): the file monitor and
    the `ext::` transport off, as before, and `maintenance.auto=false` with `gc.auto=0`: git's automatic
    maintenance is what runs `gc.recentObjectsHook`, a setting with several values that an empty one does not set
    aside. axGIT never tidies a repository; git in a console does, and `git maintenance`. Since the review's third
    pass: `GIT_NO_LAZY_FETCH=1`, so that git never fetches a missing object of a partial clone by itself (a
    clone made with `--filter=tree:0` ran the remote's upload program on its first status, before the notice was
    answered, H1; a missing object is now git's own error, said as it is, and git before 2.44 does not know the
    variable); and `GIT_LFS_SKIP_SMUDGE=1` with `GIT_LFS_SKIP_PUSH=1`: Git LFS fetches and sends no file through
    axGIT (a switch in a repository with no risk at all posted to the host a tracked `.lfsconfig` named, M5).
    **A Git LFS file stays a pointer file** after a switch or a pull in axGIT; `git lfs pull` in a terminal
    fetches the files.
  - Each of these is tested against git itself with a script that leaves a line when it runs, in repositories
    made for the test (`GitTrustScanTests`, `GitSessionTests`), and each test also runs the script once the way
    it did run. The test that closes the class (`GitSessionTests`): every name of every kind of program name
    (`GitTrust.ProgramNamesTable`, each kind with names of it written out) set to such a script, each in a clone
    of its own, and every command axGIT has run untrusted: the script never runs.
  - Not covered (open): what a setting axGIT does not know does that is no program and no proxy stays in force
    for reading, staging, committing and switching while the repository is not trusted (`http.sslVerify`,
    `url.<base>.insteadOf` and the like reach nothing then, since nothing is connected, D1); the notice names
    every such setting. A trusted repository runs everything it names, as in a console.

### AG-2 The window

- AG-2.1 From the top: the menu bar; the list, named with what it shows and its count ("Changes, 3 files", "Log, 200
  commits", "Branches, 2"; "<repository>, 3 lists" at the repository's level); the details text under it, read-only, named "Details" and, after a diff, "Diff of <file>"; the
  status bar. Tab, Shift+Tab, Ctrl+Tab, Ctrl+Shift+Tab and F6 move between the list and the details (AX-8.2); a
  notice takes the place of both while it shows (AX-6.4).
- AG-2.2 **Levels** (a navigator, the bundle's item window, `docs/axKit/framework.md` FW-4.10; since 2026-11-22, three
  views of one list until then, AG-D3). The repository's level, "<repository>, 3 lists", has three rows, Changes,
  Log and Branches, each with its count once it has been read ("Changes, 3 files"). Right or Enter on one goes into
  it; Left or Backspace in a list goes up, onto the list left; Left at the repository's level and Right on a row of
  a list play the edge sound. The window starts inside Changes. Alt+1 Changes, Alt+2 Log and Alt+3 Branches go
  straight into a list from anywhere (Navigation; Ctrl+1, Ctrl+2 and Ctrl+3, the keys until 2026-11-16, stayed as
  further keys until 2026-11-26, bundle AX-8.14). The key
  says the list's name and the row you are on, also when that list already shows (axClaude D26); while a command
  runs it says "Working…" and the list stays (2026-10-23: the switch went through with an empty list under the new
  name; since 2026-11-22 the level changes only once the list's read is in). Each list keeps the row it was left on
  while the window is open, by its key (the file's path, the commit's hash, the branch's name); F5 reads the status
  and the list shown again, at the repository's level all three, and says "Refreshed: <the list's name>". A list
  is read on its first visit and again after a commit, a switch or a fetch made it stale.
  Space selects files in Changes (bundle AX-8.16; AG-3.2, AG-3.4a): a selected file reads ", selected", the status
  bar counts them, Shift with an arrow selects as it moves, Shift+Space to here, Ctrl+A all, Ctrl+Shift+A and
  Escape none. A commit and a branch are not selected, since nothing acts on several of them: the edge sound.
- AG-2.3 The title is `<repository folder name>, axGIT, <the repository's full path>` (the maintainer's word of 2026-11-26, bundle AX-9.6; `axGIT, access layer for Git folder <full path>` from 2026-11-23, `<repository folder name> - axGIT` before), and `axGIT` alone without one. The status bar's left
  label: the branch, then the upstream state when there is an upstream ("main · 2 ahead" / "main · 1 behind" / "main
  · up to date" / "main · no upstream"), then the counts ("3 changed, 1 staged"); "Working: pull…" while a command
  runs. The right label: the repository's full path. No AccessibleName, no Spring (axClaude §6.2).
- AG-2.4 The details text: the bundle's edit keys (Page Up and Page Down, Ctrl+Up and Ctrl+Down by paragraph, AX-8.2),
  Ctrl+A and Ctrl+C, find with Ctrl+F, F3 and Shift+F3 (the match selected, "Not found" when none); with the focus in the
  list, Ctrl+F, F3 and Shift+F3 find in the list's lines instead ("Name contains", the bundle's list find, as every list
  window; since 2026-10-25, the user's choice; they always searched the details before). Nothing moves the
  caret but the user, except that Enter on a file or a commit puts the focus on the first line of what it loaded.
- AG-2.5 **Filter and sort** (2026-11-22; the navigator's, bundle AX-8.17): Options → Filter… shows only the rows of the level whose name contains a text (a changed file's path, a commit's subject, a branch's name); Options → Sort by ▸ puts them by name or back in git's own order; Ctrl+Delete in the list clears the filter. Both act on what the list shows; no git command is run for them.

### AG-3 Changes

- AG-3.1 The Changes view lists `git status --porcelain=v2 --branch` (`GitStatus.Parse`): one item per file, "<kind>
  <path>, <state>": kinds Modified, Added, Deleted, Renamed (with "old → new"), Copied, Type changed, Untracked,
  Conflict; states "staged", "unstaged", "staged and unstaged"; an untracked file and a conflict have no state. Order:
  conflicts, then the rest as git lists them. The list's name is "Changes, N files" ("Changes, no changes" when the
  tree is clean).
- AG-3.2 **Stage** (Actions and Context, Ctrl+Shift+S) and **Unstage** (Ctrl+Shift+U) act on the selected files, or
  on the file you are on when none is selected (2026-11-22; until then Space staged the file you were on or
  unstaged it, and Space selects now). Stage takes each whole file with anything unstaged (`git add -- <paths>`,
  an untracked one too), Unstage each with anything staged (`git restore --staged -- <paths>`), one git call for up
  to fifty paths, every path after `--`. The rows change in place, the selection stays so that Commit can follow,
  and one sentence is said: "Staged <path>" or "Staged 3 files", "Unstaged <path>" or "Unstaged 3 files". With
  nothing to do: "<path> is already staged", "<path> is not staged", "Nothing to stage among the 3 selected files",
  "Nothing staged among the 3 selected files". A conflict is not staged from here ("A conflict: resolve it in the
  editor or the terminal first"). In another list than Changes both say "For the files of Changes: Alt+1".
  Actions → Stage all (`git add -A`) and Unstage all (`git restore --staged .`) say "Staged all" and "Unstaged
  all", whatever is selected; they have no key since 2026-11-22 (Ctrl+Shift+A, Stage all's, is the bundle's
  Deselect all, and Ctrl+Shift+U is Unstage): Ctrl+A, then Ctrl+Shift+S, does the same with keys.
- AG-3.3 Enter on an item shows its diff in the details ("Diff of <path>"): `git diff --cached` for a staged file,
  `git diff` for an unstaged one, `git diff HEAD` for one that is both, the file's own text for an untracked one
  (first 200 KB, and no more of the file is read, since 2026-11-27; "Untracked file, N lines" as the first line,
  or "Untracked file, the first N lines" with "(cut after 200 KB)" as the last when the file goes on), `git diff` for a conflict. The focus goes to the first
  line of the details; "No changes" is announced and the focus stays when the diff is empty.
- AG-3.4 Changes → Discard changes… (Delete) on a tracked file asks "Discard the changes to <path>? They cannot be
  brought back." with Discard and Cancel; Discard runs `git restore --staged --worktree -- <path>` and says
  "Discarded <path>". An untracked file is not deleted from here ("Untracked file: delete it in File Explorer").
  Delete on a commit says "A commit is not removed here", on a branch "A branch is not deleted here", on a list of
  the repository's level "A list: Right goes into it".
- AG-3.4a **Several files** (2026-11-22, AX-8.16). With two or more files selected, Delete asks once, "Discard the
  changes to 3 files?", with "They cannot be brought back." and each path; the untracked files among them are
  counted in a line of their own as left alone, and with only untracked files selected nothing is asked ("Untracked
  files: delete them in File Explorer"). Discard (Alt+D) and Cancel, **Cancel the default**, since several files
  go at once. One sentence, "Discarded 3 files"; the selection is given up and the list read again.
- AG-3.5 Changes → Commit… (Ctrl+Enter): with nothing staged, "Nothing staged. Ctrl+Shift+S stages the selected files" and no notice;
  otherwise the notice "Commit N staged files" with a text field "Message" (required; the first line is the subject)
  and the buttons Commit and Cancel. Commit runs `git commit -m <message>` and says "Committed <short hash>"; the
  Changes view refreshes. A failure (a hook, an identity not set) is the notice **Commit failed** with git's output.

### AG-4 History

- AG-4.1 The Log view lists the last 200 commits of the current branch (`git log -n 200 --date=short
  --format=<hash><tab><date><tab><author><tab><subject>`, `GitLog.Parse`): "<short hash> <date> <author>: <subject>".
  The list's name is "Log, N commits".
- AG-4.2 Enter on a commit shows `git show --stat --format=medium <hash>` in the details ("Commit <short hash>") with
  the focus on its first line.

### AG-5 Branches

- AG-5.1 The Branches view lists the local branches (`git branch --list --format=…`, `GitBranches.Parse`): "<name>,
  current" for the branch checked out, then its upstream and state when it has one ("origin/main, 1 ahead", "up to
  date", "2 behind, 1 ahead"), "no upstream" otherwise. The list's name is "Branches, N". A detached HEAD is no branch: the line git prints
  for it, "(HEAD detached at <hash>)", is left out and no branch reads current (until 2026-10-23 it was listed as
  one); the status bar says "detached" (AG-2.3).
- AG-5.2 Enter on a branch switches to it (`git switch <name>`): "Switched to <name>", the views refresh; Enter on the
  current one says "Already on <name>". git's refusal (changes that would be overwritten) is the notice **Switch
  failed** with its output.
- AG-5.3 Branches → New branch… (Ctrl+N): the notice "New branch" with a field "Name" (required) and the buttons Create
  and Cancel; Create runs `git switch -c <name>` and says "Switched to <name>".
- AG-5.4 Branches → Fetch (Ctrl+R, `git fetch --prune`), Pull (Ctrl+L, `git pull --ff-only`) and Push
  (Ctrl+P, `git push`; Ctrl+Shift+F, Ctrl+Shift+L and Ctrl+Shift+H until 2026-11-26, the maintainer's choice of Ctrl alone, bundle AX-8.18; with no upstream `git push --set-upstream origin <branch>`), each with the remote of AG-5.4a on its command line. Each says "Fetched",
  "Pulled" ("Already up to date" when git says so) or "Pushed" and refreshes the status bar; a failure is the notice
  **Fetch failed** (and so on) with git's output, so that a password prompt that git could not show, a diverged
  branch or a rejected push is read in full. While one runs the status bar says "Working: <verb>…" and the menu
  items that run git are disabled; nothing else is blocked. None of the three starts in a repository that is not
  trusted (AG-1.6c, D1).
  - *The remote's own programs are git's* (the review's third pass, D2): `--upload-pack=git-upload-pack` on the
    command line of a fetch and a pull, `--receive-pack=git-receive-pack` on a push's, also in a trusted
    repository: the command line comes before every setting, and git takes the first `remote.<name>.uploadpack`
    it reads, the repository's.
  - *A push says exactly what it pushes* (D6): `git push --no-recurse-submodules --receive-pack=git-receive-pack
    [--set-upstream] -- <remote> refs/heads/<branch>:refs/heads/<branch>`, the branch you are on to the branch of
    its own name, whatever `push.default` says. A branch named `+main` is that branch (pushed as "+main" it was
    read as a forced push of main, M4). When the branch follows a branch of another name (`branch.<name>.merge`),
    nothing is pushed: **Push not started**, "This branch follows a branch of another name, <name>, and axGIT
    pushes a branch to the branch of its own name. Push it in a terminal." (git's own default refuses there too).
  - *Git LFS* (D7): a push in a repository whose pre-push hook is Git LFS's (`git lfs install` wrote it) does not
    start: **Push not started**, "This repository's pre-push hook is Git LFS's, which would send the LFS files to a
    server of its own that axGIT does not ask about. Push in a terminal, where Git LFS sends them." (tried with
    git 2.53 and Git LFS 3.7: in a trusted repository the hook posted the files to the host a tracked
    `.lfsconfig` named, which no consent had named). Every run also has `GIT_LFS_SKIP_PUSH=1` (AG-1.6c).
- AG-5.4a **The server asked about is the server reached** (the review of 2026-11-27 and its second pass; bundle
  AX-11.5 and rule 8; `GitReach`, `GitRemote`). Before a fetch, pull or push, git is asked by commands that reach
  no server and run nothing the repository names, and the command then runs with the remote they gave on its
  command line.
  - *The remote* is read from git's settings (`git config --show-scope --null --list`) by git's own order: for a
    fetch or pull the branch's `remote`, else the one remote the repository has, else origin; for a push the
    branch's `pushRemote`, else `remote.pushDefault`, else the same; for the push of a branch without an
    upstream, origin, which that push names and ties the branch to. That name is on the command line, after
    `--` so that a name like an option is a name: `git fetch --prune --no-recurse-submodules --no-all
    --upload-pack=git-upload-pack -- <remote>`, `git pull` likewise with `--ff-only`, `git push
    --no-recurse-submodules --receive-pack=git-receive-pack -- <remote> refs/heads/<branch>:refs/heads/<branch>`
    (AG-5.4). Until the second pass git was asked which
    remote it would use (`for-each-ref`) and then run without one, and the two did not always agree: a branch
    with a remote and no branch to follow (origin was asked about, the branch's remote reached), `fetch.all`
    (every remote reached), a push from a detached HEAD with `remote.pushDefault` and `push.default=matching`
    (origin asked about, the default remote reached).
  - *The addresses*: `git remote get-url -- <remote>` for a fetch or pull, the one address a fetch uses, and `git
    remote get-url --push --all -- <remote>` for a push, every address a push goes to (each `pushurl` the remote
    has, else its `url`), each as git rewrites it (`url.<base>.insteadOf`, `pushInsteadOf`). The consent is asked
    for the host of each, one after the other; a share's is the computer it lies on (M1: until the review's third
    pass a share counted as this computer and asked nothing); an address on this computer asks nothing. The
    reminder says the port when the address gives one: "Connecting to host.example, port 2222; …".
  - *An address is read by a strict form, and anything else is not read* (the review's third pass, D4;
    `GitRemote.Read`, tested against a table of hard addresses). Accepted are exactly: `https://host[:port]/path`
    and `http://` (a user, and after it a colon and a password of the characters an address needs no escape for,
    may stand before the host), `ssh://[user@]host[:port]/path`, `git://host[:port]/path`; the scp form
    `[user@]host:path` with no slash before the colon and a host of more than one letter; a path on this computer
    (a drive, its colon and a slash or backslash, then a valid Windows path: no name with `< > : " | ? *`, none
    ending in a dot or a space, none a device's name; `.` and `..` and a path starting `./` or `../`, either
    separator; an absolute path starting with one slash; `file:///` and a drive); and a share (`//host/share`,
    `\\host\share`, `file://host/share`). A user and a host are ASCII letters, digits, dots and hyphens (a user an
    underscore too), never starting with a hyphen or a dot; an IPv6 host stands in brackets, in a form with a
    scheme only; a port is 1 to 65535. A host with letters outside ASCII is read in its punycode form alone
    (`xn--…`), which is what reaches the network anyway. Everything else is **not read**, and nothing runs: the
    notice says "The remote origin has an address axGIT does not read: <address>. axGIT reads an address of https,
    http, ssh or git, the form user@host:path, a share, or a path on this computer." Until then the reader
    followed git's own lenient rules, and each lenience named one host and reached another:
    `[127.0.0.1]:x@[evil.invalid]:repo.git` read as 127.0.0.1 while git ran ssh to evil.invalid; `c:nul`,
    `c:a:b`, `c:repo.` and `d:x<y` read as folders while git, taking them for no valid Windows path, ran ssh to
    the host c or d (tried with git 2.53 and an ssh that printed its arguments). A transport that starts a
    program (`ext::`, any `name::`, any other scheme) is not read, as before.
  - *Nothing runs when git cannot name the server*: a remote git does not know (also a name that is an address,
    where nobody was asked and the command ran), an address no server can be read from, a remote's name that is
    a group of remotes too (`remotes.<name>`: git fetches the group's remotes in place of the one named, found
    while this was tried), and a push while HEAD is on no branch. The notice **Fetch not started** (Pull, Push)
    says why, with git's own line, and "Nothing was sent and nothing was fetched." A branch that follows another
    branch of this repository (the remote `.`) reaches no server, and its pull runs without a question.
  - Each row is tested against git itself, in repositories whose addresses are of the reserved name "invalid":
    the command is run through a proxy on a closed port of this computer, so that nothing leaves it, and the host
    git's own trace names is the one that was asked about (`GitTrustScanTests`).
  - Fetch, Pull and Push are run with `--no-recurse-submodules`: a submodule has remotes of its own, on
    servers this consent does not name, and git would reach them by itself (a fetch on demand, a pull or push
    where the repository's settings say to recurse); a submodule is fetched and pushed in a terminal.
  - A sign-in in an address never reaches the log or a notice (`GitRemote.WithoutSignIn`): git names a remote it
    does not know with its whole address, the user and the password in it, and the first line of a failed
    command is written to the log (AG-8.2). The complaint of every git run, its first line and its whole text
    are without the part between the scheme and the host, by the type that carries them (`GitResult`).
  - A repository cloned without all of its objects (a partial clone) fetched a missing one from its remote when a
    status, a diff or a show needed it, with no consent asked for. Since the review's third pass git never fetches
    by itself (`GIT_NO_LAZY_FETCH`, AG-1.6c): such a command fails with git's own words, and a fetch or a pull
    brings what is missing.
- AG-5.4b **A proxy the repository names** (the review's second pass of 2026-11-27). A proxy is where the bytes
  go first. `http.proxy`, `http.<address>.proxy` and `remote.<name>.proxy` are not harmless settings (AG-1.6c):
  the repository is asked about, and while it is not trusted they are set to nothing. In a trusted repository,
  where the settings name a proxy at all, git is asked which one it would use for each http or https address
  (`git config --show-scope --get-urlmatch http.proxy <address>`; the remote's own comes first, as for git);
  when it comes from the repository's own settings its host needs the consent as a server does, and the first
  time the window reaches it the line is "Connecting through the proxy <host>, which this repository's settings
  name". A proxy of the user's own settings, or of the computer's, is the user's and is not named. A proxy whose
  host cannot be read stops the command, as an address does.
- AG-5.4c **A remote that is a folder is a second repository** (the review's third pass of 2026-11-27, D3;
  `GitReach.FolderProblemAsync`). git reaches an address on this computer, a share, or the remote `.` with its own
  transport for a folder, which runs the other repository's receive hooks on a push and clears what axGIT gives
  through the environment (tried with git 2.53: a push to `.` with only harmless settings, and a push to a bare
  repository in the work tree named `./vendor/mirror`, where no risk was found at all, both ran every receive
  hook, H2). So before a push into a folder (after the consent for a share's computer) the repository there is
  read as a repository inside is read (AG-1.6a): the guard of D9, its settings against the table, the hooks folder
  git names for it, the values that decide. Anything found, or a repository that cannot be read, and nothing is
  pushed: **Push not started**, "The repository this remote names, <folder>, has programs or settings of its own:
  <names>. A push runs them there; push in a terminal if you mean to." There is no trust for it. A bare
  repository as `git init --bare` makes one takes the push. A fetch and a pull from a folder run nothing of the
  other repository's (tried: its hooks, `uploadpack.packObjectsHook`, `core.fsmonitor`, `core.alternateRefsCommand`,
  with the upload program named on the command line) and stay allowed.

### AG-6 Menus, keys, texts

- AG-6.1 Menus (AX-9.1, the bundle's eight since 2026-11-11): **Session (S):** Open repository… Ctrl+O | Open recent
  repository ▸ (ten, "1. name (path)", AX-9.5; "No recent repositories" when none) | ─ | Refresh F5 | ─ | Show current
  repository in ▸ (File Explorer | axClaude | axTerminal | axFile) | ─ | Exit Alt+F4. **Actions (A):** Stage
  Ctrl+Shift+S | Unstage Ctrl+Shift+U | Stage all | Unstage all | Commit… Ctrl+Enter | ─ | New branch… Ctrl+N |
  Fetch Ctrl+R | Pull Ctrl+L | Push Ctrl+P | ─ | Select all files Ctrl+A | Deselect all
  Ctrl+Shift+A. **Navigation (N):** Changes Alt+1 | Log Alt+2 | Branches Alt+3 | ─ | Up to the repository Left (the
  View menu's until 2026-11-11). **Context (C)**, what acts on the row you are on, by its kind (AX-9.11; Shift+F10,
  the Applications key and a right click in the list): on a file, Show diff Enter | Stage Ctrl+Shift+S | Unstage
  Ctrl+Shift+U | Open file in editor Ctrl+E | Discard changes… Delete | ─ | Select or deselect Space | Select to
  here Shift+Space; on a commit, Show the commit Enter; on a branch, Switch to branch Enter; on a list of the
  repository's level, Open Enter; "Nothing selected" on an empty list. **Options (O):** Text ▸ (Font… | Larger text Ctrl+Plus | Smaller text Ctrl+Minus |
  Windows text size) | Developer ▸ (Open settings file) | ─ | the settings of all of axKit (AX-9.1, 2026-11-26).
  **Window (W)**, **axKit (K)** (AX-9.1). **Help (H):** Command palette… F1 | Keyboard shortcuts | User
  guide | ─ | Git resources ▸ (git-scm.com | Pro Git book). Every item has a mnemonic (AX-7.4).
- AG-6.2 Keys (AX-8). Bundle-wide: F1 and Ctrl+Comma, the command palette with the keys (Ctrl+Shift+P until
  2026-11-26), the control keys, Ctrl+F, F3, Shift+F3, Ctrl+S (axGIT holds nothing to save:
  "Nothing to save here. Ctrl+Shift+S stages, Ctrl+Enter commits"), Ctrl+Enter (the bundle's main action, AX-8.10: here Commit, AG-3.5, a command on `BundleKeys.MainAction`), Ctrl+Plus, Ctrl+Minus, the edit keys of the details,
  Alt+F4. Window-specific (`AxGitKeys`): Ctrl+O, F5 (Ctrl+W, File Explorer, until 2026-11-26), Alt+1, Alt+2, Alt+3 (AX-8.14), Ctrl+Shift+S, Ctrl+Shift+U, Ctrl+E, Ctrl+N, Ctrl+R, Ctrl+L, Ctrl+P, Ctrl+Delete (clear the filter), and the navigator's keys
  (`ItemKeys`, `SelectionKeys`), named in the table by what they do here: Right, Left, Backspace, Enter, Delete,
  Tab and Shift+Tab, Space and the other selection keys, Shift+F10 and the Applications key. The navigator's keys
  act only while the list has the focus; in the details they are the control's own, but for Tab, Shift+Tab and
  Escape, which come back to the list. F2, Ctrl+Z, Ctrl+Shift+N and F7, a navigator's rename, undo, new list and
  read in full, are no keys of this window, which has no use for them. The collision test covers the table. Enter
  and Delete act by the kind of row (AG-3.3, AG-3.4, AG-4.2, AG-5.2), so the rule of 2026-10-23, that a command of
  one view asked for in another said which view first ("Changes view first: Alt+1"), is gone with the views; what
  is left of it is Stage and Unstage outside Changes (AG-3.2).
- AG-6.3 The F1 text (`HelpText.Shortcuts`) names them all; `docs/axGIT/user-guide.md` is embedded (Help → User guide);
  About is the bundle's (AX-9.9).

### AG-7 Announcements

One UI Automation notification each, on the list, or on the notice while one shows (AX-7.3): "Staged <path>",
"Unstaged <path>", "Staged 3 files", "Unstaged 3 files" and the four sentences of AG-3.2 for nothing to do, "Staged
all", "Unstaged all", "Nothing staged. Ctrl+Shift+S stages the selected files", "Committed <hash>", "Discarded
<path>", "Discarded 3 files", the selection's words (AX-8.16), "Untracked file: delete it in File Explorer", "No changes", "Diff of <path>, N lines", "Commit <hash>, N
lines", "Switched to <name>", "Already on <name>", "Fetched", "Pulled", "Already up to date", "Pushed", "Refreshed:
<list name>", "Changes, N files" and the other lists' names when a list's key is pressed in the list already shown,
"<repository>, 3 lists, <the list left>" on the way up,
"Working…" when a command is asked for while one runs, "Nothing to save here. Ctrl+Shift+S stages, Ctrl+Enter commits",
"Not found" (find), "Text size n", "Windows text size", "Copied diagnostics", "No repository" (a command without
one), "No file selected", "No commit selected", "No branch selected", "For the files of Changes: Alt+1" (Stage or Unstage
in another list, AG-3.2), "A commit is not removed here", "A branch is not deleted here", "A conflict: resolve it in the editor or the terminal first" (a pull or a switch that git refuses for
one). A failure that git explains in one line is announced as "Failed: <line>"; more than one line is a notice.

### AG-8 Settings, status

- AG-8.1 `%APPDATA%\AxKit\axGIT.json` (AX-5.3; `GitSettings`): `fontFamily`, `fontSize`, `fontBold`,
  `checkForUpdates`, `recentRepositories` (ten, newest first), `trusted`, `seal`, `window`. The defaults are the
  Windows font, the update check on, no repositories.
- AG-8.1a **The trusted repositories are sealed** (bundle AX-5.6, 2026-11-16): `trusted` is a list of `{"path",
  "risks"}`, the repository's root and the mark of AG-1.6, and `seal` covers the list. A file whose seal does not
  check trusts no repository, keeps every other setting, and says once: "The repositories you trusted in axGIT's
  settings were changed outside axKit and are not used: git runs none of their own programs. To trust one again, open
  it and choose Trust this repository." A file of 5.9.0 or earlier holds `trustedRepositories`, bare paths: read while
  the file has never been sealed, each such trust counts for what the repository runs at its next opening and is
  bound to it then, and the key is no longer written. `GitSettings`, tested in `GitSealTests`.
- AG-8.2 The log is `%LOCALAPPDATA%\AxKit\logs\axGIT.log`: every git command by its subcommand ("git commit") with its
  exit code and duration, never its arguments (a commit's message, the paths; until 2026-10-23 the whole argument
  list was logged) nor its output beyond the first line of an error, which is written without the sign-in of any
  address in it (AG-5.4a). A git command that cannot end (a program it
  started keeps its output open) is cut off at the timeout like one that hangs, with what was read so far (AG-1.5).

## 5. Architecture

```
src/AxGit.Core/   class library (net10.0): GitRunner (git.exe as a process: arguments in, exit code and texts out,
                  async, with the repository and the environment), GitStatus (porcelain v2 to ChangedFile records and
                  the branch state), GitLog (the log lines to Commit records), GitBranches (the branch lines to Branch
                  records), GitSettings; references AxKit.Core
src/AxGit/        class library (net10.0-windows): AxGitApp (Run, Usage), GitForm (the window), AxGitKeys, HelpText
                  (F1 text, the embedded user guide); references AxGit.Core, AxKit.Core, AxKit.Forms
tests/AxGit.Tests/ GitStatusTests, GitLogTests, GitBranchesTests, GitRunnerTests (the argument list and the settings):
                  the parsers and the labels, from recorded git output, no git process
docs/axGIT/       this spec, user-guide.md (embedded), nvda-test-plan.md
```

`GitForm` derives from the bundle's window base, `AxKit.Forms/AxKitForm` (bundle AX-6.5), which holds the notice flow,
the fonts, the window's place, find and the update flow; the base was made from the editor shell when axGIT would
have been the third copy of them.

## 6. Design decisions

- **AG-D1 A window per repository, reached from where the user is.** One window holds one thing (bundle B-2), so a
  repository gets a window named after it, and the Window menu finds it again. The worry was a window detached from
  the file or folder under work; the answer is the way in and out: the "Show current … in ▸" submenus the other
  windows already keep for an axKit app beside File Explorer open axGIT on the repository of the folder or file the
  user is in, and axGIT's own submenu sends back. Inside axClaude or the editors it would have been a Git menu three
  times over with a second set of lists in windows built around one thing; and in axClaude much of it can be asked of
  Claude, so axGIT's worth is the quick, deterministic read of a repository without spending a turn.
- **AG-D2 git.exe, nothing reimplemented.** A library would be a dependency (B-10) and a second git; the program the
  user has is the one source of truth, its messages are the ones the user knows, and reading its porcelain output is
  a parser of a hundred lines, tested.
- **AG-D3 One list with views, the state in the words.** NVDA reads a list box best. The staged state is part of the
  item's text ("…, staged") rather than a check box, so that arrows explore without touching anything and Space is
  the one key that acts; the same list shows the log and the branches with Enter acting on the view's item.
  **Changed on 2026-11-22** (`docs/axKit/framework.md` FW-4.10; the maintainer had not used axGIT and kept nothing
  of its design: "we can potentially simplify it into one of the interface templates"): the three views are three
  lists one goes into (AG-2.2), and Space selects, as everywhere in axKit (bundle AX-8.16), so that staging is a
  command on the selected files (AG-3.2). The staged state stays in the row's words.
- **AG-D4 Streamlined scope.** The daily set only; everything else is a non-goal until it is asked for, since the
  whole of git is endless and a file-manager-sized menu is what the user said to avoid.
- **AG-D5 Announce on demand, focus to the details on Enter.** Nothing speaks on arrows. Enter on a file or a commit
  is the command to read it, so the focus goes to the details, the one place the focus moves by itself, like a notice.
- **AG-D6 Commands on a background task.** git can take seconds (a push) or minutes (a fetch over a slow line); the
  window stays alive, the status bar says what is running, and one command runs at a time so that the lists never
  show two states.

## 7. Open questions

- Whether Enter on a conflict should open the file in the editor rather than show the diff.
- A "next hunk" key in the details text (a `TextJump` pair for lines that start with `@@`).
- Whether the Log should take a file from the Changes view (the file's history).

## 8. Status

- First draft on 2026-10-18, released in 2.12.0 the same day: AG-1 to AG-8 built; heard under NVDA by the user:
  the start from axClaude and axDown, the three views, a diff, staging and unstaging, the notices, Ctrl+E into
  axDown, and the way back through the axKit menu; `docs/axGIT/nvda-test-plan.md` sections 3.5 to 4.5 (commit,
  discard, branches, fetch, pull, push) are still to run. The window moved onto the bundle's window base the same
  day (AX-6.5).
- 2026-10-22: released in 3.3.1 (3.3.0 tagged, not published). The whole NVDA test plan marked passed at the user's word, no step reported failing; the record by step is `docs/axKit/nvda-test-record.md`.
