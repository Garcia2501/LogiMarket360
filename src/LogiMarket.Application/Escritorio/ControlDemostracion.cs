using LogiMarket.Application.Pagos;
using LogiMarket.Application.Seguridad;

namespace LogiMarket.Application.Escritorio;

public sealed record AjustesDemostracion(ProveedorPago Proveedor, bool RechazarCobro, bool FallarAvisos);

public sealed class EstadoDemostracion(bool habilitado)
{
    private AjustesDemostracion _ajustes = new(ProveedorPago.LEGADO, false, false);
    public bool Habilitado { get; } = habilitado;
    public AjustesDemostracion Ajustes => Volatile.Read(ref _ajustes);

    internal void Aplicar(AjustesDemostracion ajustes) => Volatile.Write(ref _ajustes, ajustes);
}

public sealed class ControlDemostracion(EstadoDemostracion estado, AutorizacionOperaciones acceso)
{
    public async Task AplicarAsync(AjustesDemostracion ajustes, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.ConfigurarDemostracion, ct);
        if (!estado.Habilitado)
            throw new InvalidOperationException("Los fallos simulados requieren abrir la base de demostración.");
        if (!Enum.IsDefined(ajustes.Proveedor))
            throw new ArgumentOutOfRangeException(nameof(ajustes));
        estado.Aplicar(ajustes);
    }
}
