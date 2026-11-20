using LogiMarket.Application.Clientes;
using LogiMarket.Domain.Clientes;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioClientes(LogiMarketDbContext db) : IRepositorioClientes
{
    public Task<Cliente?> BuscarPorIdAsync(Guid id, CancellationToken ct) =>
        db.Clientes.SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Cliente>> ListarAsync(CancellationToken ct) =>
        await db.Clientes.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(ct);

    public Task<Cliente?> BuscarAsync(string identificacion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificacion);
        return db.Clientes.SingleOrDefaultAsync(e => e.Identificacion == identificacion.Trim(), cancellationToken);
    }

    public void Agregar(Cliente cliente) => db.Clientes.Add(cliente);
}
