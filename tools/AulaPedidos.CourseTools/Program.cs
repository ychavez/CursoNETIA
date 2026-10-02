using System.Text.Json;

namespace AulaPedidos.CourseTools;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, signal) =>
        {
            signal.Cancel = true;
            cancellation.Cancel();
        };
        ProcessRunner.CancellationToken = cancellation.Token;
        try
        {
            if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
            {
                PrintHelp();
                return 0;
            }

            var arguments = args.ToList();
            string? explicitRoot = null;
            for (var index = 0; index < arguments.Count; index++)
            {
                if (arguments[index] != "--root") continue;
                if (explicitRoot is not null || index + 1 >= arguments.Count || arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException("Usa --root una sola vez, seguido de la ruta de la solución.");
                explicitRoot = arguments[index + 1];
                arguments.RemoveRange(index, 2);
                index--;
            }
            if (arguments.Count == 0) throw new ArgumentException("Falta el comando. Consulta --help.");
            var root = FindRoot(explicitRoot);
            var commandArgs = arguments.Skip(1).ToArray();
            return arguments[0] switch
            {
                "setup" => await CourseCommands.SetupAsync(root, commandArgs),
                "token" => CourseCommands.Token(root, commandArgs),
                "verify" => await CourseCommands.VerifyAsync(root, commandArgs),
                "run-api" => await CourseCommands.RunApiAsync(root, commandArgs),
                "run-notifications" => await CourseCommands.RunNotificationsAsync(root, commandArgs),
                "outbox-status" => await CourseCommands.OutboxStatusAsync(root, commandArgs),
                "smoke" => await SmokeCommand.ExecuteAsync(root, commandArgs),
                "new-lab" => await LabCommand.ExecuteAsync(root, commandArgs),
                _ => throw new ArgumentException("Comando desconocido. Consulta --help.")
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Operación cancelada.");
            return 130;
        }
        catch (ProcessCommandException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return exception.ExitCode > 0 ? exception.ExitCode : 1;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        catch (JsonException)
        {
            Console.Error.WriteLine("Un archivo JSON no es válido. Revisa el archivo indicado por el comando sin publicar sus secretos.");
            return 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("No se pudo acceder a un archivo o directorio. Comprueba las rutas, permisos y archivos abiertos.");
            return 1;
        }
        catch (HttpRequestException)
        {
            Console.Error.WriteLine("La solicitud HTTP falló. Comprueba que la API y sus dependencias estén disponibles.");
            return 1;
        }
    }

    private static string FindRoot(string? explicitRoot)
    {
        if (explicitRoot is not null)
        {
            var root = Path.GetFullPath(explicitRoot);
            if (!File.Exists(Path.Combine(root, "AulaPedidos.slnx")))
                throw new ArgumentException("--root debe apuntar al directorio que contiene AulaPedidos.slnx.");
            return root;
        }
        for (var current = new DirectoryInfo(Directory.GetCurrentDirectory()); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "AulaPedidos.slnx"))) return current.FullName;
        throw new InvalidOperationException("No se encontró AulaPedidos.slnx. Ejecuta desde el repositorio o indica --root.");
    }

    private static void PrintHelp() => Console.WriteLine("""
        AulaPedidos CourseTools (.NET 10)
        dotnet run --project tools/AulaPedidos.CourseTools -- <comando> [opciones]

        setup [--skip-database | --secrets-only]
        token [--role Admin|Student] [--subject alumno] [--minutes 30]
        verify [--coverage]
        run-api
        run-notifications [--failures-before-success 2] [--always-fail]
        outbox-status
        smoke [--base-url http://localhost:5080]
        new-lab --destination <ruta>

        --root <ruta> selecciona otra copia de AulaPedidos.slnx.
        setup conserva secretos existentes y configura User Secrets para F5.
        --secrets-only prepara sólo .local/secrets.json y .env, sin User Secrets.
        token imprime intencionalmente una credencial temporal de laboratorio.
        """);
}
