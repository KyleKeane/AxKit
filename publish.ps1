<#
.SYNOPSIS
  Builds the distributable axKit: a self-contained single-file executable with every app inside, the installer and
  the bundle's README, zipped for sending, and installs it for the current user unless -NoInstall is given.

.DESCRIPTION
  Output: publish\win-x64\ (AxKit.exe, the apps' icons, install.cmd, install.ps1, README.md, LICENSE, PRIVACY.md, THIRD-PARTY-NOTICES.md, apps.json) and publish\AxKit-<version>-win-x64.zip.
  Send the zip to someone: they extract it and double-click install.cmd (see README.md inside). Without -NoInstall the
  script then runs install.ps1 from publish\win-x64 on this machine (Start menu entries, File Explorer entries and
  the console commands; see install.ps1 -? for details and -Uninstall).

  The release workflow calls it twice (docs/axkit/code-signing.md): -NoInstall -NoZip builds the folder, AxKit.exe is
  signed for Windows in it, then -ZipOnly zips the folder as it is. Neither installs anything.

.EXAMPLE
  .\publish.ps1                    # build the zip and install here
  .\publish.ps1 -NoInstall         # build the zip only
  .\publish.ps1 -NoInstall -NoZip  # build publish\win-x64 only
  .\publish.ps1 -ZipOnly           # zip publish\win-x64 as it is

  If PowerShell refuses to run scripts: powershell -ExecutionPolicy Bypass -File .\publish.ps1
#>
[CmdletBinding()]
param(
    [switch]$NoInstall,
    [switch]$NoZip,
    [switch]$ZipOnly
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out = Join-Path $root 'publish\win-x64'
$exe = Join-Path $out 'AxKit.exe'

if ($ZipOnly) {
    # The folder of an earlier -NoZip run, with AxKit.exe signed in place: nothing is built or copied again.
    if (-not (Test-Path $exe)) {
        Write-Host "Nothing to zip: $exe is not there. Run publish.ps1 -NoInstall -NoZip first."
        exit 1
    }
} else {
    # A fresh output folder: an earlier publish may have left files behind (AxClaude.exe from before the bundle), and
    # everything in the folder goes into the zip.
    if (Test-Path $out) { Remove-Item $out -Recurse -Force }

    # The reporter account's token for the reports (docs/axkit/reports.md RP-6.2), from the release secret when it is
    # set: written into the file the build embeds, and removed again whatever the build did, so that it never stays.
    $tokenTool = Join-Path $root 'tools\reporter-token.ps1'
    if (-not [string]::IsNullOrWhiteSpace($env:AXKIT_REPORTER_TOKEN)) {
        & $tokenTool -Token $env:AXKIT_REPORTER_TOKEN
    } else {
        Write-Host "No AXKIT_REPORTER_TOKEN in the environment: this build cannot send reports (docs/axkit/reports.md RP-6.2)."
    }

    Write-Host "Publishing axKit (Release, win-x64, self-contained, single file)..."
    try {
        dotnet publish "$root\src\AxKit\AxKit.csproj" -c Release -r win-x64 --self-contained `
            -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none `
            -o $out -nologo -v q
        $published = $LASTEXITCODE
    } finally {
        & $tokenTool -Remove
        # The compiler server ends with the build instead of idling for ten minutes (Directory.Build.rsp; 2026-11-08).
        dotnet build-server shutdown | Out-Null
    }
    if ($published -ne 0) {
        Write-Host "Publish failed."
        exit $published
    }

    # The apps' icons for their Start menu and File Explorer entries, next to the executable (a project item would be
    # bundled into the single file): every .ico of src\AxKit, so that a new app's icon is never left out (3.0.0 shipped
    # without axTerminal's when the list was kept by hand).
    Get-ChildItem (Join-Path $root 'src\AxKit') -Filter '*.ico' | Copy-Item -Destination $out -Force
    Copy-Item (Join-Path $root 'install.ps1') $out -Force
    Copy-Item (Join-Path $root 'install.cmd') $out -Force
    Copy-Item (Join-Path $root 'LICENSE') $out -Force
    # The terms of the licence's section 7 and the disclaimer, the Licence notice of About (NOTICE.md).
    Copy-Item (Join-Path $root 'NOTICE.md') $out -Force
    # What axKit sends anywhere, and the notices of what it contains (PRIVACY.md, THIRD-PARTY-NOTICES.md), beside the licence.
    Copy-Item (Join-Path $root 'PRIVACY.md') $out -Force
    Copy-Item (Join-Path $root 'THIRD-PARTY-NOTICES.md') $out -Force
    # The bundle's guide: install, the apps, update, remove. Each app's own guide is inside it, under Help.
    Copy-Item (Join-Path $root 'docs\axkit\README.md') (Join-Path $out 'README.md') -Force
    Get-ChildItem $out -Filter '*.pdb' | Remove-Item -Force
    # The list of apps (AxKit.exe apps, AX-1.4) in the zip as apps.json, which install.ps1 reads when the program cannot
    # answer on the machine it installs on (3.7.1's update failed so on one machine, 2026-10-24). Asked of the program
    # just built, so that it is that version's list; a program that cannot answer here fails the publish.
    $start = New-Object System.Diagnostics.ProcessStartInfo $exe
    $start.Arguments = 'apps'
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.CreateNoWindow = $true
    $process = [System.Diagnostics.Process]::Start($start)
    $listing = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0 -or -not $listing.Trim().StartsWith('[')) {
        Write-Host "AxKit.exe apps did not answer (exit code $($process.ExitCode)); the zip was not made."
        exit 1
    }
    [System.IO.File]::WriteAllText((Join-Path $out 'apps.json'), $listing)
    Write-Host ("Published {0} ({1:N1} MB)" -f $exe, ((Get-Item $exe).Length / 1MB))

    if ($NoZip) {
        exit 0
    }
}

$version = (Get-Item $exe).VersionInfo.FileVersion
if ($version -match '^(\d+\.\d+\.\d+)') { $version = $Matches[1] }
$zip = Join-Path $root "publish\AxKit-$version-win-x64.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
Write-Host ("Zip for sending: {0} ({1:N1} MB)" -f $zip, ((Get-Item $zip).Length / 1MB))

if ($NoInstall -or $ZipOnly) {
    exit 0
}

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $out 'install.ps1')
exit $LASTEXITCODE
