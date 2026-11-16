<#
.SYNOPSIS
  Runs the app scripts with NVDA listening: the NVDA test plans as the assistant runs them (docs/axKit/automation.md,
  section 3).

.DESCRIPTION
  Builds AxKit.exe and the runner into %TEMP%\axkit-nvda (never the running app's bin), gathers the scripts of
  tests\scripts\<app> and tests\nvda-scripts (all apps, or the one named), and runs them with tools\AxAutomate --nvda:
  the windows come to the front of the user's desktop, the keys go through SendInput, and every announcement and focus
  move a script expects must also be spoken by NVDA, read from the axkitCapture add-on (tools\nvda-capture).

  It takes the keyboard and the mouse. Run it only at the user's word, after asking them to step away: it refuses to
  start (exit code 2) when NVDA is not running, when the installed capture add-on is not the repository's, and,
  right before the runner takes the keyboard, while the last key or mouse input is under 20 seconds old (it waits a
  minute for that quiet first). Windows' voice repeats the warning through the run and says
  when it is over. The report is %TEMP%\axkit-nvda\report.md; the result
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

# The run takes the keyboard and the mouse, so the user must have stepped away (docs/axKit/automation.md section 3):
# a key or a mouse move in the last 20 seconds means someone is at the computer, and the run does not start. Windows
# keeps the time of the last input of the session (GetLastInputInfo); the runner's own keys come later and are not
# asked about. Asked once, right before the runner takes the keyboard: the build before it is usually longer than
# the 20 seconds after the user's answer to "step away". When the input is newer, the script waits for the quiet, a
# minute at most, and then refuses.
Add-Type -Namespace AxKitNvdaPass -Name LastInput -MemberDefinition @'
[StructLayout(LayoutKind.Sequential)]
public struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
[DllImport("user32.dll")]
public static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
public static int SecondsAgo() {
    LASTINPUTINFO info = new LASTINPUTINFO();
    info.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
    if (!GetLastInputInfo(ref info)) { return -1; }
    return (int)(unchecked((uint)Environment.TickCount - info.dwTime) / 1000);
}
'@
function Stop-WhenSomeoneIsThere {
    $seconds = [AxKitNvdaPass.LastInput]::SecondsAgo()
    for ($waited = 0; $seconds -ge 0 -and $seconds -lt 20 -and $waited -lt 60; $waited++) {
        if ($waited -eq 0) { Write-Host "The last key or mouse input was $seconds seconds ago: waiting for 20 seconds without input..." }
        Start-Sleep -Seconds 1
        $seconds = [AxKitNvdaPass.LastInput]::SecondsAgo()
    }
    if ($seconds -lt 0) {
        Write-Host 'Not started: Windows did not say when the last key or mouse input was, so nobody is known to have stepped away.'
        exit 2
    }
    if ($seconds -lt 20) {
        Write-Host "Not started: the last key or mouse input was $seconds seconds ago, so someone is at the computer. The run takes the keyboard: ask the user to step away, then start it again (-NoBuild runs the build that is there)."
        exit 2
    }
}

$capture = Join-Path $env:APPDATA 'nvda\addons\axkitCapture\globalPlugins\axkitCapture.py'
if (-not (Test-Path $capture)) {
    Write-Host 'The NVDA capture add-on is not installed: copy tools\nvda-capture to %APPDATA%\nvda\addons\axkitCapture and restart NVDA.'
    exit 2
}
if (-not (Get-Process nvda -ErrorAction SilentlyContinue)) {
    Write-Host 'NVDA is not running.'
    exit 2
}
# The installed add-on is the repository's: a pass heard through another version of it proves nothing about this one's
# scripts, so the run stops (a warning until 2026-11-16). Compared line by line, not by hash: the installed copy may
# differ in its line endings alone.
if (Compare-Object @(Get-Content $capture) @(Get-Content (Join-Path $root 'tools\nvda-capture\globalPlugins\axkitCapture.py'))) {
    Write-Host 'Not started: the installed capture add-on differs from tools\nvda-capture. Copy the folder to %APPDATA%\nvda\addons\axkitCapture, restart NVDA and start the pass again.'
    exit 2
}

if (-not $NoBuild) {
    Write-Host 'Building axKit and the runner into the scratch folder...'
    dotnet build (Join-Path $root 'src\AxKit\AxKit.csproj') -c Release -o (Join-Path $work 'axkit') --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host 'axKit did not build.'; exit $LASTEXITCODE }
    dotnet build (Join-Path $root 'tools\AxAutomate\AxAutomate.Runner.csproj') -c Release -o (Join-Path $work 'runner') --nologo -v q | Out-Null
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
Stop-WhenSomeoneIsThere
# The runner's error output is not redirected: under $ErrorActionPreference = 'Stop', Windows PowerShell 5.1 would turn
# its first line into a terminating error and end this script before the summary (the root CLAUDE.md's 2>&1 gotcha).
& $runner $scripts --exe $exe --nvda --record --repository $root --report $report |
    ForEach-Object { "$_" } |
    Where-Object { $_ -match '^(==|FAIL|PASS|FAILED|STOPPED|NVDA tier|The NVDA tier|NVDA did not|Goals:|The command inventory|Recorded)' -or $_ -match '^\s*(FAIL|failed)' } |
    ForEach-Object { $_ -replace '[A-Za-z]:(\\\\|\\)Users(\\\\|\\)[^\\"\s]+', '%USERPROFILE%' }
$code = $LASTEXITCODE
Write-Host 'Report: %TEMP%\axkit-nvda\report.md'
# The pass has written what it heard into the workflow record: the owed page is written again from it, so a hearing
# that names its script (tools\owed.ps1) leaves the page with the pass and not by anyone reading the report.
& (Join-Path $root 'tools\owed.ps1')
exit $code
