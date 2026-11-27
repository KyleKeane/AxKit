<#
.SYNOPSIS
Writes docs\axKit\cases.md, the readable page of the table of cases docs\axKit\cases.json; or checks that the page is
current (-Check).

.DESCRIPTION
docs\axKit\cases.json is the one table of how axKit handles each unexpected situation (docs\axKit\SPEC.md AX-5.10).
Its head holds the format line and two closed lists, the things acted on and the situations; then one row per case:

    id         C-n, counted up from C-1
    thing      what is acted on, one of the head's things
    apps       the apps the case is of, by name, or "all" alone
    situation  what is wrong, one of the head's situations
    does       what axKit does, in one plain sentence
    says       the exact words spoken or shown, or "nothing" with the reason
    words      where the words are constants in the code, their names as Type.Member (left out when there are none)
    proof      the test as Project/Class.Method, or the window script as tests/scripts/<app>/<file>: <title of the goal
               or check>; empty when nothing proves the case yet
    owed       what would prove a case that has no proof (only then). It starts with its kind, which tools\owed.ps1
               reads for docs\axKit\owed.md: "a window script ...", "a run of a real ..." or "a test ..."
    source     the requirement or the repair the case comes from
    file       for a file of the settings folder that is not an app's own, its member of AppPaths (only then)

A change to how an unexpected situation is handled changes its row in the same commit, then this script writes the
page. The page is never edited by hand. test.ps1 and build.yml run -Check, so that the table and its page never part.
tests\AxKit.Tests\CasesTests holds the table to the repository: the writing, the lists, every proof and every
constant; this script checks only what it needs to write the page.
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$dataPath = Join-Path $root 'docs\axKit\cases.json'
$documentPath = Join-Path $root 'docs\axKit\cases.md'
$data = [IO.File]::ReadAllText($dataPath) | ConvertFrom-Json

$things = @($data.things)
$situations = @($data.situations)
$rows = @($data.rows)
$problems = @()
if ($data.format -ne 'axkit-cases 1') { $problems += "the format is '$($data.format)', not 'axkit-cases 1'" }
foreach ($row in $rows) {
    if ($things -cnotcontains $row.thing) { $problems += "$($row.id): the thing '$($row.thing)' is not in the list of things" }
    if ($situations -cnotcontains $row.situation) { $problems += "$($row.id): the situation '$($row.situation)' is not in the list of situations" }
    if (-not $row.proof -and -not $row.owed) { $problems += "$($row.id): no proof and no owed" }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host "docs/axKit/cases.json: $_" }
    Write-Host 'The fields are described at the top of tools/cases.ps1.'
    exit 1
}

# "axFile, axGIT and axTask", for the ear.
function Join-Names($names) {
    $list = @($names)
    if ($list.Count -le 1) { return "$list" }
    return (($list[0..($list.Count - 2)] -join ', ') + ' and ' + $list[-1])
}

# A sentence ends with its full stop, question mark or closing bracket; one that does not gets a full stop.
function End-Sentence([string]$text) {
    $trimmed = $text.Trim()
    if ($trimmed -match '[.?!)]$') { return $trimmed }
    return $trimmed + '.'
}

function Title([string]$text) { $text.Substring(0, 1).ToUpperInvariant() + $text.Substring(1) }

$unproved = @($rows | Where-Object { -not $_.proof })

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Cases')
$lines.Add('')
$lines.Add('Written by `tools/cases.ps1` from `docs/axKit/cases.json`; never edit it by hand. How axKit handles each unexpected')
$lines.Add('situation: what it does, what it says, and the test or window script that proves it. A change to how a situation')
$lines.Add('is handled changes its row in the JSON in the same commit, then the script writes this page; `CasesTests` holds')
$lines.Add('every proof and every sentence named here to the code (`docs/axKit/SPEC.md` AX-5.10).')
$lines.Add('')
$lines.Add('## Where it stands')
$lines.Add('')
$lines.Add("- $($rows.Count) cases, $($rows.Count - $unproved.Count) with a proof, $($unproved.Count) without.")
foreach ($thing in $things) {
    $count = @($rows | Where-Object { $_.thing -ceq $thing }).Count
    if ($count -gt 0) { $lines.Add("- $(Title $thing): $count.") }
}

if ($unproved.Count -gt 0) {
    $lines.Add('')
    $lines.Add('## Not proved yet')
    $lines.Add('')
    foreach ($row in $unproved) {
        $lines.Add("- $($row.id), $($row.thing), $($row.situation). Owed: $(End-Sentence $row.owed)")
    }
}

foreach ($thing in $things) {
    $ofThing = @($rows | Where-Object { $_.thing -ceq $thing })
    if ($ofThing.Count -eq 0) { continue }
    $lines.Add('')
    $lines.Add('## ' + (Title $thing))
    foreach ($situation in $situations) {
        $ofSituation = @($ofThing | Where-Object { $_.situation -ceq $situation })
        if ($ofSituation.Count -eq 0) { continue }
        $lines.Add('')
        $lines.Add('### ' + (Title $thing) + ': ' + $situation)
        foreach ($row in $ofSituation) {
            $where = if (@($row.apps).Count -eq 1 -and @($row.apps)[0] -ceq 'all') { 'every app' } else { Join-Names $row.apps }
            $lines.Add('')
            $lines.Add("**$($row.id), $where.**")
            $lines.Add('')
            $lines.Add("- Does: $(End-Sentence $row.does)")
            $lines.Add("- Says: $($row.says)")
            if ($row.words) { $lines.Add("- The words in the code: $(Join-Names @($row.words | ForEach-Object { '`' + $_ + '`' })).") }
            if ($row.file) { $lines.Add('- The file: `' + $row.file + '`.') }
            if ($row.proof) { $lines.Add('- Proved by: `' + $row.proof + '`.') }
            else { $lines.Add("- Not proved yet. Owed: $(End-Sentence $row.owed)") }
            $lines.Add("- From: $($row.source).")
        }
    }
}

$text = ($lines -join "`r`n") + "`r`n"
if ($Check) {
    $current = if (Test-Path $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    # Line endings aside: the checkout's are native, LF on CI.
    if (($current -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host 'docs/axKit/cases.md is not what docs/axKit/cases.json gives: run tools\cases.ps1 and commit the page.'
        exit 1
    }

    Write-Host "docs/axKit/cases.md is current: $($rows.Count) cases, $($unproved.Count) without a proof."
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote docs/axKit/cases.md: $($rows.Count) cases, $($unproved.Count) without a proof."
# An exit code of its own: without it the caller reads the code of whatever ran before (tools\pages.ps1).
exit 0
