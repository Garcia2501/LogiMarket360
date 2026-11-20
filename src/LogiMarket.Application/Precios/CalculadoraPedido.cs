using LogiMarket.Domain.Clientes;

namespace LogiMarket.Application.Precios;

public sealed record BasePromocion(decimal Subtotal, int UnidadesFisicas, CategoriaCliente Categoria);
public sealed record ValoracionPedido(decimal Subtotal, decimal Descuento, decimal Tarifa, decimal Total,
    string Promocion, int UnidadesFisicas);

public interface IPoliticaDescuento
{
    string Nombre { get; }
    int Prioridad { get; }
    decimal Tasa { get; }
    bool Aplica(BasePromocion datos);
}

public sealed class SinDescuento : IPoliticaDescuento
{
    public string Nombre => "NORMAL";
    public int Prioridad => 0;
    public decimal Tasa => 0m;
    public bool Aplica(BasePromocion datos) => true;
}

public sealed class Vip10 : IPoliticaDescuento
{
    public string Nombre => "VIP";
    public int Prioridad => 10;
    public decimal Tasa => 0.10m;
    public bool Aplica(BasePromocion datos) => datos.Categoria == CategoriaCliente.VIP;
}

public sealed class Mayorista15 : IPoliticaDescuento
{
    public string Nombre => "MAYORISTA";
    public int Prioridad => 20;
    public decimal Tasa => 0.15m;
    public bool Aplica(BasePromocion datos) => datos.UnidadesFisicas >= 10;
}

// patron strategy: selecciona una sola politica por elegibilidad y prioridad
public sealed class CalculadoraPedido(IEnumerable<IPoliticaDescuento> politicas)
{
    private readonly IPoliticaDescuento[] _politicas = politicas.OrderByDescending(p => p.Prioridad).ToArray();

    public ValoracionPedido Calcular(BasePromocion datos, decimal tarifa)
    {
        var politica = _politicas.First(p => p.Aplica(datos));
        var descuento = Redondear(datos.Subtotal * politica.Tasa);
        return new(datos.Subtotal, descuento, tarifa, Redondear(datos.Subtotal - descuento + tarifa),
            politica.Nombre, datos.UnidadesFisicas);
    }

    public static decimal Redondear(decimal monto) => decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
}
