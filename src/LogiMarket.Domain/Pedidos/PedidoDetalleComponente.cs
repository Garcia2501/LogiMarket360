using LogiMarket.Domain.Catalogo;

namespace LogiMarket.Domain.Pedidos;

public sealed class PedidoDetalleComponente
{
    private PedidoDetalleComponente() { }

    internal PedidoDetalleComponente(Guid detalleId, string ruta, ComponenteCatalogoSnapshot nodo,
        int cantidadPorUnidad)
    {
        Id = Guid.NewGuid();
        PedidoDetalleId = detalleId;
        Ruta = ruta;
        Sku = nodo.Sku;
        Nombre = nodo.Nombre;
        CantidadEnPadre = nodo.Cantidad;
        CantidadPorUnidad = cantidadPorUnidad;
        PrecioUnitario = nodo.PrecioUnitario;
        PesoKg = nodo.PesoKg;
        EsProducto = nodo.Componentes.Count == 0;
    }

    public Guid Id { get; private set; }
    public Guid PedidoDetalleId { get; private set; }
    public string Ruta { get; private set; } = null!;
    public Sku Sku { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public int CantidadEnPadre { get; private set; }
    public int CantidadPorUnidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal PesoKg { get; private set; }
    public bool EsProducto { get; private set; }
}
