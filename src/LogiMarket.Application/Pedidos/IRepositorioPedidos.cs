using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Pedidos;

public interface IRepositorioPedidos
{
    Task<Pedido?> BuscarAsync(Guid id, CancellationToken cancellationToken = default);
    void Agregar(Pedido pedido);
}
