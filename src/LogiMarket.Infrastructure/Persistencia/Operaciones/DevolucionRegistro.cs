namespace LogiMarket.Infrastructure.Persistencia.Operaciones;

public sealed class DevolucionRegistro
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PedidoId { get; init; }
    public required string Motivo { get; init; }
    public bool ProductoSinUso { get; init; }
    public bool ReintegroInventario { get; init; }
    public required string ReferenciaReembolso { get; init; }
    public decimal ImporteReembolso { get; init; }
    public DateTime FechaUtc { get; init; }
}
