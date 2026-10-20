param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$researchRoot = Split-Path -Parent $PSScriptRoot
$researchLogs = Join-Path $researchRoot 'Logs'
New-Item -ItemType Directory -Path $researchLogs -Force | Out-Null
foreach ($testPlatform in @('EditMode', 'PlayMode')) {
    $researchResult = Join-Path $researchLogs ($testPlatform.ToLowerInvariant() + '-results.xml')
    $researchLog = Join-Path $researchLogs ($testPlatform.ToLowerInvariant() + '.log')
    if (Test-Path -LiteralPath $researchResult) { Remove-Item -LiteralPath $researchResult }
    $researchArguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $researchRoot + '"'),
        '-runTests', '-testPlatform', $testPlatform, '-testResults', ('"' + $researchResult + '"'), '-logFile', ('"' + $researchLog + '"'))
    $researchProcess = Start-Process -FilePath $UnityEditor -ArgumentList $researchArguments -WindowStyle Hidden -Wait -PassThru
    if ($researchProcess.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $researchResult)) {
        throw "$testPlatform failed to produce successful results. See $researchLog"
    }
    [xml]$researchReport = Get-Content -LiteralPath $researchResult -Raw
    $researchRun = $researchReport.'test-run'
    if ($researchRun.result -ne 'Passed' -or [int]$researchRun.failed -ne 0 -or [int]$researchRun.total -eq 0) {
        throw "$testPlatform tests failed. See $researchResult"
    }
    Write-Output "$testPlatform : $($researchRun.passed)/$($researchRun.total) passed"
}
