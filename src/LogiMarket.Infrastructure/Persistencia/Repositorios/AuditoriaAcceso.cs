using LogiMarket.Application.Seguridad;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class AuditoriaAcceso(LogiMarketDbContext db, DbContextOptions<LogiMarketDbContext> opciones,
    TimeProvider reloj) : IAuditoriaAcceso
{
    private readonly List<AuditoriaRegistro> _pendientes = [];

    public Task DenegadoAsync(Guid? usuarioId, Operacion operacion, CancellationToken ct)
    {
        _pendientes.Add(new AuditoriaRegistro
        {
            UsuarioId = usuarioId,
            Categoria = "SEGURIDAD",
            Accion = operacion.ToString(),
            Resultado = "DENEGADO",
            Correlacion = Guid.NewGuid().ToString("N"),
            FechaUtc = reloj.GetUtcNow().UtcDateTime
        });

        return db.Database.CurrentTransaction is null ? GuardarPendientesAsync(ct) : Task.CompletedTask;
    }

    public async Task GuardarPendientesAsync(CancellationToken ct)
    {
        if (_pendientes.Count == 0)
            return;

        await using var escritura = new LogiMarketDbContext(opciones);
        escritura.Auditorias.AddRange(_pendientes);
        await escritura.SaveChangesAsync(ct);
        _pendientes.Clear();
    }
}
