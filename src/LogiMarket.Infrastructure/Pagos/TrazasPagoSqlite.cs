using LogiMarket.Application.Pagos;
using LogiMarket.Application.Seguridad;
using LogiMarket.Infrastructure.Persistencia;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Pagos;

public sealed class TrazasPagoSqlite(DbContextOptions<LogiMarketDbContext> opciones, SesionLocal sesion, TimeProvider reloj)
    : ITrazasPago
{
    private readonly List<AuditoriaRegistro> _pendientes = [];

    public void Anotar(ProveedorPago proveedor, string clave, string etapa, string resultado)
    {
        var fecha = reloj.GetUtcNow().UtcDateTime;
        if (_pendientes.Count > 0 && fecha <= _pendientes[^1].FechaUtc)
            fecha = _pendientes[^1].FechaUtc.AddTicks(1);

        _pendientes.Add(new AuditoriaRegistro
        {
            UsuarioId = sesion.Actual?.UsuarioId,
            Categoria = "COBRO",
            Accion = etapa,
            Resultado = resultado,
            Correlacion = ReferenciaSimulada.Crear(proveedor.ToString(), clave),
            FechaUtc = fecha
        });
    }

    public async Task GuardarAsync(CancellationToken ct = default)
    {
        if (_pendientes.Count == 0)
            return;

        await using var db = new LogiMarketDbContext(opciones);
        db.Auditorias.AddRange(_pendientes);
        await db.SaveChangesAsync(ct);
        _pendientes.Clear();
    }
}
