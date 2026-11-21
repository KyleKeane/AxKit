<#
.SYNOPSIS
  Says where the reports on GitHub and the items of the priorities list disagree (CLAUDE.md rule 9).

.DESCRIPTION
  The reports are the issues of KyleKeane/AxKit-reports; the work on them is the items of
  <parent>\axkit-management\priorities. An item points at its reports in its "Where it is written" field with the
  words "KyleKeane/AxKit-reports issue 8" or "KyleKeane/AxKit-reports issues 8 and 9" (up to the next semicolon).
  Nothing else ties the two together, so this script reads both and names every disagreement:

    new          an open report that no item points at: read it and write its item
    close it     an open report whose items are all done: answer it and close it
    item open    a closed report with an item that is not done: finish the item, or mark it done

  It changes nothing. One line per disagreement, or one line saying that they agree. Exit code 0 when they agree, 1
  when something is named, 2 when GitHub or the lists could not be read. tools\sync-repos.ps1 runs it on every push,
  where its exit code is ignored: it informs, it never stops a push.

.EXAMPLE
  .\tools\reports-check.ps1
  .\tools\reports-check.ps1 -Lists C:\temp\priorities -IssuesFile C:\temp\issues.json     for the tests
#>
param(
    # The priorities list; <parent>\axkit-management\priorities by default.
    [string]$Lists,
    # Instead of asking GitHub: a file with the JSON of gh issue list --json number,state,title.
    [string]$IssuesFile,
    [string]$Repository = 'KyleKeane/AxKit-reports'
)

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Lists) {
    $Lists = Join-Path (Split-Path -Parent (Split-Path -Parent $here)) 'axkit-management\priorities'
}
if (-not (Test-Path $Lists)) {
    Write-Host "Reports: the priorities list is not at $Lists; nothing checked."
    exit 2
}

if ($IssuesFile) {
    $issuesText = [IO.File]::ReadAllText($IssuesFile)
} else {
    # Outside call, program, gh: reads the numbers, states and titles of the reports repository's issues from GitHub.
    $issuesText = (& gh issue list --repo $Repository --state all --limit 500 --json 'number,state,title' 2>$null) -join "`n"
    if ($LASTEXITCODE -ne 0 -or -not $issuesText) {
        Write-Host 'Reports: GitHub could not be asked (gh missing, signed out, or no network); nothing checked.'
        exit 2
    }
}
# Windows PowerShell 5.1 hands a JSON array on as one object: unrolled here, so that Count is the reports' number.
$issues = @(($issuesText | ConvertFrom-Json) | ForEach-Object { $_ })

# Each report's items: number -> the list of (title, done).
$pointing = @{}
foreach ($file in Get-ChildItem $Lists -Filter *.json) {
    if ($file.Name -eq 'list.json') { continue }
    try { $item = [IO.File]::ReadAllText($file.FullName) | ConvertFrom-Json } catch { continue }
    if (-not $item.fields) { continue }
    $where = [string]($item.fields | Where-Object { $_.title -eq 'Where it is written' } | Select-Object -First 1).value
    $title = [string]($item.fields | Where-Object { $_.title -eq 'Title' } | Select-Object -First 1).value
    $done = [bool]($item.fields | Where-Object { $_.title -eq 'Done' } | Select-Object -First 1).value
    foreach ($match in [regex]::Matches($where, 'AxKit-reports issues? ([^;]*)', 'IgnoreCase')) {
        foreach ($number in [regex]::Matches($match.Groups[1].Value, '\d+')) {
            $key = [int]$number.Value
            if (-not $pointing.ContainsKey($key)) { $pointing[$key] = @() }
            $pointing[$key] += [pscustomobject]@{ Title = $title; Done = $done }
        }
    }
}

# Since 2026-11-21 a report gets an entry of the outside reports list, not an item (docs/axKit/outside-reports.md):
# each report's entry, by number. A report with an entry is judged by it; an older one by its items, as before.
. (Join-Path $here 'approvals.ps1')
$outsideList = Join-Path (Split-Path -Parent $Lists) 'outside-reports'
$entryOf = @{}
foreach ($entry in @(Get-OutsideEntries $outsideList (Get-PinnedApprovalKey (Split-Path -Parent $here)))) {
    foreach ($number in $entry.Reports) { $entryOf[[int]$number] = $entry }
}

$lines = @()
foreach ($issue in ($issues | Sort-Object number)) {
    $open = "$($issue.state)" -ieq 'OPEN'
    if ($entryOf.ContainsKey([int]$issue.number)) {
        $state = $entryOf[[int]$issue.number].State
        $finished = $state -in @('Done', 'Declined')
        if ($open -and $finished) {
            $lines += "Reports: close it: report $($issue.number) is open and its entry is $($state.ToLowerInvariant()): answer it and close it."
        } elseif (-not $open -and -not $finished) {
            $lines += "Reports: entry open: report $($issue.number) is closed and its entry is '$state'."
        }
        continue
    }
    $items = @()
    if ($pointing.ContainsKey([int]$issue.number)) { $items = @($pointing[[int]$issue.number]) }
    $notDone = @($items | Where-Object { -not $_.Done })
    if ($open -and $items.Count -eq 0) {
        $lines += "Reports: new: report $($issue.number), '$($issue.title)', has no entry in the outside reports list: read it and write its entry (tools\outside-report.ps1)."
    } elseif ($open -and $notDone.Count -eq 0) {
        $lines += "Reports: close it: report $($issue.number) is open and its item is done: '$($items[0].Title)'."
    } elseif (-not $open -and $notDone.Count -gt 0) {
        $lines += "Reports: item open: report $($issue.number) is closed and its item is not done: '$($notDone[0].Title)'."
    }
}

if ($lines.Count -eq 0) {
    $openCount = @($issues | Where-Object { "$($_.state)" -ieq 'OPEN' }).Count
    Write-Host "Reports: the reports and the list agree ($($issues.Count) reports, $openCount open)."
    exit 0
}
$lines | ForEach-Object { Write-Host $_ }
exit 1
