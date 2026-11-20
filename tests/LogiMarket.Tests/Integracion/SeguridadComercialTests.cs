using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogiMarket.Tests.Integracion;

public sealed class SeguridadComercialTests
{
    [Theory]
    [InlineData(Rol.ADMIN, true, true, true, true)]
    [InlineData(Rol.VENTAS, false, true, false, false)]
    [InlineData(Rol.BODEGA, false, false, true, false)]
    [InlineData(Rol.CONSULTA, false, false, false, false)]
    public void PermisosPorRol(Rol rol, bool catalogo, bool cobro, bool despacho, bool devolucion)
    {
        Assert.True(AutorizacionOperaciones.Permite(rol, Operacion.Consultar));
        Assert.Equal(catalogo, AutorizacionOperaciones.Permite(rol, Operacion.GestionarCatalogo));
        Assert.Equal(cobro, AutorizacionOperaciones.Permite(rol, Operacion.Cobrar));
        Assert.Equal(despacho, AutorizacionOperaciones.Permite(rol, Operacion.Despachar));
        Assert.Equal(devolucion, AutorizacionOperaciones.Permite(rol, Operacion.Devolver));
    }

    [Fact]
    public async Task AutenticacionYDenegacionAuditadaSinFormularios()
    {
        await using var entorno = new EntornoComercial();
        await entorno.InicializarAsync();
        await using var scope = entorno.Abrir();
        var servicios = scope.ServiceProvider;
        var autenticacion = servicios.GetRequiredService<AutenticacionLocal>();
        var autorizacion = servicios.GetRequiredService<AutorizacionOperaciones>();
        var sesion = servicios.GetRequiredService<SesionLocal>();

        Assert.False(await autenticacion.IniciarAsync("no.existe", entorno.Clave(Rol.CONSULTA)));
        Assert.False(await autenticacion.IniciarAsync("consulta.demo", entorno.Clave(Rol.ADMIN)));
        Assert.Null(sesion.Actual);
        Assert.True(await autenticacion.IniciarAsync(" CONSULTA.DEMO ", entorno.Clave(Rol.CONSULTA)));
        var identidad = await autorizacion.ExigirAsync(Operacion.Consultar);
        Assert.Equal(Rol.CONSULTA, identidad.Rol);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => autorizacion.ExigirAsync(Operacion.Cobrar));
        autenticacion.Cerrar();
        Assert.Null(sesion.Actual);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => autorizacion.ExigirAsync(Operacion.Consultar));

        var auditorias = await servicios.GetRequiredService<LogiMarketDbContext>().Auditorias.ToListAsync();
        Assert.Contains(auditorias, a => a.UsuarioId == identidad.UsuarioId && a.Accion == "Cobrar");
        Assert.Contains(auditorias, a => a.UsuarioId is null && a.Accion == "Consultar");
        Assert.All(auditorias, a => Assert.Equal("DENEGADO", a.Resultado));
    }
}
