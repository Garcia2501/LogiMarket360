public abstract class FabricaEntrega
{
    public abstract ModalidadEntrega Modalidad { get; }
    public FamiliaEntrega Crear() => new(CrearTarifa(), CrearPlazo(), CrearEtiqueta());
    protected abstract TarifaEntrega CrearTarifa();
    protected abstract PlazoEntrega CrearPlazo();
    protected abstract EtiquetaEntrega CrearEtiqueta();
}
