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

## 2. The automation's account: not offered to this account (2026-11-09)

Microsoft's submission API, and the Store Developer CLI on top of it, speak to Partner Center only as a Microsoft
Entra application with the Manager role on the account, added under Account settings, User management, Microsoft
Entra applications, after a tenant is associated under Account settings, Organization profile, Tenants. On
2026-11-09 the maintainer's Individual account showed no Tenants entry at all, and a report of October 2026 by another
developer with an Individual account (MicrosoftDocs/windows-dev-docs issue 6153, unanswered) shows the Microsoft Entra
applications tab and the developer-program roles missing even after a tenant was associated. Microsoft's pages say
Entra sign-in is for Company accounts and that an Individual account cannot be changed into one. So the API route is
built (section 4, `store.yml`, `tools/store-submit.ps1`) and kept switched off, ready for the day the account gets the
option (a support request to Partner Center asking for it is section 7); until then every release reaches the Store
as section 5 says, by an upload on the website, which is to be automated from this computer (section 6). The steps
below are what the option needs, kept for that day.

Everything here is in the Partner Center website, signed in with the account that reserved axKit. The values you
copy are not secrets except the key, which you type into prompts only, never into a chat.

1. Open https://partner.microsoft.com/dashboard. Press the gear icon (Settings) at the upper right, then Account
   settings. The Account settings page has a navigation on its left side with headings; the first heading is
   Organization profile, and its entries include Legal info, Identifiers and Tenants (Microsoft's pages of 2026 give
   this path: Account settings, Organization profile, Tenants). With NVDA, Insert+F7 lists the page's links; typing
   "ten" lands on Tenants. If the heading is collapsed, press Enter on Organization profile first.
2. The Tenants page has two tabs, Partner and Developer. On the Developer tab press Associate (the Windows pages
   call the button "Associate Microsoft Entra ID with your Partner Center account"). Sign in with the Azure account
   you made for the code signing (its tenant is the one), then Confirm. If the page offers Create instead, a new
   tenant costs nothing and serves as well. If the navigation has no Tenants entry at all, read me the entries you
   do see under Organization profile, and we look together.
3. Back in Account settings choose User management in the left navigation, then the tab "Microsoft Entra
   applications", then "Add Microsoft Entra application". Choose "Create Microsoft Entra application". Name it
   `axKit release`, reply URL and app id URI as Partner Center suggests (any values work; the application is never
   signed in to interactively). Under "Roles applicable to developer programs" tick Manager. Press Create.
4. Open the application you just created in that list. Copy its Tenant ID and Client ID; both can go into the chat.
5. On the same page press "Add new key". Partner Center shows the key once. Do not close the page yet.
6. In your own terminal, in the repository's folder, run `.\tools\store-credentials.ps1` and answer its three
   prompts; the key is typed hidden. Then run `.\tools\store-credentials.ps1 -Show` to see the two ids echoed.
7. Still in your terminal, the same three values for GitHub Actions, each command prompting for its value:
   `gh secret set PARTNER_CENTER_TENANT_ID --env release`, `gh secret set PARTNER_CENTER_CLIENT_ID --env release`,
   `gh secret set PARTNER_CENTER_CLIENT_SECRET --env release`.
8. Now close the key's page. Tell me; I run `tools\store-submit.ps1 -Status`, which reads axKit's state from
   Partner Center with the three values and proves them.

## 3. Your steps: the first submission (about an hour, once)

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
6. Packages: upload the package file, the `AxKit-5.2.0.msix` of the 5.2.0 release on GitHub (released 2026-11-08;
   it carries the reporter token the local build lacks), which I downloaded into the repository's `publish\store`
   folder; I tell you its full path when you reach the field. Partner Center then asks for the restricted capability
   `runFullTrust`: paste the sentence from `store-listing.md`, Submission options. Save.
7. Submission options: Notes for certification, the text from `store-listing.md`; Publishing hold options, "Publish
   this submission as soon as it passes certification". Save.
8. Press Submit to the Store. Tell me. Certification takes from hours to days; I read the state (section 5) and, if
   it fails, fix what it names and we submit again.

## 4. How a release reaches the Store once the API route is open (built, switched off)

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

## 5. How a release reaches the Store today: the upload on the website

Until section 4 can run, a release is `release.ps1 <version>` as before, and then five steps on the website, which
take about five minutes: the release's `AxKit-<version>.msix` downloaded from its GitHub page (I do that and name
the path), then in Partner Center: Apps and games, axKit, "Update" (a new submission, a copy of the last one),
Packages, the file uploaded, the old package removed from the list, Save; What's new in this version under Store
listings, the release's notes pasted (I put them on your clipboard); Submit to the Store. The Store does the rest.
Section 6 is the plan to take these clicks off your hands.

## 6. Automating the website: tools/EdgeDrive (built 2026-11-09, used for the first submission)

The website is driven from this computer by `tools/EdgeDrive`, a small C# program with no package that speaks the
DevTools protocol to a Microsoft Edge window, one command per run (the commands are listed at the top of its
`Program.cs`: `targets`, `open`, `nav`, `text`, `find`, `click`, `clickxy`, `focus`, `type`, `press`, `upload`,
`dropxy`, `shot`, `wait`, `eval`, `evalfile`). The window is Edge started on a profile of its own, since Edge refuses
debugging on the everyday profile:

    msedge.exe --user-data-dir="%LOCALAPPDATA%\AxKit-release\edge-profile" --remote-debugging-port=9222 https://partner.microsoft.com/dashboard

The maintainer signs in to Partner Center in that window once, with his own second factor; the profile keeps the
session, and no password is ever handled by the tool. While the port is open, any program of this account could
drive that window, so it is opened for the work and closed after. What the first submission taught (2026-11-09):
Partner Center is an Angular application of web components (`he-button`, `he-select`, `he-checkbox`, `he-option`)
inside shadow roots, so every search of the page walks the shadow roots; a synthetic click serves most controls, but
state the page must register (the age ratings' radios, the IARC terms box) needs a real click (`clickxy`, a mouse
event at the control's place); a button's disabled attribute lags behind what the page shows, so a screenshot
(`shot`) decides; the age ratings questionnaire renders its follow-up questions ten to thirty seconds after an
answer, and loses every answer when the page is left before Save; a file set on the upload field is ignored, and a
file dropped on the "Drag your packages here" area (`dropxy`, a drag event carrying the file's path) uploads. A
release's upload is section 5's five steps done with these commands; a script for them follows once a second
submission has shown that the pages hold still. The API route of section 4 replaces all of it the day the account
gets the option.

## 7. The support request for the API route

A request to Partner Center's support (Help and support, in the dashboard's upper right), in the maintainer's words:
the account is an Individual developer account with the product axKit (Store ID 9PK01L2K5000); it needs the Microsoft
Store submission API for its releases; Account settings shows no Tenants entry and no Microsoft Entra applications
tab; please enable the tenant association and the Microsoft Entra applications with the developer-program roles for
this account, or say that an Individual account cannot have them. The answer decides between sections 4 and 6.

## 8. Reading the state, and when something fails

- `.\tools\store-submit.ps1 -Status` prints the last published and the pending submission with its status and the
  Store's error and warning codes, once the API route is open; until then the same is in Partner Center under the
  product's overview.
- A failed certification names the policy; the fix goes into the next release (or, for a listing text, into
  `store-listing.md` and the next submission).
- A submission changed by hand in Partner Center can no longer be touched by the API: the script says so; delete that
  submission in Partner Center and let the next release make a new one.
- While a submission is pending the API refuses a new one (409): a release made during a certification is posted by
  running the workflow again from the Actions tab (`store.yml`, Run workflow, the tag) once the Store is free.
- To stop the automation, remove the variable: `gh variable delete STORE_SUBMIT`. The releases go on as before.

## 9. Security

The application of section 2, when it exists, can do what the Manager role allows on Partner Center and nothing
else in Azure; it holds no subscription role. Its key is in the `release` environment, which only `main` and `v*`
tags may use, and in the maintainer's protected file; it never enters a build, a log or the chat. Rotating it is: Add
new key in Partner Center, the three commands of section 2 steps 6 and 7 again, and the old key removed on the
application's page. The website automation of section 6 holds no credential at all: it drives the maintainer's own
signed-in browser on this computer, and nothing else can.
