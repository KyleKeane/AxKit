<#
.SYNOPSIS
  Runs every app's automated window checks on the hidden desktop, with the record on, and writes a short dated report:
  a script the maintainer may schedule for the night. It commits and pushes nothing.

.DESCRIPTION
  What tools\window-passes.ps1 -Run does for the apps whose windows changed, this does for every app of
  Dispatch.Apps, through the same functions (tools\window-run.ps1): AxKit built once into a scratch folder (never
  into src\AxKit\bin), each app's scripts folder run with --record, and no script whose "# Needs:" line names Claude
  Code signed in, the network or GitHub: the real Claude Code is never started, and each app's line says how many
  live scripts were left out.

  The report is %LOCALAPPDATA%\AxKit\logs\nightly.txt, written anew by every run: the date and the commit, one line
  per app (passed, or the failing scripts), and what became of the record. It holds apps' and scripts' names and no
  path of the computer.

  The record, docs\axKit\workflow-record.json, is left changed in the working tree only when the tree was clean
  before the run and every script passed: then the night's passes wait there to be committed. Otherwise the file is
  put back exactly as it was before the run (that one file and nothing else), so that a working session never finds
  changes it did not make: over uncommitted work a recorded pass counts as changed again anyway (the + of
  tools\window-passes.ps1), and a failing night is the report's to tell.

  Exit code 0 when every script run passed, 1 when one failed, 2 when the build failed.

  To schedule it for three o'clock every night, as the signed-in user (the run needs the user's session for its
  desktop; nothing is shown on the user's own), one line, with the repository's folder in place of <repository>:

  schtasks /Create /TN "axKit nightly window checks" /SC DAILY /ST 03:00 /TR "powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"<repository>\tools\nightly.ps1\""

  and to take it away: schtasks /Delete /TN "axKit nightly window checks". The script makes no scheduled task itself.

.EXAMPLE
  .\tools\nightly.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
. (Join-Path $root 'tools\apps.ps1')
. (Join-Path $root 'tools\window-run.ps1')

$recordPath = Join-Path $root 'docs\axKit\workflow-record.json'
$recordBefore = [IO.File]::ReadAllBytes($recordPath)
$cleanBefore = @(git -C $root status --porcelain).Count -eq 0
$commit = "$(git -C $root rev-parse --short HEAD)".Trim()

$report = New-Object System.Collections.Generic.List[string]
$report.Add("axKit nightly window checks, $((Get-Date).ToString('yyyy-MM-dd HH:mm')), commit $commit$(if (-not $cleanBefore) { ', with changes not committed' })")

$failures = 0
$built = $true
$scratch = Join-Path ([IO.Path]::GetTempPath()) "axkit-window-run\$PID"
try {
    $exe = Build-WindowRun $root $scratch
    if (-not $exe) {
        $built = $false
        $report.Add('Nothing run: the build failed.')
    } else {
        foreach ($app in @(Get-AxKitApps $root)) {
            $result = Invoke-WindowScripts $root $exe $app.Name
            $report.Add($result.Line)
            $failures += @($result.Failed).Count
        }
    }
} finally {
    Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue
}

# Whether two byte arrays hold the same bytes.
function Test-SameBytes([byte[]]$one, [byte[]]$other) {
    return [Convert]::ToBase64String($one) -ceq [Convert]::ToBase64String($other)
}

$recordChanged = -not (Test-SameBytes $recordBefore ([IO.File]::ReadAllBytes($recordPath)))
if ($built -and $failures -eq 0 -and $cleanBefore) {
    $report.Add('Every script passed.' + $(if ($recordChanged) { ' The passes are in docs/axKit/workflow-record.json, in the working tree: commit it.' } else { ' The record did not change.' }))
} else {
    if ($recordChanged) { [IO.File]::WriteAllBytes($recordPath, $recordBefore) }
    $why = if (-not $built) { 'the build failed' } elseif ($failures -gt 0) { "$failures script(s) failed" } else { 'the working tree held changes not committed before the run' }
    $report.Add("The record docs/axKit/workflow-record.json is as it was before the run, since $why.")
}

$logs = Join-Path $env:LOCALAPPDATA 'AxKit\logs'
New-Item -ItemType Directory -Force -Path $logs | Out-Null
[IO.File]::WriteAllText((Join-Path $logs 'nightly.txt'), (($report -join "`r`n") + "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
$report | ForEach-Object { Write-Host $_ }

if (-not $built) { exit 2 }
if ($failures -gt 0) { exit 1 }
exit 0
