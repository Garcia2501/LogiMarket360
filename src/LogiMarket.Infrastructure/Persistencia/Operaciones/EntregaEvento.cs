namespace LogiMarket.Infrastructure.Persistencia.Operaciones;

public sealed class EntregaEvento
{
    public Guid EventoId { get; init; }
    public required string Suscriptor { get; init; }
    public DateTime FechaUtc { get; init; }
}
