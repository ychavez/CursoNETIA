#requires -Version 7.4
[CmdletBinding()]
param([switch]$SkipDatabase)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Push-Location $taskRoot
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Instala .NET SDK 10 y Visual Studio 2026.' }
    New-Item -ItemType Directory -Force -Path '.local' | Out-Null
    $taskSecretsFile = Join-Path $taskRoot '.local/secrets.json'
    if (-not (Test-Path -LiteralPath $taskSecretsFile)) {
        $taskSecrets = @{
            JwtKey = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
            NotificationsKey = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
            SqlPassword = 'Aula9!' + [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(20))
        }
        $taskSecrets | ConvertTo-Json | Set-Content -LiteralPath $taskSecretsFile -Encoding utf8
    }
    $taskSecrets = Get-Content -Raw -LiteralPath $taskSecretsFile | ConvertFrom-Json
    # Sólo claves locales desechables. Nunca reutilizar estos archivos en producción.
    & dotnet user-secrets set 'Jwt:SigningKey' $taskSecrets.JwtKey --project src/AulaPedidos.Api | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar user-secrets de API.' }
    & dotnet user-secrets set 'Notifications:SharedKey' $taskSecrets.NotificationsKey --project src/AulaPedidos.Api | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar Notifications:SharedKey.' }
    & dotnet user-secrets set 'ConnectionStrings:Database' "Data Source=$taskRoot/.local/AulaPedidos.db" --project src/AulaPedidos.Api | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar la conexión local de API.' }
    & dotnet user-secrets set 'Notifications:SharedKey' $taskSecrets.NotificationsKey --project tools/AulaPedidos.NotificationsMock | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar la clave del receptor.' }
    & dotnet user-secrets set 'ConnectionStrings:Database' "Data Source=$taskRoot/.local/notifications.db" --project tools/AulaPedidos.NotificationsMock | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar la conexión local del receptor.' }
    if (-not (Test-Path -LiteralPath '.env')) {
        @(
            'JWT_KEY=' + $taskSecrets.JwtKey
            'NOTIFICATIONS_KEY=' + $taskSecrets.NotificationsKey
            'SQL_PASSWORD=' + $taskSecrets.SqlPassword
        ) | Set-Content -LiteralPath '.env' -Encoding utf8
    }
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet tool restore.' }
    & dotnet restore AulaPedidos.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Falló la restauración. Revisa SDK, red y packages.lock.json.' }
    & dotnet build AulaPedidos.slnx --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación.' }
    if (-not $SkipDatabase) {
        & dotnet ef database update --project src/AulaPedidos.Infrastructure --context SqliteAulaPedidosDbContext --connection "Data Source=$taskRoot/.local/AulaPedidos.db"
        if ($LASTEXITCODE -ne 0) { throw 'Falló la migración SQLite.' }
    }
    Write-Host 'Entorno preparado. Terminal 1: ./scripts/run-notifications.ps1; terminal 2: ./scripts/run-local.ps1; terminal 3: ./scripts/smoke.ps1'
} finally { Pop-Location }
