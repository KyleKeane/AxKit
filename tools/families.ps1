<#
.SYNOPSIS
Writes docs\axKit\families.md, the four families of axKit side by side (what starts something, the functions, the
events, the sounds) and the links between them; or checks that the page is current (-Check).

.DESCRIPTION
The page is written from docs\axKit\families.json and never by hand. The file is one data structure with four lists
of the same shape, a family with its members:

    actions     who or what starts something: the user's own hands (a key, typing, a menu, the palette, a hotkey),
                the program under an access layer, an AI, axKit itself, Windows, the automated checks
    functions   the commands of a window by the menu they are in, and the four marks that say how far one reaches
    events      what can happen, by class; each kind of event with what starts it, its sound or none, and its words
    sounds      the sounds by class of earcon; each is one method of src\AxKit.Forms\Sounds.cs

and "keys", which ties each family of docs\axKit\key-families.json to the kinds of event its keys end in.

Every run, with or without -Check, holds the file to the code, so that a new sound, a new place a sound is played
from, a new menu or a new family of keys shows as a line to correct and not as a page that is no longer true:

  - every id is used once; every action, event and sound named by another entry exists;
  - every public method of Sounds is a sound here (its "methods", or its id), and every sound here is a method;
  - every sound is the sound of at least one kind of event;
  - every kind of event a file under src raises through the door (EventDoor.Raise(EventKind.X), and the door's calls
    for the sounds that last) is a kind of the file. Which files raise a kind is read from the code itself and never
    written in the file (its "where" lists, kept by hand until 2026-11-23, broke two builds that day when a new file
    was not named; the maintainer: "building systems that stop hand maintanence");
  - every top menu of docs\axKit\window-commands.json is a family of functions and the other way round;
  - every family of docs\axKit\key-families.json is in "keys" and the other way round.

test.ps1 and build.yml run -Check.

.EXAMPLE
  .\tools\families.ps1          # writes the page
  .\tools\families.ps1 -Check   # exit code 1 when the file does not hold or the page is not what the file gives
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
. (Join-Path $root 'tools\ordinal.ps1')
$filePath = Join-Path $root 'docs\axKit\families.json'
$keyFamiliesPath = Join-Path $root 'docs\axKit\key-families.json'
$inventoryPath = Join-Path $root 'docs\axKit\window-commands.json'
$soundsPath = Join-Path $root 'src\AxKit.Forms\Sounds.cs'
$documentPath = Join-Path $root 'docs\axKit\families.md'

$file = [IO.File]::ReadAllText($filePath) | ConvertFrom-Json
$keyFamilies = [IO.File]::ReadAllText($keyFamiliesPath) | ConvertFrom-Json
$inventory = [IO.File]::ReadAllText($inventoryPath) | ConvertFrom-Json
$problems = New-Object System.Collections.Generic.List[string]

# Names as the page writes them, each between backticks, joined by a comma.
function Get-Named([object]$names) {
    $mark = [string][char]96
    return (@($names | Where-Object { $_ } | ForEach-Object { $mark + $_ + $mark }) -join ', ')
}

function Get-Count([object]$number, [string]$one, [string]$many) {
    if ([int]$number -eq 1) { return "1 $one" }
    return "$number $many"
}

# --- The ids: each used once in its list.
$actionIds = @{}
foreach ($family in $file.actions) {
    foreach ($member in $family.members) {
        if ($actionIds.ContainsKey($member.id)) { $problems.Add("actions: the id '$($member.id)' is used twice.") }
        $actionIds[$member.id] = $family.id
    }
}
$soundIds = @{}
$methodOf = @{}
foreach ($family in $file.sounds) {
    foreach ($member in $family.members) {
        if ($soundIds.ContainsKey($member.id)) { $problems.Add("sounds: the id '$($member.id)' is used twice.") }
        $soundIds[$member.id] = $family.id
        $methods = @($member.id)
        if ($member.PSObject.Properties.Name -contains 'methods') { $methods = @($member.methods) }
        foreach ($method in $methods) { $methodOf[$method] = $member.id }
    }
}
$eventIds = @{}
foreach ($family in $file.events) {
    foreach ($member in $family.members) {
        if ($eventIds.ContainsKey($member.id)) { $problems.Add("events: the id '$($member.id)' is used twice.") }
        $eventIds[$member.id] = $family.id
    }
}

# --- The sounds against Sounds.cs: the public methods, and the files that call them.
$soundsSource = [IO.File]::ReadAllText($soundsPath)
$declared = @([regex]::Matches($soundsSource, '(?m)^\s*public static void (\w+)\(') | ForEach-Object { $_.Groups[1].Value })
foreach ($method in $declared) {
    if (-not $methodOf.ContainsKey($method)) { $problems.Add("sounds: Sounds.$method is a public method of Sounds.cs and no sound of the file names it.") }
}
foreach ($method in $methodOf.Keys) {
    if ($declared -notcontains $method) { $problems.Add("sounds: '$method' is named in the file and Sounds.cs has no public method of that name.") }
}

# kind id -> the files under src that raise it through the door, as "src/..." with forward slashes. Since the door
# (docs\axKit\events.md EV-5, 2026-11-17) no file plays a sound itself: it raises a kind, EventDoor.Raise(EventKind.X),
# and the kind's name in code is its id here with a capital in place of each hyphen (AxKit.Core EventPresenter.Id).
# The two sounds that last, and the done sound of a window that is put away, have calls of their own.
$doorPath = Join-Path $root 'src\AxKit.Forms\EventDoor.cs'
# The door's own calls: the kind each raises, and the sound it plays.
$doorCalls = @{
    'Working'             = @('working', 'Music')
    'BackgroundRunning'   = @('background-running', 'BackgroundChord')
    'TurnDoneThroughMute' = @('turn-done', 'Done')
}
# Each raise found: the file, the kind's id, and the sound when the raise names one (a kind with two sounds), else ''.
$raises = New-Object System.Collections.Generic.List[object]
$sourceRoot = Join-Path $root 'src'
foreach ($source in Get-ChildItem -Path $sourceRoot -Recurse -Filter *.cs) {
    if ($source.FullName -match '\\(bin|obj)\\' -or $source.FullName -eq $soundsPath -or $source.FullName -eq $doorPath) { continue }
    # A comment that names a raise is no raise: each line is cut at its comment first.
    $code = (@([IO.File]::ReadAllLines($source.FullName) | ForEach-Object { ($_ -split '//')[0] })) -join "`n"
    $relative = 'src/' + $source.FullName.Substring($sourceRoot.Length + 1).Replace('\', '/')
    foreach ($match in [regex]::Matches($code, '\bEventDoor\.Raise\(\s*EventKind\.(\w+)\s*(?:,\s*EventSound\.(\w+))?')) {
        $kindId = [regex]::Replace($match.Groups[1].Value, '(?<!^)([A-Z])', '-$1').ToLowerInvariant()
        $raises.Add([pscustomobject]@{ File = $relative; Kind = $kindId; Sound = $match.Groups[2].Value })
    }
    foreach ($match in [regex]::Matches($code, '\bEventDoor\.(Working|BackgroundRunning|TurnDoneThroughMute)\(')) {
        $call = $doorCalls[$match.Groups[1].Value]
        $raises.Add([pscustomobject]@{ File = $relative; Kind = $call[0]; Sound = $call[1] })
    }
}

# --- The events: what each names exists; every file that raises a kind is named by that kind.
$eventsOfSound = @{}
$whereOfEvent = @{}
$soundsOfEvent = @{}
foreach ($family in $file.events) {
    foreach ($member in $family.members) {
        $whereOfEvent[$member.id] = @{}
        $soundsOfEvent[$member.id] = @($member.sounds | Where-Object { $_ })
        foreach ($start in @($member.starts | Where-Object { $_ })) {
            if (-not $actionIds.ContainsKey($start)) { $problems.Add("events: '$($member.id)' is started by '$start', which is no action of the file.") }
        }
        foreach ($sound in $soundsOfEvent[$member.id]) {
            if (-not $soundIds.ContainsKey($sound)) { $problems.Add("events: '$($member.id)' has the sound '$sound', which is no sound of the file."); continue }
            if (-not $eventsOfSound.ContainsKey($sound)) { $eventsOfSound[$sound] = New-Object System.Collections.Generic.List[string] }
            $eventsOfSound[$sound].Add($member.id)
        }
    }
}
foreach ($sound in $soundIds.Keys) {
    if (-not $eventsOfSound.ContainsKey($sound)) { $problems.Add("sounds: '$sound' is the sound of no kind of event.") }
}

# sound id -> the files under src it is played from, for the page: the sound a raise names, else its kind's one sound.
$callers = @{}
foreach ($raise in $raises) {
    if (-not $whereOfEvent.ContainsKey($raise.Kind)) { $problems.Add("events: $($raise.File) raises '$($raise.Kind)', which is no kind of event of the file."); continue }
    $whereOfEvent[$raise.Kind][$raise.File] = $true
    $played = $raise.Sound
    if (-not $played -and $soundsOfEvent[$raise.Kind].Count -gt 0) { $played = $soundsOfEvent[$raise.Kind][0] }
    if (-not $played) { continue }
    if ($soundsOfEvent[$raise.Kind] -cnotcontains $played) {
        $line = "events: $($raise.File) raises '$($raise.Kind)' with the sound '$played', which the kind does not have."
        if (-not $problems.Contains($line)) { $problems.Add($line) }
        continue
    }
    if (-not $callers.ContainsKey($played)) { $callers[$played] = New-Object System.Collections.Generic.List[string] }
    if (-not $callers[$played].Contains($raise.File)) { $callers[$played].Add($raise.File) }
}

# --- The functions against the inventory: the top menus, and the commands that are a key alone.
$menuCount = @{}
$menuMarks = @{}
$menuApps = @{}
$aloneCount = 0
$total = 0
foreach ($app in $inventory.apps) {
    foreach ($command in $app.commands) {
        $total++
        if ($command.keyOnly) { $aloneCount++; continue }
        $top = ($command.menu -split ', ')[0]
        if (-not $menuCount.ContainsKey($top)) { $menuCount[$top] = 0; $menuMarks[$top] = @{}; $menuApps[$top] = @{} }
        $menuCount[$top]++
        $menuMarks[$top][$command.mark] = 1 + [int]$menuMarks[$top][$command.mark]
        $menuApps[$top][$app.app] = $true
    }
}
$markIds = @($file.marks | ForEach-Object { $_.id })
$menusOfFile = @{}
foreach ($family in $file.functions) {
    if ($family.PSObject.Properties.Name -contains 'menu') {
        $menusOfFile[$family.menu] = $true
        if (-not $menuCount.ContainsKey($family.menu)) { $problems.Add("functions: '$($family.id)' names the menu '$($family.menu)', which no window of the inventory has.") }
    }
}
foreach ($menu in $menuCount.Keys) {
    if (-not $menusOfFile.ContainsKey($menu)) { $problems.Add("functions: the menu '$menu' of the inventory is no family of functions in the file.") }
}
foreach ($mark in $file.marks) {
    foreach ($kind in @($mark.events | Where-Object { $_ })) {
        if (-not $eventIds.ContainsKey($kind)) { $problems.Add("marks: '$($mark.id)' ends in '$kind', which is no kind of event of the file.") }
    }
}

# --- The keys: every family of key-families.json, once, and each event it names exists.
$keyFamilyOf = @{}
foreach ($family in $keyFamilies.families) { $keyFamilyOf[$family.id] = $family }
$keysSeen = @{}
foreach ($entry in $file.keys) {
    if (-not $keyFamilyOf.ContainsKey($entry.family)) { $problems.Add("keys: '$($entry.family)' is no family of key-families.json.") }
    if ($keysSeen.ContainsKey($entry.family)) { $problems.Add("keys: '$($entry.family)' is there twice.") }
    $keysSeen[$entry.family] = $true
    foreach ($kind in @($entry.events | Where-Object { $_ })) {
        if (-not $eventIds.ContainsKey($kind)) { $problems.Add("keys: '$($entry.family)' ends in '$kind', which is no kind of event of the file.") }
    }
}
foreach ($family in $keyFamilies.families) {
    if (-not $keysSeen.ContainsKey($family.id)) { $problems.Add("keys: the family '$($family.id)' of key-families.json is not in the file.") }
}

if ($problems.Count -gt 0) {
    foreach ($problem in $problems) { Write-Host $problem }
    Write-Host "docs/axKit/families.json does not hold: $(Get-Count $problems.Count 'line' 'lines') above to correct."
    exit 1
}

# --- The page.
$eventById = @{}
foreach ($family in $file.events) { foreach ($member in $family.members) { $eventById[$member.id] = $member } }
$soundById = @{}
foreach ($family in $file.sounds) { foreach ($member in $family.members) { $soundById[$member.id] = $member } }
$actionById = @{}
foreach ($family in $file.actions) { foreach ($member in $family.members) { $actionById[$member.id] = $member } }

function Get-SoundText([object]$kind) {
    $names = @($kind.sounds | Where-Object { $_ })
    if ($names.Count -eq 0) { return 'no sound' }
    return 'sound ' + ((Get-Named $names) -replace ', ', ' or ')
}

$eventCount = 0
$withSound = 0
foreach ($family in $file.events) { foreach ($member in $family.members) { $eventCount++; if (@($member.sounds | Where-Object { $_ }).Count -gt 0) { $withSound++ } } }
$summary = "$(Get-Count $eventCount 'kind of event' 'kinds of event'), $withSound with a sound, $($eventCount - $withSound) without; $(Get-Count $soundIds.Count 'sound' 'sounds'); $(Get-Count $file.functions.Count 'family of functions' 'families of functions'); $(Get-Count $actionIds.Count 'kind of start' 'kinds of start')."

$p = New-Object System.Collections.Generic.List[string]
$p.Add('# The four families: what starts something, the functions, the events, the sounds')
$p.Add('')
$p.Add('Written by `tools/families.ps1` from `docs/axKit/families.json`; never by hand. ' + $file.note)
$p.Add('')
$p.Add("Status: $($file.status). In numbers: $summary")
$p.Add('')
$p.Add('## How to read it')
$p.Add('')
$p.Add($file.chain)
$p.Add('')
$p.Add('Each thing has a short name to say or to search for, written `like this`: an event, a sound, a way of starting.')
$p.Add('')
$p.Add('## 1. What starts something')
foreach ($family in $file.actions) {
    $p.Add('')
    $p.Add("### $($family.title)")
    $p.Add('')
    $p.Add($family.learn)
    $p.Add('')
    foreach ($member in $family.members) {
        $p.Add("- ``$($member.id)``: $($member.title). $($member.what)")
    }
}
$p.Add('')
$p.Add('## 2. The functions')
$p.Add('')
$p.Add("A function is a command of a window (``docs/axKit/command-language.md``): $total in the inventory, of which $aloneCount are a key with no menu item. The menu a command is in is its family; its mark says how far it reaches.")
$p.Add('')
$p.Add('### The marks, and the events a command of each mark ends in')
$p.Add('')
foreach ($mark in $file.marks) {
    $p.Add("- **$($mark.id)**: $($mark.learn)")
    $p.Add("  - Ends in: $(Get-Named $mark.events).")
}
$p.Add('')
$p.Add('### The families, by menu')
$p.Add('')
foreach ($family in $file.functions) {
    if ($family.PSObject.Properties.Name -contains 'menu') {
        $marks = @($markIds | Where-Object { $menuMarks[$family.menu].ContainsKey($_) } | ForEach-Object { "$_ $($menuMarks[$family.menu][$_])" }) -join ', '
        $p.Add("- ``$($family.id)``, the $($family.menu) menu: $($family.learn)")
        $p.Add("  - $(Get-Count $menuCount[$family.menu] 'command' 'commands') in $(Get-Count $menuApps[$family.menu].Count 'app' 'apps'): $marks.")
    } else {
        $p.Add("- ``$($family.id)``: $($family.learn)")
        if ($family.id -eq 'keys-alone') { $p.Add("  - $(Get-Count $aloneCount 'row' 'rows') of the inventory.") }
    }
}
$p.Add('')
$p.Add('## 3. The events')
foreach ($family in $file.events) {
    $p.Add('')
    $p.Add("### $($family.title)")
    $p.Add('')
    $p.Add($family.learn)
    $p.Add('')
    foreach ($member in $family.members) {
        $p.Add("- ``$($member.id)``: $($member.when)")
        $p.Add("  - Started by: $(Get-Named $member.starts).")
        $p.Add("  - Heard: $(Get-SoundText $member).")
        $p.Add("  - Said: $($member.words)")
        if ($member.PSObject.Properties.Name -contains 'apps') { $p.Add("  - In: $($member.apps)") }
        if ($member.PSObject.Properties.Name -contains 'note') { $p.Add("  - Note: $($member.note)") }
    }
}
$p.Add('')
$p.Add('## 4. The sounds')
foreach ($family in $file.sounds) {
    $p.Add('')
    $p.Add("### $($family.title)")
    $p.Add('')
    $p.Add($family.learn)
    $p.Add('')
    foreach ($member in $family.members) {
        $p.Add("- ``$($member.id)``: $($member.heard)")
        $p.Add("  - Chosen: $($member.chosen)")
        $p.Add("  - Switch: $($member.switch)")
        $p.Add("  - The sound of: $(Get-Named $eventsOfSound[$member.id]).")
        if ($callers.ContainsKey($member.id)) {
            $p.Add("  - Played from $(Get-Count $callers[$member.id].Count 'file' 'files'): $((@(Sort-Ordinal $callers[$member.id]) | ForEach-Object { $_ -replace '^src/', '' }) -join ', ').")
        }
    }
}
$p.Add('')
$p.Add('## 5. The keys, and what you can hear after one')
$p.Add('')
$p.Add('Each family of keys (`docs/axKit/key-groups.md`) with the kinds of event its keys end in, and so the sounds that can follow a key of that family.')
$p.Add('')
foreach ($entry in $file.keys) {
    $family = $keyFamilyOf[$entry.family]
    $p.Add("- ``$($entry.family)``, $($family.pattern): $($family.does).")
    foreach ($kind in @($entry.events | Where-Object { $_ })) {
        $p.Add("  - ``$kind``: $(Get-SoundText $eventById[$kind]).")
    }
    if ($entry.PSObject.Properties.Name -contains 'note') { $p.Add("  - Note: $($entry.note)") }
}
$p.Add('')
$p.Add('## 6. Events with no sound')
$p.Add('')
$p.Add('Every kind of event that has words or a change on the screen and no sound of its own today, by class. A sound is added only when the maintainer has chosen it by ear.')
foreach ($family in $file.events) {
    $silent = @($family.members | Where-Object { @($_.sounds | Where-Object { $_ }).Count -eq 0 })
    if ($silent.Count -eq 0) { continue }
    $p.Add('')
    $p.Add("- $($family.title):")
    foreach ($member in $silent) { $p.Add("  - ``$($member.id)``: $($member.when)") }
}
$text = ($p -join "`r`n") + "`r`n"

if ($Check) {
    $current = if (Test-Path $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    # Line endings aside: the checkout's are native, LF on CI.
    if (($current -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host 'docs/axKit/families.md is not what families.json and the code give: run tools\families.ps1 and commit the page.'
        exit 1
    }

    Write-Host "docs/axKit/families.md is current: $summary"
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote docs/axKit/families.md: $summary"
