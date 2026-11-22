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
               orderby pedido.FechaCreacionUtc descending, pedido.Id
               select new PedidoReportado(pedido.Id, pedido.Estado, cliente.Nombre,
                   pago == null ? null : pago.Importe)).ToListAsync(ct);
}
