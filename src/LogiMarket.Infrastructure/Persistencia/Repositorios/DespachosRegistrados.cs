using LogiMarket.Application.Entregas;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class DespachosRegistrados(LogiMarketDbContext db) : IDespachosRegistrados
{
    public async Task<DatosDespacho?> BuscarAsync(Guid pedidoId, CancellationToken ct)
    {
        var registro = await db.Despachos.AsNoTracking().SingleOrDefaultAsync(e => e.PedidoId == pedidoId, ct);
        return registro is null ? null : new(registro.PedidoId, registro.Guia, registro.PlazoDias,
            registro.FechaDespachoUtc, registro.FechaEntregaUtc);
    }

    public void Registrar(DatosDespacho despacho) => db.Despachos.Add(new DespachoRegistro
    {
        PedidoId = despacho.PedidoId, Guia = despacho.Guia, PlazoDias = despacho.PlazoDias,
        FechaDespachoUtc = despacho.FechaDespachoUtc
    });

    public async Task RegistrarEntregaAsync(Guid pedidoId, DateTime fecha, CancellationToken ct)
    {
        var registro = await db.Despachos.SingleAsync(e => e.PedidoId == pedidoId, ct);
        db.Entry(registro).Property(e => e.FechaEntregaUtc).CurrentValue = fecha;
    }
}
