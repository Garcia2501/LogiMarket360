using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Pagos;

namespace LogiMarket.Application.Comandos;

public interface IComandoPedido
{
    Guid PedidoId { get; }
    string Detalle { get; }
}

public sealed record ConfirmarPedidoCommand(Guid PedidoId, string Clave, ProveedorPago Proveedor) : IComandoPedido
{
    public string Detalle => Proveedor.ToString();
}

public sealed record CancelarPedidoCommand(Guid PedidoId) : IComandoPedido
{
    public string Detalle => "";
}

public sealed record DespacharPedidoCommand(Guid PedidoId) : IComandoPedido
{
    public string Detalle => "";
}

public sealed record ProcesarDevolucionCommand(Guid PedidoId, MotivoDevolucion Motivo, bool ProductoSinUso) : IComandoPedido
{
    public string Detalle => $"{Motivo}/{(ProductoSinUso ? "SIN_USO" : "USADO")}";
}
