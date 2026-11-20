using LogiMarket.Application.Persistencia;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Clientes;

namespace LogiMarket.Application.Clientes;

public sealed record ClienteVisto(Guid Id, string Identificacion, string Nombre, CategoriaCliente Categoria,
    bool Activo, string? Contacto);

public sealed class AdministrarClientes(IRepositorioClientes clientes, IUnidadDeTrabajo unidad, AutorizacionOperaciones acceso)
{
    public async Task<ClienteVisto> RegistrarAsync(string identificacion, string nombre, CategoriaCliente categoria,
        string? contacto = null, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.GestionarClientes, ct);
        return await unidad.EnTransaccionAsync(async token =>
        {
            if (await clientes.BuscarAsync(identificacion, token) is not null)
                throw new InvalidOperationException("Ya existe un cliente con esa identificación.");
            var cliente = new Cliente(identificacion, nombre, categoria, contacto);
            clientes.Agregar(cliente);
            return Proyectar(cliente);
        }, ct);
    }

    public async Task<ClienteVisto> ActualizarAsync(Guid id, string nombre, CategoriaCliente categoria, string? contacto,
        bool activo, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.GestionarClientes, ct);
        return await unidad.EnTransaccionAsync(async token =>
        {
            var cliente = await clientes.BuscarPorIdAsync(id, token) ?? throw new KeyNotFoundException("El cliente no existe.");
            cliente.Actualizar(nombre, categoria, contacto);
            cliente.CambiarDisponibilidad(activo);
            return Proyectar(cliente);
        }, ct);
    }

    public async Task<IReadOnlyList<ClienteVisto>> BuscarAsync(string? texto = null, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        var termino = texto?.Trim() ?? "";
        return (await clientes.ListarAsync(ct)).Where(c =>
            c.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
            c.Identificacion.Contains(termino, StringComparison.OrdinalIgnoreCase)).Select(Proyectar).ToArray();
    }

    private static ClienteVisto Proyectar(Cliente c) => new(c.Id, c.Identificacion, c.Nombre, c.Categoria, c.Activo, c.Contacto);
}
