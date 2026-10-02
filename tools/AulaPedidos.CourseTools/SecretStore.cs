using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AulaPedidos.CourseTools;

internal sealed record LocalSecrets(string JwtKey, string NotificationsKey, string SqlPassword);

internal static class SecretStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static readonly string[] EnvironmentKeys = ["JWT_KEY", "NOTIFICATIONS_KEY", "SQL_PASSWORD"];

    internal static LocalSecrets Read(string root)
    {
        var path = Path.Combine(root, ".local", "secrets.json");
        if (!File.Exists(path))
            throw new InvalidOperationException("Falta .local/secrets.json. Ejecuta CourseTools setup primero.");
        var secrets = JsonSerializer.Deserialize<LocalSecrets>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidOperationException(".local/secrets.json no contiene las claves del laboratorio.");
        Validate(secrets);
        return secrets;
    }

    internal static LocalSecrets Ensure(string root)
    {
        var directory = Path.Combine(root, ".local");
        Directory.CreateDirectory(directory);
        // Dos configuraciones simultáneas no deben generar pares distintos de archivos.
        using var setupLock = new FileStream(Path.Combine(directory, "setup.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var secretsPath = Path.Combine(directory, "secrets.json");
        var environmentPath = Path.Combine(root, ".env");
        var existingEnvironment = File.Exists(environmentPath) ? ReadEnvironment(environmentPath) : null;
        LocalSecrets secrets;
        if (File.Exists(secretsPath)) secrets = Read(root);
        else if (existingEnvironment is not null)
        {
            secrets = new LocalSecrets(existingEnvironment["JWT_KEY"], existingEnvironment["NOTIFICATIONS_KEY"], existingEnvironment["SQL_PASSWORD"]);
            Validate(secrets);
        }
        else
        {
            secrets = new LocalSecrets(
                Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                "Aula9!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20)));
        }

        if (existingEnvironment is not null &&
            (existingEnvironment["JWT_KEY"] != secrets.JwtKey ||
             existingEnvironment["NOTIFICATIONS_KEY"] != secrets.NotificationsKey ||
             existingEnvironment["SQL_PASSWORD"] != secrets.SqlPassword))
            throw new InvalidOperationException(".env y .local/secrets.json no coinciden. Reconcilia las claves manualmente; setup no sobrescribe ni rota secretos existentes.");

        // Validar el formato de ambos resultados antes de crear archivos.
        var environmentText = existingEnvironment is null
            ? $"JWT_KEY={EnvironmentValue(secrets.JwtKey)}\nNOTIFICATIONS_KEY={EnvironmentValue(secrets.NotificationsKey)}\nSQL_PASSWORD={EnvironmentValue(secrets.SqlPassword)}\n"
            : null;
        if (!File.Exists(secretsPath)) WriteNew(secretsPath, JsonSerializer.Serialize(secrets, JsonOptions) + Environment.NewLine);
        if (environmentText is not null) WriteNew(environmentPath, environmentText);
        return secrets;
    }

    private static Dictionary<string, string> ReadEnvironment(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var equals = line.IndexOf('=');
            if (equals < 1) continue;
            var name = line[..equals].Trim();
            if (!EnvironmentKeys.Contains(name, StringComparer.Ordinal)) continue;
            var value = line[(equals + 1)..].Trim();
            if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
                value = value[1..^1];
            else
            {
                // No interpretar expansión de variables como si fuera una clave literal.
                if (value.Contains('$') || value.Contains('\\'))
                    throw new InvalidOperationException("Las claves de .env usan expansión o escapes. Configura valores literales compatibles antes de ejecutar setup.");
                if (value.Length >= 2 && value[0] == '"' && value[^1] == '"') value = value[1..^1];
                else
                {
                    var comment = value.IndexOf(" #", StringComparison.Ordinal);
                    if (comment >= 0) value = value[..comment].TrimEnd();
                    if (value.StartsWith('"') || value.StartsWith('\''))
                        throw new InvalidOperationException("No se pudo interpretar una clave de .env. Revisa sus comillas sin publicar el valor.");
                }
            }
            if (!values.TryAdd(name, value)) throw new InvalidOperationException(".env repite una clave del laboratorio; corrige la ambigüedad sin rotar credenciales.");
        }
        if (EnvironmentKeys.Any(key => !values.ContainsKey(key)))
            throw new InvalidOperationException(".env debe incluir JWT_KEY, NOTIFICATIONS_KEY y SQL_PASSWORD. setup conserva el archivo existente; completa sus claves antes de continuar.");
        return values;
    }

    private static void Validate(LocalSecrets secrets)
    {
        if (string.IsNullOrWhiteSpace(secrets.JwtKey) || Encoding.UTF8.GetByteCount(secrets.JwtKey) < 32 ||
            string.IsNullOrWhiteSpace(secrets.NotificationsKey) || string.IsNullOrWhiteSpace(secrets.SqlPassword))
            throw new InvalidOperationException("Los secretos del laboratorio están incompletos o la clave JWT tiene menos de 32 bytes. No se reemplazaron.");
        if (new[] { secrets.JwtKey, secrets.NotificationsKey, secrets.SqlPassword }.Any(value => value.Any(char.IsControl)))
            throw new InvalidOperationException("Los secretos del laboratorio no pueden contener caracteres de control.");
    }

    private static string EnvironmentValue(string value)
    {
        if (value.Contains('\''))
            throw new InvalidOperationException("Una clave existente contiene comillas simples. Prepara .env manualmente con los mismos valores; setup no cambia claves.");
        // Compose no interpola variables dentro de comillas simples.
        return $"'{value}'";
    }

    private static void WriteNew(string path, string value)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, value, new UTF8Encoding(false));
            File.Move(temporary, path, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
