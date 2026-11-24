<#
.SYNOPSIS
  Writes every generated page again, in one run: the same scripts test.ps1 and tools\push-check.ps1 check.

.DESCRIPTION
  A change to a script, a command, a key or a document often makes several pages stale at once (the workflow pages,
  the apps page, the families page, a guide's Menus section). Each page's own script writes it; this runs them all, in
  the order test.ps1 checks them, so that one command brings every page up to date before a commit. Each script prints
  its own line. Exit code 0 when every script ran, 1 when one failed. The script stays ASCII (Windows PowerShell 5.1
  reads it as ANSI).

.EXAMPLE
  .\tools\pages.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$failed = $false
# doc-menus only checks (the documents' menu paths are written by hand and checked against the windows).
foreach ($page in 'outside-calls', 'workflows', 'repairs', 'apps-page', 'cases', 'key-map', 'families', 'owed', 'components', 'guide-menus') {
    & (Join-Path $here "$page.ps1")
    if ($LASTEXITCODE -ne 0) {
        Write-Host "tools\$page.ps1 failed."
        $failed = $true
    }
}

& (Join-Path $here 'doc-menus.ps1') -Check
if ($LASTEXITCODE -ne 0) { $failed = $true }

if ($failed) { exit 1 }
Write-Host 'Every page is written.'
exit 0
