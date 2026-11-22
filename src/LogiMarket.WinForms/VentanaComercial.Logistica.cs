using LogiMarket.Application.Comandos;
using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Escritorio;
using LogiMarket.Application.Navegacion;
using LogiMarket.Application.Reportes;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Pedidos;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal sealed partial class VentanaComercial
{
    partial void RegistrarSeguimiento()
    {
        Registrar(Seccion.Despacho, "Despacho y entrega", ConstruirDespacho);
        Registrar(Seccion.Devoluciones, "Devoluciones", ConstruirDevoluciones);
        Registrar(Seccion.Reportes, "Reportes y comprobantes", ConstruirReportes);
        Registrar(Seccion.Auditoria, "Auditoría y demostración", ConstruirAuditoria, Operacion.ConsultarAuditoria);
    }

    private Pantalla ConstruirDespacho()
    {
        var vista = new Pantalla("Despacho y entrega", "Selecciona un pedido pagado, genera su guía y registra su entrega.");
        var tabla = TablaPedidos("Pedidos para despacho y entrega");
        var detalle = Tema.TextoPlano();
        Button? despachar = null;
        Button? entregar = null;

        async Task Mostrar(IServiceProvider sp, Guid id)
        {
            Elegir(id);
            var pedido = await sp.GetRequiredService<DespacharPedidos>().ConsultarAsync(id);
            detalle.Text = pedido.Despacho is { } guia
                ? $"Guía {guia.Guia}   ·   Plazo {guia.PlazoDias} día(s)   ·   Despacho {Tema.Fecha(guia.FechaDespachoUtc)}" +
                  (guia.FechaEntregaUtc is DateTime fecha ? $"   ·   Entrega {Tema.Fecha(fecha)}" : "")
                : "Guía pendiente de emisión.";
            despachar!.Enabled = Tiene(Operacion.Despachar) && pedido.Estado == EstadoPedido.PAGADO;
            entregar!.Enabled = Tiene(Operacion.Entregar) && pedido.Estado == EstadoPedido.DESPACHADO;
        }

        async Task Cargar(IServiceProvider sp)
        {
            var filas = await sp.GetRequiredService<ConsultarReportePedidos>().EjecutarAsync(null);
            tabla.Cargar(FilasPedidos(filas.Where(p => p.Estado is EstadoPedido.PAGADO or EstadoPedido.DESPACHADO or EstadoPedido.ENTREGADO)),
                _pedidoActivo);
            if (tabla.Seleccion is { } fila)
                await Mostrar(sp, fila.Id);
            else
            {
                detalle.Text = "Selecciona un pedido pagado, despachado o entregado.";
                despachar!.Enabled = entregar!.Enabled = false;
            }
        }
        tabla.AlSeleccionar(fila =>
        {
            if (fila is not null)
                _ = EjecutarAsync(vista, async sp => { await Mostrar(sp, fila.Id); return null; });
        });
        despachar = Accion(vista, "Marcar como despachado", async sp =>
        {
            var pedido = Elegida(tabla);
            if (!Confirmar($"¿Emitir la guía y despachar el pedido de {pedido.Cliente}?"))
                return null;
            Elegir(pedido.Id);
            var resultado = await sp.GetRequiredService<OperacionesPedido>().DespacharAsync(new DespacharPedidoCommand(pedido.Id));
            return await NotificarAsync(CambioComercial.EstadoPedido, $"Despacho registrado · {resultado.Guia}.");
        }, Operacion.Despachar, principal: true);
        entregar = Accion(vista, "Registrar entrega", async sp =>
        {
            var pedido = Elegida(tabla);
            if (!Confirmar($"¿Confirmar la entrega del pedido de {pedido.Cliente}?"))
                return null;
            Elegir(pedido.Id);
            await sp.GetRequiredService<DespacharPedidos>().EntregarAsync(pedido.Id);
            return await NotificarAsync(CambioComercial.EstadoPedido, "Entrega registrada.");
        }, Operacion.Entregar);
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Accion(vista, "Actualizar", async sp => { await Cargar(sp); return null; }),
            despachar, entregar));
        Tema.Fila(vista.Cuerpo, tabla.Control, 100);
        Tema.Fila(vista.Cuerpo, Tema.TextoPlano("Seguimiento"));
        Tema.Fila(vista.Cuerpo, detalle);
        vista.Cargar = Cargar;
        return vista;
    }

    private Pantalla ConstruirDevoluciones()
    {
        var vista = new Pantalla("Devoluciones y reintegros", "Comprueba la elegibilidad antes de reembolsar y reintegrar el inventario.");
        var tabla = TablaPedidos("Pedidos entregados");
        var motivo = Tema.Lista<MotivoDevolucion>("Motivo de devolución", 180);
        var sinUso = new CheckBox { Text = "Producto sin uso", AccessibleName = "Producto sin uso", AutoSize = true };
        var entrega = Tema.TextoPlano();
        var resultado = new Tarjeta("Reembolso sin flete");
        var previos = new Dictionary<Guid, decimal?>();
        Button? previsualizar = null;
        Button? devolver = null;

        async Task Mostrar(IServiceProvider sp, Guid id)
        {
            Elegir(id);
            var detalle = await sp.GetRequiredService<ConsultarEscritorio>().DetalleAsync(id);
            var yaDevuelto = previos.GetValueOrDefault(id);
            entrega.Text = detalle.Despacho?.FechaEntregaUtc is { } fecha ? $"Entrega registrada: {Tema.Fecha(fecha)}" : "Entrega sin fecha registrada.";
            resultado.Mostrar(yaDevuelto is decimal monto ? Tema.Usd(monto) : "—");
            previsualizar!.Enabled = devolver!.Enabled = Tiene(Operacion.Devolver) && yaDevuelto is null;
            if (yaDevuelto is not null)
                entrega.Text += " · devolución registrada";
        }

        async Task Cargar(IServiceProvider sp)
        {
            var filas = await sp.GetRequiredService<ConsultarReportePedidos>().EjecutarAsync("ESTADO = ENTREGADO");
            previos.Clear();
            foreach (var p in filas)
                previos.Add(p.Id, p.ImporteReembolso);
            tabla.Cargar(FilasPedidos(filas), _pedidoActivo);
            if (tabla.Seleccion is { } fila)
                await Mostrar(sp, fila.Id);
            else
            {
                entrega.Text = "Selecciona un pedido entregado.";
                resultado.Mostrar("—");
                previsualizar!.Enabled = devolver!.Enabled = false;
            }
        }
        tabla.AlSeleccionar(p =>
        {
            if (p is not null)
                _ = EjecutarAsync(vista, async sp => { await Mostrar(sp, p.Id); return null; });
        });

        previsualizar = Accion(vista, "Validar política", async sp =>
        {
            var fila = Elegida(tabla);
            resultado.Mostrar("—");
            var datos = await sp.GetRequiredService<AdministrarDevoluciones>().PrevisualizarAsync(fila.Id,
                (MotivoDevolucion)motivo.SelectedItem!, sinUso.Checked);
            resultado.Mostrar(Tema.Usd(datos.Importe));
            return "Política aprobada. El importe excluye el envío.";
        }, Operacion.Devolver);
        devolver = Accion(vista, "Confirmar devolución", async sp =>
        {
            var fila = Elegida(tabla);
            var politica = (MotivoDevolucion)motivo.SelectedItem!;
            resultado.Mostrar("—");
            var datos = await sp.GetRequiredService<AdministrarDevoluciones>().PrevisualizarAsync(fila.Id, politica, sinUso.Checked);
            resultado.Mostrar(Tema.Usd(datos.Importe));
            if (!Confirmar($"¿Registrar la devolución total de {fila.Cliente} y el reembolso simulado de {Tema.Usd(datos.Importe)}?"))
                return null;
            Elegir(fila.Id);
            var aplicada = await sp.GetRequiredService<OperacionesPedido>()
                .DevolverAsync(new ProcesarDevolucionCommand(fila.Id, politica, sinUso.Checked));
            return await NotificarAsync(CambioComercial.DevolucionAceptada, $"Devolución registrada · {Tema.Usd(aplicada.Importe)}.");
        }, Operacion.Devolver, principal: true);
        motivo.SelectionChangeCommitted += (_, _) => resultado.Mostrar("—");
        sinUso.CheckedChanged += (_, _) => resultado.Mostrar("—");
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Motivo", motivo), sinUso, previsualizar, devolver));
        Tema.Fila(vista.Cuerpo, tabla.Control, 100);
        Tema.Fila(vista.Cuerpo, entrega);
        Tema.Fila(vista.Cuerpo, Tema.TextoPlano("DEFECTO · hasta 30 días   |   ARREPENTIMIENTO · hasta 7 días y producto sin uso"));
        Tema.Fila(vista.Cuerpo, Tema.Acciones(resultado.Contenedor));
        vista.Cargar = Cargar;
        return vista;
    }
}
