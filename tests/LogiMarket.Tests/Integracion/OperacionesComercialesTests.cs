using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Clientes;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Precios;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LogiMarket.Tests.Integracion;

public sealed class OperacionesComercialesTests
{
    [Theory]
    [InlineData(false, "NORMAL", 0)]
    [InlineData(true, "VIP", 6)]
    public void PromocionYUmbralFisico(bool vip, string promocion, int descuento)
    {
        var calculadora = new CalculadoraPedido([new SinDescuento(), new Vip10(), new Mayorista15()]);
        var categoria = vip ? CategoriaCliente.VIP : CategoriaCliente.NORMAL;
        Assert.Equal((promocion, (decimal)descuento), Resumen(calculadora.Calcular(new(60m, 9, categoria), 5m)));
        Assert.Equal(("MAYORISTA", 15m), Resumen(calculadora.Calcular(new(100m, 10, categoria), 5m)));
    }

    [Fact]
    public void P32_StrategyAdmiteExtensionYRedondeaMitadHaciaFuera()
    {
        var calculadora = new CalculadoraPedido([new SinDescuento(), new Vip10(), new Mayorista15()]);
        var valor = calculadora.Calcular(new(10.05m, 1, CategoriaCliente.VIP), 5m);
        Assert.Equal(1.01m, valor.Descuento);
        var extensible = new CalculadoraPedido([new SinDescuento(), new Vip10(), new Mayorista15(), new Especial()]);
        Assert.Equal(("ESPECIAL", 25m), Resumen(extensible.Calcular(new(100m, 10, CategoriaCliente.VIP), 5m)));
    }

    [Theory]
    [InlineData(ModalidadEntrega.ESTANDAR, 5, 3, "EST-")]
    [InlineData(ModalidadEntrega.EXPRES, 12, 1, "EXP-")]
    public void P12_FabricaProduceFamiliaCoherente(ModalidadEntrega tipo, int tarifa, int plazo, string prefijo)
    {
        var familias = new FamiliasEntrega([new FabricaEntregaEstandar(), new FabricaEntregaExpres()]);
        var familia = familias.Obtener(tipo);
        Assert.Equal((decimal)tarifa, familia.Tarifa.Valor);
        Assert.Equal(plazo, familia.Plazo.Dias);
        Assert.StartsWith(prefijo, familia.Etiqueta.Crear(Guid.NewGuid()));
        Assert.Equal(tipo, familia.Tarifa.Modalidad);
        Assert.Equal(tipo, familia.Plazo.Modalidad);
        Assert.Equal(tipo, familia.Etiqueta.Modalidad);
    }

    [Fact]
    public async Task ABMCatalogoClientesYBorradoresPreservaInstantaneas()
    {
        await using var entorno = new EntornoComercial();
        await entorno.InicializarAsync();
        await entorno.EntrarAsync(Rol.ADMIN);
        await using var scope = entorno.Abrir();
        var catalogo = scope.ServiceProvider.GetRequiredService<AdministrarCatalogo>();
        var clientes = scope.ServiceProvider.GetRequiredService<AdministrarClientes>();
        var pedidos = scope.ServiceProvider.GetRequiredService<AdministrarPedidos>();
        var datos = new DatosProducto("Adaptador", 7m, 0.1m, "Accesorio", null);
        Assert.Equal("C01", (await catalogo.CrearAsync(" c01 ", datos, 5)).Sku);
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalogo.CrearAsync("C01", datos, 0));
        Assert.Equal(57m, await catalogo.CrearKitAsync("KIT_NUEVO", "Nuevo", [new("KIT_OFICINA", 1), new("C01", 1)]));
        var cliente = await clientes.RegistrarAsync("test", "Cliente Normal", CategoriaCliente.NORMAL);
        Assert.Single(await clientes.BuscarAsync("test"));
        await clientes.ActualizarAsync(cliente.Id, "Cliente VIP", CategoriaCliente.VIP, null, true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => pedidos.CrearAsync(cliente.Id, ModalidadEntrega.ESTANDAR, []));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => pedidos.CrearAsync(Guid.NewGuid(), ModalidadEntrega.ESTANDAR, [new("P01", 1)]));
        var borrador = await pedidos.CrearAsync(cliente.Id, ModalidadEntrega.ESTANDAR,
            [new("KIT_TELETRABAJO", 1), new("C01", 1)]);
        Assert.Equal(4, borrador.Valoracion.UnidadesFisicas);
        Assert.Equal(67m, borrador.Valoracion.Subtotal);
        await catalogo.ActualizarAsync("C01", datos with { Precio = 100m });
        await pedidos.CambiarCantidadAsync(borrador.Id, borrador.Lineas[1].Id, 2);
        var actual = await scope.ServiceProvider.GetRequiredService<IRepositorioPedidos>().BuscarAsync(borrador.Id);
        Assert.Equal(74m, actual!.Subtotal);
        await catalogo.DesactivarAsync("C01");
        Assert.False(Assert.Single(await catalogo.BuscarAsync("c01")).Activo);
        await pedidos.CancelarAsync(borrador.Id);
        Assert.Equal(EstadoPedido.CANCELADO, actual.Estado);
    }

    [Fact]
    public async Task P23_CadenaDetienePrimeroClienteYLuegoStock()
    {
        await using var entorno = new EntornoComercial();
        await entorno.InicializarAsync();
        await entorno.EntrarAsync(Rol.VENTAS);
        await using var scope = entorno.Abrir();
        var servicios = scope.ServiceProvider;
        var clientes = servicios.GetRequiredService<AdministrarClientes>();
        var pedidos = servicios.GetRequiredService<AdministrarPedidos>();
        var cliente = await clientes.RegistrarAsync("test", "Cliente", CategoriaCliente.NORMAL);
        var orden = await pedidos.CrearAsync(cliente.Id, ModalidadEntrega.ESTANDAR, [new("KIT_TELETRABAJO", 6)]);
        await clientes.ActualizarAsync(cliente.Id, "Cliente", CategoriaCliente.NORMAL, null, false);
        var error = await Assert.ThrowsAsync<RechazoPedidoException>(() => pedidos.ValidarAsync(orden.Id));
        Assert.Equal("CLIENTE", error.Codigo);
        await clientes.ActualizarAsync(cliente.Id, "Cliente", CategoriaCliente.NORMAL, null, true);
        error = await Assert.ThrowsAsync<RechazoPedidoException>(() => pedidos.ValidarAsync(orden.Id));
        Assert.Equal("STOCK", error.Codigo);
        Assert.Contains("P02", error.Message);
    }

    private static (string, decimal) Resumen(ValoracionPedido v) => (v.Promocion, v.Descuento);

    private sealed class Especial : IPoliticaDescuento
    {
        public string Nombre => "ESPECIAL";
        public int Prioridad => 30;
        public decimal Tasa => 0.25m;
        public bool Aplica(BasePromocion datos) => true;
    }
}
