namespace LogiMarket.Domain.Inventario;

public enum TipoMovimiento
{
    AJUSTE = 1,
    VENTA,
    DEVOLUCION
}

public sealed class MovimientoInventario
{
    private MovimientoInventario() { }

    internal MovimientoInventario(Guid productoId, int cantidad, TipoMovimiento tipo,
        string motivo, string referencia, int stockResultante, DateTimeOffset fecha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        ArgumentException.ThrowIfNullOrWhiteSpace(referencia);
        if (cantidad == 0 || !Enum.IsDefined(tipo) ||
            (tipo == TipoMovimiento.VENTA && cantidad > 0) ||
            (tipo == TipoMovimiento.DEVOLUCION && cantidad < 0))
        {
            throw new ArgumentException("El signo debe corresponder al tipo de movimiento.", nameof(cantidad));
        }

        Id = Guid.NewGuid();
        ProductoId = productoId;
        Cantidad = cantidad;
        Tipo = tipo;
        Motivo = motivo.Trim();
        Referencia = referencia.Trim();
        StockResultante = stockResultante;
        FechaUtc = fecha.UtcDateTime;
    }

    public Guid Id { get; private set; }
    public Guid ProductoId { get; private set; }
    public int Cantidad { get; private set; }
    public TipoMovimiento Tipo { get; private set; }
    public string Motivo { get; private set; } = null!;
    public string Referencia { get; private set; } = null!;
    public int StockResultante { get; private set; }
    public DateTime FechaUtc { get; private set; }
}
