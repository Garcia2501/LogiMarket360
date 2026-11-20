namespace LogiMarket.Domain.Catalogo;

public sealed class KitComponente
{
    private KitComponente() { }

    internal KitComponente(Guid kitId, IElementoCatalogo elemento, int cantidad)
    {
        Id = Guid.NewGuid();
        KitId = kitId;
        switch (elemento)
        {
            case ProductoSimple producto:
                Producto = producto;
                ProductoId = producto.Id;
                break;
            case KitCompuesto kit:
                KitHijo = kit;
                KitHijoId = kit.Id;
                break;
            default:
                throw new ArgumentException("Tipo de componente no admitido.", nameof(elemento));
        }
        CambiarCantidad(cantidad);
    }

    public Guid Id { get; private set; }
    public Guid KitId { get; private set; }
    public Guid? ProductoId { get; private set; }
    public Guid? KitHijoId { get; private set; }
    public int Cantidad { get; private set; }
    public ProductoSimple? Producto { get; private set; }
    public KitCompuesto? KitHijo { get; private set; }

    public IElementoCatalogo Elemento => Producto ?? (IElementoCatalogo?)KitHijo
        ?? throw new InvalidOperationException("El componente del kit no se ha cargado.");

    internal void CambiarCantidad(int cantidad)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cantidad);
        Cantidad = cantidad;
    }
}
