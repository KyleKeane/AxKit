<#
.SYNOPSIS
Writes docs\axKit\apps.md, the one list of the bundle's apps with where each one's code, tests, documents and checks
are; or checks that the page is current (-Check).

.DESCRIPTION
The apps are Dispatch.Apps in src\AxKit.Core\Dispatch.cs, the one source (tools\apps.ps1 reads it). Until 2026-11-13
the list was also written by hand in at least eight documents, and counts such as "the nine apps" and "fourteen
today" stayed behind as apps were added (docs/axKit/repairs.json R-44). The page gives, for each app in the windows'
order: its name and what it is, its command, its group, what it opens, the program an access layer works through,
whether it starts at sign-in and has a Start menu entry in the Store's copy, and the folders that exist for it: the
window's code, the pure code, the tests, the documents, the automated scripts with their count, and its run script.

A document that needs the apps points to this page; it does not repeat the list. -Check writes nothing and exits with
1 when the page on disk is not what the sources give; then run the script without -Check and commit the page.
test.ps1 and build.yml run the check.
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
. (Join-Path $root 'tools\apps.ps1')
$documentPath = Join-Path $root 'docs\axKit\apps.md'
$apps = @(Get-AxKitApps $root)

$groupNames = @{ Files = 'Files'; Organise = 'Organise'; Utilities = 'Utilities'; AccessLayers = 'Access layers'; Claude = 'Claude (access layers for Claude Code)' }
$opens = @{ Nothing = 'nothing'; Folders = 'a folder'; Files = 'a file'; FilesAndFolders = 'a file or a folder' }

function Existing([string]$relative) { if (Test-Path -LiteralPath (Join-Path $root $relative)) { '`' + $relative.Replace('\', '/') + '`' } }

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# The apps')
$lines.Add('')
$lines.Add('Written by `tools/apps-page.ps1` from `Dispatch.Apps` (`src/AxKit.Core/Dispatch.cs`); never edit it by hand.')
$lines.Add("axKit has $($apps.Count) apps, in the order every list of the windows shows them. A document that needs the apps")
$lines.Add('points here. The shared pieces every app stands on are `src/AxKit.Core` (pure code) and `src/AxKit.Forms` (the')
$lines.Add('window base and the shared controls), with their notes in each folder''s `CLAUDE.md`; the executable is `src/AxKit`.')
$lines.Add('A new app is made with `tools/new-app.ps1` (`docs/axKit/adding-an-app.md`).')

$lastGroup = ''
foreach ($app in $apps) {
    if ($app.Group -ne $lastGroup) {
        $lastGroup = $app.Group
        $lines.Add('')
        $lines.Add('## ' + $groupNames[$app.Group])
    }

    $lines.Add('')
    $lines.Add("### $($app.Name)")
    $lines.Add('')
    $what = if ($app.Description.StartsWith($app.Name + ', ')) { $app.Description.Substring($app.Name.Length + 2) } else { $app.Description }
    $lines.Add("- What: $what.")
    if ($app.AccessLayerFor) { $lines.Add("- Access layer for: $($app.AccessLayerFor), which must be installed; the app reaches the network only through it.") }
    $facts = @("command ``axkit $($app.Verb)``", "opens $($opens[$app.Opens])")
    if ($app.AtSignIn) { $facts += 'runs in the background from sign-in' }
    if ($app.StoreEntry) { $facts += 'has a Start menu entry in the Store''s copy' }
    $lines.Add('- ' + ($facts -join '; ') + '.')
    $code = @((Existing "src\$($app.Project)"), (Existing "src\$($app.Project).Core")) | Where-Object { $_ }
    $lines.Add('- Code: ' + ($code -join ' (the window), ') + $(if ($code.Count -gt 1) { ' (pure code)' } else { ' (the window; its pure code is in `src/AxKit.Core`)' }) + '; notes in each folder''s `CLAUDE.md`.')
    $tests = Existing "tests\$($app.Project).Tests"
    $lines.Add('- Tests: ' + $(if ($tests) { $tests } else { '`tests/AxKit.Tests`' }) + '.')
    $docs = Join-Path $root "docs\$($app.Name)"
    if (Test-Path -LiteralPath $docs) {
        $names = @(Get-ChildItem -LiteralPath $docs -Filter *.md | Sort-Object { $_.Name.ToLowerInvariant() } | ForEach-Object { '`' + $_.Name + '`' })
        $lines.Add("- Documents: ``docs/$($app.Name)/`` ($($names -join ', ')).")
    }

    $scripts = Join-Path $root "tests\scripts\$($app.Name)"
    if (Test-Path -LiteralPath $scripts) {
        $count = @(Get-ChildItem -LiteralPath $scripts -Filter *.txt).Count
        $lines.Add("- Automated checks: ``tests/scripts/$($app.Name)/``, $count script$(if ($count -ne 1) { 's' }) (their workflows are in ``docs/axKit/workflows.md``).")
    }

    $run = Existing "run-$($app.Name).ps1"
    if ($run) { $lines.Add("- Run from the sources: $run, or ``run.ps1 -App $($app.Verb)``.") } else { $lines.Add("- Run from the sources: ``run.ps1 -App $($app.Verb)``.") }
}

$text = ($lines -join "`r`n") + "`r`n"
if ($Check) {
    $current = if (Test-Path $documentPath) { [IO.File]::ReadAllText($documentPath) } else { '' }
    # Line endings aside: the checkout's are native, LF on CI.
    if (($current -replace "`r`n", "`n") -cne ($text -replace "`r`n", "`n")) {
        Write-Host 'docs/axKit/apps.md is not what the sources give: run tools\apps-page.ps1 and commit the page.'
        exit 1
    }

    Write-Host "docs/axKit/apps.md is current: $($apps.Count) apps."
    exit 0
}

[IO.File]::WriteAllText($documentPath, $text, (New-Object System.Text.UTF8Encoding($false)))
Write-Host "Wrote docs/axKit/apps.md: $($apps.Count) apps."
