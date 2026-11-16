<#
.SYNOPSIS
  The functions that run an app's automated window checks with the record on, for the two scripts that run them
  without anyone naming the folders: tools\window-passes.ps1 -Run (the apps whose windows changed) and
  tools\nightly.ps1 (every app).

.DESCRIPTION
  Dot-source it. The functions, each given the repository's root:

    Get-WindowScripts <root> <app>          the scripts of tests\scripts\<app>: Run, the ones a run plays, and Live,
                                            the ones it leaves out
    Build-WindowRun <root> <scratch>        builds AxKit once into the scratch folder, never into src\AxKit\bin, and
                                            the runner tools\AxAutomate; the built AxKit.exe, or nothing when a build
                                            failed (its output is then printed)
    Invoke-WindowScripts <root> <exe> <app> runs the app's scripts on the hidden desktop with --record and returns
                                            App, Ran, LeftOut, Failed (the scripts' names) and Line, the one line to
                                            print

  A live script is one whose "# Needs:" line, among the comment lines at its head, names Claude Code signed in, the
  network or GitHub: it would start the real Claude Code or ask a server, so it is run only by a person or the
  assistant who means to (docs/axKit/automation.md). The scripts whose line says "no Claude Code, no network" play a
  stand-in and are run. An app with a live script is run script by script, the others by their folder in one run.
#>

# What a "# Needs:" line says of a script that is never run unattended.
$script:LiveNeed = 'Claude Code signed in|the network|GitHub'

function Get-WindowScripts([string]$Root, [string]$AppName) {
    $folder = Join-Path $Root "tests\scripts\$AppName"
    $run = @()
    $live = @()
    if (Test-Path -LiteralPath $folder) {
        foreach ($file in @(Get-ChildItem -LiteralPath $folder -Filter '*.txt' -File | Sort-Object Name)) {
            $isLive = $false
            foreach ($line in [IO.File]::ReadAllLines($file.FullName)) {
                # The head of a script is its comment lines; the first step ends it.
                if ($line -notmatch '^#') { break }
                if ($line -match '^# Needs:' -and $line -match $script:LiveNeed) { $isLive = $true }
            }
            if ($isLive) { $live += $file } else { $run += $file }
        }
    }
    return [pscustomobject]@{ Folder = $folder; Run = $run; Live = $live }
}

function Build-WindowRun([string]$Root, [string]$Scratch) {
    # A failing build writes to the error stream, which Windows PowerShell would turn into an exception under Stop;
    # the exit code is the check.
    $before = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = @(& dotnet build (Join-Path $Root 'src\AxKit\AxKit.csproj') -c Release -o $Scratch --nologo -v q 2>&1 | ForEach-Object { "$_" })
        $ok = $LASTEXITCODE -eq 0
        if ($ok) {
            $output = @(& dotnet build (Join-Path $Root 'tools\AxAutomate') -c Release --nologo -v q 2>&1 | ForEach-Object { "$_" })
            $ok = $LASTEXITCODE -eq 0
        }
        # The development computer has 7 GB: the build servers end with the builds.
        & dotnet build-server shutdown | Out-Null
    } finally {
        $ErrorActionPreference = $before
    }

    if (-not $ok) {
        $output | ForEach-Object { Write-Host $_ }
        return $null
    }
    return (Join-Path $Scratch 'AxKit.exe')
}

function Invoke-WindowScripts([string]$Root, [string]$Exe, [string]$AppName) {
    $scripts = Get-WindowScripts $Root $AppName
    $leftOut = @($scripts.Live).Count
    $said = if ($leftOut -gt 0) { " $leftOut live script(s) left out." } else { '' }
    if (@($scripts.Run).Count -eq 0) {
        $why = if ($leftOut -gt 0) { 'every script of it is live' } else { 'it has no scripts' }
        return [pscustomobject]@{ App = $AppName; Ran = 0; LeftOut = $leftOut; Failed = @(); Line = "${AppName}: nothing run, $why.$said" }
    }

    # The folder in one run; with a live script in it, the others one by one, so that no live one is ever started.
    $targets = if ($leftOut -eq 0) { @($scripts.Folder) } else { @($scripts.Run | ForEach-Object { $_.FullName }) }
    $failed = @()
    $inventoryStale = $false
    $before = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    Push-Location -LiteralPath $Root
    try {
        foreach ($target in $targets) {
            $output = @(& dotnet run --no-build --project 'tools\AxAutomate' -c Release -- $target --exe $Exe --record 2>&1 | ForEach-Object { "$_" })
            $code = $LASTEXITCODE
            if ($code -eq 3) { $inventoryStale = $true }
            if ($code -eq 0 -or $code -eq 3) { continue }
            if ((Test-Path -LiteralPath $target -PathType Leaf)) {
                $failed += [IO.Path]::GetFileName($target)
                continue
            }
            # A folder's run names its failing scripts in one line: "FAILED: 2 of 9 script(s): a.txt, b.txt".
            $line = @($output | Where-Object { $_ -match '^FAILED: \d+ of \d+ script\(s\): (.+)$' }) | Select-Object -Last 1
            if ($line -and $line -match '^FAILED: \d+ of \d+ script\(s\): (.+)$') { $failed += @($Matches[1] -split ', ') }
            else { $failed += "the run could not start (exit code $code)" }
        }
    } finally {
        Pop-Location
        $ErrorActionPreference = $before
    }

    $ran = @($scripts.Run).Count
    $line = if ($failed.Count -eq 0) { "${AppName}: passed, $ran script(s).$said" } else { "${AppName}: FAILED: $($failed -join ', ').$said" }
    if ($inventoryStale) { $line += ' The command inventory is stale: a run with --inventory write, then tools\workflows.ps1.' }
    return [pscustomobject]@{ App = $AppName; Ran = $ran; LeftOut = $leftOut; Failed = $failed; Line = $line }
}
