# Other PC: Docker Desktop running. Run from the pack folder, or from repo root.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$pack = Join-Path $root "pack"
if (-not (Test-Path (Join-Path $pack "iot-images.tar"))) {
    $pack = Get-Location
}
$tar = Join-Path $pack "iot-images.tar"
$compose = Join-Path $pack "docker-compose.yml"
if (-not (Test-Path $tar)) { throw "iot-images.tar not found in $pack" }
if (-not (Test-Path $compose)) { throw "docker-compose.yml not found in $pack" }

Write-Host "Loading images from $tar ..."
docker load -i $tar
if ($LASTEXITCODE -ne 0) { throw "docker load failed" }

Write-Host "Starting stack..."
docker compose -f $compose up -d
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }

Write-Host "PulseGrid  http://localhost"
Write-Host "API       http://localhost:8080"
Write-Host "Grafana   http://localhost:3000"
Write-Host "Login     admin@iot.local / Admin123!"
