<#
.SYNOPSIS
  The start of a working session, in one run: is main behind origin/main, where do the repositories beside this one
  stand, do the reports on GitHub and the priorities list agree, did an outside program change. A few lines, each
  starting with what was checked and ending with "ok" or with what to do.

.DESCRIPTION
  Five checks, in this order, each by the script that owns it; none of them is changed or copied here:

    1. git fetch origin, then how many commits main lacks of origin/main (and the branch in hand, when it is another);
       and how GitHub's build of origin/main's newest commit ended (gh), since a push that git took can still fail there;
       and every worktree's branch that holds commits main lacks (finished work left beside main, 2026-11-23).
    2. tools\sync-repos.ps1 -Status: the management lists and the reports clone, fetched and compared, nothing
       committed, pulled or pushed.
    3. tools\reports-check.ps1: the reports on GitHub against the items of the priorities list (CLAUDE.md rule 9).
    4. tools\upstream.ps1: the outside programs' versions against the ones the checks last passed with (rule 12).

  It commits nothing and pushes nothing, here or beside; what it changes is what a fetch changes, the remote-tracking
  branches of this repository and of the two beside it. Run twice, it says the same twice. Without the network it says
  so in one line and goes on with what was fetched last; a folder that is not beside the repository is said and passed
  over. Under a line that asks for something, the owning script's own lines follow, indented, with the folder above
  the repository written as "..": no path of the computer is printed.

  Exit code 0 when nothing is asked, 1 when a line says what to do. The script stays ASCII (Windows PowerShell 5.1
  reads it as ANSI).

.EXAMPLE
  .\tools\session-start.ps1
#>
[CmdletBinding()]
param()

# git and the scripts below write to stderr without failing; their exit codes are the check.
$ErrorActionPreference = 'Continue'

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$above = Split-Path -Parent $root
$engine = (Get-Process -Id $PID).Path
$script:asked = $false

# One line of another script as this one prints it: the folder above the repository as "..", the profile as "~".
function Format-Line([string]$text) {
    $shown = $text.Replace($above, '..')
    if ($env:USERPROFILE) { $shown = $shown.Replace($env:USERPROFILE, '~') }
    return $shown.Trim()
}

# Runs one of the session's scripts in a process of its own, so that its exit, its error preference and its output
# stay its own. Gives its exit code and its lines; -1 when it could not be started.
function Invoke-Check([string]$name, [string[]]$options) {
    $path = Join-Path $here $name
    if (-not (Test-Path -LiteralPath $path)) {
        return [pscustomobject]@{ Code = -1; Lines = @("tools\$name is missing") }
    }
    $words = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $path) + @($options | Where-Object { $_ })
    # Outside call, program, powershell: runs a script of this repository's tools folder and reads its lines.
    $lines = @(& $engine @words 2>&1 | ForEach-Object { "$_" } | Where-Object { $_.Trim() })
    return [pscustomobject]@{ Code = $LASTEXITCODE; Lines = $lines }
}

function Write-Asked([string]$line, [string[]]$details) {
    $script:asked = $true
    Write-Host $line
    foreach ($detail in @($details | Where-Object { $_ })) { Write-Host "  $(Format-Line $detail)" }
}

function Get-Plural([int]$count, [string]$one, [string]$many) {
    if ($count -eq 1) { return "1 $one" }
    return "$count $many"
}

# ---- 1. the code: fetch, then main against origin/main ----

$branch = "$(& git -C $root rev-parse --abbrev-ref HEAD 2>$null)".Trim()
# Outside call, program, git: fetches origin, the one network contact of this check.
& git -C $root fetch -q origin 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'fetch: origin could not be reached (the network is away, or git is not signed in): what follows compares with origin/main as it was fetched last.'
}

& git -C $root rev-parse --verify -q main 2>&1 | Out-Null
$hasMain = $LASTEXITCODE -eq 0
& git -C $root rev-parse --verify -q origin/main 2>&1 | Out-Null
$hasOrigin = $LASTEXITCODE -eq 0
if (-not $hasMain -or -not $hasOrigin) {
    Write-Host 'code: this clone has no main or no origin/main to compare: not checked.'
} else {
    $behind = [int]"$(& git -C $root rev-list --count main..origin/main 2>$null)".Trim()
    if ($behind -gt 0) {
        $how = if ($branch -eq 'main') { 'git pull --ff-only' } else { 'git fetch origin main:main' }
        Write-Asked "code: main is $(Get-Plural $behind 'commit' 'commits') behind origin/main: $how" @()
    } else {
        Write-Host 'code: main has everything of origin/main: ok'
    }
    if ($branch -and $branch -ne 'main' -and $branch -ne 'HEAD') {
        $lacking = [int]"$(& git -C $root rev-list --count HEAD..origin/main 2>$null)".Trim()
        if ($lacking -gt 0) {
            Write-Asked "code: the branch $branch lacks $(Get-Plural $lacking 'commit' 'commits') of origin/main: merge or rebase before new work" @()
        } else {
            Write-Host "code: the branch $branch has everything of origin/main: ok"
        }
    }
}

# ---- 1a. the worktrees: work finished on a branch beside and never brought into main ----
# On 2026-11-23 a night's work landed on two branches in worktrees beside this one, and no one merged them: the next
# day the maintainer believed it undone, and one of its items was built a second time. So every worktree's branch
# that holds commits main lacks is named here, until it is merged or removed.

if ($hasMain) {
    $worktreeBranches = @(& git -C $root worktree list --porcelain 2>$null | Where-Object { $_ -like 'branch refs/heads/*' } | ForEach-Object { $_.Substring('branch refs/heads/'.Length) } | Where-Object { $_ -ne 'main' })
    $unmerged = New-Object System.Collections.Generic.List[string]
    foreach ($other in $worktreeBranches) {
        $ahead = [int]"$(& git -C $root rev-list --count main..$other 2>$null)".Trim()
        if ($ahead -gt 0) { $unmerged.Add("$other has $(Get-Plural $ahead 'commit' 'commits') main lacks") }
    }
    if ($unmerged.Count -gt 0) {
        Write-Asked "worktrees: work beside main is not merged: merge it into main once its checks pass, or remove the worktree and its branch" $unmerged
    } else {
        Write-Host "worktrees: $(Get-Plural $worktreeBranches.Count 'branch beside main' 'branches beside main'), nothing unmerged: ok"
    }
}

# ---- 1b. the build: what GitHub made of origin/main's newest commit ----
# A push is not done when git takes it: the build on GitHub runs the page checks under another PowerShell and the
# tests on another computer. On 2026-11-17 every build of an afternoon failed at one page check while the pushes
# went on, since nothing looked. So the session looks.

if ($hasOrigin) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        Write-Host 'build: gh is not on this computer: GitHub''s build of origin/main is not asked.'
    } else {
        $tip = "$(& git -C $root rev-parse origin/main 2>$null)".Trim()
        # Outside call, program, gh: asks GitHub how the build of origin/main's newest commit ended.
        $answered = "$(& gh run list --workflow build.yml --commit $tip --limit 1 --json status,conclusion 2>$null)".Trim()
        $built = $null
        if ($LASTEXITCODE -eq 0 -and $answered) {
            try { $built = @($answered | ConvertFrom-Json)[0] } catch { $built = $null }
        }
        if (-not $built -or -not "$($built.status)") {
            Write-Host 'build: GitHub has no build of origin/main''s newest commit, or could not be asked: not checked.'
        } elseif ("$($built.status)" -ne 'completed') {
            Write-Host "build: GitHub's build of origin/main is $($built.status): ok"
        } elseif ("$($built.conclusion)" -eq 'success') {
            Write-Host 'build: GitHub''s build of origin/main passed: ok'
        } else {
            Write-Asked "build: GitHub's build of origin/main ended with $($built.conclusion): read it with gh run view --log-failed and repair it before anything else is pushed" @()
        }
    }
}

# ---- 2. the repositories beside this one, as they stand ----

$sync = Invoke-Check 'sync-repos.ps1' @('-Status')
$named = 0
foreach ($line in $sync.Lines) {
    if ($line -notmatch '^(axkit-[a-z]+): (.*)$') { continue }
    $named++
    $name = $Matches[1]
    $said = $Matches[2].TrimEnd('.')
    if ($said -eq 'up to date') {
        Write-Host "${name}: ok"
    } elseif ($said -match 'not beside this repository') {
        Write-Host "${name}: not beside this repository: passed over."
    } elseif ($said -match 'could not fetch') {
        Write-Host "${name}: origin could not be reached: not checked."
    } elseif ($said -match 'to pull|to push|changed') {
        Write-Asked "${name}: $(Format-Line $said): run tools\sync-repos.ps1" @()
    } else {
        Write-Asked "${name}: $(Format-Line $said): look in that folder" @()
    }
}
if ($named -eq 0) {
    Write-Asked 'lists: tools\sync-repos.ps1 -Status said nothing of the repositories beside this one: run it by itself' $sync.Lines
}

# ---- 3. the reports against the priorities list ----

$reports = Invoke-Check 'reports-check.ps1' @()
$text = $reports.Lines -join ' '
if ($reports.Code -eq 0) {
    $counts = if ($text -match '\(([^)]*)\)') { " ($($Matches[1]))" } else { '' }
    Write-Host "reports: the reports and the list agree${counts}: ok"
} elseif ($reports.Code -eq 1) {
    $new = @($reports.Lines | Where-Object { $_ -match '^Reports: new:' }).Count
    $close = @($reports.Lines | Where-Object { $_ -match '^Reports: close it:' }).Count
    $open = @($reports.Lines | Where-Object { $_ -match '^Reports: (item|entry) open:' }).Count
    $parts = @()
    if ($new -gt 0) { $parts += "$(Get-Plural $new 'open report' 'open reports') without an entry: read it and write its entry in the outside reports list (tools\outside-report.ps1)" }
    if ($close -gt 0) { $parts += "$(Get-Plural $close 'open report' 'open reports') whose item or entry is finished: answer it and close it" }
    if ($open -gt 0) { $parts += "$(Get-Plural $open 'closed report' 'closed reports') whose item or entry is not finished: finish it" }
    if ($parts.Count -eq 0) { $parts += 'tools\reports-check.ps1 names a disagreement: act on each line' }
    Write-Asked "reports: $($parts -join '; ')" $reports.Lines
} elseif ($text -match 'priorities list is not at') {
    Write-Host 'reports: the priorities list is not beside this repository: not checked.'
} elseif ($text -match 'GitHub could not be asked') {
    Write-Host 'reports: GitHub could not be asked (gh missing, signed out, or no network): not checked.'
} else {
    Write-Asked 'reports: tools\reports-check.ps1 did not run to its end: run it by itself' @()
}

# ---- 3b. the outside reports: nothing from outside worked on without the maintainer's signed approval ----

$outside = Invoke-Check 'outside-check.ps1' @()
if ($outside.Code -eq 0) {
    $said = @($outside.Lines | Where-Object { $_ -match '^Outside reports: ' } | Select-Object -Last 1)
    $detail = if ($said.Count -eq 1) { $said[0] -replace '^Outside reports: ', '' -replace '\.$', '' } else { 'checked' }
    Write-Host "outside reports: ${detail}: ok"
} else {
    Write-Asked 'outside reports: an item from outside has no valid approval of yours: nothing is done on it; move it to the outside reports list or ask for the approval' $outside.Lines
}

# ---- 4. the outside programs ----

$upstream = Invoke-Check 'upstream.ps1' @()
$changed = @($upstream.Lines | Where-Object { $_ -match '^Changed: ' } | Select-Object -First 1)
if ($upstream.Code -eq 0) {
    Write-Host 'outside programs: none changed: ok'
} elseif ($upstream.Code -eq 1 -and $changed.Count -eq 1) {
    $names = ($changed[0] -replace '^Changed: ', '') -replace '\. Run .*$', ''
    Write-Asked "outside programs: changed: ${names}: run tools\upstream.ps1 -Verify before other work" @($upstream.Lines | Where-Object { $_ -match ': changed$' })
} else {
    Write-Asked 'outside programs: tools\upstream.ps1 did not run to its end: run it by itself' @($upstream.Lines | Select-Object -Last 3)
}

if ($script:asked) { exit 1 }
exit 0
