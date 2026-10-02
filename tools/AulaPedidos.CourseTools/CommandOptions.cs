using System.Globalization;

namespace AulaPedidos.CourseTools;

internal sealed class CommandOptions
{
    private readonly Dictionary<string, string?> values = new(StringComparer.Ordinal);

    public CommandOptions(string[] args, IEnumerable<string> flags, IEnumerable<string> valueOptions)
    {
        var acceptedFlags = flags.ToHashSet(StringComparer.Ordinal);
        var acceptedValues = valueOptions.ToHashSet(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            if (values.ContainsKey(option)) throw new ArgumentException("No repitas opciones en el mismo comando.");
            if (acceptedFlags.Contains(option)) values.Add(option, null);
            else if (acceptedValues.Contains(option))
            {
                if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException("Falta el valor de una opción. Consulta --help.");
                values.Add(option, args[index]);
            }
            else throw new ArgumentException("Opción desconocida para este comando. Consulta --help.");
        }
    }

    public bool Has(string name) => values.ContainsKey(name);
    public string Get(string name, string fallback) => values.GetValueOrDefault(name) ?? fallback;
    public int GetInt(string name, int fallback, int minimum, int maximum)
    {
        if (!values.TryGetValue(name, out var value)) return fallback;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) || number < minimum || number > maximum)
            throw new ArgumentException($"{name} debe ser un entero entre {minimum} y {maximum}.");
        return number;
    }
}
