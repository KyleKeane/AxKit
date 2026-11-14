<#
.SYNOPSIS
The upstream watch: says which outside program (Claude Code, Git, KeePassXC's tool, the shells) has changed its
version since axKit's checks last passed with it, and runs those checks again (-Verify).

.DESCRIPTION
The programs under the access layers update themselves, Claude Code sometimes daily, and a changed output can break
the one place of axKit that reads it (the root CLAUDE.md, rule 12). docs\axKit\upstream.json records, for each
program, where it is looked for, how its version is asked, the version the checks last passed with, the date, and the
checks: test projects and folders or scripts of tests\scripts.

Without an option: one line per program, the installed version and the verified one, "changed" where they differ.
Quick, no build: run it at the start of a session. Exit code 0 when none changed, 1 when one did.

-Verify [<program>]: runs the checks of every changed program, or of the one named (by its id or its name), each
check once: the test projects through test.ps1, the scripts through tools\AxAutomate on its own desktop. Where all of
a program's checks pass, its new version and today's date are written into the file. Where one fails, the file keeps
that program as it was, the failing script and step are printed, and the exit code is 1. Where the checks cannot run
(the program is not installed, Claude Code is not signed in, the network is off, the build failed), the output says
so, nothing is written for that program, and the exit code is 2: that is not a failure of a parser.

-Check: only that the file is well formed: every entry has its fields, the checks it names exist. It never asks an
installed version, since CI's computer has other versions. test.ps1 runs it.

-File <path>: another file than docs\axKit\upstream.json, for the tests and for trying -Verify on a copy.

The script stays ASCII (Windows PowerShell 5.1 reads it as ANSI) and prints no path of the computer.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Program = '',
    [switch]$Verify,
    [switch]$Check,
    [string]$File = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path | Split-Path -Parent
$jsonPath = if ($File -ne '') { $File } else { Join-Path $root 'docs\axKit\upstream.json' }
$shownName = if ($File -ne '') { Split-Path -Leaf $File } else { 'docs/axKit/upstream.json' }

# The first existing place of an entry, or $null: a place with a folder is taken with its environment variables
# filled in, a bare file name is searched in the PATH's full folders only (never the current folder, S-1).
function Find-Place($entry) {
    foreach ($place in @($entry.places)) {
        $expanded = [Environment]::ExpandEnvironmentVariables($place)
        if ($expanded.Contains('%')) { continue }
        if ($expanded.Contains('\')) {
            if (Test-Path -LiteralPath $expanded -PathType Leaf) { return $expanded }
            continue
        }
        foreach ($folder in ($env:PATH -split ';')) {
            if ($folder -eq '' -or -not [IO.Path]::IsPathRooted($folder)) { continue }
            $candidate = Join-Path $folder $expanded
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { return $candidate }
        }
    }
    return $null
}

function Join-Arguments($words) {
    (@($words) | ForEach-Object { if ($_ -eq '' -or $_ -match '\s') { '"' + $_ + '"' } else { $_ } }) -join ' '
}

# Runs a program without a window to its end, its input closed at once (keepassxc-cli waited on an open one,
# 2026-11-14), under a time limit. Gives the exit code and the output, or $null when it did not start or end.
function Invoke-Quietly([string]$path, $words, [int]$seconds) {
    $info = New-Object System.Diagnostics.ProcessStartInfo
    if ($path.EndsWith('.cmd', [StringComparison]::OrdinalIgnoreCase)) {
        # A script (npm's claude.cmd) runs through Windows' own cmd, as AxClaude.Core's ClaudeLauncher starts it.
        $info.FileName = Join-Path $env:SystemRoot 'System32\cmd.exe'
        $info.Arguments = '/d /s /c ""' + $path + '" ' + (Join-Arguments $words) + '"'
    } else {
        $info.FileName = $path
        $info.Arguments = Join-Arguments $words
    }
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardInput = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    try { $process = [System.Diagnostics.Process]::Start($info) } catch { return $null }
    $process.StandardInput.Close()
    $output = $process.StandardOutput.ReadToEndAsync()
    $errors = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($seconds * 1000)) {
        try { $process.Kill() } catch { }
        return $null
    }
    [pscustomobject]@{ ExitCode = $process.ExitCode; Output = $output.Result }
}

# What the program answers to its version question, the first line with text; 'not installed' when it is not
# found, 'no answer' when it did not answer in twenty seconds.
function Get-Installed($entry) {
    $path = Find-Place $entry
    if ($null -eq $path) { return 'not installed' }
    $result = Invoke-Quietly $path $entry.arguments 20
    if ($null -eq $result) { return 'no answer' }
    $line = @($result.Output -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' })
    if ($line.Count -eq 0) { return 'no answer' }
    return $line[0]
}

function Read-Entries {
    if (-not (Test-Path -LiteralPath $jsonPath)) { throw "$shownName is missing." }
    return ([IO.File]::ReadAllText($jsonPath) | ConvertFrom-Json)
}

function ConvertTo-JsonText([string]$text) {
    '"' + $text.Replace('\', '\\').Replace('"', '\"') + '"'
}

function ConvertTo-JsonList($words) {
    '[' + ((@($words) | ForEach-Object { ConvertTo-JsonText $_ }) -join ', ') + ']'
}

# The file written whole, in the one shape it has: ConvertTo-Json of Windows PowerShell would reflow it and write
# its quotes and angle brackets as escapes.
function Write-Entries($document) {
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add('{')
    $lines.Add('  "about": ' + (ConvertTo-JsonText $document.about) + ',')
    $lines.Add('  "programs": [')
    $entries = @($document.programs)
    for ($index = 0; $index -lt $entries.Count; $index++) {
        $entry = $entries[$index]
        $fields = New-Object System.Collections.Generic.List[string]
        $fields.Add('      "id": ' + (ConvertTo-JsonText $entry.id))
        $fields.Add('      "name": ' + (ConvertTo-JsonText $entry.name))
        $fields.Add('      "places": ' + (ConvertTo-JsonList $entry.places))
        $fields.Add('      "arguments": ' + (ConvertTo-JsonList $entry.arguments))
        $fields.Add('      "verified": ' + (ConvertTo-JsonText $entry.verified))
        $fields.Add('      "date": ' + (ConvertTo-JsonText $entry.date))
        $fields.Add('      "tests": ' + (ConvertTo-JsonList $entry.tests))
        $fields.Add('      "scripts": ' + (ConvertTo-JsonList $entry.scripts))
        if ($entry.PSObject.Properties['needs']) { $fields.Add('      "needs": ' + (ConvertTo-JsonText $entry.needs)) }
        if ($entry.PSObject.Properties['signedIn']) { $fields.Add('      "signedIn": ' + (ConvertTo-JsonList $entry.signedIn)) }
        if ($entry.PSObject.Properties['host']) { $fields.Add('      "host": ' + (ConvertTo-JsonText $entry.host)) }
        $lines.Add('    {')
        $lines.Add($fields -join ",`r`n")
        $lines.Add('    }' + $(if ($index -lt $entries.Count - 1) { ',' } else { '' }))
    }
    $lines.Add('  ]')
    $lines.Add('}')
    [IO.File]::WriteAllText($jsonPath, (($lines -join "`r`n") + "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
}

# What is wrong with the file, one line each; nothing when it is well formed.
function Get-Problems($document) {
    $problems = New-Object System.Collections.Generic.List[string]
    $known = 'id', 'name', 'places', 'arguments', 'verified', 'date', 'tests', 'scripts', 'needs', 'signedIn', 'host'
    $seen = @{}
    $entries = @($document.programs)
    if ($entries.Count -eq 0) { $problems.Add('no programs') }
    foreach ($entry in $entries) {
        $label = if ($entry.id) { $entry.id } else { '(an entry without an id)' }
        foreach ($field in 'id', 'name', 'verified', 'date') {
            if (-not ($entry.$field -is [string]) -or $entry.$field.Trim() -eq '') { $problems.Add("${label}: $field is missing") }
        }
        foreach ($field in 'places', 'arguments', 'tests', 'scripts') {
            if (-not $entry.PSObject.Properties[$field] -or -not ($entry.$field -is [array])) { $problems.Add("${label}: $field is not a list") }
        }
        foreach ($property in $entry.PSObject.Properties) {
            if ($known -cnotcontains $property.Name) { $problems.Add("${label}: the field $($property.Name) is not one the script writes back") }
        }
        if ($entry.id) {
            if ($seen.ContainsKey($entry.id.ToLowerInvariant())) { $problems.Add("${label}: the id is there twice") }
            $seen[$entry.id.ToLowerInvariant()] = $true
        }
        if (@($entry.places).Count -eq 0) { $problems.Add("${label}: no place to look for the program") }
        if ($entry.date -and $entry.date -notmatch '^\d{4}-\d{2}-\d{2}$') { $problems.Add("${label}: the date is not yyyy-MM-dd") }
        if (@($entry.tests).Count + @($entry.scripts).Count -eq 0) { $problems.Add("${label}: no check verifies it") }
        foreach ($test in @($entry.tests)) {
            if (-not (Test-Path -LiteralPath (Join-Path $root "tests\$test.Tests") -PathType Container)) { $problems.Add("${label}: no test project tests/$test.Tests") }
        }
        foreach ($scriptName in @($entry.scripts)) {
            if (-not (Test-Path -LiteralPath (Join-Path $root (Join-Path 'tests\scripts' $scriptName)))) { $problems.Add("${label}: no tests/scripts/$scriptName") }
        }
    }
    return $problems
}

# One check, run once in a run of -Verify however many programs name it: 'passed', 'failed' or 'could not run',
# with the lines that say what failed.
$results = @{}
function Invoke-Check([string]$kind, [string]$target) {
    $key = "$kind $target"
    if ($results.ContainsKey($key)) { return $results[$key] }
    $state = 'passed'
    $detail = New-Object System.Collections.Generic.List[string]
    if ($kind -eq 'tests') {
        Write-Host "Running the test project $target..."
        & (Join-Path $root 'test.ps1') $target | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) { $state = 'failed'; $detail.Add("the test project tests/$target.Tests failed (its failing tests are named above)") }
    } else {
        Write-Host "Running the window scripts tests/scripts/$target on a desktop of their own..."
        $current = ''
        $steps = New-Object System.Collections.Generic.List[string]
        try {
            & dotnet run --project (Join-Path $root 'tools\AxAutomate') -c Release -- (Join-Path $root (Join-Path 'tests\scripts' $target)) --build | ForEach-Object {
                Write-Host $_
                if ($_ -match '^== (.+)$') { $current = $Matches[1] }
                elseif ($_ -match '^FAIL') { $steps.Add("$(if ($current) { $current } else { $target }): $_") }
            }
            $code = $LASTEXITCODE
        } finally {
            dotnet build-server shutdown | Out-Null
        }
        if ($code -eq 1) {
            $state = 'failed'
            $detail.Add("tests/scripts/$target failed at:")
            foreach ($step in $steps) { $detail.Add("  $step") }
        } elseif ($code -ne 0) {
            # The runner's own exit code 2: the command line, the build or the program, never a step of a script.
            $state = 'could not run'
            $detail.Add("tests/scripts/$target could not start (the build or the runner, see above): nothing was checked")
        }
    }
    $results[$key] = [pscustomobject]@{ State = $state; Detail = $detail }
    return $results[$key]
}

# Why an entry's checks cannot run now, or $null: the program is gone, Claude Code is signed out, its host does not
# answer. Told apart from a failure, which would be read as a broken parser.
function Get-Obstacle($entry) {
    $path = Find-Place $entry
    if ($null -eq $path) { return 'it is not installed' }
    if ($entry.PSObject.Properties['signedIn']) {
        $answer = Invoke-Quietly $path $entry.signedIn 30
        if ($null -eq $answer -or $answer.ExitCode -ne 0) { return "it is not signed in ($(Split-Path -Leaf $path) $(Join-Arguments $entry.signedIn) did not end with 0)" }
    }
    if ($entry.PSObject.Properties['host']) {
        $client = New-Object System.Net.Sockets.TcpClient
        try {
            $reached = $client.ConnectAsync($entry.host, 443).Wait(5000) -and $client.Connected
        } catch { $reached = $false } finally { $client.Close() }
        if (-not $reached) { return "$($entry.host) does not answer: the network is off or closed to it" }
    }
    return $null
}

$document = Read-Entries
$problems = @(Get-Problems $document)
if ($problems.Count -gt 0) {
    Write-Host "$shownName is not well formed:"
    $problems | ForEach-Object { Write-Host "  $_" }
    exit 1
}
$entries = @($document.programs)

if ($Check) {
    Write-Host "$shownName is current: $($entries.Count) programs."
    exit 0
}

$named = $null
if ($Program -ne '') {
    $named = @($entries | Where-Object { $_.id -eq $Program -or $_.name -eq $Program })
    if ($named.Count -ne 1) {
        Write-Host "No program '$Program' in $shownName. The programs: $(($entries | ForEach-Object { $_.id }) -join ', ')."
        exit 1
    }
}

# The installed versions, asked once.
$installed = @{}
$changed = New-Object System.Collections.Generic.List[object]
foreach ($entry in $(if ($named) { $named } else { $entries })) {
    $installed[$entry.id] = Get-Installed $entry
    $differs = $installed[$entry.id] -cne $entry.verified
    if ($differs) { $changed.Add($entry) }
    Write-Host "$($entry.name): installed $($installed[$entry.id]), verified $($entry.verified) on $($entry.date)$(if ($differs) { ': changed' })"
}

if (-not $Verify) {
    if ($changed.Count -eq 0) {
        Write-Host 'No outside program changed.'
        exit 0
    }
    Write-Host "Changed: $(($changed | ForEach-Object { $_.name }) -join ', '). Run tools\upstream.ps1 -Verify before other work."
    exit 1
}

$chosen = @(if ($named) { $named } else { $changed })
if ($chosen.Count -eq 0) {
    Write-Host 'No outside program changed: nothing to verify.'
    exit 0
}

# The runner and test.ps1 write a failing step to stderr; as in test.ps1, the exit codes are the check.
$ErrorActionPreference = 'Continue'
$failedAny = $false
$blockedAny = $false
$summary = New-Object System.Collections.Generic.List[string]
foreach ($entry in $chosen) {
    Write-Host ''
    Write-Host "Verifying $($entry.name) $($installed[$entry.id])..."
    $obstacle = if ($installed[$entry.id] -eq 'no answer') { 'it did not answer its version question' } else { Get-Obstacle $entry }
    if ($obstacle) {
        $blockedAny = $true
        $summary.Add("$($entry.name): not verified, its checks cannot run: $obstacle.$(if ($entry.PSObject.Properties['needs']) { " They need: $($entry.needs)." }) This says nothing about axKit's parser; $shownName keeps $($entry.verified).")
        continue
    }
    $state = 'passed'
    $detail = New-Object System.Collections.Generic.List[string]
    foreach ($test in @($entry.tests)) {
        $result = Invoke-Check 'tests' $test
        if ($result.State -ne 'passed') { if ($state -ne 'failed') { $state = $result.State }; $detail.AddRange($result.Detail) }
    }
    foreach ($scriptName in @($entry.scripts)) {
        $result = Invoke-Check 'scripts' $scriptName
        if ($result.State -ne 'passed') { if ($state -ne 'failed') { $state = $result.State }; $detail.AddRange($result.Detail) }
    }
    if ($state -eq 'passed') {
        $entry.verified = $installed[$entry.id]
        $entry.date = Get-Date -Format 'yyyy-MM-dd'
        Write-Entries $document
        $summary.Add("$($entry.name): verified with $($entry.verified); written into $shownName with the date $($entry.date).")
    } elseif ($state -eq 'failed') {
        $failedAny = $true
        $summary.Add("$($entry.name): FAILED with $($installed[$entry.id]); $shownName keeps $($entry.verified).")
        foreach ($line in $detail) { $summary.Add("  $line") }
        if ($entry.PSObject.Properties['needs']) {
            $summary.Add("  These checks need: $($entry.needs). Read the failing step before the parser is blamed: a step that waits for a model's words can fail on the wording.")
        }
        $summary.Add("  Fix it in the program's one place (the root CLAUDE.md, rule 12), then run -Verify $($entry.id) again.")
    } else {
        $blockedAny = $true
        $summary.Add("$($entry.name): not verified; $shownName keeps $($entry.verified).")
        foreach ($line in $detail) { $summary.Add("  $line") }
    }
}

Write-Host ''
$summary | ForEach-Object { Write-Host $_ }
if ($failedAny) { exit 1 }
if ($blockedAny) { exit 2 }
exit 0
