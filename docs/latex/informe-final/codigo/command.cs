internal async Task<T> EjecutarAsync<TComando, T>(TComando comando,
    Func<TComando, CancellationToken, Task<T>> receptor, CancellationToken ct) where TComando : IComandoPedido
{
    var actor = sesion.Actual?.UsuarioId;
    var correlacion = Guid.NewGuid().ToString("N");
    var resultado = "APROBADO";
    try
    {
        return await receptor(comando, ct);
    }
    catch (Exception error)
    {
        resultado = error.GetType().Name;
        throw;
    }
    finally
    {
