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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);
        var unidades = new Dictionary<Sku, int>();
        Acumular(cantidad, unidades);
        return new ReadOnlyDictionary<Sku, int>(unidades);
    }

    private void Acumular(int multiplicador, Dictionary<Sku, int> unidades)
    {
        int total = checked(multiplicador * Cantidad);
        if (Componentes.Count == 0)
        {
            unidades[Sku] = checked(unidades.GetValueOrDefault(Sku) + total);
            return;
        }

        foreach (var componente in Componentes)
            componente.Acumular(total, unidades);
    }
}
