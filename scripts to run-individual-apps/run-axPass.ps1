<#
.SYNOPSIS
  Builds axKit and starts axPass: run.ps1 with -App pass. Every other argument goes through unchanged.

.EXAMPLE
  & '.\scripts to run-individual-apps\run-axPass.ps1' manual_tests\axPass\test.kdbx   # build and start axPass on the test database
  & '.\scripts to run-individual-apps\run-axPass.ps1' -NoBuild                        # without building
#>
& "$PSScriptRoot\..\run.ps1" -App pass @args
exit $LASTEXITCODE
