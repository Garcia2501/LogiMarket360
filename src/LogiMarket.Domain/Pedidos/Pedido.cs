using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;

namespace LogiMarket.Domain.Pedidos;

public sealed partial class Pedido
{
    private readonly List<PedidoDetalle> _lineas = [];
    private readonly List<HistorialPedido> _historial = [];

    private Pedido() { }

    public Pedido(Cliente cliente, ModalidadEntrega entrega, IElementoCatalogo primerElemento, int cantidad,
        Guid usuarioId, DateTimeOffset fecha)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        if (!cliente.Activo)
            throw new InvalidOperationException("El cliente no está activo.");

        Id = Guid.NewGuid();
        ClienteId = cliente.Id;
        CambiarEntrega(entrega);
        AgregarLinea(primerElemento, cantidad);
        Transicionar(EstadoPedido.BORRADOR, usuarioId, fecha, anterior: null);
    }

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public EstadoPedido Estado { get; private set; } = EstadoPedido.BORRADOR;
    public ModalidadEntrega Entrega { get; private set; }
    public Guid Version { get; private set; }
    public DateTime FechaCreacionUtc { get; private set; }
    public IReadOnlyList<PedidoDetalle> Lineas => _lineas.AsReadOnly();
    public IReadOnlyList<HistorialPedido> Historial => _historial.OrderBy(h => h.Secuencia).ToArray();
    public decimal Subtotal => _lineas.Sum(linea => linea.Subtotal);

    public void CambiarEntrega(ModalidadEntrega entrega)
    {
        EstadoActual.ExigirEdicion();
        if (!Enum.IsDefined(entrega))
            throw new ArgumentOutOfRangeException(nameof(entrega));

        Entrega = entrega;
        Version = Guid.NewGuid();
    }

    public PedidoDetalle AgregarLinea(IElementoCatalogo elemento, int cantidad)
    {
        EstadoActual.ExigirEdicion();
        var linea = new PedidoDetalle(Id, elemento, cantidad);
        _lineas.Add(linea);
        Version = Guid.NewGuid();
        return linea;
    }

    public void CambiarCantidad(Guid lineaId, int cantidad)
    {
        EstadoActual.ExigirEdicion();
        BuscarLinea(lineaId).CambiarCantidad(cantidad);
        Version = Guid.NewGuid();
    }

    public void QuitarLinea(Guid lineaId)
    {
        EstadoActual.ExigirEdicion();
        if (_lineas.Count == 1)
            throw new InvalidOperationException("El pedido debe conservar al menos una línea.");

        _lineas.Remove(BuscarLinea(lineaId));
        Version = Guid.NewGuid();
    }

    public void Validar(Guid usuarioId, DateTimeOffset fecha) =>
        Transicionar(EstadoActual.Validar(), usuarioId, fecha, Estado);

    public void RegistrarPagoAprobado(Guid usuarioId, DateTimeOffset fecha) =>
        Transicionar(EstadoActual.RegistrarPago(), usuarioId, fecha, Estado);

    public void RegistrarDespacho(Guid usuarioId, DateTimeOffset fecha) =>
        Transicionar(EstadoActual.Despachar(), usuarioId, fecha, Estado);

    public void RegistrarEntrega(Guid usuarioId, DateTimeOffset fecha) =>
        Transicionar(EstadoActual.Entregar(), usuarioId, fecha, Estado);

    public void Cancelar(Guid usuarioId, DateTimeOffset fecha) =>
        Transicionar(EstadoActual.Cancelar(), usuarioId, fecha, Estado);

    private EstadoDelPedido EstadoActual => EstadoDelPedido.Para(Estado);

    private PedidoDetalle BuscarLinea(Guid id) =>
        _lineas.SingleOrDefault(l => l.Id == id)
        ?? throw new InvalidOperationException("La línea no pertenece al pedido.");

    private void Transicionar(EstadoPedido nuevo, Guid usuarioId, DateTimeOffset fecha, EstadoPedido? anterior)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("Se requiere un usuario para registrar el estado.", nameof(usuarioId));

        if (anterior is null)
            FechaCreacionUtc = fecha.UtcDateTime;

        _historial.Add(new HistorialPedido(Id, _historial.Count + 1, anterior, nuevo, usuarioId, fecha.UtcDateTime));
        Estado = nuevo;
        Version = Guid.NewGuid();
    }
}
