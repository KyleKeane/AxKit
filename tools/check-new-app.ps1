<#
.SYNOPSIS
  Checks that tools\new-app.ps1 still makes an app that builds and passes the bundle's checks: it adds a throwaway app
  to a clean copy of the last commit and builds and tests that copy.

.DESCRIPTION
  The new-app script writes code against the window framework, which moves under it: by 2026-11-23 the window it wrote
  called methods that were gone and stood on the bare window base, which the components test forbids, and nobody saw
  until an app was made with it (axTemplate). This check makes the mistake visible at once.

  It makes a worktree of HEAD in the temporary folder, copies this checkout's tools\new-app.ps1 over the copy's (so the
  script in hand is the one checked), runs it for the app axZeta (a standalone app of the Tools group that opens
  nothing), builds src\AxKit into a folder of its own, and runs the new app's test project and the bundle tests that
  hold every app in its places (ComponentsTests, DispatchTests, InventoryTests, KeyTests, LayoutTests, MenuTests,
  CommandMarkTests, HandKeyTests, NotesTests). The worktree is removed at the end, whatever happened. Nothing in this
  checkout changes.

  Exit code 0 when the app builds and the tests pass, 1 when something failed (the step and its output are printed).
  It takes some minutes: run it after a change to the window framework or to the script, and before a release.
  The script stays ASCII (Windows PowerShell 5.1 reads it as ANSI).

.EXAMPLE
  .\tools\check-new-app.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$copy = Join-Path ([IO.Path]::GetTempPath()) ('axkit-newapp-' + (Split-Path -Leaf $root))
$output = Join-Path ([IO.Path]::GetTempPath()) ('axkit-newapp-build-' + (Split-Path -Leaf $root))
$failed = $null

function Invoke-Step([string]$what, [scriptblock]$step) {
    Write-Host "== $what"
    $lines = @(& $step 2>&1 | ForEach-Object { "$_" })
    if ($LASTEXITCODE -ne 0) {
        $lines | Where-Object { $_ -match 'error|Assert|Expected|Actual|Failed |\[FAIL\]|^\s+at |missing|not '} | Select-Object -First 60 | ForEach-Object { Write-Host "  $_" }
        return $false
    }
    return $true
}

try {
    if (Test-Path -LiteralPath $copy) { & git -C $root worktree remove --force $copy 2>&1 | Out-Null }
    # Outside call, program, git: a worktree of this repository's last commit, in the temporary folder.
    & git -C $root worktree add --detach $copy HEAD 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No clean copy of the last commit could be made (git worktree add).' }
    Copy-Item -LiteralPath (Join-Path $root 'tools\new-app.ps1') -Destination (Join-Path $copy 'tools\new-app.ps1') -Force

    $engine = (Get-Process -Id $PID).Path
    # Outside call, program, powershell: the new-app script of the copy, which writes only into the copy.
    if (-not (Invoke-Step 'tools\new-app.ps1 axZeta' { & $engine -NoProfile -ExecutionPolicy Bypass -File (Join-Path $copy 'tools\new-app.ps1') axZeta zeta Tools Nothing -Description 'axZeta, a check of the new-app script' })) { $failed = 'the new-app script' }
    # Outside call, program, dotnet: builds the copy with the new app.
    elseif (-not (Invoke-Step 'dotnet build src\AxKit' { & dotnet build (Join-Path $copy 'src\AxKit') -c Release -o $output -nologo -v q })) { $failed = 'the build' }
    elseif (-not (Invoke-Step 'dotnet test tests\AxZeta.Tests' { & dotnet test (Join-Path $copy 'tests\AxZeta.Tests') -c Release -nologo })) { $failed = 'the new app''s tests' }
    else {
        $filter = 'FullyQualifiedName~ComponentsTests|FullyQualifiedName~DispatchTests|FullyQualifiedName~InventoryTests|FullyQualifiedName~KeyTests|FullyQualifiedName~LayoutTests|FullyQualifiedName~MenuTests|FullyQualifiedName~CommandMarkTests|FullyQualifiedName~HandKeyTests|FullyQualifiedName~NotesTests'
        if (-not (Invoke-Step 'dotnet test tests\AxKit.Tests (the places of every app)' { & dotnet test (Join-Path $copy 'tests\AxKit.Tests') -c Release -nologo --filter $filter })) { $failed = 'the bundle tests' }
    }
} catch {
    $failed = "$_"
} finally {
    & dotnet build-server shutdown 2>&1 | Out-Null
    & git -C $root worktree remove --force $copy 2>&1 | Out-Null
    if (Test-Path -LiteralPath $output) { Remove-Item -LiteralPath $output -Recurse -Force -ErrorAction SilentlyContinue }
}

if ($failed) {
    Write-Host "The new-app script makes an app that fails at ${failed}: put the script's templates right (tools\new-app.ps1)."
    exit 1
}

Write-Host 'The new-app script makes an app that builds and passes the bundle''s checks: ok'
exit 0
