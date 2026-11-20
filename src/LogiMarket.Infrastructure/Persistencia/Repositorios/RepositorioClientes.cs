using LogiMarket.Application.Clientes;
using LogiMarket.Domain.Clientes;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioClientes(LogiMarketDbContext db) : IRepositorioClientes
{
    public Task<Cliente?> BuscarAsync(string identificacion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificacion);
        return db.Clientes.SingleOrDefaultAsync(e => e.Identificacion == identificacion.Trim(), cancellationToken);
    }

    public void Agregar(Cliente cliente) => db.Clientes.Add(cliente);
}
