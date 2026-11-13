# axKit's Microsoft Store account: the setup and the runbook

What exists on Partner Center for axKit, what the maintainer does there by hand, and how a release reaches the Store
through its website (the plan is `store-distribution.md`; the rules are `SPEC.md` AX-13).
As `reports-setup.md` does for the reports: no secret is written here, only where each lives.

## 1. What exists

- A Partner Center developer account, an individual one on a personal Microsoft account (never written anywhere),
  with the publisher display name axKit. Its dashboard: https://partner.microsoft.com/dashboard
- The product axKit, Store ID 9PK01L2K5000, package identity `axKit.axKit`, publisher
  `CN=F1B55F68-5EAE-4338-B94B-73044EAE40DA` (`store-distribution.md` 3.0). Its Store page, once published:
  https://apps.microsoft.com/detail/9PK01L2K5000; the install line `winget install 9PK01L2K5000 -s msstore`.
- No application, key or secret for Partner Center: nothing of the repository signs in there (section 2).

## 2. The API route: closed to this account, removed on 2026-11-13

Microsoft's submission API, and the Store Developer CLI on top of it, speak to Partner Center only as a Microsoft
Entra application with the Manager role on the account, added under Account settings, User management, Microsoft
Entra applications, after a tenant is associated under Account settings, Organization profile, Tenants. On
2026-11-09 the maintainer's Individual account showed no Tenants entry at all, and a report of October 2026 by another
developer with an Individual account (MicrosoftDocs/windows-dev-docs issue 6153, unanswered) shows the Microsoft Entra
applications tab and the developer-program roles missing even after a tenant was associated. Microsoft's pages say
Entra sign-in is for Company accounts and that an Individual account cannot be changed into one.

A route through the API was built on 2026-11-09 all the same (a workflow that ran on a published release behind a
repository variable, a script that posted the package, and a script that kept the application's three values
encrypted on the maintainer's computer) and was never switched on: the application it needed could not be made. On
2026-11-13 the maintainer decided to give the route up for good, and the workflow and the two scripts were removed.
Every release reaches the Store as section 5 says, by an upload on the website, by hand or with the tool of
section 6.

## 3. The first submission (done 2026-11-08 through tools/EdgeDrive; the steps kept as the record of what a submission holds)

Microsoft requires the first publication to go through Partner Center's website, and every later one goes the same
way (section 5), by hand or through section 6's tool. The first
submission (Submission 1, `AxKit-5.2.1.msix`, "In certification" since 2026-11-08, Pacific time) was made by the
assistant with `tools/EdgeDrive` while the maintainer was away, every section as below; he only signed in. The texts
are `store-listing.md`; the files to upload are in the repository's `publish\store` folder (the six screenshots,
`1-axdown.png` to `6-axprocess.png`) and `publish\msix` (the logo `StoreLogo-300.png`).

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
6. Packages: upload the package file, the `AxKit-<version>.msix` of the release on GitHub (it carries the reporter
   token the local build lacks), downloaded into the repository's `publish\store` folder; the first submission
   carries `AxKit-5.2.1.msix` (the 5.2.0 package, with seventeen entries, was refused: section 6). Save.
7. Submission options: Publishing hold options, "Publish this submission as soon as it passes certification"; under
   Restricted capabilities the page asks why the package needs `runFullTrust`: the sentence from `store-listing.md`,
   Submission options, in the box (500 characters at most; the section stays Incomplete without it). Save. The notes
   for certification are on the Additional Testing Information page (Supplemental info in the left list), saved by
   the floppy icon beside its title.
8. Press "Submit for certification" on the product's overview once every section says Complete. Certification takes
   from hours to three business days; the state is on the overview (section 8) and, if it fails, the policy it names
   is fixed and the next submission goes the same way.

## 4. How a release reaches the Store through the API (removed on 2026-11-13)

This section described the API route, which was removed on 2026-11-13 (section 2). Section 5 is how a release
reaches the Store.

## 5. How a release reaches the Store: the upload on the website

A release is `release.ps1 <version>` as before, and then five steps on the website, which take about five minutes: the release's `AxKit-<version>.msix` downloaded from its GitHub page (I do that and name
the path), then in Partner Center: Apps and games, axKit, "Update" (a new submission, a copy of the last one),
Packages, the file uploaded, the old package removed from the list, Save; What's new in this version under Store
listings, the release's notes pasted (I put them on your clipboard); Submit to the Store. The Store does the rest:
certification takes its hours or days, and the Store updates every installed copy by itself afterwards. Section 6 is
the tool that takes these clicks off your hands.

## 6. Automating the website: tools/EdgeDrive (built 2026-11-09, used for the first submission)

The website is driven from this computer by `tools/EdgeDrive`, a small C# program with no package that speaks the
DevTools protocol to a Microsoft Edge window, one command per run (the commands are listed at the top of its
`Program.cs`: `targets`, `open`, `use`, `nav`, `text`, `find`, `click`, `clickxy`, `focus`, `type`, `press`,
`upload`, `dropxy`, `front`, `close`, `dialog`, `shot`, `wait`, `eval`, `evalfile`, and `call` for any other method of
the protocol with its parameters as JSON). The window is Edge started on a profile of its own, since Edge refuses
debugging on the everyday profile, with the switches that keep its pages awake:

    msedge.exe --user-data-dir="%LOCALAPPDATA%\AxKit-release\edge-profile" --remote-debugging-port=9222 --disable-features=CalculateNativeWinOcclusion,IntensiveWakeUpThrottling --disable-background-timer-throttling --disable-renderer-backgrounding --disable-backgrounding-occluded-windows https://partner.microsoft.com/dashboard

The switches matter: Edge treats every page as hidden while the computer's session is locked (the maintainer away
from the keyboard is exactly when the tool runs) and throttles its timers to one tick a minute, so uploads, deletes
and saves seemed ignored and happened minutes later, in a heap; with the switches the page stays visible and acts at
once (found 2026-11-08). The maintainer signs in to Partner Center in that window once, with his own second factor;
the profile keeps the session, and no password is ever handled by the tool. While the port is open, any program of
this account could drive that window, so it is opened for the work and closed after (`call Browser.close '{}'`; the
`close` command closes one tab, and closing the last tab ends Edge with it, which cost a sign-in once). What the
first submission taught (2026-11-08):
Partner Center is an Angular application of web components (`he-button`, `he-select`, `he-checkbox`, `he-option`)
inside shadow roots, so every search of the page walks the shadow roots; a synthetic click serves most controls, but
state the page must register (the age ratings' radios, the IARC terms box) needs a real click (`clickxy`, a mouse
event at the control's place); a button's disabled attribute lags behind what the page shows, so a screenshot
(`shot`) decides; the age ratings questionnaire renders its follow-up questions ten to thirty seconds after an
answer, and loses every answer when the page is left before Save; a file set on the upload field is taken late or not at all, and a
file dropped on the "Drag your packages here" area (`dropxy`, drag events carrying the file's path, as a hand drags:
enter, moves, drop) uploads, so drop once and wait for the page's "Uploading" line before anything else, since every
drop that seemed ignored uploaded too and the same package then stands twice (the Store refuses a duplicate; Delete
the extra). The page's Delete link acts but shows the result only after a reload. A JavaScript confirm holds the page
and every evaluation until `dialog accept`. The Additional Testing Information page saves through the floppy icon
beside its title ("Save description"), which a script click presses. The package's validation runs for several
minutes after the upload and names its errors and warnings under the package. The Store listing page (2026-11-08):
its text fields are plain Angular controls, set by a script (value, then an `input` event) from the texts of
`store-listing.md`; Product features grow one field per press of "Add more" (`feature-0` onwards); the screenshot
card's file input (the first `input[type=file]` of the page, under `clear-upload-card`) takes one file through
`upload` at once while the page is visible, and every uploaded image gets a file input of its own in front of it,
so the card's input is found again before each file; a file dropped with `dropxy` on that card lands three times and
opens in a tab of its own, which hides the listing page behind it, so screenshots go through the input, never a
drop; each image's caption is "Add image caption", a textarea `captionTextArea` and its OK; the keyword picker is an
`he-select` with `freeform` and `multiple`: a keyword typed into its inner input and Enter makes a chip, but the
page's form reads the picker only on its `he-blur` event, so a real click elsewhere (`clickxy` on another field)
after the last keyword is what saves them, a chip is removed by the remove button inside the chip's own shadow root
(removing the element from the page leaves the keyword in the form), and an unfinished word left in the input
glues itself to the next keyword, so the input is emptied first; the logo goes to the "1:1 App tile icon" input (the
file input whose card text says 300 x 300). The page's Save, and the Submission options' Save, land on the product's
overview when they succeed, so a saved page is reopened to check it. "Submit for certification" on the overview
needs every section Complete; it takes the submission to "In certification" at once and shows a satisfaction survey,
dismissed by its Cancel. A release's upload is section 5's five steps done with these commands; a script for them
follows once a second submission has shown that the pages hold still.

## 7. The support request for the API route (dropped on 2026-11-13)

This section held the words of a request to Partner Center's support to open the submission API to the account. The
route was removed on 2026-11-13 (section 2), so the request is no longer needed.

## 8. Reading the state, and when something fails

- Where things stand: Submission 1 (`AxKit-5.2.1.msix`, every section Complete: Free in all markets, Developer
  tools, the accessibility and generative AI declarations, 3+/Everyone, the English listing with six screenshots
  and their captions, seven keywords and the 300 pixel logo, publish as soon as certified, the `runFullTrust`
  reason) was submitted for certification on 2026-11-08 in the afternoon, Pacific time. The overview page
  (`https://partner.microsoft.com/en-us/dashboard/products/9PK01L2K5000/overview`) shows the step it is at:
  Pre-processing, Certification, Publishing; certification usually takes hours, up to three business days. When it
  is live: `winget install 9PK01L2K5000 -s msstore` on this computer beside the development copy, then the NVDA pass
  of the Store copy (`store-distribution.md`, phase 6, step 3).
- The last published and the pending submission, with the status and the Store's error and warning codes, are in
  Partner Center under the product's overview.
- A failed certification names the policy; the fix goes into the next release (or, for a listing text, into
  `store-listing.md` and the next submission).
- While a submission is in certification, a release made meanwhile waits: its package is uploaded as the next
  submission once the Store is free.

## 9. Security

No application, key or secret for Partner Center exists: the API route that would have needed them was removed on
2026-11-13 (section 2). The website automation of section 6 holds no
credential at all: it drives the maintainer's own signed-in browser on this computer, and only while the debugging
port of that window is open (section 6).
