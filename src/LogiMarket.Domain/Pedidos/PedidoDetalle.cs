using LogiMarket.Domain.Catalogo;

namespace LogiMarket.Domain.Pedidos;

public sealed class PedidoDetalle
{
    private readonly List<PedidoDetalleComponente> _componentes = [];

    private PedidoDetalle() { }

    internal PedidoDetalle(Guid pedidoId, IElementoCatalogo elemento, int cantidad)
    {
        ArgumentNullException.ThrowIfNull(elemento);
        var snapshot = elemento.Capturar();
        Id = Guid.NewGuid();
        PedidoId = pedidoId;
        Sku = snapshot.Sku;
        Nombre = snapshot.Nombre;
        PrecioUnitario = snapshot.PrecioUnitario;
        RegistrarComponente(snapshot, "0", 1);
        CambiarCantidad(cantidad);
    }

    public Guid Id { get; private set; }
    public Guid PedidoId { get; private set; }
    public Sku Sku { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public decimal PrecioUnitario { get; private set; }
    public int Cantidad { get; private set; }
    public decimal Subtotal => PrecioUnitario * Cantidad;
    public IReadOnlyList<PedidoDetalleComponente> Componentes => _componentes.AsReadOnly();

    internal PedidoDetalle CopiarPara(Guid pedidoId)
    {
        var copia = new PedidoDetalle
        {
            Id = Guid.NewGuid(), PedidoId = pedidoId, Sku = Sku, Nombre = Nombre,
            PrecioUnitario = PrecioUnitario, Cantidad = Cantidad
        };
        copia._componentes.AddRange(_componentes.Select(c => c.CopiarPara(copia.Id)));
        return copia;
    }

    internal void CambiarCantidad(int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);
        Cantidad = cantidad;
    }

    private void RegistrarComponente(ComponenteCatalogoSnapshot nodo, string ruta, int multiplicador)
    {
        int cantidad = checked(nodo.Cantidad * multiplicador);
        _componentes.Add(new PedidoDetalleComponente(Id, ruta, nodo, cantidad));
        for (int i = 0; i < nodo.Componentes.Count; i++)
            RegistrarComponente(nodo.Componentes[i], $"{ruta}/{i}", cantidad);
    }
}
