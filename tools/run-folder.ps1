<#
.SYNOPSIS
  A run's own folder under the temporary folder, for the scripts that build or copy into one: named by the checkout
  and by the process, so that two sessions in one checkout never meet in it.

.DESCRIPTION
  Dot-source it. Get-RunFolder <root> <prefix> gives the path "<temp>\<prefix>-<the checkout's folder name>-<process
  id>"; it makes nothing. The script that asks removes the folder when it is done. Before it answers, the function
  removes the folders of the same prefix and checkout whose process has ended: what a run that was stopped left
  behind. A script that keeps git worktrees there runs "git worktree prune" after it, which forgets the ones removed.

  Until 2026-11-27 these folders were named by the checkout alone: two sessions working in one checkout (the
  maintainer's and an agent's, or two agents) built into the same folder, and the second build failed on the files
  the first one's program held open, or a clean copy was taken away under the checks running in it. The scripts:
  tools\workflows.ps1 and tools\graph-check.ps1 (the runner of the automated checks; tools\pages.ps1 runs both in one
  process, so they share one folder in a run), tools\push-check.ps1, tools\check-new-app.ps1 and tools\upstream.ps1.
  A folder that is kept between runs on purpose is not made here: tools\run-scripts.ps1 names its runner by the
  commit and the state of its sources, so that an unchanged runner is not built twice.

  The script stays ASCII (Windows PowerShell 5.1 reads it as ANSI).
#>

function Get-RunFolder([string]$Root, [string]$Prefix) {
    $temp = [IO.Path]::GetTempPath()
    $start = $Prefix + '-' + (Split-Path -Leaf $Root) + '-'
    foreach ($left in @(Get-ChildItem -LiteralPath $temp -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name.StartsWith($start, [StringComparison]::OrdinalIgnoreCase) })) {
        $owner = $left.Name.Substring($start.Length)
        if ($owner -match '^\d+$' -and [int]$owner -ne $PID -and -not (Get-Process -Id ([int]$owner) -ErrorAction SilentlyContinue)) {
            Remove-Item -LiteralPath $left.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    return (Join-Path $temp ($start + $PID))
}
