<#
.SYNOPSIS
  Builds axKit and starts axClaude: run.ps1 with -App claude. Every other argument goes through unchanged.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axClaude.ps1'                       # the current folder
  & '.\scripts to run-individual-apps\run-axClaude.ps1' C:\src\myproject -New # a new conversation in that folder
  & '.\scripts to run-individual-apps\run-axClaude.ps1' -NoBuild              # skip the build
#>
& "$PSScriptRoot\..\run.ps1" -App claude @args
exit $LASTEXITCODE
