using LogiMarket.Application.Pagos;
using LogiMarket.Application.Reportes;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LogiMarket.Tests.Integracion.ConfirmacionTests;

namespace LogiMarket.Tests.Integracion;

public sealed class FiltroPedidosTests
{
    [Fact]
    public async Task P25_FiltraSobreEstadosYMontosPersistidos()
    {
        await using var entorno = await CrearEntornoAsync();
        var ana = await CrearPedidoAsync(entorno, "Ana Pérez", CategoriaCliente.VIP,
            ModalidadEntrega.ESTANDAR, [new("KIT_TELETRABAJO", 1)]);
        var luis = await CrearPedidoAsync(entorno, "Luis", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        var pendiente = await CrearPedidoAsync(entorno, "Carmen", CategoriaCliente.NORMAL,
            ModalidadEntrega.EXPRES, [new("P03", 10)]);
        await ConfirmarAsync(entorno, ana, "ana-filtro", ProveedorPago.LEGADO);
        await ConfirmarAsync(entorno, luis, "luis-filtro", ProveedorPago.ALTERNO);

        await entorno.EntrarAsync(Rol.CONSULTA);
        await using var scope = entorno.Abrir();
        var reporte = scope.ServiceProvider.GetRequiredService<ConsultarReportePedidos>();
        var fila = Assert.Single(await reporte.EjecutarAsync("ESTADO = PAGADO AND TOTAL >= 50"));
        Assert.Equal((ana, 59m), (fila.Id, fila.TotalCobrado));
        Assert.Equal(ana, Assert.Single(await reporte.EjecutarAsync("CLIENTE = \"ana pérez\"")).Id);
        Assert.Null(Assert.Single(await reporte.EjecutarAsync("ESTADO = VALIDADO")).TotalCobrado);
        Assert.Equal(pendiente, Assert.Single(await reporte.EjecutarAsync("CLIENTE = Carmen")).Id);
    }

    [Fact]
    public void P26_AndPrecedeAOrYLosParentesisCambianLaSemantica()
    {
        var fila = new PedidoReportado(Guid.NewGuid(), EstadoPedido.PAGADO, "Ana", 25m);
        var sinParentesis = ParserFiltroPedido.Parsear("ESTADO = PAGADO OR CLIENTE = Ana AND TOTAL >= 50");
        var agrupado = ParserFiltroPedido.Parsear("(ESTADO = PAGADO OR CLIENTE = Ana) AND TOTAL >= 50");
        Assert.IsType<FiltroOr>(sinParentesis);
        Assert.IsType<FiltroAnd>(agrupado);
        Assert.True(sinParentesis.Cumple(fila));
        Assert.False(agrupado.Cumple(fila));
    }

    [Theory]
    [InlineData("ESTADO = 3")]
    [InlineData("TOTAL > 50")]
    [InlineData("TOTAL >= 1,25")]
    [InlineData("CLIENTE = \"Ana")]
    [InlineData("(ESTADO = PAGADO")]
    [InlineData("ESTADO = PAGADO OR")]
    [InlineData("ESTADO = PAGADO CLIENTE = Ana")]
    public void P26_SyntaxInvalidaDaUnaColumnaYMensaje(string texto)
    {
        var error = Assert.Throws<SintaxisFiltroException>(() => ParserFiltroPedido.Parsear(texto));
        Assert.InRange(error.Posicion, 1, texto.Length + 1);
        Assert.Contains("Filtro inválido", error.Message);
    }

    [Fact]
    public void LimitaAnidacionYPermiteComillasDoblesEnNombre()
    {
        Assert.Throws<SintaxisFiltroException>(() => ParserFiltroPedido.Parsear(new string('(', 50) + "TOTAL >= 0"));
        var filtro = ParserFiltroPedido.Parsear("CLIENTE = \"Ana \"\"A\"\"\"");
        Assert.True(filtro.Cumple(new(Guid.NewGuid(), EstadoPedido.PAGADO, "Ana \"A\"", 10m)));
    }
}
