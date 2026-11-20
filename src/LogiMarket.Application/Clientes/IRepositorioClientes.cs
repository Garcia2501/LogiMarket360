using LogiMarket.Domain.Clientes;

namespace LogiMarket.Application.Clientes;

public interface IRepositorioClientes
{
    Task<Cliente?> BuscarAsync(string identificacion, CancellationToken cancellationToken = default);
    void Agregar(Cliente cliente);
}
