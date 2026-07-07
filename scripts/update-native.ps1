<#
.SYNOPSIS
    Downloads the latest Windows x64 (WGL) build of maplibre-native-c and updates the bundled plugin binary.

.DESCRIPTION
    Uses the GitHub CLI (`gh`) to fetch the `maplibre-native-c-windows-x64-wgl.tar.gz` asset from the
    `unstable-native-snapshot` release tag of the `maplibre/maplibre-native-ffi` repository, extracts it into a
    temporary directory, and overwrites the DLL bundled in this repository's Unity package
    (Packages/com.fukuda-a-hu.maplibre-unity/Runtime/Plugins/Windows/x86_64/maplibre-native-c.dll).

    Requires the GitHub CLI (`gh`) to be installed and authenticated.

.EXAMPLE
    pwsh ./scripts/update-native.ps1
#>

$ErrorActionPreference = 'Stop'

$Repo = 'maplibre/maplibre-native-ffi'
$Tag = 'unstable-native-snapshot'
$AssetName = 'maplibre-native-c-windows-x64-wgl.tar.gz'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$DestDll = Join-Path $RepoRoot 'Packages\com.fukuda-a-hu.maplibre-unity\Runtime\Plugins\Windows\x86_64\maplibre-native-c.dll'

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI ('gh') was not found on PATH. Install it from https://cli.github.com/ and authenticate with 'gh auth login' before running this script."
}

$TempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("maplibre-native-update-" + [System.Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

try {
    $ArchivePath = Join-Path $TempDir $AssetName

    Write-Host "Downloading $AssetName from $Repo@$Tag ..."
    gh release download $Tag --repo $Repo --pattern $AssetName --dir $TempDir --clobber

    if (-not (Test-Path $ArchivePath)) {
        throw "Expected downloaded asset was not found at '$ArchivePath'."
    }

    $ExtractDir = Join-Path $TempDir 'extracted'
    New-Item -ItemType Directory -Path $ExtractDir -Force | Out-Null

    Write-Host "Extracting archive ..."
    tar -xzf $ArchivePath -C $ExtractDir
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to extract '$ArchivePath' (tar exit code $LASTEXITCODE)."
    }

    $ExtractedDll = Get-ChildItem -Path $ExtractDir -Recurse -Filter 'maplibre-native-c.dll' | Select-Object -First 1
    if (-not $ExtractedDll) {
        throw "Could not locate 'maplibre-native-c.dll' inside the extracted archive."
    }

    Write-Host "Copying $($ExtractedDll.FullName) -> $DestDll"
    Copy-Item -Path $ExtractedDll.FullName -Destination $DestDll -Force

    Write-Host "Done. Updated native binary at '$DestDll'."
}
finally {
    if (Test-Path $TempDir) {
        Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
