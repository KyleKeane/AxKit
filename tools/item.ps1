<#
.SYNOPSIS
  Writes, notes and finishes an item of the priorities list (CLAUDE.md rule 9), so that an item is never written
  with a script made up for the occasion. It commits and pushes nothing: the next push of the code does (the
  pre-push hook, tools\sync-repos.ps1).

.DESCRIPTION
  The list is <parent>\axkit-management\priorities beside this repository, one JSON file per item. Four forms:

    new -Title <title> -App <app> -Priority <priority> -Where <where it is written> -Notes <notes>
        writes a new item as the list's items are (docs/axTask/forms-framework.md section 13): the link to the
        list's list.json, the Title, and each of the list's "item fields" with the value a new item starts with:
        Done not checked, Started, Tags and Due empty. The file is named from the title as axTask
        names it (AxKit.Core ItemFiles.FileStem: a character Windows forbids becomes a space, no full stop at the
        end, 120 characters at most). Refused when a file of that name exists, and when the priority is not one of
        the list's (High, Normal, Low).

    note -Name <words of the title> -Text <text>
        puts the text, with today's date before it, in front of the item's Notes.

    done -Name <words of the title>
        checks the item's Done. (A commit whose message says "Item done: <words>" does the same by itself.)

    archive
        moves every done item into the top folder's Archive, in a list of the same name (Archive\priorities, with a
        copy of list.json), where axTask shows it among the archived and no list or smart list shows it; the sync of
        every push (tools\sync-repos.ps1) runs it before the lists are committed (the maintainer's ask of 2026-11-23).

  The words name an item as a commit message's do (tools\items.ps1, which tools\item-of-commit.ps1 reads too): the
  items whose title holds them, without regard to case; they must name exactly one. Each form prints one line with
  the item's title and prints nothing else of the item. Where the lists are not beside the repository the script
  says so and ends with 1. Exit code 0 when the item was written, 1 when it was refused.

.EXAMPLE
  .\tools\item.ps1 new -Title 'axCalc: the history says its length' -App axCalc -Priority Low -Where 'docs/axCalc/SPEC.md AC-5' -Notes 'Small. Proposed Low: nothing waits on it.'
  .\tools\item.ps1 note -Name 'history says its length' -Text 'The words are decided: "12 entries".'
  .\tools\item.ps1 done -Name 'history says its length'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)][ValidateSet('new', 'note', 'done', 'archive')][string]$Form,
    [string]$Title,
    [string]$App,
    [string]$Priority,
    [string]$Where,
    [string]$Notes,
    [string]$Name,
    [string]$Text,
    # Another list than the one beside the repository (the tests').
    [string]$Lists,
    # The code repository whose docs\axKit\approval-key.pub verifies an approval; this script's own by default (the tests' own).
    [string]$PinnedFrom
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'items.ps1')

function Refuse([string]$why) {
    Write-Host "Not written: $why"
    exit 1
}

if (-not $Lists) {
    . (Join-Path $PSScriptRoot 'siblings.ps1')
    $Lists = Join-Path (Get-BesideFolder 'axkit-management') 'priorities'
}

if (-not (Test-Path -LiteralPath $Lists)) {
    Refuse 'the priorities list is not beside this repository (..\axkit-management\priorities). The lists are on the maintainer''s computer alone.'
}

# The file name a title gives: AxKit.Core ItemFiles.FileStem, in this script's words.
function Get-FileStem([string]$of) {
    $invalid = [IO.Path]::GetInvalidFileNameChars()
    $chars = $of.Trim().ToCharArray() | ForEach-Object { if ($invalid -contains $_ -or [char]::IsControl($_)) { ' ' } else { $_ } }
    $stem = ((-join $chars) -split ' ' | Where-Object { $_ }) -join ' '
    $stem = $stem.TrimEnd('.', ' ')
    if ($stem.Length -gt 120) { $stem = $stem.Substring(0, 120).TrimEnd('.', ' ') }
    $devices = @('CON', 'PRN', 'AUX', 'NUL') + (1..9 | ForEach-Object { "COM$_"; "LPT$_" })
    if ($devices -contains $stem.Split('.')[0].TrimEnd()) { $stem = '_' + $stem }
    if ($stem.Length -eq 0) { return 'Untitled' }
    return $stem
}

# The one item the words name; none and several are refused, as a commit message's are.
function Get-NamedItem([string]$words) {
    if (-not $words) { Refuse 'say which item with -Name <words of its title>.' }
    $found = @(Find-ListItems @(Get-ListItems $Lists) $words)
    if ($found.Count -eq 0) { Refuse ("no item's title holds '$words'." + (Get-NearestTitles @(Get-ListItems $Lists) $words)) }
    if ($found.Count -gt 1) {
        $titles = ($found | Select-Object -First 6 | ForEach-Object { '    ' + $_.Title }) -join "`n"
        Refuse "'$words' names $($found.Count) items; use more of the title:`n$titles"
    }
    return $found[0]
}

if (Test-EarlierList $Lists) {
    Refuse 'the priorities list is written in axTask''s earlier format, which the scripts no longer read. Open the list in axTask and convert it (Enter on the list) first.'
}

# The list's configuration: the fields of its items, in their order (list.json, "item fields").
$listConfig = Get-ListConfig $Lists

$today = (Get-Date).ToString('yyyy-MM-dd')

# Outside reports (docs/axKit/outside-reports.md OR-7.2): an item or a note that names a report is written only when the
# maintainer approved an entry of that report, with his passphrase, that names the item by its title; the approval is
# verified with the key pinned in this repository. Everything else of a report stays in the outside reports list.
. (Join-Path $PSScriptRoot 'approvals.ps1')
function Assert-ApprovedFromOutside([string]$itemTitle, [string[]]$texts) {
    $reports = @()
    foreach ($one in $texts) { $reports += @(Get-ReportNumbers $one) }
    if ($reports.Count -eq 0) { return }
    $repositoryRoot = if ($PinnedFrom) { $PinnedFrom } else { Split-Path $PSScriptRoot -Parent }
    $outsideList = Join-Path (Split-Path $Lists -Parent) 'outside-reports'
    $entries = @(Get-OutsideEntries $outsideList (Get-PinnedApprovalKey $repositoryRoot))
    foreach ($report in ($reports | Select-Object -Unique)) {
        if (-not (Test-ApprovedForItem $entries $report $itemTitle)) {
            Refuse "report $report is outside work: write its entry with tools\outside-report.ps1, and this item follows only once the maintainer has approved an entry that names '$itemTitle' (docs/axKit/outside-reports.md)."
        }
    }
}

if ($Form -eq 'new') {
    foreach ($needed in @(@('Title', $Title), @('App', $App), @('Priority', $Priority), @('Where', $Where), @('Notes', $Notes))) {
        if (-not "$($needed[1])".Trim()) { Refuse "a new item needs -$($needed[0])." }
    }

    # The fields of a new item are the list's own, from its configuration (list.json, "item fields"): the item is
    # the link to that file, its Title, and each field with the value a new item starts with.
    if ($null -eq $listConfig) { Refuse 'the list has no configuration that reads (list.json, format axkit-list 2), so the fields of a new item are not known.' }
    $titles = @(Get-ItemFieldTitles $listConfig)

    Assert-ApprovedFromOutside $Title.Trim() @($Title, $Where, $Notes)
    $document = New-ListItem $Title.Trim() $listConfig

    $priorityField = @($listConfig.'item fields') | Where-Object { "$($_.title)" -ieq 'Priority' } | Select-Object -First 1
    $priorities = @($priorityField.options)
    $chosen = @($priorities | Where-Object { $_ -ieq $Priority })
    if ($chosen.Count -ne 1) { Refuse "the priority '$Priority' is not one of the list's: $($priorities -join ', ')." }

    $values = [ordered]@{ 'Priority' = $chosen[0]; 'App' = $App.Trim(); 'Where it is written' = $Where.Trim(); 'Notes' = $Notes.Trim() }
    foreach ($key in $values.Keys) {
        if ($titles -notcontains $key) { Refuse "the list's items have no field '$key' (list.json, ""item fields"")." }
        $document = Set-ItemValue $document $key $values[$key] $listConfig
    }

    $file = Join-Path $Lists ((Get-FileStem $Title) + '.json')
    if (Test-Path -LiteralPath $file) { Refuse "an item of that name exists: $([IO.Path]::GetFileName($file))." }
    Save-ListItem $file $document
    Write-Host "Priorities list, new item: $($Title.Trim())"
    exit 0
}

if ($Form -eq 'archive') {
    # The top folder's Archive (axTask's, docs/axKit/item-apps.md IA-D4), which axTask leaves out of every list and
    # smart list, with a list of the same name as this one: the done items move there, with a copy of this list's
    # list.json, so that they still read as items, in axTask's Archive and for tools\reports-check.ps1.
    $archive = Join-Path (Join-Path (Split-Path $Lists -Parent) 'Archive') (Split-Path $Lists -Leaf)
    $finished = @(Get-ListItems $Lists | Where-Object { Get-ItemValue $_.Document 'Done' })
    if ($finished.Count -eq 0) {
        Write-Host 'Priorities list, archived: no item is done'
        exit 0
    }

    New-Item -ItemType Directory -Force -Path $archive | Out-Null
    Copy-Item -LiteralPath (Join-Path $Lists 'list.json') -Destination (Join-Path $archive 'list.json') -Force
    foreach ($one in $finished) {
        # A name the Archive holds already gets " - 2", as axTask names a file (AxKit.Core ItemFiles.FreePath).
        $stem = [IO.Path]::GetFileNameWithoutExtension($one.File)
        $to = Join-Path $archive "$stem.json"
        $count = 2
        while (Test-Path -LiteralPath $to) {
            $to = Join-Path $archive "$stem - $count.json"
            $count++
        }

        Move-Item -LiteralPath $one.File -Destination $to
    }

    Write-Host "Priorities list, archived: $($finished.Count) done item(s), into Archive\$(Split-Path $Lists -Leaf)"
    exit 0
}

$item = Get-NamedItem $Name

# A field the item's file does not hold yet is one of the item's all the same when the list's configuration names it
# (it is shown empty until it is given a value); a name neither has is refused.
function Assert-ItemField([string]$named) {
    if (-not (Test-ItemValue $item.Document $named) -and (@(Get-ItemFieldTitles $listConfig) -notcontains $named)) {
        Refuse "the item has no $named field: $($item.Title)"
    }
}

if ($Form -eq 'note') {
    if (-not "$Text".Trim()) { Refuse 'a note needs -Text.' }
    Assert-ApprovedFromOutside $item.Title @($Text)
    Assert-ItemField 'Notes'
    $sentence = $Text.Trim()
    if ($sentence -notmatch '[.?!)"]$') { $sentence += '.' }
    $noted = ("${today}: $sentence " + "$(Get-ItemValue $item.Document 'Notes')").TrimEnd()
    Save-ListItem $item.File (Set-ItemValue $item.Document 'Notes' $noted $listConfig)
    Write-Host "Priorities list, note of $today in front of the Notes: $($item.Title)"
    exit 0
}

# done
Assert-ItemField 'Done'
if (Get-ItemValue $item.Document 'Done') {
    Write-Host "Priorities list, already done: $($item.Title)"
    exit 0
}
Save-ListItem $item.File (Set-ItemValue $item.Document 'Done' $true $listConfig)
Write-Host "Priorities list, marked done: $($item.Title)"
exit 0
