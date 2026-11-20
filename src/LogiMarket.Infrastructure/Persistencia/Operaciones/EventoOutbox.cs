namespace LogiMarket.Infrastructure.Persistencia.Operaciones;

public sealed class EventoOutbox
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PedidoId { get; init; }
    public required string Tipo { get; init; }
    public required string Contenido { get; init; }
    public DateTime FechaUtc { get; init; }
    public DateTime? ProcesadoUtc { get; init; }
    public int Intentos { get; init; }
    public string? UltimoError { get; init; }
}
