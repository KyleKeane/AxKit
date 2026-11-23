<#
.SYNOPSIS
Checks that every menu path a document names is a menu path the windows have; with -Check it exits with 1 when one
is not.

.DESCRIPTION
The maintainer, 2026-11-23: "when we hit any issues that are common easy mistakes we should stop to investigate how
we can remove that possible mistake moving forward by building systems that stop hand maintanence." That day Apps and
Access layers moved from the Window menu to the axKit menu, and some twenty documents named the old place; an F1 line
had named the axKit menu's old letter for twelve days. A document may say where a command is, in words; this script
holds those words to the windows' real menus, so that a move of a menu item fails the check until every document
follows.

What it reads, in every Markdown file under docs and README.md (the generated pages left out, they are written from
the same data):
  - "Alt+<letter>, <name>": the letter must be one of the menu bar's (AxKitForm.BarMenus in
    src\AxKit.Forms\AxKitForm.cs), and <name> a submenu or an item that some window has under that menu in the command
    inventory (docs\axKit\window-commands.json). A single letter after the comma ("Alt+W, O, 1") is a key, not a name,
    and is left alone.
  - "the <menu> menu's <name>": the same, by the menu's name.
A name is matched by its words from the start, the longest that is a name of the menu, without case.
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent

# The bar menus and their letters, from the code: "&Window" is W, "ax&Kit" is K.
$formCode = [IO.File]::ReadAllText((Join-Path $root 'src\AxKit.Forms\AxKitForm.cs'))
$barLine = [regex]::Match($formCode, 'BarMenus\s*=\s*\[(?<items>[^\]]*)\]')
if (-not $barLine.Success) { throw 'BarMenus was not found in src\AxKit.Forms\AxKitForm.cs.' }
$menuOfLetter = @{}
$menuNames = @{}
foreach ($item in [regex]::Matches($barLine.Groups['items'].Value, '"([^"]*)"')) {
    $text = $item.Groups[1].Value
    $name = $text.Replace('&', '')
    $menuOfLetter[[string]$text[$text.IndexOf('&') + 1]] = $name
    $menuNames[$name.ToLowerInvariant()] = $name
}

# Every name under each top menu, from the inventory: its submenus and its items, without case.
$inventory = [IO.File]::ReadAllText((Join-Path $root 'docs\axKit\window-commands.json')) | ConvertFrom-Json
$namesUnder = @{}
foreach ($app in $inventory.apps) {
    foreach ($command in $app.commands) {
        if (-not $command.menu) { continue }
        $parts = @($command.menu -split ', ')
        $top = $parts[0]
        if (-not $namesUnder.ContainsKey($top)) { $namesUnder[$top] = @{} }
        foreach ($part in ($parts | Select-Object -Skip 1)) { $namesUnder[$top][$part.ToLowerInvariant()] = $true }
        $namesUnder[$top][([string]$command.label).ToLowerInvariant()] = $true
    }
}

# A name as it is compared: without its mnemonic, what follows " (" ("Check for updates (axKit 5.10.0)..."), the closing
# dots, and case.
function Get-PlainName([string]$name) {
    $plain = $name.Replace('&', '')
    if ($plain.IndexOf(' (') -ge 0) { $plain = $plain.Substring(0, $plain.IndexOf(' (')) }
    return $plain.TrimEnd('.').Trim().ToLowerInvariant()
}

foreach ($top in @($namesUnder.Keys)) {
    $plain = @{}
    foreach ($name in $namesUnder[$top].Keys) { $plain[(Get-PlainName $name)] = $true }
    $namesUnder[$top] = $plain
}

# The bundle's own menus (Window, axKit, Help) have items that fill as they open or that the test runs never show
# (the open windows, the closed files, Check for updates): their names come from the window base's code too, every
# menu name with a mnemonic it makes.
foreach ($top in @('Window', 'axKit', 'Help')) { if (-not $namesUnder.ContainsKey($top)) { $namesUnder[$top] = @{} } }
foreach ($source in Get-ChildItem (Join-Path $root 'src\AxKit.Forms') -Filter *.cs | Where-Object { $_.Name -match '^(AxKitForm|WindowMenu)' }) {
    foreach ($made in [regex]::Matches([IO.File]::ReadAllText($source.FullName), 'new(?: ToolStripMenuItem)?\(\$?"(?<name>[^"]*&[^"]*)"')) {
        $plain = Get-PlainName $made.Groups['name'].Value
        foreach ($top in @('Window', 'axKit', 'Help')) { $namesUnder[$top][$plain] = $true }
    }
}

# Whether the words after a menu start with a name the menu has. Words that are no name are prose ("the Window
# menu's items") or a key ("Alt+O, then F"): a name starts with a capital, and is not a key's.
function Test-MenuName([string]$menu, [string]$words) {
    $first = ($words.Trim() -split '\s+')[0]
    if ($first -cnotmatch '^[A-Z]' -or $first -match '^(Ctrl|Shift|Alt|Tab|Enter|Escape|Space|Home|End|Delete|F\d+)$') { return $true }
    if (-not $namesUnder.ContainsKey($menu)) { return $false }
    $split = @($words.Trim() -split '\s+')
    for ($count = $split.Count; $count -ge 1; $count--) {
        if ($namesUnder[$menu].ContainsKey((($split | Select-Object -First $count) -join ' ').ToLowerInvariant())) { return $true }
    }
    return $false
}

$generated = '\\docs\\axKit\\(key-map|key-groups|window-commands|workflows|outside-calls|families|apps|owed|cases|components|repairs)\.md$|\\docs\\axKit\\workflows\\'
$files = @(Get-ChildItem (Join-Path $root 'docs') -Recurse -Filter *.md | Where-Object { $_.FullName -notmatch $generated } | ForEach-Object { $_.FullName }) + @(Join-Path $root 'README.md')
$problems = New-Object System.Collections.Generic.List[string]
foreach ($file in $files) {
    $relative = $file.Substring($root.Length + 1).Replace('\', '/')
    $lineNumber = 0
    foreach ($line in [IO.File]::ReadAllLines($file)) {
        $lineNumber++
        foreach ($match in [regex]::Matches($line, 'Alt\+(?<letter>[A-Z]), (?<words>[A-Za-z][A-Za-z ]+)')) {
            $words = $match.Groups['words'].Value
            # A single letter is the next key of the path ("Alt+W, O, 1"), not a name.
            if (($words.Trim() -split '\s+')[0].Length -eq 1) { continue }
            $menu = $menuOfLetter[$match.Groups['letter'].Value]
            if (-not $menu) { continue }
            if (-not (Test-MenuName $menu $words)) { $problems.Add("${relative}:${lineNumber}: Alt+$($match.Groups['letter'].Value), $($words.Trim()): the $menu menu has no such name") }
        }
        foreach ($match in [regex]::Matches($line, "the (?<menu>[A-Za-z]+) menu's (?<words>[A-Za-z][A-Za-z ]+)")) {
            $menu = $menuNames[$match.Groups['menu'].Value.ToLowerInvariant()]
            if (-not $menu) { continue }
            if (-not (Test-MenuName $menu $match.Groups['words'].Value)) { $problems.Add("${relative}:${lineNumber}: the $menu menu's $($match.Groups['words'].Value.Trim()): the $menu menu has no such name") }
        }
    }
}

foreach ($problem in $problems) { Write-Host $problem }
if ($problems.Count -gt 0) {
    Write-Host "$($problems.Count) menu path(s) in the documents that the windows do not have: correct each document (or the menu)."
    if ($Check) { exit 1 }
    exit 0
}

Write-Host "Every menu path the documents name is one the windows have ($($files.Count) documents)."
exit 0
