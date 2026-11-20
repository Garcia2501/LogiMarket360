using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Clientes;
using LogiMarket.Application.Inventario;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Persistencia;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Inventario;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using LogiMarket.Infrastructure.Persistencia.Repositorios;
using LogiMarket.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogiMarket.Tests.Integracion;

public sealed class PersistenciaTests
{
    private static readonly DateTimeOffset Ahora = DateTimeOffset.UnixEpoch;

    [Fact]
    public async Task RepositoriosConservanPedidoHistoricoAlModificarYCargarCatalogo()
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        await using var servicios = new ServiceCollection()
            .AgregarPersistenciaLogiMarket(basePrueba.Archivo)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        Guid idPedido;
        Guid idUsuario;

        await using (var scope = servicios.CreateAsyncScope())
        {
            var proveedor = scope.ServiceProvider;
            var db = proveedor.GetRequiredService<LogiMarketDbContext>();
            var catalogo = proveedor.GetRequiredService<IEscrituraCatalogo>();
            var clientes = proveedor.GetRequiredService<IRepositorioClientes>();
            var pedidos = proveedor.GetRequiredService<IRepositorioPedidos>();
            var cliente = new Cliente("cliente-demo", "Cliente de prueba", CategoriaCliente.VIP);
            var usuario = CrearUsuario();
            idUsuario = usuario.Id;
            var mouse = new ProductoSimple(new Sku("P01"), "Mouse", 20m, 0.20m, 8);
            var kit = new KitCompuesto(new Sku("KIT_A"), "Oficina", mouse, 2);
            var pedido = new Pedido(cliente, ModalidadEntrega.EXPRES, kit, 3, usuario.Id, Ahora);
            db.Usuarios.Add(usuario);
            catalogo.AgregarKit(kit);
            clientes.Agregar(cliente);
            pedidos.Agregar(pedido);
            await proveedor.GetRequiredService<IUnidadDeTrabajo>().GuardarAsync();
            idPedido = pedido.Id;
        }

        await using (var scope = servicios.CreateAsyncScope())
        {
            var proveedor = scope.ServiceProvider;
            var catalogo = proveedor.GetRequiredService<IConsultaCatalogo>();
            var kit = Assert.IsType<KitCompuesto>(await catalogo.BuscarKitAsync(new Sku("KIT_A")));
            var producto = Assert.IsType<ProductoSimple>(await catalogo.BuscarProductoAsync(new Sku("P01")));
            producto.Actualizar("Mouse actualizado", 25m, 0.22m);
            var teclado = new ProductoSimple(new Sku("P02"), "Teclado", 30m, 0.70m, 5);
            kit.AgregarComponente(teclado);
            kit.QuitarComponente(producto.Sku);
            var pedido = Assert.IsType<Pedido>(await proveedor.GetRequiredService<IRepositorioPedidos>().BuscarAsync(idPedido));
            pedido.Validar(idUsuario, Ahora.AddDays(1));
            await proveedor.GetRequiredService<IUnidadDeTrabajo>().GuardarAsync();
        }

        await using var reabierto = servicios.CreateAsyncScope();
        var verificaciones = reabierto.ServiceProvider;
        var clientePersistido = await verificaciones.GetRequiredService<IRepositorioClientes>().BuscarAsync(" cliente-demo ");
        Assert.Equal(CategoriaCliente.VIP, clientePersistido?.Categoria);
        var pedidoPersistido = Assert.IsType<Pedido>(
            await verificaciones.GetRequiredService<IRepositorioPedidos>().BuscarAsync(idPedido));
        Assert.Equal(EstadoPedido.VALIDADO, pedidoPersistido.Estado);
        Assert.Equal(120m, pedidoPersistido.Subtotal);
        Assert.Equal(new[] { EstadoPedido.BORRADOR, EstadoPedido.VALIDADO }, pedidoPersistido.Historial.Select(h => h.Nuevo));
        var componente = Assert.Single(Assert.Single(pedidoPersistido.Lineas).Componentes, e => e.EsProducto);
        Assert.Equal((new Sku("P01"), 2, 20m, 0.20m),
            (componente.Sku, componente.CantidadPorUnidad, componente.PrecioUnitario, componente.PesoKg));
        var kitPersistido = Assert.IsType<KitCompuesto>(
            await verificaciones.GetRequiredService<IConsultaCatalogo>().BuscarKitAsync(new Sku("KIT_A")));
        Assert.Equal(new Sku("P02"), Assert.Single(kitPersistido.Expandir()).Key);
    }

    [Fact]
    public async Task ConcurrenciaDeInventarioRevierteElSegundoMovimiento()
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        var sku = new Sku("P01");
        await using (var db = basePrueba.Abrir())
        {
            db.Productos.Add(new ProductoSimple(sku, "Mouse", 20m, 0.20m, 1));
            await db.SaveChangesAsync();
        }

        await using var primerContexto = basePrueba.Abrir();
        await using var segundoContexto = basePrueba.Abrir();
        IConsultaCatalogo primeraConsulta = new RepositorioCatalogo(primerContexto);
        IConsultaCatalogo segundaConsulta = new RepositorioCatalogo(segundoContexto);
        var primero = Assert.IsType<ProductoSimple>(await primeraConsulta.BuscarProductoAsync(sku));
        var segundo = Assert.IsType<ProductoSimple>(await segundaConsulta.BuscarProductoAsync(sku));
        Assert.Equal(primero.Version, segundo.Version);

        IRegistroInventario registro1 = new RegistroInventario(primerContexto);
        IRegistroInventario registro2 = new RegistroInventario(segundoContexto);
        registro1.Agregar(primero.MoverInventario(-1, TipoMovimiento.VENTA, "pedido", "op-1", Ahora));
        registro2.Agregar(segundo.MoverInventario(-1, TipoMovimiento.VENTA, "pedido", "op-2", Ahora));
        IUnidadDeTrabajo primeraUnidad = new UnidadDeTrabajoSqlite(primerContexto);
        IUnidadDeTrabajo segundaUnidad = new UnidadDeTrabajoSqlite(segundoContexto);
        await primeraUnidad.GuardarAsync();
        await Assert.ThrowsAsync<ConflictoConcurrenciaException>(() => segundaUnidad.GuardarAsync());

        await using var reabierto = basePrueba.Abrir();
        Assert.Equal(0, (await reabierto.Productos.SingleAsync()).Stock);
        Assert.Equal("op-1", (await reabierto.Movimientos.SingleAsync()).Referencia);
    }

    [Fact]
    public async Task UnidadDeTrabajoRevierteStockMovimientosYOutboxAlFallar()
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        await using var db = basePrueba.Abrir();
        var usuario = CrearUsuario();
        var cliente = new Cliente("demo", "Cliente de prueba", CategoriaCliente.NORMAL);
        var producto = new ProductoSimple(new Sku("P01"), "Mouse", 20m, 0.20m, 8);
        var pedido = new Pedido(cliente, ModalidadEntrega.ESTANDAR, producto, 1, usuario.Id, Ahora);
        db.Usuarios.Add(usuario);
        db.Clientes.Add(cliente);
        db.Productos.Add(producto);
        db.Pedidos.Add(pedido);
        var unidad = new UnidadDeTrabajoSqlite(db);
        await unidad.GuardarAsync();

        await Assert.ThrowsAsync<FalloSimulado>(() => unidad.EnTransaccionAsync<bool>(async ct =>
        {
            db.Movimientos.Add(producto.MoverInventario(-1, TipoMovimiento.VENTA, "pedido", "op-1", Ahora));
            db.EventosOutbox.Add(new EventoOutbox
            {
                PedidoId = pedido.Id, Tipo = "PedidoPagado", Contenido = "{}", FechaUtc = Ahora.UtcDateTime
            });
            await unidad.GuardarAsync(ct);
            Assert.Equal(1, await db.EventosOutbox.CountAsync(ct));
            throw new FalloSimulado();
        }));

        await using var reabierto = basePrueba.Abrir();
        Assert.Equal(8, (await reabierto.Productos.SingleAsync()).Stock);
        Assert.Empty(await reabierto.Movimientos.ToListAsync());
        Assert.Empty(await reabierto.EventosOutbox.ToListAsync());
    }

    private static Usuario CrearUsuario() =>
        new("cuenta.demo", Rol.ADMIN, new Pbkdf2HashClave().Crear(Guid.NewGuid().ToString("N")));

    private sealed class FalloSimulado : Exception;
}
