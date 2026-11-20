namespace LogiMarket.Domain.Catalogo;

public sealed record Sku
{
    public Sku(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        Valor = valor.Trim().ToUpperInvariant();
    }

    public string Valor { get; }

    public override string ToString() => Valor;
}
