namespace LogiMarket.Application.Eventos;

public sealed record AvisoSimulado(Guid EventoId, CanalAviso Canal, string Evento, string Mensaje, DateTime FechaUtc);

public interface IRegistroAvisosSimulados
{
    void Registrar(AvisoSimulado aviso);
}

public interface IAvisoSimulado
{
    void Enviar(EventoPendiente evento);
}

// patron factory method: cada creador construye el emisor simulado de su canal
public abstract class CreadorAviso
{
    public abstract CanalAviso Canal { get; }
    public abstract IAvisoSimulado Crear(IRegistroAvisosSimulados registro);
}

public sealed class CreadorEmail : CreadorAviso
{
    public override CanalAviso Canal => CanalAviso.EMAIL;
    public override IAvisoSimulado Crear(IRegistroAvisosSimulados registro) => new EmailSimulado(registro);

    private sealed class EmailSimulado(IRegistroAvisosSimulados registro) : IAvisoSimulado
    {
        public void Enviar(EventoPendiente evento) => registro.Registrar(new(evento.Id, CanalAviso.EMAIL,
            evento.Datos.Tipo, $"Asunto: {evento.Datos.Tipo}. Pedido {evento.Datos.PedidoId:D}; referencia {evento.Datos.Referencia}.",
            evento.Datos.FechaUtc));
    }
}

public sealed class CreadorSms : CreadorAviso
{
    public override CanalAviso Canal => CanalAviso.SMS;
    public override IAvisoSimulado Crear(IRegistroAvisosSimulados registro) => new SmsSimulado(registro);

    private sealed class SmsSimulado(IRegistroAvisosSimulados registro) : IAvisoSimulado
    {
        public void Enviar(EventoPendiente evento) => registro.Registrar(new(evento.Id, CanalAviso.SMS,
            evento.Datos.Tipo, $"{evento.Datos.Tipo}: {evento.Datos.Referencia}", evento.Datos.FechaUtc));
    }
}

public sealed class ObservadorAvisos(IEnumerable<CreadorAviso> creadores, IRegistroAvisosSimulados registro)
    : IObservadorEvento
{
    private readonly IReadOnlyDictionary<CanalAviso, CreadorAviso> _creadores = creadores.ToDictionary(c => c.Canal);
    public string Nombre => "AVISOS";

    public Task RecibirAsync(EventoPendiente evento, CanalAviso canal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _creadores[canal].Crear(registro).Enviar(evento);
        return Task.CompletedTask;
    }
}
