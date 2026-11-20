namespace LogiMarket.Domain.Clientes;

public enum CategoriaCliente
{
    NORMAL = 1,
    VIP
}

public sealed class Cliente
{
    private Cliente() { }

    public Cliente(string identificacion, string nombre, CategoriaCliente categoria, string? contacto = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identificacion);
        Id = Guid.NewGuid();
        Identificacion = identificacion.Trim();
        Activo = true;
        Actualizar(nombre, categoria, contacto);
    }

    public Guid Id { get; private set; }
    public string Identificacion { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public CategoriaCliente Categoria { get; private set; }
    public string? Contacto { get; private set; }
    public bool Activo { get; private set; }

    public void Actualizar(string nombre, CategoriaCliente categoria, string? contacto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        if (!Enum.IsDefined(categoria))
            throw new ArgumentOutOfRangeException(nameof(categoria));

        Nombre = nombre.Trim();
        Categoria = categoria;
        Contacto = string.IsNullOrWhiteSpace(contacto) ? null : contacto.Trim();
    }

    public void CambiarDisponibilidad(bool activo) => Activo = activo;
}
