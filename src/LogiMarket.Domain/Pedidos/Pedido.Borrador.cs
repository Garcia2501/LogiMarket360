using LogiMarket.Domain.Clientes;

namespace LogiMarket.Domain.Pedidos;

public sealed partial class Pedido
{
    // patron prototype: genera un borrador nuevo con copias de las lineas historicas
    public Pedido ClonarComoBorrador(Cliente cliente, Guid usuarioId, DateTimeOffset fecha)
    {
        EstadoActual.ExigirEdicion();
        ArgumentNullException.ThrowIfNull(cliente);
        if (!cliente.Activo || cliente.Id != ClienteId)
            throw new InvalidOperationException("El cliente de la plantilla debe seguir activo.");

        var nuevo = new Pedido { Id = Guid.NewGuid(), ClienteId = ClienteId, Entrega = Entrega };
        nuevo._lineas.AddRange(_lineas.Select(linea => linea.CopiarPara(nuevo.Id)));
        nuevo.Transicionar(EstadoPedido.BORRADOR, usuarioId, fecha, anterior: null);
        return nuevo;
    }

    // patron memento: captura y restaura el borrador completo sin tocar su historial de estados
    public BorradorMemento CapturarBorrador()
    {
        EstadoActual.ExigirEdicion();
        return new BorradorMemento(Id, Entrega, _lineas);
    }

    public void RestaurarBorrador(BorradorMemento memento)
    {
        EstadoActual.ExigirEdicion();
        ArgumentNullException.ThrowIfNull(memento);
        if (memento.PedidoId != Id)
            throw new InvalidOperationException("El estado guardado pertenece a otro pedido.");

        var lineas = memento.CopiarLineas();
        _lineas.Clear();
        _lineas.AddRange(lineas);
        Entrega = memento.Entrega;
        Version = Guid.NewGuid();
    }

    public sealed class BorradorMemento
    {
        private readonly PedidoDetalle[] _captura;

        internal BorradorMemento(Guid id, ModalidadEntrega entrega, IEnumerable<PedidoDetalle> lineas)
        {
            PedidoId = id;
            Entrega = entrega;
            _captura = lineas.Select(l => l.CopiarPara(id)).ToArray();
        }

        internal Guid PedidoId { get; }
        internal ModalidadEntrega Entrega { get; }
        internal PedidoDetalle[] CopiarLineas() => _captura.Select(l => l.CopiarPara(PedidoId)).ToArray();
    }
}
