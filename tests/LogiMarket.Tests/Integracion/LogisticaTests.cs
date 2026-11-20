using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LogiMarket.Tests.Integracion.ConfirmacionTests;

namespace LogiMarket.Tests.Integracion;

public sealed class LogisticaTests
{
    [Fact]
    public async Task P09_BodegaDespachaEntregaYConsultaGuiaSoloTrasPago()
    {
        await using var entorno = await CrearEntornoAsync();
        var orden = await CrearPedidoAsync(entorno, "despacho", CategoriaCliente.NORMAL,
            ModalidadEntrega.EXPRES, [new("P01", 1)]);
        await entorno.EntrarAsync(Rol.BODEGA);
        await using var scope = entorno.Abrir();
        var logistica = scope.ServiceProvider.GetRequiredService<DespacharPedidos>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => logistica.DespacharAsync(orden));
        await using (var db = entorno.AbrirBase())
        {
            Assert.Empty(await db.Despachos.ToListAsync());
            Assert.Empty(await db.EventosOutbox.ToListAsync());
            Assert.Equal(EstadoPedido.VALIDADO, (await db.Pedidos.SingleAsync()).Estado);
        }
        await entorno.EntrarAsync(Rol.VENTAS);
        await ConfirmarAsync(entorno, orden, "pago-despacho", ProveedorPago.LEGADO);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => logistica.DespacharAsync(orden));
        await entorno.EntrarAsync(Rol.BODEGA);
        await Assert.ThrowsAsync<InvalidOperationException>(() => logistica.EntregarAsync(orden));

        var despacho = await logistica.DespacharAsync(orden);
        Assert.StartsWith("EXP-", despacho.Guia);
        Assert.Equal(1, despacho.PlazoDias);
        var entrega = await logistica.EntregarAsync(orden);
        Assert.NotNull(entrega.FechaEntregaUtc);
        await Assert.ThrowsAsync<InvalidOperationException>(() => logistica.EntregarAsync(orden));
        await entorno.EntrarAsync(Rol.CONSULTA);
        var visto = await logistica.ConsultarAsync(orden);
        Assert.Equal(EstadoPedido.ENTREGADO, visto.Estado);
        Assert.Equal(entrega, visto.Despacho);
        Assert.Equal(5, visto.Historial.Count);
        await using var reabierto = entorno.AbrirBase();
        Assert.Equal(1, await reabierto.EventosOutbox.CountAsync(e => e.Tipo == EventoComercial.DespachoRegistrado));
    }

    [Theory]
    [InlineData(MotivoDevolucion.DEFECTO, 20, false, null)]
    [InlineData(MotivoDevolucion.DEFECTO, 30, false, null)]
    [InlineData(MotivoDevolucion.DEFECTO, 31, true, "PLAZO")]
    [InlineData(MotivoDevolucion.ARREPENTIMIENTO, 7, true, null)]
    [InlineData(MotivoDevolucion.ARREPENTIMIENTO, 8, true, "PLAZO")]
    [InlineData(MotivoDevolucion.ARREPENTIMIENTO, 10, true, "PLAZO")]
    [InlineData(MotivoDevolucion.ARREPENTIMIENTO, 2, false, "CONDICION")]
    public async Task P33_PoliticasDePlazoYCondicion(MotivoDevolucion motivo, int dias, bool sinUso, string? error)
    {
        var reloj = new RelojPrueba();
        await using var entorno = await CrearEntornoAsync(s => s.AddSingleton<TimeProvider>(reloj));
        var orden = await PedidoEntregadoAsync(entorno, "regreso", CategoriaCliente.NORMAL, [new("P01", 1)]);
        reloj.Avanzar(dias);
        await entorno.EntrarAsync(Rol.ADMIN);
        await using var scope = entorno.Abrir();
        var devoluciones = scope.ServiceProvider.GetRequiredService<AdministrarDevoluciones>();
        if (error is null)
        {
            var resultado = await devoluciones.DevolverAsync(orden, motivo, sinUso);
            Assert.Equal(20m, resultado.Importe);
        }
        else
        {
            var rechazo = await Assert.ThrowsAsync<RechazoPedidoException>(() =>
                devoluciones.DevolverAsync(orden, motivo, sinUso));
            Assert.Equal(error, rechazo.Codigo);
        }

        await using var db = entorno.AbrirBase();
        Assert.Equal(error is null ? 1 : 0, await db.Devoluciones.CountAsync());
        Assert.Equal(error is null ? 8 : 7, (await db.Productos.OrderBy(p => p.Sku).FirstAsync()).Stock);
        Assert.Equal(error is null ? 1 : 0, await db.EventosOutbox.CountAsync(e => e.Tipo == EventoComercial.DevolucionAceptada));
    }

    [Fact]
    public async Task P35_AnaRecibe54YUnSoloReintegro()
    {
        var reloj = new RelojPrueba();
        await using var entorno = await CrearEntornoAsync(s => s.AddSingleton<TimeProvider>(reloj));
        var orden = await PedidoEntregadoAsync(entorno, "Ana", CategoriaCliente.VIP, [new("KIT_TELETRABAJO", 1)]);
        reloj.Avanzar(20);
        await entorno.EntrarAsync(Rol.ADMIN);
        await using var scope = entorno.Abrir();
        var devoluciones = scope.ServiceProvider.GetRequiredService<AdministrarDevoluciones>();
        var aprobada = await devoluciones.DevolverAsync(orden, MotivoDevolucion.DEFECTO, false);
        Assert.Equal(54m, aprobada.Importe);
        var duplicada = await Assert.ThrowsAsync<RechazoPedidoException>(() =>
            devoluciones.DevolverAsync(orden, MotivoDevolucion.DEFECTO, false));
        Assert.Equal("DEVUELTO", duplicada.Codigo);
        var outbox = scope.ServiceProvider.GetRequiredService<ReprocesarEventos>();
        Assert.Equal(new ResumenOutbox(3, 0), await outbox.EjecutarAsync(CanalAviso.SMS));
        Assert.Equal(new ResumenOutbox(0, 0), await outbox.EjecutarAsync(CanalAviso.EMAIL));

        await using var db = entorno.AbrirBase();
        Assert.Equal(59m, (await db.Pagos.SingleAsync()).Importe);
        Assert.Equal(aprobada.ReferenciaReembolso, (await db.Devoluciones.SingleAsync()).ReferenciaReembolso);
        Assert.Equal(3, await db.Movimientos.CountAsync(m => m.Cantidad > 0));
        Assert.Equal(new[] { 8, 5, 20, 4, 30 }, (await db.Productos.OrderBy(p => p.Sku).ToListAsync()).Select(p => p.Stock));
        Assert.Equal(1, await db.EventosOutbox.CountAsync(e => e.Tipo == EventoComercial.DevolucionAceptada));
        Assert.Equal(new[] { EventoComercial.DespachoRegistrado, EventoComercial.DevolucionAceptada, EventoComercial.PedidoPagado },
            await db.Auditorias.Where(a => a.Categoria == "AVISO_SMS").OrderBy(a => a.Accion).Select(a => a.Accion).ToArrayAsync());
        Assert.Equal(9, await db.EntregasEvento.CountAsync());
    }

    private static async Task<Guid> PedidoEntregadoAsync(EntornoComercial entorno, string cliente, CategoriaCliente categoria,
        IReadOnlyList<LineaSolicitada> lineas)
    {
        var orden = await CrearPedidoAsync(entorno, cliente, categoria, ModalidadEntrega.ESTANDAR, lineas);
        await ConfirmarAsync(entorno, orden, "pago-" + cliente, ProveedorPago.LEGADO);
        await entorno.EntrarAsync(Rol.BODEGA);
        await using var scope = entorno.Abrir();
        var logistica = scope.ServiceProvider.GetRequiredService<DespacharPedidos>();
        await logistica.DespacharAsync(orden);
        await logistica.EntregarAsync(orden);
        return orden;
    }

    private sealed class RelojPrueba : TimeProvider
    {
        private DateTimeOffset _ahora = new(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _ahora;
        public void Avanzar(int dias) => _ahora = _ahora.AddDays(dias);
    }
}
