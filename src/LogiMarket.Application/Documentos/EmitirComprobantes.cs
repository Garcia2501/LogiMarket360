using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Seguridad;

namespace LogiMarket.Application.Documentos;

public enum TipoComprobante
{
    Venta,
    Devolucion
}

public interface IConsultaDevolucion
{
    Task<DevolucionAplicada?> BuscarAsync(Guid pedidoId, CancellationToken ct);
}

public sealed class EmitirComprobantes(AutorizacionOperaciones acceso, IRepositorioPedidos pedidos,
    IPagosRegistrados pagos, IConsultaDevolucion devoluciones)
{
    public async Task EjecutarAsync(Guid id, TipoComprobante tipo, ISalidaDocumento salida, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        ArgumentNullException.ThrowIfNull(salida);

        Comprobante comprobante = tipo switch
        {
            TipoComprobante.Venta => new ComprobanteVenta(salida,
                await pedidos.BuscarAsync(id, ct) ?? throw new KeyNotFoundException("El pedido no existe."),
                await pagos.BuscarPorPedidoAsync(id, ct) ?? throw new InvalidOperationException("El pedido aún no fue pagado.")),
            TipoComprobante.Devolucion => new ComprobanteDevolucion(salida,
                await devoluciones.BuscarAsync(id, ct) ?? throw new InvalidOperationException("El pedido aún no fue devuelto.")),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo))
        };

        await comprobante.EmitirAsync(ct);
    }
}
