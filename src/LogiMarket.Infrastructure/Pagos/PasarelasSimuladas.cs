using System.Security.Cryptography;
using System.Text;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Seguridad;

namespace LogiMarket.Infrastructure.Pagos;

public sealed class EstadoSimulacionPago
{
    public bool RechazarCobro { get; set; }
}

public sealed class PasarelaLegadaSimulada(EstadoSimulacionPago simulacion)
{
    public string? CobrarCentavos(long centavos, string clave)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(centavos);
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);
        return simulacion.RechazarCobro ? null : ReferenciaSimulada.Crear("LEG", clave);
    }
}

// patron adapter: traduce el importe decimal y el resultado de la pasarela legada en centavos
public sealed class PasarelaLegadaAdapter(PasarelaLegadaSimulada legado) : IPasarelaPago
{
    public Task<RespuestaPasarela> CobrarAsync(decimal importe, string clave, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var centavos = ReferenciaSimulada.Centavos(importe);
        return Task.FromResult(ReferenciaSimulada.Respuesta(legado.CobrarCentavos(centavos, clave)));
    }
}

public sealed class PasarelaAlternaSimulada(EstadoSimulacionPago simulacion) : IPasarelaPago
{
    public Task<RespuestaPasarela> CobrarAsync(decimal importe, string clave, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ReferenciaSimulada.Centavos(importe);
        ArgumentException.ThrowIfNullOrWhiteSpace(clave);
        return Task.FromResult(ReferenciaSimulada.Respuesta(simulacion.RechazarCobro
            ? null : ReferenciaSimulada.Crear("ALT", clave)));
    }
}

internal static class ReferenciaSimulada
{
    public static string Crear(string prefijo, string clave) =>
        $"{prefijo}-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clave)))}";

    public static long Centavos(decimal importe)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(importe);
        if (decimal.Truncate(importe * 100m) != importe * 100m)
            throw new ArgumentException("El cobro exige un importe expresado en centavos.", nameof(importe));
        return checked((long)(importe * 100m));
    }

    public static RespuestaPasarela Respuesta(string? referencia) =>
        referencia is null ? new(false, null, "El proveedor de demostración rechazó el cobro.") : new(true, referencia, null);
}

public sealed class ResolutorPasarelas(AutorizacionOperaciones acceso, ITrazasPago trazas,
    PasarelaLegadaAdapter legado, PasarelaAlternaSimulada alterna) : IResolutorPasarelas
{
    public IPasarelaPago Obtener(ProveedorPago proveedor)
    {
        IPasarelaPago pasarela = proveedor switch
        {
            ProveedorPago.LEGADO => legado,
            ProveedorPago.ALTERNO => alterna,
            _ => throw new ArgumentOutOfRangeException(nameof(proveedor))
        };
        return new PagoAutorizadoProxy(acceso, new AuditoriaPagoDecorator(proveedor, trazas, pasarela));
    }
}
