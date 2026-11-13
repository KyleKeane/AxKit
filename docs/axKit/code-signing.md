# Code signing for Windows (Azure Artifact Signing; prepared 2026-10-25, the workflow built 2026-11-03, on Azure 2026-11-06, retired 2026-11-09)

**Retired on 2026-11-09, the user's decision: "we will use the Windows Store for signing".** The Microsoft Store signs
axKit's package with Microsoft's certificate after certification (`store-distribution.md`, decision 4.4), so the
Authenticode signature this page set up is not needed, and it never signed a release: the identity validation was
still pending, the variable was never set, and every release's notes said "carries no Windows signature". The release
workflow's two signing steps, the `release` environment's three `AZURE_*` secrets and the development environment's
Azure CLI are gone; the GitHub release's `AxKit.exe` stays unsigned, which it always was, and its notes now point at
the Store's copy. This page is kept as the record of what was set up and, in section 7, of what the maintainer deletes
on Azure. Nothing below it is current.

Why: Windows 11's Smart App Control blocks a program that carries no Authenticode signature and that Microsoft's cloud
does not know yet. Every axKit release is a new program file, so on a computer where Smart App Control is on, an update
can be refused ("An Application Control policy has blocked this file"; 3.7.1 and 3.7.4 on the maintainer's second
computer). axKit's own release key (`tools/release-key.ps1`, AX-3.4) signs the zip for the apps' updater; Windows does
not know that key and never checks it. Since 3.7.5 a blocked update stops before it changes anything (AX-3.6). The
lasting fix is an Authenticode signature on `AxKit.exe` from a certificate authority Windows trusts, which Smart App
Control accepts.

The way: **Azure Artifact Signing**, Microsoft's signing service (Trusted Signing, before its rename). Microsoft checks
the maintainer's identity once, against a government ID, and from then on issues, for each signing, a certificate from
its own certificate authority in the maintainer's verified name, valid for a few days, and signs in its cloud: the
private key never exists outside Microsoft's hardware, so there is no key to keep, lose or steal. Windows trusts that
authority, Smart App Control accepts the signed program, and Windows shows the maintainer's name, Dr. Kyle Keane, as
the publisher. The service is paid, the Basic tier at about ten dollars a month with a monthly allowance of signatures,
and it signs from GitHub Actions without a person present. The maintainer chose it on 2026-11-06, after the free
signing service for open-source projects planned on 2026-10-25 declined the project's application, over the other
routes: a commercial certificate with a cloud key costs about three hundred dollars a year; a cheap open-source
certificate keeps its key behind a phone app, so a person must be present at every signing; a certificate on a USB
token cannot sign from GitHub Actions at all.

**Status (2026-11-06, the last before the retirement):** section 1 exists and section 2's steps 1 to 4 are done: the
Azure side has identities that can only sign, and the release workflow signs when the repository variable
`AZURE_CERTIFICATE_PROFILE` is set, and until then releases unsigned, saying so in the notes. Left: the identity
validation (step 5; the first attempt failed on 2026-11-06), the certificate profile (step 6), the variable (step 7),
the development computer's sign-in (step 8) and the first signed release (step 9). None of them happened: see the
top of the page and section 7.

## 1. What exists, and who can do what

The rule: the maintainer's own Azure account, which owns the subscription and can spend money, is used in a browser
only, for the few administrative steps, and is never left signed in on a computer where an AI assistant works. Signing
is done by identities that hold one role, on one resource, and nothing else.

| What | Where | Who may touch it |
|---|---|---|
| The Azure subscription: pay as you go, with a billing account of type Individual, which the identity validation requires | The maintainer's Azure account is its Owner and the only holder of a billing role | The maintainer, in the Azure portal |
| The signing account `axkit-code-signing`, Basic tier, in the resource group `azure-signup-rg`, East US, endpoint `https://eus.codesigning.azure.net/`: a passive resource with no identity of its own | The subscription | Made by the maintainer in the portal; its certificate profiles need the Owner |
| The role Artifact Signing Identity Verifier, on the signing account alone | The maintainer's account | Lets the portal's New identity validation button work, nothing else |
| The GitHub Actions identity: an app registration named "axKit release signing" with no password and no certificate; one federated credential, issuer `https://token.actions.githubusercontent.com`, subject `repo:KyleKeane/AxKit:environment:release`, audience `api://AzureADTokenExchange`; the role Artifact Signing Certificate Profile Signer on the signing account alone; no directory role | Microsoft Entra ID of the maintainer's tenant | Only a run of this repository's release workflow in the `release` environment (branch `main` and tags `v*`) can become it, by exchanging the run's OpenID Connect token; it can sign and nothing else |
| The user `axkit-signer` (display name "axKit signer"), in the same tenant, with the same signer role alone and no directory role | What the development computer's Azure CLI is signed in as | The maintainer and the assistant on that computer: `az` can sign and read the signing account, and cannot make, change or see anything else, nor spend |
| The three ids `azure/login` needs, `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` | Secrets of the GitHub environment `release` | The sign job alone; the build job, which runs the tests and their packages, never sees them (S-30). They are ids, not credentials: without the federated credential's match they open nothing |
| The repository variable `AZURE_CERTIFICATE_PROFILE` | GitHub, at the repository | Names the certificate profile and is the switch: unset, the workflow skips signing and the release notes say the program is unsigned |

What none of the signing identities can do: make or change a resource, read the subscription beyond the signing
account, see or change billing, or run an identity validation. The one cost a signer can cause is signatures beyond
the Basic tier's monthly allowance; a budget alert on the subscription (Cost Management) is the maintainer's option.
Every id of the account, the tenant, the subscription and the identities stays out of this repository, as every id
does; the names above are resource names, not ids.

## 2. Setting it up (the user does the marked steps; Claude the rest)

Nothing of this is needed on a second development computer: the signing happens in GitHub Actions, and a release from
another computer needs only what AX-3.4 already needs there (the release key, `gh` signed in). The Azure CLI on a
development computer is for reading the signing account, and for signing a file by hand if that is ever needed, as
`axkit-signer` (`docs/axKit/development-environment.md`).

1. **Two-factor sign-in on GitHub** (the user), if not on yet (the audit's open item S-36).
2. **The Azure account** (the user; done 2026-11-06): an Azure account with a pay-as-you-go subscription (not a free
   trial, which the service refuses) whose billing account is of type Individual, with the legal name and sold-to
   address as on the government ID; the resource provider `Microsoft.CodeSigning` registered; an Artifact Signing
   account `axkit-code-signing`, Basic, East US, made in the portal. Individuals must be in the United States or
   Canada.
3. **The Azure CLI on the development computer** (Claude; done): `winget install --id Microsoft.AzureCLI --source winget`,
   then `az extension add --name artifact-signing`.
4. **The identities and their roles** (done 2026-11-06 by a script the user ran himself as the Owner, since the
   assistant's permission layer refuses role grants and identity creation, rightly). What it did, for a repetition:
   ```
   az role assignment create --assignee-object-id <the owner's object id> --assignee-principal-type User --role "Artifact Signing Identity Verifier" --scope <the signing account's id>
   az ad app create --display-name "axKit release signing" --sign-in-audience AzureADMyOrg
   az ad sp create --id <the app's client id>
   az ad app federated-credential create --id <the app's client id> --parameters federated-credential.json
   az role assignment create --assignee-object-id <the service principal's object id> --assignee-principal-type ServicePrincipal --role "Artifact Signing Certificate Profile Signer" --scope <the signing account's id>
   az ad user create --display-name "axKit signer" --user-principal-name axkit-signer@<the tenant's domain> --password <a first password, changed at the first sign-in> --force-change-password-next-sign-in true
   az role assignment create --assignee-object-id <the user's object id> --assignee-principal-type User --role "Artifact Signing Certificate Profile Signer" --scope <the signing account's id>
   gh secret set AZURE_CLIENT_ID --env release --repo KyleKeane/AxKit --body <the app's client id>
   gh secret set AZURE_TENANT_ID --env release --repo KyleKeane/AxKit --body <the tenant id>
   gh secret set AZURE_SUBSCRIPTION_ID --env release --repo KyleKeane/AxKit --body <the subscription id>
   ```
   `federated-credential.json` is `{"name":"github-release-environment","issuer":"https://token.actions.githubusercontent.com","subject":"repo:KyleKeane/AxKit:environment:release","audiences":["api://AzureADTokenExchange"]}`.
   The signing account's id is `az artifact-signing show --name axkit-code-signing --resource-group azure-signup-rg --query id -o tsv`.
   The environment `release`, which only the branch `main` and tags `v*` may use, exists since 2026-11-03. To check
   what the identities hold: `az role assignment list --scope <the signing account's id>` lists the three roles above,
   and `az role assignment list --assignee <object id> --all` for each identity shows that one role and nothing at
   the subscription.
5. **Identity validation** (the user, in the portal, as the Owner; Public Trust, Individual): in the signing account,
   Identity validations; the dropdown from Organization to Individual; New identity, Public; the form takes the
   billing account's name and address, read only, so they are corrected there first if they differ from the ID; the
   primary e-mail is the one the Microsoft account signs in with. Then the e-mailed link (it expires after seven days
   and is not resent): sign in with that address, Get verified through the ID verifier (AU10TIX), a PIN from the
   e-mail, a phone number, a QR code to the phone, photos of both sides of the ID, a face check, the Verified ID added
   to Microsoft Authenticator and presented back in the browser (Verifiable Credential, Share). The portal row turns
   Completed. A failed attempt is deleted, its Verified ID removed from Authenticator, and a new request made; the
   causes Microsoft lists are the face check, and a name or an address that differs from the ID. The first attempt
   failed on 2026-11-06.
6. **The certificate profile** (the Owner: the user in the portal, or Claude with the user signed in as the Owner for
   this one command and signed out after): name `axkit-release`, type Public Trust, on the completed identity
   validation, with the street address and the postal code left out of the certificate's subject:
   ```
   az artifact-signing certificate-profile create --account-name axkit-code-signing --resource-group azure-signup-rg --name axkit-release --profile-type PublicTrust --identity-validation-id <from the portal's row> --include-street-address false --include-postal-code false
   ```
   The subject is then the maintainer's name, city, state and country, which is what Windows shows.
7. **The switch** (Claude): `gh variable set AZURE_CERTIFICATE_PROFILE --repo KyleKeane/AxKit --body axkit-release`.
   From the next release on, the sign job signs.
8. **The development computer's sign-in** (the user, on his word): `az logout`, then `az login --use-device-code` as
   `axkit-signer`; the command prints a page and a code, and the first sign-in sets a new password and registers
   Microsoft Authenticator. Check: `az account show --query user.name -o tsv` names `axkit-signer`, and
   `az role assignment list --assignee <its object id> --all` shows the signer role alone. From then on nothing on
   that computer can do more on Azure than sign. The Owner signs in again only in a browser, or for one command on
   the maintainer's word, and is signed out after.
9. **The first signed release** (Claude; the user approves): release with `release.ps1` as always. The sign job signs
   in, signs, checks and zips; the notes name the signer; `release.ps1` signs the zip and publishes as before. A
   failed signing leaves no zip, no draft and nothing published; once the cause is fixed,
   `gh workflow run release.yml -f tag=v<version>` runs the workflow again for the same tag. Last, on the second
   computer with Smart App Control on: Update and restart, no block. That release's `CHANGELOG.md` section says that
   releases from it on are signed, and this page's Status paragraph goes.

## 3. The code signing policy (in `README.md` since 2026-10-25)

> **Code signing policy.** Windows code signing through Microsoft's Azure Artifact Signing, in the release workflow,
> with a certificate Microsoft issues for each signing in the maintainer's verified name, by an identity that can sign
> and do nothing else. Team: Dr. Kyle Keane (www.kylekeane.com): author, reviewer and approver. Every release is built
> from this repository by GitHub Actions and approved by the maintainer before it is published.
>
> **Privacy.** axKit transfers no information to other networked systems unless the user asks it to, except the update
> check, which the user can turn off (Options, General, Check for updates at start). In full: axKit's own code makes
> HTTPS requests to GitHub alone, for the update check (`api.github.com`, the version in the User-Agent header) and the
> update the user chooses (the release's zip and signature from `github.com`, checked before they are installed). Its
> access layers start Claude Code, git, the user's shell and KeePassXC's tool as child processes on the computer, and
> those programs use the network as they would without axKit, under their makers' terms, on the user's command and
> after the user's consent: Claude Code talks to Anthropic, git reaches the repository's remotes on fetch, pull and
> push, the shell's commands reach what they reach, KeePassXC's tool nothing.

## 4. How a release is signed (`.github/workflows/release.yml`)

Three jobs. **build** (a token that can only read, S-30) tests, writes the release notes, and uploads `install.cmd`,
`notes.md` and `tag.txt` as the artifact `build`. **sign** (environment `release`, with `id-token: write`; a job of its
own, so that the tests and their packages never run beside the identity's ids or the reporter token) checks the
repository out, sets the SDK up, downloads `build`, runs `publish.ps1 -NoInstall -NoZip`, which builds `publish/win-x64`
without zipping it, with the reports' reporter token from the environment's secret `AXKIT_REPORTER_TOKEN` written into
the build (docs/axKit/reports.md RP-6.2, docs/axKit/reports-setup.md; since 2026-11-07, when publishing moved here from
the build job for that token) and, when `AZURE_CERTIFICATE_PROFILE` is set, runs `azure/login` with the three ids (GitHub issues
the run an OpenID Connect token for the `release` environment; Azure exchanges it for the GitHub Actions identity,
because that identity's federated credential names this repository and that environment; no secret is stored
anywhere) and `azure/artifact-signing-action` with the endpoint, the account, the profile, `publish/win-x64` filtered
to `exe`, SHA-256 and Microsoft's timestamp server `http://timestamp.acs.microsoft.com` (the certificate lives days;
the timestamp keeps the signature valid after it). The job then checks `publish/win-x64/AxKit.exe` with
`Get-AuthenticodeSignature`, stops unless Windows finds the signature Valid, writes the signer's name into the release
notes, runs `publish.ps1 -ZipOnly` and uploads the artifact `release`. With the variable unset it skips the two signing
steps and writes into the notes that the program carries no Windows signature. **draft** uploads the artifact to a
draft release and attests the build's provenance, as before. Every action is pinned to a commit;
`tests/AxKit.Tests/ReleaseWorkflowTests.cs` keeps the order (publish, sign in, sign, check, zip, draft), the pins, the
switch on both signing steps, the unsigned notice, and the ids in the sign job alone.

`release.ps1` and `tools/sign-release.ps1` are unchanged: the release key still signs the zip, which is what the apps'
updater and `install.cmd` check (AX-3.4).

On a user's computer, nothing changes except that Windows now finds a signature on `AxKit.exe`, with the maintainer's
name as the publisher: Smart App Control and SmartScreen read it; axKit itself makes no new network contact. The
update from an unsigned version to the first signed one goes through the old version's installer, which copies the
new, signed program; Windows checks that file when it starts.

## 5. If it breaks

- `azure/login` fails: the federated credential's subject must read exactly `repo:KyleKeane/AxKit:environment:release`
  (a run outside that environment, or a renamed repository, gets no token), the environment's branch policies must
  still be `main` and `v*`, and the three secrets must be there (`gh secret list --env release`).
- The signing step fails: the variable names a profile that does not exist or is disabled (the portal's Certificate
  profiles shows its status; an identity validation that lapsed disables it, and is renewed as in section 2, step 5),
  the identity lost its role (`az role assignment list --scope <the signing account's id>`), or the account's
  endpoint changed; the step's log says which.
- It fails at `Get-AuthenticodeSignature`: look at the status it printed before relaxing anything.
- The subscription stops paying: the account is disabled and signing fails; the Azure portal says so.
- Once a year, in the longevity check (`docs/axKit/longevity.md`): the sign job of the last release passed; the
  identities hold their one role and nothing more; the environment, the secrets and the variable are still there; the
  service's price and terms have not changed under the project.
- Leaving the service: the two signing steps of the sign job are the one piece that knows it; another signer takes
  their place, and the check and the zip after them stay.
- Users on versions up to the last unsigned one whose Smart App Control is on may still be refused the update to the
  first signed release, when Smart App Control blocks the old installer's copy of the new file; the update stops
  cleanly (AX-3.6) and the zip installs by hand from the release page, where Windows checks the signed program.

## 6. Until then

- Users whose Smart App Control is off or in evaluation update as before.
- Users whose Smart App Control is on may be refused an update; since 3.7.5 the update stops cleanly with the reason,
  and they stay on their version, or turn Smart App Control off (Windows Security, App and browser control). It has
  to stay off while an unsigned axKit is in use: it checks the program at every start, not at the install, has no
  exception for one program, and accepts only a signature from a certificate authority in Microsoft's Trusted Root
  Program, so a certificate the user makes and installs on their own computer changes nothing (Microsoft's Smart App
  Control pages, 2026-11-07). Turning it off was a one-way switch until a reset; Microsoft's FAQ says recent Windows
  updates let it be turned on again. SmartScreen and Defender keep working with it off.
- After a release, `AxKit.exe` can be submitted to Microsoft's file submission page as a developer's file wrongly
  blocked, which can build its reputation sooner; per release, and not certain.

## 7. Retiring (2026-11-09)

Done in the repository the same day: the two signing steps and the `id-token` permission out of `release.yml` (the
job is `package` now), `ReleaseWorkflowTests` rewritten for it, the three `AZURE_*` secrets deleted from the `release`
environment (`gh secret delete <name> --env release`), the Azure CLI out of `development-environment.md`, and the
pages that said "from the first release whose notes say so" (`README.md`, `SECURITY.md`, `GOVERNANCE.md`,
`docs/axKit/README.md`, SPEC AX-3.4) rewritten to say that the Store's copy is the signed one. No release was ever
signed this way, so no user has a signed `AxKit.exe` to lose.

Done on 2026-11-13, at the maintainer's word, with the Azure CLI signed in by him: the signing account
`axkit-code-signing` deleted, then the application registration "axKit release signing" and the user "axKit signer",
then the resource group (it was the sign-up's own, `azure-signup-rg`, holding the signing account and the free
services entry, and a subscription cannot be cancelled while a resource is left), then the subscription cancelled (its
state read back as Warned, which is Azure's word for cancelled; Azure deletes it for good after its own waiting
time, and the Microsoft account itself stays). Nothing of the signing set-up is left in Azure. The list as it stood
until then:

Left to the maintainer, in the Azure portal, when convenient (nothing of it costs anything while it idles except the
signing account's Basic tier, about ten dollars a month, which is the reason to do it soon):

1. The Artifact Signing account `axkit-code-signing` (its resource group holds it alone): delete the resource group,
   which takes the account, its certificate profile if one was made and the pending identity validation with it.
2. Microsoft Entra ID: the application registration for the release workflow with its federated credential (the one
   named for this repository and the `release` environment), and the user `axkit-signer`.
3. The subscription, if it served this alone: cancel it, so that nothing bills; Azure keeps the account.
4. `%USERPROFILE%\.azure` on the development computer (the CLI's tokens), and `winget uninstall Microsoft.AzureCLI`
   if nothing else uses it.
