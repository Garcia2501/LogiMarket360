using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Escritorio;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Persistencia;
using LogiMarket.Infrastructure.Persistencia;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogiMarket.Tests.Arquitectura;

public sealed class ContratosEscritorioTests
{
    [Fact]
    public void P38_PuertosSegregadosYComposicionDePantallas()
    {
        Assert.All(typeof(IConsultaCatalogo).GetMethods(), metodo => Assert.NotEqual(typeof(void), metodo.ReturnType));
        Assert.All(typeof(IEscrituraCatalogo).GetMethods(), metodo => Assert.Equal(typeof(void), metodo.ReturnType));
        var tipos = typeof(AdministrarCatalogo).GetConstructors().Single().GetParameters().Select(p => p.ParameterType);
        Assert.Contains(typeof(IConsultaCatalogo), tipos);
        Assert.Contains(typeof(IEscrituraCatalogo), tipos);
        Assert.Contains(typeof(IUnidadDeTrabajo), tipos);
        Assert.Contains(typeof(ILecturaEscritorio),
            typeof(ConsultarEscritorio).GetConstructors().Single().GetParameters().Select(p => p.ParameterType));

        using var host = new ServiceCollection().AgregarPersistenciaLogiMarket(":memory:")
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = host.CreateScope();
        Assert.Same(scope.ServiceProvider.GetRequiredService<IConsultaCatalogo>(),
            scope.ServiceProvider.GetRequiredService<IEscrituraCatalogo>());
        Assert.IsType<ConsultarEscritorio>(scope.ServiceProvider.GetRequiredService<ConsultarEscritorio>());
        Assert.IsType<ConfirmacionPedidoFacade>(scope.ServiceProvider.GetRequiredService<ConfirmacionPedidoFacade>());
    }
}
