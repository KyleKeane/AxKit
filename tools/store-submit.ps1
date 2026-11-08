<#
.SYNOPSIS
  Posts a release's package to the Microsoft Store through Partner Center's submission API, or reads the Store's state
  of axKit (docs/axkit/store-setup.md sections 4 and 5; docs/axkit/SPEC.md AX-13.6). Plain PowerShell and HTTPS, no
  module: the API is Microsoft's documented one, https://learn.microsoft.com/windows/uwp/monetize/manage-app-submissions.

.DESCRIPTION
  The credentials are the Partner Center application's tenant id, client id and key, read from the environment
  (PARTNER_CENTER_TENANT_ID, PARTNER_CENTER_CLIENT_ID, PARTNER_CENTER_CLIENT_SECRET, the "release" environment's
  secrets in GitHub Actions) or, on the maintainer's computer, from the file tools/store-credentials.ps1 keeps.

  -Package <msix> [-Notes <file>]: a new submission is created as a copy of the last published one; its packages are
  marked for deletion and the given package added; What's new is set from the notes file (the first 1500 characters,
  the Store's limit); the package goes up in a zip to the address the API gives; the submission is committed; the
  script then waits (WaitMinutes) until the Store has taken it in (PreProcessing, Certification or later) or refused
  it, and says which. Certification itself takes hours or days at Microsoft's and needs nothing from us. A pending
  submission stops the script unless -ReplacePending deletes it first: one made by hand in Partner Center is never
  deleted without the maintainer's word.

  -Status: the application as Partner Center sees it, the last published and the pending submission with their
  status and the Store's error and warning codes.

  -DryRun with -Package: shapes the submission from a built-in sample, writes the zip, and touches no network: what
  the script would send, for a check without credentials.

.EXAMPLE
  .\tools\store-submit.ps1 -Status
  .\tools\store-submit.ps1 -Package publish\AxKit-5.2.1.msix -Notes notes.md
  .\tools\store-submit.ps1 -Package publish\AxKit-5.2.1.msix -DryRun
#>
[CmdletBinding(DefaultParameterSetName = 'Submit')]
param(
    [Parameter(ParameterSetName = 'Submit', Mandatory)][string]$Package,
    [Parameter(ParameterSetName = 'Submit')][string]$Notes,
    [Parameter(ParameterSetName = 'Submit')][switch]$ReplacePending,
    [Parameter(ParameterSetName = 'Submit')][switch]$DryRun,
    [Parameter(ParameterSetName = 'Status', Mandatory)][switch]$Status,
    [string]$ApplicationId = '9PK01L2K5000',
    [int]$WaitMinutes = 20
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$api = 'https://manage.devcenter.microsoft.com/v1.0/my'
$notesLimit = 1500
$script:token = $null

# ---- the credentials and the token ----

function Get-Credentials {
    $tenant = $env:PARTNER_CENTER_TENANT_ID
    $client = $env:PARTNER_CENTER_CLIENT_ID
    $secret = $env:PARTNER_CENTER_CLIENT_SECRET
    if ($tenant -and $client -and $secret) {
        return @{ TenantId = $tenant.Trim(); ClientId = $client.Trim(); Secret = $secret }
    }
    $file = Join-Path $env:LOCALAPPDATA 'AxKit-release\store-credentials.xml'
    if (-not (Test-Path $file)) {
        throw "No credentials: set PARTNER_CENTER_TENANT_ID, PARTNER_CENTER_CLIENT_ID and PARTNER_CENTER_CLIENT_SECRET, or run tools\store-credentials.ps1 once (docs/axkit/store-setup.md section 2)."
    }
    $kept = Import-Clixml -LiteralPath $file
    $plain = [System.Runtime.InteropServices.Marshal]::PtrToStringUni([System.Runtime.InteropServices.Marshal]::SecureStringToGlobalAllocUnicode($kept.Secret))
    return @{ TenantId = $kept.TenantId; ClientId = $kept.ClientId; Secret = $plain }
}

# The error the API sent, which Invoke-RestMethod hides behind its status line.
function Error-Detail([System.Management.Automation.ErrorRecord]$record) {
    $detail = $record.ErrorDetails.Message
    if (-not $detail -and $record.Exception.Response) {
        try {
            $stream = $record.Exception.Response.GetResponseStream()
            if ($stream) { $detail = (New-Object System.IO.StreamReader $stream).ReadToEnd() }
        } catch { }
    }
    return "$($record.Exception.Message) $detail".Trim()
}

function Get-Token {
    $credentials = Get-Credentials
    try {
        $answer = Invoke-RestMethod -Method Post -Uri "https://login.microsoftonline.com/$($credentials.TenantId)/oauth2/token" -ContentType 'application/x-www-form-urlencoded' -Body @{
            grant_type = 'client_credentials'
            client_id = $credentials.ClientId
            client_secret = $credentials.Secret
            resource = 'https://manage.devcenter.microsoft.com'
        }
    } catch {
        throw "The sign-in as the Partner Center application failed: $(Error-Detail $_)"
    }
    return $answer.access_token
}

# One call of the API: the method, the path under /v1.0/my, and the body as an object (sent as JSON) or nothing.
function Api([string]$method, [string]$path, $body = $null) {
    if (-not $script:token) { $script:token = Get-Token }
    $arguments = @{ Method = $method; Uri = "$api/$path"; Headers = @{ Authorization = "Bearer $script:token" } }
    if ($null -ne $body) {
        $arguments.ContentType = 'application/json'
        $arguments.Body = [System.Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 64))
    }
    try {
        return Invoke-RestMethod @arguments
    } catch {
        throw "$method $path failed: $(Error-Detail $_)"
    }
}

function Say([string]$text) {
    Write-Host $text
    if ($env:GITHUB_STEP_SUMMARY) { Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $text }
}

function Status-Line($submission, [string]$label) {
    if (-not $submission -or -not $submission.id) { Say "$label : none"; return }
    $state = Api 'Get' "applications/$ApplicationId/submissions/$($submission.id)/status"
    Say "$label : submission $($submission.id), status $($state.status)"
    foreach ($problem in @($state.statusDetails.errors)) { Say "  error $($problem.code): $($problem.details)" }
    foreach ($problem in @($state.statusDetails.warnings)) { Say "  warning $($problem.code): $($problem.details)" }
}

# ---- status ----

if ($Status) {
    $app = Api 'Get' "applications/$ApplicationId"
    Say "axKit in the Store: $($app.primaryName), package family $($app.packageFamilyName), first published $(if ($app.firstPublishedDate) { $app.firstPublishedDate } else { 'not yet' })."
    Status-Line $app.lastPublishedApplicationSubmission 'Last published'
    Status-Line $app.pendingApplicationSubmission 'Pending'
    exit 0
}

# ---- submit ----

$packageFile = Get-Item -LiteralPath $Package
if ($packageFile.Extension -ne '.msix') { throw "$Package is not an .msix file." }
$fileName = $packageFile.Name
$releaseNotes = $null
if ($Notes) {
    $releaseNotes = (Get-Content -LiteralPath $Notes -Raw -Encoding UTF8).Trim()
    # ASCII only in this file: Windows PowerShell reads a script without a byte order mark as ANSI (found 2026-11-09).
    if ($releaseNotes.Length -gt $notesLimit) { $releaseNotes = $releaseNotes.Substring(0, $notesLimit - 3).TrimEnd() + '...' }
}

# The sample a dry run shapes: the fields the API returns that the script touches, with one package to replace.
$sample = @'
{
  "id": "1152921504621243540",
  "fileUploadUrl": "https://example.invalid/upload?sas=sample",
  "status": "PendingCommit",
  "targetPublishMode": "Immediate",
  "applicationPackages": [
    { "fileName": "AxKit-5.2.0.msix", "fileStatus": "Uploaded", "id": "1152921504621243541", "version": "5.2.0.0", "architecture": "x64", "languages": ["en-US"], "minimumDirectXVersion": "None", "minimumSystemRam": "None" }
  ],
  "listings": {
    "en-us": { "baseListing": { "title": "axKit", "description": "The description.", "releaseNotes": "The last release's notes.", "images": [] } }
  }
}
'@

if ($DryRun) {
    Say "Dry run: nothing is sent. The package $fileName, the notes $(if ($releaseNotes) { "$($releaseNotes.Length) characters" } else { 'none' })."
    $submission = $sample | ConvertFrom-Json
} else {
    $app = Api 'Get' "applications/$ApplicationId"
    if ($app.pendingApplicationSubmission -and $app.pendingApplicationSubmission.id) {
        $pending = $app.pendingApplicationSubmission.id
        if (-not $ReplacePending) {
            Status-Line $app.pendingApplicationSubmission 'Pending'
            throw "A submission is pending ($pending). Wait for it (tools\store-submit.ps1 -Status), or -ReplacePending deletes it first; never one made by hand in Partner Center without the maintainer's word."
        }
        Say "Deleting the pending submission $pending (-ReplacePending)."
        Api 'Delete' "applications/$ApplicationId/submissions/$pending" | Out-Null
    }
    $submission = Api 'Post' "applications/$ApplicationId/submissions"
    Say "Submission $($submission.id) created as a copy of the last published one."
}

# The package: every package of the copy marked for deletion, the new one added, so that one package serves every
# computer; the Store keeps nothing older.
$replaced = @()
foreach ($old in @($submission.applicationPackages)) {
    if ($old.fileStatus -ne 'PendingDelete') { $old.fileStatus = 'PendingDelete' }
    $replaced += "$($old.fileName) ($($old.version))"
}
$new = [pscustomobject]@{ fileName = $fileName; fileStatus = 'PendingUpload'; minimumDirectXVersion = 'None'; minimumSystemRam = 'None' }
$submission.applicationPackages = @(@($submission.applicationPackages) + @($new))
Say "Packages: $fileName added; marked for deletion: $(if ($replaced) { $replaced -join ', ' } else { 'none' })."

# What's new, in every language the listing has.
if ($releaseNotes) {
    foreach ($language in $submission.listings.PSObject.Properties) {
        $language.Value.baseListing.releaseNotes = $releaseNotes
    }
    Say "What's new set for $(@($submission.listings.PSObject.Properties).Count) language(s)."
}

# The zip: the package at the root under its own name, as the submission names it.
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("axkit-store-" + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Force (Join-Path $work 'content') | Out-Null
Copy-Item -LiteralPath $packageFile.FullName -Destination (Join-Path $work "content\$fileName")
$zip = Join-Path $work 'upload.zip'
Compress-Archive -Path (Join-Path $work 'content\*') -DestinationPath $zip
Say ("Upload zip: {0:N1} MB." -f ((Get-Item $zip).Length / 1MB))

if ($DryRun) {
    $shaped = Join-Path $work 'submission.json'
    $submission | ConvertTo-Json -Depth 64 | Set-Content -LiteralPath $shaped -Encoding UTF8
    Say "Dry run done. The shaped submission: $shaped; the zip: $zip."
    exit 0
}

try {
    # The update first, then the files, then the commit, in the API's order.
    Api 'Put' "applications/$ApplicationId/submissions/$($submission.id)" $submission | Out-Null
    Say 'Submission updated.'
    try {
        Invoke-WebRequest -Method Put -Uri $submission.fileUploadUrl -InFile $zip -ContentType 'application/zip' -Headers @{ 'x-ms-blob-type' = 'BlockBlob' } -UseBasicParsing | Out-Null
    } catch {
        throw "The upload failed: $(Error-Detail $_)"
    }
    Say 'Package uploaded.'
    $commit = Api 'Post' "applications/$ApplicationId/submissions/$($submission.id)/commit"
    Say "Committed: $($commit.status)."
} finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

# Until the Store has taken the submission in or refused it.
$deadline = (Get-Date).AddMinutes($WaitMinutes)
$taken = 'PreProcessing', 'Certification', 'Release', 'PendingPublication', 'Publishing', 'Published'
$failed = 'CommitFailed', 'PreProcessingFailed', 'CertificationFailed', 'ReleaseFailed', 'PublishFailed', 'Canceled'
while ($true) {
    Start-Sleep -Seconds 30
    $state = Api 'Get' "applications/$ApplicationId/submissions/$($submission.id)/status"
    foreach ($problem in @($state.statusDetails.errors)) { Say "  error $($problem.code): $($problem.details)" }
    foreach ($problem in @($state.statusDetails.warnings)) { Say "  warning $($problem.code): $($problem.details)" }
    if ($taken -contains $state.status) {
        Say "The Store has the submission $($submission.id): $($state.status). Certification goes on at Microsoft's; tools\store-submit.ps1 -Status reads it."
        exit 0
    }
    if ($failed -contains $state.status) {
        Say "The Store refused the submission $($submission.id): $($state.status). The codes above say why; Partner Center has the same."
        exit 1
    }
    if ((Get-Date) -gt $deadline) {
        Say "Still $($state.status) after $WaitMinutes minutes; tools\store-submit.ps1 -Status reads it later."
        exit 0
    }
    Write-Host "  $($state.status)..."
}
