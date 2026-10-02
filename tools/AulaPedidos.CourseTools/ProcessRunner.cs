using System.ComponentModel;
using System.Diagnostics;

namespace AulaPedidos.CourseTools;

internal static class ProcessRunner
{
    internal static CancellationToken CancellationToken { get; set; }

    internal static async Task<int> DotnetAsync(string root, IEnumerable<string> args,
        IReadOnlyDictionary<string, string?>? environment = null, string? standardInput = null,
        bool suppressOutput = false)
    {
        var arguments = args.ToArray();
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = standardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        if (environment is not null)
            foreach (var pair in environment)
            {
                if (pair.Value is null) start.Environment.Remove(pair.Key);
                else start.Environment[pair.Key] = pair.Value;
            }
        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start()) throw new InvalidOperationException("No se pudo iniciar dotnet.");
        }
        catch (Win32Exception)
        {
            throw new InvalidOperationException("No se pudo iniciar dotnet. Instala el SDK .NET 10 y comprueba PATH.");
        }
        // Reenviar bytes mantiene visible la salida de los hijos también con CreateNoWindow.
        // User Secrets se drena a Stream.Null sin retener su salida en cadenas de memoria.
        using var output = suppressOutput ? Stream.Null : Console.OpenStandardOutput();
        using var error = suppressOutput ? Stream.Null : Console.OpenStandardError();
        var outputTask = process.StandardOutput.BaseStream.CopyToAsync(output, CancellationToken);
        var errorTask = process.StandardError.BaseStream.CopyToAsync(error, CancellationToken);
        try
        {
            if (standardInput is not null)
            {
                await process.StandardInput.WriteAsync(standardInput.AsMemory(), CancellationToken);
                process.StandardInput.Close();
            }
            await Task.WhenAll(process.WaitForExitAsync(CancellationToken), outputTask, errorTask);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) when (process.HasExited) { }
            await process.WaitForExitAsync(System.Threading.CancellationToken.None);
            try { await Task.WhenAll(outputTask, errorTask); }
            catch (OperationCanceledException) { }
            throw;
        }
        if (process.ExitCode != 0)
            throw new ProcessCommandException($"dotnet {arguments.FirstOrDefault() ?? ""} terminó con código {process.ExitCode}.", process.ExitCode);
        return 0;
    }
}

internal sealed class ProcessCommandException(string message, int exitCode) : Exception(message)
{
    internal int ExitCode { get; } = exitCode;
}
