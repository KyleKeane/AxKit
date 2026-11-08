<#
.SYNOPSIS
  Keeps the Partner Center application's three values for tools/store-submit.ps1 on this computer: the tenant id, the
  client id and the key, typed at prompts and saved encrypted for this Windows account (Export-Clixml protects the
  key with the account's data protection, as the release key is kept; docs/axkit/store-setup.md).

.DESCRIPTION
  The file is %LOCALAPPDATA%\AxKit-release\store-credentials.xml, outside the repository. Nothing is ever printed
  but the two ids; the key never shows. In GitHub Actions the same three values are the "release" environment's
  secrets PARTNER_CENTER_TENANT_ID, PARTNER_CENTER_CLIENT_ID and PARTNER_CENTER_CLIENT_SECRET, set with
  gh secret set <name> --env release, which prompts for the value as this script does.

.EXAMPLE
  .\tools\store-credentials.ps1          # asks for the three values and saves them
  .\tools\store-credentials.ps1 -Show    # prints the ids and whether a key is kept
  .\tools\store-credentials.ps1 -Remove  # deletes the file
#>
[CmdletBinding()]
param(
    [switch]$Show,
    [switch]$Remove
)

$ErrorActionPreference = 'Stop'
$folder = Join-Path $env:LOCALAPPDATA 'AxKit-release'
$file = Join-Path $folder 'store-credentials.xml'

if ($Remove) {
    if (Test-Path $file) { Remove-Item $file -Force; Write-Host "Removed $file." } else { Write-Host "Nothing kept at $file." }
    exit 0
}

if ($Show) {
    if (-not (Test-Path $file)) { Write-Host "No credentials kept at $file. Run tools\store-credentials.ps1 to save them."; exit 1 }
    $kept = Import-Clixml -LiteralPath $file
    Write-Host "Tenant id: $($kept.TenantId)"
    Write-Host "Client id: $($kept.ClientId)"
    Write-Host "Key: $(if ($kept.Secret) { 'kept, encrypted for this Windows account' } else { 'missing' })"
    exit 0
}

Write-Host "The Partner Center application's values (docs/axkit/store-setup.md): the Tenant ID and Client ID from the application's page under Account settings, User management, Microsoft Entra applications; the key from Add new key there, shown once."
$tenant = (Read-Host 'Tenant ID').Trim()
$client = (Read-Host 'Client ID').Trim()
$secret = Read-Host 'Key (typed hidden)' -AsSecureString
if (-not $tenant -or -not $client -or $secret.Length -eq 0) { throw 'All three are needed; nothing was saved.' }
New-Item -ItemType Directory -Force $folder | Out-Null
[pscustomobject]@{ TenantId = $tenant; ClientId = $client; Secret = $secret } | Export-Clixml -LiteralPath $file
Write-Host "Saved to $file, the key encrypted for this Windows account. tools\store-submit.ps1 -Status reads the Store's state with them."
