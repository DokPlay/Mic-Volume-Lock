param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

dotnet publish `
    -c Release `
    -r $Runtime `
    -o ".\publish" `
    -p:SelfContained=true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Write-Host "Ready: .\publish\MicVolumeLock.exe"
