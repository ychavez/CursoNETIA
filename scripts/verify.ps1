#requires -Version 7.4
[CmdletBinding()]
param([switch]$Coverage)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    & dotnet restore AulaPedidos.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Restore falló.' }
    & dotnet build AulaPedidos.slnx -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build falló.' }
    $taskTestArguments = @('test','AulaPedidos.slnx','-c','Release','--no-build','--logger','trx','--results-directory','artifacts/tests')
    if ($Coverage) { $taskTestArguments += @('--collect','XPlat Code Coverage') }
    & dotnet @taskTestArguments
    if ($LASTEXITCODE -ne 0) { throw 'Tests fallaron.' }
    & dotnet ef migrations has-pending-model-changes --project src/AulaPedidos.Infrastructure --context SqliteAulaPedidosDbContext
    if ($LASTEXITCODE -ne 0) { throw 'Falta migración SQLite.' }
    & dotnet ef migrations has-pending-model-changes --project src/AulaPedidos.Infrastructure --context SqlServerAulaPedidosDbContext
    if ($LASTEXITCODE -ne 0) { throw 'Falta migración SQL Server.' }
    Write-Host 'Build, pruebas y consistencia de migraciones correctos.'
} finally { Pop-Location }
