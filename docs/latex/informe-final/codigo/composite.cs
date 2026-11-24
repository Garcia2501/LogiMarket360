public decimal CalcularPrecio() => Capturar().PrecioUnitario;

public IReadOnlyDictionary<Sku, int> Expandir(int cantidad = 1) => Capturar().Expandir(cantidad);

public ComponenteCatalogoSnapshot Capturar() => Capturar(new HashSet<Guid>());

private ComponenteCatalogoSnapshot Capturar(HashSet<Guid> ruta)
{
    if (!ruta.Add(Id))
        throw new InvalidOperationException("La composición cargada contiene un ciclo.");
