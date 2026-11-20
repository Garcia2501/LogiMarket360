namespace LogiMarket.Application.Pagos;

public sealed record PagoAplicado(Guid PedidoId, string Referencia, string Clave, string Proveedor, decimal Subtotal,
    decimal Descuento, decimal Tarifa, decimal Importe, DateTime FechaUtc);

public interface IPagosRegistrados
{
    Task<PagoAplicado?> BuscarPorClaveAsync(string clave, CancellationToken ct);
    Task<PagoAplicado?> BuscarPorPedidoAsync(Guid pedidoId, CancellationToken ct);
    void Registrar(PagoAplicado pago);
}
