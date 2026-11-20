namespace LogiMarket.Domain.Pedidos;

public enum EstadoPedido
{
    BORRADOR = 1,
    VALIDADO,
    PAGADO,
    DESPACHADO,
    ENTREGADO,
    CANCELADO
}

public enum ModalidadEntrega
{
    ESTANDAR = 1,
    EXPRES
}
