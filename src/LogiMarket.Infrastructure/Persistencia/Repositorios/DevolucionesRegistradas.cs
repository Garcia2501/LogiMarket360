using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Pagos;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class DevolucionesRegistradas(LogiMarketDbContext db) : IDevolucionesRegistradas
{
    public Task<bool> ExisteAsync(Guid pedidoId, CancellationToken ct) =>
        db.Devoluciones.AnyAsync(d => d.PedidoId == pedidoId, ct);

    public void Registrar(DevolucionAplicada datos) => db.Devoluciones.Add(new DevolucionRegistro
    {
        PedidoId = datos.PedidoId, Motivo = datos.Motivo.ToString(), ProductoSinUso = datos.ProductoSinUso,
        ReintegroInventario = datos.ReintegroInventario, ReferenciaReembolso = datos.ReferenciaReembolso,
        ImporteReembolso = datos.Importe, FechaUtc = datos.FechaUtc
    });
}

public sealed class ReembolsoSimulado : IReembolsoSimulado
{
    public string Reembolsar(PagoAplicado pago, decimal importe)
    {
        if (importe < 0m || importe != pago.Importe - pago.Tarifa)
            throw new ArgumentOutOfRangeException(nameof(importe));
        return $"DEV-{pago.Referencia}";
    }
}
