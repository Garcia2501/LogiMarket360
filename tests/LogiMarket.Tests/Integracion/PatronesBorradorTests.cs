using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LogiMarket.Tests.Integracion.ConfirmacionTests;

namespace LogiMarket.Tests.Integracion;

public sealed class PatronesBorradorTests
{
    [Fact]
    public async Task P14_ClonaInstantaneaDeKitSinIdentidadesYPermiteEdicionIndependiente()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "plantilla", CategoriaCliente.VIP, ModalidadEntrega.ESTANDAR,
            [new("KIT_TELETRABAJO", 1), new("P04", 1)], validar: false);
        await entorno.EntrarAsync(Rol.ADMIN);
        await using var scope = entorno.Abrir();
        var sp = scope.ServiceProvider;
        var servicio = sp.GetRequiredService<AdministrarPedidos>();
        var repo = sp.GetRequiredService<IRepositorioPedidos>();
        await sp.GetRequiredService<AdministrarCatalogo>().ActualizarKitAsync("KIT_TELETRABAJO", "Nueva composición", [new("P05", 1)]);

        var copia1 = await servicio.ClonarBorradorAsync(id);
        var copia2 = await servicio.ClonarBorradorAsync(id);
        var kit = Assert.Single(copia1.Lineas, l => l.Sku == "KIT_TELETRABAJO");
        await servicio.CambiarCantidadAsync(copia1.Id, kit.Id, 2);
        var plantilla = (await repo.BuscarAsync(id))!;
        var primero = (await repo.BuscarAsync(copia1.Id))!;
        var segundo = (await repo.BuscarAsync(copia2.Id))!;
        Assert.Equal((100m, 160m, 100m), (plantilla.Subtotal, primero.Subtotal, segundo.Subtotal));
        Assert.Equal(3, new[] { plantilla.Id, primero.Id, segundo.Id }.Distinct().Count());
        var original = Assert.Single(plantilla.Lineas, l => l.Sku.Valor == "KIT_TELETRABAJO");
        var editada = Assert.Single(primero.Lineas, l => l.Sku == original.Sku);
        Assert.NotSame(original.Componentes, editada.Componentes);
        Assert.DoesNotContain(editada.Componentes, c => original.Componentes.Any(o => o.Id == c.Id));
        Assert.Equal(new[] { "P01", "P02", "P03" }, editada.Componentes.Where(c => c.EsProducto).Select(c => c.Sku.Valor).Order());
        Assert.Equal(EstadoPedido.BORRADOR, Assert.Single(primero.Historial).Nuevo);
        await using var db = entorno.AbrirBase();
        Assert.Empty(await db.Pagos.ToListAsync());
    }

    [Fact]
    public async Task P29_DeshacerYRehacerRestauraYElPagoCierraElHistorial()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "memento", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("KIT_TELETRABAJO", 1)], validar: false);
        await using var scope = entorno.Abrir();
        var servicio = scope.ServiceProvider.GetRequiredService<AdministrarPedidos>();
        var original = await servicio.ConsultarBorradorAsync(id);
        await servicio.CambiarCantidadAsync(id, Assert.Single(original.Lineas).Id, 2);
        await servicio.AgregarLineaAsync(id, new("P04", 1));
        await servicio.CambiarEntregaAsync(id, ModalidadEntrega.EXPRES);

        var uno = await servicio.DeshacerAsync(id);
        Assert.Equal(ModalidadEntrega.ESTANDAR, uno.Entrega);
        Assert.Equal(2, uno.Lineas.Count);
        var dos = await servicio.DeshacerAsync(id);
        Assert.Equal(2, Assert.Single(dos.Lineas).Cantidad);
        Assert.Equal(6, dos.Valoracion.UnidadesFisicas);
        Assert.Equal(120m, dos.Valoracion.Subtotal);
        var rehecho = await servicio.RehacerAsync(id);
        Assert.Equal(160m, rehecho.Valoracion.Subtotal);

        await servicio.ValidarAsync(id);
        await ConfirmarAsync(entorno, id, "memento", ProveedorPago.LEGADO);
        await using var nuevoScope = entorno.Abrir();
        var nuevoServicio = nuevoScope.ServiceProvider.GetRequiredService<AdministrarPedidos>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => nuevoServicio.DeshacerAsync(id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => nuevoServicio.ClonarBorradorAsync(id));
        await using var db = entorno.AbrirBase();
        Assert.Single(await db.Pagos.ToListAsync());
        Assert.Equal(4, await db.Movimientos.CountAsync());
        Assert.Equal(EstadoPedido.PAGADO, (await db.Pedidos.SingleAsync()).Estado);
    }

    [Fact]
    public async Task LimitaHistorialACincoEstadosYDescartaLaRamaRehecha()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "limite", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)], validar: false);
        await using var scope = entorno.Abrir();
        var svc = scope.ServiceProvider.GetRequiredService<AdministrarPedidos>();
        var vista = await svc.ConsultarBorradorAsync(id);
        var lineaId = Assert.Single(vista.Lineas).Id;
        for (int i = 2; i <= 7; i++)
            await svc.CambiarCantidadAsync(id, lineaId, i);

        for (int esperado = 6; esperado >= 3; esperado--)
            Assert.Equal(esperado, Assert.Single((await svc.DeshacerAsync(id)).Lineas).Cantidad);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DeshacerAsync(id));
        vista = await svc.ConsultarBorradorAsync(id);
        await svc.CambiarCantidadAsync(id, Assert.Single(vista.Lineas).Id, 9);
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.RehacerAsync(id));
    }
}
