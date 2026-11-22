using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Clientes;
using LogiMarket.Application.Persistencia;
using LogiMarket.Application.Precios;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Pedidos;

namespace LogiMarket.Application.Pedidos;

public sealed record LineaSolicitada(string Sku, int Cantidad);
public sealed record LineaVista(Guid Id, string Sku, string Nombre, decimal PrecioUnitario, int Cantidad);
public sealed record BorradorVisto(Guid Id, Guid ClienteId, EstadoPedido Estado, ModalidadEntrega Entrega,
    IReadOnlyList<LineaVista> Lineas, ValoracionPedido Valoracion);

public sealed class AdministrarPedidos(IRepositorioPedidos pedidos, IRepositorioClientes clientes,
    AdministrarCatalogo catalogo, AutorizacionOperaciones acceso, IUnidadDeTrabajo unidad,
    CadenaValidacionPedido cadena, ValorarPedido valorar, TimeProvider reloj, HistorialBorrador historial)
{
    public async Task<BorradorVisto> CrearAsync(Guid clienteId, ModalidadEntrega entrega,
        IReadOnlyList<LineaSolicitada> lineas, CancellationToken ct = default)
    {
        var actor = await acceso.ExigirAsync(Operacion.EditarPedido, ct);
        return await unidad.EnTransaccionAsync(async token =>
        {
            var cliente = await clientes.BuscarPorIdAsync(clienteId, token) ?? throw new KeyNotFoundException("El cliente no existe.");
            var builder = new PedidoBuilder();
            foreach (var linea in lineas)
                builder.Agregar(await catalogo.ObtenerElementoActivoAsync(linea.Sku, token), linea.Cantidad);
            var pedido = builder.Construir(cliente, entrega, actor.UsuarioId, reloj.GetUtcNow());
            pedidos.Agregar(pedido);
            return Proyectar(pedido, cliente.Categoria);
        }, ct);
    }

    public async Task<BorradorVisto> ConsultarBorradorAsync(Guid id, CancellationToken ct = default)
    {
        await acceso.ExigirAsync(Operacion.Consultar, ct);
        var pedido = await ObtenerAsync(id, ct);
        if (pedido.Estado is not (EstadoPedido.BORRADOR or EstadoPedido.VALIDADO))
            throw new RechazoPedidoException("ESTADO", "La cotización está disponible solo antes del pago.");

        var cliente = await clientes.BuscarPorIdAsync(pedido.ClienteId, ct)
            ?? throw new KeyNotFoundException("El cliente no existe.");
        return Proyectar(pedido, cliente.Categoria);
    }

    public async Task<BorradorVisto> ClonarBorradorAsync(Guid id, CancellationToken ct = default)
    {
        var actor = await acceso.ExigirAsync(Operacion.EditarPedido, ct);
        return await unidad.EnTransaccionAsync(async token =>
        {
            var plantilla = await ObtenerAsync(id, token);
            var cliente = await clientes.BuscarPorIdAsync(plantilla.ClienteId, token)
                ?? throw new KeyNotFoundException("El cliente no existe.");
            var copia = plantilla.ClonarComoBorrador(cliente, actor.UsuarioId, reloj.GetUtcNow());
            pedidos.Agregar(copia);
            return Proyectar(copia, cliente.Categoria);
        }, ct);
    }

    public Task<BorradorVisto> DeshacerAsync(Guid id, CancellationToken ct = default) =>
        NavegarHistorialAsync(id, -1, ct);

    public Task<BorradorVisto> RehacerAsync(Guid id, CancellationToken ct = default) =>
        NavegarHistorialAsync(id, 1, ct);

    public Task AgregarLineaAsync(Guid pedidoId, LineaSolicitada linea, CancellationToken ct = default) =>
        EditarAsync(pedidoId, async (p, token) =>
            p.AgregarLinea(await catalogo.ObtenerElementoActivoAsync(linea.Sku, token), linea.Cantidad), ct);

    public Task ReemplazarLineaAsync(Guid pedidoId, Guid lineaId, LineaSolicitada linea, CancellationToken ct = default) =>
        EditarAsync(pedidoId, async (p, token) =>
        {
            p.AgregarLinea(await catalogo.ObtenerElementoActivoAsync(linea.Sku, token), linea.Cantidad);
            p.QuitarLinea(lineaId);
        }, ct);

    public Task CambiarCantidadAsync(Guid pedidoId, Guid lineaId, int cantidad, CancellationToken ct = default) =>
        EditarAsync(pedidoId, (p, _) =>
        {
            p.CambiarCantidad(lineaId, cantidad);
            return Task.CompletedTask;
        }, ct);

    public Task QuitarLineaAsync(Guid pedidoId, Guid lineaId, CancellationToken ct = default) =>
        EditarAsync(pedidoId, (p, _) =>
        {
            p.QuitarLinea(lineaId);
            return Task.CompletedTask;
        }, ct);

    public Task CambiarEntregaAsync(Guid pedidoId, ModalidadEntrega entrega, CancellationToken ct = default) =>
        EditarAsync(pedidoId, (p, _) =>
        {
            p.CambiarEntrega(entrega);
            return Task.CompletedTask;
        }, ct);

    public async Task ValidarAsync(Guid id, CancellationToken ct = default)
    {
        var actor = await acceso.ExigirAsync(Operacion.ValidarPedido, ct);
        await unidad.EnTransaccionAsync(async token =>
        {
            var pedido = await ObtenerAsync(id, token);
            var cliente = await clientes.BuscarPorIdAsync(pedido.ClienteId, token);
            await cadena.RevisarAsync(pedido, cliente, paraConfirmar: false, token);
            pedido.Validar(actor.UsuarioId, reloj.GetUtcNow());
            return true;
        }, ct);
        historial.Cerrar(id);
    }

    public async Task CancelarAsync(Guid id, CancellationToken ct = default)
    {
        var actor = await acceso.ExigirAsync(Operacion.CancelarPedido, ct);
        await unidad.EnTransaccionAsync(async token =>
        {
            var pedido = await ObtenerAsync(id, token);
            pedido.Cancelar(actor.UsuarioId, reloj.GetUtcNow());
            return true;
        }, ct);
        historial.Cerrar(id);
    }

    private async Task EditarAsync(Guid id, Func<Pedido, CancellationToken, Task> editar, CancellationToken ct)
    {
        var actor = await acceso.ExigirAsync(Operacion.EditarPedido, ct);
        var cambio = await unidad.EnTransaccionAsync(async token =>
        {
            var pedido = await ObtenerAsync(id, token);
            var version = pedido.Version;
            var anterior = pedido.CapturarBorrador();
            await editar(pedido, token);
            return (VersionAnterior: version, Anterior: anterior, Nueva: pedido.Version, Siguiente: pedido.CapturarBorrador());
        }, ct);
        historial.Registrar(id, actor.UsuarioId, cambio.VersionAnterior, cambio.Anterior, cambio.Nueva, cambio.Siguiente);
    }

    private async Task<BorradorVisto> NavegarHistorialAsync(Guid id, int direccion, CancellationToken ct)
    {
        var actor = await acceso.ExigirAsync(Operacion.EditarPedido, ct);
        var cambio = await unidad.EnTransaccionAsync(async token =>
        {
            var pedido = await ObtenerAsync(id, token);
            var cliente = await clientes.BuscarPorIdAsync(pedido.ClienteId, token)
                ?? throw new KeyNotFoundException("El cliente no existe.");
            var version = pedido.Version;
            var destino = historial.Obtener(id, actor.UsuarioId, version, direccion);
            pedido.RestaurarBorrador(destino.Estado);
            return (Anterior: version, Nueva: pedido.Version, destino.Indice, Vista: Proyectar(pedido, cliente.Categoria));
        }, ct);
        historial.Confirmar(id, actor.UsuarioId, cambio.Anterior, cambio.Nueva, cambio.Indice);
        return cambio.Vista;
    }

    private async Task<Pedido> ObtenerAsync(Guid id, CancellationToken ct) =>
        await pedidos.BuscarAsync(id, ct) ?? throw new KeyNotFoundException("El pedido no existe.");

    private BorradorVisto Proyectar(Pedido pedido, Domain.Clientes.CategoriaCliente categoria)
    {
        var valoracion = valorar.Calcular(pedido, categoria);
        return new(pedido.Id, pedido.ClienteId, pedido.Estado, pedido.Entrega,
            pedido.Lineas.Select(l => new LineaVista(l.Id, l.Sku.Valor, l.Nombre, l.PrecioUnitario, l.Cantidad)).ToArray(),
            valoracion);
    }
}
