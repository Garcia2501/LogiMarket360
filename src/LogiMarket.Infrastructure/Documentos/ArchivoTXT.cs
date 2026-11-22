using System.Text;
using LogiMarket.Application.Documentos;

namespace LogiMarket.Infrastructure.Documentos;

public sealed class ArchivoTXT : ISalidaDocumento
{
    private readonly string _ruta;

    public ArchivoTXT(string ruta)
    {
        _ruta = Path.GetFullPath(ruta);
        if (!string.Equals(Path.GetExtension(_ruta), ".txt", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El comprobante requiere extensión .txt.", nameof(ruta));
    }

    public Task EmitirAsync(DocumentoInterno documento, CancellationToken ct = default) =>
        File.WriteAllTextAsync(_ruta, $"{documento.Titulo}{Environment.NewLine}{documento.Contenido}",
            Encoding.UTF8, ct);
}

public sealed class FabricaArchivoTxt : IFabricaArchivoTxt
{
    public ISalidaDocumento Crear(string ruta) => new ArchivoTXT(ruta);
}
