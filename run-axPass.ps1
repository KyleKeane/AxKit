<#
.SYNOPSIS
  Builds axKit and starts axPass: run.ps1 with -App pass. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axPass.ps1 manual_tests\axPass\test.kdbx   # build and start axPass on the test database
  .\run-axPass.ps1 -NoBuild                        # without building
#>
& "$PSScriptRoot\run.ps1" -App pass @args
exit $LASTEXITCODE
