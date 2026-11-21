<#
.SYNOPSIS
  Writes the triage entry of an outside report (docs/axKit/outside-reports.md OR-3, OR-4, OR-7.4): the assistant's one
  way to write into the outside reports list. It never writes an approval.

.DESCRIPTION
  The list is <parent>\axkit-management\outside-reports, one JSON item per report, made with its list.json when
  missing. Four forms:

    new -Title <the app and the matter, in the assistant's words> -Report 'KyleKeane/AxKit-reports issue <n>'
        a new entry, State "To review", Received today. Refused when an entry already names the report.

    review -Name <words of the entry's title> [-Review <harm review>] [-Category <category>] [-Item <item title>]
                                              [-Proposed <scope>] [-Priority High|Normal|Low]
        writes the fields given. The categories: Same as an item, Addition to an item, New item, Not actionable,
        Declined for harm.

    ready -Name <words>
        State "Awaiting approval", after checking the entry is whole (OR-4.5): a Harm review; a category; for Same as an
        item and Addition to an item an Item that is the exact title of a priorities item, for New item an Item no
        priorities item has yet and a Proposed priority; and a Proposed for those three.

    done -Name <words>
        State "Done", for an entry Approved whose approval verifies, once its work is finished and the report answered.

  An entry that is Approved, Declined or Done is refused every change but done: a change would void the signature
  (OR-5.3). Approval, Approved on and the states Approved and Declined are written by axTask alone, with the
  maintainer's passphrase. Exit code 0 when written, 1 when refused. The script stays ASCII.

.EXAMPLE
  .\tools\outside-report.ps1 new -Title 'axStart: says nothing at its first launch' -Report 'KyleKeane/AxKit-reports issue 15'
  .\tools\outside-report.ps1 review -Name 'says nothing at its first' -Review 'Nothing found.' -Category 'New item' -Item 'axStart: ...' -Proposed '...' -Priority High
  .\tools\outside-report.ps1 ready -Name 'says nothing at its first'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)][ValidateSet('new', 'review', 'ready', 'done')][string]$Form,
    [string]$Title,
    [string]$Report,
    [string]$Name,
    [string]$Review,
    [string]$Category,
    [string]$Item,
    [string]$Proposed,
    [string]$Priority,
    # The folder that holds the lists; <parent>\axkit-management by default (the tests' own).
    [string]$Management,
    # The code repository whose pinned key verifies an approval; this script's own by default.
    [string]$Repository
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'items.ps1')
. (Join-Path $PSScriptRoot 'approvals.ps1')

function Refuse([string]$why) {
    Write-Host "Not written: $why"
    exit 1
}

if (-not $Repository) { $Repository = Split-Path $PSScriptRoot -Parent }
if (-not $Management) { $Management = Join-Path (Split-Path $Repository -Parent) 'axkit-management' }
$priorities = Join-Path $Management 'priorities'
$outside = Join-Path $Management 'outside-reports'
if (-not (Test-Path -LiteralPath $Management)) {
    Refuse 'the management lists are not beside this repository (..\axkit-management).'
}

$categories = @('Same as an item', 'Addition to an item', 'New item', 'Not actionable', 'Declined for harm')
$acting = @('Same as an item', 'Addition to an item', 'New item')
$states = @('To review', 'Awaiting approval', 'Approved', 'Declined', 'Done')

function Get-ListTemplate {
    return @(
        [pscustomobject]@{ title = 'Report'; type = 'text'; value = '' },
        [pscustomobject]@{ title = 'Received'; type = 'date'; value = '' },
        [pscustomobject]@{ title = 'State'; type = 'choice'; value = 'To review'; options = $states },
        [pscustomobject]@{ title = 'Harm review'; type = 'longtext'; value = '' },
        [pscustomobject]@{ title = 'Category'; type = 'choice'; value = ''; options = $categories },
        [pscustomobject]@{ title = 'Item'; type = 'text'; value = '' },
        [pscustomobject]@{ title = 'Proposed'; type = 'longtext'; value = '' },
        [pscustomobject]@{ title = 'Proposed priority'; type = 'choice'; value = ''; options = @('High', 'Normal', 'Low') },
        [pscustomobject]@{ title = 'Approval'; type = 'text'; value = '' },
        [pscustomobject]@{ title = 'Approved on'; type = 'date'; value = '' }
    )
}

# The list itself, made once: its notes say what it is, and its new items have the triage fields.
if (-not (Test-Path -LiteralPath $outside)) {
    New-Item -ItemType Directory -Force $outside | Out-Null
}
$listFile = Join-Path $outside 'list.json'
if (-not (Test-Path -LiteralPath $listFile)) {
    $list = [pscustomobject]@{
        format = 'axkit-list 1'
        fields = @(
            [pscustomobject]@{ title = 'Section'; type = 'text'; value = 'Lists' },
            [pscustomobject]@{ title = 'Notes'; type = 'longtext'; value = 'Outside reports: one entry for each report of KyleKeane/AxKit-reports, with the assistant''s harm review and the scope it proposes. Nothing of a report is worked on until you approve its entry with your passphrase (Approve this report, in the Context menu). docs/axKit/outside-reports.md.' }
        )
        'new item' = @(Get-ListTemplate)
    }
    Save-ListItem $listFile $list
}

function Get-Field($document, [string]$named) {
    return ($document.fields | Where-Object { $_.title -eq $named } | Select-Object -First 1)
}

function Get-FileStem([string]$of) {
    $invalid = [IO.Path]::GetInvalidFileNameChars()
    $chars = $of.Trim().ToCharArray() | ForEach-Object { if ($invalid -contains $_ -or [char]::IsControl($_)) { ' ' } else { $_ } }
    $stem = ((-join $chars) -split ' ' | Where-Object { $_ }) -join ' '
    $stem = $stem.TrimEnd('.', ' ')
    if ($stem.Length -gt 120) { $stem = $stem.Substring(0, 120).TrimEnd('.', ' ') }
    if ($stem.Length -eq 0) { return 'Untitled' }
    return $stem
}

$entries = @(Get-ListItems $outside)

if ($Form -eq 'new') {
    if (-not "$Title".Trim()) { Refuse 'a new entry needs -Title, in your own words.' }
    $numbers = @(Get-ReportNumbers $Report)
    if ($numbers.Count -eq 0) { Refuse "-Report must be written 'KyleKeane/AxKit-reports issue <n>' (or 'issues <n> and <m>')." }
    foreach ($entry in $entries) {
        $theirs = @(Get-ReportNumbers ([string](Get-Field $entry.Document 'Report').value))
        foreach ($number in $numbers) {
            if ($theirs -contains $number) { Refuse "report $number already has its entry: $($entry.Title)." }
        }
    }
    $fields = @([pscustomobject]@{ title = 'Title'; type = 'text'; value = $Title.Trim() }) + @(Get-ListTemplate)
    $document = [pscustomobject]@{ format = 'axkit-item 1'; fields = $fields }
    (Get-Field $document 'Report').value = $Report.Trim()
    (Get-Field $document 'Received').value = (Get-Date).ToString('yyyy-MM-dd')
    $file = Join-Path $outside ((Get-FileStem $Title) + '.json')
    if (Test-Path -LiteralPath $file) { Refuse "an entry of that name exists: $([IO.Path]::GetFileName($file))." }
    Save-ListItem $file $document
    Write-Host "Outside reports, new entry to review: $($Title.Trim())"
    exit 0
}

if (-not $Name) { Refuse 'say which entry with -Name <words of its title>.' }
$found = @(Find-ListItems $entries $Name)
if ($found.Count -eq 0) { Refuse "no entry's title holds '$Name'." }
if ($found.Count -gt 1) { Refuse "'$Name' names $($found.Count) entries; use more of the title." }
$entry = $found[0]
$state = [string](Get-Field $entry.Document 'State').value

if ($Form -eq 'done') {
    $key = Get-PinnedApprovalKey $Repository
    $checked = @(Get-OutsideEntries $outside $key | Where-Object { $_.Path -eq $entry.File })
    if ($state -ne 'Approved' -or $checked.Count -ne 1 -or -not $checked[0].Valid) {
        Refuse "only an entry the maintainer approved, with an approval that verifies, is done; this one is '$state'."
    }
    (Get-Field $entry.Document 'State').value = 'Done'
    Save-ListItem $entry.File $entry.Document
    Write-Host "Outside reports, done: $($entry.Title)"
    exit 0
}

if ($state -in @('Approved', 'Declined', 'Done')) {
    Refuse "the entry is ${state}: a change would void the maintainer's signature. A new matter gets a new entry."
}

if ($Form -eq 'review') {
    if ($Category -and $categories -notcontains $Category) { Refuse "the category must be one of: $($categories -join ', ')." }
    if ($Priority -and @('High', 'Normal', 'Low') -notcontains $Priority) { Refuse 'the priority must be High, Normal or Low.' }
    $values = [ordered]@{ 'Harm review' = $Review; 'Category' = $Category; 'Item' = $Item; 'Proposed' = $Proposed; 'Proposed priority' = $Priority }
    $written = @()
    foreach ($field in $values.Keys) {
        if ($PSBoundParameters.ContainsKey(@{ 'Harm review' = 'Review'; 'Category' = 'Category'; 'Item' = 'Item'; 'Proposed' = 'Proposed'; 'Proposed priority' = 'Priority' }[$field])) {
            (Get-Field $entry.Document $field).value = "$($values[$field])".Trim()
            $written += $field
        }
    }
    if ($written.Count -eq 0) { Refuse 'give at least one of -Review, -Category, -Item, -Proposed, -Priority.' }
    (Get-Field $entry.Document 'State').value = 'To review'
    Save-ListItem $entry.File $entry.Document
    Write-Host "Outside reports, written ($($written -join ', ')): $($entry.Title)"
    exit 0
}

# ready
$harm = [string](Get-Field $entry.Document 'Harm review').value
$chosen = [string](Get-Field $entry.Document 'Category').value
$itemTitle = [string](Get-Field $entry.Document 'Item').value
$scope = [string](Get-Field $entry.Document 'Proposed').value
$proposedPriority = [string](Get-Field $entry.Document 'Proposed priority').value
if (-not $harm.Trim()) { Refuse 'the Harm review is empty; "Nothing found." is a finding and is written.' }
if ($categories -notcontains $chosen) { Refuse 'the entry has no category.' }
if ($acting -contains $chosen) {
    if (-not $scope.Trim()) { Refuse "a category that acts ($chosen) needs Proposed: exactly what would be done." }
    if (-not $itemTitle.Trim()) { Refuse "the category $chosen needs Item, the title of the priorities item." }
    $existing = @(Get-ListItems $priorities | Where-Object { $_.Title -ceq $itemTitle })
    if ($chosen -eq 'New item') {
        if ($existing.Count -gt 0) { Refuse "a priorities item has the title '$itemTitle' already: the category is Same as an item or Addition to an item." }
        if (-not $proposedPriority) { Refuse 'a new item needs a Proposed priority.' }
    } elseif ($existing.Count -ne 1) {
        Refuse "no priorities item has the exact title '$itemTitle'."
    }
}
(Get-Field $entry.Document 'State').value = 'Awaiting approval'
Save-ListItem $entry.File $entry.Document
Write-Host "Outside reports, awaiting the maintainer's approval: $($entry.Title)"
exit 0
