    public Pedido Construir(Cliente? cliente, ModalidadEntrega entrega, Guid usuarioId, DateTimeOffset fecha)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        if (_lineas.Count == 0)
            throw new InvalidOperationException("El pedido requiere al menos una línea.");
        var primera = _lineas[0];
        var pedido = new Pedido(cliente, entrega, primera.Elemento, primera.Cantidad, usuarioId, fecha);
        foreach (var linea in _lineas.Skip(1))
            pedido.AgregarLinea(linea.Elemento, linea.Cantidad);
        return pedido;
    }
}
