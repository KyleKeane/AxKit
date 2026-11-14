<#
.SYNOPSIS
  Signs a draft release with the release key and publishes it (docs/axKit/SPEC.md AX-3.4; security audit S-27).

.DESCRIPTION
  release.ps1 calls it after pushing the tag; it can be run alone for a draft already on GitHub. It
    1. waits for the tag's release workflow to finish (it leaves the release as a draft with the zip and install.cmd),
    2. downloads the draft's zip into a fresh folder and checks the build provenance GitHub attested for it (that this
       repository's release workflow built it),
    3. signs the zip's SHA-256 with the release key (tools/release-key.ps1), which lives only on this machine,
       checks the signature against the public half the apps hold, and uploads "<zip>.sig" to the draft,
    4. publishes the release. Only then do the apps see it, and they install it only when the signature checks.

  -Rehearse signs and publishes nothing: it asks GitHub the questions of step 1 and about the release and its files,
  through the same functions as the real run, about the newest published release (or the given version's), and ends
  before the download. release.ps1 -Rehearse calls it. Exit code 0 when every question was answered sensibly.

.EXAMPLE
  .\tools\sign-release.ps1 3.7.0
  .\tools\sign-release.ps1 -Rehearse
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Version,

    [switch]$Rehearse
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$repo = 'KyleKeane/AxKit'

# ---- The questions to GitHub, each one a function that the real run and the rehearsal both call. None changes
# ---- anything.

# One question: gh with these words, each word one argument. The answer is what gh wrote, trimmed; the command as
# text and gh's exit code stay in $script:asked and $script:askedExit. Every --jq filter is one plain word, with no
# space and no double quote in it: Windows PowerShell 5.1 hands a filter with a quoted space to gh as two arguments
# (the 5.8.0 release, 2026-11-14; release.ps1 has the same function, and ReleaseScriptTests reads both scripts).
function Get-GitHubAnswer([string[]]$words) {
    $script:asked = 'gh ' + ($words -join ' ')
    $lines = @(gh @words)
    $script:askedExit = $LASTEXITCODE
    return ($lines -join "`n").Trim()
}

# The run of release.yml that a tag started: its number, or nothing (or 'null') while GitHub has none yet.
function Find-ReleaseRun([string]$ofTag) {
    return Get-GitHubAnswer @('run', 'list', '--repo', $repo, '--workflow', 'release.yml', '--branch', $ofTag, '--limit', '1', '--json', 'databaseId', '--jq', '.[0].databaseId')
}

# Follows a run to its end; the exit code says whether it succeeded. A run that has ended answers at once.
function Watch-Run([string]$ofRun) {
    $script:asked = "gh run watch $ofRun --repo $repo --exit-status"
    gh run watch $ofRun --repo $repo --exit-status
    $script:askedExit = $LASTEXITCODE
}

# 'true' for a draft, 'false' for a published release, nothing when the tag has no release.
function Get-ReleaseIsDraft([string]$ofTag) {
    return Get-GitHubAnswer @('release', 'view', $ofTag, '--repo', $repo, '--json', 'isDraft', '--jq', '.isDraft')
}

# The names of the release's files, one per line.
function Get-ReleaseFiles([string]$ofTag) {
    return Get-GitHubAnswer @('release', 'view', $ofTag, '--repo', $repo, '--json', 'assets', '--jq', '.assets[].name')
}

# ---- The rehearsal: the questions above about a release that is already published, and nothing else. It always
# ---- ends in an exit, above the first statement that downloads, signs, uploads or publishes.
if ($Rehearse) {
    $failed = 0
    function Write-Answer([string]$what, [string]$answer, [bool]$sensible) {
        if ($script:askedExit -eq 0 -and $sensible) {
            Write-Host "Answered: ${what}: $($answer -replace "`n", ', ')"
            Write-Host "  $script:asked"
        } else {
            $script:failed++
            Write-Host "FAILED: ${what}."
            Write-Host "  Command: $script:asked"
            Write-Host "  Exit code: $script:askedExit"
            Write-Host "  Output: '$answer'"
        }
    }

    try {
        if ($Version) {
            $rehearsedTag = "v$Version"
        } else {
            # The rehearsal's own question: which release to ask about.
            $rehearsedTag = Get-GitHubAnswer @('release', 'list', '--repo', $repo, '--exclude-drafts', '--exclude-pre-releases', '--limit', '1', '--json', 'tagName', '--jq', '.[0].tagName')
            Write-Answer 'the newest published release' $rehearsedTag ($rehearsedTag -match '^v\d+\.\d+\.\d+$')
        }
        if ($rehearsedTag -match '^v\d+\.\d+\.\d+$') {
            $id = Find-ReleaseRun $rehearsedTag
            Write-Answer "the release workflow's run for $rehearsedTag" $id ($id -match '^\d+$')
            if ($id -match '^\d+$') {
                Watch-Run $id
                Write-Answer "the end of run $id (gh's own lines are above)" 'it succeeded' $true
            }
            $draft = Get-ReleaseIsDraft $rehearsedTag
            Write-Answer "whether $rehearsedTag is a draft" $draft ($draft -eq 'true' -or $draft -eq 'false')
            $files = Get-ReleaseFiles $rehearsedTag
            $zipName = "AxKit-$($rehearsedTag.Substring(1))-win-x64.zip"
            Write-Answer "the files of $rehearsedTag (with $zipName among them)" $files (($files -split "`n") -contains $zipName)
        }
    } catch {
        $failed++
        Write-Host "FAILED: the rehearsal of sign-release.ps1 itself stopped: $($_.Exception.Message)"
    }
    Write-Host 'sign-release.ps1: nothing was downloaded, signed, uploaded or published.'
    if ($failed -gt 0) { exit 1 }
    exit 0
}
# ---- End of the rehearsal. Everything below signs and publishes.

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "The version must be three numbers, like 1.0.1 (got '$Version')." }
$tag = "v$Version"

Write-Host "Waiting for the release workflow of $tag..."
$run = $null
for ($try = 0; -not $run -and $try -lt 24; $try++) {
    $id = Find-ReleaseRun $tag
    if ($id -and $id -ne 'null') { $run = $id } else { Start-Sleep -Seconds 5 }
}
if (-not $run) { throw "No release workflow for $tag appeared within two minutes." }
Watch-Run $run
if ($LASTEXITCODE -ne 0) { throw "The release workflow of $tag failed; nothing was signed or published." }

$draft = Get-ReleaseIsDraft $tag
if (-not $draft) { throw "No release for $tag was found on GitHub: look at the draft's tag name there (gh release list)." }
if ($draft -ne 'true') { throw "Release $tag is not a draft: it was published without its signature. Look at it on GitHub before anything else." }

$name = "AxKit-$Version-win-x64.zip"
if (((Get-ReleaseFiles $tag) -split "`n") -notcontains $name) { throw "The draft of $tag does not hold $name; nothing was signed or published." }

$folder = Join-Path $env:TEMP ("axkit-sign-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $folder | Out-Null
try {
    gh release download $tag --repo $repo --pattern $name --dir $folder
    if ($LASTEXITCODE -ne 0) { throw "The draft's $name could not be downloaded." }
    $zip = Join-Path $folder $name

    gh attestation verify $zip --repo $repo | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "GitHub has no provenance from this repository's release workflow for $name; nothing was signed." }

    & (Join-Path $PSScriptRoot 'release-key.ps1') sign $zip | Out-Host
    & (Join-Path $PSScriptRoot 'release-key.ps1') verify $zip | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'The signature just made does not check against the public half; nothing was published.' }

    gh release upload $tag "$zip.sig" --repo $repo --clobber
    if ($LASTEXITCODE -ne 0) { throw 'The signature could not be uploaded.' }
    gh release edit $tag --repo $repo --draft=false
    if ($LASTEXITCODE -ne 0) { throw 'The release could not be published.' }
    Write-Host "Signed and published ${tag}: https://github.com/$repo/releases/tag/$tag"
}
finally {
    Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue
}
