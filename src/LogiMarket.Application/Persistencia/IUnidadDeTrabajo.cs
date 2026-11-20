namespace LogiMarket.Application.Persistencia;

public interface IUnidadDeTrabajo
{
    Task GuardarAsync(CancellationToken cancellationToken = default);

    Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancellationToken = default);
}

public sealed class ConflictoConcurrenciaException(Exception causa)
    : InvalidOperationException("Los datos cambiaron desde que se cargaron. Vuelva a consultarlos.", causa);
