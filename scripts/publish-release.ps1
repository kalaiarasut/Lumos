$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $root "artifacts\publish\Lumos"
$installerDir = Join-Path $root "artifacts\installer"
$installerScript = Join-Path $root "installer\Lumos.iss"
$projectFile = Join-Path $root "src\Lumos\Lumos.csproj"
$solutionFile = Join-Path $root "Lumos.sln"

[xml]$projectXml = Get-Content -LiteralPath $projectFile
$version = $projectXml.Project.PropertyGroup.Version

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $installerDir | Out-Null

dotnet test $solutionFile
dotnet publish $projectFile `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $publishDir

if (-not (Test-Path (Join-Path $publishDir "Lumos.exe"))) {
    throw "Publish failed: Lumos.exe was not created at $publishDir."
}

$iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
if (-not $iscc) {
    $knownIsccPaths = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe"),
        "C:\Program Files (x86)\Inno Setup 7\ISCC.exe",
        "C:\Program Files\Inno Setup 7\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )

    $isccPath = $knownIsccPaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if ($isccPath) {
        $iscc = Get-Item -LiteralPath $isccPath
    }
}

if ($iscc) {
    $isccExecutable = if ($iscc.Source) { $iscc.Source } else { $iscc.FullName }
    & $isccExecutable $installerScript
} else {
    Write-Warning "Inno Setup compiler (iscc.exe) was not found on PATH. Publish output is ready at $publishDir."
    Write-Warning "Install Inno Setup and rerun this script to build artifacts\installer\LumosSetup-$version.exe."
}
