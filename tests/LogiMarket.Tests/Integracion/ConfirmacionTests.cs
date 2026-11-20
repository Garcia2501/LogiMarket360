using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Clientes;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Pagos;
using LogiMarket.Infrastructure.Persistencia;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogiMarket.Tests.Integracion;

public sealed class ConfirmacionTests
{
    [Theory]
    [InlineData("Ana", CategoriaCliente.VIP, ModalidadEntrega.ESTANDAR, 59, 6, 3)]
    [InlineData("Luis", CategoriaCliente.NORMAL, ModalidadEntrega.EXPRES, 102, 0, 2)]
    [InlineData("Carmen", CategoriaCliente.NORMAL, ModalidadEntrega.ESTANDAR, 90, 15, 1)]
    public async Task P02_P03_P04_EscenariosOficiales(string nombre, CategoriaCliente categoria,
        ModalidadEntrega modalidad, int total, int descuento, int movimientos)
    {
        await using var entorno = await CrearEntornoAsync();
        LineaSolicitada[] lineas = nombre switch
        {
            "Ana" => [new("KIT_TELETRABAJO", 1)],
            "Luis" => [new("P04", 2), new("P05", 2)],
            _ => [new("P03", 10)]
        };
        var orden = await CrearPedidoAsync(entorno, nombre, categoria, modalidad, lineas);
        var pago = await ConfirmarAsync(entorno, orden, "escenario-" + nombre, ProveedorPago.LEGADO);
        Assert.Equal((decimal)total, pago.Importe);
        Assert.Equal((decimal)descuento, pago.Descuento);

        await using var db = entorno.AbrirBase();
        Assert.Equal(EstadoPedido.PAGADO, (await db.Pedidos.SingleAsync()).Estado);
        Assert.Equal(pago.Referencia, (await db.Pagos.SingleAsync()).Referencia);
        Assert.Equal(movimientos, await db.Movimientos.CountAsync());
        var productos = await db.Productos.OrderBy(p => p.Sku).ToListAsync();
        int[] saldos = nombre switch { "Ana" => [7, 4, 19, 4, 30], "Luis" => [8, 5, 20, 2, 28], _ => [8, 5, 10, 4, 30] };
        Assert.Equal(saldos, productos.Select(p => p.Stock));
        Assert.Equal(EventoComercial.PedidoPagado, (await db.EventosOutbox.SingleAsync()).Tipo);
    }

    [Fact]
    public async Task P05_KitSinStockNoInvocaProveedor()
    {
        await using var entorno = await CrearEntornoAsync();
        var orden = await CrearPedidoAsync(entorno, "sin-stock", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("KIT_TELETRABAJO", 6)], validar: false);
        var error = await Assert.ThrowsAsync<RechazoPedidoException>(() =>
            ConfirmarAsync(entorno, orden, "sin-stock", ProveedorPago.LEGADO));
        Assert.Equal("STOCK", error.Codigo);
        Assert.Contains("P02", error.Message);
        await ComprobarSinEfectosAsync(entorno, EstadoPedido.BORRADOR);
        await using var db = entorno.AbrirBase();
        Assert.Empty(await db.Auditorias.ToListAsync());
    }

    [Fact]
    public async Task P06_P19_RechazoMantieneValidadoYRegistraTrazaEnOrden()
    {
        await using var entorno = await CrearEntornoAsync();
        var orden = await CrearPedidoAsync(entorno, "rechazo", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        await using (var scope = entorno.Abrir())
            scope.ServiceProvider.GetRequiredService<EstadoSimulacionPago>().RechazarCobro = true;
        var error = await Assert.ThrowsAsync<RechazoPedidoException>(() =>
            ConfirmarAsync(entorno, orden, "rechazo", ProveedorPago.ALTERNO));
        Assert.Equal("COBRO", error.Codigo);
        await ComprobarSinEfectosAsync(entorno, EstadoPedido.VALIDADO);

        await using var db = entorno.AbrirBase();
        var trazas = await db.Auditorias.OrderBy(a => a.FechaUtc).ToListAsync();
        Assert.Equal(new[] { "INICIO", "PASARELA", "FIN" }, trazas.Select(t => t.Accion));
        Assert.Equal("RECHAZADO", trazas[^1].Resultado);
        Assert.Single(trazas.Select(t => t.Correlacion).Distinct());
    }

    [Fact]
    public async Task P07_P08_IdempotenciaYProteccionDelPedidoPagado()
    {
        await using var entorno = await CrearEntornoAsync();
        var orden = await CrearPedidoAsync(entorno, "idempotente", CategoriaCliente.VIP,
            ModalidadEntrega.ESTANDAR, [new("KIT_TELETRABAJO", 1)]);
        var pago = await ConfirmarAsync(entorno, orden, "operacion", ProveedorPago.LEGADO);
        var repetido = await ConfirmarAsync(entorno, orden, " operacion ", ProveedorPago.ALTERNO);
        Assert.Equal(pago, repetido);
        await Assert.ThrowsAsync<RechazoPedidoException>(() => ConfirmarAsync(entorno, orden, "otra", ProveedorPago.LEGADO));
        await using var scope = entorno.Abrir();
        var gestor = scope.ServiceProvider.GetRequiredService<AdministrarPedidos>();
        var pedido = (await scope.ServiceProvider.GetRequiredService<IRepositorioPedidos>().BuscarAsync(orden))!;
        await Assert.ThrowsAsync<InvalidOperationException>(() => gestor.CancelarAsync(orden));
        await Assert.ThrowsAsync<InvalidOperationException>(() => gestor.CambiarCantidadAsync(orden, pedido.Lineas[0].Id, 7));
        await using var db = entorno.AbrirBase();
        Assert.Single(await db.Pagos.ToListAsync());
        Assert.Equal(3, await db.Movimientos.CountAsync());
        Assert.Single(await db.EventosOutbox.ToListAsync());
        Assert.Equal(1, await db.Auditorias.CountAsync(a => a.Categoria == "COBRO" && a.Accion == "PASARELA"));
        Assert.Equal(3, await db.HistorialPedidos.CountAsync());
        Assert.Equal(60m, (await db.PedidoDetalles.SingleAsync()).PrecioUnitario);
    }

    [Fact]
    public async Task ClaveIdempotenteNoSePuedeReutilizarEnOtroPedido()
    {
        await using var entorno = await CrearEntornoAsync();
        var primero = await CrearPedidoAsync(entorno, "primero", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        var segundo = await CrearPedidoAsync(entorno, "segundo", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        await ConfirmarAsync(entorno, primero, "clave-reusada", ProveedorPago.LEGADO);

        var error = await Assert.ThrowsAsync<RechazoPedidoException>(() =>
            ConfirmarAsync(entorno, segundo, "clave-reusada", ProveedorPago.ALTERNO));
        Assert.Equal("CLAVE", error.Codigo);
        await using var db = entorno.AbrirBase();
        Assert.Equal(segundo, (await db.Pedidos.SingleAsync(p => p.Estado == EstadoPedido.VALIDADO)).Id);
        Assert.Single(await db.Pagos.ToListAsync());
        Assert.Single(await db.EventosOutbox.ToListAsync());
        Assert.Equal(1, await db.Auditorias.CountAsync(a => a.Categoria == "COBRO" && a.Accion == "PASARELA"));
    }

    [Theory]
    [InlineData(Rol.CONSULTA)]
    [InlineData(Rol.BODEGA)]
    public async Task P22_ProxyDeniegaAntesDelDecoradorYDeLaPasarela(Rol rol)
    {
        await using var entorno = await CrearEntornoAsync();
        await entorno.EntrarAsync(rol);
        await using var scope = entorno.Abrir();
        var acceso = scope.ServiceProvider.GetRequiredService<AutorizacionOperaciones>();
        var trazas = scope.ServiceProvider.GetRequiredService<ITrazasPago>();
        var contador = new PasarelaContada();
        IPasarelaPago proxy = new PagoAutorizadoProxy(acceso, new AuditoriaPagoDecorator(ProveedorPago.LEGADO, trazas, contador));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => proxy.CobrarAsync(10m, "denegada"));
        await trazas.GuardarAsync();
        Assert.Equal(0, contador.Invocaciones);
        await using var db = entorno.AbrirBase();
        Assert.Equal("SEGURIDAD", (await db.Auditorias.SingleAsync()).Categoria);
    }

    [Fact]
    public async Task P16_AmbosProveedoresCompartenContrato()
    {
        await using var entorno = await CrearEntornoAsync();
        await using var scope = entorno.Abrir();
        var resolver = scope.ServiceProvider.GetRequiredService<IResolutorPasarelas>();
        foreach (var tipo in Enum.GetValues<ProveedorPago>())
        {
            var pasarela = resolver.Obtener(tipo);
            var resultado = await pasarela.CobrarAsync(5.21m, "contrato");
            Assert.True(resultado.Aprobado);
            Assert.Equal(resultado, await pasarela.CobrarAsync(5.21m, "contrato"));
            await Assert.ThrowsAsync<ArgumentException>(() => pasarela.CobrarAsync(1.001m, "contrato"));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => pasarela.CobrarAsync(0m, "contrato"));
        }
        await scope.ServiceProvider.GetRequiredService<ITrazasPago>().GuardarAsync();
        await using var db = entorno.AbrirBase();
        Assert.Equal(2, await db.Auditorias.Where(a => a.Resultado == "APROBADO")
            .Select(a => a.Correlacion).Distinct().CountAsync());
        Assert.Equal(4, await db.Auditorias.CountAsync(a => a.Accion == "ERROR"));
    }

    [Fact]
    public async Task P20_FalloDePersistenciaReviertePagoEstadoMovimientosYEvento()
    {
        await using var entorno = await CrearEntornoAsync(s => s.AddScoped<IColaEventos, ColaConFkInvalida>());
        var orden = await CrearPedidoAsync(entorno, "rollback", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        await Assert.ThrowsAsync<DbUpdateException>(() => ConfirmarAsync(entorno, orden, "rollback", ProveedorPago.LEGADO));
        await ComprobarSinEfectosAsync(entorno, EstadoPedido.VALIDADO);
        await using var db = entorno.AbrirBase();
        Assert.Equal(2, await db.HistorialPedidos.CountAsync());
        Assert.True(await db.Auditorias.AnyAsync(a => a.Accion == "PASARELA"));
    }

    [Fact]
    public async Task P36_DosConfirmacionesCompitenPorUnaUnidad()
    {
        await using var entorno = await CrearEntornoAsync();
        await entorno.EntrarAsync(Rol.ADMIN);
        await using (var scope = entorno.Abrir())
            await scope.ServiceProvider.GetRequiredService<AdministrarCatalogo>().AjustarAsync("P01", -7, "fixture");
        await entorno.EntrarAsync(Rol.VENTAS);
        var a = await CrearPedidoAsync(entorno, "concurrente-a", CategoriaCliente.NORMAL, ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        var b = await CrearPedidoAsync(entorno, "concurrente-b", CategoriaCliente.NORMAL, ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var primero = CompetirAsync(a, "concurrente-a");
        var segundo = CompetirAsync(b, "concurrente-b");
        inicio.SetResult();
        Assert.Single(await Task.WhenAll(primero, segundo), aprobado => aprobado);

        await using var db = entorno.AbrirBase();
        Assert.Single(await db.Pagos.ToListAsync());
        Assert.Single(await db.EventosOutbox.ToListAsync());
        Assert.Equal(0, (await db.Productos.OrderBy(p => p.Sku).FirstAsync()).Stock);
        Assert.Equal(1, await db.Auditorias.CountAsync(auditoria => auditoria.Categoria == "COBRO" && auditoria.Accion == "PASARELA"));

        async Task<bool> CompetirAsync(Guid pedido, string clave)
        {
            await inicio.Task;
            try
            {
                await ConfirmarAsync(entorno, pedido, clave, ProveedorPago.LEGADO);
                return true;
            }
            catch (RechazoPedidoException error) when (error.Codigo == "STOCK")
            {
                return false;
            }
        }
    }

    internal static async Task<EntornoComercial> CrearEntornoAsync(Action<IServiceCollection>? configurar = null)
    {
        var entorno = new EntornoComercial(configurar);
        try
        {
            await entorno.InicializarAsync();
            await entorno.EntrarAsync(Rol.VENTAS);
            return entorno;
        }
        catch
        {
            await entorno.DisposeAsync();
            throw;
        }
    }

    internal static async Task<Guid> CrearPedidoAsync(EntornoComercial entorno, string identificacion, CategoriaCliente categoria,
        ModalidadEntrega modalidad, IReadOnlyList<LineaSolicitada> lineas, bool validar = true)
    {
        await using var scope = entorno.Abrir();
        var clientes = scope.ServiceProvider.GetRequiredService<AdministrarClientes>();
        var pedidos = scope.ServiceProvider.GetRequiredService<AdministrarPedidos>();
        var cliente = await clientes.RegistrarAsync(identificacion, identificacion, categoria);
        var orden = await pedidos.CrearAsync(cliente.Id, modalidad, lineas);
        if (validar)
            await pedidos.ValidarAsync(orden.Id);
        return orden.Id;
    }

    internal static async Task<PagoAplicado> ConfirmarAsync(EntornoComercial entorno, Guid pedido, string clave, ProveedorPago proveedor)
    {
        await using var scope = entorno.Abrir();
        return await scope.ServiceProvider.GetRequiredService<ConfirmacionPedidoFacade>().ConfirmarAsync(pedido, clave, proveedor);
    }

    private static async Task ComprobarSinEfectosAsync(EntornoComercial entorno, EstadoPedido estado)
    {
        await using var db = entorno.AbrirBase();
        Assert.Equal(estado, (await db.Pedidos.SingleAsync()).Estado);
        Assert.Empty(await db.Pagos.ToListAsync());
        Assert.Empty(await db.Movimientos.ToListAsync());
        Assert.Empty(await db.EventosOutbox.ToListAsync());
        Assert.Equal(new[] { 8, 5, 20, 4, 30 }, (await db.Productos.OrderBy(p => p.Sku).ToListAsync()).Select(p => p.Stock));
    }

    private sealed class PasarelaContada : IPasarelaPago
    {
        public int Invocaciones { get; private set; }

        public Task<RespuestaPasarela> CobrarAsync(decimal importe, string clave, CancellationToken ct = default)
        {
            Invocaciones++;
            return Task.FromResult(new RespuestaPasarela(true, "prueba", null));
        }
    }

    private sealed class ColaConFkInvalida(LogiMarketDbContext db) : IColaEventos
    {
        public void Agregar(EventoComercial evento) => db.EventosOutbox.Add(new EventoOutbox
        {
            PedidoId = Guid.NewGuid(), Tipo = evento.Tipo, Contenido = "{}", FechaUtc = evento.FechaUtc
        });
    }
}
