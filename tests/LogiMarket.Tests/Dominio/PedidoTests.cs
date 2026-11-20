using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using Xunit;

namespace LogiMarket.Tests.Dominio;

public sealed class PedidoTests
{
    private static readonly DateTimeOffset Ahora = DateTimeOffset.UnixEpoch;

    [Fact]
    public void InstantaneaPreservaPreciosComposicionYCantidadesAlEditarCatalogo()
    {
        var mouse = new ProductoSimple(new Sku("P01"), "Mouse USB", 20m, 0.20m, 8);
        var teclado = new ProductoSimple(new Sku("P02"), "Teclado", 30m, 0.70m, 5);
        var interno = new KitCompuesto(new Sku("KIT_A"), "A", mouse, 2);
        var externo = new KitCompuesto(new Sku("KIT_B"), "B", interno, 3);
        externo.AgregarComponente(teclado);
        var pedido = CrearPedido(externo, 2);

        mouse.Actualizar("Otro mouse", 40m, 0.30m);
        interno.CambiarCantidad(mouse.Sku, 4);
        externo.QuitarComponente(teclado.Sku);

        var linea = Assert.Single(pedido.Lineas);
        var captura = Assert.Single(linea.Componentes, c => c.Sku == mouse.Sku);
        Assert.Equal((20m, 0.20m, 6, 2, "0/0/0", "Mouse USB"),
            (captura.PrecioUnitario, captura.PesoKg, captura.CantidadPorUnidad, captura.CantidadEnPadre, captura.Ruta, captura.Nombre));
        Assert.Equal(300m, pedido.Subtotal);
        Assert.Contains(linea.Componentes, c => c.Sku == teclado.Sku);
        Assert.Equal(480m, externo.CalcularPrecio());
    }

    [Fact]
    public void StateRegistraSoloTransicionesAutorizadasPorElEstado()
    {
        var usuario = Guid.NewGuid();
        var pedido = CrearPedido(Producto());
        Assert.Throws<InvalidOperationException>(() => pedido.RegistrarDespacho(usuario, Ahora));

        pedido.Validar(usuario, Ahora);
        pedido.RegistrarPagoAprobado(usuario, Ahora);

        Assert.Throws<InvalidOperationException>(() => pedido.Cancelar(usuario, Ahora));
        Assert.Throws<InvalidOperationException>(() => pedido.RegistrarPagoAprobado(usuario, Ahora));
        Assert.Throws<InvalidOperationException>(() => pedido.CambiarCantidad(pedido.Lineas[0].Id, 2));
        Assert.Throws<InvalidOperationException>(() => pedido.AgregarLinea(Producto(), 1));

        pedido.RegistrarDespacho(usuario, Ahora);
        pedido.RegistrarEntrega(usuario, Ahora);

        Assert.Equal(EstadoPedido.ENTREGADO, pedido.Estado);
        Assert.Equal(new[] { EstadoPedido.BORRADOR, EstadoPedido.VALIDADO, EstadoPedido.PAGADO,
            EstadoPedido.DESPACHADO, EstadoPedido.ENTREGADO }, pedido.Historial.Select(h => h.Nuevo));
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, pedido.Historial.Select(h => h.Secuencia));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StatePermiteCancelarBorradorOValidado(bool validarPrimero)
    {
        var usuario = Guid.NewGuid();
        var pedido = CrearPedido(Producto());
        if (validarPrimero)
            pedido.Validar(usuario, Ahora);

        pedido.Cancelar(usuario, Ahora);
        Assert.Equal(EstadoPedido.CANCELADO, pedido.Estado);
        Assert.Throws<InvalidOperationException>(() => pedido.Validar(usuario, Ahora));
    }

    [Fact]
    public void PedidoRechazaClienteInactivoYLineasInvalidas()
    {
        var cliente = new Cliente("demo", "Cliente de prueba", CategoriaCliente.NORMAL);
        cliente.CambiarDisponibilidad(false);
        var usuario = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() =>
            new Pedido(cliente, ModalidadEntrega.ESTANDAR, Producto(), 1, usuario, Ahora));
        var pedido = CrearPedido(Producto());
        Assert.Throws<ArgumentOutOfRangeException>(() => pedido.AgregarLinea(Producto(), 0));
        Assert.Throws<InvalidOperationException>(() => pedido.QuitarLinea(pedido.Lineas[0].Id));
        Assert.Single(pedido.Lineas);
    }

    [Fact]
    public void UsuarioYClienteRechazanEnumsFueraDelCatalogo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Cliente("demo", "Cliente de prueba", (CategoriaCliente)3));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Usuario("cuenta.demo", (Rol)0, Guid.NewGuid().ToString()));
    }

    private static ProductoSimple Producto() => new(new Sku("P01"), "Mouse USB", 20m, 0.20m, 8);

    private static Pedido CrearPedido(IElementoCatalogo elemento, int cantidad = 1) =>
        new(new Cliente("demo", "Cliente de prueba", CategoriaCliente.NORMAL),
            ModalidadEntrega.ESTANDAR, elemento, cantidad, Guid.NewGuid(), Ahora);
}
