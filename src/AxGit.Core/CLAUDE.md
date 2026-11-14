# AxGit.Core, the pure code

Everything of axGIT that needs no window: git as a process and the parsers of its output. Unit tested in `tests/AxGit.Tests` from recorded git output, with no git process but a fake git.cmd the runner tests write. The spec is `docs/axGIT/SPEC.md` ("SPEC.md" below); the window is `src/AxGit`, with its own `CLAUDE.md`. The bundle rules are in the root `CLAUDE.md`.

## Commands

```
dotnet test tests/AxGit.Tests/AxGit.Tests.csproj
git -C <repo> status --porcelain=v2 --branch --untracked-files=all   # what GitStatus.Parse reads
git -C <repo> log -n200 --date=short --format=%h%x09%ad%x09%an%x09%s   # what GitLog.Parse reads
git -C <repo> branch --list --format=%(refname:short)%09%(HEAD)%09%(upstream:short)%09%(upstream:track)
```

## Where things are

This folder is the one place for everything git is told and everything read from it (root `CLAUDE.md` rule 12; `docs/axKit/access-layers.md`): the window writes no option of git's and matches no text of git's, and `GitTextsTests` fails when it does or when a characteristic word leaves its file here.

- `GitTexts` (2026-11-14): git's words that belong to no record of a view: the argument lists of the commands on the whole repository (`StageAllArguments`, `UnstageAllArguments`, `CommitArguments`, `FetchArguments`, `PullArguments`, `PushArguments`, which ties a branch without an upstream to `DefaultRemote`), and the two texts of git's output an announcement is chosen by (`SaysUpToDate` after a pull, `CommittedHash` from the summary line of a commit). A new command of the window's gets its arguments here or on the record it acts on, never in the window.
- `GitRunner`: `RunAsync` (one process per command, run through `AxKit.Core/ProcessRun` since 2026-10-25, `-C <repo> -c core.quotepath=false -c color.ui=never` in front of the arguments, `GIT_TERMINAL_PROMPT=0`, both streams read in UTF-8 on tasks, a timeout that kills the process; `GitNotFoundException` when git is not on the PATH; `Program`, the executable, git unless the tests point it at a fake), `RootAsync` (the repository that holds a folder or file), `Command` (the argument list, pure, tested). `GitResult` carries the exit code and both texts, `FirstLine` for an announcement and `AllText` for a notice.
- `GitStatus` and `ChangedFile`, `RepositoryStatus` (SPEC.md AG-3.1): porcelain v2 to records; the item's `Label` ("Modified src/Foo.cs, unstaged"), the list's name, the status bar text, and the argument lists for Space (`StageArguments`), Enter (`DiffArguments`) and Delete (`DiscardArguments`), so that the window knows no git syntax. `Unquote` undoes git's C-style quoting of odd paths.
- `GitLog` and `Commit` (AG-4): the tab-separated log to records, `Label`, `ShowArguments`.
- `GitBranches` and `Branch` (AG-5): the branch list to records with the upstream state (a detached HEAD, which git lists in parentheses, left out), `Label`, `SwitchArguments`, `CreateArguments`.
- `GitTrust` (AG-1.6; security audit S-2): a repository's own programs, the file monitor, diff and filter programs, hooks and the ssh command its `.git/config` names, read without running anything and each set to nothing on every git command until the user trusts the repository; `HooksFolder` reads the hooks folder from git's one line.
- `GitSettings` (AG-8.1): the settings file, as `EditorSettings` is for the editors, with the recent repositories.
- `GitRemote` (bundle AX-11.5, 2026-10-29): the remote a branch's upstream names (`NameOf`), `git remote get-url`'s arguments, and `Host`, the host of a remote's address for the server's consent (https, ssh with a user and a port, the scp form, an IPv6 literal in brackets kept whole; null for a path or a share, which reaches no one else's service). Tested in `GitRemoteTests`.

## Gotchas already learned

- `git status --porcelain=v2` without `-z` quotes a path only when it holds control characters, quotes or a backslash once `core.quotepath` is off; `Unquote` handles the C escapes and octal bytes. Renames come as one line with the new path, a tab, the old path.
- The log and branch formats use `%x09` and `%09` for tabs, so a subject with spaces or colons parses cleanly; a subject never holds a tab.
- `LC_ALL=C.UTF-8` keeps git's messages in English (the announcements quote them) and its output in UTF-8; `GIT_PAGER=cat` is belt and braces, git does not page without a terminal anyway.
