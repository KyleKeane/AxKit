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
    signature    an entry Approved, Declined or Done whose approval does not verify against the pinned key
    approved?    an entry whose State is Approved or Declined while no key is pinned

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
if (-not $Management) { $Management = Join-Path (Split-Path $Repository -Parent) 'axkit-management' }
$priorities = Join-Path $Management 'priorities'
if (-not (Test-Path -LiteralPath $priorities)) {
    exit 0
}

$key = Get-PinnedApprovalKey $Repository
$entries = @(Get-OutsideEntries (Join-Path $Management 'outside-reports') $key)

$lines = @()
foreach ($entry in $entries) {
    $name = [IO.Path]::GetFileNameWithoutExtension($entry.Path)
    if ($entry.State -in @('Approved', 'Declined') -and $null -eq $key) {
        $lines += "Outside reports: approved?: '$name' is $($entry.State) and no approval key is pinned (docs\axKit\approval-key.pub)."
    } elseif (($entry.State -in @('Approved', 'Declined') -or ($entry.State -eq 'Done' -and $entry.Fields['Approval'])) -and -not $entry.Valid) {
        $lines += "Outside reports: signature: '$name' is $($entry.State) and its approval does not verify: it was changed after the maintainer signed it, or signed with another key."
    }
}
foreach ($one in @(Get-UnapprovedOutsideItems $priorities $entries)) {
    $lines += "Outside reports: unapproved: the item '$one' comes from outside and has no approval of the maintainer that names it."
}

if ($lines.Count -eq 0) {
    $pinned = if ($null -eq $key) { 'no approval key pinned yet' } else { "approval key $(($key.Fingerprint -split '(.{4})' | Where-Object { $_ }) -join ' ')" }
    Write-Host "Outside reports: no item from outside without an approval ($($entries.Count) entries; $pinned)."
    exit 0
}
$lines | ForEach-Object { Write-Host $_ }
Write-Host 'Nothing from outside is worked on without the maintainer''s signed approval (docs/axKit/outside-reports.md).'
exit 1
