using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Documentos;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class ConsultaDevolucion(LogiMarketDbContext db) : IConsultaDevolucion
{
    public async Task<DevolucionAplicada?> BuscarAsync(Guid pedidoId, CancellationToken ct)
    {
        var registro = await db.Devoluciones.AsNoTracking().SingleOrDefaultAsync(d => d.PedidoId == pedidoId, ct);
        return registro is null ? null : new(registro.PedidoId, Enum.Parse<MotivoDevolucion>(registro.Motivo),
            registro.ProductoSinUso, registro.ReintegroInventario, registro.ReferenciaReembolso,
            registro.ImporteReembolso, registro.FechaUtc);
    }
}
