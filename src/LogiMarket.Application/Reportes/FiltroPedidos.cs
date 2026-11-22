using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Reportes;

// patron interpreter: cada nodo aplica la semantica de una expresion del filtro
public abstract record FiltroPedidos
{
    public abstract bool Cumple(PedidoReportado pedido);
}

public sealed record FiltroEstado(EstadoPedido Estado) : FiltroPedidos
{
    public override bool Cumple(PedidoReportado pedido) => pedido.Estado == Estado;
}

public sealed record FiltroTotal(decimal Minimo) : FiltroPedidos
{
    public override bool Cumple(PedidoReportado pedido) => pedido.TotalCobrado is decimal total && total >= Minimo;
}

public sealed record FiltroCliente(string Cliente) : FiltroPedidos
{
    public override bool Cumple(PedidoReportado pedido) =>
        string.Equals(pedido.Cliente, Cliente, StringComparison.OrdinalIgnoreCase);
}

public sealed record FiltroAnd(FiltroPedidos Izquierdo, FiltroPedidos Derecho) : FiltroPedidos
{
    public override bool Cumple(PedidoReportado pedido) => Izquierdo.Cumple(pedido) && Derecho.Cumple(pedido);
}

public sealed record FiltroOr(FiltroPedidos Izquierdo, FiltroPedidos Derecho) : FiltroPedidos
{
    public override bool Cumple(PedidoReportado pedido) => Izquierdo.Cumple(pedido) || Derecho.Cumple(pedido);
}
