using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AulaPedidos.NotificationsMock;

/// <summary>La fila durable es el efecto simulado; PK EventId implementa la deduplicación.</summary>
public sealed class ReceiptStore(IConfiguration configuration, TimeProvider timeProvider)
{
    private readonly string connectionString = configuration.GetConnectionString("Database") ?? "Data Source=receipts.db";

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS NotificationReceipts (
                EventId TEXT PRIMARY KEY NOT NULL,
                OrderId TEXT NOT NULL,
                Total TEXT NOT NULL,
                OccurredAtUtcTicks INTEGER NOT NULL,
                ReceivedAtUtcTicks INTEGER NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ReceiptResult> AcceptAsync(OrderNotification notification, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction();
        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO NotificationReceipts (EventId, OrderId, Total, OccurredAtUtcTicks, ReceivedAtUtcTicks)
            VALUES ($eventId, $orderId, $total, $occurredAt, $receivedAt)
            ON CONFLICT(EventId) DO NOTHING;
            """;
        insert.Parameters.AddWithValue("$eventId", notification.EventId.ToString("D"));
        insert.Parameters.AddWithValue("$orderId", notification.OrderId.ToString("D"));
        insert.Parameters.AddWithValue("$total", notification.Total.ToString("0.00", CultureInfo.InvariantCulture));
        insert.Parameters.AddWithValue("$occurredAt", notification.OccurredAt.UtcTicks);
        insert.Parameters.AddWithValue("$receivedAt", timeProvider.GetUtcNow().UtcTicks);
        var inserted = await insert.ExecuteNonQueryAsync(cancellationToken);
        if (inserted == 1)
        {
            // Aquí podrían agregarse otros efectos DENTRO de esta misma transacción local.
            // Enviar correo externo aquí no sería atómico: necesitaría su propia outbox.
            await transaction.CommitAsync(cancellationToken);
            return ReceiptResult.Accepted;
        }

        var existing = await ReadAsync(connection, transaction, notification.EventId, cancellationToken);
        var identical = existing is not null && existing.OrderId == notification.OrderId && existing.Total == notification.Total &&
            existing.OccurredAt == notification.OccurredAt;
        await transaction.CommitAsync(cancellationToken);
        return identical ? ReceiptResult.Duplicate : ReceiptResult.Conflict;
    }

    public async Task<NotificationReceipt?> GetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await ReadAsync(connection, null, eventId, cancellationToken);
    }

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='NotificationReceipts';";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) == 1;
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(connectionString);
        try { await connection.OpenAsync(cancellationToken); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static async Task<NotificationReceipt?> ReadAsync(SqliteConnection connection, SqliteTransaction? transaction,
        Guid eventId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT EventId, OrderId, Total, OccurredAtUtcTicks, ReceivedAtUtcTicks FROM NotificationReceipts WHERE EventId = $eventId;";
        command.Parameters.AddWithValue("$eventId", eventId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new NotificationReceipt(Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)),
            decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
            new DateTimeOffset(reader.GetInt64(3), TimeSpan.Zero), new DateTimeOffset(reader.GetInt64(4), TimeSpan.Zero));
    }
}

public sealed class ReceiptHealthCheck(ReceiptStore store) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await store.IsReadyAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Receipts schema unavailable");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return HealthCheckResult.Unhealthy("Receipts database unavailable"); }
    }
}
