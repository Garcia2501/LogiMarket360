internal async Task<DevolucionAplicada> EjecutarAsync(ContextoDevolucion contexto, Guid id,
    bool productoSinUso, CancellationToken ct)
{
    var (pedido, pago, _, ahora) = await RevisarAsync(contexto, id, productoSinUso, ct);
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
