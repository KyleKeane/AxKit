<#
.SYNOPSIS
  Runs the app scripts with NVDA listening: the NVDA test plans as the assistant runs them (docs/axKit/automation.md,
  section 3).

.DESCRIPTION
  Builds AxKit.exe and the runner into %TEMP%\axkit-nvda (never the running app's bin), gathers the scripts of
  tests\scripts\<app> and tests\nvda-scripts (all apps, or the one named), and runs them with tools\AxAutomate --nvda:
  the windows come to the front of the user's desktop, the keys go through SendInput, and every announcement and focus
  move a script expects must also be spoken by NVDA, read from the axkitCapture add-on (tools\nvda-capture).

  It takes the keyboard and the mouse. Run it only at the user's word, after asking them to step away; Windows' voice
  repeats the warning through the run and says when it is over. The report is %TEMP%\axkit-nvda\report.md; the result
  lines are printed without the account's paths. Exit code 0 when every script passed.

.EXAMPLE
  .\nvda-pass.ps1                 # every app's scripts and the NVDA-only scripts
  .\nvda-pass.ps1 axCalc          # tests\scripts\axCalc, and the NVDA-only scripts whose names start with axcalc
  .\nvda-pass.ps1 -NoBuild axDown # the last build again
  .\nvda-pass.ps1 -Skip axClaude  # every app but axClaude
#>
[CmdletBinding()]
param(
    # One app, by its folder name under tests\scripts (axCalc, axDown, ...); empty for all.
    [Parameter(Position = 0)]
    [string]$App = '',
    # Apps left out of a run of all, by folder name (axClaude: slow, and its replies need the network).
    [string[]]$Skip = @(),
    # Reuse the last build in %TEMP%\axkit-nvda.
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$work = Join-Path $env:TEMP 'axkit-nvda'
$exe = Join-Path $work 'axkit\AxKit.exe'
$runner = Join-Path $work 'runner\AxAutomate.exe'

$capture = Join-Path $env:APPDATA 'nvda\addons\axkitCapture\globalPlugins\axkitCapture.py'
if (-not (Test-Path $capture)) {
    Write-Host 'The NVDA capture add-on is not installed: copy tools\nvda-capture to %APPDATA%\nvda\addons\axkitCapture and restart NVDA.'
    exit 2
}
if (-not (Get-Process nvda -ErrorAction SilentlyContinue)) {
    Write-Host 'NVDA is not running.'
    exit 2
}
# Compared line by line, not by hash: the installed copy may differ in its line endings alone.
if (Compare-Object @(Get-Content $capture) @(Get-Content (Join-Path $root 'tools\nvda-capture\globalPlugins\axkitCapture.py'))) {
    Write-Host 'The installed capture add-on differs from tools\nvda-capture: copy the folder to %APPDATA%\nvda\addons\axkitCapture and restart NVDA. Running with the installed one.'
}

if (-not $NoBuild) {
    Write-Host 'Building axKit and the runner into the scratch folder...'
    dotnet build (Join-Path $root 'src\AxKit\AxKit.csproj') -c Release -o (Join-Path $work 'axkit') --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host 'axKit did not build.'; exit $LASTEXITCODE }
    dotnet build (Join-Path $root 'tools\AxAutomate\AxAutomate.csproj') -c Release -o (Join-Path $work 'runner') --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host 'The runner did not build.'; exit $LASTEXITCODE }
}

# The scripts of the pass in one folder, each app's in its own subfolder, in the runner's order.
$scripts = Join-Path $work 'scripts'
if (Test-Path $scripts) { Remove-Item $scripts -Recurse -Force }
New-Item -ItemType Directory -Force $scripts | Out-Null
# The folders carry the apps' spoken names (axCalc, axDown; 5.6.0), matched whatever case is typed.
$apps = @(Get-ChildItem (Join-Path $root 'tests\scripts') -Directory |
    Where-Object { ($App -eq '' -or $_.Name -eq $App) -and $_.Name -notin $Skip })
if ($App -ne '' -and $apps.Count -eq 0) {
    Write-Host "No scripts in tests\scripts\$App."
    exit 1
}
foreach ($folder in $apps) {
    Copy-Item $folder.FullName (Join-Path $scripts $folder.Name) -Recurse
}
$nvdaOnly = Join-Path $scripts 'zz-nvda'
New-Item -ItemType Directory -Force $nvdaOnly | Out-Null
Get-ChildItem (Join-Path $root 'tests\nvda-scripts') -Filter *.txt |
    Where-Object { $App -eq '' -or $_.Name.StartsWith($App, [StringComparison]::OrdinalIgnoreCase) } |
    Copy-Item -Destination $nvdaOnly

$report = Join-Path $work 'report.md'
# The runner's error output is not redirected: under $ErrorActionPreference = 'Stop', Windows PowerShell 5.1 would turn
# its first line into a terminating error and end this script before the summary (the root CLAUDE.md's 2>&1 gotcha).
& $runner $scripts --exe $exe --nvda --report $report |
    ForEach-Object { "$_" } |
    Where-Object { $_ -match '^(==|FAIL|PASS|FAILED|NVDA tier|NVDA did not)' -or $_ -match '^\s*(FAIL|failed)' } |
    ForEach-Object { $_ -replace '[A-Za-z]:(\\\\|\\)Users(\\\\|\\)[^\\"\s]+', '%USERPROFILE%' }
$code = $LASTEXITCODE
Write-Host 'Report: %TEMP%\axkit-nvda\report.md'
exit $code
