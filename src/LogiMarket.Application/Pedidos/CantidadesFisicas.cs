using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Pedidos;

public static class CantidadesFisicas
{
    public static IReadOnlyDictionary<Sku, int> DelPedido(Pedido pedido) => pedido.Lineas
        .SelectMany(linea => linea.Componentes.Where(c => c.EsProducto)
            .Select(c => (c.Sku, Cantidad: checked(c.CantidadPorUnidad * linea.Cantidad))))
        .GroupBy(c => c.Sku)
        .ToDictionary(g => g.Key, g => g.Sum(c => c.Cantidad));
}
