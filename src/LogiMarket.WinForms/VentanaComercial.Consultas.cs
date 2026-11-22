using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Documentos;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Navegacion;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Reportes;
using LogiMarket.Application.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal sealed partial class VentanaComercial
{
    private readonly FormularioPrincipalMediator _mediador = new();
    private readonly DataGridView _tablaCatalogo = Tabla("Catálogo");
    private readonly DataGridView _tablaPedidos = Tabla("Pedidos");
    private readonly Label _resumenCarrito = new() { AutoSize = true, AccessibleName = "Resumen del carrito" };
    private readonly Label _resumenInventario = new() { AutoSize = true, AccessibleName = "Existencias de componentes" };
    private readonly Label _resumenMetricas = new() { AutoSize = true, AccessibleName = "Métricas de ventas" };
    private readonly TextBox _skuLogistica = new() { Width = 165, Text = "KIT_TELETRABAJO", AccessibleName = "SKU de logística" };
    private readonly TextBox _filtroPedidos = new() { Width = 310, PlaceholderText = "ESTADO = PAGADO AND TOTAL >= 50",
        AccessibleName = "Filtro de pedidos" };
    private readonly ComboBox _tipoDocumento = Seleccion<TipoComprobante>("Tipo de comprobante");
    private string? _filtroActivo;

    private void ConfigurarConsultas()
    {
        _mediador.Conectar(AreaComercial.Catalogo, (_, ct) => ConsultarVistaAsync(async (sp, token) =>
            _tablaCatalogo.DataSource = (await sp.GetRequiredService<ConsultarCatalogoVisual>().ListarAsync(token)).ToArray(), ct));
        _mediador.Conectar(AreaComercial.Carrito, (id, ct) => ConsultarVistaAsync((sp, token) => RefrescarCarritoAsync(sp, id, token), ct));
        _mediador.Conectar(AreaComercial.Pedidos, (_, ct) => ConsultarVistaAsync(async (sp, token) =>
            _tablaPedidos.DataSource = (await sp.GetRequiredService<ConsultarReportePedidos>().EjecutarAsync(_filtroActivo, token)).ToArray(), ct));
        _mediador.Conectar(AreaComercial.Inventario, (_, ct) => ConsultarVistaAsync(RefrescarInventarioAsync, ct));
        _mediador.Conectar(AreaComercial.Metricas, (_, ct) => ConsultarVistaAsync(async (sp, token) =>
        {
            var pedidos = await sp.GetRequiredService<ConsultarReportePedidos>().EjecutarAsync(null, token);
            var emitidos = sp.GetRequiredService<MetricasSesion>().Contar(EventoComercial.PedidoPagado);
            _resumenMetricas.Text = $"Pedidos con cobro: {pedidos.Count(p => p.TotalCobrado is not null)} · Eventos de venta procesados: {emitidos}";
        }, ct));

        var pestanas = new TabControl { Width = 900, Height = 235, AccessibleName = "Consultas comerciales" };
        pestanas.TabPages.Add(ConstruirCatalogo());
        pestanas.TabPages.Add(ConstruirPedidos());
        _contenido.Controls.Add(pestanas);
        _contenido.Controls.Add(Fila(_resumenCarrito));
        _contenido.Controls.Add(Fila(_resumenInventario));
        _contenido.Controls.Add(Fila(_resumenMetricas));
    }

    private TabPage ConstruirCatalogo()
    {
        Columna(_tablaCatalogo, "SKU", nameof(RenglonCatalogoVisual.Sku));
        Columna(_tablaCatalogo, "Producto", nameof(RenglonCatalogoVisual.Nombre));
        Columna(_tablaCatalogo, "Stock", nameof(RenglonCatalogoVisual.Stock));
        Columna(_tablaCatalogo, "Cantidad", nameof(RenglonCatalogoVisual.Cantidad));
        Columna(_tablaCatalogo, "Precio USD", nameof(RenglonCatalogoVisual.Precio), "F2");
        var panel = Fila(Etiqueta("SKU logístico"), _skuLogistica,
            Boton("Desglose y peso", Operacion.Consultar, async sp =>
            {
                var catalogo = sp.GetRequiredService<AdministrarCatalogo>();
                await RefrescarInventarioAsync(sp, CancellationToken.None);
                var informe = await catalogo.ConsultarLogisticaAsync(_skuLogistica.Text, 1);
                return $"Peso {informe.PesoKg:F2} kg · {string.Join(", ", informe.Componentes.Select(c => $"{c.Sku} × {c.Cantidad}"))}";
            }),
            Boton("Medir 1.000 renglones", Operacion.Consultar, async sp =>
            {
                var demo = await sp.GetRequiredService<ConsultarCatalogoVisual>().DemostrarAsync();
                _tablaCatalogo.DataSource = demo.Renglones.ToArray();
                var m = demo.Medicion;
                return $"{m.Renglones} renglones · Metadatos {m.ObjetosCompartidos}/{m.ObjetosIndependientes} · " +
                    $"Bytes asignados {m.BytesCompartidos:N0}/{m.BytesIndependientes:N0} (compartidos/independientes).";
            }));
        return Pagina("Catálogo", _tablaCatalogo, panel);
    }

    private TabPage ConstruirPedidos()
    {
        Columna(_tablaPedidos, "Pedido", nameof(PedidoReportado.Id));
        Columna(_tablaPedidos, "Cliente", nameof(PedidoReportado.Cliente));
        Columna(_tablaPedidos, "Estado", nameof(PedidoReportado.Estado));
        Columna(_tablaPedidos, "Cobrado USD", nameof(PedidoReportado.TotalCobrado), "F2");
        _tablaPedidos.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && _tablaPedidos.Rows[e.RowIndex].DataBoundItem is PedidoReportado pedido)
                _pedido.Text = pedido.Id.ToString("D");
        };

        var panel = Fila(_filtroPedidos, Boton("Filtrar", Operacion.Consultar, async sp =>
        {
            var filas = await sp.GetRequiredService<ConsultarReportePedidos>().EjecutarAsync(_filtroPedidos.Text);
            _filtroActivo = _filtroPedidos.Text;
            _tablaPedidos.DataSource = filas.ToArray();
            return $"{filas.Count} pedido(s). Doble clic para seleccionar.";
        }), _tipoDocumento, Boton("Vista previa", Operacion.Consultar, MostrarComprobanteAsync),
            Boton("Guardar TXT", Operacion.Consultar, GuardarComprobanteAsync));
        return Pagina("Pedidos y comprobantes", _tablaPedidos, panel);
    }

    private async Task RefrescarCarritoAsync(IServiceProvider sp, Guid? id, CancellationToken ct)
    {
        _renglonesBorrador.DataSource = null;
        _resumenCarrito.Text = "Carrito: sin borrador editable.";
        if (id is null)
            return;

        try
        {
            var vista = await sp.GetRequiredService<AdministrarPedidos>().ConsultarBorradorAsync(id.Value, ct);
            _renglonesBorrador.DataSource = vista.Lineas.ToArray();
            _resumenCarrito.Text = $"Carrito {vista.Estado} · {vista.Lineas.Count} línea(s) · " +
                $"Subtotal USD {vista.Valoracion.Subtotal:F2} · Total USD {vista.Valoracion.Total:F2}";
        }
        catch (RechazoPedidoException e) when (e.Codigo == "ESTADO") { }
    }

    private async Task RefrescarInventarioAsync(IServiceProvider sp, CancellationToken ct)
    {
        var filas = await sp.GetRequiredService<AdministrarCatalogo>().ConsultarExistenciasAsync(_skuLogistica.Text, 1, ct);
        _resumenInventario.Text = "Inventario · " + string.Join(" · ", filas.Select(f => $"{f.Sku}: {f.Disponibles}"));
    }

    private async Task ConsultarVistaAsync(Func<IServiceProvider, CancellationToken, Task> consultar, CancellationToken ct)
    {
        await using var scope = _servicios.CreateAsyncScope();
        await consultar(scope.ServiceProvider, ct);
    }

    private async Task<string> RefrescarAsync(CambioComercial cambio, Guid? pedidoId, string mensaje)
    {
        var errores = await _mediador.NotificarAsync(cambio, pedidoId);
        return errores.Count == 0 ? mensaje : $"{mensaje} No se pudo refrescar: {string.Join(", ", errores.Select(e => e.Area))}.";
    }

    private void LimpiarConsultas()
    {
        _tablaCatalogo.DataSource = null;
        _tablaPedidos.DataSource = null;
        _renglonesBorrador.DataSource = null;
        _resumenCarrito.Text = _resumenInventario.Text = _resumenMetricas.Text = "";
        _filtroActivo = null;
        _filtroPedidos.Clear();
    }

    private async Task<string> MostrarComprobanteAsync(IServiceProvider sp)
    {
        await sp.GetRequiredService<EmitirComprobantes>().EjecutarAsync(IdPedido(),
            (TipoComprobante)_tipoDocumento.SelectedItem!, new VistaPrevia((documento, _) =>
            {
                using var visor = new Form { Text = documento.Titulo, ClientSize = new Size(630, 365),
                    StartPosition = FormStartPosition.CenterParent };
                visor.Controls.Add(new TextBox
                {
                    Text = documento.Contenido, Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical, AccessibleName = documento.Titulo
                });
                visor.ShowDialog(this);
                return Task.CompletedTask;
            }));
        return "Vista previa cerrada.";
    }

    private async Task<string> GuardarComprobanteAsync(IServiceProvider sp)
    {
        var id = IdPedido();
        var tipo = (TipoComprobante)_tipoDocumento.SelectedItem!;
        using var dialogo = new SaveFileDialog { Filter = "Texto (*.txt)|*.txt", DefaultExt = "txt",
            FileName = $"{tipo}-{id:N}.txt", AddExtension = true, OverwritePrompt = true };
        if (dialogo.ShowDialog(this) != DialogResult.OK)
            return "Guardado cancelado.";
        var salida = sp.GetRequiredService<IFabricaArchivoTxt>().Crear(dialogo.FileName);
        await sp.GetRequiredService<EmitirComprobantes>().EjecutarAsync(id, tipo, salida);
        return $"Comprobante guardado: {dialogo.FileName}";
    }

    private static DataGridView Tabla(string nombre) => new()
    {
        Dock = DockStyle.Fill, AccessibleName = nombre, ReadOnly = true, AutoGenerateColumns = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    private static void Columna(DataGridView tabla, string titulo, string propiedad, string formato = "") =>
        tabla.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = titulo, DataPropertyName = propiedad,
            DefaultCellStyle = new DataGridViewCellStyle { Format = formato } });

    private static TabPage Pagina(string titulo, DataGridView tabla, FlowLayoutPanel acciones)
    {
        var pagina = new TabPage(titulo);
        acciones.Dock = DockStyle.Top;
        pagina.Controls.Add(tabla);
        pagina.Controls.Add(acciones);
        return pagina;
    }
}
