namespace LogiMarket.Infrastructure.Persistencia.Operaciones;

public sealed class DespachoRegistro
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PedidoId { get; init; }
    public required string Guia { get; init; }
    public int PlazoDias { get; init; }
    public DateTime FechaDespachoUtc { get; init; }
    public DateTime? FechaEntregaUtc { get; init; }
}
