using LogiMarket.Domain.Catalogo;

namespace LogiMarket.Application.Catalogo;

public interface IConsultaCatalogo
{
    Task<ProductoSimple?> BuscarProductoAsync(Sku sku, CancellationToken cancellationToken = default);
    Task<KitCompuesto?> BuscarKitAsync(Sku sku, CancellationToken cancellationToken = default);
}

public interface IEscrituraCatalogo
{
    void AgregarProducto(ProductoSimple producto);
    void AgregarKit(KitCompuesto kit);
}
