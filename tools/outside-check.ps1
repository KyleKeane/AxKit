<#
.SYNOPSIS
  Holds the work to the maintainer's approvals of outside reports (docs/axKit/outside-reports.md OR-7.1): no item of
  the priorities list is worked on from a report without his signed approval, and every signature verifies.

.DESCRIPTION
  Reads the outside reports list (<parent>\axkit-management\outside-reports) and the priorities list beside it, and
  verifies with the public key pinned in this repository (docs\axKit\approval-key.pub), never with a key beside the
  repository. Asks no network. Names, one line each:

    unapproved   an item not done whose Where it is written names a report, with no approved entry of that report
                 naming the item by its title (it was written around the process, or its entry changed after the
                 approval and no longer verifies)
    signature    an entry Approved whose approval does not verify against the pinned key (an approval in force is
                 the pinned key's own); an entry Declined or Done that holds no approval of the pinned key or of a
                 key pinned before it (git's history of the pinned file): closed without him, or changed after
    pin          the key beside the lists (approval-key.json, the one his passphrase opens) is not the pinned one:
                 a new key or a change waits for tools\pin-approval-key.ps1

  Its one line when nothing is named says the pinned key's fingerprint, and names every commit that replaced a
  pinned key without the old key's signature (OR-6.3), which stays said and refuses nothing.

  The pre-push hook runs it and refuses the push when it names anything; tools\session-start.ps1 says its result.
  Where the lists are not beside the repository (another person's clone, the build service), it says nothing and
  exits 0. Exit code 0 when nothing is named, 1 when something is. The script stays ASCII.

.EXAMPLE
  .\tools\outside-check.ps1
  .\tools\outside-check.ps1 -Management C:\temp\management -Repository C:\temp\repo     for the tests
#>
[CmdletBinding()]
param(
    # The folder that holds the lists (priorities, outside-reports); <parent>\axkit-management by default.
    [string]$Management,
    # The code repository whose docs\axKit\approval-key.pub is pinned; this script's own by default.
    [string]$Repository
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'approvals.ps1')

if (-not $Repository) { $Repository = Split-Path $PSScriptRoot -Parent }
if (-not $Management) { . (Join-Path $PSScriptRoot 'siblings.ps1'); $Management = Get-BesideFolder 'axkit-management' }
$priorities = Join-Path $Management 'priorities'
if (-not (Test-Path -LiteralPath $priorities)) {
    exit 0
}

$key = Get-PinnedApprovalKey $Repository
# The keys pinned before this one, from git's history: an entry closed under one of them is still his (OR-6.5).
$history = @(Get-ApprovalKeyHistory $Repository)
$former = @($history | ForEach-Object { $_.Key } | Where-Object { $null -eq $key -or $_.Fingerprint -ne $key.Fingerprint })
$entries = @(Get-OutsideEntries (Join-Path $Management 'outside-reports') $key $former)

$lines = @()

# The key beside the lists is the one his passphrase opens; the pinned key is the one the checks trust. They are the
# same key, or a change waits to be pinned (OR-6.6).
$beside = Get-ApprovalKeyBesideLists $Management
if ($null -ne $beside -and ($null -eq $key -or $key.Fingerprint -ne $beside.Fingerprint)) {
    $now = if ($null -eq $key) { 'no key is pinned' } else { "the pinned key is $(Get-SpokenFingerprint $key.Fingerprint)" }
    $lines += "Outside reports: pin: the approval key beside the lists is $(Get-SpokenFingerprint $beside.Fingerprint) and ${now}: pin it with tools\pin-approval-key.ps1."
}

foreach ($entry in $entries) {
    $name = [IO.Path]::GetFileNameWithoutExtension($entry.Path)
    if ($entry.State -eq 'Approved' -and -not $entry.Valid) {
        $why = if ($null -eq $key) { 'no approval key is pinned (docs\axKit\approval-key.pub)' }
               elseif ($entry.ValidEver) { 'it was signed with a key pinned before this one: tools\outside-report.ps1 reopen offers it to the maintainer again' }
               else { 'it was changed after the maintainer signed it, or signed with a key never pinned' }
        $lines += "Outside reports: signature: '$name' is Approved and its approval does not verify: $why."
    } elseif ($entry.State -in @('Declined', 'Done') -and -not $entry.ValidEver) {
        $lines += "Outside reports: signature: '$name' is $($entry.State) and holds no approval of a key this repository pinned: it was closed without the maintainer, or changed after he signed it."
    }
}
foreach ($one in @(Get-UnapprovedOutsideItems $priorities $entries)) {
    $lines += "Outside reports: unapproved: the item '$one' comes from outside and has no approval of the maintainer that names it."
}

if ($lines.Count -eq 0) {
    $pinned = if ($null -eq $key) { 'no approval key pinned yet' } else { "approval key $(Get-SpokenFingerprint $key.Fingerprint)" }
    # Said every time, never a refusal: a key that replaced another without the old key's signature (a passphrase
    # forgotten and set up anew by the maintainer's own commit, OR-6.3, or a change nobody signed).
    $unsigned = @($history | Where-Object { -not $_.Signed })
    if ($unsigned.Count -gt 0) {
        $pinned += "; replaced without the old key's signature in commit $(($unsigned | ForEach-Object { $_.Commit }) -join ', '): the maintainer knows why, or is told"
    }
    Write-Host "Outside reports: no item from outside without an approval ($($entries.Count) entries; $pinned)."
    exit 0
}
$lines | ForEach-Object { Write-Host $_ }
Write-Host 'Nothing from outside is worked on without the maintainer''s signed approval (docs/axKit/outside-reports.md).'
exit 1
