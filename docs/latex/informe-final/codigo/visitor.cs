public abstract class VisitanteComponentes : IVisitanteCatalogo
{
    private readonly HashSet<Guid> _ruta = [];

    public abstract void Visitar(ProductoSimple producto, int cantidad);

    public void Visitar(KitCompuesto kit, int cantidad)
    {
        if (!_ruta.Add(kit.Id))
            throw new InvalidOperationException("La composición cargada contiene un ciclo.");
        try
        {
            foreach (var componente in kit.Componentes)
                componente.Elemento.Aceptar(this, checked(cantidad * componente.Cantidad));
        }
