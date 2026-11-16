<#
.SYNOPSIS
Says which apps' windows changed after their last recorded pass of the automated window checks, and fails for a
release when any did (-Check).

.DESCRIPTION
The rule it holds: a window that changed is checked by its scripts before it is released (docs/axKit/automation.md).
It was an instruction, "run them after a change to a window", and nothing held a release to it.

The record is docs\axKit\workflow-record.json, which a run of the scripts writes when given --record
(docs/axKit/user-workflows.md WF-6): for every goal of every script, on the desktop tier, the commit of its last
pass. For each app of Dispatch.Apps the script takes the newest commit at which a goal of the app's scripts
(tests\scripts\<app>) passed on the desktop tier, and asks git whether the app's window code has a commit after it:
the app's own folders (src\<App>, src\<App>.Core) and the two every window stands on (src\AxKit.Forms,
src\AxKit.Core). An app is named when

  - its window code has a commit after its last recorded pass,
  - it has no recorded pass on the desktop tier at all, or
  - the commit of its pass is not in this repository.

A pass recorded over work not yet committed carries a + after its commit (WF-6.3). The record cannot say what that
work was, so the commit that later holds it counts as a change after the pass: the scripts are run with --record once
more from the committed tree before a release. A record's own commit touches no window code and changes nothing.

release.ps1 and release.ps1 -Rehearse run -Check. test.ps1 does not: during ordinary work the windows are ahead of
their record most of the time, and a check that fails all day is read by nobody. Without -Check the script prints the
same lines and exits with 0. It reads the record and asks git; it writes nothing.

.EXAMPLE
  .\tools\window-passes.ps1            # what a release would say, exit code 0
  .\tools\window-passes.ps1 -Check     # the same, exit code 1 when an app is named
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
. (Join-Path $root 'tools\apps.ps1')

$record = [IO.File]::ReadAllText((Join-Path $root 'docs\axKit\workflow-record.json')) | ConvertFrom-Json

# The commits at which each app's goals passed on the desktop tier, by the app's folder under tests/scripts.
$passes = @{}
foreach ($goal in $record.goals) {
    if ($goal.script -notmatch '^tests/scripts/([^/]+)/') { continue }
    $folder = $Matches[1].ToLowerInvariant()
    $desktop = $goal.tiers.desktop
    if (-not $desktop -or -not $desktop.passed -or -not $desktop.passed.commit) { continue }
    if (-not $passes.ContainsKey($folder)) { $passes[$folder] = @{} }
    $passes[$folder][$desktop.passed.commit] = $true
}

$named = New-Object System.Collections.Generic.List[string]
$current = 0
foreach ($app in @(Get-AxKitApps $root)) {
    $folder = $app.Name.ToLowerInvariant()
    if (-not $passes.ContainsKey($folder)) {
        $named.Add("$($app.Name) has no recorded pass of its window checks: run its scripts with --record.")
        continue
    }

    # The newest of the app's passes: the one with the fewest commits between it and HEAD.
    $newest = $null
    $fewest = -1
    foreach ($commit in $passes[$folder].Keys) {
        $sha = $commit.TrimEnd('+')
        # --quiet: nothing on the error stream, which Windows PowerShell would turn into an error under Stop.
        git -C $root rev-parse --verify --quiet "$sha^{commit}" | Out-Null
        if ($LASTEXITCODE -ne 0) { continue }
        $after = [int]"$(git -C $root rev-list --count "$sha..HEAD")".Trim()
        if ($fewest -lt 0 -or $after -lt $fewest) { $fewest = $after; $newest = $commit }
    }
    if (-not $newest) {
        $named.Add("$($app.Name)'s recorded pass is at a commit this repository does not have ($(@($passes[$folder].Keys) -join ', ')): run its scripts with --record.")
        continue
    }

    $folders = @("src/$($app.Project)", "src/$($app.Project).Core", 'src/AxKit.Forms', 'src/AxKit.Core') |
        Where-Object { Test-Path -LiteralPath (Join-Path $root $_) }
    $changes = @(git -C $root log --format=%h "$($newest.TrimEnd('+'))..HEAD" -- $folders)
    if ($changes.Count -eq 0) {
        $current++
        continue
    }

    $over = if ($newest.EndsWith('+')) { ', which was recorded over work not yet committed' } else { '' }
    $named.Add("$($app.Name)'s window changed after its last recorded pass ($newest$over; $($changes.Count) commit(s) since, the newest $($changes[0])): run its scripts with --record.")
}

$named | ForEach-Object { Write-Host $_ }
if ($named.Count -gt 0) { Write-Host 'To run an app''s scripts with --record: dotnet run --project tools\AxAutomate -c Release -- tests\scripts\<app> --build --record' }
Write-Host "Window checks against docs/axKit/workflow-record.json: $current app(s) current, $($named.Count) changed since the last recorded pass."
if ($Check -and $named.Count -gt 0) { exit 1 }
exit 0
