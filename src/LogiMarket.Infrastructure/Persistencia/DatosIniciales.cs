using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia;

public sealed class DatosIniciales(LogiMarketDbContext db, UnidadDeTrabajoSqlite unidad, IHashClave hashClave)
{
    private static readonly (string Sku, string Nombre, decimal Precio, int Stock, decimal PesoKg)[] Productos =
    [
        ("P01", "Mouse USB", 20.00m, 8, 0.20m),
        ("P02", "Teclado", 30.00m, 5, 0.70m),
        ("P03", "Memoria USB", 10.00m, 20, 0.10m),
        ("P04", "Webcam", 40.00m, 4, 0.35m),
        ("P05", "Alfombrilla", 5.00m, 30, 0.10m)
    ];

    private static readonly (Rol Rol, string Nombre)[] Usuarios =
    [
        (Rol.ADMIN, "admin.demo"),
        (Rol.VENTAS, "ventas.demo"),
        (Rol.BODEGA, "bodega.demo"),
        (Rol.CONSULTA, "consulta.demo")
    ];

    public async Task InicializarAsync(Func<Rol, string>? obtenerClaveDemo = null,
        CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);
        await unidad.EnTransaccionAsync(async ct =>
        {
            var catalogo = await SembrarProductosAsync(ct);
            await SembrarKitsAsync(catalogo, ct);
            await SembrarUsuariosAsync(obtenerClaveDemo, ct);
            return true;
        }, cancellationToken);
    }

    private async Task<Dictionary<string, ProductoSimple>> SembrarProductosAsync(CancellationToken ct)
    {
        var catalogo = new Dictionary<string, ProductoSimple>(StringComparer.Ordinal);
        foreach (var datos in Productos)
        {
            var sku = new Sku(datos.Sku);
            var producto = await db.Productos.SingleOrDefaultAsync(e => e.Sku == sku, ct);
            if (producto is null)
            {
                producto = new ProductoSimple(sku, datos.Nombre, datos.Precio, datos.PesoKg, datos.Stock);
                db.Productos.Add(producto);
            }

            catalogo.Add(datos.Sku, producto);
        }

        return catalogo;
    }

    private async Task SembrarKitsAsync(IReadOnlyDictionary<string, ProductoSimple> catalogo, CancellationToken ct)
    {
        var skuOficina = new Sku("KIT_OFICINA");
        var oficina = await db.Kits.SingleOrDefaultAsync(e => e.Sku == skuOficina, ct);
        if (oficina is null)
        {
            oficina = new KitCompuesto(skuOficina, "Kit oficina", catalogo["P01"]);
            oficina.AgregarComponente(catalogo["P02"]);
            db.Kits.Add(oficina);
        }

        var skuTeletrabajo = new Sku("KIT_TELETRABAJO");
        if (!await db.Kits.AnyAsync(e => e.Sku == skuTeletrabajo, ct))
        {
            var teletrabajo = new KitCompuesto(skuTeletrabajo, "Kit teletrabajo", oficina);
            teletrabajo.AgregarComponente(catalogo["P03"]);
            db.Kits.Add(teletrabajo);
        }
    }

    private async Task SembrarUsuariosAsync(Func<Rol, string>? obtenerClave, CancellationToken ct)
    {
        foreach (var datos in Usuarios)
        {
            if (await db.Usuarios.AnyAsync(e => e.Nombre == datos.Nombre, ct))
                continue;

            var clave = obtenerClave?.Invoke(datos.Rol);
            if (string.IsNullOrWhiteSpace(clave))
                throw new InvalidOperationException($"Falta configurar la clave de demostración para {datos.Rol}.");

            db.Usuarios.Add(new Usuario(datos.Nombre, datos.Rol, hashClave.Crear(clave)));
        }
    }
}
