using System.Collections.ObjectModel;

namespace LogiMarket.Domain.Catalogo;

public sealed class ComponenteCatalogoSnapshot
{
    internal ComponenteCatalogoSnapshot(Sku sku, string nombre, decimal precioUnitario, decimal pesoKg,
        int cantidad, IEnumerable<ComponenteCatalogoSnapshot> componentes)
    {
        Sku = sku;
        Nombre = nombre;
        PrecioUnitario = precioUnitario;
        PesoKg = pesoKg;
        Cantidad = cantidad;
        Componentes = Array.AsReadOnly(componentes.ToArray());
    }

    public Sku Sku { get; }
    public string Nombre { get; }
    public decimal PrecioUnitario { get; }
    public decimal PesoKg { get; }
    public int Cantidad { get; }
    public IReadOnlyList<ComponenteCatalogoSnapshot> Componentes { get; }

    internal ComponenteCatalogoSnapshot ConCantidad(int cantidad) =>
        new(Sku, Nombre, PrecioUnitario, PesoKg, cantidad, Componentes);

    public IReadOnlyDictionary<Sku, int> Expandir(int cantidad = 1)
    {
        var unidades = IteradorComponentesFisicos.Recorrer(this, cantidad)
            .GroupBy(c => c.Sku)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Cantidad));
        return new ReadOnlyDictionary<Sku, int>(unidades);
    }
}
