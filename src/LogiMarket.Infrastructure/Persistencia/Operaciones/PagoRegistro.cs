namespace LogiMarket.Infrastructure.Persistencia.Operaciones;

public sealed class PagoRegistro
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PedidoId { get; init; }
    public required string Referencia { get; init; }
    public required string ClaveIdempotencia { get; init; }
    public required string Proveedor { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Descuento { get; init; }
    public decimal Tarifa { get; init; }
    public decimal Importe { get; init; }
    public DateTime FechaUtc { get; init; }
}
