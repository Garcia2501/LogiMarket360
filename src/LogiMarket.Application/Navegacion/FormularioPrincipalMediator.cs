namespace LogiMarket.Application.Navegacion;

public enum AreaComercial
{
    Catalogo,
    Carrito,
    Pedidos,
    Inventario,
    Metricas
}

public enum CambioComercial
{
    Inicio,
    BorradorEditado,
    EstadoPedido,
    VentaConfirmada,
    DevolucionAceptada,
    EventosProcesados,
    CatalogoEditado
}

public sealed record ErrorActualizacion(AreaComercial Area, string Mensaje);

// patron mediator: distribuye cambios confirmados a las vistas que dependen de ellos
public sealed class FormularioPrincipalMediator
{
    private readonly Dictionary<AreaComercial, Func<Guid?, CancellationToken, Task>> _vistas = [];

    public void Conectar(AreaComercial area, Func<Guid?, CancellationToken, Task> actualizar)
    {
        ArgumentNullException.ThrowIfNull(actualizar);
        _vistas.Add(area, actualizar);
    }

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
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                errores.Add(new(area, error.Message));
            }
        }
        return errores;
    }

    private static AreaComercial[] Destinos(CambioComercial cambio) => cambio switch
    {
        CambioComercial.Inicio or CambioComercial.VentaConfirmada or CambioComercial.DevolucionAceptada =>
            [AreaComercial.Catalogo, AreaComercial.Carrito, AreaComercial.Pedidos, AreaComercial.Inventario, AreaComercial.Metricas],
        CambioComercial.BorradorEditado => [AreaComercial.Carrito, AreaComercial.Pedidos],
        CambioComercial.EstadoPedido => [AreaComercial.Carrito, AreaComercial.Pedidos, AreaComercial.Metricas],
        CambioComercial.CatalogoEditado => [AreaComercial.Catalogo, AreaComercial.Inventario, AreaComercial.Metricas],
        CambioComercial.EventosProcesados => [AreaComercial.Metricas],
        _ => throw new ArgumentOutOfRangeException(nameof(cambio))
    };
}
