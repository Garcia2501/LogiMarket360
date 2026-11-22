using LogiMarket.Application.Documentos;
using LogiMarket.Application.Escritorio;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Navegacion;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal sealed partial class VentanaComercial
{
    private Pantalla ConstruirReportes()
    {
        var vista = new Pantalla("Reportes y comprobantes", "Consulta ventas y devoluciones por fecha y emite comprobantes internos.");
        var desde = new DateTimePicker { Width = 117, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30),
            AccessibleName = "Fecha inicial" };
        var hasta = new DateTimePicker { Width = 117, Format = DateTimePickerFormat.Short, Value = DateTime.Today,
            AccessibleName = "Fecha final" };
        var filtro = Tema.TextoCampo("Filtro de pedidos", 272);
        filtro.PlaceholderText = "ESTADO = PAGADO AND TOTAL >= 50";
        var cobros = new Tarjeta("Cobros del período");
        var neto = new Tarjeta("Ventas netas");
        var devoluciones = new Tarjeta("Devoluciones");
        var tabla = TablaPedidos("Pedidos del reporte");
        var tipo = Tema.Lista<TipoComprobante>("Tipo de comprobante");
        string? filtroVigente = null;
        async Task Cargar(IServiceProvider sp, bool desdeEdicion)
        {
            if (hasta.Value.Date < desde.Value.Date)
                throw new ArgumentException("La fecha final no puede ser anterior a la inicial.");
            var dominio = sp.GetRequiredService<ConsultarEscritorio>();
            var resultado = await dominio.PeriodoAsync(desde.Value.Date.ToUniversalTime(),
                hasta.Value.Date.AddDays(1).ToUniversalTime(), desdeEdicion ? filtro.Text : filtroVigente);
            if (desdeEdicion)
                filtroVigente = filtro.Text;
            cobros.Mostrar(resultado.Cobros);
            neto.Mostrar(Tema.Usd(resultado.VentasNetas));
            devoluciones.Mostrar(resultado.Devoluciones);
            tabla.Cargar(FilasPedidos(resultado.Filas), _pedidoActivo);
        }
        tabla.AlSeleccionar(fila => { if (fila is not null) Elegir(fila.Id); });
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Desde", desde), Tema.Campo("Hasta", hasta),
            Tema.Campo("Expresión de filtro", filtro),
            Accion(vista, "Consultar", async sp =>
            {
                try
                {
                    await Cargar(sp, true);
                    _errores.SetError(filtro, "");
                    return "Resultados del período actualizados.";
                }
                catch (LogiMarket.Application.Reportes.SintaxisFiltroException e)
                {
                    _errores.SetError(filtro, e.Message);
                    filtro.Focus();
                    throw;
                }
            }, principal: true)));
        Tema.Fila(vista.Cuerpo, Tema.Acciones(cobros.Contenedor, neto.Contenedor, devoluciones.Contenedor));
        Tema.Fila(vista.Cuerpo, tabla.Control, 100);
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Tipo de comprobante", tipo), Accion(vista, "Vista previa", async sp =>
        {
            var fila = Elegida(tabla);
            await sp.GetRequiredService<EmitirComprobantes>().EjecutarAsync(fila.Id, (TipoComprobante)tipo.SelectedItem!,
                new VistaPrevia(MostrarDocumentoAsync));
            return null;
        }), Accion(vista, "Guardar TXT", async sp =>
        {
            var pedido = Elegida(tabla);
            var clase = (TipoComprobante)tipo.SelectedItem!;
            using var guardar = new SaveFileDialog { Filter = "Texto (*.txt)|*.txt", DefaultExt = "txt",
                AddExtension = true, OverwritePrompt = true, FileName = $"{clase}-{pedido.Id:N}.txt" };
            if (guardar.ShowDialog(this) != DialogResult.OK)
                return null;
            await sp.GetRequiredService<EmitirComprobantes>().EjecutarAsync(pedido.Id, clase,
                sp.GetRequiredService<IFabricaArchivoTxt>().Crear(guardar.FileName));
            return $"Comprobante guardado: {Path.GetFileName(guardar.FileName)}.";
        })));
        vista.Cargar = sp => Cargar(sp, false);
        return vista;
    }

    private Task MostrarDocumentoAsync(DocumentoInterno documento, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var visor = new Form { Text = documento.Titulo, ClientSize = new Size(720, 405),
            StartPosition = FormStartPosition.CenterParent, BackColor = Tema.Papel, Font = Font };
        var cuerpo = Tema.Pilas();
        cuerpo.Padding = new Padding(19);
        Tema.Fila(cuerpo, new Label { Text = documento.Titulo, AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold) });
        Tema.Fila(cuerpo, new TextBox { Text = documento.Contenido, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical, BackColor = Tema.Papel, AccessibleName = documento.Titulo }, 100);
        var cerrar = Tema.Boton("Cerrar");
        cerrar.DialogResult = DialogResult.Cancel;
        visor.CancelButton = cerrar;
        Tema.Fila(cuerpo, Tema.Acciones(cerrar));
        visor.Controls.Add(cuerpo);
        visor.ShowDialog(this);
        return Task.CompletedTask;
    }

    private Pantalla ConstruirAuditoria()
    {
        var vista = new Pantalla("Auditoría y demostración", "Consulta operaciones, métricas de sesión y resultados del procesador Outbox.");
        var ventas = new Tarjeta("Ventas de sesión");
        var despachos = new Tarjeta("Despachos de sesión");
        var devueltos = new Tarjeta("Devoluciones de sesión");
        var pendientes = new Tarjeta("Outbox pendientes");
        var filtro = Tema.TextoCampo("Categoría de auditoría", 167);
        var auditoria = new Cuadricula<AuditoriaVista>("Registro de auditoría", a => a.Id)
            .Col("Fecha UTC", nameof(AuditoriaVista.FechaUtc), 98, "dd/MM/yyyy HH:mm").Col("Categoría", nameof(AuditoriaVista.Categoria), 85)
            .Col("Acción", nameof(AuditoriaVista.Accion), 100).Col("Resultado", nameof(AuditoriaVista.Resultado), 150)
            .Col("Correlación", nameof(AuditoriaVista.Correlacion), 150).Col("Referencia", nameof(AuditoriaVista.Referencia), 115);
        var outbox = new Cuadricula<OutboxVisto>("Eventos Outbox", e => e.Id).Col("Tipo", nameof(OutboxVisto.Tipo), 124)
            .Col("Creado UTC", nameof(OutboxVisto.FechaUtc), 108, "dd/MM/yyyy HH:mm").Col("Intentos", nameof(OutboxVisto.Intentos), 60)
            .Col("Procesado UTC", nameof(OutboxVisto.ProcesadoUtc), 108, "dd/MM/yyyy HH:mm")
            .Col("Último error", nameof(OutboxVisto.UltimoError), 120);
        var proveedor = Tema.Lista<ProveedorPago>("Proveedor de demostración", 132);
        var rechazar = new CheckBox { Text = "Rechazar cobro", AutoSize = true, AccessibleName = "Inducir rechazo de cobro" };
        var fallar = new CheckBox { Text = "Fallar avisos de Outbox", AutoSize = true, AccessibleName = "Inducir fallo de Outbox" };
        var canal = Tema.Lista<CanalAviso>("Canal de aviso", 103);
        var demo = _servicios.GetRequiredService<EstadoDemostracion>();
        proveedor.SelectedItem = demo.Ajustes.Proveedor;
        rechazar.Checked = demo.Ajustes.RechazarCobro;
        fallar.Checked = demo.Ajustes.FallarAvisos;
        proveedor.Enabled = rechazar.Enabled = fallar.Enabled = EsDemo && Tiene(Operacion.ConfigurarDemostracion);

        async Task Cargar(IServiceProvider sp)
        {
            var leer = sp.GetRequiredService<ConsultarEscritorio>();
            var indicadores = await leer.IndicadoresAsync();
            ventas.Mostrar(indicadores.Ventas);
            despachos.Mostrar(indicadores.Despachos);
            devueltos.Mostrar(indicadores.Devoluciones);
            pendientes.Mostrar(indicadores.Pendientes);
            auditoria.Cargar(await leer.AuditoriaAsync(filtro.Text));
            outbox.Cargar(await leer.OutboxAsync());
        }
        var aplicar = Accion(vista, "Aplicar escenario", async sp =>
        {
            var seleccionado = (ProveedorPago)proveedor.SelectedItem!;
            await sp.GetRequiredService<ControlDemostracion>().AplicarAsync(new(seleccionado, rechazar.Checked, fallar.Checked));
            return $"Escenario activo · {seleccionado} · cobro {(rechazar.Checked ? "rechazado" : "normal")} · avisos {(fallar.Checked ? "en fallo" : "normales")}.";
        }, Operacion.ConfigurarDemostracion);
        aplicar.Enabled = proveedor.Enabled;
        Tema.Fila(vista.Cuerpo, Tema.Acciones(ventas.Contenedor, despachos.Contenedor, devueltos.Contenedor, pendientes.Contenedor));
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Categoría", filtro),
            Accion(vista, "Filtrar auditoría", async sp => { await Cargar(sp); return "Auditoría actualizada."; }, Operacion.ConsultarAuditoria)));
        Tema.Fila(vista.Cuerpo, auditoria.Control, 54);
        Tema.Fila(vista.Cuerpo, Tema.TextoPlano("Eventos Outbox · intentos y resultado"));
        Tema.Fila(vista.Cuerpo, outbox.Control, 46);
        Tema.Fila(vista.Cuerpo, Tema.TextoPlano(EsDemo ? "Sesión de demostración en base aislada."
            : "Los escenarios de fallo solo están disponibles al abrir la aplicación con --demo."));
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Proveedor", proveedor), rechazar, fallar, aplicar,
            Tema.Campo("Canal", canal), Accion(vista, "Reprocesar pendientes", async sp =>
            {
                var resultado = await sp.GetRequiredService<ReprocesarEventos>().EjecutarAsync((CanalAviso)canal.SelectedItem!);
                return await NotificarAsync(CambioComercial.EventosProcesados,
                    $"Procesados: {resultado.Procesados} · fallidos: {resultado.Fallidos}.");
            }, Operacion.ReprocesarEventos, principal: true)));
        vista.Cargar = Cargar;
        return vista;
    }
}
