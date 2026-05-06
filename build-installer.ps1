param(
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

& "$PSScriptRoot\build-release.ps1" -Runtime $Runtime

$candidates = @(
    (Get-Command iscc.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique

if (-not $candidates -or $candidates.Count -eq 0) {
    Write-Host "Inno Setup 6 compiler was not found."
    Write-Host "Install it first, then run this script again:"
    Write-Host "  winget install --id JRSoftware.InnoSetup -e"
    Write-Host "Expected compiler paths:"
    Write-Host "  $env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    Write-Host "  C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    Write-Host "  C:\Program Files\Inno Setup 6\ISCC.exe"
    exit 1
}

$iscc = @($candidates)[0]
New-Item -ItemType Directory -Force -Path "$PSScriptRoot\dist" | Out-Null
& $iscc "$PSScriptRoot\installer\MicVolumeLock.iss"

Write-Host "Ready: .\dist\MicVolumeLockSetup.exe"
