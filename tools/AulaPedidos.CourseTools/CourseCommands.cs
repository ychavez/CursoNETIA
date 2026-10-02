using System.Globalization;
using System.Text.Json;

namespace AulaPedidos.CourseTools;

internal static class CourseCommands
{
    internal static async Task<int> SetupAsync(string root, string[] args)
    {
        var options = new CommandOptions(args, ["--skip-database", "--secrets-only"], []);
        if (options.Has("--skip-database") && options.Has("--secrets-only"))
            throw new ArgumentException("Elige --skip-database o --secrets-only, no ambos.");
        var secrets = SecretStore.Ensure(root);
        if (options.Has("--secrets-only"))
        {
            Console.WriteLine("Secretos locales y .env preparados. Se conservaron los valores existentes; no se modificó User Secrets.");
            return 0;
        }

        // JSON por stdin: las credenciales no aparecen en la línea de comandos ni en la salida.
        await SetUserSecretsAsync(root, "src/AulaPedidos.Api", new Dictionary<string, string>
        {
            ["Jwt:SigningKey"] = secrets.JwtKey,
            ["Notifications:SharedKey"] = secrets.NotificationsKey,
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:Database"] = SqliteConnection(root, "AulaPedidos.db")
        });
        await SetUserSecretsAsync(root, "tools/AulaPedidos.NotificationsMock", new Dictionary<string, string>
        {
            ["Notifications:SharedKey"] = secrets.NotificationsKey,
            ["ConnectionStrings:Database"] = SqliteConnection(root, "notifications.db")
        });
        await ProcessRunner.DotnetAsync(root, ["tool", "restore"]);
        await ProcessRunner.DotnetAsync(root, ["restore", "AulaPedidos.slnx", "--locked-mode"]);
        await ProcessRunner.DotnetAsync(root, ["build", "AulaPedidos.slnx", "--no-restore"]);
        if (!options.Has("--skip-database"))
            await ProcessRunner.DotnetAsync(root,
                ["ef", "database", "update", "--project", "src/AulaPedidos.Infrastructure", "--context", "SqliteAulaPedidosDbContext"],
                new Dictionary<string, string?> { ["ConnectionStrings__Database"] = SqliteConnection(root, "AulaPedidos.db") });
        Console.WriteLine("Entorno preparado. En terminales separadas ejecuta CourseTools run-notifications, run-api y smoke.");
        return 0;
    }

    internal static int Token(string root, string[] args)
    {
        var options = new CommandOptions(args, [], ["--role", "--subject", "--minutes"]);
        Console.WriteLine(TokenService.Create(root, options.Get("--role", "Student"), options.Get("--subject", "alumno"),
            options.GetInt("--minutes", 30, 1, 60)));
        return 0;
    }

    internal static async Task<int> VerifyAsync(string root, string[] args)
    {
        var options = new CommandOptions(args, ["--coverage"], []);
        await ProcessRunner.DotnetAsync(root, ["tool", "restore"]);
        await ProcessRunner.DotnetAsync(root, ["restore", "AulaPedidos.slnx", "--locked-mode"]);
        // CourseTools está ejecutándose: no recompilar su apphost ocupado en Windows.
        // Compilar la API, el receptor y ambos proyectos de pruebas cubre sus dependencias.
        var projects = new[]
        {
            "src/AulaPedidos.Api/AulaPedidos.Api.csproj",
            "tools/AulaPedidos.NotificationsMock/AulaPedidos.NotificationsMock.csproj",
            "tests/AulaPedidos.UnitTests/AulaPedidos.UnitTests.csproj",
            "tests/AulaPedidos.IntegrationTests/AulaPedidos.IntegrationTests.csproj"
        };
        foreach (var project in projects)
            await ProcessRunner.DotnetAsync(root, ["build", project, "-c", "Release", "--no-restore"]);
        foreach (var project in projects.Where(project => project.StartsWith("tests/", StringComparison.Ordinal)))
        {
            var testArgs = new List<string> { "test", project, "-c", "Release", "--no-build", "--logger", "trx", "--results-directory", "artifacts/tests" };
            if (options.Has("--coverage")) testArgs.AddRange(["--collect", "XPlat Code Coverage"]);
            await ProcessRunner.DotnetAsync(root, testArgs);
        }
        // Evitar heredar una conexión local de SQLite al verificar el modelo SQL Server.
        var environment = new Dictionary<string, string?> { ["ConnectionStrings__Database"] = null };
        foreach (var context in new[] { "SqliteAulaPedidosDbContext", "SqlServerAulaPedidosDbContext" })
            await ProcessRunner.DotnetAsync(root,
                ["ef", "migrations", "has-pending-model-changes", "--project", "src/AulaPedidos.Infrastructure", "--context", context], environment);
        Console.WriteLine("Compilación, pruebas y consistencia de migraciones correctas.");
        return 0;
    }

    internal static Task<int> RunApiAsync(string root, string[] args)
    {
        _ = new CommandOptions(args, [], []);
        return ProcessRunner.DotnetAsync(root,
            ["run", "--project", "src/AulaPedidos.Api", "--no-launch-profile", "--", "--urls", "http://localhost:5080"], ApiEnvironment(root));
    }

    internal static Task<int> RunNotificationsAsync(string root, string[] args)
    {
        var options = new CommandOptions(args, ["--always-fail"], ["--failures-before-success"]);
        var failures = options.GetInt("--failures-before-success", 0, 0, int.MaxValue);
        var secrets = SecretStore.Read(root);
        var environment = new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["DOTNET_ENVIRONMENT"] = "Development",
            ["Notifications__SharedKey"] = secrets.NotificationsKey,
            ["ConnectionStrings__Database"] = SqliteConnection(root, "notifications.db"),
            ["Simulation__FailuresBeforeSuccess"] = failures.ToString(CultureInfo.InvariantCulture),
            ["Simulation__AlwaysFail"] = options.Has("--always-fail") ? "true" : "false"
        };
        return ProcessRunner.DotnetAsync(root,
            ["run", "--project", "tools/AulaPedidos.NotificationsMock", "--no-launch-profile", "--", "--urls", "http://localhost:5099"], environment);
    }

    internal static Task<int> OutboxStatusAsync(string root, string[] args)
    {
        _ = new CommandOptions(args, [], []);
        return ProcessRunner.DotnetAsync(root,
            ["run", "--project", "src/AulaPedidos.Api", "--no-launch-profile", "--", "--outbox-status"], ApiEnvironment(root));
    }

    private static Dictionary<string, string?> ApiEnvironment(string root)
    {
        var secrets = SecretStore.Read(root);
        return new Dictionary<string, string?>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["DOTNET_ENVIRONMENT"] = "Development",
            ["Database__Provider"] = "Sqlite",
            ["ConnectionStrings__Database"] = SqliteConnection(root, "AulaPedidos.db"),
            ["Jwt__DemoMode"] = "true",
            ["Jwt__SigningKey"] = secrets.JwtKey,
            ["Notifications__SharedKey"] = secrets.NotificationsKey,
            ["Notifications__BaseUrl"] = "http://localhost:5099/"
        };
    }

    private static string SqliteConnection(string root, string name)
    {
        // Una ruta entre comillas evita interpretar un punto y coma como otra opción.
        var path = Path.Combine(root, ".local", name).Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"Data Source=\"{path}\"";
    }

    private static Task<int> SetUserSecretsAsync(string root, string project, Dictionary<string, string> secrets) =>
        ProcessRunner.DotnetAsync(root, ["user-secrets", "set", "--project", project],
            standardInput: JsonSerializer.Serialize(secrets), suppressOutput: true);
}
