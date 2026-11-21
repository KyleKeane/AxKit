<#
.SYNOPSIS
  Writes, notes and finishes an item of the priorities list (CLAUDE.md rule 9), so that an item is never written
  with a script made up for the occasion. It commits and pushes nothing: the next push of the code does (the
  pre-push hook, tools\sync-repos.ps1).

.DESCRIPTION
  The list is <parent>\axkit-management\priorities beside this repository, one JSON file per item. Three forms:

    new -Title <title> -App <app> -Priority <priority> -Where <where it is written> -Notes <notes>
        writes a new item in the shape the list gives its new items (the "new item" fields of the list's list.json,
        the Title in front): Done not checked, Started, Tags and Due empty. The file is named from the title as axTask
        names it (AxKit.Core ItemFiles.FileStem: a character Windows forbids becomes a space, no full stop at the
        end, 120 characters at most). Refused when a file of that name exists, and when the priority is not one of
        the list's (High, Normal, Low).

    note -Name <words of the title> -Text <text>
        puts the text, with today's date before it, in front of the item's Notes.

    done -Name <words of the title>
        checks the item's Done. (A commit whose message says "Item done: <words>" does the same by itself.)

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
    [Parameter(Mandatory = $true, Position = 0)][ValidateSet('new', 'note', 'done')][string]$Form,
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
    $repository = Split-Path $PSScriptRoot -Parent
    $Lists = Join-Path (Split-Path $repository -Parent) 'axkit-management\priorities'
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
    if ($found.Count -eq 0) { Refuse "no item's title holds '$words'." }
    if ($found.Count -gt 1) {
        $titles = ($found | Select-Object -First 6 | ForEach-Object { '    ' + $_.Title }) -join "`n"
        Refuse "'$words' names $($found.Count) items; use more of the title:`n$titles"
    }
    return $found[0]
}

function Get-Field($document, [string]$named) {
    return ($document.fields | Where-Object { $_.title -eq $named } | Select-Object -First 1)
}

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

    # The fields of a new item are the list's own, from its list.json; a list without them gets the fields the
    # priorities list has had since 2026-11-10.
    $template = $null
    $listFile = Join-Path $Lists 'list.json'
    if (Test-Path -LiteralPath $listFile) {
        $template = (Get-Content -LiteralPath $listFile -Raw | ConvertFrom-Json).'new item'
    }
    if (-not $template) {
        $template = @(
            [pscustomobject]@{ title = 'Done'; type = 'check'; value = $false },
            [pscustomobject]@{ title = 'Started'; type = 'date'; value = '' },
            [pscustomobject]@{ title = 'Priority'; type = 'choice'; value = ''; options = @('High', 'Normal', 'Low') },
            [pscustomobject]@{ title = 'App'; type = 'text'; value = '' },
            [pscustomobject]@{ title = 'Tags'; type = 'text'; value = '' },
            [pscustomobject]@{ title = 'Due'; type = 'date'; value = '' },
            [pscustomobject]@{ title = 'Where it is written'; type = 'text'; value = '' },
            [pscustomobject]@{ title = 'Notes'; type = 'longtext'; value = '' }
        )
    }

    Assert-ApprovedFromOutside $Title.Trim() @($Title, $Where, $Notes)
    $fields = @([pscustomobject]@{ title = 'Title'; type = 'text'; value = $Title.Trim() }) + @($template | Where-Object { $_.title -ne 'Title' })
    $document = [pscustomobject]@{ format = 'axkit-item 1'; fields = $fields }

    $priorities = @((Get-Field $document 'Priority').options)
    $chosen = @($priorities | Where-Object { $_ -ieq $Priority })
    if ($chosen.Count -ne 1) { Refuse "the priority '$Priority' is not one of the list's: $($priorities -join ', ')." }

    $values = @{ 'Priority' = $chosen[0]; 'App' = $App.Trim(); 'Where it is written' = $Where.Trim(); 'Notes' = $Notes.Trim() }
    foreach ($key in $values.Keys) {
        $field = Get-Field $document $key
        if (-not $field) { Refuse "the list's new items have no field '$key' (list.json, ""new item"")." }
        $field.value = $values[$key]
    }

    $file = Join-Path $Lists ((Get-FileStem $Title) + '.json')
    if (Test-Path -LiteralPath $file) { Refuse "an item of that name exists: $([IO.Path]::GetFileName($file))." }
    Save-ListItem $file $document
    Write-Host "Priorities list, new item: $($Title.Trim())"
    exit 0
}

$item = Get-NamedItem $Name

if ($Form -eq 'note') {
    if (-not "$Text".Trim()) { Refuse 'a note needs -Text.' }
    Assert-ApprovedFromOutside $item.Title @($Text)
    $field = Get-Field $item.Document 'Notes'
    if (-not $field) { Refuse "the item has no Notes field: $($item.Title)" }
    $sentence = $Text.Trim()
    if ($sentence -notmatch '[.?!)"]$') { $sentence += '.' }
    $field.value = ("${today}: $sentence " + "$($field.value)").TrimEnd()
    Save-ListItem $item.File $item.Document
    Write-Host "Priorities list, note of $today in front of the Notes: $($item.Title)"
    exit 0
}

# done
$field = Get-Field $item.Document 'Done'
if (-not $field) { Refuse "the item has no Done field: $($item.Title)" }
if ($field.value) {
    Write-Host "Priorities list, already done: $($item.Title)"
    exit 0
}
$field.value = $true
Save-ListItem $item.File $item.Document
Write-Host "Priorities list, marked done: $($item.Title)"
exit 0
