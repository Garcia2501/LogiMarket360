namespace LogiMarket.Domain.Catalogo;

public sealed class KitCompuesto : IElementoCatalogo
{
    private readonly List<KitComponente> _componentes = [];

    private KitCompuesto() { }

    public KitCompuesto(Sku sku, string nombre, IElementoCatalogo primerComponente, int cantidad = 1)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Id = Guid.NewGuid();
        Sku = sku;
        Nombre = nombre.Trim();
        Activo = true;
        AgregarComponente(primerComponente, cantidad);
    }

    public Guid Id { get; private set; }
    public Sku Sku { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public bool Activo { get; private set; }
    public Guid Version { get; private set; }
    public IReadOnlyList<KitComponente> Componentes => _componentes.AsReadOnly();

    public void Renombrar(string nombre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre.Trim();
        Version = Guid.NewGuid();
    }

    public void CambiarDisponibilidad(bool activo)
    {
        Activo = activo;
        Version = Guid.NewGuid();
    }

    public void AgregarComponente(IElementoCatalogo elemento, int cantidad = 1)
    {
        ArgumentNullException.ThrowIfNull(elemento);
        if (elemento.Sku == Sku ||
            elemento is KitCompuesto kit && kit.Contiene(Sku, new HashSet<Guid>()))
        {
            throw new InvalidOperationException($"La composición de {Sku} produciría un ciclo.");
        }

        if (_componentes.Any(c => c.Elemento.Sku == elemento.Sku))
            throw new InvalidOperationException("El componente ya existe. Cambie su cantidad.");

        _componentes.Add(new KitComponente(Id, elemento, cantidad));
        Version = Guid.NewGuid();
    }

    public void CambiarCantidad(Sku sku, int cantidad)
    {
        BuscarComponente(sku).CambiarCantidad(cantidad);
        Version = Guid.NewGuid();
    }

    public void QuitarComponente(Sku sku)
    {
        var componente = BuscarComponente(sku);
        if (_componentes.Count == 1)
            throw new InvalidOperationException("Un kit debe conservar al menos un componente.");

        _componentes.Remove(componente);
        Version = Guid.NewGuid();
    }

    public decimal CalcularPrecio() => Capturar().PrecioUnitario;

    public IReadOnlyDictionary<Sku, int> Expandir(int cantidad = 1) => Capturar().Expandir(cantidad);

    public ComponenteCatalogoSnapshot Capturar() => Capturar(new HashSet<Guid>());

    private ComponenteCatalogoSnapshot Capturar(HashSet<Guid> ruta)
    {
        if (!ruta.Add(Id))
            throw new InvalidOperationException("La composición cargada contiene un ciclo.");

        var hijos = _componentes.Select(componente =>
        {
            var elemento = componente.Elemento;
            var snapshot = elemento is KitCompuesto kit ? kit.Capturar(ruta) : elemento.Capturar();
            return snapshot.ConCantidad(componente.Cantidad);
        }).ToArray();

        ruta.Remove(Id);
        if (hijos.Length == 0)
            throw new InvalidOperationException("El kit no tiene componentes cargados.");

        return new ComponenteCatalogoSnapshot(Sku, Nombre,
            hijos.Sum(h => h.PrecioUnitario * h.Cantidad),
            hijos.Sum(h => h.PesoKg * h.Cantidad), 1, hijos);
    }

    private bool Contiene(Sku sku, HashSet<Guid> visitados) =>
        Sku == sku || (visitados.Add(Id) &&
            _componentes.Any(c => c.Elemento is KitCompuesto hijo && hijo.Contiene(sku, visitados)));

    private KitComponente BuscarComponente(Sku sku) =>
        _componentes.SingleOrDefault(c => c.Elemento.Sku == sku)
        ?? throw new InvalidOperationException($"El componente {sku} no pertenece al kit.");
}
