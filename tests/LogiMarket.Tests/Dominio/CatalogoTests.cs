using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Inventario;
using Xunit;

namespace LogiMarket.Tests.Dominio;

public sealed class CatalogoTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SkuRequiereUnValor(string? valor)
    {
        Assert.ThrowsAny<ArgumentException>(() => new Sku(valor!));
    }

    [Fact]
    public void SkuNormalizaSinPerderIdentidad()
    {
        Assert.Equal(new Sku("P01"), new Sku(" p01 "));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(-1, 0, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 0, -1)]
    public void ProductoRechazaValoresInvalidos(int precio, int peso, int stock)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProductoSimple(new Sku("P01"), "Mouse USB", precio, peso, stock));
    }

    [Fact]
    public void ProductoAdmitePesoYStockCeroYPrecioDecimalPositivo()
    {
        var producto = new ProductoSimple(new Sku("P01"), "Mouse USB", 0.001m, 0m, 0);
        Assert.Equal((0.001m, 0m, 0), (producto.Precio, producto.PesoKg, producto.Stock));
    }

    [Fact]
    public void RechazarMovimientoNoCambiaElStockNiSuVersion()
    {
        var producto = CrearProducto("P01", 20m, 0.20m);
        var version = producto.Version;
        var ahora = DateTimeOffset.UnixEpoch;

        Assert.Throws<InvalidOperationException>(() =>
            producto.MoverInventario(-9, TipoMovimiento.VENTA, "pedido", "mov-1", ahora));
        Assert.Throws<ArgumentException>(() =>
            producto.MoverInventario(1, TipoMovimiento.VENTA, "pedido", "mov-2", ahora));
        Assert.Equal(8, producto.Stock);
        Assert.Equal(version, producto.Version);
    }

    [Fact]
    public void MovimientoConservaMotivoReferenciaYSaldos()
    {
        var producto = CrearProducto("P01", 20m, 0.20m);
        var version = producto.Version;
        var movimiento = producto.MoverInventario(-3, TipoMovimiento.VENTA, "pedido", "mov-1", DateTimeOffset.UnixEpoch);

        Assert.Equal((producto.Id, -3, 5, "pedido", "mov-1"),
            (movimiento.ProductoId, movimiento.Cantidad, movimiento.StockResultante, movimiento.Motivo, movimiento.Referencia));
        Assert.Equal(5, producto.Stock);
        Assert.NotEqual(version, producto.Version);
    }

    [Fact]
    public void CalcularKitsOficialesYExpandirAnidacion()
    {
        var mouse = CrearProducto("P01", 20m, 0.20m);
        var teclado = CrearProducto("P02", 30m, 0.70m);
        var usb = CrearProducto("P03", 10m, 0.10m);
        var oficina = new KitCompuesto(new Sku("KIT_OFICINA"), "Oficina", mouse);
        oficina.AgregarComponente(teclado);
        var teletrabajo = new KitCompuesto(new Sku("KIT_TELETRABAJO"), "Teletrabajo", oficina);
        teletrabajo.AgregarComponente(usb);

        Assert.Equal(50m, oficina.CalcularPrecio());
        Assert.Equal(60m, teletrabajo.CalcularPrecio());
        Assert.Equal(1.00m, teletrabajo.Capturar().PesoKg);
        Assert.Equal(new[] { "P01", "P02", "P03" }, teletrabajo.Expandir().Keys.Select(s => s.Valor).Order());
        Assert.All(teletrabajo.Expandir(6).Values, cantidad => Assert.Equal(6, cantidad));
        Assert.Equal(2, teletrabajo.Componentes.Count);
    }

    [Fact]
    public void ExpandirPropagaMultiplicadoresYAcumulaSkuRepetido()
    {
        var mouse = CrearProducto("P01", 20m, 0.20m);
        var hijo = new KitCompuesto(new Sku("KIT_A"), "A", mouse, 2);
        var padre = new KitCompuesto(new Sku("KIT_B"), "B", hijo, 3);
        padre.AgregarComponente(mouse, 4);

        Assert.Equal(200m, padre.CalcularPrecio());
        var cantidades = padre.Expandir(5);
        Assert.Equal(50, Assert.Single(cantidades).Value);
    }

    [Fact]
    public void KitRechazaCiclosDirectosEIndirectos()
    {
        var mouse = CrearProducto("P01", 20m, 0.20m);
        var a = new KitCompuesto(new Sku("KIT_A"), "A", mouse);
        var b = new KitCompuesto(new Sku("KIT_B"), "B", a);
        var c = new KitCompuesto(new Sku("KIT_C"), "C", b);

        Assert.Throws<InvalidOperationException>(() => a.AgregarComponente(a));
        Assert.Throws<InvalidOperationException>(() => a.AgregarComponente(c));
        Assert.Equal(20m, c.CalcularPrecio());
        Assert.Single(a.Componentes);
    }

    [Fact]
    public void KitProtegeCantidadDuplicadosYUltimoComponente()
    {
        var mouse = CrearProducto("P01", 20m, 0.20m);
        Assert.Throws<ArgumentOutOfRangeException>(() => new KitCompuesto(new Sku("KIT_A"), "A", mouse, 0));
        var kit = new KitCompuesto(new Sku("KIT_A"), "A", mouse);
        Assert.Throws<InvalidOperationException>(() => kit.AgregarComponente(mouse));
        Assert.Throws<ArgumentOutOfRangeException>(() => kit.CambiarCantidad(mouse.Sku, 0));
        Assert.Throws<InvalidOperationException>(() => kit.QuitarComponente(mouse.Sku));
        Assert.Equal(1, Assert.Single(kit.Componentes).Cantidad);
    }

    private static ProductoSimple CrearProducto(string sku, decimal precio, decimal peso) =>
        new(new Sku(sku), sku, precio, peso, 8);
}
