namespace LogiMarket.Domain.Catalogo;

// patron composite: productos y kits ofrecen el mismo contrato de catalogo
public interface IElementoCatalogo
{
    Guid Id { get; }
    Sku Sku { get; }
    string Nombre { get; }
    decimal CalcularPrecio();
    ComponenteCatalogoSnapshot Capturar();
}
