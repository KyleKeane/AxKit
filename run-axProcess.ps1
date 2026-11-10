<#
.SYNOPSIS
  Builds axKit and starts axProcess: run.ps1 with -App process. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axProcess.ps1                          # build and start axProcess
  .\run-axProcess.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App process @args
exit $LASTEXITCODE
