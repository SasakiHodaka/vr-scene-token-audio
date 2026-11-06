param(
    [string]$ProjectPath = 'C:\Users\acd-pc67\Downloads\fusion-vr-training-2.0.6',
    [string]$BackupRoot = (Join-Path $env:USERPROFILE 'SceneTokenBackups')
)
$ErrorActionPreference = 'Stop'
$backupProject = (Resolve-Path -LiteralPath $ProjectPath).Path
$backupScene = Join-Path $backupProject 'Assets\Training\Scene\Drone Training Capture.unity'
$backupModule = Join-Path $backupProject 'Assets\SceneTokenResearchIntegration'
if (-not (Test-Path -LiteralPath $backupScene) -or -not (Test-Path -LiteralPath $backupModule)) {
    throw 'Capture scene and installed module are required'
}
$backupDestination = Join-Path $BackupRoot ((Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $backupDestination -Force | Out-Null
$backupSources = @{
    'Drone Training Capture.unity' = $backupScene
    'Drone Training Capture.unity.meta' = $backupScene + '.meta'
    'EditorBuildSettings.asset' = Join-Path $backupProject 'ProjectSettings\EditorBuildSettings.asset'
}
foreach ($backupName in $backupSources.Keys) {
    Copy-Item -LiteralPath $backupSources[$backupName] -Destination (Join-Path $backupDestination $backupName)
    if ((Get-FileHash -LiteralPath $backupSources[$backupName]).Hash -ne (Get-FileHash -LiteralPath (Join-Path $backupDestination $backupName)).Hash) {
        throw "Backup mismatch: $backupName"
    }
}
Copy-Item -LiteralPath $backupModule -Destination (Join-Path $backupDestination 'SceneTokenResearchIntegration') -Recurse
$backupManifest = foreach ($backupFile in Get-ChildItem -LiteralPath $backupDestination -File -Recurse) {
    [pscustomobject]@{ Path = $backupFile.FullName.Substring($backupDestination.Length).TrimStart('\'); SHA256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $backupFile.FullName).Hash }
}
foreach ($backupFile in Get-ChildItem -LiteralPath $backupModule -File -Recurse) {
    $backupRelative = $backupFile.FullName.Substring($backupModule.Length).TrimStart('\')
    $backupCopy = Join-Path (Join-Path $backupDestination 'SceneTokenResearchIntegration') $backupRelative
    if ((Get-FileHash -LiteralPath $backupFile.FullName).Hash -ne (Get-FileHash -LiteralPath $backupCopy).Hash) { throw "Module backup mismatch: $backupRelative" }
}
$backupManifest | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $backupDestination 'sha256-manifest.json')
Set-Content -Encoding UTF8 -LiteralPath (Join-Path $backupDestination 'README.txt') -Value @(
    'Disk snapshot of the capture scene, installed research module and build scene list.',
    'No Unity memory/unsaved changes are captured. No Photon credentials or SDK assets are included.',
    'This backup is local only. Research source code and instructions are separately versioned on GitHub.',
    ('Source project: ' + $backupProject)
)
Write-Output "Verified local backup: $backupDestination"
Write-Output "Files checksummed: $($backupManifest.Count)"
