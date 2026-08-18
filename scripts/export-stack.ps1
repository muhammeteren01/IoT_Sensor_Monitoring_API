# Builds images and writes pack/iot-images.tar for another PC (Docker only, no source).
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not (Test-Path "..\IoT_Sensor_Monitoring_UI")) {
    throw "UI repo missing. Expected ..\IoT_Sensor_Monitoring_UI next to this API repo."
}

Write-Host "Building API, Worker, UI..."
docker compose build
if ($LASTEXITCODE -ne 0) { throw "docker compose build failed" }

Write-Host "Building Grafana image with dashboards baked in..."
docker build -f Dockerfile.grafana -t iot-grafana:local .
if ($LASTEXITCODE -ne 0) { throw "grafana image build failed" }

$out = Join-Path $root "pack\iot-images.tar"
Write-Host "Saving images to $out ..."
docker save -o $out `
    iot-api:local `
    iot-worker:local `
    iot-ui:local `
    iot-grafana:local `
    postgres:16-alpine
if ($LASTEXITCODE -ne 0) { throw "docker save failed" }

Write-Host "Done. Copy the pack folder to the other PC:"
Write-Host "  pack\docker-compose.yml"
Write-Host "  pack\iot-images.tar"
Write-Host "On the other PC: .\scripts\import-stack.ps1"
