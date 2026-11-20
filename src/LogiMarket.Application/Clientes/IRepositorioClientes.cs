using LogiMarket.Domain.Clientes;

namespace LogiMarket.Application.Clientes;

public interface IRepositorioClientes
{
    Task<Cliente?> BuscarPorIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken ct);
    Task<Cliente?> BuscarAsync(string identificacion, CancellationToken cancellationToken = default);
    void Agregar(Cliente cliente);
}
