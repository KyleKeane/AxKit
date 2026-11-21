<#
.SYNOPSIS
  The maintainer's signed approvals of outside reports, as the scripts check them (docs/axKit/outside-reports.md
  sections 5 to 7). Dot-sourced by tools\reports-check.ps1, tools\item.ps1, tools\item-of-commit.ps1 and
  tools\outside-report.ps1; it defines functions and runs nothing.

.DESCRIPTION
  The same rules as AxTask.Core's Approvals, which signs in axTask: the approval text of an entry (OR-5.3) and the
  check of its signature with the public key pinned in the code repository, docs\axKit\approval-key.pub (OR-6.2),
  never with a key beside the repository. RSA with PKCS#1 v1.5 padding over SHA-256, through .NET's RSACng, which
  Windows PowerShell 5.1 and PowerShell 7 both have. A test of AxTask.Tests verifies, through this file, a signature
  made by Approvals, so that the two make the same text.

  An entry is one item file of the outside reports list (..\axkit-management\outside-reports): the fields by their
  titles (OR-3). The script stays ASCII (Windows PowerShell 5.1 reads it as ANSI).
#>

$script:ApprovalTextFormat = 'axkit-approval 1'
$script:ApprovalKeyFormat = 'axkit-approval-key 1'
$script:ApprovalChangeFormat = 'axkit-approval-key-change 1'

# The words that name a report, as an item's Where it is written and an entry's Report hold them.
$script:ReportWordsPattern = 'KyleKeane/AxKit-reports issues? (\d+)(?: and (\d+))?'

function Get-ApprovalLongText([string]$value) {
    if ($null -eq $value) { $value = '' }
    return $value.Replace("`r`n", "`n").Trim()
}

function Get-ApprovalLine([string]$name, [string]$value) {
    if ($null -eq $value) { $value = '' }
    $one = $value.Replace("`r`n", "`n").Replace("`n", '\n').Trim()
    return "${name}: $one`n"
}

function Get-ApprovalSha256Hex([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($text))
    } finally {
        $sha.Dispose()
    }
    return (($hash | ForEach-Object { $_.ToString('x2') }) -join '')
}

# The text an approval signs, from an entry's fields (a hashtable or an object with the field titles as members).
function Get-ApprovalText($fields, [string]$decision) {
    $text = "$script:ApprovalTextFormat`n"
    $text += Get-ApprovalLine 'decision' $decision
    $text += Get-ApprovalLine 'report' $fields['Report']
    $text += Get-ApprovalLine 'title' $fields['Title']
    $text += Get-ApprovalLine 'category' $fields['Category']
    $text += Get-ApprovalLine 'item' $fields['Item']
    $text += Get-ApprovalLine 'priority' $fields['Proposed priority']
    $text += Get-ApprovalLine 'proposed' (Get-ApprovalLongText $fields['Proposed'])
    $text += Get-ApprovalLine 'review' (Get-ApprovalSha256Hex (Get-ApprovalLongText $fields['Harm review']))
    $text += Get-ApprovalLine 'date' $fields['Approved on']
    return $text
}

# The pinned public key: an object with Modulus, Exponent (byte arrays) and Fingerprint, or $null when the file is
# missing or is none (a line missing, a fingerprint that does not fit the key).
function Get-PinnedApprovalKey([string]$repositoryRoot) {
    $path = Join-Path $repositoryRoot 'docs\axKit\approval-key.pub'
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    return Read-PinnedApprovalKeyText ([IO.File]::ReadAllText($path))
}

# The same from the file's text (as git show gives a staged or committed copy).
function Read-PinnedApprovalKeyText([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) { return $null }
    $lines = @($text.Replace("`r`n", "`n").Split("`n"))
    if ($lines.Count -eq 0 -or $lines[0].Trim() -ne $script:ApprovalKeyFormat) { return $null }
    $values = @{}
    foreach ($line in $lines | Select-Object -Skip 1) {
        $colon = $line.IndexOf(':')
        if ($colon -gt 0) { $values[$line.Substring(0, $colon).Trim()] = $line.Substring($colon + 1).Trim() }
    }
    if (-not $values.ContainsKey('modulus') -or -not $values.ContainsKey('exponent') -or -not $values.ContainsKey('fingerprint')) { return $null }
    try {
        $modulus = [Convert]::FromBase64String($values['modulus'])
        $exponent = [Convert]::FromBase64String($values['exponent'])
    } catch {
        return $null
    }
    $both = New-Object byte[] ($modulus.Length + $exponent.Length)
    [Array]::Copy($modulus, 0, $both, 0, $modulus.Length)
    [Array]::Copy($exponent, 0, $both, $modulus.Length, $exponent.Length)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($both) } finally { $sha.Dispose() }
    $fingerprint = ((($hash | ForEach-Object { $_.ToString('x2') }) -join '')).Substring(0, 16)
    if ($fingerprint -ne $values['fingerprint']) { return $null }
    return [pscustomobject]@{ Modulus = $modulus; Exponent = $exponent; Fingerprint = $fingerprint }
}

# Whether a signature (base64) is the pinned key's over the text. Never throws: anything unreadable is no signature.
function Test-ApprovalSignature([string]$text, [string]$signature, $key) {
    if ($null -eq $key -or [string]::IsNullOrWhiteSpace($signature)) { return $false }
    try {
        $parameters = New-Object System.Security.Cryptography.RSAParameters
        $parameters.Modulus = $key.Modulus
        $parameters.Exponent = $key.Exponent
        $rsa = New-Object System.Security.Cryptography.RSACng
        try {
            $rsa.ImportParameters($parameters)
            return $rsa.VerifyData([System.Text.Encoding]::UTF8.GetBytes($text), [Convert]::FromBase64String($signature.Trim()),
                [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
        } finally {
            $rsa.Dispose()
        }
    } catch {
        return $false
    }
}

# The text a change of the pinned key signs with the old key (OR-6.3).
function Get-ApprovalChangeText([string]$newFingerprint) {
    return "$script:ApprovalChangeFormat`nnew: $newFingerprint`n"
}

# Every item of the priorities list from outside that may not be worked on: an item not done whose Where it is
# written names a report, with no approved entry of that report naming it. Each as "<title>: report <n>".
function Get-UnapprovedOutsideItems([string]$priorities, $entries) {
    $found = @()
    if (-not (Test-Path -LiteralPath $priorities)) { return $found }
    foreach ($file in @(Get-ChildItem -LiteralPath $priorities -Filter *.json -File | Where-Object { $_.Name -ne 'list.json' })) {
        $fields = Read-ApprovalItemFields $file.FullName
        if ($null -eq $fields) { continue }
        if ($fields['Done'] -eq 'True') { continue }
        $title = [string]$fields['Title']
        foreach ($report in @(Get-ReportNumbers $fields['Where it is written'])) {
            if (-not (Test-ApprovedForItem $entries $report $title)) { $found += "${title}: report $report" }
        }
    }
    return $found
}

# The fields of an item file by their titles, or $null for a file that is no item.
function Read-ApprovalItemFields([string]$path) {
    try {
        $json = [IO.File]::ReadAllText($path) | ConvertFrom-Json
    } catch {
        return $null
    }
    if ($null -eq $json -or $null -eq $json.fields) { return $null }
    $fields = @{}
    foreach ($field in @($json.fields)) {
        if ($null -ne $field.title) { $fields[[string]$field.title] = if ($null -eq $field.value) { '' } else { [string]$field.value } }
    }
    return $fields
}

# The report numbers some words name ("KyleKeane/AxKit-reports issue 8", "issues 8 and 9"), as integers.
function Get-ReportNumbers([string]$words) {
    $numbers = @()
    if ([string]::IsNullOrEmpty($words)) { return $numbers }
    foreach ($match in [regex]::Matches($words, $script:ReportWordsPattern)) {
        $numbers += [int]$match.Groups[1].Value
        if ($match.Groups[2].Success) { $numbers += [int]$match.Groups[2].Value }
    }
    return $numbers
}

# Every entry of the outside reports list: Path, Fields, Reports (numbers), State, and Valid, whether an Approved or
# Declined entry's signature verifies against the pinned key for its State's decision.
function Get-OutsideEntries([string]$listFolder, $key) {
    $entries = @()
    if (-not (Test-Path -LiteralPath $listFolder)) { return $entries }
    foreach ($file in @(Get-ChildItem -LiteralPath $listFolder -Filter *.json -File | Where-Object { $_.Name -ne 'list.json' })) {
        $fields = Read-ApprovalItemFields $file.FullName
        if ($null -eq $fields) { continue }
        $state = [string]$fields['State']
        $valid = $false
        if ($state -eq 'Approved' -or $state -eq 'Declined' -or ($state -eq 'Done' -and $fields['Approval'])) {
            $decision = if ($state -eq 'Declined') { 'declined' } else { 'approved' }
            $valid = Test-ApprovalSignature (Get-ApprovalText $fields $decision) $fields['Approval'] $key
        }
        $entries += [pscustomobject]@{
            Path = $file.FullName
            Fields = $fields
            Reports = @(Get-ReportNumbers $fields['Report'])
            State = $state
            Valid = $valid
        }
    }
    return $entries
}

# Whether an approved entry of the report names the item by its title: the one way an item from outside may exist
# or be worked on (OR-7.1 to OR-7.3).
function Test-ApprovedForItem($entries, [int]$report, [string]$itemTitle) {
    foreach ($entry in $entries) {
        if ($entry.Reports -notcontains $report) { continue }
        if (-not $entry.Valid) { continue }
        if ($entry.State -ne 'Approved' -and $entry.State -ne 'Done') { continue }
        if ($entry.Fields['Category'] -notin @('Same as an item', 'Addition to an item', 'New item')) { continue }
        if ([string]$entry.Fields['Item'] -eq $itemTitle) { return $true }
    }
    return $false
}
