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
- AG-1.2 The repository is the one that holds the path, found by axKit's own code going up from the folder (or the
  file's folder) by git's own rule (AG-1.6a; `git rev-parse --show-toplevel` until the review's fourth pass). Without a path, the last repository opened, or none: the window opens with "No repository" and Open
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
  run with `-C <repository>`, `--git-dir=<the git folder the check read>` and `--work-tree=<repository>` (the
  review's fifth pass, P1: git works on the repository that was read and looks for none of its own),
  `-c core.quotepath=false`, `-c color.ui=never`, `-c core.fsmonitor=false`,
  `-c protocol.ext.allow=never` (security audit S-1, S-2), diffs and `show` with `--no-ext-diff --no-textconv`,
  `GIT_TERMINAL_PROMPT=0`, `GIT_CEILING_DIRECTORIES` the folder above the repository, none of git's variables that
  name a repository's folders (`GIT_DIR`, `GIT_WORK_TREE`, `GIT_COMMON_DIR`, `GIT_INDEX_FILE`,
  `GIT_OBJECT_DIRECTORY`, `GIT_ALTERNATE_OBJECT_DIRECTORIES`, `GIT_NAMESPACE`) from the environment axKit was started
  with, and UTF-8 output; one command at a time, on a background task, never on the UI thread
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
- AG-1.6 **A repository's own programs** (2026-10-24, security audit S-2; the line since the review's fourth pass of
  2026-11-28, T1). A repository that arrives with its `.git` folder (an archive, a USB stick, a share) can name
  programs git runs: a file monitor (`git status`), diff and filter drivers, hooks (commit, switch), an ssh command
  (fetch, push), a credential helper; and files git reads on another computer. **Before the user trusts a
  repository, axGIT runs no git command in it at all, and every repository is asked about once** (G1 of the
  review's sixth pass, 2026-11-28). Until then a repository whose check found nothing was trusted without asking,
  and every High of six rounds had that shape: the scan read the repository by a rule a little different from
  git's (a split index, version 4's byte prefixes, a submodule's `core.worktree`, a byte order mark before HEAD, an
  include's leading space, a `.git` file in an untracked folder), found nothing, and git then ran a program or read
  a share there. A re-reading of git's formats in C# is never complete, so the guarantee no longer depends on it,
  as an editor's workspace trust does not. Three rounds of review had each found a
  way in which git, run in such a repository with its programs set aside by name, still ran one or reached
  another computer (a repository's own `core.unsetenvvars` cleared every setting axGIT gave a submodule's run, and
  the submodule's file monitor ran at the status of opening, H1; a push into a folder read one repository and
  pushed into another, H2; a legacy remotes file, M1; an include or a value on a share, M2). Setting aside by name
  can never be made complete, so the line is where it can be held: what the check needs is read by axKit's own
  code (AG-1.6a), every setting that is not known to be harmless (AG-1.6c), every hook and every place the scan
  does not read is a risk (`GitTrust`), and what the scan finds is what the notice says; the notice comes first,
  for every repository the user has not trusted for what is there now:
  - its title is **This repository runs programs of its own** (a hook or a program setting among the risks),
    **This repository leads git elsewhere** (a place), **This repository has settings axGIT does not know**, or,
    where the check found nothing, **This repository is not trusted yet**;
  - its text: "axGIT has run nothing in <path>." (on a later check, "axGIT runs nothing more in this repository
    until you choose."), then, where nothing was found, "axGIT found nothing of concern in it. It reads the
    repository's files itself and cannot promise that it found everything." (`GitTrust.NothingFound`), or "Its settings or hooks name programs git would run: …", "The programs as its
    settings write them: core.sshcommand: ssh -v; …" (each setting that names a program with its whole value,
    every hidden character written out as in a command offered to run, P5 of the review's fifth pass), "It has
    settings axGIT does not know to be harmless: …", "Its files lead git to places axGIT does not read: …" where
    there are any, the sentence of T4 where a program lies in the work tree (AG-1.6b), what trust means, every time
    (below, G2), and "Trust it only if you know where it comes from. Keep it off closes the repository.";
  - **Keep it off** (Enter, Escape) closes the repository: as it opens, it is never opened ("Not opened"); open,
    it is closed ("Repository closed"): no status, no list, nothing runs;
  - **Trust this repository** checks the repository again ("Working: checking…", P3 of the fifth pass: trusted is
    what was seen) and, when it holds what the notice showed, keeps it in the settings (`trusted`, AG-8.1a; where
    the git folder's identity lasts, T1 below, and otherwise for the session alone), says
    "Trusted", opens it (or reads its lists again), and from then on git works in it as git does (G2). Alt+T
    answers so from anywhere in the notice, once its keys' hold is over. When it
    changed while the notice was open (a filter added meanwhile), nothing is kept and nothing runs: the notice
    comes again with what it holds now, starting "What axGIT finds in this repository changed while you were asked,
    so it is not trusted; here is what it holds now."
  A repository whose check found nothing is asked about too, once (G1; until the sixth pass it opened with no
  notice). A repository axGIT itself made would be trusted at its making; axGIT makes none (it has no init and no
  clone), so there is no such case. A trust given before the sixth pass stays valid where its mark still matches;
  a repository that opened without the notice then was never trusted, and is asked about once.
  - *What trust means* (G2 of the sixth pass; `GitTrust.TrustLimit`, said in the notice every time, and in these
    terms in the bundle's statements of rule 8, `PRIVACY.md`, `README.md`, `docs/axKit/README.md`, `SECURITY.md`
    and the Outside call comment of `GitRunner`): "Until you trust a repository, axGIT runs no git in it and
    reaches no server: it reads only the repository's own files, where they lie (a repository on a share is read
    on the share), and the files on this computer its settings name. Once you trust it, git works in it as git does: it runs the programs and hooks the
    repository's settings name, whatever they run in turn, and axGIT sets aside only those whose path is in the
    work tree, on the commands that change the repository; and git reads the files and places those settings and
    its index name, which can be on another computer." So after trust a status reads an untracked folder's `.git`
    file and the place it names (H4, through a share), and every file the settings and the index name, wherever
    they lie; the scan names what it reads, and a miss of the scan is no longer a door to running before trust.
    Until the seventh pass (T4, M1 and M2) the first sentence said "and reaches nothing", which was not exact: a
    repository on a share is read there, and a file on this computer a setting names (an include, a hooks folder,
    a program) is read for the mark.
  - *The limits that need a program writing at the same moment* (L1 of the sixth pass), said and not closed in
    code: the check and git's start are two steps, so a file changed between them, or a link put on a path after
    the check looked at it and before it is opened by its path, is seen at the next check only; and the parse kept
    for a settings file (AG-1.6b) is found by its content's hash, read before and after git's parse, so content
    swapped and swapped back between those two reads is not seen.
  The trust is for what axGIT found when it was
  given (`repairs.md` R-53, and T4 of the fourth pass): it is kept with the mark of the scan (`TrustScan.Mark`,
  `GitTrust.Mark`), the SHA-256 of the risks, the value of every setting that is not harmless, the content of
  every hook, of every file such a setting names (a program, an included settings file) and the listing of each
  hooks folder. A trusted repository in which axGIT now finds anything else (a hook more or fewer, a hook's or a
  program's file changed, an included file changed, another setting) is not trusted, and the notice comes back,
  starting "What axGIT finds in this repository differs from what you trusted, so it is not trusted now." (the
  sentence does not say the repository changed: after an update of axGIT the check may be what did; a trust kept
  before the fourth pass marked the names alone and is asked about once more). Since the review's seventh pass
  (2026-11-28) **a trust covers the repository the user was shown, as it was shown**:
  - *the git folder itself* (T1): the trust keeps the git folder's identity (`FolderIdentity`: the file system's
    name from `GetVolumeInformationByHandleW`, the volume's serial number and the 128-bit file ID from
    `GetFileInformationByHandleEx` with `FileIdInfo`, through a handle opened for its attributes, with backup
    semantics and not through a final link; `RepositoryFiles.Identity`), and holds only when the path, the mark and
    the identity all match, in the settings and within a session. Another git folder at the same path (a
    repository moved there, a clone made again) is asked about as a new repository, starting "The repository in this
    folder is not the one you trusted here, so nothing is trusted for it yet." (the reviewer's case: two clean
    repositories at one path have one mark). Where the volume gives no lasting identity, no trust is kept between
    openings: the rule is by the file system's name, NTFS and ReFS last and every other does not (FAT, FAT32, exFAT,
    UDF, a share that names none, or no name read); there the notice says "Its drive (FAT32) gives a folder no lasting
    identity, so axGIT keeps no trust of it: a trust holds while this repository stays open, and you are asked again
    when you next open it." A trust kept before the seventh pass has no identity and is asked about once more.
  - *every source of a risk* (T2): a risk that names a file marks its content (`file <path> <hash>`: the alternates
    and http-alternates, a legacy remotes or branches file, a `.git` file, a `commondir` file), a link marks its
    target as written in it, read without following it (`link <path> <target>`), and a risk in a value marks the
    value, of a harmless name too (a fetch line outside refs/remotes, a value on another computer, a submodule's
    `core.worktree` elsewhere, a `.gitmodules` path that is not read). Until then an alternates file, a legacy remotes
    file or a link could point elsewhere with the trust kept. Three kinds say that axGIT did not read something and
    are trusted as such, not by a content (`GitTrust.NotMarked`): a repository inside that could not be read, an
    index axGIT does not read (it changes at every command git runs), folders deeper than it looks. The kinds are one
    list, `GitTrust.RiskKind`, told from the risk's words by `GitTrust.KindOf`; a risk of no kind is not opened, and
    the class test (`GitTrustShownTests`) changes the source of every kind and sees the mark change.
  - *values as written* (T3): the mark's lines are kept as they are, sorted with ordinal comparison; a setting's name
    is in git's form (`GitTrust.NameOfSetting`: section and variable in lower case, the middle part as written).
    Until then every line was put in lower case, so that "echo ABC" and "echo abc" were one mark. Every mark of an
    earlier version differs, so each repository is asked about once more after the update.
  `GitTrust.Overrides`, the setting
  aside by name of the passes before, stays as a second line, used where a trusted repository's program lies in
  its work tree (AG-1.6b).
- AG-1.6a **What the read covers, and when it runs** (the reviews of 2026-11-27 and 2026-11-28; `GitTrustScan`).
  - *The root*: the folder of the repository that holds the folder or file asked for is found by axKit's own code
    going up (`GitTrustScan.RootOf`): the first folder with a `.git` entry. A `.git` folder counts only when it is
    a repository by git's own rule (`GitTrustScan.IsGitDirectory`, setup.c): its HEAD is a reference (`ref:
    refs/…`) or an object id, and its common folder, the one its `commondir` names when it has one, which must be
    a folder that exists, holds `objects` and `refs`; one that is not ends the look, and the folder is "Not a git
    repository" (P1 of the fifth pass, tried with git 2.53: with a HEAD that was no reference, or a commondir that
    named nothing, axGIT read the inner folder and git, run there, went up to the repository above and ran its
    hooks on Stage all). HEAD, `commondir` and a `.git` file are read as bytes by git's rule (G3 of the sixth
    pass, H6: a HEAD with a byte order mark was read as text, the mark taken off, and git took no such folder for a
    repository): after `ref:` only git's own whitespace (a space, a tab, a line end) before `refs/`, no byte order
    mark; a `.git` file starts `gitdir: ` with its one space; a path in a `.git` or `commondir` file is the whole
    rest less its line ends, and one with whitespace at its start or end, a control character (a second line) or a
    byte order mark is not read another way: it is not opened, or, inside, "a .git file axGIT does not read". A `.git` file or link is the entry, which the read below takes or refuses. A
    repository's own folder and a bare repository have no work tree and are not opened. Until the fourth pass `git
    rev-parse --show-toplevel` ran there first.
  - *git works on the repository that was read* (P1): every run names the git folder the read found and the root
    as the work tree (AG-1.5), so that git looks for no repository of its own, and the read of the repository
    opened must find that folder: a `.git` that is a link, a `.git` file or `commondir` that names another computer
    or a place axGIT does not read, or a folder that is no repository by git's rule, is not opened ("Repository not
    opened", with the reason, "Its .git is a link, which axGIT does not follow, so axGIT does not open it."). A
    named git folder passes over git's own check of who owns a repository (`safe.directory`), so the first time a
    repository is trusted in a window git is asked once whether it opens it itself, `git rev-parse
    --absolute-git-dir` finding it from the root with nothing above it; when git refuses (a repository another
    account owns), it is not opened, with git's words, and the folder git prints must be the one the read found,
    or it is not opened: "git opens another git folder, <printed>, than the one axGIT read, <read>." (H6 of the
    sixth pass: git, rejecting a `.git` the scan had read, took the folder above for a bare repository and read its
    settings). Since the seventh pass (L1) the two are compared by their identities (AG-1.6, T1) where both last,
    and as full paths otherwise, so that a root reached through a junction or a subst drive, which git prints by its
    real path, opens after Trust.
  - *A path of the repository's is read only in a positive local form* (P2 of the fifth pass; `RepositoryFiles`,
    the one way every read below goes): its full path is a plain path on a drive of this computer (not a network
    drive) with no link on its way from the drive's root, or it lies under the repository's own folder with no link
    on its way from there. Anything else is a place axGIT does not read, and is never touched, not even its
    attributes: a share in any spelling (`\\host`, `//host`, `\\?\UNC\`, `\??\UNC\`), a device path (`\\?\`,
    `\\.\`, `\??\`), an alternate stream, a name Windows takes for a device (`NUL`, `COM1`), a junction or a
    symbolic link on the way. Such a path in a setting is a risk, "include.path names a place axGIT does not read"
    (tried with git 2.53 through this computer's own share: until then `\??\UNC\localhost\C$`, `\\.\C:`, `\??\C:`,
    a stream and a junction were each read). An untracked file shown with Enter (AG-3.3) is read the same way.
  - *git as a parser of one file*: each settings file is read with `git config --file <path> --no-includes --list
    -z`, with `GIT_DIR` naming a folder that does not exist, so that git looks for no repository at all and runs
    nothing of one (tried with git 2.53, also run from inside a repository, on a file that set every name of `git
    help --config` to a program, `core.unsetenvvars` and `core.fsmonitor` among them: every name was listed and
    nothing ran). This is the only git that runs for the check.
  - *What is read*, of the repository opened and of every repository inside it: its `.git` entry, a folder or a
    file with the folder it names (a link there is a risk, "a link at .git", and is not followed; a folder outside
    the work tree is a risk, "a .git file or commondir file that names a folder outside the work tree", as for a
    linked work tree or a submodule opened by itself; a folder on another computer is named and not read); the
    `commondir` file the same way; every entry of the `.git` folder for a link (each "a link at <place>", never
    followed); `objects/info/alternates` ("object stores of other repositories (alternates)"); the legacy
    `remotes` and `branches` files git still obeys ("a legacy remotes file origin", M1); the settings files of
    the repository's own scopes, `config` and `config.worktree`, through the parser, every setting's name and
    value; the files the includes name, read the same way for the mark and for what they set, a relative one from
    the folder of the file that names it (L5 of the fifth pass: `config.worktree`'s from the work tree's git
    folder) (an include is a risk by its name, what it brings in counts at each command, and every include counts
    whatever its condition); the hooks folders, the default one or the one `core.hooksPath` names, relative to the
    work tree, and each one an included file names (L5: its hooks are risks too, and a push looks for Git LFS's
    pre-push hook in each); the files the other settings name, for the mark and for T4; and the index. A path in
    a setting is read as git reads it, untrimmed, or named (M1 of the sixth pass: an include with a leading space
    named " extra.cfg" for git and "extra.cfg" for the scan): one with whitespace at its start or end, a control
    character, a drive without a separator (`C:x`, relative to that drive's own current folder), a `~` other than
    `~/`, git's own `%(prefix)/`, or, where git opens it itself (an include, a hooks folder, a submodule's work
    tree), a path rooted without a drive (`/x`) is "include.path names a place axGIT does not read", and nothing is
    read for it. In a repository inside, `core.worktree` is a risk, "submodule libs/zip: core.worktree", unless it
    names the folder of the gitlink the submodule was reached from (H3 of the sixth pass: git, entering the
    submodule, worked on the other folder).
  - *The repositories inside are those git enters* (P4 of the fifth pass; tried with git 2.53 on every command
    axGIT has, with a submodule, a repository Stage all added, an untracked repository, one in an ignored folder
    and a gitlink left in HEAD alone): git runs itself inside another repository only where the index holds it as
    a gitlink (mode 160000), on a status, and obeys that repository's own settings there; inside an untracked one,
    and one in an ignored folder, it ran nothing. Its status does read an untracked folder's `.git` file and the
    place it names, to tell whether the folder holds a repository (H4 of the sixth pass, through a share): the scan
    does not read untracked folders, and that read is what trust allows (G2). So the repositories inside are the gitlinks of the index, read in
    C# from git's documented format (`GitIndex`: versions 2, 3 and 4, a split index with its shared index; any
    other form is a risk, "an index axGIT does not read"), strictly since the sixth pass (G3): an entry with an
    empty name (a split index writes so an entry that replaces one of its shared index, H1, and which path it is
    lies in a bitmap not read), a name whose length in the flags differs from where it ends (git takes the
    flags'), a path that is no UTF-8, and any path git's own check (verify_path with core.protectNTFS) would never
    have let in, since git looks at every path of the index in the work tree (H5: a path on a share was read by a
    status): one with a backslash, a colon (a drive, a stream), an empty part (a slash at its start or end, two
    together), a part ending in a dot or a space (`.` and `..` among them), or a part `.git` or `git~1`; each
    path of version 4 is put together as bytes and decoded once (H2: the bytes dropped were taken as characters,
    and a gitlink after a path with a letter outside ASCII was read as another path). The paths `.gitmodules` names (through the parser) are held to the same rule, and one that fails it is named, "submodule.x.path names a place axGIT does not read";
    each is followed into its own index twelve levels down (`GitTrust.Deep`), and then the repositories git keeps under
    `.git/modules` that no gitlink led to, with or without a work tree, twelve levels of folders down (after the
    others, so that a submodule is read with the folder it was reached from, H3). Once Stage all has made an
    untracked repository a gitlink, the check after it finds it, and nothing more runs until it is trusted. Until
    the fifth pass the whole work tree was walked for `.git` entries, ignored folders too; the folders of the work
    tree are no longer walked. The `.git` folder is still walked for links, 64 levels down
    (`GitTrust.TreeDeep`); a folder deeper that holds folders is "folders deeper than axGIT looks". What one would
    run is named with it, "submodule libs/zip: core.sshcommand", and is part of the mark. A repository inside whose
    settings the parser cannot read is "submodule <path>: settings that could not be read".
  - *Places on another computer* (D9 of the third pass, M2 of the fourth). git reads through a share at its first
    command (tried with git 2.53 through `//localhost/C$`: a `.git` file, an include, an alternate object store,
    `core.excludesFile`, `core.worktree`, `core.attributesFile`, `mailmap.file`, `commit.template`). The guard of the
    third pass read the syntax itself and missed an include on one line with its section, a value split over two
    lines, a `.git` link to a share and every other setting. Now git's parser reads the values, and any value that
    names a place on another computer is a risk, "core.excludesfile names another computer, server": a share
    (`\\host\share`, `//host/share`, `\\?\UNC\host`), a device path (`\\?\` other than a drive's, `\\.\`, "names a
    device path"), a `file://` address with a host, or a path on a drive Windows calls a network drive; a share of
    the computer the repository itself lies on is its own. Nothing of another computer is read by the check, and of
    the computer a repository on a share lies on, only what lies under its own folder (P2).
  - *When, and where*: the read is one piece of work, before the repository becomes the window's ("Working:
    opening…"), and runs off the window's thread from its first step (bundle rule 3; `GitTrustScan.ReadAsync`,
    four repositories inside read at a time). A read that fails (the parser cannot read the repository's own
    settings file, its `.git` file names nothing) opens nothing: the notice **Repository not opened** says "What
    <path> would run could not be read, so axGIT runs nothing in it." with the reason, and a repository already
    open stays. A repository is never taken as running nothing because it could not be read, and a check that
    fails in any way trusts nothing (L2 of the fifth pass: until then the trust of the check before stayed).
- AG-1.6b **The check again** (the review's second pass of 2026-11-27, and T4 of the fourth). The ground moves under
  a check: a settings file included for one branch alone counts after a switch; a pull or a discard writes a file
  the settings include; Stage all makes a folder with a repository of its own an entry of the index; a switch
  brings another hook. So the whole read of AG-1.6a is made again before and after every command that changes the
  repository (stage, unstage, discard, commit, switch, a new branch, fetch, pull, push; axGIT has no stash): before
  it, so that what runs is what was seen (P3 of the review's fifth pass: a setting that changed between two commands
  ran on the second), and the command runs only when the check holds, else it is refused, nothing runs, and the
  notice comes; after it, whether it succeeded or failed (D8, `GitSession.ChangeAsync`, in a `finally`). Also before
  every fetch, pull and push, ahead of the questions of AG-5.4a (T4), after the user trusts a repository (P3,
  AG-1.6), and at F5 ("Working: checking…"). When it is no longer what the user trusted, the notice of AG-1.6 comes
  again, and nothing runs until it is answered; Keep it off closes the repository. After a command that failed, its
  sentence ("Switch failed. Git said: …") stands first in that notice; a command the check before it refused has no
  such sentence, since nothing ran. When the read fails then, the repository is closed: **Repository closed**, "What
  <path> would run could no longer be read, so axGIT runs nothing more in it and has closed it." A check costs a
  read of the repository's files and a run of git's parser for each settings file whose content changed since the
  check before (`GitTrustScan`; the same content gives the same settings, so its parse is kept); measured on
  2026-11-28 on the development computer (a 7 GB virtual machine), each check: 40 ms for a repository of 3,000
  files, 20 ms for an index of 200,000 entries, 370 ms with 50 submodules (1.9 seconds the first time, when every
  submodule's settings are parsed). So a changing command takes two checks more than it would alone.
  - *The limit of trust* (P5 of the fifth pass): a program a setting names is a command line, which may run
    anything, files of the work tree among them, and no reading of its text can tell that it stays outside. axGIT
    does not guess: the notice names every such setting with its whole value and says what trust means (AG-1.6,
    G2 of the sixth pass, which took the place of P5's sentence).
  - *A program in the work tree* (T4; M3, tried with git 2.53: a switch brought a post-checkout hook into a hooks
    folder of the work tree and ran it, before any check could see it). Where the hooks folder, or the file of a
    setting that runs a program, lies in the work tree (`TrustScan.InTree`; not in a `.git` folder, which no
    checkout writes), a trusted repository's changing commands run without them: the hooks folder is an empty one
    of axKit's own, and each such setting is set to nothing (`GitTrust.Overrides`, through the environment); a hook
    of the settings, `hook.<name>.command` (git 2.54 and later), is set aside by the two switches git documents for
    it in 2.54.0 and 2.56.1, an empty `hook.<name>.event`, which empties its list of events, and
    `hook.<name>.enabled=false`, since an empty command is one git cannot start and the command stopped (L7 of the
    fifth pass; git 2.53, on the development computer, knows no such hooks, so this was read in git's documentation
    and is tested on GitHub's build alone). The sixth pass (L2) read git-hook's page again (git-scm.com, 2.55.0,
    unchanged to 2.56.1): it also documents `hook.<event>.enabled=false`, "no hooks fire for that event", the
    broader switch, which turns off every hook of an event, the hooks folder's too, and needs each event named; the
    two per-hook switches are the plainer ones and stay, still not runnable here. The
    notice of AG-1.6 says so once, when the repository is asked about: "Its hooks folder or programs lie in its
    work tree, where a switch or a pull can change them while git runs (…): even trusted, axGIT stages, commits,
    switches, fetches, pulls and pushes without them, and you run those that need them in a terminal." Its reads
    (status, log, diff) run them as they are, and the check after every change has seen them as they are.
- AG-1.6c **Only what is known to be harmless is passed over** (the review's second pass of 2026-11-27;
  `GitTrust.Inert`). Until then the rule was the list of the names that run a program, and such a list is never
  complete: git and the programs it starts gain names without notice (Git LFS's `lfs.extension.<name>.clean` and
  `gc.recentObjectsHook` were not on it, and ran). The rule is turned around.
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
    submodule, where it names the folder of the gitlink the submodule was reached from (any other is a risk, H3 of
    the sixth pass, AG-1.6a). `include.path` and `includeIf` are never in it. Every other name is a risk, named in the notice
    as a setting axGIT does not know to be harmless (or as a program, by the names of `GitTrust.ProgramNamesTable`),
    and part of the mark. An ordinary repository's notice names nothing (since the sixth pass it is asked about
    once all the same, G1); one with a setting of the user's own that is not in the table names it.
  - *Where the value decides the harm, the value is read* (`GitTrust.ValueRisks`): a value on another computer
    (AG-1.6a), whatever its name; and `remote.<name>.fetch` outside `refs/remotes/`, "remote.origin.fetch writes
    outside refs/remotes" (axGIT's own fetch names its line since the fourth pass, AG-5.4, so such a line no
    longer moves a branch there; it is still asked about, since a fetch in a console obeys it). Every other name
    of the table was gone through with the question what its value can do: a number or a switch; a text git does
    not read; an address, read before anything reaches it (AG-5.4a); a remote's name, which axGIT reads and puts
    on the command line; a branch's name, which a push and a pull name themselves (AG-5.4); `pull.rebase` and
    `branch.<name>.rebase`, which even set to `interactive` open nothing under axGIT's `--ff-only`.
  - *For every run in every repository* (`GitRunner.Command`, `GitRunner.FixedEnvironment`): the file monitor and
    the `ext::` transport off; `maintenance.auto=false` with `gc.auto=0` (git's automatic maintenance runs
    `gc.recentObjectsHook`, a setting with several values that an empty one does not set aside; axGIT never tidies
    a repository); since the fourth pass `core.unsetenvvars=PERL5LIB`, Git for Windows' own default, so that a
    repository's own `core.unsetenvvars` cannot clear what axGIT gives a submodule's run through the environment
    (H1), and `http.followRedirects=false`, so that a fetch, pull or push reaches no server it was redirected to
    (L1), and on a fetch, pull or push `http.<address>.followRedirects=false` for each http and https address it
    reaches, its user kept and its password not (L1 of the fifth pass: git takes the setting of the most particular
    address, so a repository's setting for its own address won over the general one; given through the
    environment as the command's own setting, `GitReach.NoRedirects`); the folders git works on named and git's
    variables for other folders left out (AG-1.5, P1); `GIT_NO_LAZY_FETCH=1` (a partial clone fetches no missing object by itself, git before 2.44 does not know
    the variable); and `GIT_LFS_SKIP_SMUDGE=1` with `GIT_LFS_SKIP_PUSH=1`: Git LFS fetches and sends no file
    through axGIT. **A Git LFS file stays a pointer file** after a switch or a pull in axGIT; `git lfs pull` in a
    terminal fetches the files.
  - *The tests that close the class*: in `GitTrustScanTests`, every name of `git help --config` (a fixture,
    `tests/fixtures/git/help-config.txt`, with the version of git it was made with on its first line) set to a
    program that leaves a mark, in the repository opened, its work tree's file and a submodule's, with
    `core.unsetenvvars`, `core.fsmonitor`, `core.hooksPath` and an include among them: the check runs git only as
    the parser, each parser run succeeds, no mark comes, every name not known to be harmless is a risk, and every
    command axGIT has is refused before trust. With the installed git of the fixture's version, a name the
    installed git knows that the fixture lacks fails the test; with another version the test runs the names both
    know, says so loudly, and fails afterwards off CI alone, with the command that renews the fixture (GitHub's
    build runs a newer git). In `GitSessionTests`, the second line: every name of `GitTrust.ProgramNamesTable` set
    in a clone and its submodule to a program in the clone's work tree, the clone's own `core.unsetenvvars`
    clearing the variables axGIT's settings travel in, and every command run through a trusted `GitSession` as the
    window runs it (L3 of the fifth pass: the reads as they are, the changing commands with what the check found
    in the work tree set aside): no changing command leaves a mark, and every one but a signed commit succeeds, a
    hook of the settings among them (L7, on git 2.54 and later). In `GitBoundsTests`, the fifth pass: P1's climb,
    P2's forms, P3's race, L1 through `git config --get-urlmatch`, L2 and L5, and each of git's variables for other
    folders on its own (L3 of the sixth pass). In `GitAskedOnceTests`, the sixth pass: a clean repository and each
    of the review's six (H1 to H6) are not trusted and refuse every command axGIT has until the user's trust, no
    mark comes, and git's own trace (`GIT_TRACE2_EVENT`) shows no git started but the parser of one settings file
    with no repository; HEAD, `commondir` and `.git` files in each form; the folder git finds compared at the first
    trust; the path values of M1. In `GitIndexTests`, indexes written byte by byte for G3's rules. In
    `RepositoryFilesTests`, every way to the disk (a handle, `RandomAccess`, `Path.Exists` among them) outside
    `RepositoryFiles` fails, in the scan, the session, the reach, the remote and the window (a line of the window's
    that touches a path not of the repository's says so at its end). Each test also runs the script once the way it
    did run.

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
  items that run git are disabled; nothing else is blocked. Each starts with the check of AG-1.6b (T4), so that
  nothing is reached in a repository that is not trusted now (nothing runs in one at all, AG-1.6).
  - *A fetch and a pull name what they bring* (the review's fourth pass, T3): `git fetch --prune
    --no-recurse-submodules --no-all --upload-pack=git-upload-pack --refmap= -- <remote>
    +refs/heads/*:refs/remotes/<remote>/*`, and `git pull --ff-only` with the same options and
    `+<branch followed>:refs/remotes/<remote>/<name>`, the branch the current one follows (`branch.<name>.merge`,
    `GitTexts.MergeOf`). No line of the repository's settings or of a legacy `.git/remotes` file applies, not even to
    the remote branches git would update by the way (the empty `--refmap`): a legacy file with "Pull:
    +refs/heads/*:refs/heads/*" made the fetch with `--prune` delete every local branch the remote did not have (M1,
    tried with git 2.53). A branch that follows none is not pulled: **Pull not started**, "This branch follows no
    branch of a remote, so there is nothing named to pull. Nothing was fetched." From the repository itself (the
    remote `.`) a fetch reads its HEAD into FETCH_HEAD alone, and a pull names the branch followed. No fetch, pull
    or push follows an http redirect (`http.followRedirects=false`, L1, and the same for each address it reaches,
    which a repository's setting for that address cannot turn on, L1 of the fifth pass, AG-1.6c).
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
    `--` so that a name like an option is a name, with the lines of AG-5.4 after it: `git fetch … -- <remote>
    +refs/heads/*:refs/remotes/<remote>/*`, `git pull --ff-only … -- <remote> +<branch>:…`, `git push
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
- AG-5.4c **axGIT pushes only to a server** (the review's fourth pass of 2026-11-28, T2; `GitReach.PushRefused`).
  git reaches an address on this computer, a share, or the remote `.` with its own transport for a folder, which
  runs the other repository's receive hooks on a push and clears what axGIT gives through the environment; and
  which repository it enters is git's to decide: with the address `./vendor/mirror`, a plain folder of that name
  beside a bare repository `vendor/mirror.git`, the reads the third pass made before the push (`git -C` there) read
  the repository above, which held nothing, while the push entered `vendor/mirror.git` and ran its receive hook
  (H2, tried with git 2.53); a share can change between such a read and the push (M4). So a push whose remote is
  a folder, a share or the repository itself does not start, before any consent is asked: **Push not started**,
  "This remote is a folder; push to it in a terminal. Nothing was sent." A fetch and a pull from a folder run
  nothing of the other repository's (tried: its hooks, `uploadpack.packObjectsHook`, `core.fsmonitor`,
  `core.alternateRefsCommand`, with the upload program named on the command line) and stay allowed; a share's
  computer is asked about first, as a server is. Until the fourth pass (D3) the repository of a folder was read
  before a push and the push refused when anything was found.

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
  "risks", "identity"}`, the repository's root, the mark of AG-1.6 and, since the seventh pass (T1), its git
  folder's identity ("NTFS 9CC676A6C6768076 0000…"), and `seal` covers the list; a trust with an identity is sealed
  as its own form, so that a file of an earlier version keeps its seal (its trusts, with no identity, are asked
  about once more), and an identity changed or taken out by hand is refused like a mark. A file whose seal does not
  check trusts no repository, keeps every other setting, and says once: "The repositories you trusted in axGIT's
  settings were changed outside axKit and are not used: git runs none of their own programs. To trust one again, open
  it and choose Trust this repository." A file of 5.9.0 or earlier holds `trustedRepositories`, bare paths: read while
  the file has never been sealed, each becomes a trust with an empty mark, and the key is no longer written. Since
  the review's fifth pass (2026-11-28, L5) an empty mark trusts nothing: the repository is asked about once more, as
  one that was trusted ("What axGIT finds in this repository differs from what you trusted"), and the trust given
  then is kept with its mark; until then such a trust counted for whatever the repository ran at its next opening.
  `GitSettings`, tested in `GitSealTests`.
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
