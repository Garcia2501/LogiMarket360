using LogiMarket.Application.Eventos;
using Xunit;

namespace LogiMarket.Tests.Aplicacion;

public sealed class MetricasSesionTests
{
    [Fact]
    public async Task P15_AcumulaEventosConcurrentesSinContarRepeticiones()
    {
        var metricas = new MetricasSesion();
        var observador = new ObservadorMetricas(metricas);
        var eventos = Enumerable.Range(0, 50).Select(_ => new EventoPendiente(Guid.NewGuid(),
            new EventoComercial(Guid.NewGuid(), EventoComercial.PedidoPagado, "prueba", 59m, DateTime.UtcNow))).ToArray();

        Assert.Equal(0, metricas.Contar(EventoComercial.PedidoPagado));
        await Parallel.ForEachAsync(eventos.Concat(eventos), async (evento, ct) =>
        {
            await observador.RecibirAsync(evento, CanalAviso.EMAIL, ct);
        });
        Assert.Equal(eventos.Length, metricas.Contar(EventoComercial.PedidoPagado));
        Assert.Equal(0, metricas.Contar(EventoComercial.DespachoRegistrado));
    }
}
