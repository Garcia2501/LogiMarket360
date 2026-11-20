using LogiMarket.Application.Seguridad;

namespace LogiMarket.Application.Comandos;

public sealed record RegistroComando(Guid? UsuarioId, Guid PedidoId, string Comando, string Entrada,
    string Resultado, string Correlacion, DateTime FechaUtc);

public interface IRegistroComandos
{
    Task RegistrarAsync(RegistroComando registro, CancellationToken ct);
}

// patron command: el invocador ejecuta y audita objetos de operacion sin conocer el receptor
public sealed class InvocadorComandos(IRegistroComandos registros, SesionLocal sesion, TimeProvider reloj)
{
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
            await registros.RegistrarAsync(new(actor, comando.PedidoId, typeof(TComando).Name, comando.Detalle,
                resultado, correlacion, reloj.GetUtcNow().UtcDateTime), CancellationToken.None);
        }
    }
}
