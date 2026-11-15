<#
.SYNOPSIS
  Builds axKit and starts axHelp: run.ps1 with -App help. Every other argument goes through unchanged.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axHelp.ps1'                          # build and start axHelp
  & '.\scripts to run-individual-apps\run-axHelp.ps1' -NoBuild                 # without building
#>
& "$PSScriptRoot\..\run.ps1" -App help @args
exit $LASTEXITCODE
