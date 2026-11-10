<#
.SYNOPSIS
  Builds axKit and starts axTask: run.ps1 with -App task. Every other argument goes through unchanged.

.EXAMPLE
  .\run-axTask.ps1 manual_tests\axTask\tasks   # build and start axTask on the test folder
  .\run-axTask.ps1 -NoBuild                    # without building
#>
& "$PSScriptRoot\run.ps1" -App task @args
exit $LASTEXITCODE
