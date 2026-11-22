using LogiMarket.Application.Catalogo;
using LogiMarket.Application.Clientes;
using LogiMarket.Application.Escritorio;
using LogiMarket.Application.Navegacion;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Catalogo;
using LogiMarket.Domain.Clientes;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal sealed partial class VentanaComercial
{
    partial void RegistrarCatalogo()
    {
        Registrar(Seccion.Catalogo, "Catálogo y kits", ConstruirCatalogo);
        Registrar(Seccion.Clientes, "Clientes", ConstruirClientes);
        Registrar(Seccion.Inventario, "Inventario", ConstruirInventario);
    }

    private Pantalla ConstruirCatalogo()
    {
        var vista = new Pantalla("Catálogo y kits", "Consulta productos, edita el catálogo y revisa la composición física.");
        var filtro = Tema.TextoCampo("Buscar producto", 205);
        filtro.PlaceholderText = "SKU, nombre o categoría";
        var productos = new Cuadricula<ProductoFila>("Productos del catálogo", p => p.Sku)
            .Col("SKU", nameof(ProductoFila.Sku), 75).Col("Producto", nameof(ProductoFila.Nombre), 160)
            .Col("Categoría", nameof(ProductoFila.Categoria), 95).Col("Precio USD", nameof(ProductoFila.Precio), 80, "N2")
            .Col("Stock", nameof(ProductoFila.Stock), 57).Col("Cantidad", nameof(ProductoFila.Cantidad), 61)
            .Col("Estado", nameof(ProductoFila.Estado), 70);
        var kits = new Cuadricula<KitVisto>("Kits disponibles", k => k.Sku)
            .Col("SKU del kit", nameof(KitVisto.Sku)).Col("Nombre", nameof(KitVisto.Nombre));
        var arbol = new TreeView { AccessibleName = "Composición del kit", BackColor = Tema.Papel, MinimumSize = new Size(0, 95) };
        var detalle = Tema.TextoPlano();
        async Task Cargar(IServiceProvider sp)
        {
            var catalogo = sp.GetRequiredService<AdministrarCatalogo>();
            productos.Cargar((await catalogo.BuscarAsync(filtro.Text)).Select(p =>
                new ProductoFila(p.Sku, p.Nombre, p.Categoria, p.Precio, p.Stock, 1, p.Activo ? "Activo" : "Inactivo")));
            kits.Cargar(await sp.GetRequiredService<ConsultarEscritorio>().KitsAsync());
        }
        async Task<string?> EditarProducto(IServiceProvider sp, bool nuevo)
        {
            var catalogo = sp.GetRequiredService<AdministrarCatalogo>();
            ProductoVisto? original = null;
            if (!nuevo)
            {
                var sku = Elegida(productos).Sku;
                original = (await catalogo.BuscarAsync(sku)).Single(p => p.Sku == sku);
            }
            var codigo = Tema.TextoCampo("SKU");
            codigo.Text = original?.Sku ?? "";
            codigo.ReadOnly = !nuevo;
            var nombre = Tema.TextoCampo("Nombre del producto");
            nombre.Text = original?.Nombre ?? "";
            var categoria = Tema.TextoCampo("Categoría");
            categoria.Text = original?.Categoria ?? "";
            var descripcion = Tema.TextoCampo("Descripción", 270);
            descripcion.Text = original?.Descripcion ?? "";
            var precio = Tema.Numero("Precio USD", original?.Precio ?? 1m, 0.01m, 999999999m, 2);
            var peso = Tema.Numero("Peso en kg", original?.PesoKg ?? 0m, 0m, 999999m, 3);
            var stock = Tema.Numero("Stock inicial", 0, 0, int.MaxValue);
            var campos = new List<(string, Control)> { ("SKU", codigo), ("Nombre", nombre), ("Categoría", categoria),
                ("Descripción", descripcion), ("Precio USD", precio), ("Peso kg", peso) };
            if (nuevo)
                campos.Add(("Stock inicial", stock));
            if (!EditarCampos(nuevo ? "Nuevo producto" : "Editar producto", campos, codigo, nombre))
                return null;
            var datos = new DatosProducto(nombre.Text, precio.Value, peso.Value, categoria.Text, descripcion.Text);
            if (nuevo)
                await catalogo.CrearAsync(codigo.Text, datos, (int)stock.Value);
            else
                await catalogo.ActualizarAsync(codigo.Text, datos);
            return await NotificarAsync(CambioComercial.CatalogoEditado, "Producto guardado.");
        }

        async Task<string?> EditarKit(IServiceProvider sp, bool nuevo)
        {
            var catalogo = sp.GetRequiredService<AdministrarCatalogo>();
            var codigo = Tema.TextoCampo("SKU del kit");
            var nombre = Tema.TextoCampo("Nombre del kit");
            var componentes = Tema.TextoCampo("Componentes SKU:cantidad", 288);
            componentes.PlaceholderText = "KIT_OFICINA:1, P03:1";
            if (!nuevo)
            {
                var raiz = await catalogo.ConsultarEstructuraAsync(Elegida(kits).Sku);
                codigo.Text = raiz.Sku.Valor;
                codigo.ReadOnly = true;
                nombre.Text = raiz.Nombre;
                componentes.Text = string.Join(", ", raiz.Componentes.Select(c => $"{c.Sku.Valor}:{c.Cantidad}"));
            }
            if (!EditarCampos(nuevo ? "Nuevo kit" : "Editar composición", [("SKU del kit", codigo),
                    ("Nombre", nombre), ("Componentes SKU:cantidad, separados por coma", componentes)],
                codigo, nombre, componentes))
                return null;
            var lineas = LeerComponentes(componentes.Text);
            var total = nuevo
                ? await catalogo.CrearKitAsync(codigo.Text, nombre.Text, lineas)
                : await catalogo.ActualizarKitAsync(codigo.Text, nombre.Text, lineas);
            arbol.Nodes.Clear();
            arbol.Nodes.Add(Nodo(await catalogo.ConsultarEstructuraAsync(codigo.Text)));
            arbol.ExpandAll();
            return await NotificarAsync(CambioComercial.CatalogoEditado, $"Kit guardado · {Tema.Usd(total)}.");
        }

        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Buscar producto", filtro), Accion(vista, "Buscar", async sp =>
        {
            await Cargar(sp);
            return "Catálogo actualizado.";
        }, principal: true), Accion(vista, "Nuevo producto", sp => EditarProducto(sp, true), Operacion.GestionarCatalogo),
            Accion(vista, "Editar producto", sp => EditarProducto(sp, false), Operacion.GestionarCatalogo),
            Accion(vista, "Desactivar", async sp =>
            {
                var fila = Elegida(productos);
                if (!Confirmar($"¿Desactivar el producto {fila.Sku}?"))
                    return null;
                await sp.GetRequiredService<AdministrarCatalogo>().DesactivarAsync(fila.Sku);
                return await NotificarAsync(CambioComercial.CatalogoEditado, "Producto desactivado.");
            }, Operacion.GestionarCatalogo), Accion(vista, "Medir 1.000 filas", async sp =>
            {
                var demo = await sp.GetRequiredService<ConsultarCatalogoVisual>().DemostrarAsync();
                productos.Cargar(demo.Renglones.Select(p => new ProductoFila(p.Sku, p.Nombre, p.Categoria, p.Precio,
                    p.Stock, p.Cantidad, "Muestra")));
                var m = demo.Medicion;
                return $"{m.Renglones} filas · Metadatos {m.ObjetosCompartidos}/{m.ObjetosIndependientes} · " +
                    $"Bytes {m.BytesCompartidos:N0}/{m.BytesIndependientes:N0} (compartidos/independientes).";
            })));
        Tema.Fila(vista.Cuerpo, productos.Control, 58);
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Accion(vista, "Ver composición", async sp =>
        {
            var clave = Elegida(kits).Sku;
            var catalogo = sp.GetRequiredService<AdministrarCatalogo>();
            var raiz = await catalogo.ConsultarEstructuraAsync(clave);
            var logistica = await catalogo.ConsultarLogisticaAsync(clave, 1);
            var saldos = await catalogo.ConsultarExistenciasAsync(clave, 1);
            arbol.Nodes.Clear();
            arbol.Nodes.Add(Nodo(raiz));
            arbol.ExpandAll();
            detalle.Text = $"Peso {logistica.PesoKg:F2} kg · " +
                string.Join("  ·  ", saldos.Select(s => $"{s.Sku} × {s.Requeridas} (stock {s.Disponibles})"));
            return $"Composición de {clave}.";
        }), Accion(vista, "Nuevo kit", sp => EditarKit(sp, true), Operacion.GestionarCatalogo),
            Accion(vista, "Editar kit", sp => EditarKit(sp, false), Operacion.GestionarCatalogo)));
        Tema.Fila(vista.Cuerpo, LadoALado(kits.Control, arbol), 42);
        Tema.Fila(vista.Cuerpo, detalle);
        vista.Cargar = Cargar;
        return vista;
    }

    private Pantalla ConstruirClientes()
    {
        var vista = new Pantalla("Gestión de clientes", "Busca por identificación o nombre, registra clientes y actualiza su ficha.");
        var filtro = Tema.TextoCampo("Buscar cliente", 247);
        var tabla = new Cuadricula<ClienteVisto>("Clientes", c => c.Id).Col("Identificación", nameof(ClienteVisto.Identificacion))
            .Col("Nombre", nameof(ClienteVisto.Nombre), 152).Col("Categoría", nameof(ClienteVisto.Categoria), 76)
            .Col("Contacto", nameof(ClienteVisto.Contacto), 145).Col("Activo", nameof(ClienteVisto.Activo), 57);
        Task Cargar(IServiceProvider sp) => CargarTablaAsync(sp);
        async Task CargarTablaAsync(IServiceProvider sp)
        {
            tabla.Cargar(await sp.GetRequiredService<AdministrarClientes>().BuscarAsync(filtro.Text));
        }
        async Task<string?> Editar(IServiceProvider sp, ClienteVisto? cliente)
        {
            var identificacion = Tema.TextoCampo("Identificación");
            identificacion.Text = cliente?.Identificacion ?? "";
            identificacion.ReadOnly = cliente is not null;
            var nombre = Tema.TextoCampo("Nombre del cliente");
            nombre.Text = cliente?.Nombre ?? "";
            var contacto = Tema.TextoCampo("Contacto", 268);
            contacto.Text = cliente?.Contacto ?? "";
            var categoria = Tema.Lista<CategoriaCliente>("Categoría del cliente");
            categoria.SelectedItem = cliente?.Categoria ?? CategoriaCliente.NORMAL;
            var activo = new CheckBox { AutoSize = true, Text = "Activo", AccessibleName = "Cliente activo", Checked = cliente?.Activo ?? true };
            var campos = new List<(string, Control)>
            {
                ("Identificación", identificacion), ("Nombre", nombre), ("Contacto", contacto), ("Categoría", categoria)
            };
            if (cliente is not null)
                campos.Add(("Disponibilidad", activo));
            if (!EditarCampos(cliente is null ? "Nuevo cliente" : "Editar cliente", campos, identificacion, nombre))
                return null;
            var servicio = sp.GetRequiredService<AdministrarClientes>();
            if (cliente is null)
                await servicio.RegistrarAsync(identificacion.Text, nombre.Text, (CategoriaCliente)categoria.SelectedItem!, contacto.Text);
            else
                await servicio.ActualizarAsync(cliente.Id, nombre.Text, (CategoriaCliente)categoria.SelectedItem!, contacto.Text, activo.Checked);
            await Cargar(sp);
            return "Ficha del cliente guardada.";
        }
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Buscar cliente", filtro), Accion(vista, "Buscar", async sp =>
        {
            await Cargar(sp);
            return "Resultados actualizados.";
        }, principal: true), Accion(vista, "Nuevo cliente", sp => Editar(sp, null), Operacion.GestionarClientes),
            Accion(vista, "Editar cliente", sp => Editar(sp, Elegida(tabla)), Operacion.GestionarClientes)));
        Tema.Fila(vista.Cuerpo, tabla.Control, 100);
        vista.Cargar = Cargar;
        return vista;
    }

    private Pantalla ConstruirInventario()
    {
        var vista = new Pantalla("Inventario y movimientos", "Existencias actuales y trazabilidad de ventas, devoluciones y ajustes.");
        var sku = Tema.TextoCampo("Filtrar movimientos por SKU", 168);
        var saldos = new Cuadricula<SaldoVisual>("Existencias actuales", p => p.Sku).Col("SKU", nameof(SaldoVisual.Sku))
            .Col("Producto", nameof(SaldoVisual.Nombre), 155).Col("Disponible", nameof(SaldoVisual.Stock), 70)
            .Col("Alerta", nameof(SaldoVisual.Alerta), 64);
        var movimientos = new Cuadricula<MovimientoVisto>("Movimientos de inventario", m => m.Id)
            .Col("Fecha UTC", nameof(MovimientoVisto.FechaUtc), 103, "dd/MM/yyyy HH:mm").Col("SKU", nameof(MovimientoVisto.Sku), 50)
            .Col("Tipo", nameof(MovimientoVisto.Tipo), 82).Col("Cantidad", nameof(MovimientoVisto.Cantidad), 60)
            .Col("Saldo", nameof(MovimientoVisto.Saldo), 57).Col("Motivo", nameof(MovimientoVisto.Motivo), 110)
            .Col("Referencia", nameof(MovimientoVisto.Referencia), 150);
        async Task Cargar(IServiceProvider sp)
        {
            var catalogo = sp.GetRequiredService<AdministrarCatalogo>();
            saldos.Cargar((await catalogo.BuscarAsync()).Where(p => p.Activo).Select(p =>
                new SaldoVisual(p.Sku, p.Nombre, p.Stock, p.Stock <= 3 ? "Bajo" : "Disponible")));
            movimientos.Cargar(await sp.GetRequiredService<ConsultarEscritorio>().MovimientosAsync(sku.Text));
        }
        Tema.Fila(vista.Cuerpo, Tema.Acciones(Tema.Campo("Movimientos por SKU", sku),
            Accion(vista, "Filtrar", async sp => { await Cargar(sp); return "Inventario actualizado."; }, principal: true),
            Accion(vista, "Registrar ajuste", async sp =>
            {
                var producto = Elegida(saldos);
                var cantidad = Tema.Numero("Cantidad del ajuste", 1, -int.MaxValue, int.MaxValue);
                var motivo = Tema.TextoCampo("Motivo del ajuste", 285);
                if (!EditarCampos($"Ajuste de {producto.Sku}", [("Cantidad con signo", cantidad), ("Motivo", motivo)], motivo))
                    return null;
                if (cantidad.Value == 0)
                    throw new ArgumentException("La cantidad debe ser distinta de cero.");
                if (!Confirmar($"¿Registrar un ajuste de {cantidad.Value} unidades para {producto.Sku}?"))
                    return null;
                var resultado = await sp.GetRequiredService<AdministrarCatalogo>().AjustarAsync(producto.Sku, (int)cantidad.Value, motivo.Text);
                return await NotificarAsync(CambioComercial.CatalogoEditado, $"Ajuste registrado. Disponible: {resultado.Stock}.");
            }, Operacion.GestionarCatalogo)));
        Tema.Fila(vista.Cuerpo, saldos.Control, 43);
        Tema.Fila(vista.Cuerpo, Tema.TextoPlano("Movimientos del inventario · últimos 500"));
        Tema.Fila(vista.Cuerpo, movimientos.Control, 57);
        vista.Cargar = Cargar;
        return vista;
    }

    private static T Elegida<T>(Cuadricula<T> tabla) where T : class =>
        tabla.Seleccion ?? throw new InvalidOperationException("Selecciona una fila de la tabla.");

    private static TreeNode Nodo(ComponenteCatalogoSnapshot dato) =>
        new($"{dato.Sku.Valor} · {dato.Nombre} × {dato.Cantidad}", dato.Componentes.Select(Nodo).ToArray());

    private static ComponenteSolicitado[] LeerComponentes(string texto) =>
        texto.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(item =>
        {
            var partes = item.Split(':', 2, StringSplitOptions.TrimEntries);
            return partes.Length == 2 && int.TryParse(partes[1], out var unidades) && unidades > 0 && partes[0].Length > 0
                ? new ComponenteSolicitado(partes[0], unidades)
                : throw new FormatException("Usa SKU:cantidad positiva y separa las líneas con comas.");
        }).ToArray();

    private static Control LadoALado(Control uno, Control dos)
    {
        var tabla = new TableLayoutPanel { ColumnCount = 2, RowCount = 1, Dock = DockStyle.Fill };
        tabla.ColumnStyles.Add(new(SizeType.Percent, 50));
        tabla.ColumnStyles.Add(new(SizeType.Percent, 50));
        uno.Dock = dos.Dock = DockStyle.Fill;
        tabla.Controls.Add(uno, 0, 0);
        tabla.Controls.Add(dos, 1, 0);
        return tabla;
    }

    private bool EditarCampos(string titulo, IReadOnlyList<(string Nombre, Control Control)> campos, params TextBox[] requeridos)
    {
        using var dialogo = new Form { Text = titulo, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false, BackColor = Tema.Fondo, Font = Font, ClientSize = new Size(440, 98 + 59 * campos.Count) };
        using var errores = new ErrorProvider(dialogo) { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        var cuerpo = Tema.Pilas();
        cuerpo.Padding = new Padding(20, 13, 20, 9);
        foreach (var (nombre, control) in campos)
            Tema.Fila(cuerpo, Tema.Campo(nombre, control));
        var guardar = Tema.Boton("Guardar", true);
        var cancelar = Tema.Boton("Cancelar");
        cancelar.DialogResult = DialogResult.Cancel;
        guardar.Click += (_, _) =>
        {
            var faltante = requeridos.FirstOrDefault(s => string.IsNullOrWhiteSpace(s.Text));
            if (faltante is not null)
            {
                errores.SetError(faltante, "Completa este campo.");
                faltante.Focus();
                return;
            }
            dialogo.DialogResult = DialogResult.OK;
        };
        Tema.Fila(cuerpo, Tema.Acciones(guardar, cancelar));
        dialogo.Controls.Add(cuerpo);
        dialogo.AcceptButton = guardar;
        dialogo.CancelButton = cancelar;
        var confirmado = dialogo.ShowDialog(this) == DialogResult.OK;
        foreach (var (_, control) in campos)
            control.Parent?.Controls.Remove(control);
        return confirmado;
    }

    private sealed record ProductoFila(string Sku, string Nombre, string? Categoria, decimal Precio, int Stock, int Cantidad, string Estado);
    private sealed record SaldoVisual(string Sku, string Nombre, int Stock, string Alerta);
}
