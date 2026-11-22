using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Escritorio;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LogiMarket.Tests.Integracion.ConfirmacionTests;

namespace LogiMarket.Tests.Integracion;

public sealed class EscritorioTests
{
    [Fact]
    public async Task P08_ElPrecioDeCatalogoNoReescribeUnPedidoPagado()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "precio", CategoriaCliente.NORMAL, ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        await ConfirmarAsync(entorno, id, "precio", ProveedorPago.LEGADO);
        await entorno.EntrarAsync(Rol.ADMIN);
        await using var scope = entorno.Abrir();
        var sp = scope.ServiceProvider;
        var consulta = sp.GetRequiredService<ConsultarEscritorio>();
        var anterior = await consulta.DetalleAsync(id);
        var linea = Assert.Single(anterior.Lineas);
        var pedidos = sp.GetRequiredService<AdministrarPedidos>();
        var catalogo = sp.GetRequiredService<AdministrarCatalogo>();
        await catalogo.ActualizarAsync("P01", new DatosProducto("Mouse USB", 99m, 0.2m, null, null));
        await Assert.ThrowsAsync<InvalidOperationException>(() => pedidos.CancelarAsync(id));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pedidos.ReemplazarLineaAsync(id, linea.Id, new("P01", 1)));
        var despues = await consulta.DetalleAsync(id);
        Assert.Equal(20m, Assert.Single(despues.Lineas).PrecioUnitario);
        Assert.Equal(anterior.Pago, despues.Pago);
        Assert.Equal(anterior.Historial, despues.Historial);
        Assert.Equal(99m, Assert.Single(await catalogo.BuscarAsync("P01")).Precio);
        Assert.Equal(2, (await consulta.KitsAsync()).Count);
        Assert.Equal(7, Assert.Single(await consulta.MovimientosAsync("P01")).Saldo);
    }

    [Fact]
    public async Task PronosticoDevolucionReutilizaPoliticaSinRegistrarUnReembolso()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "prevision", CategoriaCliente.VIP,
            ModalidadEntrega.ESTANDAR, [new("KIT_TELETRABAJO", 1)]);
        await ConfirmarAsync(entorno, id, "prevision", ProveedorPago.LEGADO);
        await entorno.EntrarAsync(Rol.ADMIN);
        await using var scope = entorno.Abrir();
        var sp = scope.ServiceProvider;
        var despacho = sp.GetRequiredService<DespacharPedidos>();
        await despacho.DespacharAsync(id);
        await despacho.EntregarAsync(id);
        var servicio = sp.GetRequiredService<AdministrarDevoluciones>();
        Assert.Equal(54m, (await servicio.PrevisualizarAsync(id, MotivoDevolucion.DEFECTO, false)).Importe);
        await Assert.ThrowsAsync<RechazoPedidoException>(() =>
            servicio.PrevisualizarAsync(id, MotivoDevolucion.ARREPENTIMIENTO, false));
        Assert.Equal(54m, (await servicio.DevolverAsync(id, MotivoDevolucion.DEFECTO, false)).Importe);
        var reporte = await sp.GetRequiredService<ConsultarEscritorio>().PeriodoAsync(DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1), null);
        Assert.Equal((1, 1, 5m), (reporte.Cobros, reporte.Devoluciones, reporte.VentasNetas));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcotaLosFallosSimuladosYReprocesaSinDuplicados(bool habilitado)
    {
        await using var entorno = await CrearEntornoAsync(s => s.AddSingleton(new EstadoDemostracion(habilitado)));
        var id = await CrearPedidoAsync(entorno, "demo-escritorio", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        await using var scope = entorno.Abrir();
        var sp = scope.ServiceProvider;
        var control = sp.GetRequiredService<ControlDemostracion>();
        var ajustes = new AjustesDemostracion(ProveedorPago.ALTERNO, true, true);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => control.AplicarAsync(ajustes));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sp.GetRequiredService<ConsultarEscritorio>().AuditoriaAsync(null));
        await entorno.EntrarAsync(Rol.ADMIN);
        if (!habilitado)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => control.AplicarAsync(ajustes));
            Assert.True((await ConfirmarAsync(entorno, id, "normal", ProveedorPago.ALTERNO)).Importe > 0m);
            return;
        }

        await control.AplicarAsync(ajustes);
        var error = await Assert.ThrowsAsync<RechazoPedidoException>(() =>
            ConfirmarAsync(entorno, id, "demo", ProveedorPago.ALTERNO));
        Assert.Equal("COBRO", error.Codigo);
        await control.AplicarAsync(ajustes with { RechazarCobro = false });
        await ConfirmarAsync(entorno, id, "demo", ProveedorPago.ALTERNO);
        var reproceso = sp.GetRequiredService<ReprocesarEventos>();
        Assert.Equal(new ResumenOutbox(0, 1), await reproceso.EjecutarAsync(CanalAviso.EMAIL));
        var consulta = sp.GetRequiredService<ConsultarEscritorio>();
        Assert.NotNull(Assert.Single(await consulta.OutboxAsync()).UltimoError);
        await control.AplicarAsync(ajustes with { RechazarCobro = false, FallarAvisos = false });
        Assert.Equal(new ResumenOutbox(1, 0), await reproceso.EjecutarAsync(CanalAviso.EMAIL));
        Assert.Equal(new ResumenOutbox(0, 0), await reproceso.EjecutarAsync(CanalAviso.EMAIL));
        Assert.Equal(1, (await consulta.IndicadoresAsync()).Ventas);
        Assert.Single(await consulta.AuditoriaAsync("AVISO_EMAIL"));
    }
}
