<#
.SYNOPSIS
  Exact text replacements in files, the way the assistant's edits went wrong least: every old text must be found, once,
  or nothing is written.

.DESCRIPTION
  The pitfalls this script removes (docs/axKit/commands.md; the review of the sessions of 2026-11-25):
  - a replacement of several lines failed on a file with Windows line ends, since the text was written with \n:
    here both the old and the new text take the file's own line ends before they are looked for;
  - a replacement found nothing and the edit went on as if it had (a helper printed MISSING and the next step ran):
    here a text that is not found stops the whole run, and no file is changed;
  - a replacement matched more than was meant (a pattern folded the body of the method it was written into, which
    then called itself): here a text found more than once stops the run too, unless -All says it is meant;
  - a file lost or gained its byte order mark, or its line ends, when written back: here the file keeps both.
  One file and one pair from the command line, or many pairs from a JSON file (-Pairs: an array of objects with file,
  old, new and, when wanted, all), applied in order; every pair is checked before any file is written. Exit code 0
  when every pair was found and written, 1 when one was not found or found too often (nothing written), 2 on a usage
  error. The script stays ASCII.

.EXAMPLE
  .\tools\replace.ps1 -File src\AxTask\TaskForm.cs -Old 'Hide done tasks' -New 'Hide finished tasks'
  .\tools\replace.ps1 -Pairs C:\scratch\edits.json
#>
[CmdletBinding()]
param(
    [string]$File = '',
    [string]$Old = '',
    [string]$New = '',
    [switch]$All,
    [string]$Pairs = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$edits = @()
if ($Pairs) {
    if (-not (Test-Path -LiteralPath $Pairs)) { Write-Host "No pairs file: $Pairs"; exit 2 }
    # Assigned first: Windows PowerShell 5.1's ConvertFrom-Json gives a JSON array to the pipe as one object, and a
    # foreach over the pipe's result saw a single pair holding every file (2026-11-25).
    $list = Get-Content -LiteralPath $Pairs -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($pair in $list) {
        $edits += [pscustomobject]@{ File = "$($pair.file)"; Old = "$($pair.old)"; New = "$($pair.new)"; All = [bool]$pair.all }
    }
} elseif ($File -and $Old) {
    $edits += [pscustomobject]@{ File = $File; Old = $Old; New = $New; All = [bool]$All }
} else {
    Write-Host 'Give -File, -Old and -New, or -Pairs <file.json>.'
    exit 2
}

# The files read once, with what they hold: their text, their line end and whether they began with a byte order mark.
$texts = @{}
function Get-FileText([string]$path) {
    $full = if ([IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $root $path }
    if (-not $texts.ContainsKey($full)) {
        if (-not (Test-Path -LiteralPath $full)) { throw "No such file: $path" }
        $bytes = [IO.File]::ReadAllBytes($full)
        $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        $text = [Text.Encoding]::UTF8.GetString($bytes, $(if ($bom) { 3 } else { 0 }), $bytes.Length - $(if ($bom) { 3 } else { 0 }))
        $nl = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
        $texts[$full] = [pscustomobject]@{ Path = $path; Text = $text; Line = $nl; Bom = $bom; Changed = $false }
    }
    return $texts[$full]
}

function Get-Count([string]$text, [string]$part) {
    $count = 0; $at = 0
    while (($at = $text.IndexOf($part, $at, [StringComparison]::Ordinal)) -ge 0) { $count++; $at += [Math]::Max(1, $part.Length) }
    return $count
}

$problems = @()
foreach ($edit in $edits) {
    $entry = Get-FileText $edit.File
    # Both texts in the file's own line ends.
    $oldText = ($edit.Old -replace "`r?`n", "`n") -replace "`n", $entry.Line
    $newText = ($edit.New -replace "`r?`n", "`n") -replace "`n", $entry.Line
    $count = Get-Count $entry.Text $oldText
    $short = $edit.Old.Split("`n")[0]
    if ($short.Length -gt 70) { $short = $short.Substring(0, 70) + '...' }
    if ($count -eq 0) { $problems += "$($edit.File): not found: $short"; continue }
    if ($count -gt 1 -and -not $edit.All) { $problems += "$($edit.File): found $count times (give all when every one is meant): $short"; continue }
    $entry.Text = $entry.Text.Replace($oldText, $newText)
    $entry.Changed = $true
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host $_ }
    Write-Host "Nothing written: $($problems.Count) of $($edits.Count) replacement(s) did not hold."
    exit 1
}

foreach ($written in $texts.Values | Where-Object { $_.Changed }) {
    $full = if ([IO.Path]::IsPathRooted($written.Path)) { $written.Path } else { Join-Path $root $written.Path }
    [IO.File]::WriteAllText($full, $written.Text, (New-Object Text.UTF8Encoding($written.Bom)))
}
Write-Host "Replaced: $($edits.Count) replacement(s) in $(@($texts.Values | Where-Object { $_.Changed }).Count) file(s)."
exit 0
