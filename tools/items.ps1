<#
.SYNOPSIS
  The items of a list of the private management repository, read, found by words of their titles, changed and
  written, for the scripts that work on them: tools\item-of-commit.ps1 (the commit marks its item), tools\item.ps1
  (an item is written, noted and finished), tools\outside-report.ps1 and tools\reports-check.ps1. One way of
  reading, naming and writing an item, in one place.

.DESCRIPTION
  Dot-source it. The files are of the lists' one shape (docs/axTask/forms-framework.md section 13): an item is one
  JSON object of names and values with "schema", the link to its list's list.json, which says which fields the
  list's items have, in which order and of which type ("item fields").

  Get-ListItems <folder> gives every item of the list: its Title, its File and its Document (the file's JSON as
  read). A file that does not read, or that is no item (it names no "schema"), is passed over; list.json is the
  list's own. Find-ListItems <items> <words> gives the items whose title holds the words, without regard to case;
  when exactly one title is the words and nothing more, that item alone. The caller refuses none and several.

  Get-ItemValue <document> <name> gives a value by its name, without regard to case; $null when the item holds
  none. Set-ItemValue <document> <name> <value> [<config>] gives the document with the value set: a name the item
  holds keeps its place; a new one is put where the list's configuration has its field, as axTask puts it, and
  last when the configuration does not name it. Get-ListConfig <folder> gives the list's list.json, or $null.
  New-ListItem <title> <config> gives a new item: the link, the Title, then each field of the configuration with
  the value a new item starts with. Save-ListItem <file> <document> writes a file as axTask writes it
  (ConvertTo-ListJson: one member a line, two spaces, Windows line ends, UTF-8 without a byte order mark), so that
  a file reads the same whoever wrote it last.
#>

function Get-ListItems([string]$Lists) {
    $items = @()
    foreach ($file in Get-ChildItem -LiteralPath $Lists -Filter *.json) {
        if ($file.Name -eq 'list.json') { continue }
        try {
            $document = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
        } catch {
            continue
        }
        if ($null -eq $document -or -not "$(Get-ItemValue $document 'schema')".Trim()) { continue }
        $title = Get-ItemValue $document 'Title'
        if (-not $title) { $title = $file.BaseName }
        $items += [pscustomobject]@{ Title = "$title"; File = $file.FullName; Document = $document }
    }
    return $items
}

function Find-ListItems($Items, [string]$Words) {
    $found = @($Items | Where-Object { $_.Title.IndexOf($Words, [StringComparison]::OrdinalIgnoreCase) -ge 0 })
    $exact = @($found | Where-Object { $_.Title -ieq $Words })
    if ($exact.Count -eq 1) { $found = $exact }
    return $found
}

# The titles nearest to words that found none (the review of the sessions of 2026-11-25: eight refusals of words that
# named no item, each followed by a search by hand): the few that share the most words with them, as one line.
function Get-NearestTitles($Items, [string]$Words, [int]$Count = 3) {
    $wanted = @($Words.ToLowerInvariant() -split '[^a-z0-9]+' | Where-Object { $_.Length -gt 2 })
    $scored = foreach ($item in $Items) {
        $have = @($item.Title.ToLowerInvariant() -split '[^a-z0-9]+')
        $shared = @($wanted | Where-Object { $have -contains $_ }).Count
        if ($shared -gt 0) { [pscustomobject]@{ Title = $item.Title; Shared = $shared } }
    }
    $nearest = @($scored | Sort-Object -Property @{ Expression = 'Shared'; Descending = $true }, Title | Select-Object -First $Count | ForEach-Object { $_.Title })
    if ($nearest.Count -eq 0) { return '' }
    return ' The nearest titles: ' + ($nearest -join '; ') + '.'
}

function Get-ItemValue($Document, [string]$Name) {
    if ($null -eq $Document) { return $null }
    $property = $Document.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Test-ItemValue($Document, [string]$Name) {
    return ($null -ne $Document) -and ($null -ne $Document.PSObject.Properties[$Name])
}

function Get-ListConfig([string]$Lists) {
    $file = Join-Path $Lists 'list.json'
    if (-not (Test-Path -LiteralPath $file)) { return $null }
    try {
        $config = Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        return $null
    }
    if ($null -eq $config -or "$($config.format)" -ne 'axkit-list 2') { return $null }
    return $config
}

# Whether the list is written in axTask's earlier format (its list.json says "axkit-list 1"), which these scripts do
# not read: axTask converts it (docs/axTask/forms-framework.md AF-13.6), and a script says so instead of finding no item.
function Test-EarlierList([string]$Lists) {
    $file = Join-Path $Lists 'list.json'
    if (-not (Test-Path -LiteralPath $file)) { return $false }
    try {
        return "$((Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json).format)" -eq 'axkit-list 1'
    } catch {
        return $false
    }
}

# The titles of the configuration's item fields, in its order.
function Get-ItemFieldTitles($Config) {
    if ($null -eq $Config) { return @() }
    return @(@($Config.'item fields') | Where-Object { $null -ne $_ -and "$($_.title)" } | ForEach-Object { "$($_.title)" })
}

function Set-ItemValue($Document, [string]$Name, $Value, $Config) {
    if (Test-ItemValue $Document $Name) {
        $Document.PSObject.Properties[$Name].Value = $Value
        return $Document
    }

    # A new name: where the configuration has its field, after the link, the Title and the fields before it that
    # the item holds (AxKit.Core FlatItem.With, in this script's words); last when the configuration does not name it.
    $titles = @(Get-ItemFieldTitles $Config)
    $index = -1
    $spelled = $Name
    for ($i = 0; $i -lt $titles.Count; $i++) { if ($titles[$i] -ieq $Name) { $index = $i; $spelled = $titles[$i]; break } }
    $names = @($Document.PSObject.Properties | ForEach-Object { $_.Name })
    $at = $names.Count
    if ($index -ge 0) {
        $at = 0
        for ($position = 0; $position -lt $names.Count; $position++) {
            $member = $names[$position]
            $before = -1
            for ($i = 0; $i -lt $titles.Count; $i++) { if ($titles[$i] -ieq $member) { $before = $i; break } }
            if ($member -ceq 'schema' -or $member -ieq 'Title' -or ($before -ge 0 -and $before -lt $index)) { $at = $position + 1 }
        }
    }

    $ordered = [ordered]@{}
    for ($position = 0; $position -le $names.Count; $position++) {
        if ($position -eq $at) { $ordered[$spelled] = $Value }
        if ($position -lt $names.Count) { $ordered[$names[$position]] = $Document.PSObject.Properties[$names[$position]].Value }
    }
    return [pscustomobject]$ordered
}

function New-ListItem([string]$Title, $Config) {
    $ordered = [ordered]@{ schema = 'list.json'; Title = $Title }
    foreach ($field in @($Config.'item fields')) {
        if ($null -eq $field -or -not "$($field.title)") { continue }
        if ("$($field.title)" -ieq 'Title' -or "$($field.title)" -ieq 'schema') { continue }
        $start = $field.value
        if ($null -eq $start) { $start = if ("$($field.type)" -eq 'check') { $false } else { '' } }
        $ordered["$($field.title)"] = $start
    }
    return [pscustomobject]$ordered
}

# JSON as axTask writes it (System.Text.Json, indented, the relaxed escaping): a text with only the quote, the
# backslash and the control characters escaped, so that a file is read by a person as it is written.
function ConvertTo-ListJsonText([string]$Text) {
    $builder = New-Object System.Text.StringBuilder
    [void]$builder.Append('"')
    foreach ($character in $Text.ToCharArray()) {
        $code = [int]$character
        if ($character -eq '"') { [void]$builder.Append('\"') }
        elseif ($character -eq '\') { [void]$builder.Append('\\') }
        elseif ($code -eq 10) { [void]$builder.Append('\n') }
        elseif ($code -eq 13) { [void]$builder.Append('\r') }
        elseif ($code -eq 9) { [void]$builder.Append('\t') }
        elseif ($code -lt 32) { [void]$builder.Append('\u' + $code.ToString('X4')) }
        else { [void]$builder.Append($character) }
    }
    [void]$builder.Append('"')
    return $builder.ToString()
}

function ConvertTo-ListJsonNode($Value, [string]$Indent) {
    if ($null -eq $Value) { return 'null' }
    if ($Value -is [bool]) { if ($Value) { return 'true' } else { return 'false' } }
    if ($Value -is [string]) { return ConvertTo-ListJsonText $Value }
    if ($Value -is [int] -or $Value -is [long] -or $Value -is [double] -or $Value -is [decimal] -or $Value -is [single]) {
        return ([System.IConvertible]$Value).ToString([System.Globalization.CultureInfo]::InvariantCulture)
    }
    # PowerShell 7 reads a text that looks like a date and a time as one; written back in the lists' own form.
    if ($Value -is [datetime]) {
        $form = if ($Value.Second -eq 0) { "yyyy-MM-dd'T'HH:mm" } else { "yyyy-MM-dd'T'HH:mm:ss" }
        return ConvertTo-ListJsonText $Value.ToString($form, [System.Globalization.CultureInfo]::InvariantCulture)
    }

    $inner = $Indent + '  '
    $newline = "`r`n"
    if ($Value -is [System.Collections.IDictionary]) {
        $members = @($Value.Keys | ForEach-Object { (ConvertTo-ListJsonText "$_") + ': ' + (ConvertTo-ListJsonNode $Value[$_] $inner) })
        if ($members.Count -eq 0) { return '{}' }
        return '{' + $newline + $inner + ($members -join (',' + $newline + $inner)) + $newline + $Indent + '}'
    }
    if ($Value -is [System.Management.Automation.PSCustomObject]) {
        $members = @($Value.PSObject.Properties | ForEach-Object { (ConvertTo-ListJsonText $_.Name) + ': ' + (ConvertTo-ListJsonNode $_.Value $inner) })
        if ($members.Count -eq 0) { return '{}' }
        return '{' + $newline + $inner + ($members -join (',' + $newline + $inner)) + $newline + $Indent + '}'
    }
    if ($Value -is [System.Collections.IEnumerable]) {
        $entries = @($Value | ForEach-Object { ConvertTo-ListJsonNode $_ $inner })
        if ($entries.Count -eq 0) { return '[]' }
        return '[' + $newline + $inner + ($entries -join (',' + $newline + $inner)) + $newline + $Indent + ']'
    }
    return ConvertTo-ListJsonText "$Value"
}

function ConvertTo-ListJson($Document) {
    return (ConvertTo-ListJsonNode $Document '') + "`r`n"
}

function Save-ListItem([string]$File, $Document) {
    [System.IO.File]::WriteAllText($File, (ConvertTo-ListJson $Document), (New-Object System.Text.UTF8Encoding($false)))
}
