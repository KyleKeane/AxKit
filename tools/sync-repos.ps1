<#
.SYNOPSIS
  Syncs the two repositories that sit beside this one on the maintainer's computer: axkit-reports, the clone of the
  public reports repository (the issue forms, the workflow, the blocklist), and axkit-management, the private axTask
  lists (the issues read from GitHub and the maintainer's priorities). Each is fetched, pulled fast-forward and
  pushed; the management lists are committed first, as one dated commit, since axTask writes them. git runs this on
  every push of the code repository through the pre-push hook (tools/hooks/pre-push, installed once per clone with
  "git config core.hooksPath tools/hooks"), so that a push of the code never leaves the lists or the reports clone
  behind, and release.ps1 refuses to run until that hook is installed. A folder that is not there is skipped with one
  line, so a clone of the code alone works as before.

.DESCRIPTION
  The folders are found by name beside this repository, <parent>\axkit-reports and <parent>\axkit-management
  (docs/axkit/development-environment.md section 4). For each one that exists:
    1. git fetch origin;
    2. axkit-management only: every change is committed as "Lists as of <date> <time>". axTask's files change on
       every refresh from GitHub, and that churn is a backup's job. axkit-reports is never committed here: a change to
       its forms, workflow or blocklist gets a message of its own by hand, and uncommitted changes are only named;
    3. git pull --ff-only; a history that has diverged, or local changes in the way, are named and left for a hand
       merge in that folder. Nothing is ever forced;
    4. git push when the branch is ahead of origin.
  One line per repository says what happened. The exit code is 1 when a repository could not be synced and 0
  otherwise; the hook ignores it, so a sibling's trouble is printed and never stops a push of the code.

.EXAMPLE
  .\tools\sync-repos.ps1            sync both
  .\tools\sync-repos.ps1 -Status    fetch and say where each stands; commit, pull and push nothing
#>
[CmdletBinding()]
param([switch]$Status)

# Native commands write progress and refusals to stderr; under Stop, Windows PowerShell would turn the first such line
# into an exception. Here every git call is followed by a look at its exit code instead.
$ErrorActionPreference = 'Continue'

$parent = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$script:failed = $false

function Invoke-Git {
    # Runs git in the folder, quietly, and returns $true when it succeeded; the output is kept in $script:gitOutput.
    param([string]$Path, [string[]]$Arguments)
    $script:gitOutput = @(& git -C $Path @Arguments 2>&1 | ForEach-Object { "$_" })
    return ($LASTEXITCODE -eq 0)
}

function Sync-Sibling {
    param([string]$Name, [bool]$CommitLists)

    $path = Join-Path $parent $Name
    if (-not (Test-Path -LiteralPath (Join-Path $path '.git'))) {
        Write-Host "${Name}: not beside this repository, skipped."
        return
    }

    $said = @()
    if (-not (Invoke-Git $path @('fetch', '-q', 'origin'))) {
        Write-Host "${Name}: could not fetch origin ($($script:gitOutput -join ' ')). Nothing was changed."
        $script:failed = $true
        return
    }

    [void](Invoke-Git $path @('rev-parse', '--abbrev-ref', 'HEAD'))
    $branch = ($script:gitOutput -join '').Trim()
    [void](Invoke-Git $path @('status', '--porcelain'))
    $changed = @($script:gitOutput | Where-Object { $_ })

    if ($changed.Count -gt 0) {
        if ($CommitLists -and -not $Status) {
            $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm'
            $ok = (Invoke-Git $path @('add', '-A')) -and (Invoke-Git $path @('commit', '-q', '-m', "Lists as of $stamp"))
            if (-not $ok) {
                Write-Host "${Name}: could not commit the changed lists ($($script:gitOutput -join ' '))."
                $script:failed = $true
                return
            }
            $said += "$($changed.Count) file(s) committed as 'Lists as of $stamp'"
            $changed = @()
        } elseif ($CommitLists) {
            $said += "$($changed.Count) file(s) changed since the last backup"
        } else {
            $said += "$($changed.Count) file(s) changed and not committed (a change here gets its own message by hand)"
        }
    }

    # Behind and ahead of origin's branch; a branch origin does not have yet counts as ahead.
    $behind = 0
    $ahead = 1
    if (Invoke-Git $path @('rev-parse', '--verify', '-q', "origin/$branch")) {
        [void](Invoke-Git $path @('rev-list', '--count', "HEAD..origin/$branch"))
        $behind = [int]($script:gitOutput -join '').Trim()
        [void](Invoke-Git $path @('rev-list', '--count', "origin/$branch..HEAD"))
        $ahead = [int]($script:gitOutput -join '').Trim()
    }

    if ($Status) {
        if ($behind -gt 0) { $said += "$behind commit(s) to pull" }
        if ($ahead -gt 0) { $said += "$ahead commit(s) to push" }
        if ($said.Count -eq 0) { $said += 'up to date' }
        Write-Host "${Name}: $($said -join '; ')."
        return
    }

    if ($behind -gt 0) {
        if (Invoke-Git $path @('pull', '-q', '--ff-only', 'origin', $branch)) {
            $said += "pulled $behind commit(s)"
        } else {
            Write-Host "${Name}: origin has $behind commit(s) this computer has not, and they could not be brought in by a fast-forward (the histories have diverged, or changed files stand in the way). Merge by hand in $path. Not pushed."
            $script:failed = $true
            return
        }
    }

    if ($ahead -gt 0) {
        if (Invoke-Git $path @('push', '-q', '-u', 'origin', $branch)) {
            $said += "pushed $ahead commit(s)"
        } else {
            Write-Host "${Name}: could not push ($($script:gitOutput -join ' '))."
            $script:failed = $true
            return
        }
    }

    if ($said.Count -eq 0) { $said += 'up to date' }
    Write-Host "${Name}: $($said -join '; ')."
}

Sync-Sibling -Name 'axkit-management' -CommitLists $true
Sync-Sibling -Name 'axkit-reports' -CommitLists $false

if ($script:failed) { exit 1 }
exit 0
