<#
.SYNOPSIS
  The items of a list of the private management repository, read and found by words of their titles, for the two
  scripts that work on them: tools\item-of-commit.ps1 (the commit marks its item) and tools\item.ps1 (an item is
  written, noted and finished). One way of naming an item, in one place.

.DESCRIPTION
  Dot-source it. Get-ListItems <folder> gives every item of the list: its Title, its File and its Document (the
  file's JSON as read; a file that does not read is passed over, and list.json is the list's own). Find-ListItems
  <items> <words> gives the items whose title holds the words, without regard to case; when exactly one title is the
  words and nothing more, that item alone. The caller refuses none and several. Save-ListItem <file> <document>
  writes an item as the lists hold it: the JSON as Windows PowerShell writes it, UTF-8 without a byte order mark.
#>

function Get-ListItems([string]$Lists) {
    $items = @()
    foreach ($file in Get-ChildItem -LiteralPath $Lists -Filter *.json) {
        if ($file.Name -eq 'list.json') { continue }
        try {
            $document = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        } catch {
            continue
        }
        $title = ($document.fields | Where-Object { $_.title -eq 'Title' } | Select-Object -First 1).value
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

function Save-ListItem([string]$File, $Document) {
    [System.IO.File]::WriteAllText($File, ($Document | ConvertTo-Json -Depth 10), (New-Object System.Text.UTF8Encoding($false)))
}
