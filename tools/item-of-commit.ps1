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
  script says nothing and allows every commit: the lists are the maintainer's own. Merge commits and the commits git
  writes itself (a revert, a fixup) are allowed as they are.

.EXAMPLE
  .\tools\item-of-commit.ps1 .git\COMMIT_EDITMSG
  .\tools\item-of-commit.ps1 message.txt -Lists C:\temp\priorities -Check     say what would happen, write nothing
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$MessageFile,
    [string]$Lists,
    [switch]$Check
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

# What git writes itself is allowed as it is.
if ($subject -match '^(Merge |Revert "|fixup! |squash! |amend! )') {
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

# The items: each file's Title, and the file.
$items = @()
foreach ($file in Get-ChildItem -LiteralPath $Lists -Filter *.json) {
    if ($file.Name -eq 'list.json') { continue }
    try {
        $document = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
    } catch {
        continue
    }
    $title = ($document.fields | Where-Object { $_.title -eq 'Title' } | Select-Object -First 1).value
    if (-not $title) { $title = $file.BaseName }
    $items += [pscustomobject]@{ Title = "$title"; File = $file.FullName; Document = $document }
}

$today = (Get-Date).ToString('yyyy-MM-dd')
$utf8 = New-Object System.Text.UTF8Encoding($false)
foreach ($one in $named) {
    if ($one.Words -match '^none\b[\s,:.-]*(.*)$') {
        if ($one.Done) { Refuse '"Item done: none" finishes nothing; write "Item: none, <the reason>".' }
        if ($Matches[1].Trim().Length -lt 3) { Refuse '"Item: none" needs its reason after a comma.' }
        continue
    }

    $words = $one.Words.TrimEnd('.')
    $found = @($items | Where-Object { $_.Title.IndexOf($words, [StringComparison]::OrdinalIgnoreCase) -ge 0 })
    $exact = @($found | Where-Object { $_.Title -ieq $words })
    if ($exact.Count -eq 1) { $found = $exact }
    if ($found.Count -eq 0) {
        Refuse "no item's title holds '$words'. Write the item first (rule 9), or correct the words."
    }
    if ($found.Count -gt 1) {
        $titles = ($found | Select-Object -First 6 | ForEach-Object { '    ' + $_.Title }) -join "`n"
        Refuse "'$words' names $($found.Count) items; use more of the title:`n$titles"
    }

    $item = $found[0]
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
        [System.IO.File]::WriteAllText($item.File, ($item.Document | ConvertTo-Json -Depth 10), $utf8)
        Write-Host "Priorities list, marked ${what}: $($item.Title)"
    }
}

exit 0
