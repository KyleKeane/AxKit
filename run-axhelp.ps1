<#
.SYNOPSIS
  Builds axKit and starts axHelp: run.ps1 with -App help. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axhelp.ps1                          # build and start axHelp
  .\run-axhelp.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App help @args
exit $LASTEXITCODE
