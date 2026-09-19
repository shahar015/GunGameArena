param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content "$root\thunderstore\manifest.json" | ConvertFrom-Json
$version = $manifest.version_number
$bin = "$root\src\GunGameArena\bin\$Configuration"
$stage = "$root\dist\stage"
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force "$stage\plugins\GunGameArena" | Out-Null
Copy-Item "$bin\GunGameArena.dll", "$bin\GunGameArena.Core.dll" "$stage\plugins\GunGameArena\"
Copy-Item "$root\thunderstore\manifest.json", "$root\thunderstore\README.md", "$root\thunderstore\CHANGELOG.md", "$root\thunderstore\icon.png" $stage
$zip = "$root\dist\GunGameArena-$version.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path "$stage\*" -DestinationPath $zip
Remove-Item -Recurse -Force $stage
Write-Host "Packed $zip"
