using LogiMarket.Application.Comandos;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pagos;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using LogiMarket.Infrastructure.Persistencia.Eventos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LogiMarket.Tests.Integracion.ConfirmacionTests;

namespace LogiMarket.Tests.Integracion;

public sealed class OutboxComercialTests
{
    [Fact]
    public async Task P37_ReiniciarProcesadorReintentaSinDuplicarNingunEfecto()
    {
        var control = new FalloAviso();
        await using var entorno = await CrearEntornoAsync(s => s.AddSingleton(control)
            .AddScoped<IRegistroAvisosSimulados, RegistroInterrumpido>());
        var id = await CrearPedidoAsync(entorno, "outbox", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        await ConfirmarAsync(entorno, id, "evento-fase-dos", ProveedorPago.LEGADO);
        await entorno.EntrarAsync(Rol.ADMIN);

        Assert.Equal(new ResumenOutbox(0, 1), await ReprocesarAsync(entorno, CanalAviso.EMAIL));
        await using (var previo = entorno.AbrirBase())
        {
            var pendiente = await previo.EventosOutbox.SingleAsync();
            Assert.Null(pendiente.ProcesadoUtc);
            Assert.Equal("IOException", pendiente.UltimoError);
            Assert.Equal(1, pendiente.Intentos);
            Assert.Equal("AUDITORIA", (await previo.EntregasEvento.SingleAsync()).Suscriptor);
            Assert.Single(await previo.Auditorias.Where(a => a.Categoria == "NEGOCIO").ToListAsync());
            Assert.False(await previo.Auditorias.AnyAsync(a => a.Categoria.StartsWith("AVISO_")));
        }

        await entorno.ReiniciarAsync();
        await entorno.EntrarAsync(Rol.ADMIN);
        control.Fallar = false;
        Assert.Equal(new ResumenOutbox(1, 0), await ReprocesarAsync(entorno, CanalAviso.EMAIL));
        Assert.Equal(new ResumenOutbox(0, 0), await ReprocesarAsync(entorno, CanalAviso.SMS));

        await using var db = entorno.AbrirBase();
        var final = await db.EventosOutbox.SingleAsync();
        Assert.NotNull(final.ProcesadoUtc);
        Assert.Null(final.UltimoError);
        Assert.Equal(2, final.Intentos);
        Assert.Equal(3, await db.EntregasEvento.CountAsync());
        Assert.Equal(1, await db.Auditorias.CountAsync(a => a.Categoria == "NEGOCIO"));
        Assert.Equal(1, await db.Auditorias.CountAsync(a => a.Categoria == "AVISO_EMAIL"));
        Assert.Single(await db.Pagos.ToListAsync());
        Assert.Single(await db.Movimientos.ToListAsync());
        Assert.Equal(7, (await db.Productos.OrderBy(p => p.Sku).FirstAsync()).Stock);
        Assert.Equal(EstadoPedido.PAGADO, (await db.Pedidos.SingleAsync()).Estado);
        Assert.Equal(1, await db.Auditorias.CountAsync(a => a.Categoria == "COBRO" && a.Accion == "PASARELA"));
        await using var vistaA = entorno.Abrir();
        await using var vistaB = entorno.Abrir();
        var metrica = vistaA.ServiceProvider.GetRequiredService<MetricasSesion>();
        Assert.Same(metrica, vistaB.ServiceProvider.GetRequiredService<MetricasSesion>());
        Assert.Equal(1, metrica.Contar(EventoComercial.PedidoPagado));
    }

    [Theory]
    [InlineData(CanalAviso.EMAIL)]
    [InlineData(CanalAviso.SMS)]
    public async Task P11_P30_CanalElegidoYTresObservadoresRecibenUnEvento(CanalAviso canal)
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "canal", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        await ConfirmarAsync(entorno, id, "canal", ProveedorPago.ALTERNO);
        await entorno.EntrarAsync(Rol.ADMIN);
        Assert.Equal(new ResumenOutbox(1, 0), await ReprocesarAsync(entorno, canal));

        await using var db = entorno.AbrirBase();
        var aviso = await db.Auditorias.SingleAsync(a => a.Categoria.StartsWith("AVISO_"));
        Assert.Equal($"AVISO_{canal}", aviso.Categoria);
        Assert.Contains(EventoComercial.PedidoPagado, aviso.Resultado);
        Assert.Equal(canal == CanalAviso.EMAIL, aviso.Resultado.StartsWith("Asunto:"));
        Assert.Equal(new[] { "AUDITORIA", "AVISOS", "METRICAS" },
            await db.EntregasEvento.OrderBy(e => e.Suscriptor).Select(e => e.Suscriptor).ToArrayAsync());
        await using var scope = entorno.Abrir();
        Assert.Equal(1, scope.ServiceProvider.GetRequiredService<MetricasSesion>().Contar(EventoComercial.PedidoPagado));
    }

    [Fact]
    public async Task P24_CommandInvocaYRegistraExitoYRechazo()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "Ana QA", CategoriaCliente.VIP, ModalidadEntrega.ESTANDAR, [new("KIT_TELETRABAJO", 1)]);
        await using var scope = entorno.Abrir();
        var comandos = scope.ServiceProvider.GetRequiredService<OperacionesPedido>();
        var pago = await comandos.ConfirmarAsync(new ConfirmarPedidoCommand(id, "comando", ProveedorPago.LEGADO));
        Assert.Equal((60m, 6m, 5m, 59m), (pago.Subtotal, pago.Descuento, pago.Tarifa, pago.Importe));
        await Assert.ThrowsAsync<InvalidOperationException>(() => comandos.CancelarAsync(new CancelarPedidoCommand(id)));
        await using var db = entorno.AbrirBase();
        var registros = await db.Auditorias.Where(a => a.Categoria == "COMANDO").ToListAsync();
        Assert.Contains(registros, a => a.Accion == "ConfirmarPedidoCommand" && a.Resultado == "APROBADO"
            && a.Referencia == $"{id:D}/LEGADO");
        Assert.Contains(registros, a => a.Accion == "CancelarPedidoCommand" && a.Resultado == "InvalidOperationException");
    }

    private static async Task<ResumenOutbox> ReprocesarAsync(EntornoComercial entorno, CanalAviso canal)
    {
        await using var scope = entorno.Abrir();
        return await scope.ServiceProvider.GetRequiredService<ReprocesarEventos>().EjecutarAsync(canal);
    }

    private sealed class FalloAviso
    {
        public bool Fallar { get; set; } = true;
    }

    private sealed class RegistroInterrumpido(LogiMarketDbContext db, FalloAviso control) : IRegistroAvisosSimulados
    {
        public void Registrar(AvisoSimulado aviso)
        {
            if (control.Fallar)
                throw new IOException("Fallo de notificación simulado.");
            new RegistroAvisosSqlite(db).Registrar(aviso);
        }
    }
}
