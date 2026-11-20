using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using LogiMarket.Infrastructure.Seguridad;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LogiMarket.Tests.Integracion;

public sealed class RestriccionesSqliteTests
{
    private const int IndiceUnico = 2067;
    private const int LlavePrimaria = 1555;
    private const int RestriccionCheck = 275;
    private const int LlaveForanea = 787;
    private const int Trigger = 1811;
    private static readonly DateTimeOffset Ahora = DateTimeOffset.UnixEpoch;

    [Fact]
    public async Task SkuNormalizadoEsUnicoYElPrecioDecimalSeConserva()
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        await using var db = basePrueba.Abrir();
        db.Productos.Add(new ProductoSimple(new Sku("P01"), "Ratón", 0.001234m, 0m, 0));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var producto = await db.Productos.SingleAsync();
        Assert.Equal(0.001234m, producto.Precio);
        db.Productos.Add(new ProductoSimple(new Sku(" p01 "), "Otro", 1m, 0m, 0));
        await AssertRestriccionAsync(IndiceUnico, db);
    }

    [Theory]
    [InlineData("UPDATE Producto SET Precio = '0.0'")]
    [InlineData("UPDATE Producto SET Precio = '-0.005'")]
    [InlineData("UPDATE Producto SET PesoKg = '-0.005'")]
    [InlineData("UPDATE Producto SET PesoKg = 'invalido'")]
    [InlineData("UPDATE Producto SET Stock = -1")]
    [InlineData("UPDATE Producto SET Stock = 0.5")]
    [InlineData("UPDATE Producto SET Stock = 2147483648")]
    public async Task SqliteRechazaProductoInvalidoSinPasarPorDominio(string sql)
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        await using var db = basePrueba.Abrir();
        db.Productos.Add(Producto());
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync(sql));
        Assert.Equal(RestriccionCheck, error.SqliteExtendedErrorCode);
        Assert.Equal(8, (await db.Productos.AsNoTracking().SingleAsync()).Stock);
    }

    [Fact]
    public async Task ProductoYKitCompartenEspacioDeSku()
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        await using var db = basePrueba.Abrir();
        var producto = Producto();
        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        db.Kits.Add(new KitCompuesto(producto.Sku, "Kit", Producto("P02")));
        await AssertRestriccionAsync(Trigger, db);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.Kits.ToListAsync());

        var p01 = await db.Productos.SingleAsync();
        db.Kits.Add(new KitCompuesto(new Sku("KIT_A"), "Kit", p01));
        await db.SaveChangesAsync();
        var conflicto = new ProductoSimple(new Sku("kit_a"), "Duplicado", 1m, 0m, 0);
        db.Productos.Add(conflicto);
        await AssertRestriccionAsync(Trigger, db);
    }

    [Fact]
    public async Task RelacionesDeKitsExigenUnSoloTipoYPadresExistentes()
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        await using var db = basePrueba.Abrir();
        var producto = Producto();
        var kit = new KitCompuesto(new Sku("KIT_A"), "Kit", producto);
        var otroKit = new KitCompuesto(new Sku("KIT_B"), "Otro kit", producto);
        db.Kits.AddRange(kit, otroKit);
        await db.SaveChangesAsync();

        var doble = await Assert.ThrowsAsync<SqliteException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO KitComponente (Id, KitId, ProductoId, KitHijoId, Cantidad)
                VALUES ({Guid.NewGuid()}, {kit.Id}, {producto.Id}, {otroKit.Id}, 1)
                """));
        Assert.Equal(RestriccionCheck, doble.SqliteExtendedErrorCode);
        var huerfano = await Assert.ThrowsAsync<SqliteException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO KitComponente (Id, KitId, ProductoId, Cantidad)
                VALUES ({Guid.NewGuid()}, {kit.Id}, {Guid.NewGuid()}, 1)
                """));
        Assert.Equal(LlaveForanea, huerfano.SqliteExtendedErrorCode);
    }

    [Fact]
    public async Task SqliteTambienBloqueaCicloIndirectoYProtegeLasAristas()
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        await using var db = basePrueba.Abrir();
        var a = new KitCompuesto(new Sku("KIT_A"), "A", Producto());
        var b = new KitCompuesto(new Sku("KIT_B"), "B", a);
        var c = new KitCompuesto(new Sku("KIT_C"), "C", b);
        db.Kits.Add(c);
        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<SqliteException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO KitComponente (Id, KitId, KitHijoId, Cantidad)
                VALUES ({Guid.NewGuid()}, {a.Id}, {c.Id}, 1)
                """));
        Assert.Equal(Trigger, error.SqliteExtendedErrorCode);
        Assert.Equal(3, await db.KitComponentes.CountAsync());
    }

    [Theory]
    [InlineData("pedido")]
    [InlineData("referencia")]
    [InlineData("clave")]
    public async Task PagoTieneUnicidadDePedidoReferenciaYClave(string repetido)
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        await using var db = basePrueba.Abrir();
        var pedidos = CrearPedidos(db);
        db.Pagos.Add(Pago(pedidos[0].Id, "cobro-1", "operacion-1"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        db.Pagos.Add(Pago(
            repetido == "pedido" ? pedidos[0].Id : pedidos[1].Id,
            repetido == "referencia" ? "cobro-1" : "cobro-2",
            repetido == "clave" ? "operacion-1" : "operacion-2"));
        await AssertRestriccionAsync(IndiceUnico, db);
    }

    [Fact]
    public async Task DevolucionEsUnicaPorPedidoYOutboxRegistraUnaEntregaPorSuscriptor()
    {
        using var basePrueba = new BaseSqlite();
        await basePrueba.MigrarAsync();
        await using var db = basePrueba.Abrir();
        var pedido = CrearPedidos(db)[0];
        var evento = new EventoOutbox
        {
            PedidoId = pedido.Id, Tipo = "DevolucionAceptada", Contenido = "{}", FechaUtc = Ahora.UtcDateTime
        };
        db.Devoluciones.Add(Devolucion(pedido.Id, "reembolso-1"));
        db.EventosOutbox.Add(evento);
        db.EntregasEvento.Add(new EntregaEvento
        {
            EventoId = evento.Id, Suscriptor = "auditoria", FechaUtc = Ahora.UtcDateTime
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        db.Devoluciones.Add(Devolucion(pedido.Id, "reembolso-2"));
        await AssertRestriccionAsync(IndiceUnico, db);
        db.ChangeTracker.Clear();

        db.EntregasEvento.Add(new EntregaEvento
        {
            EventoId = evento.Id, Suscriptor = "auditoria", FechaUtc = Ahora.UtcDateTime
        });
        await AssertRestriccionAsync(LlavePrimaria, db);
    }

    private static async Task AssertRestriccionAsync(int codigo, LogiMarketDbContext db)
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(codigo, Assert.IsType<SqliteException>(error.InnerException).SqliteExtendedErrorCode);
    }

    private static ProductoSimple Producto(string sku = "P01") => new(new Sku(sku), sku, 20m, 0.20m, 8);

    private static Pedido[] CrearPedidos(LogiMarketDbContext db)
    {
        var cliente = new Cliente("fixture", "Cliente de prueba", CategoriaCliente.NORMAL);
        var usuario = new Usuario("cuenta.demo", Rol.ADMIN, new Pbkdf2HashClave().Crear(Guid.NewGuid().ToString("N")));
        var producto = Producto();
        db.Clientes.Add(cliente);
        db.Usuarios.Add(usuario);

        Pedido[] pedidos =
        [
            new(cliente, ModalidadEntrega.ESTANDAR, producto, 1, usuario.Id, Ahora),
            new(cliente, ModalidadEntrega.EXPRES, producto, 1, usuario.Id, Ahora)
        ];
        db.Pedidos.AddRange(pedidos);
        return pedidos;
    }

    private static PagoRegistro Pago(Guid pedidoId, string referencia, string clave) => new()
    {
        PedidoId = pedidoId, Referencia = referencia, ClaveIdempotencia = clave, Proveedor = "fixture",
        Subtotal = 20m, Tarifa = 5m, Importe = 25m, FechaUtc = Ahora.UtcDateTime
    };

    private static DevolucionRegistro Devolucion(Guid pedidoId, string referencia) => new()
    {
        PedidoId = pedidoId, Motivo = "DEFECTO", ReferenciaReembolso = referencia, ImporteReembolso = 20m,
        FechaUtc = Ahora.UtcDateTime
    };
}
