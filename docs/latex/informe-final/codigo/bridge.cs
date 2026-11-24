public abstract class Comprobante(ISalidaDocumento salida)
{
    public Task EmitirAsync(CancellationToken ct = default) => salida.EmitirAsync(Construir(), ct);

    protected abstract DocumentoInterno Construir();

    protected static string Usd(decimal importe) => importe.ToString("0.00", CultureInfo.InvariantCulture);
    protected const string Leyenda = "Comprobante interno de demostración. Sin validez tributaria.";
}
