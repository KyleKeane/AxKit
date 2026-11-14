# axKit in the Microsoft Store: MSIX, the Store and winget

A plan written on 2026-11-08, the day the Partner Center account was opened, from Microsoft's documentation as read
that day (section 9) and the repository at 5.1.1. Nothing of it is built yet. Section 4 lists the decisions that are
the maintainer's; section 7 lists what the documentation did not say and the first packaging test must answer. The
work follows the bundle's order: specification first, then infrastructure, then code, then the NVDA pass, then the
release. The code runs on a branch, `store`, and merges when the Store copy has passed its NVDA checks.

State on 2026-11-13: the package, the listing and the first publication are done, and every release's package is
uploaded in Partner Center's website by hand or through `tools/EdgeDrive` (`store-setup.md` sections 5 and 6). The
command-line route this plan describes (the tenant, the Microsoft Entra application, the Store Developer CLI, the
submission API; section 2, phase 1 steps 2 and 3, phase 7) does not exist: the account is an Individual account, to
which Partner Center opens none of it, and what was built for it was removed on 2026-11-13 at the maintainer's
decision. Those passages stay as the plan's record.

## 0. The short version (chosen 2026-11-08)

The maintainer found the full plan too much to take in at once and asked for the simplest way. It is this, and the
recommended option of every decision in section 4 is taken as decided. First, the maintainer reserves the name axKit in
Partner Center and reads off the three identity values of section 2 step 4 (ten minutes, the steps given one at a time).
Then I build the package, the listing texts and the screenshots and check them with NVDA (phases 2 to 5, nothing
needed from him). Then one submission by hand in the browser, step by step with me (phase 6, about an hour): upload
the file, paste the texts, answer the age questionnaire, submit. Then Microsoft certifies (days) and `winget install`
works. The tenant, the Entra application and its key (section 2 steps 1 to 3) were first deferred; on 2026-11-09 the
maintainer asked for the automated publishing as soon as possible, and a workflow and a script for the submission API
were built that day. His account, an Individual account, turned out to have no tenant and no Entra application to
offer (`store-setup.md` section 2), so the route was never switched on, and it was removed on 2026-11-13 at his
decision. Every release's package is uploaded in Partner Center's website (`store-setup.md` sections 5 and 6).

## 1. Why, in one paragraph

Today axKit is a zip with `install.ps1` and an updater of its own (AX-2, AX-3). Windows 11's Smart App Control blocks
a program file that carries no Authenticode signature, so every update is a risk on such a computer, and Azure
Artifact Signing was set up for that (`docs/axKit/code-signing.md`), its identity check still pending. The Microsoft
Store removes the problem at the root: the Store re-signs every package with Microsoft's own certificate after
certification, so a Store copy of axKit passes Smart App Control with no certificate of ours; the Store installs,
updates and uninstalls it; and Windows Package Manager's `msstore` source makes `winget install` the command-line
path, with no manifest of ours to keep. The price: axKit ships as an MSIX package, which changes how it is installed
and what it may write where (section 3), and the first publication goes through Partner Center's website by hand
(section 2).

## 2. What runs from the command line and what does not

As planned on 2026-11-08. The command-line side below was closed to the account and removed on 2026-11-13; what
holds is `store-setup.md` section 2, and every submission goes through the website (section 5 there).

Partner Center's programmatic side, the Microsoft Store submission API and the Microsoft Store Developer CLI
(`msstore`) built on it, authenticates only as a Microsoft Entra application: a tenant ID, a client ID and a key, with
the application given the Manager role in Partner Center. Neither accepts a personal Microsoft account's sign-in; the
CLI's documentation says so in as many words ("don't use your MSA"). The account axKit publishes from is a personal
Microsoft account, so it is the sign-in for Partner Center's website and nothing else: it never goes into a file, a
script or a session.

By hand, in Partner Center's website, signed in with that account, once (phase 1, about an hour) and once more for
the first submission (phase 6):

1. Associate a Microsoft Entra tenant with the Partner Center account: Account settings, Tenants, Associate
   Microsoft Entra ID. The tenant of the Azure signing work serves, or Partner Center creates one at no charge. The
   account's owner can do it; being the tenant's global administrator is not needed for the association.
2. Add a Microsoft Entra application: Account settings, User management, Microsoft Entra applications, Create (or
   Add an existing one), the Manager role, then Add new key. The key is shown once, on that page only.
3. Read off the Tenant ID and the Client ID from the application's page, and the Seller ID from Account settings
   (its exact place: section 7).
4. Reserve the product name: Apps and games, New product, MSIX or PWA app, name axKit. The CLI cannot create an app.
   Then read the package identity from the product's Product management, Product identity (View app identity
   details): Package/Identity/Name, Package/Identity/Publisher and PublisherDisplayName. These are not secrets; they
   go into the manifest, letter for letter (the Store compares them case-sensitively).
5. The first submission. The submission API cannot be used before one submission of the app exists in Partner Center
   with the age ratings questionnaire answered, and the CLI's documentation says the first publication must go
   through Partner Center. I prepare every text, image and the package (phase 5); the uploads, the questionnaire and
   the Submit button are clicks in the browser.
6. A submission changed in Partner Center can no longer be continued by the API, so after the first one, changes go
   through the command line only.

From the command line and from GitHub Actions, with the four values as secrets, everything else: building the
package, checking it, uploading and submitting each new version, reading its status, keeping the listing's texts and
images, and the winget path, which needs nothing from us once the listing is live.

Secrets: the key and the IDs are typed into `gh secret set <NAME>` (which prompts for the value and hides it) and into
`msstore reconfigure` (which prompts the same way), in the maintainer's own terminal, never into the chat (the token
of 5.1.0). The CLI keeps them in Windows' credential store.

## 3. The shape of the Store build

### 3.0 The product's identity (reserved 2026-11-08)

Package/Identity/Name `axKit.axKit`; Package/Identity/Publisher `CN=F1B55F68-5EAE-4338-B94B-73044EAE40DA`;
Package/Properties/PublisherDisplayName `axKit` (the account's public publisher name, so the Store shows axKit as the
publisher, not a person); Store ID `9PK01L2K5000`. Hence the Store page `ms-windows-store://pdp/?productid=9PK01L2K5000`,
the web page https://apps.microsoft.com/detail/9PK01L2K5000 and the install line `winget install 9PK01L2K5000 -s msstore`.
None of these is a secret; they are in the package, where everyone reads them.

### 3.1 One package

`AxKit.msix`, x64, made from a folder publish (every file of the self-contained app as a file; the package is the
single file now) by `makeappx pack /h SHA256` from the Windows SDK, which the GitHub runners have. Identity Name and
Publisher come from Partner Center; the version is `<Version>` of `AxKit.csproj` with `.0` appended, since the Store
reserves the fourth part and requires 0; PublisherDisplayName as Partner Center shows it. `TargetDeviceFamily
Windows.Desktop`, minimum `10.0.19041.0` (Windows 10 version 2004, May 2020: the `uap10` manifest namespace the
per-app parameters need; the unpackaged build keeps 1809, SPEC 5.4). Capabilities: `runFullTrust` alone, the one every
packaged desktop app declares. The package is uploaded unsigned; the Store signs it after certification. The manifest
is written by a script from `apps.json`, the list of apps `publish.ps1` already writes next to the exe for
`install.ps1`, so the apps are declared in one place (B-10).

### 3.2 Eight entries (the Store's limit, found 2026-11-09)

The first design gave every app its own `<Application>` and three hidden ones; the Store's package validation refused
it: at most eight entries per package, and a hidden entry (`AppListEntry="none"`) is a "headless app" that needs a
waiver. So the package has eight entries, the apps marked `StoreEntry` in `Dispatch.Apps` (axClaude, axDown, axFile,
axTerminal, axGIT, axTask, axStart, axClip in 5.2.1; axHelp in axGIT's place from the next package, the user's choice
of 2026-11-11, AX-13.2), each running `AxKit.exe`, named as today's Start menu entries (AX-2.2),
with its own icon as PNG assets (44 and 150, and the 50 by 50 store logo) made by `tools/make-icon.ps1`. The axClaude
entry is first, with the id `AxKit` and no parameters: it starts axClaude as a bare `AxKit.exe` does, and it carries
every app's console command and the file types of the six apps without an entry; the other seven carry their verb as
parameters. The six (axJSON, axMedia, axPreview, axProcess, axPass, axCalc) are reached by their command, by a file of
their kind, by the Window menu's New window and from axFile. SPEC AX-13.2 has the whole.

### 3.3 Extensions

- File types, per app, as `install.ps1` registers them today (axDown `.md`, `.markdown`, `.txt`; axJSON `.json`;
  axMedia the sound and video types; axPass `.kdbx`; from `apps.json`), so that Windows' Open with lists the app on
  them. A package declares a file type once (makeappx refuses a second, found 2026-11-08), so a type belongs to the
  first app of the list that has it, and axPreview, whose Open with says every file, declares none: Windows has no
  package form of "every file", and the files part of File Explorer's Open with axKit submenu (AX-2.3, which showed
  axDown and axPreview on any file) has none either. axPreview is reached from axFile (Shift+Enter), from a window's
  Show in submenu and from a shell (`axpreview <file>`).
- Folders: the submenu on a folder and a folder's background has no plain manifest equivalent. A packaged app's
  entries there (`desktop4:FileExplorerContextMenus`) need a COM server, an `IExplorerCommand` DLL, which this round
  does not build (section 8). In the Store build a folder is opened from axFile, from axStart's search, or from a
  shell with `axkit claude <folder>`.
- The console commands as app execution aliases on PATH, in place of the `.cmd` shims in `%USERPROFILE%\.local\bin`
  (AX-2.4): `axKit.exe` and one per app (`axDown.exe`), all on the first entry, since Windows gives the started
  process the command's own path as the first word of its command line (measured 2026-11-09:
  `"…\WindowsApps\axDown.exe"`), which `Dispatch.VerbOfProgramName` reads the app off; measured on 2026-11-08,
  `axDown.exe <file>` opens the file in axDown and `axKit.exe preview <file>` in axPreview.
- Startup tasks (`windows.startupTask`) for axStart and axClip, in place of the Run key values `SignInStart`
  writes: a packaged process's writes under HKCU go to a private hive, so a Run value written from inside the package
  would never run. Windows lists the tasks under Settings, Apps, Startup, where the user turns them on and off; the
  Store build's own Start with Windows item opens that page (`ms-settings:startupapps`) and says so
  (`HotkeyForm.StartWithWindowsItem`). The first design put each task on a hidden entry whose parameters were
  `start --hidden` (measured 2026-11-08: both apps started hidden from them); the Store allows no hidden entry, so
  since 2026-11-09 each task sits on its app's own entry with `<verb> --hidden` as the task's own parameters
  (`uap10:Parameters` on the extension, which makeappx's schema accepts); whether Windows passes them at sign-in is
  the Store copy's pass (section 7).

### 3.4 Virtualization, accepted

A packaged desktop app's new files and folders under AppData Local and Roaming are redirected to a private folder of
the package; its writes under HKCU are copied to a private hive; its package folder is read-only. Hence:

- axKit's own files (`AxKit.Core/AppPaths`: settings, logs, reports, the MCP key and audit) land in the private
  folder, which Windows removes with the package (policy 10.2.7, clean uninstall). Files that already exist under
  `%APPDATA%\AxKit` from an unpackaged install stay readable and modifiable from the package; new files are private.
  A development copy and the Store copy on one computer therefore share what existed before the Store copy's first
  start and diverge in new files. Acceptable; written into AX-5.
- The capability that turns virtualization off, `unvirtualizedResources`, is restricted and, in Microsoft's words,
  designed for certain desktop PC games published by Microsoft and its partners. The plan does not rely on it.
- The programs axKit starts behave as they do outside axKit, without any code for it. Measured on 2026-11-08 with a
  probe that Windows started as an entry of the package (section 7): a child it created by `CreateProcess`, by .NET's
  `Process.Start` and through the shell each ran with no package identity, and their writes under `%LOCALAPPDATA%`
  and `%APPDATA%` landed in the user's folders, not in the package's. That is Windows' default for the children of a
  packaged desktop app (the `PROC_THREAD_ATTRIBUTE_DESKTOP_APP_POLICY` attribute exists to change it, and is not
  needed). So the shells of axTerminal, Claude Code in axClaude, git, KeePassXC's tool, winget and the installers, the
  programs the user starts from axStart and axFile, and the browser all keep their own files where they always were.
  A child that runs a program file of the package keeps the identity (measured the same way), which is what New
  window and the AI tools' window start rely on.
- `Environment.ProcessPath` lies under `C:\Program Files\WindowsApps\<package>\` and changes with every update, so
  whatever records the exe's path for later use records the alias `%LOCALAPPDATA%\Microsoft\WindowsApps\axKit.exe`
  in the Store build (`PackageIdentity.ProgramPath`): the MCP command line written into Claude Code's settings
  (`AiToolsSetup.cs`). `SignInStart` writes nothing in that copy (3.3).

### 3.5 What the Store build turns off

A process with package identity (`GetCurrentPackageFullName` succeeds; one P/Invoke, listed in SPEC 5.4) makes no
update check, downloads nothing and never offers Update to; Check for updates says that the Microsoft Store keeps
axKit current and offers to open axKit's Store page (`ms-windows-store://pdp/?productid=<Store ID>`: a program start,
with its Outside call comment and a row in `tools/outside-calls.ps1`, B-17). The installer, the Run key, the File
Explorer registry keys, the shims and the Settings, Apps entry are not written: the package provides all of them. The
release key's check of the zip is not needed. Everything else is the same code at the same version; About names the
Store as where this copy came from.

### 3.6 The listing

Name axKit; category Developer tools; price Free; every market; visibility "Make this product available and
discoverable in the Microsoft Store", which the winget path needs. The description begins with the dependencies
(policy 10.2.4: a dependency on software that is not integrated is disclosed at the beginning of the description;
10.2.3 forbids offering to install software that does not enhance the product, and Git, Claude Code, KeePassXC and
PowerShell 7 are what axGIT, axClaude, axPass and axTerminal exist for, each offered from its maker only when it is
missing, AX-2.9), then `README.md`'s first paragraph, then one line per app under features, then a sentence that
axClaude shows the output of Claude Code, a live generative AI (policy 11.16 asks for the disclosure in the listing, a
note in the submission and a means to report content: the F4 report, AX-12). Privacy policy: `PRIVACY.md` on GitHub,
required for every desktop app (10.5.1). Website www.kylekeane.com; support contact the reports repository's issues
page. Screenshots: PNG, 1366 by 768 or larger, up to ten, one per app's window, made by `tools/AxAutomate` on its own
desktop (a screenshot step to add), captions under 200 characters. Store logo 300 by 300 (one page calls it
mandatory, another strongly recommended: provided). Age ratings: the IARC questionnaire answered truthfully; axTerminal
and axClaude reach the internet without a filter, so a rating above 3+ is likely and does not matter for a developer
tool. Additional licence terms: AGPL-3.0-only with its link. The texts live in `docs/axKit/store-listing.md`, the
source of truth the listing is pasted from.

### 3.7 winget

Once the listing is live, `winget search axKit -s msstore` shows the Store ID and `winget install <Store ID> -s
msstore` installs; the exact line goes into `README.md`, and the Store page's link into the updater's notice.
Microsoft's pages do not state the conditions under which a Store app appears in the `msstore` source; the current
catalogue suggests free, publicly listed apps. Checked on the day the listing goes live (section 7). A manifest in the
community repository `winget-pkgs` (`KyleKeane.axKit`) is a second, later path for computers where the Store source
is disabled (section 8).

### 3.8 The GitHub path

GitHub releases continue: the tag, the notes, the source, and the zip with `install.ps1` as the developer's and the
Store-less computer's path, with its updater unchanged (AX-3). The Store copy is the one offered to users: `README.md`
leads with winget and the Store. The code signing of the exe: decision 4.4.

## 4. Decisions for the maintainer

The recommendation came first in each; on 2026-11-08 the maintainer took the recommended option of every one
(section 0), and 4.4 alone stays open until the first Store release is checked.

- 4.1 One path for users. The Store (and winget) is the install path `README.md` offers; the zip stays as the
  developer's and the fallback path. Taken.
- 4.2 Virtualization accepted; the children run outside the package by Windows' default, measured (3.4), so neither
  the breakaway attribute nor `unvirtualizedResources` is needed. Settled by measurement.
- 4.3 The folder context menu is absent from the Store build for now (3.3). Taken; the COM server is section 8.
- 4.4 Azure Artifact Signing retired on 2026-11-09, before the first Store release was checked, on the user's
  decision ("we will use the Windows Store for signing"): the Store's signature is the one, and the zip's `AxKit.exe`
  stays unsigned, as it always was, its notes pointing here. Settled; `docs/axKit/code-signing.md` section 7 has what
  is deleted on Azure.
- 4.5 Start at sign-in in the Store build is Windows' own Startup page (3.3), built as `HotkeyForm.StartWithWindowsItem`.
  Taken.
- 4.6 The clicks in Partner Center are the maintainer's, with NVDA, from my step lists (phase 1 and 6). Taken.
- 4.7 The publisher display name is public on the listing; the account's is axKit (3.0), settled.

## 5. The steps, in order

State on 2026-11-08 night, branch `store`: phase 1's name and identity are done (3.0; the tenant, the Entra application
and the CLI wait, section 0); phases 2, 3 and 4 are built (SPEC AX-13 and its cross-references in AX-1, AX-2, AX-3,
AX-5 and 5.4; `publish.ps1 -Msix` with its three tools; `PackageIdentity`, `StoreListing`, `Dispatch.WithApplicationVerb`,
the Store copy's Check for updates, About and Start with Windows; `build.yml` builds the package on every push and
`release.yml` attaches it to the draft release); phase 5 is `store-listing.md`, its logo
`publish\msix\StoreLogo-300.png` and six screenshots in `publish\store` (`tools/store-screenshots.ps1`, retaken from a
demo folder under the Public profile after the first set showed the maintainer's folder path; axTerminal's and
axGIT's come with `-WithAccessLayers` once he has connected them here, axClaude's by hand); phase 6
waits for the maintainer (his steps are `store-setup.md` section 3); phase 7 was built on 2026-11-09 (a workflow on
a published release behind a repository variable, a script against the submission API and a script for the
maintainer's copy of the credentials), never switched on, and removed on 2026-11-13 (phase 7 below). The
registration and probe tests of section 7 passed; the NVDA pass of the Store copy comes with phase 6.

### Phase 1: Partner Center (the maintainer, in the browser, about an hour; can start now)

1. Check that the account's identity verification is complete and the account is in good standing (Account
   settings); nothing publishes before it is.
2. Steps 1 to 4 of section 2: the tenant, the Entra application with the Manager role and its key, the Tenant ID,
   Client ID and Seller ID, the name axKit, the package identity values.
3. In his own terminal: `winget install "Microsoft Store Developer CLI"` (it needs the .NET 9 Desktop Runtime,
   `winget install Microsoft.DotNet.DesktopRuntime.9`; a development tool, recorded in
   `docs/axKit/development-environment.md`), then `msstore reconfigure`, answering its prompts, then `msstore apps
   list`, which must show axKit. Then `gh secret set` for the four values (names in phase 7).
4. The package identity values go to me in a message or a file: they are public.

Gate: `msstore apps list` shows axKit.

### Phase 2: Specification (me, one session)

`docs/axKit/SPEC.md`: AX-2 gains the Store installation (the package, its Applications, extensions and what it
replaces, 3.1 to 3.3), AX-3 the packaged build's update behaviour (3.5), AX-4 the submission in the release (phase 7),
AX-5 the private AppData of the packaged copy (3.4), 5.4 the two new APIs (`GetCurrentPackageFullName`, the desktop
app policy attribute), B-17 the Store page start. `PRIVACY.md`, `README.md`, `docs/axKit/README.md` and `SECURITY.md`
name winget and the Store and say that the Store copy never downloads an update itself. `CHANGELOG.md` gets its
section. `docs/axKit/development-environment.md` gains the Windows SDK's `makeappx` (its path), Developer Mode for
the loose registration, and the CLI with its runtime. `docs/axKit/plan.md` points here. One commit per document.

### Phase 3: Packaging infrastructure (me, one session, branch `store`)

1. `tools/make-manifest.ps1`: `apps.json` in, `AppxManifest.xml` out, with the identity values as parameters and the
   version from the csproj. A test compares the manifest's Applications with `Dispatch.Apps`.
2. `tools/make-icon.ps1 -Png`: the package assets per app.
3. `publish.ps1 -Msix`: the folder publish, the manifest, the assets, `makeappx pack`, into `publish\AxKit.msix`.
4. The development loop: `Add-AppxPackage -Register publish\win-x64-msix\AppxManifest.xml` registers the loose
   folder as the package without a signature (Developer Mode on). The first packaging test runs every item of section
   7 and records the answers there.
5. `appcert.exe` (the Windows App Certification Kit, deprecated but still running) on the package, once, for the
   Store's own kind of check.
6. `build.yml` builds the package on every push and keeps it as an artifact, with placeholder identity values.

Gate: the loose package starts every app from the Start menu, and section 7 has its answers.

### Phase 4: Code (me, one to two sessions, branch `store`)

1. `AxKit.Core/Packaged.cs`: the detection, false in tests and in `run.ps1` builds.
2. The updater off and the Store page on (3.5); About names the Store.
3. `SignInStart`: nothing written when packaged; the setting opens the Startup page (4.5).
4. The alias path wherever the exe's path is recorded (3.4).
5. The breakaway attribute in `PtyHost` and a `ProcessStarter` for the other children, with the same Outside call
   comments the starts carry today.
6. The accessibility review skill on every changed text, `test.ps1`, one commit per step.

### Phase 5: Listing materials (me, half a session)

`docs/axKit/store-listing.md` with every text of 3.6; the screenshots and the store logo into `publish\store\`
(ignored by git except the page).

### Phase 6: The first submission (step 1 done 2026-11-08, by tools/EdgeDrive; step 2 done by 2026-11-10; step 3 open)

1. Done: Submission 1 holds `AxKit-5.2.1.msix` (the 5.2.1 release's package from GitHub), the texts of
   `store-listing.md`, the six screenshots with captions and the 300 pixel logo, the properties and declarations
   (3.6; the `runFullTrust` reason on the Submission options page, the live generative AI in the notes for
   certification), the age ratings (3+), and was submitted for certification on 2026-11-08 (`store-setup.md`
   sections 3 and 8). The maintainer signed in; the rest was driven through `tools/EdgeDrive` (store-setup.md
   section 6), since the account has no API route (section 2 there).
2. Done: certified and published by 2026-11-10 (`winget show --id 9PK01L2K5000 -s msstore` finds the listing). Certification usually takes hours, up to three business days (the overview page says so); a rejection comes with
   the policy number, I fix the text or the package and the resubmission goes the same way.
3. Live: on this computer, `winget install <Store ID> -s msstore` next to the development copy; the NVDA pass of
   every app's plan on the Store copy, plus the Store items (Start menu entries, Windows search, the alias, the
   startup tasks after a sign-in, the file verbs, Check for updates, where the settings landed), recorded in
   `docs/axKit/nvda-test-record.md`. Then `store` merges to `main`.

### Phase 7: The release process (built 2026-11-09; the API route removed 2026-11-13)

`release.yml` builds the package beside the zip and attaches it to the draft; `release.ps1` publishes as before; the
release's attested `AxKit-<version>.msix` is then uploaded in Partner Center's website as a new submission, by hand
or through `tools/EdgeDrive` (`store-setup.md` sections 5 and 6). `docs/axKit/store-setup.md` records the account
and the runbook as `reports-setup.md` does for the reports; this page keeps the plan and its decisions.

The decision of 2026-11-09 was a plain script against the submission API over the Store Developer CLI (the CLI needs
a .NET 9 runtime of its own, and its documentation leaves open whether it takes a package built outside its project
types), run by a workflow on a published release behind a repository variable, with a Microsoft Entra application's
three values as secrets of the `release` environment. It was checked without the network only and never switched
on, since Partner Center offers an Individual account neither the tenant nor the application
(`store-setup.md` section 2). On 2026-11-13 the maintainer decided to give the route up for good, and the workflow
and its two scripts were removed.

### Phase 8: Later

arm64 in a bundle with x64; the community winget manifest; the folder context menu's COM server; retiring
`install.ps1` once the Store path has carried a few releases.

## 6. Risks and what is done about them

- The name axKit is taken in the Store: reserved first, in phase 1; the fallback name is decided then.
- The account's verification is incomplete and a submission cannot be published: checked first in phase 1.
- Certification rejects on a policy: the usual ones are the privacy policy, the dependency sentence, the AI
  disclosure and the restricted capability note, all covered in 3.6; each resubmission costs days, so the first
  submission is checked against the policy list before it goes.
- A manifest feature does not do what the plan assumes (the parameters, the startup task's arguments, the alias):
  section 7 answers each in the loose package before any code depends on it, and 3.3 names the fallback of each.
- A child program misbehaves inside the package: the breakaway attribute is tested on every kind of child in
  section 7; the last resort is the restricted capability of 4.2.
- Two copies on the development computer: a development axMedia and a Store axMedia both answer an `AppMessage`
  hand-over. Acceptable on that computer only; the plans test one copy at a time.
- The Store copy needs Windows 10 version 2004; the zip keeps 1809. Stated in the listing and in SPEC 5.4.
- The CLI needs a .NET 9 runtime on the development computer, outside the bundle's zero-dependency rule, which is
  about axKit's own code. Settled: the CLI is not used (phase 7).

## 7. To verify (the documentation does not say)

Answered in phase 1 (items 1 and 9) and in the first packaging test of phase 3 (the rest); the answers are written
here when known.

Known on 2026-11-08 evening, from building the package (`publish.ps1 -Msix`, 46.6 MB, the manifest accepted by
makeappx's schema check): `uap10:Parameters` on an Application element passes the schema (its behaviour is item 3);
a startup task carries no arguments of its own, so each app that starts at sign-in has a hidden entry
("AxStartSignIn", `AppListEntry="none"`) with the task, and the program reads the entry's id when Windows passes
nothing (`Dispatch.WithApplicationVerb`), which also settles item 5 whatever Windows does with an alias's parameters;
a file type is declared once per package (3.3).

The registration test of the same evening (the loose folder registered with Developer Mode on, the user away; the
package removed again afterwards, cleanly: no Start entry, no alias left) answered items 3 to 7 and 11:

- Windows lists fourteen Start entries, "axKit axCalc" to "axKit axTerminal, access layer for your shell", with the
  hidden entries absent; the AxCalc entry starts axCalc from the package (item 3).
- The aliases `axKit.exe`, `axDown.exe` and the rest are on the PATH under `%LOCALAPPDATA%\Microsoft\WindowsApps`;
  `axDown.exe <file>` opened the file in axDown and `axKit.exe preview <file>` in axPreview (item 5).
- Every declared file type carries the package's progid under its Open with list (`.md`, `.txt`, `.markdown` axDown's,
  `.json` axJSON's, `.mp3` axMedia's, `.kdbx` axPass's); the menu itself is for the NVDA pass (item 6).
- The hidden sign-in entries start their apps hidden with `--hidden` (axClip ran so; axStart handed over to the
  development copy already running and ended, which is its single-instance rule). Whether Windows lists the two tasks
  under Settings, Apps, Startup, and runs them at sign-in: the Store copy's pass (item 4).
- A process the test itself placed inside the package (`Invoke-CommandInDesktopPackage`) was virtualized: its writes
  under `%LOCALAPPDATA%` and `%APPDATA%` landed in the package's `LocalCache`. That was the wrong question, answered
  by the probe below. The apps themselves wrote their logs into the user's existing `%LOCALAPPDATA%\AxKit\logs` files
  in place, as 3.4 expects of existing files.

The probe of the same night (a small windowless program, `BreakawayProbe`, added as a hidden entry to a copy of the
manifest and started as Windows starts an entry, `shell:AppsFolder`, then removed with the package) answered items 3
and 7 for good: the probe ran with the package's identity and received the entry's `uap10:Parameters` as its command
line; the children it created by `CreateProcess`, by `Process.Start` and through the shell each had no package
identity (`GetPackageFullName` empty) and wrote their marker files into the user's AppData folders; a child that ran a
program file of the package kept the identity. So no breakaway code is needed, and the hidden entries' parameters
reach the apps (item 7 and item 3 done; 3.4 says so).
- Package detection and the entry's id gave the right app in every start, whether Windows passed the entry's
  parameters or not; the two cannot be told apart from outside and need not be (item 11).

1. Where Partner Center shows the Seller ID (expected under Account settings, with the account identifiers).
2. `msstore publish --inputFile AxKit.msix --appId <id>` accepts a package not built from one of the CLI's project
   types (the option is documented without a restriction; every example passes a project folder).
3. Several Application elements on one executable with different `uap10:Parameters`: each starts its app from the
   Start menu, Windows search finds them by the start of each word as today, Open with lists them.
4. A startup task with arguments (`start --hidden`), and whether `Enabled="true"` starts it at the next sign-in once
   the app has run.
5. An execution alias on an Application with parameters: whether `axDown.exe notes.md` reaches `AxKit.exe down
   notes.md`.
6. File type verbs with `%1` appear in the classic menu (Shift+F10, Show more options) and in Windows 11's menu.
7. Children break away: in axTerminal's PowerShell a file written under `$env:LOCALAPPDATA` appears in the real
   folder and the history is the user's own; Claude Code in axClaude sees its own settings; git and KeePassXC's tool
   work in axGIT and axPass; Install it now's winget works.
8. Settings across the development copy and the Store copy behave as 3.4 says.
9. The CLI's GitHub action exists and is pinned by commit; otherwise the runner installs the CLI by winget.
10. axKit appears in winget's `msstore` source within a day of going live, and the exact install line.
11. `GetCurrentPackageFullName` returns the package full name in the loose package and error 15700 unpackaged; the
    Store page protocol opens the listing.
12. The App Certification Kit passes on the self-contained .NET publish.

The first submission of 2026-11-09, driven through `tools/EdgeDrive` (store-setup.md section 6), answered the rest
and added three findings: the Store's package validation allows eight entries per package and no hidden one (3.2;
the first package, with seventeen, was refused, and 5.2.1 carries the eight); Windows gives a process started
through a console command the command's own path as its command line's first word, which .NET's
`GetCommandLineArgs` hides, so `PackageIdentity.CommandLineProgram` reads `GetCommandLineW` (measured: a bare
`axCalc.exe` opened axClaude until then, axCalc after); and a Start menu entry starts its app in Windows' System32
folder, which axClaude took for the project until FR-8.1 learned that Windows' own folders are never one (the
measurement left System32 as the maintainer's remembered folder for an hour; put right by hand). Item 12, the App
Certification Kit, was not run: the Store's own validation is the check that counts, and it ran. Two more from the
listing and the submission (2026-11-08): the Submission options page asks for the reason behind `runFullTrust` in a
box of its own (Restricted capabilities, 500 characters) and stays Incomplete without it, so the sentence is in
`store-listing.md`; and Edge treats every page as hidden while the computer's session is locked, which made the
website's uploads, deletes and saves land minutes late and several at once until Edge was started with occlusion
tracking and background throttling off (the switches in store-setup.md section 6).

## 8. Not in this round

The folder context menu's COM server (`desktop4:FileExplorerContextMenus` with an `IExplorerCommand` server, in C#
through NativeAOT or in C++, either a new kind of project), arm64, the community winget manifest, a Store copy with
`unvirtualizedResources`, and any paid listing.

## 9. Sources, read 2026-11-08

- Microsoft Store Developer CLI: https://learn.microsoft.com/windows/apps/publish/msstore-dev-cli/overview and
  https://learn.microsoft.com/windows/apps/publish/msstore-dev-cli/commands
- The submission API: https://learn.microsoft.com/windows/uwp/monetize/create-and-manage-submissions-using-windows-store-services
- Tenants and applications in Partner Center:
  https://learn.microsoft.com/windows/apps/publish/partner-center/associate-existing-azure-ad-tenant-with-partner-center-account
  and https://learn.microsoft.com/windows/apps/publish/partner-center/manage-azure-ad-applications-in-partner-center
- Packaging a desktop app by hand: https://learn.microsoft.com/windows/msix/desktop/desktop-to-uwp-manual-conversion,
  https://learn.microsoft.com/windows/msix/desktop/desktop-to-uwp-behind-the-scenes,
  https://learn.microsoft.com/windows/msix/package/create-app-package-with-makeappx-tool,
  https://learn.microsoft.com/uwp/schemas/appxpackage/uapmanifestschema/element-application,
  https://learn.microsoft.com/windows/apps/package-and-deploy/app-capability-declarations
- Package requirements of the Store: https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/app-package-requirements
- The submission and its listing: https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/create-app-submission,
  https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/screenshots-and-images,
  https://learn.microsoft.com/windows/apps/publish/publish-your-app/msix/age-ratings
- Store policies, version 7.20 of 2026-09-15: https://learn.microsoft.com/windows/apps/publish/store-policies
- The developer account: https://learn.microsoft.com/windows/apps/publish/partner-center/open-a-developer-account
- winget: https://learn.microsoft.com/windows/package-manager/winget/install and
  https://learn.microsoft.com/windows/package-manager/winget/source
- The desktop app policy attribute: `UpdateProcThreadAttribute`,
  https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute
