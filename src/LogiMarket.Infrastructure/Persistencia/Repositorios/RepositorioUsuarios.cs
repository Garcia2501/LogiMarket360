using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioUsuarios(LogiMarketDbContext db) : IConsultaUsuarios
{
    public Task<Usuario?> BuscarAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var normalizado = Usuario.NormalizarNombre(nombre);
        return db.Usuarios.SingleOrDefaultAsync(e => e.Nombre == normalizado, cancellationToken);
    }
}
