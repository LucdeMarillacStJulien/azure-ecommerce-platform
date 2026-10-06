<#
.SYNOPSIS
    Builds and starts the whole platform offline in Docker:
    Azurite + SQL Server + EF migrations + FunctionApp + web app.
.DESCRIPTION
    First run needs internet once to download the base images and NuGet packages.
    After that it runs fully offline. Stop with:  docker compose down
    Wipe all local data with:                       docker compose down -v
#>
Set-Location $PSScriptRoot
$url = 'http://localhost:8080'

docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Docker is not running. Start Docker Desktop and try again.' -ForegroundColor Red
    exit 1
}

Write-Host 'Building and starting containers...' -ForegroundColor Cyan
docker compose up -d --build
if ($LASTEXITCODE -ne 0) {
    Write-Host 'docker compose failed (see above).' -ForegroundColor Red
    Write-Host 'If a port is "already allocated", stop whatever uses 10000-10002, 1433, 7135 or 8080 (e.g. a local Azurite).'
    exit 1
}

Write-Host "Waiting for $url ..." -ForegroundColor Cyan
$deadline = (Get-Date).AddMinutes(3)
do {
    Start-Sleep -Seconds 3
    try   { $ready = (Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200 }
    catch { $ready = $false }
} until ($ready -or (Get-Date) -gt $deadline)

if (-not $ready) {
    docker compose logs --tail 40 migrate functions web
    Write-Host 'The web app did not come up in 3 minutes. Logs above.' -ForegroundColor Red
    exit 1
}

Write-Host "Ready: $url  (log in with the seeded admin account from Program.cs)" -ForegroundColor Green
Start-Process $url
