<#
.SYNOPSIS
    Fetches the H3VR game/BepInEx-profile reference assemblies needed to build
    GunGameArena in CI, without a real H3VR install available.

.DESCRIPTION
    Downloads:
      - H3VR.GameLibs (stripped Unity/game reference DLLs) from the BepInEx NuGet feed
      - BepInEx core DLLs, GunGame, Sodalite and Atlas plugin DLLs from Thunderstore

    and lays them out under -GameDir and -ProfileDir exactly as Directory.Build.props /
    GunGameArena.csproj expect (H3VR_DIR / H3VR_PROFILE_DIR).

    Idempotent: safe to run multiple times: every destination file is (re)written with
    -Force, and no download is skipped based on prior local state, so the result is the
    same regardless of how many times this runs. Fails loudly (non-zero exit) and prints
    exactly which expected DLLs are missing if the layout isn't complete afterward.

.PARAMETER GameDir
    Path that will act as the H3VR install directory (H3VR_DIR). Reference assemblies
    are written to "$GameDir\h3vr_Data\Managed".

.PARAMETER ProfileDir
    Path that will act as the BepInEx profile directory (H3VR_PROFILE_DIR). DLLs are
    written to "$ProfileDir\core" and "$ProfileDir\plugins\...".
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameDir,

    [Parameter(Mandatory = $true)]
    [string]$ProfileDir
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$tempRoot = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [System.IO.Path]::GetTempPath() }
$workDir = Join-Path $tempRoot ("ggci-fetch-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $workDir | Out-Null

function Get-RemoteZip {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $zipPath = Join-Path $workDir "$Label.zip"
    Write-Host "Downloading $Label ..."
    Invoke-WebRequest -Uri $Url -OutFile $zipPath -MaximumRedirection 5
    $extractDir = Join-Path $workDir $Label
    if (Test-Path $extractDir) { Remove-Item -Recurse -Force $extractDir }
    Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force
    Remove-Item -Force $zipPath
    return $extractDir
}

function Copy-DllTo {
    param(
        [Parameter(Mandatory = $true)][string]$SourceFile,
        [Parameter(Mandatory = $true)][string]$DestinationDir,
        [string]$DestinationName
    )
    if (-not (Test-Path $SourceFile)) {
        throw "Expected source file not found: $SourceFile"
    }
    New-Item -ItemType Directory -Force -Path $DestinationDir | Out-Null
    $destName = if ($DestinationName) { $DestinationName } else { Split-Path -Leaf $SourceFile }
    Copy-Item -Path $SourceFile -Destination (Join-Path $DestinationDir $destName) -Force
}

Write-Host "== GunGameArena CI reference-assembly fetch =="
Write-Host "GameDir:    $GameDir"
Write-Host "ProfileDir: $ProfileDir"
Write-Host ""

$managedDir = Join-Path $GameDir "h3vr_Data\Managed"
$profileCoreDir = Join-Path $ProfileDir "core"
New-Item -ItemType Directory -Force -Path $managedDir | Out-Null
New-Item -ItemType Directory -Force -Path $profileCoreDir | Out-Null

# 1. Stripped game reference assemblies: H3VR.GameLibs on the BepInEx NuGet feed.
#    NOTE: the feed's "api/v2/package/..." URL is the *push* endpoint (POST/DELETE only,
#    returns 405 on GET) - downloads must go through the v3 flat-container endpoint instead:
#    {feed}/v3/package/{id-lowercase}/{version}/{id-lowercase}.{version}.nupkg
$gameLibsDir = Get-RemoteZip -Url "https://nuget.bepinex.dev/v3/package/h3vr.gamelibs/1.120.26/h3vr.gamelibs.1.120.26.nupkg" -Label "H3VR.GameLibs"
$libRoot = Join-Path $gameLibsDir "lib"
if (-not (Test-Path $libRoot)) {
    throw "H3VR.GameLibs package did not contain a 'lib' folder (looked in $gameLibsDir)"
}
$gameDlls = Get-ChildItem -Path $libRoot -Filter "*.dll" -Recurse
if (-not $gameDlls -or $gameDlls.Count -eq 0) {
    throw "No DLLs found anywhere under $libRoot in the H3VR.GameLibs package"
}
foreach ($dll in $gameDlls) {
    Copy-DllTo -SourceFile $dll.FullName -DestinationDir $managedDir
}

# 1b. H3VR.GameLibs 1.120.26 does not include UnityEngine.dll itself (only
#     Assembly-CSharp[.firstpass].dll and UnityEngine.UI.dll) - H3VR runs on Unity 5.6, so
#     pull the net35 UnityEngine.dll from BepInEx's separate "UnityEngine" 5.6.1 package.
$unityEngineDir = Get-RemoteZip -Url "https://nuget.bepinex.dev/v3/package/unityengine/5.6.1/unityengine.5.6.1.nupkg" -Label "UnityEngine"
$unityEngineDll = Join-Path $unityEngineDir "lib\net35\UnityEngine.dll"
Copy-DllTo -SourceFile $unityEngineDll -DestinationDir $managedDir

# 2. BepInEx core, from the H3VR BepInExPack.
$bepinexDir = Get-RemoteZip -Url "https://thunderstore.io/package/download/BepInEx/BepInExPack_H3VR/5.4.1700/" -Label "BepInExPack_H3VR"
$bepinexCoreSrc = Get-ChildItem -Path $bepinexDir -Filter "core" -Recurse -Directory | Select-Object -First 1
if (-not $bepinexCoreSrc) {
    throw "Could not find a 'core' folder inside the BepInExPack_H3VR package (looked under $bepinexDir)"
}
Copy-DllTo -SourceFile (Join-Path $bepinexCoreSrc.FullName "BepInEx.dll") -DestinationDir $profileCoreDir
Copy-DllTo -SourceFile (Join-Path $bepinexCoreSrc.FullName "0Harmony.dll") -DestinationDir $profileCoreDir

# 3. GunGame plugin (Kodeman-GunGame).
$gunGameDir = Get-RemoteZip -Url "https://thunderstore.io/package/download/Kodeman/GunGame/1.0.2/" -Label "Kodeman-GunGame"
$gunGameDll = Get-ChildItem -Path $gunGameDir -Filter "GunGame.dll" -Recurse | Select-Object -First 1
if (-not $gunGameDll) {
    throw "GunGame.dll not found in the Kodeman-GunGame package (looked under $gunGameDir)"
}
Copy-DllTo -SourceFile $gunGameDll.FullName -DestinationDir (Join-Path $ProfileDir "plugins\Kodeman-GunGame")

# 4. Sodalite (nrgill28-Sodalite).
$sodaliteDir = Get-RemoteZip -Url "https://thunderstore.io/package/download/nrgill28/Sodalite/1.5.1/" -Label "nrgill28-Sodalite"
$sodaliteDll = Get-ChildItem -Path $sodaliteDir -Filter "Sodalite.dll" -Recurse | Select-Object -First 1
if (-not $sodaliteDll) {
    throw "Sodalite.dll not found in the nrgill28-Sodalite package (looked under $sodaliteDir)"
}
Copy-DllTo -SourceFile $sodaliteDll.FullName -DestinationDir (Join-Path $ProfileDir "plugins\nrgill28-Sodalite")

# 5. Atlas (nrgill28-Atlas).
$atlasDir = Get-RemoteZip -Url "https://thunderstore.io/package/download/nrgill28/Atlas/1.0.4/" -Label "nrgill28-Atlas"
$atlasDll = Get-ChildItem -Path $atlasDir -Filter "Atlas.dll" -Recurse | Select-Object -First 1
if (-not $atlasDll) {
    throw "Atlas.dll not found in the nrgill28-Atlas package (looked under $atlasDir)"
}
Copy-DllTo -SourceFile $atlasDll.FullName -DestinationDir (Join-Path $ProfileDir "plugins\nrgill28-Atlas")

Remove-Item -Recurse -Force $workDir -ErrorAction SilentlyContinue

# Verify the layout the csproj files expect is fully present. Fail loudly, listing
# exactly what was (and wasn't) found, rather than letting the build fail cryptically.
$expected = @(
    (Join-Path $managedDir "Assembly-CSharp.dll"),
    (Join-Path $managedDir "Assembly-CSharp-firstpass.dll"),
    (Join-Path $managedDir "UnityEngine.dll"),
    (Join-Path $managedDir "UnityEngine.UI.dll"),
    (Join-Path $profileCoreDir "BepInEx.dll"),
    (Join-Path $profileCoreDir "0Harmony.dll"),
    (Join-Path $ProfileDir "plugins\Kodeman-GunGame\GunGame.dll"),
    (Join-Path $ProfileDir "plugins\nrgill28-Sodalite\Sodalite.dll"),
    (Join-Path $ProfileDir "plugins\nrgill28-Atlas\Atlas.dll")
)

$found = @()
$missing = @()
foreach ($path in $expected) {
    if (Test-Path $path) { $found += $path } else { $missing += $path }
}

Write-Host ""
Write-Host "Found $($found.Count)/$($expected.Count) expected DLLs:"
foreach ($path in $found) { Write-Host "  [OK] $path" }

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "Missing DLLs:"
    foreach ($path in $missing) { Write-Host "  [MISSING] $path" }
    throw "ci-fetch-refs.ps1: $($missing.Count) expected DLL(s) missing after fetch. See list above."
}

Write-Host ""
Write-Host "All expected reference DLLs are present."
