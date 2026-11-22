using LogiMarket.Application.Reportes;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class LecturaReportePedidos(LogiMarketDbContext db) : ILecturaReportePedidos
{
    public async Task<IReadOnlyList<PedidoReportado>> ListarAsync(CancellationToken ct) =>
        await (from pedido in db.Pedidos.AsNoTracking()
               join cliente in db.Clientes on pedido.ClienteId equals cliente.Id
               join pago in db.Pagos on pedido.Id equals pago.PedidoId into pagos
               from pago in pagos.DefaultIfEmpty()
               join devolucion in db.Devoluciones on pedido.Id equals devolucion.PedidoId into devoluciones
               from devolucion in devoluciones.DefaultIfEmpty()
               orderby pedido.FechaCreacionUtc descending, pedido.Id
               select new PedidoReportado(pedido.Id, pedido.Estado, cliente.Nombre,
                   pago == null ? null : pago.Importe, pedido.FechaCreacionUtc, pago == null ? null : pago.FechaUtc,
                   devolucion == null ? null : devolucion.FechaUtc, devolucion == null ? null : devolucion.ImporteReembolso,
                   pedido.Entrega)).ToListAsync(ct);
}
