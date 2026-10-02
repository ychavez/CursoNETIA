using System.Text;
using System.Xml.Linq;

namespace AulaPedidos.CourseTools;

public static class LabCommand
{
    private static readonly StringComparison PathComparison = OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".local", "artifacts", "TestResults", ".vs", ".idea", "scripts"
    };
    private static readonly HashSet<string> ExcludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".ps1", ".psm1", ".psd1", ".pfx", ".pem", ".key", ".db", ".db-shm", ".db-wal", ".db-journal", ".user", ".suo"
    };

    public static async Task<int> ExecuteAsync(string root, string[] args)
    {
        if (args.Length != 2 || args[0] != "--destination" || string.IsNullOrWhiteSpace(args[1]))
            throw new ArgumentException("Uso: new-lab --destination ../AulaPedidos-lab");

        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(args[1], root));
        EnsureOutsideReference(root, destination);
        EnsureNoLinks(root);
        EnsureNoLinks(destination);
        if (Path.Exists(destination))
            throw new InvalidOperationException("El destino debe ser nuevo; no se sobrescribe trabajo existente.");

        // Prepare an explicit allowlist before creating anything in the destination.
        var files = new Dictionary<string, string>(PathComparer);
        foreach (var file in new[]
        {
            "global.json", "Directory.Build.props", "NuGet.Config", ".editorconfig", ".gitignore", "dotnet-tools.json", "AulaPedidos.slnx",
            ".github/copilot-instructions.md"
        }) AddFile(root, file, files);
        foreach (var folder in new[] { ".github/instructions", ".github/prompts", ".github/agents", "docs", "instructor/plantillas" })
            foreach (var file in EnumerateSafeFiles(Path.Combine(root, folder))) AddFile(root, Path.GetRelativePath(root, file), files);

        var projectFiles = new HashSet<string>(PathComparer);
        foreach (var folder in new[] { "src", "tests", "tools" })
        {
            foreach (var project in EnumerateSafeFiles(Path.Combine(root, folder)).Where(file => Path.GetExtension(file).Equals(".csproj", StringComparison.OrdinalIgnoreCase)))
            {
                var relative = Path.GetRelativePath(root, project);
                AddFile(root, relative, files);
                projectFiles.Add(relative);
                var lockFile = Path.Combine(Path.GetDirectoryName(project)!, "packages.lock.json");
                if (File.Exists(lockFile)) AddFile(root, Path.GetRelativePath(root, lockFile), files);
            }
        }

        // Unlike the exercise projects, the course CLI must remain complete and runnable.
        const string toolDirectory = "tools/AulaPedidos.CourseTools";
        AddFile(root, toolDirectory + "/AulaPedidos.CourseTools.csproj", files);
        AddFile(root, toolDirectory + "/packages.lock.json", files);
        AddFile(root, toolDirectory + "/Program.cs", files);
        foreach (var source in EnumerateSafeFiles(Path.Combine(root, toolDirectory)).Where(file => Path.GetExtension(file).Equals(".cs", StringComparison.OrdinalIgnoreCase)))
            AddFile(root, Path.GetRelativePath(root, source), files);

        var parent = Path.GetDirectoryName(destination)
            ?? throw new ArgumentException("El destino debe tener una carpeta contenedora.");
        Directory.CreateDirectory(parent);
        EnsureNoLinks(parent);
        var staging = Path.Combine(parent, ".aulapedidos-lab-" + Guid.NewGuid().ToString("N"));
        EnsureOutsideReference(root, staging);
        if (Path.Exists(staging)) throw new IOException("No se pudo reservar una carpeta temporal nueva.");
        Directory.CreateDirectory(staging);
        var labId = Guid.NewGuid().ToString("N");
        try
        {
            foreach (var (relative, source) in files)
            {
                ProcessRunner.CancellationToken.ThrowIfCancellationRequested();
                var target = Path.Combine(staging, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (projectFiles.Contains(relative))
                {
                    var project = XDocument.Parse(await File.ReadAllTextAsync(source, ProcessRunner.CancellationToken), LoadOptions.PreserveWhitespace);
                    foreach (var id in project.Descendants().Where(element => element.Name.LocalName == "UserSecretsId"))
                        id.Value = $"AulaPedidos-Lab-{labId}-{Path.GetFileNameWithoutExtension(source)}";
                    await WriteNewAsync(target, project.ToString(SaveOptions.DisableFormatting));
                }
                else
                {
                    File.Copy(source, target, overwrite: false);
                }
            }

            const string minimalProgram = """
                var builder = WebApplication.CreateBuilder(args);
                var app = builder.Build();
                app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));
                app.Run();
                public partial class Program;
                """;
            await WriteNewAsync(Path.Combine(staging, "src/AulaPedidos.Api/Program.cs"), minimalProgram + Environment.NewLine);
            await WriteNewAsync(Path.Combine(staging, "tools/AulaPedidos.NotificationsMock/Program.cs"), minimalProgram + Environment.NewLine);
            await WriteNewAsync(Path.Combine(staging, "README.md"), """
                # Laboratorio del alumno

                Este es el punto de partida compilable, sin la implementación final de la aplicación.
                Abre AulaPedidos.slnx y sigue la carpeta instructor del repositorio de referencia.

                1. `dotnet tool restore`
                2. `dotnet restore AulaPedidos.slnx --locked-mode`
                3. `dotnet build AulaPedidos.slnx`
                4. Implementa entidades, casos de uso, infraestructura y API progresivamente.

                CourseTools se conserva completo. Para preparar configuración local cuando el laboratorio la necesite:

                `dotnet run --project tools/AulaPedidos.CourseTools -- setup --skip-database`

                La configuración y los secretos se generan en esta copia; no se copian desde la referencia.
                Cuando el módulo de datos incluya DbContext y migraciones, ejecuta setup sin --skip-database.
                No hay pruebas de la aplicación todavía: un build verde no acredita el curso.
                Consulta instructor/recorrido-laboratorio.md en la referencia para seguir los checkpoints.
                Docker y Compose se incorporan al llegar al módulo de operación.
                """ + Environment.NewLine);

            // Rename fails atomically if another process has created the requested destination.
            ProcessRunner.CancellationToken.ThrowIfCancellationRequested();
            EnsureNoLinks(parent);
            if (Path.Exists(destination))
                throw new InvalidOperationException("El destino apareció durante la copia; no se sobrescribe trabajo existente.");
            Directory.Move(staging, destination);
        }
        catch
        {
            // Only this newly created staging folder is eligible for cleanup.
            if (Directory.Exists(staging) && !IsLink(staging)) Directory.Delete(staging, recursive: true);
            throw;
        }

        Console.WriteLine($"Laboratorio preparado: {destination}");
        return 0;
    }

    private static void AddFile(string root, string relative, IDictionary<string, string> files)
    {
        relative = Path.GetRelativePath(root, Path.GetFullPath(Path.Combine(root, relative)));
        var source = Path.Combine(root, relative);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Un archivo del laboratorio quedó fuera de la referencia.");
        if (!File.Exists(source)) throw new IOException($"Falta un archivo requerido del laboratorio: {relative}");
        if (IsLink(source)) throw new InvalidOperationException($"El laboratorio no copia enlaces: {relative}");
        files.TryAdd(relative, source);
    }

    private static IEnumerable<string> EnumerateSafeFiles(string directory)
    {
        if (!Directory.Exists(directory)) yield break;
        if (IsLink(directory)) throw new InvalidOperationException("El laboratorio no recorre directorios enlazados.");
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory).Order(PathComparer))
        {
            if (ExcludedDirectories.Contains(Path.GetFileName(entry))) continue;
            if (IsLink(entry)) throw new InvalidOperationException("El laboratorio no copia archivos ni directorios enlazados.");
            if (Directory.Exists(entry))
            {
                foreach (var child in EnumerateSafeFiles(entry)) yield return child;
            }
            else if (!ExcludedExtensions.Contains(Path.GetExtension(entry))
                && !Path.GetFileName(entry).StartsWith(".env", StringComparison.OrdinalIgnoreCase)
                && !Path.GetFileName(entry).Equals("secrets.json", StringComparison.OrdinalIgnoreCase))
            {
                yield return entry;
            }
        }
    }

    private static void EnsureOutsideReference(string root, string destination)
    {
        if (destination.Equals(root, PathComparison)
            || destination.StartsWith(root + Path.DirectorySeparatorChar, PathComparison))
            throw new ArgumentException("El laboratorio debe estar fuera del repositorio de referencia.");
    }

    private static void EnsureNoLinks(string path)
    {
        for (DirectoryInfo? current = new(path); current is not null; current = current.Parent)
            if (current.LinkTarget is not null || current.Exists && IsLink(current.FullName))
                throw new ArgumentException("El destino y la referencia deben usar rutas directas, sin enlaces simbólicos ni junctions.");
    }

    private static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static async Task WriteNewAsync(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await writer.WriteAsync(text.AsMemory(), ProcessRunner.CancellationToken);
    }
}
