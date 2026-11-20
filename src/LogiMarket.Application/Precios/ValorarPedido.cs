using LogiMarket.Application.Entregas;
using LogiMarket.Application.Pedidos;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Precios;

public sealed class ValorarPedido(CalculadoraPedido calculadora, FamiliasEntrega familias)
{
    public ValoracionPedido Calcular(Pedido pedido, CategoriaCliente categoria) =>
        Calcular(pedido, categoria, CantidadesFisicas.DelPedido(pedido).Values.Sum());

    public ValoracionPedido Calcular(Pedido pedido, CategoriaCliente categoria, int unidadesFisicas) =>
        calculadora.Calcular(new(pedido.Subtotal, unidadesFisicas, categoria),
            familias.Obtener(pedido.Entrega).Tarifa.Valor);
}
