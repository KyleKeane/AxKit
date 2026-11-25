<#
.SYNOPSIS
  The checks that the assistant's own edits failed most, in about a minute: the quick checks of the pages, then only
  the tests of notes, documents, keys, menu marks, the changelog and the bundle's rules, from one build of AxKit.Tests.

.DESCRIPTION
  The pitfall this script removes (the review of the sessions of 2026-11-25): about 110 failures of the full test run
  came from the edit and not from the code (a note naming a member that a change removed or renamed, a notes file
  grown past its size, two windows given one key, a menu item that acts outside without its mark, a changelog line in
  the wrong form, a generated page not written again), each found only after the minutes of every test project. Run
  this after an edit of notes, documents, key tables or menus, before .\test.ps1; it does not replace it. It builds
  AxKit.Tests (Release) and runs the test classes named below; the build servers are shut down after (the
  development computer has 7 GB). Exit code 0 when every check passed. The script stays ASCII.

.EXAMPLE
  .\tools\doc-checks.ps1
#>
param()

$ErrorActionPreference = 'Continue'
$root = Split-Path -Parent $PSScriptRoot

# The classes, by what they hold: the notes, the documents and the help texts, the keys, the menu marks, the
# changelog, the plain code and the layout of the projects, personal data.
$classes = @(
    'NotesTests', 'AiDocumentationTests', 'AiCautionWordsTests', 'BundleHelpTests', 'TextDocumentTests', 'ItemDocumentTests',
    'KeyTests', 'KeyReferenceTests', 'AppLaunchKeysTests', 'HandKeyTests', 'CommandMarkTests',
    'ChangelogTests', 'PlainCodeTests', 'LayoutTests', 'StackTests', 'PrivacyTests'
)

Write-Host 'The quick checks of the pages...'
& (Join-Path $root 'tools\pages.ps1') -Check
$pages = $LASTEXITCODE

$filter = ($classes | ForEach-Object { "FullyQualifiedName~AxKit.Tests.$_" }) -join '|'
Write-Host "The tests of notes, documents, keys and rules ($($classes.Count) classes)..."
$lines = @(& dotnet test (Join-Path $root 'tests\AxKit.Tests') -c Release --nologo --filter $filter 2>&1 | ForEach-Object { "$_" } | & (Join-Path $PSScriptRoot 'redact.ps1'))
$tests = $LASTEXITCODE
& dotnet build-server shutdown | Out-Null
$lines | Where-Object { $_ -match '(^\s+Failed |error CS|Assert\.|^\s+(Expected|Actual)|Passed!|Failed!|Total tests)' } | Select-Object -First 60 | ForEach-Object { $_ }

if ($pages -ne 0 -or $tests -ne 0) {
    Write-Host 'doc-checks: a check failed (above); the full run is .\test.ps1.'
    exit 1
}
Write-Host 'doc-checks: the pages, the notes, the documents, the keys and the rules hold.'
exit 0
