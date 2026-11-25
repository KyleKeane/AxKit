<#
.SYNOPSIS
  Writes every generated page again, in one run; or, with -Check, runs every quick check of the repository at once:
  the pages, the documents' menu paths, the record of the outside programs and the scripts' rules.

.DESCRIPTION
  A change to a script, a command, a key or a document often makes several pages stale at once (the workflow pages,
  the apps page, the families page, a guide's Menus section). Each page's own script writes it; this runs them all, so
  that one command brings every page up to date before a commit. Each script prints its own line. Exit code 0 when
  every script ran, 1 when one failed.

  -Check writes nothing. It is the one list of the quick checks (docs/axKit/repairs.json R-81): test.ps1 runs it
  before the tests and tools\push-check.ps1 before its tests, so that every stale page and every broken rule is named
  in one run of seconds, not one at a time after minutes of tests. Each failure keeps its own lines, and the last line
  says what to do for each kind: a stale page is written again by this script without -Check; a menu path a document
  names is corrected in the document; the record of the outside programs and the scripts are corrected as their lines
  say. Exit code 0 when every check passed, 1 otherwise.

  The script stays ASCII (Windows PowerShell 5.1 reads it as ANSI).

.EXAMPLE
  .\tools\pages.ps1          write every page
  .\tools\pages.ps1 -Check   every quick check, every failure named
#>
[CmdletBinding()]
param([switch]$Check)

$ErrorActionPreference = 'Continue'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

# The page scripts, each of which writes its page, or with -Check says whether it is current. workflows.ps1 writes the
# key map and the families page as well; their own scripts are kept in the list, since each also checks alone.
$pages = 'outside-calls', 'workflows', 'repairs', 'apps-page', 'cases', 'key-map', 'families', 'owed', 'components', 'guide-menus'

if (-not $Check) {
    $failed = $false
    foreach ($page in $pages) {
        & (Join-Path $here "$page.ps1")
        if ($LASTEXITCODE -ne 0) {
            Write-Host "tools\$page.ps1 failed."
            $failed = $true
        }
    }

    # doc-menus only checks: the documents' menu paths are written by hand and checked against the windows.
    & (Join-Path $here 'doc-menus.ps1') -Check
    if ($LASTEXITCODE -ne 0) { $failed = $true }

    if ($failed) { exit 1 }
    Write-Host 'Every page is written.'
    exit 0
}

$stalePages = New-Object System.Collections.Generic.List[string]
foreach ($page in $pages) {
    & (Join-Path $here "$page.ps1") -Check
    if ($LASTEXITCODE -ne 0) { $stalePages.Add("tools\$page.ps1") }
}

$other = New-Object System.Collections.Generic.List[string]
# doc-menus: the menu paths the documents name; upstream: the record of the outside programs' verified versions, only
# that it is well formed (the installed versions are compared by tools\upstream.ps1 alone, never here, since CI's
# computer has other versions); script-lint: the scripts' rules, each a gotcha of the root CLAUDE.md until 2026-11-16.
$fixes = @{
    'doc-menus' = 'correct the menu paths in the documents its lines name'
    'upstream' = 'correct docs\axKit\upstream.json as its lines say'
    'script-lint' = 'correct the scripts as its lines say'
}
foreach ($name in 'doc-menus', 'upstream', 'script-lint') {
    & (Join-Path $here "$name.ps1") -Check
    if ($LASTEXITCODE -ne 0) { $other.Add("tools\$name.ps1 -Check failed: $($fixes[$name])") }
}

if ($stalePages.Count -eq 0 -and $other.Count -eq 0) {
    Write-Host 'Every quick check passed: the pages, the menu paths, the record of the outside programs, the scripts.'
    exit 0
}

if ($stalePages.Count -gt 0) {
    Write-Host "Stale: $($stalePages -join ', '). Run tools\pages.ps1 (without -Check), which writes every page, and commit what it writes."
}
foreach ($line in $other) { Write-Host $line }
exit 1
