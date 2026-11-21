<#
.SYNOPSIS
  Ties every commit to its item in the priorities list (CLAUDE.md rule 9), and does the list's bookkeeping from the
  commit message, so that an item is marked started and done by the commit that starts or finishes it and nobody has
  to remember to. git runs this for every commit through the commit-msg hook (tools/hooks/commit-msg).

.DESCRIPTION
  The commit message names its item on a line of its own:

      Item: <words of the item's title>          the commit works on that item
      Item done: <words of the item's title>     the commit finishes it
      Item: none, <the reason>                   upkeep that belongs to no item (a typo, a generated page)

  The words are matched against the items' titles without regard to case; they must name exactly one item. Several
  such lines may stand in one message. What the script then writes into the item's file, in the list
  <parent>\axkit-management\priorities beside this repository:

    Item:       the field Started gets today's date, when it is empty (made after Done when the item has none);
    Item done:  Started as above, and Done is checked.

  A commit without such a line, a line that names no item or more than one, and "none" without a reason are refused,
  with what to write. Where the lists are not beside the repository (another person's clone, the build service), the
  script says nothing and allows every commit: the lists are the maintainer's own. Merge commits, the commits git
  writes itself (a revert, a fixup) and the release script's own commit ("Release 1.2.3") are allowed as they are.

.EXAMPLE
  .\tools\item-of-commit.ps1 .git\COMMIT_EDITMSG
  .\tools\item-of-commit.ps1 message.txt -Lists C:\temp\priorities -Check     say what would happen, write nothing
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$MessageFile,
    [string]$Lists,
    [switch]$Check,
    # The code repository whose docs\axKit\approval-key.pub verifies an approval; this script's own by default (the tests' own).
    [string]$PinnedFrom
)

$ErrorActionPreference = 'Stop'

if (-not $Lists) {
    $repository = Split-Path $PSScriptRoot -Parent
    $Lists = Join-Path (Split-Path $repository -Parent) 'axkit-management\priorities'
}

# No lists beside the repository: not the maintainer's computer. Every commit is allowed.
if (-not (Test-Path -LiteralPath $Lists)) {
    exit 0
}

$lines = @(Get-Content -LiteralPath $MessageFile | Where-Object { $_ -notmatch '^\s*#' })
$subject = ($lines | Where-Object { $_.Trim().Length -gt 0 } | Select-Object -First 1)
if (-not $subject) {
    exit 0
}

# What git writes itself is allowed as it is, and the release script's own commit, "Release 1.2.3", which is the
# whole bundle's and no one item's.
if ($subject -match '^(Merge |Revert "|fixup! |squash! |amend! )' -or $subject -match '^Release \d+\.\d+\.\d+$') {
    exit 0
}

$named = @()
foreach ($line in $lines) {
    if ($line -match '^\s*Item( done)?:\s*(.+?)\s*$') {
        $named += [pscustomobject]@{ Done = [bool]$Matches[1]; Words = $Matches[2] }
    }
}

function Refuse([string]$why) {
    Write-Host ''
    Write-Host "Commit refused: $why"
    Write-Host 'Every commit names its item of the priorities list on a line of its own (CLAUDE.md rule 9):'
    Write-Host '    Item: <words of the item''s title>         works on it; the item is marked started'
    Write-Host '    Item done: <words of the item''s title>    finishes it; the item is marked done'
    Write-Host '    Item: none, <the reason>                  upkeep that belongs to no item'
    Write-Host "The items are the files of $Lists."
    exit 1
}

if ($named.Count -eq 0) {
    Refuse 'the message names no item.'
}

# The items, and how words name one: tools\items.ps1, which tools\item.ps1 reads the same way.
. (Join-Path $PSScriptRoot 'items.ps1')
$items = @(Get-ListItems $Lists)

# Outside reports (docs/axKit/outside-reports.md OR-7.3, OR-6.3): a commit on an item whose Where it is written names a
# report needs the maintainer's approval of an entry of that report naming the item, verified with the key pinned in
# this repository; and a change of that key needs the old key's signature over the new one's fingerprint.
. (Join-Path $PSScriptRoot 'approvals.ps1')
$repositoryRoot = Split-Path $PSScriptRoot -Parent
$pinnedKey = Get-PinnedApprovalKey $(if ($PinnedFrom) { $PinnedFrom } else { $repositoryRoot })
$outsideEntries = @(Get-OutsideEntries (Join-Path (Split-Path $Lists -Parent) 'outside-reports') $pinnedKey)

$pinnedPath = 'docs/axKit/approval-key.pub'
$keyStaged = "$(& git -C $repositoryRoot diff --cached --name-only -- $pinnedPath 2>$null)".Trim()
if ($keyStaged) {
    $before = (& git -C $repositoryRoot show "HEAD:$pinnedPath" 2>$null) -join "`n"
    $oldKey = if ($LASTEXITCODE -eq 0) { Read-PinnedApprovalKeyText $before } else { $null }
    if ($null -ne $oldKey) {
        $after = (& git -C $repositoryRoot show ":$pinnedPath" 2>$null) -join "`n"
        $newKey = if ($LASTEXITCODE -eq 0) { Read-PinnedApprovalKeyText $after } else { $null }
        $signed = @($lines | Where-Object { $_ -match '^\s*Approval key changed:\s*(\S+)\s*$' } | ForEach-Object { $Matches[1] })
        $ok = $null -ne $newKey -and $signed.Count -eq 1 -and (Test-ApprovalSignature (Get-ApprovalChangeText $newKey.Fingerprint) $signed[0] $oldKey)
        if (-not $ok) {
            Write-Host ''
            Write-Host "Commit refused: it changes or removes the pinned approval key ($pinnedPath), and its message has no line 'Approval key changed: <signature>' that the old key signed (axTask, Change the approval passphrase). docs/axKit/outside-reports.md OR-6.3."
            exit 1
        }
    }
}

$today = (Get-Date).ToString('yyyy-MM-dd')
foreach ($one in $named) {
    if ($one.Words -match '^none\b[\s,:.-]*(.*)$') {
        if ($one.Done) { Refuse '"Item done: none" finishes nothing; write "Item: none, <the reason>".' }
        if ($Matches[1].Trim().Length -lt 3) { Refuse '"Item: none" needs its reason after a comma.' }
        continue
    }

    $words = $one.Words.TrimEnd('.')
    $found = @(Find-ListItems $items $words)
    if ($found.Count -eq 0) {
        Refuse "no item's title holds '$words'. Write the item first (rule 9), or correct the words."
    }
    if ($found.Count -gt 1) {
        $titles = ($found | Select-Object -First 6 | ForEach-Object { '    ' + $_.Title }) -join "`n"
        Refuse "'$words' names $($found.Count) items; use more of the title:`n$titles"
    }

    $item = $found[0]
    $where = [string]($item.Document.fields | Where-Object { $_.title -eq 'Where it is written' } | Select-Object -First 1).value
    foreach ($report in @(Get-ReportNumbers $where)) {
        if (-not (Test-ApprovedForItem $outsideEntries $report $item.Title)) {
            Write-Host ''
            Write-Host "Commit refused: the item '$($item.Title)' comes from outside (report $report), and no entry of that report approved by the maintainer names it, or its approval no longer verifies. docs/axKit/outside-reports.md OR-7.3."
            exit 1
        }
    }
    $fields = [System.Collections.ArrayList]@($item.Document.fields)
    $started = $fields | Where-Object { $_.title -eq 'Started' } | Select-Object -First 1
    $done = $fields | Where-Object { $_.title -eq 'Done' } | Select-Object -First 1
    $changed = $false
    if (-not $started) {
        $started = [pscustomobject]@{ title = 'Started'; type = 'date'; value = '' }
        $at = if ($done) { $fields.IndexOf($done) + 1 } else { [Math]::Min(1, $fields.Count) }
        $fields.Insert($at, $started)
        $changed = $true
    }
    if (-not "$($started.value)") {
        $started.value = $today
        $changed = $true
    }
    if ($one.Done -and $done -and -not $done.value) {
        $done.value = $true
        $changed = $true
    }

    $what = if ($one.Done) { 'done' } else { 'started' }
    if ($Check) {
        Write-Host "Would mark ${what}: $($item.Title)$(if (-not $changed) { ' (already so)' })"
        continue
    }
    if ($changed) {
        $item.Document.fields = $fields.ToArray()
        Save-ListItem $item.File $item.Document
        Write-Host "Priorities list, marked ${what}: $($item.Title)"
    }
}

exit 0
