using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Clientes;
using LogiMarket.Application.Comandos;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Escritorio;
using LogiMarket.Application.Navegacion;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Reportes;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Pedidos;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal sealed partial class VentanaComercial
{
    partial void RegistrarPedidos()
    {
        Registrar(Seccion.Carrito, "Carrito y borradores", ConstruirCarrito);
        Registrar(Seccion.Pedidos, "Pedidos e historial", ConstruirPedidos);
        Registrar(Seccion.Cobro, "Confirmación y cobro", ConstruirCobro);
    }

    private Pantalla ConstruirCarrito()
    {
        var vista = new Pantalla("Carrito y borradores", "Selecciona un pedido existente o crea un borrador con los productos y kits activos.");
        var selector = new ComboBox { Width = 286, DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = "Pedido del carrito", DisplayMember = nameof(OpcionPedido.Texto) };
        var clientes = new ComboBox { Width = 199, DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = "Cliente del pedido", DisplayMember = nameof(ClienteVisto.Nombre) };
        var entrega = Tema.Lista<ModalidadEntrega>("Modalidad de entrega", 130);
        var articulos = new ComboBox { Width = 244, DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = "Producto o kit", DisplayMember = nameof(OpcionArticulo.Texto) };
        var cantidad = Tema.Numero("Cantidad de la línea", 1, 1, int.MaxValue);
        var aviso = Tema.TextoPlano();
        var tabla = new Cuadricula<LineaFila>("Líneas del carrito", l => l.Linea.Id).Col("SKU", nameof(LineaFila.Sku), 90)
            .Col("Artículo", nameof(LineaFila.Nombre), 160).Col("Cantidad", nameof(LineaFila.Cantidad), 66)
            .Col("Unitario USD", nameof(LineaFila.Unitario), 81, "N2").Col("Importe USD", nameof(LineaFila.Importe), 80, "N2");
        var subtotal = new Tarjeta("Subtotal");
        var descuento = new Tarjeta("Descuento");
        var envio = new Tarjeta("Envío");
        var total = new Tarjeta("Total");
        var ediciones = new List<Button>();
        Button? validar = null;
        LineaSolicitada LineaElegida() => articulos.SelectedItem is OpcionArticulo articulo
            ? new(articulo.Sku, (int)cantidad.Value) : throw new InvalidOperationException("Selecciona un producto o kit.");

        async Task Cargar(IServiceProvider sp)
        {
            var repo = sp.GetRequiredService<ConsultarReportePedidos>();
            var pedidos = await repo.EjecutarAsync(null);
            CargarSelector(selector, pedidos.Select(p => new OpcionPedido(p.Id, $"{p.Cliente} · {p.Estado}")), _pedidoActivo, o => o.Id);
            var clienteId = (clientes.SelectedItem as ClienteVisto)?.Id;
            var articuloId = (articulos.SelectedItem as OpcionArticulo)?.Sku;
            CargarSelector(clientes, (await sp.GetRequiredService<AdministrarClientes>().BuscarAsync()).Where(c => c.Activo), clienteId, c => c.Id);
            var catalogo = sp.GetRequiredService<AdministrarCatalogo>();
            var opciones = (await catalogo.BuscarAsync()).Where(p => p.Activo).Select(p => new OpcionArticulo(p.Sku, $"{p.Sku} · {p.Nombre}"))
                .Concat((await sp.GetRequiredService<ConsultarEscritorio>().KitsAsync()).Select(k => new OpcionArticulo(k.Sku, $"{k.Sku} · {k.Nombre}")));
            CargarSelector(articulos, opciones, articuloId, a => a.Sku);

            DetallePedidoVisto? actual = null;
            if (_pedidoActivo is Guid id && pedidos.Any(p => p.Id == id))
                actual = await sp.GetRequiredService<ConsultarEscritorio>().DetalleAsync(id);
            aviso.Text = actual is null ? "Sin pedido cargado." : $"{actual.Cliente} · {actual.Estado} · {actual.Id:D}";
            tabla.Cargar(actual?.Lineas.Select(l => new LineaFila(l)) ?? []);
            subtotal.Mostrar(actual is null ? "—" : Tema.Usd(actual.Valoracion.Subtotal));
            descuento.Mostrar(actual is null ? "—" : Tema.Usd(actual.Valoracion.Descuento));
            envio.Mostrar(actual is null ? "—" : Tema.Usd(actual.Valoracion.Tarifa));
            total.Mostrar(actual is null ? "—" : Tema.Usd(actual.Valoracion.Total));
            if (actual is not null)
                entrega.SelectedItem = actual.Entrega;
            var editable = actual?.Estado == EstadoPedido.BORRADOR;
            foreach (var boton in ediciones)
                boton.Enabled = editable && Tiene(Operacion.EditarPedido);
            validar!.Enabled = editable && Tiene(Operacion.ValidarPedido);
        }

        Button Editar(string etiqueta, Func<AdministrarPedidos, Task> operacion)
        {
            var boton = Accion(vista, etiqueta, async sp =>
            {
                await operacion(sp.GetRequiredService<AdministrarPedidos>());
                return await NotificarAsync(CambioComercial.BorradorEditado, "Borrador actualizado.");
            }, Operacion.EditarPedido);
            ediciones.Add(boton);
            return boton;
        }

        validar = Accion(vista, "Validar pedido", async sp =>
        {
            await sp.GetRequiredService<AdministrarPedidos>().ValidarAsync(PedidoActual());
            return await NotificarAsync(CambioComercial.EstadoPedido, "Pedido validado. Puedes continuar al cobro.");
        }, Operacion.ValidarPedido, principal: true);
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Pedido existente", selector), Accion(vista, "Cargar pedido", async sp =>
        {
            if (selector.SelectedItem is not OpcionPedido pedido)
                throw new InvalidOperationException("Selecciona un pedido del listado.");
            Elegir(pedido.Id);
            await Cargar(sp);
            return "Pedido cargado.";
        }), Tema.Campo("Cliente", clientes), Tema.Campo("Entrega", entrega)));
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Producto o kit", articulos), Tema.Campo("Cantidad", cantidad),
            Accion(vista, "Crear borrador", async sp =>
            {
                if (clientes.SelectedItem is not ClienteVisto cliente)
                    throw new InvalidOperationException("Selecciona un cliente activo.");
                var nuevo = await sp.GetRequiredService<AdministrarPedidos>().CrearAsync(cliente.Id,
                    (ModalidadEntrega)entrega.SelectedItem!, [LineaElegida()]);
                Elegir(nuevo.Id);
                return await NotificarAsync(CambioComercial.BorradorEditado, "Borrador creado.");
            }, Operacion.EditarPedido, principal: true), Editar("Añadir línea", p => p.AgregarLineaAsync(PedidoActual(), LineaElegida()))));
        Tema.Fila(vista.Cuerpo, aviso);
        Tema.Fila(vista.Cuerpo, tabla.Control, 100);
        Tema.Fila(vista.Cuerpo, Tema.Acciones(
            Editar("Cambiar cantidad", p => p.CambiarCantidadAsync(PedidoActual(), Elegida(tabla).Linea.Id, (int)cantidad.Value)),
            Editar("Reemplazar línea y precio", p => p.ReemplazarLineaAsync(PedidoActual(), Elegida(tabla).Linea.Id, LineaElegida())),
            Editar("Quitar línea", p => p.QuitarLineaAsync(PedidoActual(), Elegida(tabla).Linea.Id)),
            Editar("Cambiar entrega", p => p.CambiarEntregaAsync(PedidoActual(), (ModalidadEntrega)entrega.SelectedItem!)),
            Editar("Deshacer", p => p.DeshacerAsync(PedidoActual())),
            Editar("Rehacer", p => p.RehacerAsync(PedidoActual())),
            Editar("Clonar borrador", async p => Elegir((await p.ClonarBorradorAsync(PedidoActual())).Id))));
        Tema.Fila(vista.Cuerpo, Tema.Acciones(subtotal.Contenedor, descuento.Contenedor, envio.Contenedor, total.Contenedor, validar));
        vista.Cargar = Cargar;
        return vista;
    }

    private Pantalla ConstruirPedidos()
    {
        var vista = new Pantalla("Pedidos e historial", "Consulta el estado y la trazabilidad de cada pedido.");
        var buscar = Tema.TextoCampo("Buscar pedido o cliente", 217);
        var filtroEstado = new ComboBox { Width = 151, AccessibleName = "Estado del pedido", DropDownStyle = ComboBoxStyle.DropDownList,
            DataSource = new[] { "Todos" }.Concat(Enum.GetNames<EstadoPedido>()).ToArray() };
        var listado = TablaPedidos("Pedidos");
        var historial = new Cuadricula<CambioEstadoVisto>("Historial del pedido", h => h.Estado)
            .Col("Estado", nameof(CambioEstadoVisto.Estado), 85).Col("Fecha UTC", nameof(CambioEstadoVisto.FechaUtc), 105, "dd/MM/yyyy HH:mm")
            .Col("Actor", nameof(CambioEstadoVisto.UsuarioId), 180);
        var detalle = Tema.TextoPlano();
        Button? cancelar = null;
        async Task MostrarDetalle(IServiceProvider sp, Guid id)
        {
            Elegir(id);
            var dato = await sp.GetRequiredService<ConsultarEscritorio>().DetalleAsync(id);
            historial.Cargar(dato.Historial);
            detalle.Text = $"{dato.Cliente} · {dato.Estado}" +
                (dato.Pago is { } pago ? $" · Pago {pago.Proveedor} · {Tema.Usd(pago.Importe)} · {pago.Referencia}" : "") +
                (dato.Despacho is { } despacho ? $" · Guía {despacho.Guia}" : "");
            cancelar!.Enabled = Tiene(Operacion.CancelarPedido) && dato.Estado is EstadoPedido.BORRADOR or EstadoPedido.VALIDADO;
        }
        async Task Cargar(IServiceProvider sp)
        {
            var estado = (string)filtroEstado.SelectedItem!;
            var filas = await sp.GetRequiredService<ConsultarReportePedidos>().EjecutarAsync(estado == "Todos" ? null : $"ESTADO = {estado}");
            var termino = buscar.Text.Trim();
            listado.Cargar(FilasPedidos(filas.Where(p => p.Id.ToString().Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                p.Cliente.Contains(termino, StringComparison.OrdinalIgnoreCase))), _pedidoActivo);
            if (listado.Seleccion is { } pedido)
                await MostrarDetalle(sp, pedido.Id);
            else
            {
                detalle.Text = "Selecciona un pedido para consultar su historial.";
                historial.Cargar([]);
                cancelar!.Enabled = false;
            }
        }
        listado.AlSeleccionar(pedido =>
        {
            if (pedido is not null)
                _ = EjecutarAsync(vista, async sp => { await MostrarDetalle(sp, pedido.Id); return null; });
        });
        cancelar = Accion(vista, "Cancelar pedido", async sp =>
        {
            var fila = Elegida(listado);
            if (!Confirmar($"¿Cancelar el pedido de {fila.Cliente}? Esta acción no puede deshacerse."))
                return null;
            Elegir(fila.Id);
            await sp.GetRequiredService<OperacionesPedido>().CancelarAsync(new CancelarPedidoCommand(fila.Id));
            return await NotificarAsync(CambioComercial.EstadoPedido, "Pedido cancelado.");
        }, Operacion.CancelarPedido);
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Buscar", buscar), Tema.Campo("Estado", filtroEstado),
            Accion(vista, "Filtrar", async sp => { await Cargar(sp); return "Pedidos actualizados."; }, principal: true), cancelar));
        Tema.Fila(vista.Cuerpo, listado.Control, 58);
        Tema.Fila(vista.Cuerpo, Tema.TextoPlano("Historial y referencias"));
        detalle.MaximumSize = new Size(750, 0);
        Tema.Fila(vista.Cuerpo, detalle);
        Tema.Fila(vista.Cuerpo, historial.Control, 42);
        vista.Cargar = Cargar;
        return vista;
    }

    private Pantalla ConstruirCobro()
    {
        var vista = new Pantalla("Confirmación y cobro", "Revisa los importes y confirma el cobro con la pasarela simulada.");
        var lista = TablaPedidos("Pedidos para cobro");
        var proveedor = Tema.Lista<ProveedorPago>("Proveedor simulado");
        var referencia = Tema.TextoPlano();
        referencia.MaximumSize = new Size(760, 0);
        var subtotal = new Tarjeta("Subtotal");
        var descuento = new Tarjeta("Descuento");
        var envio = new Tarjeta("Envío");
        var total = new Tarjeta("Total del pedido");
        var claves = new Dictionary<Guid, string>();
        Button? confirmar = null;
        async Task MostrarDetalle(IServiceProvider sp, Guid id)
        {
            Elegir(id);
            var detalle = await sp.GetRequiredService<ConsultarEscritorio>().DetalleAsync(id);
            subtotal.Mostrar(Tema.Usd(detalle.Valoracion.Subtotal));
            descuento.Mostrar(Tema.Usd(detalle.Valoracion.Descuento));
            envio.Mostrar(Tema.Usd(detalle.Valoracion.Tarifa));
            total.Mostrar(Tema.Usd(detalle.Valoracion.Total));
            referencia.Text = detalle.Pago is { } pago ? $"Aprobado · {pago.Proveedor} · {pago.Referencia}" :
                $"{detalle.Cliente} · {detalle.Estado} · pendiente de cobro";
            confirmar!.Enabled = Tiene(Operacion.Cobrar) && detalle.Estado == EstadoPedido.VALIDADO;
        }
        async Task Cargar(IServiceProvider sp)
        {
            var demo = sp.GetRequiredService<EstadoDemostracion>();
            if (demo.Habilitado)
                proveedor.SelectedItem = demo.Ajustes.Proveedor;
            var filas = await sp.GetRequiredService<ConsultarReportePedidos>().EjecutarAsync(null);
            lista.Cargar(FilasPedidos(filas.Where(x => x.Estado is EstadoPedido.VALIDADO or EstadoPedido.PAGADO or EstadoPedido.DESPACHADO or EstadoPedido.ENTREGADO)), _pedidoActivo);
            if (lista.Seleccion is { } fila)
                await MostrarDetalle(sp, fila.Id);
            else
            {
                referencia.Text = "Selecciona un pedido para ver el desglose.";
                foreach (var tarjeta in new[] { subtotal, descuento, envio, total })
                    tarjeta.Mostrar("—");
                confirmar!.Enabled = false;
            }
        }
        lista.AlSeleccionar(pedido =>
        {
            if (pedido is not null)
                _ = EjecutarAsync(vista, async sp => { await MostrarDetalle(sp, pedido.Id); return null; });
        });
        confirmar = Accion(vista, "Confirmar cobro simulado", async sp =>
        {
            var fila = Elegida(lista);
            var ficha = await sp.GetRequiredService<ConsultarEscritorio>().DetalleAsync(fila.Id);
            if (!Confirmar($"¿Confirmar el cobro simulado de {Tema.Usd(ficha.Valoracion.Total)} para {fila.Cliente}?"))
                return null;
            if (!claves.TryGetValue(fila.Id, out var clave))
                claves.Add(fila.Id, clave = Guid.NewGuid().ToString("N"));
            Elegir(fila.Id);
            var pago = await sp.GetRequiredService<OperacionesPedido>().ConfirmarAsync(
                new ConfirmarPedidoCommand(fila.Id, clave, (ProveedorPago)proveedor.SelectedItem!));
            return await NotificarAsync(CambioComercial.VentaConfirmada, $"Cobro aprobado · {Tema.Usd(pago.Importe)}.");
        }, Operacion.Cobrar, principal: true);
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Proveedor", proveedor), confirmar));
        Tema.Fila(vista.Cuerpo, lista.Control, 100);
        Tema.Fila(vista.Cuerpo, Tema.Acciones(subtotal.Contenedor, descuento.Contenedor, envio.Contenedor, total.Contenedor));
        Tema.Fila(vista.Cuerpo, referencia);
        vista.Cargar = Cargar;
        return vista;
    }

    private static void CargarSelector<T>(ComboBox lista, IEnumerable<T> opciones, object? preferida, Func<T, object> clave) where T : class
    {
        var filas = opciones.ToArray();
        lista.DataSource = filas;
        if (preferida is not null)
            lista.SelectedItem = filas.FirstOrDefault(f => Equals(clave(f), preferida));
    }

    private sealed record OpcionPedido(Guid Id, string Texto);
    private sealed record OpcionArticulo(string Sku, string Texto);
    private sealed record LineaFila(LineaVista Linea)
    {
        public string Sku => Linea.Sku;
        public string Nombre => Linea.Nombre;
        public int Cantidad => Linea.Cantidad;
        public decimal Unitario => Linea.PrecioUnitario;
        public decimal Importe => Linea.PrecioUnitario * Linea.Cantidad;
    }
}
