using LogiMarket.Domain.Seguridad;

namespace LogiMarket.Application.Seguridad;

public sealed record IdentidadSesion(Guid UsuarioId, string Nombre, Rol Rol);

public sealed class SesionLocal
{
    private IdentidadSesion? _actual;

    public IdentidadSesion? Actual => Volatile.Read(ref _actual);

    internal void Iniciar(Usuario usuario) =>
        Volatile.Write(ref _actual, new IdentidadSesion(usuario.Id, usuario.Nombre, usuario.Rol));

    public void Cerrar() => Volatile.Write(ref _actual, null);
}

public sealed class AutenticacionLocal(IConsultaUsuarios usuarios, IHashClave hash, SesionLocal sesion)
{
    public async Task<bool> IniciarAsync(string nombre, string clave, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(clave))
            return false;

        var usuario = await usuarios.BuscarAsync(nombre, ct);
        if (usuario is null || !hash.Verificar(clave, usuario.HashClave))
            return false;

        sesion.Iniciar(usuario);
        return true;
    }

    public void Cerrar() => sesion.Cerrar();
}
