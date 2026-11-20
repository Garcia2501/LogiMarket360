namespace LogiMarket.Domain.Seguridad;

public enum Rol
{
    ADMIN = 1,
    VENTAS,
    BODEGA,
    CONSULTA
}

public sealed class Usuario
{
    public Usuario(string nombre, Rol rol, string hashClave)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashClave);
        if (!Enum.IsDefined(rol))
            throw new ArgumentOutOfRangeException(nameof(rol));

        Id = Guid.NewGuid();
        Nombre = NormalizarNombre(nombre);
        Rol = rol;
        HashClave = hashClave;
    }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; }
    public Rol Rol { get; private set; }
    public string HashClave { get; private set; }

    public static string NormalizarNombre(string nombre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        return nombre.Trim().ToLowerInvariant();
    }
}
