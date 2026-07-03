param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishProfile = "FolderProfile"
$installerScript = Join-Path $repoRoot "Installer\TargetTable.iss"

Write-Host "Publishing app with profile '$publishProfile'..."
dotnet publish (Join-Path $repoRoot "TimeTableApp.csproj") /p:PublishProfile=$publishProfile /p:Configuration=$Configuration

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    $commonPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $commonPath) {
        $iscc = @{ Source = $commonPath }
    }
}

if (-not $iscc) {
    Write-Warning "Inno Setup compiler (ISCC.exe) was not found. Install Inno Setup 6, then run this script again to build the installer."
    exit 0
}

Write-Host "Building installer with $($iscc.Source)..."
& $iscc.Source $installerScript
