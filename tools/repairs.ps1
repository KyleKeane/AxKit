<#
.SYNOPSIS
Writes docs\axKit\repairs.md, the readable page of the repair list docs\axKit\repairs.json; or checks that the page is
current (-Check); or prints the open items in a few lines (-Open).

.DESCRIPTION
docs\axKit\repairs.json is the one list of what an audit found and what became of it (the audit of 2026-11-13 started
it). It holds the principles the repairs follow, each with an id, and the items, each with:

    id         R-nn, never reused
    area       security, tooling, framework, core, apps, docs, changes
    severity   critical, high, medium, low
    title      the defect in a few words
    where      the files it is in
    principle  the id of the principle it broke
    fix        what is done about it
    verify     how the fix is checked: the test, the script, the command
    owed       only where a done fix is tested in code and still owed a check no test makes: a list of short
               sentences, each starting with its kind and a colon, "hearing: " (to be heard with NVDA), "live: " (a
               real outside program or situation) or "window: " (a window check on the hidden desktop).
               tools\owed.ps1 writes docs\axKit\owed.md from them; an entry is taken out when the thing was heard or
               tried, and verify then says how. A hearing that a script reaches names it at its end, "(heard by
               tests/scripts/<app>/<name>.txt)", and stays: the page drops it by itself once that script has
               passed with NVDA listening (tools\owed.ps1)
    status     open, done, deferred (with why in note), declined (with why in note)
    commit     the commit that fixed it, when done
    note       anything else

An item is added when a defect is found and its status changed when it is fixed, in the same commit as the fix. The
page is written by this script and never by hand. test.ps1 and build.yml run -Check, so that the list and its page
never part. A done item stays, as the record of what was repaired and how it was checked.

Every run, with or without -Check, first holds the list to its fields: an id used once, a known severity, status and
principle, a done item with its verify, a deferred or declined one with its note, what is owed only on a done item
and each with its kind, and a commit that is named being a
commit of this repository (looked up where the whole history is, so not on CI's shallow checkout).
#>
[CmdletBinding()]
param([switch]$Check, [switch]$Open)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
. (Join-Path $root 'tools\ordinal.ps1')
$dataPath = Join-Path $root 'docs\axKit\repairs.json'
$documentPath = Join-Path $root 'docs\axKit\repairs.md'
$data = [IO.File]::ReadAllText($dataPath) | ConvertFrom-Json

$severities = 'critical', 'high', 'medium', 'low'
$statuses = 'open', 'done', 'deferred', 'declined'
$principleIds = @($data.principles | ForEach-Object { $_.id })
$problems = @()
$seen = @{}
# The commits are looked up only where the whole history is: CI checks out the newest commit alone (a shallow clone),
# and a copy without git has none.
$history = (Get-Command git -ErrorAction SilentlyContinue) -and ("$(git -C $root rev-parse --is-shallow-repository)".Trim() -eq 'false')
$knownCommits = @{}
foreach ($item in $data.items) {
    if ($seen.ContainsKey($item.id)) { $problems += "$($item.id): the id is used twice" }
    $seen[$item.id] = $true
    if ($severities -notcontains $item.severity) { $problems += "$($item.id): severity '$($item.severity)' is not one of $($severities -join ', ')" }
    if ($statuses -notcontains $item.status) { $problems += "$($item.id): status '$($item.status)' is not one of $($statuses -join ', ')" }
    if ($principleIds -notcontains $item.principle) { $problems += "$($item.id): principle '$($item.principle)' is not in the list" }
    if ($item.status -eq 'done' -and -not $item.verify) { $problems += "$($item.id): done without saying how it was checked (verify)" }
    if (($item.status -eq 'deferred' -or $item.status -eq 'declined') -and -not $item.note) { $problems += "$($item.id): $($item.status) without saying why (note)" }
    # What is owed (tools\owed.ps1 reads it): only of a fix that is done, each entry with its kind in front.
    if ($item.owed -and $item.status -ne 'done') { $problems += "$($item.id): owed on an item that is not done" }
    foreach ($entry in @($item.owed)) {
        if ($entry -and $entry -notmatch '^(hearing|live|window): \S') { $problems += "$($item.id): the owed entry '$entry' does not start with hearing: , live: or window: " }
    }
    # A commit that is named is one this repository has: a mistyped one would send the reader of the record nowhere.
    if ($item.commit -and $history -and -not $knownCommits.ContainsKey($item.commit)) {
        # --quiet: nothing on the error stream, which Windows PowerShell would turn into an error under Stop.
        git -C $root rev-parse --verify --quiet "$($item.commit)^{commit}" | Out-Null
        if ($LASTEXITCODE -ne 0) { $problems += "$($item.id): the commit '$($item.commit)' is not in this repository" }
        else { $knownCommits[$item.commit] = $true }
    }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host "docs/axKit/repairs.json: $_" }
    Write-Host 'The fields are described at the top of tools/repairs.ps1.'
    exit 1
}

function Rank($item) { [array]::IndexOf($severities, $item.severity) }

# The items of a list in the page's order: by severity, then by id, as text (tools\ordinal.ps1).
function InOrder($items) { Sort-Ordinal @($items) { "{0:D3}`t{1}" -f [int](Rank $_), $_.id } }

if ($Open) {
    InOrder ($data.items | Where-Object { $_.status -eq 'open' }) | ForEach-Object { "{0} {1,-8} {2,-9} {3}" -f $_.id, $_.severity, $_.area, $_.title }
    exit 0
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Repairs')
$lines.Add('')
$lines.Add('Written by `tools/repairs.ps1` from `docs/axKit/repairs.json`; never edit it by hand. The list of what the audits')
$lines.Add('found and what became of each finding: change the JSON in the same commit as the fix, then run the script.')
$lines.Add('`tools/repairs.ps1 -Open` prints what is still open.')
$lines.Add('')
$lines.Add('## Where it stands')
$lines.Add('')
foreach ($status in $statuses) {
    $of = @($data.items | Where-Object { $_.status -eq $status })
    if ($of.Count -gt 0) {
        $by = ($severities | ForEach-Object { $s = $_; $n = @($of | Where-Object { $_.severity -eq $s }).Count; if ($n -gt 0) { "$n $s" } }) -join ', '
        $lines.Add("- **$status**: $($of.Count) ($by)")
    }
}

$lines.Add('')
$lines.Add('## The principles')
$lines.Add('')
foreach ($principle in $data.principles) {
    $count = @($data.items | Where-Object { $_.principle -eq $principle.id }).Count
    $lines.Add("- **$($principle.id). $($principle.name).** $($principle.text) ($count finding$(if ($count -ne 1) { 's' }))")
}

foreach ($status in $statuses) {
    $of = @(InOrder ($data.items | Where-Object { $_.status -eq $status }))
    if ($of.Count -eq 0) { continue }
    $lines.Add('')
    $lines.Add('## ' + $status.Substring(0, 1).ToUpperInvariant() + $status.Substring(1))
    foreach ($item in $of) {
        $lines.Add('')
        $lines.Add("### $($item.id), $($item.severity), $($item.area): $($item.title)")
        $lines.Add('')
        $lines.Add("- Where: $(@($item.where) -join '; ')")
        $lines.Add("- Principle: $($item.principle)")
        $lines.Add("- Fix: $($item.fix)")
        if ($item.verify) { $lines.Add("- Checked by: $($item.verify)") }
        if ($item.commit) { $lines.Add("- Commit: $($item.commit)") }
        if ($item.note) { $lines.Add("- Note: $($item.note)") }
    }
}

$text = ($lines -join "`r`n") + "`r`n"
if ($Check) {
    $current = if (Test-Path $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    # Line endings aside: the checkout's are native, LF on CI.
    if (($current -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host 'docs/axKit/repairs.md is not what docs/axKit/repairs.json gives: run tools\repairs.ps1 and commit the page.'
        exit 1
    }

    Write-Host "docs/axKit/repairs.md is current: $(@($data.items).Count) findings, $(@($data.items | Where-Object { $_.status -eq 'open' }).Count) open."
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote docs/axKit/repairs.md: $(@($data.items).Count) findings, $(@($data.items | Where-Object { $_.status -eq 'open' }).Count) open."
# An exit code of its own: without it the caller reads the code of whatever ran before (tools\pages.ps1, tools\repair.ps1).
exit 0
