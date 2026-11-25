<#
.SYNOPSIS
  A filter for what a command prints, before it reaches a session: the profile folder, the account's name and e-mail
  addresses replaced (root CLAUDE.md rule 6).

.DESCRIPTION
  The pitfall this script removes (the review of the sessions of 2026-11-25): a log, a run's output or a page's text
  printed into the session held the account path (C:\Users\<name>\...) or an e-mail address, and each command had to
  replace them by hand, which was forgotten more than once. Pipe the output through it: every line comes out with
  the profile folder as %USERPROFILE%, the account name as <user> (whole words only) and an e-mail address as
  <email>. It reads its input as text lines and writes text lines. The script stays ASCII.

.EXAMPLE
  Get-Content $log | .\tools\redact.ps1
  dotnet run --project tools/AxAutomate -c Release --no-build -- tests\scripts\axTask 2>&1 | .\tools\redact.ps1
#>
[CmdletBinding()]
param([Parameter(ValueFromPipeline = $true)][AllowEmptyString()][object]$Line)

begin {
    $profileFolder = [regex]::Escape($env:USERPROFILE)
    $account = if ($env:USERNAME) { '\b' + [regex]::Escape($env:USERNAME) + '\b' } else { $null }
    $mail = '[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}'
}

process {
    $text = "$Line"
    $text = [regex]::Replace($text, $profileFolder, '%USERPROFILE%', 'IgnoreCase')
    # The profile folder with forward slashes, as git and some tools print it.
    $text = [regex]::Replace($text, [regex]::Escape($env:USERPROFILE.Replace('\', '/')), '%USERPROFILE%', 'IgnoreCase')
    $text = [regex]::Replace($text, $mail, '<email>')
    if ($account) { $text = [regex]::Replace($text, $account, '<user>', 'IgnoreCase') }
    $text
}
