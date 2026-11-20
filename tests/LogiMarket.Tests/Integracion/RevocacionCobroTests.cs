using LogiMarket.Application.Pagos;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Infrastructure.Pagos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LogiMarket.Tests.Integracion.ConfirmacionTests;

namespace LogiMarket.Tests.Integracion;

public sealed class RevocacionCobroTests
{
    [Fact]
    public async Task CierreDeSesionAntesDeLaPasarelaDeniegaYConservaAuditoriaTrasRollback()
    {
        await using var entorno = await CrearEntornoAsync(s => s
            .AddScoped<ResolutorPasarelas>()
            .AddScoped<IResolutorPasarelas, ResolverTrasCerrarSesion>());
        var pedido = await CrearPedidoAsync(entorno, "revocacion", CategoriaCliente.NORMAL,
            ModalidadEntrega.ESTANDAR, [new("P01", 1)]);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            ConfirmarAsync(entorno, pedido, "revocacion", ProveedorPago.LEGADO));

        await using var db = entorno.AbrirBase();
        Assert.Empty(await db.Pagos.ToListAsync());
        Assert.Empty(await db.Movimientos.ToListAsync());
        Assert.Empty(await db.EventosOutbox.ToListAsync());
        Assert.Equal(EstadoPedido.VALIDADO, (await db.Pedidos.SingleAsync()).Estado);
        var denegacion = Assert.Single(await db.Auditorias.ToListAsync());
        Assert.Equal(("SEGURIDAD", "Cobrar", "DENEGADO"), (denegacion.Categoria, denegacion.Accion, denegacion.Resultado));
    }

    private sealed class ResolverTrasCerrarSesion(ResolutorPasarelas interno, SesionLocal sesion) : IResolutorPasarelas
    {
        public IPasarelaPago Obtener(ProveedorPago proveedor)
        {
            sesion.Cerrar();
            return interno.Obtener(proveedor);
        }
    }
}
