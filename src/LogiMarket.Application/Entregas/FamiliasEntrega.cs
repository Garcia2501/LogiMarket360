using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Entregas;

public sealed record TarifaEntrega(ModalidadEntrega Modalidad, decimal Valor);
public sealed record PlazoEntrega(ModalidadEntrega Modalidad, int Dias);
public sealed record EtiquetaEntrega(ModalidadEntrega Modalidad, string Prefijo)
{
    public string Crear(Guid pedidoId) => $"{Prefijo}-{pedidoId:N}";
}

public sealed class FamiliaEntrega
{
    internal FamiliaEntrega(TarifaEntrega tarifa, PlazoEntrega plazo, EtiquetaEntrega etiqueta)
    {
        if (tarifa.Modalidad != plazo.Modalidad || tarifa.Modalidad != etiqueta.Modalidad)
            throw new ArgumentException("Los componentes no pertenecen a la misma familia de entrega.");
        Tarifa = tarifa;
        Plazo = plazo;
        Etiqueta = etiqueta;
    }

    public TarifaEntrega Tarifa { get; }
    public PlazoEntrega Plazo { get; }
    public EtiquetaEntrega Etiqueta { get; }
}

// patron abstract factory: crea tarifa plazo y etiqueta de una modalidad compatible
public abstract class FabricaEntrega
{
    public abstract ModalidadEntrega Modalidad { get; }
    public FamiliaEntrega Crear() => new(CrearTarifa(), CrearPlazo(), CrearEtiqueta());
    protected abstract TarifaEntrega CrearTarifa();
    protected abstract PlazoEntrega CrearPlazo();
    protected abstract EtiquetaEntrega CrearEtiqueta();
}

public sealed class FabricaEntregaEstandar : FabricaEntrega
{
    public override ModalidadEntrega Modalidad => ModalidadEntrega.ESTANDAR;
    protected override TarifaEntrega CrearTarifa() => new(Modalidad, 5.00m);
    protected override PlazoEntrega CrearPlazo() => new(Modalidad, 3);
    protected override EtiquetaEntrega CrearEtiqueta() => new(Modalidad, "EST");
}

public sealed class FabricaEntregaExpres : FabricaEntrega
{
    public override ModalidadEntrega Modalidad => ModalidadEntrega.EXPRES;
    protected override TarifaEntrega CrearTarifa() => new(Modalidad, 12.00m);
    protected override PlazoEntrega CrearPlazo() => new(Modalidad, 1);
    protected override EtiquetaEntrega CrearEtiqueta() => new(Modalidad, "EXP");
}

public sealed class FamiliasEntrega(IEnumerable<FabricaEntrega> fabricas)
{
    private readonly IReadOnlyDictionary<ModalidadEntrega, FabricaEntrega> _fabricas = fabricas.ToDictionary(f => f.Modalidad);

    public FamiliaEntrega Obtener(ModalidadEntrega modalidad) => _fabricas[modalidad].Crear();
}
