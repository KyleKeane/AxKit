# Reports: what exists on GitHub and how it is set up

The reports of `docs/axkit/reports.md` (RP-6.2 to RP-6.4, RP-7) rest on a few things outside this repository. This page
records them and the steps to make them again, as `code-signing.md` does for the Windows signing. Written 2026-11-07.

## 1. What exists

| Thing | Where | Who can do what |
| --- | --- | --- |
| The reports repository `AxKit-reports` | https://github.com/KyleKeane/AxKit-reports, public, made 2026-11-07 | Issues are the reports; `blocklist.json` at its root is the blocklist (RP-6.4); `.github/workflows/close-blocked.yml` closes and locks a blocked report; the labels `feedback`, `bug` and `feature`; two issue forms for people with a GitHub account of their own; issues on, wiki, projects and discussions off |
| The reporter token | A fine-grained personal access token of the maintainer's own GitHub account: repository access `AxKit-reports` only, permission Issues: read and write, nothing else, expiry one year | Creates, edits, labels and closes issues of the reports repository, nothing else (RP-6.2). Issues it posts show the maintainer's name as the author; the reporter line in the body tells which install sent them |
| The secret `AXKIT_REPORTER_TOKEN` | A secret of the GitHub environment `release` of this repository (the one the Store's credentials are in) | Read by the package job of `release.yml` alone; `publish.ps1` writes it into the build through `tools/reporter-token.ps1` |
| The maintainer's token for axTask's GitHub actions (RP-7.2) | A second fine-grained token of the maintainer's account: `AxKit-reports`, permissions Issues: read and write and Contents: read and write (the blocklist) | Pasted once into axTask (List menu, GitHub account…), kept on the maintainer's computer protected as the MCP key is; never in the repository or on GitHub. Optional until the maintainer wants to relabel, close, comment or block from axTask |

Why one account: GitHub lets a fine-grained token reach only repositories its own account owns (or an organization the
account belongs to), so a separate reporter account could hold no fine-grained token for `KyleKeane/AxKit-reports`; it
would need a classic token, which reaches every repository the account can see, or an organization owning the
repository. The maintainer's own token with one repository and one permission is the narrowest token there is for
this (RP-D2). A reporter account can come later, with an organization, without a change to the code.

## 2. Setting it up, in order

1. **The repository** exists since 2026-11-07, made with `gh repo create KyleKeane/AxKit-reports --public`: `README.md`,
   `blocklist.json` (`{"reporters": [], "versions": []}`), the labels `feedback` and `feature` beside the default `bug`,
   the two issue forms from `tools/reports-repository/ISSUE_TEMPLATE/` and the workflow of step 2.
2. **The workflow.** `tools/reports-repository/close-blocked.yml` is the repository's `.github/workflows/close-blocked.yml`:
   on `issues: opened` it reads the issue's last line (`Sent from axKit <version> with F4; reporter <id>`), compares the
   id and the version with `blocklist.json`, and closes and locks a blocked one with a comment naming the page. It runs
   with the repository's own token (`issues: write`), no secret.
3. **The reporter token** (the maintainer, in the browser, about two minutes): github.com → the profile picture →
   Settings → Developer settings → Personal access tokens → Fine-grained tokens → Generate new token. Token name
   "axKit reporter"; expiration one year; Resource owner the maintainer's own account; Repository access "Only select
   repositories", `AxKit-reports`; Permissions → Repository permissions → Issues → Read and write (Metadata read comes
   with it); Generate token; copy it once.
4. **The secret.** `gh secret set AXKIT_REPORTER_TOKEN --env release --repo KyleKeane/AxKit < <file holding the token>`
   from a terminal, or Settings → Environments → `release` → Environment secrets on the website. Never a repository
   secret: the build job, which runs the tests and their packages, must not see it (S-30, `ReleaseWorkflowTests`).
5. **A local send first.** `tools\reporter-token.ps1 -Token (Get-Content <file>)` writes the token's file for a
   development build (ignored by git); rebuild and F4, a word, Enter: "Sent as issue N", and the issue in the
   repository. `tools\reporter-token.ps1 -Remove` and deleting the file afterwards leave nothing behind.
6. **A release.** `release.ps1 <version>` as always; the sign job writes the token into the build. The notes do not
   mention the token. Check with a fresh install the same way as step 5.
7. **The maintainer's second token** for axTask (section 1, last row), made the same way with Issues and Contents, and
   pasted into axTask once, when the GitHub actions are wanted.

## 3. When something goes wrong

- **Nonsense from one install or one version** (RP-6.4): add the reporter id or the version to `blocklist.json`, from
  axTask (Block this reporter, Block this version) or by hand on GitHub; the workflow closes what still arrives, and
  the senders stop within a day, when they read the list again.
- **The token leaked or abused.** Delete it on GitHub (Settings → Developer settings → Fine-grained tokens → the token);
  make a new one; replace the environment secret; release a new version. Every earlier version then gets GitHub's 401
  and says "This version can no longer send reports: update axKit from the Help menu, or copy the report" (RP-6.4).
  Note the day in `CHANGELOG.md`.
- **GitHub revokes the token itself.** The same as a leak: a new token, a new release. The apps lose nothing; unsent
  reports wait in Your reports.
- **Yearly** (`docs/axkit/longevity.md`): rotate the token before it expires (one year), check that the workflow still
  runs (open a test issue with a blocked version line and see it closed), and that `blocklist.json` still parses
  (`Blocklist.Parse` in `GitHubIssuesTests` is its shape).

## 4. Why these choices

RP-D2 and RP-D8 in `docs/axkit/reports.md`: no sign-in for the user, so a token in the program; a separate repository,
so that a leak reaches one repository's issues and nothing else; one permission on one repository, the narrowest a
token in a program can be; GitHub's terms ask that credentials stay confidential, and this is as close as a token in a
program comes to that.
