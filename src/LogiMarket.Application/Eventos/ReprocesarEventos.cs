using LogiMarket.Application.Seguridad;

namespace LogiMarket.Application.Eventos;

public sealed class ReprocesarEventos(AutorizacionOperaciones acceso, IProcesadorOutbox procesador)
{
    public async Task<ResumenOutbox> EjecutarAsync(CanalAviso canal, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.ReprocesarEventos, ct);
        if (!Enum.IsDefined(canal))
            throw new ArgumentOutOfRangeException(nameof(canal));
        return await procesador.ProcesarAsync(canal, ct);
    }
}
