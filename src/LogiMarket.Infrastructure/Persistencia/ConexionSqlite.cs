using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LogiMarket.Infrastructure.Persistencia;

public static class ConexionSqlite
{
    public static DbContextOptions<LogiMarketDbContext> CrearOpciones(string archivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archivo);
        var conexion = new SqliteConnectionStringBuilder
        {
            DataSource = archivo,
            ForeignKeys = true,
            DefaultTimeout = 5,
            Pooling = false
        };

        return new DbContextOptionsBuilder<LogiMarketDbContext>()
            .UseSqlite(conexion.ToString())
            .Options;
    }
}

public sealed class LogiMarketDbContextFactory : IDesignTimeDbContextFactory<LogiMarketDbContext>
{
    public LogiMarketDbContext CreateDbContext(string[] args) =>
        new(ConexionSqlite.CrearOpciones(args.SingleOrDefault() ?? "logimarket360.db"));
}
