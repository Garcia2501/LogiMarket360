using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Inventario;
using LogiMarket.Domain.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using LogiMarket.Infrastructure.Persistencia.Repositorios;
using LogiMarket.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogiMarket.Tests.Integracion;

public sealed class InicializacionTests
{
    [Fact]
    public async Task P01_SeedDesdeArchivoAusenteEsIdempotenteYPersiste()
    {
        using var basePrueba = new BaseSqlite();
        Assert.False(File.Exists(basePrueba.Archivo));
        await using var proveedor = new ServiceCollection()
            .AgregarPersistenciaLogiMarket(basePrueba.Archivo)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        await using (var scope = proveedor.CreateAsyncScope())
        {
            var seed = scope.ServiceProvider.GetRequiredService<DatosIniciales>();
            await seed.InicializarAsync(rol => basePrueba.ClavesDemo[rol]);
        }
        Assert.True(File.Exists(basePrueba.Archivo));

        await using var reabierto = proveedor.CreateAsyncScope();
        var servicios = reabierto.ServiceProvider;
        await servicios.GetRequiredService<DatosIniciales>().InicializarAsync();
        var db = servicios.GetRequiredService<LogiMarketDbContext>();
        var catalogo = servicios.GetRequiredService<IConsultaCatalogo>();
        Assert.Same(catalogo, servicios.GetRequiredService<IEscrituraCatalogo>());

        Assert.EndsWith("_EsquemaInicial", Assert.Single(await db.Database.GetAppliedMigrationsAsync()));
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());

        var productos = await db.Productos.OrderBy(p => p.Sku).ToListAsync();
        Assert.Equal(new[]
        {
            ("P01", "Mouse USB", 20.00m, 8, 0.20m),
            ("P02", "Teclado", 30.00m, 5, 0.70m),
            ("P03", "Memoria USB", 10.00m, 20, 0.10m),
            ("P04", "Webcam", 40.00m, 4, 0.35m),
            ("P05", "Alfombrilla", 5.00m, 30, 0.10m)
        }, productos.Select(p => (p.Sku.Valor, p.Nombre, p.Precio, p.Stock, p.PesoKg)));

        Assert.Equal(2, await db.Kits.CountAsync());
        Assert.Equal(4, await db.KitComponentes.CountAsync());
        var oficina = Assert.IsType<KitCompuesto>(await catalogo.BuscarKitAsync(new Sku("KIT_OFICINA")));
        var teletrabajo = Assert.IsType<KitCompuesto>(await catalogo.BuscarKitAsync(new Sku("KIT_TELETRABAJO")));
        Assert.Equal(50m, oficina.CalcularPrecio());
        Assert.Equal(60m, teletrabajo.CalcularPrecio());
        Assert.Equal(new[] { "P01", "P02", "P03" }, teletrabajo.Expandir().Keys.Select(s => s.Valor).Order());
        Assert.All(teletrabajo.Expandir().Values, valor => Assert.Equal(1, valor));

        var hasher = servicios.GetRequiredService<IHashClave>();
        var usuarios = await db.Usuarios.OrderBy(u => u.Rol).ToListAsync();
        Assert.Equal(Enum.GetValues<Rol>(), usuarios.Select(u => u.Rol));
        Assert.Equal(4, usuarios.Select(u => u.HashClave).Distinct().Count());
        Assert.All(usuarios, usuario =>
        {
            var clave = basePrueba.ClavesDemo[usuario.Rol];
            Assert.DoesNotContain(clave, usuario.HashClave);
            Assert.True(hasher.Verificar(clave, usuario.HashClave));
        });
        var admin = await servicios.GetRequiredService<IConsultaUsuarios>().BuscarAsync(" ADMIN.DEMO ");
        Assert.Equal(Rol.ADMIN, admin?.Rol);
    }

    [Fact]
    public async Task SeedRepetidoNoSobrescribeInventarioPreciosNiComposicion()
    {
        using var basePrueba = new BaseSqlite();
        var hasher = new Pbkdf2HashClave();
        Guid idMovimiento;
        string hashInicial;
        await using (var db = basePrueba.Abrir())
        {
            var unidad = new UnidadDeTrabajoSqlite(db);
            await new DatosIniciales(db, unidad, hasher).InicializarAsync(rol => basePrueba.ClavesDemo[rol]);
            var repositorio = new RepositorioCatalogo(db);
            var producto = Assert.IsType<ProductoSimple>(await repositorio.BuscarProductoAsync(new Sku("P03")));
            var kit = Assert.IsType<KitCompuesto>(await repositorio.BuscarKitAsync(new Sku("KIT_TELETRABAJO")));
            producto.Actualizar("Memoria USB actualizada", 11.25m, 0.12m);
            kit.CambiarCantidad(producto.Sku, 2);
            var movimiento = producto.MoverInventario(-2, TipoMovimiento.AJUSTE, "recuento", "recuento-1",
                DateTimeOffset.UnixEpoch);
            idMovimiento = movimiento.Id;
            db.Movimientos.Add(movimiento);
            hashInicial = (await db.Usuarios.SingleAsync(u => u.Rol == Rol.ADMIN)).HashClave;
            await unidad.GuardarAsync();
        }

        await using var reabierta = basePrueba.Abrir();
        await new DatosIniciales(reabierta, new UnidadDeTrabajoSqlite(reabierta), hasher).InicializarAsync();
        var consulta = new RepositorioCatalogo(reabierta);
        var persistido = Assert.IsType<ProductoSimple>(await consulta.BuscarProductoAsync(new Sku("P03")));
        var kitPersistido = Assert.IsType<KitCompuesto>(await consulta.BuscarKitAsync(new Sku("KIT_TELETRABAJO")));
        Assert.Equal((18, 11.25m, 0.12m), (persistido.Stock, persistido.Precio, persistido.PesoKg));
        Assert.Equal(72.50m, kitPersistido.CalcularPrecio());
        Assert.Equal(idMovimiento, (await reabierta.Movimientos.SingleAsync()).Id);
        Assert.Equal(hashInicial, (await reabierta.Usuarios.SingleAsync(u => u.Rol == Rol.ADMIN)).HashClave);
        Assert.Equal(5, await reabierta.Productos.CountAsync());
        Assert.Equal(2, await reabierta.Kits.CountAsync());
        Assert.Equal(4, await reabierta.Usuarios.CountAsync());
    }

    [Fact]
    public async Task SeedSinClavesNoDejaDatosParcialesYSePuedeReintentar()
    {
        using var basePrueba = new BaseSqlite();
        await using var db = basePrueba.Abrir();
        var seed = new DatosIniciales(db, new UnidadDeTrabajoSqlite(db), new Pbkdf2HashClave());
        await Assert.ThrowsAsync<InvalidOperationException>(() => seed.InicializarAsync());

        Assert.Empty(await db.Productos.ToListAsync());
        Assert.Empty(await db.Kits.ToListAsync());
        Assert.Empty(await db.Usuarios.ToListAsync());
        await seed.InicializarAsync(rol => basePrueba.ClavesDemo[rol]);
        Assert.Equal(5, await db.Productos.CountAsync());
        Assert.Equal(2, await db.Kits.CountAsync());
        Assert.Equal(4, await db.Usuarios.CountAsync());
    }

    [Fact]
    public void HashUsaSalDistintaYRechazaClaveIncorrectaOFormatoInvalido()
    {
        var protector = new Pbkdf2HashClave();
        var clave = Guid.NewGuid().ToString("N");
        var hash = protector.Crear(clave);
        Assert.True(protector.Verificar(clave, hash));
        Assert.NotEqual(hash, protector.Crear(clave));
        Assert.False(protector.Verificar(Guid.NewGuid().ToString("N"), hash));
        Assert.False(protector.Verificar(clave, "version-invalida"));
    }
}
