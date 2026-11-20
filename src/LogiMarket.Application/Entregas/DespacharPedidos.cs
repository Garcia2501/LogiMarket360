using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Persistencia;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Entregas;

public sealed record DatosDespacho(Guid PedidoId, string Guia, int PlazoDias, DateTime FechaDespachoUtc, DateTime? FechaEntregaUtc);
public sealed record CambioEstadoVisto(EstadoPedido Estado, Guid UsuarioId, DateTime FechaUtc);
public sealed record SeguimientoPedido(Guid Id, EstadoPedido Estado, DatosDespacho? Despacho,
    IReadOnlyList<CambioEstadoVisto> Historial);

public interface IDespachosRegistrados
{
    Task<DatosDespacho?> BuscarAsync(Guid pedidoId, CancellationToken ct);
    void Registrar(DatosDespacho despacho);
    Task RegistrarEntregaAsync(Guid pedidoId, DateTime fecha, CancellationToken ct);
}

public sealed class DespacharPedidos(AutorizacionOperaciones acceso, IUnidadDeTrabajo unidad, IRepositorioPedidos pedidos,
    IDespachosRegistrados despachos, FamiliasEntrega familias, IColaEventos eventos, TimeProvider reloj)
{
    public async Task<DatosDespacho> DespacharAsync(Guid id, CancellationToken ct = default)
    {
        var actor = await acceso.ExigirAsync(Operacion.Despachar, ct);
        return await unidad.EnTransaccionAsync(async token =>
        {
            var pedido = await ObtenerAsync(id, token);
            var fecha = reloj.GetUtcNow();
            pedido.RegistrarDespacho(actor.UsuarioId, fecha);
            var familia = familias.Obtener(pedido.Entrega);
            var guia = familia.Etiqueta.Crear(id);
            var despacho = new DatosDespacho(id, guia, familia.Plazo.Dias, fecha.UtcDateTime, null);
            despachos.Registrar(despacho);
            eventos.Agregar(new EventoComercial(id, EventoComercial.DespachoRegistrado, guia, null, fecha.UtcDateTime));
            return despacho;
        }, ct);
    }

    public async Task<DatosDespacho> EntregarAsync(Guid id, CancellationToken ct = default)
    {
        var actor = await acceso.ExigirAsync(Operacion.Entregar, ct);
        return await unidad.EnTransaccionAsync(async token =>
        {
            var pedido = await ObtenerAsync(id, token);
            var fecha = reloj.GetUtcNow();
            pedido.RegistrarEntrega(actor.UsuarioId, fecha);
            var despacho = await despachos.BuscarAsync(id, token)
                ?? throw new InvalidOperationException("El pedido no tiene guía de despacho.");
            if (fecha.UtcDateTime < despacho.FechaDespachoUtc)
                throw new InvalidOperationException("La entrega no puede ser anterior al despacho.");
            await despachos.RegistrarEntregaAsync(id, fecha.UtcDateTime, token);
            return despacho with { FechaEntregaUtc = fecha.UtcDateTime };
        }, ct);
    }

    public async Task<SeguimientoPedido> ConsultarAsync(Guid id, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        var pedido = await ObtenerAsync(id, ct);
        var despacho = await despachos.BuscarAsync(id, ct);
        return new(id, pedido.Estado, despacho, pedido.Historial
            .Select(h => new CambioEstadoVisto(h.Nuevo, h.UsuarioId, h.FechaUtc)).ToArray());
    }

    private async Task<Pedido> ObtenerAsync(Guid id, CancellationToken ct) =>
        await pedidos.BuscarAsync(id, ct) ?? throw new KeyNotFoundException("El pedido no existe.");
}
