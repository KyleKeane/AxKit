<#
.SYNOPSIS
  The release signing key (docs/axKit/security-audit-2026-10-24.md S-27, the user's decision): makes it, signs a
  release's zip with it, and prints its public half.

.DESCRIPTION
  An ECDSA P-256 key kept on the maintainer's machine, never in this repository: its private half is kept in
  %LOCALAPPDATA%\AxKit-release\release-key.bin, encrypted for the Windows account that made it (DPAPI), so that a copy
  of the file is useless elsewhere. release.ps1 signs every release's zip with it before the release is published;
  the updater (AxKit.Core/Updates/ReleaseSigning) and install.cmd refuse a zip whose signature does not check against
  the public half built into them. Losing the key means one release installed
  by hand with a new key's public half in it.

  Windows PowerShell 5.1 and .NET Framework only: CngKey, ECDsaCng and ProtectedData, nothing to install.

.EXAMPLE
  .\tools\release-key.ps1 new                          # once: makes the key, prints the public half for ReleaseSigning.cs
  .\tools\release-key.ps1 public                       # the public half again
  .\tools\release-key.ps1 sign publish\AxKit-3.7.0-win-x64.zip    # writes AxKit-3.7.0-win-x64.zip.sig next to it
  .\tools\release-key.ps1 verify publish\AxKit-3.7.0-win-x64.zip  # checks the .sig against the public half
  Get-Content ..\axkit-keys\release-key.passphrase.txt | .\tools\release-key.ps1 export ..\axkit-keys\release-key.backup
  Get-Content ..\axkit-keys\release-key.passphrase.txt | .\tools\release-key.ps1 import ..\axkit-keys\release-key.backup

  export and import (2026-11-15, the maintainer's decision): a backup of the private half, encrypted with a passphrase
  read from standard input (PBKDF2-SHA256, AES-256-CBC, HMAC-SHA256), for the private repository KyleKeane/AxKit-keys,
  where it is kept beside its passphrase, so that a second computer can release. This puts the key behind the GitHub
  sign-in, which S-27 had kept it from; the maintainer chose it knowingly. import never replaces a key that exists.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet('new', 'public', 'sign', 'verify', 'export', 'import')]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$File
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security
Add-Type -AssemblyName System.Core
$keyFolder = Join-Path $env:LOCALAPPDATA 'AxKit-release'
$keyFile = Join-Path $keyFolder 'release-key.bin'
$entropy = [Text.Encoding]::UTF8.GetBytes('AxKit release signing key')

function Get-PrivateKey {
    if (-not (Test-Path -LiteralPath $keyFile)) {
        throw "No release key at $keyFile. Make one with: .\tools\release-key.ps1 new"
    }

    $blob = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyFile), $entropy, 'CurrentUser')
    return [Security.Cryptography.CngKey]::Import($blob, [Security.Cryptography.CngKeyBlobFormat]::EccPrivateBlob)
}

# The public half as the apps hold it: the point's X and Y, 32 bytes each, in base64.
function Get-PublicText([Security.Cryptography.CngKey]$key) {
    $blob = $key.Export([Security.Cryptography.CngKeyBlobFormat]::EccPublicBlob)
    # BCRYPT_ECCKEY_BLOB: magic (4), key length (4), X (32), Y (32).
    return [Convert]::ToBase64String($blob, 8, 64)
}

function Get-Hash([string]$path) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $stream = [IO.File]::OpenRead((Resolve-Path -LiteralPath $path).Path)
        try { return $sha.ComputeHash($stream) } finally { $stream.Dispose() }
    }
    finally { $sha.Dispose() }
}

# The backup's keys from a passphrase: 32 bytes for AES and 32 for the HMAC.
$backupMagic = [Text.Encoding]::ASCII.GetBytes('AXKITRK1')
function Get-BackupKeys([string]$passphrase, [byte[]]$salt) {
    $derive = New-Object Security.Cryptography.Rfc2898DeriveBytes($passphrase, $salt, 200000, [Security.Cryptography.HashAlgorithmName]::SHA256)
    try { return , $derive.GetBytes(64) } finally { $derive.Dispose() }
}

function Read-Passphrase {
    $line = [Console]::In.ReadLine()
    if (-not $line -or $line.Trim().Length -lt 12) { throw 'Give the passphrase on standard input, twelve characters at least.' }
    return $line.Trim()
}

switch ($Command) {
    'export' {
        if (-not $File) { throw 'Name the backup file to write.' }
        if (Test-Path -LiteralPath $File) { throw "$File exists already; a backup never replaces a file." }
        $passphrase = Read-Passphrase
        $blob = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyFile), $entropy, 'CurrentUser')
        $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
        $salt = New-Object byte[] 16; $rng.GetBytes($salt)
        $iv = New-Object byte[] 16; $rng.GetBytes($iv)
        $keys = Get-BackupKeys $passphrase $salt
        $aes = [Security.Cryptography.Aes]::Create()
        $aes.Key = $keys[0..31]; $aes.IV = $iv
        $cipher = $aes.CreateEncryptor().TransformFinalBlock($blob, 0, $blob.Length)
        [Array]::Clear($blob, 0, $blob.Length)
        $hmac = New-Object Security.Cryptography.HMACSHA256(,[byte[]]$keys[32..63])
        $body = $backupMagic + $salt + $iv + $cipher
        $mac = $hmac.ComputeHash([byte[]]$body)
        [IO.File]::WriteAllBytes($File, [byte[]]($backupMagic + $salt + $iv + $mac + $cipher))
        Write-Host "Wrote the release key's backup: $File"
    }
    'import' {
        if (-not $File) { throw 'Name the backup file to read.' }
        if (Test-Path -LiteralPath $keyFile) { throw "A release key already exists at $keyFile; import never replaces it." }
        $passphrase = Read-Passphrase
        $all = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $File).Path)
        if ([Text.Encoding]::ASCII.GetString($all, 0, 8) -ne 'AXKITRK1') { throw "$File is not a release key backup." }
        $salt = $all[8..23]; $iv = $all[24..39]; $mac = $all[40..71]; $cipher = $all[72..($all.Length - 1)]
        $keys = Get-BackupKeys $passphrase ([byte[]]$salt)
        $hmac = New-Object Security.Cryptography.HMACSHA256(,[byte[]]$keys[32..63])
        $expected = $hmac.ComputeHash([byte[]]($backupMagic + $salt + $iv + $cipher))
        if ([Convert]::ToBase64String($expected) -ne [Convert]::ToBase64String([byte[]]$mac)) { throw 'The passphrase is wrong or the backup was changed; nothing was written.' }
        $aes = [Security.Cryptography.Aes]::Create()
        $aes.Key = $keys[0..31]; $aes.IV = [byte[]]$iv
        $blob = $aes.CreateDecryptor().TransformFinalBlock([byte[]]$cipher, 0, $cipher.Length)
        $key = [Security.Cryptography.CngKey]::Import($blob, [Security.Cryptography.CngKeyBlobFormat]::EccPrivateBlob)
        New-Item -ItemType Directory -Force -Path $keyFolder | Out-Null
        [IO.File]::WriteAllBytes($keyFile, [Security.Cryptography.ProtectedData]::Protect($blob, $entropy, 'CurrentUser'))
        [Array]::Clear($blob, 0, $blob.Length)
        Write-Host "Put the release key back: $keyFile. Its public half:"
        Write-Output (Get-PublicText $key)
    }
    'new' {
        if (Test-Path -LiteralPath $keyFile) {
            throw "A release key already exists at $keyFile. It is never replaced by this script: the apps trust its public half."
        }

        $parameters = New-Object Security.Cryptography.CngKeyCreationParameters
        $parameters.ExportPolicy = [Security.Cryptography.CngExportPolicies]::AllowPlaintextExport
        $key = [Security.Cryptography.CngKey]::Create([Security.Cryptography.CngAlgorithm]::ECDsaP256, $null, $parameters)
        $private = $key.Export([Security.Cryptography.CngKeyBlobFormat]::EccPrivateBlob)
        New-Item -ItemType Directory -Force -Path $keyFolder | Out-Null
        [IO.File]::WriteAllBytes($keyFile, [Security.Cryptography.ProtectedData]::Protect($private, $entropy, 'CurrentUser'))
        [Array]::Clear($private, 0, $private.Length)
        Write-Host "Made the release key: $keyFile (readable only by this Windows account)."
        Write-Host "Its public half, for src\AxKit.Core\Updates\ReleaseSigning.cs and install.cmd:"
        Write-Output (Get-PublicText $key)
    }
    'public' {
        Write-Output (Get-PublicText (Get-PrivateKey))
    }
    'sign' {
        if (-not $File) { throw 'Name the file to sign.' }
        $key = Get-PrivateKey
        $ecdsa = New-Object Security.Cryptography.ECDsaCng($key)
        try {
            # The signature over the file's SHA-256, as r and s (32 bytes each), in base64: what ECDsa.VerifyHash reads.
            $signature = $ecdsa.SignHash((Get-Hash $File))
            $target = (Resolve-Path -LiteralPath $File).Path + '.sig'
            [IO.File]::WriteAllText($target, [Convert]::ToBase64String($signature))
            Write-Host "Signed: $target"
        }
        finally { $ecdsa.Dispose() }
    }
    'verify' {
        if (-not $File) { throw 'Name the file to check.' }
        $public = [Convert]::FromBase64String((& $PSCommandPath public))
        $blob = [byte[]](0x45, 0x43, 0x53, 0x31, 0x20, 0, 0, 0) + $public
        $ecdsa = New-Object Security.Cryptography.ECDsaCng([Security.Cryptography.CngKey]::Import($blob, [Security.Cryptography.CngKeyBlobFormat]::EccPublicBlob))
        $signature = [Convert]::FromBase64String([IO.File]::ReadAllText((Resolve-Path -LiteralPath "$File.sig").Path).Trim())
        if ($ecdsa.VerifyHash((Get-Hash $File), $signature)) { Write-Host 'The signature checks.'; exit 0 }
        Write-Host 'The signature does NOT check.'
        exit 1
    }
}
