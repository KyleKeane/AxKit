<#
.SYNOPSIS
  Builds axKit and starts axClaude: run.ps1 with -App claude. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axClaude.ps1                       # the current folder
  .\run-axClaude.ps1 C:\src\myproject -New # a new conversation in that folder
  .\run-axClaude.ps1 -NoBuild              # skip the build
#>
& "$PSScriptRoot\run.ps1" -App claude @args
exit $LASTEXITCODE
