#requires -Version 7.4
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskOldConnection = $env:ConnectionStrings__Database
$taskOldEnvironment = $env:ASPNETCORE_ENVIRONMENT
Push-Location $taskRoot
try {
    $env:ConnectionStrings__Database = "Data Source=$taskRoot/.local/AulaPedidos.db"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    & dotnet run --project src/AulaPedidos.Api --no-build --no-launch-profile -- --outbox-status
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo consultar outbox.' }
} finally {
    $env:ConnectionStrings__Database = $taskOldConnection
    $env:ASPNETCORE_ENVIRONMENT = $taskOldEnvironment
    Pop-Location
}
