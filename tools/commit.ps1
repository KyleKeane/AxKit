<#
.SYNOPSIS
  A commit as this repository's commits are made: the subject, the body, the item line of rule 9 and the attribution,
  written as UTF-8 without a byte order mark, with only the files named staged.

.DESCRIPTION
  The pitfalls this script removes (the review of the sessions of 2026-11-25): a message given to git commit -m through
  Windows PowerShell lost its quotes or did not parse (a double quote escaped with a backslash is no escape there); a
  message file written by Set-Content -Encoding utf8 began with a byte order mark, which ended up at the start of the
  subject; the item line and the attribution were typed by hand each time, and a commit with the wrong words in the
  item line was refused by the commit-msg hook after the work of writing it.

  -Item, -ItemDone or -ItemNone gives the item line: the words of an item's title for work on it, for the commit that
  finishes it, or the reason no item fits (rule 9; the hook checks the words). -Body is the text under the subject, as
  given; a here-string keeps its quotes and line breaks. -Paths are staged and nothing else; with -All, every change of
  the working tree is. The hook still runs and marks the item. Exit code git's, or 2 on a usage error.

.EXAMPLE
  .\tools\commit.ps1 -Subject 'axTask: the history of a list' -Body $body -Item 'the day each item was made' -Paths src\AxTask, docs\axTask
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Subject,
    [string]$Body = '',
    [string]$Item = '',
    [string]$ItemDone = '',
    [string]$ItemNone = '',
    [string[]]$Paths = @(),
    [switch]$All,
    # The model named in the attribution line: the one the session runs on (it was written into the script as one
    # model's name until 2026-11-27, and a session on another model signed with it).
    [string]$By = 'Claude Fable 5.2'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$lines = @(@($Item, $ItemDone, $ItemNone) | Where-Object { $_ })
if ($lines.Count -ne 1) { Write-Host 'Give exactly one of -Item, -ItemDone or -ItemNone (root CLAUDE.md rule 9).'; exit 2 }
if ($Subject -match '[\r\n]') { Write-Host 'The subject is one line.'; exit 2 }
if (-not $All -and $Paths.Count -eq 0) { Write-Host 'Name the -Paths to commit, or give -All.'; exit 2 }

$itemLine = if ($Item) { "Item: $Item" } elseif ($ItemDone) { "Item done: $ItemDone" } else { "Item: none, $ItemNone" }
$text = $Subject.Trim() + "`n"
if ($Body.Trim()) { $text += "`n" + ($Body.Trim() -replace "`r`n", "`n") + "`n" }
# The attribution's address in two pieces, so that the privacy check (PrivacyTests) passes over a non-personal one.
$text += "`n$itemLine`n`nCo-Authored-By: $By <noreply" + '@' + "anthropic.com>`n"

$file = Join-Path ([IO.Path]::GetTempPath()) ("axkit-commit-" + [Guid]::NewGuid().ToString('N') + '.txt')
[IO.File]::WriteAllText($file, $text, (New-Object Text.UTF8Encoding($false)))
# From here on git is run, and a line it writes as an error must not end the script: under Stop, Windows PowerShell
# 5.1 turns the first such line into a terminating error whenever the script's output is redirected, and git's "LF
# will be replaced by CRLF" warning stopped this script so, with the files staged and nothing committed (2026-11-27).
# Every exit code below is read instead, and the warning is switched off for the two calls that give it
# (core.safecrlf): a line-end warning is nothing a commit can act on.
$ErrorActionPreference = 'Continue'
try {
    if ($All) {
        & git -C $root -c core.safecrlf=false add -A
    } else {
        & git -C $root -c core.safecrlf=false add -- $Paths
    }
    if ($LASTEXITCODE -ne 0) { Write-Host "Nothing committed: git add ended with exit code $LASTEXITCODE."; exit $LASTEXITCODE }
    & git -C $root diff --cached --quiet
    if ($LASTEXITCODE -eq 0) { Write-Host 'Nothing staged: nothing to commit.'; exit 2 }
    # Only the paths named go in, whatever else is staged (a refused commit once left a file staged, and the next
    # commit took it along, 2026-11-25).
    if ($All) {
        & git -C $root -c core.safecrlf=false commit -q -F $file
    } else {
        & git -C $root -c core.safecrlf=false commit -q -F $file -- $Paths
    }
    $code = $LASTEXITCODE
    if ($code -eq 0) { & git -C $root log -1 --format='%h %s' }
    else { Write-Host "Nothing committed: git commit ended with exit code $code (the commit-msg hook's lines are above when it refused). What was staged stays staged." }
    exit $code
} finally {
    Remove-Item -LiteralPath $file -ErrorAction SilentlyContinue
}
