using LogiMarket.Application.Navegacion;
using LogiMarket.Application.Comandos;
using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Eventos;
using LogiMarket.Application.Pagos;
using LogiMarket.Application.Seguridad;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal sealed partial class VentanaComercial : Form
{
    private readonly IServiceProvider _servicios;
    private readonly SesionLocal _sesion;
    private readonly TextBox _usuario = new() { Width = 180, AccessibleName = "Usuario" };
    private readonly TextBox _clave = new() { Width = 180, UseSystemPasswordChar = true, AccessibleName = "Contraseña" };
    private readonly TextBox _pedido = new() { Width = 278, AccessibleName = "Identificador del pedido" };
    private readonly TextBox _idempotencia = new() { Width = 278, AccessibleName = "Clave de confirmación" };
    private readonly ComboBox _proveedor = Seleccion<ProveedorPago>("Proveedor simulado");
    private readonly ComboBox _motivo = Seleccion<MotivoDevolucion>("Motivo de devolución");
    private readonly ComboBox _canal = Seleccion<CanalAviso>("Canal simulado");
    private readonly CheckBox _sinUso = new() { Text = "Producto sin uso", AutoSize = true };
    private readonly Label _estado = new() { AutoSize = true, AccessibleName = "Estado de sesión" };
    private readonly Label _mensaje = new() { AutoSize = true, MaximumSize = new Size(680, 0), AccessibleName = "Resultado" };
    private readonly List<(Button Boton, Operacion Permiso)> _operaciones = [];
    private readonly Button _entrar = new() { Text = "Iniciar sesión", AutoSize = true };
    private readonly Button _salir = new() { Text = "Cerrar sesión", AutoSize = true };
    private readonly FlowLayoutPanel _contenido = new()
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
        Padding = new Padding(16)
    };

    public VentanaComercial(IServiceProvider servicios, SesionLocal sesion)
    {
        _servicios = servicios;
        _sesion = sesion;
        Text = "LogiMarket 360";
        AutoScaleMode = AutoScaleMode.Font;
        MinimumSize = new Size(830, 500);
        ClientSize = new Size(960, 545);
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(_contenido);
        FilasSesion();
        FilasBorrador();
        FilasOperaciones();
        _contenido.Controls.Add(_mensaje);
        ConfigurarConsultas();
        ActualizarSesion();
    }

    private void FilasSesion()
    {
        _idempotencia.Text = Guid.NewGuid().ToString("N");
        _pedido.TextChanged += (_, _) => _idempotencia.Text = Guid.NewGuid().ToString("N");
        _entrar.Click += async (_, _) => await EjecutarAsync(async sp =>
        {
            var permitido = await sp.GetRequiredService<AutenticacionLocal>().IniciarAsync(_usuario.Text, _clave.Text);
            _clave.Clear();
            return permitido ? await RefrescarAsync(CambioComercial.Inicio, null, "Sesión iniciada.") : "Usuario o contraseña incorrectos.";
        });
        _salir.Click += (_, _) =>
        {
            _sesion.Cerrar();
            _usuario.Clear();
            _clave.Clear();
            _pedido.Clear();
            LimpiarBorrador();
            LimpiarConsultas();
            _idempotencia.Text = Guid.NewGuid().ToString("N");
            _mensaje.Text = "Sesión cerrada.";
            ActualizarSesion();
        };
        _contenido.Controls.Add(Fila(
            new Label { Text = "Usuario", AutoSize = true }, _usuario,
            new Label { Text = "Contraseña", AutoSize = true }, _clave, _entrar, _salir));
        _contenido.Controls.Add(_estado);
    }

    private void FilasOperaciones()
    {
        _contenido.Controls.Add(Fila(new Label { Text = "Pedido (GUID)", AutoSize = true }, _pedido,
            Boton("Seguimiento", Operacion.Consultar, async sp =>
            {
                var pedido = await sp.GetRequiredService<DespacharPedidos>().ConsultarAsync(IdPedido());
                return await RefrescarAsync(CambioComercial.EstadoPedido, pedido.Id,
                    pedido.Despacho is { } despacho ? $"{pedido.Estado} · {despacho.Guia}" : pedido.Estado.ToString());
            })));
        _contenido.Controls.Add(Fila(new Label { Text = "Clave de confirmación", AutoSize = true }, _idempotencia,
            _proveedor, Boton("Confirmar cobro", Operacion.Cobrar, async sp =>
            {
                var pago = await sp.GetRequiredService<OperacionesPedido>().ConfirmarAsync(
                    new(IdPedido(), _idempotencia.Text, (ProveedorPago)_proveedor.SelectedItem!));
                return await RefrescarAsync(CambioComercial.VentaConfirmada, pago.PedidoId, $"Pago USD {pago.Importe:F2} · {pago.Referencia}");
            })));
        _contenido.Controls.Add(Fila(
            Boton("Cancelar pedido", Operacion.CancelarPedido, async sp =>
            {
                await sp.GetRequiredService<OperacionesPedido>().CancelarAsync(new(IdPedido()));
                return await RefrescarAsync(CambioComercial.EstadoPedido, IdPedido(), "Pedido cancelado.");
            }),
            Boton("Despachar", Operacion.Despachar, async sp =>
            {
                var despacho = await sp.GetRequiredService<OperacionesPedido>().DespacharAsync(new(IdPedido()));
                return await RefrescarAsync(CambioComercial.EstadoPedido, despacho.PedidoId, $"Guía {despacho.Guia} · {despacho.PlazoDias} día(s)");
            }),
            Boton("Registrar entrega", Operacion.Entregar, async sp =>
            {
                await sp.GetRequiredService<DespacharPedidos>().EntregarAsync(IdPedido());
                return await RefrescarAsync(CambioComercial.EstadoPedido, IdPedido(), "Entrega registrada.");
            })));
        _contenido.Controls.Add(Fila(_motivo, _sinUso, Boton("Devolver pedido", Operacion.Devolver, async sp =>
        {
            var resultado = await sp.GetRequiredService<OperacionesPedido>().DevolverAsync(
                new(IdPedido(), (MotivoDevolucion)_motivo.SelectedItem!, _sinUso.Checked));
            return await RefrescarAsync(CambioComercial.DevolucionAceptada, resultado.PedidoId,
                $"Reembolso simulado USD {resultado.Importe:F2} · {resultado.ReferenciaReembolso}");
        })));
        _contenido.Controls.Add(Fila(_canal, Boton("Procesar Outbox", Operacion.ReprocesarEventos, async sp =>
        {
            var resultado = await sp.GetRequiredService<ReprocesarEventos>().EjecutarAsync((CanalAviso)_canal.SelectedItem!);
            return await RefrescarAsync(CambioComercial.EventosProcesados, null,
                $"Eventos procesados: {resultado.Procesados}. Pendientes por fallo: {resultado.Fallidos}.");
        })));
    }

    private Button Boton(string texto, Operacion permiso, Func<IServiceProvider, Task<string>> accion)
    {
        var boton = new Button { Text = texto, AutoSize = true };
        boton.Click += async (_, _) => await EjecutarAsync(accion);
        _operaciones.Add((boton, permiso));
        return boton;
    }

    private async Task EjecutarAsync(Func<IServiceProvider, Task<string>> accion)
    {
        _contenido.Enabled = false;
        try
        {
            await using var scope = _servicios.CreateAsyncScope();
            _mensaje.Text = await accion(scope.ServiceProvider);
        }
        catch (Exception error)
        {
            _mensaje.Text = error.Message;
        }
        finally
        {
            _contenido.Enabled = true;
            ActualizarSesion();
        }
    }

    private void ActualizarSesion()
    {
        var identidad = _sesion.Actual;
        _estado.Text = identidad is null ? "Sin sesión" : $"{identidad.Nombre} · {identidad.Rol}";
        _entrar.Enabled = _usuario.Enabled = _clave.Enabled = identidad is null;
        _salir.Enabled = identidad is not null;
        AcceptButton = identidad is null ? _entrar : null;
        foreach (var (boton, permiso) in _operaciones)
            boton.Enabled = identidad is not null && AutorizacionOperaciones.Permite(identidad.Rol, permiso);
    }

    private Guid IdPedido() => Guid.TryParse(_pedido.Text, out var id)
        ? id : throw new ArgumentException("Indica un identificador de pedido válido.");

    private static ComboBox Seleccion<T>(string nombre) where T : struct, Enum =>
        new() { DropDownStyle = ComboBoxStyle.DropDownList, DataSource = Enum.GetValues<T>(), AccessibleName = nombre };

    private static FlowLayoutPanel Fila(params Control[] controles)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        panel.Controls.AddRange(controles);
        return panel;
    }
}
