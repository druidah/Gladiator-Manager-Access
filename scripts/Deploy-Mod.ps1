param(
    [string]$Configuration = "Debug"
)

# Call build script
& "$PSScriptRoot\Build-Mod.ps1" -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    exit 1
}

Write-Host "Deployed to Mods folder (via csproj target)" -ForegroundColor Green
