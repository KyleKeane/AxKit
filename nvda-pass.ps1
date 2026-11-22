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
  start (exit code 2) when NVDA is not running and when the installed capture add-on is not the repository's. It
  speaks before it takes anything (the user's ask of 2026-11-21, after he touched the keyboard not knowing a pass
  was about to start): Windows' voice says at the script's start that a pass is being prepared, and right before
  the runner takes the keyboard it says one short sentence and counts from ten to one aloud; a key or a mouse move after the countdown's
  sentence began stops the pass for good (exit code 2), said in the same voice. -WarningOnly plays the warning and
  the countdown alone, and starts nothing. Windows' voice then repeats the warning through the run, with the number
  of the test that runs, how many remain and about how long they take, and says
  when it is over. The report is %TEMP%\axkit-nvda\report.md; the result
  lines are printed without the account's paths. Exit code 0 when every script passed.

.EXAMPLE
  .\nvda-pass.ps1                 # every app's scripts and the NVDA-only scripts
  .\nvda-pass.ps1 axCalc          # tests\scripts\axCalc, and the NVDA-only scripts whose names start with axcalc
  .\nvda-pass.ps1 -NoBuild axDown # the last build again
  .\nvda-pass.ps1 -Skip axClaude  # every app but axClaude
  .\nvda-pass.ps1 -Only '*-stream-*', 'axMedia\03-playing.txt'   # only these scripts, heard again after a repair
  .\nvda-pass.ps1 axHelp -Only '01-start.txt' -Inventory write   # an app only live scripts list: its commands written
#>
[CmdletBinding()]
param(
    # One app, by its folder name under tests\scripts (axCalc, axDown, ...); empty for all.
    [Parameter(Position = 0)]
    [string]$App = '',
    # Apps left out of a run of all, by folder name (axClaude: slow, and its replies need the network).
    [string[]]$Skip = @(),
    # Reuse the last build in %TEMP%\axkit-nvda.
    [switch]$NoBuild,
    # Only the scripts that match one of these patterns (wildcards as -like takes them), by the file's name
    # ('*-stream-*') or by its folder and name ('axMedia\03-playing.txt'), the NVDA-only scripts as zz-nvda\<name>:
    # for hearing again what a pass failed, after the repair, without the two hours of the whole pass (2026-11-18).
    [string[]]$Only = @(),
    # The runner's --inventory mode for this pass; without it the runner's own default, check. 'write' is how the
    # command inventory gets the rows of an app whose scripts are all live (axHelp), which no run on the hidden
    # desktop lists: only the scripts with a "check commands" or "do" step play then (2026-11-18).
    [ValidateSet('', 'check', 'write', 'rewrite', 'off')]
    [string]$Inventory = '',
    # The spoken warning and the countdown alone, for hearing them: nothing is built and no pass starts.
    [switch]$WarningOnly
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$work = Join-Path $env:TEMP 'axkit-nvda'
$exe = Join-Path $work 'axkit\AxKit.exe'
$runner = Join-Path $work 'runner\AxAutomate.exe'

# The run takes the keyboard and the mouse, so the user must have stepped away (docs/axKit/automation.md section 3),
# and must hear that it is coming before it takes anything (the user's ask, 2026-11-21: he touched the keyboard not
# knowing a pass was about to start; until then the script waited in silence for 20 seconds without input, and the
# first spoken word came from the runner, with the keyboard already taken). So Windows' own voice speaks from the
# script's start, and right before the runner takes the keyboard it counts from ten to one aloud. A key or a mouse
# move after the countdown's sentence began means someone is at the computer: the pass stops for good and says so.
# Windows keeps the time of the last input of the session (GetLastInputInfo); the runner's own keys come later and
# are not asked about. The voice is SAPI's, as the runner's is: NVDA never speaks it, and no window takes the focus.
Add-Type -Namespace AxKitNvdaPass -Name LastInput -MemberDefinition @'
[StructLayout(LayoutKind.Sequential)]
public struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }
[DllImport("user32.dll")]
public static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
public static long MillisecondsAgo() {
    LASTINPUTINFO info = new LASTINPUTINFO();
    info.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));
    if (!GetLastInputInfo(ref info)) { return -1; }
    return (long)unchecked((uint)Environment.TickCount - info.dwTime);
}
'@
$spoken = $null
try { $spoken = New-Object -ComObject SAPI.SpVoice } catch { $spoken = $null }

# One sentence in Windows' voice, also written as a line. -Whole waits until it has been spoken to its end; without
# it the script goes on while the voice speaks. A new sentence takes the place of one still being spoken. A computer
# without the voice gets a tone, so that something is heard.
function Write-Spoken([string]$Sentence, [switch]$Whole) {
    Write-Host "(Windows' voice: $Sentence)"
    if ($null -eq $spoken) {
        [Console]::Beep(880, 400)
        return
    }
    # SAPI's flags: 1 speaks without waiting, 2 ends what is still being spoken.
    $flags = 3
    if ($Whole) { $flags = 2 }
    try { [void]$spoken.Speak($Sentence, $flags) } catch { [Console]::Beep(880, 400) }
}

# The spoken countdown right before the runner takes the keyboard: one sentence, then ten seconds counted aloud;
# any key or mouse input since the sentence began stops the pass for good.
function Stop-WhenSomeoneIsThere {
    if ([AxKitNvdaPass.LastInput]::MillisecondsAgo() -lt 0) {
        Write-Spoken 'No test pass.' -Whole
        Write-Host 'Not started: Windows did not say when the last key or mouse input was, so nobody is known to have stepped away.'
        exit 2
    }
    # One short sentence, then every second from ten to one (the user's words on hearing the first form, 2026-11-21:
    # "shorter sentence and just count down from ten").
    $began = [Diagnostics.Stopwatch]::StartNew()
    Write-Spoken 'NVDA test pass. Press any key to stop it.' -Whole
    for ($left = 10; $left -ge 0; $left--) {
        $ago = [AxKitNvdaPass.LastInput]::MillisecondsAgo()
        if ($ago -ge 0 -and $ago -lt $began.ElapsedMilliseconds) {
            Write-Spoken 'Stopped. No test pass.' -Whole
            Write-Host 'Not started: a key or the mouse was used during the spoken countdown, so someone is at the computer. The run takes the keyboard: ask the user to step away, then start it again (-NoBuild runs the build that is there).'
            exit 2
        }
        if ($left -gt 0) {
            Write-Spoken "$left"
            Start-Sleep -Seconds 1
        }
    }
}

if ($WarningOnly) {
    Write-Spoken 'Only the warning: nothing starts.' -Whole
    Write-Spoken 'NVDA test pass being prepared.' -Whole
    Stop-WhenSomeoneIsThere
    Write-Spoken 'Here the pass would start.' -Whole
    exit 0
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

# Said before anything is built, so that whoever is at the computer knows from the first moment what is coming.
Write-Spoken 'NVDA test pass being prepared.'

if (-not $NoBuild) {
    Write-Host 'Building axKit and the runner into the scratch folder...'
    dotnet build (Join-Path $root 'src\AxKit\AxKit.csproj') -c Release -o (Join-Path $work 'axkit') --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Spoken 'No test pass.' -Whole; Write-Host 'axKit did not build.'; exit $LASTEXITCODE }
    dotnet build (Join-Path $root 'tools\AxAutomate\AxAutomate.Runner.csproj') -c Release -o (Join-Path $work 'runner') --nologo -v q | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Spoken 'No test pass.' -Whole; Write-Host 'The runner did not build.'; exit $LASTEXITCODE }
}

# The scripts of the pass in one folder, each app's in its own subfolder, in the runner's order.
$scripts = Join-Path $work 'scripts'
if (Test-Path $scripts) { Remove-Item $scripts -Recurse -Force }
New-Item -ItemType Directory -Force $scripts | Out-Null
# The folders carry the apps' spoken names (axCalc, axDown; 5.6.0), matched whatever case is typed.
$apps = @(Get-ChildItem (Join-Path $root 'tests\scripts') -Directory |
    Where-Object { ($App -eq '' -or $_.Name -eq $App) -and $_.Name -notin $Skip })
if ($App -ne '' -and $apps.Count -eq 0) {
    Write-Spoken 'No test pass.' -Whole
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

# -Only: of what was gathered, the scripts that match no pattern are taken away again. A script is a file the runner
# would play and enter in the record: a .txt file with its "# Workflow:" line, or a workflow file. The other files of
# a folder stay (message.txt beside axClaude's scripts is what one of them sends), for the scripts that read them.
if ($Only.Count -gt 0) {
    $kept = 0
    foreach ($file in @(Get-ChildItem $scripts -Recurse -File)) {
        $isScript = $file.Name -like '*.workflow.json' -or
            ($file.Extension -eq '.txt' -and @(Get-Content -LiteralPath $file.FullName -TotalCount 3 | Where-Object { $_ -like '# Workflow:*' }).Count -gt 0)
        if (-not $isScript) { continue }
        $place = "$($file.Directory.Name)\$($file.Name)"
        $wanted = $false
        foreach ($pattern in $Only) {
            if ($file.Name -like $pattern -or $place -like $pattern) { $wanted = $true }
        }
        if ($wanted) { $kept++ } else { Remove-Item -LiteralPath $file.FullName -Force }
    }
    if ($kept -eq 0) {
        Write-Spoken 'No test pass.' -Whole
        Write-Host "No script matches -Only $($Only -join ', ')."
        exit 1
    }
    Write-Host "Only $kept script(s): the ones that match $($Only -join ', ')."
}

$report = Join-Path $work 'report.md'
$more = @()
if ($Inventory -ne '') { $more = @('--inventory', $Inventory) }
Stop-WhenSomeoneIsThere
# The runner's error output is not redirected: under $ErrorActionPreference = 'Stop', Windows PowerShell 5.1 would turn
# its first line into a terminating error and end this script before the summary (the root CLAUDE.md's 2>&1 gotcha).
& $runner $scripts --exe $exe --nvda --record --repository $root --report $report @more |
    ForEach-Object { "$_" } |
    # What the run says of the command inventory is shown whole, the rows it names among it ("  axHelp: ... has also
    # ctrl+slash; the inventory has none"): until 2026-11-18 only its first line was, and the row had to be looked for
    # in the report.
    Where-Object { $_ -match '^(==|FAIL|PASS|FAILED|STOPPED|NVDA tier|The NVDA tier|NVDA did not|Goals:|The command inventory|--inventory|Whole in this run|Recorded|\(Windows'' voice|\(The scripts'' times)' -or $_ -match '^\s*(FAIL|failed)' -or $_ -match '^\s+\S+: .*the inventory' } |
    ForEach-Object { $_ -replace '[A-Za-z]:(\\\\|\\)Users(\\\\|\\)[^\\"\s]+', '%USERPROFILE%' }
$code = $LASTEXITCODE
Write-Host 'Report: %TEMP%\axkit-nvda\report.md'
# The pass has written what it heard into the workflow record: the owed page is written again from it, so a hearing
# that names its script (tools\owed.ps1) leaves the page with the pass and not by anyone reading the report.
& (Join-Path $root 'tools\owed.ps1')
exit $code
