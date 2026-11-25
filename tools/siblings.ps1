<#
.SYNOPSIS
  Where a repository beside this one is (axkit-management, axkit-reports, axkit-keys): one way, for every script.

.DESCRIPTION
  Dot-source it. Get-BesideFolder <name> gives the path of the folder of that name beside the main checkout. From a
  worktree (a branch's own folder anywhere, the scratchpad's included) git's common directory names the main
  checkout, so the lists are found there too; without git's answer, the folder above this script's repository is
  used. Until 2026-11-24 eight scripts found the folders their own way, and from a worktree that is not beside the
  checkout some found nothing and passed in silence (docs/axKit/repairs.json R-83). The script stays ASCII.
#>

# Whether the git hooks are installed in the clone at $Root (root CLAUDE.md rules 7 and 9): core.hooksPath names its
# tools\hooks, as tools/hooks or as the full path; in a worktree the setting is the checkout's, with the full path of
# the checkout's hooks, and those are the hooks that run there, so that passes too. test.ps1 and release.ps1 both ask
# it; until 2026-11-24 each had a copy, and release.ps1's refused a worktree (docs/axKit/repairs.json R-84).
function Test-HooksInstalled([string]$Root) {
    $hooks = "$(git -C $Root config --get core.hooksPath)".Trim()
    if (-not $hooks) { return $false }
    $folder = if ([IO.Path]::IsPathRooted($hooks)) { $hooks } else { Join-Path $Root $hooks }
    $folder = [IO.Path]::GetFullPath($folder).TrimEnd('\')
    $installed = @([IO.Path]::GetFullPath((Join-Path $Root 'tools\hooks')).TrimEnd('\'))
    $common = "$(git -C $Root rev-parse --path-format=absolute --git-common-dir)".Trim()
    if ($common) {
        $installed += [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $common) 'tools\hooks')).TrimEnd('\')
    }
    return ($installed -icontains $folder)
}

function Get-BesideFolder([string]$Name) {
    $common = (& git -C $PSScriptRoot rev-parse --path-format=absolute --git-common-dir 2>$null | Select-Object -First 1)
    $checkout = if ($common) { Split-Path $common -Parent } else { Split-Path $PSScriptRoot -Parent }
    return (Join-Path (Split-Path $checkout -Parent) $Name)
}
