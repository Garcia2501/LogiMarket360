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
