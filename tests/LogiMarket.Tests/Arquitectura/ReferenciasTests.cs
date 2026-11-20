using System.Xml.Linq;
using LogiMarket.Application.Persistencia;
using LogiMarket.Domain.Pedidos;
using Xunit;

namespace LogiMarket.Tests.Arquitectura;

public sealed class ReferenciasTests
{
    [Theory]
    [InlineData("Domain")]
    [InlineData("Application", "Domain")]
    [InlineData("Infrastructure", "Application", "Domain")]
    [InlineData("WinForms", "Application", "Infrastructure")]
    [InlineData("Tests", "Application", "Domain", "Infrastructure")]
    public void ProyectoSoloRefiereLasCapasPermitidas(string nombre, params string[] permitidas)
    {
        using var recurso = typeof(ReferenciasTests).Assembly.GetManifestResourceStream($"Arquitectura.LogiMarket.{nombre}.csproj");
        var xml = XDocument.Load(recurso!);
        var referencias = xml.Descendants("ProjectReference").Select(nodo =>
        {
            var archivo = ((string)nodo.Attribute("Include")!).Replace('\\', '/').Split('/')[^1];
            return Path.GetFileNameWithoutExtension(archivo)["LogiMarket.".Length..];
        });

        Assert.Equal(permitidas.Order(), referencias.Order());
        if (nombre is "Domain" or "Application")
            Assert.Empty(xml.Descendants("PackageReference"));
    }

    [Fact]
    public void EnsambladosInternosCarecenDeDependenciasTecnologicas()
    {
        Assert.All(
            new[] { typeof(Pedido).Assembly, typeof(IUnidadDeTrabajo).Assembly },
            assembly => Assert.DoesNotContain(assembly.GetReferencedAssemblies(), referencia =>
                referencia.Name is "System.Windows.Forms" or "LogiMarket.WinForms" or "LogiMarket.Infrastructure" ||
                referencia.Name!.Contains("EntityFramework", StringComparison.Ordinal) ||
                referencia.Name.Contains("Sqlite", StringComparison.Ordinal)));
    }
}
