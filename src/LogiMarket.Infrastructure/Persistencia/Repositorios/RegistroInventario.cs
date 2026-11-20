using LogiMarket.Application.Inventario;
using LogiMarket.Domain.Inventario;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class RegistroInventario(LogiMarketDbContext db) : IRegistroInventario
{
    public void Agregar(MovimientoInventario movimiento) => db.Movimientos.Add(movimiento);
}
