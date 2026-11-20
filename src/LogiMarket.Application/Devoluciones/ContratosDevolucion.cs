using LogiMarket.Application.Pagos;

namespace LogiMarket.Application.Devoluciones;

public enum MotivoDevolucion
{
    DEFECTO = 1,
    ARREPENTIMIENTO
}

public sealed record DevolucionAplicada(Guid PedidoId, MotivoDevolucion Motivo, bool ProductoSinUso,
    bool ReintegroInventario, string ReferenciaReembolso, decimal Importe, DateTime FechaUtc);

public interface IDevolucionesRegistradas
{
    Task<bool> ExisteAsync(Guid pedidoId, CancellationToken ct);
    void Registrar(DevolucionAplicada datos);
}

public interface IReembolsoSimulado
{
    string Reembolsar(PagoAplicado pago, decimal importe);
}
