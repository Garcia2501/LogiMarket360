var pedido = await pedidos.BuscarAsync(id, ct) ?? throw new KeyNotFoundException("El pedido no existe.");
var cliente = await clientes.BuscarPorIdAsync(pedido.ClienteId, ct);
var existencias = await cadena.RevisarAsync(pedido, cliente, paraConfirmar: true, ct);
var valor = valorar.Calcular(pedido, cliente!.Categoria, existencias.Values.Sum(e => e.Cantidad));

var respuesta = await pasarelas.Obtener(proveedor).CobrarAsync(valor.Total, clave, ct);
if (!respuesta.Aprobado || string.IsNullOrWhiteSpace(respuesta.Referencia))
    throw new RechazoPedidoException("COBRO", respuesta.Motivo ?? "El cobro no fue aprobado.");

var ahora = reloj.GetUtcNow();
var pago = new PagoAplicado(id, respuesta.Referencia, clave, proveedor.ToString(), valor.Subtotal, valor.Descuento,
    valor.Tarifa, valor.Total, ahora.UtcDateTime);
foreach (var existencia in existencias.Values)
    inventario.Agregar(existencia.Producto.MoverInventario(-existencia.Cantidad, TipoMovimiento.VENTA,
        "Confirmación de pedido", pago.Referencia, ahora));
