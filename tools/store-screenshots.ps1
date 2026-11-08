<#
.SYNOPSIS
  Takes the Store listing's screenshots (docs/axkit/store-listing.md, docs/axkit/SPEC.md AX-13.6): each app's window
  maximised on the files of manual_tests, the whole screen saved as a PNG into publish\store. Run only with the user
  away from the keyboard: the windows open in front and take the focus.

.DESCRIPTION
  Starts the Release program of the package folder (publish\msix\package\AxKit.exe, built by publish.ps1 -Msix) once per
  shot, unpackaged, waits for the window, maximises it, captures the screen at its real size (the process is made DPI
  aware first, so a 150 % screen is captured in full pixels), closes the window and goes on. The Store wants PNG of
  1366 by 768 or larger; the screen must be at least that. axClaude is left out, since it would start a Claude Code
  session on the maintainer's account; axMedia too, since it plays. The captions are in store-listing.md.

.EXAMPLE
  .\tools\store-screenshots.ps1          # after publish.ps1 -Msix; the user away
#>
[CmdletBinding()]
param(
    [string]$Exe = (Join-Path (Split-Path -Parent $PSScriptRoot) 'publish\msix\package\AxKit.exe'),
    [string]$Out = (Join-Path (Split-Path -Parent $PSScriptRoot) 'publish\store')
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path $Exe)) { throw "$Exe is not there: run publish.ps1 -Msix first." }
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -Namespace Shots -Name Native -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
'@
[void][Shots.Native]::SetProcessDPIAware()
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
if ($screen.Width -lt 1366 -or $screen.Height -lt 768) { throw "The screen is $($screen.Width) by $($screen.Height); the Store wants 1366 by 768 or more." }
New-Item -ItemType Directory -Force $Out | Out-Null

# The test files: manual_tests\prepare.ps1 makes them (git ignores all but the script and the _main.md files).
& (Join-Path $root 'manual_tests\prepare.ps1') axdown | Out-Null
& (Join-Path $root 'manual_tests\prepare.ps1') axjson | Out-Null
& (Join-Path $root 'manual_tests\prepare.ps1') axtask | Out-Null
$markdown = Get-ChildItem (Join-Path $root 'manual_tests\axdown') -Filter '*.md' -File | Where-Object { $_.Name -ne '_main.md' } | Select-Object -First 1
$json = Get-ChildItem (Join-Path $root 'manual_tests\axjson') -Filter '*.json' -File | Select-Object -First 1
if (-not $markdown) { $markdown = Get-Item (Join-Path $root 'docs\axkit\README.md') }
if (-not $json) { $json = Get-Item (Join-Path $root 'global.json') }

# One shot per line: the file's number and name, the verb, the argument, and the seconds to wait for the window to settle.
$shots = @(
    @{ Name = '1-axdown'; Verb = 'down'; Argument = $markdown.FullName; Settle = 3 },
    @{ Name = '2-axfile'; Verb = 'file'; Argument = $root; Settle = 3 },
    @{ Name = '3-axterminal'; Verb = 'terminal'; Argument = $root; Settle = 6 },
    @{ Name = '4-axgit'; Verb = 'git'; Argument = $root; Settle = 5 },
    @{ Name = '5-axtask'; Verb = 'task'; Argument = (Join-Path $root 'manual_tests\axtask'); Settle = 3 },
    @{ Name = '6-axjson'; Verb = 'json'; Argument = $json.FullName; Settle = 3 },
    @{ Name = '7-axcalc'; Verb = 'calc'; Argument = ''; Settle = 3 },
    @{ Name = '8-axprocess'; Verb = 'process'; Argument = ''; Settle = 5 }
)

function Wait-Window([int[]]$before) {
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 500
        $new = Get-Process AxKit -ErrorAction SilentlyContinue | Where-Object { $before -notcontains $_.Id -and $_.MainWindowHandle -ne [IntPtr]::Zero }
        if ($new) { return $new | Select-Object -First 1 }
    }
    return $null
}

foreach ($shot in $shots) {
    $before = @(Get-Process AxKit -ErrorAction SilentlyContinue | ForEach-Object { $_.Id })
    $arguments = if ($shot.Argument) { "$($shot.Verb) `"$($shot.Argument)`"" } else { $shot.Verb }
    Start-Process -FilePath $Exe -ArgumentList $arguments | Out-Null
    $process = Wait-Window $before
    if (-not $process) { Write-Host "$($shot.Name): no window appeared"; continue }
    [void][Shots.Native]::ShowWindow($process.MainWindowHandle, 3)   # SW_MAXIMIZE
    [void][Shots.Native]::SetForegroundWindow($process.MainWindowHandle)
    Start-Sleep -Seconds $shot.Settle
    $bitmap = New-Object System.Drawing.Bitmap $screen.Width, $screen.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($screen.Left, $screen.Top, 0, 0, $bitmap.Size)
    $graphics.Dispose()
    $file = Join-Path $Out "$($shot.Name).png"
    $bitmap.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    Write-Host ("{0}: {1} ({2} by {3}, '{4}')" -f $shot.Name, $file, $screen.Width, $screen.Height, $process.MainWindowTitle)
    [void]$process.CloseMainWindow()
    if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 1
}
Write-Host "Screenshots in $Out; the captions are in docs/axkit/store-listing.md."
