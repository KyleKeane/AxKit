<#
.SYNOPSIS
Says which apps' windows changed after their last recorded pass of the automated window checks, fails for a release
when any did (-Check), or runs the checks of the apps it names, with the record on (-Run).

.DESCRIPTION
The rule it holds: a window that changed is checked by its scripts before it is released (docs/axKit/automation.md).
It was an instruction, "run them after a change to a window", and nothing held a release to it.

The record is docs\axKit\workflow-record.json, which a run of the scripts writes when given --record
(docs/axKit/user-workflows.md WF-6): for every goal of every script, on the desktop tier, the commit of its last
pass. For each app of Dispatch.Apps the script takes the newest commit at which a goal of the app's scripts
(tests\scripts\<app>) passed on the desktop tier or with NVDA listening (the nvda tier), and asks git whether the app's window code has a commit after it:
the app's own folders (src\<App>, src\<App>.Core) and the two every window stands on (src\AxKit.Forms,
src\AxKit.Core). An app is named when

  - its window code has a commit after its last recorded pass,
  - it has no recorded pass on either tier at all, or
  - the commit of its pass is not in this repository.

A pass recorded over work not yet committed carries a + after its commit (WF-6.3). The record cannot say what that
work was, so the commit that later holds it counts as a change after the pass: the scripts are run with --record once
more from the committed tree before a release. A record's own commit touches no window code and changes nothing.

release.ps1 and release.ps1 -Rehearse run -Check. test.ps1 does not: during ordinary work the windows are ahead of
their record most of the time, and a check that fails all day is read by nobody. Without -Check the script prints the
same lines and exits with 0. Without -Run it reads the record and asks git; it writes nothing.

-Run does what the lines ask for. It builds AxKit once into a scratch folder (never into src\AxKit\bin), runs the
scripts folder of each app named above on the hidden desktop with --record (tools\window-run.ps1), prints one line
per app (passed, or the failing scripts) and ends with what is still named. A script whose "# Needs:" line names
Claude Code signed in, the network or GitHub is never run here: an app that has one is run script by script without
it, and its line says how many were left out; an app with live scripts alone (axHelp) stays named until a person
runs them. -Only <app>, ... runs the named apps instead, changed or not. It refuses while src or tests hold a change
that is not committed: a pass recorded over such work carries the + and counts as changed again with the next
commit, so the run would be for nothing. The record file it changes is left in the working tree, to be committed.
Exit code 0 when every script run passed, 1 when one failed, 2 when the run was refused or the build failed.

.EXAMPLE
  .\tools\window-passes.ps1            # what a release would say, exit code 0
  .\tools\window-passes.ps1 -Check     # the same, exit code 1 when an app is named
  .\tools\window-passes.ps1 -Run       # run the scripts of every app named, with the record on
  .\tools\window-passes.ps1 -Run -Only axCalc
#>
[CmdletBinding()]
param([switch]$Check, [switch]$Run, [string[]]$Only)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
. (Join-Path $root 'tools\apps.ps1')
. (Join-Path $root 'tools\window-run.ps1')

# The apps whose window code changed after their last recorded pass: for each its Name and the Line that says why.
function Get-ChangedWindows {
    $record = [IO.File]::ReadAllText((Join-Path $root 'docs\axKit\workflow-record.json')) | ConvertFrom-Json

    # The commits at which each app's goals passed on the desktop tier, by the app's folder under tests/scripts.
    $passes = @{}
    foreach ($goal in $record.goals) {
        if ($goal.script -notmatch '^tests/scripts/([^/]+)/') { continue }
        $folder = $Matches[1].ToLowerInvariant()
        # A pass with NVDA listening plays the same script on the real desktop and counts as well: it is the only
        # tier at which a script that talks to the real Claude Code is run (axHelp's two).
        foreach ($tier in @($goal.tiers.desktop, $goal.tiers.nvda)) {
            if (-not $tier -or -not $tier.passed -or -not $tier.passed.commit) { continue }
            if (-not $passes.ContainsKey($folder)) { $passes[$folder] = @{} }
            $passes[$folder][$tier.passed.commit] = $true
        }
    }

    foreach ($app in @(Get-AxKitApps $root)) {
        $folder = $app.Name.ToLowerInvariant()
        if (-not $passes.ContainsKey($folder)) {
            [pscustomobject]@{ Name = $app.Name; Line = "$($app.Name) has no recorded pass of its window checks: run its scripts with --record." }
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
            [pscustomobject]@{ Name = $app.Name; Line = "$($app.Name)'s recorded pass is at a commit this repository does not have ($(@($passes[$folder].Keys) -join ', ')): run its scripts with --record." }
            continue
        }

        $folders = @("src/$($app.Project)", "src/$($app.Project).Core", 'src/AxKit.Forms', 'src/AxKit.Core') |
            Where-Object { Test-Path -LiteralPath (Join-Path $root $_) }
        $changes = @(git -C $root log --format=%h "$($newest.TrimEnd('+'))..HEAD" -- $folders)
        if ($changes.Count -eq 0) { continue }

        $over = if ($newest.EndsWith('+')) { ', which was recorded over work not yet committed' } else { '' }
        [pscustomobject]@{ Name = $app.Name; Line = "$($app.Name)'s window changed after its last recorded pass ($newest$over; $($changes.Count) commit(s) since, the newest $($changes[0])): run its scripts with --record." }
    }
}

$apps = @(Get-AxKitApps $root)
$named = @(Get-ChangedWindows)

if ($Run) {
    $names = @($named | ForEach-Object { $_.Name })
    if ($Only) {
        $names = @()
        foreach ($one in $Only) {
            $known = @($apps | Where-Object { $_.Name -ieq $one })
            if ($known.Count -eq 0) {
                Write-Host "No app is named $one. The apps: $(@($apps | ForEach-Object { $_.Name }) -join ', ')."
                exit 2
            }
            $names += $known[0].Name
        }
    }
    if ($names.Count -eq 0) {
        Write-Host 'No window changed after its last recorded pass: nothing to run.'
        exit 0
    }

    $uncommitted = @(git -C $root status --porcelain -- src tests)
    if ($uncommitted.Count -gt 0) {
        Write-Host "Not run: src or tests hold $($uncommitted.Count) change(s) that are not committed. A pass recorded over work not yet committed counts as changed again with the commit that holds the work, so the run would be for nothing. Commit first, then run this again."
        exit 2
    }

    $scratch = Join-Path ([IO.Path]::GetTempPath()) "axkit-window-run\$PID"
    Write-Host "Building axKit once into a scratch folder, then $($names.Count) app(s): $($names -join ', ')."
    $exe = Build-WindowRun $root $scratch
    if (-not $exe) {
        Write-Host 'Not run: the build failed.'
        exit 2
    }

    $failures = 0
    try {
        foreach ($name in $names) {
            $result = Invoke-WindowScripts $root $exe $name
            Write-Host $result.Line
            $failures += @($result.Failed).Count
        }
    } finally {
        Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
    }

    $named = @(Get-ChangedWindows)
    if ($named.Count -eq 0) { Write-Host 'Still changed since the last recorded pass: none.' }
    else { Write-Host "Still changed since the last recorded pass: $(@($named | ForEach-Object { $_.Name }) -join ', ')." }
    Write-Host 'The passes are in docs/axKit/workflow-record.json, in the working tree: commit it.'
    if ($failures -gt 0) { exit 1 }
    exit 0
}

$named | ForEach-Object { Write-Host $_.Line }
if ($named.Count -gt 0) { Write-Host 'To run them all with --record: tools\window-passes.ps1 -Run. One app''s: dotnet run --project tools\AxAutomate -c Release -- tests\scripts\<app> --build --record' }
Write-Host "Window checks against docs/axKit/workflow-record.json: $($apps.Count - $named.Count) app(s) current, $($named.Count) changed since the last recorded pass."
if ($Check -and $named.Count -gt 0) { exit 1 }
exit 0
