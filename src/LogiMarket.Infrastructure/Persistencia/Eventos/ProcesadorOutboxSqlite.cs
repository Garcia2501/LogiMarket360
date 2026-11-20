using System.Text.Json;
using LogiMarket.Application.Eventos;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Eventos;

public sealed class ProcesadorOutboxSqlite(LogiMarketDbContext db, PublicadorEventos publicador, TimeProvider reloj)
    : IProcesadorOutbox
{
    public async Task<ResumenOutbox> ProcesarAsync(CanalAviso canal, CancellationToken ct)
    {
        var pendientes = await db.EventosOutbox.AsNoTracking().Where(e => e.ProcesadoUtc == null)
            .OrderBy(e => e.FechaUtc).ThenBy(e => e.Id).Take(100).ToListAsync(ct);
        int procesados = 0;
        int fallidos = 0;

        foreach (var pendiente in pendientes)
        {
            var porProcesar = Pendiente(pendiente);
            if (await porProcesar.ExecuteUpdateAsync(s => s.SetProperty(e => e.Intentos, e => e.Intentos + 1), ct) == 0)
                continue;

            try
            {
                var datos = JsonSerializer.Deserialize<EventoComercial>(pendiente.Contenido)
                    ?? throw new JsonException("El evento no contiene datos.");
                await publicador.PublicarAsync(new EventoPendiente(pendiente.Id, datos), canal, ct);
                procesados += await porProcesar.ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.ProcesadoUtc, reloj.GetUtcNow().UtcDateTime)
                    .SetProperty(e => e.UltimoError, (string?)null), ct);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                await porProcesar.ExecuteUpdateAsync(s => s.SetProperty(e => e.UltimoError, error.GetType().Name), ct);
                fallidos++;
            }
        }

        return new(procesados, fallidos);
    }

    private IQueryable<EventoOutbox> Pendiente(EventoOutbox evento) =>
        db.EventosOutbox.Where(e => e.Id == evento.Id && e.ProcesadoUtc == null);
}
