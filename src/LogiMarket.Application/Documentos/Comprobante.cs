using System.Globalization;
using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Pagos;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Documentos;

public sealed record DocumentoInterno(string Titulo, string Contenido);

public interface ISalidaDocumento
{
    Task EmitirAsync(DocumentoInterno documento, CancellationToken ct = default);
}

public interface IFabricaArchivoTxt
{
    ISalidaDocumento Crear(string ruta);
}

public sealed class VistaPrevia(Func<DocumentoInterno, CancellationToken, Task> mostrar) : ISalidaDocumento
{
    public Task EmitirAsync(DocumentoInterno documento, CancellationToken ct = default) => mostrar(documento, ct);
}

// patron bridge: separa el contenido de venta o devolucion del medio de salida
public abstract class Comprobante(ISalidaDocumento salida)
{
    public Task EmitirAsync(CancellationToken ct = default) => salida.EmitirAsync(Construir(), ct);

    protected abstract DocumentoInterno Construir();

    protected static string Usd(decimal importe) => importe.ToString("0.00", CultureInfo.InvariantCulture);
    protected const string Leyenda = "Comprobante interno de demostración. Sin validez tributaria.";
}

public sealed class ComprobanteVenta(ISalidaDocumento salida, Pedido pedido, PagoAplicado pago) : Comprobante(salida)
{
    protected override DocumentoInterno Construir()
    {
        var lineas = new List<string> { Leyenda, $"Pedido: {pedido.Id:D}", $"Pago: {pago.Referencia}" };
        lineas.AddRange(pedido.Lineas.Select(l =>
            $"{l.Sku.Valor} · {l.Nombre} · {l.Cantidad} × USD {Usd(l.PrecioUnitario)}"));
        lineas.Add($"Subtotal USD {Usd(pago.Subtotal)}");
        lineas.Add($"Descuento USD {Usd(pago.Descuento)}");
        lineas.Add($"Envío USD {Usd(pago.Tarifa)}");
        lineas.Add($"Total cobrado USD {Usd(pago.Importe)}");
        return new("Comprobante de venta", string.Join(Environment.NewLine, lineas));
    }
}

public sealed class ComprobanteDevolucion(ISalidaDocumento salida, DevolucionAplicada devolucion) : Comprobante(salida)
{
    protected override DocumentoInterno Construir() => new("Comprobante de devolución", string.Join(Environment.NewLine,
        Leyenda, $"Pedido: {devolucion.PedidoId:D}", $"Motivo: {devolucion.Motivo}",
        $"Reembolso: {devolucion.ReferenciaReembolso}", $"Importe reembolsado USD {Usd(devolucion.Importe)}",
        "El envío está excluido del reembolso."));
}
