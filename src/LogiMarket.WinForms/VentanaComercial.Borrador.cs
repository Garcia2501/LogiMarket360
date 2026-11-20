using System.Globalization;
using LogiMarket.Application.Clientes;
using LogiMarket.Application.Pedidos;
using LogiMarket.Application.Seguridad;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal sealed partial class VentanaComercial
{
    private readonly TextBox _identificacionCliente = new() { Width = 100, AccessibleName = "Identificación del cliente" };
    private readonly TextBox _nombreCliente = new() { Width = 180, AccessibleName = "Nombre del cliente" };
    private readonly ComboBox _categoriaCliente = Seleccion<CategoriaCliente>("Categoría del cliente");
    private readonly ComboBox _entrega = Seleccion<ModalidadEntrega>("Modalidad de entrega");
    private readonly TextBox _lineas = new() { Width = 280, AccessibleName = "SKU y cantidades" };
    private Guid? _clienteId;

    private void FilasBorrador()
    {
        _identificacionCliente.TextChanged += (_, _) => _clienteId = null;
        _lineas.PlaceholderText = "KIT_TELETRABAJO:1, P03:2";
        _contenido.Controls.Add(Fila(
            Etiqueta("Identificación"), _identificacionCliente, Etiqueta("Nombre"), _nombreCliente, _categoriaCliente,
            Boton("Registrar cliente", Operacion.GestionarClientes, async sp =>
            {
                var cliente = await sp.GetRequiredService<AdministrarClientes>().RegistrarAsync(
                    _identificacionCliente.Text, _nombreCliente.Text, (CategoriaCliente)_categoriaCliente.SelectedItem!);
                _clienteId = cliente.Id;
                return $"Cliente seleccionado: {cliente.Nombre}.";
            }),
            Boton("Seleccionar cliente", Operacion.EditarPedido, async sp =>
            {
                var identificacion = _identificacionCliente.Text.Trim();
                var candidatos = await sp.GetRequiredService<AdministrarClientes>().BuscarAsync(identificacion);
                var cliente = candidatos.SingleOrDefault(c => c.Identificacion == identificacion)
                    ?? throw new InvalidOperationException("No existe un cliente con esa identificación.");
                _clienteId = cliente.Id;
                _nombreCliente.Text = cliente.Nombre;
                _categoriaCliente.SelectedItem = cliente.Categoria;
                return $"Cliente seleccionado: {cliente.Nombre}.";
            })));

        _contenido.Controls.Add(Fila(Etiqueta("Líneas SKU:cantidad"), _lineas, _entrega,
            Boton("Crear borrador", Operacion.EditarPedido, async sp =>
            {
                var clienteId = _clienteId ?? throw new InvalidOperationException("Selecciona un cliente.");
                var pedido = await sp.GetRequiredService<AdministrarPedidos>().CrearAsync(clienteId,
                    (ModalidadEntrega)_entrega.SelectedItem!, LeerLineas());
                _pedido.Text = pedido.Id.ToString("D");
                return $"Borrador creado. Subtotal USD {pedido.Valoracion.Subtotal:F2}, descuento USD {pedido.Valoracion.Descuento:F2}, " +
                    $"envío USD {pedido.Valoracion.Tarifa:F2}, total USD {pedido.Valoracion.Total:F2}.";
            }),
            Boton("Validar pedido", Operacion.ValidarPedido, async sp =>
            {
                await sp.GetRequiredService<AdministrarPedidos>().ValidarAsync(IdPedido());
                return "Pedido validado.";
            })));
    }

    private LineaSolicitada[] LeerLineas() => _lineas.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(texto =>
        {
            var partes = texto.Split(':', 2, StringSplitOptions.TrimEntries);
            return partes.Length == 2 && int.TryParse(partes[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int cantidad)
                ? new LineaSolicitada(partes[0], cantidad)
                : throw new FormatException("Escribe cada línea como SKU:cantidad y separa varias líneas por comas.");
        }).ToArray();

    private void LimpiarBorrador()
    {
        _identificacionCliente.Clear();
        _nombreCliente.Clear();
        _lineas.Clear();
    }

    private static Label Etiqueta(string texto) => new() { Text = texto, AutoSize = true };
}
