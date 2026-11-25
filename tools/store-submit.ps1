<#
.SYNOPSIS
  Puts a published release into the Microsoft Store: the release's AxKit-<version>.msix uploaded as a new submission
  in Partner Center, its "What's new in this version" written, each page read back, and, with -Submit, the
  submission sent for certification. The steps are the four by-hand submissions of 2026-11-08 to 2026-11-18, as
  docs/axKit/store-setup.md sections 5 and 6 record them, done by tools/EdgeDrive and the page scripts of
  tools/EdgeDrive/store.

.DESCRIPTION
  Run after release.ps1 has published <version>, and only at the maintainer's word, as the release itself.
    1. The release's package is downloaded with gh into an empty folder of its own.
    2. EdgeDrive is built into a folder of its own, and Edge is started on its own profile with the switches of
       section 6 unless its debugging port already answers.
    3. The product's overview is loaded and "Store presence" waited for (reloaded once). A sign-in page stops the
       script: the maintainer signs in in that Edge window, with his own second factor, and runs it again.
    4. "Start update" when no update is in draft, then "In draft" waited for; submission-pages.js gives the
       submission's pages.
    5. Packages: drop-point.js, then the drop, then "will be removed" waited for (the page marks the older package
       itself once the new one has validated), then Save.
    6. The English listing: release-notes.template.js writes the text of -WhatsNew, the page's Save, the listing
       opened again and its text compared.
    7. With -Submit only: the overview, "Submit for certification", "In certification" waited for, the survey's
       Cancel. Without -Submit the draft is left ready and the script says so.
    8. The Edge window is closed (its debugging port with it) and one line says where the submission stands, for
       section 8 of store-setup.md.
  -Rehearse reads only: the package found on GitHub, the overview's state and the submission's pages, nothing pressed.
  Every step that does not find what it waits for stops the script with what it saw; nothing is retried blindly, since
  a second drop or a second Start update makes a mess the Store then refuses (section 6).

.EXAMPLE
  .\tools\store-submit.ps1 5.11.0 -Rehearse
  .\tools\store-submit.ps1 5.11.0 -WhatsNew C:\scratch\whats-new.txt
  .\tools\store-submit.ps1 5.11.0 -WhatsNew C:\scratch\whats-new.txt -Submit
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Version,
    [string]$WhatsNew,
    [switch]$Submit,
    [switch]$Rehearse
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$overview = 'https://partner.microsoft.com/en-us/dashboard/products/9PK01L2K5000/overview'
$pageScripts = Join-Path $PSScriptRoot 'EdgeDrive\store'

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "The version is three numbers, as release.ps1 takes it: $Version" }
if (-not $Rehearse) {
    if (-not $WhatsNew -or -not (Test-Path $WhatsNew)) { throw 'The text of "What''s new in this version" is a file named with -WhatsNew (1500 characters at most).' }
    $notes = [IO.File]::ReadAllText((Resolve-Path $WhatsNew)).Trim()
    if ($notes.Length -eq 0 -or $notes.Length -gt 1500) { throw "What's new holds $($notes.Length) characters; the field takes 1 to 1500." }
}

function Stop-Submission([string]$why) {
    Write-Host "Stopped: $why"
    Write-Host 'Nothing more was pressed. The Edge window stays open to read; close it with: EdgeDrive call Browser.close ''{}'''
    exit 1
}

# 1. The package, from the release's page.
$work = Join-Path ([IO.Path]::GetTempPath()) ("store-submit-" + $Version + "-" + [DateTime]::Now.ToString('HHmmss'))
New-Item -ItemType Directory -Path $work | Out-Null
# Outside call, a program: gh, the release's assets on GitHub.
& gh release download "v$Version" --repo KyleKeane/AxKit --pattern "AxKit-$Version.msix" --dir $work
if ($LASTEXITCODE -ne 0) { throw "The release v$Version has no AxKit-$Version.msix on GitHub, or gh could not reach it." }
$package = Join-Path $work "AxKit-$Version.msix"
Write-Host "Package: AxKit-$Version.msix, $([Math]::Round((Get-Item $package).Length / 1MB, 1)) MB"

# 2. EdgeDrive, and Edge with its port.
$drive = Join-Path $work 'edgedrive'
& dotnet build (Join-Path $PSScriptRoot 'EdgeDrive') -c Release -o $drive --nologo -v quiet | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'EdgeDrive did not build.' }
& dotnet build-server shutdown | Out-Null
$edgeDrive = Join-Path $drive 'EdgeDrive.exe'

function Invoke-Drive {
    $out = & $edgeDrive @args 2>&1 | Out-String
    return @{ Ok = ($LASTEXITCODE -eq 0); Text = $out.Trim() }
}

$portOpen = $true
try { Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:9222/json/version' -TimeoutSec 3 | Out-Null } catch { $portOpen = $false }
if (-not $portOpen) {
    $edge = @("${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe", "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $edge) { throw 'Microsoft Edge was not found.' }
    $profileDir = Join-Path $env:LOCALAPPDATA 'AxKit-release\edge-profile'
    # Outside call, a program: Microsoft Edge on its own profile, Partner Center, at the maintainer's word.
    Start-Process $edge -ArgumentList "--user-data-dir=`"$profileDir`"", '--remote-debugging-port=9222', '--disable-features=CalculateNativeWinOcclusion,IntensiveWakeUpThrottling', '--disable-background-timer-throttling', '--disable-renderer-backgrounding', '--disable-backgrounding-occluded-windows', $overview
    Start-Sleep -Seconds 5
}

$pages = Invoke-Drive targets
$target = ($pages.Text -split "`n" | Where-Object { $_ -match '^\s*([0-9A-F]{32})\s+page\s' } | Select-Object -First 1)
if (-not $target) { Stop-Submission 'Edge shows no page to drive.' }
Invoke-Drive use ($target.Trim() -split '\s+')[0] | Out-Null

# 3. The overview.
Invoke-Drive nav $overview | Out-Null
$where = (Invoke-Drive eval 'location.host').Text
if ($where -match 'login\.|live\.com|microsoftonline') { Stop-Submission 'Partner Center asks for a sign-in: sign in in the Edge window, then run this again.' }
if (-not (Invoke-Drive wait 'Store presence' 120).Ok) {
    Invoke-Drive nav $overview | Out-Null
    if (-not (Invoke-Drive wait 'Store presence' 120).Ok) { Stop-Submission 'The overview did not show "Store presence" in four minutes.' }
}

$state = (Invoke-Drive eval "(() => { const t = document.body.innerText; const m = t.match(/Product update[^\n]*/); return m ? m[0] : 'no update' })()").Text
Write-Host "Overview: $state"
if ($state -match 'In certification') { Stop-Submission 'A submission is in certification: this release waits until the Store is free (store-setup.md section 8).' }

if ($Rehearse) {
    if ($state -match 'In draft') { Write-Host (Invoke-Drive evalfile (Join-Path $pageScripts 'submission-pages.js')).Text }
    Write-Host "Rehearsed: the package and the overview are in reach; nothing was pressed."
    Invoke-Drive call Browser.close '{}' | Out-Null
    exit 0
}

# 4. The update in draft.
if ($state -notmatch 'In draft') {
    Invoke-Drive click 'Start update' | Out-Null
    if (-not (Invoke-Drive wait 'In draft' 420).Ok) { Stop-Submission '"Start update" did not make an update in draft in seven minutes.' }
}

$found = (Invoke-Drive evalfile (Join-Path $pageScripts 'submission-pages.js')).Text
$packagesPage = ([regex]::Match($found, 'packages: (\S+)')).Groups[1].Value
$listingPage = ([regex]::Match($found, 'listings \(English\): (\S+)')).Groups[1].Value
if (-not $packagesPage -or -not $listingPage) { Stop-Submission "The submission's pages were not found: $found" }
Write-Host (($found -split "`n")[0])

# 5. The package.
Invoke-Drive nav $packagesPage | Out-Null
Invoke-Drive front | Out-Null
$point = (Invoke-Drive evalfile (Join-Path $pageScripts 'drop-point.js')).Text
$at = [regex]::Match($point, '^(\d+) (\d+)$')
if (-not $at.Success) { Stop-Submission "No point to drop the package on: $point" }
Invoke-Drive dropxy $at.Groups[1].Value $at.Groups[2].Value $package | Out-Null
if (-not (Invoke-Drive wait 'will be removed' 600).Ok) { Stop-Submission 'The package did not validate, or the older one was not marked for removal, in ten minutes: read the Packages page.' }
Invoke-Drive click 'Save' | Out-Null
if (-not (Invoke-Drive wait 'Store presence' 180).Ok) { Stop-Submission 'The Packages page did not save.' }
Write-Host "Packages: AxKit-$Version.msix uploaded and saved."

# 6. What's new.
Invoke-Drive nav $listingPage | Out-Null
if (-not (Invoke-Drive wait "What's new" 120).Ok) { Stop-Submission 'The English listing did not open.' }
$script = [IO.File]::ReadAllText((Join-Path $pageScripts 'release-notes.template.js')).Replace('__TEXT__', (ConvertTo-Json -InputObject $notes -Compress))
$scriptFile = Join-Path $work 'release-notes.js'
[IO.File]::WriteAllText($scriptFile, $script, (New-Object System.Text.UTF8Encoding($false)))
$set = (Invoke-Drive evalfile $scriptFile).Text
if ($set -notmatch 'they are the same') { Stop-Submission "What's new was not set: $set" }
# The page's own Save button, whatever the text holds: find lists every element whose text holds the word.
$saves = (Invoke-Drive find 'Save').Text -split "`n"
$saveIndex = 0
for ($i = 0; $i -lt $saves.Count; $i++) { if ($saves[$i] -match '^\d+\. button: Save\s*$') { $saveIndex = $i + 1; break } }
if ($saveIndex -eq 0) { Stop-Submission 'The listing has no Save button to press.' }
Invoke-Drive click 'Save' $saveIndex | Out-Null
if (-not (Invoke-Drive wait 'Store presence' 180).Ok) { Stop-Submission 'The listing did not save.' }
Invoke-Drive nav $listingPage | Out-Null
Invoke-Drive wait "What's new" 120 | Out-Null
$kept = (Invoke-Drive eval "(() => { const all = []; const walk = (r) => { for (const e of r.querySelectorAll('*')) { all.push(e); if (e.shadowRoot) walk(e.shadowRoot); } }; walk(document); const f = all.find((e) => e.id === 'releaseNotes'); return f ? f.value : '' })()").Text
if ($kept.Trim() -ne $notes) { Stop-Submission "The listing's What's new reads otherwise after its Save ($($kept.Length) characters)." }
Write-Host "Listing: What's new saved, $($notes.Length) characters, read back the same."

# 7. Certification, only with -Submit.
Invoke-Drive nav $overview | Out-Null
Invoke-Drive wait 'Store presence' 180 | Out-Null
if (-not $Submit) {
    Invoke-Drive call Browser.close '{}' | Out-Null
    Write-Host "Ready: the update is in draft with AxKit-$Version.msix and its What's new. Run again with -Submit to send it for certification, or press Submit for certification on the overview."
    exit 0
}

Invoke-Drive click 'Submit for certification' | Out-Null
if (-not (Invoke-Drive wait 'In certification' 300).Ok) { Stop-Submission '"Submit for certification" did not take the update to In certification.' }
Invoke-Drive click 'Cancel' 1 | Out-Null
$final = (Invoke-Drive eval "(() => { const t = document.body.innerText; const m = t.match(/Product update[^\n]*/); return m ? m[0] : '' })()").Text
Invoke-Drive call Browser.close '{}' | Out-Null

# 8. Where it stands.
Write-Host "Submitted: $final, AxKit-$Version.msix, $(Get-Date -Format 'yyyy-MM-dd'). For store-setup.md section 8."
