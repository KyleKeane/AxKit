<#
.SYNOPSIS
  Takes the Store listing's screenshots (docs/axKit/store-listing.md, docs/axKit/SPEC.md AX-13.6): each app's window
  maximised on the files of manual_tests, the whole screen saved as a PNG into publish\store. Run only with the user
  away from the keyboard: the windows open in front and take the focus.

.DESCRIPTION
  Starts the Release program of the package folder (publish\msix\package\AxKit.exe, built by publish.ps1 -Msix) once per
  shot, unpackaged, waits for the window, puts it at the top left at 1920 by 1200 and captures that rectangle in real
  pixels (the process is made DPI aware first), closes the window and goes on. The Store wants PNG of 1366 by 768 or
  larger. axClaude is left out, since it would start a Claude Code session on the maintainer's account; axMedia too,
  since it plays; axGIT and axTerminal only with -WithAccessLayers, once the user has connected them on this computer,
  since until then they show their consent reminder. The captions are in store-listing.md.

.EXAMPLE
  .\tools\store-screenshots.ps1                     # after publish.ps1 -Msix; the user away
  .\tools\store-screenshots.ps1 -WithAccessLayers   # axTerminal and axGIT too, after their first Connect here
#>
[CmdletBinding()]
param(
    [string]$Exe = (Join-Path (Split-Path -Parent $PSScriptRoot) 'publish\msix\package\AxKit.exe'),
    [string]$Out = (Join-Path (Split-Path -Parent $PSScriptRoot) 'publish\store'),
    # axGIT and axTerminal too: they show their consent reminder instead of the app until the user has connected once
    # on this computer, so they are taken only when asked, after that.
    [switch]$WithAccessLayers
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path $Exe)) { throw "$Exe is not there: run publish.ps1 -Msix first." }
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -Namespace Shots -Name Native -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
[DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
[StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
[DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attribute, out RECT rect, int size);
'@
[void][Shots.Native]::SetProcessDPIAware()
# The window is put at the top left at this size and that rectangle alone is captured: it leaves out whatever Windows
# draws at the screen's bottom right (an activation watermark on a computer that has none), and it is the Store's
# minimum and more.
$width = 1920
$height = 1200
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
if ($screen.Width -lt $width -or $screen.Height -lt $height) { throw "The screen is $($screen.Width) by $($screen.Height); the shots need $width by $height." }
New-Item -ItemType Directory -Force $Out | Out-Null

# The files the shots show live in a demo folder under the Public profile, never under the maintainer's own: a window's
# status bar prints the folder's path, and an account's name is personal data (bundle rule 6). The folder is made
# afresh from the repository's pages and the prepared test files and removed at the end.
$demo = Join-Path $env:PUBLIC 'axKit demo'
if (Test-Path $demo) { Remove-Item $demo -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $demo 'Tasks') | Out-Null
foreach ($page in 'README.md', 'CHANGELOG.md', 'PRIVACY.md', 'NOTICE.md', 'GOVERNANCE.md', 'LICENSE', 'global.json') {
    Copy-Item (Join-Path $root $page) $demo
}
Copy-Item (Join-Path $root 'docs\axKit\README.md') (Join-Path $demo 'axKit guide.md')
& (Join-Path $root 'manual_tests\prepare.ps1') axjson | Out-Null
& (Join-Path $root 'manual_tests\prepare.ps1') axtask | Out-Null
$json = Get-ChildItem (Join-Path $root 'manual_tests\axJSON') -Filter '*.json' -File | Sort-Object Length -Descending | Select-Object -First 1
if ($json) { Copy-Item $json.FullName $demo; $jsonName = $json.Name } else { $jsonName = 'global.json' }
Copy-Item (Join-Path $root 'manual_tests\axTask\*') (Join-Path $demo 'Tasks') -Recurse -Force
# The repository as a git repository is the maintainer's folder; the access layers' shots take the demo folder, which
# git does not know, so axGIT shows its "no repository" state unless the maintainer points it elsewhere by hand.

# One shot per line: the file's number and name, the verb, the argument, and the seconds to wait for the window to settle.
$shots = @(
    @{ Name = '1-axdown'; Verb = 'down'; Argument = (Join-Path $demo 'axKit guide.md'); Settle = 3 },
    @{ Name = '2-axfile'; Verb = 'file'; Argument = $demo; Settle = 3 },
    @{ Name = '3-axtask'; Verb = 'task'; Argument = (Join-Path $demo 'Tasks'); Settle = 3 },
    @{ Name = '4-axjson'; Verb = 'json'; Argument = (Join-Path $demo $jsonName); Settle = 3 },
    @{ Name = '5-axcalc'; Verb = 'calc'; Argument = ''; Settle = 3 },
    @{ Name = '6-axprocess'; Verb = 'process'; Argument = ''; Settle = 5 }
)
if ($WithAccessLayers) {
    $shots += @{ Name = '7-axterminal'; Verb = 'terminal'; Argument = $demo; Settle = 6 }
    $shots += @{ Name = '8-axgit'; Verb = 'git'; Argument = $demo; Settle = 5 }
}

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
    [void][Shots.Native]::MoveWindow($process.MainWindowHandle, $screen.Left, $screen.Top, $width, $height, $true)
    [void][Shots.Native]::SetForegroundWindow($process.MainWindowHandle)
    Start-Sleep -Seconds $shot.Settle
    # The window's visible frame (DWMWA_EXTENDED_FRAME_BOUNDS, 9): the rectangle given to MoveWindow includes an
    # invisible border on three sides, behind which another window would show.
    $frame = New-Object Shots.Native+RECT
    if ([Shots.Native]::DwmGetWindowAttribute($process.MainWindowHandle, 9, [ref]$frame, [System.Runtime.InteropServices.Marshal]::SizeOf($frame)) -ne 0) {
        $frame.Left = $screen.Left; $frame.Top = $screen.Top; $frame.Right = $screen.Left + $width; $frame.Bottom = $screen.Top + $height
    }
    $shotWidth = $frame.Right - $frame.Left
    $shotHeight = $frame.Bottom - $frame.Top
    $bitmap = New-Object System.Drawing.Bitmap $shotWidth, $shotHeight
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($frame.Left, $frame.Top, 0, 0, $bitmap.Size)
    $graphics.Dispose()
    $file = Join-Path $Out "$($shot.Name).png"
    $bitmap.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    Write-Host ("{0}: {1} ({2} by {3}, '{4}')" -f $shot.Name, $file, $shotWidth, $shotHeight, $process.MainWindowTitle)
    [void]$process.CloseMainWindow()
    if (-not $process.WaitForExit(5000)) { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 1
}
Remove-Item $demo -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Screenshots in $Out; the captions are in docs/axKit/store-listing.md."
