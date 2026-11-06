param(
    [string]$ProjectPath = 'C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6'
)
$ErrorActionPreference = 'Stop'
$resumeRoot = Split-Path -Parent $PSScriptRoot
$resumeChecks = [System.Collections.Generic.List[object]]::new()
function Add-ResumeCheck([string]$Name, [bool]$Passed, [string]$Detail) {
    $resumeChecks.Add([pscustomobject]@{ Check = $Name; Passed = $Passed; Detail = $Detail })
}
$resumeBranch = git -C $resumeRoot branch --show-current
$resumeDirty = @(git -C $resumeRoot status --porcelain)
Add-ResumeCheck 'Research branch' ($resumeBranch -eq 'main') $resumeBranch
Add-ResumeCheck 'Research changes saved' ($resumeDirty.Count -eq 0) "$($resumeDirty.Count) uncommitted paths"
$resumeScene = Join-Path $ProjectPath 'Assets\Training\Scene\Drone Training Capture.unity'
Add-ResumeCheck 'Capture scene exists' (Test-Path -LiteralPath $resumeScene) $resumeScene
$resumeModule = Join-Path $ProjectPath 'Assets\SceneTokenResearchIntegration'
$resumeSource = Join-Path $resumeRoot 'Assets\Scripts\SceneTokenAudio'
$resumeDifferences = [System.Collections.Generic.List[string]]::new()
foreach ($resumeFile in Get-ChildItem -LiteralPath $resumeSource -File -Recurse) {
    if ($resumeFile.Extension -notin @('.cs', '.asmdef', '.meta')) { continue }
    $resumeRelative = $resumeFile.FullName.Substring($resumeSource.Length).TrimStart('\')
    $resumeInstalled = Join-Path (Join-Path $resumeModule 'Runtime') $resumeRelative
    if (-not (Test-Path -LiteralPath $resumeInstalled)) { $resumeDifferences.Add($resumeRelative); continue }
    $resumeExpected = (Get-Content -Raw -Encoding UTF8 -LiteralPath $resumeFile.FullName).Replace("`r`n", "`n")
    $resumeActual = (Get-Content -Raw -Encoding UTF8 -LiteralPath $resumeInstalled).Replace("`r`n", "`n")
    if ($resumeExpected -cne $resumeActual) { $resumeDifferences.Add($resumeRelative) }
}
Add-ResumeCheck 'Installed runtime matches source' ($resumeDifferences.Count -eq 0) "$($resumeDifferences.Count) differences (line endings normalized)"
foreach ($resumeRelative in $resumeDifferences) { Write-Output "DIFFERENCE: $resumeRelative" }
$resumeOriginal = Join-Path $ProjectPath 'Assets\Training\Scene\Drone Training.unity'
$resumeOriginalHash = Join-Path $resumeModule 'original-scene.sha256.txt'
$resumeOriginalOk = $false
if ((Test-Path -LiteralPath $resumeOriginal) -and (Test-Path -LiteralPath $resumeOriginalHash)) {
    $resumeOriginalOk = (Get-FileHash -Algorithm SHA256 -LiteralPath $resumeOriginal).Hash -eq (Get-Content -Raw -LiteralPath $resumeOriginalHash).Trim()
}
Add-ResumeCheck 'Original scene unchanged' $resumeOriginalOk 'Compared with installer baseline'
$resumeSetup = Join-Path $resumeModule 'Editor\AssemblyCaptureSceneSetup.cs'
$resumeSetupOk = (Test-Path -LiteralPath $resumeSetup) -and (
    (Get-Content -Raw -Encoding UTF8 -LiteralPath $resumeSetup).Replace("`r`n", "`n") -ceq
    (Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $resumeRoot 'Assets\Editor\AssemblyCaptureSceneSetup.cs')).Replace("`r`n", "`n"))
Add-ResumeCheck 'Installed scene setup matches source' $resumeSetupOk $resumeSetup
$resumeChecks | Format-Table -AutoSize -Wrap
$resumeCaptures = Join-Path $env:USERPROFILE 'AppData\LocalLow\Photon\FusionTraining Unity6\SceneTokenAssemblyCaptures'
$resumeCsvFiles = @()
if (Test-Path -LiteralPath $resumeCaptures) {
    $resumeCsvFiles = @(Get-ChildItem -LiteralPath $resumeCaptures -Filter events-audio.csv -File -Recurse)
}
Write-Output "Capture CSV files found: $($resumeCsvFiles.Count) (contents and human-operation provenance are not evaluated here)"
foreach ($resumeCsv in $resumeCsvFiles) { Write-Output $resumeCsv.FullName }
if (@($resumeChecks | Where-Object { -not $_.Passed }).Count -gt 0) { exit 1 }
Write-Output 'READY: saved files and installation checks passed. This is not a Unity runtime/VR test.'
