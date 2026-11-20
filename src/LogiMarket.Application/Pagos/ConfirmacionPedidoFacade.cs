using LogiMarket.Application.Clientes;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Inventario;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Persistencia;
using LogiMarket.Application.Precios;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Inventario;

namespace LogiMarket.Application.Pagos;

// patron facade: coordina la confirmacion y delega las reglas en sus colaboradores
public sealed class ConfirmacionPedidoFacade(AutorizacionOperaciones acceso, IUnidadDeTrabajo unidad,
    IRepositorioPedidos pedidos, IRepositorioClientes clientes, CadenaValidacionPedido cadena,
    ValorarPedido valorar, IResolutorPasarelas pasarelas, IPagosRegistrados pagos,
    IRegistroInventario inventario, IColaEventos eventos, ITrazasPago trazas, TimeProvider reloj)
{
    public async Task<PagoAplicado> ConfirmarAsync(Guid pedidoId, string clave, ProveedorPago proveedor,
        CancellationToken ct = default)
    {
        var actor = await acceso.ExigirAsync(Operacion.Cobrar, ct);
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);
        var claveNormalizada = clave.Trim();

        try
        {
            return await unidad.EnTransaccionAsync(token =>
                ConfirmarEnTransaccionAsync(pedidoId, claveNormalizada, proveedor, actor, token), ct);
        }
        finally
        {
            await acceso.GuardarDenegacionesPendientesAsync(CancellationToken.None);
            await trazas.GuardarAsync(CancellationToken.None);
        }
    }

    private async Task<PagoAplicado> ConfirmarEnTransaccionAsync(Guid id, string clave, ProveedorPago proveedor,
        IdentidadSesion actor, CancellationToken ct)
    {
        if (await pagos.BuscarPorClaveAsync(clave, ct) is { } repetido)
        {
            if (repetido.PedidoId != id)
                throw new RechazoPedidoException("CLAVE", "La clave de idempotencia pertenece a otro pedido.");
            return repetido;
        }

        if (await pagos.BuscarPorPedidoAsync(id, ct) is not null)
            throw new RechazoPedidoException("PAGO_EXISTENTE", "El pedido ya tiene un pago aprobado.");

        var pedido = await pedidos.BuscarAsync(id, ct) ?? throw new KeyNotFoundException("El pedido no existe.");
        var cliente = await clientes.BuscarPorIdAsync(pedido.ClienteId, ct);
        var productos = await cadena.RevisarAsync(pedido, cliente, paraConfirmar: true, ct);
        var unidades = CantidadesFisicas.DelPedido(pedido);
        var valor = valorar.Calcular(pedido, cliente!.Categoria, unidades.Values.Sum());

        var respuesta = await pasarelas.Obtener(proveedor).CobrarAsync(valor.Total, clave, ct);
        if (!respuesta.Aprobado || string.IsNullOrWhiteSpace(respuesta.Referencia))
            throw new RechazoPedidoException("COBRO", respuesta.Motivo ?? "El cobro no fue aprobado.");

        var ahora = reloj.GetUtcNow();
        var pago = new PagoAplicado(id, respuesta.Referencia, clave, proveedor.ToString(), valor.Subtotal, valor.Descuento,
            valor.Tarifa, valor.Total, ahora.UtcDateTime);
        foreach (var unidadFisica in unidades)
            inventario.Agregar(productos[unidadFisica.Key].MoverInventario(-unidadFisica.Value, TipoMovimiento.VENTA,
                "Confirmación de pedido", pago.Referencia, ahora));

        pedido.RegistrarPagoAprobado(actor.UsuarioId, ahora);
        pagos.Registrar(pago);
        eventos.Agregar(new EventoComercial(id, EventoComercial.PedidoPagado, pago.Referencia, pago.Importe, ahora.UtcDateTime));
        return pago;
    }
}
