using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Tests.Integracion;

internal sealed class BaseSqlite : IDisposable
{
    public BaseSqlite()
    {
        Archivo = Path.Combine(Path.GetTempPath(), $"logimarket-test-{Guid.NewGuid():N}.db");
        ClavesDemo = Enum.GetValues<Rol>().ToDictionary(rol => rol, _ => Guid.NewGuid().ToString("N"));
    }

    public string Archivo { get; }
    public IReadOnlyDictionary<Rol, string> ClavesDemo { get; }

    public LogiMarketDbContext Abrir() => new(ConexionSqlite.CrearOpciones(Archivo));

    public async Task MigrarAsync()
    {
        await using var db = Abrir();
        await db.Database.MigrateAsync();
    }

    public void Dispose()
    {
        File.Delete(Archivo);
        File.Delete(Archivo + "-shm");
        File.Delete(Archivo + "-wal");
        File.Delete(Archivo + "-journal");
    }
}
