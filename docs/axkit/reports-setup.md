# Reports: what exists on GitHub and how it is set up

The reports of `docs/axkit/reports.md` (RP-6.2 to RP-6.4, RP-7) rest on a few things outside this repository. This page
records them and the steps to make them again, as `code-signing.md` does for the Windows signing. Written 2026-11-07.

## 1. What exists

| Thing | Where | Who can do what |
| --- | --- | --- |
| The reports repository `AxKit-reports` | https://github.com/KyleKeane/AxKit-reports, public | Issues are the reports; `blocklist.json` at its root is the blocklist (RP-6.4); `.github/workflows/close-blocked.yml` closes and locks a blocked report; the labels `feedback`, `bug` and `feature`; two issue forms for people with a GitHub account of their own |
| The reporter account `axkit-reporter` | A machine user of the maintainer's on GitHub (GitHub allows one per person) | Nothing but the token below; no access to the code repository |
| The reporter token | A fine-grained personal access token of `axkit-reporter`: resource owner `KyleKeane`, repository access `AxKit-reports` only, permission Issues: read and write, no other permission, expiry one year | Creates, edits, labels and closes issues of the reports repository, nothing else (RP-6.2) |
| The secret `AXKIT_REPORTER_TOKEN` | A secret of the GitHub environment `release` of this repository (the one the Azure ids are in) | Read by the sign job of `release.yml` alone; `publish.ps1` writes it into the build through `tools/reporter-token.ps1` |
| The maintainer's own token, for axTask's GitHub lists (RP-7.2) | A fine-grained token of the maintainer's account: `AxKit-reports`, permissions Issues: read and write and Contents: read and write | Pasted once into axTask (List menu, GitHub account…), kept on the maintainer's computer protected as the MCP key is; never in the repository or on GitHub |

## 2. Setting it up, in order

1. **The repository.** `gh repo create KyleKeane/AxKit-reports --public --description "Reports to axKit: bugs, ideas and feedback, sent from the apps (F4) or filed here"`, with `README.md` saying what the repository is and pointing to `docs/axkit/reports.md`, `blocklist.json` as `{"reporters": [], "versions": []}`, the labels made with `gh label create feedback`, `bug` (exists by default) and `feature`, and the two issue forms copied from `tools/reports-repository/ISSUE_TEMPLATE/`. Issues on, everything else off (no wiki, no projects, no discussions).
2. **The workflow.** `tools/reports-repository/close-blocked.yml` goes to the repository's `.github/workflows/close-blocked.yml`: on `issues: opened` it reads the issue's last line (`Sent from axKit <version> with F4; reporter <id>`), compares the id and the version with `blocklist.json`, and closes and locks a blocked one with a comment naming the page. It runs with the repository's own token (`issues: write`), no secret.
3. **The machine user.** On GitHub, signed out, create the account `axkit-reporter` with an address of the maintainer's that is not the main account's (an alias works); confirm it; nothing else, no profile. Sign in as it and, under Settings → Developer settings → Fine-grained tokens, make the token of section 1 (resource owner is the maintainer's account once `axkit-reporter` is invited as a collaborator of `AxKit-reports` with the Triage role; accept the invitation first). Copy the token once.
4. **The secret.** In this repository: Settings → Environments → `release` → Environment secrets → `AXKIT_REPORTER_TOKEN`, the token pasted. Never a repository secret: the build job, which runs the tests and their packages, must not see it (S-30, `ReleaseWorkflowTests`).
5. **A release.** `release.ps1 <version>` as always; the sign job writes the token into the build. The notes do not mention the token. Check with a fresh install: F4, a word, Enter, "Sent as issue N", and the issue in the repository posted by `axkit-reporter`.
6. **The maintainer's own token** for axTask (section 1, last row), made under the maintainer's account and pasted into axTask once.

## 3. When something goes wrong

- **Nonsense from one install or one version** (RP-6.4): add the reporter id or the version to `blocklist.json`, from axTask (Block this reporter, Block this version) or by hand on GitHub; the workflow closes what still arrives, and the senders stop within a day, when they read the list again.
- **The token leaked or abused.** Revoke it on GitHub (as `axkit-reporter`, Settings → Developer settings → the token → Delete); make a new one; replace the environment secret; release a new version. Every earlier version then gets GitHub's 401 and says "This version can no longer send reports: update axKit from the Help menu, or copy the report" (RP-6.4). Note the day in `CHANGELOG.md`.
- **GitHub revokes the token itself.** The same as a leak: a new token, a new release. The apps lose nothing; unsent reports wait in Your reports.
- **The machine user is suspended.** Reports fall back to copying and GitHub's form until a new account and token exist; the issue forms in the reports repository keep working for people with an account.
- **Yearly** (`docs/axkit/longevity.md`): rotate the token before it expires (one year), check that the workflow still runs (open a test issue with a blocked version line and see it closed), and that `blocklist.json` still parses (`Blocklist.Parse` in `GitHubIssuesTests` is its shape).

## 4. Why these choices

RP-D2 and RP-D8 in `docs/axkit/reports.md`: no sign-in for the user, so a token in the program; a separate repository
and a machine user with one permission, so that a leak reaches one repository's issues and nothing else; GitHub's
terms ask that credentials stay confidential, and this is the narrowest a token in a program can be.
