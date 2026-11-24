using LogiMarket.Application.Catalogo;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Pedidos;

public sealed class RechazoPedidoException(string codigo, string mensaje) : InvalidOperationException(mensaje)
{
    public string Codigo { get; } = codigo;
}

// patron chain of responsibility: cada paso valida y delega solo si el anterior fue satisfecho
public sealed class CadenaValidacionPedido
{
    private readonly Paso _primero;

    public CadenaValidacionPedido(IConsultaCatalogo catalogo) =>
        _primero = new ClienteActivo(new LineasPositivas(new StockSuficiente(catalogo, new EstadoValido())));

    public async Task<IReadOnlyDictionary<Sku, (ProductoSimple Producto, int Cantidad)>> RevisarAsync(Pedido pedido, Cliente? cliente,
        bool paraConfirmar, CancellationToken ct)
    {
        var contexto = new Contexto(pedido, cliente, paraConfirmar);
        await _primero.EjecutarAsync(contexto, ct);
        return contexto.Productos;
    }

    private sealed record Contexto(Pedido Pedido, Cliente? Cliente, bool ParaConfirmar)
    {
        public Dictionary<Sku, (ProductoSimple Producto, int Cantidad)> Productos { get; } = [];
    }

    private abstract class Paso(Paso? siguiente)
    {
        public async Task EjecutarAsync(Contexto contexto, CancellationToken ct)
        {
            await RevisarAsync(contexto, ct);
            if (siguiente is not null)
                await siguiente.EjecutarAsync(contexto, ct);
        }

        protected abstract Task RevisarAsync(Contexto contexto, CancellationToken ct);
    }

    private sealed class ClienteActivo(Paso siguiente) : Paso(siguiente)
    {
        protected override Task RevisarAsync(Contexto contexto, CancellationToken ct)
        {
            if (contexto.Cliente is not { Activo: true } || contexto.Cliente.Id != contexto.Pedido.ClienteId)
                throw new RechazoPedidoException("CLIENTE", "El pedido requiere un cliente activo.");
            return Task.CompletedTask;
        }
    }

    private sealed class LineasPositivas(Paso siguiente) : Paso(siguiente)
    {
        protected override Task RevisarAsync(Contexto contexto, CancellationToken ct)
        {
            if (contexto.Pedido.Lineas.Count == 0 ||
                contexto.Pedido.Lineas.Any(linea => linea.Cantidad <= 0 || !linea.Componentes.Any(c => c.EsProducto)))
            {
                throw new RechazoPedidoException("LINEAS", "El pedido requiere líneas y cantidades válidas.");
            }
            return Task.CompletedTask;
        }
    }

    private sealed class StockSuficiente(IConsultaCatalogo catalogo, Paso siguiente) : Paso(siguiente)
    {
        protected override async Task RevisarAsync(Contexto contexto, CancellationToken ct)
        {
            foreach (var unidad in CantidadesFisicas.DelPedido(contexto.Pedido).OrderBy(e => e.Key.Valor))
            {
                var producto = await catalogo.BuscarProductoAsync(unidad.Key, ct);
                if (producto is not { Activo: true } || producto.Stock < unidad.Value)
                    throw new RechazoPedidoException("STOCK", $"Stock insuficiente o SKU inactivo: {unidad.Key}.");
                contexto.Productos.Add(unidad.Key, (producto, unidad.Value));
            }
        }
    }

    private sealed class EstadoValido() : Paso(null)
    {
        protected override Task RevisarAsync(Contexto contexto, CancellationToken ct)
        {
            var esperado = contexto.ParaConfirmar ? EstadoPedido.VALIDADO : EstadoPedido.BORRADOR;
            if (contexto.Pedido.Estado != esperado)
                throw new RechazoPedidoException("ESTADO", $"El pedido debe estar {esperado}.");
            return Task.CompletedTask;
        }
    }
}
