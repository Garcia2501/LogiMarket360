using LogiMarket.Domain.Inventario;

namespace LogiMarket.Domain.Catalogo;

public sealed class ProductoSimple
{
    private ProductoSimple() { }

    public ProductoSimple(Sku sku, string nombre, decimal precio, decimal pesoKg, int stock,
        string? categoria = null, string? descripcion = null)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentOutOfRangeException.ThrowIfNegative(stock);
        Id = Guid.NewGuid();
        Sku = sku;
        Stock = stock;
        Activo = true;
        Actualizar(nombre, precio, pesoKg, categoria, descripcion);
    }

    public Guid Id { get; private set; }
    public Sku Sku { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public decimal Precio { get; private set; }
    public decimal PesoKg { get; private set; }
    public int Stock { get; private set; }
    public string? Categoria { get; private set; }
    public string? Descripcion { get; private set; }
    public bool Activo { get; private set; }
    public Guid Version { get; private set; }

    public void Actualizar(string nombre, decimal precio, decimal pesoKg,
        string? categoria = null, string? descripcion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(precio, 0m);
        ArgumentOutOfRangeException.ThrowIfNegative(pesoKg);
        Nombre = nombre.Trim();
        Precio = precio;
        PesoKg = pesoKg;
        Categoria = categoria?.Trim();
        Descripcion = descripcion?.Trim();
        Version = Guid.NewGuid();
    }

    public void CambiarDisponibilidad(bool activo)
    {
        Activo = activo;
        Version = Guid.NewGuid();
    }

    public MovimientoInventario MoverInventario(int cantidad, TipoMovimiento tipo, string motivo,
        string referencia, DateTimeOffset fecha)
    {
        int resultante = checked(Stock + cantidad);
        if (resultante < 0)
            throw new InvalidOperationException($"Existencias insuficientes para {Sku}.");

        var movimiento = new MovimientoInventario(Id, cantidad, tipo, motivo, referencia, resultante, fecha);
        Stock = resultante;
        Version = Guid.NewGuid();
        return movimiento;
    }
}
