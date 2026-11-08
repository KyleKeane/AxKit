# axKit's Microsoft Store account: the setup and the runbook

What exists on Partner Center for axKit, what the maintainer does there by hand, and how a release reaches the Store
from the command line once the setup is done (the plan is `store-distribution.md`; the rules are `SPEC.md` AX-13).
As `reports-setup.md` does for the reports: no secret is written here, only where each lives.

## 1. What exists

- A Partner Center developer account, an individual one on a personal Microsoft account (never written anywhere),
  with the publisher display name axKit. Its dashboard: https://partner.microsoft.com/dashboard
- The product axKit, Store ID 9PK01L2K5000, package identity `axKit.axKit`, publisher
  `CN=F1B55F68-5EAE-4338-B94B-73044EAE40DA` (`store-distribution.md` 3.0). Its Store page, once published:
  https://apps.microsoft.com/detail/9PK01L2K5000; the install line `winget install 9PK01L2K5000 -s msstore`.
- To be made in section 2: a Microsoft Entra tenant associated with the account, and in it an application with the
  Manager role, whose tenant id, client id and key are the only way a program may speak to Partner Center. The three
  live in two places only: GitHub's `release` environment as the secrets `PARTNER_CENTER_TENANT_ID`,
  `PARTNER_CENTER_CLIENT_ID` and `PARTNER_CENTER_CLIENT_SECRET`, and on the maintainer's computer in
  `%LOCALAPPDATA%\AxKit-release\store-credentials.xml`, encrypted for the account (`tools/store-credentials.ps1`).
- The switch: the repository variable `STORE_SUBMIT`, set to `on` once the first publication is live. Unset, no
  workflow touches the Store.

## 2. Your steps, part one: the automation's account (about twenty minutes, once)

Everything here is in the Partner Center website, signed in with the account that reserved axKit. The values you
copy are not secrets except the key, which you type into prompts only, never into a chat.

1. Open https://partner.microsoft.com/dashboard. Press the gear icon at the upper right, then Account settings.
2. In the settings menu choose Tenants, then "Associate Microsoft Entra ID with your Partner Center account". Sign in
   with the Azure account you made for the code signing (its tenant is the one), then Confirm. If Partner Center
   offers to create a new tenant instead, that is fine too: it costs nothing.
3. Back in Account settings choose User management, then the tab "Microsoft Entra applications", then "Add Microsoft
   Entra application". Choose "Create Microsoft Entra application". Name it `axKit release`, reply URL and app id URI
   as Partner Center suggests (any values work; the application is never signed in to interactively). Under "Roles
   applicable to developer programs" tick Manager. Press Create.
4. Open the application you just created in that list. Copy its Tenant ID and Client ID; both can go into the chat.
5. On the same page press "Add new key". Partner Center shows the key once. Do not close the page yet.
6. In your own terminal, in the repository's folder, run `.\tools\store-credentials.ps1` and answer its three
   prompts; the key is typed hidden. Then run `.\tools\store-credentials.ps1 -Show` to see the two ids echoed.
7. Still in your terminal, the same three values for GitHub Actions, each command prompting for its value:
   `gh secret set PARTNER_CENTER_TENANT_ID --env release`, `gh secret set PARTNER_CENTER_CLIENT_ID --env release`,
   `gh secret set PARTNER_CENTER_CLIENT_SECRET --env release`.
8. Now close the key's page. Tell me; I run `tools\store-submit.ps1 -Status`, which reads axKit's state from
   Partner Center with the three values and proves them.

## 3. Your steps, part two: the first submission (about an hour, once)

Microsoft requires the first publication to go through Partner Center's website; every later one goes from the
command line (section 4). The texts are `store-listing.md`: for each field, tell me its name and I put the text on
your clipboard, then paste. The files to upload are in the repository's `publish\store` folder (the six screenshots,
`1-axdown.png` to `6-axprocess.png`) and `publish\msix` (the logo `StoreLogo-300.png`); I tell you each full path
when you reach the field.

1. In Partner Center open Apps and games, then axKit, then "Start your submission". The submission is a list of
   sections; each has a Save button, and the last page has the Submit button. They can be done in any order and over
   several days.
2. Pricing and availability: Markets, all; Visibility, "Make this product available and discoverable in the
   Microsoft Store"; Pricing, Free. Save.
3. Properties: Category, Developer tools. Privacy policy URL, website and support contact as `store-listing.md`
   says. Under Product declarations tick "This product has been tested to meet accessibility guidelines". Save.
4. Age ratings: start the questionnaire and answer truthfully (`store-listing.md`, Age ratings). Save.
5. Store listings, English (United States): Description; What's new in this version; Product features, one per
   field; Screenshots, the six files; Store logos, the 300 pixel file; Search terms; Copyright and trademark info;
   Additional license terms; Developed by. Save. (The texts come from me, field by field.)
6. Packages: upload the package file. It is the `AxKit-5.2.0.msix` of the 5.2.0 release on GitHub, which I make on
   your word (it carries the reporter token the local build lacks); I download it for you and tell you its path.
   Partner Center then asks for the restricted capability `runFullTrust`: paste the sentence from `store-listing.md`,
   Submission options. Save. Leave this section for last if the release is not out yet.
7. Submission options: Notes for certification, the text from `store-listing.md`; Publishing hold options, "Publish
   this submission as soon as it passes certification". Save.
8. Press Submit to the Store. Tell me. Certification takes from hours to days; I read the state (section 5) and, if
   it fails, fix what it names and we submit again.

## 4. How a release reaches the Store once both parts are done

`release.ps1 <version>` works as before: the tests, the version, the tag, the workflow, the signed zip, the published
GitHub release. When the release is published and `STORE_SUBMIT` is `on`, the workflow `store.yml` runs in the
`release` environment: it downloads the release's `AxKit-<version>.msix` (the attested file GitHub published, never
a rebuild), and `tools/store-submit.ps1` posts it to Partner Center with the submission API: a new submission is
created as a copy of the last published one, the old package is marked for deletion and the new one added, What's new
is set from the release notes, the zip with the package is uploaded to the address the API gives, the submission is
committed, and the script waits until the Store has taken it in (PreProcessing or Certification) or refused it,
writing the result into the run's summary. Certification then takes its hours or days without us; the Store updates
every installed copy by itself afterwards. Nothing else of the release changes: the zip path's users get their update
notice as before.

The same script by hand, with the credentials kept by `tools/store-credentials.ps1`:
`.\tools\store-submit.ps1 -Package publish\AxKit-5.2.1.msix -Notes notes.md` submits, `-Status` reads the state, and
`-DryRun` shapes the submission without touching the network.

## 5. Reading the state, and when something fails

- `.\tools\store-submit.ps1 -Status` prints the last published and the pending submission with its status and the
  Store's error and warning codes; the same is in Partner Center under the product's overview.
- A failed certification names the policy; the fix goes into the next release (or, for a listing text, into
  `store-listing.md` and the next submission).
- A submission changed by hand in Partner Center can no longer be touched by the API: the script says so; delete that
  submission in Partner Center and let the next release make a new one.
- While a submission is pending the API refuses a new one (409): a release made during a certification is posted by
  running the workflow again from the Actions tab (`store.yml`, Run workflow, the tag) once the Store is free.
- To stop the automation, remove the variable: `gh variable delete STORE_SUBMIT`. The releases go on as before.

## 6. Security

The application can do what the Manager role allows on Partner Center and nothing else in Azure; it holds no
subscription role. Its key is in the `release` environment, which only `main` and `v*` tags may use, and in the
maintainer's protected file; it never enters a build, a log or the chat. Rotating it is: Add new key in Partner
Center, the three commands of section 2 steps 6 and 7 again, and the old key removed on the application's page.
