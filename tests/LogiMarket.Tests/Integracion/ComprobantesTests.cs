using LogiMarket.Application.Devoluciones;
using LogiMarket.Application.Documentos;
using LogiMarket.Application.Entregas;
using LogiMarket.Application.Pagos;
using LogiMarket.Domain.Clientes;
using LogiMarket.Domain.Pedidos;
using LogiMarket.Domain.Seguridad;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static LogiMarket.Tests.Integracion.ConfirmacionTests;

namespace LogiMarket.Tests.Integracion;

public sealed class ComprobantesTests
{
    [Fact]
    public async Task BridgeEntregaVentaYDevolucionACadaUnoDeLosDosMedios()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "Ana", CategoriaCliente.VIP, ModalidadEntrega.ESTANDAR,
            [new("KIT_TELETRABAJO", 1)]);
        await ConfirmarAsync(entorno, id, "comprobantes", ProveedorPago.LEGADO);

        await entorno.EntrarAsync(Rol.BODEGA);
        await using (var bodega = entorno.Abrir())
        {
            var operaciones = bodega.ServiceProvider.GetRequiredService<DespacharPedidos>();
            await operaciones.DespacharAsync(id);
            await operaciones.EntregarAsync(id);
        }
        await entorno.EntrarAsync(Rol.ADMIN);
        await using (var admin = entorno.Abrir())
            await admin.ServiceProvider.GetRequiredService<AdministrarDevoluciones>().DevolverAsync(id, MotivoDevolucion.DEFECTO, false);
        await entorno.EntrarAsync(Rol.CONSULTA);
        await using var scope = entorno.Abrir();
        var servicio = scope.ServiceProvider.GetRequiredService<EmitirComprobantes>();
        var fabrica = scope.ServiceProvider.GetRequiredService<IFabricaArchivoTxt>();
        var presentados = new List<DocumentoInterno>();
        var visor = new VistaPrevia((documento, _) =>
        {
            presentados.Add(documento);
            return Task.CompletedTask;
        });
        foreach (var tipo in Enum.GetValues<TipoComprobante>())
        {
            await servicio.EjecutarAsync(id, tipo, visor);
            var ruta = Path.Combine(Path.GetTempPath(), $"logimarket-{Guid.NewGuid():N}.txt");
            try
            {
                await servicio.EjecutarAsync(id, tipo, fabrica.Crear(ruta));
                Assert.Equal($"{presentados[^1].Titulo}{Environment.NewLine}{presentados[^1].Contenido}",
                    await File.ReadAllTextAsync(ruta));
            }
            finally
            {
                File.Delete(ruta);
            }
        }

        Assert.Equal(2, presentados.Count);
        Assert.Contains("Total cobrado USD 59.00", presentados[0].Contenido);
        Assert.Contains("Importe reembolsado USD 54.00", presentados[1].Contenido);
        Assert.All(presentados, d => Assert.Contains("Sin validez tributaria", d.Contenido));
    }

    [Fact]
    public async Task DeniegaSinSesionYRechazaComprobanteSinPagoNiDevolucion()
    {
        await using var entorno = await CrearEntornoAsync();
        var id = await CrearPedidoAsync(entorno, "pendiente", CategoriaCliente.NORMAL, ModalidadEntrega.ESTANDAR,
            [new("P01", 1)]);
        await using var scope = entorno.Abrir();
        var sp = scope.ServiceProvider;
        var servicio = sp.GetRequiredService<EmitirComprobantes>();
        var salida = new VistaPrevia((_, _) => throw new InvalidOperationException("No se debe mostrar."));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EjecutarAsync(id, TipoComprobante.Venta, salida));
        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EjecutarAsync(id, TipoComprobante.Devolucion, salida));
        sp.GetRequiredService<LogiMarket.Application.Seguridad.SesionLocal>().Cerrar();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => servicio.EjecutarAsync(id, TipoComprobante.Venta, salida));
    }
}
