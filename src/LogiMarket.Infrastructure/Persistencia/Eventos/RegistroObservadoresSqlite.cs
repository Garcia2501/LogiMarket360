using LogiMarket.Application.Escritorio;
using LogiMarket.Application.Eventos;
using LogiMarket.Infrastructure.Persistencia.Operaciones;

namespace LogiMarket.Infrastructure.Persistencia.Eventos;

public sealed class ObservadorAuditoria(LogiMarketDbContext db) : IObservadorEvento
{
    public string Nombre => "AUDITORIA";

    public Task RecibirAsync(EventoPendiente evento, CanalAviso canal, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        db.Auditorias.Add(new AuditoriaRegistro
        {
            Categoria = "NEGOCIO", Accion = evento.Datos.Tipo, Resultado = "PROCESADO",
            Correlacion = evento.Id.ToString("N"), FechaUtc = evento.Datos.FechaUtc, Referencia = evento.Datos.Referencia
        });
        return Task.CompletedTask;
    }
}

public sealed class RegistroAvisosSqlite(LogiMarketDbContext db, EstadoDemostracion? demo = null) : IRegistroAvisosSimulados
{
    public void Registrar(AvisoSimulado aviso)
    {
        if (demo is { Habilitado: true } && demo.Ajustes.FallarAvisos)
            throw new IOException("Fallo de aviso activado en la base de demostración.");
        db.Auditorias.Add(new AuditoriaRegistro
        {
            Categoria = $"AVISO_{aviso.Canal}", Accion = aviso.Evento, Resultado = aviso.Mensaje,
            Correlacion = aviso.EventoId.ToString("N"), FechaUtc = aviso.FechaUtc
        });
    }
}
