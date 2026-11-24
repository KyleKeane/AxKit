<#
.SYNOPSIS
  The checks a push must pass, run by the pre-push hook: the bundle's own tests, the test projects of what the pushed
  commits changed, and every page and script check of test.ps1. The push is refused when one fails.

.DESCRIPTION
  On 2026-11-24 two commits went in with a test of tests\AxKit.Tests failing, because only the test projects that
  seemed to matter had been run; and a page check found a stale page more than once after a push, on GitHub. A push
  now runs, before anything leaves the computer:
    - tests\AxKit.Tests always: it holds the source checks of the whole bundle (keys, menus, marks, layout, notes,
      privacy, the documents);
    - the test project of every app the pushed commits changed (src\Ax<Name>, src\Ax<Name>.Core or its tests), and
      every test project when a shared piece changed (src\AxKit.Core, src\AxKit.Forms, src\AxKit, Directory.Build.*);
    - the page checks, tools\upstream.ps1 -Check and tools\script-lint.ps1, as test.ps1 runs them.
  What was pushed is what lies between the remote's main and HEAD (git diff --name-only origin/main...HEAD); with no
  remote main, everything. Exit code 0 when all passed, 1 when one failed, with its lines. The run takes a minute or two,
  every project when shared code changed. The script stays ASCII (Windows PowerShell 5.1 reads it as ANSI).

.EXAMPLE
  .\tools\push-check.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$all = @(Get-ChildItem -Path (Join-Path $root 'tests') -Filter '*.Tests.csproj' -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.BaseName -replace '\.Tests$', '' })

& git -C $root rev-parse --verify -q origin/main 2>&1 | Out-Null
$changed = if ($LASTEXITCODE -eq 0) { @(& git -C $root diff --name-only origin/main...HEAD 2>$null) } else { @('src/AxKit.Core/') }

$projects = New-Object System.Collections.Generic.List[string]
$projects.Add('AxKit')
$shared = $false
foreach ($file in $changed) {
    if ($file -match '^src/AxKit(\.Core|\.Forms)?/' -or $file -match '^Directory\.Build\.') { $shared = $true; continue }
    if ($file -match '^(src|tests)/(Ax[A-Za-z0-9]+?)(\.Core|\.Tests)?/') {
        $name = $Matches[2]
        if ($all -contains $name -and -not $projects.Contains($name)) { $projects.Add($name) }
    }
    if ($file -match '^tools/AxAutomate/' -and -not $projects.Contains('AxAutomate')) { $projects.Add('AxAutomate') }
}
if ($shared) {
    foreach ($name in $all) { if (-not $projects.Contains($name)) { $projects.Add($name) } }
}

Write-Host "Push check: the test projects $($projects -join ', '), then the page and script checks."
try {
    foreach ($project in $projects) {
        $path = Join-Path $root "tests\$project.Tests\$project.Tests.csproj"
        if (-not (Test-Path -LiteralPath $path)) { continue }
        # Outside call, program, dotnet: runs one test project of this repository in Release, as test.ps1 does.
        $lines = @(& dotnet test $path --configuration Release --nologo -v q 2>&1 | ForEach-Object { "$_" })
        if ($LASTEXITCODE -ne 0) {
            $lines | Where-Object { $_ -match 'error|Assert|Expected|Actual|Failed |\[FAIL\]' } | Select-Object -First 40 | ForEach-Object { Write-Host "  $_" }
            Write-Host "Push check: $project.Tests failed."
            exit 1
        }
        Write-Host "  $project.Tests passed"
    }
} finally {
    & dotnet build-server shutdown 2>&1 | Out-Null
}

foreach ($page in 'outside-calls', 'workflows', 'repairs', 'apps-page', 'cases', 'key-map', 'families', 'owed', 'components', 'guide-menus', 'doc-menus') {
    $lines = @(& (Join-Path $root "tools\$page.ps1") -Check 2>&1 | ForEach-Object { "$_" })
    if ($LASTEXITCODE -ne 0) {
        $lines | ForEach-Object { Write-Host "  $_" }
        Write-Host "Push check: the page check tools\$page.ps1 -Check failed: run tools\$page.ps1 and commit the page it writes."
        exit 1
    }
}

foreach ($check in 'upstream', 'script-lint') {
    $lines = @(& (Join-Path $root "tools\$check.ps1") -Check 2>&1 | ForEach-Object { "$_" })
    if ($LASTEXITCODE -ne 0) {
        $lines | ForEach-Object { Write-Host "  $_" }
        Write-Host "Push check: tools\$check.ps1 -Check failed."
        exit 1
    }
}

Write-Host 'Push check: every check passed.'
exit 0
