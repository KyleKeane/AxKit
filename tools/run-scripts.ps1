<#
.SYNOPSIS
  Runs window scripts on the hidden desktop from a runner of its own: several scripts or folders in one call, the
  output redacted, one line per script at the end.

.DESCRIPTION
  The pitfalls this script removes (the review of the sessions of 2026-11-25):
  - "dotnet run --project tools\AxAutomate --no-build" ran the runner from its own bin folder, and a check started
    beside it (tools\upstream.ps1 -Verify, which builds that folder) could not copy its files (MSB3027) and failed
    twice: here the runner is built into a folder of its own under %TEMP%, by the commit and the state of the
    runner's sources, and run from there, so nothing else's build meets it;
  - the runner takes one script or folder a call, and a loop of calls in a foreach statement could not be redirected
    (PowerShell refuses *> after a statement): here any number of scripts and folders are given at once;
  - the output carried the account path, replaced by hand: here it goes through tools\redact.ps1.
  The runner's own lock (RunLock) still keeps two runs of the hidden desktop apart; a run waits for the one before.
  -Exe names the AxKit.exe to run (by default the scratch build, %TEMP%\axkit-scratch\program\AxKit.exe); -Live adds
  the live scripts; -Record writes the passes into docs\axKit\workflow-record.json; -Inventory passes check, write,
  rewrite or off (off by default). -Full prints every line of the runs, not only the failures and the totals. The
  scratch build is refused when it is older than the newest file under src (a run once recorded passes for code it
  had not played). Exit code 0 when every script passed, 1 otherwise, 2 when the runner did not build or the build is
  stale. The script stays ASCII.

.EXAMPLE
  .\tools\run-scripts.ps1 tests\scripts\axTask\26-the-day-an-item-was-made.txt tests\scripts\axMedia
  .\tools\run-scripts.ps1 tests\scripts\axClaude\05-modes.txt -Live -Record
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0, ValueFromRemainingArguments = $true)][string[]]$Scripts,
    [string]$Exe = '',
    [switch]$Live,
    [switch]$Record,
    [ValidateSet('check', 'write', 'rewrite', 'off')][string]$Inventory = 'off',
    [switch]$Full
)

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot
$named = [bool]$Exe
if (-not $Exe) { $Exe = Join-Path $env:TEMP 'axkit-scratch\program\AxKit.exe' }
if (-not (Test-Path -LiteralPath $Exe)) { Write-Host "No AxKit.exe at $Exe : build it first (dotnet build src\AxKit -c Release -o <folder>)."; exit 2 }
# A build older than the code it would check (2026-11-26: a live run with -Record played the scratch build of the
# morning, and the record named a commit it had not tested). The scratch build is refused; an -Exe named on purpose
# (an older release, a replay) is only said to be older.
$sourceFiles = @(Get-ChildItem (Join-Path $root 'src') -Recurse -File -Include *.cs, *.csproj, *.json, *.resx | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
$newest = ($sourceFiles | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum
if ($newest -and (Get-Item -LiteralPath $Exe).LastWriteTimeUtc -lt $newest) {
    if (-not $named) {
        Write-Host "The scratch AxKit.exe is older than the code under src: build it first (dotnet build src\AxKit -c Release -o $(Split-Path $Exe -Parent))."
        exit 2
    }
    Write-Host 'Note: the AxKit.exe named is older than the code under src.'
}
# Every script and folder there before anything runs: a guessed name made the runner end in a stack trace.
$absent = @($Scripts | Where-Object { -not (Test-Path -LiteralPath $_) -and -not (Test-Path -LiteralPath (Join-Path $root $_)) })
if ($absent.Count -gt 0) {
    foreach ($one in $absent) {
        $folder = Split-Path $one -Parent
        $near = if ($folder -and (Test-Path -LiteralPath (Join-Path $root $folder))) { @(Get-ChildItem (Join-Path $root $folder) -Filter ((Split-Path $one -Leaf).Split('-')[0] + '*') | ForEach-Object { $_.Name }) } else { @() }
        Write-Host "No script or folder: $one$(if ($near.Count -gt 0) { ' (beside it: ' + ($near -join ', ') + ')' })"
    }
    exit 2
}

# The runner's own folder, named by the commit and by the newest change among its sources, so that a change made
# since is built and an unchanged runner is not built twice.
$sources = @(Get-ChildItem (Join-Path $root 'tools\AxAutomate'), (Join-Path $root 'src\AxAutomate.Core'), (Join-Path $root 'src\AxKit.Core') -Recurse -File -Include *.cs, *.csproj | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
$stamp = ($sources | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum.Ticks
$head = "$(git -C $root rev-parse --short HEAD)".Trim()
$runnerFolder = Join-Path $env:TEMP ("axkit-runner\" + $head + '-' + $stamp)
$runner = Join-Path $runnerFolder 'AxAutomate.exe'
if (-not (Test-Path -LiteralPath $runner)) {
    Write-Host 'Building the runner into a folder of its own...'
    & dotnet build (Join-Path $root 'tools\AxAutomate') -c Release -o $runnerFolder --nologo -v q 2>&1 | Where-Object { $_ -match ' error ' } | ForEach-Object { "$_" }
    $built = $LASTEXITCODE
    & dotnet build-server shutdown | Out-Null
    if ($built -ne 0 -or -not (Test-Path -LiteralPath $runner)) { Write-Host 'The runner did not build.'; exit 2 }
}

$results = New-Object System.Collections.Generic.List[string]
$failed = $false
foreach ($script in $Scripts) {
    $arguments = @($script, '--exe', $Exe, '--inventory', $Inventory, '--repository', $root)
    if ($Live) { $arguments += '--live' }
    if ($Record) { $arguments += '--record' }
    $lines = @(& $runner @arguments 2>&1 | ForEach-Object { "$_" } | & (Join-Path $PSScriptRoot 'redact.ps1'))
    $code = $LASTEXITCODE
    if ($Full) {
        $lines | ForEach-Object { $_ }
    } else {
        $lines | Where-Object { $_ -match '^(FAIL \d+|  FAILED|FAILED:|The command inventory|Rows this run)' } | ForEach-Object { if ($_.Length -gt 600) { $_.Substring(0, 600) + ' ...' } else { $_ } }
    }
    $total = @($lines | Where-Object { $_ -match '^(PASS|FAILED):' } | Select-Object -Last 1)
    $goals = @($lines | Where-Object { $_ -match '^Goals:' } | Select-Object -Last 1)
    $results.Add("$script : $(if ($total) { $total[0] } else { "exit $code" })$(if ($goals) { '; ' + $goals[0] } else { '' })")
    if ($code -ne 0) { $failed = $true }
}

$results | ForEach-Object { $_ }
if ($failed) { exit 1 }
exit 0
