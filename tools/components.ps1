<#
.SYNOPSIS
Writes docs\axKit\components.md, the page of the canonical pieces every window is made of, the types of window and
which app is which; or checks that the page is current (-Check).

.DESCRIPTION
The source is docs\axKit\components.json (docs\axKit\framework.md FW-7.3; the maintainer, 2026-11-22: "a similar
document to the key mappings that holds a collection of ui interface components so that read-only text, editable
text, selectable list, and forms are all becoming canonical components that are used to compose anything further").
The page is for reading: each component with what it is, what is built into it, what it says and its sound when the
focus enters it; each type of window with its panes; each app with its type today and the type it is to be.

The script also holds the file to itself: every pane of a window type and every type an app names is one the file
defines; every class it names is in the file it names. What the sources must hold beyond that (an app's window
derives from its type's class, every app of the bundle is listed once) is tests\AxKit.Tests\ComponentsTests.cs.

-Check writes nothing and exits with 1 when the page on disk is not what the file gives, or when the file does not
hold; then correct the file, run the script without -Check and commit the page. test.ps1 and build.yml run the check.
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$sourcePath = Join-Path $root 'docs\axKit\components.json'
$documentPath = Join-Path $root 'docs\axKit\components.md'
$data = [IO.File]::ReadAllText($sourcePath, [Text.Encoding]::UTF8) | ConvertFrom-Json

$problems = New-Object System.Collections.Generic.List[string]
$componentIds = @($data.components | ForEach-Object { $_.id })
$typeIds = @($data.'window types' | ForEach-Object { $_.id })

# Every class the file names is declared in the file it names.
foreach ($entry in @($data.components) + @($data.'window types')) {
    $file = Join-Path $root ($entry.file -replace '/', '\')
    if (-not (Test-Path -LiteralPath $file)) {
        $problems.Add("'$($entry.id)': there is no file $($entry.file).")
    }
    elseif ([IO.File]::ReadAllText($file) -notmatch ("class " + [regex]::Escape($entry.class) + "\b")) {
        $problems.Add("'$($entry.id)': $($entry.file) declares no class $($entry.class).")
    }
}

foreach ($type in $data.'window types') {
    foreach ($pane in $type.panes) {
        if ($componentIds -notcontains $pane) { $problems.Add("The window type '$($type.id)' names the pane '$pane', which is no component of the file.") }
    }
}

foreach ($app in $data.apps) {
    foreach ($named in @($app.type, $app.'to be')) {
        if ($named -and ($typeIds -notcontains $named)) { $problems.Add("$($app.app) names the type '$named', which is no window type of the file.") }
    }

    if (-not $app.'to be') { $problems.Add("$($app.app) does not say which type it is to be.") }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host "components: $_" }
    Write-Host "docs/axKit/components.json does not hold: $($problems.Count) line(s) above to correct."
    exit 1
}

$soundOf = @{
    'list-opened'  = 'three quick beeps up, then four quick beeps going down'
    'text-opened'  = 'three quick beeps up, then one low long tone'
    'field-opened' = 'three quick beeps up, then one long tone'
    'form-opened'  = 'three quick beeps up, then beeps and tones together'
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# The components and the window types')
$lines.Add('')
$lines.Add('Written by `tools/components.ps1` from `docs/axKit/components.json`; never edit it by hand. The design is')
$lines.Add('`docs/axKit/framework.md`: every window of axKit is one of a few types, and each type is made only of the')
$lines.Add('pieces below. Learn a piece once and it works the same wherever it appears.')
$lines.Add('')
$lines.Add('## The components')
foreach ($component in $data.components) {
    $lines.Add('')
    $lines.Add("### $($component.title)")
    $lines.Add('')
    $lines.Add("- What: $($component.what)")
    $lines.Add("- Built into it: $($component.'built in')")
    if ($component.selection) { $lines.Add("- Selecting: $($component.selection)") }
    $lines.Add("- Its sound: $($soundOf[$component.'entry event']) (the kind of event ``$($component.'entry event')``, SPEC AX-7.11). It plays when your own command opens it; at every entry of the focus once that is built (framework step 2). Then: $($component.says)")
    $lines.Add("- Its keys, by family (``docs/axKit/key-groups.md``): $($component.keys -join ', ').")
    $lines.Add("- The code: ``$($component.class)``, ``$($component.file)``.")
}

$lines.Add('')
$lines.Add('## The window types')
foreach ($type in $data.'window types') {
    $made = @($type.panes | ForEach-Object { $id = $_; ($data.components | Where-Object { $_.id -eq $id }).title.ToLowerInvariant() })
    $here = @($data.apps | Where-Object { $_.type -eq $type.id } | ForEach-Object { $_.app })
    $coming = @($data.apps | Where-Object { $_.type -ne $type.id -and $_.'to be' -eq $type.id } | ForEach-Object { $_.app })
    $lines.Add('')
    $lines.Add("### $($type.title)")
    $lines.Add('')
    $lines.Add("- What: $($type.what)")
    $lines.Add("- Made of: $($made -join '; ').")
    $lines.Add("- Moving: $($type.moving)")
    $lines.Add('- Apps on it: ' + $(if ($here.Count -gt 0) { ($here -join ', ') + '.' } else { 'none yet.' }))
    if ($coming.Count -gt 0) { $lines.Add("- To move onto it: $($coming -join ', ').") }
    $lines.Add("- The code: ``$($type.class)``, ``$($type.file)``.")
}

$owed = @($data.apps | Where-Object { -not $_.type })
$lines.Add('')
$lines.Add('## The apps')
$lines.Add('')
$lines.Add("$($data.apps.Count) apps: $($data.apps.Count - $owed.Count) on a type, $($owed.Count) with a window of their own still to move.")
$lines.Add('')
$lines.Add('| App | Type today | To be | Note |')
$lines.Add('|---|---|---|---|')
foreach ($app in $data.apps) {
    $today = if ($app.type) { $app.type } else { 'a window of its own' }
    $lines.Add("| $($app.app) | $today | $($app.'to be') | $($app.note) |")
}

$text = ($lines -join "`r`n") + "`r`n"
$summary = "$($data.components.Count) components, $($data.'window types'.Count) window types, $($data.apps.Count) apps, $($owed.Count) still to move"
if ($Check) {
    $current = if (Test-Path $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    # Line endings aside: the checkout's are native, LF on CI.
    if (($current -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host 'docs/axKit/components.md is not what docs/axKit/components.json gives: run tools\components.ps1 and commit the page.'
        exit 1
    }

    Write-Host "docs/axKit/components.md is current: $summary."
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote docs/axKit/components.md: $summary."
