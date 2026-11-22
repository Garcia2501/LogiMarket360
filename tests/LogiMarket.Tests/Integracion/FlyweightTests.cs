using LogiMarket.Application.Catalogo;
using LogiMarket.Domain.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace LogiMarket.Tests.Integracion;

public sealed class FlyweightTests(ITestOutputHelper salida)
{
    [Fact]
    public async Task P21_MilRenglonesCompartenCincoInstanciasYMidenAsignaciones()
    {
        await using var entorno = new EntornoComercial();
        await entorno.InicializarAsync();
        await entorno.EntrarAsync(Rol.CONSULTA);
        await using var scope = entorno.Abrir();
        var consulta = scope.ServiceProvider.GetRequiredService<ConsultarCatalogoVisual>();
        var demo = await consulta.DemostrarAsync();
        Assert.Equal(1000, demo.Renglones.Count);
        Assert.Equal(5, demo.Medicion.ObjetosCompartidos);
        Assert.Equal(1000, demo.Medicion.ObjetosIndependientes);
        Assert.Same(demo.Renglones[0].Metadatos, demo.Renglones[5].Metadatos);
        Assert.NotEqual(demo.Renglones[0].Cantidad, demo.Renglones[5].Cantidad);
        Assert.Equal(20m, demo.Renglones[0].Precio);
        Assert.Equal(8, demo.Renglones[0].Stock);
        Assert.InRange(demo.Medicion.BytesCompartidos, 1, demo.Medicion.BytesIndependientes - 1);
        salida.WriteLine($"flyweight: renglones={demo.Medicion.Renglones}; compartidos={demo.Medicion.ObjetosCompartidos}; " +
            $"independientes={demo.Medicion.ObjetosIndependientes}; bytes_compartidos={demo.Medicion.BytesCompartidos}; " +
            $"bytes_independientes={demo.Medicion.BytesIndependientes}; repeticiones={demo.Medicion.Repeticiones}");
    }

    [Fact]
    public void ActualizaMetadatosSinMutarViejasFilasYExcluyeDatosExtrinsecosDeLaClave()
    {
        var producto = new ProductoVisto("P01", "Mouse", 20m, 0m, 8, true, "Accesorio", "USB");
        var fabrica = new CatalogoVisualFlyweightFactory();
        var antes = fabrica.CrearRenglon(producto, 1);
        var despues = fabrica.CrearRenglon(producto with { Precio = 25m, Stock = 11 }, 3);
        Assert.Same(antes.Metadatos, despues.Metadatos);
        Assert.Equal((25m, 11, 3), (despues.Precio, despues.Stock, despues.Cantidad));
        var renovado = fabrica.CrearRenglon(producto with { Nombre = "Mouse nuevo" }, 1);
        Assert.NotSame(antes.Metadatos, renovado.Metadatos);
        Assert.Equal("Mouse", antes.Nombre);
        Assert.Equal("Mouse nuevo", renovado.Nombre);
    }
}
