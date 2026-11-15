<#
.SYNOPSIS
  Builds axKit and starts axCalc: run.ps1 with -App calc. Every other argument goes through unchanged.

.EXAMPLE
  .\run-individual-apps\run-axCalc.ps1                          # build and start axCalc
  .\run-individual-apps\run-axCalc.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\..\run.ps1" -App calc @args
exit $LASTEXITCODE
