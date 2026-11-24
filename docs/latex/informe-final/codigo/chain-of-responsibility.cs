public async Task<IReadOnlyDictionary<Sku, (ProductoSimple Producto, int Cantidad)>> RevisarAsync(Pedido pedido, Cliente? cliente,
    bool paraConfirmar, CancellationToken ct)
{
    var contexto = new Contexto(pedido, cliente, paraConfirmar);
    await _primero.EjecutarAsync(contexto, ct);
    return contexto.Productos;
}

private sealed record Contexto(Pedido Pedido, Cliente? Cliente, bool ParaConfirmar)
{
    public Dictionary<Sku, (ProductoSimple Producto, int Cantidad)> Productos { get; } = [];
}
