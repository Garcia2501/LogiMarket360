using LogiMarket.Domain.Inventario;

namespace LogiMarket.Application.Inventario;

public interface IRegistroInventario
{
    void Agregar(MovimientoInventario movimiento);
}
