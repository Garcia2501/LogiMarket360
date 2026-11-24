public sealed class MetricasSesion
{
    private readonly ConcurrentDictionary<Guid, string> _eventos = new();

    public int Contar(string tipo) => _eventos.Values.Count(t => t == tipo);

    internal void Registrar(EventoPendiente evento) => _eventos.TryAdd(evento.Id, evento.Datos.Tipo);
}

public sealed class ObservadorMetricas(MetricasSesion metricas) : IObservadorEvento
{
    public string Nombre => "METRICAS";
