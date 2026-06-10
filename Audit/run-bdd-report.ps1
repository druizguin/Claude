# Ejecuta los tests BDD y genera el reporte LivingDoc HTML
# Uso: ./run-bdd-report.ps1
# Uso con configuracion: ./run-bdd-report.ps1 -Configuration Release

param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net8.0"
)

$ErrorActionPreference = "Stop"
$ProjectDir = Join-Path $PSScriptRoot "Audit.BDD.Tests"
$OutputDir  = Join-Path $ProjectDir "bin" $Configuration $Framework

# 1. Restaurar herramientas locales (incluye livingdoc CLI)
Write-Host "`n[1/3] Restaurando dotnet tools..." -ForegroundColor Cyan
Push-Location $ProjectDir
dotnet tool restore
Pop-Location

# 2. Ejecutar tests (SpecFlow.Plus.LivingDocPlugin genera TestExecution.json)
Write-Host "`n[2/3] Ejecutando tests BDD..." -ForegroundColor Cyan
dotnet test $ProjectDir -c $Configuration --no-restore

# 3. Generar LivingDoc HTML
Write-Host "`n[3/3] Generando LivingDoc HTML..." -ForegroundColor Cyan
$dll    = Join-Path $OutputDir "Audit.BDD.Tests.dll"
$outDir = Join-Path $OutputDir "TestResults"
$html   = Join-Path $outDir "LivingDoc.html"

New-Item -ItemType Directory -Force $outDir | Out-Null

Push-Location $ProjectDir
dotnet tool run livingdoc test-assembly $dll --output $html
Pop-Location

Write-Host "`nLivingDoc report generado en:" -ForegroundColor Green
Write-Host "  $html" -ForegroundColor White
