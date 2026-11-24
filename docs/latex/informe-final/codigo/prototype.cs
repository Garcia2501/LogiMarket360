public Pedido ClonarComoBorrador(Cliente cliente, Guid usuarioId, DateTimeOffset fecha)
{
    EstadoActual.ExigirEdicion();
    ArgumentNullException.ThrowIfNull(cliente);
    if (!cliente.Activo || cliente.Id != ClienteId)
        throw new InvalidOperationException("El cliente de la plantilla debe seguir activo.");

    var nuevo = new Pedido { Id = Guid.NewGuid(), ClienteId = ClienteId, Entrega = Entrega };
    nuevo._lineas.AddRange(_lineas.Select(linea => linea.CopiarPara(nuevo.Id)));
    nuevo.Transicionar(EstadoPedido.BORRADOR, usuarioId, fecha, anterior: null);
    return nuevo;
}
