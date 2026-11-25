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

# What is pushed is the commits, not the files on disk: with changes not committed (work in progress, or another
# session's), the checks run on a clean copy of HEAD, a worktree in the temporary folder, and this script is run again
# there (2026-11-24: an edit made while a push was being checked broke the build the check was testing).
if (@(& git -C $root status --porcelain --untracked-files=no).Count -gt 0) {
    $copy = Join-Path ([IO.Path]::GetTempPath()) ('axkit-push-' + (Split-Path -Leaf $root))
    if (Test-Path -LiteralPath $copy) { & git -C $root worktree remove --force $copy 2>&1 | Out-Null }
    # Outside call, program, git: a worktree of HEAD in the temporary folder, for the checks alone.
    & git -C $root worktree add --detach $copy HEAD 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'Push check: the working tree has changes not committed, and no clean copy of HEAD could be made: commit or put them aside, then push again.'
        exit 1
    }

    Write-Host 'Push check: the working tree has changes not committed; the checks run on a clean copy of HEAD.'
    try {
        # Outside call, program, powershell: this script, in the clean copy.
        & (Get-Process -Id $PID).Path -NoProfile -ExecutionPolicy Bypass -File (Join-Path $copy 'tools\push-check.ps1')
        $code = $LASTEXITCODE
    } finally {
        & git -C $root worktree remove --force $copy 2>&1 | Out-Null
    }

    exit $code
}

$all = @(Get-ChildItem -Path (Join-Path $root 'tests') -Filter '*.Tests.csproj' -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object { $_.BaseName -replace '\.Tests$', '' })

& git -C $root rev-parse --verify -q origin/main 2>&1 | Out-Null
$changed = if ($LASTEXITCODE -eq 0) { @(& git -C $root diff --name-only origin/main...HEAD 2>$null) } else { @('src/AxKit.Core/') }

$projects = New-Object System.Collections.Generic.List[string]
$projects.Add('AxKit')
$shared = $false
foreach ($file in $changed) {
    # What every test project compiles or reads (docs/axKit/repairs.json R-82): the shared code, the build's settings,
    # the shared test helpers and their props, the fixtures, the SDK's version. src/AxKit itself, the executable, is
    # referenced by no test project and runs AxKit.Tests alone, which always runs.
    if ($file -match '^src/AxKit\.(Core|Forms)/' -or $file -match '^Directory\.Build\.' -or $file -match '^global\.json$' -or $file -match '^tests/(Shared/|fixtures/|Directory\.Build\.)') { $shared = $true; continue }
    if ($file -match '^(src|tests)/(Ax[A-Za-z0-9]+?)(\.Core|\.Tests)?/') {
        $name = $Matches[2]
        if ($all -contains $name -and -not $projects.Contains($name)) { $projects.Add($name) }
    }
    if ($file -match '^tools/AxAutomate/' -and -not $projects.Contains('AxAutomate')) { $projects.Add('AxAutomate') }
}
if ($shared) {
    foreach ($name in $all) { if (-not $projects.Contains($name)) { $projects.Add($name) } }
}

Write-Host "Push check: the quick checks, then the test projects $($projects -join ', ')."
# The quick checks first, every one of them, so that a push learns of all its stale pages at once and in seconds,
# before the tests' minutes (2026-11-24: two pushes in a row were refused one page at a time, after the tests); the
# list of them is tools\pages.ps1 -Check's alone (docs/axKit/repairs.json R-81).
& (Join-Path $root 'tools\pages.ps1') -Check
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Push check: the quick checks above failed; nothing was pushed.'
    exit 1
}

try {
    foreach ($project in $projects) {
        $path = Join-Path $root "tests\$project.Tests\$project.Tests.csproj"
        if (-not (Test-Path -LiteralPath $path)) { continue }
        # Outside call, program, dotnet: runs one test project of this repository in Release, as test.ps1 does.
        $lines = @(& dotnet test $path --configuration Release --nologo -v q 2>&1 | ForEach-Object { "$_" })
        if ($LASTEXITCODE -ne 0) {
            # Files edited while the check ran (2026-11-24: a push refused on a half-made change that was never
            # committed): the failure says nothing of the commits, so the whole check runs again on a clean copy.
            if (@(& git -C $root status --porcelain --untracked-files=no).Count -gt 0) {
                Write-Host "Push check: $project.Tests failed, but files changed while the check ran; it runs again on a clean copy of HEAD."
                & dotnet build-server shutdown 2>&1 | Out-Null
                # Outside call, program, powershell: this script again, which now finds the tree changed and uses a clean copy.
                & (Get-Process -Id $PID).Path -NoProfile -ExecutionPolicy Bypass -File $MyInvocation.MyCommand.Path
                exit $LASTEXITCODE
            }

            $lines | Where-Object { $_ -match 'error|Assert|Expected|Actual|Failed |\[FAIL\]' } | Select-Object -First 40 | ForEach-Object { Write-Host "  $_" }
            Write-Host "Push check: $project.Tests failed."
            exit 1
        }
        Write-Host "  $project.Tests passed"
    }
} finally {
    & dotnet build-server shutdown 2>&1 | Out-Null
}

Write-Host 'Push check: every check passed.'
exit 0
