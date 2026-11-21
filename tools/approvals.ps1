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

# The words that name a report, as an item's Where it is written and an entry's Report hold them: "AxKit-reports
# issue 8", "issues 8 and 9", "issues 8, 9 and 10", whatever their case, with or without a # before a number.
$script:ReportWordsPattern = '(?i)AxKit-reports\s+issues?\s+((?:#?\d+)(?:\s*(?:,|and|&)\s*#?\d+)*)'

function Get-ApprovalLongText([string]$value) {
    if ($null -eq $value) { $value = '' }
    return $value.Replace("`r`n", "`n").Trim()
}

function Get-ApprovalLine([string]$name, [string]$value) {
    if ($null -eq $value) { $value = '' }
    # A backslash doubled before a line end becomes \n, as Approvals.Line: two values never make the same line.
    $one = $value.Replace("`r`n", "`n").Trim().Replace('\', '\\').Replace("`n", '\n')
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
    $fingerprint = Get-ApprovalFingerprint $modulus $exponent
    if ($fingerprint -ne $values['fingerprint']) { return $null }
    return [pscustomobject]@{ Modulus = $modulus; Exponent = $exponent; Fingerprint = $fingerprint }
}

# A key's fingerprint: the first sixteen hexadecimal characters of the SHA-256 of the modulus and the exponent.
function Get-ApprovalFingerprint([byte[]]$modulus, [byte[]]$exponent) {
    $both = New-Object byte[] ($modulus.Length + $exponent.Length)
    [Array]::Copy($modulus, 0, $both, 0, $modulus.Length)
    [Array]::Copy($exponent, 0, $both, $modulus.Length, $exponent.Length)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $hash = $sha.ComputeHash($both) } finally { $sha.Dispose() }
    return ((($hash | ForEach-Object { $_.ToString('x2') }) -join '')).Substring(0, 16)
}

# A fingerprint as it is said and printed: in groups of four.
function Get-SpokenFingerprint([string]$fingerprint) {
    return (($fingerprint -split '(.{4})' | Where-Object { $_ }) -join ' ')
}

# The public half of the key file beside the lists (approval-key.json, the one the maintainer's passphrase opens), as
# Get-PinnedApprovalKey gives the pinned one; $null when there is none or it does not read.
function Get-ApprovalKeyBesideLists([string]$management) {
    $path = Join-Path $management 'approval-key.json'
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    try {
        $json = [IO.File]::ReadAllText($path) | ConvertFrom-Json
        $modulus = [Convert]::FromBase64String([string]$json.modulus)
        $exponent = [Convert]::FromBase64String([string]$json.exponent)
    } catch {
        return $null
    }
    return [pscustomobject]@{ Modulus = $modulus; Exponent = $exponent; Fingerprint = (Get-ApprovalFingerprint $modulus $exponent) }
}

# git asked in a repository, its lines given, or $null when git fails (a path a commit does not hold, no repository,
# no git). Its error text is dropped here: under 'Stop', Windows PowerShell makes a native program's error line end
# the script (the first pin, 2026-11-21).
function Invoke-ApprovalGit([string]$repositoryRoot, [string[]]$arguments) {
    $ErrorActionPreference = 'Continue'
    try {
        $out = @(& git -C $repositoryRoot @arguments 2>$null)
    } catch {
        return $null
    }
    if ($LASTEXITCODE -ne 0) { return $null }
    return , $out
}

# Every key the code repository pinned, oldest first, from git's history of docs\axKit\approval-key.pub: Key, Commit
# (short) and Signed, whether the commit that replaced the key before it carries that key's signature of the change
# (OR-6.3; the first is signed by nothing and counts as signed). Empty where there is no history to read.
function Get-ApprovalKeyHistory([string]$repositoryRoot) {
    $history = @()
    $path = 'docs/axKit/approval-key.pub'
    $commits = Invoke-ApprovalGit $repositoryRoot @('log', '--reverse', '--format=%H', '--', $path)
    if ($null -eq $commits) { return $history }
    $previous = $null
    foreach ($commit in $commits) {
        $text = Invoke-ApprovalGit $repositoryRoot @('show', "${commit}:$path")
        $key = if ($null -ne $text) { Read-PinnedApprovalKeyText ($text -join "`n") } else { $null }
        if ($null -eq $key) { continue }
        if ($null -ne $previous -and $previous.Fingerprint -eq $key.Fingerprint) { continue }
        $signed = $true
        if ($null -ne $previous) {
            $message = Invoke-ApprovalGit $repositoryRoot @('show', '-s', '--format=%B', $commit)
            $signatures = @($message | Where-Object { $_ -match '^\s*Approval key changed:\s*(\S+)\s*$' } | ForEach-Object { $Matches[1] })
            $signed = $signatures.Count -eq 1 -and (Test-ApprovalSignature (Get-ApprovalChangeText $key.Fingerprint) $signatures[0] $previous)
        }
        $history += [pscustomobject]@{ Key = $key; Commit = $commit.Substring(0, 8); Signed = $signed }
        $previous = $key
    }
    return $history
}

# The first character of a text that a screen reader does not speak, as "U+200B, character 57", or $null: a control
# character other than a line end or a tab, a zero-width or direction character, a separator, a character of private
# use or one that is none. AxTask.Core's OutsideReports.Unheard refuses to sign such an entry (OR-5.6); the entry is
# checked here already when it is made ready.
function Get-UnheardCharacter([string]$text) {
    if ([string]::IsNullOrEmpty($text)) { return $null }
    $unheard = @('Control', 'Format', 'LineSeparator', 'ParagraphSeparator', 'PrivateUse', 'OtherNotAssigned', 'Surrogate')
    $i = 0
    while ($i -lt $text.Length) {
        $one = $text[$i]
        $pair = [char]::IsHighSurrogate($one) -and ($i + 1) -lt $text.Length -and [char]::IsLowSurrogate($text[$i + 1])
        if ($one -ne [char]13 -and $one -ne [char]10 -and $one -ne [char]9) {
            $category = [Globalization.CharUnicodeInfo]::GetUnicodeCategory($text, $i).ToString()
            if ($unheard -contains $category) {
                $code = if ($pair) { [char]::ConvertToUtf32($text, $i) } else { [int]$one }
                return ('U+{0:X4}, character {1}' -f $code, ($i + 1))
            }
        }
        $i += $(if ($pair) { 2 } else { 1 })
    }
    return $null
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
        foreach ($number in [regex]::Matches($match.Groups[1].Value, '\d+')) { $numbers += [int]$number.Value }
    }
    return $numbers
}

# Every entry of the outside reports list: Path, Fields, Reports (numbers), State, Valid, whether an Approved, Declined
# or Done entry's signature verifies against the pinned key for its State's decision, and ValidEver, whether it
# verifies against the pinned key or one of the keys pinned before it ($formerKeys, from Get-ApprovalKeyHistory): an
# entry closed under an earlier key stays what the maintainer signed, and only Valid lets work be done (OR-6.5).
function Get-OutsideEntries([string]$listFolder, $key, $formerKeys = @()) {
    $entries = @()
    if (-not (Test-Path -LiteralPath $listFolder)) { return $entries }
    foreach ($file in @(Get-ChildItem -LiteralPath $listFolder -Filter *.json -File | Where-Object { $_.Name -ne 'list.json' })) {
        $fields = Read-ApprovalItemFields $file.FullName
        if ($null -eq $fields) { continue }
        $state = [string]$fields['State']
        $valid = $false
        $validEver = $false
        if ($state -eq 'Approved' -or $state -eq 'Declined' -or $state -eq 'Done') {
            $decision = if ($state -eq 'Declined') { 'declined' } else { 'approved' }
            $text = Get-ApprovalText $fields $decision
            $valid = Test-ApprovalSignature $text $fields['Approval'] $key
            $validEver = $valid
            foreach ($former in @($formerKeys)) {
                if (-not $validEver) { $validEver = Test-ApprovalSignature $text $fields['Approval'] $former }
            }
        }
        $entries += [pscustomobject]@{
            Path = $file.FullName
            Fields = $fields
            Reports = @(Get-ReportNumbers $fields['Report'])
            State = $state
            Valid = $valid
            ValidEver = $validEver
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
