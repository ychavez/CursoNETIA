#requires -Version 7.4
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath (Join-Path $taskRoot '.local/secrets.json'))) { throw 'Ejecuta primero ./scripts/setup.ps1.' }
$taskOldConnection = $env:ConnectionStrings__Database
$taskOldEnvironment = $env:ASPNETCORE_ENVIRONMENT
Push-Location $taskRoot
try {
    $env:ConnectionStrings__Database = "Data Source=$taskRoot/.local/AulaPedidos.db"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    & dotnet run --project src/AulaPedidos.Api --no-launch-profile -- --urls http://localhost:5080
    if ($LASTEXITCODE -ne 0) { throw 'La API terminó con error.' }
} finally {
    $env:ConnectionStrings__Database = $taskOldConnection
    $env:ASPNETCORE_ENVIRONMENT = $taskOldEnvironment
    Pop-Location
}
