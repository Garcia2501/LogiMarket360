using LogiMarket.Domain.Seguridad;

namespace LogiMarket.Application.Seguridad;

public enum Operacion
{
    Consultar,
    GestionarCatalogo,
    GestionarClientes,
    EditarPedido,
    ValidarPedido,
    CancelarPedido,
    Cobrar,
    Despachar,
    Entregar,
    Devolver,
    ReprocesarEventos,
    ConsultarAuditoria,
    ConfigurarDemostracion
}

public interface IAuditoriaAcceso
{
    Task DenegadoAsync(Guid? usuarioId, Operacion operacion, CancellationToken ct);
    Task GuardarPendientesAsync(CancellationToken ct);
}

public sealed class AutorizacionOperaciones(SesionLocal sesion, IAuditoriaAcceso auditoria)
{
    public async Task<IdentidadSesion> ExigirAsync(Operacion operacion, CancellationToken ct = default)
    {
        var identidad = sesion.Actual;
        if (identidad is not null && Permite(identidad.Rol, operacion))
            return identidad;

        await auditoria.DenegadoAsync(identidad?.UsuarioId, operacion, ct);
        throw new UnauthorizedAccessException("La sesión no tiene permiso para esta operación.");
    }

    public Task GuardarDenegacionesPendientesAsync(CancellationToken ct) => auditoria.GuardarPendientesAsync(ct);

    public static bool Permite(Rol rol, Operacion operacion)
    {
        if (!Enum.IsDefined(operacion) || !Enum.IsDefined(rol))
            return false;

        return operacion == Operacion.Consultar || rol == Rol.ADMIN || (rol, operacion) switch
        {
            (Rol.VENTAS, Operacion.GestionarClientes or Operacion.EditarPedido or Operacion.ValidarPedido
                or Operacion.CancelarPedido or Operacion.Cobrar) => true,
            (Rol.BODEGA, Operacion.Despachar or Operacion.Entregar) => true,
            _ => false
        };
    }
}
