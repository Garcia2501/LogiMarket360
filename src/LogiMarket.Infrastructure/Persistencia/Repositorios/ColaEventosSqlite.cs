using System.Text.Json;
using LogiMarket.Application.Eventos;
using LogiMarket.Infrastructure.Persistencia.Operaciones;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class ColaEventosSqlite(LogiMarketDbContext db) : IColaEventos
{
    public void Agregar(EventoComercial evento) => db.EventosOutbox.Add(new EventoOutbox
    {
        PedidoId = evento.PedidoId, Tipo = evento.Tipo, Contenido = JsonSerializer.Serialize(evento), FechaUtc = evento.FechaUtc
    });
}
