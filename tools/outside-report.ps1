<#
.SYNOPSIS
  Writes the triage entry of an outside report (docs/axKit/outside-reports.md OR-3, OR-4, OR-7.4): the assistant's one
  way to write into the outside reports list. It never writes an approval.

.DESCRIPTION
  The list is <parent>\axkit-management\outside-reports, one JSON item per report, made with its list.json when
  missing. Five forms:

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

    reopen -Name <words>
        State "Awaiting approval" again, its approval emptied, for an entry Approved whose approval the pinned key
        does not verify (the approval key changed, OR-6.5). Refused while the approval verifies.

  An entry that is Approved, Declined or Done is refused every change but done and reopen: a change would void the signature
  (OR-5.3). Approval, Approved on and the states Approved and Declined are written by axTask alone, with the
  maintainer's passphrase. Exit code 0 when written, 1 when refused. The script stays ASCII.

.EXAMPLE
  .\tools\outside-report.ps1 new -Title 'axStart: says nothing at its first launch' -Report 'KyleKeane/AxKit-reports issue 15'
  .\tools\outside-report.ps1 review -Name 'says nothing at its first' -Review 'Nothing found.' -Category 'New item' -Item 'axStart: ...' -Proposed '...' -Priority High
  .\tools\outside-report.ps1 ready -Name 'says nothing at its first'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)][ValidateSet('new', 'review', 'ready', 'done', 'reopen')][string]$Form,
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
        [pscustomobject]@{ title = 'Approved on'; type = 'date'; value = '' },
        # For the list's action that answers the report (docs/axTask/forms-framework.md section 10, step 7): the
        # number of the report's issue, the first when the entry names several, and the day it was answered.
        # Neither is a field an approval signs (OR-5.3).
        [pscustomobject]@{ title = 'Issue'; type = 'text'; value = '' },
        [pscustomobject]@{ title = 'Answered'; type = 'date'; value = '' }
    )
}

# The list's one action (the maintainer's yes of 2026-11-21): Answer the report on GitHub. Its form asks for the
# answer, which goes to gh as its input; the only value of the entry among the arguments is the issue's number.
# Nothing of a report's own words is an argument, and axTask asks before the first run with every argument shown.
function Get-ListActions {
    return @(
        [ordered]@{
            name = 'Answer the report on GitHub'
            does = 'run'
            fields = @([ordered]@{ title = 'Type your answer'; type = 'longtext' })
            program = 'gh'
            arguments = @('issue', 'comment', '{Issue}', '--repo', 'KyleKeane/AxKit-reports', '--body-file', '-')
            input = '{form:Type your answer}'
            then = [ordered]@{ set = [ordered]@{ Answered = '{today}' } }
        }
    )
}

# The list itself, made once: its notes say what it is, and its new items have the triage fields.
if (-not (Test-Path -LiteralPath $outside)) {
    New-Item -ItemType Directory -Force $outside | Out-Null
}
$listFile = Join-Path $outside 'list.json'
if (-not (Test-Path -LiteralPath $listFile)) {
    # The list's configuration (docs/axTask/forms-framework.md section 11): its details, the fields of its entries.
    $list = [ordered]@{
        format = 'axkit-list 2'
        details = [ordered]@{
            Section = 'Lists'
            Notes = 'Outside reports: one entry for each report of KyleKeane/AxKit-reports, with the assistant''s harm review and the scope it proposes. Nothing of a report is worked on until you approve its entry with your passphrase (Approve this report, in the Context menu). docs/axKit/outside-reports.md.'
        }
        'item fields' = @(Get-ListTemplate)
        actions = @(Get-ListActions)
    }
    Save-ListItem $listFile ([pscustomobject]$list)
}
if (Test-EarlierList $outside) {
    Refuse 'the outside reports list is written in axTask''s earlier format, which the scripts no longer read. Open the list in axTask and convert it (Enter on the list) first.'
}
$listConfig = Get-ListConfig $outside
if ($null -eq $listConfig) { Refuse 'the outside reports list has no configuration that reads (list.json, format axkit-list 2).' }

# A value of an entry as text, by its name; empty when the entry holds none.
function Get-Field($document, [string]$named) {
    return [string](Get-ItemValue $document $named)
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
        $theirs = @(Get-ReportNumbers (Get-Field $entry.Document 'Report'))
        foreach ($number in $numbers) {
            if ($theirs -contains $number) { Refuse "report $number already has its entry: $($entry.Title)." }
        }
    }
    $document = New-ListItem $Title.Trim() $listConfig
    $document = Set-ItemValue $document 'Report' $Report.Trim() $listConfig
    $document = Set-ItemValue $document 'Received' (Get-Date).ToString('yyyy-MM-dd') $listConfig
    # The issue's number alone, for the list's action that answers the report: the first when the entry names several.
    $document = Set-ItemValue $document 'Issue' "$($numbers[0])" $listConfig
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
$state = Get-Field $entry.Document 'State'

# The entry written with these values set, each where the list's configuration has its field when the file lacked it.
function Save-Entry([System.Collections.IDictionary]$changes) {
    $changed = $entry.Document
    foreach ($field in $changes.Keys) {
        $changed = Set-ItemValue $changed $field $changes[$field] $listConfig
    }
    Save-ListItem $entry.File $changed
}

if ($Form -eq 'done') {
    $key = Get-PinnedApprovalKey $Repository
    $checked = @(Get-OutsideEntries $outside $key | Where-Object { $_.Path -eq $entry.File })
    if ($state -ne 'Approved' -or $checked.Count -ne 1 -or -not $checked[0].Valid) {
        Refuse "only an entry the maintainer approved, with an approval that verifies, is done; this one is '$state'."
    }
    Save-Entry ([ordered]@{ 'State' = 'Done' })
    Write-Host "Outside reports, done: $($entry.Title)"
    exit 0
}

if ($Form -eq 'reopen') {
    # After a change of the approval key (OR-6.5): an approval in force that the pinned key no longer verifies is
    # offered to the maintainer again. Nothing he signed with the pinned key is ever taken back this way.
    $key = Get-PinnedApprovalKey $Repository
    $checked = @(Get-OutsideEntries $outside $key | Where-Object { $_.Path -eq $entry.File })
    if ($state -ne 'Approved' -or $checked.Count -ne 1) { Refuse "only an Approved entry is offered again; this one is '$state'." }
    if ($checked[0].Valid) { Refuse 'its approval verifies with the pinned key: it stands, and nothing is taken back.' }
    Save-Entry ([ordered]@{ 'State' = 'Awaiting approval'; 'Approval' = ''; 'Approved on' = '' })
    Write-Host "Outside reports, awaiting the maintainer's approval again: $($entry.Title)"
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
    $reviewed = [ordered]@{}
    foreach ($field in $values.Keys) {
        if ($PSBoundParameters.ContainsKey(@{ 'Harm review' = 'Review'; 'Category' = 'Category'; 'Item' = 'Item'; 'Proposed' = 'Proposed'; 'Proposed priority' = 'Priority' }[$field])) {
            $reviewed[$field] = "$($values[$field])".Trim()
            $written += $field
        }
    }
    if ($written.Count -eq 0) { Refuse 'give at least one of -Review, -Category, -Item, -Proposed, -Priority.' }
    $reviewed['State'] = 'To review'
    Save-Entry $reviewed
    Write-Host "Outside reports, written ($($written -join ', ')): $($entry.Title)"
    exit 0
}

# ready
$harm = Get-Field $entry.Document 'Harm review'
$chosen = Get-Field $entry.Document 'Category'
$itemTitle = Get-Field $entry.Document 'Item'
$scope = Get-Field $entry.Document 'Proposed'
$proposedPriority = Get-Field $entry.Document 'Proposed priority'
if (-not $harm.Trim()) { Refuse 'the Harm review is empty; "Nothing found." is a finding and is written.' }
# What he signs is what he hears (OR-5.6): axTask refuses to sign an entry with a character that is not spoken, so it
# is named here already.
foreach ($signedField in @('Title', 'Report', 'Category', 'Item', 'Proposed priority', 'Proposed', 'Harm review')) {
    $unheard = Get-UnheardCharacter (Get-Field $entry.Document $signedField)
    if ($unheard) { Refuse "the field $signedField holds a character that is not spoken ($unheard): write it again without it." }
}
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
Save-Entry ([ordered]@{ 'State' = 'Awaiting approval' })
Write-Host "Outside reports, awaiting the maintainer's approval: $($entry.Title)"
exit 0
