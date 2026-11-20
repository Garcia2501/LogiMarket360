namespace LogiMarket.Application.Eventos;

public enum CanalAviso
{
    EMAIL = 1,
    SMS
}

public sealed record EventoPendiente(Guid Id, EventoComercial Datos);
public sealed record ResumenOutbox(int Procesados, int Fallidos);

public interface IObservadorEvento
{
    string Nombre { get; }
    Task RecibirAsync(EventoPendiente evento, CanalAviso canal, CancellationToken ct);
}

public interface IEntregaDurable
{
    Task EntregarAsync(EventoPendiente evento, IObservadorEvento observador, CanalAviso canal, CancellationToken ct);
}

public interface IProcesadorOutbox
{
    Task<ResumenOutbox> ProcesarAsync(CanalAviso canal, CancellationToken ct);
}

// patron observer: publica el evento confirmado a todos los suscriptores registrados
public sealed class PublicadorEventos(IEnumerable<IObservadorEvento> observadores, IEntregaDurable entregas)
{
    public async Task PublicarAsync(EventoPendiente evento, CanalAviso canal, CancellationToken ct)
    {
        foreach (var observador in observadores)
            await entregas.EntregarAsync(evento, observador, canal, ct);
    }
}
