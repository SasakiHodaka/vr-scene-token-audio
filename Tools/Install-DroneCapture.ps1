param(
    [string]$ProjectPath = 'C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6'
)
$ErrorActionPreference = 'Stop'
$captureSourceRoot = Split-Path -Parent $PSScriptRoot
$captureTargetRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$captureOriginalScene = Join-Path $captureTargetRoot 'Assets\Training\Scene\Drone Training.unity'
if (-not (Test-Path -LiteralPath $captureOriginalScene)) { throw 'Drone Training.unity not found' }
$captureTarget = Join-Path $captureTargetRoot 'Assets\SceneTokenResearchIntegration'
if ((Test-Path -LiteralPath $captureTarget) -and -not (Test-Path -LiteralPath (Join-Path $captureTarget 'installed-by-scene-token.txt'))) {
    throw 'Integration folder exists without installer marker; refusing to overwrite'
}
New-Item -ItemType Directory -Path $captureTarget -Force | Out-Null
Set-Content -LiteralPath (Join-Path $captureTarget 'installed-by-scene-token.txt') -Value 'Research capture module. Original SDK and original scene are not replaced.' -Encoding UTF8
$captureRuntime = Join-Path $captureTarget 'Runtime'
New-Item -ItemType Directory -Path $captureRuntime -Force | Out-Null
foreach ($captureSourceItem in Get-ChildItem -LiteralPath (Join-Path $captureSourceRoot 'Assets\Scripts\SceneTokenAudio') -Force) {
    Copy-Item -LiteralPath $captureSourceItem.FullName -Destination $captureRuntime -Recurse -Force
}
$captureEditor = Join-Path $captureTarget 'Editor'
New-Item -ItemType Directory -Path $captureEditor -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $captureSourceRoot 'Assets\Editor\AssemblyCaptureSceneSetup.cs') -Destination $captureEditor -Force
$captureTests = Join-Path $captureTarget 'Tests\PlayMode'
New-Item -ItemType Directory -Path $captureTests -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $captureSourceRoot 'Assets\Tests\PlayMode\AssemblyCaptureTests.cs') -Destination $captureTests -Force
Set-Content -LiteralPath (Join-Path $captureTests 'SceneTokenAssemblyCapture.Tests.asmdef') -Encoding UTF8 -Value '{"name":"SceneTokenAssemblyCapture.Tests","references":["SceneTokenAudio"],"optionalUnityReferences":["TestAssemblies"]}'
$captureOriginalHash = (Get-FileHash -LiteralPath $captureOriginalScene -Algorithm SHA256).Hash
$captureHashPath = Join-Path $captureTarget 'original-scene.sha256.txt'
if (Test-Path -LiteralPath $captureHashPath) {
    if ((Get-Content -Raw -LiteralPath $captureHashPath).Trim() -ne $captureOriginalHash) {
        throw 'Original scene changed since first install; inspect before continuing'
    }
} else { Set-Content -LiteralPath $captureHashPath -Encoding UTF8 -Value $captureOriginalHash }
Write-Output "Installed research module: $captureTarget"
Write-Output "Original scene SHA256: $captureOriginalHash"
