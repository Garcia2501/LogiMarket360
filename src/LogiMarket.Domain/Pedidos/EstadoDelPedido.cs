namespace LogiMarket.Domain.Pedidos;

// patron state: cada estado admite solo las acciones que le corresponden
internal abstract class EstadoDelPedido
{
    private static readonly IReadOnlyDictionary<EstadoPedido, EstadoDelPedido> Estados =
        new Dictionary<EstadoPedido, EstadoDelPedido>
        {
            [EstadoPedido.BORRADOR] = new Borrador(),
            [EstadoPedido.VALIDADO] = new Validado(),
            [EstadoPedido.PAGADO] = new Pagado(),
            [EstadoPedido.DESPACHADO] = new Despachado(),
            [EstadoPedido.ENTREGADO] = new Cerrado(),
            [EstadoPedido.CANCELADO] = new Cerrado()
        };

    public static EstadoDelPedido Para(EstadoPedido estado) => Estados[estado];

    public virtual void ExigirEdicion() => throw Invalida();
    public virtual EstadoPedido Validar() => throw Invalida();
    public virtual EstadoPedido RegistrarPago() => throw Invalida();
    public virtual EstadoPedido Despachar() => throw Invalida();
    public virtual EstadoPedido Entregar() => throw Invalida();
    public virtual EstadoPedido Cancelar() => throw Invalida();

    private static InvalidOperationException Invalida() =>
        new("La operación no está permitida en el estado actual del pedido.");

    private sealed class Borrador : EstadoDelPedido
    {
        public override void ExigirEdicion() { }
        public override EstadoPedido Validar() => EstadoPedido.VALIDADO;
        public override EstadoPedido Cancelar() => EstadoPedido.CANCELADO;
    }

    private sealed class Validado : EstadoDelPedido
    {
        public override EstadoPedido RegistrarPago() => EstadoPedido.PAGADO;
        public override EstadoPedido Cancelar() => EstadoPedido.CANCELADO;
    }

    private sealed class Pagado : EstadoDelPedido
    {
        public override EstadoPedido Despachar() => EstadoPedido.DESPACHADO;
    }

    private sealed class Despachado : EstadoDelPedido
    {
        public override EstadoPedido Entregar() => EstadoPedido.ENTREGADO;
    }

    private sealed class Cerrado : EstadoDelPedido { }
}
