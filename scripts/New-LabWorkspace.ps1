#requires -Version 7.4
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Destination)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$taskDestination = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)
if ($taskDestination.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $taskDestination -eq $taskRoot) {
    throw 'El laboratorio debe estar fuera del repositorio de referencia.'
}
if (Test-Path -LiteralPath $taskDestination) { throw 'El destino debe ser nuevo; no se sobrescribe trabajo existente.' }
New-Item -ItemType Directory -Path $taskDestination | Out-Null
foreach ($taskFile in @('global.json','Directory.Build.props','NuGet.Config','.editorconfig','.gitignore','dotnet-tools.json','AulaPedidos.slnx')) {
    Copy-Item -LiteralPath (Join-Path $taskRoot $taskFile) -Destination $taskDestination
}
New-Item -ItemType Directory -Path (Join-Path $taskDestination '.github') | Out-Null
Copy-Item -LiteralPath (Join-Path $taskRoot '.github/copilot-instructions.md') -Destination (Join-Path $taskDestination '.github')
foreach ($taskFolder in @('.github/instructions','.github/prompts','.github/agents','docs')) {
    if (Test-Path -LiteralPath (Join-Path $taskRoot $taskFolder)) {
        Copy-Item -LiteralPath (Join-Path $taskRoot $taskFolder) -Destination (Join-Path $taskDestination (Split-Path -Parent $taskFolder)) -Recurse
    }
}
$taskProjectRoots = @('src','tests','tools')
$taskLabId = [guid]::NewGuid().ToString('N')
New-Item -ItemType Directory -Force -Path (Join-Path $taskDestination 'instructor') | Out-Null
Copy-Item -LiteralPath (Join-Path $taskRoot 'instructor/plantillas') -Destination (Join-Path $taskDestination 'instructor') -Recurse
foreach ($taskProjectRoot in $taskProjectRoots) {
    foreach ($taskProject in (Get-ChildItem -LiteralPath (Join-Path $taskRoot $taskProjectRoot) -Filter '*.csproj' -Recurse)) {
        $taskRelative = [IO.Path]::GetRelativePath($taskRoot, $taskProject.DirectoryName)
        $taskTargetFolder = Join-Path $taskDestination $taskRelative
        New-Item -ItemType Directory -Force -Path $taskTargetFolder | Out-Null
        $taskProjectText = Get-Content -Raw -LiteralPath $taskProject.FullName
        $taskSecretId = "AulaPedidos-Lab-$taskLabId-$($taskProject.BaseName)"
        $taskProjectText = $taskProjectText -replace '<UserSecretsId>.*?</UserSecretsId>', "<UserSecretsId>$taskSecretId</UserSecretsId>"
        Set-Content -LiteralPath (Join-Path $taskTargetFolder $taskProject.Name) -Value $taskProjectText -Encoding utf8
        $taskLock = Join-Path $taskProject.DirectoryName 'packages.lock.json'
        if (Test-Path -LiteralPath $taskLock) { Copy-Item -LiteralPath $taskLock -Destination $taskTargetFolder }
    }
}
$taskMinimalProgram = @'
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));
app.Run();
public partial class Program;
'@
Set-Content -LiteralPath (Join-Path $taskDestination 'src/AulaPedidos.Api/Program.cs') -Value $taskMinimalProgram -Encoding utf8
Set-Content -LiteralPath (Join-Path $taskDestination 'tools/AulaPedidos.NotificationsMock/Program.cs') -Value $taskMinimalProgram -Encoding utf8
@'
# Laboratorio del alumno
Este es el punto de partida compilable, sin la implementación final.
Abre AulaPedidos.slnx. Usa la carpeta instructor del repositorio de referencia para seguir las clases.
1. dotnet tool restore
2. dotnet restore AulaPedidos.slnx --locked-mode
3. dotnet build AulaPedidos.slnx
4. Implementa entidades, casos de uso, infraestructura y API progresivamente.
No hay pruebas aún: un build verde NO acredita el curso.
Consulta el mapa de checkpoints en instructor/recorrido-laboratorio.md de la referencia.
Los scripts y Compose completos se incorporan al llegar a las clases de operación.
'@ | Set-Content -LiteralPath (Join-Path $taskDestination 'README.md') -Encoding utf8
Write-Host "Laboratorio preparado: $taskDestination"
