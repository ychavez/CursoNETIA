#requires -Version 7.4
[CmdletBinding()]
param([int]$FailuresBeforeSuccess = 0, [switch]$AlwaysFail)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskSecretsFile = Join-Path $taskRoot '.local/secrets.json'
if (-not (Test-Path -LiteralPath $taskSecretsFile)) { throw 'Ejecuta primero ./scripts/setup.ps1.' }
$taskSecrets = Get-Content -Raw -LiteralPath $taskSecretsFile | ConvertFrom-Json
$taskNames = @('Notifications__SharedKey','ConnectionStrings__Database','Simulation__FailuresBeforeSuccess','Simulation__AlwaysFail')
$taskPrevious = @{}
foreach ($taskName in $taskNames) { $taskPrevious[$taskName] = [Environment]::GetEnvironmentVariable($taskName) }
Push-Location $taskRoot
try {
    $env:Notifications__SharedKey = $taskSecrets.NotificationsKey
    $env:ConnectionStrings__Database = "Data Source=$taskRoot/.local/notifications.db"
    $env:Simulation__FailuresBeforeSuccess = "$FailuresBeforeSuccess"
    $env:Simulation__AlwaysFail = "$AlwaysFail"
    & dotnet run --project tools/AulaPedidos.NotificationsMock --no-launch-profile -- --urls http://localhost:5099
    if ($LASTEXITCODE -ne 0) { throw 'El receptor terminó con error.' }
} finally {
    foreach ($taskName in $taskNames) { [Environment]::SetEnvironmentVariable($taskName, $taskPrevious[$taskName]) }
    Pop-Location
}
