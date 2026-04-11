param(
    [string]$Configuration = "Debug"
)

dotnet build GladiatorManagerAccess.csproj -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed."
    exit 1
}

Write-Host "Build succeeded." -ForegroundColor Green
