using System.Collections.ObjectModel;

namespace LogiMarket.Domain.Catalogo;

// patron visitor: separa el desglose y el peso de los elementos del catalogo
public interface IVisitanteCatalogo
{
    void Visitar(ProductoSimple producto, int cantidad);
    void Visitar(KitCompuesto kit, int cantidad);
}

public abstract class VisitanteComponentes : IVisitanteCatalogo
{
    private readonly HashSet<Guid> _ruta = [];

    public abstract void Visitar(ProductoSimple producto, int cantidad);

    public void Visitar(KitCompuesto kit, int cantidad)
    {
        if (!_ruta.Add(kit.Id))
            throw new InvalidOperationException("La composición cargada contiene un ciclo.");
        try
        {
            foreach (var componente in kit.Componentes)
                componente.Elemento.Aceptar(this, checked(cantidad * componente.Cantidad));
        }
        finally
        {
            _ruta.Remove(kit.Id);
        }
    }
}

public sealed class VisitanteDesgloseSku : VisitanteComponentes
{
    private readonly Dictionary<Sku, int> _cantidades = [];

    public IReadOnlyDictionary<Sku, int> Cantidades => new ReadOnlyDictionary<Sku, int>(_cantidades);

    public override void Visitar(ProductoSimple producto, int cantidad) =>
        _cantidades[producto.Sku] = checked(_cantidades.GetValueOrDefault(producto.Sku) + cantidad);
}

public sealed class VisitantePesoLogistico : VisitanteComponentes
{
    public decimal PesoKg { get; private set; }

    public override void Visitar(ProductoSimple producto, int cantidad) =>
        PesoKg += producto.PesoKg * cantidad;
}
