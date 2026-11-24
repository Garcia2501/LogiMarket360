public async Task<IReadOnlyList<ErrorActualizacion>> NotificarAsync(CambioComercial cambio,
    Guid? pedidoId = null, CancellationToken ct = default)
{
    var errores = new List<ErrorActualizacion>();
    foreach (var area in Destinos(cambio))
    {
        ct.ThrowIfCancellationRequested();
        if (!_vistas.TryGetValue(area, out var actualizar))
            continue;

        try
        {
            await actualizar(pedidoId, ct);
