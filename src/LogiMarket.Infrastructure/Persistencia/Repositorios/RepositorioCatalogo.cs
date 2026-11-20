using LogiMarket.Application.Catalogo;
using LogiMarket.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class RepositorioCatalogo(LogiMarketDbContext db) : IConsultaCatalogo, IEscrituraCatalogo
{
    public async Task<IReadOnlyList<ProductoSimple>> ListarProductosAsync(CancellationToken ct) =>
        await db.Productos.AsNoTracking().OrderBy(p => p.Sku).ToListAsync(ct);

    public Task<ProductoSimple?> BuscarProductoAsync(Sku sku, CancellationToken cancellationToken = default) =>
        db.Productos.SingleOrDefaultAsync(producto => producto.Sku == sku, cancellationToken);

    public async Task<KitCompuesto?> BuscarKitAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        var kit = await db.Kits.SingleOrDefaultAsync(e => e.Sku == sku, cancellationToken);
        if (kit is not null)
            await CargarComposicionAsync(kit, [], cancellationToken);
        return kit;
    }

    private async Task CargarComposicionAsync(KitCompuesto kit, HashSet<Guid> cargados,
        CancellationToken cancellationToken)
    {
        if (!cargados.Add(kit.Id))
            return;

        await db.Entry(kit).Collection(e => e.Componentes).Query()
            .Include(e => e.Producto)
            .Include(e => e.KitHijo)
            .LoadAsync(cancellationToken);

        foreach (var componente in kit.Componentes)
        {
            if (componente.KitHijo is { } hijo)
                await CargarComposicionAsync(hijo, cargados, cancellationToken);
        }
    }

    public void AgregarProducto(ProductoSimple producto) => db.Productos.Add(producto);

    public void AgregarKit(KitCompuesto kit) => db.Kits.Add(kit);
}
