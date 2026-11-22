using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Clientes;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Precios;
using LogiMarket.Application.Reportes;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Inventario;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Escritorio;

public sealed record KitVisto(string Sku, string Nombre);
public sealed record MovimientoVisto(Guid Id, DateTime FechaUtc, string Sku, TipoMovimiento Tipo, int Cantidad,
    int Saldo, string Motivo, string Referencia);
public sealed record AuditoriaVista(Guid Id, DateTime FechaUtc, string Categoria, string Accion, string Resultado,
    string Correlacion, string? Referencia);
public sealed record OutboxVisto(Guid Id, DateTime FechaUtc, string Tipo, int Intentos, DateTime? ProcesadoUtc, string? UltimoError);
public sealed record IndicadoresSesion(int Ventas, int Despachos, int Devoluciones, int Pendientes);
public sealed record DetallePedidoVisto(Guid Id, string Cliente, EstadoPedido Estado, ModalidadEntrega Entrega,
    IReadOnlyList<LineaVista> Lineas, ValoracionPedido Valoracion, PagoAplicado? Pago,
    IReadOnlyList<CambioEstadoVisto> Historial, DatosDespacho? Despacho);
public sealed record ResumenGeneral(int Activos, int PorDespachar, int StockBajo, decimal Ventas,
    IndicadoresSesion Sesion, IReadOnlyList<PedidoReportado> Recientes);
public sealed record ResumenPeriodo(IReadOnlyList<PedidoReportado> Filas, int Cobros, int Devoluciones, decimal VentasNetas);

public interface ILecturaEscritorio
{
    Task<IReadOnlyList<KitVisto>> KitsAsync(CancellationToken ct);
    Task<IReadOnlyList<MovimientoVisto>> MovimientosAsync(string? sku, CancellationToken ct);
    Task<IReadOnlyList<AuditoriaVista>> AuditoriaAsync(string? categoria, CancellationToken ct);
    Task<IReadOnlyList<OutboxVisto>> OutboxAsync(CancellationToken ct);
    Task<int> PendientesAsync(CancellationToken ct);
}

public sealed class ConsultarEscritorio(AutorizacionOperaciones acceso, ILecturaEscritorio datos,
    IRepositorioPedidos pedidos, IRepositorioClientes clientes, IPagosRegistrados pagos, IDespachosRegistrados despachos,
    ValorarPedido valorar, ConsultarReportePedidos reporte, AdministrarCatalogo catalogo, MetricasSesion metricas)
{
    public async Task<IReadOnlyList<KitVisto>> KitsAsync(CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        return await datos.KitsAsync(ct);
    }

    public async Task<IReadOnlyList<MovimientoVisto>> MovimientosAsync(string? sku, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        return await datos.MovimientosAsync(sku, ct);
    }

    public async Task<IReadOnlyList<AuditoriaVista>> AuditoriaAsync(string? categoria, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.ConsultarAuditoria, ct);
        return await datos.AuditoriaAsync(categoria, ct);
    }

    public async Task<IReadOnlyList<OutboxVisto>> OutboxAsync(CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.ConsultarAuditoria, ct);
        return await datos.OutboxAsync(ct);
    }

    public async Task<IndicadoresSesion> IndicadoresAsync(CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        return new(metricas.Contar(EventoComercial.PedidoPagado), metricas.Contar(EventoComercial.DespachoRegistrado),
            metricas.Contar(EventoComercial.DevolucionAceptada), await datos.PendientesAsync(ct));
    }

    public async Task<ResumenGeneral> ResumenAsync(CancellationToken ct = default)
    {
        var filas = await reporte.EjecutarAsync(null, ct);
        var productos = await catalogo.BuscarAsync(ct: ct);
        return new(filas.Count(p => p.Estado is not (EstadoPedido.CANCELADO or EstadoPedido.ENTREGADO)),
            filas.Count(p => p.Estado == EstadoPedido.PAGADO), productos.Count(p => p.Activo && p.Stock <= 3),
            filas.Sum(p => p.TotalCobrado ?? 0m), await IndicadoresAsync(ct), filas.Take(12).ToArray());
    }

    public async Task<DetallePedidoVisto> DetalleAsync(Guid id, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        var pedido = await pedidos.BuscarAsync(id, ct) ?? throw new KeyNotFoundException("El pedido no existe.");
        var cliente = await clientes.BuscarPorIdAsync(pedido.ClienteId, ct)
            ?? throw new KeyNotFoundException("El cliente no existe.");
        var pago = await pagos.BuscarPorPedidoAsync(id, ct);
        var valor = pago is null
            ? valorar.Calcular(pedido, cliente.Categoria)
            : new ValoracionPedido(pago.Subtotal, pago.Descuento, pago.Tarifa, pago.Importe, "",
                CantidadesFisicas.DelPedido(pedido).Values.Sum());
        return new(id, cliente.Nombre, pedido.Estado, pedido.Entrega,
            pedido.Lineas.Select(l => new LineaVista(l.Id, l.Sku.Valor, l.Nombre, l.PrecioUnitario, l.Cantidad)).ToArray(),
            valor, pago, pedido.Historial.Select(h => new CambioEstadoVisto(h.Nuevo, h.UsuarioId, h.FechaUtc)).ToArray(),
            await despachos.BuscarAsync(id, ct));
    }

    public async Task<ResumenPeriodo> PeriodoAsync(DateTime desdeUtc, DateTime hastaUtc, string? filtro, CancellationToken ct = default)
    {
        if (hastaUtc <= desdeUtc)
            throw new ArgumentException("La fecha inicial debe ser anterior a la fecha final.");
        var pedidosDelPeriodo = await reporte.EjecutarAsync(filtro, ct);
        bool Incluida(DateTime? fecha) => fecha >= desdeUtc && fecha < hastaUtc;
        var cobros = pedidosDelPeriodo.Where(p => Incluida(p.FechaPagoUtc)).ToArray();
        var retornos = pedidosDelPeriodo.Where(p => Incluida(p.FechaDevolucionUtc)).ToArray();
        return new(pedidosDelPeriodo.Where(p => Incluida(p.FechaCreacionUtc) || Incluida(p.FechaPagoUtc) ||
            Incluida(p.FechaDevolucionUtc)).ToArray(), cobros.Length, retornos.Length,
            cobros.Sum(p => p.TotalCobrado ?? 0m) - retornos.Sum(p => p.ImporteReembolso ?? 0m));
    }
}
