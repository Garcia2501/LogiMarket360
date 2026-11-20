using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Inventario;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Persistencia;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Inventario;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Devoluciones;

// patron template method: fija elegibilidad inspeccion reembolso registro reintegro y evento
public abstract class ProcesarDevolucion
{
    public abstract MotivoDevolucion Motivo { get; }
    protected abstract int DiasPermitidos { get; }
    protected abstract void Inspeccionar(bool productoSinUso);

    internal async Task<DevolucionAplicada> EjecutarAsync(ContextoDevolucion contexto, Guid id,
        bool productoSinUso, CancellationToken ct)
    {
        var pedido = await contexto.Pedidos.BuscarAsync(id, ct) ?? throw new KeyNotFoundException("El pedido no existe.");
        if (pedido.Estado != EstadoPedido.ENTREGADO)
            throw new RechazoPedidoException("ESTADO", "Solo se devuelve un pedido entregado.");
        if (await contexto.Devoluciones.ExisteAsync(id, ct))
            throw new RechazoPedidoException("DEVUELTO", "El pedido ya fue devuelto.");

        var despacho = await contexto.Despachos.BuscarAsync(id, ct);
        var ahora = contexto.Reloj.GetUtcNow();
        if (despacho?.FechaEntregaUtc is not { } fechaEntrega || ahora.UtcDateTime < fechaEntrega ||
            DateOnly.FromDateTime(ahora.UtcDateTime).DayNumber - DateOnly.FromDateTime(fechaEntrega).DayNumber > DiasPermitidos)
        {
            throw new RechazoPedidoException("PLAZO", "La devolución está fuera del plazo permitido.");
        }
        Inspeccionar(productoSinUso);

        var pago = await contexto.Pagos.BuscarPorPedidoAsync(id, ct)
            ?? throw new RechazoPedidoException("PAGO", "El pedido no tiene un pago aprobado.");
        var importe = pago.Importe - pago.Tarifa;
        var referencia = contexto.Reembolso.Reembolsar(pago, importe);
        var devolucion = new DevolucionAplicada(id, Motivo, productoSinUso, true, referencia, importe, ahora.UtcDateTime);
        contexto.Devoluciones.Registrar(devolucion);

        foreach (var unidad in CantidadesFisicas.DelPedido(pedido))
        {
            var producto = await contexto.Catalogo.BuscarProductoAsync(unidad.Key, ct)
                ?? throw new RechazoPedidoException("SKU", $"El SKU {unidad.Key} requerido para el reintegro no existe.");
            contexto.Inventario.Agregar(producto.MoverInventario(unidad.Value, TipoMovimiento.DEVOLUCION,
                Motivo.ToString(), referencia, ahora));
        }
        contexto.Eventos.Agregar(new EventoComercial(id, EventoComercial.DevolucionAceptada, referencia, importe, ahora.UtcDateTime));
        return devolucion;
    }
}

public sealed class DevolucionPorDefecto : ProcesarDevolucion
{
    public override MotivoDevolucion Motivo => MotivoDevolucion.DEFECTO;
    protected override int DiasPermitidos => 30;
    protected override void Inspeccionar(bool productoSinUso) { }
}

public sealed class DevolucionPorArrepentimiento : ProcesarDevolucion
{
    public override MotivoDevolucion Motivo => MotivoDevolucion.ARREPENTIMIENTO;
    protected override int DiasPermitidos => 7;

    protected override void Inspeccionar(bool productoSinUso)
    {
        if (!productoSinUso)
            throw new RechazoPedidoException("CONDICION", "El producto devuelto por arrepentimiento debe estar sin uso.");
    }
}

public sealed record ContextoDevolucion(IRepositorioPedidos Pedidos, IDespachosRegistrados Despachos,
    IDevolucionesRegistradas Devoluciones, IPagosRegistrados Pagos, IConsultaCatalogo Catalogo,
    IRegistroInventario Inventario, IReembolsoSimulado Reembolso, IColaEventos Eventos, TimeProvider Reloj);

public sealed class AdministrarDevoluciones(IEnumerable<ProcesarDevolucion> politicas, ContextoDevolucion contexto,
    IUnidadDeTrabajo unidad, AutorizacionOperaciones acceso)
{
    private readonly IReadOnlyDictionary<MotivoDevolucion, ProcesarDevolucion> _politicas =
        politicas.ToDictionary(p => p.Motivo);

    public async Task<DevolucionAplicada> DevolverAsync(Guid id, MotivoDevolucion motivo, bool productoSinUso,
        CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Devolver, ct);
        if (!_politicas.TryGetValue(motivo, out var politica))
            throw new ArgumentOutOfRangeException(nameof(motivo));

        return await unidad.EnTransaccionAsync(token => politica.EjecutarAsync(contexto, id, productoSinUso, token), ct);
    }
}
