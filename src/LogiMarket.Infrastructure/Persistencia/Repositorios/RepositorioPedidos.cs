using LogiMarket.Application.Pedidos;
using LogiMarket.Domain.Pedidos;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioPedidos(LogiMarketDbContext db) : IRepositorioPedidos
{
    public Task<Pedido?> BuscarAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Pedidos
            .Include(p => p.Lineas).ThenInclude(l => l.Componentes)
            .Include(p => p.Historial)
            .AsSplitQuery()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Agregar(Pedido pedido) => db.Pedidos.Add(pedido);
}
