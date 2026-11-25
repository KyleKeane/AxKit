<#
.SYNOPSIS
  A check Claude Code runs before each command of its PowerShell tool (a PreToolUse hook, .claude/settings.json):
  the command is parsed as Windows PowerShell 5.1 will parse it, and a command that does not parse is refused with
  the parser's message and how to write it, before anything of it runs.

.DESCRIPTION
  The pitfall this script removes (the review of the sessions of 2026-11-25): 23 commands of the assistant's did not
  parse ("An empty pipe element is not allowed" after a foreach statement, "Variable reference is not valid" for
  "$name:" in a string, "Missing ')'" after a double quote escaped with a backslash), each found only when the tool
  ran it, sometimes after part of a line had already been written. Claude Code gives the hook the tool call as JSON
  on its input; exit code 2 refuses the call and gives the error output back to the assistant; exit code 0 lets it
  run. Anything this script cannot read lets the command run: the check never stands in the way of work. The script
  stays ASCII.

.EXAMPLE
  '{"tool_name":"PowerShell","tool_input":{"command":"foreach ($a in 1,2) { $a } | Select-Object -First 1"}}' | powershell -NoProfile -File tools\hooks\claude-powershell-check.ps1
#>
$ErrorActionPreference = 'Stop'
try {
    $call = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $command = "$($call.tool_input.command)"
} catch {
    exit 0
}
if (-not $command.Trim()) { exit 0 }

$tokens = $null
$errors = $null
[void][System.Management.Automation.Language.Parser]::ParseInput($command, [ref]$tokens, [ref]$errors)
if (-not $errors -or $errors.Count -eq 0) { exit 0 }

$said = New-Object System.Collections.Generic.List[string]
$said.Add('The command does not parse in Windows PowerShell 5.1, so nothing of it ran (tools/hooks/claude-powershell-check.ps1):')
foreach ($problem in @($errors | Select-Object -First 3)) {
    $at = $problem.Extent.StartOffset
    $near = $command.Substring([Math]::Max(0, $at - 30), [Math]::Min(60, $command.Length - [Math]::Max(0, $at - 30))) -replace '\s+', ' '
    $said.Add("  $($problem.Message) Near: $near")
}
$ids = @($errors | ForEach-Object { $_.ErrorId })
if ($ids -contains 'EmptyPipeElement') { $said.Add('  A foreach, for or if statement cannot be piped or redirected: wrap it, & { foreach (...) { ... } } | ..., or use ForEach-Object.') }
if ($ids -contains 'InvalidVariableReferenceWithDrive') { $said.Add('  "$name:" in a string reads as a drive: write "${name}:".') }
if ($command -match '\\"') { $said.Add('  A backslash does not escape a double quote in PowerShell: use single quotes outside, or a here-string @''...''@ (its closing ''@ at the start of a line), or tools\commit.ps1 for a commit message.') }
$said | ForEach-Object { [Console]::Error.WriteLine($_) }
exit 2
