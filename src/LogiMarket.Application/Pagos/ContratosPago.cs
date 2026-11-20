namespace LogiMarket.Application.Pagos;

public enum ProveedorPago
{
    LEGADO = 1,
    ALTERNO
}

public sealed record RespuestaPasarela(bool Aprobado, string? Referencia, string? Motivo);

public interface IPasarelaPago
{
    Task<RespuestaPasarela> CobrarAsync(decimal importe, string clave, CancellationToken ct = default);
}

public interface IResolutorPasarelas
{
    IPasarelaPago Obtener(ProveedorPago proveedor);
}

public interface ITrazasPago
{
    void Anotar(ProveedorPago proveedor, string clave, string etapa, string resultado);
    Task GuardarAsync(CancellationToken ct = default);
}
