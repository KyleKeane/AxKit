<#
.SYNOPSIS
Writes docs\axKit\owed.md, the one list of everything that is tested in code and not yet heard or tried for real; or
checks that the page is current (-Check).

.DESCRIPTION
Work that is built and tested is often still owed a check no test makes: a sentence heard with NVDA, a real outside
program, a situation no script reaches. Each such thing is written where it belongs, and this script gathers them
into one page, so that what is owed never rests on anyone's memory. The three sources:

  docs\axKit\cases.json      a row with "owed" (a case without a proof; tools\cases.ps1 describes the fields). Its
                             kind is read off the first words of what is owed: "a window script ..." is a window
                             check, "a run of a real ..." a live check, "a test ..." a test in code. A row that
                             starts otherwise is refused, with these words.
  docs\axKit\repairs.json    an item with "owed" (tools\repairs.ps1 describes the fields): each entry starts with
                             its kind, "hearing: ", "live: " or "window: ".
  docs\axKit\nvda-test-record.md   every section whose heading starts with "Owed:". The page names the heading and
                             points there; the steps stay in the record.

The page groups them by kind: owed a hearing with NVDA; owed a live check with a real outside program or situation;
owed a window check on the hidden desktop; owed a test in code. Each entry is one sentence with its id (a section of
the record has none: its heading names it).

An entry leaves the page when its source changes: the case gets its proof, the repair's entry is taken out and its
verify says how it was checked, the record's section becomes a dated pass. Then this script writes the page again.
The page is never edited by hand. test.ps1 and build.yml run -Check.
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$documentPath = Join-Path $root 'docs\axKit\owed.md'
$cases = [IO.File]::ReadAllText((Join-Path $root 'docs\axKit\cases.json')) | ConvertFrom-Json
$repairs = [IO.File]::ReadAllText((Join-Path $root 'docs\axKit\repairs.json')) | ConvertFrom-Json
$recordLines = [IO.File]::ReadAllLines((Join-Path $root 'docs\axKit\nvda-test-record.md'))

# What is owed ends with a full stop. Its first letter stays as written: an app's name starts with a small a.
function Sentence([string]$text) {
    $trimmed = $text.Trim()
    if ($trimmed -match '[.?!]$') { return $trimmed }
    return $trimmed + '.'
}

# "axFile, axGIT and axTask", for the ear.
function Join-Names($names) {
    $list = @($names)
    if ($list.Count -le 1) { return "$list" }
    return (($list[0..($list.Count - 2)] -join ', ') + ' and ' + $list[-1])
}

$kinds = [ordered]@{
    hearing = 'Owed a hearing with NVDA'
    live    = 'Owed a live check with a real outside program or situation'
    window  = 'Owed a window check on the hidden desktop'
    test    = 'Owed a test in code'
}
$entries = @{}
foreach ($kind in $kinds.Keys) { $entries[$kind] = New-Object System.Collections.Generic.List[string] }
$problems = @()

# The NVDA test record's own Owed sections: named, with the place, never copied.
foreach ($line in $recordLines) {
    if ($line -match '^##+ Owed: (.+?)\s*$') {
        $entries['hearing'].Add("- The NVDA test record: $(Sentence $Matches[1]) The steps are in ``docs/axKit/nvda-test-record.md``, under the heading ""Owed:"" with these words.")
    }
}

foreach ($item in $repairs.items) {
    foreach ($entry in @($item.owed)) {
        if (-not $entry) { continue }
        if ($entry -notmatch '^(hearing|live|window): (\S.*)$') {
            $problems += "docs/axKit/repairs.json: $($item.id): the owed entry '$entry' does not start with hearing: , live: or window: "
            continue
        }
        $entries[$Matches[1]].Add("- $($item.id): $(Sentence $Matches[2])")
    }
}

foreach ($row in $cases.rows) {
    if (-not $row.owed) { continue }
    $kind = if ($row.owed -match '^a window script\b') { 'window' } elseif ($row.owed -match '^a run of a real\b') { 'live' } elseif ($row.owed -match '^a test\b') { 'test' } else { '' }
    if (-not $kind) {
        $problems += "docs/axKit/cases.json: $($row.id): what is owed starts with none of 'a window script', 'a run of a real', 'a test', so its kind cannot be told"
        continue
    }
    $where = if (@($row.apps).Count -eq 1 -and @($row.apps)[0] -ceq 'all') { 'every app' } else { Join-Names $row.apps }
    $entries[$kind].Add("- $($row.id), $where, $($row.thing) $($row.situation): $(Sentence $row.owed)")
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host $_ }
    Write-Host 'The sources are described at the top of tools/owed.ps1.'
    exit 1
}

$total = 0
foreach ($kind in $kinds.Keys) { $total += $entries[$kind].Count }

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Owed')
$lines.Add('')
$lines.Add('Written by `tools/owed.ps1` from `docs/axKit/cases.json`, `docs/axKit/repairs.json` and the Owed sections of')
$lines.Add('`docs/axKit/nvda-test-record.md`; never edit it by hand. Everything that is tested in code and not yet heard or')
$lines.Add('tried for real, by the kind of check it waits for. An entry leaves when its source changes: a case gets its proof,')
$lines.Add('a repair''s `owed` entry is taken out and its `verify` says how it was checked, a section of the record becomes a')
$lines.Add('dated pass; then the script writes this page again.')
$lines.Add('')
$lines.Add('## Where it stands')
$lines.Add('')
$lines.Add("- $total owed in all.")
foreach ($kind in $kinds.Keys) {
    $lines.Add("- $($kinds[$kind]): $($entries[$kind].Count).")
}

foreach ($kind in $kinds.Keys) {
    if ($entries[$kind].Count -eq 0) { continue }
    $lines.Add('')
    $lines.Add('## ' + $kinds[$kind])
    $lines.Add('')
    foreach ($entry in $entries[$kind]) { $lines.Add($entry) }
}

$text = ($lines -join "`r`n") + "`r`n"
if ($Check) {
    $current = if (Test-Path $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    # Line endings aside: the checkout's are native, LF on CI.
    if (($current -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host 'docs/axKit/owed.md is not what its sources give: run tools\owed.ps1 and commit the page.'
        exit 1
    }

    Write-Host "docs/axKit/owed.md is current: $total owed."
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote docs/axKit/owed.md: $total owed."
