#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('Admin','Student')][string]$Role = 'Student',
    [ValidateLength(1,100)][string]$Subject = 'alumno',
    [ValidateRange(1,60)][int]$Minutes = 30
)
$ErrorActionPreference = 'Stop'
if ($Subject.Trim() -ne $Subject -or [string]::IsNullOrWhiteSpace($Subject)) { throw 'Subject no puede contener espacios exteriores.' }
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskSecretsFile = Join-Path $taskRoot '.local/secrets.json'
if (-not (Test-Path -LiteralPath $taskSecretsFile)) { throw 'Ejecuta ./scripts/setup.ps1.' }
$taskSecrets = Get-Content -Raw -LiteralPath $taskSecretsFile | ConvertFrom-Json
function ConvertTo-Base64Url([byte[]]$Bytes) { [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+','-').Replace('/','_') }
$taskNow = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$taskPermissions = @('orders.read','orders.write')
if ($Role -eq 'Admin') { $taskPermissions += 'catalog.write' }
$taskHeader = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
$taskPayload = @{
    iss = 'AulaPedidos.Demo'; aud = 'AulaPedidos.Api'; sub = $Subject; role = $Role
    permission = $taskPermissions; iat = $taskNow; nbf = $taskNow
    exp = $taskNow + $Minutes * 60; jti = [guid]::NewGuid().ToString()
} | ConvertTo-Json -Compress
$taskEncodedPayload = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes($taskPayload))
$taskUnsigned = "$taskHeader.$taskEncodedPayload"
$taskHmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($taskSecrets.JwtKey))
try { $taskSignature = ConvertTo-Base64Url ($taskHmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($taskUnsigned))) }
finally { $taskHmac.Dispose() }
# Salida intencionada de credencial temporal de laboratorio. No pegarla en documentación/commits.
"$taskUnsigned.$taskSignature"
