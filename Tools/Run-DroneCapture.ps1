param(
    [string]$ProjectPath = 'C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6',
    [ValidateSet('Prepare','Verify','Test')][string]$Operation = 'Verify',
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Unity.exe'
)
$ErrorActionPreference = 'Stop'
$captureProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$captureLogRoot = Join-Path $captureProject 'Logs\assembly-capture'
New-Item -ItemType Directory -Path $captureLogRoot -Force | Out-Null
$captureLog = Join-Path $captureLogRoot ($Operation.ToLowerInvariant() + '.log')
$captureArguments = @('-batchmode','-nographics','-projectPath',('"' + $captureProject + '"'),'-logFile',('"' + $captureLog + '"'))
if ($Operation -eq 'Test') {
    $captureResults = Join-Path $captureLogRoot 'playmode-results.xml'
    if (Test-Path -LiteralPath $captureResults) { Remove-Item -LiteralPath $captureResults }
    $captureArguments += @('-runTests','-testPlatform','PlayMode','-testFilter','SceneTokenAudio.Tests.AssemblyCaptureTests','-testResults',('"' + $captureResults + '"'))
} else {
    $captureArguments += @('-quit','-executeMethod',('AssemblyCaptureSceneSetup.' + $Operation))
}
$captureProcess = Start-Process -FilePath $UnityEditor -ArgumentList $captureArguments -WindowStyle Hidden -Wait -PassThru
if ($captureProcess.ExitCode -ne 0) { throw "Unity failed. See $captureLog" }
if ($Operation -eq 'Test') {
    if (-not (Test-Path -LiteralPath $captureResults)) { throw "No test results. See $captureLog" }
    [xml]$captureReport = Get-Content -Raw -LiteralPath $captureResults
    if ($captureReport.'test-run'.result -ne 'Passed' -or [int]$captureReport.'test-run'.total -lt 2) { throw "Capture tests failed. See $captureResults" }
    Write-Output "Capture PlayMode: $($captureReport.'test-run'.passed)/$($captureReport.'test-run'.total) passed"
} else { Write-Output "$Operation completed. See $captureLogRoot" }
