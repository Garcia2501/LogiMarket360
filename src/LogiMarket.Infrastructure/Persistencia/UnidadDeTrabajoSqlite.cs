using LogiMarket.Application.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia;

public sealed class UnidadDeTrabajoSqlite(LogiMarketDbContext db) : IUnidadDeTrabajo
{
    public async Task GuardarAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            db.ChangeTracker.Clear();
            throw new ConflictoConcurrenciaException(exception);
        }
    }

    public async Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);
        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var resultado = await operacion(cancellationToken);
            await GuardarAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return resultado;
        }
        catch
        {
            db.ChangeTracker.Clear();
            throw;
        }
    }
}
