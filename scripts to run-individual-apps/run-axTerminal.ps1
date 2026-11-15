<#
.SYNOPSIS
  Builds axKit and starts axTerminal: run.ps1 with -App terminal. Every other argument goes through unchanged. Without
  a folder the shell starts in the current folder.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axTerminal.ps1'                      # a shell in the current folder
  & '.\scripts to run-individual-apps\run-axTerminal.ps1' C:\src\myproject     # a shell in that folder
  & '.\scripts to run-individual-apps\run-axTerminal.ps1' -NoBuild             # without building
#>
& "$PSScriptRoot\..\run.ps1" -App terminal @args
exit $LASTEXITCODE
