using LogiMarket.Application.Inventario;
using LogiMarket.Application.Persistencia;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Inventario;

namespace LogiMarket.Application.Catalogo;

public sealed record DatosProducto(string Nombre, decimal Precio, decimal PesoKg, string? Categoria, string? Descripcion);
public sealed record ProductoVisto(string Sku, string Nombre, decimal Precio, decimal PesoKg, int Stock, bool Activo,
    string? Categoria, string? Descripcion);
public sealed record ExistenciaComponente(string Sku, int Requeridas, int Disponibles);
public sealed record DesgloseLogistico(decimal PesoKg, IReadOnlyList<ComponenteSolicitado> Componentes);

public sealed record ComponenteSolicitado(string Sku, int Cantidad);

public sealed class AdministrarCatalogo(IConsultaCatalogo consultas, IEscrituraCatalogo escritura,
    IRegistroInventario inventario, IUnidadDeTrabajo unidad, AutorizacionOperaciones acceso, TimeProvider reloj)
{
    public async Task<IReadOnlyList<ProductoVisto>> BuscarAsync(string? texto = null, CancellationToken ct = default)
    {
        var actor = await acceso.ExigirAsync(Operacion.Consultar, ct);
        var termino = texto?.Trim() ?? "";
        return (await consultas.ListarProductosAsync(ct))
            .Where(p => (p.Activo || actor.Rol == Domain.Seguridad.Rol.ADMIN) &&
                (p.Sku.Valor.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                 p.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                 p.Categoria?.Contains(termino, StringComparison.OrdinalIgnoreCase) == true))
            .Select(Proyectar).ToArray();
    }

    public async Task<ProductoVisto> CrearAsync(string codigo, DatosProducto datos, int stock, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.GestionarCatalogo, ct);
        var sku = new Sku(codigo);
        return await unidad.EnTransaccionAsync(async token =>
        {
            await ExigirSkuLibreAsync(sku, token);
            var producto = new ProductoSimple(sku, datos.Nombre, datos.Precio, datos.PesoKg, stock,
                datos.Categoria, datos.Descripcion);
            escritura.AgregarProducto(producto);
            return Proyectar(producto);
        }, ct);
    }

    public async Task<ProductoVisto> ActualizarAsync(string codigo, DatosProducto datos, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.GestionarCatalogo, ct);
        return await unidad.EnTransaccionAsync(async token =>
        {
            var producto = await ObtenerProductoAsync(codigo, token);
            producto.Actualizar(datos.Nombre, datos.Precio, datos.PesoKg, datos.Categoria, datos.Descripcion);
            return Proyectar(producto);
        }, ct);
    }

    public async Task DesactivarAsync(string codigo, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.GestionarCatalogo, ct);
        await unidad.EnTransaccionAsync(async token =>
        {
            (await ObtenerProductoAsync(codigo, token)).CambiarDisponibilidad(false);
            return true;
        }, ct);
    }

    public async Task<ProductoVisto> AjustarAsync(string codigo, int cantidad, string motivo, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.GestionarCatalogo, ct);
        return await unidad.EnTransaccionAsync(async token =>
        {
            var producto = await ObtenerProductoAsync(codigo, token);
            inventario.Agregar(producto.MoverInventario(cantidad, TipoMovimiento.AJUSTE, motivo,
                Guid.NewGuid().ToString("N"), reloj.GetUtcNow()));
            return Proyectar(producto);
        }, ct);
    }

    public async Task<decimal> CrearKitAsync(string codigo, string nombre,
        IReadOnlyList<ComponenteSolicitado> componentes, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.GestionarCatalogo, ct);
        if (componentes.Count == 0)
            throw new ArgumentException("El kit requiere al menos un componente.", nameof(componentes));
        var sku = new Sku(codigo);

        return await unidad.EnTransaccionAsync(async token =>
        {
            await ExigirSkuLibreAsync(sku, token);
            var primero = componentes[0];
            var kit = new KitCompuesto(sku, nombre, await ObtenerElementoActivoAsync(primero.Sku, token), primero.Cantidad);
            foreach (var componente in componentes.Skip(1))
                kit.AgregarComponente(await ObtenerElementoActivoAsync(componente.Sku, token), componente.Cantidad);
            escritura.AgregarKit(kit);
            return kit.CalcularPrecio();
        }, ct);
    }

    public async Task<decimal> ActualizarKitAsync(string codigo, string nombre,
        IReadOnlyList<ComponenteSolicitado> componentes, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.GestionarCatalogo, ct);
        if (componentes.Count == 0)
            throw new ArgumentException("El kit requiere al menos un componente.", nameof(componentes));
        var cantidades = componentes.ToDictionary(c => new Sku(c.Sku), c => c.Cantidad);

        return await unidad.EnTransaccionAsync(async token =>
        {
            var kit = await consultas.BuscarKitAsync(new Sku(codigo), token)
                ?? throw new KeyNotFoundException("El kit no existe.");
            kit.Renombrar(nombre);

            var anteriores = kit.Componentes.Select(c => c.Elemento.Sku).ToHashSet();
            foreach (var (sku, cantidad) in cantidades)
            {
                if (anteriores.Contains(sku))
                    kit.CambiarCantidad(sku, cantidad);
                else
                    kit.AgregarComponente(await ObtenerElementoActivoAsync(sku.Valor, token), cantidad);
            }
            foreach (var sku in anteriores.Where(sku => !cantidades.ContainsKey(sku)))
                kit.QuitarComponente(sku);
            return kit.CalcularPrecio();
        }, ct);
    }

    public async Task<IReadOnlyList<ExistenciaComponente>> ConsultarExistenciasAsync(string codigo, int cantidad,
        CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        var elemento = await ObtenerElementoActivoAsync(codigo, ct);
        var filas = new List<ExistenciaComponente>();
        foreach (var grupo in IteradorComponentesFisicos.Recorrer(elemento, cantidad).GroupBy(c => c.Sku))
        {
            var producto = await consultas.BuscarProductoAsync(grupo.Key, ct);
            filas.Add(new(grupo.Key.Valor, grupo.Sum(c => c.Cantidad), producto is { Activo: true } ? producto.Stock : 0));
        }
        return filas.AsReadOnly();
    }

    public async Task<DesgloseLogistico> ConsultarLogisticaAsync(string codigo, int cantidad, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        var elemento = await ObtenerElementoActivoAsync(codigo, ct);
        var desglose = new VisitanteDesgloseSku();
        var peso = new VisitantePesoLogistico();
        elemento.Aceptar(desglose, cantidad);
        elemento.Aceptar(peso, cantidad);
        return new(peso.PesoKg, desglose.Cantidades.OrderBy(c => c.Key.Valor, StringComparer.Ordinal)
            .Select(c => new ComponenteSolicitado(c.Key.Valor, c.Value)).ToArray());
    }

    internal async Task<IElementoCatalogo> ObtenerElementoActivoAsync(string codigo, CancellationToken ct)
    {
        var sku = new Sku(codigo);
        var producto = await consultas.BuscarProductoAsync(sku, ct);
        if (producto is { Activo: true })
            return producto;
        var kit = await consultas.BuscarKitAsync(sku, ct);
        return kit is { Activo: true } ? kit : throw new KeyNotFoundException($"El SKU {sku} no está disponible.");
    }

    private async Task ExigirSkuLibreAsync(Sku sku, CancellationToken ct)
    {
        if (await consultas.BuscarProductoAsync(sku, ct) is not null ||
            await consultas.BuscarKitAsync(sku, ct) is not null)
            throw new InvalidOperationException($"El SKU {sku} ya existe.");
    }

    private async Task<ProductoSimple> ObtenerProductoAsync(string codigo, CancellationToken ct) =>
        await consultas.BuscarProductoAsync(new Sku(codigo), ct)
        ?? throw new KeyNotFoundException("El producto no existe.");

    private static ProductoVisto Proyectar(ProductoSimple p) =>
        new(p.Sku.Valor, p.Nombre, p.Precio, p.PesoKg, p.Stock, p.Activo, p.Categoria, p.Descripcion);
}
