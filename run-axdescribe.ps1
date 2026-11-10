<#
.SYNOPSIS
  Builds axKit and starts axDescribe: run.ps1 with -App describe. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axdescribe.ps1                          # build and start axDescribe
  .\run-axdescribe.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App describe @args
exit $LASTEXITCODE
