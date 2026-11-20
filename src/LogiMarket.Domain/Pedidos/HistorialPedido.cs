namespace LogiMarket.Domain.Pedidos;

public sealed class HistorialPedido
{
    private HistorialPedido() { }

    internal HistorialPedido(Guid pedidoId, int secuencia, EstadoPedido? anterior, EstadoPedido nuevo,
        Guid usuarioId, DateTime fechaUtc)
    {
        Id = Guid.NewGuid();
        PedidoId = pedidoId;
        Secuencia = secuencia;
        Anterior = anterior;
        Nuevo = nuevo;
        UsuarioId = usuarioId;
        FechaUtc = fechaUtc;
    }

    public Guid Id { get; private set; }
    public Guid PedidoId { get; private set; }
    public int Secuencia { get; private set; }
    public EstadoPedido? Anterior { get; private set; }
    public EstadoPedido Nuevo { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTime FechaUtc { get; private set; }
}
