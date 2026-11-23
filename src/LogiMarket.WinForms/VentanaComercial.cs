using LogiMarket.Application.Escritorio;
using LogiMarket.Application.Navegacion;
using LogiMarket.Application.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal sealed partial class VentanaComercial : Form
{
    private const int AnchuraNavegacion = 251;
    private enum Seccion { Panel, Catalogo, Clientes, Carrito, Pedidos, Cobro, Despacho, Devoluciones, Inventario, Reportes, Auditoria }
    private readonly IServiceProvider _servicios;
    private readonly SesionLocal _sesion;
    private readonly FormularioPrincipalMediator _mediador = new();
    private readonly Dictionary<Seccion, (string Nombre, Func<Pantalla> Crear, Operacion Permiso)> _definiciones = [];
    private readonly Dictionary<Seccion, Pantalla> _pantallas = [];
    private readonly Dictionary<Seccion, Button> _enlaces = [];
    private readonly ColumnStyle _columnaNavegacion = new(SizeType.Absolute, AnchuraNavegacion);
    private readonly Panel _centro = new() { Dock = DockStyle.Fill, BackColor = Tema.Fondo };
    private readonly FlowLayoutPanel _navegacion = new()
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
        AutoScroll = true, BackColor = Tema.Barra, Padding = new Padding(12, 15, 12, 10)
    };
    private readonly Label _rol = Tema.TextoPlano();
    private readonly Button _salir = Tema.Boton("Cerrar sesión");
    private readonly ErrorProvider _errores;
    private Seccion? _paginaActiva;
    private Guid? _pedidoActivo;
    private bool _ocupado;

    public VentanaComercial(IServiceProvider servicios, SesionLocal sesion)
    {
        _servicios = servicios;
        _sesion = sesion;
        _errores = new ErrorProvider(this) { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        Text = "LogiMarket 360";
        Font = new Font("Segoe UI", 10f);
        ForeColor = Tema.Texto;
        BackColor = Tema.Fondo;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(900, 630);
        ClientSize = new Size(1210, 790);
        StartPosition = FormStartPosition.CenterScreen;

        var distribucion = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        distribucion.ColumnStyles.Add(_columnaNavegacion);
        distribucion.ColumnStyles.Add(new(SizeType.Percent, 100));
        distribucion.RowStyles.Add(new(SizeType.Absolute, 68));
        distribucion.RowStyles.Add(new(SizeType.Percent, 100));
        var cabecera = Cabecera();
        distribucion.Controls.Add(cabecera, 0, 0);
        distribucion.SetColumnSpan(cabecera, 2);
        distribucion.Controls.Add(_navegacion, 0, 1);
        distribucion.Controls.Add(_centro, 1, 1);
        Controls.Add(distribucion);
        _salir.Click += (_, _) => CerrarSesion();
        RegistrarPantallas();

        _mediador.Conectar(AreaComercial.Catalogo, (_, _) => ActualizarCacheAsync(Seccion.Catalogo));
        _mediador.Conectar(AreaComercial.Carrito, (_, _) => ActualizarCacheAsync(Seccion.Carrito));
        _mediador.Conectar(AreaComercial.Pedidos, async (_, _) =>
        {
            await ActualizarCacheAsync(Seccion.Pedidos);
            if (_paginaActiva is Seccion.Cobro or Seccion.Despacho or Seccion.Devoluciones or Seccion.Reportes)
                await ActualizarCacheAsync(_paginaActiva.Value);
        });
        _mediador.Conectar(AreaComercial.Inventario, (_, _) => ActualizarCacheAsync(Seccion.Inventario));
        _mediador.Conectar(AreaComercial.Metricas, async (_, _) =>
        {
            await ActualizarCacheAsync(Seccion.Panel);
            await ActualizarCacheAsync(Seccion.Auditoria);
        });

        Shown += async (_, _) =>
        {
            if (_sesion.Actual is null)
                MostrarAcceso();
            else
            {
                MostrarMenu();
                await NavegarAsync(Seccion.Panel);
            }
        };
        FormClosing += (_, e) => e.Cancel = _ocupado;
        FormClosed += (_, _) =>
        {
            _sesion.Cerrar();
            foreach (var pantalla in _pantallas.Values)
                pantalla.Dispose();
            _errores.Dispose();
        };
    }

    private Control Cabecera()
    {
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = Tema.Papel, Padding = new Padding(15, 14, 18, 11), Margin = new Padding(0) };
        tabla.ColumnStyles.Add(new(SizeType.Percent, 100));
        tabla.ColumnStyles.Add(new(SizeType.AutoSize));
        tabla.ColumnStyles.Add(new(SizeType.AutoSize));
        tabla.Controls.Add(new Label { Text = "LogiMarket 360", AutoSize = true, ForeColor = Tema.Texto,
            Anchor = AnchorStyles.Left, Font = new Font("Segoe UI", 16, FontStyle.Bold) }, 0, 0);
        _rol.Anchor = AnchorStyles.Right;
        _rol.Margin = new Padding(4, 0, 15, 0);
        tabla.Controls.Add(_rol, 1, 0);
        tabla.Controls.Add(_salir, 2, 0);
        return tabla;
    }

    private void RegistrarPantallas()
    {
        Registrar(Seccion.Panel, "Panel general", ConstruirPanel);
        RegistrarCatalogo();
        RegistrarPedidos();
        RegistrarSeguimiento();
    }

    partial void RegistrarCatalogo();
    partial void RegistrarPedidos();
    partial void RegistrarSeguimiento();

    private void Registrar(Seccion pagina, string nombre, Func<Pantalla> crear, Operacion permiso = Operacion.Consultar) =>
        _definiciones.Add(pagina, (nombre, crear, permiso));

    private void MostrarAcceso()
    {
        _navegacion.Visible = false;
        _columnaNavegacion.Width = 0;
        _salir.Visible = false;
        _rol.Text = EsDemo ? "Base de demostración · sin sesión" : "Sin sesión";
        var tarjeta = Tema.Pilas();
        tarjeta.Padding = new Padding(28);
        tarjeta.BackColor = Tema.Papel;
        tarjeta.Width = 397;
        tarjeta.Height = 301;
        tarjeta.Dock = DockStyle.None;
        var usuario = Tema.TextoCampo("Usuario", 304);
        var clave = Tema.TextoCampo("Contraseña", 304);
        clave.UseSystemPasswordChar = true;
        var ingresar = Tema.Boton("Ingresar", true);
        var mensaje = Tema.TextoPlano();
        mensaje.ForeColor = Tema.Riesgo;
        Tema.Fila(tarjeta, new Label { Text = "Acceso al sistema", Font = new Font("Segoe UI", 18, FontStyle.Bold),
            AutoSize = true, ForeColor = Tema.Texto, Margin = new Padding(0, 0, 0, 10) });
        Tema.Fila(tarjeta, Tema.Campo("Usuario", usuario));
        Tema.Fila(tarjeta, Tema.Campo("Contraseña", clave));
        Tema.Fila(tarjeta, Tema.Acciones(ingresar));
        Tema.Fila(tarjeta, mensaje);
        ingresar.Click += async (_, _) =>
        {
            if (_ocupado || !Requerido(usuario) || !Requerido(clave))
                return;
            _ocupado = true;
            tarjeta.Enabled = false;
            bool iniciada = false;
            try
            {
                await using var scope = _servicios.CreateAsyncScope();
                iniciada = await scope.ServiceProvider.GetRequiredService<AutenticacionLocal>().IniciarAsync(usuario.Text, clave.Text);
                if (!iniciada)
                    mensaje.Text = "Usuario o contraseña incorrectos.";
            }
            catch (Exception e) { mensaje.Text = e.Message; }
            finally
            {
                clave.Clear();
                tarjeta.Enabled = true;
                _ocupado = false;
            }
            if (iniciada)
            {
                _centro.Controls.Remove(tarjeta);
                tarjeta.Dispose();
                MostrarMenu();
                await NavegarAsync(Seccion.Panel);
            }
        };
        void Centrar(object? sender, EventArgs e) =>
            tarjeta.Location = new Point(Math.Max(10, (_centro.ClientSize.Width - tarjeta.Width) / 2),
                Math.Max(15, (_centro.ClientSize.Height - tarjeta.Height) / 2));
        tarjeta.Resize += Centrar;
        _centro.Resize += Centrar;
        tarjeta.Disposed += (_, _) => _centro.Resize -= Centrar;
        _centro.Controls.Add(tarjeta);
        Centrar(null, EventArgs.Empty);
        AcceptButton = ingresar;
        usuario.Select();
    }

    private void MostrarMenu()
    {
        AcceptButton = null;
        _columnaNavegacion.Width = AnchuraNavegacion;
        _navegacion.Visible = _salir.Visible = true;
        var identidad = _sesion.Actual!;
        _rol.Text = $"{identidad.Nombre} · {identidad.Rol}" + (EsDemo ? " · Demostración" : "");
        foreach (var (seccion, configuracion) in _definiciones.OrderBy(d => d.Key))
        {
            if (!Tiene(configuracion.Permiso))
                continue;
            var enlace = Tema.Boton(configuracion.Nombre);
            enlace.AutoSize = false;
            enlace.Width = 219;
            enlace.Height = 37;
            enlace.TextAlign = ContentAlignment.MiddleLeft;
            enlace.Click += async (_, _) => await NavegarAsync(seccion);
            _enlaces.Add(seccion, enlace);
            _navegacion.Controls.Add(enlace);
        }
    }

    private async Task NavegarAsync(Seccion seccion)
    {
        if (_ocupado)
            return;
        var definicion = _definiciones[seccion];
        if (!Tiene(definicion.Permiso))
            return;
        if (!_pantallas.TryGetValue(seccion, out var pagina))
            _pantallas.Add(seccion, pagina = definicion.Crear());
        _paginaActiva = seccion;
        _centro.Controls.Clear();
        _centro.Controls.Add(pagina.Contenedor);
        foreach (var (key, boton) in _enlaces)
            boton.BackColor = key == seccion ? Tema.Suave : Tema.Papel;

        if (pagina.Cargar is { } cargar)
            await EjecutarAsync(pagina, async sp => { await cargar(sp); return null; });
    }

    private void CerrarSesion()
    {
        if (_ocupado)
            return;
        _sesion.Cerrar();
        _pedidoActivo = null;
        _paginaActiva = null;
        _centro.Controls.Clear();
        foreach (var vista in _pantallas.Values)
            vista.Dispose();
        _pantallas.Clear();
        foreach (var enlace in _enlaces.Values)
            enlace.Dispose();
        _enlaces.Clear();
        MostrarAcceso();
    }

    private bool EsDemo => _servicios.GetRequiredService<EstadoDemostracion>().Habilitado;
    private bool Tiene(Operacion op) => _sesion.Actual is { } s && AutorizacionOperaciones.Permite(s.Rol, op);
    private void Elegir(Guid pedido) => _pedidoActivo = pedido;
    private Guid PedidoActual() => _pedidoActivo ?? throw new InvalidOperationException("Selecciona un pedido primero.");

    private Button Accion(Pantalla pantalla, string titulo, Func<IServiceProvider, Task<string?>> accion,
        Operacion permiso = Operacion.Consultar, bool principal = false)
    {
        var boton = Tema.Boton(titulo, principal);
        boton.Enabled = Tiene(permiso);
        boton.Click += async (_, _) => await EjecutarAsync(pantalla, accion);
        return boton;
    }

    private async Task EjecutarAsync(Pantalla pantalla, Func<IServiceProvider, Task<string?>> accion)
    {
        if (_ocupado)
            return;
        _ocupado = true;
        UseWaitCursor = true;
        _salir.Enabled = false;
        try
        {
            await using var scope = _servicios.CreateAsyncScope();
            var mensaje = await accion(scope.ServiceProvider);
            if (mensaje is not null)
                pantalla.Avisar(mensaje);
        }
        catch (Exception e)
        {
            pantalla.Avisar(e.Message, error: true);
        }
        finally
        {
            UseWaitCursor = false;
            _ocupado = false;
            _salir.Enabled = true;
        }
    }

    private async Task ActualizarCacheAsync(Seccion seccion)
    {
        if (!_pantallas.TryGetValue(seccion, out var pantalla) || pantalla.Cargar is not { } cargar)
            return;
        await using var scope = _servicios.CreateAsyncScope();
        await cargar(scope.ServiceProvider);
    }

    private async Task<string> NotificarAsync(CambioComercial cambio, string exito)
    {
        var errores = await _mediador.NotificarAsync(cambio, _pedidoActivo);
        return errores.Count == 0 ? exito : $"{exito} Actualización pendiente en {string.Join(", ", errores.Select(x => x.Area))}.";
    }

    private bool Requerido(TextBox campo)
    {
        var valido = !string.IsNullOrWhiteSpace(campo.Text);
        _errores.SetError(campo, valido ? "" : "Completa este campo.");
        if (!valido)
            campo.Focus();
        return valido;
    }

    private bool Confirmar(string mensaje) => MessageBox.Show(this, mensaje, "Confirmar operación",
        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
}
