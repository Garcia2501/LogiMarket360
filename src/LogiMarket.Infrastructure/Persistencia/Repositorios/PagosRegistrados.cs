using LogiMarket.Application.Pagos;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class PagosRegistrados(LogiMarketDbContext db) : IPagosRegistrados
{
    public async Task<PagoAplicado?> BuscarPorClaveAsync(string clave, CancellationToken ct) =>
        Proyectar(await db.Pagos.AsNoTracking().SingleOrDefaultAsync(p => p.ClaveIdempotencia == clave, ct));

    public async Task<PagoAplicado?> BuscarPorPedidoAsync(Guid pedidoId, CancellationToken ct) =>
        Proyectar(await db.Pagos.AsNoTracking().SingleOrDefaultAsync(p => p.PedidoId == pedidoId, ct));

    public void Registrar(PagoAplicado pago) => db.Pagos.Add(new PagoRegistro
    {
        PedidoId = pago.PedidoId, Referencia = pago.Referencia, ClaveIdempotencia = pago.Clave, Proveedor = pago.Proveedor,
        Subtotal = pago.Subtotal, Descuento = pago.Descuento, Tarifa = pago.Tarifa, Importe = pago.Importe, FechaUtc = pago.FechaUtc
    });

    private static PagoAplicado? Proyectar(PagoRegistro? p) => p is null ? null : new(p.PedidoId, p.Referencia,
        p.ClaveIdempotencia, p.Proveedor, p.Subtotal, p.Descuento, p.Tarifa, p.Importe, p.FechaUtc);
}
