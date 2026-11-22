namespace LogiMarket.Application.Catalogo;

public sealed record MetadatosProductoVisual(string Sku, string Nombre, string? Categoria, string? Descripcion);

public sealed record RenglonCatalogoVisual(MetadatosProductoVisual Metadatos, int Stock, int Cantidad, decimal Precio)
{
    public string Sku => Metadatos.Sku;
    public string Nombre => Metadatos.Nombre;
    public string? Categoria => Metadatos.Categoria;
    public string? Descripcion => Metadatos.Descripcion;
}

// patron flyweight: comparte metadatos inmutables por sku dentro de cada proyeccion
public sealed class CatalogoVisualFlyweightFactory
{
    private readonly Dictionary<string, MetadatosProductoVisual> _metadatos = new(StringComparer.Ordinal);

    public RenglonCatalogoVisual CrearRenglon(ProductoVisto producto, int cantidad)
    {
        if (!_metadatos.TryGetValue(producto.Sku, out var metadatos) ||
            metadatos.Nombre != producto.Nombre || metadatos.Categoria != producto.Categoria ||
            metadatos.Descripcion != producto.Descripcion)
        {
            metadatos = CopiarMetadatos(producto);
            _metadatos[producto.Sku] = metadatos;
        }
        return new(metadatos, producto.Stock, cantidad, producto.Precio);
    }

    internal static MetadatosProductoVisual CopiarMetadatos(ProductoVisto p) => new(p.Sku, p.Nombre, p.Categoria, p.Descripcion);
}
