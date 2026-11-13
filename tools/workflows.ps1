<#
.SYNOPSIS
Writes docs\axKit\workflows.md, the page of every workflow the automated checks keep working, by app, each under its
label in the user's words; or checks that the page is current (-Check).

.DESCRIPTION
Every script of tests\scripts and tests\nvda-scripts starts with a line

    # Workflow: <a task in the user's words>

(docs/axAutomate/SPEC.md AU-5.2a; tests/AxAutomate.Tests fails on a script without one or on two with the same
label). The page lists, for each app, every workflow's label, what the script's header says it covers, and the script.
The labels are the names a user asks for help by, and the start of a tutorial for each.

-Check writes nothing and exits with 1 when the page on disk is not what the scripts give; then run the script without
-Check and commit the page. build.yml runs the check on every push.
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$documentPath = Join-Path $root 'docs\axKit\workflows.md'

function Read-Workflow([string]$path) {
    $label = $null
    $covers = New-Object System.Collections.Generic.List[string]
    foreach ($line in [IO.File]::ReadAllLines($path)) {
        $text = $line.Trim()
        if ($text.Length -eq 0) { if ($covers.Count -gt 0) { break } else { continue } }
        if (-not $text.StartsWith('#')) { break }
        $comment = $text.TrimStart('#').Trim()
        if ($comment -match '^(?i)workflow:\s*(.+)$') { $label = $Matches[1].Trim() } else { $covers.Add($comment) }
    }
    [pscustomobject]@{ Label = $label; Covers = ($covers -join ' ') }
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Workflows')
$lines.Add('')
$lines.Add('Written by `tools/workflows.ps1` from the scripts of `tests/scripts` and `tests/nvda-scripts`; never edit it by hand.')
$lines.Add('Every workflow below is played by the automated checks (`docs/axKit/automation.md`), with NVDA listening in')
$lines.Add('`nvda-pass.ps1`, so each is kept working from release to release. The label is the task in the words a user asks')
$lines.Add('for help with, and the script is its steps; together they are the start of a tutorial for it.')

# Ordinal order: Windows PowerShell and PowerShell 7 (CI) sort a hyphen differently by culture.
$files = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
Get-ChildItem (Join-Path $root 'tests\scripts'), (Join-Path $root 'tests\nvda-scripts') -Recurse -File -Filter *.txt | ForEach-Object { $files.Add($_) }
$files.Sort([Comparison[System.IO.FileInfo]] { param($a, $b) [string]::CompareOrdinal($a.FullName.ToLowerInvariant(), $b.FullName.ToLowerInvariant()) })
$groups = $files |
    Group-Object { if ($_.Directory.Name -eq 'nvda-scripts') { 'NVDA only' } elseif ($_.Directory.Name -eq 'mcp') { 'AI tools' } else { $_.Directory.Name } } |
    Sort-Object { if ($_.Name -eq 'NVDA only') { 2 } elseif ($_.Name -eq 'AI tools') { 1 } else { 0 } }, { $_.Name.ToLowerInvariant() }
$missing = @()
foreach ($group in $groups) {
    $lines.Add('')
    $lines.Add('## ' + $group.Name)
    $lines.Add('')
    $number = 0
    foreach ($file in $group.Group) {
        $workflow = Read-Workflow $file.FullName
        $relative = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
        if (-not $workflow.Label) { $missing += $relative; continue }
        $number++
        $lines.Add("$number. **$($workflow.Label)** (``$relative``): $($workflow.Covers)")
    }
}

if ($missing.Count -gt 0) {
    $missing | ForEach-Object { Write-Host "No '# Workflow:' line: $_" }
    exit 1
}

$text = ($lines -join "`r`n") + "`r`n"
if ($Check) {
    $current = if (Test-Path $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    # Line endings aside: the checkout's are native, LF on CI.
    if (($current -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host 'docs/axKit/workflows.md is not current: run tools\workflows.ps1 and commit the page.'
        exit 1
    }

    Write-Host "docs/axKit/workflows.md is current: $(($groups | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum) workflows."
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote docs/axKit/workflows.md: $(($groups | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum) workflows."
