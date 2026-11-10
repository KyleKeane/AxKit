<#
.SYNOPSIS
  Builds the distributable axKit: a self-contained single-file executable with every app inside, the installer and
  the bundle's README, zipped for sending, and installs it for the current user unless -NoInstall is given.

.DESCRIPTION
  Output: publish\win-x64\ (AxKit.exe, the apps' icons, install.cmd, install.ps1, README.md, LICENSE, NOTICE.md, PRIVACY.md, THIRD-PARTY-NOTICES.md, apps.json) and publish\AxKit-<version>-win-x64.zip.
  Send the zip to someone: they extract it and double-click install.cmd (see README.md inside). Without -NoInstall the
  script then runs install.ps1 from publish\win-x64 on this machine (Start menu entries, File Explorer entries and
  the console commands; see install.ps1 -? for details and -Uninstall).

  The release workflow calls it twice (docs/axKit/code-signing.md): -NoInstall -NoZip builds the folder, AxKit.exe is
  signed for Windows in it, then -ZipOnly zips the folder as it is. Neither installs anything.

  -Msix builds the MSIX package for the Microsoft Store instead (docs/axKit/store-distribution.md 3.1): a folder
  publish into publish\msix\package (every file as a file; the package is the single file), AppxManifest.xml from the
  apps listing (tools\make-manifest.ps1), the images (tools\make-icon.ps1 -Png), then makeappx from the Windows SDK
  (Windows Kits when the SDK is installed, as on the GitHub runners; otherwise tools\get-sdk-tools.ps1 fetches
  Microsoft's packaging tools once into tools\sdk) into publish\AxKit-<version>.msix. Unsigned: the Store signs it
  after certification. A local test registers the folder as the package, with Developer Mode on:
  Add-AppxPackage -Register publish\msix\package\AppxManifest.xml; Remove-AppxPackage removes it. Nothing is installed.

.EXAMPLE
  .\publish.ps1                    # build the zip and install here
  .\publish.ps1 -NoInstall         # build the zip only
  .\publish.ps1 -NoInstall -NoZip  # build publish\win-x64 only
  .\publish.ps1 -ZipOnly           # zip publish\win-x64 as it is
  .\publish.ps1 -Msix              # build the MSIX package for the Store

  If PowerShell refuses to run scripts: powershell -ExecutionPolicy Bypass -File .\publish.ps1
#>
[CmdletBinding()]
param(
    [switch]$NoInstall,
    [switch]$NoZip,
    [switch]$ZipOnly,
    [switch]$Msix
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$out = Join-Path $root 'publish\win-x64'
$exe = Join-Path $out 'AxKit.exe'

# The reporter account's token for the reports (docs/axKit/reports.md RP-6.2), from the release secret when it is
# set: written into the file the build embeds before a publish, and removed again whatever the build did, so that it
# never stays.
$tokenTool = Join-Path $root 'tools\reporter-token.ps1'
function Set-ReporterToken {
    if (-not [string]::IsNullOrWhiteSpace($env:AXKIT_REPORTER_TOKEN)) {
        & $tokenTool -Token $env:AXKIT_REPORTER_TOKEN
    } else {
        Write-Host "No AXKIT_REPORTER_TOKEN in the environment: this build cannot send reports (docs/axKit/reports.md RP-6.2)."
    }
}

# The list of apps (AxKit.exe apps, AX-1.4) as JSON, asked of the program just built, so that it is that version's
# list; a program that cannot answer here fails the publish. install.ps1 reads it as apps.json when the program cannot
# answer on the machine it installs on (3.7.1's update failed so on one machine, 2026-10-24); tools\make-manifest.ps1
# builds the package's manifest from it.
function Get-AppsListing([string]$program) {
    $start = New-Object System.Diagnostics.ProcessStartInfo $program
    $start.Arguments = 'apps'
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.CreateNoWindow = $true
    $process = [System.Diagnostics.Process]::Start($start)
    $listing = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0 -or -not $listing.Trim().StartsWith('[')) {
        Write-Host "AxKit.exe apps did not answer (exit code $($process.ExitCode)); nothing was made."
        exit 1
    }
    return $listing
}

# The bundle's version as the program file carries it, major.minor.patch.
function Get-ProgramVersion([string]$program) {
    $version = (Get-Item $program).VersionInfo.FileVersion
    if ($version -match '^(\d+\.\d+\.\d+)') { $version = $Matches[1] }
    return $version
}

# makeappx.exe: the newest Windows SDK under Windows Kits, else the tools tools\get-sdk-tools.ps1 keeps (fetched once).
function Find-MakeAppx {
    $kits = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $found = Get-ChildItem $kits -Directory -Filter '10.*' -ErrorAction SilentlyContinue |
        Sort-Object { [version]$_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName 'x64\makeappx.exe' } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
    if ($found) { return $found }
    return (& (Join-Path $root 'tools\get-sdk-tools.ps1') | Select-Object -Last 1)
}

if ($Msix) {
    $package = Join-Path $root 'publish\msix\package'
    if (Test-Path $package) { Remove-Item $package -Recurse -Force }
    Set-ReporterToken
    Write-Host "Publishing axKit for the package (Release, win-x64, self-contained, one file per file)..."
    try {
        dotnet publish "$root\src\AxKit\AxKit.csproj" -c Release -r win-x64 --self-contained `
            -p:PublishSingleFile=false -p:DebugType=none `
            -o $package -nologo -v q
        $published = $LASTEXITCODE
    } finally {
        & $tokenTool -Remove
        dotnet build-server shutdown | Out-Null
    }
    if ($published -ne 0) {
        Write-Host "Publish failed."
        exit $published
    }
    Get-ChildItem $package -Filter '*.pdb' | Remove-Item -Force
    $packageExe = Join-Path $package 'AxKit.exe'
    $version = Get-ProgramVersion $packageExe
    $appsFile = Join-Path $root 'publish\msix\apps.json'
    [System.IO.File]::WriteAllText($appsFile, (Get-AppsListing $packageExe))
    & (Join-Path $root 'tools\make-manifest.ps1') -Apps $appsFile -Out (Join-Path $package 'AppxManifest.xml') -Version $version
    & (Join-Path $root 'tools\make-icon.ps1') -Png (Join-Path $package 'Assets')
    $makeappx = Find-MakeAppx
    $packageFile = Join-Path $root "publish\AxKit-$version.msix"
    if (Test-Path $packageFile) { Remove-Item $packageFile -Force }
    & $makeappx pack /o /h SHA256 /d $package /p $packageFile
    if ($LASTEXITCODE -ne 0) {
        Write-Host "makeappx failed (exit code $LASTEXITCODE); the package was not made."
        exit $LASTEXITCODE
    }
    Write-Host ("Package: {0} ({1:N1} MB), from {2}" -f $packageFile, ((Get-Item $packageFile).Length / 1MB), $package)
    exit 0
}

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

    Set-ReporterToken
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
    Copy-Item (Join-Path $root 'docs\axKit\README.md') (Join-Path $out 'README.md') -Force
    Get-ChildItem $out -Filter '*.pdb' | Remove-Item -Force
    # The list of apps in the zip as apps.json, for install.ps1.
    [System.IO.File]::WriteAllText((Join-Path $out 'apps.json'), (Get-AppsListing $exe))
    Write-Host ("Published {0} ({1:N1} MB)" -f $exe, ((Get-Item $exe).Length / 1MB))

    if ($NoZip) {
        exit 0
    }
}

$version = Get-ProgramVersion $exe
$zip = Join-Path $root "publish\AxKit-$version-win-x64.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
Write-Host ("Zip for sending: {0} ({1:N1} MB)" -f $zip, ((Get-Item $zip).Length / 1MB))

if ($NoInstall -or $ZipOnly) {
    exit 0
}

& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $out 'install.ps1')
exit $LASTEXITCODE
