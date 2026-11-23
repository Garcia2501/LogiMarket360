using LogiMarket.Application.Escritorio;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal sealed partial class VentanaComercial
{
    private Pantalla ConstruirPanel()
    {
        var vista = new Pantalla("Panel general", "Actividad comercial, existencias y eventos de la sesión.");
        var activos = new Tarjeta("Pedidos activos");
        var despacho = new Tarjeta("Por despachar");
        var stock = new Tarjeta("Stock bajo");
        var ventas = new Tarjeta("Ventas brutas");
        var eventos = new Tarjeta("Ventas de sesión");
        var alertas = Tema.TextoPlano();
        var tabla = TablaPedidos("Pedidos recientes");
        tabla.Control.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0 && tabla.Seleccion is { } fila && _definiciones.ContainsKey(Seccion.Pedidos))
            {
                Elegir(fila.Id);
                await NavegarAsync(Seccion.Pedidos);
            }
        };
        Tema.Fila(vista.Cuerpo, Tema.Acciones(activos.Contenedor, despacho.Contenedor, stock.Contenedor,
            ventas.Contenedor, eventos.Contenedor));
        Tema.Fila(vista.Cuerpo, alertas);
        Tema.Fila(vista.Cuerpo, Tema.TextoPlano("Pedidos recientes"));
        Tema.Fila(vista.Cuerpo, tabla.Control, 100);
        vista.Cargar = async sp =>
        {
            var resumen = await sp.GetRequiredService<ConsultarEscritorio>().ResumenAsync();
            activos.Mostrar(resumen.Activos);
            despacho.Mostrar(resumen.PorDespachar);
            stock.Mostrar(resumen.StockBajo);
            ventas.Mostrar(Tema.Usd(resumen.Ventas));
            eventos.Mostrar(resumen.Sesion.Ventas);
            alertas.Text = $"Outbox pendientes: {resumen.Sesion.Pendientes}   ·   Productos con stock de 3 unidades o menos: {resumen.StockBajo}";
            tabla.Cargar(FilasPedidos(resumen.Recientes), _pedidoActivo);
        };
        return vista;
    }

    private sealed record PedidoFila(Guid Id, string Cliente, string Estado, string Fecha, string Total, string Entrega);

    private static Cuadricula<PedidoFila> TablaPedidos(string nombre) => new Cuadricula<PedidoFila>(nombre, x => x.Id)
        .Col("Pedido", nameof(PedidoFila.Id), 185, minimo: 114).Col("Cliente", nameof(PedidoFila.Cliente), 110, minimo: 110)
        .Col("Estado", nameof(PedidoFila.Estado), 80, minimo: 90).Col("Creado", nameof(PedidoFila.Fecha), 95, minimo: 125)
        .Col("Cobrado", nameof(PedidoFila.Total), 76, minimo: 94).Col("Entrega", nameof(PedidoFila.Entrega), 78, minimo: 85);

    private static IEnumerable<PedidoFila> FilasPedidos(IEnumerable<LogiMarket.Application.Reportes.PedidoReportado> pedidos) =>
        pedidos.Select(p => new PedidoFila(p.Id, p.Cliente, p.Estado.ToString(), Tema.Fecha(p.FechaCreacionUtc),
            p.TotalCobrado is decimal total ? Tema.Usd(total) : "", p.Entrega.ToString()));
}
