# Reports: what exists on GitHub and how it is set up

The reports of `docs/axKit/reports.md` (RP-6.2 to RP-6.4, RP-7) rest on a few things outside this repository. This page
records them and the steps to make them again, as `code-signing.md` does for the Windows signing. Written 2026-11-07.

## 1. What exists

| Thing | Where | Who can do what |
| --- | --- | --- |
| The reports repository `AxKit-reports` | https://github.com/KyleKeane/AxKit-reports, made 2026-11-07, private since 2026-11-14 (RP-D11): only the maintainer opens it | Issues are the reports, each a JSON envelope with the report sealed for the maintainer's key (RP-6.1a; those of 5.8.0 and earlier are plain text); `.github/workflows/close-blocked.yml` closes and locks a blocked report; the labels `feedback`, `bug` and `feature`; issues on, wiki, projects and discussions off. Its two issue forms and its old `blocklist.json` serve nobody since it is private: the maintainer removes them |
| The block list `AxKit-blocklist` | https://github.com/KyleKeane/AxKit-blocklist, public, made 2026-11-14 (RP-D12) | `blocklist.json` at its root, `{"reporters": [], "versions": []}`: random install ids and axKit versions whose reports are not accepted, nothing else. The sender and the workflow read it without a sign-in at `https://raw.githubusercontent.com/KyleKeane/AxKit-blocklist/main/blocklist.json`. Its issues are where a blocked install asks, with its install id, when the block is a mistake |
| The reporter token | A fine-grained personal access token of the maintainer's own GitHub account: repository access `AxKit-reports` only, permission Issues: read and write, nothing else, expiry one year | Reads, creates, edits, labels and closes issues of the reports repository, nothing else (RP-6.2). Issues it posts show the maintainer's name as the author; the envelope's `reporter` tells which install sent them |
| The reports key | The private half: `%LOCALAPPDATA%\AxKit-release\reports-key.bin` on the maintainer's computer, made 2026-11-14 with `tools/ReportKey new`, protected for his Windows account. The public half: the constant `ReportSeal.PublicKey` in `src/AxKit.Core/Reports/ReportSeal.cs` | Every copy of axKit seals its reports for the public half; only the private half opens them (section 5). Never in this repository; its passphrase-encrypted backup and the passphrase are in the private repository `KyleKeane/AxKit-keys` since 2026-11-15 (section 5) |
| The keys repository `AxKit-keys` | https://github.com/KyleKeane/AxKit-keys, private, made 2026-11-15, cloned beside this repository as `..\axkit-keys` | The backups of the reports key and the release key, each beside its passphrase (section 5; `docs/axKit/development-environment.md` section 2). Only the maintainer opens it; the reporter token reaches `AxKit-reports` alone and cannot read it |
| The secret `AXKIT_REPORTER_TOKEN` | A secret of the GitHub environment `release` of this repository (the one the Store's credentials are in) | Read by the package job of `release.yml` alone; `publish.ps1` writes it into the build through `tools/reporter-token.ps1` |

axTask's GitHub lists were removed on 2026-11-14 (RP-7, RP-D10): axTask is local only, and the maintainer reads,
relabels, closes, answers and blocks on GitHub itself, in the browser or with `gh`. A second token of the maintainer's, made
for axTask's GitHub actions before that day (Issues and Contents on `AxKit-reports`), is no longer used by anything:
delete it on GitHub (Settings → Developer settings → Fine-grained tokens), and its protected copy on the computer,
`%LOCALAPPDATA%\AxKit\github.token`.

Why one account: GitHub lets a fine-grained token reach only repositories its own account owns (or an organization the
account belongs to), so a separate reporter account could hold no fine-grained token for `KyleKeane/AxKit-reports`; it
would need a classic token, which reaches every repository the account can see, or an organization owning the
repository. The maintainer's own token with one repository and one permission is the narrowest token there is for
this (RP-D2). A reporter account can come later, with an organization, without a change to the code.

## 2. Setting it up, in order

1. **The repositories.** The reports repository exists since 2026-11-07, made with
   `gh repo create KyleKeane/AxKit-reports --public` and made private on 2026-11-14: `README.md`, the labels
   `feedback` and `feature` beside the default `bug`, and the workflow of step 2. The block list's repository exists
   since 2026-11-14, public, issues on: `README.md` and `blocklist.json` (`{"reporters": [], "versions": []}`).
2. **The workflow.** `tools/reports-repository/close-blocked.yml` is the reports repository's
   `.github/workflows/close-blocked.yml`: on `issues: opened` it reads the reporter id and the version from the JSON
   envelope in the issue's body (its members `reporter` and `version`; for an issue of 5.8.0 or earlier, from the
   last line, `Sent from axKit <version> with F4; reporter <id>`), fetches the block list from its public address
   with `curl`, and closes and locks a blocked one with a comment naming the list. It runs
   with the repository's own token (`issues: write`), no secret, and checks nothing out. The copy in this repository
   is applied to the reports repository by the maintainer, by hand.
3. **The reporter token** (the maintainer, in the browser, about two minutes): github.com → the profile picture →
   Settings → Developer settings → Personal access tokens → Fine-grained tokens → Generate new token. Token name
   "axKit reporter"; expiration one year; Resource owner the maintainer's own account; Repository access "Only select
   repositories", `AxKit-reports`; Permissions → Repository permissions → Issues → Read and write (Metadata read comes
   with it); Generate token; copy it once.
4. **The secret.** `gh secret set AXKIT_REPORTER_TOKEN --env release --repo KyleKeane/AxKit < <file holding the token>`
   from a terminal, or Settings → Environments → `release` → Environment secrets on the website. Never a repository
   secret: the build job, which runs the tests and their packages, must not see it (S-30, `ReleaseWorkflowTests`).
5. **A local send first.** `tools\reporter-token.ps1 -Token (Get-Content <file>)` writes the token's file for a
   development build (ignored by git); rebuild and F4, a word, Enter: "Sent, report N", and the issue in the
   repository, its body the envelope; `tools/ReportKey issues` shows its text (section 5). `tools\reporter-token.ps1 -Remove` and deleting the file afterwards leave nothing behind.
6. **A release.** `release.ps1 <version>` as always; the sign job writes the token into the build. The notes do not
   mention the token. Check with a fresh install the same way as step 5.

## 3. When something goes wrong

- **Nonsense from one install or one version: blocking** (RP-6.4). Add the reporter id to `reporters`, or the
  version to `versions`, in `blocklist.json` of `KyleKeane/AxKit-blocklist`, on GitHub itself (the file's edit page
  in the browser, https://github.com/KyleKeane/AxKit-blocklist/edit/main/blocklist.json, or a commit pushed with
  git). The id and the version are the envelope's `reporter` and `version`, in the clear in the issue's body and in
  the lines `tools/ReportKey issues` prints. The workflow closes what still arrives, and the senders stop within a
  day, when they read the list again; a blocked install is told so in the app, with its id and that repository's
  issues page as the place to ask.
- **Unblocking.** Take the id or the version out of the same file. The install sends again by itself within a day,
  when its copy of the list has run out: with its next report, or when the user opens Your reports. A request to be
  unblocked comes as an issue of `KyleKeane/AxKit-blocklist`, with the install id in it; answer and close it there.
  Such an issue is public and written under the asker's own GitHub account: the id in it names an install, nobody.
- **Copies up to 5.8.0** ask for the list at its old address in the reports repository, which answers "not found"
  since that repository is private: their own check finds an empty list, and only the workflow stops them.
- **The token leaked or abused.** Delete it on GitHub (Settings → Developer settings → Fine-grained tokens → the token);
  make a new one; replace the environment secret; release a new version. Every earlier version then gets GitHub's 401
  and says "This version can no longer send reports: update axKit from the Help menu, or copy the report" (RP-6.4).
  Note the day in `CHANGELOG.md`.
- **GitHub revokes the token itself.** The same as a leak: a new token, a new release. The apps lose nothing; unsent
  reports wait in Your reports.
- **Yearly** (`docs/axKit/longevity.md`): rotate the token before it expires (one year), check that the workflow still
  runs (open a test issue with a blocked version line and see it closed), that `blocklist.json` still parses
  (`Blocklist.Parse` in `GitHubIssuesTests` is its shape), and that the reports key still opens a report and its
  backup still opens with its passphrase (section 5).

## 4. Why these choices

RP-D2 and RP-D8 in `docs/axKit/reports.md`: no sign-in for the user, so a token in the program; a separate repository,
so that a leak reaches one repository's issues and nothing else; one permission on one repository, the narrowest a
token in a program can be; GitHub's terms ask that credentials stay confidential, and this is as close as a token in a
program comes to that. RP-D11 and RP-D12: since the token can read that repository's issues, the repository is
private and every report is sealed for a key of the maintainer's before it leaves the user's computer; and the block
list, which every copy must read without a sign-in, has a public repository of its own that holds nothing else.

## 5. The reports key: where it is, its backup, and how reports are read

Every report is encrypted on the user's computer with the public half of one RSA key (3072 bits), a constant in the
code, `ReportSeal.PublicKey` (RP-6.1a). The private half opens them. It was made on 2026-11-14 with
`tools/ReportKey new` on the maintainer's computer and is kept there, as the release key is
(`tools/release-key.ps1`), with one backup in the private repository `KyleKeane/AxKit-keys` (below):

- **Where.** `%LOCALAPPDATA%\AxKit-release\reports-key.bin`, protected for the Windows account that made it (DPAPI,
  through `AxKit.Core/UserSecret`): a copy of the file is useless on another computer or for another account. It is
  never printed, never in this repository, and no test reads it (the tests make their own pairs).
- **Losing it means that every sealed report is unreadable, for good**: the ones already on GitHub and the ones
  every released copy goes on sending until a release carries a new public half. A new Windows account, a
  reinstalled Windows or a new computer loses it unless the backup is imported there (below).
- **The backup.** `dotnet run --project tools\ReportKey -c Release -- export <file>` writes the private half
  encrypted with a passphrase (twelve characters at least, typed at the prompt without being shown, or read from
  standard input; PBKDF2-SHA256 with 600000 rounds, then AES-256-GCM; a text file of base64). It refuses to
  overwrite a file. `import <file>` brings the key back from such a file on a computer that has none (it never
  replaces an existing key file), and prints the public half, which must equal the constant in the code; `public`
  prints the public half and its fingerprint at any time and says whether it is the one this build seals for.
- **Where the backup is (the maintainer's decision, 2026-11-15).** In the private repository
  `KyleKeane/AxKit-keys`, cloned beside this one as `..\axkit-keys`: `reports-key.backup` with its passphrase in
  `reports-key.passphrase.txt`, and the release key's backup beside it (`release-key.backup`,
  `release-key.passphrase.txt`; `docs/axKit/development-environment.md` section 2). The maintainer chose this
  knowingly in place of a password manager: the files are protected by his GitHub sign-in alone, so a stolen
  GitHub account could open the sealed reports (and sign an update with the release key). The reporter token that
  ships in axKit reaches `AxKit-reports` alone, so no token shipped with axKit can read `AxKit-keys`. Restore on a
  computer without the key, from this repository's folder:
  `Get-Content ..\axkit-keys\reports-key.passphrase.txt | dotnet run --project tools\ReportKey -c Release -- import ..\axkit-keys\reports-key.backup`.
  A new backup is the same with `export` and a new file name (export never overwrites), then committed and pushed
  in `..\axkit-keys` by hand: no script syncs that repository.
- **Reading the reports.** `dotnet run --project tools\ReportKey -c Release -- issues` asks `gh` for the open
  issues of the reports repository and prints each: its number, labels, state and title, the envelope's members in
  the clear (format, reporter, version, kind, key, method), then the plain report; an issue without an envelope, of
  5.8.0 or earlier, is printed as it is and marked "not sealed". `open <file>` does the same for one issue body or
  envelope in a file, or on standard input (`gh issue view 12 --repo KyleKeane/AxKit-reports --json body --jq .body`
  piped into `open`). On GitHub's website a sealed report shows its envelope only. `tools/reports-check.ps1` needs
  no key: it reads numbers, states and titles.
- **Replacing the key** (lost, or thought to be copied): `new` on a computer without a key file, the new public half
  into `ReportSeal.PublicKey`, a release. The envelope's `key` member, the public half's fingerprint, tells which
  private key opens a report, so reports sealed for the old key stay readable with the old key if it still exists,
  and `open` says which key a report wants before it tries. The tool never replaces an existing key file.
- `AXKIT_REPORTS_KEY` names another key file for the tool, for trying it without touching the real key.
