<#
.SYNOPSIS
  Builds axKit and starts one of its apps (the EXAMPLE lines below): axClaude on a project folder by default.

.EXAMPLE
  .\run.ps1                                  # axClaude on the current folder
  .\run.ps1 C:\src\myproject                 # axClaude on that folder
  .\run.ps1 C:\src\myproject -- --resume     # extra arguments go to claude (default: --continue)
  .\run.ps1 C:\src\myproject -New            # a new conversation (PowerShell swallows a bare --)
  .\run.ps1 -App down C:\notes\todo.md       # axDown on that file (scripts to run-individual-apps\run-axDown.ps1 says the same)
  .\run.ps1 -App down                        # axDown with an empty document (scripts to run-individual-apps\run-axDown.ps1 opens the tutorial file instead)
  .\run.ps1 -App json C:\x\settings.json     # axJSON on that file (scripts to run-individual-apps\run-axJSON.ps1 says the same)
  .\run.ps1 -App git C:\src\myproject        # axGIT on the repository that holds the folder (scripts to run-individual-apps\run-axGIT.ps1 says the same)
  .\run.ps1 -App terminal C:\src\myproject   # axTerminal with a shell in that folder (scripts to run-individual-apps\run-axTerminal.ps1 says the same)
  .\run.ps1 -App media C:\Music\Dune         # axMedia on the folder's sound and video files (scripts to run-individual-apps\run-axMedia.ps1 says the same)
  .\run.ps1 -App preview C:\notes\README.md  # axPreview on the file, read-only (scripts to run-individual-apps\run-axPreview.ps1 says the same)
  .\run.ps1 -App start                       # axStart, the launcher: its field shown; it keeps running (scripts to run-individual-apps\run-axStart.ps1 says the same)
  .\run.ps1 -App process                     # axProcess (scripts to run-individual-apps\run-axProcess.ps1 says the same)
  .\run.ps1 -App task C:\my\tasks            # axTask on a folder of lists (scripts to run-individual-apps\run-axTask.ps1 says the same)
  .\run.ps1 -App pass C:\Vault\mine.kdbx     # axPass on a KeePass database (scripts to run-individual-apps\run-axPass.ps1 says the same)
  .\run.ps1 -App calc                        # axCalc, the calculator (scripts to run-individual-apps\run-axCalc.ps1 says the same)
  .\run.ps1 -App clip                        # axClip, the clipboard history (scripts to run-individual-apps\run-axClip.ps1 says the same)
  .\run.ps1 -NoBuild                         # skip the build, just start
  .\run.ps1 -Test                            # run the unit tests instead (test.ps1: every test project, in Release)

  The folder "scripts to run-individual-apps" holds one script per app (run-axClaude.ps1, run-axDown.ps1 and so on): each passes
  -App for you and takes the same other arguments.
  If PowerShell refuses to run scripts: powershell -ExecutionPolicy Bypass -File .\run.ps1
  Close a running axKit window first: the build writes into the folder it holds open. axStart and axClip, which run in
  the background, are ended before the build; they and axHelp are started hidden after it, whether they ran before or not.
#>
[CmdletBinding()]
param(
    # The app to start: the verb AxKit.exe takes (docs/axKit/SPEC.md AX-1), one of Dispatch.Apps' (src/AxKit.Core/Dispatch.cs,
    # read by tools/apps.ps1, so that a new app needs no line here). axClaude unless said otherwise.
    [string]$App = 'claude',

    # axClaude's project folder (default: the current folder), axDown's or axJSON's file (default: an empty document),
    # a folder or file inside axGIT's repository (default: the last one opened), axTerminal's folder, axFile's folder
    # or file (default: the last folder opened), axMedia's file or folder (default: an empty queue), or axPreview's
    # file (default: an empty window), axTask's folder or axPass's database (default: the last one). axStart, axProcess, axCalc and axClip take none.
    [Parameter(Position = 0)]
    [Alias('Project')]
    [string]$Path,

    [switch]$NoBuild,
    [switch]$Test,
    [switch]$New,

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ClaudeArgs
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# The apps, off Dispatch.Apps without a build. A verb is matched without case, as AxKit.exe matches it.
. (Join-Path $root 'tools\apps.ps1')
$apps = @(Get-AxKitApps $root)
$chosen = $apps | Where-Object { $_.Verb -eq $App } | Select-Object -First 1
if (-not $chosen) {
    Write-Host "There is no app '$App'. -App takes one of: $(($apps | ForEach-Object Verb) -join ', ')."
    exit 1
}
$App = $chosen.Verb

if ($Test) {
    # test.ps1: every test project one by one, in Release, as the workflows run them (the solution would build into
    # the bin\Debug a running axKit holds open).
    & (Join-Path $root 'test.ps1')
    exit $LASTEXITCODE
}

# The .NET version is Directory.Build.props' AxKitDotNet, the one place it is set.
$dotnet = ([xml](Get-Content (Join-Path $root 'Directory.Build.props') -Raw)).Project.PropertyGroup.AxKitDotNet
$exe = Join-Path $root "src\AxKit\bin\Debug\$dotnet-windows\AxKit.exe"

if (-not $NoBuild) {
    # The background apps started from this build, axStart, axClip and axHelp (and axDescribe before 5.7.0), keep running after every
    # window has closed, and hold the folder the build writes into: each is ended before the build and started again,
    # hidden, from the new one after, so that the hotkey runs the new code (asked 2026-10-23). axStart keeps no unsaved
    # state; axClip's history is in memory unless it is kept, and starts empty again; axHelp keeps
    # its settings in its file.
    $background = @(Get-CimInstance Win32_Process -Filter "Name = 'AxKit.exe'" |
        Where-Object { $_.ExecutablePath -eq $exe -and $_.CommandLine -match '\s(start|clip|help|describe)(\s|$)' } |
        ForEach-Object { [pscustomobject]@{ Id = $_.ProcessId; Verb = [regex]::Match($_.CommandLine, '\s(start|clip|help|describe)(\s|$)').Groups[1].Value } })
    foreach ($process in $background) {
        Write-Host "Ending axKit $($process.Verb) (process $($process.Id)), which holds the build folder; the background apps start again after the build."
        Stop-Process -Id $process.Id -Force
        Wait-Process -Id $process.Id -Timeout 10 -ErrorAction SilentlyContinue
    }

    # axHelp's conversation hides when it is closed and runs on (axClaude FR-9.6, 2026-11-15): an axClaude process no
    # window shows, with Claude Code under it and, when the AI tools are on, their broker, another AxKit.exe of this
    # folder. Ending axHelp by force above leaves it running, unseen, and the build then fails on files in use (the
    # user had to restart the computer, 2026-11-15). So it is ended here with everything under it; Claude Code keeps
    # the conversation itself, and axHelp starts a window on it again at the next press of its key.
    $hidden = @(Get-CimInstance Win32_Process -Filter "Name = 'AxKit.exe'" |
        Where-Object { $_.ExecutablePath -eq $exe -and $_.CommandLine -match '\s--hide-on-close(\s|$)' })
    foreach ($process in $hidden) {
        Write-Host "Ending axHelp's conversation (process $($process.ProcessId)), which runs on hidden and holds the build folder."
        & taskkill.exe /PID $process.ProcessId /T /F 2>&1 | Out-Null
        Wait-Process -Id $process.ProcessId -Timeout 10 -ErrorAction SilentlyContinue
    }

    # A broker of the AI tools (AxKit.exe mcp) whose Claude Code has ended holds the folder for nobody: ended too.
    $brokers = @(Get-CimInstance Win32_Process -Filter "Name = 'AxKit.exe'" |
        Where-Object { $_.ExecutablePath -eq $exe -and $_.CommandLine -match '\smcp(\s|$)' -and -not (Get-Process -Id $_.ParentProcessId -ErrorAction SilentlyContinue) })
    foreach ($process in $brokers) {
        Write-Host "Ending an AI tools broker left behind (process $($process.ProcessId))."
        Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
        Wait-Process -Id $process.ProcessId -Timeout 10 -ErrorAction SilentlyContinue
    }

    # What still runs from this folder is a window the user has open, or a broker of a Claude Code that runs: the
    # build cannot replace files they hold. Said in one plain line each, in place of the compiler's wall of errors.
    $holding = @(Get-CimInstance Win32_Process -Filter "Name = 'AxKit.exe'" | Where-Object { $_.ExecutablePath -eq $exe })
    if ($holding.Count -gt 0) {
        Write-Host "axKit still runs from the folder the build writes into. Close these, then run again:"
        foreach ($process in $holding) {
            $words = ($process.CommandLine -replace '^"?[^"]*AxKit\.exe"?\s*', '').Trim()
            $what = if ($words -match '^mcp(\s|$)') { "the AI tools' broker of a Claude Code that is running (close that Claude Code)" }
                elseif ($words) { "the window started as: axkit $words" }
                else { 'an axKit window' }
            Write-Host "  process $($process.ProcessId): $what"
        }
        Write-Host "Or start what is already built, without building: .\run.ps1 -NoBuild"
        # The background apps ended above run again, from the build that is there: the hotkeys never stay dead.
        foreach ($verb in @($background | ForEach-Object Verb | Where-Object { $_ -ne 'describe' } | Sort-Object -Unique)) {
            Start-Process -FilePath $exe -ArgumentList $verb, '--hidden' -WindowStyle Hidden
        }
        exit 1
    }

    Write-Host "Building axKit..."
    dotnet build "$root\src\AxKit\AxKit.csproj" -c Debug -nologo -v q
    $built = $LASTEXITCODE
    # The compiler server ends with the build instead of idling for ten minutes on several hundred megabytes
    # (Directory.Build.rsp ends the MSBuild nodes; 2026-11-08).
    dotnet build-server shutdown | Out-Null
}
if (-not (Test-Path $exe)) {
    Write-Host "AxKit.exe was not found at $exe. Run without -NoBuild first."
    exit 1
}

# The background apps, axStart, axClip and axHelp, run after every start from here, whether they ran before or not
# (asked 2026-11-15): each that is not running from this build is started hidden, so that its hotkey works and runs
# the code just built; after a failed build too, from the build that is there. The app being started itself is left
# to the lines below.
$runningVerbs = @(Get-CimInstance Win32_Process -Filter "Name = 'AxKit.exe'" |
    Where-Object { $_.ExecutablePath -eq $exe } |
    ForEach-Object { [regex]::Match($_.CommandLine, '\s(start|clip|help)(\s|$)').Groups[1].Value })
foreach ($verb in 'start', 'clip', 'help') {
    if ($verb -ne $App -and $runningVerbs -notcontains $verb) {
        Write-Host "Starting axKit $verb in the background, hidden."
        Start-Process -FilePath $exe -ArgumentList $verb, '--hidden' -WindowStyle Hidden
    }
}

if (-not $NoBuild -and $built -ne 0) {
    Write-Host "Build failed."
    exit $built
}

if ($App -ne 'claude') {
    $name = $chosen.Name
    # An app that opens nothing (Opens.Nothing in Dispatch.Apps) is started without the path.
    if ($Path -and $chosen.Opens -ne 'Nothing') {
        Write-Host "Starting $name on $Path"
        & $exe $App $Path
    } else {
        Write-Host "Starting $name"
        & $exe $App
    }
    exit $LASTEXITCODE
}

if (-not $Path) { $Path = (Get-Location).Path }
$Path = (Resolve-Path $Path).Path
Write-Host "Starting axClaude on $Path"
if ($New) {
    # A bare -- never reaches the app: PowerShell takes it as the end of the script's own parameters.
    & $exe claude $Path '--'
} elseif ($ClaudeArgs -and $ClaudeArgs.Count -gt 0) {
    & $exe claude $Path '--' @ClaudeArgs
} else {
    & $exe claude $Path
}
