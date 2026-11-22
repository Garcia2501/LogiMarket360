using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Pedidos;

public sealed class HistorialBorrador
{
    private const int Capacidad = 5;
    private readonly Dictionary<Guid, LineaTemporal> _historiales = [];
    private readonly object _candado = new();

    internal void Registrar(Guid pedido, Guid actor, Guid versionAnterior, Pedido.BorradorMemento anterior,
        Guid versionNueva, Pedido.BorradorMemento siguiente)
    {
        lock (_candado)
        {
            if (!_historiales.TryGetValue(pedido, out var linea) ||
                linea.Actor != actor || linea.Version != versionAnterior)
            {
                linea = new LineaTemporal(actor, versionAnterior, anterior);
                _historiales[pedido] = linea;
            }

            linea.Estados.RemoveRange(linea.Indice + 1, linea.Estados.Count - linea.Indice - 1);
            linea.Estados.Add(siguiente);
            if (linea.Estados.Count > Capacidad)
                linea.Estados.RemoveAt(0);
            linea.Indice = linea.Estados.Count - 1;
            linea.Version = versionNueva;
        }
    }

    internal (int Indice, Pedido.BorradorMemento Estado) Obtener(Guid pedido, Guid actor, Guid version, int direccion)
    {
        lock (_candado)
        {
            if (!_historiales.TryGetValue(pedido, out var linea) || linea.Actor != actor || linea.Version != version)
                throw new InvalidOperationException("No hay historial vigente para este borrador y usuario.");

            int destino = linea.Indice + direccion;
            if (destino < 0 || destino >= linea.Estados.Count)
                throw new InvalidOperationException(direccion < 0 ? "No hay cambios para deshacer." : "No hay cambios para rehacer.");

            return (destino, linea.Estados[destino]);
        }
    }

    internal void Confirmar(Guid pedido, Guid actor, Guid versionAnterior, Guid versionNueva, int indice)
    {
        lock (_candado)
        {
            if (_historiales.TryGetValue(pedido, out var linea) && linea.Actor == actor && linea.Version == versionAnterior)
            {
                linea.Indice = indice;
                linea.Version = versionNueva;
            }
        }
    }

    internal void Cerrar(Guid pedido)
    {
        lock (_candado)
            _historiales.Remove(pedido);
    }

    private sealed class LineaTemporal(Guid actor, Guid version, Pedido.BorradorMemento anterior)
    {
        public Guid Actor { get; } = actor;
        public Guid Version { get; set; } = version;
        public int Indice { get; set; }
        public List<Pedido.BorradorMemento> Estados { get; } = [anterior];
    }
}
