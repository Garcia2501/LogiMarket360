using LogiMarket.Application.Seguridad;

namespace LogiMarket.Application.Pagos;

// patron proxy: comprueba el permiso antes de invocar el decorador o el proveedor
public sealed class PagoAutorizadoProxy(AutorizacionOperaciones acceso, IPasarelaPago siguiente) : IPasarelaPago
{
    public async Task<RespuestaPasarela> CobrarAsync(decimal importe, string clave, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Cobrar, ct);
        return await siguiente.CobrarAsync(importe, clave, ct);
    }
}
