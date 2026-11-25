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

function Get-BesideFolder([string]$Name) {
    $common = (& git -C $PSScriptRoot rev-parse --path-format=absolute --git-common-dir 2>$null | Select-Object -First 1)
    $checkout = if ($common) { Split-Path $common -Parent } else { Split-Path $PSScriptRoot -Parent }
    return (Join-Path (Split-Path $checkout -Parent) $Name)
}
