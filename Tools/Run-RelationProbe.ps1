param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$relationRoot = Split-Path -Parent $PSScriptRoot
$relationLogs = Join-Path $relationRoot 'Logs'
New-Item -ItemType Directory -Path $relationLogs -Force | Out-Null
$relationOutput = Join-Path $relationLogs 'relation-probe/results.csv'
if (Test-Path -LiteralPath $relationOutput) { Remove-Item -LiteralPath $relationOutput }
$relationLog = Join-Path $relationLogs 'relation-probe-run.log'
$relationArguments = @('-batchmode', '-nographics', '-quit', '-projectPath', ('"' + $relationRoot + '"'),
    '-executeMethod', 'RelationProbeExperiment.RunBatch', '-logFile', ('"' + $relationLog + '"'))
$relationProcess = Start-Process -FilePath $UnityEditor -ArgumentList $relationArguments -WindowStyle Hidden -Wait -PassThru
if ($relationProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $relationOutput)) {
    throw "Relation probe failed. See $relationLog"
}
$relationRows = @(Import-Csv -LiteralPath $relationOutput)
if ($relationRows.Count -ne 960) { throw "Unexpected row count: $($relationRows.Count)" }
Write-Output "Relation probe completed: $($relationRows.Count) rows; $relationOutput"
