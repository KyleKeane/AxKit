<#
.SYNOPSIS
Writes docs\axKit\key-map.md, every key of every window of axKit and what it does in each app; or checks that the page
is current (-Check).

.DESCRIPTION
The page is written from three sources and never by hand:

    docs\axKit\window-commands.json   the inventory of every window's commands, each with its key ("key") and the
                                      other keys it answers ("also"); written by a run with --inventory write
    src\AxKit.Forms\BundleKeys.cs     the bundle-wide keys (SPEC AX-8.2): every "public const Keys <Name> = ...;"
    docs\axKit\key-families.json      the proposed families of keys and the commands that do not fit them

A chord has one meaning when every command on it, in every app, carries the same label, the case of letters aside;
otherwise its meaning differs, and its spread is the number of its labels. The page lists the chords with one meaning
in a fixed order (plain keys, F-keys, Ctrl, Ctrl+Shift, Alt, others), then the chords whose meaning differs, most
spread first, then the bundle keys the inventory has no command for, then the families.

The families are a draft (their file's "status"): this script moves no key and no test holds a key to its family.
Every run, with or without -Check, holds the families' file to the inventory: a key named there is of its family's
pattern ("match", a regular expression over the chord) and every command named there (chord, app, label) is in the
inventory, so that a renamed command or a moved key shows as a line to correct and not as a page that is no longer
true. test.ps1 and build.yml run -Check.

.EXAMPLE
  .\tools\key-map.ps1          # writes the page
  .\tools\key-map.ps1 -Check   # exit code 1 when the page is not what the sources give
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$inventoryPath = Join-Path $root 'docs\axKit\window-commands.json'
$familiesPath = Join-Path $root 'docs\axKit\key-families.json'
$bundlePath = Join-Path $root 'src\AxKit.Forms\BundleKeys.cs'
$documentPath = Join-Path $root 'docs\axKit\key-map.md'

$inventory = [IO.File]::ReadAllText($inventoryPath) | ConvertFrom-Json
$families = [IO.File]::ReadAllText($familiesPath) | ConvertFrom-Json
$problems = New-Object System.Collections.Generic.List[string]

# A key of the Keys enumeration as the chord vocabulary names it (AxKit.Core/KeyChord): the names that differ.
$keyNames = @{
    'return' = 'enter'; 'oemplus' = 'plus'; 'add' = 'plus'; 'oemminus' = 'minus'; 'subtract' = 'minus'
    'oemperiod' = 'period'; 'oemcomma' = 'comma'; 'oemquestion' = 'slash'; 'oemsemicolon' = 'semicolon'; 'back' = 'backspace'
}

# "Keys.Control | Keys.Shift | Keys.P" as "ctrl+shift+p".
function ConvertTo-Chord([string]$expression) {
    $modifiers = @{ 'control' = $false; 'shift' = $false; 'alt' = $false }
    $key = ''
    foreach ($part in $expression.Split('|')) {
        $name = $part.Trim().ToLowerInvariant() -replace '^keys\.', ''
        if ($modifiers.ContainsKey($name)) { $modifiers[$name] = $true }
        elseif ($keyNames.ContainsKey($name)) { $key = $keyNames[$name] }
        elseif ($name -match '^d([0-9])$') { $key = $Matches[1] }
        else { $key = $name }
    }
    $text = ''
    if ($modifiers['control']) { $text += 'ctrl+' }
    if ($modifiers['shift']) { $text += 'shift+' }
    if ($modifiers['alt']) { $text += 'alt+' }
    return $text + $key
}

# The bundle-wide keys: chord to the constant's name, the first name when two constants hold one chord (the numpad's).
$bundle = @{}
$bundleOrder = New-Object System.Collections.Generic.List[string]
foreach ($line in [IO.File]::ReadAllLines($bundlePath)) {
    if ($line -match '^\s*public const Keys (\w+) = ([^;]+);') {
        $chordOfConstant = ConvertTo-Chord $Matches[2]
        if (-not $bundle.ContainsKey($chordOfConstant)) {
            $bundle[$chordOfConstant] = $Matches[1]
            $bundleOrder.Add($chordOfConstant)
        }
    }
}
if ($bundle.Count -eq 0) { $problems.Add('src/AxKit.Forms/BundleKeys.cs: no "public const Keys" line was found') }

# One row per key of a command: the app, the chord, the label.
$rows = New-Object System.Collections.Generic.List[object]
$appNames = @($inventory.apps | ForEach-Object { $_.app })
$keyed = 0
foreach ($app in $inventory.apps) {
    foreach ($command in $app.commands) {
        if ($command.key) { $keyed++ }
        $keysOfCommand = @()
        if ($command.key) { $keysOfCommand += $command.key }
        $keysOfCommand += @($command.also | Where-Object { $_ })
        foreach ($chord in $keysOfCommand) {
            $rows.Add([pscustomobject]@{ App = $app.app; Chord = $chord; Label = $command.label })
        }
    }
}

# A chord as it is said: "Ctrl+Shift+P", "Page Down", "F3".
$spoken = @{
    'ctrl' = 'Ctrl'; 'shift' = 'Shift'; 'alt' = 'Alt'; 'enter' = 'Enter'; 'escape' = 'Escape'; 'tab' = 'Tab'
    'space' = 'Space'; 'backspace' = 'Backspace'; 'delete' = 'Delete'; 'insert' = 'Insert'; 'up' = 'Up arrow'
    'down' = 'Down arrow'; 'left' = 'Left arrow'; 'right' = 'Right arrow'; 'home' = 'Home'; 'end' = 'End'
    'pageup' = 'Page Up'; 'pagedown' = 'Page Down'; 'plus' = 'Plus'; 'minus' = 'Minus'; 'period' = 'Period'
    'comma' = 'Comma'; 'apps' = 'Applications key'; 'slash' = 'Slash'; 'semicolon' = 'Semicolon'
}
function Get-Spoken([string]$chord) {
    $parts = foreach ($part in $chord.Split('+')) {
        if ($spoken.ContainsKey($part)) { $spoken[$part] } else { $part.ToUpperInvariant() }
    }
    return $parts -join '+'
}

# The fixed order: plain keys, F-keys (with their modifiers), Ctrl, Ctrl+Shift, Alt, others; inside a group digits,
# then letters, then the named keys, each in its own order.
function Get-Order([string]$chord) {
    $parts = $chord.Split('+')
    $key = $parts[$parts.Length - 1]
    $modifiers = if ($parts.Length -gt 1) { ($parts[0..($parts.Length - 2)]) -join '+' } else { '' }
    $group = if ($key -match '^f[0-9]+$') { 1 }
        elseif ($modifiers -eq '') { 0 }
        elseif ($modifiers -eq 'ctrl') { 2 }
        elseif ($modifiers -eq 'ctrl+shift') { 3 }
        elseif ($modifiers -match 'alt') { 4 }
        else { 5 }
    $inside = if ($key -match '^[0-9]$') { 'a' + $key }
        elseif ($key -match '^[a-z]$') { 'b' + $key }
        elseif ($key -match '^f([0-9]+)$') { 'c' + ([int]$Matches[1]).ToString('00') }
        else { 'd' + $key }
    return '{0}{1} {2}' -f $group, $inside, $modifiers
}

# "axDown", "axDown and axJSON", "axDown, axJSON and axPreview".
function Join-Names($names) {
    $list = @($names)
    if ($list.Count -le 1) { return "$list" }
    return (($list[0..($list.Count - 2)]) -join ', ') + ' and ' + $list[$list.Count - 1]
}

# Each chord with its apps and its meanings: a meaning is a label, the case aside, with the apps that have it.
$chords = New-Object System.Collections.Generic.List[object]
$byChord = @{}
foreach ($row in $rows) {
    if (-not $byChord.ContainsKey($row.Chord)) {
        $entry = [pscustomobject]@{
            Chord = $row.Chord; Order = (Get-Order $row.Chord); Apps = (New-Object System.Collections.Generic.List[string])
            Meanings = (New-Object System.Collections.Generic.List[object]); Bundle = $bundle[$row.Chord]
        }
        $byChord[$row.Chord] = $entry
        $chords.Add($entry)
    }
    $entry = $byChord[$row.Chord]
    if (-not $entry.Apps.Contains($row.App)) { $entry.Apps.Add($row.App) }
    $meaning = $entry.Meanings | Where-Object { $_.Label -ieq $row.Label } | Select-Object -First 1
    if (-not $meaning) {
        $meaning = [pscustomobject]@{ Label = $row.Label; Apps = (New-Object System.Collections.Generic.List[string]) }
        $entry.Meanings.Add($meaning)
    }
    if (-not $meaning.Apps.Contains($row.App)) { $meaning.Apps.Add($row.App) }
}

# Where a chord is: a bundle key or a window key, and in which apps.
function Get-Scope($entry) {
    $kind = if ($entry.Bundle) { 'Bundle key' } else { 'Window key' }
    if ($entry.Apps.Count -eq $appNames.Count) { return "$kind, in every app" }
    if ($entry.Apps.Count -eq 1) { return "$kind, in $($entry.Apps[0]) alone" }
    return "$kind, in $(Join-Names $entry.Apps)"
}

# The families' file against the inventory.
$known = @{}
foreach ($row in $rows) { $known["$($row.Chord)|$($row.App)|$($row.Label)"] = $true }
if ($families.format -ne 'axkit-key-families 1') { $problems.Add("docs/axKit/key-families.json: the format is '$($families.format)', not 'axkit-key-families 1'") }
if ('proposed', 'settled' -notcontains $families.status) { $problems.Add("docs/axKit/key-families.json: the status '$($families.status)' is neither proposed nor settled") }
$familyIds = @{}
foreach ($family in $families.families) {
    $where = "docs/axKit/key-families.json: family '$($family.id)'"
    if (-not $family.id -or $familyIds.ContainsKey($family.id)) { $problems.Add("${where}: the id is missing or used twice") }
    $familyIds[$family.id] = $true
    foreach ($field in 'pattern', 'match', 'does') {
        if (-not $family.$field) { $problems.Add("${where}: '$field' is missing") }
    }
    foreach ($chord in @($family.windows)) {
        if ($chord -notmatch $family.match) { $problems.Add("${where}: the Windows key '$chord' is not of the pattern") }
        if (-not $byChord.ContainsKey($chord)) { $problems.Add("${where}: the Windows key '$chord' is on no command of the inventory") }
    }
    foreach ($misfit in @($family.misfits)) {
        if ($misfit.chord -notmatch $family.match) { $problems.Add("${where}: the misfit '$($misfit.chord)' is not of the pattern") }
        if (-not $misfit.kind) { $problems.Add("${where}: the misfit '$($misfit.chord)', '$($misfit.does)', does not say its kind") }
        if (@($family.windows) -contains $misfit.chord) { $problems.Add("${where}: '$($misfit.chord)' is both a Windows key and a misfit") }
        $appsOfMisfit = if ($misfit.apps -is [string] -and $misfit.apps -eq 'all') { $appNames } else { @($misfit.apps) }
        if ($appsOfMisfit.Count -eq 0) { $problems.Add("${where}: the misfit '$($misfit.chord)', '$($misfit.does)', names no app") }
        foreach ($name in $appsOfMisfit) {
            if (-not $known.ContainsKey("$($misfit.chord)|$name|$($misfit.does)")) {
                $problems.Add("${where}: the inventory has no command '$($misfit.does)' on '$($misfit.chord)' in $name")
            }
        }
    }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host $_ }
    Write-Host 'The sources of the key map are described at the top of tools/key-map.ps1.'
    exit 1
}

# Sorted by a text of each item, character by character: the same order under every culture and every PowerShell.
function Sort-Ordinal($items, [scriptblock]$text) {
    # Through the pipeline: @() over a generic list throws in Windows PowerShell 5.1 ("Argument types do not match").
    [object[]]$list = $items | ForEach-Object { $_ }
    [string[]]$sortKeys = @($list | ForEach-Object { & $text $_ })
    [Array]::Sort([Array]$sortKeys, [Array]$list, [System.Collections.IComparer][StringComparer]::Ordinal)
    return , $list
}

# "997" for 2, "985" for 14: a count as a text that sorts the larger first.
function Get-Falling([int]$count) { return (999 - $count).ToString('000') }

$same = Sort-Ordinal ($chords | Where-Object { $_.Meanings.Count -eq 1 }) { param($entry) $entry.Order }
$differ = Sort-Ordinal ($chords | Where-Object { $_.Meanings.Count -gt 1 }) { param($entry) (Get-Falling $entry.Meanings.Count) + (Get-Falling $entry.Apps.Count) + $entry.Order }
$bundleInUse = @($chords | Where-Object { $_.Bundle }).Count
$silent = @($bundleOrder | Where-Object { -not $byChord.ContainsKey($_) })

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Key map')
$lines.Add('')
$lines.Add('Written by `tools/key-map.ps1` from `docs/axKit/window-commands.json`, `src/AxKit.Forms/BundleKeys.cs` and')
$lines.Add('`docs/axKit/key-families.json`; never edit it by hand. Every key of every window and what it does in each app.')
$lines.Add('')
$lines.Add("The inventory holds $keyed commands with a key in $($appNames.Count) apps, on $($chords.Count) different keys, counting the")
$lines.Add("second keys some commands answer. $($same.Count) keys have one meaning wherever they are; $($differ.Count) have several.")
$lines.Add("$bundleInUse of them are bundle keys, the ones of ``BundleKeys``, which are the same in every window; the others")
$lines.Add('are window keys, each in its window''s own table.')
$lines.Add('')
$lines.Add('A key has one meaning when every command on it carries the same name in every app. The inventory lists the')
$lines.Add('commands of each app''s window; the keys of a text box or a list itself (the arrows, typing) are not in it.')
$lines.Add('')
$lines.Add('## Keys with one meaning everywhere')
$lines.Add('')
$lines.Add('In this order: plain keys, F-keys, Ctrl, Ctrl+Shift, Alt, others.')
$lines.Add('')
foreach ($entry in $same) {
    $lines.Add("- $(Get-Spoken $entry.Chord). $(Get-Scope $entry): $($entry.Meanings[0].Label).")
}

$lines.Add('')
$lines.Add('## Keys whose meaning differs')
$lines.Add('')
$lines.Add('The most spread first: the number of meanings, then the number of apps.')
foreach ($entry in $differ) {
    $lines.Add('')
    $lines.Add("### $(Get-Spoken $entry.Chord)")
    $lines.Add('')
    $lines.Add("$(Get-Scope $entry). $($entry.Meanings.Count) meanings.")
    $lines.Add('')
    foreach ($meaning in (Sort-Ordinal $entry.Meanings { param($item) (Get-Falling $item.Apps.Count) + $item.Label })) {
        $lines.Add("- $(Join-Names $meaning.Apps): $($meaning.Label).")
    }
}

$lines.Add('')
$lines.Add('## Bundle keys with no command in the inventory')
$lines.Add('')
if ($silent.Count -eq 0) {
    $lines.Add('None: every bundle key is on a command of some window.')
} else {
    $lines.Add('These are keys of `BundleKeys` that work inside a text or a control and are no command of a window''s list.')
    $lines.Add('')
    foreach ($chord in $silent) {
        $lines.Add("- $(Get-Spoken $chord): ``BundleKeys.$($bundle[$chord])``.")
    }
}

$lines.Add('')
$lines.Add("## Key families, $($families.status)")
$lines.Add('')
$lines.Add('From `docs/axKit/key-families.json`. A family is a pattern of keys and the one kind of thing such a key would do;')
$lines.Add('a misfit is a command on a key of the pattern that does another kind of thing today. The families are a draft')
$lines.Add('for the maintainer to settle: no key was moved for them and no test holds a key to its family.')
foreach ($family in $families.families) {
    $ofFamily = @($chords | Where-Object { $_.Chord -match $family.match })
    $misfits = @($family.misfits)
    $lines.Add('')
    $lines.Add("### $($family.pattern): $($family.does)")
    $lines.Add('')
    $count = if ($misfits.Count -eq 0) { 'No misfits' } elseif ($misfits.Count -eq 1) { '1 misfit' } else { "$($misfits.Count) misfits" }
    $lines.Add("$($ofFamily.Count) key$(if ($ofFamily.Count -ne 1) { 's' }) of the inventory. $count.")
    if (@($family.windows).Count -gt 0) {
        $lines.Add('')
        $lines.Add("Windows' own keys, not counted: $(Join-Names @($family.windows | ForEach-Object { Get-Spoken $_ })).")
    }
    if ($misfits.Count -gt 0) {
        $lines.Add('')
        foreach ($misfit in $misfits) {
            $who = if ($misfit.apps -is [string]) { 'every app' } else { Join-Names @($misfit.apps) }
            $lines.Add("- $(Get-Spoken $misfit.chord), ${who}: $($misfit.does). Its kind: $($misfit.kind).")
        }
    }
}

$text = ($lines -join "`r`n") + "`r`n"
$summary = "$($chords.Count) keys, $($same.Count) with one meaning, $($differ.Count) with several, $(@($families.families).Count) families."
if ($Check) {
    $current = if (Test-Path $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    # Line endings aside: the checkout's are native, LF on CI.
    if (($current -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host 'docs/axKit/key-map.md is not what the inventory, BundleKeys and the families give: run tools\key-map.ps1 and commit the page.'
        exit 1
    }

    Write-Host "docs/axKit/key-map.md is current: $summary"
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote docs/axKit/key-map.md: $summary"
