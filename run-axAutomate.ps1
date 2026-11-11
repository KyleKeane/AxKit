<#
.SYNOPSIS
  Builds axKit and starts axAutomate: run.ps1 with -App automate. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axAutomate.ps1                          # build and start axAutomate
  .\run-axAutomate.ps1 C:\some\path            # build and start axAutomate on a folder
  .\run-axAutomate.ps1 -NoBuild                 # without building
#>
& "$PSScriptRoot\run.ps1" -App automate @args
exit $LASTEXITCODE
