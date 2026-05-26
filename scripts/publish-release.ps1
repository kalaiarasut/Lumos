$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $root "artifacts\publish\Lumos"
$installerDir = Join-Path $root "artifacts\installer"
$installerScript = Join-Path $root "installer\Lumos.iss"

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $installerDir | Out-Null

dotnet test (Join-Path $root "Lumos.sln")
dotnet publish (Join-Path $root "src\Lumos\Lumos.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $publishDir

$iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
if ($iscc) {
    & $iscc.Source $installerScript
} else {
    Write-Warning "Inno Setup compiler (iscc.exe) was not found on PATH. Publish output is ready at $publishDir."
    Write-Warning "Install Inno Setup and rerun this script to build artifacts\installer\LumosSetup-0.2.0.exe."
}
