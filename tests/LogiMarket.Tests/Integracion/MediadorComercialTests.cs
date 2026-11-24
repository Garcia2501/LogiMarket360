using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Comandos;
using LogiMarket.Application.Escritorio;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Navegacion;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Reportes;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LogiMarket.Tests.Integracion.ConfirmacionTests;

namespace LogiMarket.Tests.Integracion;

public sealed class MediadorComercialTests
{
    [Fact]
    public async Task ConfirmacionRefrescaCincoColaboradoresSobreElEstadoConfirmado()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "mediator", CategoriaCliente.NORMAL, ModalidadEntrega.ESTANDAR,
            [new("KIT_TELETRABAJO", 1)]);
        await ConfirmarAsync(entorno, id, "mediator", ProveedorPago.LEGADO);
        await using var scope = entorno.Abrir();
        var sp = scope.ServiceProvider;
        var mediador = new FormularioPrincipalMediator();
        var refrescadas = new List<AreaComercial>();

        mediador.Conectar(AreaComercial.Catalogo, async (_, ct) =>
        {
            var filas = await sp.GetRequiredService<ConsultarCatalogoVisual>().ListarAsync(ct);
            Assert.Equal(7, Assert.Single(filas, p => p.Sku == "P01").Stock);
            refrescadas.Add(AreaComercial.Catalogo);
        });
        mediador.Conectar(AreaComercial.Carrito, async (pedido, ct) =>
        {
            Assert.Equal(id, pedido);
            var error = await Assert.ThrowsAsync<RechazoPedidoException>(() =>
                sp.GetRequiredService<AdministrarPedidos>().ConsultarBorradorAsync(pedido!.Value, ct));
            Assert.Equal("ESTADO", error.Codigo);
            refrescadas.Add(AreaComercial.Carrito);
        });
        mediador.Conectar(AreaComercial.Pedidos, async (_, ct) =>
        {
            var fila = Assert.Single(await sp.GetRequiredService<ConsultarReportePedidos>().EjecutarAsync(null, ct));
            Assert.Equal((id, EstadoPedido.PAGADO, 65m), (fila.Id, fila.Estado, fila.TotalCobrado));
            refrescadas.Add(AreaComercial.Pedidos);
        });
        mediador.Conectar(AreaComercial.Inventario, async (_, ct) =>
        {
            var filas = await sp.GetRequiredService<AdministrarCatalogo>().ConsultarExistenciasAsync("KIT_TELETRABAJO", 1, ct);
            Assert.Equal(new[] { 7, 4, 19 }, filas.Select(f => f.Disponibles));
            refrescadas.Add(AreaComercial.Inventario);
        });
        mediador.Conectar(AreaComercial.Metricas, async (_, ct) =>
        {
            var panel = await sp.GetRequiredService<ConsultarEscritorio>().ResumenAsync(ct);
            Assert.Equal(65m, panel.Ventas);
            Assert.Equal(1, panel.Sesion.Pendientes);
            Assert.Equal(0, panel.Sesion.Ventas);
            refrescadas.Add(AreaComercial.Metricas);
        });

        Assert.Empty(await mediador.NotificarAsync(CambioComercial.VentaConfirmada, id));
        Assert.Equal(Enum.GetValues<AreaComercial>(), refrescadas);
    }

    [Fact]
    public async Task DespachoRefrescaIndicadoresYOutboxDeLasVistas()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "mediador-despacho", CategoriaCliente.NORMAL, ModalidadEntrega.ESTANDAR,
            [new("P01", 1)]);
        await ConfirmarAsync(entorno, id, "mediador-despacho", ProveedorPago.LEGADO);
        await using var scope = entorno.Abrir();
        var servicios = scope.ServiceProvider;
        var escritorio = servicios.GetRequiredService<ConsultarEscritorio>();
        var anterior = await escritorio.ResumenAsync();
        Assert.Equal(1, anterior.PorDespachar);
        Assert.Equal(1, anterior.Sesion.Pendientes);

        var mediador = new FormularioPrincipalMediator();
        ResumenGeneral? panel = null;
        IndicadoresSesion? auditoria = null;
        mediador.Conectar(AreaComercial.Metricas, async (_, ct) =>
        {
            panel = await escritorio.ResumenAsync(ct);
            auditoria = await escritorio.IndicadoresAsync(ct);
        });

        await entorno.EntrarAsync(Rol.BODEGA);
        await servicios.GetRequiredService<OperacionesPedido>().DespacharAsync(new DespacharPedidoCommand(id));
        Assert.Empty(await mediador.NotificarAsync(CambioComercial.EstadoPedido, id));

        Assert.Equal(0, Assert.IsType<ResumenGeneral>(panel).PorDespachar);
        Assert.Equal(2, Assert.IsType<IndicadoresSesion>(auditoria).Pendientes);
    }

    [Fact]
    public async Task ErrorDeUnaVistaNoImpideRefrescarLasOtras()
    {
        var mediador = new FormularioPrincipalMediator();
        bool recibio = false;
        mediador.Conectar(AreaComercial.Catalogo, (_, _) => throw new InvalidOperationException("Consulta fallida."));
        mediador.Conectar(AreaComercial.Metricas, (_, _) =>
        {
            recibio = true;
            return Task.CompletedTask;
        });

        var error = Assert.Single(await mediador.NotificarAsync(CambioComercial.VentaConfirmada));
        Assert.Equal(AreaComercial.Catalogo, error.Area);
        Assert.True(recibio);
    }
}
