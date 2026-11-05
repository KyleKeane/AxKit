# Code signing for Windows (SignPath Foundation; prepared 2026-10-25, the workflow built 2026-11-03)

Why: Windows 11's Smart App Control blocks a program that carries no Authenticode signature and that Microsoft's cloud
does not know yet. Every axKit release is a new program file, so on a computer where Smart App Control is on, an update
can be refused ("An Application Control policy has blocked this file"; 3.7.1 and 3.7.4 on the maintainer's second
computer). axKit's own release key (`tools/release-key.ps1`, AX-3.4) signs the zip for the apps' updater; Windows does
not know that key and never checks it. Since 3.7.5 a blocked update stops before it changes anything (AX-3.6). The
lasting fix is an Authenticode signature on `AxKit.exe`, which Smart App Control accepts.

The way: **SignPath Foundation**, which signs open-source projects for free with its own certificate, from the
project's GitHub build, after an application and a review of the project. The signature names SignPath Foundation as
the publisher, and the maintainer approves every release's signing by hand, which is the Foundation's condition and
the project's code signing policy (section 3). The alternatives, a paid signing service or a certificate on a hardware
token, cost money every year and put the maintainer's legal name on the signature; none is in use.

**Status (2026-11-03):** the repository side is built on the branch `signing` (section 4) and merges once SignPath has
accepted the project and section 2 is done; `main` releases unsigned until then. Next is the application (section 2,
step 2, the user's). Once the sign job has passed on a release, this paragraph goes.

## 1. Does axKit qualify (SignPath Foundation's terms, read 2026-10-25)

| Condition | axKit |
|---|---|
| An OSI-approved licence, no commercial dual licensing | The GNU AGPL 3 only (`LICENSE`) since 2026-10-29, MIT before; the terms in `NOTICE.md` are those its section 7 allows. The contributor licence in `CONTRIBUTING.md` lets the copyright holder relicense; no other edition is sold today, and one sold later would need a new look at this condition |
| Only open-source components, no proprietary code of the maintainer's | Yes: no packages; the .NET runtime inside the single file is Microsoft's, already signed by Microsoft |
| Actively maintained, already released in the form to be signed | Yes: releases since 1.0.0, the zip on GitHub |
| Functionality described on the download page | The release page and `README.md` |
| Own project: the signing team develops it and owns the repository | Yes (the maintainer) |
| No hacking tools, nothing that circumvents security measures | Yes. axKit installs per user, never elevated; the AI tools are off until the user turns them on |
| Privacy: data sent elsewhere needs a privacy policy shown at install, and a way to turn it off | Done 2026-10-25: `PRIVACY.md` (every network contact and outside program, per app), in the zip, in every window (About, Privacy), summarised in the README, and said by the installer before it changes anything; the update check has its setting |
| System changes come with clear warnings; uninstall is provided | Done: the installer's first lines say every place it writes before it writes them; Settings, Apps has Uninstall (AX-2.5) |
| Multi-factor sign-in for every team member on SignPath and GitHub | The user turns it on for GitHub and for SignPath (`GOVERNANCE.md` commits to it) |
| Roles: authors, reviewers, approvers | Done: `GOVERNANCE.md` and the README's code signing policy (the maintainer, all three) |
| A "Code signing policy" on the home page and the release pages | Done: the README's section, linked from every release page's notes (the release workflow); its one sentence about SignPath changes once they accept |
| Product name and version in the file's metadata, the same on every build | `src/AxKit/AxKit.csproj`: `<Product>axKit</Product>`, `<Version>` set by `release.ps1`; the artifact configuration (section 2) holds the product name to "axKit" |
| Built from source by an automated build; each release approved by hand | The release workflow builds the tag on GitHub's own runners; SignPath checks the build's origin; the maintainer approves each signing in SignPath |

Nothing found that disqualifies axKit; SignPath decides on the application.

## 2. Setting it up (the user does the marked steps; Claude the rest)

SignPath's own side has no command line: its web app (app.signpath.io) is where steps 3 and 4 happen, once. The
GitHub side is `gh`. Nothing of this is needed on a second development computer: the signing happens in GitHub
Actions, and a release from another computer needs only what AX-3.4 already needs there (the release key, `gh` signed
in).

1. **Two-factor sign-in on GitHub** (the user), if not on yet (the audit's open item S-36).
2. **Apply** (the user, in a browser): signpath.org, Apply, for the project https://github.com/KyleKeane/AxKit, an
   AGPL-3.0-only bundle of accessible Windows apps for a blind developer, built by GitHub Actions
   (`.github/workflows/release.yml`), released on GitHub since 1.0.0, with the code signing policy in its README
   (section 3). SignPath's review takes days to weeks; the answer comes by e-mail with an invitation to an
   organisation of the project's own in SignPath, with SignPath Foundation's certificate in it.
3. **The SignPath side** (the user, in app.signpath.io; the names are the ones the workflow uses):
   1. Multi-factor sign-in on the SignPath account (the Foundation's condition).
   2. Trusted Build Systems (top menu), Add predefined, GitHub.com.
   3. Projects, add a project: name axKit, slug `AxKit` (the workflow's `project-slug`), Repository URL
      `https://github.com/KyleKeane/AxKit` (origin verification reads it). In the project, Trusted build systems,
      Link, GitHub.com.
   4. An artifact configuration, custom XML (the default one of the project):
      ```xml
      <artifact-configuration xmlns="http://signpath.io/artifact-configuration/v1">
        <zip-file>
          <pe-file path="AxKit.exe" product-name="axKit">
            <authenticode-sign />
          </pe-file>
        </zip-file>
      </artifact-configuration>
      ```
      A GitHub artifact is a zip; the build job uploads `AxKit.exe` alone as the artifact `unsigned`, so the zip has
      the file at its root. `product-name` holds the file's product name to "axKit", as the terms ask. A version
      restriction (`product-version="${version}"` with a `<parameters>` block) would need the workflow to pass the
      version in the action's `parameters` input; not done, the tag names the version.
   5. A signing policy: name Release signing, slug `release-signing` (the workflow's `signing-policy-slug`), purpose
      release, SignPath Foundation's certificate, the approval process on with the maintainer as approver and one
      approval required, Verify origin on, allowed branch names `main` and `v*` (a release runs on a tag push, and
      GitHub reports the tag's name as the run's branch; if origin verification refuses a tag build, the pattern is
      widened in SignPath, nothing changes here).
   6. A CI user (Users; for example `axkit-ci`), made the policy's submitter: with trusted build system verification,
      no interactive user may submit. Its API token (My profile, Generate token) is shown once; it goes to step 4.
4. **The GitHub side** (Claude, from PowerShell on the maintainer's computer; the user pastes the token):
   ```
   gh api -X PUT repos/KyleKeane/AxKit/environments/release -F "deployment_branch_policy[protected_branches]=false" -F "deployment_branch_policy[custom_branch_policies]=true"
   gh api -X POST repos/KyleKeane/AxKit/environments/release/deployment-branch-policies -f name=main -f type=branch
   gh api -X POST repos/KyleKeane/AxKit/environments/release/deployment-branch-policies -f "name=v*" -f type=tag
   gh variable set SIGNPATH_ORGANIZATION_ID --repo KyleKeane/AxKit --body <the organisation's id, from its page in SignPath>
   gh secret set SIGNPATH_API_TOKEN --env release --repo KyleKeane/AxKit
   ```
   The environment `release`, which only the branch `main` and tags `v*` may use, exists since 2026-11-03 (the
   first three lines answer with the environment and each policy as made). The last line, run by the user, asks for
   the value: paste the token and press Enter, so that it never passes through Claude's session. `gh secret list
   --env release` and `gh variable list` then show the two names. The organisation id is not a secret; it is kept
   out of this repository all the same, as every id is.
5. **The first signed release** (Claude; the user approves): merge `signing` into `main`, take the Status paragraph
   out of this page, release with `release.ps1`. The sign job submits the request and waits up to an hour; SignPath
   e-mails the maintainer a link to the request, where Approve is. Then the zip, the draft, and `release.ps1`
   signs and publishes as before. A failed or refused signing leaves no zip, no draft and nothing published; once the
   cause is fixed, `gh workflow run release.yml -f tag=v<version>` runs the workflow again for the same tag, which
   builds afresh and submits a new request. Last, on the second computer with Smart App Control on: Update and
   restart, no block.

## 3. The code signing policy (in `README.md` since 2026-10-25; the first paragraph's wording once accepted)

> **Code signing policy.** Free code signing provided by SignPath.io, certificate by SignPath Foundation.
> Team: Dr. Kyle Keane (www.kylekeane.com): author, reviewer and approver. Every release is built from this repository
> by GitHub Actions and approved by hand before it is signed.
>
> **Privacy.** axKit asks GitHub for the latest release at start, to offer updates; this is a setting (Options, General,
> Check for updates at start) and can be turned off. axClaude runs Claude Code, the user's own program, which talks to
> Anthropic as Claude Code does. Nothing else leaves the computer: axKit sends no other data to any networked system
> unless the user asks it to.

## 4. How a release is signed (`.github/workflows/release.yml`, built 2026-11-03)

Three jobs. **build** (a token that can only read, S-30) tests, runs `publish.ps1 -NoInstall -NoZip`, which builds
`publish/win-x64` without zipping it, writes the release notes, uploads `publish/win-x64/AxKit.exe` alone as the
artifact `unsigned` (the one SignPath signs; its id is the job's output) and the folder with `install.cmd`, `notes.md`
and `tag.txt` as the artifact `build`. **sign** (environment `release`; a job of its own, so that the tests and their
packages never run beside the API token) checks the repository out for `publish.ps1`, downloads `build`, and runs
`signpath/github-action-submit-signing-request` with the token, the organisation id, the project `AxKit`, the policy
`release-signing` and the `unsigned` artifact's id; SignPath fetches the artifact from the run, verifies its origin,
waits for the maintainer's approval (the job waits up to an hour), signs, and the action puts the signed file in
`signed\AxKit.exe`. The job copies it over `publish/win-x64/AxKit.exe`, checks it with `Get-AuthenticodeSignature`,
stops unless Windows finds the signature Valid, writes the signer's name into the release notes, runs
`publish.ps1 -ZipOnly` and uploads the artifact `release`. **draft** uploads it to a draft release and attests the
build's provenance, as before. Every action is pinned to a commit; `tests/AxKit.Tests/ReleaseWorkflowTests.cs` keeps
the order (publish, upload unsigned, sign, check, zip, draft), the pins, and the token in the sign job alone.

`release.ps1` and `tools/sign-release.ps1` are unchanged: the release key still signs the zip, which is what the apps'
updater and `install.cmd` check (AX-3.4). `release.ps1`'s wait for the workflow covers the wait for the approval.

On a user's computer, nothing changes except that Windows now finds a signature on `AxKit.exe`, with SignPath
Foundation as the publisher: Smart App Control and SmartScreen read it; axKit itself makes no new network contact. The
update from an unsigned version to the first signed one goes through the old version's installer, which copies the
new, signed program; Windows checks that file when it starts.

## 5. If it breaks

- The sign job fails on origin verification: the project's Repository URL, the policy's allowed branch names (a tag
  build reports the tag's name) or a run not on GitHub's own runners; SignPath's request page says which.
- It fails on the artifact configuration: `AxKit.exe` is not at the zip's root (the `unsigned` upload changed) or its
  product name is no longer "axKit" (`src/AxKit/AxKit.csproj`).
- It times out: nobody approved within the hour. Approve, or run the workflow again for the tag (section 2, step 5).
- It fails to sign in: the API token was revoked or its CI user lost the submitter role; a new token, pasted with
  `gh secret set SIGNPATH_API_TOKEN --env release`.
- It fails at `Get-AuthenticodeSignature`: look at the status it printed before relaxing anything.
- Once a year, in the longevity check (`docs/axkit/longevity.md`): the sign job of the last release passed; the
  environment, the secret and the variable are still there; SignPath's terms have not changed under the project.
- Leaving the service: the sign job of the workflow is the one piece that knows it; another signer takes its place
  there, and the check and the zip after it stay.
- Users on versions up to 5.0.0 whose Smart App Control is on may still be refused the update to the first signed
  release, when Smart App Control blocks the old installer's copy of the new file; the update stops cleanly (AX-3.6)
  and the zip installs by hand from the release page, where Windows checks the signed program.

## 6. Until then

- Users whose Smart App Control is off or in evaluation update as before.
- Users whose Smart App Control is on may be refused an update; since 3.7.5 the update stops cleanly with the reason,
  and they stay on their version, or turn Smart App Control off (Windows Security, App and browser control), which
  Windows cannot turn on again without a reset.
- After a release, `AxKit.exe` can be submitted to Microsoft's file submission page as a developer's file wrongly
  blocked, which can build its reputation sooner; per release, and not certain.
