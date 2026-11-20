using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;

namespace LogiMarket.Application.Comandos;

public sealed class OperacionesPedido(InvocadorComandos invocador, ConfirmacionPedidoFacade confirmacion,
    AdministrarPedidos pedidos, DespacharPedidos despachos, AdministrarDevoluciones devoluciones)
{
    public Task<PagoAplicado> ConfirmarAsync(ConfirmarPedidoCommand comando, CancellationToken ct = default) =>
        invocador.EjecutarAsync(comando, (c, token) => confirmacion.ConfirmarAsync(c.PedidoId, c.Clave, c.Proveedor, token), ct);

    public Task CancelarAsync(CancelarPedidoCommand comando, CancellationToken ct = default) =>
        invocador.EjecutarAsync(comando, async (c, token) =>
        {
            await pedidos.CancelarAsync(c.PedidoId, token);
            return true;
        }, ct);

    public Task<DatosDespacho> DespacharAsync(DespacharPedidoCommand comando, CancellationToken ct = default) =>
        invocador.EjecutarAsync(comando, (c, token) => despachos.DespacharAsync(c.PedidoId, token), ct);

    public Task<DevolucionAplicada> DevolverAsync(ProcesarDevolucionCommand comando, CancellationToken ct = default) =>
        invocador.EjecutarAsync(comando, (c, token) => devoluciones.DevolverAsync(c.PedidoId, c.Motivo, c.ProductoSinUso, token), ct);
}
