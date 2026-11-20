using LogiMarket.Application.Eventos;
using LogiMarket.Application.Persistencia;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Eventos;

public sealed class EntregaDurableSqlite(LogiMarketDbContext db, IUnidadDeTrabajo unidad, TimeProvider reloj)
    : IEntregaDurable
{
    public Task EntregarAsync(EventoPendiente evento, IObservadorEvento observador, CanalAviso canal, CancellationToken ct) =>
        unidad.EnTransaccionAsync(async token =>
        {
            if (await db.EntregasEvento.AnyAsync(e => e.EventoId == evento.Id && e.Suscriptor == observador.Nombre, token))
                return false;

            await observador.RecibirAsync(evento, canal, token);
            db.EntregasEvento.Add(new EntregaEvento
            {
                EventoId = evento.Id, Suscriptor = observador.Nombre, FechaUtc = reloj.GetUtcNow().UtcDateTime
            });
            return true;
        }, ct);
}
