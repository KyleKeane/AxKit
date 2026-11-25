<#
.SYNOPSIS
  One item of the repair list (docs\axKit\repairs.json) changed by its id: done with its commit and how it was checked,
  a note added, an owed check added; then the page written again by tools\repairs.ps1.

.DESCRIPTION
  The pitfall this script removes (the review of the sessions of 2026-11-25): the items were changed by hand-made
  regular expressions over the file's lines, a different one each time, and the page was forgotten until a check
  named it. The file keeps one item a line; this script reads that line as JSON, changes the fields asked for, and
  writes the line back in the file's own form ({ "id": "R-75", "area": "...", ... }, the fields in their order),
  every other line as it was. -Status done needs -Commit and -Verify (repairs.ps1 holds the fields). -Note is added
  after the note there is. -Owed adds one owed check, its kind first ("hearing: ...", "live: ...", "window: ...").
  Exit code 0 when the item was changed and the page written, 1 when the id is not in the list, 2 on a usage error.
  The script stays ASCII.

.EXAMPLE
  .\tools\repair.ps1 R-84 -Status done -Commit a4de5e5d -Verify 'the release rehearsal passes the hooks check'
  .\tools\repair.ps1 R-95 -Owed 'hearing: the refused sound in every app'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$Id,
    [ValidateSet('', 'open', 'done', 'deferred', 'declined')][string]$Status = '',
    [string]$Commit = '',
    [string]$Verify = '',
    [string]$Note = '',
    [string]$Owed = '',
    [string]$Fix = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$path = Join-Path $root 'docs\axKit\repairs.json'

if ($Status -eq 'done' -and (-not $Commit -or -not $Verify)) { Write-Host 'A done item names its -Commit and how it was checked, -Verify.'; exit 2 }
if ($Owed -and $Owed -notmatch '^(hearing|live|window): ') { Write-Host 'An owed check starts with its kind: "hearing: ", "live: " or "window: ".'; exit 2 }

# JSON's own string, written as the file writes it: quotes and backslashes escaped, nothing else.
function ConvertTo-JsonText([string]$text) {
    $escaped = $text.Replace('\', '\\').Replace('"', '\"').Replace("`r", '\r').Replace("`n", '\n').Replace("`t", '\t')
    return '"' + $escaped + '"'
}

function Format-RepairItem($item) {
    $parts = foreach ($property in $item.PSObject.Properties) {
        $value = $property.Value
        $json = if ($value -is [array] -or $value -is [System.Collections.IList]) {
            '[' + ((@($value) | ForEach-Object { ConvertTo-JsonText "$_" }) -join ', ') + ']'
        } else {
            ConvertTo-JsonText "$value"
        }
        "`"$($property.Name)`": $json"
    }
    return '{ ' + ($parts -join ', ') + ' }'
}

$raw = [IO.File]::ReadAllText($path)
$lines = $raw -split "`n"
$index = -1
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match ('^\s*\{ "id": "' + [regex]::Escape($Id) + '"')) { $index = $i; break }
}
if ($index -lt 0) { Write-Host "No item $Id in docs\axKit\repairs.json."; exit 1 }

$line = $lines[$index].TrimEnd("`r")
$indent = [regex]::Match($line, '^\s*').Value
$comma = $line.TrimEnd().EndsWith(',')
$item = ($line.Trim().TrimEnd(',')) | ConvertFrom-Json

function Set-Field($object, [string]$name, $value) {
    if ($object.PSObject.Properties[$name]) { $object.$name = $value } else { $object | Add-Member -NotePropertyName $name -NotePropertyValue $value }
}

if ($Fix) { Set-Field $item 'fix' $Fix }
if ($Verify) { Set-Field $item 'verify' $Verify }
if ($Owed) { Set-Field $item 'owed' (@(@($item.owed) | Where-Object { $_ }) + $Owed) }
if ($Status) { Set-Field $item 'status' $Status }
if ($Commit) { Set-Field $item 'commit' $Commit }
if ($Note) { Set-Field $item 'note' ((("$($item.note)").Trim() + ' ' + $Note.Trim()).Trim()) }

# The fields in the list's own order, owed before status as the file has them.
$order = 'id', 'area', 'severity', 'title', 'where', 'principle', 'fix', 'verify', 'owed', 'status', 'commit', 'note'
$ordered = New-Object PSObject
foreach ($name in $order) { if ($item.PSObject.Properties[$name]) { $ordered | Add-Member -NotePropertyName $name -NotePropertyValue $item.$name } }
foreach ($property in $item.PSObject.Properties) { if ($order -notcontains $property.Name) { $ordered | Add-Member -NotePropertyName $property.Name -NotePropertyValue $property.Value } }

$lines[$index] = $indent + (Format-RepairItem $ordered) + $(if ($comma) { ',' } else { '' }) + $(if ($lines[$index].EndsWith("`r")) { "`r" } else { '' })
[IO.File]::WriteAllText($path, ($lines -join "`n"), (New-Object Text.UTF8Encoding($false)))
Write-Host "$Id changed."
& (Join-Path $PSScriptRoot 'repairs.ps1')
exit $LASTEXITCODE
