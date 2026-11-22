using LogiMarket.Application.Catalogo;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogiMarket.Tests.Integracion;

public sealed class RecorridosCatalogoTests
{
    [Fact]
    public void IteratorEsDeterministaYPropagaMultiplicidadesSinPerderRamasCompartidas()
    {
        var a = new ProductoSimple(new Sku("A"), "A", 1m, 0.2m, 30);
        var b = new ProductoSimple(new Sku("B"), "B", 1m, 0.3m, 30);
        var interno = new KitCompuesto(new Sku("KIT_1"), "Interno", b, 3);
        interno.AgregarComponente(a, 2);
        var externo = new KitCompuesto(new Sku("KIT_2"), "Externo", interno, 2);
        externo.AgregarComponente(a);

        var recorrido = IteradorComponentesFisicos.Recorrer(externo, 4);
        ComponenteFisico[] esperado = [new(a.Sku, 4), new(a.Sku, 16), new(b.Sku, 24)];
        Assert.Equal(esperado, recorrido);
        Assert.Equal(esperado, recorrido);
        Assert.Throws<ArgumentOutOfRangeException>(() => IteradorComponentesFisicos.Recorrer(externo, 0));
        Assert.Throws<OverflowException>(() => IteradorComponentesFisicos.Recorrer(externo, int.MaxValue).ToArray());
    }

    [Fact]
    public async Task P27_P34_ConsultaRealDelKitOficial()
    {
        await using var entorno = new EntornoComercial();
        await entorno.InicializarAsync();
        await entorno.EntrarAsync(Rol.CONSULTA);
        await using var scope = entorno.Abrir();
        var catalogo = scope.ServiceProvider.GetRequiredService<AdministrarCatalogo>();
        var filas = await catalogo.ConsultarExistenciasAsync("KIT_TELETRABAJO", 1);
        Assert.Equal(new[] { new ExistenciaComponente("P01", 1, 8), new("P02", 1, 5), new("P03", 1, 20) }, filas);
        var logistica = await catalogo.ConsultarLogisticaAsync("KIT_TELETRABAJO", 1);
        Assert.Equal(1.00m, logistica.PesoKg);
        Assert.Equal(new[] { new ComponenteSolicitado("P01", 1), new("P02", 1), new("P03", 1) }, logistica.Componentes);
        logistica = await catalogo.ConsultarLogisticaAsync("KIT_TELETRABAJO", 3);
        Assert.Equal(3.00m, logistica.PesoKg);
        Assert.All(logistica.Componentes, c => Assert.Equal(3, c.Cantidad));
    }
}
