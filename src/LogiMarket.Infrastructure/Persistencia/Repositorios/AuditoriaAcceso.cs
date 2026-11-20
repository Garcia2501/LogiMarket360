using LogiMarket.Application.Seguridad;
using LogiMarket.Infrastructure.Persistencia.Operaciones;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class AuditoriaAcceso(LogiMarketDbContext db, TimeProvider reloj) : IAuditoriaAcceso
{
    public async Task DenegadoAsync(Guid? usuarioId, Operacion operacion, CancellationToken ct)
    {
        db.Auditorias.Add(new AuditoriaRegistro
        {
            UsuarioId = usuarioId,
            Categoria = "SEGURIDAD",
            Accion = operacion.ToString(),
            Resultado = "DENEGADO",
            Correlacion = Guid.NewGuid().ToString("N"),
            FechaUtc = reloj.GetUtcNow().UtcDateTime
        });
        await db.SaveChangesAsync(ct);
    }
}
