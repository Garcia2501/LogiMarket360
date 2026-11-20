using LogiMarket.Application.Comandos;
using LogiMarket.Infrastructure.Persistencia.Operaciones;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistencia.Repositorios;

public sealed class RegistroComandosSqlite(DbContextOptions<LogiMarketDbContext> opciones) : IRegistroComandos
{
    public async Task RegistrarAsync(RegistroComando registro, CancellationToken ct)
    {
        await using var db = new LogiMarketDbContext(opciones);
        db.Auditorias.Add(new AuditoriaRegistro
        {
            UsuarioId = registro.UsuarioId, Categoria = "COMANDO", Accion = registro.Comando,
            Resultado = registro.Resultado, Correlacion = registro.Correlacion, FechaUtc = registro.FechaUtc,
            Referencia = $"{registro.PedidoId:D}/{registro.Entrada}"
        });
        await db.SaveChangesAsync(ct);
    }
}
