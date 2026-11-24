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
