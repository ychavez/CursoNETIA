using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AulaPedidos.Infrastructure.Persistence;

public sealed class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<SqliteAulaPedidosDbContext>
{
    public SqliteAulaPedidosDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database") ?? "Data Source=aula-pedidos.db";
        var options = new DbContextOptionsBuilder<SqliteAulaPedidosDbContext>().UseSqlite(connection).Options;
        return new SqliteAulaPedidosDbContext(options, TimeProvider.System);
    }
}

public sealed class SqlServerDesignTimeFactory : IDesignTimeDbContextFactory<SqlServerAulaPedidosDbContext>
{
    public SqlServerAulaPedidosDbContext CreateDbContext(string[] args)
    {
        // Crear/scriptar migraciones no requiere abrir la conexión. Actualizar sí requiere
        // una cadena real suministrada desde secretos o ConnectionStrings__Database.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Server=localhost;Database=AulaPedidos;Integrated Security=true;Encrypt=true;TrustServerCertificate=false";
        var options = new DbContextOptionsBuilder<SqlServerAulaPedidosDbContext>().UseSqlServer(connection).Options;
        return new SqlServerAulaPedidosDbContext(options, TimeProvider.System);
    }
}
