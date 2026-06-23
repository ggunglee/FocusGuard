[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $repoRoot 'dist'
$setupSource = Join-Path $repoRoot 'packaging\FocusGuard-Setup.bat'
$localDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { 'dotnet' }

function Publish-Variant {
    param(
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $FolderName
    )

    $outputPath = [System.IO.Path]::GetFullPath((Join-Path $distRoot $FolderName))
    $expectedRoot = [System.IO.Path]::GetFullPath($distRoot) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $outputPath.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe distribution path: $outputPath"
    }

    $publishTemp = Join-Path $repoRoot ("artifacts\publish\" + $FolderName)
    if (Test-Path -LiteralPath $publishTemp) {
        Remove-Item -LiteralPath $publishTemp -Recurse -Force
    }
    if (Test-Path -LiteralPath $outputPath) {
        Remove-Item -LiteralPath $outputPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $publishTemp -Force | Out-Null
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

    & $dotnet publish (Join-Path $repoRoot $Project) `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $publishTemp

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $Project"
    }

    $publishedExe = Join-Path $publishTemp 'FocusGuard.exe'
    if (-not (Test-Path -LiteralPath $publishedExe)) {
        throw "Published executable was not found: $publishedExe"
    }

    Copy-Item -LiteralPath $publishedExe -Destination (Join-Path $outputPath 'FocusGuard.exe')
    Copy-Item -LiteralPath $setupSource -Destination (Join-Path $outputPath 'FocusGuard-Setup.bat')

    $files = @(Get-ChildItem -LiteralPath $outputPath -File)
    $expectedNames = @('FocusGuard.exe', 'FocusGuard-Setup.bat')
    if ($files.Count -ne 2 -or @($files.Name | Where-Object { $_ -notin $expectedNames }).Count -ne 0) {
        throw "Unexpected files in $outputPath"
    }

    Remove-Item -LiteralPath $publishTemp -Recurse -Force
    Write-Host "Published $FolderName"
}

Publish-Variant -Project 'src\FocusGuard\FocusGuard.csproj' -FolderName 'FocusGuard-appscript-win-x64'
Publish-Variant -Project 'src\FocusGuard.Local\FocusGuard.Local.csproj' -FolderName 'FocusGuard-local-win-x64'

$artifactRoot = Join-Path $repoRoot 'artifacts\publish'
if ((Test-Path -LiteralPath $artifactRoot) -and -not (Get-ChildItem -LiteralPath $artifactRoot -Force)) {
    Remove-Item -LiteralPath $artifactRoot -Force
}

Write-Host 'Both FocusGuard distributions are ready.'
