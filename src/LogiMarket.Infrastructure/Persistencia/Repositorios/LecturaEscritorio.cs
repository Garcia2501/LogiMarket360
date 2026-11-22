using LogiMarket.Application.Escritorio;
using LogiMarket.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class LecturaEscritorio(LogiMarketDbContext db) : ILecturaEscritorio
{
    public async Task<IReadOnlyList<KitVisto>> KitsAsync(CancellationToken ct)
    {
        var filas = await db.Kits.AsNoTracking().Where(k => k.Activo).OrderBy(k => k.Sku)
            .Select(k => new { k.Sku, k.Nombre }).ToListAsync(ct);
        return filas.Select(k => new KitVisto(k.Sku.Valor, k.Nombre)).ToArray();
    }

    public async Task<IReadOnlyList<MovimientoVisto>> MovimientosAsync(string? sku, CancellationToken ct)
    {
        var productos = db.Productos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(sku))
        {
            var clave = new Sku(sku);
            productos = productos.Where(p => p.Sku == clave);
        }

        var filas = await (from movimiento in db.Movimientos.AsNoTracking()
                           join producto in productos on movimiento.ProductoId equals producto.Id
                           orderby movimiento.FechaUtc descending, movimiento.Id
                           select new { movimiento, producto.Sku }).Take(500).ToListAsync(ct);
        return filas.Select(r => new MovimientoVisto(r.movimiento.Id, r.movimiento.FechaUtc, r.Sku.Valor, r.movimiento.Tipo,
            r.movimiento.Cantidad, r.movimiento.StockResultante, r.movimiento.Motivo, r.movimiento.Referencia)).ToArray();
    }

    public async Task<IReadOnlyList<AuditoriaVista>> AuditoriaAsync(string? categoria, CancellationToken ct)
    {
        var query = db.Auditorias.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(a => a.Categoria.Contains(categoria.Trim().ToUpperInvariant()));
        return await query.OrderByDescending(a => a.FechaUtc).ThenBy(a => a.Id).Take(500)
            .Select(a => new AuditoriaVista(a.Id, a.FechaUtc, a.Categoria, a.Accion, a.Resultado, a.Correlacion, a.Referencia))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OutboxVisto>> OutboxAsync(CancellationToken ct) =>
        await db.EventosOutbox.AsNoTracking().OrderBy(e => e.ProcesadoUtc != null).ThenByDescending(e => e.FechaUtc)
            .Take(500).Select(e => new OutboxVisto(e.Id, e.FechaUtc, e.Tipo, e.Intentos, e.ProcesadoUtc, e.UltimoError))
            .ToListAsync(ct);

    public Task<int> PendientesAsync(CancellationToken ct) => db.EventosOutbox.CountAsync(e => e.ProcesadoUtc == null, ct);
}
