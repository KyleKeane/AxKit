<#
.SYNOPSIS
  Pins the maintainer's approval key in this repository (docs/axKit/outside-reports.md OR-6.1, OR-6.3): the one
  command after he has set or changed his approval passphrase in axTask.

.DESCRIPTION
  axTask writes the key beside the lists (..\axkit-management): approval-key.json, its public half approval-key.pub,
  and, for a change, approval-key-change.txt with the line the old key signed. This script copies the public half to
  docs\axKit\approval-key.pub, commits it alone, with that line in the message when there is one (the commit-msg hook
  refuses a change of the pinned key without it), and then deletes approval-key-change.txt, so that axTask allows
  the next change. It never reads the private half and never skips a hook.

  It refuses when other changes are staged (the pin is a commit of its own), when the key beside the lists is the
  pinned one already, and when the public half beside the lists is not the key file's own. Exit code 0 when
  pinned, 1 when refused. The script stays ASCII.

.EXAMPLE
  .\tools\pin-approval-key.ps1 -Trailer 'Co-Authored-By: <the assistant> <address>'
#>
[CmdletBinding()]
param(
    # A last line for the commit message (the assistant's attribution line).
    [string]$Trailer,
    # The folder that holds the lists; <parent>\axkit-management by default (the tests' own).
    [string]$Management,
    # The code repository to pin in; this script's own by default (the tests' own).
    [string]$Repository
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'approvals.ps1')

function Refuse([string]$why) {
    Write-Host "Not pinned: $why"
    exit 1
}

if (-not $Repository) { $Repository = Split-Path $PSScriptRoot -Parent }
if (-not $Management) { $Management = Join-Path (Split-Path $Repository -Parent) 'axkit-management' }

$beside = Get-ApprovalKeyBesideLists $Management
if ($null -eq $beside) { Refuse 'no approval key is set up beside the lists (axTask: Options, Approval passphrase).' }
$publicFile = Join-Path $Management 'approval-key.pub'
$publicText = if (Test-Path -LiteralPath $publicFile) { [IO.File]::ReadAllText($publicFile) } else { '' }
$publicHalf = Read-PinnedApprovalKeyText $publicText
if ($null -eq $publicHalf -or $publicHalf.Fingerprint -ne $beside.Fingerprint) {
    Refuse 'approval-key.pub beside the lists is not the public half of approval-key.json: set the passphrase again in axTask.'
}

$pinned = Get-PinnedApprovalKey $Repository
if ($null -ne $pinned -and $pinned.Fingerprint -eq $beside.Fingerprint) {
    Refuse "the key $(Get-SpokenFingerprint $beside.Fingerprint) is pinned already."
}

$staged = Invoke-ApprovalGit $Repository @('diff', '--cached', '--name-only')
if ($null -eq $staged) { Refuse 'git could not be asked in the repository.' }
if (@($staged | Where-Object { $_ }).Count -gt 0) { Refuse 'other changes are staged: the pin is a commit of its own.' }

$changeFile = Join-Path $Management 'approval-key-change.txt'
$changeLine = ''
if (Test-Path -LiteralPath $changeFile) {
    $changeLine = @(Get-Content -LiteralPath $changeFile | Where-Object { $_ -match '^\s*Approval key changed:\s*\S+\s*$' } | Select-Object -First 1) -join ''
}
if ($null -ne $pinned -and -not $changeLine) {
    Refuse "a key is pinned ($(Get-SpokenFingerprint $pinned.Fingerprint)) and approval-key-change.txt holds no line the old key signed: the passphrase is changed in axTask (Options, Approval passphrase), which writes it."
}

$target = Join-Path $Repository 'docs\axKit\approval-key.pub'
$before = if (Test-Path -LiteralPath $target) { [IO.File]::ReadAllText($target) } else { $null }
[IO.File]::WriteAllText($target, $publicText)

$was = if ($null -eq $pinned) { 'The first approval key' } else { "It replaces the key $($pinned.Fingerprint), which signed the change" }
$message = @(
    "Pin the maintainer's approval key $($beside.Fingerprint)",
    '',
    "$was (docs/axKit/outside-reports.md OR-6). From this commit on the checks verify",
    'the approvals of outside reports against it.',
    ''
)
if ($changeLine) { $message += $changeLine.Trim(); $message += '' }
$message += 'Item: none, the pin of the approval key the maintainer set in axTask'
if ($Trailer) { $message += ''; $message += $Trailer.Trim() }
$messageFile = Join-Path ([IO.Path]::GetTempPath()) ("axkit-pin-" + [Guid]::NewGuid().ToString('N') + '.txt')
[IO.File]::WriteAllLines($messageFile, $message)

try {
    & git -C $Repository add -- 'docs/axKit/approval-key.pub'
    if ($LASTEXITCODE -ne 0) { Refuse 'git could not stage the key.' }
    & git -C $Repository commit -q -F $messageFile
    if ($LASTEXITCODE -ne 0) {
        # The hook refused, or git did: nothing stays staged or changed.
        $null = Invoke-ApprovalGit $Repository @('reset', '-q', '--', 'docs/axKit/approval-key.pub')
        if ($null -eq $before) { Remove-Item -LiteralPath $target -Force } else { [IO.File]::WriteAllText($target, $before) }
        Refuse 'the commit was refused (see the lines above); the pinned key is as it was.'
    }
} finally {
    Remove-Item -LiteralPath $messageFile -Force -ErrorAction SilentlyContinue
}

if (Test-Path -LiteralPath $changeFile) { Remove-Item -LiteralPath $changeFile -Force }
Write-Host "Pinned: approval key $(Get-SpokenFingerprint $beside.Fingerprint). Push it with the next push."
exit 0
